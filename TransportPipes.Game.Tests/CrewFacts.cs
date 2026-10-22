using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class CrewFacts
{
    private static readonly DistrictCoord Capital = new(0, 0);

    private static (TestGame Game, Company Ana, Crew Crew) Founded(int holds = 3)
    {
        var game = new TestGame();
        var ana = game.NewCompany("Ana");
        var (result, crew) = game.World.FoundCrew(ana.Id, "Kettle & Co.", "kettle", "#236b69", "#c9963a", holds);
        Assert.IsTrue(result.Success, result.Error);
        return (game, ana, crew!);
    }

    [TestMethod]
    public void FoundingACrewHoldsTheStationsOtherSeatsForFriends()
    {
        var (game, ana, crew) = Founded();

        Assert.AreEqual(crew.Id, ana.CrewId);
        CollectionAssert.AreEquivalent(new[] { 1, 5, 6 }, crew.Holds.Select(t => t.Slot).ToArray(), "the other three seats of its station");
        Assert.AreEqual(3, crew.Holds.Select(t => t.Code).Distinct().Count(), "a code per seat");
        var stranger = game.NewCompany("Stranger");
        Assert.IsFalse(crew.Holds.Any(t => t.Slot == stranger.Slot), "held seats aren't given to strangers");
    }

    [TestMethod]
    public void AFriendMovesInWithASeatCode()
    {
        var (game, ana, crew) = Founded();
        var code = crew.Holds.Single(t => t.Slot == 6).Code;

        var ben = game.World.CreateCompany(99, "Ben", "#2d6fb4", code: GameWorld.ShowCode(code).ToLowerInvariant());

        Assert.AreEqual(6, ben.Slot, "on the seat the code holds");
        Assert.AreEqual(crew.Id, ben.CrewId);
        Assert.HasCount(2, crew.Holds);
        CollectionAssert.Contains(ana.Report.Notes, "Ben moved in next door");
        Assert.IsNull(game.World.HoldByCode(code), "a code is used once");
    }

    [TestMethod]
    public void TheCrewCallsAFriendByTheNameTheSeatWasKeptFor()
    {
        // Run 1: Ana never learned that Riverside Co. was Ben (F9)
        var (game, ana, crew) = Founded();
        var hold = crew.Holds.Single(t => t.Slot == 6);
        Assert.IsTrue(game.World.NameHold(ana.Id, GameWorld.ShowCode(hold.Code), " Ben ").Success);
        Assert.AreEqual("Ben", hold.FriendName);

        var ben = game.World.CreateCompany(99, "Riverside Co.", "#2d6fb4", code: GameWorld.ShowCode(hold.Code));

        Assert.AreEqual("Ben", ben.KnownAs);
        Assert.AreEqual("Riverside Co. (Ben)", game.World.NameFor(ben, ana), "to the crew");
        Assert.AreEqual("Riverside Co.", game.World.NameFor(ben, ben), "not to himself");
        var stranger = game.NewCompany("Stranger");
        Assert.AreEqual("Riverside Co.", game.World.NameFor(ben, stranger), "nor to strangers");
        StringAssert.Contains(game.World.NameHold(ana.Id, "NOPE-NOPE", "Cleo").Error, "No such");
    }

    [TestMethod]
    public void AFriendWithTheCrewLinkSettlesNextToTheCrew()
    {
        var (game, ana, crew) = Founded(holds: 2);
        var home = game.EstateOf(ana).Station;

        var cleo = game.World.CreateCompany(98, "Cleo", "#2d6fb4", code: GameWorld.ShowCode(crew.InviteCode));
        Assert.AreEqual(home, game.EstateOf(cleo).Station, "the free seat of the crew's own station, closed to strangers");
        Assert.AreEqual(crew.Id, cleo.CrewId);

        var dev = game.World.CreateCompany(97, "Dev", "#2d6fb4", code: crew.InviteCode);
        var devStation = game.EstateOf(dev).Station;
        Assert.AreNotEqual(home, devStation, "the crew's station is full");
        Assert.IsTrue(devStation.IsSmall && devStation.District == home.District && WorldLayout.CornersNextTo(devStation.Index).Contains(home.Index), "the small station next door");
        Assert.AreEqual(crew.Id, dev.CrewId);
    }

    [TestMethod]
    public void AStationACrewHoldsIsClosedToStrangersUntilItOpensIt()
    {
        var (game, ana, crew) = Founded(holds: 2);
        var station = game.EstateOf(ana).Station;
        var free = GameWorld.StationSeats(station).Single(t => game.World.HoldOn(t) == null && t.Slot != ana.Slot);

        Assert.IsFalse(game.World.StationOpenToStrangers(station), "a crew lives in or holds three seats");
        Assert.IsFalse(game.World.SeatFree(free));

        Assert.IsTrue(game.World.SetStationOpen(ana.Id, true).Success);
        Assert.IsTrue(game.World.SeatFree(free));
    }

    [TestMethod]
    public void TheInviteLinkLetsFriendsInForADayThenAMemberDoes()
    {
        var (game, ana, crew) = Founded(holds: 0);
        var cleo = game.NewCompany("Cleo");
        Assert.IsTrue(game.World.JoinCrew(cleo.Id, crew.InviteCode).Success);
        Assert.AreEqual(crew.Id, cleo.CrewId);

        game.Pass(25 * 60);
        var dev = game.NewCompany("Dev");
        Assert.IsTrue(game.World.JoinCrew(dev.Id, GameWorld.ShowCode(crew.InviteCode)).Success);
        Assert.IsNull(dev.CrewId, "after a day the link asks a member first");
        CollectionAssert.Contains(crew.JoinRequests.ToList(), dev.Id);

        Assert.IsTrue(game.World.AcceptJoinRequest(ana.Id, dev.Id).Success);
        Assert.AreEqual(crew.Id, dev.CrewId);
    }

    [TestMethod]
    public void LeavingTakesOneTapAndGivesAFreeMoveOnceAMonth()
    {
        var (game, ana, crew) = Founded(holds: 0);
        var ben = game.NewCompany("Ben");
        game.World.JoinCrew(ben.Id, crew.InviteCode);
        var moves = ben.FreeMoves;

        Assert.IsTrue(game.World.LeaveCrew(ben.Id).Success);
        Assert.IsNull(ben.CrewId);
        Assert.AreEqual(moves + 1, ben.FreeMoves);
        game.World.JoinCrew(ben.Id, crew.InviteCode);
        game.World.LeaveCrew(ben.Id);
        Assert.AreEqual(moves + 1, ben.FreeMoves, "once a month");

        Assert.IsTrue(game.World.LeaveCrew(ana.Id).Success);
        Assert.IsNull(game.World.Crew(crew.Id), "the last one out: the crew is gone");
    }

    [TestMethod]
    public void HeldSeatsLapseWhenNobodyInTheCrewPlays()
    {
        var (game, ana, crew) = Founded();

        game.Pass(15 * 24 * 60);

        Assert.IsEmpty(crew.Holds, "nobody played for two weeks");
        Assert.IsNotNull(game.World.Crew(crew.Id));
    }

    [TestMethod]
    public void TheGameSuggestsASeatForEveryWayOfStarting()
    {
        var game = new TestGame();
        var mo = game.NewCompany("Mo");

        var neighbours = game.World.SuggestSeat(Placement.Neighbours);
        Assert.AreEqual(game.EstateOf(mo).Station, WorldLayout.StationOf(neighbours.District, neighbours.Slot), "next to an active player");

        var own = game.World.SuggestSeat(Placement.OwnStation);
        var ownStation = WorldLayout.StationOf(own.District, own.Slot);
        Assert.IsTrue(GameWorld.StationSeats(ownStation).All(t => game.World.Estate(t.District, t.Slot)?.OwnerId is null), "a station nobody lives in");

        var friends = game.World.SuggestSeat(Placement.Friends, seats: 4);
        var friendsStation = WorldLayout.StationOf(friends.District, friends.Slot);
        Assert.IsFalse(friendsStation.IsSmall);
        Assert.AreEqual(4, GameWorld.StationSeats(friendsStation).Count(t => game.World.SeatFree(t)), "room for four");
    }

    [TestMethod]
    public void FriendsAndAStrangerAreSuggestedDifferentStations()
    {
        // Run 1, a quiet server: Mia ("Find me neighbours") and Ana ("Start with friends") were sent to the same empty
        // station 20 seconds apart, and the crew's claim refused Mia's seat
        var game = new TestGame();

        var stranger = game.World.SuggestSeat(Placement.Neighbours);
        var friends = game.World.SuggestSeat(Placement.Friends, seats: 4);

        Assert.AreNotEqual(WorldLayout.StationOf(stranger.District, stranger.Slot), WorldLayout.StationOf(friends.District, friends.Slot));
    }

    [TestMethod]
    public void OwnStationPrefersFourSeats()
    {
        var game = new TestGame();

        var own = game.World.SuggestSeat(Placement.OwnStation);

        Assert.IsFalse(WorldLayout.StationOf(own.District, own.Slot).IsSmall, "room for whoever comes later");
    }

    [TestMethod]
    public void AnEstateMovesToAChosenSeatWithAFewMinutesBetweenMoves()
    {
        var game = new TestGame();
        var mo = game.NewCompany("Mo");
        var seat = new EstateKey(Capital, 17);

        Assert.IsTrue(game.World.RelocateTo(mo.Id, seat).Success);
        game.Pass(2);

        Assert.AreEqual(17, mo.Slot);
        StringAssert.Contains(game.World.RelocateTo(mo.Id, new EstateKey(Capital, 18)).Error, "minutes");
        game.Pass(5);
        Assert.IsTrue(game.World.RelocateTo(mo.Id, new EstateKey(Capital, 18)).Success);
    }

    [TestMethod]
    public void ACrewOfFiveSharesOneYardAcrossItsSmallStation()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", slot: 0);
        game.World.FoundCrew(ana.Id, "Kettle & Co.", "kettle", "#236b69", "#c9963a", 3);
        var crew = game.World.CrewOf(ana)!;
        foreach (var hold in crew.Holds.ToList())
        {
            game.World.CreateCompany(10 + hold.Slot, $"Friend {hold.Slot}", "#888888", code: hold.Code);
        }
        // The fifth lives in the small station north of the hub, next to the crew's corner station
        var eve = game.NewCompany("Eve", level: 2, slot: 2);
        game.World.JoinCrew(eve.Id, crew.InviteCode);
        var small = WorldLayout.StationOf(Capital, 2);
        Assert.AreEqual(new StationRef(Capital, 0), game.World.JoinedTo(small), "one big station");

        var moily = game.Build(ana, "windMoily");
        game.ConnectToGate(ana, moily);
        var mine = game.Build(eve, "ironMine", game.Spot(eve, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(eve, mine);
        game.Connect(eve, mine, game.TradingPost(eve));
        game.Pass(3);

        Assert.IsTrue(game.World.YardTrades(game.EstateOf(eve)).Any(t => !t.Selling && t.Partner == ana.Id), "Eve's mine gets Ana's energy across the big station");
    }

    [TestMethod]
    public void AFriendMovingInGetsACompanyNameNobodyUses()
    {
        var (game, _, crew) = Founded();
        var seat = crew.Holds[0].Seat;

        var first = game.World.FreeCompanyName(seat);
        game.World.CreateCompany(96, first, "#2d6fb4");

        Assert.AreNotEqual(first, game.World.FreeCompanyName(seat));
        Assert.IsNull(game.World.CompanyNameError(game.World.FreeCompanyName(seat)));
        Assert.IsNull(game.World.CompanyNameError(game.World.FreeCompanyName()));
    }

    [TestMethod]
    public void NamesNobodyShouldSeeAreRefused()
    {
        var game = new TestGame();
        var ana = game.NewCompany("Ana");

        Assert.IsFalse(game.World.FoundCrew(ana.Id, "Sh1t Pipes", "kettle", "#236b69", "#c9963a").Result.Success);
        Assert.IsTrue(NameFilter.IsAllowed("Peacock Grapes of Sussex"), "no false alarms on ordinary words");
    }

    [TestMethod]
    public void AFriendsInviteClosesACrewOfOne()
    {
        // Run 3: Ben started his own crew before Ana's link reached him; "Join and move" said "Leave your crew first"
        var game = new TestGame();
        var ana = game.NewCompany("Ana");
        var (_, crew) = game.World.FoundCrew(ana.Id, "Teapot Four", "kettle", "#236b69", "#c9963a", 2);
        var ben = game.NewCompany("Ben");
        var (_, own) = game.World.FoundCrew(ben.Id, "Ana and Ben", "kettle", "#236b69", "#c9963a", 2);
        var seat = crew!.Holds[0];

        var result = game.World.JoinCrew(ben.Id, seat.Code);

        Assert.IsTrue(result.Success, result.Error);
        Assert.AreEqual(crew.Id, ben.CrewId);
        Assert.IsNull(game.World.Crew(own!.Id), "his crew of one is closed, with its held seats");
        game.Pass(24 * 60);
        Assert.AreEqual(seat.Seat, new EstateKey(ben.District, ben.Slot), "and he lives in the seat Ana kept, once moved");

        // A crew with others in it still has to be left first
        var cleo = game.NewCompany("Cleo");
        var (_, cleos) = game.World.FoundCrew(cleo.Id, "Cleo's", "kettle", "#236b69", "#c9963a", 1);
        var dee = game.NewCompany("Dee");
        Assert.IsTrue(game.World.JoinCrew(dee.Id, cleos!.Holds[0].Code).Success);
        StringAssert.Contains(game.World.JoinCrew(cleo.Id, crew.Holds[0].Code).Error, "Leave your crew first");
    }
}
