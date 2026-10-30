using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class ExchangeFacts
{
    private static (TestGame Game, Company Seller, Company Buyer, Building SellerStorage, Building BuyerStorage) Traders()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var seller = game.NewCompany("Seller", level: GameWorld.ExchangeLevel);
        var buyer = game.NewCompany("Buyer", level: GameWorld.ExchangeLevel);
        var sellerMap = game.Map(seller, game.Local(seller, 1, 1), "O");
        var buyerMap = game.Map(buyer, game.Local(buyer, 1, 1), "O");
        return (game, seller, buyer, sellerMap[(0, 0)], buyerMap[(0, 0)]);
    }

    [TestMethod]
    public void TheExchangeOpensAtCompanyLevelTwelve()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 11);
        StringAssert.Contains(game.World.PlaceExchangeOrder(company.Id, "energy", false, 10, 1).Error, "Level 12");
    }

    [TestMethod]
    public void MatchingOrdersTradeAtTheWaitingOrdersPriceAndShipByCargo()
    {
        var (game, seller, buyer, sellerStorage, buyerStorage) = Traders();
        sellerStorage.Contents["ironOre"] = 500;

        Assert.IsTrue(game.World.PlaceExchangeOrder(seller.Id, "ironOre", true, 400, 4).Success);
        Assert.AreEqual(100, sellerStorage.Contents["ironOre"], 1e-9, "the goods are held by the order");
        var goldSeller = seller.Gold;
        var goldBuyer = buyer.Gold;

        // The buyer would pay 5: the order that waited asked 4
        Assert.IsTrue(game.World.PlaceExchangeOrder(buyer.Id, "ironOre", false, 300, 5).Success);

        Assert.AreEqual(goldSeller + 300 * 4 * 0.96, seller.Gold, 1e-6, "a 4% fee leaves the game (1.4 §8)");
        Assert.AreEqual(goldBuyer - 300 * 4, buyer.Gold, 1e-6);
        Assert.AreEqual(100, game.World.ExchangeOrders.Single().Amount, "100 still for sale");
        var shipment = game.World.Shipments.Single();
        Assert.AreEqual(300, shipment.Amount);
        Assert.IsFalse(buyerStorage.Contents.ContainsKey("ironOre"));

        game.Pass((shipment.ArrivesAt - game.Time).TotalMinutes + 1);

        Assert.AreEqual(300, buyerStorage.Contents["ironOre"], 1e-9);
        Assert.IsEmpty(game.World.Shipments);
    }

    [TestMethod]
    public void AnOrderSaysItsPriceWhatItHoldsAndWhenItCloses()
    {
        // Run 4: Cleo typed a price and got another without a word; her order expired a week later without a word either,
        // and both friends read it as filled
        var (game, _, buyer, _, _) = Traders();

        var placed = game.World.PlaceExchangeOrder(buyer.Id, "stone", false, 3000, 1);

        Assert.IsTrue(placed.Success);
        StringAssert.Contains(placed.Note, "1× to 2.5× the base: 1.5 g, not the 1 g you typed");
        StringAssert.Contains(placed.Note, "waits up to 7 days for a seller");
        var gold = buyer.Gold;
        game.World.TakeChanges();

        // It closes at the first midnight after its week
        game.Pass(8 * 24 * 60);

        Assert.IsEmpty(game.World.ExchangeOrders);
        Assert.AreEqual(gold + 3000 * 1.5, buyer.Gold, 1, "the held gold is back");
        var heard = game.World.TakeChanges().Events.Where(t => t.Company == buyer.Id).Select(t => t.Text).ToList();
        Assert.IsTrue(heard.Any(t => t.StartsWith("Your Exchange order to buy stone closed after 7 days: none came;")), string.Join(" | ", heard));
    }

    [TestMethod]
    public void YouBuyOnlyGoodsOfYourEraOrOlder()
    {
        var (game, _, buyer, _, _) = Traders();

        StringAssert.Contains(game.World.PlaceExchangeOrder(buyer.Id, "steel", false, 10, 100).Error, "your own Era or older");
    }

    [TestMethod]
    public void ASellOrderSaysWhatYourStoragesHold()
    {
        // Run 4: Ana's sell order was refused, and the refusal never reached her text view
        var (game, seller, _, storage, _) = Traders();
        storage.Contents["stone"] = 120;

        var refused = game.World.PlaceExchangeOrder(seller.Id, "stone", true, 500, 0);

        Assert.IsFalse(refused.Success);
        Assert.AreEqual("A sell order holds its goods in your Storages: you have 120 stone there, not 500.", refused.Error);
    }

    [TestMethod]
    public void CancellingAnOrderGivesEverythingBack()
    {
        var (game, seller, buyer, sellerStorage, _) = Traders();
        sellerStorage.Contents["ironOre"] = 500;
        game.World.PlaceExchangeOrder(seller.Id, "ironOre", true, 200, 4);
        var gold = buyer.Gold;
        game.World.PlaceExchangeOrder(buyer.Id, "coal", false, 100, 3);
        Assert.AreEqual(gold - 100 * 3, buyer.Gold, "its own price, held");

        Assert.IsTrue(game.World.CancelExchangeOrder(buyer.Id, game.World.ExchangeOrders.Single(t => !t.Selling).Id).Success);
        Assert.AreEqual(gold, buyer.Gold);
        Assert.IsTrue(game.World.CancelExchangeOrder(seller.Id, game.World.ExchangeOrders.Single().Id).Success);
        game.Pass(1);
        Assert.AreEqual(500, sellerStorage.Contents["ironOre"], 1e-9);
    }
}
