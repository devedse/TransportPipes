using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;
using TransportPipes.Game.State;
using TransportPipes.Game.World;
using TransportPipes.Server.Dtos;
using TransportPipes.TestsCommon.Database;

namespace TransportPipes.Server.Tests;

[TestClass]
public class GameServerFacts
{
    [TestMethod]
    public async Task RegisteringLogsYouIn()
    {
        using var server = new GameServer();
        var cookie = await server.RegisterAsync("Pipe_Fan");

        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        var me = await client.GetFromJsonAsync<UserInfo>("/api/auth/me", GameServer.Json);

        Assert.AreEqual("Pipe_Fan", me!.UserName);
    }

    [TestMethod]
    public async Task UserNamesAreUniqueAndPasswordsAreChecked()
    {
        using var server = new GameServer();
        await server.RegisterAsync("Loady");

        var again = await server.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest("loady", "secret123", null), GameServer.Json);
        Assert.IsFalse((await again.Content.ReadFromJsonAsync<AuthResult>(GameServer.Json))!.Success);
        Assert.IsFalse((await server.LoginAsync("Loady", "wrong")).Result.Success);
        Assert.IsTrue((await server.LoginAsync("LOADY", "secret123")).Result.Success, "user names are not case sensitive");
    }

    [TestMethod]
    public async Task TheGameNeedsALogin()
    {
        using var server = new GameServer();

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await server.CreateClient().GetAsync("/api/auth/me")).StatusCode);
        await Assert.ThrowsAsync<HttpRequestException>(() => server.ConnectAsync(cookie: null));
    }

    [TestMethod]
    public async Task TheContentIsServed()
    {
        using var server = new GameServer();

        var content = await server.CreateClient().GetFromJsonAsync<JsonElement>("/api/content");

        Assert.IsTrue(content.GetProperty("buildings").EnumerateArray().Any(t => t.GetProperty("id").GetString() == "windMoily"));
        Assert.AreEqual("producer", content.GetProperty("buildings")[0].GetProperty("kind").GetString());
        Assert.AreEqual(32, content.GetProperty("layout").GetProperty("estateSize").GetInt32());
    }

    [TestMethod]
    public async Task ANewPlayerFoundsACompanyAndGetsAnEstate()
    {
        using var server = new GameServer();
        await using var client = await server.ConnectAsync(await server.RegisterAsync("Founder"));

        var hello = await client.Invoke<HelloDto>("Hello");
        Assert.IsNull(hello.Company);

        Assert.IsFalse((await client.Invoke<CommandResultDto>("CreateCompany", "X", "#ff0000", null, null, null, null)).Success, "too short");
        await client.Command("CreateCompany", "Pipe Dreams", "#3399ff", null, null, null, null);

        hello = await client.Invoke<HelloDto>("Hello");
        Assert.AreEqual("Pipe Dreams", hello.Company!.Name);
        Assert.AreEqual(2000, hello.Company.Gold);
        Assert.AreEqual(1, hello.Company.Level);
        Assert.AreEqual(1, hello.Company.BuildingCounts["hq"]);

        // The chunk with the HQ contains it, the district lists the company
        var hq = new TilePos(hello.Company.EstateX + GameWorld.HqLocal.X, hello.Company.EstateY + GameWorld.HqLocal.Y);
        var (cx, cy) = WorldLayout.ChunkOf(hq.X, hq.Y);
        var chunks = await client.Invoke<IReadOnlyList<ChunkDto>>("GetChunks", new[] { cx, cy });
        Assert.IsTrue(chunks.Single().Objects.Buildings.Any(t => t.Type == "hq" && t.X == hq.X && t.Y == hq.Y));
        Assert.AreEqual(WorldLayout.ChunkSize * WorldLayout.ChunkSize, Convert.FromBase64String(chunks.Single().Terrain!).Length);
        var district = await client.Invoke<DistrictDto>("GetDistrict", hello.Company.DistrictX, hello.Company.DistrictY);
        Assert.AreEqual("Pipe Dreams", district.Estates.Single(t => t.Slot == hello.Company.Slot).OwnerName);
    }

    [TestMethod]
    public async Task AFriendOpensASeatLinkPlaysAsAGuestAndKeepsTheEstate()
    {
        using var server = new GameServer();
        await using var ana = await server.ConnectAsync(await server.RegisterAsync("Ana"));
        var stations = await ana.Invoke<StationsDto>("GetStations", null, null);
        var started = await ana.Invoke<FoundCrewResultDto>("StartWithFriends", "Ana Pipes", "#3399ff", "Kettle & Co.", "kettle", "#236b69", "#c9963a",
            stations.Friends.DistrictX, stations.Friends.DistrictY, stations.Friends.Slot, 3);
        Assert.IsTrue(started.Success, started.Error);
        Assert.HasCount(3, started.Holds);
        var code = started.Holds[0].Code;

        // The link shows the crew and the seat before the friend has an account
        var http = server.CreateClient();
        var invite = await http.GetFromJsonAsync<InviteDto>($"/api/invite/{code}", GameServer.Json);
        Assert.IsTrue(invite!.Valid);
        Assert.AreEqual("Kettle & Co.", invite.CrewName);
        Assert.AreEqual(started.Holds[0].Slot, invite.Slot);

        // "Move in": a guest, then the company on the kept seat
        var guest = await http.PostAsync("/api/auth/guest", null);
        var result = await guest.Content.ReadFromJsonAsync<GuestResult>(GameServer.Json);
        Assert.IsTrue(result!.Success, result.Error);
        var cookie = guest.Headers.GetValues("Set-Cookie").Select(t => t.Split(';')[0]).Single(t => t.StartsWith("transportpipes="));
        await using (var ben = await server.ConnectAsync(cookie))
        {
            Assert.IsTrue((await ben.Invoke<HelloDto>("Hello")).User.IsGuest);
            await ben.Command("CreateCompany", invite.Name!, "#236b69", null, null, null, code);
            var hello = await ben.Invoke<HelloDto>("Hello");
            Assert.AreEqual(started.Holds[0].Slot, hello.Company!.Slot);
            Assert.AreEqual("Kettle & Co.", hello.Company.Crew!.Name);
        }

        // Keeping the estate: a name and a password of its own, and the guest is a player like any other
        var keep = new HttpRequestMessage(HttpMethod.Post, "/api/auth/keep") { Content = JsonContent.Create(new KeepRequest("Ben", "secret123", null), options: GameServer.Json) };
        keep.Headers.Add("Cookie", cookie);
        var kept = await (await http.SendAsync(keep)).Content.ReadFromJsonAsync<AuthResult>(GameServer.Json);
        Assert.IsTrue(kept!.Success, kept.Error);
        var (login, again) = await server.LoginAsync("Ben", "secret123");
        Assert.IsTrue(login.Success);
        await using var back = await server.ConnectAsync(again);
        var home = await back.Invoke<HelloDto>("Hello");
        Assert.IsFalse(home.User.IsGuest);
        Assert.AreEqual(started.Holds[0].Slot, home.Company!.Slot);
    }

    [TestMethod]
    public async Task ALinkIsOfferedAcceptedAndDrawnThroughTheHub()
    {
        using var server = new GameServer();
        await using var ana = await server.ConnectAsync(await server.RegisterAsync("Ana"));
        await using var ben = await server.ConnectAsync(await server.RegisterAsync("Ben"));
        // The start screen's picker opens the first district
        await ana.Invoke<StationsDto>("GetStations", null, null);
        await ana.Command("CreateCompany", "Ana Pipes", "#3399ff", 0, 0, 0, null);
        await ben.Command("CreateCompany", "Ben Works", "#ff9933", 0, 0, 3, null);
        var benId = (await ben.Invoke<HelloDto>("Hello")).Company!.Id;

        var plan = await ana.Invoke<LinkPlanDto>("PlanLink", benId, "coal", true, 1);
        Assert.IsNull(plan.Error);
        Assert.IsGreaterThan(0, plan.Length);
        await ana.Command("OfferLink", benId, "coal", true, 1);

        var offers = await ben.Invoke<LinksDto>("GetLinks");
        var offer = offers.Links.Single();
        Assert.AreEqual("offered", offer.State);
        Assert.IsFalse(offer.YouSell);
        Assert.AreEqual("Ana Pipes", offer.PartnerName);
        StringAssert.Contains(offer.Line, "accept");
        Assert.IsEmpty(await ana.Invoke<IReadOnlyList<MapLinkDto>>("GetMapLinks"), "an offer isn't laid yet");

        await ben.Command("AcceptLink", offer.Id);
        Assert.AreEqual("active", (await ana.Invoke<LinksDto>("GetLinks")).Links.Single().State);
        var drawn = await ana.Invoke<IReadOnlyList<MapLinkDto>>("GetMapLinks");
        Assert.AreEqual(plan.Route.Count, drawn.Single().Route.Count, "the map draws the route that was planned");
    }

    /// <summary>A spot next to the trading post for a pipe and a wind moily behind it</summary>
    private static Task<(TilePos Pipe, TilePos Moily)> SpotNextToTradingPost(GameServer server, long userId) =>
        server.World.RunAsync(world =>
        {
            var company = world.CompanyOfUser(userId)!;
            var post = world.BuildingsOf(company.Id).Single(t => t.Type == "tradingPost").Position;
            foreach (var (dx, dy) in new[] { (1, 0), (0, 1), (0, -1) })
            {
                var pipe = post.Offset(dx, dy);
                var moily = post.Offset(dx * 2, dy * 2);
                if (!world.IsOccupied(pipe) && world.Tile(pipe).Terrain is not (TerrainType.Water or TerrainType.Mountain)
                    && world.CanPlace(company, world.Content.Building("windMoily"), moily).Success)
                {
                    return (pipe, moily);
                }
            }
            throw new AssertFailedException("No room next to the trading post");
        }, changesWorld: false);

    [TestMethod]
    public async Task AWindMoilyEarnsGoldOnceItIsBuilt()
    {
        using var server = new GameServer();
        await using var client = await server.ConnectAsync(await server.RegisterAsync("Builder"));
        var hello = await client.Invoke<HelloDto>("Hello");
        await client.Command("CreateCompany", "Breezy", "#33cc66", null, null, null, null);
        var (pipe, moily) = await SpotNextToTradingPost(server, hello.User.Id);
        // Watching the chunk: updates of it are pushed
        var (cx, cy) = WorldLayout.ChunkOf(moily.X, moily.Y);
        await client.Invoke<IReadOnlyList<ChunkDto>>("GetChunks", new[] { cx, cy });

        await client.Command("PlaceBuilding", "windMoily", moily.X, moily.Y);
        await TestClient.WaitFor(client.Chunks, t => t.Buildings.Any(b => b.Type == "windMoily" && b.ConstructionEndsAt != null));
        await client.Command("PlacePipes", new[] { pipe.X, pipe.Y }, 1);
        var company = await client.Invoke<CompanyDto>("GetCompany");
        Assert.AreEqual(2000 - 200 - 2, company.Gold);
        Assert.AreEqual(0, company.GoldPerMinute, "still under construction");

        // 30 seconds to build, then a minute of wind
        await server.AdvanceAsync(TimeSpan.FromSeconds(90));

        var toast = await TestClient.WaitFor(client.Toasts, t => t.Kind == "constructionFinished");
        StringAssert.Contains(toast.Text, "Wind Moily");
        company = await client.Invoke<CompanyDto>("GetCompany");
        Assert.IsGreaterThan(0, company.GoldPerMinute);
        Assert.AreEqual(2000 - 202 + company.GoldPerMinute, company.Gold, 1e-6);

        var details = await client.Invoke<BuildingDetailsDto>("GetBuilding", (await server.World.RunAsync(w => w.BuildingAt(moily)!.Id, changesWorld: false)));
        Assert.AreEqual(1, details.Rate, 1e-9);
        Assert.AreEqual("sale", details.Flows.Single().Kind);
        Assert.AreEqual(200, details.UpgradeCost);
    }

    [TestMethod]
    public async Task TheWorldAndTheLoginSurviveARestart()
    {
        var connectionString = PostgresTestDatabase.CreateDatabase();
        try
        {
            string cookie;
            TilePos moily;
            var time = new FakeTimeProvider(GameServer.StartTime());
            using (var server = new GameServer(connectionString, time))
            {
                cookie = await server.RegisterAsync("Keeper");
                await using var client = await server.ConnectAsync(cookie);
                var hello = await client.Invoke<HelloDto>("Hello");
                await client.Command("CreateCompany", "Keepers & Co", "#aa66ff", null, null, null, null);
                TilePos pipe;
                (pipe, moily) = await SpotNextToTradingPost(server, hello.User.Id);
                await client.Command("PlaceBuilding", "windMoily", moily.X, moily.Y);
                await client.Command("PlacePipes", new[] { pipe.X, pipe.Y }, 1);
                await server.AdvanceAsync(TimeSpan.FromMinutes(1));
            }

            // The server was down for an hour: the world catches up when it starts again
            time.Advance(TimeSpan.FromHours(1));
            using (var server = new GameServer(connectionString, time))
            {
                await using var client = await server.ConnectAsync(cookie);
                var hello = await client.Invoke<HelloDto>("Hello");

                Assert.AreEqual("Keepers & Co", hello.Company!.Name);
                Assert.AreEqual(1, hello.Company.BuildingCounts["windMoily"]);
                Assert.IsGreaterThan(0, hello.Company.GoldPerMinute);
                // 30 seconds of construction, then 60.5 minutes of wind; the hub pays up to 25% more for scarce energy after
                // its first market update, so the rate now is the highest one of that hour
                var earned = hello.Company.Gold - (2000 - 202);
                Assert.IsGreaterThan(hello.Company.GoldPerMinute * 60.5 / 1.3, earned);
                Assert.IsLessThanOrEqualTo(hello.Company.GoldPerMinute * 60.5 * 1.001, earned);
                Assert.IsNotNull(hello.Report, "an offline report after an hour away");
                Assert.IsTrue(await server.World.RunAsync(w => w.BuildingAt(moily)?.Type == "windMoily", changesWorld: false));
            }
        }
        finally
        {
            PostgresTestDatabase.DropDatabase(connectionString);
        }
    }
}
