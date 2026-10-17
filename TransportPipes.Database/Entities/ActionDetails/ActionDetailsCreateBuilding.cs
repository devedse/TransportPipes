using TransportPipes.Common.Enums;

namespace TransportPipes.Database.Entities.ActionDetails;

public class ActionDetailsCreateBuilding : ActionDetailsBase
{
    public GameObjectType GameObject { get; set; }
    public int PriceOfBuilding { get; set; }

    public override string ToString()
    {
        return string.Format("ActionDetailsCreateBuilding: GameObject: {0}", GameObject);
    }
}
