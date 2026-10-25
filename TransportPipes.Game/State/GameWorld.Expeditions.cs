using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// Expeditions (GDD §11.3): from the Expedition Office a team goes into the wilderness for 1, 4 or 12 hours and comes
/// back with gold and, by luck, the location of a rich deposit, a decoration in a crate or a relic. A classic idle timer
/// with a surprise.
/// </summary>
public sealed partial class GameWorld
{
    public static readonly int[] ExpeditionHours = [1, 4, 12];
    private const int ExpeditionLogLength = 10;
    public const string RelicTitle = "Archaeologist";

    /// <summary>Supplies for the team: more for longer trips and bigger companies</summary>
    public static double ExpeditionCost(Company company, int hours) => Math.Round(hours * (500.0 + 100 * company.Level) / 100) * 100;

    public string? ExpeditionBlocker(Company company)
    {
        if (!HasBuilding(company.Id, BuildingKind.ExpeditionOffice))
        {
            return "Build an Expedition Office first.";
        }
        if (company.Expedition is { } running)
        {
            return $"Your team is out until {running.EndsAt:HH:mm} UTC.";
        }
        return null;
    }

    public CommandResult StartExpedition(long companyId, int hours)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (!ExpeditionHours.Contains(hours))
        {
            return CommandResult.Fail("Expeditions take 1, 4 or 12 hours.");
        }
        if (ExpeditionBlocker(company) is { } blocker)
        {
            return CommandResult.Fail(blocker);
        }
        var payment = Pay(company, ExpeditionCost(company, hours), new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }
        company.Expedition = new Expedition(hours, Now, Now.AddHours(hours));
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    private IEnumerable<(DateTime At, Action Run)> ExpeditionMoments(DateTime from, DateTime to)
    {
        foreach (var company in _companies.Values)
        {
            if (company.Expedition is { } expedition && expedition.EndsAt <= to)
            {
                yield return (expedition.EndsAt > from ? expedition.EndsAt : from, () => ExpeditionBack(company));
            }
        }
    }

    /// <summary>The team is back: gold always, and by luck a deposit, a crate or a relic (longer trips, better luck)</summary>
    private void ExpeditionBack(Company company)
    {
        if (company.Expedition is not { } expedition)
        {
            return;
        }
        company.Expedition = null;
        var random = new SeededRandom(Noise.Hash(Generator.Seed, 7000, (int)company.Id, (int)(expedition.StartedAt.Ticks / TimeSpan.TicksPerMinute % int.MaxValue)));
        var luck = expedition.Hours switch { 1 => 0.25, 4 => 0.55, _ => 0.9 };
        var finds = new List<string>();

        var gold = Math.Round(ExpeditionCost(company, expedition.Hours) * (1.2 + random.NextDouble() * 0.8));
        company.Gold += gold;
        finds.Add($"{gold:N0} gold");

        if (random.NextDouble() < luck)
        {
            var roll = random.NextDouble();
            var missing = Content.Relics.Where(t => !company.Relics.Contains(t.Id)).ToList();
            if (roll < 0.35 && missing.Count > 0)
            {
                var relic = missing[random.Next(missing.Count)];
                company.Relics.Add(relic.Id);
                finds.Add($"a relic: {relic.Name}");
                if (company.Relics.Count == Content.Relics.Count && company.Titles.Add(RelicTitle))
                {
                    finds.Add($"the title {RelicTitle}");
                }
            }
            else if (roll < 0.7 && WildSpotNear(company.District, random) is { } spot)
            {
                var resources = YourEra(company) >= 3 ? new[] { "oil", "rareEarth", "copperOre", "coal" } : ["copperOre", "coal", "ironOre"];
                var resource = resources[random.Next(resources.Length)];
                StartDiscovery(spot, resource);
                finds.Add($"a rich {Content.Resources[resource].Name.ToLowerInvariant()} deposit in the wilderness");
            }
            else
            {
                var decorations = Content.BuildingList.Where(t => t.Kind == BuildingKind.Decoration && t.Buildable && t.Unlock.Collection == null && t.Layer != BuildingLayer.Deep).ToList();
                if (decorations.Count > 0)
                {
                    var decoration = decorations[random.Next(decorations.Count)];
                    company.Crates.Add(new PackedBuilding { Type = decoration.Id });
                    finds.Add($"a {decoration.Name} in a crate");
                }
            }
        }

        var text = $"Your {expedition.Hours} h expedition is back with {string.Join(", ", finds)}";
        company.ExpeditionLog.Insert(0, text);
        if (company.ExpeditionLog.Count > ExpeditionLogLength)
        {
            company.ExpeditionLog.RemoveRange(ExpeditionLogLength, company.ExpeditionLog.Count - ExpeditionLogLength);
        }
        AddStat(company, "expeditions", 1);
        company.Touch();
        Changes.Companies.Add(company.Id);
        Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Achievement, text));
    }
}
