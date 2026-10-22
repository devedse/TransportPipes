using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>The advisor's next steps (docs/v1.1-design.html §4): what they suggest, and that their numbers are the game's</summary>
[TestClass]
public class AdvisorFacts
{
    [TestMethod]
    public void AFactoryShortOfEnergyIsTheFirstStep()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 3);
        // A mine and a factory on the trading post's pipes, and no energy at all
        game.Map(company, game.Local(company, 2, 2), "M+F+T");

        var steps = game.World.NextSteps(company);

        var first = steps[0];
        Assert.AreEqual(AdvisorKind.Shortage, first.Kind, string.Join(" | ", steps.Select(t => t.Title)));
        StringAssert.Contains(first.Title, "energy short");
        Assert.AreEqual(AdvisorAction.Build, first.Action);
        Assert.IsTrue(first.BuildingType is "solarPanel" or "windMoily", first.BuildingType);
        Assert.IsGreaterThan(0, first.Cost);
        Assert.IsNotNull(first.At, "a spot where it can stand");
        Assert.IsTrue(game.World.CanPlace(company, game.World.Content.Building(first.BuildingType!), first.At!.Value).Success);
    }

    [TestMethod]
    public void AFactoryWhoseIngotsCantLeaveIsNotShortOfEnergy()
    {
        // Ana's day 4: the factory piped to the mine and a windmill, the mine to the trading post and a windmill of its
        // own (run 1's network card said "Energy 20 short" for 32 days)
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 3);
        var map = game.Map(company, game.Local(company, 2, 2), "T+M+F+W\n  +\n  W");
        var factory = map[(4, 0)];
        game.Pass(1);

        Assert.IsFalse(game.World.NetworkNeeds(game.World.NetworkOf(factory.Id)!, company.Id)
            .Any(t => t.Value.Consumers.Contains(factory.Id)), "a stuck factory is short of nothing");
        var steps = game.World.NextSteps(company);
        Assert.IsFalse(steps.Any(t => t.Kind == AdvisorKind.Shortage), string.Join(" | ", steps.Select(t => t.Title)));
        var unstick = steps.Single(t => t.Kind == AdvisorKind.Stuck);
        StringAssert.StartsWith(unstick.Title, "Unstick your Small Factory: one pipe");
        StringAssert.Contains(unstick.Reason, "iron ingots can't leave");
        Assert.AreEqual(AdvisorAction.LayPipe, unstick.Action);
        Assert.IsNotNull(unstick.Tiles);
    }

    [TestMethod]
    public void AProducerWhoseOnlyTakerIsStuckSaysSo()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 3);
        var map = game.Map(company, game.Local(company, 2, 2), "W+F");
        game.Pass(1);

        var status = game.World.IdleStatusOf(map[(0, 0)]);

        Assert.AreEqual("Idle: Small Factory is stuck", status.Pill);
        Assert.AreEqual(IdleFixKind.Open, status.Fix!.Kind);
        Assert.AreEqual(map[(2, 0)].Id, status.Fix.Building);
        Assert.AreEqual("⛔ Stuck: iron ingots can't leave", game.World.IdleStatusOf(map[(2, 0)]).Pill);
    }

    [TestMethod]
    public void TheFixPipeStaysOnTheEstateAndReachesThePost()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 3);
        var map = game.Map(company, game.Local(company, 4, 4), "M+F+W");
        var factory = map[(2, 0)];
        game.Pass(1);
        Assert.IsTrue(game.World.IdleReasonOf(factory).IsStuck);

        var plan = game.World.PlanFixPipe(factory)!;

        Assert.AreEqual("tradingPost", plan.Target!.Type);
        Assert.IsTrue(plan.Tiles.All(t => game.EstateOf(company).Contains(t)), "on the owner's estate");
        Assert.AreEqual(plan.NewTiles * game.World.Content.PipeTier(1).Gold, plan.Cost, 1e-9);
        var gold = company.Gold;
        Assert.IsTrue(game.World.PlacePipes(company.Id, plan.Tiles).Success, "the player's own rules allow it");
        Assert.AreEqual(gold - plan.Cost, company.Gold, 1e-6, "it costs what the button says");
        game.Pass(1);
        Assert.AreNotEqual(IdleCause.Stuck, game.World.IdleReasonOf(factory).Cause, "the ingots can leave now");
    }

    [TestMethod]
    public void ADemolishedTypeIsNotSuggestedForAWeek()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");
        Assert.IsTrue(game.World.Demolish(company.Id, moily.Id).Success);

        var steps = game.World.NextSteps(company);

        Assert.IsFalse(steps.Any(t => t.Action == AdvisorAction.Build && t.BuildingType == "windMoily"), string.Join(" | ", steps.Select(t => t.Title)));
        game.Pass(8 * 24 * 60);
        Assert.IsFalse(game.World.DemolishedRecently(company, "windMoily"), "a week later it may come back");
    }

    [TestMethod]
    public void NoTwoStepsShareATitle()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 6, gold: 200_000);
        game.Map(company, game.Local(company, 2, 2), "W+T+W\n\nS+F+M\n\nM+F+S");

        var steps = game.World.NextSteps(company, count: 6);

        Assert.AreEqual(steps.Count, steps.Select(t => t.Title).Distinct().Count(), string.Join(" | ", steps.Select(t => t.Title)));
    }

    [TestMethod]
    public void AnUpgradeStepEarnsWhatItSays()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 50_000);
        var moily = game.Map(company, game.Local(company, 2, 2), "W+T")[(0, 0)];

        var step = game.World.NextSteps(company).First(t => t.Kind is AdvisorKind.Upgrade or AdvisorKind.Full && t.Building == moily.Id);
        var before = game.World.GoldPerMinute()[company.Id];
        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, step.Levels).Success);
        game.World.SolveNetworks();
        var after = game.World.GoldPerMinute()[company.Id];

        Assert.AreEqual(after - before, step.GainPerMinute, Math.Max(0.01, step.GainPerMinute * 0.01), "the gain is what the flow solution says");
        Assert.AreEqual(step.Cost / step.GainPerMinute, step.PaybackMinutes!.Value, 1e-9);
    }

    [TestMethod]
    public void IdleLabsWithPointsSuggestTheResearchThatHelpsMost()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 5);
        game.Map(company, game.Local(company, 2, 2), "W+M\n + \n +T");
        game.Build(company, "lab");
        company.ResearchPoints = 100_000;

        var research = game.World.NextSteps(company, 8).SingleOrDefault(t => t.Kind == AdvisorKind.Research);

        Assert.IsNotNull(research);
        Assert.AreEqual(AdvisorAction.Research, research.Action);
        Assert.AreEqual(0, research.Cost, "research costs points, not gold");
        var tech = game.World.Content.ResearchList.Single(t => research.Title.EndsWith(t.Name, StringComparison.Ordinal));
        Assert.IsNull(game.World.ResearchBlocker(company, tech), "a tech the company can start");
    }

    [TestMethod]
    public void StepsNeverSuggestLockedBuildingsAndSayWhenTheyAreAffordable()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 10, level: 2);
        game.Map(company, game.Local(company, 2, 2), "W+M\n + \nF+T");

        var steps = game.World.NextSteps(company, 8);

        Assert.IsNotEmpty(steps);
        foreach (var step in steps.Where(t => t.BuildingType != null && t.Action == AdvisorAction.Build))
        {
            Assert.IsTrue(game.World.IsUnlocked(company, game.World.Content.Building(step.BuildingType!), out _), step.Title);
        }
        foreach (var step in steps.Where(t => t.Cost > company.Gold))
        {
            Assert.IsNotNull(step.WaitMinutes, $"{step.Title} says when");
        }
    }

    [TestMethod]
    public void TheNextParcelShowsWhatItHolds()
    {
        var game = new TestGame(surveys: true);
        var company = game.NewCompany(gold: 1_000_000, level: 5);

        var parcel = game.World.NextSteps(company, 8).SingleOrDefault(t => t.Kind == AdvisorKind.Parcel);

        Assert.IsNotNull(parcel);
        Assert.IsNotNull(parcel.Parcel);
        Assert.IsTrue(game.World.BuyParcel(company.Id, parcel.Parcel!.Value).Success, "a parcel the company can buy");
    }

    [TestMethod]
    public void RightAfterAnUpgradeNothingLooksShort()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 3);
        var map = game.Map(company, game.Local(company, 2, 2), "W+M\n + \nF+T");
        Assert.IsTrue(game.World.Upgrade(company.Id, map[(0, 0)].Id, 9).Success, "the network is rebuilt, not solved yet");

        var steps = game.World.NextSteps(company, 8);

        Assert.IsFalse(steps.Any(t => t.Kind == AdvisorKind.Shortage), string.Join(" | ", steps.Select(t => t.Title)));
    }

    [TestMethod]
    public void FullPipesAreUpgradedWhereTheyLie()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 3);
        // A moily at level 10 makes 141 energy; a grey pipe carries 100 a minute to the trading post
        var map = game.Map(company, game.Local(company, 2, 2), "W+++T", pipeTier: 1);
        Assert.IsTrue(game.World.Upgrade(company.Id, map[(0, 0)].Id, 9).Success);
        company.ResearchPoints = 1_000;
        game.World.SolveNetworks();

        var step = game.World.NextSteps(company, 8).SingleOrDefault(t => t.Kind == AdvisorKind.Pipes);

        Assert.IsNotNull(step, string.Join(" | ", game.World.NextSteps(company, 8).Select(t => t.Title)));
        Assert.AreEqual(AdvisorAction.Research, step.Action, "Copper Pipes need research first");
        Assert.AreEqual("copperPipes", step.Research);

        // Copper Pipes need a copper ingot per tile from the storages
        company.Researched.Add("copperPipes");
        game.Map(company, game.Local(company, 10, 2), "O");
        game.World.Building(game.World.BuildingAt(game.Local(company, 10, 2))!.Id)!.Contents["copperIngot"] = 100;
        step = game.World.NextSteps(company, 8).Single(t => t.Kind == AdvisorKind.Pipes);
        Assert.AreEqual(AdvisorAction.Pipes, step.Action);
        Assert.AreEqual(2, step.Tier);
        var before = game.World.GoldPerMinute()[company.Id];
        Assert.IsTrue(game.World.UpgradePipes(company.Id, step.Tiles!, step.Tier).Success);
        game.World.SolveNetworks();

        Assert.IsTrue(step.Tiles!.All(t => game.World.ConductorAt(t)!.Tier == 2), "upgraded where they lie");
        Assert.AreEqual(before + step.GainPerMinute, game.World.GoldPerMinute()[company.Id], Math.Max(0.01, step.GainPerMinute * 0.02), "it brings what it said");
        Assert.IsFalse(game.World.NextSteps(company, 8).Any(t => t.Kind == AdvisorKind.Pipes), "not full any more");
    }

    [TestMethod]
    public void OnlyOwnLowerPipesAreUpgradedAndPaidFor()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1_000, level: 3);
        var other = game.NewCompany("Other", level: 3);
        company.Researched.Add("copperPipes");
        game.Map(company, game.Local(company, 2, 2), "++", pipeTier: 1);
        game.Map(company, game.Local(company, 2, 3), "+", pipeTier: 2);
        game.Map(other, game.Local(other, 2, 2), "+", pipeTier: 1);
        game.Map(company, game.Local(company, 6, 2), "O");
        game.World.Building(game.World.BuildingAt(game.Local(company, 6, 2))!.Id)!.Contents["copperIngot"] = 10;
        var tiles = new[] { game.Local(company, 2, 2), game.Local(company, 3, 2), game.Local(company, 2, 3), game.Local(other, 2, 2) };

        Assert.IsTrue(game.World.UpgradePipes(company.Id, tiles, 2).Success);

        Assert.AreEqual(2, game.World.ConductorAt(tiles[0])!.Tier);
        Assert.AreEqual(2, game.World.ConductorAt(tiles[1])!.Tier);
        Assert.AreEqual(1, game.World.ConductorAt(tiles[3])!.Tier, "not someone else's");
        var copper = game.World.Content.PipeTier(2);
        Assert.AreEqual(1_000 - 2 * copper.Gold, company.Gold, 1e-9, "two tiles: the copper one was already copper");
        Assert.IsFalse(game.World.UpgradePipes(company.Id, tiles, 2).Success, "nothing left to upgrade");
    }

    [TestMethod]
    public void PipesWithoutTheirMaterialsPointAtMakingThem()
    {
        // The starter kit: copper ore on every estate
        var game = new TestGame(content: TestGame.PlainContent, surveys: true);
        var company = game.NewCompany(level: 10);
        var map = game.Map(company, game.Local(company, 2, 2), "W+++T", pipeTier: 1);
        Assert.IsTrue(game.World.Upgrade(company.Id, map[(0, 0)].Id, 9).Success);
        company.Researched.Add("copperPipes");
        game.World.SolveNetworks();

        var step = game.World.NextSteps(company, 8).Single(t => t.Kind == AdvisorKind.Pipes);

        // Materials come out of storages: a Storage first
        Assert.AreEqual("storage", step.BuildingType, step.Title);
        game.Build(company, "storage");
        game.World.SolveNetworks();
        step = game.World.NextSteps(company, 8).Single(t => t.Kind == AdvisorKind.Pipes);
        Assert.AreEqual(AdvisorAction.Build, step.Action, step.Title);
        StringAssert.Contains(step.Title, "copper");
        var maker = game.World.Content.Building(step.BuildingType!);
        Assert.IsTrue(maker.Out.ContainsKey("copperIngot") || maker.Out.ContainsKey("copperOre"), "the smelter, or the copper mine before it");
    }

    [TestMethod]
    public void ABuildingThatSellsNothingIsPipedFirst()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");

        var step = game.World.NextSteps(company)[0];

        Assert.AreEqual(AdvisorKind.Start, step.Kind);
        Assert.AreEqual(AdvisorAction.Connect, step.Action);
        Assert.AreEqual(moily.Id, step.Building);
        StringAssert.Contains(step.Title, "Pipe your Wind Moily");
        Assert.IsGreaterThan(0, step.GainPerMinute);
    }

    [TestMethod]
    public void ANewCompanyIsToldWhatToBuildFirst()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();

        var step = game.World.NextSteps(company).Single();

        Assert.AreEqual(AdvisorKind.Start, step.Kind);
        Assert.AreEqual(AdvisorAction.Build, step.Action);
        Assert.IsTrue(step.BuildingType is "solarPanel" or "windMoily", step.BuildingType);
        Assert.IsTrue(game.World.CanPlace(company, game.World.Content.Building(step.BuildingType!), step.At!.Value).Success);
    }

    [TestMethod]
    public void StoragesKeepTheMaterialsOfThePipesYouCanLay()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 10);
        Assert.AreEqual(0, game.World.StorageNeed(company, "copperIngot"), "not before Copper Pipes");

        company.Researched.Add("copperPipes");

        Assert.AreEqual(GameWorld.PipeMaterialReserve * game.World.Content.PipeTier(2).Materials["copperIngot"], game.World.StorageNeed(company, "copperIngot"));
    }
}
