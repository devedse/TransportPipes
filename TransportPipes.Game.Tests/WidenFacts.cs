using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>
/// Widen (after run 3, the design team's step 9): a full Copper or Steel Pipe carries ×2 for an hour of income, ×4 for
/// two hours more, for each link; Grey doesn't widen (run 3: Cleo's Coal Pit could make 882 coal a minute, its pipe
/// carried 100). After run 4 it is the pipe door's first step at a Copper or Steel Pipe's limit
/// </summary>
[TestClass]
public class WidenFacts
{
    /// <summary>A Wind Moily that makes more than its pipe of the given tier carries, piped to a trading post</summary>
    private static (TestGame Game, Company Company, Building Moily, Building Post) FullPipe(int tier, int levels)
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 100_000_000, level: 40);
        company.Researched.Add("copperPipes");
        company.MeasuredIncome = 1_000;
        var map = game.Map(company, game.Local(company, 1, 1), "W+T", pipeTier: tier);
        game.World.UpgradeMany(company.Id, [map[(0, 0)].Id], levels);
        // A post that sells far more than the pipe brings: the pipe is the limit
        game.World.UpgradeMany(company.Id, [map[(2, 0)].Id], 9);
        game.Pass(2);
        return (game, company, map[(0, 0)], map[(2, 0)]);
    }

    private static Conductor PipeTile(TestGame game, Company company) => game.World.Conductors.Single(t => t.Owner == company.Id && t.Pos == game.Local(company, 2, 1));

    private static double Carried(TestGame game, Building moily) => game.World.PipeCarry(moily, "energy").Carried;

    [TestMethod]
    public void AFullCopperPipeWidensForAnHourOfIncomeThenTwoMore()
    {
        var (game, company, moily, _) = FullPipe(2, 60);
        Assert.AreEqual(IdleCause.PipeFull, game.World.IdleReasonOf(moily).Cause, $"it makes {game.World.PipeCarry(moily, "energy").Could:0} a minute");
        Assert.AreEqual(500, Carried(game, moily), 1, "a Copper Pipe carries 500");

        // Its fix is its pipe door: Widen ×2 on its one link, an hour of its income
        Assert.AreEqual(IdleFixKind.Climb, game.World.IdleStatusOf(moily).Fix!.Kind);
        var door = game.World.PipeDoorFor(company, moily)!;
        Assert.AreEqual(PipeDoorKind.Widen, door.Kind);
        Assert.AreEqual(2, door.Tier);
        var income = game.World.MeasuredIncome(company);
        Assert.AreEqual(60 * income, door.Gold, 1, "an hour of its income");
        StringAssert.StartsWith(door.Text, "Widen its link ×2");
        var gold = company.Gold;

        Assert.IsTrue(game.World.ClimbPipe(company.Id, moily.Id).Success);
        Assert.AreEqual(gold - door.Gold - door.LevelCost, company.Gold, 1);
        game.Pass(1);

        Assert.AreEqual(2, PipeTile(game, company).Widen);
        Assert.AreEqual(1_000, Carried(game, moily), 1, "it carries twice as much");
        var next = game.World.PipeDoorFor(company, moily)!;
        Assert.AreEqual(PipeDoorKind.Widen, next.Kind);
        Assert.AreEqual(4, next.Tier);
        Assert.AreEqual(120 * game.World.MeasuredIncome(company), next.Gold, 1, "×4: two more hours");

        Assert.IsTrue(game.World.ClimbPipe(company.Id, moily.Id).Success);
        game.Pass(1);
        Assert.AreEqual(2_000, Carried(game, moily), 1);
        var steel = game.World.PipeDoorFor(company, moily)!;
        Assert.AreEqual(PipeDoorKind.Locked, steel.Kind, "×4 is the cap: past it, the next tier, which comes with Era 2");
        StringAssert.Contains(steel.Text, "with Era 2");
        StringAssert.Contains(game.World.WidenPipes(company.Id, next.Tiles, 4).Error, "already");
    }

    [TestMethod]
    public void WidenGoesStraightToFourWhenOnlyThatCarriesTheNextLevels()
    {
        // Run 4's e2e: a level-24 moily made 537 a minute on a 500 Copper Pipe; level 25 doubles it, so ×2 freed 37 a
        // minute and carried no level, ×4 carries the next ones
        var (game, company, moily, _) = FullPipe(2, 23);
        Assert.AreEqual(IdleCause.PipeFull, game.World.IdleReasonOf(moily).Cause);

        var door = game.World.PipeDoorFor(company, moily)!;

        Assert.AreEqual(PipeDoorKind.Widen, door.Kind);
        Assert.AreEqual(4, door.Tier, door.Text);
        Assert.IsGreaterThan(moily.Level, door.ToLevel, "the levels ×4 carries");
        Assert.AreEqual(180 * game.World.MeasuredIncome(company), door.Gold, 1, "three hours of income");
    }

    [TestMethod]
    public void WidenIsPaidForEachLinkItWidens()
    {
        // After run 4: one price widened any number of links
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 100_000_000, level: 40);
        company.Researched.Add("copperPipes");
        company.MeasuredIncome = 1_000;
        var map = game.Map(company, game.Local(company, 1, 1), "T+W+T", pipeTier: 2);
        game.World.UpgradeMany(company.Id, [map[(2, 0)].Id], 60);
        game.World.UpgradeMany(company.Id, [map[(0, 0)].Id, map[(4, 0)].Id], 9);
        game.Pass(2);

        var door = game.World.PipeDoorFor(company, map[(2, 0)])!;

        Assert.AreEqual(PipeDoorKind.Widen, door.Kind);
        StringAssert.StartsWith(door.Text, "Widen its 2 links ×2");
        Assert.AreEqual(2 * 60 * game.World.MeasuredIncome(company), door.Gold, 1, "an hour of income for each link");
    }

    [TestMethod]
    public void AFullPipeShowsWhenAnotherTakerHasAllItNeeds()
    {
        // The e2e's starter kit: the Moily feeds an Iron Mine on the same pipe (all it needs) and a post (its pipe full)
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 100_000_000, level: 40);
        company.Researched.Add("copperPipes");
        var map = game.Map(company, game.Local(company, 1, 1), "W+T\n M", pipeTier: 1);
        game.World.UpgradeMany(company.Id, [map[(0, 0)].Id], 14);
        game.Pass(2);

        Assert.AreEqual(IdleCause.PipeFull, game.World.IdleReasonOf(map[(0, 0)]).Cause);
        Assert.AreEqual(IdleFixKind.Climb, game.World.IdleStatusOf(map[(0, 0)]).Fix?.Kind);
        Assert.AreEqual(PipeDoorKind.Tier, game.World.PipeDoorFor(company, map[(0, 0)])!.Kind);
    }

    [TestMethod]
    public void AFullPipeShowsWhileTheYardTakesSomeToo()
    {
        // The e2e again: the station's Foundation bought a little of the Moily's energy at its gate, and the full pipe
        // to the post went unsaid
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var company = game.NewCompany(gold: 100_000_000, level: 40);
        company.Researched.Add("copperPipes");
        var map = game.Map(company, game.Local(company, 1, 1), "W+T", pipeTier: 1);
        game.ConnectToGate(company, map[(0, 0)]);
        game.World.UpgradeMany(company.Id, [map[(0, 0)].Id], 14);
        for (var i = 0; i < 5; i++)
        {
            game.Pass(1);
        }
        var network = game.World.NetworkOf(map[(0, 0)].Id)!;
        Assert.IsTrue(network.GateFlows.Any(t => !t.Import && t.Building == map[(0, 0)].Id && t.Sent > 1e-9), "the Yard takes some");

        Assert.AreEqual(IdleCause.PipeFull, game.World.IdleReasonOf(map[(0, 0)]).Cause);
    }

    [TestMethod]
    public void GreyPipesDontWiden()
    {
        var (game, company, moily, _) = FullPipe(1, 14);

        var door = game.World.PipeDoorFor(company, moily)!;

        Assert.AreEqual(PipeDoorKind.Tier, door.Kind, "copper is the first door past 100 a minute");
        Assert.AreEqual(2, door.Tier);
        StringAssert.Contains(game.World.WidenPipes(company.Id, door.Tiles, 2).Error, "Only Copper and Steel");
    }

    [TestMethod]
    public void ANewTierStartsUnwidened()
    {
        var (game, company, moily, _) = FullPipe(2, 60);
        var tiles = game.World.PipeDoorFor(company, moily)!.Tiles;
        game.World.WidenPipes(company.Id, tiles, 2);
        game.World.Era = 2;
        company.Researched.Add("steelPipes");
        var storage = game.Build(company, "storage");
        storage.Contents["steel"] = 100;

        Assert.IsTrue(game.World.UpgradePipes(company.Id, tiles, 3).Success);

        Assert.AreEqual(3, PipeTile(game, company).Tier);
        Assert.AreEqual(1, PipeTile(game, company).Widen, "×4 a tier: steel starts at ×1");
    }
}
