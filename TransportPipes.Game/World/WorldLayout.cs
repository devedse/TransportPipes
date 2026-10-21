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

    /// <summary>A station's Yard: a paved plaza nobody builds on</summary>
    Yard,

    /// <summary>The Deep Level: solid rock, dug out parcel by parcel into caverns</summary>
    Rock,
    Cavern,
}

public readonly record struct TilePos(int X, int Y)
{
    public TilePos Offset(int dx, int dy) => new(X + dx, Y + dy);

    public override string ToString() => $"<{X},{Y}>";
}

public readonly record struct DistrictCoord(int X, int Y)
{
    public override string ToString() => $"D({X},{Y})";
}

public enum ZoneKind
{
    Estate,
    Hub,
    Corridor,
    Wilderness,

    /// <summary>A station's shared plaza at the corridor crossing (Slot is the station's index)</summary>
    Yard,
}

/// <summary>Where a tile is: in an estate (with its slot), the hub, a corridor, a Yard (with its station) or the wilderness</summary>
public readonly record struct Zone(ZoneKind Kind, DistrictCoord District, int Slot, int LocalX, int LocalY)
{
    public int Parcel => WorldLayout.ParcelOf(LocalX, LocalY);
}

/// <summary>A station (v1.2 §4): the seats around one Yard, by its district and its index there (0..7)</summary>
public readonly record struct StationRef(DistrictCoord District, int Index)
{
    /// <summary>Stations 0..3 are the corner groups of four seats, 4..7 the small stations of two on the hub's arms</summary>
    public bool IsSmall => Index >= WorldLayout.CornerStations;

    public override string ToString() => $"S({District.X},{District.Y}:{Index})";
}

/// <summary>A rectangle of tiles (inclusive bounds)</summary>
public readonly record struct TileRect(int X0, int Y0, int X1, int Y1)
{
    public bool Contains(TilePos pos) => pos.X >= X0 && pos.X <= X1 && pos.Y >= Y0 && pos.Y <= Y1;

    public int Width => X1 - X0 + 1;
    public int Height => Y1 - Y0 + 1;

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
/// The fixed geometry of the world: districts of 5x5 blocks of 32 tiles with 2 tile corridors between and around them,
/// the hub in the middle block, 24 estates around it and 30 tiles of wilderness between districts.
/// The centre tile of the Capital's hub is (0,0).
///
/// The estates of a district live in stations (v1.2 §4): the four corner groups of 2x2 estates around a Yard at their
/// corridor crossing, and the four pairs on the hub's arms (small stations) around a Yard on the corridor between them.
/// A Yard is 10x10 tiles: the corridor and 4 tiles of each of its estates. Every estate has one Yard gate, where its
/// own pipes meet the Yard.
/// </summary>
public static class WorldLayout
{
    public const int EstateSize = 32;
    public const int CorridorWidth = 2;
    public const int BlocksPerSide = 5;
    public const int ParcelSize = 8;
    public const int ParcelsPerSide = EstateSize / ParcelSize;
    public const int ParcelCount = ParcelsPerSide * ParcelsPerSide;
    public const int WildernessWidth = 30;
    public const int ChunkSize = 32;

    /// <summary>
    /// The Deep Level lies far away in the same coordinates: a deep tile is its surface tile shifted down by this
    /// (whole districts and chunks, so zones and parcels line up)
    /// </summary>
    public const int DeepOffset = DistrictPitch * ChunkSize * 150;

    public static bool IsDeep(TilePos pos) => pos.Y >= DeepOffset / 2;

    /// <summary>The surface tile above a deep tile (a surface tile stays itself)</summary>
    public static TilePos Surface(TilePos pos) => IsDeep(pos) ? pos.Offset(0, -DeepOffset) : pos;

    /// <summary>The deep tile under a surface tile</summary>
    public static TilePos Deep(TilePos pos) => IsDeep(pos) ? pos : pos.Offset(0, DeepOffset);

    public const int BlockPitch = EstateSize + CorridorWidth;
    public const int DistrictSize = CorridorWidth + BlocksPerSide * BlockPitch;
    public const int DistrictPitch = DistrictSize + WildernessWidth;
    public const int HubBlock = BlocksPerSide / 2;
    public const int EstatesPerDistrict = BlocksPerSide * BlocksPerSide - 1;

    /// <summary>Local coordinate (within the district) of the hub's centre tile, which is world (0,0) for the Capital</summary>
    public const int OriginOffset = CorridorWidth + HubBlock * BlockPitch + EstateSize / 2;

    public static int FloorDiv(int a, int b) => (int)Math.Floor(a / (double)b);

    public static int DistrictOriginX(int districtX) => districtX * DistrictPitch - OriginOffset;

    public static int DistrictOriginY(int districtY) => districtY * DistrictPitch - OriginOffset;

    public static DistrictCoord DistrictOf(int x, int y) =>
        new(FloorDiv(x + OriginOffset, DistrictPitch), FloorDiv(y + OriginOffset, DistrictPitch));

    public static Zone ZoneAt(int x, int y)
    {
        var district = DistrictOf(x, y);
        var lx = x - DistrictOriginX(district.X);
        var ly = y - DistrictOriginY(district.Y);
        if (lx >= DistrictSize || ly >= DistrictSize)
        {
            return new Zone(ZoneKind.Wilderness, district, -1, lx, ly);
        }
        if (YardAtLocal(lx, ly) is { } station)
        {
            var yard = YardLocal(station);
            return new Zone(ZoneKind.Yard, district, station, lx - yard.X0, ly - yard.Y0);
        }

        var (bx, ox) = BlockAxis(lx);
        var (by, oy) = BlockAxis(ly);
        if (bx < 0 || by < 0)
        {
            return new Zone(ZoneKind.Corridor, district, -1, lx, ly);
        }
        if (bx == HubBlock && by == HubBlock)
        {
            return new Zone(ZoneKind.Hub, district, -1, ox, oy);
        }
        return new Zone(ZoneKind.Estate, district, SlotOf(bx, by), ox, oy);
    }

    /// <returns>The block index along an axis and the offset in it, or -1 when the coordinate is in a corridor</returns>
    private static (int Block, int Offset) BlockAxis(int local)
    {
        var inner = local - CorridorWidth;
        if (inner < 0)
        {
            return (-1, 0);
        }
        var block = inner / BlockPitch;
        var offset = inner % BlockPitch;
        return offset >= EstateSize || block >= BlocksPerSide ? (-1, 0) : (block, offset);
    }

    public static int SlotOf(int blockX, int blockY)
    {
        var raw = blockY * BlocksPerSide + blockX;
        var hubRaw = HubBlock * BlocksPerSide + HubBlock;
        return raw > hubRaw ? raw - 1 : raw;
    }

    public static (int BlockX, int BlockY) BlockOfSlot(int slot)
    {
        var hubRaw = HubBlock * BlocksPerSide + HubBlock;
        var raw = slot >= hubRaw ? slot + 1 : slot;
        return (raw % BlocksPerSide, raw / BlocksPerSide);
    }

    public static TilePos BlockOrigin(DistrictCoord district, int blockX, int blockY) =>
        new(DistrictOriginX(district.X) + CorridorWidth + blockX * BlockPitch, DistrictOriginY(district.Y) + CorridorWidth + blockY * BlockPitch);

    public static TilePos EstateOrigin(DistrictCoord district, int slot)
    {
        var (bx, by) = BlockOfSlot(slot);
        return BlockOrigin(district, bx, by);
    }

    public static TilePos HubOrigin(DistrictCoord district) => BlockOrigin(district, HubBlock, HubBlock);

    public static int ParcelOf(int localX, int localY) => localY / ParcelSize * ParcelsPerSide + localX / ParcelSize;

    public static (int X, int Y) ParcelOrigin(int parcel) => (parcel % ParcelsPerSide * ParcelSize, parcel / ParcelsPerSide * ParcelSize);

    // ---------------------------------------------------------------- Stations and Yards

    public const int CornerStations = 4;
    public const int StationsPerDistrict = 8;
    public const int YardSize = 10;

    /// <summary>How deep a Yard reaches into each of its estates</summary>
    public const int YardDepth = (YardSize - CorridorWidth) / 2;

    /// <summary>
    /// Stations by their blocks: 0 NW, 1 NE, 2 SW, 3 SE (2x2 estates each), 4 N, 5 S, 6 W, 7 E (the small stations of
    /// two estates on the hub's arms)
    /// </summary>
    private static readonly (int X0, int Y0, int X1, int Y1)[] StationBlocks =
    [
        (0, 0, 1, 1), (3, 0, 4, 1), (0, 3, 1, 4), (3, 3, 4, 4),
        (2, 0, 2, 1), (2, 3, 2, 4), (0, 2, 1, 2), (3, 2, 4, 2),
    ];

    /// <summary>The station an estate slot belongs to</summary>
    public static int StationOfSlot(int slot)
    {
        var (bx, by) = BlockOfSlot(slot);
        for (var i = 0; i < StationBlocks.Length; i++)
        {
            var (x0, y0, x1, y1) = StationBlocks[i];
            if (bx >= x0 && bx <= x1 && by >= y0 && by <= y1)
            {
                return i;
            }
        }
        throw new ArgumentOutOfRangeException(nameof(slot));
    }

    public static StationRef StationOf(DistrictCoord district, int slot) => new(district, StationOfSlot(slot));

    /// <summary>The estate slots of a station (4, or 2 for a small one), in slot order</summary>
    public static IReadOnlyList<int> StationSlots(int station)
    {
        var (x0, y0, x1, y1) = StationBlocks[station];
        var slots = new List<int>();
        for (var by = y0; by <= y1; by++)
        {
            for (var bx = x0; bx <= x1; bx++)
            {
                slots.Add(SlotOf(bx, by));
            }
        }
        return slots;
    }

    public static IEnumerable<StationRef> Stations(DistrictCoord district) =>
        Enumerable.Range(0, StationsPerDistrict).Select(t => new StationRef(district, t));

    /// <summary>The corner stations next to a small station (a crew of 5–8 joins them into one big station)</summary>
    public static IReadOnlyList<int> CornersNextTo(int smallStation) => smallStation switch
    {
        4 => [0, 1],
        5 => [2, 3],
        6 => [0, 2],
        7 => [1, 3],
        _ => [],
    };

    /// <summary>The first corridor tile between two blocks along an axis (the corridor after block b)</summary>
    private static int CorridorAfter(int block) => CorridorWidth + block * BlockPitch + EstateSize;

    /// <summary>The Yard of a station in district-local coordinates</summary>
    public static TileRect YardLocal(int station)
    {
        var (x0, y0, x1, y1) = StationBlocks[station];
        // A corner station's Yard sits on its corridor crossing; a small station's on the corridor between its two
        // estates, towards the outer end, so it stays clear of the middle of the estates' sides
        int yx, yy;
        if (x0 != x1 && y0 != y1)
        {
            yx = CorridorAfter(x0) - YardDepth;
            yy = CorridorAfter(y0) - YardDepth;
        }
        else if (x0 == x1)
        {
            yx = CorridorWidth + x0 * BlockPitch + SmallYardOffset;
            yy = CorridorAfter(y0) - YardDepth;
        }
        else
        {
            yx = CorridorAfter(x0) - YardDepth;
            yy = CorridorWidth + y0 * BlockPitch + SmallYardOffset;
        }
        return new TileRect(yx, yy, yx + YardSize - 1, yy + YardSize - 1);
    }

    /// <summary>Where along its estates' side a small station's Yard starts</summary>
    private const int SmallYardOffset = 20;

    /// <summary>The Yard of a station in world coordinates</summary>
    public static TileRect Yard(StationRef station)
    {
        var local = YardLocal(station.Index);
        var ox = DistrictOriginX(station.District.X);
        var oy = DistrictOriginY(station.District.Y);
        return new TileRect(local.X0 + ox, local.Y0 + oy, local.X1 + ox, local.Y1 + oy);
    }

    private static readonly TileRect[] YardsLocal = Enumerable.Range(0, StationsPerDistrict).Select(YardLocal).ToArray();

    private static int? YardAtLocal(int lx, int ly)
    {
        for (var i = 0; i < YardsLocal.Length; i++)
        {
            var yard = YardsLocal[i];
            if (lx >= yard.X0 && lx <= yard.X1 && ly >= yard.Y0 && ly <= yard.Y1)
            {
                return i;
            }
        }
        return null;
    }

    /// <summary>The part of an estate the Yard takes (estate-local coordinates)</summary>
    public static TileRect YardPart(int slot)
    {
        var yard = YardsLocal[StationOfSlot(slot)];
        var (bx, by) = BlockOfSlot(slot);
        var ox = CorridorWidth + bx * BlockPitch;
        var oy = CorridorWidth + by * BlockPitch;
        return new TileRect(Math.Max(yard.X0, ox) - ox, Math.Max(yard.Y0, oy) - oy, Math.Min(yard.X1, ox + EstateSize - 1) - ox, Math.Min(yard.Y1, oy + EstateSize - 1) - oy);
    }

    /// <summary>
    /// An estate's Yard gate (estate-local): the tile right next to the Yard's part of the estate, on the side of the
    /// estate's middle (beside a corner part, at its row nearest the middle; in front of the middle of a side part),
    /// where the estate's own pipes meet the Yard
    /// </summary>
    public static (int X, int Y) YardGateLocal(int slot)
    {
        var part = YardPart(slot);
        const int middle = EstateSize / 2;
        if (part.Width > YardDepth)
        {
            // A small station's part runs along the top or bottom of the estate
            return ((part.X0 + part.X1) / 2, part.Y0 > middle ? part.Y0 - 1 : part.Y1 + 1);
        }
        if (part.Height > YardDepth)
        {
            return (part.X0 > middle ? part.X0 - 1 : part.X1 + 1, (part.Y0 + part.Y1) / 2);
        }
        return (part.X0 > middle ? part.X0 - 1 : part.X1 + 1, part.Y0 > middle ? part.Y0 : part.Y1);
    }

    public static TilePos YardGate(DistrictCoord district, int slot)
    {
        var origin = EstateOrigin(district, slot);
        var (gx, gy) = YardGateLocal(slot);
        return origin.Offset(gx, gy);
    }

    public static (int X, int Y) ChunkOf(int x, int y) => (FloorDiv(x, ChunkSize), FloorDiv(y, ChunkSize));

    /// <summary>Districts in the order they open: the Capital, then rings around it</summary>
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
