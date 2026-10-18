using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class SeasonFacts
{
    [TestMethod]
    public void AFranchiseTradesTheBusinessForShares()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 24);
        var map = game.Map(company, game.Local(company, 1, 1), " W\nT+");
        var moily = map[(1, 0)];
        company.LifetimeEarnings = 1_000_000;
        StringAssert.Contains(game.World.FranchiseBlocker(company), "Level 25");
        company.Level = 25;
        StringAssert.Contains(game.World.FranchiseBlocker(company), "Era 2");
        game.World.Era = 2;
        Assert.AreEqual(31, game.World.FranchiseShares(company), "the square root of a million, in thousands");

        Assert.IsTrue(game.World.StartFranchise(company.Id).Success);
        Assert.AreEqual(game.Time + GameWorld.FranchiseNotice, company.FranchiseAt);
        game.Pass(60);
        Assert.IsNotNull(game.World.Building(moily.Id), "nothing happens during the notice");

        game.Pass(24 * 60);

        Assert.IsNull(game.World.Building(moily.Id));
        Assert.IsGreaterThanOrEqualTo(31, company.Shares, "counted when it happens: the moily earned a little more meanwhile");
        Assert.AreEqual(1, company.Level);
        Assert.AreEqual(game.World.StartingGold, company.Gold, 1e-6);
        Assert.IsTrue(game.World.BuildingsOf(company.Id).Any(t => t.Type == "tradingPost"), "the trading post stays");
        Assert.AreEqual(0, game.World.FranchiseShares(company), "the next Shares come from new earnings");

        // Every Share is +1% production
        var again = game.Map(company, game.Local(company, 4, 4), " W\nT+");
        var sale = game.World.NetworkOf(again[(1, 0)].Id)!.Solution.Flows.Single(t => t.Kind == FlowKind.Sale);
        Assert.AreEqual(game.World.Content.Building("windMoily").Out["energy"] * (1 + company.Shares / 100.0), sale.Sent, 1e-6);
    }

    [TestMethod]
    public void TheSpaceElevatorEndsTheSeasonWithLegacyForEveryone()
    {
        var game = new TestGame();
        var rich = game.NewCompany("Rich", gold: 1_000_000);
        var poor = game.NewCompany("Poor", gold: 1000);
        game.Pass(1);
        while (game.World.Era < GameWorld.LastEra)
        {
            game.World.AdvanceEra();
        }

        game.World.StartFinale();
        Assert.AreEqual(game.Time + GameWorld.FinaleCountdown, game.World.FinaleAt);
        Assert.IsTrue(game.World.Events.Any(t => t.Kind == EventKind.Finale));
        game.Pass(24 * 60 + 1);

        Assert.IsNotNull(game.World.SeasonEndedAt);
        var results = game.World.SeasonResults;
        Assert.AreEqual(rich.UserId, results[0].UserId);
        Assert.AreEqual("Season 1 Champion", results[0].Title);
        Assert.IsGreaterThan(results[1].Legacy, results[0].Legacy);
        Assert.IsGreaterThan(0, results[1].Legacy, "everyone gets some Legacy");
    }

    [TestMethod]
    public void LegacyGivesGoldTitlesAndAManagerSlot()
    {
        var game = new TestGame();
        var legacyLevel = GameWorld.LegacyLevelOf(400);
        Assert.AreEqual(6, legacyLevel);

        var veteran = game.World.CreateCompany(99, "Veteran", "#ffffff", legacyLevel, ["Season 1 Champion"]);

        Assert.AreEqual(game.World.StartingGold + 3000, veteran.Gold);
        Assert.IsTrue(veteran.Titles.Contains("Season 1 Champion"));
        Assert.AreEqual(1, game.World.ManagerSlots(veteran), "a manager slot from Legacy level 5, at Company Level 1");
    }
}
