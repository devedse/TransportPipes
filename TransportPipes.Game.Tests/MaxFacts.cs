using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>Smart Max (F8): the Max button buys the levels a building's pipes can feed</summary>
[TestClass]
public class MaxFacts
{
    [TestMethod]
    public void MaxStopsWhereTheNetworkCanFeedTheBuilding()
    {
        // A Small Factory on one Wind Moily's energy (30 a minute) and a Storage full of iron ore, selling its ingots
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 2_000_000, level: 10);
        var map = game.Map(company, game.Local(company, 1, 1), "O+F+W\n  +\n  T");
        map[(0, 0)].Contents["ironOre"] = 50_000;
        game.World.MarkCompanyNetworksDirty(company.Id);
        game.Pass(2);
        var factory = map[(2, 0)];

        var plan = game.World.SmartMax(company, factory);

        Assert.AreEqual(5, plan.Levels, "level 6 needs 32 of the 30 energy its pipes bring (90% busy is enough); level 7 would need 35");
        Assert.AreEqual(game.World.UpgradeCost(factory, 5), plan.Cost, 1e-6);
        Assert.IsGreaterThan(plan.Levels, plan.AffordableLevels, "2M gold could buy many more levels");
        Assert.IsLessThan(0.9, plan.AffordableShare, $"which would run at {plan.AffordableShare:P0}");
        Assert.AreEqual("energy", plan.Resource);
        Assert.IsFalse(plan.Output);
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
    /// <summary>Five Wind Moilies at level 25 selling an hour and a half to one trading post: far beyond its energy Hub Order</summary>
    private static (TestGame Game, Company Company, Building Post) FloodedPost(int moilies = 5)
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 50_000_000, level: 30);
        var map = game.Map(company, game.Local(company, 1, 1), new string('W', moilies) + "\n" + new string('+', moilies) + "T");
        foreach (var moily in map.Values.Where(t => t.Type == "windMoily"))
        {
            moily.Level = 25;
        }
        game.World.MarkCompanyNetworksDirty(company.Id);
        game.Pass(90);
        return (game, company, map[(moilies, 1)]);
    }

    [TestMethod]
    public void MaxOnAFloodedPostBuysTheLevelsThatRaiseWhatTheMarketPays()
    {
        var (game, company, post) = FloodedPost();
        var sold = game.World.HubMarketOf(company, company.District).SoldRate["energy"];
        Assert.IsGreaterThan(game.World.HubOrder(company, "energy"), sold, "it sells more energy than its Hub Order");

        var plan = game.World.SmartMax(company, post);

        Assert.IsGreaterThan(0, plan.Levels, "every level raises the Hub Order by 10%");
        var gain = game.World.PostLevelGain(company, plan.Levels);
        Assert.IsGreaterThan(0, gain);
        Assert.AreEqual(gain, game.World.UpgradePreview(company, post, plan.Levels).Gain, 1e-6, "the panel says it");
    }

    [TestMethod]
    public void AJustFloodedPostAlreadyShowsWhatALevelBrings()
    {
        // The e2e's plot: eight Moilys grown to level 25 a minute ago; the market's hour average hasn't seen them yet
        var (game, company, post) = FloodedPost();
        game.World.HubMarketOf(company, company.District).SoldRate.Clear();

        Assert.IsGreaterThan(0, game.World.SmartMax(company, post).Levels, "what its posts sell now counts");
        StringAssert.StartsWith(game.World.UpgradePreview(company, post, 1).Text, "+");
    }

    [TestMethod]
    public void MaxOnAPostWithinItsHubOrdersBuysNothing()
    {
        var (game, company, post) = FloodedPost(moilies: 1);
        Assert.IsLessThan(game.World.HubOrder(company, "energy"), game.World.HubMarketOf(company, company.District).SoldRate["energy"]);

        var plan = game.World.SmartMax(company, post);

        Assert.AreEqual(0, plan.Levels, "the market pays the full price for all it sells already");
        Assert.IsGreaterThan(0, plan.AffordableLevels);
        StringAssert.StartsWith(game.World.UpgradePreview(company, post, 1).Text, "adds nothing");
    }

    [TestMethod]
    public void GrowEverythingTakesAFloodedPost()
    {
        var (game, company, post) = FloodedPost();

        var part = game.World.PlanGrowth(company).Parts.SingleOrDefault(t => t.Building == post.Id);

        Assert.IsNotNull(part, "its levels pay back within days");
        Assert.AreEqual(game.World.SmartMax(company, post).Levels, part.Levels);
    }

    [TestMethod]
    public void MaxIsPricedLikeTheUpgrade()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 12_345, level: 10);
        var moily = game.Build(company, "windMoily");
        game.Connect(company, moily, game.TradingPost(company));
        game.Pass(1);

        var levels = game.World.AffordableLevels(company, moily);

        Assert.IsTrue(game.World.UpgradeCost(moily, levels) <= company.Gold, "it can pay for what Max offers");
        Assert.IsTrue(game.World.UpgradeCost(moily, levels + 1) > company.Gold, "and not one level more");
    }
}
