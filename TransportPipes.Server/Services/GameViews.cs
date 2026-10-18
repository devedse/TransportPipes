using System.Text.Json;
using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;
using TransportPipes.Server.Dtos;

namespace TransportPipes.Server.Services;

/// <summary>Turns the game world into what clients see. Only call these while the world is locked.</summary>
public static class GameViews
{
    public static string Camel<T>(T value) where T : struct, Enum => JsonNamingPolicy.CamelCase.ConvertName(value.ToString());

    public static CompanyDto Company(GameWorld world, Company company, double goldPerMinute, double researchPerMinute)
    {
        var estate = world.EstateOf(company)!;
        var (parcelCost, parcelLevel) = world.NextParcel(company);
        var progression = world.Content.Progression;
        return new CompanyDto(
            company.Id,
            company.Name,
            company.Color,
            company.Gold,
            goldPerMinute,
            company.Xp,
            company.Level,
            progression.XpForLevel(company.Level),
            company.Level >= progression.MaxCompanyLevel ? progression.XpForLevel(company.Level) : progression.XpForLevel(company.Level + 1),
            company.ResearchPoints,
            researchPerMinute,
            company.LifetimeEarnings,
            company.District.X,
            company.District.Y,
            company.Slot,
            estate.Origin.X,
            estate.Origin.Y,
            estate.UnlockedParcels,
            parcelLevel == int.MaxValue ? null : parcelCost,
            parcelLevel == int.MaxValue ? null : parcelLevel,
            company.SellPrices,
            company.BuyMaxPrices,
            world.BuildingsOf(company.Id).GroupBy(t => t.Type).ToDictionary(t => t.Key, t => t.Count()),
            company.Researched.OrderBy(t => t).ToList(),
            company.CurrentResearch,
            company.ResearchEndsAt,
            company.HubReputation,
            world.ReputationLevel(company),
            world.ReputationLevel(company) < GameWorld.ReputationLevels.Length ? GameWorld.ReputationLevels[world.ReputationLevel(company)] : null,
            company.DailyOrders.Select(t => new DailyOrderDto(t.Id, t.Resource, t.Amount, t.Delivered, t.RewardGold, t.RewardXp, t.RewardReputation, t.ExpiresAt, t.Urgent)).ToList(),
            company.NoTrade.OrderBy(t => t).ToList(),
            company.CorporationId,
            world.CorporationOf(company)?.Tag,
            company.CorporationId != null ? Camel(company.Role) : null,
            company.MentorId,
            company.Likes,
            world.Calendar(company).Select(t => Event(world, t)).ToList(),
            (int)world.Horizon(company).TotalHours,
            world.Era,
            world.EraDeadline,
            company.Titles.OrderBy(t => t).ToList(),
            company.Achievements.Where(t => t.StartsWith("collection:")).Select(t => t["collection:".Length..]).ToList(),
            world.OutpostsOf(company.Id).Select(t => Outpost(world, t, company.Id)).ToList(),
            world.Now);
    }

    public static GameEventDto Event(GameWorld world, GameEvent gameEvent) => new(
        gameEvent.Id,
        Camel(gameEvent.Kind),
        world.EventName(gameEvent),
        world.EventEffect(gameEvent),
        gameEvent.StartsAt,
        gameEvent.EndsAt,
        gameEvent.Resource,
        gameEvent.ActiveAt(world.Now),
        gameEvent.District == null);

    public static MarketDto Market(GameWorld world, Company company)
    {
        var district = company.District;
        var market = world.Market(district);
        var resources = world.Content.ResourceList
            .Where(t => t.Era <= world.Era + 1)
            .Select(t =>
            {
                var (low, high) = world.FairBand(company, t.Id);
                var pending = company.PendingPrices.TryGetValue(t.Id, out var p) ? p : null;
                return new MarketResourceDto(
                    t.Id,
                    t.Price,
                    world.HubBuyPrice(district, t.Id),
                    world.HubSellPrice(district, t.Id),
                    market.SoldRate.TryGetValue(t.Id, out var sold) ? sold : 0,
                    world.HubDemand(district, t.Id),
                    world.SellPrice(company, t.Id),
                    !company.SellPrices.ContainsKey(t.Id),
                    pending?.Price,
                    pending?.At,
                    world.BuyMaxPrice(company, t.Id),
                    company.NoTrade.Contains(t.Id),
                    low,
                    high);
            })
            .ToList();
        return new MarketDto(district.X, district.Y, WorldNames.District(district), resources, GameWorld.NextMarketUpdate(world.Now));
    }

    public static List<ResearchStateDto> Research(GameWorld world, Company company) =>
        world.Content.ResearchList.Select(tech =>
        {
            var state = company.Researched.Contains(tech.Id) ? "done"
                : company.CurrentResearch == tech.Id ? "researching"
                : world.ResearchBlocker(company, tech) is null ? "available"
                : "locked";
            return new ResearchStateDto(tech.Id, state, world.ResearchCost(company, tech), state == "locked" ? world.ResearchBlocker(company, tech) : null, world.ResearchDiffusion(company, tech.Id));
        }).ToList();

    public static ReportDto Report(GameWorld world, Company company)
    {
        var report = company.Report;
        var full = world.FullStorages(company.Id)
            .Select(t =>
            {
                var definition = world.Content.Building(t.Type);
                var upgradable = t.Level < world.Content.Progression.MaxBuildingLevel;
                return new FullStorageDto(t.Id, t.Type, t.Level, world.StorageCapacity(t, definition), upgradable ? world.UpgradeCost(t, 1) : null);
            })
            .ToList();
        return new ReportDto(
            report.Since,
            report.GoldEarned,
            report.GoldSpent,
            report.Sold,
            report.Produced,
            report.LevelsGained,
            report.BuildingsCompleted,
            report.Notes,
            report.ResearchDone,
            report.Contracts,
            report.NewNeighbours,
            report.OrdersCompleted,
            report.LikesReceived,
            full);
    }

    /// <summary>Whether a report has anything to tell</summary>
    public static bool HasNews(CompanyReport report) =>
        report.GoldEarned > 0 || report.BuildingsCompleted > 0 || report.LevelsGained > 0 || report.ResearchDone.Count > 0
        || report.Contracts.Count > 0 || report.NewNeighbours.Count > 0 || report.OrdersCompleted > 0 || report.LikesReceived > 0;

    public static BuildingDto Building(GameWorld world, Building building) => new(
        building.Id,
        building.Type,
        building.Owner,
        building.X,
        building.Y,
        building.Size,
        building.Level,
        building.ConstructionEndsAt,
        world.NetworkOf(building.Id)?.Id,
        building.Damaged);

    public static ChunkObjectsDto ChunkObjects(GameWorld world, int cx, int cy)
    {
        var buildings = new List<BuildingDto>();
        var conductors = new List<ConductorDto>();
        var overrides = new List<TerrainOverrideDto>();
        var x0 = cx * WorldLayout.ChunkSize;
        var y0 = cy * WorldLayout.ChunkSize;
        for (var y = y0; y < y0 + WorldLayout.ChunkSize; y++)
        {
            for (var x = x0; x < x0 + WorldLayout.ChunkSize; x++)
            {
                var pos = new TilePos(x, y);
                if (world.BuildingAt(pos) is { } building && building.X == x && building.Y == y)
                {
                    buildings.Add(Building(world, building));
                }
                if (world.ConductorAt(pos) is { } conductor)
                {
                    conductors.Add(new ConductorDto(x, y, Camel(conductor.Kind), conductor.Tier, conductor.Owner,
                        conductor.Side is { } side ? Camel(side) : null, world.NetworkAt(pos)?.Id));
                }
                if (world.TerrainOverrides.TryGetValue(pos, out var terrain))
                {
                    overrides.Add(new TerrainOverrideDto(x, y, Camel(terrain)));
                }
            }
        }
        var outposts = world.Outposts
            .Where(t => t.X < x0 + WorldLayout.ChunkSize && t.X + GameWorld.OutpostSize > x0 && t.Y < y0 + WorldLayout.ChunkSize && t.Y + GameWorld.OutpostSize > y0)
            .Select(t => Outpost(world, t, null))
            .ToList();
        var deposits = new List<DepositOverrideDto>();
        foreach (var discovery in world.Discoveries.Where(t => Math.Abs(t.X - (x0 + WorldLayout.ChunkSize / 2)) < WorldLayout.ChunkSize && Math.Abs(t.Y - (y0 + WorldLayout.ChunkSize / 2)) < WorldLayout.ChunkSize))
        {
            for (var y = discovery.Y - 3; y <= discovery.Y + 3; y++)
            {
                for (var x = discovery.X - 3; x <= discovery.X + 3; x++)
                {
                    if (x >= x0 && x < x0 + WorldLayout.ChunkSize && y >= y0 && y < y0 + WorldLayout.ChunkSize && world.DepositOverride(new TilePos(x, y)) is { } deposit)
                    {
                        deposits.Add(new DepositOverrideDto(x, y, deposit.Deposit, deposit.Richness));
                    }
                }
            }
        }
        return new ChunkObjectsDto(cx, cy, buildings, conductors, overrides, outposts, deposits);
    }

    public static OutpostDto Outpost(GameWorld world, Outpost outpost, long? viewer)
    {
        var owner = world.Company(outpost.Owner);
        var mine = viewer == outpost.Owner;
        return new OutpostDto(outpost.Id, outpost.Owner, owner?.Name ?? "", owner?.Color ?? "#888888", outpost.X, outpost.Y, GameWorld.OutpostSize, mine ? outpost.Lease : null, mine ? outpost.PaidUntil : null);
    }

    /// <summary>The best claim covering a tile</summary>
    public static OutpostCheckDto OutpostCheck(GameWorld world, Company company, TilePos tile)
    {
        var (origin, error) = world.BestOutpostAt(company, tile);
        var tiles = Enumerable.Range(0, GameWorld.OutpostSize * GameWorld.OutpostSize)
            .Select(i => world.Tile(origin.Offset(i % GameWorld.OutpostSize, i / GameWorld.OutpostSize)))
            .Where(t => t.Deposit != null && t.Richness >= 3)
            .ToList();
        var deposit = tiles.GroupBy(t => t.Deposit).OrderByDescending(t => t.Count()).Select(t => t.Key).FirstOrDefault();
        return new OutpostCheckDto(error, origin.X, origin.Y, world.OutpostLease(), GameWorld.OutpostSize, deposit, tiles.Count);
    }

    public static ChunkDto Chunk(GameWorld world, int cx, int cy)
    {
        const int size = WorldLayout.ChunkSize;
        var terrain = new byte[size * size];
        var deposits = new byte[size * size];
        var richness = new byte[size * size];
        var wind = new byte[size * size];
        var sun = new byte[size * size];
        var depositNames = new List<string>();
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var info = world.Generator.Tile(new TilePos(cx * size + x, cy * size + y));
                var i = y * size + x;
                terrain[i] = (byte)info.Terrain;
                if (info.Deposit is { } deposit)
                {
                    var index = depositNames.IndexOf(deposit);
                    if (index < 0)
                    {
                        depositNames.Add(deposit);
                        index = depositNames.Count - 1;
                    }
                    deposits[i] = (byte)(index + 1);
                    richness[i] = (byte)info.Richness;
                }
                wind[i] = (byte)Math.Clamp(Math.Round(info.Wind * 100), 0, 255);
                sun[i] = (byte)Math.Clamp(Math.Round(info.Sun * 100), 0, 255);
            }
        }
        return new ChunkDto(cx, cy, Convert.ToBase64String(terrain), Convert.ToBase64String(deposits), Convert.ToBase64String(richness),
            Convert.ToBase64String(wind), Convert.ToBase64String(sun), depositNames, ChunkObjects(world, cx, cy));
    }

    public static DistrictDto District(GameWorld world, DistrictCoord district)
    {
        var estates = new List<EstateDto>();
        for (var slot = 0; slot < WorldLayout.EstatesPerDistrict; slot++)
        {
            var origin = WorldLayout.EstateOrigin(district, slot);
            var estate = world.Estate(district, slot);
            var owner = estate?.OwnerId is { } ownerId ? world.Company(ownerId) : null;
            var profile = world.Generator.Estate(district, slot);
            estates.Add(new EstateDto(
                slot,
                origin.X,
                origin.Y,
                owner?.Id,
                owner?.Name,
                owner?.Color,
                owner?.Level ?? 0,
                estate?.UnlockedParcels ?? 0,
                (estate?.GatePolicies ?? new Dictionary<GateSide, GatePolicy>()).ToDictionary(t => Camel(t.Key), t => Camel(t.Value)),
                Camel(profile.Primary),
                profile.Secondary is { } secondary ? Camel(secondary) : null,
                owner != null ? world.CorporationOf(owner)?.Tag : null));
        }
        var hub = WorldLayout.HubOrigin(district);
        return new DistrictDto(
            district.X,
            district.Y,
            WorldLayout.DistrictOriginX(district.X),
            WorldLayout.DistrictOriginY(district.Y),
            WorldNames.District(district),
            estates,
            HubStructures(world, district));
    }

    /// <summary>The 4 lots of the corporation quarter, by HQ level</summary>
    private static readonly (int X, int Y)[] HqLots = [(0, 0), (4, 0), (0, 4), (4, 4)];

    private static List<HubStructureDto> HubStructures(GameWorld world, DistrictCoord district)
    {
        var structures = new List<HubStructureDto>();
        foreach (var structure in world.Generator.HubStructures(district))
        {
            var wonder = structure.Type switch
            {
                "wonderSite" => world.DistrictWonder(district),
                "eraWonderSite" => world.EraWonder,
                _ => null,
            };
            if (wonder != null)
            {
                var definition = world.WonderDefinition(wonder);
                structures.Add(new HubStructureDto(structure.Type, structure.X, structure.Y, structure.Size, definition.Name, Level: wonder.Stage, Variant: wonder.Type, Stages: definition.Stages.Count));
                continue;
            }
            structures.Add(new HubStructureDto(structure.Type, structure.X, structure.Y, structure.Size));
            if (structure.Type != "corporationQuarter")
            {
                continue;
            }
            var corporations = world.Corporations
                .Where(t => t.District == district)
                .OrderByDescending(t => t.HqLevel).ThenByDescending(t => world.Members(t).Count()).ThenBy(t => t.Id)
                .Take(HqLots.Length)
                .ToList();
            for (var i = 0; i < corporations.Count; i++)
            {
                var corporation = corporations[i];
                structures.Add(new HubStructureDto("corporationHq", structure.X + HqLots[i].X, structure.Y + HqLots[i].Y, 3, corporation.Tag, corporation.Color, corporation.HqLevel, corporation.Id));
            }
        }
        return structures;
    }

    // ---------------------------------------------------------------- Society

    private static readonly string[] PerkTexts =
    [
        "Corporation chat; members trade freely (no maximum price)",
        "+5% production of the chosen resource",
        "No tolls between members",
        "+1 manager slot for every member",
        "Research 20% cheaper for members",
        "Production perk +6%",
        "Production perk +7%",
        "Production perk +8%",
        "Production perk +9%",
        "Production perk +10%",
    ];

    private static double WeeklyEarnings(Company company) => company.LifetimeEarnings - company.WeekStartEarnings;

    private static CorporationMemberDto Member(GameWorld world, Company company, CorporationGoal? goal, IReadOnlySet<long> online) => new(
        company.Id,
        company.Name,
        company.Color,
        company.Level,
        Camel(company.Role),
        WeeklyEarnings(company),
        goal?.Contributions.GetValueOrDefault(company.Id) ?? 0,
        online.Contains(company.UserId));

    public static CorporationsDto Corporations(GameWorld world, Company company, IReadOnlySet<long> online)
    {
        var mine = world.CorporationOf(company) is { } corporation ? Corporation(world, corporation, company, online) : null;
        var district = world.Corporations
            .Where(t => t.District == company.District)
            .Select(t => new CorporationSummaryDto(
                t.Id,
                t.Name,
                t.Tag,
                t.Color,
                t.Description,
                world.Members(t).Count(),
                t.HqLevel,
                world.Members(t).Sum(WeeklyEarnings),
                t.Applications.Contains(company.Id),
                t.Invites.Contains(company.Id)))
            .OrderByDescending(t => t.Invited).ThenByDescending(t => t.HqLevel).ThenByDescending(t => t.Members)
            .ToList();
        DateTime? canJoinAt = company.LeftCorporationAt is { } left && left + GameWorld.RejoinWait > world.Now ? left + GameWorld.RejoinWait : null;
        return new CorporationsDto(mine, district, GameWorld.CorporationFoundLevel, GameWorld.CorporationFoundCost, GameWorld.MaxCorporationMembers, canJoinAt);
    }

    private static CorporationDto Corporation(GameWorld world, Corporation corporation, Company viewer, IReadOnlySet<long> online)
    {
        var goal = corporation.Goal;
        var members = world.Members(corporation)
            .OrderByDescending(t => t.Role).ThenByDescending(t => WeeklyEarnings(t))
            .Select(t => Member(world, t, goal, online))
            .ToList();
        var officer = viewer.Role is CorporationRole.Leader or CorporationRole.Officer;
        var applications = officer
            ? corporation.Applications.Select(world.Company).OfType<Company>().Select(t => Member(world, t, null, online)).ToList()
            : [];
        CorporationGoalDto? goalDto = goal == null
            ? null
            : new CorporationGoalDto(
                goal.Resource,
                goal.Amount,
                goal.Delivered,
                goal.RewardGold,
                goal.EndsAt,
                goal.Contributions.GetValueOrDefault(viewer.Id),
                world.Materials(viewer.Id).GetValueOrDefault(goal.Resource),
                goal.Rewarded);
        return new CorporationDto(
            corporation.Id,
            corporation.Name,
            corporation.Tag,
            corporation.Color,
            corporation.Description,
            corporation.Treasury,
            corporation.Tax,
            corporation.HqLevel,
            corporation.HqLevel < GameWorld.MaxHqLevel ? GameWorld.HqUpgradeCost(corporation.HqLevel) : null,
            corporation.PerkResource,
            Camel(viewer.Role),
            members,
            applications,
            goalDto,
            PerkTexts.Select((text, i) => new CorporationPerkDto(i + 1, text, corporation.HqLevel >= i + 1)).ToList());
    }

    public static CompanyDetailsDto CompanyDetails(GameWorld world, Company company)
    {
        var partners = world.PartnerCounts();
        var levels = world.BuildingsOf(company.Id).Where(t => !t.UnderConstruction).GroupBy(t => t.Type).ToDictionary(t => t.Key, t => t.Max(b => b.Level));
        return new CompanyDetailsDto(
            world.ManagerSlots(company),
            world.Content.Managers.Select(t => new ManagerDto(t.Id, t.Name, t.Cost, t.Color, t.Automation, t.Bonus, company.Managers.Contains(t.Id))).ToList(),
            world.Content.Achievements
                .Select(t => new AchievementDto(t.Id, t.Name, t.Description, t.Amount, world.StatValue(company, t.Stat, partners), company.Achievements.Contains(t.Id), t.Xp, t.Gold, t.Title))
                .ToList(),
            world.Content.Collections
                .Select(t => new CollectionDto(
                    t.Id,
                    t.Name,
                    t.Description,
                    t.Level,
                    t.Buildings,
                    t.Buildings.Where(b => levels.GetValueOrDefault(b) >= t.Level).ToList(),
                    company.Achievements.Contains(GameWorld.CollectionKey(t.Id)),
                    world.Content.BuildingList.FirstOrDefault(b => b.Unlock.Collection == t.Id)?.Id))
                .ToList(),
            company.Titles.OrderBy(t => t).ToList(),
            [
                new StatDto("Gold earned", company.LifetimeEarnings),
                new StatDto("Net worth", world.NetWorth(company)),
                new StatDto("Buildings built", world.StatValue(company, "buildings")),
                new StatDto("Pipe tiles laid", world.StatValue(company, "pipes")),
                new StatDto("Levels bought", world.StatValue(company, "levelsBought")),
                new StatDto("Trading partners", partners.GetValueOrDefault(company.Id)),
                new StatDto("Contracts finished", company.ContractsCompleted),
                new StatDto("Daily orders", world.StatValue(company, "orders")),
                new StatDto("Techs researched", company.Researched.Count),
                new StatDto("Delivered to Wonders (g)", world.StatValue(company, "wonder")),
                new StatDto("Likes", company.Likes),
            ],
            company.Stats.Where(t => t.Key.StartsWith("produced:")).OrderByDescending(t => t.Value).Take(8)
                .Select(t => new StatDto(t.Key["produced:".Length..], t.Value)).ToList());
    }

    public static WondersDto Wonders(GameWorld world, Company company)
    {
        var district = world.DistrictWonder(company.District);
        return new WondersDto(
            world.Era,
            world.EraStartedAt,
            world.EraDeadline,
            district == null ? null : Wonder(world, district, company),
            world.EraWonder == null ? null : Wonder(world, world.EraWonder, company),
            WorldNames.District(company.District));
    }

    private static WonderDto Wonder(GameWorld world, Wonder wonder, Company company)
    {
        var definition = world.WonderDefinition(wonder);
        var materials = world.Materials(company.Id);
        var total = wonder.Contributions.Values.Sum();
        var (share, tier) = wonder.District == null ? world.EraWonderTier(company) : (total > 0 ? wonder.Contributions.GetValueOrDefault(company.Id) / total : 0, WonderTier.None);
        return new WonderDto(
            wonder.Type,
            definition.Name,
            definition.Description,
            wonder.District == null,
            wonder.Stage,
            definition.Stages.Count,
            wonder.Finished,
            wonder.Required.Select(t => new WonderNeedDto(t.Key, t.Value, wonder.Delivered.GetValueOrDefault(t.Key), materials.GetValueOrDefault(t.Key))).ToList(),
            total,
            wonder.Contributions.GetValueOrDefault(company.Id),
            Camel(tier),
            share,
            wonder.Contributions.OrderByDescending(t => t.Value).Take(5)
                .Select(t => world.Company(t.Key) is { } c ? new WonderBuilderDto(c.Id, c.Name, c.Color, t.Value) : null)
                .OfType<WonderBuilderDto>()
                .ToList());
    }

    public static LeaderboardDto Leaderboard(GameWorld world, Company viewer, LeaderboardKind kind, bool district)
    {
        var all = world.Leaderboard(kind, district ? viewer.District : null, int.MaxValue);
        LeaderboardEntryDto View(LeaderboardEntry entry) => new(entry.Company, entry.Name, entry.Color, entry.Value, entry.Rank, entry.Company == viewer.Id);
        var you = all.FirstOrDefault(t => t.Company == viewer.Id);
        return new LeaderboardDto(Camel(kind), district, all.Take(50).Select(View).ToList(), you != null ? View(you) : null);
    }

    public static EstateVisitDto Visit(GameWorld world, Company owner, Company viewer, IReadOnlyList<GuestbookNoteDto> guestbook)
    {
        var estate = world.EstateOf(owner)!;
        var corporation = world.CorporationOf(owner);
        var canMentor = !owner.IsNpc && owner.Id != viewer.Id && viewer.MentorId == null && viewer.Level < GameWorld.MenteeMaxLevel
            && owner.Level >= GameWorld.MentorLevel && world.Mentees(owner.Id) < GameWorld.MaxMentees;
        return new EstateVisitDto(
            owner.Id,
            owner.Name,
            owner.Color,
            owner.Level,
            owner.IsNpc,
            owner.Id == viewer.Id,
            owner.Likes,
            world.LikedToday(viewer.Id, owner.Id),
            corporation?.Tag,
            corporation?.Name,
            world.NetWorth(owner),
            world.BuildingsOf(owner.Id).Count(),
            world.PartnerCounts().GetValueOrDefault(owner.Id),
            owner.CreatedAt,
            estate.Origin.X,
            estate.Origin.Y,
            canMentor,
            viewer.MentorId == owner.Id,
            world.Mentees(owner.Id),
            guestbook);
    }

    public static NetworkFlowsDto Flows(GameWorld world, Network network)
    {
        var flows = new List<FlowDto>();
        foreach (var flow in network.Solution.Flows)
        {
            var path = new List<int>();
            var link = network.Links
                .Where(t => t.Connects(flow.From, flow.To) && t.Allows(flow.Resource))
                .OrderByDescending(t => t.Priority).ThenByDescending(t => t.Efficiency)
                .FirstOrDefault();
            if (link != null)
            {
                var tiles = link.A == flow.From ? link.Path : link.Path.Reverse();
                foreach (var tile in tiles)
                {
                    path.Add(tile.X);
                    path.Add(tile.Y);
                }
            }
            flows.Add(new FlowDto(flow.From, flow.To, flow.Resource, flow.Sent, flow.Delivered, Camel(flow.Kind), flow.PricePerUnit, path));
        }
        return new NetworkFlowsDto(network.Id, network.Solution.Rates, flows);
    }

    public static BuildingDetailsDto BuildingDetails(GameWorld world, Building building)
    {
        var definition = world.Content.Building(building.Type);
        var owner = world.Company(building.Owner);
        var network = world.NetworkOf(building.Id);
        var solution = network?.Solution ?? FlowSolution.Empty;
        var node = world.ToFlowNode(building);
        var rate = solution.Rate(building.Id);

        var received = new Dictionary<string, double>();
        var sent = new Dictionary<string, double>();
        var flows = new List<BuildingFlowDto>();
        foreach (var flow in solution.Flows)
        {
            if (flow.To == building.Id)
            {
                received[flow.Resource] = (received.TryGetValue(flow.Resource, out var v) ? v : 0) + flow.Delivered;
                flows.Add(Partner("in", flow, flow.From, flow.Delivered));
            }
            if (flow.From == building.Id)
            {
                sent[flow.Resource] = (sent.TryGetValue(flow.Resource, out var v) ? v : 0) + flow.Sent;
                flows.Add(Partner("out", flow, flow.To, flow.Sent));
            }
        }

        BuildingFlowDto Partner(string direction, Flow flow, long partnerId, double amount)
        {
            var partner = world.Building(partnerId);
            var partnerOwner = partner != null ? world.Company(partner.Owner)?.Name ?? "" : "";
            return new BuildingFlowDto(direction, flow.Resource, amount, partnerId, partner?.Type ?? "", partnerOwner, Camel(flow.Kind), flow.PricePerUnit);
        }

        var inputs = node?.In.Select(t => new ResourceAmountDto(t.Key, received.TryGetValue(t.Key, out var v) ? v : 0, t.Value)).ToList() ?? [];
        var outputs = node?.Out.Select(t => new ResourceAmountDto(t.Key, sent.TryGetValue(t.Key, out var v) ? v : 0, t.Value)).ToList() ?? [];

        var upgradable = definition.Kind is not (BuildingKind.Hq or BuildingKind.Gate or BuildingKind.Decoration)
            && building.Level < world.Content.Progression.MaxBuildingLevel;
        var affordable = upgradable && owner != null ? world.AffordableLevels(owner, building) : 0;
        var maxLevels = world.Content.Progression.MaxBuildingLevel - building.Level;

        return new BuildingDetailsDto(
            Building(world, building),
            owner?.Name ?? "",
            rate,
            world.LocationMultiplier(building, definition),
            inputs,
            outputs,
            definition.ResearchPerMinute * world.Content.Progression.OutputMultiplier(building.Level) * rate,
            definition.Kind == BuildingKind.Storage ? building.Contents : null,
            definition.Kind == BuildingKind.Storage ? world.StorageCapacity(building, definition) : null,
            definition.Kind == BuildingKind.TradingPost ? world.TradingPostThroughput(building, definition) : null,
            definition.Kind == BuildingKind.TradingPost ? building.Imports.OrderBy(t => t).ToList() : null,
            definition.Kind == BuildingKind.Valve ? building.Filter.OrderBy(t => t).ToList() : null,
            network?.PumpFactor ?? 1,
            upgradable ? world.UpgradeCost(building, 1) : null,
            upgradable ? world.UpgradeCost(building, Math.Min(10, maxLevels)) : null,
            affordable,
            affordable > 0 ? world.UpgradeCost(building, affordable) : null,
            flows,
            definition.Kind is BuildingKind.Hq or BuildingKind.Gate ? 0 : Math.Round(definition.Cost.Gold * 0.5),
            Issues(world, building, node, network),
            building.Setting,
            world.TollIncome(building.Id),
            building.Damaged ? world.RepairCost(building) : null);
    }

    /// <summary>Why a building can't run (fully): missing suppliers or buyers on its pipes</summary>
    private static List<string> Issues(GameWorld world, Building building, FlowNode? node, Network? network)
    {
        var issues = new List<string>();
        if (building.Damaged)
        {
            issues.Add("Storm damage: it works at half speed until it is repaired.");
        }
        if (node == null || node.Kind != FlowNodeKind.Building || building.UnderConstruction)
        {
            return issues;
        }
        if (network == null)
        {
            issues.Add("Not connected: lay a pipe from it to the buildings it works with.");
            return issues;
        }
        var linked = network.Links
            .Where(t => t.A == building.Id || t.B == building.Id)
            .Select(t => world.Building(t.A == building.Id ? t.B : t.A))
            .OfType<Building>()
            .Where(t => !t.UnderConstruction)
            .ToList();
        string Name(string resource) => world.Content.Resources.TryGetValue(resource, out var r) ? r.Name.ToLowerInvariant() : resource;

        foreach (var resource in node.In.Keys)
        {
            var supplied = linked.Any(other =>
            {
                var definition = world.Content.Building(other.Type);
                var mine = other.Owner == building.Owner;
                return world.ToFlowNode(other) is { } n && n.Out.ContainsKey(resource)
                    || (mine && definition.Kind == BuildingKind.Storage && other.Contents.ContainsKey(resource))
                    || (mine && definition.Kind == BuildingKind.TradingPost && other.Imports.Contains(resource));
            });
            if (!supplied)
            {
                issues.Add($"No {Name(resource)} on its pipes: connect a building that makes it, or let your trading post buy it.");
            }
        }
        if (node.Out.Count > 0)
        {
            var hasSink = linked.Any(other =>
            {
                var definition = world.Content.Building(other.Type);
                return other.Owner == building.Owner && definition.Kind is BuildingKind.Storage or BuildingKind.TradingPost;
            });
            foreach (var resource in node.Out.Keys.Where(_ => !hasSink))
            {
                if (!linked.Any(other => world.ToFlowNode(other) is { } n && n.In.ContainsKey(resource)))
                {
                    issues.Add($"Nothing on its pipes takes its {Name(resource)}: connect your trading post, a storage or a building that uses it.");
                }
            }
        }
        return issues;
    }

    public static NetworkDetailsDto NetworkDetails(GameWorld world, Network network)
    {
        var solution = network.Solution;
        var isMachine = new Dictionary<long, bool>();
        bool Machine(long id) => isMachine.TryGetValue(id, out var v) ? v
            : isMachine[id] = world.Building(id) is { } b && world.Content.Building(b.Type).Kind is not (BuildingKind.Storage or BuildingKind.TradingPost);

        var resources = new Dictionary<string, double[]>();
        double[] Row(string resource) => resources.TryGetValue(resource, out var row) ? row : resources[resource] = new double[8];
        foreach (var flow in solution.Flows)
        {
            var row = Row(flow.Resource);
            if (Machine(flow.From))
            {
                row[0] += flow.Sent;
            }
            if (Machine(flow.To) && flow.Kind is FlowKind.Internal or FlowKind.Trade or FlowKind.Contract or FlowKind.FromStorage or FlowKind.Import)
            {
                row[1] += flow.Delivered;
            }
            switch (flow.Kind)
            {
                case FlowKind.Sale: row[2] += flow.Delivered; break;
                case FlowKind.Import: row[3] += flow.Sent; break;
                case FlowKind.Trade or FlowKind.Contract: row[4] += flow.Delivered; break;
                case FlowKind.ToStorage: row[5] += flow.Delivered; break;
                case FlowKind.FromStorage: row[6] += flow.Sent; break;
            }
            row[7] += flow.Sent - flow.Delivered;
        }

        var used = new Dictionary<FlowLink, double>(ReferenceEqualityComparer.Instance);
        foreach (var flow in solution.Flows)
        {
            var link = network.Links
                .Where(t => t.Connects(flow.From, flow.To) && t.Allows(flow.Resource))
                .OrderByDescending(t => t.Priority).ThenByDescending(t => t.Efficiency)
                .FirstOrDefault();
            if (link != null)
            {
                used[link] = (used.TryGetValue(link, out var v) ? v : 0) + flow.Sent;
            }
        }
        var bottlenecks = used
            .Where(t => t.Key.Capacity < 1e12 && t.Value >= t.Key.Capacity * 0.999)
            .Select(t => new BottleneckDto(t.Key.A, t.Key.B, t.Key.Capacity, t.Value, t.Key.Path.SelectMany(p => new[] { p.X, p.Y }).ToList()))
            .ToList();

        var owners = network.Owners(world)
            .Select(world.Company)
            .OfType<Company>()
            .Select(t => new NetworkOwnerDto(t.Id, t.Name, t.Color, solution.GoldPerMinute.TryGetValue(t.Id, out var g) ? g : 0))
            .ToList();
        var pumps = network.Buildings.Count(t => world.Building(t) is { } b && world.Content.Building(b.Type).Kind == BuildingKind.Pump);

        return new NetworkDetailsDto(
            network.Id,
            network.Buildings.Count(t => world.Building(t) is { } b && !world.Content.Building(b.Type).IsConductor),
            network.Conductors.Count,
            pumps,
            network.PumpFactor,
            owners,
            resources.OrderBy(t => t.Key).Select(t => new NetworkResourceDto(t.Key, t.Value[0], t.Value[1], t.Value[2], t.Value[3], t.Value[4], t.Value[5], t.Value[6], t.Value[7])).ToList(),
            bottlenecks);
    }

    /// <summary>The companies of a district as one of them sees them: surplus, needs, trade with you</summary>
    public static List<NeighbourDto> Neighbourhood(GameWorld world, Company me)
    {
        var surplus = new Dictionary<long, Dictionary<string, double>>();
        var needs = new Dictionary<long, Dictionary<string, double>>();
        var earn = new Dictionary<long, double>();
        var pay = new Dictionary<long, double>();
        var connected = new HashSet<long>();
        void Add(Dictionary<long, Dictionary<string, double>> map, long company, string resource, double amount)
        {
            if (!map.TryGetValue(company, out var rates))
            {
                map[company] = rates = new Dictionary<string, double>();
            }
            rates[resource] = (rates.TryGetValue(resource, out var v) ? v : 0) + amount;
        }

        foreach (var network in world.Networks.Where(t => t.District == me.District))
        {
            var owners = network.Owners(world).ToHashSet();
            if (owners.Contains(me.Id))
            {
                connected.UnionWith(owners);
            }
            var received = new Dictionary<(long, string), double>();
            foreach (var flow in network.Solution.Flows)
            {
                var from = world.Building(flow.From)!.Owner;
                var to = world.Building(flow.To)!.Owner;
                received[(flow.To, flow.Resource)] = (received.TryGetValue((flow.To, flow.Resource), out var r) ? r : 0) + flow.Delivered;
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
                if (world.Building(id) is not { } building || world.ToFlowNode(building) is not { Kind: FlowNodeKind.Building } node)
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
                    var other = world.Building(link.A == id ? link.B : link.A);
                    if (other == null)
                    {
                        return false;
                    }
                    var kind = world.Content.Building(other.Type).Kind;
                    return (other.Owner == building.Owner && kind is BuildingKind.Storage or BuildingKind.TradingPost)
                        || world.ToFlowNode(other) is { } n && node.Out.Keys.Any(n.In.ContainsKey);
                });
                if (!placeable)
                {
                    continue;
                }
                foreach (var (resource, amount) in node.In)
                {
                    var missing = amount * node.MaxRate - (received.TryGetValue((id, resource), out var got) ? got : 0);
                    if (missing > 0.01)
                    {
                        Add(needs, building.Owner, resource, missing);
                    }
                }
            }
        }
        // Buildings without any pipe need everything
        foreach (var building in world.Buildings.Where(t => world.NetworkOf(t.Id) == null && !t.UnderConstruction))
        {
            if (world.Company(building.Owner)?.District == me.District && world.ToFlowNode(building) is { Kind: FlowNodeKind.Building } node)
            {
                foreach (var (resource, amount) in node.In)
                {
                    Add(needs, building.Owner, resource, amount);
                }
            }
        }

        List<ResourceRateDto> Rates(Dictionary<long, Dictionary<string, double>> map, long company) =>
            map.TryGetValue(company, out var rates)
                ? rates.Where(t => t.Value > 0.01).OrderByDescending(t => t.Value * world.BasePrice(t.Key)).Select(t => new ResourceRateDto(t.Key, t.Value)).ToList()
                : [];

        return world.Companies
            .Where(t => t.District == me.District && t.Id != me.Id)
            .Select(t =>
            {
                var estate = world.EstateOf(t)!;
                return new NeighbourDto(
                    t.Id,
                    t.Name,
                    t.Color,
                    t.Level,
                    GameWorld.ContractStars(t),
                    t.IsNpc,
                    t.Slot,
                    estate.Origin.X,
                    estate.Origin.Y,
                    estate.GatePolicies.ToDictionary(g => Camel(g.Key), g => Camel(g.Value)),
                    connected.Contains(t.Id),
                    Rates(surplus, t.Id),
                    Rates(needs, t.Id),
                    earn.GetValueOrDefault(t.Id),
                    pay.GetValueOrDefault(t.Id));
            })
            .OrderBy(t => Math.Abs(t.EstateX - world.EstateOf(me)!.Origin.X) + Math.Abs(t.EstateY - world.EstateOf(me)!.Origin.Y))
            .ToList();
    }

    public static ContractDto Contract(GameWorld world, Contract contract)
    {
        string Name(long id) => id == 0 ? "" : world.Company(id)?.Name ?? "?";
        var proposer = world.Company(contract.Proposer);
        return new ContractDto(
            contract.Id,
            contract.Proposer,
            Name(contract.Proposer),
            contract.Seller,
            Name(contract.Seller),
            contract.Buyer,
            Name(contract.Buyer),
            contract.Resource,
            contract.Rate,
            contract.Price,
            contract.Days,
            Camel(contract.State),
            contract.CreatedAt,
            contract.EndsAt,
            contract.Delivered,
            contract.Expected,
            proposer == null ? 0 : GameWorld.ContractStars(proposer));
    }

    public static ContractsDto Contracts(GameWorld world, Company me)
    {
        var recent = world.Now.AddDays(-3);
        var mine = world.Contracts
            .Where(t => t.Proposer == me.Id || t.Seller == me.Id || t.Buyer == me.Id)
            .Where(t => t.IsOpen || (t.EndsAt ?? t.CreatedAt) > recent)
            .OrderBy(t => t.State).ThenByDescending(t => t.Id)
            .Select(t => Contract(world, t))
            .ToList();
        var board = world.Contracts
            .Where(t => t.State == ContractState.Offered && (t.Seller == 0 || t.Buyer == 0) && t.Proposer != me.Id && world.Company(t.Proposer)?.District == me.District)
            .OrderByDescending(t => t.Id)
            .Select(t => Contract(world, t))
            .ToList();
        return new ContractsDto(mine, board);
    }

    public static ExchangeDto Exchange(GameWorld world, Company me)
    {
        var orders = world.ExchangeOrders.Where(t => t.District == me.District).ToList();
        var books = orders
            .GroupBy(t => t.Resource)
            .OrderBy(t => t.Key)
            .Select(g =>
            {
                var bids = g.Where(t => !t.Selling).ToList();
                var asks = g.Where(t => t.Selling).ToList();
                var bestBid = bids.Count == 0 ? 0 : bids.Max(t => t.Price);
                var bestAsk = asks.Count == 0 ? 0 : asks.Min(t => t.Price);
                return new ExchangeBookDto(g.Key, bestBid, bids.Where(t => t.Price == bestBid).Sum(t => t.Amount), bestAsk, asks.Where(t => t.Price == bestAsk).Sum(t => t.Amount));
            })
            .ToList();
        var mine = orders
            .Where(t => t.Company == me.Id)
            .OrderByDescending(t => t.Id)
            .Select(t => new ExchangeOrderDto(t.Id, t.Resource, t.Selling, t.Amount, t.Price, t.CreatedAt, true))
            .ToList();
        var shipments = world.Shipments
            .Where(t => t.To == me.Id || t.From == me.Id)
            .OrderBy(t => t.ArrivesAt)
            .Select(t => new ShipmentDto(t.Id, t.Resource, t.Amount, t.ArrivesAt, t.To == me.Id, world.Company(t.To == me.Id ? t.From : t.To)?.Name ?? ""))
            .ToList();
        return new ExchangeDto(world.HasBuilding(me.Id, BuildingKind.ExchangeOffice), books, mine, shipments, GameWorld.ExchangeFee);
    }

    public static ToastDto Toast(WorldEvent worldEvent) => new(Camel(worldEvent.Kind), worldEvent.Text, worldEvent.Building);

    /// <summary>The content files as the client sees them</summary>
    public static object Content(GameContent content) => new
    {
        content.ResourceList,
        Buildings = content.BuildingList,
        content.PipeTiers,
        Research = content.ResearchList,
        Progression = new
        {
            content.Progression.MaxCompanyLevel,
            XpForLevel = Enumerable.Range(0, content.Progression.MaxCompanyLevel + 2).Select(content.Progression.XpForLevel).ToList(),
            content.Progression.XpRewards,
            content.Progression.MaxBuildingLevel,
            content.Progression.OutputPerLevel,
            content.Progression.CostPerLevel,
            content.Progression.Milestones,
            content.Progression.CostPerCopy,
            content.Progression.StartParcels,
            content.Progression.ParcelLevels,
            ParcelCosts = Enumerable.Range(0, content.Progression.ParcelLevels.Count).Select(content.Progression.ParcelCost).ToList(),
            content.Progression.HubSpread,
        },
        Layout = new
        {
            WorldLayout.EstateSize,
            WorldLayout.CorridorWidth,
            WorldLayout.BlocksPerSide,
            WorldLayout.ParcelSize,
            WorldLayout.ParcelsPerSide,
            WorldLayout.WildernessWidth,
            WorldLayout.ChunkSize,
            WorldLayout.BlockPitch,
            WorldLayout.DistrictSize,
            WorldLayout.DistrictPitch,
            WorldLayout.HubBlock,
            WorldLayout.OriginOffset,
            GateLocal = WorldLayout.GateLocal.ToDictionary(t => Camel(t.Key), t => new { t.Value.X, t.Value.Y }),
            Terrain = Enum.GetValues<TerrainType>().Select(Camel).ToList(),
            RichnessMultiplier = GameWorld.RichnessMultiplier,
        },
    };
}
