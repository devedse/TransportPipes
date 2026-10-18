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
}

public enum GateSide
{
    Top,
    Right,
    Bottom,
    Left,
}

/// <summary>Where a tile is: in an estate (with its slot), the hub, a corridor or the wilderness</summary>
public readonly record struct Zone(ZoneKind Kind, DistrictCoord District, int Slot, int LocalX, int LocalY)
{
    public int Parcel => WorldLayout.ParcelOf(LocalX, LocalY);
}

/// <summary>
/// The fixed geometry of the world: districts of 5x5 blocks of 32 tiles with 2 tile corridors between and around them,
/// the hub in the middle block, 24 estates around it and 30 tiles of wilderness between districts.
/// The centre tile of the Capital's hub is (0,0).
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
    public const int ChunkSize = 8;

    public const int BlockPitch = EstateSize + CorridorWidth;
    public const int DistrictSize = CorridorWidth + BlocksPerSide * BlockPitch;
    public const int DistrictPitch = DistrictSize + WildernessWidth;
    public const int HubBlock = BlocksPerSide / 2;
    public const int EstatesPerDistrict = BlocksPerSide * BlocksPerSide - 1;

    /// <summary>Local coordinate (within the district) of the hub's centre tile, which is world (0,0) for the Capital</summary>
    public const int OriginOffset = CorridorWidth + HubBlock * BlockPitch + EstateSize / 2;

    /// <summary>Gate positions within an estate (local coordinates)</summary>
    public static readonly IReadOnlyDictionary<GateSide, (int X, int Y)> GateLocal = new Dictionary<GateSide, (int X, int Y)>
    {
        [GateSide.Top] = (EstateSize / 2, 0),
        [GateSide.Right] = (EstateSize - 1, EstateSize / 2),
        [GateSide.Bottom] = (EstateSize / 2, EstateSize - 1),
        [GateSide.Left] = (0, EstateSize / 2),
    };

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

    public static TilePos GatePosition(DistrictCoord district, int slot, GateSide side)
    {
        var origin = EstateOrigin(district, slot);
        var (gx, gy) = GateLocal[side];
        return origin.Offset(gx, gy);
    }

    /// <summary>The corridor tile right outside a gate</summary>
    public static TilePos OutsideGate(DistrictCoord district, int slot, GateSide side)
    {
        var gate = GatePosition(district, slot, side);
        return side switch
        {
            GateSide.Top => gate.Offset(0, -1),
            GateSide.Right => gate.Offset(1, 0),
            GateSide.Bottom => gate.Offset(0, 1),
            _ => gate.Offset(-1, 0),
        };
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
