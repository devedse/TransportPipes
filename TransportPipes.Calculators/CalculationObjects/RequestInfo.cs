using TransportPipes.Common.GameLogicObjects.GameObjectInfo;
using TransportPipes.Database.Entities;

namespace TransportPipes.Calculators.CalculationObjects;

public class RequestInfo
{
    public DbGameObject Requester { get; private set; }
    public List<BackendResourceInfo> ResourcesRequested { get; private set; }
    public List<DbGameObject> OtherProviders { get; private set; }

    public RequestInfo(DbGameObject Requester, List<BackendResourceInfo> resourcesRequested, List<DbGameObject> otherProviders)
    {
        this.Requester = Requester;
        this.ResourcesRequested = resourcesRequested;
        this.OtherProviders = otherProviders;
    }

    public override string ToString()
    {
        return String.Join(",", ResourcesRequested.Select(t => " " + t.ResourceType + ": " + t.Value));
    }
}
