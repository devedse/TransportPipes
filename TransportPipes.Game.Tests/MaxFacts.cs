using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>Smart Max (F8): the Max button buys the levels a building's pipes can feed</summary>
[TestClass]
public class MaxFacts
{
    [TestMethod]
    public void MaxStopsWhereTheNetworkCanFeedTheBuilding()
    {
        // Cleo's Lab, small: one factory (iron ore from a full Storage, energy from a moily) feeds the Lab 6 ingots a
        // minute, and another moily its energy
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 2_000_000, level: 10);
        var map = game.Map(company, game.Local(company, 1, 1), "O+F+R+W\n  +\n  W");
        map[(0, 0)].Contents["ironOre"] = 50_000;
        game.World.MarkCompanyNetworksDirty(company.Id);
        game.Pass(2);
        var lab = map[(4, 0)];

        var plan = game.World.SmartMax(company, lab);

        Assert.AreEqual(8, plan.Levels, "level 9 needs 4.3 ingots of the 6 its pipes bring; level 10 would need 9.4");
        Assert.AreEqual(game.World.UpgradeCost(lab, 8), plan.Cost, 1e-6);
        Assert.IsTrue(plan.AffordableLevels > plan.Levels, "2M gold could buy many more levels");
        Assert.IsTrue(plan.AffordableShare < 0.3, $"which would run at {plan.AffordableShare:P0}");
        Assert.IsTrue(plan.Resource is "ironIngot" or "energy");
        Assert.IsFalse(plan.Output);
    }

    [TestMethod]
    public void MaxStopsWhereThePostIsFull()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 10_000_000, level: 10);
        var map = game.Map(company, game.Local(company, 1, 1), "W+T");
        game.Pass(2);
        var moily = map[(0, 0)];
        var post = map[(2, 0)];
        var capacity = game.World.TradingPostThroughput(post, game.World.Content.Building("tradingPost"));

        var plan = game.World.SmartMax(company, moily);

        var output = game.World.ToFlowNode(moily)!.Out["energy"];
        var multiplier = (int levels) => game.World.Content.Progression.OutputMultiplier(moily.Level + levels) / game.World.Content.Progression.OutputMultiplier(moily.Level);
        Assert.IsTrue(output * multiplier(plan.Levels) <= capacity / GameWorld.UsefulShare + 1e-6, "it sells what it makes at the useful level");
        Assert.IsTrue(output * multiplier(plan.Levels + 1) > capacity / GameWorld.UsefulShare, "one level more would make more than the post sells");
        Assert.IsTrue(plan.Output, "its output holds it back");
        Assert.AreEqual("energy", plan.Resource);
    }

    [TestMethod]
    public void MaxStopsWhereThePipeIsFull()
    {
        // Run 3: Cleo's Coal Pit went to level 25 for 117k and made 882 coal a minute; its grey pipe to the trading post
        // carried 100 (cleo d06 #184, d21 #393). A grey connection carries 100 a minute: Max stops where that is full.
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 10_000_000, level: 30);
        var map = game.Map(company, game.Local(company, 1, 1), "W+T", pipeTier: 1);
        game.Pass(2);
        var moily = map[(0, 0)];

        var plan = game.World.SmartMax(company, moily);

        var output = game.World.ToFlowNode(moily)!.Out["energy"];
        var multiplier = (int levels) => game.World.Content.Progression.OutputMultiplier(moily.Level + levels) / game.World.Content.Progression.OutputMultiplier(moily.Level);
        Assert.IsTrue(output * multiplier(plan.Levels) <= 100 / GameWorld.UsefulShare + 1e-6, $"the pipe carries what it makes at the useful level ({output * multiplier(plan.Levels):0.#} a minute)");
        Assert.IsTrue(output * multiplier(plan.Levels + 1) > 100 / GameWorld.UsefulShare, "one level more would make more than its pipe carries");
        Assert.IsTrue(plan.Output, "its output holds it back");
    }

    /// <summary>A Wind Moily that makes far more than its Trading Post sells, and a bank that buys many levels</summary>
    private static (TestGame Game, Company Company, Building Moily, Building Post) FullPost()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 50_000_000, level: 30);
        var map = game.Map(company, game.Local(company, 1, 1), "W+T");
        game.World.UpgradeMany(company.Id, [map[(0, 0)].Id], 30);
        game.Pass(2);
        return (game, company, map[(0, 0)], map[(2, 0)]);
    }

    [TestMethod]
    public void MaxOnAFullPostBuysTheLevelsItsBuildingsKeepBusy()
    {
        // After run 4's bots: Max bought every level the bank could on a Trading Post, so Grow left the full post out
        var (game, company, moily, post) = FullPost();
        Assert.IsTrue(game.World.TradingPostFull(post), "the post sells all it can");
        var offered = game.World.ToFlowNode(moily)!.Out["energy"] * game.World.ToFlowNode(moily)!.MaxRate;
        var now = game.World.TradingPostThroughput(post, game.World.Content.Building("tradingPost"));
        var throughput = (int levels) => now * game.World.Content.Progression.OutputMultiplier(post.Level + levels) / game.World.Content.Progression.OutputMultiplier(post.Level);

        var plan = game.World.SmartMax(company, post);

        Assert.IsGreaterThan(0, plan.Levels, "a full post grows");
        Assert.IsLessThan(plan.AffordableLevels, plan.Levels, "not every level the bank buys");
        Assert.IsTrue(offered / throughput(plan.Levels) >= GameWorld.UsefulShare, $"busy at the useful level ({offered:0} of {throughput(plan.Levels):0} a minute)");
        Assert.IsTrue(offered / throughput(plan.Levels + 1) < GameWorld.UsefulShare, "one level more would stand idle");
        Assert.IsFalse(plan.Output);
        Assert.AreEqual(offered, plan.Have, offered * 0.02, "what its buildings bring");
    }

    [TestMethod]
    public void MaxOnAPostWithRoomBuysNothing()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 50_000_000, level: 30);
        var map = game.Map(company, game.Local(company, 1, 1), "W+T");
        game.Pass(2);
        Assert.IsFalse(game.World.TradingPostFull(map[(2, 0)]));

        var plan = game.World.SmartMax(company, map[(2, 0)]);

        Assert.AreEqual(0, plan.Levels, "a level more would sell nothing more");
        Assert.IsGreaterThan(0, plan.AffordableLevels);
        Assert.IsLessThan(0.9, plan.AffordableShare);
    }

    [TestMethod]
    public void GrowEverythingTakesAFullPost()
    {
        var (game, company, _, post) = FullPost();

        var part = game.World.PlanGrowth(company).Parts.SingleOrDefault(t => t.Building == post.Id);

        Assert.IsNotNull(part, "the full post's levels pay back within a week");
        Assert.AreEqual(game.World.SmartMax(company, post).Levels, part.Levels);
    }

    [TestMethod]
    public void WhatPaysBackWithinTheHourComesBeforeAGift()
    {
        // After run 4's bots: Mia's Trading Post, back in 5 minutes, waited under the storey's purse to day 28
        var (game, company, _, post) = FullPost();
        game.World.WonderBuysEnabled = true;
        game.Pass(1);
        game.World.EraWonder!.Required = new Dictionary<string, double> { ["bricks"] = 1_000 };

        var steps = game.World.NextSteps(company, 8);

        var full = steps.FindIndex(t => t.Building == post.Id);
        var gift = steps.FindIndex(t => t.Kind == AdvisorKind.Shared);
        Assert.IsGreaterThanOrEqualTo(0, gift, string.Join(" | ", steps.Select(t => t.Title)));
        Assert.IsGreaterThanOrEqualTo(0, full, string.Join(" | ", steps.Select(t => t.Title)));
        Assert.IsLessThanOrEqualTo(60, steps[full].Cost / steps[full].GainPerMinute, "back within the hour");
        Assert.IsLessThan(gift, full, string.Join(" | ", steps.Select(t => t.Title)));
    }

    [TestMethod]
    public void MaxIsPricedLikeTheUpgrade()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 123_456, level: 10);
        var moily = game.Build(company, "windMoily");
        game.Connect(company, moily, game.TradingPost(company));
        game.Pass(1);

        var levels = game.World.AffordableLevels(company, moily);

        Assert.IsTrue(game.World.UpgradeCost(moily, levels) <= company.Gold, "it can pay for what Max offers");
        Assert.IsTrue(game.World.UpgradeCost(moily, levels + 1) > company.Gold, "and not one level more");
    }
}
