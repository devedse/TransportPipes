using System.Text.RegularExpressions;
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
    Achievement,
    Crew,
    Link,
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
    public HashSet<long> Crews { get; } = new();
    /// <summary>Companies whose links were offered, changed or ended</summary>
    public HashSet<long> Links { get; } = new();

    /// <summary>Districts whose hub changed (a Wonder grew)</summary>
    public HashSet<DistrictCoord> Districts { get; } = new();

    public bool Exchange { get; set; }
    public bool Wonders { get; set; }
    public bool Outposts { get; set; }
    public bool Blueprints { get; set; }
    public List<WorldEvent> Events { get; } = new();

    public bool IsEmpty => Tiles.Count == 0 && Companies.Count == 0 && Networks.Count == 0 && Storages.Count == 0 && Estates.Count == 0 && Markets.Count == 0 && Contracts.Count == 0 && Corporations.Count == 0 && Crews.Count == 0 && Links.Count == 0 && Districts.Count == 0 && !Exchange && !Wonders && !Outposts && !Blueprints && Events.Count == 0;
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

    /// <param name="dealtLand">The land goods are dealt per station (step 5); old worlds keep their land</param>
    public GameWorld(GameContent content, int seed, Func<DateTime> clock, bool dealtLand = false)
    {
        Content = content;
        Generator = new WorldGenerator(seed, dealtLand);
        _clock = clock;
        LastTick = clock();
        StartingGold = content.Progression.StartingGold;
        PlayerPriceFactor = content.Progression.Together.PlayerPrice;
    }

    public DateTime Now => _clock();

    public WorldChanges TakeChanges()
    {
        // Networks are rebuilt when something asks for them: first, so a rebuild's new ids are in these changes (and
        // the clients get the new ids of all its pipes, not only of the tiles that changed)
        UpdateNetworks();
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

    /// <summary>The company's estate; null while it is packed (asleep on the Quiet Isles, or moving)</summary>
    public Estate? EstateOf(Company company) => Estate(company.District, company.Slot) is { } estate && estate.OwnerId == company.Id ? estate : null;

    public Estate? EstateAt(TilePos pos)
    {
        var zone = WorldLayout.ZoneAt(pos.X, pos.Y);
        return zone.Kind == ZoneKind.Estate ? Estate(zone.District, zone.Slot) : null;
    }

    public TileInfo Tile(TilePos pos)
    {
        if (WorldLayout.IsDeep(pos))
        {
            return DeepTile(pos);
        }
        var info = Generator.Tile(pos);
        if (_discoveries.Count != 0 && info.IsBuildableTerrain && DiscoveryAt(pos) is { } discovery)
        {
            info = info with { Deposit = discovery.Resource, Richness = 5 };
        }
        if (_seeds.TryGetValue(pos, out var seed))
        {
            info = info with { Deposit = seed.Deposit, Richness = seed.Richness };
        }
        if (_terrainOverrides.TryGetValue(pos, out var terrain))
        {
            // Raised hills catch more wind, flattened ones less
            var wind = terrain == TerrainType.Hills && info.Terrain != TerrainType.Hills ? info.Wind * 1.2
                : terrain != TerrainType.Hills && info.Terrain == TerrainType.Hills ? info.Wind / 1.2
                : info.Wind;
            info = info with { Terrain = terrain, Wind = wind };
        }
        return info;
    }

    /// <summary>Deposits that differ from the generated terrain (discoveries), for the clients</summary>
    public (string Deposit, int Richness)? DepositOverride(TilePos pos) =>
        _seeds.TryGetValue(pos, out var seed) ? seed
        : _discoveries.Count != 0 && Generator.Tile(pos).IsBuildableTerrain && DiscoveryAt(pos) is { } discovery ? (discovery.Resource, 5)
        : null;

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

    /// <summary>For testing: opens the next district before the open ones are full</summary>
    public void OpenNextDistrict()
    {
        if (WorldLayout.DistrictOrder().FirstOrDefault(t => !_openDistricts.Contains(t)) is var next)
        {
            OpenDistrict(next);
        }
    }

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
    private Estate FindFreeEstate(bool kit = false)
    {
        foreach (var district in WorldLayout.DistrictOrder())
        {
            OpenDistrict(district);
            for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
            {
                var estate = _estates[(district, slot)];
                // Corporation blocks are kept for their members, held seats for their crew's friends
                if (estate.OwnerId == null && (estate.ReservedFor == null || estate.ReservedUntil <= LastTick) && SeatFree(KeyOf(estate)))
                {
                    return estate;
                }
            }
            for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
            {
                var estate = _estates[(district, slot)];
                if (estate.OwnerId is { } owner && Company(owner) is { IsNpc: true } && SeatFree(KeyOf(estate)))
                {
                    EvictTenant(estate, kit);
                    return estate;
                }
            }
        }
        throw new InvalidOperationException("The world is infinite, this can't happen.");
    }

    /// <summary>A seat for a newcomer: free, or kept warm by a tenant who moves out (null when it isn't free for them)</summary>
    private Estate? TakeSeat(EstateKey seat, string? code = null)
    {
        OpenDistrict(seat.District);
        if (Estate(seat.District, seat.Slot) is not { } estate || !SeatFree(seat, null, code))
        {
            return null;
        }
        if (estate.OwnerId is { } owner && Company(owner) is { IsNpc: true })
        {
            // The House leaves the newcomer a starter kit
            EvictTenant(estate, kit: true);
        }
        return estate.OwnerId == null ? estate : null;
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

    private static readonly string[] NameIdeas =
    [
        "Brass & Kettle", "Copperleaf Co.", "Steam & Sons", "Valve Street Works", "Gaslight Pipes",
        "Ironfern Co.", "Bellows & Bolt", "Lantern Lane Co.", "Cogwheel Pipes", "Riverbend Works",
    ];

    /// <summary>A company name nobody uses yet, for a newcomer who moves in with two taps: its land's name, or a pleasant one</summary>
    public string FreeCompanyName(EstateKey? seat = null)
    {
        var land = seat is { } chosen ? Regex.Replace(Generator.Estate(chosen.District, chosen.Slot).Primary.ToString(), "(?<=[a-z])(?=[A-Z])", " ") : null;
        string[] ideas = land != null
            ? [$"{land} Co.", $"{land} Works", $"{land} Pipes", $"{land} & Sons"]
            : [.. NameIdeas.Skip((int)(NextCompanyId % NameIdeas.Length)), .. NameIdeas.Take((int)(NextCompanyId % NameIdeas.Length))];
        for (var n = 1; ; n++)
        {
            foreach (var idea in ideas)
            {
                var name = n == 1 ? idea : $"{idea} {n}";
                if (CompanyNameError(name) == null)
                {
                    return name;
                }
            }
        }
    }

    public static readonly (int X, int Y) HqLocal = (15, 15);
    public static readonly (int X, int Y) TradingPostLocal = (18, 15);

    /// <summary>
    /// A new company, on a seat it chose (when that is free, or a House keeps it warm) or the first free one; Legacy from
    /// earlier seasons brings starting gold, titles and a manager slot
    /// </summary>
    public Company CreateCompany(long userId, string name, string color, int legacyLevel = 0, IEnumerable<string>? titles = null, EstateKey? seat = null, string? code = null)
    {
        // A seat code brings the newcomer to the seat its crew holds for it, a crew link next to the crew
        var held = code != null ? HoldByCode(code) : null;
        var linked = held == null && code != null ? CrewByInvite(code) : null;
        if (held is { } invite)
        {
            seat = invite.Hold.Seat;
        }
        else if (linked != null)
        {
            seat = SeatNearCrew(linked, code!);
        }
        _kit = null;
        var estate = seat is { } chosen && TakeSeat(chosen, code) is { } free ? free : FindFreeEstate(kit: true);
        var now = Now;
        var company = new Company
        {
            Id = NextCompanyId++,
            UserId = userId,
            Name = name.Trim(),
            Color = color,
            Gold = StartingGold + LegacyGold(legacyLevel),
            LegacyLevel = legacyLevel,
            Titles = new HashSet<string>(titles ?? []),
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

        AddYardGate(company, estate);

        // The stranger next door (step 6): alone, in the small station beside a crew's, it shares their Yard and project
        if (code == null && SmallMode && estate.Station.IsSmall && CrewCornerNextTo(estate.Station) != null)
        {
            company.SeatedNextDoor = true;
        }
        // The first-comer: alone on an empty server, it reads once who comes next
        if (!_companies.Values.Any(t => !t.IsNpc && t.Id != company.Id && IsActive(t)))
        {
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Neighbour,
                $"You're the first in {WorldNames.District(estate.District)}. The next player who comes alone will live next to you."));
        }

        foreach (var neighbour in _companies.Values.Where(t => t.District == company.District && t.Id != company.Id && !t.IsNpc))
        {
            neighbour.Report.NewNeighbours.Add(company.Name);
            neighbour.Touch();
            Changes.Events.Add(new WorldEvent(neighbour.Id, WorldEventKind.Neighbour, $"{company.Name} moved into your district"));
        }

        AddBuilding(company, "hq", estate.Origin.Offset(HqLocal.X, HqLocal.Y), finished: true);
        AddBuilding(company, "tradingPost", estate.Origin.Offset(TradingPostLocal.X, TradingPostLocal.Y), finished: true);
        if (Generator.DealtLand)
        {
            GiveLandKit(company);
        }
        else
        {
            HandOverKit(company);
        }
        company.BuiltTypes.Clear();
        if (held is { } joined && HoldOn(joined.Hold.Seat) is not null && KeyOf(estate) == joined.Hold.Seat && CrewMembers(joined.Crew).Count() < MaxCrewSize)
        {
            company.CrewId = joined.Crew.Id;
            company.KnownAs = joined.Hold.FriendName;
            RemoveHold(joined.Crew, joined.Hold);
            AnnounceArrival(joined.Crew, company);
        }
        else if (linked != null)
        {
            JoinCrew(company.Id, code!);
        }
        if (SurveysEnabled)
        {
            Survey(estate);
        }
        StartProbation(company);
        Changes.Companies.Add(company.Id);
        return company;
    }

    /// <summary>An estate's Yard gate (v1.2 §4): where its owner's pipes meet the station's Yard</summary>
    private void AddYardGate(Company company, Estate estate)
    {
        var pos = estate.YardGate;
        _conductors[pos] = new Conductor { Pos = pos, Kind = ConductorKind.Gate, Owner = company.Id };
        MarkDirty(pos);
        Changes.Tiles.Add(pos);
    }

    /// <summary>Takes an estate's Yard gate away (the estate is packed or given up)</summary>
    private void RemoveYardGate(Estate estate)
    {
        var pos = estate.YardGate;
        if (_conductors.Remove(pos, out var gate))
        {
            Deleted.Add(gate);
            MarkDirty(pos);
            Changes.Tiles.Add(pos);
        }
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
        if (definition.Unlock.Collection is { } collection && !company.Achievements.Contains(CollectionKey(collection)))
        {
            reason = $"{definition.Name} is the reward of a collection.";
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
        var footprint = new List<TilePos>();
        for (var dy = 0; dy < definition.Size; dy++)
        {
            for (var dx = 0; dx < definition.Size; dx++)
            {
                footprint.Add(pos.Offset(dx, dy));
            }
        }

        var deep = footprint.Any(WorldLayout.IsDeep);
        if (deep && definition.Layer == BuildingLayer.Surface)
        {
            return CommandResult.Fail($"{definition.Name} can't be built in the Deep Level.");
        }
        if (!deep && definition.Layer == BuildingLayer.Deep)
        {
            return CommandResult.Fail($"{definition.Name} is built in the Deep Level.");
        }
        if (deep)
        {
            return CanPlaceDeep(estate, definition, footprint, ignoreBuilding);
        }
        if (definition.Kind == BuildingKind.DeepLift && footprint.Select(WorldLayout.Deep).Any(t => _buildingAt.ContainsKey(t)))
        {
            return CommandResult.Fail("Something is in the way down in the Deep Level.");
        }

        foreach (var tile in footprint)
        {
            if (OutpostAt(tile) is { } outpost)
            {
                if (outpost.Owner != company.Id)
                {
                    return CommandResult.Fail("This is someone else's outpost.");
                }
            }
            else if (WorldLayout.ZoneAt(tile.X, tile.Y).Kind == ZoneKind.Yard)
            {
                return CommandResult.Fail("Nobody builds in the Yard.");
            }
            else if (OwnEstateAt(company, tile) is not { } own)
            {
                return CommandResult.Fail("You can only build on your own estates and your outposts.");
            }
            else if (!own.IsUnlocked(own.ParcelAt(tile)))
            {
                return CommandResult.Fail("This parcel is still locked. Buy it first.");
            }
            // A building may stand on its owner's own pipe: the pipe runs on under it, so a factory set into a line
            // doesn't split it (run 2: Ana and Cleo removed a tile to set a factory in, and lost half their income)
            if ((_buildingAt.TryGetValue(tile, out var other) && other != ignoreBuilding)
                || (_conductors.TryGetValue(tile, out var under) && !PipeMayRunUnder(company, definition, under)))
            {
                return CommandResult.Fail(_conductors.TryGetValue(tile, out var pipe) && pipe.Owner != company.Id
                    ? "Someone else's pipe runs here."
                    : "Something is already standing here.");
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
            return CommandResult.Fail(Generator.DealtLand ? $"{definition.Name} needs the old forest nearby: its trees are marked on the Deposits map." : $"{definition.Name} needs a forest nearby.");
        }
        return CommandResult.Ok;
    }

    /// <summary>Whether a building may stand on a pipe tile, the pipe running on under it: its owner's own plain pipe, for a building that isn't part of the pipe itself</summary>
    private bool PipeMayRunUnder(Company company, BuildingDefinition definition, Conductor conductor) =>
        conductor.Kind == ConductorKind.Pipe && conductor.Owner == company.Id && !definition.IsConductor;

    /// <summary>Deep buildings stand in the dug out caverns under your own estate</summary>
    private CommandResult CanPlaceDeep(Estate estate, BuildingDefinition definition, List<TilePos> footprint, long? ignoreBuilding)
    {
        foreach (var tile in footprint)
        {
            if (!InEstateOrBelow(estate, tile))
            {
                return CommandResult.Fail("In the Deep Level you build only under your own estate.");
            }
            if ((_buildingAt.TryGetValue(tile, out var other) && other != ignoreBuilding) || _conductors.ContainsKey(tile))
            {
                return CommandResult.Fail("Something is already standing here.");
            }
            if (Tile(tile).Terrain != TerrainType.Cavern)
            {
                return CommandResult.Fail("Dig out this parcel first.");
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
                // Dealt land: only the old forest's marked trees (the wood of a seat that was dealt it)
                if (Tile(tile) is { Terrain: TerrainType.Forest } info && (!Generator.DealtLand || info.Deposit == "wood") && !_buildingAt.ContainsKey(tile))
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

    /// <summary>
    /// Builds a building on every spot of a painted row or area, in order, until the gold runs out (spots where it can't
    /// stand are left out); returns how many were built, the gold they cost, and why the rest wasn't
    /// </summary>
    public (int Placed, double Gold, string? Error) PlaceBuildings(long companyId, string type, IReadOnlyList<TilePos> spots)
    {
        if (Company(companyId) is not { } company)
        {
            return (0, 0, "Unknown company.");
        }
        var before = company.Gold;
        var placed = 0;
        string? error = null;
        foreach (var spot in spots.Distinct().Take(100))
        {
            var result = PlaceBuilding(companyId, type, spot);
            if (result.Success)
            {
                placed++;
            }
            else if (result.Error is { } reason && (reason.StartsWith("You need", StringComparison.Ordinal) || placed == 0 && error == null))
            {
                error = reason;
                if (reason.StartsWith("You need", StringComparison.Ordinal))
                {
                    break;
                }
            }
        }
        return (placed, Math.Round(before - company.Gold, 2), placed == spots.Count ? null : error);
    }

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
        GainXp(company, Content.Progression.XpReward("building") + (company.BuiltTypes.Add(type) ? Content.Progression.XpReward("firstBuildingOfType") : 0), "buildings", type);
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
        if (!definition.Buildable)
        {
            return CommandResult.Fail("Demolish the Deep Lift on the surface.");
        }

        var refund = DemolishRefund(building);
        RemoveBuilding(building);
        company.Gold += refund;
        // The advisor doesn't suggest it again for a week (F3): the minute of the world it went
        company.Stats[$"demolished:{building.Type}"] = (LastTick - DateTime.UnixEpoch).TotalMinutes;
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>Whether a company demolished a building of this type in the last days (the advisor then doesn't suggest it)</summary>
    public bool DemolishedRecently(Company company, string type, double days = 7) =>
        company.Stats.TryGetValue($"demolished:{type}", out var minute) && (LastTick - DateTime.UnixEpoch).TotalMinutes - minute < days * 24 * 60;

    /// <summary>Half the gold back, all of it with a Recycler (rearranging and evolving the estate costs nothing then)</summary>
    public double DemolishRefund(Building building) =>
        Math.Round(Content.Building(building.Type).Cost.Gold * (HasBuilding(building.Owner, BuildingKind.Recycler) ? 1 : 0.5) * ComplexMembers(building));

    private void RemoveBuilding(Building building)
    {
        if (!_buildings.ContainsKey(building.Id))
        {
            return;
        }
        // A Deep Lift takes its shaft along
        if (Content.Building(building.Type).Kind == BuildingKind.DeepLift && !WorldLayout.IsDeep(building.Position) && LiftPartner(building) is { } shaft)
        {
            RemoveBuilding(shaft);
        }
        // The other end of a teleporter stops
        if (Content.Building(building.Type).Kind == BuildingKind.Teleporter && (TeleportReceiver(building) ?? TeleportSender(building)) is { } partner)
        {
            MarkNeedsSolve(partner.Id);
        }
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
        if (ComplexMembers(building) > 1)
        {
            return CommandResult.Fail("A complex can't be moved.");
        }
        if (definition.Kind == BuildingKind.DeepLift)
        {
            return CommandResult.Fail("A Deep Lift can't be moved.");
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
        // A complex upgrades all its members; Foreman Rick
        return Math.Round(total * ComplexMembers(building) * ManagerUpgradeFactor(Company(building.Owner)), 2);
    }

    /// <summary>How many levels the company can afford right now, priced like the upgrade (a complex's members, Foreman Rick)</summary>
    public int AffordableLevels(Company company, Building building)
    {
        var definition = Content.Building(building.Type);
        var factor = ComplexMembers(building) * ManagerUpgradeFactor(company);
        var gold = company.Gold;
        var levels = 0;
        while (building.Level + levels < Content.Progression.MaxBuildingLevel)
        {
            var cost = Content.Progression.UpgradeCost(definition, building.Level + levels) * factor;
            if (cost > gold + 1e-6)
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
        if (!definition.Buildable)
        {
            return CommandResult.Fail("Upgrade the Deep Lift on the surface.");
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
        AddStat(company, "levelsBought", levels);
        building.Touch();
        MarkDirty(building);
        foreach (var tile in building.Footprint())
        {
            Changes.Tiles.Add(tile);
        }
        var milestones = Content.Progression.Milestones.Count(t => t > from && t <= building.Level);
        GainXp(company, levels * Content.Progression.XpReward("upgradeLevel") + milestones * Content.Progression.XpReward("milestone"), "upgrades", building.Type);
        return CommandResult.Ok;
    }

    /// <summary>Whether a building can be upgraded by one more level now (its kind, built, below the maximum)</summary>
    public bool CanUpgrade(Building building)
    {
        var definition = Content.Building(building.Type);
        return definition.Kind is not (BuildingKind.Hq or BuildingKind.Gate or BuildingKind.Decoration) && definition.Buildable
            && !building.UnderConstruction && building.Level < Content.Progression.MaxBuildingLevel;
    }

    /// <summary>
    /// Upgrades several buildings at once (the selection, v1.1 §12): each by <paramref name="levelsEach"/>, or, with a
    /// budget, one level at a time always the cheapest next one (so their levels stay even) until the budget or the gold
    /// runs out. Returns the levels bought and the gold spent.
    /// </summary>
    public (int Levels, double Gold, string? Error) UpgradeMany(long companyId, IReadOnlyCollection<long> ids, int levelsEach, double? budget = null)
    {
        if (Company(companyId) is not { } company)
        {
            return (0, 0, "Unknown company.");
        }
        var buildings = ids.Distinct().Select(Building).OfType<Building>().Where(t => t.Owner == companyId && CanUpgrade(t)).ToList();
        if (buildings.Count == 0)
        {
            return (0, 0, "Nothing there can be upgraded.");
        }
        var gold = company.Gold;
        var levels = 0;
        if (budget is { } limit)
        {
            var left = Math.Min(limit, company.Gold);
            for (var i = 0; i < 5000; i++)
            {
                var next = buildings.Where(CanUpgrade).Select(t => (Building: t, Cost: UpgradeCost(t, 1))).OrderBy(t => t.Cost).FirstOrDefault();
                if (next.Building == null || next.Cost > left + 1e-9 || !Upgrade(companyId, next.Building.Id, 1).Success)
                {
                    break;
                }
                left -= next.Cost;
                levels++;
            }
        }
        else
        {
            // Cheapest first: when the gold doesn't reach for all, the most of them get their levels
            foreach (var building in buildings.OrderBy(t => UpgradeCost(t, 1)))
            {
                var each = Math.Min(levelsEach, Content.Progression.MaxBuildingLevel - building.Level);
                if (each > 0 && Upgrade(companyId, building.Id, each).Success)
                {
                    levels += each;
                }
            }
        }
        return levels == 0 ? (0, 0, "Not enough gold for any of them.") : (levels, Math.Round(gold - company.Gold, 2), null);
    }

    /// <summary>Buys a parcel of one of the company's estates (0 is the first estate, then the branches)</summary>
    public CommandResult BuyParcel(long companyId, int parcel, int estateIndex = 0)
    {
        if (Company(companyId) is not { } company || EstatesOf(company).ElementAtOrDefault(estateIndex) is not { } estate)
        {
            return CommandResult.Fail("You don't have that estate.");
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
        GainXp(company, Content.Progression.XpReward("parcel"), "parcels", "parcel");
        return CommandResult.Ok;
    }

    /// <summary>The cost and the Company Level needed for the next parcel</summary>
    public (double Cost, int Level) NextParcel(Company company, Estate? estate = null)
    {
        estate ??= EstateOf(company);
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
            if (WorldLayout.IsDeep(tile))
            {
                // Down in the Deep Level: only in your own caverns
                if (!estate.Contains(WorldLayout.Surface(tile)))
                {
                    return CommandResult.Fail("In the Deep Level pipes stay under your own estate.");
                }
                if (IsOccupied(tile))
                {
                    return CommandResult.Fail("Something is already there.");
                }
                if (Tile(tile).Terrain != TerrainType.Cavern)
                {
                    return CommandResult.Fail("Dig out this parcel first.");
                }
                cost += pipe.Gold;
                continue;
            }
            var zone = WorldLayout.ZoneAt(tile.X, tile.Y);
            switch (zone.Kind)
            {
                case ZoneKind.Estate:
                    // Pipes may cross parcels you haven't bought yet, so the Yard gate can be reached from the start
                    if (OwnEstateAt(company, tile) == null)
                    {
                        return CommandResult.Fail("You can't lay pipes on someone else's estate. Trade through your Yard gate.");
                    }
                    break;
                case ZoneKind.Wilderness:
                    // Around an outpost
                    if (OutpostAt(tile) is { } outpost && outpost.Owner != companyId)
                    {
                        return CommandResult.Fail("This is someone else's outpost.");
                    }
                    break;
                case ZoneKind.Yard:
                    return CommandResult.Fail("The Yard is shared: pipe to your Yard gate, the game does the rest.");
                default:
                    return CommandResult.Fail("Pipes go on your estate (the corridors carry links, which the game lays).");
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
        AddStat(company, "pipes", newTiles.Count);
        return CommandResult.Ok;
    }

    /// <summary>
    /// Raises a company's own pipes to a better tier where they lie (the pipe tool drawn over them, the network view's
    /// Upgrade): each tile costs what a new pipe of that tier costs
    /// </summary>
    public CommandResult UpgradePipes(long companyId, IReadOnlyList<TilePos> tiles, int tier)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
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
        var upgrade = tiles.Distinct()
            .Select(t => _conductors.TryGetValue(t, out var conductor) ? conductor : null)
            .OfType<Conductor>()
            .Where(t => t.Kind == ConductorKind.Pipe && t.Owner == companyId && t.Tier < tier)
            .ToList();
        if (upgrade.Count == 0)
        {
            return CommandResult.Fail($"Those pipes are already {pipe.Name}s or better.");
        }
        var payment = Pay(company, pipe.Gold * upgrade.Count, pipe.Materials.ToDictionary(t => t.Key, t => t.Value * upgrade.Count));
        if (!payment.Success)
        {
            return payment;
        }
        foreach (var conductor in upgrade)
        {
            conductor.Tier = tier;
            conductor.Touch();
            MarkDirty(conductor.Pos);
            Changes.Tiles.Add(conductor.Pos);
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

    /// <summary>A company changed outside a command (admin tools): save it and tell its player</summary>
    public void CompanyChanged(Company company)
    {
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkCompanyNetworksDirty(company.Id);
    }

    // ---------------------------------------------------------------- Experience

    /// <summary>XP for a company, recorded by where it came from (see Company.XpLog)</summary>
    public void GainXp(Company company, double xp, string source, string item)
    {
        if (xp <= 0)
        {
            return;
        }
        if (!company.XpLog.TryGetValue(source, out var items))
        {
            company.XpLog[source] = items = new Dictionary<string, double>();
        }
        items[item] = items.GetValueOrDefault(item) + xp;
        if (company.Tailwind > 1)
        {
            // Far behind the people you live with: the Tailwind's share is booked on its own
            var tailwind = xp * (company.Tailwind - 1);
            if (!company.XpLog.TryGetValue("tailwind", out var boost))
            {
                company.XpLog["tailwind"] = boost = new Dictionary<string, double>();
            }
            boost["Tailwind"] = boost.GetValueOrDefault("Tailwind") + tailwind;
            xp += tailwind;
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
