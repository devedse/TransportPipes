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
    DateTime ServerTime);

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
    double? UpgradeCost,
    double? UpgradeCost10,
    int MaxAffordableLevels,
    double? UpgradeCostMax,
    IReadOnlyList<BuildingFlowDto> Flows,
    double DemolishRefund,
    IReadOnlyList<string> Issues);

public record ToastDto(string Kind, string Text, long? Building);
