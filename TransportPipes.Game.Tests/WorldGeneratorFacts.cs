using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>The land of 1.4 §5: what every plot has, what each lacks, its strength, and the later Eras' deposits</summary>
[TestClass]
public class WorldGeneratorFacts
{
    private static readonly WorldGenerator Generator = new(1234);

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

    [TestMethod]
    public void TheWorldIsTheSameForTheSameSeed()
    {
        var other = new WorldGenerator(1234);
        for (var x = -200; x < 200; x += 7)
        {
            for (var y = -200; y < 200; y += 5)
            {
                Assert.AreEqual(Generator.Tile(x, y), other.Tile(x, y));
            }
        }
    }

    [TestMethod]
    public void TheFirstHourIsTheSameForEveryone()
    {
        // Every start area has iron ore, a windy hill, water, and its first basic
        foreach (var district in new[] { new DistrictCoord(0, 0), new DistrictCoord(1, -1) })
        {
            for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
            {
                var estate = Generator.Estate(district, slot);
                var start = Tiles(estate).Where(t => WorldLayout.StartParcels(slot).Contains(WorldLayout.ParcelOf(t.X, t.Y))).ToList();
                Assert.IsGreaterThanOrEqualTo(6, start.Count(t => t.Info.Deposit == "ironOre"), $"slot {slot}: iron ore");
                Assert.IsGreaterThanOrEqualTo(8, start.Count(t => t.Info.Terrain == TerrainType.Hills), $"slot {slot}: a hill");
                Assert.IsGreaterThanOrEqualTo(3, start.Count(t => t.Info.Terrain == TerrainType.Water), $"slot {slot}: water");
                Assert.IsTrue(start.Any(t => t.Info.Deposit is "copperOre" or "coal" or "stone" || t.Info.Terrain == TerrainType.Beach), $"slot {slot}: a basic");
            }
        }
    }

    [TestMethod]
    public void TheFourPlotsOfAYardLackDifferentThings()
    {
        foreach (var station in WorldLayout.Stations(new DistrictCoord(0, 0)).Concat(WorldLayout.Stations(new DistrictCoord(-1, 2))))
        {
            var plots = WorldLayout.StationSlots(station.Index).Select(t => Generator.Estate(station.District, t)).ToList();
            CollectionAssert.AreEquivalent(WorldGenerator.Basics, plots.Select(t => t.Lacks[0]).ToList());
            CollectionAssert.AreEquivalent(WorldGenerator.Later, plots.Select(t => t.Lacks[1]).ToList());
            // Any three of them have everything
            foreach (var left in plots)
            {
                var three = plots.Where(t => t != left).SelectMany(t => t.Goods).ToHashSet();
                Assert.IsTrue(WorldGenerator.LandGoods.All(three.Contains));
            }
        }
    }

    [TestMethod]
    public void APlotHasWhatItHasAndNotWhatItLacks()
    {
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
        {
            var estate = Generator.Estate(new DistrictCoord(0, 0), slot);
            var tiles = Tiles(estate).Select(t => t.Info).ToList();
            Assert.HasCount(9, estate.Goods, "water, iron ore, wood and six of the eight");
            foreach (var lacked in estate.Lacks)
            {
                Assert.IsFalse(estate.Goods.Contains(lacked));
                Assert.IsFalse(tiles.Any(t => t.Deposit == lacked), $"slot {slot} has no {lacked}");
            }
            if (estate.Lacks.Contains("sand"))
            {
                Assert.IsFalse(tiles.Any(t => t.Terrain == TerrainType.Beach), $"slot {slot}: no beach without sand");
            }
            foreach (var deposit in new[] { "ironOre", "copperOre", "coal", "stone", "bauxite", "oil", "rareEarth", "titaniumOre" }.Where(estate.Goods.Contains))
            {
                Assert.IsGreaterThanOrEqualTo(4, tiles.Count(t => t.Deposit == deposit), $"slot {slot}: {deposit}");
            }
            Assert.IsTrue(tiles.Count(t => t.Terrain == TerrainType.Forest) >= 12, $"slot {slot}: forest");
            Assert.IsTrue(tiles.Count(t => t.Terrain == TerrainType.Desert) >= 20, $"slot {slot}: a desert for sun");
        }
    }

    [TestMethod]
    public void EveryPlotHasARichDepositOfItsStrengthNextToItsStartArea()
    {
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
        {
            var estate = Generator.Estate(new DistrictCoord(0, 0), slot);
            Assert.IsTrue(estate.Goods.Contains(estate.Strength));
            var rich = Tiles(estate).Where(t => t.Info.Deposit == estate.Strength && t.Info.Richness >= 4).ToList();
            Assert.IsNotEmpty(rich, $"slot {slot}: a ★4 {estate.Strength}");
            var start = WorldLayout.StartParcels(slot);
            Assert.IsTrue(rich.Any(t => Neighbours(WorldLayout.ParcelOf(t.X, t.Y)).Any(start.Contains)), "next to the start area");
            Assert.IsTrue(Tiles(estate).Where(t => t.Info.Deposit != null && t.Info.Deposit != estate.Strength).All(t => t.Info.Richness <= 4), "others ★1 to ★3, their middles one more");
        }
    }

    private static IEnumerable<int> Neighbours(int parcel)
    {
        var (px, py) = (parcel % 4, parcel / 4);
        return new[] { (px - 1, py), (px + 1, py), (px, py - 1), (px, py + 1) }.Where(t => t.Item1 is >= 0 and < 4 && t.Item2 is >= 0 and < 4).Select(t => t.Item2 * 4 + t.Item1);
    }

    [TestMethod]
    public void TheWayFromTheHqAndTheTradingPostToTheGateIsOpen()
    {
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
        {
            var estate = Generator.Estate(new DistrictCoord(0, 0), slot);
            var (gx, gy) = WorldLayout.YardGateLocal(slot);
            var (hx, hy) = WorldLayout.HqLocal(slot);
            var (px, py) = WorldLayout.TradingPostLocal(slot);
            foreach (var (x, y) in new[] { (gx, gy), (hx, hy), (hx + 1, hy + 1), (px, py) })
            {
                Assert.IsTrue(estate.Local(x, y).IsBuildableTerrain, $"slot {slot}: ({x},{y})");
                Assert.IsNull(estate.Local(x, y).Deposit);
            }
        }
    }

    [TestMethod]
    public void TheOutskirtsHoldEveryLandResource()
    {
        var district = new DistrictCoord(0, 0);
        var origin = WorldLayout.DistrictOrigin(district);
        var deposits = new HashSet<string>();
        var beach = false;
        foreach (var (x, y) in WorldLayout.OutskirtsParcels(5))
        {
            for (var dy = 0; dy < 8; dy++)
            {
                for (var dx = 0; dx < 8; dx++)
                {
                    var tile = Generator.Tile(origin.X + x + dx, origin.Y + y + dy);
                    if (tile.Deposit != null)
                    {
                        deposits.Add(tile.Deposit);
                    }
                    beach |= tile.Terrain == TerrainType.Beach;
                }
            }
        }
        Assert.IsTrue(new[] { "ironOre", "copperOre", "coal", "stone", "bauxite" }.All(deposits.Contains), string.Join(", ", deposits));
        Assert.IsTrue(beach, "sand");
    }
}
