using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class ResearchFacts
{
    [TestMethod]
    public void TheResearchContentIsComplete()
    {
        var content = TransportPipes.Game.Content.GameContent.Default;

        Assert.IsGreaterThanOrEqualTo(40, content.ResearchList.Count);
        foreach (var branch in Enum.GetValues<TransportPipes.Game.Content.ResearchBranch>())
        {
            Assert.IsGreaterThanOrEqualTo(3, content.ResearchList.Count(t => t.Era == 1 && t.Branch == branch), $"Era 1 needs {branch} techs");
        }
        // Every tech that a building or pipe needs exists (checked when loading), and every Era 1 tech can be reached
        foreach (var tech in content.ResearchList.Where(t => t.Era == 1))
        {
            Assert.IsTrue(tech.Requires.All(r => content.Research[r].Era <= tech.Era), tech.Id);
        }
    }

    [TestMethod]
    public void LabsTurnEnergyAndIngotsIntoResearchPoints()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 4);
        var lab = game.Build(company, "lab");
        var post = game.TradingPost(company);
        game.Connect(company, lab, post);
        game.World.SetImport(company.Id, post.Id, "energy", true);
        game.World.SetImport(company.Id, post.Id, "ironIngot", true);

        game.Pass(10);

        Assert.AreEqual(5 * 10, company.ResearchPoints, 1e-6, "5 research points per minute at level 1");
    }

    [TestMethod]
    public void ResearchCostsPointsTakesTimeAndThenGivesItsBonus()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");
        game.Connect(company, moily, game.TradingPost(company));
        game.World.SolveNetworks();
        var wind = 30 * game.World.LocationMultiplier(moily, game.World.Content.Building("windMoily"));
        Assert.AreEqual(wind, game.World.ToFlowNode(moily)!.Out["energy"], 1e-9);

        StringAssert.Contains(game.World.StartResearch(company.Id, "betterBearings").Error, "150 research points");
        company.ResearchPoints = 200;
        Assert.IsTrue(game.World.StartResearch(company.Id, "betterBearings").Success);
        Assert.AreEqual(50, company.ResearchPoints);
        StringAssert.Contains(game.World.StartResearch(company.Id, "solarTracking").Error, "busy");

        game.Pass(4);
        Assert.IsFalse(game.World.HasResearch(company, "betterBearings"));
        var goldBefore = company.Gold;
        game.Pass(2);

        Assert.IsTrue(game.World.HasResearch(company, "betterBearings"));
        Assert.IsNull(company.CurrentResearch);
        Assert.AreEqual(wind * 1.25, game.World.ToFlowNode(moily)!.Out["energy"], 1e-9);
        // Done after 5 minutes: 1 minute at the old output, 1 minute at the new one (minus the pipe's losses)
        var efficiency = game.World.NetworkOf(moily.Id)!.Links.Single(t => t.Connects(moily.Id, game.TradingPost(company).Id)).Efficiency;
        Assert.AreEqual(goldBefore + (wind + wind * 1.25) * efficiency, company.Gold, 1e-6);
    }

    [TestMethod]
    public void ATechNeedsItsPrerequisitesAndItsEra()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        company.ResearchPoints = 100_000;

        StringAssert.Contains(game.World.StartResearch(company.Id, "valves").Error, "Copper Pipes first");
        StringAssert.Contains(game.World.StartResearch(company.Id, "steelPipes").Error, "Era 2");
        StringAssert.Contains(game.World.StartResearch(company.Id, "deepLift").Error, "Company Level 30");
        Assert.IsTrue(game.World.StartResearch(company.Id, "copperPipes").Success);
        game.World.FinishResearch(company.Id);
        Assert.IsTrue(game.World.StartResearch(company.Id, "valves").Success);
    }

    [TestMethod]
    public void ResearchUnlocksBuildings()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 6);
        var hydro = game.World.Content.Building("hydroWheel");

        Assert.IsFalse(game.World.IsUnlocked(company, hydro, out var reason));
        StringAssert.Contains(reason, "research");
        company.Researched.Add("hydroWheel");
        Assert.IsTrue(game.World.IsUnlocked(company, hydro, out _));
    }

    [TestMethod]
    public void ResearchFinishesDuringACatchUp()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        company.ResearchPoints = 1000;
        game.World.StartResearch(company.Id, "copperPipes");

        game.Pass(60 * 24);

        Assert.IsTrue(game.World.HasResearch(company, "copperPipes"));
        Assert.IsTrue(game.World.TakeChanges().Events.Any(t => t.Kind == WorldEventKind.ResearchFinished));
    }

    [TestMethod]
    public void AfterTheWholeTreeMasteryGivesHalfAPercentPerStepForEver()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 40);
        company.ResearchPoints = 1_000_000;
        StringAssert.Contains(game.World.StartResearch(company.Id, GameWorld.MasteryId).Error, "whole research tree");
        company.Researched.UnionWith(game.World.Content.ResearchList.Select(t => t.Id));
        var cost = GameWorld.MasteryCost(company);

        Assert.IsTrue(game.World.StartResearch(company.Id, GameWorld.MasteryId).Success);
        Assert.AreEqual(1_000_000 - cost, company.ResearchPoints);
        game.World.FinishResearch(company.Id);

        Assert.AreEqual(1, company.Mastery);
        Assert.IsGreaterThan(cost, GameWorld.MasteryCost(company), "every step costs more");
        var moily = game.World.Content.Building("windMoily");
        var before = game.World.OutputBonus(company, moily, "energy");
        Assert.IsTrue(game.World.StartResearch(company.Id, GameWorld.MasteryId).Success);
        game.World.FinishResearch(company.Id);
        Assert.AreEqual(before + GameWorld.MasteryBonus, game.World.OutputBonus(company, moily, "energy"), 1e-9);
    }

    [TestMethod]
    public void TheQueueStartsEachTechByItselfInOrder()
    {
        // F11: a research queue of five
        var game = new TestGame();
        var company = game.NewCompany(level: 5);
        company.ResearchPoints = 600;
        Assert.IsTrue(game.World.QueueResearch(company.Id, "oreWashing", true).Success);
        Assert.AreEqual("oreWashing", company.CurrentResearch, "the Labs were free: it starts at once");
        Assert.IsTrue(game.World.QueueResearch(company.Id, "doubleFurnace", true).Success);
        Assert.IsTrue(game.World.QueueResearch(company.Id, "betterBearings", true).Success);
        CollectionAssert.AreEqual(new[] { "doubleFurnace", "betterBearings" }, company.ResearchQueue.ToArray());

        game.Pass(11);
        Assert.IsTrue(company.Researched.Contains("oreWashing"));
        Assert.AreEqual("doubleFurnace", company.CurrentResearch, "the next starts by itself, once its prerequisite is done");

        foreach (var tech in new[] { "solarTracking", "hydroWheel", "copperPipes", "valves" })
        {
            game.World.QueueResearch(company.Id, tech, true);
        }
        StringAssert.Contains(game.World.QueueResearch(company.Id, "bigFactory", true).Error, "5");
        Assert.IsTrue(game.World.QueueResearch(company.Id, "valves", false).Success);
        Assert.HasCount(4, company.ResearchQueue);
    }

    [TestMethod]
    public void ResearchKeepsGoingWhileThePlayerIsAway()
    {
        // Run 2: research points piled up to 64k while the queue sat empty; with nothing queued, the cheapest tech the
        // points cover starts by itself, once the player has been away half an hour (one who is here chooses)
        var game = new TestGame();
        var company = game.NewCompany(level: 5);
        company.ResearchPoints = 100_000;
        game.Build(company, "lab");
        game.Pass(10);
        Assert.IsNull(company.CurrentResearch, "the player is here: they choose");

        company.LastSeenAt = game.World.Now.AddHours(-1);
        game.Pass(10);

        Assert.IsNotNull(company.CurrentResearch, "away: the cheapest tech started");
        Assert.AreEqual(game.World.Content.ResearchList.Where(t => game.World.ResearchBlocker(company, t) == null || t.Id == company.CurrentResearch)
            .Min(t => game.World.ResearchCost(company, t)), game.World.ResearchCost(company, game.World.Content.Research[company.CurrentResearch!]), 1e-9);

        company.CurrentResearch = null;
        Assert.IsTrue(game.World.SetKeepResearching(company.Id, false).Success);
        game.Pass(10);
        Assert.IsNull(company.CurrentResearch, "switched off");
    }
}
