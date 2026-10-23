using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>
/// Goods that stood a stage, named at both ends (after run 4: Mia-B's wood was the crew's Water Tower's keystone and
/// Ben's sand filled a fifth of it, and no card said so; only a ticked Send was ever announced)
/// </summary>
[TestClass]
public class StoodGiftFacts
{
    [TestMethod]
    public void BothEndsHearWhoseGoodsStoodTheStageAndMayThankForThem()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var ana = game.NewCompany("Ana", level: 5);
        var ben = game.NewCompany("Ben", level: 5);
        game.Pass(1);
        var station = game.EstateOf(ana).Station;
        var project = game.World.ProjectOf(station);
        project.Basket = new Dictionary<string, double> { ["stone"] = 100, ["sand"] = 100 };
        project.StageGifts = new Dictionary<long, Dictionary<string, double>>
        {
            [ana.Id] = new() { ["stone"] = 100 },
            [ben.Id] = new() { ["sand"] = 100 },
        };
        game.World.TakeChanges();

        game.World.FinishStageNow(station);

        var events = game.World.TakeChanges().Events;
        Assert.IsTrue(events.Any(t => t.Company == ben.Id && t.Text.StartsWith("Your sand built a third of") && t.InBell), string.Join(" | ", events.Select(t => t.Text)));
        Assert.IsTrue(events.Any(t => t.Company == ben.Id && t.Text.Contains("(Ana's stone in it 💛)")), "the station's news names the other's goods");
        var stood = ben.Report.StageGifts.Single(t => t.Giver == ana.Id);
        Assert.AreEqual("stone", stood.Good);
        Assert.AreEqual(1.5 / 2.7, stood.Share, 1e-6);
        Assert.IsTrue(ben.Report.StageGifts.Any(t => t.Giver == ben.Id && t.Good == "sand"), "and his own");

        Assert.IsTrue(game.World.Thank(ben.Id, ana.Id, "stone").Success, "a gift that stood a stage may be thanked");
        Assert.IsTrue(ana.Report.Thanks.Single().Contains("thanked you for your stone"));
    }
}
