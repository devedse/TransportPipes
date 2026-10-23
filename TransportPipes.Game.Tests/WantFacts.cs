using TransportPipes.Game.World;
using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>
/// Wants (after run 3, the design team's step 2): a building short of materials stands as a scaffold that wants them,
/// and its owner's Storages, a friend's Storage and the station's spare fill it (run 3: "I don't know how to hand you
/// 100 bricks directly")
/// </summary>
[TestClass]
public class WantFacts
{
    private static (TestGame Game, Company Ana, Company Ben) Station()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", gold: 1_000_000, level: 20);
        var ben = game.NewCompany("Ben", gold: 1_000_000, level: 20);
        game.Pass(1);
        return (game, ana, ben);
    }

    private static Building MarketHall(TestGame game, Company company) =>
        game.World.Building(game.World.BuildingsOf(company.Id).Where(t => t.Type == "marketHall").Select(t => t.Id).Single())!;

    private static void Store(TestGame game, Company company, string resource, double amount)
    {
        var storage = game.Build(company, "storage");
        game.World.FinishConstruction(storage.Id);
        storage.Contents[resource] = amount;
    }

    [TestMethod]
    public void ABuildingShortOfItsMaterialsStandsAsAScaffoldThatWantsThem()
    {
        var (game, ana, _) = Station();
        Store(game, ana, "bricks", 40);
        var gold = ana.Gold;

        var placed = game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));

        Assert.IsTrue(placed.Success, placed.Error);
        var site = MarketHall(game, ana);
        Assert.IsTrue(GameWorld.IsSite(site), "it stands, waiting for its bricks");
        var want = game.World.WantsOf(site).Single();
        Assert.AreEqual("bricks", want.Resource);
        Assert.AreEqual(40, want.Got, 1e-6, "its Storage gave what it held");
        Assert.AreEqual(460, want.Left, 1e-6);
        Assert.AreEqual(gold - game.World.Content.Building("marketHall").Cost.Gold, ana.Gold, 1e-6, "its gold is paid");
    }

    [TestMethod]
    public void AFriendsStorageFillsItWithOneTapAndItIsBuilt()
    {
        var (game, ana, ben) = Station();
        game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));
        var site = MarketHall(game, ana);
        Store(game, ben, "bricks", 800);
        var (anaGold, benGold) = (ana.Gold, ben.Gold);
        game.World.TakeChanges();

        var sent = game.World.SendToSite(ben.Id, site.Id);

        Assert.IsTrue(sent.Success, sent.Error);
        Assert.IsFalse(GameWorld.IsSite(site), "it has its bricks");
        Assert.IsTrue(site.UnderConstruction, "and is being built");
        Assert.AreEqual(300, game.World.Materials(ben.Id).GetValueOrDefault("bricks"), 1e-6, "500 left Ben's Storage");
        var price = 500 * game.World.PlayerPrice("bricks");
        Assert.AreEqual(benGold + price, ben.Gold, 1e-6, "Ben is paid the one player price");
        Assert.AreEqual(anaGold - price, ana.Gold, 1e-6);
        var events = game.World.TakeChanges().Events;
        Assert.IsTrue(events.Any(t => t.Company == ana.Id && t.Text.Contains("Ben") && t.Text.Contains("Market Hall")), string.Join(" | ", events.Select(t => t.Text)));
        Assert.IsTrue(events.Any(t => t.Company == ben.Id && t.Text.StartsWith("Your bricks built Ana's Market Hall")), string.Join(" | ", events.Select(t => t.Text)));
    }

    [TestMethod]
    public void TheStationsSpareFillsAWantThroughTheYardOwnGoodsFirst()
    {
        var (game, ana, ben) = Station();
        game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));
        var site = MarketHall(game, ana);
        // The engine is the same for any good: an energy want, which Ben's and Ana's moilies have spare at their gates
        ana.Asks.RemoveAll(t => t.Site == site.Id);
        ana.Asks.Add(new CompanyAsk { Resource = "energy", Amount = 300, Until = DateTime.MaxValue, Site = site.Id });
        game.ConnectToGate(ben, game.Build(ben, "windMoily"));
        var benGold = ben.Gold;

        game.Pass(60);

        Assert.IsFalse(GameWorld.IsSite(site), $"filled by the Yard: {string.Join(", ", ana.Asks.Select(t => $"{t.Resource} {t.Got}/{t.Amount}"))}");
        Assert.IsGreaterThan(benGold, ben.Gold, "Ben's energy was paid for");
    }

    /// <summary>Ana's grey pipe from a moily to her trading post, Copper Pipe researched</summary>
    private static (TestGame Game, Company Ana, Company Ben, List<TilePos> Pipe) GreyPipe()
    {
        var (game, ana, ben) = Station();
        ana.Researched.Add("copperPipes");
        var moily = game.Build(ana, "windMoily");
        var pipe = game.Connect(ana, moily, game.TradingPost(ana));
        game.Pass(1);
        return (game, ana, ben, pipe);
    }

    [TestMethod]
    public void ACopperPipeWithoutItsIngotsWantsThem()
    {
        // Run 3: Cleo's Copper Pipe click did nothing; its 28 ingots had to come from her own Storage (cleo d21 #399–#404)
        var (game, ana, ben, pipe) = GreyPipe();
        var gold = ana.Gold;

        Assert.IsTrue(game.World.UpgradePipes(ana.Id, pipe, 2).Success);

        var want = ana.Asks.Single(t => t.PipeTiles != null);
        Assert.AreEqual("copperIngot", want.Resource);
        Assert.AreEqual(pipe.Count, want.Amount, 1e-6, "an ingot a tile");
        Assert.IsTrue(pipe.All(t => game.World.Conductors.First(c => c.Pos == t).Tier == 1), "the pipe carries on as it was meanwhile");
        Assert.AreEqual(gold - 10 * pipe.Count, ana.Gold, 1e-6, "its gold is paid");

        // Ben's Storage has copper ingots: one tap and the pipe is copper
        Store(game, ben, "copperIngot", 100);
        var target = game.World.WantsNear(ben).Single().Target;
        Assert.IsTrue(game.World.SendToSite(ben.Id, target).Success);
        Assert.IsTrue(pipe.All(t => game.World.Conductors.First(c => c.Pos == t).Tier == 2), "copper now");
        Assert.IsEmpty(ana.Asks);
    }

    [TestMethod]
    public void APipeWantGivenUpGivesItsGoldBack()
    {
        var (game, ana, _, pipe) = GreyPipe();
        var gold = ana.Gold;
        game.World.UpgradePipes(ana.Id, pipe, 2);
        var id = ana.Asks.Single().WantId;

        Assert.IsTrue(game.World.CancelPipeWant(ana.Id, id).Success);

        Assert.AreEqual(gold, ana.Gold, 1e-6);
        Assert.IsTrue(pipe.All(t => game.World.Conductors.First(c => c.Pos == t).Tier == 1));
    }

    [TestMethod]
    public void AFullGreyPipeOffersCopper()
    {
        // A moily that makes more than its grey pipe carries: its fix is the Copper Pipe on that link
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 10_000_000, level: 30);
        company.Researched.Add("copperPipes");
        var map = game.Map(company, game.Local(company, 1, 1), "W+T", pipeTier: 1);
        game.World.UpgradeMany(company.Id, [map[(0, 0)].Id], 14);
        game.Pass(2);

        var fix = game.World.IdleStatusOf(map[(0, 0)]).Fix;

        Assert.AreEqual(IdleFixKind.PipeTier, fix?.Kind);
        Assert.AreEqual(2, fix!.Tier);
        StringAssert.StartsWith(fix.Label, "Copper Pipe on its pipe · 1 tile");
    }

    [TestMethod]
    public void AnOrderShipsTheRestOverSixHoursWhenNobodyNearMakesIt()
    {
        var (game, ana, _) = Station();
        game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));
        var site = MarketHall(game, ana);
        var gold = ana.Gold;

        Assert.IsTrue(game.World.OrderRest(ana.Id, site.Id).Success);
        Assert.AreEqual(gold - 500 * game.World.BasePrice("bricks") * GameWorld.OrderPrice, ana.Gold, 1, "paid up front at 2 × base");
        game.Pass(3 * 60);
        var want = game.World.WantsOf(site).Single();
        Assert.AreEqual(250, want.Got, 15, "half in three hours");
        game.Pass(3 * 60 + 5);

        Assert.IsFalse(GameWorld.IsSite(site), "in six hours it has them all, and is built");
    }

    [TestMethod]
    public void AnOrderWaitsForTheFriendWhoHoldsItThenFriendsComeFirst()
    {
        // The friends' window: Ben holds bricks and hasn't played since Ana's Market Hall wanted them
        var (game, ana, ben) = Station();
        Store(game, ben, "bricks", 800);
        ben.LastSeenAt = game.World.Now.AddHours(-1);
        game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));
        var site = MarketHall(game, ana);
        game.World.OrderRest(ana.Id, site.Id);
        var paid = ana.Gold;

        game.Pass(60);
        var want = game.World.WantsOf(site).Single();
        Assert.AreEqual(0, want.Got, 1e-6, "the hub waits for Ben's turn");
        StringAssert.Contains(game.World.OrderStartText(ana, want, ana), "Ben's next session");

        // Ben comes and sends his: what he brings comes off the order, its gold back to Ana
        ben.LastSeenAt = game.World.Now;
        Assert.IsTrue(game.World.SendToSite(ben.Id, site.Id).Success);

        Assert.IsFalse(GameWorld.IsSite(site));
        Assert.AreEqual(paid + 500 * game.World.BasePrice("bricks") * GameWorld.OrderPrice - 500 * game.World.PlayerPrice("bricks"), ana.Gold, 1,
            "the order's gold back, Ben's bricks paid at the one player price");
    }

    [TestMethod]
    public void ANeighbourCanOrderTheRestForAFriend()
    {
        var (game, ana, ben) = Station();
        game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));
        var site = MarketHall(game, ana);
        var (anaGold, benGold) = (ana.Gold, ben.Gold);

        Assert.IsTrue(game.World.OrderRest(ben.Id, site.Id).Success);

        Assert.AreEqual(anaGold, ana.Gold, 1e-6, "Ana pays nothing");
        Assert.IsLessThan(benGold, ben.Gold, "Ben paid the hub for her");
        game.Pass(6 * 60 + 5);
        Assert.IsFalse(GameWorld.IsSite(site));
    }

    [TestMethod]
    public void CancelGivesTheGoldBack()
    {
        var (game, ana, ben) = Station();
        var gold = ana.Gold;
        game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));
        var site = MarketHall(game, ana);
        Store(game, ben, "bricks", 100);
        game.World.SendToSite(ben.Id, site.Id);

        Assert.IsTrue(game.World.CancelSite(ana.Id, site.Id).Success);

        Assert.IsNull(game.World.Building(site.Id), "the scaffold goes");
        Assert.AreEqual(gold, ana.Gold, 1e-6, "with all the gold: the building's and the bricks'");
        Assert.IsEmpty(ana.Asks);
    }
}
