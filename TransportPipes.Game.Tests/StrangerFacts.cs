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
    public void AStrangerWhoseYardSplitsKeepsTheStagesSheStoodWith()
    {
        // Run 3: Capital W shared Capital SW's project until its held seats lapsed (day 17); then it began again at the
        // Foundation and lost its Water Tower, against "expansion over rebuilding"
        var game = new TestGame();
        game.World.TenantsEnabled = true;
        game.World.ProjectsEnabled = true;
        game.World.OpenNextDistrict();
        var ana = game.NewCompany("Ana", slot: 0);
        game.World.FoundCrew(ana.Id, "Kettle & Co.", "kettle", "#236b69", "#c9963a", 3);
        var corner = WorldLayout.StationOf(ana.District, ana.Slot);
        var mia = game.World.CreateCompany(99, "Mia", "#e84393", seat: game.World.SuggestSeat(Placement.Neighbours));
        var home = WorldLayout.StationOf(mia.District, mia.Slot);
        var shared = game.World.ProjectOf(home);
        Assert.AreSame(game.World.ProjectOf(corner), shared, "she shares the crew's project");
        game.World.FinishStageNow(home);
        game.World.FinishStageNow(home);
        Assert.AreEqual(ProjectStage.Workshop, shared.Stage);

        Assert.IsTrue(game.World.LeaveCrew(ana.Id).Success);
        CollectionAssert.DoesNotContain(game.World.BigStation(corner).ToList(), home, "her Yard is her own now");

        var own = game.World.ProjectOf(home);
        Assert.AreNotSame(shared, own);
        CollectionAssert.AreEquivalent(new[] { ProjectStage.Foundation, ProjectStage.WaterTower }, own.Finished.Keys.ToList(), "the stages she stood with stay");
        Assert.AreEqual(ProjectStage.Workshop, own.Stage, "and her station goes on from there");
        Assert.AreEqual(shared.StartedAt, own.StartedAt, "the Foundry Hall opens on the same day");
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

    /// <summary>
    /// The squatter probe (step 6): a stranger the game seats next to the crew and who never plays again holds nothing the
    /// crew needs (their corner station holds all five land goods), so the crew's chains are fed as well as without her.
    /// Slow (three minutes): run it with TP_BALANCE=1.
    /// </summary>
    [TestMethod]
    public void ASquatterNextDoorChangesNoChain()
    {
        if (Environment.GetEnvironmentVariable("TP_BALANCE") != "1")
        {
            Assert.Inconclusive("Set TP_BALANCE=1 to run the squatter probe.");
        }
        var without = CrewChains(squatter: false);
        var with = CrewChains(squatter: true);
        foreach (var (name, alone) in without)
        {
            var beside = with[name];
            Assert.IsTrue(alone.Chain > 0, $"{name} runs a chain that buys an input");
            Assert.IsTrue(beside.Short <= alone.Short + 1e-9 * alone.Chain, $"{name}: short {beside.Short:N0} of {beside.Chain:N0} building-hours beside her, {alone.Short:N0} of {alone.Chain:N0} without");
            Assert.IsTrue(beside.Gold >= 0.99 * alone.Gold, $"{name}: {beside.Gold:N0} gold beside her, {alone.Gold:N0} without");
        }
    }

    /// <summary>Three light bots in a crew for five evenings: per member the building-hours its processors were short of an input they buy, all of them, and the gold</summary>
    private static Dictionary<string, (double Short, double Chain, double Gold)> CrewChains(bool squatter)
    {
        var game = new TestGame(seed: 4242, tutorial: true, surveys: true, dealtLand: true);
        var world = game.World;
        world.TenantsEnabled = true;
        world.ProjectsEnabled = true;
        world.OpenNextDistrict();
        game.Pass(20 * 60);
        var ana = game.NewCompany("Ana", gold: world.StartingGold);
        var (_, crew) = world.FoundCrew(ana.Id, "Kettle & Co.", "kettle", "#236b69", "#c9963a", 2);
        var members = new List<Company> { ana };
        foreach (var name in new[] { "Ben", "Cleo" })
        {
            var member = game.NewCompany(name, gold: world.StartingGold);
            Assert.IsTrue(world.JoinCrew(member.Id, crew!.InviteCode).Success);
            members.Add(member);
        }
        if (squatter)
        {
            var mia = world.CreateCompany(99, "Mia", "#e84393", seat: world.SuggestSeat(Placement.Neighbours));
            Assert.IsTrue(mia.SeatedNextDoor);
        }
        var bots = members.Select(t => new BalanceSimulation.Bot(game, t) { MaxBuildings = 15 }).ToList();
        var chains = members.ToDictionary(t => t.Name, _ => (Short: 0.0, Chain: 0.0, Gold: 0.0));
        for (var day = 1; day <= 5; day++)
        {
            for (var minute = 0; minute < 20; minute += 5)
            {
                bots.ForEach(t => t.CheckIn(advisor: true));
                game.Pass(5);
            }
            for (var hour = 0; hour < 24; hour++)
            {
                game.Pass(hour == 0 ? 40 : 60);
                foreach (var member in members)
                {
                    var own = world.Generator.LandOf(member.District, member.Slot);
                    var (shortHours, chainHours, _) = chains[member.Name];
                    foreach (var building in world.BuildingsOf(member.Id).Where(t => !t.UnderConstruction))
                    {
                        var bought = world.Content.Building(building.Type).In.Keys.Where(t => t != "energy" && !own.Contains(t) && t != "ironOre").ToList();
                        if (bought.Count == 0)
                        {
                            continue;
                        }
                        chainHours++;
                        if (world.IdleReasonOf(building) is { Cause: IdleCause.InputMissing or IdleCause.InputShort, Resource: var resource } && bought.Contains(resource!))
                        {
                            shortHours++;
                        }
                    }
                    chains[member.Name] = (shortHours, chainHours, member.Gold);
                }
            }
        }
        return chains;
    }
}
