using System.Collections.Concurrent;

namespace TransportPipes.Game.World;

public enum EstateSpecialty
{
    WindyHeights,
    SunnyFlats,
    Riverside,
    CopperGulch,
    CoalValley,
    OldForest,
    StoneQuarry,
}

/// <summary>Everything the world says about a tile before anything is built on it</summary>
public readonly record struct TileInfo(TerrainType Terrain, string? Deposit, int Richness, double Wind, double Sun)
{
    public bool IsBuildableTerrain => Terrain is not (TerrainType.Water or TerrainType.Mountain or TerrainType.Road or TerrainType.City or TerrainType.Yard);
}

public sealed record EstateProfile(DistrictCoord District, int Slot, TilePos Origin, EstateSpecialty Primary, EstateSpecialty? Secondary, TileInfo[,] Tiles)
{
    public IEnumerable<EstateSpecialty> Specialties => Secondary is { } secondary ? [Primary, secondary] : [Primary];

    public TileInfo Local(int x, int y) => Tiles[x, y];
}

/// <summary>A decorative structure of a hub city (drawn by the client, not a player building)</summary>
public sealed record HubStructure(string Type, int X, int Y, int Size);

/// <summary>
/// Generates the terrain of the whole world deterministically from the season seed. Estates get guaranteed basics
/// (an iron deposit and a hill in the start parcels) and one or two specialties, evenly spread over each district.
/// </summary>
public sealed class WorldGenerator
{
    public static readonly EstateSpecialty[] AllSpecialties = Enum.GetValues<EstateSpecialty>();

    private static readonly int[] StartParcels = [5, 6, 9, 10];
    private static readonly int[] OuterParcels = [0, 1, 2, 3, 4, 7, 8, 11, 12, 13, 14, 15];

    private readonly ConcurrentDictionary<(DistrictCoord, int), EstateProfile> _estates = new();
    private readonly ConcurrentDictionary<DistrictCoord, EstateSpecialty[][]> _districtSpecialties = new();

    public int Seed { get; }

    public WorldGenerator(int seed)
    {
        Seed = seed;
    }

    public TileInfo Tile(int x, int y) => Tile(new TilePos(x, y));

    public TileInfo Tile(TilePos pos)
    {
        if (WorldLayout.IsDeep(pos))
        {
            return DeepTile(pos.X, pos.Y);
        }
        var zone = WorldLayout.ZoneAt(pos.X, pos.Y);
        return zone.Kind switch
        {
            ZoneKind.Estate => Estate(zone.District, zone.Slot).Local(zone.LocalX, zone.LocalY),
            ZoneKind.Hub => new TileInfo(TerrainType.City, null, 0, 1, 1),
            ZoneKind.Corridor => new TileInfo(TerrainType.Road, null, 0, 1, 1),
            ZoneKind.Yard => new TileInfo(TerrainType.Yard, null, 0, 1, 1),
            _ => Wilderness(pos.X, pos.Y),
        };
    }

    public EstateProfile Estate(DistrictCoord district, int slot) =>
        _estates.GetOrAdd((district, slot), key => GenerateEstate(key.Item1, key.Item2));

    // ---------------------------------------------------------------- Climate

    private double BaseWind(int x, int y) => 0.75 + 0.45 * Noise.Fractal(Seed + 101, x, y, 26);

    private double BaseSun(int x, int y) => 0.9 + 0.2 * Noise.Value(Seed + 202, x, y, 30);

    private static double Clamp(double v, double min, double max) => Math.Max(min, Math.Min(max, v));

    private TileInfo WithClimate(int x, int y, TerrainType terrain, string? deposit, int richness, double windBonus = 0, double sunBonus = 0)
    {
        var wind = BaseWind(x, y) + windBonus + terrain switch { TerrainType.Hills => 0.3, TerrainType.Beach => 0.15, TerrainType.Forest => -0.15, _ => 0 };
        var sun = BaseSun(x, y) + sunBonus + terrain switch { TerrainType.Desert => 0.45, TerrainType.Forest => -0.25, _ => 0 };
        return new TileInfo(terrain, deposit, richness, Math.Round(Clamp(wind, 0.6, 1.6), 2), Math.Round(Clamp(sun, 0.7, 1.5), 2));
    }

    // ---------------------------------------------------------------- The Deep Level

    private static readonly string[] DeepDeposits = ["ironOre", "copperOre", "coal", "magma", "ironOre", "copperOre", "rareEarth"];

    /// <summary>
    /// Solid rock (dug out by the players) with rich veins of every metal (★3–★5), pools of magma and a little rare
    /// earth. No sun and no wind down here.
    /// </summary>
    private TileInfo DeepTile(int x, int y)
    {
        var vein = Noise.Fractal(Seed + 501, x, y, 6);
        if (vein < 0.62)
        {
            return new TileInfo(TerrainType.Rock, null, 0, 0, 0);
        }
        var kind = Noise.Value(Seed + 502, x, y, 11);
        var deposit = DeepDeposits[Math.Min(DeepDeposits.Length - 1, (int)(kind * DeepDeposits.Length))];
        var richness = vein > 0.74 ? 5 : vein > 0.68 ? 4 : 3;
        return new TileInfo(TerrainType.Rock, deposit, richness, 0, 0);
    }

    // ---------------------------------------------------------------- Wilderness

    private TileInfo Wilderness(int x, int y)
    {
        var elevation = Noise.Fractal(Seed + 1, x, y, 18);
        var moisture = Noise.Fractal(Seed + 2, x, y, 22);
        TerrainType terrain;
        if (elevation < 0.27) terrain = TerrainType.Water;
        else if (elevation < 0.31) terrain = TerrainType.Beach;
        else if (elevation > 0.8) terrain = TerrainType.Mountain;
        else if (elevation > 0.68) terrain = TerrainType.Hills;
        else if (moisture < 0.28) terrain = TerrainType.Desert;
        else if (moisture > 0.68) terrain = TerrainType.Forest;
        else terrain = TerrainType.Grass;

        string? deposit = null;
        var richness = 0;
        if (terrain is not (TerrainType.Water or TerrainType.Mountain))
        {
            // Rare rich deposits in clusters of 6x6 cells
            var cx = WorldLayout.FloorDiv(x, 6);
            var cy = WorldLayout.FloorDiv(y, 6);
            var roll = Noise.Unit(Seed, 7, cx, cy);
            if (roll < 0.16)
            {
                var centerX = cx * 6 + 3;
                var centerY = cy * 6 + 3;
                var radius = 1.2 + Noise.Unit(Seed, 8, cx, cy) * 1.6;
                if (Math.Abs(x - centerX) + Math.Abs(y - centerY) <= radius * 1.4)
                {
                    var kind = Noise.Unit(Seed, 9, cx, cy);
                    deposit = terrain == TerrainType.Desert && kind < 0.5 ? "oil"
                        : terrain == TerrainType.Hills && kind < 0.25 ? "rareEarth"
                        : kind < 0.08 ? "geothermal"
                        : kind < 0.4 ? "copperOre"
                        : kind < 0.6 ? "coal"
                        : kind < 0.8 ? "ironOre"
                        : "stone";
                    richness = 3 + (int)(Noise.Unit(Seed, 10, cx, cy) * 3);
                }
            }
        }
        return WithClimate(x, y, terrain, deposit, richness);
    }

    // ---------------------------------------------------------------- Estates

    private EstateSpecialty[][] DistrictSpecialties(DistrictCoord district) =>
        _districtSpecialties.GetOrAdd(district, d =>
        {
            // Every specialty appears about equally often as the primary specialty in a district
            var random = new SeededRandom(Noise.Hash(Seed, 1000, d.X, d.Y));
            var primaries = new List<EstateSpecialty>();
            while (primaries.Count < WorldLayout.EstatesPerDistrict)
            {
                primaries.AddRange(AllSpecialties);
            }
            primaries = primaries.Take(WorldLayout.EstatesPerDistrict).ToList();
            random.Shuffle(primaries);
            return primaries.Select(primary =>
            {
                if (random.NextDouble() < 0.5)
                {
                    return new[] { primary };
                }
                var others = AllSpecialties.Where(t => t != primary).ToList();
                return new[] { primary, others[random.Next(others.Count)] };
            }).ToArray();
        });

    private EstateProfile GenerateEstate(DistrictCoord district, int slot)
    {
        var origin = WorldLayout.EstateOrigin(district, slot);
        var specialties = DistrictSpecialties(district)[slot];
        var random = new SeededRandom(Noise.Hash(Seed, 2000, district.X, district.Y, slot));
        var size = WorldLayout.EstateSize;

        var terrain = new TerrainType[size, size];
        var deposit = new string?[size, size];
        var richness = new int[size, size];
        var windBonus = new double[size, size];
        var sunBonus = new double[size, size];

        // Base: grass with some natural variation (keeps the start parcels mostly open)
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var wx = origin.X + x;
                var wy = origin.Y + y;
                var start = StartParcels.Contains(WorldLayout.ParcelOf(x, y));
                var forest = Noise.Fractal(Seed + 3, wx, wy, 7);
                var hills = Noise.Fractal(Seed + 4, wx, wy, 9);
                terrain[x, y] = forest > (start ? 0.86 : 0.76) ? TerrainType.Forest
                    : hills > (start ? 0.88 : 0.8) ? TerrainType.Hills
                    : TerrainType.Grass;
            }
        }

        var freeParcels = OuterParcels.ToList();
        random.Shuffle(freeParcels);
        int TakeParcel()
        {
            var parcel = freeParcels[0];
            freeParcels.RemoveAt(0);
            return parcel;
        }

        void Rect(int x0, int y0, int w, int h, Action<int, int> action)
        {
            for (var y = Math.Max(0, y0); y < Math.Min(size, y0 + h); y++)
            {
                for (var x = Math.Max(0, x0); x < Math.Min(size, x0 + w); x++)
                {
                    action(x, y);
                }
            }
        }

        void Blob(int cx, int cy, double radius, Action<int, int, double> action)
        {
            var r = (int)Math.Ceiling(radius);
            Rect(cx - r, cy - r, r * 2 + 1, r * 2 + 1, (x, y) =>
            {
                var d = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d <= radius)
                {
                    action(x, y, d);
                }
            });
        }

        void DepositCluster(int parcel, string resource, int stars, int width, int height)
        {
            var (px, py) = WorldLayout.ParcelOrigin(parcel);
            var x0 = px + random.Next(1, Math.Max(2, WorldLayout.ParcelSize - width));
            var y0 = py + random.Next(1, Math.Max(2, WorldLayout.ParcelSize - height));
            Rect(x0, y0, width, height, (x, y) =>
            {
                if (terrain[x, y] is TerrainType.Water)
                {
                    return;
                }
                deposit[x, y] = resource;
                // The centre of a cluster is one star richer
                var center = x > x0 && x < x0 + width - 1 && y > y0 && y < y0 + height - 1;
                richness[x, y] = Math.Min(5, stars + (center ? 1 : 0));
            });
        }

        void Lake(int parcel, double radius)
        {
            var (px, py) = WorldLayout.ParcelOrigin(parcel);
            var cx = px + 4;
            var cy = py + 4;
            Blob(cx, cy, radius + 1.4, (x, y, d) =>
            {
                terrain[x, y] = d <= radius ? TerrainType.Water : TerrainType.Beach;
                deposit[x, y] = null;
            });
        }

        void Fill(int parcel, TerrainType type, double coverage, Action<int, int>? extra = null)
        {
            var (px, py) = WorldLayout.ParcelOrigin(parcel);
            Rect(px, py, WorldLayout.ParcelSize, WorldLayout.ParcelSize, (x, y) =>
            {
                var n = Noise.Value(Seed + 5, origin.X + x, origin.Y + y, 3);
                if (n < coverage)
                {
                    terrain[x, y] = type;
                    extra?.Invoke(x, y);
                }
            });
        }

        // Guaranteed basics in the start parcels: an iron deposit and a windy hill
        var startOrder = StartParcels.ToList();
        random.Shuffle(startOrder);
        DepositCluster(startOrder[0], "ironOre", 2, 3, 3);
        {
            var (px, py) = WorldLayout.ParcelOrigin(startOrder[1]);
            Rect(px + 1, py + 1, 4, 3, (x, y) => terrain[x, y] = TerrainType.Hills);
        }

        // Specialties
        foreach (var specialty in specialties)
        {
            switch (specialty)
            {
                case EstateSpecialty.WindyHeights:
                    foreach (var parcel in new[] { TakeParcel(), TakeParcel() })
                    {
                        Fill(parcel, TerrainType.Hills, 0.85, (x, y) => windBonus[x, y] = 0.35);
                    }
                    break;
                case EstateSpecialty.SunnyFlats:
                    foreach (var parcel in new[] { TakeParcel(), TakeParcel() })
                    {
                        Fill(parcel, TerrainType.Desert, 0.9);
                    }
                    break;
                case EstateSpecialty.Riverside:
                    Lake(TakeParcel(), 2.6);
                    Lake(TakeParcel(), 1.6);
                    break;
                case EstateSpecialty.CopperGulch:
                    {
                        var parcel = TakeParcel();
                        DepositCluster(parcel, "copperOre", 4, 4, 4);
                        var (px, py) = WorldLayout.ParcelOrigin(parcel);
                        Rect(px + 6, py, 2, 2, (x, y) => { terrain[x, y] = TerrainType.Mountain; deposit[x, y] = null; });
                        DepositCluster(TakeParcel(), "copperOre", 2, 3, 2);
                    }
                    break;
                case EstateSpecialty.CoalValley:
                    DepositCluster(TakeParcel(), "coal", 4, 4, 4);
                    DepositCluster(TakeParcel(), "stone", 2, 3, 3);
                    break;
                case EstateSpecialty.OldForest:
                    foreach (var parcel in new[] { TakeParcel(), TakeParcel(), TakeParcel() })
                    {
                        Fill(parcel, TerrainType.Forest, 0.8);
                    }
                    break;
                case EstateSpecialty.StoneQuarry:
                    {
                        var parcel = TakeParcel();
                        Fill(parcel, TerrainType.Hills, 0.6);
                        DepositCluster(parcel, "stone", 4, 4, 4);
                    }
                    break;
            }
        }

        // Small extras every estate gets: a bit of stone, a forest patch, sometimes a pond or a little coal
        DepositCluster(TakeParcel(), "stone", 1, 2, 2);
        {
            var (px, py) = WorldLayout.ParcelOrigin(TakeParcel());
            Blob(px + 4, py + 4, 2.3, (x, y, _) => { if (deposit[x, y] == null) terrain[x, y] = TerrainType.Forest; });
        }
        if (random.NextDouble() < 0.5 && !specialties.Contains(EstateSpecialty.Riverside))
        {
            Lake(TakeParcel(), 1.1);
        }
        if (random.NextDouble() < 0.4 && !specialties.Contains(EstateSpecialty.CoalValley))
        {
            DepositCluster(TakeParcel(), "coal", 1, 2, 2);
        }

        // Keep the HQ spot, the Trading Post spot and the way to the Yard gate open
        Rect(14, 14, 6, 4, (x, y) => { terrain[x, y] = TerrainType.Grass; deposit[x, y] = null; });
        var (gx, gy) = WorldLayout.YardGateLocal(slot);
        void Open(int x, int y)
        {
            if (terrain[x, y] is TerrainType.Water or TerrainType.Mountain)
            {
                terrain[x, y] = TerrainType.Grass;
            }
        }
        // An L from the middle to the gate, and the tiles around the gate
        for (var x = Math.Min(16, gx); x <= Math.Max(16, gx); x++)
        {
            Open(x, 16);
        }
        for (var y = Math.Min(16, gy); y <= Math.Max(16, gy); y++)
        {
            Open(gx, y);
        }
        Rect(gx - 1, gy - 1, 3, 3, Open);

        var tiles = new TileInfo[size, size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                tiles[x, y] = WithClimate(origin.X + x, origin.Y + y, terrain[x, y], deposit[x, y], deposit[x, y] == null ? 0 : richness[x, y], windBonus[x, y], sunBonus[x, y]);
            }
        }

        return new EstateProfile(district, slot, origin, specialties[0], specialties.Length > 1 ? specialties[1] : null, tiles);
    }

    // ---------------------------------------------------------------- Hub cities

    /// <summary>
    /// The layout of a hub city (local coordinates within the hub block). The Capital has the Era Wonder's site where
    /// other hubs have a park.
    /// </summary>
    public IReadOnlyList<HubStructure> HubStructures(DistrictCoord district) =>
    [
        new("marketHall", 13, 13, 3),
        new("wonderSite", 2, 2, 6),
        new("townHall", 24, 3, 3),
        new("exchange", 3, 24, 3),
        new("contractBoard", 18, 13, 2),
        new("corporationQuarter", 22, 22, 8),
        new("tradingHouse", 13, 4, 2),
        new("tradingHouse", 4, 13, 2),
        new("tradingHouse", 26, 13, 2),
        new("tradingHouse", 13, 26, 2),
        district == new DistrictCoord(0, 0) ? new("eraWonderSite", 6, 16, 6) : new("park", 9, 18, 3),
        new("park", 18, 8, 3),
    ];
}
