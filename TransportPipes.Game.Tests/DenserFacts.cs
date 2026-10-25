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

    [TestMethod]
    public void AWindMoilyEvolvesIntoABigTurbineWhereItStandsKeepingItsPipes()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 30);
        game.World.Era = 2;
        company.Researched.Add("bigTurbine");
        var origin = game.InStart(company, 2, 2);
        var map = game.Map(company, origin, "W+++\n    ");
        var moily = map[(0, 0)];
        moily.Level = 5;
        var plan = game.World.EvolvePlan(company, moily);
        Assert.IsNull(plan.Problem, plan.Problem);
        var bigTurbine = game.World.Content.Building("bigTurbine");
        Assert.AreEqual(Math.Max(0, game.World.BuildCost(company, bigTurbine) - game.World.EvolveCredit(moily)), plan.Cost, 1e-6);
        Assert.IsGreaterThan(game.World.Content.Building("windMoily").Cost.Gold, game.World.EvolveCredit(moily), "the upgrades count too");
        // Its materials, in a storage
        var storage = game.Map(company, game.InStart(company, 12, 12), "O")[(0, 0)];
        foreach (var (material, amount) in bigTurbine.Cost.Materials)
        {
            storage.Contents[material] = amount;
        }
        var gold = company.Gold;

        var evolved = game.World.Evolve(company.Id, moily.Id);
        Assert.IsTrue(evolved.Success, evolved.Error);

        Assert.IsNull(game.World.Building(moily.Id));
        var turbine = game.World.BuildingAt(origin)!;
        Assert.AreEqual("bigTurbine", turbine.Type);
        Assert.IsTrue(turbine.Footprint().Contains(origin), "it stands where the moily stood");
        Assert.AreEqual(gold - plan.Cost, company.Gold, 1e-6);
        Assert.IsNotNull(game.World.ConductorAt(origin.Offset(2, 0)), "the pipes stay");
    }

    [TestMethod]
    public void EvolvingNeedsTheSuccessorUnlockedAndRoomAround()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 30);
        var origin = game.Local(company, 10, 10);
        // Hemmed in on every side
        var map = game.Map(company, origin.Offset(-1, -1), "SSS\nSWS\nSSS");
        var moily = map[(1, 1)];

        StringAssert.Contains(game.World.EvolvePlan(company, moily).Problem, "Era");
        game.World.Era = 2;
        company.Researched.Add("bigTurbine");
        StringAssert.Contains(game.World.EvolvePlan(company, moily).Problem, "Make room");
        StringAssert.Contains(game.World.Evolve(company.Id, map[(0, 0)].Id).Error, "Era 3", "solar panels evolve into Solar Farms (Era 3)");
    }

    [TestMethod]
    public void WithARecyclerDemolishingGivesAllTheGoldBack()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 30);
        var moily = game.Map(company, game.Local(company, 10, 10), "W")[(0, 0)];
        var other = game.Map(company, game.Local(company, 12, 10), "W")[(0, 0)];
        var cost = game.World.Content.Building("windMoily").Cost.Gold;

        var gold = company.Gold;
        Assert.IsTrue(game.World.Demolish(company.Id, moily.Id).Success);
        Assert.AreEqual(gold + Math.Round(cost * 0.5), company.Gold, 1e-6);
        game.World.LoadBuilding(new Building { Id = game.World.NextBuildingId, Type = "recycler", Owner = company.Id, X = game.Local(company, 20, 20).X, Y = game.Local(company, 20, 20).Y });
        gold = company.Gold;
        Assert.IsTrue(game.World.Demolish(company.Id, other.Id).Success);
        Assert.AreEqual(gold + cost, company.Gold, 1e-6);
    }
}
