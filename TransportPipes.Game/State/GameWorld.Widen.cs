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

    /// <summary>What widening a company's pipes from one factor to another costs: the hours between, of its measured income</summary>
    public double WidenCost(Company company, int from, int to) => Math.Ceiling((WidenHours(to) - WidenHours(from)) * 60 * MeasuredIncome(company));

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
        var cost = WidenCost(company, from, factor);
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

    /// <summary>The owner's pipe tiles on the full links a building's good leaves by</summary>
    private List<Conductor> FullLinkPipes(Building building, string resource)
    {
        if (NetworkOf(building.Id) is not { } network)
        {
            return [];
        }
        var links = network.Links.Where(t => (t.A == building.Id || t.B == building.Id) && t.Allows(resource)
            && network.Solution.Flows.Where(f => f.From == building.Id && f.Resource == resource && t.Connects(f.From, f.To)).Sum(f => f.Sent) >= t.Capacity * 0.98).ToList();
        return links.SelectMany(t => t.Path).Distinct()
            .Select(t => _conductors.TryGetValue(t, out var c) ? c : null).OfType<Conductor>()
            .Where(t => t.Kind == ConductorKind.Pipe && t.Owner == building.Owner).ToList();
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
        return (from, factor, WidenCost(owner, from, factor), pipes.Select(t => t.Pos).ToList(), carries);
    }
}
