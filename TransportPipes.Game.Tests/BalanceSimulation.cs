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
        // The typical day, doing what the advisor says (docs/v1.1-design.html §4)
        ["typical-advisor"] = [(8, 5), (12.5, 5), (18, 5), (21, 20), (23, 5)],
    };

    [TestMethod]
    [DataRow("typical")]
    [DataRow("casual")]
    [DataRow("hardcore")]
    [DataRow("typical-advisor")]
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
        // Like a real server: Loady, achievements, and the surveyors' starter kit on every estate
        var game = new TestGame(seed: 4242, achievements: true, tutorial: true, surveys: true);
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
                bot.CheckIn(advisor: profile.EndsWith("advisor", StringComparison.Ordinal));
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

    /// <summary>
    /// The first session at a human pace (docs/ImplementationStatus.md, M10): a new player follows Loady, one action at
    /// a time with time to look and think in between, then plays on alone. The report shows when each of Loady's steps
    /// is done and where the player has to wait.
    /// </summary>
    [TestMethod]
    public void TheFirstSession()
    {
        var minutes = int.TryParse(Environment.GetEnvironmentVariable("TP_SESSION_MINUTES"), out var m) ? m : 45;
        AtAHumanPace($"The first {minutes} minutes at a human pace", "first-session.md", [(0, minutes)]);
    }

    /// <summary>
    /// The first day at a human pace (GDD §10.2–10.3): the first session of 20 minutes, a short one after three hours,
    /// one in the evening, one before bed, and the next morning
    /// </summary>
    [TestMethod]
    public void TheFirstDay() =>
        AtAHumanPace("The first day at a human pace (sessions at 0:00, 3:00, 10:00, 14:30 and the next morning at 24:00)", "first-day.md",
            [(0, 20), (180, 5), (600, 20), (870, 2), (1440, 10)]);

    /// <summary>
    /// The first day again, but besides Loady the player does only what the advisor says (docs/v1.1-design.html §4):
    /// the first of its next steps it can afford. Compare with first-day.md: the advice should get as far.
    /// </summary>
    [TestMethod]
    public void TheFirstDayWithTheAdvisor() =>
        AtAHumanPace("The first two days following the advisor (day 1: sessions at 0:00, 3:00, 10:00, 14:30; day 2 the same)", "first-day-advisor.md",
            [(0, 20), (180, 5), (600, 20), (870, 2), (1440, 10), (1620, 5), (2040, 20), (2310, 5), (2880, 10)], advisor: true);

    private void AtAHumanPace(string title, string fileName, (double Start, double Minutes)[] sessions, bool advisor = false)
    {
        if (Environment.GetEnvironmentVariable("TP_BALANCE") != "1")
        {
            Assert.Inconclusive("Set TP_BALANCE=1 to run the balance simulation.");
        }
        // Like a real server: Loady, achievements, and the surveyors' starter kit on every estate
        var game = new TestGame(seed: 4242, achievements: true, tutorial: true, surveys: true);
        game.World.TenantsEnabled = true;
        var company = game.NewCompany("Bot", gold: game.World.StartingGold);
        // The server gives a new company today's orders
        game.World.EnsureOrders(company);
        var bot = new Bot(game, company);
        var report = new StringBuilder();
        report.AppendLine($"## {title}");
        report.AppendLine();
        report.AppendLine("| Time | Level | Gold | Gold/min | Loady | What happened |");
        report.AppendLine("| --- | --- | --- | --- | --- | --- |");
        var steps = game.World.Content.Tutorial.Count;
        string Clock(double t)
        {
            var seconds = (int)Math.Round(t * 60);
            return seconds >= 3600 ? $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}" : $"{seconds / 60}:{seconds % 60:00}";
        }
        // Income as of the last tick (an action re-solves the networks only on the next one)
        var income = 0.0;
        void Line(double t, string what) =>
            report.AppendLine($"| {Clock(t)} | {company.Level} | {company.Gold:N0} | {income:N1} | {Math.Min(company.TutorialStep + 1, steps)}/{steps} | {what} |");
        var lastStep = company.TutorialStep;
        var lastLevel = company.Level;
        void Changes(double t)
        {
            income = game.World.GoldPerMinute().GetValueOrDefault(company.Id);
            if (company.TutorialStep != lastStep)
            {
                Line(t, $"**Loady: {string.Join(", ", game.World.Content.Tutorial.Skip(lastStep).Take(company.TutorialStep - lastStep).Select(x => x.Id))} done**");
                lastStep = company.TutorialStep;
            }
            if (company.Level != lastLevel)
            {
                Line(t, $"**Level {company.Level}**");
                lastLevel = company.Level;
            }
        }
        const double tick = 1.0 / 6;
        var now = 0.0;
        for (var index = 0; index < sessions.Length; index++)
        {
            var (start, length) = sessions[index];
            if (start > now)
            {
                // Away: the company runs on its own
                var gold = company.Gold;
                while (now < start)
                {
                    var step = Math.Min(5, start - now);
                    game.Pass(step);
                    now += step;
                    Changes(now);
                }
                Line(now, $"(away for {Clock(start - sessions[index - 1].Start - sessions[index - 1].Minutes)}: +{company.Gold - gold:N0} gold)");
            }
            if (sessions.Length > 1)
            {
                Line(now, $"**Session {index + 1}, {length} minutes**{(game.World.CurrentTutorialStep(company) is { } current ? $" · Loady: {current.Id}{(game.World.TutorialWaitLevel(company, current) is { } wait ? $" (waits for Level {wait})" : "")}" : "")}");
            }
            // Reading Loady's welcome (or the report) first; then each action takes its time (choosing, placing, dragging a pipe)
            var busyUntil = now + 0.5;
            double? waitingSince = null;
            string? waitingFor = null;
            for (; now < start + length - 1e-9; now += tick)
            {
                company.LastSeenAt = game.Time;
                if (now >= busyUntil)
                {
                    var step = game.World.CurrentTutorialStep(company);
                    // Loady first; when that can't be done yet, what a player would do meanwhile (build, upgrade)
                    var (what, seconds) = step != null ? bot.FollowLoady(step) : (null, 0);
                    if (what == null && (advisor ? bot.FollowAdvisor() : bot.Act()))
                    {
                        (what, seconds) = (bot.TakeNotes().TrimStart(' ', '·') is { Length: > 0 } note ? note : "built or upgraded something", 20);
                    }
                    if (what != null)
                    {
                        if (waitingSince is { } since && now - since >= 1)
                        {
                            Line(now, $"(waited {Clock(now - since)}{(waitingFor != null ? $" on \"{waitingFor}\"" : "")})");
                        }
                        waitingSince = null;
                        Line(now, what);
                        busyUntil = now + seconds / 60.0;
                    }
                    else if (waitingSince == null)
                    {
                        waitingSince = now;
                        waitingFor = step?.Id;
                    }
                }
                game.Pass(tick);
                Changes(now + tick);
            }
            if (waitingSince is { } open)
            {
                Line(now, $"(waiting since {Clock(open)}{(waitingFor != null ? $" on \"{waitingFor}\"" : "")})");
            }
        }
        var storages = game.World.BuildingsOf(company.Id).Where(t => t.Type == "storage").ToList();
        report.AppendLine();
        report.AppendLine($"Buildings: {string.Join(", ", game.World.BuildingsOf(company.Id).GroupBy(t => t.Type).Select(t => $"{t.Key} ×{t.Count()}"))}.");
        if (storages.Count > 0)
        {
            report.AppendLine($"Storages: {string.Join("; ", storages.Select(t => string.Join(", ", t.Contents.Select(c => $"{c.Value:N0} {c.Key}"))))}. Orders: {string.Join("; ", company.DailyOrders.Select(t => $"{t.Delivered:N0}/{t.Amount:N0} {t.Resource}"))}");
        }
        TestContext.WriteLine(report.ToString());
        var file = Path.Combine(AppContext.BaseDirectory, fileName);
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

        public void CheckIn(bool advisor = false)
        {
            // Playing counts as being seen (else the company would fall asleep after 30 days)
            company.LastSeenAt = game.Time;
            for (var action = 0; action < 30; action++)
            {
                if (!(advisor ? FollowAdvisor() : Act()))
                {
                    break;
                }
            }
            DeliverOrders();
        }

        /// <summary>One sensible thing to do, if there is one</summary>
        public bool Act() => TryPump() || TryResearch() || TryParcel() || TryChain() || TryUpgrade() || TryProducer();

        /// <summary>The first of the advisor's next steps the company can afford now, done</summary>
        public bool FollowAdvisor()
        {
            var advice = World.NextSteps(company);
            if (advice.Count == 0)
            {
                _notes.Add("no steps");
            }
            foreach (var step in advice)
            {
                if (step.Cost > company.Gold)
                {
                    continue;
                }
                var done = step.Action switch
                {
                    AdvisorAction.Upgrade => World.Upgrade(company.Id, step.Building!.Value, step.Levels).Success,
                    // A human keeps the build tool and puts down as many as it says
                    AdvisorAction.Build => Enumerable.Range(0, step.Count).Count(_ => company.Gold >= Cost(step.BuildingType!) && PlaceAt(step.BuildingType!, step.At)) > 0,
                    AdvisorAction.Research => step.Research != null && World.StartResearch(company.Id, step.Research).Success,
                    AdvisorAction.Parcel => World.BuyParcel(company.Id, step.Parcel!.Value).Success,
                    AdvisorAction.Partner => ConnectNeighbour() != null,
                    AdvisorAction.Pipes => World.UpgradePipes(company.Id, step.Tiles!, step.Tier).Success,
                    AdvisorAction.Connect => ConnectAll(),
                    _ => false,
                };
                if (done)
                {
                    _notes.Add(step.Title);
                    return true;
                }
                // The bot can't always do it (a spot it can't pipe to): the report shows where
                _notes.Add($"could not: {step.Title}");
            }
            return false;
        }

        /// <summary>A building where the advisor said, piped to the network; elsewhere when it can't be piped there</summary>
        private bool PlaceAt(string type, TilePos? at)
        {
            var post = World.BuildingsOf(company.Id).First(t => World.Content.Building(t.Type).Kind == BuildingKind.TradingPost);
            if (at is { } pos && World.PlaceBuilding(company.Id, type, pos).Success)
            {
                var building = World.BuildingAt(pos)!;
                if (TryConnect(building, post))
                {
                    return true;
                }
                World.Demolish(company.Id, building.Id);
            }
            return Place(type, preferMultiplier: true);
        }

        /// <summary>
        /// What Loady says, done the way a new player would (one action, and how many seconds it takes), or null when
        /// it can't be done yet (gold, level)
        /// </summary>
        public (string? What, int Seconds) FollowLoady(TutorialStep step)
        {
            switch (step.Id)
            {
                case "firstPanel":
                    return Place("solarPanel", preferMultiplier: true, connect: false) ? ("built a Solar Panel", 30) : (null, 0);
                case "firstSale":
                    return ConnectAll() ? ("piped the panel to the Trading Post", 25) : (null, 0);
                case "firstUpgrade":
                    var panel = World.BuildingsOf(company.Id).FirstOrDefault(t => t.Type == "solarPanel" && !t.UnderConstruction);
                    return panel != null && World.Upgrade(company.Id, panel.Id, 1).Success ? ("upgraded the panel", 10) : (null, 0);
                case "firstMoily":
                    return company.Gold >= Cost("windMoily") && Place("windMoily", preferMultiplier: true) ? ("built and piped a Wind Moily", 40) : (null, 0);
                case "firstMine":
                    if (!Unlocked("ironMine"))
                    {
                        return (null, 0);
                    }
                    return company.Gold >= Cost("ironMine") && Place("ironMine", preferMultiplier: true) ? ("built and piped an Iron Mine", 40) : (null, 0);
                case "firstIngot":
                    if (Count("smallFactory") > 0)
                    {
                        // Built: the ingot comes when the factory has ore and energy (more energy if it is short)
                        return SpareEnergy() < 0 && company.Gold >= Cost("windMoily") && Place("windMoily", preferMultiplier: true) ? ("built a Wind Moily for the factory", 40) : (null, 0);
                    }
                    return Unlocked("smallFactory") && company.Gold >= Cost("smallFactory") && Place("smallFactory", preferMultiplier: false) ? ("built and piped a Small Factory", 40) : (null, 0);
                case "firstNeighbour":
                    return ConnectNeighbour() is { } neighbour ? ($"piped to {neighbour}", 45) : (null, 0);
                case "firstStorage":
                    return company.Gold >= Cost("storage") && Place("storage", preferMultiplier: false) ? ("built and piped a Storage", 30) : (null, 0);
                case "firstOrder":
                    var delivered = company.DailyOrders.Where(t => !t.Done).Select(t => World.DeliverOrder(company.Id, t.Id)).Count(t => t.Success);
                    return delivered > 0 ? ($"delivered to {delivered} order(s)", 20) : (null, 0);
                case "firstParcel":
                    return TryParcel() ? ("bought a parcel", 20) : (null, 0);
                case "firstResearch":
                    if (Count("lab") == 0)
                    {
                        return Unlocked("lab") && company.Gold >= Cost("lab") && Place("lab", preferMultiplier: false) ? ("built and piped a Lab", 40) : (null, 0);
                    }
                    return company.CurrentResearch == null && TryResearch() ? (TakeNotes().TrimStart(' ', '·'), 20) : (null, 0);
                // Chapter 2
                case "copperIngots":
                case "bricks":
                    return BuildFirstMissing(step) is { } built ? ($"built and piped {Article(built)}", 40) : (null, 0);
                case "firstMilestone":
                    var best = World.BuildingsOf(company.Id).Where(t => t.Type != "hq" && t.Level < 10 && World.CanUpgrade(t)).OrderByDescending(t => t.Level).FirstOrDefault();
                    return best != null && World.UpgradeCost(best, 10 - best.Level) <= company.Gold && World.Upgrade(company.Id, best.Id, 10 - best.Level).Success
                        ? ($"upgraded a {World.Content.Building(best.Type).Name} to level 10", 15) : (null, 0);
                case "threeResearches":
                    return company.CurrentResearch == null && TryResearch() ? (TakeNotes().TrimStart(' ', '·'), 20) : (null, 0);
                case "moreOrders":
                    var orders = company.DailyOrders.Where(t => !t.Done).Select(t => World.DeliverOrder(company.Id, t.Id)).Count(t => t.Success);
                    return orders > 0 ? ($"delivered to {orders} order(s)", 20) : (null, 0);
                case "neighbourIncome":
                    return ConnectNeighbour() is { } partner ? ($"piped to {partner}", 45) : (null, 0);
                case "secondParcel":
                    return TryParcel() ? ("bought a parcel", 20) : (null, 0);
                case "corporation":
                    return World.FoundCorporation(company.Id, "Bot Guild", "BOT", "#3366cc").Success ? ("founded a corporation", 60) : (null, 0);
                default:
                    return (null, 0);
            }
        }

        /// <summary>The first building of a step's chain the company doesn't have yet, built and piped (its name), or null</summary>
        private string? BuildFirstMissing(TutorialStep step)
        {
            foreach (var type in step.HintBuildings)
            {
                if (Count(type) > 0)
                {
                    continue;
                }
                return Unlocked(type) && company.Gold >= Cost(type) && Place(type, preferMultiplier: true) ? World.Content.Building(type).Name : null;
            }
            // All there: what they lack is usually energy
            return SpareEnergy() < 0 && company.Gold >= Cost("windMoily") && Place("windMoily", preferMultiplier: true) ? "Wind Moily" : null;
        }

        private static string Article(string name) => ("AEIOU".Contains(name[0]) ? "an " : "a ") + name;

        /// <summary>Pipes the buildings that aren't on a pipe yet to the network</summary>
        private bool ConnectAll()
        {
            var post = World.BuildingsOf(company.Id).First(t => World.Content.Building(t.Type).Kind == BuildingKind.TradingPost);
            var network = World.Conductors.Where(t => t.Owner == company.Id && t.Kind == ConductorKind.Pipe).Select(t => t.Pos).ToHashSet();
            var loose = World.BuildingsOf(company.Id).Where(t => t != post && World.Content.Building(t.Type).Kind != BuildingKind.Hq && !t.Border().Any(network.Contains)).ToList();
            return loose.Count > 0 && loose.All(t => TryConnect(t, post));
        }

        /// <summary>
        /// A pipe from the network to the nearest gate of a tenant that is short of what the company makes (what the
        /// neighbour card in the game suggests)
        /// </summary>
        private string? ConnectNeighbour()
        {
            var post = World.BuildingsOf(company.Id).First(t => World.Content.Building(t.Type).Kind == BuildingKind.TradingPost);
            var made = World.BuildingsOf(company.Id).SelectMany(t => World.Content.Building(t.Type).Out.Keys).ToHashSet();
            var neighbourhood = World.NeighbourhoodOf(company);
            var gates = World.Companies.Where(t => t.IsNpc && t.District == company.District && neighbourhood.CanSupply(t.Id, made))
                .SelectMany(t => Enum.GetValues<GateSide>().Select(side => (Tenant: t, Outside: WorldLayout.OutsideGate(t.District, t.Slot, side))))
                .OrderBy(t => Math.Abs(t.Outside.X - post.X) + Math.Abs(t.Outside.Y - post.Y));
            var network = World.Conductors.Where(t => t.Owner == company.Id && t.Kind == ConductorKind.Pipe).Select(t => t.Pos).ToList();
            foreach (var (tenant, outside) in gates.Take(6))
            {
                try
                {
                    var pipe = game.Connect(company, network, [outside]);
                    return $"{tenant.Name} ({pipe.Count} tiles of pipe)";
                }
                catch (AssertFailedException)
                {
                    // Too far or too expensive: the next one
                }
            }
            return null;
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
            if (!World.Upgrade(company.Id, best.Building.Id, 1).Success)
            {
                return false;
            }
            _notes.Add($"upgraded a {best.Building.Type} to level {best.Building.Level}");
            return true;
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
        private bool Place(string type, bool preferMultiplier, bool connect = true)
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
                if (!connect || TryConnect(building, post))
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
