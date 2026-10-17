using System.Text.Json;
using TransportPipes.Database.Entities.ActionDetails;
using TransportPipes.Database.Entities.ClientUpdateDetails;

namespace TransportPipes.Database.Helpers;

/// <summary>
/// Serializes the polymorphic action / client update details to JSON with a "$type" discriminator so they can be
/// deserialized to the right class again (this replaces the old TypedJsvStringSerializer).
/// </summary>
public static class DetailsJsonSerializer
{
    public static JsonSerializerOptions Options { get; } = new JsonSerializerOptions
    {
        // jsonb normalizes the key order, so the "$type" discriminator is not necessarily the first property anymore.
        AllowOutOfOrderMetadataProperties = true
    };

    public static string Serialize(ActionDetailsBase? actionDetails)
    {
        return JsonSerializer.Serialize(actionDetails, Options);
    }

    public static ActionDetailsBase? DeserializeActionDetails(string json)
    {
        return JsonSerializer.Deserialize<ActionDetailsBase>(json, Options);
    }

    public static string Serialize(ClientUpdateDetailsBase? clientUpdateDetails)
    {
        return JsonSerializer.Serialize(clientUpdateDetails, Options);
    }

    public static ClientUpdateDetailsBase? DeserializeClientUpdateDetails(string json)
    {
        return JsonSerializer.Deserialize<ClientUpdateDetailsBase>(json, Options);
    }
}
