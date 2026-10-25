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
    private static readonly string[] ReturnSections = ["people", "social", "crew", "project", "look", "invest", "orders", "today"];

    /// <summary>What the card needs from before the Hello started the company's next report</summary>
    public sealed record AwayFacts(TimeSpan Away, CompanyReport Report);

    /// <summary>
    /// The return card (F5): one card for every return with news, built on the server's clock. Gold and level, then at
    /// most six lines in this order: your people (who moved in, who bought your goods, whose goods ran your buildings,
    /// who answered you in the town chat), what to look at (storm damage with Repair all, a stuck building, crates, a
    /// full storage), the Era, a social line, today in the town, and the Invest line. When there are more, the lowest go
    /// first: the Invest line, then today.
    /// </summary>
    public static ReturnCardDto? ReturnCard(GameWorld world, Company me, AwayFacts away, string? chatLine)
    {
        var report = away.Report;
        var lines = new List<(int Rank, ReturnLineDto Line)>();
        void Add(int rank, string kind, string icon, string text, string? action = null, long? building = null, double? cost = null, long? partner = null, string? resource = null, string? partnerName = null) =>
            lines.Add((rank, new ReturnLineDto(kind, icon, text, action, building, cost, partner, resource, partnerName)));
        string Kind(Building building) => world.Content.Building(building.Type).Name;

        // Your people
        if (report.NewNeighbours.Count > 0)
        {
            Add(2, "people", "🏘", $"{And(report.NewNeighbours.Distinct().Take(3).ToList())} moved in next door");
        }
        // What your goods did for them, as a sale with its money (run 2: "X took N of your energy" read as taking without
        // asking). The first such line says once that the Yard sells your spare by itself.
        var taken = report.SoldToUnits
            .Select(t => (Parts: t.Key.Split(':', 2), Units: t.Value, Gold: report.SoldToGold.GetValueOrDefault(t.Key)))
            .Where(t => t.Parts.Length == 2 && long.TryParse(t.Parts[0], out _) && t.Units > 1e-6)
            .Select(t => (Buyer: world.Company(long.Parse(t.Parts[0])), Resource: t.Parts[1], t.Units, t.Gold))
            .Where(t => t.Buyer is { })
            .OrderByDescending(t => t.Gold)
            .Take(2)
            .ToList();
        var why = taken.Count > 0 && !me.YardSalesExplained ? " (the Yard sells your spare to neighbours who lack it, at your price, by itself)" : "";
        if (taken.Count > 0)
        {
            Add(3, "people", "🤝", string.Join("; ", taken.Select((t, i) =>
            {
                var building = world.TradeNeed(t.Buyer!, t.Resource);
                return i == 0
                    ? $"{world.PossessiveFor(t.Buyer!, me)} {building ?? "buildings"} bought {Amount(t.Units)} of your {world.GoodsWord(t.Resource)} · +{Amount(t.Gold)} g"
                    : $"{world.NameFor(t.Buyer!, me)} {Amount(t.Units)} {world.GoodsWord(t.Resource)} · +{Amount(t.Gold)} g";
            })) + why);
        }
        if (why.Length > 0)
        {
            me.YardSalesExplained = true;
            me.Touch();
        }
        // What a neighbour's goods did for you: the biggest (run 2: Dev asked how to feed the Lab that Ana's ingots were
        // feeding)
        var got = report.BoughtFromUnits
            .Select(t => (Parts: t.Key.Split(':', 2), Units: t.Value))
            .Where(t => t.Parts.Length == 2 && long.TryParse(t.Parts[0], out _) && t.Units > 1e-6)
            .Select(t => (Seller: world.Company(long.Parse(t.Parts[0])), Resource: t.Parts[1], t.Units))
            .Where(t => t.Seller is { })
            .OrderByDescending(t => t.Units * world.BasePrice(t.Resource))
            .FirstOrDefault();
        if (got.Seller is { } giver)
        {
            var building = world.TradeNeed(me, got.Resource);
            Add(2, "people", "🤝", $"{world.PossessiveFor(giver, me)} {world.GoodsWord(got.Resource)} ran your {building ?? "buildings"} while you were away: {Amount(got.Units)} of it");
        }
        // A running trade with a friend that fell to under half, on both cards with its cause (after run 3, row 9)
        if (world.FallsFor(me, report.Since).FirstOrDefault() is { Seller: not null } fall)
        {
            var own = fall.Fall.Owner == me.Id && fall.Fall.Building != null;
            Add(1, "people", "📉", world.FallText(fall.Seller, fall.Buyer, fall.Resource, fall.Fall, me), own ? "show" : null, own ? fall.Fall.Building : null);
        }
        if (chatLine != null)
        {
            Add(3, "people", "💬", chatLine);
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
        // A full Storage only when buildings slow behind it (run 3: Cleo's on 5 of her 5 cards, "a warning I cannot act on")
        if (world.FullStorages(me.Id).Select(t => (Storage: t, Line: world.StorageLine(t))).FirstOrDefault(t => t.Line.Waiting != null) is { Storage: not null } full)
        {
            Add(3, "look", "🛢", $"Your {Kind(full.Storage)} is full of {full.Line.Holds}: {full.Line.Waiting} slow behind it", "show", full.Storage.Id);
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

        // Your own scaffold, said as it is (after run 4: Dev's Big Factory and Ana's Cement Kiln read "is being built" and
        // waited to the end for bricks nobody made): what it waits for, from whom, or Order the rest
        if (world.OpenWants(me).Where(t => t.Want.Left > 1e-6).OrderByDescending(t => t.Want.Left * world.BasePrice(t.Want.Resource)).FirstOrDefault() is { Name: not null } waiting)
        {
            var good = world.GoodsWord(waiting.Want.Resource);
            var maker = world.NearPlayers(me).FirstOrDefault(c => c.Id != me.Id && world.BuildingsOf(c.Id).Any(b => !b.UnderConstruction && world.Content.Building(b.Type).Out.ContainsKey(waiting.Want.Resource)));
            var how = maker != null ? $"{world.NameFor(maker, me)} makes {good}: the Yard brings them"
                : $"nobody near makes {good}";
            Add(2, "look", "🏗", $"Your {waiting.Name} waits for {waiting.Want.Left:N0} {good}: {how}",
                world.OrderCost(me, waiting.Target) is { } own && own <= me.Gold ? "orderWant" : "show",
                waiting.Target > 0 ? waiting.Target : null, world.OrderCost(me, waiting.Target));
        }
        // A neighbour's scaffold you could help with (after run 3, step 2): your Storage at a tap, or your spare going there
        if (world.WantsNear(me).Where(t => t.Holds >= 1 || t.Makes).OrderByDescending(t => t.Holds).FirstOrDefault() is { Owner: not null } near)
        {
            var name = near.Name;
            var who = world.NameFor(near.Owner, me);
            Add(2, "people", "🧱", near.Holds >= 1
                    ? $"{who}'s {name} wants {near.Want.Left:N0} {world.GoodsWord(near.Want.Resource)}: your Storage holds {Math.Floor(near.Holds):N0}"
                    : $"{who}'s {name} wants {near.Want.Left:N0} {world.GoodsWord(near.Want.Resource)}: your spare goes there first",
                near.Holds >= 1 ? "fillWant" : null, near.Target);
        }
        // A friend's want that waited, which you can't fill but can order for them (the designers' rule after run 3: Ben's
        // "I still can't help"): "Ana's Steel Mill wants 120 bricks · Order the rest for Ana ▸"
        else
        {
            // Your station and your crew, a friend away for a month too (after run 4: crew mates elsewhere could only watch)
            var orderable = world.WantsNear(me)
                .Select(w => (w.Owner, w.Target, w.Name, w.Want, Cost: world.OrderCost(me, w.Target)))
                .Where(t => t.Cost is { } cost && cost <= me.Gold)
                .OrderBy(t => t.Owner.CrewId != null && t.Owner.CrewId == me.CrewId ? 0 : 1)
                .ThenByDescending(t => t.Want.Left * world.BasePrice(t.Want.Resource))
                .FirstOrDefault();
            if (orderable.Owner != null)
            {
                var who = world.NameFor(orderable.Owner, me);
                Add(2, "people", "🧱", $"{world.PossessiveFor(orderable.Owner, me)} {orderable.Name} wants {orderable.Want.Left:N0} {world.GoodsWord(orderable.Want.Resource)}",
                    "orderWant", orderable.Target, orderable.Cost, orderable.Owner.Id, orderable.Want.Resource, who);
            }
        }
        // The crew in one line, from what it does now (after run 3, row 11: "a wall of 30+ messages … I understood little"),
        // without the want a line above offers already
        var offered = lines.Where(t => t.Line.Action is "fillWant" or "orderWant").Select(t => t.Line.Building).ToHashSet();
        if (CrewLine(world, me, offered) is { } crewLine)
        {
            Add(2, "crew", "👥", crewLine.Text, crewLine.Target != null ? "fillWant" : null, crewLine.Target);
        }
        // How the last Era ended, and your part (after run 3, step 5: "Era 1 ended without me knowing the Dynamo result")
        if (me.EraEnded is { } ended && world.TownAt(me.District).EraStartedAt >= report.Since)
        {
            Add(1, "project", "🏛", ended);
        }

        // Hub orders show once they are filled, never as a task (after run 3, step 7)
        if (report.OrdersCompleted > 0)
        {
            var goods = report.OrdersFilled.Select(world.GoodsWord).ToList();
            Add(4, "orders", "📋", $"Your {And(goods)} filled {Plural(report.OrdersCompleted, "hub order")} at twice the price: +{report.OrderGold:N0} g over the hub's");
        }

        // The social line: who of your crew came to look, else who liked your plot
        if (report.VisitedBy.Count > 0)
        {
            Add(3, "social", "👁", $"{And(report.VisitedBy.Take(3).ToList())} visited your plot 💛");
        }
        else if (report.LikesReceived > 0)
        {
            Add(5, "social", "♥", $"{Plural(report.LikesReceived, "visitor")} liked your plot");
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
            // Everything that pays back within five days, in one tap (after run 4: Ben came back to 13M and spent 3.4k one
            // Max at a time); it leads the card's money line
            var plan = world.PlanGrowth(me);
            if (plan.Parts.Count > 0)
            {
                var land = plan.Parts.Any(t => t.Parcel != null);
                var grown = plan.Parts.Count(t => t.Parcel == null);
                var what = string.Join(" and ", new[] { land ? "a new parcel" : null, grown > 0 ? Plural(grown, "building") : null }.OfType<string>());
                Add(7, "invest", "💡", $"Grow everything that pays: about +{Amount(plan.Gain)} g/min for {Amount(plan.Cost)} g ({Math.Max(1, Math.Round(100 * plan.Cost / Math.Max(1, me.Gold))):0}% of your gold) · {what}",
                    "grow", null, plan.Cost);
            }
            else if (producers.FirstOrDefault(t => world.IdleReasonOf(t).Cause is IdleCause.PipeFull) is { } held)
            {
                var reason = world.IdleReasonOf(held);
                var good = reason.Resource ?? world.Content.Building(held.Type).Out.Keys.First();
                var (could, carried) = world.PipeCarry(held, good);
                // Its key: Widen, the next pipe tier, a second trading post (after run 3, step 9)
                var key = world.IdleStatusOf(held, plan: false).Fix?.Label.Split(" · ")[0];
                Add(7, "invest", "🧱", reason.Cause == IdleCause.PipeFull
                        ? $"What stops you: your {Kind(held)} sells {Amount(carried)} of the {Amount(could)} {world.GoodsWord(good)} it could make a minute: its pipes carry no more{(key != null ? $" · {key}" : "")}"
                        : $"What stops you: your {Kind(held)} could make more, and its trading post sells all it can",
                    "show", held.Id);
            }
        }

        // The card leads with what matters and folds the rest (after run 3, row 11, and the designers' rule): one line about
        // people (a friend's want, a sale), the crew's line, the shared goal when it stood or you can unblock
        // it, one-tap fixes, one "what stops you"
        var lead = lines.Where(t => t.Line.Kind is "people" or "social").OrderBy(t => t.Rank).Take(1)
            .Concat(lines.Where(t => t.Line.Kind == "crew").Take(1))
            .Concat(lines.Where(t => t.Line.Kind == "project" && t.Rank <= 2).OrderBy(t => t.Rank).Take(2))
            .Concat(lines.Where(t => t.Line.Kind == "look" && t.Line.Action != null).OrderBy(t => t.Rank).Take(2))
            .Concat(lines.Where(t => t.Line.Kind == "invest").Take(1))
            .Select(t => t.Line)
            .Take(ReturnLines)
            .ToList();
        // A line unchanged since the last card folds into one "Still:" line, its fix kept (run 3: Cleo's full Storage on 5
        // of her 5 cards, Ana's factory at 55, 59, 50 and 40%)
        var still = lead.Where(t => t.Kind is "look" or "invest" && me.LastCardLines.Contains(t.Text)).ToList();
        var shown = lead.Except(still).OrderBy(t => Array.IndexOf(ReturnSections, t.Kind)).ToList();
        if (still.Count > 0)
        {
            var fix = still.FirstOrDefault(t => t.Action != null) ?? still[0];
            shown.Add(fix with { Kind = "still", Icon = "↺", Text = $"Still: {string.Join(" · ", still.Select(t => t.Text.Split(": ")[0]))}" });
        }
        me.LastCardLines = lead.Select(t => t.Text).ToList();
        me.Touch();
        var folded = lines.Select(t => t.Line).Where(t => !lead.Contains(t)).OrderBy(t => Array.IndexOf(ReturnSections, t.Kind)).ToList();

        var fine = new List<string>();
        if (report.ResearchDone.Count > 0)
        {
            fine.Add($"Research done: {And(report.ResearchDone)}.");
        }
        if (report.GoldEarned < 1 && report.LevelsGained == 0 && shown.Count == 0 && folded.Count == 0)
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
            report.GoldEarned < 1 && away.Away >= GameWorld.RestingAfter
                ? "Your plot waited for you. Pipe your buildings to your Trading Post (or your Yard gate) and they earn while you're away."
                : null,
            folded);
    }

    /// <summary>
    /// The crew in one line, built from what it does now, not its chat (after run 3, row 11: Ben, "a wall of 30+ messages
    /// … I understood little"): what friends bring to a shared goal, a friend's want, and what you could send ("Your
    /// crew: Cleo brings bricks to the Great Dynamo · Ana's Market Hall wants 100 bricks")
    /// </summary>
    private static (string Text, long? Target)? CrewLine(GameWorld world, Company me, IReadOnlySet<long?> offered)
    {
        if (world.CrewOf(me) is not { } crew)
        {
            return null;
        }
        var mates = world.CrewMembers(crew).Where(t => t.Id != me.Id && world.IsActive(t)).ToList();
        if (mates.Count == 0)
        {
            return null;
        }
        var parts = new List<string>();
        foreach (var mate in mates)
        {
            if (world.WonderOf(me.District) is { } wonder && wonder.Incoming.GetValueOrDefault(mate.Id)?.Where(t => t.Value > 1e-9).Select(t => t.Key).FirstOrDefault() is { } toWonder)
            {
                parts.Add($"{world.NameFor(mate, me)} brings {world.GoodsWord(toWonder)} to the {world.WonderDefinition(wonder).Name.Replace("The ", "")}");
            }
        }
        if (mates.SelectMany(t => world.OpenWants(t).Where(w => !offered.Contains(w.Target)).Select(w => (Mate: t, w.Name, w.Want)))
                .OrderByDescending(t => t.Want.Left * world.BasePrice(t.Want.Resource)).FirstOrDefault() is { Mate: not null } want)
        {
            parts.Add($"{world.PossessiveFor(want.Mate, me)} {want.Name} wants {Amount(want.Want.Left)} {world.GoodsWord(want.Want.Resource)}");
        }
        if (parts.Count == 0)
        {
            return null;
        }
        var near = world.WantsNear(me).Where(t => t.Owner.CrewId == crew.Id && t.Holds >= 1 && !offered.Contains(t.Target)).OrderByDescending(t => t.Holds).FirstOrDefault();
        return ($"Your crew: {string.Join(" · ", parts.Take(3))}{(near.Owner != null ? $" · you could send your {world.GoodsWord(near.Want.Resource)}" : "")}", near.Owner != null ? near.Target : null);
    }

    /// <summary>Up to three public things of a company's town since a moment: its events</summary>
    private static List<string> TodayHere(GameWorld world, Company me, CompanyReport report)
    {
        var since = report.Since;
        var items = new List<(DateTime At, string Text)>();
        foreach (var gameEvent in world.ActiveEvents(me.District).Where(t => (t.StartsAt >= since || t.ActiveAt(world.Now)) && t.Kind is EventKind.Expo or EventKind.Shortage or EventKind.Discovery or EventKind.Festival))
        {
            items.Add((gameEvent.StartsAt, $"{world.EventName(gameEvent)} ({world.EventEffect(gameEvent).Split([';', ':'])[0].ToLowerInvariant()})"));
        }
        // Only what you can act on (after run 3, row 11): other stations' stages and newcomers are no news for you
        return items.OrderByDescending(t => t.At).Take(3).Select(t => t.Text).ToList();
    }

    private static string Plural(int count, string one) => count == 1 ? $"1 {one}" : $"{count} {one}s";

    private static string Amount(double value) =>
        value >= 1e9 ? $"{value / 1e9:0.#}B" : value >= 1e6 ? $"{value / 1e6:0.#}M" : value >= 10_000 ? $"{value / 1000:0.#}k" : value >= 100 ? $"{value:N0}" : $"{value:0.#}";
}
