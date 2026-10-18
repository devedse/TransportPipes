using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using TransportPipes.Server.Dtos;
using TransportPipes.Server.Hubs;
using TransportPipes.Server.Services;
using TransportPipes.TestsCommon.Database;

namespace TransportPipes.Server.Tests;

/// <summary>
/// The real server on a PostgreSQL database, with a fake clock: the world only ticks when the test advances time
/// </summary>
public sealed class GameServer : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = CreateJson();

    private readonly bool _ownsDatabase;

    public string ConnectionString { get; }
    public FakeTimeProvider Time { get; }

    public GameServer(string? connectionString = null, FakeTimeProvider? time = null)
    {
        _ownsDatabase = connectionString == null;
        ConnectionString = connectionString ?? PostgresTestDatabase.CreateDatabase();
        Time = time ?? new FakeTimeProvider(StartTime());
    }

    /// <summary>
    /// A minute after the next quarter hour: hub markets update every 15 minutes, so tests that check exact earnings
    /// over a few minutes don't depend on the time they run
    /// </summary>
    public static DateTimeOffset StartTime()
    {
        var now = DateTimeOffset.UtcNow;
        var quarter = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute / 15 * 15, 0, TimeSpan.Zero);
        return quarter.AddMinutes(16);
    }

    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:TransportPipes", ConnectionString);
        builder.UseSetting("TransportPipes:TickSeconds", "1");
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Time));
    }

    public WorldHost World => Services.GetRequiredService<WorldHost>();

    /// <summary>Moves the clock forward and lets the world catch up</summary>
    public async Task AdvanceAsync(TimeSpan span)
    {
        Time.Advance(span);
        await World.TickAsync();
    }

    /// <summary>Registers a user and returns the login cookie</summary>
    public async Task<string> RegisterAsync(string userName, string password = "secret123")
    {
        var response = await CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest(userName, password, null), Json);
        var result = await response.Content.ReadFromJsonAsync<AuthResult>(Json);
        Assert.IsTrue(result!.Success, result.Error);
        return Cookie(response);
    }

    public async Task<(AuthResult Result, string? Cookie)> LoginAsync(string userName, string password)
    {
        var response = await CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(userName, password), Json);
        var result = (await response.Content.ReadFromJsonAsync<AuthResult>(Json))!;
        return (result, result.Success ? Cookie(response) : null);
    }

    private static string Cookie(HttpResponseMessage response) =>
        response.Headers.GetValues("Set-Cookie").Select(t => t.Split(';')[0]).Single(t => t.StartsWith("transportpipes="));

    public async Task<TestClient> ConnectAsync(string? cookie)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(Server.BaseAddress, GameHub.Path), options =>
            {
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                if (cookie != null)
                {
                    options.Headers["Cookie"] = cookie;
                }
            })
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            })
            .Build();
        var client = new TestClient(connection);
        await connection.StartAsync();
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && _ownsDatabase)
        {
            PostgresTestDatabase.DropDatabase(ConnectionString);
        }
    }
}

/// <summary>A SignalR client that records everything the server pushes</summary>
public sealed class TestClient : IAsyncDisposable
{
    public HubConnection Connection { get; }

    public Channel<ChunkObjectsDto> Chunks { get; } = Channel.CreateUnbounded<ChunkObjectsDto>();
    public Channel<CompanyDto> Companies { get; } = Channel.CreateUnbounded<CompanyDto>();
    public Channel<ToastDto> Toasts { get; } = Channel.CreateUnbounded<ToastDto>();
    public Channel<ChatMessageDto> Chat { get; } = Channel.CreateUnbounded<ChatMessageDto>();
    public Channel<int> Inbox { get; } = Channel.CreateUnbounded<int>();

    public TestClient(HubConnection connection)
    {
        Connection = connection;
        connection.On<ChunkObjectsDto>(nameof(IGameClient.ChunkChanged), t => Chunks.Writer.TryWrite(t));
        connection.On<CompanyDto>(nameof(IGameClient.CompanyChanged), t => Companies.Writer.TryWrite(t));
        connection.On<ToastDto>(nameof(IGameClient.Toast), t => Toasts.Writer.TryWrite(t));
        connection.On<ChatMessageDto>(nameof(IGameClient.ChatMessage), t => Chat.Writer.TryWrite(t));
        connection.On<int>(nameof(IGameClient.InboxChanged), t => Inbox.Writer.TryWrite(t));
    }

    public Task<T> Invoke<T>(string method, params object?[] args) => Connection.InvokeCoreAsync<T>(method, args);

    public async Task Command(string method, params object?[] args)
    {
        var result = await Invoke<CommandResultDto>(method, args);
        Assert.IsTrue(result.Success, result.Error);
    }

    public static async Task<T> WaitFor<T>(Channel<T> channel, Func<T, bool> condition, TimeSpan? timeout = null)
    {
        using var cancellation = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(15));
        while (true)
        {
            var item = await channel.Reader.ReadAsync(cancellation.Token);
            if (condition(item))
            {
                return item;
            }
        }
    }

    public async ValueTask DisposeAsync() => await Connection.DisposeAsync();
}
