using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
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

    /// <summary>Your league (v1.2 §13): about 25 companies at a similar stage, by what they earned this week</summary>
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
        if (Company(likerId) is not { } liker || Company(ownerId) is not { } owner)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (likerId == ownerId)
        {
            return CommandResult.Fail("You can't like your own plot.");
        }
        if (!_likesToday.Add((likerId, ownerId)))
        {
            return CommandResult.Fail("You liked this plot today already.");
        }
        // Festivals: likes count double
        var factor = LikeFactor(owner.District);
        owner.Likes += factor;
        owner.Report.LikesReceived += factor;
        var gold = 20 * owner.Level * factor;
        owner.Gold += gold;
        CountEarned(owner, gold, "rewards", "likes");
        owner.Touch();
        Changes.Companies.Add(owner.Id);
        Changes.Events.Add(new WorldEvent(owner.Id, WorldEventKind.Social, $"{liker.Name} likes your plot!"));
        return CommandResult.Ok;
    }

    public bool LikedToday(long likerId, long ownerId) => _likesToday.Contains((likerId, ownerId));

    /// <summary>A newcomer chooses a mentor (Company Level 30+); both are rewarded at levels 10, 15 and 20</summary>
    public CommandResult ChooseMentor(long newcomerId, long mentorId)
    {
        if (Company(newcomerId) is not { } newcomer || Company(mentorId) is not { } mentor)
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
            value += EvolveCredit(building);
        }
        if (EstateOf(company) is { } estate)
        {
            var extra = estate.UnlockedCount - WorldLayout.StartParcelCount;
            for (var i = 0; i < extra; i++)
            {
                value += Content.Progression.ParcelCost(i);
            }
        }
        return value;
    }

    /// <summary>
    /// What the residents of a company's station could trade through the Yard, as the company sees it (the station
    /// panel, Loady's neighbour card): per resident what it has spare (it goes to the hub) and what its buildings lack,
    /// and what it trades with <paramref name="me"/> now (earn: me selling to it, pay: me buying from it). Connected are
    /// the residents whose own pipes reach their Yard gate.
    /// </summary>
    public Neighbourhood NeighbourhoodOf(Company me)
    {
        var surplus = new Dictionary<long, Dictionary<string, double>>();
        var needs = new Dictionary<long, Dictionary<string, double>>();
        var shortfalls = new Dictionary<long, List<IReadOnlyDictionary<string, double>>>();
        var earn = new Dictionary<long, double>();
        var pay = new Dictionary<long, double>();
        var connected = new HashSet<long> { me.Id };
        var links = new Dictionary<long, (double Efficiency, int Tiles)>();
        if (EstateOf(me) is not { } home)
        {
            return new Neighbourhood(surplus, needs, shortfalls, earn, pay, connected, links);
        }
        SolveNetworks();
        foreach (var member in YardMembers(home.Station))
        {
            var id = member.Company.Id;
            connected.Add(id);
            links[id] = (1, 0);
            surplus[id] = member.Spare.Where(t => t.Value > 0.01).ToDictionary(t => t.Key, t => t.Value);
            needs[id] = member.Lacks.Where(t => t.Value > 0.01).ToDictionary(t => t.Key, t => t.Value);
            if (needs[id].Count > 0)
            {
                shortfalls[id] = [needs[id]];
            }
        }
        // Residents whose pipes don't reach their gate yet need everything their buildings use
        foreach (var slot in WorldLayout.StationSlots(home.Station.Index))
        {
            if (Estate(home.District, slot)?.OwnerId is not { } owner || connected.Contains(owner) || Company(owner) is not { } resident)
            {
                continue;
            }
            var missing = new Dictionary<string, double>();
            foreach (var building in BuildingsOf(owner).Where(t => !t.UnderConstruction))
            {
                if (ToFlowNode(building) is { Kind: FlowNodeKind.Building } node)
                {
                    foreach (var (resource, amount) in node.In)
                    {
                        missing[resource] = missing.GetValueOrDefault(resource) + amount;
                    }
                }
            }
            needs[resident.Id] = missing;
            if (missing.Count > 0)
            {
                shortfalls[resident.Id] = [missing];
            }
        }
        foreach (var (partner, resource, perMinute, selling) in YardTrades(home))
        {
            if (Company(partner) is not { } other)
            {
                continue;
            }
            if (selling)
            {
                earn[partner] = earn.GetValueOrDefault(partner) + perMinute * SettlementPrice(me, other, resource);
            }
            else
            {
                pay[partner] = pay.GetValueOrDefault(partner) + perMinute * SettlementPrice(other, me, resource);
            }
        }
        return new Neighbourhood(surplus, needs, shortfalls, earn, pay, connected, links);
    }

    /// <summary>The number of companies each company trades with right now (through the Yards)</summary>
    public Dictionary<long, int> PartnerCounts()
    {
        var pairs = new HashSet<(long, long)>();
        foreach (var (key, terms) in _gateTerms)
        {
            if (Estate(key.District, key.Slot)?.OwnerId is not { } buyer)
            {
                continue;
            }
            foreach (var sources in terms.Import.Values)
            {
                foreach (var seller in sources.Keys.Where(t => t != buyer))
                {
                    pairs.Add((seller, buyer));
                    pairs.Add((buyer, seller));
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
            LeaderboardKind.Level => company.LifetimeEarnings,
            LeaderboardKind.Partners => partners!.GetValueOrDefault(company.Id),
            LeaderboardKind.Efficiency => TileEfficiency(company, income ??= GoldPerMinute()),
            _ => company.Likes,
        };
        return _companies.Values
            .Where(t => district == null || t.District == district)
            .Select(t => (Company: t, Value: Value(t)))
            .OrderByDescending(t => t.Value).ThenBy(t => t.Company.Id)
            .Take(count)
            .Select((t, i) => new LeaderboardEntry(t.Company.Id, t.Company.Name, t.Company.Color, t.Value, i + 1))
            .ToList();
    }
}

/// <summary>What the residents of a station could trade, as one company sees it (<see cref="GameWorld.NeighbourhoodOf"/>)</summary>
public sealed record Neighbourhood(
    Dictionary<long, Dictionary<string, double>> Surplus,
    Dictionary<long, Dictionary<string, double>> Needs,
    Dictionary<long, List<IReadOnlyDictionary<string, double>>> Shortfalls,
    Dictionary<long, double> Earn,
    Dictionary<long, double> Pay,
    HashSet<long> Connected,
    Dictionary<long, (double Efficiency, int Tiles)> Links)
{
    /// <summary>Whether a company that makes these could give one of the company's buildings all it misses</summary>
    public bool CanSupply(long company, IReadOnlySet<string> made) =>
        Shortfalls.TryGetValue(company, out var list) && list.Any(t => t.Keys.All(made.Contains));
}
