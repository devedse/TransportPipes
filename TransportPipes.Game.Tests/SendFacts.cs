using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class SendFacts
{
    /// <summary>
    /// A station of three: A has one Wind Moily piped to its gate (and its trading post); B and C each have two Iron
    /// Mines piped to their gates that need more energy than A makes
    /// </summary>
    private static (TestGame Game, Company A, Company B, Company C) Station()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var a = game.NewCompany("A");
        var b = game.NewCompany("B", level: 2, slot: 1);
        var c = game.NewCompany("C", level: 2, slot: 5);
        Assert.AreEqual(WorldLayout.StationOf(a.District, a.Slot), WorldLayout.StationOf(c.District, c.Slot), "slots 0, 1 and 5 share a station");
        var moily = game.Build(a, "windMoily");
        game.ConnectToGate(a, moily);
        game.Connect(a, moily, game.TradingPost(a));
        foreach (var buyer in new[] { b, c })
        {
            for (var i = 0; i < 2; i++)
            {
                var mine = game.Build(buyer, "ironMine", game.Spot(buyer, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
                game.ConnectToGate(buyer, mine);
                game.Connect(buyer, mine, game.TradingPost(buyer));
            }
        }
        game.World.SolveNetworks();
        // Their gates met the Yard (a Handshake), before anyone traded
        foreach (var company in new[] { a, b, c })
        {
            company.Stats["yard"] = 1;
        }
        return (game, a, b, c);
    }

    private static double Gets(TestGame game, Company buyer, Company seller, string resource) =>
        game.World.YardTrades(game.EstateOf(buyer)).Where(t => !t.Selling && t.Partner == seller.Id && t.Resource == resource).Sum(t => t.PerMinute);

    [TestMethod]
    public void ABuyerHearsWhoseGoodsItGot()
    {
        // Run 2: Ana's ingots ran Dev's Lab and he never heard; the receiver's card needs what it got from whom
        var (game, a, b, c) = Station();
        var since = game.World.Now;

        game.Pass(60);

        var bought = game.World.BoughtSince(b, since);
        Assert.IsTrue(bought.TryGetValue((a.Id, "energy"), out var units) && units > 1, $"B got A's energy: {string.Join(", ", bought.Select(t => $"{t.Key}={t.Value:F1}"))}");
        var taken = game.World.TakenSince(a, since);
        Assert.AreEqual(taken[(b.Id, "energy")], units, 1e-9, "the same trade from the other end");
        Assert.IsFalse(game.World.BoughtSince(a, since).ContainsKey((b.Id, "energy")), "A bought nothing from B");
    }

    [TestMethod]
    public void ASendGoesToTheNamedNeighbourFirstUpToTheirLack()
    {
        var (game, a, b, c) = Station();
        Assert.IsTrue(game.World.SendGood(a.Id, "energy", c.Id, null).Success);

        game.Pass(3);

        var toC = Gets(game, c, a, "energy");
        var toB = Gets(game, b, a, "energy");
        Assert.IsGreaterThan(toB, toC, $"C first ({toC:F2} against {toB:F2})");
        Assert.IsLessThanOrEqualTo(20 * 1.1, toC, "never more than C's honest lack (two mines' energy)");
    }

    [TestMethod]
    public void ASendNeverCutsAUsualPartnersRunningTrade()
    {
        // Mia's coal when Dev moves in: B buys A's energy for a while, then A sends to C
        var (game, a, b, c) = Station();
        game.World.SetTradeBlocked(c.Id, a.Id, true);
        game.Pass(60);
        game.World.SetTradeBlocked(c.Id, a.Id, false);
        var before = Gets(game, b, a, "energy");
        Assert.IsGreaterThan(0, before);

        Assert.IsTrue(game.World.SendGood(a.Id, "energy", c.Id, null).Success);
        game.Pass(3);

        Assert.AreEqual(before, Gets(game, b, a, "energy"), before * 0.02, "B's running trade stays");
    }

    [TestMethod]
    public void WhoCanSendToWhom()
    {
        var (game, a, b, c) = Station();
        var house = game.World.Companies.FirstOrDefault(t => t.IsNpc);
        var far = game.NewCompany("Far", slot: 3);
        Assert.AreNotEqual(WorldLayout.StationOf(a.District, a.Slot), WorldLayout.StationOf(far.District, far.Slot));

        StringAssert.Contains(game.World.SendGood(a.Id, "energy", far.Id, null).Error, "doesn't live in your station");
        if (house != null)
        {
            StringAssert.Contains(game.World.SendGood(a.Id, "energy", house.Id, null).Error, "Houses");
        }
        // Strangers: once both gates met the Yard
        a.Stats.Remove("yard");
        StringAssert.Contains(game.World.SendGood(a.Id, "energy", b.Id, null).Error, "gates haven't met");
        game.Pass(2);
        Assert.IsTrue(game.World.SendGood(a.Id, "energy", b.Id, null).Success, "a Handshake later");
        // At most six
        foreach (var good in new[] { "ironOre", "water", "wood", "stone", "sand" })
        {
            game.World.SendGood(a.Id, good, b.Id, null);
        }
        StringAssert.Contains(game.World.SendGood(a.Id, "coal", c.Id, null).Error, "6 things");
        Assert.IsTrue(game.World.StopSend(a.Id, "sand", b.Id).Success);
        Assert.IsTrue(game.World.SendGood(a.Id, "coal", c.Id, null).Success);
    }

    [TestMethod]
    public void TheFirstGiftIsAnnouncedOnceAndThanksIsOnceADay()
    {
        var (game, a, b, c) = Station();
        Assert.IsTrue(game.World.SendGood(a.Id, "energy", c.Id, null).Success);
        game.World.TakeChanges();
        game.Pass(3);
        var events = game.World.TakeChanges().Events;
        Assert.IsTrue(events.Any(t => t.Company == a.Id && t.Text.StartsWith("∴ Your energy reached C")), "to the giver");
        Assert.IsTrue(events.Any(t => t.Company == c.Id && t.Text.Contains("A sends you energy")), "to the receiver");
        game.Pass(3);
        Assert.IsFalse(game.World.TakeChanges().Events.Any(t => t.Text.Contains("reached")), "once");

        var gold = a.Gold;
        var xp = a.Xp;
        Assert.IsTrue(game.World.Thank(c.Id, a.Id, "energy").Success);
        CollectionAssert.Contains(a.Report.Thanks, "C thanked you for your energy 💛");
        Assert.AreEqual(gold, a.Gold, "no gold");
        Assert.AreEqual(xp, a.Xp, "no XP");
        StringAssert.Contains(game.World.Thank(c.Id, a.Id, "energy").Error, "today already");
        StringAssert.Contains(game.World.Thank(a.Id, c.Id, "ironOre").Error, "doesn't come to you");
    }

    [TestMethod]
    public void ASendBetweenTwoAccountsOnOneDevicePaysTheHubPrice()
    {
        // The funnel probe: a Send can't move gold between alts faster than the hub would
        var (game, a, b, c) = Station();
        game.World.SeenOn(a, "browser-1", null);
        game.World.SeenOn(c, "browser-1", null);
        Assert.IsTrue(game.World.SendGood(a.Id, "energy", c.Id, null).Success);
        game.Pass(3);

        Assert.AreEqual(game.World.HubBuyPrice(a, "energy"), game.World.YardPrice(a, c, "energy", selling: true), 1e-9);
    }
}
