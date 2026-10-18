namespace TransportPipes.Server.Dtos;

// The contract with the frontend (TransportPipes.Frontend/src/api/contracts.ts). Serialized as camelCase JSON, enums as
// camelCase strings.

public record VersionInfo(string Version, string? Commit);

public record UserInfo(long Id, string UserName);

public record LoginRequest(string UserName, string Password);

public record RegisterRequest(string UserName, string Password, string? Email);

public record AuthResult(bool Success, string? Error, UserInfo? User);

public record CommandResultDto(bool Success, string? Error)
{
    public static CommandResultDto Ok { get; } = new(true, null);

    public static CommandResultDto Fail(string error) => new(false, error);
}

/// <summary>Everything the client needs right after connecting</summary>
public record HelloDto(UserInfo User, CompanyDto? Company, ReportDto? Report, DateTime ServerTime, int Era);

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
    DateTime ServerTime);

public record DailyOrderDto(int Id, string Resource, double Amount, double Delivered, double RewardGold, double RewardXp, double RewardReputation, DateTime ExpiresAt);

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
    double BandHigh);

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
    IReadOnlyList<string> Notes);

public record BuildingDto(long Id, string Type, long Owner, int X, int Y, int Size, int Level, DateTime? ConstructionEndsAt, int? Network);

public record ConductorDto(int X, int Y, string Kind, int Tier, long Owner, string? Side, int? Network);

/// <summary>
/// A 32×32 area. Terrain is sent once (packed, one byte per tile, row by row), objects with every change.
/// </summary>
public record ChunkDto(int Cx, int Cy, string? Terrain, string? Deposits, string? Richness, string? Wind, string? Sun, IReadOnlyList<string> DepositNames, ChunkObjectsDto Objects);

public record ChunkObjectsDto(int Cx, int Cy, IReadOnlyList<BuildingDto> Buildings, IReadOnlyList<ConductorDto> Conductors, IReadOnlyList<TerrainOverrideDto> TerrainOverrides);

public record TerrainOverrideDto(int X, int Y, string Terrain);

public record EstateDto(int Slot, int X, int Y, long? OwnerId, string? OwnerName, string? OwnerColor, int OwnerLevel, int UnlockedParcels, IReadOnlyDictionary<string, string> GatePolicies, string Primary, string? Secondary);

public record HubStructureDto(string Type, int X, int Y, int Size);

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
    double TollIncome);

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

public record ExchangeOrderDto(long Id, string Resource, bool Selling, double Amount, double Price, DateTime CreatedAt, bool Mine);

public record ShipmentDto(long Id, string Resource, double Amount, DateTime ArrivesAt, bool Incoming, string Partner);

public record ExchangeBookDto(string Resource, double BestBid, double BidAmount, double BestAsk, double AskAmount);

public record ExchangeDto(bool HasOffice, IReadOnlyList<ExchangeBookDto> Books, IReadOnlyList<ExchangeOrderDto> Orders, IReadOnlyList<ShipmentDto> Shipments, double Fee);
