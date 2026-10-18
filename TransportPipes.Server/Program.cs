using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddSingleton(TimeProvider.System);
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

var app = builder.Build();

await app.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();

// The frontend (TransportPipes.Frontend) is built into wwwroot by the Dockerfile
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<GameHub>(GameHub.Path);
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
    return new UserInfo(user.Id, user.UserName);
}

auth.MapPost("/register", async (RegisterRequest request, AccountService accounts, HttpContext context) =>
{
    var (user, error) = await accounts.RegisterAsync(request.UserName ?? "", request.Password ?? "", request.Email);
    return user == null
        ? new AuthResult(false, error, null)
        : new AuthResult(true, null, await SignInAsync(context, user));
});

auth.MapPost("/login", async (LoginRequest request, AccountService accounts, HttpContext context) =>
{
    var user = await accounts.LoginAsync(request.UserName ?? "", request.Password ?? "");
    return user == null
        ? new AuthResult(false, "Wrong user name or password.", null)
        : new AuthResult(true, null, await SignInAsync(context, user));
});

auth.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.NoContent();
});

auth.MapGet("/me", (ClaimsPrincipal user) =>
    new UserInfo(long.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!), user.FindFirstValue(ClaimTypes.Name) ?? ""))
    .RequireAuthorization();

app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
