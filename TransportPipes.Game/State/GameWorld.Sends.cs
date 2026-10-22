using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

/// <summary>
/// A Send (step 4 of the redesign, docs/playtest-redesign.html): the giver's spare of a good goes to one neighbour first.
/// A standing promise, kept by the stand-in under the giver's name while the giver sleeps.
/// </summary>
public sealed class GoodSend
{
    public required string Resource { get; init; }

    /// <summary>The neighbour it goes to</summary>
    public long To { get; init; }

    /// <summary>The neighbour's building it is for (the lines name it); null: whatever of theirs lacks it</summary>
    public long? Building { get; init; }

    public DateTime Since { get; init; }
}

/// <summary>One named end of a building's Yard trade (step 4): who takes its goods, or who sends it its inputs</summary>
public sealed record TradeEnd(string Resource, Company Partner, Building? PartnerBuilding, double PerMinute, double Price, bool Sent);

/// <summary>A neighbour who could use a good (a bid): the building that needs it, how much more a minute, and the House that sells it to them now</summary>
public sealed record GoodBid(string Resource, Company Buyer, Building? Building, double PerMinute, Company? House);

/// <summary>
/// A building's trade as its owner sees it (step 4's "Where it goes" with both ends named): who takes its goods, who
/// sends it its inputs, who could use its goods, what the project takes of them and whether its pipes reach the gate
/// </summary>
public sealed record BuildingTrade(IReadOnlyList<TradeEnd> Takers, IReadOnlyList<TradeEnd> Suppliers, IReadOnlyList<GoodBid> Bids, IReadOnlyDictionary<string, double> ToProject, bool ReachesGate);

/// <summary>
/// Send and Thanks (step 4). The automatic Yard keeps carrying every resident's spare to every honest lack, friends
/// included, so a friend who never taps still runs his friends' buildings. A Send adds the name, the choice and a real
/// priority: the Yard's order is contracts, then the usual partners' running trade, then Sends, then everyone else pro
/// rata, then the project, Houses, storage and the hub. Nobody's running trade is cut by a Send. A Send to the
/// station's project is the "project first" box. Thanks gives no gold, XP or counts: once a day per pair, it puts a
/// line on the giver's card.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>At most this many Sends per giver (the project's count too)</summary>
    public const int MaxSends = 6;

    /// <summary>A giver's Sends, the project's included</summary>
    public int SendCount(Company company) => company.Sends.Count + company.ProjectFirst.Count;

    /// <summary>Whether a giver sends a good to a neighbour</summary>
    public bool SendsTo(Company giver, long to, string resource) => giver.Sends.Any(t => t.To == to && t.Resource == resource);

    /// <summary>
    /// Why a company can't send to another (null: it can): a player living in its station, a crew mate any time, a
    /// stranger once both gates met the Yard (a Handshake); never a House
    /// </summary>
    public string? SendBlocker(Company giver, Company receiver)
    {
        if (receiver.IsNpc)
        {
            return "Houses don't take gifts: they trade.";
        }
        if (receiver.Id == giver.Id)
        {
            return "That's you.";
        }
        if (EstateOf(giver) is not { } home || !StationPlayers(home.Station).Any(t => t.Id == receiver.Id))
        {
            return $"{receiver.Name} doesn't live in your station.";
        }
        var crewMates = giver.CrewId != null && giver.CrewId == receiver.CrewId;
        if (!crewMates && !(giver.Stats.ContainsKey("yard") && receiver.Stats.ContainsKey("yard")))
        {
            return $"Your gates haven't met yet: once you and {receiver.Name} pipe to the Yard you can send.";
        }
        if (!TradesWith(giver, receiver) || !TradesWith(receiver, giver))
        {
            return $"You don't trade with {receiver.Name}.";
        }
        return null;
    }

    /// <summary>
    /// Sends a good to a neighbour (or, with no neighbour, to the station's project) first, from now on: one tap
    /// </summary>
    public CommandResult SendGood(long giverId, string resource, long? toId, long? buildingId)
    {
        if (Company(giverId) is not { } giver)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (!Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown good.");
        }
        if (toId is not { } to)
        {
            if (giver.ProjectFirst.Contains(resource))
            {
                return CommandResult.Ok;
            }
            if (SendCount(giver) >= MaxSends)
            {
                return CommandResult.Fail($"You send {MaxSends} things already: stop one first.");
            }
            return SetProjectFirst(giverId, resource, true);
        }
        if (Company(to) is not { } receiver)
        {
            return CommandResult.Fail("Unknown neighbour.");
        }
        if (SendBlocker(giver, receiver) is { } blocker)
        {
            return CommandResult.Fail(blocker);
        }
        if (buildingId is { } id && Building(id) is not { } building)
        {
            return CommandResult.Fail("Unknown building.");
        }
        if (buildingId is { } target && Building(target)!.Owner != to)
        {
            return CommandResult.Fail("That building isn't theirs.");
        }
        var existing = giver.Sends.FirstOrDefault(t => t.To == to && t.Resource == resource);
        if (existing == null && SendCount(giver) >= MaxSends)
        {
            return CommandResult.Fail($"You send {MaxSends} things already: stop one first.");
        }
        if (existing != null)
        {
            giver.Sends.Remove(existing);
        }
        giver.Sends.Add(new GoodSend { Resource = resource, To = to, Building = buildingId ?? existing?.Building, Since = existing?.Since ?? LastTick });
        giver.Touch();
        Changes.Companies.Add(giver.Id);
        MarkTradeDirty(giver.Id);
        return CommandResult.Ok;
    }

    /// <summary>Stops a Send: the good goes back to the Yard's usual order</summary>
    public CommandResult StopSend(long giverId, string resource, long? toId)
    {
        if (Company(giverId) is not { } giver)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (toId == null)
        {
            return SetProjectFirst(giverId, resource, false);
        }
        if (giver.Sends.RemoveAll(t => t.To == toId && t.Resource == resource) == 0)
        {
            return CommandResult.Fail("You don't send that.");
        }
        giver.Touch();
        Changes.Companies.Add(giver.Id);
        MarkTradeDirty(giver.Id);
        return CommandResult.Ok;
    }

    /// <summary>Whether a company thanked another today already (Thanks: once a day per pair)</summary>
    public bool ThankedToday(Company thanker, long giverId) => thanker.ThankedOn.GetValueOrDefault(giverId) == WorldDay;

    /// <summary>
    /// Thanks for a neighbour's goods: once a day per pair, no gold, no XP, no counts. The giver hears it, and it is the
    /// social line of their next return card.
    /// </summary>
    public CommandResult Thank(long thankerId, long giverId, string resource)
    {
        if (Company(thankerId) is not { } thanker || Company(giverId) is not { } giver || giver.IsNpc || giver.Id == thanker.Id)
        {
            return CommandResult.Fail("Unknown neighbour.");
        }
        if (ThankedToday(thanker, giverId))
        {
            return CommandResult.Fail($"You thanked {giver.Name} today already.");
        }
        var gets = EstateOf(thanker) is { } estate && YardTrades(estate).Any(t => !t.Selling && t.Partner == giverId && t.Resource == resource);
        if (!gets && UsualRate(giver, thanker, resource) <= 1e-9)
        {
            return CommandResult.Fail($"{giver.Name}'s {GoodsWord(resource)} doesn't come to you.");
        }
        thanker.ThankedOn[giverId] = WorldDay;
        thanker.Touch();
        var line = $"{NameFor(thanker, giver)} thanked you for your {GoodsWord(resource)} 💛";
        giver.Report.Thanks.Add(line);
        giver.Touch();
        Changes.Events.Add(new WorldEvent(giver.Id, WorldEventKind.Neighbour, line));
        Changes.Companies.Add(giver.Id);
        Changes.Companies.Add(thanker.Id);
        return CommandResult.Ok;
    }

    /// <summary>A building's trade as its owner sees it: both ends named (the pairs show only to their two ends)</summary>
    public BuildingTrade TradeOf(Building building)
    {
        var empty = new BuildingTrade([], [], [], new Dictionary<string, double>(), false);
        if (Company(building.Owner) is not { IsNpc: false } owner || EstateAt(building.Position) is not { } estate)
        {
            return empty;
        }
        var definition = Content.Building(building.Type);
        var network = NetworkOf(building.Id);
        // Its own path to the gate (goods never pass through a building, so sharing the gate's pipes isn't enough)
        var reaches = network != null && network.GateLinks.Any(t => t.Building == building.Id);
        var trades = YardTrades(estate);
        // This building's part of its estate's gate trade in a good
        double Share(string resource, bool import)
        {
            var flows = network?.GateFlows.Where(t => t.Import == import && t.Resource == resource && t.Estate == KeyOf(estate)).ToList() ?? [];
            var total = flows.Sum(t => t.Sent);
            return total <= 1e-12 ? 0 : flows.Where(t => t.Building == building.Id).Sum(t => t.Sent) / total;
        }
        var takers = new List<TradeEnd>();
        var toProject = new Dictionary<string, double>();
        var bids = new List<GoodBid>();
        var project = ProjectsEnabled ? ExistingProject(estate.Station) : null;
        foreach (var resource in definition.Out.Keys.Where(t => t != GameContent.ResearchPoints))
        {
            var share = reaches ? Share(resource, import: false) : 0;
            foreach (var trade in trades.Where(t => t.Selling && t.Resource == resource))
            {
                if (Company(trade.Partner) is { } partner && trade.PerMinute * share > 1e-6)
                {
                    takers.Add(new TradeEnd(resource, partner, NeedOf(partner, resource), trade.PerMinute * share, YardPrice(owner, partner, resource, selling: true), SendsTo(owner, partner.Id, resource)));
                }
            }
            if (project?.Incoming.GetValueOrDefault(owner.Id)?.GetValueOrDefault(resource) is > 1e-9 and var incoming && share > 0)
            {
                toProject[resource] = incoming * share;
            }
            bids.AddRange(Bids(owner, resource));
        }
        var suppliers = new List<TradeEnd>();
        foreach (var resource in definition.In.Keys)
        {
            var share = reaches ? Share(resource, import: true) : 0;
            foreach (var trade in trades.Where(t => !t.Selling && t.Resource == resource))
            {
                if (Company(trade.Partner) is { } partner && trade.PerMinute * share > 1e-6)
                {
                    suppliers.Add(new TradeEnd(resource, partner, null, trade.PerMinute * share, YardPrice(owner, partner, resource, selling: false), SendsTo(partner, owner.Id, resource)));
                }
            }
        }
        return new BuildingTrade(takers, suppliers, bids, toProject, reaches);
    }

    /// <summary>A bid needs at least this much of the good a minute at the giver's gate</summary>
    private const double SendSpareAtLeast = 0.5;

    /// <summary>What of a good a company puts through its Yard gate a minute: sold in the Yard, and spare nobody took</summary>
    private double AtGate(Company me, Estate estate, string resource, IReadOnlyList<YardResident> residents) =>
        (residents.FirstOrDefault(t => t.Company.Id == me.Id)?.Spare.GetValueOrDefault(resource) ?? 0)
        + YardTrades(estate).Where(t => t.Selling && t.Resource == resource).Sum(t => t.PerMinute);

    /// <summary>
    /// Why a Send moves nothing now, if it doesn't: the receiver gets all it needs (by its usual partners, say), or the
    /// giver's own buildings use all of it (run 2: Dev's "goes to ana_pm Pipes first" moved nothing for 28 days)
    /// </summary>
    public string? SendIdleReason(Company giver, GoodSend send)
    {
        var to = send.To;
        if (Company(to) is not { } receiver || EstateOf(giver) is not { } estate)
        {
            return null;
        }
        var residents = YardResidents(estate.Station);
        var lacks = residents.FirstOrDefault(t => t.Company.Id == to)?.Lacks.GetValueOrDefault(send.Resource) ?? 0;
        var houses = EstateOf(receiver) is { } theirs ? YardTrades(theirs).Where(t => !t.Selling && t.Resource == send.Resource && Company(t.Partner) is { IsNpc: true }).Sum(t => t.PerMinute) : 0;
        if (lacks + houses < 0.05)
        {
            return $"{(send.Building is { } b && Building(b) is { } building ? $"their {Content.Building(building.Type).Name}" : "they")} {(send.Building != null ? "gets" : "get")} all {(send.Building != null ? "it needs" : "they need")} now";
        }
        if (AtGate(giver, estate, send.Resource, residents) <= 1e-3)
        {
            return $"your own buildings use all your {GoodsWord(send.Resource)}";
        }
        return null;
    }

    /// <summary>The name of a company's building that needs a good most ("Glassworks"), if any</summary>
    public string? TradeNeed(Company company, string resource) => NeedOf(company, resource) is { } building ? Content.Building(building.Type).Name : null;

    /// <summary>A company's building that needs a good most: one short of it, else the one that takes the most of it from the gate</summary>
    private Building? NeedOf(Company company, string resource)
    {
        var buildings = BuildingsOf(company.Id).Where(t => !t.UnderConstruction && Content.Building(t.Type).In.ContainsKey(resource)).ToList();
        return buildings.FirstOrDefault(t => IdleReasonOf(t) is { Resource: var r, Cause: IdleCause.InputMissing or IdleCause.InputShort } && r == resource)
            ?? buildings.OrderByDescending(t => NetworkOf(t.Id)?.GateFlows.Where(f => f.Import && f.Building == t.Id && f.Resource == resource).Sum(f => f.Delivered) ?? 0).FirstOrDefault();
    }

    /// <summary>
    /// Who in a company's station could use a good it makes (bids: "Ana's Glassworks could use your sand"): players it
    /// may send to, not sent to yet, with what they still lack and what Houses sell them now (a Send takes both); what
    /// other players sell them stays private. Only where a Send would move something: you have spare of it, and they
    /// could use at least half of what one building makes of it (run 2: every bid was energy for 0.3-0.8 a minute, and
    /// Dev's two Sends never moved a unit)
    /// </summary>
    public List<GoodBid> Bids(Company me, string resource, IReadOnlyList<YardResident>? residents = null)
    {
        if (EstateOf(me) is not { } estate)
        {
            return [];
        }
        residents ??= YardResidents(estate.Station);
        if (AtGate(me, estate, resource, residents) <= SendSpareAtLeast)
        {
            return [];
        }
        var enough = Math.Max(1, 0.5 * OneBuildingRate(resource));
        var bids = new List<GoodBid>();
        foreach (var resident in residents.Where(t => t.Connected && !t.Company.IsNpc && t.Company.Id != me.Id))
        {
            var buyer = resident.Company;
            if (SendsTo(me, buyer.Id, resource) || SendBlocker(me, buyer) != null || EstateOf(buyer) is not { } theirs)
            {
                continue;
            }
            var houses = YardTrades(theirs).Where(t => !t.Selling && t.Resource == resource && Company(t.Partner) is { IsNpc: true }).ToList();
            var couldUse = resident.Lacks.GetValueOrDefault(resource) + houses.Sum(t => t.PerMinute);
            if (couldUse >= enough)
            {
                var house = houses.OrderByDescending(t => t.PerMinute).Select(t => Company(t.Partner)).FirstOrDefault();
                bids.Add(new GoodBid(resource, buyer, NeedOf(buyer, resource), couldUse, house));
            }
        }
        return bids.OrderByDescending(t => t.PerMinute).ToList();
    }

    /// <summary>
    /// "Make it yours: Send ▸": when a company's goods ran a neighbour's building by themselves, its card says so once
    /// per neighbour and good (true: show it now)
    /// </summary>
    public bool OfferSendOnce(Company me, Company partner, string resource) =>
        !SendsTo(me, partner.Id, resource) && SendBlocker(me, partner) == null && me.GiftsAnnounced.Add($"offer:{partner.Id}:{resource}");

    /// <summary>What a company and each neighbour gave each other this week (units each way, per good), from the trades they took</summary>
    public List<(Company Partner, string Resource, double Given, double Received)> GiftsThisWeek(Company me)
    {
        var week = LastTick - TimeSpan.FromDays(UsualTradeDays - 1);
        var given = TakenSince(me, week);
        var gifts = new Dictionary<(long, string), (double Given, double Received)>();
        foreach (var ((buyer, resource), units) in given)
        {
            gifts[(buyer, resource)] = (units, 0);
        }
        foreach (var other in _companies.Values.Where(t => !t.IsNpc && t.Id != me.Id && t.UsualTrades.Keys.Any(k => k.StartsWith($"{me.Id}:", StringComparison.Ordinal))))
        {
            foreach (var ((buyer, resource), units) in TakenSince(other, week).Where(t => t.Key.Buyer == me.Id))
            {
                var (g, _) = gifts.GetValueOrDefault((other.Id, resource));
                gifts[(other.Id, resource)] = (g, units);
            }
        }
        return gifts.Where(t => Company(t.Key.Item1) is { IsNpc: false })
            .Select(t => (Company(t.Key.Item1)!, t.Key.Item2, t.Value.Given, t.Value.Received))
            .OrderByDescending(t => (t.Given + t.Received) * BasePrice(t.Item2))
            .ToList();
    }

    /// <summary>
    /// The first gift is announced, to both ends, once per pair and good: "Your sand reached Ana's Glassworks: 15 a
    /// minute" (a Send's goods, taken and paid)
    /// </summary>
    private void AnnounceGift(Company giver, Company receiver, string resource, double perMinute)
    {
        if (giver.IsNpc || receiver.IsNpc || perMinute <= 1e-9
            || giver.Sends.FirstOrDefault(t => t.To == receiver.Id && t.Resource == resource) is not { } send
            || !giver.GiftsAnnounced.Add($"{receiver.Id}:{resource}"))
        {
            return;
        }
        var good = GoodsWord(resource);
        var building = send.Building is { } id && Building(id) is { } b ? Content.Building(b.Type).Name : null;
        giver.Touch();
        Changes.Events.Add(new WorldEvent(giver.Id, WorldEventKind.Neighbour,
            $"∴ Your {good} reached {NameFor(receiver, giver)}{(building != null ? $"'s {building}" : "")}: {Amount(perMinute)} a minute"));
        Changes.Events.Add(new WorldEvent(receiver.Id, WorldEventKind.Neighbour,
            $"💛 {NameFor(giver, receiver)} sends you {good}{(building != null ? $": your {building} runs on it" : "")}"));
    }

    /// <summary>The first of a giver's goods the project takes is announced: "∴ Your sand reached the Water Tower: 15 a minute"</summary>
    private void AnnounceProjectGift(Company giver, StationProject project, string good, double perMinute)
    {
        if (giver.IsNpc || !giver.ProjectFirst.Contains(good) || !giver.GiftsAnnounced.Add($"project:{project.Stage}:{good}"))
        {
            return;
        }
        giver.Touch();
        Changes.Events.Add(new WorldEvent(giver.Id, WorldEventKind.Neighbour,
            $"∴ Your {GoodsWord(good)} reached the {StageName(project.Stage)}: {Amount(perMinute)} a minute"));
    }
}
