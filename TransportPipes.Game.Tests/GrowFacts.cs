using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>
/// Your gold, in one tap (after run 4: Ben came back to 13M and spent 3.4k one level at a time; the advisor's bots,
/// pressing every step, held 2.7 days of income where people held 24): every pipe door and Max that pays back within a
/// week, the best first, as far as the gold goes, with an hour's Undo
/// </summary>
[TestClass]
public class GrowFacts
{
    /// <summary>A Wind Moily at a full grey pipe (with copper ingots in a Storage) and a Solar Panel that Max pays for</summary>
    private static (TestGame Game, Company Company, Building Moily, Building Panel) Estate()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 2_000_000, level: 12);
        var map = game.Map(company, game.Local(company, 1, 1), "W+++T\n    +\nS+++T", pipeTier: 1);
        game.World.UpgradeMany(company.Id, [map[(0, 0)].Id], 14);
        game.World.UpgradeMany(company.Id, [map[(4, 0)].Id, map[(4, 2)].Id], 9);
        game.Build(company, "storage").Contents["copperIngot"] = 100;
        game.Pass(2);
        return (game, company, map[(0, 0)], map[(0, 2)]);
    }

    [TestMethod]
    public void GrowEverythingBuysEachDoorAndMaxThatPaysWithinAWeek()
    {
        var (game, company, moily, panel) = Estate();
        var plan = game.World.PlanGrowth(company);
        Assert.IsTrue(plan.Parts.Any(t => t.Building == moily.Id && t.Door), "the moily's pipe door");
        Assert.IsTrue(plan.Parts.Any(t => t.Building == panel.Id && !t.Door), "the panel's Max");
        Assert.IsTrue(plan.Parts.All(t => t.Cost / t.Gain <= GameWorld.GrowPaybackMinutes), "each part back within a week");
        var gold = company.Gold;
        var before = game.World.GoldPerMinute()[company.Id];

        var result = game.World.GrowEverything(company.Id);

        Assert.IsTrue(result.Success, result.Error);
        StringAssert.StartsWith(result.Note, $"Grown: {plan.Parts.Count} buildings", result.Note);
        Assert.AreEqual(plan.Cost, gold - company.Gold, plan.Cost * 0.05, "what it said");
        game.Pass(2);
        Assert.AreEqual(before + plan.Gain, game.World.GoldPerMinute()[company.Id], plan.Gain * 0.25, "about what it said: the parts are solved one by one");

        // An hour's Undo: the levels back, less what they earned
        var levels = (moily.Level, panel.Level);
        var undo = game.World.UndoGrowth(company.Id);
        Assert.IsTrue(undo.Success, undo.Error);
        Assert.IsLessThan(levels.Item2, panel.Level, "its levels back");
        Assert.IsGreaterThan(gold - plan.Cost, company.Gold, "gold back");
        Assert.IsFalse(game.World.UndoGrowth(company.Id).Success, "once");
    }

    [TestMethod]
    public void WithNothingThatPaysItSaysSo()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 0, level: 12);
        game.Pass(1);

        Assert.IsEmpty(game.World.PlanGrowth(company).Parts);
        StringAssert.StartsWith(game.World.GrowEverything(company.Id).Error, "Nothing pays back within a week now");
    }

    [TestMethod]
    public void ABankOfADaysIncomeIsOfferedTheEraWonder()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.WonderBuysEnabled = true;
        var company = game.NewCompany(gold: 10_000_000, level: 12);
        game.Pass(1);
        game.World.EraWonder!.Required = new Dictionary<string, double> { ["bricks"] = 1_000 };

        var step = game.World.NextSteps(company, 8).FirstOrDefault(t => t.Kind == AdvisorKind.Shared);

        Assert.IsNotNull(step, string.Join(" | ", game.World.NextSteps(company, 8).Select(t => t.Title)));
        Assert.AreEqual(AdvisorAction.Wonder, step.Action);
        StringAssert.StartsWith(step.Title, "Give to The Great Dynamo: it wants 1,000 bricks");
        StringAssert.StartsWith(step.Reason, "Nobody here makes bricks, so all of it may be ordered");
    }
}
