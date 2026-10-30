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
/// 2×2 (or 3×3) building: four (nine) merged buildings with the output of five (twelve), sharing one level. They come
/// with a track's tier 4 (8) for its buildings (1.4 §12).
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

    private static bool CanBeComplex(BuildingDefinition definition) =>
        definition.Size == 1 && definition.Kind is BuildingKind.Producer && definition.Out.Count != 0;

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
            // The building's track: complexes from tier 4, mega complexes from tier 8 (1.4 §12)
            if (ComplexSizeFor(building) < size)
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
            // The complex has the lowest level: a level's goods still coming go back
            DropWants(owner, member.Id);
            RemoveBuilding(member);
        }
        var price = members.Average(CopyPrice);
        var complex = new Building { Id = NextBuildingId++, Type = type, Owner = owner.Id, X = origin.X, Y = origin.Y, Size = size, Level = level, Price = price };
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

    /// <summary>After a Complexes tier: every square of identical buildings merges</summary>
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

    // ---------------------------------------------------------------- Evolve in place

    /// <summary>What a building is worth when it evolves: what the copy cost and every level bought for it</summary>
    public double EvolveCredit(Building building)
    {
        var upgrades = 0.0;
        for (var level = 1; level < building.Level; level++)
        {
            upgrades += Content.Progression.UpgradeCost(CopyPrice(building), level);
        }
        return (CopyPrice(building) + upgrades) * ComplexMembers(building);
    }

    /// <summary>Where a building's successor would stand (covering the old footprint), or null with the reason</summary>
    public (TilePos? At, double Cost, string? Problem) EvolvePlan(Company company, Building building)
    {
        var definition = Content.Building(building.Type);
        if (definition.EvolvesTo is not { } next || !Content.Buildings.TryGetValue(next, out var target))
        {
            return (null, 0, "This building doesn't evolve.");
        }
        if (building.UnderConstruction)
        {
            return (null, 0, "Wait until it is built.");
        }
        if (ComplexMembers(building) > 1)
        {
            return (null, 0, "A complex doesn't evolve.");
        }
        if (!IsUnlocked(company, target, out var reason))
        {
            return (null, 0, reason);
        }
        var cost = Math.Max(0, BuildCost(company, target) - EvolveCredit(building));
        var grow = target.Size - building.Size;
        for (var dy = 0; dy >= -grow; dy--)
        {
            for (var dx = 0; dx >= -grow; dx--)
            {
                var origin = building.Position.Offset(dx, dy);
                if (CanPlace(company, target, origin, building.Id).Success)
                {
                    return (origin, cost, null);
                }
            }
        }
        return (null, cost, $"Make room around it: a {target.Name} is {target.Size}×{target.Size}.");
    }

    /// <summary>
    /// Evolves a building into its successor where it stands: the old one's gold and upgrades count towards the new
    /// one, the pipes around it stay connected. An Era change becomes an upgrade, not a rebuild.
    /// </summary>
    public CommandResult Evolve(long companyId, long buildingId)
    {
        if (Company(companyId) is not { } company || Building(buildingId) is not { } building || building.Owner != companyId)
        {
            return CommandResult.Fail("That is not your building.");
        }
        var (at, cost, problem) = EvolvePlan(company, building);
        if (at is not { } origin)
        {
            return CommandResult.Fail(problem ?? "It can't evolve here.");
        }
        var target = Content.Building(Content.Building(building.Type).EvolvesTo!);
        var payment = Pay(company, cost, target.Cost.Materials);
        if (!payment.Success)
        {
            return payment;
        }
        var price = BuildCost(company, target);
        // A trading post keeps its levels as a Market Hall: its Hub Orders stay (1.4 §8)
        var keeps = target.Kind == BuildingKind.TradingPost ? building.Level : 1;
        DropWants(company, building.Id);
        RemoveBuilding(building);
        var evolved = AddBuilding(company, target.Id, origin, finished: false, price);
        evolved.Level = keeps;
        company.BuiltTypes.Add(target.Id);
        AddStat(company, "evolved", 1);
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }
}
