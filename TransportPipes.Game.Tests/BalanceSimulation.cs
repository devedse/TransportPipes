using System.Text;
using TransportPipes.Game.Content;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>
/// The balance check (docs/ImplementationStatus.md, M8): a bot plays the first two weeks through the game's API, like
/// a sensible player, and the report shows when it reaches each Company Level. Slow: run it with TP_BALANCE=1.
/// </summary>
[TestClass]
public class BalanceSimulation
{
    public TestContext TestContext { get; set; } = null!;

    /// <summary>Sessions per day as (start hour, length in minutes); the player checks in every 5 minutes during one</summary>
    private static readonly Dictionary<string, (double Start, int Minutes)[]> Profiles = new()
    {
        // GDD §10.7, a typical day in week 2
        ["typical"] = [(8, 5), (12.5, 5), (18, 5), (21, 20), (23, 5)],
        ["casual"] = [(8, 5), (20, 15)],
        ["hardcore"] = [(8, 120), (12, 60), (17, 240)],
    };

    [TestMethod]
    [DataRow("typical")]
    [DataRow("casual")]
    [DataRow("hardcore")]
    public void TheFirstTwoWeeks(string profile)
    {
        if (Environment.GetEnvironmentVariable("TP_BALANCE") != "1")
        {
            Assert.Inconclusive("Set TP_BALANCE=1 to run the balance simulation.");
        }
        if (Environment.GetEnvironmentVariable("TP_BALANCE_PROFILE") is { Length: > 0 } only && only != profile)
        {
            Assert.Inconclusive($"Only {only}.");
        }
        var days = int.TryParse(Environment.GetEnvironmentVariable("TP_BALANCE_DAYS"), out var d) ? d : 14;
        var game = new TestGame(seed: 4242, achievements: true, tutorial: true);
        var company = game.NewCompany("Bot", gold: game.World.StartingGold);
        var bot = new Bot(game, company);
        var report = new StringBuilder();
        var sessions = Profiles[profile];
        report.AppendLine($"## {profile}: sessions at {string.Join(", ", sessions.Select(t => $"{t.Start:0.#}h ({t.Minutes} min)"))}");
        report.AppendLine($"XP for levels 5, 10, 22, 30, 40, 60: {string.Join(", ", new[] { 5, 10, 22, 30, 40, 60 }.Select(t => game.World.Content.Progression.XpForLevel(t).ToString("N0")))}");
        report.AppendLine();
        report.AppendLine("| Time | Level | Gold | Gold/min | Buildings | Parcels | What happened |");
        report.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
        var lastLevel = company.Level;
        var nextSample = 0;
        // The first session starts at 8:00 on day 1 and lasts 20 minutes (the first-session target of GDD §10.2)
        const int step = 5;
        for (var minute = 0; minute < days * 24 * 60; minute += step)
        {
            var dayMinute = (minute + 8 * 60) % (24 * 60);
            var playing = minute < 20 || sessions.Any(t => dayMinute >= t.Start * 60 && dayMinute < t.Start * 60 + t.Minutes);
            if (playing)
            {
                bot.CheckIn();
            }
            try
            {
                game.Pass(step);
            }
            catch (KeyNotFoundException)
            {
                var networks = game.World.Networks.Where(t => t.Solution.Flows.Any(f => game.World.Building(f.From) == null || game.World.Building(f.To) == null)).ToList();
                TestContext.WriteLine($"minute {minute}, era {game.World.Era}, stale networks: {string.Join("; ", networks.Select(n => $"{n.Id} members {string.Join(",", n.Buildings)} needsSolve {n.NeedsSolve} flows {string.Join(",", n.Solution.Flows.Select(f => $"{f.From}->{f.To}"))}"))}");
                TestContext.WriteLine("notes: " + bot.TakeNotes());
                throw;
            }
            if (company.Level != lastLevel || minute >= nextSample)
            {
                var income = game.World.GoldPerMinute().GetValueOrDefault(company.Id);
                var note = company.Level != lastLevel ? $"Level {company.Level}" : "";
                note += bot.TakeNotes();
                report.AppendLine($"| {Time(minute + step)} | {company.Level} | {company.Gold:N0} | {income:N1} | {game.World.BuildingsOf(company.Id).Count()} | {game.EstateOf(company).UnlockedCount} | {note} |");
                lastLevel = company.Level;
                nextSample = minute + 12 * 60;
            }
        }
        // Where the gold comes from at the end: sales per resource and the hub's price against its base
        report.AppendLine();
        report.AppendLine("| Resource | Sold/min | Price | Base price | Buildings making it |");
        report.AppendLine("| --- | --- | --- | --- | --- |");
        var sales = game.World.Networks.SelectMany(t => t.Solution.Flows).Where(t => t.Kind == Simulation.FlowKind.Sale && game.World.Building(t.From)?.Owner == company.Id)
            .GroupBy(t => t.Resource).OrderByDescending(t => t.Sum(f => f.Delivered * f.PricePerUnit));
        foreach (var sale in sales)
        {
            var makers = game.World.BuildingsOf(company.Id).Count(t => game.World.Content.Building(t.Type).Out.ContainsKey(sale.Key));
            report.AppendLine($"| {sale.Key} | {sale.Sum(t => t.Delivered):N1} | {sale.Average(t => t.PricePerUnit):N2} | {game.World.Content.Resources[sale.Key].Price:N2} | {makers} |");
        }
        var posts = game.World.BuildingsOf(company.Id).Where(t => game.World.Content.Building(t.Type).Kind == BuildingKind.TradingPost).ToList();
        report.AppendLine();
        report.AppendLine($"Trading posts: {string.Join(", ", posts.Select(t => $"{t.Type} level {t.Level}, sells up to {game.World.TradingPostThroughput(t, game.World.Content.Building(t.Type)):N0}/min, full: {game.World.TradingPostFull(t)}"))}");
        TestContext.WriteLine(report.ToString());
        var file = Path.Combine(AppContext.BaseDirectory, $"balance-{profile}.md");
        File.WriteAllText(file, report.ToString());
        TestContext.WriteLine(file);
    }

    private static string Time(int minutes) => minutes < 24 * 60 ? $"{minutes / 60}h{minutes % 60:00}" : $"day {minutes / (24 * 60) + 1} {minutes / 60 % 24}h";

    /// <summary>A sensible player: energy first, then chains, upgrades with a quick payback, parcels and research</summary>
    private sealed class Bot(TestGame game, Company company)
    {
        private readonly List<string> _notes = new();
        private GameWorld World => game.World;

        public string TakeNotes()
        {
            var text = _notes.Count == 0 ? "" : (" · " + string.Join(", ", _notes.Distinct()));
            _notes.Clear();
            return text;
        }

        public void CheckIn()
        {
            // Playing counts as being seen (else the company would fall asleep after 30 days)
            company.LastSeenAt = game.Time;
            for (var action = 0; action < 30; action++)
            {
                if (!TryPump() && !TryResearch() && !TryParcel() && !TryChain() && !TryUpgrade() && !TryProducer())
                {
                    break;
                }
            }
            DeliverOrders();
        }

        private bool Unlocked(string type) => World.IsUnlocked(company, World.Content.Building(type), out _);

        private double Cost(string type) => World.BuildCost(company, World.Content.Building(type));

        private double Income => World.GoldPerMinute().GetValueOrDefault(company.Id);

        private int Count(string type) => World.BuildingsOf(company.Id).Count(t => t.Type == type);

        /// <summary>Energy left over in the company's networks (made minus used), per minute</summary>
        private double SpareEnergy()
        {
            double made = 0, used = 0;
            foreach (var building in World.BuildingsOf(company.Id).Where(t => !t.UnderConstruction))
            {
                if (World.ToFlowNode(building) is { } node)
                {
                    made += node.Out.GetValueOrDefault("energy");
                    used += node.In.GetValueOrDefault("energy");
                }
            }
            return made - used;
        }

        /// <summary>Energy producers: the best value for the gold where they make the most</summary>
        private bool TryProducer()
        {
            var options = new[] { "windMoily", "solarPanel", "bigTurbine", "solarFarm" }.Where(t => World.Content.Buildings.ContainsKey(t) && Unlocked(t)).ToList();
            var best = options.Select(t => (Type: t, Value: World.Content.Building(t).Out.GetValueOrDefault("energy") / Cost(t))).OrderByDescending(t => t.Value).FirstOrDefault();
            if (best.Type == null || Cost(best.Type) > company.Gold)
            {
                return false;
            }
            return Place(best.Type, preferMultiplier: true);
        }

        /// <summary>A mine and its factory: ore into ingots (and energy for them)</summary>
        private bool TryChain()
        {
            foreach (var (mine, factory) in new[] { ("ironMine", "smallFactory"), ("copperMine", "smelter"), ("coalPit", "brickworks") })
            {
                if (!Unlocked(mine) || !Unlocked(factory) || Count(mine) > Count(factory) || Count(mine) >= 3)
                {
                    continue;
                }
                if (Count(mine) == Count(factory) && Count(mine) > 0 && company.Gold < (Cost(mine) + Cost(factory)) * 1.5)
                {
                    continue;
                }
                var need = World.Content.Building(mine).In.GetValueOrDefault("energy") + World.Content.Building(factory).In.GetValueOrDefault("energy");
                if (SpareEnergy() < need && company.Gold > Cost("windMoily"))
                {
                    Place("windMoily", preferMultiplier: true);
                }
                if (Count(mine) == Count(factory) && company.Gold > Cost(mine) && Place(mine, preferMultiplier: true))
                {
                    _notes.Add($"built a {mine}");
                    return true;
                }
                if (Count(mine) > Count(factory) && company.Gold > Cost(factory) && Place(factory, preferMultiplier: false))
                {
                    _notes.Add($"built a {factory}");
                    return true;
                }
            }
            return false;
        }

        /// <summary>The upgrade with the quickest payback, if it pays back within two hours of current income</summary>
        private bool TryUpgrade()
        {
            // A full trading post first: it caps everything else (a player sees "Sells all it can")
            var post = World.BuildingsOf(company.Id).FirstOrDefault(World.TradingPostFull);
            if (post != null && World.UpgradeCost(post, 1) <= company.Gold)
            {
                if (World.Content.Building(post.Type).EvolvesTo != null && World.EvolvePlan(company, post).Problem == null && World.Evolve(company.Id, post.Id).Success)
                {
                    _notes.Add("evolved the trading post");
                    return true;
                }
                return World.Upgrade(company.Id, post.Id, 1).Success;
            }
            var best = World.BuildingsOf(company.Id)
                .Where(t => !t.UnderConstruction && World.Content.Building(t.Type).Out.Count > 0)
                .Select(t => (Building: t, Cost: World.UpgradeCost(t, 1)))
                .Where(t => t.Cost <= company.Gold)
                .OrderBy(t => t.Cost)
                .FirstOrDefault();
            if (best.Building == null || best.Cost > Math.Max(200, Income * 240))
            {
                return false;
            }
            return World.Upgrade(company.Id, best.Building.Id, 1).Success;
        }

        /// <summary>A network short of pump stations gets one (an alert tells a player the same)</summary>
        private bool TryPump()
        {
            if (!Unlocked("pumpStation") || company.Gold < Cost("pumpStation") || !World.Alerts(company).Any(t => t.Text.Contains("Pump Station")))
            {
                return false;
            }
            if (Place("pumpStation", preferMultiplier: false))
            {
                _notes.Add("built a pump station");
                return true;
            }
            return false;
        }

        private bool TryParcel()
        {
            var (cost, level) = World.NextParcel(company);
            if (company.Level < level || company.Gold < cost * 1.3 || cost <= 0)
            {
                return false;
            }
            for (var parcel = 0; parcel < WorldLayout.ParcelCount; parcel++)
            {
                if (World.BuyParcel(company.Id, parcel).Success)
                {
                    _notes.Add("bought a parcel");
                    return true;
                }
            }
            return false;
        }

        private bool TryResearch()
        {
            if (Unlocked("lab") && Count("lab") == 0 && company.Gold > Cost("lab") * 2 && Place("lab", preferMultiplier: false))
            {
                _notes.Add("built a lab");
                return true;
            }
            if (company.CurrentResearch != null)
            {
                return false;
            }
            // What holds the company back first (pumps for a big network), then the cheapest
            var wanted = World.Alerts(company).Any(t => t.Text.Contains("Pump Station")) ? "pumpStations" : null;
            var tech = World.Content.ResearchList.Where(t => World.ResearchBlocker(company, t) == null)
                .OrderBy(t => t.Id == wanted ? 0 : 1).ThenBy(t => World.ResearchCost(company, t)).FirstOrDefault();
            if (tech != null && company.ResearchPoints >= World.ResearchCost(company, tech) && World.StartResearch(company.Id, tech.Id).Success)
            {
                _notes.Add($"researching {tech.Name}");
                return true;
            }
            return false;
        }

        private void DeliverOrders()
        {
            foreach (var order in company.DailyOrders.ToList())
            {
                World.DeliverOrder(company.Id, order.Id);
            }
        }

        /// <summary>Places a building close to the trading post (or where it makes the most) and pipes it to the network</summary>
        private bool Place(string type, bool preferMultiplier)
        {
            var definition = World.Content.Building(type);
            var estate = game.EstateOf(company);
            var post = World.BuildingsOf(company.Id).FirstOrDefault(t => World.Content.Building(t.Type).Kind == BuildingKind.TradingPost)
                ?? throw new InvalidOperationException($"No trading post: {string.Join(",", World.BuildingsOf(company.Id).GroupBy(t => t.Type).Select(t => $"{t.Key}×{t.Count()}"))}; notes {string.Join(",", _notes)}");
            var spots = new List<(TilePos Pos, double Score)>();
            for (var y = 0; y < WorldLayout.EstateSize; y++)
            {
                for (var x = 0; x < WorldLayout.EstateSize; x++)
                {
                    var pos = estate.Origin.Offset(x, y);
                    if (!World.CanPlace(company, definition, pos).Success)
                    {
                        continue;
                    }
                    var distance = Math.Abs(pos.X - post.X) + Math.Abs(pos.Y - post.Y);
                    var multiplier = preferMultiplier ? World.LocationMultiplier(new Building { Type = type, X = pos.X, Y = pos.Y, Size = definition.Size }, definition) : 1;
                    spots.Add((pos, multiplier * 100 - distance));
                }
            }
            foreach (var spot in spots.OrderByDescending(t => t.Score).Take(12))
            {
                if (!World.PlaceBuilding(company.Id, type, spot.Pos).Success)
                {
                    return false;
                }
                var building = World.BuildingAt(spot.Pos)!;
                if (TryConnect(building, post))
                {
                    return true;
                }
                // No way to pipe it: take it back
                World.Demolish(company.Id, building.Id);
            }
            return false;
        }

        private bool TryConnect(Building building, Building post)
        {
            var network = World.Conductors.Where(t => t.Owner == company.Id && t.Kind == ConductorKind.Pipe).Select(t => t.Pos).ToHashSet();
            if (building.Border().Any(network.Contains))
            {
                return true;
            }
            var goals = network.Concat(post.Border().Where(t => !World.IsOccupied(t))).ToList();
            try
            {
                game.Connect(company, building.Border().Where(t => !World.IsOccupied(t) || network.Contains(t)), goals);
                return true;
            }
            catch (AssertFailedException)
            {
                return false;
            }
        }
    }
}
