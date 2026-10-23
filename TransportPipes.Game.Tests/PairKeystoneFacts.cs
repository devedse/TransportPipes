using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>
/// The two-friend keystone (after run 3, the design team's step 8): water was the keystone in all four stations and
/// needed nobody; from the Water Tower on, a good made of two friends' land goods is the keystone instead
/// </summary>
[TestClass]
public class PairKeystoneFacts
{
    /// <summary>Ana and Ben in one station, playing, at Level 6; Ana makes the goods in anaMakes, Ben those in benMakes</summary>
    private static (TestGame Game, Company Ana, Company Ben, StationProject Project) Station(string[] anaMakes, string[] benMakes, int level = 6)
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var ana = game.NewCompany("Ana", level: level);
        var ben = game.NewCompany("Ben", level: level);
        foreach (var (company, makes) in new[] { (ana, anaMakes), (ben, benMakes) })
        {
            game.ConnectToGate(company, game.Build(company, "windMoily"));
            company.MeasuredIncome = 100;
            var x = 2;
            foreach (var type in makes)
            {
                // Placed as they are (no deposit rule): what counts is that it makes the good
                game.World.LoadBuilding(new Building { Id = game.World.NextBuildingId, Type = type, Owner = company.Id, X = game.Local(company, x, 26).X, Y = game.Local(company, x, 26).Y });
                x += 3;
            }
        }
        game.Pass(3);
        ana.LastSeenAt = ben.LastSeenAt = game.World.Now;
        var station = game.EstateOf(ana).Station;
        var project = game.World.ProjectOf(station);
        project.DeliveredAt[ana.Id] = game.World.Now;
        project.DeliveredAt[ben.Id] = game.World.Now;
        return (game, ana, ben, project);
    }

    [TestMethod]
    public void TwoFriendsWhoEachMakeAnInputGetTheKeystoneTogether()
    {
        var (game, ana, ben, project) = Station(["quarry"], ["coalPit"]);
        game.World.TakeChanges();

        game.World.FinishStageNow(game.EstateOf(ana).Station);

        Assert.AreEqual(ProjectStage.WaterTower, project.Stage);
        Assert.AreEqual("bricks", project.Keystone, "stone and coal make bricks");
        CollectionAssert.AreEqual(new[] { ana.Id, ben.Id }, project.KeystonePair);
        Assert.AreEqual(3 * 60 * 10, project.Basket["bricks"], "three hours of one Brickworks");
        var events = game.World.TakeChanges().Events;
        Assert.IsTrue(events.Any(t => t.Company == ana.Id && t.Text == "The Water Tower wants bricks from your stone and Ben's coal: one of you builds a Brickworks 💛"),
            string.Join(" | ", events.Select(t => t.Text)));
        Assert.IsTrue(events.Any(t => t.Company == ben.Id && t.Text.StartsWith("The Water Tower wants bricks from your coal and Ana's stone")));
    }

    [TestMethod]
    public void ItStaysWhileThePairCanMakeItTogether()
    {
        // The replay of run 3's day 14 found it passed over at once: "nobody here can make it now" counted one maker's
        // own goods only
        var (game, ana, ben, project) = Station(["quarry"], ["coalPit"]);
        game.World.FinishStageNow(game.EstateOf(ana).Station);

        game.Pass(16);

        Assert.AreEqual("bricks", project.Keystone);
        Assert.IsEmpty(project.PassedKeystones);
        var makers = game.World.MakersOf(project, "bricks").Select(t => t.Maker.Id).ToList();
        CollectionAssert.AreEquivalent(new[] { ana.Id, ben.Id }, makers, "either can build the Brickworks");
    }

    [TestMethod]
    public void ItWaitsForAFriendAwayThreeDaysAndItsLineHoldsStill()
    {
        // Run 4: Ana's and Mia-B's pair was dropped "nobody here can make it now" the hour Mia-B's last session was three
        // days old, while both were away for the keystone's whole life; Mia's maker line flipped as people came and went
        var (game, ana, ben, project) = Station(["quarry"], ["coalPit"]);
        game.World.FinishStageNow(game.EstateOf(ana).Station);
        Assert.AreEqual("bricks", project.Keystone);

        ben.LastSeenAt = ben.SessionStartedAt = game.World.Now.AddDays(-4);
        game.Pass(30);

        Assert.AreEqual("bricks", project.Keystone, "Ben played this week: the pair waits for him");
        Assert.IsEmpty(project.PassedKeystones);
        CollectionAssert.AreEquivalent(new[] { ana.Id, ben.Id }, game.World.MakersOf(project, "bricks").Select(t => t.Maker.Id).ToList(), "the line names both still");
        Assert.IsNull(game.World.KeystonePassesAt(project), "its clock waits till Ben has seen it");

        ben.LastSeenAt = ben.SessionStartedAt = game.World.Now.AddDays(-8);
        game.Pass(30);
        Assert.AreNotEqual("bricks", project.Keystone, "a week away: the pair can't make it now");
    }

    [TestMethod]
    public void AFoundryHallChoosesItsKeystoneWhenItOpens()
    {
        // Run 4: Capital SE's Foundry Hall said "opens in 10 days" while its keystone was chosen and dropped twice
        var (game, ana, ben, project) = Station(["quarry"], ["coalPit"]);
        var station = game.EstateOf(ana).Station;
        while (project.Stage != ProjectStage.FoundryHall)
        {
            game.World.FinishStageNow(station);
        }

        Assert.IsNull(project.Keystone, "nothing is chosen before it opens");
        Assert.IsFalse(project.KeystoneChosen);
        game.Pass(13 * 24 * 60);
        Assert.IsNull(project.Keystone);

        game.Pass(24 * 60);
        foreach (var company in new[] { ana, ben })
        {
            company.LastSeenAt = company.SessionStartedAt = game.World.Now;
        }
        game.Pass(20);

        Assert.IsTrue(project.KeystoneChosen, "chosen when it opens, from who plays now");
        Assert.IsNotNull(project.Keystone);
        Assert.IsGreaterThanOrEqualTo(game.World.Now.AddMinutes(-40), GameWorld.KeystoneWantedSince(project), "its clock starts at the opening");
    }

    [TestMethod]
    public void NotWhenOneMakesBothInputs()
    {
        var (game, ana, _, project) = Station(["quarry", "coalPit"], []);

        game.World.FinishStageNow(game.EstateOf(ana).Station);

        Assert.AreNotEqual("bricks", project.Keystone, "she can make them alone: today's rule");
        Assert.IsEmpty(project.KeystonePair);
    }

    [TestMethod]
    public void NotWhenOneOfThemIsAway()
    {
        var (game, ana, ben, project) = Station(["quarry"], ["coalPit"]);
        ben.LastSeenAt = game.World.Now.AddDays(-5);

        game.World.FinishStageNow(game.EstateOf(ana).Station);

        Assert.IsEmpty(project.KeystonePair, "both play when the stage starts");
    }

    [TestMethod]
    public void NotBelowLevelSix()
    {
        var (game, ana, _, project) = Station(["quarry"], ["coalPit"], level: 5);

        game.World.FinishStageNow(game.EstateOf(ana).Station);

        Assert.IsEmpty(project.KeystonePair, "the Brickworks unlocks at Level 6");
    }

    [TestMethod]
    public void CopperIngotsWhenSomebodyMakesBricksAlready()
    {
        var (game, ana, ben, project) = Station(["copperMine", "brickworks"], ["coalPit"]);

        game.World.FinishStageNow(game.EstateOf(ana).Station);

        Assert.AreEqual("copperIngot", project.Keystone, "copper ore and coal make copper ingots");
        CollectionAssert.AreEqual(new[] { ana.Id, ben.Id }, project.KeystonePair);
    }

    [TestMethod]
    public void APairKeystonePassedOverDoesNotComeBack()
    {
        var (game, ana, _, project) = Station(["quarry"], ["coalPit"]);
        game.World.FinishStageNow(game.EstateOf(ana).Station);
        Assert.AreEqual("bricks", project.Keystone);
        project.KeystoneSince = game.World.Now.AddDays(-3);
        foreach (var id in project.KeystonePair)
        {
            project.KeystoneSeen[id] = game.World.Now.AddDays(-3);
        }

        game.Pass(16);

        Assert.Contains("bricks", project.PassedKeystones, "the 2-day pass-over stays");
        Assert.AreNotEqual("bricks", project.Keystone);
    }
}
