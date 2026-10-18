namespace TransportPipes.Game.State;

/// <summary>One company's result at the end of a season</summary>
public sealed record SeasonResult(long UserId, string Company, double NetWorth, int Rank, int Legacy, string? Title);

/// <summary>
/// The season (GDD §8.8): it ends when the Space Elevator (the Era 5 Wonder) is finished or Era 5's deadline passes,
/// after a 24 hour launch countdown. Every company's result becomes Legacy for its owner: account perks for the next
/// seasons (more starting gold, a manager slot, titles), never so much that newcomers can't compete. The next world
/// opens a week later (the server does that).
/// </summary>
public sealed partial class GameWorld
{
    public static readonly TimeSpan FinaleCountdown = TimeSpan.FromHours(24);
    public const int LegacyManagerLevel = 5;

    public int Season { get; set; } = 1;

    /// <summary>When the Space Elevator launches and the season ends (null: not yet)</summary>
    public DateTime? FinaleAt { get; set; }

    public DateTime? SeasonEndedAt { get; set; }

    public List<SeasonResult> SeasonResults { get; private set; } = new();

    public void LoadSeason(int season, DateTime? finaleAt, DateTime? endedAt, IEnumerable<SeasonResult> results)
    {
        Season = Math.Max(1, season);
        FinaleAt = finaleAt;
        SeasonEndedAt = endedAt;
        SeasonResults = results.ToList();
    }

    /// <summary>Legacy level from Legacy points: the square root, in tens</summary>
    public static int LegacyLevelOf(int legacy) => (int)Math.Floor(Math.Sqrt(Math.Max(0, legacy) / 10.0));

    /// <summary>The extra starting gold Legacy gives (at most 10,000)</summary>
    public static double LegacyGold(int legacyLevel) => Math.Min(10_000, 500.0 * legacyLevel);

    /// <summary>The countdown to the launch starts</summary>
    public void StartFinale()
    {
        if (FinaleAt != null)
        {
            return;
        }
        FinaleAt = LastTick + FinaleCountdown;
        var finale = new GameEvent { Id = NextEventId++, Kind = EventKind.Finale, StartsAt = FinaleAt.Value, EndsAt = FinaleAt.Value.AddDays(7), Announced = true };
        _events.Add(finale);
        foreach (var company in _companies.Values.Where(t => !t.IsNpc))
        {
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Wonder, $"The Space Elevator launches {FinaleAt:ddd HH:mm} UTC: the season finale! Last chance to climb the rankings"));
            Changes.Companies.Add(company.Id);
        }
        Changes.Wonders = true;
    }

    private IEnumerable<(DateTime At, Action Run)> SeasonMoments(DateTime from, DateTime to)
    {
        if (FinaleAt is { } at && at <= to && SeasonEndedAt == null)
        {
            yield return (at > from ? at : from, EndSeason);
        }
    }

    /// <summary>The ceremony: rankings by net worth, Legacy for everyone, titles for the best</summary>
    public void EndSeason()
    {
        if (SeasonEndedAt != null)
        {
            return;
        }
        SeasonEndedAt = LastTick;
        var ranked = _companies.Values.Where(t => !t.IsNpc)
            .Select(t => (Company: t, Worth: NetWorth(t)))
            .OrderByDescending(t => t.Worth).ThenBy(t => t.Company.Id)
            .ToList();
        var titles = new[] { $"Season {Season} Champion", $"Season {Season} Silver", $"Season {Season} Bronze" };
        SeasonResults = ranked.Select((t, i) =>
        {
            var company = t.Company;
            var legacy = (int)Math.Floor(Math.Sqrt(t.Worth / 1000))
                + 10 * company.Achievements.Count
                + 5 * company.Titles.Count
                + 20 * company.Franchises
                + Math.Max(0, 100 - 10 * i);
            var title = i < titles.Length ? titles[i] : null;
            return new SeasonResult(company.UserId, company.Name, Math.Round(t.Worth), i + 1, legacy, title);
        }).ToList();
        foreach (var result in SeasonResults)
        {
            if (CompanyOfUser(result.UserId) is { } company)
            {
                Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Wonder, $"The season is over! You finished #{result.Rank} and earned {result.Legacy} Legacy"));
                Changes.Companies.Add(company.Id);
            }
        }
        Changes.Wonders = true;
    }
}
