using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>What a House does for its station (v1.2 §9): contact is positive only, there is no rival</summary>
public enum HouseKind
{
    /// <summary>A steady producer of its land's good</summary>
    Reliable,

    /// <summary>Works that buy what the station makes and sell their products to the hub</summary>
    Collector,

    /// <summary>A producer that grows with the station's players</summary>
    Grower,

    /// <summary>Works short of one good: they buy it only from the station (the Yard panel says what they need)</summary>
    NeighbourInNeed,
}

/// <summary>
/// Houses (v1.2 §9): named NPC companies with a ⚙ badge that keep the seats nobody has taken warm, so a station never
/// feels empty and a newcomer always has someone to trade with. They are as strong as the station's players (their
/// buildings' level is the median level of its active humans less three, at least five), trade at the House prices
/// within the one NPC cap, and when a player takes the seat they move out and leave a starter kit: a few of their
/// buildings, piped to the Yard gate already.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>The estates of a district that get a House (spread out, the ones players get last); in small mode every free seat gets one</summary>
    public static readonly int[] TenantSlots = [23, 20, 17, 14, 11, 8, 5, 2];

    public const double TenantGold = 1_000_000_000;

    /// <summary>Below this many active companies the server is in small mode (v1.2 §11): Houses fill every empty seat</summary>
    public int SmallModeBelow { get; set; } = 50;

    /// <summary>A starter kit: at most this many of the House's buildings, at most this level</summary>
    public const int KitBuildings = 4;
    public const int KitMinBuildings = 3;
    public const int KitLevel = 5;

    private const int MinHouseLevel = 5;

    private sealed record TenantPlan(string Name, string Color, string[] Buildings, bool TradingPost);

    private static HouseKind KindFor(int index) => (HouseKind)(index % 4);

    private static TenantPlan PlanFor(EstateSpecialty specialty, HouseKind kind, int slot) => kind switch
    {
        HouseKind.Collector => slot % 2 == 0
            ? new TenantPlan("Ironworks", "#8e9aaf", ["smallFactory", "smallFactory"], true)
            : new TenantPlan("Brickworks", "#b5655a", ["brickworks", "brickworks"], true),
        HouseKind.NeighbourInNeed => slot % 2 == 0
            ? new TenantPlan("Sawmill", "#a07850", ["sawmill", "sawmill", "windMoily"], false)
            : new TenantPlan("Glassworks", "#6ab0b8", ["glassworks", "windMoily", "windMoily"], false),
        _ => specialty switch
        {
            EstateSpecialty.WindyHeights => new TenantPlan("Power", "#d4b84a", ["windMoily", "windMoily", "windMoily"], false),
            EstateSpecialty.SunnyFlats => new TenantPlan("Solar", "#e0a040", ["solarPanel", "solarPanel", "solarPanel", "solarPanel"], false),
            EstateSpecialty.Riverside => new TenantPlan("Waterworks", "#5a9ad4", ["waterPump", "waterPump", "windMoily"], false),
            EstateSpecialty.CopperGulch => new TenantPlan("Copper Co", "#c47a4a", ["copperMine", "copperMine", "windMoily"], false),
            EstateSpecialty.CoalValley => new TenantPlan("Coal Co", "#6a6a70", ["coalPit", "coalPit", "windMoily"], false),
            EstateSpecialty.OldForest => new TenantPlan("Timber", "#7a9a4a", ["lumberCamp", "lumberCamp", "windMoily"], false),
            _ => new TenantPlan("Stone", "#9a9a90", ["quarry", "windMoily", "windMoily"], false),
        },
    };

    /// <summary>A tenant's trading post is never the bottleneck</summary>
    private const double TenantThroughput = 1_000_000;

    /// <summary>What a House's trading post buys from the hub: only a Collector's inputs (a Neighbour in need waits for the station)</summary>
    private HashSet<string> TenantInputs(Company tenant) =>
        tenant.House is HouseKind.NeighbourInNeed ? [] : BuildingsOf(tenant.Id).SelectMany(t => Content.Building(t.Type).In.Keys).ToHashSet();

    /// <summary>Whether the server is in small mode: fewer than 50 active companies (only where Houses are on)</summary>
    public bool SmallMode => TenantsEnabled && ActivePlayers() < SmallModeBelow;

    /// <summary>How strong a station's Houses are: the median level of its active humans less three (at least five)</summary>
    public int HouseStrength(StationRef station)
    {
        var levels = BigStation(station).SelectMany(StationSeats)
            .Select(t => Estate(t.District, t.Slot)?.OwnerId is { } owner ? Company(owner) : null)
            .OfType<Company>()
            .Where(t => !t.IsNpc && IsActive(t))
            .Select(t => t.Level)
            .Order()
            .ToList();
        var median = levels.Count == 0 ? 0 : levels[levels.Count / 2];
        return Math.Clamp(median - 3, MinHouseLevel, Content.Progression.MaxBuildingLevel);
    }

    /// <summary>Gives every open district its Houses (at startup, and for worlds that existed before them)</summary>
    public void EnsureTenants()
    {
        foreach (var district in _openDistricts.ToList())
        {
            CreateTenants(district);
        }
    }

    /// <summary>The seats of a district that get a House: the usual few, and in small mode or when a crew holds it every free seat</summary>
    private void CreateTenants(DistrictCoord district)
    {
        var small = SmallMode;
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
        {
            var index = Array.IndexOf(TenantSlots, slot);
            var seat = new EstateKey(district, slot);
            if (!_estates.TryGetValue((district, slot), out var estate) || estate.OwnerId != null)
            {
                continue;
            }
            if (index >= 0 || small || HoldOn(seat) != null)
            {
                CreateTenant(estate, index >= 0 ? index : slot);
            }
        }
    }

    private void CreateTenant(Estate estate, int index)
    {
        var profile = Generator.Estate(estate.District, estate.Slot);
        var kind = KindFor(index);
        var plan = PlanFor(profile.Primary, kind, estate.Slot);
        var name = $"{WorldNames.District(estate.District)} {plan.Name}";
        if (_companies.Values.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            name = $"{name} {(char)('A' + estate.Slot % 26)}";
        }
        var strength = HouseStrength(estate.Station);
        var id = NextCompanyId++;
        var company = new Company
        {
            Id = id,
            UserId = -id,
            Name = name,
            Color = plan.Color,
            Gold = TenantGold,
            Level = strength,
            CreatedAt = Now,
            LastSeenAt = Now,
            IsNpc = true,
            House = kind,
            District = estate.District,
            Slot = estate.Slot,
            Report = new CompanyReport { Since = Now },
        };
        _companies[company.Id] = company;
        estate.OwnerId = company.Id;
        estate.UnlockedParcels = (1 << WorldLayout.ParcelCount) - 1;
        estate.Touch();
        Changes.Estates.Add((estate.District, estate.Slot));
        Changes.Companies.Add(company.Id);

        AddYardGate(company, estate);

        var types = plan.Buildings.ToList();
        if (kind == HouseKind.Grower)
        {
            types.AddRange(Enumerable.Repeat(plan.Buildings[0], GrowerExtra(strength)));
        }
        foreach (var type in types.Append(plan.TradingPost ? "tradingPost" : null).OfType<string>())
        {
            AddHouseBuilding(company, estate, type, strength);
        }
    }

    /// <summary>A Grower adds a building for every five levels of strength above the least (at most three)</summary>
    private static int GrowerExtra(int strength) => Math.Min(3, (strength - MinHouseLevel) / 5);

    private Building? AddHouseBuilding(Company house, Estate estate, string type, int level)
    {
        var center = estate.Origin.Offset(WorldLayout.EstateSize / 2, WorldLayout.EstateSize / 2);
        if (FindTenantSpot(house, estate, Content.Building(type), center) is not { } spot)
        {
            return null;
        }
        var building = AddBuilding(house, type, spot, finished: true);
        if (Content.Building(type).Kind == BuildingKind.Producer)
        {
            building.Level = level;
        }
        // Every building gets a pipe to the Yard gate, so the station's residents trade with it
        LayTenantPipe(house, estate, building, estate.YardGate);
        return building;
    }

    /// <summary>Where a newcomer's HQ and trading post will stand (local tiles, with room around them): Houses keep it free</summary>
    private static bool InHqArea(int localX, int localY, int size) =>
        localX + size - 1 >= HqLocal.X - 2 && localX <= TradingPostLocal.X + 2 && localY + size - 1 >= HqLocal.Y - 2 && localY <= HqLocal.Y + 3;

    /// <summary>
    /// The free spot nearest to the estate's centre where a House's building may stand: in the start area first (so it
    /// can be a newcomer's starter kit), never where the newcomer's HQ and trading post go
    /// </summary>
    private TilePos? FindTenantSpot(Company company, Estate estate, BuildingDefinition definition, TilePos center)
    {
        var candidates = new List<TilePos>();
        for (var y = 3; y < WorldLayout.EstateSize - 3 - definition.Size; y++)
        {
            for (var x = 3; x < WorldLayout.EstateSize - 3 - definition.Size; x++)
            {
                if (!InHqArea(x, y, definition.Size))
                {
                    candidates.Add(estate.Origin.Offset(x, y));
                }
            }
        }
        bool InStart(TilePos pos) => FootprintIn(estate, pos, definition.Size, Content.Progression.StartParcels);
        foreach (var pos in candidates.OrderBy(t => InStart(t) ? 0 : 1).ThenBy(t => Math.Abs(t.X - center.X) + Math.Abs(t.Y - center.Y)))
        {
            // Room around it for the pipes
            if (BorderTiles(pos, definition.Size).Any(IsOccupied) || !CanPlace(company, definition, pos).Success)
            {
                continue;
            }
            return pos;
        }
        return null;
    }

    /// <summary>Whether a building's footprint lies wholly on these parcels of an estate</summary>
    private static bool FootprintIn(Estate estate, TilePos pos, int size, IReadOnlyList<int> parcels)
    {
        for (var dy = 0; dy < size; dy++)
        {
            for (var dx = 0; dx < size; dx++)
            {
                if (!parcels.Contains(WorldLayout.ParcelOf(pos.X + dx - estate.Origin.X, pos.Y + dy - estate.Origin.Y)))
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>A pipe from a building to the tile inside a gate (breadth first over free land of the estate)</summary>
    private void LayTenantPipe(Company company, Estate estate, Building building, TilePos gate)
    {
        bool Free(TilePos t) =>
            estate.Contains(t) && (_conductors.TryGetValue(t, out var c) ? c.Owner == company.Id && c.Kind == ConductorKind.Pipe : !_buildingAt.ContainsKey(t))
            && Tile(t).Terrain is not (TerrainType.Water or TerrainType.Mountain)
            && !InHqArea(t.X - estate.Origin.X, t.Y - estate.Origin.Y, 1);

        var previous = new Dictionary<TilePos, TilePos?>();
        var queue = new Queue<TilePos>();
        foreach (var start in building.Border().Where(Free))
        {
            previous[start] = null;
            queue.Enqueue(start);
        }
        while (queue.TryDequeue(out var tile))
        {
            if (Math.Abs(tile.X - gate.X) + Math.Abs(tile.Y - gate.Y) == 1)
            {
                for (TilePos? t = tile; t != null; t = previous[t.Value])
                {
                    if (!_conductors.ContainsKey(t.Value))
                    {
                        _conductors[t.Value] = new Conductor { Pos = t.Value, Tier = 1, Owner = company.Id };
                        MarkDirty(t.Value);
                        Changes.Tiles.Add(t.Value);
                    }
                }
                return;
            }
            foreach (var (dx, dy) in Directions)
            {
                var next = tile.Offset(dx, dy);
                if (!previous.ContainsKey(next) && Free(next))
                {
                    previous[next] = tile;
                    queue.Enqueue(next);
                }
            }
        }
    }

    /// <summary>
    /// Once a day: Houses grow with their station's players (their buildings' level, a Grower's buildings), and the seats
    /// that should have a House get one (in small mode every free seat, and every seat a crew keeps for a friend)
    /// </summary>
    private void UpdateHouses()
    {
        foreach (var house in _companies.Values.Where(t => t.IsNpc).ToList())
        {
            if (EstateOf(house) is not { } estate)
            {
                continue;
            }
            // A tenant from before Houses had kinds: works collect, the others supply
            house.House ??= BuildingsOf(house.Id).Any(t => Content.Building(t.Type).In.Count > 0) ? HouseKind.Collector : HouseKind.Reliable;
            var strength = HouseStrength(estate.Station);
            if (house.Level != strength)
            {
                house.Level = strength;
                house.Touch();
                Changes.Companies.Add(house.Id);
            }
            var buildings = BuildingsOf(house.Id).Where(t => Content.Building(t.Type).Kind == BuildingKind.Producer).ToList();
            foreach (var building in buildings.Where(t => t.Level != strength))
            {
                building.Level = strength;
                building.Touch();
                MarkDirty(building.Position);
                Changes.Tiles.Add(building.Position);
            }
            if (house.House == HouseKind.Grower)
            {
                var plan = PlanFor(Generator.Estate(estate.District, estate.Slot).Primary, HouseKind.Grower, estate.Slot);
                var target = plan.Buildings.Length + GrowerExtra(strength);
                if (buildings.Count < target)
                {
                    AddHouseBuilding(house, estate, plan.Buildings[0], strength);
                }
            }
        }
        foreach (var district in _openDistricts.ToList())
        {
            CreateTenants(district);
        }
    }

    /// <summary>What a House left the newcomer who took its seat: its name and the buildings to hand over</summary>
    private (string House, List<Building> Buildings)? _kit;

    /// <summary>
    /// The House moves out: its buildings, pipes and offers disappear and the estate is free. For a newcomer (kit) a few
    /// of its producers in the start area that need nothing to run stay: the starter kit, handed over (with new pipes to
    /// the gate) when the newcomer's company is founded.
    /// </summary>
    private void EvictTenant(Estate estate, bool kit = false)
    {
        if (estate.OwnerId is not { } ownerId || Company(ownerId) is not { IsNpc: true } tenant)
        {
            return;
        }
        var start = Content.Progression.StartParcels;
        var kept = kit
            ? BuildingsOf(tenant.Id)
                .Where(t => Content.Building(t.Type).Kind == BuildingKind.Producer && Content.Building(t.Type).In.Count == 0
                    && FootprintIn(estate, t.Position, Content.Building(t.Type).Size, start)
                    && !InHqArea(t.Position.X - estate.Origin.X, t.Position.Y - estate.Origin.Y, Content.Building(t.Type).Size))
                .OrderBy(t => t.Id)
                .Take(KitBuildings)
                .ToList()
            : [];
        foreach (var building in BuildingsOf(tenant.Id).Except(kept).ToList())
        {
            RemoveBuilding(building);
        }
        foreach (var conductor in _conductors.Values.Where(t => t.Owner == tenant.Id).ToList())
        {
            MarkDirty(conductor.Pos);
            _conductors.Remove(conductor.Pos);
            Deleted.Add(conductor);
            Changes.Tiles.Add(conductor.Pos);
        }
        foreach (var contract in _contracts.Values.Where(t => t.IsOpen && (t.Seller == tenant.Id || t.Buyer == tenant.Id || t.Proposer == tenant.Id)).ToList())
        {
            contract.State = ContractState.Cancelled;
            ContractChanged(contract);
        }
        foreach (var link in LinksOf(tenant.Id).ToList())
        {
            RemoveLink(link, refund: true);
        }
        _companies.Remove(tenant.Id);
        Deleted.Add(tenant);
        RemoveYardGate(estate);
        estate.OwnerId = null;
        estate.UnlockedParcels = 0;
        estate.Touch();
        Changes.Estates.Add((estate.District, estate.Slot));
        _kit = kit ? (tenant.Name, kept) : null;
    }

    /// <summary>The starter kit goes to the newcomer: the House's buildings (at most level five), piped to the gate</summary>
    private void HandOverKit(Company company)
    {
        if (_kit is not { } kit)
        {
            return;
        }
        _kit = null;
        foreach (var building in kit.Buildings)
        {
            building.Owner = company.Id;
            building.Level = Math.Min(building.Level, KitLevel);
            building.Touch();
            MarkDirty(building.Position);
            Changes.Tiles.Add(building.Position);
        }
        // Piped to the newcomer's Yard gate, so the station trades with it from the first minute
        var estate = EstateOf(company)!;
        foreach (var building in kit.Buildings)
        {
            LayTenantPipe(company, estate, building, estate.YardGate);
        }
        // At least three: what suits the land, of what the newcomer may build already
        var plan = PlanFor(Generator.Estate(estate.District, estate.Slot).Primary, HouseKind.Reliable, estate.Slot);
        for (var i = 0; kit.Buildings.Count < KitMinBuildings && i < KitMinBuildings * 2; i++)
        {
            var type = plan.Buildings[i % plan.Buildings.Length];
            if (!IsUnlocked(company, Content.Building(type), out _))
            {
                type = "windMoily";
            }
            if (AddHouseBuilding(company, estate, type, 1) is { } added)
            {
                kit.Buildings.Add(added);
            }
        }
        if (kit.Buildings.Count == 0)
        {
            return;
        }
        var names = kit.Buildings.GroupBy(t => Content.Building(t.Type).Name)
            .Select(t => t.Count() == 1 ? $"a {t.Key}" : $"{t.Count()} {t.Key}s");
        Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Neighbour,
            $"{kit.House} ⚙ moved out and left you {string.Join(" and ", names)}, piped to your Yard gate already"));
    }
}
