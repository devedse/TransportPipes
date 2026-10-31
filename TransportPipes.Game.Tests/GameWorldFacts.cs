using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class GameWorldFacts
{
    [TestMethod]
    public void ANewCompanyGetsAnEstateWithHqTradingPostAndAYardGate()
    {
        var game = new TestGame();
        var company = game.World.CreateCompany(1, "Pipe Dreams", "#33aaff");

        Assert.AreEqual(2000, company.Gold);
        Assert.AreEqual(1, company.Level);
        var estate = game.EstateOf(company);
        Assert.AreEqual(company.Id, estate.OwnerId);
        Assert.AreEqual(4, estate.UnlockedCount);
        Assert.IsTrue(WorldLayout.StartParcels(estate.Slot).All(estate.IsUnlocked), "the start area against the Yard");
        CollectionAssert.AreEquivalent(new[] { "hq", "tradingPost" }, game.World.BuildingsOf(company.Id).Select(t => t.Type).ToArray());
        Assert.AreEqual(estate.YardGate, game.World.Conductors.Single(t => t.Kind == ConductorKind.Gate && t.Owner == company.Id).Pos);
        Assert.AreEqual(0, company.LifetimeEarnings, "nothing earned yet");
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
        StringAssert.Contains(game.World.PlaceBuilding(company.Id, "windMoily", game.Local(neighbour, 12, 12)).Error, "own plot");
        // Just beside the plot: a road, or the outskirts set aside for it (1.4 §5)
        var beside = game.World.PlaceBuilding(company.Id, "windMoily", game.Local(company, -1, 12)).Error!;
        Assert.IsTrue(beside.Contains("own plot") || beside.Contains("outskirts"), beside);
        StringAssert.Contains(game.World.PlaceBuilding(company.Id, "windMoily", hq.Position).Error, "already");
        Assert.IsTrue(game.World.PlaceBuilding(company.Id, "windMoily", game.Spot(company, "windMoily")).Success);
    }

    [TestMethod]
    public void BuildingCostsGoldAndEveryCopyCostsMore()
    {
        var game = new TestGame();
        var company = game.NewCompany(gold: 2000);

        var first = game.Build(company, "windMoily");
        Assert.AreEqual(1800, company.Gold);
        Assert.AreEqual(0, company.LifetimeEarnings, "spending isn't earning: the Company Level counts gold earned");

        var second = game.Build(company, "windMoily");
        Assert.AreEqual(1800 - 214, company.Gold, "the second copy costs 7% more");
        Assert.AreEqual(200, first.Price);
        Assert.AreEqual(214, second.Price, "each copy remembers what it cost: its levels cost from it");
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
        var loss = game.World.Content.PipeTier(1).LossPerTile;
        Assert.AreEqual(30 * wind * Math.Pow(1 - loss, path.Count), sale.Delivered, 1e-9, "the grey pipe loses a little per tile");
        Assert.AreEqual(gold + sale.Delivered * 1 * 10, company.Gold, 1e-6);
        Assert.AreEqual(sale.Delivered * 10, company.LifetimeEarnings, 1e-6, "the gold earned counts for the Company Level");
        Assert.AreEqual(sale.Delivered * 10, company.EarnedLog["market"]["energy"], 1e-6);
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
    public void AStorageKeepsWhatItsCompanyNeedsAndTheRestIsSold()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");
        var storage = game.Build(company, "storage");
        game.Connect(company, moily, storage);
        game.Connect(company, moily, game.TradingPost(company));
        game.World.EnsureOrders(company);
        Assert.AreEqual(0, game.World.StorageNeed(company, "energy"), 1e-9, "hub orders buy at the trading post (after run 3, step 7): nothing is kept for them");
        var gold = company.Gold;

        game.Pass(1);
        game.Pass(60);
        Assert.IsGreaterThan(gold, company.Gold, "it is sold");
        Assert.AreEqual(FlowKind.Sale, game.World.NetworkOf(moily.Id)!.Solution.Flows.Single().Kind);

        // Told to keep everything, it fills up
        Assert.IsTrue(game.World.SetBuildingSetting(company.Id, storage.Id, 1).Success);
        game.Pass(60 * 24);
        Assert.AreEqual(5000, storage.Contents["energy"], 1e-6);
    }

    [TestMethod]
    public void AStorageTakesWhatAFullTradingPostCantSellAboveWhatItKeeps()
    {
        // 12 moilies make 360 energy/min, the trading post sells 600 a minute of everything; with 3 more it's full
        var game = new TestGame();
        var company = game.NewCompany();
        var storage = game.Build(company, "storage");
        storage.Setting = 0.1;
        var post = game.TradingPost(company);
        game.Connect(company, storage, post);
        for (var i = 0; i < 24; i++)
        {
            var moily = game.Build(company, "windMoily");
            game.Connect(company, moily, post);
        }
        game.Pass(60);
        var flows = game.World.NetworkOf(post.Id)!.Solution.Flows;
        var sold = flows.Where(t => t.Kind == FlowKind.Sale).Sum(t => t.Delivered);
        var stored = flows.Where(t => t.Kind == FlowKind.ToStorage).Sum(t => t.Delivered);
        Console.WriteLine($"sold {sold}, stored {stored}, in storage {storage.Contents.GetValueOrDefault("energy")}");
        Assert.IsGreaterThan(500, storage.Contents.GetValueOrDefault("energy"), "above what it keeps: the overflow of a full trading post");
    }

    [TestMethod]
    public void AStorageKeepsWhatTheBuildingsNeed()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 16);
        var storage = game.Build(company, "storage");
        var definition = game.World.Content.Building("storage");
        game.World.EnsureOrders(company);
        Assert.IsTrue(company.DailyOrders.Any(t => t.Resource == "energy"), "a company that makes nothing yet is asked for energy");

        Assert.AreEqual(0, game.World.StorageKeep(storage, definition, "energy"), 1e-6, "orders fill at the trading post, not from a Storage");
        Assert.AreEqual(500, game.World.StorageKeep(storage, definition, "bricks"), 1e-6, "enough for a Market Hall (Company Level 16)");
        Assert.AreEqual(0, game.World.StorageKeep(storage, definition, "ironOre"), 1e-6, "nothing needs it: all of it is sold");

        Assert.IsTrue(game.World.SetBuildingSetting(company.Id, storage.Id, 0.5).Success);
        Assert.AreEqual(2500, game.World.StorageKeep(storage, definition, "ironOre"), 1e-6, "half its capacity, as its owner chose");
    }

    /// <summary>
    /// A Storage's sliders (the owner, 2026-10-31: "drag sliders per resource ... if you specify more than the total that
    /// would fit ... you can't save it"): each good kept as set, at least what the company needs, never more than the
    /// room together; it fills up to them, and the rest goes on to be sold
    /// </summary>
    [TestMethod]
    public void AStorageKeepsWhatItsSlidersSayAndNoMoreThanItsRoom()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 16);
        var storage = game.Build(company, "storage");
        var definition = game.World.Content.Building("storage");
        var room = game.World.StorageCapacity(storage, definition);

        var over = game.World.SetStorageKeeps(company.Id, storage.Id, new Dictionary<string, double> { ["energy"] = room, ["ironOre"] = 1 });
        Assert.IsFalse(over.Success, "more than fits");
        StringAssert.Contains(over.Error, "more than fits");
        Assert.IsEmpty(storage.Keeps, "nothing saved");

        Assert.IsTrue(game.World.SetStorageKeeps(company.Id, storage.Id, new Dictionary<string, double> { ["energy"] = 1200, ["ironOre"] = 300, ["bricks"] = 100 }).Success);
        Assert.AreEqual(1200, game.World.StorageKeep(storage, definition, "energy"), 1e-6);
        Assert.AreEqual(300, game.World.StorageKeep(storage, definition, "ironOre"), 1e-6);
        Assert.AreEqual(500, game.World.StorageKeep(storage, definition, "bricks"), 1e-6, "at least what the company needs (a Market Hall's bricks)");
        Assert.AreEqual(0, game.World.StorageKeep(storage, definition, "copperOre"), 1e-6, "no slider: only what's needed");

        // It fills up to its slider, and what comes beyond goes on to be sold
        var moilies = Enumerable.Range(0, 4).Select(_ => game.Build(company, "windMoily")).ToList();
        foreach (var moily in moilies)
        {
            game.Connect(company, moily, storage);
            game.Connect(company, moily, game.TradingPost(company));
        }
        game.Pass(1);
        game.Pass(60 * 24);
        Assert.AreEqual(1200, storage.Contents["energy"], 1, "it keeps what its slider says");
        var gold = company.Gold;
        game.Pass(60);
        Assert.IsGreaterThan(gold, company.Gold, "the rest is sold");

        // Its sliders replace the share buttons; the share buttons replace its sliders
        Assert.AreEqual(0, storage.Setting);
        Assert.IsTrue(game.World.SetBuildingSetting(company.Id, storage.Id, 0.5).Success);
        Assert.IsTrue(game.World.SetStorageKeeps(company.Id, storage.Id, new Dictionary<string, double>()).Success);
        Assert.AreEqual(0, game.World.StorageKeep(storage, definition, "ironOre"), 1e-6, "no sliders: what's needed");
    }

    [TestMethod]
    public void PipesOfTwoPlayersThatMeetStayTwoNetworks()
    {
        // Playtest: pipes joined across the wilderness made one network of four players, and everything got slow
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana");
        var ben = game.NewCompany("Ben");
        var anas = game.Map(ana, game.Local(ana, 1, 1), "W++");
        // Ben's pipe touching Ana's, and his Wind Moily on it (no placement rules here)
        var bens = game.Map(ben, game.Local(ana, 4, 1), "+W");
        game.Pass(1);

        var anaNetwork = game.World.NetworkOf(anas[(0, 0)].Id)!;
        var benNetwork = game.World.NetworkOf(bens[(1, 0)].Id)!;
        Assert.AreNotEqual(anaNetwork.Id, benNetwork.Id, "two networks");
        Assert.IsTrue(anaNetwork.Buildings.All(t => game.World.Building(t)!.Owner == ana.Id), "Ana's has only hers");
        Assert.IsFalse(anaNetwork.Solution.Flows.Any(t => t.Kind == FlowKind.Trade), "nothing trades over the pipes: what goes to others passes the Yard gate");
    }

    [TestMethod]
    public void ABuildingOnAForestLeavesItAndItShowsAgainWhenTheBuildingGoes()
    {
        // Playtest: a Lumber Camp placed on the forest felled it, and moved or demolished the land stayed bare
        var game = new TestGame();
        var company = game.NewCompany(gold: 1_000_000, level: 10);
        var forest = game.Spot(company, "windMoily", t => game.World.Tile(t).Terrain == TerrainType.Forest);
        var moily = game.Build(company, "windMoily", forest);
        Assert.AreEqual(TerrainType.Forest, game.World.Tile(forest).Terrain, "it stands on the forest");

        var to = game.Spot(company, "windMoily", t => t != forest && game.World.Tile(t).Terrain != TerrainType.Forest);
        Assert.IsTrue(game.World.Move(company.Id, moily.Id, to).Success);
        Assert.AreEqual(TerrainType.Forest, game.World.Tile(forest).Terrain, "moved away: the forest is there");

        // Felled by a building before the fix: it grows back once the building goes
        Assert.IsTrue(game.World.Move(company.Id, moily.Id, forest).Success);
        game.World.LoadTerrainOverride(forest, TerrainType.Grass);
        Assert.IsTrue(game.World.Demolish(company.Id, moily.Id).Success);
        Assert.AreEqual(TerrainType.Forest, game.World.Tile(forest).Terrain, "demolished: the forest grows back");
    }

    [TestMethod]
    public void WhatAStorageHoldsAboveItsLineIsSoldDownToTheLine()
    {
        // Playtest: its line lowered to 805 each, the 3,891 energy from before stayed, and nothing more fitted
        var game = new TestGame();
        var company = game.NewCompany();
        var storage = game.Build(company, "storage");
        var post = game.TradingPost(company);
        game.Connect(company, storage, post);
        storage.Setting = 0.1;
        var keep = game.World.StorageKeep(storage, game.World.Content.Building("storage"), "energy");
        storage.Contents["energy"] = keep + 2000;
        game.World.MarkNeedsSolve(storage.Id);
        var gold = company.Gold;

        game.Pass(1);
        var flow = game.World.NetworkOf(storage.Id)!.Solution.Flows.Single();
        Assert.AreEqual(FlowKind.Sale, flow.Kind, "what's above the line goes on to be sold");
        Assert.AreEqual(storage.Id, flow.From);
        game.Pass(20);

        Assert.AreEqual(keep, storage.Contents["energy"], 1e-3, "down to the line, no further");
        Assert.AreEqual(2000 * game.World.BasePrice("energy"), company.Gold - gold, 2000 * game.World.BasePrice("energy") * 0.3, "sold at the market's price");
        Assert.IsEmpty(game.World.NetworkOf(storage.Id)!.Solution.Flows, "then it keeps the rest");
    }

    [TestMethod]
    public void AStorageTakenBelowItsLineWhileItSellsStopsSelling()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var storage = game.Build(company, "storage");
        game.Connect(company, storage, game.TradingPost(company));
        storage.Setting = 0.1;
        var keep = game.World.StorageKeep(storage, game.World.Content.Building("storage"), "energy");
        storage.Contents["energy"] = keep + 2000;
        game.World.MarkNeedsSolve(storage.Id);
        game.Pass(1);

        // A want or an order took from it meanwhile
        storage.Contents["energy"] = keep - 100;
        game.Pass(5);

        Assert.AreEqual(keep - 100, storage.Contents["energy"], 1, "it sells no further");
    }

    [TestMethod]
    public void TheChangesOfANewPipeCarryTheIdOfTheNetworkItRebuilt()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");
        game.Connect(company, moily, game.TradingPost(company));
        var network = game.World.NetworkOf(moily.Id)!;
        var before = network.Id;
        game.World.TakeChanges();

        // One more pipe tile beside the network: it is rebuilt with a new id
        var estate = game.EstateOf(company);
        var tile = network.Conductors.SelectMany(TestGame.Neighbours)
            .First(t => estate.Contains(t) && !game.World.IsOccupied(t) && game.World.Tile(t).Terrain is not (TerrainType.Mountain or TerrainType.Water));
        Assert.IsTrue(game.World.PlacePipes(company.Id, [tile]).Success);
        var changes = game.World.TakeChanges();
        var after = game.World.NetworkOf(moily.Id)!.Id;

        Assert.AreNotEqual(before, after);
        Assert.Contains(after, changes.Networks, "the clients learn the new id with the pipe (else most pipes keep the old one)");
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
        Assert.AreEqual(b.LifetimeEarnings, a.LifetimeEarnings, 1e-6 * b.LifetimeEarnings);
        var storageA = once.World.Buildings.Single(t => t.Type == "storage");
        var storageB = often.World.Buildings.Single(t => t.Type == "storage");
        Assert.AreEqual(storageB.StoredTotal, storageA.StoredTotal, 1e-6);
    }

    [TestMethod]
    public void UpgradingRaisesTheOutputTenPercentALevel()
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
        Assert.AreEqual(level1 * Math.Pow(1.1, 9), game.World.ToFlowNode(moily)!.Out["energy"], 1e-9);
        Assert.AreEqual(Math.Min(100, level1 * Math.Pow(1.1, 9)), Sent(), 1e-9, "a grey pipe carries 100 per minute");
    }

    [TestMethod]
    public void TheIncomeIsRightStraightAfterAnUpgrade()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");
        game.Connect(company, moily, game.TradingPost(company));
        game.Pass(1);
        var before = game.World.GoldPerMinute()[company.Id];

        // No tick between the upgrade and the read (run 1's "+0/min" right after a command)
        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 1).Success);
        var straightAfter = game.World.GoldPerMinute()[company.Id];
        game.Pass(1);

        Assert.IsTrue(straightAfter > before, "the upgrade shows at once");
        Assert.AreEqual(game.World.GoldPerMinute()[company.Id], straightAfter, 1e-9, "the same rate as after a tick");
    }

    [TestMethod]
    public void ParcelsAreBoughtNextToYourLandWhenYourLevelIsHighEnough()
    {
        var game = new TestGame();
        var company = game.NewCompany(gold: 25_000);
        var estate = game.EstateOf(company);
        bool Touches(int parcel) => new[] { -1, 1, -4, 4 }.Select(t => parcel + t)
            .Where(t => t is >= 0 and < 16 && (Math.Abs(t % 4 - parcel % 4) + Math.Abs(t / 4 - parcel / 4)) == 1).Any(estate.IsUnlocked);
        var next = Enumerable.Range(0, 16).First(t => !estate.IsUnlocked(t) && Touches(t));
        var far = Enumerable.Range(0, 16).First(t => !estate.IsUnlocked(t) && !Touches(t));

        StringAssert.Contains(game.World.BuyParcel(company.Id, next).Error, "Company Level 5");
        company.Level = 5;
        StringAssert.Contains(game.World.BuyParcel(company.Id, far).Error, "next to land you own");
        Assert.IsTrue(game.World.BuyParcel(company.Id, next).Success);
        Assert.AreEqual(5_000, company.Gold, "20,000 for the first");
        Assert.IsTrue(estate.IsUnlocked(next));
        Assert.AreEqual((50_000, 7), game.World.NextParcel(company), "each 2.5 times the last, at every second level");
        var (px, py) = WorldLayout.ParcelOrigin(next);
        var spot = game.Local(company, px + 4, py + 2);
        Assert.IsTrue(game.World.PlaceBuilding(company.Id, "solarPanel", spot).Success || game.World.Tile(spot).Terrain is TerrainType.Water or TerrainType.Mountain || game.World.Tile(spot).Deposit != null);
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

        StringAssert.Contains(game.World.PlacePipes(a.Id, [game.Local(b, 12, 12)]).Error, "someone else's plot");
        Assert.IsTrue(game.World.PlacePipes(a.Id, [game.Local(a, 2, 2)]).Success, "pipes may cross parcels you haven't bought");
    }

    [TestMethod]
    public void APipeRunsOnThroughYourOwnYardGate()
    {
        // 1.4's e2e: the gate stands by the trading post, and a drag from a building to the post crossed it
        var game = new TestGame();
        var a = game.NewCompany("A");
        var b = game.NewCompany("B");
        var gate = game.EstateOf(a).YardGate;
        var free = TestGame.Neighbours(gate).Where(t => WorldLayout.ZoneAt(t.X, t.Y).Kind == ZoneKind.Estate
            && game.World.ConductorAt(t) == null && game.World.BuildingAt(t) == null).ToList();

        var placed = game.World.PlacePipes(a.Id, [free[0], gate]);

        Assert.IsTrue(placed.Success, placed.Error);
        Assert.AreEqual(ConductorKind.Gate, game.World.ConductorAt(gate)!.Kind, "the gate stays a gate");
        StringAssert.Contains(game.World.PlacePipes(b.Id, [gate]).Error, "someone else's plot", "not through someone else's");
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

