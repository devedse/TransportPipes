using System.Diagnostics;
using System.Runtime.CompilerServices;
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
    private readonly HistoryStore _history;
    private DateTime _lastPrune;

    public WorldHost(WorldStore store, HistoryStore history, IHubContext<GameHub, IGameClient> hub, ConnectedUsers connectedUsers, SocialService social, AccountService accounts, IOptions<TransportPipesOptions> options, ILogger<WorldHost> logger, TimeProvider time, PlaytestLog? playtestLog = null)
    {
        _playtestLog = playtestLog;
        _store = store;
        _history = history;
        _hub = hub;
        _connectedUsers = connectedUsers;
        _social = social;
        _accounts = accounts;
        _options = options.Value;
        _logger = logger;
        _time = time;
    }

    public Task Ready => _ready.Task;

    public DateTime Now => _time.GetUtcNow().UtcDateTime;

    private readonly PlaytestLog? _playtestLog;

    /// <summary>Saves the world now (the playtest director, before a snapshot of the database)</summary>
    public async Task SaveNowAsync()
    {
        await Ready;
        await SaveAsync(force: true);
    }

    /// <summary>Runs something on the world (a command or a query) and sends the changes it caused to the clients</summary>
    /// <param name="what">What runs, for the log of slow ones (the caller's name)</param>
    public async Task<T> RunAsync<T>(Func<GameWorld, T> action, bool changesWorld = true, [CallerMemberName] string what = "")
    {
        await Ready;
        List<Func<Task>> pushes;
        T result;
        var clock = Stopwatch.StartNew();
        await _lock.WaitAsync();
        var waited = clock.Elapsed;
        TimeSpan ran;
        try
        {
            result = action(_world!);
            ran = clock.Elapsed - waited;
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
        var held = clock.Elapsed;
        await SendAsync(pushes);
        if (clock.Elapsed.TotalMilliseconds >= _options.SlowWorldMilliseconds)
        {
            _logger.LogWarning("Slow {What}: {Total:F0} ms ({Waited:F0} waiting for the world, {Ran:F0} running, {Collected:F0} collecting {Pushes} updates, {Sent:F0} sending them)",
                what, clock.Elapsed.TotalMilliseconds, waited.TotalMilliseconds, ran.TotalMilliseconds, (held - waited - ran).TotalMilliseconds, pushes.Count, (clock.Elapsed - held).TotalMilliseconds);
        }
        return result;
    }

    public Task RunAsync(Action<GameWorld> action, bool changesWorld = true, [CallerMemberName] string what = "") => RunAsync(world =>
    {
        action(world);
        return true;
    }, changesWorld, what);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var world = await LoadWorldAsync(stoppingToken);
            var away = Now - world.LastTick;
            world.Tick();
            world.TakeChanges();
            _world = world;
            _lastSave = Now;
            _logger.LogInformation("Loaded the world (seed {Seed}, {Companies} companies, {Buildings} buildings) and caught up {Away} in {Elapsed} ms",
                world.Generator.Seed, world.Companies.Count(), world.Buildings.Count(), away, stopwatch.ElapsedMilliseconds);
            await SaveAsync(force: true);
            _ready.SetResult();
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
                    await PruneHistoryAsync();
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
        var world = await _store.LoadAsync(GameContent.Default, () => Now, _options.WorldSeed, cancellationToken, keepOlder: _options.Playtest);
        if (_options.StartingGold is { } startingGold)
        {
            world.StartingGold = startingGold;
        }
        if (_store.WorldReset)
        {
            _logger.LogWarning("The saved world was made with older rules: it was thrown away and a new world starts (accounts stay)");
        }
        world.WonderBuysEnabled = true;
        world.EventsEnabled = _options.Events;
        world.TutorialEnabled = _options.Tutorial;
        return world;
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

    /// <summary>Thins the statistics' old samples once an hour (docs/design-notes/statistics.md)</summary>
    private async Task PruneHistoryAsync()
    {
        if (Now - _lastPrune < TimeSpan.FromHours(1))
        {
            return;
        }
        _lastPrune = Now;
        await _history.PruneAsync(Now);
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
            var clock = Stopwatch.StartNew();
            await _lock.WaitAsync();
            var waited = clock.Elapsed;
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
            var held = clock.Elapsed - waited;
            try
            {
                await _store.SaveAsync(snapshot);
                if (clock.Elapsed.TotalMilliseconds >= _options.SlowWorldMilliseconds)
                {
                    _logger.LogWarning("Slow save: {Total:F0} ms ({Waited:F0} waiting for the world, {Held:F0} taking the snapshot with the world held, {Written:F0} writing it)",
                        clock.Elapsed.TotalMilliseconds, waited.TotalMilliseconds, held.TotalMilliseconds, (clock.Elapsed - waited - held).TotalMilliseconds);
                }
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

        if (changes.YardRound)
        {
            var next = world.NextYardRound;
            pushes.Add(() => _hub.Clients.All.YardRound(next));
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
            foreach (var company in companies)
            {
                var dto = GameViews.Company(world, company, gold.GetValueOrDefault(company.Id));
                pushes.Add(() => _hub.Clients.Group(GameHub.UserGroup(company.UserId)).CompanyChanged(dto));
            }
        }

        // A town's Launch: Legacy for every player's account (1.4 §13)
        if (changes.Legacy.Count > 0)
        {
            var legacy = changes.Legacy.ToList();
            pushes.Add(() => _accounts.AddLegacyAsync(legacy));
        }

        var notifications = new List<(long Company, string Kind, string Text)>();
        foreach (var worldEvent in changes.Events)
        {
            if (world.Company(worldEvent.Company) is not { } company)
            {
                continue;
            }
            if (online.Contains(company.UserId))
            {
                var toast = GameViews.Toast(worldEvent);
                pushes.Add(() => _hub.Clients.Group(GameHub.UserGroup(company.UserId)).Toast(toast));
            }
            if (worldEvent.InBell)
            {
                notifications.Add((company.Id, GameViews.Camel(worldEvent.Kind), worldEvent.Text));
            }
        }
        if (notifications.Count != 0)
        {
            var users = notifications.Select(t => t.Company).Distinct().ToDictionary(t => t, t => world.Company(t)!.UserId);
            pushes.Add(() => NotifyAsync(notifications, users, online));
        }

        // Crews: their residents, and who asked to settle
        var crewUsers = changes.Crews
            .SelectMany(id => world.Crew(id) is { } crew ? world.CrewMembers(crew).Concat(crew.Requests.Select(t => world.Company(t.Company)).OfType<Company>()) : [])
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
