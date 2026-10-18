using TransportPipes.Game.Content;

namespace TransportPipes.Game.Tests;

[TestClass]
public class ContentFacts
{
    [TestMethod]
    public void TheEmbeddedContentLoadsAndIsValid()
    {
        var content = GameContent.LoadEmbedded();

        Assert.IsGreaterThanOrEqualTo(24, content.ResourceList.Count);
        Assert.IsGreaterThanOrEqualTo(60, content.BuildingList.Count);
        Assert.HasCount(5, content.PipeTiers);
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
    public void CompanyLevelsNeedMoreXpEachLevel()
    {
        var progression = GameContent.Default.Progression;

        Assert.AreEqual(0, progression.XpForLevel(1));
        Assert.AreEqual(100, progression.XpForLevel(2));
        for (var level = 2; level < progression.MaxCompanyLevel; level++)
        {
            Assert.IsGreaterThan(progression.XpForLevel(level), progression.XpForLevel(level + 1));
        }
        Assert.AreEqual(1, progression.LevelForXp(99));
        Assert.AreEqual(2, progression.LevelForXp(100));
        Assert.AreEqual(progression.MaxCompanyLevel, progression.LevelForXp(double.MaxValue));
    }

    [TestMethod]
    public void MilestonesDoubleTheOutput()
    {
        var progression = GameContent.Default.Progression;

        Assert.AreEqual(1, progression.OutputMultiplier(1), 1e-9);
        Assert.AreEqual(Math.Pow(1.1, 8), progression.OutputMultiplier(9), 1e-9);
        Assert.AreEqual(Math.Pow(1.1, 9) * 2, progression.OutputMultiplier(10), 1e-9);
        Assert.AreEqual(Math.Pow(1.1, 24) * 4, progression.OutputMultiplier(25), 1e-9);
    }

    [TestMethod]
    public void ParcelsGetMoreExpensive()
    {
        var progression = GameContent.Default.Progression;

        Assert.HasCount(12, progression.ParcelOrder);
        Assert.HasCount(12, progression.ParcelLevels);
        Assert.AreEqual(5000, progression.ParcelCost(0));
        Assert.AreEqual(10000, progression.ParcelCost(1));
        CollectionAssert.AreEquivalent(Enumerable.Range(0, 16).ToList(), progression.StartParcels.Concat(progression.ParcelOrder).ToList());
    }
}
