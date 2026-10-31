using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>
/// Your gold, in one tap (after run 4: Ben came back to 13M and spent 3.4k one level at a time; the advisor's bots,
/// pressing every step, held 2.7 days of income where people held 24): the next parcel first, as the model of 1.4 §17
/// buys it, then every pipe door and Max that pays back within five days, the best first, as far as the gold goes, with
/// an hour's Undo
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
        game.UpgradeNow(company, [map[(0, 0)].Id], 14);
        game.UpgradeNow(company, [map[(4, 0)].Id, map[(4, 2)].Id], 9);
        game.Build(company, "storage").Contents["copperIngot"] = 100;
        game.Pass(2);
        return (game, company, map[(0, 0)], map[(0, 2)]);
    }

    [TestMethod]
    public void GrowEverythingBuysTheNextParcelThenEachDoorAndMaxThatPaysWithinFiveDays()
    {
        var (game, company, moily, panel) = Estate();
        var parcels = game.EstateOf(company).UnlockedCount;
        var plan = game.World.PlanGrowth(company);
        Assert.IsNotNull(plan.Parts[0].Parcel, "land first");
        Assert.IsTrue(plan.Parts.Any(t => t.Building == moily.Id && t.Door), "the moily's pipe door");
        Assert.IsTrue(plan.Parts.Any(t => t.Building == panel.Id && !t.Door), "the panel's Max");
        Assert.IsTrue(plan.Parts.Where(t => t.Parcel == null).All(t => t.Cost / t.Gain <= GameWorld.GrowPaybackMinutes), "each level back within five days");
        var gold = company.Gold;
        var before = game.World.GoldPerMinute()[company.Id];

        var result = game.World.GrowEverything(company.Id);

        Assert.IsTrue(result.Success, result.Error);
        StringAssert.StartsWith(result.Note, $"Grown: a new parcel and {plan.Parts.Count - 1} buildings", result.Note);
        Assert.AreEqual(parcels + 1, game.EstateOf(company).UnlockedCount);
        Assert.AreEqual(plan.Cost, gold - company.Gold, 1, "the gold it said");
        // The goods of the levels from 11 come in (the market brings what nobody near makes within about ten minutes)
        game.Pass(12);
        var gained = game.World.GoldPerMinute()[company.Id] - before;
        Assert.AreEqual(gained, plan.Gain, Math.Max(0.5, gained * 0.01), $"what it said: +{plan.Gain:0.0}, brought +{gained:0.0}");

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
        StringAssert.StartsWith(game.World.GrowEverything(company.Id).Error, "Nothing pays back within five days now");
    }

    [TestMethod]
    public void GrowSavesForTheNextParcelWhileItIsAFewHoursAway()
    {
        var (game, company, _, _) = Estate();
        var (cost, _) = game.World.NextParcel(company);
        var income = game.World.GoldPerMinute()[company.Id];
        company.Gold = Math.Max(0, cost - income * 60);

        var plan = game.World.PlanGrowth(company);

        Assert.IsEmpty(plan.Parts, "the gold is kept for the land");
        StringAssert.StartsWith(plan.Saving, "Saving for your next parcel");
        StringAssert.StartsWith(game.World.GrowEverything(company.Id).Error, "Saving for your next parcel");

        // A parcel its level doesn't allow yet: nothing to save for, the levels that pay
        company.Level = 1;
        Assert.IsNull(game.World.PlanGrowth(company).Saving);
    }

    [TestMethod]
    public void ABankOfADaysIncomeIsOfferedTheTownsWonder()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.WonderBuysEnabled = true;
        var company = game.NewCompany(gold: 10_000_000, level: 12);
        game.Pass(1);
        game.World.WonderOf(company.District)!.Required = new Dictionary<string, double> { ["bricks"] = 1_000 };

        var step = game.World.NextSteps(company, 8).FirstOrDefault(t => t.Kind == AdvisorKind.Shared);

        Assert.IsNotNull(step, string.Join(" | ", game.World.NextSteps(company, 8).Select(t => t.Title)));
        Assert.AreEqual(AdvisorAction.Wonder, step.Action);
        StringAssert.StartsWith(step.Title, "Give to The Great Dynamo: its stage 1 wants 5,000 g of goods");
        StringAssert.StartsWith(step.Reason, "Buy and give: a day of your income buys 12.5k g of its goods");
    }

    [TestMethod]
    public void GrowSaysWhatItBringsTogether()
    {
        // Playtest: Grow promised +504 g/min and brought about +300. Forty moilies sell energy beyond their Hub Order:
        // ten of them levelled together sell more, and the market pays less for all of it; the two trading posts both
        // counted the Hub Orders only the higher one raises.
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 60_000, level: 6);
        var row = new string('W', 20);
        var map = game.Map(company, game.Local(company, 1, 1), row + "\n" + new string('+', 20) + "T\n" + row + "\nMMMM\n++++\nFFFF");
        foreach (var moily in map.Values.Where(t => t.Type == "windMoily").Take(10))
        {
            game.SetLevel(moily, 6);
        }
        game.Pass(300);
        var before = game.World.GoldPerMinute()[company.Id];
        var plan = game.World.PlanGrowth(company);
        Assert.IsTrue(plan.Parts.Count(t => game.World.Building(t.Building) is { Type: "tradingPost" }) <= 1, "one trading post's levels: the Hub Orders follow the highest");

        Assert.IsTrue(game.World.GrowEverything(company.Id).Success);
        company.Gold = 1e9;
        game.World.OrderAll(company.Id);
        game.Pass(300);

        var gained = game.World.GoldPerMinute()[company.Id] - before;
        Assert.AreEqual(gained, plan.Gain, Math.Max(0.5, gained * 0.01), $"it said +{plan.Gain:0.0} g/min and brought +{gained:0.0}");
        Assert.IsGreaterThan(0, plan.Goods, "and says what its levels' goods cost on top");
    }
}
