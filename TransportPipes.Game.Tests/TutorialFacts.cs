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
    public void TheStarterKitDoesntDoLoadysStepsForThePlayer()
    {
        // Found by the first-session run: the House's kit built the first panel, sold and built the Moily, so a newcomer
        // never placed or piped anything before the Iron Mine
        var game = new TestGame(tutorial: true);
        game.World.TenantsEnabled = true;
        game.World.OpenNextDistrict();
        var company = game.NewCompany(gold: 2000, slot: 3);
        Assert.IsTrue(company.KitBuildings.Count > 0, "a House left a kit");
        game.Pass(2);
        Assert.AreEqual("firstPanel", game.World.CurrentTutorialStep(company)!.Id, "the kit is not the player's panel");

        var pipes = game.World.Conductors.Select(t => t.Pos).ToHashSet();
        var panel = game.Build(company, "solarPanel", game.Spot(company, "solarPanel", t => !TestGame.Neighbours(t).Any(pipes.Contains)));
        game.Pass(1);
        Assert.AreEqual("firstSale", game.World.CurrentTutorialStep(company)!.Id, "although the kit sells already");

        game.Connect(company, panel, game.TradingPost(company));
        game.Pass(1);
        Assert.AreEqual("firstUpgrade", game.World.CurrentTutorialStep(company)!.Id);
    }

    [TestMethod]
    public void ChapterOneRetiresAtLevelTen()
    {
        var game = new TestGame(content: TestGame.PlainContent, tutorial: true);
        var veteran = game.NewCompany(level: 12);
        var newcomer = game.NewCompany("New");
        var climber = game.NewCompany("Climber");
        climber.TutorialStep = 4;

        game.Pass(1);
        Assert.AreEqual(1, game.World.CurrentTutorialStep(climber)!.Chapter);
        game.World.GainXp(climber, game.World.Content.Progression.XpForLevel(10) - climber.Xp, "test", "level");
        game.Pass(1);

        Assert.AreEqual(2, game.World.CurrentTutorialStep(veteran)!.Chapter, "a veteran starts with chapter 2");
        Assert.AreEqual(2, game.World.CurrentTutorialStep(climber)!.Chapter, "at Level 10 chapter 1 is over for everyone");
        Assert.IsTrue(game.World.TakeChanges().Events.Any(t => t.Company == climber.Id && t.Text.Contains("past my first chapter")));
        Assert.AreEqual(1, game.World.CurrentTutorialStep(newcomer)!.Chapter);
        Assert.IsTrue(game.World.SkipTutorial(newcomer.Id).Success);
        Assert.IsNull(game.World.CurrentTutorialStep(newcomer));
    }

    [TestMethod]
    public void AStarterKitCountsFromStepThree()
    {
        var game = new TestGame(tutorial: true);
        game.World.TenantsEnabled = true;
        game.World.OpenNextDistrict();
        var company = game.NewCompany(gold: 20_000, slot: 3);
        var pipes = game.World.Conductors.Select(t => t.Pos).ToHashSet();
        var panel = game.Build(company, "solarPanel", game.Spot(company, "solarPanel", t => !TestGame.Neighbours(t).Any(pipes.Contains)));
        game.Connect(company, panel, game.TradingPost(company));
        game.Pass(1);
        Assert.AreEqual("firstUpgrade", game.World.CurrentTutorialStep(company)!.Id, "steps 1 and 2 are the player's own");
        game.World.TakeChanges();

        Assert.IsTrue(game.World.Upgrade(company.Id, panel.Id, 1).Success);
        game.Pass(2);

        Assert.AreEqual("firstMine", game.World.CurrentTutorialStep(company)!.Id, "the kit's moilies and its gate pipe do steps 4 and 5");
        var loady = game.World.TakeChanges().Events.Where(t => t.Company == company.Id && t.Text.StartsWith("Loady")).Select(t => t.Text).ToList();
        // The upgrade and the two steps the kit did come in the same check: one thank-you for all three
        Assert.HasCount(1, loady, string.Join(" | ", loady));
        StringAssert.Contains(loady[0], "starter kit already did steps 3–5");
    }

    [TestMethod]
    public void AStepWaitingForAnIngotSaysWhy()
    {
        var game = new TestGame(content: TestGame.PlainContent, tutorial: true);
        var company = game.NewCompany(level: 3);
        company.TutorialStep = game.World.Content.Tutorial.ToList().FindIndex(t => t.Id == "firstIngot");
        game.Map(company, game.Local(company, 2, 2), "T+M+F+W\n  +\n  W");
        game.Pass(1);
        var step = game.World.CurrentTutorialStep(company)!;

        StringAssert.StartsWith(game.World.TutorialTip(company, step), "Built, but stuck: its iron ingots can't leave. Show me lays the pipe to your Trading Post");
        Assert.IsNotNull(game.World.TutorialPipe(company, step), "Show me lays it");
        StringAssert.Contains(step.Text, "Goods never pass through a building");
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
        var moily = game.Build(seller, "windMoily");
        game.ConnectToGate(seller, moily);
        var mine = game.Build(buyer, "ironMine", game.Spot(buyer, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(buyer, mine);
        game.Connect(buyer, mine, game.TradingPost(buyer));
        game.Pass(3);

        Assert.IsGreaterThan(0, game.World.StatValue(seller, "neighbourIncome"), "the mine buys the moily's energy through the Yard");
        Assert.AreEqual(0, game.World.StatValue(buyer, "neighbourIncome"));
    }
}
