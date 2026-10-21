using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using TransportPipes.Database;
using TransportPipes.Game.Content;
using TransportPipes.Game.State;
using TransportPipes.Game.World;
using TransportPipes.Server.Dtos;
using TransportPipes.Server.Hubs;

namespace TransportPipes.Server.Services;

/// <summary>
/// Owns the game world: loads it, ticks it, saves it and tells clients what changed. All access goes through
/// <see cref="RunAsync{T}"/>, which serialises it.
/// </summary>
public sealed class WorldHost : BackgroundService
{
    private readonly WorldStore _store;
    private readonly IHubContext<GameHub, IGameClient> _hub;
    private readonly ConnectedUsers _connectedUsers;
    private readonly SocialService _social;
    private readonly AccountService _accounts;
    private readonly TransportPipesOptions _options;
    private readonly ILogger<WorldHost> _logger;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Held from taking a snapshot until it is written, and by the season reset: a snapshot of the old world must not
    /// land in the database after the reset (taken before _lock, never the other way round)
    /// </summary>
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private GameWorld? _world;
    private bool _saveSoon;
    private DateTime _lastSave;

    public WorldHost(WorldStore store, IHubContext<GameHub, IGameClient> hub, ConnectedUsers connectedUsers, SocialService social, AccountService accounts, IOptions<TransportPipesOptions> options, ILogger<WorldHost> logger, TimeProvider time, PlaytestLog? playtestLog = null)
    {
        _playtestLog = playtestLog;
        _store = store;
        _hub = hub;
        _connectedUsers = connectedUsers;
        _social = social;
        _accounts = accounts;
        _options = options.Value;
        _logger = logger;
        _time = time;
    }

    public Task Ready => _ready.Task;

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    private readonly PlaytestLog? _playtestLog;

    /// <summary>Saves the world now (the playtest director, before a snapshot of the database)</summary>
    public async Task SaveNowAsync()
    {
        await Ready;
        await SaveAsync(force: true);
    }

    /// <summary>Runs something on the world (a command or a query) and sends the changes it caused to the clients</summary>
    public async Task<T> RunAsync<T>(Func<GameWorld, T> action, bool changesWorld = true)
    {
        await Ready;
        List<Func<Task>> pushes;
        T result;
        await _lock.WaitAsync();
        try
        {
            result = action(_world!);
            pushes = CollectPushes(_world!);
            if (changesWorld)
            {
                _saveSoon = true;
            }
        }
        finally
        {
            _lock.Release();
        }
        await SendAsync(pushes);
        return result;
    }

    public Task RunAsync(Action<GameWorld> action, bool changesWorld = true) => RunAsync(world =>
    {
        action(world);
        return true;
    }, changesWorld);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            GameViews.SeasonBreak = TimeSpan.FromDays(_options.SeasonBreakDays);
            var world = await LoadWorldAsync(stoppingToken);
            var away = Now - world.LastTick;
            world.Tick();
            // Estates from before the starter kit get it now (once), and their players a note
            var surveyed = _options.Surveys ? world.SurveyEstates() : [];
            world.TakeChanges();
            _world = world;
            _lastSave = Now;
            _logger.LogInformation("Loaded the world (seed {Seed}, {Companies} companies, {Buildings} buildings) and caught up {Away} in {Elapsed} ms",
                world.Generator.Seed, world.Companies.Count(), world.Buildings.Count(), away, stopwatch.ElapsedMilliseconds);
            await SaveAsync(force: true);
            _ready.SetResult();
            if (surveyed.Count > 0)
            {
                _logger.LogInformation("Surveyed {Estates} estates for their starter kit", surveyed.Count);
                await NotifyAsync(surveyed.Select(t => (t.Company, "survey", t.Text + " Look for the new deposits near your HQ.")).ToList());
            }
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogCritical(ex, "Could not load the world");
            _ready.SetException(ex);
            throw;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.TickSeconds), _time);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await TickAsync();
                    await SaveAsync(force: false);
                    await SeasonAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "World tick failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task<GameWorld> LoadWorldAsync(CancellationToken cancellationToken)
    {
        var world = await _store.LoadAsync(GameContent.Default, () => Now, _options.WorldSeed, cancellationToken);
        if (_options.StartingGold is { } startingGold)
        {
            world.StartingGold = startingGold;
        }
        if (_store.WorldReset)
        {
            _logger.LogWarning("The saved world was made with older rules: it was thrown away and a new world starts (accounts stay)");
        }
        world.TenantsEnabled = _options.Tenants;
        world.QuietGraceEnabled = true;
        world.ProjectsEnabled = true;
        world.TrunksEnabled = true;
        world.EventsEnabled = _options.Events;
        world.TutorialEnabled = _options.Tutorial;
        world.SurveysEnabled = _options.Surveys;
        if (_options.Tenants)
        {
            world.EnsureTenants();
        }
        return world;
    }

    /// <summary>
    /// The end of a season: the results go to the players' Legacy (once), and after the break the next world opens
    /// </summary>
    public async Task SeasonAsync()
    {
        List<SeasonResult>? results = null;
        DateTime? ended = null;
        var season = 1;
        await _lock.WaitAsync();
        try
        {
            if (_world!.SeasonEndedAt is { } endedAt)
            {
                ended = endedAt;
                season = _world.Season;
                if (!_store.ResultsSaved)
                {
                    // Claimed here, so the results go to Legacy only once
                    results = _world.SeasonResults.ToList();
                    _store.ResultsSaved = true;
                }
            }
        }
        finally
        {
            _lock.Release();
        }
        if (results != null)
        {
            Dictionary<long, int> legacy;
            try
            {
                legacy = await _accounts.AddLegacyAsync(results);
            }
            catch
            {
                _store.ResultsSaved = false;
                throw;
            }
            // In a persistent world the companies keep going: their Legacy level grows now
            await RunAsync(world =>
            {
                foreach (var (user, total) in legacy)
                {
                    if (world.CompanyOfUser(user) is { } company)
                    {
                        company.LegacyLevel = GameWorld.LegacyLevelOf(total);
                        company.Touch();
                    }
                }
            });
            _saveSoon = true;
            _logger.LogInformation("Season {Season} ended: Legacy for {Players} players", season, results.Count);
        }
        if (ended is { } end && Now >= end + GameViews.SeasonBreak)
        {
            await _lock.WaitAsync();
            try
            {
                // Someone else may have started the next chapter meanwhile
                if (_world!.SeasonEndedAt != end)
                {
                    return;
                }
                _world.StartNextChapter();
                _store.ResultsSaved = false;
                _saveSoon = true;
            }
            finally
            {
                _lock.Release();
            }
            _logger.LogInformation("Chapter {Season} begins", season + 1);
            await _hub.Clients.All.SeasonChanged();
        }
    }

    /// <summary>Simulates the world up to now and sends what changed</summary>
    public Task TickAsync() => RunAsync(world =>
    {
        foreach (var userId in _connectedUsers.OnlineUsers)
        {
            if (world.CompanyOfUser(userId) is { } company)
            {
                company.LastSeenAt = Now;
            }
        }
        world.Tick();
    }, changesWorld: false);

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_world != null)
        {
            await SaveAsync(force: true);
            _logger.LogInformation("Saved the world");
        }
    }

    /// <summary>Saves after commands (within a tick) and at least every SaveSeconds</summary>
    private async Task SaveAsync(bool force)
    {
        if (!force && !_saveSoon && (Now - _lastSave).TotalSeconds < _options.SaveSeconds)
        {
            return;
        }
        await _saveLock.WaitAsync();
        try
        {
            WorldSnapshot snapshot;
            await _lock.WaitAsync();
            try
            {
                snapshot = _store.TakeSnapshot(_world!);
                _saveSoon = false;
                _lastSave = Now;
            }
            finally
            {
                _lock.Release();
            }
            try
            {
                await _store.SaveAsync(snapshot);
            }
            catch (Exception ex)
            {
                _saveSoon = true;
                _logger.LogError(ex, "Could not save the world");
            }
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <summary>Builds the messages for everything that changed (while the world is locked) to send them afterwards</summary>
    private List<Func<Task>> CollectPushes(GameWorld world)
    {
        var changes = world.TakeChanges();
        _playtestLog?.Write(world, changes.Events);
        var pushes = new List<Func<Task>>();
        if (changes.IsEmpty)
        {
            return pushes;
        }

        var chunks = changes.Tiles.Select(t => WorldLayout.ChunkOf(t.X, t.Y)).ToHashSet();
        // A rebuilt network has a new id: the chunks of all its pipes and buildings go out again with it (also when
        // none of their tiles changed), else the clients keep the old id on most of its pipes
        var rebuilt = changes.Networks.ToHashSet();
        foreach (var network in world.Networks.Where(t => rebuilt.Contains(t.Id)))
        {
            foreach (var tile in network.Conductors)
            {
                chunks.Add(WorldLayout.ChunkOf(tile.X, tile.Y));
            }
            foreach (var building in network.Buildings.Select(world.Building).OfType<Building>())
            {
                chunks.Add(WorldLayout.ChunkOf(building.X, building.Y));
            }
        }
        foreach (var (cx, cy) in chunks)
        {
            var objects = GameViews.ChunkObjects(world, cx, cy);
            pushes.Add(() => _hub.Clients.Group(GameHub.ChunkGroup(cx, cy)).ChunkChanged(objects));
        }

        if (changes.Networks.Count != 0)
        {
            var ids = changes.Networks.ToList();
            pushes.Add(() => _hub.Clients.All.NetworksChanged(ids));
        }

        if (changes.Exchange)
        {
            pushes.Add(() => _hub.Clients.All.ExchangeChanged());
        }

        if (changes.Wonders)
        {
            pushes.Add(() => _hub.Clients.All.WondersChanged());
        }

        foreach (var district in changes.Markets)
        {
            pushes.Add(() => _hub.Clients.All.MarketChanged(district.X, district.Y));
        }

        foreach (var district in changes.Estates.Select(t => t.District).Concat(changes.Districts).Distinct())
        {
            var dto = GameViews.District(world, district);
            pushes.Add(() => _hub.Clients.All.DistrictChanged(dto));
        }

        var online = _connectedUsers.OnlineUsers.ToHashSet();
        var companies = changes.Companies.Select(world.Company).OfType<Company>().Where(t => online.Contains(t.UserId)).ToList();
        if (companies.Count != 0)
        {
            var gold = world.GoldPerMinute();
            var research = world.ResearchPerMinute();
            foreach (var company in companies)
            {
                var dto = GameViews.Company(world, company, gold.GetValueOrDefault(company.Id), research.GetValueOrDefault(company.Id));
                pushes.Add(() => _hub.Clients.Group(GameHub.UserGroup(company.UserId)).CompanyChanged(dto));
            }
        }

        var notifications = new List<(long Company, string Kind, string Text)>();
        foreach (var worldEvent in changes.Events)
        {
            if (world.Company(worldEvent.Company) is not { IsNpc: false } company)
            {
                continue;
            }
            if (online.Contains(company.UserId))
            {
                var toast = GameViews.Toast(worldEvent);
                pushes.Add(() => _hub.Clients.Group(GameHub.UserGroup(company.UserId)).Toast(toast));
            }
            if (worldEvent.Kind != WorldEventKind.ConstructionFinished)
            {
                notifications.Add((company.Id, GameViews.Camel(worldEvent.Kind), worldEvent.Text));
            }
        }
        if (notifications.Count != 0)
        {
            var users = notifications.Select(t => t.Company).Distinct().ToDictionary(t => t, t => world.Company(t)!.UserId);
            pushes.Add(() => NotifyAsync(notifications, users, online));
        }

        var corporationUsers = changes.Corporations
            .Select(world.Corporation).OfType<Corporation>()
            .SelectMany(world.Members)
            .Concat(changes.Corporations.SelectMany(id => world.Corporation(id) is { } corporation
                ? corporation.Applications.Concat(corporation.Invites).Select(world.Company).OfType<Company>()
                : []))
            .Select(t => t.UserId)
            .Where(online.Contains)
            .Distinct()
            .ToList();
        foreach (var userId in corporationUsers)
        {
            pushes.Add(() => _hub.Clients.Group(GameHub.UserGroup(userId)).CorporationChanged());
        }

        // Crews: their members, and who asked to join
        var crewUsers = changes.Crews
            .SelectMany(id => world.Crew(id) is { } crew ? world.CrewMembers(crew).Concat(crew.JoinRequests.Select(world.Company).OfType<Company>()) : [])
            .Select(t => t.UserId)
            .Where(online.Contains)
            .Distinct()
            .ToList();
        foreach (var userId in crewUsers)
        {
            pushes.Add(() => _hub.Clients.Group(GameHub.UserGroup(userId)).CrewChanged());
        }

        // Links: both ends hear about it, and every map draws them again
        foreach (var userId in changes.Links.Select(world.Company).OfType<Company>().Select(t => t.UserId).Where(online.Contains).Distinct().ToList())
        {
            pushes.Add(() => _hub.Clients.Group(GameHub.UserGroup(userId)).LinksChanged());
        }
        if (changes.Links.Count > 0)
        {
            pushes.Add(() => _hub.Clients.All.MapLinksChanged());
        }
        return pushes;
    }

    /// <summary>Puts messages in inboxes (outside the world lock) and tells the online owners</summary>
    public async Task NotifyAsync(IReadOnlyList<(long Company, string Kind, string Text)> messages)
    {
        var users = await RunAsync(world => messages.Select(t => t.Company).Distinct()
            .Select(world.Company).OfType<Company>().ToDictionary(t => t.Id, t => t.UserId), changesWorld: false);
        await NotifyAsync(messages.Where(t => users.ContainsKey(t.Company)).ToList(), users, _connectedUsers.OnlineUsers.ToHashSet());
    }

    private async Task NotifyAsync(IReadOnlyList<(long Company, string Kind, string Text)> messages, Dictionary<long, long> users, HashSet<long> online)
    {
        var companies = await _social.NotifyAsync(messages);
        foreach (var company in companies)
        {
            if (users.TryGetValue(company, out var userId) && online.Contains(userId))
            {
                var unread = await _social.UnreadAsync(company);
                await _hub.Clients.Group(GameHub.UserGroup(userId)).InboxChanged(unread);
            }
        }
    }

    private async Task SendAsync(List<Func<Task>> pushes)
    {
        foreach (var push in pushes)
        {
            try
            {
                await push();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not send an update to the clients");
            }
        }
    }
}
