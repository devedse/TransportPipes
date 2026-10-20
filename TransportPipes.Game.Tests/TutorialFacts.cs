using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class TutorialFacts
{
    [TestMethod]
    public void LoadyGuidesStepByStepAndPaysALittleForEachStep()
    {
        var game = new TestGame(content: TestGame.PlainContent, tutorial: true);
        var company = game.NewCompany(gold: 2000);
        var first = game.World.CurrentTutorialStep(company)!;
        Assert.AreEqual("firstPanel", first.Id);
        Assert.AreEqual("solarPanel", first.Hint["building"]);

        game.Pass(1);
        Assert.AreEqual(0, company.TutorialStep, "nothing done yet");

        var cost = game.World.BuildCost(company, game.World.Content.Building("solarPanel"));
        game.Build(company, "solarPanel", finish: false);
        game.Pass(1);
        Assert.AreEqual("firstSale", game.World.CurrentTutorialStep(company)!.Id);
        Assert.AreEqual(2000 - cost + first.Gold, company.Gold, 1e-6, "the thank-you");
        Assert.IsTrue(game.World.TakeChanges().Events.Any(t => t.Text.StartsWith("Loady")));
    }

    [TestMethod]
    public void ExperiencedPlayersSkipTheGuide()
    {
        var game = new TestGame(content: TestGame.PlainContent, tutorial: true);
        var veteran = game.NewCompany(level: 12);
        var newcomer = game.NewCompany("New");

        game.Pass(1);

        Assert.IsNull(game.World.CurrentTutorialStep(veteran));
        Assert.IsNotNull(game.World.CurrentTutorialStep(newcomer));
        Assert.IsTrue(game.World.SkipTutorial(newcomer.Id).Success);
        Assert.IsNull(game.World.CurrentTutorialStep(newcomer));
    }
}
