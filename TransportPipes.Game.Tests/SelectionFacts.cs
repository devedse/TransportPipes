using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class SelectionFacts
{
    [TestMethod]
    public void TheSelectionIsUpgradedTogether()
    {
        var game = new TestGame();
        var company = game.NewCompany(gold: 1_000_000);
        var moilies = Enumerable.Range(0, 4).Select(_ => game.Build(company, "windMoily")).ToList();
        moilies[0].Level = 5;
        var ids = moilies.Select(t => t.Id).Append(game.TradingPost(company).Id).ToList();

        var (levels, gold, error) = game.World.UpgradeMany(company.Id, ids, 1);
        Assert.IsNull(error);
        Assert.AreEqual(5, levels, "the four moilies and the trading post, one level each");
        CollectionAssert.AreEqual(new[] { 6, 2, 2, 2 }, moilies.Select(t => t.Level).ToArray());
        Assert.IsGreaterThan(0, gold);
    }

    [TestMethod]
    public void ABudgetBuysTheCheapestLevelsFirstSoTheyLevelOut()
    {
        var game = new TestGame();
        var company = game.NewCompany(gold: 1_000_000);
        var moilies = Enumerable.Range(0, 3).Select(_ => game.Build(company, "windMoily")).ToList();
        moilies[0].Level = 8;
        var budget = 50_000.0;
        var before = company.Gold;

        var (levels, gold, _) = game.World.UpgradeMany(company.Id, moilies.Select(t => t.Id).ToList(), 1, budget);

        Assert.IsLessThanOrEqualTo(budget + 1e-6, gold, "within the budget");
        Assert.AreEqual(before - gold, company.Gold, 1e-6);
        Assert.IsTrue(moilies[1].Level >= 7 && moilies[2].Level >= 7, "the low ones caught up first");
        Assert.IsLessThanOrEqualTo(1, Math.Abs(moilies[1].Level - moilies[2].Level), "evenly");
        Assert.IsGreaterThan(0, levels);
    }

    [TestMethod]
    public void OnlyOwnUpgradableBuildingsAreUpgraded()
    {
        var game = new TestGame();
        var company = game.NewCompany(gold: 1_000_000);
        var other = game.NewCompany("Other", gold: 1_000_000);
        var mine = game.Build(company, "windMoily");
        var theirs = game.Build(other, "windMoily");
        var hq = game.World.BuildingsOf(company.Id).Single(t => t.Type == "hq");

        game.World.UpgradeMany(company.Id, [mine.Id, theirs.Id, hq.Id], 3);

        Assert.AreEqual(4, mine.Level);
        Assert.AreEqual(1, theirs.Level);
        Assert.AreEqual(1, hq.Level, "the HQ grows with the Company Level");
    }

    [TestMethod]
    public void APaintedRowIsBuiltAsFarAsTheGoldGoes()
    {
        var game = new TestGame();
        var definition = game.World.Content.Building("solarPanel");
        var company = game.NewCompany(gold: 0);
        var spots = Enumerable.Range(0, 16).SelectMany(i => new[] { game.Local(company, 8 + i, 20), game.Local(company, 8 + i, 21) }).Where(t => game.World.CanPlace(company, definition, t).Success).Take(8).ToList();
        // Enough gold for three panels (each copy costs a little more)
        company.Gold = Enumerable.Range(0, 3).Sum(i => game.World.Content.Progression.BuildCost(definition, i)) + 1;

        Assert.HasCount(8, spots, "free spots in the start parcels");
        var (placed, gold, error) = game.World.PlaceBuildings(company.Id, "solarPanel", spots);

        Assert.AreEqual(3, placed);
        StringAssert.StartsWith(error, "You need");
        Assert.AreEqual(1, company.Gold, 1e-6);
        Assert.AreEqual(gold, Enumerable.Range(0, 3).Sum(i => game.World.Content.Progression.BuildCost(definition, i)), 1e-6);
        Assert.AreEqual(3, game.World.BuildingsOf(company.Id).Count(t => t.Type == "solarPanel"));
    }
}
