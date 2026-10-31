using TransportPipes.Game.State;
using TransportPipes.Game.World;

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

    /// <summary>What an action took (or, less than zero, gave back) is the price shown</summary>
    private static void Priced(Company company, double shown, Func<CommandResult> act, string what, double within = 0.01)
    {
        var gold = company.Gold;
        var result = act();
        Assert.IsTrue(result.Success, $"{what}: {result.Error}");
        Assert.AreEqual(shown, gold - company.Gold, within, $"{what}: shown {shown:0.00} g, took {gold - company.Gold:0.00} g");
    }

    [TestMethod]
    public void EveryPriceShownIsTheGoldTaken()
    {
        // With the Workshop wonder's discount (buildings, levels and parcels 3% cheaper: the build menu showed the full price)
        var (game, company, map) = Estate(4, factory: true);
        var world = game.World;
        company.Gold = 1e10;
        Assert.IsTrue(world.ChooseMonument(company.Id, 1, "workshop").Success);
        Assert.IsTrue(world.PayMonument(company.Id, 1, 100_000_000).Success);
        Assert.AreEqual(0.97, world.CostFactor(company), 1e-9);
        var (moily, other, spare) = (map[(0, 0)], map[(1, 0)], map[(3, 0)]);

        Priced(company, world.BuildCost(company, world.Content.Building("windMoily")), () => world.PlaceBuilding(company.Id, "windMoily", game.Spot(company, "windMoily")), "a Wind Moily");
        Priced(company, world.UpgradeCost(moily, 1), () => world.Upgrade(company.Id, moily.Id, 1), "+1 level");
        Priced(company, world.UpgradeCost(moily, 10), () => world.Upgrade(company.Id, moily.Id, 10), "+10 levels");
        Priced(company, world.OrderCost(company, moily.Id) ?? 0, () => world.OrderRest(company.Id, moily.Id), "its levels' goods, ordered", within: 1);
        company.Gold = world.UpgradeCost(other, 3) + 1;
        var affordable = world.AffordableLevels(company, other);
        Assert.AreEqual(3, affordable);
        Priced(company, world.UpgradeCost(other, affordable), () => world.Upgrade(company.Id, other.Id, affordable), "the levels the gold buys");
        company.Gold = 1e10;
        Priced(company, -world.DemolishRefund(spare), () => world.Demolish(company.Id, spare.Id), "a Wind Moily demolished");
        spare = map[(2, 0)];
        spare.Damaged = true;
        Priced(company, world.RepairCost(spare), () => world.Repair(company.Id, spare.Id), "a repair");

        var parcel = world.NextSteps(company, 8).Single(t => t.Kind == AdvisorKind.Parcel);
        Priced(company, world.NextParcel(company).Cost, () => world.BuyParcel(company.Id, parcel.Parcel!.Value), "the next parcel");

        game.Build(company, "lab");
        var track = world.Content.TrackList.Single(t => t.Id == "iron");
        Priced(company, world.TierCost(company, track, world.NextTier(company, track)!.Value).Total, () => world.StartResearch(company.Id, "iron"), "the Iron track's tier");

        // A pipe over water: a bridge, three times the price, and half of that back
        var water = Enumerable.Range(0, 30).SelectMany(x => Enumerable.Range(4, 20).Select(y => game.Local(company, x, y)))
            .First(t => !world.IsOccupied(t) && world.Tile(t).Terrain is not (TerrainType.Mountain or TerrainType.Water));
        world.LoadTerrainOverride(water, TerrainType.Water);
        var pipe = world.Content.PipeTiers[0].Gold;
        Priced(company, pipe * 3, () => world.PlacePipes(company.Id, [water]), "a bridge");
        Priced(company, -pipe * 1.5, () => world.RemovePipes(company.Id, [water]), "the bridge taken away");
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
