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

    public static CompanyDto Company(GameWorld world, Company company, double goldPerMinute)
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
            company.Level,
            progression.EarnedForLevel(company.Level),
            company.Level >= progression.MaxCompanyLevel ? progression.EarnedForLevel(company.Level) : progression.EarnedForLevel(company.Level + 1),
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
            new ResearchLinesDto(world.HasLab(company), company.Researched.OrderBy(t => t).ToList(), company.CurrentResearch, company.ResearchEndsAt, company.ResearchQueue.ToList(),
                new Dictionary<string, int>(company.TrackTiers), company.CurrentTrack, company.TrackEndsAt, company.TrackQueue.ToList(), company.KeepResearching, world.ResearchOpen(company),
                company.ResearchHeld.ToDictionary(t => t.Key, t => Math.Round(t.Value, 1)),
                company.CurrentResearch is { } tech && world.Content.Research.GetValueOrDefault(tech) is { } item ? world.ResearchMinutes(company, item.Minutes) : null,
                company.CurrentTrack is { } track && world.TrackTier(company, track) < world.Content.TrackRules.Tiers.Count
                    ? world.ResearchMinutes(company, world.Content.TrackRules.Tiers[world.TrackTier(company, track)].Minutes) : null),
            Orders(world, company),
            company.NoTrade.OrderBy(t => t).ToList(),
            company.MentorId,
            company.Likes,
            world.Calendar(company).Select(t => Event(world, t)).ToList(),
            (int)world.Horizon(company).TotalHours,
            world.TownEra(company.District),
            world.YourEra(company),
            company.Titles.OrderBy(t => t).ToList(),
            company.Achievements.Where(t => t.StartsWith("collection:")).Select(t => t["collection:".Length..]).ToList(),
            world.OutpostsOf(company.Id).Select(t => Outpost(world, t, company.Id)).ToList(),
            company.LegacyLevel,
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
            world.Alerts(company).Select(t => new AlertDto(t.Text, t.Building, t.Kind,
                t.Kind == "damage" ? Math.Round(world.BuildingsOf(company.Id).Where(b => b.Damaged).Sum(world.RepairCost)) : null)).ToList(),
            world.Now,
            new EarnedDto(Math.Max(0, goldPerMinute),
                company.EarnedLog.ToDictionary(t => t.Key, t => (IReadOnlyDictionary<string, double>)t.Value.ToDictionary(i => i.Key, i => Math.Round(i.Value)))),
            world.CrewOf(company) is { } crew
                ? new CrewBadgeDto(crew.Id, crew.NameHidden ? WorldNames.Station(crew.Yard) : crew.Name, crew.Emblem, crew.Color, crew.Color2, world.CrewMembers(crew).Count(), crew.Requests.Count)
                : null,
            Land(world, company),
            WantsNear(world, company),
            // Levels bought together wait for a step's goods each: one line per good of a building, as its panel shows it
            // (playtest: a Solar Panel bought up to 13 had three planks lines; each one's Buy the rest bought all three)
            world.OpenWants(company).GroupBy(t => (t.Target, t.Want.Resource)).Select(g => new OwnWantDto(g.Key.Target, g.First().Name, g.Key.Resource,
                g.Sum(t => t.Want.Amount), Math.Round(g.Sum(t => t.Want.Got), 1), Math.Round(g.Sum(t => t.Want.Incoming.Values.Sum()), 2),
                g.First().Want.PipeTiles?.Count, null, world.OrderCost(company, g.Key.Target, g.Key.Resource),
                g.Max(t => t.Want.ToLevel) is > 0 and var level ? level : null,
                g.First().Want.PipeTiles?.SelectMany(p => new[] { p.X, p.Y }).ToList(), g.First().Want.PipeTiles != null ? g.First().Want.PipeTier : null)).ToList(),
            EraWonderLine(world, company),
            PipesLine(world, company),
            company.Prices.OrderBy(t => t.Key, StringComparer.Ordinal).Select(t => new PriceDto(t.Key, t.Value.Value, t.Value.Pending, t.Value.Back, t.Value.PendingAt,
                t.Key.StartsWith("for:", StringComparison.Ordinal) && long.TryParse(t.Key[4..], out var id) ? world.Company(id)?.Name : null)).ToList(),
            company.Give.ToDictionary(t => t.Key, t => Camel(t.Value)),
            company.WantsFromMarket,
            estate == null ? null : new OutskirtsDto(estate.OutskirtsParcels, world.NextOutskirtsCost(company), world.OutskirtsBlocker(company)),
            Workshops(world, company),
            company.Explored.ToList(),
            Math.Round(world.OwnGoodsIntoWants(company)),
            world.CostFactor(company),
            world.NextYardRound,
            Math.Round(world.MarketForWants(company).Sum(t => t.PerMinute * t.Price), 1));
    }

    /// <summary>Building in the Yard (1.4 §7): your workshop tiles, and the crew's Depot</summary>
    private static WorkshopsDto? Workshops(GameWorld world, Company company)
    {
        if (world.EstateOf(company) is not { } estate)
        {
            return null;
        }
        var crew = world.CrewOf(company);
        var depot = world.DepotOf(estate.Station);
        return new WorkshopsDto(
            world.WorkshopTilesUsed(company),
            GameWorld.WorkshopTiles,
            GameWorld.WorkshopLevel,
            depot == null || crew == null
                ? null
                : new DepotDto(depot.Id, depot.X, depot.Y, world.DepotCapacity(estate.Station), world.DepotHolds(crew),
                    crew.Depot.SelectMany(t => t.Value.Where(g => g.Value >= 1).Select(g => new DepotGoodDto(g.Key, Math.Floor(g.Value), t.Key, world.Company(t.Key)?.Name ?? "", t.Key == company.Id)))
                        .OrderByDescending(t => t.Yours).ThenBy(t => t.Resource, StringComparer.Ordinal).ToList()));
    }

    /// <summary>
    /// What your gold can do (after run 4: 0.2% of the gold earned was spent, and the five offers of gold in the group
    /// chat had no door): the shared goal first, then a want gold can fill, then "Grow everything that pays"
    /// </summary>
    public static GoldUsesDto GoldUses(GameWorld world, Company me)
    {
        var hour = Math.Round(60 * Math.Max(GameWorld.MinMeasuredIncome, world.MeasuredIncome(me)));
        GoldLineDto? shared = null;
        if (world.WonderOf(me.District) is { FullAt: null } wonder && world.BuyAndGiveCost(me) is > 0 and var all)
        {
            var name = world.WonderDefinition(wonder).Name;
            var day = Math.Min(all, 24 * hour);
            shared = new GoldLineDto($"{name}'s stage {wonder.Stage + 1} wants goods: buy and give them, and your town gets nearer to Era {world.TownEra(me.District) + 1}", "wonder",
                day < all ? day : null, AllCost: all);
        }
        GoldLineDto? want = null;
        if (world.WantsNear(me).Where(t => !t.Makes && t.Holds < 1)
                .Select(t => (t.Owner, t.Target, t.Name, t.Want, Cost: world.OrderCost(me, t.Target, t.Want.Resource)))
                .Where(t => t.Cost is { } cost && cost <= me.Gold)
                .OrderBy(t => t.Owner.CrewId != null && t.Owner.CrewId == me.CrewId ? 0 : 1)
                .FirstOrDefault() is { Owner: not null } friend)
        {
            var name = world.NameFor(friend.Owner, me);
            want = new GoldLineDto($"{world.PossessiveFor(friend.Owner, me)} {friend.Name} waits for {friend.Want.Left:N0} {world.GoodsWord(friend.Want.Resource)}", "want", friend.Cost, friend.Want.Resource, friend.Target, name);
        }
        else if (world.OpenWants(me).Where(t => t.Want.Left > 1e-6)
                     .Select(t => (t.Target, t.Name, t.Want, Cost: world.OrderCost(me, t.Target, t.Want.Resource)))
                     .FirstOrDefault(t => t.Cost is { } cost && cost <= me.Gold) is { Name: not null } own)
        {
            want = new GoldLineDto($"Your {own.Name} waits for {own.Want.Left:N0} {world.GoodsWord(own.Want.Resource)}", "want", own.Cost, own.Want.Resource, own.Target);
        }
        var plan = world.PlanGrowth(me);
        var grow = plan.Parts.Count == 0 && plan.Saving == null ? null
            : new GrowPlanDto(plan.Parts.Select(t => new GrowPartDto(t.Building, t.Text, Math.Round(t.Cost), Math.Round(t.Gain, 1))).ToList(), plan.Cost, plan.Gain,
                me.LastGrowth is { } last && world.Now - last.At <= GameWorld.UndoFor ? last.At + GameWorld.UndoFor : null, plan.Saving, plan.Goods,
                LevelGoods(world, me, GrowGoods(world, plan)));
        if (grow == null && me.LastGrowth is { } recent && world.Now - recent.At <= GameWorld.UndoFor)
        {
            grow = new GrowPlanDto([], 0, 0, recent.At + GameWorld.UndoFor);
        }
        return new GoldUsesDto(shared, want, grow, hour, Income(world, me));
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
        var era = world.YourEra(me);
        var ladder = string.Join(" · ", world.Content.PipeTiers.Where(t => t.Era <= Math.Max(2, era + 1)).Select(t =>
            t.Research == null || world.HasResearch(me, t.Research) ? (t.Era <= era ? t.Name : $"{t.Name} with Era {t.Era}")
            : world.Content.Research.GetValueOrDefault(t.Research) is { Grant: > 0 } grant ? $"{t.Name} at Level {grant.Grant}{(t.Era > era ? $" in Era {t.Era}" : "")}"
            : $"{t.Name} by research{(t.Era > era ? $" in Era {t.Era}" : "")}"));
        return new PipesLineDto(full.Count > 1 ? $"{text} ({full.Count} buildings at their pipe's limit)" : text, best.Id, full.Count, ladder);
    }

    /// <summary>
    /// The town's Era Wonder on the goal tracker (after run 3, step 5: Cleo found the Great Dynamo only by hunting, and "Era
    /// 1 ended without me knowing"): its stage's share, and your goods going in or what it wants and who in the town makes it
    /// </summary>
    private static ProjectWeekDto? EraWonderLine(GameWorld world, Company me)
    {
        if (!world.WonderBuysEnabled || world.WonderOf(me.District) is not { } wonder)
        {
            return null;
        }
        var definition = world.WonderDefinition(wonder);
        if (wonder.FullAt is { } full)
        {
            return new ProjectWeekDto($"🏛 {definition.Name} stage {wonder.Stage + 1} has its goods · it stands in {Span(full + GameWorld.StageBuildTime - world.Now)}", 1);
        }
        var target = wonder.Required.Sum(t => t.Value * world.BasePrice(t.Key));
        var share = target <= 0 ? 0 : wonder.Required.Sum(t => Math.Min(t.Value, wonder.Delivered.GetValueOrDefault(t.Key)) * world.BasePrice(t.Key)) / target;
        var head = $"🏛 {definition.Name} stage {wonder.Stage + 1} of {definition.Stages.Count} {Math.Floor(share * 100):0}%";
        var mine = wonder.Incoming.GetValueOrDefault(me.Id)?.Where(t => t.Value > 1e-9).Select(t => world.GoodsWord(t.Key)).ToList() ?? [];
        if (mine.Count > 0)
        {
            return new ProjectWeekDto($"{head} · your {And(mine)} {(mine.Count == 1 ? "goes" : "go")} in", share);
        }
        var lacking = wonder.Required.Keys.Where(t => world.WonderLacks(wonder, t) > 1e-6).ToList();
        var wants = lacking.Select(t => world.WonderMakers(me.District, t) is { Count: > 0 } who ? $"{world.GoodsWord(t)} ({And(MakerNames(who.Take(2), me))})" : world.GoodsWord(t)).ToList();
        return new ProjectWeekDto($"{head} · wants {And(wants)}: give them in Hub › Town & Wonder", share);
    }

    /// <summary>Your plot's land (1.4 §5): what it has and lacks, its strength, and every plot of your Yard with theirs</summary>
    private static LandDto? Land(GameWorld world, Company me)
    {
        if (world.EstateOf(me) is not { } home)
        {
            return null;
        }
        var seats = GameWorld.StationSeats(home.Station).Select(seat =>
        {
            var owner = world.Estate(seat.District, seat.Slot)?.OwnerId is { } id ? world.Company(id) : null;
            var profile = world.Generator.Estate(seat.District, seat.Slot);
            var goods = profile.Goods;
            // "Makes" only when someone does (run 2: Mia was told Jo makes coal; Jo never built a Coal Pit)
            var makes = owner == null ? [] : world.BuildingsOf(owner.Id).Where(t => !t.UnderConstruction)
                .SelectMany(t => world.Content.Building(t.Type).Out.Keys).Where(goods.Contains).Distinct().ToList();
            return new LandSeatDto(seat.Slot, owner == null ? null : world.NameFor(owner, me), owner?.Id == me.Id, goods, profile.Lacks, profile.Strength, makes);
        }).ToList();
        var mine = world.Generator.Estate(home.District, home.Slot);
        return new LandDto(mine.Goods, mine.Lacks, mine.Strength, seats);
    }

    /// <summary>Today's hub orders, each with what the trading posts sell of its good now (they fill from it), what the Storages hold of it and the hub's price</summary>
    private static List<DailyOrderDto> Orders(GameWorld world, Company company)
    {
        var selling = world.SellingPerMinute(company);
        return company.DailyOrders.Select(t => new DailyOrderDto(t.Id, t.Resource, t.Amount, Math.Floor(t.Delivered), t.RewardGold, t.ExpiresAt, t.Urgent,
            Math.Round(selling.GetValueOrDefault(t.Resource), 2), Math.Round(t.Premium), Math.Floor(world.StoredOf(company.Id, t.Resource)),
            Math.Round(world.HubBuyPrice(company, t.Resource), 4))).ToList();
    }

    /// <summary>Who can make a good, as a line says it: the others first, then "you"</summary>
    private static List<string> MakerNames(IEnumerable<Company> makers, Company me) =>
        makers.OrderBy(t => t.Id == me.Id ? 1 : 0).Select(t => t.Id == me.Id ? "you" : t.Name).ToList();

    /// <summary>"Ben", "Ben and Dev", "Ben, Dev and you"</summary>
    private static string And(IReadOnlyList<string> items) =>
        items.Count <= 1 ? items.FirstOrDefault() ?? "" : $"{string.Join(", ", items.Take(items.Count - 1))} and {items[^1]}";

    private static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

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
        var resources = world.MarketGoods(district)
            .Select(t => new MarketResourceDto(
                t.Id,
                t.Price,
                world.HubBuyPrice(company, district, t.Id),
                world.HubSellPrice(district, t.Id),
                market.SoldRate.TryGetValue(t.Id, out var sold) ? sold : 0,
                world.HubOrder(company, district, t.Id),
                world.PlayerPrice(t.Id),
                company.NoTrade.Contains(t.Id),
                company.NoBuy.Contains(t.Id)))
            .ToList();
        return new MarketDto(district.X, district.Y, WorldNames.District(district), resources, GameWorld.NextMarketUpdate(world.Now));
    }

    public static List<ResearchStateDto> Research(GameWorld world, Company company) =>
        world.Content.ResearchList.Select(tech =>
        {
            var state = company.Researched.Contains(tech.Id) ? "done"
                : company.CurrentResearch == tech.Id ? "researching"
                : world.ResearchBlocker(company, tech, evenBusy: true) is null ? "available"
                : "locked";
            return new ResearchStateDto(tech.Id, state, world.ResearchCost(company, tech), state == "locked" ? world.ResearchBlocker(company, tech, evenBusy: true) : null,
                Math.Round(world.ResearchMinutes(company, tech.Minutes), 1));
        }).ToList();

    /// <summary>Every track as the research panel shows it (1.4 §12)</summary>
    public static List<TrackStateDto> Tracks(GameWorld world, Company company) =>
        world.Content.TrackList.Select(track =>
        {
            var next = world.NextTier(company, track);
            var cost = next is { } tier ? world.TierCost(company, track, tier) : default;
            var effect = next is { } t ? world.Content.TrackRules.Tiers[t - 1] : null;
            return new TrackStateDto(track.Id, world.TrackTier(company, track.Id), next, cost.Gold, cost.Good, cost.Units, cost.FromStorages, cost.Bought, cost.Total,
                effect == null ? 0 : Math.Round(world.ResearchMinutes(company, effect.Minutes)), effect == null ? null : Camel(effect.Effect), effect?.Amount ?? 0,
                Math.Round(world.TierGain(company, track), 1), track.Goods.Select(g => world.TrackHubOrderFactor(company, g)).DefaultIfEmpty(1).Max(),
                world.TierBlocker(company, track, evenBusy: true));
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
        var refund = wants.Sum(t => t.Paid) + (owner.Purchases.FirstOrDefault(p => p.Building == site.Id)?.Cost ?? world.BuildCost(owner, world.Content.Building(site.Type)));
        return new SiteDto(WantViews(world, owner, wants, viewer), Math.Round(refund));
    }

    /// <summary>Your neighbours' wants (all of them, or one neighbour's): one line per good of a building, its level steps together</summary>
    private static List<WantNearDto> WantsNear(GameWorld world, Company viewer, Company? of = null) =>
        world.WantsNear(viewer).Where(t => of == null || t.Owner.Id == of.Id)
            .GroupBy(t => (t.Owner.Id, t.Target, t.Want.Resource))
            .Select(g => new WantNearDto(g.Key.Target, world.NameFor(g.First().Owner, viewer), g.First().Name, g.Key.Resource, Math.Round(g.Sum(t => t.Want.Left)),
                Math.Floor(g.First().Holds), g.First().Makes, world.OrderCost(viewer, g.Key.Target, g.Key.Resource)))
            .ToList();

    /// <summary>Where a company's gold a minute comes from, line by line, and what isn't in it</summary>
    private static IncomeDto Income(GameWorld world, Company company)
    {
        var lines = world.IncomeBreakdown(company);
        return new IncomeDto(
            lines.Select(t => new IncomeLineDto(t.Kind, t.Resource, t.Who, Math.Round(t.Units, 1), Math.Round(t.Gold, 1),
                t.Kind == "hub" ? Math.Round(world.HubOrder(company, t.Resource), 1) : null)).ToList(),
            Math.Round(lines.Sum(t => t.Gold), 1),
            Math.Round(company.DailyOrders.Sum(t => t.Premium)),
            Math.Round(world.OwnGoodsIntoWants(company)),
            world.MarketForWants(company).Select(t => new WantImportDto(t.Resource, Math.Round(t.PerMinute, 1), Math.Round(t.Price, 2), Math.Round(t.Left, 1))).ToList());
    }

    /// <summary>
    /// Some levels' goods as the buttons show them (the owner, 2026-10-31: "it should show 1100g, 100 wood, 3838 iron
    /// ingot"), the dearest first, with what the market would charge for all of them; null: none
    /// </summary>
    public static LevelGoodsDto? LevelGoods(GameWorld world, Company owner, IReadOnlyDictionary<string, double> goods) =>
        goods.Count == 0 ? null : new LevelGoodsDto(
            goods.OrderByDescending(t => t.Value * world.BasePrice(t.Key)).Select(t => new ResourceAmountDto(t.Key, Math.Round(t.Value, 1), Math.Round(t.Value, 1))).ToList(),
            Math.Ceiling(goods.Sum(t => t.Value * world.WantMarketPrice(owner, t.Key))));

    /// <summary>A level's goods still coming (1.4 §9): the wants, and what buying the rest costs</summary>
    private static LevelWaitDto? LevelWait(GameWorld world, Building building, Company? viewer)
    {
        if (!world.WaitsForLevel(building) || world.Company(building.Owner) is not { } owner)
        {
            return null;
        }
        return new LevelWaitDto(world.BoughtLevel(building), WantViews(world, owner, world.WantsOf(building), viewer), world.OrderCost(viewer ?? owner, building.Id));
    }

    /// <summary>Wants as a panel shows them: who brought what, what comes now, what the rest costs, the chain when nobody near makes it</summary>
    private static List<SiteWantDto> WantViews(GameWorld world, Company owner, List<CompanyAsk> wants, Company? viewer)
    {
        // What players near make (your station and crew, who played this week): the same rule as the placing toast and
        // Next steps, so the panel never says less than they did
        var near = world.NearPlayers(owner).SelectMany(c => world.BuildingsOf(c.Id).Where(b => !b.UnderConstruction))
            .SelectMany(b => world.Content.Building(b.Type).Out.Keys).ToHashSet();
        // Levels bought together wait for a step's goods each: one line per good for all of them
        var merged = wants.GroupBy(t => t.Resource).Select(g => (Resource: g.Key, Amount: g.Sum(t => t.Amount), Got: g.Sum(t => t.Got),
            Incoming: g.Sum(t => t.Incoming.Values.Sum()), Left: g.Sum(t => t.Left),
            From: g.SelectMany(t => t.From).GroupBy(f => f.Key).ToDictionary(f => f.Key, f => f.Sum(v => v.Value))));
        return merged.Select(t => new SiteWantDto(t.Resource, t.Amount, Math.Round(t.Got, 1), Math.Round(t.Incoming, 2),
            t.From.Where(f => f.Value >= 0.5).OrderByDescending(f => f.Value)
                .Select(f => f.Key == owner.Id ? $"you: {f.Value:N0}"
                    : f.Key == GameWorld.HubSource ? $"bought: {f.Value:N0}"
                    : $"{(world.Company(f.Key) is { } who && viewer != null ? world.NameFor(who, viewer) : "a neighbour")}: {f.Value:N0}").ToList(),
            near.Contains(t.Resource) ? null : SiteChain(world, t.Resource, viewer ?? owner).Text,
            null,
            t.Left > 1e-6 ? Math.Ceiling(t.Left * world.WantMarketPrice(owner, t.Resource)) : null,
            near.Contains(t.Resource) ? null : SiteChain(world, t.Resource, viewer ?? owner).Building)).ToList();
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
    public static OutpostDto Outpost(GameWorld world, Outpost outpost, long? viewer)
    {
        var owner = world.Company(outpost.Owner);
        var mine = viewer == outpost.Owner;
        return new OutpostDto(outpost.Id, outpost.Owner, owner?.Name ?? "", owner?.Color ?? "#888888", outpost.X, outpost.Y, GameWorld.OutpostSize, mine ? outpost.Price : null);
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
        return new OutpostCheckDto(error, origin.X, origin.Y, world.OutpostPrice(company), GameWorld.OutpostSize, deposit, tiles.Count);
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
                profile.Goods,
                profile.Lacks,
                profile.Strength,
                estate?.OutskirtsParcels ?? 0));
        }
        return new DistrictDto(
            district.X,
            district.Y,
            WorldLayout.DistrictOriginX(district.X),
            WorldLayout.DistrictOriginY(district.Y),
            WorldNames.District(district),
            estates,
            HubStructures(world, district),
            WorldLayout.Stations(district).Select(t => world.CrewAt(t) is { } crew
                ? new DistrictYardDto(t.Index, crew.NameHidden ? WorldNames.Station(t) : crew.Name, crew.Emblem, crew.Color, crew.Color2, crew.Open,
                    world.Stalls(t).Select(s => new StallDto(s.Slot, s.Sells, s.CrewPrice, s.Wants)).ToList(),
                    world.MonumentsOf(crew).Where(m => m.Kind != null && m.Stages > 0).Select(m => new YardMonumentDto(m.Era, m.Kind!, m.Stages)).ToList())
                : new DistrictYardDto(t.Index, WorldNames.Station(t), null, null, null, true, [])).ToList(),
            world.TownEra(district),
            world.TownAt(district).LaunchedAt != null,
            world.TownAt(district).Missions,
            world.TrunkLines.Where(t => t.From == district)
                .Select(t => new MapLineDto(t.Id, t.Route.SelectMany(p => new[] { p.X, p.Y }).ToList(), t.Tier, world.Company(t.Owner)?.Color ?? "#888888")).ToList());
    }

    /// <summary>Your town's trunk lines (1.4 §20), where one could run, the tiers you may build in, your exports</summary>
    public static LinesDto Lines(GameWorld world, Company company)
    {
        var tiers = world.LineTiers(company).ToList();
        var best = tiers.DefaultIfEmpty(4).Max();
        var lines = world.TrunkLines.Where(t => t.From == company.District || t.To == company.District).Select(t =>
        {
            var carrier = world.Company(t.Owner);
            var other = t.From == company.District ? t.To : t.From;
            return new TrunkLineDto(t.Id, carrier?.Name ?? "", carrier?.Color ?? "#888888", t.Owner == company.Id, WorldNames.District(company.District), WorldNames.District(other),
                other.X, other.Y, t.Tier, t.Length, world.LineEfficiency(t), world.LineThroughput(t), Math.Round(t.Carried), t.PublicFee, t.CrewFee,
                world.LineFee(t, company), t.Owner == company.Id ? Math.Round(t.Earned) : 0, t.Route.SelectMany(p => new[] { p.X, p.Y }).ToList(),
                // The carrier's special fees: every player of the two towns (theirs or none)
                t.Owner != company.Id ? [] : world.Companies.Where(c => c.Id != company.Id && (c.District == t.From || c.District == t.To))
                    .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(c => new LinePlayerDto(c.Id, world.NameFor(c, company), c.Color, t.SpecialFees.TryGetValue(c.Id, out var fee) ? fee : null)).ToList());
        }).ToList();
        var options = world.LineNeighbours(company.District).Where(t => world.LineBetween(company.District, t) == null).Select(t =>
        {
            var plan = world.PlanLine(company.Id, t, best);
            return new LineOptionDto(t.X, t.Y, WorldNames.District(t), world.TownEra(t), best, plan.Length, plan.Gold, plan.Error);
        }).ToList();
        return new LinesDto(lines, options, tiers, company.Exports);
    }

    /// <summary>Where the statue square's park stands (hub-local): nine statues take its place, 2 tiles apart</summary>
    private static readonly (int X, int Y) StatueSquare = (5, 4);

    private static List<HubStructureDto> HubStructures(GameWorld world, DistrictCoord district)
    {
        var structures = new List<HubStructureDto>();
        foreach (var structure in world.Generator.HubStructures(district))
        {
            // The town's Era Wonder at its site (1.4 §13)
            if (structure.Type == "eraWonderSite" && world.WonderOf(district) is { } wonder)
            {
                if (GameWorld.IsMission(wonder) && world.Content.EraWonders.LastOrDefault() is { } elevator)
                {
                    // After the Launch the Space Elevator stands finished, with the town's stars
                    var stars = world.TownAt(district).Missions;
                    structures.Add(new HubStructureDto(structure.Type, structure.X, structure.Y, structure.Size, stars > 0 ? $"{elevator.Name} · {stars} ★" : elevator.Name,
                        Level: elevator.Stages.Count, Variant: elevator.Id, Stages: elevator.Stages.Count));
                    continue;
                }
                var definition = world.WonderDefinition(wonder);
                structures.Add(new HubStructureDto(structure.Type, structure.X, structure.Y, structure.Size, definition.Name, Level: wonder.Stage, Variant: wonder.Type, Stages: definition.Stages.Count));
                continue;
            }
            // The statue square: the park gives way to the Diamond builders' statues, the most Diamonds first
            if (structure.Type == "park" && structure.X == StatueSquare.X && structure.Y == StatueSquare.Y && world.TownAt(district).Statues.Count > 0)
            {
                var statues = world.TownAt(district).Statues.OrderByDescending(t => t.Diamonds).ThenBy(t => t.At).Take(9).ToList();
                for (var i = 0; i < statues.Count; i++)
                {
                    var statue = statues[i];
                    structures.Add(new HubStructureDto("statue", StatueSquare.X - 1 + 2 * (i % 3), StatueSquare.Y - 1 + 2 * (i / 3), 1,
                        statue.Diamonds > 1 ? $"{statue.Name} · {statue.Diamonds} ◆" : statue.Name, world.Company(statue.Company)?.Color,
                        statue.Diamonds, (1 + statue.Company % 3).ToString()));
                }
                continue;
            }
            structures.Add(new HubStructureDto(structure.Type, structure.X, structure.Y, structure.Size));
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
                .Select(t => new AchievementDto(t.Id, t.Name, t.Description, t.Amount, world.StatValue(company, t.Stat, partners), company.Achievements.Contains(t.Id), t.Hours, t.Gold, t.Title))
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
                new StatDto("Daily orders", world.StatValue(company, "orders")),
                new StatDto("Track tiers researched", company.TrackTiers.Values.Sum()),
                new StatDto("Era tree items researched", company.Researched.Count),
                new StatDto("Given to Wonders (g)", world.StatValue(company, "wonder")),
                new StatDto("Paid into Monuments (g)", world.StatValue(company, "monumentGold")),
                new StatDto("Likes", company.Likes),
                new StatDto("Gold per tile per hour", world.TileEfficiency(company)),
            ],
            company.Stats.Where(t => t.Key.StartsWith("produced:")).OrderByDescending(t => t.Value).Take(8)
                .Select(t => new StatDto(t.Key["produced:".Length..], t.Value)).ToList());
    }

    /// <summary>Your town and Yard (1.4 §11, §13): the town's Era and Wonder, your Yard's Monuments</summary>
    public static TownDto Town(GameWorld world, Company company)
    {
        var town = world.TownAt(company.District);
        var crew = world.CrewOf(company);
        var sites = crew == null ? [] : world.MonumentsOf(crew);
        return new TownDto(
            WorldNames.District(company.District),
            town.Era,
            world.YourEra(company),
            town.EraStartedAt,
            town.PartyUntil > world.Now ? town.PartyUntil : null,
            town.Wonder == null ? null : TownWonder(world, town.Wonder, company),
            sites.Select(t => world.MonumentKind(t.Kind) is var kind ? new MonumentDto(t.Era, t.Kind, kind?.Name, t.Stages, Math.Round(t.Paid), world.MonumentStageCost(t),
                world.MonumentCapped(t), kind?.Description, Math.Round(t.PaidBy.GetValueOrDefault(company.Id))) : null).OfType<MonumentDto>().ToList(),
            world.Content.Monuments.Kinds.Select(t => new MonumentKindDto(t.Id, t.Name, t.Description, sites.Any(s => s.Kind == t.Id))).ToList(),
            world.Content.Monuments.Level,
            Math.Round(60 * world.MeasuredIncome(company)),
            town.LaunchedAt,
            town.Missions);
    }

    private static TownWonderDto TownWonder(GameWorld world, TownWonder wonder, Company company)
    {
        var definition = world.WonderDefinition(wonder);
        var materials = world.Materials(company.Id);
        var (share, tier) = world.StageTier(wonder, company.Id);
        WonderNeedDto Need(string good, double required)
        {
            var makers = world.WonderMakers(company.District, good);
            // Nobody makes it: say which building would, and where its inputs come from (the friend with gold gets a role)
            var chain = world.WonderLacks(wonder, good) > 1 && makers.All(t => t.Id != company.Id) ? world.ChainFor(company, good) : null;
            return new WonderNeedDto(good, required, Math.Floor(wonder.Delivered.GetValueOrDefault(good)), Math.Floor(materials.GetValueOrDefault(good)),
                world.BuyAndGiveCost(company, good),
                Math.Round(wonder.Incoming.Values.Sum(g => g.GetValueOrDefault(good)), 2),
                Camel(company.Give.GetValueOrDefault(good)),
                makers.Select(c => c.Id == company.Id ? "you" : world.NameFor(c, company)).ToList(),
                chain == null ? null : world.ChainText(chain, good, company),
                chain is { Locked: null } ? chain.Building.Id : null);
        }
        var stageWorth = wonder.Required.Sum(t => t.Value * world.BasePrice(t.Key));
        var built = definition.Stages.Take(wonder.Stage).Where(t => t.Perk != WonderPerk.None).Select(t => PerkLine(t.Perk, t.PerkAmount)).ToList();
        var next = wonder.Stage < definition.Stages.Count ? definition.Stages[wonder.Stage] : null;
        return new TownWonderDto(
            wonder.Type,
            definition.Name,
            definition.Description,
            definition.Era,
            wonder.Stage,
            definition.Stages.Count,
            wonder.Required.Select(t => Need(t.Key, t.Value)).ToList(),
            Math.Round(stageWorth),
            Math.Round(definition.Worth * world.TrailFactor(definition)),
            wonder.FullAt + GameWorld.StageBuildTime,
            share,
            Camel(tier),
            Math.Round(wonder.Given.GetValueOrDefault(company.Id)),
            wonder.StageGiven.OrderByDescending(t => t.Value).Take(5)
                .Select(t => world.Company(t.Key) is { } c ? new WonderBuilderDto(c.Id, c.Name, c.Color, Math.Round(t.Value), stageWorth <= 0 ? 0 : t.Value / stageWorth) : null)
                .OfType<WonderBuilderDto>()
                .ToList(),
            world.BuyAndGiveCost(company),
            world.TrailFactor(definition),
            built,
            next == null ? null : next.Perk == WonderPerk.None ? $"Era {definition.Era + 1}" : PerkLine(next.Perk, next.PerkAmount));
    }

    private static string PerkLine(WonderPerk perk, double amount) => perk switch
    {
        WonderPerk.Energy => $"+{amount:P0} energy",
        WonderPerk.HubOrders => $"Hub Orders +{amount:P0}",
        WonderPerk.Factories => $"factories +{amount:P0}",
        WonderPerk.Research => $"research {amount:P0} faster",
        _ => $"+{amount:P0} production",
    };

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
                : WantsNear(world, viewer, owner)
                    .ToList());
    }

    public static NetworkFlowsDto Flows(GameWorld world, Network network)
    {
        var flows = new List<FlowDto>();
        foreach (var flow in network.Solution.Flows)
        {
            var path = new List<int>();
            // The links of its building, not all of the network's (every flow searched them all: a second a minute of
            // a busy server went into it)
            var link = network.Index.LinksOf(flow.From)
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
        // What goes through its Yard gates too, on the pipes to the gate (the audit, 2026-10-31: those pipes looked empty
        // on the map and in the hover): the gate's end has no building (0)
        foreach (var gate in network.GateFlows)
        {
            var link = network.GateLinks.Where(t => t.Building == gate.Building && (t.Resources == null || t.Resources.Contains(gate.Resource)))
                .OrderByDescending(t => t.Efficiency).FirstOrDefault();
            if (link == null)
            {
                continue;
            }
            var tiles = gate.Import ? link.Path.Reverse() : link.Path;
            flows.Add(new FlowDto(gate.Import ? 0 : gate.Building, gate.Import ? gate.Building : 0, gate.Resource, gate.Sent, gate.Delivered,
                gate.Import ? "fromYard" : "toYard", gate.Price, tiles.SelectMany(t => new[] { t.X, t.Y }).ToList()));
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
            // A row of its own (the audit, 2026-10-31: counted in the bars, missing from Where it goes): a seller is paid
            // for what its buyers take, a buyer pays for what leaves the Yard
            flows.Add(new BuildingFlowDto(flow.Import ? "in" : "out", flow.Resource, flow.Import ? flow.Delivered : flow.Sent, 0, "yardGate", "",
                flow.Import ? "fromYard" : "toYard", flow.Price, Math.Round(flow.Sent, 2), Math.Round(flow.Delivered, 2),
                Math.Round((flow.Import ? flow.Sent : flow.Delivered) * flow.Price, 2)));
        }

        BuildingFlowDto Partner(string direction, Flow flow, long partnerId, double amount)
        {
            var partner = world.Building(partnerId);
            var partnerOwner = partner != null ? world.Company(partner.Owner)?.Name ?? "" : "";
            // Paid as the solver pays it (the audit, 2026-10-31: the panel priced what left, the seller is paid for what arrives)
            var gold = flow.Kind switch
            {
                FlowKind.Sale or FlowKind.Trade => flow.Delivered * flow.PricePerUnit,
                FlowKind.Import => flow.Sent * flow.PricePerUnit,
                _ => 0,
            };
            return new BuildingFlowDto(direction, flow.Resource, amount, partnerId, partner?.Type ?? "", partnerOwner, Camel(flow.Kind), flow.PricePerUnit,
                Math.Round(flow.Sent, 2), Math.Round(flow.Delivered, 2), Math.Round(gold, 2));
        }

        var inputs = node?.In.Select(t => new ResourceAmountDto(t.Key, received.TryGetValue(t.Key, out var v) ? v : 0, t.Value)).ToList() ?? [];
        var outputs = node?.Out.Select(t => new ResourceAmountDto(t.Key, sent.TryGetValue(t.Key, out var v) ? v : 0, t.Value)).ToList() ?? [];

        var upgradable = world.CanUpgrade(building);
        // Max buys the levels its pipes can feed (F8)
        var max = upgradable && owner != null ? world.SmartMax(owner, building) : null;
        var affordable = max?.Levels ?? 0;
        var bought = world.BoughtLevel(building);
        var maxLevels = world.Content.Progression.MaxBuildingLevel - bought;
        var (track, tiers) = world.TrackOf(building);

        return new BuildingDetailsDto(
            Building(world, building),
            owner?.Name ?? "",
            rate,
            world.LocationMultiplier(building, definition),
            inputs,
            outputs,
            definition.Kind == BuildingKind.Storage ? building.Contents : null,
            definition.Kind == BuildingKind.Storage ? world.StorageCapacity(building, definition) : null,
            null,
            definition.Kind == BuildingKind.TradingPost ? building.Imports.OrderBy(t => t).ToList() : null,
            definition.Kind is BuildingKind.Valve ? building.Filter.OrderBy(t => t).ToList() : null,
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
            owner != null && viewer?.Id == owner.Id && world.PipeDoorFor(owner, building) is { } door ? Door(world, door) : null,
            LevelWait(world, building, viewer),
            Math.Round(world.CopyPrice(building)),
            upgradable ? world.LevelGoods(building, bought + 1).Select(t => new ResourceAmountDto(t.Key, t.Value, t.Value)).ToList() : null,
            track?.Name,
            tiers,
            world.Sleeps(building),
            PostSales(world, building, owner),
            upgradable && owner != null ? LevelGoods(world, owner, world.StepGoods(building, 1)) : null,
            upgradable && owner != null ? LevelGoods(world, owner, world.StepGoods(building, Math.Min(10, maxLevels))) : null,
            upgradable && owner != null && affordable > 0 ? LevelGoods(world, owner, world.StepGoods(building, affordable)) : null);
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
        door.OrderCost,
        Math.Round(door.Goods));

    private static string? NullIfEmpty(string text) => text.Length == 0 ? null : text;

    /// <summary>What Grow's levels take in goods, by good: a door's levels up to the level it reaches, a Max's levels</summary>
    private static Dictionary<string, double> GrowGoods(GameWorld world, GrowPlan plan)
    {
        var goods = new Dictionary<string, double>();
        foreach (var part in plan.Parts.Where(t => t.Building > 0))
        {
            if (world.Building(part.Building) is not { } building)
            {
                continue;
            }
            var levels = part.Door ? Math.Max(0, part.Levels - world.BoughtLevel(building)) : part.Levels;
            foreach (var (resource, amount) in world.StepGoods(building, levels))
            {
                goods[resource] = goods.GetValueOrDefault(resource) + amount;
            }
        }
        return goods;
    }

    /// <summary>
    /// A trading post's sales by good (playtest: "wouldn't it make sense to show in a bar how much the trading building is
    /// being used?"): what your posts sell a minute against your Hub Order, and what this one gets for it
    /// </summary>
    private static List<PostSaleDto>? PostSales(GameWorld world, Building post, Company? owner)
    {
        if (owner == null || world.Content.Building(post.Type).Kind != BuildingKind.TradingPost)
        {
            return null;
        }
        var flows = world.NetworkOf(post.Id)?.Solution.Flows.Where(t => t.To == post.Id && t.Kind == FlowKind.Sale).ToList() ?? [];
        var selling = world.SellingNow(owner);
        return flows.Select(t => t.Resource).Distinct()
            .Select(t => (Resource: t, Units: flows.Where(f => f.Resource == t).Sum(f => f.Delivered), Gold: flows.Where(f => f.Resource == t).Sum(f => f.Delivered * f.PricePerUnit)))
            .Select(t => new PostSaleDto(t.Resource, Math.Round(selling.GetValueOrDefault(t.Resource), 1), Math.Round(world.HubOrder(owner, t.Resource), 1),
                Math.Round(t.Gold, 1),
                // What a unit is paid now, and the full price (the price up to the Hub Order)
                t.Units > 1e-9 ? Math.Round(t.Gold / t.Units, 2) : 0,
                Math.Round(world.BasePrice(t.Resource) * world.EventPriceFactor(owner.District, t.Resource), 2)))
            .OrderByDescending(t => t.Gold)
            .ToList();
    }

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
            pipe?.Target is { } target ? world.Content.Building(target.Type).Name : null, company.TutorialStepSince, world.TutorialWaitEra(company, step));
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
        // What it holds, what it has a slider for, and every good on its network (all of them can come in)
        var resources = storage.Contents.Keys
            .Concat(network?.Solution.Flows.Select(t => t.Resource) ?? [])
            .Concat(storage.Keeps.Keys)
            .Distinct()
            .ToList();
        var owner = world.Company(storage.Owner);
        var room = world.StorageCapacity(storage, definition);
        return new StorageKeepDto(
            resources.ToDictionary(t => t, t => Math.Round(world.StorageKeep(storage, definition, t), 1)),
            resources.ToDictionary(t => t, t => Math.Round(storage.Keeps.Count > 0 ? storage.Keeps.GetValueOrDefault(t) : storage.Setting * room)),
            owner == null ? null : resources.ToDictionary(t => t, t => Math.Round(world.StorageNeed(owner, t))),
            storage.Keeps.Count > 0);
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
        step.Tier,
        world != null && me != null && step.Action == AdvisorAction.Upgrade && step.Building is { } id && world.Building(id) is { } upgraded && step.Levels > 0
            ? LevelGoods(world, me, world.StepGoods(upgraded, step.Levels))
            : null);

    /// <summary>
    /// What a network sends into its owner's links of a good from the Yard gate it is piped to (the owner, 2026-10-31: the
    /// seller's panel said "sold through the Yard 100", the buyer bought 25: the rest filled the buyer's tank and was lost
    /// on the link's 109 tiles)
    /// </summary>
    private static double LinkSends(GameWorld world, Network network, string resource) =>
        network.Owner is { } owner && world.Company(owner) is { } company && world.EstateOf(company) is { } estate && world.GateNetwork(estate)?.Id == network.Id
            ? world.LinksOf(owner).Where(t => t.Seller == owner && t.Flows && t.Resource == resource).Sum(t => t.Send)
            : 0;

    /// <summary>
    /// Of a good its buildings get less of than they need, what is for buildings that couldn't run on it anyway: another
    /// input missing (the owner, 2026-10-31: coal 5 short while the coal link brought 25: the 5 were a Brickworks' that
    /// had no stone, and the Yard and links bring a building only what it can use). Null: none
    /// </summary>
    private static string? ShortWhy(GameWorld world, Network network, string resource, Func<string, bool> spare)
    {
        var received = new Dictionary<(long, string), double>();
        foreach (var flow in network.Solution.Flows)
        {
            received[(flow.To, flow.Resource)] = received.GetValueOrDefault((flow.To, flow.Resource)) + flow.Delivered;
        }
        foreach (var flow in network.GateFlows.Where(t => t.Import))
        {
            received[(flow.Building, flow.Resource)] = received.GetValueOrDefault((flow.Building, flow.Resource)) + flow.Delivered;
        }
        var blocked = new Dictionary<(string Type, string Missing), (int Count, double Short, bool Any)>();
        // Every other input it lacks, said together: "no energy and no stone"
        foreach (var node in network.Nodes.Where(t => t.Kind == FlowNodeKind.Building && t.Id > 0 && t.MaxRate > 1e-9 && t.In.ContainsKey(resource)))
        {
            var need = node.In[resource] * node.MaxRate;
            var got = received.GetValueOrDefault((node.Id, resource));
            if (got >= need * 0.99 || world.Building(node.Id) is not { } building)
            {
                continue;
            }
            var others = node.In.Where(t => t.Key != resource)
                .Select(t => (t.Key, Share: received.GetValueOrDefault((node.Id, t.Key)) / (t.Value * node.MaxRate)))
                // Only what the network has no more of than it uses: a building that doesn't run takes none of what is there
                .Where(t => t.Share < 0.99 && !spare(t.Key)).OrderBy(t => t.Key, StringComparer.Ordinal).ToList();
            if (others.Count == 0)
            {
                continue;
            }
            var missing = string.Join(" and ", others.Select(t => $"{(t.Share < 0.01 ? "no" : "too little")} {world.GoodsWord(t.Key)}"));
            var key = (building.Type, missing);
            var known = blocked.GetValueOrDefault(key);
            blocked[key] = (known.Count + 1, known.Short + need - got, true);
        }
        if (blocked.Count == 0)
        {
            return null;
        }
        var parts = blocked.OrderByDescending(t => t.Value.Short).Take(3).Select(t =>
        {
            var name = world.Content.Building(t.Key.Type).Name;
            return $"{PerMinute(t.Value.Short)} of it for {(t.Value.Count == 1 ? $"your {name}, which also gets" : $"{t.Value.Count} {name}s, which also get")} {t.Key.Missing}";
        });
        return $"{string.Join("; ", parts)}: the Yard and your links bring a building only what it can use";
    }

    public static NetworkDetailsDto NetworkDetails(GameWorld world, Network network, long? viewer = null)
    {
        var solution = network.Solution;
        var isMachine = new Dictionary<long, bool>();
        bool Machine(long id) => isMachine.TryGetValue(id, out var v) ? v
            : isMachine[id] = world.Building(id) is { } b && world.Content.Building(b.Type).Kind is not (BuildingKind.Storage or BuildingKind.TradingPost);

        var resources = new Dictionary<string, double[]>();
        // Made, used, sold, hub imports, traded, stored, from storage, lost; then what the Yard brings and takes at the gate
        // and the gold its hub sales bring
        double[] Row(string resource) => resources.TryGetValue(resource, out var row) ? row : resources[resource] = new double[11];
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
            if (to != null && Machine(flow.To) && flow.Kind is FlowKind.Internal or FlowKind.Trade or FlowKind.FromStorage or FlowKind.Import)
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
                case FlowKind.Trade when from != null && to != null:
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
            if (Machine(flow.To) && flow.Kind is FlowKind.Internal or FlowKind.Trade or FlowKind.FromStorage or FlowKind.Import)
            {
                row[1] += flow.Delivered;
            }
            switch (flow.Kind)
            {
                case FlowKind.Sale:
                    row[2] += flow.Delivered;
                    row[10] += flow.Delivered * flow.PricePerUnit;
                    break;
                case FlowKind.Import: row[3] += flow.Sent; break;
                case FlowKind.Trade: row[4] += flow.Delivered; break;
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
                // What leaves the Yard for it, what its own pipes lose of it on the way (the audit, 2026-10-31: paid
                // for, never shown as lost), what arrives
                row[1] += gate.Delivered;
                row[8] += gate.Sent;
                row[7] += gate.Sent - gate.Delivered;
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
        // What its buildings could make, and why some make less (the owner, 2026-10-31: the mines made more when a Smelter
        // was upgraded, as they made only what it took)
        var outputs = world.NetworkOutputs(network, viewer);

        var used = new Dictionary<FlowLink, double>(ReferenceEqualityComparer.Instance);
        foreach (var flow in solution.Flows)
        {
            // The links of its building, not all of the network's (every flow searched them all: a second a minute of
            // a busy server went into it)
            var link = network.Index.LinksOf(flow.From)
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
                .Concat(needed.Where(t => !resources.ContainsKey(t.Key)).Select(t => (Resource: t.Key, Row: new double[11], Needed: t.Value)))
                .OrderBy(t => world.Content.Resources.TryGetValue(t.Resource, out var r) ? r.Tier : 99).ThenBy(t => t.Resource)
                .Select(t => new NetworkResourceDto(t.Resource, t.Row[0], t.Row[1], t.Row[2], t.Row[3], t.Row[4], t.Row[5], t.Row[6], t.Row[7], t.Needed, t.Row[8], t.Row[9], Math.Round(t.Row[10], 1),
                    Math.Round(Math.Min(t.Row[9], LinkSends(world, network, t.Resource)), 1),
                    t.Needed > t.Row[1] + 0.05 ? ShortWhy(world, network, t.Resource, r => resources.TryGetValue(r, out var row) && row[0] + row[3] + row[6] + row[8] - row[1] - row[7] > 0.05) : null,
                    Math.Round(outputs.GetValueOrDefault(t.Resource).Could, 1),
                    world.SlowedWhy(outputs.GetValueOrDefault(t.Resource).Slowed, outputs.GetValueOrDefault(t.Resource).First),
                    outputs.GetValueOrDefault(t.Resource).First?.Id))
                .ToList(),
            bottlenecks,
            network.Buildings.Select(world.Building).OfType<Building>()
                .Where(t => (viewer == null || t.Owner == viewer) && world.IdleReasonOf(t).Cause == IdleCause.Stuck)
                .Select(t => new StuckBuildingDto(t.Id, t.Type, $"{world.Content.Building(t.Type).Name} is stuck: {world.GoodsWord(world.IdleReasonOf(t).Resource!)} can't leave"))
                .ToList(),
            viewer is { } waiter && world.Company(waiter) is { } company
                ? world.PipeTilesWaiting(company).Where(network.Conductors.Contains).SelectMany(p => new[] { p.X, p.Y }).ToList()
                : null);
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
                    me.TradeBlocked.Contains(t.Id),
                    me.PendingBlocks.TryGetValue(t.Id, out var blockAt) ? blockAt : null);
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
            profile.Goods,
            profile.Lacks,
            profile.Strength,
            me is { } && owner == null ? world.CratesIfMovedTo(me, seat) : null);
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

    /// <summary>Find a player (1.4 §6): the companies whose name holds the text, those it starts first, with where they live</summary>
    public static IReadOnlyList<FoundPlayerDto> FindPlayer(GameWorld world, string text)
    {
        var query = text.Trim();
        if (query.Length < 2)
        {
            return [];
        }
        return world.Companies
            .Where(t => t.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenByDescending(t => t.Level)
            .ThenBy(t => t.Id)
            .Take(8)
            .Select(t => new FoundPlayerDto(t.Id, t.Name, t.Color, t.Level, t.District.X, t.District.Y, t.Slot, WorldNames.District(t.District),
                world.CrewOf(t) is { NameHidden: false } crew ? crew.Name : null))
            .ToList();
    }

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
            world.OpenDistricts.OrderBy(t => Math.Abs(t.X) + Math.Abs(t.Y))
                .Select(t => world.TownAt(t) is var town ? new DistrictRefDto(t.X, t.Y, WorldNames.District(t), town.Era, town.LaunchedAt != null, town.Missions) : null)
                .OfType<DistrictRefDto>().ToList());
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
        var price = LinkPrice(world, link);
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
        if (world.LinkStartsAt(link) != null)
        {
            // A new link waits for the Yard's rounds (the owner, 2026-10-31: "my smelters are not doing anything still";
            // it said "You buy 0 coal/min")
            var first = selling ? $"{name} your first {good}" : $"your first {good}";
            return ("info", link.StartRounds > 1
                ? $"Starting: {first} comes in about two minutes (the Yard sets a new link up at one round and brings its goods at the next)."
                : $"Starting: the Yard's next round brings {first}, within a minute.");
        }
        var notice = link.NoticeAt is { } at
            ? $" · {(link.NoticeEnds ? "ends" : "pauses")} in {Span(at - world.Now)} ({(link.NoticeBy == me.Id ? "your notice" : $"{name}'s notice")})"
            : "";
        var resting = partner != null && world.ActivityOf(partner) != Activity.Active ? $" {name} is resting; this link keeps delivering." : "";
        if (link.GuaranteeShort)
        {
            // No penalty, a warning (the owner, 2026-10-31)
            return ("warn", selling
                ? $"You make less {good} than the {link.Guaranteed:0.#} a minute you promised {name}: {PerMinute(link.Rate)} arrive now. Make more, or promise less."
                : $"{name} makes less {good} than the {link.Guaranteed:0.#} a minute they promised: {PerMinute(link.Rate)} arrive now{(tankTime != null ? $"; your tank makes up for it for {tankTime}" : "")}.");
        }
        // What of it is promised, and the rest from the seller's spare
        var promise = link.Guaranteed > 1e-9 && link.Rate > 1e-9
            ? $": {PerMinute(Math.Min(link.Guaranteed, link.Rate))} guaranteed{(link.Rate > link.Guaranteed + 0.05 ? $", {PerMinute(link.Rate - link.Guaranteed)} from {(selling ? "your" : "their")} spare" : "")}"
            : "";
        var (tone, line) = link.Flow switch
        {
            LinkFlow.Live => ("good", selling
                ? $"{name} buys {PerMinute(link.Rate)} {good}/min at {price:0.##} g{promise}"
                : $"You buy {PerMinute(link.Rate)} {good}/min from {name} at {price:0.##} g{promise}"),
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
            LinkFlow.NotAllowed => ("warn", NotAllowedWhy(world, link, name, good)),
            _ => ("info", ""),
        };
        return (tone, line + "." + resting + notice);
    }

    /// <summary>Why nothing flows on a link that isn't allowed, by its real cause (it said "a Block, a trade setting, or a buyer out of gold")</summary>
    private static string NotAllowedWhy(GameWorld world, Link link, string partnerName, string good)
    {
        if (world.Company(link.Seller) is not { } seller || world.Company(link.Buyer) is not { } buyer)
        {
            return $"Nothing flows: {partnerName} is gone";
        }
        if (!world.TradesWith(seller, buyer))
        {
            return $"Nothing flows: {seller.Name} stopped trading with {buyer.Name} (Trades › Settings › Who you trade with)";
        }
        if (!world.TradesWith(buyer, seller))
        {
            return $"Nothing flows: {buyer.Name} stopped trading with {seller.Name} (Trades › Settings › Who you trade with)";
        }
        if (seller.NoTrade.Contains(link.Resource))
        {
            return $"Nothing flows: {seller.Name} doesn't sell {good} to players (Trades › Settings, per good)";
        }
        if (buyer.NoBuy.Contains(link.Resource))
        {
            return $"Nothing flows: {buyer.Name} doesn't buy {good} from players (Trades › Settings, per good)";
        }
        return buyer.Gold <= 0 ? $"Nothing flows: {buyer.Name} is out of gold" : "Nothing flows: a trade setting stops it (Trades › Settings)";
    }

    /// <summary>What a link's buyer pays for a unit: the seller's special or public price for them (1.4 §8)</summary>
    private static double LinkPrice(GameWorld world, Link link) =>
        world.Company(link.Seller) is { } seller && world.Company(link.Buyer) is { } buyer ? world.PriceFor(seller, buyer, link.Resource) : world.PlayerPrice(link.Resource);

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
            LinkPrice(world, link),
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
            Flat(link.Route),
            world.LinkStartsAt(link),
            Math.Round(link.Flows ? link.Send + link.SendFirst : 0, 2),
            link.Guaranteed,
            link.UpTo,
            link.AskedGuaranteed,
            link.AskedUpTo,
            link.AskedBy == me.Id,
            Math.Round(world.LinkThroughput(link) * world.LinkEfficiency(link), 1),
            // What it makes now (a new link has had no round yet), and its spare as the last round measured it
            Math.Round(world.Company(link.Seller) is { } seller ? world.MakesAtGate(seller, link.Resource) : 0, 1),
            Math.Round(link.SellerSpare, 1),
            link.GuaranteeShort);
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
                ? "Nobody else here has piped to the Yard yet: your spare goes on to your Trading Post, which sells it to the town's market (the hub), until they do."
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
                                    ? $"Nobody here needs your {Name(spare)} yet: it goes on to your Trading Post, which sells it to the town's market (the hub)."
                                    : $"Nobody here needs your {Name(spare)} yet: pipe it to your Trading Post and the town's market (the hub) buys it there."
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
                t.Company.PinMakes.Order().ToList(), t.Company.PinNeeds.Order().ToList(),
                t.Company.Id == me.Id ? new Dictionary<string, double>()
                    : t.Spare.Keys.ToDictionary(r => r, r => Math.Round(world.PriceFactor(t.Company, me, r), 2)))).ToList(),
            yours,
            world.YardTotals(station).Select(t => new ResourceRateDto(t.Key, t.Value)).OrderByDescending(t => t.PerMinute).ToList(),
            line,
            Discoveries(world, station),
            world.BuildingsOf(me.Id).Where(t => !t.UnderConstruction).SelectMany(t => world.Content.Building(t.Type).Out.Keys).Distinct()
                .SelectMany(t => world.Bids(me, t, residents))
                .OrderByDescending(t => t.PerMinute * world.BasePrice(t.Resource))
                .Take(4)
                .Select(t => new BidDto(t.Resource, t.Buyer.Id, world.NameFor(t.Buyer, me), t.Building == null ? null : world.Content.Building(t.Building.Type).Name, t.Building?.Id, Math.Round(t.PerMinute, 2)))
                .ToList(),
            world.TradesThisWeek(me).Take(8)
                .Select(t => new WeekTradeDto(t.Partner.Id, world.NameFor(t.Partner, me), t.Resource, Math.Round(t.Given), Math.Round(t.Received)))
                .ToList(),
            YardIncomeLine(world, me, yours),
            world.Content.Monuments.Level);
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
        parts.Add(neighbours >= 0 ? $"Neighbours pay you {Amount(neighbours)} g a minute" : $"You pay neighbours {Amount(-neighbours)} g a minute");
        var rest = income - neighbours;
        var line = $"{string.Join(" ", parts)}, out of your {(income >= 0 ? "+" : "−")}{Amount(Math.Abs(income))}";
        return rest >= 0.5 ? $"{line}. The hub pays the rest, for what nobody here takes." : $"{line}.";
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

    public static ExchangeDto Exchange(GameWorld world, Company me)
    {
        // Every town's orders: the Exchange is how towns trade (1.4 §8)
        const bool global = true;
        var orders = world.ExchangeOrders.Where(t => t.District == me.District || t.Global).ToList();
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
        return new ExchangeDto(me.Level >= GameWorld.ExchangeLevel ? null : GameWorld.ExchangeLevel, books, mine, shipments, GameWorld.ExchangeFee, global,
            world.Content.ResourceList.Where(t => t.Era <= world.YourEra(me)).ToDictionary(t => t.Id, t => world.PlayerPrice(t.Id)),
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
        Tracks = content.TrackList,
        TrackTiers = content.TrackRules.Tiers.Select(t => new { t.Tier, t.Minutes, Effect = Camel(t.Effect), t.Amount, t.Gold }).ToList(),
        content.TrackRules.HubOrderFrom,
        content.TrackRules.HubOrderPerTier,
        Monuments = content.Monuments.Kinds,
        Wonders = content.EraWonders.Select(t => new { t.Id, t.Name, t.Era, t.Description, t.Worth }).ToList(),
        content.Relics,
        Progression = new
        {
            content.Progression.MaxCompanyLevel,
            EarnedForLevel = Enumerable.Range(0, content.Progression.MaxCompanyLevel + 2).Select(content.Progression.EarnedForLevel).ToList(),
            content.Progression.MaxBuildingLevel,
            content.Progression.OutputPerLevel,
            content.Progression.CostPerLevel,
            content.Progression.Milestones,
            content.Progression.CostPerCopy,
            content.Progression.LevelGoodsFrom,
            content.Progression.LevelGoodsShare,
            content.Progression.ParcelLevels,
            ParcelCosts = Enumerable.Range(0, content.Progression.ParcelLevels.Count).Select(content.Progression.ParcelCost).ToList(),
            content.Progression.HubSpread,
        },
        // The geometry of 1.4 §4 (TransportPipes.Frontend/src/game/layout.ts reads it): the town, its Yard groups and plots,
        // each plot's gate, start area, HQ and trading post, and the outskirts set aside for it
        Layout = new
        {
            WorldLayout.EstateSize,
            WorldLayout.ParcelSize,
            WorldLayout.ParcelsPerSide,
            WorldLayout.ChunkSize,
            WorldLayout.DistrictWidth,
            WorldLayout.DistrictHeight,
            WorldLayout.WildernessWidth,
            WorldLayout.DistrictPitchX,
            WorldLayout.DistrictPitchY,
            WorldLayout.OriginOffsetX,
            WorldLayout.OriginOffsetY,
            WorldLayout.RoadX,
            WorldLayout.RoadY,
            WorldLayout.RoadWidth,
            Hub = WorldLayout.HubLocal is var hub ? new { hub.X0, hub.Y0, hub.X1, hub.Y1 } : null,
            TownGates = WorldLayout.TownGatesLocal.Select(t => new { t.X0, t.Y0, t.X1, t.Y1 }).ToList(),
            Plots = Enumerable.Range(0, WorldLayout.EstatesPerDistrict).Select(slot => new
            {
                Origin = WorldLayout.EstateLocal(slot) is var (x, y) ? new { X = x, Y = y } : null,
                Side = Camel(WorldLayout.SideOf(slot)),
                Gate = WorldLayout.YardGateLocal(slot) is var (gx, gy) ? new { X = gx, Y = gy } : null,
                StartParcels = WorldLayout.StartParcels(slot),
                Hq = WorldLayout.HqLocal(slot) is var (hx, hy) ? new { X = hx, Y = hy } : null,
                TradingPost = WorldLayout.TradingPostLocal(slot) is var (px, py) ? new { X = px, Y = py } : null,
                Outskirts = WorldLayout.OutskirtsParcels(slot).Select(t => new { t.X, t.Y }).ToList(),
            }).ToList(),
            Yards = Enumerable.Range(0, WorldLayout.StationsPerDistrict).Select(t => WorldLayout.YardLocal(t) is var yard ? new { yard.X0, yard.Y0, yard.X1, yard.Y1 } : null).ToList(),
            Terrain = Enum.GetValues<TerrainType>().Select(Camel).ToList(),
            RichnessMultiplier = GameWorld.RichnessMultiplier,
        },
    };
}
