using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>Building levels (1.4 §9): priced per copy, up to 25, goods from level 11 that come in like building materials</summary>
[TestClass]
public class LevelFacts
{
    [TestMethod]
    public void ACopysLevelsCostFromWhatThatCopyCost()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1_000_000, level: 10);
        var first = game.Build(company, "windMoily");
        for (var i = 0; i < 9; i++)
        {
            game.Build(company, "windMoily");
        }
        var tenth = game.World.BuildingsOf(company.Id).Where(t => t.Type == "windMoily").MaxBy(t => t.Id)!;

        Assert.AreEqual(200, game.World.UpgradeCost(first, 1));
        Assert.AreEqual(Math.Round(200 * Math.Pow(1.07, 9)), tenth.Price, "every copy 7% more than the last");
        Assert.AreEqual(tenth.Price, game.World.UpgradeCost(tenth, 1), 1, "and its levels cost from it");
        Assert.AreEqual(Math.Round(200 * Math.Pow(1.15, 4)), game.World.UpgradeCost(first, 5) - game.World.UpgradeCost(first, 4), 1, "1.15 more for every level it has");
    }

    /// <summary>A Wind Moily at level 10 on a pipe to the trading post, and a Storage</summary>
    private static (TestGame Game, Company Company, Building Moily, Building Storage) MoilyAtTen()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1_000_000, level: 10);
        var map = game.Map(company, game.Local(company, 1, 1), "W+T\n O");
        game.SetLevel(map[(0, 0)], 10);
        return (game, company, map[(0, 0)], map[(1, 1)]);
    }

    [TestMethod]
    public void FromLevelElevenAStepWaitsForItsGoodsAtItsOldLevel()
    {
        var (game, company, moily, _) = MoilyAtTen();
        Assert.IsTrue(game.World.SetWantsFromMarket(company.Id, false).Success);
        var output = game.World.ToFlowNode(moily)!.Out["energy"];
        var gold = company.Gold;
        var cost = game.World.UpgradeCost(moily, 1);

        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 1).Success);

        Assert.AreEqual(gold - cost, company.Gold, 1e-6, "the gold at once");
        Assert.AreEqual(10, moily.Level, "it works at its old level under a little scaffolding");
        Assert.AreEqual(11, game.World.BoughtLevel(moily));
        Assert.IsTrue(game.World.WaitsForLevel(moily));
        var wants = game.World.WantsOf(moily);
        CollectionAssert.AreEquivalent(new[] { "planks", "bricks" }, wants.Select(t => t.Resource).ToList());
        Assert.AreEqual(cost * 0.3, wants.Sum(t => t.Amount * game.World.BasePrice(t.Resource)), cost * 0.03, "goods worth 30% of its gold");
        game.Pass(10);
        Assert.AreEqual(output, game.World.ToFlowNode(moily)!.Out["energy"], 1e-9, "nobody brings them: it stays where it was");
    }

    [TestMethod]
    public void AStepTakesWhatTheStoragesHoldAndStepsUpWhenItsGoodsAreIn()
    {
        var (game, company, moily, storage) = MoilyAtTen();
        storage.Contents["planks"] = 1000;
        storage.Contents["bricks"] = 1000;
        var output = game.World.ToFlowNode(moily)!.Out["energy"];

        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 1).Success);

        Assert.AreEqual(11, moily.Level, "its Storage held them: at once");
        Assert.IsFalse(game.World.WaitsForLevel(moily));
        Assert.IsLessThan(1000, storage.Contents["planks"]);
        Assert.AreEqual(output * 1.1, game.World.ToFlowNode(moily)!.Out["energy"], 1e-9);
    }

    [TestMethod]
    public void TheMarketBringsTheRestWithinAboutTenMinutes()
    {
        var (game, company, moily, _) = MoilyAtTen();
        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 1).Success);
        var gold = company.Gold;
        var worth = game.World.WantsOf(moily).Sum(t => t.Amount * game.World.BasePrice(t.Resource));
        var income = game.World.GoldPerMinute()[company.Id];
        game.Pass(11);

        Assert.AreEqual(11, moily.Level, "nobody near makes planks and bricks: the market brought them");
        Assert.AreEqual(gold - worth * 2.5 + income * 11, company.Gold, worth * 0.1 + income, "the goods at the market's 2.5×");
    }

    [TestMethod]
    public void MaxBuysSeveralStepsTheirGoodsComeOneAfterTheOther()
    {
        var (game, company, moily, storage) = MoilyAtTen();
        Assert.IsTrue(game.World.SetWantsFromMarket(company.Id, false).Success);

        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 3).Success);
        Assert.AreEqual(13, game.World.BoughtLevel(moily));
        Assert.AreEqual(10, moily.Level);
        Assert.AreEqual(game.World.UpgradeCost(moily, 1), game.World.Content.Progression.UpgradeCost(game.World.CopyPrice(moily), 13), 1, "the next step costs from level 13");

        var first = game.World.LevelGoods(moily, 11);
        foreach (var (good, amount) in first)
        {
            storage.Contents[good] = amount;
        }
        game.Pass(2);

        Assert.AreEqual(11, moily.Level, "the first step's goods were in: one step up");
        Assert.AreEqual(13, game.World.BoughtLevel(moily));
    }

    [TestMethod]
    public void BuyTheRestFillsALevelAtOnce()
    {
        var (game, company, moily, _) = MoilyAtTen();
        Assert.IsTrue(game.World.SetWantsFromMarket(company.Id, false).Success);
        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 2).Success);
        var cost = game.World.OrderCost(company, moily.Id)!.Value;
        var gold = company.Gold;

        Assert.IsTrue(game.World.OrderRest(company.Id, moily.Id).Success);

        Assert.AreEqual(12, moily.Level);
        Assert.AreEqual(gold - cost, company.Gold, 1);
    }

    [TestMethod]
    public void BuyTheRestOfOneGoodBuysOnlyThatGood()
    {
        // Playtest: the planks' "Buy the rest · 302 g" bought the bricks as well, for 609 g, and finished the level
        var (game, company, moily, _) = MoilyAtTen();
        Assert.IsTrue(game.World.SetWantsFromMarket(company.Id, false).Success);
        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 1).Success);
        var goods = game.World.LevelGoods(moily, 11).Keys.Order(StringComparer.Ordinal).ToList();
        Assert.IsGreaterThanOrEqualTo(2, goods.Count, "a step that takes two goods");
        var cost = game.World.OrderCost(company, moily.Id, goods[0])!.Value;
        Assert.IsLessThan(game.World.OrderCost(company, moily.Id)!.Value, cost);
        var gold = company.Gold;

        Assert.IsTrue(game.World.OrderRest(company.Id, moily.Id, goods[0]).Success);

        Assert.AreEqual(gold - cost, company.Gold, 1, "that good's price only");
        Assert.IsNull(game.World.OrderCost(company, moily.Id, goods[0]), "it is in");
        Assert.IsNotNull(game.World.OrderCost(company, moily.Id, goods[1]), "the other still waits");
        Assert.AreEqual(10, moily.Level, "the level waits for the other good");
    }

    [TestMethod]
    public void BuyAllTheRestBuysEveryWantAtOnce()
    {
        // Playtest: "there should be a button to finish all at the top" of the Yard's "What you're waiting for"
        var (game, company, moily, _) = MoilyAtTen();
        var other = game.Build(company, "windMoily");
        game.SetLevel(other, 10);
        Assert.IsTrue(game.World.SetWantsFromMarket(company.Id, false).Success);
        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 3).Success);
        Assert.IsTrue(game.World.Upgrade(company.Id, other.Id, 2).Success);
        var cost = game.World.OrderAllCost(company)!.Value;
        Assert.AreEqual(game.World.OrderCost(company, moily.Id)!.Value + game.World.OrderCost(company, other.Id)!.Value, cost, 2.0, "what both cost");
        var gold = company.Gold;
        company.Gold = cost - 1;

        Assert.IsFalse(game.World.OrderAll(company.Id).Success, "not when the gold doesn't reach");
        Assert.AreEqual(10, moily.Level);
        company.Gold = gold;
        Assert.IsTrue(game.World.OrderAll(company.Id).Success);

        Assert.AreEqual(13, moily.Level);
        Assert.AreEqual(12, other.Level);
        Assert.IsGreaterThan(gold - cost - 1e-6, company.Gold, "at most what it said");
        Assert.IsNull(game.World.OrderAllCost(company), "nothing waits any more");
    }

    [TestMethod]
    public void ABuildingThatGoesGivesBackItsLevelsGoods()
    {
        var (game, company, moily, _) = MoilyAtTen();
        Assert.IsTrue(game.World.SetWantsFromMarket(company.Id, false).Success);
        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 1).Success);
        var goods = game.World.LevelGoods(moily, 11).Keys.First();
        Assert.IsTrue(game.World.OrderRest(company.Id, moily.Id, goods).Success);
        var paid = game.World.WantsOf(moily).Sum(t => t.Paid);
        Assert.IsGreaterThan(0, paid);
        var gold = company.Gold;

        Assert.IsTrue(game.World.Demolish(company.Id, moily.Id).Success);

        Assert.IsEmpty(company.Asks, "no wants left behind");
        Assert.AreEqual(gold + paid + game.World.DemolishRefund(moily), company.Gold, 1, "the goods' gold back too");
    }

    [TestMethod]
    public void UndoTakesBackTheStepsWhoseGoodsAreStillComing()
    {
        var (game, company, moily, storage) = MoilyAtTen();
        Assert.IsTrue(game.World.SetWantsFromMarket(company.Id, false).Success);
        storage.Contents["planks"] = 10;
        var gold = company.Gold;

        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 2).Success);
        Assert.AreEqual(0, storage.Contents.GetValueOrDefault("planks"), 1e-9, "its Storage gave its planks");
        Assert.IsTrue(game.World.Undo(company.Id, moily.Id).Success);

        Assert.AreEqual(10, moily.Level);
        Assert.AreEqual(10, game.World.BoughtLevel(moily));
        Assert.IsEmpty(game.World.WantsOf(moily));
        Assert.AreEqual(gold, company.Gold, 1, "its gold back");
        Assert.AreEqual(10, storage.Contents.GetValueOrDefault("planks"), 1e-9, "and the planks");
    }

    [TestMethod]
    public void AFriendsSpareFeedsALevelThroughTheYard()
    {
        var game = new TestGame(content: TestGame.LosslessContent);
        var ana = game.NewCompany("Ana", level: 10, slot: 0);
        var ben = game.NewCompany("Ben", level: 10, slot: 1);
        Assert.IsTrue(game.World.SetWantsFromMarket(ana.Id, false).Success);
        // Ben makes planks and bricks (from a Storage of wood and stone), piped to the Yard
        var store = game.Build(ben, "storage");
        store.Contents["wood"] = 4000;
        var energy = game.Build(ben, "windMoily");
        var sawmill = game.Build(ben, "sawmill");
        game.Connect(ben, store, sawmill);
        game.Connect(ben, energy, sawmill);
        game.ConnectToGate(ben, sawmill);
        var moily = game.Build(ana, "windMoily");
        game.SetLevel(moily, 10);
        game.ConnectToGate(ana, moily);
        game.Build(ana, "storage").Contents["bricks"] = 1000;
        var benGold = ben.Gold;

        Assert.IsTrue(game.World.Upgrade(ana.Id, moily.Id, 1).Success);
        for (var i = 0; i < 30 && game.World.WaitsForLevel(moily); i++)
        {
            game.Pass(1);
        }

        Assert.AreEqual(11, moily.Level, "Ben's planks came through the Yard");
        Assert.IsGreaterThan(benGold, ben.Gold, "paid at his price");
        Assert.IsEmpty(game.World.WantsOf(moily));
    }

    [TestMethod]
    public void TheCompanyLevelComesFromTheGoldEarnedAndNeverGoesDown()
    {
        var game = new TestGame();
        var company = game.NewCompany(gold: 0);

        game.World.CountEarned(company, 499, "test", "gold");
        Assert.AreEqual(1, company.Level);
        game.World.CountEarned(company, 1, "test", "gold");
        Assert.AreEqual(2, company.Level, "500 gold earned in all");

        company.Gold = 1_000_000;
        game.Build(company, "windMoily");
        Assert.AreEqual(2, company.Level, "spending takes nothing away");
        Assert.AreEqual(500, company.LifetimeEarnings);
        Assert.AreEqual(500, company.EarnedLog["test"]["gold"]);
    }
}
