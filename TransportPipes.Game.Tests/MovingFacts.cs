using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class MovingFacts
{
    /// <summary>A company with a moily and a pipe next to its trading post</summary>
    private static (Company Company, Building Moily) Settled(TestGame game, string name)
    {
        var company = game.NewCompany(name);
        var moily = game.Build(company, "windMoily");
        return (company, moily);
    }

    internal static DistrictCoord OtherDistrict(TestGame game, Company company)
    {
        // Filling the first district opens the next one
        Company? far = null;
        for (var i = 0; i < 30 && far == null; i++)
        {
            var next = game.NewCompany($"Filler {i}");
            if (next.District != company.District)
            {
                far = next;
            }
        }
        return far!.District;
    }

    [TestMethod]
    public void AnEstateMovesWithItsBuildingsInAMinute()
    {
        var game = new TestGame();
        var (company, moily) = Settled(game, "Mover");
        var local = (moily.X - game.EstateOf(company).Origin.X, moily.Y - game.EstateOf(company).Origin.Y);
        var target = OtherDistrict(game, company);
        var gold = company.Gold;

        Assert.IsTrue(game.World.Relocate(company.Id, target).Success);
        Assert.AreEqual(gold - game.World.MoveCost(company), company.Gold);
        Assert.IsNull(game.World.EstateOf(company), "in transit");
        Assert.IsNull(game.World.Building(moily.Id));
        StringAssert.Contains(game.World.Relocate(company.Id, target).Error, "on its way");

        game.Pass(2);

        var estate = game.World.EstateOf(company)!;
        Assert.AreEqual(target, estate.District);
        var landed = game.World.BuildingsOf(company.Id).ToList();
        Assert.IsTrue(landed.Any(t => t.Type == "hq") && landed.Any(t => t.Type == "tradingPost"));
        var moved = landed.SingleOrDefault(t => t.Type == "windMoily");
        if (moved != null)
        {
            Assert.AreEqual(local, (moved.X - estate.Origin.X, moved.Y - estate.Origin.Y), "the same layout");
        }
        else
        {
            Assert.AreEqual("windMoily", company.Crates.Single().Type, "or in a crate when the land doesn't allow it");
        }
        var elsewhere = company.District == target ? new DistrictCoord(0, 0) : target;
        StringAssert.Contains(game.World.MoveBlocker(company, elsewhere), "move again in 3 minutes");
        game.Pass(3);
        Assert.IsNull(game.World.MoveBlocker(company, elsewhere), "a few minutes between moves");
    }

    [TestMethod]
    public void CratesArePlacedLaterOrScrapped()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        company.Crates.Add(new PackedBuilding { Type = "windMoily", Level = 3 });
        company.Crates.Add(new PackedBuilding { Type = "windMoily", Level = 1 });
        var gold = company.Gold;

        Assert.IsTrue(game.World.ScrapCrate(company.Id, 1).Success);
        Assert.AreEqual(gold + 150, company.Gold, "75% of 200");
        var spot = game.Spot(company, "windMoily");
        Assert.IsTrue(game.World.PlaceCrate(company.Id, 0, spot).Success);
        Assert.AreEqual(3, game.World.BuildingAt(spot)!.Level);
        Assert.IsEmpty(company.Crates);
    }

}
