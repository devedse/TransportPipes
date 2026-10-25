using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>Building in the Yard (1.4 §7): workshops at its ends, and the crew's Depot</summary>
[TestClass]
public class WorkshopFacts
{
    /// <summary>A free tile at the west end of a company's Yard, and one in its middle</summary>
    private static (TilePos End, TilePos Middle) YardTiles(TestGame game, Company company)
    {
        var yard = WorldLayout.Yard(game.EstateOf(company).Station);
        var end = yard.Tiles().First(t => t.X < yard.X0 + 8 && t.Y > yard.Y0 + 1 && !game.World.IsOccupied(t) && game.World.Tile(t).Terrain is not (TerrainType.Water or TerrainType.Mountain));
        return (end, new TilePos((yard.X0 + yard.X1) / 2, yard.Y0 + 4));
    }

    [TestMethod]
    public void WorkshopsStandAtTheYardsEndsFromLevelTen()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1e8, level: 9);
        var (end, middle) = YardTiles(game, company);
        var moily = game.World.Content.Building("windMoily");

        StringAssert.Contains(game.World.CanPlace(company, moily, end).Error, "Level 10");
        company.Level = 10;
        Assert.IsTrue(game.World.CanPlace(company, moily, end).Success, game.World.CanPlace(company, moily, end).Error);
        StringAssert.Contains(game.World.CanPlace(company, moily, middle).Error, "ends");
        Assert.IsFalse(game.World.CanPlace(company, game.World.Content.Building("lab"), end).Success, "producers and storages only");
    }

    [TestMethod]
    public void AWorkshopTradesThroughItsOwnersYardGateWithoutPipes()
    {
        var game = new TestGame(content: TestGame.LosslessContent);
        var company = game.NewCompany(gold: 1e8, level: 10);
        var post = game.TradingPost(company);
        game.ConnectToGate(company, post);
        var (end, _) = YardTiles(game, company);

        var workshop = game.Build(company, "windMoily", end);
        game.Pass(2);

        Assert.AreSame(game.World.NetworkOf(post.Id), game.World.NetworkOf(workshop.Id), "joined to its owner's gate");
        var sale = game.World.NetworkOf(workshop.Id)!.Solution.Flows.Where(t => t.From == workshop.Id && t.Kind == FlowKind.Sale).Sum(t => t.Sent);
        Assert.AreEqual(game.World.ToFlowNode(workshop)!.Out["energy"], sale, 1e-6, "its energy sells at its owner's trading post");
        Assert.AreEqual(1, game.World.WorkshopTilesUsed(company));
    }

    [TestMethod]
    public void TheDepotKeepsGoodsForTheCrewAtTheirCrewPrice()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", gold: 1e8, level: 10, slot: 0);
        var ben = game.NewCompany("Ben", gold: 1e8, level: 10, slot: 1);
        Assert.IsTrue(game.World.SetWantsFromMarket(ana.Id, false).Success);
        var yard = WorldLayout.Yard(game.EstateOf(ben).Station);
        var spot = yard.Tiles().First(t => t.X > yard.X1 - 8 && t.X < yard.X1 && t.Y > yard.Y0 + 1 && t.Y < yard.Y1 - 1
            && game.World.CanPlace(ben, game.World.Content.Building("yardDepot"), t).Success);
        game.Build(ben, "yardDepot", spot);
        game.Build(ben, "storage").Contents["planks"] = 2000;
        Assert.IsTrue(game.World.PutInDepot(ben.Id, "planks", 1000).Success);
        Assert.AreEqual(1000, game.World.CrewOf(ben)!.Depot[ben.Id]["planks"]);
        StringAssert.Contains(game.World.CanPlace(ana, game.World.Content.Building("yardDepot"), spot.Offset(0, 2)).Error, "has its Depot");

        // Ana's Wind Moily steps to level 11: its planks come from the Depot at Ben's crew price, its bricks from her Storage
        var moily = game.Build(ana, "windMoily");
        game.SetLevel(moily, 10);
        game.Build(ana, "storage").Contents["bricks"] = 1000;
        var benGold = ben.Gold;
        Assert.IsTrue(game.World.Upgrade(ana.Id, moily.Id, 1).Success);
        game.Pass(1);

        Assert.AreEqual(11, moily.Level);
        Assert.IsGreaterThan(benGold, ben.Gold, "Ben is paid for his planks");
        Assert.IsLessThan(1000, game.World.CrewOf(ben)!.Depot[ben.Id]["planks"]);
    }

    [TestMethod]
    public void AMoverTakesTheirWorkshopsAndDepotGoodsTheDepotStays()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", gold: 1e8, level: 10, slot: 0);
        var ben = game.NewCompany("Ben", gold: 1e8, level: 10, slot: 1);
        var yard = WorldLayout.Yard(game.EstateOf(ben).Station);
        var depotSpot = yard.Tiles().First(t => t.X > yard.X1 - 8 && game.World.CanPlace(ben, game.World.Content.Building("yardDepot"), t).Success);
        var depot = game.Build(ben, "yardDepot", depotSpot);
        var (end, _) = YardTiles(game, ben);
        game.Build(ben, "windMoily", end);
        var storage = game.Build(ben, "storage");
        storage.Contents["planks"] = 300;
        Assert.IsTrue(game.World.PutInDepot(ben.Id, "planks", 300).Success);
        game.Pass(1);

        Assert.IsTrue(game.World.RelocateTo(ben.Id, new EstateKey(game.EstateOf(ana).District, 4)).Success);

        Assert.AreEqual(ana.Id, depot.Owner, "the Depot stays with the crew");
        Assert.IsFalse(game.World.CrewOf(ana)!.Depot.ContainsKey(ben.Id));
        Assert.AreEqual(1, ben.Crates.Count(t => t.Type == "windMoily"), "the workshop in a crate");
        Assert.AreEqual(300, ben.Packed!.Buildings.Single(t => t.Type == "storage").Contents["planks"], 1e-6, "the planks packed in his Storage");

        Assert.IsTrue(game.World.RelocateTo(ana.Id, new EstateKey(game.EstateOf(ana).District, 8)).Success);
        Assert.IsNull(game.World.Building(depot.Id), "the last one out: the Depot goes");
    }
}
