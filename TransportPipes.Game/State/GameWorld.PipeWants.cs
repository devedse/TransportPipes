using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// The pipe is a want (after run 3, the design team's step 3): a better pipe tier short of its materials (a Copper Pipe
/// wants a copper ingot a tile) waits for them like a scaffold, and the pipes become that tier once they are in. Copper
/// ingots are copper and coal, usually two seats' land: the pipe wall is the most natural gift between friends.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>The pipes keep their tier and carry on; their gold is paid, what the Storages hold goes in, and they want the rest</summary>
    private CommandResult WantPipes(Company company, List<Conductor> pipes, PipeTierDefinition tier, Dictionary<string, double> materials)
    {
        var gold = tier.Gold * pipes.Count;
        company.Gold -= gold;
        company.Report.GoldSpent += gold;
        AddStat(company, "goldSpent", gold);
        var id = company.Asks.Select(t => t.WantId).DefaultIfEmpty(0).Max() + 1;
        foreach (var (resource, amount) in materials)
        {
            company.Asks.Add(new CompanyAsk
            {
                Resource = resource, Amount = amount, Until = DateTime.MaxValue, Hours = 0,
                PipeTiles = pipes.Select(t => t.Pos).ToList(), PipeTier = tier.Tier, WantId = id, Paid = 0, Raised = LastTick,
            });
        }
        FillWantsFromStorages(company);
        foreach (var ask in company.Asks.Where(t => t.WantId == id && t.PipeTiles != null).ToList())
        {
            FinishPipeWantIfFull(company, ask);
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        WakeCoupling();
        if (company.Asks.Any(t => t.WantId == id && t.PipeTiles != null))
        {
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.ConstructionFinished,
                $"Your {tier.Name} on {pipes.Count} tiles wants {string.Join(" and ", materials.Select(t => $"{t.Value:N0} {GoodsWord(t.Key)}"))}: the Yard and your neighbours bring them"));
        }
        return CommandResult.Ok;
    }

    /// <summary>A pipe want's goods are all in: its tiles become the better tier (those still its owner's and below it)</summary>
    private void FinishPipeWantIfFull(Company owner, CompanyAsk ask)
    {
        var parts = owner.Asks.Where(t => t.PipeTiles != null && t.WantId == ask.WantId).ToList();
        if (parts.Count == 0 || parts.Any(t => t.Left > 1e-6))
        {
            return;
        }
        foreach (var part in parts)
        {
            CloseOrder(part);
        }
        var laid = 0;
        foreach (var pos in ask.PipeTiles!)
        {
            if (_conductors.TryGetValue(pos, out var conductor) && conductor.Kind == ConductorKind.Pipe && conductor.Owner == owner.Id && conductor.Tier < ask.PipeTier)
            {
                conductor.Tier = ask.PipeTier;
                conductor.Widen = 1;
                conductor.Touch();
                MarkDirty(pos);
                Changes.Tiles.Add(pos);
                laid++;
            }
        }
        owner.Asks.RemoveAll(t => t.PipeTiles != null && t.WantId == ask.WantId);
        AddStat(owner, "pipeTiers", laid);
        var tier = Content.PipeTier(ask.PipeTier);
        var givers = parts.SelectMany(t => t.From.Where(f => f.Key != owner.Id && f.Key != HubSource && f.Value > 0.5).Select(f => (Who: f.Key, t.Resource, Units: f.Value))).ToList();
        Changes.Events.Add(new WorldEvent(owner.Id, WorldEventKind.ConstructionFinished,
            $"Your {tier.Name} is laid: {laid} tiles carry {tier.Throughput:N0} a minute{(givers.Count > 0 ? $", {string.Join(" and ", givers.Select(t => $"{Company(t.Who)?.Name}'s {GoodsWord(t.Resource)}").Distinct())} in it 💛" : "")}"));
        foreach (var (who, resource, units) in givers)
        {
            Changes.Events.Add(new WorldEvent(who, WorldEventKind.Neighbour, $"Your {GoodsWord(resource)} made {owner.Name}'s {tier.Name}: {units:N0} of them"));
        }
        owner.Touch();
        Changes.Companies.Add(owner.Id);
    }

    /// <summary>A company's open wants, a building's and a pipe's, each with its target: a building's id, or minus a pipe want's number</summary>
    public List<(long Target, string Name, CompanyAsk Want)> OpenWants(Company owner) =>
        owner.Asks.Where(t => t.IsWant && t.Left > 1e-6)
            .Select(t => t.Site is { } site
                ? (site, Building(site) is { } b ? Content.Building(b.Type).Name : "building", t)
                : (-t.WantId, Content.PipeTier(t.PipeTier).Name, t))
            .ToList();

    /// <summary>A pipe want given up: the pipes stay as they were, and the gold (the pipes' and what was paid for goods) comes back</summary>
    public CommandResult CancelPipeWant(long companyId, long wantId)
    {
        if (Company(companyId) is not { } company || company.Asks.Where(t => t.PipeTiles != null && t.WantId == wantId).ToList() is not { Count: > 0 } parts)
        {
            return CommandResult.Fail("That pipe has what it needs.");
        }
        var tier = Content.PipeTier(parts[0].PipeTier);
        foreach (var part in parts)
        {
            CloseOrder(part);
        }
        var refund = parts.Sum(t => t.Paid) + tier.Gold * parts[0].PipeTiles!.Count;
        foreach (var ask in parts)
        {
            if (ask.From.GetValueOrDefault(company.Id) is > 1e-6 and var own)
            {
                PutInStorages(company.Id, ask.Resource, own);
            }
        }
        company.Asks.RemoveAll(t => t.PipeTiles != null && t.WantId == wantId);
        company.Gold += refund;
        // Taken back: it counts as unspent (the playtest's measure)
        AddStat(company, "goldSpent", -refund);
        company.Touch();
        Changes.Companies.Add(company.Id);
        WakeCoupling();
        return CommandResult.Ok;
    }
}
