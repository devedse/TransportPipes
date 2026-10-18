using TransportPipes.Game.Content;
using TransportPipes.Game.State;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>A game world with a controllable clock and helpers to build things quickly</summary>
internal sealed class TestGame
{
    public static readonly DateTime Start = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    public DateTime Time { get; set; } = Start;
    public GameWorld World { get; }

    public TestGame(int seed = 1234)
    {
        World = new GameWorld(GameContent.Default, seed, () => Time);
    }

    public void Pass(double minutes)
    {
        Time = Time.AddMinutes(minutes);
        World.Tick();
    }

    public Company NewCompany(string name = "Test Co", double gold = 1_000_000, int level = 1)
    {
        var company = World.CreateCompany(World.Companies.Count() + 1, name, "#ff8800");
        company.Gold = gold;
        company.Level = level;
        return company;
    }

    public Estate EstateOf(Company company) => World.EstateOf(company)!;

    public TilePos Local(Company company, int x, int y) => EstateOf(company).Origin.Offset(x, y);

    public Building TradingPost(Company company) => World.BuildingsOf(company.Id).Single(t => t.Type == "tradingPost");

    /// <summary>The first spot in the start parcels where this building can stand</summary>
    public TilePos Spot(Company company, string type, Func<TilePos, bool>? where = null)
    {
        var definition = World.Content.Building(type);
        var estate = EstateOf(company);
        for (var y = 8; y < 24; y++)
        {
            for (var x = 8; x < 24; x++)
            {
                var pos = estate.Origin.Offset(x, y);
                if ((where == null || where(pos)) && World.CanPlace(company, definition, pos).Success
                    && GameWorld.BorderTiles(pos, definition.Size).Any(t => !World.IsOccupied(t) && World.Tile(t).Terrain is not (TerrainType.Mountain or TerrainType.Water)))
                {
                    return pos;
                }
            }
        }
        throw new AssertFailedException($"No spot for {type}");
    }

    public Building Build(Company company, string type, TilePos? pos = null, bool finish = true)
    {
        pos ??= Spot(company, type);
        var result = World.PlaceBuilding(company.Id, type, pos.Value);
        Assert.IsTrue(result.Success, result.Error);
        var building = World.BuildingAt(pos.Value)!;
        if (finish)
        {
            World.FinishConstruction(building.Id);
        }
        return building;
    }

    public List<TilePos> Connect(Company company, Building from, Building to) => Connect(company, Pipeable(from.Border()), Pipeable(to.Border()));

    /// <summary>Connects a building to one of the company's gates</summary>
    public List<TilePos> Connect(Company company, Building from, GateSide side)
    {
        var gate = WorldLayout.GatePosition(company.District, company.Slot, side);
        var estate = EstateOf(company);
        return Connect(company, Pipeable(from.Border()), Pipeable(Neighbours(gate).Where(estate.Contains)));
    }

    /// <summary>Lays the shortest pipe between two sets of tiles (over free land and the company's own pipes)</summary>
    public List<TilePos> Connect(Company company, IEnumerable<TilePos> starts, IEnumerable<TilePos> goals)
    {
        var goalSet = goals.ToHashSet();
        var previous = new Dictionary<TilePos, TilePos?>();
        var queue = new Queue<TilePos>();
        foreach (var start in starts)
        {
            previous[start] = null;
            queue.Enqueue(start);
        }
        while (queue.TryDequeue(out var tile))
        {
            if (goalSet.Contains(tile))
            {
                var path = new List<TilePos>();
                for (TilePos? t = tile; t != null; t = previous[t.Value])
                {
                    path.Add(t.Value);
                }
                path.Reverse();
                var result = World.PlacePipes(company.Id, path);
                Assert.IsTrue(result.Success, result.Error);
                return path;
            }
            foreach (var next in Neighbours(tile))
            {
                if (!previous.ContainsKey(next) && CanPipe(company, next))
                {
                    previous[next] = tile;
                    queue.Enqueue(next);
                }
            }
        }
        throw new AssertFailedException("No route for the pipe");
    }

    private IEnumerable<TilePos> Pipeable(IEnumerable<TilePos> tiles) => tiles.Where(t => World.ConductorAt(t)?.Kind == ConductorKind.Pipe || !World.IsOccupied(t));

    private bool CanPipe(Company company, TilePos tile)
    {
        var zone = WorldLayout.ZoneAt(tile.X, tile.Y);
        if (zone.Kind == ZoneKind.Estate ? !EstateOf(company).Contains(tile) : zone.Kind != ZoneKind.Corridor)
        {
            return false;
        }
        if (World.ConductorAt(tile) is { Kind: ConductorKind.Pipe } pipe)
        {
            return pipe.Owner == company.Id;
        }
        return !World.IsOccupied(tile) && World.Tile(tile).Terrain is not (TerrainType.Mountain or TerrainType.Water);
    }

    public static IEnumerable<TilePos> Neighbours(TilePos tile) => [tile.Offset(1, 0), tile.Offset(-1, 0), tile.Offset(0, 1), tile.Offset(0, -1)];
}
