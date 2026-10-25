using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class TeleporterFacts
{
    private static double Stored(TestGame game, Building building, string resource) =>
        game.World.NetworkOf(building.Id)!.Solution.Flows.Where(t => t.Kind == FlowKind.ToStorage && t.Resource == resource).Sum(t => t.Sent);

    /// <summary>Two networks of one company: a moily, an ore storage and a sender; a receiver and an empty storage</summary>
    private static (TestGame Game, Company Company, Building Sender, Building Receiver, Building Target) TwoNetworks()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 30);
        var a = game.Map(company, game.Local(company, 2, 2), "W+Q\nO+");
        a[(0, 1)].Contents["ironOre"] = 100_000;
        var b = game.Map(company, game.Local(company, 2, 12), "Q+O");
        return (game, company, a[(2, 0)], b[(0, 0)], b[(2, 0)]);
    }

    [TestMethod]
    public void APneumaticPostSendsWhatTheOtherNetworkReceivesWithoutMergingThem()
    {
        var (game, company, sender, receiver, target) = TwoNetworks();

        Assert.IsTrue(game.World.LinkTeleporter(company.Id, sender.Id, receiver.Id, "ironOre").Success);
        game.Pass(1);
        game.Pass(1);

        Assert.AreNotEqual(game.World.NetworkOf(sender.Id)!.Id, game.World.NetworkOf(receiver.Id)!.Id, "networks never merge");
        Assert.AreEqual(game.World.TeleportThroughput(sender), Stored(game, target, "ironOre"), 1e-6);
        // The energy for the distance comes from the sender's network
        var energy = game.World.NetworkOf(sender.Id)!.Solution.Flows.Where(t => t.To == sender.Id && t.Resource == "energy").Sum(t => t.Sent);
        Assert.AreEqual(game.World.TeleportThroughput(sender) * GameWorld.TeleportEnergyPerTile("pneumaticPost") * GameWorld.TeleportDistance(sender, receiver), energy, 1e-6);

        Assert.IsTrue(game.World.UnlinkTeleporter(company.Id, receiver.Id).Success);
        game.Pass(1);
        game.Pass(1);
        Assert.AreEqual(0, Stored(game, target, "ironOre"), 1e-9);
    }

    [TestMethod]
    public void AStationEitherSendsOrReceives()
    {
        var (game, company, sender, receiver, _) = TwoNetworks();
        var third = game.Map(company, game.Local(company, 10, 20), "Q")[(0, 0)];

        Assert.IsTrue(game.World.LinkTeleporter(company.Id, sender.Id, receiver.Id, "ironOre").Success);
        StringAssert.Contains(game.World.LinkTeleporter(company.Id, receiver.Id, third.Id, "ironOre").Error, "either sends or receives");
        StringAssert.Contains(game.World.LinkTeleporter(company.Id, third.Id, receiver.Id, "ironOre").Error, "another station");
        StringAssert.Contains(game.World.LinkTeleporter(company.Id, sender.Id, sender.Id, "ironOre").Error, "same kind");
        StringAssert.Contains(game.World.LinkTeleporter(game.NewCompany("Other").Id, sender.Id, receiver.Id, "ironOre").Error, "your own");
    }

    [TestMethod]
    public void APneumaticPostStaysInItsDistrictAQuantumLinkReachesAnywhereAndLosesTwoPercent()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 40);
        var far = new TilePos(game.Local(company, 0, 0).X + WorldLayout.DistrictPitchX * 2, game.Local(company, 0, 0).Y);
        var post = game.Map(company, game.Local(company, 2, 2), "Q")[(0, 0)];
        var farPost = game.Map(company, far.Offset(0, 6), "Q")[(0, 0)];
        StringAssert.Contains(game.World.LinkTeleporter(company.Id, post.Id, farPost.Id, "ironOre").Error, "same district");

        // Home: an ore storage and solar panels (plenty of energy for the distance) next to a Quantum Link
        var home = game.Local(company, 2, 10);
        var linkA = new Building { Id = game.World.NextBuildingId, Type = "quantumLink", Owner = company.Id, X = home.X + 2, Y = home.Y, Size = 2 };
        game.World.LoadBuilding(linkA);
        var a = game.Map(company, home, "O+\nS+\nS+\nS+");
        a[(0, 0)].Contents["ironOre"] = 100_000;
        foreach (var panel in game.World.BuildingsOf(company.Id).Where(t => t.Type == "solarPanel"))
        {
            panel.Level = 100;
        }
        // Far away: the other Quantum Link and a Vault
        var linkB = new Building { Id = game.World.NextBuildingId, Type = "quantumLink", Owner = company.Id, X = far.X, Y = far.Y, Size = 2 };
        game.World.LoadBuilding(linkB);
        var vault = new Building { Id = game.World.NextBuildingId, Type = "vault", Owner = company.Id, X = far.X + 3, Y = far.Y, Size = 2 };
        game.World.LoadBuilding(vault);
        game.Map(company, far.Offset(2, 0), "+");

        Assert.IsTrue(game.World.LinkTeleporter(company.Id, linkA.Id, linkB.Id, "ironOre").Success);
        game.Pass(1);
        game.Pass(1);

        Assert.AreEqual(1, game.World.NetworkOf(linkA.Id)!.Solution.Rate(linkA.Id), 1e-6, "ore and energy enough");
        Assert.AreEqual(game.World.TeleportThroughput(linkA) * 0.98, Stored(game, vault, "ironOre"), 1e-6);
    }
}
