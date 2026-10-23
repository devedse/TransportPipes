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
    private readonly HashSet<long> _contracts = new();
    private readonly HashSet<long> _corporations = new();
    private readonly HashSet<TilePos> _conductors = new();
    private readonly Dictionary<TilePos, TerrainType> _terrainOverrides = new();
    private readonly List<object> _orphans = new();
    private bool _worldExists;

    /// <summary>The rules the loaded world was made with (a new world: today's)</summary>
    private int _worldRules = CurrentRules;

    public WorldStore(TransportPipesDbFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// The version of the rules: a world saved with older rules is thrown away when the server starts (v1.2 changed how
    /// players play together so much that the owner chose a fresh world); accounts stay
    /// </summary>
    public const int CurrentRules = 3;

    /// <summary>The first rules with dealt land (step 5 of the redesign): older worlds keep the land they were made with</summary>
    public const int DealtLandRules = 3;

    /// <summary>Whether the last load threw an old world away</summary>
    public bool WorldReset { get; private set; }

    /// <param name="keepOlder">The playtest server: a world made with older rules stays as it was (run 1's snapshots replay on their own land)</param>
    public async Task<GameWorld> LoadAsync(GameContent content, Func<DateTime> clock, int newWorldSeed, CancellationToken cancellationToken = default, bool keepOlder = false)
    {
        await using var db = _dbFactory.CreateDbContext();
        var dbWorld = await db.Worlds.SingleOrDefaultAsync(cancellationToken);
        if (dbWorld != null && dbWorld.Rules < CurrentRules && !keepOlder)
        {
            await ResetWorldAsync(db, cancellationToken);
            dbWorld = null;
            WorldReset = true;
        }
        _worldExists = dbWorld != null;
        dbWorld ??= new DbWorld { Seed = newWorldSeed, CreatedAt = clock(), LastTick = clock() };

        _worldRules = dbWorld.Rules == 0 ? CurrentRules : dbWorld.Rules;
        var world = new GameWorld(content, dbWorld.Seed, clock, dealtLand: _worldRules >= DealtLandRules) { Era = dbWorld.Era };

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
                HubMarkets = settings.HubMarkets,
                NoTrade = settings.NoTrade,
                NoBuy = settings.NoBuy,
                TradeBlocked = settings.TradeBlocked,
                PendingBlocks = settings.PendingBlocks,
                CrewId = settings.CrewId,
                LeftCrewAt = settings.LeftCrewAt,
                IsGuest = settings.IsGuest,
                House = settings.House,
                StandIn = settings.StandIn,
                StandInAnnounced = settings.StandInAnnounced,
                KnownAs = settings.KnownAs,
                SeatedNextDoor = settings.SeatedNextDoor,
                YardSalesExplained = settings.YardSalesExplained,
                KeepResearching = settings.KeepResearching,
                HoldUntil = settings.HoldUntil,
                KeepAsked = settings.KeepAsked,
                TendedBy = settings.TendedBy ?? new Dictionary<long, int>(),
                TendedUntil = settings.TendedUntil,
                LastSeat = settings.LastSeat,
                LoneDays = settings.LoneDays,
                LoneCardAt = settings.LoneCardAt,
                LoneCard = settings.LoneCard,
                ProbationUntil = settings.ProbationUntil,
                ProjectFirst = settings.ProjectFirst ?? new HashSet<string>(),
                Purchases = settings.Purchases ?? new List<Purchase>(),
                EraEnded = settings.EraEnded,
                Refinements = settings.Refinements ?? new Dictionary<string, int>(),
                RefineChoice = settings.RefineChoice,
                Sends = settings.Sends ?? new List<GoodSend>(),
                ThankedOn = settings.ThankedOn ?? new Dictionary<long, int>(),
                GiftsAnnounced = settings.GiftsAnnounced ?? new HashSet<string>(),
                PinMakes = settings.PinMakes ?? new HashSet<string>(),
                PinNeeds = settings.PinNeeds ?? new HashSet<string>(),
                Asks = settings.Asks ?? new List<CompanyAsk>(),
                KitBuildings = settings.KitBuildings ?? new HashSet<long>(),
                TutorialStepSince = settings.TutorialStepSince,
                Devices = settings.Devices ?? new Dictionary<string, DateTime>(),
                MeasuredIncome = settings.MeasuredIncome,
                MeasuredEarned = settings.MeasuredEarned,
                UsualTrades = settings.UsualTrades ?? new Dictionary<string, UsualTrade>(),
                League = settings.League,
                EraOutput = settings.EraOutput,
                DistinctDays = settings.DistinctDays,
                LastPlayedDay = settings.LastPlayedDay,
                TradeWith = settings.TradeWith,
                BuiltTypes = settings.BuiltTypes,
                XpLog = settings.XpLog,
                Researched = settings.Researched,
                CurrentResearch = settings.CurrentResearch,
                ResearchQueue = settings.ResearchQueue ?? new List<string>(),
                ResearchEndsAt = settings.ResearchEndsAt,
                ContractScore = settings.ContractScore,
                ContractsCompleted = settings.ContractsCompleted,
                CorporationId = settings.CorporationId,
                Role = settings.Role,
                LeftCorporationAt = settings.LeftCorporationAt,
                MentorId = settings.MentorId,
                MentorRewards = settings.MentorRewards,
                WeekStartEarnings = settings.WeekStartEarnings,
                Likes = settings.Likes,
                Shares = settings.Shares,
                FranchiseAt = settings.FranchiseAt,
                Branches = settings.Branches,
                Franchises = settings.Franchises,
                EarningsAtFranchise = settings.EarningsAtFranchise,
                LegacyLevel = settings.LegacyLevel,
                Packed = settings.Packed,
                Crates = settings.Crates,
                Asleep = settings.Asleep,
                ArrivesAt = settings.ArrivesAt,
                LastMovedAt = settings.LastMovedAt,
                FreeMoves = settings.FreeMoves,
                MergeBonus = settings.MergeBonus,
                DeepParcels = settings.DeepParcels,
                TutorialStep = settings.TutorialStep,
                Expedition = settings.Expedition,
                ExpeditionLog = settings.ExpeditionLog,
                Relics = settings.Relics,
                Mastery = settings.Mastery,
                DigParcel = settings.DigParcel,
                DigStartedAt = settings.DigStartedAt,
                DigEndsAt = settings.DigEndsAt,
                SwapWith = settings.SwapWith,
                Titles = settings.Titles,
                Managers = settings.Managers,
                Achievements = settings.Achievements,
                Stats = settings.Stats,
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
                ReservedFor = row.ReservedFor,
                ReservedUntil = row.ReservedUntil,
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
                Damaged = row.Damaged,
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
                Widen = Math.Max(1, row.Widen),
            });
            _conductors.Add(new TilePos(row.X, row.Y));
        }

        foreach (var row in await db.Corporations.ToListAsync(cancellationToken))
        {
            var data = JsonSerializer.Deserialize<CorporationData>(row.Data, Json);
            world.LoadCorporation(new Corporation
            {
                Id = row.Id,
                Name = row.Name,
                Tag = row.Tag,
                Color = row.Color,
                District = new DistrictCoord(row.DistrictX, row.DistrictY),
                CreatedAt = row.CreatedAt,
                Treasury = row.Treasury,
                Tax = row.Tax,
                HqLevel = row.HqLevel,
                PerkResource = row.PerkResource,
                Description = row.Description,
                Applications = data?.Applications ?? new(),
                Invites = data?.Invites ?? new(),
                Goal = data?.Goal,
            });
            _corporations.Add(row.Id);
        }

        foreach (var row in await db.Contracts.ToListAsync(cancellationToken))
        {
            world.LoadContract(new Contract
            {
                Id = row.Id,
                Proposer = row.Proposer,
                Seller = row.Seller,
                Buyer = row.Buyer,
                Resource = row.Resource,
                Rate = row.Rate,
                Price = row.Price,
                Days = row.Days,
                PenaltyRate = row.PenaltyRate,
                CreatedAt = row.CreatedAt,
                StartedAt = row.StartedAt,
                EndsAt = row.EndsAt,
                State = (ContractState)row.State,
                Delivered = row.Delivered,
                Expected = row.Expected,
            });
            _contracts.Add(row.Id);
        }

        if (JsonSerializer.Deserialize<ExchangeState>(dbWorld.Exchange, Json) is { Orders: not null } exchange)
        {
            world.LoadExchange(exchange.Orders, exchange.Shipments ?? []);
        }

        if (JsonSerializer.Deserialize<EventsState>(dbWorld.Events, Json) is { Events: not null } events)
        {
            world.LoadEvents(events.Events, events.PlannedDays ?? []);
        }

        if (JsonSerializer.Deserialize<WondersState>(dbWorld.Wonders, Json) is { } wonders)
        {
            world.LoadWonders(wonders.EraStartedAt, wonders.EraWonder, wonders.DistrictWonders ?? []);
        }

        if (JsonSerializer.Deserialize<OutpostsState>(dbWorld.Outposts, Json) is { } outposts)
        {
            world.LoadOutposts(outposts.Outposts ?? [], outposts.Discoveries ?? []);
            world.LoadSeeds((outposts.Seeds ?? []).Select(t => (new TilePos(t.X, t.Y), t.Deposit, t.Richness)));
            world.LoadSurveyed((outposts.Surveyed ?? []).Select(t => (new DistrictCoord(t.X, t.Y), t.Slot)));
        }

        world.LoadBlueprints(JsonSerializer.Deserialize<List<Blueprint>>(dbWorld.Blueprints, Json) ?? []);
        foreach (var crew in JsonSerializer.Deserialize<List<Crew>>(dbWorld.Crews, Json) ?? [])
        {
            world.LoadCrew(crew);
        }
        foreach (var link in JsonSerializer.Deserialize<List<Link>>(dbWorld.Links, Json) ?? [])
        {
            world.LoadLink(link);
        }
        foreach (var project in JsonSerializer.Deserialize<List<StationProject>>(dbWorld.Projects, Json) ?? [])
        {
            world.LoadProject(project);
        }
        foreach (var trunk in JsonSerializer.Deserialize<List<Trunk>>(dbWorld.Trunks, Json) ?? [])
        {
            world.LoadTrunk(trunk);
        }

        if (JsonSerializer.Deserialize<SeasonState>(dbWorld.Season, Json) is { } season)
        {
            world.LoadSeason(season.Season, season.FinaleAt, season.EndedAt, season.Results ?? []);
            ResultsSaved = season.ResultsSaved;
        }

        world.LastTick = dbWorld.LastTick;
        world.TakeChanges();
        world.Deleted.Clear();
        return world;
    }

    /// <summary>Throws the world away: everything but the accounts</summary>
    private static async Task ResetWorldAsync(TransportPipesDbContext db, CancellationToken cancellationToken)
    {
        await db.Blocks.ExecuteDeleteAsync(cancellationToken);
        await db.GuestbookNotes.ExecuteDeleteAsync(cancellationToken);
        await db.InboxMessages.ExecuteDeleteAsync(cancellationToken);
        await db.ChatMessages.ExecuteDeleteAsync(cancellationToken);
        await db.Contracts.ExecuteDeleteAsync(cancellationToken);
        await db.Corporations.ExecuteDeleteAsync(cancellationToken);
        await db.TerrainOverrides.ExecuteDeleteAsync(cancellationToken);
        await db.Conductors.ExecuteDeleteAsync(cancellationToken);
        await db.Buildings.ExecuteDeleteAsync(cancellationToken);
        await db.Estates.ExecuteDeleteAsync(cancellationToken);
        await db.Companies.ExecuteDeleteAsync(cancellationToken);
        await db.Worlds.ExecuteDeleteAsync(cancellationToken);
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
                Rules = _worldRules,
                Exchange = JsonSerializer.Serialize(new ExchangeState(world.ExchangeOrders.ToList(), world.Shipments.ToList()), Json),
                Events = JsonSerializer.Serialize(new EventsState(world.Events.ToList(), world.PlannedDays.ToList()), Json),
                Wonders = JsonSerializer.Serialize(new WondersState(world.EraStartedAt, world.EraWonder, world.DistrictWonders.Values.ToList()), Json),
                Outposts = JsonSerializer.Serialize(new OutpostsState(world.Outposts.ToList(), world.Discoveries.ToList(),
                    world.Seeds.Select(t => new SeedRow(t.Key.X, t.Key.Y, t.Value.Deposit, t.Value.Richness)).ToList(),
                    world.Surveyed.Select(t => new SurveyedRow(t.District.X, t.District.Y, t.Slot)).ToList()), Json),
                Season = JsonSerializer.Serialize(new SeasonState(world.Season, world.FinaleAt, world.SeasonEndedAt, world.SeasonResults, ResultsSaved), Json),
                Blueprints = JsonSerializer.Serialize(world.Blueprints.ToList(), Json),
                Crews = JsonSerializer.Serialize(world.Crews.ToList(), Json),
                Links = JsonSerializer.Serialize(world.Links.ToList(), Json),
                Projects = JsonSerializer.Serialize(world.Projects.ToList(), Json),
                Trunks = JsonSerializer.Serialize(world.Trunks.ToList(), Json),
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
                    HubMarkets = company.HubMarkets,
                    NoTrade = company.NoTrade,
                    NoBuy = company.NoBuy,
                    TradeBlocked = company.TradeBlocked,
                    PendingBlocks = company.PendingBlocks,
                    CrewId = company.CrewId,
                    LeftCrewAt = company.LeftCrewAt,
                    IsGuest = company.IsGuest,
                    House = company.House,
                    StandIn = company.StandIn,
                    StandInAnnounced = company.StandInAnnounced,
                    KnownAs = company.KnownAs,
                    SeatedNextDoor = company.SeatedNextDoor,
                    YardSalesExplained = company.YardSalesExplained,
                    KeepResearching = company.KeepResearching,
                    HoldUntil = company.HoldUntil,
                    KeepAsked = company.KeepAsked,
                    TendedBy = company.TendedBy,
                    TendedUntil = company.TendedUntil,
                    LastSeat = company.LastSeat,
                    LoneDays = company.LoneDays,
                    LoneCardAt = company.LoneCardAt,
                    LoneCard = company.LoneCard,
                    ProbationUntil = company.ProbationUntil,
                    ProjectFirst = company.ProjectFirst,
                    Purchases = company.Purchases,
                    EraEnded = company.EraEnded,
                    Refinements = company.Refinements,
                    RefineChoice = company.RefineChoice,
                    Sends = company.Sends,
                    ThankedOn = company.ThankedOn,
                    GiftsAnnounced = company.GiftsAnnounced,
                    PinMakes = company.PinMakes,
                    PinNeeds = company.PinNeeds,
                    Asks = company.Asks,
                    KitBuildings = company.KitBuildings,
                    TutorialStepSince = company.TutorialStepSince,
                    Devices = company.Devices,
                    MeasuredIncome = company.MeasuredIncome,
                    MeasuredEarned = company.MeasuredEarned,
                    UsualTrades = company.UsualTrades,
                    League = company.League,
                    EraOutput = company.EraOutput,
                    DistinctDays = company.DistinctDays,
                    LastPlayedDay = company.LastPlayedDay,
                    TradeWith = company.TradeWith,
                    BuiltTypes = company.BuiltTypes,
                    XpLog = company.XpLog,
                    Researched = company.Researched,
                    ResearchQueue = company.ResearchQueue,
                    CurrentResearch = company.CurrentResearch,
                    ResearchEndsAt = company.ResearchEndsAt,
                    ContractScore = company.ContractScore,
                    ContractsCompleted = company.ContractsCompleted,
                    CorporationId = company.CorporationId,
                    Role = company.Role,
                    LeftCorporationAt = company.LeftCorporationAt,
                    MentorId = company.MentorId,
                    MentorRewards = company.MentorRewards,
                    WeekStartEarnings = company.WeekStartEarnings,
                    Likes = company.Likes,
                    Shares = company.Shares,
                    FranchiseAt = company.FranchiseAt,
                    Branches = company.Branches,
                    Franchises = company.Franchises,
                    EarningsAtFranchise = company.EarningsAtFranchise,
                    LegacyLevel = company.LegacyLevel,
                    Packed = company.Packed,
                    Crates = company.Crates,
                    Asleep = company.Asleep,
                    ArrivesAt = company.ArrivesAt,
                    LastMovedAt = company.LastMovedAt,
                    FreeMoves = company.FreeMoves,
                    MergeBonus = company.MergeBonus,
                    DeepParcels = company.DeepParcels,
                    TutorialStep = company.TutorialStep,
                    Expedition = company.Expedition,
                    ExpeditionLog = company.ExpeditionLog,
                    Relics = company.Relics,
                    Mastery = company.Mastery,
                    DigParcel = company.DigParcel,
                    DigStartedAt = company.DigStartedAt,
                    DigEndsAt = company.DigEndsAt,
                    SwapWith = company.SwapWith,
                    Titles = company.Titles,
                    Managers = company.Managers,
                    Achievements = company.Achievements,
                    Stats = company.Stats,
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
                ReservedFor = estate.ReservedFor,
                ReservedUntil = estate.ReservedUntil,
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
                Damaged = building.Damaged,
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
                Widen = conductor.Widen,
            }));
        }

        foreach (var corporation in world.Corporations.Where(t => t.Dirty))
        {
            snapshot.Upserts.Add((corporation, new DbCorporation
            {
                Id = corporation.Id,
                Name = corporation.Name,
                Tag = corporation.Tag,
                Color = corporation.Color,
                DistrictX = corporation.District.X,
                DistrictY = corporation.District.Y,
                CreatedAt = corporation.CreatedAt,
                Treasury = corporation.Treasury,
                Tax = corporation.Tax,
                HqLevel = corporation.HqLevel,
                PerkResource = corporation.PerkResource,
                Description = corporation.Description,
                Data = JsonSerializer.Serialize(new CorporationData(corporation.Applications, corporation.Invites, corporation.Goal), Json),
            }));
        }

        foreach (var contract in world.Contracts.Where(t => t.Dirty))
        {
            snapshot.Upserts.Add((contract, new DbContract
            {
                Id = contract.Id,
                Proposer = contract.Proposer,
                Seller = contract.Seller,
                Buyer = contract.Buyer,
                Resource = contract.Resource,
                Rate = contract.Rate,
                Price = contract.Price,
                Days = contract.Days,
                PenaltyRate = contract.PenaltyRate,
                CreatedAt = contract.CreatedAt,
                StartedAt = contract.StartedAt,
                EndsAt = contract.EndsAt,
                State = (int)contract.State,
                Delivered = contract.Delivered,
                Expected = contract.Expected,
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
                case Company company:
                    snapshot.Deletes.Add(new DbCompany { Id = company.Id });
                    break;
                case Corporation corporation:
                    snapshot.Deletes.Add(new DbCorporation { Id = corporation.Id });
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
        var newContracts = new List<long>();
        var newCorporations = new List<long>();
        try
        {
            if (_worldExists)
            {
                db.Worlds.Attach(snapshot.World);
                db.Entry(snapshot.World).Property(t => t.Era).IsModified = true;
                db.Entry(snapshot.World).Property(t => t.LastTick).IsModified = true;
                db.Entry(snapshot.World).Property(t => t.Rules).IsModified = true;
                db.Entry(snapshot.World).Property(t => t.Exchange).IsModified = true;
                db.Entry(snapshot.World).Property(t => t.Events).IsModified = true;
                db.Entry(snapshot.World).Property(t => t.Wonders).IsModified = true;
                db.Entry(snapshot.World).Property(t => t.Outposts).IsModified = true;
                db.Entry(snapshot.World).Property(t => t.Season).IsModified = true;
                db.Entry(snapshot.World).Property(t => t.Blueprints).IsModified = true;
                db.Entry(snapshot.World).Property(t => t.Crews).IsModified = true;
                db.Entry(snapshot.World).Property(t => t.Links).IsModified = true;
                db.Entry(snapshot.World).Property(t => t.Projects).IsModified = true;
                db.Entry(snapshot.World).Property(t => t.Trunks).IsModified = true;
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
                    case DbCompany company when _companies.Contains(company.Id):
                        db.Companies.Remove(company);
                        break;
                    case DbCorporation corporation when _corporations.Contains(corporation.Id):
                        db.Corporations.Remove(corporation);
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
                    case DbContract contract:
                        Upsert(db, contract, _contracts.Contains(contract.Id), () => newContracts.Add(contract.Id));
                        break;
                    case DbCorporation corporation:
                        Upsert(db, corporation, _corporations.Contains(corporation.Id), () => newCorporations.Add(corporation.Id));
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
        _contracts.UnionWith(newContracts);
        _corporations.UnionWith(newCorporations);
        foreach (var row in snapshot.Deletes)
        {
            switch (row)
            {
                case DbBuilding building:
                    _buildings.Remove(building.Id);
                    break;
                case DbCompany company:
                    _companies.Remove(company.Id);
                    break;
                case DbCorporation corporation:
                    _corporations.Remove(corporation.Id);
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
        public List<Purchase>? Purchases { get; init; }
        public string? EraEnded { get; init; }
        public Dictionary<string, int>? Refinements { get; init; }
        public string? RefineChoice { get; init; }

        public Dictionary<string, HubMarket> HubMarkets { get; init; } = new();
        public HashSet<string> NoTrade { get; init; } = new();
        public HashSet<string> NoBuy { get; init; } = new();
        public HashSet<long> TradeBlocked { get; init; } = new();
        public Dictionary<long, DateTime> PendingBlocks { get; init; } = new();
        public long? CrewId { get; init; }
        public DateTime? LeftCrewAt { get; init; }
        public bool IsGuest { get; init; }
        public HouseKind? House { get; init; }
        public StandIn? StandIn { get; init; }
        public bool StandInAnnounced { get; init; }
        public string? KnownAs { get; init; }
        public bool SeatedNextDoor { get; init; }
        public bool YardSalesExplained { get; init; }
        public bool KeepResearching { get; init; } = true;
        public DateTime? HoldUntil { get; init; }
        public bool KeepAsked { get; init; }
        public Dictionary<long, int>? TendedBy { get; init; }
        public DateTime? TendedUntil { get; init; }
        public EstateKey? LastSeat { get; init; }
        public int LoneDays { get; init; }
        public DateTime? LoneCardAt { get; init; }
        public bool LoneCard { get; init; }
        public DateTime? ProbationUntil { get; init; }
        public HashSet<string>? ProjectFirst { get; init; }
        public List<GoodSend>? Sends { get; init; }
        public Dictionary<long, int>? ThankedOn { get; init; }
        public HashSet<string>? GiftsAnnounced { get; init; }
        public HashSet<string>? PinMakes { get; init; }
        public HashSet<string>? PinNeeds { get; init; }
        public List<CompanyAsk>? Asks { get; init; }
        public HashSet<long>? KitBuildings { get; init; }
        public DateTime? TutorialStepSince { get; init; }
        public Dictionary<string, DateTime>? Devices { get; init; }
        public double MeasuredIncome { get; init; }
        public double MeasuredEarned { get; init; }
        public Dictionary<string, UsualTrade>? UsualTrades { get; init; }
        public int? League { get; init; }
        public double EraOutput { get; init; }
        public int DistinctDays { get; init; }
        public int LastPlayedDay { get; init; }
        public TradeWith TradeWith { get; init; } = TradeWith.Everyone;
        public HashSet<string> BuiltTypes { get; init; } = new();
        public Dictionary<string, Dictionary<string, double>> XpLog { get; init; } = new();
        public HashSet<string> Researched { get; init; } = new();
        public List<string>? ResearchQueue { get; init; }
        public string? CurrentResearch { get; init; }
        public DateTime? ResearchEndsAt { get; init; }
        public double ContractScore { get; init; } = 1;
        public int ContractsCompleted { get; init; }
        public long? CorporationId { get; init; }
        public CorporationRole Role { get; init; }
        public DateTime? LeftCorporationAt { get; init; }
        public long? MentorId { get; init; }
        public HashSet<int> MentorRewards { get; init; } = new();
        public double WeekStartEarnings { get; init; }
        public int Likes { get; init; }
        public int Shares { get; init; }
        public DateTime? FranchiseAt { get; init; }
        public List<EstateRef> Branches { get; init; } = new();
        public int Franchises { get; init; }
        public double EarningsAtFranchise { get; init; }
        public int LegacyLevel { get; init; }
        public PackedEstate? Packed { get; init; }
        public List<PackedBuilding> Crates { get; init; } = new();
        public bool Asleep { get; init; }
        public DateTime? ArrivesAt { get; init; }
        public DateTime? LastMovedAt { get; init; }
        public int FreeMoves { get; init; }
        public bool MergeBonus { get; init; }
        public int DeepParcels { get; init; }
        public int TutorialStep { get; init; }
        public Expedition? Expedition { get; init; }
        public List<string> ExpeditionLog { get; init; } = new();
        public HashSet<string> Relics { get; init; } = new();
        public int Mastery { get; init; }
        public int? DigParcel { get; init; }
        public DateTime? DigStartedAt { get; init; }
        public DateTime? DigEndsAt { get; init; }
        public long? SwapWith { get; init; }
        public HashSet<string> Titles { get; init; } = new();
        public HashSet<string> Managers { get; init; } = new();
        public HashSet<string> Achievements { get; init; } = new();
        public Dictionary<string, double> Stats { get; init; } = new();
        public double HubReputation { get; init; }
        public List<DailyOrder> DailyOrders { get; init; } = new();
    }


    private sealed record ExchangeState(List<ExchangeOrder> Orders, List<Shipment> Shipments);

    private sealed record EventsState(List<GameEvent> Events, List<string>? PlannedDays);

    private sealed record WondersState(DateTime EraStartedAt, Wonder? EraWonder, List<Wonder>? DistrictWonders);

    private sealed record OutpostsState(List<Outpost>? Outposts, List<Discovery>? Discoveries, List<SeedRow>? Seeds = null, List<SurveyedRow>? Surveyed = null);

    private sealed record SurveyedRow(int X, int Y, int Slot);

    private sealed record SeedRow(int X, int Y, string Deposit, int Richness);

    private sealed record SeasonState(int Season, DateTime? FinaleAt, DateTime? EndedAt, List<SeasonResult>? Results, bool ResultsSaved);

    /// <summary>Whether the results of the ended season went to the players' Legacy already</summary>
    public bool ResultsSaved { get; set; }

    private sealed record CorporationData(HashSet<long> Applications, HashSet<long> Invites, CorporationGoal? Goal);
}
