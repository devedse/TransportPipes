using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>The Yard is the crew (1.4 §6–7): who lives around it, open or closed, requests, the link, Pick for me</summary>
[TestClass]
public class CrewFacts
{
    private static readonly DistrictCoord Capital = new(0, 0);

    private static EstateKey Plot(int slot) => new(Capital, slot);

    private static StationRef Yard(int index) => new(Capital, index);

    private static Company Settle(TestGame game, string name, EstateKey? seat = null, string? code = null) =>
        game.World.CreateCompany(game.World.Companies.Count() + 1, name, "#3f8ae0", seat: seat, code: code);

    [TestMethod]
    public void TheFirstSettlerFoundsTheCrewAndEveryoneWhoMovesInJoinsIt()
    {
        var game = new TestGame();
        var ana = Settle(game, "Ana", Plot(0));
        var ben = Settle(game, "Ben", Plot(1));

        var crew = game.World.CrewAt(Yard(0))!;
        Assert.AreEqual(ana.Id, crew.Founder);
        Assert.AreEqual(crew.Id, ana.CrewId);
        Assert.AreEqual(crew.Id, ben.CrewId);
        Assert.IsTrue(crew.Open, "a Yard starts open");
        Assert.IsFalse(crew.Named, "it keeps the game's name until a resident names it");
        // Every resident is an owner: Ben names it
        Assert.IsTrue(game.World.SetYard(ben.Id, "Kettle & Co", "kettle", "#c9963a", "#236b69").Success);
        Assert.AreEqual("Kettle & Co", game.World.CrewAt(Yard(0))!.Name);
    }

    [TestMethod]
    public void AClosedYardTakesNobodyWithoutAYesAndANewcomerAsksWhilePlayingNearby()
    {
        var game = new TestGame();
        var ana = Settle(game, "Ana", Plot(0));
        Assert.IsTrue(game.World.SetYardOpen(ana.Id, false).Success);

        Assert.IsFalse(game.World.SeatFree(Plot(1)));
        var cleo = Settle(game, "Cleo", Plot(1));

        Assert.AreNotEqual(Yard(0), game.EstateOf(cleo).Station, "she plays elsewhere meanwhile");
        Assert.IsTrue(game.World.CrewAt(Yard(0))!.Requests.Any(t => t.Company == cleo.Id), "and her request waits");
        var pick = game.World.PickForMe();
        Assert.AreNotEqual(Yard(0), WorldLayout.StationOf(pick.District, pick.Slot), "Pick for me never chooses a closed Yard");
    }

    [TestMethod]
    public void AYesMovesTheNewcomerInForFree()
    {
        var game = new TestGame();
        var ana = Settle(game, "Ana", Plot(0));
        game.World.SetYardOpen(ana.Id, false);
        var cleo = Settle(game, "Cleo", Plot(1));
        var gold = cleo.Gold;
        game.Pass(1);

        Assert.IsTrue(game.World.AnswerRequest(ana.Id, cleo.Id, yes: true).Success);
        game.World.LandNow(cleo);

        Assert.AreEqual(Yard(0), game.EstateOf(cleo).Station);
        Assert.AreEqual(game.World.CrewAt(Yard(0))!.Id, cleo.CrewId);
        Assert.AreEqual(gold, cleo.Gold, 1e-9, "the move is free");
        Assert.IsEmpty(game.World.CrewAt(Yard(0))!.Requests);
    }

    [TestMethod]
    public void ARequestWaitsThreeDays()
    {
        var game = new TestGame();
        var ana = Settle(game, "Ana", Plot(0));
        game.World.SetYardOpen(ana.Id, false);
        var cleo = Settle(game, "Cleo", Plot(1));

        game.Pass(2 * 24 * 60);
        Assert.HasCount(1, game.World.CrewAt(Yard(0))!.Requests);
        game.Pass(1.5 * 24 * 60);
        Assert.IsEmpty(game.World.CrewAt(Yard(0))!.Requests);
        Assert.IsFalse(game.World.AnswerRequest(ana.Id, cleo.Id, yes: true).Success, "a lapsed request can't be answered");
    }

    [TestMethod]
    public void ALinkLetsItsFirstUserIntoAClosedYardAndOnlyThem()
    {
        var game = new TestGame();
        var ana = Settle(game, "Ana", Plot(0));
        game.World.SetYardOpen(ana.Id, false);
        var code = game.World.NextToMeLink(ana.Id);

        var ben = Settle(game, "Ben", Plot(1), code);
        Assert.AreEqual(Yard(0), game.EstateOf(ben).Station, "the link opens the closed Yard");

        var cleo = Settle(game, "Cleo", Plot(2), code);
        Assert.AreNotEqual(Yard(0), game.EstateOf(cleo).Station, "a used link lets nobody else in");
    }

    [TestMethod]
    public void ALinkWithoutAPlotSettlesAtTheFriendsYard()
    {
        var game = new TestGame();
        var ana = Settle(game, "Ana", Plot(4));
        var code = game.World.NextToMeLink(ana.Id);

        var ben = Settle(game, "Ben", code: code);

        Assert.AreEqual(Yard(1), game.EstateOf(ben).Station);
    }

    [TestMethod]
    public void PickForMeChoosesAYardWherePeoplePlayAndNeverOneWhereNobodyDoes()
    {
        var game = new TestGame();
        var ana = Settle(game, "Ana", Plot(4));
        var lively = game.World.PickForMe();
        Assert.AreEqual(Yard(1), WorldLayout.StationOf(lively.District, lively.Slot), "next to someone who plays");

        // Ana stops playing: her Yard is no place for a newcomer who wants company, an empty Yard is
        ana.LastSeenAt = game.Time.AddDays(-10);
        game.Pass(1);
        var pick = game.World.PickForMe();
        Assert.AreNotEqual(Yard(1), WorldLayout.StationOf(pick.District, pick.Slot));
        Assert.IsNull(game.World.CrewAt(WorldLayout.StationOf(pick.District, pick.Slot)), "an empty Yard");
    }

    [TestMethod]
    public void TheLastOneOutTakesTheCrewWithIt()
    {
        var game = new TestGame();
        var ana = Settle(game, "Ana", Plot(0));
        var ben = Settle(game, "Ben", Plot(1));
        game.Pass(10);

        Assert.IsTrue(game.World.RelocateTo(ben.Id, Plot(4)).Success);
        game.World.LandNow(ben);
        Assert.IsNotNull(game.World.CrewAt(Yard(0)), "Ana still lives there");
        Assert.IsTrue(game.World.LastInYard(ana));

        game.Pass(10);
        Assert.IsTrue(game.World.RelocateTo(ana.Id, Plot(8)).Success);
        game.World.LandNow(ana);
        Assert.IsNull(game.World.CrewAt(Yard(0)), "nobody lives there: the crew is gone");
        Assert.AreEqual(game.World.CrewAt(Yard(2))!.Id, ana.CrewId, "Ana founds the crew of her new Yard");
    }

    [TestMethod]
    public void ANewTownOpensWhenEveryOpenTownHasFewerThanTwoFreePlots()
    {
        var game = new TestGame();
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict - 2; slot++)
        {
            Settle(game, $"P{slot}", Plot(slot));
        }
        Assert.HasCount(1, game.World.OpenDistricts.ToList());

        Settle(game, "One more", Plot(WorldLayout.EstatesPerDistrict - 2));
        game.World.PickForMe();

        Assert.HasCount(2, game.World.OpenDistricts.ToList(), "one free plot left: the next town opens");
    }
}
