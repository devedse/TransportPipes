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

    [TestMethod]
    public void ChapterTwoFollowsTheFirstSession()
    {
        var game = new TestGame(content: TestGame.PlainContent, tutorial: true);
        var tutorial = game.World.Content.Tutorial;
        var firstOfTwo = tutorial.First(t => t.Chapter == 2);

        Assert.AreEqual(11, tutorial.Count(t => t.Chapter == 1), "chapter 1 is as it was");
        Assert.AreEqual(8, tutorial.Count(t => t.Chapter == 2));
        Assert.AreEqual((2, 1, 8), game.World.TutorialChapter(firstOfTwo));
        Assert.IsTrue(tutorial.SkipWhile(t => t.Chapter == 1).All(t => t.Chapter == 2), "chapter 2 comes after chapter 1");
    }

    [TestMethod]
    public void AChainStepWaitsForTheLevelOfTheNextBuildingItNeeds()
    {
        var game = new TestGame(content: TestGame.PlainContent, tutorial: true, surveys: true);
        var company = game.NewCompany(level: 4);
        var copper = game.World.Content.Tutorial.Single(t => t.Id == "copperIngots");
        CollectionAssert.AreEqual(new[] { "copperMine", "coalPit", "smelter" }, copper.HintBuildings.ToArray());

        Assert.AreEqual(5, game.World.TutorialWaitLevel(company, copper), "the Copper Mine comes at Level 5");
        company.Level = 5;
        Assert.IsNull(game.World.TutorialWaitLevel(company, copper));
        game.Build(company, "copperMine");
        game.Build(company, "coalPit");
        Assert.AreEqual(6, game.World.TutorialWaitLevel(company, copper), "then the Smelter, at Level 6");

        var corporation = game.World.Content.Tutorial.Single(t => t.Id == "corporation");
        Assert.AreEqual(GameWorld.CorporationFoundLevel, game.World.TutorialWaitLevel(company, corporation));
    }

    [TestMethod]
    public void AReturningPlayerFindsChapterTwoWhereItIs()
    {
        var game = new TestGame(content: TestGame.PlainContent, tutorial: true);
        // Done with chapter 1 before chapter 2 existed, and further along since
        var company = game.NewCompany(level: 9);
        company.TutorialStep = game.World.Content.Tutorial.Count(t => t.Chapter == 1);
        var moily = game.Build(company, "windMoily");
        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 9).Success);
        company.Researched.UnionWith(["copperPipes", "valves", "betterBearings"]);
        var gold = company.Gold;

        game.Pass(1);

        var step = game.World.CurrentTutorialStep(company)!;
        Assert.AreEqual("copperIngots", step.Id, "copper first: it isn't done");
        company.Stats["produced:copperIngot"] = 5;
        game.Pass(1);
        Assert.AreEqual("moreOrders", game.World.CurrentTutorialStep(company)!.Id, "the milestone and the researches were done already");
        Assert.IsGreaterThan(gold, company.Gold, "with their thank-yous");
    }

    [TestMethod]
    public void NeighbourIncomeIsWhatNeighboursPayNow()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var seller = game.NewCompany("Seller", level: 3);
        var buyer = game.NewCompany("Buyer", level: 3);
        var origin = game.Local(seller, 2, 2);
        game.Map(seller, origin, "W+");
        game.Map(buyer, origin, "  +M+T");

        Assert.IsGreaterThan(0, game.World.StatValue(seller, "neighbourIncome"), "the mine buys the moily's energy");
        Assert.AreEqual(0, game.World.StatValue(buyer, "neighbourIncome"));
    }
}
