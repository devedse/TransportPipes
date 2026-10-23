using System.Runtime.CompilerServices;
using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;

namespace TransportPipes.Game.State;

/// <summary>Why a building doesn't run (fully), the most important reason first</summary>
public enum IdleCause
{
    /// <summary>It runs as fast as it can</summary>
    None,

    /// <summary>Still under construction</summary>
    Building,

    /// <summary>What it makes has nowhere to go: no own Storage or trading post, no building that uses it and no Yard gate on its pipes</summary>
    Stuck,

    /// <summary>Its only takers are stuck themselves (Other: the first of them)</summary>
    TakerStuck,

    /// <summary>Its only takers don't run now, for a reason of their own (Other: the one that runs least)</summary>
    TakerIdle,

    /// <summary>Nothing on its pipes supplies an input (Resource)</summary>
    InputMissing,

    /// <summary>Storm damage: half speed until it is repaired</summary>
    Damaged,

    /// <summary>Too few pump stations in its network</summary>
    Pumps,

    /// <summary>An input (Resource) comes, but not enough</summary>
    InputShort,

    /// <summary>Its trading post sells all it can</summary>
    PostFull,

    /// <summary>The Storage it fills is full (for a Storage: it is full)</summary>
    Full,

    /// <summary>Its only way out is the Yard gate, and nobody in the station takes it now</summary>
    GateOnly,

    /// <summary>Its only takers run fully and use less than it makes (Other: the first of them); the rest can't pass through them</summary>
    TakersFull,

    /// <summary>What it makes (Resource) fills the pipe connections it leaves by, and they carry less than it could make</summary>
    PipeFull,

    /// <summary>A Lab with nothing left to research: it rests and eats nothing</summary>
    Resting,
}

/// <summary>Why a building doesn't run (fully): the cause, the good it is about, and the building it is about</summary>
public sealed record IdleReason(IdleCause Cause, string? Resource = null, long? Other = null)
{
    public static readonly IdleReason Running = new(IdleCause.None);

    /// <summary>Whether what it makes can't leave at all (it and the buildings that feed only it sit still)</summary>
    public bool IsStuck => Cause is IdleCause.Stuck or IdleCause.TakerStuck;
}

/// <summary>
/// One truth per building (F2): why it doesn't run, for the panel, the return card, the Next steps and Loady alike.
/// Whether it is stuck is decided from its pipes and the buildings on them, never from this round's flows, so it can't
/// flicker; the rest reads the network's last solve. Cached per solve.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>Under this share of its speed a building counts as slowed</summary>
    private const double SlowedShare = 0.99;

    private readonly ConditionalWeakTable<FlowSolution, Dictionary<long, IdleReason>> _idleCache = new();

    /// <summary>Why a building doesn't run (fully) now</summary>
    public IdleReason IdleReasonOf(Building building)
    {
        var network = NetworkOf(building.Id);
        var cache = network == null ? null : _idleCache.GetOrCreateValue(network.Solution);
        if (cache != null && cache.TryGetValue(building.Id, out var known))
        {
            return known;
        }
        var reason = Reason(building, network, new HashSet<long>());
        if (cache != null)
        {
            cache[building.Id] = reason;
        }
        return reason;
    }

    private IdleReason Reason(Building building, Network? network, HashSet<long> visited)
    {
        var definition = Content.Building(building.Type);
        if (building.UnderConstruction)
        {
            return new IdleReason(IdleCause.Building);
        }
        if (definition.Kind == BuildingKind.Storage)
        {
            return StorageCapacity(building, definition) - building.StoredTotal <= StorageEpsilon ? new IdleReason(IdleCause.Full) : IdleReason.Running;
        }
        if (definition.Kind == BuildingKind.TradingPost)
        {
            return network != null && TradingPostFull(building) ? new IdleReason(IdleCause.PostFull) : IdleReason.Running;
        }
        if (LabResting(building))
        {
            return new IdleReason(IdleCause.Resting);
        }
        if (ToFlowNode(building) is not { Kind: FlowNodeKind.Building } node || (node.In.Count == 0 && node.Out.Count == 0))
        {
            return IdleReason.Running;
        }
        visited.Add(building.Id);

        // From its pipes: is there a way out for everything it makes, and a way in for everything it needs?
        if (Outlets(building, node, network, visited) is { } blocked)
        {
            return blocked;
        }
        if (network == null)
        {
            return new IdleReason(IdleCause.InputMissing, node.In.Keys.Order(StringComparer.Ordinal).First());
        }
        foreach (var resource in node.In.Keys.Order(StringComparer.Ordinal))
        {
            if (!HasSupplier(building, network, resource))
            {
                return new IdleReason(IdleCause.InputMissing, resource);
            }
        }
        if (building.Damaged)
        {
            return new IdleReason(IdleCause.Damaged);
        }
        if (network.PumpFactorOf(building.Owner) < 1 - 1e-9)
        {
            return new IdleReason(IdleCause.Pumps);
        }

        // From the last solve: how fast it runs, and what holds it back
        var rate = network.Solution.Rate(building.Id);
        var maxRate = network.Nodes.FirstOrDefault(t => t.Id == building.Id)?.MaxRate ?? 1;
        if (rate >= maxRate * SlowedShare)
        {
            return IdleReason.Running;
        }
        var received = new Dictionary<string, double>();
        foreach (var flow in network.Solution.Flows.Where(t => t.To == building.Id))
        {
            received[flow.Resource] = received.GetValueOrDefault(flow.Resource) + flow.Delivered;
        }
        foreach (var flow in network.GateFlows.Where(t => t.Import && t.Building == building.Id))
        {
            received[flow.Resource] = received.GetValueOrDefault(flow.Resource) + flow.Delivered;
        }
        // Its pipes can't carry more away: then that's its limit, whatever its inputs (it gets only what it can use)
        if (PipeCapped(building, node, network, maxRate) is { } capped)
        {
            return new IdleReason(IdleCause.PipeFull, capped);
        }
        // Of inputs equally short, the one whose suppliers stand still: a factory with no ore from a mine that has no
        // energy is short of ore, though no energy comes either (Cleo's run 2 afternoon read "energy short")
        var scarcest = node.In
            .Select(t => (Resource: t.Key, Share: received.GetValueOrDefault(t.Key) / Math.Max(1e-12, t.Value * maxRate)))
            .OrderBy(t => Math.Round(t.Share, 6)).ThenBy(t => SuppliersStandStill(building, network, t.Resource) ? 0 : 1).ThenBy(t => t.Resource, StringComparer.Ordinal)
            .FirstOrDefault();
        if (scarcest.Resource != null && scarcest.Share < SlowedShare && scarcest.Share <= rate / Math.Max(1e-12, maxRate) + 1e-6)
        {
            return new IdleReason(IdleCause.InputShort, scarcest.Resource);
        }
        if (node.Out.Count > 0)
        {
            var linked = LinkedBuildings(building, network);
            if (linked.Any(t => t.Owner == building.Owner && Content.Building(t.Type).Kind == BuildingKind.TradingPost && TradingPostFull(t)))
            {
                return new IdleReason(IdleCause.PostFull);
            }
            if (linked.Any(t => t.Owner == building.Owner && Content.Building(t.Type).Kind == BuildingKind.Storage && StorageCapacity(t, Content.Building(t.Type)) - t.StoredTotal <= StorageEpsilon))
            {
                return new IdleReason(IdleCause.Full);
            }
            if (OnlyOutletIsTheGate(building, node, network))
            {
                return new IdleReason(IdleCause.GateOnly, node.Out.Keys.First());
            }
            // Its goods can only go to buildings that don't run now (a factory with no ore, say)
            var takers = linked.Where(t => ToFlowNode(t) is { Kind: FlowNodeKind.Building } n && node.Out.Keys.Any(n.In.ContainsKey)).ToList();
            var sinks = linked.Any(t => t.Owner == building.Owner && Content.Building(t.Type).Kind is BuildingKind.Storage or BuildingKind.TradingPost)
                || network.GateLinks.Any(t => t.Building == building.Id);
            if (!sinks && takers.Count > 0)
            {
                var least = takers.OrderBy(t => network.Solution.Rate(t.Id)).ThenBy(t => t.Id).First();
                // A taker that runs fully isn't idle: it just can't use more (Ana's factory set into her windmills' pipe, run 2)
                var full = network.Solution.Rate(least.Id) >= (network.Nodes.FirstOrDefault(t => t.Id == least.Id)?.MaxRate ?? 1) * SlowedShare;
                return new IdleReason(full ? IdleCause.TakersFull : IdleCause.TakerIdle, node.Out.Keys.First(), full ? takers.OrderBy(t => t.Id).First().Id : least.Id);
            }
        }
        return IdleReason.Running;
    }

    /// <summary>
    /// The good a building makes more of than its pipes can carry away: every connection that carries it is full, and
    /// together they carry less than it could make; null when none (run 3: Cleo's level-25 Coal Pit could make 882 coal a
    /// minute, its grey pipe to the trading post carried 100, and its card blamed energy for eleven days, because the
    /// solver gave it only the energy it could use)
    /// </summary>
    private static string? PipeCapped(Building building, FlowNode node, Network network, double maxRate)
    {
        foreach (var (resource, perRun) in node.Out.OrderBy(t => t.Key, StringComparer.Ordinal))
        {
            // What its Yard gate takes is what the station matched: like a taker that has all it needs
            var gate = network.GateFlows.Where(t => !t.Import && t.Building == building.Id && t.Resource == resource).Sum(t => t.Sent);
            var used = network.Links
                .Where(t => (t.A == building.Id || t.B == building.Id) && t.Allows(resource))
                .Select(link => (link.Capacity, Sent: network.Solution.Flows.Where(f => f.From == building.Id && f.Resource == resource && link.Connects(f.From, f.To)).Sum(f => f.Sent)))
                .Where(t => t.Sent > 1e-9)
                .Select(t => (t.Capacity, t.Sent, Full: t.Sent >= t.Capacity * 0.98))
                .ToList();
            // A connection with room takes all its taker wants of a building that has plenty spare: it doesn't hide the
            // full one beside it (step 9's e2e: a Moily that fed its own Coal Pit 6.6 energy a minute ran at 57% with no
            // word, its pipe to the post full)
            if (used.Any(t => t.Full) && used.Sum(t => t.Sent) + gate < perRun * maxRate * SlowedShare)
            {
                return resource;
            }
        }
        return null;
    }

    /// <summary>What a building could make of a good at full speed, and what its pipes carry away (the PipeFull line)</summary>
    public (double Could, double Carried) PipeCarry(Building building, string resource)
    {
        var network = NetworkOf(building.Id);
        var node = network?.Nodes.FirstOrDefault(t => t.Id == building.Id);
        var carried = network?.Solution.Flows.Where(t => t.From == building.Id && t.Resource == resource).Sum(t => t.Sent) ?? 0;
        return ((node?.Out.GetValueOrDefault(resource) ?? 0) * (node?.MaxRate ?? 1), carried);
    }

    /// <summary>Whether every building on its pipes that makes a resource stands still (and the Yard brings none of it)</summary>
    private bool SuppliersStandStill(Building building, Network network, string resource) =>
        !network.GateFlows.Any(t => t.Import && t.Building == building.Id && t.Resource == resource && t.Delivered > 1e-9)
        && LinkedBuildings(building, network)
            .Where(t => ToFlowNode(t) is { Kind: FlowNodeKind.Building } n && n.Out.ContainsKey(resource))
            .All(t => network.Solution.Rate(t.Id) < 1e-6);

    /// <summary>The buildings a building's pipes reach directly (the reach line of its panel)</summary>
    public List<Building> PipeReach(Building building) => NetworkOf(building.Id) is { } network ? LinkedBuildings(building, network) : [];

    /// <summary>The buildings a building's pipes reach directly (its links in the network), finished ones only</summary>
    private List<Building> LinkedBuildings(Building building, Network network) =>
        network.Links
            .Where(t => t.A == building.Id || t.B == building.Id)
            .Select(t => Building(t.A == building.Id ? t.B : t.A))
            .OfType<Building>()
            .Where(t => !t.UnderConstruction)
            .DistinctBy(t => t.Id)
            .ToList();

    /// <summary>
    /// Whether everything a building makes has a way out on its pipes: an own Storage or trading post, a Yard gate, or a
    /// building that uses it (and isn't stuck itself). Null when it has; else why not.
    /// </summary>
    private IdleReason? Outlets(Building building, FlowNode node, Network? network, HashSet<long> visited)
    {
        if (node.Out.Count == 0)
        {
            return null;
        }
        if (network == null)
        {
            return new IdleReason(IdleCause.Stuck, node.Out.Keys.First());
        }
        var links = network.Links.Where(t => t.A == building.Id || t.B == building.Id).ToList();
        foreach (var resource in node.Out.Keys.Order(StringComparer.Ordinal))
        {
            var takers = new List<Building>();
            var sink = false;
            foreach (var link in links.Where(t => t.Allows(resource)))
            {
                if (Building(link.A == building.Id ? link.B : link.A) is not { UnderConstruction: false } other)
                {
                    continue;
                }
                var kind = Content.Building(other.Type).Kind;
                if (other.Owner == building.Owner && kind is BuildingKind.Storage or BuildingKind.TradingPost)
                {
                    sink = true;
                    break;
                }
                if (ToFlowNode(other) is { Kind: FlowNodeKind.Building } taker && taker.In.ContainsKey(resource))
                {
                    takers.Add(other);
                }
            }
            if (sink || network.GateLinks.Any(t => t.Building == building.Id && (t.Resources == null || t.Resources.Contains(resource))))
            {
                continue;
            }
            if (takers.Count == 0)
            {
                return new IdleReason(IdleCause.Stuck, resource);
            }
            // Its takers: one that isn't stuck itself is a way out (a taker already being looked at counts as one: a loop)
            if (takers.Any(t => visited.Contains(t.Id)) || takers.Any(t => !Reason(t, network, visited).IsStuck))
            {
                continue;
            }
            return new IdleReason(IdleCause.TakerStuck, resource, takers.OrderBy(t => t.Id).First().Id);
        }
        return null;
    }

    /// <summary>Whether something on a building's pipes can supply a good: a building that makes it, an own Storage holding it, an own trading post buying it, or the Yard gate</summary>
    private bool HasSupplier(Building building, Network network, string resource)
    {
        foreach (var link in network.Links.Where(t => (t.A == building.Id || t.B == building.Id) && t.Allows(resource)))
        {
            if (Building(link.A == building.Id ? link.B : link.A) is not { UnderConstruction: false } other)
            {
                continue;
            }
            var mine = other.Owner == building.Owner;
            var kind = Content.Building(other.Type).Kind;
            if (ToFlowNode(other) is { } n && n.Out.ContainsKey(resource)
                || (mine && kind == BuildingKind.Storage && other.Contents.ContainsKey(resource))
                || (mine && kind == BuildingKind.TradingPost && other.Imports.Contains(resource)))
            {
                return true;
            }
        }
        return network.GateLinks.Any(t => t.Building == building.Id && (t.Resources == null || t.Resources.Contains(resource)));
    }

    /// <summary>Whether a building's only way out (for something it makes) is the Yard gate</summary>
    private bool OnlyOutletIsTheGate(Building building, FlowNode node, Network network) =>
        network.GateLinks.Any(t => t.Building == building.Id)
        && !LinkedBuildings(building, network).Any(other =>
            (other.Owner == building.Owner && Content.Building(other.Type).Kind is BuildingKind.Storage or BuildingKind.TradingPost)
            || (ToFlowNode(other) is { Kind: FlowNodeKind.Building } taker && node.Out.Keys.Any(taker.In.ContainsKey)));
}
