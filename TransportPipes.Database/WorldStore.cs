using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TransportPipes.Database.Entities;
using TransportPipes.Game.Content;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Database;

/// <summary>The changed part of the world, copied while the world was locked, so it can be saved without the lock</summary>
public sealed class WorldSnapshot
{
    internal DbWorld World { get; init; } = new();
    internal List<(Entity Source, object Row)> Upserts { get; } = new();
    internal List<object> Deletes { get; } = new();
    internal List<DbTerrainOverride> TerrainOverrides { get; } = new();

    public int Count => Upserts.Count + Deletes.Count;
}

/// <summary>
/// Loads the game world from the database and saves what changed. The world is the truth, the database is a snapshot of
/// it; the store remembers which rows exist so it knows whether to insert or update.
/// </summary>
public sealed class WorldStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly TransportPipesDbFactory _dbFactory;
    private readonly HashSet<long> _companies = new();
    private readonly HashSet<(int, int, int)> _estates = new();
    private readonly HashSet<long> _buildings = new();
    private readonly HashSet<TilePos> _conductors = new();
    private readonly Dictionary<TilePos, TerrainType> _terrainOverrides = new();
    private readonly List<object> _orphans = new();
    private bool _worldExists;

    public WorldStore(TransportPipesDbFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<GameWorld> LoadAsync(GameContent content, Func<DateTime> clock, int newWorldSeed, CancellationToken cancellationToken = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var dbWorld = await db.Worlds.SingleOrDefaultAsync(cancellationToken);
        _worldExists = dbWorld != null;
        dbWorld ??= new DbWorld { Seed = newWorldSeed, CreatedAt = clock(), LastTick = clock() };

        var world = new GameWorld(content, dbWorld.Seed, clock) { Era = dbWorld.Era };

        foreach (var row in await db.Companies.ToListAsync(cancellationToken))
        {
            var settings = JsonSerializer.Deserialize<CompanySettings>(row.Settings, Json) ?? new CompanySettings();
            world.LoadCompany(new Company
            {
                Id = row.Id,
                UserId = row.UserId,
                Name = row.Name,
                Color = row.Color,
                Gold = row.Gold,
                Xp = row.Xp,
                Level = row.Level,
                ResearchPoints = row.ResearchPoints,
                LifetimeEarnings = row.LifetimeEarnings,
                CreatedAt = row.CreatedAt,
                LastSeenAt = row.LastSeenAt,
                IsNpc = row.IsNpc,
                District = new DistrictCoord(row.DistrictX, row.DistrictY),
                Slot = row.Slot,
                SellPrices = settings.SellPrices,
                BuyMaxPrices = settings.BuyMaxPrices,
                NoTrade = settings.NoTrade,
                BuiltTypes = settings.BuiltTypes,
                Researched = settings.Researched,
                CurrentResearch = settings.CurrentResearch,
                ResearchEndsAt = settings.ResearchEndsAt,
                PendingPrices = settings.PendingPrices,
                HubReputation = settings.HubReputation,
                DailyOrders = settings.DailyOrders,
                Report = JsonSerializer.Deserialize<CompanyReport>(row.Report, Json) ?? new CompanyReport { Since = row.LastSeenAt },
            });
            _companies.Add(row.Id);
        }

        foreach (var row in await db.Estates.ToListAsync(cancellationToken))
        {
            var district = new DistrictCoord(row.DistrictX, row.DistrictY);
            world.LoadEstate(new Estate
            {
                District = district,
                Slot = row.Slot,
                Origin = WorldLayout.EstateOrigin(district, row.Slot),
                OwnerId = row.OwnerId,
                UnlockedParcels = row.UnlockedParcels,
                GatePolicies = JsonSerializer.Deserialize<Dictionary<GateSide, GatePolicy>>(row.GatePolicies, Json) ?? new(),
            });
            _estates.Add((row.DistrictX, row.DistrictY, row.Slot));
        }

        foreach (var row in await db.TerrainOverrides.ToListAsync(cancellationToken))
        {
            world.LoadTerrainOverride(new TilePos(row.X, row.Y), (TerrainType)row.Terrain);
            _terrainOverrides[new TilePos(row.X, row.Y)] = (TerrainType)row.Terrain;
        }

        foreach (var row in await db.Buildings.ToListAsync(cancellationToken))
        {
            _buildings.Add(row.Id);
            if (!content.Buildings.ContainsKey(row.Type))
            {
                // A building that was removed from the content files: deleted on the next save
                _orphans.Add(new DbBuilding { Id = row.Id });
                continue;
            }
            world.LoadBuilding(new Building
            {
                Id = row.Id,
                Type = row.Type,
                Owner = row.OwnerId,
                X = row.X,
                Y = row.Y,
                Size = row.Size,
                Level = row.Level,
                ConstructionEndsAt = row.ConstructionEndsAt,
                Contents = JsonSerializer.Deserialize<Dictionary<string, double>>(row.Contents, Json) ?? new(),
                Imports = JsonSerializer.Deserialize<HashSet<string>>(row.Imports, Json) ?? new(),
                Filter = JsonSerializer.Deserialize<HashSet<string>>(row.Filter, Json) ?? new(),
                Setting = row.Setting,
            });
        }

        foreach (var row in await db.Conductors.ToListAsync(cancellationToken))
        {
            world.LoadConductor(new Conductor
            {
                Pos = new TilePos(row.X, row.Y),
                Kind = (ConductorKind)row.Kind,
                Tier = row.Tier,
                Owner = row.OwnerId,
                Side = row.Side is { } side ? (GateSide)side : null,
            });
            _conductors.Add(new TilePos(row.X, row.Y));
        }

        foreach (var market in JsonSerializer.Deserialize<List<MarketRow>>(dbWorld.Markets, Json) ?? [])
        {
            world.LoadMarket(new DistrictCoord(market.X, market.Y), market.Market);
        }

        world.LastTick = dbWorld.LastTick;
        world.TakeChanges();
        world.Deleted.Clear();
        return world;
    }

    /// <summary>Copies everything that changed (call it while the world is locked)</summary>
    public WorldSnapshot TakeSnapshot(GameWorld world)
    {
        var snapshot = new WorldSnapshot
        {
            World = new DbWorld
            {
                Seed = world.Generator.Seed,
                Era = world.Era,
                LastTick = world.LastTick,
                CreatedAt = world.Now,
                Markets = JsonSerializer.Serialize(world.Markets.Select(t => new MarketRow(t.Key.X, t.Key.Y, t.Value)).ToList(), Json),
            },
        };

        foreach (var company in world.Companies.Where(t => t.Dirty))
        {
            snapshot.Upserts.Add((company, new DbCompany
            {
                Id = company.Id,
                UserId = company.UserId,
                Name = company.Name,
                Color = company.Color,
                Gold = company.Gold,
                Xp = company.Xp,
                Level = company.Level,
                ResearchPoints = company.ResearchPoints,
                LifetimeEarnings = company.LifetimeEarnings,
                CreatedAt = company.CreatedAt,
                LastSeenAt = company.LastSeenAt,
                IsNpc = company.IsNpc,
                DistrictX = company.District.X,
                DistrictY = company.District.Y,
                Slot = company.Slot,
                Settings = JsonSerializer.Serialize(new CompanySettings
                {
                    SellPrices = company.SellPrices,
                    BuyMaxPrices = company.BuyMaxPrices,
                    NoTrade = company.NoTrade,
                    BuiltTypes = company.BuiltTypes,
                    Researched = company.Researched,
                    CurrentResearch = company.CurrentResearch,
                    ResearchEndsAt = company.ResearchEndsAt,
                    PendingPrices = company.PendingPrices,
                    HubReputation = company.HubReputation,
                    DailyOrders = company.DailyOrders,
                }, Json),
                Report = JsonSerializer.Serialize(company.Report, Json),
            }));
        }

        foreach (var estate in world.Estates.Where(t => t.Dirty))
        {
            snapshot.Upserts.Add((estate, new DbEstate
            {
                DistrictX = estate.District.X,
                DistrictY = estate.District.Y,
                Slot = estate.Slot,
                OwnerId = estate.OwnerId,
                UnlockedParcels = estate.UnlockedParcels,
                GatePolicies = JsonSerializer.Serialize(estate.GatePolicies, Json),
            }));
        }

        foreach (var building in world.Buildings.Where(t => t.Dirty))
        {
            snapshot.Upserts.Add((building, new DbBuilding
            {
                Id = building.Id,
                Type = building.Type,
                OwnerId = building.Owner,
                X = building.X,
                Y = building.Y,
                Size = building.Size,
                Level = building.Level,
                ConstructionEndsAt = building.ConstructionEndsAt,
                Contents = JsonSerializer.Serialize(building.Contents, Json),
                Imports = JsonSerializer.Serialize(building.Imports, Json),
                Filter = JsonSerializer.Serialize(building.Filter, Json),
                Setting = building.Setting,
            }));
        }

        foreach (var conductor in world.Conductors.Where(t => t.Dirty))
        {
            snapshot.Upserts.Add((conductor, new DbConductor
            {
                X = conductor.Pos.X,
                Y = conductor.Pos.Y,
                Kind = (int)conductor.Kind,
                Tier = conductor.Tier,
                OwnerId = conductor.Owner,
                Side = conductor.Side is { } side ? (int)side : null,
            }));
        }

        foreach (var (pos, terrain) in world.TerrainOverrides)
        {
            if (!_terrainOverrides.TryGetValue(pos, out var saved) || saved != terrain)
            {
                snapshot.TerrainOverrides.Add(new DbTerrainOverride { X = pos.X, Y = pos.Y, Terrain = (int)terrain });
            }
        }

        snapshot.Deletes.AddRange(_orphans);
        _orphans.Clear();
        foreach (var deleted in world.Deleted)
        {
            switch (deleted)
            {
                case Building building:
                    snapshot.Deletes.Add(new DbBuilding { Id = building.Id });
                    break;
                // A pipe that was removed and laid again is just an update
                case Conductor conductor when world.ConductorAt(conductor.Pos) == null:
                    snapshot.Deletes.Add(new DbConductor { X = conductor.Pos.X, Y = conductor.Pos.Y });
                    break;
            }
        }
        world.Deleted.Clear();

        foreach (var (source, _) in snapshot.Upserts)
        {
            source.Dirty = false;
        }
        return snapshot;
    }

    /// <summary>Writes a snapshot in one transaction. When it fails the entities are marked dirty again.</summary>
    public async Task SaveAsync(WorldSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await using var db = _dbFactory.CreateDbContext();
        var newCompanies = new List<long>();
        var newEstates = new List<(int, int, int)>();
        var newBuildings = new List<long>();
        var newConductors = new List<TilePos>();
        try
        {
            if (_worldExists)
            {
                db.Worlds.Attach(snapshot.World);
                db.Entry(snapshot.World).Property(t => t.Era).IsModified = true;
                db.Entry(snapshot.World).Property(t => t.LastTick).IsModified = true;
                db.Entry(snapshot.World).Property(t => t.Markets).IsModified = true;
            }
            else
            {
                db.Worlds.Add(snapshot.World);
            }

            foreach (var row in snapshot.Deletes)
            {
                switch (row)
                {
                    case DbBuilding building when _buildings.Contains(building.Id):
                        db.Buildings.Remove(building);
                        break;
                    case DbConductor conductor when _conductors.Contains(new TilePos(conductor.X, conductor.Y)):
                        db.Conductors.Remove(conductor);
                        break;
                }
            }

            foreach (var (_, row) in snapshot.Upserts)
            {
                switch (row)
                {
                    case DbCompany company:
                        Upsert(db, company, _companies.Contains(company.Id), () => newCompanies.Add(company.Id));
                        break;
                    case DbEstate estate:
                        var key = (estate.DistrictX, estate.DistrictY, estate.Slot);
                        Upsert(db, estate, _estates.Contains(key), () => newEstates.Add(key));
                        break;
                    case DbBuilding building:
                        Upsert(db, building, _buildings.Contains(building.Id), () => newBuildings.Add(building.Id));
                        break;
                    case DbConductor conductor:
                        var pos = new TilePos(conductor.X, conductor.Y);
                        Upsert(db, conductor, _conductors.Contains(pos), () => newConductors.Add(pos));
                        break;
                }
            }

            foreach (var row in snapshot.TerrainOverrides)
            {
                Upsert(db, row, _terrainOverrides.ContainsKey(new TilePos(row.X, row.Y)), () => { });
            }
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var (source, _) in snapshot.Upserts)
            {
                source.Dirty = true;
            }
            throw;
        }

        _worldExists = true;
        _companies.UnionWith(newCompanies);
        _estates.UnionWith(newEstates);
        _buildings.UnionWith(newBuildings);
        _conductors.UnionWith(newConductors);
        foreach (var row in snapshot.Deletes)
        {
            switch (row)
            {
                case DbBuilding building:
                    _buildings.Remove(building.Id);
                    break;
                case DbConductor conductor:
                    _conductors.Remove(new TilePos(conductor.X, conductor.Y));
                    break;
            }
        }
        foreach (var row in snapshot.TerrainOverrides)
        {
            _terrainOverrides[new TilePos(row.X, row.Y)] = (TerrainType)row.Terrain;
        }
    }

    private static void Upsert<T>(TransportPipesDbContext db, T row, bool exists, Action added) where T : class
    {
        if (exists)
        {
            db.Update(row);
        }
        else
        {
            db.Add(row);
            added();
        }
    }

    private sealed class CompanySettings
    {
        public Dictionary<string, double> SellPrices { get; init; } = new();
        public Dictionary<string, double> BuyMaxPrices { get; init; } = new();
        public HashSet<string> NoTrade { get; init; } = new();
        public HashSet<string> BuiltTypes { get; init; } = new();
        public HashSet<string> Researched { get; init; } = new();
        public string? CurrentResearch { get; init; }
        public DateTime? ResearchEndsAt { get; init; }
        public Dictionary<string, PendingPrice> PendingPrices { get; init; } = new();
        public double HubReputation { get; init; }
        public List<DailyOrder> DailyOrders { get; init; } = new();
    }

    private sealed record MarketRow(int X, int Y, HubMarket Market);
}
