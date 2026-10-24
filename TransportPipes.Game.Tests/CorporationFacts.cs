using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class CorporationFacts
{
    private static (TestGame Game, Company Leader, Corporation Corporation) Founded()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var leader = game.NewCompany("Leader", level: 8);
        Assert.IsTrue(game.World.FoundCorporation(leader.Id, "Pipe Guild", "pipe", "#3399ff").Success);
        return (game, leader, game.World.Corporations.Single());
    }

    [TestMethod]
    public void FoundingNeedsLevelEightAndGold()
    {
        var game = new TestGame();
        var company = game.NewCompany(gold: 50_000, level: 7);
        StringAssert.Contains(game.World.FoundCorporation(company.Id, "Guild", "GLD", "#ffffff").Error, "Level 8");
        company.Level = 8;
        StringAssert.Contains(game.World.FoundCorporation(company.Id, "Guild", "toolong", "#ffffff").Error, "tag");
        Assert.IsTrue(game.World.FoundCorporation(company.Id, "Guild", "gld", "#ffffff").Success);

        var corporation = game.World.Corporations.Single();
        Assert.AreEqual("GLD", corporation.Tag);
        Assert.AreEqual(40_000, company.Gold);
        Assert.AreEqual(CorporationRole.Leader, company.Role);
        Assert.IsNotNull(corporation.Goal, "a goal for the first week");
        StringAssert.Contains(game.World.FoundCorporation(game.NewCompany("Other", level: 8).Id, "guild", "XYZ", "#fff000").Error, "taken");
    }

    [TestMethod]
    public void AnApplicationAndAnInviteMeetAndTheCompanyJoins()
    {
        var (game, leader, corporation) = Founded();
        var member = game.NewCompany("Member");

        Assert.IsTrue(game.World.ApplyToCorporation(member.Id, corporation.Id).Success);
        Assert.IsNull(member.CorporationId, "waits for an officer");
        Assert.IsTrue(game.World.InviteToCorporation(leader.Id, member.Id).Success);

        Assert.AreEqual(corporation.Id, member.CorporationId);
        Assert.AreEqual(CorporationRole.Member, member.Role);
        Assert.IsEmpty(corporation.Applications);
        Assert.IsTrue(game.World.SameCorporation(leader.Id, member.Id));
        StringAssert.Contains(game.World.InviteToCorporation(member.Id, game.NewCompany("X").Id).Error, "officers");
    }

    [TestMethod]
    public void OfficersDeclineApplications()
    {
        var (game, leader, corporation) = Founded();
        var applicant = game.NewCompany("Applicant");
        var member = game.NewCompany("Member");
        Assert.IsTrue(game.World.ApplyToCorporation(applicant.Id, corporation.Id).Success);
        Assert.IsTrue(game.World.ApplyToCorporation(member.Id, corporation.Id).Success);
        Assert.IsTrue(game.World.InviteToCorporation(leader.Id, member.Id).Success);
        game.World.TakeChanges();

        StringAssert.Contains(game.World.DeclineApplication(member.Id, applicant.Id).Error, "officers");
        Assert.IsTrue(game.World.DeclineApplication(leader.Id, applicant.Id).Success);

        Assert.IsEmpty(corporation.Applications);
        Assert.IsNull(applicant.CorporationId);
        Assert.IsTrue(game.World.TakeChanges().Events.Any(t => t.Company == applicant.Id && t.Text.Contains("declined")));
        StringAssert.Contains(game.World.DeclineApplication(leader.Id, applicant.Id).Error, "No application");
    }

    [TestMethod]
    public void TheLeaderLeavesAndTheNextOneLeadsTheLastOneDissolvesIt()
    {
        var (game, leader, corporation) = Founded();
        var member = game.NewCompany("Member");
        game.World.InviteToCorporation(leader.Id, member.Id);
        game.World.ApplyToCorporation(member.Id, corporation.Id);

        Assert.IsTrue(game.World.LeaveCorporation(leader.Id).Success);
        Assert.AreEqual(CorporationRole.Leader, member.Role);
        StringAssert.Contains(game.World.ApplyToCorporation(leader.Id, corporation.Id).Error, "less than a day");

        Assert.IsTrue(game.World.LeaveCorporation(member.Id).Success);
        Assert.IsEmpty(game.World.Corporations);
    }

    [TestMethod]
    public void OfficersRemoveMembersButNotTheLeader()
    {
        var (game, leader, corporation) = Founded();
        var officer = game.NewCompany("Officer");
        var member = game.NewCompany("Member");
        foreach (var company in new[] { officer, member })
        {
            game.World.InviteToCorporation(leader.Id, company.Id);
            game.World.ApplyToCorporation(company.Id, corporation.Id);
        }
        Assert.IsTrue(game.World.SetCorporationRole(leader.Id, officer.Id, CorporationRole.Officer).Success);

        StringAssert.Contains(game.World.KickFromCorporation(officer.Id, leader.Id).Error, "lower role");
        Assert.IsTrue(game.World.KickFromCorporation(officer.Id, member.Id).Success);
        Assert.IsNull(member.CorporationId);
    }

    [TestMethod]
    public void TheTreasuryUpgradesTheHqAndThePerksWork()
    {
        var (game, leader, corporation) = Founded();
        Assert.IsTrue(game.World.DonateToCorporation(leader.Id, 100_000).Success);
        StringAssert.Contains(game.World.SetCorporationSettings(leader.Id, 0.5, "plastic?", "").Error, "Unknown resource");
        Assert.IsTrue(game.World.SetCorporationSettings(leader.Id, 0.5, "energy", "We pipe").Success);
        Assert.AreEqual(0.1, corporation.Tax, "at most 10%");

        Assert.IsTrue(game.World.UpgradeCorporationHq(leader.Id).Success);
        Assert.AreEqual(2, corporation.HqLevel);
        Assert.AreEqual(100_000 - GameWorld.HqUpgradeCost(1), corporation.Treasury);
        Assert.AreEqual(0.05, game.World.CorporationOutputBonus(leader, "energy"), 1e-9);
        Assert.AreEqual(0, game.World.CorporationOutputBonus(leader, "ironOre"));
    }

    [TestMethod]
    public void MembersPayTaxOnWhatTheyEarnInTheYard()
    {
        var (game, leader, corporation) = Founded();
        var buyer = game.NewCompany("Buyer", level: 2);
        game.World.InviteToCorporation(leader.Id, buyer.Id);
        game.World.ApplyToCorporation(buyer.Id, corporation.Id);
        Assert.AreEqual(leader.Slot + 1, buyer.Slot, "neighbours in one station");
        var moily = game.Build(leader, "windMoily");
        game.ConnectToGate(leader, moily);
        var mine = game.Build(buyer, "ironMine", game.Spot(buyer, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(buyer, mine);
        game.Connect(buyer, mine, game.TradingPost(buyer));
        game.Pass(3);

        game.World.DonateToCorporation(leader.Id, 1);
        game.World.SetCorporationSettings(leader.Id, 0.1, null, "");
        var treasury = corporation.Treasury;
        var energy = game.World.YardTrades(game.EstateOf(leader)).Where(t => t.Selling).Sum(t => t.PerMinute);
        Assert.IsTrue(energy > 0, "the leader sells its energy to the buyer's mine");
        var ore = game.World.NetworkOf(mine.Id)!.Solution.Flows.Where(t => t.Kind == FlowKind.Sale).Sum(t => t.Delivered * t.PricePerUnit);
        game.Pass(10);
        // 10% of the leader's trade income (at the player price) and of the buyer's ore sales
        Assert.AreEqual(treasury + (energy * game.World.PlayerPrice("energy") + ore) * 10 * 0.1, corporation.Treasury, 1e-3);
    }

    [TestMethod]
    public void MembersReachTheWeeklyGoalTogether()
    {
        var (game, leader, corporation) = Founded();
        var goal = corporation.Goal!;
        var storage = game.Map(leader, game.Local(leader, 1, 1), "O").Values.Single();
        storage.Contents[goal.Resource] = goal.Amount;
        var treasury = corporation.Treasury;

        Assert.IsTrue(game.World.DeliverToCorporationGoal(leader.Id).Success);

        Assert.IsTrue(goal.Rewarded);
        Assert.AreEqual(treasury + goal.RewardGold, corporation.Treasury);
        Assert.AreEqual(goal.Amount, goal.Contributions[leader.Id]);
    }
}
