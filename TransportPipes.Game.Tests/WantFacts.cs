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
        var price = 500 * game.World.PriceFor(ben, ana, "bricks");
        Assert.AreEqual(GameWorld.DefaultCrewPrice * game.World.BasePrice("bricks"), game.World.PriceFor(ben, ana, "bricks"), 1e-9, "Ben's crew price");
        Assert.AreEqual(benGold + price, ben.Gold, 1e-6, "Ben is paid his price for Ana");
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
        ana.Asks.Add(new CompanyAsk { Resource = "energy", Amount = 300, Site = site.Id });
        game.ConnectToGate(ben, game.Build(ben, "windMoily"));
        var benGold = ben.Gold;

        game.Pass(60);

        Assert.IsFalse(GameWorld.IsSite(site), $"filled by the Yard: {string.Join(", ", ana.Asks.Select(t => $"{t.Resource} {t.Got}/{t.Amount}"))}");
        Assert.IsGreaterThan(benGold, ben.Gold, "Ben's energy was paid for");
    }

    [TestMethod]
    public void AWantFilledFromYourOwnGoodsSaysWhatTheyWouldHaveSold()
    {
        // Playtest: after Grow, "I went from 3900 to 3400, why the hell did that happen?": the levels took her own goods
        var (game, ana, _) = Station();
        game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));
        var site = MarketHall(game, ana);
        ana.Asks.RemoveAll(t => t.Site == site.Id);
        ana.Asks.Add(new CompanyAsk { Resource = "energy", Amount = 100_000, Site = site.Id });
        game.ConnectToGate(ana, game.Build(ana, "windMoily"));

        game.Pass(3);

        var want = ana.Asks.Single(t => t.Site == site.Id);
        Assert.IsGreaterThan(0, want.Incoming.GetValueOrDefault(ana.Id), "her own energy goes in");
        Assert.AreEqual(want.Incoming[ana.Id] * game.World.HubBuyPrice(ana, "energy"), game.World.OwnGoodsIntoWants(ana), 1e-6, "what it would have sold for");
    }

    /// <summary>Ana's Smelter with its energy and copper ore, piped to her post and her Yard gate; Ben's Coal Pit with its energy, piped to nothing else yet</summary>
    private static (TestGame Game, Company Ana, Company Ben, Building Smelter, Building Pit) SmelterAndCoal()
    {
        var (game, ana, ben) = Station();
        var pit = game.Build(ben, "coalPit", game.Seed(ben, "coal"));
        game.Connect(ben, game.Build(ben, "windMoily"), pit);
        var smelter = game.Build(ana, "smelter");
        game.Connect(ana, game.Build(ana, "windMoily"), smelter);
        game.Connect(ana, game.Build(ana, "copperMine", game.Seed(ana, "copperOre")), smelter);
        game.Connect(ana, smelter, game.TradingPost(ana));
        game.ConnectToGate(ana, smelter);
        return (game, ana, ben, smelter, pit);
    }

    [TestMethod]
    public void AShortBuildingSaysWhyTheYardDoesntBringIt()
    {
        // Playtest: "why are my buildings paused while my friends are producing coal?"
        var (game, ana, ben, smelter, pit) = SmelterAndCoal();
        game.Pass(5);
        StringAssert.Contains(game.World.IdleStatusOf(smelter, plan: false).Reach,
            "In your Yard, Ben's pipes don't reach their Yard gate. Or buy it from the hub: open your Trading Post, open Buy from the hub and tick Coal");

        // Piped to his post (next to his Yard gate): his spare coal comes
        game.Connect(ben, pit, game.TradingPost(ben));
        game.Pass(5);
        Assert.IsGreaterThan(0.9, game.World.NetworkOf(smelter.Id)!.Solution.Rate(smelter.Id), game.World.IdleLine(smelter));
    }

    [TestMethod]
    public void TheIncomeLinesAddUpToTheTopBarsGoldAMinute()
    {
        // Playtest: "add a very detailed explanation what makes this total gold come to be"; Ben sells coal through the
        // Yard, Ana buys it and sells copper ingots and energy at her hub
        var (game, ana, ben, smelter, pit) = SmelterAndCoal();
        game.Connect(ben, pit, game.TradingPost(ben));
        game.Pass(5);
        var gold = game.World.GoldPerMinute();

        foreach (var company in new[] { ana, ben })
        {
            var lines = game.World.IncomeBreakdown(company);
            Assert.AreEqual(gold.GetValueOrDefault(company.Id), lines.Sum(t => t.Gold), 1e-6, string.Join(" | ", lines.Select(t => $"{t.Kind} {t.Resource} {t.Who} {t.Gold:0.00}")));
        }
        var anas = game.World.IncomeBreakdown(ana);
        Assert.IsTrue(anas.Any(t => t.Kind == "yardBought" && t.Resource == "coal" && t.Gold < 0), "Ana pays for Ben's coal");
        Assert.IsTrue(anas.Any(t => t.Kind == "hub" && t.Resource == "copperIngot" && t.Gold > 0), "and sells her ingots at the hub");
        Assert.IsTrue(game.World.IncomeBreakdown(ben).Any(t => t.Kind is "yardSold" or "yardOnItsWay" && t.Resource == "coal" && t.Who == "Ana"), "Ben is paid by Ana");
    }

    [TestMethod]
    public void AnImportIsALineOfItsOwn()
    {
        var (game, ana, _, smelter, _) = SmelterAndCoal();
        Assert.IsTrue(game.World.SetImport(ana.Id, game.TradingPost(ana).Id, "coal", true).Success);
        game.Pass(3);

        var lines = game.World.IncomeBreakdown(ana);
        Assert.IsTrue(lines.Any(t => t.Kind == "import" && t.Resource == "coal" && t.Gold < 0), string.Join(" | ", lines.Select(t => $"{t.Kind} {t.Resource} {t.Gold:0.00}")));
        Assert.AreEqual(game.World.GoldPerMinute().GetValueOrDefault(ana.Id), lines.Sum(t => t.Gold), 1e-6);
    }

    [TestMethod]
    public void TheYardLineSaysWhoStoppedTheTrade()
    {
        // Playtest: "you and Gkoow Pipes don't trade" left open whose switch it was
        var (game, ana, ben, smelter, pit) = SmelterAndCoal();
        game.Connect(ben, pit, game.TradingPost(ben));
        Assert.IsTrue(game.World.SetTradeBlocked(ben.Id, ana.Id, true).Success);
        game.Pass(3);
        StringAssert.Contains(game.World.IdleStatusOf(smelter, plan: false).Reach, "Ben stopped trading with you: only they can switch it back on");

        Assert.IsTrue(game.World.SetTradeBlocked(ben.Id, ana.Id, false).Success);
        Assert.IsTrue(game.World.SetTradeBlocked(ana.Id, ben.Id, true).Success);
        game.Pass(3);
        StringAssert.Contains(game.World.IdleStatusOf(smelter, plan: false).Reach, "you stopped trading with Ben: switch them on again in Trades › Settings › Who you trade with");
    }

    [TestMethod]
    public void WithNobodyMakingItTheYardLineSaysWhereElseItComesFrom()
    {
        var (game, ana, ben, smelter, pit) = SmelterAndCoal();
        game.World.Demolish(ben.Id, pit.Id);
        game.Pass(2);
        StringAssert.Contains(game.World.IdleStatusOf(smelter, plan: false).Reach, "Nobody in your Yard makes coal: build it yourself, a crew mate elsewhere can send it over a link, or buy it from the hub");
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
    public void UpgradeClickedAgainDoesNotAskTwice()
    {
        // Playtest: every click on the network card's Upgrade made another Copper Pipe want on the same 13 tiles, each paid
        var (game, ana, _, pipe) = GreyPipe();
        var gold = ana.Gold;
        Assert.IsTrue(game.World.UpgradePipes(ana.Id, pipe, 2).Success);

        var again = game.World.UpgradePipes(ana.Id, pipe, 2);

        Assert.IsFalse(again.Success);
        StringAssert.Contains(again.Error, "wait for their Copper Pipe already");
        Assert.HasCount(1, ana.Asks.Where(t => t.PipeTiles != null).Select(t => t.WantId).Distinct());
        Assert.AreEqual(gold - 10 * pipe.Count, ana.Gold, 1e-6, "paid once");
        CollectionAssert.AreEquivalent(pipe, game.World.PipeTilesWaiting(ana, 2).ToList());
    }

    [TestMethod]
    public void PipeWantsWithNothingLeftToDoAreGivenBack()
    {
        // Wants made twice before the fix: once one is laid, the other gives its gold back
        var (game, ana, ben, pipe) = GreyPipe();
        Assert.IsTrue(game.World.UpgradePipes(ana.Id, pipe, 2).Success);
        var first = ana.Asks.Single(t => t.PipeTiles != null);
        ana.Asks.Add(new CompanyAsk
        {
            Resource = first.Resource, Amount = first.Amount, PipeTiles = [.. first.PipeTiles!], PipeTier = 2, WantId = first.WantId + 1, Raised = game.Time,
        });
        ana.Gold -= 10 * pipe.Count;
        var gold = ana.Gold;

        Store(game, ben, "copperIngot", 100);
        var benGold = ben.Gold;
        var target = game.World.WantsNear(ben).First().Target;
        Assert.IsTrue(game.World.SendToSite(ben.Id, target).Success);
        var paid = ben.Gold - benGold;

        Assert.IsTrue(pipe.All(t => game.World.Conductors.First(c => c.Pos == t).Tier == 2), "copper now");
        Assert.IsEmpty(ana.Asks, "the other want had nothing left to do");
        Assert.AreEqual(gold - paid + 10 * pipe.Count, ana.Gold, 1e-6, "its gold came back (Ben's ingots paid)");
        var events = game.World.TakeChanges().Events;
        Assert.IsTrue(events.Any(t => t.Company == ana.Id && t.Text.Contains("had nothing left to do")), string.Join(" | ", events.Select(t => t.Text)));
    }

    [TestMethod]
    public void TwoWantsForTheSamePipesKeepTheOneThatGotFurthest()
    {
        var (game, ana, _, pipe) = GreyPipe();
        Assert.IsTrue(game.World.UpgradePipes(ana.Id, pipe, 2).Success);
        var first = ana.Asks.Single(t => t.PipeTiles != null);
        ana.Asks.Add(new CompanyAsk
        {
            Resource = first.Resource, Amount = first.Amount, Got = 2, PipeTiles = [.. first.PipeTiles!], PipeTier = 2, WantId = first.WantId + 1, Raised = game.Time,
        });
        var gold = ana.Gold;

        game.World.TidyAllPipeWants();

        Assert.AreEqual(first.WantId + 1, ana.Asks.Single(t => t.PipeTiles != null).WantId, "the one with 2 ingots in stays");
        Assert.AreEqual(gold + 10 * pipe.Count, ana.Gold, 1e-6, "the other's gold came back");
    }

    [TestMethod]
    public void AFullGreyPipeOffersCopper()
    {
        // A moily that makes more than its grey pipe carries: its fix is the Copper Pipe on that link
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 10_000_000, level: 30);
        company.Researched.Add("copperPipes");
        var map = game.Map(company, game.Local(company, 1, 1), "W+T", pipeTier: 1);
        game.UpgradeNow(company, [map[(0, 0)].Id], 14);
        game.Pass(2);

        var fix = game.World.IdleStatusOf(map[(0, 0)]).Fix;

        Assert.AreEqual(IdleFixKind.Climb, fix?.Kind);
        StringAssert.StartsWith(fix!.Label, "Copper Pipe on its full pipe (1 tile)");
        Assert.AreEqual(2, game.World.PipeDoorFor(company, map[(0, 0)])!.Tier);
    }

    [TestMethod]
    public void BuyTheRestFillsAWantAtOnceAtTheMarketsPrice()
    {
        var (game, ana, _) = Station();
        game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));
        var site = MarketHall(game, ana);
        var gold = ana.Gold;

        Assert.IsTrue(game.World.OrderRest(ana.Id, site.Id).Success);

        Assert.AreEqual(gold - 500 * game.World.BasePrice("bricks") * 2.5, ana.Gold, 1, "the market's 2.5× (1.4 §9)");
        Assert.IsFalse(GameWorld.IsSite(site), "it has them all, and is built");
    }

    [TestMethod]
    public void AWantTakesTheMarketsGoodsUnlessItsOwnerTurnedThatOff()
    {
        var (game, ana, _) = Station();
        Assert.IsTrue(game.World.SetWantsFromMarket(ana.Id, false).Success);
        game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));
        var site = MarketHall(game, ana);
        game.Pass(5);
        Assert.AreEqual(0, game.World.WantsOf(site).Single().Got, 1e-9, "nobody brings them, and the market is off");
        var post = game.TradingPost(ana);
        game.World.SetImport(ana.Id, post.Id, "bricks", true);
        var gold = ana.Gold;

        game.Pass(12);

        Assert.IsFalse(GameWorld.IsSite(site), "importing bricks: the market brought them within about ten minutes");
        Assert.AreEqual(gold - 500 * game.World.BasePrice("bricks") * 2.5, ana.Gold, 1);
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
        Assert.AreEqual(benGold - 500 * game.World.BasePrice("bricks") * 2.5, ben.Gold, 1, "Ben bought them from the market for her");
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

    [TestMethod]
    public void AMovedScaffoldStillWaitsForItsMaterials()
    {
        // Playtest: moved, it came up built without them, and its wants stayed behind ("That has what it needs.")
        var (game, ana, _) = Station();
        Assert.IsTrue(game.World.SetWantsFromMarket(ana.Id, false).Success);
        game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));
        var site = MarketHall(game, ana);
        var to = game.Spot(ana, "marketHall", t => t != site.Position);

        Assert.IsTrue(game.World.Move(ana.Id, site.Id, to).Success);
        game.Pass(5);

        Assert.IsTrue(GameWorld.IsSite(site), "still a scaffold");
        Assert.AreEqual(500, game.World.WantsOf(site).Single().Left, 1e-6, "that wants its bricks");
        Assert.IsTrue(game.World.OrderRest(ana.Id, site.Id).Success, "which can still be bought");
    }

    [TestMethod]
    public void DemolishingAScaffoldGivesItUp()
    {
        var (game, ana, ben) = Station();
        var gold = ana.Gold;
        game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));
        var site = MarketHall(game, ana);
        Store(game, ben, "bricks", 100);
        game.World.SendToSite(ben.Id, site.Id);

        Assert.IsTrue(game.World.Demolish(ana.Id, site.Id).Success);

        Assert.IsNull(game.World.Building(site.Id));
        Assert.AreEqual(gold, ana.Gold, 1e-6, "all its gold back, as Give it up does");
        Assert.IsEmpty(ana.Asks, "no wants left behind");
    }

    [TestMethod]
    public void WantsLeftBehindAreGivenBack()
    {
        // A scaffold moved before the fix: it stands built, its wants still there with what was paid for its goods
        var (game, ana, ben) = Station();
        game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));
        var site = MarketHall(game, ana);
        Store(game, ben, "bricks", 100);
        game.World.SendToSite(ben.Id, site.Id);
        var paid = game.World.WantsOf(site).Single().Paid;
        Assert.IsGreaterThan(0, paid);
        site.ConstructionEndsAt = null;
        var gold = ana.Gold;

        game.World.TidyAllWants();

        Assert.IsEmpty(ana.Asks);
        Assert.AreEqual(gold + paid, ana.Gold, 1e-6, "what it paid for Ben's bricks comes back");
        Assert.IsNotNull(game.World.Building(site.Id), "the building stays");
    }
}
