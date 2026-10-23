using TransportPipes.Game.Content;
using TransportPipes.Game.State;
using TransportPipes.Game.World;
using TransportPipes.Server.Dtos;

namespace TransportPipes.Server.Services;

public static partial class GameViews
{
    /// <summary>The return card shows at most this many lines (the lowest folded first)</summary>
    private const int ReturnLines = 6;

    /// <summary>The order of the card's sections</summary>
    private static readonly string[] ReturnSections = ["people", "look", "project", "social", "today", "invest"];

    /// <summary>What the card needs from before the Hello woke the company and started its next report</summary>
    public sealed record AwayFacts(TimeSpan Away, CompanyReport Report, string? StandIn, bool WasAsleep, bool FromIsles);

    /// <summary>
    /// The return card (F5): one card for every return with news, built on the server's clock. Gold and level, then at
    /// most six lines in this order: your people (a stand-in, resting friends, who moved in, who took your goods, a
    /// tend, who answered you in the district chat), what to look at (storm damage with Repair all, a stuck building,
    /// crates, a full storage), the project, a social line, today in the district, and the Invest line. When there are
    /// more, the lowest go first: the Invest line, then today.
    /// </summary>
    public static ReturnCardDto? ReturnCard(GameWorld world, Company me, AwayFacts away, string? chatLine)
    {
        var report = away.Report;
        var lines = new List<(int Rank, ReturnLineDto Line)>();
        void Add(int rank, string kind, string icon, string text, string? action = null, long? building = null, double? cost = null, long? partner = null, string? resource = null, string? partnerName = null) =>
            lines.Add((rank, new ReturnLineDto(kind, icon, text, action, building, cost, partner, resource, partnerName)));
        string Kind(Building building) => world.Content.Building(building.Type).Name;

        // Your people
        if (away.StandIn != null)
        {
            Add(1, "people", "⚙", $"{away.StandIn} stood in for you: your neighbours' trades with you went on");
        }
        // Friends and neighbours who rest: their seat is kept, and from day 30 a stand-in keeps your trades going
        var mates = (world.CrewOf(me) is { } crew ? world.CrewMembers(crew) : [])
            .Concat(world.EstateOf(me) is { } home ? world.StationPlayers(home.Station) : [])
            .Where(t => t.Id != me.Id && !t.IsNpc && world.ActivityOf(t) != Activity.Active)
            .DistinctBy(t => t.Id);
        var resting = new List<string>();
        foreach (var mate in mates)
        {
            if (mate.StandIn is { } standIn)
            {
                Add(2, "people", "⚙", $"{world.NameFor(mate, me)} is resting: {standIn.Name} stands in, so your trades go on");
            }
            else if (mate.StandInAnnounced)
            {
                Add(2, "people", "⚙", $"{world.NameFor(mate, me)} hasn't played for three weeks: from next week a stand-in House keeps your trades going");
            }
            else
            {
                resting.Add(world.NameFor(mate, me));
            }
        }
        if (resting.Count > 0)
        {
            Add(4, "people", "💤", resting.Count == 1 ? $"{resting[0]} is resting: the seat is kept" : $"{And(resting)} are resting: their seats are kept");
        }
        if (report.NewNeighbours.Count > 0)
        {
            Add(2, "people", "🏘", $"{And(report.NewNeighbours.Distinct().Take(3).ToList())} moved in next door");
        }
        // What your goods did for them, as a sale with its money (run 2: "X took N of your energy" read as taking without
        // asking); the first time they ran a neighbour's building by themselves: make it yours, Send ▸. The first such
        // line says once that the Yard sells your spare by itself.
        var taken = world.TakenSince(me, report.Since)
            .Where(t => world.Company(t.Key.Buyer) is { IsNpc: false })
            .OrderByDescending(t => t.Value * world.BasePrice(t.Key.Resource))
            .Take(2)
            .ToList();
        string Sold(Company buyer, string resource, double units) => $"+{Amount(units * world.YardPrice(me, buyer, resource, selling: true))} g";
        var why = taken.Count > 0 && !me.YardSalesExplained ? " (the Yard sells your spare to neighbours who lack it, at the one price, by itself)" : "";
        if (taken.FirstOrDefault(t => world.OfferSendOnce(me, world.Company(t.Key.Buyer)!, t.Key.Resource)) is { Key.Buyer: > 0 } offer)
        {
            var buyer = world.Company(offer.Key.Buyer)!;
            var building = world.TradeNeed(buyer, offer.Key.Resource);
            Add(2, "people", "💛", $"Your {world.GoodsWord(offer.Key.Resource)} ran {world.PossessiveFor(buyer, me)} {building ?? "buildings"} while you were away: {Amount(offer.Value)} of it, {Sold(buyer, offer.Key.Resource, offer.Value)}{why}. Make it yours:",
                "send", null, null, buyer.Id, offer.Key.Resource);
        }
        else if (taken.Count > 0)
        {
            Add(3, "people", "🤝", string.Join("; ", taken.Select((t, i) =>
            {
                var buyer = world.Company(t.Key.Buyer)!;
                var building = world.TradeNeed(buyer, t.Key.Resource);
                return i == 0
                    ? $"{world.PossessiveFor(buyer, me)} {building ?? "buildings"} bought {Amount(t.Value)} of your {world.GoodsWord(t.Key.Resource)} · {Sold(buyer, t.Key.Resource, t.Value)}"
                    : $"{world.NameFor(buyer, me)} {Amount(t.Value)} {world.GoodsWord(t.Key.Resource)} · {Sold(buyer, t.Key.Resource, t.Value)}";
            })) + why);
        }
        if (why.Length > 0)
        {
            me.YardSalesExplained = true;
            me.Touch();
        }
        // What a neighbour's goods did for you: the biggest, with 💛 Thanks (run 2: Dev asked how to feed the Lab that Ana's
        // ingots were feeding, and Thanks was never pressed)
        var got = world.BoughtSince(me, report.Since)
            .Where(t => world.Company(t.Key.Seller) is { IsNpc: false })
            .OrderByDescending(t => t.Value * world.BasePrice(t.Key.Resource))
            .FirstOrDefault();
        if (got.Value > 1e-6 && world.Company(got.Key.Seller) is { } giver)
        {
            var building = world.TradeNeed(me, got.Key.Resource);
            Add(2, "people", "💛", $"{world.PossessiveFor(giver, me)} {world.GoodsWord(got.Key.Resource)} ran your {building ?? "buildings"} while you were away: {Amount(got.Value)} of it",
                world.ThankedToday(me, giver.Id) ? null : "thank", null, null, giver.Id, got.Key.Resource, world.NameFor(giver, me));
        }
        if (chatLine != null)
        {
            Add(3, "people", "💬", chatLine);
        }
        // A friend's storm damage, with 💛 Tend (run 2: no tend happened, because nobody saw a friend's damage)
        if ((world.CrewOf(me) is { } myCrew ? world.CrewMembers(myCrew) : []).FirstOrDefault(t => world.CanTend(me, t)) is { } hurt)
        {
            var count = world.TendRepairs(hurt).Count;
            Add(3, "people", "💛", $"The storm damaged {world.PossessiveFor(hurt, me)} {(count == 1 ? Kind(world.TendRepairs(hurt)[0]) : $"{count} buildings")} while they're away",
                "tend", null, null, hurt.Id, null, world.NameFor(hurt, me));
        }
        if (report.TendedDays > 0)
        {
            Add(4, "people", "💛", $"Your crew repaired your storm damage on {Days(report.TendedDays)}");
        }

        // To look at
        var buildings = world.BuildingsOf(me.Id).Where(t => !t.UnderConstruction).ToList();
        var damaged = buildings.Where(t => t.Damaged).ToList();
        if (damaged.Count > 0)
        {
            Add(1, "look", "⛈", damaged.Count == 1
                    ? $"The storm damaged your {Kind(damaged[0])}: half speed until it's repaired"
                    : $"The storm damaged {damaged.Count} of your buildings: half speed until they're repaired",
                "repairAll", damaged[0].Id, damaged.Sum(world.RepairCost));
        }
        var stuck = buildings.Where(t => world.IdleReasonOf(t).IsStuck).ToList();
        if (stuck.Count > 0)
        {
            var good = world.IdleReasonOf(stuck[0]).Resource is { } resource ? world.GoodsWord(resource) : "goods";
            Add(1, "look", "⛔", stuck.Count == 1
                    ? $"Your {Kind(stuck[0])} is stuck: its {good} can't leave"
                    : $"{stuck.Count} buildings are stuck: your {Kind(stuck[0])}'s {good} can't leave",
                "show", stuck[0].Id);
        }
        // A building running short, not stuck (run 2: Cleo's mines ran at 33% for weeks and no card said so)
        if (stuck.Count == 0 && buildings
                .Select(t => (Building: t, Status: world.IdleStatusOf(t, plan: false), Rate: world.NetworkOf(t.Id)?.Solution.Rate(t.Id) ?? 0))
                .Where(t => t.Status.Reason.Cause is IdleCause.InputShort or IdleCause.InputMissing or IdleCause.PipeFull && t.Rate < 0.6
                    && world.Content.Building(t.Building.Type).Kind is BuildingKind.Producer)
                .OrderByDescending(t => world.Content.Building(t.Building.Type).Out.Sum(o => o.Value * world.BasePrice(o.Key)) * t.Building.Level)
                .FirstOrDefault() is { Building: not null } shortOne)
        {
            Add(2, "look", "⚠", shortOne.Rate < 0.005 || shortOne.Status.Reason.Cause == IdleCause.InputMissing
                    ? $"Your {Kind(shortOne.Building)} is idle: {(shortOne.Status.Pill ?? "").Replace("Idle: ", "")}"
                    : shortOne.Status.Reason.Cause == IdleCause.PipeFull
                    ? $"Your {Kind(shortOne.Building)} runs at {shortOne.Rate * 100:0}%: {(shortOne.Status.Pill ?? "").Replace("Slowed: ", "")}"
                    : $"Your {Kind(shortOne.Building)} runs at {shortOne.Rate * 100:0}%: {(shortOne.Status.Reach ?? shortOne.Status.Pill ?? "").TrimEnd('.').Replace("Its pipes", "its pipes")}",
                "show", shortOne.Building.Id);
        }
        if (me.Crates.Count > 0)
        {
            Add(2, "look", "📦", $"{Plural(me.Crates.Count, "building")} didn't fit: {(me.Crates.Count == 1 ? "it waits" : "they wait")} in a crate in the Move tab");
        }
        if (world.FullStorages(me.Id).FirstOrDefault() is { } full)
        {
            var (holds, waiting, _) = world.StorageLine(full);
            Add(waiting != null ? 3 : 6, "look", "🛢", waiting != null
                ? $"Your {Kind(full)} is full of {holds}: {waiting} slow behind it"
                : $"Your {Kind(full)} is full of {holds}: what comes now goes on to your trading post", "show", full.Id);
        }

        // Link offers that waited for you (F11)
        var offers = world.LinksOf(me.Id).Where(t => t.State == LinkState.Offered && t.OfferedBy != me.Id).ToList();
        if (offers.FirstOrDefault() is { } linkOffer && world.Company(linkOffer.OfferedBy) is { } offerer)
        {
            var good = world.GoodsWord(linkOffer.Resource);
            Add(2, "look", "🔗", offers.Count == 1
                    ? $"{world.NameFor(offerer, me)} offers a link: {(linkOffer.Seller == me.Id ? $"your {good} to them" : $"their {good} to you")}"
                    : $"{offers.Count} link offers wait for you, {world.PossessiveFor(offerer, me)} first",
                "links");
        }

        // A friend keeps a seat for you, and you opened its link while living elsewhere: on every return until you answer
        // (run 3: three of Ben's cards said nothing of Ana's seat while he asked "which station am i in?")
        if (world.KeptSeatFor(me) is { } seatKept)
        {
            var from = world.Company(seatKept.Crew.Founder) is { } founder ? world.NameFor(founder, me) : seatKept.Crew.Name;
            var there = WorldNames.Station(WorldLayout.StationOf(seatKept.Hold.Seat.District, seatKept.Hold.Seat.Slot));
            Add(1, "people", "🪑", $"{from} keeps a seat for you in {there}{(world.EstateOf(me) is { } myHome ? $" (you live in {WorldNames.Station(myHome.Station)})" : "")}: move in, free", "seat");
        }
        // A neighbour's scaffold you could help with (after run 3, step 2): your Storage at a tap, or your spare going there
        if (world.WantsNear(me).OrderByDescending(t => t.Holds).FirstOrDefault() is { Owner: not null } near)
        {
            var name = near.Name;
            var who = world.NameFor(near.Owner, me);
            Add(2, "people", "🧱", near.Holds >= 1
                    ? $"{who}'s {name} wants {near.Want.Left:N0} {world.GoodsWord(near.Want.Resource)}: your Storage holds {Math.Floor(near.Holds):N0}"
                    : $"{who}'s {name} wants {near.Want.Left:N0} {world.GoodsWord(near.Want.Resource)}: your spare goes there first",
                near.Holds >= 1 ? "fillWant" : null, near.Target);
        }
        // The crew's side: a friend who opened your link but lives elsewhere, and where
        if (world.CrewOf(me) is { } ownCrew)
        {
            foreach (var hold in ownCrew.Holds.Where(t => t.OpenedBy is { } who && who != me.Id && !t.Declined))
            {
                if (world.Company(hold.OpenedBy!.Value) is { } friend && friend.CrewId != ownCrew.Id)
                {
                    Add(2, "people", "🪑", $"{hold.FriendName ?? friend.Name} opened your link but lives in {(world.EstateOf(friend) is { } theirs ? WorldNames.Station(theirs.Station) : "another station")}: send it again from the Crew panel, or visit", "crew");
                    break;
                }
            }
        }

        // How the last Era ended, and your part (after run 3, step 5: "Era 1 ended without me knowing the Dynamo result")
        if (me.EraEnded is { } ended && world.EraStartedAt >= report.Since)
        {
            Add(1, "project", "🏛", ended);
        }

        // The project
        if (world.ProjectsEnabled && world.EstateOf(me) is { } estate && world.ExistingProject(estate.Station) is { } project)
        {
            var stood = project.Finished.Where(t => t.Value >= report.Since).OrderBy(t => t.Value).Select(t => GameWorld.StageName(t.Key)).ToList();
            if (stood.Count > 0)
            {
                Add(2, "project", "🏗", $"The {And(stood)} stood while you were away: every seat makes {world.ProjectBonus(project) * 100:0}% more now", "project");
            }
            else if (Week(world, me) is { } week)
            {
                Add(5, "project", "🏗", week.Text.Replace("🏗", "").Trim(), "project");
            }
        }

        // The social line: who thanked you, else who liked your estate
        if (report.Thanks.Count > 0)
        {
            Add(3, "social", "💛", report.Thanks.Count == 1 ? report.Thanks[0] : $"{report.Thanks[^1]}, and {report.Thanks.Count - 1} more thanks");
        }
        else if (report.LikesReceived > 0)
        {
            Add(5, "social", "♥", $"{Plural(report.LikesReceived, "visitor")} liked your estate");
        }

        // Today here: what happened in the district since you left, for everybody to see
        var today = TodayHere(world, me, report);
        if (today.Count > 0)
        {
            Add(6, "today", "📰", $"Today in {WorldNames.District(me.District)}: {string.Join(" · ", today)}");
        }

        // What stops you (after run 3, the design team's step 1): with an hour of income banked, the upgrade that pays
        // most now as the solver measures it ("Max now pays"), else the biggest producer held back and by what (run 3:
        // "27796.8k gold banked: Max … for 13k g", and Cleo's Coal Pit "energy short" for 11 days behind a full pipe)
        var income = world.MeasuredIncome(me);
        if (income > 0 && me.Gold >= 60 * income)
        {
            var producers = buildings
                .Where(t => !t.UnderConstruction && world.Content.Building(t.Type) is { Kind: BuildingKind.Producer, Out.Count: > 0 })
                .OrderByDescending(t => world.Content.Building(t.Type).Out.Sum(o => o.Value * world.BasePrice(o.Key)) * world.Content.Progression.OutputMultiplier(t.Level))
                .Take(6)
                .ToList();
            var pays = producers
                .Select(t => (Building: t, Plan: world.SmartMax(me, t)))
                .Where(t => t.Plan.Levels >= 1 && t.Plan.Cost <= me.Gold)
                .Select(t => (t.Building, t.Plan, Preview: world.UpgradePreview(me, t.Building, t.Plan.Levels)))
                .Where(t => t.Preview.Gain > 0.5 && t.Plan.Cost / t.Preview.Gain <= 24 * 60)
                .OrderByDescending(t => t.Preview.Gain / Math.Max(1, t.Plan.Cost))
                .FirstOrDefault();
            if (pays.Building != null)
            {
                Add(7, "invest", "💡", $"Max now pays: your {Kind(pays.Building)} to level {pays.Building.Level + pays.Plan.Levels} for {Amount(pays.Plan.Cost)} g, {pays.Preview.Text}",
                    "show", pays.Building.Id, pays.Plan.Cost);
            }
            else if (producers.FirstOrDefault(t => world.IdleReasonOf(t).Cause is IdleCause.PipeFull or IdleCause.PostFull) is { } held)
            {
                var reason = world.IdleReasonOf(held);
                var good = reason.Resource ?? world.Content.Building(held.Type).Out.Keys.First();
                var (could, carried) = world.PipeCarry(held, good);
                Add(7, "invest", "🧱", reason.Cause == IdleCause.PipeFull
                        ? $"What stops you: your {Kind(held)} sells {Amount(carried)} of the {Amount(could)} {world.GoodsWord(good)} it could make a minute: its pipes carry no more"
                        : $"What stops you: your {Kind(held)} could make more, and its trading post sells all it can",
                    "show", held.Id);
            }
        }

        var kept = lines.OrderBy(t => t.Rank).Take(ReturnLines).Select(t => t.Line).ToHashSet();
        var shown = lines.Select(t => t.Line).Where(kept.Contains).OrderBy(t => Array.IndexOf(ReturnSections, t.Kind)).ToList();

        var fine = new List<string>();
        if (report.CaretakerFee > 0)
        {
            fine.Add($"The caretaker kept things going for {Amount(report.CaretakerFee)} g of it (from day 14).");
        }
        if (away.WasAsleep)
        {
            fine.Add("From day 30 your estate rested: it made nothing, and nothing was lost.");
        }
        if (report.ResearchDone.Count > 0)
        {
            fine.Add($"Research done: {And(report.ResearchDone)}.");
        }
        if (report.GoldEarned < 1 && report.LevelsGained == 0 && shown.Count == 0 && !away.FromIsles)
        {
            return null;
        }
        return new ReturnCardDto(
            // What the gold covers: since the last session ended
            Span(world.Now - report.Since),
            report.GoldEarned,
            me.Level - report.LevelsGained,
            me.Level,
            fine,
            shown,
            away.FromIsles ? Landing(world, me) : [],
            report.GoldEarned < 1 && away.Away >= GameWorld.RestingAfter
                ? "Your estate waited for you. Pipe your buildings to your Trading Post (or your Yard gate) and they earn while you're away."
                : null);
    }

    /// <summary>Up to three public things of a company's district since a moment: its events, the stations' finished stages, newcomers</summary>
    private static List<string> TodayHere(GameWorld world, Company me, CompanyReport report)
    {
        var since = report.Since;
        var items = new List<(DateTime At, string Text)>();
        foreach (var gameEvent in world.ActiveEvents(me.District).Where(t => t.StartsAt >= since || t.ActiveAt(world.Now)))
        {
            items.Add((gameEvent.StartsAt, $"{world.EventName(gameEvent)} ({world.EventEffect(gameEvent).Split([';', ':'])[0].ToLowerInvariant()})"));
        }
        foreach (var project in world.Projects.Where(t => world.StationOf(t).District == me.District))
        {
            foreach (var (stage, at) in project.Finished.Where(t => t.Value >= since))
            {
                items.Add((at, $"{WorldNames.Station(world.StationOf(project))}'s {GameWorld.StageName(stage)} stood"));
            }
        }
        // Newcomers the people line names already ("moved in next door") aren't news twice
        foreach (var company in world.Companies.Where(t => !t.IsNpc && t.Id != me.Id && t.District == me.District && t.CreatedAt >= since && !me.Report.NewNeighbours.Contains(t.Name) && !report.NewNeighbours.Contains(t.Name)))
        {
            items.Add((company.CreatedAt, $"{company.Name} moved in"));
        }
        return items.OrderByDescending(t => t.At).Take(3).Select(t => t.Text).ToList();
    }

    private static string Days(int days) => days == 1 ? "1 day" : $"{days} days";

    private static string Plural(int count, string one) => count == 1 ? $"1 {one}" : $"{count} {one}s";

    private static string Amount(double value) => value >= 10_000 ? $"{value / 1000:0.#}k" : value >= 100 ? $"{value:N0}" : $"{value:0.#}";
}
