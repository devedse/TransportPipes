using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class MarketFacts
{
    /// <summary>A row of energy producers (solar panels: 5/min, wind moilies: 30/min) with a trading post at both ends of their pipe</summary>
    private static (TestGame Game, Company Company, Building Post) SolarSeller(int panels, char producer = 'S')
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        var map = game.Map(company, game.Local(company, 0, 1), string.Join("\n", " " + new string(producer, panels), "T" + new string('+', panels) + "T"));
        return (game, company, map.Values.First(t => t.Type == "tradingPost"));
    }

    private static double SalePrice(TestGame game, Building post) =>
        game.World.NetworkOf(post.Id)!.Solution.Flows.First(t => t.Kind == FlowKind.Sale).PricePerUnit;

    private static double Multiplier(TestGame game, Company company, string resource) =>
        game.World.HubMarketOf(company, company.District).Multiplier(resource);

    [TestMethod]
    public void TheHubPaysLessWhenItIsFloodedWithAResource()
    {
        // Energy: the Hub Order is 1,000 a minute; the trading posts sell all 1,500 made (1.4 §8: no unit limit)
        var (game, company, post) = SolarSeller(50, 'W');
        Assert.AreEqual(1, SalePrice(game, post), 1e-9);

        game.Pass(60 * 4);

        var multiplier = Multiplier(game, company, "energy");
        Assert.IsLessThan(1.0, multiplier, "1,500 sold for an order of 1,000");
        Assert.IsGreaterThan(0.25, multiplier);
        // The next sales get the new price (within the 1% that triggers a new solution)
        game.World.SolveNetworks();
        Assert.AreEqual(multiplier, SalePrice(game, post), 0.011);
        Assert.AreEqual(Math.Sqrt(1000 / 1500.0), multiplier, 0.02, "the market pays base × √(order × sold): its average price is √(order ÷ sold)");
    }

    [TestMethod]
    public void UpToItsOrderTheHubPaysTheFullPrice()
    {
        var (game, company, post) = SolarSeller(4);

        game.Pass(60 * 3);

        Assert.AreEqual(1, Multiplier(game, company, "energy"), 1e-9);
        Assert.AreEqual(1, SalePrice(game, post), 0.02);
    }

    [TestMethod]
    public void NobodyElsesSalesMoveYourPrice()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var flooder = game.NewCompany("Flooder");
        var small = game.NewCompany("Small");
        game.Map(flooder, game.Local(flooder, 2, 2), string.Join("\n", " " + new string('W', 25), "T" + new string('+', 25) + "T", " " + new string('W', 25)));
        game.Map(small, game.Local(small, 0, 1), " SS\nT++T");

        game.Pass(60 * 4);

        Assert.IsLessThan(1.0, Multiplier(game, flooder, "energy"));
        Assert.AreEqual(1, Multiplier(game, small, "energy"), 1e-9, "the small seller keeps the full price");
    }

    [TestMethod]
    public void TheOrderIsTheSameHoweverManyPlay()
    {
        // 1.4 rule 2: no number depends on how many people play (v1.2's quiet grace made it four times as big alone)
        var (game, company, _) = SolarSeller(50, 'W');
        Assert.AreEqual(1000, game.World.HubOrder(company, "energy"), 1e-6);

        for (var i = 0; i < 12; i++)
        {
            game.NewCompany($"P{i}");
        }

        Assert.AreEqual(1000, game.World.HubOrder(company, "energy"), 1e-6);
    }

    [TestMethod]
    public void DailyOrdersBuyAtTwiceThePriceFromWhatTheTradingPostSells()
    {
        // After run 3, step 7: no order was delivered after day 14, because they came out of a Storage nobody mentioned
        var (game, company, post) = SolarSeller(5);
        game.World.EnsureOrders(company);
        var order = company.DailyOrders.Single();
        Assert.AreEqual("energy", order.Resource, "what its post sells: one order a good");
        var selling = game.World.SellingPerMinute(company)["energy"];
        Assert.IsLessThanOrEqualTo(selling * 8 * 60 + 10, order.Amount, "it fills within eight hours of its own selling");
        var price = SalePrice(game, post);
        game.World.TakeChanges();

        var minutes = 0;
        while (!order.Done && minutes < 9 * 60)
        {
            game.Pass(1);
            minutes++;
        }

        Assert.IsTrue(order.Done, "it filled by itself, from the sales");
        Assert.AreEqual(order.Amount * (order.RewardGold / order.Amount - price), order.Premium, 1, "it paid twice the base price: the difference to the hub's on top");
        Assert.AreEqual(order.Premium, company.EarnedLog["orders"]["energy"], 1, "counted as gold earned");
        var events = game.World.TakeChanges().Events;
        Assert.IsTrue(events.Any(t => t.Company == company.Id && t.Text.StartsWith("Your energy filled a hub order")), string.Join(" | ", events.Select(t => t.Text)));
        Assert.AreEqual(1, company.Report.OrdersCompleted);
    }

    [TestMethod]
    public void OrdersAreSizedAtMinutesOfIncome()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        company.MeasuredIncome = 6_000;
        game.World.EnsureOrders(company);

        var value = company.DailyOrders.Sum(t => t.Amount * game.World.BasePrice(t.Resource));

        Assert.IsGreaterThan(GameWorld.OrderIncomeMinutes * 6_000 * 0.75 - 30, value);
        Assert.IsLessThan(GameWorld.OrderIncomeMinutes * 6_000 * 1.25 + 30, value, "about half an hour of income in all");
    }

    [TestMethod]
    public void OrdersAskForWhatTheCompanyMakesAndTheNextStepOfItsChains()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        // A late Era unlocks buildings for everyone in the town: a company new to it still can't feed them
        var company = game.NewCompany(level: 2);
        for (var era = 1; era < 5; era++)
        {
            game.World.AdvanceEra(company.District);
        }
        CollectionAssert.AreEqual(Array.Empty<string>(), game.World.OrderResources(company), "nothing made yet (the orders ask for energy)");

        game.Build(company, "windMoily");
        CollectionAssert.AreEqual(new[] { "energy" }, game.World.OrderResources(company));

        game.Build(company, "ironMine", game.Spot(company, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        CollectionAssert.AreEqual(new[] { "energy", "ironIngot", "ironOre" }, game.World.OrderResources(company), "ingots: the factory's inputs are made");
    }

    [TestMethod]
    public void NewOrdersArriveEveryDay()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        game.World.EnsureOrders(company);
        var first = company.DailyOrders.Select(t => t.Id).ToList();

        game.Pass(60 * 24);

        Assert.HasCount(first.Count, company.DailyOrders, "one a good it makes (here energy)");
        Assert.IsTrue(company.DailyOrders.All(t => !first.Contains(t.Id)));
        Assert.IsTrue(company.DailyOrders.All(t => t.ExpiresAt > game.Time));
    }

    [TestMethod]
    public void TheTradingPostsLevelRaisesEveryHubOrder()
    {
        var (game, company, post) = SolarSeller(2);
        var order = game.World.HubOrder(company, "planks");

        post.Level = 11;

        Assert.AreEqual(order * Math.Pow(1.1, 10), game.World.HubOrder(company, "planks"), 1e-6, "+10% a level");
        Assert.AreEqual(1000 / 3.5 * 9.85, game.World.HubOrder(company, "planks") * Math.Pow(1.1, 14), 1000 / 3.5 * 0.01, "9.85× at level 25");
    }

    [TestMethod]
    public void BeyondSixteenOrdersEveryUnitPaysAQuarter()
    {
        var market = TransportPipes.Game.Content.GameContent.Default.Progression.Market;

        Assert.AreEqual(100 * 14, market.Pays(14, 100, 100), 1e-9, "within the order: the base price");
        Assert.AreEqual(14 * Math.Sqrt(100 * 400), market.Pays(14, 100, 400), 1e-9, "four times the order pays twice as much");
        Assert.AreEqual(14 * 400 + (2000 - 1600) * 14 * 0.25, market.Pays(14, 100, 2000), 1e-9, "beyond sixteen orders a quarter of the base a unit");
        Assert.AreEqual(4060, market.Pays(14, 1000 / 14.0 * Math.Pow(1.1, 9), 500), 15, "Ben's ingots (1.4 §8): 4,060 gold a minute");
    }
}
