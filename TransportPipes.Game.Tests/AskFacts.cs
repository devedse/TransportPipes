using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class AskFacts
{
    /// <summary>A (slot 0) has Wind Moilies piped to its Yard gate; B (slot 1, the same station) has a storage</summary>
    private static (TestGame Game, Company A, Company B, Building Storage) Neighbours()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var a = game.NewCompany("A");
        var b = game.NewCompany("B", level: 5);
        foreach (var _ in Enumerable.Range(0, 2))
        {
            game.ConnectToGate(a, game.Build(a, "windMoily"));
        }
        var storage = game.Build(b, "storage");
        game.ConnectToGate(b, storage);
        game.Pass(3);
        return (game, a, b, storage);
    }

    [TestMethod]
    public void NeighboursFillAnAskIntoTheAskersStorageAtThePlayerPrice()
    {
        var (game, a, b, storage) = Neighbours();
        var goldA = a.Gold;

        Assert.IsTrue(game.World.Ask(b.Id, "energy", 300, 24, renew: false).Success);
        Assert.IsTrue(game.World.Changes.Events.Any(t => t.Company == a.Id && t.Text.Contains("asks the station")), "the neighbours hear about it");
        game.Pass(60);

        var ask = b.Asks.Single();
        Assert.IsGreaterThan(0, ask.Got, "A's spare energy goes to B's Ask");
        Assert.AreEqual(ask.Got, storage.Contents.GetValueOrDefault("energy"), 1e-6, "into B's storage");
        Assert.IsGreaterThan(goldA, a.Gold);
    }

    [TestMethod]
    public void AtTheDeadlineAHouseFillsTheRestSoAnAskAlwaysCloses()
    {
        var (game, _, b, storage) = Neighbours();
        var gold = b.Gold;
        Assert.IsTrue(game.World.Ask(b.Id, "coal", 200, 12, renew: true).Success, "nobody here makes coal");

        game.Pass(12 * 60 + 1);

        Assert.AreEqual(200, storage.Contents.GetValueOrDefault("coal"), 1e-6, "a House brought it all");
        Assert.AreEqual(gold - 200 * game.World.BasePrice("coal") * 2.0, b.Gold, 1e-6, "at 2.0 × base");
        Assert.IsTrue(game.World.Changes.Events.Any(t => t.Company == b.Id && t.Text.Contains("a House the other")));
        Assert.AreEqual(200, b.Asks.Single().Amount, "and it renewed itself");
        Assert.AreEqual(0, b.Asks.Single().Got);
    }

    [TestMethod]
    public void AsksHaveLimits()
    {
        var (game, _, b, _) = Neighbours();
        StringAssert.Contains(game.World.Ask(b.Id, "coal", 1e9, 24, false).Error, "room");
        Assert.IsTrue(game.World.Ask(b.Id, "coal", 10, 24, false).Success);
        StringAssert.Contains(game.World.Ask(b.Id, "coal", 10, 24, false).Error, "already");
        Assert.IsTrue(game.World.CancelAsk(b.Id, "coal").Success);
        Assert.IsEmpty(b.Asks);
    }

    [TestMethod]
    public void ThePlanBoardShowsWhatTheStationMakesAndNeeds()
    {
        var (game, a, b, _) = Neighbours();
        Assert.IsTrue(game.World.SetPin(a.Id, "energy", need: false, on: true).Success);
        Assert.IsTrue(game.World.SetPin(b.Id, "glass", need: true, on: true).Success);

        var (makes, needs) = game.World.PlanBoard(game.EstateOf(a).Station);

        CollectionAssert.AreEquivalent(new[] { "energy" }, makes.ToArray());
        CollectionAssert.AreEquivalent(new[] { "glass" }, needs.ToArray());
        Assert.IsTrue(game.World.SetPin(b.Id, "glass", need: true, on: false).Success);
        Assert.IsEmpty(game.World.PlanBoard(game.EstateOf(a).Station).Needs);
    }
}
