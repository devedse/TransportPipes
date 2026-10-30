using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// One part of "Grow everything that pays": a building's pipe door (the pipe, then its levels), its Max, the next
/// parcel (Parcel set, Building -1) or the next outskirts parcel (Outskirts set, Building -1)
/// </summary>
public sealed record GrowPart(long Building, string Text, bool Door, int Levels, double Cost, double Gain, int? Parcel = null, int? Outskirts = null);

/// <summary>
/// What "Grow everything that pays" would buy now, its price and what it adds a minute (about: the parts are solved one
/// by one); Saving says what it keeps the gold for instead (the next parcel, soon)
/// </summary>
public sealed record GrowPlan(List<GrowPart> Parts, double Cost, double Gain, string? Saving = null);

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
                return new GrowPlan([], 0, 0, $"Saving for your next parcel ({Gold(parcelCost)}): {Gold(parcelCost - left)} more, about {Minutes((parcelCost - left) / income)} of income");
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
                    chosen.Count > 0 ? null : $"Saving for your next outskirts parcel ({Gold(cost)}): {Gold(cost - left)} more, about {Minutes((cost - left) / income)} of income");
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
            // Its pipe door first: the levels come with it
            if (PipeDoorFor(company, building) is { Kind: PipeDoorKind.Tier, Short.Count: 0 } door && door.Gain > 0.5)
            {
                parts.Add(new GrowPart(building.Id, $"{definition.Name}: {char.ToLowerInvariant(door.Text[0])}{door.Text[1..]}", true, door.ToLevel, door.Gold + door.LevelCost, door.Gain));
                continue;
            }
            if (SmartMax(company, building) is { Levels: > 0 } max && UpgradePreview(company, building, max.Levels) is { Gain: > 0.5 } preview)
            {
                parts.Add(new GrowPart(building.Id, $"{definition.Name} to level {building.Level + max.Levels}", false, max.Levels, max.Cost, preview.Gain));
            }
        }
        foreach (var part in parts.Where(t => t.Cost / t.Gain <= GrowPaybackMinutes).OrderBy(t => t.Cost / t.Gain))
        {
            if (part.Cost <= left)
            {
                chosen.Add(part);
                left -= part.Cost;
            }
        }
        return new GrowPlan(chosen, Math.Round(chosen.Sum(t => t.Cost)), Math.Round(chosen.Sum(t => t.Gain), 1));
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
        return CommandResult.Done($"Grown: {what} for {Gold(gold - company.Gold)}, about +{Gold(plan.Gain)}/min" + (done.Count > 0 ? " · Undo for an hour takes the levels back" : ""));
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
