using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>
/// What the game says will happen, happens (the owner, 2026-10-31: "whenever something says x will happen, it actually
/// does, so for example the grow button should match exactly"): every preview of gold a minute is checked against what
/// the top bar says once it is bought and the market has looked again
/// </summary>
[TestClass]
public class PromiseFacts
{
    /// <summary>Within 1% (or half a gold): what the market's rounding and a solve's last digits leave</summary>
    private static void Kept(double said, double got, string what) =>
        Assert.AreEqual(got, said, Math.Max(0.5, Math.Abs(got) * 0.01), $"{what}: said +{said:0.0}, brought +{got:0.0}");

    private static double Income(TestGame game, Company company) => game.World.GoldPerMinute().GetValueOrDefault(company.Id);

    /// <summary>Moilies (and a Small Factory on iron ore) piped to the trading post; flooded: more energy than its Hub Order takes</summary>
    private static (TestGame Game, Company Company, Dictionary<(int X, int Y), Building> Map) Estate(int moilies = 4, bool factory = false)
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 50_000_000, level: 20);
        var row = new string('W', moilies) + (factory ? "MF" : "");
        var map = game.Map(company, game.Local(company, 1, 1), row + "\n" + new string('+', row.Length) + "T");
        game.Pass(10);
        return (game, company, map);
    }

    [TestMethod]
    public void TheSettledIncomeIsTheTopBarOnceTheMarketHasLooked()
    {
        foreach (var moilies in new[] { 2, 30 })
        {
            var (game, company, _) = Estate(moilies, factory: true);
            Assert.AreEqual(Income(game, company), game.World.SettledIncome(company), Math.Max(0.1, Income(game, company) * 0.002), $"{moilies} moilies");
        }
    }

    [TestMethod]
    public void ALevelBringsWhatItsPreviewSays()
    {
        foreach (var (moilies, factory) in new[] { (2, false), (30, false), (3, true) })
        {
            var (game, company, map) = Estate(moilies, factory);
            var building = factory ? map[(moilies + 1, 0)] : map[(0, 0)];
            var before = Income(game, company);
            var preview = game.World.UpgradePreview(company, building, 3);

            game.UpgradeNow(company, [building.Id], 3);
            game.Pass(5);

            Kept(preview.Gain, Income(game, company) - before, $"{moilies} moilies{(factory ? ", the factory" : "")}");
        }
    }

    [TestMethod]
    public void LevelsFromElevenBringWhatTheySayOnceTheirGoodsAreIn()
    {
        var (game, company, map) = Estate(30);
        var moily = map[(0, 0)];
        game.SetLevel(moily, 10);
        game.Pass(10);
        var before = Income(game, company);
        var preview = game.World.UpgradePreview(company, moily, 4);

        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, 4).Success);
        Assert.IsTrue(game.World.OrderAll(company.Id).Success);
        game.Pass(5);

        Assert.AreEqual(14, moily.Level);
        Kept(preview.Gain, Income(game, company) - before, "levels 11 to 14");
    }

    [TestMethod]
    public void ATradingPostLevelBringsWhatItSays()
    {
        var (game, company, map) = Estate(30);
        foreach (var moily in map.Values.Where(t => t.Type == "windMoily"))
        {
            game.SetLevel(moily, 10);
        }
        game.Pass(10);
        Assert.IsGreaterThan(game.World.HubOrder(company, "energy"), game.World.SellingNow(company)["energy"], "more energy than its Hub Order");
        var post = map.Values.Single(t => t.Type == "tradingPost");
        var before = Income(game, company);
        var preview = game.World.UpgradePreview(company, post, 2);
        Assert.IsGreaterThan(1, preview.Gain, "a flooded post: its levels raise what the market pays");

        game.UpgradeNow(company, [post.Id], 2);
        game.Pass(5);

        Kept(preview.Gain, Income(game, company) - before, "two trading post levels");
    }

    [TestMethod]
    public void GrowBringsWhatItSays()
    {
        var (game, company, map) = Estate(30, factory: true);
        foreach (var moily in map.Values.Where(t => t.Type == "windMoily").Take(10))
        {
            game.SetLevel(moily, 6);
        }
        game.Pass(10);
        var before = Income(game, company);
        var plan = game.World.PlanGrowth(company);
        Assert.IsNotEmpty(plan.Parts);

        Assert.IsTrue(game.World.GrowEverything(company.Id).Success);
        game.World.OrderAll(company.Id);
        game.Pass(5);

        Kept(plan.Gain, Income(game, company) - before, $"Grow ({plan.Parts.Count} parts)");
    }

    [TestMethod]
    public void ANextStepBringsWhatItSays()
    {
        var (game, company, map) = Estate(4, factory: true);
        var step = game.World.NextSteps(company, 6).FirstOrDefault(t => t.Action == AdvisorAction.Upgrade && t.Building != null && t.Levels > 0);
        Assert.IsNotNull(step, "an upgrade among the next steps");
        var before = Income(game, company);

        game.UpgradeNow(company, [step.Building!.Value], step.Levels);
        game.Pass(5);

        Kept(step.GainPerMinute, Income(game, company) - before, step.Title);
    }

    [TestMethod]
    public void ANextStepOnABigNetworkSaysWhatItBrings()
    {
        // Ranked by an estimate (40 moilies and a factory: a big network), said as solved
        var (game, company, map) = Estate(40, factory: true);
        var step = game.World.NextSteps(company, 6).FirstOrDefault(t => t.Action == AdvisorAction.Upgrade && t.Building != null && t.Levels > 0);
        Assert.IsNotNull(step, "an upgrade among the next steps");
        StringAssert.DoesNotMatch(step.Reason, new System.Text.RegularExpressions.Regex("about"), step.Reason);
        var before = Income(game, company);

        game.UpgradeNow(company, [step.Building!.Value], step.Levels);
        game.Pass(5);

        Kept(step.GainPerMinute, Income(game, company) - before, step.Title);
    }

    [TestMethod]
    public void AResearchTierBringsWhatItSays()
    {
        // The Iron track's first tier: its mines and factories work 25% faster
        var (game, company, map) = Estate(4, factory: true);
        game.Build(company, "lab");
        game.Pass(2);
        var track = game.World.Content.TrackList.Single(t => t.Id == "iron");
        var said = game.World.TierGain(company, track);
        Assert.IsGreaterThan(0.5, said);
        var before = Income(game, company);

        Assert.IsTrue(game.World.StartResearch(company.Id, "iron").Success);
        game.Pass(15);

        Assert.AreEqual(1, game.World.TrackTier(company, "iron"));
        Kept(said, Income(game, company) - before, "the Iron track's first tier");
    }

    [TestMethod]
    public void AYardBuildingsLevelBringsWhatItSays()
    {
        // Ana's Smelter runs on Ben's coal through the Yard: a level takes more of it, and the Yard's next round brings it
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", gold: 1_000_000, level: 20);
        var ben = game.NewCompany("Ben", gold: 1_000_000, level: 20);
        game.Pass(1);
        var pit = game.Build(ben, "coalPit", game.Seed(ben, "coal"));
        game.Connect(ben, game.Build(ben, "windMoily"), pit);
        game.Connect(ben, pit, game.TradingPost(ben));
        game.UpgradeNow(ben, [pit.Id], 6);
        var smelter = game.Build(ana, "smelter");
        game.Connect(ana, game.Build(ana, "windMoily"), smelter);
        var mine = game.Build(ana, "copperMine", game.Seed(ana, "copperOre"));
        game.UpgradeNow(ana, [mine.Id], 4);
        game.Connect(ana, mine, smelter);
        game.Connect(ana, smelter, game.TradingPost(ana));
        game.ConnectToGate(ana, smelter);
        game.Pass(10);
        Assert.IsGreaterThan(0.9, game.World.NetworkOf(smelter.Id)!.Solution.Rate(smelter.Id), "it runs on Ben's coal");
        var before = Income(game, ana);
        var preview = game.World.UpgradePreview(ana, smelter, 2);

        game.UpgradeNow(ana, [smelter.Id], 2);
        game.Pass(5);

        Kept(preview.Gain, Income(game, ana) - before, "the Smelter's two levels");
    }
}
