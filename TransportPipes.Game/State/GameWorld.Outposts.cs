using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>An 8×8 claim on a wilderness deposit (GDD §4.6), leased by the week</summary>
public sealed class Outpost
{
    public long Id { get; init; }
    public long Owner { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public DateTime ClaimedAt { get; init; }
    public DateTime PaidUntil { get; set; }

    /// <summary>Gold per week</summary>
    public double Lease { get; init; }

    public bool Contains(TilePos tile) => tile.X >= X && tile.X < X + GameWorld.OutpostSize && tile.Y >= Y && tile.Y < Y + GameWorld.OutpostSize;

    public IEnumerable<TilePos> Tiles()
    {
        for (var dy = 0; dy < GameWorld.OutpostSize; dy++)
        {
            for (var dx = 0; dx < GameWorld.OutpostSize; dx++)
            {
                yield return new TilePos(X + dx, Y + dy);
            }
        }
    }
}

/// <summary>A rich deposit a Discovery event put in the wilderness</summary>
public sealed record Discovery(int X, int Y, string Resource, DateTime FoundAt);

/// <summary>
/// Outposts and the wilderness (GDD §4.6): claims on rare deposits between the districts, from Company Level 15, at most
/// three per company, leased by the week. Goods come home by a long pipe through the wilderness or by Cargo Depots,
/// which connect all the networks of their owner. Discovery events add rich deposits.
/// </summary>
public sealed partial class GameWorld
{
    public const int OutpostSize = 8;
    public const int MaxOutposts = 3;
    public const int OutpostLevel = 15;
    private const int OutpostMinDeposits = 4;
    private const int DiscoveryRadius = 2;
    public static readonly TimeSpan LeasePeriod = TimeSpan.FromDays(7);

    private readonly Dictionary<long, Outpost> _outposts = new();
    private readonly List<Discovery> _discoveries = new();

    public long NextOutpostId { get; set; } = 1;
    public IEnumerable<Outpost> Outposts => _outposts.Values;
    public IReadOnlyList<Discovery> Discoveries => _discoveries;

    public void LoadOutposts(IEnumerable<Outpost> outposts, IEnumerable<Discovery> discoveries)
    {
        foreach (var outpost in outposts)
        {
            _outposts[outpost.Id] = outpost;
            NextOutpostId = Math.Max(NextOutpostId, outpost.Id + 1);
        }
        _discoveries.AddRange(discoveries);
    }

    public Outpost? OutpostAt(TilePos tile) => _outposts.Values.FirstOrDefault(t => t.Contains(tile));

    public IEnumerable<Outpost> OutpostsOf(long companyId) => _outposts.Values.Where(t => t.Owner == companyId);

    /// <summary>The weekly lease of an outpost: grows with the Era</summary>
    public double OutpostLease() => 3000 * Era;

    /// <summary>A discovered deposit on a tile (richness 5)</summary>
    private Discovery? DiscoveryAt(TilePos pos)
    {
        foreach (var discovery in _discoveries)
        {
            if (Math.Abs(pos.X - discovery.X) + Math.Abs(pos.Y - discovery.Y) <= DiscoveryRadius)
            {
                return discovery;
            }
        }
        return null;
    }

    /// <summary>Why an 8×8 outpost can't be claimed here (null: it can)</summary>
    public string? OutpostBlocker(Company company, TilePos origin)
    {
        if (company.Level < OutpostLevel)
        {
            return $"Outposts come with Company Level {OutpostLevel}.";
        }
        if (OutpostsOf(company.Id).Count() >= MaxOutposts)
        {
            return $"You have {MaxOutposts} outposts already.";
        }
        var probe = new Outpost { X = origin.X, Y = origin.Y };
        var deposits = 0;
        foreach (var tile in probe.Tiles())
        {
            if (WorldLayout.ZoneAt(tile.X, tile.Y).Kind != ZoneKind.Wilderness)
            {
                return "Outposts are claimed in the wilderness between the districts.";
            }
            if (OutpostAt(tile) != null)
            {
                return "This land is claimed already.";
            }
            if (IsOccupied(tile))
            {
                return "Something is standing there.";
            }
            var info = Tile(tile);
            if (info.Deposit != null && info.Richness >= 3)
            {
                deposits++;
            }
        }
        if (deposits < OutpostMinDeposits)
        {
            return "An outpost has to cover a rich deposit (at least 4 tiles of 3 stars or more).";
        }
        return null;
    }

    /// <summary>
    /// The best 8×8 claim that covers a tile: valid, with the most rich deposit tiles. Without a valid one, the claim
    /// centred on the tile (to explain why it can't be claimed).
    /// </summary>
    public (TilePos Origin, string? Blocker) BestOutpostAt(Company company, TilePos tile)
    {
        var centred = tile.Offset(-OutpostSize / 2 + 1, -OutpostSize / 2 + 1);
        var bestRich = -1;
        TilePos? best = null;
        for (var dy = 0; dy < OutpostSize; dy++)
        {
            for (var dx = 0; dx < OutpostSize; dx++)
            {
                var origin = tile.Offset(-dx, -dy);
                if (OutpostBlocker(company, origin) != null)
                {
                    continue;
                }
                var rich = new Outpost { X = origin.X, Y = origin.Y }.Tiles().Count(t => Tile(t) is { Deposit: not null, Richness: >= 3 });
                if (rich > bestRich)
                {
                    bestRich = rich;
                    best = origin;
                }
            }
        }
        return best is { } found ? (found, null) : (centred, OutpostBlocker(company, centred));
    }

    public CommandResult ClaimOutpost(long companyId, TilePos origin)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (OutpostBlocker(company, origin) is { } blocker)
        {
            return CommandResult.Fail(blocker);
        }
        var lease = OutpostLease();
        var payment = Pay(company, lease, new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }
        var outpost = new Outpost { Id = NextOutpostId++, Owner = companyId, X = origin.X, Y = origin.Y, ClaimedAt = Now, PaidUntil = Now + LeasePeriod, Lease = lease };
        _outposts[outpost.Id] = outpost;
        OutpostChanged(outpost);
        return CommandResult.Ok;
    }

    public CommandResult ReleaseOutpost(long companyId, long outpostId)
    {
        if (!_outposts.TryGetValue(outpostId, out var outpost) || outpost.Owner != companyId)
        {
            return CommandResult.Fail("That is not your outpost.");
        }
        Release(outpost);
        return CommandResult.Ok;
    }

    /// <summary>Gives the land back: the buildings are demolished (half their cost back), the pipes stay</summary>
    private void Release(Outpost outpost)
    {
        var company = Company(outpost.Owner);
        foreach (var building in _buildings.Values.Where(t => t.Owner == outpost.Owner && outpost.Contains(t.Position)).ToList())
        {
            RemoveBuilding(building);
            if (company != null)
            {
                company.Gold += Math.Round(Content.Building(building.Type).Cost.Gold * 0.5);
            }
        }
        _outposts.Remove(outpost.Id);
        OutpostChanged(outpost);
    }

    private void OutpostChanged(Outpost outpost)
    {
        Changes.Outposts = true;
        foreach (var tile in outpost.Tiles())
        {
            Changes.Tiles.Add(tile);
        }
        if (Company(outpost.Owner) is { } company)
        {
            company.Touch();
            Changes.Companies.Add(company.Id);
        }
    }

    /// <summary>Leases due in a span: paid automatically, or the outpost is given back</summary>
    private IEnumerable<(DateTime At, Action Run)> LeaseMoments(DateTime from, DateTime to)
    {
        foreach (var outpost in _outposts.Values.Where(t => t.PaidUntil <= to).ToList())
        {
            yield return (outpost.PaidUntil > from ? outpost.PaidUntil : from, () => RenewLease(outpost));
        }
    }

    private void RenewLease(Outpost outpost)
    {
        if (!_outposts.ContainsKey(outpost.Id) || outpost.PaidUntil > LastTick)
        {
            return;
        }
        if (Company(outpost.Owner) is { } company && company.Gold >= outpost.Lease)
        {
            company.Gold -= outpost.Lease;
            company.Report.GoldSpent += outpost.Lease;
            outpost.PaidUntil += LeasePeriod;
            company.Touch();
            Changes.Companies.Add(company.Id);
            Changes.Outposts = true;
            return;
        }
        if (Company(outpost.Owner) is { } owner)
        {
            Changes.Events.Add(new WorldEvent(owner.Id, WorldEventKind.Calendar, "You couldn't pay the lease of an outpost: the land went back to the wilderness"));
        }
        Release(outpost);
    }

    // ---------------------------------------------------------------- Discoveries

    /// <summary>Every two weeks or so a rich deposit appears in the wilderness next to an open district</summary>
    private void PlanDiscovery(DateTime day, SeededRandom random)
    {
        if (_openDistricts.Count == 0 || random.NextDouble() >= 1 / 14.0)
        {
            return;
        }
        var districts = _openDistricts.OrderBy(t => t.X).ThenBy(t => t.Y).ToList();
        var district = districts[random.Next(districts.Count)];
        if (WildSpotNear(district, random) is { } pos)
        {
            var resources = Era >= 3 ? new[] { "oil", "rareEarth", "copperOre" } : ["copperOre", "coal", "ironOre"];
            var start = DateTime.SpecifyKind(day.AddHours(8 + random.Next(10)), DateTimeKind.Utc);
            if (start < LastTick + TimeSpan.FromHours(1))
            {
                return;
            }
            AddEvent(new GameEvent
            {
                Id = NextEventId++,
                Kind = EventKind.Discovery,
                District = null,
                StartsAt = start,
                EndsAt = start.AddHours(24),
                Resource = resources[random.Next(resources.Length)],
                X = pos.X,
                Y = pos.Y,
            });
        }
    }

    /// <summary>A free, buildable spot in the wilderness band around a district (null when none was found)</summary>
    private TilePos? WildSpotNear(DistrictCoord district, SeededRandom random)
    {
        var x0 = WorldLayout.DistrictOriginX(district.X);
        var y0 = WorldLayout.DistrictOriginY(district.Y);
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var side = random.Next(4);
            var along = random.Next(WorldLayout.DistrictSize);
            var depth = 4 + random.Next(WorldLayout.WildernessWidth - 8);
            var pos = side switch
            {
                0 => new TilePos(x0 + along, y0 - depth),
                1 => new TilePos(x0 + along, y0 + WorldLayout.DistrictSize + depth - 1),
                2 => new TilePos(x0 - depth, y0 + along),
                _ => new TilePos(x0 + WorldLayout.DistrictSize + depth - 1, y0 + along),
            };
            if (WorldLayout.ZoneAt(pos.X, pos.Y).Kind == ZoneKind.Wilderness && Generator.Tile(pos).IsBuildableTerrain && OutpostAt(pos) == null && DiscoveryAt(pos) == null)
            {
                return pos;
            }
        }
        return null;
    }

    /// <summary>A Discovery right now (for testing)</summary>
    public GameEvent StartDiscovery(TilePos pos, string resource)
    {
        var discovery = new GameEvent { Id = NextEventId++, Kind = EventKind.Discovery, StartsAt = LastTick, EndsAt = LastTick.AddDays(1), Resource = resource, X = pos.X, Y = pos.Y, Announced = true };
        _events.Add(discovery);
        StartEvent(discovery);
        return discovery;
    }

    /// <summary>The deposit appears when the Discovery starts</summary>
    private void Discover(GameEvent discovery)
    {
        if (discovery.X is not { } x || discovery.Y is not { } y || discovery.Resource is not { } resource)
        {
            return;
        }
        _discoveries.Add(new Discovery(x, y, resource, LastTick));
        Changes.Outposts = true;
        for (var dy = -DiscoveryRadius; dy <= DiscoveryRadius; dy++)
        {
            for (var dx = -DiscoveryRadius; dx <= DiscoveryRadius; dx++)
            {
                Changes.Tiles.Add(new TilePos(x + dx, y + dy));
            }
        }
    }
}
