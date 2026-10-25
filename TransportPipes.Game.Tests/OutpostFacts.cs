using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class OutpostFacts
{
    /// <summary>The first place in the wilderness around the company's district where an outpost can be claimed</summary>
    private static TilePos Claimable(TestGame game, Company company)
    {
        var x0 = WorldLayout.DistrictOriginX(company.District.X);
        var y0 = WorldLayout.DistrictOriginY(company.District.Y);
        for (var y = y0 - WorldLayout.WildernessWidth; y < y0 + WorldLayout.DistrictHeight + WorldLayout.WildernessWidth; y++)
        {
            for (var x = x0 - WorldLayout.WildernessWidth; x < x0 + WorldLayout.DistrictWidth + WorldLayout.WildernessWidth; x++)
            {
                if (game.World.OutpostBlocker(company, new TilePos(x, y)) == null)
                {
                    return new TilePos(x, y);
                }
            }
        }
        throw new AssertFailedException("No place for an outpost");
    }

    [TestMethod]
    public void AnOutpostIsClaimedOnARichWildernessDeposit()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 14);
        var estate = game.EstateOf(company).Origin;
        StringAssert.Contains(game.World.ClaimOutpost(company.Id, estate).Error, "Level 15");

        company.Level = 15;
        StringAssert.Contains(game.World.ClaimOutpost(company.Id, estate).Error, "wilderness");
        var spot = Claimable(game, company);
        Assert.IsTrue(game.World.ClaimOutpost(company.Id, spot).Success);
        Assert.AreEqual(1_000_000 - game.World.OutpostLease(company), company.Gold);
        StringAssert.Contains(game.World.ClaimOutpost(company.Id, spot).Error, "claimed already");

        // Buildings go up in the outpost, not next to it
        var inside = Enumerable.Range(0, GameWorld.OutpostSize * GameWorld.OutpostSize)
            .Select(i => spot.Offset(i % GameWorld.OutpostSize, i / GameWorld.OutpostSize))
            .First(t => game.World.CanPlace(company, game.World.Content.Building("storage"), t).Success);
        Assert.IsTrue(game.World.PlaceBuilding(company.Id, "storage", inside).Success);
        StringAssert.Contains(game.World.CanPlace(company, game.World.Content.Building("storage"), spot.Offset(-1, 0)).Error, "your own plot");

        var other = game.NewCompany("Neighbour", level: 15);
        StringAssert.Contains(game.World.CanPlace(other, game.World.Content.Building("storage"), inside.Offset(0, 1)).Error, "someone else's outpost");
    }

    [TestMethod]
    public void TheLeaseIsPaidEveryWeekOrTheLandGoesBack()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 15);
        var spot = Claimable(game, company);
        game.World.ClaimOutpost(company.Id, spot);
        var lease = game.World.OutpostLease(company);
        var gold = company.Gold;

        game.Pass(7 * 24 * 60 + 1);
        Assert.AreEqual(gold - lease, company.Gold, 1e-6);
        Assert.HasCount(1, game.World.OutpostsOf(company.Id));

        company.Gold = 0;
        game.Pass(7 * 24 * 60 + 1);
        Assert.IsEmpty(game.World.OutpostsOf(company.Id));
    }

    [TestMethod]
    public void CargoDepotsBringTheGoodsHomeWithoutLosses()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 15);
        var spot = Claimable(game, company);
        Assert.IsTrue(game.World.ClaimOutpost(company.Id, spot).Success);
        var outpost = game.Map(company, spot.Offset(1, 1), "WC");
        var home = game.Map(company, game.Local(company, 1, 1), "OC");

        var storage = home[(0, 0)];
        var network = game.World.NetworkOf(storage.Id)!;
        Assert.AreEqual(network, game.World.NetworkOf(outpost[(0, 0)].Id), "the trucks join the networks");
        var stored = network.Solution.Flows.Single(t => t.Kind == FlowKind.ToStorage);
        Assert.AreEqual(stored.Sent, stored.Delivered, 1e-9);
        Assert.AreEqual(game.World.Content.Building("windMoily").Out["energy"], stored.Delivered, 1e-9, "the moily's energy reaches the storage at home");
    }

    [TestMethod]
    public void AnOutpostFeedsTheYardThroughTheCargoDepots()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 15);
        var neighbour = game.NewCompany("Neighbour", level: 2);
        var mine = game.Build(neighbour, "ironMine", game.Spot(neighbour, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(neighbour, mine);
        game.Connect(neighbour, mine, game.TradingPost(neighbour));
        var spot = Claimable(game, company);
        Assert.IsTrue(game.World.ClaimOutpost(company.Id, spot).Success);
        game.Map(company, spot.Offset(1, 1), "WC");
        var depot = game.Build(company, "cargoDepot");
        game.ConnectToGate(company, depot);

        game.Pass(3);

        var trades = game.World.YardTrades(game.EstateOf(neighbour));
        Assert.IsTrue(trades.Any(t => !t.Selling && t.Partner == company.Id && t.Resource == "energy"),
            "the outpost's energy reaches the station's Yard: the neighbour's mine buys it");
    }

    [TestMethod]
    public void PipesCrossTheWildernessButNotSomeoneElsesOutpost()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 15);
        var spot = Claimable(game, company);
        game.World.ClaimOutpost(company.Id, spot);
        var other = game.NewCompany("Other", level: 15);

        var outside = spot.Offset(-1, 0);
        if (game.World.Tile(outside).Terrain != TerrainType.Mountain)
        {
            Assert.IsTrue(game.World.PlacePipes(other.Id, [outside]).Success, "anyone may lay pipes in the wilderness");
        }
        StringAssert.Contains(game.World.PlacePipes(other.Id, [spot.Offset(2, 2)]).Error, "someone else's outpost");
    }

    [TestMethod]
    public void ADiscoveryLeavesARichDeposit()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 15);
        var x0 = WorldLayout.DistrictOriginX(company.District.X);
        var y0 = WorldLayout.DistrictOriginY(company.District.Y);
        var pos = Enumerable.Range(4, 20).Select(d => new TilePos(x0 + 40, y0 - d))
            .First(t => WorldLayout.ZoneAt(t.X, t.Y).Kind == ZoneKind.Wilderness && game.World.Generator.Tile(t).IsBuildableTerrain);

        game.World.StartDiscovery(pos, "copperOre");

        Assert.AreEqual("copperOre", game.World.Tile(pos).Deposit);
        Assert.AreEqual(5, game.World.Tile(pos).Richness);
        Assert.AreEqual(("copperOre", 5), game.World.DepositOverride(pos));
    }
    [TestMethod]
    public void EveryStationThatBringsSomethingOutOfADiscoveryInItsFirst72HoursGetsItsMarker()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 15);
        var x0 = WorldLayout.DistrictOriginX(company.District.X);
        var y0 = WorldLayout.DistrictOriginY(company.District.Y);
        var pos = Enumerable.Range(6, 16).Select(d => new TilePos(x0 + 40, y0 - d))
            .First(t => WorldLayout.ZoneAt(t.X, t.Y).Kind == ZoneKind.Wilderness && game.World.Generator.Tile(t).IsBuildableTerrain);
        game.World.StartDiscovery(pos, "copperOre");
        var origin = Enumerable.Range(0, 64).Select(i => pos.Offset(-(i % 8), -(i / 8))).First(t => game.World.OutpostBlocker(company, t) == null);
        Assert.IsTrue(game.World.ClaimOutpost(company.Id, origin).Success);
        var mine = game.Build(company, "copperMine", Enumerable.Range(0, 64).Select(i => origin.Offset(i % 8, i / 8))
            .First(t => game.World.CanPlace(company, game.World.Content.Building("copperMine"), t).Success && game.World.Tile(t).Deposit == "copperOre"));
        var storage = game.Build(company, "storage", Enumerable.Range(0, 64).Select(i => origin.Offset(i % 8, i / 8))
            .First(t => game.World.CanPlace(company, game.World.Content.Building("storage"), t).Success && Math.Abs(t.X - mine.Position.X) + Math.Abs(t.Y - mine.Position.Y) >= 3));
        game.Connect(company, mine, storage);
        var moily = game.Build(company, "windMoily", Enumerable.Range(0, 64).Select(i => origin.Offset(i % 8, i / 8))
            .First(t => game.World.CanPlace(company, game.World.Content.Building("windMoily"), t).Success));
        game.Connect(company, moily, mine);

        game.Pass(60);

        var discovery = game.World.Discoveries.Single();
        var (stations, top) = game.World.DiscoveryMarks(discovery);
        Assert.AreEqual(game.EstateOf(company).Station, stations.Single(), "its marker");
        Assert.AreEqual(company.Id, top.Single().Id, "named among the three who brought most");
        var marked = discovery.Marks.Values.Single();
        game.Pass(72 * 60);
        var after = discovery.Marks.Values.Single();
        game.Pass(60);
        Assert.AreEqual(after, discovery.Marks.Values.Single(), 1e-9, "after 72 hours nothing more counts");
        Assert.IsGreaterThan(marked, after);
    }
}
