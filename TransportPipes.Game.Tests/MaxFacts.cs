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
