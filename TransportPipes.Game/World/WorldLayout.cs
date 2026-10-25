namespace TransportPipes.Game.World;

public enum TerrainType
{
    Grass,
    Hills,
    Forest,
    Beach,
    Water,
    Desert,
    Mountain,
    Road,
    City,

    /// <summary>A Yard: a paved plaza between four plots</summary>
    Yard,

    /// <summary>The Deep Level: solid rock, dug out parcel by parcel into caverns</summary>
    Rock,
    Cavern,

    /// <summary>Red hills: where bauxite lies (Era 2); no 3×3 buildings</summary>
    RedHills,

    /// <summary>The outskirts between a town's Yard groups, set aside for its plots (they open with Era 2)</summary>
    Outskirts,
}

public readonly record struct TilePos(int X, int Y)
{
    public TilePos Offset(int dx, int dy) => new(X + dx, Y + dy);

    public override string ToString() => $"<{X},{Y}>";
}

/// <summary>A town (it was called a district): its place in the grid of towns, the Capital at (0,0)</summary>
public readonly record struct DistrictCoord(int X, int Y)
{
    public override string ToString() => $"D({X},{Y})";
}

public enum ZoneKind
{
    Estate,
    Hub,

    /// <summary>A road: the two roads that cross a town (links run along them)</summary>
    Corridor,
    Wilderness,

    /// <summary>A Yard (Slot is its group's index)</summary>
    Yard,

    /// <summary>The outskirts: Slot is the plot it is set aside for, LocalX/LocalY are town-local</summary>
    Outskirts,
}

/// <summary>Where a tile is: on a plot (with its slot), the hub, a road, a Yard (with its group), the outskirts or the wilderness</summary>
public readonly record struct Zone(ZoneKind Kind, DistrictCoord District, int Slot, int LocalX, int LocalY)
{
    public int Parcel => WorldLayout.ParcelOf(LocalX, LocalY);
}

/// <summary>A Yard group (1.4 §4): the four plots around one Yard, by its town and its index there (0 NW, 1 NE, 2 SW, 3 SE)</summary>
public readonly record struct StationRef(DistrictCoord District, int Index)
{
    public override string ToString() => $"S({District.X},{District.Y}:{Index})";
}

/// <summary>Which side of its Yard a plot lies on: the plots above and below share the Yard's long sides</summary>
public enum PlotSide
{
    North,
    West,
    East,
    South,
}

/// <summary>A rectangle of tiles (inclusive bounds)</summary>
public readonly record struct TileRect(int X0, int Y0, int X1, int Y1)
{
    public bool Contains(TilePos pos) => pos.X >= X0 && pos.X <= X1 && pos.Y >= Y0 && pos.Y <= Y1;

    public bool Contains(int x, int y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;

    public int Width => X1 - X0 + 1;
    public int Height => Y1 - Y0 + 1;

    public TileRect Offset(int dx, int dy) => new(X0 + dx, Y0 + dy, X1 + dx, Y1 + dy);

    public IEnumerable<TilePos> Tiles()
    {
        for (var y = Y0; y <= Y1; y++)
        {
            for (var x = X0; x <= X1; x++)
            {
                yield return new TilePos(x, y);
            }
        }
    }
}

/// <summary>
/// The fixed geometry of the world (1.4 §4). A town is 196×164 tiles: four Yard groups of 96×80 around the hub, two
/// roads four tiles wide crossing in the middle, and the outskirts in the corners of the groups; 128 tiles of wilderness
/// lie between towns, and a town gate stands where each road leaves the town. A Yard group is a Yard of 32×16 with a plot
/// above it and below it (sharing its long sides) and one at each end; every plot is 32×32 in 16 parcels of 8×8, its
/// start area (four parcels) against the Yard with its Yard gate in the middle of that edge. The centre tile of the
/// Capital's road crossing is (0,0).
/// </summary>
public static class WorldLayout
{
    public const int EstateSize = 32;
    public const int ParcelSize = 8;
    public const int ParcelsPerSide = EstateSize / ParcelSize;
    public const int ParcelCount = ParcelsPerSide * ParcelsPerSide;
    public const int ChunkSize = 32;

    public const int YardWidth = 32;
    public const int YardHeight = 16;
    public const int RoadWidth = 4;
    public const int GroupWidth = 96;
    public const int GroupHeight = 80;

    /// <summary>A town's size, and the wilderness between towns</summary>
    public const int DistrictWidth = 2 * GroupWidth + RoadWidth;
    public const int DistrictHeight = 2 * GroupHeight + RoadWidth;
    public const int WildernessWidth = 128;
    public const int DistrictPitchX = DistrictWidth + WildernessWidth;
    public const int DistrictPitchY = DistrictHeight + WildernessWidth;

    /// <summary>Town-local: the vertical road's columns and the horizontal road's rows</summary>
    public const int RoadX = GroupWidth;
    public const int RoadY = GroupHeight;

    /// <summary>Town-local coordinates of the road crossing's centre tile, which is world (0,0) for the Capital</summary>
    public const int OriginOffsetX = RoadX + RoadWidth / 2;
    public const int OriginOffsetY = RoadY + RoadWidth / 2;

    public const int StationsPerDistrict = 4;
    public const int EstatesPerDistrict = StationsPerDistrict * 4;

    /// <summary>The hub (town-local): the market, the Wonder site and the statues, across the road crossing</summary>
    public static readonly TileRect HubLocal = new(64, 56, 131, 107);

    /// <summary>
    /// The Deep Level lies far away in the same coordinates: a deep tile is its surface tile shifted down by this
    /// (whole towns and chunks, so zones and parcels line up)
    /// </summary>
    public const int DeepOffset = DistrictPitchY * ChunkSize * 100;

    public static bool IsDeep(TilePos pos) => pos.Y >= DeepOffset / 2;

    /// <summary>The surface tile above a deep tile (a surface tile stays itself)</summary>
    public static TilePos Surface(TilePos pos) => IsDeep(pos) ? pos.Offset(0, -DeepOffset) : pos;

    /// <summary>The deep tile under a surface tile</summary>
    public static TilePos Deep(TilePos pos) => IsDeep(pos) ? pos : pos.Offset(0, DeepOffset);

    public static int FloorDiv(int a, int b) => (int)Math.Floor(a / (double)b);

    public static int DistrictOriginX(int districtX) => districtX * DistrictPitchX - OriginOffsetX;

    public static int DistrictOriginY(int districtY) => districtY * DistrictPitchY - OriginOffsetY;

    public static TilePos DistrictOrigin(DistrictCoord district) => new(DistrictOriginX(district.X), DistrictOriginY(district.Y));

    public static DistrictCoord DistrictOf(int x, int y) =>
        new(FloorDiv(x + OriginOffsetX, DistrictPitchX), FloorDiv(y + OriginOffsetY, DistrictPitchY));

    // ---------------------------------------------------------------- Yard groups

    /// <summary>A group's corner, town-local (0 NW, 1 NE, 2 SW, 3 SE)</summary>
    public static (int X, int Y) GroupOrigin(int station) =>
        (station % 2 == 0 ? 0 : GroupWidth + RoadWidth, station < 2 ? 0 : GroupHeight + RoadWidth);

    /// <summary>A plot's corner within its group, by its side</summary>
    private static (int X, int Y) SideOrigin(PlotSide side) => side switch
    {
        PlotSide.North => (32, 0),
        PlotSide.West => (0, 24),
        PlotSide.East => (64, 24),
        _ => (32, 48),
    };

    /// <summary>The Yard within its group</summary>
    private static readonly TileRect YardInGroup = new(32, 32, 32 + YardWidth - 1, 32 + YardHeight - 1);

    public static PlotSide SideOf(int slot) => (PlotSide)(slot % 4);

    public static int StationOfSlot(int slot) => slot / 4;

    public static StationRef StationOf(DistrictCoord district, int slot) => new(district, StationOfSlot(slot));

    /// <summary>The plots of a Yard group, in slot order (north, west, east, south)</summary>
    public static IReadOnlyList<int> StationSlots(int station) => [station * 4, station * 4 + 1, station * 4 + 2, station * 4 + 3];

    public static IEnumerable<StationRef> Stations(DistrictCoord district) =>
        Enumerable.Range(0, StationsPerDistrict).Select(t => new StationRef(district, t));

    /// <summary>A plot's corner, town-local</summary>
    public static (int X, int Y) EstateLocal(int slot)
    {
        var (gx, gy) = GroupOrigin(StationOfSlot(slot));
        var (sx, sy) = SideOrigin(SideOf(slot));
        return (gx + sx, gy + sy);
    }

    public static TilePos EstateOrigin(DistrictCoord district, int slot)
    {
        var (lx, ly) = EstateLocal(slot);
        return new TilePos(DistrictOriginX(district.X) + lx, DistrictOriginY(district.Y) + ly);
    }

    public static TilePos HubOrigin(DistrictCoord district) => new(DistrictOriginX(district.X) + HubLocal.X0, DistrictOriginY(district.Y) + HubLocal.Y0);

    /// <summary>A Yard, town-local</summary>
    public static TileRect YardLocal(int station)
    {
        var (gx, gy) = GroupOrigin(station);
        return YardInGroup.Offset(gx, gy);
    }

    /// <summary>A Yard in world coordinates</summary>
    public static TileRect Yard(StationRef station) =>
        YardLocal(station.Index).Offset(DistrictOriginX(station.District.X), DistrictOriginY(station.District.Y));

    // ---------------------------------------------------------------- A plot's own places

    /// <summary>
    /// A plot's Yard gate (plot-local): the middle of the edge it shares with its Yard, where its own pipes meet the Yard
    /// </summary>
    public static (int X, int Y) YardGateLocal(int slot) => SideOf(slot) switch
    {
        PlotSide.North => (16, EstateSize - 1),
        PlotSide.West => (EstateSize - 1, 16),
        PlotSide.East => (0, 16),
        _ => (16, 0),
    };

    public static TilePos YardGate(DistrictCoord district, int slot)
    {
        var (gx, gy) = YardGateLocal(slot);
        return EstateOrigin(district, slot).Offset(gx, gy);
    }

    /// <summary>
    /// A tile in a plot's own frame, as far along its Yard edge and as deep from it as it lies: the same frame on every
    /// side, so a layout carried to another side stays where it stood against the Yard
    /// </summary>
    public static (int Along, int Depth) ToYardFrame(PlotSide side, int x, int y, int size = EstateSize) => side switch
    {
        PlotSide.North => (x, size - 1 - y),
        PlotSide.South => (x, y),
        PlotSide.West => (y, size - 1 - x),
        _ => (y, x),
    };

    public static (int X, int Y) FromYardFrame(PlotSide side, int along, int depth, int size = EstateSize) => side switch
    {
        PlotSide.North => (along, size - 1 - depth),
        PlotSide.South => (along, depth),
        PlotSide.West => (size - 1 - depth, along),
        _ => (depth, along),
    };

    /// <summary>
    /// A footprint's corner on another side's plot (plot-local): every tile goes through the Yard frame, and the corner is
    /// the smallest of them
    /// </summary>
    public static (int X, int Y) MoveFootprint(PlotSide from, PlotSide to, int x, int y, int size)
    {
        var (minX, minY) = (int.MaxValue, int.MaxValue);
        foreach (var (cx, cy) in new[] { (x, y), (x + size - 1, y + size - 1) })
        {
            var (along, depth) = ToYardFrame(from, cx, cy);
            var (tx, ty) = FromYardFrame(to, along, depth);
            minX = Math.Min(minX, tx);
            minY = Math.Min(minY, ty);
        }
        return (minX, minY);
    }

    /// <summary>A parcel mask carried to another side: each parcel through the Yard frame of the 4×4 grid</summary>
    public static int MoveParcels(PlotSide from, PlotSide to, int mask)
    {
        var moved = 0;
        for (var parcel = 0; parcel < ParcelCount; parcel++)
        {
            if ((mask & (1 << parcel)) == 0)
            {
                continue;
            }
            var (along, depth) = ToYardFrame(from, parcel % ParcelsPerSide, parcel / ParcelsPerSide, ParcelsPerSide);
            var (px, py) = FromYardFrame(to, along, depth, ParcelsPerSide);
            moved |= 1 << (py * ParcelsPerSide + px);
        }
        return moved;
    }

    /// <summary>A plot's start area (1.4 §5): the four parcels (16×16) against the Yard, around the gate</summary>
    public static IReadOnlyList<int> StartParcels(int slot) => SideOf(slot) switch
    {
        PlotSide.North => [9, 10, 13, 14],
        PlotSide.West => [6, 7, 10, 11],
        PlotSide.East => [4, 5, 8, 9],
        _ => [1, 2, 5, 6],
    };

    public static int StartMask(int slot) => StartParcels(slot).Aggregate(0, (mask, parcel) => mask | (1 << parcel));

    /// <summary>How many parcels the start area has (every plot's)</summary>
    public const int StartParcelCount = 4;

    /// <summary>Where the HQ (2×2) stands on a plot: next to the Yard gate, three tiles along its edge and a tile in</summary>
    public static (int X, int Y) HqLocal(int slot)
    {
        var side = SideOf(slot);
        var (gateAlong, _) = ToYardFrame(side, YardGateLocal(slot).X, YardGateLocal(slot).Y);
        return MoveFootprint(PlotSide.South, side, gateAlong - 3, 1, 2);
    }

    /// <summary>Where the trading post stands: the other side of the gate, a tile in</summary>
    public static (int X, int Y) TradingPostLocal(int slot)
    {
        var side = SideOf(slot);
        var (gateAlong, _) = ToYardFrame(side, YardGateLocal(slot).X, YardGateLocal(slot).Y);
        return FromYardFrame(side, gateAlong + 2, 1);
    }

    public static int ParcelOf(int localX, int localY) => localY / ParcelSize * ParcelsPerSide + localX / ParcelSize;

    public static (int X, int Y) ParcelOrigin(int parcel) => (parcel % ParcelsPerSide * ParcelSize, parcel / ParcelsPerSide * ParcelSize);

    // ---------------------------------------------------------------- The outskirts

    /// <summary>
    /// The outskirts parcels a plot has set aside (1.4 §5): nine 8×8 parcels in the corners of its group next to its own
    /// land (town-local corners), in the order they are bought. For the NW group the north plot has the two columns of
    /// the top-left corner beside it and the first column of the top-right one, the west plot the rest of the top-left
    /// corner and the first column of the bottom-left one, the east plot the rest of the top-right corner, the south
    /// plot the rest of the bottom-left one; the other groups are its mirror images (the hub takes each group's corner
    /// towards the middle).
    /// </summary>
    public static IReadOnlyList<(int X, int Y)> OutskirtsParcels(int slot) => OutskirtsOf[slot];

    private static readonly IReadOnlyList<(int X, int Y)>[] OutskirtsOf = BuildOutskirts();

    private static IReadOnlyList<(int X, int Y)>[] BuildOutskirts()
    {
        // The NW group's allotment: (corner x, corner y) of each parcel, group-local, by side
        var nw = new Dictionary<PlotSide, List<(int X, int Y)>>
        {
            [PlotSide.North] = [], [PlotSide.West] = [], [PlotSide.East] = [], [PlotSide.South] = [],
        };
        for (var row = 0; row < 3; row++)
        {
            for (var col = 0; col < 4; col++)
            {
                nw[col >= 2 ? PlotSide.North : PlotSide.West].Add((col * 8, row * 8));
                nw[col == 0 ? PlotSide.North : PlotSide.East].Add((64 + col * 8, row * 8));
                nw[col == 0 ? PlotSide.West : PlotSide.South].Add((col * 8, 56 + row * 8));
            }
        }
        var result = new IReadOnlyList<(int X, int Y)>[EstatesPerDistrict];
        for (var station = 0; station < StationsPerDistrict; station++)
        {
            var mirrorX = station % 2 == 1;
            var mirrorY = station >= 2;
            var (gx, gy) = GroupOrigin(station);
            foreach (var (side, parcels) in nw)
            {
                var mirrored = side switch
                {
                    PlotSide.West when mirrorX => PlotSide.East,
                    PlotSide.East when mirrorX => PlotSide.West,
                    PlotSide.North when mirrorY => PlotSide.South,
                    PlotSide.South when mirrorY => PlotSide.North,
                    _ => side,
                };
                result[station * 4 + (int)mirrored] = parcels
                    .Select(t => (gx + (mirrorX ? GroupWidth - ParcelSize - t.X : t.X), gy + (mirrorY ? GroupHeight - ParcelSize - t.Y : t.Y)))
                    .ToList();
            }
        }
        return result;
    }

    /// <summary>The outskirts parcels by their group and their place in its 12×10 grid of parcels: (slot, its index 0..8)</summary>
    private static readonly Dictionary<(int Station, int Px, int Py), (int Slot, int Index)> OutskirtsAt = Enumerable.Range(0, EstatesPerDistrict)
        .SelectMany(slot => OutskirtsOf[slot].Select((t, i) =>
        {
            var station = StationOfSlot(slot);
            var (gx, gy) = GroupOrigin(station);
            return (Key: (station, (t.X - gx) / ParcelSize, (t.Y - gy) / ParcelSize), Value: (slot, i));
        }))
        .ToDictionary(t => t.Key, t => t.Value);

    /// <summary>Who an outskirts tile (town-local) is set aside for: (slot, its parcel's index 0..8), or null</summary>
    public static (int Slot, int Index)? OutskirtsOwner(int localX, int localY)
    {
        if (localX < 0 || localY < 0 || localX >= DistrictWidth || localY >= DistrictHeight)
        {
            return null;
        }
        var station = (localY < RoadY ? 0 : 2) + (localX < RoadX ? 0 : 1);
        var (gx, gy) = GroupOrigin(station);
        var (glx, gly) = (localX - gx, localY - gy);
        return glx < 0 || gly < 0 ? null
            : OutskirtsAt.TryGetValue((station, glx / ParcelSize, gly / ParcelSize), out var owner) ? owner : null;
    }

    // ---------------------------------------------------------------- Town gates

    /// <summary>The four town gates (town-local, 8×4 or 4×8): where the roads leave the town (top, bottom, left, right)</summary>
    public static IReadOnlyList<TileRect> TownGatesLocal { get; } =
    [
        new(RoadX - 2, 0, RoadX + RoadWidth + 1, 1),
        new(RoadX - 2, DistrictHeight - 2, RoadX + RoadWidth + 1, DistrictHeight - 1),
        new(0, RoadY - 2, 1, RoadY + RoadWidth + 1),
        new(DistrictWidth - 2, RoadY - 2, DistrictWidth - 1, RoadY + RoadWidth + 1),
    ];

    // ---------------------------------------------------------------- Zones

    public static Zone ZoneAt(int x, int y)
    {
        var district = DistrictOf(x, y);
        var lx = x - DistrictOriginX(district.X);
        var ly = y - DistrictOriginY(district.Y);
        if (lx >= DistrictWidth || ly >= DistrictHeight)
        {
            return new Zone(ZoneKind.Wilderness, district, -1, lx, ly);
        }
        if (HubLocal.Contains(lx, ly))
        {
            return new Zone(ZoneKind.Hub, district, -1, lx - HubLocal.X0, ly - HubLocal.Y0);
        }
        if (lx is >= RoadX and < RoadX + RoadWidth || ly is >= RoadY and < RoadY + RoadWidth)
        {
            return new Zone(ZoneKind.Corridor, district, -1, lx, ly);
        }
        var gx = lx < RoadX ? 0 : 1;
        var gy = ly < RoadY ? 0 : 1;
        var station = gy * 2 + gx;
        var (ox, oy) = GroupOrigin(station);
        var (glx, gly) = (lx - ox, ly - oy);
        for (var side = 0; side < 4; side++)
        {
            var (sx, sy) = SideOrigin((PlotSide)side);
            if (glx >= sx && glx < sx + EstateSize && gly >= sy && gly < sy + EstateSize)
            {
                return new Zone(ZoneKind.Estate, district, station * 4 + side, glx - sx, gly - sy);
            }
        }
        if (YardInGroup.Contains(glx, gly))
        {
            return new Zone(ZoneKind.Yard, district, station, glx - YardInGroup.X0, gly - YardInGroup.Y0);
        }
        return OutskirtsOwner(lx, ly) is { } owner
            ? new Zone(ZoneKind.Outskirts, district, owner.Slot, lx, ly)
            : new Zone(ZoneKind.Wilderness, district, -1, lx, ly);
    }

    public static (int X, int Y) ChunkOf(int x, int y) => (FloorDiv(x, ChunkSize), FloorDiv(y, ChunkSize));

    /// <summary>Towns in the order they open: the Capital, then rings around it</summary>
    public static IEnumerable<DistrictCoord> DistrictOrder()
    {
        yield return new DistrictCoord(0, 0);
        for (var ring = 1; ; ring++)
        {
            // Walk the ring clockwise starting at the right side
            for (var y = -ring + 1; y <= ring; y++) yield return new DistrictCoord(ring, y);
            for (var x = ring - 1; x >= -ring; x--) yield return new DistrictCoord(x, ring);
            for (var y = ring - 1; y >= -ring; y--) yield return new DistrictCoord(-ring, y);
            for (var x = -ring + 1; x <= ring; x++) yield return new DistrictCoord(x, -ring);
        }
    }
}
