using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>Dealt land (step 5 of the redesign, docs/playtest-redesign.html §4): needs come from the land</summary>
[TestClass]
public class DealtLandFacts
{
    private static readonly int[] Seeds = [1234, 777, 20261022];
    private static readonly int[] StartParcels = [5, 6, 9, 10];

    private static IEnumerable<(DistrictCoord District, StationRef Station, IReadOnlyList<int> Slots)> Stations()
    {
        foreach (var district in new[] { new DistrictCoord(0, 0), new DistrictCoord(1, 0), new DistrictCoord(-1, 1) })
        {
            foreach (var station in WorldLayout.Stations(district))
            {
                yield return (district, station, WorldLayout.StationSlots(station.Index));
            }
        }
    }

    /// <summary>The land goods whose deposits, beaches or marked trees lie on an estate's tiles</summary>
    private static HashSet<string> Found(EstateProfile estate)
    {
        var found = new HashSet<string>();
        for (var y = 0; y < WorldLayout.EstateSize; y++)
        {
            for (var x = 0; x < WorldLayout.EstateSize; x++)
            {
                var tile = estate.Local(x, y);
                if (tile.Deposit is "copperOre" or "coal" or "stone" or "wood")
                {
                    found.Add(tile.Deposit);
                }
                if (tile.Terrain == TerrainType.Beach)
                {
                    found.Add("sand");
                }
            }
        }
        return found;
    }

    [TestMethod]
    public void EveryStationHoldsTheFiveGoodsOnceAndNoSeatRunsAChainAlone()
    {
        foreach (var seed in Seeds)
        {
            var generator = new WorldGenerator(seed, dealtLand: true);
            foreach (var (district, station, slots) in Stations())
            {
                var goods = slots.Select(t => generator.LandOf(district, t)).ToList();
                CollectionAssert.AreEquivalent(WorldGenerator.LandGoods, goods.SelectMany(t => t).ToArray(), $"seed {seed}, {station}: all five once");
                var counts = goods.Select(t => t.Count).OrderByDescending(t => t).ToArray();
                CollectionAssert.AreEqual(slots.Count == 2 ? new[] { 3, 2 } : new[] { 2, 1, 1, 1 }, counts, $"seed {seed}, {station}");
                Assert.IsFalse(goods.Any(t => WorldGenerator.BothInputs(t.ToList())), $"seed {seed}, {station}: no seat holds both inputs of a Smelter or a Brickworks");
            }
        }
    }

    [TestMethod]
    public void NoCopies()
    {
        foreach (var seed in Seeds)
        {
            var generator = new WorldGenerator(seed, dealtLand: true);
            foreach (var (district, _, slots) in Stations())
            {
                foreach (var slot in slots)
                {
                    var estate = generator.Estate(district, slot);
                    CollectionAssert.AreEquivalent(estate.Goods.ToArray(), Found(estate).ToArray(), $"seed {seed}, {district} slot {slot}: only what it was dealt (no beach without sand)");
                }
            }
        }
    }

    [TestMethod]
    public void TheFirstGoodLiesRichBesideTheHqTheNextInTheFirstParcelsBought()
    {
        var generator = new WorldGenerator(1234, dealtLand: true);
        foreach (var (district, _, slots) in Stations())
        {
            foreach (var slot in slots)
            {
                var estate = generator.Estate(district, slot);
                for (var i = 0; i < estate.Goods.Count; i++)
                {
                    var parcels = i == 0 ? StartParcels : [i];
                    var good = estate.Goods[i];
                    var tiles = parcels.SelectMany(p =>
                    {
                        var (px, py) = WorldLayout.ParcelOrigin(p);
                        return Enumerable.Range(0, 64).Select(t => estate.Local(px + t % 8, py + t / 8));
                    }).ToList();
                    var here = good == "sand" ? tiles.Count(t => t.Terrain == TerrainType.Beach) : tiles.Count(t => t.Deposit == good);
                    Assert.IsGreaterThan(0, here, $"{district} slot {slot}: its {good} ({(i == 0 ? "start parcels" : $"parcel {i}")})");
                    if (good is "copperOre" or "coal" or "stone")
                    {
                        Assert.AreEqual(5, tiles.Where(t => t.Deposit == good).Max(t => t.Richness), $"{district} slot {slot}: rich {good}");
                    }
                }
                // Every estate: iron ore in its start parcels
                Assert.IsTrue(StartParcels.Any(p =>
                {
                    var (px, py) = WorldLayout.ParcelOrigin(p);
                    return Enumerable.Range(0, 64).Any(t => estate.Local(px + t % 8, py + t / 8).Deposit == "ironOre");
                }), $"{district} slot {slot}: iron");
            }
        }
    }

    [TestMethod]
    public void EveryKitMakesTheSeatsOwnGoodAndAboutAHundredGold()
    {
        var game = new TestGame(dealtLand: true);
        var district = new DistrictCoord(0, 0);
        var worths = new List<double>();
        foreach (var slot in Enumerable.Range(0, WorldLayout.EstatesPerDistrict).Take(12))
        {
            var company = game.NewCompany($"Seat {slot}", gold: 2000, slot: slot);
            var good = game.World.Generator.LandOf(district, slot)[0];
            var kit = company.KitBuildings.Select(game.World.Building).OfType<Building>().ToList();
            Assert.IsTrue(kit.Any(t => game.World.Content.Building(t.Type).Out.ContainsKey(good)), $"slot {slot}: a kit that makes its {good}");
            var worth = kit.Sum(t =>
            {
                var definition = game.World.Content.Building(t.Type);
                return game.World.LocationMultiplier(t, definition) * (definition.Out.Sum(o => o.Value * game.World.BasePrice(o.Key)) - definition.In.Sum(i => i.Value * game.World.BasePrice(i.Key)));
            });
            worths.Add(worth);
            Assert.IsTrue(worth is >= 85 and <= 110, $"slot {slot} ({good}): {worth:F0} g/min at base prices");
        }
        Assert.IsTrue(worths.Average() is >= 93 and <= 104, $"on average {worths.Average():F0}");
    }

    [TestMethod]
    public void ALumberCampNeedsTheOldForest()
    {
        var game = new TestGame(dealtLand: true);
        var generator = game.World.Generator;
        var district = new DistrictCoord(0, 0);
        var wood = Enumerable.Range(0, WorldLayout.EstatesPerDistrict).First(t => generator.LandOf(district, t).Contains("wood"));
        var other = Enumerable.Range(0, WorldLayout.EstatesPerDistrict).First(t => !generator.LandOf(district, t).Contains("wood") && WorldLayout.StationOf(district, t) != WorldLayout.StationOf(district, wood));
        var lumberCamp = game.World.Content.Building("lumberCamp");
        var withWood = game.NewCompany("Woody", level: 5, slot: wood);
        var without = game.NewCompany("Plain", level: 5, slot: other);

        bool CanStandSomewhere(Company company) =>
            Enumerable.Range(0, WorldLayout.EstateSize * WorldLayout.EstateSize)
                .Select(t => game.Local(company, t % WorldLayout.EstateSize, t / WorldLayout.EstateSize))
                .Any(t => game.World.CanPlace(company, lumberCamp, t).Success);

        Assert.IsTrue(CanStandSomewhere(withWood), "by its marked trees");
        Assert.IsFalse(CanStandSomewhere(without), "plain trees aren't wood");
    }
}
