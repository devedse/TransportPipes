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
}
