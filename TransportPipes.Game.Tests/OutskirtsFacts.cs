using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>The outskirts (1.4 §5): nine parcels set aside for every plot, from Era 2 and Survey the Outskirts</summary>
[TestClass]
public class OutskirtsFacts
{
    [TestMethod]
    public void TheOutskirtsOpenWithEraTwoAndTheSurvey()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1e10, level: 20);

        StringAssert.Contains(game.World.BuyOutskirts(company.Id, 0).Error, "Era 2");
        game.World.AdvanceEra(company.District);
        StringAssert.Contains(game.World.BuyOutskirts(company.Id, 0).Error, "Survey");
        company.Researched.Add("survey");
        var gold = company.Gold;

        Assert.IsTrue(game.World.BuyOutskirts(company.Id, 3).Success, "in any order");
        Assert.AreEqual(gold - 100_000_000, company.Gold, 1);
        Assert.AreEqual(140_000_000, game.World.NextOutskirtsCost(company), 1, "each 1.4 times the last");
        Assert.IsTrue(game.EstateOf(company).HasOutskirts(3));
        StringAssert.Contains(game.World.BuyOutskirts(company.Id, 3).Error, "already");
    }

    [TestMethod]
    public void BuildingsStandOnBoughtOutskirtsAndAMoveBuysThemBackForHalf()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1e10, level: 20);
        game.World.AdvanceEra(company.District);
        company.Researched.Add("survey");
        var estate = game.EstateOf(company);
        var tiles = GameWorld.OutskirtsTiles(estate, 0).ToList();
        var spot = tiles.First(t => game.World.Tile(t).Terrain is TerrainType.Grass or TerrainType.Hills && !game.World.IsOccupied(t));

        StringAssert.Contains(game.World.CanPlace(company, game.World.Content.Building("windMoily"), spot).Error, "buy it first");
        Assert.IsTrue(game.World.BuyOutskirts(company.Id, 0).Success);
        Assert.IsTrue(game.World.BuyOutskirts(company.Id, 1).Success);
        Assert.IsTrue(game.World.CanPlace(company, game.World.Content.Building("windMoily"), spot).Success);
        game.Build(company, "windMoily", spot);
        var gold = company.Gold;

        var other = MovingFacts.OtherDistrict(game, company);
        game.World.AdvanceEra(other);
        Assert.IsTrue(game.World.Relocate(company.Id, other).Success);

        Assert.AreEqual(gold + (100_000_000 + 140_000_000) * 0.5, company.Gold, 1, "half their price back");
        Assert.AreEqual(0, estate.OutskirtsParcels);
        Assert.IsTrue(company.Crates.Any(t => t.Type == "windMoily"), "what stood there waits in a crate");
    }

    [TestMethod]
    public void GrowBuysTheNextOutskirtsParcelAndTheAdvisorBuildsOnIt()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 150_000_000, level: 20);
        game.World.AdvanceEra(company.District);
        company.Researched.Add("survey");
        // Every parcel of the plot bought: the outskirts are the land left
        for (var parcel = 0; parcel < WorldLayout.ParcelCount; parcel++)
        {
            game.EstateOf(company).UnlockedParcels |= 1 << parcel;
        }

        var plan = game.World.PlanGrowth(company);
        Assert.IsTrue(plan.Parts.Any(t => t.Outskirts == 0), string.Join(", ", plan.Parts.Select(t => t.Text)));
        Assert.IsTrue(game.World.GrowEverything(company.Id).Success);
        Assert.IsTrue(game.EstateOf(company).HasOutskirts(0));

        // The advisor's spots include the outskirts: near them, a new building goes there
        var tiles = GameWorld.OutskirtsTiles(game.EstateOf(company), 0).ToHashSet();
        var near = tiles.First(t => game.World.Tile(t).Terrain is TerrainType.Grass or TerrainType.Hills);
        var spot = game.World.FindSpot(company, game.World.Content.Building("windMoily"), near);
        Assert.IsTrue(spot is { } found && tiles.Contains(found), $"{spot}");
    }
}
