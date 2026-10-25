using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>What kind of next step the advisor suggests (docs/v1.1-design.html §4)</summary>
public enum AdvisorKind
{
    /// <summary>A network's buildings get less than they need: the producer that fills it</summary>
    Shortage,

    /// <summary>A network short of pump stations</summary>
    Pump,

    /// <summary>A trading post that sells all it can: upgrade it</summary>
    Full,

    /// <summary>Pipes that carry all they can: a better tier (or the research for it)</summary>
    Pipes,

    /// <summary>Research points to spend and the labs idle: the tech that helps most</summary>
    Research,

    /// <summary>The upgrade that pays back quickest (milestones count their doubling)</summary>
    Upgrade,

    /// <summary>A new building that turns what the company makes into something worth more, or mines what its land holds</summary>
    Chain,

    /// <summary>A neighbour that needs what the company has spare</summary>
    Partner,

    /// <summary>The next parcel, with what it holds</summary>
    Parcel,

    /// <summary>A building that sells nothing because it isn't piped to the trading post, or no producer at all yet</summary>
    Start,

    /// <summary>A building whose goods can't leave (F2): the pipe that lets them out</summary>
    Stuck,

    /// <summary>A shared goal gold can give to: the Era Wonder's goods, the station's storey (after run 4)</summary>
    Shared,
}

/// <summary>What the step's button does on the client</summary>
public enum AdvisorAction
{
    /// <summary>Upgrades Building by Levels (Show me selects it)</summary>
    Upgrade,

    /// <summary>The build tool with BuildingType, the camera at At</summary>
    Build,

    /// <summary>Selects Building</summary>
    Select,

    /// <summary>Opens the research at Research</summary>
    Research,

    /// <summary>Selects Parcel</summary>
    Parcel,

    /// <summary>Plans a pipe to Partner</summary>
    Partner,

    /// <summary>Shows the pipe on Tiles as a ghost to lay with one click</summary>
    LayPipe,

    /// <summary>Goes through Building's pipe door: the next pipe tier or Widen on its full links, then the levels it carries (Show me selects it)</summary>
    Climb,

    /// <summary>The pipe tool, at Building</summary>
    Connect,

    /// <summary>Opens the Hub at your town and its Wonder (the goods to give)</summary>
    Wonder,
}

/// <summary>
/// One of the advisor's next steps: what to do, why in numbers, its cost and what it brings per minute (from the same
/// flow solution the network view shows), and how long until the company can afford it. Count: how many of
/// BuildingType to build.
/// </summary>
public sealed record AdvisorStep(
    AdvisorKind Kind,
    string Title,
    string Reason,
    AdvisorAction Action,
    double Cost,
    double GainPerMinute,
    double? WaitMinutes = null,
    long? Building = null,
    string? BuildingType = null,
    int Levels = 0,
    int? Parcel = null,
    long? Partner = null,
    TilePos? At = null,
    string? Research = null,
    int Count = 1,
    int? Network = null,
    IReadOnlyList<TilePos>? Tiles = null,
    int Tier = 0)
{
    /// <summary>Minutes until it has earned its cost back (null when it costs nothing or brings no gold)</summary>
    public double? PaybackMinutes => GainPerMinute > 0.01 && Cost > 0 ? Cost / GainPerMinute : null;
}

/// <summary>
/// The advisor (docs/v1.1-design.html §4): the three best things a company can do right now. Candidates of every kind
/// are scored by the gold per minute they bring per gold they cost, plus a few minutes of income for the effort (so a
/// tiny gain never wins by being cheap); fixing what is broken (a shortage, too few pumps) counts triple, idle research
/// has a floor. What an upgrade or a new building brings is measured by solving its network again as if it were done.
/// Never a locked building, never a spot that doesn't exist; a step the company can't afford yet says when it can.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>
    /// The most what-ifs (networks solved again) one call of the advisor makes: a count, not a time, so the same estate
    /// gets the same advice on every machine (big networks are estimated, they need none)
    /// </summary>
    private const int AdvisorMaxWhatIfs = 40;

    private int _whatIfs;

    private bool WhatIfsLeft(int reserve = 0) => _whatIfs < AdvisorMaxWhatIfs - reserve;


    /// <summary>Upgrades and chains that would take longer than this to afford aren't suggested</summary>
    private const double AdvisorMaxWaitMinutes = 240;

    /// <summary>
    /// Every step costs the player a moment too: it counts like this many minutes of income on top of its gold, so a
    /// tiny gain never beats a big one just because it's cheap
    /// </summary>
    private const double AdvisorEffortMinutes = 10;

    /// <summary>A step's effort is at least this share of the bank, so a big bank ranks steps by what they bring</summary>
    private const double AdvisorEffortBankShare = 0.02;

    /// <summary>Fixing what is broken (a shortage, too few pumps) ranks this many times higher than an investment</summary>
    private const double AdvisorFixFactor = 3;

    /// <summary>Idle labs with points waste them: research ranks at least this high</summary>
    private const double AdvisorResearchFloor = 60;

    /// <summary>A step that pays itself back within this many minutes ranks above a gift to a shared goal</summary>
    private const double AdvisorQuickPaybackMinutes = 60;

    public List<AdvisorStep> NextSteps(Company company, int count = 3)
    {
        // Right after a command its networks may not be solved yet: an empty solution would make everything look short
        SolveNetworks();
        _spots.Clear();
        _whatIfs = 0;
        var income = Math.Max(0, GoldPerMinute().GetValueOrDefault(company.Id));
        // With a big bank a step's gold matters less than what it brings (F3: run 1's Cleo was told "+16 for 353 g" with 4.8M banked)
        var effort = Math.Max(200, Math.Max(AdvisorEffortMinutes * income, AdvisorEffortBankShare * company.Gold));
        // Gold per minute gained per gold spent (and effort), scaled to a readable number
        double Worth(double gain, double cost) => 1000 * gain / (cost + effort);
        var own = Networks.Where(t => t.Buildings.Any(b => _buildings[b].Owner == company.Id)).ToList();
        var baseGold = new Dictionary<int, double>();
        double Base(Network network) => baseGold.TryGetValue(network.Id, out var v) ? v : baseGold[network.Id] = WhatIfGold(network, company.Id);
        _values.Clear();

        var candidates = new List<(AdvisorStep Step, double Score)>();
        void Add(AdvisorStep step, double factor)
        {
            // Nothing the player demolished this week (F3)
            if (step.Action == AdvisorAction.Build && step.BuildingType is { } type && DemolishedRecently(company, type))
            {
                return;
            }
            // A building whose materials the Storages lack earns once they're in, not from the click (after run 4: "+235
            // g/min for 12k g, back in 51 min" for a Big Factory that waited for its bricks to the end)
            if (step.Action == AdvisorAction.Build && step.BuildingType is { } built && Content.Buildings.TryGetValue(built, out var definition)
                && MaterialsLine(company, definition) is { Short.Count: > 0 } waits)
            {
                var once = $" once its {Join(waits.Short.Keys.Select(GoodsWord).ToList())} {(waits.Short.Count == 1 && !GoodsWord(waits.Short.Keys.First()).EndsWith('s') ? "is" : "are")} in";
                var reason = System.Text.RegularExpressions.Regex.Replace(step.Reason, @", back in [^.:]+", once);
                step = step with { Reason = reason == step.Reason ? $"{step.Reason.TrimEnd('.')}{once}." : reason };
                // 1.4 §9: the market brings them within minutes at 2.5× (unless the player turned that off): part of its price
                if (company.WantsFromMarket)
                {
                    step = step with { Cost = step.Cost + Math.Ceiling(waits.Short.Sum(t => t.Value * WantMarketPrice(company, t.Key))) };
                }
            }
            var score = step.Kind switch
            {
                AdvisorKind.Research => Math.Max(AdvisorResearchFloor, Worth(step.GainPerMinute, 0)),
                // A gift to a shared goal earns nothing back: as high as research (after run 4)
                AdvisorKind.Shared => AdvisorResearchFloor,
                // A parcel brings land, not gold: like a small share of today's income
                AdvisorKind.Parcel => Worth(Math.Max(1, income * 0.05), step.Cost),
                _ => Worth(step.GainPerMinute, step.Cost),
            } * factor;
            var wait = step.Cost > company.Gold ? (income > 0.01 ? (step.Cost - company.Gold) / income : double.PositiveInfinity) : (double?)null;
            if (wait > AdvisorMaxWaitMinutes)
            {
                return;
            }
            // Something the company has to wait for ranks lower the longer the wait
            candidates.Add((step with { WaitMinutes = wait }, wait is { } w ? score / (1 + w / 30) : score));
        }

        StuckSteps(company, Add);
        foreach (var network in own)
        {
            PumpStep(company, network, Base, Add);
            PipeStep(company, network, Add);
            ShortageSteps(company, network, Base, Add, Worth);
        }
        ResearchStep(company, Add, income, own);
        UpgradeSteps(company, own, Base, Add);
        ChainSteps(company, own, Base, Add);
        RoleStep(company, Add);
        SharedStep(company, Add, income);
        PartnerStep(company, Add);
        NeighbourChainStep(company, Add);
        ParcelStep(company, Add);
        StartSteps(company, Add);

        // A gift earns nothing back: what pays itself back within the hour comes before it, and the gift is bigger after
        // (after run 4's bots: Mia's Trading Post, back in 5 minutes, waited under the storey's purse to day 28)
        var gift = candidates.Where(t => t.Step.Kind == AdvisorKind.Shared).Select(t => t.Score).DefaultIfEmpty(0).Max();
        if (gift > 0)
        {
            candidates = candidates.Select(t => t.Step.Kind != AdvisorKind.Shared && t.Step.WaitMinutes == null && t.Step is { Cost: > 0, GainPerMinute: > 0 }
                && t.Step.Cost / t.Step.GainPerMinute <= AdvisorQuickPaybackMinutes ? (t.Step, t.Score + gift) : t).ToList();
        }

        // The best of each kind first, then the next best of any kind; never two steps with the same title
        var ranked = candidates.OrderByDescending(t => t.Score).DistinctBy(t => t.Step.Title).ToList();
        var steps = ranked.GroupBy(t => t.Step.Kind).Select(t => t.First()).OrderByDescending(t => t.Score).Take(count).ToList();
        steps.AddRange(ranked.Except(steps).Take(count - steps.Count));
        return steps.OrderByDescending(t => t.Score).Select(t => t.Step).ToList();
    }

    // ---------------------------------------------------------------- What-ifs

    /// <summary>
    /// A company's gold per minute in a network with some of its nodes changed and some added (linked like an anchor
    /// building): what an upgrade or a new building would bring. Without tolls, so compare it with the same call
    /// unchanged.
    /// </summary>
    private double WhatIfGold(Network network, long company, Func<FlowNode, FlowNode>? change = null, IReadOnlyList<FlowNode>? extra = null, long? anchor = null, bool pumps = true, Func<FlowLink, FlowLink>? changeLink = null)
    {
        _whatIfs++;
        var nodes = network.Buildings.Select(t => ToFlowNode(_buildings[t])).OfType<FlowNode>().ToList();
        if (pumps)
        {
            nodes = WithPumps(network, nodes);
        }
        if (change != null)
        {
            nodes = nodes.Select(change).ToList();
        }
        var links = LinksBetween(network, nodes);
        if (changeLink != null)
        {
            links = links.Select(changeLink).ToList();
        }
        if (extra is { Count: > 0 })
        {
            // A new building piped to the anchor: next to it, and reaching what it reaches
            var near = anchor ?? nodes.FirstOrDefault(t => t.Owner == company)?.Id;
            foreach (var node in extra)
            {
                if (near is { } a)
                {
                    links.Add(new FlowLink(node.Id, a, 0.99, double.PositiveInfinity, []));
                    links.AddRange(network.Links.Where(t => t.A == a || t.B == a).Select(t => t with { A = node.Id, B = t.A == a ? t.B : t.A }));
                }
            }
            nodes.AddRange(extra);
        }
        return nodes.Count < 2 ? 0 : FlowSolver.Solve(nodes, links, MarketFor(network)).GoldPerMinute.GetValueOrDefault(company);
    }

    /// <summary>
    /// Networks this big take too long to solve again for every what-if (a district-sized network took 100 ms a solve):
    /// their gains are estimated from the last solution, with what one more unit of each resource is worth there
    /// </summary>
    private static bool Estimated(Network network) => network.Buildings.Count > 30 || network.Links.Count > 400;

    /// <summary>A gain for a step's text: estimated ones say "about"</summary>
    private static string Plus(Network network, double gain) => $"{(Estimated(network) ? "about " : "")}+{Gold(gain)}/min";

    /// <summary>Whether one of the company's buildings makes a resource now (it runs)</summary>
    private bool IsMaking(Company company, string resource) =>
        BuildingsOf(company.Id).Any(t => Content.Building(t.Type).Out.ContainsKey(resource) && IsMaking(company, t));

    private bool IsMaking(Company company, Building building) =>
        !building.UnderConstruction && NetworkOf(building.Id) is { } network && network.Solution.Rate(building.Id) > 0.05;

    /// <summary>What one more unit of each resource is worth in a network (per call of the advisor), what its buildings are short of, and the buildings on full pipes</summary>
    private readonly Dictionary<int, (Dictionary<string, double> Value, HashSet<string> Short, HashSet<long> OnFullLinks)> _values = new();

    private (Dictionary<string, double> Value, HashSet<string> Short, HashSet<long> OnFullLinks) ValuesOf(Network network, Company company)
    {
        if (_values.TryGetValue(network.Id, out var known))
        {
            return known;
        }
        var flows = network.Solution.Flows;
        var shortOf = NetworkNeeds(network, company.Id)
            .Where(t => t.Value.Consumers.Count > 0 && t.Value.Needed - t.Value.Got > Math.Max(0.05, t.Value.Needed * 0.05))
            .Select(t => t.Key)
            .ToHashSet();
        var post = network.Buildings.Select(t => _buildings[t]).Any(t => t.Owner == company.Id && Content.Building(t.Type).Kind == BuildingKind.TradingPost);
        var value = new Dictionary<string, double>();
        foreach (var resource in network.Buildings.SelectMany(t => Content.Building(_buildings[t].Type).Out.Keys.Concat(Content.Building(_buildings[t].Type).In.Keys)).Distinct())
        {
            // What it sells for here, or would at the hub
            var sales = flows.Where(t => t.Kind == FlowKind.Sale && t.Resource == resource && t.Delivered > 0).ToList();
            var price = sales.Count > 0 ? sales.Sum(t => t.Delivered * t.PricePerUnit) / sales.Sum(t => t.Delivered) : post ? HubBuyPrice(Company(network.Owner), network.District, resource) : 0;
            if (shortOf.Contains(resource))
            {
                // Short: worth what the buildings that wait for it make of a unit
                var consumer = network.Buildings.Select(t => _buildings[t]).Where(t => t.Owner == company.Id).Select(t => Content.Building(t.Type)).FirstOrDefault(t => t.In.ContainsKey(resource));
                if (consumer != null)
                {
                    price = Math.Max(price, consumer.Out.Sum(t => t.Value * HubBuyPrice(Company(network.Owner), network.District, t.Key)) / consumer.In[resource]);
                }
            }
            value[resource] = price;
        }
        var onFull = FullLinks(network).SelectMany(t => new[] { t.A, t.B }).ToHashSet();
        return _values[network.Id] = (value, shortOf, onFull);
    }

    /// <summary>
    /// The estimated gain of a building's node changed (an upgrade) or added (old: null): more of what it makes at what
    /// that is worth here, less what its extra inputs would have sold for. Nothing when it's held back (it lacks an
    /// input, or nothing takes its output), or when an input it needs more of is short; a third when its pipes are full.
    /// It makes only as much more as its extra inputs are there for (after run 4: Cleo's Small Factory Max promised
    /// +257 g/min on ore her mine didn't make, and brought +133).
    /// </summary>
    private double EstimateChange(Network network, Company company, FlowNode? old, FlowNode? changed)
    {
        if (changed == null || (old != null && network.Solution.Rate(old.Id) < old.MaxRate * 0.98 && old.Kind == FlowNodeKind.Building))
        {
            return 0;
        }
        var values = ValuesOf(network, company);
        if (changed.Kind == FlowNodeKind.TradingPost && old != null)
        {
            return 0;
        }
        var gain = 0.0;
        var share = 1.0;
        foreach (var (resource, amount) in changed.In)
        {
            var more = amount - (old?.In.GetValueOrDefault(resource) ?? 0);
            if (more > 1e-9)
            {
                if (values.Short.Contains(resource))
                {
                    return 0;
                }
                share = Math.Min(share, SpareHere(network, company, resource) / more);
                gain -= more * HubBuyPrice(Company(network.Owner), network.District, resource);
            }
        }
        foreach (var (resource, amount) in changed.Out)
        {
            // A good the network doesn't make yet sells at the company's trading post, at what its Hub Order still takes
            var worth = values.Value.TryGetValue(resource, out var known) ? known
                : network.Buildings.Any(t => _buildings[t].Owner == company.Id && Content.Building(_buildings[t].Type).Kind == BuildingKind.TradingPost)
                    ? HubOrderValue(company, resource, amount - (old?.Out.GetValueOrDefault(resource) ?? 0))
                    : 0;
            gain += (amount - (old?.Out.GetValueOrDefault(resource) ?? 0)) * worth;
        }
        gain *= Math.Max(0, share);
        if (old != null && values.OnFullLinks.Contains(old.Id))
        {
            gain *= 0.3;
        }
        return Math.Max(0, gain);
    }

    /// <summary>
    /// What more of a resource a company's building could get in a network: what the company sells and trades of it
    /// there now, and what its makers there could make on top
    /// </summary>
    private static double SpareHere(Network network, Company company, string resource)
    {
        var mine = network.Nodes.Where(t => t.Owner == company.Id).ToDictionary(t => t.Id);
        var flows = network.Solution.Flows.Where(t => t.Resource == resource && mine.ContainsKey(t.From)).ToList();
        var leaving = flows.Where(t => t.Kind is FlowKind.Sale or FlowKind.Trade).Sum(t => t.Sent);
        var unused = mine.Values.Where(t => t.Out.ContainsKey(resource))
            .Sum(t => Math.Max(0, t.Out[resource] * (t.MaxRate - network.Solution.Rate(t.Id))));
        return leaving + unused;
    }

    /// <summary>What a company's Hub Order for a good still takes a minute beyond what it sells there now (1.4 §8)</summary>
    private double HubOrderRoom(Company company, string resource) =>
        Math.Max(0, HubOrder(company, resource) - HubMarketOf(company, company.District).SoldRate.GetValueOrDefault(resource));

    /// <summary>
    /// What a unit of more of a good brings at the company's market: the full price while its Hub Order has room, about
    /// half the price beyond it (the square-root curve's margin)
    /// </summary>
    private double HubOrderValue(Company company, string resource, double units)
    {
        var price = HubBuyPrice(company, resource);
        var room = HubOrderRoom(company, resource);
        return units <= 1e-9 ? price : price * (Math.Min(units, room) + Math.Max(0, units - room) * 0.5) / units;
    }

    /// <summary>What flows over the full links of a network is worth a minute</summary>
    private double FullLinkValue(Network network, IEnumerable<FlowLink> full)
    {
        var pairs = full.Select(t => (Math.Min(t.A, t.B), Math.Max(t.A, t.B))).ToHashSet();
        return network.Solution.Flows
            .Where(t => pairs.Contains((Math.Min(t.From, t.To), Math.Max(t.From, t.To))))
            .Sum(t => t.Delivered * (t.PricePerUnit > 0 ? t.PricePerUnit : HubBuyPrice(Company(network.Owner), network.District, t.Resource)));
    }

    /// <summary>A building that isn't there, as the solver would see it at a spot (its wind, sun, deposit)</summary>
    private FlowNode? HypotheticalNode(Company company, BuildingDefinition definition, TilePos spot, long id)
    {
        var building = new Building { Id = id, Type = definition.Id, Owner = company.Id, X = spot.X, Y = spot.Y, Size = definition.Size };
        return ToFlowNode(building) is { } node ? new FlowNode
        {
            Id = id,
            Owner = node.Owner,
            Kind = node.Kind,
            In = node.In,
            Out = node.Out,
            MaxRate = node.MaxRate,
            PriceBonus = node.PriceBonus,
            Imports = node.Imports,
        } : null;
    }

    /// <summary>Spots found during one call of the advisor (the same building near the same tile)</summary>
    private readonly Dictionary<(string, TilePos), TilePos?> _spots = new();

    /// <summary>The best free spot on the company's land for a building, near a tile: the windiest, sunniest, richest of the close ones</summary>
    public TilePos? FindSpot(Company company, BuildingDefinition definition, TilePos near)
    {
        if (_spots.TryGetValue((definition.Id, near), out var known))
        {
            return known;
        }
        return _spots[(definition.Id, near)] = SearchSpot(company, definition, near);
    }

    private TilePos? SearchSpot(Company company, BuildingDefinition definition, TilePos near)
    {
        if (EstateOf(company) is not { } estate)
        {
            return null;
        }
        var terrain = definition.Placement.Terrain.Count > 0 ? definition.Placement.Terrain : BuildingPlacement.DefaultTerrain;
        var tiles = new List<TilePos>();
        for (var parcel = 0; parcel < WorldLayout.ParcelCount; parcel++)
        {
            if (estate.IsUnlocked(parcel))
            {
                tiles.AddRange(ParcelTiles(estate, parcel));
            }
        }
        // And the outskirts it bought (1.4 §5)
        for (var parcel = 0; parcel < WorldLayout.OutskirtsParcels(estate.Slot).Count; parcel++)
        {
            if (estate.HasOutskirts(parcel))
            {
                tiles.AddRange(OutskirtsTiles(estate, parcel));
            }
        }
        TilePos? best = null;
        var bestScore = double.MinValue;
        var found = 0;
        // The full placement check only where the corner tile could take it at all
        foreach (var pos in tiles.Where(t => !IsOccupied(t) && Tile(t) is var info && terrain.Contains(info.Terrain)
                     && (definition.Placement.Deposit == null || DepositMatches(definition.Placement.Deposit, info.Deposit)))
                     .OrderBy(t => Distance(t, near)).Take(300))
        {
            // Somewhere a pipe can reach it: a free tile beside it that isn't water or mountain
            if (!CanPlace(company, definition, pos).Success
                || !BorderTiles(pos, definition.Size).Any(t => !IsOccupied(t) && Tile(t).Terrain is not (TerrainType.Mountain or TerrainType.Water)))
            {
                continue;
            }
            var probe = new Building { Type = definition.Id, Owner = company.Id, X = pos.X, Y = pos.Y, Size = definition.Size };
            var score = LocationMultiplier(probe, definition) - Distance(pos, near) * 0.005;
            if (score > bestScore)
            {
                (best, bestScore) = (pos, score);
            }
            // Without wind, sun or richness the closest spot is the best one
            if (definition.Modifier == ProductionModifier.None || ++found >= 40)
            {
                break;
            }
        }
        return best;
    }

    // ---------------------------------------------------------------- The kinds of steps

    private void PumpStep(Company company, Network network, Func<Network, double> baseGold, Action<AdvisorStep, double> add)
    {
        var factor = network.PumpFactorOf(company.Id);
        if (factor >= 0.999 || !Content.Buildings.TryGetValue("pumpStation", out var pump) || !IsUnlocked(company, pump, out _))
        {
            return;
        }
        // Estimated: the company's gold here grows with the speed its buildings get back
        var gain = Estimated(network)
            ? Math.Max(0, network.Solution.GoldPerMinute.GetValueOrDefault(company.Id)) * (1 / Math.Max(0.01, factor) - 1)
            : Math.Max(0, WhatIfGold(network, company.Id, pumps: false) - baseGold(network));
        var at = network.Buildings.Select(t => _buildings[t]).FirstOrDefault(t => t.Owner == company.Id)?.Position;
        add(new AdvisorStep(AdvisorKind.Pump, $"Build {Article(pump.Name)}: your network runs at {factor:P0}",
            $"Every {BuildingsPerPump} buildings in one network need a pump station. Put one on its pipes" + (gain > 0.05 ? $": {Plus(network, gain)}." : "."),
            AdvisorAction.Build, BuildCost(company, pump), gain, BuildingType: pump.Id, At: at), AdvisorFixFactor);
    }

    /// <summary>
    /// A shared goal for a bank of a day's income or more (after run 4: all 30,660 g the new sinks took went into the one
    /// shared door in sight, the Era Wonder's order): it ranks at least as high as research
    /// </summary>
    private void SharedStep(Company company, Action<AdvisorStep, double> add, double income)
    {
        if (company.Gold < 24 * 60 * Math.Max(MinMeasuredIncome, income))
        {
            return;
        }
        var all = BuyAndGiveCost(company);
        if (!WondersGiving || WonderOf(company.District) is not { FullAt: null } wonder || all <= 0)
        {
            return;
        }
        var name = WonderDefinition(wonder).Name;
        var day = Math.Min(all, 24 * 60 * Math.Max(MinMeasuredIncome, income));
        add(new AdvisorStep(AdvisorKind.Shared, $"Give to {name}: its stage {wonder.Stage + 1} wants {Gold(Worth(wonder.Required.ToDictionary(t => t.Key, t => WonderLacks(wonder, t.Key))))} of goods",
            $"Buy and give: a day of your income buys {Gold(day)} of its goods at the market; all of it costs {Gold(all)}. Its last stage brings your town's next Era.",
            AdvisorAction.Wonder, day, 0), 1.2);
    }

    /// <summary>
    /// A role for a friend with gold (after run 4: "I still don't know what I could do that matters next to Cleo", Dev,
    /// with 12M gold, while Ana's Cement Kiln and the Great Dynamo waited for bricks nobody near made): what a friend's
    /// scaffold or the Era Wonder waits for that nobody near makes, and the building that would make it
    /// </summary>
    private void RoleStep(Company company, Action<AdvisorStep, double> add)
    {
        var near = NearPlayers(company);
        bool Made(string good) => near.Any(c => BuildingsOf(c.Id).Any(b => Content.Building(b.Type).Out.ContainsKey(good)));
        var waits = new List<(string Good, string Who, double Units, bool Friend)>();
        foreach (var want in WantsNear(company).Where(t => !t.Makes && !Made(t.Want.Resource)))
        {
            waits.Add((want.Want.Resource, $"{PossessiveFor(want.Owner, company)} {want.Name}", want.Want.Left, true));
        }
        if (WondersGiving && WonderOf(company.District) is { FullAt: null } wonder)
        {
            foreach (var good in wonder.Required.Keys.Where(t => !Made(t)))
            {
                if (WonderLacks(wonder, good) is >= 1 and var left)
                {
                    waits.Add((good, $"the {WonderDefinition(wonder).Name.Replace("The ", "")}", left, false));
                }
            }
        }
        foreach (var group in waits.GroupBy(t => t.Good).OrderByDescending(t => t.Sum(w => w.Units) * BasePrice(t.Key)).Take(1))
        {
            if (ChainFor(company, group.Key) is not { Locked: null } chain || FindSpot(company, chain.Building, EstateOf(company)?.YardGate ?? default) is not { } spot)
            {
                continue;
            }
            var good = GoodsWord(group.Key);
            var waiting = group.Select(t => $"{t.Who} ({Amount(t.Units)})").Distinct().Take(2).ToList();
            var who = $"{Join(waiting)} wait{(waiting.Count == 1 ? "s" : "")}";
            // What it would sell for at base, a minute, as its gain: a maker of a good nobody near makes always has buyers
            var gain = chain.Building.Out.GetValueOrDefault(group.Key) * BasePrice(group.Key);
            // Who waits, as it is: a friend's building, or the Capital's Wonder (a newcomer alone has no friends waiting)
            var whom = group.Any(t => t.Friend) ? "your friends wait for" : $"{group.First().Who} waits for";
            add(new AdvisorStep(AdvisorKind.Chain, $"Make the {good} {whom}: build {Article(chain.Building.Name)}",
                $"Nobody near makes {good}, and {who} for them. {ChainText(chain, group.Key, company)}.",
                AdvisorAction.Build, chain.Cost, gain, BuildingType: chain.Building.Id, At: spot), 2);
        }
    }

    /// <summary>The links of a network that carry all they can, as the network view shows them</summary>
    public List<FlowLink> FullLinks(Network network)
    {
        var byPair = network.Links.ToLookup(t => (Math.Min(t.A, t.B), Math.Max(t.A, t.B)));
        var used = new Dictionary<FlowLink, double>(ReferenceEqualityComparer.Instance);
        foreach (var flow in network.Solution.Flows)
        {
            var link = byPair[(Math.Min(flow.From, flow.To), Math.Max(flow.From, flow.To))]
                .Where(t => t.Allows(flow.Resource))
                .OrderByDescending(t => t.Priority).ThenByDescending(t => t.Efficiency)
                .FirstOrDefault();
            if (link != null)
            {
                used[link] = used.GetValueOrDefault(link) + flow.Sent;
            }
        }
        return used.Where(t => t.Key.Capacity < 1e12 && t.Value >= t.Key.Capacity * 0.999).Select(t => t.Key).ToList();
    }

    /// <summary>
    /// A building whose pipe is full: its pipe door (the next tier or Widen on its full links, then the levels the new pipe
    /// carries), or the research before it. After run 4 the steps never ran dry where they did: when the company makes
    /// the ingots itself, can't make them, or can't afford the research yet, the door still offers the pipe, its goods a
    /// want a friend fills or an order brings
    /// </summary>
    private void PipeStep(Company company, Network network, Action<AdvisorStep, double> add)
    {
        var doors = network.Buildings.Select(Building).OfType<Building>()
            .Where(t => t.Owner == company.Id && IdleReasonOf(t).Cause == IdleCause.PipeFull)
            .Take(3)
            .Select(t => (Building: t, Door: PipeDoorFor(company, t)))
            .Where(t => t.Door != null)
            .ToList();
        foreach (var (building, door) in doors.OrderByDescending(t => t.Door!.Gain).Take(1))
        {
            var name = BuildingName(building);
            var good = IdleReasonOf(building).Resource is { } r ? GoodsWord(r) : "goods";
            var title = $"Your {name}'s pipe is full: it carries all the {good} it can";
            switch (door!.Kind)
            {
                case PipeDoorKind.Locked:
                    // The better pipe needs research the Lab can do now (before the grant)
                    var tier = Content.PipeTier(door.Tier);
                    if (tier.Research != null && Content.Research.TryGetValue(tier.Research, out var tech) && ResearchBlocker(company, tech) == null && ResearchCost(company, tech) <= company.Gold)
                    {
                        add(new AdvisorStep(AdvisorKind.Pipes, title,
                            $"Research {tech.Name}: {Plural(tier.Name, 2)} carry {Amount(tier.Throughput)} a minute{(tech.Grant > 0 ? $" (yours anyway at Company Level {tech.Grant})" : "")}.",
                            AdvisorAction.Research, 0, door.Gain, Research: tech.Id, Network: network.Id, Building: building.Id, At: new TilePos(building.X, building.Y)), 1);
                    }
                    break;
                case PipeDoorKind.Waits:
                    if (door.OrderCost is { } order)
                    {
                        add(new AdvisorStep(AdvisorKind.Pipes, $"{door.Text}",
                            $"{door.Source}. Buy the rest from the market: the pipe is laid at once.",
                            AdvisorAction.Climb, order, door.Gain, Network: network.Id, Building: building.Id, At: new TilePos(building.X, building.Y)), 1.2);
                    }
                    break;
                default:
                    var cost = door.Gold + door.LevelCost;
                    var goods = door.Materials.Count == 0 ? ""
                        : $" + {string.Join(" and ", door.Materials.Select(t => $"{Amount(t.Value)} {GoodsWord(t.Key)}"))}{(door.Short.Count > 0 ? $" ({door.Source})" : "")}";
                    add(new AdvisorStep(AdvisorKind.Pipes, title,
                        $"{door.Text}: {Plus(network, door.Gain)} for {Gold(cost)}{goods}.",
                        AdvisorAction.Climb, cost, door.Gain, Network: network.Id, Building: building.Id, At: new TilePos(building.X, building.Y)), 1.5);
                    break;
            }
        }
    }

    /// <summary>What a company's buildings in a network need of each resource and get: their full need of what holds them back</summary>
    public Dictionary<string, (double Needed, double Got, List<long> Consumers)> NetworkNeeds(Network network, long? owner = null)
    {
        var solution = network.Solution;
        var received = new Dictionary<(long, string), double>();
        var supply = new Dictionary<string, double>();
        // What goes to the hub or into storage was left over: no building could use more of it
        var spare = new HashSet<string>();
        foreach (var flow in solution.Flows)
        {
            received[(flow.To, flow.Resource)] = received.GetValueOrDefault((flow.To, flow.Resource)) + flow.Delivered;
            if (flow.Kind is FlowKind.Sale or FlowKind.ToStorage && flow.Sent > 1e-3)
            {
                spare.Add(flow.Resource);
            }
            if (flow.Kind is FlowKind.Import or FlowKind.FromStorage || (_buildings.TryGetValue(flow.From, out var from) && Content.Building(from.Type).Kind is not (BuildingKind.Storage or BuildingKind.TradingPost)))
            {
                supply[flow.Resource] = supply.GetValueOrDefault(flow.Resource) + flow.Sent;
            }
        }
        var needs = new Dictionary<string, (double Needed, double Got, List<long> Consumers)>();
        foreach (var id in network.Buildings)
        {
            var building = _buildings[id];
            if ((owner != null && building.Owner != owner) || Content.Building(building.Type).Kind is BuildingKind.Storage or BuildingKind.TradingPost
                || ToFlowNode(building) is not { } node || node.In.Count == 0)
            {
                continue;
            }
            // A building whose goods can't leave isn't short of anything: what holds it back is its way out (F2)
            if (IdleReasonOf(building).IsStuck)
            {
                continue;
            }
            // A factory without ore uses no energy either, but it is short of ore, not of energy: when nobody in the
            // network makes one of its inputs, only that one counts as short. And an input that is left over (sold, stored)
            // isn't what holds it back: a factory without energy takes no more ore, so its ore goes to the hub.
            var full = node.In.ToDictionary(t => t.Key, t => t.Value * node.MaxRate * network.PumpFactorOf(building.Owner));
            var got = node.In.ToDictionary(t => t.Key, t => received.GetValueOrDefault((id, t.Key)));
            var missing = full.Keys.Where(t => supply.GetValueOrDefault(t) < 1e-6).ToHashSet();
            var ratios = full.Where(t => t.Value > 1e-9).ToDictionary(t => t.Key, t => got[t.Key] / t.Value);
            // Only inputs that aren't left over can hold it back: when all of them are, its output does (nothing is short)
            var binding = ratios.Where(t => !spare.Contains(t.Key)).ToDictionary();
            var lowest = binding.Count == 0 ? 1 : binding.Values.Min();
            foreach (var (resource, amount) in full)
            {
                var holdsBack = missing.Count > 0 ? missing.Contains(resource) : binding.ContainsKey(resource) && binding[resource] <= lowest + 1e-3;
                var row = needs.TryGetValue(resource, out var r) ? r : (0, 0, new List<long>());
                if (holdsBack && got[resource] < amount * 0.99)
                {
                    row.Consumers.Add(id);
                }
                needs[resource] = (row.Needed + (holdsBack ? amount : got[resource]), row.Got + got[resource], row.Consumers);
            }
        }
        return needs;
    }

    /// <summary>
    /// A building whose goods can't leave (F2): the pipe to its trading post (or a Storage) that lets them out, ranked like
    /// a fix. What it brings: what it makes at the hub's prices, less what its inputs would have sold for.
    /// </summary>
    private void StuckSteps(Company company, Action<AdvisorStep, double> add)
    {
        foreach (var building in BuildingsOf(company.Id).Where(t => !t.UnderConstruction).OrderBy(t => t.Id).ToList())
        {
            if (IdleReasonOf(building) is not { Cause: IdleCause.Stuck, Resource: { } resource } || ToFlowNode(building) is not { } node)
            {
                continue;
            }
            // A generator with no pipe at all is the player's own first pipe to lay (the start step teaches it)
            if (node.In.Count == 0 && PipeReach(building).Count == 0 || PlanFixPipe(building) is not { } pipe)
            {
                continue;
            }
            var hub = (string r) => HubBuyPrice(company, r);
            var gain = Math.Max(0, node.Out.Sum(t => t.Value * node.MaxRate * hub(t.Key)) - node.In.Sum(t => t.Value * node.MaxRate * hub(t.Key)));
            var name = Content.Building(building.Type).Name;
            var target = Content.Building(pipe.Target!.Type);
            add(new AdvisorStep(AdvisorKind.Stuck, $"Unstick your {name}: one pipe, {Gold(pipe.Cost)}",
                $"Its {GoodsWord(resource)} can't leave. A pipe to your {target.Name} {(target.Kind == BuildingKind.TradingPost ? "sells" : "keeps")} them" + (gain > 0.05 ? $": about +{Gold(gain)}/min." : "."),
                AdvisorAction.LayPipe, pipe.Cost, gain, Building: building.Id, At: building.Position, Tiles: pipe.Tiles), AdvisorFixFactor);
        }
    }

    private void ShortageSteps(Company company, Network network, Func<Network, double> baseGold, Action<AdvisorStep, double> add, Func<double, double, double> worth)
    {
        var needs = NetworkNeeds(network, company.Id);
        var shortOf = needs.Where(t => t.Value.Consumers.Count > 0 && t.Value.Needed - t.Value.Got > Math.Max(0.05, t.Value.Needed * 0.05)).ToDictionary(t => t.Key, t => t.Value);
        foreach (var (resource, (needed, got, consumers)) in shortOf.OrderByDescending(t => (t.Value.Needed - t.Value.Got) * HubBuyPrice(Company(network.Owner), network.District, t.Key)))
        {
            // Shortages get half the what-ifs: upgrades and chains need theirs
            if (!WhatIfsLeft(AdvisorMaxWhatIfs / 2))
            {
                break;
            }
            // Below 5% short the buildings run nearly full: not broken. And when what makes it is itself short of
            // something (mines without energy), that is the shortage to fix: it shows on its own
            var short_ = needed - got;
            if (shortOf.Any(t => t.Key != resource && t.Value.Consumers.Any(c => Content.Building(_buildings[c].Type).Out.ContainsKey(resource))))
            {
                continue;
            }
            var consumer = _buildings[consumers[0]];
            var types = consumers.GroupBy(t => _buildings[t].Type).ToList();
            var who = types.Count > 1 ? "buildings" : Plural(Content.Building(consumer.Type).Name, consumers.Count);
            var one = consumers.Count == 1;
            // The ways to make what is missing: new producers (one Wind Moily rather than six Solar Panels), or one that
            // is there upgraded; each measured, the best gold per gold wins. A gap too big for one step gets the best
            // part of it.
            var options = new List<(BuildingDefinition Producer, TilePos Spot, int Copies, Building? Upgrade, int Levels, double Cost, double Covers)>();
            // The three that make it cheapest per unit (before looking for a spot for each)
            foreach (var producer in Content.BuildingList
                         .Where(t => t.Buildable && t.Kind == BuildingKind.Producer && t.Out.ContainsKey(resource) && !t.In.ContainsKey(resource) && IsUnlocked(company, t, out _))
                         .OrderBy(t => BuildCost(company, t) / t.Out[resource])
                         .Take(3))
            {
                if (FindSpot(company, producer, consumer.Position) is not { } spot || HypotheticalNode(company, producer, spot, -1) is not { } node)
                {
                    continue;
                }
                var each = node.Out.GetValueOrDefault(resource);
                if (each <= 1e-9)
                {
                    continue;
                }
                var copies = Math.Min(10, (int)Math.Ceiling(short_ / each - 1e-9));
                options.Add((producer, spot, copies, null, 0, Enumerable.Range(0, copies).Sum(i => Content.Progression.BuildCost(producer, CopiesOwned(company.Id, producer.Id) + i)), copies * each));
            }
            foreach (var building in network.Buildings.Select(t => _buildings[t]).Where(t => t.Owner == company.Id && CanUpgrade(t) && Content.Building(t.Type).Out.ContainsKey(resource)))
            {
                var now = ToFlowNode(building)?.Out.GetValueOrDefault(resource) ?? 0;
                var levels = 0;
                var more = 0.0;
                while (levels < 30 && building.Level + levels < Content.Progression.MaxBuildingLevel && more < short_)
                {
                    levels++;
                    more = (ToFlowNode(building, building.Level + levels)?.Out.GetValueOrDefault(resource) ?? 0) - now;
                }
                if (levels > 0)
                {
                    options.Add((Content.Building(building.Type), building.Position, 0, building, levels, UpgradeCost(building, levels), more));
                }
            }
            var measured = options
                .OrderBy(t => t.Cost / Math.Max(1e-9, Math.Min(t.Covers, short_)))
                .Take(4)
                .Select(option =>
                {
                    double gain;
                    if (Estimated(network))
                    {
                        // What fills the gap is worth what the short buildings make of it, the rest what the hub pays
                        var inputs = option.Upgrade is { } built
                            ? (ToFlowNode(built, built.Level + option.Levels)?.In ?? new Dictionary<string, double>()).ToDictionary(t => t.Key, t => t.Value - (ToFlowNode(built)?.In.GetValueOrDefault(t.Key) ?? 0))
                            : option.Producer.In.ToDictionary(t => t.Key, t => t.Value * option.Copies);
                        var values = ValuesOf(network, company);
                        gain = inputs.Any(t => t.Value > 1e-9 && values.Short.Contains(t.Key))
                            ? 0
                            : Math.Min(option.Covers, short_) * values.Value.GetValueOrDefault(resource)
                              + Math.Max(0, option.Covers - short_) * HubBuyPrice(Company(network.Owner), network.District, resource)
                              - inputs.Sum(t => Math.Max(0, t.Value) * HubBuyPrice(Company(network.Owner), network.District, t.Key));
                    }
                    else if (option.Upgrade is { } upgrade)
                    {
                        var up = ToFlowNode(upgrade, upgrade.Level + option.Levels);
                        gain = WhatIfGold(network, company.Id, t => t.Id == upgrade.Id && up != null ? WithPumps(network, [up])[0] : t) - baseGold(network);
                    }
                    else
                    {
                        var extra = Enumerable.Range(1, option.Copies).Select(i => HypotheticalNode(company, option.Producer, option.Spot, -i)!).ToList();
                        gain = WhatIfGold(network, company.Id, extra: extra, anchor: consumer.Id) - baseGold(network);
                    }
                    return (Option: option, Gain: Math.Max(0, gain));
                })
                .ToList();
            if (measured.Count == 0)
            {
                continue;
            }
            var (best, gain) = measured.OrderByDescending(t => worth(t.Gain, t.Option.Cost)).First();
            var what = best.Upgrade is { } chosen
                ? $"your {best.Producer.Name} upgraded to level {chosen.Level + best.Levels}"
                : (best.Copies == 1 ? Article(best.Producer.Name) : $"{best.Copies} {Plural(best.Producer.Name, best.Copies)}")
                    + (best.Producer.Placement.Deposit != null ? $" on {ResourceWord(best.Producer.Placement.Deposit)}" : "");
            var fixes = best.Covers >= short_ - 1e-6
                ? best.Copies > 1 ? "fix it" : "fixes it"
                : $"{(best.Copies > 1 ? "make" : "makes")} {Amount(best.Covers)} more";
            add(new AdvisorStep(AdvisorKind.Shortage, $"Your {who} {(one ? "is" : "are")} {Amount(short_)} {ResourceWord(resource)} short",
                $"{(one ? "It runs" : "They run")} at {got / needed:P0}: {what} ({Gold(best.Cost)}) {fixes}" + (gain > 0.05 ? $", {Plus(network, gain)}." : "."),
                best.Upgrade != null ? AdvisorAction.Upgrade : AdvisorAction.Build, best.Cost, gain,
                Building: best.Upgrade?.Id, BuildingType: best.Producer.Id, Levels: best.Levels, At: best.Spot, Count: Math.Max(1, best.Copies)), AdvisorFixFactor);
        }
    }

    private void ResearchStep(Company company, Action<AdvisorStep, double> add, double income, IReadOnlyList<Network> networks)
    {
        if (!HasLab(company))
        {
            // No research at all: tracks make a chain faster and leaner, the Era tree opens better pipes, so a Lab comes
            // first (more so with full pipes). It stands beside the trading post.
            var post = BuildingsOf(company.Id).FirstOrDefault(t => Content.Building(t.Type).Kind == BuildingKind.TradingPost);
            var lab = Content.BuildingList.FirstOrDefault(t => t.Buildable && t.Kind == BuildingKind.Lab && IsUnlocked(company, t, out _));
            if (post != null && lab != null && BuildingsOf(company.Id).All(t => Content.Building(t.Type).Kind != BuildingKind.Lab)
                && FindSpot(company, lab, post.Position) is { } spot)
            {
                // Full pipes need research only when they are the best pipe you may lay (a grey one waits for copper,
                // which Level 8 gives: its pipe door is the way, not a Lab)
                var bestPipe = Content.PipeTiers.Where(t => t.Era <= YourEra(company) && (t.Research == null || HasResearch(company, t.Research))).Max(t => t.Throughput);
                var fullPipes = networks.Any(t => FullLinks(t).Any(link => link.Capacity >= bestPipe * 0.999));
                add(new AdvisorStep(AdvisorKind.Research, fullPipes ? $"Build {Article(lab.Name)}: your pipes are full, better ones need research" : $"Build {Article(lab.Name)}: research makes your chains better",
                    $"{Article(lab.Name)} has two lines: a track makes one chain faster and leaner tier by tier, the Era tree opens better pipes.",
                    AdvisorAction.Build, BuildCost(company, lab), Math.Max(1, income * 0.05), BuildingType: lab.Id, At: spot), fullPipes ? 2 : 1);
            }
            return;
        }
        // The second line: the Era tree, cheapest first (copper pipes before anything)
        if (company.CurrentResearch == null && company.ResearchQueue.Count == 0
            && Content.ResearchList.Where(t => ResearchBlocker(company, t) == null).OrderBy(t => ResearchCost(company, t)).FirstOrDefault() is { } tech
            && ResearchCost(company, tech) <= company.Gold)
        {
            add(new AdvisorStep(AdvisorKind.Research, $"Research {tech.Name} on your Lab's second line",
                $"{tech.Description.TrimEnd('.')} · {Minutes(tech.Minutes)}, {Gold(ResearchCost(company, tech))}.",
                AdvisorAction.Research, ResearchCost(company, tech), Math.Max(1, income * 0.02), Research: tech.Id), 1);
        }
        // The first line: the track tier that adds the most gold a minute
        if (company.CurrentTrack != null || company.TrackQueue.Count != 0)
        {
            return;
        }
        var best = Content.TrackList
            .Where(t => TierBlocker(company, t) == null && NextTier(company, t) is not null)
            .Select(t => (Track: t, Tier: NextTier(company, t)!.Value, Gain: TierGain(company, t)))
            .OrderByDescending(t => t.Gain).ThenBy(t => TierCost(company, t.Track, t.Tier).Total)
            .FirstOrDefault();
        if (best.Track == null || TierCost(company, best.Track, best.Tier) is var cost && cost.Total > company.Gold)
        {
            return;
        }
        var effect = Content.TrackRules.Tiers[best.Tier - 1];
        var what = effect.Effect switch
        {
            TierEffect.Faster or TierEffect.Master => $"its buildings work {effect.Amount:0.##}× as fast",
            TierEffect.Leaner => $"its buildings need {1 - effect.Amount:P0} less",
            TierEffect.Complexes => "complexes: four of its buildings in a square make as much as five",
            _ => "mega complexes: nine in a square make as much as twelve",
        };
        add(new AdvisorStep(AdvisorKind.Research, $"Research {best.Track.Name} tier {best.Tier}",
            $"{char.ToUpperInvariant(what[0])}{what[1..]}{(best.Gain > 0.05 ? $" (about +{Gold(best.Gain)}/min)" : "")} · {Minutes(effect.Minutes)}, {Gold(cost.Total)}.",
            AdvisorAction.Research, cost.Total, best.Gain, Research: best.Track.Id), 1);
    }

    /// <summary>About what a track's next tier adds a minute: its buildings' net output at hub prices, by how much more they'd make</summary>
    public double TierGain(Company company, TrackDefinition track)
    {
        if (NextTier(company, track) is not { } tier)
        {
            return 0;
        }
        var gain = 0.0;
        foreach (var building in BuildingsOf(company.Id))
        {
            if (TrackOf(building).Track?.Id != track.Id || NetworkOf(building.Id) is not { } network || ToFlowNode(building) is not { } node)
            {
                continue;
            }
            var rate = network.Solution.Rate(building.Id);
            var made = node.Out.Sum(t => t.Value * HubBuyPrice(company, t.Key)) * rate;
            var used = node.In.Sum(t => t.Value * HubBuyPrice(company, t.Key)) * rate;
            var now = TrackFactors(building);
            var next = TrackFactors(building, tier);
            gain += (made - used) * (next.Faster * next.PowerFree * next.Big / (now.Faster * now.PowerFree * now.Big) - 1)
                + used * next.Faster / now.Faster * (1 - next.Leaner / now.Leaner);
        }
        return Math.Max(0, gain);
    }

    private void UpgradeSteps(Company company, List<Network> networks, Func<Network, double> baseGold, Action<AdvisorStep, double> add)
    {
        var progression = Content.Progression;
        // One of each kind at each level in each network: the same building at the same spot brings the same
        var candidates = networks
            .SelectMany(network => network.Buildings.Select(t => (Network: network, Building: _buildings[t])))
            .Where(t => t.Building.Owner == company.Id && CanUpgrade(t.Building) && Content.Building(t.Building.Type) is { Kind: BuildingKind.Producer or BuildingKind.TradingPost } d && (d.Out.Count > 0 || d.Kind == BuildingKind.TradingPost))
            .GroupBy(t => (t.Network.Id, t.Building.Type, t.Building.Level, Math.Round(LocationMultiplier(t.Building, Content.Building(t.Building.Type)), 2)))
            .Select(t => t.First())
            .OrderByDescending(t => t.Network.Solution.Rate(t.Building.Id))
            .Take(40)
            .ToList();
        // Producers that sell a lot first: their upgrades are worth measuring (a big network solves slowly)
        candidates = candidates
            .OrderByDescending(t => t.Network.Solution.Flows.Where(f => f.From == t.Building.Id).Sum(f => f.Delivered * Math.Max(f.PricePerUnit, HubBuyPrice(Company(t.Network.Owner), t.Network.District, f.Resource))))
            .ToList();
        foreach (var (network, building) in candidates)
        {
            if (!Estimated(network) && !WhatIfsLeft(4))
            {
                continue;
            }
            var definition = Content.Building(building.Type);
            var from = BoughtLevel(building);
            var best = (Levels: 0, Gain: 0.0, Cost: 0.0);
            // One level, or five at once
            foreach (var levels in new[] { 1, Math.Min(5, progression.MaxBuildingLevel - from) }.Where(t => t >= 1).Distinct())
            {
                // Its goods from level 11 count at what they'd cost (1.4 §9): its own at their worth, a neighbour's at their
                // price, the market's at 2.5×
                var cost = UpgradeCost(building, levels) + StepGoodsCost(building, levels);
                var up = ToFlowNode(building, from + levels);
                var gain = definition.Kind == BuildingKind.TradingPost ? PostLevelGain(company, from + levels - building.Level)
                    : Estimated(network)
                    ? EstimateChange(network, company, ToFlowNode(building), up)
                    : WhatIfGold(network, company.Id, t => t.Id == building.Id && up != null ? WithPumps(network, [up])[0] : t) - baseGold(network);
                if (gain > 0.01 && (best.Levels == 0 || gain / cost > best.Gain / best.Cost))
                {
                    best = (levels, gain, cost);
                }
            }
            if (best.Levels == 0)
            {
                continue;
            }
            var target = from + best.Levels;
            // Which one, when there are several (run 2: Cleo read the step for her second Wind Moily as the first's)
            var same = BuildingsOf(company.Id).Where(t => t.Type == building.Type).OrderBy(t => t.Id).ToList();
            var name = same.Count > 1 ? $"your {Ordinal(same.FindIndex(t => t.Id == building.Id) + 1)} {definition.Name}" : definition.Name;
            var payback = best.Cost / best.Gain;
            var why = definition.Kind == BuildingKind.TradingPost ? "Every level raises all your Hub Orders by 10%" : "Every level makes 10% more";
            add(new AdvisorStep(AdvisorKind.Upgrade, $"Upgrade {name} to level {target}",
                $"{why}: {Plus(network, best.Gain)} for {Gold(best.Cost)}, back in {Minutes(payback)}.",
                AdvisorAction.Upgrade, best.Cost, best.Gain, Building: building.Id, BuildingType: building.Type, Levels: best.Levels, At: building.Position), 1);
        }
    }

    private void ChainSteps(Company company, List<Network> networks, Func<Network, double> baseGold, Action<AdvisorStep, double> add)
    {
        if (networks.Count == 0)
        {
            return;
        }
        // What the company sells to the hub, per network: inputs a factory could turn into something worth more
        var spare = networks.ToDictionary(t => t, t => t.Solution.Flows
            .Where(f => f.Kind == FlowKind.Sale && _buildings.TryGetValue(f.From, out var b) && b.Owner == company.Id)
            .GroupBy(f => f.Resource).ToDictionary(g => g.Key, g => g.Sum(f => f.Sent)));
        var made = networks.ToDictionary(t => t, t => t.Solution.Flows
            .Where(f => _buildings.TryGetValue(f.From, out var b) && b.Owner == company.Id && Content.Building(b.Type).Kind == BuildingKind.Producer)
            .Select(f => f.Resource).ToHashSet());
        var main = networks.OrderByDescending(t => t.Buildings.Count(b => _buildings[b].Owner == company.Id && Content.Building(_buildings[b].Type).Kind == BuildingKind.TradingPost))
            .ThenByDescending(t => t.Buildings.Count).First();
        var evaluated = 0;
        // 1.4 §8: every good has its own Hub Order, so the most promising first: what a copy would sell at the market
        // while its goods' Hub Orders have room
        var candidates = Content.BuildingList.Where(t => t.Buildable && t.Kind == BuildingKind.Producer && t.Out.Count > 0 && IsUnlocked(company, t, out _))
            .OrderByDescending(t => t.Out.Sum(o => o.Value * HubOrderValue(company, o.Key, o.Value)) - t.In.Sum(i => i.Value * HubBuyPrice(company, i.Key)))
            .ToList();
        foreach (var definition in candidates)
        {
            if (!WhatIfsLeft() || evaluated >= 8)
            {
                break;
            }
            var owned = CopiesOwned(company.Id, definition.Id);
            var extractor = definition.Placement.Deposit != null;
            Network? network;
            if (extractor)
            {
                // An extractor of something its land holds: the first one, or another while its goods' Hub Orders have room
                network = owned == 0 || definition.Out.Keys.Any(t => HubOrderRoom(company, t) > definition.Out[t]) ? main : null;
            }
            else if (definition.In.Count > 0)
            {
                // A factory: where all its inputs are made and some are left over
                network = networks.FirstOrDefault(n => definition.In.Keys.All(made[n].Contains) && definition.In.Keys.Any(r => spare[n].GetValueOrDefault(r) > definition.In[r] * 0.5));
            }
            else
            {
                network = null;
            }
            if (network == null)
            {
                continue;
            }
            var anchor = network.Buildings.Select(t => _buildings[t]).Where(t => t.Owner == company.Id)
                .OrderByDescending(t => !extractor && Content.Building(t.Type).Out.Keys.Any(definition.In.ContainsKey) ? 1 : 0)
                .ThenByDescending(t => Content.Building(t.Type).Kind == BuildingKind.TradingPost ? 1 : 0)
                .First();
            if (FindSpot(company, definition, anchor.Position) is not { } spot || HypotheticalNode(company, definition, spot, -1) is not { } node)
            {
                continue;
            }
            evaluated++;
            var gain = Estimated(network)
                ? EstimateChange(network, company, null, node)
                : WhatIfGold(network, company.Id, extra: [node], anchor: anchor.Id) - baseGold(network);
            var cost = BuildCost(company, definition);
            if (gain <= 0.05)
            {
                continue;
            }
            var output = definition.Out.Keys.First();
            var title = extractor
                // What it would dig there: a Deep Mine's deposit is any metal (the light bots read "Mine your : build a Deep Mine")
                ? $"Mine your {ResourceWord(node.Out.Keys.FirstOrDefault() ?? definition.Placement.Deposit!)}: build {Article(definition.Name)}"
                : $"{(owned == 0 ? $"Build {Article(definition.Name)}" : $"Another {definition.Name}")}: {string.Join(" and ", definition.In.Keys.Where(r => spare[network].ContainsKey(r)).Select(ResourceWord))} into {ResourceWord(output)}";
            var payback = cost / gain;
            add(new AdvisorStep(AdvisorKind.Chain, title,
                $"{Plus(network, gain)} for {Gold(cost)}, back in {Minutes(payback)}" + (extractor ? "." : ": the hub pays more for it than for what goes in."),
                AdvisorAction.Build, cost, gain, BuildingType: definition.Id, At: spot), 1);
        }
    }

    /// <summary>
    /// The basics: a producer whose output nothing takes (not piped to the trading post), or no producer at all yet. Its
    /// gain is what the output would sell for at the hub.
    /// </summary>
    private void StartSteps(Company company, Action<AdvisorStep, double> add)
    {
        var post = BuildingsOf(company.Id).FirstOrDefault(t => Content.Building(t.Type).Kind == BuildingKind.TradingPost);
        if (post == null)
        {
            return;
        }
        var postNetwork = NetworkOf(post.Id);
        foreach (var building in BuildingsOf(company.Id).Where(t => !t.UnderConstruction && Content.Building(t.Type) is { Kind: BuildingKind.Producer, Out.Count: > 0 }))
        {
            if (postNetwork != null && postNetwork.Buildings.Contains(building.Id))
            {
                continue;
            }
            var definition = Content.Building(building.Type);
            var value = ToFlowNode(building)?.Out.Sum(t => t.Value * HubBuyPrice(company, t.Key)) ?? 0;
            if (definition.In.Count == 0 && value > 0.01)
            {
                add(new AdvisorStep(AdvisorKind.Start, $"Pipe your {definition.Name} to your Trading Post",
                    $"It sells nothing yet: what it makes would bring about +{Gold(value)}/min at the hub. Drag a pipe from it to the Trading Post.",
                    AdvisorAction.Connect, Distance(building.Position, post.Position) * Content.PipeTier(1).Gold, value, Building: building.Id, At: building.Position), AdvisorFixFactor);
                return;
            }
        }
        var first = !BuildingsOf(company.Id).Any(t => Content.Building(t.Type) is { Kind: BuildingKind.Producer, In.Count: 0, Out.Count: > 0 });

        // The generator that earns most for its price on its best spot (nothing makes anything yet: the first one)
        var best = Content.BuildingList
            .Where(t => t.Buildable && t.Kind == BuildingKind.Producer && t.In.Count == 0 && t.Out.Count > 0 && t.Placement.Deposit == null && IsUnlocked(company, t, out _))
            .Select(t => (Definition: t, Spot: FindSpot(company, t, post.Position)))
            .Where(t => t.Spot != null)
            .Select(t => (t.Definition, t.Spot, Value: HypotheticalNode(company, t.Definition, t.Spot!.Value, -1)?.Out.Sum(o => o.Value * HubOrderValue(company, o.Key, o.Value)) ?? 0))
            .OrderByDescending(t => t.Value / Math.Max(1, BuildCost(company, t.Definition)))
            .FirstOrDefault();
        if (best.Definition != null && best.Value > 0.01)
        {
            add(first
                ? new AdvisorStep(AdvisorKind.Start, $"Build {Article(best.Definition.Name)} and pipe it to your Trading Post",
                    $"About +{Gold(best.Value)}/min at the hub, on the best spot near it.",
                    AdvisorAction.Build, BuildCost(company, best.Definition), best.Value, BuildingType: best.Definition.Id, At: best.Spot)
                : new AdvisorStep(AdvisorKind.Chain, $"Build another {best.Definition.Name}",
                    $"About +{Gold(best.Value)}/min at the hub on a good spot, back in {Minutes(BuildCost(company, best.Definition) / best.Value)}; its first levels are cheap.",
                    AdvisorAction.Build, BuildCost(company, best.Definition), best.Value, BuildingType: best.Definition.Id, At: best.Spot),
                first ? AdvisorFixFactor : 1);
        }
    }

    /// <summary>Not piped to the Yard yet while a neighbour lacks what the company sells to the hub: pipe to the Yard gate</summary>
    private void PartnerStep(Company company, Action<AdvisorStep, double> add)
    {
        if (company.Level < 3 || EstateOf(company) is not { } mine || GateNetwork(mine) != null)
        {
            return;
        }
        var spare = new Dictionary<string, double>();
        foreach (var network in _networks.Values.Where(t => t.Owner == company.Id))
        {
            foreach (var flow in network.Solution.Flows.Where(t => t.Kind == FlowKind.Sale))
            {
                spare[flow.Resource] = spare.GetValueOrDefault(flow.Resource) + flow.Sent;
            }
        }
        var lacks = new Dictionary<string, (double Amount, Company Who)>();
        foreach (var resident in YardResidents(mine.Station).Where(t => t.Connected && t.Company.Id != company.Id && TradesWith(company, t.Company) && TradesWith(t.Company, company)))
        {
            foreach (var (resource, amount) in resident.Lacks.Where(t => spare.ContainsKey(t.Key)))
            {
                if (amount > lacks.GetValueOrDefault(resource).Amount)
                {
                    lacks[resource] = (amount, resident.Company);
                }
            }
        }
        var gains = lacks.Select(t => (Resource: t.Key, t.Value.Who, Gain: Math.Min(spare[t.Key], t.Value.Amount) * Math.Max(0, PlayerPrice(t.Key) - HubBuyPrice(company, t.Key))))
            .Where(t => t.Gain > 0)
            .OrderByDescending(t => t.Gain)
            .ToList();
        var gain = gains.Sum(t => t.Gain);
        if (gains.Count == 0 || gain < 1)
        {
            return;
        }
        // A grey pipe from the middle of the start parcels to the gate
        var middle = mine.Origin.Offset(WorldLayout.EstateSize / 2, WorldLayout.EstateSize / 2);
        var cost = Distance(middle, mine.YardGate) * Content.PipeTier(1).Gold;
        var first = gains[0];
        add(new AdvisorStep(AdvisorKind.Partner, $"Pipe to your Yard gate: {first.Who.Name} lacks {ResourceWord(first.Resource)}",
            $"You sell it to the hub; your neighbours pay more: about +{Gold(gain)}/min for a pipe of about {Gold(cost)}.",
            AdvisorAction.Partner, cost, gain, Partner: first.Who.Id, At: mine.YardGate), 1);
    }

    /// <summary>
    /// Interdependence (F6): a factory whose missing input a neighbour has spare, by the Yard gate so it gets it by itself
    /// ("Cleo has 6 iron ingots spare a minute: build a Lab by your Yard gate")
    /// </summary>
    private void NeighbourChainStep(Company company, Action<AdvisorStep, double> add)
    {
        if (company.Level < 3 || EstateOf(company) is not { } estate || GateNetwork(estate) == null)
        {
            return;
        }
        var made = BuildingsOf(company.Id).Where(t => !t.UnderConstruction).SelectMany(t => Content.Building(t.Type).Out.Keys).ToHashSet();
        var spare = new Dictionary<string, (double Amount, Company Who)>();
        foreach (var resident in YardResidents(estate.Station).Where(t => t.Connected && t.Company.Id != company.Id && TradesWith(company, t.Company) && TradesWith(t.Company, company)))
        {
            foreach (var (resource, amount) in resident.Spare.Where(t => t.Value > spare.GetValueOrDefault(t.Key).Amount))
            {
                spare[resource] = (amount, resident.Company);
            }
        }
        if (spare.Count == 0)
        {
            return;
        }
        bool Comes(string good) => spare.ContainsKey(good);
        double Price(string good) => PlayerPrice(good);
        (BuildingDefinition Definition, double Gain, string Good)? best = null;
        foreach (var definition in Content.BuildingList.Where(t => t.Buildable && t.Kind == BuildingKind.Producer && t.Out.Count > 0 && t.In.Keys.Any(k => k != "energy") && IsUnlocked(company, t, out _)))
        {
            var bought = definition.In.Keys.Where(t => !made.Contains(t)).ToList();
            if (bought.Count == 0 || !bought.All(Comes) || CopiesOwned(company.Id, definition.Id) > 0 && bought.Any(t => !spare.ContainsKey(t)))
            {
                continue;
            }
            // As fast as the neighbours' spare lets it run (a House feeds one fully), at the price it pays, the hub's for the rest
            var rate = Math.Min(1, bought.Min(t => spare.TryGetValue(t, out var s) ? s.Amount / definition.In[t] : 1));
            var value = definition.Out.Sum(t => t.Value * BasePrice(t.Key))
                - bought.Sum(t => definition.In[t] * Price(t))
                - definition.In.Where(t => !bought.Contains(t.Key)).Sum(t => t.Value * HubBuyPrice(company, t.Key));
            var gain = rate * value;
            if (rate >= 0.25 && gain > 0.5 && (best == null || gain > best.Value.Gain))
            {
                best = (definition, gain, bought.OrderByDescending(t => spare.ContainsKey(t) ? 1 : 0).ThenByDescending(t => BasePrice(t)).First());
            }
        }
        if (best is not { } chosen)
        {
            return;
        }
        var from = spare[chosen.Good];
        var title = $"{NameFor(from.Who, company)} has {Amount(from.Amount)} {ResourceWord(chosen.Good)} spare a minute: build {Article(chosen.Definition.Name)} by your Yard gate";
        add(new AdvisorStep(AdvisorKind.Chain, title,
            $"Piped to your gate it gets them by itself: about +{Gold(chosen.Gain)}/min.",
            AdvisorAction.Build, BuildCost(company, chosen.Definition), chosen.Gain, BuildingType: chosen.Definition.Id, At: FindSpot(company, chosen.Definition, estate.YardGate)), 1);
    }

    private void ParcelStep(Company company, Action<AdvisorStep, double> add)
    {
        var (cost, level) = NextParcel(company);
        if (level > company.Level || EstateOf(company) is not { } estate || BestParcel(company) is not { } best)
        {
            return;
        }
        var have = LandHoldings(estate);
        var holds = best.Holds.Count == 0 ? "room for more buildings" : string.Join(", ", best.Holds.Take(3).Select(t => t.Label));
        var (ox, oy) = WorldLayout.ParcelOrigin(best.Parcel);
        add(new AdvisorStep(AdvisorKind.Parcel, $"Buy a parcel: {holds}",
            $"More land for {Gold(cost)}" + (best.Holds.Any(t => !have.Contains(t.What)) ? ", with what your land doesn't have yet." : "."),
            AdvisorAction.Parcel, cost, 0, Parcel: best.Parcel, At: estate.Origin.Offset(ox + WorldLayout.ParcelSize / 2, oy + WorldLayout.ParcelSize / 2)),
            1);
    }

    /// <summary>What a plot's own parcels hold now</summary>
    private HashSet<string> LandHoldings(Estate estate)
    {
        var have = new HashSet<string>();
        for (var parcel = 0; parcel < WorldLayout.ParcelCount; parcel++)
        {
            if (estate.IsUnlocked(parcel))
            {
                have.UnionWith(ParcelHoldings(estate, parcel).Select(t => t.What));
            }
        }
        return have;
    }

    /// <summary>The parcel to buy next (the advisor's and Grow's): next to the land, holding the most it doesn't have yet</summary>
    public (int Parcel, IReadOnlyList<(string What, string Label, int Richness)> Holds)? BestParcel(Company company)
    {
        if (EstateOf(company) is not { } estate)
        {
            return null;
        }
        var have = LandHoldings(estate);
        var best = Enumerable.Range(0, WorldLayout.ParcelCount)
            .Where(t => !estate.IsUnlocked(t) && ParcelNeighbours(t).Any(estate.IsUnlocked))
            .Select(t => (Parcel: t, Holds: ParcelHoldings(estate, t)))
            .OrderByDescending(t => t.Holds.Sum(h => (have.Contains(h.What) ? 1 : 3) + h.Richness))
            // Then the one nearest the Yard gate (any order is fine, as long as it touches your land)
            .ThenBy(t => WorldLayout.ParcelOrigin(t.Parcel) is var (px, py) && WorldLayout.YardGateLocal(estate.Slot) is var (gx, gy)
                ? Math.Abs(px + WorldLayout.ParcelSize / 2 - gx) + Math.Abs(py + WorldLayout.ParcelSize / 2 - gy) : 0)
            .ThenBy(t => t.Parcel)
            .FirstOrDefault();
        if (best.Holds == null)
        {
            return null;
        }
        return (best.Parcel, best.Holds);
    }

    /// <summary>
    /// What a parcel holds: its deposits of this Era and before (with their best richness), water, forest; a later
    /// Era's deposits sleep and count for nothing yet
    /// </summary>
    public List<(string What, string Label, int Richness)> ParcelHoldings(Estate estate, int parcel)
    {
        bool Yours(string good) => good is "water" or "wood" || (Content.Resources.TryGetValue(good, out var resource) && resource.Era <= TownEra(estate.District));
        var deposits = new Dictionary<string, int>();
        var water = 0;
        var forest = 0;
        foreach (var tile in ParcelTiles(estate, parcel))
        {
            var info = Tile(tile);
            if (info.Deposit is { } deposit && Yours(deposit))
            {
                deposits[deposit] = Math.Max(deposits.GetValueOrDefault(deposit), info.Richness);
            }
            water += info.Terrain == TerrainType.Water ? 1 : 0;
            forest += info.Terrain == TerrainType.Forest ? 1 : 0;
        }
        var holds = deposits.OrderByDescending(t => t.Value).Select(t => (t.Key, $"{ResourceWord(t.Key)} {t.Value}★", t.Value)).ToList();
        if (water >= 3)
        {
            holds.Add(("water", "a shore", 0));
        }
        if (forest >= 6 && Yours("wood"))
        {
            holds.Add(("forest", "forest", 0));
        }
        return holds;
    }

    private static IEnumerable<int> ParcelNeighbours(int parcel)
    {
        var (px, py) = (parcel % WorldLayout.ParcelsPerSide, parcel / WorldLayout.ParcelsPerSide);
        return new[] { (px - 1, py), (px + 1, py), (px, py - 1), (px, py + 1) }
            .Where(t => t.Item1 is >= 0 and < WorldLayout.ParcelsPerSide && t.Item2 is >= 0 and < WorldLayout.ParcelsPerSide)
            .Select(t => t.Item2 * WorldLayout.ParcelsPerSide + t.Item1);
    }

    // ---------------------------------------------------------------- Words

    private string ResourceWord(string resource) => ResourceName(resource).ToLowerInvariant();

    private static string Plural(string name, int count) =>
        count == 1 ? name : name.EndsWith('y') && !name.EndsWith("ey", StringComparison.Ordinal) ? $"{name[..^1]}ies" : name.EndsWith('s') ? name : $"{name}s";

    public static string Article(string name) => ("aeiou".Contains(char.ToLowerInvariant(name[0])) ? "an " : "a ") + name;

    private static string Amount(double value) => value >= 100 ? $"{value:N0}" : value >= 10 ? $"{value:0}" : $"{value:0.#}";

    /// <summary>"first", "second", … "10th"</summary>
    private static string Ordinal(int n) => n switch
    {
        1 => "first", 2 => "second", 3 => "third", 4 => "fourth", 5 => "fifth",
        _ => n % 10 == 1 && n % 100 != 11 ? $"{n}st" : n % 10 == 2 && n % 100 != 12 ? $"{n}nd" : n % 10 == 3 && n % 100 != 13 ? $"{n}rd" : $"{n}th",
    };

    private static string Gold(double value) =>
        value >= 1e9 ? $"{value / 1e9:0.#}B g" : value >= 1e6 ? $"{value / 1e6:0.#}M g" : value >= 10_000 ? $"{value / 1000:0.#}k g" : $"{Amount(value)} g";

    private static string Minutes(double minutes) =>
        minutes < 1 ? "under a minute" : minutes < 90 ? $"{minutes:0} min" : minutes < 48 * 60 ? $"{minutes / 60:0.#} h" : $"{minutes / 1440:0} days";
}
