using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>The advisor's next steps (docs/v1.1-design.html §4): what they suggest, and that their numbers are the game's</summary>
[TestClass]
public class AdvisorFacts
{
    [TestMethod]
    public void AFactoryShortOfEnergyIsTheFirstStep()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 3);
        // A mine and a factory on the trading post's pipes, and no energy at all
        game.Map(company, game.Local(company, 2, 2), "M+F+T");

        var steps = game.World.NextSteps(company);

        var first = steps[0];
        Assert.AreEqual(AdvisorKind.Shortage, first.Kind, string.Join(" | ", steps.Select(t => t.Title)));
        StringAssert.Contains(first.Title, "energy short");
        Assert.AreEqual(AdvisorAction.Build, first.Action);
        Assert.IsTrue(first.BuildingType is "solarPanel" or "windMoily", first.BuildingType);
        Assert.IsGreaterThan(0, first.Cost);
        Assert.IsNotNull(first.At, "a spot where it can stand");
        Assert.IsTrue(game.World.CanPlace(company, game.World.Content.Building(first.BuildingType!), first.At!.Value).Success);
    }

    [TestMethod]
    public void ANeighboursSpareSuggestsAFactoryByTheGate()
    {
        // F6, interdependence: Cleo mines iron ore and sells it to the hub; Ana makes energy and could turn Cleo's ore into ingots
        var game = new TestGame(content: TestGame.PlainContent);
        var ana = game.NewCompany("Ana", level: 3);
        var cleo = game.NewCompany("Cleo", level: 3);
        foreach (var _ in Enumerable.Range(0, 2))
        {
            var moily = game.Build(ana, "windMoily");
            game.ConnectToGate(ana, moily);
            game.Connect(ana, moily, game.TradingPost(ana));
        }
        foreach (var _ in Enumerable.Range(0, 2))
        {
            var mine = game.Build(cleo, "ironMine", game.Spot(cleo, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"));
            game.ConnectToGate(cleo, mine);
            game.Connect(cleo, mine, game.TradingPost(cleo));
        }
        game.Pass(3);

        var steps = game.World.NextSteps(ana, 8);

        var step = steps.FirstOrDefault(t => t.Title.StartsWith("Cleo has"));
        Assert.IsNotNull(step, string.Join(" | ", steps.Select(t => t.Title)));
        StringAssert.Contains(step.Title, "iron ore spare a minute: build a Small Factory by your Yard gate");
        Assert.AreEqual("smallFactory", step.BuildingType);
        Assert.IsGreaterThan(0, step.GainPerMinute);
    }

    [TestMethod]
    public void OresSoldPastTheirHubOrderSuggestTheFactoryThatTurnsThemIntoIngots()
    {
        // The solo bot on the model's plot (B13): nine Copper Mines at level 25 sold 900 ore a minute at a quarter of its
        // price for days, and no Smelter was ever suggested (its ore and coal were priced at the market's 2.5×, so every
        // extractor of the real content ranked before it and the advisor tries eight)
        var game = new TestGame();
        var company = game.NewCompany(gold: 1e9, level: 12);
        var post = game.TradingPost(company);
        // Six mines and four Moilys at level 22, each piped to the post as it stands (89 ore a minute a mine, the Hub
        // Order 333)
        for (var i = 0; i < 10; i++)
        {
            var building = i < 6
                ? game.Build(company, "ironMine", game.Spot(company, "ironMine", t => game.World.Tile(t).Deposit == "ironOre"))
                : game.Build(company, "windMoily");
            game.Connect(company, building, post);
            building.Level = 22;
        }
        game.World.MarkCompanyNetworksDirty(company.Id);
        game.Pass(90);
        Assert.IsGreaterThan(game.World.HubOrder(company, "ironOre"), game.World.SellingNow(company)["ironOre"], "it sells iron ore past its Hub Order");

        var steps = game.World.NextSteps(company, 12);

        Assert.IsTrue(steps.Any(t => t.BuildingType == "smallFactory"), string.Join(" | ", steps.Select(t => t.Title)));
    }

    [TestMethod]
    public void ASpotIsWhereAPipeFromYourNetworkCanReach()
    {
        // B13's bot: every spot it was shown on a full plot had a free tile beside it but no way through
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1e7, level: 12);
        var solar = game.World.Content.Building("solarPanel");
        var post = game.TradingPost(company);
        // A 4×4 block of free land near the post: its ring of twelve panels walls off the 2×2 in the middle
        var corner = Enumerable.Range(3, 10).SelectMany(r => Enumerable.Range(-r, 2 * r + 1).SelectMany(dy => Enumerable.Range(-r, 2 * r + 1).Select(dx => post.Position.Offset(dx, dy))))
            .First(c => GameWorld.Footprint(c, 6).All(t => game.World.CanPlace(company, solar, t).Success));
        var origin = corner.Offset(1, 1);
        var hole = new[] { origin.Offset(1, 1), origin.Offset(2, 1), origin.Offset(1, 2), origin.Offset(2, 2) };
        foreach (var tile in GameWorld.Footprint(origin, 4).Where(t => !hole.Contains(t)))
        {
            game.Build(company, "solarPanel", tile);
        }

        var spot = game.World.FindSpot(company, solar, hole[0]);

        Assert.IsNotNull(spot);
        Assert.IsFalse(hole.Contains(spot.Value), $"not in the walled-off hole: {spot}");
    }

    [TestMethod]
    public void BuildingsThatMineNothingStayOffDeposits()
    {
        // B13's bot: Solar Panels and factories took the plot's iron tiles (13 Iron Mines where the model has 36)
        var game = new TestGame();
        var company = game.NewCompany(gold: 1e6, level: 12);
        var ore = game.Spot(company, "ironMine", t => game.World.Tile(t).Deposit == "ironOre");

        foreach (var type in new[] { "smallFactory", "sawmill", "solarPanel", "windMoily" })
        {
            var definition = game.World.Content.Building(type);
            var spot = game.World.FindSpot(company, definition, ore);
            Assert.IsNotNull(spot, type);
            Assert.IsTrue(GameWorld.Footprint(spot.Value, definition.Size).All(t => game.World.Tile(t).Deposit == null), $"{type} at {spot}");
        }
        Assert.AreEqual("ironOre", game.World.Tile(game.World.FindSpot(company, game.World.Content.Building("ironMine"), ore)!.Value).Deposit, "a mine still goes on its ore");
    }

    [TestMethod]
    public void AFactoryWhoseIngotsCantLeaveIsNotShortOfEnergy()
    {
        // Ana's day 4: the factory piped to the mine and a windmill, the mine to the trading post and a windmill of its
        // own (run 1's network card said "Energy 20 short" for 32 days)
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 3);
        var map = game.Map(company, game.Local(company, 2, 2), "T+M+F+W\n  +\n  W");
        var factory = map[(4, 0)];
        game.Pass(1);

        Assert.IsFalse(game.World.NetworkNeeds(game.World.NetworkOf(factory.Id)!, company.Id)
            .Any(t => t.Value.Consumers.Contains(factory.Id)), "a stuck factory is short of nothing");
        var steps = game.World.NextSteps(company);
        Assert.IsFalse(steps.Any(t => t.Kind == AdvisorKind.Shortage), string.Join(" | ", steps.Select(t => t.Title)));
        var unstick = steps.Single(t => t.Kind == AdvisorKind.Stuck);
        StringAssert.StartsWith(unstick.Title, "Unstick your Small Factory: one pipe");
        StringAssert.Contains(unstick.Reason, "iron ingots can't leave");
        Assert.AreEqual(AdvisorAction.LayPipe, unstick.Action);
        Assert.IsNotNull(unstick.Tiles);
    }

    [TestMethod]
    public void AProducerWhoseOnlyTakerIsStuckSaysSo()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 3);
        var map = game.Map(company, game.Local(company, 2, 2), "W+F");
        game.Pass(1);

        var status = game.World.IdleStatusOf(map[(0, 0)]);

        Assert.AreEqual("Idle: Small Factory is stuck", status.Pill);
        Assert.AreEqual(IdleFixKind.Open, status.Fix!.Kind);
        Assert.AreEqual(map[(2, 0)].Id, status.Fix.Building);
        Assert.AreEqual("⛔ Stuck: iron ingots can't leave", game.World.IdleStatusOf(map[(2, 0)]).Pill);
    }

    [TestMethod]
    public void TheFixPipeStaysOnTheEstateAndReachesThePost()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 3);
        var map = game.Map(company, game.Local(company, 4, 4), "M+F+W");
        var factory = map[(2, 0)];
        game.Pass(1);
        Assert.IsTrue(game.World.IdleReasonOf(factory).IsStuck);

        var plan = game.World.PlanFixPipe(factory)!;

        Assert.AreEqual("tradingPost", plan.Target!.Type);
        Assert.IsTrue(plan.Tiles.All(t => game.EstateOf(company).Contains(t)), "on the owner's estate");
        Assert.AreEqual(plan.NewTiles * game.World.Content.PipeTier(1).Gold, plan.Cost, 1e-9);
        var gold = company.Gold;
        Assert.IsTrue(game.World.PlacePipes(company.Id, plan.Tiles).Success, "the player's own rules allow it");
        Assert.AreEqual(gold - plan.Cost, company.Gold, 1e-6, "it costs what the button says");
        game.Pass(1);
        Assert.AreNotEqual(IdleCause.Stuck, game.World.IdleReasonOf(factory).Cause, "the ingots can leave now");
    }

    [TestMethod]
    public void ADemolishedTypeIsNotSuggestedForAWeek()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");
        Assert.IsTrue(game.World.Demolish(company.Id, moily.Id).Success);

        var steps = game.World.NextSteps(company);

        Assert.IsFalse(steps.Any(t => t.Action == AdvisorAction.Build && t.BuildingType == "windMoily"), string.Join(" | ", steps.Select(t => t.Title)));
        game.Pass(8 * 24 * 60);
        Assert.IsFalse(game.World.DemolishedRecently(company, "windMoily"), "a week later it may come back");
    }

    [TestMethod]
    public void NoTwoStepsShareATitle()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 6, gold: 200_000);
        game.Map(company, game.Local(company, 2, 2), "W+T+W\n\nS+F+M\n\nM+F+S");

        var steps = game.World.NextSteps(company, count: 6);

        Assert.AreEqual(steps.Count, steps.Select(t => t.Title).Distinct().Count(), string.Join(" | ", steps.Select(t => t.Title)));
    }

    [TestMethod]
    public void AnUpgradeStepEarnsWhatItSays()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 50_000);
        var moily = game.Map(company, game.Local(company, 2, 2), "W+T")[(0, 0)];

        var step = game.World.NextSteps(company).First(t => t.Kind is AdvisorKind.Upgrade or AdvisorKind.Full && t.Building == moily.Id);
        var before = game.World.GoldPerMinute()[company.Id];
        Assert.IsTrue(game.World.Upgrade(company.Id, moily.Id, step.Levels).Success);
        game.World.SolveNetworks();
        var after = game.World.GoldPerMinute()[company.Id];

        Assert.AreEqual(after - before, step.GainPerMinute, Math.Max(0.01, step.GainPerMinute * 0.01), "the gain is what the flow solution says");
        Assert.AreEqual(step.Cost / step.GainPerMinute, step.PaybackMinutes!.Value, 1e-9);
    }

    [TestMethod]
    public void ALabIsSuggestedAtLevelFourBesideTheTradingPost()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 3);
        game.Map(company, game.Local(company, 2, 2), "W+M\n + \n +T");
        static AdvisorStep? Lab(TestGame game, Company company) =>
            game.World.NextSteps(company, 12).FirstOrDefault(t => t.Kind == AdvisorKind.Research && t.Action == AdvisorAction.Build);
        Assert.IsNull(Lab(game, company), "the Lab comes at Level 4");

        company.Level = 4;

        var lab = Lab(game, company);
        Assert.IsNotNull(lab, string.Join(" | ", game.World.NextSteps(company, 12).Select(t => t.Title)));
        var post = game.World.BuildingsOf(company.Id).Where(t => t.Type == "tradingPost").MinBy(t => Math.Abs(lab.At!.Value.X - t.X) + Math.Abs(lab.At!.Value.Y - t.Y))!;
        Assert.IsLessThanOrEqualTo(8, Math.Abs(lab.At!.Value.X - post.X) + Math.Abs(lab.At!.Value.Y - post.Y), $"beside a trading post: {lab.At} against ({post.X},{post.Y})");
    }

    [TestMethod]
    public void AFreeLabLineSuggestsTheTierThatAddsMost()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 5);
        game.Map(company, game.Local(company, 2, 2), "W+M\n + \n +T");
        game.Build(company, "lab");
        // The second line is busy: only the track step
        Assert.IsTrue(game.World.StartResearch(company.Id, "copperPipes").Success);
        game.World.SolveNetworks();

        var research = game.World.NextSteps(company, 8).Where(t => t.Kind == AdvisorKind.Research).ToList();

        var step = research.Single(t => t.Research != null && game.World.Content.Tracks.ContainsKey(t.Research));
        Assert.AreEqual(AdvisorAction.Research, step.Action);
        Assert.AreEqual(500, step.Cost, "a first tier: 500 gold");
        Assert.AreEqual("iron", step.Research, "the mine makes more than the moily is worth: Iron adds most");
        Assert.IsNull(game.World.TierBlocker(company, game.World.Content.Tracks[step.Research!]));
    }

    [TestMethod]
    public void StepsNeverSuggestLockedBuildingsAndSayWhenTheyAreAffordable()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 10, level: 2);
        game.Map(company, game.Local(company, 2, 2), "W+M\n + \nF+T");

        var steps = game.World.NextSteps(company, 8);

        Assert.IsNotEmpty(steps);
        foreach (var step in steps.Where(t => t.BuildingType != null && t.Action == AdvisorAction.Build))
        {
            Assert.IsTrue(game.World.IsUnlocked(company, game.World.Content.Building(step.BuildingType!), out _), step.Title);
        }
        foreach (var step in steps.Where(t => t.Cost > company.Gold))
        {
            Assert.IsNotNull(step.WaitMinutes, $"{step.Title} says when");
        }
    }

    [TestMethod]
    public void TheNextParcelShowsWhatItHolds()
    {
        var game = new TestGame();
        var company = game.NewCompany(gold: 1_000_000, level: 5);

        var parcel = game.World.NextSteps(company, 8).SingleOrDefault(t => t.Kind == AdvisorKind.Parcel);

        Assert.IsNotNull(parcel);
        Assert.IsNotNull(parcel.Parcel);
        Assert.IsTrue(game.World.BuyParcel(company.Id, parcel.Parcel!.Value).Success, "a parcel the company can buy");
    }

    [TestMethod]
    public void RightAfterAnUpgradeNothingLooksShort()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 3);
        var map = game.Map(company, game.Local(company, 2, 2), "W+M\n + \nF+T");
        Assert.IsTrue(game.World.Upgrade(company.Id, map[(0, 0)].Id, 9).Success, "the network is rebuilt, not solved yet");

        var steps = game.World.NextSteps(company, 8);

        Assert.IsFalse(steps.Any(t => t.Kind == AdvisorKind.Shortage), string.Join(" | ", steps.Select(t => t.Title)));
    }

    [TestMethod]
    public void AFullPipesStepIsItsPipeDoor()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 4);
        // A moily at level 15 makes 114 energy; a grey pipe carries 100 a minute to the trading post
        var map = game.Map(company, game.Local(company, 2, 2), "W+++T", pipeTier: 1);
        game.SetLevel(map[(0, 0)], 15);
        game.Build(company, "lab");
        game.World.SolveNetworks();

        var step = game.World.NextSteps(company, 8).SingleOrDefault(t => t.Kind == AdvisorKind.Pipes);

        Assert.IsNotNull(step, string.Join(" | ", game.World.NextSteps(company, 8).Select(t => t.Title)));
        Assert.AreEqual(AdvisorAction.Research, step.Action, "below Level 8 a Lab gets Copper Pipes sooner");
        Assert.AreEqual("copperPipes", step.Research);
        StringAssert.Contains(step.Reason, "yours anyway at Company Level 8");

        // Copper Pipes need a copper ingot per tile from the storages: the door lays them, then the levels they carry
        company.Researched.Add("copperPipes");
        game.Map(company, game.Local(company, 10, 2), "O");
        game.World.Building(game.World.BuildingAt(game.Local(company, 10, 2))!.Id)!.Contents["copperIngot"] = 100;
        step = game.World.NextSteps(company, 8).Single(t => t.Kind == AdvisorKind.Pipes);
        Assert.AreEqual(AdvisorAction.Climb, step.Action);
        Assert.AreEqual(map[(0, 0)].Id, step.Building);
        StringAssert.StartsWith(step.Reason, "Copper Pipe on its full pipe (3 tiles)");
        var before = game.World.GoldPerMinute()[company.Id];
        Assert.IsTrue(game.World.ClimbPipe(company.Id, step.Building!.Value).Success);
        // The levels' goods come in (1.4 §9)
        game.World.FillSite(step.Building!.Value);
        game.World.SolveNetworks();

        Assert.IsTrue(game.World.Conductors.Where(t => t.Owner == company.Id && t.Kind == ConductorKind.Pipe).All(t => t.Tier == 2), "laid where they lie");
        Assert.AreEqual(before + step.GainPerMinute, game.World.GoldPerMinute()[company.Id], Math.Max(0.01, step.GainPerMinute * 0.05), "it brings what it said");
        Assert.IsFalse(game.World.NextSteps(company, 8).Any(t => t.Kind == AdvisorKind.Pipes && t.Action == AdvisorAction.Climb && t.Building == map[(0, 0)].Id && t.Reason.StartsWith("Copper")),
            "not full any more, or widening next");
    }

    [TestMethod]
    public void OnlyOwnLowerPipesAreUpgradedAndPaidFor()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 1_000, level: 3);
        var other = game.NewCompany("Other", level: 3);
        company.Researched.Add("copperPipes");
        game.Map(company, game.Local(company, 2, 2), "++", pipeTier: 1);
        game.Map(company, game.Local(company, 2, 3), "+", pipeTier: 2);
        game.Map(other, game.Local(other, 2, 2), "+", pipeTier: 1);
        game.Map(company, game.Local(company, 6, 2), "O");
        game.World.Building(game.World.BuildingAt(game.Local(company, 6, 2))!.Id)!.Contents["copperIngot"] = 10;
        var tiles = new[] { game.Local(company, 2, 2), game.Local(company, 3, 2), game.Local(company, 2, 3), game.Local(other, 2, 2) };

        Assert.IsTrue(game.World.UpgradePipes(company.Id, tiles, 2).Success);

        Assert.AreEqual(2, game.World.ConductorAt(tiles[0])!.Tier);
        Assert.AreEqual(2, game.World.ConductorAt(tiles[1])!.Tier);
        Assert.AreEqual(1, game.World.ConductorAt(tiles[3])!.Tier, "not someone else's");
        var copper = game.World.Content.PipeTier(2);
        Assert.AreEqual(1_000 - 2 * copper.Gold, company.Gold, 1e-9, "two tiles: the copper one was already copper");
        Assert.IsFalse(game.World.UpgradePipes(company.Id, tiles, 2).Success, "nothing left to upgrade");
    }

    [TestMethod]
    public void PipesWithoutTheirIngotsAreOrderedWhenNobodyNearMakesThem()
    {
        // After run 4: the step went quiet when the company made the ingots itself, couldn't make them, or had no Storage
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 10);
        var map = game.Map(company, game.Local(company, 2, 2), "W+++T", pipeTier: 1);
        game.SetLevel(map[(0, 0)], 15);
        game.World.SolveNetworks();
        Assert.IsTrue(game.World.HasResearch(company, "copperPipes"), "everyone's at Company Level 8");

        var step = game.World.NextSteps(company, 8).Single(t => t.Kind == AdvisorKind.Pipes);

        Assert.AreEqual(AdvisorAction.Climb, step.Action, step.Title);
        StringAssert.Contains(step.Reason, "nobody near makes copper ingots");
        var gold = company.Gold;
        Assert.IsTrue(game.World.ClimbPipe(company.Id, map[(0, 0)].Id).Success);
        Assert.IsLessThan(gold, company.Gold, "the pipe and its ingots are paid");
        Assert.IsFalse(company.Asks.Any(t => t.PipeTiles != null), "bought at once: nobody near makes them");
        Assert.IsTrue(game.World.Conductors.Where(t => t.Owner == company.Id && t.Kind == ConductorKind.Pipe).All(t => t.Tier == 2), "the market brought them, the pipe is laid");
    }

    [TestMethod]
    public void ABuildingThatSellsNothingIsPipedFirst()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();
        var moily = game.Build(company, "windMoily");

        var step = game.World.NextSteps(company)[0];

        Assert.AreEqual(AdvisorKind.Start, step.Kind);
        Assert.AreEqual(AdvisorAction.Connect, step.Action);
        Assert.AreEqual(moily.Id, step.Building);
        StringAssert.Contains(step.Title, "Pipe your Wind Moily");
        Assert.IsGreaterThan(0, step.GainPerMinute);
    }

    [TestMethod]
    public void ANewCompanyIsToldWhatToBuildFirst()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany();

        var step = game.World.NextSteps(company).Single();

        Assert.AreEqual(AdvisorKind.Start, step.Kind);
        Assert.AreEqual(AdvisorAction.Build, step.Action);
        Assert.IsTrue(step.BuildingType is "solarPanel" or "windMoily", step.BuildingType);
        Assert.IsTrue(game.World.CanPlace(company, game.World.Content.Building(step.BuildingType!), step.At!.Value).Success);
    }

    [TestMethod]
    public void StoragesKeepTheMaterialsOfThePipesYouCanLay()
    {
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(level: 6);
        Assert.AreEqual(0, game.World.StorageNeed(company, "copperIngot"), "not before Copper Pipes");

        // Company Level 8 brings them
        company.Level = 8;
        game.World.GrantResearch(company);

        Assert.AreEqual(GameWorld.PipeMaterialReserve * game.World.Content.PipeTier(2).Materials["copperIngot"], game.World.StorageNeed(company, "copperIngot"));
    }

    [TestMethod]
    public void ADeepMineSaysWhatItWouldDig()
    {
        // The light bots after run 4 read "Mine your : build a Deep Mine": its deposit is any metal
        var game = new TestGame(content: TestGame.PlainContent);
        var company = game.NewCompany(gold: 10_000_000, level: 30);
        game.TownToEra(2);
        company.Researched.Add("deepMining");
        game.Map(company, game.Local(company, 1, 1), "W+T");
        // Its bricks and planks are in the Storage (an Era-2 building costs older goods too)
        game.Build(company, "storage").Contents["bricks"] = 2000;
        game.World.Stock(company.Id, "planks", 2000);
        var spot = game.Spot(company, "deepMine");
        game.World.LoadSeeds([.. new[] { (0, 0), (1, 0), (0, 1), (1, 1) }.Select(t => (spot.Offset(t.Item1, t.Item2), "coal", 5))]);
        game.Pass(2);

        var steps = game.World.NextSteps(company, 12);

        var step = steps.FirstOrDefault(t => t.BuildingType == "deepMine");
        Assert.IsNotNull(step, string.Join(" | ", steps.Select(t => t.Title)));
        StringAssert.StartsWith(step.Title, "Mine your coal: build a Deep Mine");
    }
}
