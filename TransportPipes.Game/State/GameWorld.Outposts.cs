using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>An 8×8 claim on a wilderness deposit (1.4 §20), bought once and kept for good</summary>
public sealed class Outpost
{
    public long Id { get; init; }
    public long Owner { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public DateTime ClaimedAt { get; init; }

    /// <summary>The gold it was bought for</summary>
    public double Price { get; init; }

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
public sealed record Discovery(int X, int Y, string Resource, DateTime FoundAt)
{
    /// <summary>Per station ("x,y,index"): what its players brought out of the deposit in its first 72 hours (its marker)</summary>
    public Dictionary<string, double> Marks { get; init; } = new();

    /// <summary>The same per company (the three who bring most are named on it)</summary>
    public Dictionary<long, double> ByCompany { get; init; } = new();
}

/// <summary>
/// Outposts and the wilderness (1.4 §20): from Era 4 and its research, claims of 8×8 on rich deposits in the wilderness
/// you have mapped, at most three per company, bought once and yours for good (no rent: nothing runs out while you're
/// away, rule 7). Goods come home by Cargo Depot or Quantum Link. Discovery events add rich deposits.
/// </summary>
public sealed partial class GameWorld
{
    public const int OutpostSize = 8;
    public const int MaxOutposts = 3;
    private const int OutpostMinDeposits = 4;
    private const int DiscoveryRadius = 2;

    /// <summary>The first outpost's price; every next one twice the last (provisional until the model plays Era 4)</summary>
    public const double OutpostFirstPrice = 20_000_000_000;

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

    /// <summary>What a company's next outpost costs (the Workshop Monument counts)</summary>
    public double OutpostPrice(Company company) =>
        Math.Round(OutpostFirstPrice * Math.Pow(2, OutpostsOf(company.Id).Count()) * CostFactor(company));

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
        if (!HasResearch(company, "outposts"))
        {
            return "Research Outposts (Era 4) first.";
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
                return "Outposts are claimed in the wilderness between the towns.";
            }
            if (!IsExplored(company, tile))
            {
                return "Map this part of the wilderness first: an expedition does.";
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
        var price = OutpostPrice(company);
        var payment = Pay(company, price, new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }
        AddStat(company, "outpostGold", price);
        var outpost = new Outpost { Id = NextOutpostId++, Owner = companyId, X = origin.X, Y = origin.Y, ClaimedAt = Now, Price = price };
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

    // ---------------------------------------------------------------- Discoveries

    /// <summary>
    /// Every four days a rich deposit appears in the wilderness next to an open district (v1.2 §13), announced a day
    /// ahead and lasting a week; every station whose players bring something out of it in its first 72 hours gets its
    /// marker, so being online when it is announced decides nothing
    /// </summary>
    private void PlanDiscovery(DateTime day, SeededRandom random)
    {
        var dayNumber = (long)(day - DateTime.UnixEpoch).TotalDays;
        if (_openDistricts.Count == 0 || dayNumber % DiscoveryEveryDays != 0)
        {
            return;
        }
        var districts = _openDistricts.OrderBy(t => t.X).ThenBy(t => t.Y).ToList();
        var district = districts[random.Next(districts.Count)];
        if (WildSpotNear(district, random) is { } pos)
        {
            var resources = TownEra(district) >= 3 ? new[] { "oil", "rareEarth", "copperOre" } : ["copperOre", "coal", "ironOre"];
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
                EndsAt = start.AddDays(7),
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
            var along = random.Next(side < 2 ? WorldLayout.DistrictWidth : WorldLayout.DistrictHeight);
            var depth = 4 + random.Next(WorldLayout.WildernessWidth - 8);
            var pos = side switch
            {
                0 => new TilePos(x0 + along, y0 - depth),
                1 => new TilePos(x0 + along, y0 + WorldLayout.DistrictHeight + depth - 1),
                2 => new TilePos(x0 - depth, y0 + along),
                _ => new TilePos(x0 + WorldLayout.DistrictWidth + depth - 1, y0 + along),
            };
            if (WorldLayout.ZoneAt(pos.X, pos.Y).Kind == ZoneKind.Wilderness && Generator.Tile(pos).IsBuildableTerrain && OutpostAt(pos) == null && DiscoveryAt(pos) == null)
            {
                return pos;
            }
        }
        return null;
    }

    public const int DiscoveryEveryDays = 4;
    public static readonly TimeSpan DiscoveryMarking = TimeSpan.FromHours(72);

    /// <summary>What a building on a Discovery made counts for its owner's station (in the deposit's first 72 hours)</summary>
    private void CountDiscovery(Building building, double units)
    {
        if (_discoveries.Count == 0 || units <= 0)
        {
            return;
        }
        foreach (var discovery in _discoveries)
        {
            if (LastTick - discovery.FoundAt > DiscoveryMarking
                || Math.Abs(building.Position.X - discovery.X) > DiscoveryRadius + 3 || Math.Abs(building.Position.Y - discovery.Y) > DiscoveryRadius + 3)
            {
                continue;
            }
            if (Company(building.Owner) is not { } owner || EstateOf(owner) is not { } estate)
            {
                continue;
            }
            var station = $"{estate.Station.District.X},{estate.Station.District.Y},{estate.Station.Index}";
            discovery.Marks[station] = discovery.Marks.GetValueOrDefault(station) + units;
            discovery.ByCompany[owner.Id] = discovery.ByCompany.GetValueOrDefault(owner.Id) + units;
            Changes.Outposts = true;
        }
    }

    /// <summary>A Discovery's markers: the stations that brought something out of it, and the three companies that brought most</summary>
    public (List<StationRef> Stations, List<Company> Top) DiscoveryMarks(Discovery discovery) =>
        (discovery.Marks.Where(t => t.Value > 0).OrderByDescending(t => t.Value).Select(t => t.Key.Split(',') is [var x, var y, var i] ? new StationRef(new DistrictCoord(int.Parse(x), int.Parse(y)), int.Parse(i)) : default).ToList(),
         discovery.ByCompany.OrderByDescending(t => t.Value).Select(t => Company(t.Key)).OfType<Company>().Take(3).ToList());

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
