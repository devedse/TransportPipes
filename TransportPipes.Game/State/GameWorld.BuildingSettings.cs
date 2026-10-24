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
                break;
            default:
                return CommandResult.Fail("This building has no setting.");
        }
        building.Touch();
        MarkDirty(building);
        return CommandResult.Ok;
    }
}
