using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

public sealed partial class GameWorld
{
    /// <summary>
    /// A number the owner sets on a building: the share of its capacity a storage keeps of each resource (one of
    /// <see cref="StorageKeepShares"/>; 0: what the company needs)
    /// </summary>
    public CommandResult SetBuildingSetting(long companyId, long buildingId, double value)
    {
        if (Building(buildingId) is not { } building || building.Owner != companyId)
        {
            return CommandResult.Fail("That is not your building.");
        }
        switch (Content.Building(building.Type).Kind)
        {
            case BuildingKind.Storage:
                building.Setting = StorageKeepShares.MinBy(t => Math.Abs(t - value));
                // The share for every good, instead of its sliders
                building.Keeps.Clear();
                break;
            default:
                return CommandResult.Fail("This building has no setting.");
        }
        building.Touch();
        MarkFlowsChanged(building);
        return CommandResult.Ok;
    }

    /// <summary>
    /// A Storage's sliders (the owner, 2026-10-31: "drag sliders per resource ... if you specify more than the total that
    /// would fit ... you can't save it"): how much of each good it keeps, together at most its room; none set: it keeps
    /// what the company needs. The share buttons' setting goes.
    /// </summary>
    public CommandResult SetStorageKeeps(long companyId, long buildingId, IReadOnlyDictionary<string, double> keeps)
    {
        if (Building(buildingId) is not { } building || building.Owner != companyId)
        {
            return CommandResult.Fail("That is not your building.");
        }
        var definition = Content.Building(building.Type);
        if (definition.Kind != BuildingKind.Storage)
        {
            return CommandResult.Fail("Only a Storage keeps goods.");
        }
        if (keeps.Any(t => !Content.Resources.ContainsKey(t.Key) || double.IsNaN(t.Value) || t.Value < 0))
        {
            return CommandResult.Fail("Unknown good or amount.");
        }
        var room = StorageCapacity(building, definition);
        var total = keeps.Values.Sum();
        if (total > room + 0.5)
        {
            return CommandResult.Fail($"That is {total - room:N0} more than fits: it has room for {room:N0}.");
        }
        building.Keeps.Clear();
        foreach (var (resource, amount) in keeps.Where(t => t.Value >= 0.5))
        {
            building.Keeps[resource] = Math.Round(amount);
        }
        building.Setting = 0;
        building.Touch();
        MarkFlowsChanged(building);
        return CommandResult.Ok;
    }
}
