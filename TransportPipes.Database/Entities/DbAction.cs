using TransportPipes.Common.Enums;
using TransportPipes.Database.Entities.ActionDetails;

namespace TransportPipes.Database.Entities;

public class DbAction
{
    public long Id { get; set; }

    public TransportAction TransportAction { get; set; }

    public int X { get; set; }
    public int Y { get; set; }

    public DbUser? ExecutedBy { get; set; }
    public long ExecutedById { get; set; }

    /// <summary>
    /// Stored as JSON (jsonb) including a type discriminator, see <see cref="Helpers.DetailsJsonSerializer"/>
    /// </summary>
    public ActionDetailsBase? ActionDetails { get; set; }

    public bool Executed { get; set; }

    public DateTime DateToBeExecuted { get; set; }

    public double GoldClaimed { get; set; }
}
