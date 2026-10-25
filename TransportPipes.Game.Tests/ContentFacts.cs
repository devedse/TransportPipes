using TransportPipes.Game.Content;

namespace TransportPipes.Game.Tests;

[TestClass]
public class ContentFacts
{
    [TestMethod]
    public void EveryRecipeAddsAQuarterOfItsInputValue()
    {
        // Found by the economy designer after the playtest: three Era 2–4 recipes made less than they took, at base prices
        var content = GameContent.Default;
        double Value(IReadOnlyDictionary<string, double> goods) => goods.Sum(t => t.Value * content.Resources[t.Key].Price);
        foreach (var building in content.BuildingList.Where(t => t.In.Count > 0 && t.Out.Count > 0))
        {
            var input = Value(building.In);
            Assert.IsTrue(Value(building.Out) - input >= 0.25 * input - 1e-9, $"{building.Id}: {input:F0} in, {Value(building.Out):F0} out");
        }
    }

    [TestMethod]
    public void TheEmbeddedContentLoadsAndIsValid()
    {
        var content = GameContent.LoadEmbedded();

        Assert.IsGreaterThanOrEqualTo(24, content.ResourceList.Count);
        Assert.IsGreaterThanOrEqualTo(60, content.BuildingList.Count);
        Assert.HasCount(6, content.PipeTiers, "grey, copper, steel, composite, glass, superconductor");
        Assert.AreEqual(2000, content.Progression.StartingGold);
    }

    [TestMethod]
    public void BuildingsHaveTheirStats()
    {
        var content = GameContent.Default;

        var windMoily = content.Building("windMoily");
        Assert.AreEqual(30, windMoily.Out["energy"]);
        Assert.AreEqual(ProductionModifier.Wind, windMoily.Modifier);
        Assert.AreEqual("WindMoily", windMoily.Sprite);

        var factory = content.Building("smallFactory");
        Assert.AreEqual(20, factory.In["energy"]);
        Assert.AreEqual(12, factory.In["ironOre"]);
        Assert.AreEqual(6, factory.Out["ironIngot"]);

        var mine = content.Building("ironMine");
        Assert.AreEqual("ironOre", mine.Placement.Deposit);
        Assert.AreEqual(2, mine.Unlock.Level);

        Assert.AreEqual(BuildingKind.Storage, content.Building("warehouse").Kind);
        Assert.AreEqual(2, content.Building("warehouse").Size);
        Assert.IsFalse(content.Building("hq").Buildable);
        Assert.IsTrue(content.Building("valve").IsConductor);
    }

    [TestMethod]
    public void CompanyLevelsComeFromTheGoldEarned()
    {
        var progression = GameContent.Default.Progression;

        // 1.4 §14: 500 × 3.96^(L − 2) up to 10; 30M × 1.44^(L − 10) up to 20; 1.15B × 1.35^(L − 20) up to 25; 5.2B × 1.5^(L − 25)
        Assert.AreEqual(0, progression.EarnedForLevel(1));
        Assert.AreEqual(500, progression.EarnedForLevel(2));
        Assert.AreEqual(31_050, progression.EarnedForLevel(5), 1);
        Assert.AreEqual(30_000_000, progression.EarnedForLevel(10), 1);
        Assert.AreEqual(1_150_000_000, progression.EarnedForLevel(20), 1);
        Assert.AreEqual(5_200_000_000, progression.EarnedForLevel(25), 1);
        Assert.AreEqual(5_200_000_000 * 1.5 * 1.5, progression.EarnedForLevel(27), 1);
        for (var level = 2; level < progression.MaxCompanyLevel; level++)
        {
            Assert.IsGreaterThan(progression.EarnedForLevel(level), progression.EarnedForLevel(level + 1));
        }
        Assert.AreEqual(1, progression.LevelForEarned(499));
        Assert.AreEqual(2, progression.LevelForEarned(500));
        Assert.AreEqual(progression.MaxCompanyLevel, progression.LevelForEarned(double.MaxValue));
    }

    [TestMethod]
    public void EveryLevelAddsTenPercentUpToTwentyFive()
    {
        var progression = GameContent.Default.Progression;

        Assert.AreEqual(25, progression.MaxBuildingLevel);
        Assert.AreEqual(1, progression.OutputMultiplier(1), 1e-9);
        Assert.AreEqual(Math.Pow(1.1, 9), progression.OutputMultiplier(10), 1e-9, "the stars are looks only");
        Assert.AreEqual(9.85, progression.OutputMultiplier(25), 0.01, "9.85× at level 25");
        Assert.AreEqual(progression.OutputMultiplier(25), progression.OutputMultiplier(30), 1e-9);
        // A step costs what that copy cost × 1.15 for every level it has: 25 times at the last, 184 in all
        Assert.AreEqual(1000 * Math.Pow(1.15, 23), progression.UpgradeCost(1000, 24), 1);
        Assert.AreEqual(184, Enumerable.Range(1, 24).Sum(t => progression.UpgradeCost(1000, t)) / 1000, 1);
    }

    [TestMethod]
    public void FromLevelElevenAStepAlsoTakesGoodsOfItsBuildingsEra()
    {
        var progression = GameContent.Default.Progression;
        var prices = GameContent.Default.Resources;
        double Price(string good) => prices[good].Price;

        Assert.IsEmpty(progression.LevelGoodsFor(1, 10, 1000, Price), "up to level 10: gold only");
        var eleven = progression.LevelGoodsFor(1, 11, 1000, Price);
        CollectionAssert.AreEquivalent(new[] { "planks", "bricks" }, eleven.Keys.ToList());
        Assert.AreEqual(300, eleven.Sum(t => t.Value * Price(t.Key)), 10, "goods worth 30% of the step's gold");
        CollectionAssert.AreEquivalent(new[] { "planks", "bricks", "ironIngot", "glass", "copperIngot" }, progression.LevelGoodsFor(1, 25, 1000, Price).Keys.ToList());
        CollectionAssert.AreEquivalent(new[] { "steel", "concrete" }, progression.LevelGoodsFor(2, 12, 1000, Price).Keys.ToList(), "an Era-2 building: Era-2 goods");
    }

    [TestMethod]
    public void ParcelsGetMoreExpensive()
    {
        var progression = GameContent.Default.Progression;

        Assert.HasCount(12, progression.ParcelLevels, "the twelve parcels around the start area");
        CollectionAssert.AreEqual(new[] { 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27 }, progression.ParcelLevels.ToArray(), "a parcel at every second level from 5");
        Assert.AreEqual(20_000, progression.ParcelCost(0));
        Assert.AreEqual(50_000, progression.ParcelCost(1));
        Assert.AreEqual(477_000_000, progression.ParcelCost(11), 1_000_000, "the 16th: 477 million");
    }
}
