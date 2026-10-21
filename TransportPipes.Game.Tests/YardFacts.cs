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
