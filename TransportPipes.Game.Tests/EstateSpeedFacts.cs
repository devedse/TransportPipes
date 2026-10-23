using System.Diagnostics;
using System.Text;
using TransportPipes.Game.State;

namespace TransportPipes.Game.Tests;

/// <summary>What a build costs on a big estate: each step it causes, timed (TP_PERF=1; the report is estate-speed.md)</summary>
[TestClass]
public class EstateSpeedFacts
{
    /// <summary>Rows of buildings on both sides of pipes, joined by a pipe down the left: one network</summary>
    private static string Comb(int width, int groups)
    {
        const string letters = "WWSMFWRWMFOWSMFW";
        var rows = new List<string>();
        for (var g = 0; g < groups; g++)
        {
            for (var r = 0; r < 3; r++)
            {
                var row = new StringBuilder("+");
                for (var x = 1; x < width; x++)
                {
                    row.Append(r == 1 ? '+' : x == width - 1 && g == 0 && r == 0 ? 'T' : letters[(x * 3 + g * 5 + r) % letters.Length]);
                }
                // The pipe runs on past the buildings: a free tile beside it for the new building
                row.Append(r == 1 ? "++" : "  ");
                rows.Add(row.ToString());
            }
        }
        return string.Join('\n', rows);
    }

    /// <summary>
    /// A pipe that snakes across the estate with a building every few tiles above it: long pipes, like a grown estate
    /// (the play day's network had 68 buildings on 1,050 pipe tiles)
    /// </summary>
    private static string Snake(int width, int rows, int spacing)
    {
        const string letters = "WSMFWRMFOWSMFW";
        var lines = new List<string>();
        for (var r = 0; r < rows; r++)
        {
            var above = new StringBuilder();
            for (var x = 0; x < width; x++)
            {
                above.Append(x % spacing == 1 && x < width - 2 ? r == 0 && x == 1 ? 'T' : letters[(x / spacing + r * 3) % letters.Length] : ' ');
            }
            lines.Add(above.ToString());
            lines.Add(new string('+', width));
            // The turn to the next row, at alternate ends
            lines.Add(r == rows - 1 ? "" : r % 2 == 0 ? new string(' ', width - 1) + "+" : "+");
        }
        return string.Join('\n', lines);
    }

    [TestMethod]
    [DataRow("comb", 14, 1, 0)]
    [DataRow("comb", 14, 3, 0)]
    [DataRow("comb", 28, 4, 0)]
    [DataRow("snake", 28, 4, 3)]
    [DataRow("snake", 28, 8, 3)]
    [DataRow("snake", 28, 9, 2)]
    public void ABuildOnABigEstate(string layout, int width, int groups, int spacing)
    {
        if (Environment.GetEnvironmentVariable("TP_PERF") != "1")
        {
            Assert.Inconclusive("Set TP_PERF=1 to time a build on big estates.");
        }
        var game = new TestGame();
        var company = game.NewCompany(gold: 1e9, level: 30);
        for (var parcel = 0; parcel < 16; parcel++)
        {
            game.World.BuyParcel(company.Id, parcel);
        }
        var origin = game.Local(company, 1, 1);
        var map = game.Map(company, origin, layout == "comb" ? Comb(width, groups) : Snake(width, groups, spacing), pipeTier: 2);
        game.ConnectToGate(company, map.Values.First());
        game.Pass(2);
        var network = game.World.Networks.Single(t => t.Buildings.Contains(map.Values.First().Id));
        var report = new StringBuilder($"## {layout}: {map.Count} buildings, {network.Conductors.Count} pipe tiles, {network.Links.Count} links\n\n");
        var times = new List<(string Step, double Ms)>();
        T Time<T>(string step, Func<T> action)
        {
            var clock = Stopwatch.StartNew();
            var result = action();
            times.Add((step, clock.Elapsed.TotalMilliseconds));
            return result;
        }

        for (var round = 0; round < 2; round++)
        {
            times.Clear();
            // Beside the pipe, past the buildings
            var spot = layout == "comb" ? origin.Offset(width, round == 0 ? 0 : 2) : origin.Offset(width - 1 - round, 0);
            var placed = Time("PlaceBuilding", () => game.World.PlaceBuilding(company.Id, "windMoily", spot));
            Assert.IsTrue(placed.Success, placed.Error);
            Time("Tick (2 s later)", () => { game.Time = game.Time.AddSeconds(2); game.World.Tick(); return 0; });
            Time("TakeChanges", () => game.World.TakeChanges());
            Time("GoldPerMinute (every company)", () => game.World.GoldPerMinute());
            Time("NextSteps", () => game.World.NextSteps(company));
            var building = map.Values.First(t => t.Type == "smallFactory");
            Time("UpgradePreview (1 level)", () => game.World.UpgradePreview(company, building, 1));
            Time("SmartMax", () => game.World.SmartMax(company, building));
            Time("Tick (nothing changed)", () => { game.Time = game.Time.AddSeconds(2); game.World.Tick(); return 0; });
            var built = game.World.BuildingAt(spot)!;
            Time("Demolish", () => game.World.Demolish(company.Id, built.Id));
            Time("Tick after demolish", () => { game.Time = game.Time.AddSeconds(2); game.World.Tick(); return 0; });
            report.AppendLine($"Round {round + 1}: " + string.Join(", ", times.Select(t => $"{t.Step} {t.Ms:F0} ms")));
        }
        File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "estate-speed.md"), report + "\n");
    }
}
