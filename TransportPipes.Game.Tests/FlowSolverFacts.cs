using TransportPipes.Game.Simulation;

namespace TransportPipes.Game.Tests;

[TestClass]
public class FlowSolverFacts
{
    private const double Tolerance = 1e-4;

    // ---------------------------------------------------------------- Helpers

    private sealed class Net
    {
        public readonly List<FlowNode> Nodes = new();
        public readonly List<FlowLink> Links = new();
        public Dictionary<(long, string), double> SellerPrices = new();
        public Dictionary<(long, string), double> BuyerMax = new();
        public Func<long, long, string, bool> TradeAllowed = (_, _, _) => true;
        private long _nextId = 1;

        public long Building(long owner, Dictionary<string, double>? input = null, Dictionary<string, double>? output = null, double maxRate = 1, double research = 0)
        {
            var id = _nextId++;
            Nodes.Add(new FlowNode { Id = id, Owner = owner, In = input ?? new(), Out = output ?? new(), MaxRate = maxRate, ResearchPerMinute = research });
            return id;
        }

        public long Wind(long owner, double energy = 30) => Building(owner, output: new() { ["energy"] = energy });

        public long Mine(long owner) => Building(owner, new() { ["energy"] = 10 }, new() { ["ironOre"] = 12 });

        public long Factory(long owner) => Building(owner, new() { ["energy"] = 20, ["ironOre"] = 12 }, new() { ["ironIngot"] = 6 });

        public long Storage(long owner, bool canAccept = true, params string[] contents)
        {
            var id = _nextId++;
            Nodes.Add(new FlowNode { Id = id, Owner = owner, Kind = FlowNodeKind.Storage, StorageCanAccept = canAccept, StorageContents = contents.ToHashSet() });
            return id;
        }

        public long TradingPost(long owner, double throughput = 600, params string[] imports)
        {
            var id = _nextId++;
            Nodes.Add(new FlowNode { Id = id, Owner = owner, Kind = FlowNodeKind.TradingPost, Throughput = throughput, Imports = imports.ToHashSet() });
            return id;
        }

        /// <summary>Connects all these nodes to each other (one pipe group)</summary>
        public void Group(params long[] ids) => PipeGroup(1, 1_000_000, ids);

        public void PipeGroup(double efficiency, double capacity, params long[] ids)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                for (var j = i + 1; j < ids.Length; j++)
                {
                    Links.Add(new FlowLink(ids[i], ids[j], efficiency, capacity, []));
                }
            }
        }

        public FlowSolution Solve()
        {
            var market = new FlowMarket
            {
                HubBuyPrice = r => r switch { "energy" => 1, "ironOre" => 3, "ironIngot" => 14, _ => 1 },
                HubSellPrice = r => 2.5 * (r switch { "energy" => 1, "ironOre" => 3, "ironIngot" => 14, _ => 1 }),
                SellerPrice = (owner, r) => SellerPrices.TryGetValue((owner, r), out var p) ? p : 1.75,
                BuyerMaxPrice = (owner, r) => BuyerMax.TryGetValue((owner, r), out var p) ? p : 100,
                TradeAllowed = TradeAllowed,
            };
            var solution = FlowSolver.Solve(Nodes, Links, market);
            AssertConsistent(solution);
            return solution;
        }

        public double Delivered(FlowSolution solution, long to, string resource) => solution.Flows.Where(t => t.To == to && t.Resource == resource).Sum(t => t.Delivered);

        public double Sent(FlowSolution solution, long from, string resource) => solution.Flows.Where(t => t.From == from && t.Resource == resource).Sum(t => t.Sent);

        /// <summary>Every building consumes exactly what it gets and places exactly what it makes</summary>
        private void AssertConsistent(FlowSolution solution)
        {
            foreach (var node in Nodes.Where(t => t.Kind == FlowNodeKind.Building))
            {
                var rate = solution.Rate(node.Id);
                Assert.IsTrue(rate >= -1e-9 && rate <= node.MaxRate + 1e-9, $"rate of {node} out of range: {rate}");
                foreach (var (resource, amount) in node.In)
                {
                    var delivered = Delivered(solution, node.Id, resource);
                    Assert.AreEqual(amount * rate, delivered, Tolerance, $"{node} consumes {amount * rate} {resource} but gets {delivered}");
                }
                foreach (var (resource, amount) in node.Out)
                {
                    var sent = Sent(solution, node.Id, resource);
                    Assert.AreEqual(amount * rate, sent, Tolerance, $"{node} makes {amount * rate} {resource} but sends {sent}");
                }
            }
        }
    }

    // ---------------------------------------------------------------- Basics

    [TestMethod]
    public void AWindMoilyFillsAStorage()
    {
        var net = new Net();
        var wind = net.Wind(1);
        var storage = net.Storage(1);
        net.Group(wind, storage);

        var solution = net.Solve();

        Assert.AreEqual(1, solution.Rate(wind), Tolerance);
        Assert.AreEqual(30, solution.StorageNet[storage]["energy"], Tolerance);
    }

    [TestMethod]
    public void WithoutABuyerAProducerIdles()
    {
        var net = new Net();
        var wind = net.Wind(1);
        var fullStorage = net.Storage(1, canAccept: false);
        net.Group(wind, fullStorage);

        var solution = net.Solve();

        Assert.AreEqual(0, solution.Rate(wind), Tolerance);
        Assert.IsEmpty(solution.Flows);
    }

    [TestMethod]
    public void AStorageDoesNotStarveYourOwnFactories()
    {
        // The original algorithm gave the storage 3/4 of the energy here and ran the mine at 75%
        var net = new Net();
        var wind = net.Wind(1);
        var mine = net.Mine(1);
        var storage = net.Storage(1);
        net.Group(wind, mine, storage);

        var solution = net.Solve();

        Assert.AreEqual(1, solution.Rate(mine), Tolerance);
        Assert.AreEqual(10, net.Delivered(solution, mine, "energy"), Tolerance);
        Assert.AreEqual(20, solution.StorageNet[storage]["energy"], Tolerance);
        Assert.AreEqual(12, solution.StorageNet[storage]["ironOre"], Tolerance);
    }

    [TestMethod]
    public void AFullChainSellsIngotsAtTheTradingPost()
    {
        var net = new Net();
        var wind = net.Wind(1);
        var mine = net.Mine(1);
        var factory = net.Factory(1);
        var post = net.TradingPost(1);
        net.Group(wind, mine, factory, post);

        var solution = net.Solve();

        Assert.AreEqual(1, solution.Rate(wind), Tolerance);
        Assert.AreEqual(1, solution.Rate(mine), Tolerance);
        Assert.AreEqual(1, solution.Rate(factory), Tolerance);
        Assert.AreEqual(6, net.Delivered(solution, post, "ironIngot"), Tolerance);
        // 6 ingots at 14 g, the energy is used up by the mine and the factory
        Assert.AreEqual(6 * 14, solution.GoldPerMinute[1], Tolerance);
    }

    [TestMethod]
    public void ScarceEnergyIsSharedAndLimitsTheChain()
    {
        var net = new Net();
        var wind = net.Wind(1, energy: 20);
        var mine = net.Mine(1);
        var factory = net.Factory(1);
        var post = net.TradingPost(1);
        net.Group(wind, mine, factory, post);

        var solution = net.Solve();

        // Per 6 ingots the chain needs 30 energy (10 mine + 20 factory); with 20 energy it runs at 2/3
        Assert.AreEqual(2.0 / 3, solution.Rate(factory), 1e-3);
        Assert.AreEqual(2.0 / 3, solution.Rate(mine), 1e-3);
        Assert.AreEqual(4, net.Delivered(solution, post, "ironIngot"), 1e-2);
    }

    [TestMethod]
    public void LeftoverInputsAreNotSpilled()
    {
        // The original algorithm could lose the ore that a factory couldn't use
        var net = new Net();
        var wind = net.Wind(1, energy: 10);
        var mine = net.Building(1, output: new() { ["ironOre"] = 12 });
        var factory = net.Factory(1);
        var storage = net.Storage(1);
        net.Group(wind, mine, factory, storage);

        var solution = net.Solve();

        Assert.AreEqual(0.5, solution.Rate(factory), Tolerance);
        Assert.AreEqual(1, solution.Rate(mine), Tolerance);
        Assert.AreEqual(6, solution.StorageNet[storage]["ironOre"], Tolerance);
    }

    [TestMethod]
    public void ABuildingLimitedByOneInputLeavesTheOtherInputToOthers()
    {
        // A can only run at 25% because of its ore, so it asks for 5 energy, B asks for 20. The 20 energy is shared in
        // proportion (4 and 16) and nothing is sold to the hub while an own building still wants energy.
        var net = new Net();
        var wind = net.Wind(1, energy: 20);
        var oreSource = net.Building(1, output: new() { ["ironOre"] = 3 });
        var a = net.Factory(1);
        var b = net.Building(1, new() { ["energy"] = 20 }, new() { ["glass"] = 5 });
        var post = net.TradingPost(1);
        net.Group(wind, a, b, post);
        net.Group(oreSource, a);

        var solution = net.Solve();

        Assert.AreEqual(0.2, solution.Rate(a), 1e-3);
        Assert.AreEqual(0.8, solution.Rate(b), 1e-3);
        Assert.AreEqual(0, net.Delivered(solution, post, "energy"), 1e-3);
    }

    [TestMethod]
    public void ResourcesNeverPassThroughABuilding()
    {
        // Wind -- Mine -- Factory: the wind moily is not on the factory's pipe, so the factory gets no energy
        var net = new Net();
        var wind = net.Wind(1);
        var mine = net.Mine(1);
        var factory = net.Factory(1);
        var post = net.TradingPost(1);
        net.Group(wind, mine);
        net.Group(mine, factory, post);

        var solution = net.Solve();

        Assert.AreEqual(1, solution.Rate(mine), Tolerance);
        Assert.AreEqual(0, solution.Rate(factory), Tolerance);
        Assert.AreEqual(12, net.Delivered(solution, post, "ironOre"), Tolerance);
    }

    // ---------------------------------------------------------------- Trade between companies

    [TestMethod]
    public void NeighboursPayTheSellersPrice()
    {
        var net = new Net();
        var wind = net.Wind(1);
        var mine = net.Mine(2);
        var post = net.TradingPost(2);
        net.Group(wind, mine, post);
        net.SellerPrices[(1, "energy")] = 1.75;

        var solution = net.Solve();

        Assert.AreEqual(10, net.Delivered(solution, mine, "energy"), Tolerance);
        Assert.IsTrue(solution.Flows.Any(t => t.Kind == FlowKind.Trade && t.From == wind && t.To == mine));
        // Company 1 earns 10 * 1.75; company 2 pays that and sells 12 ore at 3
        Assert.AreEqual(17.5, solution.GoldPerMinute[1], Tolerance);
        Assert.AreEqual(-17.5 + 36, solution.GoldPerMinute[2], Tolerance);
    }

    [TestMethod]
    public void OwnBuildingsAreServedBeforeNeighbours()
    {
        var net = new Net();
        var wind = net.Wind(1, energy: 15);
        var ownMine = net.Mine(1);
        var neighbourMine = net.Mine(2);
        var post1 = net.TradingPost(1);
        var post2 = net.TradingPost(2);
        net.Group(wind, ownMine, neighbourMine, post1, post2);

        var solution = net.Solve();

        Assert.AreEqual(1, solution.Rate(ownMine), Tolerance);
        Assert.AreEqual(0.5, solution.Rate(neighbourMine), Tolerance);
    }

    [TestMethod]
    public void NoTradeAboveTheBuyersMaximumPrice()
    {
        var net = new Net();
        var wind = net.Wind(1);
        var mine = net.Mine(2);
        var post1 = net.TradingPost(1);
        var post2 = net.TradingPost(2);
        net.Group(wind, mine, post1, post2);
        net.SellerPrices[(1, "energy")] = 2.5;
        net.BuyerMax[(2, "energy")] = 2;

        var solution = net.Solve();

        Assert.AreEqual(0, solution.Rate(mine), Tolerance);
        Assert.AreEqual(30, net.Delivered(solution, post1, "energy"), Tolerance);
    }

    [TestMethod]
    public void NoTradeWhenItIsNotAllowed()
    {
        var net = new Net();
        var wind = net.Wind(1);
        var mine = net.Mine(2);
        var post1 = net.TradingPost(1);
        net.Group(wind, mine, post1);
        net.TradeAllowed = (_, _, _) => false;

        var solution = net.Solve();

        Assert.AreEqual(0, solution.Rate(mine), Tolerance);
    }

    [TestMethod]
    public void TheCheapestSellerIsUsedFirst()
    {
        var net = new Net();
        var cheap = net.Wind(1);
        var expensive = net.Wind(2);
        var mine = net.Mine(3);
        var post3 = net.TradingPost(3);
        net.Group(cheap, expensive, mine, post3);
        net.SellerPrices[(1, "energy")] = 1.2;
        net.SellerPrices[(2, "energy")] = 2.0;

        var solution = net.Solve();

        Assert.AreEqual(10, net.Sent(solution, cheap, "energy"), Tolerance);
        Assert.AreEqual(0, net.Sent(solution, expensive, "energy"), Tolerance);
        Assert.AreEqual(0, solution.Rate(expensive), Tolerance);
    }

    [TestMethod]
    public void StoragesAndTradingPostsOnlyServeTheirOwner()
    {
        var net = new Net();
        var wind = net.Wind(1);
        var neighbourStorage = net.Storage(2);
        var neighbourPost = net.TradingPost(2);
        net.Group(wind, neighbourStorage, neighbourPost);

        var solution = net.Solve();

        Assert.AreEqual(0, solution.Rate(wind), Tolerance);
    }

    // ---------------------------------------------------------------- Pipes

    [TestMethod]
    public void LossesReduceWhatArrives()
    {
        var net = new Net();
        var wind = net.Wind(1);
        var mine = net.Mine(1);
        var post = net.TradingPost(1);
        net.PipeGroup(0.9, 1000, wind, mine);
        net.Group(mine, post);

        var solution = net.Solve();

        // The mine still gets 10 energy, the wind moily sends 10/0.9
        Assert.AreEqual(10, net.Delivered(solution, mine, "energy"), Tolerance);
        Assert.AreEqual(10 / 0.9, net.Sent(solution, wind, "energy"), Tolerance);
    }

    [TestMethod]
    public void ThroughputLimitsAConnection()
    {
        var net = new Net();
        var wind = net.Wind(1, energy: 300);
        var post = net.TradingPost(1);
        net.PipeGroup(1, 100, wind, post);

        var solution = net.Solve();

        Assert.AreEqual(100, net.Delivered(solution, post, "energy"), Tolerance);
        Assert.AreEqual(1.0 / 3, solution.Rate(wind), Tolerance);
    }

    // ---------------------------------------------------------------- Storage and hub

    [TestMethod]
    public void StoragesSupplyWhenProductionFallsShort()
    {
        var net = new Net();
        var storage = net.Storage(1, true, "ironOre", "energy");
        var factory = net.Factory(1);
        var post = net.TradingPost(1);
        net.Group(storage, factory, post);

        var solution = net.Solve();

        Assert.AreEqual(1, solution.Rate(factory), Tolerance);
        Assert.AreEqual(-12, solution.StorageNet[storage]["ironOre"], Tolerance);
        Assert.AreEqual(-20, solution.StorageNet[storage]["energy"], Tolerance);
    }

    [TestMethod]
    public void TradingPostsImportWhenEnabled()
    {
        var net = new Net();
        var wind = net.Wind(1);
        var factory = net.Factory(1);
        var post = net.TradingPost(1, 600, "ironOre");
        net.Group(wind, factory, post);

        var solution = net.Solve();

        Assert.AreEqual(1, solution.Rate(factory), Tolerance);
        // Sells 6 ingots (84) and 10 surplus energy (10), imports 12 ore at 7.5
        Assert.AreEqual(84 + 10 - 12 * 7.5, solution.GoldPerMinute[1], Tolerance);
    }

    [TestMethod]
    public void TradingPostThroughputIsShared()
    {
        var net = new Net();
        var wind = net.Wind(1, energy: 500);
        var other = net.Building(1, output: new() { ["ironOre"] = 500 });
        var post = net.TradingPost(1, 600);
        net.Group(wind, other, post);

        var solution = net.Solve();

        Assert.AreEqual(600, net.Delivered(solution, post, "energy") + net.Delivered(solution, post, "ironOre"), Tolerance);
    }

    [TestMethod]
    public void LabsMakeResearchPoints()
    {
        var net = new Net();
        var wind = net.Wind(1);
        var storage = net.Storage(1, true, "ironIngot");
        var lab = net.Building(1, new() { ["energy"] = 10, ["ironIngot"] = 2 }, research: 5);
        net.Group(wind, storage, lab);

        var solution = net.Solve();

        Assert.AreEqual(1, solution.Rate(lab), Tolerance);
        Assert.AreEqual(5, solution.ResearchPerMinute[1], Tolerance);
    }

    [TestMethod]
    public void BuildingsUnderConstructionDoNothing()
    {
        var net = new Net();
        var wind = net.Wind(1);
        var mine = net.Building(1, new() { ["energy"] = 10 }, new() { ["ironOre"] = 12 }, maxRate: 0);
        var post = net.TradingPost(1);
        net.Group(wind, mine, post);

        var solution = net.Solve();

        Assert.AreEqual(0, solution.Rate(mine), Tolerance);
        Assert.AreEqual(30, net.Delivered(solution, post, "energy"), Tolerance);
    }

    [TestMethod]
    public void TheResultDoesNotDependOnTheOrderOfTheBuildings()
    {
        Dictionary<string, double> Run(bool reverse)
        {
            var net = new Net();
            var ids = new[]
            {
                net.Wind(1, 25), net.Wind(2, 30), net.Mine(1), net.Mine(2), net.Factory(1), net.Factory(2),
                net.TradingPost(1), net.TradingPost(2), net.Storage(1),
            };
            if (reverse)
            {
                net.Nodes.Reverse();
            }
            net.Group(ids);
            var solution = net.Solve();
            return ids.ToDictionary(t => t.ToString(), t => Math.Round(solution.Rate(t), 4));
        }

        CollectionAssert.AreEquivalent(Run(false).ToList(), Run(true).ToList());
    }
}
