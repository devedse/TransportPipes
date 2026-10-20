namespace TransportPipes.Server.Dtos;

// The contract with the frontend (TransportPipes.Frontend/src/api/contracts.ts). Serialized as camelCase JSON, enums as
// camelCase strings.

public record VersionInfo(string Version, string? Commit);

public record UserInfo(long Id, string UserName, bool IsAdmin = false);

public record LoginRequest(string UserName, string Password);

public record RegisterRequest(string UserName, string Password, string? Email);

public record AuthResult(bool Success, string? Error, UserInfo? User);

public record CommandResultDto(bool Success, string? Error)
{
    public static CommandResultDto Ok { get; } = new(true, null);

    public static CommandResultDto Fail(string error) => new(false, error);
}

/// <summary>Everything the client needs right after connecting</summary>
public record HelloDto(UserInfo User, CompanyDto? Company, ReportDto? Report, DateTime ServerTime, int Era, int Unread);

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
    IReadOnlyDictionary<string, double> SellPrices,
    IReadOnlyDictionary<string, double> BuyMaxPrices,
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
    DateTime ServerTime);

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

/// <summary>One resource on the hub market of the player's district, with the player's own prices</summary>
public record MarketResourceDto(
    string Resource,
    double BasePrice,
    double BuyPrice,
    double SellPrice,
    double SoldPerMinute,
    double Demand,
    double YourPrice,
    bool YourPriceIsAuto,
    double? PendingPrice,
    DateTime? PendingAt,
    double YourMaxPrice,
    bool NoTrade,
    double BandLow,
    double BandHigh,
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

public record ConductorDto(int X, int Y, string Kind, int Tier, long Owner, string? Side, int? Network);

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

public record EstateDto(int Slot, int X, int Y, long? OwnerId, string? OwnerName, string? OwnerColor, int OwnerLevel, int UnlockedParcels, IReadOnlyDictionary<string, string> GatePolicies, string Primary, string? Secondary, string? CorporationTag, string? Activity, string? ReservedFor);

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
    double TollIncome,
    double? RepairCost,
    int ComplexMembers,
    TeleporterDto? Teleporter = null);

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

public record NetworkResourceDto(string Resource, double Produced, double Used, double Sold, double Imported, double Traded, double Stored, double FromStorage, double Lost);

public record NetworkOwnerDto(long Id, string Name, string Color, double GoldPerMinute);

public record BottleneckDto(long A, long B, double Capacity, double Used, IReadOnlyList<int> Path);

public record NetworkDetailsDto(
    int Id,
    int Buildings,
    int Pipes,
    int Pumps,
    double PumpFactor,
    IReadOnlyList<NetworkOwnerDto> Owners,
    IReadOnlyList<NetworkResourceDto> Resources,
    IReadOnlyList<BottleneckDto> Bottlenecks);

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
    IReadOnlyDictionary<string, string> GatePolicies,
    bool Connected,
    IReadOnlyList<ResourceRateDto> Surplus,
    IReadOnlyList<ResourceRateDto> Needs,
    double YouEarn,
    double YouPay);

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
