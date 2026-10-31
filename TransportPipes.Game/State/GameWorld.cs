using System.Text.RegularExpressions;
using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <param name="Note">Done, and what the player should know about how (a building placed as a scaffold that waits for its bricks)</param>
public sealed record CommandResult(bool Success, string? Error = null, string? Note = null)
{
    public static readonly CommandResult Ok = new(true);

    public static CommandResult Fail(string error) => new(false, error);

    public static CommandResult Done(string note) => new(true, null, note);
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
    OrderPosted,
}

/// <summary>Something a player should hear about (a toast in the client)</summary>
public sealed record WorldEvent(long Company, WorldEventKind Kind, string Text, long? Building = null, bool? Bell = null)
{
    /// <summary>
    /// Whether it also goes to the bell: only what the player can act on (after run 3, row 11: "a wall of 30+ messages
    /// … I understood little"); levels, research, prices, filled orders and the calendar are toasts and card lines
    /// </summary>
    public bool InBell => Bell ?? Kind is WorldEventKind.Social or WorldEventKind.Neighbour or WorldEventKind.Crew or WorldEventKind.Link
        or WorldEventKind.ContractChanged or WorldEventKind.Corporation or WorldEventKind.StorageFull;
}

/// <summary>What changed in the world since the server last asked (to send to clients)</summary>
public sealed class WorldChanges
{
    public HashSet<TilePos> Tiles { get; } = new();
    public HashSet<long> Companies { get; } = new();
    public HashSet<int> Networks { get; } = new();
    public HashSet<long> Storages { get; } = new();
    public HashSet<(DistrictCoord District, int Slot)> Estates { get; } = new();
    public HashSet<DistrictCoord> Markets { get; } = new();
    public HashSet<long> Crews { get; } = new();

    /// <summary>Legacy for accounts (a town's Launch): the user, the points and a title</summary>
    public List<(long UserId, int Legacy, string? Title)> Legacy { get; } = new();
    /// <summary>Companies whose links were offered, changed or ended</summary>
    public HashSet<long> Links { get; } = new();

    /// <summary>Districts whose hub changed (a Wonder grew)</summary>
    public HashSet<DistrictCoord> Districts { get; } = new();

    public bool Exchange { get; set; }
    public bool Wonders { get; set; }
    public bool Outposts { get; set; }
    public bool Blueprints { get; set; }
    public List<WorldEvent> Events { get; } = new();

    public bool IsEmpty => Tiles.Count == 0 && Companies.Count == 0 && Networks.Count == 0 && Storages.Count == 0 && Estates.Count == 0 && Markets.Count == 0 && Crews.Count == 0 && Legacy.Count == 0 && Links.Count == 0 && Districts.Count == 0 && !Exchange && !Wonders && !Outposts && !Blueprints && Events.Count == 0;
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

    /// <summary>A company's plots: its one plot (none while it moves)</summary>
    public IEnumerable<Estate> EstatesOf(Company company) => EstateOf(company) is { } estate ? [estate] : [];

    /// <summary>The company's plot a tile belongs to (the Deep Level under it counts), or null</summary>
    public Estate? OwnEstateAt(Company company, TilePos pos) =>
        EstateOf(company) is { } estate && estate.Contains(WorldLayout.Surface(pos)) ? estate : null;

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

    public int CopiesOwned(long companyId, string type) => BuildingsOf(companyId).Count(t => t.Type == type);

    /// <summary>
    /// A company's buildings (from the owner index: every call scanned all the world's buildings, and it is called
    /// hundreds of times a tick on a busy server)
    /// </summary>
    public IEnumerable<Building> BuildingsOf(long companyId) => _byOwner.TryGetValue(companyId, out var own) ? own.Values : [];

    /// <summary>The buildings by owner (kept with _buildings: added, removed, given to another owner)</summary>
    private readonly Dictionary<long, Dictionary<long, Building>> _byOwner = new();

    private void Index(Building building)
    {
        if (!_byOwner.TryGetValue(building.Owner, out var own))
        {
            _byOwner[building.Owner] = own = new Dictionary<long, Building>();
        }
        own[building.Id] = building;
    }

    private void Unindex(Building building)
    {
        if (_byOwner.TryGetValue(building.Owner, out var own) && own.Remove(building.Id) && own.Count == 0)
        {
            _byOwner.Remove(building.Owner);
        }
    }

    /// <summary>A building goes to another owner</summary>
    private void GiveBuilding(Building building, long owner)
    {
        Unindex(building);
        building.Owner = owner;
        Index(building);
    }

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
        Index(building);
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


    /// <summary>
    /// A new company (1.4 §6), on the plot it chose when that is free for it (an open Yard, or a closed one with a
    /// resident's "Play next to me" link), else where Pick for me puts it; a plot it chose in a closed Yard becomes a
    /// request. It starts from nothing: its HQ, its trading post next to the Yard gate and its starting gold. Legacy from
    /// earlier seasons brings starting gold, titles and a manager slot.
    /// </summary>
    public Company CreateCompany(long userId, string name, string color, int legacyLevel = 0, IEnumerable<string>? titles = null, EstateKey? seat = null, string? code = null)
    {
        if (seat is { } wanted)
        {
            OpenDistrict(wanted.District);
        }
        // A friend's link without a plot: a free one at their Yard, when there is one
        if (seat == null && code != null && NextToMeOf(code) is { } friend && EstateOf(friend) is { } home)
        {
            seat = StationSeats(home.Station).Where(t => SeatFree(t, null, code)).Select(t => (EstateKey?)t).FirstOrDefault();
        }
        // Newcomers start in an Era-1 town (1.4 §11)
        var chosen = seat is { } s && SeatFree(s, null, code) && TownEra(s.District) == 1 ? s : PickForMe();
        var estate = Estate(chosen.District, chosen.Slot)!;
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
        estate.UnlockedParcels = WorldLayout.StartMask(estate.Slot);
        estate.Touch();
        Changes.Estates.Add((estate.District, estate.Slot));

        AddYardGate(company, estate);
        if (code != null && LinkOpens(code, estate.Station) && !YardOpen(estate.Station))
        {
            NextToMeOf(code)!.NextToMeUsed = true;
        }
        SettleInYard(company, estate);

        // The town hears who moved in (the Yard's crew that it's next door, from SettleInYard)
        foreach (var neighbour in _companies.Values.Where(t => t.District == company.District && t.Id != company.Id && t.CrewId != company.CrewId))
        {
            neighbour.Touch();
            Changes.Events.Add(new WorldEvent(neighbour.Id, WorldEventKind.Neighbour, $"{company.Name} moved into {WorldNames.Station(estate.Station)}, another Yard of your town"));
        }

        var (hqX, hqY) = WorldLayout.HqLocal(estate.Slot);
        var (postX, postY) = WorldLayout.TradingPostLocal(estate.Slot);
        AddBuilding(company, "hq", estate.Origin.Offset(hqX, hqY), finished: true);
        AddBuilding(company, "tradingPost", estate.Origin.Offset(postX, postY), finished: true);
        company.BuiltTypes.Clear();
        // A plot it chose in a closed Yard: it plays here and asks there
        if (seat is { } asked && asked != chosen && Estate(asked.District, asked.Slot) is { OwnerId: null } && !YardOpen(WorldLayout.StationOf(asked.District, asked.Slot)))
        {
            RequestPlot(company.Id, asked);
        }
        // Steel Pipes come with Era 2 to everyone, a newcomer too
        GrantResearch(company);
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

    /// <summary>A new building; price: what the copy cost (its levels cost from it), by default the next copy's price</summary>
    private Building AddBuilding(Company company, string type, TilePos pos, bool finished, double? price = null)
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
            Price = price ?? BuildCost(company, definition),
            ConstructionEndsAt = finished || definition.BuildSeconds <= 0 ? null : Now.AddSeconds(definition.BuildSeconds),
        };
        _buildings[building.Id] = building;
        Index(building);
        foreach (var tile in building.Footprint())
        {
            // A building stands on a forest without felling it: its trees are hidden under it and grow back when it
            // goes (playtest: a Lumber Camp placed on the forest took it, and moved away the land stayed bare)
            _buildingAt[tile] = building.Id;
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
        // A town's Era opens its buildings (1.4 §11)
        if (definition.Unlock.Era > TownEra(company.District))
        {
            reason = $"{definition.Name} needs Era {definition.Unlock.Era}.";
            return false;
        }
        // Research everyone gets at a Company Level opens the building before it, as the Copper Pipes do (playtest: the Big
        // Factory's research didn't, and it needed Level 12 as well)
        var early = definition.Unlock.Research is { } own && Content.Research.TryGetValue(own, out var tech) && tech.Grant > 0 ? tech : null;
        if (company.Level < definition.Unlock.Level && !(early != null && HasResearch(company, early.Id)))
        {
            reason = early != null
                ? $"{definition.Name} needs Company Level {definition.Unlock.Level}, or its research in your Lab sooner."
                : $"{definition.Name} needs Company Level {definition.Unlock.Level}.";
            return false;
        }
        if (definition.Unlock.Research is { } research && !HasResearch(company, research) && !(early != null && company.Level >= early.Grant))
        {
            reason = $"{definition.Name} needs research in your Lab.";
            return false;
        }
        // One Lab (the owner, 2026-10-31: a second one added nothing; its levels make research faster)
        if (definition.Kind == BuildingKind.Lab && BuildingsOf(company.Id).Any(t => Content.Building(t.Type).Kind == BuildingKind.Lab))
        {
            reason = "You have a Lab: one is enough, and its levels make research faster.";
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
            return CommandResult.Fail("You don't have a plot.");
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
                // Workshops and the Depot at the Yard's ends (1.4 §7)
                if (WorkshopBlocker(company, definition, footprint) is { } blocker)
                {
                    return CommandResult.Fail(blocker);
                }
            }
            else if (WorldLayout.ZoneAt(tile.X, tile.Y).Kind == ZoneKind.Outskirts)
            {
                if (!OwnsOutskirts(company, tile))
                {
                    return CommandResult.Fail(OutskirtsParcelAt(Estate, tile) is { } parcel && parcel.Plot == estate
                        ? "This outskirts parcel is set aside for you: buy it first."
                        : "These outskirts are set aside for another plot.");
                }
            }
            else if (OwnEstateAt(company, tile) is not { } own)
            {
                return CommandResult.Fail("You can only build on your own plots and your outposts.");
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
        }
        return SiteRule(definition, pos);
    }

    /// <summary>
    /// What the land itself asks of a building at a spot, whoever owns it and whatever stands there: its terrain, its
    /// deposit, water beside it, the forest around it. The build command and every "you can make it" use this one rule
    /// (run 3: the project said Ana "can make" wood on dealt land where no marked tree stood, and "Not on water" refused
    /// a pump the card had sent to "the water")
    /// </summary>
    public CommandResult SiteRule(BuildingDefinition definition, TilePos pos)
    {
        for (var dy = 0; dy < definition.Size; dy++)
        {
            for (var dx = 0; dx < definition.Size; dx++)
            {
                var tile = pos.Offset(dx, dy);
                // The Yard's ends are laid out like grass for its workshops (1.4 §7)
                var terrain = Tile(tile).Terrain is TerrainType.Yard && InWorkshops(tile) ? TerrainType.Grass : Tile(tile).Terrain;
                if (!definition.Placement.Terrain.Contains(terrain))
                {
                    return CommandResult.Fail(definition.Placement.Shore && terrain == TerrainType.Water
                        ? $"{definition.Name} stands beside the water, not on it."
                        : $"{definition.Name} can't be built on {terrain.ToString().ToLowerInvariant()}.");
                }
            }
        }
        if (definition.Placement.Deposit is { } deposit)
        {
            var best = Enumerable.Range(0, definition.Size * definition.Size).Select(t => Tile(pos.Offset(t % definition.Size, t / definition.Size)))
                .Where(t => DepositMatches(deposit, t.Deposit)).Select(t => t.Richness).DefaultIfEmpty(0).Max();
            if (best == 0)
            {
                return CommandResult.Fail($"{definition.Name} needs {(DepositName(deposit) is var name && "aeiou".Contains(name[0]) ? "an" : "a")} {DepositName(deposit)} deposit.");
            }
            if (best < definition.Placement.MinRichness)
            {
                return CommandResult.Fail($"{definition.Name} needs a deposit of at least {definition.Placement.MinRichness} stars.");
            }
        }
        if (definition.Placement.Shore && !BorderTiles(pos, definition.Size).Any(t => Tile(t).Terrain == TerrainType.Water))
        {
            return CommandResult.Fail($"{definition.Name} has to stand next to water.");
        }
        if (definition.Placement.Forest && ForestAround(pos, definition.Size) < 3)
        {
            return CommandResult.Fail($"{definition.Name} needs a forest nearby.");
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
                return CommandResult.Fail("In the Deep Level you build only under your own plot.");
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
                return CommandResult.Fail($"{definition.Name} needs {(DepositName(deposit) is var name && "aeiou".Contains(name[0]) ? "an" : "a")} {DepositName(deposit)} deposit.");
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

    /// <summary>The tiles a building of a size covers from its corner</summary>
    public static IEnumerable<TilePos> Footprint(TilePos pos, int size)
    {
        for (var dy = 0; dy < size; dy++)
        {
            for (var dx = 0; dx < size; dx++)
            {
                yield return pos.Offset(dx, dy);
            }
        }
    }

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
                if (Tile(tile) is { Terrain: TerrainType.Forest } && !_buildingAt.ContainsKey(tile))
                {
                    count++;
                }
            }
        }
        return count;
    }

    // ---------------------------------------------------------------- Costs

    /// <summary>What a company's next copy of a building costs (1.4 §9: 7% more for every copy it owns; the Workshop Monument)</summary>
    public double BuildCost(Company company, BuildingDefinition definition) =>
        Math.Round(Content.Progression.BuildCost(definition, CopiesOwned(company.Id, definition.Id)) * CostFactor(company));

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
        foreach (var storage in BuildingsOf(companyId).Where(t => t.Contents.Count != 0))
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
                foreach (var storage in BuildingsOf(company.Id).Where(t => t.Contents.ContainsKey(resource)).OrderBy(t => t.Id))
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
        AddStat(company, "goldSpent", gold);
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
        var cost = BuildCost(company, definition);
        // Short of its materials (not its gold): it stands as a scaffold that wants them (after run 3, step 2)
        if (company.Gold >= cost - 1e-9 && definition.Cost.Materials.Count > 0
            && definition.Cost.Materials.Any(t => Materials(company.Id).GetValueOrDefault(t.Key) < t.Value - 1e-9))
        {
            // Said as it is (after run 4: "Big Factory is being built", and it waited for its bricks to the end)
            var line = MaterialsLine(company, definition);
            var site = PlaceSite(company, definition, pos, cost);
            company.BuiltTypes.Add(type);
            RecordPurchase(company, PurchaseKind.Build, site, 0, cost);
            return CommandResult.Done($"Your {definition.Name} stands as a scaffold. {line?.Text}".TrimEnd());
        }
        var payment = Pay(company, cost, definition.Cost.Materials);
        if (!payment.Success)
        {
            return payment;
        }

        var placed = AddBuilding(company, type, pos, finished: false, cost);
        company.BuiltTypes.Add(type);
        RecordPurchase(company, PurchaseKind.Build, placed, 0, cost);
        if (definition.Kind == BuildingKind.Storage && StorageKeepsText(company) is { } keeps)
        {
            // Placing a Storage says what it holds back (after run 3, row 9: "Nobody warned me that storing means not selling")
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.StorageFull, $"Your {definition.Name} will keep {keeps} for your next buildings and pipes; the rest goes on to your trading post", placed.Id, Bell: false));
        }
        return CommandResult.Ok;
    }

    /// <summary>What a company's Storages keep back of its goods ("500 bricks and 100 copper ingots"; null: nothing)</summary>
    public string? StorageKeepsText(Company company)
    {
        var made = BuildingsOf(company.Id).Where(t => !t.UnderConstruction).SelectMany(t => Content.Building(t.Type).Out.Keys).ToHashSet();
        var keeps = made
            .Select(t => (Resource: t, Amount: StorageNeed(company, t)))
            .Where(t => t.Amount >= 1)
            .OrderByDescending(t => t.Amount * BasePrice(t.Resource))
            .Select(t => $"{t.Amount:N0} {GoodsWord(t.Resource)}")
            .Take(3)
            .ToList();
        return keeps.Count == 0 ? null : keeps.Count == 1 ? keeps[0] : $"{string.Join(", ", keeps.Take(keeps.Count - 1))} and {keeps[^1]}";
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

        if (IsSite(building))
        {
            return CancelSite(companyId, buildingId);
        }
        var refund = DemolishRefund(building);
        DropWants(company, building.Id);
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

    /// <summary>Half what the copy cost back, all of it with a Recycler (rearranging and evolving the estate costs nothing then)</summary>
    public double DemolishRefund(Building building) =>
        Math.Round(CopyPrice(building) * (HasBuilding(building.Owner, BuildingKind.Recycler) ? 1 : 0.5) * ComplexMembers(building));

    /// <summary>What a copy cost to build (1.4 §9: its levels cost from it); an old building without one, its type's price</summary>
    public double CopyPrice(Building building) => building.Price > 0 ? building.Price : Content.Building(building.Type).Cost.Gold;

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
            RegrowForest(tile);
            Changes.Tiles.Add(tile);
        }
        _buildings.Remove(building.Id);
        Unindex(building);
        Deleted.Add(building);
    }

    /// <summary>
    /// A forest a building felled before buildings stood on forests without felling them grows back once it goes: the
    /// land's own forest, made grass (no terraforming makes a forest grass, only that)
    /// </summary>
    private void RegrowForest(TilePos tile)
    {
        if (_terrainOverrides.TryGetValue(tile, out var now) && now == TerrainType.Grass && Generator.Tile(tile).Terrain == TerrainType.Forest)
        {
            // Saved as a forest (the store keeps what changed, it doesn't forget a tile)
            _terrainOverrides[tile] = TerrainType.Forest;
        }
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
            RegrowForest(tile);
            Changes.Tiles.Add(tile);
        }
        building.X = pos.X;
        building.Y = pos.Y;
        foreach (var tile in building.Footprint())
        {
            _buildingAt[tile] = building.Id;
            Changes.Tiles.Add(tile);
        }
        // A scaffold stays one: it still waits for its materials (playtest: moved, it came up without them, and its wants
        // stayed behind)
        if (definition.BuildSeconds > 0 && !IsSite(building))
        {
            building.ConstructionEndsAt = Now.AddSeconds(Math.Max(5, definition.BuildSeconds * 0.1));
        }
        building.Touch();
        MarkDirty(building);
        return CommandResult.Ok;
    }

    /// <summary>The gold of a building's next levels (from the level it was bought up to, its goods still coming or not)</summary>
    public double UpgradeCost(Building building, int levels)
    {
        var from = BoughtLevel(building);
        double total = 0;
        for (var level = from; level < from + levels; level++)
        {
            total += Content.Progression.UpgradeCost(CopyPrice(building), level);
        }
        // A complex upgrades all its members; Foreman Rick; the Workshop Monument
        return Math.Round(total * ComplexMembers(building) * ManagerUpgradeFactor(Company(building.Owner)) * CostFactor(Company(building.Owner)), 2);
    }

    /// <summary>How many levels the company can afford right now, priced like the upgrade (a complex's members, Foreman Rick)</summary>
    public int AffordableLevels(Company company, Building building)
    {
        var factor = ComplexMembers(building) * ManagerUpgradeFactor(company) * CostFactor(company);
        var from = BoughtLevel(building);
        var gold = company.Gold;
        var levels = 0;
        while (from + levels < Content.Progression.MaxBuildingLevel)
        {
            var cost = Content.Progression.UpgradeCost(CopyPrice(building), from + levels) * factor;
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
        var from = BoughtLevel(building);
        levels = Math.Min(levels, Content.Progression.MaxBuildingLevel - from);
        if (levels <= 0)
        {
            return CommandResult.Fail("This building is at its maximum level.");
        }
        var cost = UpgradeCost(building, levels);
        var payment = Pay(company, cost, new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }

        AddStat(company, "levelsBought", levels);
        // From level 11 a step also takes goods (1.4 §9): the building works at its old level until they're in
        AddLevelSteps(company, building, from, levels);
        building.Touch();
        MarkFlowsChanged(building);
        foreach (var tile in building.Footprint())
        {
            Changes.Tiles.Add(tile);
        }
        RecordPurchase(company, PurchaseKind.Upgrade, building, levels, cost);
        return CommandResult.Ok;
    }

    /// <summary>Whether a building can be upgraded by one more level now (its kind, built, below the maximum)</summary>
    public bool CanUpgrade(Building building)
    {
        var definition = Content.Building(building.Type);
        return definition.Kind is not (BuildingKind.Hq or BuildingKind.Gate or BuildingKind.Decoration) && definition.Buildable
            && !building.UnderConstruction && BoughtLevel(building) < Content.Progression.MaxBuildingLevel;
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
                var each = Math.Min(levelsEach, Content.Progression.MaxBuildingLevel - BoughtLevel(building));
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
            return CommandResult.Fail("You don't have that plot.");
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
        var extra = estate.UnlockedCount - WorldLayout.StartParcelCount;
        var levelNeeded = Content.Progression.ParcelLevels[Math.Min(extra, Content.Progression.ParcelLevels.Count - 1)];
        if (company.Level < levelNeeded)
        {
            return CommandResult.Fail($"Your next parcel needs Company Level {levelNeeded}.");
        }
        var cost = Math.Round(Content.Progression.ParcelCost(extra) * CostFactor(company));
        var payment = Pay(company, cost, new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }
        AddStat(company, "landGold", cost);
        AddMoment(company, "land", $"Bought parcel {extra + 1} of {Content.Progression.ParcelLevels.Count}", true);

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
        return CommandResult.Ok;
    }

    /// <summary>The cost and the Company Level needed for the next parcel</summary>
    public (double Cost, int Level) NextParcel(Company company, Estate? estate = null)
    {
        estate ??= EstateOf(company);
        var extra = (estate?.UnlockedCount ?? WorldLayout.StartParcelCount) - WorldLayout.StartParcelCount;
        if (extra >= Content.Progression.ParcelLevels.Count)
        {
            return (0, int.MaxValue);
        }
        return (Math.Round(Content.Progression.ParcelCost(extra) * CostFactor(company)), Content.Progression.ParcelLevels[extra]);
    }

    public CommandResult PlacePipes(long companyId, IReadOnlyList<TilePos> tiles, int tier = 1)
    {
        if (Company(companyId) is not { } company || EstateOf(company) is not { } estate)
        {
            return CommandResult.Fail("You don't have a plot.");
        }
        var pipe = Content.PipeTiers.FirstOrDefault(t => t.Tier == tier);
        if (pipe == null)
        {
            return CommandResult.Fail("Unknown pipe.");
        }
        if (pipe.Era > YourEra(company) || (pipe.Research != null && !HasResearch(company, pipe.Research)))
        {
            return CommandResult.Fail($"{pipe.Name}s are not unlocked yet.");
        }

        // Your own pipes on the way are there already, and so is your Yard gate: a pipe runs on through it (1.4: the
        // gate stands by the trading post, so a drag from a building to the post often crosses it)
        var newTiles = tiles.Distinct().Where(t => !(_conductors.TryGetValue(t, out var existing) && existing.Owner == companyId
            && existing.Kind is ConductorKind.Pipe or ConductorKind.Gate)).ToList();
        double cost = 0;
        foreach (var tile in newTiles)
        {
            if (WorldLayout.IsDeep(tile))
            {
                // Down in the Deep Level: only in your own caverns
                if (!estate.Contains(WorldLayout.Surface(tile)))
                {
                    return CommandResult.Fail("In the Deep Level pipes stay under your own plot.");
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
                        return CommandResult.Fail("You can't lay pipes on someone else's plot. Trade through your Yard gate.");
                    }
                    break;
                case ZoneKind.Outskirts:
                    if (!OwnsOutskirts(company, tile))
                    {
                        return CommandResult.Fail("Only on your own outskirts.");
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
                    return CommandResult.Fail("Pipes go on your plot (the roads carry links, which the game lays).");
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
        if (pipe.Era > YourEra(company) || (pipe.Research != null && !HasResearch(company, pipe.Research)))
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
        // Tiles that wait for this pipe already (playtest: every click on Upgrade asked again, five Copper Pipe wants on
        // the same 13 tiles, each paid)
        var waiting = PipeTilesWaiting(company, tier);
        upgrade.RemoveAll(t => waiting.Contains(t.Pos));
        if (upgrade.Count == 0)
        {
            return CommandResult.Fail($"Those pipes wait for their {pipe.Name} already: what it wants is on its way (Yard › What you're waiting for).");
        }
        var materials = pipe.Materials.ToDictionary(t => t.Key, t => t.Value * upgrade.Count);
        // Short of its materials (a Copper Pipe's ingots), not its gold: the pipe wants them (after run 3, step 3: Cleo's
        // Copper Pipe click did nothing, its 28 ingots had to come from her own Storage)
        if (company.Gold >= pipe.Gold * upgrade.Count - 1e-9 && materials.Any(t => Materials(company.Id).GetValueOrDefault(t.Key) < t.Value - 1e-9))
        {
            return WantPipes(company, upgrade, pipe, materials);
        }
        var payment = Pay(company, pipe.Gold * upgrade.Count, materials);
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
        AddStat(company, "pipeTiers", upgrade.Count);
        TidyPipeWants(company);
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
        MarkFlowsChanged(building);
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

    // ---------------------------------------------------------------- Gold earned and the Company Level

    /// <summary>
    /// Gold a company earned (1.4 §14): what the market and other players paid it, orders, rewards and expeditions, never
    /// refunds, the start gold or buy-backs. The caller adds the gold; this counts it for the Company Level, recorded by
    /// where it came from (see Company.EarnedLog).
    /// </summary>
    public void CountEarned(Company company, double gold, string source, string item)
    {
        if (gold <= 0)
        {
            return;
        }
        if (!company.EarnedLog.TryGetValue(source, out var items))
        {
            company.EarnedLog[source] = items = new Dictionary<string, double>();
        }
        items[item] = items.GetValueOrDefault(item) + gold;
        company.LifetimeEarnings += gold;
        CheckCompanyLevel(company);
    }

    /// <summary>The Company Level follows the gold earned in all; it never goes down</summary>
    public void CheckCompanyLevel(Company company)
    {
        var level = Content.Progression.LevelForEarned(company.LifetimeEarnings);
        if (level > company.Level)
        {
            for (var reached = company.Level + 1; reached <= level; reached++)
            {
                AddMoment(company, "level", $"Company Level {reached}", reached % 5 == 0);
            }
            company.Report.LevelsGained += level - company.Level;
            company.Level = level;
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.LevelUp, $"Company Level {level}!"));
            MentorRewards(company);
            GrantResearch(company);
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
    }
}
