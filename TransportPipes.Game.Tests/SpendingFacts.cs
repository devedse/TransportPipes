using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>Spending is safe (after run 3, the design team's step 1): a solved preview before a purchase, and an hour's Undo</summary>
[TestClass]
public class SpendingFacts
{
    [TestMethod]
    public void AnUpgradeSaysWhatItBrings()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1_000_000, level: 10);
        var map = game.Map(company, game.Local(company, 1, 1), "W+T");
        game.Pass(2);

        var preview = game.World.UpgradePreview(company, map[(0, 0)], 1);

        Assert.IsGreaterThan(0.5, preview.Gain);
        StringAssert.StartsWith(preview.Text, "+");
        StringAssert.EndsWith(preview.Text, "/min");
    }

    [TestMethod]
    public void AnUpgradeThePipeCantCarrySaysSo()
    {
        // Run 3: Cleo's Coal Pit went to level 25 for 117k; its grey pipe carried 100 a minute of its 882
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 10_000_000, level: 30);
        var map = game.Map(company, game.Local(company, 1, 1), "W+T", pipeTier: 1);
        var moily = map[(0, 0)];
        game.World.UpgradeMany(company.Id, [moily.Id], 14);
        game.Pass(2);
        Assert.AreEqual(IdleCause.PipeFull, game.World.IdleReasonOf(moily).Cause, "its 100-a-minute pipe is full");

        var preview = game.World.UpgradePreview(company, moily, 1);

        Assert.AreEqual(0, preview.Gain, 0.5);
        Assert.AreEqual("+0 g/min: its pipes carry 100 energy a minute", preview.Text);
    }

    [TestMethod]
    public void UndoGivesThePriceBackLessWhatItEarnedAndTakesItsXp()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1_000_000, level: 10);
        var map = game.Map(company, game.Local(company, 1, 1), "W+T");
        game.Pass(2);
        var moily = map[(0, 0)];
        var (gold, xp, level) = (company.Gold, company.Xp, moily.Level);

        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 5).Success);
        var cost = gold - company.Gold;
        Assert.IsGreaterThan(xp, company.Xp, "upgrades give XP");
        game.Pass(30);
        var undo = game.World.Undoable(company, moily);
        Assert.IsNotNull(undo);
        Assert.IsLessThan(cost, undo.Value.Refund, "less what it earned in half an hour");
        var refund = undo.Value.Refund;
        var goldBefore = company.Gold;

        Assert.IsTrue(game.World.Undo(company.Id, moily.Id).Success);

        Assert.AreEqual(level, moily.Level, "its levels go");
        Assert.AreEqual(goldBefore + refund, company.Gold, 1e-6);
        Assert.AreEqual(Math.Max(game.World.Content.Progression.XpForLevel(company.Level), xp), company.Xp, 1e-6, "and its XP");
        Assert.IsNull(game.World.Undoable(company, moily), "once");
    }

    [TestMethod]
    public void UndoLastsAnHour()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1_000_000, level: 10);
        var map = game.Map(company, game.Local(company, 1, 1), "W+T");
        game.Pass(2);
        var moily = map[(0, 0)];
        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 1).Success);

        game.Pass(61);

        Assert.IsNull(game.World.Undoable(company, moily));
        Assert.IsFalse(game.World.Undo(company.Id, moily.Id).Success);
    }
}
