using System.Threading.Channels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using TransportPipes.Server.Dtos;
using TransportPipes.Server.Hubs;
using TransportPipes.TestsCommon.Database;

namespace TransportPipes.Server.Tests;

/// <summary>
/// The real server (with its Actionizer running every 100ms) on a fresh PostgreSQL database
/// </summary>
public sealed class TransportPipesServerFixture : WebApplicationFactory<Program>
{
    private readonly string _connectionString = PostgresTestDatabase.CreateDatabase();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:TransportPipes", _connectionString);
        builder.UseSetting("TransportPipes:ActionizerIntervalSeconds", "0.1");
        builder.UseSetting("TransportPipes:StartingGold", "500");
    }

    public async Task<TestClient> ConnectAsync()
    {
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(new Uri(Server.BaseAddress, TransportHub.Path), options =>
            {
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();

        var client = new TestClient(hubConnection);
        await hubConnection.StartAsync();
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            PostgresTestDatabase.DropDatabase(_connectionString);
        }
    }
}

/// <summary>
/// A SignalR client that records everything the server pushes
/// </summary>
public sealed class TestClient : IAsyncDisposable
{
    public HubConnection Connection { get; }

    public Channel<ChunkData> ChunkUpdates { get; } = Channel.CreateUnbounded<ChunkData>();
    public Channel<GoldInfo> GoldUpdates { get; } = Channel.CreateUnbounded<GoldInfo>();
    public Channel<Notification> Notifications { get; } = Channel.CreateUnbounded<Notification>();

    public TestClient(HubConnection connection)
    {
        Connection = connection;
        connection.On<ChunkData>(nameof(ITransportClient.ChunkUpdated), chunk => ChunkUpdates.Writer.TryWrite(chunk));
        connection.On<GoldInfo>(nameof(ITransportClient.GoldUpdated), gold => GoldUpdates.Writer.TryWrite(gold));
        connection.On<Notification>(nameof(ITransportClient.Notify), notification => Notifications.Writer.TryWrite(notification));
    }

    /// <summary>
    /// Waits until a ChunkUpdated arrives that matches the condition
    /// </summary>
    public async Task<ChunkData> WaitForChunkUpdateAsync(Func<ChunkData, bool> condition, TimeSpan? timeout = null)
    {
        using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        while (true)
        {
            var chunk = await ChunkUpdates.Reader.ReadAsync(cancellationTokenSource.Token);
            if (condition(chunk))
            {
                return chunk;
            }
        }
    }

    /// <summary>
    /// Waits for the next notification (push handlers run asynchronously, so it can arrive after the invocation returned)
    /// </summary>
    public async Task<Notification> WaitForNotificationAsync(TimeSpan? timeout = null)
    {
        using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        return await Notifications.Reader.ReadAsync(cancellationTokenSource.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }
}
