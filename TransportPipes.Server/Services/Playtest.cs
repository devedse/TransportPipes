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

        // Run 2's Jo (docs/playtest-fixes.html §6): a scripted solo human, seated by placement like anyone who comes
        // alone, whose factories lack the energy a neighbour's kit makes; no agent, never chats
        group.MapPost("/jo", (WorldHost host) => host.RunAsync(world =>
        {
            if (world.Companies.FirstOrDefault(t => t.Name == "Jo's Works") is { } existing)
            {
                return new { id = existing.Id, created = false, buildings = world.BuildingsOf(existing.Id).Count() };
            }
            var seat = world.SuggestSeat(Placement.Neighbours);
            var jo = world.CreateCompany(900_001, "Jo's Works", "#7d5ba6", seat: seat);
            world.PlaceScripted(jo, "ironMine", 2);
            world.PlaceScripted(jo, "smallFactory", 4);
            return new { id = jo.Id, created = true, buildings = world.BuildingsOf(jo.Id).Count() };
        }));

        // A scripted player was here (it stays an active neighbour)
        group.MapPost("/seen/{companyId:long}", (long companyId, WorldHost host) => host.RunAsync(world =>
        {
            if (world.Company(companyId) is { } company)
            {
                company.LastSeenAt = world.Now;
                company.Touch();
            }
            return new { seen = companyId };
        }));

        // A company's station as its Yard sees it: every resident's spare and lacks at the gate, and the company's Sends
        // with why each moves nothing (run 2: Dev's Sends moved nothing for 28 days)
        group.MapGet("/yard/{companyId:long}", (long companyId, WorldHost host) => host.RunAsync(world =>
        {
            if (world.Company(companyId) is not { } company || world.EstateOf(company) is not { } estate)
            {
                return (object)new { error = "no such company" };
            }
            return new
            {
                residents = world.YardResidents(estate.Station).Select(t => new { id = t.Company.Id, name = t.Company.Name, house = t.Company.IsNpc, connected = t.Connected, spare = t.Spare, lacks = t.Lacks }),
                trades = world.YardTrades(estate).Select(t => new
                {
                    partner = t.Partner, resource = t.Resource, perMinute = t.PerMinute, selling = t.Selling,
                    price = world.Company(t.Partner) is { } partner ? world.YardPrice(company, partner, t.Resource, t.Selling) : 0,
                    sameDevice = world.Company(t.Partner) is { } other && world.SameDevice(company, other),
                }),
                devices = company.Devices.Keys,
                sends = company.Sends.Select(t => new { to = t.To, resource = t.Resource, building = t.Building, idle = world.SendIdleReason(company, t) }),
            };
        }, changesWorld: false));

        // The return card a company would see if its player came back now (F5's gate), without waking it
        group.MapGet("/card/{companyId:long}", (long companyId, WorldHost host) => host.RunAsync(world =>
        {
            if (world.Company(companyId) is not { } company)
            {
                return null;
            }
            var away = new GameViews.AwayFacts(world.Now - company.LastSeenAt, company.Report, company.StandIn?.Name, company.Asleep, company.Asleep && company.Packed != null);
            return GameViews.ReturnCard(world, company, away, null);
        }));

        // Every station's project starts its stage again, by today's rules (old baskets in a replayed snapshot)
        group.MapPost("/restage", (WorldHost host) => host.RunAsync(world =>
        {
            world.RestageProjects();
            return new { restaged = true };
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
                npcGold = company.Stats.GetValueOrDefault("npcGold"),
                projectGold = company.Stats.GetValueOrDefault("projectGold"),
                // Every building that doesn't run fully, and why (IdleReason): the digest finds when each started
                idle = world.BuildingsOf(company.Id).Select(b => (Building: b, Reason: world.IdleReasonOf(b)))
                    .Where(t => t.Reason.Cause != IdleCause.None)
                    .Select(t => new { id = t.Building.Id, type = t.Building.Type, cause = t.Reason.Cause.ToString(), resource = t.Reason.Resource, other = t.Reason.Other })
                    .ToList(),
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
                // The stage's basket (good: delivered / target, the keystone marked) and the This week line
                projectBasket = project?.Basket.ToDictionary(t => t.Key == project.Keystone ? $"{t.Key}*" : t.Key, t => $"{project.Delivered.GetValueOrDefault(t.Key):0} / {t.Value:0}"),
                week = GameViews.Week(world, company)?.Text,
                // Step 4's measures: its Sends, the first gifts it heard of, who thanked it
                sends = company.Sends.Select(t => $"{t.Resource}>{t.To}").Concat(company.ProjectFirst.Select(t => $"{t}>project")).ToList(),
                giftsAnnounced = company.GiftsAnnounced.ToList(),
                thanks = company.Report.Thanks.Count,
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
