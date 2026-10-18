using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

public enum LeaderboardKind
{
    NetWorth,
    Income,
    WeeklyEarnings,
    Level,
    Partners,
    Likes,
    Efficiency,
}

public sealed record LeaderboardEntry(long Company, string Name, string Color, double Value, int Rank);

/// <summary>Likes, mentors and leaderboards (GDD §9.5, §9.6)</summary>
public sealed partial class GameWorld
{
    public const int MentorLevel = 30;
    public const int MenteeMaxLevel = 10;
    public const int MaxMentees = 5;
    public static readonly int[] MentorRewardLevels = [10, 15, 20];

    private readonly HashSet<(long Liker, long Owner)> _likesToday = new();

    /// <summary>One like per estate per day: the owner gets a little gold and XP</summary>
    public CommandResult LikeEstate(long likerId, long ownerId)
    {
        if (Company(likerId) is not { } liker || Company(ownerId) is not { } owner || owner.IsNpc)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (likerId == ownerId)
        {
            return CommandResult.Fail("You can't like your own estate.");
        }
        if (!_likesToday.Add((likerId, ownerId)))
        {
            return CommandResult.Fail("You liked this estate today already.");
        }
        // Festivals: likes count double
        var factor = LikeFactor(owner.District);
        owner.Likes += factor;
        owner.Report.LikesReceived += factor;
        owner.Gold += 20 * owner.Level * factor;
        GainXp(owner, 10 * factor);
        owner.Touch();
        Changes.Companies.Add(owner.Id);
        Changes.Events.Add(new WorldEvent(owner.Id, WorldEventKind.Social, $"{liker.Name} likes your estate!"));
        return CommandResult.Ok;
    }

    public bool LikedToday(long likerId, long ownerId) => _likesToday.Contains((likerId, ownerId));

    /// <summary>A newcomer chooses a mentor (Company Level 30+); both are rewarded at levels 10, 15 and 20</summary>
    public CommandResult ChooseMentor(long newcomerId, long mentorId)
    {
        if (Company(newcomerId) is not { } newcomer || Company(mentorId) is not { IsNpc: false } mentor)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (newcomer.MentorId != null)
        {
            return CommandResult.Fail("You have a mentor already.");
        }
        if (newcomer.Level >= MenteeMaxLevel)
        {
            return CommandResult.Fail($"Mentors are for companies below Level {MenteeMaxLevel}.");
        }
        if (mentor.Level < MentorLevel)
        {
            return CommandResult.Fail($"A mentor needs Company Level {MentorLevel}.");
        }
        if (_companies.Values.Count(t => t.MentorId == mentorId) >= MaxMentees)
        {
            return CommandResult.Fail($"{mentor.Name} has {MaxMentees} newcomers already.");
        }
        newcomer.MentorId = mentorId;
        newcomer.Touch();
        Changes.Companies.Add(newcomer.Id);
        Changes.Events.Add(new WorldEvent(mentorId, WorldEventKind.Social, $"{newcomer.Name} chose you as mentor"));
        return CommandResult.Ok;
    }

    /// <summary>Mentorship rewards when a newcomer reaches a level</summary>
    private void MentorRewards(Company newcomer)
    {
        if (newcomer.MentorId is not { } mentorId || Company(mentorId) is not { } mentor)
        {
            return;
        }
        foreach (var level in MentorRewardLevels.Where(t => newcomer.Level >= t && !newcomer.MentorRewards.Contains(t)))
        {
            newcomer.MentorRewards.Add(level);
            var gold = 1000.0 * level;
            newcomer.Gold += gold;
            mentor.Gold += gold;
            mentor.Touch();
            Changes.Companies.Add(mentor.Id);
            Changes.Events.Add(new WorldEvent(mentor.Id, WorldEventKind.Social, $"{newcomer.Name} reached level {level}: +{gold:N0} gold for you both"));
            Changes.Events.Add(new WorldEvent(newcomer.Id, WorldEventKind.Social, $"Mentor reward for level {level}: +{gold:N0} gold"));
        }
    }

    /// <summary>The value of a company: gold plus what its buildings, upgrades and parcels cost</summary>
    public double NetWorth(Company company)
    {
        var value = company.Gold;
        foreach (var building in BuildingsOf(company.Id))
        {
            var definition = Content.Building(building.Type);
            value += definition.Cost.Gold;
            for (var level = 1; level < building.Level; level++)
            {
                value += Content.Progression.UpgradeCost(definition, level);
            }
        }
        if (EstateOf(company) is { } estate)
        {
            var extra = estate.UnlockedCount - Content.Progression.StartParcels.Count;
            for (var i = 0; i < extra; i++)
            {
                value += Content.Progression.ParcelCost(i);
            }
        }
        return value;
    }

    /// <summary>The number of companies each company trades with right now (trade and contracts)</summary>
    public Dictionary<long, int> PartnerCounts()
    {
        var pairs = new HashSet<(long, long)>();
        foreach (var network in _networks.Values)
        {
            foreach (var flow in network.Solution.Flows.Where(t => t.Kind is Simulation.FlowKind.Trade or Simulation.FlowKind.Contract))
            {
                var a = _buildings[flow.From].Owner;
                var b = _buildings[flow.To].Owner;
                if (a != b)
                {
                    pairs.Add((a, b));
                    pairs.Add((b, a));
                }
            }
        }
        var partners = new Dictionary<long, int>();
        foreach (var (a, _) in pairs)
        {
            partners[a] = partners.GetValueOrDefault(a) + 1;
        }
        return partners;
    }

    public int Mentees(long mentorId) => _companies.Values.Count(t => t.MentorId == mentorId);

    /// <summary>The top companies (players only), of one district or of the whole world</summary>
    public List<LeaderboardEntry> Leaderboard(LeaderboardKind kind, DistrictCoord? district, int count = 50)
    {
        Dictionary<long, double>? income = kind == LeaderboardKind.Income ? GoldPerMinute() : null;
        var partners = kind == LeaderboardKind.Partners ? PartnerCounts() : null;
        double Value(Company company) => kind switch
        {
            LeaderboardKind.NetWorth => NetWorth(company),
            LeaderboardKind.Income => (income!.GetValueOrDefault(company.Id)) * 60,
            LeaderboardKind.WeeklyEarnings => company.LifetimeEarnings - company.WeekStartEarnings,
            LeaderboardKind.Level => company.Xp,
            LeaderboardKind.Partners => partners!.GetValueOrDefault(company.Id),
            LeaderboardKind.Efficiency => TileEfficiency(company, income ??= GoldPerMinute()),
            _ => company.Likes,
        };
        return _companies.Values
            .Where(t => !t.IsNpc && (district == null || t.District == district))
            .Select(t => (Company: t, Value: Value(t)))
            .OrderByDescending(t => t.Value).ThenBy(t => t.Company.Id)
            .Take(count)
            .Select((t, i) => new LeaderboardEntry(t.Company.Id, t.Company.Name, t.Company.Color, t.Value, i + 1))
            .ToList();
    }
}
