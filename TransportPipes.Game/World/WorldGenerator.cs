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
    IronHills,
}

/// <summary>Everything the world says about a tile before anything is built on it</summary>
public readonly record struct TileInfo(TerrainType Terrain, string? Deposit, int Richness, double Wind, double Sun)
{
    public bool IsBuildableTerrain => Terrain is not (TerrainType.Water or TerrainType.Mountain or TerrainType.Road or TerrainType.City or TerrainType.Yard);
}

/// <summary>
/// A plot as generated (1.4 §5): what its land has and lacks (one Era-1 basic and one later resource), its strength (a
/// rich ★4 deposit of something it has), and its tiles
/// </summary>
public sealed record EstateProfile(DistrictCoord District, int Slot, TilePos Origin, IReadOnlyList<string> Lacks, string Strength, TileInfo[,] Tiles)
{
    /// <summary>The land resources it has: water, iron ore and wood on every plot, and six of the eight others</summary>
    public IReadOnlyList<string> Goods { get; init; } = [];

    /// <summary>The picker's word for the plot: its strength</summary>
    public EstateSpecialty Primary => WorldGenerator.SpecialtyOf(Strength);

    public EstateSpecialty? Secondary => null;

    public TileInfo Local(int x, int y) => Tiles[x, y];
}

/// <summary>A decorative structure of a hub (drawn by the client, not a player building), hub-local</summary>
public sealed record HubStructure(string Type, int X, int Y, int Size);

/// <summary>
/// Generates the terrain of the whole world deterministically from the season seed (1.4 §5). Every plot has water, iron
/// ore, wood, wind and sun, and lacks one Era-1 basic (copper ore, coal, stone or sand) and one later resource (bauxite,
/// oil, rare earth or titanium ore), dealt per Yard so its four plots lack different ones and any three of them have
/// everything. The start area against the Yard holds what the first hour needs; the twelve parcels around it hold the
/// rest, the plot's rich deposit next to the start area. The later Eras' resources lie on the land from the start.
/// </summary>
public sealed class WorldGenerator
{
    /// <summary>The Era-1 basics a plot may lack (it has the other three)</summary>
    public static readonly string[] Basics = ["copperOre", "coal", "stone", "sand"];

    /// <summary>The later resources a plot may lack: bauxite (Era 2), oil (3), rare earth (4), titanium ore (5)</summary>
    public static readonly string[] Later = ["bauxite", "oil", "rareEarth", "titaniumOre"];

    /// <summary>The land resources every plot has</summary>
    public static readonly string[] Always = ["water", "ironOre", "wood"];

    /// <summary>The land goods a plot may lack: the four basics and the four later resources</summary>
    public static readonly string[] LandGoods = [.. Basics, .. Later];

    /// <summary>The picker's word for a plot by its strength</summary>
    public static EstateSpecialty SpecialtyOf(string good) => good switch
    {
        "copperOre" => EstateSpecialty.CopperGulch,
        "coal" => EstateSpecialty.CoalValley,
        "stone" => EstateSpecialty.StoneQuarry,
        "sand" => EstateSpecialty.Riverside,
        "wood" => EstateSpecialty.OldForest,
        _ => EstateSpecialty.IronHills,
    };

    private readonly ConcurrentDictionary<(DistrictCoord, int), EstateProfile> _estates = new();
    private readonly ConcurrentDictionary<StationRef, (string Basic, string Later, string Strength)[]> _deals = new();

    public int Seed { get; }

    public WorldGenerator(int seed)
    {
        Seed = seed;
    }

    /// <summary>What a plot's land has (the card's "has")</summary>
    public IReadOnlyList<string> LandOf(DistrictCoord district, int slot) => Estate(district, slot).Goods;

    /// <summary>What a plot's land lacks: one basic and one later resource</summary>
    public IReadOnlyList<string> LacksOf(DistrictCoord district, int slot) => Estate(district, slot).Lacks;

    /// <summary>
    /// A Yard's deal: each of its four plots lacks a different basic and a different later resource, and has a rich
    /// deposit of one of the basics it has (or of iron), different from its neighbours' where it can be
    /// </summary>
    private (string Basic, string Later, string Strength)[] Deal(StationRef station) =>
        _deals.GetOrAdd(station, s =>
        {
            var random = new SeededRandom(Noise.Hash(Seed, 3100, s.District.X, s.District.Y, s.Index));
            var basics = Basics.ToList();
            var later = Later.ToList();
            random.Shuffle(basics);
            random.Shuffle(later);
            var deal = new (string Basic, string Later, string Strength)[4];
            var used = new HashSet<string>();
            for (var i = 0; i < 4; i++)
            {
                // A rich deposit of a basic it has and a mine digs (sand lies on beaches: no stars), else iron
                var options = new[] { "copperOre", "coal", "stone", "ironOre" }.Where(t => t != basics[i]).ToList();
                random.Shuffle(options);
                var strength = options.FirstOrDefault(t => !used.Contains(t)) ?? options[0];
                used.Add(strength);
                deal[i] = (basics[i], later[i], strength);
            }
            return deal;
        });

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
            ZoneKind.Outskirts => Outskirts(zone.District, zone.LocalX, zone.LocalY),
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
        var wind = BaseWind(x, y) + windBonus + terrain switch
        {
            TerrainType.Hills or TerrainType.RedHills => 0.3,
            TerrainType.Beach => 0.15,
            TerrainType.Forest => -0.15,
            _ => 0,
        };
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

    // ---------------------------------------------------------------- A plot's land, parcel by parcel

    /// <summary>The 8×8 (or larger) canvas a parcel kind is drawn on</summary>
    private sealed class Canvas(int width, int height, SeededRandom random, Func<int, int, double> noise)
    {
        public readonly TerrainType[,] Terrain = new TerrainType[width, height];
        public readonly string?[,] Deposit = new string?[width, height];
        public readonly int[,] Richness = new int[width, height];
        public readonly double[,] WindBonus = new double[width, height];
        public SeededRandom Random { get; } = random;

        public int Width { get; } = width;
        public int Height { get; } = height;

        public void Rect(int x0, int y0, int w, int h, Action<int, int> action)
        {
            for (var y = Math.Max(0, y0); y < Math.Min(Height, y0 + h); y++)
            {
                for (var x = Math.Max(0, x0); x < Math.Min(Width, x0 + w); x++)
                {
                    action(x, y);
                }
            }
        }

        public void Blob(double cx, double cy, double radius, Action<int, int, double> action)
        {
            var r = (int)Math.Ceiling(radius);
            Rect((int)Math.Floor(cx) - r, (int)Math.Floor(cy) - r, r * 2 + 2, r * 2 + 2, (x, y) =>
            {
                var d = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d <= radius)
                {
                    action(x, y, d);
                }
            });
        }

        /// <summary>A deposit of about w×h tiles in a parcel's area, its centre one star richer</summary>
        public void Cluster(int px, int py, int area, string resource, int stars, int w, int h, Func<int, int, bool>? free = null)
        {
            for (var attempt = 0; attempt < 30; attempt++)
            {
                var x0 = px + Random.Next(1, Math.Max(2, area - w));
                var y0 = py + Random.Next(1, Math.Max(2, area - h));
                var fits = true;
                Rect(x0, y0, w, h, (x, y) => fits &= Deposit[x, y] == null && Terrain[x, y] is not (TerrainType.Water or TerrainType.Mountain) && (free?.Invoke(x, y) ?? true));
                if (!fits && attempt < 29)
                {
                    continue;
                }
                Rect(x0, y0, w, h, (x, y) =>
                {
                    if (Terrain[x, y] is TerrainType.Water or TerrainType.Mountain)
                    {
                        return;
                    }
                    Deposit[x, y] = resource;
                    var centre = x > x0 && x < x0 + w - 1 && y > y0 && y < y0 + h - 1;
                    Richness[x, y] = Math.Min(5, stars + (centre ? 1 : 0));
                });
                return;
            }
        }

        /// <summary>A share of a parcel's tiles turned to a terrain, in patches</summary>
        public void Fill(int px, int py, int area, TerrainType type, double coverage, Action<int, int>? extra = null) =>
            Rect(px, py, area, area, (x, y) =>
            {
                if (noise(x, y) < coverage && Deposit[x, y] == null && Terrain[x, y] != TerrainType.Water)
                {
                    Terrain[x, y] = type;
                    extra?.Invoke(x, y);
                }
            });

        /// <summary>A pond: water with a shore ring of beach (the plot has sand) or grass</summary>
        public void Pond(double cx, double cy, double radius, bool beach)
        {
            Blob(cx, cy, radius + 1.2, (x, y, d) =>
            {
                if (d <= radius)
                {
                    Terrain[x, y] = TerrainType.Water;
                    Deposit[x, y] = null;
                }
                else if (beach && Terrain[x, y] != TerrainType.Water)
                {
                    Terrain[x, y] = TerrainType.Beach;
                }
            });
        }
    }

    /// <summary>What an outer parcel of a plot holds (the model's land, docs/design-notes/v1.4/sim/plot.py)</summary>
    private static readonly string[] OuterKinds = ["rich", "hill", "iron", "basicB", "basicC", "forest", "lake", "redhills", "basicA2", "desert", "grass", "grass2"];

    private EstateProfile GenerateEstate(DistrictCoord district, int slot)
    {
        var origin = WorldLayout.EstateOrigin(district, slot);
        var random = new SeededRandom(Noise.Hash(Seed, 2100, district.X, district.Y, slot));
        var (lacksBasic, lacksLater, strength) = Deal(WorldLayout.StationOf(district, slot))[slot % 4];
        var has = Basics.Where(t => t != lacksBasic).ToList();
        random.Shuffle(has);
        var size = WorldLayout.EstateSize;
        var canvas = new Canvas(size, size, random, (x, y) => Noise.Value(Seed + 5, origin.X + x, origin.Y + y, 3));
        var start = WorldLayout.StartParcels(slot);
        var (gateX, gateY) = WorldLayout.YardGateLocal(slot);
        var (hqX, hqY) = WorldLayout.HqLocal(slot);
        var (postX, postY) = WorldLayout.TradingPostLocal(slot);
        // The HQ, the trading post and the way between them and the gate stay open
        bool Kept(int x, int y) =>
            (x >= hqX - 1 && x <= hqX + 2 && y >= hqY - 1 && y <= hqY + 2) || (Math.Abs(x - postX) <= 1 && Math.Abs(y - postY) <= 1)
            || (Math.Abs(x - gateX) <= 3 && Math.Abs(y - gateY) <= 3);

        // Base: grass, a few trees and hills outside the start area
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var inStart = start.Contains(WorldLayout.ParcelOf(x, y));
                var forest = Noise.Fractal(Seed + 3, origin.X + x, origin.Y + y, 7);
                var hills = Noise.Fractal(Seed + 4, origin.X + x, origin.Y + y, 9);
                canvas.Terrain[x, y] = inStart || Kept(x, y) ? TerrainType.Grass
                    : forest > 0.8 ? TerrainType.Forest
                    : hills > 0.84 ? TerrainType.Hills
                    : TerrainType.Grass;
            }
        }

        // The start area: what the first hour needs. Iron ore and the first basic in two of its parcels, a hill with
        // stronger wind, a pond for water and a patch of forest; sun everywhere
        var starts = start.ToList();
        random.Shuffle(starts);
        var gateParcel = WorldLayout.ParcelOf(gateX, gateY);
        // The gate's parcel keeps the HQ and the post: the deposits go to the others first
        starts = starts.OrderBy(t => t == gateParcel || t == WorldLayout.ParcelOf(hqX, hqY) ? 1 : 0).ToList();
        void InParcel(int parcel, Action<int, int> draw)
        {
            var (px, py) = WorldLayout.ParcelOrigin(parcel);
            draw(px, py);
        }
        InParcel(starts[0], (px, py) => canvas.Cluster(px, py, 8, "ironOre", 2, 3, 3, (x, y) => !Kept(x, y)));
        InParcel(starts[1], (px, py) => Basic(px, py, 8, has[0], 2, start: true));
        InParcel(starts[2], (px, py) => canvas.Rect(px + 1, py + 1, 5, 4, (x, y) =>
        {
            if (!Kept(x, y) && canvas.Deposit[x, y] == null)
            {
                canvas.Terrain[x, y] = TerrainType.Hills;
                canvas.WindBonus[x, y] = 0.2;
            }
        }));
        InParcel(starts[3], (px, py) =>
        {
            var (cx, cy) = (px + 4.0, py + 4.0);
            if (Kept((int)cx, (int)cy))
            {
                (cx, cy) = (px + 2.0, py + 2.0);
            }
            canvas.Pond(cx, cy, 1.3, beach: has.Contains("sand") && has[0] != "sand");
        });
        // A patch of forest along the start area's far edge
        {
            var farParcel = starts.OrderByDescending(t => Distance(WorldLayout.ParcelOrigin(t), (gateX, gateY))).First();
            var (fx, fy) = WorldLayout.ParcelOrigin(farParcel);
            canvas.Blob(fx + 5.5, fy + 5.5, 2.3, (x, y, _) =>
            {
                if (!Kept(x, y) && canvas.Deposit[x, y] == null && canvas.Terrain[x, y] == TerrainType.Grass)
                {
                    canvas.Terrain[x, y] = TerrainType.Forest;
                }
            });
        }

        // The twelve parcels around it: the rich deposit next to the start area, the rest anywhere
        var outer = Enumerable.Range(0, WorldLayout.ParcelCount).Where(t => !start.Contains(t)).ToList();
        random.Shuffle(outer);
        var nextToStart = outer.First(t => Neighbours(t).Any(start.Contains));
        outer.Remove(nextToStart);
        outer.Insert(0, nextToStart);
        for (var i = 0; i < outer.Count; i++)
        {
            var (px, py) = WorldLayout.ParcelOrigin(outer[i]);
            switch (OuterKinds[i])
            {
                case "rich":
                    canvas.Cluster(px, py, 8, strength, 4, 4, 3);
                    break;
                case "hill":
                    canvas.Fill(px, py, 8, TerrainType.Hills, 0.65, (x, y) => canvas.WindBonus[x, y] = 0.3);
                    if (has.Contains("stone"))
                    {
                        canvas.Cluster(px, py, 8, "stone", 2, 4, 3);
                    }
                    if (lacksLater != "rareEarth")
                    {
                        canvas.Cluster(px, py, 8, "rareEarth", 2, 3, 2);
                    }
                    break;
                case "iron":
                    canvas.Cluster(px, py, 8, "ironOre", 3, 4, 3);
                    break;
                case "basicB":
                    Basic(px, py, 8, has[1], 2);
                    break;
                case "basicC":
                    Basic(px, py, 8, has[2], 2);
                    break;
                case "basicA2":
                    Basic(px, py, 8, has[0], 3);
                    break;
                case "forest":
                    canvas.Fill(px, py, 8, TerrainType.Forest, 0.6);
                    break;
                case "lake":
                    canvas.Pond(px + 4, py + 4, 2.0, beach: has.Contains("sand"));
                    break;
                case "redhills":
                    canvas.Fill(px, py, 8, TerrainType.RedHills, 0.35);
                    if (lacksLater != "bauxite")
                    {
                        canvas.Cluster(px, py, 8, "bauxite", 2, 4, 3);
                        canvas.Rect(px, py, 8, 8, (x, y) =>
                        {
                            if (canvas.Deposit[x, y] == "bauxite")
                            {
                                canvas.Terrain[x, y] = TerrainType.RedHills;
                            }
                        });
                    }
                    break;
                case "desert":
                    canvas.Fill(px, py, 8, TerrainType.Desert, 0.9);
                    if (lacksLater != "oil")
                    {
                        canvas.Cluster(px, py, 8, "oil", 2, 3, 3);
                    }
                    break;
                case "grass":
                    if (lacksLater != "titaniumOre")
                    {
                        // Mountains build nothing: titanium ore lies at their feet
                        canvas.Rect(px + 2, py + 2, 2, 2, (x, y) => { canvas.Terrain[x, y] = TerrainType.Mountain; canvas.Deposit[x, y] = null; });
                        canvas.Rect(px + 1, py + 4, 4, 2, (x, y) =>
                        {
                            if (canvas.Terrain[x, y] != TerrainType.Mountain)
                            {
                                canvas.Deposit[x, y] = "titaniumOre";
                                canvas.Richness[x, y] = 2;
                            }
                        });
                    }
                    break;
            }
        }

        // Nothing blocks the HQ, the trading post or the way to the gate
        canvas.Rect(0, 0, size, size, (x, y) =>
        {
            if (Kept(x, y))
            {
                if (canvas.Terrain[x, y] is TerrainType.Water or TerrainType.Mountain or TerrainType.Forest)
                {
                    canvas.Terrain[x, y] = TerrainType.Grass;
                }
                if ((x >= hqX && x <= hqX + 1 && y >= hqY && y <= hqY + 1) || (x == postX && y == postY))
                {
                    canvas.Deposit[x, y] = null;
                }
            }
        });

        var tiles = new TileInfo[size, size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                tiles[x, y] = WithClimate(origin.X + x, origin.Y + y, canvas.Terrain[x, y], canvas.Deposit[x, y],
                    canvas.Deposit[x, y] == null ? 0 : canvas.Richness[x, y], canvas.WindBonus[x, y]);
            }
        }
        var goods = Always.Concat(has).Concat(Later.Where(t => t != lacksLater)).ToList();
        return new EstateProfile(district, slot, origin, [lacksBasic, lacksLater], strength, tiles) { Goods = goods };

        // A basic where it lies: copper ore, coal and stone in deposits, sand on a beach by a pond
        void Basic(int px, int py, int area, string resource, int stars, bool start = false)
        {
            if (resource == "sand")
            {
                var (cx, cy) = (px + area / 2.0, py + area / 2.0);
                if (start && Kept((int)cx, (int)cy))
                {
                    (cx, cy) = (px + 2.5, py + 2.5);
                }
                canvas.Pond(cx, cy, 1.4, beach: true);
                return;
            }
            canvas.Cluster(px, py, area, resource, stars, start ? 3 : 4, 3, (x, y) => !Kept(x, y));
        }
    }

    private static int Distance((int X, int Y) a, (int X, int Y) b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    private static IEnumerable<int> Neighbours(int parcel)
    {
        var (px, py) = (parcel % WorldLayout.ParcelsPerSide, parcel / WorldLayout.ParcelsPerSide);
        foreach (var (x, y) in new[] { (px - 1, py), (px + 1, py), (px, py - 1), (px, py + 1) })
        {
            if (x is >= 0 and < WorldLayout.ParcelsPerSide && y is >= 0 and < WorldLayout.ParcelsPerSide)
            {
                yield return y * WorldLayout.ParcelsPerSide + x;
            }
        }
    }

    // ---------------------------------------------------------------- The outskirts

    /// <summary>What a plot's nine outskirts parcels hold (1.4 §5): every land resource at ★2–★3, its missing basic first</summary>
    private static readonly string[] OutskirtsKinds = ["lacks", "bauxite", "ironOre", "coal", "forest", "stone", "sand", "copperOre", "grass"];

    private readonly ConcurrentDictionary<(DistrictCoord, int, int), TileInfo[,]> _outskirts = new();

    /// <summary>A tile of the outskirts (town-local)</summary>
    private TileInfo Outskirts(DistrictCoord district, int localX, int localY)
    {
        if (WorldLayout.OutskirtsOwner(localX, localY) is not { } owner)
        {
            return Wilderness(WorldLayout.DistrictOriginX(district.X) + localX, WorldLayout.DistrictOriginY(district.Y) + localY);
        }
        var (ox, oy) = WorldLayout.OutskirtsParcels(owner.Slot)[owner.Index];
        var tiles = _outskirts.GetOrAdd((district, owner.Slot, owner.Index), key => GenerateOutskirts(key.Item1, key.Item2, key.Item3));
        return tiles[localX - ox, localY - oy];
    }

    private TileInfo[,] GenerateOutskirts(DistrictCoord district, int slot, int index)
    {
        var (ox, oy) = WorldLayout.OutskirtsParcels(slot)[index];
        var wx = WorldLayout.DistrictOriginX(district.X) + ox;
        var wy = WorldLayout.DistrictOriginY(district.Y) + oy;
        var random = new SeededRandom(Noise.Hash(Seed, 2200, district.X, district.Y, slot, index));
        var size = WorldLayout.ParcelSize;
        var canvas = new Canvas(size, size, random, (x, y) => Noise.Value(Seed + 6, wx + x, wy + y, 3));
        var kind = OutskirtsKinds[index] == "lacks" ? Estate(district, slot).Lacks[0] : OutskirtsKinds[index];
        switch (kind)
        {
            case "forest":
                canvas.Fill(0, 0, size, TerrainType.Forest, 0.55);
                canvas.Pond(1.5, 1.5, 1.2, beach: false);
                break;
            case "sand":
                canvas.Pond(4, 4, 1.6, beach: true);
                break;
            case "bauxite":
                canvas.Fill(0, 0, size, TerrainType.RedHills, 0.4);
                canvas.Cluster(0, 0, size, "bauxite", 3, 4, 3);
                break;
            case "stone":
                canvas.Fill(0, 0, size, TerrainType.Hills, 0.4);
                canvas.Cluster(0, 0, size, "stone", 2, 4, 3);
                break;
            case "grass":
                break;
            default:
                canvas.Cluster(0, 0, size, kind, kind is "ironOre" or "copperOre" ? 3 : 2, 4, 3);
                break;
        }
        var tiles = new TileInfo[size, size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                tiles[x, y] = WithClimate(wx + x, wy + y, canvas.Terrain[x, y], canvas.Deposit[x, y], canvas.Deposit[x, y] == null ? 0 : canvas.Richness[x, y], canvas.WindBonus[x, y]);
            }
        }
        return tiles;
    }

    // ---------------------------------------------------------------- Hubs

    /// <summary>
    /// The layout of a hub (hub-local, 68×52, the roads ending at its plaza): the town's Wonder site in the middle (the
    /// Capital's is the Era Wonder's, with its District Wonder's site in a corner), the market, the town hall, the
    /// Exchange, trading houses, parks; and the town gates where the roads leave the town (hub-relative, outside it)
    /// </summary>
    public IReadOnlyList<HubStructure> HubStructures(DistrictCoord district)
    {
        // Every town builds its own Era Wonder (1.4 §11): its site in the middle of the hub
        var structures = new List<HubStructure>
        {
            new("eraWonderSite", 31, 23, 6),
            new("marketHall", 25, 24, 3),
            new("townHall", 40, 24, 3),
            new("exchange", 32, 17, 3),
            new("contractBoard", 33, 32, 2),
            new("tradingHouse", 14, 10, 2),
            new("tradingHouse", 52, 10, 2),
            new("tradingHouse", 14, 40, 2),
            new("tradingHouse", 52, 40, 2),
            new("park", 56, 3, 3),
            new("park", 6, 44, 3),
            new("park", 57, 44, 3),
        };
        // The statue square: a park until the town has Diamond builders (GameViews.StatueSquare)
        structures.Add(new("park", 5, 4, 3));
        foreach (var gate in WorldLayout.TownGatesLocal)
        {
            structures.Add(new("townGate", gate.X0 - WorldLayout.HubLocal.X0, gate.Y0 - WorldLayout.HubLocal.Y0, Math.Max(gate.Width, gate.Height)));
        }
        return structures;
    }
}
