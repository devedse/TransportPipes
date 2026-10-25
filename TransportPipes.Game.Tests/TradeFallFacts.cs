using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>
/// A running gift stays running, and both ends hear when it falls (after run 3, the analysts' row 9: Cleo's stone from
/// Ana fell from 55.8 to 0.8 a minute on day 22, and neither was told)
/// </summary>
[TestClass]
public class TradeFallFacts
{
    private static void Minutes(TestGame game, int minutes)
    {
        for (var i = 0; i < minutes; i++)
        {
            game.Pass(1);
        }
    }

    [TestMethod]
    public void BothEndsHearWhenARunningTradeFallsWithItsCauseAndItEndsWhenItRecovers()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var a = game.NewCompany("Ana");
        var b = game.NewCompany("Ben", level: 2);
        foreach (var _ in Enumerable.Range(0, 2))
        {
            var moily = game.Build(a, "windMoily");
            game.ConnectToGate(a, moily);
        }
        var mine = game.Build(b, "ironMine", game.Spot(b, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        var toGate = game.ConnectToGate(b, mine);
        var toPost = game.Connect(b, mine, game.TradingPost(b));
        Minutes(game, 60);
        Assert.IsGreaterThanOrEqualTo(3, game.World.UsualRate(a, b, "energy"), "Ana's energy runs Ben's mine");
        game.World.TakeChanges();

        // Ben's mine loses its way to the post: its ore can't leave, it takes no energy
        Assert.IsTrue(game.World.RemovePipes(b.Id, [toPost.Last(t => !toGate.Contains(t) && game.World.ConductorAt(t)?.Kind == ConductorKind.Pipe)]).Success);
        Minutes(game, 31);

        var fall = a.TradeFalls.GetValueOrDefault($"{b.Id}:energy");
        Assert.IsNotNull(fall, "a fall is recorded");
        Assert.AreEqual(mine.Id, fall.Building, "its cause is Ben's mine");
        var events = game.World.TakeChanges().Events;
        Assert.IsTrue(events.Any(t => t.Company == a.Id && t.Text.StartsWith("Your energy to Ben fell from") && t.Text.Contains("Ben's Iron Mine is idle (its iron ore reaches no trading post)")), string.Join(" | ", events.Select(t => t.Text)));
        Assert.IsTrue(events.Any(t => t.Company == b.Id && t.Text.StartsWith("Ana's energy to you fell from") && t.Text.Contains("your Iron Mine is idle") && t.Building == mine.Id), string.Join(" | ", events.Select(t => t.Text)));
        Assert.IsTrue(events.Where(t => t.Text.Contains("fell from")).All(t => t.InBell), "both can act on it");
        Assert.HasCount(1, game.World.FallsFor(b, game.World.Now.AddHours(-1)), "Ben's card tells it too");

        // Its pipe back: the trade runs again, and the fall is over
        game.Connect(b, mine, game.TradingPost(b));
        Minutes(game, 31);
        Assert.IsEmpty(a.TradeFalls);
    }

    [TestMethod]
    public void TheCardsBooksAreExactlyWhatWasPaidSinceTheLastSession()
    {
        // Run 3: Ana's card said +86.5k g against about 14k paid, counting whole days of the books
        var game = new TestGame(content: TestGame.PlainContent);
        var a = game.NewCompany("Ana");
        var b = game.NewCompany("Ben", level: 2);
        game.ConnectToGate(a, game.Build(a, "windMoily"));
        var mine = game.Build(b, "ironMine", game.Spot(b, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(b, mine);
        game.Connect(b, mine, game.TradingPost(b));
        Minutes(game, 30);
        a.Report = new CompanyReport { Since = game.World.Now };
        b.Report = new CompanyReport { Since = game.World.Now };
        var gold = a.Gold;
        Minutes(game, 30);

        var units = a.Report.SoldToUnits.GetValueOrDefault($"{b.Id}:energy");
        Assert.IsGreaterThan(1, units);
        Assert.AreEqual(units, b.Report.BoughtFromUnits.GetValueOrDefault($"{a.Id}:energy"), 1e-6);
        Assert.AreEqual(a.Gold - gold, a.Report.SoldToGold.GetValueOrDefault($"{b.Id}:energy"), 1, "what Ana was paid, no more");
    }
}
