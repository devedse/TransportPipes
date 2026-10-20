using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class GameWorldFacts
{
    [TestMethod]
    public void ANewCompanyGetsAnEstateWithHqTradingPostAndGates()
    {
        var game = new TestGame();
        var company = game.World.CreateCompany(1, "Pipe Dreams", "#33aaff");

        Assert.AreEqual(2000, company.Gold);
        Assert.AreEqual(1, company.Level);
        var estate = game.EstateOf(company);
        Assert.AreEqual(company.Id, estate.OwnerId);
        Assert.AreEqual(4, estate.UnlockedCount);
        Assert.IsTrue(new[] { 5, 6, 9, 10 }.All(estate.IsUnlocked));
        CollectionAssert.AreEquivalent(new[] { "hq", "tradingPost" }, game.World.BuildingsOf(company.Id).Select(t => t.Type).ToArray());
        Assert.AreEqual(4, game.World.Conductors.Count(t => t.Kind == ConductorKind.Gate && t.Owner == company.Id));
        Assert.AreEqual(0, company.Xp, "the free buildings give no XP");
    }

    [TestMethod]
    public void CompaniesFillADistrictBeforeTheNextOneOpens()
    {
        var game = new TestGame();
        var companies = Enumerable.Range(0, WorldLayout.EstatesPerDistrict + 1).Select(t => game.NewCompany($"Company {t}")).ToList();

        var first = companies[0].District;
        Assert.IsTrue(companies.Take(WorldLayout.EstatesPerDistrict).All(t => t.District == first));
        CollectionAssert.AreEquivalent(Enumerable.Range(0, WorldLayout.EstatesPerDistrict).ToArray(), companies.Take(WorldLayout.EstatesPerDistrict).Select(t => t.Slot).ToArray());
        Assert.AreNotEqual(first, companies[^1].District);
    }

    [TestMethod]
    public void YouCanOnlyBuildOnYourOwnUnlockedLand()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var neighbour = game.NewCompany("Neighbour");
        var hq = game.World.BuildingsOf(company.Id).Single(t => t.Type == "hq");

        StringAssert.Contains(game.World.PlaceBuilding(company.Id, "windMoily", game.Local(company, 2, 2)).Error, "locked");
        StringAssert.Contains(game.World.PlaceBuilding(company.Id, "windMoily", game.Local(neighbour, 12, 12)).Error, "own estate");
        StringAssert.Contains(game.World.PlaceBuilding(company.Id, "windMoily", game.Local(company, -1, 12)).Error, "own estate");
        StringAssert.Contains(game.World.PlaceBuilding(company.Id, "windMoily", hq.Position).Error, "already");
        Assert.IsTrue(game.World.PlaceBuilding(company.Id, "windMoily", game.Spot(company, "windMoily")).Success);
    }

    [TestMethod]
    public void BuildingCostsGoldGivesXpAndEveryCopyCostsMore()
    {
        var game = new TestGame();
        var company = game.NewCompany(gold: 2000);

        game.Build(company, "windMoily");
        Assert.AreEqual(1800, company.Gold);
        Assert.AreEqual(60, company.Xp, "10 for a building, 50 for the first of its type");

        game.Build(company, "windMoily");
        Assert.AreEqual(1800 - 214, company.Gold, "the second copy costs 7% more");
        Assert.AreEqual(70, company.Xp);
    }

    [TestMethod]
    public void BuildingsNeedTheirCompanyLevelAndEnoughGold()
    {
        var game = new TestGame();
        var company = game.NewCompany(gold: 100);
        var ironSpot = game.Spot(company, "windMoily", t => game.World.Tile(t).Deposit == "ironOre");

        StringAssert.Contains(game.World.PlaceBuilding(company.Id, "ironMine", ironSpot).Error, "Company Level 2");
        company.Level = 2;
        StringAssert.Contains(game.World.PlaceBuilding(company.Id, "ironMine", ironSpot).Error, "600 gold");
        company.Gold = 1000;
        Assert.IsTrue(game.World.PlaceBuilding(company.Id, "ironMine", ironSpot).Success);
        Assert.AreEqual(400, company.Gold);
    }

    [TestMethod]
    public void AMineNeedsADeposit()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 2);
        var plain = game.Spot(company, "windMoily", t => game.World.Tile(t).Deposit == null);

        StringAssert.Contains(game.World.PlaceBuilding(company.Id, "ironMine", plain).Error, "iron ore deposit");
    }

    [TestMethod]
    public void AWindMoilySellsItsEnergyThroughTheTradingPost()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");
        var post = game.TradingPost(company);
        var path = game.Connect(company, moily, post);
        var gold = company.Gold;

        game.Pass(10);

        var network = game.World.NetworkOf(moily.Id)!;
        Assert.AreSame(network, game.World.NetworkOf(post.Id));
        var sale = network.Solution.Flows.Single();
        Assert.AreEqual(FlowKind.Sale, sale.Kind);
        var wind = game.World.LocationMultiplier(moily, game.World.Content.Building("windMoily"));
        Assert.AreEqual(30 * wind, sale.Sent, 1e-9);
        Assert.AreEqual(30 * wind * Math.Pow(0.99, path.Count), sale.Delivered, 1e-9, "1% loss per grey pipe tile");
        Assert.AreEqual(gold + sale.Delivered * 1 * 10, company.Gold, 1e-6);
        Assert.AreEqual(sale.Delivered * 10, company.LifetimeEarnings, 1e-6);
        Assert.AreEqual(sale.Delivered * 10 / 25, company.Xp - 60, 1e-6, "one XP per 25 gold at Company Level 1");
    }

    [TestMethod]
    public void NothingIsProducedWhileABuildingIsUnderConstruction()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily", finish: false);
        game.Connect(company, moily, game.TradingPost(company));
        var gold = company.Gold;

        game.Pass(0.25);
        Assert.IsTrue(moily.UnderConstruction);
        Assert.AreEqual(gold, company.Gold);

        // Built after 30 seconds: from then on it produces
        game.Pass(1.25);
        Assert.IsFalse(moily.UnderConstruction);
        var perMinute = game.World.NetworkOf(moily.Id)!.Solution.GoldPerMinute[company.Id];
        Assert.AreEqual(gold + perMinute * 1, company.Gold, 1e-6);
        Assert.AreEqual(1, company.Report.BuildingsCompleted);
    }

    [TestMethod]
    public void AStorageFillsUpFirstAndThenTheSurplusIsSold()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");
        var storage = game.Build(company, "storage");
        var post = game.TradingPost(company);
        game.Connect(company, moily, storage);
        game.Connect(company, moily, post);
        var gold = company.Gold;

        game.Pass(10);
        Assert.AreEqual(gold, company.Gold, "everything goes into the storage first");
        Assert.IsGreaterThan(0, storage.Contents["energy"]);

        game.Pass(60 * 24);
        Assert.AreEqual(5000, storage.Contents["energy"], 1e-6);
        Assert.IsGreaterThan(gold, company.Gold);
        Assert.AreEqual(FlowKind.Sale, game.World.NetworkOf(moily.Id)!.Solution.Flows.Single().Kind);
    }

    [TestMethod]
    public void ACatchUpOfADayGivesTheSameResultAsTicksEveryMinute()
    {
        TestGame Setup()
        {
            var game = new TestGame();
            var company = game.NewCompany();
            var moily = game.Build(company, "windMoily");
            var storage = game.Build(company, "storage");
            game.Connect(company, moily, storage);
            game.Connect(company, moily, game.TradingPost(company));
            game.Build(company, "windMoily", finish: false);
            return game;
        }

        var once = Setup();
        once.Pass(60 * 6);
        var often = Setup();
        for (var minute = 0; minute < 60 * 6; minute++)
        {
            often.Pass(1);
        }

        var a = once.World.Companies.Single();
        var b = often.World.Companies.Single();
        Assert.AreEqual(b.Gold, a.Gold, 1e-6 * b.Gold);
        Assert.AreEqual(b.Xp, a.Xp, 1e-6 * b.Xp);
        var storageA = once.World.Buildings.Single(t => t.Type == "storage");
        var storageB = often.World.Buildings.Single(t => t.Type == "storage");
        Assert.AreEqual(storageB.StoredTotal, storageA.StoredTotal, 1e-6);
    }

    [TestMethod]
    public void UpgradingRaisesTheOutputAndMilestonesDoubleIt()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");
        game.Connect(company, moily, game.TradingPost(company));
        game.World.SolveNetworks();
        double Sent() => game.World.NetworkOf(moily.Id)!.Solution.Flows.Single().Sent;
        var level1 = Sent();
        var gold = company.Gold;

        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 1).Success);
        game.World.SolveNetworks();
        Assert.AreEqual(2, moily.Level);
        Assert.AreEqual(gold - 200, company.Gold);
        Assert.AreEqual(level1 * 1.1, Sent(), 1e-9);

        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 8).Success);
        game.World.SolveNetworks();
        Assert.AreEqual(10, moily.Level);
        Assert.AreEqual(level1 * Math.Pow(1.1, 9) * 2, game.World.ToFlowNode(moily)!.Out["energy"], 1e-9, "level 10 is a milestone");
        Assert.AreEqual(100, Sent(), 1e-9, "a grey pipe carries 100 per minute");
    }

    [TestMethod]
    public void ParcelsAreBoughtNextToYourLandWhenYourLevelIsHighEnough()
    {
        var game = new TestGame();
        var company = game.NewCompany(gold: 6000);

        StringAssert.Contains(game.World.BuyParcel(company.Id, 1).Error, "Company Level 5");
        company.Level = 5;
        StringAssert.Contains(game.World.BuyParcel(company.Id, 0).Error, "next to land you own");
        Assert.IsTrue(game.World.BuyParcel(company.Id, 1).Success);
        Assert.AreEqual(1000, company.Gold);
        Assert.IsTrue(game.EstateOf(company).IsUnlocked(1));
        Assert.AreEqual((10000, 7), game.World.NextParcel(company));
        Assert.IsTrue(game.World.PlaceBuilding(company.Id, "solarPanel", game.Local(company, 12, 2)).Success || game.World.Tile(game.Local(company, 12, 2)).Terrain is TerrainType.Water or TerrainType.Mountain);
    }

    [TestMethod]
    public void RemovingAPipeSplitsTheNetwork()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");
        var post = game.TradingPost(company);
        var path = game.Connect(company, moily, post);
        Assert.AreSame(game.World.NetworkOf(moily.Id), game.World.NetworkOf(post.Id));

        Assert.IsTrue(game.World.RemovePipes(company.Id, [path[path.Count / 2]]).Success);
        game.World.SolveNetworks();
        Assert.AreNotSame(game.World.NetworkOf(moily.Id), game.World.NetworkOf(post.Id));
        Assert.AreEqual(0, game.World.NetworkOf(moily.Id)?.Solution.Flows.Count ?? 0);
    }

    [TestMethod]
    public void DemolishingRefundsHalfAndTheHqStays()
    {
        var game = new TestGame();
        var company = game.NewCompany(gold: 2000);
        var moily = game.Build(company, "windMoily");

        Assert.IsTrue(game.World.Demolish(company.Id, moily.Id).Success);
        Assert.AreEqual(1900, company.Gold);
        Assert.IsNull(game.World.Building(moily.Id));
        Assert.IsFalse(game.World.Demolish(company.Id, game.World.BuildingsOf(company.Id).Single(t => t.Type == "hq").Id).Success);
    }

    [TestMethod]
    public void MovingABuildingKeepsItsLevel()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");
        game.World.Upgrade(company.Id, moily.Id, 4);
        var to = game.Spot(company, "windMoily", t => t != moily.Position);

        Assert.IsTrue(game.World.Move(company.Id, moily.Id, to).Success);
        Assert.AreEqual(to, moily.Position);
        Assert.AreEqual(5, moily.Level);
        Assert.AreSame(moily, game.World.BuildingAt(to));
    }

    /// <summary>Company A (slot 0) sells energy to company B (slot 1) through their gates and a corridor pipe</summary>
    private static (TestGame Game, Company A, Company B, Building Moily, Building Mine) Neighbours(bool mineToStorage = false)
    {
        var game = new TestGame();
        var a = game.NewCompany("A");
        var b = game.NewCompany("B", level: 2);
        Assert.AreEqual(a.Slot + 1, b.Slot);

        var moily = game.Build(a, "windMoily");
        game.Connect(a, moily, GateSide.Right);
        var rightGate = WorldLayout.GatePosition(a.District, a.Slot, GateSide.Right);
        Assert.IsTrue(game.World.PlacePipes(a.Id, [rightGate.Offset(1, 0), rightGate.Offset(2, 0)]).Success);
        Assert.AreEqual(WorldLayout.GatePosition(b.District, b.Slot, GateSide.Left), rightGate.Offset(3, 0));

        var mine = game.Build(b, "ironMine", game.Spot(b, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.Connect(b, mine, GateSide.Left);
        game.Connect(b, mine, mineToStorage ? game.Build(b, "storage") : game.TradingPost(b));
        game.World.SolveNetworks();
        return (game, a, b, moily, mine);
    }

    [TestMethod]
    public void NeighboursTradeThroughTheirGates()
    {
        var (game, a, b, moily, mine) = Neighbours();
        var goldA = a.Gold;

        game.Pass(10);

        var network = game.World.NetworkOf(moily.Id)!;
        Assert.AreSame(network, game.World.NetworkOf(mine.Id));
        var trade = network.Solution.Flows.Single(t => t.Kind == FlowKind.Trade);
        Assert.AreEqual(moily.Id, trade.From);
        Assert.AreEqual(mine.Id, trade.To);
        Assert.AreEqual(10, trade.Delivered, 1e-9, "the mine needs 10 energy");
        Assert.AreEqual(1.75, trade.PricePerUnit, "the automatic price is the middle of the fair band");
        Assert.AreEqual(goldA + 10 * 1.75 * 10, a.Gold, 1e-6);
        Assert.IsTrue(network.Solution.Flows.Any(t => t.Kind == FlowKind.Sale && t.From == mine.Id), "B sells the ore");
    }

    [TestMethod]
    public void AClosedGateStopsTheTrade()
    {
        var (game, a, b, moily, mine) = Neighbours();

        Assert.IsTrue(game.World.SetGatePolicy(b.Id, GateSide.Left, GatePolicy.Closed).Success);
        game.World.SolveNetworks();

        Assert.AreNotSame(game.World.NetworkOf(moily.Id), game.World.NetworkOf(mine.Id));
    }

    [TestMethod]
    public void ACompanyWithoutGoldCantBuyFromOthers()
    {
        var (game, a, b, moily, mine) = Neighbours(mineToStorage: true);
        b.Gold = 35;
        var goldA = a.Gold;

        game.Pass(10);

        Assert.AreEqual(0, b.Gold, 1e-6);
        Assert.AreEqual(goldA + 35, a.Gold, 1e-6, "B could pay for two minutes of energy");
        Assert.IsFalse(game.World.NetworkOf(moily.Id)!.Solution.Flows.Any(t => t.Kind == FlowKind.Trade));
    }

    [TestMethod]
    public void ATradingPostCanImportFromTheHub()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 2);
        var mine = game.Build(company, "ironMine", game.Spot(company, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        var post = game.TradingPost(company);
        game.Connect(company, mine, post);

        Assert.IsTrue(game.World.SetImport(company.Id, post.Id, "energy", true).Success);
        game.World.SolveNetworks();

        var flows = game.World.NetworkOf(mine.Id)!.Solution.Flows;
        var import = flows.Single(t => t.Kind == FlowKind.Import);
        Assert.AreEqual("energy", import.Resource);
        Assert.AreEqual(10, import.Delivered, 1e-9);
        Assert.AreEqual(2.5, import.PricePerUnit);
        Assert.IsTrue(flows.Any(t => t.Kind == FlowKind.Sale && t.Resource == "ironOre"));
    }

    [TestMethod]
    public void PipesCantGoOnSomeoneElsesEstate()
    {
        var game = new TestGame();
        var a = game.NewCompany("A");
        var b = game.NewCompany("B");

        StringAssert.Contains(game.World.PlacePipes(a.Id, [game.Local(b, 12, 12)]).Error, "someone else's estate");
        Assert.IsTrue(game.World.PlacePipes(a.Id, [game.Local(a, 2, 2)]).Success, "pipes may cross parcels you haven't bought");
    }

    [TestMethod]
    public void TheNetworkOfAnUntouchedAreaKeepsItsSolution()
    {
        var game = new TestGame();
        var a = game.NewCompany("A");
        var b = game.NewCompany("B");
        var moilyA = game.Build(a, "windMoily");
        game.Connect(a, moilyA, game.TradingPost(a));
        game.World.SolveNetworks();
        var networkA = game.World.NetworkOf(moilyA.Id);

        var moilyB = game.Build(b, "windMoily");
        game.Connect(b, moilyB, game.TradingPost(b));
        game.World.SolveNetworks();

        Assert.AreSame(networkA, game.World.NetworkOf(moilyA.Id));
    }

    [TestMethod]
    public void ExtendingANetworkWithAPipeReplacesItInsteadOfCopyingIt()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        var origin = game.Local(company, 2, 2);
        game.Map(company, origin, "W+T");
        game.Pass(1);
        var income = game.World.GoldPerMinute()[company.Id];

        // Three more pipe tiles, one by one, off the side of the network
        for (var i = 1; i <= 3; i++)
        {
            Assert.IsTrue(game.World.PlacePipes(company.Id, [origin.Offset(1, i)]).Success);
            game.Pass(1);
        }

        Assert.AreEqual(1, game.World.Networks.Count(t => t.Buildings.Count > 0), "one network, not a stale copy per extension");
        Assert.AreEqual(income, game.World.GoldPerMinute()[company.Id], 1e-9, "the income is counted once");
    }
}

