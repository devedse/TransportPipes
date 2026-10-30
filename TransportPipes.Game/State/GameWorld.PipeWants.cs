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
                Resource = resource, Amount = amount,
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
        var laid = 0;
        foreach (var pos in ask.PipeTiles!)
        {
            if (_conductors.TryGetValue(pos, out var conductor) && conductor.Kind == ConductorKind.Pipe && conductor.Owner == owner.Id && conductor.Tier < ask.PipeTier)
            {
                conductor.Tier = ask.PipeTier;
                conductor.Touch();
                MarkDirty(pos);
                Changes.Tiles.Add(pos);
                laid++;
            }
        }
        owner.Asks.RemoveAll(t => t.PipeTiles != null && t.WantId == ask.WantId);
        TidyPipeWants(owner);
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

    /// <summary>A company's pipe tiles that wait for a pipe of a tier or better (an open pipe want's)</summary>
    public HashSet<TilePos> PipeTilesWaiting(Company company, int tier = 0) =>
        company.Asks.Where(t => t.PipeTiles != null && t.PipeTier >= tier).SelectMany(t => t.PipeTiles!).ToHashSet();

    /// <summary>
    /// Pipe wants with nothing left to do are given back (playtest: Upgrade clicked five times made five Copper Pipe wants
    /// on the same 13 tiles; once one was laid the others still wanted their ingots): one whose tiles are all that pipe or
    /// better already, and one whose tiles another wants for the same pipe or better (the one that got furthest stays).
    /// Its gold and what was paid for its goods come back, its owner's own goods go back into the Storages.
    /// </summary>
    private void TidyPipeWants(Company company)
    {
        var wants = company.Asks.Where(t => t.PipeTiles != null).GroupBy(t => t.WantId)
            .Select(g => (Id: g.Key, Tier: g.First().PipeTier, Tiles: g.First().PipeTiles!, Done: g.Sum(t => t.Got) / Math.Max(1e-9, g.Sum(t => t.Amount))))
            .OrderByDescending(t => t.Tier).ThenByDescending(t => t.Done).ThenBy(t => t.Id)
            .ToList();
        var kept = new HashSet<TilePos>();
        foreach (var want in wants)
        {
            var open = want.Tiles
                .Where(p => _conductors.TryGetValue(p, out var conductor) && conductor.Kind == ConductorKind.Pipe && conductor.Owner == company.Id && conductor.Tier < want.Tier)
                .ToList();
            if (open.Count > 0 && !open.All(kept.Contains))
            {
                kept.UnionWith(open);
                continue;
            }
            var gold = company.Gold;
            if (CancelPipeWant(company.Id, want.Id).Success)
            {
                Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.ConstructionFinished,
                    $"Your {Content.PipeTier(want.Tier).Name} on {want.Tiles.Count} tiles had nothing left to do ({(open.Count == 0 ? "those pipes are better already" : "another wants the same pipes")}): +{company.Gold - gold:N0} g back", Bell: false));
            }
        }
    }

    /// <summary>The pipe wants of every company, tidied once after a load (wants made twice before the playtest's fix)</summary>
    internal void TidyAllPipeWants()
    {
        foreach (var company in _companies.Values.Where(t => t.Asks.Any(a => a.PipeTiles != null)).ToList())
        {
            TidyPipeWants(company);
        }
    }

    /// <summary>A pipe want given up: the pipes stay as they were, and the gold (the pipes' and what was paid for goods) comes back</summary>
    public CommandResult CancelPipeWant(long companyId, long wantId)
    {
        if (Company(companyId) is not { } company || company.Asks.Where(t => t.PipeTiles != null && t.WantId == wantId).ToList() is not { Count: > 0 } parts)
        {
            return CommandResult.Fail("That pipe has what it needs.");
        }
        var tier = Content.PipeTier(parts[0].PipeTier);
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
