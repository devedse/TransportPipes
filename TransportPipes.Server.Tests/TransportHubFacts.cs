using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using TransportPipes.Common.Enums;
using TransportPipes.Server.Dtos;
using TransportPipes.Server.Services;

namespace TransportPipes.Server.Tests;

[TestClass]
public class TransportHubFacts
{
    [TestMethod]
    public async Task HealthEndpointReportsHealthy()
    {
        await using var factory = new TransportPipesServerFixture();
        using var httpClient = factory.CreateClient();

        var response = await httpClient.GetAsync("/health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("Healthy", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task VersionEndpointReturnsTheVersion()
    {
        await using var factory = new TransportPipesServerFixture();
        using var httpClient = factory.CreateClient();

        var version = await httpClient.GetFromJsonAsync<VersionInfo>("/api/version");

        Assert.IsNotNull(version);
        Assert.AreEqual(AppVersion.Current, version);
        Assert.IsFalse(string.IsNullOrWhiteSpace(version.Version));
    }

    [TestMethod]
    public async Task RegisterAndLoginWork()
    {
        await using var factory = new TransportPipesServerFixture();
        await using var client = await factory.ConnectAsync();

        var registered = await client.Connection.InvokeAsync<LoginResult>("Register", "Player1", "secret", "Player1@Test.com");
        Assert.IsTrue(registered.Success, registered.Message);
        Assert.AreEqual("Player1", registered.UserName);
        Assert.AreNotEqual(0, registered.UserId);

        await using var otherClient = await factory.ConnectAsync();

        var wrongPassword = await otherClient.Connection.InvokeAsync<LoginResult>("Login", "Player1", "wrong");
        Assert.IsFalse(wrongPassword.Success);

        //User names are case-insensitive
        var loggedIn = await otherClient.Connection.InvokeAsync<LoginResult>("Login", "PLAYER1", "secret");
        Assert.IsTrue(loggedIn.Success, loggedIn.Message);
        Assert.AreEqual(registered.UserId, loggedIn.UserId);
        Assert.AreEqual("Player1", loggedIn.UserName);

        var duplicate = await otherClient.Connection.InvokeAsync<LoginResult>("Register", "player1", "other", "");
        Assert.IsFalse(duplicate.Success);
        StringAssert.Contains(duplicate.Message, "already exists");

        var tooShort = await otherClient.Connection.InvokeAsync<LoginResult>("Register", "abc", "secret", "");
        Assert.IsFalse(tooShort.Success);
    }

    [TestMethod]
    public async Task ReturnsAllBuildableGameObjects()
    {
        await using var factory = new TransportPipesServerFixture();
        await using var client = await factory.ConnectAsync();

        var gameObjectInfos = await client.Connection.InvokeAsync<List<GameObjectInfo>>("GetGameObjectInfos");

        Assert.AreEqual(7, gameObjectInfos.Count);

        var pipe = gameObjectInfos.Single(t => t.GameObjectType == (int)GameObjectType.GreyPipe);
        Assert.AreEqual("GreyPipe", pipe.GameObjectTypeName);
        Assert.AreEqual(0, pipe.Price);

        var windMoily = gameObjectInfos.Single(t => t.GameObjectTypeName == "WindMoily");
        Assert.AreEqual(100, windMoily.Price);
        Assert.AreEqual("Energy", windMoily.ProductionInfo.Single().ResourceTypeName);
        Assert.AreEqual(10 / 3600.0, windMoily.ProductionInfo.Single().Value, 0.0000001);
    }

    [TestMethod]
    public async Task ReturnsAChunk()
    {
        await using var factory = new TransportPipesServerFixture();
        await using var client = await factory.ConnectAsync();

        var chunk = await client.Connection.InvokeAsync<ChunkData>("RequestChunk", 0, 0);

        Assert.AreEqual(0, chunk.ChunkX);
        Assert.AreEqual(8, chunk.Terrain.Length);
        Assert.IsTrue(chunk.Terrain.All(t => t.Length == 8));
        Assert.IsTrue(chunk.GameObjects.SelectMany(t => t).All(t => t == "none"));
        Assert.IsTrue(chunk.Owners.SelectMany(t => t).All(t => t == 0));
        //The origin of the world is marked
        Assert.AreEqual(3, chunk.Terrain[0][0]);
    }

    [TestMethod]
    public async Task BuildingRequiresALogin()
    {
        await using var factory = new TransportPipesServerFixture();
        await using var client = await factory.ConnectAsync();

        await client.Connection.InvokeAsync("BuildGameObject", 1, 1, (int)GameObjectType.WindMoily);

        var notification = await client.WaitForNotificationAsync();
        Assert.AreEqual("error", notification.Level);
        Assert.AreEqual("Please log in first.", notification.Message);
    }

    [TestMethod]
    public async Task BuildingAPipeIsExecutedByTheActionizerAndPushedToTheClients()
    {
        await using var factory = new TransportPipesServerFixture();
        await using var client = await factory.ConnectAsync();
        await using var spectator = await factory.ConnectAsync();

        var user = await client.Connection.InvokeAsync<LoginResult>("Register", "Builder", "secret", "");
        Assert.IsTrue(user.Success, user.Message);

        //Tile 100,100 is in chunk 12,12 at position 4,4. Pipes are free and are built instantly.
        await client.Connection.InvokeAsync("BuildGameObject", 100, 100, (int)GameObjectType.GreyPipe);

        var builtChunk = await spectator.WaitForChunkUpdateAsync(t => t.ChunkX == 12 && t.ChunkY == 12 && t.GameObjects[4][4] == "GreyPipeurdl");
        Assert.AreEqual(user.UserId, builtChunk.Owners[4][4]);

        var gold = await client.GoldUpdates.Reader.ReadAsync();
        Assert.AreEqual("Builder", gold.UserName);
        Assert.AreEqual(500, gold.CurrentGold, 0.001);

        await client.Connection.InvokeAsync("DeleteGameObject", 100, 100);

        await spectator.WaitForChunkUpdateAsync(t => t.ChunkX == 12 && t.ChunkY == 12 && t.GameObjects[4][4] == "none");
        Assert.IsFalse(client.Notifications.Reader.TryRead(out _));
    }

    [TestMethod]
    public async Task BuildingClaimsGoldAndShowsTheBuildingUnderConstruction()
    {
        await using var factory = new TransportPipesServerFixture();
        await using var client = await factory.ConnectAsync();

        var user = await client.Connection.InvokeAsync<LoginResult>("Register", "Builder", "secret", "");

        //A WindMoily costs 100 gold and takes a minute to build
        await client.Connection.InvokeAsync("BuildGameObject", 3, 2, (int)GameObjectType.WindMoily);

        var chunk = await client.WaitForChunkUpdateAsync(t => t.ChunkX == 0 && t.ChunkY == 0);
        Assert.AreEqual("UnderConstruction", chunk.GameObjects[3][2]);
        Assert.AreEqual(user.UserId, chunk.Owners[3][2]);

        var gold = await client.GoldUpdates.Reader.ReadAsync();
        Assert.AreEqual(400, gold.CurrentGold, 0.001);

        //Only one action can be queued per tile
        await client.Connection.InvokeAsync("BuildGameObject", 3, 2, (int)GameObjectType.SolarPanel);
        StringAssert.Contains((await client.WaitForNotificationAsync()).Message, "already an action queued");

        var status = await client.Connection.InvokeAsync<string?>("GetStatusOfGameObject", 3, 2);
        Assert.IsNull(status);
    }

    [TestMethod]
    public async Task BuildingSomethingTooExpensiveIsRefused()
    {
        await using var factory = new TransportPipesServerFixture();
        await using var client = await factory.ConnectAsync();

        await client.Connection.InvokeAsync<LoginResult>("Register", "Builder", "secret", "");

        await client.Connection.InvokeAsync("BuildGameObject", 3, 2, (int)GameObjectType.TradingBuilding);

        var notification = await client.WaitForNotificationAsync();
        Assert.AreEqual("warning", notification.Level);
        StringAssert.Contains(notification.Message, "enough money");
    }
}
