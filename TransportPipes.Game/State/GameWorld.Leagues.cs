namespace TransportPipes.Game.State;

/// <summary>
/// Leagues (v1.2 §13): about 25 companies at a similar stage, ranked by what they earned in the week, regrouped every
/// week with no demotion; only verified companies (three days of play) count. Off in small mode, where everyone knows
/// everyone anyway.
/// </summary>
public sealed partial class GameWorld
{
    public const int LeagueSize = 25;
    private const int LeagueVerifiedDays = 3;

    /// <summary>The week ends: the three who earned most in each league hear it (the first gets the title), then the leagues are drawn anew</summary>
    private void RegroupLeagues()
    {
        foreach (var league in _companies.Values.Where(t => t.League != null).GroupBy(t => t.League!.Value).ToList())
        {
            var ranked = league.OrderByDescending(t => t.LifetimeEarnings - t.WeekStartEarnings).ThenBy(t => t.Id).Take(3).ToList();
            for (var i = 0; i < ranked.Count; i++)
            {
                var company = ranked[i];
                Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Achievement, i == 0
                    ? "You earned most in your league this week: League champion!"
                    : $"You came {(i == 1 ? "second" : "third")} in your league this week"));
                if (i == 0)
                {
                    company.Titles.Add("League champion");
                }
                company.Touch();
            }
        }
        var players = SmallMode
            ? []
            : _companies.Values.Where(t => !t.IsNpc && IsActive(t) && t.DistinctDays >= LeagueVerifiedDays)
                .OrderByDescending(t => t.Level).ThenBy(t => t.Id).ToList();
        foreach (var company in _companies.Values.Where(t => t.League != null))
        {
            company.League = null;
            company.Touch();
        }
        // Groups of about 25 at a similar stage (the last group takes the rest when it would be small)
        var groups = Math.Max(1, (int)Math.Round(players.Count / (double)LeagueSize));
        for (var i = 0; i < players.Count; i++)
        {
            players[i].League = 1 + i * groups / Math.Max(1, players.Count);
            players[i].Touch();
        }
    }
}
