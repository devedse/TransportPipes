using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>
/// The calculation and connection scenarios of the original game (TransportPipes.Actionizer.Tests), with the same maps,
/// on the new world and flow solver. Pipes are lossless like in the original. Numbers follow the new content (wind
/// moily 30 energy, solar panel 5, iron mine 10 energy to 12 ore, small factory 20 energy and 12 ore to 6 ingots).
/// </summary>
[TestClass]
public class OriginalScenarioFacts
{
    private const double Tolerance = 1e-4;

    private sealed class Scenario
    {
        public TestGame Game { get; } = new(content: TestGame.PlainContent);
        public Company A { get; }
        public Company B { get; }
        public Dictionary<(int X, int Y), Building> Buildings { get; } = new();
        public TilePos Origin { get; }

        public Scenario()
        {
            A = Game.NewCompany("A");
            B = Game.NewCompany("B");
            Origin = Game.Local(A, 1, 1);
        }

        public Scenario Map(string map, Company? owner = null)
        {
            foreach (var (pos, building) in Game.Map(owner ?? A, Origin, map))
            {
                Buildings[pos] = building;
            }
            return this;
        }

        public Building At(int x, int y) => Buildings[(x, y)];

        public IEnumerable<Building> All(string type) => Buildings.Values.Where(t => t.Type == type);

        public Building One(string type) => All(type).Single();

        public FlowSolution Solution(Building building) => Game.World.NetworkOf(building.Id)?.Solution ?? FlowSolution.Empty;

        public double Delivered(Building to, string resource, Building? from = null) =>
            Solution(to).Flows.Where(t => t.To == to.Id && t.Resource == resource && (from == null || t.From == from.Id)).Sum(t => t.Delivered);

        public double Stored(Building storage, string resource) =>
            Solution(storage).StorageNet.TryGetValue(storage.Id, out var net) && net.TryGetValue(resource, out var amount) ? amount : 0;

        public bool Linked(Building a, Building b) => Game.World.NetworkOf(a.Id)?.Links.Any(t => t.Connects(a.Id, b.Id)) == true;

        public Network? Network(Building building) => Game.World.NetworkOf(building.Id);
    }

    private static string Rows(params string[] rows) => string.Join("\n", rows);

    // ---------------------------------------------------------------- DatabaseCalculationsFacts

    [TestMethod]
    public void StorageContainsEnergyOfWindMoilyAfterCalculation()
    {
        var s = new Scenario().Map("W+O");

        Assert.AreEqual(30, s.Delivered(s.One("storage"), "energy"), Tolerance);
        Assert.AreEqual(30, s.Stored(s.One("storage"), "energy"), Tolerance);
    }

    [TestMethod]
    public void WindmillMineStorageOnlyProvidesWhatTheMineNeeds()
    {
        // The wind moily and the storage are on different pipes: energy can't pass through the mine
        var s = new Scenario().Map(" W+M+O ");

        Assert.AreEqual(10, s.Delivered(s.One("ironMine"), "energy"), Tolerance);
        Assert.AreEqual(12, s.Stored(s.One("storage"), "ironOre"), Tolerance);
        Assert.AreEqual(0, s.Stored(s.One("storage"), "energy"), Tolerance);
        Assert.AreEqual(1.0 / 3, s.Solution(s.One("windMoily")).Rate(s.One("windMoily").Id), Tolerance);
    }

    [TestMethod]
    public void ReturnsRightNumbersWhenCalculatingSimpleConnection()
    {
        var s = new Scenario().Map(Rows(
            "          ",
            "  W       ",
            "  +       ",
            "  +       ",
            "  +++O    ",
            "   +      ",
            "   +      ",
            "   +      ",
            "   O      "));

        Assert.AreEqual(15, s.Stored(s.At(5, 4), "energy"), Tolerance);
        Assert.AreEqual(15, s.Stored(s.At(3, 8), "energy"), Tolerance);
    }

    [TestMethod]
    public void TwoWindMoiliesShareTheEnergyAMineNeeds()
    {
        var s = new Scenario().Map(Rows(
            "            ",
            " W+++       ",
            "    M+++O   ",
            " W+++       "));

        var mine = s.One("ironMine");
        Assert.AreEqual(5, s.Delivered(mine, "energy", s.At(1, 1)), Tolerance);
        Assert.AreEqual(5, s.Delivered(mine, "energy", s.At(1, 3)), Tolerance);
        Assert.AreEqual(12, s.Stored(s.One("storage"), "ironOre"), Tolerance);
    }

    [TestMethod]
    public void AStorageOnlyTakesResourcesOfItsOwner()
    {
        // Changed rule: in the original game company A's storage bought B's energy; now storages only serve their owner
        var s = new Scenario();
        s.Map(" +O");
        s.Map("W", s.B);
        var gold = (s.A.Gold, s.B.Gold);

        s.Game.Pass(10);

        Assert.AreEqual(0, s.Stored(s.One("storage"), "energy"), Tolerance);
        Assert.AreEqual(gold, (s.A.Gold, s.B.Gold));
    }

    [TestMethod]
    public void DoesNotReturnNanAsMoneyInThisConnection()
    {
        var s = new Scenario();
        s.Map(Rows(
            " +M+F       ",
            "    +       ",
            "    M       ",
            "    +       ",
            "    W+M+F+T "));
        s.Map("W", s.B);

        s.Game.Pass(10);

        foreach (var network in s.Game.World.Networks)
        {
            Assert.IsTrue(network.Solution.Rates.Values.All(double.IsFinite));
            Assert.IsTrue(network.Solution.GoldPerMinute.Values.All(double.IsFinite));
            Assert.IsTrue(network.Solution.Flows.All(t => double.IsFinite(t.Sent) && double.IsFinite(t.Delivered)));
        }
        Assert.IsTrue(double.IsFinite(s.A.Gold) && double.IsFinite(s.B.Gold));
        // No factory gets energy (they only touch mines and the trading post), so the whole chain stands still
        Assert.IsTrue(s.Game.World.Networks.All(t => t.Solution.Flows.Count == 0));
    }

    [TestMethod]
    public void ADistributionIsTheSameFromEveryStartingPoint()
    {
        var s = new Scenario().Map(Rows(
            "            ",
            "   O+W      ",
            "   +        ",
            " O+++W      "));

        Assert.AreEqual(15, s.Stored(s.At(1, 3), "energy"), Tolerance);
        Assert.AreEqual(30 + 15, s.Stored(s.At(3, 1), "energy"), Tolerance);
        Assert.AreEqual(30, s.Delivered(s.At(3, 1), "energy", s.At(5, 1)), Tolerance);
    }

    [TestMethod]
    public void SolarAndWindMillDistribution()
    {
        var s = new Scenario().Map(Rows(
            "             ",
            " W++ ++M++   ",
            "   + +   +   ",
            "   ++++M+++O ",
            "   + +   +   ",
            " S++ ++M++   "));

        var wind = s.One("windMoily");
        var solar = s.One("solarPanel");
        foreach (var mine in s.All("ironMine"))
        {
            // 35 energy for 30 needed: every mine gets 10, in proportion to what each producer offers
            Assert.AreEqual(10, s.Delivered(mine, "energy"), Tolerance);
            Assert.AreEqual(10 * 30 / 35.0, s.Delivered(mine, "energy", wind), Tolerance);
            Assert.AreEqual(10 * 5 / 35.0, s.Delivered(mine, "energy", solar), Tolerance);
        }
        Assert.AreEqual(3 * 12, s.Stored(s.One("storage"), "ironOre"), Tolerance);
    }

    [TestMethod]
    public void FactoryNotBeingAbleToWorkAtFullSpeed()
    {
        // A solar panel (5 energy) instead of the wind moily, so energy is short like in the original scenario
        var s = new Scenario().Map(Rows(
            "        ",
            " S+F++O ",
            " + +    ",
            " M++    "));

        var share = 5 / 30.0;
        var factory = s.One("smallFactory");
        Assert.AreEqual(20 * share, s.Delivered(factory, "energy"), Tolerance);
        Assert.AreEqual(12 * share, s.Delivered(factory, "ironOre"), Tolerance);
        Assert.AreEqual(10 * share, s.Delivered(s.One("ironMine"), "energy"), Tolerance);
        Assert.AreEqual(6 * share, s.Stored(s.One("storage"), "ironIngot"), Tolerance);
    }

    [TestMethod]
    public void SolarPanelWith2MinesAndStorage()
    {
        var s = new Scenario().Map(Rows(
            "           ",
            " S+++M+++O ",
            "   +   +   ",
            "   ++M++   "));

        foreach (var mine in s.All("ironMine"))
        {
            Assert.AreEqual(2.5, s.Delivered(mine, "energy"), Tolerance);
            Assert.AreEqual(3, s.Delivered(s.One("storage"), "ironOre", mine), Tolerance);
        }
    }

    [TestMethod]
    public void DoesCalculationsWhenEverythingIsConnectedInterjoined()
    {
        var s = new Scenario().Map(Rows(
            "   W     ",
            "   +     ",
            " O+++M   ",
            "   +     ",
            "   F     "));

        var storage = s.One("storage");
        Assert.AreEqual(6, s.Stored(storage, "ironIngot"), Tolerance);
        Assert.AreEqual(0, s.Stored(storage, "energy"), Tolerance);
        Assert.AreEqual(0, s.Stored(storage, "ironOre"), Tolerance);
    }

    [TestMethod]
    public void TwoChainsThatTouchAreOneNetwork()
    {
        var s = new Scenario().Map(Rows(
            " W W     ",
            " + +     ",
            " M+M     ",
            " + +     ",
            " T T     "));

        Assert.AreSame(s.Network(s.At(1, 0)), s.Network(s.At(3, 0)));
        Assert.AreEqual(2 * 12 * 3, s.Solution(s.At(1, 0)).GoldPerMinute[s.A.Id], Tolerance);
    }

    [TestMethod]
    public void TouchingPipesOfTwoPlayersDontTrade()
    {
        // The original game traded over pipes that touched; since v1.2 what goes to others passes the Yard gate, and a
        // network is one owner's (playtest: four players' pipes made one network, and everything got slow). The
        // solver's price for a trade is in FlowSolverFacts
        var s = new Scenario();
        s.Map(" W+      ");
        s.Map("   M+T   ", s.B);

        Assert.AreNotEqual(s.Network(s.At(1, 0))!.Id, s.Network(s.At(3, 0))!.Id, "two networks");
        Assert.IsFalse(s.Solution(s.One("windMoily")).Flows.Any(t => t.Kind == FlowKind.Trade), "nothing trades over the pipes");
    }

    // ---------------------------------------------------------------- ConnectionFinderFacts

    [TestMethod]
    public void FindsAllBuildingsInAComplexConnection()
    {
        var s = new Scenario().Map(Rows(
            "    WW    ",
            "  W+++O   ",
            "  +++++   ",
            "  + +O+   ",
            "  +++++   ",
            "   +      ",
            "   +O     ",
            "   +++W   ",
            "   O      "));

        var network = s.Network(s.At(2, 1))!;
        CollectionAssert.AreEquivalent(s.Buildings.Values.Select(t => t.Id).ToArray(), network.Buildings.ToArray());
        Assert.AreEqual(21, network.Conductors.Count);
    }

    [TestMethod]
    public void BuildingsTouchingEachOtherAreNotConnected()
    {
        var s = new Scenario().Map(Rows(
            "          ",
            "  WW      ",
            "  +       ",
            "  +       ",
            "  +++OO   ",
            "   +      ",
            "   +      ",
            "   +      ",
            "   OO     "));

        var network = s.Network(s.At(2, 1))!;
        Assert.AreEqual(3, network.Buildings.Count);
        Assert.IsNull(s.Network(s.At(3, 1)));
        Assert.IsNull(s.Network(s.At(6, 4)));
        Assert.IsNull(s.Network(s.At(4, 8)));
    }

    [TestMethod]
    public void ResourcesOnlyFlowWithinOnePipeGroup()
    {
        var s = new Scenario().Map(Rows(
            "          ",
            "  W+++    ",
            "  +  +++M ",
            "  +    M  ",
            "  +++O    ",
            "   +      ",
            "   +      ",
            "   +      ",
            "   O      "));

        var wind = s.At(2, 1);
        var (mine1, mine2, storage1, storage2) = (s.At(8, 2), s.At(7, 3), s.At(5, 4), s.At(3, 8));
        // Two pipe groups meet at the wind moily: the mines on one, the storages on the other
        Assert.IsTrue(s.Linked(wind, mine1) && s.Linked(wind, mine2) && s.Linked(wind, storage1) && s.Linked(wind, storage2));
        Assert.IsTrue(s.Linked(mine1, mine2) && s.Linked(storage1, storage2));
        Assert.IsFalse(s.Linked(mine1, storage1) || s.Linked(mine2, storage2));
        // So the mines' ore can't reach the storages
        Assert.AreEqual(0, s.Stored(storage1, "ironOre") + s.Stored(storage2, "ironOre"), Tolerance);
        Assert.AreEqual(0, s.Solution(mine1).Rate(mine1.Id), Tolerance);
        Assert.AreEqual(15, s.Stored(storage1, "energy"), Tolerance);
    }

    [TestMethod]
    public void DoesNotConnectSeparatePipes()
    {
        var s = new Scenario().Map(Rows(
            "          ",
            "  W+++    ",
            "  +  +++M ",
            "  +    M  ",
            "  ++++OS  ",
            "   +   +  ",
            "   +   +  ",
            "   +   +  ",
            "   O   O  "));

        var network = s.Network(s.At(2, 1))!;
        var solar = s.At(7, 4);
        Assert.IsTrue(network.Buildings.Contains(s.At(6, 4).Id));
        Assert.IsFalse(network.Buildings.Contains(solar.Id), "the solar panel only touches the storage, not its pipe");
        CollectionAssert.AreEquivalent(new[] { solar.Id, s.At(7, 8).Id }, s.Network(solar)!.Buildings.ToArray());
    }

    [TestMethod]
    public void PipesWithoutBuildingsAreANetworkToo()
    {
        var s = new Scenario().Map(Rows(
            "          ",
            "  +       ",
            "  +       ",
            "  +       ",
            "  +++     ",
            "   +      ",
            "   +      ",
            "   +      "));

        var network = s.Game.World.NetworkAt(s.Origin.Offset(2, 1))!;
        Assert.AreEqual(9, network.Conductors.Count);
        Assert.AreEqual(0, network.Buildings.Count);
        Assert.AreEqual(0, network.Solution.Flows.Count);
    }

    [TestMethod]
    public void AMineIsBothConsumerAndProvider()
    {
        var s = new Scenario().Map(Rows(
            "          ",
            "  W       ",
            "  +       ",
            "  +       ",
            "  +++M    ",
            "   +      ",
            "   +      ",
            "   +      ",
            "   O      "));

        var mine = s.One("ironMine");
        Assert.AreEqual(10, s.Delivered(mine, "energy"), Tolerance);
        Assert.AreEqual(12, s.Delivered(s.One("storage"), "ironOre", mine), Tolerance);
        Assert.AreEqual(20, s.Delivered(s.One("storage"), "energy"), Tolerance);
    }
}
