namespace TransportPipes.Common.GameLogicObjects.GameObjectInfo;

public class BackendResourceWorthInfo
{
    public BackendResourceType ResourceType { get; private set; }
    public double SellValueToMarket { get; private set; }
    public double BuyValueFromMarket { get; private set; }
    public double ValueForPlayers { get; private set; }

    public BackendResourceWorthInfo(BackendResourceType resourceType, double sellValueToMarket, double buyValueFromMarket, double valueForPlayers)
    {
        this.ResourceType = resourceType;
        this.SellValueToMarket = sellValueToMarket;
        this.BuyValueFromMarket = buyValueFromMarket;
        this.ValueForPlayers = valueForPlayers;
    }
}
