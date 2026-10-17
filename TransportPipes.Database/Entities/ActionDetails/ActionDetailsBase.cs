using System.Text.Json.Serialization;

namespace TransportPipes.Database.Entities.ActionDetails;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ActionDetailsCreateBuilding), "CreateBuilding")]
[JsonDerivedType(typeof(ActionDetailsDeleteBuilding), "DeleteBuilding")]
[JsonDerivedType(typeof(ActionDetailsResearch), "Research")]
public abstract class ActionDetailsBase
{
}
