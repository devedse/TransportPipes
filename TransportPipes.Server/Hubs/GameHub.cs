using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TransportPipes.Database;
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
    Task MarketChanged(int districtX, int districtY);
    Task ExchangeChanged();
    Task ChatMessage(ChatMessageDto message);
    Task InboxChanged(int unread);
    Task WondersChanged();
    Task CrewChanged();

    /// <summary>Your links changed (an offer, an answer, a notice, the end)</summary>
    Task LinksChanged();

    /// <summary>A link somewhere was laid, rerouted or removed: the map draws them again</summary>
    Task MapLinksChanged();

    /// <summary>When the Yards' next round comes (a round ran, or something brought it sooner)</summary>
    Task YardRound(DateTime next);
}

/// <summary>The game protocol. Every call needs a logged in user (the login cookie).</summary>
[Authorize]
public sealed class GameHub : Hub<IGameClient>
{
    public const string Path = "/hubs/game";

    /// <summary>A client asks for at most this many chunks at once (a screen full when zoomed out)</summary>
    public const int MaxChunksPerRequest = 64;

    private static readonly TimeSpan ReportAfter = TimeSpan.FromMinutes(15);

    private readonly WorldHost _world;
    private readonly ConnectedUsers _connectedUsers;
    private readonly SocialService _social;
    private readonly AccountService _accounts;
    private readonly HistoryStore _history;
    private readonly TransportPipesOptions _options;

    public GameHub(WorldHost world, ConnectedUsers connectedUsers, SocialService social, AccountService accounts, HistoryStore history,
        Microsoft.Extensions.Options.IOptions<TransportPipesOptions> options)
    {
        _world = world;
        _connectedUsers = connectedUsers;
        _social = social;
        _accounts = accounts;
        _history = history;
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

    public async Task<HelloDto> Hello()
    {
        var userId = UserId;
        // The same-device signals (v1.2 §3): the browser's random id, and a day's hash of the IP address (never the address)
        var http = Context.GetHttpContext();
        var browserId = http?.Request.Query["bid"].ToString();
        var ip = http?.Connection.RemoteIpAddress?.ToString();
        // The playtest's players all connect from one machine: each gets an address of its own, or every pair would
        // count as one device and trade at the hub price (run 2 did, unseen; TransportPipes:Playtest, and the dev
        // server's e2e browsers where cheats are on)
        if ((_options.Playtest || _options.Cheats) && http?.Request.Headers["X-Playtest-Ip"].ToString() is { Length: > 0 } playtestIp)
        {
            ip = playtestIp;
        }
        var ipDay = string.IsNullOrEmpty(ip) ? null
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{ip}|{DateTime.UtcNow:yyyy-MM-dd}")))[..16];
        var user = new UserInfo(userId, UserName);
        var (isAdmin, _, _) = await _accounts.ModerationAsync(userId);
        user = user with { IsAdmin = isAdmin, IsGuest = await _accounts.IsGuestAsync(userId) };
        var start = await _world.RunAsync(world =>
        {
            var company = world.CompanyOfUser(userId);
            if (company == null)
            {
                return (Hello: new HelloDto(user, null, null, world.Now, 1, 0), Away: (GameViews.AwayFacts?)null, Company: (Company?)null, Crew: false);
            }
            // The return card (F5): what happened since the last session (from before the company wakes and its next report starts)
            var away = new GameViews.AwayFacts(world.Now - company.LastSeenAt, company.Report);
            var leftAt = company.LastSeenAt;
            var hasCard = world.Now - company.Report.Since >= ReportAfter;
            if (hasCard)
            {
                company.Report = new CompanyReport { Since = world.Now };
                company.Touch();
            }
            company.LastSeenAt = world.Now;
            company.SessionStartedAt = world.Now;
            world.SeenOn(company, browserId, ipDay);
            world.CountPlayDay(company);
            world.EnsureOrders(company);
            return (Hello: new HelloDto(user, CompanyView(world, company), null, world.Now, world.TownEra(company.District), 0, leftAt), Away: hasCard ? away : null, Company: company, Crew: world.CrewOf(company) != null);
        });
        var hello = start.Hello;
        if (start is { Away: { } facts, Company: { } me })
        {
            // A player without a crew: who answered them in the district chat while they were away
            var chatLine = start.Crew ? null : await ChatAnswerAsync(me, facts.Report.Since);
            hello = hello with { Card = await _world.RunAsync(world => GameViews.ReturnCard(world, me, facts, chatLine)) };
        }
        return hello.Company is { } dto ? hello with { Unread = await _social.UnreadAsync(dto.Id) } : hello;
    }

    /// <summary>"Mia answered you in the Capital chat, 3 min after you wrote": the others' messages after your last one, while you were away</summary>
    private async Task<string?> ChatAnswerAsync(Company me, DateTime since)
    {
        var history = await _social.HistoryAsync(SocialService.DistrictChannel(me.District.X, me.District.Y), me.Id, null);
        if (history.LastOrDefault(t => t.Sender == me.Id) is not { } mine)
        {
            return null;
        }
        var answers = history.Where(t => t.Id > mine.Id && t.Sender != me.Id && t.SentAt >= since).ToList();
        if (answers.Count == 0)
        {
            return null;
        }
        var names = answers.Select(t => t.SenderName).Distinct().Take(3).ToList();
        var after = answers[0].SentAt - mine.SentAt;
        var when = after.TotalMinutes < 1 ? "right after you wrote" : after.TotalHours < 1 ? $"{Math.Ceiling(after.TotalMinutes):0} min after you wrote" : after.TotalHours < 24 ? $"{(int)after.TotalHours} h after you wrote" : $"{(int)after.TotalDays} days after you wrote";
        return $"{(names.Count == 1 ? names[0] : $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}")} answered you in the {WorldNames.District(me.District)} chat, {when}";
    }

    private static CompanyDto CompanyView(GameWorld world, Company company) =>
        GameViews.Company(world, company, world.GoldPerMinute().GetValueOrDefault(company.Id));

    /// <summary>
    /// A new company (1.4 §6) on the plot the player chose on the map (a plot of a closed Yard: it plays where Pick for
    /// me puts it and asks there), or where Pick for me puts it; a friend's "Play next to me" code lets it into their
    /// Yard although the Yard is closed
    /// </summary>
    public async Task<CommandResultDto> CreateCompany(string name, string color, int? districtX, int? districtY, int? slot, string? code)
    {
        var userId = UserId;
        var (legacy, titles) = await _accounts.LegacyAsync(userId);
        var guest = await _accounts.IsGuestAsync(userId);
        return await _world.RunAsync(world =>
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
            var seat = districtX is { } x && districtY is { } y && slot is { } s ? new EstateKey(new DistrictCoord(x, y), s) : (EstateKey?)null;
            if (seat is { } chosen && world.Estate(chosen.District, chosen.Slot) is { OwnerId: not null })
            {
                return CommandResultDto.Fail("Someone took that plot a moment ago: pick another one.", "seatTaken");
            }
            var created = world.CreateCompany(userId, name, color, GameWorld.LegacyLevelOf(legacy), titles, seat, code);
            created.IsGuest = guest;
            world.CountPlayDay(created);
            world.EnsureOrders(created);
            return CommandResultDto.Ok;
        });
    }

    /// <summary>A town's Yards for the start on the map (the town Pick for me chooses when none is given)</summary>
    public Task<StationsDto> GetStations(int? districtX, int? districtY, string? code) =>
        _world.RunAsync(world =>
        {
            var company = world.CompanyOfUser(UserId);
            return GameViews.Stations(world, company, districtX is { } x && districtY is { } y ? new DistrictCoord(x, y) : null, code);
        });

    public Task<CrewViewDto?> GetCrew() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Crew(world, company) : null, changesWorld: false);

    /// <summary>Any resident names the Yard and picks its banner (1.4 §7)</summary>
    public Task<CommandResultDto> SetYard(string name, string emblem, string color, string color2) =>
        Command((world, company) => world.SetYard(company, name, emblem, color, color2));

    /// <summary>Find a player by name, to fly the map to them</summary>
    public Task<IReadOnlyList<FoundPlayerDto>> FindPlayer(string text) =>
        _world.RunAsync(world => GameViews.FindPlayer(world, text ?? ""), changesWorld: false);

    /// <summary>Any resident opens or closes the Yard</summary>
    public Task<CommandResultDto> SetYardOpen(bool open) =>
        Command((world, company) => world.SetYardOpen(company, open));

    /// <summary>Asks to settle on a free plot of a closed Yard (the request waits three days)</summary>
    public Task<CommandResultDto> RequestPlot(int districtX, int districtY, int slot) =>
        Command((world, company) => world.RequestPlot(company, new EstateKey(new DistrictCoord(districtX, districtY), slot)));

    /// <summary>A resident says yes (the newcomer moves in for free) or no to a request</summary>
    public Task<CommandResultDto> AnswerRequest(long requester, bool yes) =>
        Command((world, company) => world.AnswerRequest(company, requester, yes));

    /// <summary>Sets one of your prices (null: back to its default): a cut counts at once, a rise waits an hour (1.4 §8)</summary>
    public Task<CommandResultDto> SetPrice(string key, double? value) =>
        Command((world, company) => world.SetPrice(company, key, value));

    /// <summary>Fills one of today's hub orders from what your Storages hold of its good, paid at once</summary>
    public Task<CommandResultDto> DeliverOrder(int orderId) =>
        Command((world, company) => world.DeliverOrder(company, orderId));

    /// <summary>Your "Play next to me" link (1.4 §6): its code, valid for seven days</summary>
    public Task<string?> GetNextToMeLink() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameWorld.ShowCode(world.NextToMeLink(company.Id)) : null);

    /// <summary>A friend's tap: what your Storages hold of what a neighbour's scaffold wants goes into it (after run 3, step 2)</summary>
    public Task<CommandResultDto> SendToSite(long siteId) =>
        Command((world, company) => world.SendToSite(company, siteId));

    /// <summary>Buys the rest of a want, one's own or a neighbour's, at once from the market (1.4 §9)</summary>
    public Task<CommandResultDto> OrderRest(long target, string? resource) =>
        Command((world, company) => world.OrderRest(company, target, resource));

    /// <summary>Buys the rest of all your own wants at once</summary>
    public Task<CommandResultDto> OrderAll() =>
        Command((world, company) => world.OrderAll(company));

    /// <summary>Gives up a scaffold that still waits for its materials (minus a number: a pipe want): its gold comes back</summary>
    public Task<CommandResultDto> CancelSite(long siteId) =>
        Command((world, company) => siteId < 0 ? world.CancelPipeWant(company, -siteId) : world.CancelSite(company, siteId));

    /// <summary>Takes a building's last purchase back within the hour (after run 3: spending is safe)</summary>
    public Task<CommandResultDto> Undo(long buildingId) =>
        Command((world, company) => world.Undo(company, buildingId));

    /// <summary>Moves your plot to a free one (into a closed Yard with a friend's "Play next to me" code)</summary>
    public Task<CommandResultDto> RelocateTo(int districtX, int districtY, int slot, string? code) =>
        Command((world, company) => world.RelocateTo(company, new EstateKey(new DistrictCoord(districtX, districtY), slot), code));

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

    /// <summary>Upgrades the selection: each by a number of levels, or with a budget the cheapest levels first</summary>
    public Task<BulkUpgradeDto> UpgradeMany(long[] buildings, int levelsEach, double? budget)
    {
        var userId = UserId;
        return _world.RunAsync(world =>
        {
            if (world.CompanyOfUser(userId) is not { } company)
            {
                return new BulkUpgradeDto(false, "Found your company first.", 0, 0);
            }
            var (levels, gold, error) = world.UpgradeMany(company.Id, buildings.Take(500).ToList(), Math.Clamp(levelsEach, 1, 100), budget);
            return new BulkUpgradeDto(error == null, error, levels, gold);
        });
    }

    /// <summary>What upgrading the selection costs</summary>
    public Task<SelectionCostsDto> GetSelectionCosts(long[] buildings)
    {
        var userId = UserId;
        return _world.RunAsync(world =>
        {
            var company = world.CompanyOfUser(userId);
            var own = buildings.Take(500).Distinct().Select(world.Building).OfType<TransportPipes.Game.State.Building>()
                .Where(t => company != null && t.Owner == company.Id && world.CanUpgrade(t)).ToList();
            var max = world.Content.Progression.MaxBuildingLevel;
            // What they make together now: what flows out of them
            var ids = buildings.Take(500).ToHashSet();
            var makes = world.Networks
                .SelectMany(t => t.Solution.Flows)
                .Where(t => ids.Contains(t.From) && world.Building(t.From) is { } b && company != null && b.Owner == company.Id && world.Content.Building(b.Type).Kind == TransportPipes.Game.Content.BuildingKind.Producer)
                .GroupBy(t => t.Resource)
                .ToDictionary(t => t.Key, t => Math.Round(t.Sum(f => f.Sent), 2));
            // +10 counts from the level bought, as UpgradeMany does (it counted from the level reached: a building whose
            // goods were still coming showed more levels than it bought)
            Dictionary<string, double> Goods(Func<TransportPipes.Game.State.Building, int> levels) => own
                .SelectMany(t => world.StepGoods(t, levels(t))).GroupBy(t => t.Key).ToDictionary(t => t.Key, t => t.Sum(g => g.Value));
            return new SelectionCostsDto(
                Math.Round(own.Sum(t => world.UpgradeCost(t, 1)), 2),
                Math.Round(own.Sum(t => world.UpgradeCost(t, Math.Min(10, max - world.BoughtLevel(t)))), 2),
                own.Count == 0 ? 0 : Math.Round(own.Min(t => world.UpgradeCost(t, 1)), 2),
                own.Count,
                makes,
                company == null ? null : GameViews.LevelGoods(world, company, Goods(_ => 1)),
                company == null ? null : GameViews.LevelGoods(world, company, Goods(t => Math.Min(10, max - world.BoughtLevel(t)))));
        }, changesWorld: false);
    }

    /// <summary>What "Spend …, cheapest first" buys for a budget: its levels, their gold and their goods</summary>
    public Task<SpendPlanDto?> GetSpendPlan(long[] buildings, double budget)
    {
        var userId = UserId;
        return _world.RunAsync(world =>
        {
            if (world.CompanyOfUser(userId) is not { } company)
            {
                return null;
            }
            var (levels, gold, goods) = world.PlanCheapest(company, buildings.Take(500).ToList(), budget);
            return new SpendPlanDto(levels, gold, GameViews.LevelGoods(world, company, goods));
        }, changesWorld: false);
    }

    /// <summary>The advisor's three best next steps for the player's company</summary>
    public Task<List<NextStepDto>> GetNextSteps()
    {
        var userId = UserId;
        return _world.RunAsync(world => world.CompanyOfUser(userId) is { } company
            ? world.NextSteps(company).Select(t => GameViews.NextStep(t, world, company)).ToList()
            : [], changesWorld: false);
    }

    public Task<NetworkDetailsDto?> GetNetwork(int id)
    {
        var userId = UserId;
        return _world.RunAsync(world => world.Networks.FirstOrDefault(t => t.Id == id) is { } network
            ? GameViews.NetworkDetails(world, network, world.CompanyOfUser(userId)?.Id)
            : null, changesWorld: false);
    }

    public Task<BuildingDetailsDto?> GetBuilding(long id)
    {
        var userId = UserId;
        return _world.RunAsync(world => world.Building(id) is { } building ? GameViews.BuildingDetails(world, building, world.CompanyOfUser(userId)) : null, changesWorld: false);
    }

    public Task<IReadOnlyList<ResearchStateDto>> GetResearch() =>
        _world.RunAsync<IReadOnlyList<ResearchStateDto>>(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Research(world, company) : [], changesWorld: false);

    /// <summary>The tracks (the Lab's first line, 1.4 §12)</summary>
    public Task<IReadOnlyList<TrackStateDto>> GetTracks() =>
        _world.RunAsync<IReadOnlyList<TrackStateDto>>(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Tracks(world, company) : [], changesWorld: false);

    /// <summary>Starts an Era tree item on the second line, or a track's next tier on the first</summary>
    public Task<CommandResultDto> StartResearch(string tech) =>
        Command((world, company) => world.StartResearch(company, tech));

    /// <summary>A line's queue (1.4 §12): up to five, started by themselves in order; a track once for each of its tiers</summary>
    public Task<CommandResultDto> QueueResearch(string tech, bool queued) =>
        Command((world, company) => world.QueueResearch(company, tech, queued));

    public Task<CommandResultDto> SetKeepResearching(bool keep) =>
        Command((world, company) => world.SetKeepResearching(company, keep));

    public Task<MarketDto?> GetMarket() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Market(world, company) : null, changesWorld: false);

    public Task<CommandResultDto> SetPin(string resource, bool need, bool on) =>
        Command((world, company) => world.SetPin(company, resource, need, on));

    public Task<YardDto?> GetYard() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Yard(world, company) : null, changesWorld: false);

    // ---------------------------------------------------------------- Links (v1.2 §7)

    public Task<LinksDto?> GetLinks() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Links(world, company) : null, changesWorld: false);

    public Task<LinkPlanDto?> PlanLink(long partnerId, string resource, bool selling, int tier) =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.LinkPlan(world, world.PlanLink(company.Id, partnerId, resource, selling, tier)) : null, changesWorld: false);

    public Task<CommandResultDto> OfferLink(long partnerId, string resource, bool selling, int tier) =>
        Command((world, company) => world.OfferLink(company, partnerId, resource, selling, tier).Result);

    public Task<CommandResultDto> AcceptLink(long linkId) =>
        Command((world, company) => world.AcceptLink(company, linkId));

    public Task<CommandResultDto> DeclineLink(long linkId) =>
        Command((world, company) => world.DeclineLink(company, linkId));

    public Task<CommandResultDto> EndLink(long linkId, bool end) =>
        Command((world, company) => world.EndLink(company, linkId, end));

    public Task<CommandResultDto> ResumeLink(long linkId) =>
        Command((world, company) => world.ResumeLink(company, linkId));

    public Task<CommandResultDto> UpgradeLink(long linkId, int tier) =>
        Command((world, company) => world.UpgradeLink(company, linkId, tier));

    public Task<IReadOnlyList<MapLinkDto>> GetMapLinks() =>
        _world.RunAsync(GameViews.MapLinks, changesWorld: false);

    public Task<IReadOnlyList<NeighbourDto>> GetNeighbourhood() =>
        _world.RunAsync<IReadOnlyList<NeighbourDto>>(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Neighbourhood(world, company) : [], changesWorld: false);

    public Task<ExchangeDto?> GetExchange() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Exchange(world, company) : null, changesWorld: false);

    public Task<CommandResultDto> PlaceExchangeOrder(string resource, bool selling, double amount, double price) =>
        Command((world, company) => world.PlaceExchangeOrder(company, resource, selling, amount, price));

    public Task<CommandResultDto> CancelExchangeOrder(long order) =>
        Command((world, company) => world.CancelExchangeOrder(company, order));

    public Task<CommandResultDto> SetNoTrade(string resource, bool noTrade) =>
        Command((world, company) => world.SetNoTrade(company, resource, noTrade));

    public Task<CommandResultDto> SetTradeBlocked(long other, bool blocked) =>
        Command((world, company) => world.SetTradeBlocked(company, other, blocked));

    public Task<CommandResultDto> SetNoBuy(string resource, bool noBuy) =>
        Command((world, company) => world.SetNoBuy(company, resource, noBuy));

    // ---------------------------------------------------------------- Company: managers and achievements

    public Task<CompanyDetailsDto?> GetCompanyDetails() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.CompanyDetails(world, company) : null, changesWorld: false);

    public Task<CommandResultDto> HireManager(string manager) =>
        Command((world, company) => world.HireManager(company, manager));

    public Task<CommandResultDto> DismissManager(string manager) =>
        Command((world, company) => world.DismissManager(company, manager));

    // ---------------------------------------------------------------- Moving (GDD §12)

    public Task<IReadOnlyList<DistrictChoiceDto>> GetDistricts() =>
        _world.RunAsync<IReadOnlyList<DistrictChoiceDto>>(world =>
        {
            if (world.CompanyOfUser(UserId) is not { } me)
            {
                return [];
            }
            return world.OpenDistricts.OrderBy(t => t.X).ThenBy(t => t.Y).Select(district =>
            {
                var estates = Enumerable.Range(0, WorldLayout.EstatesPerDistrict).Select(slot => world.Estate(district, slot)).OfType<Estate>().ToList();
                var players = world.Companies.Where(t => t.District == district).ToList();
                var free = estates.Count(t => t.OwnerId == null);
                return new DistrictChoiceDto(
                    district.X,
                    district.Y,
                    WorldNames.District(district),
                    free,
                    players.Count,
                    players.Count == 0 ? 0 : players.Count(t => world.ActivityOf(t) == Activity.Active) / (double)players.Count,
                    district == me.District);
            }).ToList();
        }, changesWorld: false);

    public Task<CommandResultDto> PlaceCrate(int crate, int x, int y) =>
        Command((world, company) => world.PlaceCrate(company, crate, new TilePos(x, y)));

    public Task<CommandResultDto> ScrapCrate(int crate) =>
        Command((world, company) => world.ScrapCrate(company, crate));

    // ---------------------------------------------------------------- Outposts

    public Task<OutpostCheckDto?> CheckOutpost(int x, int y) =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.OutpostCheck(world, company, new TilePos(x, y)) : null, changesWorld: false);

    public Task<CommandResultDto> ClaimOutpost(int x, int y) =>
        Command((world, company) => world.ClaimOutpost(company, new TilePos(x, y)));

    public Task<CommandResultDto> ReleaseOutpost(long outpost) =>
        Command((world, company) => world.ReleaseOutpost(company, outpost));

    public Task<CommandResultDto> StartExpedition(int hours) =>
        Command((world, company) => world.StartExpedition(company, hours));

    public Task<CommandResultDto> SkipTutorial() =>
        Command((world, company) => world.SkipTutorial(company));

    public Task<CommandResultDto> Evolve(long building) =>
        Command((world, company) => world.Evolve(company, building));

    // ---------------------------------------------------------------- Teleporters

    public Task<CommandResultDto> LinkTeleporter(long sender, long receiver, string resource) =>
        Command((world, company) => world.LinkTeleporter(company, sender, receiver, resource));

    public Task<CommandResultDto> UnlinkTeleporter(long station) =>
        Command((world, company) => world.UnlinkTeleporter(company, station));

    // ---------------------------------------------------------------- The Deep Level

    public Task<CommandResultDto> Dig(int parcel) =>
        Command((world, company) => world.Dig(company, parcel));

    // ---------------------------------------------------------------- Blueprints

    public Task<BlueprintsDto?> GetBlueprints() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Blueprints(world, company) : null, changesWorld: false);

    public Task<CommandResultDto> SaveBlueprint(string name, int x, int y, int width, int height) =>
        Command((world, company) => world.SaveBlueprint(company, name, new TilePos(x, y), width, height));

    public Task<CommandResultDto> BuildBlueprint(long blueprint, int x, int y) =>
        Command((world, company) => world.BuildBlueprint(company, blueprint, new TilePos(x, y)));

    public Task<CommandResultDto> ShareBlueprint(long blueprint, BlueprintSharing sharing) =>
        Command((world, company) => world.ShareBlueprint(company, blueprint, sharing));

    public Task<CommandResultDto> RateBlueprint(long blueprint, int stars) =>
        Command((world, company) => world.RateBlueprint(company, blueprint, stars));

    public Task<CommandResultDto> DeleteBlueprint(long blueprint) =>
        Command((world, company) => world.DeleteBlueprint(company, blueprint));

    // ---------------------------------------------------------------- Your town: its Wonder, your Yard's Monuments (1.4 §13)

    public Task<TownDto?> GetTown() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Town(world, company) : null, changesWorld: false);

    /// <summary>What you give the Wonder of a good by itself: "off", "beyondHubOrder" or "all"</summary>
    public Task<CommandResultDto> SetGive(string resource, string mode) =>
        Enum.TryParse<GiveMode>(mode, ignoreCase: true, out var value)
            ? Command((world, company) => world.SetGive(company, resource, value))
            : Task.FromResult(CommandResultDto.Fail("Unknown choice."));

    /// <summary>Gives the Wonder what your Storages hold of what it lacks</summary>
    public Task<CommandResultDto> GiveFromStorages() =>
        Command((world, company) => world.GiveFromStorages(company));

    /// <summary>Buys what the stage lacks (one good, or every good) at the market's 2.5× and gives it; gold: at most this much</summary>
    public Task<CommandResultDto> BuyAndGive(string? resource, double? gold) =>
        Command((world, company) => world.BuyAndGive(company, resource, gold));

    public Task<CommandResultDto> ChooseMonument(int era, string kind) =>
        Command((world, company) => world.ChooseMonument(company, era, kind));

    public Task<CommandResultDto> PayMonument(int era, double gold) =>
        Command((world, company) => world.PayMonument(company, era, gold));

    // ---------------------------------------------------------------- Trunk lines (1.4 §20)

    public Task<LinesDto?> GetLines() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Lines(world, company) : null, changesWorld: false);

    /// <summary>Builds a trunk line to a neighbouring town in a tier: you are its carrier</summary>
    public Task<CommandResultDto> BuildLine(int toX, int toY, int tier) =>
        Command((world, company) => world.BuildLine(company, new DistrictCoord(toX, toY), tier));

    /// <summary>A carrier's fee (of the goods' base value) for "public", "crew" or a player's id (null: their special fee goes)</summary>
    public Task<CommandResultDto> SetLineFee(long line, string who, double? fee) =>
        Command((world, company) => world.SetLineFee(company, line, who, fee));

    /// <summary>Sends a good over a line to the other town's market (null: stops)</summary>
    public Task<CommandResultDto> SetExport(string resource, long? line) =>
        Command((world, company) => world.SetExport(company, resource, line));

    // ---------------------------------------------------------------- The Yard's Depot and the outskirts (1.4 §5, §7)

    /// <summary>Puts goods from your Storages into your Yard's Depot, for the crew</summary>
    public Task<CommandResultDto> PutInDepot(string resource, double amount) =>
        Command((world, company) => world.PutInDepot(company, resource, amount));

    /// <summary>Takes your own goods back from the Depot</summary>
    public Task<CommandResultDto> TakeFromDepot(string resource, double amount) =>
        Command((world, company) => world.TakeFromDepot(company, resource, amount));

    /// <summary>Buys one of the nine outskirts parcels set aside for your plot</summary>
    public Task<CommandResultDto> BuyOutskirts(int index) =>
        Command((world, company) => world.BuyOutskirts(company, index));

    /// <summary>Whether the market brings what your levels and buildings still lack, at 2.5× (on by default)</summary>
    public Task<CommandResultDto> SetWantsFromMarket(bool on) =>
        Command((world, company) => world.SetWantsFromMarket(company, on));

    // ---------------------------------------------------------------- Statistics (docs/design-notes/statistics.md)

    /// <summary>A company's statistics over a range (day, week, month, all): your own whole, another's public part</summary>
    public async Task<StatisticsDto?> GetStatistics(long? company, string range)
    {
        var userId = UserId;
        if (await _world.RunAsync(world => Statistics.Of(world, userId, company), changesWorld: false) is not { } live)
        {
            return null;
        }
        range = Statistics.RangeName(range);
        var (from, step) = Statistics.Range(range, live.Now, live.Founded);
        var samples = await _history.SamplesAsync(StatSubject.Company, live.Company, from, step);
        var moments = await _history.MomentsAsync(live.Company, from, bigOnly: !live.Own);
        var town = await _history.SamplesAsync(StatSubject.Town, GameWorld.TownKey(live.District), from, step);
        return Statistics.Dto(live, range, from, samples, moments, town, WorldNames.District(live.District));
    }

    /// <summary>One series of another company over a range, to compare with: only one everyone may see</summary>
    public async Task<StatsSeriesDto?> GetStatisticsSeries(long company, string key, string range)
    {
        var userId = UserId;
        if (await _world.RunAsync(world => Statistics.Of(world, userId, company), changesWorld: false) is not { } live
            || (!live.Own && !Statistics.Public.Contains(key)))
        {
            return null;
        }
        var (from, step) = Statistics.Range(Statistics.RangeName(range), live.Now, live.Founded);
        return Statistics.Series(live, key, await _history.SamplesAsync(StatSubject.Company, live.Company, from, step));
    }

    /// <summary>The admin's Pace: every company's gold earned by days since it was founded, and the model's player</summary>
    public async Task<PaceDto?> GetPace()
    {
        if (!await IsAdminAsync())
        {
            return null;
        }
        var daily = await _history.DailyAsync("earned");
        var companies = await _world.RunAsync(world => world.Companies.Select(t => (t.Id, t.Name, t.Color, t.CreatedAt, t.LifetimeEarnings)).ToList(), changesWorld: false);
        var now = _world.Now;
        var lines = companies.Select(c =>
        {
            var points = daily.GetValueOrDefault(c.Id, []).Where(t => t.At > c.CreatedAt).Append((At: now, Value: c.LifetimeEarnings)).ToList();
            return new PaceLineDto(c.Id, c.Name, c.Color, c.CreatedAt, points.Select(t => Math.Round((t.At - c.CreatedAt).TotalDays, 3)).Prepend(0).ToList(),
                points.Select(t => t.Value).Prepend(0).ToList());
        }).ToList();
        return new PaceDto(lines, ModelPace.Earned);
    }

    // ---------------------------------------------------------------- Visiting, mentors and leaderboards

    public Task<LeaderboardDto?> GetLeaderboard(LeaderboardKind kind, bool district) =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Leaderboard(world, company, kind, district) : null, changesWorld: false);

    public async Task<EstateVisitDto?> VisitEstate(long owner)
    {
        var userId = UserId;
        var viewer = await _world.RunAsync(world => world.CompanyOfUser(userId)?.Id, changesWorld: false);
        if (viewer is not { } viewerId)
        {
            return null;
        }
        var guestbook = await _social.GuestbookAsync(owner, viewerId);
        return await _world.RunAsync(world =>
        {
            if (world.Company(owner) is not { } company || world.Company(viewerId) is not { } me)
            {
                return null;
            }
            world.RecordVisit(company, me);
            return GameViews.Visit(world, company, me, guestbook);
        });
    }

    public Task<CommandResultDto> LikeEstate(long owner) =>
        Command((world, company) => world.LikeEstate(company, owner));

    public Task<CommandResultDto> ChooseMentor(long mentor) =>
        Command((world, company) => world.ChooseMentor(company, mentor));

    public async Task<CommandResultDto> WriteGuestbook(long owner, string text)
    {
        var userId = UserId;
        var (author, error) = await _world.RunAsync<((long Id, string Name)?, string?)>(world =>
        {
            if (world.CompanyOfUser(userId) is not { } company)
            {
                return (null, "Found your company first.");
            }
            if (world.Company(owner) is not { } ownerCompany || ownerCompany.Id == company.Id)
            {
                return (null, "You can only write in other players' guestbooks.");
            }
            return ((company.Id, company.Name), null);
        }, changesWorld: false);
        if (author is not { } me)
        {
            return CommandResultDto.Fail(error!);
        }
        if (await _social.WriteNoteAsync(owner, me.Id, me.Name, text) is { } failed)
        {
            return CommandResultDto.Fail(failed);
        }
        await _world.NotifyAsync([(owner, "guestbook", $"{me.Name} wrote in your guestbook: {text.Trim()}")]);
        return CommandResultDto.Ok;
    }

    public async Task<CommandResultDto> DeleteGuestbookNote(long note)
    {
        var userId = UserId;
        var company = await _world.RunAsync(world => world.CompanyOfUser(userId)?.Id, changesWorld: false);
        return company is { } id && await _social.DeleteNoteAsync(id, note) ? CommandResultDto.Ok : CommandResultDto.Fail("That note is not in your guestbook.");
    }

    // ---------------------------------------------------------------- Chat

    /// <summary>Who may read and write a channel, and who gets its messages (user ids)</summary>
    private static (bool Allowed, string Name, List<long> Recipients) Channel(GameWorld world, Company company, string channel)
    {
        if (channel == SocialService.DistrictChannel(company.District.X, company.District.Y))
        {
            var recipients = world.Companies.Where(t => t.District == company.District).Select(t => t.UserId).ToList();
            return (true, WorldNames.District(company.District), recipients);
        }
        if (channel == SocialService.WorldChannel)
        {
            return (true, "World", world.Companies.Select(t => t.UserId).ToList());
        }
        // Your Yard's own channel (after run 3, row 12: "Capital chat full of other Mias … unclear who my friends are")
        if (world.CrewOf(company) is { } crew && channel == SocialService.CrewChannel(crew.Id))
        {
            return (true, crew.Name, world.CrewMembers(crew).Select(t => t.UserId).ToList());
        }
        if (SocialService.DirectPair(channel) is { } pair && (pair.A == company.Id || pair.B == company.Id)
            && world.Company(pair.A == company.Id ? pair.B : pair.A) is { } other)
        {
            return (true, other.Name, [company.UserId, other.UserId]);
        }
        return (false, "", []);
    }

    public async Task<ChatDto?> GetChat()
    {
        var userId = UserId;
        var me = await _world.RunAsync(world => world.CompanyOfUser(userId) is { } company
            ? (company.Id, company.District, world.CrewOf(company) is { } crew ? (crew.Id, crew.NameHidden ? $"Crew {crew.Id}" : crew.Name, world.CrewMembers(crew).Count()) : default((long Id, string Name, int Members)?))
            : default((long, DistrictCoord, (long Id, string Name, int Members)?)?), changesWorld: false);
        if (me is not { } info)
        {
            return null;
        }
        var (companyId, district, ownCrew) = info;
        // Your Yard first, then your town and the world (1.4 §19); alone in your Yard the town first, where people are
        // (run 4: Mia's four greetings went to a channel that held only a silent Jo)
        var channels = new List<ChatChannelDto>();
        var districtChannel = SocialService.DistrictChannel(district.X, district.Y);
        var districtLine = new ChatChannelDto(districtChannel, WorldNames.District(district), "district", await _social.LastMessageAtAsync(districtChannel));
        if (ownCrew is not { Members: > 1 })
        {
            channels.Add(districtLine);
        }
        if (ownCrew is { } myCrew)
        {
            var crewChannel = SocialService.CrewChannel(myCrew.Id);
            channels.Add(new ChatChannelDto(crewChannel, myCrew.Name, "crew", await _social.LastMessageAtAsync(crewChannel)));
        }
        if (ownCrew is { Members: > 1 })
        {
            channels.Add(districtLine);
        }
        channels.Add(new ChatChannelDto(SocialService.WorldChannel, "World", "world", await _social.LastMessageAtAsync(SocialService.WorldChannel)));
        var direct = await _social.DirectChannelsAsync(companyId);
        var names = await _world.RunAsync(world => direct.ToDictionary(t => t.Channel, t =>
        {
            var pair = SocialService.DirectPair(t.Channel)!.Value;
            return world.Company(pair.A == companyId ? pair.B : pair.A)?.Name;
        }), changesWorld: false);
        foreach (var (channel, lastAt) in direct)
        {
            if (names.GetValueOrDefault(channel) is { } name)
            {
                channels.Add(new ChatChannelDto(channel, name, "direct", lastAt));
            }
        }
        return new ChatDto(channels, await _social.BlockedAsync(companyId));
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetChatHistory(string channel, long? before)
    {
        var userId = UserId;
        var company = await _world.RunAsync(world => world.CompanyOfUser(userId) is { } c && Channel(world, c, channel).Allowed ? c.Id : (long?)null, changesWorld: false);
        return company is { } id ? await _social.HistoryAsync(channel, id, before) : [];
    }

    public async Task<CommandResultDto> SendChat(string channel, string text)
    {
        var userId = UserId;
        var (me, recipients, error) = await _world.RunAsync<((long Id, string Name)?, List<long>, string?)>(world =>
        {
            if (world.CompanyOfUser(userId) is not { } company)
            {
                return (null, [], "Found your company first.");
            }
            var (allowed, _, users) = Channel(world, company, channel);
            return allowed ? ((company.Id, company.Name), users, null) : (null, [], "You can't write in that channel.");
        }, changesWorld: false);
        if (me is not { } sender)
        {
            return CommandResultDto.Fail(error!);
        }
        if (SocialService.DirectPair(channel) is { } pair)
        {
            var other = pair.A == sender.Id ? pair.B : pair.A;
            if ((await _social.BlockedAsync(other)).Contains(sender.Id))
            {
                return CommandResultDto.Fail("This company doesn't take your messages.");
            }
        }
        if ((await _accounts.ModerationAsync(userId)).MutedUntil is { } muted)
        {
            return CommandResultDto.Fail($"A moderator muted you until {muted:HH:mm} UTC.");
        }
        if (_social.CheckMessage(sender.Id, text) is { } failed)
        {
            return CommandResultDto.Fail(failed);
        }
        var first = !await _social.EverSpokeAsync(sender.Id);
        var message = await _social.SendAsync(channel, sender.Id, sender.Name, text);
        await Clients.Groups(recipients.Distinct().Select(UserGroup).ToList()).ChatMessage(message);
        if (first && (channel.StartsWith("district:", StringComparison.Ordinal) || channel.StartsWith("station:", StringComparison.Ordinal)))
        {
            await TellCrewsOfAHello(userId, channel);
        }
        return CommandResultDto.Ok;
    }

    /// <summary>
    /// A newcomer's first hello, as one line in the chats of the crews that live in its district (after run 4: the crew
    /// only ever wrote in its Crew channel, and nobody answered the player alone for two weeks)
    /// </summary>
    private async Task TellCrewsOfAHello(long userId, string channel)
    {
        var told = await _world.RunAsync(world =>
        {
            if (world.CompanyOfUser(userId) is not { } company || world.CrewOf(company) != null || world.EstateOf(company) is not { } estate)
            {
                return [];
            }
            var where = channel.StartsWith("district:", StringComparison.Ordinal) ? WorldNames.District(company.District) : WorldNames.Station(estate.Station);
            var text = $"👋 {company.Name}, new in {WorldNames.Station(estate.Station)}, said hello in the {where} chat: answer there";
            return world.Crews
                .Select(crew => (Crew: crew, Members: world.CrewMembers(crew).ToList()))
                .Where(t => t.Members.Any(m => m.District == company.District))
                .Select(t => (Channel: SocialService.CrewChannel(t.Crew.Id), Text: text, Users: t.Members.Select(m => m.UserId).ToList()))
                .ToList();
        }, changesWorld: false);
        foreach (var (crewChannel, text, users) in told)
        {
            var line = await _social.SendAsync(crewChannel, 0, "Newcomer", text);
            await Clients.Groups(users.Distinct().Select(UserGroup).ToList()).ChatMessage(line);
        }
    }

    public async Task<CommandResultDto> ReportChat(long message)
    {
        var userId = UserId;
        // The world may only be read inside RunAsync: collect the channels the reporter may read first
        var me = await _world.RunAsync(world => world.CompanyOfUser(userId) is { } company
            ? (company.Id, SocialService.DistrictChannel(company.District.X, company.District.Y), company.CrewId is { } crewId ? SocialService.CrewChannel(crewId) : null)
            : default((long Id, string District, string? Crew)?), changesWorld: false);
        if (me is not { } reporter)
        {
            return CommandResultDto.Fail("Found your company first.");
        }
        var reported = await _social.ReportAsync(message, channel =>
            channel == reporter.District || channel == reporter.Crew || channel == SocialService.WorldChannel || SocialService.DirectPair(channel) is { } pair && (pair.A == reporter.Id || pair.B == reporter.Id));
        return reported != null ? CommandResultDto.Ok : CommandResultDto.Fail("Unknown message.");
    }

    public async Task<CommandResultDto> SetBlocked(long company, bool blocked)
    {
        var userId = UserId;
        var me = await _world.RunAsync(world => world.CompanyOfUser(userId)?.Id, changesWorld: false);
        if (me is not { } id || id == company)
        {
            return CommandResultDto.Fail("You can't block yourself.");
        }
        await _social.SetBlockedAsync(id, company, blocked);
        return CommandResultDto.Ok;
    }

    // ---------------------------------------------------------------- Moderation (admins only)

    private async Task<bool> IsAdminAsync() => (await _accounts.ModerationAsync(UserId)).IsAdmin;

    public async Task<IReadOnlyList<ReportedMessageDto>> GetReports() =>
        await IsAdminAsync() ? await _social.ReportsAsync() : [];

    public async Task<CommandResultDto> ModerateMessage(long message, string action)
    {
        if (!await IsAdminAsync())
        {
            return CommandResultDto.Fail("Only moderators do that.");
        }
        var sender = await _social.SenderAsync(message);
        var user = sender is { } company ? await _world.RunAsync(world => world.Company(company)?.UserId, changesWorld: false) : null;
        switch (action)
        {
            case "dismiss":
                await _social.DismissReportAsync(message);
                break;
            case "delete":
                await _social.DeleteMessageAsync(message);
                break;
            case "mute" when user is { } muted:
                await _accounts.MuteAsync(muted, TimeSpan.FromHours(24));
                await _social.DeleteMessageAsync(message);
                break;
            case "ban" when user is { } banned:
                await _accounts.BanAsync(banned, TimeSpan.FromDays(7));
                await _social.DeleteMessageAsync(message);
                break;
            default:
                return CommandResultDto.Fail("dismiss, delete, mute or ban");
        }
        return CommandResultDto.Ok;
    }

    /// <summary>Renames a company with an offensive name</summary>
    public async Task<CommandResultDto> RenameCompany(long company, string name)
    {
        if (!await IsAdminAsync())
        {
            return CommandResultDto.Fail("Only moderators do that.");
        }
        return await _world.RunAsync(world =>
        {
            if (world.Company(company) is not { } target)
            {
                return CommandResultDto.Fail("Unknown company.");
            }
            if (world.CompanyNameError(name) is { } error)
            {
                return CommandResultDto.Fail(error);
            }
            target.Name = name.Trim();
            world.CompanyChanged(target);
            return CommandResultDto.Ok;
        });
    }

    // ---------------------------------------------------------------- Inbox

    public async Task<InboxDto?> GetInbox()
    {
        var userId = UserId;
        var company = await _world.RunAsync(world => world.CompanyOfUser(userId)?.Id, changesWorld: false);
        return company is { } id ? await _social.InboxAsync(id) : null;
    }

    public async Task MarkInboxRead()
    {
        var userId = UserId;
        if (await _world.RunAsync(world => world.CompanyOfUser(userId)?.Id, changesWorld: false) is { } id)
        {
            await _social.MarkReadAsync(id);
            await Clients.Group(UserGroup(userId)).InboxChanged(0);
        }
    }

    public Task<CompanyDto?> GetCompany() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? CompanyView(world, company) : null, changesWorld: false);

    // ---------------------------------------------------------------- Commands

    private Task<CommandResultDto> Command(Func<GameWorld, long, CommandResult> command, [CallerMemberName] string what = "")
    {
        var userId = UserId;
        return _world.RunAsync(world =>
        {
            if (world.CompanyOfUser(userId) is not { } company)
            {
                return CommandResultDto.Fail("Found your company first.");
            }
            var result = command(world, company.Id);
            return result.Success
                ? result.Note is { } note ? new CommandResultDto(true, null, null, note) : CommandResultDto.Ok
                : CommandResultDto.Fail(result.Error ?? "That didn't work.");
        }, what: what);
    }

    /// <summary>Testing only (TransportPipes:Cheats): gold, level, research, finish, away (show the offline report) or fill (fill the storages)</summary>
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
                case "deep":
                    // The whole Deep Level dug out
                    company.DeepParcels = (1 << WorldLayout.ParcelCount) - 1;
                    company.Touch();
                    break;
                case "earn":
                    // A million earned
                    company.Gold += 1_000_000;
                    world.CountEarned(company, 1_000_000, "cheats", "earn");
                    break;
                case "level":
                    // Five Company Levels: the gold earned they need, and that gold
                    var gold = world.Content.Progression.EarnedForLevel(company.Level + 5) - company.LifetimeEarnings;
                    company.Gold += Math.Max(0, gold);
                    world.CountEarned(company, gold, "cheats", "level");
                    break;
                case "research":
                    // Both lines' research done at once
                    world.FinishResearch(companyId);
                    break;
                case "bricks":
                    // 600 bricks into its Storages (wants, after run 3)
                    world.Stock(companyId, "bricks", 600);
                    break;
                case "tutorial":
                    // On to Loady's next step
                    company.TutorialStep = Math.Min(company.TutorialStep + 1, world.Content.Tutorial.Count);
                    company.Touch();
                    break;
                case "yard":
                    world.CoupleNow(3);
                    break;
                case "finish":
                    foreach (var building in world.BuildingsOf(companyId).Where(t => t.UnderConstruction).ToList())
                    {
                        world.FinishConstruction(building.Id);
                    }
                    world.FinishResearch(companyId);
                    world.StandFullStageNow(company.District);
                    if (company.DigEndsAt != null)
                    {
                        // The miners are done a moment from now
                        company.DigEndsAt = world.Now.AddSeconds(1);
                    }
                    break;
                case "storm":
                case "heatwave":
                case "expo":
                case "shortage":
                case "festival":
                    var kind = Enum.Parse<EventKind>(what, ignoreCase: true);
                    var resource = kind is EventKind.Expo or EventKind.Shortage ? world.Content.ResourceList.First(t => t.Tier == 2).Id : null;
                    world.StartEventNow(kind, kind == EventKind.Festival ? null : company.District, TimeSpan.FromHours(2), resource);
                    break;
                case "era":
                    world.AdvanceEra(company.District);
                    break;
                case "alltech":
                    world.GrantAllResearch(company);
                    break;
                case "eratech":
                    // Every tier and tree item of your Era
                    world.GrantAllResearch(company, thisEra: true);
                    break;
                case "district":
                    world.OpenNextDistrict();
                    break;
                case "arrive":
                    world.LandNow(company);
                    break;
                case "gone":
                    // The next Hello comes as if the player was away for ten days (the way back's screens)
                    company.LastSeenAt = world.Now.AddDays(-10);
                    company.Report.Since = company.LastSeenAt;
                    company.Touch();
                    break;
                case "era5":
                    // Your town to Era 5
                    while (world.TownEra(company.District) < GameWorld.LastEra)
                    {
                        world.AdvanceEra(company.District);
                    }
                    break;
                case "discover":
                    // A copper Discovery in the wilderness north of your estate, 8 tiles from the district's edge
                    var estateOrigin = world.EstateOf(company)!.Origin;
                    var edge = Enumerable.Range(0, 200).Select(d => new TilePos(estateOrigin.X + 12, estateOrigin.Y - 5 - d))
                        .First(t => WorldLayout.ZoneAt(t.X, t.Y).Kind == ZoneKind.Wilderness);
                    var spot = Enumerable.Range(8, 15).Select(d => edge.Offset(0, -d))
                        .FirstOrDefault(t => world.Generator.Tile(t).IsBuildableTerrain && world.OutpostAt(t) == null);
                    world.StartDiscovery(spot, "copperOre");
                    break;
                case "stock":
                    // 1,000 of every resource of this Era in your first storage
                    if (world.BuildingsOf(companyId).FirstOrDefault(t => world.Content.Building(t.Type).Kind == TransportPipes.Game.Content.BuildingKind.Storage) is not { } store)
                    {
                        return CommandResult.Fail("Build a storage first.");
                    }
                    foreach (var definition in world.Content.ResourceList.Where(t => t.Era <= world.YourEra(company) && t.Id != "energy"))
                    {
                        store.Contents[definition.Id] = store.Contents.GetValueOrDefault(definition.Id) + 1000;
                    }
                    store.Touch();
                    break;
                case "wonder":
                    // Fills your storages with what your town's Wonder's stage still lacks
                    var silo = world.BuildingsOf(companyId).FirstOrDefault(t => world.Content.Building(t.Type).Kind == TransportPipes.Game.Content.BuildingKind.Storage);
                    if (silo == null || world.WonderOf(company.District) is not { } townWonder)
                    {
                        return CommandResult.Fail("Build a storage first.");
                    }
                    foreach (var need in townWonder.Required.Keys)
                    {
                        silo.Contents[need] = silo.Contents.GetValueOrDefault(need) + world.WonderLacks(townWonder, need);
                    }
                    silo.Touch();
                    break;
                case "away":
                    // The next Hello shows the report as if the player was away for an hour
                    company.Report.Since = world.Now.AddHours(-1);
                    company.Touch();
                    break;
                case "fill":
                    foreach (var storage in world.BuildingsOf(companyId).Where(t => world.Content.Building(t.Type).Kind == TransportPipes.Game.Content.BuildingKind.Storage))
                    {
                        storage.Contents["ironOre"] = world.StorageCapacity(storage, world.Content.Building(storage.Type)) - storage.StoredTotal + storage.Contents.GetValueOrDefault("ironOre");
                        storage.Touch();
                    }
                    break;
                default:
                    return CommandResult.Fail("gold, level, research, finish, away, fill, storm, heatwave, expo, shortage or festival");
            }
            world.CompanyChanged(company);
            return CommandResult.Ok;
        });
    }

    public Task<CommandResultDto> PlaceBuilding(string type, int x, int y) =>
        Command((world, company) => world.PlaceBuilding(company, type, new TilePos(x, y)));

    /// <summary>A painted row or area: spots as x0, y0, x1, y1, ...</summary>
    public Task<BulkPlaceDto> PlaceBuildings(string type, int[] spots)
    {
        var userId = UserId;
        return _world.RunAsync(world =>
        {
            if (world.CompanyOfUser(userId) is not { } company)
            {
                return new BulkPlaceDto(0, 0, "Found your company first.");
            }
            var (placed, gold, error) = world.PlaceBuildings(company.Id, type, Tiles(spots));
            return new BulkPlaceDto(placed, gold, error);
        });
    }

    /// <summary>Pipe tiles as x0, y0, x1, y1, ...</summary>
    public Task<CommandResultDto> PlacePipes(int[] tiles, int tier) =>
        Command((world, company) => world.PlacePipes(company, Tiles(tiles), tier));

    public Task<CommandResultDto> UpgradePipes(int[] tiles, int tier) =>
        Command((world, company) => world.UpgradePipes(company, Tiles(tiles), tier));

    /// <summary>What your gold can do: the shared goal, a want, and "Grow everything that pays" (after run 4)</summary>
    public Task<GoldUsesDto?> GetGoldUses()
    {
        var userId = UserId;
        return _world.RunAsync(world => world.CompanyOfUser(userId) is { } company ? GameViews.GoldUses(world, company) : null, changesWorld: false);
    }

    public Task<CommandResultDto> GrowEverything() => Command((world, company) => world.GrowEverything(company));

    public Task<CommandResultDto> UndoGrowth() => Command((world, company) => world.UndoGrowth(company));

    /// <summary>Goes through a building's pipe door (order: order the goods it lacks now, even when a friend makes them)</summary>
    public Task<CommandResultDto> ClimbPipe(long buildingId, bool order) =>
        Command((world, company) => world.ClimbPipe(company, buildingId, order));

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

    public Task<CommandResultDto> Terraform(int x, int y, TerraformAction action, string? resource) =>
        Command((world, company) => world.Terraform(company, new TilePos(x, y), action, resource));

    public Task<CommandResultDto> Repair(long building) =>
        Command((world, company) => world.Repair(company, building));

    public Task<CommandResultDto> RepairAll() =>
        Command((world, company) => world.RepairAll(company));

    public Task<CommandResultDto> Demolish(long building) =>
        Command((world, company) => world.Demolish(company, building));

    public Task<CommandResultDto> Move(long building, int x, int y) =>
        Command((world, company) => world.Move(company, building, new TilePos(x, y)));

    public Task<CommandResultDto> BuyParcel(int parcel, int estate) =>
        Command((world, company) => world.BuyParcel(company, parcel, estate));

    public Task<CommandResultDto> SetImport(long building, string resource, bool enabled) =>
        Command((world, company) => world.SetImport(company, building, resource, enabled));

    public Task<CommandResultDto> SetBuildingSetting(long building, double value) =>
        Command((world, company) => world.SetBuildingSetting(company, building, value));



    public Task<CommandResultDto> SetValveFilter(long building, string resource, bool enabled) =>
        Command((world, company) => world.SetValveFilter(company, building, resource, enabled));
}
