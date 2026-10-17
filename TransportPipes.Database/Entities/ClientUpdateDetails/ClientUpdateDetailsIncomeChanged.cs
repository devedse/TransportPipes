namespace TransportPipes.Database.Entities.ClientUpdateDetails;

public class ClientUpdateDetailsIncomeChanged : ClientUpdateDetailsBase
{
    public List<long> DbUserIds { get; set; } = new List<long>();
}
