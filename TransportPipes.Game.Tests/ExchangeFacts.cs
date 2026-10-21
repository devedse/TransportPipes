using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class ExchangeFacts
{
    private static (TestGame Game, Company Seller, Company Buyer, Building SellerStorage, Building BuyerStorage) Traders()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var seller = game.NewCompany("Seller");
        var buyer = game.NewCompany("Buyer");
        var sellerMap = game.Map(seller, game.Local(seller, 1, 1), "O E");
        var buyerMap = game.Map(buyer, game.Local(buyer, 1, 1), "O E");
        return (game, seller, buyer, sellerMap[(0, 0)], buyerMap[(0, 0)]);
    }

    [TestMethod]
    public void TheExchangeNeedsAnExchangeOffice()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        StringAssert.Contains(game.World.PlaceExchangeOrder(company.Id, "energy", false, 10, 1).Error, "Exchange Office");
    }

    [TestMethod]
    public void MatchingOrdersTradeAtThePlayerPriceAndShipByCargo()
    {
        var (game, seller, buyer, sellerStorage, buyerStorage) = Traders();
        sellerStorage.Contents["ironOre"] = 500;

        Assert.IsTrue(game.World.PlaceExchangeOrder(seller.Id, "ironOre", true, 400, 4).Success);
        Assert.AreEqual(100, sellerStorage.Contents["ironOre"], 1e-9, "the goods are held by the order");
        var goldSeller = seller.Gold;
        var goldBuyer = buyer.Gold;

        // Whatever prices they name, players trade at the one player price
        Assert.IsTrue(game.World.PlaceExchangeOrder(buyer.Id, "ironOre", false, 300, 5).Success);
        var price = game.World.PlayerPrice("ironOre");
        Assert.AreEqual(3 * 1.75, price);

        Assert.AreEqual(goldSeller + 300 * price * 0.95, seller.Gold, 1e-6, "5% fee");
        Assert.AreEqual(goldBuyer - 300 * price, buyer.Gold, 1e-6);
        Assert.AreEqual(100, game.World.ExchangeOrders.Single().Amount, "100 still for sale");
        var shipment = game.World.Shipments.Single();
        Assert.AreEqual(300, shipment.Amount);
        Assert.IsFalse(buyerStorage.Contents.ContainsKey("ironOre"));

        game.Pass((shipment.ArrivesAt - game.Time).TotalMinutes + 1);

        Assert.AreEqual(300, buyerStorage.Contents["ironOre"], 1e-9);
        Assert.IsEmpty(game.World.Shipments);
    }

    [TestMethod]
    public void CancellingAnOrderGivesEverythingBack()
    {
        var (game, seller, buyer, sellerStorage, _) = Traders();
        sellerStorage.Contents["ironOre"] = 500;
        game.World.PlaceExchangeOrder(seller.Id, "ironOre", true, 200, 4);
        var gold = buyer.Gold;
        game.World.PlaceExchangeOrder(buyer.Id, "coal", false, 100, 3);
        Assert.AreEqual(gold - 100 * game.World.PlayerPrice("coal"), buyer.Gold);

        Assert.IsTrue(game.World.CancelExchangeOrder(buyer.Id, game.World.ExchangeOrders.Single(t => !t.Selling).Id).Success);
        Assert.AreEqual(gold, buyer.Gold);
        Assert.IsTrue(game.World.CancelExchangeOrder(seller.Id, game.World.ExchangeOrders.Single().Id).Success);
        game.Pass(1);
        Assert.AreEqual(500, sellerStorage.Contents["ironOre"], 1e-9);
    }
}
