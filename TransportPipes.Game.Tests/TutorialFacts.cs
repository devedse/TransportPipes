using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class TutorialFacts
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void TheGateStepCompletesOnConnectionWithoutWaitingForATradeRound(int slot)
    {
        var game = new TestGame(content: TestGame.PlainContent, tutorial: true);
        var company = game.NewCompany(slot: slot);
        company.TutorialStep = game.World.Content.Tutorial.ToList().FindIndex(t => t.Id == "firstYard");
        game.Pass(0.01);
        Assert.AreEqual("firstYard", game.World.CurrentTutorialStep(company)!.Id);

        game.ConnectToGate(company, game.TradingPost(company));
        game.Pass(0.01);

        Assert.AreEqual(0, game.World.StatValue(company, "yard"), "no trading round has run yet");
        Assert.AreEqual("firstMine", game.World.CurrentTutorialStep(company)!.Id, "the connection itself completes the quest");
    }

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
    public void LoadysUpgradeStepOpensABuildingWhereALevelPays()
    {
        // Run 4: "upgrade it" opened a Solar Panel whose preview said "adds nothing" (Mia-B: "a bit odd to be told to
        // upgrade something pointless")
        var game = new TestGame(tutorial: true);
        game.World.OpenNextDistrict();
        var company = game.NewCompany(gold: 2000, slot: 3);
        var pipes = game.World.Conductors.Select(t => t.Pos).ToHashSet();
        var panel = game.Build(company, "solarPanel", game.Spot(company, "solarPanel", t => !TestGame.Neighbours(t).Any(pipes.Contains)));
        game.Connect(company, panel, game.TradingPost(company));
        game.Pass(2);
        var step = game.World.CurrentTutorialStep(company)!;
        Assert.AreEqual("firstUpgrade", step.Id);

        var hint = game.World.TutorialHint(company, step);

        Assert.IsTrue(hint.ContainsKey("target"), "Show me opens a building the server chose");
        var target = game.World.Building(long.Parse(hint["target"]))!;
        var preview = game.World.UpgradePreview(company, target, 1);
        Assert.IsGreaterThan(0.05, preview.Gain, $"{target.Type}: {preview.Text}");
        StringAssert.StartsWith(preview.Text, "+");
        foreach (var other in game.World.BuildingsOf(company.Id).Where(t => t.Id != target.Id && game.World.UpgradePreview(company, t, 1).Gain > 0.05))
        {
            Assert.IsLessThanOrEqualTo(game.World.UpgradeCost(other, 1) / game.World.UpgradePreview(company, other, 1).Gain + 1e-9,
                game.World.UpgradeCost(target, 1) / preview.Gain, $"the soonest payback: not {other.Type}'s");
        }
    }

    [TestMethod]
    public void LoadySaysWhyABuiltFactoryDoesntCountYet()
    {
        // Run 4: Loady's bubble kept "Put a Small Factory…" after Dev built it (the step counts its first ingot)
        var game = new TestGame(content: TestGame.PlainContent, tutorial: true);
        var company = game.NewCompany(gold: 100_000, level: 5);
        company.TutorialStep = game.World.Content.Tutorial.ToList().FindIndex(t => t.Id == "firstIngot");
        var step = game.World.CurrentTutorialStep(company)!;
        game.Build(company, "smallFactory", finish: false);

        StringAssert.Contains(game.World.TutorialTip(company, step), "Small Factory is being built");

        // One on pipes with energy and no ore
        var other = game.NewCompany("Other", gold: 100_000, level: 5);
        other.TutorialStep = company.TutorialStep;
        var map = game.Map(other, game.Local(other, 1, 1), "W+F+T");
        game.Pass(2);
        Assert.AreEqual(IdleCause.InputMissing, game.World.IdleReasonOf(map[(2, 0)]).Cause, "no ore on its pipes");
        StringAssert.StartsWith(game.World.TutorialTip(other, step), "Your Small Factory stands but makes nothing yet");
    }

    [TestMethod]
    public void LoadySaysHowFarTodaysOrderIs()
    {
        // Playtest: "I have no idea how much I'm contributing to this 2200 order now"
        var game = new TestGame(content: TestGame.PlainContent, tutorial: true);
        var company = game.NewCompany(gold: 100_000, level: 3);
        company.TutorialStep = game.World.Content.Tutorial.ToList().FindIndex(t => t.Id == "firstOrder");
        var step = game.World.CurrentTutorialStep(company)!;
        game.Map(company, game.Local(company, 1, 1), " SSSSS\nT+++++T");
        game.World.EnsureOrders(company);
        var order = company.DailyOrders.Single();
        order.Delivered = 3;

        StringAssert.StartsWith(game.World.TutorialTip(company, step), $"Your energy order: 3 of {order.Amount:N0} delivered, full in about");

        var storage = game.Build(company, "storage");
        storage.Contents["energy"] = 100;
        StringAssert.EndsWith(game.World.TutorialTip(company, step), "Your Storages have some: Deliver in the Hub's orders fills it now.");
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
        game.ReachLevel(climber, 10);
        game.Pass(1);

        Assert.AreEqual(2, game.World.CurrentTutorialStep(veteran)!.Chapter, "a veteran starts with chapter 2");
        Assert.AreEqual(2, game.World.CurrentTutorialStep(climber)!.Chapter, "at Level 10 chapter 1 is over for everyone");
        Assert.IsTrue(game.World.TakeChanges().Events.Any(t => t.Company == climber.Id && t.Text.Contains("past my first chapter")));
        Assert.AreEqual(1, game.World.CurrentTutorialStep(newcomer)!.Chapter);
        Assert.IsTrue(game.World.SkipTutorial(newcomer.Id).Success);
        Assert.IsNull(game.World.CurrentTutorialStep(newcomer));
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

        Assert.AreEqual(11, tutorial.Count(t => t.Chapter == 1), "chapter 1, the first hour of 1.4 §6");
        Assert.AreEqual(8, tutorial.Count(t => t.Chapter == 2), "with the first Copper Pipe (after run 4)");
        Assert.AreEqual((2, 1, 8), game.World.TutorialChapter(firstOfTwo));
        Assert.IsTrue(tutorial.Zip(tutorial.Skip(1)).All(t => t.Second.Chapter >= t.First.Chapter), "the chapters in order");
        Assert.IsTrue(tutorial.Where(t => t.Chapter > 2).All(t => t.Era == t.Chapter - 1), "after chapter 2, a chapter for every Era (1.4 §11)");
    }

    [TestMethod]
    public void AnErasChapterWaitsUntilYourTownGetsThere()
    {
        var game = new TestGame(content: TestGame.PlainContent, tutorial: true);
        var company = game.NewCompany(level: 15);
        var tutorial = game.World.Content.Tutorial;
        company.TutorialStep = tutorial.ToList().FindIndex(t => t.Era == 2);
        company.Stats["outskirtsGold"] = 1;

        game.Pass(1);

        Assert.AreEqual(2, game.World.TutorialWaitEra(company, game.World.CurrentTutorialStep(company)!));
        Assert.AreEqual(tutorial.ToList().FindIndex(t => t.Era == 2), company.TutorialStep, "nothing of it counts before Era 2");
        game.World.AdvanceEra(company.District);
        Assert.IsNull(game.World.TutorialWaitEra(company, game.World.CurrentTutorialStep(company)!));
    }

    [TestMethod]
    public void AChainStepWaitsForTheLevelOfTheNextBuildingItNeeds()
    {
        var game = new TestGame(content: TestGame.PlainContent, tutorial: true);
        var company = game.NewCompany(level: 4);
        var copper = game.World.Content.Tutorial.Single(t => t.Id == "copperIngots");
        CollectionAssert.AreEqual(new[] { "copperMine", "coalPit", "smelter" }, copper.HintBuildings.ToArray());

        Assert.AreEqual(5, game.World.TutorialWaitLevel(company, copper), "the Copper Mine comes at Level 5");
        company.Level = 5;
        Assert.IsNull(game.World.TutorialWaitLevel(company, copper));
        game.Build(company, "copperMine", game.Seed(company, "copperOre"));
        game.Build(company, "coalPit", game.Seed(company, "coal"));
        Assert.AreEqual(6, game.World.TutorialWaitLevel(company, copper), "then the Smelter, at Level 6");
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
        Assert.AreEqual("copperPipe", game.World.CurrentTutorialStep(company)!.Id, "the milestone was done already; the first Copper Pipe is next");
        company.Stats["pipeTiers"] = 3;
        game.Pass(1);
        Assert.AreEqual("moreOrders", game.World.CurrentTutorialStep(company)!.Id, "the researches were done already");
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
