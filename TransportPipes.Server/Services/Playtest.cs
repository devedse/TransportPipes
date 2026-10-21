using System.Text.Json;
using Microsoft.Extensions.Options;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Server.Services;

/// <summary>
/// The playtest's clock (TransportPipes:Playtest, docs/playtest-design.html): the real time, moved forward between
/// sessions and slowed during them, so AI players live through weeks in an afternoon. It is kept in a file, so a
/// restarted server goes on from where the clock was (time never runs backwards for a saved world).
/// </summary>
public sealed class PlaytestClock : TimeProvider
{
    private readonly object _lock = new();
    private readonly string _file;
    private DateTimeOffset _realAnchor;
    private DateTimeOffset _gameAnchor;
    private double _rate = 1;

    private sealed record State(DateTimeOffset Game, DateTimeOffset Real, double Rate);

    public PlaytestClock(string file)
    {
        _file = file;
        _realAnchor = System.GetUtcNow();
        _gameAnchor = _realAnchor;
        if (File.Exists(file) && JsonSerializer.Deserialize<State>(File.ReadAllText(file)) is { } state)
        {
            // Where the clock had got to, and on at its rate since
            _rate = state.Rate;
            _gameAnchor = state.Game + (_realAnchor - state.Real) * state.Rate;
        }
        Save();
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_lock)
        {
            return _gameAnchor + (System.GetUtcNow() - _realAnchor) * _rate;
        }
    }

    public double Rate
    {
        get
        {
            lock (_lock)
            {
                return _rate;
            }
        }
    }

    /// <summary>Moves the clock forward (the world catches up on its next tick)</summary>
    public void Advance(TimeSpan by) => Change(() => _gameAnchor += by);

    /// <summary>Game seconds per real second from now on</summary>
    public void SetRate(double rate) => Change(() => _rate = Math.Clamp(rate, 0.01, 1000));

    private void Change(Action change)
    {
        lock (_lock)
        {
            var now = System.GetUtcNow();
            _gameAnchor += (now - _realAnchor) * _rate;
            _realAnchor = now;
            change();
            Save();
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_file))!);
        File.WriteAllText(_file, JsonSerializer.Serialize(new State(_gameAnchor, _realAnchor, _rate)));
    }
}

/// <summary>Every world event of a playtest (what the players saw as toasts and notes), one JSON line each</summary>
public sealed class PlaytestLog(IOptions<TransportPipesOptions> options)
{
    private readonly object _lock = new();
    private readonly string _file = Path.Combine(options.Value.PlaytestDataDir, "events.jsonl");

    public void Write(GameWorld world, IReadOnlyCollection<WorldEvent> events)
    {
        if (events.Count == 0)
        {
            return;
        }
        var lines = events.Select(t => JsonSerializer.Serialize(new
        {
            at = world.Now,
            company = t.Company,
            name = world.Company(t.Company)?.Name,
            kind = t.Kind.ToString(),
            text = t.Text,
        }));
        lock (_lock)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_file))!);
            File.AppendAllLines(_file, lines);
        }
    }
}

/// <summary>The playtest director's calls: the clock, saving, and the numbers per company (for the report's graphs)</summary>
public static class PlaytestEndpoints
{
    public sealed record AdvanceRequest(double Hours);

    public sealed record RateRequest(double Rate);

    public static void MapPlaytest(this WebApplication app, TransportPipesOptions options)
    {
        var group = app.MapGroup("/playtest").AddEndpointFilter(async (context, next) =>
            string.IsNullOrEmpty(options.PlaytestToken) || context.HttpContext.Request.Headers["X-Playtest-Token"] != options.PlaytestToken
                ? Results.StatusCode(StatusCodes.Status403Forbidden)
                : await next(context));

        group.MapGet("/now", (PlaytestClock clock) => new { now = clock.GetUtcNow(), rate = clock.Rate });

        group.MapPost("/advance", async (AdvanceRequest request, PlaytestClock clock, WorldHost host) =>
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            clock.Advance(TimeSpan.FromHours(Math.Max(0, request.Hours)));
            await host.TickAsync();
            return new { now = clock.GetUtcNow(), caughtUpMs = stopwatch.ElapsedMilliseconds };
        });

        group.MapPost("/rate", (RateRequest request, PlaytestClock clock) =>
        {
            clock.SetRate(request.Rate);
            return new { now = clock.GetUtcNow(), rate = clock.Rate };
        });

        group.MapPost("/save", async (WorldHost host) =>
        {
            await host.SaveNowAsync();
            return new { saved = true };
        });

        group.MapGet("/companies", (WorldHost host) => host.RunAsync(Companies, changesWorld: false));
    }

    /// <summary>The players (and how many Houses), with what the report draws</summary>
    private static object Companies(GameWorld world)
    {
        var gold = world.GoldPerMinute();
        var players = world.Companies.Where(t => !t.IsNpc).OrderBy(t => t.Id).Select(company =>
        {
            var estate = world.EstateOf(company);
            var station = estate?.Station;
            var project = station is { } s && world.ProjectsEnabled ? world.ProjectOf(s) : null;
            return new
            {
                id = company.Id,
                name = company.Name,
                crew = company.CrewId is { } crewId ? world.Crew(crewId)?.Name : null,
                station = station is { } at ? WorldNames.Station(at) : null,
                slot = company.Slot,
                level = company.Level,
                xp = company.Xp,
                gold = company.Gold,
                goldPerMinute = gold.GetValueOrDefault(company.Id),
                neighbourIncome = world.NeighbourIncome(company),
                earned = company.LifetimeEarnings,
                buildings = world.BuildingsOf(company.Id).Count(),
                parcels = estate?.UnlockedCount ?? 0,
                tutorialStep = company.TutorialStep,
                asleep = company.Asleep,
                packed = company.Packed != null,
                standIn = company.StandIn?.Name,
                lastSeen = company.LastSeenAt,
                links = world.LinksOf(company.Id).Count(),
                asks = company.Asks.Count,
                tailwind = world.TailwindFor(company),
                projectStage = project?.Stage.ToString(),
                projectFinished = project?.Finished.Keys.Select(t => t.ToString()).ToList(),
                projectCredit = project?.Credit.GetValueOrDefault(company.Id) ?? 0,
            };
        }).ToList();
        return new
        {
            now = world.Now,
            era = world.Era,
            smallMode = world.SmallMode,
            activePlayers = world.ActivePlayers(),
            houses = world.Companies.Count(t => t.IsNpc),
            players,
        };
    }
}
