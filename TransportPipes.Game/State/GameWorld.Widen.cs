using TransportPipes.Game.Simulation;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// Widen (after run 3, the design team's step 9): a Copper or Steel Pipe at its limit carries ×2 for an hour of its
/// owner's measured income, ×4 for two hours more; the cap is ×4 a tier, past it the next tier is a want. Grey Pipes don't
/// widen, so the first door past 100 a minute stays a friend's copper. The one private sink that grows with income.
/// </summary>
public sealed partial class GameWorld
{
    public const int MaxWiden = 4;

    /// <summary>The hours of income a pipe widened this far has cost: ×2 one, ×4 three</summary>
    public static double WidenHours(int factor) => factor >= 4 ? 3 : factor >= 2 ? 1 : 0;

    /// <summary>Copper and Steel Pipes widen; Grey and the late tiers don't</summary>
    public static bool Widens(int tier) => tier is 2 or 3;

    /// <summary>
    /// What widening a company's pipes from one factor to another costs: the hours between, of its measured income, for
    /// each link they carry (after run 4: one price widened any number of links)
    /// </summary>
    public double WidenCost(Company company, int from, int to, int links = 1) => Math.Ceiling((WidenHours(to) - WidenHours(from)) * 60 * MeasuredIncome(company) * Math.Max(1, links));

    /// <summary>Widens a company's Copper or Steel Pipe tiles to ×2 or ×4, for hours of its income</summary>
    public CommandResult WidenPipes(long companyId, IReadOnlyList<TilePos> tiles, int factor)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (factor is not (2 or MaxWiden))
        {
            return CommandResult.Fail("A pipe widens ×2 or ×4.");
        }
        var pipes = tiles.Distinct()
            .Select(t => _conductors.TryGetValue(t, out var conductor) ? conductor : null)
            .OfType<Conductor>()
            .Where(t => t.Kind == ConductorKind.Pipe && t.Owner == companyId)
            .ToList();
        if (pipes.Count == 0)
        {
            return CommandResult.Fail("Those aren't your pipes.");
        }
        if (pipes.Any(t => !Widens(t.Tier)))
        {
            return CommandResult.Fail("Only Copper and Steel Pipes widen: a Grey Pipe takes Copper first.");
        }
        var from = pipes.Min(t => Math.Max(1, t.Widen));
        if (from >= factor)
        {
            return CommandResult.Fail($"That pipe carries ×{from} already.");
        }
        var cost = WidenCost(company, from, factor, LinksThrough(companyId, pipes.Select(t => t.Pos).ToList()));
        var payment = Pay(company, cost, new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }
        AddStat(company, "widened", cost);
        foreach (var conductor in pipes)
        {
            conductor.Widen = Math.Max(conductor.Widen, factor);
            conductor.Touch();
            MarkDirty(conductor.Pos);
            Changes.Tiles.Add(conductor.Pos);
        }
        return CommandResult.Ok;
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

    /// <summary>How many of a company's links run through some pipe tiles (Widen is paid per link)</summary>
    private int LinksThrough(long companyId, IReadOnlyCollection<TilePos> tiles)
    {
        var set = tiles.ToHashSet();
        return Math.Max(1, _networks.Values.Where(t => t.Conductors.Overlaps(set))
            .SelectMany(t => t.Links)
            .Where(t => t.Path.Any(set.Contains))
            .Select(t => (Math.Min(t.A, t.B), Math.Max(t.A, t.B)))
            .Distinct()
            .Count());
    }

    /// <summary>Widen for a building whose Copper or Steel Pipe is full: to what, for how much, and what it carries then (null: none)</summary>
    public (int From, int Factor, double Cost, List<TilePos> Tiles, double Carries)? WidenOffer(Building building, string resource)
    {
        if (Company(building.Owner) is not { } owner || FullLinkPipes(building, resource) is not { Count: > 0 } pipes || pipes.Any(t => !Widens(t.Tier)))
        {
            return null;
        }
        var from = pipes.Min(t => Math.Max(1, t.Widen));
        if (from >= MaxWiden)
        {
            return null;
        }
        var factor = from >= 2 ? MaxWiden : 2;
        var carries = Content.PipeTier(pipes.Min(t => t.Tier)).Throughput * factor * (1 + ThroughputBonus(owner.Id));
        var tiles = pipes.Select(t => t.Pos).ToList();
        return (from, factor, WidenCost(owner, from, factor, LinksThrough(owner.Id, tiles)), tiles, carries);
    }
}
