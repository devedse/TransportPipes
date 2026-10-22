using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class ReturnFacts
{
    /// <summary>Days pass; the companies given play every day, the others are away</summary>
    private static void Days(TestGame game, double days, params Company[] playing)
    {
        for (var day = 0.0; day < days; day += 1)
        {
            foreach (var company in playing)
            {
                company.LastSeenAt = game.World.Now;
            }
            game.Pass(Math.Min(1, days - day) * 24 * 60);
        }
    }

    private static (TestGame Game, Company Ana, Company Ben, Crew Crew) Crew()
    {
        var game = new TestGame();
        var ana = game.NewCompany("Ana", level: 20);
        var ben = game.NewCompany("Ben", level: 2);
        var (_, crew) = game.World.FoundCrew(ana.Id, "Kettle & Co.", "kettle", "#236b69", "#c9963a", 0);
        Assert.IsTrue(game.World.JoinCrew(ben.Id, crew!.InviteCode).Success);
        return (game, ana, ben, crew);
    }

    [TestMethod]
    public void ACrewMatesSeatIsHeldToDay90AndAFriendKeepsIt60DaysMore()
    {
        var (game, ana, ben, crew) = Crew();
        Days(game, 31, ana);
        Assert.IsTrue(ben.Asleep);
        Assert.AreEqual(ben.LastSeenAt + GameWorld.CrewHold, game.World.HoldEnds(ben));

        Days(game, 53, ana);
        Assert.IsTrue(crew.KeepCards.Contains(ben.Id), "a week before the hold ends: the keep card");
        Assert.IsTrue(game.World.Changes.Events.Any(t => t.Company == ana.Id && t.Text.Contains("Keep Ben's seat")));
        Assert.IsTrue(game.World.KeepSeat(ana.Id, ben.Id, keep: true).Success);
        Assert.IsFalse(game.World.KeepSeat(ana.Id, ben.Id, keep: true).Success, "the first answer counts, once");

        Days(game, 10, ana);
        Assert.IsNotNull(game.World.EstateOf(ben), "still in place on day 94");
        Days(game, 60, ana);
        Assert.IsNull(game.World.EstateOf(ben), "after 150 days it travels to the Quiet Isles");
        Assert.IsTrue(game.World.QuietIsles.Contains(ben));
    }

    [TestMethod]
    public void WithoutACrewTheSeatIsHeldToDay37AndAHouseWarmsIt()
    {
        var game = new TestGame();
        game.World.TenantsEnabled = true;
        game.World.SmallModeBelow = 0;
        var mo = game.NewCompany("Mo");
        var seat = (mo.District, mo.Slot);

        Days(game, 36);
        Assert.IsTrue(mo.Asleep);
        Assert.IsNotNull(game.World.EstateOf(mo));
        Days(game, 2);
        Assert.IsNull(game.World.EstateOf(mo), "packed on day 37");
        Assert.IsTrue(game.World.Estate(seat.District, seat.Slot)?.OwnerId is { } owner && game.World.Company(owner)!.IsNpc, "a House warms the seat");
    }

    [TestMethod]
    public void AReturnerFromTheQuietIslesLandsOnItsOldSeatAndCanChooseAnother()
    {
        var game = new TestGame();
        var mo = game.NewCompany("Mo");
        game.Build(mo, "windMoily");
        var seat = (mo.District, mo.Slot);
        Days(game, 40);
        Assert.IsNull(game.World.EstateOf(mo), "on the Quiet Isles");

        game.World.WakeUp(mo);

        Assert.AreEqual(seat, (mo.District, mo.Slot), "its old seat, still free");
        Assert.IsTrue(game.World.BuildingsOf(mo.Id).Any(t => t.Type == "windMoily"), "with its layout");
        var cards = game.World.LandingCards(mo);
        Assert.AreEqual("yours", cards[0].Kind);
        Assert.IsTrue(cards[0].Here);
        Assert.IsTrue(cards.Skip(1).Any(), "and other places to land, each with its crates");
        Assert.IsTrue(cards.All(t => t.Crates >= 0));
        Assert.AreEqual(1, mo.FreeMoves, "a free move to one of them");
    }

    [TestMethod]
    public void TendingRepairsAFriendsStormDamageAndBanksThreeDays()
    {
        var (game, ana, ben, _) = Crew();
        var cleo = game.NewCompany("Cleo");
        game.World.JoinCrew(cleo.Id, game.World.CrewOf(ana)!.InviteCode);
        var moily = game.Build(ben, "windMoily");
        Assert.IsFalse(game.World.Tend(ana.Id, ben.Id).Success, "Ben was here today");

        Days(game, 2, ana, cleo);
        Assert.IsFalse(game.World.Tend(ana.Id, ben.Id).Success, "F9: a tend shows only when it does a chore");
        moily.Damaged = true;
        var gold = ben.Gold;
        Assert.IsTrue(game.World.Tend(ana.Id, ben.Id).Success);
        Assert.IsFalse(moily.Damaged, "it repairs the storm damage");
        Assert.IsLessThan(gold, ben.Gold, "Ben's estate pays the repair, as it would");
        Assert.AreEqual(GameWorld.TendBanksDays, ben.Report.TendedDays, "it banks for three days");
        moily.Damaged = true;
        Assert.IsFalse(game.World.Tend(ana.Id, ben.Id).Success, "once a day");
        Assert.IsTrue(game.World.Tend(cleo.Id, ben.Id).Success);
        Assert.AreEqual(GameWorld.TendBanksDays, ben.Report.TendedDays, "the same days count once");
        Assert.IsTrue(game.World.Tended(ben));

        Days(game, 1, ana, cleo);
        moily.Damaged = true;
        Assert.IsTrue(game.World.Tend(ana.Id, ben.Id).Success);
        Assert.AreEqual(GameWorld.TendBanksDays + 1, ben.Report.TendedDays, "one more day");
        Assert.IsFalse(game.World.Tend(ana.Id, game.NewCompany("Stranger").Id).Success, "only crew mates");
    }

    [TestMethod]
    public void TheOnlyActivePlayerInAStationIsOfferedLivelierOnesOnTheFifthDay()
    {
        var game = new TestGame();
        var mo = game.NewCompany("Mo", slot: 0);
        var jo = game.NewCompany("Jo", slot: 3);
        var kim = game.NewCompany("Kim", slot: 4);

        Days(game, 4, mo, jo, kim);
        Assert.IsFalse(mo.LoneCard);
        Days(game, 1, mo, jo, kim);

        Assert.IsTrue(mo.LoneCard, "the fifth day alone");
        var stations = game.World.LivelierStations(mo);
        Assert.AreEqual(game.EstateOf(jo).Station, stations[0].Station, "where two play");
        Assert.AreEqual(2, stations[0].Active);
        Assert.IsTrue(game.World.DismissLoneCard(mo.Id).Success);
        Days(game, 10, mo, jo, kim);
        Assert.IsFalse(mo.LoneCard, "at most once every 30 days");
    }

    [TestMethod]
    public void ACrewMovesAsABlock()
    {
        var (game, ana, ben, crew) = Crew();
        var seat = game.World.CrewMoveSeat(ana);

        Assert.IsTrue(game.World.CrewMove(ana.Id, seat).Success);
        game.Pass(2);

        Assert.AreEqual(seat.Slot, ana.Slot);
        var kept = crew.Holds.Single(t => t.For == ben.Id);
        Assert.AreEqual(WorldLayout.StationOf(seat.District, seat.Slot), WorldLayout.StationOf(kept.Seat.District, kept.Seat.Slot), "a seat next to Ana for Ben");
        Assert.IsFalse(game.World.SeatFree(kept.Seat, game.NewCompany("Stranger")), "only Ben takes it");
        Assert.IsTrue(game.World.RelocateTo(ben.Id, kept.Seat).Success, "Ben follows with one tap");
        game.Pass(2);
        Assert.AreEqual(kept.Slot, ben.Slot);
        Assert.IsEmpty(crew.Holds);
    }

    [TestMethod]
    public void MostOfTheCrewAsleepTheOthersMoveTogetherAndTheSleepersSeatsFollow()
    {
        var (game, ana, ben, crew) = Crew();
        var sleepers = new[] { "Cleo", "Dev", "Eve" }.Select(t => game.NewCompany(t)).ToList();
        foreach (var sleeper in sleepers)
        {
            Assert.IsTrue(game.World.JoinCrew(sleeper.Id, crew.InviteCode).Success);
        }
        Days(game, 31, ana, ben);
        Assert.IsTrue(sleepers.All(t => t.Asleep));
        Assert.IsTrue(crew.ConsolidationOpen, "the consolidation card");

        Assert.IsTrue(game.World.Consolidate(ana.Id).Success);
        game.Pass(2);

        var station = game.EstateOf(ana).Station;
        Assert.IsTrue(sleepers.All(t => game.World.EstateOf(t) == null), "the sleepers' estates wait on the Quiet Isles");
        var home = game.World.BigStation(station)[0];
        bool NextToAna(EstateKey seat)
        {
            var at = WorldLayout.StationOf(seat.District, seat.Slot);
            return game.World.BigStation(at)[0] == home || (at.IsSmall && at.District == home.District && WorldLayout.CornersNextTo(at.Index).Contains(home.Index));
        }
        Assert.IsTrue(sleepers.All(t => t.LastSeat is { } seat && NextToAna(seat)), "their seats point to the new station (and the small ones next to it)");

        game.World.WakeUp(sleepers[0]);
        Assert.IsTrue(NextToAna(new EstateKey(sleepers[0].District, sleepers[0].Slot)), "Cleo lands next to her friends");
    }

    [TestMethod]
    public void AStrangerInACrewsStationIsOnProbationForTwoWeeks()
    {
        var game = new TestGame();
        var ana = game.NewCompany("Ana", slot: 0);
        var (_, crew) = game.World.FoundCrew(ana.Id, "Kettle & Co.", "kettle", "#236b69", "#c9963a", 1);
        var free = GameWorld.StationSeats(game.EstateOf(ana).Station).First(t => game.World.HoldOn(t) == null && t.Slot != 0);
        var sam = game.NewCompany("Sam", slot: free.Slot);
        Assert.IsNotNull(sam.ProbationUntil, "a crew lives in or holds two seats of the station");
        var elsewhere = game.NewCompany("Lu", slot: 3);
        Assert.IsNull(elsewhere.ProbationUntil);

        Days(game, 6, ana);
        Assert.IsNull(game.World.EstateOf(sam), "away five days in a row in the first two weeks: to the Quiet Isles, nothing lost");
        Assert.IsNotNull(game.World.EstateOf(elsewhere), "everywhere else the normal timeline");

        game.World.WakeUp(sam);
        Assert.AreEqual(free.Slot, sam.Slot, "the old seat is the first landing card while it is free");
    }

    [TestMethod]
    public void TailwindHelpsWhoeverIsFarBehindTheirFriends()
    {
        var (game, ana, ben, _) = Crew();
        Assert.AreEqual(2.5, game.World.TailwindFor(ben), 1e-9, "18 levels behind: at most ×2.5");
        Assert.AreEqual(1, game.World.TailwindFor(ana), "ahead: none");

        ben.Level = 12;
        Assert.AreEqual(1.8, game.World.TailwindFor(ben), 1e-9);
        ben.Level = 16;
        Assert.AreEqual(1, game.World.TailwindFor(ben), "within 5 levels: none");

        ben.Level = 12;
        game.Pass(16);
        Assert.AreEqual(1.8, ben.Tailwind, 1e-9, "worked out every quarter of an hour");
        var xp = ben.Xp;
        game.World.GainXp(ben, 10, "test", "test");
        Assert.AreEqual(xp + 18, ben.Xp, 1e-9);
    }
}
