using System.Text.Json;
using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.State;
using TransportPipes.Game.World;
using TransportPipes.Server.Dtos;

namespace TransportPipes.Server.Services;

/// <summary>Turns the game world into what clients see. Only call these while the world is locked.</summary>
public static partial class GameViews
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
            Orders(world, company),
            company.NoTrade.OrderBy(t => t).ToList(),
            company.MentorId,
            company.Likes,
            world.Calendar(company).Select(t => Event(world, t)).ToList(),
            (int)world.Horizon(company).TotalHours,
            world.Era,
            world.EraDeadline,
            company.Titles.OrderBy(t => t).ToList(),
            company.Achievements.Where(t => t.StartsWith("collection:")).Select(t => t["collection:".Length..]).ToList(),
            world.OutpostsOf(company.Id).Select(t => Outpost(world, t, company.Id)).ToList(),
            company.LegacyLevel,
            SeasonInfo(world),
            new MovingDto(
                Camel(world.ActivityOf(company)),
                company.ArrivesAt,
                company.Crates.Select((t, i) => new CrateDto(i, t.Type, t.Level)).ToList(),
                company.FreeMoves,
                world.MoveCost(company),
                company.LastMovedAt is { } moved && moved + world.MoveEvery > world.Now && company.FreeMoves == 0 ? moved + world.MoveEvery : null),
            new DeepDto(
                world.HasDeepLevel(company),
                company.DeepParcels,
                company.DigParcel,
                company.DigStartedAt,
                company.DigEndsAt,
                GameWorld.DigCost(GameWorld.DugCount(company)),
                GameWorld.DigTime(GameWorld.DugCount(company)).TotalHours,
                WorldLayout.DeepOffset),
            world.CurrentTutorialStep(company) is { } step
                ? Tutorial(world, company, step)
                : null,
            new ExpeditionDto(
                company.Expedition?.Hours,
                company.Expedition?.EndsAt,
                GameWorld.ExpeditionHours.ToDictionary(t => t, t => GameWorld.ExpeditionCost(company, t)),
                company.Expedition == null ? world.ExpeditionBlocker(company) : null,
                company.ExpeditionLog.Take(5).ToList(),
                company.Relics.OrderBy(t => t).ToList()),
            new MasteryDto(company.Mastery, world.ResearchedEverything(company), GameWorld.MasteryCost(company), GameWorld.MasteryMinutes(company), company.Mastery * GameWorld.MasteryBonus),
            world.Alerts(company).Select(t => new AlertDto(t.Text, t.Building, t.Kind,
                t.Kind == "damage" ? Math.Round(world.BuildingsOf(company.Id).Where(b => b.Damaged).Sum(world.RepairCost)) : null)).ToList(),
            world.Now,
            Experience(world, company, goldPerMinute),
            Camel(company.TradeWith),
            world.CrewOf(company) is { } crew
                ? new CrewBadgeDto(crew.Id, crew.NameHidden ? $"Crew {crew.Id}" : crew.Name, crew.Emblem, crew.Color, crew.Color2, world.CrewMembers(crew).Count())
                : null,
            Land(world, company),
            company.ResearchQueue.ToList(),
            company.KeepResearching,
            world.ResearchOpen(company),
            world.WantsNear(company).Select(t => new WantNearDto(t.Target, world.NameFor(t.Owner, company), t.Name, t.Want.Resource,
                Math.Round(t.Want.Left), Math.Floor(t.Holds), t.Makes, t.Want.Order == null ? Math.Ceiling(t.Want.Left * world.BasePrice(t.Want.Resource) * GameWorld.OrderPrice) : null)).ToList(),
            world.OpenWants(company).Select(t => new OwnWantDto(t.Target, t.Name, t.Want.Resource, t.Want.Amount, Math.Round(t.Want.Got, 1),
                Math.Round(t.Want.Incoming.Values.Sum() + (t.Want.Order?.Started != null ? t.Want.Order.Rate : 0), 2), t.Want.PipeTiles?.Count,
                t.Want.Order != null ? $"Ordered: the hub brings the rest {world.OrderStartText(company, t.Want, company)}" : null,
                t.Want.Order == null ? Math.Ceiling(t.Want.Left * world.BasePrice(t.Want.Resource) * GameWorld.OrderPrice) : null)).ToList(),
            EraWonderLine(world, company),
            PipesLine(world, company));
    }

    /// <summary>
    /// What your gold can do (after run 4: 0.2% of the gold earned was spent, and the five offers of gold in the group
    /// chat had no door): the shared goal first, then a want gold can fill, then "Grow everything that pays"
    /// </summary>
    public static GoldUsesDto GoldUses(GameWorld world, Company me)
    {
        var hour = Math.Round(60 * Math.Max(GameWorld.MinMeasuredIncome, world.MeasuredIncome(me)));
        GoldLineDto? shared = null;
        if (me.District == GameWorld.Capital && world.EraWonder is { Finished: false } wonder && !GameWorld.Unsized(wonder))
        {
            var name = world.WonderDefinition(wonder).Name;
            var best = wonder.Required.Keys
                .Select(t => (Good: t, Offer: world.WonderOrderOffer(t)))
                .Where(t => t.Offer != null)
                .OrderByDescending(t => t.Offer!.Value.Cost)
                .FirstOrDefault();
            if (best.Offer is { } offer)
            {
                var good = world.GoodsWord(best.Good);
                var who = world.WonderMakers(best.Good) is { Count: > 0 } makers ? $"half comes by pipe from {And(MakerNames(makers.Take(2), me))}" : $"nobody here makes {good}";
                var day = Math.Min(offer.Cost, 24 * hour);
                shared = new GoldLineDto($"{name} wants {offer.Units:N0} {good} you can order: {who}", "wonder", day < offer.Cost ? day : null, best.Good, AllCost: offer.Cost);
            }
        }
        GoldLineDto? want = null;
        if (world.WantsNear(me).Where(t => t.Want.Order == null && !t.Makes && t.Holds < 1)
                .Select(t => (t.Owner, t.Target, t.Name, t.Want, Cost: world.OrderCost(me, t.Target)))
                .Where(t => t.Cost is { } cost && cost <= me.Gold)
                .OrderBy(t => t.Owner.CrewId != null && t.Owner.CrewId == me.CrewId ? 0 : 1)
                .FirstOrDefault() is { Owner: not null } friend)
        {
            var name = world.NameFor(friend.Owner, me);
            want = new GoldLineDto($"{world.PossessiveFor(friend.Owner, me)} {friend.Name} waits for {friend.Want.Left:N0} {world.GoodsWord(friend.Want.Resource)}", "want", friend.Cost, friend.Want.Resource, friend.Target, name);
        }
        else if (world.OpenWants(me).Where(t => t.Want.Order == null && t.Want.Left > 1e-6)
                     .Select(t => (t.Target, t.Name, t.Want, Cost: world.OrderCost(me, t.Target)))
                     .FirstOrDefault(t => t.Cost is { } cost && cost <= me.Gold) is { Name: not null } own)
        {
            want = new GoldLineDto($"Your {own.Name} waits for {own.Want.Left:N0} {world.GoodsWord(own.Want.Resource)}", "want", own.Cost, own.Want.Resource, own.Target);
        }
        var plan = world.PlanGrowth(me);
        var grow = plan.Parts.Count == 0 ? null
            : new GrowPlanDto(plan.Parts.Select(t => new GrowPartDto(t.Building, t.Text, Math.Round(t.Cost), Math.Round(t.Gain, 1))).ToList(), plan.Cost, plan.Gain,
                me.LastGrowth is { } last && world.Now - last.At <= GameWorld.UndoFor ? last.At + GameWorld.UndoFor : null);
        if (grow == null && me.LastGrowth is { } recent && world.Now - recent.At <= GameWorld.UndoFor)
        {
            grow = new GrowPlanDto([], 0, 0, recent.At + GameWorld.UndoFor);
        }
        return new GoldUsesDto(shared, want, grow, hour);
    }

    /// <summary>
    /// The pipe ladder while a pipe is full (after run 4: the way up showed only in a stuck building's own panel, and all
    /// 1,087 pipe tiles stayed grey): the building that would gain most, its door in words, and the ladder
    /// </summary>
    private static PipesLineDto? PipesLine(GameWorld world, Company me)
    {
        var full = world.BuildingsOf(me.Id).Where(t => world.IdleReasonOf(t).Cause == IdleCause.PipeFull).ToList();
        if (full.Count == 0)
        {
            return null;
        }
        var best = full.OrderByDescending(t => world.Content.Building(t.Type).Out.Sum(o => o.Value * world.BasePrice(o.Key)) * world.Content.Progression.OutputMultiplier(t.Level)).First();
        var name = world.Content.Building(best.Type).Name;
        var door = world.PipeDoorFor(me, best, preview: false);
        var text = door != null ? $"Your {name}'s pipe is full · {door.Text}" : $"Your {name}'s pipe is full";
        var ladder = string.Join(" · ", world.Content.PipeTiers.Where(t => t.Era <= Math.Max(2, world.Era)).Select(t =>
            t.Research == null || world.HasResearch(me, t.Research) ? (t.Era <= world.Era ? t.Name : $"{t.Name} with Era {t.Era}")
            : world.Content.Research.GetValueOrDefault(t.Research) is { Grant: > 0 } grant ? $"{t.Name} at Level {grant.Grant}{(t.Era > world.Era ? $" in Era {t.Era}" : "")}"
            : $"{t.Name} by research"));
        return new PipesLineDto(full.Count > 1 ? $"{text} ({full.Count} buildings at their pipe's limit)" : text, best.Id, full.Count, ladder);
    }

    /// <summary>
    /// The Era Wonder on the goal tracker (after run 3, step 5: Cleo found the Great Dynamo only by hunting, and "Era 1
    /// ended without me knowing"): its stage's share, and your goods going in or what it wants and who in the Capital makes it
    /// </summary>
    private static ProjectWeekDto? EraWonderLine(GameWorld world, Company me)
    {
        // It is the Capital's want: elsewhere the District Wonder is the shared goal
        if (!world.WonderBuysEnabled || me.District != GameWorld.Capital || world.EraWonder is not { Finished: false } wonder)
        {
            return null;
        }
        string Name(string resource) => world.GoodsWord(resource);
        var definition = world.WonderDefinition(wonder);
        if (GameWorld.Unsized(wonder))
        {
            return new ProjectWeekDto($"🏛 {definition.Name} opens at midnight, sized to six hours of what the Capital makes · {Span(world.EraDeadline - world.Now)} left", 0);
        }
        var target = wonder.Required.Sum(t => t.Value * world.BasePrice(t.Key));
        var share = target <= 0 ? 0 : wonder.Required.Sum(t => Math.Min(t.Value, wonder.Delivered.GetValueOrDefault(t.Key)) * world.BasePrice(t.Key)) / target;
        var head = $"🏛 {definition.Name} {(definition.Stages.Count > 1 ? $"stage {wonder.Stage + 1} " : "")}{Math.Floor(share * 100):0}% · {Span(world.EraDeadline - world.Now)} left";
        var mine = wonder.Incoming.GetValueOrDefault(me.Id)?.Where(t => t.Value > 1e-9).Select(t => Name(t.Key)).ToList() ?? [];
        if (mine.Count > 0)
        {
            return new ProjectWeekDto($"{head} · your {And(mine)} {(mine.Count == 1 ? "goes" : "go")} in", share);
        }
        var lacking = wonder.Required.Where(t => wonder.Delivered.GetValueOrDefault(t.Key) < t.Value - 1e-6).Select(t => t.Key).ToList();
        var wants = lacking.Select(t => world.WonderMakers(t) is { Count: > 0 } who ? $"{world.GoodsWord(t)} ({And(MakerNames(who.Take(2), me))})" : world.GoodsWord(t)).ToList();
        // Nobody here makes them: gold may order all of it (after run 4, the Cathedral's steel and concrete)
        var orderable = lacking.Where(t => world.WonderMakers(t).Count == 0 && world.WonderOrderOffer(t) != null).ToList();
        var tail = orderable.Count == 0 ? ""
            : orderable.Count == lacking.Count ? " · nobody here makes them: order them in Hub › Wonders"
            : $" · nobody here makes {And(orderable.Select(world.GoodsWord).ToList())}: order in Hub › Wonders";
        return new ProjectWeekDto($"{head} · wants {And(wants)}{tail}", share);
    }

    /// <summary>Dealt land (step 5): the goods your seat holds, and who in your station holds the others (null on old land)</summary>
    private static LandDto? Land(GameWorld world, Company me)
    {
        if (!world.Generator.DealtLand || world.EstateOf(me) is not { } home)
        {
            return null;
        }
        var seats = GameWorld.StationSeats(home.Station).Select(seat =>
        {
            var owner = world.Estate(seat.District, seat.Slot)?.OwnerId is { } id ? world.Company(id) : null;
            var goods = world.Generator.LandOf(seat.District, seat.Slot);
            // "Makes" only when someone does (run 2: Mia was told Jo makes coal; Jo never built a Coal Pit)
            var makes = owner == null ? [] : world.BuildingsOf(owner.Id).Where(t => !t.UnderConstruction)
                .SelectMany(t => world.Content.Building(t.Type).Out.Keys).Where(goods.Contains).Distinct().ToList();
            return new LandSeatDto(seat.Slot, owner == null ? null : world.NameFor(owner, me), owner?.Id == me.Id, goods, makes);
        }).ToList();
        return new LandDto(world.Generator.LandOf(home.District, home.Slot), seats);
    }

    /// <summary>Today's hub orders, each with what the trading posts sell of its good now (they fill from it)</summary>
    private static List<DailyOrderDto> Orders(GameWorld world, Company company)
    {
        var selling = world.SellingPerMinute(company);
        return company.DailyOrders.Select(t => new DailyOrderDto(t.Id, t.Resource, t.Amount, Math.Floor(t.Delivered), t.RewardGold, t.RewardXp, t.RewardReputation, t.ExpiresAt, t.Urgent,
            Math.Round(selling.GetValueOrDefault(t.Resource), 2), Math.Round(t.Premium))).ToList();
    }

    /// <summary>Who can make a good, as a line says it: the others first, then "you"</summary>
    private static List<string> MakerNames(IEnumerable<Company> makers, Company me) =>
        makers.OrderBy(t => t.Id == me.Id ? 1 : 0).Select(t => t.Id == me.Id ? "you" : t.Name).ToList();

    /// <summary>"Ben", "Ben and Dev", "Ben, Dev and you"</summary>
    private static string And(IReadOnlyList<string> items) =>
        items.Count <= 1 ? items.FirstOrDefault() ?? "" : $"{string.Join(", ", items.Take(items.Count - 1))} and {items[^1]}";

    private static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

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
            return new ResearchStateDto(tech.Id, state, world.ResearchCost(company, tech), state == "locked" ? world.ResearchBlocker(company, tech) : null);
        }).ToList();

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
        world.TeleportReceiver(building)?.Id,
        GameWorld.IsSite(building) ? SiteShare(world, building) : null);

    /// <summary>How much of a scaffold's materials is in, by worth</summary>
    private static double SiteShare(GameWorld world, Building site)
    {
        var wants = world.WantsOf(site);
        var target = wants.Sum(t => t.Amount * world.BasePrice(t.Resource));
        return target <= 0 ? 1 : Math.Round(wants.Sum(t => Math.Min(t.Got, t.Amount) * world.BasePrice(t.Resource)) / target, 3);
    }

    /// <summary>A scaffold as its panel shows it: each want, who brought what and what comes now, and the chain when nobody near makes it</summary>
    private static SiteDto? Site(GameWorld world, Building site, Company? viewer)
    {
        if (!GameWorld.IsSite(site) || world.Company(site.Owner) is not { } owner)
        {
            return null;
        }
        var wants = world.WantsOf(site);
        // What players near make (your station and crew, who played this week): the same rule as the placing toast and
        // Next steps, so the panel never says less than they did
        var near = world.NearPlayers(owner).SelectMany(c => world.BuildingsOf(c.Id).Where(b => !b.UnderConstruction))
            .SelectMany(b => world.Content.Building(b.Type).Out.Keys).ToHashSet();
        var list = wants.Select(t => new SiteWantDto(t.Resource, t.Amount, Math.Round(t.Got, 1), Math.Round(t.Incoming.Values.Sum() + (t.Order?.Started != null ? t.Order.Rate : 0), 2),
            t.From.Where(f => f.Value >= 0.5).OrderByDescending(f => f.Value)
                .Select(f => f.Key == owner.Id ? $"you: {f.Value:N0}"
                    : f.Key == GameWorld.HubSource ? $"bought: {f.Value:N0}"
                    : $"{(world.Company(f.Key) is { } who && viewer != null ? world.NameFor(who, viewer) : "a neighbour")}: {f.Value:N0}").ToList(),
            near.Contains(t.Resource) ? null : SiteChain(world, t.Resource, viewer ?? owner).Text,
            t.Order != null ? $"Ordered: the hub brings the rest {world.OrderStartText(owner, t, viewer ?? owner)}" : null,
            t.Order == null && t.Left > 1e-6 ? Math.Ceiling(t.Left * world.BasePrice(t.Resource) * GameWorld.OrderPrice) : null,
            near.Contains(t.Resource) ? null : SiteChain(world, t.Resource, viewer ?? owner).Building)).ToList();
        var refund = wants.Sum(t => t.Paid) + (owner.Purchases.FirstOrDefault(p => p.Building == site.Id)?.Cost ?? world.BuildCost(owner, world.Content.Building(site.Type)));
        return new SiteDto(list, Math.Round(refund));
    }

    /// <summary>
    /// "nobody near makes bricks yet: a Brickworks (1,400 g) makes them from coal and stone: …", and the building's type
    /// when the viewer may build it (for Show me)
    /// </summary>
    private static (string Text, string? Building) SiteChain(GameWorld world, string good, Company viewer) =>
        world.ChainFor(viewer, good) is { } chain
            ? ($"nobody near makes {world.GoodsWord(good)} yet · {world.ChainText(chain, good, viewer)}", chain.Locked == null ? chain.Building.Id : null)
            : ($"nobody near makes {world.GoodsWord(good)} yet", null);

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
                owner is { } ? Camel(world.ActivityOf(owner)) : null,
                profile.Goods));
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
                    .Where(t => t.District == district && world.HasResearch(t, "brand"))
                    .OrderByDescending(world.NetWorth)
                    .Take(Billboards.Length)
                    .ToList();
                for (var i = 0; i < brands.Count; i++)
                {
                    structures.Add(new HubStructureDto("billboard", Billboards[i].X, Billboards[i].Y, 2, brands[i].Name, brands[i].Color));
                }
            }
        }
        return structures;
    }

    // ---------------------------------------------------------------- Society

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
        // The Capital builds the Era Wonder: its District Wonder would be a second, greyed goal beside it (run 4: Ben took
        // the Observatory's "Deliver from storage" for the Cathedral's missing Order button)
        var district = company.District == GameWorld.Capital && world.WonderBuysEnabled ? null : world.DistrictWonder(company.District);
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
        var era = wonder.District == null && company.District == GameWorld.Capital;
        WonderNeedDto Need(string good, double required)
        {
            var offer = era ? world.WonderOrderOffer(good) : null;
            var orders = wonder.Orders.Where(o => o.Resource == good).ToList();
            var orderers = orders.GroupBy(o => o.By)
                .Select(g => world.Company(g.Key) is { } c ? $"{(c.Id == company.Id ? "you" : world.NameFor(c, company))} {g.Sum(o => o.Units):N0}" : null)
                .OfType<string>()
                .ToList();
            var makers = era ? world.WonderMakers(good) : [];
            // Nobody makes it: say which building would, and where its inputs come from (the friend with gold gets a role)
            // What neither arrived nor is ordered: once it's all ordered, the line has nothing left to say
            var lacks = wonder.Delivered.GetValueOrDefault(good) + orders.Sum(o => o.Units - o.Shipped) < required - 1;
            var chain = era && lacks && makers.Count == 0 ? world.ChainFor(company, good) : null;
            return new WonderNeedDto(good, required, wonder.Delivered.GetValueOrDefault(good), materials.GetValueOrDefault(good),
                offer?.Units, offer?.Cost,
                Math.Round(wonder.Incoming.Values.Sum(g => g.GetValueOrDefault(good)), 2),
                Math.Floor(orders.Sum(o => o.Units - o.Shipped)),
                Math.Floor(orders.Sum(o => o.Units)),
                orderers,
                era ? world.WonderOrderCap(good) : 0,
                makers.Select(c => c.Id == company.Id ? "you" : world.NameFor(c, company)).ToList(),
                chain == null ? null : world.ChainText(chain, good, company),
                chain is { Locked: null } ? chain.Building.Id : null);
        }
        double Hours(Company c) => 60 * Math.Max(GameWorld.MinMeasuredIncome, world.MeasuredIncome(c));
        return new WonderDto(
            wonder.Type,
            definition.Name,
            definition.Description,
            wonder.District == null,
            wonder.Stage,
            definition.Stages.Count,
            wonder.Finished,
            wonder.Required.Select(t => Need(t.Key, t.Value)).ToList(),
            total,
            wonder.Contributions.GetValueOrDefault(company.Id),
            Camel(tier),
            share,
            wonder.Contributions.OrderByDescending(t => t.Value).Take(5)
                .Select(t => world.Company(t.Key) is { } c ? new WonderBuilderDto(c.Id, c.Name, c.Color, t.Value, Math.Round(t.Value / Hours(c), 1)) : null)
                .OfType<WonderBuilderDto>()
                .ToList(),
            GameWorld.Unsized(wonder),
            Math.Round(Hours(company)));
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
        var canMentor = owner.Id != viewer.Id && viewer.MentorId == null && viewer.Level < GameWorld.MenteeMaxLevel
            && owner.Level >= GameWorld.MentorLevel && world.Mentees(owner.Id) < GameWorld.MaxMentees;
        return new EstateVisitDto(
            owner.Id,
            owner.Name,
            owner.Color,
            owner.Level,
            owner.Id == viewer.Id,
            owner.Likes,
            world.LikedToday(viewer.Id, owner.Id),
            world.CrewOf(owner) is { } crew ? (crew.NameHidden ? $"Crew {crew.Id}" : crew.Name) : null,
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
            owner.Id == viewer.Id ? null
                : world.WantsNear(viewer).Where(t => t.Owner.Id == owner.Id)
                    .Select(t => new WantNearDto(t.Target, world.NameFor(t.Owner, viewer), t.Name, t.Want.Resource, Math.Round(t.Want.Left), Math.Floor(t.Holds), t.Makes,
                        t.Want.Order == null ? world.OrderCost(viewer, t.Target) : null))
                    .ToList());
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

    public static BuildingDetailsDto BuildingDetails(GameWorld world, Building building, Company? viewer = null)
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

        // What crosses its Yard gate counts too: goods the Yard brings it, and goods it sends to the Yard
        foreach (var flow in network?.GateFlows.Where(t => t.Building == building.Id) ?? [])
        {
            if (flow.Import)
            {
                received[flow.Resource] = received.GetValueOrDefault(flow.Resource) + flow.Delivered;
            }
            else
            {
                sent[flow.Resource] = sent.GetValueOrDefault(flow.Resource) + flow.Sent;
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
        // Max buys the levels its pipes can feed (F8)
        var max = upgradable && owner != null ? world.SmartMax(owner, building) : null;
        var affordable = max?.Levels ?? 0;
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
            definition.Kind == BuildingKind.Storage ? Keep(world, building, definition, network) : null,
            max != null && max.AffordableLevels > max.Levels
                ? new MaxMoreDto(max.AffordableLevels, Math.Round(max.AffordableCost), Math.Round(max.AffordableShare, 3), max.Resource, Math.Round(max.Have, 2), Math.Round(max.Need, 2), max.Output)
                : null,
            Status(world, building),
            viewer != null && viewer.Id == building.Owner && definition.Kind is not (BuildingKind.Hq or BuildingKind.Gate or BuildingKind.Decoration) ? Trade(world, building, viewer) : null,
            upgradable && owner != null && viewer?.Id == owner.Id ? NullIfEmpty(world.UpgradePreview(owner, building, 1).Text) : null,
            upgradable && owner != null && viewer?.Id == owner.Id && max != null && max.Levels > 1 ? NullIfEmpty(world.UpgradePreview(owner, building, max.Levels).Text) : null,
            owner != null && viewer?.Id == owner.Id && world.Undoable(owner, building) is { } undo ? undo.Refund : null,
            owner != null && viewer?.Id == owner.Id && world.Undoable(owner, building) is { } until ? until.Purchase.At + GameWorld.UndoFor : null,
            Site(world, building, viewer),
            owner != null && viewer?.Id == owner.Id && world.PipeDoorFor(owner, building) is { } door ? Door(world, door) : null);
    }

    private static PipeDoorDto Door(GameWorld world, PipeDoor door) => new(
        Camel(door.Kind),
        door.Text,
        world.DoorLabel(door),
        Math.Round(door.Gold),
        Math.Round(door.LevelCost),
        door.ToLevel,
        Math.Round(door.Gain, 1),
        door.Materials.Select(t => new ResourceAmountDto(t.Key, t.Value, t.Value)).ToList(),
        door.Short.Select(t => new ResourceAmountDto(t.Key, t.Value, t.Value)).ToList(),
        door.Source,
        door.OrderCost);

    private static string? NullIfEmpty(string text) => text.Length == 0 ? null : text;

    /// <summary>Why a building's good reaches nobody at its Yard gate; null when some of it reaches the gate</summary>
    private static string? NothingMoves(GameWorld world, Building building, string good)
    {
        if (world.IdleReasonOf(building) is { Cause: not IdleCause.None } idle && (world.NetworkOf(building.Id)?.Solution.Rate(building.Id) ?? 0) < 0.005)
        {
            return world.IdleLine(building) is { } line ? $"it doesn't run ({char.ToLowerInvariant(line[0])}{line[1..]})" : "it doesn't run";
        }
        if (world.NetworkOf(building.Id) is not { } network || !network.GateLinks.Any(t => t.Building == building.Id && (t.Resources == null || t.Resources.Contains(good))))
        {
            return "its pipes don't reach your Yard gate";
        }
        if (network.GateFlows.Any(t => !t.Import && t.Building == building.Id && t.Resource == good && t.Sent > 1e-9))
        {
            return null;
        }
        return network.Solution.Flows.Any(t => t.From == building.Id && t.Resource == good && t.Kind == FlowKind.Sale && t.Sent > 1e-9)
            ? "the Yard takes none of it now: it sells at your trading post"
            : "your own buildings use all of it";
    }

    /// <summary>
    /// A building's trade with its neighbours, for its owner only (step 4): who takes its goods and who sends it its
    /// inputs (both ends named), who could use its goods, and the pipe to the gate when its pipes don't reach it
    /// </summary>
    private static BuildingTradeDto? Trade(GameWorld world, Building building, Company me)
    {
        var trade = world.TradeOf(building);
        var definition = world.Content.Building(building.Type);
        string Name(Building? b) => b == null ? "" : world.Content.Building(b.Type).Name;
        TradeEndDto End(TradeEnd t) => new(t.Resource, t.Partner.Id, world.NameFor(t.Partner, me), t.PartnerBuilding == null ? null : Name(t.PartnerBuilding),
            t.PartnerBuilding?.Id, Math.Round(t.PerMinute, 3), Math.Round(t.Price, 3));
        if (trade.Takers.Count == 0 && trade.Suppliers.Count == 0 && trade.Bids.Count == 0)
        {
            return null;
        }
        var pipe = trade.ReachesGate ? null : world.PlanGatePipe(building);
        var runsOn = trade.Suppliers.OrderByDescending(t => t.PerMinute).FirstOrDefault();
        return new BuildingTradeDto(
            trade.Takers.Select(End).ToList(),
            trade.Suppliers.Select(End).ToList(),
            trade.Bids.Take(3).Select(t => new BidDto(t.Resource, t.Buyer.Id, world.NameFor(t.Buyer, me), t.Building == null ? null : Name(t.Building), t.Building?.Id, Math.Round(t.PerMinute, 2))).ToList(),
            trade.ReachesGate,
            pipe == null ? null : new BuildingFixDto("layPipe", $"Lay the pipe to your gate · {(pipe.NewTiles == 1 ? "1 tile" : $"{pipe.NewTiles} tiles")} · {pipe.Cost:0} g",
                pipe.Tiles.SelectMany(t => new[] { t.X, t.Y }).ToArray(), pipe.NewTiles, Math.Round(pipe.Cost, 2), building.Id, null, "your gate"),
            runsOn == null ? null : $"runs on {world.PossessiveFor(runsOn.Partner, me)} {world.GoodsWord(runsOn.Resource)}");
    }

    /// <summary>Loady's step as the player sees it, with the pipe its Show me lays (F2, F3)</summary>
    private static TutorialDto Tutorial(GameWorld world, Company company, TutorialStep step)
    {
        var pipe = world.TutorialPipe(company, step);
        return new TutorialDto(company.TutorialStep, world.Content.Tutorial.Count, step.Id, world.TutorialText(step), world.TutorialProgress(company, step), step.Amount, step.Gold, world.TutorialHint(company, step),
            world.TutorialWaitLevel(company, step), world.TutorialTip(company, step), step.Chapter, world.TutorialChapter(step).Index, world.TutorialChapter(step).Count,
            step.PhoneText, pipe?.Tiles.SelectMany(t => new[] { t.X, t.Y }).ToArray(), Math.Round(pipe?.Cost ?? 0, 2),
            pipe?.Target is { } target ? world.Content.Building(target.Type).Name : null, company.TutorialStepSince);
    }

    /// <summary>The panel head (F2): the cause, the pill, the reach line and the one fix, for the owner's eyes (others see the pill)</summary>
    private static BuildingStatusDto Status(GameWorld world, Building building)
    {
        var status = world.IdleStatusOf(building);
        var fix = status.Fix is { } f
            ? new BuildingFixDto(Camel(f.Kind), f.Label, (f.Tiles ?? f.Pipe?.Tiles)?.SelectMany(t => new[] { t.X, t.Y }).ToArray() ?? [], f.Pipe?.NewTiles ?? 0, Math.Round(f.Cost, 2),
                f.Building, f.BuildingType, f.Pipe?.Target is { } target ? world.Content.Building(target.Type).Name : null, f.OwnNeed, f.Tier)
            : null;
        return new BuildingStatusDto(Camel(status.Reason.Cause), status.Pill, status.Reach, fix);
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

    /// <summary>Why a building can't run (fully), besides its status (IdleReason says the cause, the pill and the fix)</summary>
    private static List<string> Issues(GameWorld world, Building building, FlowNode? node, Network? network) => [];

    /// <summary>A network as a company sees it (its own pump stations and speed)</summary>
    public static NextStepDto NextStep(AdvisorStep step, GameWorld? world = null, Company? me = null) => new(
        JsonNamingPolicy.CamelCase.ConvertName(step.Kind.ToString()),
        step.Title,
        // A step names every cost (run 3: the Big Factory's step said 12k gold; it also takes 100 bricks, ana d21 #286),
        // and when the Storages lack them, where they come from (run 4)
        world != null && me != null && step.Action == AdvisorAction.Build && step.BuildingType is { } type && world.MaterialsLine(me, world.Content.Building(type)) is { } line
            ? $"{step.Reason.TrimEnd()} {line.Text}"
            : step.Reason,
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
        // Made, used, sold, hub imports, traded, stored, from storage, lost; then what the Yard brings and takes at the gate
        double[] Row(string resource) => resources.TryGetValue(resource, out var row) ? row : resources[resource] = new double[10];
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
                    // What leaves the hub is paid, what the pipes lose on the way too (as the solver counts it)
                    Money(to.Owner)[3] += flow.Sent * flow.PricePerUnit;
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

        // The Yard gate: what neighbours' goods bring to its buildings, and what its buildings sell to neighbours (run 2:
        // Ana's card said "sand 15 short" while her Glassworks ran on Ben's sand)
        // Its money too (after run 4: Cleo's panel said "+2,547/min · hub +2,598" with nothing for the 51 between, and
        // her top bar a third number): what comes in at the gate is paid in the network's gold, what goes out the Yard
        // pays apart, and the network at an estate's gate shows it
        foreach (var gate in network.GateFlows)
        {
            if (world.Building(gate.Building) is not { } building || (viewer != null && building.Owner != viewer))
            {
                continue;
            }
            var row = Row(gate.Resource);
            if (gate.Import)
            {
                row[1] += gate.Delivered;
                row[8] += gate.Delivered;
                // The buyer pays for all that leaves the Yard: what its pipes lose on the way too
                Money(building.Owner)[2] += gate.Sent * gate.Price;
            }
            else
            {
                row[0] += gate.Sent;
                row[9] += gate.Delivered;
                row[7] += gate.Sent - gate.Delivered;
            }
        }
        var yardGold = new Dictionary<long, double>();
        foreach (var owner in network.Owners(world).Select(world.Company).OfType<Company>())
        {
            if (world.EstateOf(owner) is { } estate && world.GateNetwork(estate) == network)
            {
                yardGold[owner.Id] = world.TradeIncomeOf(owner.Id);
                Money(owner.Id)[1] += yardGold[owner.Id];
            }
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
            .Select(t => new NetworkOwnerDto(t.Id, t.Name, t.Color,
                (solution.GoldPerMinute.TryGetValue(t.Id, out var g) ? g : 0) + yardGold.GetValueOrDefault(t.Id),
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
                .Concat(needed.Where(t => !resources.ContainsKey(t.Key)).Select(t => (Resource: t.Key, Row: new double[10], Needed: t.Value)))
                .OrderBy(t => world.Content.Resources.TryGetValue(t.Resource, out var r) ? r.Tier : 99).ThenBy(t => t.Resource)
                .Select(t => new NetworkResourceDto(t.Resource, t.Row[0], t.Row[1], t.Row[2], t.Row[3], t.Row[4], t.Row[5], t.Row[6], t.Row[7], t.Needed, t.Row[8], t.Row[9]))
                .ToList(),
            bottlenecks,
            network.Buildings.Select(world.Building).OfType<Building>()
                .Where(t => (viewer == null || t.Owner == viewer) && world.IdleReasonOf(t).Cause == IdleCause.Stuck)
                .Select(t => new StuckBuildingDto(t.Id, t.Type, $"{world.Content.Building(t.Type).Name} is stuck: {world.GoodsWord(world.IdleReasonOf(t).Resource!)} can't leave"))
                .ToList());
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

    /// <summary>
    /// A plot as the map's card shows it (1.4 §6): its land, and who lives there; free, closed (free, but its Yard is
    /// closed: you ask first), taken or yours
    /// </summary>
    public static SeatDto Seat(GameWorld world, EstateKey seat, Company? me, string? code = null)
    {
        var estate = world.Estate(seat.District, seat.Slot);
        var origin = WorldLayout.EstateOrigin(seat.District, seat.Slot);
        var profile = world.Generator.Estate(seat.District, seat.Slot);
        var owner = estate?.OwnerId is { } id ? world.Company(id) : null;
        var state = owner != null && owner.Id == me?.Id ? "you"
            : owner is { } ? "taken"
            : world.SeatFree(seat, me, code) ? "free"
            : "closed";
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
            owner is { } && owner.Id != me?.Id ? ActivityBucket(world, owner) : null,
            profile.Goods);
    }

    /// <summary>A Yard on the map: its crew's name and banner, whether it's open, its plots and its Plan board</summary>
    public static StationDto Station(GameWorld world, StationRef station, Company? me, string? code = null)
    {
        var yard = WorldLayout.Yard(station);
        var seats = GameWorld.StationSeats(station).Select(t => Seat(world, t, me, code)).ToList();
        var crew = world.CrewAt(station);
        return new StationDto(
            station.District.X,
            station.District.Y,
            station.Index,
            crew is { NameHidden: false } ? crew.Name : WorldNames.Station(station),
            crew?.Emblem,
            crew?.Color,
            crew?.Color2,
            world.YardOpen(station),
            seats.Count(t => t.State == "free"),
            seats.Count(t => t.Activity == "lively"),
            seats,
            yard.X0,
            yard.Y0,
            yard.X1,
            yard.Y1,
            world.PlanBoard(station).Makes.Order().ToList(),
            world.PlanBoard(station).Needs.Order().ToList());
    }

    private static SeatRefDto Ref(EstateKey seat) => new(seat.District.X, seat.District.Y, seat.Slot);

    /// <summary>A town's Yards for the map's start (the one Pick for me chooses when none is given)</summary>
    public static StationsDto Stations(GameWorld world, Company? me, DistrictCoord? district, string? code = null)
    {
        var pick = world.PickForMe(me);
        var shown = district ?? pick.District;
        return new StationsDto(
            shown.X,
            shown.Y,
            WorldNames.District(shown),
            WorldLayout.Stations(shown).Select(t => Station(world, t, me, code)).ToList(),
            Ref(pick),
            world.OpenDistricts.OrderBy(t => Math.Abs(t.X) + Math.Abs(t.Y)).Select(t => new DistrictRefDto(t.X, t.Y, WorldNames.District(t))).ToList());
    }

    /// <summary>
    /// What a "Play next to me" link opens (1.4 §6): who sent it and their Yard; whether it lets you in although the Yard
    /// is closed; a free name for a newcomer's company. A link that ran out opens nothing.
    /// </summary>
    public static NextToMeDto NextToMe(GameWorld world, string code)
    {
        if (world.NextToMeOf(code) is not { } friend || world.EstateOf(friend) is not { } estate)
        {
            return new NextToMeDto(false, null, null, null, false, null);
        }
        var opens = !friend.NextToMeUsed && !world.YardOpen(estate.Station);
        return new NextToMeDto(true, friend.Name, friend.Color, Station(world, estate.Station, null, code), opens, world.FreeCompanyName());
    }

    /// <summary>The Yard's crew as its residents see it (1.4 §7): its name and banner, its residents, the requests, your link</summary>
    public static CrewViewDto Crew(GameWorld world, Company me)
    {
        if (world.CrewOf(me) is not { } crew)
        {
            return new CrewViewDto(null, GameWorld.CrewEmblems);
        }
        CrewMemberDto Member(Company t) =>
            new(t.Id, t.Name, t.Color, t.Level, t.Id == me.Id ? null : ActivityBucket(world, t), t.Slot, t.Id == me.Id);
        var requests = crew.Requests
            .Select(r => world.Company(r.Company) is { } c ? new CrewRequestDto(c.Id, c.Name, c.Color, c.Level, r.Slot, r.At + GameWorld.RequestWaits) : null)
            .OfType<CrewRequestDto>()
            .ToList();
        return new CrewViewDto(new CrewDto(
            crew.Id,
            crew.NameHidden ? $"Crew {crew.Id}" : crew.Name,
            crew.Emblem,
            crew.Color,
            crew.Color2,
            crew.Named,
            crew.Open,
            world.CrewMembers(crew).Select(Member).ToList(),
            requests,
            me.NextToMeCode is { } link && me.NextToMeUntil > world.Now && !me.NextToMeUsed ? GameWorld.ShowCode(link) : null,
            me.NextToMeUntil,
            world.LastInYard(me)), GameWorld.CrewEmblems);
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
            var left = Span(world.OfferLapsesAt(link) - world.Now);
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
        var resting = partner != null && world.ActivityOf(partner) != Activity.Active ? $" {name} is resting; this link keeps delivering." : "";
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
            link.State == LinkState.Offered ? world.OfferLapsesAt(link) : null,
            tone,
            line,
            Flat(link.Route));
    }

    /// <summary>Your links, who a link would suit, and whom you could link with</summary>
    public static LinksDto Links(GameWorld world, Company me)
    {
        var home = world.EstateOf(me) is { } estate ? estate.Station : (StationRef?)null;
        var partners = world.Companies
            .Where(t => t.Id != me.Id && world.EstateOf(t) is { } e && e.Station != home)
            // Your crew first, and said so (after run 3, row 12: Ben was offered as a link with no word he was Ana's friend)
            .OrderBy(t => me.CrewId != null && t.CrewId == me.CrewId ? 0 : 1).ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(t => new LinkPartnerDto(t.Id, t.Name, t.Color, t.Level, WorldNames.Station(world.EstateOf(t)!.Station), me.CrewId != null && t.CrewId == me.CrewId))
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
            .Select(t => new YardTradeDto(t.Partner, world.NameFor(world.Company(t.Partner)!, me), t.Resource, t.PerMinute, t.Selling, world.YardPrice(me, world.Company(t.Partner)!, t.Resource, t.Selling)))
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
                            // Someone here lacks it: the Yard's next round brings it (after run 4: "Nobody here needs
                            // energy yet" above a Brickworks that lacked 31 energy)
                            ? residents.FirstOrDefault(t => t.Company.Id != me.Id && t.Connected && t.Lacks.GetValueOrDefault(spare) > 0.01) is { } needs
                                ? $"{world.NameFor(needs.Company, me)} lacks {Name(spare)} you have spare: the Yard's next round brings it to them, in a few minutes."
                            : HubTakes(world, me, spare)
                                    ? $"Nobody here needs {Name(spare)} yet: the hub takes it."
                                    : $"Nobody here needs {Name(spare)} yet: pipe it to your Trading Post and the hub buys it."
                            : "Nothing to trade yet: what you make your own buildings use.";
        return new YardDto(
            world.CrewAt(station) is { NameHidden: false } crew ? crew.Name : WorldNames.Station(station),
            yard.X0,
            yard.Y0,
            yard.X1,
            yard.Y1,
            estate.YardGate.X,
            estate.YardGate.Y,
            connected,
            residents.Select(t => new YardResidentDto(t.Company.Id, world.NameFor(t.Company, me), t.Company.Color, t.Company.Level, t.Slot, t.Connected,
                Camel(world.ActivityOf(t.Company)), Rates(t.Spare), Rates(t.Lacks), me.TradeBlocked.Contains(t.Company.Id),
                t.Company.PinMakes.Order().ToList(), t.Company.PinNeeds.Order().ToList())).ToList(),
            yours,
            world.YardTotals(station).Select(t => new ResourceRateDto(t.Key, t.Value)).OrderByDescending(t => t.PerMinute).ToList(),
            line,
            Discoveries(world, station),
            world.BuildingsOf(me.Id).Where(t => !t.UnderConstruction).SelectMany(t => world.Content.Building(t.Type).Out.Keys).Distinct()
                .Where(t => t != GameContent.ResearchPoints)
                .SelectMany(t => world.Bids(me, t, residents))
                .OrderByDescending(t => t.PerMinute * world.BasePrice(t.Resource))
                .Take(4)
                .Select(t => new BidDto(t.Resource, t.Buyer.Id, world.NameFor(t.Buyer, me), t.Building == null ? null : world.Content.Building(t.Building.Type).Name, t.Building?.Id, Math.Round(t.PerMinute, 2)))
                .ToList(),
            world.TradesThisWeek(me).Take(8)
                .Select(t => new WeekTradeDto(t.Partner.Id, world.NameFor(t.Partner, me), t.Resource, Math.Round(t.Given), Math.Round(t.Received)))
                .ToList(),
            YardIncomeLine(world, me, yours));
    }

    /// <summary>
    /// "Here the Yard pays you 40 g/min for what you send, of your +105/min: the hub pays the rest" (the Yard pays for
    /// what neighbours take, and the hub's price for what nobody took) (after run 4: the trades alone, against the top
    /// bar, left Cleo asking where the rest came from)
    /// </summary>
    private static string? YardIncomeLine(GameWorld world, Company me, IReadOnlyList<YardTradeDto> yours)
    {
        // What the Yard pays you, as the top bar counts it, less what you pay neighbours (that is in your networks' gold)
        var paid = world.TradeIncomeOf(me.Id);
        var neighbours = paid - yours.Where(t => !t.Selling).Sum(t => t.PerMinute * t.Price);
        if (Math.Abs(neighbours) < 0.5)
        {
            return null;
        }
        var income = world.GoldPerMinute().GetValueOrDefault(me.Id);
        var parts = new List<string>();
        parts.Add(neighbours >= 0 ? $"the Yard pays you {Amount(neighbours)} g/min for what you send" : $"you pay neighbours {Amount(-neighbours)} g/min");
        var rest = income - neighbours;
        var line = $"Here {string.Join(" ", parts)}, of your {(income >= 0 ? "+" : "−")}{Amount(Math.Abs(income))}/min";
        return rest >= 0.5 ? $"{line}: the hub pays the rest, for what nobody here takes." : $"{line}.";
    }

    /// <summary>The Discoveries of the last week: where they are, their markers</summary>
    private static IReadOnlyList<DiscoveryDto> Discoveries(GameWorld world, StationRef station)
    {
        var home = station;
        return world.Discoveries.Where(t => world.Now - t.FoundAt < TimeSpan.FromDays(7)).OrderByDescending(t => t.FoundAt).Select(t =>
        {
            var (stations, top) = world.DiscoveryMarks(t);
            return new DiscoveryDto(t.Resource, t.X, t.Y, WorldNames.District(WorldLayout.DistrictOf(t.X, t.Y)), t.FoundAt + GameWorld.DiscoveryMarking,
                stations.Select(WorldNames.Station).ToList(),
                top.Select(c => world.EstateOf(c) is { } e ? $"{c.Name} ({WorldNames.Station(e.Station)})" : c.Name).ToList(),
                stations.Contains(home));
        }).ToList();
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
            .Select(t => new ExchangeOrderDto(t.Id, t.Resource, t.Selling, t.Amount, t.Price, t.CreatedAt, true, t.Global, t.Placed, t.CreatedAt + GameWorld.OrderLifetime))
            .ToList();
        var shipments = world.Shipments
            .Where(t => t.To == me.Id || t.From == me.Id)
            .OrderBy(t => t.ArrivesAt)
            .Select(t => new ShipmentDto(t.Id, t.Resource, t.Amount, t.ArrivesAt, t.To == me.Id, world.Company(t.To == me.Id ? t.From : t.To)?.Name ?? ""))
            .ToList();
        return new ExchangeDto(world.HasBuilding(me.Id, BuildingKind.ExchangeOffice), books, mine, shipments, GameWorld.ExchangeFee, global,
            world.Content.ResourceList.Where(t => t.Era <= world.Era + 1).ToDictionary(t => t.Id, t => world.PlayerPrice(t.Id)),
            world.Content.ResourceList.Select(t => (t.Id, Stored: world.StoredOf(me.Id, t.Id))).Where(t => t.Stored >= 0.5).ToDictionary(t => t.Id, t => Math.Floor(t.Stored)));
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
