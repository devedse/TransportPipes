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

    public TestGame(int seed = 1234, GameContent? content = null, bool events = false, bool achievements = false, bool tutorial = false, bool surveys = false)
    {
        World = new GameWorld(content ?? GameContent.Default, seed, () => Time) { EventsEnabled = events, AchievementsEnabled = achievements, TutorialEnabled = tutorial, SurveysEnabled = surveys };
    }

    /// <summary>The game content without location modifiers (wind, sun, richness), so every building makes its listed amount</summary>
    public static GameContent PlainContent { get; } = CreatePlainContent();

    /// <summary>PlainContent whose pipes lose nothing (so how long a pipe is doesn't matter)</summary>
    public static GameContent LosslessContent { get; } = new(PlainContent.ResourceList, PlainContent.BuildingList, PlainContent.PipeTiers.Select(t => t with { LossPerTile = 0 }).ToList(),
        PlainContent.Progression, PlainContent.ResearchList, [.. PlainContent.DistrictWonders, .. PlainContent.EraWonders], PlainContent.Managers, PlainContent.Achievements,
        PlainContent.Collections, PlainContent.Tutorial, PlainContent.Relics);

    private static GameContent CreatePlainContent()
    {
        var content = GameContent.Default;
        var buildings = content.BuildingList.Select(t => new BuildingDefinition
        {
            Id = t.Id,
            Name = t.Name,
            Category = t.Category,
            Kind = t.Kind,
            Size = t.Size,
            Buildable = t.Buildable,
            Unlock = t.Unlock,
            Cost = t.Cost,
            BuildSeconds = t.BuildSeconds,
            In = t.In,
            Out = t.Out,
            OutFromDeposit = t.OutFromDeposit,
            ResearchPerMinute = t.ResearchPerMinute,
            Placement = t.Placement,
            Modifier = ProductionModifier.None,
            Capacity = t.Capacity,
            Throughput = t.Throughput,
            PriceBonus = t.PriceBonus,
            Range = t.Range,
            Reach = t.Reach,
            Layer = t.Layer,
            EvolvesTo = t.EvolvesTo,
            Sprite = t.Sprite,
            Description = t.Description,
        }).ToList();
        return new GameContent(content.ResourceList, buildings, content.PipeTiers, content.Progression, content.ResearchList, [.. content.DistrictWonders, .. content.EraWonders], content.Managers, content.Achievements, content.Collections, content.Tutorial, content.Relics);
    }

    private static readonly Dictionary<char, string> MapLetters = new()
    {
        ['W'] = "windMoily",
        ['S'] = "solarPanel",
        ['M'] = "ironMine",
        ['F'] = "smallFactory",
        ['O'] = "storage",
        ['T'] = "tradingPost",
        ['V'] = "valve",
        ['P'] = "priorityValve",
        ['U'] = "pumpStation",
        ['E'] = "exchangeOffice",
        ['C'] = "cargoDepot",
        ['L'] = "powerLinePylon",
        ['H'] = "showroom",
        ['B'] = "blueprintOffice",
        ['Q'] = "pneumaticPost",
        ['R'] = "lab",
    };

    /// <summary>
    /// Puts a map like the original game's tests on the world, without any placement rules: W wind moily, S solar panel,
    /// M iron mine, F small factory, O storage, T trading post, V valve, P priority valve, U pump station, L power line pylon, H showroom, B blueprint office, Q pneumatic post, R lab, + pipe
    /// (lossless by default, like the original game).
    /// Returns the buildings by their map position.
    /// </summary>
    public Dictionary<(int X, int Y), Building> Map(Company company, TilePos origin, string map, int pipeTier = 5)
    {
        var buildings = new Dictionary<(int X, int Y), Building>();
        var rows = map.Replace("\r", "").Split('\n');
        for (var y = 0; y < rows.Length; y++)
        {
            for (var x = 0; x < rows[y].Length; x++)
            {
                var c = rows[y][x];
                var pos = origin.Offset(x, y);
                if (c == '+')
                {
                    World.LoadConductor(new Conductor { Pos = pos, Tier = pipeTier, Owner = company.Id });
                }
                else if (MapLetters.TryGetValue(c, out var type))
                {
                    var building = new Building { Id = World.NextBuildingId, Type = type, Owner = company.Id, X = pos.X, Y = pos.Y };
                    World.LoadBuilding(building);
                    buildings[(x, y)] = building;
                }
            }
        }
        World.SolveNetworks();
        return buildings;
    }

    public void Pass(double minutes)
    {
        Time = Time.AddMinutes(minutes);
        World.Tick();
    }

    public Company NewCompany(string name = "Test Co", double gold = 1_000_000, int level = 1, int? slot = null)
    {
        var company = World.CreateCompany(World.Companies.Count() + 1, name, "#ff8800", seat: slot is { } s ? new EstateKey(new DistrictCoord(0, 0), s) : null);
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

    /// <summary>Connects a building to the company's Yard gate</summary>
    public List<TilePos> ConnectToGate(Company company, Building from)
    {
        var estate = EstateOf(company);
        return Connect(company, Pipeable(from.Border()), Pipeable(Neighbours(estate.YardGate).Where(t => CanPipe(company, t))));
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
        // Pipes go on the company's own estate and through the wilderness (its outposts there too)
        var zone = WorldLayout.ZoneAt(tile.X, tile.Y);
        if (zone.Kind == ZoneKind.Wilderness ? World.OutpostAt(tile) is { } outpost && outpost.Owner != company.Id
            : zone.Kind != ZoneKind.Estate || !EstateOf(company).Contains(tile))
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
