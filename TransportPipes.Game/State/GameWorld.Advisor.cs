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

    /// <summary>Upgrades the pipe Tiles to Tier (Show me selects Network)</summary>
    Pipes,

    /// <summary>The pipe tool, at Building</summary>
    Connect,
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

    /// <summary>Fixing what is broken (a shortage, too few pumps) ranks this many times higher than an investment</summary>
    private const double AdvisorFixFactor = 3;

    /// <summary>Idle labs with points waste them: research ranks at least this high</summary>
    private const double AdvisorResearchFloor = 60;

    public List<AdvisorStep> NextSteps(Company company, int count = 3)
    {
        // Right after a command its networks may not be solved yet: an empty solution would make everything look short
        SolveNetworks();
        _spots.Clear();
        _whatIfs = 0;
        var income = Math.Max(0, GoldPerMinute().GetValueOrDefault(company.Id));
        var effort = Math.Max(200, AdvisorEffortMinutes * income);
        // Gold per minute gained per gold spent (and effort), scaled to a readable number
        double Worth(double gain, double cost) => 1000 * gain / (cost + effort);
        var own = Networks.Where(t => t.Buildings.Any(b => _buildings[b].Owner == company.Id)).ToList();
        var baseGold = new Dictionary<int, double>();
        double Base(Network network) => baseGold.TryGetValue(network.Id, out var v) ? v : baseGold[network.Id] = WhatIfGold(network, company.Id);
        _values.Clear();

        var candidates = new List<(AdvisorStep Step, double Score)>();
        void Add(AdvisorStep step, double factor)
        {
            var score = step.Kind switch
            {
                AdvisorKind.Research => Math.Max(AdvisorResearchFloor, Worth(step.GainPerMinute, 0)),
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

        foreach (var network in own)
        {
            PumpStep(company, network, Base, Add);
            PipeStep(company, network, Base, Add);
            ShortageSteps(company, network, Base, Add, Worth);
        }
        ResearchStep(company, Add, income, own);
        UpgradeSteps(company, own, Base, Add);
        ChainSteps(company, own, Base, Add);
        PartnerStep(company, Add);
        ParcelStep(company, Add);
        StartSteps(company, Add);

        // The best of each kind first, then the next best of any kind
        var ranked = candidates.OrderByDescending(t => t.Score).ToList();
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
        var post = network.Buildings.Select(t => _buildings[t]).Any(t => t.Owner == company.Id && Content.Building(t.Type).Kind == BuildingKind.TradingPost && !TradingPostFull(t));
        var value = new Dictionary<string, double>();
        foreach (var resource in network.Buildings.SelectMany(t => Content.Building(_buildings[t].Type).Out.Keys.Concat(Content.Building(_buildings[t].Type).In.Keys)).Distinct())
        {
            // What it sells for here, or would at the hub
            var sales = flows.Where(t => t.Kind == FlowKind.Sale && t.Resource == resource && t.Delivered > 0).ToList();
            var price = sales.Count > 0 ? sales.Sum(t => t.Delivered * t.PricePerUnit) / sales.Sum(t => t.Delivered) : post ? HubBuyPrice(network.District, resource) : 0;
            if (shortOf.Contains(resource))
            {
                // Short: worth what the buildings that wait for it make of a unit
                var consumer = network.Buildings.Select(t => _buildings[t]).Where(t => t.Owner == company.Id).Select(t => Content.Building(t.Type)).FirstOrDefault(t => t.In.ContainsKey(resource));
                if (consumer != null)
                {
                    price = Math.Max(price, consumer.Out.Sum(t => t.Value * HubBuyPrice(network.District, t.Key)) / consumer.In[resource]);
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
            // A full trading post sells more: about half its extra throughput finds something to sell
            var sold = network.Solution.Flows.Where(t => t.To == old.Id && t.Kind == FlowKind.Sale).ToList();
            var price = sold.Count > 0 ? sold.Sum(t => t.Delivered * t.PricePerUnit) / Math.Max(1e-9, sold.Sum(t => t.Delivered)) : 0;
            return TradingPostFull(_buildings[old.Id]) ? (changed.Throughput - old.Throughput) * price * 0.5 : 0;
        }
        var gain = 0.0;
        foreach (var (resource, amount) in changed.In)
        {
            var more = amount - (old?.In.GetValueOrDefault(resource) ?? 0);
            if (more > 1e-9)
            {
                if (values.Short.Contains(resource))
                {
                    return 0;
                }
                gain -= more * HubBuyPrice(network.District, resource);
            }
        }
        foreach (var (resource, amount) in changed.Out)
        {
            gain += (amount - (old?.Out.GetValueOrDefault(resource) ?? 0)) * values.Value.GetValueOrDefault(resource);
        }
        if (old != null && values.OnFullLinks.Contains(old.Id))
        {
            gain *= 0.3;
        }
        return Math.Max(0, gain);
    }

    /// <summary>What flows over the full links of a network is worth a minute</summary>
    private double FullLinkValue(Network network, IEnumerable<FlowLink> full)
    {
        var pairs = full.Select(t => (Math.Min(t.A, t.B), Math.Max(t.A, t.B))).ToHashSet();
        return network.Solution.Flows
            .Where(t => pairs.Contains((Math.Min(t.From, t.To), Math.Max(t.From, t.To))))
            .Sum(t => t.Delivered * (t.PricePerUnit > 0 ? t.PricePerUnit : HubBuyPrice(network.District, t.Resource)));
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
            ResearchPerMinute = node.ResearchPerMinute,
            Throughput = node.Throughput,
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
        TilePos? best = null;
        var bestScore = double.MinValue;
        var found = 0;
        // The full placement check only where the corner tile could take it at all
        foreach (var pos in tiles.Where(t => !IsOccupied(t) && Tile(t) is var info && terrain.Contains(info.Terrain)
                     && (definition.Placement.Deposit == null || DepositMatches(definition.Placement.Deposit, info.Deposit)))
                     .OrderBy(t => Distance(t, near)).Take(300))
        {
            if (!CanPlace(company, definition, pos).Success)
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

    private void PipeStep(Company company, Network network, Func<Network, double> baseGold, Action<AdvisorStep, double> add)
    {
        var full = FullLinks(network);
        if (full.Count == 0)
        {
            return;
        }
        // The weakest of the company's own pipes on each full link: upgrading them raises what it carries
        var weakest = new HashSet<TilePos>();
        var tier = int.MaxValue;
        foreach (var link in full)
        {
            var own = link.Path.Select(t => _conductors.TryGetValue(t, out var c) ? c : null)
                .OfType<Conductor>().Where(t => t.Kind == ConductorKind.Pipe && t.Owner == company.Id).ToList();
            if (own.Count == 0)
            {
                continue;
            }
            var lowest = own.Min(t => t.Tier);
            tier = Math.Min(tier, lowest);
            weakest.UnionWith(own.Where(t => t.Tier == lowest).Select(t => t.Pos));
        }
        if (weakest.Count == 0 || Content.PipeTiers.Where(t => t.Tier > tier && t.Era <= Era).OrderBy(t => t.Tier).FirstOrDefault() is not { } next)
        {
            return;
        }
        var current = Content.PipeTier(tier);
        var tiles = weakest.Where(t => _conductors[t].Tier == tier).ToList();
        var factor = next.Throughput / Math.Max(1e-9, current.Throughput);
        var fullSet = full.ToHashSet(ReferenceEqualityComparer.Instance);
        var gain = Estimated(network)
            ? FullLinkValue(network, full) * Math.Min(factor - 1, 1) * 0.5
            : Math.Max(0, WhatIfGold(network, company.Id, changeLink: t => fullSet.Contains(t) ? t with { Capacity = t.Capacity * factor } : t) - baseGold(network));
        if (gain < 0.05)
        {
            return;
        }
        var at = tiles[0];
        var title = $"Your pipes are full: {tiles.Count} {current.Name} tiles carry all they can";
        if (next.Research != null && !HasResearch(company, next.Research))
        {
            // The better pipe needs research first
            if (Content.Research.TryGetValue(next.Research, out var tech) && ResearchBlocker(company, tech) == null && ResearchCost(company, tech) <= company.ResearchPoints)
            {
                add(new AdvisorStep(AdvisorKind.Pipes, title,
                    $"Research {tech.Name}: {Plural(next.Name, 2)} carry {Amount(next.Throughput)} a minute instead of {Amount(current.Throughput)}, {Plus(network, gain)} here.",
                    AdvisorAction.Research, 0, gain, Research: tech.Id, Network: network.Id, At: at), 1);
            }
            return;
        }
        var cost = tiles.Count * next.Gold;
        var materials = next.Materials.ToDictionary(t => t.Key, t => t.Value * tiles.Count);
        var have = Materials(company.Id);
        var missing = materials.Where(t => have.GetValueOrDefault(t.Key) < t.Value - 1e-9).ToList();
        if (missing.Count > 0)
        {
            // The pipes need something the company doesn't have (copper ingots): the next building on the way to it
            var (resource, amount) = missing[0];
            var why = $"{Plural(next.Name, 2)} carry {Amount(next.Throughput)} a minute instead of {Amount(current.Throughput)} ({Plus(network, gain)} here) and need {Amount(amount)} {ResourceWord(resource)}";
            if (SupplyStep(company, resource, $"{next.Name}s need {ResourceWord(resource)}", why, gain / 2, at) is { } supply)
            {
                add(supply, 1);
            }
            return;
        }
        add(new AdvisorStep(AdvisorKind.Pipes, title,
            $"{Plural(next.Name, 2)} carry {Amount(next.Throughput)} a minute instead of {Amount(current.Throughput)}: {Plus(network, gain)} for {Gold(cost)}.",
            AdvisorAction.Pipes, cost, gain, Network: network.Id, Tiles: tiles, Tier: next.Tier, At: at), 1.5);
    }

    /// <summary>
    /// The first building a company lacks to make a resource it needs (a Smelter for copper ingots, or before it a
    /// Copper Mine for the ore), or null when it already makes it or can't
    /// </summary>
    private AdvisorStep? SupplyStep(Company company, string resource, string title, string why, double gain, TilePos near, int depth = 0)
    {
        var makers = Content.BuildingList.Where(t => t.Buildable && t.Kind == BuildingKind.Producer && t.Out.ContainsKey(resource) && IsUnlocked(company, t, out _)).ToList();
        if (depth == 0 && !BuildingsOf(company.Id).Any(t => Content.Building(t.Type).Kind == BuildingKind.Storage)
            && Content.BuildingList.FirstOrDefault(t => t.Buildable && t.Kind == BuildingKind.Storage && IsUnlocked(company, t, out _)) is { } storage
            && FindSpot(company, storage, near) is { } storageSpot)
        {
            // Materials come out of storages: without one there is nowhere to keep them
            return new AdvisorStep(AdvisorKind.Pipes, $"{title}: build {Article(storage.Name)}",
                $"{why}. Materials come from your storages: {Article(storage.Name)} keeps what you make of it.",
                AdvisorAction.Build, BuildCost(company, storage), gain, BuildingType: storage.Id, At: storageSpot);
        }
        // Something makes it already: the storages keep it as it comes
        if (depth > 2 || makers.Count == 0 || IsMaking(company, resource))
        {
            return null;
        }
        var made = BuildingsOf(company.Id).Where(t => IsMaking(company, t)).SelectMany(t => Content.Building(t.Type).Out.Keys).ToHashSet();
        // A maker the company has that stands idle: what it lacks
        foreach (var idle in makers.Where(t => CopiesOwned(company.Id, t.Id) > 0))
        {
            if (idle.In.Keys.FirstOrDefault(r => !made.Contains(r) && r != resource) is { } lacking
                && SupplyStep(company, lacking, title, why, gain, near, depth + 1) is { } deeper)
            {
                return deeper;
            }
        }
        if (makers.Any(t => CopiesOwned(company.Id, t.Id) > 0))
        {
            return null;
        }
        // A maker whose inputs the company already makes first; else what its missing input needs
        foreach (var maker in makers.OrderBy(t => t.In.Keys.Count(r => !made.Contains(r))).ThenBy(t => t.Cost.Gold))
        {
            if (maker.In.Keys.FirstOrDefault(r => !made.Contains(r)) is { } input)
            {
                if (SupplyStep(company, input, title, why, gain, near, depth + 1) is { } deeper)
                {
                    return deeper;
                }
                continue;
            }
            if (FindSpot(company, maker, near) is not { } spot)
            {
                continue;
            }
            var where = maker.Placement.Deposit != null ? $" on your {ResourceWord(maker.Placement.Deposit)}" : "";
            return new AdvisorStep(AdvisorKind.Pipes, $"{title}: build {Article(maker.Name)}{where}",
                $"{why}. {(depth == 0 ? $"{Article(maker.Name)} makes it" : $"{Article(maker.Name)} is the first step to it")}" + (maker.In.Count > 0 ? $" from {string.Join(" and ", maker.In.Keys.Select(ResourceWord))}." : "."),
                AdvisorAction.Build, BuildCost(company, maker), gain, BuildingType: maker.Id, At: spot);
        }
        return null;
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
            // A factory without ore uses no energy either, but it is short of ore, not of energy: when nobody in the
            // network makes one of its inputs, only that one counts as short. And an input that is left over (sold, stored)
            // isn't what holds it back: a factory without energy takes no more ore, so its ore goes to the hub.
            var full = node.In.ToDictionary(t => t.Key, t => t.Value * node.MaxRate * network.PumpFactorOf(building.Owner));
            var got = node.In.ToDictionary(t => t.Key, t => received.GetValueOrDefault((id, t.Key)));
            var missing = full.Keys.Where(t => supply.GetValueOrDefault(t) < 1e-6).ToHashSet();
            var ratios = full.Where(t => t.Value > 1e-9).ToDictionary(t => t.Key, t => got[t.Key] / t.Value);
            var binding = ratios.Keys.Any(t => !spare.Contains(t)) ? ratios.Where(t => !spare.Contains(t.Key)).ToDictionary() : ratios;
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

    private void ShortageSteps(Company company, Network network, Func<Network, double> baseGold, Action<AdvisorStep, double> add, Func<double, double, double> worth)
    {
        var needs = NetworkNeeds(network, company.Id);
        var shortOf = needs.Where(t => t.Value.Consumers.Count > 0 && t.Value.Needed - t.Value.Got > Math.Max(0.05, t.Value.Needed * 0.05)).ToDictionary(t => t.Key, t => t.Value);
        foreach (var (resource, (needed, got, consumers)) in shortOf.OrderByDescending(t => (t.Value.Needed - t.Value.Got) * HubBuyPrice(network.District, t.Key)))
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
                              + Math.Max(0, option.Covers - short_) * HubBuyPrice(network.District, resource)
                              - inputs.Sum(t => Math.Max(0, t.Value) * HubBuyPrice(network.District, t.Key));
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
        if (!BuildingsOf(company.Id).Any(t => Content.Building(t.Type).ResearchPerMinute > 0))
        {
            // No research at all: techs add output and unlock better pipes, so a Lab comes first (more so with full pipes)
            var post = BuildingsOf(company.Id).FirstOrDefault(t => Content.Building(t.Type).Kind == BuildingKind.TradingPost);
            if (post != null && Content.BuildingList.FirstOrDefault(t => t.Buildable && t.ResearchPerMinute > 0 && IsUnlocked(company, t, out _)) is { } lab
                && FindSpot(company, lab, post.Position) is { } spot)
            {
                var fullPipes = networks.Any(t => FullLinks(t).Count > 0);
                add(new AdvisorStep(AdvisorKind.Research, fullPipes ? $"Build {Article(lab.Name)}: your pipes are full, better ones need research" : $"Build {Article(lab.Name)}: research makes everything better",
                    $"Techs add output to your buildings and unlock better pipes. {Article(lab.Name)} turns {string.Join(" and ", lab.In.Keys.Select(ResourceWord))} into research points.",
                    AdvisorAction.Build, BuildCost(company, lab), Math.Max(1, income * 0.05), BuildingType: lab.Id, At: spot), fullPipes ? 2 : 1);
            }
            return;
        }
        if (company.CurrentResearch != null)
        {
            return;
        }
        // What each startable tech would add to what the company's buildings make now, in gold
        var made = new List<(Building Building, BuildingDefinition Definition, string Resource, double Value)>();
        foreach (var building in BuildingsOf(company.Id))
        {
            if (NetworkOf(building.Id) is not { } network || ToFlowNode(building) is not { } node)
            {
                continue;
            }
            var rate = network.Solution.Rate(building.Id);
            foreach (var (resource, amount) in node.Out)
            {
                made.Add((building, Content.Building(building.Type), resource, rate * amount * HubBuyPrice(network.District, resource)));
            }
        }
        var best = Content.ResearchList
            .Where(t => ResearchBlocker(company, t) == null && ResearchCost(company, t) <= company.ResearchPoints)
            .Select(t => (Tech: t, Helps: made.Where(m => t.OutputBonuses.Any(b => b.Applies(m.Definition, m.Resource))).ToList()))
            .Select(t => (t.Tech, t.Helps, Gain: t.Helps.Sum(m => m.Value * t.Tech.OutputBonuses.Where(b => b.Applies(m.Definition, m.Resource)).Sum(b => b.Amount))))
            .OrderByDescending(t => t.Gain).ThenBy(t => ResearchCost(company, t.Tech))
            .FirstOrDefault();
        if (best.Tech == null)
        {
            return;
        }
        var bonus = best.Tech.OutputBonuses.FirstOrDefault();
        var helped = best.Helps.Select(t => t.Building.Id).Distinct().Count();
        var what = bonus != null && helped > 0
            ? $"+{bonus.Amount:P0} for {helped} of your buildings" + (best.Gain > 0.05 ? $" (+{Gold(best.Gain)}/min)" : "")
            : best.Tech.Description.TrimEnd('.');
        add(new AdvisorStep(AdvisorKind.Research, $"Pick a research: {best.Tech.Name}",
            $"{what} · you have {Amount(company.ResearchPoints)} points and your labs are idle.",
            AdvisorAction.Research, 0, best.Gain, Research: best.Tech.Id), 1);
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
            .OrderByDescending(t => TradingPostFull(t.Building) ? 1 : 0)
            .ThenByDescending(t => t.Network.Solution.Rate(t.Building.Id))
            .Take(40)
            .ToList();
        // Producers that sell a lot first: their upgrades are worth measuring (a big network solves slowly)
        candidates = candidates.OrderByDescending(t => TradingPostFull(t.Building) ? 1 : 0)
            .ThenByDescending(t => t.Network.Solution.Flows.Where(f => f.From == t.Building.Id).Sum(f => f.Delivered * Math.Max(f.PricePerUnit, HubBuyPrice(t.Network.District, f.Resource))))
            .ToList();
        foreach (var (network, building) in candidates)
        {
            if (!Estimated(network) && !WhatIfsLeft(4))
            {
                continue;
            }
            var definition = Content.Building(building.Type);
            var full = TradingPostFull(building);
            // One level, or straight to the next milestone when it is close (it doubles the output)
            var milestone = progression.Milestones.Where(t => t > building.Level && t <= building.Level + 15 && t <= progression.MaxBuildingLevel).DefaultIfEmpty(0).Min();
            var best = (Levels: 0, Gain: 0.0, Cost: 0.0);
            foreach (var levels in milestone > 0 ? new[] { 1, milestone - building.Level } : [1])
            {
                var cost = UpgradeCost(building, levels);
                var up = ToFlowNode(building, building.Level + levels);
                var gain = Estimated(network)
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
            var target = building.Level + best.Levels;
            var name = definition.Name;
            var payback = best.Cost / best.Gain;
            var why = progression.Milestones.Contains(target) ? $"Level {target} doubles it" : "Every level makes 10% more";
            add(new AdvisorStep(full ? AdvisorKind.Full : AdvisorKind.Upgrade,
                full ? $"Your {name} sells all it can" : $"Upgrade {name} to level {target}",
                full ? $"Upgrade it to level {target}: {Plus(network, best.Gain)} for {Gold(best.Cost)}, back in {Minutes(payback)}."
                    : $"{why}: {Plus(network, best.Gain)} for {Gold(best.Cost)}, back in {Minutes(payback)}.",
                AdvisorAction.Upgrade, best.Cost, best.Gain, Building: building.Id, BuildingType: building.Type, Levels: best.Levels, At: building.Position),
                full ? 1.5 : 1);
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
        foreach (var definition in Content.BuildingList.Where(t => t.Buildable && t.Kind == BuildingKind.Producer && t.Out.Count > 0 && IsUnlocked(company, t, out _)))
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
                // An extractor of something its land holds that it doesn't mine yet
                network = owned == 0 ? main : null;
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
                ? $"Mine your {ResourceWord(definition.Placement.Deposit!)}: build {Article(definition.Name)}"
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
            var value = ToFlowNode(building)?.Out.Sum(t => t.Value * HubBuyPrice(company.District, t.Key)) ?? 0;
            if (definition.In.Count == 0 && value > 0.01)
            {
                add(new AdvisorStep(AdvisorKind.Start, $"Pipe your {definition.Name} to your Trading Post",
                    $"It sells nothing yet: what it makes would bring about +{Gold(value)}/min at the hub. Drag a pipe from it to the Trading Post.",
                    AdvisorAction.Connect, Distance(building.Position, post.Position) * Content.PipeTier(1).Gold, value, Building: building.Id, At: building.Position), AdvisorFixFactor);
                return;
            }
        }
        if (BuildingsOf(company.Id).Any(t => Content.Building(t.Type) is { Kind: BuildingKind.Producer, In.Count: 0, Out.Count: > 0 }))
        {
            return;
        }
        // Nothing makes anything yet: the generator that earns most for its price on its best spot
        var best = Content.BuildingList
            .Where(t => t.Buildable && t.Kind == BuildingKind.Producer && t.In.Count == 0 && t.Out.Count > 0 && t.Placement.Deposit == null && IsUnlocked(company, t, out _))
            .Select(t => (Definition: t, Spot: FindSpot(company, t, post.Position)))
            .Where(t => t.Spot != null)
            .Select(t => (t.Definition, t.Spot, Value: HypotheticalNode(company, t.Definition, t.Spot!.Value, -1)?.Out.Sum(o => o.Value * HubBuyPrice(company.District, o.Key)) ?? 0))
            .OrderByDescending(t => t.Value / Math.Max(1, BuildCost(company, t.Definition)))
            .FirstOrDefault();
        if (best.Definition != null && best.Value > 0.01)
        {
            add(new AdvisorStep(AdvisorKind.Start, $"Build {Article(best.Definition.Name)} and pipe it to your Trading Post",
                $"About +{Gold(best.Value)}/min at the hub, on the best spot near it.",
                AdvisorAction.Build, BuildCost(company, best.Definition), best.Value, BuildingType: best.Definition.Id, At: best.Spot), AdvisorFixFactor);
        }
    }

    private void PartnerStep(Company company, Action<AdvisorStep, double> add)
    {
        if (company.Level < 3 || EstateOf(company) is not { } mine)
        {
            return;
        }
        var neighbourhood = NeighbourhoodOf(company);
        var spare = neighbourhood.Surplus.GetValueOrDefault(company.Id)?.Where(t => t.Value > 0.01).ToDictionary(t => t.Key, t => t.Value) ?? [];
        if (spare.Count == 0)
        {
            return;
        }
        var hq = mine.Origin.Offset(HqLocal.X, HqLocal.Y);
        var loss = Content.PipeTier(1).LossPerTile;
        var best = (Company: (Company?)null, Gain: 0.0, Resources: new List<string>(), Tiles: 0);
        foreach (var (other, shortfalls) in neighbourhood.Shortfalls)
        {
            if (other == company.Id || neighbourhood.Connected.Contains(other) || Company(other) is not { } partner || EstateOf(partner) is not { } estate
                || !TradesWith(company, partner) || !TradesWith(partner, company))
            {
                continue;
            }
            var tiles = Distance(hq, estate.Origin.Offset(HqLocal.X, HqLocal.Y));
            var arrives = Math.Pow(1 - loss, tiles);
            foreach (var shortfall in shortfalls.Where(t => t.Keys.All(spare.ContainsKey)))
            {
                var gain = shortfall.Sum(t => Math.Min(spare[t.Key], t.Value) * Math.Max(0, SellPrice(company, t.Key) * arrives - HubBuyPrice(company.District, t.Key)));
                if (gain > best.Gain)
                {
                    best = (partner, gain, shortfall.Keys.ToList(), tiles);
                }
            }
        }
        if (best.Company is not { } chosen || best.Gain < 1)
        {
            return;
        }
        // About a grey pipe from HQ to HQ
        var cost = best.Tiles * Content.PipeTier(1).Gold;
        add(new AdvisorStep(AdvisorKind.Partner, $"Pipe to {chosen.Name}: they need {string.Join(" and ", best.Resources.Select(ResourceWord))}",
            $"You have it spare and they pay more than the hub: about +{Gold(best.Gain)}/min for a pipe of about {Gold(cost)}.",
            AdvisorAction.Partner, cost, best.Gain, Partner: chosen.Id), 1);
    }

    private void ParcelStep(Company company, Action<AdvisorStep, double> add)
    {
        var (cost, level) = NextParcel(company);
        if (level > company.Level || EstateOf(company) is not { } estate)
        {
            return;
        }
        // The parcel next to the company's land that holds the most it doesn't have yet
        var have = new HashSet<string>();
        for (var parcel = 0; parcel < WorldLayout.ParcelCount; parcel++)
        {
            if (estate.IsUnlocked(parcel))
            {
                have.UnionWith(ParcelHoldings(estate, parcel).Select(t => t.What));
            }
        }
        var best = Enumerable.Range(0, WorldLayout.ParcelCount)
            .Where(t => !estate.IsUnlocked(t) && ParcelNeighbours(t).Any(estate.IsUnlocked))
            .Select(t => (Parcel: t, Holds: ParcelHoldings(estate, t)))
            .OrderByDescending(t => t.Holds.Sum(h => (have.Contains(h.What) ? 1 : 3) + h.Richness))
            .ThenBy(t => Content.Progression.ParcelOrder.ToList().IndexOf(t.Parcel) is var i and >= 0 ? i : 99)
            .FirstOrDefault();
        if (best.Holds == null)
        {
            return;
        }
        var holds = best.Holds.Count == 0 ? "room for more buildings" : string.Join(", ", best.Holds.Take(3).Select(t => t.Label));
        var (ox, oy) = WorldLayout.ParcelOrigin(best.Parcel);
        add(new AdvisorStep(AdvisorKind.Parcel, $"Buy a parcel: {holds}",
            $"More land for {Gold(cost)}" + (best.Holds.Any(t => !have.Contains(t.What)) ? ", with what your land doesn't have yet." : "."),
            AdvisorAction.Parcel, cost, 0, Parcel: best.Parcel, At: estate.Origin.Offset(ox + WorldLayout.ParcelSize / 2, oy + WorldLayout.ParcelSize / 2)),
            1);
    }

    /// <summary>What a parcel holds: its deposits (with their best richness), water, forest</summary>
    public List<(string What, string Label, int Richness)> ParcelHoldings(Estate estate, int parcel)
    {
        var deposits = new Dictionary<string, int>();
        var water = 0;
        var forest = 0;
        foreach (var tile in ParcelTiles(estate, parcel))
        {
            var info = Tile(tile);
            if (info.Deposit is { } deposit)
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
        if (forest >= 6)
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

    private static string Article(string name) => ("aeiou".Contains(char.ToLowerInvariant(name[0])) ? "an " : "a ") + name;

    private static string Amount(double value) => value >= 100 ? $"{value:N0}" : value >= 10 ? $"{value:0}" : $"{value:0.#}";

    private static string Gold(double value) => value >= 10_000 ? $"{value / 1000:0.#}k g" : $"{Amount(value)} g";

    private static string Minutes(double minutes) =>
        minutes < 1 ? "under a minute" : minutes < 90 ? $"{minutes:0} min" : minutes < 48 * 60 ? $"{minutes / 60:0.#} h" : $"{minutes / 1440:0} days";
}
