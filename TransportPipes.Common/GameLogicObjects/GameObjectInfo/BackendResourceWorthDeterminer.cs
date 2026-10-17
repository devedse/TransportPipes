namespace TransportPipes.Common.GameLogicObjects.GameObjectInfo;

public static class BackendResourceWorthDeterminer
{
    public static IReadOnlyDictionary<BackendResourceType, BackendResourceWorthInfo> AllResourceTypeValues { get; } = CreateResourceTypeValues();

    private static Dictionary<BackendResourceType, BackendResourceWorthInfo> CreateResourceTypeValues()
    {
        var allResourceTypeValues = new Dictionary<BackendResourceType, BackendResourceWorthInfo>();

        AddNewInfo(allResourceTypeValues, 1, 3, 2, BackendResourceType.Energy);
        AddNewInfo(allResourceTypeValues, 2, 6, 4, BackendResourceType.IronOre);
        AddNewInfo(allResourceTypeValues, 6, 24, 12, BackendResourceType.Iron);

        return allResourceTypeValues;
    }

    private static void AddNewInfo(Dictionary<BackendResourceType, BackendResourceWorthInfo> allResourceTypeValues, double sellValueToMarket, double buyValueFromMarket, double valueForPlayers, BackendResourceType resourceType)
    {
        allResourceTypeValues.Add(resourceType, new BackendResourceWorthInfo(resourceType, sellValueToMarket, buyValueFromMarket, valueForPlayers));
    }
}
