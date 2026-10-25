using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class DeepFacts
{
    private static (TestGame Game, Company Company, Building Lift) WithLift()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 30);
        company.Researched.Add("deepLift");
        var pos = game.Local(company, 2, 2);
        var lift = new Building { Id = game.World.NextBuildingId, Type = "deepLift", Owner = company.Id, X = pos.X, Y = pos.Y, Size = 2, ConstructionEndsAt = game.Time.AddSeconds(1) };
        game.World.LoadBuilding(lift);
        game.World.FinishConstruction(lift.Id);
        return (game, company, lift);
    }

    [TestMethod]
    public void AFinishedDeepLiftReachesDownWithAShaftInAPocket()
    {
        var (game, company, lift) = WithLift();

        var bottom = WorldLayout.Deep(lift.Position);
        var shaft = game.World.BuildingAt(bottom)!;
        Assert.AreEqual("liftShaft", shaft.Type);
        Assert.AreSame(lift, game.World.LiftPartner(shaft));
        Assert.AreEqual(TerrainType.Cavern, game.World.Tile(bottom).Terrain);
        Assert.AreEqual(TerrainType.Rock, game.World.Tile(bottom.Offset(3, 0)).Terrain);
        Assert.AreEqual(0, game.World.Tile(bottom).Wind, "no wind down here");
        StringAssert.Contains(game.World.Demolish(company.Id, shaft.Id).Error, "surface");
        StringAssert.Contains(game.World.Move(company.Id, lift.Id, game.Local(company, 10, 10)).Error, "can't be moved");

        Assert.IsTrue(game.World.Demolish(company.Id, lift.Id).Success);
        Assert.IsNull(game.World.BuildingAt(bottom), "the shaft goes with its lift");
    }

    [TestMethod]
    public void DiggingAParcelTakesHoursAndBringsUpStoneAndOre()
    {
        var (game, company, lift) = WithLift();
        game.Map(company, game.Local(company, 8, 2), "O");
        var estate = game.EstateOf(company);
        var parcel = estate.ParcelAt(lift.Position);
        var far = WorldLayout.ParcelCount - 1;

        StringAssert.Contains(game.World.Dig(company.Id, far).Error, "next to");
        var gold = company.Gold;
        Assert.IsTrue(game.World.Dig(company.Id, parcel).Success);
        Assert.AreEqual(gold - GameWorld.DigCost(0), company.Gold);
        StringAssert.Contains(game.World.Dig(company.Id, parcel + 1).Error, "still digging");
        Assert.AreEqual(TerrainType.Rock, game.World.Tile(WorldLayout.Deep(lift.Position).Offset(3, 3)).Terrain);

        game.Pass(GameWorld.DigTime(0).TotalMinutes + 1);

        Assert.AreEqual(1 << parcel, company.DeepParcels);
        Assert.IsNull(company.DigParcel);
        Assert.AreEqual(TerrainType.Cavern, game.World.Tile(WorldLayout.Deep(lift.Position).Offset(3, 3)).Terrain);
        var storage = game.World.BuildingAt(game.Local(company, 8, 2))!;
        Assert.AreEqual(GameWorld.DigTime(0).TotalMinutes * GameWorld.DigStonePerMinute, storage.Contents["stone"], 1e-6);
        Assert.IsTrue(game.World.Dig(company.Id, parcel + 1).Success, "next to a dug out parcel");
        Assert.AreEqual(GameWorld.DigTime(1), company.DigEndsAt - company.DigStartedAt);
        Assert.IsGreaterThan(GameWorld.DigTime(0), GameWorld.DigTime(1));
        Assert.IsGreaterThan(TimeSpan.FromDays(3), GameWorld.DigTime(15), "days for the last ones");
    }

    [TestMethod]
    public void DeepBuildingsStayDownAndSurfaceBuildingsUp()
    {
        var (game, company, lift) = WithLift();
        company.DeepParcels = 0xFFFF;
        var estate = game.EstateOf(company);
        var magmaTap = game.World.Content.Building("magmaTap");

        StringAssert.Contains(game.World.CanPlace(company, magmaTap, game.Local(company, 10, 10)).Error, "Deep Level");
        StringAssert.Contains(game.World.CanPlace(company, game.World.Content.Building("windMoily"), WorldLayout.Deep(game.Local(company, 10, 10))).Error, "Deep Level");
        StringAssert.Contains(game.World.CanPlace(company, game.World.Content.Building("storage"), WorldLayout.Deep(game.Local(company, 40, 10))).Error, "your own plot");

        // Rich veins of metal and magma down there
        var deposits = new Dictionary<string, int>();
        for (var y = 0; y < WorldLayout.EstateSize; y++)
        {
            for (var x = 0; x < WorldLayout.EstateSize; x++)
            {
                var tile = game.World.Tile(WorldLayout.Deep(estate.Origin.Offset(x, y)));
                if (tile.Deposit is { } deposit)
                {
                    Assert.IsGreaterThanOrEqualTo(3, tile.Richness);
                    deposits[deposit] = deposits.GetValueOrDefault(deposit) + 1;
                }
            }
        }
        Assert.IsGreaterThanOrEqualTo(3, deposits.Count, string.Join(", ", deposits));
        var magma = Enumerable.Range(0, WorldLayout.EstateSize * WorldLayout.EstateSize)
            .Select(i => WorldLayout.Deep(estate.Origin.Offset(i % WorldLayout.EstateSize, i / WorldLayout.EstateSize)))
            .FirstOrDefault(t => game.World.CanPlace(company, magmaTap, t).Success);
        Assert.AreNotEqual(default, magma, "somewhere for a Magma Tap");
        // The steel for it
        game.Map(company, game.Local(company, 20, 2), "O")[(0, 0)].Contents["steel"] = 1000;
        var placed = game.World.PlaceBuilding(company.Id, "magmaTap", magma);
        Assert.IsTrue(placed.Success, placed.Error);
        Assert.IsTrue(game.World.PlaceBuilding(company.Id, "crystalGrotto", WorldLayout.Deep(lift.Position).Offset(-1, -1)).Success);
    }

    [TestMethod]
    public void EnergyFromTheDeepComesUpTheLiftWithoutLossesAsFastAsTheLiftAllows()
    {
        var (game, company, lift) = WithLift();
        company.DeepParcels = 0xFFFF;
        var bottom = WorldLayout.Deep(lift.Position);
        // Down: a Magma Tap with steel pipes to the shaft (they would lose a little on the surface). Up: the lift into a storage.
        var tap = new Building { Id = game.World.NextBuildingId, Type = "magmaTap", Owner = company.Id, X = bottom.X + 8, Y = bottom.Y, Size = 2 };
        game.World.LoadBuilding(tap);
        game.Map(company, bottom.Offset(2, 0), "++++++", pipeTier: 3);
        game.Map(company, lift.Position.Offset(2, 0), "+O");
        game.Pass(1);

        var stored = game.World.NetworkOf(tap.Id)!.Solution.Flows.Where(t => t.Kind == FlowKind.ToStorage).Sum(t => t.Sent);
        Assert.AreEqual(game.World.Content.Building("deepLift").Throughput, stored, 1e-6, "the lift is the bottleneck");

        Assert.IsTrue(game.World.Upgrade(company.Id, lift.Id, 1).Success);
        game.Pass(1);
        stored = game.World.NetworkOf(tap.Id)!.Solution.Flows.Where(t => t.Kind == FlowKind.ToStorage).Sum(t => t.Sent);
        Assert.AreEqual(game.World.Content.Building("magmaTap").Out["energy"], stored, 1e-6, "everything, without losses");
    }

    [TestMethod]
    public void TheDeepLevelMovesAlongWithTheCompany()
    {
        var (game, company, lift) = WithLift();
        company.DeepParcels = 0b11;
        var old = game.EstateOf(company).Origin;
        var oldSide = WorldLayout.SideOf(game.EstateOf(company).Slot);
        var bottom = WorldLayout.Deep(lift.Position);
        var vault = new Building { Id = game.World.NextBuildingId, Type = "vault", Owner = company.Id, X = bottom.X + 3, Y = bottom.Y, Size = 2 };
        game.World.LoadBuilding(vault);

        Assert.IsTrue(game.World.Relocate(company.Id, MovingFacts.OtherDistrict(game, company)).Success);
        Assert.IsNull(game.World.BuildingAt(bottom), "the shaft is packed with its lift");
        Assert.IsNull(game.World.Building(vault.Id));
        game.Pass(61);

        var estate = game.World.EstateOf(company)!;
        Assert.AreNotEqual(old, estate.Origin);
        // The caverns come along, turned to face the new Yard like the plot
        var turned = WorldLayout.MoveParcels(oldSide, WorldLayout.SideOf(estate.Slot), 0b11);
        Assert.AreEqual(turned, company.DeepParcels, "the caverns come along");
        var cavern = WorldLayout.ParcelOrigin(System.Numerics.BitOperations.TrailingZeroCount(turned));
        Assert.AreEqual(TerrainType.Cavern, game.World.Tile(WorldLayout.Deep(estate.Origin.Offset(cavern.X + 2, cavern.Y + 2))).Terrain);
        Assert.AreEqual(TerrainType.Rock, game.World.Tile(WorldLayout.Deep(old.Offset(10, 2))).Terrain);
        if (game.World.BuildingsOf(company.Id).SingleOrDefault(t => t.Type == "deepLift") is { } moved)
        {
            Assert.AreEqual("liftShaft", game.World.BuildingAt(WorldLayout.Deep(moved.Position))!.Type);
        }
        Assert.IsTrue(game.World.BuildingsOf(company.Id).Any(t => t.Type == "vault") || company.Crates.Any(t => t.Type == "vault"));
        Assert.IsFalse(company.Crates.Any(t => t.Type == "liftShaft"));
    }
}
