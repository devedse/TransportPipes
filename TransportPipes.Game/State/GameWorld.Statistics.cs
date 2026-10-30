using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>Whose numbers a sample holds</summary>
public enum StatSubject
{
    Company,
    Town,
}

/// <summary>
/// A company's or a town's numbers at one moment (docs/design-notes/statistics.md): values as they were, and counters
/// that only grow (drawn as a rate per minute)
/// </summary>
public sealed record StatSample(StatSubject Kind, long Subject, DateTime At, Dictionary<string, double> Values);

/// <summary>A key moment of a company (docs/design-notes/statistics.md): big ones are flags on every graph, small ones ticks</summary>
/// <param name="Ref">A good or building type for its icon</param>
public sealed record Moment(long Company, DateTime At, string Kind, string Text, bool Big, string? Ref = null);

/// <summary>Statistics (docs/design-notes/statistics.md): a sample of every company and town every 15 minutes, and key moments</summary>
public sealed partial class GameWorld
{
    /// <summary>A sample of every company and town this often, with the market update</summary>
    public static readonly TimeSpan SampleEvery = MarketInterval;

    /// <summary>At most this many samples (and moments) wait for the database; a world without one (the bots') keeps the newest</summary>
    private const int MaxPending = 20_000;

    /// <summary>Samples and moments the database hasn't saved yet (the store takes them with every save)</summary>
    public List<StatSample> PendingSamples { get; } = new();
    public List<Moment> PendingMoments { get; } = new();

    /// <summary>When the last samples were taken (calendar events that began since are moments of the next ones)</summary>
    private DateTime? _lastSample;

    /// <summary>A town's subject in samples: its district packed in one number</summary>
    public static long TownKey(DistrictCoord town) => ((long)town.X << 32) | (uint)town.Y;

    public static DistrictCoord TownOfKey(long key) => new((int)(key >> 32), (int)(key & 0xffffffff));

    /// <summary>The samples of every company and of every town with players, as things stand now (at a market update)</summary>
    private void TakeSamples()
    {
        var at = LastTick;
        var gold = GoldPerMinute();
        var pairs = PartnerPairs();
        NewPartners(pairs);
        foreach (var company in _companies.Values.Where(t => t.TradedWithUnknown))
        {
            company.TradedWithUnknown = false;
            company.Touch();
        }
        CalendarMoments(_lastSample ?? at - SampleEvery, at);
        var pipes = _conductors.Values.Where(t => t.Kind == ConductorKind.Pipe).GroupBy(t => t.Owner).ToDictionary(t => t.Key, t => t.Count());
        var buildings = _buildings.Values.ToLookup(t => t.Owner);
        foreach (var company in _companies.Values)
        {
            // The minutes its player was around, by quarter hours: a graph shades the stretches without any
            if (company.LastSeenAt > at - SampleEvery)
            {
                AddStat(company, "played", SampleEvery.TotalMinutes);
                company.Touch();
            }
            PendingSamples.Add(new StatSample(StatSubject.Company, company.Id, at, CompanySample(company, gold, pairs, buildings[company.Id], pipes.GetValueOrDefault(company.Id))));
        }
        foreach (var residents in _companies.Values.GroupBy(t => t.District))
        {
            // A town nobody looked at yet has no record: it is in Era 1 with its Wonder not begun
            var town = _towns.GetValueOrDefault(residents.Key) ?? new Town { X = residents.Key.X, Y = residents.Key.Y };
            PendingSamples.Add(new StatSample(StatSubject.Town, TownKey(residents.Key), at, TownSample(town, residents.ToList(), gold)));
        }
        Trim(PendingSamples);
        Trim(PendingMoments);
        _lastSample = at;
    }

    private static void Trim<T>(List<T> pending)
    {
        if (pending.Count > MaxPending)
        {
            pending.RemoveRange(0, pending.Count - MaxPending);
        }
    }

    /// <summary>
    /// A company's numbers now (the samples, and the live point at the end of its graphs): gold, what it earned and spent
    /// by kind, what it made and sold per good, its plot, research and trade, and the minutes its player was around
    /// </summary>
    public Dictionary<string, double> CompanySample(Company company, IReadOnlyDictionary<long, double>? goldPerMinute = null,
        HashSet<(long, long)>? pairs = null, IEnumerable<Building>? buildings = null, int? pipes = null)
    {
        goldPerMinute ??= GoldPerMinute();
        pairs ??= PartnerPairs();
        var own = (buildings ?? BuildingsOf(company.Id)).Where(t => !t.UnderConstruction).ToList();
        var stat = company.Stats;
        var values = new Dictionary<string, double>
        {
            ["income"] = Math.Round(goldPerMinute.GetValueOrDefault(company.Id), 1),
            ["worth"] = Math.Round(NetWorth(company)),
            ["gold"] = Math.Floor(company.Gold),
            ["level"] = company.Level,
            ["earned"] = Math.Round(company.LifetimeEarnings),
            ["spent"] = Math.Round(stat.GetValueOrDefault("goldSpent")),
            ["spent:research"] = Math.Round(stat.GetValueOrDefault("researchGold")),
            ["spent:land"] = Math.Round(stat.GetValueOrDefault("landGold")),
            ["spent:gifts"] = Math.Round(stat.GetValueOrDefault("wonderGold") + stat.GetValueOrDefault("monumentGold")),
            ["buildings"] = own.Count,
            ["levels"] = own.Where(t => Content.Building(t.Type).Kind != BuildingKind.Hq).Sum(t => t.Level),
            ["land"] = LandTiles(company),
            ["pipes"] = pipes ?? _conductors.Values.Count(t => t.Kind == ConductorKind.Pipe && t.Owner == company.Id),
            ["perTile"] = TileEfficiency(company, goldPerMinute),
            ["tiers"] = company.TrackTiers.Values.Sum(),
            ["tree"] = company.Researched.Count,
            ["era"] = company.Era,
            ["partners"] = pairs.Count(t => t.Item1 == company.Id),
            ["likes"] = company.Likes,
            ["given"] = Math.Round(stat.GetValueOrDefault("wonder")),
            ["soldToPlayers"] = Math.Round(stat.GetValueOrDefault("soldToPlayers")),
            ["boughtFromPlayers"] = Math.Round(stat.GetValueOrDefault("boughtFromPlayers")),
            ["played"] = stat.GetValueOrDefault("played"),
        };
        // Where the gold came from: the market, other players, daily orders; achievements and the rest are rewards
        foreach (var (source, items) in company.EarnedLog)
        {
            var key = source is "market" or "players" or "orders" ? $"earned:{source}" : "earned:rewards";
            values[key] = Math.Round(values.GetValueOrDefault(key) + items.Values.Sum());
            if (source is "market" or "players")
            {
                foreach (var (good, gold) in items)
                {
                    values[$"sold:{good}"] = Math.Round(values.GetValueOrDefault($"sold:{good}") + gold);
                }
            }
        }
        foreach (var (key, amount) in stat)
        {
            if (key.StartsWith("produced:", StringComparison.Ordinal))
            {
                values[$"made:{key["produced:".Length..]}"] = Math.Round(amount, 1);
            }
        }
        return values;
    }

    /// <summary>The tiles a company owns: its plot's parcels, its outskirts, its Deep parcels and its outposts</summary>
    public int LandTiles(Company company)
    {
        var parcel = WorldLayout.ParcelSize * WorldLayout.ParcelSize;
        var tiles = 0;
        foreach (var estate in EstatesOf(company))
        {
            tiles += estate.UnlockedCount * parcel;
            for (var i = 0; i < WorldLayout.OutskirtsParcels(estate.Slot).Count; i++)
            {
                tiles += estate.HasOutskirts(i) ? OutskirtsTiles(estate, i).Count() : 0;
            }
        }
        tiles += System.Numerics.BitOperations.PopCount((uint)company.DeepParcels) * parcel;
        tiles += _outposts.Values.Count(t => t.Owner == company.Id) * OutpostSize * OutpostSize;
        return tiles;
    }

    /// <summary>A town's numbers now (the live point at the end of its graphs)</summary>
    public Dictionary<string, double> TownSampleNow(DistrictCoord coord, IReadOnlyDictionary<long, double>? goldPerMinute = null) =>
        TownSample(_towns.GetValueOrDefault(coord) ?? new Town { X = coord.X, Y = coord.Y }, CompaniesInTown(coord).ToList(), goldPerMinute ?? GoldPerMinute());

    /// <summary>A town's numbers: its players' income and value together, how many, its Era and how far its Wonder is</summary>
    private Dictionary<string, double> TownSample(Town town, List<Company> residents, IReadOnlyDictionary<long, double> goldPerMinute) => new()
    {
        ["income"] = Math.Round(residents.Sum(t => goldPerMinute.GetValueOrDefault(t.Id)), 1),
        ["worth"] = Math.Round(residents.Sum(NetWorth)),
        ["players"] = residents.Count,
        ["era"] = town.Era,
        ["wonder"] = town.Wonder is { } wonder ? Math.Round(wonder.Stage + StageShare(wonder), 3) : 0,
        ["missions"] = town.Missions,
    };

    /// <summary>How much of a Wonder's current stage is in, by the value of its goods (0 to 1)</summary>
    public double StageShare(TownWonder wonder)
    {
        if (wonder.FullAt != null)
        {
            return 1;
        }
        var target = wonder.Required.Sum(t => t.Value * BasePrice(t.Key));
        return target <= 0 ? 0 : wonder.Required.Sum(t => Math.Min(t.Value, wonder.Delivered.GetValueOrDefault(t.Key)) * BasePrice(t.Key)) / target;
    }

    // ---------------------------------------------------------------- Key moments

    /// <summary>A key moment of a company's story, now (never before it was founded: its first come as it settles)</summary>
    private void AddMoment(Company company, string kind, string text, bool big, string? reference = null) =>
        PendingMoments.Add(new Moment(company.Id, LastTick < company.CreatedAt ? company.CreatedAt : LastTick, kind, text, big, reference));

    /// <summary>The first unit of a good a company ever made is a moment ("Your first Steel"); counts what it made</summary>
    private void CountProduced(Company producer, string resource, double units)
    {
        if (units > 0 && !producer.Stats.ContainsKey($"produced:{resource}"))
        {
            AddMoment(producer, "first", $"First {ResourceName(resource)}", true, resource);
        }
        AddStat(producer, $"produced:{resource}", units);
    }

    /// <summary>Companies that trade with a company they never traded with before: a moment each (the very first a big one)</summary>
    private void NewPartners(HashSet<(long, long)> pairs)
    {
        foreach (var (a, b) in pairs.OrderBy(t => t.Item1).ThenBy(t => t.Item2))
        {
            if (Company(a) is not { } company || Company(b) is not { } partner || company.TradedWith.Contains(b))
            {
                continue;
            }
            var first = company.TradedWith.Count == 0;
            company.TradedWith.Add(b);
            company.Touch();
            if (company.TradedWithUnknown)
            {
                continue;
            }
            AddMoment(company, "partner", first ? $"First trade with another player: {partner.Name}" : $"First trade with {partner.Name}", first);
        }
    }

    /// <summary>Calendar events that began in a town since the last samples: they explain a bump in its players' income</summary>
    private void CalendarMoments(DateTime from, DateTime to)
    {
        // From the last samples up to these: one that begins right at a sample is a moment of the next ones
        foreach (var gameEvent in _events.Where(t => t.StartsAt >= from && t.StartsAt < to && t.Kind != EventKind.Discovery))
        {
            foreach (var company in CompaniesIn(gameEvent))
            {
                PendingMoments.Add(new Moment(company.Id, gameEvent.StartsAt, "event", $"{EventName(gameEvent)}: {EventEffect(gameEvent)}", false, gameEvent.Resource));
            }
        }
    }
}
