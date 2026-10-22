using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>A pipe the game plans for a player (F2's fix button, Loady's pipe step on a phone): the new tiles and their price</summary>
/// <param name="Tiles">The whole route, own pipes it walks along included (they cost nothing)</param>
/// <param name="NewTiles">How many of them are new</param>
/// <param name="Cost">The gold for the new tiles (bridges over water cost three times as much)</param>
/// <param name="Target">The building it reaches (its trading post, a Storage), or null for the Yard gate</param>
public sealed record PipePlan(IReadOnlyList<TilePos> Tiles, int NewTiles, double Cost, Building? Target);

public sealed partial class GameWorld
{
    /// <summary>The most tiles a planned pipe looks at (an estate and a bit)</summary>
    private const int PipePlanSearch = 6000;

    /// <summary>
    /// The cheapest grey pipe from a building to its owner's trading post or a Storage with room (F2's fix for a stuck
    /// building): over the owner's own estate by the player's own pipe rules (any parcel of it, no mountains, bridges
    /// over water at three times the price), along the owner's pipes for free. Null when there is no way.
    /// </summary>
    public PipePlan? PlanFixPipe(Building building)
    {
        if (Company(building.Owner) is not { } company)
        {
            return null;
        }
        var targets = BuildingsOf(company.Id)
            .Where(t => t.Id != building.Id && !t.UnderConstruction && Content.Building(t.Type).Kind switch
            {
                BuildingKind.TradingPost => !TradingPostFull(t),
                BuildingKind.Storage => StorageCapacity(t, Content.Building(t.Type)) - t.StoredTotal > StorageEpsilon,
                _ => false,
            })
            // The trading post first: it sells, a Storage only keeps
            .OrderBy(t => Content.Building(t.Type).Kind == BuildingKind.TradingPost ? 0 : 1)
            .ToList();
        foreach (var target in targets)
        {
            var goal = target.Border().ToHashSet();
            if (PlanPipe(company, building, goal.Contains) is { } plan)
            {
                return plan with { Target = target };
            }
        }
        return null;
    }

    /// <summary>The cheapest grey pipe from a building to the inside of its owner's Yard gate (F2's fix when the best taker is at the gate)</summary>
    public PipePlan? PlanGatePipe(Building building)
    {
        if (Company(building.Owner) is not { } company || EstateOf(company) is not { } estate)
        {
            return null;
        }
        var gate = estate.YardGate;
        return PlanPipe(company, building, t => Math.Abs(t.X - gate.X) + Math.Abs(t.Y - gate.Y) == 1);
    }

    /// <summary>Whether a player may lay a pipe on a tile (as PlacePipes checks it), and what it costs: null when not</summary>
    private double? PipeTileCost(Company company, TilePos tile, PipeTierDefinition pipe)
    {
        if (WorldLayout.ZoneAt(tile.X, tile.Y).Kind != ZoneKind.Estate || OwnEstateAt(company, tile) == null || IsOccupied(tile))
        {
            return null;
        }
        var terrain = Tile(tile).Terrain;
        return terrain == TerrainType.Mountain ? null : pipe.Gold * (terrain == TerrainType.Water ? 3 : 1);
    }

    /// <summary>
    /// The cheapest pipe from a building to the first tile that meets the goal (0-1 search: the owner's own pipes cost
    /// nothing), starting next to the building
    /// </summary>
    private PipePlan? PlanPipe(Company company, Building from, Func<TilePos, bool> goal)
    {
        var pipe = Content.PipeTier(1);
        bool OwnPipe(TilePos t) => _conductors.TryGetValue(t, out var c) && c.Owner == company.Id && c.Kind == ConductorKind.Pipe;
        var cost = new Dictionary<TilePos, double>();
        var previous = new Dictionary<TilePos, TilePos?>();
        var queue = new PriorityQueue<TilePos, double>();
        foreach (var start in from.Border())
        {
            var price = OwnPipe(start) ? 0 : PipeTileCost(company, start, pipe);
            if (price is { } p && (!cost.TryGetValue(start, out var known) || p < known))
            {
                cost[start] = p;
                previous[start] = null;
                queue.Enqueue(start, p);
            }
        }
        var looked = 0;
        while (queue.TryDequeue(out var tile, out var at) && looked++ < PipePlanSearch)
        {
            if (at > cost[tile] + 1e-9)
            {
                continue;
            }
            if (goal(tile))
            {
                var path = new List<TilePos>();
                for (TilePos? t = tile; t != null; t = previous[t.Value])
                {
                    path.Add(t.Value);
                }
                path.Reverse();
                return new PipePlan(path, path.Count(t => !OwnPipe(t)), at, null);
            }
            foreach (var next in new[] { tile.Offset(1, 0), tile.Offset(-1, 0), tile.Offset(0, 1), tile.Offset(0, -1) })
            {
                var price = OwnPipe(next) ? 0 : PipeTileCost(company, next, pipe);
                if (price is not { } p)
                {
                    continue;
                }
                var total = at + p;
                if (!cost.TryGetValue(next, out var known) || total < known - 1e-9)
                {
                    cost[next] = total;
                    previous[next] = tile;
                    queue.Enqueue(next, total);
                }
            }
        }
        return null;
    }
}
