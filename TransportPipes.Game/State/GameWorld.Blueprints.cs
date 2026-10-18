using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

public enum BlueprintSharing
{
    Private,
    Corporation,
    Board,
}

public sealed record BlueprintBuilding(string Type, int Dx, int Dy);

/// <summary>A saved layout (GDD §11.1): rebuilt in one go at the normal costs, shared with a corporation or on the board</summary>
public sealed class Blueprint
{
    public long Id { get; init; }
    public long Owner { get; init; }
    public required string Name { get; set; }
    public int Width { get; init; }
    public int Height { get; init; }
    public List<BlueprintBuilding> Buildings { get; init; } = new();
    public List<PackedPipe> Pipes { get; init; } = new();
    public BlueprintSharing Sharing { get; set; }
    public Dictionary<long, int> Ratings { get; init; } = new();
    public DateTime CreatedAt { get; init; }

    public double Rating => Ratings.Count == 0 ? 0 : Ratings.Values.Average();
}

public sealed partial class GameWorld
{
    /// <summary>The unlock level of the Blueprint Office (for the messages)</summary>
    public const int BlueprintLevel = 22;
    public const int MaxBlueprints = 20;

    private readonly Dictionary<long, Blueprint> _blueprints = new();

    public long NextBlueprintId { get; set; } = 1;
    public IEnumerable<Blueprint> Blueprints => _blueprints.Values;

    public void LoadBlueprints(IEnumerable<Blueprint> blueprints)
    {
        foreach (var blueprint in blueprints)
        {
            _blueprints[blueprint.Id] = blueprint;
            NextBlueprintId = Math.Max(NextBlueprintId, blueprint.Id + 1);
        }
    }

    /// <summary>The blueprints a company may use: its own, its corporation's shared ones and the board's</summary>
    public IEnumerable<Blueprint> BlueprintsFor(Company company) => _blueprints.Values.Where(t =>
        t.Owner == company.Id
        || t.Sharing == BlueprintSharing.Board
        || (t.Sharing == BlueprintSharing.Corporation && company.CorporationId != null && Company(t.Owner)?.CorporationId == company.CorporationId));

    /// <summary>Saves the buildings and pipes of a rectangle of the estate</summary>
    public CommandResult SaveBlueprint(long companyId, string name, TilePos from, int width, int height)
    {
        if (Company(companyId) is not { } company || EstateOf(company) is not { } estate)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (!HasBuilding(companyId, BuildingKind.BlueprintOffice))
        {
            return CommandResult.Fail($"Saving a blueprint needs a Blueprint Office (Company Level {BlueprintLevel}).");
        }
        name = name.Trim();
        if (name.Length is < 3 or > 30)
        {
            return CommandResult.Fail("A blueprint name has 3 to 30 characters.");
        }
        if (width is < 1 or > 32 || height is < 1 or > 32)
        {
            return CommandResult.Fail("A blueprint is at most 32 by 32 tiles.");
        }
        if (_blueprints.Values.Count(t => t.Owner == companyId) >= MaxBlueprints)
        {
            return CommandResult.Fail($"You can keep {MaxBlueprints} blueprints.");
        }
        bool Inside(TilePos t) => t.X >= from.X && t.Y >= from.Y && t.X < from.X + width && t.Y < from.Y + height;
        var blueprint = new Blueprint { Id = NextBlueprintId++, Owner = companyId, Name = name, Width = width, Height = height, CreatedAt = Now };
        foreach (var building in BuildingsOf(companyId).Where(t => t.Footprint().All(Inside)))
        {
            var definition = Content.Building(building.Type);
            if (!definition.Buildable)
            {
                continue;
            }
            // A complex is saved as its members
            var cells = ComplexMembers(building) > 1 ? building.Size : 1;
            for (var dy = 0; dy < cells; dy++)
            {
                for (var dx = 0; dx < cells; dx++)
                {
                    blueprint.Buildings.Add(new BlueprintBuilding(building.Type, building.X - from.X + dx, building.Y - from.Y + dy));
                }
            }
        }
        foreach (var conductor in _conductors.Values.Where(t => t.Owner == companyId && t.Kind == ConductorKind.Pipe && Inside(t.Pos) && estate.Contains(t.Pos)))
        {
            blueprint.Pipes.Add(new PackedPipe(conductor.Pos.X - from.X, conductor.Pos.Y - from.Y, conductor.Tier));
        }
        if (blueprint.Buildings.Count == 0)
        {
            return CommandResult.Fail("There are no buildings of yours in that area.");
        }
        _blueprints[blueprint.Id] = blueprint;
        Changes.Blueprints = true;
        return CommandResult.Ok;
    }

    /// <summary>Builds a blueprint at a spot: everything that fits, at the normal costs</summary>
    public CommandResult BuildBlueprint(long companyId, long blueprintId, TilePos origin)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (!_blueprints.TryGetValue(blueprintId, out var blueprint) || !BlueprintsFor(company).Contains(blueprint))
        {
            return CommandResult.Fail("Unknown blueprint.");
        }
        // After a Franchise the own blueprints come along (GDD §11.7): a fast replay without a new office first
        if (!HasBuilding(companyId, BuildingKind.BlueprintOffice) && !(company.Franchises > 0 && blueprint.Owner == companyId))
        {
            return CommandResult.Fail($"Building a blueprint needs a Blueprint Office (Company Level {BlueprintLevel}).");
        }
        var built = 0;
        var skipped = 0;
        foreach (var item in blueprint.Buildings)
        {
            if (PlaceBuilding(companyId, item.Type, origin.Offset(item.Dx, item.Dy)).Success)
            {
                built++;
            }
            else
            {
                skipped++;
            }
        }
        foreach (var pipe in blueprint.Pipes)
        {
            // One by one: a tile that doesn't fit doesn't stop the others
            PlacePipes(companyId, [origin.Offset(pipe.Dx, pipe.Dy)], BestPipeTier(company, pipe.Tier));
        }
        if (built == 0)
        {
            return CommandResult.Fail("Nothing of the blueprint fits there (or you can't pay for it).");
        }
        if (skipped > 0)
        {
            Changes.Events.Add(new WorldEvent(companyId, WorldEventKind.ConstructionFinished, $"{built} buildings of \"{blueprint.Name}\" are being built; {skipped} didn't fit or aren't unlocked"));
        }
        return CommandResult.Ok;
    }

    /// <summary>The best pipe up to a tier that the company has (a shared blueprint may use better pipes)</summary>
    private int BestPipeTier(Company company, int tier) =>
        Content.PipeTiers.Where(t => t.Tier <= tier && t.Era <= Era && (t.Research == null || HasResearch(company, t.Research)))
            .Select(t => t.Tier).DefaultIfEmpty(1).Max();

    /// <summary>What building the blueprint costs a company now (gold; land, no bridges)</summary>
    public double BlueprintCost(Company company, Blueprint blueprint)
    {
        var copies = new Dictionary<string, int>();
        double gold = 0;
        foreach (var item in blueprint.Buildings)
        {
            if (!Content.Buildings.TryGetValue(item.Type, out var definition))
            {
                continue;
            }
            var owned = copies.TryGetValue(item.Type, out var count) ? count : CopiesOwned(company.Id, item.Type);
            gold += Content.Progression.BuildCost(definition, owned);
            copies[item.Type] = owned + 1;
        }
        return gold + blueprint.Pipes.Sum(t => Content.PipeTier(BestPipeTier(company, t.Tier)).Gold);
    }

    public CommandResult ShareBlueprint(long companyId, long blueprintId, BlueprintSharing sharing)
    {
        if (!_blueprints.TryGetValue(blueprintId, out var blueprint) || blueprint.Owner != companyId)
        {
            return CommandResult.Fail("That is not your blueprint.");
        }
        if (sharing != BlueprintSharing.Private && !HasBuilding(companyId, BuildingKind.BlueprintOffice))
        {
            return CommandResult.Fail("Sharing a blueprint needs a Blueprint Office.");
        }
        blueprint.Sharing = sharing;
        Changes.Blueprints = true;
        return CommandResult.Ok;
    }

    public CommandResult DeleteBlueprint(long companyId, long blueprintId)
    {
        if (!_blueprints.TryGetValue(blueprintId, out var blueprint) || blueprint.Owner != companyId)
        {
            return CommandResult.Fail("That is not your blueprint.");
        }
        _blueprints.Remove(blueprintId);
        Changes.Blueprints = true;
        return CommandResult.Ok;
    }

    /// <summary>Rates a blueprint of someone else (one to five stars)</summary>
    public CommandResult RateBlueprint(long companyId, long blueprintId, int stars)
    {
        if (Company(companyId) is not { } company || !_blueprints.TryGetValue(blueprintId, out var blueprint) || !BlueprintsFor(company).Contains(blueprint))
        {
            return CommandResult.Fail("Unknown blueprint.");
        }
        if (blueprint.Owner == companyId)
        {
            return CommandResult.Fail("You can't rate your own blueprint.");
        }
        blueprint.Ratings[companyId] = Math.Clamp(stars, 1, 5);
        Changes.Blueprints = true;
        return CommandResult.Ok;
    }
}
