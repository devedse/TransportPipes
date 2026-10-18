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
    private readonly TransportPipesOptions _options;
    private readonly ILogger<WorldHost> _logger;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private GameWorld? _world;
    private bool _saveSoon;
    private DateTime _lastSave;

    public WorldHost(WorldStore store, IHubContext<GameHub, IGameClient> hub, ConnectedUsers connectedUsers, IOptions<TransportPipesOptions> options, ILogger<WorldHost> logger, TimeProvider time)
    {
        _store = store;
        _hub = hub;
        _connectedUsers = connectedUsers;
        _options = options.Value;
        _logger = logger;
        _time = time;
    }

    public Task Ready => _ready.Task;

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

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
            var world = await _store.LoadAsync(GameContent.Default, () => Now, _options.WorldSeed, stoppingToken);
            if (_options.StartingGold is { } startingGold)
            {
                world.StartingGold = startingGold;
            }
            world.TenantsEnabled = _options.Tenants;
            if (_options.Tenants)
            {
                world.EnsureTenants();
            }
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

    /// <summary>Builds the messages for everything that changed (while the world is locked) to send them afterwards</summary>
    private List<Func<Task>> CollectPushes(GameWorld world)
    {
        var changes = world.TakeChanges();
        var pushes = new List<Func<Task>>();
        if (changes.IsEmpty)
        {
            return pushes;
        }

        var chunks = changes.Tiles.Select(t => WorldLayout.ChunkOf(t.X, t.Y)).Distinct().ToList();
        if (chunks.Count != 0)
        {
            // Networks change when tiles change, so the network ids of the objects too
            foreach (var networkId in changes.Networks)
            {
                if (world.Networks.FirstOrDefault(t => t.Id == networkId) is { } network)
                {
                    foreach (var tile in network.Conductors)
                    {
                        var chunk = WorldLayout.ChunkOf(tile.X, tile.Y);
                        if (!chunks.Contains(chunk))
                        {
                            chunks.Add(chunk);
                        }
                    }
                }
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

        foreach (var district in changes.Markets)
        {
            pushes.Add(() => _hub.Clients.All.MarketChanged(district.X, district.Y));
        }

        foreach (var district in changes.Estates.Select(t => t.District).Distinct())
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

        foreach (var worldEvent in changes.Events)
        {
            if (world.Company(worldEvent.Company) is { } company && online.Contains(company.UserId))
            {
                var toast = GameViews.Toast(worldEvent);
                pushes.Add(() => _hub.Clients.Group(GameHub.UserGroup(company.UserId)).Toast(toast));
            }
        }
        return pushes;
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
