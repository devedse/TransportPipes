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

        // A scripted player was here, a session of its own (it stays an active neighbour and has seen what's new)
        group.MapPost("/seen/{companyId:long}", (long companyId, WorldHost host) => host.RunAsync(world =>
        {
            if (world.Company(companyId) is { } company)
            {
                company.LastSeenAt = company.SessionStartedAt = world.Now;
                company.Touch();
            }
            return new { seen = companyId };
        }));

        // A company's Yard as it sees it: every resident's spare and lacks at the gate, and its trades
        group.MapGet("/yard/{companyId:long}", (long companyId, WorldHost host) => host.RunAsync(world =>
        {
            if (world.Company(companyId) is not { } company || world.EstateOf(company) is not { } estate)
            {
                return (object)new { error = "no such company" };
            }
            return new
            {
                residents = world.YardResidents(estate.Station).Select(t => new { id = t.Company.Id, name = t.Company.Name, connected = t.Connected, spare = t.Spare, lacks = t.Lacks }),
                trades = world.YardTrades(estate).Select(t => new
                {
                    partner = t.Partner, resource = t.Resource, perMinute = t.PerMinute, selling = t.Selling,
                    price = world.Company(t.Partner) is { } partner ? world.YardPrice(company, partner, t.Resource, t.Selling) : 0,
                    sameDevice = world.Company(t.Partner) is { } other && world.SameDevice(company, other),
                }),
                devices = company.Devices.Keys,
            };
        }, changesWorld: false));

        // The return card a company would see if its player came back now (F5's gate), without waking it
        group.MapGet("/card/{companyId:long}", (long companyId, WorldHost host) => host.RunAsync(world =>
        {
            if (world.Company(companyId) is not { } company)
            {
                return null;
            }
            var away = new GameViews.AwayFacts(world.Now - company.LastSeenAt, company.Report);
            return GameViews.ReturnCard(world, company, away, null);
        }));

        // A company's networks as the solver sees them: what every building could make and needs, how fast it runs,
        // and where every unit went (for finding out why a building starves)
        group.MapGet("/networks/{companyId:long}", (long companyId, WorldHost host) => host.RunAsync(world =>
            world.Networks.Where(n => n.Buildings.Any(id => world.Building(id)?.Owner == companyId)).Select(n => new
            {
                id = n.Id,
                nodes = n.Nodes.Select(t => new
                {
                    t.Id,
                    type = world.Building(t.Id)?.Type,
                    t.Owner,
                    kind = t.Kind.ToString(),
                    t.In,
                    t.Out,
                    t.MaxRate,
                    rate = n.Solution.Rate(t.Id),
                }),
                flows = n.Solution.Flows.Select(f => new { f.From, f.To, f.Resource, f.Sent, f.Delivered, kind = f.Kind.ToString(), f.PricePerUnit }),
                gateFlows = n.GateFlows,
                links = n.Links.Select(l => new { l.A, l.B, l.Efficiency, l.Capacity, l.Resources }),
            }).ToList(), changesWorld: false));
    }

    /// <summary>The players, with what the report draws</summary>
    private static object Companies(GameWorld world)
    {
        var gold = world.GoldPerMinute();
        var players = world.Companies.OrderBy(t => t.Id).Select(company =>
        {
            var estate = world.EstateOf(company);
            var station = estate?.Station;
            return new
            {
                id = company.Id,
                name = company.Name,
                crew = company.CrewId is { } crewId ? world.Crew(crewId)?.Name : null,
                station = station is { } at ? WorldNames.Station(at) : null,
                slot = company.Slot,
                level = company.Level,
                era = world.YourEra(company),
                gold = company.Gold,
                goldPerMinute = gold.GetValueOrDefault(company.Id),
                neighbourIncome = world.NeighbourIncome(company),
                yardMatched = world.NeighbourIncome(company),
                // What the Yard brings this company per minute now, by good (what it was matched, taken or not)
                yardImports = world.EstatesOf(company).SelectMany(world.YardTrades).Where(t => !t.Selling)
                    .GroupBy(t => t.Resource).ToDictionary(t => t.Key, t => t.Sum(x => x.PerMinute)),
                earned = company.LifetimeEarnings,
                // Lifetime measures (F0): what the Yard paid, what it sent that nobody took, and where new gold came from
                yardPaid = company.Stats.GetValueOrDefault("yardPaid"),
                yardFloor = company.Stats.GetValueOrDefault("yardFloor"),
                yardSent = company.Stats.GetValueOrDefault("yardSent"),
                yardUntaken = company.Stats.GetValueOrDefault("yardUntaken"),
                hubGold = company.Stats.GetValueOrDefault("hubGold"),
                // Every building that doesn't run fully, and why (IdleReason): the digest finds when each started
                idle = world.BuildingsOf(company.Id).Select(b => (Building: b, Reason: world.IdleReasonOf(b)))
                    .Where(t => t.Reason.Cause != IdleCause.None)
                    .Select(t => new { id = t.Building.Id, type = t.Building.Type, cause = t.Reason.Cause.ToString(), resource = t.Reason.Resource, other = t.Reason.Other })
                    .ToList(),
                buildings = world.BuildingsOf(company.Id).Count(),
                parcels = estate?.UnlockedCount ?? 0,
                tutorialStep = company.TutorialStep,
                packed = company.Packed != null,
                lastSeen = company.LastSeenAt,
                links = world.LinksOf(company.Id).Count(),
                wants = company.Asks.Count,
                // Run 4's measures (docs/playtest-run3-design.html, "How run 4 will show it worked"): what gold bought, the
                // wants' materials by where they came from, the Era wonder and resting Labs
                spent = company.Stats.GetValueOrDefault("goldSpent"),
                ordered = company.Stats.GetValueOrDefault("ordered"),
                widened = company.Stats.GetValueOrDefault("widened"),
                levelsBought = company.Stats.GetValueOrDefault("levelsBought"),
                wantOwn = company.Stats.GetValueOrDefault("wantOwn"),
                wantFriends = company.Stats.GetValueOrDefault("wantFriends"),
                wantHub = company.Stats.GetValueOrDefault("wantHub"),
                wonder = company.Stats.GetValueOrDefault("wonder"),
                wonderGold = company.Stats.GetValueOrDefault("wonderGold"),
                tiers = new Dictionary<string, int>(company.TrackTiers),
                researched = company.Researched.Count,
                researchGold = company.Stats.GetValueOrDefault("researchGold"),
                monumentGold = company.Stats.GetValueOrDefault("monumentGold"),
                imported = company.Stats.GetValueOrDefault("imported"),
                orders = company.Stats.GetValueOrDefault("orders"),
                tradeFalls = company.TradeFalls.Count,
                // After run 4: which doors were open at the sample, and what was pressed since the start ("doors logged
                // as shown": a run that spends little with these open says the plan is wrong)
                pipeTiers = company.Stats.GetValueOrDefault("pipeTiers"),
                doors = new
                {
                    pipe = world.BuildingsOf(company.Id).Count(t => world.PipeDoorFor(company, t, preview: false) is { Kind: not PipeDoorKind.Locked }),
                    grow = world.PlanGrowth(company).Parts.Count,
                    wonder = world.BuyAndGiveCost(company) > 0 ? 1 : 0,
                    wants = world.WantsNear(company).Count,
                },
                pressed = company.Stats.Where(t => t.Key.StartsWith("pressed:", StringComparison.Ordinal)).ToDictionary(t => t.Key["pressed:".Length..], t => t.Value),
            };
        }).ToList();
        return new
        {
            now = world.Now,
            towns = world.Towns.Select(t => new { x = t.X, y = t.Y, era = t.Era, wonder = t.Wonder?.Type, stage = t.Wonder?.Stage, full = t.Wonder?.FullAt }).ToList(),
            activePlayers = world.ActivePlayers(),
            players,
        };
    }
}
