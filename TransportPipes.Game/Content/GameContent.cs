using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Content;

/// <summary>
/// All game data (resources, buildings, pipes, progression), loaded from the YAML files in Content/.
/// </summary>
public sealed class GameContent
{
    /// <summary>Pseudo resource for research points made by labs; it is credited to the company, it never flows.</summary>
    public const string ResearchPoints = "research";

    public IReadOnlyDictionary<string, ResourceDefinition> Resources { get; }
    public IReadOnlyList<ResourceDefinition> ResourceList { get; }
    public IReadOnlyDictionary<string, BuildingDefinition> Buildings { get; }
    public IReadOnlyList<BuildingDefinition> BuildingList { get; }
    public IReadOnlyList<PipeTierDefinition> PipeTiers { get; }
    public ProgressionSettings Progression { get; }
    public IReadOnlyDictionary<string, ResearchDefinition> Research { get; }
    public IReadOnlyList<ResearchDefinition> ResearchList { get; }

    /// <summary>District Wonders (one is picked per district) and the Era Wonders by Era</summary>
    public IReadOnlyList<WonderDefinition> DistrictWonders { get; }
    public IReadOnlyList<WonderDefinition> EraWonders { get; }

    public GameContent(IReadOnlyList<ResourceDefinition> resources, IReadOnlyList<BuildingDefinition> buildings, IReadOnlyList<PipeTierDefinition> pipeTiers, ProgressionSettings progression, IReadOnlyList<ResearchDefinition>? research = null, IReadOnlyList<WonderDefinition>? wonders = null)
    {
        DistrictWonders = wonders?.Where(t => t.Era == 0).ToList() ?? [];
        EraWonders = wonders?.Where(t => t.Era > 0).OrderBy(t => t.Era).ToList() ?? [];
        ResearchList = research ?? [];
        Research = ResearchList.ToDictionary(t => t.Id);
        ResourceList = resources;
        Resources = resources.ToDictionary(t => t.Id);
        BuildingList = buildings;
        Buildings = buildings.ToDictionary(t => t.Id);
        PipeTiers = pipeTiers.OrderBy(t => t.Tier).ToList();
        Progression = progression;
        Validate();
    }

    public BuildingDefinition Building(string id) =>
        Buildings.TryGetValue(id, out var definition) ? definition : throw new KeyNotFoundException($"Unknown building '{id}'.");

    public PipeTierDefinition PipeTier(int tier) =>
        PipeTiers.FirstOrDefault(t => t.Tier == tier) ?? throw new KeyNotFoundException($"Unknown pipe tier {tier}.");

    private static GameContent? _default;

    /// <summary>The content embedded in this assembly (cached, it never changes at runtime).</summary>
    public static GameContent Default => _default ??= LoadEmbedded();

    public static GameContent LoadEmbedded()
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        T Read<T>(string file)
        {
            var assembly = typeof(GameContent).Assembly;
            using var stream = assembly.GetManifestResourceStream($"Content.{file}")
                ?? throw new InvalidOperationException($"Embedded content file {file} not found.");
            using var reader = new StreamReader(stream);
            return deserializer.Deserialize<T>(reader);
        }

        var resources = Read<List<ResourceDto>>("resources.yaml").Select(t => t.ToDefinition()).ToList();
        var buildings = Read<List<BuildingDto>>("buildings.yaml").Select(t => t.ToDefinition()).ToList();
        var pipes = Read<List<PipeTierDto>>("pipes.yaml").Select(t => t.ToDefinition()).ToList();
        var progression = Read<ProgressionDto>("progression.yaml").ToSettings();
        var research = Read<List<ResearchDto>>("research.yaml").Select(t => t.ToDefinition()).ToList();
        var wonders = Read<WondersDto>("wonders.yaml");
        return new GameContent(resources, buildings, pipes, progression, research, [.. wonders.District.Select(t => t.ToDefinition()), .. wonders.Era.Select(t => t.ToDefinition())]);
    }

    private void Validate()
    {
        var errors = new List<string>();
        var specialDeposits = new HashSet<string> { "anyMetal", "geothermal", "magma" };

        void CheckResource(string owner, string resource)
        {
            if (resource != ResearchPoints && !Resources.ContainsKey(resource))
            {
                errors.Add($"{owner}: unknown resource '{resource}'");
            }
        }

        foreach (var building in BuildingList)
        {
            if (building.Size is < 1 or > 3)
            {
                errors.Add($"{building.Id}: size must be 1, 2 or 3");
            }
            foreach (var resource in building.In.Keys.Concat(building.Out.Keys).Concat(building.Cost.Materials.Keys).Concat(building.OutFromDeposit))
            {
                CheckResource(building.Id, resource);
            }
            if (building.Placement.Deposit is { } deposit && !specialDeposits.Contains(deposit))
            {
                CheckResource(building.Id, deposit);
            }
        }
        foreach (var pipe in PipeTiers)
        {
            foreach (var resource in pipe.Materials.Keys)
            {
                CheckResource($"pipe {pipe.Id}", resource);
            }
        }
        if (BuildingList.Select(t => t.Id).Distinct().Count() != BuildingList.Count)
        {
            errors.Add("Building ids must be unique");
        }

        void CheckResearch(string owner, string? research)
        {
            if (research != null && ResearchList.Count != 0 && !Research.ContainsKey(research))
            {
                errors.Add($"{owner}: unknown research '{research}'");
            }
        }
        foreach (var building in BuildingList)
        {
            CheckResearch(building.Id, building.Unlock.Research);
        }
        foreach (var pipe in PipeTiers)
        {
            CheckResearch($"pipe {pipe.Id}", pipe.Research);
        }
        foreach (var tech in ResearchList)
        {
            foreach (var required in tech.Requires)
            {
                CheckResearch($"research {tech.Id}", required);
            }
            foreach (var target in tech.OutputBonuses.SelectMany(t => t.Targets))
            {
                var colon = target.IndexOf(':');
                var kind = colon < 0 ? "" : target[..colon];
                var value = colon < 0 ? "" : target[(colon + 1)..];
                var known = kind switch
                {
                    "modifier" => Enum.TryParse<ProductionModifier>(value, true, out _),
                    "building" => Buildings.ContainsKey(value),
                    "category" => BuildingList.Any(t => t.Category == value),
                    "resource" => Resources.ContainsKey(value),
                    _ => false,
                };
                if (!known)
                {
                    errors.Add($"research {tech.Id}: unknown bonus target '{target}'");
                }
            }
        }
        if (ResearchList.Select(t => t.Id).Distinct().Count() != ResearchList.Count)
        {
            errors.Add("Research ids must be unique");
        }

        foreach (var wonder in DistrictWonders.Concat(EraWonders))
        {
            if (wonder.Stages.Count == 0)
            {
                errors.Add($"wonder {wonder.Id}: needs stages");
            }
            foreach (var resource in wonder.Stages.SelectMany(t => t.Keys))
            {
                CheckResource($"wonder {wonder.Id}", resource);
            }
        }
        if (EraWonders.Count != 0 && !EraWonders.Select(t => t.Era).SequenceEqual(Enumerable.Range(1, EraWonders.Count)))
        {
            errors.Add("There is one Era Wonder per Era, from Era 1");
        }

        if (errors.Count != 0)
        {
            throw new InvalidOperationException("Invalid game content:" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }
    }

    // ---------------------------------------------------------------- YAML shapes

    private sealed class ResourceDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "#ffffff";
        public int Tier { get; set; } = 1;
        public int Era { get; set; } = 1;
        public double Price { get; set; }
        public double? Demand { get; set; }

        /// <summary>Without a demand the hub wants about 1,000 gold worth per minute per active company</summary>
        public ResourceDefinition ToDefinition() => new(Id, Name, Icon, Color, Tier, Era, Price, Demand ?? Math.Round(1000 / Math.Max(Price, 0.01)));
    }

    private sealed class UnlockDto
    {
        public int Level { get; set; } = 1;
        public int Era { get; set; } = 1;
        public string? Research { get; set; }
    }

    private sealed class CostDto
    {
        public double Gold { get; set; }
        public Dictionary<string, double> Materials { get; set; } = new();
    }

    private sealed class PlacementDto
    {
        public List<string>? Terrain { get; set; }
        public string? Deposit { get; set; }
        public bool Shore { get; set; }
        public bool Forest { get; set; }
        public int MinRichness { get; set; }
    }

    private sealed class BuildingDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Kind { get; set; } = "producer";
        public int Size { get; set; } = 1;
        public bool Buildable { get; set; } = true;
        public UnlockDto Unlock { get; set; } = new();
        public CostDto Cost { get; set; } = new();
        public double BuildSeconds { get; set; }
        public Dictionary<string, double> In { get; set; } = new();
        public Dictionary<string, double> Out { get; set; } = new();
        public List<string> OutFromDeposit { get; set; } = new();
        public double Research { get; set; }
        public PlacementDto Placement { get; set; } = new();
        public string Modifier { get; set; } = "none";
        public double Capacity { get; set; }
        public double Throughput { get; set; }
        public double PriceBonus { get; set; }
        public string? Range { get; set; }
        public int Reach { get; set; }
        public string Layer { get; set; } = "surface";
        public string? Sprite { get; set; }
        public string Description { get; set; } = "";

        public BuildingDefinition ToDefinition() => new()
        {
            Id = Id,
            Name = Name,
            Category = Category,
            Kind = Enum.Parse<BuildingKind>(Kind, ignoreCase: true),
            Size = Size,
            Buildable = Buildable,
            Unlock = new BuildingUnlock(Unlock.Level, Unlock.Era, Unlock.Research),
            Cost = new BuildingCost(Cost.Gold, Cost.Materials),
            BuildSeconds = BuildSeconds,
            In = In,
            Out = Out,
            OutFromDeposit = OutFromDeposit,
            ResearchPerMinute = Research,
            Placement = new BuildingPlacement(
                Placement.Terrain?.Select(t => Enum.Parse<TerrainType>(t, ignoreCase: true)).ToList() ?? BuildingPlacement.DefaultTerrain,
                Placement.Deposit,
                Placement.Shore,
                Placement.Forest,
                Placement.MinRichness),
            Modifier = Enum.Parse<ProductionModifier>(Modifier, ignoreCase: true),
            Capacity = Capacity,
            Throughput = Throughput,
            PriceBonus = PriceBonus,
            Range = Range,
            Reach = Reach,
            Layer = Enum.Parse<BuildingLayer>(Layer, ignoreCase: true),
            Sprite = Sprite,
            Description = Description,
        };
    }

    private sealed class WondersDto
    {
        public List<WonderDto> District { get; set; } = new();
        public List<WonderDto> Era { get; set; } = new();
    }

    private sealed class WonderDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Era { get; set; }
        public WonderPerkDto? Perk { get; set; }
        public string Description { get; set; } = "";
        public List<Dictionary<string, double>> Stages { get; set; } = new();

        public WonderDefinition ToDefinition() => new()
        {
            Id = Id,
            Name = Name,
            Era = Era,
            Perk = Perk == null ? WonderPerk.None : Enum.Parse<WonderPerk>(Perk.Kind, ignoreCase: true),
            PerkAmount = Perk?.Amount ?? 0,
            Description = Description,
            Stages = Stages,
        };
    }

    private sealed class WonderPerkDto
    {
        public string Kind { get; set; } = "";
        public double Amount { get; set; }
    }

    private sealed class PipeTierDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Tier { get; set; }
        public int Era { get; set; } = 1;
        public double Gold { get; set; }
        public Dictionary<string, double> Materials { get; set; } = new();
        public double LossPerTile { get; set; }
        public double Throughput { get; set; }
        public string? Research { get; set; }

        public PipeTierDefinition ToDefinition() => new(Id, Name, Tier, Era, Gold, Materials, LossPerTile, Throughput, Research);
    }

    private sealed class AmountDto
    {
        public double Amount { get; set; }
    }

    private sealed class OutputBonusDto
    {
        public List<string> Targets { get; set; } = new();
        public double Amount { get; set; }
    }

    private sealed class ResearchEffectDto
    {
        public OutputBonusDto? Output { get; set; }
        public AmountDto? Throughput { get; set; }
        public AmountDto? BandWidth { get; set; }
        public AmountDto? ManagerSlots { get; set; }
    }

    private sealed class ResearchDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Branch { get; set; } = "energy";
        public int Era { get; set; } = 1;
        public int Level { get; set; }
        public double Cost { get; set; }
        public double Minutes { get; set; }
        public List<string> Requires { get; set; } = new();
        public List<ResearchEffectDto> Effects { get; set; } = new();
        public string Description { get; set; } = "";

        public ResearchDefinition ToDefinition() => new()
        {
            Id = Id,
            Name = Name,
            Branch = Enum.Parse<ResearchBranch>(Branch, ignoreCase: true),
            Era = Era,
            Level = Level,
            Cost = Cost,
            Minutes = Minutes,
            Requires = Requires,
            OutputBonuses = Effects.Where(t => t.Output != null).Select(t => new OutputBonus(t.Output!.Targets, t.Output.Amount)).ToList(),
            ThroughputBonus = Effects.Sum(t => t.Throughput?.Amount ?? 0),
            BandWidthBonus = Effects.Sum(t => t.BandWidth?.Amount ?? 0),
            ManagerSlots = (int)Effects.Sum(t => t.ManagerSlots?.Amount ?? 0),
            Description = Description,
        };
    }

    private sealed class ProgressionDto
    {
        public CompanyLevelDto CompanyLevel { get; set; } = new();
        public Dictionary<string, double> Xp { get; set; } = new();
        public BuildingLevelsDto BuildingLevels { get; set; } = new();
        public ParcelsDto Parcels { get; set; } = new();
        public Dictionary<int, int> ManagerSlots { get; set; } = new();
        public double StartingGold { get; set; }
        public double HubSpread { get; set; } = 2.5;

        public ProgressionSettings ToSettings() => new()
        {
            MaxCompanyLevel = CompanyLevel.MaxLevel,
            XpBase = CompanyLevel.XpBase,
            XpExponent = CompanyLevel.XpExponent,
            XpRewards = Xp,
            MaxBuildingLevel = BuildingLevels.MaxLevel,
            OutputPerLevel = BuildingLevels.OutputPerLevel,
            CostPerLevel = BuildingLevels.CostPerLevel,
            Milestones = BuildingLevels.Milestones,
            CostPerCopy = BuildingLevels.CostPerCopy,
            StartParcels = Parcels.Start,
            ParcelOrder = Parcels.Order,
            ParcelLevels = Parcels.Levels,
            FirstParcelCost = Parcels.FirstCost,
            ParcelCostFactor = Parcels.CostFactor,
            ManagerSlots = ManagerSlots,
            StartingGold = StartingGold,
            HubSpread = HubSpread,
        };
    }

    private sealed class CompanyLevelDto
    {
        public int MaxLevel { get; set; } = 60;
        public double XpBase { get; set; } = 100;
        public double XpExponent { get; set; } = 1.7;
    }

    private sealed class BuildingLevelsDto
    {
        public int MaxLevel { get; set; } = 100;
        public double OutputPerLevel { get; set; } = 1.1;
        public double CostPerLevel { get; set; } = 1.15;
        public List<int> Milestones { get; set; } = new();
        public double CostPerCopy { get; set; } = 1.07;
    }

    private sealed class ParcelsDto
    {
        public List<int> Start { get; set; } = new();
        public List<int> Order { get; set; } = new();
        public List<int> Levels { get; set; } = new();
        public double FirstCost { get; set; }
        public double CostFactor { get; set; } = 2;
    }
}
