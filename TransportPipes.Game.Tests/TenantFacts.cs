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
    public void InSmallModeHousesKeepEverySeatWarm()
    {
        var game = WithTenants();
        Assert.IsTrue(game.World.SmallMode, "fewer than 50 active companies");
        var player = game.NewCompany();

        var houses = game.World.Companies.Where(t => t.IsNpc).ToList();
        Assert.HasCount(WorldLayout.EstatesPerDistrict - 1, houses, "every seat but the player's");
        Assert.IsTrue(houses.All(t => t.District == player.District));
        Assert.AreEqual(houses.Count, houses.Select(t => t.Name).Distinct().Count(), "unique names");
        CollectionAssert.AreEquivalent(Enum.GetValues<HouseKind>(), houses.Select(t => t.House!.Value).Distinct().ToArray(), "every kind");
        foreach (var house in houses)
        {
            var buildings = game.World.BuildingsOf(house.Id).ToList();
            Assert.IsGreaterThanOrEqualTo(2, buildings.Count, house.Name);
            // Their pipes reach the Yard gate
            var estate = game.World.EstateOf(house)!;
            Assert.AreSame(game.World.NetworkOf(buildings[0].Id), game.World.NetworkAt(estate.YardGate), house.Name);
            Assert.AreEqual(5, house.Level, "nobody active in the station yet: the least strength");
        }
    }

    [TestMethod]
    public void OutsideSmallModeOnlyTheUsualSeatsGetHouses()
    {
        var game = new TestGame();
        game.World.TenantsEnabled = true;
        game.World.SmallModeBelow = 0;
        game.World.OpenNextDistrict();

        var houses = game.World.Companies.Where(t => t.IsNpc).ToList();
        CollectionAssert.AreEquivalent(GameWorld.TenantSlots, houses.Select(t => t.Slot).ToArray());
        var players = Enumerable.Range(0, WorldLayout.EstatesPerDistrict - GameWorld.TenantSlots.Length).Select(t => game.NewCompany($"P{t}")).ToList();
        Assert.IsTrue(players.All(t => !GameWorld.TenantSlots.Contains(t.Slot)), "Houses keep their seats while there is room");

        var newcomer = game.NewCompany("Newcomer");
        Assert.AreEqual(players[0].District, newcomer.District);
        Assert.IsTrue(GameWorld.TenantSlots.Contains(newcomer.Slot), "then a House moves out");
    }

    [TestMethod]
    public void AHouseLeavesTheNewcomerAStarterKit()
    {
        var game = WithTenants();
        var house = TenantIn(game, 3);
        var producers = game.World.BuildingsOf(house.Id).Where(t => game.World.Content.Building(t.Type).Kind == TransportPipes.Game.Content.BuildingKind.Producer).Select(t => t.Id).ToList();

        var player = game.NewCompany(slot: 3);
        game.World.SolveNetworks();

        Assert.IsNull(game.World.Company(house.Id), "the House moved out");
        var kit = game.World.BuildingsOf(player.Id).Where(t => t.Type is not ("hq" or "tradingPost")).ToList();
        Assert.IsTrue(kit.Count is >= GameWorld.KitMinBuildings and <= GameWorld.KitBuildings, $"{kit.Count} buildings");
        Assert.IsTrue(kit.All(t => game.World.Content.Building(t.Type).Kind == TransportPipes.Game.Content.BuildingKind.Producer
            && game.World.Content.Building(t.Type).In.Count == 0 && t.Level <= GameWorld.KitLevel), "producers that need nothing, at most level five");
        Assert.IsTrue(kit.Any(t => producers.Contains(t.Id)) || producers.All(id => game.World.Building(id) == null), "the House's own where it had them");
        var gate = game.World.GateNetwork(game.EstateOf(player));
        Assert.IsNotNull(gate, "piped to the Yard gate already");
        Assert.IsTrue(kit.Any(t => game.World.NetworkOf(t.Id) == gate));
        Assert.IsTrue(game.World.Changes.Events.Any(t => t.Company == player.Id && t.Text.Contains("left you")), "a note");
        Assert.IsFalse(game.World.Conductors.Any(t => t.Owner == house.Id), "the rest of its pipes are gone");
    }

    [TestMethod]
    public void HousesGrowWithTheirStation()
    {
        var game = WithTenants();
        // Slot 15 is in the south-west station with the Houses in slots 14, 19 and 20
        var player = game.NewCompany(level: 20, slot: 15);
        var house = TenantIn(game, 20);

        game.Pass(24 * 60);

        Assert.AreEqual(17, game.World.HouseStrength(game.EstateOf(player).Station), "the median active level less three");
        Assert.AreEqual(17, house.Level);
        Assert.IsTrue(game.World.BuildingsOf(house.Id).Where(t => game.World.Content.Building(t.Type).Kind == TransportPipes.Game.Content.BuildingKind.Producer).All(t => t.Level == 17));
    }

    [TestMethod]
    public void AHouseWarmsASeatKeptForAFriend()
    {
        var game = new TestGame();
        game.World.TenantsEnabled = true;
        game.World.SmallModeBelow = 0;
        game.World.OpenNextDistrict();
        var ana = game.NewCompany("Ana", slot: 0);
        var (_, crew) = game.World.FoundCrew(ana.Id, "Kettle & Co.", "kettle", "#236b69", "#c9963a", 3);
        game.Pass(24 * 60);

        var held = crew!.Holds.Select(t => t.Slot).ToList();
        Assert.IsTrue(held.All(slot => game.World.Estate(new DistrictCoord(0, 0), slot)?.OwnerId is { } owner && game.World.Company(owner)!.IsNpc), "a House keeps each held seat warm");

        var hold = crew.Holds[0];
        var ben = game.World.CreateCompany(77, "Ben", "#2d6fb4", code: hold.Code);
        Assert.AreEqual(hold.Slot, ben.Slot);
        Assert.IsTrue(game.World.BuildingsOf(ben.Id).Count() > 2, "and leaves the friend its starter kit");
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
        // Everything the player makes counts, its starter kit too
        var made = game.World.BuildingsOf(player.Id).Where(t => game.World.NetworkOf(t.Id) != null).Sum(t =>
            game.World.NetworkOf(t.Id)!.Solution.Flows.Where(f => f.From == t.Id && f.Resource == "energy").Sum(f => f.Sent)
            + game.World.NetworkOf(t.Id)!.GateFlows.Where(f => f.Building == t.Id && !f.Import && f.Resource == "energy").Sum(f => f.Sent));
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
