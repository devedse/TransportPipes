using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class ManagerFacts
{
    [TestMethod]
    public void ManagersNeedAFreeSlot()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 9);
        StringAssert.Contains(game.World.HireManager(company.Id, GameWorld.EngineerJo).Error, "No free manager slot");

        company.Level = 10;
        Assert.AreEqual(1, game.World.ManagerSlots(company));
        Assert.IsTrue(game.World.HireManager(company.Id, GameWorld.EngineerJo).Success);
        Assert.AreEqual(1_000_000 - 5000, company.Gold);
        StringAssert.Contains(game.World.HireManager(company.Id, GameWorld.PlannerSam).Error, "No free manager slot");

        game.Build(company, "engineerWorkshop");
        Assert.AreEqual(2, game.World.ManagerSlots(company), "the Engineer's Workshop adds a slot");
        Assert.IsTrue(game.World.HireManager(company.Id, GameWorld.PlannerSam).Success);
        Assert.IsTrue(game.World.DismissManager(company.Id, GameWorld.EngineerJo).Success);
        Assert.IsFalse(company.Managers.Contains(GameWorld.EngineerJo));
    }

    [TestMethod]
    public void EngineerJoRepairsStormDamageRightAwayAndAddsWind()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 10);
        var map = game.Map(company, game.Local(company, 1, 1), " " + new string('W', 40) + "\nO" + new string('+', 40));
        double Stored() => game.World.NetworkOf(map[(0, 1)].Id)!.Solution.Flows.Where(t => t.Kind == FlowKind.ToStorage).Sum(t => t.Delivered);
        var before = Stored();

        game.World.HireManager(company.Id, GameWorld.EngineerJo);
        game.World.SolveNetworks();
        Assert.AreEqual(before * 1.05, Stored(), 1e-6);

        game.World.StartEventNow(EventKind.Storm, company.District, TimeSpan.FromHours(1));
        Assert.IsFalse(map.Values.Any(t => t.Damaged));
        Assert.IsGreaterThan(0, company.Stats["repairs"]);
    }

    [TestMethod]
    public void AccountantBeaSellsWhatAFullStorageCantTake()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 10, gold: 100);
        var storage = game.Map(company, game.Local(company, 1, 1), "O").Values.Single();
        storage.Contents["bricks"] = 5000;
        game.World.HireManager(company.Id, GameWorld.EngineerJo);
        company.Managers.Clear();
        company.Managers.Add(GameWorld.AccountantBea);

        game.Pass(16);

        Assert.AreEqual(4750, storage.Contents["bricks"], 1e-6, "5% of the capacity");
        Assert.IsGreaterThan(100 + 250 * 4, company.Gold);
    }

    [TestMethod]
    public void ForemanRickUpgradesWhatPaysBackQuicklyAndCheaper()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 10);
        var map = game.Map(company, game.Local(company, 1, 1), " W\nT+");
        var moily = map[(1, 0)];
        var cost = game.World.UpgradeCost(moily, 1);
        company.Managers.Add(GameWorld.ForemanRick);
        Assert.AreEqual(cost * 0.95, game.World.UpgradeCost(moily, 1), 0.01);

        game.Pass(16);

        Assert.AreEqual(2, moily.Level, "a cheap first level pays back within two hours");
    }

    [TestMethod]
    public void PlannerSamStartsTheNextResearch()
    {
        var game = new TestGame();
        var company = game.NewCompany(level: 10);
        company.Managers.Add(GameWorld.PlannerSam);
        company.ResearchPoints = 1000;
        Assert.IsTrue(game.World.StartResearch(company.Id, "betterBearings").Success);
        Assert.AreEqual(game.Time.AddMinutes(5 * 0.9), company.ResearchEndsAt, "10% faster");

        game.Pass(5);

        Assert.IsTrue(company.Researched.Contains("betterBearings"));
        Assert.IsNotNull(company.CurrentResearch, "the next one started");
    }

    [TestMethod]
    public void AchievementsAndCollectionsAreRewarded()
    {
        var game = new TestGame(achievements: true);
        var company = game.NewCompany(gold: 1_000_000);
        game.Pass(2);
        Assert.IsFalse(company.Achievements.Contains("firstSteps"));

        game.Build(company, "windMoily");
        game.Pass(2);

        Assert.IsTrue(company.Achievements.Contains("firstSteps"));
        Assert.IsGreaterThan(0, company.Xp);

        // A collection: every building on its list at level 10
        var collection = game.World.Content.Collections.First(t => t.Id == "powerOfEra1");
        var decoration = game.World.Content.Building("goldenMoily");
        Assert.IsFalse(game.World.IsUnlocked(company, decoration, out var reason));
        StringAssert.Contains(reason, "collection");
        var x = 1;
        foreach (var type in collection.Buildings)
        {
            // Placed without the rules (a Hydro Wheel needs a shore)
            var pos = game.Local(company, x, 28);
            game.World.LoadBuilding(new Building { Id = game.World.NextBuildingId, Type = type, Owner = company.Id, X = pos.X, Y = pos.Y, Level = 10 });
            x += 3;
        }
        game.Pass(2);

        Assert.IsTrue(company.Achievements.Contains(GameWorld.CollectionKey("powerOfEra1")));
        Assert.IsTrue(company.Titles.Contains("Power Collector"));
        Assert.IsTrue(game.World.IsUnlocked(company, decoration, out _));
    }
}
