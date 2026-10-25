using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TransportPipes.Database;
using TransportPipes.Database.Entities;
using TransportPipes.Game.Content;
using TransportPipes.Server.Dtos;
using TransportPipes.Server.Hubs;
using TransportPipes.Server.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("TransportPipes")
    ?? throw new InvalidOperationException("Configure the database with the ConnectionStrings__TransportPipes environment variable (or ConnectionStrings:TransportPipes in appsettings).");

builder.Services.Configure<TransportPipesOptions>(builder.Configuration.GetSection(TransportPipesOptions.SectionName));
// The server runs behind a proxy: the player's address is the one it forwards (the guest limit counts per address)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var playtest = builder.Configuration.GetSection(TransportPipesOptions.SectionName).Get<TransportPipesOptions>() is { Playtest: true } options
    ? options
    : null;
if (playtest != null)
{
    // The director's clock: one for the world, the logins and everything else
    var clock = new PlaytestClock(Path.Combine(playtest.PlaytestDataDir, "clock.json"));
    builder.Services.AddSingleton(clock);
    builder.Services.AddSingleton<TimeProvider>(clock);
    builder.Services.AddSingleton<PlaytestLog>();
}
else
{
    builder.Services.AddSingleton(TimeProvider.System);
}
builder.Services.AddSingleton(services => new TransportPipesDbFactory(connectionString, services.GetRequiredService<ILoggerFactory>()));
builder.Services.AddDbContext<TransportPipesDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddSingleton<IPasswordHasher<DbUser>, PasswordHasher<DbUser>>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<AccountService>();
builder.Services.AddSingleton<ConnectedUsers>();
builder.Services.AddSingleton<SocialService>();
builder.Services.AddSingleton<WorldStore>();
builder.Services.AddSingleton<WorldHost>();
builder.Services.AddHostedService(services => services.GetRequiredService<WorldHost>());

// The keys that sign the login cookies are kept in the database, so logins survive restarts and new images
builder.Services.AddDataProtection().PersistKeysToDbContext<TransportPipesDbContext>().SetApplicationName("TransportPipes");
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "transportpipes";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(60);
        options.SlidingExpiration = true;
        // An API: answer 401/403 instead of redirecting to a login page
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
        // A login of a user that no longer exists (a reset database) is not valid
        options.Events.OnValidatePrincipal = async context =>
        {
            var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var accounts = context.HttpContext.RequestServices.GetRequiredService<AccountService>();
            if (id == null || !long.TryParse(id, out var userId) || !await accounts.ExistsAsync(userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });
builder.Services.AddAuthorization();

void ConfigureJson(JsonSerializerOptions options)
{
    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
}

builder.Services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));
builder.Services.AddSignalR().AddJsonProtocol(options => ConfigureJson(options.PayloadSerializerOptions));
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

var app = builder.Build();
app.UseForwardedHeaders();

await app.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();

// The frontend (TransportPipes.Frontend) is built into wwwroot by the Dockerfile. Only its files and the game's content
// are compressed (no response with a secret in it is), and the ones with a hash in their name or URL are kept by
// browsers for good.
app.UseWhen(
    context => context.Request.Path == "/api/content"
        || (!context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments(GameHub.Path)),
    branch => branch.UseResponseCompression());
app.UseDefaultFiles();
var staticFiles = new StaticFileOptions { OnPrepareResponse = context => StaticCaching.Apply(context.Context) };
app.UseStaticFiles(staticFiles);
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<GameHub>(GameHub.Path);
if (playtest != null)
{
    app.MapPlaytest(playtest);
}
app.MapHealthChecks("/health");
app.MapGet("/api/version", () => AppVersion.Current);

var content = GameViews.Content(GameContent.Default);
app.MapGet("/api/content", () => content);

var auth = app.MapGroup("/api/auth");

static async Task<UserInfo> SignInAsync(HttpContext context, DbUser user)
{
    var identity = new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.UserName)],
        CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });
    return new UserInfo(user.Id, user.UserName, user.IsAdmin, user.IsGuest);
}

// Playing without an account first (v1.2 §5): a few guests a day per address, so nobody makes an army of them
var guestsToday = new System.Collections.Concurrent.ConcurrentDictionary<string, (DateOnly Day, int Count)>();

auth.MapPost("/register", async (RegisterRequest request, AccountService accounts, HttpContext context) =>
{
    var (user, error) = await accounts.RegisterAsync(request.UserName ?? "", request.Password ?? "", request.Email);
    return user == null
        ? new AuthResult(false, error, null)
        : new AuthResult(true, null, await SignInAsync(context, user));
});

auth.MapPost("/login", async (LoginRequest request, AccountService accounts, HttpContext context, TimeProvider time) =>
{
    var user = await accounts.LoginAsync(request.UserName ?? "", request.Password ?? "");
    if (user?.BannedUntil is { } banned && banned > time.GetUtcNow().UtcDateTime)
    {
        return new AuthResult(false, $"This account is banned until {banned:yyyy-MM-dd HH:mm} UTC.", null);
    }
    return user == null
        ? new AuthResult(false, "Wrong user name or password.", null)
        : new AuthResult(true, null, await SignInAsync(context, user));
});

auth.MapPost("/guest", async (AccountService accounts, HttpContext context, TimeProvider time, IOptions<TransportPipesOptions> options) =>
{
    var address = context.Connection.RemoteIpAddress?.ToString() ?? "?";
    var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
    var count = guestsToday.AddOrUpdate(address, (today, 1), (_, t) => t.Day == today ? (today, t.Count + 1) : (today, 1));
    if (count.Count > options.Value.GuestsPerAddressPerDay)
    {
        return new GuestResult(false, "Too many new players from here today: log in, or make an account.", null, null);
    }
    var (user, password) = await accounts.CreateGuestAsync();
    return new GuestResult(true, null, await SignInAsync(context, user), password);
});

auth.MapPost("/keep", async (KeepRequest request, ClaimsPrincipal principal, AccountService accounts, WorldHost world, HttpContext context) =>
{
    var id = long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var (user, error) = await accounts.KeepAsync(id, request.UserName ?? "", request.Password ?? "", request.Email);
    if (user == null)
    {
        return new AuthResult(false, error, null);
    }
    await world.RunAsync(w =>
    {
        if (w.CompanyOfUser(id) is { } company)
        {
            company.IsGuest = false;
            company.Touch();
        }
    });
    return new AuthResult(true, null, await SignInAsync(context, user));
})
    .RequireAuthorization();

// What a "Play next to me" link opens, for someone who isn't playing yet (1.4 §6)
app.MapGet("/api/invite/{code}", (string code, WorldHost world) =>
    world.RunAsync(w => GameViews.NextToMe(w, code), changesWorld: false));

auth.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
});

auth.MapGet("/me", async (ClaimsPrincipal user, AccountService accounts) =>
{
    var id = long.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var (isAdmin, _, _) = await accounts.ModerationAsync(id);
    return new UserInfo(id, user.FindFirstValue(ClaimTypes.Name) ?? "", isAdmin, await accounts.IsGuestAsync(id));
})
    .RequireAuthorization();

app.MapFallbackToFile("index.html", staticFiles);

app.Run();

public partial class Program;
