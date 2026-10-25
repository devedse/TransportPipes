using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>Trunk lines between towns (1.4 §20): built from Era 3, owned by their carrier, carrying links and a second market</summary>
[TestClass]
public class TrunkLineFacts
{
    /// <summary>
    /// Ana's Wind Moily in the first town and Ben's Iron Mine (it needs energy) in the town next to it, both piped to
    /// their Yard gates; both towns in Era 3; Cleo, in Ana's town, may build lines
    /// </summary>
    private static (TestGame Game, Company Ana, Company Ben, Company Cleo, DistrictCoord There) TwoTowns()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", gold: 1e12, level: 30, slot: 0);
        var cleo = game.NewCompany("Cleo", gold: 1e12, level: 30, slot: 5);
        var there = new DistrictCoord(1, 0);
        while (!game.World.OpenDistricts.Contains(there))
        {
            game.World.OpenNextDistrict();
        }
        var ben = game.World.CreateCompany(game.World.Companies.Count() + 1, "Ben", "#3366ff", seat: new EstateKey(there, 12));
        ben.Gold = 1e12;
        ben.Level = 30;
        game.World.GrantResearch(ben);
        game.TownToEra(3);
        game.World.AdvanceEra(there);
        game.World.AdvanceEra(there);
        foreach (var company in new[] { ana, cleo })
        {
            company.Researched.UnionWith(["trunkLines", "compositePipes"]);
            // A composite line takes plastic from your Storages
            game.Build(company, "storage").Contents["plastic"] = 2000;
        }

        var moily = game.Build(ana, "windMoily");
        game.ConnectToGate(ana, moily);
        game.Connect(ana, moily, game.TradingPost(ana));
        var mine = game.Build(ben, "ironMine", game.Spot(ben, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
        game.ConnectToGate(ben, mine);
        game.Connect(ben, mine, game.TradingPost(ben));
        game.World.SolveNetworks();
        return (game, ana, ben, cleo, there);
    }

    [TestMethod]
    public void ALineRunsToANeighbouringTownAndWhoeverBuildsItFirstIsItsCarrier()
    {
        var (game, ana, ben, cleo, there) = TwoTowns();
        var plan = game.World.PlanLine(cleo.Id, there, 4);
        Assert.IsNull(plan.Error, plan.Error);
        Assert.IsGreaterThanOrEqualTo(WorldLayout.WildernessWidth, plan.Length, "across the wilderness between the towns");
        var gold = cleo.Gold;

        var built = game.World.BuildLine(cleo.Id, there, 4);
        Assert.IsTrue(built.Success, built.Error);

        Assert.AreEqual(gold - plan.Gold, cleo.Gold, 1);
        var line = game.World.LineBetween(ana.District, there)!;
        Assert.AreEqual(cleo.Id, line.Owner);
        StringAssert.Contains(game.World.BuildLine(ana.Id, there, 4).Error, "first", "one line between two towns: the first builder has it");
        StringAssert.Contains(game.World.PlanLine(ben.Id, ana.District, 4).Error, "Trunk Lines", "Ben hasn't researched it");
    }

    [TestMethod]
    public void ALinkToAnotherTownRunsOverTheLineAndPaysItsCarrierAFee()
    {
        var (game, ana, ben, cleo, there) = TwoTowns();
        StringAssert.Contains(game.World.PlanLink(ana.Id, ben.Id, "energy", selling: true, tier: 1).Error, "trunk line", "no line yet");

        var built = game.World.BuildLine(cleo.Id, there, 4);
        Assert.IsTrue(built.Success, built.Error);
        var line = game.World.LineBetween(ana.District, there)!;
        var plan = game.World.PlanLink(ana.Id, ben.Id, "energy", selling: true, tier: 1);
        Assert.IsNull(plan.Error, plan.Error);
        Assert.AreEqual(game.World.LinkEfficiency(1, plan.Length - line.Length) * game.World.LineEfficiency(line), plan.Efficiency, 1e-9,
            "the link's pipe to the gates, the line's loss across the wilderness");

        var (offer, link) = game.World.OfferLink(ana.Id, ben.Id, "energy", selling: true);
        Assert.IsTrue(offer.Success, offer.Error);
        Assert.IsTrue(game.World.AcceptLink(ben.Id, link!.Id).Success);
        var carrier = cleo.Gold;
        game.Pass(60);

        Assert.IsGreaterThan(0, link.Rate, "energy reaches Ben's mine over the line");
        Assert.IsGreaterThan(carrier, cleo.Gold, "Cleo earns her fee");
        Assert.AreEqual(GameWorld.DefaultLineFee, game.World.LineFee(line, ana), 1e-12);
        Assert.IsGreaterThan(0, line.Carried);
    }

    [TestMethod]
    public void AGoodSentOverALineSellsAtTheOtherTownsMarketAgainstHalfItsHubOrder()
    {
        var (game, ana, _, cleo, there) = TwoTowns();
        var built = game.World.BuildLine(cleo.Id, there, 4);
        Assert.IsTrue(built.Success, built.Error);
        var line = game.World.LineBetween(ana.District, there)!;
        var home = game.World.HubOrder(ana, "energy");

        Assert.IsTrue(game.World.SetExport(ana.Id, "energy", line.Id).Success);

        var abroad = game.World.BaseHubOrder(ana, there, "energy");
        Assert.AreEqual(home + GameWorld.ExportHubOrderShare * abroad * game.World.LineEfficiency(line), game.World.HubOrder(ana, "energy"), 1e-6);
        Assert.IsTrue(game.World.SetExport(ana.Id, "energy", null).Success);
        Assert.AreEqual(home, game.World.HubOrder(ana, "energy"), 1e-9, "stopped");
    }
}
