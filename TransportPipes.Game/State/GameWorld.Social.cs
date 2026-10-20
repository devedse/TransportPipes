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
        GainXp(owner, 10 * factor, "likes", "likes");
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

    /// <summary>
    /// What the companies of a district could trade, as one of them sees it (the Neighbours panel, Loady's neighbour
    /// card): per company what it sells to the hub or leaves unused (surplus), what its buildings miss (needs, and per
    /// building in shortfalls: a building only runs, and buys, when all it misses comes), and what it trades with
    /// <paramref name="me"/> (earn: me selling to it, pay: me buying from it).
    /// </summary>
    public Neighbourhood NeighbourhoodOf(Company me)
    {
        var surplus = new Dictionary<long, Dictionary<string, double>>();
        var needs = new Dictionary<long, Dictionary<string, double>>();
        var earn = new Dictionary<long, double>();
        var pay = new Dictionary<long, double>();
        var connected = new HashSet<long>();
        var shortfalls = new Dictionary<long, List<IReadOnlyDictionary<string, double>>>();
        void AddShortfall(long company, Dictionary<string, double> missing)
        {
            if (missing.Count > 0)
            {
                (shortfalls.TryGetValue(company, out var list) ? list : shortfalls[company] = []).Add(missing);
            }
        }
        void Add(Dictionary<long, Dictionary<string, double>> map, long company, string resource, double amount)
        {
            if (!map.TryGetValue(company, out var rates))
            {
                map[company] = rates = new Dictionary<string, double>();
            }
            rates[resource] = (rates.TryGetValue(resource, out var v) ? v : 0) + amount;
        }

        // Per company on my pipes: the best pipe between one of my buildings and one of its (how much arrives, how long)
        var links = new Dictionary<long, (double Efficiency, int Tiles)>();
        foreach (var network in Networks.Where(t => t.District == me.District))
        {
            var owners = network.Owners(this).ToHashSet();
            if (owners.Contains(me.Id))
            {
                connected.UnionWith(owners);
                foreach (var link in network.Links)
                {
                    var a = Building(link.A)?.Owner;
                    var b = Building(link.B)?.Owner;
                    var other = a == me.Id ? b : b == me.Id ? a : null;
                    if (other is { } id && id != me.Id && (!links.TryGetValue(id, out var best) || link.Efficiency > best.Efficiency))
                    {
                        links[id] = (link.Efficiency, link.Path.Count);
                    }
                }
            }
            var received = new Dictionary<(long, string), double>();
            // Bought from the hub (a tenant's works): they would rather buy it from a neighbour
            var imported = new Dictionary<(long, string), double>();
            foreach (var flow in network.Solution.Flows)
            {
                var from = Building(flow.From)!.Owner;
                var to = Building(flow.To)!.Owner;
                received[(flow.To, flow.Resource)] = (received.TryGetValue((flow.To, flow.Resource), out var r) ? r : 0) + flow.Delivered;
                if (flow.Kind == FlowKind.Import)
                {
                    imported[(flow.To, flow.Resource)] = (imported.TryGetValue((flow.To, flow.Resource), out var i) ? i : 0) + flow.Delivered;
                }
                if (flow.Kind == FlowKind.Sale)
                {
                    // Sold to the hub: they could sell it to you instead
                    Add(surplus, from, flow.Resource, flow.Sent);
                }
                if (flow.Kind is FlowKind.Trade or FlowKind.Contract)
                {
                    var value = flow.Delivered * flow.PricePerUnit;
                    if (from == me.Id)
                    {
                        earn[to] = (earn.TryGetValue(to, out var e) ? e : 0) + value;
                    }
                    if (to == me.Id)
                    {
                        pay[from] = (pay.TryGetValue(from, out var p) ? p : 0) + value;
                    }
                }
            }
            var sent = new Dictionary<(long, string), double>();
            foreach (var flow in network.Solution.Flows)
            {
                sent[(flow.From, flow.Resource)] = (sent.TryGetValue((flow.From, flow.Resource), out var v) ? v : 0) + flow.Sent;
            }
            foreach (var id in network.Buildings)
            {
                if (Building(id) is not { } building || ToFlowNode(building) is not { Kind: FlowNodeKind.Building } node)
                {
                    continue;
                }
                var rate = network.Solution.Rate(id);
                // What it could make but nobody takes: spare for a neighbour (only when it has what it needs to make it)
                var inputsMet = node.In.All(t => (received.TryGetValue((id, t.Key), out var got) ? got : 0) >= t.Value * rate - 1e-6);
                foreach (var (resource, amount) in node.Out)
                {
                    var unused = amount * node.MaxRate - (sent.TryGetValue((id, resource), out var s) ? s : 0);
                    if (unused > 0.01 && (node.In.Count == 0 || inputsMet))
                    {
                        Add(surplus, building.Owner, resource, unused);
                    }
                }
                // What it misses, when its product has somewhere to go (otherwise it doesn't really need it)
                var placeable = node.Out.Count == 0 || network.Links.Any(link =>
                {
                    if (!link.Connects(id, link.A == id ? link.B : link.A))
                    {
                        return false;
                    }
                    var other = Building(link.A == id ? link.B : link.A);
                    if (other == null)
                    {
                        return false;
                    }
                    var kind = Content.Building(other.Type).Kind;
                    return (other.Owner == building.Owner && kind is BuildingKind.Storage or BuildingKind.TradingPost)
                        || ToFlowNode(other) is { } n && node.Out.Keys.Any(n.In.ContainsKey);
                });
                if (!placeable)
                {
                    continue;
                }
                // What it misses together (nothing runs before all of it comes), and what it imports: each of those
                // sells to it on its own (the hub brings the rest)
                var shortfall = new Dictionary<string, double>();
                var fromHub = new List<string>();
                foreach (var (resource, amount) in node.In)
                {
                    var hub = imported.TryGetValue((id, resource), out var bought) ? bought : 0;
                    var missing = amount * node.MaxRate - (received.TryGetValue((id, resource), out var got) ? got : 0) + hub;
                    if (missing > 0.01)
                    {
                        Add(needs, building.Owner, resource, missing);
                        if (hub > 0.01)
                        {
                            fromHub.Add(resource);
                        }
                        else
                        {
                            shortfall[resource] = missing;
                        }
                    }
                }
                AddShortfall(building.Owner, shortfall);
                if (shortfall.Count == 0)
                {
                    foreach (var resource in fromHub)
                    {
                        AddShortfall(building.Owner, new Dictionary<string, double> { [resource] = 1 });
                    }
                }
            }
        }
        // Buildings without any pipe need everything
        foreach (var building in Buildings.Where(t => NetworkOf(t.Id) == null && !t.UnderConstruction))
        {
            if (Company(building.Owner)?.District == me.District && ToFlowNode(building) is { Kind: FlowNodeKind.Building } node)
            {
                foreach (var (resource, amount) in node.In)
                {
                    Add(needs, building.Owner, resource, amount);
                }
                AddShortfall(building.Owner, node.In.ToDictionary());
            }
        }
        return new Neighbourhood(surplus, needs, shortfalls, earn, pay, connected, links);
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

/// <summary>What the companies of a district could trade, as one company sees it (<see cref="GameWorld.NeighbourhoodOf"/>)</summary>
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
