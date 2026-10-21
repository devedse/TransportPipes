using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class TrunkFacts
{
    /// <summary>A company with Wind Moilies piped to its Yard gate and its trading post; a second district is open</summary>
    private static (TestGame Game, Company Ana) Producer()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.TrunksEnabled = true;
        var ana = game.NewCompany("Ana");
        foreach (var _ in Enumerable.Range(0, 3))
        {
            var moily = game.Build(ana, "windMoily");
            game.ConnectToGate(ana, moily);
            game.Connect(ana, moily, game.TradingPost(ana));
        }
        game.World.OpenNextDistrict();
        game.Pass(3);
        return (game, ana);
    }

    /// <summary>Ana's own hub pays half the base price for energy: she sells far more than its order</summary>
    private static void Flooded(TestGame game, Company ana)
    {
        var market = game.World.HubMarketOf(ana, ana.District);
        market.Announced["energy"] = 0.5;
        market.SoldRate["energy"] = 1000;
        game.World.MarkCompanyNetworksDirty(ana.Id);
        game.World.CoupleNow(3);
    }

    [TestMethod]
    public void AFloodedHubSendsPartOfTheSpareOverThePublicTrunkToTheNextHub()
    {
        var (game, ana) = Producer();
        Assert.IsEmpty(game.World.RemoteSales(ana), "her own hub pays the full price: everything stays home");

        Flooded(game, ana);

        var sale = game.World.RemoteSales(ana).Single();
        Assert.AreEqual("energy", sale.Resource);
        Assert.IsNotNull(sale.Market.Hub, "the neighbouring hub, its own order there");
        Assert.AreEqual(0.02, sale.Market.Fee, "the public trunk's fee");
        Assert.IsLessThan(1, sale.Market.Efficiency, "copper loses on the way");
        var gold = ana.Gold;
        var home = game.World.GoldPerMinute()[ana.Id];
        game.Pass(10);
        Assert.AreEqual(gold + 10 * (home + sale.PerMinute * sale.Price), ana.Gold, 1, "paid what arrives at the far hub's price");
    }

    [TestMethod]
    public void ThePortBuysTier2GoodsForAnyAmount()
    {
        var (game, ana) = Producer();
        var port = game.World.RemoteMarkets(ana).Single(t => t.Port);
        Assert.AreEqual(0.02, port.Fee);
        Assert.IsTrue(game.World.RemoteMarkets(ana).Count(t => !t.Port) >= 1, "and the open neighbouring hubs");
    }

    [TestMethod]
    public void ACrewBuildsATrunkSegmentBySegmentAndItLosesLessThanCopper()
    {
        var (game, ana) = Producer();
        game.World.Era = 2;
        var (_, crew) = game.World.FoundCrew(ana.Id, "Kettle & Co.", "kettle", "#236b69", "#c9963a", 0);
        var storage = game.Build(ana, "storage");
        storage.Contents["steel"] = 5000;
        storage.Contents["concrete"] = 2000;
        var hub = game.World.OpenDistricts.First(t => t != ana.District);

        StringAssert.Contains(game.World.StartTrunk(ana.Id, ana.District, false).Result.Error, "another");
        var (started, trunk) = game.World.StartTrunk(ana.Id, hub, false);
        Assert.IsTrue(started.Success, started.Error);
        Assert.HasCount(1, trunk!.Segments, "the first segment is paid");
        Assert.AreEqual(300, 5000 - storage.Contents["steel"], 1e-9, "300 steel a segment");
        Assert.IsFalse(trunk.Built);
        Assert.IsFalse(game.World.RemoteMarkets(ana).Any(t => t.Trunk == trunk.Id), "not before it is built");

        while (!trunk.Built)
        {
            storage.Contents["steel"] = 5000;
            storage.Contents["concrete"] = 2000;
            Assert.IsTrue(game.World.BuildTrunkSegment(ana.Id, trunk.Id).Success);
        }

        var steel = game.World.RemoteMarkets(ana).Single(t => t.Trunk == trunk.Id);
        var copper = game.World.RemoteMarkets(ana).Single(t => t.Hub == hub && t.Trunk == null);
        Assert.AreEqual(0, steel.Fee, "the crew's own trunk: no fee");
        Assert.IsGreaterThan(copper.Efficiency, steel.Efficiency, "steel loses less than the public copper");
        Assert.AreEqual((int)Math.Ceiling(trunk.Length / 10.0), trunk.Segments.Count, "10 tiles a segment");
        _ = crew;
    }
}
