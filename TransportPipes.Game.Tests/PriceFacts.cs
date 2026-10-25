using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>Your own prices (1.4 §8): public, crew and special prices between 1× and 2.5×, the lowest one counts</summary>
[TestClass]
public class PriceFacts
{
    /// <summary>Ana and Ben share a Yard; Cleo lives in another one</summary>
    private static (TestGame Game, Company Ana, Company Ben, Company Cleo) Three()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", slot: 0);
        var ben = game.NewCompany("Ben", level: 2, slot: 1);
        var cleo = game.NewCompany("Cleo", slot: 12);
        Assert.AreEqual(ana.CrewId, ben.CrewId);
        Assert.AreNotEqual(ana.CrewId, cleo.CrewId);
        return (game, ana, ben, cleo);
    }

    [TestMethod]
    public void TheCrewPaysTheCrewPriceAndEveryoneElseThePublicOne()
    {
        var (game, ana, ben, cleo) = Three();

        Assert.AreEqual(1.15, game.World.PriceFactor(ana, ben, "energy"));
        Assert.AreEqual(1.3, game.World.PriceFactor(ana, cleo, "energy"));
        Assert.AreEqual(1.3 * game.World.BasePrice("ironIngot"), game.World.PriceFor(ana, cleo, "ironIngot"), 1e-9);
    }

    [TestMethod]
    public void ABuyerPaysTheLowestPriceThatAppliesToThem()
    {
        var (game, ana, ben, cleo) = Three();

        Assert.IsTrue(game.World.SetPrice(ana.Id, GameWorld.ForKey(cleo.Id), 1.0).Success);
        Assert.IsTrue(game.World.SetPrice(ana.Id, GameWorld.CrewKey("energy"), 1.05).Success);
        Assert.IsTrue(game.World.SetPrice(ana.Id, GameWorld.PublicKey(), 1.1).Success);

        Assert.AreEqual(1.0, game.World.PriceFactor(ana, cleo, "energy"), "Cleo's special price");
        Assert.AreEqual(1.05, game.World.PriceFactor(ana, ben, "energy"), "the crew's energy");
        Assert.AreEqual(1.1, game.World.PriceFactor(ana, ben, "ironIngot"), "the public price is lower than the crew's 1.15");
    }

    [TestMethod]
    public void APriceStaysBetweenTheMarketsTwoPrices()
    {
        var (game, ana, _, cleo) = Three();

        StringAssert.Contains(game.World.SetPrice(ana.Id, GameWorld.PublicKey(), 0.9).Error, "between 1× and 2.5×");
        StringAssert.Contains(game.World.SetPrice(ana.Id, GameWorld.PublicKey(), 2.6).Error, "between 1× and 2.5×");
        StringAssert.Contains(game.World.SetPrice(ana.Id, GameWorld.ForKey(ana.Id), 1.0).Error, "another player");
        Assert.IsTrue(game.World.SetPrice(ana.Id, GameWorld.PublicKey("energy"), 2.5).Success);
        Assert.AreEqual(1.3, game.World.PriceFactor(ana, cleo, "energy"), "a rise waits an hour");
    }

    [TestMethod]
    public void ARiseWaitsAnHourAndItsBuyersAreToldACutCountsAtOnce()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana");
        var ben = game.NewCompany("Ben", level: 2);
        game.ConnectToGate(ana, game.Build(ana, "windMoily"));
        var mine = game.Build(ben, "ironMine", game.Spot(ben, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(ben, mine);
        game.Connect(ben, mine, game.TradingPost(ben));
        game.Pass(30);
        Assert.IsGreaterThan(0, game.World.UsualRate(ana, ben, "energy"), "Ben buys Ana's energy");
        game.World.TakeChanges();

        Assert.IsTrue(game.World.SetPrice(ana.Id, GameWorld.CrewKey(), 1.25).Success);

        var events = game.World.TakeChanges().Events;
        Assert.IsTrue(events.Any(t => t.Company == ben.Id && t.Text == "Ana's price for goods rises to 1.25× in an hour"), string.Join(" | ", events.Select(t => t.Text)));
        Assert.AreEqual(1.15, game.World.PriceFactor(ana, ben, "energy"));
        game.Pass(61);
        Assert.AreEqual(1.25, game.World.PriceFactor(ana, ben, "energy"), "an hour later");
        Assert.IsTrue(game.World.SetPrice(ana.Id, GameWorld.CrewKey(), 1.6).Success);
        game.Pass(61);
        Assert.AreEqual(1.3, game.World.PriceFactor(ana, ben, "energy"), "the public price is lower: a buyer pays the lowest that applies");

        Assert.IsTrue(game.World.SetPrice(ana.Id, GameWorld.CrewKey(), 1.2).Success);
        Assert.AreEqual(1.2, game.World.PriceFactor(ana, ben, "energy"), "a cut counts at once");
        Assert.IsTrue(game.World.SetPrice(ana.Id, GameWorld.CrewKey(), null).Success);
        Assert.AreEqual(1.15, game.World.PriceFactor(ana, ben, "energy"), "back to the default, a cut too");
    }

    [TestMethod]
    public void TheYardTakesFromTheSellerWithTheLowestPriceFirst()
    {
        // Ana and Dev both have energy spare; Ben's mine needs 10: the cheaper seller gets the trade
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", slot: 0);
        var ben = game.NewCompany("Ben", level: 2, slot: 1);
        var dev = game.NewCompany("Dev", slot: 2);
        foreach (var seller in new[] { ana, dev })
        {
            var moily = game.Build(seller, "windMoily");
            game.ConnectToGate(seller, moily);
            game.Connect(seller, moily, game.TradingPost(seller));
        }
        Assert.IsTrue(game.World.SetPrice(dev.Id, GameWorld.CrewKey("energy"), 1.0).Success);
        var mine = game.Build(ben, "ironMine", game.Spot(ben, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(ben, mine);
        game.Connect(ben, mine, game.TradingPost(ben));

        game.Pass(10);

        var trades = game.World.YardTrades(game.EstateOf(ben)).Where(t => !t.Selling && t.Resource == "energy").ToList();
        Assert.IsTrue(trades.Any(t => t.Partner == dev.Id && t.PerMinute > 5), string.Join(", ", trades.Select(t => $"{t.Partner} {t.PerMinute}")));
        Assert.IsFalse(trades.Any(t => t.Partner == ana.Id && t.PerMinute > 1e-6), "Ana's dearer energy isn't needed");
        Assert.AreEqual(game.World.BasePrice("energy"), game.World.YardPrice(ben, dev, "energy", selling: false), 1e-9, "Ben pays Dev's 1.0×");
    }
}
