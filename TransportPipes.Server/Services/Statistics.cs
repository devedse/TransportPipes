using TransportPipes.Database;
using TransportPipes.Game.State;
using TransportPipes.Game.World;
using TransportPipes.Server.Dtos;

namespace TransportPipes.Server.Services;

/// <summary>
/// Statistics for the client (docs/design-notes/statistics.md): a range of saved samples with the live point at its end,
/// the key moments, the town, and who to compare with. Another company shows only what everyone may see.
/// </summary>
public static class Statistics
{
    /// <summary>The series anyone may see of another company (its bank, spending, production and trades stay its own)</summary>
    public static readonly HashSet<string> Public =
        ["income", "worth", "earned", "level", "buildings", "levels", "land", "pipes", "perTile", "tiers", "tree", "era", "partners", "likes", "given"];

    /// <summary>At most this many players to compare with</summary>
    private const int MaxPeers = 15;

    /// <summary>What the world knows of a company now, taken while it is locked</summary>
    public sealed record Live(
        long Company,
        string Name,
        string Color,
        DateTime Founded,
        bool Own,
        DateTime Now,
        DistrictCoord District,
        Dictionary<string, double> Sample,
        Dictionary<string, double> TownSample,
        IReadOnlyList<StatsPeerDto> Peers);

    /// <summary>A company's live view for a viewer (null: the viewer has no company, or the company is unknown)</summary>
    public static Live? Of(GameWorld world, long userId, long? companyId)
    {
        if (world.CompanyOfUser(userId) is not { } viewer || (companyId is { } id ? world.Company(id) : viewer) is not { } company)
        {
            return null;
        }
        var crew = world.CrewOf(viewer)?.Id;
        var peers = world.Companies.Where(t => t.Id != viewer.Id && t.Id != company.Id && (t.District == viewer.District || (crew != null && t.CrewId == crew)))
            .OrderByDescending(t => crew != null && t.CrewId == crew).ThenByDescending(t => t.LifetimeEarnings).Take(MaxPeers)
            .Select(t => new StatsPeerDto(t.Id, t.Name, t.Color, crew != null && t.CrewId == crew)).ToList();
        var gold = world.GoldPerMinute();
        return new Live(company.Id, company.Name, company.Color, company.CreatedAt, company.Id == viewer.Id, world.Now, company.District,
            world.CompanySample(company, gold), world.TownSampleNow(company.District, gold), peers);
    }

    /// <summary>Where a range ("day", "week", "month", "all") begins and its step: at most about 200 points</summary>
    public static (DateTime From, TimeSpan Step) Range(string range, DateTime now, DateTime founded)
    {
        var (from, step) = range switch
        {
            "day" => (now.AddDays(-1), TimeSpan.FromMinutes(15)),
            "week" => (now.AddDays(-7), TimeSpan.FromHours(1)),
            "month" => (now.AddDays(-30), TimeSpan.FromHours(4)),
            _ => (founded, (now - founded).TotalDays switch
            {
                <= 2.5 => TimeSpan.FromMinutes(15),
                <= 10 => TimeSpan.FromHours(1),
                <= 40 => TimeSpan.FromHours(4),
                _ => TimeSpan.FromDays(1),
            }),
        };
        return (from < founded ? founded.AddMinutes(-1) : from, step);
    }

    public static string RangeName(string range) => range is "day" or "week" or "month" ? range : "all";

    public static StatisticsDto Dto(Live live, string range, DateTime from, List<HistoryStore.Sample> samples, List<Moment> moments,
        List<HistoryStore.Sample> town, string townName)
    {
        bool Keep(string key) => live.Own || Public.Contains(key);
        var (keys, at, rows) = Columns(samples, new HistoryStore.Sample(live.Now, live.Sample), Keep);
        var (townKeys, townAt, townRows) = Columns(town, new HistoryStore.Sample(live.Now, live.TownSample), _ => true);
        var shown = moments.Select(t => new MomentDto(t.At, t.Kind, t.Text, t.Big, t.Ref)).ToList();
        if (live.Founded >= from)
        {
            // The first moment of every company (not saved: its founding time is)
            shown.Insert(0, new MomentDto(live.Founded, "founded", $"{live.Name} founded", true, null));
        }
        return new StatisticsDto(live.Company, live.Name, live.Color, live.Founded, live.Own, range, from, live.Now, keys, at, rows, shown,
            new StatsTownDto(townName, townKeys, townAt, townRows), live.Own ? live.Peers : []);
    }

    /// <summary>One series of a company (to compare with): only one everyone may see, unless it is your own</summary>
    public static StatsSeriesDto Series(Live live, string key, List<HistoryStore.Sample> samples)
    {
        var all = samples.Append(new HistoryStore.Sample(live.Now, live.Sample)).ToList();
        return new StatsSeriesDto(live.Company, live.Name, live.Color, live.Founded, key, all.Select(t => t.At).ToList(),
            all.Select(t => t.Values.TryGetValue(key, out var v) ? v : (double?)null).ToList());
    }

    /// <summary>Samples as columns: the keys any of them has (that may be shown), a row per sample, the live one last</summary>
    private static (List<string> Keys, List<DateTime> At, List<IReadOnlyList<double?>> Rows) Columns(List<HistoryStore.Sample> samples, HistoryStore.Sample live,
        Func<string, bool> keep)
    {
        var all = samples.Where(t => t.At < live.At).Append(live).ToList();
        var keys = all.SelectMany(t => t.Values.Keys).Distinct().Where(keep).Order(StringComparer.Ordinal).ToList();
        var rows = all.Select(t => (IReadOnlyList<double?>)keys.Select(k => t.Values.TryGetValue(k, out var v) ? v : (double?)null).ToList()).ToList();
        return (keys, all.Select(t => t.At).ToList(), rows);
    }
}
