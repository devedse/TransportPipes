using System.Text.Json.Serialization;

namespace TransportPipes.Database.Entities.ClientUpdateDetails;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ClientUpdateDetailsIncomeChanged), "IncomeChanged")]
[JsonDerivedType(typeof(ClientUpdateDetailsTileNeedsRefresh), "TileNeedsRefresh")]
public abstract class ClientUpdateDetailsBase
{
}
