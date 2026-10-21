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
        game.World.OpenNextDistrict();
        return game;
    }

    /// <summary>A tenant in the given slot of the Capital</summary>
    private static Company TenantIn(TestGame game, int slot) =>
        game.World.Companies.Single(t => t.IsNpc && t.Slot == slot && t.District == new DistrictCoord(0, 0));

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
            // Their pipes reach the Yard gate
            var estate = game.World.EstateOf(tenant)!;
            Assert.AreSame(game.World.NetworkOf(buildings[0].Id), game.World.NetworkAt(estate.YardGate), tenant.Name);
        }
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
        Assert.AreEqual(1, game.World.Conductors.Count(t => estate.Contains(t.Pos)), "only the newcomer's Yard gate");
    }

    [TestMethod]
    public void ANewcomerCanChooseATenantsSeat()
    {
        var game = WithTenants();
        var tenant = TenantIn(game, GameWorld.TenantSlots[0]);

        var player = game.NewCompany(slot: tenant.Slot);

        Assert.AreEqual(GameWorld.TenantSlots[0], player.Slot);
        Assert.IsNull(game.World.Company(tenant.Id), "the tenant moved out");
    }

    [TestMethod]
    public void AWorksTenantInTheStationBuysAQuarterOfAPlayersEnergy()
    {
        // Slot 15 shares the south-west station with the Ironworks in slot 20 and the Brickworks in slot 14
        var game = WithTenants();
        var works = TenantIn(game, 20);
        Assert.IsTrue(game.World.BuildingsOf(works.Id).Any(t => t.Type == "smallFactory"));
        var player = game.NewCompany(level: 2, slot: 15);
        var moilies = Enumerable.Range(0, 3).Select(_ => game.Build(player, "windMoily")).ToList();
        foreach (var moily in moilies)
        {
            game.Connect(player, moily, game.TradingPost(player));
            game.ConnectToGate(player, moily);
        }

        game.Pass(5);

        var estate = game.EstateOf(player);
        var sold = game.World.YardTrades(estate).Where(t => t.Selling && game.World.Company(t.Partner)!.IsNpc && t.Resource == "energy").Sum(t => t.PerMinute);
        var made = moilies.Sum(t => game.World.NetworkOf(t.Id)!.Solution.Flows.Where(f => f.From == t.Id && f.Resource == "energy").Sum(f => f.Sent)
            + game.World.NetworkOf(t.Id)!.GateFlows.Where(f => f.Building == t.Id && !f.Import).Sum(f => f.Sent));
        Assert.IsTrue(sold > 0, "the works buy the player's spare energy");
        Assert.IsTrue(sold <= 0.25 * made + 1e-6, $"Houses buy at most a quarter of what a player makes ({sold:0.##} of {made:0.##})");
        Assert.AreEqual(Math.Min(game.World.HubBuyPrice(player, "energy") * 1.25, 1.25 * game.World.BasePrice("energy")), game.World.HouseBuyPrice(player, "energy"), 1e-9);
    }

    [TestMethod]
    public void APlayerChoosesWhoItTradesWith()
    {
        var game = WithTenants();
        var works = TenantIn(game, 20);
        var player = game.NewCompany(level: 2, slot: 15);
        var moily = game.Build(player, "windMoily");
        game.Connect(player, moily, game.TradingPost(player));
        game.ConnectToGate(player, moily);
        bool Trades()
        {
            game.Pass(3);
            return game.World.YardTrades(game.EstateOf(player)).Any(t => t.Selling && t.Partner == works.Id && t.PerMinute > 1e-6);
        }
        Assert.IsTrue(Trades());

        Assert.IsTrue(game.World.SetTradeBlocked(player.Id, works.Id, true).Success);
        game.Pass(60);
        Assert.IsFalse(Trades(), "not with them, after an hour's notice");
        Assert.IsTrue(game.World.SetTradeBlocked(player.Id, works.Id, false).Success);
        Assert.IsTrue(Trades());

        Assert.IsFalse(game.World.SetTradeWith(player.Id, TradeWith.Corporation).Success, "needs a corporation");
        Assert.IsTrue(game.World.SetTradeWith(player.Id, TradeWith.Contracts).Success);
        Assert.IsFalse(Trades(), "contract partners only");
    }

    [TestMethod]
    public void TenantsAnswerContractOffersRightAway()
    {
        var game = WithTenants();
        var works = TenantIn(game, 20);
        var player = game.NewCompany(level: 2, slot: 15);
        // A contract promises only what the player makes: a moily
        game.Build(player, "windMoily");

        // Selling energy to an ironworks: accepted, at the one player price
        Assert.IsTrue(game.World.ProposeContract(player.Id, works.Id, selling: true, "energy", 1, 0, 3).Success);
        Assert.AreEqual(ContractState.Active, game.World.Contracts.Last().State);
        Assert.AreEqual(game.World.PlayerPrice("energy"), game.World.Contracts.Last().Price);
        // Something it neither makes nor uses: declined
        Assert.IsTrue(game.World.ProposeContract(player.Id, works.Id, selling: false, "glass", 10, 20, 3).Success);
        Assert.AreEqual(ContractState.Declined, game.World.Contracts.Last().State);
        // Someone in another station: no way for the goods
        var far = TenantIn(game, 2);
        StringAssert.Contains(game.World.ProposeContract(player.Id, far.Id, selling: true, "energy", 1, 0, 3).Error, "station");
    }
}
