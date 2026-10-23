using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>
/// The pipe door (after run 4: the pipe tier is the game's real level cap; the panel said "+0 g/min: its pipes carry 205
/// iron ore a minute" with no button, and all 1,087 pipe tiles stayed grey for 36 days): Copper Pipes for everyone at
/// Company Level 8 and Steel Pipes with Era 2, and on a full pipe one button that lays the better pipe, then buys the
/// levels it carries
/// </summary>
[TestClass]
public class PipeDoorFacts
{
    [TestMethod]
    public void CopperPipesComeAtCompanyLevelEightAndSteelPipesWithEraTwo()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 7);
        Assert.IsFalse(game.World.HasResearch(company, "copperPipes"));

        game.World.GainXp(company, game.World.Content.Progression.XpForLevel(8) - company.Xp + 1, "test", "level");

        Assert.AreEqual(8, company.Level);
        Assert.IsTrue(game.World.HasResearch(company, "copperPipes"), "a level-up brings them");
        Assert.IsTrue(game.World.TakeChanges().Events.Any(t => t.Company == company.Id && t.Text.StartsWith("Copper Pipes are yours at Company Level 8")));
        Assert.IsFalse(game.World.HasResearch(company, "steelPipes"), "Steel Pipes come with Era 2");

        game.World.AdvanceEra();

        Assert.IsTrue(game.World.HasResearch(company, "steelPipes"));
        var newcomer = game.NewCompany("Newcomer");
        Assert.IsFalse(game.World.HasResearch(newcomer, "steelPipes"), "never before Copper Pipes");
    }

    /// <summary>A Wind Moily on a grey pipe to a trading post that sells all it gets: the pipe is the limit</summary>
    private static (TestGame Game, Company Company, Building Moily) GreyMoily(double ingots)
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 10_000_000, level: 12);
        var map = game.Map(company, game.Local(company, 1, 1), "W+++T", pipeTier: 1);
        game.World.UpgradeMany(company.Id, [map[(0, 0)].Id], 14);
        game.World.UpgradeMany(company.Id, [map[(4, 0)].Id], 9);
        if (ingots > 0)
        {
            game.Build(company, "storage").Contents["copperIngot"] = ingots;
        }
        game.Pass(2);
        Assert.AreEqual(IdleCause.PipeFull, game.World.IdleReasonOf(map[(0, 0)]).Cause);
        return (game, company, map[(0, 0)]);
    }

    [TestMethod]
    public void TheDoorLaysTheBetterPipeThenTheLevelsItCarries()
    {
        var (game, company, moily) = GreyMoily(ingots: 100);
        var door = game.World.PipeDoorFor(company, moily)!;
        Assert.AreEqual(PipeDoorKind.Tier, door.Kind);
        Assert.AreEqual(2, door.Tier);
        Assert.IsEmpty(door.Short, "the Storage holds the ingots");
        Assert.IsGreaterThan(moily.Level, door.ToLevel, "levels the copper carries");
        StringAssert.StartsWith(door.Text, $"Copper Pipe on its link (3 tiles), then Level {door.ToLevel}");
        Assert.IsGreaterThan(10, door.Gain);
        var gold = company.Gold;
        var before = game.World.GoldPerMinute()[company.Id];

        Assert.IsTrue(game.World.ClimbPipe(company.Id, moily.Id).Success);
        Assert.AreEqual(gold - door.Gold - door.LevelCost, company.Gold, 1, "what the button said");
        game.Pass(2);

        Assert.IsTrue(game.World.Conductors.Where(t => t.Owner == company.Id && t.Kind == ConductorKind.Pipe).All(t => t.Tier == 2));
        Assert.AreEqual(door.ToLevel, moily.Level);
        Assert.AreEqual(before + door.Gain, game.World.GoldPerMinute()[company.Id], Math.Max(1, door.Gain * 0.05), "it brings what it said");
    }

    [TestMethod]
    public void APipeWaitsForAFriendsIngotsAndOffersAnOrder()
    {
        var (game, company, moily) = GreyMoily(ingots: 0);
        var cleo = game.NewCompany("Cleo", level: 12);
        game.Build(cleo, "smelter");
        var door = game.World.PipeDoorFor(company, moily)!;
        Assert.AreEqual("Cleo makes copper ingots: the Yard brings them", door.Source);
        Assert.AreEqual(0, door.ToLevel, "the levels wait for the pipe");

        Assert.IsTrue(game.World.ClimbPipe(company.Id, moily.Id).Success);

        var want = company.Asks.Single(t => t.PipeTiles != null);
        Assert.IsNull(want.Order, "a friend makes them: no order unless asked");
        var waits = game.World.PipeDoorFor(company, moily)!;
        Assert.AreEqual(PipeDoorKind.Waits, waits.Kind);
        StringAssert.StartsWith(waits.Text, "Its Copper Pipe waits for 3 copper ingots");
        Assert.IsNotNull(waits.OrderCost);
        Assert.IsTrue(game.World.ClimbPipe(company.Id, moily.Id).Success, "the door orders them now");
        Assert.IsNotNull(company.Asks.Single(t => t.PipeTiles != null).Order);
    }
}
