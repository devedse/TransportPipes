namespace TransportPipes.Database.Entities;

/// <summary>A chat message in a channel: district:x:y, corporation:id or direct:a:b (company ids, a &lt; b)</summary>
public class DbChatMessage
{
    public long Id { get; set; }
    public string Channel { get; set; } = string.Empty;
    public long Sender { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool Reported { get; set; }
}

/// <summary>A message in a company's inbox: offers, partners' changes, research done, ...</summary>
public class DbInboxMessage
{
    public long Id { get; set; }
    public long Company { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime At { get; set; }
    public bool Read { get; set; }
}

/// <summary>A note a visitor left in an estate's guestbook</summary>
public class DbGuestbookNote
{
    public long Id { get; set; }
    public long Owner { get; set; }
    public long Author { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime At { get; set; }
}

/// <summary>A company that does not want to see another company's chat messages</summary>
public class DbBlock
{
    public long Company { get; set; }
    public long Blocked { get; set; }
}
