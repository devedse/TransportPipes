using TransportPipes.Common.Enums;

namespace TransportPipes.Common.GameLogicObjects.GameObjectInfo;

public class BackendGameObjectInfo
{
    public string GameObjectTypeName { get { return GameObjectType.ToString(); } }
    public GameObjectType GameObjectType { get; private set; }
    public int Price { get; private set; }
    public List<BackendResourceInfo> ConsumptionInfo { get; private set; }
    public List<BackendResourceInfo> ProductionInfo { get; private set; }
    public int BuildTimeInSeconds { get; private set; }

    public BackendGameObjectInfo(GameObjectType gameObjectType, int price, List<BackendResourceInfo> consumptionInfo, List<BackendResourceInfo> productionInfo, int buildTimeInSeconds)
    {
        this.GameObjectType = gameObjectType;
        this.Price = price;
        this.ConsumptionInfo = consumptionInfo;
        this.ProductionInfo = productionInfo;
        this.BuildTimeInSeconds = buildTimeInSeconds;
    }
}
