using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

/// <summary>
/// Achievements and collections (GDD §8.5). Counters live in Company.Stats; the rest is read from the company. Checked
/// at most once a minute.
/// </summary>
public sealed partial class GameWorld
{
    private static readonly TimeSpan AchievementCheckEvery = TimeSpan.FromMinutes(1);
    private DateTime _lastAchievementCheck;

    /// <summary>Checks achievements (off in most tests: their gold would change the expected numbers)</summary>
    public bool AchievementsEnabled { get; set; } = true;

    public static string CollectionKey(string collection) => $"collection:{collection}";

    public void AddStat(Company company, string stat, double amount)
    {
        if (amount == 0)
        {
            return;
        }
        company.Stats[stat] = company.Stats.GetValueOrDefault(stat) + amount;
    }

    /// <summary>The value of a stat for a company (partners: the counts of the current trade, computed once per check)</summary>
    public double StatValue(Company company, string stat, IReadOnlyDictionary<long, int>? partners = null) => stat switch
    {
        "earned" => company.LifetimeEarnings,
        "partners" => (partners ?? PartnerCounts()).GetValueOrDefault(company.Id),
        "researched" => company.Researched.Count + company.TrackTiers.Values.Sum(),
        "likes" => company.Likes,
        "mentees" => Mentees(company.Id),
        "parcels" => EstateOf(company)?.UnlockedCount ?? 0,
        "estates" => EstatesOf(company).Count(),
        "level" => company.Level,
        // The highest level of a building (the HQ grows with the Company Level, it doesn't count)
        "maxLevel" => BuildingsOf(company.Id).Where(t => Content.Building(t.Type).Kind != BuildingKind.Hq).Select(t => t.Level).DefaultIfEmpty(0).Max(),
        // Gold a minute neighbours pay now (the Yard and links)
        "neighbourIncome" => NeighbourIncome(company),
        _ => company.Stats.GetValueOrDefault(stat),
    };

    /// <summary>How far a company is with a collection: the buildings of the list it has at the level</summary>
    public int CollectionProgress(Company company, CollectionDefinition collection)
    {
        var levels = BuildingsOf(company.Id).Where(t => !t.UnderConstruction).GroupBy(t => t.Type).ToDictionary(t => t.Key, t => t.Max(b => b.Level));
        return collection.Buildings.Count(t => levels.GetValueOrDefault(t) >= collection.Level);
    }

    private void CheckAchievements()
    {
        if (!AchievementsEnabled || LastTick - _lastAchievementCheck < AchievementCheckEvery || (Content.Achievements.Count == 0 && Content.Collections.Count == 0))
        {
            return;
        }
        _lastAchievementCheck = LastTick;
        Dictionary<long, int>? partners = null;
        foreach (var company in _companies.Values)
        {
            foreach (var achievement in Content.Achievements)
            {
                if (company.Achievements.Contains(achievement.Id))
                {
                    continue;
                }
                if (achievement.Stat == "partners")
                {
                    partners ??= PartnerCounts();
                }
                if (StatValue(company, achievement.Stat, partners) >= achievement.Amount)
                {
                    Unlock(company, achievement.Id, achievement.Name, achievement.Hours, achievement.Gold, achievement.Title, achievement.Output != null);
                }
            }
            foreach (var collection in Content.Collections)
            {
                var key = CollectionKey(collection.Id);
                if (!company.Achievements.Contains(key) && CollectionProgress(company, collection) >= collection.Buildings.Count)
                {
                    Unlock(company, key, collection.Name, collection.Hours, 0, collection.Title, false);
                }
            }
        }
    }

    /// <summary>An achievement or collection reached: hours of the company's own income (at least the gold), a title</summary>
    private void Unlock(Company company, string key, string name, double hours, double gold, string? title, bool changesOutput)
    {
        company.Achievements.Add(key);
        AddMoment(company, "achievement", key.StartsWith("collection:", StringComparison.Ordinal) ? $"Collection: {name}" : $"Achievement: {name}", false);
        gold = Math.Round(Math.Max(gold, hours * 60 * MeasuredIncome(company)));
        company.Gold += gold;
        CountEarned(company, gold, "achievements", name);
        if (title != null)
        {
            company.Titles.Add(title);
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        var rewards = new List<string>();
        if (gold > 0)
        {
            rewards.Add($"+{Gold(gold)}");
        }
        if (title != null)
        {
            rewards.Add($"title \"{title}\"");
        }
        Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Achievement, $"{(key.StartsWith("collection:") ? "Collection" : "Achievement")}: {name}! {string.Join(", ", rewards)}"));
        if (changesOutput)
        {
            MarkCompanyNetworksDirty(company.Id);
        }
    }

    /// <summary>The small output bonuses of achievements</summary>
    private double AchievementOutputBonus(Company company, BuildingDefinition building, string resource)
    {
        var bonus = 0.0;
        foreach (var achievement in Content.Achievements)
        {
            if (achievement.Output is { } output && company.Achievements.Contains(achievement.Id) && output.Applies(building, resource))
            {
                bonus += output.Amount;
            }
        }
        return bonus;
    }
}
