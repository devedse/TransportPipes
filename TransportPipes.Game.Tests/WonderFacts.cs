using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>Per-town Eras, their Wonders and the Yards' Monuments (1.4 §11, §13)</summary>
[TestClass]
public class WonderFacts
{
    /// <summary>A company whose storage holds plenty of everything the stage still lacks</summary>
    private static void Stock(TestGame game, Company company, TownWonder wonder)
    {
        if (!game.World.BuildingsOf(company.Id).Any(t => t.Type == "storage"))
        {
            game.Build(company, "storage");
        }
        foreach (var good in wonder.Required.Keys)
        {
            game.World.BuildingsOf(company.Id).First(t => t.Type == "storage").Contents[good] = game.World.WonderLacks(wonder, good);
        }
    }

    [TestMethod]
    public void EveryTownHasItsOwnEraAndAWonderOfAFixedSize()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var wonder = game.World.WonderOf(company.District)!;

        Assert.AreEqual(1, game.World.TownEra(company.District));
        Assert.AreEqual("greatDynamo", wonder.Type);
        Assert.AreEqual(0, wonder.Stage);
        var worth = wonder.Required.Sum(t => t.Value * game.World.BasePrice(t.Key));
        Assert.AreEqual(4.3e9 * 0.1667, worth, 4.3e9 * 0.001, "a sixth of 4.3 billion gold's worth, however many play there");
        CollectionAssert.AreEquivalent(new[] { "bricks", "planks", "ironIngot" }, wonder.Required.Keys.ToList());
        Assert.AreEqual(1, game.World.TownEra(new DistrictCoord(5, 5)), "another town has its own Era");
    }

    [TestMethod]
    public void TownsThatFinishedItBeforeMakeItSmallerDownToHalf()
    {
        var game = new TestGame();
        var definition = game.World.Content.EraWonders[0];
        game.World.WondersFinished[definition.Id] = 3;
        var town = game.World.TownAt(new DistrictCoord(3, 0));
        var worth = town.Wonder!.Required.Sum(t => t.Value * game.World.BasePrice(t.Key));

        Assert.AreEqual(4.3e9 * 0.1667 * 0.7, worth, 4.3e9 * 0.001);
        game.World.WondersFinished[definition.Id] = 9;
        Assert.AreEqual(0.5, game.World.TrailFactor(definition));
    }

    [TestMethod]
    public void GivingFromStoragesCountsAsYourPartAndPaysNothing()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var wonder = game.World.WonderOf(company.District)!;
        Stock(game, company, wonder);
        var gold = company.Gold;

        Assert.IsTrue(game.World.GiveFromStorages(company.Id).Success);

        Assert.AreEqual(gold, company.Gold, "a gift: no gold comes back");
        Assert.AreEqual(wonder.Required.Sum(t => t.Value * game.World.BasePrice(t.Key)), wonder.Given[company.Id], 1);
        Assert.IsNotNull(wonder.FullAt, "the stage has all its goods");
        StringAssert.Contains(game.World.GiveFromStorages(company.Id).Error, "being built");
    }

    [TestMethod]
    public void BuyAndGiveBuysAtTheMarketsTwoAndAHalfTimes()
    {
        var game = new TestGame();
        var company = game.NewCompany(gold: 1e10);
        var wonder = game.World.WonderOf(company.District)!;
        var cost = game.World.BuyAndGiveCost(company, "planks");
        Assert.AreEqual(wonder.Required["planks"] * 3.5 * 2.5, cost, 1);
        var gold = company.Gold;

        Assert.IsTrue(game.World.BuyAndGive(company.Id, "planks", 1_000_000).Success);
        Assert.AreEqual(gold - 1_000_000, company.Gold, 1, "as much as the gold buys");
        Assert.AreEqual(1_000_000 / (3.5 * 2.5), wonder.Delivered["planks"], 1);

        Assert.IsTrue(game.World.BuyAndGive(company.Id).Success);
        Assert.IsNotNull(wonder.FullAt, "every good, all of it");
    }

    [TestMethod]
    public void AFullStageStandsADayLaterWithItsPerkAndItsGiversRewards()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1e10);
        var moily = game.Build(company, "windMoily");
        var wind = game.World.ToFlowNode(moily)!.Out["energy"];
        var wonder = game.World.WonderOf(company.District)!;
        company.MeasuredIncome = 1000;
        Assert.IsTrue(game.World.BuyAndGive(company.Id).Success);

        game.Pass(23 * 60);
        Assert.AreEqual(0, wonder.Stage, "a stage takes at least a day to build once it's full");
        var gold = company.Gold;
        game.Pass(2 * 60);

        Assert.AreEqual(1, wonder.Stage);
        Assert.IsNull(wonder.FullAt);
        Assert.AreEqual(wind * 1.05, game.World.ToFlowNode(moily)!.Out["energy"], 1e-9, "the Great Dynamo's first stage: +5% energy for the town");
        Assert.AreEqual(12 * 60 * company.MeasuredIncome, company.Gold - gold, 60 * company.MeasuredIncome, "Diamond: twelve hours of your income");
        Assert.Contains("The Great Dynamo · Diamond", company.Titles);
        Assert.AreEqual(0.3333, wonder.Required.Sum(t => t.Value * game.World.BasePrice(t.Key)) / 4.3e9, 0.001, "the second stage: a third");
    }

    [TestMethod]
    public void ADiamondBuilderGetsAStatueInTheHubAndOneMoreDiamondOnItEachTime()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1e11);
        var town = game.World.TownAt(company.District);
        company.MeasuredIncome = 1000;

        Assert.IsTrue(game.World.BuyAndGive(company.Id).Success);
        game.Pass(25 * 60);
        Assert.AreEqual(1, town.Statues.Single(t => t.Company == company.Id).Diamonds);

        Assert.IsTrue(game.World.BuyAndGive(company.Id).Success);
        game.Pass(25 * 60);
        var statue = town.Statues.Single();
        Assert.AreEqual(2, statue.Diamonds, "one statue a builder: its Diamonds count");
        Assert.AreEqual(company.Name, statue.Name);
    }

    [TestMethod]
    public void TheLastStageBringsTheNextEraToItsTownOnly()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1e11);
        var other = game.World.TownAt(new DistrictCoord(4, 0));
        var wonder = game.World.WonderOf(company.District)!;
        wonder.Stage = 2;
        wonder.Required = new Dictionary<string, double> { ["bricks"] = 100 };
        Assert.IsTrue(game.World.BuyAndGive(company.Id).Success);
        var moily = game.Build(company, "windMoily");
        var wind = game.World.ToFlowNode(moily)!.Out["energy"];

        game.Pass(25 * 60);

        Assert.AreEqual(2, game.World.TownEra(company.District));
        Assert.AreEqual(1, other.Era);
        Assert.AreEqual(2, company.Era, "your Era: the highest your town reached while you lived there");
        Assert.AreEqual("copperCathedral", game.World.WonderOf(company.District)!.Type);
        Assert.AreEqual(1, game.World.WondersFinished["greatDynamo"]);
        Assert.AreEqual(wind * (1 + 0.05 + 0.05 + 0.1) / (1 + 0.05 + 0.05), game.World.ToFlowNode(moily)!.Out["energy"], 1e-9, "a launch party: +10% for a day");
        game.Pass(24 * 60);
        Assert.AreEqual(wind, game.World.ToFlowNode(moily)!.Out["energy"], 1e-9, "the party is over (the perks were set by hand above)");
    }

    [TestMethod]
    public void TheSpaceElevatorLaunchesThenMissionsComeWithoutEnd()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1e18);
        game.TownToEra(5);
        var town = game.World.TownAt(company.District);
        var wonder = town.Wonder!;
        wonder.Stage = 2;
        wonder.Required = new Dictionary<string, double> { ["steel"] = 100 };
        Assert.IsTrue(game.World.BuyAndGive(company.Id).Success);
        game.World.TakeChanges();

        game.Pass(25 * 60);

        Assert.IsNotNull(town.LaunchedAt, "it launched");
        Assert.IsTrue(company.Titles.Contains("Launched the Space Elevator"));
        Assert.IsTrue(GameWorld.IsMission(town.Wonder!), "the missions begin");
        var first = game.World.WonderDefinition(town.Wonder!);
        Assert.AreEqual("Mission 1", first.Name);
        var legacy = game.World.TakeChanges().Legacy.Single();
        Assert.AreEqual((company.UserId, GameWorld.LaunchLegacy + GameWorld.LaunchGiverLegacy), (legacy.UserId, legacy.Legacy), "Legacy for its account, more for a giver");

        // A mission done: a star, +1% production for good, and the next one 10% bigger
        town.Wonder!.Required = new Dictionary<string, double> { ["spaceframes"] = 10 };
        Assert.IsTrue(game.World.BuyAndGive(company.Id).Success);
        game.Pass(25 * 60);

        Assert.AreEqual(1, town.Missions);
        Assert.AreEqual(0.05 + 0.05 + 0.01, game.World.TownPerk(company.District, WonderPerk.Everything), 1e-9, "the Space Elevator's two perks and a mission's");
        Assert.AreEqual("Mission 2", game.World.WonderDefinition(town.Wonder!).Name);
        Assert.AreEqual(first.Worth * 1.1, game.World.WonderDefinition(town.Wonder!).Worth, first.Worth * 1e-9);
    }

    [TestMethod]
    public void ANewEraMakesTheOlderGoodsHubOrdersBiggerAndOpensItsGoods()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var order = game.World.HubOrder(company, "planks");
        Assert.AreEqual(0, game.World.HubOrder(company, "steel"), "the market doesn't buy Era-2 goods yet");

        game.World.AdvanceEra(company.District);

        Assert.AreEqual(order * 1.5, game.World.HubOrder(company, "planks"), 1e-6, "half as much again for every Era since");
        Assert.AreEqual(4000 / 60.0, game.World.HubOrder(company, "steel"), 1e-6, "an Era-2 good: 4,000 gold's worth a minute");
    }

    [TestMethod]
    public void ABuildingOfALaterEraSleepsUntilItsTownGetsThere()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        var pos = game.Spot(company, "wireMill");
        var mill = new Building { Id = game.World.NextBuildingId, Type = "wireMill", Owner = company.Id, X = pos.X, Y = pos.Y, Size = 1 };
        game.World.LoadBuilding(mill);

        Assert.IsTrue(game.World.Sleeps(mill));
        Assert.IsNull(game.World.ToFlowNode(mill), "it makes and uses nothing");
        Assert.AreEqual(IdleCause.Sleeping, game.World.IdleReasonOf(mill).Cause);

        game.World.AdvanceEra(company.District);
        Assert.IsFalse(game.World.Sleeps(mill));
        Assert.IsNotNull(game.World.ToFlowNode(mill));
    }

    [TestMethod]
    public void TheGiveSwitchSendsTheSpareBeyondTheHubOrderOrAllOfIt()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.WonderBuysEnabled = true;
        var ana = game.NewCompany("Ana", level: 3);
        foreach (var _ in Enumerable.Range(0, 3))
        {
            game.ConnectToGate(ana, game.Build(ana, "windMoily"));
        }
        game.ConnectToGate(ana, game.Build(ana, "ironMine", game.Spot(ana, "ironMine", t => game.World.Tile(t).Deposit == "ironOre")));
        var wonder = game.World.WonderOf(ana.District)!;
        wonder.Required = new Dictionary<string, double> { ["ironOre"] = 400 };
        game.Pass(30);
        Assert.AreEqual(0, wonder.Delivered.GetValueOrDefault("ironOre"), "nothing goes without the switch");

        Assert.IsTrue(game.World.SetGive(ana.Id, "ironOre", GiveMode.BeyondHubOrder).Success);
        game.Pass(30);
        Assert.AreEqual(0, wonder.Delivered.GetValueOrDefault("ironOre"), "a mine makes less than its Hub Order: nothing beyond it");

        Assert.IsTrue(game.World.SetGive(ana.Id, "ironOre", GiveMode.All).Success);
        var gold = ana.Gold;
        for (var i = 0; i < 30; i++)
        {
            game.Pass(1);
        }
        var given = wonder.Delivered.GetValueOrDefault("ironOre");
        Assert.IsGreaterThan(10, given, "her ore goes in by pipe");
        Assert.AreEqual(given * game.World.BasePrice("ironOre"), wonder.Given[ana.Id], 1e-6, "her part, at base");
    }

    [TestMethod]
    public void YouMoveOnlyToTownsOfYourEraOrOneBelow()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var newer = MovingFacts.OtherDistrict(game, company);
        game.World.AdvanceEra(newer);

        StringAssert.Contains(game.World.Relocate(company.Id, newer).Error, "your Era (1) or one below");

        game.World.AdvanceEra(company.District);
        Assert.AreEqual(2, company.Era, "your Era: your town's");
        Assert.IsNull(game.World.EraMoveBlocker(company, newer), "a town of your Era");
        var older = new DistrictCoord(9, 9);
        Assert.AreEqual(1, game.World.TownEra(older));
        Assert.IsNull(game.World.EraMoveBlocker(company, older), "or one below: your Era-2 buildings sleep there");
        game.World.AdvanceEra(company.District);
        StringAssert.Contains(game.World.EraMoveBlocker(company, older), "your Era (3) or one below");
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

    // ---------------------------------------------------------------- Monuments

    [TestMethod]
    public void AYardBuildsAMonumentWithGoldInStagesWithoutEnd()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", gold: 1e10, level: 5);
        var crew = game.World.CrewOf(ana)!;
        var sites = game.World.MonumentsOf(crew);
        Assert.HasCount(1, sites, "one site per Era of the town");

        StringAssert.Contains(game.World.PayMonument(ana.Id, 1, 1e8).Error, "Choose");
        Assert.IsTrue(game.World.ChooseMonument(ana.Id, 1, "grandFountain").Success);
        var moily = game.Build(ana, "windMoily");
        var wind = game.World.ToFlowNode(moily)!.Out["energy"];

        Assert.IsTrue(game.World.PayMonument(ana.Id, 1, 100_000_000 + 140_000_000).Success);

        Assert.AreEqual(2, sites[0].Stages, "100 million, then 40% more");
        Assert.AreEqual(196_000_000, game.World.MonumentStageCost(sites[0]), 1);
        Assert.AreEqual(wind * 1.06, game.World.ToFlowNode(moily)!.Out["energy"], 1e-9, "the Grand Fountain: +3% a stage");
        game.World.AdvanceEra(ana.District);
        Assert.HasCount(2, game.World.MonumentsOf(crew), "a new Era, a new site");
        Assert.AreEqual(1e9, game.World.MonumentStageCost(game.World.MonumentsOf(crew)[1]), 1, "ten times higher a later Era");
        StringAssert.Contains(game.World.ChooseMonument(ana.Id, 2, "grandFountain").Error, "already");
    }

    [TestMethod]
    public void EveryResidentGetsTheMonumentsPerks()
    {
        var game = new TestGame();
        var ana = game.NewCompany("Ana", gold: 1e10, level: 5, slot: 0);
        var ben = game.NewCompany("Ben", gold: 1e10, level: 5, slot: 1);
        Assert.AreEqual(game.World.CrewOf(ana)?.Id, game.World.CrewOf(ben)?.Id, "one Yard, one crew");
        var cost = game.World.BuildCost(ben, game.World.Content.Building("windMoily"));

        Assert.IsTrue(game.World.ChooseMonument(ana.Id, 1, "workshop").Success);
        Assert.IsTrue(game.World.PayMonument(ana.Id, 1, 100_000_000).Success);

        Assert.AreEqual(Math.Round(cost * 0.97), game.World.BuildCost(ben, game.World.Content.Building("windMoily")), 1, "Ana paid, Ben's buildings cost 3% less too");
    }

    [TestMethod]
    public void ACappedMonumentStopsAtTenStages()
    {
        var game = new TestGame();
        var ana = game.NewCompany("Ana", gold: 1e13, level: 5);
        var crew = game.World.CrewOf(ana)!;
        Assert.IsTrue(game.World.ChooseMonument(ana.Id, 1, "observatory").Success);
        var gold = ana.Gold;

        Assert.IsTrue(game.World.PayMonument(ana.Id, 1, 1e12).Success);

        var site = game.World.MonumentsOf(crew)[0];
        Assert.AreEqual(10, site.Stages);
        var ten = Enumerable.Range(1, 10).Sum(n => game.World.Content.Monuments.StageCost(1, n));
        Assert.AreEqual(gold - ten, ana.Gold, 1, "what's over comes back");
        StringAssert.Contains(game.World.PayMonument(ana.Id, 1, 1e9).Error, "all it can");
    }

    [TestMethod]
    public void AMonumentNeedsCompanyLevelFive()
    {
        var game = new TestGame();
        var ana = game.NewCompany("Ana", gold: 1e10, level: 4);
        Assert.IsTrue(game.World.ChooseMonument(ana.Id, 1, "clockTower").Success);

        StringAssert.Contains(game.World.PayMonument(ana.Id, 1, 1e8).Error, "Level 5");
    }
}
