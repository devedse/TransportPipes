using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// One part of "Grow everything that pays": a building's pipe door (the pipe, then its levels: Levels is the level it
/// reaches), its Max (Levels more), the next parcel (Parcel set, Building -1) or the next outskirts parcel (Outskirts
/// set, Building -1); Goods: what the goods of its levels from 11 cost as they would come
/// </summary>
public sealed record GrowPart(long Building, string Text, bool Door, int Levels, double Cost, double Gain, int? Parcel = null, int? Outskirts = null, double Goods = 0,
    IReadOnlyList<TilePos>? PipeTiles = null, int PipeTier = 0);

/// <summary>
/// What "Grow everything that pays" would buy now, its price, what its levels' goods cost on top (they come after the
/// click), and what it adds a minute once they're in; Saving says what it keeps the gold for instead (the next parcel, soon)
/// </summary>
public sealed record GrowPlan(List<GrowPart> Parts, double Cost, double Gain, string? Saving = null, double Goods = 0);

/// <summary>
/// Your gold, in one tap (after run 4, the design team's "your gold can"; 1.4 §17: the model's player): the next parcel
/// as soon as the level and the gold allow (saving for it while it is at most eight hours of income away), then every
/// pipe door and every Max that pays back within five days, the best payback first, as far as the gold goes, with one
/// preview and an hour's Undo. Run 4's returners came back to days of income and bought one level at a time: Ben came
/// back to 13M and spent 3.4k.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>A part pays back within this many minutes, or it isn't in the plan (the model's five days)</summary>
    public const double GrowPaybackMinutes = 5 * 24 * 60;

    /// <summary>Grow saves for the next parcel while it costs at most this many minutes of income</summary>
    public const double SaveForParcelMinutes = 8 * 60;

    /// <summary>What "Grow everything that pays" buys now</summary>
    public GrowPlan PlanGrowth(Company company)
    {
        // The estimates' values per network, fresh (the advisor's are from its last look)
        _values.Clear();
        var left = company.Gold;
        var chosen = new List<GrowPart>();
        // Land first, as the model buys it: more room is what the levels can't give
        var (parcelCost, parcelLevel) = NextParcel(company);
        if (parcelLevel <= company.Level && BestParcel(company) is { } parcel)
        {
            var holds = parcel.Holds.Count == 0 ? "room for more buildings" : string.Join(", ", parcel.Holds.Take(2).Select(t => t.Label));
            if (parcelCost <= left)
            {
                chosen.Add(new GrowPart(-1, $"A new parcel: {holds}", false, 0, parcelCost, 0, parcel.Parcel));
                left -= parcelCost;
            }
            else if (GoldPerMinute().GetValueOrDefault(company.Id) is var income && income > 0 && (parcelCost - left) / income <= SaveForParcelMinutes)
            {
                return new GrowPlan([], 0, 0, $"Saving for your next parcel ({Gold(parcelCost)}): {Gold(parcelCost - left)} more, {Minutes((parcelCost - left) / income)} at your income now");
            }
        }
        // Then the outskirts from Era 2, in the same way (1.4 §5)
        if (OutskirtsBlocker(company) == null && EstateOf(company) is { } home)
        {
            var cost = NextOutskirtsCost(company);
            var free = Enumerable.Range(0, WorldLayout.OutskirtsParcels(home.Slot).Count).First(t => !home.HasOutskirts(t));
            if (cost <= left)
            {
                chosen.Add(new GrowPart(-1, "An outskirts parcel: room beyond your plot", false, 0, cost, 0, Outskirts: free));
                left -= cost;
            }
            else if (GoldPerMinute().GetValueOrDefault(company.Id) is var income && income > 0 && (cost - left) / income <= SaveForParcelMinutes)
            {
                return new GrowPlan(chosen, Math.Round(chosen.Sum(t => t.Cost)), 0,
                    chosen.Count > 0 ? null : $"Saving for your next outskirts parcel ({Gold(cost)}): {Gold(cost - left)} more, {Minutes((cost - left) / income)} at your income now");
            }
        }
        var parts = new List<GrowPart>();
        foreach (var building in BuildingsOf(company.Id).Where(t => !t.UnderConstruction && !IsSite(t)).ToList())
        {
            var definition = Content.Building(building.Type);
            if (definition.Kind is BuildingKind.Hq or BuildingKind.Gate or BuildingKind.Decoration || !definition.Buildable)
            {
                continue;
            }
            // Which parts pay is ranked by an estimate on a big network (solving each of 200 buildings took 4 s); what the plan
            // brings is solved once for all of them (PlanGain)
            var network = NetworkOf(building.Id);
            var estimate = network != null && Estimated(network);
            // Its pipe door first: the levels come with it
            if (PipeDoorFor(company, building, exact: !estimate) is { Kind: PipeDoorKind.Tier, Short.Count: 0 } door && door.Gain > 0.5)
            {
                parts.Add(new GrowPart(building.Id, $"{definition.Name}: {char.ToLowerInvariant(door.Text[0])}{door.Text[1..]}", true, door.ToLevel, door.Gold + door.LevelCost, door.Gain,
                    Goods: StepGoodsCost(building, Math.Max(0, door.ToLevel - BoughtLevel(building))), PipeTiles: door.Tiles, PipeTier: door.Tier));
                continue;
            }
            if (SmartMax(company, building) is { Levels: > 0 } max)
            {
                var gain = estimate && network != null
                    ? EstimateChange(network, company, NetworkNode(network, building), ToFlowNode(building, BoughtLevel(building) + max.Levels))
                    : UpgradePreview(company, building, max.Levels).Gain;
                if (gain > 0.5)
                {
                    parts.Add(new GrowPart(building.Id, $"{definition.Name} to level {BoughtLevel(building) + max.Levels}", false, max.Levels, max.Cost, gain,
                        Goods: StepGoodsCost(building, max.Levels)));
                }
            }
        }
        // What pays back counts the goods of its levels from 11 too (playtest: Grow said 50.2k g, and its levels then
        // waited for 20.5k g of bricks and more)
        var post = false;
        // Of two equal trading posts, the one that sells
        double Sells(long id) => NetworkOf(id)?.Solution.Flows.Where(t => t.To == id && t.Kind == FlowKind.Sale).Sum(t => t.Delivered) ?? 0;
        foreach (var part in parts.Where(t => (t.Cost + t.Goods) / t.Gain <= GrowPaybackMinutes).OrderBy(t => (t.Cost + t.Goods) / t.Gain).ThenByDescending(t => Sells(t.Building)))
        {
            // One trading post: the Hub Orders follow the highest, a second one's levels would add nothing
            var isPost = Building(part.Building) is { } b && Content.Building(b.Type).Kind == BuildingKind.TradingPost;
            if (part.Cost <= left && !(isPost && post))
            {
                post |= isPost;
                chosen.Add(part);
                left -= part.Cost;
            }
        }
        return new GrowPlan(chosen, Math.Round(chosen.Sum(t => t.Cost)), Math.Round(PlanGain(company, chosen), 1), Goods: Math.Round(chosen.Sum(t => t.Goods)));
    }

    /// <summary>
    /// What a plan's parts add a minute together, as they will run once their goods are in (playtest: Grow promised +504
    /// g/min and brought about +300; "the grow button should match exactly"): every network with all its parts at once,
    /// their levels and their new pipes (GameWorld.WhatIf), at the price the market settles on for all the company sells,
    /// its Hub Orders at the highest trading post's new level
    /// </summary>
    private double PlanGain(Company company, IReadOnlyList<GrowPart> parts)
    {
        var to = new Dictionary<long, int>();
        var tiers = new Dictionary<TilePos, int>();
        foreach (var part in parts.Where(t => t.Parcel == null && t.Outskirts == null))
        {
            if (Building(part.Building) is not { } building)
            {
                continue;
            }
            var level = part.Door ? part.Levels : BoughtLevel(building) + part.Levels;
            if (level > building.Level)
            {
                to[building.Id] = Math.Max(to.GetValueOrDefault(building.Id), level);
            }
            foreach (var tile in part.PipeTiles ?? [])
            {
                tiers[tile] = Math.Max(tiers.GetValueOrDefault(tile), part.PipeTier);
            }
        }
        var posts = BuildingsOf(company.Id).Where(t => !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.TradingPost).ToList();
        var postThen = posts.Select(t => to.TryGetValue(t.Id, out var level) ? level : BoughtLevel(t)).DefaultIfEmpty(1).Max();
        var factor = Math.Pow(Content.Progression.Market.PostLevel, Math.Max(0, postThen - PostLevel(company)));
        var solutions = new Dictionary<int, FlowSolution>();
        foreach (var network in _networks.Values.Where(t => t.Owner == company.Id))
        {
            var changed = network.Buildings.Where(t => to.ContainsKey(t) && Content.Building(_buildings[t].Type).Kind != BuildingKind.TradingPost).ToHashSet();
            var pipes = tiers.Where(t => network.Conductors.Contains(t.Key)).ToDictionary(t => t.Key, t => t.Value);
            if (changed.Count == 0 && pipes.Count == 0)
            {
                continue;
            }
            solutions[network.Id] = SolveWhatIf(network,
                t => changed.Contains(t.Id) && ToFlowNode(_buildings[t.Id], to[t.Id]) is { } up ? WithPumps(network, [up])[0] : t,
                pipeTiers: pipes);
        }
        // From the levels bought: those still waiting for their goods count as in
        return Math.Max(0, SettledGain(company, solutions, factor));
    }

    /// <summary>Buys the plan: each pipe door, each Max, the best first, as far as the gold goes; remembered for an hour's Undo</summary>
    public CommandResult GrowEverything(long companyId)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        var plan = PlanGrowth(company);
        if (plan.Parts.Count == 0)
        {
            return CommandResult.Fail(plan.Saving ?? "Nothing pays back within five days now: your buildings are at what their pipes and inputs carry.");
        }
        var gold = company.Gold;
        var done = new List<long>();
        var land = 0;
        // The parcel first, then the pipe doors: they read the flows as they are now, and an upgrade elsewhere changes them
        foreach (var part in plan.Parts.OrderBy(t => t.Parcel != null || t.Outskirts != null ? 0 : t.Door ? 1 : 2))
        {
            if (part.Parcel is { } parcel)
            {
                land += BuyParcel(companyId, parcel).Success ? 1 : 0;
                continue;
            }
            if (part.Outskirts is { } outskirts)
            {
                land += BuyOutskirts(companyId, outskirts).Success ? 1 : 0;
                continue;
            }
            var result = part.Door
                ? ClimbPipe(companyId, part.Building)
                : Upgrade(companyId, part.Building, Math.Min(part.Levels, Building(part.Building) is { } b ? AffordableLevels(company, b) : 0));
            if (result.Success)
            {
                done.Add(part.Building);
            }
        }
        if (done.Count == 0 && land == 0)
        {
            return CommandResult.Fail("None of it could be bought now.");
        }
        company.LastGrowth = new GrowthPurchase { At = LastTick, Buildings = done };
        AddStat(company, "pressed:grow", 1);
        company.Touch();
        Changes.Companies.Add(company.Id);
        var what = string.Join(" and ", new[] { land > 0 ? "a new parcel" : null, done.Count > 0 ? $"{done.Count} {(done.Count == 1 ? "building" : "buildings")}" : null }.OfType<string>());
        return CommandResult.Done($"Grown: {what} for {Gold(gold - company.Gold)}, +{Gold(plan.Gain)}/min"
            + (plan.Goods >= 1 ? " once its levels' goods are in (your own go in first; the Grow button says what the market would charge for the rest)" : ""));
    }

    /// <summary>The last "Grow everything" within the hour: its levels back, as each building's Undo gives them</summary>
    public CommandResult UndoGrowth(long companyId)
    {
        if (Company(companyId) is not { } company || company.LastGrowth is not { } last || LastTick - last.At > UndoFor)
        {
            return CommandResult.Fail("Nothing to take back: an hour has passed, or nothing was grown.");
        }
        var undone = last.Buildings.Count(t => Undo(companyId, t).Success);
        company.LastGrowth = null;
        company.Touch();
        Changes.Companies.Add(company.Id);
        return undone > 0 ? CommandResult.Done($"Taken back: {undone} {(undone == 1 ? "building" : "buildings")}' levels, the gold less what they earned since (pipes stay)") : CommandResult.Fail("Nothing to take back.");
    }
}

/// <summary>The last "Grow everything": when, and the buildings it upgraded (for its hour's Undo)</summary>
public sealed class GrowthPurchase
{
    public DateTime At { get; set; }
    public List<long> Buildings { get; set; } = new();
}
