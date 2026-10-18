using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class WonderFacts
{
    /// <summary>A company whose storage holds plenty of everything a Wonder stage needs</summary>
    private static Building Stocked(TestGame game, Company company, IReadOnlyDictionary<string, double> needs)
    {
        var storage = game.World.BuildingsOf(company.Id).FirstOrDefault(t => t.Type == "silo")
            ?? game.Map(company, game.Local(company, 1, 1), "O").Values.Single();
        foreach (var (resource, amount) in needs)
        {
            storage.Contents[resource] = amount;
        }
        return storage;
    }

    [TestMethod]
    public void TheDistrictBuildsItsWonderAndGetsItsPerk()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var district = company.District;
        var wonder = game.World.DistrictWonder(district)!;
        var definition = game.World.WonderDefinition(wonder);
        Assert.AreEqual(3, definition.Stages.Count);

        StringAssert.Contains(game.World.DeliverToWonder(company.Id, eraWonder: false).Error, "nothing it needs");
        for (var stage = 0; stage < definition.Stages.Count; stage++)
        {
            var storage = Stocked(game, company, wonder.Required);
            storage.Contents[wonder.Required.Keys.First()] /= 2;
            Assert.IsTrue(game.World.DeliverToWonder(company.Id, eraWonder: false).Success);
            Assert.AreEqual(stage, wonder.Stage, "half of one resource is missing");
            Stocked(game, company, wonder.Required);
            Assert.IsTrue(game.World.DeliverToWonder(company.Id, eraWonder: false).Success);
            Assert.AreEqual(stage + 1, wonder.Stage);
        }

        Assert.IsTrue(wonder.Finished);
        Assert.IsGreaterThan(0, wonder.Contributions[company.Id]);
        Assert.IsGreaterThan(1, company.Level, "building a Wonder gives XP");
        StringAssert.Contains(game.World.DeliverToWonder(company.Id, eraWonder: false).Error, "finished");
    }

    [TestMethod]
    public void AFinishedClockTowerRaisesHubDemand()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var before = game.World.HubDemand(company.District, "energy");
        game.World.LoadWonders(TestGame.Start, null, [new Wonder { Type = "clockTower", District = company.District, Stage = 3, FinishedAt = TestGame.Start }]);

        Assert.AreEqual(before * 1.1, game.World.HubDemand(company.District, "energy"), 1e-6);
    }

    [TestMethod]
    public void TheCapitalAlsoBuildsTheEraWonder()
    {
        var game = new TestGame();
        game.Pass(1);

        Assert.IsNotNull(game.World.DistrictWonder(GameWorld.Capital));
        Assert.AreEqual("greatDynamo", game.World.EraWonder!.Type);
        Assert.IsTrue(game.World.Generator.HubStructures(GameWorld.Capital).Any(t => t.Type == "eraWonderSite"));
        Assert.IsFalse(game.World.Generator.HubStructures(new DistrictCoord(1, 0)).Any(t => t.Type == "eraWonderSite"));
    }

    [TestMethod]
    public void FinishingTheEraWonderStartsTheNextEraAndRewardsItsBuilders()
    {
        var game = new TestGame();
        var big = game.NewCompany("Big Builder", gold: 0);
        var small = game.NewCompany("Small Builder", gold: 0);
        game.Pass(1);
        var wonder = game.World.EraWonder!;
        var stages = game.World.WonderDefinition(wonder).Stages.Count;

        for (var stage = 0; stage < stages; stage++)
        {
            var needs = wonder.Required.ToDictionary(t => t.Key, t => t.Value);
            Stocked(game, small, needs.ToDictionary(t => t.Key, t => Math.Min(5, t.Value)));
            Assert.IsTrue(game.World.DeliverToWonder(small.Id, eraWonder: true).Success);
            Stocked(game, big, needs);
            Assert.IsTrue(game.World.DeliverToWonder(big.Id, eraWonder: true).Success);
        }

        Assert.AreEqual(2, game.World.Era);
        Assert.AreEqual("copperCathedral", game.World.EraWonder!.Type);
        Assert.IsTrue(big.Titles.Contains("The Great Dynamo · Diamond"));
        Assert.AreEqual(GameWorld.TierReward(WonderTier.Diamond, 1), big.Gold);
        Assert.IsTrue(small.Titles.Any(t => t.EndsWith("Bronze")), "a little counts too");
        Assert.IsTrue(game.World.ActiveEvents(big.District).Any(t => t.Kind == EventKind.EraLaunch), "a launch party");
    }

    [TestMethod]
    public void AnEraEndsOnItsDeadline()
    {
        var game = new TestGame();
        game.NewCompany();
        game.Pass(1);

        game.Pass(GameWorld.EraLength.TotalMinutes + 1);

        Assert.AreEqual(2, game.World.Era);
        Assert.AreEqual((TestGame.Start + GameWorld.EraLength).Date, game.World.EraStartedAt.Date);
    }

    [TestMethod]
    public void PowerLinesCarryOnlyEnergyAndLoseNothing()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        var map = game.Map(company, game.Local(company, 1, 1), string.Join("\n",
            " W",
            " +L      L+O",
            " S"));
        var storage = map[(11, 1)];
        var network = game.World.NetworkOf(storage.Id)!;
        Assert.AreEqual(network, game.World.NetworkOf(map[(1, 0)].Id), "the cable joins the networks");

        var stored = network.Solution.Flows.Where(t => t.Kind == FlowKind.ToStorage).ToList();
        Assert.IsNotEmpty(stored);
        Assert.IsTrue(stored.All(t => t.Resource == "energy"));
        Assert.AreEqual(stored.Sum(t => t.Sent), stored.Sum(t => t.Delivered), 1e-9, "no losses on the cable");

        // Out of reach: no connection
        var far = game.Map(company, game.Local(company, 1, 20), " W\n +L         L+O");
        Assert.AreNotEqual(game.World.NetworkOf(far[(1, 0)].Id), game.World.NetworkOf(far[(14, 1)].Id));
    }
}
