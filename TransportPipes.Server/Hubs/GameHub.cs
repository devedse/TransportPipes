using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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
    Task MarketChanged(int districtX, int districtY);
    Task ExchangeChanged();
    Task ChatMessage(ChatMessageDto message);
    Task InboxChanged(int unread);
    Task CorporationChanged();
    Task WondersChanged();
    Task SeasonChanged();
    Task CrewChanged();

    /// <summary>Your links changed (an offer, an answer, a notice, the end)</summary>
    Task LinksChanged();

    /// <summary>A link somewhere was laid, rerouted or removed: the map draws them again</summary>
    Task MapLinksChanged();
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
    private readonly TransportPipesOptions _options;

    public GameHub(WorldHost world, ConnectedUsers connectedUsers, SocialService social, AccountService accounts, Microsoft.Extensions.Options.IOptions<TransportPipesOptions> options)
    {
        _world = world;
        _connectedUsers = connectedUsers;
        _social = social;
        _accounts = accounts;
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
        // count as one device and trade at the hub price (run 2 did, unseen; TransportPipes:Playtest only)
        if (_options.Playtest && http?.Request.Headers["X-Playtest-Ip"].ToString() is { Length: > 0 } playtestIp)
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
                return (Hello: new HelloDto(user, null, null, world.Now, world.Era, 0), Away: (GameViews.AwayFacts?)null, Company: (Company?)null, Crew: false);
            }
            // The return card (F5): what happened since the last session (from before the company wakes and its next report starts)
            var away = new GameViews.AwayFacts(world.Now - company.LastSeenAt, company.Report, company.StandIn?.Name, company.Asleep, company.Asleep && company.Packed != null);
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
            // Back from sleep (in place, or from the Quiet Isles)
            world.WakeUp(company);
            world.EnsureOrders(company);
            return (Hello: new HelloDto(user, CompanyView(world, company), null, world.Now, world.Era, 0, leftAt), Away: hasCard ? away : null, Company: company, Crew: world.CrewOf(company) != null);
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
        GameViews.Company(world, company, world.GoldPerMinute().GetValueOrDefault(company.Id), world.ResearchPerMinute().GetValueOrDefault(company.Id));

    /// <summary>
    /// A new company on a seat the player chose (or the one the game suggests: next to active players), or on the seat
    /// a friend's code holds for it (it joins the friend's crew)
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
            if (seat is { } chosen && code == null && !world.SeatFree(chosen))
            {
                return CommandResultDto.Fail("Someone took that seat a moment ago: here's the next free one.", "seatTaken");
            }
            seat ??= code == null ? world.SuggestSeat(Placement.Neighbours) : null;
            var created = world.CreateCompany(userId, name, color, GameWorld.LegacyLevelOf(legacy), titles, seat, code);
            created.IsGuest = guest;
            world.CountPlayDay(created);
            world.EnsureOrders(created);
            return CommandResultDto.Ok;
        });
    }

    /// <summary>Start with friends: a company on the seat the player picked and a crew, with the station's other seats held for friends</summary>
    public async Task<FoundCrewResultDto> StartWithFriends(string name, string color, string crewName, string emblem, string crewColor, string crewColor2, int districtX, int districtY, int slot, int holdSeats)
    {
        var created = await CreateCompany(name, color, districtX, districtY, slot, null);
        if (!created.Success)
        {
            return new FoundCrewResultDto(false, created.Error, [], created.Code);
        }
        return await FoundCrew(crewName, emblem, crewColor, crewColor2, holdSeats);
    }

    /// <summary>The stations of a district for the picker (the suggested one when none is given)</summary>
    public Task<StationsDto> GetStations(int? districtX, int? districtY) =>
        _world.RunAsync(world =>
        {
            var company = world.CompanyOfUser(UserId);
            return GameViews.Stations(world, company, districtX is { } x && districtY is { } y ? new DistrictCoord(x, y) : null);
        });

    public Task<CrewViewDto?> GetCrew() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Crew(world, company) : null, changesWorld: false);

    public Task<FoundCrewResultDto> FoundCrew(string name, string emblem, string color, string color2, int holdSeats) =>
        _world.RunAsync(world =>
        {
            if (world.CompanyOfUser(UserId) is not { } company)
            {
                return new FoundCrewResultDto(false, "Found your company first.", []);
            }
            var (result, crew) = world.FoundCrew(company.Id, name, emblem, color, color2, holdSeats);
            return new FoundCrewResultDto(result.Success, result.Error, crew?.Holds.Select(t => GameViews.Held(world, t)).ToList() ?? []);
        });

    public Task<CommandResultDto> JoinCrew(string code) =>
        Command((world, company) => world.JoinCrew(company, code));

    public Task<CommandResultDto> LeaveCrew() =>
        Command((world, company) => world.LeaveCrew(company));

    public Task<CommandResultDto> AcceptJoinRequest(long applicant) =>
        Command((world, company) => world.AcceptJoinRequest(company, applicant));

    public Task<CommandResultDto> RenewInvite() =>
        Command((world, company) => world.RenewInvite(company));

    public Task<CommandResultDto> HoldSeats(int count) =>
        Command((world, company) => world.HoldSeatsFor(company, count));

    public Task<CommandResultDto> ReleaseHold(string code) =>
        Command((world, company) => world.ReleaseHold(company, code));

    public Task<CommandResultDto> NameHold(string code, string? name) =>
        Command((world, company) => world.NameHold(company, code, name));

    /// <summary>Takes a building's last purchase back within the hour (after run 3: spending is safe)</summary>
    public Task<CommandResultDto> Undo(long buildingId) =>
        Command((world, company) => world.Undo(company, buildingId));

    /// <summary>A friend's seat link opened while playing elsewhere: kept in front of the player until they answer (decline: "Not with them")</summary>
    public Task<CommandResultDto> SawSeatCode(string code, bool decline) =>
        Command((world, company) => world.SawSeatCode(company, code, decline));

    public Task<CommandResultDto> SetStationOpen(bool open) =>
        Command((world, company) => world.SetStationOpen(company, open));

    public Task<CommandResultDto> RelocateTo(int districtX, int districtY, int slot) =>
        Command((world, company) => world.RelocateTo(company, new EstateKey(new DistrictCoord(districtX, districtY), slot)));

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
            return new SelectionCostsDto(
                Math.Round(own.Sum(t => world.UpgradeCost(t, 1)), 2),
                Math.Round(own.Sum(t => world.UpgradeCost(t, Math.Min(10, max - t.Level))), 2),
                own.Count == 0 ? 0 : Math.Round(own.Min(t => world.UpgradeCost(t, 1)), 2),
                own.Count,
                makes);
        }, changesWorld: false);
    }

    /// <summary>The advisor's three best next steps for the player's company</summary>
    public Task<List<NextStepDto>> GetNextSteps()
    {
        var userId = UserId;
        return _world.RunAsync(world => world.CompanyOfUser(userId) is { } company
            ? world.NextSteps(company).Select(t => GameViews.NextStep(t, world)).ToList()
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

    /// <summary>A Send (step 4): a good's spare goes to a neighbour (to: a company) or the station's project (to: null) first</summary>
    public Task<CommandResultDto> SendGood(string resource, long? to, long? building) =>
        Command((world, company) => world.SendGood(company, resource, to, building));

    public Task<CommandResultDto> StopSend(string resource, long? to) =>
        Command((world, company) => world.StopSend(company, resource, to));

    public Task<CommandResultDto> Thank(long giver, string resource) =>
        Command((world, company) => world.Thank(company, giver, resource));

    public Task<IReadOnlyList<ResearchStateDto>> GetResearch() =>
        _world.RunAsync<IReadOnlyList<ResearchStateDto>>(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Research(world, company) : [], changesWorld: false);

    public Task<CommandResultDto> StartResearch(string tech) =>
        Command((world, company) => world.StartResearch(company, tech));

    /// <summary>The research queue (F11): up to five techs that start by themselves, in order</summary>
    public Task<CommandResultDto> QueueResearch(string tech, bool queued) =>
        Command((world, company) => world.QueueResearch(company, tech, queued));

    public Task<CommandResultDto> SetKeepResearching(bool keep) =>
        Command((world, company) => world.SetKeepResearching(company, keep));

    public Task<MarketDto?> GetMarket() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Market(world, company) : null, changesWorld: false);

    public Task<IReadOnlyList<LandingCardDto>> GetLivelierStations() =>
        _world.RunAsync<IReadOnlyList<LandingCardDto>>(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Livelier(world, company) : [], changesWorld: false);

    public Task<CommandResultDto> DismissLoneCard() =>
        Command((world, company) => world.DismissLoneCard(company));

    /// <summary>Moves the crew together: you to the suggested station, a seat there kept for everyone else for a week</summary>
    public Task<CommandResultDto> CrewMove() =>
        Command((world, company) => world.Company(company) is { } me ? world.CrewMove(company, world.CrewMoveSeat(me)) : CommandResult.Fail("Unknown company."));

    public Task<CommandResultDto> Consolidate() =>
        Command((world, company) => world.Consolidate(company));

    public Task<CommandResultDto> DismissConsolidation() =>
        Command((world, company) => world.DismissConsolidation(company));

    public Task<CommandResultDto> SetPin(string resource, bool need, bool on) =>
        Command((world, company) => world.SetPin(company, resource, need, on));

    public Task<CommandResultDto> Ask(string resource, double amount, double hours, bool renew) =>
        Command((world, company) => world.Ask(company, resource, amount, hours, renew));

    public Task<CommandResultDto> CancelAsk(string resource) =>
        Command((world, company) => world.CancelAsk(company, resource));

    public Task<ProjectDto?> GetProject() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Project(world, company) : null, changesWorld: false);

    public Task<CommandResultDto> SetProjectFirst(string resource, bool first) =>
        Command((world, company) => world.SetProjectFirst(company, resource, first));

    public Task<CommandResultDto> Tend(long friendId) =>
        Command((world, company) => world.Tend(company, friendId));

    public Task<CommandResultDto> KeepSeat(long sleeperId, bool keep) =>
        Command((world, company) => world.KeepSeat(company, sleeperId, keep));

    /// <summary>Where you could land now (the third screen of the way back; a move there is free once)</summary>
    public Task<IReadOnlyList<LandingCardDto>> GetLandingCards() =>
        _world.RunAsync<IReadOnlyList<LandingCardDto>>(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Landing(world, company) : [], changesWorld: false);

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

    public Task<CommandResultDto> StartTrunk(int? districtX, int? districtY, bool port) =>
        Command((world, company) => world.StartTrunk(company, districtX is { } x && districtY is { } y ? new DistrictCoord(x, y) : null, port).Result);

    public Task<CommandResultDto> BuildTrunkSegment(long trunkId) =>
        Command((world, company) => world.BuildTrunkSegment(company, trunkId));

    public Task<IReadOnlyList<MapLinkDto>> GetMapLinks() =>
        _world.RunAsync(GameViews.MapLinks, changesWorld: false);

    public Task<IReadOnlyList<NeighbourDto>> GetNeighbourhood() =>
        _world.RunAsync<IReadOnlyList<NeighbourDto>>(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Neighbourhood(world, company) : [], changesWorld: false);

    public Task<ContractsDto?> GetContracts() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Contracts(world, company) : null, changesWorld: false);

    /// <summary>partner: a company id, or null for the contract board</summary>
    public Task<CommandResultDto> ProposeContract(long? partner, bool selling, string resource, double rate, double price, double days) =>
        Command((world, company) => world.ProposeContract(company, partner, selling, resource, rate, price, days));

    public Task<CommandResultDto> AcceptContract(long contract) =>
        Command((world, company) => world.AcceptContract(company, contract));

    public Task<CommandResultDto> DeclineContract(long contract) =>
        Command((world, company) => world.DeclineContract(company, contract));

    public Task<ExchangeDto?> GetExchange() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Exchange(world, company) : null, changesWorld: false);

    public Task<CommandResultDto> PlaceExchangeOrder(string resource, bool selling, double amount, double price) =>
        Command((world, company) => world.PlaceExchangeOrder(company, resource, selling, amount, price));

    public Task<CommandResultDto> CancelExchangeOrder(long order) =>
        Command((world, company) => world.CancelExchangeOrder(company, order));

    public Task<CommandResultDto> DeliverOrder(int order) =>
        Command((world, company) => world.DeliverOrder(company, order));

    public Task<CommandResultDto> SetNoTrade(string resource, bool noTrade) =>
        Command((world, company) => world.SetNoTrade(company, resource, noTrade));

    public Task<CommandResultDto> SetTradeBlocked(long other, bool blocked) =>
        Command((world, company) => world.SetTradeBlocked(company, other, blocked));

    public Task<CommandResultDto> SetTradeWith(string tradeWith) =>
        Enum.TryParse<TradeWith>(tradeWith, ignoreCase: true, out var value)
            ? Command((world, company) => world.SetTradeWith(company, value))
            : Task.FromResult(CommandResultDto.Fail("Unknown choice."));

    public Task<CommandResultDto> SetNoBuy(string resource, bool noBuy) =>
        Command((world, company) => world.SetNoBuy(company, resource, noBuy));

    // ---------------------------------------------------------------- Corporations

    public Task<CorporationsDto?> GetCorporations()
    {
        var online = _connectedUsers.OnlineUsers.ToHashSet();
        return _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Corporations(world, company, online) : null, changesWorld: false);
    }

    public Task<CommandResultDto> FoundCorporation(string name, string tag, string color) =>
        IsColor(color)
            ? Command((world, company) => world.FoundCorporation(company, name, tag, color))
            : Task.FromResult(CommandResultDto.Fail("Pick a colour."));

    public Task<CommandResultDto> ApplyToCorporation(long corporation) =>
        Command((world, company) => world.ApplyToCorporation(company, corporation));

    /// <summary>An officer accepts an application or invites a company</summary>
    public Task<CommandResultDto> InviteToCorporation(long company) =>
        Command((world, officer) => world.InviteToCorporation(officer, company));

    /// <summary>A company declines an invite, or an officer declines an application (corporation = your own, company = them)</summary>
    public Task<CommandResultDto> DeclineCorporation(long corporation) =>
        Command((world, company) => world.DeclineCorporation(company, corporation));

    public Task<CommandResultDto> DeclineApplication(long applicant) =>
        Command((world, officer) => world.DeclineApplication(officer, applicant));

    public Task<CommandResultDto> LeaveCorporation() =>
        Command((world, company) => world.LeaveCorporation(company));

    public Task<CommandResultDto> KickFromCorporation(long member) =>
        Command((world, officer) => world.KickFromCorporation(officer, member));

    public Task<CommandResultDto> SetCorporationRole(long member, CorporationRole role) =>
        Command((world, leader) => world.SetCorporationRole(leader, member, role));

    public Task<CommandResultDto> DonateToCorporation(double gold) =>
        Command((world, company) => world.DonateToCorporation(company, gold));

    public Task<CommandResultDto> SetCorporationSettings(double tax, string? perkResource, string description) =>
        Command((world, officer) => world.SetCorporationSettings(officer, tax, perkResource, description));

    public Task<CommandResultDto> UpgradeCorporationHq() =>
        Command((world, officer) => world.UpgradeCorporationHq(officer));

    public Task<CommandResultDto> DeliverToCorporationGoal() =>
        Command((world, company) => world.DeliverToCorporationGoal(company));

    // ---------------------------------------------------------------- Company: managers and achievements

    public Task<CompanyDetailsDto?> GetCompanyDetails() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.CompanyDetails(world, company) : null, changesWorld: false);

    public Task<CommandResultDto> HireManager(string manager) =>
        Command((world, company) => world.HireManager(company, manager));

    public Task<CommandResultDto> DismissManager(string manager) =>
        Command((world, company) => world.DismissManager(company, manager));

    // ---------------------------------------------------------------- Franchise and season

    /// <summary>Opens a franchise: a new estate, in the given district or (district null) the frontier</summary>
    public Task<CommandResultDto> OpenFranchise(int? districtX, int? districtY) =>
        Command((world, company) => world.OpenFranchise(company, districtX is { } x && districtY is { } y ? new DistrictCoord(x, y) : null));

    public Task<SeasonDto> GetSeason()
    {
        var userId = UserId;
        return _world.RunAsync(world => GameViews.Season(world, userId), changesWorld: false);
    }

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
                var players = world.Companies.Where(t => !t.IsNpc && !t.Asleep && t.District == district).ToList();
                var free = estates.Count(t => (t.OwnerId == null || world.Company(t.OwnerId.Value) is { IsNpc: true })
                    && (t.ReservedFor == null || t.ReservedUntil <= world.Now || t.ReservedFor == me.CorporationId));
                return new DistrictChoiceDto(
                    district.X,
                    district.Y,
                    WorldNames.District(district),
                    free,
                    players.Count,
                    players.Count == 0 ? 0 : players.Count(t => world.ActivityOf(t) == Activity.Active) / (double)players.Count,
                    estates.Any(t => t.ReservedFor != null && t.ReservedFor == me.CorporationId && t.ReservedUntil > world.Now),
                    district == me.District);
            }).ToList();
        }, changesWorld: false);

    public Task<CommandResultDto> PlaceCrate(int crate, int x, int y) =>
        Command((world, company) => world.PlaceCrate(company, crate, new TilePos(x, y)));

    public Task<CommandResultDto> ScrapCrate(int crate) =>
        Command((world, company) => world.ScrapCrate(company, crate));

    public Task<CommandResultDto> ProposeSwap(long other) =>
        Command((world, company) => world.ProposeSwap(company, other));

    public Task<CommandResultDto> ReserveBlock(int x, int y) =>
        Command((world, company) => world.ReserveBlock(company, new DistrictCoord(x, y)));

    public Task<IReadOnlyList<SleeperDto>> GetQuietIsles() =>
        _world.RunAsync<IReadOnlyList<SleeperDto>>(world =>
            world.QuietIsles.OrderByDescending(t => t.Level).Take(100).Select(t => new SleeperDto(t.Id, t.Name, t.Color, t.Level, t.LastSeenAt)).ToList(),
            changesWorld: false);

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

    // ---------------------------------------------------------------- Wonders

    public Task<WondersDto?> GetWonders() =>
        _world.RunAsync(world => world.CompanyOfUser(UserId) is { } company ? GameViews.Wonders(world, company) : null, changesWorld: false);

    public Task<CommandResultDto> DeliverToWonder(bool eraWonder) =>
        Command((world, company) => world.DeliverToWonder(company, eraWonder));

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
            world.Company(owner) is { } company && world.Company(viewerId) is { } me ? GameViews.Visit(world, company, me, guestbook) : null, changesWorld: false);
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
            if (world.Company(owner) is not { IsNpc: false } ownerCompany || ownerCompany.Id == company.Id)
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
            var recipients = world.Companies.Where(t => t.District == company.District && !t.IsNpc).Select(t => t.UserId).ToList();
            return (true, WorldNames.District(company.District), recipients);
        }
        if (world.CorporationOf(company) is { } corporation && channel == SocialService.CorporationChannel(corporation.Id))
        {
            return (true, corporation.Name, world.Members(corporation).Select(t => t.UserId).ToList());
        }
        if (SocialService.DirectPair(channel) is { } pair && (pair.A == company.Id || pair.B == company.Id)
            && world.Company(pair.A == company.Id ? pair.B : pair.A) is { IsNpc: false } other)
        {
            return (true, other.Name, [company.UserId, other.UserId]);
        }
        return (false, "", []);
    }

    public async Task<ChatDto?> GetChat()
    {
        var userId = UserId;
        var me = await _world.RunAsync(world => world.CompanyOfUser(userId) is { } company
            ? (company.Id, company.District, world.CorporationOf(company) is { } c ? (c.Id, c.Name) : default((long Id, string Name)?))
            : default((long, DistrictCoord, (long Id, string Name)?)?), changesWorld: false);
        if (me is not { } info)
        {
            return null;
        }
        var (companyId, district, corporation) = info;
        var channels = new List<ChatChannelDto>();
        var districtChannel = SocialService.DistrictChannel(district.X, district.Y);
        channels.Add(new ChatChannelDto(districtChannel, WorldNames.District(district), "district", await _social.LastMessageAtAsync(districtChannel)));
        if (corporation is { } corp)
        {
            var corporationChannel = SocialService.CorporationChannel(corp.Id);
            channels.Add(new ChatChannelDto(corporationChannel, corp.Name, "corporation", await _social.LastMessageAtAsync(corporationChannel)));
        }
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
        var message = await _social.SendAsync(channel, sender.Id, sender.Name, text);
        await Clients.Groups(recipients.Distinct().Select(UserGroup).ToList()).ChatMessage(message);
        return CommandResultDto.Ok;
    }

    public async Task<CommandResultDto> ReportChat(long message)
    {
        var userId = UserId;
        // The world may only be read inside RunAsync: collect the channels the reporter may read first
        var me = await _world.RunAsync(world => world.CompanyOfUser(userId) is { } company
            ? (company.Id, SocialService.DistrictChannel(company.District.X, company.District.Y), company.CorporationId is { } corporationId ? SocialService.CorporationChannel(corporationId) : null)
            : default((long Id, string District, string? Corporation)?), changesWorld: false);
        if (me is not { } reporter)
        {
            return CommandResultDto.Fail("Found your company first.");
        }
        var reported = await _social.ReportAsync(message, channel =>
            channel == reporter.District || channel == reporter.Corporation || SocialService.DirectPair(channel) is { } pair && (pair.A == reporter.Id || pair.B == reporter.Id));
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
                    // A million earned (for the Franchise)
                    company.Gold += 1_000_000;
                    company.LifetimeEarnings += 1_000_000;
                    break;
                case "level":
                    world.GainXp(company, world.Content.Progression.XpForLevel(company.Level + 5) - company.Xp, "cheats", "level");
                    break;
                case "research":
                    company.ResearchPoints += 5_000;
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
                    world.AdvanceEra();
                    break;
                case "alltech":
                    world.GrantAllResearch(company);
                    break;
                case "district":
                    world.OpenNextDistrict();
                    break;
                case "arrive":
                    world.LandNow(company);
                    break;
                case "sleep":
                    world.PutToSleep(company);
                    break;
                case "gone":
                    // The next Hello comes as if the player was away for ten days (the way back's screens)
                    company.LastSeenAt = world.Now.AddDays(-10);
                    company.Report.Since = company.LastSeenAt;
                    company.Touch();
                    break;
                case "restage":
                    // Every project's stage starts again by today's rules
                    world.RestageProjects();
                    break;
                case "project":
                    // The station's project: its stage is delivered now
                    if (world.EstateOf(company) is { } home)
                    {
                        world.FinishStageNow(home.Station);
                    }
                    break;
                case "isles":
                    // Asleep, and the hold over: the estate waits on the Quiet Isles
                    world.PutToSleep(company);
                    world.PackForTesting(company);
                    company.LastSeenAt = world.Now.AddDays(-40);
                    company.Report.Since = company.LastSeenAt;
                    break;
                case "finale":
                    // To Era 5 and the launch countdown
                    while (world.Era < GameWorld.LastEra)
                    {
                        world.AdvanceEra();
                    }
                    world.StartFinale();
                    break;
                case "endseason":
                    world.StartFinale();
                    world.EndSeason();
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
                    foreach (var definition in world.Content.ResourceList.Where(t => t.Era <= world.Era && t.Id != "energy"))
                    {
                        store.Contents[definition.Id] = store.Contents.GetValueOrDefault(definition.Id) + 1000;
                    }
                    store.Touch();
                    break;
                case "wonder":
                    // Fills your storages with what the next stages need (an hour of a big district's work)
                    var silo = world.BuildingsOf(companyId).FirstOrDefault(t => world.Content.Building(t.Type).Kind == TransportPipes.Game.Content.BuildingKind.Storage);
                    if (silo == null || world.DistrictWonder(company.District) is not { } districtWonder)
                    {
                        return CommandResult.Fail("Build a storage first.");
                    }
                    foreach (var (need, required) in districtWonder.Required)
                    {
                        silo.Contents[need] = silo.Contents.GetValueOrDefault(need) + required;
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

    public Task<CommandResultDto> SetShowroomProduct(long building, string resource) =>
        Command((world, company) => world.SetShowroomProduct(company, building, resource));

    public Task<CommandResultDto> SetValveFilter(long building, string resource, bool enabled) =>
        Command((world, company) => world.SetValveFilter(company, building, resource, enabled));
}
