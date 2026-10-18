using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

public sealed record CommandResult(bool Success, string? Error = null)
{
    public static readonly CommandResult Ok = new(true);

    public static CommandResult Fail(string error) => new(false, error);
}

public enum WorldEventKind
{
    ConstructionFinished,
    LevelUp,
    ResearchFinished,
    PriceChanged,
    OrderCompleted,
    ContractChanged,
    ExchangeTrade,
    Corporation,
    Social,
    StorageFull,
    Neighbour,
    Calendar,
    Wonder,
}

/// <summary>Something a player should hear about (a toast in the client)</summary>
public sealed record WorldEvent(long Company, WorldEventKind Kind, string Text, long? Building = null);

/// <summary>What changed in the world since the server last asked (to send to clients)</summary>
public sealed class WorldChanges
{
    public HashSet<TilePos> Tiles { get; } = new();
    public HashSet<long> Companies { get; } = new();
    public HashSet<int> Networks { get; } = new();
    public HashSet<long> Storages { get; } = new();
    public HashSet<(DistrictCoord District, int Slot)> Estates { get; } = new();
    public HashSet<DistrictCoord> Markets { get; } = new();
    public HashSet<long> Contracts { get; } = new();
    public HashSet<long> Corporations { get; } = new();

    /// <summary>Districts whose hub changed (a Wonder grew)</summary>
    public HashSet<DistrictCoord> Districts { get; } = new();

    public bool Exchange { get; set; }
    public bool Wonders { get; set; }
    public List<WorldEvent> Events { get; } = new();

    public bool IsEmpty => Tiles.Count == 0 && Companies.Count == 0 && Networks.Count == 0 && Storages.Count == 0 && Estates.Count == 0 && Markets.Count == 0 && Contracts.Count == 0 && Corporations.Count == 0 && Districts.Count == 0 && !Exchange && !Wonders && Events.Count == 0;
}

/// <summary>
/// The whole game world in memory: estates, companies, buildings, pipes and networks, with all player commands and the
/// simulation tick. Not thread safe: the server serialises access.
/// </summary>
public sealed partial class GameWorld
{
    public const double StorageEpsilon = 1e-6;

    public GameContent Content { get; }
    public WorldGenerator Generator { get; }
    public int Era { get; set; } = 1;
    public DateTime LastTick { get; set; }

    private readonly Func<DateTime> _clock;
    private readonly Dictionary<long, Company> _companies = new();
    private readonly Dictionary<(DistrictCoord, int), Estate> _estates = new();
    private readonly Dictionary<long, Building> _buildings = new();
    private readonly Dictionary<TilePos, long> _buildingAt = new();
    private readonly Dictionary<TilePos, Conductor> _conductors = new();
    private readonly Dictionary<TilePos, TerrainType> _terrainOverrides = new();
    private readonly HashSet<DistrictCoord> _openDistricts = new();

    public long NextCompanyId { get; set; } = 1;
    public long NextBuildingId { get; set; } = 1;

    public WorldChanges Changes { get; private set; } = new();

    /// <summary>Deleted entities the persistence layer has to remove</summary>
    public List<object> Deleted { get; } = new();

    /// <summary>The gold a new company starts with (the content's value unless the server configures another)</summary>
    public double StartingGold { get; set; }

    public GameWorld(GameContent content, int seed, Func<DateTime> clock)
    {
        Content = content;
        Generator = new WorldGenerator(seed);
        _clock = clock;
        LastTick = clock();
        StartingGold = content.Progression.StartingGold;
    }

    public DateTime Now => _clock();

    public WorldChanges TakeChanges()
    {
        var changes = Changes;
        Changes = new WorldChanges();
        return changes;
    }

    // ---------------------------------------------------------------- Queries

    public IEnumerable<Company> Companies => _companies.Values;
    public IEnumerable<Estate> Estates => _estates.Values;
    public IEnumerable<Building> Buildings => _buildings.Values;
    public IEnumerable<Conductor> Conductors => _conductors.Values;
    public IReadOnlyDictionary<TilePos, TerrainType> TerrainOverrides => _terrainOverrides;
    public IEnumerable<DistrictCoord> OpenDistricts => _openDistricts;

    public Company? Company(long id) => _companies.TryGetValue(id, out var company) ? company : null;

    public Company? CompanyOfUser(long userId) => _companies.Values.FirstOrDefault(t => t.UserId == userId);

    public Building? Building(long id) => _buildings.TryGetValue(id, out var building) ? building : null;

    public Building? BuildingAt(TilePos pos) => _buildingAt.TryGetValue(pos, out var id) ? _buildings[id] : null;

    public Conductor? ConductorAt(TilePos pos) => _conductors.TryGetValue(pos, out var conductor) ? conductor : null;

    public Estate? Estate(DistrictCoord district, int slot) => _estates.TryGetValue((district, slot), out var estate) ? estate : null;

    public Estate? EstateOf(Company company) => Estate(company.District, company.Slot);

    public Estate? EstateAt(TilePos pos)
    {
        var zone = WorldLayout.ZoneAt(pos.X, pos.Y);
        return zone.Kind == ZoneKind.Estate ? Estate(zone.District, zone.Slot) : null;
    }

    public TileInfo Tile(TilePos pos)
    {
        var info = Generator.Tile(pos);
        return _terrainOverrides.TryGetValue(pos, out var terrain) ? info with { Terrain = terrain } : info;
    }

    public bool IsOccupied(TilePos pos) => _buildingAt.ContainsKey(pos) || _conductors.ContainsKey(pos);

    public int CopiesOwned(long companyId, string type) => _buildings.Values.Count(t => t.Owner == companyId && t.Type == type);

    public IEnumerable<Building> BuildingsOf(long companyId) => _buildings.Values.Where(t => t.Owner == companyId);

    // ---------------------------------------------------------------- Loading (persistence)

    public void LoadCompany(Company company)
    {
        _companies[company.Id] = company;
        NextCompanyId = Math.Max(NextCompanyId, company.Id + 1);
        company.Dirty = false;
    }

    public void LoadEstate(Estate estate)
    {
        _estates[(estate.District, estate.Slot)] = estate;
        _openDistricts.Add(estate.District);
        estate.Dirty = false;
    }

    public void LoadBuilding(Building building)
    {
        _buildings[building.Id] = building;
        foreach (var tile in building.Footprint())
        {
            _buildingAt[tile] = building.Id;
        }
        NextBuildingId = Math.Max(NextBuildingId, building.Id + 1);
        building.Dirty = false;
        MarkDirty(building);
    }

    public void LoadConductor(Conductor conductor)
    {
        _conductors[conductor.Pos] = conductor;
        conductor.Dirty = false;
        MarkDirty(conductor.Pos);
    }

    public void LoadTerrainOverride(TilePos pos, TerrainType terrain) => _terrainOverrides[pos] = terrain;

    // ---------------------------------------------------------------- Companies and estates

    private void OpenDistrict(DistrictCoord district)
    {
        if (!_openDistricts.Add(district))
        {
            return;
        }
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
        {
            _estates[(district, slot)] = new Estate { District = district, Slot = slot, Origin = WorldLayout.EstateOrigin(district, slot) };
        }
        if (TenantsEnabled)
        {
            CreateTenants(district);
        }
    }

    /// <summary>NPC tenants on free estates (off in most tests)</summary>
    public bool TenantsEnabled { get; set; }

    /// <summary>
    /// The estate a new company gets: the first free estate in the oldest district with room; when only tenants are left
    /// there, a tenant moves out
    /// </summary>
    private Estate FindFreeEstate()
    {
        foreach (var district in WorldLayout.DistrictOrder())
        {
            OpenDistrict(district);
            for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
            {
                var estate = _estates[(district, slot)];
                if (estate.OwnerId == null)
                {
                    return estate;
                }
            }
            for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
            {
                var estate = _estates[(district, slot)];
                if (estate.OwnerId is { } owner && Company(owner) is { IsNpc: true })
                {
                    EvictTenant(estate);
                    return estate;
                }
            }
        }
        throw new InvalidOperationException("The world is infinite, this can't happen.");
    }

    public const int MinCompanyNameLength = 3;
    public const int MaxCompanyNameLength = 24;

    /// <summary>Why a company name can't be used, or null when it is fine</summary>
    public string? CompanyNameError(string name)
    {
        name = name.Trim();
        if (name.Length is < MinCompanyNameLength or > MaxCompanyNameLength)
        {
            return $"A company name has {MinCompanyNameLength} to {MaxCompanyNameLength} characters.";
        }
        if (!name.All(t => char.IsLetterOrDigit(t) || t is ' ' or '-' or '_' or '.' or '&' or '\''))
        {
            return "Use letters, digits, spaces and - _ . & ' in a company name.";
        }
        if (_companies.Values.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return "That name is already taken.";
        }
        return null;
    }

    public static readonly (int X, int Y) HqLocal = (15, 15);
    public static readonly (int X, int Y) TradingPostLocal = (18, 15);

    public Company CreateCompany(long userId, string name, string color)
    {
        var estate = FindFreeEstate();
        var now = Now;
        var company = new Company
        {
            Id = NextCompanyId++,
            UserId = userId,
            Name = name.Trim(),
            Color = color,
            Gold = StartingGold,
            CreatedAt = now,
            LastSeenAt = now,
            District = estate.District,
            Slot = estate.Slot,
            Report = new CompanyReport { Since = now },
        };
        _companies[company.Id] = company;

        estate.OwnerId = company.Id;
        estate.UnlockedParcels = Content.Progression.StartParcels.Aggregate(0, (mask, parcel) => mask | (1 << parcel));
        estate.Touch();
        Changes.Estates.Add((estate.District, estate.Slot));

        foreach (var side in Enum.GetValues<GateSide>())
        {
            var pos = WorldLayout.GatePosition(estate.District, estate.Slot, side);
            var gate = new Conductor { Pos = pos, Kind = ConductorKind.Gate, Owner = company.Id, Side = side };
            _conductors[pos] = gate;
            MarkDirty(pos);
        }

        foreach (var neighbour in _companies.Values.Where(t => t.District == company.District && t.Id != company.Id && !t.IsNpc))
        {
            neighbour.Report.NewNeighbours.Add(company.Name);
            neighbour.Touch();
            Changes.Events.Add(new WorldEvent(neighbour.Id, WorldEventKind.Neighbour, $"{company.Name} moved into your district"));
        }

        AddBuilding(company, "hq", estate.Origin.Offset(HqLocal.X, HqLocal.Y), finished: true);
        AddBuilding(company, "tradingPost", estate.Origin.Offset(TradingPostLocal.X, TradingPostLocal.Y), finished: true);
        company.BuiltTypes.Clear();
        Changes.Companies.Add(company.Id);
        return company;
    }

    private Building AddBuilding(Company company, string type, TilePos pos, bool finished)
    {
        var definition = Content.Building(type);
        var building = new Building
        {
            Id = NextBuildingId++,
            Type = type,
            Owner = company.Id,
            X = pos.X,
            Y = pos.Y,
            Size = definition.Size,
            ConstructionEndsAt = finished || definition.BuildSeconds <= 0 ? null : Now.AddSeconds(definition.BuildSeconds),
        };
        _buildings[building.Id] = building;
        foreach (var tile in building.Footprint())
        {
            _buildingAt[tile] = building.Id;
            // Building on a forest clears it
            if (Tile(tile).Terrain == TerrainType.Forest)
            {
                _terrainOverrides[tile] = TerrainType.Grass;
            }
            Changes.Tiles.Add(tile);
        }
        MarkDirty(building);
        return building;
    }

    // ---------------------------------------------------------------- Unlocks

    public bool IsUnlocked(Company company, BuildingDefinition definition, out string? reason)
    {
        if (!definition.Buildable)
        {
            reason = $"{definition.Name} can't be built.";
            return false;
        }
        if (definition.Unlock.Era > Era)
        {
            reason = $"{definition.Name} needs Era {definition.Unlock.Era}.";
            return false;
        }
        if (company.Level < definition.Unlock.Level)
        {
            reason = $"{definition.Name} needs Company Level {definition.Unlock.Level}.";
            return false;
        }
        if (definition.Unlock.Research is { } research && !HasResearch(company, research))
        {
            reason = $"{definition.Name} needs research.";
            return false;
        }
        if (definition.Layer == BuildingLayer.Deep)
        {
            reason = $"{definition.Name} can only be built in the Deep Level.";
            return false;
        }
        reason = null;
        return true;
    }


    // ---------------------------------------------------------------- Placement

    /// <summary>Checks whether a building can stand here (ignoring one building, for moving)</summary>
    public CommandResult CanPlace(Company company, BuildingDefinition definition, TilePos pos, long? ignoreBuilding = null)
    {
        var estate = EstateOf(company);
        if (estate == null)
        {
            return CommandResult.Fail("You don't have an estate.");
        }
        if (definition.Kind == BuildingKind.TollMeter)
        {
            return CanPlaceTollMeter(company, pos);
        }

        var footprint = new List<TilePos>();
        for (var dy = 0; dy < definition.Size; dy++)
        {
            for (var dx = 0; dx < definition.Size; dx++)
            {
                footprint.Add(pos.Offset(dx, dy));
            }
        }

        foreach (var tile in footprint)
        {
            if (!estate.Contains(tile))
            {
                return CommandResult.Fail("You can only build on your own estate.");
            }
            if (!estate.IsUnlocked(estate.ParcelAt(tile)))
            {
                return CommandResult.Fail("This parcel is still locked. Buy it first.");
            }
            if ((_buildingAt.TryGetValue(tile, out var other) && other != ignoreBuilding) || _conductors.ContainsKey(tile))
            {
                return CommandResult.Fail("Something is already standing here.");
            }
            var terrain = Tile(tile).Terrain;
            if (!definition.Placement.Terrain.Contains(terrain))
            {
                return CommandResult.Fail($"{definition.Name} can't be built on {terrain.ToString().ToLowerInvariant()}.");
            }
        }

        if (definition.Placement.Deposit is { } deposit)
        {
            var best = footprint.Select(Tile).Where(t => DepositMatches(deposit, t.Deposit)).Select(t => t.Richness).DefaultIfEmpty(0).Max();
            if (best == 0)
            {
                return CommandResult.Fail($"{definition.Name} needs a {DepositName(deposit)} deposit.");
            }
            if (best < definition.Placement.MinRichness)
            {
                return CommandResult.Fail($"{definition.Name} needs a deposit of at least {definition.Placement.MinRichness} stars.");
            }
        }

        var border = BorderTiles(pos, definition.Size).ToList();
        if (definition.Placement.Shore && !border.Any(t => Tile(t).Terrain == TerrainType.Water))
        {
            return CommandResult.Fail($"{definition.Name} has to stand next to water.");
        }
        if (definition.Placement.Forest && ForestAround(pos, definition.Size) < 3)
        {
            return CommandResult.Fail($"{definition.Name} needs a forest nearby.");
        }
        return CommandResult.Ok;
    }

    /// <summary>A Toll Meter goes in a corridor, in the line of one of your own pipes</summary>
    private CommandResult CanPlaceTollMeter(Company company, TilePos pos)
    {
        if (WorldLayout.ZoneAt(pos.X, pos.Y).Kind != ZoneKind.Corridor)
        {
            return CommandResult.Fail("Toll meters go on your public pipes in the corridors.");
        }
        if (IsOccupied(pos))
        {
            return CommandResult.Fail("Something is already standing here.");
        }
        var ownPipes = Directions.Count(d => _conductors.TryGetValue(pos.Offset(d.X, d.Y), out var c) && c.Owner == company.Id && c.Kind == ConductorKind.Pipe);
        if (ownPipes == 0)
        {
            return CommandResult.Fail("Put it in the line of one of your own corridor pipes.");
        }
        return CommandResult.Ok;
    }

    public static bool DepositMatches(string wanted, string? actual) =>
        actual != null && (wanted == actual || (wanted == "anyMetal" && actual is "ironOre" or "copperOre" or "coal"));

    private string DepositName(string deposit) =>
        deposit == "anyMetal" ? "metal" : Content.Resources.TryGetValue(deposit, out var resource) ? resource.Name.ToLowerInvariant() : deposit;

    public static IEnumerable<TilePos> BorderTiles(TilePos pos, int size)
    {
        for (var i = 0; i < size; i++)
        {
            yield return pos.Offset(i, -1);
            yield return pos.Offset(i, size);
            yield return pos.Offset(-1, i);
            yield return pos.Offset(size, i);
        }
    }

    /// <summary>Forest tiles within 2 tiles of the footprint that are not built on</summary>
    public int ForestAround(TilePos pos, int size)
    {
        var count = 0;
        for (var y = pos.Y - 2; y < pos.Y + size + 2; y++)
        {
            for (var x = pos.X - 2; x < pos.X + size + 2; x++)
            {
                var tile = new TilePos(x, y);
                if (Tile(tile).Terrain == TerrainType.Forest && !_buildingAt.ContainsKey(tile))
                {
                    count++;
                }
            }
        }
        return count;
    }

    // ---------------------------------------------------------------- Costs

    public double BuildCost(Company company, BuildingDefinition definition) =>
        Content.Progression.BuildCost(definition, CopiesOwned(company.Id, definition.Id));

    /// <summary>Materials of a company: the contents of all its storages</summary>
    public Dictionary<string, double> Materials(long companyId)
    {
        var total = new Dictionary<string, double>();
        foreach (var storage in _buildings.Values.Where(t => t.Owner == companyId && t.Contents.Count != 0))
        {
            foreach (var (resource, amount) in storage.Contents)
            {
                total[resource] = (total.TryGetValue(resource, out var v) ? v : 0) + amount;
            }
        }
        return total;
    }

    private CommandResult Pay(Company company, double gold, IReadOnlyDictionary<string, double> materials)
    {
        if (company.Gold < gold - 1e-9)
        {
            return CommandResult.Fail($"You need {Math.Ceiling(gold):N0} gold.");
        }
        if (materials.Count != 0)
        {
            var available = Materials(company.Id);
            foreach (var (resource, amount) in materials)
            {
                if ((available.TryGetValue(resource, out var have) ? have : 0) < amount - 1e-9)
                {
                    return CommandResult.Fail($"You need {amount:N0} {Content.Resources[resource].Name.ToLowerInvariant()} in your storages.");
                }
            }
            foreach (var (resource, amount) in materials)
            {
                var left = amount;
                foreach (var storage in _buildings.Values.Where(t => t.Owner == company.Id && t.Contents.ContainsKey(resource)).OrderBy(t => t.Id))
                {
                    var take = Math.Min(left, storage.Contents[resource]);
                    storage.Contents[resource] -= take;
                    if (storage.Contents[resource] <= StorageEpsilon)
                    {
                        storage.Contents.Remove(resource);
                    }
                    storage.Touch();
                    MarkDirty(storage);
                    left -= take;
                    if (left <= 1e-9)
                    {
                        break;
                    }
                }
            }
        }
        company.Gold -= gold;
        company.Report.GoldSpent += gold;
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    // ---------------------------------------------------------------- Commands

    public CommandResult PlaceBuilding(long companyId, string type, TilePos pos)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (!Content.Buildings.TryGetValue(type, out var definition))
        {
            return CommandResult.Fail("Unknown building.");
        }
        if (!IsUnlocked(company, definition, out var reason))
        {
            return CommandResult.Fail(reason!);
        }
        var placement = CanPlace(company, definition, pos);
        if (!placement.Success)
        {
            return placement;
        }
        var payment = Pay(company, BuildCost(company, definition), definition.Cost.Materials);
        if (!payment.Success)
        {
            return payment;
        }

        AddBuilding(company, type, pos, finished: false);
        GainXp(company, Content.Progression.XpReward("building") + (company.BuiltTypes.Add(type) ? Content.Progression.XpReward("firstBuildingOfType") : 0));
        return CommandResult.Ok;
    }

    public CommandResult Demolish(long companyId, long buildingId)
    {
        if (Company(companyId) is not { } company || Building(buildingId) is not { } building || building.Owner != companyId)
        {
            return CommandResult.Fail("That is not your building.");
        }
        var definition = Content.Building(building.Type);
        if (definition.Kind is BuildingKind.Hq or BuildingKind.Gate)
        {
            return CommandResult.Fail($"The {definition.Name} can't be demolished.");
        }

        RemoveBuilding(building);
        company.Gold += Math.Round(definition.Cost.Gold * 0.5);
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    private void RemoveBuilding(Building building)
    {
        MarkDirty(building);
        foreach (var tile in building.Footprint())
        {
            _buildingAt.Remove(tile);
            Changes.Tiles.Add(tile);
        }
        _buildings.Remove(building.Id);
        Deleted.Add(building);
    }

    public CommandResult Move(long companyId, long buildingId, TilePos pos)
    {
        if (Company(companyId) is not { } company || Building(buildingId) is not { } building || building.Owner != companyId)
        {
            return CommandResult.Fail("That is not your building.");
        }
        var definition = Content.Building(building.Type);
        if (definition.Kind is BuildingKind.Gate)
        {
            return CommandResult.Fail("Gates can't be moved.");
        }
        var placement = CanPlace(company, definition, pos, building.Id);
        if (!placement.Success)
        {
            return placement;
        }

        MarkDirty(building);
        foreach (var tile in building.Footprint())
        {
            _buildingAt.Remove(tile);
            Changes.Tiles.Add(tile);
        }
        building.X = pos.X;
        building.Y = pos.Y;
        foreach (var tile in building.Footprint())
        {
            _buildingAt[tile] = building.Id;
            if (Tile(tile).Terrain == TerrainType.Forest)
            {
                _terrainOverrides[tile] = TerrainType.Grass;
            }
            Changes.Tiles.Add(tile);
        }
        if (definition.BuildSeconds > 0)
        {
            building.ConstructionEndsAt = Now.AddSeconds(Math.Max(5, definition.BuildSeconds * 0.1));
        }
        building.Touch();
        MarkDirty(building);
        return CommandResult.Ok;
    }

    public double UpgradeCost(Building building, int levels)
    {
        var definition = Content.Building(building.Type);
        double total = 0;
        for (var level = building.Level; level < building.Level + levels; level++)
        {
            total += Content.Progression.UpgradeCost(definition, level);
        }
        return total;
    }

    /// <summary>How many levels the company can afford right now (for the Max button)</summary>
    public int AffordableLevels(Company company, Building building)
    {
        var definition = Content.Building(building.Type);
        var gold = company.Gold;
        var levels = 0;
        while (building.Level + levels < Content.Progression.MaxBuildingLevel)
        {
            var cost = Content.Progression.UpgradeCost(definition, building.Level + levels);
            if (cost > gold)
            {
                break;
            }
            gold -= cost;
            levels++;
        }
        return levels;
    }

    public CommandResult Upgrade(long companyId, long buildingId, int levels)
    {
        if (Company(companyId) is not { } company || Building(buildingId) is not { } building || building.Owner != companyId)
        {
            return CommandResult.Fail("That is not your building.");
        }
        var definition = Content.Building(building.Type);
        if (definition.Kind is BuildingKind.Hq or BuildingKind.Gate or BuildingKind.Decoration)
        {
            return CommandResult.Fail($"The {definition.Name} grows with your Company Level.");
        }
        if (building.UnderConstruction)
        {
            return CommandResult.Fail("Wait until it is built.");
        }
        levels = Math.Min(levels, Content.Progression.MaxBuildingLevel - building.Level);
        if (levels <= 0)
        {
            return CommandResult.Fail("This building is at its maximum level.");
        }
        var payment = Pay(company, UpgradeCost(building, levels), new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }

        var from = building.Level;
        building.Level += levels;
        building.Touch();
        MarkDirty(building);
        foreach (var tile in building.Footprint())
        {
            Changes.Tiles.Add(tile);
        }
        var milestones = Content.Progression.Milestones.Count(t => t > from && t <= building.Level);
        GainXp(company, levels * Content.Progression.XpReward("upgradeLevel") + milestones * Content.Progression.XpReward("milestone"));
        return CommandResult.Ok;
    }

    public CommandResult BuyParcel(long companyId, int parcel)
    {
        if (Company(companyId) is not { } company || EstateOf(company) is not { } estate)
        {
            return CommandResult.Fail("You don't have an estate.");
        }
        if (parcel is < 0 or >= WorldLayout.ParcelCount)
        {
            return CommandResult.Fail("Unknown parcel.");
        }
        if (estate.IsUnlocked(parcel))
        {
            return CommandResult.Fail("You already own this parcel.");
        }
        var (px, py) = (parcel % WorldLayout.ParcelsPerSide, parcel / WorldLayout.ParcelsPerSide);
        var neighbours = new[] { (px - 1, py), (px + 1, py), (px, py - 1), (px, py + 1) }
            .Where(t => t.Item1 is >= 0 and < WorldLayout.ParcelsPerSide && t.Item2 is >= 0 and < WorldLayout.ParcelsPerSide)
            .Select(t => t.Item2 * WorldLayout.ParcelsPerSide + t.Item1);
        if (!neighbours.Any(estate.IsUnlocked))
        {
            return CommandResult.Fail("You can only buy a parcel next to land you own.");
        }
        var extra = estate.UnlockedCount - Content.Progression.StartParcels.Count;
        var levelNeeded = Content.Progression.ParcelLevels[Math.Min(extra, Content.Progression.ParcelLevels.Count - 1)];
        if (company.Level < levelNeeded)
        {
            return CommandResult.Fail($"Your next parcel needs Company Level {levelNeeded}.");
        }
        var payment = Pay(company, Content.Progression.ParcelCost(extra), new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }

        estate.UnlockedParcels |= 1 << parcel;
        estate.Touch();
        Changes.Estates.Add((estate.District, estate.Slot));
        var (ox, oy) = WorldLayout.ParcelOrigin(parcel);
        for (var y = 0; y < WorldLayout.ParcelSize; y++)
        {
            for (var x = 0; x < WorldLayout.ParcelSize; x++)
            {
                Changes.Tiles.Add(estate.Origin.Offset(ox + x, oy + y));
            }
        }
        GainXp(company, Content.Progression.XpReward("parcel"));
        return CommandResult.Ok;
    }

    /// <summary>The cost and the Company Level needed for the next parcel</summary>
    public (double Cost, int Level) NextParcel(Company company)
    {
        var estate = EstateOf(company);
        var extra = (estate?.UnlockedCount ?? 4) - Content.Progression.StartParcels.Count;
        if (extra >= Content.Progression.ParcelLevels.Count)
        {
            return (0, int.MaxValue);
        }
        return (Content.Progression.ParcelCost(extra), Content.Progression.ParcelLevels[extra]);
    }

    public CommandResult PlacePipes(long companyId, IReadOnlyList<TilePos> tiles, int tier = 1)
    {
        if (Company(companyId) is not { } company || EstateOf(company) is not { } estate)
        {
            return CommandResult.Fail("You don't have an estate.");
        }
        var pipe = Content.PipeTiers.FirstOrDefault(t => t.Tier == tier);
        if (pipe == null)
        {
            return CommandResult.Fail("Unknown pipe.");
        }
        if (pipe.Era > Era || (pipe.Research != null && !HasResearch(company, pipe.Research)))
        {
            return CommandResult.Fail($"{pipe.Name}s are not unlocked yet.");
        }

        var newTiles = tiles.Distinct().Where(t => !(_conductors.TryGetValue(t, out var existing) && existing.Owner == companyId && existing.Kind == ConductorKind.Pipe)).ToList();
        double cost = 0;
        foreach (var tile in newTiles)
        {
            var zone = WorldLayout.ZoneAt(tile.X, tile.Y);
            switch (zone.Kind)
            {
                case ZoneKind.Estate:
                    // Pipes may cross parcels you haven't bought yet, so every gate can be reached from the start
                    if (!estate.Contains(tile))
                    {
                        return CommandResult.Fail("You can't lay pipes on someone else's estate. Connect through the corridors.");
                    }
                    break;
                case ZoneKind.Corridor:
                    break;
                default:
                    return CommandResult.Fail("Pipes go on your estate and in the corridors.");
            }
            if (IsOccupied(tile))
            {
                return CommandResult.Fail("Something is already there.");
            }
            var terrain = Tile(tile).Terrain;
            if (terrain is TerrainType.Mountain)
            {
                return CommandResult.Fail("Pipes can't cross mountains.");
            }
            // Bridges over water cost three times as much
            cost += pipe.Gold * (terrain == TerrainType.Water ? 3 : 1);
        }
        if (newTiles.Count == 0)
        {
            return CommandResult.Ok;
        }

        var materials = pipe.Materials.ToDictionary(t => t.Key, t => t.Value * newTiles.Count);
        var payment = Pay(company, cost, materials);
        if (!payment.Success)
        {
            return payment;
        }
        foreach (var tile in newTiles)
        {
            _conductors[tile] = new Conductor { Pos = tile, Tier = tier, Owner = companyId };
            MarkDirty(tile);
            Changes.Tiles.Add(tile);
        }
        return CommandResult.Ok;
    }

    public CommandResult RemovePipes(long companyId, IReadOnlyList<TilePos> tiles)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        var removed = 0;
        double refund = 0;
        foreach (var tile in tiles.Distinct())
        {
            if (_conductors.TryGetValue(tile, out var conductor) && conductor.Owner == companyId && conductor.Kind == ConductorKind.Pipe)
            {
                MarkDirty(tile);
                _conductors.Remove(tile);
                Deleted.Add(conductor);
                Changes.Tiles.Add(tile);
                refund += Content.PipeTier(conductor.Tier).Gold * 0.5;
                removed++;
            }
        }
        if (removed == 0)
        {
            return CommandResult.Fail("There are no pipes of yours there.");
        }
        company.Gold += refund;
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    // ---------------------------------------------------------------- Settings

    public CommandResult SetImport(long companyId, long buildingId, string resource, bool enabled)
    {
        if (Building(buildingId) is not { } building || building.Owner != companyId || Content.Building(building.Type).Kind != BuildingKind.TradingPost)
        {
            return CommandResult.Fail("That is not your trading post.");
        }
        if (!Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown resource.");
        }
        if (enabled)
        {
            building.Imports.Add(resource);
        }
        else
        {
            building.Imports.Remove(resource);
        }
        building.Touch();
        MarkDirty(building);
        return CommandResult.Ok;
    }

    /// <summary>Which resources a valve lets through (none chosen: everything)</summary>
    public CommandResult SetValveFilter(long companyId, long buildingId, string resource, bool enabled)
    {
        if (Building(buildingId) is not { } building || building.Owner != companyId || Content.Building(building.Type).Kind != BuildingKind.Valve)
        {
            return CommandResult.Fail("That is not your valve.");
        }
        if (!Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown resource.");
        }
        if (enabled)
        {
            building.Filter.Add(resource);
        }
        else
        {
            building.Filter.Remove(resource);
        }
        building.Touch();
        MarkDirty(building);
        return CommandResult.Ok;
    }

    public CommandResult SetGatePolicy(long companyId, GateSide side, GatePolicy policy)
    {
        if (Company(companyId) is not { } company || EstateOf(company) is not { } estate)
        {
            return CommandResult.Fail("You don't have an estate.");
        }
        estate.GatePolicies[side] = policy;
        estate.Touch();
        Changes.Estates.Add((estate.District, estate.Slot));
        MarkDirty(WorldLayout.GatePosition(estate.District, estate.Slot, side));
        return CommandResult.Ok;
    }

    /// <summary>A company changed outside a command (admin tools): save it and tell its player</summary>
    public void CompanyChanged(Company company)
    {
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkCompanyNetworksDirty(company.Id);
    }

    // ---------------------------------------------------------------- Experience

    public void GainXp(Company company, double xp)
    {
        if (xp <= 0)
        {
            return;
        }
        company.Xp += xp;
        var level = Content.Progression.LevelForXp(company.Xp);
        if (level > company.Level)
        {
            company.Report.LevelsGained += level - company.Level;
            company.Level = level;
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.LevelUp, $"Company Level {level}!"));
            MentorRewards(company);
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
    }
}
