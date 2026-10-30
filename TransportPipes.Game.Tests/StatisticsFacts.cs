using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>Statistics (docs/design-notes/statistics.md): a sample of every company and town every 15 minutes, and key moments</summary>
[TestClass]
public class StatisticsFacts
{
    private static List<Moment> MomentsOf(TestGame game, Company company, string? kind = null) =>
        game.World.PendingMoments.Where(t => t.Company == company.Id && (kind == null || t.Kind == kind)).ToList();

    [TestMethod]
    public void EveryFifteenMinutesASampleHoldsWhatTheCompanyEarnsMakesAndHas()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");
        game.Connect(company, moily, game.TradingPost(company));
        company.LastSeenAt = game.Time.AddMinutes(5);

        game.Pass(60);

        var samples = game.World.PendingSamples.Where(t => t.Kind == StatSubject.Company && t.Subject == company.Id).ToList();
        CollectionAssert.AreEqual(new[] { 15, 30, 45, 60 }, samples.Select(t => (int)(t.At - TestGame.Start).TotalMinutes).ToArray());
        var last = samples[^1].Values;
        Assert.AreEqual(game.World.GoldPerMinute()[company.Id], last["income"], 0.1, "income: gold a minute now, the top bar's number");
        Assert.AreEqual(company.LifetimeEarnings, last["earned"], 1);
        Assert.AreEqual(last["earned"], last["earned:market"], 1, "all of it from the market");
        Assert.AreEqual(company.LifetimeEarnings, last["sold:energy"], 1, "for its energy");
        Assert.AreEqual(company.Stats["produced:energy"], last["made:energy"], 0.1);
        Assert.IsGreaterThan(samples[0].Values["earned"], last["earned"], "a counter grows");
        Assert.AreEqual(Math.Round(game.World.NetWorth(company)), last["worth"]);
        Assert.AreEqual(Math.Floor(company.Gold), last["gold"]);
        Assert.AreEqual(game.World.BuildingsOf(company.Id).Count(), last["buildings"]);
        Assert.AreEqual(game.EstateOf(company).UnlockedCount * WorldLayout.ParcelSize * WorldLayout.ParcelSize, last["land"]);
        Assert.AreEqual(15, samples[0].Values["played"], "seen in its first quarter hour");
        Assert.AreEqual(15, last["played"], "away since");

        var town = game.World.PendingSamples.Single(t => t.Kind == StatSubject.Town && t.At == samples[^1].At);
        Assert.AreEqual(company.District, GameWorld.TownOfKey(town.Subject));
        Assert.AreEqual(1, town.Values["players"]);
        Assert.AreEqual(1, town.Values["era"]);
        Assert.AreEqual(last["income"], town.Values["income"], 0.1, "its players' income together");
    }

    [TestMethod]
    public void SpendingIsCountedByKind()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1e9, level: 30);
        var gold = company.Gold;
        var parcel = Enumerable.Range(0, WorldLayout.ParcelCount).First(t => game.World.BuyParcel(company.Id, t).Success);
        var land = gold - company.Gold;
        game.Build(company, "lab");
        Assert.IsTrue(game.World.StartResearch(company.Id, "iron").Success);

        var sample = game.World.CompanySample(company);

        Assert.IsTrue(game.EstateOf(company).IsUnlocked(parcel));
        Assert.AreEqual(land, sample["spent:land"], 1);
        Assert.AreEqual(500, sample["spent:research"], 1, "the Iron track's first tier");
        Assert.AreEqual(gold - company.Gold, sample["spent"], 1, "and the Lab: buildings and levels are the rest");
    }

    [TestMethod]
    public void TheFirstUnitOfAGoodIsABigMomentOnce()
    {
        var game = new TestGame();
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");
        game.Connect(company, moily, game.TradingPost(company));

        game.Pass(30);
        game.Pass(30);

        var first = MomentsOf(game, company, "first").Single();
        Assert.AreEqual("First Energy", first.Text);
        Assert.AreEqual("energy", first.Ref);
        Assert.IsTrue(first.Big);
    }

    [TestMethod]
    public void LevelsLandAndResearchAreMoments()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1e9);

        game.ReachLevel(company, 6);
        game.Build(company, "lab");
        Enumerable.Range(0, WorldLayout.ParcelCount).First(t => game.World.BuyParcel(company.Id, t).Success);
        Assert.IsTrue(game.World.StartResearch(company.Id, "iron").Success);
        game.Pass(11);

        var levels = MomentsOf(game, company, "level");
        CollectionAssert.AreEqual(new[] { "Company Level 2", "Company Level 3", "Company Level 4", "Company Level 5", "Company Level 6" }, levels.Select(t => t.Text).ToArray());
        CollectionAssert.AreEqual(new[] { "Company Level 5" }, levels.Where(t => t.Big).Select(t => t.Text).ToArray(), "every fifth is big");
        var land = MomentsOf(game, company, "land").Single();
        Assert.AreEqual($"Bought parcel 1 of {game.World.Content.Progression.ParcelLevels.Count}", land.Text);
        Assert.IsTrue(land.Big);
        var track = MomentsOf(game, company, "track").Single();
        Assert.AreEqual("Iron tier 1", track.Text);
        Assert.IsFalse(track.Big, "a tier is small, a master big");
        Assert.AreEqual(TestGame.Start.AddMinutes(10), track.At, "when it was done");
    }

    [TestMethod]
    public void NeighboursTradingIsAMomentAndMovingInNextDoorToo()
    {
        var (game, a, b, _, _) = YardFacts.Neighbours();

        game.Pass(15);
        game.Pass(15);

        Assert.AreEqual("B moved in next door", MomentsOf(game, a, "crew").Single().Text);
        StringAssert.StartsWith(MomentsOf(game, b, "crew").Single().Text, "Moved in at the Yard of");
        var trade = MomentsOf(game, a, "partner").Single();
        Assert.AreEqual("First trade with another player: B", trade.Text);
        Assert.IsTrue(trade.Big);
        Assert.AreEqual("First trade with another player: A", MomentsOf(game, b, "partner").Single().Text);
        Assert.AreEqual(1, game.World.PendingSamples.Last(t => t.Subject == a.Id && t.Kind == StatSubject.Company).Values["partners"]);
    }

    [TestMethod]
    public void TheTownsEraAndItsCalendarAreMomentsOfItsPlayers()
    {
        var game = new TestGame(events: true);
        var company = game.NewCompany();

        game.World.AdvanceEra(company.District);
        game.World.StartEventNow(EventKind.Storm, company.District, TimeSpan.FromHours(2));
        game.Pass(15);

        var era = MomentsOf(game, company, "era").Single();
        StringAssert.StartsWith(era.Text, "Era 2: ");
        Assert.IsTrue(era.Big);
        StringAssert.StartsWith(MomentsOf(game, company, "event").Single().Text, "Storm: ");
        Assert.AreEqual(2, game.World.PendingSamples.Last(t => t.Kind == StatSubject.Town).Values["era"]);
    }
}
