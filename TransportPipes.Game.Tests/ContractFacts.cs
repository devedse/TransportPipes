using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class ContractFacts
{
    private const double Tolerance = 1e-6;

    /// <summary>A's wind moily (30 energy) next to B's iron mine (needs 10) that sells its ore</summary>
    private static (TestGame Game, Company A, Company B) Partners()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var a = game.NewCompany("A");
        var b = game.NewCompany("B");
        var origin = game.Local(a, 1, 1);
        game.Map(a, origin, " W+");
        game.Map(b, origin, "   M+T");
        return (game, a, b);
    }

    private static List<Flow> Flows(TestGame game, Company owner) =>
        game.World.BuildingsOf(owner.Id).Select(t => game.World.NetworkOf(t.Id)).OfType<Network>().First().Solution.Flows.ToList();

    [TestMethod]
    public void AContractIsServedFirstAtItsOwnPrice()
    {
        var (game, a, b) = Partners();
        Assert.IsTrue(game.World.ProposeContract(a.Id, b.Id, selling: true, "energy", 6, 1.2, 7).Success);
        var contract = game.World.Contracts.Single();
        Assert.AreEqual(ContractState.Offered, contract.State);
        StringAssert.Contains(game.World.AcceptContract(a.Id, contract.Id).Error, "yourself");

        Assert.IsTrue(game.World.AcceptContract(b.Id, contract.Id).Success);
        game.World.SolveNetworks();

        var flows = Flows(game, a);
        var contracted = flows.Single(t => t.Kind == FlowKind.Contract);
        Assert.AreEqual(6, contracted.Delivered, Tolerance);
        Assert.AreEqual(1.2, contracted.PricePerUnit);
        var open = flows.Single(t => t.Kind == FlowKind.Trade);
        Assert.AreEqual(4, open.Delivered, Tolerance, "the rest of what the mine needs is open trade");
        Assert.AreEqual(1.75, open.PricePerUnit);
    }

    [TestMethod]
    public void TheSellerPaysAPenaltyForWhatWasMissing()
    {
        var (game, a, b) = Partners();
        // 20 per minute, but B's mine only uses 10
        game.World.ProposeContract(b.Id, a.Id, selling: false, "energy", 20, 1.5, 1);
        var contract = game.World.Contracts.Single();
        game.World.AcceptContract(a.Id, contract.Id);
        var goldA = a.Gold;
        var goldB = b.Gold;

        game.Pass(60 * 24 + 1);

        Assert.AreEqual(ContractState.Ended, contract.State);
        Assert.AreEqual(0.5, contract.Fulfillment, 1e-3);
        var penalty = (contract.Expected - contract.Delivered) * 1.5 * 0.05;
        Assert.IsGreaterThan(0, penalty);
        Assert.AreEqual(0.9, a.ContractScore, 1e-3, "the seller's reputation drops: 80% old + 20% of 50%");
        Assert.AreEqual(4, GameWorld.ContractStars(a));
        // A earned 10/min for a day at 1.5 minus the penalty; B paid that, got the penalty and sold its ore
        // ... and one more minute of open trade at the (market) auto price after the contract ended
        Assert.AreEqual(goldA + 10 * 1.5 * 60 * 24 - penalty + 10 * game.World.SellPrice(a, "energy"), a.Gold, 0.01, "A's gold");
        Assert.IsGreaterThan(goldB, b.Gold);
    }

    [TestMethod]
    public void AContractGateOnlyLetsContractsThrough()
    {
        var (game, a, b) = Partners();
        Assert.IsTrue(game.World.PathAllowsTrade([], a.Id, b.Id, "energy"));
        game.World.ProposeContract(a.Id, b.Id, selling: true, "energy", 5, 1.5, 3);
        game.World.AcceptContract(b.Id, game.World.Contracts.Single().Id);

        Assert.IsTrue(game.World.HasContract(a.Id, b.Id, "energy"));
        Assert.IsFalse(game.World.HasContract(b.Id, a.Id, "energy"), "contracts have a direction");
    }

    [TestMethod]
    public void AnyoneInTheDistrictCanTakeAnOfferFromTheBoard()
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
        game.World.ProposeContract(a.Id, b.Id, selling: true, "ironOre", 5, 4, 3);
        var (energy, ore) = (game.World.Contracts.First(), game.World.Contracts.Last());

        Assert.IsTrue(game.World.DeclineContract(b.Id, energy.Id).Success);
        Assert.IsTrue(game.World.DeclineContract(a.Id, ore.Id).Success);

        Assert.AreEqual(ContractState.Declined, energy.State);
        Assert.AreEqual(ContractState.Cancelled, ore.State);
        StringAssert.Contains(game.World.AcceptContract(b.Id, energy.Id).Error, "gone");
    }

    [TestMethod]
    public void ContractTermsAreChecked()
    {
        var (game, a, b) = Partners();
        StringAssert.Contains(game.World.ProposeContract(a.Id, b.Id, true, "energy", 5, 100, 3).Error, "price between");
        StringAssert.Contains(game.World.ProposeContract(a.Id, b.Id, true, "energy", 5, 1.5, 60).Error, "1 to 30 days");
        StringAssert.Contains(game.World.ProposeContract(a.Id, b.Id, true, "energy", 0, 1.5, 3).Error, "amount");
        StringAssert.Contains(game.World.ProposeContract(a.Id, a.Id, true, "energy", 5, 1.5, 3).Error, "another company");
        Assert.IsTrue(game.World.ProposeContract(a.Id, b.Id, true, "energy", 5, 1.5, 3).Success);
        StringAssert.Contains(game.World.ProposeContract(a.Id, b.Id, true, "energy", 5, 1.5, 3).Error, "already have");
    }
}
