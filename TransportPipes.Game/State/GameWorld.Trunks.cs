using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// A crew trunk (v1.2 §13): a steel pipe from the crew's station along the public corridors to another hub or the Port,
/// built in 10-tile segments that any member pays (shown in their colour). It carries the members' own goods; nobody
/// owns a corridor tile, so a parallel trunk is always possible.
/// </summary>
public sealed class Trunk : Entity
{
    public long Id { get; init; }
    public long Crew { get; init; }

    /// <summary>The (big) station it starts at ("x,y,index" of its corner), and where it goes: a hub's district, or the Port</summary>
    public required string Station { get; init; }
    public DistrictCoord? Hub { get; init; }
    public bool Port { get; init; }

    public List<TilePos> Route { get; set; } = new();
    public int Length { get; set; }

    /// <summary>Who paid each segment so far (their colour on the trunk)</summary>
    public List<long> Segments { get; set; } = new();
    public int SegmentsNeeded { get; set; }
    public DateTime CreatedAt { get; init; }

    public bool Built => Segments.Count >= SegmentsNeeded;
}

/// <summary>Where a company's goods can go beyond its own hub: another hub (its own Hub Order there) or the Port</summary>
public sealed record RemoteMarket(DistrictCoord? Hub, bool Port, double Efficiency, double Fee, long? Trunk);

public sealed partial class GameWorld
{
    public const int TrunkSegmentTiles = 10;
    private const double TrunkSegmentHours = 2;
    private static readonly IReadOnlyDictionary<string, double> TrunkSegmentMaterials = new Dictionary<string, double> { ["steel"] = 300, ["concrete"] = 100 };
    private const double PublicTrunkFee = 0.02;
    private const double PortPrice = 0.6;
    private const int SteelTier = 3;
    private const int CopperTier = 2;

    /// <summary>Selling beyond the own hub (public trunks, the Port, crew trunks): on in the server, off in tests about other things</summary>
    public bool TrunksEnabled { get; set; }

    private readonly Dictionary<long, Trunk> _trunks = new();
    public long NextTrunkId { get; set; } = 1;
    public IEnumerable<Trunk> Trunks => _trunks.Values;

    public void LoadTrunk(Trunk trunk)
    {
        _trunks[trunk.Id] = trunk;
        trunk.Dirty = false;
        NextTrunkId = Math.Max(NextTrunkId, trunk.Id + 1);
    }

    /// <summary>What a company sends beyond its hub per minute this round: by good and market, at the gate (it arrives less the loss)</summary>
    private readonly Dictionary<long, List<(string Resource, RemoteMarket Market, double Sent)>> _remoteSends = new();

    /// <summary>The Port: where it lies (off the Capital's western edge), so the trunks to it have a length</summary>
    private static TilePos PortAnchor => new(WorldLayout.DistrictOriginX(0) - WorldLayout.WildernessWidth / 2, 0);

    /// <summary>Where a hub meets the corridors: the crossing at its block's top corner</summary>
    private static TilePos HubAnchor(DistrictCoord district)
    {
        var hub = WorldLayout.HubOrigin(district);
        return new TilePos(hub.X - WorldLayout.CorridorWidth, hub.Y - WorldLayout.CorridorWidth);
    }

    private static int Manhattan(TilePos a, TilePos b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    /// <summary>
    /// The markets a company reaches beyond its own hub (v1.2 §13): public copper trunks from its hub to the neighbouring
    /// open hubs and the Port, for everyone at a 2% fee that leaves the game; and its crew's built steel trunks
    /// </summary>
    public List<RemoteMarket> RemoteMarkets(Company company)
    {
        var markets = new List<RemoteMarket>();
        if (EstateOf(company) is not { } estate)
        {
            return markets;
        }
        var home = company.District;
        var copper = Content.PipeTier(CopperTier).LossPerTile;
        var toHub = Manhattan(YardAnchor(estate.Station), HubAnchor(home));
        foreach (var (dx, dy) in Directions)
        {
            var neighbour = new DistrictCoord(home.X + dx, home.Y + dy);
            if (_openDistricts.Contains(neighbour))
            {
                markets.Add(new RemoteMarket(neighbour, false, Math.Pow(1 - copper, toHub + WorldLayout.DistrictPitch), PublicTrunkFee, null));
            }
        }
        markets.Add(new RemoteMarket(null, true, Math.Pow(1 - copper, toHub + Manhattan(HubAnchor(home), PortAnchor)), PublicTrunkFee, null));
        var station = ProjectKey(BigStation(estate.Station)[0]);
        foreach (var trunk in _trunks.Values.Where(t => t.Built && t.Station == station && (t.Crew == company.CrewId || CrewAsleep(t.Crew))))
        {
            markets.Add(new RemoteMarket(trunk.Hub, trunk.Port, Math.Pow(1 - Content.PipeTier(SteelTier).LossPerTile, trunk.Length), trunk.Crew == company.CrewId ? 0 : PublicTrunkFee, trunk.Id));
        }
        return markets;
    }

    /// <summary>When every member of a crew sleeps, its trunks are for everybody (at the public fee)</summary>
    private bool CrewAsleep(long crewId) => Crew(crewId) is not { } crew || CrewMembers(crew).All(t => t.Asleep);

    /// <summary>What a market pays a company for a unit that reaches it, less the fee</summary>
    private double RemotePrice(Company company, RemoteMarket market, string resource) =>
        (market.Port
            ? BasePrice(resource) * PortPrice
            : BasePrice(resource) * HubMarketOf(company, market.Hub!.Value).Multiplier(resource)) * (1 - market.Fee);

    /// <summary>
    /// The trunks' share of a station's round, after the Yard and the project: of what each company still has spare (it
    /// would go to its own hub at less than the full price), a share goes to the best market it reaches, so that both
    /// hubs' orders pay alike (a second hub roughly √2 for a split volume good); the Port takes whatever pays better there
    /// </summary>
    private void MatchTrunks(List<YardMember> members, Func<EstateKey, GateTerms> termsOf)
    {
        foreach (var member in members.Where(t => !t.Company.IsNpc && !t.Company.Asleep && t.Network != null))
        {
            var company = member.Company;
            var markets = RemoteMarkets(company);
            var sends = new List<(string, RemoteMarket, double)>();
            foreach (var (resource, spare) in member.Spare.Where(t => t.Value > 1e-6).OrderBy(t => t.Key, StringComparer.Ordinal).ToList())
            {
                if (company.NoTrade.Contains(resource) || Content.Resources[resource].Tier < 1)
                {
                    continue;
                }
                var homeMultiplier = HubMarketOf(company, company.District).Multiplier(resource);
                if (homeMultiplier >= 0.999)
                {
                    // Its own hub pays the full price for all of it
                    continue;
                }
                var tier = Content.Resources[resource].Tier;
                var best = markets.Where(t => !t.Port || tier >= 2)
                    .Select(t => (Market: t, Value: RemotePrice(company, t, resource) * t.Efficiency))
                    .OrderByDescending(t => t.Value)
                    .FirstOrDefault();
                if (best.Market == null || best.Value <= BasePrice(resource) * homeMultiplier + 1e-9)
                {
                    continue;
                }
                double share;
                if (best.Market.Port)
                {
                    // Unlimited at 0.6: the rest stays home until its price there reaches the Port's
                    var home = HubDemand(company.District, resource);
                    var sold = HubMarketOf(company, company.District).SoldRate.GetValueOrDefault(resource) + SentBeyond(company, resource);
                    var keep = home / Math.Pow(PortPrice * (1 - best.Market.Fee) * best.Market.Efficiency, 2);
                    share = sold <= 1e-9 ? 0 : Math.Clamp((sold - keep) / sold, 0, 1);
                }
                else
                {
                    // Both orders pay alike: x = S·D₂e² ÷ (D₁ + D₂e²)
                    var d1 = HubDemand(company.District, resource);
                    var e2 = Math.Pow(best.Market.Efficiency * (1 - best.Market.Fee), 2);
                    var d2 = HubDemand(best.Market.Hub!.Value, resource) * e2;
                    share = d1 + d2 <= 1e-12 ? 0 : d2 / (d1 + d2);
                }
                var amount = spare * share;
                if (amount <= 1e-6)
                {
                    continue;
                }
                Add(member.Spare, resource, -amount);
                var terms = termsOf(member.Key);
                terms.Export[resource] = terms.Export.GetValueOrDefault(resource) + amount;
                sends.Add((resource, best.Market, amount));
            }
            _remoteMatched[company.Id] = sends;
        }
    }

    /// <summary>What the last round matched for the trunks (it is sent this round, less when a company sent less)</summary>
    private readonly Dictionary<long, List<(string Resource, RemoteMarket Market, double Amount)>> _remoteMatched = new();

    private double SentBeyond(Company company, string resource) =>
        _remoteSends.TryGetValue(company.Id, out var sends) ? sends.Where(t => t.Resource == resource).Sum(t => t.Sent) : 0;

    /// <summary>What was matched for the trunks last round leaves the gates now (in the shares the companies really sent)</summary>
    private void DeliverTrunks(List<YardMember> members)
    {
        foreach (var member in members)
        {
            _remoteSends.Remove(member.Company.Id);
            if (!_remoteMatched.TryGetValue(member.Company.Id, out var matched) || !_gateTerms.TryGetValue(member.Key, out var terms))
            {
                continue;
            }
            var sends = new List<(string, RemoteMarket, double)>();
            foreach (var (resource, market, amount) in matched)
            {
                var exported = terms.Export.GetValueOrDefault(resource);
                var ratio = exported <= 1e-12 ? 0 : Math.Min(1, member.Sent.GetValueOrDefault(resource) / exported);
                if (amount * ratio > 1e-9)
                {
                    sends.Add((resource, market, amount * ratio));
                }
            }
            _remoteSends[member.Company.Id] = sends;
        }
    }

    /// <summary>What reaches the far markets is paid (their price, less the public fee) and counts in the company's order there</summary>
    private void SettleTrunks(double minutes)
    {
        foreach (var (companyId, sends) in _remoteSends)
        {
            if (Company(companyId) is not { } company)
            {
                continue;
            }
            foreach (var (resource, market, sent) in sends)
            {
                var units = sent * minutes * market.Efficiency;
                var gold = units * RemotePrice(company, market, resource);
                company.Gold += gold;
                company.Report.GoldEarned += gold;
                company.LifetimeEarnings += gold;
                Add(company.Report.Sold, resource, units);
                if (market.Hub is { } hub)
                {
                    HubMarketOf(company, hub).AddSold(resource, units);
                }
                GainXpFromEarnings(company, gold);
                company.Touch();
                Changes.Companies.Add(company.Id);
            }
        }
    }

    /// <summary>What a company sends beyond its own hub now: the good, where to, how much per minute arrives, at what price</summary>
    public IReadOnlyList<(string Resource, RemoteMarket Market, double PerMinute, double Price)> RemoteSales(Company company) =>
        _remoteSends.TryGetValue(company.Id, out var sends)
            ? sends.Select(t => (t.Resource, t.Market, t.Sent * t.Market.Efficiency, RemotePrice(company, t.Market, t.Resource))).ToList()
            : [];

    // ---------------------------------------------------------------- Crew trunks

    /// <summary>A trunk's route: from the station's Yard along the corridors to the hub (or the Port)</summary>
    private List<TilePos> TrunkRoute(StationRef station, DistrictCoord? hub, bool port)
    {
        var target = port ? PortAnchor : HubAnchor(hub!.Value);
        var start = YardAnchor(station);
        // Along the corridor lines (they run on through the wilderness): first along the line the station's Yard is on
        var turn = OnYLine(start.Y) ? new TilePos(target.X, start.Y) : new TilePos(start.X, target.Y);
        var route = new List<TilePos> { start, turn, target };
        return route.Where((t, i) => i == 0 || t != route[i - 1]).ToList();
    }

    /// <summary>What a segment costs its payer: two hours of T(L) at its level, and the trunk's steel and concrete</summary>
    public double TrunkSegmentGold(Company company) => Math.Round(TrunkSegmentHours * 60 * TypicalIncome(company.Level));

    public static IReadOnlyDictionary<string, double> TrunkSegmentCost => TrunkSegmentMaterials;

    /// <summary>Starts a crew trunk from the company's station to a hub (or the Port), paying its first segment</summary>
    public (CommandResult Result, Trunk? Trunk) StartTrunk(long companyId, DistrictCoord? hub, bool port)
    {
        if (Company(companyId) is not { } company || CrewOf(company) is not { } crew)
        {
            return (CommandResult.Fail("Trunks are for crews: found one first."), null);
        }
        if (EstateOf(company) is not { } estate)
        {
            return (CommandResult.Fail("Your estate is on its way."), null);
        }
        if (Content.Resources.TryGetValue("steel", out var steel) && steel.Era > Era)
        {
            return (CommandResult.Fail("Trunks are steel: they come with Era 2."), null);
        }
        if (!port && (hub is not { } target || !_openDistricts.Contains(target) || target == company.District))
        {
            return (CommandResult.Fail("Choose another open district's hub, or the Port."), null);
        }
        var station = ProjectKey(BigStation(estate.Station)[0]);
        if (_trunks.Values.Any(t => t.Crew == crew.Id && t.Station == station && t.Hub == hub && t.Port == port))
        {
            return (CommandResult.Fail("Your crew builds that trunk already."), null);
        }
        var route = TrunkRoute(estate.Station, hub, port);
        var length = RouteLength(route);
        var trunk = new Trunk
        {
            Id = NextTrunkId++,
            Crew = crew.Id,
            Station = station,
            Hub = port ? null : hub,
            Port = port,
            Route = route,
            Length = length,
            SegmentsNeeded = Math.Max(1, (int)Math.Ceiling(length / (double)TrunkSegmentTiles)),
            CreatedAt = LastTick,
        };
        var paid = PaySegment(company, trunk);
        if (!paid.Success)
        {
            NextTrunkId--;
            return (paid, null);
        }
        _trunks[trunk.Id] = trunk;
        Changes.Links.Add(company.Id);
        foreach (var member in CrewMembers(crew).Where(t => t.Id != company.Id))
        {
            Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Crew,
                $"{company.Name} starts a trunk to {(port ? "the Port" : $"{WorldNames.District(hub!.Value)}'s hub")}: {trunk.SegmentsNeeded} segments, each paid by whoever likes"));
        }
        return (CommandResult.Ok, trunk);
    }

    /// <summary>Pays the next segment of a crew trunk</summary>
    public CommandResult BuildTrunkSegment(long companyId, long trunkId)
    {
        if (Company(companyId) is not { } company || _trunks.GetValueOrDefault(trunkId) is not { } trunk || trunk.Crew != company.CrewId)
        {
            return CommandResult.Fail("That is not your crew's trunk.");
        }
        if (trunk.Built)
        {
            return CommandResult.Fail("The trunk is built.");
        }
        return PaySegment(company, trunk);
    }

    private CommandResult PaySegment(Company company, Trunk trunk)
    {
        var gold = TrunkSegmentGold(company);
        var payment = Pay(company, gold, TrunkSegmentMaterials);
        if (!payment.Success)
        {
            return payment;
        }
        company.Report.GoldSpent += gold;
        trunk.Segments.Add(company.Id);
        trunk.Touch();
        Changes.Links.Add(company.Id);
        if (trunk.Built && Crew(trunk.Crew) is { } crew)
        {
            WakeCoupling();
            foreach (var member in CrewMembers(crew))
            {
                Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Crew,
                    $"The trunk to {(trunk.Port ? "the Port" : $"{WorldNames.District(trunk.Hub!.Value)}'s hub")} is built: your spare sells there too"));
            }
        }
        return CommandResult.Ok;
    }

    public IEnumerable<Trunk> TrunksOf(Crew crew) => _trunks.Values.Where(t => t.Crew == crew.Id).OrderBy(t => t.Id);
}
