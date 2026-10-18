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

    private static DistrictCoord OtherDistrict(TestGame game, Company company)
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
    public void AnEstateMovesWithItsBuildingsInAnHour()
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

        game.Pass(61);

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
        StringAssert.Contains(game.World.MoveBlocker(company, company.District == target ? new DistrictCoord(0, 0) : target), "once a week");
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

    [TestMethod]
    public void AfterAMonthAwayTheEstateSleepsOnTheQuietIslesAndWakesUp()
    {
        var game = new TestGame();
        var (company, _) = Settled(game, "Sleeper");
        var district = company.District;
        var corridor = WorldLayout.OutsideGate(district, company.Slot, GateSide.Top);
        Assert.IsTrue(game.World.PlacePipes(company.Id, [corridor]).Success);

        game.Pass(13 * 24 * 60);
        Assert.AreEqual(Activity.Resting, game.World.ActivityOf(company));
        game.Pass(2 * 24 * 60);
        Assert.AreEqual(Activity.Dormant, game.World.ActivityOf(company));
        game.Pass(16 * 24 * 60);

        Assert.IsTrue(company.Asleep);
        Assert.IsNull(game.World.EstateOf(company), "the spot is free for a newcomer");
        Assert.IsTrue(game.World.QuietIsles.Contains(company));
        Assert.AreEqual(0, game.World.ConductorAt(corridor)!.Owner, "public pipes become hub pipes");

        game.World.WakeUp(company);

        Assert.IsFalse(company.Asleep);
        Assert.IsNotNull(game.World.EstateOf(company));
        Assert.IsTrue(game.World.BuildingsOf(company.Id).Any(t => t.Type == "hq"));
        Assert.AreEqual(1, company.FreeMoves, "a free move to wherever they like");
    }

    [TestMethod]
    public void TwoPlayersSwapEstates()
    {
        var game = new TestGame();
        var (a, _) = Settled(game, "Alpha");
        var (b, _) = Settled(game, "Bravo");
        var slotA = a.Slot;
        var slotB = b.Slot;

        Assert.IsTrue(game.World.ProposeSwap(a.Id, b.Id).Success);
        Assert.AreEqual(slotA, a.Slot, "only when both agree");
        Assert.IsTrue(game.World.ProposeSwap(b.Id, a.Id).Success);

        Assert.AreEqual(slotB, a.Slot);
        Assert.AreEqual(slotA, b.Slot);
        Assert.IsNotNull(game.World.EstateOf(a));
        Assert.IsNotNull(game.World.EstateOf(b));
    }

    [TestMethod]
    public void ACorporationBlockKeepsEstatesForItsMembers()
    {
        var game = new TestGame();
        var leader = game.NewCompany("Leader", level: 8);
        game.World.FoundCorporation(leader.Id, "Block Guild", "BLK", "#ffffff");
        var corporation = game.World.Corporations.Single();
        for (var i = 0; i < 4; i++)
        {
            var member = game.NewCompany($"Member {i}");
            game.World.ApplyToCorporation(member.Id, corporation.Id);
            game.World.InviteToCorporation(leader.Id, member.Id);
        }
        var district = OtherDistrict(game, leader);

        Assert.IsTrue(game.World.ReserveBlock(leader.Id, district).Success);

        var newcomer = game.NewCompany("Newcomer");
        Assert.IsFalse(game.World.EstateOf(newcomer)!.ReservedFor == corporation.Id, "newcomers don't get reserved estates");
    }
}
