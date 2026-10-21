using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class SeasonFacts
{
    [TestMethod]
    public void AFranchiseCashesInSharesAndOpensANewEstateWithoutTearingAnythingDown()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 24);
        var map = game.Map(company, game.Local(company, 1, 1), " W\nT+");
        var moily = map[(1, 0)];
        company.LifetimeEarnings = 50_000;
        StringAssert.Contains(game.World.FranchiseBlocker(company), "Level 25");
        company.Level = 25;
        StringAssert.Contains(game.World.FranchiseBlocker(company), "Era 2");
        game.World.Era = 2;
        StringAssert.Contains(game.World.FranchiseBlocker(company), "7 of the 10 Shares");
        company.LifetimeEarnings = 1_000_000;
        Assert.AreEqual(31, game.World.FranchiseShares(company), "the square root of a million, in thousands");
        var gold = company.Gold;

        Assert.IsTrue(game.World.OpenFranchise(company.Id).Success);

        Assert.AreEqual(31, company.Shares);
        Assert.IsNotNull(game.World.Building(moily.Id), "nothing is torn down");
        Assert.AreEqual(25, company.Level);
        Assert.AreEqual(gold, company.Gold, 1e-6);
        Assert.AreEqual(0, game.World.FranchiseShares(company), "the next Shares come from new earnings");
        var estates = game.World.EstatesOf(company).ToList();
        Assert.HasCount(2, estates);
        var branch = estates[1];
        Assert.AreNotEqual(estates[0].District, branch.District, "in another district");
        Assert.AreEqual(game.World.Content.Progression.StartParcels.Count, branch.UnlockedCount, "it starts small");
        Assert.IsTrue(game.World.BuildingsOf(company.Id).Any(t => t.Type == "tradingPost" && branch.Contains(t.Position)), "with a trading post");
        Assert.AreSame(branch, game.World.OwnEstateAt(company, branch.Origin.Offset(16, 16)));

        // It is built on and grows like the first estate
        var spot = Enumerable.Range(0, 32 * 32).Select(i => branch.Origin.Offset(i % 32, i / 32))
            .First(t => game.World.CanPlace(company, game.World.Content.Building("windMoily"), t).Success);
        Assert.IsTrue(game.World.PlaceBuilding(company.Id, "windMoily", spot).Success);
        var parcel = Enumerable.Range(0, 16).First(t => !branch.IsUnlocked(t) && game.World.BuyParcel(company.Id, t, estateIndex: 1).Success);
        Assert.IsTrue(branch.IsUnlocked(parcel));

        // Every Share is +1% production
        var again = game.Map(company, game.Local(company, 4, 4), " W\nT+");
        var sale = game.World.NetworkOf(again[(1, 0)].Id)!.Solution.Flows.Single(t => t.Kind == FlowKind.Sale);
        Assert.AreEqual(game.World.Content.Building("windMoily").Out["energy"] * (1 + company.Shares / 100.0), sale.Sent, 1e-6);

        // Three estates at most
        company.LifetimeEarnings += 1_000_000;
        Assert.IsTrue(game.World.OpenFranchise(company.Id).Success);
        company.LifetimeEarnings += 1_000_000;
        StringAssert.Contains(game.World.FranchiseBlocker(company), "3 estates");
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
        Assert.IsTrue(rich.Titles.Contains("Season 1 Champion"), "the title goes to the company right away");

        // The next chapter: a new race, the same world
        var estate = game.EstateOf(rich);
        game.World.StartNextChapter();
        Assert.AreEqual(2, game.World.Season);
        Assert.IsNull(game.World.SeasonEndedAt);
        Assert.IsNull(game.World.FinaleAt);
        Assert.AreEqual(GameWorld.LastEra, game.World.Era, "the Eras stay unlocked");
        Assert.AreEqual("spaceElevator", game.World.EraWonder!.Type);
        Assert.AreEqual(0, game.World.EraWonder.Stage, "a fresh race");
        Assert.AreSame(estate, game.EstateOf(rich), "estates stay");
        Assert.AreEqual(1_000_000, rich.Gold, 1e-6);
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
