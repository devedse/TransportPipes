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

        Assert.AreEqual(AdvisorAction.Build, step.Action, step.Title);
        StringAssert.Contains(step.Title, "copper");
        var maker = game.World.Content.Building(step.BuildingType!);
        Assert.IsTrue(maker.Out.ContainsKey("copperIngot") || maker.Out.ContainsKey("copperOre"), "the smelter, or the copper mine before it");
    }
}
