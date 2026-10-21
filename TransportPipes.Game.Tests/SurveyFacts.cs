using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class SurveyFacts
{
    private static IEnumerable<TilePos> Reach(TestGame game, Estate estate)
    {
        var p = game.World.Content.Progression;
        foreach (var parcel in p.StartParcels.Concat(p.ParcelOrder.Take(1)))
        {
            var (px, py) = WorldLayout.ParcelOrigin(parcel);
            for (var y = 0; y < WorldLayout.ParcelSize; y++)
            {
                for (var x = 0; x < WorldLayout.ParcelSize; x++)
                {
                    yield return estate.Origin.Offset(px + x, py + y);
                }
            }
        }
    }

    [TestMethod]
    public void EveryNewEstateHasTheStarterKitWithinReach()
    {
        var game = new TestGame(surveys: true);
        for (var i = 0; i < 12; i++)
        {
            var company = game.NewCompany($"P{i}");
            var estate = game.EstateOf(company);
            var tiles = Reach(game, estate).Select(game.World.Tile).ToList();
            foreach (var resource in GameWorld.StarterDeposits.Append("ironOre"))
            {
                Assert.IsTrue(tiles.Any(t => t.Deposit == resource), $"{company.Name}: {resource}");
            }
            Assert.IsTrue(tiles.Any(t => t.Terrain == TerrainType.Water), $"{company.Name}: a shore");
        }
    }

    [TestMethod]
    public void SurveyorsOnlyChangeFreeLandAndOnlyOnce()
    {
        // An estate from before the starter kit: buildings and pipes on it, then the survey
        var game = new TestGame();
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");
        game.Connect(company, moily, game.TradingPost(company));
        var estate = game.EstateOf(company);
        var occupied = Reach(game, estate).Where(t => game.World.IsOccupied(t)).ToList();
        var before = occupied.ToDictionary(t => t, game.World.Tile);

        var added = game.World.Survey(estate);

        foreach (var tile in occupied)
        {
            Assert.AreEqual(before[tile], game.World.Tile(tile), $"nothing under {tile} changed");
        }
        Assert.IsNotEmpty(added);
        Assert.IsEmpty(game.World.Survey(estate), "once");
        var messages = game.World.SurveyEstates();
        Assert.IsEmpty(messages, "already surveyed");
    }
}
