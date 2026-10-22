namespace TransportPipes.Server.Dtos;

// The contract with the frontend (TransportPipes.Frontend/src/api/contracts.ts). Serialized as camelCase JSON, enums as
// camelCase strings.

public record VersionInfo(string Version, string? Commit);

public record UserInfo(long Id, string UserName, bool IsAdmin = false, bool IsGuest = false);

public record LoginRequest(string UserName, string Password);

public record RegisterRequest(string UserName, string Password, string? Email);

public record AuthResult(bool Success, string? Error, UserInfo? User);

/// <summary>A guest's account: the browser keeps its name and password, so the estate isn't lost with the cookie</summary>
public record GuestResult(bool Success, string? Error, UserInfo? User, string? Password);

/// <summary>A guest keeps the estate: a name and a password of its own</summary>
public record KeepRequest(string UserName, string Password, string? Email);

public record CommandResultDto(bool Success, string? Error)
{
    public static CommandResultDto Ok { get; } = new(true, null);

    public static CommandResultDto Fail(string error) => new(false, error);
}

/// <summary>Everything the client needs right after connecting</summary>
public record HelloDto(UserInfo User, CompanyDto? Company, ReportDto? Report, DateTime ServerTime, int Era, int Unread, ReturnDto? Return = null);

/// <summary>
/// The way back after three or more days (v1.2 §10): what happened, that the estate is safe, and where it can land
/// </summary>
public record ReturnDto(
    double DaysAway,
    double Earned,
    double CaretakerFee,
    int TendedDays,
    string? StandIn,
    bool WasAsleep,
    bool FromQuietIsles,
    string Station,
    int Links,
    int Crates,
    IReadOnlyList<LandingCardDto> Landing);

/// <summary>A place to land: your seat, next to your crew, a lively station, the Frontier; with the crates it would mean</summary>
public record LandingCardDto(string Kind, SeatRefDto Seat, string Station, int Crates, bool Here, int Neighbours);

public record CompanyDto(
    long Id,
    string Name,
    string Color,
    double Gold,
    double GoldPerMinute,
    double Xp,
    int Level,
    double LevelXp,
    double NextLevelXp,
    double ResearchPoints,
    double ResearchPerMinute,
    double LifetimeEarnings,
    int DistrictX,
    int DistrictY,
    int Slot,
    int EstateX,
    int EstateY,
    int UnlockedParcels,
    double? NextParcelCost,
    int? NextParcelLevel,
    IReadOnlyDictionary<string, int> BuildingCounts,
    IReadOnlyList<string> Researched,
    string? CurrentResearch,
    DateTime? ResearchEndsAt,
    double HubReputation,
    int ReputationLevel,
    double? NextReputation,
    IReadOnlyList<DailyOrderDto> DailyOrders,
    IReadOnlyList<string> NoTrade,
    long? CorporationId,
    string? CorporationTag,
    string? CorporationRole,
    long? MentorId,
    int Likes,
    IReadOnlyList<GameEventDto> Events,
    int ForecastHours,
    int Era,
    DateTime EraDeadline,
    IReadOnlyList<string> Titles,
    IReadOnlyList<string> Collections,
    IReadOnlyList<OutpostDto> Outposts,
    FranchiseDto Franchise,
    SeasonInfoDto Season,
    MovingDto Moving,
    DeepDto Deep,
    IReadOnlyList<BranchDto> Branches,
    TutorialDto? Tutorial,
    ExpeditionDto Expedition,
    MasteryDto Mastery,
    IReadOnlyList<AlertDto> Alerts,
    DateTime ServerTime,
    ExperienceDto Experience,
    string TradeWith,
    CrewBadgeDto? Crew,
    /// <summary>The XP multiplier while far behind the people you live with (v1.2 §10)</summary>
    double Tailwind,
    /// <summary>The lone-human card waits: nobody else in the station played lately</summary>
    bool LoneCard,
    /// <summary>The league it plays in this week (null: none)</summary>
    int? League);

/// <summary>A company's crew as the top bar and the map show it</summary>
public record CrewBadgeDto(long Id, string Name, string Emblem, string Color, string Color2, int Members);

/// <summary>
/// Experience: XP per minute from what the company earns now, where its XP came from (per source per item) and what
/// gives XP
/// </summary>
public record ExperienceDto(double PerMinute, IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> Log, IReadOnlyList<XpRuleDto> Rules);

public record XpRuleDto(string Source, string Text);

/// <summary>Something that holds the company back, with the building to look at</summary>
public record AlertDto(string Text, long? Building);

/// <summary>Mastery research after the whole tree: the level reached and the next step</summary>
public record MasteryDto(int Level, bool Available, double Cost, double Minutes, double Bonus);

/// <summary>Expeditions (GDD §11.3): the one out, what they cost, what came back, the relics found</summary>
public record ExpeditionDto(int? Hours, DateTime? EndsAt, IReadOnlyDictionary<int, double> Costs, string? Blocker, IReadOnlyList<string> Log, IReadOnlyList<string> Relics);

/// <summary>Loady's guide: the step the company is on</summary>
/// <summary>
/// Loady's current step; WaitLevel: the Company Level it needs first (null when it can be done now); Tip: why it isn't
/// done yet although the player did what it says
/// </summary>
/// <summary>Loady's current step; Chapter, ChapterStep (1-based) and ChapterSteps place it in its chapter</summary>
public record TutorialDto(int Step, int Steps, string Id, string Text, double Progress, double Amount, double Gold, IReadOnlyDictionary<string, string> Hint, int? WaitLevel, string? Tip, int Chapter, int ChapterStep, int ChapterSteps);

/// <summary>A branch estate opened by a franchise (its index for BuyParcel is its place in this list plus one)</summary>
public record BranchDto(int DistrictX, int DistrictY, string DistrictName, int Slot, int X, int Y, int UnlockedParcels, double NextParcelCost, int NextParcelLevel);

/// <summary>
/// The Deep Level (GDD §11.2): whether there is a lift, the dug out parcels, the parcel being dug and what the next one
/// costs. Deep tiles are surface tiles shifted down by DeepOffset.
/// </summary>
public record DeepDto(bool Open, int Parcels, int? DigParcel, DateTime? DigStartedAt, DateTime? DigEndsAt, double NextCost, double NextHours, int Offset);

/// <summary>Moving and activity (GDD §12): a move in progress, crates, free moves, a proposed swap</summary>
public record MovingDto(string Activity, bool Asleep, DateTime? ArrivesAt, IReadOnlyList<CrateDto> Crates, int FreeMoves, double MoveCost, DateTime? CanMoveAt, long? SwapWith);

public record CrateDto(int Index, string Type, int Level);

/// <summary>A district a company could move to</summary>
public record DistrictChoiceDto(int X, int Y, string Name, int FreeEstates, int Players, double ActiveShare, bool ReservedForYou, bool Yours);

public record SleeperDto(long Id, string Name, string Color, int Level, DateTime Since);

/// <summary>The Franchise (GDD §8.7): Shares so far and what a franchise would give now</summary>
public record FranchiseDto(int Shares, int Franchises, int SharesNow, string? Blocker, DateTime? At, int LegacyLevel);

/// <summary>The season: the finale countdown, and when it ended the next world's opening</summary>
public record SeasonInfoDto(int Season, DateTime? FinaleAt, DateTime? EndedAt, DateTime? NextWorldAt);

public record SeasonResultDto(string Company, double NetWorth, int Rank, int Legacy, string? Title, bool You);

public record SeasonDto(int Season, DateTime? FinaleAt, DateTime? EndedAt, DateTime? NextWorldAt, IReadOnlyList<SeasonResultDto> Top, SeasonResultDto? You);

/// <summary>An event in the calendar (GDD §15)</summary>
public record GameEventDto(long Id, string Kind, string Name, string Effect, DateTime StartsAt, DateTime EndsAt, string? Resource, bool Active, bool Everywhere);

public record DailyOrderDto(int Id, string Resource, double Amount, double Delivered, double RewardGold, double RewardXp, double RewardReputation, DateTime ExpiresAt, bool Urgent);

/// <summary>
/// One resource at the hub of the player's district: what the hub pays the player (its own Hub Order), what it charges,
/// what the player sold there lately against the order, the one price between players, and the player's trade settings
/// </summary>
public record MarketResourceDto(
    string Resource,
    double BasePrice,
    double BuyPrice,
    double SellPrice,
    double SoldPerMinute,
    double Demand,
    double PlayerPrice,
    bool NoTrade,
    bool NoBuy,
    double? Heading);

public record MarketDto(int DistrictX, int DistrictY, string District, IReadOnlyList<MarketResourceDto> Resources, DateTime NextUpdate);

/// <summary>A tech as one company sees it</summary>
public record ResearchStateDto(string Id, string State, double Cost, string? Reason, double Diffusion);

public record ReportDto(
    DateTime Since,
    double GoldEarned,
    double GoldSpent,
    IReadOnlyDictionary<string, double> Sold,
    IReadOnlyDictionary<string, double> Produced,
    int LevelsGained,
    int BuildingsCompleted,
    IReadOnlyList<string> Notes,
    IReadOnlyList<string> ResearchDone,
    IReadOnlyList<string> Contracts,
    IReadOnlyList<string> NewNeighbours,
    int OrdersCompleted,
    int LikesReceived,
    IReadOnlyList<FullStorageDto> FullStorages);

/// <summary>A full storage in the offline report, with a one-tap upgrade</summary>
public record FullStorageDto(long Building, string Type, int Level, double Capacity, double? UpgradeCost);

public record BuildingDto(long Id, string Type, long Owner, int X, int Y, int Size, int Level, DateTime? ConstructionEndsAt, int? Network, bool Damaged, long? Link = null);

public record ConductorDto(int X, int Y, string Kind, int Tier, long Owner, int? Network);

/// <summary>
/// A 32×32 area. Terrain is sent once (packed, one byte per tile, row by row), objects with every change.
/// </summary>
public record ChunkDto(int Cx, int Cy, string? Terrain, string? Deposits, string? Richness, string? Wind, string? Sun, IReadOnlyList<string> DepositNames, ChunkObjectsDto Objects);

public record ChunkObjectsDto(
    int Cx,
    int Cy,
    IReadOnlyList<BuildingDto> Buildings,
    IReadOnlyList<ConductorDto> Conductors,
    IReadOnlyList<TerrainOverrideDto> TerrainOverrides,
    IReadOnlyList<OutpostDto> Outposts,
    IReadOnlyList<DepositOverrideDto> Deposits);

/// <summary>An outpost claim (lease details only for its owner)</summary>
public record OutpostDto(long Id, long Owner, string OwnerName, string OwnerColor, int X, int Y, int Size, double? Lease, DateTime? PaidUntil);

/// <summary>A deposit that differs from the generated terrain (a Discovery)</summary>
public record DepositOverrideDto(int X, int Y, string Deposit, int Richness);

public record OutpostCheckDto(string? Error, int X, int Y, double Lease, int Size, string? Deposit, int RichTiles);

public record TerrainOverrideDto(int X, int Y, string Terrain);

public record EstateDto(int Slot, int X, int Y, long? OwnerId, string? OwnerName, string? OwnerColor, int OwnerLevel, int UnlockedParcels, string Primary, string? Secondary, string? CorporationTag, string? Activity, string? ReservedFor);

/// <summary>A hub structure; corporation HQs have the corporation's tag, colour and HQ level</summary>
public record HubStructureDto(string Type, int X, int Y, int Size, string? Label = null, string? Color = null, int Level = 0, long? Corporation = null, string? Variant = null, int Stages = 0);

public record DistrictDto(int X, int Y, int OriginX, int OriginY, string Name, IReadOnlyList<EstateDto> Estates, IReadOnlyList<HubStructureDto> Hub);

public record FlowDto(long From, long To, string Resource, double Sent, double Delivered, string Kind, double Price, IReadOnlyList<int> Path);

public record NetworkFlowsDto(int Id, IReadOnlyDictionary<long, double> Rates, IReadOnlyList<FlowDto> Flows);

public record ResourceAmountDto(string Resource, double Amount, double Max);

public record BuildingFlowDto(string Direction, string Resource, double Amount, long Partner, string PartnerType, string PartnerOwner, string Kind, double Price);

public record BuildingDetailsDto(
    BuildingDto Building,
    string OwnerName,
    double Rate,
    double LocationMultiplier,
    IReadOnlyList<ResourceAmountDto> Inputs,
    IReadOnlyList<ResourceAmountDto> Outputs,
    double ResearchPerMinute,
    IReadOnlyDictionary<string, double>? Contents,
    double? Capacity,
    double? Throughput,
    IReadOnlyList<string>? Imports,
    IReadOnlyList<string>? Filter,
    double PumpFactor,
    double? UpgradeCost,
    double? UpgradeCost10,
    int MaxAffordableLevels,
    double? UpgradeCostMax,
    IReadOnlyList<BuildingFlowDto> Flows,
    double DemolishRefund,
    IReadOnlyList<string> Issues,
    double Setting,
    double? RepairCost,
    int ComplexMembers,
    TeleporterDto? Teleporter = null,
    EvolveDto? Evolve = null,
    StorageKeepDto? Keep = null,
    MaxMoreDto? MaxMore = null,
    BuildingStatusDto? Status = null);

/// <summary>
/// A building's panel head (F2): why it doesn't run fully (Cause, from IdleReason), the status pill (the cause in five
/// words), the reach line, and the one fix button; null pill: it runs fine
/// </summary>
public record BuildingStatusDto(string Cause, string? Pill, string? Reach, BuildingFixDto? Fix);

/// <summary>
/// The one fix button: Kind layPipe (Tiles as x,y pairs, to show as a ghost and lay), open (Building), repair, upgrade
/// (Building by a level) or build (the build tool with BuildingType); Target names where a pipe goes
/// </summary>
public record BuildingFixDto(string Kind, string Label, int[] Tiles, int NewTiles, double Cost, long? Building, string? BuildingType, string? Target);

/// <summary>
/// Smart Max (F8): the Max button buys MaxAffordableLevels, the levels its pipes can feed; these are the levels the gold
/// could buy beyond them, how fast the building would run there, and what holds it back (an input: what its pipes bring
/// and what it would need; or its output)
/// </summary>
public record MaxMoreDto(int Levels, double Cost, double Share, string? Resource, double Have, double Need, bool Output);

/// <summary>
/// A storage: how much of each resource it keeps (its setting, or what the company needs), and whether a full trading
/// post of its network sends it the rest (then it holds more)
/// </summary>
public record StorageKeepDto(IReadOnlyDictionary<string, double> Keeps, bool TradingPostFull);

/// <summary>Upgrading several buildings: the levels bought and the gold spent (or why nothing happened)</summary>
public record BulkUpgradeDto(bool Success, string? Error, int Levels, double Gold);

/// <summary>A painted row or area: how many were built and what they cost; Error: why the rest wasn't</summary>
public record BulkPlaceDto(int Placed, double Gold, string? Error);

/// <summary>What upgrading several buildings costs: all by one level, all by ten, and the cheapest next level</summary>
/// <summary>Upgrading a selection: +1 each, +10 each, the cheapest next level; Makes: what the buildings make together now, per minute</summary>
public record SelectionCostsDto(double PlusOne, double PlusTen, double Cheapest, int Upgradable, Dictionary<string, double> Makes);

/// <summary>One of the advisor's next steps (GameWorld.NextSteps): x, y is where Show me looks</summary>
public record NextStepDto(
    string Kind,
    string Title,
    string Reason,
    string Action,
    double Cost,
    double GainPerMinute,
    double? PaybackMinutes,
    double? WaitMinutes,
    long? Building,
    string? BuildingType,
    int Levels,
    int? Parcel,
    long? Partner,
    int? X,
    int? Y,
    string? Research,
    int Count,
    int? Network,
    int[] Tiles,
    int Tier);

/// <summary>What a building can evolve into where it stands, what it costs and what is in the way</summary>
public record EvolveDto(string Into, double Cost, string? Problem);

/// <summary>A teleporter station (GDD §11.4): what it sends or receives, and the stations it could link to</summary>
public record TeleporterDto(
    string? Role,
    long? Partner,
    string? Resource,
    double Distance,
    double PerMinute,
    double Throughput,
    double EnergyPerUnit,
    double Loss,
    string Range,
    IReadOnlyList<TeleporterCandidateDto> Candidates);

public record TeleporterCandidateDto(long Id, int X, int Y, double Distance, string? Problem);

public record ToastDto(string Kind, string Text, long? Building);

/// <summary>A resource in a network per minute; Needed: what its buildings would use at full speed</summary>
public record NetworkResourceDto(string Resource, double Produced, double Used, double Sold, double Imported, double Traded, double Stored, double FromStorage, double Lost, double Needed);

/// <summary>What one company's buildings in a network make, use and sell of a resource, per minute</summary>
public record OwnerResourceDto(string Resource, double Made, double Used, double Sold);

/// <summary>
/// A company in a network: its gold per minute and where it comes from (sales to the hub and to neighbours) and goes
/// (bought from neighbours, imported from the hub)
/// </summary>
public record NetworkOwnerDto(long Id, string Name, string Color, double GoldPerMinute, IReadOnlyList<OwnerResourceDto> Resources,
    double HubSales, double NeighbourSales, double NeighbourPurchases, double HubPurchases);

public record BottleneckDto(long A, long B, double Capacity, double Used, IReadOnlyList<int> Path);

public record NetworkDetailsDto(
    int Id,
    int Buildings,
    int Pipes,
    int Pumps,
    double PumpFactor,
    IReadOnlyList<NetworkOwnerDto> Owners,
    IReadOnlyList<NetworkResourceDto> Resources,
    IReadOnlyList<BottleneckDto> Bottlenecks,
    IReadOnlyList<StuckBuildingDto>? Stuck = null);

/// <summary>A building of the network whose goods can't leave (F2): the network card names it first</summary>
public record StuckBuildingDto(long Building, string Type, string Line);

public record ResourceRateDto(string Resource, double PerMinute);

public record NeighbourDto(
    long Id,
    string Name,
    string Color,
    int Level,
    int ContractStars,
    bool IsNpc,
    int Slot,
    int EstateX,
    int EstateY,
    bool Connected,
    IReadOnlyList<ResourceRateDto> Surplus,
    IReadOnlyList<ResourceRateDto> Needs,
    /// <summary>What each building that misses something misses (it buys only when all of it comes)</summary>
    IReadOnlyList<IReadOnlyList<string>> Shortfalls,
    double YouEarn,
    double YouPay,
    /// <summary>The best pipe from your buildings to theirs: how much of what is sent arrives, and how many tiles long</summary>
    double? LinkEfficiency,
    int? PipeTiles,
    /// <summary>You don't trade with them (trade settings)</summary>
    bool Blocked);

public record ContractDto(
    long Id,
    long Proposer,
    string ProposerName,
    long Seller,
    string SellerName,
    long Buyer,
    string BuyerName,
    string Resource,
    double Rate,
    double Price,
    double Days,
    string State,
    DateTime CreatedAt,
    DateTime? EndsAt,
    double Delivered,
    double Expected,
    int ProposerStars);

public record ContractsDto(IReadOnlyList<ContractDto> Mine, IReadOnlyList<ContractDto> Board);

public record ExchangeOrderDto(long Id, string Resource, bool Selling, double Amount, double Price, DateTime CreatedAt, bool Mine, bool Global);

public record ShipmentDto(long Id, string Resource, double Amount, DateTime ArrivesAt, bool Incoming, string Partner);

public record ExchangeBookDto(string Resource, double BestBid, double BidAmount, double BestAsk, double AskAmount);

public record ExchangeDto(bool HasOffice, IReadOnlyList<ExchangeBookDto> Books, IReadOnlyList<ExchangeOrderDto> Orders, IReadOnlyList<ShipmentDto> Shipments, double Fee, bool Global);

// ---------------------------------------------------------------- Society (M4)

public record CorporationSummaryDto(long Id, string Name, string Tag, string Color, string Description, int Members, int HqLevel, double WeeklyEarnings, bool Applied, bool Invited);

public record CorporationMemberDto(long Id, string Name, string Color, int Level, string Role, double WeeklyEarnings, double Contribution, bool Online);

public record CorporationGoalDto(string Resource, double Amount, double Delivered, double RewardGold, DateTime EndsAt, double YourContribution, double YourStock, bool Rewarded);

public record CorporationPerkDto(int Level, string Text, bool Active);

public record CorporationDto(
    long Id,
    string Name,
    string Tag,
    string Color,
    string Description,
    double Treasury,
    double Tax,
    int HqLevel,
    double? NextHqCost,
    string? PerkResource,
    string YourRole,
    IReadOnlyList<CorporationMemberDto> Members,
    IReadOnlyList<CorporationMemberDto> Applications,
    CorporationGoalDto? Goal,
    IReadOnlyList<CorporationPerkDto> Perks);

public record CorporationsDto(CorporationDto? Mine, IReadOnlyList<CorporationSummaryDto> District, int FoundLevel, double FoundCost, int MaxMembers, DateTime? CanJoinAt);

public record LeaderboardEntryDto(long Company, string Name, string Color, double Value, int Rank, bool You);

public record LeaderboardDto(string Kind, bool District, IReadOnlyList<LeaderboardEntryDto> Entries, LeaderboardEntryDto? You);

public record GuestbookNoteDto(long Id, long Author, string AuthorName, string Text, DateTime At);

/// <summary>An estate as a visitor sees it</summary>
public record EstateVisitDto(
    long Id,
    string Name,
    string Color,
    int Level,
    bool IsNpc,
    bool IsYou,
    int Likes,
    bool LikedToday,
    string? CorporationTag,
    string? CorporationName,
    double NetWorth,
    int Buildings,
    int Partners,
    DateTime CreatedAt,
    int EstateX,
    int EstateY,
    bool CanChooseAsMentor,
    bool IsYourMentor,
    int Mentees,
    IReadOnlyList<GuestbookNoteDto> Guestbook,
    bool SwapFromThem,
    bool SwapFromYou);

public record ChatMessageDto(long Id, string Channel, long Sender, string SenderName, string Text, DateTime SentAt);

public record ChatChannelDto(string Id, string Name, string Kind, DateTime? LastAt);

public record ChatDto(IReadOnlyList<ChatChannelDto> Channels, IReadOnlyList<long> Blocked);

public record InboxMessageDto(long Id, string Kind, string Text, DateTime At, bool Read);

public record InboxDto(IReadOnlyList<InboxMessageDto> Messages, int Unread);

// ---------------------------------------------------------------- Wonders and Eras (M5)

public record WonderNeedDto(string Resource, double Required, double Delivered, double YourStock);

public record WonderBuilderDto(long Company, string Name, string Color, double Value);

public record WonderDto(
    string Type,
    string Name,
    string Description,
    bool IsEra,
    int Stage,
    int Stages,
    bool Finished,
    IReadOnlyList<WonderNeedDto> Needs,
    double Total,
    double YourContribution,
    string YourTier,
    double YourShare,
    IReadOnlyList<WonderBuilderDto> TopBuilders);

public record WondersDto(int Era, DateTime EraStartedAt, DateTime EraDeadline, WonderDto? District, WonderDto? EraWonder, string DistrictName);

// ---------------------------------------------------------------- Company: managers, achievements, statistics (M5)

public record ManagerDto(string Id, string Name, double Cost, string Color, string Automation, string Bonus, bool Hired);

public record AchievementDto(string Id, string Name, string Description, double Amount, double Value, bool Done, double Xp, double Gold, string? Title);

public record CollectionDto(string Id, string Name, string Description, int Level, IReadOnlyList<string> Buildings, IReadOnlyList<string> Done, bool Finished, string? Reward);

public record StatDto(string Name, double Value);

public record CompanyDetailsDto(
    int ManagerSlots,
    IReadOnlyList<ManagerDto> Managers,
    IReadOnlyList<AchievementDto> Achievements,
    IReadOnlyList<CollectionDto> Collections,
    IReadOnlyList<string> Titles,
    IReadOnlyList<StatDto> Stats,
    IReadOnlyList<StatDto> Produced);

// ---------------------------------------------------------------- Moderation (M6)

public record ReportedMessageDto(long Id, string Channel, long Sender, string SenderName, string Text, DateTime SentAt);

// ---------------------------------------------------------------- Blueprints (M7)

public record BlueprintItemDto(string Type, int Dx, int Dy);

public record BlueprintPipeDto(int Dx, int Dy, int Tier);

public record BlueprintDto(
    long Id,
    string Name,
    long Owner,
    string OwnerName,
    string OwnerColor,
    int Width,
    int Height,
    IReadOnlyList<BlueprintItemDto> Buildings,
    IReadOnlyList<BlueprintPipeDto> Pipes,
    string Sharing,
    double Rating,
    int Ratings,
    int? MyRating,
    double Cost,
    DateTime CreatedAt);

public record BlueprintsDto(bool HasOffice, bool CanRebuild, int Max, IReadOnlyList<BlueprintDto> Mine, IReadOnlyList<BlueprintDto> Corporation, IReadOnlyList<BlueprintDto> Board);

/// <summary>A resident of a station: whether its pipes reach its Yard gate, what it has spare (goes to the hub) and lacks</summary>
/// <summary>A resident of a station; House is a House's kind (reliable, collector, grower, neighbourInNeed), StandIn the House standing in for an asleep player</summary>
public record YardResidentDto(long Id, string Name, string Color, int Level, bool IsNpc, int Slot, bool Connected, string? Activity, IReadOnlyList<ResourceRateDto> Spare, IReadOnlyList<ResourceRateDto> Lacks, bool Blocked, string? House, string? StandIn,
    IReadOnlyList<string> Makes, IReadOnlyList<string> Needs, IReadOnlyList<AskDto> Asks);

/// <summary>An Ask (v1.2 §12): a good asked of the station, how much came, until when, whether it renews itself</summary>
public record AskDto(string Resource, double Amount, double Got, DateTime Until, bool Renew, double PerMinute);

/// <summary>One of your trades through the Yard: with whom, what, how much per minute, which way, at what price</summary>
public record YardTradeDto(long Partner, string PartnerName, string Resource, double PerMinute, bool Selling, double Price);

/// <summary>
/// Your station and its Yard (v1.2 §7): its name and place, its residents, your trades with them, what goes through the
/// Yard, whether your own pipes reach your Yard gate, and one line about what happens
/// </summary>
public record YardDto(
    string Station,
    bool Small,
    int X0,
    int Y0,
    int X1,
    int Y1,
    int GateX,
    int GateY,
    bool Connected,
    IReadOnlyList<YardResidentDto> Residents,
    IReadOnlyList<YardTradeDto> Yours,
    IReadOnlyList<ResourceRateDto> Totals,
    string Line,
    IReadOnlyList<DiscoveryDto> Discoveries);

/// <summary>A Discovery (v1.2 §13): where, until when its markers are given, which stations got one, the three who brought most</summary>
public record DiscoveryDto(string Resource, int X, int Y, string Near, DateTime MarkingEnds, IReadOnlyList<string> Stations, IReadOnlyList<string> Top, bool YourStation);

/// <summary>A seat of a station as the picker shows it: its land, and who lives there (or that it is free, held, warmed by a House)</summary>
public record SeatDto(
    int Slot,
    int EstateX,
    int EstateY,
    string Primary,
    string? Secondary,
    /// <summary>free, house (a House keeps it warm: you can take it), held (kept for a crew's friend), taken, you</summary>
    string State,
    long? OwnerId,
    string? OwnerName,
    string? OwnerColor,
    int OwnerLevel,
    /// <summary>For strangers: lively, some days or resting (never a count of days)</summary>
    string? Activity,
    string? CrewName,
    bool HeldForYou);

/// <summary>A station on the picker: its seats, how lively it is, whether strangers may settle</summary>
public record StationDto(int DistrictX, int DistrictY, int Index, string Name, bool Small, bool Open, int Free, int Lively, IReadOnlyList<SeatDto> Seats, string? CrewName, int X0, int Y0, int X1, int Y1, string? Project = null,
    IReadOnlyList<string>? Makes = null, IReadOnlyList<string>? Needs = null);

public record SeatRefDto(int DistrictX, int DistrictY, int Slot);

/// <summary>The stations of a district for the picker, with the seats the game suggests for each way of starting</summary>
public record StationsDto(int DistrictX, int DistrictY, string District, IReadOnlyList<StationDto> Stations, SeatRefDto Neighbours, SeatRefDto OwnStation, SeatRefDto Friends, IReadOnlyList<DistrictRefDto> Districts);

public record DistrictRefDto(int DistrictX, int DistrictY, string Name);

/// <summary>What a seat code or crew link opens: the crew, who invites, the station and (for a seat code) the seat kept</summary>
public record InviteDto(bool Valid, string? CrewName, string? Emblem, string? Color, string? Color2, string? From, StationDto? Station, int? Slot, bool SeatCode, bool NeedsApproval, string? Name);

/// <summary>A crew mate; CanTend: you may tend their estate today (v1.2 §12), Tended: someone did lately, Asleep: a stand-in runs their trades</summary>
public record CrewMemberDto(long Id, string Name, string Color, int Level, string? Activity, string Station, int Slot, bool You, bool CanTend = false, bool Tended = false, bool Asleep = false);

/// <summary>The keep card (v1.2 §10): an asleep crew mate's seat whose hold ends soon</summary>
public record KeepCardDto(long Id, string Name, DateTime Ends);

/// <summary>A seat the crew keeps: for a friend (with its code), or for a member (For: a crew move, an asleep friend's seat)</summary>
public record HeldSeatDto(string Code, int DistrictX, int DistrictY, int Slot, string Primary, string? Secondary, DateTime Until, long? For = null, string? ForName = null, string? Station = null);

/// <summary>Your crew: its members, the seats it keeps for friends (with their codes), its invite link and who asked to join</summary>
public record CrewDto(
    long Id,
    string Name,
    string Emblem,
    string Color,
    string Color2,
    IReadOnlyList<CrewMemberDto> Members,
    IReadOnlyList<HeldSeatDto> Holds,
    string InviteCode,
    DateTime InviteOpenUntil,
    IReadOnlyList<CrewMemberDto> Requests,
    bool StationOpen,
    bool StationClosed,
    int MaxSize,
    IReadOnlyList<KeepCardDto> KeepCards,
    bool ConsolidationCard,
    /// <summary>Where the crew could move together (a station with a seat for everyone)</summary>
    string? MoveTo);

/// <summary>The crew panel: your crew (or none), and the emblems to choose from</summary>
public record CrewViewDto(CrewDto? Crew, IReadOnlyList<string> Emblems);

/// <summary>Founding a crew with friends: the seats held for them, each with its code</summary>
public record FoundCrewResultDto(bool Success, string? Error, IReadOnlyList<HeldSeatDto> Holds);

/// <summary>
/// A link (v1.2 §7) as one of its ends sees it: the good, the partner, the flow, the tank at the buyer's gate, the notice,
/// and one state line that always says what happens
/// </summary>
public record LinkDto(
    long Id,
    long PartnerId,
    string PartnerName,
    string PartnerColor,
    string Resource,
    bool YouSell,
    bool YouOffered,
    /// <summary>offered, active, paused</summary>
    string State,
    int Tier,
    int Length,
    double Efficiency,
    double Throughput,
    double Price,
    double Need,
    double Rate,
    double Tank,
    double TankSize,
    double Delivered,
    DateTime? NoticeAt,
    bool NoticeEnds,
    bool NoticeYours,
    bool CanResume,
    DateTime? OfferEndsAt,
    /// <summary>good, info, warn</summary>
    string Tone,
    string Line,
    IReadOnlyList<int> Route);

public record LinkSuggestionDto(long PartnerId, string PartnerName, string PartnerColor, string Station, string Resource, bool YouSell, double PerMinute, int Length, double Efficiency);

public record LinksDto(IReadOnlyList<LinkDto> Links, IReadOnlyList<LinkSuggestionDto> Suggestions, int Max, IReadOnlyList<LinkPartnerDto> Partners, TrunksDto Trunks);

/// <summary>
/// Beyond your own hub (v1.2 §13): what goes to far markets now, your crew's trunks, and where a new one could go
/// </summary>
public record TrunksDto(
    IReadOnlyList<RemoteSaleDto> Sales,
    IReadOnlyList<TrunkDto> Crew,
    IReadOnlyList<TrunkTargetDto> Targets,
    double SegmentGold,
    IReadOnlyList<ResourceRateDto> SegmentMaterials,
    bool CanBuild,
    string? Blocker);

public record RemoteSaleDto(string Resource, string Market, double PerMinute, double Price, bool Public);

public record TrunkDto(long Id, string Target, int Length, int Segments, int Needed, IReadOnlyList<string> Colors, bool Built, double Efficiency);

public record TrunkTargetDto(int? DistrictX, int? DistrictY, bool Port, string Name);

/// <summary>Someone a link could go to (players outside your station)</summary>
public record LinkPartnerDto(long Id, string Name, string Color, int Level, string Station);

public record LinkPlanDto(int Length, double Efficiency, double Throughput, double Gold, IReadOnlyList<ResourceRateDto> Materials, string? Error, IReadOnlyList<int> Route);

/// <summary>A link on the map: its route (x, y pairs) along the corridors</summary>
public record MapLinkDto(long Id, string Resource, int Tier, bool Flowing, long Seller, long Buyer, IReadOnlyList<int> Route, string Kind = "link", IReadOnlyList<string>? Colors = null, int Needed = 0);

/// <summary>
/// A station's project (v1.2 §12): the stage, its basket (with the keystone nobody here makes), who helped (the ring of
/// colours, as shares, never a list of who didn't), what the finished stages give every seat, and your own part
/// </summary>
public record ProjectDto(
    string Station,
    string Stage,
    int StageIndex,
    IReadOnlyList<string> Stages,
    IReadOnlyList<ProjectGoodDto> Basket,
    IReadOnlyList<ProjectHelperDto> Helpers,
    double Bonus,
    IReadOnlyList<string> FoundryChains,
    DateTime? OpensAt,
    double YourCredit,
    double YourAllowance,
    IReadOnlyList<string> ProjectFirst,
    string Line);

public record ProjectGoodDto(string Resource, double Target, double Delivered, bool Keystone, bool MadeHere);

public record ProjectHelperDto(string Color, double Share, bool You);
