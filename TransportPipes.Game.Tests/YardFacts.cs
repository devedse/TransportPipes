using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class YardFacts
{
    /// <summary>
    /// Company A (slot 0) has a Wind Moily, company B (slot 1, the same station) an Iron Mine that needs energy; both pipe
    /// to their Yard gates
    /// </summary>
    private static (TestGame Game, Company A, Company B, Building Moily, Building Mine) Neighbours(bool mineToStorage = false)
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var a = game.NewCompany("A");
        var b = game.NewCompany("B", level: 2);
        Assert.AreEqual(WorldLayout.StationOf(a.District, a.Slot), WorldLayout.StationOf(b.District, b.Slot), "slots 0 and 1 share a station");

        var moily = game.Build(a, "windMoily");
        game.ConnectToGate(a, moily);
        game.Connect(a, moily, game.TradingPost(a));

        var mine = game.Build(b, "ironMine", game.Spot(b, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(b, mine);
        game.Connect(b, mine, mineToStorage ? game.Build(b, "storage") : game.TradingPost(b));
        game.World.SolveNetworks();
        return (game, a, b, moily, mine);
    }

    private static double ImportedBy(TestGame game, Building building, string resource) =>
        game.World.NetworkOf(building.Id)!.GateFlows.Where(t => t.Import && t.Building == building.Id && t.Resource == resource).Sum(t => t.Delivered);

    private static double ExportedBy(TestGame game, Building building, string resource) =>
        game.World.NetworkOf(building.Id)!.GateFlows.Where(t => !t.Import && t.Building == building.Id && t.Resource == resource).Sum(t => t.Delivered);

    [TestMethod]
    public void NeighboursTradeThroughTheYardAtThePlayerPrice()
    {
        var (game, a, b, moily, mine) = Neighbours();

        // The first round matches, the second delivers what was sent
        game.Pass(3);

        Assert.AreNotSame(game.World.NetworkOf(moily.Id), game.World.NetworkOf(mine.Id), "networks never cross a gate");
        Assert.AreEqual(10, ImportedBy(game, mine, "energy"), 1e-6, "the mine gets the 10 energy it needs from A");
        var atTheGate = game.World.NetworkOf(mine.Id)!.GateFlows.Where(t => t.Import).Sum(t => t.Sent);
        Assert.IsGreaterThan(10, atTheGate, "B's own pipe loses a little between its gate and its mine");
        Assert.AreEqual(atTheGate, ExportedBy(game, moily, "energy"), 1e-6, "A sends that to the Yard");
        Assert.AreEqual(1.75, game.World.PlayerPrice("energy"));

        var goldA = a.Gold;
        var goldB = b.Gold;
        var paid = atTheGate * 1.75;
        var oreSales = game.World.NetworkOf(mine.Id)!.Solution.GoldPerMinute[b.Id] + paid;
        var energySales = game.World.NetworkOf(moily.Id)!.Solution.GoldPerMinute[a.Id];
        game.Pass(10);
        Assert.AreEqual(goldA + 10 * (energySales + paid), a.Gold, 1e-6, "A earns what B pays for the energy, besides its hub sales");
        Assert.AreEqual(goldB + 10 * (oreSales - paid), b.Gold, 1e-6, "B pays the player price for what leaves the Yard for it");
    }

    /// <summary>
    /// A's Wind Moily piped to its gate and (unless gateOnly) its trading post; B, next door, has a Small Factory piped
    /// to its gate and post but no iron ore, so it can't use the energy it seems to lack (run 1's Lab trap, small)
    /// </summary>
    private static (TestGame Game, Company A, Company B, Building Moily, Building Factory) StuckBuyer(bool withB = true, bool gateOnly = false)
    {
        // Lossless pipes: what A's own pipes lose on the longer way to its gate is its own (not the floor's business)
        var game = new TestGame(content: TestGame.LosslessContent);
        var a = game.NewCompany("A");
        var moily = game.Build(a, "windMoily");
        game.ConnectToGate(a, moily);
        if (!gateOnly)
        {
            game.Connect(a, moily, game.TradingPost(a));
        }
        var b = game.NewCompany("B", level: 2);
        Building factory = null!;
        if (withB)
        {
            factory = game.Build(b, "smallFactory");
            game.ConnectToGate(b, factory);
            game.Connect(b, factory, game.TradingPost(b));
        }
        game.World.SolveNetworks();
        return (game, a, b, moily, factory);
    }

    [TestMethod]
    public void SellingThroughTheYardNeverPaysLessThanTheHub()
    {
        double GoldOfA(bool withB)
        {
            var (game, a, _, _, _) = StuckBuyer(withB);
            game.Pass(5);
            var before = a.Gold;
            game.Pass(60);
            return a.Gold - before;
        }

        var alone = GoldOfA(withB: false);
        var besideTheStuckBuyer = GoldOfA(withB: true);
        Assert.IsTrue(alone > 0);
        Assert.IsTrue(besideTheStuckBuyer >= alone * 0.999, $"next to a buyer that takes nothing A still earns its hub's worth ({besideTheStuckBuyer:F1} against {alone:F1})");
    }

    [TestMethod]
    public void ABuyerThatCantRunIsMatchedNothing()
    {
        var (game, a, _, _, _) = StuckBuyer();
        game.Pass(15);

        Assert.AreEqual(0, game.World.StatValue(a, "yardSent"), 1e-9, "a factory without iron ore lacks no energy it could use");
    }

    [TestMethod]
    public void ABuyerHeldBackByAnotherInputIsMatchedOnlyWhatItCanUse()
    {
        // Cleo's Lab, small: B's factory at level 10 could use 94 energy a minute, but its one mine brings only a fifth
        // of the iron ore it needs, so the energy it can use is a fifth too
        var game = new TestGame(content: TestGame.LosslessContent);
        var a = game.NewCompany("A");
        var post = game.TradingPost(a);
        for (var i = 0; i < 5; i++)
        {
            var moily = game.Build(a, "windMoily");
            game.ConnectToGate(a, moily);
            game.Connect(a, moily, post);
        }
        var b = game.NewCompany("B", level: 10);
        var mine = game.Build(b, "ironMine", game.Spot(b, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        var factory = game.Build(b, "smallFactory");
        Assert.IsTrue(game.World.Upgrade(b.Id, factory.Id, 9).Success);
        game.Connect(b, mine, factory);
        game.ConnectToGate(b, mine);
        game.ConnectToGate(b, factory);
        game.Connect(b, factory, game.TradingPost(b));
        game.Pass(20);

        var factoryNode = game.World.ToFlowNode(factory)!;
        var mineNode = game.World.ToFlowNode(mine)!;
        var oreShare = mineNode.Out["ironOre"] / factoryNode.In["ironOre"];
        var canUse = mineNode.In["energy"] + factoryNode.In["energy"] * oreShare;
        var matched = game.World.YardTrades(game.EstateOf(b)).Where(t => !t.Selling && t.Resource == "energy").Sum(t => t.PerMinute);
        Assert.IsTrue(oreShare < 0.25, $"the ore is short ({oreShare:P0})");
        Assert.AreEqual(canUse, matched, canUse * 0.05, $"matched {matched:F1} energy a minute, of {mineNode.In["energy"] + factoryNode.In["energy"]:F1} wanted");
        var sent = game.World.StatValue(a, "yardSent");
        Assert.IsTrue(game.World.StatValue(a, "yardUntaken") < 0.05 * sent, "and B takes what it is matched");
    }

    [TestMethod]
    public void AStorageBoundProducerOffersItsSpare()
    {
        // A's moily fills a Storage that doesn't keep energy (no trading post on its pipes): that energy is spare
        var game = new TestGame(content: TestGame.LosslessContent);
        var a = game.NewCompany("A");
        var moily = game.Build(a, "windMoily");
        game.ConnectToGate(a, moily);
        game.Connect(a, moily, game.Build(a, "storage"));
        var b = game.NewCompany("B", level: 2);
        var mine = game.Build(b, "ironMine", game.Spot(b, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(b, mine);
        game.Connect(b, mine, game.TradingPost(b));
        game.Pass(5);

        Assert.AreEqual(10, ImportedBy(game, mine, "energy"), 1e-6, "B's mine runs on A's stored-away energy");
        Assert.IsTrue(game.World.StatValue(a, "yardPaid") > 0);
    }

    [TestMethod]
    public void AMatchNobodyTakesShrinks()
    {
        // After B's mine goes, what the Yard sends B falls to the probe at once (it took nothing of its match)
        var (game, a) = TradeThenTheBuyerStops(gateOnly: false);
        game.Pass(3);
        var sent = game.World.StatValue(a, "yardSent");
        game.Pass(10);

        Assert.IsTrue((game.World.StatValue(a, "yardSent") - sent) / 10 <= 0.5 + 1e-6, "at most the probe of half a unit a minute");
    }

    /// <summary>B's mine buys A's energy through the Yard; then B demolishes it, and what the Yard already sent arrives at a gate with no taker</summary>
    private static (TestGame Game, Company A) TradeThenTheBuyerStops(bool gateOnly)
    {
        var game = new TestGame(content: TestGame.LosslessContent);
        var a = game.NewCompany("A");
        var moily = game.Build(a, "windMoily");
        game.ConnectToGate(a, moily);
        if (!gateOnly)
        {
            game.Connect(a, moily, game.TradingPost(a));
        }
        var b = game.NewCompany("B", level: 2);
        var mine = game.Build(b, "ironMine", game.Spot(b, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(b, mine);
        game.Connect(b, mine, game.TradingPost(b));
        game.Pass(3);
        Assert.IsTrue(game.World.StatValue(a, "yardPaid") > 0, "B's mine takes A's energy");
        Assert.IsTrue(game.World.Demolish(b.Id, mine.Id).Success);
        return (game, a);
    }

    [TestMethod]
    public void WhatTheBuyerDoesntTakeIsPaidAtTheSellersHubPrice()
    {
        var (game, a) = TradeThenTheBuyerStops(gateOnly: false);
        var floor = game.World.StatValue(a, "yardFloor");
        var untaken = game.World.StatValue(a, "yardUntaken");
        game.Pass(0.5);
        var units = game.World.StatValue(a, "yardUntaken") - untaken;
        var gold = game.World.StatValue(a, "yardFloor") - floor;

        Assert.IsTrue(units > 1, "what the Yard sent before the mine went arrives with nobody to take it");
        Assert.AreEqual(units * game.World.HubBuyPrice(a, "energy"), gold, 1e-6, "conservation: the seller gets its hub price for what wasn't taken");
    }

    [TestMethod]
    public void TheFloorIsCappedByWhatWouldHaveGoneToTheHub()
    {
        // A's moily reaches only its gate: what it made for the Yard would never have been sold at the hub
        var (game, a) = TradeThenTheBuyerStops(gateOnly: true);
        game.Pass(0.5);

        Assert.IsTrue(game.World.StatValue(a, "yardUntaken") > 1);
        Assert.AreEqual(0, game.World.StatValue(a, "yardFloor"), "no gold for goods that had no way to the hub");
    }

    [TestMethod]
    public void GoldIsConservedThroughTheYard()
    {
        var (game, a, b, _, mine) = Neighbours();
        game.Pass(5);
        double Paid() => game.World.StatValue(a, "yardPaid");
        var paid = Paid();
        var taken = game.World.NetworkOf(mine.Id)!.GateFlows.Where(t => t.Import).Sum(t => t.Sent);
        var goldB = b.Gold;
        var ownB = game.World.NetworkOf(mine.Id)!.Solution.GoldPerMinute[b.Id];
        game.Pass(10);

        Assert.AreEqual(taken * game.World.PlayerPrice("energy") * 10, Paid() - paid, 1e-6, "A gets what B's buildings took, at the player price");
        Assert.AreEqual(goldB + 10 * ownB, b.Gold, 1e-6, "and B paid exactly that (its network's rate already counts it)");
        Assert.AreEqual(0, game.World.StatValue(a, "yardFloor"), 1e-9, "the mine takes all it was matched");
    }

    [TestMethod]
    public void TheTopBarCountsWhatNeighboursPay()
    {
        var (game, a, b, _, _) = Neighbours();
        game.Pass(5);
        var rate = game.World.GoldPerMinute();
        var (goldA, goldB) = (a.Gold, b.Gold);
        game.Pass(10);

        Assert.IsTrue(game.World.StatValue(a, "yardPaid") > 0, "B pays A through the Yard");
        Assert.AreEqual(goldA + 10 * rate[a.Id], a.Gold, 1e-6, "A's rate counts what B pays");
        Assert.AreEqual(goldB + 10 * rate[b.Id], b.Gold, 1e-6, "and B's what it pays");
    }

    [TestMethod]
    public void TheYardPanelListsOnlyWhatArrives()
    {
        var (game, a) = TradeThenTheBuyerStops(gateOnly: false);

        // The Yard still sends what it matched before the mine went, and nobody takes it
        Assert.IsFalse(game.World.YardTrades(game.EstateOf(a)).Any(t => t.Selling), "no trade is shown that nobody takes");
        Assert.AreEqual(0, game.World.NeighbourIncome(a), 1e-9);
    }

    [TestMethod]
    public void TheFloorNeverPaysTheProjectTwice()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var a = game.NewCompany("A");
        var moily = game.Build(a, "windMoily");
        game.ConnectToGate(a, moily);
        game.Connect(a, moily, game.TradingPost(a));
        game.Pass(30);

        Assert.IsTrue(game.World.StatValue(a, "projectGold") > 0, "the Foundation takes A's energy and pays for it");
        Assert.AreEqual(0, game.World.StatValue(a, "yardFloor"), "what the project took is paid by the project only");
    }

    [TestMethod]
    public void WhenTwoYardPipesMeetBothSeeTheHandshake()
    {
        var (game, a, b, _, _) = Neighbours();
        game.Pass(2);

        Assert.IsTrue(game.World.Changes.Events.Any(t => t.Company == a.Id && t.Text.Contains("Handshake")));
        Assert.IsTrue(game.World.Changes.Events.Any(t => t.Company == b.Id && t.Text.Contains("Handshake")));
        Assert.AreEqual(1, game.World.StatValue(a, "yard"), "Loady's Yard step counts the pipe");
    }

    [TestMethod]
    public void NothingIsCreatedOnTheWay()
    {
        var (game, a, _, moily, mine) = Neighbours();

        for (var i = 0; i < 6; i++)
        {
            game.Pass(1);
            Assert.IsTrue(ImportedBy(game, mine, "energy") <= ExportedBy(game, moily, "energy") + 1e-6 || i == 0,
                "a buyer never gets more than its seller sends");
        }
        Assert.IsTrue(game.World.NetworkOf(moily.Id)!.Solution.Flows.Any(t => t.Kind == FlowKind.Sale && t.Resource == "energy"), "A sells the rest to the hub");
        Assert.IsTrue(a.Gold > 0);
    }

    [TestMethod]
    public void WithoutAPipeToTheGateThereIsNoTrade()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var a = game.NewCompany("A");
        var b = game.NewCompany("B", level: 2);
        var moily = game.Build(a, "windMoily");
        game.Connect(a, moily, game.TradingPost(a));
        var mine = game.Build(b, "ironMine", game.Spot(b, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(b, mine);
        game.Connect(b, mine, game.TradingPost(b));

        game.Pass(5);

        Assert.AreEqual(0, ImportedBy(game, mine, "energy"));
    }

    [TestMethod]
    public void ABlockStopsTheTrade()
    {
        var (game, a, b, moily, mine) = Neighbours();
        game.Pass(3);
        Assert.IsTrue(ImportedBy(game, mine, "energy") > 1);

        Assert.IsTrue(game.World.SetTradeBlocked(b.Id, a.Id, true).Success);
        game.Pass(5);
        Assert.IsTrue(ImportedBy(game, mine, "energy") > 1, "a block waits an hour: what is on its way still arrives");
        game.Pass(60);

        Assert.AreEqual(0, ImportedBy(game, mine, "energy"), 1e-9);
        Assert.AreEqual(0, ExportedBy(game, moily, "energy"), 1e-9);
    }

    [TestMethod]
    public void ACompanyWithoutGoldCantBuyFromOthers()
    {
        var (game, a, b, _, mine) = Neighbours(mineToStorage: true);
        game.Pass(3);
        Assert.IsTrue(ImportedBy(game, mine, "energy") > 1);

        b.Gold = 0;
        game.Pass(5);

        Assert.AreEqual(0, ImportedBy(game, mine, "energy"), 1e-9);
    }

    [TestMethod]
    public void CompaniesInDifferentStationsDontShareAYard()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var a = game.NewCompany("A");
        // Slot 2 is in the small station north of the hub
        var others = Enumerable.Range(0, 2).Select(i => game.NewCompany($"B{i}")).ToList();
        var b = others.Last();
        Assert.AreNotEqual(WorldLayout.StationOf(a.District, a.Slot), WorldLayout.StationOf(b.District, b.Slot));
        var moily = game.Build(a, "windMoily");
        game.ConnectToGate(a, moily);
        game.Connect(a, moily, game.TradingPost(a));
        b.Level = 2;
        var mine = game.Build(b, "ironMine", game.Spot(b, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(b, mine);
        game.Connect(b, mine, game.TradingPost(b));

        game.Pass(5);

        Assert.AreEqual(0, ImportedBy(game, mine, "energy"));
    }

    [TestMethod]
    public void TermsSettleSoTheNetworksStopBeingSolvedAgain()
    {
        var (game, _, _, moily, _) = Neighbours();
        game.Pass(10);
        var network = game.World.NetworkOf(moily.Id)!;
        var solution = network.Solution;

        game.Pass(30);

        Assert.AreSame(solution, game.World.NetworkOf(moily.Id)!.Solution, "nothing changed, so nothing was solved again");
    }

    [TestMethod]
    public void AContractIsDeliveredThroughTheYard()
    {
        var (game, a, b, _, _) = Neighbours();
        Assert.IsTrue(game.World.ProposeContract(a.Id, b.Id, selling: true, "energy", 5, 99, 1).Success);
        var contract = game.World.Contracts.Single();
        Assert.AreEqual(1.75, contract.Price, "one price between players");
        Assert.IsTrue(game.World.AcceptContract(b.Id, contract.Id).Success);

        game.Pass(60);

        Assert.IsTrue(contract.Fulfillment > 0.9, $"delivered {contract.Fulfillment:P0}");
    }

    [TestMethod]
    public void PipesCantGoInTheCorridorsOrTheYard()
    {
        var game = new TestGame();
        var a = game.NewCompany("A");
        var estate = game.EstateOf(a);
        var corridor = estate.Origin.Offset(WorldLayout.EstateSize, 5);
        Assert.AreEqual(ZoneKind.Corridor, WorldLayout.ZoneAt(corridor.X, corridor.Y).Kind);
        Assert.IsFalse(game.World.PlacePipes(a.Id, [corridor]).Success);

        var yard = WorldLayout.Yard(estate.Station);
        var inYard = new TilePos(yard.X0 + 5, yard.Y0 + 5);
        StringAssert.Contains(game.World.PlacePipes(a.Id, [inYard]).Error, "Yard");
    }
}
