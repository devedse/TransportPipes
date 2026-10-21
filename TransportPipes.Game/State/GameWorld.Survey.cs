using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// The starter kit (docs/v1.1-design.html §6–7): every estate has copper, coal and stone (a 2×2 at 2★) in its start
/// parcels and a shore within its start parcels or its first parcel, so the buildings of Company Level 5 can be used.
/// The generator only guarantees iron; surveyors add what's missing as deposit seeds and a pond (terrain overrides),
/// only on free land, so this works on running worlds. Every estate is surveyed once.
/// </summary>
public sealed partial class GameWorld
{
    public static readonly string[] StarterDeposits = ["copperOre", "coal", "stone"];

    private const int StarterRichness = 2;

    private readonly HashSet<(DistrictCoord District, int Slot)> _surveyed = new();

    public IReadOnlyCollection<(DistrictCoord District, int Slot)> Surveyed => _surveyed;

    /// <summary>New estates get their starter kit (off in most tests: the deposits would change their maps)</summary>
    public bool SurveysEnabled { get; set; } = true;

    public void LoadSurveyed(IEnumerable<(DistrictCoord District, int Slot)> estates) => _surveyed.UnionWith(estates);

    /// <summary>Surveys every player's estate that wasn't yet; returns per company what was found</summary>
    public List<(long Company, string Text)> SurveyEstates()
    {
        var found = new List<(long, string)>();
        foreach (var estate in _estates.Values.Where(t => t.OwnerId is { } owner && Company(owner) is { IsNpc: false }).ToList())
        {
            if (Survey(estate) is { Count: > 0 } added)
            {
                found.Add((estate.OwnerId!.Value, $"Surveyors found {Join(added)} on your estate."));
            }
        }
        return found;
    }

    /// <summary>Adds the missing basics to an estate (once); returns what was added ("copper", "a pond"...)</summary>
    public List<string> Survey(Estate estate)
    {
        var added = new List<string>();
        if (!_surveyed.Add((estate.District, estate.Slot)))
        {
            return added;
        }
        var start = Content.Progression.StartParcels.ToList();
        var reach = start.Concat(Content.Progression.ParcelOrder.Take(1)).ToList();
        var startTiles = start.SelectMany(t => ParcelTiles(estate, t)).ToList();
        var reachTiles = reach.SelectMany(t => ParcelTiles(estate, t)).ToList();
        var hq = estate.Origin.Offset(HqLocal.X, HqLocal.Y);

        foreach (var resource in StarterDeposits)
        {
            if (reachTiles.Any(t => DepositMatches(resource, Tile(t).Deposit)))
            {
                continue;
            }
            // A 2×2 on free land, a few tiles from the HQ (not in its way, not far)
            var spot = startTiles
                .Where(t => Distance(t, hq) >= 4)
                .OrderBy(t => Math.Abs(Distance(t, hq) - 7) + hash(t) * 0.5)
                .Select(t => (TilePos?)t)
                .FirstOrDefault(t => Square(t!.Value, 2).All(c => FreeForSurvey(estate, c)));
            if (spot is not { } corner)
            {
                continue;
            }
            foreach (var tile in Square(corner, 2))
            {
                _seeds[tile] = (resource, StarterRichness);
                MarkDirty(tile);
                Changes.Tiles.Add(tile);
            }
            added.Add(Content.Resources[resource].Name.ToLowerInvariant().Replace(" ore", ""));
        }

        // A shore: water next to land, within reach
        if (!reachTiles.Any(t => Tile(t).Terrain == TerrainType.Water))
        {
            var first = Content.Progression.ParcelOrder.Take(1).SelectMany(t => ParcelTiles(estate, t));
            foreach (var centre in first.Concat(startTiles).OrderBy(t => hash(t)))
            {
                var pond = Square(centre.Offset(-1, -1), 3).Where(t => t != centre.Offset(-1, -1) && t != centre.Offset(1, 1)).ToList();
                if (pond.All(t => FreeForSurvey(estate, t)) && Ring(centre, 2).All(t => estate.Contains(t) && !IsOccupied(t)))
                {
                    foreach (var tile in pond)
                    {
                        _terrainOverrides[tile] = TerrainType.Water;
                        MarkDirty(tile);
                        Changes.Tiles.Add(tile);
                    }
                    added.Add("a pond");
                    break;
                }
            }
        }

        estate.Touch();
        Changes.Estates.Add((estate.District, estate.Slot));
        if (added.Count > 0 && estate.OwnerId is { } owner)
        {
            Changes.Events.Add(new WorldEvent(owner, WorldEventKind.Achievement, $"Surveyors found {Join(added)} on your estate!"));
        }
        return added;

        static double hash(TilePos t) => (((t.X * 73856093) ^ (t.Y * 19349663)) & 0xffff) / 65536.0;
    }

    private static IEnumerable<TilePos> ParcelTiles(Estate estate, int parcel)
    {
        var (px, py) = WorldLayout.ParcelOrigin(parcel);
        for (var y = 0; y < WorldLayout.ParcelSize; y++)
        {
            for (var x = 0; x < WorldLayout.ParcelSize; x++)
            {
                yield return estate.Origin.Offset(px + x, py + y);
            }
        }
    }

    private static IEnumerable<TilePos> Square(TilePos corner, int size)
    {
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                yield return corner.Offset(x, y);
            }
        }
    }

    private static IEnumerable<TilePos> Ring(TilePos centre, int radius)
    {
        for (var y = -radius; y <= radius; y++)
        {
            for (var x = -radius; x <= radius; x++)
            {
                yield return centre.Offset(x, y);
            }
        }
    }

    private static int Distance(TilePos a, TilePos b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    /// <summary>A tile the surveyors may change: in the estate, on open land with nothing on it or next to it, no deposit</summary>
    private bool FreeForSurvey(Estate estate, TilePos tile)
    {
        if (!estate.Contains(tile) || _seeds.ContainsKey(tile) || _terrainOverrides.ContainsKey(tile))
        {
            return false;
        }
        var info = Tile(tile);
        if (info.Deposit != null || info.Terrain is not (TerrainType.Grass or TerrainType.Desert or TerrainType.Beach or TerrainType.Hills))
        {
            return false;
        }
        // Nothing on it or right next to it (a pipe, a building, a gate)
        return Ring(tile, 1).All(t => !IsOccupied(t));
    }

    private static string Join(List<string> items) =>
        items.Count == 1 ? items[0] : $"{string.Join(", ", items.Take(items.Count - 1))} and {items[^1]}";
}
