using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransportPipes.Database;
using TransportPipes.Database.Entities;
using TransportPipes.Server.Dtos;

namespace TransportPipes.Server.Tests;

/// <summary>Statistics (docs/design-notes/statistics.md): saved with the world, served whole to you and in part to others</summary>
[TestClass]
public class StatisticsFacts
{
    [TestMethod]
    public async Task YourStatisticsAreSavedAndOthersSeeOnlyTheirPublicPart()
    {
        using var server = new GameServer();
        await using var owner = await server.ConnectAsync(await server.RegisterAsync("Grapher"));
        var hello = await owner.Invoke<HelloDto>("Hello");
        await owner.Command("CreateCompany", "Graphs & Co", "#3366cc", null, null, null, null);
        var (pipe, moily) = await GameServerFacts.SpotNextToTradingPost(server, hello.User.Id);
        await owner.Command("PlaceBuilding", "windMoily", moily.X, moily.Y);
        await owner.Command("PlacePipes", new[] { pipe.X, pipe.Y }, 1);

        // The clock starts a minute after a quarter hour: two samples in half an hour
        await server.AdvanceAsync(TimeSpan.FromMinutes(30));
        await server.World.SaveNowAsync();

        var mine = await owner.Invoke<StatisticsDto>("GetStatistics", null, "day");
        var keys = mine.Keys.ToList();
        Assert.IsTrue(mine.Own);
        Assert.HasCount(3, mine.At, "two samples and the live point");
        Assert.IsGreaterThan(0, mine.Rows[^1][keys.IndexOf("income")]!.Value);
        Assert.IsGreaterThan(mine.Rows[0][keys.IndexOf("earned")]!.Value, mine.Rows[^1][keys.IndexOf("earned")]!.Value);
        CollectionAssert.Contains(keys, "gold");
        CollectionAssert.Contains(keys, "made:energy");
        Assert.AreEqual("founded", mine.Moments[0].Kind);
        Assert.IsTrue(mine.Moments.Any(t => t is { Kind: "first", Text: "First Energy", Big: true }), string.Join(", ", mine.Moments.Select(t => t.Text)));
        Assert.HasCount(3, mine.Town.At);
        Assert.AreEqual(1, mine.Town.Rows[^1][mine.Town.Keys.ToList().IndexOf("players")]);

        // Someone else sees what the leaderboards show and the big moments
        await using var visitor = await server.ConnectAsync(await server.RegisterAsync("Peeker"));
        await visitor.Invoke<HelloDto>("Hello");
        await visitor.Command("CreateCompany", "Peek Ltd", "#cc3366", null, null, null, null);
        var theirs = await visitor.Invoke<StatisticsDto>("GetStatistics", mine.Company, "day");
        Assert.IsFalse(theirs.Own);
        CollectionAssert.Contains(theirs.Keys.ToList(), "income");
        CollectionAssert.DoesNotContain(theirs.Keys.ToList(), "gold", "the bank is its own");
        Assert.IsFalse(theirs.Keys.Any(t => t.StartsWith("made:") || t.StartsWith("spent")), "and what it makes and spends");
        Assert.IsTrue(theirs.Moments.All(t => t.Big));
        Assert.IsEmpty(theirs.Peers);
        Assert.IsNull(await visitor.Invoke<StatsSeriesDto?>("GetStatisticsSeries", mine.Company, "gold", "day"));
        var worth = await visitor.Invoke<StatsSeriesDto>("GetStatisticsSeries", mine.Company, "worth", "day");
        Assert.HasCount(3, worth.Values);
        Assert.IsNull(await visitor.Invoke<PaceDto?>("GetPace"), "the Pace is the admin's");
    }

    [TestMethod]
    public async Task OldSamplesAreThinned()
    {
        using var server = new GameServer();
        await server.World.Ready;
        var factory = server.Services.GetRequiredService<TransportPipesDbFactory>();
        var now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
        DateTime[] times =
        [
            now.AddDays(-1).AddMinutes(15), // the last 2 days: every sample
            now.AddDays(-3), now.AddDays(-3).AddMinutes(15), // up to 30 days: on the hour
            now.AddDays(-31).AddHours(-12), now.AddDays(-31).AddHours(-11), // after that: midnight
        ];
        await using (var db = factory.CreateDbContext())
        {
            db.StatSamples.AddRange(times.Select(t => new DbStatSample { Kind = 0, Subject = 99, At = t, Values = "{\"income\":1}" }));
            await db.SaveChangesAsync();
        }

        await server.Services.GetRequiredService<HistoryStore>().PruneAsync(now);

        await using (var db = factory.CreateDbContext())
        {
            var left = await db.StatSamples.Where(t => t.Subject == 99).OrderBy(t => t.At).Select(t => t.At).ToListAsync();
            CollectionAssert.AreEqual(new[] { times[3], times[1], times[0] }, left);
        }
    }
}
