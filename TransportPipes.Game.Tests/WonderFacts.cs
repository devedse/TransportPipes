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
        var storage = game.World.BuildingsOf(company.Id).FirstOrDefault(t => t.Type is "silo" or "storage")
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
            if (stage == stages - 1)
            {
                // The tiers are a share of each one's own Era output: Big delivers a fifth of it, Small a fiftieth
                double Worth(Dictionary<string, double> goods) => goods.Sum(t => t.Value * game.World.BasePrice(t.Key));
                var smallLast = Worth(needs.ToDictionary(t => t.Key, t => Math.Min(5, t.Value)));
                small.EraOutput = (wonder.Contributions.GetValueOrDefault(small.Id) + smallLast) * 50;
                big.EraOutput = (wonder.Contributions.GetValueOrDefault(big.Id) + Worth(needs)) * 5;
            }
            Stocked(game, small, needs.ToDictionary(t => t.Key, t => Math.Min(5, t.Value)));
            Assert.IsTrue(game.World.DeliverToWonder(small.Id, eraWonder: true).Success);
            Stocked(game, big, needs);
            Assert.IsTrue(game.World.DeliverToWonder(big.Id, eraWonder: true).Success);
        }

        Assert.AreEqual(2, game.World.Era);
        Assert.AreEqual("copperCathedral", game.World.EraWonder!.Type);
        Assert.IsTrue(big.Titles.Contains("The Great Dynamo · Diamond"));
        Assert.AreEqual(game.World.TierReward(big, WonderTier.Diamond), big.Gold, 1, "twelve hours of its own income");
        StringAssert.StartsWith(big.EraEnded, "Era 1 ended: the Great Dynamo stands, all 3 stages; your part: ");
        Assert.IsTrue(small.Titles.Any(t => t.EndsWith("Bronze")), "a little counts too");
        Assert.IsTrue(game.World.ActiveEvents(big.District).Any(t => t.Kind == EventKind.EraLaunch), "a launch party");

        game.World.AdvanceEra();
        Assert.AreEqual(1, game.World.ActiveEvents(big.District).Count(t => t.Kind == EventKind.EraLaunch), "one party at a time");
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

    /// <summary>Ana in the Capital, her Wind Moilies and Iron Mine piped to her Yard gate; the Era Wonder's stage wants iron ore</summary>
    private static (TestGame Game, Company Ana, Wonder Wonder) CapitalMiner()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var ana = game.NewCompany("Ana", level: 3);
        Assert.AreEqual(GameWorld.Capital, ana.District);
        foreach (var _ in Enumerable.Range(0, 3))
        {
            game.ConnectToGate(ana, game.Build(ana, "windMoily"));
        }
        var mine = game.Build(ana, "ironMine", game.Spot(ana, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(ana, mine);
        game.Pass(1);
        var wonder = game.World.EraWonder!;
        wonder.Required = new Dictionary<string, double> { ["ironOre"] = 400 };
        return (game, ana, wonder);
    }

    [TestMethod]
    public void TheEraWonderBuysTheCapitalsSpareAtBase()
    {
        // Run 3: the Great Dynamo sat at 0 of 1,800 bricks while Cleo's bricks went to the hub behind a full Storage
        var (game, ana, wonder) = CapitalMiner();
        var gold = ana.Gold;

        Minutes(game, 30);

        var delivered = wonder.Delivered.GetValueOrDefault("ironOre");
        Assert.IsGreaterThan(10, delivered, "her ore goes in by pipe");
        Assert.AreEqual(delivered * game.World.BasePrice("ironOre"), wonder.Contributions[ana.Id], 1e-6, "her part, at base");
        Assert.AreEqual(delivered * game.World.BasePrice("ironOre"), ana.Stats.GetValueOrDefault("wonderGold"), 1e-6, "and she is paid for it");
        Assert.IsGreaterThan(gold, ana.Gold);
    }

    [TestMethod]
    public void OrdersBringHalfAtMostAndCountAQuarter()
    {
        var (game, ana, wonder) = CapitalMiner();
        var offer = game.World.WonderOrderOffer("ironOre")!.Value;
        Assert.AreEqual(200, offer.Units, 1, "half of it comes only by pipe");
        var gold = ana.Gold;

        Assert.IsTrue(game.World.OrderForWonder(ana.Id, "ironOre").Success);

        Assert.AreEqual(gold - offer.Cost, ana.Gold, 1e-6);
        Assert.IsNull(game.World.WonderOrderOffer("ironOre"), "nothing more to order");
        Minutes(game, OrderMinutes);
        Assert.AreEqual(1, wonder.Stage, "the hub shipped its half in six hours, her pipe the other: the stage stands");
        Assert.AreEqual(200 * game.World.BasePrice("ironOre") * 1.25, wonder.Contributions[ana.Id], 1, "piped goods count whole, ordered a quarter; the pipe stopped at what it lacked");
    }

    private const int OrderMinutes = (int)GameWorld.OrderHours * 60 + 5;

    /// <summary>A Yard round a minute: what was matched in one arrives in the next</summary>
    private static void Minutes(TestGame game, int minutes)
    {
        for (var i = 0; i < minutes; i++)
        {
            game.Pass(1);
        }
    }

    [TestMethod]
    public void AnOrderedGoodCountsAQuarterForTheTier()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var ana = game.NewCompany("Ana", level: 3);
        Assert.AreEqual(GameWorld.Capital, ana.District);
        game.Pass(1);
        var wonder = game.World.EraWonder!;
        wonder.Required = new Dictionary<string, double> { ["ironOre"] = 400 };

        game.World.OrderForWonder(ana.Id, "ironOre");
        Minutes(game, OrderMinutes);

        Assert.AreEqual(200, wonder.Delivered["ironOre"], 1e-6);
        Assert.AreEqual(200 * game.World.BasePrice("ironOre") * 0.25, wonder.Contributions[ana.Id], 1e-6, "bought goods count a quarter");
        Assert.AreEqual(0, wonder.Stage, "the other half only comes by pipe");
    }

    [TestMethod]
    public void OnlyTheCapitalOrdersForItsWonder()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var ana = game.NewCompany("Ana");
        game.Pass(1);
        ana.District = new DistrictCoord(1, 0);

        StringAssert.Contains(game.World.OrderForWonder(ana.Id, game.World.EraWonder!.Required.Keys.First()).Error, "Capital");
    }

    [TestMethod]
    public void AStageThatStandsLiftsTheCapitalsPipesForTheEra()
    {
        var (game, ana, wonder) = CapitalMiner();
        Assert.AreEqual(0, game.World.WonderPipeBonus(ana.Id));
        var storage = Stocked(game, ana, wonder.Required);

        Assert.IsTrue(game.World.DeliverToWonder(ana.Id, eraWonder: true).Success);

        Assert.AreEqual(1, wonder.Stage);
        Assert.AreEqual(0.5, game.World.WonderPipeBonus(ana.Id), "its pipes carry half as much again");
        game.World.AdvanceEra();
        Assert.AreEqual(0, game.World.WonderPipeBonus(ana.Id), "until the Era ends");
        StringAssert.StartsWith(ana.EraEnded, "Era 1 ended: the Great Dynamo got 1 of its 3 stages; your part: ");
    }
}
