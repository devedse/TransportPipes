using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class StandInFacts
{
    /// <summary>A (slot 0) has a Wind Moily, B (slot 1, the same station) an Iron Mine that needs energy; both pipe to their Yard gates</summary>
    private static (TestGame Game, Company A, Company B, Building Moily, Building Mine) Neighbours()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var a = game.NewCompany("A");
        var b = game.NewCompany("B", level: 2);
        var moily = game.Build(a, "windMoily");
        game.ConnectToGate(a, moily);
        game.Connect(a, moily, game.TradingPost(a));
        var mine = game.Build(b, "ironMine", game.Spot(b, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(b, mine);
        game.Connect(b, mine, game.TradingPost(b));
        // A crew: an asleep member's seat is held to day 90, so the stand-in runs its full course
        var (_, crew) = game.World.FoundCrew(a.Id, "Kettle & Co.", "kettle", "#236b69", "#c9963a", 0);
        game.World.JoinCrew(b.Id, crew!.InviteCode);
        game.Pass(3);
        return (game, a, b, moily, mine);
    }

    /// <summary>Days pass; the companies given play every day, the others are away</summary>
    private static void Days(TestGame game, double days, params Company[] playing)
    {
        for (var day = 0.0; day < days; day += 1)
        {
            foreach (var company in playing)
            {
                company.LastSeenAt = game.World.Now;
            }
            game.Pass(Math.Min(1, days - day) * 24 * 60);
        }
    }

    private static double ImportedBy(TestGame game, Building building, string resource) =>
        game.World.NetworkOf(building.Id)!.GateFlows.Where(t => t.Import && t.Building == building.Id && t.Resource == resource).Sum(t => t.Delivered);

    [TestMethod]
    public void AnAsleepEstateStaysInPlaceAndMakesNothing()
    {
        var (game, a, b, moily, _) = Neighbours();
        Days(game, 31, b);

        Assert.IsTrue(a.Asleep);
        Assert.IsNotNull(game.World.EstateOf(a), "asleep in place");
        Assert.IsNotNull(game.World.Building(moily.Id), "its buildings stay");
        Assert.AreEqual(0, game.World.GoldPerMinute().GetValueOrDefault(a.Id), "it makes and earns nothing");
        Assert.IsNotNull(a.StandIn);
    }

    [TestMethod]
    public void PartnersHearAboutTheStandInOnDay23()
    {
        var (game, a, b, _, _) = Neighbours();
        Days(game, 22, b);
        game.World.Changes.Events.Clear();
        Days(game, 1.5, b);

        Assert.IsTrue(game.World.Changes.Events.Any(t => t.Company == b.Id && t.Text.Contains("stand-in")), "B hears it a week ahead");
        Assert.IsFalse(game.World.Changes.Events.Any(t => t.Company == a.Id && t.Text.Contains("stand-in")));
    }

    [TestMethod]
    public void ACrewMatesStandInKeepsTheOldPriceForAWeekButNotForTheSameDevice()
    {
        var (game, a, b, _, _) = Neighbours();
        b.DistinctDays = 10;
        game.World.SeenOn(a, "browser-a", null);
        game.World.SeenOn(b, "browser-b", null);
        Days(game, 31, a);
        Assert.IsTrue(b.Asleep);
        Assert.IsTrue(b.StandIn!.OldPriceWeek, "a crew mate who played on 10 days");
        Assert.AreEqual(game.World.PlayerPrice("energy"), game.World.StandInBuyPrice(b, a, "energy"), 1e-9, "the old price for a week");

        game.World.SeenOn(a, "browser-b", null);
        Assert.IsTrue(game.World.SharedDeviceWithSleeper(b, a));
        Assert.AreEqual(game.World.HouseBuyPrice(a, "energy"), game.World.StandInBuyPrice(b, a, "energy"), 1e-9, "never for an account on the sleeper's own device");
    }

    [TestMethod]
    public void TheStandInKeepsSellingToTheFriendWhoStays()
    {
        var (game, a, b, _, mine) = Neighbours();
        Assert.AreEqual(10, ImportedBy(game, mine, "energy"), 1e-6);
        Days(game, 31, b);
        Assert.IsTrue(a.Asleep);

        Assert.AreEqual(10, ImportedBy(game, mine, "energy"), 1e-6, "the mine still gets all its energy, from A's stand-in");
        var goldA = a.Gold;
        var goldB = b.Gold;
        Days(game, 1, b);
        Assert.AreEqual(goldA, a.Gold, 1e-9, "the stand-in's gold is never the sleeper's");
        Assert.IsLessThan(goldB + 24 * 60 * game.World.GoldPerMinute()[b.Id] + 1, b.Gold, "B pays as before");

        Days(game, 20, b);
        Assert.IsLessThan(10 - 1e-6, ImportedBy(game, mine, "energy"), "after two weeks the stand-in sells 10% less a week");
        Assert.IsGreaterThan(7, ImportedBy(game, mine, "energy"));
    }

    [TestMethod]
    public void AStrangersStandInBuysAtHouseTermsFromTheFirstDay()
    {
        var (game, a, b, moily, _) = Neighbours();
        Days(game, 31, a);
        Assert.IsTrue(b.Asleep);
        var standIn = b.StandIn!;
        Assert.IsFalse(standIn.OldPriceWeek, "not a crew member with 10 days of play");
        Assert.AreEqual(game.World.HouseBuyPrice(a, "energy"), game.World.StandInBuyPrice(b, a, "energy"), 1e-9, "House terms at once: a fake quitter farms nothing");

        var goldA = a.Gold;
        Days(game, 1, a);
        Assert.IsGreaterThan(goldA + 24 * 60 * game.World.GoldPerMinute()[a.Id] + 1, a.Gold, "A is paid for what the stand-in takes");

        Days(game, 14, a);
        var exported = game.World.NetworkOf(moily.Id)!.GateFlows.Where(t => !t.Import).Sum(t => t.Delivered);
        Assert.AreEqual(0.25 * standIn.Buys["energy"], exported, 0.05 * standIn.Buys["energy"], "and a quarter of the volume: the rest goes to the Yard and the hub");
    }

    [TestMethod]
    public void TheSleeperWakesWhereItSleptAndItsTradesAreItsOwnAgain()
    {
        var (game, a, b, _, mine) = Neighbours();
        Days(game, 31, b);
        Assert.IsTrue(a.Asleep);
        var seat = (a.District, a.Slot);

        game.World.WakeUp(a);
        game.Pass(3);

        Assert.IsFalse(a.Asleep);
        Assert.IsNull(a.StandIn);
        Assert.AreEqual(seat, (a.District, a.Slot), "in place");
        Assert.AreEqual(10, ImportedBy(game, mine, "energy"), 1e-6);
        var goldA = a.Gold;
        game.Pass(10);
        Assert.IsGreaterThan(goldA, a.Gold, "A earns again");
    }
}
