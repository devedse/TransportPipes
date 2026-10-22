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
    public void TheFoundationBuysEnergyFromTheStationAtTheHubsFullPrice()
    {
        var (game, ana) = Producer();
        var gold = ana.Gold;
        game.Pass(3);
        var project = ProjectOf(game, ana);
        Assert.AreEqual(ProjectStage.Foundation, project.Stage);
        Assert.AreEqual(900, project.Basket["energy"], "F4: 900 energy, a quarter of an hour of a few kits");
        Assert.HasCount(1, project.Basket);

        game.Pass(30);

        Assert.IsTrue(project.Finished.ContainsKey(ProjectStage.Foundation), "it stands within half an hour on three moilies");
        Assert.IsGreaterThan(gold, ana.Gold, "and pays for what it took");
        Assert.IsTrue(project.Credit.ContainsKey(ana.Id) || project.Stage == ProjectStage.WaterTower);
    }

    [TestMethod]
    public void TheFoundationStandsInMinutesOnStarterKits()
    {
        // Run 1: the Foundation stood on day 3. A newcomer's kit alone (three moilies piped to the gate) and the Houses
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.TenantsEnabled = true;
        game.World.ProjectsEnabled = true;
        game.World.OpenNextDistrict();
        var mia = game.NewCompany("Mia", slot: 3);
        Assert.IsTrue(mia.KitBuildings.Count > 0, "a House left a kit");

        game.Pass(30);

        Assert.IsTrue(ProjectOf(game, mia).Finished.ContainsKey(ProjectStage.Foundation), "by 20:30 on the first evening");
    }

    [TestMethod]
    public void NoStageTail()
    {
        // Run 1: 150 ore took 18 hours, the project taking an ever smaller share of what was left
        var (game, ana) = Producer();
        game.Pass(3);
        var project = ProjectOf(game, ana);
        project.Basket = new Dictionary<string, double> { ["ironOre"] = 150 };
        project.Delivered = new Dictionary<string, double>();
        project.Stage = ProjectStage.WaterTower;
        // The keystone takes what comes (no allowance), so only the tail could hold it up
        project.Keystone = "ironOre";

        game.Pass(20);

        // 150 ore at 12 a minute is 12.5 minutes, plus a round to match and one to deliver
        Assert.IsTrue(project.Finished.ContainsKey(ProjectStage.WaterTower), $"150 ore within 20 minutes of one mine (delivered {project.Delivered.GetValueOrDefault("ironOre"):F1})");
    }

    [TestMethod]
    public void EveryRecipeGoodIsMadeOrMakeable()
    {
        var (game, ana) = Producer(level: 5);
        game.Pass(3);
        var station = game.EstateOf(ana).Station;
        game.World.FinishStageNow(station);
        var project = game.World.ProjectOf(station);

        Assert.AreEqual(ProjectStage.WaterTower, project.Stage);
        Assert.IsTrue(project.Basket.Count >= 2, string.Join(", ", project.Basket.Keys));
        foreach (var good in project.Basket.Keys)
        {
            var made = game.World.BuildingsOf(ana.Id).Any(t => game.World.Content.Building(t.Type).Out.ContainsKey(good));
            Assert.IsTrue(made || game.World.MakersOf(project, good).Count > 0, $"{good}: made here, or makeable with one building by Level 5");
        }
        if (project.Keystone is { } keystone)
        {
            Assert.IsTrue(game.World.MakersOf(project, keystone).Any(t => t.Maker.Id == ana.Id), "the keystone names who can make it");
        }
    }

    [TestMethod]
    public void MeasuredIncomeFollowsPaidGold()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana");
        foreach (var _ in Enumerable.Range(0, 4))
        {
            game.Connect(ana, game.Build(ana, "windMoily"), game.TradingPost(ana));
        }
        game.Pass(10);
        var gold = ana.Gold;
        game.Pass(24 * 60);
        var rate = (ana.Gold - gold) / (24 * 60);

        Assert.AreEqual(rate, game.World.MeasuredIncome(ana), rate * 0.1 + 1, "a day later I(c) is what it earns a minute");
    }

    [TestMethod]
    public void ABasketShrinksWhenACountedResidentStops()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var ana = game.NewCompany("Ana", level: 3);
        var ben = game.NewCompany("Ben", level: 3);
        foreach (var company in new[] { ana, ben })
        {
            foreach (var _ in Enumerable.Range(0, 3))
            {
                game.ConnectToGate(company, game.Build(company, "windMoily"));
            }
            company.MeasuredIncome = 100;
        }
        game.Pass(3);
        var station = game.EstateOf(ana).Station;
        var project = game.World.ProjectOf(station);
        project.DeliveredAt[ana.Id] = game.World.Now;
        project.DeliveredAt[ben.Id] = game.World.Now;
        game.World.FinishStageNow(station);
        var made = project.Basket.Where(t => t.Key != project.Keystone).ToDictionary();
        Assert.HasCount(2, project.Counted, "both residents sized it");
        var benShare = project.Counted[ben.Id] / project.Counted.Values.Sum();

        // Ben stops; Ana plays on
        for (var day = 0; day < 4; day++)
        {
            ana.LastSeenAt = game.World.Now;
            game.Pass(24 * 60);
        }

        Assert.IsFalse(project.Counted.ContainsKey(ben.Id) && project.Stage == ProjectStage.WaterTower, "his part came off");
        if (project.Stage == ProjectStage.WaterTower)
        {
            foreach (var (good, basket) in made)
            {
                // He takes his part of what was still missing; what came stays
                var delivered = project.Delivered.GetValueOrDefault(good);
                Assert.IsTrue(project.Basket[good] <= delivered + Math.Ceiling(Math.Max(0, basket - delivered) * (1 - benShare)) + 1e-6, good);
            }
        }
    }

    [TestMethod]
    public void AStageStandsOnlyByDelivery()
    {
        // Run 2: from day 12 the stages stood by themselves while everyone was away (the Foundry Hall with nothing
        // delivered, the Landmark with an empty basket): when the counted residents left, the baskets shrank to what
        // had come
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var ana = game.NewCompany("Ana", level: 3);
        var ben = game.NewCompany("Ben", level: 3);
        foreach (var company in new[] { ana, ben })
        {
            game.ConnectToGate(company, game.Build(company, "windMoily"));
            company.MeasuredIncome = 100;
        }
        game.Pass(3);
        var station = game.EstateOf(ana).Station;
        var project = game.World.ProjectOf(station);
        project.DeliveredAt[ana.Id] = game.World.Now;
        project.DeliveredAt[ben.Id] = game.World.Now;
        game.World.FinishStageNow(station);
        Assert.AreEqual(ProjectStage.WaterTower, project.Stage);
        // Goods neither of them makes, a third of each already in
        project.Basket = new Dictionary<string, double> { ["stone"] = 900, ["sand"] = 600 };
        project.Delivered = new Dictionary<string, double> { ["stone"] = 300, ["sand"] = 200 };
        project.Keystone = null;

        // Both stop playing for a week
        game.Pass(7 * 24 * 60);

        Assert.AreEqual(ProjectStage.WaterTower, project.Stage, "nobody delivered, so it doesn't stand");
        Assert.HasCount(1, project.Counted, "the last one counted stays counted");
        Assert.IsTrue(project.Basket["stone"] > project.Delivered["stone"] && project.Basket["sand"] > project.Delivered["sand"], "something is still missing of each");
        Assert.IsTrue(project.Basket["stone"] >= 300 + 600 * 0.4, $"the one who left took only their part of the missing stone: {project.Basket["stone"]}");
    }

    [TestMethod]
    public void AKeystoneNobodyBringsIsPassedOverAfterTwoDays()
    {
        // Run 2: Capital NW's keystone was coal, on the land of a resident who never built a Coal Pit; it sat at 0 for
        // 24 days. A keystone nobody brings any of for two days is passed over for another, or dropped.
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var ana = game.NewCompany("Ana", level: 3);
        var ben = game.NewCompany("Ben", level: 3);
        foreach (var company in new[] { ana, ben })
        {
            game.ConnectToGate(company, game.Build(company, "windMoily"));
            company.MeasuredIncome = 100;
        }
        game.Pass(3);
        var station = game.EstateOf(ana).Station;
        var project = game.World.ProjectOf(station);
        project.DeliveredAt[ana.Id] = game.World.Now;
        project.DeliveredAt[ben.Id] = game.World.Now;
        game.World.FinishStageNow(station);
        project.Basket = new Dictionary<string, double> { ["energy"] = 1_000_000, ["coal"] = 500 };
        project.Delivered = new Dictionary<string, double>();
        project.Keystone = "coal";
        project.KeystoneSince = game.World.Now;

        // Both play every day; nobody brings coal
        for (var day = 0; day < 3; day++)
        {
            ana.LastSeenAt = ben.LastSeenAt = game.World.Now;
            game.Pass(24 * 60);
        }

        Assert.AreNotEqual("coal", project.Keystone, "passed over");
        Assert.IsFalse(project.Basket.ContainsKey("coal"), "and out of the basket");
        CollectionAssert.Contains(project.PassedKeystones.ToList(), "coal", "and not chosen again this stage");
        Assert.AreEqual(ProjectStage.WaterTower, project.Stage, "the energy still has to come");
    }

    [TestMethod]
    public void EveryoneWithSpareHelpsAtOnce()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var ana = game.NewCompany("Ana");
        var ben = game.NewCompany("Ben");
        foreach (var company in new[] { ana, ben })
        {
            game.ConnectToGate(company, game.Build(company, "windMoily"));
        }
        game.Pass(10);

        var project = ProjectOf(game, ana);
        Assert.IsGreaterThan(0, project.Credit.GetValueOrDefault(ana.Id));
        Assert.IsGreaterThan(0, project.Credit.GetValueOrDefault(ben.Id), "not first come, first served: the ring has both colours");
    }

    [TestMethod]
    public void AFinishedStageLiftsEverySeatAndTheNextAsksForAKeystone()
    {
        var (game, ana) = Producer(level: 5);
        game.Pass(3);
        var station = game.EstateOf(ana).Station;
        var moily = game.World.BuildingsOf(ana.Id).First(t => t.Type == "windMoily");
        var before = game.World.ToFlowNode(moily)!.Out["energy"] * game.World.ToFlowNode(moily)!.MaxRate;

        game.World.FinishStageNow(station);
        var project = game.World.ProjectOf(station);

        Assert.AreEqual(ProjectStage.WaterTower, project.Stage);
        Assert.HasCount(3, project.Basket, "two goods the station makes and a keystone");
        Assert.IsNotNull(project.Keystone, "one that nobody here makes yet");
        Assert.IsTrue(project.Basket.ContainsKey(project.Keystone!));
        var after = game.World.ToFlowNode(moily)!.Out["energy"] * game.World.ToFlowNode(moily)!.MaxRate;
        Assert.AreEqual(before * 1.03, after, before * 1e-6, "the Foundation: +3% production for every seat");
        // Sized by what the station makes (F4): at most 1.5 × 4 hours of the delivering resident's measured income, besides the keystone
        var made = project.Basket.Where(t => t.Key != project.Keystone).Sum(t => t.Value * game.World.BasePrice(t.Key));
        Assert.IsTrue(made <= 1.5 * 4 * 60 * game.World.MeasuredIncome(ana) + 10, $"{made:F0} against {game.World.MeasuredIncome(ana):F0} a minute");
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

        // Two hours of measured income a day at most (a newcomer starts with one day's worth), so nobody carries it alone
        var day = 2 * 60 * game.World.MeasuredIncome(ana);
        Assert.IsLessThanOrEqualTo(2 * day * 1.01 + 1, project.Credit.GetValueOrDefault(ana.Id));
        Assert.IsGreaterThan(0, project.Credit.GetValueOrDefault(ana.Id));
    }

    [TestMethod]
    public void TheCardTellsThePaceAndWhenYourShareIsFull()
    {
        var (game, ana) = Producer(level: 5);
        game.Pass(3);
        var station = game.EstateOf(ana).Station;
        game.World.FinishStageNow(station);
        var project = game.World.ProjectOf(station);
        // The keystone takes what comes: one mine fills 600 ore in about 50 minutes
        project.Basket = new Dictionary<string, double> { ["ironOre"] = 600 };
        project.Keystone = "ironOre";
        game.Pass(3);

        var card = game.World.ProjectCardFor(project, ana);
        Assert.IsNotNull(card.MinutesLeft);
        Assert.AreEqual(600 / 12.0, card.MinutesLeft.Value, 10, "at this pace: what's left over what comes a minute");
        CollectionAssert.AreEqual(new[] { "ironOre" }, card.GoingIn.ToArray(), "your ore goes in");
        Assert.IsFalse(card.ShareCapped, "the keystone takes what comes");
        Assert.AreEqual(0.03, card.Bonus, "the Water Tower: +3% for every seat");
        Assert.AreEqual(0.03 * game.World.MeasuredIncome(ana), card.YourGain, 1e-9);

        // A made good: Ana's energy is more than two hours of her output a day spread over the day
        project.Basket = new Dictionary<string, double> { ["energy"] = 1_000_000 };
        project.Keystone = null;
        game.Pass(3);

        card = game.World.ProjectCardFor(project, ana);
        Assert.IsTrue(card.ShareCapped, "the card says the daily share is full");
        Assert.AreEqual(2 * 60 * game.World.MeasuredIncome(ana), card.SharePerDay, 1e-6);
    }

    [TestMethod]
    public void AStageThatStartedWhileNobodyPlayedIsSizedWhenSomebodyIsBack()
    {
        // Run 1's day 7: Mia's station finished its Foundation while she was away, and the Water Tower's basket stayed empty for good
        var (game, ana) = Producer();
        game.Pass(3);
        var station = game.EstateOf(ana).Station;
        ana.LastSeenAt = game.World.Now - TimeSpan.FromDays(30);
        game.World.FinishStageNow(station);
        var project = game.World.ProjectOf(station);
        Assert.IsEmpty(project.Basket, "nobody counted when it started");

        ana.LastSeenAt = game.World.Now;
        // An hour after it started, at the next Yard round (they come at most 16 minutes apart)
        game.Pass(60 + 17);

        Assert.IsNotEmpty(project.Basket, "sized once she is back");
        Assert.AreEqual(ProjectStage.WaterTower, project.Stage);
    }

    [TestMethod]
    public void ASendToTheProjectTakesWhatWentToTheHubNotANeighboursTrade()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        game.World.ProjectsEnabled = true;
        var a = game.NewCompany("A");
        var b = game.NewCompany("B", level: 2);
        var moily = game.Build(a, "windMoily");
        game.ConnectToGate(a, moily);
        game.Connect(a, moily, game.TradingPost(a));
        var mine = game.Build(b, "ironMine", game.Spot(b, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(b, mine);
        game.Connect(b, mine, game.TradingPost(b));
        game.Pass(3);
        double ToB() => game.World.YardTrades(game.EstateOf(b)).Where(t => !t.Selling && t.Partner == a.Id && t.Resource == "energy").Sum(t => t.PerMinute);
        var before = ToB();
        Assert.IsGreaterThan(0, before, "B buys A's energy");
        var project = ProjectOf(game, a);
        // A basket that would take all of A's energy
        project.Basket["energy"] = 1_000_000;

        Assert.IsTrue(game.World.SendGood(a.Id, "energy", null, null).Success);
        Assert.IsTrue(a.ProjectFirst.Contains("energy"), "a Send to the project is the project-first box");
        game.Pass(3);

        // Step 4: the Yard's order is the usual partners' running trade, then Sends: nobody's trade is cut by a Send
        Assert.AreEqual(before, ToB(), before * 0.02, "B keeps its energy");
        Assert.IsGreaterThan(0, project.Incoming.GetValueOrDefault(a.Id)?.GetValueOrDefault("energy") ?? 0, "the project gets the rest");
    }
}
