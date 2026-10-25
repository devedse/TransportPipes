using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class SeasonFacts
{
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
