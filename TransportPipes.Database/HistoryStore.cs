using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TransportPipes.Game.State;

namespace TransportPipes.Database;

/// <summary>
/// The statistics' history (docs/design-notes/statistics.md): reads the samples and key moments the world store saved,
/// and thins old samples: every sample of the last 2 days stays, those on the hour for 30 days, those at midnight for good
/// </summary>
public sealed class HistoryStore(TransportPipesDbFactory dbFactory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static readonly TimeSpan KeepAll = TimeSpan.FromDays(2);
    public static readonly TimeSpan KeepHourly = TimeSpan.FromDays(30);

    public sealed record Sample(DateTime At, Dictionary<string, double> Values);

    /// <summary>The samples of a company or town from a moment on: the first of every step</summary>
    public async Task<List<Sample>> SamplesAsync(StatSubject kind, long subject, DateTime from, TimeSpan step, CancellationToken cancellationToken = default)
    {
        await using var db = dbFactory.CreateDbContext();
        var rows = await db.StatSamples.Where(t => t.Kind == (int)kind && t.Subject == subject && t.At >= from)
            .OrderBy(t => t.At).ToListAsync(cancellationToken);
        var samples = new List<Sample>();
        var bucket = long.MinValue;
        foreach (var row in rows)
        {
            if (row.At.Ticks / step.Ticks == bucket)
            {
                continue;
            }
            bucket = row.At.Ticks / step.Ticks;
            samples.Add(new Sample(row.At, Values(row.Values)));
        }
        return samples;
    }

    /// <summary>The key moments of a company from a moment on, oldest first</summary>
    public async Task<List<Moment>> MomentsAsync(long company, DateTime from, bool bigOnly = false, CancellationToken cancellationToken = default)
    {
        await using var db = dbFactory.CreateDbContext();
        var rows = await db.Moments.Where(t => t.Company == company && t.At >= from && (!bigOnly || t.Big))
            .OrderBy(t => t.At).ThenBy(t => t.Id).ToListAsync(cancellationToken);
        return rows.Select(t => new Moment(t.Company, t.At, t.Kind, t.Text, t.Big, t.Ref)).ToList();
    }

    /// <summary>One series of every company at every midnight (the admin's Pace): by company, oldest first</summary>
    public async Task<Dictionary<long, List<(DateTime At, double Value)>>> DailyAsync(string series, CancellationToken cancellationToken = default)
    {
        await using var db = dbFactory.CreateDbContext();
        var rows = await db.StatSamples.Where(t => t.Kind == (int)StatSubject.Company && t.At.Hour == 0 && t.At.Minute == 0)
            .OrderBy(t => t.At).ToListAsync(cancellationToken);
        var daily = new Dictionary<long, List<(DateTime At, double Value)>>();
        foreach (var row in rows)
        {
            if (Values(row.Values).TryGetValue(series, out var value))
            {
                if (!daily.TryGetValue(row.Subject, out var list))
                {
                    daily[row.Subject] = list = new List<(DateTime At, double Value)>();
                }
                list.Add((row.At, value));
            }
        }
        return daily;
    }

    /// <summary>Thins the samples: off the hour after 2 days, off midnight after 30</summary>
    public async Task PruneAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        await using var db = dbFactory.CreateDbContext();
        var all = now - KeepAll;
        var hourly = now - KeepHourly;
        await db.StatSamples.Where(t => t.At < all && t.At.Minute != 0).ExecuteDeleteAsync(cancellationToken);
        await db.StatSamples.Where(t => t.At < hourly && t.At.Hour != 0).ExecuteDeleteAsync(cancellationToken);
    }

    private static Dictionary<string, double> Values(string json) =>
        JsonSerializer.Deserialize<Dictionary<string, double>>(json, Json) ?? new Dictionary<string, double>();
}
