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

    /// <summary>Checks achievements (off in most tests: their XP would change the expected numbers)</summary>
    public bool AchievementsEnabled { get; set; } = true;

    public static string CollectionKey(string collection) => $"collection:{collection}";

    public void AddStat(Company company, string stat, double amount)
    {
        if (company.IsNpc || amount == 0)
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
        "contracts" => company.ContractsCompleted,
        "reputation" => ReputationLevel(company),
        "researched" => company.Researched.Count,
        "likes" => company.Likes,
        "corporation" => company.CorporationId != null ? 1 : 0,
        "mentees" => Mentees(company.Id),
        "parcels" => EstateOf(company)?.UnlockedCount ?? 0,
        "level" => company.Level,
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
        foreach (var company in _companies.Values.Where(t => !t.IsNpc))
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
                    Unlock(company, achievement.Id, achievement.Name, achievement.Xp, achievement.Gold, achievement.Title, achievement.Output != null);
                }
            }
            foreach (var collection in Content.Collections)
            {
                var key = CollectionKey(collection.Id);
                if (!company.Achievements.Contains(key) && CollectionProgress(company, collection) >= collection.Buildings.Count)
                {
                    Unlock(company, key, collection.Name, collection.Xp, 0, collection.Title, false);
                }
            }
        }
    }

    private void Unlock(Company company, string key, string name, double xp, double gold, string? title, bool changesOutput)
    {
        company.Achievements.Add(key);
        company.Gold += gold;
        if (title != null)
        {
            company.Titles.Add(title);
        }
        GainXp(company, xp);
        company.Touch();
        Changes.Companies.Add(company.Id);
        var rewards = new List<string>();
        if (xp > 0)
        {
            rewards.Add($"+{xp:N0} XP");
        }
        if (gold > 0)
        {
            rewards.Add($"+{gold:N0} gold");
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
