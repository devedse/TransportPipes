using TransportPipes.Game.Content;
using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>Research (1.4 §12): two lines in the Lab, the tracks and the Era tree</summary>
[TestClass]
public class ResearchFacts
{
    [TestMethod]
    public void TheResearchContentIsComplete()
    {
        var content = GameContent.Default;

        Assert.HasCount(21, content.TrackList);
        Assert.HasCount(9, content.TrackRules.Tiers);
        CollectionAssert.AreEqual(new double[] { 500, 3000, 20000, 100000, 500000, 2_000_000, 8_000_000, 20_000_000, 50_000_000 },
            content.TrackRules.Tiers.Select(t => t.Gold).ToArray(), "the tier gold of an Era-1 track (1.4 §22)");
        Assert.AreEqual(9.7 * 24 * 60, content.TrackRules.Tiers.Sum(t => t.Minutes), 60, "about ten days to master a track");
        Assert.AreEqual(4 * 3000, content.TrackRules.TierGold(2, 2), "four times the gold a later Era");
        Assert.AreEqual(6, content.TrackList.Count(t => t.Era == 1), "Power, Iron, Copper, Stone, Sand, Wood");
        Assert.IsFalse(content.Tracks.ContainsKey("coal"), "coal belongs to the Copper and Stone tracks");
        foreach (var era in Enumerable.Range(1, 5))
        {
            Assert.IsNotEmpty(content.ResearchList.Where(t => t.Era == era), $"the Era tree has items in Era {era}");
        }
        Assert.AreEqual(75_000_000, content.ResearchList.Where(t => t.Era == 2).Sum(t => t.Gold), "the Era tree costs 75 million in Era 2");
    }

    private static (TestGame Game, Company Company) WithLab(int level = 4, double gold = 1_000_000_000)
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: gold, level: level);
        game.Build(company, "lab");
        return (game, company);
    }

    [TestMethod]
    public void EveryLabLevelMakesResearchAPercentFaster()
    {
        // The owner, 2026-10-31: its levels did nothing; "maybe 1% per level"
        var game = new TestGame();
        var company = game.NewCompany(level: 10);
        var lab = game.Build(company, "lab");
        game.SetLevel(lab, 11);

        Assert.IsTrue(game.World.StartResearch(company.Id, "iron").Success);

        Assert.AreEqual(1.1, game.World.LabSpeed(company), 1e-9);
        Assert.AreEqual(10 / 1.1, (company.TrackEndsAt!.Value - game.Time).TotalMinutes, 1e-6, "a tenth faster at level 11");
    }

    [TestMethod]
    public void TheBigFactorysResearchOpensItBeforeLevelTwelveAndLevelTwelveGivesIt()
    {
        // Playtest: like the Copper Pipes it read as research, but it also needed Level 12; researched early, nothing came
        var (game, company) = WithLab(level: 6);
        var big = game.World.Content.Building("bigFactory");
        Assert.IsFalse(game.World.IsUnlocked(company, big, out var reason));
        Assert.AreEqual("Big Factory needs Company Level 12, or its research in your Lab sooner.", reason);

        Assert.IsTrue(game.World.StartResearch(company.Id, "bigFactory").Success);
        game.Pass(31);

        Assert.IsTrue(game.World.IsUnlocked(company, big, out reason), reason);

        // Without the research, Level 12 gives it, and says so
        var other = game.NewCompany(level: 11);
        game.World.TakeChanges();
        game.ReachLevel(other, 12);
        game.Pass(1);
        Assert.IsTrue(game.World.HasResearch(other, "bigFactory"));
        Assert.IsTrue(game.World.IsUnlocked(other, big, out reason), reason);
        Assert.IsTrue(game.World.TakeChanges().Events.Any(t => t.Company == other.Id && t.Text == "Big Factory is yours at Company Level 12: you can build the Big Factory now"));
    }

    [TestMethod]
    public void ResearchNeedsALab()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 4);

        StringAssert.Contains(game.World.StartResearch(company.Id, "iron").Error, "Lab");
        StringAssert.Contains(game.World.StartResearch(company.Id, "valves").Error, "Lab");
        game.Build(company, "lab");
        Assert.IsTrue(game.World.StartResearch(company.Id, "iron").Success);
    }

    [TestMethod]
    public void ATierCostsGoldAndTimeThenItsBuildingsWorkFaster()
    {
        var (game, company) = WithLab();
        var mine = game.Build(company, "ironMine", game.Spot(company, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        var before = game.World.ToFlowNode(mine)!;
        var gold = company.Gold;

        Assert.IsTrue(game.World.StartResearch(company.Id, "iron").Success);
        Assert.AreEqual(gold - 500, company.Gold, 1e-6, "tier 1 costs 500 gold");
        game.Pass(9);
        Assert.AreEqual(0, game.World.TrackTier(company, "iron"), "ten minutes");
        game.Pass(2);

        Assert.AreEqual(1, game.World.TrackTier(company, "iron"));
        var after = game.World.ToFlowNode(mine)!;
        Assert.AreEqual(before.Out["ironOre"] * 1.25, after.Out["ironOre"], 1e-9, "faster: ×1.25");
        Assert.AreEqual(before.In["energy"] * 1.25, after.In["energy"], 1e-9, "everything it takes goes up too");
    }

    [TestMethod]
    public void ThePowerTrackCountsHalfAndItsLeanerTiersMakeWindAndSunMore()
    {
        var (game, company) = WithLab();
        var moily = game.Build(company, "windMoily");
        var wind = game.World.ToFlowNode(moily)!.Out["energy"];

        game.World.SetTrackTiers(company, "power", 1);
        Assert.AreEqual(wind * 1.125, game.World.ToFlowNode(moily)!.Out["energy"], 1e-9, "half of ×1.25");
        game.World.SetTrackTiers(company, "power", 2);
        Assert.AreEqual(wind * 1.125 * 1.2, game.World.ToFlowNode(moily)!.Out["energy"], 1e-9, "leaner: wind has no inputs to save, it makes 20% more");
    }

    [TestMethod]
    public void LeanerTiersSaveInputs()
    {
        var (game, company) = WithLab();
        var factory = game.Build(company, "smallFactory");
        var before = game.World.ToFlowNode(factory)!;

        game.World.SetTrackTiers(company, "iron", 2);

        var after = game.World.ToFlowNode(factory)!;
        Assert.AreEqual(before.Out["ironIngot"] * 1.25, after.Out["ironIngot"], 1e-9);
        Assert.AreEqual(before.In["ironOre"] * 1.25 * 0.8, after.In["ironOre"], 1e-9, "20% less of every input for the same output");
    }

    [TestMethod]
    public void ACoalPitFollowsTheBetterOfCopperAndStone()
    {
        var (game, company) = WithLab(level: 5);
        var pit = game.Build(company, "coalPit", game.Seed(company, "coal"));
        var coal = game.World.ToFlowNode(pit)!.Out["coal"];

        game.World.SetTrackTiers(company, "stone", 1);
        game.World.SetTrackTiers(company, "copper", 3);

        Assert.AreEqual("copper", game.World.TrackOf(pit).Track?.Id);
        Assert.AreEqual(coal * 1.25 * 1.5, game.World.ToFlowNode(pit)!.Out["coal"], 1e-9);
    }

    [TestMethod]
    public void FromTierThreeATierTakesItsTracksGoodAndBuysWhatTheStoragesLack()
    {
        var (game, company) = WithLab();
        game.World.SetTrackTiers(company, "iron", 2);
        game.Build(company, "storage");
        game.World.Stock(company.Id, "ironIngot", 100);
        var track = game.World.Content.Tracks["iron"];
        var cost = game.World.TierCost(company, track, 3);
        Assert.AreEqual(20_000, cost.Gold);
        Assert.AreEqual(Math.Ceiling(20_000 * 0.2 / 14), cost.Units, "iron ingots worth a fifth of the gold");
        Assert.AreEqual(100, cost.FromStorages);
        var gold = company.Gold;

        Assert.IsTrue(game.World.StartResearch(company.Id, "iron").Success);

        Assert.AreEqual(0, game.World.Materials(company.Id).GetValueOrDefault("ironIngot"), 1e-9, "its Storages gave theirs");
        Assert.AreEqual(gold - 20_000 - (cost.Units - 100) * 14 * 2.5, company.Gold, 1, "the rest bought at the market's 2.5×");
    }

    [TestMethod]
    public void FromTierFiveEveryTierMakesTheTracksHubOrdersBigger()
    {
        var (game, company) = WithLab();
        var order = game.World.HubOrder(company, "ironIngot");
        Assert.AreEqual(1000 / 14.0, order, 1e-6, "an Era-1 good: 1,000 gold's worth a minute");

        game.World.SetTrackTiers(company, "iron", 4);
        Assert.AreEqual(order, game.World.HubOrder(company, "ironIngot"), 1e-6, "the first four tiers make the chain better, not the market bigger");
        game.World.SetTrackTiers(company, "iron", 6);
        Assert.AreEqual(order * 1.8 * 1.8, game.World.HubOrder(company, "ironIngot"), 1e-6);
        Assert.AreEqual(1000 / 3.5, game.World.HubOrder(company, "planks"), 1e-6, "only its own goods");
    }

    [TestMethod]
    public void TheTwoLinesRunSideBySideAndTheirQueuesStartByThemselves()
    {
        var (game, company) = WithLab();
        Assert.IsTrue(game.World.StartResearch(company.Id, "iron").Success);
        Assert.IsTrue(game.World.StartResearch(company.Id, "copperPipes").Success, "the Era tree on the second line");
        Assert.IsTrue(game.World.QueueResearch(company.Id, "iron", true).Success);
        Assert.IsTrue(game.World.QueueResearch(company.Id, "power", true).Success);
        Assert.IsTrue(game.World.QueueResearch(company.Id, "valves", true).Success);

        game.Pass(11);
        Assert.AreEqual(1, game.World.TrackTier(company, "iron"));
        Assert.IsTrue(game.World.HasResearch(company, "copperPipes"));
        Assert.AreEqual("iron", company.CurrentTrack, "its second tier, from the queue");
        Assert.AreEqual("valves", company.CurrentResearch);

        game.Pass(61);
        Assert.AreEqual(2, game.World.TrackTier(company, "iron"));
        Assert.AreEqual("power", company.CurrentTrack);
        Assert.IsTrue(game.World.HasResearch(company, "valves"));
    }

    [TestMethod]
    public void ATrackStandsInTheQueueOnlyAsOftenAsItHasTiersLeft()
    {
        var (game, company) = WithLab();
        game.World.SetTrackTiers(company, "wood", 7);
        Assert.IsTrue(game.World.QueueResearch(company.Id, "wood", true).Success);
        Assert.IsTrue(game.World.QueueResearch(company.Id, "wood", true).Success);

        StringAssert.Contains(game.World.QueueResearch(company.Id, "wood", true).Error, "Every tier");
    }

    [TestMethod]
    public void TheMasterGetsATitle()
    {
        var (game, company) = WithLab();
        game.World.SetTrackTiers(company, "sand", 8);

        Assert.IsTrue(game.World.StartResearch(company.Id, "sand").Success);
        game.World.FinishResearch(company.Id);

        Assert.AreEqual(9, game.World.TrackTier(company, "sand"));
        Assert.Contains("Sand Master", company.Titles);
        StringAssert.Contains(game.World.StartResearch(company.Id, "sand").Error, "Mastered");
    }

    [TestMethod]
    public void ComplexesComeWithTheTracksFourthTier()
    {
        var (game, company) = WithLab();
        var panels = game.Map(company, game.Local(company, 20, 20), "SS\nSS").Values.ToList();
        foreach (var panel in panels)
        {
            game.World.FinishConstruction(panel.Id);
        }
        Assert.HasCount(4, game.World.BuildingsOf(company.Id).Where(t => t.Type == "solarPanel"));

        game.World.SetTrackTiers(company, "power", 4);

        var complex = game.World.BuildingsOf(company.Id).Single(t => t.Type == "solarPanel");
        Assert.AreEqual(4, game.World.ComplexMembers(complex));
        Assert.IsFalse(panels.Any(t => game.World.Building(t.Id) != null), "the four merged into one");
    }

    [TestMethod]
    public void CopperPipesComeAtLevelEightForEveryone()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 7);
        Assert.IsFalse(game.World.HasResearch(company, "copperPipes"));

        company.Gold += game.World.Content.Progression.EarnedForLevel(8);
        game.World.CountEarned(company, game.World.Content.Progression.EarnedForLevel(8), "test", "gold");

        Assert.AreEqual(8, company.Level);
        Assert.IsTrue(game.World.HasResearch(company, "copperPipes"));
    }

    [TestMethod]
    public void ResearchKeepsGoingWhileThePlayerIsAway()
    {
        var (game, company) = WithLab();
        company.LastSeenAt = game.Time;

        game.Pass(60);

        Assert.IsGreaterThanOrEqualTo(1, company.TrackTiers.Values.Sum() + company.Researched.Count(t => t != "copperPipes"), "the cheapest started by itself");
        Assert.IsNotNull(company.CurrentTrack ?? company.CurrentResearch);
    }

    [TestMethod]
    public void AnEraTwoTrackWaitsForEraTwo()
    {
        var (game, company) = WithLab();

        StringAssert.Contains(game.World.StartResearch(company.Id, "steel").Error, "Era 2");
        game.World.AdvanceEra(company.District);
        Assert.IsTrue(game.World.StartResearch(company.Id, "steel").Success);
        Assert.AreEqual(500 * 4, game.World.Content.TrackRules.TierGold(1, 2));
    }
}
