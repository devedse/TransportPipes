using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>Why a building doesn't run (F2): one truth, decided from its pipes for "stuck"</summary>
[TestClass]
public class IdleFacts
{
    private static (TestGame Game, Company Company, Dictionary<(int X, int Y), Building> Map) Setup(params string[] rows)
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 10);
        var map = game.Map(company, game.Local(company, 1, 1), string.Join("\n", rows));
        game.Pass(1);
        return (game, company, map);
    }

    [TestMethod]
    public void AFactoryWhoseIngotsHaveNoTakerIsStuck()
    {
        // Ana's day 4: the factory piped to the mine and a windmill, the mine to the trading post. Goods never pass
        // through a building, so the ingots can't leave.
        var (game, _, map) = Setup("T+M+F+W");
        var factory = map[(4, 0)];

        var reason = game.World.IdleReasonOf(factory);

        Assert.AreEqual(IdleCause.Stuck, reason.Cause);
        Assert.AreEqual("ironIngot", reason.Resource);
        Assert.IsTrue(reason.IsStuck);
        Assert.AreEqual(IdleCause.None, game.World.IdleReasonOf(map[(0, 0)]).Cause, "the trading post is fine");
    }

    [TestMethod]
    public void AMoilyWhoseOnlyTakerIsStuckSaysWho()
    {
        // Dev's day 10: the moily feeds only the dredger, whose sand has nowhere to go
        var (game, _, map) = Setup("W+F");
        var moily = map[(0, 0)];
        var factory = map[(2, 0)];

        var reason = game.World.IdleReasonOf(moily);

        Assert.AreEqual(IdleCause.TakerStuck, reason.Cause);
        Assert.AreEqual(factory.Id, reason.Other);
        Assert.AreEqual(IdleCause.Stuck, game.World.IdleReasonOf(factory).Cause);
    }

    [TestMethod]
    public void AFullStorageIsFullNotStuck()
    {
        var (game, _, map) = Setup("W+O");
        var moily = map[(0, 0)];
        var storage = map[(2, 0)];
        Assert.AreEqual(IdleCause.None, game.World.IdleReasonOf(moily).Cause, "an empty Storage takes it all");

        storage.Contents["energy"] = game.World.StorageCapacity(storage, game.World.Content.Building("storage"));
        game.World.MarkCompanyNetworksDirty(storage.Owner);
        game.Pass(1);

        var reason = game.World.IdleReasonOf(moily);
        Assert.AreEqual(IdleCause.Full, reason.Cause, "slowed, not stuck: it has a way out, and that is full");
        Assert.IsFalse(reason.IsStuck);
        Assert.AreEqual(IdleCause.Full, game.World.IdleReasonOf(storage).Cause);
    }

    [TestMethod]
    public void StuckNeverDependsOnThisRoundsFlows()
    {
        var (game, _, map) = Setup("T+M+F+W", "", "W+T");
        var factory = map[(4, 0)];
        var moily = map[(0, 2)];
        var network = game.World.NetworkOf(factory.Id)!;
        var before = (game.World.IdleReasonOf(factory), game.World.IdleReasonOf(moily));

        // The same pipes with nothing flowing at all
        network.Solution = FlowSolution.Empty;
        game.World.NetworkOf(moily.Id)!.Solution = FlowSolution.Empty;

        Assert.AreEqual(before.Item1, game.World.IdleReasonOf(factory));
        Assert.AreEqual(before.Item2.IsStuck, game.World.IdleReasonOf(moily).IsStuck);
        Assert.IsFalse(game.World.IdleReasonOf(moily).IsStuck);
    }

    [TestMethod]
    public void AMoilyFeedingAFactoryWithoutOreSaysWhoIsIdle()
    {
        // The factory has a way out (the post) but no iron ore: the moily's energy waits for it
        var (game, _, map) = Setup("W+F+T");
        var moily = map[(0, 0)];
        var factory = map[(2, 0)];

        Assert.AreEqual(new IdleReason(IdleCause.InputMissing, "ironOre"), game.World.IdleReasonOf(factory));
        var reason = game.World.IdleReasonOf(moily);
        Assert.AreEqual(IdleCause.TakerIdle, reason.Cause);
        Assert.AreEqual(factory.Id, reason.Other);
    }

    [TestMethod]
    public void AMoilyWhoseOnlyTakerRunsFullySaysItSellsNothing()
    {
        // Ana's run 2 evening: a factory set into her windmills' pipe runs fully on what it can use; the rest of the
        // energy can't pass through it to the trading post, so the moily runs slowly, and the factory isn't "idle"
        var (game, _, map) = Setup("W+F+T", "  +", "W+M");
        var moily = map[(0, 0)];
        var factory = map[(2, 0)];
        var rate = game.World.NetworkOf(moily.Id)!.Solution.Rate(moily.Id);

        Assert.AreEqual(IdleCause.None, game.World.IdleReasonOf(factory).Cause, "the factory runs fully");
        Assert.IsTrue(rate < 0.99, $"the moily runs at {rate:P0}");
        var reason = game.World.IdleReasonOf(moily);
        Assert.AreEqual(IdleCause.TakersFull, reason.Cause);
        Assert.AreEqual(factory.Id, reason.Other);
        var status = game.World.IdleStatusOf(moily);
        Assert.AreEqual("Slowed: only the Small Factory takes its energy", status.Pill);
        Assert.AreEqual(IdleFixKind.LayPipe, status.Fix?.Kind, "one pipe to the trading post sells the rest");
    }

    [TestMethod]
    public void AFactoryWhoseMineStandsStillIsShortOfOre()
    {
        // Cleo's run 2 afternoon: a factory set into the line between her mine and her windmills; the mine's only pipe
        // reaches the factory, so it has no energy, and the factory gets no ore. Neither input comes, but ore is the cause.
        var (game, _, map) = Setup("  M", "  +", "W+F+T", "+++++");
        var factory = map[(2, 2)];
        var mine = map[(2, 0)];

        Assert.AreEqual(new IdleReason(IdleCause.InputMissing, "energy"), game.World.IdleReasonOf(mine));
        Assert.AreEqual(IdleCause.None, game.World.IdleReasonOf(map[(0, 2)]).Cause, "the moily sells to the trading post");
        Assert.AreEqual(new IdleReason(IdleCause.InputShort, "ironOre"), game.World.IdleReasonOf(factory));
    }

    [TestMethod]
    public void AMineWhosePipeCarriesLessThanItMakesSaysThePipeIsTheLimit()
    {
        // Run 3, Cleo's level-25 Coal Pit: it could make 882 coal a minute, its grey pipe to the trading post carried 100,
        // and its card said "energy short" for eleven days (the solver gives it only the energy it can use)
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 10);
        var map = game.Map(company, game.Local(company, 1, 1), string.Join("\n", "W+M+T", "  +", "W+W"), pipeTier: 1);
        game.Pass(1);
        var mine = map[(2, 0)];
        company.Gold = 1e12;
        company.Level = 60;
        game.UpgradeNow(company, [mine.Id], 24);
        game.UpgradeNow(company, [map[(0, 0)].Id, map[(0, 2)].Id, map[(2, 2)].Id], 15);
        game.Pass(1);
        var rate = game.World.NetworkOf(mine.Id)!.Solution.Rate(mine.Id);
        Assert.IsTrue(rate < 0.9, $"it runs at {rate:P0}");

        Assert.AreEqual(new IdleReason(IdleCause.PipeFull, "ironOre"), game.World.IdleReasonOf(mine), "the pipe, not its energy");
        var status = game.World.IdleStatusOf(mine);
        Assert.AreEqual("Slowed: its pipes carry 100 iron ore a minute", status.Pill);
        Assert.AreEqual(IdleFixKind.Climb, status.Fix?.Kind, "its pipe door: Copper Pipes from Company Level 8");
        StringAssert.StartsWith(status.Fix!.Label, "Copper Pipe on its link");
    }

    [TestMethod]
    public void BeforeCopperPipesASecondTakerOnAPipeOfItsOwnCarriesMore()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 6);
        var map = game.Map(company, game.Local(company, 1, 1), "W+T", pipeTier: 1);
        company.Gold = 1e9;
        game.UpgradeNow(company, [map[(0, 0)].Id], 14);
        game.Pass(2);

        var status = game.World.IdleStatusOf(map[(0, 0)]);

        Assert.AreEqual(IdleCause.PipeFull, status.Reason.Cause);
        Assert.AreEqual(IdleFixKind.Build, status.Fix?.Kind);
        Assert.AreEqual("tradingPost", status.Fix?.BuildingType, "a second taker on a pipe of its own carries as much again");
        StringAssert.Contains(game.World.PipeDoorFor(company, map[(0, 0)])!.Text, "yours at Company Level 8 (you're 6)");
    }

    [TestMethod]
    public void AFixThatLeavesItIdleSaysWhatItWillStillNeed()
    {
        // Run 3, Mia's day 8: the "Unstick" pipe took her Water Pump to the trading post, and then it had no energy for
        // four weeks, and nothing said so (mia d08 #96, metrics idle "InputMissing energy")
        var (game, _, map) = Setup("M   T");
        var fix = game.World.IdleStatusOf(map[(0, 0)]).Fix;

        Assert.AreEqual(IdleFixKind.LayPipe, fix?.Kind);
        StringAssert.EndsWith(fix!.Label, "· then it needs energy");
    }

    [TestMethod]
    public void AFixPipeGoesWhereItsInputsAreToo()
    {
        // A Storage with a moily on its pipes, and a trading post with nothing: the pipe goes where the energy is, though
        // a trading post comes first otherwise
        var (game, _, map) = Setup("O+W", "", "", "M", "", "", "T");
        var fix = game.World.IdleStatusOf(map[(0, 3)]).Fix;

        Assert.AreEqual(IdleFixKind.LayPipe, fix?.Kind);
        StringAssert.StartsWith(fix!.Label, "Lay the pipe to your Storage");
        Assert.IsFalse(fix.Label.Contains("then it needs"), fix.Label);
    }

    [TestMethod]
    public void AStuckMoilyPipesToItsOwnShortMineFirst()
    {
        // Cleo's run 2 evening: her new Wind Moily was stuck while her own Iron Mine had no energy; the one fix offered to
        // send to a neighbour. Your own short building comes first.
        var (game, _, map) = Setup("W", "", "", "M+T");
        var moily = map[(0, 0)];
        var mine = map[(0, 3)];
        Assert.AreEqual(new IdleReason(IdleCause.InputMissing, "energy"), game.World.IdleReasonOf(mine));
        Assert.IsTrue(game.World.IdleReasonOf(moily).IsStuck);

        var status = game.World.IdleStatusOf(moily);

        Assert.IsTrue(status.Fix is { Kind: IdleFixKind.LayPipe, OwnNeed: true }, status.Fix?.Label);
        StringAssert.StartsWith(status.Fix!.Label, "Lay the pipe to your Iron Mine");
        Assert.AreEqual(mine.Id, status.Fix.Pipe!.Target!.Id);
    }

    [TestMethod]
    public void AFactorySetOnTheLineKeepsThePipeRunningUnderIt()
    {
        // Run 2: Ana and Cleo removed a pipe tile to set a factory into the line, which split it. Now a building may stand
        // on its owner's pipe, and the pipe runs on under it.
        var (game, company, map) = Setup("W+++T");
        var moily = map[(0, 0)];
        var post = map[(4, 0)];
        var spot = game.Local(company, 1 + 2, 1);
        company.Gold = 1_000_000;
        game.EstateOf(company).UnlockedParcels = -1;

        var result = game.World.PlaceBuilding(company.Id, "smallFactory", spot);

        Assert.IsTrue(result.Success, result.Error);
        game.Pass(5);
        var factory = game.World.BuildingsOf(company.Id).Single(t => t.Type == "smallFactory");
        Assert.IsTrue(game.World.ConductorAt(spot) != null, "the pipe stays under it");
        CollectionAssert.Contains(game.World.PipeReach(moily).Select(t => t.Id).ToList(), post.Id, "the moily still reaches the trading post");
        CollectionAssert.Contains(game.World.PipeReach(moily).Select(t => t.Id).ToList(), factory.Id, "and the factory on the line");
        Assert.AreEqual(IdleCause.None, game.World.IdleReasonOf(moily).Cause, "it sells what the factory doesn't take");
    }
}
