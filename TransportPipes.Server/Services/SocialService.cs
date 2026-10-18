using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using TransportPipes.Database;
using TransportPipes.Database.Entities;
using TransportPipes.Server.Dtos;

namespace TransportPipes.Server.Services;

/// <summary>
/// Chat, the inbox, guestbooks and block lists (GDD §9.6). They live in the database, not in the game world: they don't
/// change the simulation and grow without bounds.
/// </summary>
public sealed class SocialService
{
    public const int MaxChatLength = 300;
    public const int MaxNoteLength = 200;
    public const int PageSize = 50;
    private const int KeepInbox = 200;

    /// <summary>At most this many chat messages per company in <see cref="RateWindow"/></summary>
    private const int RateLimit = 6;

    private static readonly TimeSpan RateWindow = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan NoteEvery = TimeSpan.FromDays(1);

    private readonly TransportPipesDbFactory _db;
    private readonly TimeProvider _time;
    private readonly ConcurrentDictionary<long, Queue<DateTime>> _sent = new();

    public SocialService(TransportPipesDbFactory db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    public static string DistrictChannel(int x, int y) => $"district:{x}:{y}";

    public static string CorporationChannel(long id) => $"corporation:{id}";

    public static string DirectChannel(long a, long b) => a < b ? $"direct:{a}:{b}" : $"direct:{b}:{a}";

    /// <summary>The two companies of a direct channel</summary>
    public static (long A, long B)? DirectPair(string channel)
    {
        var parts = channel.Split(':');
        return parts.Length == 3 && parts[0] == "direct" && long.TryParse(parts[1], out var a) && long.TryParse(parts[2], out var b) && a < b ? (a, b) : null;
    }

    // ---------------------------------------------------------------- Chat

    /// <summary>Checks the text and the rate limit; null when the message may be sent</summary>
    public string? CheckMessage(long company, string text)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            return "Write something first.";
        }
        if (text.Length > MaxChatLength)
        {
            return $"A message has at most {MaxChatLength} characters.";
        }
        var sent = _sent.GetOrAdd(company, _ => new Queue<DateTime>());
        lock (sent)
        {
            while (sent.Count > 0 && Now - sent.Peek() > RateWindow)
            {
                sent.Dequeue();
            }
            if (sent.Count >= RateLimit)
            {
                return "Slow down a little.";
            }
            sent.Enqueue(Now);
        }
        return null;
    }

    public async Task<ChatMessageDto> SendAsync(string channel, long sender, string senderName, string text)
    {
        await using var db = _db.CreateDbContext();
        var message = new DbChatMessage { Channel = channel, Sender = sender, SenderName = senderName, Text = text.Trim(), SentAt = Now };
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync();
        return View(message);
    }

    /// <summary>The newest messages of a channel (older than a message id, to scroll back), without blocked senders</summary>
    public async Task<IReadOnlyList<ChatMessageDto>> HistoryAsync(string channel, long reader, long? before)
    {
        await using var db = _db.CreateDbContext();
        var blocked = db.Blocks.Where(t => t.Company == reader).Select(t => t.Blocked);
        var query = db.ChatMessages.Where(t => t.Channel == channel && !blocked.Contains(t.Sender));
        if (before is { } id)
        {
            query = query.Where(t => t.Id < id);
        }
        var messages = await query.OrderByDescending(t => t.Id).Take(PageSize).ToListAsync();
        messages.Reverse();
        return messages.Select(View).ToList();
    }

    /// <summary>The direct channels of a company, with the time of their last message</summary>
    public async Task<IReadOnlyList<(string Channel, DateTime LastAt)>> DirectChannelsAsync(long company)
    {
        await using var db = _db.CreateDbContext();
        var low = $"direct:{company}:";
        var high = $":{company}";
        var channels = await db.ChatMessages
            .Where(t => t.Channel.StartsWith("direct:") && (t.Channel.StartsWith(low) || t.Channel.EndsWith(high)))
            .GroupBy(t => t.Channel)
            .Select(t => new { Channel = t.Key, LastAt = t.Max(m => m.SentAt) })
            .OrderByDescending(t => t.LastAt)
            .Take(30)
            .ToListAsync();
        return channels.Where(t => DirectPair(t.Channel) is { } pair && (pair.A == company || pair.B == company)).Select(t => (t.Channel, t.LastAt)).ToList();
    }

    public async Task<DateTime?> LastMessageAtAsync(string channel)
    {
        await using var db = _db.CreateDbContext();
        return await db.ChatMessages.Where(t => t.Channel == channel).OrderByDescending(t => t.Id).Select(t => (DateTime?)t.SentAt).FirstOrDefaultAsync();
    }

    /// <summary>Flags a message for the moderators; returns the channel it was in (to check the reporter could see it)</summary>
    public async Task<string?> ReportAsync(long messageId, Func<string, bool> canRead)
    {
        await using var db = _db.CreateDbContext();
        var message = await db.ChatMessages.AsTracking().FirstOrDefaultAsync(t => t.Id == messageId);
        if (message == null || !canRead(message.Channel))
        {
            return null;
        }
        message.Reported = true;
        await db.SaveChangesAsync();
        return message.Channel;
    }

    public async Task<IReadOnlyList<long>> BlockedAsync(long company)
    {
        await using var db = _db.CreateDbContext();
        return await db.Blocks.Where(t => t.Company == company).Select(t => t.Blocked).ToListAsync();
    }

    public async Task SetBlockedAsync(long company, long other, bool blocked)
    {
        await using var db = _db.CreateDbContext();
        var row = await db.Blocks.AsTracking().FirstOrDefaultAsync(t => t.Company == company && t.Blocked == other);
        if (blocked && row == null)
        {
            db.Blocks.Add(new DbBlock { Company = company, Blocked = other });
        }
        else if (!blocked && row != null)
        {
            db.Blocks.Remove(row);
        }
        await db.SaveChangesAsync();
    }

    private static ChatMessageDto View(DbChatMessage message) => new(message.Id, message.Channel, message.Sender, message.SenderName, message.Text, message.SentAt);

    // ---------------------------------------------------------------- Inbox

    /// <summary>Adds messages to inboxes; returns the companies that got one</summary>
    public async Task<IReadOnlyCollection<long>> NotifyAsync(IReadOnlyList<(long Company, string Kind, string Text)> messages)
    {
        if (messages.Count == 0)
        {
            return [];
        }
        await using var db = _db.CreateDbContext();
        foreach (var (company, kind, text) in messages)
        {
            db.InboxMessages.Add(new DbInboxMessage { Company = company, Kind = kind, Text = text.Length > 500 ? text[..500] : text, At = Now });
        }
        await db.SaveChangesAsync();
        var companies = messages.Select(t => t.Company).ToHashSet();
        // Keep the newest messages of every inbox
        foreach (var company in companies)
        {
            var oldest = await db.InboxMessages.Where(t => t.Company == company).OrderByDescending(t => t.Id).Skip(KeepInbox).Select(t => (long?)t.Id).FirstOrDefaultAsync();
            if (oldest is { } id)
            {
                await db.InboxMessages.Where(t => t.Company == company && t.Id <= id).ExecuteDeleteAsync();
            }
        }
        return companies;
    }

    public async Task<int> UnreadAsync(long company)
    {
        await using var db = _db.CreateDbContext();
        return await db.InboxMessages.CountAsync(t => t.Company == company && !t.Read);
    }

    public async Task<InboxDto> InboxAsync(long company)
    {
        await using var db = _db.CreateDbContext();
        var messages = await db.InboxMessages.Where(t => t.Company == company).OrderByDescending(t => t.Id).Take(PageSize).ToListAsync();
        var unread = await db.InboxMessages.CountAsync(t => t.Company == company && !t.Read);
        return new InboxDto(messages.Select(t => new InboxMessageDto(t.Id, t.Kind, t.Text, t.At, t.Read)).ToList(), unread);
    }

    public async Task MarkReadAsync(long company)
    {
        await using var db = _db.CreateDbContext();
        await db.InboxMessages.Where(t => t.Company == company && !t.Read).ExecuteUpdateAsync(t => t.SetProperty(m => m.Read, true));
    }

    // ---------------------------------------------------------------- Guestbook

    public async Task<IReadOnlyList<GuestbookNoteDto>> GuestbookAsync(long owner, long reader)
    {
        await using var db = _db.CreateDbContext();
        var blocked = db.Blocks.Where(t => t.Company == reader).Select(t => t.Blocked);
        return await db.GuestbookNotes
            .Where(t => t.Owner == owner && !blocked.Contains(t.Author))
            .OrderByDescending(t => t.Id)
            .Take(20)
            .Select(t => new GuestbookNoteDto(t.Id, t.Author, t.AuthorName, t.Text, t.At))
            .ToListAsync();
    }

    /// <summary>One note per visitor per estate per day</summary>
    public async Task<string?> WriteNoteAsync(long owner, long author, string authorName, string text)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            return "Write something first.";
        }
        if (text.Length > MaxNoteLength)
        {
            return $"A note has at most {MaxNoteLength} characters.";
        }
        await using var db = _db.CreateDbContext();
        var since = Now - NoteEvery;
        if (await db.GuestbookNotes.AnyAsync(t => t.Owner == owner && t.Author == author && t.At > since))
        {
            return "You left a note here today already.";
        }
        db.GuestbookNotes.Add(new DbGuestbookNote { Owner = owner, Author = author, AuthorName = authorName, Text = text, At = Now });
        await db.SaveChangesAsync();
        return null;
    }

    /// <summary>The owner may remove notes from their guestbook</summary>
    public async Task<bool> DeleteNoteAsync(long owner, long note)
    {
        await using var db = _db.CreateDbContext();
        return await db.GuestbookNotes.Where(t => t.Owner == owner && t.Id == note).ExecuteDeleteAsync() > 0;
    }
}
