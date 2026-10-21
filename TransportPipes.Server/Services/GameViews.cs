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
        // While the estate is packed (moving, or asleep) there is no estate
        var estate = world.EstateOf(company);
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
            estate?.Origin.X ?? 0,
            estate?.Origin.Y ?? 0,
            estate?.UnlockedParcels ?? 0,
            parcelLevel == int.MaxValue ? null : parcelCost,
            parcelLevel == int.MaxValue ? null : parcelLevel,
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
            new FranchiseDto(company.Shares, company.Franchises, world.FranchiseShares(company), world.FranchiseBlocker(company), company.FranchiseAt, company.LegacyLevel),
            SeasonInfo(world),
            new MovingDto(
                Camel(world.ActivityOf(company)),
                company.Asleep,
                company.ArrivesAt,
                company.Crates.Select((t, i) => new CrateDto(i, t.Type, t.Level)).ToList(),
                company.FreeMoves,
                world.MoveCost(company),
                company.LastMovedAt is { } moved && moved + world.MoveEvery > world.Now && company.FreeMoves == 0 ? moved + world.MoveEvery : null,
                company.SwapWith),
            new DeepDto(
                world.HasDeepLevel(company),
                company.DeepParcels,
                company.DigParcel,
                company.DigStartedAt,
                company.DigEndsAt,
                GameWorld.DigCost(GameWorld.DugCount(company)),
                GameWorld.DigTime(GameWorld.DugCount(company)).TotalHours,
                WorldLayout.DeepOffset),
            world.EstatesOf(company).Skip(1).Select(t =>
            {
                var (cost, level) = world.NextParcel(company, t);
                return new BranchDto(t.District.X, t.District.Y, WorldNames.District(t.District), t.Slot, t.Origin.X, t.Origin.Y, t.UnlockedParcels, cost, level);
            }).ToList(),
            world.CurrentTutorialStep(company) is { } step
                ? new TutorialDto(company.TutorialStep, world.Content.Tutorial.Count, step.Id, step.Text, world.TutorialProgress(company, step), step.Amount, step.Gold, step.Hint, world.TutorialWaitLevel(company, step), world.TutorialTip(company, step),
                    step.Chapter, world.TutorialChapter(step).Index, world.TutorialChapter(step).Count)
                : null,
            new ExpeditionDto(
                company.Expedition?.Hours,
                company.Expedition?.EndsAt,
                GameWorld.ExpeditionHours.ToDictionary(t => t, t => GameWorld.ExpeditionCost(company, t)),
                company.Expedition == null ? world.ExpeditionBlocker(company) : null,
                company.ExpeditionLog.Take(5).ToList(),
                company.Relics.OrderBy(t => t).ToList()),
            new MasteryDto(company.Mastery, world.ResearchedEverything(company), GameWorld.MasteryCost(company), GameWorld.MasteryMinutes(company), company.Mastery * GameWorld.MasteryBonus),
            world.Alerts(company).Select(t => new AlertDto(t.Text, t.Building)).ToList(),
            world.Now,
            Experience(world, company, goldPerMinute),
            Camel(company.TradeWith),
            world.CrewOf(company) is { } crew
                ? new CrewBadgeDto(crew.Id, crew.NameHidden ? $"Crew {crew.Id}" : crew.Name, crew.Emblem, crew.Color, crew.Color2, world.CrewMembers(crew).Count())
                : null,
            company.Tailwind,
            company.LoneCard);
    }

    private static ExperienceDto Experience(GameWorld world, Company company, double goldPerMinute)
    {
        var p = world.Content.Progression;
        var goldPerXp = p.XpReward("goldPerXp") * company.Level;
        var rules = new List<XpRuleDto>
        {
            new("earnings", $"1 XP per {goldPerXp:N0} gold earned (it grows with your level)"),
            new("buildings", $"{p.XpReward("building"):N0} XP per building, {p.XpReward("firstBuildingOfType"):N0} more for the first of its kind"),
            new("upgrades", $"{p.XpReward("upgradeLevel"):N0} XP per level, {p.XpReward("milestone"):N0} more at levels 10, 25, 50, 75 and 100"),
            new("parcels", $"{p.XpReward("parcel"):N0} XP per parcel"),
            new("research", $"{p.XpReward("research"):N0} XP per research"),
            new("orders", $"{30 + 8 * company.Level:N0} XP per daily order (it grows with your level)"),
            new("contracts", $"{p.XpReward("contract"):N0} XP per finished contract"),
            new("achievements", "XP for every achievement"),
            new("corporation", "XP when your corporation reaches its weekly goal"),
            new("likes", "10 XP per like of your estate"),
        };
        return new ExperienceDto(
            Math.Max(0, goldPerMinute) / goldPerXp,
            company.XpLog.ToDictionary(t => t.Key, t => (IReadOnlyDictionary<string, double>)t.Value.ToDictionary(i => i.Key, i => Math.Round(i.Value, 1))),
            rules);
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
        var market = world.HubMarketOf(company, district);
        var resources = world.Content.ResourceList
            .Where(t => t.Era <= world.Era + 1)
            .Select(t => new MarketResourceDto(
                t.Id,
                t.Price,
                world.HubBuyPrice(company, district, t.Id),
                world.HubSellPrice(district, t.Id),
                market.SoldRate.TryGetValue(t.Id, out var sold) ? sold : 0,
                world.HubDemand(district, t.Id),
                world.PlayerPrice(t.Id),
                company.NoTrade.Contains(t.Id),
                company.NoBuy.Contains(t.Id),
                world.HubPriceHeading(company, t.Id)))
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
        building.Damaged,
        world.TeleportReceiver(building)?.Id);

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
                    conductors.Add(new ConductorDto(x, y, Camel(conductor.Kind), conductor.Tier, conductor.Owner, world.NetworkAt(pos)?.Id));
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
        foreach (var (pos, seed) in world.Seeds.Where(t => t.Key.X >= x0 && t.Key.X < x0 + WorldLayout.ChunkSize && t.Key.Y >= y0 && t.Key.Y < y0 + WorldLayout.ChunkSize))
        {
            deposits.Add(new DepositOverrideDto(pos.X, pos.Y, seed.Deposit, seed.Richness));
        }
        return new ChunkObjectsDto(cx, cy, buildings, conductors, overrides, outposts, deposits);
    }

    /// <summary>The next world opens this long after the season ended</summary>
    public static TimeSpan SeasonBreak { get; set; } = TimeSpan.FromDays(7);

    public static SeasonInfoDto SeasonInfo(GameWorld world) =>
        new(world.Season, world.FinaleAt, world.SeasonEndedAt, world.SeasonEndedAt is { } ended ? ended + SeasonBreak : null);

    public static SeasonDto Season(GameWorld world, long userId)
    {
        SeasonResultDto View(SeasonResult result) => new(result.Company, result.NetWorth, result.Rank, result.Legacy, result.Title, result.UserId == userId);
        var info = SeasonInfo(world);
        var you = world.SeasonResults.FirstOrDefault(t => t.UserId == userId);
        return new SeasonDto(info.Season, info.FinaleAt, info.EndedAt, info.NextWorldAt, world.SeasonResults.Take(20).Select(View).ToList(), you == null ? null : View(you));
    }

    public static OutpostDto Outpost(GameWorld world, Outpost outpost, long? viewer)
    {
        var owner = world.Company(outpost.Owner);
        var mine = viewer == outpost.Owner;
        return new OutpostDto(outpost.Id, outpost.Owner, owner?.Name ?? "", owner?.Color ?? "#888888", outpost.X, outpost.Y, GameWorld.OutpostSize, mine ? outpost.Lease : null, mine ? outpost.PaidUntil : null);
    }

    /// <summary>The best claim covering a tile</summary>
    public static BlueprintsDto Blueprints(GameWorld world, Company company)
    {
        BlueprintDto View(Blueprint blueprint)
        {
            var owner = world.Company(blueprint.Owner);
            return new BlueprintDto(blueprint.Id, blueprint.Name, blueprint.Owner, owner?.Name ?? "?", owner?.Color ?? "#888888", blueprint.Width, blueprint.Height,
                blueprint.Buildings.Select(t => new BlueprintItemDto(t.Type, t.Dx, t.Dy)).ToList(),
                blueprint.Pipes.Select(t => new BlueprintPipeDto(t.Dx, t.Dy, t.Tier)).ToList(),
                blueprint.Sharing.ToString().ToLowerInvariant(), Math.Round(blueprint.Rating, 2), blueprint.Ratings.Count,
                blueprint.Ratings.TryGetValue(company.Id, out var mine) ? mine : null,
                Math.Round(world.BlueprintCost(company, blueprint)), blueprint.CreatedAt);
        }

        var usable = world.BlueprintsFor(company).ToList();
        return new BlueprintsDto(
            world.HasBuilding(company.Id, BuildingKind.BlueprintOffice),
            false,
            GameWorld.MaxBlueprints,
            usable.Where(t => t.Owner == company.Id).OrderBy(t => t.Name).Select(View).ToList(),
            usable.Where(t => t.Owner != company.Id && t.Sharing == BlueprintSharing.Corporation).OrderBy(t => t.Name).Select(View).ToList(),
            usable.Where(t => t.Owner != company.Id && t.Sharing == BlueprintSharing.Board)
                .OrderByDescending(t => t.Rating).ThenByDescending(t => t.Ratings.Count).Take(40).Select(View).ToList());
    }

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
                Camel(profile.Primary),
                profile.Secondary is { } secondary ? Camel(secondary) : null,
                owner != null ? world.CorporationOf(owner)?.Tag : null,
                owner is { IsNpc: false } ? Camel(world.ActivityOf(owner)) : null,
                estate?.ReservedFor is { } corporationId && estate.ReservedUntil > world.Now ? world.Corporation(corporationId)?.Tag : null));
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

    /// <summary>The two billboards of a hub: the richest companies of the district with the Brand tech</summary>
    private static readonly (int X, int Y)[] Billboards = [(9, 9), (20, 17)];

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
            if (structure.Type == "marketHall")
            {
                var brands = world.Companies
                    .Where(t => t.District == district && !t.IsNpc && world.HasResearch(t, "brand"))
                    .OrderByDescending(world.NetWorth)
                    .Take(Billboards.Length)
                    .ToList();
                for (var i = 0; i < brands.Count; i++)
                {
                    structures.Add(new HubStructureDto("billboard", Billboards[i].X, Billboards[i].Y, 2, brands[i].Name, brands[i].Color));
                }
            }
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
        // The stations' projects (v1.2 §12): in the top corner of each Yard, the current stage rising with its basket
        var hub = WorldLayout.HubOrigin(district);
        foreach (var station in WorldLayout.Stations(district))
        {
            if (world.ExistingProject(station) is not { } project || world.BigStation(station)[0] != station)
            {
                continue;
            }
            var yard = WorldLayout.Yard(station);
            // What stands is the last finished stage; before the Foundation is built, its plan rises with the basket
            var built = project.Finished.Keys.Where(t => t != ProjectStage.Done).DefaultIfEmpty(ProjectStage.Done).Max();
            var stage = built == ProjectStage.Done ? ProjectStage.Foundation : built;
            var target = project.Basket.Sum(t => t.Value * world.BasePrice(t.Key));
            var done = built != ProjectStage.Done || target <= 0 ? 100
                : (int)Math.Floor(100 * project.Basket.Sum(t => Math.Min(t.Value, project.Delivered.GetValueOrDefault(t.Key)) * world.BasePrice(t.Key)) / target);
            structures.Add(new HubStructureDto($"project-{Camel(stage)}", yard.X0 - hub.X, yard.Y0 - hub.Y, 4,
                $"{GameWorld.StageName(stage)} · {WorldNames.Station(station)}", Level: Math.Min(100, done), Stages: 100));
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
                new StatDto("Gold per tile per hour", world.TileEfficiency(company)),
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
        var estate = world.EstateOf(owner);
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
            estate?.Origin.X ?? 0,
            estate?.Origin.Y ?? 0,
            canMentor,
            viewer.MentorId == owner.Id,
            world.Mentees(owner.Id),
            guestbook,
            owner.SwapWith == viewer.Id,
            viewer.SwapWith == owner.Id);
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
            definition.Kind is BuildingKind.Valve or BuildingKind.Showroom ? building.Filter.OrderBy(t => t).ToList() : null,
            network?.PumpFactorOf(building.Owner) ?? 1,
            upgradable ? world.UpgradeCost(building, 1) : null,
            upgradable ? world.UpgradeCost(building, Math.Min(10, maxLevels)) : null,
            affordable,
            affordable > 0 ? world.UpgradeCost(building, affordable) : null,
            flows,
            definition.Kind is BuildingKind.Hq or BuildingKind.Gate ? 0 : world.DemolishRefund(building),
            Issues(world, building, node, network),
            building.Setting,
            building.Damaged ? world.RepairCost(building) : null,
            world.ComplexMembers(building),
            definition.Kind == BuildingKind.Teleporter ? Teleporter(world, building) : null,
            definition.EvolvesTo is { } next && owner != null ? Evolve(world, owner, building, next) : null,
            definition.Kind == BuildingKind.Storage ? Keep(world, building, definition, network) : null);
    }

    private static StorageKeepDto Keep(GameWorld world, Building storage, BuildingDefinition definition, Network? network)
    {
        var resources = storage.Contents.Keys
            .Concat(network?.Solution.Flows.Where(t => t.To == storage.Id).Select(t => t.Resource) ?? [])
            .Distinct();
        var full = network != null && network.Buildings
            .Select(world.Building)
            .Any(t => t != null && t.Owner == storage.Owner && world.Content.Building(t.Type).Kind == BuildingKind.TradingPost && world.TradingPostFull(t));
        return new StorageKeepDto(resources.ToDictionary(t => t, t => Math.Round(world.StorageKeep(storage, definition, t), 1)), full);
    }

    private static EvolveDto Evolve(GameWorld world, Company owner, Building building, string next)
    {
        var (_, cost, problem) = world.EvolvePlan(owner, building);
        return new EvolveDto(next, Math.Round(cost), problem);
    }

    private static TeleporterDto Teleporter(GameWorld world, Building station)
    {
        var receiver = world.TeleportReceiver(station);
        var sender = receiver == null ? world.TeleportSender(station) : null;
        var partner = receiver ?? sender;
        var from = sender ?? station;
        var candidates = world.BuildingsOf(station.Owner)
            .Where(t => t.Id != station.Id && t.Type == station.Type)
            .Select(t => new TeleporterCandidateDto(t.Id, t.X, t.Y, Math.Round(GameWorld.TeleportDistance(station, t), 1), world.TeleportLinkProblem(station, t)))
            .OrderBy(t => t.Distance)
            .ToList();
        return new TeleporterDto(
            receiver != null ? "sender" : sender != null ? "receiver" : null,
            partner?.Id,
            receiver != null ? station.Filter.FirstOrDefault() : sender?.Filter.FirstOrDefault(),
            partner == null ? 0 : Math.Round(GameWorld.TeleportDistance(station, partner), 1),
            Math.Round(world.Teleported(from), 2),
            world.TeleportThroughput(station),
            Math.Round(GameWorld.TeleportEnergyPerTile(station.Type) * (partner == null ? 0 : GameWorld.TeleportDistance(station, partner)), 4),
            GameWorld.TeleportLoss(station.Type),
            world.Content.Building(station.Type).Range ?? "district",
            candidates);
    }

    /// <summary>Why a building can't run (fully): missing suppliers or buyers on its pipes</summary>
    private static List<string> Issues(GameWorld world, Building building, FlowNode? node, Network? network)
    {
        var issues = new List<string>();
        if (building.Damaged)
        {
            issues.Add("Storm damage: it works at half speed until it is repaired.");
        }
        if (node is { Kind: FlowNodeKind.TradingPost } && network != null && world.TradingPostFull(building))
        {
            issues.Add($"Sells all it can ({node.Throughput:N0} a minute): upgrade it, or build a Market Hall, to sell more.");
        }
        if (node == null || node.Kind != FlowNodeKind.Building || building.UnderConstruction)
        {
            return issues;
        }
        if (network != null && node.Out.Count > 0 && network.Solution.Rate(building.Id) < 0.99
            && network.Buildings.Select(world.Building).Any(t => t != null && t.Owner == building.Owner && world.TradingPostFull(t)))
        {
            issues.Add("Slowed down: your trading post sells all it can. Upgrade it to sell more.");
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

    /// <summary>A network as a company sees it (its own pump stations and speed)</summary>
    public static NextStepDto NextStep(AdvisorStep step) => new(
        JsonNamingPolicy.CamelCase.ConvertName(step.Kind.ToString()),
        step.Title,
        step.Reason,
        JsonNamingPolicy.CamelCase.ConvertName(step.Action.ToString()),
        Math.Round(step.Cost, 2),
        Math.Round(step.GainPerMinute, 2),
        step.PaybackMinutes is { } payback ? Math.Round(payback, 1) : null,
        step.WaitMinutes is { } wait ? Math.Round(wait, 1) : null,
        step.Building,
        step.BuildingType,
        step.Levels,
        step.Parcel,
        step.Partner,
        step.At?.X,
        step.At?.Y,
        step.Research,
        step.Count,
        step.Network,
        step.Tiles?.SelectMany(t => new[] { t.X, t.Y }).ToArray() ?? [],
        step.Tier);

    public static NetworkDetailsDto NetworkDetails(GameWorld world, Network network, long? viewer = null)
    {
        var solution = network.Solution;
        var isMachine = new Dictionary<long, bool>();
        bool Machine(long id) => isMachine.TryGetValue(id, out var v) ? v
            : isMachine[id] = world.Building(id) is { } b && world.Content.Building(b.Type).Kind is not (BuildingKind.Storage or BuildingKind.TradingPost);

        var resources = new Dictionary<string, double[]>();
        double[] Row(string resource) => resources.TryGetValue(resource, out var row) ? row : resources[resource] = new double[8];
        // Per company: made, used, sold; and its money: hub sales, neighbour sales, neighbour purchases, hub imports
        var perOwner = new Dictionary<(long, string), double[]>();
        var money = new Dictionary<long, double[]>();
        double[] Money(long owner) => money.TryGetValue(owner, out var row) ? row : money[owner] = new double[4];
        double[] OwnerRow(long owner, string resource) => perOwner.TryGetValue((owner, resource), out var row) ? row : perOwner[(owner, resource)] = new double[3];
        foreach (var flow in solution.Flows)
        {
            var from = world.Building(flow.From);
            var to = world.Building(flow.To);
            if (from != null && Machine(flow.From))
            {
                OwnerRow(from.Owner, flow.Resource)[0] += flow.Sent;
            }
            if (to != null && Machine(flow.To) && flow.Kind is FlowKind.Internal or FlowKind.Trade or FlowKind.Contract or FlowKind.FromStorage or FlowKind.Import)
            {
                OwnerRow(to.Owner, flow.Resource)[1] += flow.Delivered;
            }
            if (from != null && flow.Kind == FlowKind.Sale)
            {
                OwnerRow(from.Owner, flow.Resource)[2] += flow.Delivered;
            }
            var value = flow.Delivered * flow.PricePerUnit;
            switch (flow.Kind)
            {
                case FlowKind.Sale when from != null:
                    Money(from.Owner)[0] += value;
                    break;
                case FlowKind.Trade or FlowKind.Contract when from != null && to != null:
                    Money(from.Owner)[1] += value;
                    Money(to.Owner)[2] += value;
                    break;
                case FlowKind.Import when to != null:
                    Money(to.Owner)[3] += value;
                    break;
            }
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

        // What the buildings need of each resource: of what holds them back their full need, of the rest what they get
        // (the advisor's shortages are the same numbers)
        var needed = world.NetworkNeeds(network).ToDictionary(t => t.Key, t => t.Value.Needed);

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
            .Select(t => new NetworkOwnerDto(t.Id, t.Name, t.Color, solution.GoldPerMinute.TryGetValue(t.Id, out var g) ? g : 0,
                perOwner.Where(p => p.Key.Item1 == t.Id && p.Value.Any(v => v > 0.005))
                    .OrderBy(p => world.Content.Resources.TryGetValue(p.Key.Item2, out var r) ? r.Tier : 99).ThenBy(p => p.Key.Item2)
                    .Select(p => new OwnerResourceDto(p.Key.Item2, p.Value[0], p.Value[1], p.Value[2]))
                    .ToList(),
                Money(t.Id)[0], Money(t.Id)[1], Money(t.Id)[2], Money(t.Id)[3]))
            .ToList();
        var pumps = network.Buildings.Count(t => world.Building(t) is { } b && (viewer == null || b.Owner == viewer) && world.Content.Building(b.Type).Kind == BuildingKind.Pump);

        return new NetworkDetailsDto(
            network.Id,
            network.Buildings.Count(t => world.Building(t) is { } b && !world.Content.Building(b.Type).IsConductor),
            network.Conductors.Count,
            pumps,
            viewer is { } me ? network.PumpFactorOf(me) : network.PumpFactor,
            owners,
            resources
                .Select(t => (Resource: t.Key, Row: t.Value, Needed: needed.GetValueOrDefault(t.Key)))
                .Concat(needed.Where(t => !resources.ContainsKey(t.Key)).Select(t => (Resource: t.Key, Row: new double[8], Needed: t.Value)))
                .OrderBy(t => world.Content.Resources.TryGetValue(t.Resource, out var r) ? r.Tier : 99).ThenBy(t => t.Resource)
                .Select(t => new NetworkResourceDto(t.Resource, t.Row[0], t.Row[1], t.Row[2], t.Row[3], t.Row[4], t.Row[5], t.Row[6], t.Row[7], t.Needed))
                .ToList(),
            bottlenecks);
    }

    /// <summary>The companies of a district as one of them sees them: surplus, needs, trade with you</summary>
    public static List<NeighbourDto> Neighbourhood(GameWorld world, Company me)
    {
        var (surplus, needs, shortfalls, earn, pay, connected, links) = world.NeighbourhoodOf(me);
        List<ResourceRateDto> Rates(Dictionary<long, Dictionary<string, double>> map, long company) =>
            map.TryGetValue(company, out var rates)
                ? rates.Where(t => t.Value > 0.01).OrderByDescending(t => t.Value * world.BasePrice(t.Key)).Select(t => new ResourceRateDto(t.Key, t.Value)).ToList()
                : [];

        var home = world.EstateOf(me);
        if (home == null)
        {
            return [];
        }
        return world.Companies
            .Where(t => t.District == me.District && t.Id != me.Id && world.EstateOf(t) != null)
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
                    connected.Contains(t.Id),
                    Rates(surplus, t.Id),
                    Rates(needs, t.Id),
                    shortfalls.TryGetValue(t.Id, out var missing)
                        ? missing.Select(m => m.Keys.Order().ToList()).DistinctBy(m => string.Join(",", m)).ToList<IReadOnlyList<string>>()
                        : [],
                    earn.GetValueOrDefault(t.Id),
                    pay.GetValueOrDefault(t.Id),
                    links.TryGetValue(t.Id, out var link) ? Math.Round(link.Efficiency, 3) : null,
                    links.TryGetValue(t.Id, out var tiles) ? tiles.Tiles : null,
                    me.TradeBlocked.Contains(t.Id));
            })
            .OrderBy(t => Math.Abs(t.EstateX - home.Origin.X) + Math.Abs(t.EstateY - home.Origin.Y))
            .ToList();
    }

    /// <summary>How lively a stranger looks on the picker: never a count of days</summary>
    private static string ActivityBucket(GameWorld world, Company company) =>
        world.Now - company.LastSeenAt < TimeSpan.FromDays(1) ? "lively" : world.Now - company.LastSeenAt < TimeSpan.FromDays(7) ? "some days" : "resting";

    public static SeatDto Seat(GameWorld world, EstateKey seat, Company? me)
    {
        var estate = world.Estate(seat.District, seat.Slot);
        var origin = WorldLayout.EstateOrigin(seat.District, seat.Slot);
        var profile = world.Generator.Estate(seat.District, seat.Slot);
        var owner = estate?.OwnerId is { } id ? world.Company(id) : null;
        var hold = world.HoldOn(seat);
        var state = owner != null && owner.Id == me?.Id ? "you"
            : owner is { IsNpc: false } ? "taken"
            : hold != null ? "held"
            : owner is { IsNpc: true } ? "house"
            : "free";
        return new SeatDto(
            seat.Slot,
            origin.X,
            origin.Y,
            Camel(profile.Primary),
            profile.Secondary is { } secondary ? Camel(secondary) : null,
            state,
            owner?.Id,
            owner?.Name,
            owner?.Color,
            owner?.Level ?? 0,
            owner is { IsNpc: false } && owner.Id != me?.Id ? ActivityBucket(world, owner) : null,
            hold?.Crew.Name ?? (owner?.CrewId is { } crew ? world.Crew(crew)?.Name : null),
            hold != null && me?.CrewId == hold.Value.Crew.Id);
    }

    public static StationDto Station(GameWorld world, StationRef station, Company? me)
    {
        var yard = WorldLayout.Yard(station);
        var seats = GameWorld.StationSeats(station).Select(t => Seat(world, t, me)).ToList();
        var crews = seats.Where(t => t.CrewName != null).GroupBy(t => t.CrewName).OrderByDescending(t => t.Count()).FirstOrDefault();
        return new StationDto(
            station.District.X,
            station.District.Y,
            station.Index,
            WorldNames.Station(station),
            station.IsSmall,
            world.StationOpenToStrangers(station, me),
            GameWorld.StationSeats(station).Count(t => world.SeatFree(t, me)),
            seats.Count(t => t.Activity == "lively"),
            seats,
            crews?.Count() >= 2 ? crews.Key : null,
            yard.X0,
            yard.Y0,
            yard.X1,
            yard.Y1,
            ProjectLine(world, station),
            world.PlanBoard(station).Makes.Order().ToList(),
            world.PlanBoard(station).Needs.Order().ToList());
    }

    private static SeatRefDto Ref(EstateKey seat) => new(seat.District.X, seat.District.Y, seat.Slot);

    /// <summary>The stations of a district for the picker (the suggested one's district when none is given)</summary>
    public static StationsDto Stations(GameWorld world, Company? me, DistrictCoord? district, int level = 1)
    {
        var neighbours = world.SuggestSeat(Placement.Neighbours, me?.Level ?? level, 1, me);
        var own = world.SuggestSeat(Placement.OwnStation, me?.Level ?? level, 1, me);
        var friends = world.SuggestSeat(Placement.Friends, me?.Level ?? level, 4, me);
        var shown = district ?? neighbours.District;
        return new StationsDto(
            shown.X,
            shown.Y,
            WorldNames.District(shown),
            WorldLayout.Stations(shown).Select(t => Station(world, t, me)).ToList(),
            Ref(neighbours),
            Ref(own),
            Ref(friends),
            world.OpenDistricts.OrderBy(t => Math.Abs(t.X) + Math.Abs(t.Y)).Select(t => new DistrictRefDto(t.X, t.Y, WorldNames.District(t))).ToList());
    }

    /// <summary>What a code opens (the invite screen of someone who isn't playing yet)</summary>
    public static InviteDto Invite(GameWorld world, string code)
    {
        if (world.HoldByCode(code) is { } held)
        {
            var from = world.CrewMembers(held.Crew).FirstOrDefault(t => t.Id == held.Crew.Founder) ?? world.CrewMembers(held.Crew).FirstOrDefault();
            var station = WorldLayout.StationOf(held.Hold.Seat.District, held.Hold.Seat.Slot);
            return new InviteDto(true, held.Crew.Name, held.Crew.Emblem, held.Crew.Color, held.Crew.Color2, from?.Name, Station(world, station, null), held.Hold.Slot, true, false, world.FreeCompanyName(held.Hold.Seat));
        }
        if (world.CrewByInvite(code) is { } crew)
        {
            var from = world.CrewMembers(crew).FirstOrDefault();
            var home = from != null && world.EstateOf(from) is { } estate ? estate.Station : (StationRef?)null;
            return new InviteDto(true, crew.Name, crew.Emblem, crew.Color, crew.Color2, from?.Name, home is { } h ? Station(world, h, null) : null, null, false,
                world.Now - crew.InviteCodeAt > GameWorld.InviteOpenFor, world.FreeCompanyName());
        }
        return new InviteDto(false, null, null, null, null, null, null, null, false, false, null);
    }

    public static HeldSeatDto Held(GameWorld world, SeatHold hold)
    {
        var profile = world.Generator.Estate(hold.Seat.District, hold.Seat.Slot);
        return new HeldSeatDto(GameWorld.ShowCode(hold.Code), hold.DistrictX, hold.DistrictY, hold.Slot, Camel(profile.Primary), profile.Secondary is { } secondary ? Camel(secondary) : null, hold.Until,
            hold.For, hold.For is { } member ? world.Company(member)?.Name : null, WorldNames.Station(WorldLayout.StationOf(hold.Seat.District, hold.Seat.Slot)));
    }

    public static CrewViewDto Crew(GameWorld world, Company me)
    {
        if (world.CrewOf(me) is not { } crew)
        {
            return new CrewViewDto(null, GameWorld.CrewEmblems);
        }
        CrewMemberDto Member(Company t) =>
            new(t.Id, t.Name, t.Color, t.Level, t.Id == me.Id ? null : t.Asleep ? "asleep" : ActivityBucket(world, t),
                world.EstateOf(t) is { } estate ? WorldNames.Station(estate.Station) : t.Asleep ? "the Quiet Isles" : "on the way", t.Slot, t.Id == me.Id,
                world.CanTend(me, t), world.Tended(t), t.Asleep);
        var home = world.EstateOf(me)?.Station;
        var closed = home is { } station && !world.StationOpenToStrangers(station);
        return new CrewViewDto(new CrewDto(
            crew.Id,
            crew.NameHidden ? $"Crew {crew.Id}" : crew.Name,
            crew.Emblem,
            crew.Color,
            crew.Color2,
            world.CrewMembers(crew).Select(Member).ToList(),
            crew.Holds.Select(t => Held(world, t)).ToList(),
            GameWorld.ShowCode(crew.InviteCode),
            crew.InviteCodeAt + GameWorld.InviteOpenFor,
            crew.JoinRequests.Select(world.Company).OfType<Company>().Select(Member).ToList(),
            home is { } h && crew.OpenStations.Contains($"{h.District.X},{h.District.Y},{h.Index}"),
            closed,
            GameWorld.MaxCrewSize,
            crew.KeepCards.Select(world.Company).OfType<Company>().Select(t => new KeepCardDto(t.Id, t.Name, world.HoldEnds(t))).ToList(),
            crew.ConsolidationOpen,
            world.CrewMembers(crew).Count() > 1 ? WorldNames.Station(WorldLayout.StationOf(world.CrewMoveSeat(me).District, world.CrewMoveSeat(me).Slot)) : null), GameWorld.CrewEmblems);
    }

    /// <summary>A duration as a state line says it: "45 min", "1 h 40 min", "2 days"</summary>
    private static string Span(TimeSpan span) =>
        span.TotalMinutes < 1 ? "a moment"
        : span.TotalHours < 1 ? $"{Math.Ceiling(span.TotalMinutes):0} min"
        : span.TotalHours < 24 ? (span.Minutes == 0 ? $"{(int)span.TotalHours} h" : $"{(int)span.TotalHours} h {span.Minutes} min")
        : $"{Math.Round(span.TotalDays):0} days";

    private static string PerMinute(double rate) => rate >= 100 ? $"{rate:N0}" : rate >= 10 ? $"{rate:0.#}" : $"{rate:0.##}";

    private static IReadOnlyList<int> Flat(IEnumerable<TilePos> route) => route.SelectMany(t => new[] { t.X, t.Y }).ToList();

    /// <summary>One line that says what a link does now, and why when nothing flows (v1.2 §7: no pipe ends in silence)</summary>
    private static (string Tone, string Line) LinkLine(GameWorld world, Link link, Company me)
    {
        var partner = world.Company(link.Partner(me.Id));
        var name = partner?.Name ?? "Your partner";
        var good = world.Content.Resources.TryGetValue(link.Resource, out var r) ? r.Name.ToLowerInvariant() : link.Resource;
        var selling = link.Seller == me.Id;
        var price = world.PlayerPrice(link.Resource);
        var tank = world.TankNow(link);
        var tankTime = link.Need > 1e-9 && tank > 1e-6 ? Span(TimeSpan.FromMinutes(tank / link.Need)) : null;
        if (link.State == LinkState.Offered)
        {
            var left = Span(link.CreatedAt + GameWorld.LinkOfferLifetime - world.Now);
            return link.OfferedBy == me.Id
                ? ("info", $"Waiting for {name} to accept (the offer stays {left} more; the pipe is refunded if they don't)")
                : ("info", selling ? $"{name} would like to buy your {good} at {price:0.##} g: accept to start" : $"{name} offers you their {good} at {price:0.##} g: accept to start");
        }
        if (link.State == LinkState.Paused)
        {
            var by = link.PausedBy == me.Id ? "you" : name;
            return ("warn", !selling && tankTime != null
                ? $"Paused by {by}: nothing flows; your tank keeps {tankTime} of it"
                : $"Paused by {by}: nothing flows. {(link.PausedBy == me.Id ? "Resume it when you like" : "They resume it")}");
        }
        var notice = link.NoticeAt is { } at
            ? $" · {(link.NoticeEnds ? "ends" : "pauses")} in {Span(at - world.Now)} ({(link.NoticeBy == me.Id ? "your notice" : $"{name}'s notice")})"
            : "";
        var resting = partner?.StandIn is { } standIn
            ? selling
                ? $" {standIn.Name} ⚙ stands in for {name}: it keeps buying, its terms gliding to a House's from {standIn.Since.AddDays(standIn.OldPriceWeek ? 7 : 0):d MMM}."
                : $" {standIn.Name} ⚙ stands in for {name}: it keeps this link going."
            : partner != null && world.ActivityOf(partner) != Activity.Active ? $" {name} is resting; this link keeps delivering." : "";
        var (tone, line) = link.Flow switch
        {
            LinkFlow.Live => ("good", selling
                ? $"{name} buys {PerMinute(link.Rate)} {good}/min at {price:0.##} g"
                : $"You buy {PerMinute(link.Rate)} {good}/min from {name} at {price:0.##} g"),
            LinkFlow.Capped => ("info", $"The pipe is full at {PerMinute(link.Rate)}/min: the rest goes to the Yard and the hub. A better pipe carries more"),
            LinkFlow.NotNeeded => ("info", selling
                ? $"{name} needs no {good} right now: yours goes to the Yard and the hub"
                : $"You need no {good} right now: the link waits"),
            LinkFlow.SellerShort => ("warn", selling
                ? $"You have less {good} spare than {name} needs{(link.Rate > 0 ? $" ({PerMinute(link.Rate)}/min arrives)" : "")}: make more, or their tank runs dry"
                : tankTime != null
                    ? $"{name} has less spare than you need: your tank gives {tankTime} more"
                    : $"{name} has no {good} spare right now: the Yard and the hub fill in"),
            LinkFlow.NotConnected => ("warn", world.EstateOf(me) is not { } estate || world.GateNetwork(estate) == null
                ? "Pipe your network to your Yard gate: the link starts there"
                : !selling && tankTime != null
                    ? $"{name}'s Yard gate isn't piped: your tank gives {tankTime} more"
                    : $"{name} hasn't piped their Yard gate yet (or is on the way)"),
            LinkFlow.NotAllowed => ("warn", "Nothing flows: a Block, a trade setting, or a buyer out of gold"),
            _ => ("info", ""),
        };
        return (tone, line + "." + resting + notice);
    }

    public static LinkDto Link(GameWorld world, Link link, Company me)
    {
        var partner = world.Company(link.Partner(me.Id));
        var (tone, line) = LinkLine(world, link, me);
        return new LinkDto(
            link.Id,
            link.Partner(me.Id),
            partner?.Name ?? "?",
            partner?.Color ?? "#999999",
            link.Resource,
            link.Seller == me.Id,
            link.OfferedBy == me.Id,
            Camel(link.State),
            link.Tier,
            link.Length,
            world.LinkEfficiency(link),
            world.LinkThroughput(link),
            world.PlayerPrice(link.Resource),
            link.Need,
            link.Rate,
            world.TankNow(link),
            link.TankSize,
            link.Delivered,
            link.NoticeAt,
            link.NoticeEnds,
            link.NoticeBy == me.Id,
            link.State == LinkState.Paused ? link.PausedBy == me.Id || link.PausedBy == null : link.NoticeAt != null && link.NoticeBy == me.Id,
            link.State == LinkState.Offered ? link.CreatedAt + GameWorld.LinkOfferLifetime : null,
            tone,
            line,
            Flat(link.Route));
    }

    /// <summary>Your links, who a link would suit, and whom you could link with</summary>
    public static LinksDto Links(GameWorld world, Company me)
    {
        var home = world.EstateOf(me) is { } estate ? world.BigStation(estate.Station)[0] : (StationRef?)null;
        var partners = world.Companies
            .Where(t => !t.IsNpc && t.Id != me.Id && !t.Asleep && world.EstateOf(t) is { } e && world.BigStation(e.Station)[0] != home)
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(t => new LinkPartnerDto(t.Id, t.Name, t.Color, t.Level, WorldNames.Station(world.EstateOf(t)!.Station)))
            .ToList();
        return new LinksDto(
            world.LinksOf(me.Id).OrderBy(t => t.State == LinkState.Offered && t.OfferedBy != me.Id ? 0 : 1).ThenBy(t => t.Id).Select(t => Link(world, t, me)).ToList(),
            world.LinkSuggestions(me).Select(t => new LinkSuggestionDto(t.Partner.Id, t.Partner.Name, t.Partner.Color,
                WorldNames.Station(world.EstateOf(t.Partner)!.Station), t.Resource, t.Selling, t.PerMinute, t.Length, world.LinkEfficiency(1, t.Length))).ToList(),
            GameWorld.MaxLinksPerEstate,
            partners);
    }

    public static LinkPlanDto LinkPlan(GameWorld world, GameWorld.LinkPlan plan) =>
        new(plan.Length, plan.Efficiency, plan.Throughput, plan.Gold, plan.Materials.Select(t => new ResourceRateDto(t.Key, t.Value)).ToList(), plan.Error, Flat(plan.Route));

    /// <summary>Every link on the map (offers aren't laid yet)</summary>
    public static IReadOnlyList<MapLinkDto> MapLinks(GameWorld world) =>
        world.Links.Where(t => t.State != LinkState.Offered)
            .Select(t => new MapLinkDto(t.Id, t.Resource, t.Tier, t.Rate > 0, t.Seller, t.Buyer, Flat(t.Route)))
            .ToList();

    /// <summary>The lone-human card's stations (and the Move tab's "livelier stations"): where active players live and a seat is free</summary>
    public static IReadOnlyList<LandingCardDto> Livelier(GameWorld world, Company company) =>
        world.LivelierStations(company).Select(t => new LandingCardDto("lively", Ref(t.Seat), WorldNames.Station(t.Station),
            world.Estate(t.Seat.District, t.Seat.Slot) is { } target ? world.CratesIfMovedTo(company, target) : 0, false, t.Active)).ToList();

    public static IReadOnlyList<LandingCardDto> Landing(GameWorld world, Company company) =>
        world.LandingCards(company).Select(t =>
        {
            var station = WorldLayout.StationOf(t.Seat.District, t.Seat.Slot);
            var neighbours = world.BigStation(station).SelectMany(GameWorld.StationSeats)
                .Count(s => s != t.Seat && world.Estate(s.District, s.Slot)?.OwnerId is { } o && world.Company(o) is { IsNpc: false } c && c.Id != company.Id && !c.Asleep);
            return new LandingCardDto(t.Kind, Ref(t.Seat), WorldNames.Station(station), t.Crates, t.Here, neighbours);
        }).ToList();

    public static ReturnDto Return(GameWorld world, Company company, TimeSpan away, double earned, double fee, int tended, string? standIn, bool wasAsleep, bool fromIsles) =>
        new(
            Math.Round(away.TotalDays, 1),
            earned,
            fee,
            tended,
            standIn,
            wasAsleep,
            fromIsles,
            world.EstateOf(company) is { } estate ? WorldNames.Station(estate.Station) : "",
            world.LinksOf(company.Id).Count(t => t.State != LinkState.Offered),
            company.Crates.Count,
            fromIsles ? Landing(world, company) : []);

    public static AskDto Ask(CompanyAsk ask) =>
        new(ask.Resource, ask.Amount, ask.Got, ask.Until, ask.Renew, ask.Incoming.Values.Sum());

    /// <summary>The station's project as its residents see it</summary>
    public static ProjectDto? Project(GameWorld world, Company me)
    {
        if (world.EstateOf(me) is not { } estate)
        {
            return null;
        }
        var project = world.ProjectOf(estate.Station);
        var made = world.StationMakes(estate.Station);
        var total = project.Credit.Values.Sum();
        var helpers = project.Credit.Where(t => t.Value > 0 && world.Company(t.Key) != null)
            .OrderByDescending(t => t.Value)
            .Select(t => new ProjectHelperDto(world.Company(t.Key)!.Color, total <= 0 ? 0 : t.Value / total, t.Key == me.Id))
            .ToList();
        string Name(string resource) => world.Content.Resources.TryGetValue(resource, out var r) ? r.Name.ToLowerInvariant() : resource;
        var opens = project.Stage == ProjectStage.FoundryHall ? project.StartedAt + TimeSpan.FromDays(14) : (DateTime?)null;
        var keystone = project.Keystone;
        var line = project.Stage == ProjectStage.Done
            ? "Every stage stands: the station's Landmark is finished."
            : opens is { } at && at > world.Now
                ? $"The Foundry Hall opens in {Span(at - world.Now)}: until then the station grows."
                : keystone != null && !made.Contains(keystone) && project.Delivered.GetValueOrDefault(keystone) < project.Basket.GetValueOrDefault(keystone)
                    ? $"Wanted by the {GameWorld.StageName(project.Stage)}: {Name(keystone)}. Nobody here makes it yet."
                    : $"The {GameWorld.StageName(project.Stage)} takes your spare at the hub's full price: pipe it to your Yard gate.";
        return new ProjectDto(
            WorldNames.Station(estate.Station),
            GameWorld.StageName(project.Stage),
            (int)project.Stage,
            Enum.GetValues<ProjectStage>().Where(t => t != ProjectStage.Done).Select(GameWorld.StageName).ToList(),
            project.Basket.OrderBy(t => t.Key == keystone ? 1 : 0).ThenBy(t => t.Key)
                .Select(t => new ProjectGoodDto(t.Key, t.Value, Math.Min(t.Value, project.Delivered.GetValueOrDefault(t.Key)), t.Key == keystone, made.Contains(t.Key))).ToList(),
            helpers,
            world.ProjectBonus(project),
            project.FoundryChains,
            opens,
            project.Credit.GetValueOrDefault(me.Id),
            project.Allowance.GetValueOrDefault(me.Id),
            me.ProjectFirst.Order().ToList(),
            line);
    }

    /// <summary>The project's stage and progress for a station card in the picker</summary>
    private static string? ProjectLine(GameWorld world, StationRef station)
    {
        if (world.ExistingProject(station) is not { } project)
        {
            return null;
        }
        if (project.Stage == ProjectStage.Done)
        {
            return "Landmark finished";
        }
        var target = project.Basket.Sum(t => t.Value * world.BasePrice(t.Key));
        var done = project.Basket.Sum(t => Math.Min(t.Value, project.Delivered.GetValueOrDefault(t.Key)) * world.BasePrice(t.Key));
        return $"{GameWorld.StageName(project.Stage)} · {(target <= 0 ? 0 : 100 * done / target):0}%";
    }

    /// <summary>Whether a company sells a good to the hub (some of its buildings reach a Trading Post)</summary>
    private static bool HubTakes(GameWorld world, Company me, string resource) =>
        world.BuildingsOf(me.Id).Any(b => world.NetworkOf(b.Id)?.Solution.Flows.Any(f => f.From == b.Id && f.Kind == FlowKind.Sale && f.Resource == resource) == true);

    /// <summary>The player's station and its Yard</summary>
    public static YardDto? Yard(GameWorld world, Company me)
    {
        if (world.EstateOf(me) is not { } estate)
        {
            return null;
        }
        var station = estate.Station;
        var yard = WorldLayout.Yard(station);
        var residents = world.YardResidents(station);
        List<ResourceRateDto> Rates(IReadOnlyDictionary<string, double> rates) =>
            rates.Where(t => t.Value > 0.01).OrderByDescending(t => t.Value * world.BasePrice(t.Key)).Select(t => new ResourceRateDto(t.Key, t.Value)).ToList();
        var yours = world.YardTrades(estate)
            .Where(t => t.PerMinute > 1e-6 && world.Company(t.Partner) != null)
            .Select(t => new YardTradeDto(t.Partner, world.Company(t.Partner)!.Name, t.Resource, t.PerMinute, t.Selling, world.YardPrice(me, world.Company(t.Partner)!, t.Resource, t.Selling)))
            .OrderByDescending(t => t.PerMinute * t.Price)
            .ToList();
        var connected = residents.FirstOrDefault(t => t.Company.Id == me.Id)?.Connected ?? false;
        var mine = residents.FirstOrDefault(t => t.Company.Id == me.Id);
        string Name(string resource) => world.Content.Resources.TryGetValue(resource, out var r) ? r.Name.ToLowerInvariant() : resource;
        var line = !connected
            ? "Lay a pipe from your network to your Yard gate: your neighbours buy what you have spare, and sell you what you lack."
            : residents.Count(t => t.Connected) < 2
                ? "Nobody else here has piped to the Yard yet: the hub takes your spare until they do."
                : yours.FirstOrDefault(t => t.Selling) is { } sale
                    ? $"{sale.PartnerName} buys your {Name(sale.Resource)} at {sale.Price:0.##} g."
                    : yours.FirstOrDefault() is { } purchase
                        ? $"You buy {Name(purchase.Resource)} from {purchase.PartnerName} at {purchase.Price:0.##} g."
                        : mine?.Spare.Where(t => t.Value > 0.01).OrderByDescending(t => t.Value).Select(t => t.Key).FirstOrDefault() is { } spare
                            ? HubTakes(world, me, spare)
                                ? $"Nobody here needs {Name(spare)} yet: the hub takes it."
                                : $"Nobody here needs {Name(spare)} yet: pipe it to your Trading Post and the hub buys it."
                            : "Nothing to trade yet: what you make your own buildings use.";
        return new YardDto(
            WorldNames.Station(station),
            station.IsSmall,
            yard.X0,
            yard.Y0,
            yard.X1,
            yard.Y1,
            estate.YardGate.X,
            estate.YardGate.Y,
            connected,
            residents.Select(t => new YardResidentDto(t.Company.Id, t.Company.Name, t.Company.Color, t.Company.Level, t.Company.IsNpc, t.Slot, t.Connected,
                t.Company.IsNpc ? null : Camel(world.ActivityOf(t.Company)), Rates(t.Spare), Rates(t.Lacks), me.TradeBlocked.Contains(t.Company.Id),
                t.Company.House is { } kind ? Camel(kind) : null, t.Company.StandIn?.Name,
                t.Company.PinMakes.Order().ToList(), t.Company.PinNeeds.Order().ToList(),
                t.Company.Asks.Select(Ask).ToList())).ToList(),
            yours,
            world.YardTotals(station).Select(t => new ResourceRateDto(t.Key, t.Value)).OrderByDescending(t => t.PerMinute).ToList(),
            line);
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
        // Global Exchange: the global orders of every hub as well
        var global = world.HasResearch(me, "globalExchange");
        var orders = world.ExchangeOrders.Where(t => t.District == me.District || (global && t.Global)).ToList();
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
            .Select(t => new ExchangeOrderDto(t.Id, t.Resource, t.Selling, t.Amount, t.Price, t.CreatedAt, true, t.Global))
            .ToList();
        var shipments = world.Shipments
            .Where(t => t.To == me.Id || t.From == me.Id)
            .OrderBy(t => t.ArrivesAt)
            .Select(t => new ShipmentDto(t.Id, t.Resource, t.Amount, t.ArrivesAt, t.To == me.Id, world.Company(t.To == me.Id ? t.From : t.To)?.Name ?? ""))
            .ToList();
        return new ExchangeDto(world.HasBuilding(me.Id, BuildingKind.ExchangeOffice), books, mine, shipments, GameWorld.ExchangeFee, global);
    }

    public static ToastDto Toast(WorldEvent worldEvent) => new(Camel(worldEvent.Kind), worldEvent.Text, worldEvent.Building);

    /// <summary>The content files as the client sees them</summary>
    public static object Content(GameContent content) => new
    {
        content.ResourceList,
        Buildings = content.BuildingList,
        content.PipeTiers,
        Research = content.ResearchList,
        content.Relics,
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
            WorldLayout.YardSize,
            YardGates = Enumerable.Range(0, WorldLayout.EstatesPerDistrict).Select(WorldLayout.YardGateLocal).Select(t => new { t.X, t.Y }).ToList(),
            Stations = Enumerable.Range(0, WorldLayout.StationsPerDistrict).Select(t => new
            {
                Yard = WorldLayout.YardLocal(t) is var yard ? new { yard.X0, yard.Y0, yard.X1, yard.Y1 } : null,
                Slots = WorldLayout.StationSlots(t),
            }).ToList(),
            StationOfSlot = Enumerable.Range(0, WorldLayout.EstatesPerDistrict).Select(WorldLayout.StationOfSlot).ToList(),
            Terrain = Enum.GetValues<TerrainType>().Select(Camel).ToList(),
            RichnessMultiplier = GameWorld.RichnessMultiplier,
        },
    };
}
