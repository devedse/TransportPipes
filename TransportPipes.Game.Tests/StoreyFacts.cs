using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>
/// The Landmark's storeys (after run 4: once Capital SE's Landmark stood on day 17 the crew had nothing to build
/// together; "come back tomorrow" was yes for 2 of 6): one after another without end, each a goods part by the Yard and
/// a purse anyone gives hours of their income to; each stands for +10% on the residents' pipes and a plaque in hours
/// </summary>
[TestClass]
public class StoreyFacts
{
    /// <summary>Ana and Ben in one station whose Landmark stands now: its first storey opens</summary>
    private static (TestGame Game, Company Ana, Company Ben, StationProject Project) Landmark()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var ana = game.NewCompany("Ana", gold: 100_000_000, level: 20);
        var ben = game.NewCompany("Ben", gold: 100_000_000, level: 20);
        game.Pass(1);
        ana.MeasuredIncome = 2_000;
        ben.MeasuredIncome = 1_000;
        var station = game.EstateOf(ana).Station;
        var project = game.World.ProjectOf(station);
        project.Stage = ProjectStage.Landmark;
        project.Basket = new Dictionary<string, double> { ["energy"] = 10 };
        project.Delivered = new Dictionary<string, double> { ["energy"] = 10 };
        game.World.TakeChanges();
        game.World.FinishStageNow(station);
        return (game, ana, ben, project);
    }

    [TestMethod]
    public void TheFirstStoreyOpensWhenTheLandmarkStandsWithAPurseOfADaysIncome()
    {
        var (game, ana, _, project) = Landmark();

        Assert.AreEqual(ProjectStage.Storey, project.Stage);
        Assert.AreEqual(0, project.Storeys);
        Assert.IsNotEmpty(project.Basket, "its goods part");
        Assert.AreEqual(24 * 60 * 3_000, project.Purse, 24 * 60 * 3_000 * 0.02, "a day of the station's income");
        Assert.IsTrue(game.World.TakeChanges().Events.Any(t => t.Company == ana.Id && t.Text.StartsWith("Your station's Landmark opens storey 1")));
    }

    [TestMethod]
    public void GoldAndGoodsMakeAStoreyStandForTenPercentOnThePipesAndAPlaqueInHours()
    {
        var (game, ana, ben, project) = Landmark();
        var station = game.EstateOf(ana).Station;
        StringAssert.Contains(game.World.GiveToStorey(ana.Id, 2).Error, "an hour, six hours or a day");
        var gold = ben.Gold;

        var given = game.World.GiveToStorey(ben.Id, 6);

        Assert.IsTrue(given.Success, given.Error);
        Assert.AreEqual(6 * 60 * game.World.MeasuredIncome(ben), gold - ben.Gold, 1, "six hours of his own income");
        StringAssert.StartsWith(given.Note, "Given ");
        // The goods come by the Yard; then Ana gives the rest of the purse
        foreach (var good in project.Basket.Keys.ToList())
        {
            project.Delivered[good] = project.Basket[good];
        }
        while (GameWorld.PurseLeft(project) >= 0.5)
        {
            Assert.IsTrue(game.World.GiveToStorey(ana.Id, 24).Success);
        }

        Assert.AreEqual(1, project.Storeys);
        Assert.AreEqual(ProjectStage.Done, project.Stage, "the next opens at midnight");
        Assert.AreEqual(game.World.LastTick.Date.AddDays(1), project.NextStoreyAt);
        Assert.AreEqual(GameWorld.StoreyPipeBonus, game.World.StoreyPipeBonusOf(ana.Id), 1e-9, "their pipes carry 10% more");
        var plaque = project.Plaques.Single();
        Assert.AreEqual(6, plaque.Hours[ben.Id], 0.1, "Ben's six hours");
        Assert.IsGreaterThan(20, plaque.Hours[ana.Id], "Ana's day and more");
        Assert.IsTrue(game.World.TakeChanges().Events.Any(t => t.Company == ben.Id && t.Text.StartsWith("Your station's Landmark rose to storey 1 (")));

        // At midnight the next opens, its purse ×1.2
        var purse = project.Purse;
        game.Pass((game.Time.Date.AddDays(1) - game.Time).TotalMinutes + 1);
        Assert.AreEqual(ProjectStage.Storey, project.Stage);
        Assert.AreEqual(purse * GameWorld.StoreyPurseGrowthFactor, project.Purse, purse * 0.1, "×1.2, on today's incomes");
        Assert.AreEqual(station, game.World.StationOf(project));
    }

    [TestMethod]
    public void ALandmarkThatStoodBeforeTheStoreysGetsItsFirstAtMidnight()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var ana = game.NewCompany("Ana", level: 20);
        game.Pass(1);
        var project = game.World.ProjectOf(game.EstateOf(ana).Station);
        project.Stage = ProjectStage.Done;
        project.Finished[ProjectStage.Landmark] = game.World.LastTick;

        game.Pass((game.Time.Date.AddDays(1) - game.Time).TotalMinutes + 1);

        Assert.AreEqual(ProjectStage.Storey, project.Stage);
    }
}
