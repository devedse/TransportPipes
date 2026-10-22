using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>The stranger next door and the first-comer (step 6 of the redesign, docs/playtest-redesign.html §4)</summary>
[TestClass]
public class StrangerFacts
{
    [TestMethod]
    public void OnASmallServerSomeoneWhoComesAloneLivesBesideACrew()
    {
        var game = new TestGame();
        game.World.TenantsEnabled = true;
        game.World.ProjectsEnabled = true;
        game.World.OpenNextDistrict();
        var ana = game.NewCompany("Ana", slot: 0);
        var (_, crew) = game.World.FoundCrew(ana.Id, "Kettle & Co.", "kettle", "#236b69", "#c9963a", 3);
        var corner = WorldLayout.StationOf(ana.District, ana.Slot);
        Assert.IsTrue(game.World.SmallMode);

        var seat = game.World.SuggestSeat(Placement.Neighbours);
        var mia = game.World.CreateCompany(99, "Mia", "#e84393", seat: seat);

        var home = WorldLayout.StationOf(mia.District, mia.Slot);
        Assert.IsTrue(home.IsSmall && WorldLayout.CornersNextTo(home.Index).Contains(corner.Index), $"beside the crew: {home}");
        Assert.IsTrue(mia.SeatedNextDoor);
        CollectionAssert.Contains(game.World.BigStation(corner).ToList(), home, "she shares the crew's Yard and project");
        Assert.IsNull(mia.ProbationUntil, "no probation for a stranger the game seated");
        Assert.IsFalse(crew!.Holds.Any(t => t.Seat == new EstateKey(mia.District, mia.Slot)), "the crew's own seats stay closed");
    }

    [TestMethod]
    public void TheFirstComerReadsWhoComesNext()
    {
        var game = new TestGame();
        game.World.TakeChanges();
        var first = game.NewCompany("First");
        var events = game.World.TakeChanges().Events;
        Assert.IsTrue(events.Any(t => t.Company == first.Id && t.Text.StartsWith("You're the first in")), string.Join(" | ", events.Select(t => t.Text)));

        var second = game.NewCompany("Second");
        Assert.IsFalse(game.World.TakeChanges().Events.Any(t => t.Company == second.Id && t.Text.StartsWith("You're the first")), "once, for the first only");
    }
}
