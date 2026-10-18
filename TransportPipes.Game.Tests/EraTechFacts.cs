using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class EraTechFacts
{
    [TestMethod]
    public void FuturesAllowLongerContracts()
    {
        var game = new TestGame();
        var a = game.NewCompany("A");
        var b = game.NewCompany("B");

        StringAssert.Contains(game.World.ProposeContract(a.Id, b.Id, true, "energy", 10, 2, 8).Error, "Futures");
        a.Researched.Add("futures");
        Assert.IsTrue(game.World.ProposeContract(a.Id, b.Id, true, "energy", 10, 2, 30).Success);
    }

    [TestMethod]
    public void TrainsMakeCargoFaster()
    {
        var game = new TestGame();
        var a = game.NewCompany("A");
        var b = game.NewCompany("B");
        var before = game.World.CargoTime(a, b);

        b.Researched.Add("trains");

        Assert.AreEqual(before / 2, game.World.CargoTime(a, b));
    }

    [TestMethod]
    public void MarketInsightShowsWhereThePriceIsHeading()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        Assert.IsNull(game.World.HubPriceHeading(company, "energy"));

        company.Researched.Add("marketInsight");

        Assert.AreEqual(game.World.BasePrice("energy") * HubMarket.MaxMultiplier, game.World.HubPriceHeading(company, "energy")!.Value, 1e-6, "nobody sells energy: it rises");
    }

    [TestMethod]
    public void AShowroomMakesPartnersPayTenPercentMore()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var a = game.NewCompany("A", level: 20);
        game.NewCompany("B");
        var showroom = game.Map(a, game.Local(a, 20, 20), "H").GetValueOrDefault((0, 0));
        Assert.IsNotNull(showroom);
        var price = game.World.SellPrice(a, "energy");

        Assert.IsTrue(game.World.SetShowroomProduct(a.Id, showroom.Id, "energy").Success);

        Assert.AreEqual(1.1, game.World.ShowroomFactor(a, "energy"));
        Assert.AreEqual(1, game.World.ShowroomFactor(a, "wood"));
        Assert.AreEqual(price, game.World.SellPrice(a, "energy"), "the asked price stays, partners pay the premium on top");
    }

    [TestMethod]
    public void GlobalOrdersTradeAcrossHubs()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        // The 25th company lives in the next district
        var first = game.NewCompany("First");
        Company? far = null;
        for (var i = 0; i < 30 && far == null; i++)
        {
            var next = game.NewCompany($"Filler {i}");
            if (next.District != first.District)
            {
                far = next;
            }
        }
        Assert.IsNotNull(far);
        foreach (var company in new[] { first, far })
        {
            game.Map(company, game.Local(company, 1, 1), "E");
            var storage = game.Map(company, game.Local(company, 3, 1), "O").Values.Single();
            storage.Contents["glass"] = 100;
        }
        var price = game.World.HubBuyPrice(first.District, "glass");

        // Without Global Exchange the orders don't meet
        Assert.IsTrue(game.World.PlaceExchangeOrder(first.Id, "glass", true, 10, price).Success);
        Assert.IsTrue(game.World.PlaceExchangeOrder(far.Id, "glass", false, 10, price).Success);
        Assert.AreEqual(2, game.World.ExchangeOrders.Count());

        first.Researched.Add("globalExchange");
        far.Researched.Add("globalExchange");
        Assert.IsTrue(game.World.PlaceExchangeOrder(first.Id, "glass", true, 5, price).Success);
        Assert.IsTrue(game.World.PlaceExchangeOrder(far.Id, "glass", false, 5, price).Success);
        Assert.AreEqual(2, game.World.ExchangeOrders.Count(), "the two global orders traded");
    }
}
