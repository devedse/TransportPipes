using Microsoft.AspNetCore.Identity;
using TransportPipes.Actionizer;
using TransportPipes.Database;
using TransportPipes.Database.Entities;
using TransportPipes.Server.Hubs;
using TransportPipes.Server.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("TransportPipes")
    ?? throw new InvalidOperationException("Configure the database with the ConnectionStrings__TransportPipes environment variable (or ConnectionStrings:TransportPipes in appsettings).");

builder.Services.Configure<TransportPipesOptions>(builder.Configuration.GetSection(TransportPipesOptions.SectionName));

builder.Services.AddSingleton(services => new TransportPipesDbFactory(connectionString, services.GetRequiredService<ILoggerFactory>()));
builder.Services.AddSingleton<IPasswordHasher<DbUser>, PasswordHasher<DbUser>>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<ConnectedUsers>();
builder.Services.AddSingleton<GameFrontEndService>();
builder.Services.AddSingleton<ClientUpdateDispatcher>();
builder.Services.AddSingleton<TransportActionizer>();
builder.Services.AddHostedService<ActionizerBackgroundService>();

builder.Services.AddSignalR();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

await app.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();

// The frontend (TransportPipes.Frontend) is built into wwwroot by the Dockerfile
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHub<TransportHub>(TransportHub.Path);
app.MapHealthChecks("/health");
app.MapGet("/api/version", () => AppVersion.Current);
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
