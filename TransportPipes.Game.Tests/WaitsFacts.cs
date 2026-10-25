using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>
/// Wants that say they wait, and a friend's want in sight (after run 4: Dev's Big Factory and Ana's Cement Kiln read
/// "is being built" and waited to the end for bricks nobody near made, while Dev had 12M gold and "no stone or coal")
/// </summary>
[TestClass]
public class WaitsFacts
{
    /// <summary>Ana in the Capital; Dev, her crew mate in her Yard</summary>
    private static (TestGame Game, Company Ana, Company Dev) Crew()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", gold: 1_000_000, level: 20);
        var dev = game.NewCompany("Dev", gold: 12_000_000, level: 20);
        Assert.AreEqual(ana.CrewId, dev.CrewId, "Pick for me settles Dev in Ana's Yard");
        game.Pass(1);
        return (game, ana, dev);
    }

    [TestMethod]
    public void AScaffoldSaysItWaitsAndWhereItsGoodsWouldComeFrom()
    {
        var (game, ana, _) = Crew();
        var hall = game.World.Content.Building("marketHall");
        StringAssert.StartsWith(game.World.MaterialsLine(ana, hall)!.Value.Text, "It waits for 500 bricks before it stands: nobody near makes bricks: buy them from the market (6,250 g)");

        var placed = game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));

        Assert.IsTrue(placed.Success, placed.Error);
        StringAssert.StartsWith(placed.Note, "Your Market Hall stands as a scaffold. It waits for 500 bricks");
        var storage = game.Build(ana, "storage");
        storage.Contents["bricks"] = 600;
        Assert.AreEqual("It also takes 500 bricks from your Storages.", game.World.MaterialsLine(ana, hall)!.Value.Text);
    }

    [TestMethod]
    public void ACrewMateWhoMakesNoneOfItCanOrderIt()
    {
        var (game, ana, dev) = Crew();
        game.World.TakeChanges();
        game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));

        var events = game.World.TakeChanges().Events;
        Assert.IsTrue(events.Any(t => t.Company == dev.Id && t.Text.Contains("you can buy them for Ana in the Yard")), string.Join(" | ", events.Select(t => t.Text)));
        var near = game.World.WantsNear(dev).Single();
        Assert.IsFalse(near.Makes);
        Assert.AreEqual(0, near.Holds);
        var result = game.World.OrderRest(dev.Id, near.Target);
        Assert.IsTrue(result.Success, result.Error);
        StringAssert.StartsWith(result.Note, "💛 Bought for Ana's Market Hall");
    }

    [TestMethod]
    public void TheFriendWithGoldIsOfferedTheBuildingThatMakesWhatAFriendWaitsFor()
    {
        var (game, ana, dev) = Crew();
        // The town's Wonder waits for goods nobody makes too: here only the friend's building does
        game.World.WonderOf(ana.District)!.FullAt = game.Time;
        game.World.PlaceBuilding(ana.Id, "marketHall", game.Spot(ana, "marketHall"));

        var step = game.World.NextSteps(dev, 8).FirstOrDefault(t => t.Title.StartsWith("Make the bricks"));

        Assert.IsNotNull(step, string.Join(" | ", game.World.NextSteps(dev, 8).Select(t => t.Title)));
        Assert.AreEqual("brickworks", step.BuildingType);
        StringAssert.StartsWith(step.Reason, "Nobody near makes bricks, and Ana's Market Hall (500) waits for them. A Brickworks (");
    }
}
