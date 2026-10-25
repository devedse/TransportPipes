using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TransportPipes.Game.State;
using TransportPipes.Server.Dtos;

namespace TransportPipes.Server.Tests;

[TestClass]
public class SocietyFacts
{
    private static async Task<(TestClient Client, CompanyDto Company)> PlayerAsync(GameServer server, string name)
    {
        var client = await server.ConnectAsync(await server.RegisterAsync(name));
        await client.Command("CreateCompany", name, "#3399ff", null, null, null, null);
        var hello = await client.Invoke<HelloDto>("Hello");
        return (client, hello.Company!);
    }

    [TestMethod]
    public async Task NeighboursChatInTheirDistrictAndDirectly()
    {
        using var server = new GameServer();
        var (ann, annCompany) = await PlayerAsync(server, "Annie");
        var (bob, bobCompany) = await PlayerAsync(server, "Bobby");
        await using var _ = ann;
        await using var __ = bob;

        var chat = await ann.Invoke<ChatDto>("GetChat");
        var district = chat.Channels.Single(t => t.Kind == "district").Id;
        await ann.Command("SendChat", district, "  Anyone selling coal?  ");

        var heard = await TestClient.WaitFor(bob.Chat, t => t.Channel == district);
        Assert.AreEqual("Anyone selling coal?", heard.Text);
        Assert.AreEqual("Annie", heard.SenderName);
        Assert.AreEqual("Anyone selling coal?", (await bob.Invoke<IReadOnlyList<ChatMessageDto>>("GetChatHistory", district, null)).Single().Text);

        var direct = $"direct:{Math.Min(annCompany.Id, bobCompany.Id)}:{Math.Max(annCompany.Id, bobCompany.Id)}";
        await bob.Command("SendChat", direct, "I do, 4 g");
        Assert.AreEqual("I do, 4 g", (await TestClient.WaitFor(ann.Chat, t => t.Channel == direct)).Text);
        Assert.IsTrue((await ann.Invoke<ChatDto>("GetChat")).Channels.Any(t => t.Id == direct && t.Name == "Bobby"));

        // Nobody else reads or writes a direct channel, and a blocked company can't write
        Assert.IsFalse((await ann.Invoke<CommandResultDto>("SendChat", "direct:998:999", "hi")).Success);
        await ann.Command("SetBlocked", bobCompany.Id, true);
        Assert.IsFalse((await bob.Invoke<CommandResultDto>("SendChat", direct, "hello?")).Success);
        Assert.IsEmpty(await ann.Invoke<IReadOnlyList<ChatMessageDto>>("GetChatHistory", direct, null), "blocked senders are hidden");

        // The rate limit
        var results = new List<bool>();
        for (var i = 0; i < 8; i++)
        {
            results.Add((await ann.Invoke<CommandResultDto>("SendChat", district, $"spam {i}")).Success);
        }
        Assert.IsTrue(results.Contains(false));
    }

    [TestMethod]
    public async Task APlayerAloneInAYardFindsTheTownChatFirst()
    {
        // Run 4: Mia's four greetings went to her station's channel, which held only a silent scripted Jo
        using var server = new GameServer();
        var (mia, _) = await PlayerAsync(server, "Mia");
        await using var _ = mia;

        var chat = await mia.Invoke<ChatDto>("GetChat");

        Assert.AreEqual("district", chat.Channels[0].Kind, string.Join(", ", chat.Channels.Select(t => t.Kind)));
        Assert.IsTrue(chat.Channels.Any(t => t.Kind == "crew"), "the Yard's chat is still there");
    }

    [TestMethod]
    public async Task VisitorsLikeEstatesAndSignTheGuestbook()
    {
        using var server = new GameServer();
        var (ann, annCompany) = await PlayerAsync(server, "Annie");
        var (bob, _) = await PlayerAsync(server, "Bobby");
        await using var _1 = ann;
        await using var _2 = bob;

        await bob.Command("LikeEstate", annCompany.Id);
        Assert.IsFalse((await bob.Invoke<CommandResultDto>("LikeEstate", annCompany.Id)).Success, "once a day");
        await bob.Command("WriteGuestbook", annCompany.Id, "Lovely pipes!");
        Assert.IsFalse((await bob.Invoke<CommandResultDto>("WriteGuestbook", annCompany.Id, "Again")).Success, "one note a day");

        var visit = await bob.Invoke<EstateVisitDto>("VisitEstate", annCompany.Id);
        Assert.AreEqual(1, visit.Likes);
        Assert.IsTrue(visit.LikedToday);
        Assert.AreEqual("Lovely pipes!", visit.Guestbook.Single().Text);

        // The owner hears about both in the inbox
        await TestClient.WaitFor(ann.Inbox, t => t >= 2);
        var inbox = await ann.Invoke<InboxDto>("GetInbox");
        Assert.IsTrue(inbox.Messages.Any(t => t.Kind == "social" && t.Text.Contains("likes your plot")));
        Assert.IsTrue(inbox.Messages.Any(t => t.Kind == "guestbook" && t.Text.Contains("Lovely pipes!")));
        await ann.Invoke<object>("MarkInboxRead");
        Assert.AreEqual(0, (await ann.Invoke<InboxDto>("GetInbox")).Unread);

        // A day later Bobby may like it again
        await server.AdvanceAsync(TimeSpan.FromDays(1));
        await bob.Command("LikeEstate", annCompany.Id);
    }

    [TestMethod]
    public async Task NeighboursAreAYardsCrewWithItsOwnChat()
    {
        using var server = new GameServer();
        var (ann, annCompany) = await PlayerAsync(server, "Annie");
        var (bob, bobCompany) = await PlayerAsync(server, "Bobby");
        await using var _1 = ann;
        await using var _2 = bob;

        // Pick for me settles Bobby in Annie's Yard: its crew; any resident names it
        var crew = (await bob.Invoke<CrewViewDto>("GetCrew")).Crew!;
        Assert.HasCount(2, crew.Members);
        Assert.IsFalse(crew.Named);
        await bob.Command("SetYard", "Pipe Guild", "kettle", "#aa3355", "#f4ead3");
        Assert.AreEqual("Pipe Guild", (await ann.Invoke<CompanyDto>("GetCompany")).Crew!.Name);

        var channel = (await bob.Invoke<ChatDto>("GetChat")).Channels.Single(t => t.Kind == "crew");
        Assert.AreEqual("Pipe Guild", channel.Name);
        await bob.Command("SendChat", channel.Id, "Thanks for having me");
        Assert.AreEqual("Thanks for having me", (await TestClient.WaitFor(ann.Chat, t => t.Channel == channel.Id)).Text);
        Assert.IsTrue((await bob.Invoke<ChatDto>("GetChat")).Channels.Any(t => t.Kind == "world"), "and the world chat");

        // Leaderboards list both, with you marked
        var board = await bob.Invoke<LeaderboardDto>("GetLeaderboard", LeaderboardKind.Level, true);
        Assert.AreEqual(annCompany.Id, board.Entries[0].Company);
        Assert.AreEqual(2, board.You!.Rank);
    }
}

[TestClass]
public class SeasonServerFacts
{
    [TestMethod]
    public async Task AfterTheChapterTheNextOneStartsAndTheWorldStays()
    {
        using var server = new GameServer();
        var cookie = await server.RegisterAsync("Veteran");
        await using var client = await server.ConnectAsync(cookie);
        await client.Command("CreateCompany", "Old Timer", "#3399ff", null, null, null, null);
        await server.World.RunAsync(world =>
        {
            var company = world.Companies.Single();
            company.Gold = 5_000_000;
            world.StartFinale();
            world.EndSeason();
        });
        await server.World.SeasonAsync();
        var season = await client.Invoke<SeasonDto>("GetSeason");
        Assert.AreEqual(1, season.You!.Rank);
        Assert.AreEqual("Season 1 Champion", season.You.Title);
        var before = (await client.Invoke<HelloDto>("Hello")).Company!;

        // A day later: the next chapter, in the same world
        server.Time.Advance(TimeSpan.FromDays(1.1));
        await server.World.SeasonAsync();

        var company = (await client.Invoke<HelloDto>("Hello")).Company!;
        Assert.AreEqual(before.Id, company.Id, "the company goes on");
        Assert.AreEqual(before.EstateX, company.EstateX);
        Assert.AreEqual(2, company.Season.Season);
        Assert.IsNull(company.Season.EndedAt);
        Assert.IsTrue(company.Titles.Contains("Season 1 Champion"));
        Assert.IsGreaterThan(0, company.LegacyLevel, "Legacy grows for the company that goes on");
    }
}

[TestClass]
public class ModerationFacts
{
    [TestMethod]
    public async Task ModeratorsReviewReportsAndMuteOrBan()
    {
        using var server = new GameServer();
        var troll = await server.ConnectAsync(await server.RegisterAsync("Troll"));
        var victim = await server.ConnectAsync(await server.RegisterAsync("Victim"));
        var modCookie = await server.RegisterAsync("Moderator");
        await using var _1 = troll;
        await using var _2 = victim;
        await troll.Command("CreateCompany", "Troll Inc", "#ff0000", null, null, null, null);
        await victim.Command("CreateCompany", "Victim Inc", "#00ff00", null, null, null, null);
        await using (var db = server.Services.GetRequiredService<TransportPipes.Database.TransportPipesDbFactory>().CreateDbContext())
        {
            await db.Users.Where(t => t.UserName == "Moderator").ExecuteUpdateAsync(t => t.SetProperty(u => u.IsAdmin, true));
        }
        await using var moderator = await server.ConnectAsync(modCookie);

        var district = (await troll.Invoke<ChatDto>("GetChat")).Channels.Single(t => t.Kind == "district").Id;
        await troll.Command("SendChat", district, "something nasty");
        var message = await TestClient.WaitFor(victim.Chat, t => t.Text == "something nasty");
        await victim.Command("ReportChat", message.Id);

        Assert.IsEmpty(await victim.Invoke<IReadOnlyList<ReportedMessageDto>>("GetReports"), "only for moderators");
        var reports = await moderator.Invoke<IReadOnlyList<ReportedMessageDto>>("GetReports");
        Assert.AreEqual("something nasty", reports.Single().Text);
        Assert.IsTrue((await moderator.Invoke<HelloDto>("Hello")).User.IsAdmin);

        await moderator.Command("ModerateMessage", message.Id, "mute");
        Assert.IsEmpty(await moderator.Invoke<IReadOnlyList<ReportedMessageDto>>("GetReports"), "the message is gone");
        StringAssert.Contains((await troll.Invoke<CommandResultDto>("SendChat", district, "again")).Error, "muted");

        await moderator.Command("RenameCompany", (await troll.Invoke<CompanyDto>("GetCompany")).Id, "Nice Company");
        Assert.AreEqual("Nice Company", (await troll.Invoke<CompanyDto>("GetCompany")).Name);
    }

    [TestMethod]
    public async Task ABannedPlayerCantLogIn()
    {
        using var server = new GameServer();
        await server.RegisterAsync("Banned");
        await using (var db = server.Services.GetRequiredService<TransportPipes.Database.TransportPipesDbFactory>().CreateDbContext())
        {
            await db.Users.Where(t => t.UserName == "Banned").ExecuteUpdateAsync(t => t.SetProperty(u => u.BannedUntil, DateTime.UtcNow.AddYears(1)));
        }

        var (result, _) = await server.LoginAsync("Banned", "secret123");

        Assert.IsFalse(result.Success);
        StringAssert.Contains(result.Error, "banned");
    }
}
