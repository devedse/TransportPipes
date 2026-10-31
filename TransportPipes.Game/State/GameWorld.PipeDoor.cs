using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>What the pipe door does: lay the next tier, wait for its goods (buy the rest), or nothing yet</summary>
public enum PipeDoorKind
{
    Tier,
    Waits,
    Locked,
}

/// <summary>
/// The way up a full pipe, as one button with one preview
/// </summary>
/// <param name="Text">What the button does, in words: "Copper Pipe on its 2 links (34 tiles), then Level 30"</param>
/// <param name="Tiles">The pipe tiles it lays</param>
/// <param name="Tier">The tier it lays</param>
/// <param name="Gold">The pipe's gold</param>
/// <param name="Materials">What the pipe takes (34 copper ingots), and of it what the Storages lack</param>
/// <param name="ToLevel">The level the new pipe carries that the gold reaches (0: no levels with it)</param>
/// <param name="LevelCost">Those levels' gold</param>
/// <param name="Gain">Gold a minute after both, solved like the network will run</param>
/// <param name="Source">Where the goods it lacks come from: a friend, or the market ("Cleo makes copper ingots …")</param>
/// <param name="OrderTarget">A waiting pipe want's target for Buy the rest (minus its number)</param>
/// <param name="OrderCost">What buying the rest of the goods costs now</param>
public sealed record PipeDoor(PipeDoorKind Kind, string Text, List<TilePos> Tiles, int Tier, double Gold, Dictionary<string, double> Materials, Dictionary<string, double> Short,
    int ToLevel, double LevelCost, double Gain, string? Source, long? OrderTarget = null, double? OrderCost = null, double Goods = 0);

/// <summary>
/// The pipe door (after run 4: the pipe tier is the game's real level cap. The hub pays at least a quarter of the base for
/// anything, so a level pays back within hours wherever the pipe carries it, but the upgrade panel said "+0 g/min: its
/// pipes carry 205 iron ore a minute" with no button, the next tier showed only in a stuck building's status, and all
/// 1,087 pipe tiles stayed grey for 36 days). On the panel of a building whose pipe is full, one button goes the whole
/// way (1.4 §9): the next tier on its full links, then the levels the new pipe carries and its inputs feed, as far as
/// the gold goes. Goods the pipe takes that the Storages lack become a want that friends fill through the Yard, or the
/// market when nobody near makes them.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>
    /// The door of a building whose output its pipe holds back (null: its pipe isn't what stops it); preview: false skips
    /// solving its gain (a line that only names it)
    /// </summary>
    public PipeDoor? PipeDoorFor(Company company, Building building, bool preview = true, bool exact = true)
    {
        if (building.Owner != company.Id || building.UnderConstruction || IdleReasonOf(building) is not { Cause: IdleCause.PipeFull, Resource: { } good }
            || NetworkOf(building.Id) is not { } network || NetworkNode(network, building) is not { Kind: FlowNodeKind.Building } node)
        {
            return null;
        }
        var full = FullLinks(building, good);
        var pipes = FullLinkPipes(building, good);
        if (pipes.Count == 0)
        {
            return null;
        }
        var tiles = pipes.Select(t => t.Pos).ToHashSet();
        string Links(int count) => count == 1 ? "its link" : $"its {count} links";

        // A pipe of it already waits for its goods: order the rest, or say who brings them
        if (company.Asks.FirstOrDefault(t => t.PipeTiles != null && t.PipeTiles.Any(tiles.Contains) && t.Left > 1e-6) is { } waiting)
        {
            var parts = company.Asks.Where(t => t.PipeTiles != null && t.WantId == waiting.WantId).ToList();
            var lacks = parts.Where(t => t.Left > 1e-6).ToDictionary(t => t.Resource, t => t.Left);
            var name = Content.PipeTier(waiting.PipeTier).Name;
            var cost = OrderCost(company, -waiting.WantId);
            return new PipeDoor(PipeDoorKind.Waits, $"Its {name} waits for {string.Join(" and ", lacks.Select(t => $"{Amount(t.Value)} {GoodsWord(t.Key)}"))}",
                waiting.PipeTiles!, waiting.PipeTier, 0, lacks, lacks, 0, 0, 0, MaterialSource(company, lacks.Keys), -waiting.WantId, cost);
        }

        // The next tier on its full links
        var lowest = pipes.Min(t => t.Tier);
        var current = Content.PipeTier(lowest);
        var next = Content.PipeTiers.Where(t => t.Tier > lowest).OrderBy(t => t.Tier).FirstOrDefault();
        if (next == null)
        {
            return null;
        }
        if (next.Era > YourEra(company) || (next.Research != null && !HasResearch(company, next.Research)))
        {
            var tech = next.Research != null ? Content.Research.GetValueOrDefault(next.Research) : null;
            var when = next.Era > YourEra(company) ? $"with Era {next.Era}"
                : tech is { Grant: > 0 } ? $"at Company Level {tech.Grant} (you're {company.Level})"
                : tech != null ? $"with the research {tech.Name}" : "later";
            return new PipeDoor(PipeDoorKind.Locked, $"{Plural(next.Name, 2)} carry {Amount(next.Throughput)} a minute: yours {when}",
                [], next.Tier, 0, new(), new(), 0, 0, 0, null);
        }
        var upgrade = pipes.Where(t => t.Tier < next.Tier).ToList();
        var materials = next.Materials.ToDictionary(t => t.Key, t => t.Value * upgrade.Count);
        var have = Materials(company.Id);
        var shortOf = materials.Where(t => have.GetValueOrDefault(t.Key) < t.Value - 1e-9).ToDictionary(t => t.Key, t => t.Value - have.GetValueOrDefault(t.Key));
        var pipeGold = next.Gold * upgrade.Count;
        var tierFactor = next.Throughput / Math.Max(1e-9, current.Throughput);
        // The levels come with the pipe: when its goods must come first, the door lays the want and the levels wait
        var (to, toCost) = shortOf.Count == 0 ? DoorLevels(company, building, network, node, full, tierFactor, company.Gold - pipeGold) : (building.Level, 0.0);
        var tierGain = !preview ? 0
            : exact ? DoorGain(company, building, network, upgrade.Select(t => t.Pos), next.Tier, shortOf.Count == 0 ? to : building.Level)
            : DoorEstimate(building, network, full, tierFactor, shortOf.Count == 0 ? to : building.Level);
        var text = $"{next.Name} on {Links(full.Count)} ({Tiles(upgrade.Count)}){(to > building.Level ? $", then Level {to}" : "")}";
        return new PipeDoor(PipeDoorKind.Tier, text, upgrade.Select(t => t.Pos).ToList(), next.Tier, pipeGold, materials, shortOf,
            to > building.Level ? to : 0, toCost, tierGain, shortOf.Count == 0 ? null : MaterialSource(company, shortOf.Keys),
            Goods: to > BoughtLevel(building) ? StepGoodsCost(building, to - BoughtLevel(building)) : 0);
    }

    /// <summary>
    /// The highest level a building would run at least 90% at with its full links carrying a factor more (Max's rule),
    /// as far as some gold goes, and its price
    /// </summary>
    private (int Level, double Cost) DoorLevels(Company company, Building building, Network network, FlowNode node, List<FlowLink> full, double factor, double gold)
    {
        var set = full.ToHashSet(ReferenceEqualityComparer.Instance);
        var reach = LevelReach(network, node, t => set.Contains(t) ? t with { Capacity = t.Capacity * factor } : t);
        var levels = 0;
        var cost = 0.0;
        while (building.Level + levels < Content.Progression.MaxBuildingLevel)
        {
            var step = UpgradeCost(building, levels + 1) - cost;
            var share = reach.Share(Content.Progression.OutputMultiplier(building.Level + levels + 1) / Content.Progression.OutputMultiplier(building.Level));
            if (cost + step > gold + 1e-6 || share < UsefulShare)
            {
                break;
            }
            cost += step;
            levels++;
        }
        return (building.Level + levels, Math.Round(cost));
    }

    /// <summary>
    /// What the company would make a minute more with these pipe tiles a better tier and the building at a level: its
    /// network solved with the new pipe laid (its throughput and its loss) at the price the market settles on
    /// (GameWorld.WhatIf; it scaled the full links' capacity, estimated on big networks)
    /// </summary>
    private readonly Dictionary<(long Building, int Tier, int Level), ((int, long, long) Stamp, double Gain)> _doorGains = new();

    private double DoorGain(Company company, Building building, Network network, IEnumerable<TilePos> tiles, int tier, int level)
    {
        // Once for as long as its network stays as it is (a building's panel asks for it at every refresh)
        var stamp = StampOf(network);
        if (_doorGains.TryGetValue((building.Id, tier, level), out var known) && known.Stamp == stamp)
        {
            return known.Gain;
        }
        var gain = SolvedDoorGain(company, building, network, tiles, tier, level);
        if (_doorGains.Count > 5_000)
        {
            _doorGains.Clear();
        }
        _doorGains[(building.Id, tier, level)] = (stamp, gain);
        return gain;
    }

    private double SolvedDoorGain(Company company, Building building, Network network, IEnumerable<TilePos> tiles, int tier, int level)
    {
        var up = level > building.Level ? ToFlowNode(building, level) : null;
        var tiers = tiles.Distinct().ToDictionary(t => t, _ => tier);
        var solution = SolveWhatIf(network, change: t => up != null && t.Id == building.Id ? WithPumps(network, [up])[0] : t, pipeTiers: tiers);
        return Math.Max(0, SettledGain(company, new Dictionary<int, FlowSolution> { [network.Id] = solution }));
    }

    /// <summary>
    /// A quick estimate of a door on a big network, to rank it (Grow's candidates): what the full links carry now, times
    /// what they would carry more (half of it found a taker), but no more than the building could make beyond what its
    /// pipes carry now at its new level
    /// </summary>
    private double DoorEstimate(Building building, Network network, List<FlowLink> full, double factor, int level)
    {
        var room = 0.0;
        if (network.Index.Nodes.GetValueOrDefault(building.Id) is { } node)
        {
            var scale = Content.Progression.OutputMultiplier(level) / Content.Progression.OutputMultiplier(building.Level);
            foreach (var (resource, perRun) in node.Out)
            {
                var carried = network.Index.FlowsFrom(building.Id).Where(t => t.Resource == resource).Sum(t => t.Sent);
                room += Math.Max(0, perRun * node.MaxRate * scale - carried) * BasePrice(resource);
            }
        }
        return Math.Min(FullLinkValue(network, full) * Math.Min(factor - 1, 3) * 0.5, room);
    }

    /// <summary>The links a building's good leaves by that carry all they can</summary>
    private List<FlowLink> FullLinks(Building building, string resource) =>
        NetworkOf(building.Id) is not { } network ? []
            : network.Links.Where(t => (t.A == building.Id || t.B == building.Id) && t.Allows(resource)
                && network.Solution.Flows.Where(f => f.From == building.Id && f.Resource == resource && t.Connects(f.From, f.To)).Sum(f => f.Sent) >= t.Capacity * 0.98).ToList();

    /// <summary>The owner's pipe tiles on the full links a building's good leaves by</summary>
    private List<Conductor> FullLinkPipes(Building building, string resource) =>
        FullLinks(building, resource).SelectMany(t => t.Path).Distinct()
            .Select(t => _conductors.TryGetValue(t, out var c) ? c : null).OfType<Conductor>()
            .Where(t => t.Kind == ConductorKind.Pipe && t.Owner == building.Owner).ToList();

    /// <summary>"Cleo makes copper ingots: the Yard brings them", "nobody near makes copper ingots: buy them now"</summary>
    private string MaterialSource(Company company, IEnumerable<string> goods)
    {
        var parts = new List<string>();
        foreach (var good in goods)
        {
            var maker = NearPlayers(company).FirstOrDefault(c => c.Id != company.Id && BuildingsOf(c.Id).Any(b => !b.UnderConstruction && Content.Building(b.Type).Out.ContainsKey(good)));
            parts.Add(maker != null ? $"{NameFor(maker, company)} makes {GoodsWord(good)}: the Yard brings them" : $"nobody near makes {GoodsWord(good)}: buy them now");
        }
        return string.Join("; ", parts);
    }

    /// <summary>
    /// Goes through a building's pipe door: lays the next tier (or its want, when the Storages lack its goods, bought from
    /// the market when asked or when nobody near makes them), then buys the levels the new pipe carries
    /// </summary>
    public CommandResult ClimbPipe(long companyId, long buildingId, bool order = false)
    {
        if (Company(companyId) is not { } company || Building(buildingId) is not { } building || building.Owner != companyId)
        {
            return CommandResult.Fail("That is not your building.");
        }
        if (PipeDoorFor(company, building) is not { } door)
        {
            return CommandResult.Fail("Its pipe isn't what holds it back now.");
        }
        AddStat(company, "pressed:climb", 1);
        switch (door.Kind)
        {
            case PipeDoorKind.Locked:
                return CommandResult.Fail(door.Text + ".");
            case PipeDoorKind.Waits:
                return door.OrderTarget is { } target ? OrderRest(companyId, target) : CommandResult.Fail("Its goods are on their way.");
            default:
            {
                var laid = UpgradePipes(companyId, door.Tiles, door.Tier);
                if (!laid.Success)
                {
                    return laid;
                }
                if (door.Short.Count > 0)
                {
                    // Its goods come first: from a friend through the Yard, or now when nobody near makes them (or when asked)
                    var name = Content.PipeTier(door.Tier).Name;
                    var goods = string.Join(" and ", door.Short.Select(t => $"{Amount(t.Value)} {GoodsWord(t.Key)}"));
                    var want = company.Asks.Where(t => t.PipeTiles != null && t.PipeTiles.Any(door.Tiles.Contains)).Select(t => t.WantId).DefaultIfEmpty(0).Max();
                    if (want > 0 && (order || door.Source?.StartsWith("nobody", StringComparison.Ordinal) == true) && OrderCost(company, -want) is { } cost && cost <= company.Gold
                        && OrderRest(companyId, -want).Success)
                    {
                        return CommandResult.Done($"Your {name} is laid: its {goods} bought from the market");
                    }
                    return CommandResult.Done($"Your {name} waits for {goods}: {door.Source}");
                }
                break;
            }
        }
        var done = $"{Content.PipeTier(door.Tier).Name} laid";
        if (door.ToLevel > building.Level && Upgrade(companyId, buildingId, door.ToLevel - building.Level).Success)
        {
            return CommandResult.Done($"{done}, and your {Content.Building(building.Type).Name} is Level {door.ToLevel}: it carries more now");
        }
        return CommandResult.Done($"{done}: it carries more now");
    }
}
