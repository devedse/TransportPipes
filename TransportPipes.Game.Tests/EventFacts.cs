using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class EventFacts
{
    private static double Stored(TestGame game, Building storage) =>
        game.World.NetworkOf(storage.Id)!.Solution.Flows.Where(t => t.Kind == FlowKind.ToStorage).Sum(t => t.Delivered);

    [TestMethod]
    public void EveryDistrictGetsEventsThatAreAnnouncedADayAhead()
    {
        var game = new TestGame(events: true);
        var company = game.NewCompany();
        game.World.TakeChanges();

        // A week of weather and hub events
        var announced = new List<WorldEvent>();
        for (var hour = 0; hour < 24 * 7; hour++)
        {
            game.Pass(60);
            announced.AddRange(game.World.TakeChanges().Events.Where(t => t.Company == company.Id && t.Kind == WorldEventKind.Calendar));
        }

        var events = game.World.Events.Where(t => t.Covers(company.District)).ToList();
        Assert.IsGreaterThan(2, events.Count, "storms, expos and shortages happen every week");
        Assert.IsTrue(events.Where(t => t.StartsAt <= game.Time + GameWorld.AnnounceAhead).All(t => t.Announced));
        Assert.IsTrue(announced.Any(t => t.Text.StartsWith("Coming")), "a notice ahead of time");
        Assert.IsTrue(events.All(t => t.StartsAt >= TestGame.Start.AddHours(1)), "nothing starts unannounced");
    }

    [TestMethod]
    public void TheCalendarReachesFurtherWithAWeatherStation()
    {
        var game = new TestGame(events: true);
        var company = game.NewCompany(level: 20);
        game.Pass(1);
        Assert.AreEqual(GameWorld.AnnounceAhead, game.World.Horizon(company));
        Assert.IsTrue(game.World.Calendar(company).All(t => t.StartsAt <= game.Time + GameWorld.AnnounceAhead));

        game.Build(company, "weatherStation");

        Assert.AreEqual(GameWorld.ForecastAhead, game.World.Horizon(company));
        Assert.IsGreaterThanOrEqualTo(game.World.Calendar(company).Count(t => t.StartsAt <= game.Time + GameWorld.AnnounceAhead), game.World.Calendar(company).Count());
    }

    [TestMethod]
    public void AStormDoublesWindAndDamagesSomeMoilies()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var map = game.Map(company, game.Local(company, 1, 1), " " + new string('W', 40) + "\nO" + new string('+', 40));
        var storage = map[(0, 1)];
        var calm = Stored(game, storage);
        Assert.IsGreaterThan(0, calm);

        var storm = game.World.StartEventNow(EventKind.Storm, company.District, TimeSpan.FromHours(6));
        game.World.SolveNetworks();

        var damaged = map.Values.Where(t => t.Damaged).ToList();
        Assert.IsNotEmpty(damaged, "about one in ten");
        Assert.IsLessThan(20, damaged.Count);
        var windy = Stored(game, storage);
        Assert.IsGreaterThan(calm * 1.8, windy, "wind ×2, minus the damaged ones at half speed");

        // A repair costs a tenth of the building
        var gold = company.Gold;
        Assert.IsTrue(game.World.Repair(company.Id, damaged[0].Id).Success);
        Assert.AreEqual(gold - 20, company.Gold);
        Assert.IsFalse(damaged[0].Damaged);
        StringAssert.Contains(game.World.Repair(company.Id, damaged[0].Id).Error, "isn't damaged");

        game.Pass(6 * 60 + 1);
        Assert.IsTrue(storm.Ended);
        Assert.IsTrue(damaged.Skip(1).All(t => t.Damaged), "still broken without an Engineer's Workshop");
    }

    [TestMethod]
    public void AnEngineersWorkshopRepairsAfterTheStorm()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 10);
        var map = game.Map(company, game.Local(company, 1, 1), " " + new string('W', 40) + "\nT" + new string('+', 40));
        game.Build(company, "engineerWorkshop");

        game.World.StartEventNow(EventKind.Storm, company.District, TimeSpan.FromHours(1));
        Assert.IsTrue(map.Values.Any(t => t.Damaged));
        game.Pass(61);

        Assert.IsFalse(map.Values.Any(t => t.Damaged));
    }

    [TestMethod]
    public void AnExpoTriplesTheHubPriceOfItsProduct()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        double Factor(string resource) =>
            game.World.HubBuyPrice(company, resource) / game.World.BasePrice(resource) / game.World.HubMarketOf(company, company.District).Multiplier(resource);
        Assert.AreEqual(1, Factor("glass"), 1e-3);

        game.World.StartEventNow(EventKind.Expo, company.District, TimeSpan.FromHours(6), "glass");

        Assert.AreEqual(3, Factor("glass"), 1e-3);
        Assert.AreEqual(1, Factor("bricks"), 1e-3);
        game.Pass(6 * 60 + 1);
        Assert.AreEqual(1, Factor("glass"), 1e-3);
    }

    [TestMethod]
    public void AShortageBringsAnUrgentOrderUntilItEnds()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        game.World.EnsureOrders(company);
        var daily = company.DailyOrders.Count;

        game.World.StartEventNow(EventKind.Shortage, company.District, TimeSpan.FromHours(8), "planks");

        var urgent = company.DailyOrders.Single(t => t.Urgent);
        Assert.AreEqual("planks", urgent.Resource);
        Assert.HasCount(daily + 1, company.DailyOrders);
        game.Pass(8 * 60 + 1);
        Assert.IsFalse(company.DailyOrders.Any(t => t.Urgent));
        Assert.HasCount(daily, company.DailyOrders, "the daily orders stay");
    }

    [TestMethod]
    public void LikesCountDoubleDuringAFestival()
    {
        var game = new TestGame();
        var visitor = game.NewCompany("Visitor");
        var owner = game.NewCompany("Owner", gold: 0, level: 3);

        game.World.StartEventNow(EventKind.Festival, null, TimeSpan.FromDays(1));
        game.World.LikeEstate(visitor.Id, owner.Id);

        Assert.AreEqual(2, owner.Likes);
        Assert.AreEqual(120, owner.Gold);
    }
}
