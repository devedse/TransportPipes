using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TransportPipes.Game.State;
using TransportPipes.Game.World;
using TransportPipes.Server.Dtos;
using TransportPipes.Server.Services;

namespace TransportPipes.Server.Hubs;

/// <summary>What the server pushes to clients</summary>
public interface IGameClient
{
    Task ChunkChanged(ChunkObjectsDto chunk);
    Task NetworksChanged(IReadOnlyList<int> networks);
    Task DistrictChanged(DistrictDto district);
    Task CompanyChanged(CompanyDto company);
    Task Toast(ToastDto toast);
}

/// <summary>The game protocol. Every call needs a logged in user (the login cookie).</summary>
[Authorize]
public sealed class GameHub : Hub<IGameClient>
{
    public const string Path = "/hubs/game";

    /// <summary>A client asks for at most this many chunks at once (a screen full when zoomed out)</summary>
    public const int MaxChunksPerRequest = 64;

    private static readonly TimeSpan ReportAfter = TimeSpan.FromMinutes(5);

    private readonly WorldHost _world;
    private readonly ConnectedUsers _connectedUsers;
    private readonly TransportPipesOptions _options;

    public GameHub(WorldHost world, ConnectedUsers connectedUsers, Microsoft.Extensions.Options.IOptions<TransportPipesOptions> options)
    {
        _world = world;
        _connectedUsers = connectedUsers;
        _options = options.Value;
    }

    public static string UserGroup(long userId) => $"user:{userId}";

    public static string ChunkGroup(int cx, int cy) => $"chunk:{cx}:{cy}";

    private long UserId => long.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private string UserName => Context.User!.FindFirstValue(ClaimTypes.Name) ?? "";

    public override async Task OnConnectedAsync()
    {
        _connectedUsers.Connected(UserId);
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(UserId));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = UserId;
        if (_connectedUsers.Disconnected(userId))
        {
            // The report of the next visit starts now
            await _world.RunAsync(world =>
            {
                if (world.CompanyOfUser(userId) is { } company)
                {
                    company.LastSeenAt = world.Now;
                    company.Report = new CompanyReport { Since = world.Now };
                    company.Touch();
                }
            });
        }
        await base.OnDisconnectedAsync(exception);
    }

    public Task<HelloDto> Hello()
    {
        var userId = UserId;
        var user = new UserInfo(userId, UserName);
        return _world.RunAsync(world =>
        {
            var company = world.CompanyOfUser(userId);
            if (company == null)
            {
                return new HelloDto(user, null, null, world.Now, world.Era);
            }
            ReportDto? report = null;
            if (world.Now - company.Report.Since >= ReportAfter && (company.Report.GoldEarned > 0 || company.Report.BuildingsCompleted > 0 || company.Report.LevelsGained > 0))
            {
                report = GameViews.Report(company.Report);
                company.Report = new CompanyReport { Since = world.Now };
                company.Touch();
            }
            company.LastSeenAt = world.Now;
            return new HelloDto(user, CompanyView(world, company), report, world.Now, world.Era);
        });
    }

    private static CompanyDto CompanyView(GameWorld world, Company company) =>
        GameViews.Company(world, company, world.GoldPerMinute().GetValueOrDefault(company.Id), world.ResearchPerMinute().GetValueOrDefault(company.Id));

    public Task<CommandResultDto> CreateCompany(string name, string color)
    {
        var userId = UserId;
        return _world.RunAsync(world =>
        {
            if (world.CompanyOfUser(userId) != null)
            {
                return CommandResultDto.Fail("You already have a company.");
            }
            if (world.CompanyNameError(name) is { } error)
            {
                return CommandResultDto.Fail(error);
            }
            if (!IsColor(color))
            {
                return CommandResultDto.Fail("Pick a colour.");
            }
            world.CreateCompany(userId, name, color);
            return CommandResultDto.Ok;
        });
    }

    private static bool IsColor(string color) =>
        color.Length == 7 && color[0] == '#' && color.Skip(1).All(Uri.IsHexDigit);

    /// <summary>Terrain and objects of these chunks (x0, y0, x1, y1, ...); the client gets updates of them from now on</summary>
    public async Task<IReadOnlyList<ChunkDto>> GetChunks(int[] chunks)
    {
        var coords = Coords(chunks);
        foreach (var (cx, cy) in coords)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ChunkGroup(cx, cy));
        }
        return await _world.RunAsync(world => coords.Select(t => GameViews.Chunk(world, t.X, t.Y)).ToList(), changesWorld: false);
    }

    /// <summary>The client no longer shows these chunks</summary>
    public async Task Unwatch(int[] chunks)
    {
        foreach (var (cx, cy) in Coords(chunks))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ChunkGroup(cx, cy));
        }
    }

    private static List<(int X, int Y)> Coords(int[] pairs)
    {
        if (pairs.Length % 2 != 0 || pairs.Length / 2 > MaxChunksPerRequest)
        {
            throw new HubException($"Ask for 1 to {MaxChunksPerRequest} chunks as x,y pairs.");
        }
        return Enumerable.Range(0, pairs.Length / 2).Select(t => (pairs[t * 2], pairs[t * 2 + 1])).Distinct().ToList();
    }

    public Task<DistrictDto> GetDistrict(int x, int y) =>
        _world.RunAsync(world => GameViews.District(world, new DistrictCoord(x, y)), changesWorld: false);

    public Task<IReadOnlyList<NetworkFlowsDto>> GetFlows(int[] networks) =>
        _world.RunAsync<IReadOnlyList<NetworkFlowsDto>>(world =>
        {
            var wanted = networks.Take(200).ToHashSet();
            return world.Networks.Where(t => wanted.Contains(t.Id)).Select(t => GameViews.Flows(world, t)).ToList();
        }, changesWorld: false);

    public Task<NetworkDetailsDto?> GetNetwork(int id) =>
        _world.RunAsync(world => world.Networks.FirstOrDefault(t => t.Id == id) is { } network ? GameViews.NetworkDetails(world, network) : null, changesWorld: false);

    public Task<BuildingDetailsDto?> GetBuilding(long id) =>
        _world.RunAsync(world => world.Building(id) is { } building ? GameViews.BuildingDetails(world, building) : null, changesWorld: false);

    public Task<IReadOnlyList<ResearchStateDto>> GetResearch() =>
        _world.RunAsync<IReadOnlyList<ResearchStateDto>>(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Research(world, company) : [], changesWorld: false);

    public Task<CommandResultDto> StartResearch(string tech) =>
        Command((world, company) => world.StartResearch(company, tech));

    public Task<CompanyDto?> GetCompany() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? CompanyView(world, company) : null, changesWorld: false);

    // ---------------------------------------------------------------- Commands

    private Task<CommandResultDto> Command(Func<GameWorld, long, CommandResult> command)
    {
        var userId = UserId;
        return _world.RunAsync(world =>
        {
            if (world.CompanyOfUser(userId) is not { } company)
            {
                return CommandResultDto.Fail("Found your company first.");
            }
            var result = command(world, company.Id);
            return result.Success ? CommandResultDto.Ok : CommandResultDto.Fail(result.Error ?? "That didn't work.");
        });
    }

    /// <summary>Testing only (TransportPipes:Cheats): gold, level, research or finish</summary>
    public Task<CommandResultDto> Cheat(string what)
    {
        if (!_options.Cheats)
        {
            return Task.FromResult(CommandResultDto.Fail("Cheats are off."));
        }
        return Command((world, companyId) =>
        {
            var company = world.Company(companyId)!;
            switch (what)
            {
                case "gold":
                    company.Gold += 100_000;
                    break;
                case "level":
                    world.GainXp(company, world.Content.Progression.XpForLevel(company.Level + 5) - company.Xp);
                    break;
                case "research":
                    company.ResearchPoints += 5_000;
                    break;
                case "finish":
                    foreach (var building in world.BuildingsOf(companyId).Where(t => t.UnderConstruction).ToList())
                    {
                        world.FinishConstruction(building.Id);
                    }
                    world.FinishResearch(companyId);
                    break;
                default:
                    return CommandResult.Fail("gold, level, research or finish");
            }
            world.CompanyChanged(company);
            return CommandResult.Ok;
        });
    }

    public Task<CommandResultDto> PlaceBuilding(string type, int x, int y) =>
        Command((world, company) => world.PlaceBuilding(company, type, new TilePos(x, y)));

    /// <summary>Pipe tiles as x0, y0, x1, y1, ...</summary>
    public Task<CommandResultDto> PlacePipes(int[] tiles, int tier) =>
        Command((world, company) => world.PlacePipes(company, Tiles(tiles), tier));

    public Task<CommandResultDto> RemovePipes(int[] tiles) =>
        Command((world, company) => world.RemovePipes(company, Tiles(tiles)));

    private static List<TilePos> Tiles(int[] pairs)
    {
        if (pairs.Length % 2 != 0 || pairs.Length > 2000)
        {
            throw new HubException("Send at most 1000 tiles as x,y pairs.");
        }
        return Enumerable.Range(0, pairs.Length / 2).Select(t => new TilePos(pairs[t * 2], pairs[t * 2 + 1])).ToList();
    }

    public Task<CommandResultDto> Upgrade(long building, int levels) =>
        Command((world, company) => world.Upgrade(company, building, Math.Clamp(levels, 1, 100)));

    public Task<CommandResultDto> Demolish(long building) =>
        Command((world, company) => world.Demolish(company, building));

    public Task<CommandResultDto> Move(long building, int x, int y) =>
        Command((world, company) => world.Move(company, building, new TilePos(x, y)));

    public Task<CommandResultDto> BuyParcel(int parcel) =>
        Command((world, company) => world.BuyParcel(company, parcel));

    public Task<CommandResultDto> SetSellPrice(string resource, double? price) =>
        Command((world, company) => world.SetSellPrice(company, resource, price));

    public Task<CommandResultDto> SetBuyMaxPrice(string resource, double? price) =>
        Command((world, company) => world.SetBuyMaxPrice(company, resource, price));

    public Task<CommandResultDto> SetImport(long building, string resource, bool enabled) =>
        Command((world, company) => world.SetImport(company, building, resource, enabled));

    public Task<CommandResultDto> SetValveFilter(long building, string resource, bool enabled) =>
        Command((world, company) => world.SetValveFilter(company, building, resource, enabled));

    public Task<CommandResultDto> SetGatePolicy(string side, string policy) =>
        Command((world, company) => Enum.TryParse<GateSide>(side, true, out var s) && Enum.TryParse<GatePolicy>(policy, true, out var p)
            ? world.SetGatePolicy(company, s, p)
            : CommandResult.Fail("Unknown gate or policy."));
}
