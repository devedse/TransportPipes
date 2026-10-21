using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class ValveFacts
{
    private const double Tolerance = 1e-4;

    private static (TestGame Game, Company Company, Dictionary<(int X, int Y), Building> Map) Setup(params string[] rows)
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        var map = game.Map(company, game.Local(company, 1, 1), string.Join("\n", rows));
        return (game, company, map);
    }

    private static double Stored(TestGame game, Building storage, string resource) =>
        game.World.NetworkOf(storage.Id)?.Solution.StorageNet.TryGetValue(storage.Id, out var net) == true && net.TryGetValue(resource, out var v) ? v : 0;

    [TestMethod]
    public void AValveLetsEverythingThroughUntilResourcesAreChosen()
    {
        var (game, company, map) = Setup(
            " W+++V+O",
            "   +    ",
            "   M    ");
        var storage = map[(7, 0)];
        Assert.AreEqual(12, Stored(game, storage, "ironOre"), Tolerance);
        Assert.AreEqual(20, Stored(game, storage, "energy"), Tolerance);

        Assert.IsTrue(game.World.SetValveFilter(company.Id, map[(5, 0)].Id, "ironOre", true).Success);
        game.World.SolveNetworks();

        Assert.AreEqual(12, Stored(game, storage, "ironOre"), Tolerance, "the ore still passes");
        Assert.AreEqual(0, Stored(game, storage, "energy"), Tolerance, "the energy is stopped by the valve");
        Assert.AreEqual(1.0 / 3, game.World.NetworkOf(storage.Id)!.Solution.Rate(map[(1, 0)].Id), Tolerance, "the wind moily only serves the mine");
    }

    [TestMethod]
    public void BuildingsBehindAPriorityValveAreServedFirst()
    {
        var (game, _, map) = Setup(
            " S+P+M+O",
            "  +    +",
            "  M+++++");
        var solution = game.World.NetworkOf(map[(1, 0)].Id)!.Solution;

        Assert.AreEqual(0.5, solution.Rate(map[(5, 0)].Id), Tolerance, "5 energy for the mine behind the priority valve");
        Assert.AreEqual(0, solution.Rate(map[(2, 2)].Id), Tolerance, "nothing left for the other one");
    }

    /// <summary>A company that can build pump stations (Company Level 9 and the research)</summary>
    private static (TestGame Game, Company Company, Dictionary<(int X, int Y), Building> Map) SetupWithPumps(params string[] rows)
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 9);
        company.Researched.Add("pumpStations");
        var map = game.Map(company, game.Local(company, 1, 1), string.Join("\n", rows));
        return (game, company, map);
    }

    [TestMethod]
    public void BigNetworksNeedPumpStationsOnceTheyCanBeBuilt()
    {
        var panels = new string('S', 60);
        var (before, _, beforeMap) = Setup(panels, new string('+', 60) + "T", "");
        Assert.AreEqual(1, before.World.NetworkOf(beforeMap[(0, 0)].Id)!.PumpFactor, "no penalty for what can't be built yet");

        var (game, _, map) = SetupWithPumps(panels, new string('+', 60) + "T", "");
        var network = game.World.NetworkOf(map[(0, 0)].Id)!;

        Assert.AreEqual(50.0 / 60, network.PumpFactor, 1e-9);
        Assert.AreEqual(60 * 5 * 50.0 / 60, network.Solution.Flows.Where(t => t.Kind == FlowKind.Sale).Sum(t => t.Delivered), Tolerance);

        var (game2, _, map2) = SetupWithPumps(panels, new string('+', 60) + "T", "U");
        var network2 = game2.World.NetworkOf(map2[(0, 0)].Id)!;
        Assert.AreEqual(1, network2.PumpFactor);
        Assert.AreEqual(60 * 5 - 5, network2.Solution.Flows.Where(t => t.Kind == FlowKind.Sale).Sum(t => t.Delivered), Tolerance, "the pump uses 5 energy");
    }

    [TestMethod]
    public void AValveIsPartOfThePipeAndConnectsBothSides()
    {
        var (game, _, map) = Setup(" W+V+O");
        Assert.AreSame(game.World.NetworkOf(map[(1, 0)].Id), game.World.NetworkOf(map[(5, 0)].Id));
        Assert.AreEqual(30, Stored(game, map[(5, 0)], "energy"), Tolerance);
    }
}
