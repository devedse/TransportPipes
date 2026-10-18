using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class DenserFacts
{
    [TestMethod]
    public void FourMoiliesInASquareMergeIntoAComplexWithTheOutputOfFive()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        company.Researched.Add("complexes");
        var origin = game.Local(company, 4, 4);
        game.Map(company, origin, "WW\nWW\n++\nO+");
        var moily = game.World.BuildingAt(origin)!;
        foreach (var member in new[] { origin, origin.Offset(1, 0), origin.Offset(0, 1), origin.Offset(1, 1) })
        {
            game.World.BuildingAt(member)!.ConstructionEndsAt = game.Time.AddSeconds(1);
        }

        game.Pass(1);

        var complex = game.World.BuildingAt(origin)!;
        Assert.AreEqual(2, complex.Size);
        Assert.AreEqual(4, game.World.ComplexMembers(complex));
        Assert.AreSame(complex, game.World.BuildingAt(origin.Offset(1, 1)));
        var stored = game.World.NetworkOf(complex.Id)!.Solution.Flows.Where(t => t.Kind == FlowKind.ToStorage).Sum(t => t.Sent);
        Assert.AreEqual(game.World.Content.Building("windMoily").Out["energy"] * 5, stored, 1e-6);
        Assert.AreEqual(4 * game.World.Content.Progression.UpgradeCost(game.World.Content.Building("windMoily"), 1), game.World.UpgradeCost(complex, 1), 0.01);
        StringAssert.Contains(game.World.Move(company.Id, complex.Id, origin.Offset(5, 5)).Error, "complex");
        Assert.IsNull(game.World.Building(moily.Id));
    }

    [TestMethod]
    public void WithoutTheResearchNothingMerges()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        var origin = game.Local(company, 4, 4);
        game.Map(company, origin, "WW\nWW");
        game.World.BuildingAt(origin)!.ConstructionEndsAt = game.Time.AddSeconds(1);

        game.Pass(1);

        Assert.AreEqual(1, game.World.BuildingAt(origin)!.Size);
    }

    [TestMethod]
    public void TileEfficiencyIsGoldPerTilePerHour()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        game.Map(company, game.Local(company, 1, 1), " W\nT+");
        var gold = game.World.GoldPerMinute()[company.Id];
        var tiles = game.EstateOf(company).UnlockedCount * WorldLayout.ParcelSize * WorldLayout.ParcelSize;

        Assert.AreEqual(Math.Round(gold * 60 / tiles, 2), game.World.TileEfficiency(company));
        Assert.AreEqual(company.Id, game.World.Leaderboard(LeaderboardKind.Efficiency, company.District)[0].Company);
    }

    [TestMethod]
    public void ATerraformerReshapesTheEstate()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 40);
        game.World.Era = 3;
        company.Researched.Add("terraforming");
        var tile = game.Spot(company, "windMoily");
        StringAssert.Contains(game.World.Terraform(company.Id, tile, TerraformAction.Raise).Error, "Terraformer");
        var terraformer = new Building { Id = game.World.NextBuildingId, Type = "terraformer", Owner = company.Id, X = game.Local(company, 28, 28).X, Y = game.Local(company, 28, 28).Y, Size = 2 };
        game.World.LoadBuilding(terraformer);
        var wind = game.World.Tile(tile).Wind;
        var terrain = game.World.Tile(tile).Terrain;

        if (terrain is TerrainType.Grass or TerrainType.Desert or TerrainType.Beach)
        {
            Assert.IsTrue(game.World.Terraform(company.Id, tile, TerraformAction.Raise).Success);
            Assert.AreEqual(TerrainType.Hills, game.World.Tile(tile).Terrain);
            Assert.AreEqual(wind * 1.2, game.World.Tile(tile).Wind, 1e-9, "hills catch more wind");
        }

        // Seeding a deposit takes steel
        var seed = game.Spot(company, "storage");
        StringAssert.Contains(game.World.Terraform(company.Id, seed, TerraformAction.SeedDeposit, "ironOre").Error, "steel");
        var storage = game.Map(company, game.Local(company, 26, 26), "O").Values.Single();
        storage.Contents["steel"] = 100;
        Assert.IsTrue(game.World.Terraform(company.Id, seed, TerraformAction.SeedDeposit, "ironOre").Success);
        Assert.AreEqual("ironOre", game.World.Tile(seed).Deposit);
        Assert.AreEqual(1, game.World.Tile(seed).Richness);
        Assert.IsTrue(game.World.Terraform(company.Id, seed, TerraformAction.SeedDeposit, "ironOre").Success);
        Assert.AreEqual(2, game.World.Tile(seed).Richness);
    }
}
