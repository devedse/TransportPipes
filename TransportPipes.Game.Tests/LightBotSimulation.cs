using System.Text;
using TransportPipes.Game.Content;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>
/// The light bots (step 5 of the redesign, docs/playtest-redesign.html §7): human-size players on dealt land decide the
/// one price. A crew of four in one corner station (Ben stops on day 3, Dev arrives on day 8), Mia in the small station
/// next door and Jo alone in another; 20 minutes every second evening, at most 15 buildings, doing what the advisor
/// says. Each runs at 1.3 and 1.5, with today's 1.75 as the baseline. Slow: run it with TP_BALANCE=1.
/// </summary>
[TestClass]
public class LightBotSimulation
{
    public TestContext TestContext { get; set; } = null!;

    private sealed class Player(string name, int startDay, int? stopDay)
    {
        public string Name { get; } = name;
        public int StartDay { get; } = startDay;
        public int? StopDay { get; } = stopDay;
        public Company? Company { get; set; }
        public BalanceSimulation.Bot? Bot { get; set; }
        public int? Level10Day { get; set; }

        /// <summary>Minutes its processors ran at a loss on what they bought, and minutes they were short of a bought input</summary>
        public double LossMinutes { get; set; }
        public HashSet<string> LossTypes { get; } = new();
        public double ShortMinutes { get; set; }
        public double ChainMinutes { get; set; }

        /// <summary>The highest price it paid for each good it bought, and the largest step up between two samples</summary>
        public Dictionary<string, double> LastPrice { get; } = new();
        public double LargestPriceStep { get; set; }

        /// <summary>Per sample: income per building, for solo against crew (days 10 on)</summary>
        public List<double> IncomePerBuilding { get; } = new();

        /// <summary>Paid trade with players and all production gold by day 14</summary>
        public double PaidByDay14 { get; set; }
        public double EarnedByDay14 { get; set; }

        /// <summary>Gold held on day 28 in days of income (after run 3: the advisor's bot should bank ≤ 3)</summary>
        public double? BankedDays28 { get; set; }

        public bool PlaysOn(int day) => day >= StartDay && (StopDay == null || day <= StopDay) && (day - StartDay) % 2 == 0;
    }

    [TestMethod]
    [DataRow(1.3)]
    [DataRow(1.5)]
    [DataRow(1.75)]
    public void ThePriceOnDealtLand(double price)
    {
        if (Environment.GetEnvironmentVariable("TP_BALANCE") != "1")
        {
            Assert.Inconclusive("Set TP_BALANCE=1 to run the light bots.");
        }
        if (Environment.GetEnvironmentVariable("TP_BALANCE_PRICE") is { Length: > 0 } only && double.Parse(only, System.Globalization.CultureInfo.InvariantCulture) != price)
        {
            Assert.Inconclusive($"Only {only}.");
        }
        var days = int.TryParse(Environment.GetEnvironmentVariable("TP_BALANCE_DAYS"), out var d) ? d : 36;
        var lever = Environment.GetEnvironmentVariable("TP_BALANCE_LEVER") is { Length: > 0 } l ? l : null;
        var game = new TestGame(seed: 4242, content: Lever(lever), tutorial: true, surveys: true, dealtLand: true);
        var world = game.World;
        world.TenantsEnabled = true;
        world.ProjectsEnabled = true;
        world.PlayerPriceFactor = price;
        world.OpenNextDistrict();

        // The crew's corner station, the small station next to it, and another corner station for Jo
        var district = new DistrictCoord(0, 0);
        var stations = WorldLayout.Stations(district).ToList();
        var crewStation = stations.First(t => WorldLayout.StationSlots(t.Index).Count == 4);
        var small = stations.First(t => WorldLayout.StationSlots(t.Index).Count == 2 && WorldLayout.CornersNextTo(t.Index).Contains(crewStation.Index));
        var joStation = stations.Last(t => WorldLayout.StationSlots(t.Index).Count == 4 && t != crewStation && !WorldLayout.CornersNextTo(small.Index).Contains(t.Index));
        var crewSlots = WorldLayout.StationSlots(crewStation.Index);
        var seats = new Dictionary<string, int>
        {
            ["Ana"] = crewSlots[0],
            ["Ben"] = crewSlots[1],
            ["Cleo"] = crewSlots[2],
            ["Dev"] = crewSlots[3],
            ["Mia"] = WorldLayout.StationSlots(small.Index)[0],
            ["Jo"] = WorldLayout.StationSlots(joStation.Index)[0],
        };
        var players = new List<Player>
        {
            new("Ana", 1, null), new("Ben", 1, 3), new("Cleo", 1, null), new("Dev", 8, null), new("Mia", 1, null), new("Jo", 1, null),
        };
        var crew = new[] { "Ana", "Ben", "Cleo", "Dev" };

        var report = new StringBuilder();
        report.AppendLine($"## The light bots at {price:0.00}{(lever != null ? $", lever \"{lever}\"" : "")}");
        report.AppendLine();
        report.AppendLine($"Seats: {string.Join(", ", seats.Select(t => $"{t.Key} {string.Join("+", world.Generator.LandOf(district, t.Value))}"))}");
        report.AppendLine();
        report.AppendLine("| Day | " + string.Join(" | ", players.Select(t => t.Name)) + " |");
        report.AppendLine("| --- |" + string.Join("", players.Select(_ => " --- |")));

        const int session = 20;
        for (var day = 1; day <= days; day++)
        {
            // The day until 20:00, then the evening's session (a check-in every 5 minutes), then the night to midnight
            game.Pass(20 * 60);
            foreach (var player in players.Where(t => t.Company == null && t.StartDay == day))
            {
                player.Company = game.NewCompany(player.Name, gold: world.StartingGold, slot: seats[player.Name]);
                player.Bot = new BalanceSimulation.Bot(game, player.Company) { MaxBuildings = 15 };
            }
            for (var minute = 0; minute < session; minute += 5)
            {
                foreach (var player in players.Where(t => t.Company != null && t.PlaysOn(day)))
                {
                    player.Bot!.CheckIn(advisor: true);
                }
                game.Pass(5);
            }
            game.Pass(4 * 60 - session);
            Sample(world, players, day);
            if (day == 14)
            {
                foreach (var player in players.Where(t => t.Company != null))
                {
                    player.PaidByDay14 = player.Company!.Stats.GetValueOrDefault("yardPaid");
                    player.EarnedByDay14 = Production(player.Company);
                }
            }
            if (day == 28)
            {
                foreach (var player in players.Where(t => t.Company != null))
                {
                    player.BankedDays28 = player.Company!.Gold / Math.Max(1, world.GoldPerMinute().GetValueOrDefault(player.Company.Id) * 1440);
                }
            }
            report.AppendLine($"| {day} | " + string.Join(" | ", players.Select(t => t.Company is { } c
                ? $"L{c.Level} {world.GoldPerMinute().GetValueOrDefault(c.Id):0}/min {world.BuildingsOf(c.Id).Count()}b"
                : "—")) + " |");
        }

        // The marks (§7)
        report.AppendLine();
        report.AppendLine("| Mark | Pass | Value |");
        report.AppendLine("| --- | --- | --- |");
        var crewPlayers = players.Where(t => crew.Contains(t.Name) && t.Company != null).ToList();
        var paidShare = crewPlayers.Sum(t => t.PaidByDay14) / Math.Max(1, crewPlayers.Sum(t => t.EarnedByDay14));
        var paidNow = crewPlayers.Sum(t => t.Company!.Stats.GetValueOrDefault("yardPaid")) / Math.Max(1, crewPlayers.Sum(t => Production(t.Company!)));
        report.AppendLine($"| Paid trade between humans by day 14 | ≥ 15% of the crew's output | {paidShare:P1} (day {days}: {paidNow:P1}) |");
        var loss = players.Sum(t => t.LossMinutes);
        report.AppendLine($"| Buildings running at a loss on their supply | 0 minutes | {loss:N0} building-minutes ({string.Join(", ", players.Where(t => t.LossMinutes > 0).Select(t => $"{t.Name} {t.LossMinutes:N0}"))}) |");
        var jo = players.Single(t => t.Name == "Jo");
        var crewPerBuilding = crewPlayers.Where(t => t.IncomePerBuilding.Count > 0).Select(t => t.IncomePerBuilding.Average()).DefaultIfEmpty(0).Average();
        var soloShare = jo.IncomePerBuilding.Count == 0 || crewPerBuilding <= 0 ? 0 : jo.IncomePerBuilding.Average() / crewPerBuilding;
        report.AppendLine($"| Solo against crew, per building (days 10 on) | 85–92% | {soloShare:P0} (Jo {jo.IncomePerBuilding.DefaultIfEmpty(0).Average():0.0} g/min, crew {crewPerBuilding:0.0}) |");
        report.AppendLine($"| Pacing: Level 10 on days 4–6 | days 4–6 | {string.Join(", ", players.Select(t => $"{t.Name} {(t.Level10Day?.ToString() ?? "—")}"))} |");
        var npc = players.Where(t => t.Company != null).Sum(t => t.Company!.Stats.GetValueOrDefault("npcGold"));
        var faucets = players.Where(t => t.Company != null).Sum(t => Production(t.Company!));
        report.AppendLine($"| New gold from NPCs | ≤ 6% of faucets | {npc / Math.Max(1, faucets):P1} |");
        var ana = players.Single(t => t.Name == "Ana");
        var supplied = ana.ChainMinutes <= 0 ? 1 : 1 - ana.ShortMinutes / ana.ChainMinutes;
        report.AppendLine($"| The advisor's bot banks on day 28 | ≤ 3 days of income | {string.Join(", ", players.Where(t => t.BankedDays28 != null).Select(t => $"{t.Name} {t.BankedDays28:0.0}"))} |");
        report.AppendLine($"| Ben's stop: Ana's chains get their need | ≥ 99% to day {days}; price step ≤ +8% | {supplied:P1}; largest step {ana.LargestPriceStep:P1} |");
        report.AppendLine();
        report.AppendLine("| Player | Gold | Level | Buildings | Hub | Yard paid | Houses paid | Project | Floor |");
        report.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var player in players.Where(t => t.Company != null))
        {
            var c = player.Company!;
            report.AppendLine($"| {player.Name} | {c.Gold:N0} | {c.Level} | {world.BuildingsOf(c.Id).Count()} | {c.Stats.GetValueOrDefault("hubGold"):N0} | {c.Stats.GetValueOrDefault("yardPaid"):N0} | {c.Stats.GetValueOrDefault("npcGold"):N0} | {c.Stats.GetValueOrDefault("projectGold"):N0} | {c.Stats.GetValueOrDefault("yardFloor"):N0} |");
        }
        report.AppendLine();
        foreach (var player in players.Where(t => t.Company != null))
        {
            report.AppendLine($"- {player.Name}: {string.Join(", ", world.BuildingsOf(player.Company!.Id).GroupBy(t => t.Type).Select(t => $"{t.Count()}× {t.Key} L{t.Max(b => b.Level)}"))}"
                + (player.LossTypes.Count > 0 ? $"; at a loss: {string.Join(", ", player.LossTypes.Take(6))}" : ""));
        }
        TestContext.WriteLine(report.ToString());
        var file = Path.Combine(AppContext.BaseDirectory, $"light-bots-{price:0.00}{(lever != null ? $"-{lever}" : "")}.md");
        File.WriteAllText(file, report.ToString());
        TestContext.WriteLine(file);
    }

    /// <summary>
    /// A content lever for friends' trade (run 2's recommendation 7, light-bots.md): "coal" makes iron ingots need coal,
    /// the one good every station holds once (a Small Factory: 4 coal a minute, a Big Factory a third of its ore);
    /// "order" halves the iron ingots' Hub Order, so a fifth factory pays less than a first chain that needs a neighbour
    /// </summary>
    private static GameContent Lever(string? lever)
    {
        var content = GameContent.Default;
        if (lever is null or "none")
        {
            return content;
        }
        var resources = content.ResourceList.Select(t => lever == "order" && t.Id == "ironIngot" ? t with { Demand = t.Demand * 0.5 } : t).ToList();
        var buildings = content.BuildingList.Select(t => lever != "coal" || t.Id is not ("smallFactory" or "bigFactory") ? t : new BuildingDefinition
        {
            Id = t.Id,
            Name = t.Name,
            Category = t.Category,
            Kind = t.Kind,
            Size = t.Size,
            Buildable = t.Buildable,
            Unlock = t.Unlock,
            Cost = t.Cost,
            BuildSeconds = t.BuildSeconds,
            In = new Dictionary<string, double>(t.In) { ["coal"] = t.Id == "smallFactory" ? 4 : Math.Round(t.In.GetValueOrDefault("ironOre") / 3) },
            Out = t.Out,
            OutFromDeposit = t.OutFromDeposit,
            ResearchPerMinute = t.ResearchPerMinute,
            Placement = t.Placement,
            Modifier = t.Modifier,
            Capacity = t.Capacity,
            Throughput = t.Throughput,
            PriceBonus = t.PriceBonus,
            Range = t.Range,
            Reach = t.Reach,
            Layer = t.Layer,
            EvolvesTo = t.EvolvesTo,
            Sprite = t.Sprite,
            Description = t.Description,
        }).ToList();
        return new GameContent(resources, buildings, content.PipeTiers, content.Progression, content.ResearchList, [.. content.DistrictWonders, .. content.EraWonders], content.Managers, content.Achievements, content.Collections, content.Tutorial, content.Relics);
    }

    /// <summary>All production gold a company earned (the faucets): the hub, the Yard, its floor, Houses, the project</summary>
    private static double Production(Company company) =>
        new[] { "hubGold", "yardPaid", "yardFloor", "npcGold", "projectGold", "askPaid" }.Sum(company.Stats.GetValueOrDefault);

    /// <summary>
    /// Once a day: who reached Level 10, the processors running at a loss on what they buy or short of a bought input
    /// (counted for the four hours after the evening), the prices paid, income per building
    /// </summary>
    private static void Sample(GameWorld world, List<Player> players, int day)
    {
        foreach (var player in players.Where(t => t.Company != null))
        {
            var company = player.Company!;
            if (company.Level >= 10 && player.Level10Day == null)
            {
                player.Level10Day = day;
            }
            if (world.EstateOf(company) is not { } estate)
            {
                continue;
            }
            var imports = world.YardTrades(estate).Where(t => !t.Selling && t.PerMinute > 1e-6).ToList();
            var prices = imports.GroupBy(t => t.Resource).ToDictionary(t => t.Key,
                t => t.Sum(x => x.PerMinute * world.YardPrice(company, world.Company(x.Partner)!, x.Resource, selling: false)) / t.Sum(x => x.PerMinute));
            foreach (var (resource, paid) in prices)
            {
                if (player.LastPrice.TryGetValue(resource, out var before) && before > 0)
                {
                    player.LargestPriceStep = Math.Max(player.LargestPriceStep, paid / before - 1);
                }
                player.LastPrice[resource] = paid;
            }
            foreach (var building in world.BuildingsOf(company.Id).Where(t => !t.UnderConstruction))
            {
                var definition = world.Content.Building(building.Type);
                var bought = definition.In.Keys.Where(t => t != "energy" && prices.ContainsKey(t)).ToList();
                if (bought.Count == 0)
                {
                    continue;
                }
                player.ChainMinutes += 4 * 60;
                if (world.IdleReasonOf(building) is { Cause: IdleCause.InputMissing or IdleCause.InputShort, Resource: var short_ } && bought.Contains(short_!))
                {
                    player.ShortMinutes += 4 * 60;
                }
                // At list prices: what its goods are worth against what its supply costs (bought at the price paid, its own at
                // the base), so the hub's lower price for a flood of ingots doesn't count as the price's fault
                var value = definition.Out.Sum(t => t.Value * world.BasePrice(t.Key));
                var cost = definition.In.Sum(t => t.Value * (prices.TryGetValue(t.Key, out var p) ? p : world.BasePrice(t.Key)));
                if (value < cost)
                {
                    player.LossMinutes += 4 * 60;
                    player.LossTypes.Add($"{building.Type} L{building.Level} ({value:0}<{cost:0})");
                }
            }
            if (day >= 10)
            {
                var count = world.BuildingsOf(company.Id).Count(t => world.Content.Building(t.Type).Kind == BuildingKind.Producer);
                if (count > 0)
                {
                    player.IncomePerBuilding.Add(world.GoldPerMinute().GetValueOrDefault(company.Id) / count);
                }
            }
        }
    }
}
