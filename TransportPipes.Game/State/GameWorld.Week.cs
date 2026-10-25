using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>The week (the weekly leaderboards count from Monday) and a few small helpers about land and goods</summary>
public sealed partial class GameWorld
{
    /// <summary>The next Monday midnight (UTC)</summary>
    public static DateTime NextWeek(DateTime after)
    {
        var days = ((int)DayOfWeek.Monday - (int)after.DayOfWeek + 7) % 7;
        var next = after.Date.AddDays(days == 0 ? 7 : days);
        return DateTime.SpecifyKind(next, DateTimeKind.Utc);
    }

    private void NewWeek()
    {
        foreach (var company in _companies.Values)
        {
            company.WeekStartEarnings = company.LifetimeEarnings;
            company.Touch();
        }
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

    private static int Distance(TilePos a, TilePos b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    /// <summary>What one level-1 building that makes a good makes of it a minute (the cheapest such producer)</summary>
    public double OneBuildingRate(string good) =>
        Content.BuildingList.Where(t => t.Buildable && t.Kind == BuildingKind.Producer && t.Out.ContainsKey(good))
            .OrderBy(t => t.Unlock.Level).ThenBy(t => t.Cost.Gold).Select(t => t.Out[good]).FirstOrDefault(1);
}
