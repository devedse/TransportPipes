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

    [TestMethod]
    public void TheHubPaysLessWhenItIsFloodedWithAResource()
    {
        // Energy: the hub wants 1,000 per minute per active company; two trading posts sell 1,200 of the 1,500 made
        var (game, company, post) = SolarSeller(50, 'W');
        Assert.AreEqual(1, SalePrice(game, post), 1e-9);

        game.Pass(60 * 4);

        var multiplier = game.World.Market(company.District).Multiplier("energy");
        Assert.IsLessThan(1.0, multiplier, "1,200 sold for 1,000 wanted");
        Assert.IsGreaterThan(0.25, multiplier);
        // The next sales get the new price (within the 1% that triggers a new solution), plus the reputation the sales earned
        game.World.SolveNetworks();
        var reputation = 1 + (game.World.ReputationLevel(company) - 1) * 0.01;
        Assert.AreEqual(multiplier * reputation, SalePrice(game, post), 0.011);
    }

    [TestMethod]
    public void TheHubPaysABitMoreForWhatIsScarce()
    {
        var (game, company, post) = SolarSeller(4);

        game.Pass(60 * 3);

        Assert.AreEqual(HubMarket.MaxMultiplier, game.World.Market(company.District).Multiplier("energy"), 0.02);
        Assert.AreEqual(HubMarket.MaxMultiplier, SalePrice(game, post), 0.02);
    }

    [TestMethod]
    public void APriceChangeWaitsAnHourWhenAPartnerBuys()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var a = game.NewCompany("A");
        var b = game.NewCompany("B");
        game.Map(a, game.Local(a, 1, 1), " W+");
        game.Map(b, game.Local(a, 1, 1), "   M+T");
        Assert.AreEqual(1.75, game.World.SellPrice(a, "energy"));

        Assert.IsTrue(game.World.SetSellPrice(a.Id, "energy", 2.2).Success);
        Assert.AreEqual(1.75, game.World.SellPrice(a, "energy"), "B is warned first");
        Assert.AreEqual(2.2, a.PendingPrices["energy"].Price);

        game.Pass(61);
        Assert.AreEqual(2.2, game.World.SellPrice(a, "energy"));
        Assert.IsEmpty(a.PendingPrices);

        // Without a partner for wood, it changes right away
        Assert.IsTrue(game.World.SetSellPrice(a.Id, "wood", 2).Success);
        Assert.AreEqual(2, game.World.SellPrice(a, "wood"));
    }

    [TestMethod]
    public void PricesStayInTheFairBand()
    {
        var game = new TestGame();
        var company = game.NewCompany();

        game.World.SetSellPrice(company.Id, "energy", 100);
        Assert.AreEqual(2.5, game.World.SellPrice(company, "energy"));
        company.Researched.Add("haggling");
        game.World.SetSellPrice(company.Id, "energy", 100);
        Assert.AreEqual(2.75, game.World.SellPrice(company, "energy"), "Haggling: 10% beyond the band");
    }

    [TestMethod]
    public void DailyOrdersAreDeliveredFromStoragesForGoldXpAndReputation()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        game.World.EnsureOrders(company);
        Assert.HasCount(GameWorld.OrdersPerDay, company.DailyOrders);
        var order = company.DailyOrders[0];
        Assert.AreEqual("energy", order.Resource, "a level 1 company can only make energy");

        StringAssert.Contains(game.World.DeliverOrder(company.Id, order.Id).Error, "no energy");

        var storage = game.Map(company, game.Local(company, 1, 1), "O").Values.Single();
        storage.Contents["energy"] = order.Amount / 2;
        Assert.IsTrue(game.World.DeliverOrder(company.Id, order.Id).Success);
        Assert.AreEqual(order.Amount / 2, order.Delivered);
        var gold = company.Gold;
        storage.Contents["energy"] = order.Amount;
        Assert.IsTrue(game.World.DeliverOrder(company.Id, order.Id).Success);

        Assert.IsTrue(order.Done);
        Assert.AreEqual(order.Amount / 2, storage.Contents["energy"], 1e-9, "only what was still missing is taken");
        Assert.AreEqual(gold + order.RewardGold, company.Gold);
        Assert.AreEqual(order.RewardReputation, company.HubReputation);
        StringAssert.Contains(game.World.DeliverOrder(company.Id, order.Id).Error, "complete");
    }

    [TestMethod]
    public void NewOrdersArriveEveryDay()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        game.World.EnsureOrders(company);
        var first = company.DailyOrders.Select(t => t.Id).ToList();

        game.Pass(60 * 24);

        Assert.HasCount(GameWorld.OrdersPerDay, company.DailyOrders);
        Assert.IsTrue(company.DailyOrders.All(t => !first.Contains(t.Id)));
        Assert.IsTrue(company.DailyOrders.All(t => t.ExpiresAt > game.Time));
    }

    [TestMethod]
    public void HubReputationRaisesTheHubPrice()
    {
        var (game, company, post) = SolarSeller(2);
        company.HubReputation = 1000;

        Assert.AreEqual(5, game.World.ReputationLevel(company));
        game.World.CompanyChanged(company);
        game.World.SolveNetworks();
        Assert.AreEqual(1.04, SalePrice(game, post), 1e-9, "+1% per level above 1");
    }
}
