using TransportPipes.Game.World;

namespace TransportPipes.Game.Content;

/// <summary>A good: its base price, its Era, and how the pipe carries it (power, fluid or solid: 1.4 §9)</summary>
public sealed record ResourceDefinition(string Id, string Name, string Icon, string Color, int Tier, int Era, double Price, string Kind = "solid");

public sealed record PipeTierDefinition(string Id, string Name, int Tier, int Era, double Gold, IReadOnlyDictionary<string, double> Materials, double LossPerTile, double Throughput, string? Research);

public enum BuildingKind
{
    Producer,
    Storage,
    TradingPost,
    Lab,
    Hq,
    Gate,
    Valve,
    PriorityValve,
    Pump,
    CargoDepot,
    Workshop,
    WeatherStation,
    Recycler,
    ExpeditionOffice,
    BlueprintOffice,
    DeepLift,
    Teleporter,
    Terraformer,
    PowerLine,

    /// <summary>The Yard's Depot: the crew's shared storage (1.4 §7)</summary>
    Depot,

    Decoration,
}

public enum ProductionModifier
{
    None,
    Wind,
    Sun,
    Richness,
    Forest,
}

public enum BuildingLayer
{
    Surface,
    Deep,

    /// <summary>On the surface and in the Deep Level</summary>
    Any,
}

/// <summary>What a building needs: a Company Level, an Era, a tech, or a finished collection (decorations)</summary>
public sealed record BuildingUnlock(int Level, int Era, string? Research, string? Collection = null);

/// <summary>A manager (GDD §8.4); the automation and the bonus are in the code, by id</summary>
public sealed record ManagerDefinition(string Id, string Name, double Cost, string Color, string Automation, string Bonus);

/// <summary>A relic expeditions bring back: a collectible with lore</summary>
public sealed record RelicDefinition(string Id, string Name, string Lore);

/// <summary>
/// A step of Loady's guide (GDD §10.2), done when the stat reaches the amount; chapter 1 is the first session, chapter 2
/// the middle game (docs/v1.1-design.html §5)
/// </summary>
public sealed record TutorialStep(string Id, string Text, string Stat, double Amount, double Gold, IReadOnlyDictionary<string, string> Hint, int Chapter = 1, string? PhoneText = null)
{
    /// <summary>The buildings of the step's hint, in the order to build them ("copperMine,coalPit,smelter")</summary>
    public IReadOnlyList<string> HintBuildings => Hint.TryGetValue("building", out var list) ? list.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : [];
}

/// <summary>An achievement (GDD §8.5): a stat that reaches an amount; it pays gold and hours of your income (1.4 §16)</summary>
public sealed record AchievementDefinition(string Id, string Name, string Stat, double Amount, double Hours, double Gold, string? Title, OutputBonus? Output, string Description);

/// <summary>A collection: every building of a list at a level; it unlocks a decoration and pays hours of your income</summary>
public sealed record CollectionDefinition(string Id, string Name, int Level, IReadOnlyList<string> Buildings, double Hours, string? Title, string Description);

public sealed record BuildingCost(double Gold, IReadOnlyDictionary<string, double> Materials);

public sealed record BuildingPlacement(IReadOnlyList<TerrainType> Terrain, string? Deposit, bool Shore, bool Forest, int MinRichness)
{
    public static readonly IReadOnlyList<TerrainType> DefaultTerrain = [TerrainType.Grass, TerrainType.Hills, TerrainType.Forest, TerrainType.Desert, TerrainType.Beach];
}

public sealed class BuildingDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Category { get; init; }
    public BuildingKind Kind { get; init; }
    public int Size { get; init; } = 1;
    public bool Buildable { get; init; } = true;
    public required BuildingUnlock Unlock { get; init; }
    public required BuildingCost Cost { get; init; }
    public double BuildSeconds { get; init; }

    /// <summary>Inputs per minute at level 1</summary>
    public IReadOnlyDictionary<string, double> In { get; init; } = new Dictionary<string, double>();

    /// <summary>Outputs per minute at level 1</summary>
    public IReadOnlyDictionary<string, double> Out { get; init; } = new Dictionary<string, double>();

    /// <summary>For buildings like the Deep Mine: the output resource is the deposit's resource (one of these)</summary>
    public IReadOnlyList<string> OutFromDeposit { get; init; } = [];

    public double ResearchPerMinute { get; init; }
    public required BuildingPlacement Placement { get; init; }
    public ProductionModifier Modifier { get; init; }
    public double Capacity { get; init; }
    public double Throughput { get; init; }
    public double PriceBonus { get; init; }
    public string? Range { get; init; }

    /// <summary>Power line pylons: how far (in tiles) the cable reaches to the next pylon</summary>
    public int Reach { get; init; }

    public BuildingLayer Layer { get; init; }

    /// <summary>The bigger, better building it can evolve into where it stands</summary>
    public string? EvolvesTo { get; init; }

    public string? Sprite { get; init; }
    public string Description { get; init; } = "";

    /// <summary>Sits in a pipe and carries resources like a pipe tile (valves, toll meters)</summary>
    public bool IsConductor => Kind is BuildingKind.Gate or BuildingKind.Valve or BuildingKind.PriorityValve or BuildingKind.PowerLine or BuildingKind.CargoDepot
        or BuildingKind.DeepLift;
}

/// <summary>What a Wonder stage gives everyone in its town for good (1.4 §13)</summary>
public enum WonderPerk
{
    None,

    /// <summary>Energy buildings make more</summary>
    Energy,

    HubOrders,

    /// <summary>Factories make more</summary>
    Factories,

    /// <summary>Research is faster</summary>
    Research,

    /// <summary>Every building makes more</summary>
    Everything,
}

/// <summary>A stage of an Era Wonder: its share of the Wonder's worth, the goods it takes (in equal worth), its perk</summary>
public sealed record WonderStage(double Share, IReadOnlyList<string> Goods, WonderPerk Perk, double PerkAmount);

/// <summary>A town's Era Wonder (1.4 §13): built with goods by everyone in the town, in three stages</summary>
public sealed class WonderDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Era { get; init; }
    public string Description { get; init; } = "";

    /// <summary>The gold's worth of goods at base prices it takes in all</summary>
    public double Worth { get; init; }

    public required IReadOnlyList<WonderStage> Stages { get; init; }
}

/// <summary>What a Monument's stages give every resident of its Yard (1.4 §13)</summary>
public enum MonumentPerk
{
    Production,
    HubOrders,
    Research,
    Cheaper,
    YardSales,
}

/// <summary>A kind of Monument: its perk a stage, and the most stages it gives it for (0: no limit)</summary>
public sealed record MonumentKind(string Id, string Name, MonumentPerk Perk, double Amount, int Cap, string Description);

/// <summary>The Monuments (1.4 §13): a site per Era in every Yard, stages without end paid in gold</summary>
public sealed class MonumentRules
{
    public double FirstStage { get; init; } = 100_000_000;
    public double EraFactor { get; init; } = 10;
    public double StageFactor { get; init; } = 1.4;

    /// <summary>The Company Level from which a resident may pay into a Monument</summary>
    public int Level { get; init; } = 5;

    public IReadOnlyList<MonumentKind> Kinds { get; init; } = [];

    /// <summary>The gold of a site's stage n (1: the first)</summary>
    public double StageCost(int era, int stage) => Math.Round(FirstStage * Math.Pow(EraFactor, era - 1) * Math.Pow(StageFactor, stage - 1));
}

/// <summary>+Amount output for buildings that match one of the targets (modifier:wind, building:x, category:x, resource:x)</summary>
public sealed record OutputBonus(IReadOnlyList<string> Targets, double Amount)
{
    public bool Applies(BuildingDefinition building, string resource) => Targets.Any(target =>
    {
        var colon = target.IndexOf(':');
        var (kind, value) = (target[..colon], target[(colon + 1)..]);
        return kind switch
        {
            "modifier" => string.Equals(building.Modifier.ToString(), value, StringComparison.OrdinalIgnoreCase),
            "building" => building.Id == value,
            "category" => building.Category == value,
            "resource" => resource == value,
            _ => false,
        };
    });
}

/// <summary>An item of the Era tree (1.4 §12): short research that opens an Era's pipes, buildings and systems</summary>
public sealed class ResearchDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Era { get; init; } = 1;

    /// <summary>Every company gets it at this Company Level once its Era has come, Lab or not (0: only by research)</summary>
    public int Grant { get; init; }

    public double Gold { get; init; }
    public double Minutes { get; init; }
    public int ManagerSlots { get; init; }
    public string Description { get; init; } = "";
}

/// <summary>What a track's tier does (1.4 §12)</summary>
public enum TierEffect
{
    Faster,
    Leaner,
    Complexes,
    MegaComplexes,
    Master,
}

/// <summary>A tier of every track: how long it takes, what it does, its gold in an Era-1 track (×4 per Era)</summary>
public sealed record TrackTier(int Tier, double Minutes, TierEffect Effect, double Amount, double Gold);

/// <summary>
/// A research track (1.4 §12): deep research for one chain, nine tiers over about ten days. Buildings may name the
/// deposit they stand on ("deepMine:coal"); a building in two tracks follows the better of them (a Coal Pit)
/// </summary>
public sealed class TrackDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Era { get; init; } = 1;

    /// <summary>The share of each faster tier's boost (Power: half)</summary>
    public double Strength { get; init; } = 1;

    /// <summary>What tiers from TierGoodsFrom take besides gold (null: gold only)</summary>
    public string? Good { get; init; }

    /// <summary>Whose Hub Orders it raises from HubOrderFrom</summary>
    public IReadOnlyList<string> Goods { get; init; } = [];

    public IReadOnlyList<string> Buildings { get; init; } = [];
    public string Description { get; init; } = "";
}

/// <summary>How the tracks work: their tiers, the goods a deep tier takes, the Hub Orders a master gets</summary>
public sealed class TrackRules
{
    public IReadOnlyList<TrackTier> Tiers { get; init; } = [];
    public int TierGoodsFrom { get; init; } = 3;
    public double TierGoodsShare { get; init; } = 0.2;
    public int HubOrderFrom { get; init; } = 5;
    public double HubOrderPerTier { get; init; } = 1.8;

    /// <summary>What the leaner tiers give wind, sun and water power instead (more output)</summary>
    public IReadOnlyList<double> LeanerOutput { get; init; } = [];

    /// <summary>A tier's gold in a track of an Era</summary>
    public double TierGold(int tier, int era) => tier < 1 || tier > Tiers.Count ? 0 : Tiers[tier - 1].Gold * Math.Pow(4, era - 1);
}

/// <summary>The rules of playing together (docs/multiplayer-design.html), as multiples of a good's base price</summary>
public sealed class TogetherSettings
{
    /// <summary>The default public price (1.4 §8: your own prices replace it)</summary>
    public double PlayerPrice { get; init; } = 1.3;

    /// <summary>Moving to another plot: how long you wait between moves, how long the plot is in the air, what it costs</summary>
    public double MoveCooldownMinutes { get; init; } = 5;
    public double MoveFlightMinutes { get; init; } = 1;
    public double MoveCostPerLevel { get; init; }
}

/// <summary>A piece of the Company Level curve: from level From, the gold earned for it is Gold × Factor^(level − From)</summary>
public sealed record LevelPiece(int From, double Gold, double Factor);

/// <summary>The goods a level step takes up to a level (each an equal share of the goods' worth)</summary>
public sealed record LevelGoodsBand(int UpTo, IReadOnlyList<string> Goods);

/// <summary>
/// The market (1.4 §8): a Hub Order is worth HubOrderValue gold a minute at an Era-1 good's base price, EraValue times
/// more a later Era, EraGrowth times more for each Era the town is past the good's, PostLevel times more for every
/// trading post level. Beyond it the market pays base × √(order × sold) up to FloodFrom orders, then Floor × base a unit.
/// </summary>
public sealed class MarketSettings
{
    public double HubOrderValue { get; init; } = 1000;
    public double EraValue { get; init; } = 4;
    public double EraGrowth { get; init; } = 1.5;
    public double PostLevel { get; init; } = 1.1;
    public double FloodFrom { get; init; } = 16;
    public double Floor { get; init; } = 0.25;

    /// <summary>What the market pays for so many units a minute of a good against a Hub Order (both a minute)</summary>
    public double Pays(double basePrice, double hubOrder, double units)
    {
        if (units <= hubOrder || hubOrder <= 0)
        {
            return hubOrder <= 0 ? units * basePrice * Floor : units * basePrice;
        }
        var flood = FloodFrom * hubOrder;
        if (units <= flood)
        {
            return basePrice * Math.Sqrt(hubOrder * units);
        }
        return basePrice * Math.Sqrt(hubOrder * flood) + (units - flood) * basePrice * Floor;
    }
}

public sealed class ProgressionSettings
{
    public int MaxCompanyLevel { get; init; }

    /// <summary>Company Level from the gold earned in all (1.4 §14)</summary>
    public IReadOnlyList<LevelPiece> LevelPieces { get; init; } = [];

    public int MaxBuildingLevel { get; init; }
    public double OutputPerLevel { get; init; }
    public double CostPerLevel { get; init; }

    /// <summary>The stars a building shows (every fifth level): looks only</summary>
    public IReadOnlyList<int> Milestones { get; init; } = [];

    public double CostPerCopy { get; init; }

    /// <summary>From this level a step also takes goods worth LevelGoodsShare of its gold, of its building's Era</summary>
    public int LevelGoodsFrom { get; init; } = 11;
    public double LevelGoodsShare { get; init; } = 0.3;
    public IReadOnlyDictionary<int, IReadOnlyList<LevelGoodsBand>> LevelGoods { get; init; } = new Dictionary<int, IReadOnlyList<LevelGoodsBand>>();

    public IReadOnlyList<int> ParcelLevels { get; init; } = [];
    public double FirstParcelCost { get; init; }
    public double ParcelCostFactor { get; init; }
    public double FirstOutskirtsCost { get; init; } = 100_000_000;
    public double OutskirtsCostFactor { get; init; } = 1.4;
    public double OutskirtsBuyBack { get; init; } = 0.5;
    public IReadOnlyDictionary<int, int> ManagerSlots { get; init; } = new Dictionary<int, int>();
    public double StartingGold { get; init; }
    public double HubSpread { get; init; }
    public MarketSettings Market { get; init; } = new();

    /// <summary>Trading and moving (v1.2, 1.4 §8)</summary>
    public TogetherSettings Together { get; init; } = new();

    /// <summary>The gold earned in all that a Company Level needs (1.4 §14)</summary>
    public double EarnedForLevel(int level)
    {
        if (level <= 1 || LevelPieces.Count == 0)
        {
            return 0;
        }
        var piece = LevelPieces.Last(t => t.From <= level);
        return Math.Round(piece.Gold * Math.Pow(piece.Factor, level - piece.From));
    }

    public int LevelForEarned(double earned)
    {
        var level = 1;
        while (level < MaxCompanyLevel && earned >= EarnedForLevel(level + 1))
        {
            level++;
        }
        return level;
    }

    /// <summary>Output multiplier of a building at this level: +10% a level (9.85× at 25)</summary>
    public double OutputMultiplier(int level) => Math.Pow(OutputPerLevel, Math.Min(level, Math.Max(1, MaxBuildingLevel)) - 1);

    /// <summary>The gold of the step from a level to the next, for a copy that cost so much to build (1.4 §9)</summary>
    public double UpgradeCost(double copyPrice, int level) => Math.Round(Math.Max(copyPrice, 10) * Math.Pow(CostPerLevel, level - 1));

    /// <summary>The goods the step up to a level takes, for a building of an Era, worth LevelGoodsShare of the step's gold</summary>
    public IReadOnlyDictionary<string, double> LevelGoodsFor(int era, int toLevel, double stepGold, Func<string, double> basePrice)
    {
        if (toLevel < LevelGoodsFrom || !LevelGoods.TryGetValue(Math.Clamp(era, 1, 5), out var bands))
        {
            return new Dictionary<string, double>();
        }
        var band = bands.FirstOrDefault(t => toLevel <= t.UpTo) ?? bands[^1];
        var worth = stepGold * LevelGoodsShare / band.Goods.Count;
        return band.Goods.ToDictionary(t => t, t => Math.Ceiling(worth / Math.Max(0.01, basePrice(t))));
    }

    /// <summary>The gold for another copy of a building when you already own a number of them</summary>
    public double BuildCost(BuildingDefinition definition, int copiesOwned) =>
        Math.Round(definition.Cost.Gold * Math.Pow(CostPerCopy, copiesOwned));

    /// <summary>The cost of the n-th extra parcel (0 = the first one after the start parcels)</summary>
    public double ParcelCost(int extraParcelIndex) => Math.Round(FirstParcelCost * Math.Pow(ParcelCostFactor, extraParcelIndex));

    /// <summary>The cost of the n-th outskirts parcel (0 = the first)</summary>
    public double OutskirtsCost(int index) => Math.Round(FirstOutskirtsCost * Math.Pow(OutskirtsCostFactor, index));
}
