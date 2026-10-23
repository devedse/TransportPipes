using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

/// <summary>One part of "Grow everything that pays": a building's pipe door (the pipe, then its levels), or its Max</summary>
public sealed record GrowPart(long Building, string Text, bool Door, int Levels, double Cost, double Gain);

/// <summary>What "Grow everything that pays" would buy now, its price and what it adds a minute (about: the parts are solved one by one)</summary>
public sealed record GrowPlan(List<GrowPart> Parts, double Cost, double Gain);

/// <summary>
/// Your gold, in one tap (after run 4, the design team's "your gold can"): every pipe door and every Max that pays back
/// within a week, the best payback first, as far as the gold goes, with one preview and an hour's Undo. Run 4's
/// returners came back to days of income and bought one level at a time: Ben came back to 13M and spent 3.4k; the
/// advisor's bots, pressing every step, held 2.7 days of income where people held 24.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>A part pays back within this many minutes, or it isn't in the plan</summary>
    public const double GrowPaybackMinutes = 7 * 24 * 60;

    /// <summary>What "Grow everything that pays" buys now</summary>
    public GrowPlan PlanGrowth(Company company)
    {
        var parts = new List<GrowPart>();
        foreach (var building in BuildingsOf(company.Id).Where(t => !t.UnderConstruction && !IsSite(t)).ToList())
        {
            var definition = Content.Building(building.Type);
            if (definition.Kind is BuildingKind.Hq or BuildingKind.Gate or BuildingKind.Decoration || !definition.Buildable)
            {
                continue;
            }
            // Its pipe door first: the levels come with it
            if (PipeDoorFor(company, building) is { Kind: PipeDoorKind.Tier or PipeDoorKind.Widen, Short.Count: 0 } door && door.Gain > 0.5)
            {
                parts.Add(new GrowPart(building.Id, $"{definition.Name}: {char.ToLowerInvariant(door.Text[0])}{door.Text[1..]}", true, door.ToLevel, door.Gold + door.LevelCost, door.Gain));
                continue;
            }
            if (SmartMax(company, building) is { Levels: > 0 } max && UpgradePreview(company, building, max.Levels) is { Gain: > 0.5 } preview)
            {
                parts.Add(new GrowPart(building.Id, $"{definition.Name} to level {building.Level + max.Levels}", false, max.Levels, max.Cost, preview.Gain));
            }
        }
        var chosen = new List<GrowPart>();
        var left = company.Gold;
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
            return CommandResult.Fail("Nothing pays back within a week now: your buildings are at what their pipes and inputs carry.");
        }
        var gold = company.Gold;
        var done = new List<long>();
        // The pipe doors first: they read the flows as they are now, and an upgrade elsewhere changes them
        foreach (var part in plan.Parts.OrderBy(t => t.Door ? 0 : 1))
        {
            var result = part.Door
                ? ClimbPipe(companyId, part.Building)
                : Upgrade(companyId, part.Building, Math.Min(part.Levels, Building(part.Building) is { } b ? AffordableLevels(company, b) : 0));
            if (result.Success)
            {
                done.Add(part.Building);
            }
        }
        if (done.Count == 0)
        {
            return CommandResult.Fail("None of it could be bought now.");
        }
        company.LastGrowth = new GrowthPurchase { At = LastTick, Buildings = done };
        AddStat(company, "pressed:grow", 1);
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Done($"Grown: {done.Count} {(done.Count == 1 ? "building" : "buildings")} for {Gold(gold - company.Gold)}, about +{Gold(plan.Gain)}/min · Undo for an hour takes the levels back");
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
