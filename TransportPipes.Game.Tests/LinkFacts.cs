using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class LinkFacts
{
    /// <summary>
    /// Ana (slot 0, the north-west station) has a Wind Moily, Ben (slot 3, the north-east station) an Iron Mine that needs
    /// energy; both pipe to their Yard gates, so only a link brings them together
    /// </summary>
    private static (TestGame Game, Company Ana, Company Ben, Building Moily, Building Mine) Apart()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", slot: 0);
        var ben = game.NewCompany("Ben", level: 2, slot: 12);
        Assert.AreNotEqual(game.EstateOf(ana).Station, game.EstateOf(ben).Station);

        var moily = game.Build(ana, "windMoily");
        game.ConnectToGate(ana, moily);
        game.Connect(ana, moily, game.TradingPost(ana));

        var mine = game.Build(ben, "ironMine", game.Spot(ben, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(ben, mine);
        game.Connect(ben, mine, game.TradingPost(ben));
        game.World.SolveNetworks();
        return (game, ana, ben, moily, mine);
    }

    private static Link Linked(TestGame game, Company seller, Company buyer, string resource = "energy")
    {
        var (offer, link) = game.World.OfferLink(seller.Id, buyer.Id, resource, selling: true);
        Assert.IsTrue(offer.Success, offer.Error);
        Assert.IsTrue(game.World.AcceptLink(buyer.Id, link!.Id).Success);
        return link;
    }

    private static double ImportedBy(TestGame game, Building building, string resource) =>
        game.World.NetworkOf(building.Id)!.GateFlows.Where(t => t.Import && t.Building == building.Id && t.Resource == resource).Sum(t => t.Delivered);

    [TestMethod]
    public void ALinkBringsGoodsFromAnotherStationAtThePlayerPrice()
    {
        var (game, ana, ben, _, mine) = Apart();
        game.Pass(3);
        Assert.AreEqual(0, ImportedBy(game, mine, "energy"), "no Yard joins two stations");

        var link = Linked(game, ana, ben);
        game.Pass(3);

        Assert.AreEqual(10, ImportedBy(game, mine, "energy"), 1e-6, "the mine gets the 10 energy it needs from Ana");
        Assert.AreEqual(LinkFlow.Live, link.Flow);
        Assert.IsGreaterThan(10, link.Rate, "Ben's own pipe loses a little between his gate and his mine");
        Assert.IsGreaterThan(link.Rate, link.Send * game.World.LinkEfficiency(link) + 1e-9, "Ana sends more than arrives: the link's pipe loses some, and the tank fills");

        var goldAna = ana.Gold;
        var energySales = game.World.NetworkOf(game.World.BuildingsOf(ana.Id).Single(t => t.Type == "windMoily").Id)!.Solution.GoldPerMinute[ana.Id];
        game.Pass(10);
        Assert.AreEqual(goldAna + 10 * (energySales + link.Rate * 1.3), ana.Gold, 1e-3, "Ana is paid the player price for what arrives at Ben's gate");
    }

    [TestMethod]
    public void TheRouteRunsAlongTheRoadsAndAcrossTheWilderness()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var capital = new DistrictCoord(0, 0);
        foreach (var (from, to) in new[]
                 {
                     (new StationRef(capital, 0), new StationRef(capital, 3)),
                     (new StationRef(capital, 1), new StationRef(new DistrictCoord(1, 0), 2)),
                     (new StationRef(capital, 2), new StationRef(new DistrictCoord(1, 1), 1)),
                 })
        {
            var a = WorldLayout.YardGate(from.District, WorldLayout.StationSlots(from.Index)[0]);
            var b = WorldLayout.YardGate(to.District, WorldLayout.StationSlots(to.Index)[0]);
            var route = GameWorld.LinkRoute(a, from, b, to);
            Assert.AreEqual(a, route[0]);
            Assert.AreEqual(b, route[^1]);
            for (var i = 0; i + 1 < route.Count; i++)
            {
                Assert.IsTrue(route[i].X == route[i + 1].X || route[i].Y == route[i + 1].Y, $"straight segments: {route[i]} {route[i + 1]}");
            }
            // Out of the Yards it runs on roads, the hub's plaza or the wilderness: the legs between the roads
            var legs = route.Skip(3).Take(route.Count - 6).ToList();
            for (var i = 0; i + 1 < legs.Count; i++)
            {
                var (p, q) = (legs[i], legs[i + 1]);
                var steps = Math.Abs(p.X - q.X) + Math.Abs(p.Y - q.Y);
                for (var s = 0; s <= steps; s++)
                {
                    var t = new TilePos(p.X + Math.Sign(q.X - p.X) * s, p.Y + Math.Sign(q.Y - p.Y) * s);
                    var zone = WorldLayout.ZoneAt(t.X, t.Y).Kind;
                    Assert.IsTrue(zone is ZoneKind.Corridor or ZoneKind.Hub or ZoneKind.Wilderness, $"{t} is {zone} on the way from {from} to {to}");
                }
            }
            Assert.AreEqual(GameWorld.RouteLength(route), route.Zip(route.Skip(1), (p, q) => Math.Abs(p.X - q.X) + Math.Abs(p.Y - q.Y)).Sum());
        }
        Assert.AreEqual(Math.Pow(0.997, 100), game.World.LinkEfficiency(1, 100), 1e-12, "grey pipe: 0.3% a tile");
        Assert.IsGreaterThan(game.World.LinkEfficiency(1, 100), game.World.LinkEfficiency(2, 100), "copper loses less");
    }

    [TestMethod]
    public void TheTankKeepsTheBuyerGoingWhenTheSellerStops()
    {
        var (game, ana, ben, moily, mine) = Apart();
        var link = Linked(game, ana, ben);
        // It fills from what Ana has spare besides Ben's need
        game.Pass(480);
        Assert.AreEqual(link.TankSize, game.World.TankNow(link), 1e-6, "full");
        Assert.AreEqual(link.Need * GameWorld.TankMinutes, link.TankSize, 1e-6, "two hours of what Ben needs");

        Assert.IsTrue(game.World.Demolish(ana.Id, moily.Id).Success);
        // A steady link rounds every 16 minutes: the tank's level follows from the next round
        game.Pass(46);
        Assert.IsTrue(link.Flow is LinkFlow.SellerShort or LinkFlow.NotConnected, "Ana has nothing to send now");
        Assert.AreEqual(10, ImportedBy(game, mine, "energy"), 1e-6, "the tank keeps the mine going");
        Assert.IsLessThan(link.TankSize * 0.85, game.World.TankNow(link));

        game.Pass(120);
        Assert.AreEqual(0, game.World.TankNow(link), 1e-6, "two hours later the tank is empty");
        Assert.AreEqual(0, ImportedBy(game, mine, "energy"), 1e-6);
        Assert.AreEqual(0, link.Rate);
        Assert.AreEqual(LinkFlow.SellerShort, link.Flow, "the link says why nothing comes");
    }

    [TestMethod]
    public void APauseComesWithAnHoursNotice()
    {
        var (game, ana, ben, _, mine) = Apart();
        var link = Linked(game, ana, ben);
        game.Pass(5);

        Assert.IsTrue(game.World.EndLink(ben.Id, link.Id, end: false).Success);
        game.Pass(50);
        Assert.AreEqual(LinkState.Active, link.State, "it keeps flowing during the notice");
        Assert.AreEqual(10, ImportedBy(game, mine, "energy"), 1e-6);

        game.Pass(15);
        Assert.AreEqual(LinkState.Paused, link.State);
        Assert.AreEqual(0, link.Send, "Ana sends nothing over a paused link");
        Assert.IsFalse(game.World.ResumeLink(ana.Id, link.Id).Success, "Ben paused it: resuming is his");
        Assert.IsTrue(game.World.ResumeLink(ben.Id, link.Id).Success);
        game.Pass(3);
        Assert.AreEqual(LinkFlow.Live, link.Flow);

        Assert.IsTrue(game.World.EndLink(ana.Id, link.Id, end: true).Success);
        game.Pass(61);
        Assert.IsNull(game.World.Link(link.Id), "ended after the notice");
    }

    [TestMethod]
    public void AnOfferWaitsADayAfterTheOtherSidesNextSessionThenIsRefunded()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", slot: 0);
        var dee = game.NewCompany("Dee", slot: 8);
        var gold = dee.Gold;

        var (offer, link) = game.World.OfferLink(dee.Id, ana.Id, "coal", selling: false);
        Assert.IsTrue(offer.Success, offer.Error);
        Assert.AreEqual(gold - link!.Paid, dee.Gold, 1e-9, "the offerer pays the pipe");
        Assert.AreEqual(2.0 * link.Length, link.Paid, 1e-9, "grey pipe: 2 gold a tile");
        CollectionAssert.Contains(game.World.Changes.Events.Where(t => t.Company == ana.Id).Select(t => t.Kind).ToList(), WorldEventKind.Link);

        // F11: Ana hasn't played since: the offer waits for her
        game.Pass(73 * 60);
        Assert.IsNotNull(game.World.Link(link.Id), "it waits for Ana's next session");

        ana.LastSeenAt = ana.SessionStartedAt = game.World.Now;
        game.Pass(23 * 60);
        Assert.IsNotNull(game.World.Link(link.Id), "a day to answer");
        game.Pass(2 * 60);

        Assert.IsNull(game.World.Link(link.Id));
        Assert.AreEqual(gold, dee.Gold, 1e-6, "refunded");
    }

    [TestMethod]
    public void AnOfferMadeWhileTheyPlayWaitsForTheirNextSession()
    {
        // Run 2: Mia offered Ana a link while Ana was playing; Ana's last-seen moved on with every tick of that session, so
        // the day to answer started then, and the offer lapsed before her next session
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", slot: 0);
        var dee = game.NewCompany("Dee", slot: 8);
        ana.SessionStartedAt = game.World.Now;
        game.Pass(10);
        var (offer, link) = game.World.OfferLink(dee.Id, ana.Id, "coal", selling: false);
        Assert.IsTrue(offer.Success, offer.Error);

        // Ana plays on a while (online: seen every tick), then leaves; 30 hours pass
        for (var i = 0; i < 3; i++)
        {
            ana.LastSeenAt = game.World.Now;
            game.Pass(5);
        }
        game.Pass(30 * 60);

        Assert.IsNotNull(game.World.Link(link!.Id), "it waits for the session after the offer");
    }

    [TestMethod]
    public void AnOfferToSomeoneWhoNeverComesLapsesInTwoWeeks()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", slot: 0);
        var dee = game.NewCompany("Dee", slot: 8);
        var (_, link) = game.World.OfferLink(dee.Id, ana.Id, "coal", selling: false);
        ana.LastSeenAt = game.World.Now - TimeSpan.FromDays(1);

        game.Pass(13 * 24 * 60);
        Assert.IsNotNull(game.World.Link(link!.Id));
        game.Pass(24 * 60 + 5);
        Assert.IsNull(game.World.Link(link.Id), "at most two weeks");
    }

    [TestMethod]
    public void LinksHaveLimits()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", slot: 0);
        var ben = game.NewCompany("Ben", slot: 1);
        var dee = game.NewCompany("Dee", slot: 8);

        StringAssert.Contains(game.World.OfferLink(ana.Id, ben.Id, "coal", true).Result.Error, "at your Yard", "neighbours trade through the Yard");
        foreach (var resource in new[] { "coal", "wood", "stone", "sand", "water" })
        {
            Assert.IsTrue(game.World.OfferLink(ana.Id, dee.Id, resource, true).Result.Success);
        }
        StringAssert.Contains(game.World.OfferLink(ana.Id, dee.Id, "energy", true).Result.Error, "an hour");
        StringAssert.Contains(game.World.OfferLink(ana.Id, dee.Id, "coal", true).Result.Error, "already");
    }

    [TestMethod]
    public void ALinkTakesTheNewWayWhenAnEstateMoves()
    {
        var (game, ana, ben, _, _) = Apart();
        var link = Linked(game, ana, ben);
        var before = link.Length;

        Assert.IsTrue(game.World.RelocateTo(ben.Id, new EstateKey(new DistrictCoord(0, 0), 9)).Success);
        game.Pass(2);

        Assert.AreEqual(9, ben.Slot);
        Assert.AreNotEqual(before, link.Length, "rerouted to the new gate");
        Assert.AreEqual(game.World.EstateOf(ben)!.YardGate, link.Route[^1]);
    }
}
