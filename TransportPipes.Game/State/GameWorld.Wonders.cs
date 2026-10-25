using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>A town (1.4 §11): its own Era, its Era Wonder and its launch party</summary>
public sealed class Town
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Era { get; set; } = 1;
    public DateTime EraStartedAt { get; set; }

    /// <summary>The Wonder of the town's Era (null after the Launch)</summary>
    public TownWonder? Wonder { get; set; }

    /// <summary>Everyone in the town makes 10% more until then (a new Era's launch party)</summary>
    public DateTime? PartyUntil { get; set; }

    /// <summary>When its Space Elevator launched (null: not yet)</summary>
    public DateTime? LaunchedAt { get; set; }

    /// <summary>The missions it finished since (each a star on the world map, +1% production for good)</summary>
    public int Missions { get; set; }

    public DistrictCoord Coord => new(X, Y);
}

/// <summary>A town's Era Wonder being built (1.4 §13)</summary>
public sealed class TownWonder
{
    public required string Type { get; init; }

    /// <summary>The stages built (the current stage is the next one)</summary>
    public int Stage { get; set; }

    /// <summary>What the current stage needs and what was given so far</summary>
    public Dictionary<string, double> Required { get; set; } = new();
    public Dictionary<string, double> Delivered { get; set; } = new();

    /// <summary>The worth at base prices every company gave to the current stage, and to the whole Wonder</summary>
    public Dictionary<long, double> StageGiven { get; set; } = new();
    public Dictionary<long, double> Given { get; set; } = new();

    /// <summary>The current stage is full: it is built a day after (null: still taking goods)</summary>
    public DateTime? FullAt { get; set; }

    /// <summary>The Yard rounds' gifts: what each giver's gate was matched last round, and what arrives now a minute</summary>
    public Dictionary<long, Dictionary<string, double>> Matched { get; set; } = new();
    public Dictionary<long, Dictionary<string, double>> Incoming { get; set; } = new();
}

public enum WonderTier
{
    None,
    Bronze,
    Silver,
    Gold,
    Diamond,
}

/// <summary>
/// Per-town Eras and their Wonders (1.4 §11, §13). Each town has its own Era, and moves up when its Era Wonder is finished,
/// never by a deadline. A Wonder is built with goods by everyone in the town in three stages of fixed sizes (rule 2): its
/// residents give their spare by itself with a good's "give" switch (beyond their Hub Order, or all of it), give from
/// their Storages, or buy and give with gold at the market's 2.5×. Giving is a gift: no gold comes back, and the goods
/// leave the game. A stage takes at least a day to build once it's full; the givers' shares of it pay rewards. Each stage
/// gives the town a perk for good; the last brings the next Era.
/// </summary>
public sealed partial class GameWorld
{
    public const int LastEra = 5;

    /// <summary>A full stage stands this long after</summary>
    public static readonly TimeSpan StageBuildTime = TimeSpan.FromDays(1);

    /// <summary>A new Era's launch party: +10% production in the town for a day</summary>
    public static readonly TimeSpan PartyLength = TimeSpan.FromDays(1);
    public const double PartyBonus = 0.1;

    /// <summary>The shares of a stage that make a Bronze, Silver, Gold and Diamond builder, and the hours of income they pay</summary>
    private static readonly (WonderTier Tier, double Share, double Hours)[] Tiers =
    [
        (WonderTier.Diamond, 0.15, 12),
        (WonderTier.Gold, 0.07, 6),
        (WonderTier.Silver, 0.03, 3),
        (WonderTier.Bronze, 0.01, 1),
    ];

    private readonly Dictionary<DistrictCoord, Town> _towns = new();

    /// <summary>How many towns finished each Wonder (the trail: every one makes it 10% smaller for the towns after)</summary>
    public Dictionary<string, int> WondersFinished { get; private set; } = new();

    /// <summary>The Era Wonders buy in the Yards (on in the server; off in tests unless a test sets it)</summary>
    public bool WonderBuysEnabled { get; set; }

    public IEnumerable<Town> Towns => _towns.Values;

    public void LoadTowns(IEnumerable<Town> towns, Dictionary<string, int>? finished)
    {
        foreach (var town in towns)
        {
            _towns[town.Coord] = town;
        }
        WondersFinished = finished ?? new();
    }

    /// <summary>A town, made when first asked for: Era 1, its Great Dynamo open</summary>
    public Town TownAt(DistrictCoord coord)
    {
        if (!_towns.TryGetValue(coord, out var town))
        {
            town = new Town { X = coord.X, Y = coord.Y, EraStartedAt = LastTick };
            town.Wonder = NewWonder(1);
            _towns[coord] = town;
        }
        return town;
    }

    /// <summary>A town's Era</summary>
    public int TownEra(DistrictCoord coord) => _towns.TryGetValue(coord, out var town) ? town.Era : 1;

    /// <summary>A company's Era: the highest Era a town reached while it lived there (it never goes down)</summary>
    public int YourEra(Company company) => Math.Max(company.Era, TownEra(company.District));

    /// <summary>The town a building stands in (a Deep Level building: the town above it)</summary>
    public static DistrictCoord TownOf(Building building) => WorldLayout.Surface(building.Position) is var surface ? WorldLayout.DistrictOf(surface.X, surface.Y) : default;

    /// <summary>The Era of the town a building stands in</summary>
    public int EraAt(Building building) => TownEra(TownOf(building));

    /// <summary>A building from a later Era than its town's sleeps (1.4 §9): it makes and uses nothing until the town gets there</summary>
    public bool Sleeps(Building building) => Content.Building(building.Type).Unlock.Era > EraAt(building);

    public WonderDefinition WonderDefinition(TownWonder wonder) =>
        wonder.Type.StartsWith(MissionPrefix, StringComparison.Ordinal)
            ? Content.Missions.Mission(int.Parse(wonder.Type[MissionPrefix.Length..]))
            : Content.EraWonders.First(t => t.Id == wonder.Type);

    /// <summary>A mission's type: this and its number (0: the first)</summary>
    private const string MissionPrefix = "mission-";

    /// <summary>Whether a town builds a mission now (after its Launch) rather than an Era Wonder</summary>
    public static bool IsMission(TownWonder wonder) => wonder.Type.StartsWith(MissionPrefix, StringComparison.Ordinal);

    /// <summary>The town's next mission (1.4 §13)</summary>
    private TownWonder NewMission(Town town)
    {
        var definition = Content.Missions.Mission(town.Missions);
        return new TownWonder { Type = definition.Id, Required = StageRequirements(definition, 0) };
    }

    /// <summary>The town's Wonder, or null after the Launch</summary>
    public TownWonder? WonderOf(DistrictCoord town) => TownAt(town).Wonder;

    private TownWonder? NewWonder(int era)
    {
        if (Content.EraWonders.FirstOrDefault(t => t.Era == era) is not { } definition)
        {
            return null;
        }
        var wonder = new TownWonder { Type = definition.Id };
        wonder.Required = StageRequirements(definition, 0);
        return wonder;
    }

    /// <summary>The trail: every town that finished a Wonder before makes it 10% smaller, down to half</summary>
    public double TrailFactor(WonderDefinition definition) =>
        definition.Id.StartsWith(MissionPrefix, StringComparison.Ordinal) ? 1 : Math.Max(0.5, 1 - 0.1 * WondersFinished.GetValueOrDefault(definition.Id));

    /// <summary>A stage's goods: its share of the Wonder's worth (with the trail), split evenly over its goods at base prices</summary>
    private Dictionary<string, double> StageRequirements(WonderDefinition definition, int stage)
    {
        var part = definition.Stages[stage];
        var worth = definition.Worth * part.Share * TrailFactor(definition) / part.Goods.Count;
        return part.Goods.ToDictionary(t => t, t => Math.Ceiling(worth / Math.Max(0.01, BasePrice(t))));
    }

    /// <summary>What goods are worth at base</summary>
    private double Worth(IReadOnlyDictionary<string, double> goods) => goods.Sum(t => t.Value * BasePrice(t.Key));

    /// <summary>What a Wonder's current stage still lacks of a good (nothing once it's full)</summary>
    public double WonderLacks(TownWonder wonder, string good) =>
        wonder.FullAt != null ? 0 : Math.Max(0, wonder.Required.GetValueOrDefault(good) - wonder.Delivered.GetValueOrDefault(good));

    // ---------------------------------------------------------------- Perks

    /// <summary>The perks of every Wonder stage a town has built (they last for good), of a kind</summary>
    public double TownPerk(DistrictCoord coord, WonderPerk perk)
    {
        if (!_towns.TryGetValue(coord, out var town))
        {
            return 0;
        }
        var total = 0.0;
        foreach (var definition in Content.EraWonders.Where(t => t.Era <= town.Era))
        {
            // The last Era's Wonder stands whole once the town launched it
            var built = definition.Era < town.Era || (town.LaunchedAt != null && definition.Era == town.Era) ? definition.Stages.Count
                : town.Wonder?.Type == definition.Id ? town.Wonder.Stage : 0;
            total += definition.Stages.Take(built).Where(t => t.Perk == perk).Sum(t => t.PerkAmount);
        }
        // Every mission finished after the Launch: more production for good
        return total + (perk == WonderPerk.Everything ? town.Missions * Content.Missions.Perk : 0);
    }

    /// <summary>What a building makes more from its town's Wonders and launch party</summary>
    private double WonderOutputFactor(Building building, BuildingDefinition definition, string resource)
    {
        var town = TownOf(building);
        var factor = 1 + TownPerk(town, WonderPerk.Everything);
        if (resource == "energy")
        {
            factor += TownPerk(town, WonderPerk.Energy);
        }
        if (definition.Category == "factory")
        {
            factor += TownPerk(town, WonderPerk.Factories);
        }
        if (_towns.TryGetValue(town, out var t) && t.PartyUntil > LastTick)
        {
            factor += PartyBonus;
        }
        return factor;
    }

    private double WonderHubOrderFactor(DistrictCoord town) => 1 + TownPerk(town, WonderPerk.HubOrders);

    /// <summary>Research is faster with the town's Wonders (the Thinking Machine)</summary>
    private double WonderResearchFactor(Company company) => 1 / (1 + TownPerk(company.District, WonderPerk.Research));

    // ---------------------------------------------------------------- Giving

    /// <summary>Sets what a company gives the town's Wonder of a good by itself</summary>
    public CommandResult SetGive(long companyId, string resource, GiveMode mode)
    {
        if (Company(companyId) is not { } company || !Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown good.");
        }
        if (mode == GiveMode.Off)
        {
            company.Give.Remove(resource);
        }
        else
        {
            company.Give[resource] = mode;
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkTradeDirty(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>Gives the town's Wonder what the company's Storages hold of what the stage still lacks</summary>
    public CommandResult GiveFromStorages(long companyId)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (WonderOf(company.District) is not { } wonder)
        {
            return CommandResult.Fail("Your town has no Wonder to build.");
        }
        if (wonder.FullAt != null)
        {
            return CommandResult.Fail("This stage is full: it's being built.");
        }
        // Its own goods in the Yard's Depot first ("where the crew piles goods for the Wonder", 1.4 §7)
        var fromDepot = GiveFromDepot(company, wonder);
        var materials = Materials(company.Id);
        var gift = new Dictionary<string, double>();
        foreach (var good in wonder.Required.Keys)
        {
            var amount = Math.Floor(Math.Min(WonderLacks(wonder, good), materials.GetValueOrDefault(good)) * 1000) / 1000;
            if (amount > 1e-9)
            {
                gift[good] = amount;
            }
        }
        if (gift.Count == 0 && fromDepot > 0)
        {
            CheckStageFull(TownAt(company.District));
            return CommandResult.Ok;
        }
        if (gift.Count == 0)
        {
            return CommandResult.Fail($"Your Storages hold nothing it needs ({string.Join(", ", wonder.Required.Keys.Where(t => WonderLacks(wonder, t) > 1e-6).Select(GoodsWord))}).");
        }
        var payment = Pay(company, 0, gift);
        if (!payment.Success)
        {
            return payment;
        }
        foreach (var (good, amount) in gift)
        {
            CountGift(company, wonder, good, amount);
        }
        CheckStageFull(TownAt(company.District));
        return CommandResult.Ok;
    }

    /// <summary>What buying and giving the rest of a stage's goods costs: what it lacks at the market's 2.5×</summary>
    public double BuyAndGiveCost(Company company, string? good = null) =>
        WonderOf(company.District) is { } wonder
            ? Math.Ceiling(wonder.Required.Keys.Where(t => good == null || t == good).Sum(t => WonderLacks(wonder, t) * HubSellPrice(company.District, t)))
            : 0;

    /// <summary>
    /// "Buy and give" (1.4 §13): gold buys the goods the stage still needs from the market at 2.5× and gives them; all of a
    /// good (or of every good), or as much as some gold buys
    /// </summary>
    public CommandResult BuyAndGive(long companyId, string? good = null, double? gold = null)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (WonderOf(company.District) is not { } wonder)
        {
            return CommandResult.Fail("Your town has no Wonder to build.");
        }
        if (wonder.FullAt != null)
        {
            return CommandResult.Fail("This stage is full: it's being built.");
        }
        var goods = wonder.Required.Keys.Where(t => (good == null || t == good) && WonderLacks(wonder, t) > 1e-6).ToList();
        var cost = goods.Sum(t => WonderLacks(wonder, t) * HubSellPrice(company.District, t));
        if (cost <= 0)
        {
            return CommandResult.Fail("It has what it needs.");
        }
        var budget = Math.Min(gold ?? cost, company.Gold);
        var share = Math.Min(1, budget / cost);
        if (share * cost < 1)
        {
            return CommandResult.Fail($"It costs {Math.Ceiling(cost):N0} gold.");
        }
        var spent = 0.0;
        foreach (var item in goods)
        {
            var units = WonderLacks(wonder, item) * share;
            spent += units * HubSellPrice(company.District, item);
            CountGift(company, wonder, item, units);
        }
        company.Gold -= spent;
        company.Report.GoldSpent += spent;
        AddStat(company, "goldSpent", spent);
        AddStat(company, "wonderGold", spent);
        AddStat(company, "pressed:buyAndGive", 1);
        company.Touch();
        Changes.Companies.Add(company.Id);
        CheckStageFull(TownAt(company.District));
        return CommandResult.Ok;
    }

    /// <summary>A gift arrives: it counts for the stage, and as the giver's part</summary>
    private void CountGift(Company giver, TownWonder wonder, string good, double units)
    {
        var counted = Math.Min(units, WonderLacks(wonder, good));
        if (counted <= 1e-9)
        {
            return;
        }
        wonder.Delivered[good] = wonder.Delivered.GetValueOrDefault(good) + counted;
        var worth = counted * BasePrice(good);
        wonder.StageGiven[giver.Id] = wonder.StageGiven.GetValueOrDefault(giver.Id) + worth;
        wonder.Given[giver.Id] = wonder.Given.GetValueOrDefault(giver.Id) + worth;
        AddStat(giver, "wonder", worth);
        giver.Touch();
        Changes.Companies.Add(giver.Id);
        Changes.Wonders = true;
    }

    /// <summary>A stage with all its goods is full: it stands a day later</summary>
    private void CheckStageFull(Town town)
    {
        if (town.Wonder is not { FullAt: null } wonder || wonder.Required.Count == 0
            || wonder.Required.Any(t => wonder.Delivered.GetValueOrDefault(t.Key) < t.Value - 1e-6))
        {
            return;
        }
        wonder.FullAt = LastTick;
        wonder.Matched.Clear();
        wonder.Incoming.Clear();
        var definition = WonderDefinition(wonder);
        foreach (var company in CompaniesInTown(town.Coord))
        {
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Wonder,
                $"{definition.Name}: stage {wonder.Stage + 1} has all its goods! It stands {LastTick + StageBuildTime:ddd HH:mm}"));
        }
        MarkWonderTiles(town);
    }

    private IEnumerable<Company> CompaniesInTown(DistrictCoord town) => _companies.Values.Where(t => t.District == town);

    /// <summary>The hub (with the Wonder) is drawn again</summary>
    private void MarkWonderTiles(Town town)
    {
        Changes.Wonders = true;
        Changes.Districts.Add(town.Coord);
    }

    /// <summary>A full stage that has been building a day stands: its perk, its rewards, the next stage or the next Era</summary>
    private void CheckStagesBuilt()
    {
        foreach (var town in _towns.Values.Where(t => t.Wonder is { FullAt: not null } w && LastTick - w.FullAt >= StageBuildTime).ToList())
        {
            CompleteStage(town);
        }
    }

    private void CompleteStage(Town town)
    {
        var wonder = town.Wonder!;
        var definition = WonderDefinition(wonder);
        var stage = definition.Stages[wonder.Stage];
        PayStageRewards(town, wonder, definition);
        wonder.Stage++;
        wonder.FullAt = null;
        wonder.Delivered.Clear();
        wonder.StageGiven.Clear();
        // The perk changes production and prices in the town
        MarkTownDirty(town.Coord);
        if (wonder.Stage < definition.Stages.Count)
        {
            wonder.Required = StageRequirements(definition, wonder.Stage);
            var perk = stage.Perk == WonderPerk.None ? "" : $": {PerkText(stage.Perk, stage.PerkAmount)} for everyone in the town";
            foreach (var company in CompaniesInTown(town.Coord))
            {
                Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Wonder, $"{definition.Name}: stage {wonder.Stage} of {definition.Stages.Count} stands{perk}"));
            }
            MarkWonderTiles(town);
            return;
        }
        if (IsMission(wonder))
        {
            // A mission launched: a star for the town, a little more for everyone in it, and the next one
            town.Missions++;
            town.Wonder = NewMission(town);
            foreach (var company in CompaniesInTown(town.Coord))
            {
                Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Wonder,
                    $"{definition.Name} launched: +{Content.Missions.Perk:P0} production for good for everyone in your town, and its star number {town.Missions}"));
            }
            MarkTownDirty(town.Coord);
            MarkWonderTiles(town);
            return;
        }
        WondersFinished[definition.Id] = WondersFinished.GetValueOrDefault(definition.Id) + 1;
        if (town.Era >= LastEra)
        {
            Launch(town, wonder, definition);
            return;
        }
        AdvanceEra(town);
    }

    private static string PerkText(WonderPerk perk, double amount) => perk switch
    {
        WonderPerk.Energy => $"+{amount:P0} energy",
        WonderPerk.HubOrders => $"Hub Orders +{amount:P0}",
        WonderPerk.Factories => $"factories +{amount:P0}",
        WonderPerk.Research => $"research {amount:P0} faster",
        _ => $"+{amount:P0} production",
    };

    /// <summary>A giver's share of a stage, and the tier that makes</summary>
    public (double Share, WonderTier Tier) StageTier(TownWonder wonder, long companyId)
    {
        var needed = Worth(wonder.Required);
        var share = needed <= 0 ? 0 : wonder.StageGiven.GetValueOrDefault(companyId) / needed;
        foreach (var (tier, least, _) in Tiers)
        {
            if (share >= least - 1e-9)
            {
                return (share, tier);
            }
        }
        return (share, WonderTier.None);
    }

    /// <summary>A builder's tier pays hours of its own income</summary>
    public double TierReward(Company company, WonderTier tier) =>
        Math.Round(60 * MeasuredIncome(company) * Tiers.Where(t => t.Tier == tier).Select(t => t.Hours).DefaultIfEmpty(0).First());

    private void PayStageRewards(Town town, TownWonder wonder, WonderDefinition definition)
    {
        foreach (var (companyId, _) in wonder.StageGiven)
        {
            if (Company(companyId) is not { } company || StageTier(wonder, companyId) is not { Tier: not WonderTier.None } result)
            {
                continue;
            }
            var reward = TierReward(company, result.Tier);
            company.Gold += reward;
            CountEarned(company, reward, "rewards", definition.Name);
            company.Titles.Add($"{definition.Name} · {result.Tier}");
            company.Touch();
            Changes.Companies.Add(company.Id);
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Wonder,
                $"{result.Tier} builder of {definition.Name}'s stage {wonder.Stage + 1} ({result.Share:P0} of it): +{Gold(reward)}"));
        }
    }

    // ---------------------------------------------------------------- Eras

    /// <summary>
    /// A town's next Era (1.4 §11): its buildings wake, the market buys the new goods and wants more of the old, the
    /// Era's tracks and tree open, a launch party, a new Monument site in every Yard
    /// </summary>
    public void AdvanceEra(Town town)
    {
        if (town.Era >= LastEra)
        {
            return;
        }
        var finished = town.Wonder is { } old ? WonderDefinition(old).Name : "The Wonder";
        town.Era++;
        town.EraStartedAt = LastTick;
        town.PartyUntil = LastTick + PartyLength;
        town.Wonder = NewWonder(town.Era);
        foreach (var company in CompaniesInTown(town.Coord).ToList())
        {
            company.Era = Math.Max(company.Era, town.Era);
            company.EraEnded = $"{finished} stands: Era {town.Era} began";
            company.Touch();
            GrantResearch(company);
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Wonder,
                $"{finished} stands: Era {town.Era} begins in your town! New buildings, goods and research, a new Monument site, and +10% production for a day"));
            Changes.Companies.Add(company.Id);
        }
        MarkTownDirty(town.Coord);
        MarkWonderTiles(town);
        Changes.Outposts = true;
    }

    /// <summary>For tests and admins: a town's next Era at once</summary>
    public void AdvanceEra(DistrictCoord town) => AdvanceEra(TownAt(town));

    /// <summary>Everything in a town is solved again (perks, sleeping buildings, Hub Orders) and its market redrawn</summary>
    private void MarkTownDirty(DistrictCoord town)
    {
        foreach (var network in _networks.Values.Where(t => t.District == town))
        {
            network.NeedsSolve = true;
            Changes.Networks.Add(network.Id);
        }
        foreach (var company in CompaniesInTown(town))
        {
            MarkCompanyNetworksDirty(company.Id);
        }
        Changes.Markets.Add(town);
    }

    // ---------------------------------------------------------------- Gifts in the Yard round

    /// <summary>Whether the Wonders take gifts in the Yards now</summary>
    private bool WondersGiving => WonderBuysEnabled && Content.EraWonders.Count > 0;

    /// <summary>The start of a Yard round: what the gates were matched for the Wonder last round arrives now, less when they sent less</summary>
    private void DeliverWonderGifts(StationRef station, List<YardMember> members)
    {
        if (!WondersGiving || WonderOf(station.District) is not { FullAt: null } wonder)
        {
            return;
        }
        foreach (var giver in members)
        {
            if (!wonder.Matched.TryGetValue(giver.Company.Id, out var goods) || !_gateTerms.TryGetValue(giver.Key, out var sent))
            {
                continue;
            }
            foreach (var (good, amount) in goods)
            {
                var exported = sent.Export.GetValueOrDefault(good);
                var ratio = exported <= 1e-12 ? 0 : Math.Min(1, giver.Sent.GetValueOrDefault(good) / exported);
                if (amount * ratio > 1e-9)
                {
                    if (!wonder.Incoming.TryGetValue(giver.Company.Id, out var incoming))
                    {
                        wonder.Incoming[giver.Company.Id] = incoming = new Dictionary<string, double>();
                    }
                    incoming[good] = incoming.GetValueOrDefault(good) + amount * ratio;
                }
            }
        }
    }

    /// <summary>
    /// After the Yard's trade: the residents' "give" switches send the Wonder their spare of what it lacks (beyond their
    /// Hub Order, or all of it); the next round comes no later than it would be full
    /// </summary>
    private Dictionary<(EstateKey Giver, string Resource), double> MatchWonderGifts(StationRef station, List<YardMember> members, ref TimeSpan? limit)
    {
        var result = new Dictionary<(EstateKey, string), double>();
        if (!WondersGiving || WonderOf(station.District) is not { FullAt: null } wonder)
        {
            return result;
        }
        foreach (var good in wonder.Required.Keys.Order(StringComparer.Ordinal))
        {
            // What arrives this minute is on its way already
            var open = WonderLacks(wonder, good) - wonder.Incoming.Values.Sum(t => t.GetValueOrDefault(good));
            var lacks = open - wonder.Matched.Values.Sum(t => t.GetValueOrDefault(good));
            foreach (var member in members.Where(t => t.Company.Give.ContainsKey(good) && t.Spare.GetValueOrDefault(good) > 1e-9).OrderBy(t => t.Company.Id))
            {
                if (lacks <= 1e-9)
                {
                    break;
                }
                var spare = member.Spare[good];
                var gives = member.Company.Give[good] == GiveMode.All ? spare : Math.Max(0, spare - HubOrder(member.Company, good));
                var amount = Math.Min(gives, lacks);
                if (amount <= 1e-9)
                {
                    continue;
                }
                Add(member.Spare, good, -amount);
                lacks -= amount;
                result[(member.Key, good)] = amount;
                if (!wonder.Matched.TryGetValue(member.Company.Id, out var goods))
                {
                    wonder.Matched[member.Company.Id] = goods = new Dictionary<string, double>();
                }
                goods[good] = goods.GetValueOrDefault(good) + amount;
            }
            var rate = wonder.Matched.Values.Sum(t => t.GetValueOrDefault(good));
            if (rate > 1e-9)
            {
                var until = TimeSpan.FromMinutes(Math.Clamp(open / rate, CouplingInterval.TotalMinutes, MaxCouplingInterval.TotalMinutes));
                limit = limit is { } l && l < until ? l : until;
            }
        }
        return result;
    }

    /// <summary>What arrived at the Wonders counts as its givers' part (no gold: a gift)</summary>
    private void SettleWonderGifts(double minutes)
    {
        if (!WondersGiving)
        {
            return;
        }
        foreach (var town in _towns.Values.Where(t => t.Wonder is { FullAt: null }).ToList())
        {
            var wonder = town.Wonder!;
            foreach (var (giverId, goods) in wonder.Incoming)
            {
                if (Company(giverId) is not { } giver)
                {
                    continue;
                }
                foreach (var (good, perMinute) in goods)
                {
                    var units = perMinute * minutes;
                    if (units > 1e-9)
                    {
                        Add(giver.Report.Sold, good, units);
                        CountGift(giver, wonder, good, units);
                    }
                }
            }
            CheckStageFull(town);
        }
    }

    /// <summary>The town's players who make a good: played this week and own a finished building that makes it</summary>
    public List<Company> WonderMakers(DistrictCoord town, string good)
    {
        var since = LastTick - ActivePeriod;
        return CompaniesInTown(town)
            .Where(t => t.LastSeenAt >= since && BuildingsOf(t.Id).Any(b => !b.UnderConstruction && Content.Building(b.Type).Out.ContainsKey(good)))
            .OrderBy(t => t.Id)
            .ToList();
    }

    // ---------------------------------------------------------------- The Launch and Legacy

    /// <summary>Legacy every player in a town gets at its Launch, and on top for those who gave to its Space Elevator</summary>
    public const int LaunchLegacy = 40;
    public const int LaunchGiverLegacy = 40;

    /// <summary>
    /// The town's Space Elevator launches (1.4 §13): titles for everyone in the town, Legacy for their accounts (a giver
    /// gets more), and the missions begin
    /// </summary>
    private void Launch(Town town, TownWonder wonder, WonderDefinition definition)
    {
        town.LaunchedAt = LastTick;
        town.Wonder = NewMission(town);
        foreach (var company in CompaniesInTown(town.Coord))
        {
            company.Titles.Add("Launched the Space Elevator");
            company.Touch();
            var gave = wonder.Given.GetValueOrDefault(company.Id) > 0;
            Changes.Legacy.Add((company.UserId, LaunchLegacy + (gave ? LaunchGiverLegacy : 0), "Launched the Space Elevator"));
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Wonder,
                $"{definition.Name} launches! Your town reached the stars: Legacy for your account, and the missions begin"));
            Changes.Companies.Add(company.Id);
        }
        MarkTownDirty(town.Coord);
        MarkWonderTiles(town);
    }

    /// <summary>Legacy (1.4 §13, after the Launch): from level 5 a free manager slot for any future company</summary>
    public const int LegacyManagerLevel = 5;

    /// <summary>Legacy level from Legacy points: the square root, in tens</summary>
    public static int LegacyLevelOf(int legacy) => (int)Math.Floor(Math.Sqrt(Math.Max(0, legacy) / 10.0));

    /// <summary>The extra starting gold Legacy gives (at most 10,000)</summary>
    public static double LegacyGold(int legacyLevel) => Math.Min(10_000, 500.0 * legacyLevel);
}
