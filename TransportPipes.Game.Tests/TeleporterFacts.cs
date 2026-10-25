using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class TeleporterFacts
{
    private static double Stored(TestGame game, Building building, string resource) =>
        game.World.NetworkOf(building.Id)!.Solution.Flows.Where(t => t.Kind == FlowKind.ToStorage && t.Resource == resource).Sum(t => t.Sent);

    /// <summary>Two networks of one company far apart: a moily, an ore storage and a Quantum Link; a Quantum Link and an empty storage</summary>
    private static (TestGame Game, Company Company, Building Sender, Building Receiver, Building Target) TwoNetworks()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 30);
        var a = game.Map(company, game.Local(company, 2, 2), "W+K\nO+ \nS+\nS+");
        a[(0, 1)].Contents["ironOre"] = 100_000;
        foreach (var panel in game.World.BuildingsOf(company.Id).Where(t => t.Type == "solarPanel"))
        {
            // Plenty of energy for the distance
            panel.Level = 25;
        }
        var b = game.Map(company, game.Local(company, 2, 20), "K +O");
        // Room for a few minutes of 4,900 ore
        b[(3, 0)].Level = 25;
        return (game, company, a[(2, 0)], b[(0, 0)], b[(3, 0)]);
    }

    [TestMethod]
    public void AQuantumLinkSendsWhatTheOtherNetworkReceivesWithoutMergingThemAndLosesTwoPercent()
    {
        var (game, company, sender, receiver, target) = TwoNetworks();

        Assert.IsTrue(game.World.LinkTeleporter(company.Id, sender.Id, receiver.Id, "ironOre").Success);
        game.Pass(1);
        game.Pass(1);

        Assert.AreNotEqual(game.World.NetworkOf(sender.Id)!.Id, game.World.NetworkOf(receiver.Id)!.Id, "networks never merge");
        Assert.AreEqual(1, game.World.NetworkOf(sender.Id)!.Solution.Rate(sender.Id), 1e-6, "ore and energy enough");
        Assert.AreEqual(game.World.TeleportThroughput(sender) * 0.98, Stored(game, target, "ironOre"), 1e-6);
        // The energy for the distance comes from the sender's network
        var energy = game.World.NetworkOf(sender.Id)!.Solution.Flows.Where(t => t.To == sender.Id && t.Resource == "energy").Sum(t => t.Sent);
        Assert.AreEqual(game.World.TeleportThroughput(sender) * GameWorld.TeleportEnergyPerTile("quantumLink") * GameWorld.TeleportDistance(sender, receiver), energy, 1e-6);

        Assert.IsTrue(game.World.UnlinkTeleporter(company.Id, receiver.Id).Success);
        game.Pass(1);
        game.Pass(1);
        Assert.AreEqual(0, Stored(game, target, "ironOre"), 1e-9);
    }

    [TestMethod]
    public void AStationEitherSendsOrReceives()
    {
        var (game, company, sender, receiver, _) = TwoNetworks();
        var third = game.Map(company, game.Local(company, 10, 26), "K")[(0, 0)];

        Assert.IsTrue(game.World.LinkTeleporter(company.Id, sender.Id, receiver.Id, "ironOre").Success);
        StringAssert.Contains(game.World.LinkTeleporter(company.Id, receiver.Id, third.Id, "ironOre").Error, "either sends or receives");
        StringAssert.Contains(game.World.LinkTeleporter(company.Id, third.Id, receiver.Id, "ironOre").Error, "another station");
        StringAssert.Contains(game.World.LinkTeleporter(company.Id, sender.Id, sender.Id, "ironOre").Error, "same kind");
        StringAssert.Contains(game.World.LinkTeleporter(game.NewCompany("Other").Id, sender.Id, receiver.Id, "ironOre").Error, "your own");
    }
}
