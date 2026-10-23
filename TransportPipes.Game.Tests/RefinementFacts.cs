using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>
/// Refinements (after run 3, the design team's step 6): research points have a use after an Era's tree (run 3: Cleo banked
/// 1.6M points by day 21 with every tech locked, while her Lab ate 400 g/min of ingots for nothing)
/// </summary>
[TestClass]
public class RefinementFacts
{
    /// <summary>Ana and Ben in one station; Ana has done Era 1's tree and has points to spend</summary>
    private static (TestGame Game, Company Ana, Company Ben) Station()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", level: 40);
        var ben = game.NewCompany("Ben", level: 40);
        ana.Researched.UnionWith(game.World.Content.ResearchList.Where(t => t.Era <= game.World.Era).Select(t => t.Id));
        ana.ResearchPoints = 1_000_000;
        game.Pass(1);
        return (game, ana, ben);
    }

    [TestMethod]
    public void AfterTheErasTreeTheLabsRefineTheWallsFirstThenTheBestGood()
    {
        var (game, ana, _) = Station();
        game.Build(ana, "windMoily");
        Assert.AreEqual("refine:pipes", ana.CurrentResearch, "Refinements go on by themselves");
        Assert.AreEqual(8_000, GameWorld.RefineCost(ana), "the next costs 5k × 1.6");
        Assert.AreEqual("trading", game.World.NextRefineLine(ana), "and is the next wall");
        var throughput = game.World.ThroughputBonus(ana.Id);

        game.World.FinishResearch(ana.Id);

        Assert.AreEqual(1, ana.Refinements["pipes"]);
        Assert.AreEqual(throughput + 0.10, game.World.ThroughputBonus(ana.Id), 1e-9, "pipes carry +10%");
        Assert.AreEqual("refine:trading", ana.CurrentResearch, "the next starts by itself");
        game.World.FinishResearch(ana.Id);
        Assert.AreEqual("refine:hubOrders", ana.CurrentResearch);
        game.World.FinishResearch(ana.Id);
        Assert.AreEqual("refine:energy", ana.CurrentResearch, "then the good she earns most from");
        Assert.AreEqual(1_000_000 - 5_000 - 8_000 - 12_800 - 20_500, ana.ResearchPoints, 1, "5k × 1.6ⁿ");
    }

    [TestMethod]
    public void TradingPostsSellMoreForTheStation()
    {
        var (game, ana, ben) = Station();
        var post = game.TradingPost(ben);
        var definition = game.World.Content.Building(post.Type);
        var before = game.World.TradingPostThroughput(post, definition);

        ana.Refinements["trading"] = 2;
        game.Pass(1);

        Assert.AreEqual(before * 1.2, game.World.TradingPostThroughput(post, definition), 1e-6, "Ben's post sells +20% by Ana's research");
        Assert.AreEqual(before * 1.2, game.World.ToFlowNode(post)!.Throughput, 1e-6, "and the network sees it");
    }

    [TestMethod]
    public void NoRefinementWhileTheTreeIsOpen()
    {
        var (game, _, ben) = Station();
        ben.ResearchPoints = 1_000_000;

        Assert.IsEmpty(game.World.RefineLines(ben));
        Assert.IsFalse(game.World.StartResearch(ben.Id, "refine:pipes").Success);
        Assert.IsFalse(game.World.LabsRest(ben), "his Lab makes points for the tree");
    }

    [TestMethod]
    public void ARefinementHelpsEveryoneInTheStationWhoRunsThatChainAndSaysWhose()
    {
        var (game, ana, ben) = Station();
        var moily = game.Build(ben, "windMoily");
        var definition = game.World.Content.Building("windMoily");
        var before = game.World.OutputBonus(ben, definition, "energy");
        Assert.IsTrue(game.World.ChooseRefinement(ana.Id, "energy").Success);
        game.World.FinishResearch(ana.Id);
        game.World.TakeChanges();

        game.World.FinishResearch(ana.Id);

        Assert.AreEqual("refine:energy", ana.CurrentResearch, "her choice stays until its cap");
        Assert.AreEqual(before + 0.02, game.World.OutputBonus(ben, definition, "energy"), 1e-9, "Ben's moily makes 2% more");
        var events = game.World.TakeChanges().Events;
        Assert.IsTrue(events.Any(t => t.Company == ben.Id && t.Text == "Energy +2% · Ana's research 💛"), string.Join(" | ", events.Select(t => t.Text)));
        Assert.IsNotNull(moily);
    }

    [TestMethod]
    public void ALineStopsAtFiftyPercentForTheWholeStation()
    {
        var (game, ana, ben) = Station();
        ana.Refinements["pipes"] = 4;
        ben.Refinements["pipes"] = 1;
        ben.Researched.UnionWith(ana.Researched);

        Assert.AreEqual(0.5, game.World.Refined(ana, "pipes"), 1e-9);
        Assert.DoesNotContain("pipes", game.World.RefineLines(ben), "capped for Ben too");
        Assert.IsFalse(game.World.ChooseRefinement(ben.Id, "pipes").Success);
    }

    [TestMethod]
    public void ALabWithNothingLeftToResearchRestsAndEatsNothing()
    {
        var (game, ana, _) = Station();
        game.World.FinishResearch(ana.Id);
        var lab = game.Build(ana, "lab");
        foreach (var line in new[] { "pipes", "trading" }.Concat(game.World.MadeGoods(ana)))
        {
            ana.Refinements[line] = 25;
        }
        ana.Refinements["hubOrders"] = 10;
        game.World.FinishResearch(ana.Id);
        game.Pass(16);

        Assert.IsTrue(game.World.LabsRest(ana));
        Assert.IsTrue(game.World.LabResting(lab));
        Assert.IsNull(game.World.ToFlowNode(lab), "it takes no part in its network");
        Assert.AreEqual(IdleCause.Resting, game.World.IdleReasonOf(lab).Cause);
    }
}
