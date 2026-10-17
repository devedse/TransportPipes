namespace TransportPipes.Common.Enums;

public static class GenericGameObjectTypeHelper
{
    public static bool IsPipe(this GameObjectType gameObjectType)
    {
        return (int)gameObjectType >= 10000;
    }
}
