using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

public enum TerraformAction
{
    Drain,
    Lake,
    Raise,
    Flatten,
    Forest,
    SeedDeposit,
}

/// <summary>
/// Denser (GDD §11.1): complexes, tile efficiency and terraforming. A complex is a 1×1 building type standing as one
/// 2×2 (or 3×3) building: four (nine) merged buildings with the output of five (twelve), sharing one level.
/// </summary>
public sealed partial class GameWorld
{
    public const double SeedCost = 20_000;
    public const double SeedSteel = 50;

    /// <summary>Deposits seeded by terraforming: a resource and its stars</summary>
    private readonly Dictionary<TilePos, (string Deposit, int Richness)> _seeds = new();

    public IReadOnlyDictionary<TilePos, (string Deposit, int Richness)> Seeds => _seeds;

    public void LoadSeeds(IEnumerable<(TilePos Pos, string Deposit, int Richness)> seeds)
    {
        foreach (var (pos, deposit, richness) in seeds)
        {
            _seeds[pos] = (deposit, richness);
        }
    }

    // ---------------------------------------------------------------- Complexes

    /// <summary>How many 1×1 buildings a complex stands for (1 for an ordinary building)</summary>
    public int ComplexMembers(Building building) =>
        Content.Building(building.Type).Size == 1 && building.Size > 1 ? building.Size * building.Size : 1;

    /// <summary>The output factor of a complex: five for 2×2, twelve for 3×3</summary>
    public static double ComplexFactor(int members) => members switch
    {
        4 => 5,
        9 => 12,
        _ => 1,
    };

    private bool CanBeComplex(BuildingDefinition definition) =>
        definition.Size == 1 && definition.Kind is BuildingKind.Producer or BuildingKind.Lab && (definition.Out.Count != 0 || definition.ResearchPerMinute > 0);

    /// <summary>Merges a finished building with its identical neighbours into a complex when they form a square</summary>
    private void TryMergeComplex(Building building)
    {
        var definition = Content.Building(building.Type);
        if (building.UnderConstruction || building.Size != 1 || !CanBeComplex(definition) || Company(building.Owner) is not { } owner)
        {
            return;
        }
        foreach (var size in new[] { 3, 2 })
        {
            if (!HasResearch(owner, size == 3 ? "megaComplexes" : "complexes"))
            {
                continue;
            }
            for (var dy = 0; dy < size; dy++)
            {
                for (var dx = 0; dx < size; dx++)
                {
                    var origin = building.Position.Offset(-dx, -dy);
                    var members = new List<Building>();
                    for (var y = 0; y < size && members.Count == y * size; y++)
                    {
                        for (var x = 0; x < size; x++)
                        {
                            if (BuildingAt(origin.Offset(x, y)) is { Size: 1, UnderConstruction: false } member && member.Type == building.Type && member.Owner == building.Owner)
                            {
                                members.Add(member);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    if (members.Count == size * size)
                    {
                        Merge(owner, members, origin, size);
                        return;
                    }
                }
            }
        }
    }

    private void Merge(Company owner, List<Building> members, TilePos origin, int size)
    {
        var type = members[0].Type;
        var level = members.Min(t => t.Level);
        foreach (var member in members)
        {
            RemoveBuilding(member);
        }
        var complex = new Building { Id = NextBuildingId++, Type = type, Owner = owner.Id, X = origin.X, Y = origin.Y, Size = size, Level = level };
        _buildings[complex.Id] = complex;
        foreach (var tile in complex.Footprint())
        {
            _buildingAt[tile] = complex.Id;
            Changes.Tiles.Add(tile);
        }
        MarkDirty(complex);
        Changes.Events.Add(new WorldEvent(owner.Id, WorldEventKind.ConstructionFinished,
            $"{size * size} {Content.Building(type).Name}s merged into a {(size == 3 ? "mega complex" : "complex")}: the output of {ComplexFactor(size * size)}", complex.Id));
    }

    /// <summary>For testing: every tech at once</summary>
    public void GrantAllResearch(Company company)
    {
        company.Researched.UnionWith(Content.ResearchList.Select(t => t.Id));
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkCompanyNetworksDirty(company.Id);
        MergeAllComplexes(company);
    }

    /// <summary>After Complexes research: every square of identical buildings merges</summary>
    private void MergeAllComplexes(Company company)
    {
        foreach (var building in BuildingsOf(company.Id).Where(t => t.Size == 1).OrderBy(t => t.Y).ThenBy(t => t.X).ToList())
        {
            if (_buildings.ContainsKey(building.Id))
            {
                TryMergeComplex(building);
            }
        }
    }

    // ---------------------------------------------------------------- Tile efficiency

    /// <summary>Gold per estate tile per hour: how well a company uses its land</summary>
    public double TileEfficiency(Company company, IReadOnlyDictionary<long, double>? goldPerMinute = null)
    {
        var tiles = EstatesOf(company).Sum(t => t.UnlockedCount) * WorldLayout.ParcelSize * WorldLayout.ParcelSize;
        var gold = (goldPerMinute ?? GoldPerMinute()).GetValueOrDefault(company.Id);
        return tiles == 0 ? 0 : Math.Round(gold * 60 / tiles, 2);
    }

    // ---------------------------------------------------------------- Terraforming

    public static double TerraformCost(TerraformAction action) => action switch
    {
        TerraformAction.Drain => 2000,
        TerraformAction.Lake => 1000,
        TerraformAction.Raise => 3000,
        TerraformAction.Flatten => 1500,
        TerraformAction.Forest => 500,
        _ => SeedCost,
    };

    /// <summary>Reshapes a tile of your estate with a Terraformer (GDD §11.1)</summary>
    public CommandResult Terraform(long companyId, TilePos tile, TerraformAction action, string? resource = null)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (!HasBuilding(company.Id, BuildingKind.Terraformer))
        {
            return CommandResult.Fail("Terraforming needs a Terraformer.");
        }
        if (WorldLayout.IsDeep(tile) || OwnEstateAt(company, tile) is not { } estate || !estate.IsUnlocked(estate.ParcelAt(tile)))
        {
            return CommandResult.Fail("Only on your own unlocked land.");
        }
        if (IsOccupied(tile))
        {
            return CommandResult.Fail("Something is standing there.");
        }
        var info = Tile(tile);
        TerrainType? terrain = action switch
        {
            TerraformAction.Drain when info.Terrain == TerrainType.Water => TerrainType.Grass,
            TerraformAction.Lake when info.Terrain is TerrainType.Grass or TerrainType.Desert or TerrainType.Beach => TerrainType.Water,
            TerraformAction.Raise when info.Terrain is TerrainType.Grass or TerrainType.Desert or TerrainType.Beach => TerrainType.Hills,
            TerraformAction.Flatten when info.Terrain is TerrainType.Hills or TerrainType.Mountain => TerrainType.Grass,
            TerraformAction.Forest when info.Terrain is TerrainType.Grass => TerrainType.Forest,
            _ => null,
        };
        var materials = new Dictionary<string, double>();
        if (action == TerraformAction.SeedDeposit)
        {
            if (resource == null || !Content.Resources.TryGetValue(resource, out var definition) || definition.Tier != 1 || resource is "energy" or "water")
            {
                return CommandResult.Fail("Choose a raw resource to seed.");
            }
            if (info.Deposit != null && info.Deposit != resource)
            {
                return CommandResult.Fail($"There is a {Content.Resources[info.Deposit].Name.ToLowerInvariant()} deposit here.");
            }
            if (info.Richness >= 5)
            {
                return CommandResult.Fail("This deposit is as rich as it gets.");
            }
            materials["steel"] = SeedSteel;
        }
        else if (terrain == null)
        {
            return CommandResult.Fail("That doesn't work on this kind of land.");
        }
        var payment = Pay(company, TerraformCost(action), materials);
        if (!payment.Success)
        {
            return payment;
        }
        if (terrain is { } changed)
        {
            _terrainOverrides[tile] = changed;
        }
        else
        {
            _seeds[tile] = (resource!, info.Deposit == null ? 1 : info.Richness + 1);
        }
        MarkDirty(tile);
        Changes.Tiles.Add(tile);
        Changes.Outposts = true;
        // Buildings next to it may produce differently now (wind, shores, forests, deposits)
        foreach (var neighbour in BuildingsOf(company.Id).Where(t => Math.Abs(t.X - tile.X) <= 4 && Math.Abs(t.Y - tile.Y) <= 4))
        {
            MarkNeedsSolve(neighbour.Id);
        }
        return CommandResult.Ok;
    }
}
