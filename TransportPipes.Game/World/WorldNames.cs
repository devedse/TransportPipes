namespace TransportPipes.Game.World;

/// <summary>Names of districts: the Capital in the middle, then names in the order the districts open</summary>
public static class WorldNames
{
    private static readonly string[] Names =
    [
        "Riverbend", "Copperfield", "Windhaven", "Stonebridge", "Ironvale", "Sunnyside", "Oakmere", "Coalbrook",
        "Glassford", "Brightwater", "Millbrook", "Redcliff", "Ashwood", "Pipewell", "Clearlake", "Highmoor",
        "Fernhill", "Saltmarsh", "Goldcrest", "Thornbury", "Mistvale", "Kettleby", "Larkspur", "Northwick",
    ];

    public static string District(DistrictCoord district)
    {
        if (district == new DistrictCoord(0, 0))
        {
            return "Capital";
        }
        var index = WorldLayout.DistrictOrder().Take(10_000).Select((t, i) => (t, i)).First(t => t.t == district).i - 1;
        var name = Names[index % Names.Length];
        return index < Names.Length ? name : $"{name} {index / Names.Length + 1}";
    }
}
