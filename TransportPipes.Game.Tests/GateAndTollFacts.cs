using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class GateAndTollFacts
{
    private const double Tolerance = 1e-6;

    [TestMethod]
    public void AContractGateCarriesThePipeButOnlyContractTrades()
    {
        var game = new TestGame();
        var a = game.NewCompany("A");
        var b = game.NewCompany("B", level: 2);
        var moily = game.Build(a, "windMoily");
        game.Connect(a, moily, GateSide.Right);
        var gate = WorldLayout.GatePosition(a.District, a.Slot, GateSide.Right);
        game.World.PlacePipes(a.Id, [gate.Offset(1, 0), gate.Offset(2, 0)]);
        var mine = game.Build(b, "ironMine", game.Spot(b, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.Connect(b, mine, GateSide.Left);
        game.Connect(b, mine, game.TradingPost(b));
        game.World.SolveNetworks();
        Assert.IsTrue(game.World.NetworkOf(moily.Id)!.Solution.Flows.Any(t => t.Kind == FlowKind.Trade));

        Assert.IsTrue(game.World.SetGatePolicy(b.Id, GateSide.Left, GatePolicy.Contract).Success);
        game.World.SolveNetworks();

        Assert.AreSame(game.World.NetworkOf(moily.Id), game.World.NetworkOf(mine.Id), "the pipe still goes through");
        Assert.IsFalse(game.World.NetworkOf(moily.Id)!.Solution.Flows.Any(t => t.Kind == FlowKind.Trade), "but without a contract nothing is traded");
    }

    [TestMethod]
    public void ATollMeterTakesItsShareOfWhatFlowsThroughIt()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var a = game.NewCompany("A");
        var b = game.NewCompany("B");
        var c = game.NewCompany("C");
        var origin = game.Local(a, 1, 1);
        game.Map(a, origin, " W+");
        var meter = game.Map(c, origin, "   X").Values.Single();
        game.Map(b, origin, "    +M+T");

        var trade = game.World.NetworkOf(meter.Id)!.Solution;
        Assert.AreEqual(10 * 1.75, trade.GoldPerMinute[a.Id], Tolerance, "no toll yet");

        Assert.IsTrue(game.World.SetBuildingSetting(c.Id, meter.Id, 0.2).Success);
        Assert.AreEqual(GameWorld.MaxToll, meter.Setting, "at most 5%");
        game.World.SolveNetworks();

        var solution = game.World.NetworkOf(meter.Id)!.Solution;
        var fee = 10 * 1.75 * 0.05;
        Assert.AreEqual(10 * 1.75 - fee, solution.GoldPerMinute[a.Id], Tolerance, "the seller pays the toll");
        Assert.AreEqual(fee / 2, solution.GoldPerMinute[c.Id], Tolerance, "half goes to the meter's owner, half is destroyed");
        Assert.AreEqual(fee / 2, game.World.TollIncome(meter.Id), Tolerance);
    }

    [TestMethod]
    public void TollMetersStandOnYourOwnCorridorPipe()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 9);
        var gate = WorldLayout.GatePosition(company.District, company.Slot, GateSide.Right);
        var meter = game.World.Content.Building("tollMeter");

        StringAssert.Contains(game.World.CanPlace(company, meter, game.Local(company, 12, 12)).Error, "corridors");
        StringAssert.Contains(game.World.CanPlace(company, meter, gate.Offset(2, 0)).Error, "own corridor pipes");
        game.World.PlacePipes(company.Id, [gate.Offset(1, 0)]);
        Assert.IsTrue(game.World.PlaceBuilding(company.Id, "tollMeter", gate.Offset(2, 0)).Success);
    }
}
