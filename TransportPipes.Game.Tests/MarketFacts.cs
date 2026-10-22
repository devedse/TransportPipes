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
        // Energy: the hub's order is 1,000 per minute; two trading posts sell 1,200 of the 1,500 made
        var (game, company, post) = SolarSeller(50, 'W');
        Assert.AreEqual(1, SalePrice(game, post), 1e-9);

        game.Pass(60 * 4);

        var multiplier = Multiplier(game, company, "energy");
        Assert.IsLessThan(1.0, multiplier, "1,200 sold for an order of 1,000");
        Assert.IsGreaterThan(0.25, multiplier);
        // The next sales get the new price (within the 1% that triggers a new solution), plus the reputation the sales earned
        game.World.SolveNetworks();
        var reputation = 1 + (game.World.ReputationLevel(company) - 1) * 0.01;
        Assert.AreEqual(multiplier * reputation, SalePrice(game, post), 0.011);
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
    public void OnAQuietServerTheOrderIsBigger()
    {
        var (game, company, _) = SolarSeller(50, 'W');
        Assert.AreEqual(1000, game.World.HubDemand(company.District, "energy"), 1e-6);

        game.World.QuietGraceEnabled = true;

        Assert.AreEqual(4000, game.World.HubDemand(company.District, "energy"), 1e-6, "below 10 active players: four times");
        for (var i = 0; i < 12; i++)
        {
            game.NewCompany($"P{i}");
        }
        Assert.AreEqual(2000, game.World.HubDemand(company.District, "energy"), 1e-6, "below 50: twice");
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

        // It says how an order fills (run 2: "Your storages have no coal" left players asking what a storage is)
        StringAssert.Contains(game.World.DeliverOrder(company.Id, order.Id).Error, "energy");

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
    public void OrdersAskForWhatTheCompanyMakesAndTheNextStepOfItsChains()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        // A late era unlocks buildings for everyone: a new company still can't feed them
        game.World.Era = 5;
        var company = game.NewCompany(level: 2);
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
