using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

[TestClass]
public class TenantFacts
{
    private static TestGame WithTenants()
    {
        var game = new TestGame();
        game.World.TenantsEnabled = true;
        return game;
    }

    [TestMethod]
    public void ANewDistrictGetsTenantsWithWorkingEstates()
    {
        var game = WithTenants();
        var player = game.NewCompany();

        var tenants = game.World.Companies.Where(t => t.IsNpc).ToList();
        Assert.HasCount(GameWorld.TenantSlots.Length, tenants);
        Assert.IsTrue(tenants.All(t => t.District == player.District));
        Assert.AreEqual(tenants.Count, tenants.Select(t => t.Name).Distinct().Count(), "unique names");
        foreach (var tenant in tenants)
        {
            var buildings = game.World.BuildingsOf(tenant.Id).ToList();
            Assert.IsGreaterThanOrEqualTo(2, buildings.Count, tenant.Name);
            // Their pipes reach all four gates
            foreach (var side in Enum.GetValues<GateSide>())
            {
                var gate = WorldLayout.GatePosition(tenant.District, tenant.Slot, side);
                Assert.AreSame(game.World.NetworkOf(buildings[0].Id), game.World.NetworkAt(gate), $"{tenant.Name} {side}");
            }
        }
        game.World.SolveNetworks();
        var producer = tenants.First(t => !game.World.BuildingsOf(t.Id).Any(b => b.Type == "tradingPost"));
        Assert.IsTrue(game.World.BuildingsOf(producer.Id).Any(b => game.World.NetworkOf(b.Id)!.Solution.Flows.Count > 0 || b.Type == "windMoily"));
    }

    [TestMethod]
    public void PlayersGetFreeEstatesFirstAndThenATenantMovesOut()
    {
        var game = WithTenants();
        var players = Enumerable.Range(0, WorldLayout.EstatesPerDistrict - GameWorld.TenantSlots.Length).Select(t => game.NewCompany($"P{t}")).ToList();
        Assert.IsTrue(players.All(t => !GameWorld.TenantSlots.Contains(t.Slot)), "tenants keep their estates while there is room");
        Assert.HasCount(GameWorld.TenantSlots.Length, game.World.Companies.Where(t => t.IsNpc).ToList());

        var newcomer = game.NewCompany("Newcomer");

        Assert.AreEqual(players[0].District, newcomer.District);
        Assert.IsTrue(GameWorld.TenantSlots.Contains(newcomer.Slot));
        Assert.HasCount(GameWorld.TenantSlots.Length - 1, game.World.Companies.Where(t => t.IsNpc && t.District == newcomer.District).ToList());
        var estate = game.EstateOf(newcomer);
        Assert.AreEqual(newcomer.Id, estate.OwnerId);
        CollectionAssert.AreEquivalent(new[] { "hq", "tradingPost" }, game.World.BuildingsOf(newcomer.Id).Select(t => t.Type).ToArray());
        Assert.IsFalse(game.World.Buildings.Any(t => estate.Contains(t.Position) && t.Owner != newcomer.Id), "the tenant's buildings are gone");
        Assert.AreEqual(4, game.World.Conductors.Count(t => estate.Contains(t.Pos)), "only the newcomer's gates");
    }

    [TestMethod]
    public void APlayerCanBuyFromAPowerTenantNextDoor()
    {
        var game = WithTenants();
        var player = game.NewCompany(level: 2);
        // A tenant that sells energy, and the player's iron mine piped to the corridor next to the tenant's gate
        var tenant = game.World.Companies.First(t => t.IsNpc && game.World.BuildingsOf(t.Id).Count(b => b.Type == "windMoily") >= 1 && !game.World.BuildingsOf(t.Id).Any(b => b.Type == "tradingPost"));
        var mine = game.Build(player, "ironMine", game.Spot(player, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.Connect(player, mine, game.TradingPost(player));
        var gate = WorldLayout.GatePosition(tenant.District, tenant.Slot, GateSide.Left);
        var outside = WorldLayout.OutsideGate(tenant.District, tenant.Slot, GateSide.Left);
        // Route over corridors from the mine to the tenant's gate (the test helper routes over the player's estate and corridors)
        game.Connect(player, mine.Border(), [outside]);
        game.World.SolveNetworks();

        var flows = game.World.NetworkOf(mine.Id)!.Solution.Flows;
        Assert.IsTrue(flows.Any(t => t.Kind == FlowKind.Trade && t.To == mine.Id && game.World.Building(t.From)!.Owner == tenant.Id), "the mine gets energy from the tenant");
        Assert.IsNotNull(gate);
    }

    [TestMethod]
    public void TenantsAnswerContractOffersRightAway()
    {
        var game = WithTenants();
        var player = game.NewCompany();
        var works = game.World.Companies.First(t => t.IsNpc && game.World.BuildingsOf(t.Id).Any(b => b.Type == "smallFactory"));

        // Selling energy to an ironworks at a fair price: accepted
        Assert.IsTrue(game.World.ProposeContract(player.Id, works.Id, selling: true, "energy", 10, 1.6, 3).Success);
        Assert.AreEqual(ContractState.Active, game.World.Contracts.Last().State);
        // Too expensive: declined
        Assert.IsTrue(game.World.ProposeContract(player.Id, works.Id, selling: true, "ironOre", 10, 7, 3).Success);
        Assert.AreEqual(ContractState.Declined, game.World.Contracts.Last().State);
        // Something it neither makes nor uses: declined
        Assert.IsTrue(game.World.ProposeContract(player.Id, works.Id, selling: false, "glass", 10, 20, 3).Success);
        Assert.AreEqual(ContractState.Declined, game.World.Contracts.Last().State);
    }
}
