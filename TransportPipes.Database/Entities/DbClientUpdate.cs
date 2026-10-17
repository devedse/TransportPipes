using TransportPipes.Database.Entities.ClientUpdateDetails;

namespace TransportPipes.Database.Entities;

public class DbClientUpdate
{
    public long Id { get; set; }

    public DbUser? ExecutedBy { get; set; }
    public long? ExecutedById { get; set; }

    /// <summary>
    /// Stored as JSON (jsonb) including a type discriminator, see <see cref="Helpers.DetailsJsonSerializer"/>
    /// </summary>
    public ClientUpdateDetailsBase? ClientUpdateDetails { get; set; }
}
