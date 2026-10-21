using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class ContractFacts
{
    private const double Tolerance = 1e-6;

    /// <summary>A's wind moily (30 energy) and B's iron mine (needs 10) that sells its ore, neighbours in one station, both piped to their Yard gates</summary>
    private static (TestGame Game, Company A, Company B) Partners()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var a = game.NewCompany("A");
        var b = game.NewCompany("B", level: 2);
        var moily = game.Build(a, "windMoily");
        game.ConnectToGate(a, moily);
        game.Connect(a, moily, game.TradingPost(a));
        var mine = game.Build(b, "ironMine", game.Spot(b, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(b, mine);
        game.Connect(b, mine, game.TradingPost(b));
        return (game, a, b);
    }

    [TestMethod]
    public void AContractIsServedFirstAtThePlayerPrice()
    {
        var (game, a, b) = Partners();
        Assert.IsTrue(game.World.ProposeContract(a.Id, b.Id, selling: true, "energy", 6, 1.2, 7).Success);
        var contract = game.World.Contracts.Single();
        Assert.AreEqual(ContractState.Offered, contract.State);
        Assert.AreEqual(game.World.PlayerPrice("energy"), contract.Price, "one price between players");
        StringAssert.Contains(game.World.AcceptContract(a.Id, contract.Id).Error, "yourself");

        Assert.IsTrue(game.World.AcceptContract(b.Id, contract.Id).Success);
        game.Pass(60);

        Assert.IsGreaterThan(0.95, contract.Fulfillment, $"6 a minute delivered ({contract.Fulfillment:P1})");
        var bought = game.World.YardTrades(game.EstateOf(b)).Where(t => !t.Selling && t.Partner == a.Id).Sum(t => t.PerMinute);
        Assert.IsTrue(bought is >= 10 and < 11, $"the rest of what the mine needs (and its pipe loses) is open trade: {bought:0.##}");
    }

    [TestMethod]
    public void TheSellerPaysAPenaltyForWhatWasMissing()
    {
        var (game, a, b) = Partners();
        // 20 per minute, but B's mine only uses 10
        game.World.ProposeContract(b.Id, a.Id, selling: false, "energy", 20, 1.5, 1);
        var contract = game.World.Contracts.Single();
        game.World.AcceptContract(a.Id, contract.Id);
        var goldB = b.Gold;

        game.Pass(60 * 24 + 1);

        Assert.AreEqual(ContractState.Ended, contract.State);
        Assert.IsTrue(contract.Fulfillment is > 0.5 and < 0.55, $"what the mine took (and its pipe lost): {contract.Fulfillment:P1}");
        var penalty = (contract.Expected - contract.Delivered) * contract.Price * 0.05;
        Assert.IsGreaterThan(0, penalty);
        Assert.AreEqual(0.8 + 0.2 * contract.Fulfillment, a.ContractScore, 1e-3, "the seller's reputation drops: 80% old + 20% of what it delivered");
        Assert.AreEqual(4, GameWorld.ContractStars(a));
        Assert.IsGreaterThan(goldB, b.Gold, "B sold its ore and got the penalty");
    }

    [TestMethod]
    public void ContractsHaveADirection()
    {
        var (game, a, b) = Partners();
        game.World.ProposeContract(a.Id, b.Id, selling: true, "energy", 5, 1.5, 3);
        game.World.AcceptContract(b.Id, game.World.Contracts.Single().Id);

        Assert.IsTrue(game.World.HasContract(a.Id, b.Id, "energy"));
        Assert.IsFalse(game.World.HasContract(b.Id, a.Id, "energy"), "contracts have a direction");
    }

    [TestMethod]
    public void AnyoneInTheStationCanTakeAnOfferFromTheBoard()
    {
        var (game, a, b) = Partners();
        Assert.IsTrue(game.World.ProposeContract(a.Id, null, selling: true, "energy", 5, 1.5, 3).Success);
        var contract = game.World.Contracts.Single();
        Assert.AreEqual(0, contract.Buyer);

        Assert.IsTrue(game.World.AcceptContract(b.Id, contract.Id).Success);
        Assert.AreEqual(b.Id, contract.Buyer);
        Assert.AreEqual(ContractState.Active, contract.State);
        Assert.AreEqual(game.Time.AddDays(3), contract.EndsAt);
    }

    [TestMethod]
    public void OffersCanBeDeclinedAndWithdrawn()
    {
        var (game, a, b) = Partners();
        game.World.ProposeContract(a.Id, b.Id, selling: true, "energy", 5, 1.5, 3);
        game.World.ProposeContract(b.Id, a.Id, selling: true, "ironOre", 5, 4, 3);
        var (energy, ore) = (game.World.Contracts.First(), game.World.Contracts.Last());

        Assert.IsTrue(game.World.DeclineContract(b.Id, energy.Id).Success);
        Assert.IsTrue(game.World.DeclineContract(b.Id, ore.Id).Success);

        Assert.AreEqual(ContractState.Declined, energy.State);
        Assert.AreEqual(ContractState.Cancelled, ore.State);
        StringAssert.Contains(game.World.AcceptContract(b.Id, energy.Id).Error, "gone");
    }

    [TestMethod]
    public void ContractTermsAreChecked()
    {
        var (game, a, b) = Partners();
        StringAssert.Contains(game.World.ProposeContract(a.Id, b.Id, true, "energy", 5, 1.5, 60).Error, "1 to 7 days");
        StringAssert.Contains(game.World.ProposeContract(a.Id, b.Id, true, "energy", 0, 1.5, 3).Error, "amount");
        StringAssert.Contains(game.World.ProposeContract(a.Id, a.Id, true, "energy", 5, 1.5, 3).Error, "another company");
        Assert.IsTrue(game.World.ProposeContract(a.Id, b.Id, true, "energy", 5, 1.5, 3).Success);
        StringAssert.Contains(game.World.ProposeContract(a.Id, b.Id, true, "energy", 5, 1.5, 3).Error, "already have");
    }

    [TestMethod]
    public void AContractNobodyCanMeetCreatesNoGold()
    {
        // Found by reading the code: a buying contract for a million ingots a minute, accepted by a tenant that makes a
        // few, paid its buyer a shortfall penalty of about a billion gold a day
        var game = new TestGame();
        game.World.TenantsEnabled = true;
        var player = game.NewCompany(gold: 10_000, level: 10);
        // Without its starter kit: what it sells would hide a penalty
        foreach (var building in game.World.BuildingsOf(player.Id).Where(t => t.Type is not ("hq" or "tradingPost")).ToList())
        {
            game.World.Demolish(player.Id, building.Id);
        }
        var tenant = game.World.Companies.First(t => t.IsNpc && game.World.CanDeliver(player, t) && game.World.BuildingsOf(t.Id).Any(b => game.World.Content.Building(b.Type).Out.Count > 0));
        var resource = game.World.BuildingsOf(tenant.Id).SelectMany(b => game.World.Content.Building(b.Type).Out.Keys).First();
        var price = game.World.PlayerPrice(resource);
        var before = player.Gold;

        game.World.ProposeContract(player.Id, tenant.Id, selling: false, resource, rate: 1_000_000, price: price, days: 1);
        game.Pass(24 * 60 + 30);

        Assert.IsLessThan(before + 10_000, player.Gold, $"no penalty gold from a contract nobody could meet ({player.Gold - before:N0} gold more)");
        Assert.IsGreaterThanOrEqualTo(0, tenant.Gold, "a penalty never makes anyone's gold negative");
    }

    [TestMethod]
    public void APlayerCanOnlyPromiseWhatTheyCanMake()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var seller = game.NewCompany("Seller", gold: 100_000, level: 10);
        var buyer = game.NewCompany("Buyer", gold: 100_000, level: 10);
        game.Map(seller, game.Local(seller, 2, 2), "W+T");
        var price = game.World.PlayerPrice("energy");

        Assert.IsFalse(game.World.ProposeContract(seller.Id, buyer.Id, selling: true, "energy", rate: 5_000, price: price, days: 1).Success, "a moily makes 30 a minute");
        Assert.IsTrue(game.World.ProposeContract(seller.Id, buyer.Id, selling: true, "energy", rate: 25, price: price, days: 1).Success);
    }
}
