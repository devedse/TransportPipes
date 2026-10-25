using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>What the one fix button of a building's panel does (F2)</summary>
public enum IdleFixKind
{
    /// <summary>Shows Pipe as a ghost to lay with one click</summary>
    LayPipe,

    /// <summary>Opens Building (the stuck taker)</summary>
    Open,

    /// <summary>Repairs the storm damage</summary>
    Repair,

    /// <summary>Upgrades Building (a full trading post) by a level</summary>
    Upgrade,

    /// <summary>The build tool with BuildingType</summary>
    Build,

    /// <summary>
    /// Goes through Building's pipe door: the next tier on its full links (its goods a want, or ordered), or Widen at a
    /// Copper or Steel Pipe's limit, then the levels the new pipe carries (after run 4: one door, on the panel)
    /// </summary>
    Climb,
}

/// <summary>The one fix button of a building's panel: what it does, its label, and what it needs; OwnNeed: a pipe to your own short building</summary>
public sealed record IdleFix(IdleFixKind Kind, string Label, PipePlan? Pipe = null, long? Building = null, string? BuildingType = null, double Cost = 0, bool OwnNeed = false,
    IReadOnlyList<TilePos>? Tiles = null, int Tier = 0);

/// <summary>A building's panel head (F2): the status pill (the cause in five words), the reach line, and the one fix</summary>
public sealed record IdleStatus(IdleReason Reason, string? Pill, string? Reach, IdleFix? Fix);

public sealed partial class GameWorld
{
    /// <summary>What a good is called where it can't leave or is missing: "iron ingots", "sand"</summary>
    public string GoodsWord(string resource)
    {
        var name = Content.Resources.TryGetValue(resource, out var r) ? r.Name.ToLowerInvariant() : resource;
        return name.EndsWith("ingot", StringComparison.Ordinal) ? name + "s" : name;
    }

    private string BuildingName(Building building) => Content.Building(building.Type).Name;

    private static string Tiles(int count) => count == 1 ? "1 tile" : $"{count} tiles";

    /// <summary>"Iron Mine, Wind Moily": the kinds of buildings a building's pipes reach</summary>
    private string ReachNames(Building building) =>
        string.Join(", ", PipeReach(building).Select(BuildingName).Distinct());

    /// <summary>The short line for a building that doesn't run (the map bubble, the goal tracker, the return card): "Stuck: iron ingots can't leave"</summary>
    public string? IdleLine(Building building) => IdleStatusOf(building, plan: false).Pill?.TrimStart('⛔', ' ');

    /// <summary>A building's status pill, reach line and fix (plan: false skips planning the fix's pipe)</summary>
    public IdleStatus IdleStatusOf(Building building, bool plan = true)
    {
        var reason = IdleReasonOf(building);
        var definition = Content.Building(building.Type);
        var good = reason.Resource is { } resource ? GoodsWord(resource) : "";
        string Reach(string prefix) => PipeReach(building).Count == 0 ? "Its pipes reach nothing yet." : $"{prefix} {ReachNames(building)}.";
        IdleFix? FixPipe()
        {
            if (!plan || PlanFixPipe(building) is not { } pipe)
            {
                return null;
            }
            // A fix that leaves it idle says so: what it will still need
            var missing = MissingAfterPipe(building, pipe.Target!);
            return new IdleFix(IdleFixKind.LayPipe, $"Lay the pipe to your {BuildingName(pipe.Target!)} · {Tiles(pipe.NewTiles)} · {Gold(pipe.Cost)}"
                + (missing.Count > 0 ? $" · then it needs {string.Join(" and ", missing.Select(GoodsWord))}" : ""), pipe, Cost: pipe.Cost);
        }
        // Your own short building first, else the trading post or a Storage
        IdleFix? FixOwnNeedFirst()
        {
            if (plan && reason.Resource is { } made && PlanOwnNeedPipe(building, made) is { } own)
            {
                return new IdleFix(IdleFixKind.LayPipe, $"Lay the pipe to your {BuildingName(own.Target!)} · {Tiles(own.NewTiles)} · {Gold(own.Cost)}", own, Cost: own.Cost, OwnNeed: true);
            }
            return FixPipe();
        }
        switch (reason.Cause)
        {
            case IdleCause.Damaged:
                return new IdleStatus(reason, "Half speed: storm damage", null, new IdleFix(IdleFixKind.Repair, $"Repair · {Gold(RepairCost(building))}", Building: building.Id, Cost: RepairCost(building)));
            case IdleCause.Stuck:
                return new IdleStatus(reason, $"⛔ Stuck: {good} can't leave",
                    (PipeReach(building).Count == 0 ? "Its pipes reach nothing yet." : $"Its pipes reach only: {ReachNames(building)}.") + " Goods never pass through a building.",
                    FixOwnNeedFirst());
            case IdleCause.TakerStuck when reason.Other is { } other && Building(other) is { } taker:
                return new IdleStatus(reason, $"Idle: {BuildingName(taker)} is stuck",
                    $"Its pipe reaches only the {BuildingName(taker)}, and its {(IdleReasonOf(taker).Resource is { } r ? GoodsWord(r) : "goods")} can't leave.",
                    new IdleFix(IdleFixKind.Open, $"Open the {BuildingName(taker)}", Building: taker.Id));
            case IdleCause.TakerIdle when reason.Other is { } other && Building(other) is { } taker:
                // A taker that runs, if slowly, isn't "not running" (run 2: Cleo's mine at 24 of 42 energy)
                var takerRate = NetworkOf(taker.Id)?.Solution.Rate(taker.Id) ?? 0;
                return new IdleStatus(reason, takerRate > 0.005
                        ? $"Slowed: its {BuildingName(taker)} runs at {takerRate * 100:0}%"
                        : $"Idle: {BuildingName(taker)} isn't running",
                    $"Its {good} {(good.EndsWith('s') ? "go" : "goes")} only to the {BuildingName(taker)}, which can't take more now.",
                    new IdleFix(IdleFixKind.Open, $"Open the {BuildingName(taker)}", Building: taker.Id));
            case IdleCause.TakersFull when reason.Other is { } other && Building(other) is { } taker:
                var only = PipeReach(building).Where(t => ToFlowNode(t) is { Kind: FlowNodeKind.Building } n && n.In.ContainsKey(reason.Resource!)).Select(BuildingName).Distinct().ToList();
                return new IdleStatus(reason, $"Slowed: only {(only.Count == 1 ? $"the {only[0]}" : "the buildings on its pipe")} takes its {good}",
                    $"It sells nothing: its pipe reaches no trading post or Storage, and goods never pass through a building.", FixOwnNeedFirst());
            case IdleCause.Full when definition.Kind == BuildingKind.Storage:
                // What it holds, who waits behind it, and one pipe that frees them
                var (holds, waiting, first) = StorageLine(building);
                IdleFix? freeThem = null;
                if (plan && first != null && PlanFixPipe(first) is { } free && free.Target?.Id != building.Id)
                {
                    freeThem = new IdleFix(IdleFixKind.LayPipe, $"Lay the pipe from your {BuildingName(first)} to your {BuildingName(free.Target!)} · {Tiles(free.NewTiles)} · {Gold(free.Cost)}", free, Cost: free.Cost);
                }
                return new IdleStatus(reason, "Full", waiting != null
                    ? $"It holds {holds}; {waiting} slow behind it, because their pipes reach nothing else that takes it."
                    : $"It holds {holds}. What comes now goes on to your trading post.", freeThem);
            case IdleCause.Full:
                return new IdleStatus(reason, "Slowed: its Storage is full", $"Nothing else on its pipes takes its {GoodsWord(definition.Out.Keys.FirstOrDefault() ?? "")}.", FixPipe());
            case IdleCause.InputMissing:
                return new IdleStatus(reason, $"Idle: no {good} here", Reach("Its pipes reach:"), plan ? SupplyFix(building, reason.Resource!) : null);
            case IdleCause.InputShort:
                var (have, need) = InputHaveNeed(building, reason.Resource!);
                var rate = NetworkOf(building.Id)?.Solution.Rate(building.Id) ?? 0;
                return new IdleStatus(reason, $"Running at {rate:P0}: {good} short", $"Its pipes bring {Amount(have)} of the {Amount(need)} {good} it needs.", null);
            case IdleCause.PostFull when definition.Kind == BuildingKind.TradingPost:
                return new IdleStatus(reason, "Sells all it can", $"{Amount(TradingPostThroughput(building, definition))} a minute: upgrade it, or build a Market Hall, to sell more.",
                    new IdleFix(IdleFixKind.Upgrade, $"Upgrade it · {Gold(UpgradeCost(building, 1))}", Building: building.Id, Cost: UpgradeCost(building, 1)));
            case IdleCause.PostFull:
                var post = PipeReach(building).FirstOrDefault(t => t.Owner == building.Owner && Content.Building(t.Type).Kind == BuildingKind.TradingPost && TradingPostFull(t));
                return new IdleStatus(reason, "Slowed: Trading Post full",
                    post == null ? null : $"It sells all it can: {Amount(TradingPostThroughput(post, Content.Building(post.Type)))} a minute.",
                    post == null ? null : new IdleFix(IdleFixKind.Upgrade, $"Upgrade the Trading Post · {Gold(UpgradeCost(post, 1))}", Building: post.Id, Cost: UpgradeCost(post, 1)));
            case IdleCause.Resting:
                return new IdleStatus(reason, "Rests: nothing to research",
                    "Every tech this Era is done and every Refinement is at its cap: it eats nothing until the next Era's research.", null);
            case IdleCause.Pumps:
                var network = NetworkOf(building.Id);
                return new IdleStatus(reason, "Slowed: too few pumps",
                    $"{network?.Buildings.Count ?? 0} buildings on these pipes: everything runs at {network?.PumpFactorOf(building.Owner) ?? 1:P0}.",
                    new IdleFix(IdleFixKind.Build, "Build a Pump Station", BuildingType: "pumpStation"));
            case IdleCause.PipeFull:
                // The pipe is the limit: a second taker on a pipe of its own carries as much again (each connection
                // carries its pipe's throughput), a better pipe carries more
                var (could, carried) = PipeCarry(building, reason.Resource!);
                var second = Content.BuildingList.FirstOrDefault(t => t.Kind == BuildingKind.TradingPost && t.Buildable && Company(building.Owner) is { } owner && IsUnlocked(owner, t, out _));
                // Its pipe door: the next tier or Widen, then the levels the new pipe carries (after run 4: one door, with its
                // preview, where the run's players looked); before Copper Pipes, a second taker on a pipe of its own
                IdleFix? fix = Company(building.Owner) is { } climber && PipeDoorFor(climber, building, preview: false) is { Kind: not PipeDoorKind.Locked } door
                    ? new IdleFix(IdleFixKind.Climb, DoorLabel(door), Building: building.Id, Cost: door.Kind == PipeDoorKind.Waits ? door.OrderCost ?? 0 : door.Gold + door.LevelCost)
                    : second == null ? null : new IdleFix(IdleFixKind.Build, $"Build a second {second.Name} on its pipe · {Gold(second.Cost.Gold)}", BuildingType: second.Id, Cost: second.Cost.Gold);
                return new IdleStatus(reason, $"Slowed: its pipes carry {Amount(carried)} {good} a minute",
                    $"It could make {Amount(could)} a minute, but a pipe connection carries only so much (Grey Pipe: 100 a minute). A better or wider pipe, or a second taker on a pipe of its own, takes more.",
                    fix);
            case IdleCause.GateOnly:
                return new IdleStatus(reason, $"Waiting: nobody here takes its {good}", "Its only way out is your Yard gate (G), and nobody at your Yard needs it now.", FixPipe());
            default:
                return new IdleStatus(reason, null, null, null);
        }
    }

    /// <summary>
    /// A pipe door's button: "Copper Pipe on its 2 links (34 tiles), then Level 30 · 212k + 34 copper ingots", "Widen its
    /// link ×2, then Level 27 · 98k", "Its Copper Pipe waits for 22 copper ingots · Order them · 1,584"
    /// </summary>
    public string DoorLabel(PipeDoor door) => door.Kind switch
    {
        PipeDoorKind.Waits => door.OrderCost is { } cost ? $"{door.Text} · Order them · {Gold(cost)}" : $"{door.Text}: the hub brings them",
        _ => $"{door.Text} · {Gold(door.Gold + door.LevelCost)}{string.Concat(door.Materials.Select(t => $" + {Amount(t.Value)} {GoodsWord(t.Key)}"))}",
    };

    /// <summary>What a building gets of an input now, and what it would need at full speed</summary>
    private (double Have, double Need) InputHaveNeed(Building building, string resource)
    {
        var network = NetworkOf(building.Id);
        var node = network?.Nodes.FirstOrDefault(t => t.Id == building.Id) ?? ToFlowNode(building);
        var have = network == null ? 0
            : network.Solution.Flows.Where(t => t.To == building.Id && t.Resource == resource).Sum(t => t.Delivered)
              + network.GateFlows.Where(t => t.Import && t.Building == building.Id && t.Resource == resource).Sum(t => t.Delivered);
        return (have, (node?.In.GetValueOrDefault(resource) ?? 0) * (node?.MaxRate ?? 1));
    }

    /// <summary>The fix for a missing input: a pipe to an own building that makes it, or the build tool with one that does</summary>
    private IdleFix? SupplyFix(Building building, string resource)
    {
        if (Company(building.Owner) is not { } company)
        {
            return null;
        }
        var makers = BuildingsOf(company.Id).Where(t => t.Id != building.Id && !t.UnderConstruction && Content.Building(t.Type).Out.ContainsKey(resource)).ToList();
        foreach (var maker in makers.OrderBy(t => Math.Abs(t.X - building.X) + Math.Abs(t.Y - building.Y)))
        {
            var goal = maker.Border().ToHashSet();
            if (PlanPipe(company, building, goal.Contains) is { } pipe)
            {
                return new IdleFix(IdleFixKind.LayPipe, $"Plan a pipe to your {BuildingName(maker)} · {Tiles(pipe.NewTiles)} · {Gold(pipe.Cost)}", pipe with { Target = maker }, Cost: pipe.Cost);
            }
        }
        var producer = Content.BuildingList
            .Where(t => t.Buildable && t.Kind == BuildingKind.Producer && t.Out.ContainsKey(resource) && !t.In.ContainsKey(resource) && IsUnlocked(company, t, out _))
            .OrderBy(t => BuildCost(company, t) / t.Out[resource])
            .FirstOrDefault();
        return producer == null ? null : new IdleFix(IdleFixKind.Build, $"Build {Article(producer.Name)}", BuildingType: producer.Id);
    }
}
