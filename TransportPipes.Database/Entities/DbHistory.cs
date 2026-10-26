namespace TransportPipes.Database.Entities;

/// <summary>A company's or a town's numbers at one moment (docs/design-notes/statistics.md): kind 0 a company, 1 a town</summary>
public class DbStatSample
{
    public int Kind { get; set; }
    public long Subject { get; set; }
    public DateTime At { get; set; }

    /// <summary>The numbers by series, as JSON</summary>
    public string Values { get; set; } = "{}";
}

/// <summary>A key moment of a company (docs/design-notes/statistics.md)</summary>
public class DbMoment
{
    public long Id { get; set; }
    public long Company { get; set; }
    public DateTime At { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool Big { get; set; }
    public string? Ref { get; set; }
}
