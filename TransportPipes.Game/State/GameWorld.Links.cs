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
    public static readonly TimeSpan LinkOfferLifetime = TimeSpan.FromHours(72);
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

    /// <summary>What a link's pipe carries per minute (with the seller's pipe research)</summary>
    public double LinkThroughput(Link link) => LinkPipe(link).Throughput * (1 + ThroughputBonus(link.Seller));

    /// <summary>The share of what the seller sends that reaches the buyer's gate: the pipe's loss per tile over the route</summary>
    public double LinkEfficiency(Link link) => LinkEfficiency(link.Tier, link.Length);

    public double LinkEfficiency(int tier, int length) => Math.Pow(1 - Content.PipeTier(tier).LossPerTile, length);

    // ---------------------------------------------------------------- The route

    /// <summary>Whether a world x lies on a vertical corridor line (its first tile; corridors are two wide)</summary>
    private static bool OnXLine(int x)
    {
        var district = WorldLayout.DistrictOf(x, 0).X;
        var local = x - WorldLayout.DistrictOriginX(district);
        return local is >= 0 and < WorldLayout.DistrictSize && local % WorldLayout.BlockPitch == 0;
    }

    private static bool OnYLine(int y)
    {
        var district = WorldLayout.DistrictOf(0, y).Y;
        var local = y - WorldLayout.DistrictOriginY(district);
        return local is >= 0 and < WorldLayout.DistrictSize && local % WorldLayout.BlockPitch == 0;
    }

    /// <summary>The corridor lines (their first tiles) nearest below and above a coordinate, along one axis</summary>
    private static IEnumerable<int> LinesAround(int v, Func<int, int> origin, Func<int, int> districtOf)
    {
        var district = districtOf(v);
        foreach (var d in new[] { district - 1, district, district + 1 })
        {
            for (var k = 0; k <= WorldLayout.BlocksPerSide; k++)
            {
                yield return origin(d) + k * WorldLayout.BlockPitch;
            }
        }
    }

    /// <summary>The line to turn on between two coordinates: one between them when there is one (no detour), else the nearest</summary>
    private static int TurnLine(int a, int b, Func<int, int> origin, Func<int, int> districtOf)
    {
        var (low, high) = (Math.Min(a, b), Math.Max(a, b));
        var lines = LinesAround(low, origin, districtOf).Concat(LinesAround(high, origin, districtOf)).Distinct().ToList();
        return lines.OrderBy(t => t >= low && t <= high ? 0 : Math.Min(Math.Abs(t - low), Math.Abs(t - high))).ThenBy(t => t).First();
    }

    /// <summary>Where a station's Yard meets the corridors: the crossing (or the corridor) in its middle</summary>
    public static TilePos YardAnchor(StationRef station)
    {
        var yard = WorldLayout.Yard(station);
        var cx = (yard.X0 + yard.X1) / 2;
        var cy = (yard.Y0 + yard.Y1) / 2;
        // The first tile of the two-wide corridor through the Yard's middle
        foreach (var x in new[] { cx - 1, cx, cx + 1 })
        {
            if (OnXLine(x))
            {
                cx = x;
                break;
            }
        }
        foreach (var y in new[] { cy - 1, cy, cy + 1 })
        {
            if (OnYLine(y))
            {
                cy = y;
                break;
            }
        }
        return new TilePos(cx, cy);
    }

    /// <summary>
    /// A link's route (v1.2 §7): from the seller's Yard gate under its Yard to the corridors, along them (turning at most
    /// twice, through the wilderness between districts where it has to) to the buyer's Yard and its gate
    /// </summary>
    public static List<TilePos> LinkRoute(TilePos fromGate, StationRef fromStation, TilePos toGate, StationRef toStation)
    {
        var a = YardAnchor(fromStation);
        var b = YardAnchor(toStation);
        // From a gate straight into its Yard, then under it to the corridor in its middle
        TilePos Turn(TilePos gate, TilePos anchor, StationRef station) =>
            WorldLayout.Yard(station).Contains(new TilePos(gate.X, anchor.Y)) ? new TilePos(gate.X, anchor.Y) : new TilePos(anchor.X, gate.Y);
        var route = new List<TilePos> { fromGate, Turn(fromGate, a, fromStation), a };
        bool AH(TilePos p) => OnYLine(p.Y);
        bool AV(TilePos p) => OnXLine(p.X);
        int XTurn(int x0, int x1) => TurnLine(x0, x1, WorldLayout.DistrictOriginX, x => WorldLayout.DistrictOf(x, 0).X);
        int YTurn(int y0, int y1) => TurnLine(y0, y1, WorldLayout.DistrictOriginY, y => WorldLayout.DistrictOf(0, y).Y);
        if (a == b)
        {
        }
        else if (AH(a) && AV(b))
        {
            route.Add(new TilePos(b.X, a.Y));
        }
        else if (AV(a) && AH(b))
        {
            route.Add(new TilePos(a.X, b.Y));
        }
        else if (AH(a) && AH(b))
        {
            if (a.Y != b.Y)
            {
                var x = XTurn(a.X, b.X);
                route.Add(new TilePos(x, a.Y));
                route.Add(new TilePos(x, b.Y));
            }
        }
        else
        {
            if (a.X != b.X)
            {
                var y = YTurn(a.Y, b.Y);
                route.Add(new TilePos(a.X, y));
                route.Add(new TilePos(b.X, y));
            }
        }
        route.Add(b);
        route.Add(Turn(toGate, b, toStation));
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
        if (pipe.Era > Era || (pipe.Research != null && !HasResearch(company, pipe.Research)))
        {
            return Fail($"Research {pipe.Name} first.");
        }
        if (partner.IsNpc)
        {
            return Fail("Houses trade through their Yard.");
        }
        if (EstateOf(company) is not { } mine || EstateOf(partner) is not { } theirs)
        {
            return Fail("One of you is on the way: link when you've landed.");
        }
        if (BigStation(mine.Station)[0] == BigStation(theirs.Station)[0])
        {
            return Fail($"{partner.Name} lives in your station: you trade through the Yard already.");
        }
        var (seller, buyer) = selling ? (mine, theirs) : (theirs, mine);
        var route = LinkRoute(seller.YardGate, seller.Station, buyer.YardGate, buyer.Station);
        var length = RouteLength(route);
        var materials = pipe.Materials.ToDictionary(t => t.Key, t => t.Value * length);
        return new LinkPlan(route, length, LinkEfficiency(tier, length), pipe.Throughput, pipe.Gold * length, materials, null);
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
            return (CommandResult.Fail($"An estate has at most {MaxLinksPerEstate} links."), null);
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
            return CommandResult.Fail($"An estate has at most {MaxLinksPerEstate} links.");
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
        if (pipe.Era > Era || (pipe.Research != null && !HasResearch(company, pipe.Research)))
        {
            return CommandResult.Fail($"Research {pipe.Name} first.");
        }
        var payment = Pay(company, pipe.Gold * link.Length, pipe.Materials.ToDictionary(t => t.Key, t => t.Value * link.Length));
        if (!payment.Success)
        {
            return payment;
        }
        company.Report.GoldSpent += pipe.Gold * link.Length;
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
            if (link.State == LinkState.Offered && link.CreatedAt + LinkOfferLifetime <= to)
            {
                yield return (link.CreatedAt + LinkOfferLifetime, () => LapseOffer(link));
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
            $"Nobody took your {LinkName(link)} offer in three days; the pipe is refunded"));
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
            var import = termsOf(KeyOf(buyerEstate)).Import;
            if (!import.TryGetValue((link.Resource, 0), out var sources))
            {
                import[(link.Resource, 0)] = sources = new Dictionary<long, double>();
            }
            sources[seller.Id] = sources.GetValueOrDefault(seller.Id) + link.Rate;
        }
    }

    /// <summary>
    /// The links' share of this round, before the Yard (v1.2 §7: contracts, then links, then the Yard, then the hub): each
    /// link takes what its buyer still lacks (as far as its pipe carries it) from what its seller still has spare, plus
    /// what fills the buyer's tank; what's left goes to the Yard and the hub
    /// </summary>
    private void MatchLinks(Dictionary<EstateKey, YardMember> members, Func<EstateKey, GateTerms> termsOf)
    {
        var contracted = _contracts.Values.Where(t => t.State == ContractState.Active).Select(t => (t.Seller, t.Buyer, t.Resource)).ToHashSet();
        foreach (var link in _links.Values.Where(t => t.State != LinkState.Offered)
                     .OrderBy(t => contracted.Contains((t.Seller, t.Buyer, t.Resource)) ? 0 : 1).ThenBy(t => t.Id))
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
        var me = new YardMember { Key = KeyOf(mine), Company = company, Network = network };
        Measure(me);
        var gets = network.GateFlows.Where(t => t.Import && t.Estate == me.Key).GroupBy(t => t.Resource).ToDictionary(t => t.Key, t => t.Sum(f => f.Sent));
        var lacks = me.Lacks.Where(t => t.Value - gets.GetValueOrDefault(t.Key) > 0.05).Select(t => t.Key).ToHashSet();
        var spare = me.Spare.Where(t => t.Value - me.Sent.GetValueOrDefault(t.Key) > 0.05).Select(t => t.Key).ToHashSet();
        if (lacks.Count == 0 && spare.Count == 0)
        {
            return [];
        }
        var home = BigStation(mine.Station)[0];
        var suggestions = new List<LinkSuggestion>();
        foreach (var other in _companies.Values.Where(t => !t.IsNpc && t.Id != company.Id && !t.Asleep && t.Packed == null && IsActive(t)))
        {
            if (EstateOf(other) is not { } theirs || BigStation(theirs.Station)[0] == home || GateNetwork(theirs) is not { } theirNetwork
                || !TradesWith(company, other) || !TradesWith(other, company))
            {
                continue;
            }
            var them = new YardMember { Key = KeyOf(theirs), Company = other, Network = theirNetwork };
            Measure(them);
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
