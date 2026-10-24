using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class WorldGeneratorFacts
{
    private static readonly WorldGenerator Generator = new(1234);

    [TestMethod]
    public void TheWorldIsTheSameForTheSameSeed()
    {
        var other = new WorldGenerator(1234);
        for (var x = -120; x < 120; x += 7)
        {
            for (var y = -120; y < 120; y += 5)
            {
                Assert.AreEqual(Generator.Tile(x, y), other.Tile(x, y));
            }
        }
    }

    [TestMethod]
    public void EveryEstateHasIronAndAHillInItsStartParcels()
    {
        var district = new DistrictCoord(0, 0);
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
        {
            var estate = Generator.Estate(district, slot);
            var startTiles = Tiles(estate).Where(t => new[] { 5, 6, 9, 10 }.Contains(WorldLayout.ParcelOf(t.X, t.Y))).ToList();

            Assert.IsGreaterThanOrEqualTo(5, startTiles.Count(t => t.Info.Deposit == "ironOre"), $"slot {slot} needs an iron deposit");
            Assert.IsGreaterThanOrEqualTo(6, startTiles.Count(t => t.Info.Terrain == TerrainType.Hills), $"slot {slot} needs a hill");
            Assert.IsTrue(startTiles.All(t => t.Info.Terrain != TerrainType.Water), $"slot {slot} start parcels must be dry");
        }
    }

    [TestMethod]
    public void SpecialtiesAreSpreadOverTheDistrict()
    {
        var district = new DistrictCoord(1, 0);
        var primaries = Enumerable.Range(0, WorldLayout.EstatesPerDistrict).Select(slot => Generator.Estate(district, slot).Primary).ToList();

        foreach (var specialty in WorldGenerator.AllSpecialties)
        {
            Assert.IsGreaterThanOrEqualTo(3, primaries.Count(t => t == specialty), $"{specialty} should be a primary specialty at least 3 times");
        }
    }

    [TestMethod]
    public void SpecialtiesShapeTheEstate()
    {
        var district = new DistrictCoord(0, 0);
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
        {
            var estate = Generator.Estate(district, slot);
            var tiles = Tiles(estate).Select(t => t.Info).ToList();
            foreach (var specialty in estate.Specialties)
            {
                switch (specialty)
                {
                    case EstateSpecialty.Riverside:
                        Assert.IsGreaterThan(10, tiles.Count(t => t.Terrain == TerrainType.Water));
                        Assert.IsGreaterThan(10, tiles.Count(t => t.Terrain == TerrainType.Beach));
                        break;
                    case EstateSpecialty.CopperGulch:
                        Assert.IsGreaterThan(8, tiles.Count(t => t.Deposit == "copperOre"));
                        break;
                    case EstateSpecialty.CoalValley:
                        Assert.IsGreaterThan(8, tiles.Count(t => t.Deposit == "coal"));
                        break;
                    case EstateSpecialty.SunnyFlats:
                        Assert.IsGreaterThan(80, tiles.Count(t => t.Terrain == TerrainType.Desert));
                        Assert.IsTrue(tiles.Where(t => t.Terrain == TerrainType.Desert).All(t => t.Sun >= 1.2));
                        break;
                    case EstateSpecialty.WindyHeights:
                        Assert.IsGreaterThan(80, tiles.Count(t => t.Terrain == TerrainType.Hills));
                        break;
                    case EstateSpecialty.OldForest:
                        Assert.IsGreaterThan(120, tiles.Count(t => t.Terrain == TerrainType.Forest));
                        break;
                    case EstateSpecialty.StoneQuarry:
                        Assert.IsGreaterThan(8, tiles.Count(t => t.Deposit == "stone" && t.Richness >= 4));
                        break;
                }
            }
        }
    }

    [TestMethod]
    public void TheHqSpotAndTheWayToTheYardGateAreBuildable()
    {
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
        {
            var estate = Generator.Estate(new DistrictCoord(0, 0), slot);
            for (var x = 15; x <= 18; x++)
            {
                Assert.AreEqual(TerrainType.Grass, estate.Local(x, 15).Terrain);
            }
            var (gx, gy) = WorldLayout.YardGateLocal(slot);
            Assert.IsTrue(estate.Local(gx, gy).IsBuildableTerrain, $"slot {slot}");
        }
    }

    [TestMethod]
    public void ClimateStaysWithinItsRange()
    {
        foreach (var (x, y) in new[] { (-300, 40), (0, 0), (90, 90), (500, -500), (37, 12) })
        {
            var tile = Generator.Tile(x, y);
            Assert.IsTrue(tile.Wind is >= 0.6 and <= 1.6);
            Assert.IsTrue(tile.Sun is >= 0.7 and <= 1.5);
        }
        Assert.AreEqual(TerrainType.Road, Generator.Tile(17, 0).Terrain);
        Assert.AreEqual(TerrainType.City, Generator.Tile(3, 3).Terrain);
    }

    private static IEnumerable<(int X, int Y, TileInfo Info)> Tiles(EstateProfile estate)
    {
        for (var y = 0; y < WorldLayout.EstateSize; y++)
        {
            for (var x = 0; x < WorldLayout.EstateSize; x++)
            {
                yield return (x, y, estate.Local(x, y));
            }
        }
    }
}
