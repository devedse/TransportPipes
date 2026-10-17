namespace TransportPipes.Database.Entities.ActionDetails;

public class ActionDetailsResearch : ActionDetailsBase
{
    public int ResearchLevel { get; set; }
    public DateTime NogWat { get; set; }

    public override string ToString()
    {
        return string.Format("ActionDetailsResearch: ResearchLevel: {0} NogWat: {1}", ResearchLevel, NogWat);
    }
}
