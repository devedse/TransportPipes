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

/// <summary>A command's result; Code names a failure the client acts on ("seatTaken": the picker loads the stations again)</summary>
/// <param name="Note">Done, and what the player should know about how (said instead of the client's own success line)</param>
public record CommandResultDto(bool Success, string? Error, string? Code = null, string? Note = null)
{
    public static CommandResultDto Ok { get; } = new(true, null);

    public static CommandResultDto Fail(string error, string? code = null) => new(false, error, code);
}

/// <summary>Everything the client needs right after connecting; LeftAt: when the player last left (the chat's "new since you left")</summary>
public record HelloDto(UserInfo User, CompanyDto? Company, ReturnCardDto? Card, DateTime ServerTime, int Era, int Unread, DateTime? LeftAt = null);

/// <summary>
/// The return card (F5): one card for every return with news, built on the server's clock. Gold and level, the fine
/// print (research done), and at most six lines in their sections.
/// </summary>
public record ReturnCardDto(
    string Away,
    double Earned,
    int LevelFrom,
    int Level,
    IReadOnlyList<string> Fine,
    IReadOnlyList<ReturnLineDto> Lines,
    string? Hint,
    /// <summary>What folds under "more" (after run 3, row 11: the card leads with what matters)</summary>
    IReadOnlyList<ReturnLineDto>? Folded = null);

/// <summary>
/// A line of the return card: its section, and its one button (repairAll, show, upgrade, links, fillWant, orderWant,
/// grow) with what it costs
/// </summary>
public record ReturnLineDto(string Kind, string Icon, string Text, string? Action, long? Building, double? Cost, long? Partner = null, string? Resource = null, string? PartnerName = null);

public record CompanyDto(
    long Id,
    string Name,
    string Color,
    double Gold,
    double GoldPerMinute,
    /// <summary>The Company Level (1.4 §14): the gold earned in all it needs, and what the next one needs</summary>
    int Level,
    double LevelEarned,
    double NextLevelEarned,
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
    ResearchLinesDto Research,
    IReadOnlyList<DailyOrderDto> DailyOrders,
    IReadOnlyList<string> NoTrade,
    long? MentorId,
    int Likes,
    IReadOnlyList<GameEventDto> Events,
    int ForecastHours,
    /// <summary>Your town's Era, and your own (the highest a town reached while you lived there)</summary>
    int Era,
    int YourEra,
    IReadOnlyList<string> Titles,
    IReadOnlyList<string> Collections,
    IReadOnlyList<OutpostDto> Outposts,
    /// <summary>The Legacy level the player brought from earlier seasons</summary>
    int LegacyLevel,
    MovingDto Moving,
    DeepDto Deep,
    TutorialDto? Tutorial,
    ExpeditionDto Expedition,
    IReadOnlyList<AlertDto> Alerts,
    DateTime ServerTime,
    EarnedDto Earned,
    CrewBadgeDto? Crew,
    /// <summary>Your plot's land: what it has, and who in your Yard has the rest</summary>
    LandDto? Land = null,
    /// <summary>Neighbours' scaffolds and pipes this player could help with (after run 3, steps 2 and 3)</summary>
    IReadOnlyList<WantNearDto>? WantsNear = null,
    /// <summary>This player's own open wants, a building's or a pipe's</summary>
    IReadOnlyList<OwnWantDto>? Wants = null,
    /// <summary>The Era Wonder on the goal tracker (after run 3, step 5): its stage's share, and your part or what it wants</summary>
    ProjectWeekDto? EraWonderLine = null,
    /// <summary>The pipe ladder on the goal tracker while a pipe is full (after run 4: the way up was nowhere in sight)</summary>
    PipesLineDto? Pipes = null,
    /// <summary>Your prices for other players (1.4 §8): every one you set, with a rise that waits</summary>
    IReadOnlyList<PriceDto>? Prices = null,
    /// <summary>What you give your town's Wonder by itself, per good: "beyondHubOrder" or "all" (1.4 §13)</summary>
    IReadOnlyDictionary<string, string>? Give = null,
    /// <summary>Whether the market brings what your levels and buildings lack, at 2.5× (1.4 §9)</summary>
    bool WantsFromMarket = true,
    OutskirtsDto? Outskirts = null,
    WorkshopsDto? Workshops = null,
    /// <summary>The wilderness chunks your expeditions mapped ("x,y"): the rest of it is fog on your map (1.4 §20)</summary>
    IReadOnlyList<string>? Explored = null,
    /// <summary>What your own goods going into your wants now would sell for a minute (your income is that much lower until they're in)</summary>
    double OwnGoodsIntoWants = 0);

/// <summary>Your plot's outskirts (1.4 §5): the parcels you bought (bits 0..8), what the next costs, and why you can't buy one yet</summary>
public record OutskirtsDto(int Parcels, double NextCost, string? Blocker);

/// <summary>Building in the Yard (1.4 §7): the tiles of its ends your workshops cover, of how many, from which Level; the crew's Depot</summary>
public record WorkshopsDto(int TilesUsed, int Tiles, int Level, DepotDto? Depot);

/// <summary>The Yard's Depot: where it stands, what it holds of how much, and every resident's goods in it</summary>
public record DepotDto(long Building, int X, int Y, double Capacity, double Holds, IReadOnlyList<DepotGoodDto> Goods);

public record DepotGoodDto(string Resource, double Amount, long Owner, string OwnerName, bool Yours);

/// <summary>
/// The Lab's two lines (1.4 §12): the Era tree's items done, being researched and queued; the tracks' tiers, the tier
/// being researched and the queue (a track once for every tier); whether research starts by itself while you're away,
/// and whether something can be researched now
/// </summary>
public record ResearchLinesDto(bool HasLab, IReadOnlyList<string> Researched, string? CurrentResearch, DateTime? ResearchEndsAt, IReadOnlyList<string> ResearchQueue,
    IReadOnlyDictionary<string, int> Tiers, string? CurrentTrack, DateTime? TrackEndsAt, IReadOnlyList<string> TrackQueue, bool KeepResearching, bool Open);

/// <summary>
/// A track as the research panel shows it: its tiers, the next tier's price (gold, and from tier 3 its good: from your
/// Storages, the rest bought), how long it takes, what it does and about what it adds a minute; why it can't start
/// </summary>
public record TrackStateDto(string Id, int Tiers, int? Next, double Gold, string? Good, double Units, double FromStorages, double Bought, double Total,
    double Minutes, string? Effect, double Amount, double Gain, double HubOrderFactor, string? Reason);

/// <summary>
/// One of your prices: its key ("public", "crew", "public:good", "crew:good", "for:companyId"), what it is now, and a
/// rise that counts at PendingAt (Pending null with Back: the setting goes); ForName names a special price's player
/// </summary>
public record PriceDto(string Key, double Value, double? Pending, bool Back, DateTime? PendingAt, string? ForName);

/// <summary>
/// What your gold can do (after run 4, "your gold can"): at most three lines, shared first: the shared goal, a want
/// nobody near can fill, and "Grow everything that pays"
/// </summary>
public record GoldUsesDto(GoldLineDto? Shared, GoldLineDto? Want, GrowPlanDto? Grow, double Hour);

/// <summary>
/// One line: its words, and what its button does ("wonder": order Resource for Cost, or all; "want": order the rest of
/// Target for Cost, for ForName)
/// </summary>
public record GoldLineDto(string Text, string Action, double? Cost = null, string? Resource = null, long? Target = null, string? ForName = null, double? AllCost = null);

/// <summary>"Grow everything that pays": its parts ("Iron Mine to level 30 · 165k · +94 g/min"), its price and gain</summary>
/// <summary>Grow's plan: its parts, its gold, what it adds a minute once in, and what its levels' goods cost on top (they come after)</summary>
public record GrowPlanDto(IReadOnlyList<GrowPartDto> Parts, double Cost, double Gain, DateTime? UndoUntil, string? Saving = null, double Goods = 0);

public record GrowPartDto(long Building, string Text, double Cost, double Gain);

/// <summary>"Your Iron Mine's Grey Pipe is full: Copper Pipe on its link, then Level 30": the building to open, and the ladder ("Grey · Copper · Steel with Era 2")</summary>
public record PipesLineDto(string Text, long Building, int Full, string Ladder);

/// <summary>
/// An own want: its target (a building's id, or minus a pipe want's number), what it wants, what is in and coming; Tiles
/// for a pipe; ToLevel: the level its goods are for (null: a new building's materials or a pipe's); a pipe's tiles (x, y,
/// x, y, …) and the tier they become, for the map
/// </summary>
public record OwnWantDto(long Target, string Name, string Resource, double Amount, double Got, double PerMinute, int? Tiles, string? Order = null, double? OrderCost = null,
    int? ToLevel = null, IReadOnlyList<int>? PipeAt = null, int? PipeTier = null);

/// <summary>
/// Your plot's land (1.4 §5): what it has and lacks (one basic, one later resource), its strength (a ★4 deposit), and
/// every plot of your Yard with its land and who lives there
/// </summary>
public record LandDto(IReadOnlyList<string> Goods, IReadOnlyList<string> Lacks, string Strength, IReadOnlyList<LandSeatDto> Station);

/// <param name="Makes">Of its land goods, those its owner makes now (a finished building of theirs makes it)</param>
public record LandSeatDto(int Slot, string? Owner, bool You, IReadOnlyList<string> Goods, IReadOnlyList<string> Lacks, string Strength, IReadOnlyList<string> Makes);

/// <summary>Your Yard's crew on the top bar: its name and banner, its residents, the requests that wait for an answer</summary>
public record CrewBadgeDto(long Id, string Name, string Emblem, string Color, string Color2, int Members, int Requests);

/// <summary>The gold earned (1.4 §14): what it earns a minute now, and where it came from (per source per item)</summary>
public record EarnedDto(double PerMinute, IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> Log);

/// <summary>Something that holds the company back, with the building to look at</summary>
/// <summary>What holds a company back (the goal tracker): pumps, post, storage or damage (Repair all), with the building to look at and what repairing all costs</summary>
public record AlertDto(string Text, long? Building, string Kind = "", double? Cost = null);

/// <summary>Expeditions (GDD §11.3): the one out, what they cost, what came back, the relics found</summary>
public record ExpeditionDto(int? Hours, DateTime? EndsAt, IReadOnlyDictionary<int, double> Costs, string? Blocker, IReadOnlyList<string> Log, IReadOnlyList<string> Relics);

/// <summary>Loady's guide: the step the company is on</summary>
/// <summary>
/// Loady's current step; WaitLevel: the Company Level it needs first (null when it can be done now); Tip: why it isn't
/// done yet although the player did what it says
/// </summary>
/// <summary>Loady's current step; Chapter, ChapterStep (1-based) and ChapterSteps place it in its chapter</summary>
/// <param name="PhoneText">The step's text on a phone, when it differs</param>
/// <param name="Pipe">The pipe Show me lays (tiles as x, y pairs; empty: Show me does what the hint says), with its price</param>
/// <param name="Since">When the step began (open two days: it shrinks to one line)</param>
public record TutorialDto(int Step, int Steps, string Id, string Text, double Progress, double Amount, double Gold, IReadOnlyDictionary<string, string> Hint, int? WaitLevel, string? Tip, int Chapter, int ChapterStep, int ChapterSteps,
    string? PhoneText = null, int[]? Pipe = null, double PipeCost = 0, string? PipeTarget = null, DateTime? Since = null, int? WaitEra = null);

/// <summary>
/// The Deep Level (GDD §11.2): whether there is a lift, the dug out parcels, the parcel being dug and what the next one
/// costs. Deep tiles are surface tiles shifted down by DeepOffset.
/// </summary>
public record DeepDto(bool Open, int Parcels, int? DigParcel, DateTime? DigStartedAt, DateTime? DigEndsAt, double NextCost, double NextHours, int Offset);

/// <summary>Moving and activity (1.4 §18): a move in progress, crates, free moves</summary>
public record MovingDto(string Activity, DateTime? ArrivesAt, IReadOnlyList<CrateDto> Crates, int FreeMoves, double MoveCost, DateTime? CanMoveAt);

public record CrateDto(int Index, string Type, int Level);

/// <summary>A district a company could move to</summary>
public record DistrictChoiceDto(int X, int Y, string Name, int FreeEstates, int Players, double ActiveShare, bool Yours);

/// <summary>An event in the calendar (GDD §15)</summary>
public record GameEventDto(long Id, string Kind, string Name, string Effect, DateTime StartsAt, DateTime EndsAt, string? Resource, bool Active, bool Everywhere);

/// <param name="Selling">What the company's trading posts sell of the good a minute now (an order fills from it, after run 3, step 7)</param>
/// <param name="Premium">What it paid over the hub's price so far</param>
public record DailyOrderDto(int Id, string Resource, double Amount, double Delivered, double RewardGold, DateTime ExpiresAt, bool Urgent, double Selling = 0, double Premium = 0, double Stored = 0, double HubPrice = 0);

/// <summary>
/// One good at the market of the player's town (1.4 §8): what it pays the player now (its own Hub Order), what it
/// charges, what the player sold there lately against its Hub Order, the default player price, and the trade settings
/// </summary>
public record MarketResourceDto(
    string Resource,
    double BasePrice,
    double BuyPrice,
    double SellPrice,
    double SoldPerMinute,
    double HubOrder,
    double PlayerPrice,
    bool NoTrade,
    bool NoBuy);

public record MarketDto(int DistrictX, int DistrictY, string District, IReadOnlyList<MarketResourceDto> Resources, DateTime NextUpdate);

/// <summary>A tech as one company sees it</summary>
public record ResearchStateDto(string Id, string State, double Cost, string? Reason);

/// <param name="SiteShare">A scaffold that wants materials: how much of them is in (0–1); null for any other building</param>
public record BuildingDto(long Id, string Type, long Owner, int X, int Y, int Size, int Level, DateTime? ConstructionEndsAt, int? Network, bool Damaged, long? Link = null, double? SiteShare = null);

/// <summary>A scaffold's wants (after run 3, step 2): each material, what is in, what comes now and from whom, and what Cancel gives back</summary>
public record SiteDto(IReadOnlyList<SiteWantDto> Wants, double Refund);

/// <param name="Chain">When nobody near makes it: the building that would, and who makes its inputs</param>
/// <param name="Order">An order's state ("the hub brings it from Cleo's next session, at the latest Thu 20:00"); null: none</param>
/// <param name="OrderCost">What ordering the rest costs now (null: ordered already, or nothing left)</param>
/// <param name="ChainBuilding">The building of the chain line, when the viewer may build it (Show me)</param>
public record SiteWantDto(string Resource, double Amount, double Got, double PerMinute, IReadOnlyList<string> From, string? Chain, string? Order = null, double? OrderCost = null, string? ChainBuilding = null);

/// <summary>A neighbour's scaffold this player could help: what it wants, what this player's Storages hold of it, and whether it makes it</summary>
public record WantNearDto(long Site, string Owner, string Building, string Resource, double Left, double Holds, bool Makes, double? OrderCost = null);

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
/// <param name="Price">What its owner bought it for (only for its owner): an outpost is bought once (1.4 §20)</param>
public record OutpostDto(long Id, long Owner, string OwnerName, string OwnerColor, int X, int Y, int Size, double? Price);

/// <summary>A deposit that differs from the generated terrain (a Discovery)</summary>
public record DepositOverrideDto(int X, int Y, string Deposit, int Richness);

public record OutpostCheckDto(string? Error, int X, int Y, double Price, int Size, string? Deposit, int RichTiles);

public record TerrainOverrideDto(int X, int Y, string Terrain);

/// <summary>An estate on the map; Goods: the land goods dealt to it (step 5), House: a House lives there</summary>
/// <summary>A plot on the map: who lives there, its parcels, and its land (what it has and lacks, its strength)</summary>
public record EstateDto(int Slot, int X, int Y, long? OwnerId, string? OwnerName, string? OwnerColor, int OwnerLevel, int UnlockedParcels, string Primary, string? Secondary, string? Activity,
    IReadOnlyList<string>? Goods = null, IReadOnlyList<string>? Lacks = null, string? Strength = null, int OutskirtsParcels = 0);

/// <summary>A hub structure; a Wonder site has its Wonder's name, stage and variant</summary>
public record HubStructureDto(string Type, int X, int Y, int Size, string? Label = null, string? Color = null, int Level = 0, string? Variant = null, int Stages = 0);

/// <param name="Era">The town's Era, whether its Space Elevator launched, and its stars (missions done): the world map's race</param>
public record DistrictDto(int X, int Y, int OriginX, int OriginY, string Name, IReadOnlyList<EstateDto> Estates, IReadOnlyList<HubStructureDto> Hub, IReadOnlyList<DistrictYardDto> Yards,
    int Era = 1, bool Launched = false, int Stars = 0, IReadOnlyList<MapLineDto>? Lines = null);

/// <summary>
/// A trunk line (1.4 §20) as a player sees it: its carrier, the two towns, its tier, how much of what's sent arrives and
/// how much it carries, its fees (and yours), what it earned its carrier, its route
/// </summary>
public record TrunkLineDto(long Id, string Carrier, string CarrierColor, bool Yours, string From, string To, int ToX, int ToY, int Tier, int Length,
    double Efficiency, double Throughput, double Carried, double PublicFee, double CrewFee, double YourFee, double Earned, IReadOnlyList<int> Route,
    IReadOnlyList<LinePlayerDto> Players);

/// <summary>A player of the two towns a carrier may give a fee of their own (null: the public or crew fee)</summary>
public record LinePlayerDto(long Id, string Name, string Color, double? Fee);

/// <summary>A neighbouring town a line could run to from yours: its name and Era, what the line would cost, and why it can't be (or null)</summary>
public record LineOptionDto(int X, int Y, string Name, int Era, int Tier, int Length, double Gold, string? Error);

/// <summary>Your town's trunk lines, where one could run, the tiers you may build, and the goods you send over them to other markets</summary>
public record LinesDto(IReadOnlyList<TrunkLineDto> Lines, IReadOnlyList<LineOptionDto> Options, IReadOnlyList<int> Tiers, IReadOnlyDictionary<string, long> Exports);

/// <summary>A trunk line on the map: its route's corners (x, y pairs), its tier and its carrier's colour</summary>
public record MapLineDto(long Id, IReadOnlyList<int> Route, int Tier, string Color);

/// <summary>A town's Yard on the map: its crew's name and banner (none while nobody lives there), open or closed, its stalls</summary>
public record DistrictYardDto(int Index, string Name, string? Emblem, string? Color, string? Color2, bool Open, IReadOnlyList<StallDto> Stalls,
    IReadOnlyList<YardMonumentDto>? Monuments = null);

/// <summary>A Monument standing in a Yard's square (1.4 §13): its Era's site, its kind and how many stages it has</summary>
public record YardMonumentDto(int Era, string Kind, int Stages);

/// <summary>A resident's stall at its Yard gate: what it sells the crew, its crew price, what it would buy</summary>
public record StallDto(int Slot, IReadOnlyList<string> Sells, double CrewPrice, IReadOnlyList<string> Wants);

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
    BuildingStatusDto? Status = null,
    BuildingTradeDto? Trade = null,
    /// <summary>What the next level and Max will do to your income, solved (after run 3: "adds nothing: its pipes carry 100 coal a minute")</summary>
    string? UpgradePreview = null,
    string? MaxPreview = null,
    /// <summary>Its last purchase can be taken back for this much gold until UndoUntil (null: nothing to undo)</summary>
    double? UndoRefund = null,
    DateTime? UndoUntil = null,
    /// <summary>A scaffold that still wants materials (null: none)</summary>
    SiteDto? Site = null,
    /// <summary>The station's Refinements this building gets, named: "Bricks +6% · Cleo's research 💛"</summary>
    /// <summary>Its pipe door, when its pipe is what holds it back (after run 4)</summary>
    PipeDoorDto? PipeDoor = null,
    /// <summary>The levels bought whose goods are still coming (1.4 §9; null: none)</summary>
    LevelWaitDto? LevelWait = null,
    /// <summary>What this copy cost to build (its levels cost from it), and the goods the next step takes</summary>
    double CopyPrice = 0,
    IReadOnlyList<ResourceAmountDto>? NextLevelGoods = null,
    /// <summary>The track it follows and your tiers in it (1.4 §12)</summary>
    string? Track = null,
    int TrackTiers = 0,
    /// <summary>A building of a later Era than its town's: it sleeps until the town gets there</summary>
    bool Sleeping = false,
    /// <summary>A trading post: what it sells by good against your Hub Orders (null: not a trading post)</summary>
    IReadOnlyList<PostSaleDto>? Sales = null);

/// <summary>
/// A good a trading post sells: what all your posts sell of it a minute, your Hub Order for it (what the market pays the
/// full price for: beyond it each unit pays less), and the gold a minute this post gets for it
/// </summary>
public record PostSaleDto(string Resource, double PerMinute, double HubOrder, double Gold);

/// <summary>A building that works at its old level until the goods of the levels bought are in (1.4 §9)</summary>
public record LevelWaitDto(int BoughtLevel, IReadOnlyList<SiteWantDto> Wants, double? BuyRest);

/// <summary>
/// The way up a full pipe (after run 4: the pipe tier is the game's real level cap, and its door was nowhere a player
/// looked): the next tier or Widen on its full links, then the levels the new pipe carries, with one preview
/// </summary>
/// <param name="Kind">"tier", "widen", "waits" (its goods come first) or "locked" ("Copper Pipes: yours at Company Level 8")</param>
/// <param name="Text">What it does: "Copper Pipe on its 2 links (34 tiles), then Level 30"</param>
/// <param name="Label">Its button: the text with the price</param>
/// <param name="Short">Of what the pipe takes, what your Storages lack, and where it comes from</param>
public record PipeDoorDto(string Kind, string Text, string Label, double Gold, double LevelCost, int ToLevel, double Gain,
    IReadOnlyList<ResourceAmountDto> Materials, IReadOnlyList<ResourceAmountDto> Short, string? Source, double? OrderCost);

/// <summary>
/// A building's trade with its neighbours, for its owner (step 4): who takes its goods and who sends it its inputs (both
/// ends named), who could use its goods, what the project wants of them, and the pipe to the gate a Send lays first
/// </summary>
public record BuildingTradeDto(
    IReadOnlyList<TradeEndDto> Takers,
    IReadOnlyList<TradeEndDto> Suppliers,
    IReadOnlyList<BidDto> Bids,
    bool ReachesGate,
    BuildingFixDto? GatePipe,
    /// <summary>"runs on Ben's sand": its biggest supplier</summary>
    string? RunsOn);

/// <summary>One end of a Yard trade: the partner, its building, how much a minute at what price; Sent: a Send; CanThank: Thanks today</summary>
public record TradeEndDto(string Resource, long Partner, string PartnerName, string? PartnerBuilding, long? PartnerBuildingId, double PerMinute, double Price);

/// <summary>"Ana's Glassworks could use 15/min · it buys from Bertha ⚙ now · Send to Ana ▸"</summary>
public record BidDto(string Resource, long Buyer, string BuyerName, string? Building, long? BuildingId, double PerMinute);

/// <summary>
/// A building's panel head (F2): why it doesn't run fully (Cause, from IdleReason), the status pill (the cause in five
/// words), the reach line, and the one fix button; null pill: it runs fine
/// </summary>
public record BuildingStatusDto(string Cause, string? Pill, string? Reach, BuildingFixDto? Fix);

/// <summary>
/// The one fix button: Kind layPipe (Tiles as x,y pairs, to show as a ghost and lay), open (Building), repair, upgrade
/// (Building by a level) or build (the build tool with BuildingType); Target names where a pipe goes
/// </summary>
public record BuildingFixDto(string Kind, string Label, int[] Tiles, int NewTiles, double Cost, long? Building, string? BuildingType, string? Target, bool OwnNeed = false, int Tier = 0);

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
public record StorageKeepDto(IReadOnlyDictionary<string, double> Keeps);

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
/// <param name="FromYard">What neighbours' goods bring to its buildings through the Yard gate</param>
/// <param name="ToYard">What its buildings sell to neighbours through the Yard gate</param>
public record NetworkResourceDto(string Resource, double Produced, double Used, double Sold, double Imported, double Traded, double Stored, double FromStorage, double Lost, double Needed, double FromYard, double ToYard);

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
    IReadOnlyList<StuckBuildingDto>? Stuck = null,
    /// <summary>The viewer's pipe tiles that wait for a better pipe already (x, y pairs): Upgrade leaves them out</summary>
    IReadOnlyList<int>? Waiting = null);

/// <summary>A building of the network whose goods can't leave (F2): the network card names it first</summary>
public record StuckBuildingDto(long Building, string Type, string Line);

public record ResourceRateDto(string Resource, double PerMinute);

public record NeighbourDto(
    long Id,
    string Name,
    string Color,
    int Level,
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
    bool Blocked,
    /// <summary>A block you set that waits its hour's notice: when it counts (playtest: the switch snapped back)</summary>
    DateTime? BlockAt = null);

public record ExchangeOrderDto(long Id, string Resource, bool Selling, double Amount, double Price, DateTime CreatedAt, bool Mine, bool Global,
    /// <summary>What was placed (0: not kept), and when it closes</summary>
    double Placed = 0, DateTime? ExpiresAt = null);

public record ShipmentDto(long Id, string Resource, double Amount, DateTime ArrivesAt, bool Incoming, string Partner);

public record ExchangeBookDto(string Resource, double BestBid, double BidAmount, double BestAsk, double AskAmount);

/// <param name="Opens">The Company Level the Exchange opens at (1.4 §8: 12), null once it's open</param>
public record ExchangeDto(int? Opens, IReadOnlyList<ExchangeBookDto> Books, IReadOnlyList<ExchangeOrderDto> Orders, IReadOnlyList<ShipmentDto> Shipments, double Fee, bool Global,
    /// <summary>The default player price of each good, and what your Storages hold (a sell order holds its goods there; after run 4)</summary>
    IReadOnlyDictionary<string, double>? Prices = null, IReadOnlyDictionary<string, double>? Stored = null);

// ---------------------------------------------------------------- Society (M4)

public record LeaderboardEntryDto(long Company, string Name, string Color, double Value, int Rank, bool You);

public record LeaderboardDto(string Kind, bool District, IReadOnlyList<LeaderboardEntryDto> Entries, LeaderboardEntryDto? You);

public record GuestbookNoteDto(long Id, long Author, string AuthorName, string Text, DateTime At);

/// <summary>An estate as a visitor sees it</summary>
public record EstateVisitDto(
    long Id,
    string Name,
    string Color,
    int Level,
    bool IsYou,
    int Likes,
    bool LikedToday,
    /// <summary>The Yard they live at</summary>
    string? Yard,
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
    /// <summary>What their scaffolds and pipes wait for, and what ordering it for them costs you (after run 4: a friend's want in sight where you visit)</summary>
    IReadOnlyList<WantNearDto>? Wants = null);

public record ChatMessageDto(long Id, string Channel, long Sender, string SenderName, string Text, DateTime SentAt);

public record ChatChannelDto(string Id, string Name, string Kind, DateTime? LastAt);

public record ChatDto(IReadOnlyList<ChatChannelDto> Channels, IReadOnlyList<long> Blocked);

public record InboxMessageDto(long Id, string Kind, string Text, DateTime At, bool Read);

public record InboxDto(IReadOnlyList<InboxMessageDto> Messages, int Unread);

// ---------------------------------------------------------------- Your town: its Era, its Wonder, your Yard's Monuments (1.4 §11, §13)

/// <param name="BuyCost">What buying and giving the rest of it costs (the market's 2.5×)</param>
/// <param name="PerMinute">What the town's "give" switches send it now a minute</param>
/// <param name="Give">Your give switch for it: "off", "beyondHubOrder" or "all"</param>
/// <param name="Makers">The town's players who make it</param>
/// <param name="Chain">When you don't make it: the building that would ("A Brickworks (1,400 g) makes bricks from …")</param>
/// <param name="ChainBuilding">That building's type, for Show me (null when you may not build it yet)</param>
public record WonderNeedDto(string Resource, double Required, double Delivered, double YourStock, double BuyCost, double PerMinute, string Give,
    IReadOnlyList<string> Makers, string? Chain = null, string? ChainBuilding = null);

public record WonderBuilderDto(long Company, string Name, string Color, double Value, double Share);

/// <summary>A town's Era Wonder: its stage and goods, when a full stage stands, your part and tier, the perks it gave</summary>
/// <param name="Trail">How much smaller towns that finished it before made it (0.9: 10%)</param>
public record TownWonderDto(
    string Type,
    string Name,
    string Description,
    int Era,
    int Stage,
    int Stages,
    IReadOnlyList<WonderNeedDto> Needs,
    double StageWorth,
    double Worth,
    DateTime? StandsAt,
    double YourStageShare,
    string YourTier,
    double YourGiven,
    IReadOnlyList<WonderBuilderDto> TopBuilders,
    double BuyAllCost,
    double Trail,
    IReadOnlyList<string> Perks,
    string? NextPerk);

/// <summary>A Monument site of your Yard: its Era, what was built there, its stages and the next stage's price</summary>
public record MonumentDto(int Era, string? Kind, string? Name, int Stages, double Paid, double NextCost, bool Capped, string? Perk, double YourPaid);

public record MonumentKindDto(string Id, string Name, string Description, bool Built);

/// <summary>Your town and Yard (1.4 §11, §13): its Era, its launch party, its Wonder, your Yard's Monuments</summary>
public record TownDto(string Name, int Era, int YourEra, DateTime EraStartedAt, DateTime? PartyUntil, TownWonderDto? Wonder, IReadOnlyList<MonumentDto> Monuments,
    IReadOnlyList<MonumentKindDto> Kinds, int MonumentLevel, double YourHour, DateTime? LaunchedAt, int Stars = 0);

// ---------------------------------------------------------------- Company: managers, achievements, statistics (M5)

public record ManagerDto(string Id, string Name, double Cost, string Color, string Automation, string Bonus, bool Hired);

/// <param name="Gold">What it pays: hours of your income, at least this much gold</param>
public record AchievementDto(string Id, string Name, string Description, double Amount, double Value, bool Done, double Hours, double Gold, string? Title);

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

/// <summary>
/// A resident of a Yard: whether its pipes reach its Yard gate, what it has spare (goes to the hub) and lacks, and what it
/// pinned on the Plan board
/// </summary>
/// <param name="Prices">Their price for you (a multiple of the base) of each good they have spare: what their stall shows you</param>
public record YardResidentDto(long Id, string Name, string Color, int Level, int Slot, bool Connected, string? Activity, IReadOnlyList<ResourceRateDto> Spare, IReadOnlyList<ResourceRateDto> Lacks, bool Blocked,
    IReadOnlyList<string> Makes, IReadOnlyList<string> Needs, IReadOnlyDictionary<string, double> Prices);

/// <summary>One of your trades through the Yard: with whom, what, how much per minute, which way, at what price</summary>
public record YardTradeDto(long Partner, string PartnerName, string Resource, double PerMinute, bool Selling, double Price);

/// <summary>What you and a neighbour sold each other of a good this week (units each way)</summary>
public record WeekTradeDto(long Partner, string Name, string Resource, double Given, double Received);

/// <summary>
/// Your Yard (1.4 §7): its name and place, its residents, your trades with them, what goes through the Yard, whether
/// your own pipes reach your Yard gate, and one line about what happens
/// </summary>
public record YardDto(
    string Station,
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
    IReadOnlyList<DiscoveryDto> Discoveries,
    /// <summary>Who could use your goods ("Ana's Glassworks could use your sand")</summary>
    IReadOnlyList<BidDto> Bids,
    /// <summary>What you and each neighbour sold each other this week</summary>
    IReadOnlyList<WeekTradeDto> Trades,
    /// <summary>What the Yard pays you of your income, and whom for it (after run 4: Cleo added up her trades and asked where the rest came from)</summary>
    string? IncomeLine = null,
    /// <summary>The Company Level from which you pay into the Yard's Monuments (its "how it works" says when)</summary>
    int MonumentLevel = 0);

/// <summary>A Discovery (v1.2 §13): where, until when its markers are given, which stations got one, the three who brought most</summary>
public record DiscoveryDto(string Resource, int X, int Y, string Near, DateTime MarkingEnds, IReadOnlyList<string> Stations, IReadOnlyList<string> Top, bool YourStation);

/// <summary>
/// A plot as the map's card shows it (1.4 §6): its land and who lives there. State: free, closed (free, but its Yard is
/// closed: you ask first), taken, you
/// </summary>
public record SeatDto(
    int Slot,
    int EstateX,
    int EstateY,
    string Primary,
    string? Secondary,
    string State,
    long? OwnerId,
    string? OwnerName,
    string? OwnerColor,
    int OwnerLevel,
    /// <summary>For others: lively, some days or resting (never a count of days)</summary>
    string? Activity,
    /// <summary>What the plot's land has and lacks, and its strength (a ★4 deposit of something it has)</summary>
    IReadOnlyList<string>? Goods = null,
    IReadOnlyList<string>? Lacks = null,
    string? Strength = null,
    /// <summary>For a player who would move here: how many of their buildings wouldn't fit its land and wait in crates</summary>
    int? Crates = null);

/// <summary>A Yard on the map: its crew's name and banner, whether anyone may settle, its plots and its Plan board</summary>
public record StationDto(int DistrictX, int DistrictY, int Index, string Name, string? Emblem, string? Color, string? Color2, bool Open, int Free, int Lively,
    IReadOnlyList<SeatDto> Seats, int X0, int Y0, int X1, int Y1, IReadOnlyList<string> Makes, IReadOnlyList<string> Needs);

public record SeatRefDto(int DistrictX, int DistrictY, int Slot);

/// <summary>A player Find a player found (1.4 §6): who, and where they live</summary>
public record FoundPlayerDto(long Id, string Name, string Color, int Level, int DistrictX, int DistrictY, int Slot, string District, string? Yard);

/// <summary>A town's Yards for the start on the map, the plot Pick for me chooses, and the open towns</summary>
public record StationsDto(int DistrictX, int DistrictY, string District, IReadOnlyList<StationDto> Stations, SeatRefDto PickForMe, IReadOnlyList<DistrictRefDto> Districts);

/// <summary>A town in the picker's list, with where it stands in the race: its Era, its Launch and its stars</summary>
public record DistrictRefDto(int DistrictX, int DistrictY, string Name, int Era = 1, bool Launched = false, int Stars = 0);

/// <summary>What a "Play next to me" link opens (1.4 §6): who sent it, their Yard, whether it lets you into a closed Yard, a free company name</summary>
public record NextToMeDto(bool Valid, string? From, string? FromColor, StationDto? Yard, bool Opens, string? Name);

/// <summary>A resident of your Yard</summary>
public record CrewMemberDto(long Id, string Name, string Color, int Level, string? Activity, int Slot, bool You);

/// <summary>Someone who asked to settle in your closed Yard, on which plot, and until when the request waits</summary>
public record CrewRequestDto(long Id, string Name, string Color, int Level, int Slot, DateTime Until);

/// <summary>
/// Your Yard's crew (1.4 §7): its name and banner (Named: a resident named it), open or closed, its residents, the
/// requests, your "Play next to me" link and until when it works, and whether you'd be the last one out
/// </summary>
public record CrewDto(long Id, string Name, string Emblem, string Color, string Color2, bool Named, bool Open, IReadOnlyList<CrewMemberDto> Members,
    IReadOnlyList<CrewRequestDto> Requests, string? Link, DateTime? LinkUntil, bool LastOut);

/// <summary>The crew panel: your crew (or none), and the emblems to choose from</summary>
public record CrewViewDto(CrewDto? Crew, IReadOnlyList<string> Emblems);

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

public record LinksDto(IReadOnlyList<LinkDto> Links, IReadOnlyList<LinkSuggestionDto> Suggestions, int Max, IReadOnlyList<LinkPartnerDto> Partners);

/// <summary>Someone a link could go to (players outside your station)</summary>
public record LinkPartnerDto(long Id, string Name, string Color, int Level, string Station, bool Crew = false);

public record LinkPlanDto(int Length, double Efficiency, double Throughput, double Gold, IReadOnlyList<ResourceRateDto> Materials, string? Error, IReadOnlyList<int> Route);

/// <summary>A link on the map: its route (x, y pairs) along the corridors</summary>
public record MapLinkDto(long Id, string Resource, int Tier, bool Flowing, long Seller, long Buyer, IReadOnlyList<int> Route);

/// <summary>The "This week" row while the station builds its project (F4): "🏗 Water Tower 12% · wants sand · Ben can"</summary>
/// <summary>The station's project in one line (This week, the return card, the Project tab), and the step its Show me takes</summary>
public record ProjectWeekDto(string Text, double Share, NextStepDto? Step = null);

// ---------------------------------------------------------------- Statistics (docs/design-notes/statistics.md)

/// <summary>
/// A company's statistics over a range: its samples as columns (a key per series, a row per time, null where a sample
/// lacks a series; the last row is now), its key moments, its town's samples and who it can be compared with
/// </summary>
public record StatisticsDto(
    long Company,
    string Name,
    string Color,
    DateTime Founded,
    bool Own,
    string Range,
    DateTime From,
    DateTime Now,
    IReadOnlyList<string> Keys,
    IReadOnlyList<DateTime> At,
    IReadOnlyList<IReadOnlyList<double?>> Rows,
    IReadOnlyList<MomentDto> Moments,
    StatsTownDto Town,
    IReadOnlyList<StatsPeerDto> Peers);

/// <summary>A key moment: big ones are flags on every graph; ref is a good or building for its icon</summary>
public record MomentDto(DateTime At, string Kind, string Text, bool Big, string? Ref);

public record StatsTownDto(string Name, IReadOnlyList<string> Keys, IReadOnlyList<DateTime> At, IReadOnlyList<IReadOnlyList<double?>> Rows);

/// <summary>Someone to compare with: your crew first, then your town</summary>
public record StatsPeerDto(long Id, string Name, string Color, bool Crew);

/// <summary>One series of a company over a range (to compare with)</summary>
public record StatsSeriesDto(long Company, string Name, string Color, DateTime Founded, string Key, IReadOnlyList<DateTime> At, IReadOnlyList<double?> Values);

/// <summary>The admin's Pace: every company's gold earned by days since it was founded, and the model's regular player</summary>
public record PaceDto(IReadOnlyList<PaceLineDto> Companies, IReadOnlyList<double> Model);

public record PaceLineDto(long Id, string Name, string Color, DateTime Founded, IReadOnlyList<double> Days, IReadOnlyList<double> Earned);
