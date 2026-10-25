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
    public IReadOnlyDictionary<string, ResourceDefinition> Resources { get; }
    public IReadOnlyList<ResourceDefinition> ResourceList { get; }
    public IReadOnlyDictionary<string, BuildingDefinition> Buildings { get; }
    public IReadOnlyList<BuildingDefinition> BuildingList { get; }
    public IReadOnlyList<PipeTierDefinition> PipeTiers { get; }
    public ProgressionSettings Progression { get; }
    /// <summary>The Era tree (the Lab's second line)</summary>
    public IReadOnlyDictionary<string, ResearchDefinition> Research { get; }
    public IReadOnlyList<ResearchDefinition> ResearchList { get; }

    /// <summary>The tracks (the Lab's first line) and how their tiers work</summary>
    public IReadOnlyDictionary<string, TrackDefinition> Tracks { get; }
    public IReadOnlyList<TrackDefinition> TrackList { get; }
    public TrackRules TrackRules { get; }

    /// <summary>The Era Wonders by Era (1.4 §13)</summary>
    public IReadOnlyList<WonderDefinition> EraWonders { get; }

    /// <summary>The Monuments a Yard can build (1.4 §13)</summary>
    public MonumentRules Monuments { get; }

    public IReadOnlyList<ManagerDefinition> Managers { get; }
    public IReadOnlyList<AchievementDefinition> Achievements { get; }
    public IReadOnlyList<CollectionDefinition> Collections { get; }
    public IReadOnlyList<TutorialStep> Tutorial { get; }
    public IReadOnlyList<RelicDefinition> Relics { get; }

    public GameContent(
        IReadOnlyList<ResourceDefinition> resources,
        IReadOnlyList<BuildingDefinition> buildings,
        IReadOnlyList<PipeTierDefinition> pipeTiers,
        ProgressionSettings progression,
        IReadOnlyList<ResearchDefinition>? research = null,
        IReadOnlyList<WonderDefinition>? wonders = null,
        IReadOnlyList<ManagerDefinition>? managers = null,
        IReadOnlyList<AchievementDefinition>? achievements = null,
        IReadOnlyList<CollectionDefinition>? collections = null,
        IReadOnlyList<TutorialStep>? tutorial = null,
        IReadOnlyList<RelicDefinition>? relics = null,
        IReadOnlyList<TrackDefinition>? tracks = null,
        TrackRules? trackRules = null,
        MonumentRules? monuments = null)
    {
        Monuments = monuments ?? new MonumentRules();
        TrackList = tracks ?? [];
        Tracks = TrackList.ToDictionary(t => t.Id);
        TrackRules = trackRules ?? new TrackRules();
        Tutorial = tutorial ?? [];
        Relics = relics ?? [];
        Managers = managers ?? [];
        Achievements = achievements ?? [];
        Collections = collections ?? [];
        EraWonders = wonders?.OrderBy(t => t.Era).ToList() ?? [];
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

    /// <summary>A copy with some parts replaced (tests, the bots' what-ifs)</summary>
    public GameContent With(
        IReadOnlyList<ResourceDefinition>? resources = null,
        IReadOnlyList<BuildingDefinition>? buildings = null,
        IReadOnlyList<PipeTierDefinition>? pipeTiers = null,
        ProgressionSettings? progression = null,
        IReadOnlyList<TutorialStep>? tutorial = null,
        TrackRules? trackRules = null,
        IReadOnlyList<WonderDefinition>? wonders = null) =>
        new(resources ?? ResourceList, buildings ?? BuildingList, pipeTiers ?? PipeTiers, progression ?? Progression, ResearchList, wonders ?? EraWonders, Managers,
            Achievements, Collections, tutorial ?? Tutorial, Relics, TrackList, trackRules ?? TrackRules, Monuments);

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
        var research = Read<ResearchFileDto>("research.yaml");
        var wonders = Read<WondersDto>("wonders.yaml");
        var managers = Read<List<ManagerDto>>("managers.yaml").Select(t => new ManagerDefinition(t.Id, t.Name, t.Cost, t.Color, t.Automation, t.Bonus)).ToList();
        var achievements = Read<AchievementsDto>("achievements.yaml");
        var tutorial = Read<List<TutorialDto>>("tutorial.yaml").Select(t => new TutorialStep(t.Id, t.Text, t.Stat, t.Amount, t.Gold, t.Hint, t.Chapter, t.PhoneText)).ToList();
        var relics = Read<List<RelicDto>>("relics.yaml").Select(t => new RelicDefinition(t.Id, t.Name, t.Lore)).ToList();
        return new GameContent(
            resources,
            buildings,
            pipes,
            progression,
            research.Tree.Select(t => t.ToDefinition()).ToList(),
            wonders.Era.Select(t => t.ToDefinition()).ToList(),
            managers,
            achievements.Achievements.Select(t => t.ToDefinition()).ToList(),
            achievements.Collections.Select(t => new CollectionDefinition(t.Id, t.Name, t.Level, t.Buildings, t.Hours, t.Title, t.Description)).ToList(),
            tutorial,
            relics,
            research.Tracks.Select(t => t.ToDefinition()).ToList(),
            research.ToRules(),
            wonders.Monuments.ToRules());
    }

    private void Validate()
    {
        var errors = new List<string>();
        var specialDeposits = new HashSet<string> { "anyMetal", "geothermal", "magma" };

        void CheckResource(string owner, string resource)
        {
            if (!Resources.ContainsKey(resource))
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
            if (building.EvolvesTo is { } next && (!Buildings.TryGetValue(next, out var successor) || successor.Size < building.Size))
            {
                errors.Add($"{building.Id}: evolves into '{next}', which must be a building at least as big");
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
        foreach (var track in TrackList)
        {
            foreach (var building in track.Buildings)
            {
                var (id, deposit) = SplitTrackBuilding(building);
                if (!Buildings.ContainsKey(id) || (deposit != null && !Resources.ContainsKey(deposit)))
                {
                    errors.Add($"track {track.Id}: unknown building '{building}'");
                }
            }
            foreach (var good in track.Goods.Append(track.Good).OfType<string>())
            {
                CheckResource($"track {track.Id}", good);
            }
        }
        if (TrackList.Count != 0 && TrackRules.Tiers.Count != 9)
        {
            errors.Add("A track has nine tiers");
        }
        if (ResearchList.Select(t => t.Id).Concat(TrackList.Select(t => t.Id)).Distinct().Count() != ResearchList.Count + TrackList.Count)
        {
            errors.Add("Research ids must be unique");
        }

        foreach (var wonder in EraWonders)
        {
            if (wonder.Stages.Count == 0 || wonder.Stages.Any(t => t.Goods.Count == 0))
            {
                errors.Add($"wonder {wonder.Id}: needs stages with goods");
            }
            if (Math.Abs(wonder.Stages.Sum(t => t.Share) - 1) > 0.01)
            {
                errors.Add($"wonder {wonder.Id}: its stages' shares add up to one");
            }
            foreach (var resource in wonder.Stages.SelectMany(t => t.Goods))
            {
                CheckResource($"wonder {wonder.Id}", resource);
            }
        }
        if (Monuments.Kinds.Select(t => t.Id).Distinct().Count() != Monuments.Kinds.Count)
        {
            errors.Add("Monument ids must be unique");
        }
        foreach (var collection in Collections)
        {
            foreach (var building in collection.Buildings.Where(t => !Buildings.ContainsKey(t)))
            {
                errors.Add($"collection {collection.Id}: unknown building '{building}'");
            }
        }
        foreach (var building in BuildingList.Where(t => t.Unlock.Collection != null && Collections.All(c => c.Id != t.Unlock.Collection)))
        {
            errors.Add($"{building.Id}: unknown collection '{building.Unlock.Collection}'");
        }
        if (Achievements.Select(t => t.Id).Distinct().Count() != Achievements.Count || Managers.Select(t => t.Id).Distinct().Count() != Managers.Count)
        {
            errors.Add("Achievement and manager ids must be unique");
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

    /// <summary>"deepMine:coal": a building on a deposit; "smelter": the building wherever it stands</summary>
    public static (string Building, string? Deposit) SplitTrackBuilding(string entry)
    {
        var colon = entry.IndexOf(':');
        return colon < 0 ? (entry, null) : (entry[..colon], entry[(colon + 1)..]);
    }

    private sealed class ResourceDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "#ffffff";
        public int Tier { get; set; } = 1;
        public int Era { get; set; } = 1;
        public double Price { get; set; }
        public string Kind { get; set; } = "solid";

        public ResourceDefinition ToDefinition() => new(Id, Name, Icon, Color, Tier, Era, Price, Kind);
    }

    private sealed class UnlockDto
    {
        public int Level { get; set; } = 1;
        public int Era { get; set; } = 1;
        public string? Research { get; set; }
        public string? Collection { get; set; }
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
        public string? EvolvesTo { get; set; }
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
            Unlock = new BuildingUnlock(Unlock.Level, Unlock.Era, Unlock.Research, Unlock.Collection),
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
            EvolvesTo = EvolvesTo,
            Sprite = Sprite,
            Description = Description,
        };
    }

    private sealed class ManagerDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public double Cost { get; set; }
        public string Color { get; set; } = "#888888";
        public string Automation { get; set; } = "";
        public string Bonus { get; set; } = "";
    }

    private sealed class RelicDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Lore { get; set; } = "";
    }

    private sealed class TutorialDto
    {
        public string Id { get; set; } = "";
        public string Text { get; set; } = "";
        public string Stat { get; set; } = "";
        public double Amount { get; set; }
        public double Gold { get; set; }
        public Dictionary<string, string> Hint { get; set; } = new();
        public int Chapter { get; set; } = 1;
        public string? PhoneText { get; set; }
    }

    private sealed class AchievementsDto
    {
        public List<AchievementDto> Achievements { get; set; } = new();
        public List<CollectionDto> Collections { get; set; } = new();
    }

    private sealed class AchievementDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Stat { get; set; } = "";
        public double Amount { get; set; }
        public double Hours { get; set; }
        public double Gold { get; set; }
        public string? Title { get; set; }
        public OutputBonusDto? Output { get; set; }
        public string Description { get; set; } = "";

        public AchievementDefinition ToDefinition() =>
            new(Id, Name, Stat, Amount, Hours, Gold, Title, Output == null ? null : new OutputBonus(Output.Targets, Output.Amount), Description);
    }

    private sealed class OutputBonusDto
    {
        public List<string> Targets { get; set; } = new();
        public double Amount { get; set; }
    }

    private sealed class CollectionDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public List<string> Buildings { get; set; } = new();
        public double Hours { get; set; }
        public string? Title { get; set; }
        public string Description { get; set; } = "";
    }

    private sealed class WondersDto
    {
        public List<WonderDto> Era { get; set; } = new();
        public MonumentsDto Monuments { get; set; } = new();
    }

    private sealed class WonderDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Era { get; set; }
        public string Description { get; set; } = "";
        public double Worth { get; set; }
        public List<WonderStageDto> Stages { get; set; } = new();

        public WonderDefinition ToDefinition() => new()
        {
            Id = Id,
            Name = Name,
            Era = Era,
            Description = Description,
            Worth = Worth,
            Stages = Stages.Select(t => new WonderStage(t.Share, t.Goods,
                t.Perk == null ? WonderPerk.None : Enum.Parse<WonderPerk>(t.Perk.Kind, ignoreCase: true), t.Perk?.Amount ?? 0)).ToList(),
        };
    }

    private sealed class WonderStageDto
    {
        public double Share { get; set; }
        public List<string> Goods { get; set; } = new();
        public WonderPerkDto? Perk { get; set; }
    }

    private sealed class WonderPerkDto
    {
        public string Kind { get; set; } = "";
        public double Amount { get; set; }
    }

    private sealed class MonumentsDto
    {
        public double FirstStage { get; set; } = 100_000_000;
        public double EraFactor { get; set; } = 10;
        public double StageFactor { get; set; } = 1.4;
        public int Level { get; set; } = 5;
        public List<MonumentKindDto> Kinds { get; set; } = new();

        public MonumentRules ToRules() => new()
        {
            FirstStage = FirstStage,
            EraFactor = EraFactor,
            StageFactor = StageFactor,
            Level = Level,
            Kinds = Kinds.Select(t => new MonumentKind(t.Id, t.Name, Enum.Parse<MonumentPerk>(t.Perk, ignoreCase: true), t.Amount, t.Cap, t.Description)).ToList(),
        };
    }

    private sealed class MonumentKindDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Perk { get; set; } = "production";
        public double Amount { get; set; }
        public int Cap { get; set; }
        public string Description { get; set; } = "";
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

    private sealed class ResearchFileDto
    {
        public List<TierDto> Tiers { get; set; } = new();
        public int TierGoodsFrom { get; set; } = 3;
        public double TierGoodsShare { get; set; } = 0.2;
        public int HubOrderFrom { get; set; } = 5;
        public double HubOrderPerTier { get; set; } = 1.8;
        public List<double> LeanerOutput { get; set; } = new();
        public List<TrackDto> Tracks { get; set; } = new();
        public List<TreeDto> Tree { get; set; } = new();

        public TrackRules ToRules() => new()
        {
            Tiers = Tiers.Select((t, i) => new TrackTier(i + 1, t.Minutes, Enum.Parse<TierEffect>(t.Effect, ignoreCase: true), t.Amount, t.Gold)).ToList(),
            TierGoodsFrom = TierGoodsFrom,
            TierGoodsShare = TierGoodsShare,
            HubOrderFrom = HubOrderFrom,
            HubOrderPerTier = HubOrderPerTier,
            LeanerOutput = LeanerOutput,
        };
    }

    private sealed class TierDto
    {
        public double Minutes { get; set; }
        public string Effect { get; set; } = "faster";
        public double Amount { get; set; }
        public double Gold { get; set; }
    }

    private sealed class TrackDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Era { get; set; } = 1;
        public double Strength { get; set; } = 1;
        public string? Good { get; set; }
        public List<string> Goods { get; set; } = new();
        public List<string> Buildings { get; set; } = new();
        public string Description { get; set; } = "";

        public TrackDefinition ToDefinition() => new()
        {
            Id = Id,
            Name = Name,
            Era = Era,
            Strength = Strength,
            Good = Good,
            Goods = Goods,
            Buildings = Buildings,
            Description = Description,
        };
    }

    private sealed class TreeDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Era { get; set; } = 1;
        public double Minutes { get; set; }
        public double Gold { get; set; }
        public int Grant { get; set; }
        public int ManagerSlots { get; set; }
        public string Description { get; set; } = "";

        public ResearchDefinition ToDefinition() => new()
        {
            Id = Id,
            Name = Name,
            Era = Era,
            Minutes = Minutes,
            Gold = Gold,
            Grant = Grant,
            ManagerSlots = ManagerSlots,
            Description = Description,
        };
    }

    private sealed class ProgressionDto
    {
        public CompanyLevelDto CompanyLevel { get; set; } = new();
        public BuildingLevelsDto BuildingLevels { get; set; } = new();
        public Dictionary<int, List<LevelGoodsDto>> LevelGoods { get; set; } = new();
        public ParcelsDto Parcels { get; set; } = new();
        public OutskirtsDto Outskirts { get; set; } = new();
        public Dictionary<int, int> ManagerSlots { get; set; } = new();
        public double StartingGold { get; set; }
        public double HubSpread { get; set; } = 2.5;
        public TogetherDto Together { get; set; } = new();
        public MarketDto Market { get; set; } = new();

        public ProgressionSettings ToSettings() => new()
        {
            Together = Together.ToSettings(),
            MaxCompanyLevel = CompanyLevel.MaxLevel,
            LevelPieces = CompanyLevel.GoldPieces.Select(t => new LevelPiece(t.From, t.Gold, t.Factor)).ToList(),
            MaxBuildingLevel = BuildingLevels.MaxLevel,
            OutputPerLevel = BuildingLevels.OutputPerLevel,
            CostPerLevel = BuildingLevels.CostPerLevel,
            Milestones = BuildingLevels.Milestones,
            CostPerCopy = BuildingLevels.CostPerCopy,
            LevelGoodsFrom = BuildingLevels.LevelGoodsFrom,
            LevelGoodsShare = BuildingLevels.LevelGoodsShare,
            LevelGoods = LevelGoods.ToDictionary(t => t.Key, t => (IReadOnlyList<LevelGoodsBand>)t.Value.Select(b => new LevelGoodsBand(b.UpTo, b.Goods)).ToList()),
            ParcelLevels = Parcels.Levels,
            FirstParcelCost = Parcels.FirstCost,
            ParcelCostFactor = Parcels.CostFactor,
            FirstOutskirtsCost = Outskirts.FirstCost,
            OutskirtsCostFactor = Outskirts.CostFactor,
            OutskirtsBuyBack = Outskirts.BuyBack,
            ManagerSlots = ManagerSlots,
            StartingGold = StartingGold,
            HubSpread = HubSpread,
            Market = Market.ToSettings(),
        };
    }

    private sealed class TogetherDto
    {
        public double PlayerPrice { get; set; } = 1.3;
        public double MoveCooldownMinutes { get; set; } = 5;
        public double MoveFlightMinutes { get; set; } = 1;
        public double MoveCostPerLevel { get; set; }

        public TogetherSettings ToSettings() => new()
        {
            PlayerPrice = PlayerPrice,
            MoveCooldownMinutes = MoveCooldownMinutes,
            MoveFlightMinutes = MoveFlightMinutes,
            MoveCostPerLevel = MoveCostPerLevel,
        };
    }

    private sealed class MarketDto
    {
        public double HubOrderValue { get; set; } = 1000;
        public double EraValue { get; set; } = 4;
        public double EraGrowth { get; set; } = 1.5;
        public double PostLevel { get; set; } = 1.1;
        public double FloodFrom { get; set; } = 16;
        public double Floor { get; set; } = 0.25;

        public MarketSettings ToSettings() => new()
        {
            HubOrderValue = HubOrderValue,
            EraValue = EraValue,
            EraGrowth = EraGrowth,
            PostLevel = PostLevel,
            FloodFrom = FloodFrom,
            Floor = Floor,
        };
    }

    private sealed class CompanyLevelDto
    {
        public int MaxLevel { get; set; } = 60;
        public List<LevelPieceDto> GoldPieces { get; set; } = new();
    }

    private sealed class LevelPieceDto
    {
        public int From { get; set; }
        public double Gold { get; set; }
        public double Factor { get; set; }
    }

    private sealed class BuildingLevelsDto
    {
        public int MaxLevel { get; set; } = 25;
        public double OutputPerLevel { get; set; } = 1.1;
        public double CostPerLevel { get; set; } = 1.15;
        public List<int> Milestones { get; set; } = new();
        public double CostPerCopy { get; set; } = 1.07;
        public int LevelGoodsFrom { get; set; } = 11;
        public double LevelGoodsShare { get; set; } = 0.3;
    }

    private sealed class LevelGoodsDto
    {
        public int UpTo { get; set; }
        public List<string> Goods { get; set; } = new();
    }

    private sealed class ParcelsDto
    {
        public List<int> Levels { get; set; } = new();
        public double FirstCost { get; set; }
        public double CostFactor { get; set; } = 2;
    }

    private sealed class OutskirtsDto
    {
        public double FirstCost { get; set; } = 100_000_000;
        public double CostFactor { get; set; } = 1.4;
        public double BuyBack { get; set; } = 0.5;
    }
}
