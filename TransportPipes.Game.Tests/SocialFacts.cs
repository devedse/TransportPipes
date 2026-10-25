using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class SocialFacts
{
    [TestMethod]
    public void AnEstateCanBeLikedOnceADay()
    {
        var game = new TestGame();
        var visitor = game.NewCompany("Visitor");
        var owner = game.NewCompany("Owner", gold: 0, level: 3);

        StringAssert.Contains(game.World.LikeEstate(owner.Id, owner.Id).Error, "own plot");
        Assert.IsTrue(game.World.LikeEstate(visitor.Id, owner.Id).Success);
        StringAssert.Contains(game.World.LikeEstate(visitor.Id, owner.Id).Error, "today");
        Assert.AreEqual(1, owner.Likes);
        Assert.AreEqual(60, owner.Gold);

        game.Pass(60 * 24);
        Assert.IsTrue(game.World.LikeEstate(visitor.Id, owner.Id).Success);
    }

    [TestMethod]
    public void AMentorAndANewcomerAreRewardedAtLevelTen()
    {
        var game = new TestGame();
        var mentor = game.NewCompany("Mentor", gold: 0, level: 30);
        var newcomer = game.NewCompany("Newcomer", gold: 0);

        StringAssert.Contains(game.World.ChooseMentor(mentor.Id, newcomer.Id).Error, "below Level");
        Assert.IsTrue(game.World.ChooseMentor(newcomer.Id, mentor.Id).Success);

        game.World.GainXp(newcomer, game.World.Content.Progression.XpForLevel(10), "test", "test");

        Assert.AreEqual(10, newcomer.Level);
        Assert.AreEqual(10_000, mentor.Gold);
        Assert.AreEqual(10_000, newcomer.Gold);
        game.World.GainXp(newcomer, 1, "test", "test");
        Assert.AreEqual(10_000, mentor.Gold, "once per level");
    }

    [TestMethod]
    public void TheLeaderboardRanksByNetWorth()
    {
        var game = new TestGame();
        var poor = game.NewCompany("Poor", gold: 100);
        var rich = game.NewCompany("Rich", gold: 50_000);
        game.Build(rich, "windMoily");

        var board = game.World.Leaderboard(LeaderboardKind.NetWorth, poor.District);

        Assert.AreEqual(rich.Id, board[0].Company);
        Assert.AreEqual(1, board[0].Rank);
        Assert.AreEqual(50_000 + 2500, board[0].Value, "the gold spent on the moily is still worth it, plus the free trading post");
        Assert.AreEqual(poor.Id, board[1].Company);
    }

    [TestMethod]
    public void AFullStorageIsNoticedOnce()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        var map = game.Map(company, game.Local(company, 1, 1), "W+O");
        var storage = map[(2, 0)];
        game.World.TakeChanges();

        // A storage holds 5,000: a moily (1 energy per minute) fills the last 30 in half an hour
        storage.Contents["energy"] = 4970;
        game.Pass(40);

        var events = game.World.TakeChanges().Events.Where(t => t.Kind == WorldEventKind.StorageFull).ToList();
        Assert.HasCount(1, events);
        Assert.AreEqual(storage.Id, events[0].Building);
        Assert.AreEqual(storage.Id, game.World.FullStorages(company.Id).Single().Id);

        game.Pass(60);
        Assert.IsFalse(game.World.TakeChanges().Events.Any(t => t.Kind == WorldEventKind.StorageFull), "it stays full, no new notice");
    }

    [TestMethod]
    public void NeighboursHearAboutANewcomer()
    {
        var game = new TestGame();
        var old = game.NewCompany("Old Timer");
        game.World.TakeChanges();

        game.NewCompany("Newcomer");

        Assert.IsTrue(game.World.TakeChanges().Events.Any(t => t.Company == old.Id && t.Kind == WorldEventKind.Neighbour));
        CollectionAssert.Contains(old.Report.NewNeighbours, "Newcomer");
    }
}
