using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

public enum LinkState
{
    /// <summary>Offered by one side, waiting for the other's tap (at most three days)</summary>
    Offered,
    Active,

    /// <summary>Paused by one side (after the notice): nothing flows, the tank waits; either side resumes it</summary>
    Paused,
}

/// <summary>How a link's last round went, for its state line</summary>
public enum LinkFlow
{
    /// <summary>The buyer got what it needed from the seller</summary>
    Live,

    /// <summary>One of the two hasn't piped its Yard gate (or is on its way)</summary>
    NotConnected,

    /// <summary>A Block, trade settings or a buyer without gold</summary>
    NotAllowed,

    /// <summary>The buyer needs none of it right now: the seller's goes to the Yard and the hub</summary>
    NotNeeded,

    /// <summary>The seller has less spare than the buyer needs: the tank, then the Yard and the hub fill in</summary>
    SellerShort,

    /// <summary>The pipe carries less than the buyer needs and the seller has: the rest goes to the Yard and the hub</summary>
    Capped,
}

/// <summary>
/// A link (v1.2 §7): one good from one named company's Yard gate to another's, on a pipe auto-routed through the
/// corridors. Never transitive, never across a third estate. A tank at the buyer's gate keeps two hours of the flow, a
/// pause or the end comes with an hour's notice, and what the link doesn't bring the Yard and the hub do.
/// </summary>
public sealed class Link : Entity
{
    public long Id { get; init; }
    public long Seller { get; init; }
    public long Buyer { get; init; }
    public required string Resource { get; init; }

    /// <summary>The pipe tier (its loss per tile and throughput)</summary>
    public int Tier { get; set; } = 1;

    public long OfferedBy { get; init; }

    /// <summary>When the side it was offered to first played after the offer (F11: the offer waits 24 h from then)</summary>
    public DateTime? PartnerSeenAt { get; set; }

    public LinkState State { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime? AcceptedAt { get; set; }

    /// <summary>What the offerer paid for the pipe (refunded when the offer isn't taken)</summary>
    public double Paid { get; set; }
    public Dictionary<string, double> PaidMaterials { get; set; } = new();

    /// <summary>A pause or the end announced by one side: it takes effect at this moment (the notice)</summary>
    public DateTime? NoticeAt { get; set; }
    public bool NoticeEnds { get; set; }
    public long? NoticeBy { get; set; }
    public long? PausedBy { get; set; }

    /// <summary>The route's corners, from the seller's Yard gate to the buyer's, and its length in tiles</summary>
    public List<TilePos> Route { get; set; } = new();
    public int Length { get; set; }

    /// <summary>Last round, per minute: what the buyer needs from the link, what the seller's gate sends for it (with what fills the tank), what reached the buyer</summary>
    public double Need { get; set; }
    public double Send { get; set; }
    public double Rate { get; set; }
    public LinkFlow Flow { get; set; }

    /// <summary>The tank at the buyer's gate (units), how big it is (two hours of the need) and how it changes per minute until the next round</summary>
    public double Tank { get; set; }
    public double TankSize { get; set; }
    public double TankChange { get; set; }
    public DateTime? LastRound { get; set; }

    /// <summary>What the buyer got through the link since it started</summary>
    public double Delivered { get; set; }

    public bool Flows => State == LinkState.Active;

    public long Partner(long company) => company == Seller ? Buyer : Seller;

    public override string ToString() => $"link #{Id} {Resource} {Seller}→{Buyer} ({State})";
}

public sealed partial class GameWorld
{
    /// <summary>F11: an offer waits until a day after the other side's next session, at most two weeks</summary>
    public static readonly TimeSpan LinkOfferAfterSeen = TimeSpan.FromHours(24);
    public static readonly TimeSpan LinkOfferLongest = TimeSpan.FromDays(14);

    /// <summary>
    /// When an offer lapses: a day after the side it was offered to began a session after it was made (at most two weeks
    /// after it was made). Run 2: an offer made while Ana was playing lapsed before her next session, because the session
    /// she was in counted, and her return card never showed it.
    /// </summary>
    public DateTime OfferLapsesAt(Link link)
    {
        if (link.PartnerSeenAt == null && Company(link.Partner(link.OfferedBy)) is { } partner && partner.SessionStartedAt > link.CreatedAt)
        {
            link.PartnerSeenAt = partner.SessionStartedAt;
            link.Touch();
        }
        var longest = link.CreatedAt + LinkOfferLongest;
        return link.PartnerSeenAt is { } seen && seen + LinkOfferAfterSeen < longest ? seen + LinkOfferAfterSeen : longest;
    }
    public static readonly TimeSpan LinkNotice = TimeSpan.FromHours(1);

    /// <summary>The tank at the buyer's gate holds this many minutes of the link's flow</summary>
    public const double TankMinutes = 120;

    /// <summary>A tank fills in an hour at the most from what the seller has spare</summary>
    private const double TankFillMinutes = 60;

    public const int MaxLinksPerEstate = 12;
    public const int MaxLinkOffersPerHour = 5;

    private readonly Dictionary<long, Link> _links = new();
    public long NextLinkId { get; set; } = 1;
    public IEnumerable<Link> Links => _links.Values;

    public Link? Link(long id) => _links.TryGetValue(id, out var link) ? link : null;

    public void LoadLink(Link link)
    {
        _links[link.Id] = link;
        link.Dirty = false;
        NextLinkId = Math.Max(NextLinkId, link.Id + 1);
    }

    public IEnumerable<Link> LinksOf(long companyId) =>
        _links.Values.Where(t => t.Seller == companyId || t.Buyer == companyId).OrderBy(t => t.Id);

    /// <summary>A flowing (or paused) link that carries a good from one company to another</summary>
    public Link? LinkBetween(long seller, long buyer, string resource) =>
        _links.Values.FirstOrDefault(t => t.Seller == seller && t.Buyer == buyer && t.Resource == resource && t.State != LinkState.Offered);

    public PipeTierDefinition LinkPipe(Link link) => Content.PipeTier(link.Tier);

    /// <summary>What is in a link's tank now (it changes steadily between rounds)</summary>
    public double TankNow(Link link) =>
        link.LastRound is { } last && LastTick > last
            ? Math.Clamp(link.Tank + link.TankChange * (LastTick - last).TotalMinutes, 0, link.TankSize)
            : link.Tank;

    /// <summary>What a link's pipe carries per minute</summary>
    public double LinkThroughput(Link link) => LinkPipe(link).Throughput;

    /// <summary>The share of what the seller sends that reaches the buyer's gate: the pipe's loss per tile over the route</summary>
    /// <summary>What reaches the buyer: the link's pipe over its length, and between towns the trunk line's loss over its part</summary>
    public double LinkEfficiency(Link link) =>
        LineOf(link) is { } line ? LinkEfficiency(link.Tier, Math.Max(0, link.Length - line.Length)) * LineEfficiency(line) : LinkEfficiency(link.Tier, link.Length);

    public double LinkEfficiency(int tier, int length) => Math.Pow(1 - Content.PipeTier(tier).LossPerTile, length);

    // ---------------------------------------------------------------- The route

    /// <summary>The line a link takes along a town's vertical road (world x) and its horizontal one (world y)</summary>
    private static int RoadLineX(int districtX) => WorldLayout.DistrictOriginX(districtX) + WorldLayout.RoadX + 1;

    private static int RoadLineY(int districtY) => WorldLayout.DistrictOriginY(districtY) + WorldLayout.RoadY + 1;

    /// <summary>The middle of a Yard, where its links leave it (they run under the plots to the nearer road)</summary>
    public static TilePos YardAnchor(StationRef station)
    {
        var yard = WorldLayout.Yard(station);
        return new TilePos((yard.X0 + yard.X1) / 2, (yard.Y0 + yard.Y1) / 2);
    }

    /// <summary>Where a Yard's links meet its town's roads: straight from its middle to the nearer road</summary>
    private static (TilePos At, bool Horizontal) RoadExit(StationRef station)
    {
        var anchor = YardAnchor(station);
        var x = RoadLineX(station.District.X);
        var y = RoadLineY(station.District.Y);
        return Math.Abs(anchor.Y - y) <= Math.Abs(anchor.X - x) ? (new TilePos(anchor.X, y), true) : (new TilePos(x, anchor.Y), false);
    }

    /// <summary>
    /// A link's route (1.4 §8): from the seller's Yard gate into its Yard, under the plots to the nearer road, along the
    /// roads (across the wilderness between towns, where the roads of a row or a column of towns line up) to the buyer's
    /// road, and in to its Yard and its gate
    /// </summary>
    public static List<TilePos> LinkRoute(TilePos fromGate, StationRef fromStation, TilePos toGate, StationRef toStation)
    {
        var a = YardAnchor(fromStation);
        var b = YardAnchor(toStation);
        var turnA = new TilePos(fromGate.X, a.Y);
        var route = new List<TilePos> { fromGate, WorldLayout.Yard(fromStation).Contains(turnA) ? turnA : new TilePos(a.X, fromGate.Y), a };
        if (fromStation != toStation)
        {
            var (ea, horizontalA) = RoadExit(fromStation);
            var (eb, horizontalB) = RoadExit(toStation);
            route.Add(ea);
            if (horizontalA && horizontalB && ea.Y != eb.Y)
            {
                // Two horizontal roads: along the first to the buyer's town's vertical road, along it to the second
                var x = RoadLineX(toStation.District.X);
                route.Add(new TilePos(x, ea.Y));
                route.Add(new TilePos(x, eb.Y));
            }
            else if (!horizontalA && !horizontalB && ea.X != eb.X)
            {
                var y = RoadLineY(toStation.District.Y);
                route.Add(new TilePos(ea.X, y));
                route.Add(new TilePos(eb.X, y));
            }
            else if (horizontalA && !horizontalB)
            {
                route.Add(new TilePos(eb.X, ea.Y));
            }
            else if (!horizontalA && horizontalB)
            {
                route.Add(new TilePos(ea.X, eb.Y));
            }
            route.Add(eb);
            route.Add(b);
        }
        var turnB = new TilePos(toGate.X, b.Y);
        route.Add(WorldLayout.Yard(toStation).Contains(turnB) ? turnB : new TilePos(b.X, toGate.Y));
        route.Add(toGate);
        return route.Where((t, i) => i == 0 || t != route[i - 1]).ToList();
    }

    public static int RouteLength(IReadOnlyList<TilePos> route) =>
        route.Zip(route.Skip(1), (p, q) => Math.Abs(p.X - q.X) + Math.Abs(p.Y - q.Y)).Sum();

    private void RouteLink(Link link)
    {
        if (EstateOf(Company(link.Seller)!) is { } from && EstateOf(Company(link.Buyer)!) is { } to)
        {
            link.Route = LinkRoute(from.YardGate, from.Station, to.YardGate, to.Station);
            link.Length = RouteLength(link.Route);
            LinkChanged(link);
        }
    }

    /// <summary>A company's estate landed somewhere else: its links take the new way (for free)</summary>
    private void RerouteLinks(Company company)
    {
        foreach (var link in LinksOf(company.Id).ToList())
        {
            RouteLink(link);
        }
    }

    // ---------------------------------------------------------------- Offers and notices

    /// <summary>What a link would be: its route, how much arrives, what the pipe costs and why it can't be (or null)</summary>
    public sealed record LinkPlan(List<TilePos> Route, int Length, double Efficiency, double Throughput, double Gold, IReadOnlyDictionary<string, double> Materials, string? Error);

    public LinkPlan PlanLink(long companyId, long partnerId, string resource, bool selling, int tier)
    {
        LinkPlan Fail(string error) => new([], 0, 0, 0, 0, new Dictionary<string, double>(), error);
        if (Company(companyId) is not { } company || Company(partnerId) is not { } partner || partner.Id == company.Id)
        {
            return Fail("Unknown company.");
        }
        if (!Content.Resources.ContainsKey(resource))
        {
            return Fail("Unknown resource.");
        }
        if (Content.PipeTiers.FirstOrDefault(t => t.Tier == tier) is not { } pipe)
        {
            return Fail("Unknown pipe.");
        }
        if (pipe.Era > YourEra(company) || (pipe.Research != null && !HasResearch(company, pipe.Research)))
        {
            return Fail($"Research {pipe.Name} first.");
        }
        if (EstateOf(company) is not { } mine || EstateOf(partner) is not { } theirs)
        {
            return Fail("One of you is on the way: link when you've landed.");
        }
        if (mine.Station == theirs.Station)
        {
            return Fail($"{partner.Name} lives at your Yard: you trade through the Yard already.");
        }
        var (seller, buyer) = selling ? (mine, theirs) : (theirs, mine);
        // To another town only over a trunk line between the two (1.4 §20): the link pays its pipe up to the town gates
        var line = mine.District == theirs.District ? null : LineBetween(mine.District, theirs.District);
        if (mine.District != theirs.District && line == null)
        {
            return Fail(LineNeighbours(mine.District).Contains(theirs.District)
                ? $"{partner.Name} lives in {WorldNames.District(theirs.District)}: a link there runs over a trunk line, and nobody built one yet (Era 3, Trunk Lines)."
                : $"{partner.Name} lives in {WorldNames.District(theirs.District)}, too far: trunk lines join only neighbouring towns.");
        }
        var route = LinkRoute(seller.YardGate, seller.Station, buyer.YardGate, buyer.Station);
        var length = RouteLength(route);
        var piped = line == null ? length : Math.Max(0, length - line.Length);
        var materials = pipe.Materials.ToDictionary(t => t.Key, t => t.Value * piped);
        var efficiency = LinkEfficiency(tier, piped) * (line == null ? 1 : LineEfficiency(line));
        var throughput = line == null ? pipe.Throughput : Math.Min(pipe.Throughput, LineThroughput(line));
        return new LinkPlan(route, length, efficiency, throughput, pipe.Gold * piped, materials, null);
    }

    /// <summary>
    /// Offers a link (v1.2 §7): you sell a good to a company outside your station, or buy one from it. The offerer pays the
    /// pipe; the other side accepts with one tap (an offer waits three days, then the pipe is refunded).
    /// </summary>
    public (CommandResult Result, Link? Link) OfferLink(long companyId, long partnerId, string resource, bool selling, int tier = 1)
    {
        var plan = PlanLink(companyId, partnerId, resource, selling, tier);
        if (plan.Error != null)
        {
            return (CommandResult.Fail(plan.Error), null);
        }
        var company = Company(companyId)!;
        var partner = Company(partnerId)!;
        var (seller, buyer) = selling ? (company, partner) : (partner, company);
        if (_links.Values.Any(t => t.Seller == seller.Id && t.Buyer == buyer.Id && t.Resource == resource))
        {
            return (CommandResult.Fail("There is a link for that already."), null);
        }
        if (LinksOf(company.Id).Count() >= MaxLinksPerEstate)
        {
            return (CommandResult.Fail($"A plot has at most {MaxLinksPerEstate} links."), null);
        }
        if (LinksOf(partner.Id).Count() >= MaxLinksPerEstate)
        {
            return (CommandResult.Fail($"{partner.Name} has {MaxLinksPerEstate} links already."), null);
        }
        if (_links.Values.Count(t => t.OfferedBy == company.Id && t.CreatedAt > LastTick - TimeSpan.FromHours(1)) >= MaxLinkOffersPerHour)
        {
            return (CommandResult.Fail($"At most {MaxLinkOffersPerHour} offers an hour."), null);
        }
        if (!TradesWith(company, partner) || !TradesWith(partner, company))
        {
            return (CommandResult.Fail($"You don't trade with {partner.Name}."), null);
        }
        var payment = Pay(company, plan.Gold, plan.Materials);
        if (!payment.Success)
        {
            return (payment, null);
        }
        var link = new Link
        {
            Id = NextLinkId++,
            Seller = seller.Id,
            Buyer = buyer.Id,
            Resource = resource,
            Tier = tier,
            OfferedBy = company.Id,
            State = LinkState.Offered,
            CreatedAt = LastTick,
            Paid = plan.Gold,
            PaidMaterials = plan.Materials.ToDictionary(),
            Route = plan.Route,
            Length = plan.Length,
        };
        _links[link.Id] = link;
        company.Report.GoldSpent += plan.Gold;
        AddStat(company, "goldSpent", plan.Gold);
        company.Touch();
        Changes.Companies.Add(company.Id);
        LinkChanged(link);
        var what = Content.Resources[resource].Name.ToLowerInvariant();
        Changes.Events.Add(new WorldEvent(partner.Id, WorldEventKind.Link, selling
            ? $"{company.Name} offers you a link: their {what} at the player price. Accept it in your Links"
            : $"{company.Name} would like to buy your {what} through a link. Accept it in your Links"));
        return (CommandResult.Ok, link);
    }

    public CommandResult AcceptLink(long companyId, long linkId)
    {
        if (Link(linkId) is not { State: LinkState.Offered } link || link.OfferedBy == companyId || link.Partner(link.OfferedBy) != companyId)
        {
            return CommandResult.Fail("That offer isn't open for you.");
        }
        if (LinksOf(companyId).Count(t => t.State != LinkState.Offered) >= MaxLinksPerEstate)
        {
            return CommandResult.Fail($"A plot has at most {MaxLinksPerEstate} links.");
        }
        link.State = LinkState.Active;
        link.AcceptedAt = LastTick;
        RouteLink(link);
        LinkChanged(link);
        WakeCoupling();
        var accepter = Company(companyId)!;
        Changes.Events.Add(new WorldEvent(link.OfferedBy, WorldEventKind.Link,
            $"{accepter.Name} accepted your link: {Content.Resources[link.Resource].Name.ToLowerInvariant()} flows from the next round"));
        return CommandResult.Ok;
    }

    /// <summary>Declines an offer (or takes back your own): the pipe is refunded to whoever paid it</summary>
    public CommandResult DeclineLink(long companyId, long linkId)
    {
        if (Link(linkId) is not { State: LinkState.Offered } link || (link.Seller != companyId && link.Buyer != companyId))
        {
            return CommandResult.Fail("That offer isn't open.");
        }
        RemoveLink(link, refund: true);
        if (link.OfferedBy != companyId)
        {
            Changes.Events.Add(new WorldEvent(link.OfferedBy, WorldEventKind.Link,
                $"{Company(companyId)!.Name} declined your link; the pipe is refunded"));
        }
        return CommandResult.Ok;
    }

    /// <summary>Pauses a link or ends it: either side, with an hour's notice (it keeps flowing until then)</summary>
    public CommandResult EndLink(long companyId, long linkId, bool end)
    {
        if (Link(linkId) is not { } link || (link.Seller != companyId && link.Buyer != companyId) || link.State == LinkState.Offered)
        {
            return CommandResult.Fail("Unknown link.");
        }
        if (link.State == LinkState.Paused)
        {
            if (!end)
            {
                return CommandResult.Fail("The link is paused already.");
            }
            // Nothing flows: it ends right away
            RemoveLink(link, refund: false);
            Changes.Events.Add(new WorldEvent(link.Partner(companyId), WorldEventKind.Link, $"{Company(companyId)!.Name} ended your {LinkName(link)}"));
            return CommandResult.Ok;
        }
        if (link.NoticeAt != null && (link.NoticeEnds || !end))
        {
            return CommandResult.Fail("The notice is running already.");
        }
        link.NoticeAt = LastTick + LinkNotice;
        link.NoticeEnds = end;
        link.NoticeBy = companyId;
        LinkChanged(link);
        Changes.Events.Add(new WorldEvent(link.Partner(companyId), WorldEventKind.Link,
            $"{Company(companyId)!.Name} {(end ? "ends" : "pauses")} your {LinkName(link)} in an hour: the Yard and the hub fill in after that"));
        return CommandResult.Ok;
    }

    /// <summary>Resumes a paused link (either side), or takes back a notice</summary>
    public CommandResult ResumeLink(long companyId, long linkId)
    {
        if (Link(linkId) is not { } link || (link.Seller != companyId && link.Buyer != companyId) || link.State == LinkState.Offered)
        {
            return CommandResult.Fail("Unknown link.");
        }
        if (link.State == LinkState.Active && link.NoticeAt == null)
        {
            return CommandResult.Fail("The link flows already.");
        }
        if (link.State == LinkState.Active && link.NoticeBy != companyId)
        {
            return CommandResult.Fail($"{Company(link.Partner(companyId))!.Name} gave the notice: only they take it back.");
        }
        if (link.State == LinkState.Paused && link.PausedBy != companyId && link.PausedBy != null)
        {
            // The other side paused it: resuming is theirs
            return CommandResult.Fail($"{Company(link.PausedBy.Value)?.Name ?? "Your partner"} paused it: only they resume it.");
        }
        link.State = LinkState.Active;
        link.NoticeAt = null;
        link.NoticeBy = null;
        link.PausedBy = null;
        LinkChanged(link);
        WakeCoupling();
        Changes.Events.Add(new WorldEvent(link.Partner(companyId), WorldEventKind.Link, $"{Company(companyId)!.Name}'s {LinkName(link)} flows again"));
        return CommandResult.Ok;
    }

    /// <summary>A better pipe for a link (either side pays the new pipe; the old one is gone)</summary>
    public CommandResult UpgradeLink(long companyId, long linkId, int tier)
    {
        if (Link(linkId) is not { } link || (link.Seller != companyId && link.Buyer != companyId))
        {
            return CommandResult.Fail("Unknown link.");
        }
        var company = Company(companyId)!;
        if (Content.PipeTiers.FirstOrDefault(t => t.Tier == tier) is not { } pipe || pipe.Tier <= link.Tier)
        {
            return CommandResult.Fail("Choose a better pipe.");
        }
        if (pipe.Era > YourEra(company) || (pipe.Research != null && !HasResearch(company, pipe.Research)))
        {
            return CommandResult.Fail($"Research {pipe.Name} first.");
        }
        var payment = Pay(company, pipe.Gold * link.Length, pipe.Materials.ToDictionary(t => t.Key, t => t.Value * link.Length));
        if (!payment.Success)
        {
            return payment;
        }
        company.Report.GoldSpent += pipe.Gold * link.Length;
        AddStat(company, "goldSpent", pipe.Gold * link.Length);
        link.Tier = tier;
        LinkChanged(link);
        WakeCoupling();
        return CommandResult.Ok;
    }

    private string LinkName(Link link) => $"{Content.Resources[link.Resource].Name.ToLowerInvariant()} link";

    private void RemoveLink(Link link, bool refund)
    {
        if (refund && Company(link.OfferedBy) is { } payer)
        {
            payer.Gold += link.Paid;
            payer.Report.GoldSpent -= link.Paid;
            payer.Touch();
            Changes.Companies.Add(payer.Id);
            // The materials went into the pipe: their worth comes back as gold at the hub's price
            foreach (var (resource, amount) in link.PaidMaterials)
            {
                payer.Gold += amount * BasePrice(resource);
            }
        }
        _links.Remove(link.Id);
        Deleted.Add(link);
        Changes.Links.Add(link.Seller);
        Changes.Links.Add(link.Buyer);
        WakeCoupling();
    }

    private void LinkChanged(Link link)
    {
        link.Touch();
        Changes.Links.Add(link.Seller);
        Changes.Links.Add(link.Buyer);
    }

    /// <summary>The moments offers lapse and notices take effect</summary>
    private IEnumerable<(DateTime At, Action Run)> LinkMoments(DateTime from, DateTime to)
    {
        foreach (var link in _links.Values.OrderBy(t => t.Id))
        {
            if (link.State == LinkState.Offered && OfferLapsesAt(link) is var lapses && lapses <= to)
            {
                yield return (lapses, () => LapseOffer(link));
            }
            else if (link.NoticeAt is { } at && at <= to)
            {
                yield return (at, () => NoticeEnds(link));
            }
        }
    }

    private void LapseOffer(Link link)
    {
        if (!_links.ContainsKey(link.Id) || link.State != LinkState.Offered)
        {
            return;
        }
        RemoveLink(link, refund: true);
        Changes.Events.Add(new WorldEvent(link.OfferedBy, WorldEventKind.Link,
            $"Your {LinkName(link)} offer wasn't taken; the pipe is refunded"));
    }

    private void NoticeEnds(Link link)
    {
        if (!_links.ContainsKey(link.Id) || link.NoticeAt == null)
        {
            return;
        }
        var by = link.NoticeBy;
        if (link.NoticeEnds)
        {
            RemoveLink(link, refund: false);
            foreach (var side in new[] { link.Seller, link.Buyer })
            {
                Changes.Events.Add(new WorldEvent(side, WorldEventKind.Link, $"The {LinkName(link)} between {Company(link.Seller)?.Name} and {Company(link.Buyer)?.Name} has ended"));
            }
            return;
        }
        link.State = LinkState.Paused;
        link.PausedBy = by;
        link.NoticeAt = null;
        link.NoticeBy = null;
        LinkChanged(link);
        WakeCoupling();
    }

    // ---------------------------------------------------------------- The rounds

    /// <summary>
    /// What a link brought since the last round arrives at its buyer now: what the seller sent (less what it sent short,
    /// less the pipe's loss) up to what the buyer needed, the rest into the tank; when less came, the tank makes up for it
    /// </summary>
    private void DeliverLinks(Dictionary<EstateKey, YardMember> members, Func<EstateKey, GateTerms> termsOf, ref TimeSpan? limit)
    {
        foreach (var link in _links.Values.Where(t => t.State != LinkState.Offered).OrderBy(t => t.Id))
        {
            var previousRound = link.LastRound;
            // The tank since the last round
            if (link.LastRound is { } last && LastTick > last)
            {
                link.Tank = Math.Clamp(link.Tank + link.TankChange * (LastTick - last).TotalMinutes, 0, link.TankSize);
            }
            link.LastRound = LastTick;
            link.TankChange = 0;
            link.Rate = 0;
            if (Company(link.Seller) is not { } seller || Company(link.Buyer) is not { } buyer
                || EstateOf(buyer) is not { } buyerEstate || !members.TryGetValue(KeyOf(buyerEstate), out _))
            {
                continue;
            }
            var arrived = 0.0;
            if (link.Flows && EstateOf(seller) is { } sellerEstate && members.TryGetValue(KeyOf(sellerEstate), out var from)
                && _gateTerms.TryGetValue(KeyOf(sellerEstate), out var old))
            {
                var exported = old.Export.GetValueOrDefault(link.Resource);
                var ratio = exported <= 1e-12 ? 0 : Math.Min(1, from.Sent.GetValueOrDefault(link.Resource) / exported);
                arrived = link.Send * ratio * LinkEfficiency(link);
            }
            var need = link.Flows ? link.Need : 0;
            var taken = Math.Min(arrived, need);
            var drain = need > taken && link.Tank > 1e-9 ? need - taken : 0;
            link.Rate = taken + drain;
            link.TankChange = arrived - taken - drain;
            if (drain > 0)
            {
                // The next round comes before the tank runs dry
                var minutes = link.Tank / drain;
                var until = TimeSpan.FromMinutes(Math.Clamp(minutes, CouplingInterval.TotalMinutes, MaxCouplingInterval.TotalMinutes));
                limit = limit is { } l && l < until ? l : until;
            }
            else if (link.TankChange > 1e-9 && link.TankSize > link.Tank)
            {
                var until = TimeSpan.FromMinutes(Math.Clamp((link.TankSize - link.Tank) / link.TankChange, CouplingInterval.TotalMinutes, MaxCouplingInterval.TotalMinutes));
                limit = limit is { } l && l < until ? l : until;
            }
            if (link.Rate <= 1e-9)
            {
                link.Rate = 0;
                continue;
            }
            // Between towns the trunk line's carrier takes its fee from the seller (1.4 §20)
            if (LineOf(link) is { } line && previousRound is { } then && LastTick > then)
            {
                PayLineFee(line, seller, link.Resource, link.Rate * (LastTick - then).TotalMinutes);
            }
            var import = termsOf(KeyOf(buyerEstate)).Import;
            var priceClass = GateClass(seller, buyer, link.Resource);
            if (!import.TryGetValue((link.Resource, priceClass), out var sources))
            {
                import[(link.Resource, priceClass)] = sources = new Dictionary<long, double>();
            }
            sources[seller.Id] = sources.GetValueOrDefault(seller.Id) + link.Rate;
        }
    }

    /// <summary>
    /// The links' share of this round, before the Yard (1.4 §8: links, then the Yard, then the market): each
    /// link takes what its buyer still lacks (as far as its pipe carries it) from what its seller still has spare, plus
    /// what fills the buyer's tank; what's left goes to the Yard and the hub
    /// </summary>
    private void MatchLinks(Dictionary<EstateKey, YardMember> members, Func<EstateKey, GateTerms> termsOf)
    {
        foreach (var link in _links.Values.Where(t => t.State != LinkState.Offered).OrderBy(t => t.Id))
        {
            link.Need = 0;
            link.Send = 0;
            if (!link.Flows)
            {
                continue;
            }
            var seller = Company(link.Seller);
            var buyer = Company(link.Buyer);
            YardMember? from = seller != null && EstateOf(seller) is { } s ? members.GetValueOrDefault(KeyOf(s)) : null;
            YardMember? to = buyer != null && EstateOf(buyer) is { } b ? members.GetValueOrDefault(KeyOf(b)) : null;
            if (seller == null || buyer == null || to == null || (from == null && link.Tank <= 1e-9))
            {
                link.Flow = LinkFlow.NotConnected;
                continue;
            }
            if (!YardTradeAllowed(seller, buyer, link.Resource))
            {
                link.Flow = LinkFlow.NotAllowed;
                continue;
            }
            var efficiency = LinkEfficiency(link);
            var throughput = LinkThroughput(link);
            var lacks = to.Lacks.GetValueOrDefault(link.Resource);
            var spare = from?.Spare.GetValueOrDefault(link.Resource) ?? 0;
            var need = Math.Min(lacks, throughput * efficiency);
            var send = Math.Min(need / efficiency, spare);
            link.Flow = from == null ? LinkFlow.NotConnected
                : need <= 1e-9 ? LinkFlow.NotNeeded
                : send * efficiency < need - 1e-6 ? LinkFlow.SellerShort
                : lacks > throughput * efficiency + 1e-6 && spare > send + 1e-6 ? LinkFlow.Capped
                : LinkFlow.Live;
            if (need > 1e-9)
            {
                link.TankSize = need * TankMinutes;
            }
            // What the seller can't send now the tank covers while it has some; the rest is for the Yard and the hub
            var covered = link.Tank > 1e-9 ? need : send * efficiency;
            var fill = from != null && link.Tank < link.TankSize - 1e-9
                ? Math.Min(Math.Min(spare - send, throughput - send), link.TankSize / TankFillMinutes / efficiency)
                : 0;
            fill = Math.Max(0, fill);
            link.Need = need;
            link.Send = send + fill;
            Add(to.Lacks, link.Resource, -covered);
            if (from != null && link.Send > 1e-9)
            {
                Add(from.Spare, link.Resource, -link.Send);
                var terms = termsOf(from.Key);
                terms.Export[link.Resource] = terms.Export.GetValueOrDefault(link.Resource) + link.Send;
            }
        }
    }

    /// <summary>Counts what a link brought its buyer (for the panel's "since it started")</summary>
    private void CountLinkDeliveries(double minutes)
    {
        foreach (var link in _links.Values.Where(t => t.Rate > 0))
        {
            link.Delivered += link.Rate * minutes;
        }
    }

    // ---------------------------------------------------------------- Suggestions

    /// <summary>Someone outside your station a link would suit: they have spare what you lack, or lack what you have spare</summary>
    public sealed record LinkSuggestion(Company Partner, string Resource, bool Selling, double PerMinute, int Length);

    /// <summary>
    /// Who a link would suit (v1.2 §7): estates outside your station whose gate has spare what yours still lacks after the
    /// Yard (or lacks what you still have spare), the nearest first
    /// </summary>
    public List<LinkSuggestion> LinkSuggestions(Company company, int count = 8)
    {
        SolveNetworks();
        if (EstateOf(company) is not { } mine || GateNetwork(mine) is not { } network)
        {
            return [];
        }
        // Spare and lacks measured as the Yard measures them (a lack counts only as far as the building can use it)
        var stations = new Dictionary<StationRef, List<YardMember>>();
        YardMember? Member(Estate estate)
        {
            var corner = estate.Station;
            if (!stations.TryGetValue(corner, out var members))
            {
                stations[corner] = members = YardMembers(corner);
            }
            return members.FirstOrDefault(t => t.Key == KeyOf(estate) && t.Network != null);
        }
        if (Member(mine) is not { } me)
        {
            return [];
        }
        var gets = network.GateFlows.Where(t => t.Import && t.Estate == me.Key).GroupBy(t => t.Resource).ToDictionary(t => t.Key, t => t.Sum(f => f.Sent));
        var lacks = me.Lacks.Where(t => t.Value - gets.GetValueOrDefault(t.Key) > 0.05).Select(t => t.Key).ToHashSet();
        var spare = me.Spare.Where(t => t.Value - me.Sent.GetValueOrDefault(t.Key) > 0.05).Select(t => t.Key).ToHashSet();
        if (lacks.Count == 0 && spare.Count == 0)
        {
            return [];
        }
        var home = mine.Station;
        var suggestions = new List<LinkSuggestion>();
        foreach (var other in _companies.Values.Where(t => t.Id != company.Id && t.Packed == null && IsActive(t)))
        {
            if (EstateOf(other) is not { } theirs || theirs.Station == home || GateNetwork(theirs) is not { } theirNetwork
                || !TradesWith(company, other) || !TradesWith(other, company))
            {
                continue;
            }
            if (Member(theirs) is not { } them)
            {
                continue;
            }
            var theirGets = theirNetwork.GateFlows.Where(t => t.Import && t.Estate == them.Key).GroupBy(t => t.Resource).ToDictionary(t => t.Key, t => t.Sum(f => f.Sent));
            var length = RouteLength(LinkRoute(mine.YardGate, mine.Station, theirs.YardGate, theirs.Station));
            foreach (var resource in lacks)
            {
                var theirSpare = them.Spare.GetValueOrDefault(resource) - them.Sent.GetValueOrDefault(resource);
                if (theirSpare > 0.05 && LinkBetween(other.Id, company.Id, resource) == null)
                {
                    suggestions.Add(new LinkSuggestion(other, resource, false, theirSpare, length));
                }
            }
            foreach (var resource in spare)
            {
                var theirLack = them.Lacks.GetValueOrDefault(resource) - theirGets.GetValueOrDefault(resource);
                if (theirLack > 0.05 && LinkBetween(company.Id, other.Id, resource) == null)
                {
                    suggestions.Add(new LinkSuggestion(other, resource, true, theirLack, length));
                }
            }
        }
        return suggestions.OrderBy(t => t.Length).ThenByDescending(t => t.PerMinute).Take(count).ToList();
    }
}
