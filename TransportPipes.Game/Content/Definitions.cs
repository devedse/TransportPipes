using TransportPipes.Game.World;

namespace TransportPipes.Game.Content;

/// <summary>Demand: units per minute the hub wants per active company before its price drops</summary>
public sealed record ResourceDefinition(string Id, string Name, string Icon, string Color, int Tier, int Era, double Price, double Demand);

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
    TollMeter,
    CargoDepot,
    ExchangeOffice,
    Showroom,
    Workshop,
    WeatherStation,
    Recycler,
    ExpeditionOffice,
    BlueprintOffice,
    DeepLift,
    Bank,
    Teleporter,
    Terraformer,
    PowerLine,
    CorporationHq,
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
}

public sealed record BuildingUnlock(int Level, int Era, string? Research);

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
    public string? Sprite { get; init; }
    public string Description { get; init; } = "";

    /// <summary>Sits in a pipe and carries resources like a pipe tile (valves, toll meters)</summary>
    public bool IsConductor => Kind is BuildingKind.Gate or BuildingKind.Valve or BuildingKind.PriorityValve or BuildingKind.TollMeter or BuildingKind.PowerLine;
}

public enum WonderPerk
{
    None,
    HubDemand,
    Production,
    Research,
    Cargo,
}

/// <summary>A District Wonder (Era 0) or the Era Wonder of the Capital (GDD §9.3, §9.4)</summary>
public sealed class WonderDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int Era { get; init; }
    public WonderPerk Perk { get; init; }
    public double PerkAmount { get; init; }
    public string Description { get; init; } = "";

    /// <summary>What every stage needs, for 8 active companies</summary>
    public required IReadOnlyList<IReadOnlyDictionary<string, double>> Stages { get; init; }
}

public enum ResearchBranch
{
    Energy,
    Industry,
    Logistics,
    Economy,
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

public sealed class ResearchDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public ResearchBranch Branch { get; init; }
    public int Era { get; init; } = 1;

    /// <summary>The Company Level needed to start it (late-game techs)</summary>
    public int Level { get; init; }

    /// <summary>Research points, before knowledge diffusion</summary>
    public double Cost { get; init; }

    public double Minutes { get; init; }
    public IReadOnlyList<string> Requires { get; init; } = [];
    public IReadOnlyList<OutputBonus> OutputBonuses { get; init; } = [];
    public double ThroughputBonus { get; init; }
    public double BandWidthBonus { get; init; }
    public int ManagerSlots { get; init; }
    public string Description { get; init; } = "";
}

public sealed class ProgressionSettings
{
    public int MaxCompanyLevel { get; init; }
    public double XpBase { get; init; }
    public double XpExponent { get; init; }
    public IReadOnlyDictionary<string, double> XpRewards { get; init; } = new Dictionary<string, double>();
    public int MaxBuildingLevel { get; init; }
    public double OutputPerLevel { get; init; }
    public double CostPerLevel { get; init; }
    public IReadOnlyList<int> Milestones { get; init; } = [];
    public double CostPerCopy { get; init; }
    public IReadOnlyList<int> StartParcels { get; init; } = [];
    public IReadOnlyList<int> ParcelOrder { get; init; } = [];
    public IReadOnlyList<int> ParcelLevels { get; init; } = [];
    public double FirstParcelCost { get; init; }
    public double ParcelCostFactor { get; init; }
    public IReadOnlyDictionary<int, int> ManagerSlots { get; init; } = new Dictionary<int, int>();
    public double StartingGold { get; init; }
    public double HubSpread { get; init; }

    public double XpReward(string key) => XpRewards.TryGetValue(key, out var value) ? value : 0;

    /// <summary>Total XP needed to reach a Company Level</summary>
    public double XpForLevel(int level) => level <= 1 ? 0 : Math.Round(XpBase * Math.Pow(level - 1, XpExponent));

    public int LevelForXp(double xp)
    {
        var level = 1;
        while (level < MaxCompanyLevel && xp >= XpForLevel(level + 1))
        {
            level++;
        }
        return level;
    }

    /// <summary>Output multiplier of a building at this level (milestones double the output)</summary>
    public double OutputMultiplier(int level)
    {
        var milestones = Milestones.Count(t => level >= t);
        return Math.Pow(OutputPerLevel, level - 1) * Math.Pow(2, milestones);
    }

    /// <summary>The gold to upgrade from this level to the next</summary>
    public double UpgradeCost(BuildingDefinition definition, int level) =>
        Math.Round(Math.Max(definition.Cost.Gold, 10) * Math.Pow(CostPerLevel, level - 1));

    /// <summary>The gold for another copy of a building when you already own a number of them</summary>
    public double BuildCost(BuildingDefinition definition, int copiesOwned) =>
        Math.Round(definition.Cost.Gold * Math.Pow(CostPerCopy, copiesOwned));

    /// <summary>The cost of the n-th extra parcel (0 = the first one after the start parcels)</summary>
    public double ParcelCost(int extraParcelIndex) => Math.Round(FirstParcelCost * Math.Pow(ParcelCostFactor, extraParcelIndex));
}
