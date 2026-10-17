namespace TransportPipes.Common.GameLogicObjects.GameObjectInfo;

public class BackendResourceInfo
{
    public string ResourceTypeName { get { return ResourceType.ToString(); } }
    public BackendResourceType ResourceType { get; private set; }
    public double Value { get; set; }

    public BackendResourceInfo(BackendResourceType resourceType, double value)
    {
        this.ResourceType = resourceType;
        this.Value = value;
    }
}
