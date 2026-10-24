using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

[TestClass]
public class LeagueFacts
{
    [TestMethod]
    public void LeaguesOfAbout25AtASimilarStageAreDrawnEveryWeek()
    {
        var game = new TestGame();
        var companies = Enumerable.Range(1, 60).Select(i => game.NewCompany($"Co {i}", level: i)).ToList();
        foreach (var company in companies)
        {
            company.DistinctDays = 3;
        }
        var newcomer = game.NewCompany("Newcomer");
        void Days(int days)
        {
            for (var d = 0; d < days; d++)
            {
                foreach (var company in companies.Append(newcomer))
                {
                    company.LastSeenAt = game.World.Now;
                }
                game.Pass(24 * 60);
            }
        }

        Days(8);

        Assert.IsNull(newcomer.League, "three days of play first");
        var leagues = companies.GroupBy(t => t.League).ToList();
        Assert.HasCount(2, leagues, "60 companies: two leagues");
        Assert.IsTrue(leagues.All(t => t.Key != null && t.Count() == 30));
        var top = companies.Single(t => t.Level == 60).League;
        Assert.IsTrue(companies.Where(t => t.Level > 30).All(t => t.League == top), "by stage");

        companies[0].LifetimeEarnings += 1_000_000;
        Days(7);
        Assert.Contains("League champion", companies[0].Titles, "the one who earned most this week");
        var board = game.World.Leaderboard(LeaderboardKind.League, null, 50, companies[0].League);
        Assert.HasCount(30, board, "the league table");
    }
}
