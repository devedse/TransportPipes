using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class ProjectFacts
{
    /// <summary>A company whose Wind Moilies and Iron Mine are piped to its Yard gate (nobody else in its station)</summary>
    private static (TestGame Game, Company Company) Producer(int level = 3)
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var company = game.NewCompany("Ana", level: level);
        foreach (var _ in Enumerable.Range(0, 3))
        {
            game.ConnectToGate(company, game.Build(company, "windMoily"));
        }
        var mine = game.Build(company, "ironMine", game.Spot(company, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(company, mine);
        game.World.SolveNetworks();
        return (game, company);
    }

    private static StationProject ProjectOf(TestGame game, Company company) => game.World.ProjectOf(game.EstateOf(company).Station);

    [TestMethod]
    public void TheFoundationBuysEnergyAndOreFromTheStationAtTheHubsFullPrice()
    {
        var (game, ana) = Producer();
        var gold = ana.Gold;
        game.Pass(3);
        var project = ProjectOf(game, ana);
        Assert.AreEqual(ProjectStage.Foundation, project.Stage);
        CollectionAssert.AreEquivalent(new[] { "energy", "ironOre" }, project.Basket.Keys.ToArray());

        game.Pass(30);

        Assert.IsGreaterThan(0, project.Delivered.GetValueOrDefault("energy") + project.Delivered.GetValueOrDefault("ironOre") + (project.Finished.ContainsKey(ProjectStage.Foundation) ? 1 : 0),
            "the project takes what nobody else needs");
        Assert.IsGreaterThan(gold, ana.Gold, "and pays for it");
        Assert.IsTrue(project.Credit.ContainsKey(ana.Id) || project.Finished.ContainsKey(ProjectStage.Foundation));
    }

    [TestMethod]
    public void AFinishedStageLiftsEverySeatAndTheNextAsksForAKeystone()
    {
        var (game, ana) = Producer();
        game.Pass(3);
        var station = game.EstateOf(ana).Station;
        var moily = game.World.BuildingsOf(ana.Id).First(t => t.Type == "windMoily");
        var before = game.World.ToFlowNode(moily)!.Out["energy"] * game.World.ToFlowNode(moily)!.MaxRate;

        game.World.FinishStageNow(station);
        var project = game.World.ProjectOf(station);

        Assert.AreEqual(ProjectStage.WaterTower, project.Stage);
        Assert.HasCount(3, project.Basket, "three goods");
        Assert.IsNotNull(project.Keystone, "one that nobody here makes");
        Assert.IsTrue(project.Basket.ContainsKey(project.Keystone!));
        var after = game.World.ToFlowNode(moily)!.Out["energy"] * game.World.ToFlowNode(moily)!.MaxRate;
        Assert.AreEqual(before * 1.03, after, before * 1e-6, "the Foundation: +3% production for every seat");
        // Sized by the one delivering resident: six hours of T(L)
        var worth = project.Basket.Sum(t => t.Value * game.World.BasePrice(t.Key));
        Assert.AreEqual(6 * 60 * game.World.TypicalIncome(ana.Level), worth, worth * 0.01 + 10);
    }

    [TestMethod]
    public void NobodyDeliversMoreThanTwoHoursOfTheirOutputADay()
    {
        var (game, ana) = Producer(level: 5);
        game.Pass(3);
        var station = game.EstateOf(ana).Station;
        game.World.FinishStageNow(station);
        var project = game.World.ProjectOf(station);
        // A basket Ana's land can fill: energy and ore only
        project.Basket = new Dictionary<string, double> { ["energy"] = 1_000_000, ["ironOre"] = 1_000_000 };
        project.Keystone = null;

        game.Pass(24 * 60);

        // Two hours of T(L) a day at most (a newcomer starts with one day's worth), so nobody carries it alone
        var day = 2 * 60 * game.World.TypicalIncome(ana.Level);
        Assert.IsLessThanOrEqualTo(2 * day * 1.01 + 1, project.Credit.GetValueOrDefault(ana.Id));
        Assert.IsGreaterThan(0, project.Credit.GetValueOrDefault(ana.Id));
    }

    [TestMethod]
    public void ProjectFirstPutsTheProjectBeforeTheNeighbours()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var a = game.NewCompany("A");
        var b = game.NewCompany("B", level: 2);
        var moily = game.Build(a, "windMoily");
        game.ConnectToGate(a, moily);
        var mine = game.Build(b, "ironMine", game.Spot(b, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(b, mine);
        game.Connect(b, mine, game.TradingPost(b));
        game.Pass(3);
        double ToB() => game.World.YardTrades(game.EstateOf(b)).Where(t => !t.Selling && t.Partner == a.Id && t.Resource == "energy").Sum(t => t.PerMinute);
        var before = ToB();
        Assert.IsGreaterThan(0, before, "the neighbour first, normally");
        var project = ProjectOf(game, a);
        // A basket that takes all of A's energy
        project.Basket["energy"] = 1_000_000;

        Assert.IsTrue(game.World.SetProjectFirst(a.Id, "energy", true).Success);
        game.Pass(3);

        Assert.IsLessThan(before - 1e-6, ToB(), "project first: the Foundation gets A's energy before B");
        Assert.IsGreaterThan(0, project.Incoming.GetValueOrDefault(a.Id)?.GetValueOrDefault("energy") ?? 0);
    }
}
