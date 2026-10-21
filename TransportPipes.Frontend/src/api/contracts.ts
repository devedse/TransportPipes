// The contract with the server (TransportPipes.Server/Dtos/GameDtos.cs and GameViews.Content). camelCase JSON, enums as
// camelCase strings.

export interface VersionInfo {
  version: string;
  commit: string | null;
}

export interface UserInfo {
  id: number;
  userName: string;
  isAdmin: boolean;
  /** Plays without an account yet (the browser keeps its hidden name and password) */
  isGuest: boolean;
}

/** A guest's account: the browser keeps its name and password, so the estate isn't lost with the cookie */
export interface GuestResult {
  success: boolean;
  error: string | null;
  user: UserInfo | null;
  password: string | null;
}

export interface AuthResult {
  success: boolean;
  error: string | null;
  user: UserInfo | null;
}

export interface CommandResult {
  success: boolean;
  error: string | null;
}

export interface CompanyDto {
  id: number;
  name: string;
  color: string;
  gold: number;
  goldPerMinute: number;
  xp: number;
  level: number;
  levelXp: number;
  nextLevelXp: number;
  researchPoints: number;
  researchPerMinute: number;
  lifetimeEarnings: number;
  districtX: number;
  districtY: number;
  slot: number;
  estateX: number;
  estateY: number;
  unlockedParcels: number;
  nextParcelCost: number | null;
  nextParcelLevel: number | null;
  buildingCounts: Record<string, number>;
  researched: string[];
  currentResearch: string | null;
  researchEndsAt: string | null;
  hubReputation: number;
  reputationLevel: number;
  nextReputation: number | null;
  dailyOrders: DailyOrderDto[];
  noTrade: string[];
  corporationId: number | null;
  corporationTag: string | null;
  corporationRole: CorporationRole | null;
  mentorId: number | null;
  likes: number;
  /** Running events and the ones within the forecast */
  events: GameEventDto[];
  forecastHours: number;
  era: number;
  eraDeadline: string;
  titles: string[];
  /** Finished collections (they unlock decorations) */
  collections: string[];
  outposts: OutpostDto[];
  franchise: FranchiseDto;
  season: SeasonInfoDto;
  moving: MovingDto;
  deep: DeepDto;
  /** Branch estates opened by franchising */
  branches: BranchDto[];
  /** Loady's guide: the current step (null when done) */
  tutorial: TutorialDto | null;
  expedition: ExpeditionDto;
  mastery: { level: number; available: boolean; cost: number; minutes: number; bonus: number };
  /** What holds the company back right now */
  alerts: Array<{ text: string; building: number | null }>;
  serverTime: string;
  /** XP per minute from earnings, where the XP came from (per source per item) and what gives XP */
  experience: {
    perMinute: number;
    log: Record<string, Record<string, number>>;
    rules: Array<{ source: string; text: string }>;
  };
  /** Who it trades with on its pipes */
  tradeWith: "everyone" | "corporation" | "contracts";
  /** The company's crew (v1.2 §6) */
  crew: CrewBadgeDto | null;
  /** The XP multiplier while far behind the people you live with (v1.2 §10) */
  tailwind: number;
  /** The lone-human card waits: nobody else in the station played lately */
  loneCard: boolean;
  /** The league it plays in this week (v1.2 §13; none in small mode) */
  league: number | null;
}

export type ActivityStage = "active" | "resting" | "dormant" | "asleep";

/** Moving and activity (GDD §12) */
export interface MovingDto {
  activity: ActivityStage;
  asleep: boolean;
  arrivesAt: string | null;
  crates: CrateDto[];
  freeMoves: number;
  moveCost: number;
  canMoveAt: string | null;
  swapWith: number | null;
}

export interface CrateDto {
  index: number;
  type: string;
  level: number;
}

export interface DistrictChoiceDto {
  x: number;
  y: number;
  name: string;
  freeEstates: number;
  players: number;
  activeShare: number;
  reservedForYou: boolean;
  yours: boolean;
}

export interface SleeperDto {
  id: number;
  name: string;
  color: string;
  level: number;
  since: string;
}

/** The Franchise (GDD §8.7) */
export interface FranchiseDto {
  shares: number;
  franchises: number;
  sharesNow: number;
  blocker: string | null;
  at: string | null;
  legacyLevel: number;
}

export interface SeasonInfoDto {
  season: number;
  finaleAt: string | null;
  endedAt: string | null;
  nextWorldAt: string | null;
}

export interface SeasonResultDto {
  company: string;
  netWorth: number;
  rank: number;
  legacy: number;
  title: string | null;
  you: boolean;
}

export interface SeasonDto {
  season: number;
  finaleAt: string | null;
  endedAt: string | null;
  nextWorldAt: string | null;
  top: SeasonResultDto[];
  you: SeasonResultDto | null;
}

export type EventKind =
  | "storm"
  | "heatwave"
  | "drought"
  | "coldSnap"
  | "expo"
  | "shortage"
  | "discovery"
  | "festival"
  | "eraLaunch";

/** An event in the calendar (GDD §15) */
export interface GameEventDto {
  id: number;
  kind: EventKind;
  name: string;
  effect: string;
  startsAt: string;
  endsAt: string;
  resource: string | null;
  active: boolean;
  everywhere: boolean;
}

export interface DailyOrderDto {
  id: number;
  resource: string;
  amount: number;
  delivered: number;
  rewardGold: number;
  rewardXp: number;
  rewardReputation: number;
  expiresAt: string;
  /** A Shortage order: bigger, better paid, gone when the event ends */
  urgent: boolean;
}

/**
 * One resource at the hub of the player's district: what the hub pays the player (its own Hub Order), what it charges,
 * what the player sold there lately against the order, the one price between players, and the trade settings
 */
export interface MarketResourceDto {
  resource: string;
  basePrice: number;
  /** What the hub pays you now (your own order: nobody else's sales move it) */
  buyPrice: number;
  /** What the hub charges */
  sellPrice: number;
  /** What you sold there per minute lately, and its order: up to it the full price */
  soldPerMinute: number;
  demand: number;
  /** The one price between players (the Yard, contracts, the Exchange) */
  playerPrice: number;
  noTrade: boolean;
  /** Not bought from other companies */
  noBuy: boolean;
  /** Market Insight: where the hub's buy price is heading */
  heading: number | null;
}

export interface MarketDto {
  districtX: number;
  districtY: number;
  district: string;
  resources: MarketResourceDto[];
  nextUpdate: string;
}

export interface ResearchStateDto {
  id: string;
  state: "done" | "researching" | "available" | "locked";
  cost: number;
  reason: string | null;
  /** Share of the other active companies that have it (0..1) */
  diffusion: number;
}

export interface ReportDto {
  since: string;
  goldEarned: number;
  goldSpent: number;
  sold: Record<string, number>;
  produced: Record<string, number>;
  levelsGained: number;
  buildingsCompleted: number;
  notes: string[];
  researchDone: string[];
  contracts: string[];
  newNeighbours: string[];
  ordersCompleted: number;
  likesReceived: number;
  fullStorages: FullStorageDto[];
}

/** A full storage in the offline report, with a one-tap upgrade */
export interface FullStorageDto {
  building: number;
  type: string;
  level: number;
  capacity: number;
  upgradeCost: number | null;
}

export interface HelloDto {
  user: UserInfo;
  company: CompanyDto | null;
  report: ReportDto | null;
  serverTime: string;
  era: number;
  unread: number;
  /** The way back after three or more days away (v1.2 §10) */
  return: ReturnDto | null;
}

/** The way back: what happened, that the estate is safe, and where it can land */
export interface ReturnDto {
  daysAway: number;
  earned: number;
  caretakerFee: number;
  tendedDays: number;
  standIn: string | null;
  wasAsleep: boolean;
  fromQuietIsles: boolean;
  station: string;
  links: number;
  crates: number;
  landing: LandingCardDto[];
}

/** A place to land: your seat, next to your crew, a lively station, the Frontier */
export interface LandingCardDto {
  kind: "yours" | "crew" | "lively" | "frontier";
  seat: SeatRefDto;
  station: string;
  crates: number;
  here: boolean;
  neighbours: number;
}

export interface BuildingDto {
  id: number;
  type: string;
  owner: number;
  x: number;
  y: number;
  size: number;
  level: number;
  constructionEndsAt: string | null;
  network: number | null;
  /** Storm damage: half speed until repaired */
  damaged: boolean;
  /** A teleporter that sends: the station it sends to */
  link?: number | null;
}

export type ConductorKind = "pipe" | "gate";

export interface ConductorDto {
  x: number;
  y: number;
  kind: ConductorKind;
  tier: number;
  owner: number;
  network: number | null;
}

export interface TerrainOverrideDto {
  x: number;
  y: number;
  terrain: TerrainType;
}

export interface ChunkObjectsDto {
  cx: number;
  cy: number;
  buildings: BuildingDto[];
  conductors: ConductorDto[];
  terrainOverrides: TerrainOverrideDto[];
  outposts: OutpostDto[];
  /** Deposits that differ from the generated terrain (Discoveries) */
  deposits: DepositOverrideDto[];
}

/** An outpost claim in the wilderness (lease details only for its owner) */
export interface OutpostDto {
  id: number;
  owner: number;
  ownerName: string;
  ownerColor: string;
  x: number;
  y: number;
  size: number;
  lease: number | null;
  paidUntil: string | null;
}

export interface DepositOverrideDto {
  x: number;
  y: number;
  deposit: string;
  richness: number;
}

export interface OutpostCheckDto {
  error: string | null;
  /** The best claim covering the tile */
  x: number;
  y: number;
  lease: number;
  size: number;
  deposit: string | null;
  richTiles: number;
}

export interface ChunkDto {
  cx: number;
  cy: number;
  /** Base64, one byte per tile (row by row): the terrain index */
  terrain: string;
  deposits: string;
  richness: string;
  /** Base64 bytes: wind and sun × 100 */
  wind: string;
  sun: string;
  depositNames: string[];
  objects: ChunkObjectsDto;
}

export interface EstateDto {
  slot: number;
  x: number;
  y: number;
  ownerId: number | null;
  ownerName: string | null;
  ownerColor: string | null;
  ownerLevel: number;
  unlockedParcels: number;
  primary: string;
  secondary: string | null;
  corporationTag: string | null;
  /** The owner's activity stage (players only) */
  activity: ActivityStage | null;
  /** A corporation block: the tag of the corporation it is kept for */
  reservedFor: string | null;
}

/** A hub structure; corporation HQs have the corporation's tag, colour and HQ level */
export interface HubStructureDto {
  type: string;
  x: number;
  y: number;
  size: number;
  label: string | null;
  color: string | null;
  level: number;
  corporation: number | null;
  /** Wonders: the Wonder's id and how many stages it has (level = stages built) */
  variant: string | null;
  stages: number;
}

export interface DistrictDto {
  x: number;
  y: number;
  originX: number;
  originY: number;
  name: string;
  estates: EstateDto[];
  hub: HubStructureDto[];
}

export type FlowKind = "internal" | "trade" | "toStorage" | "fromStorage" | "sale" | "import";

export interface FlowDto {
  from: number;
  to: number;
  resource: string;
  sent: number;
  delivered: number;
  kind: FlowKind;
  price: number;
  /** Tiles as x0, y0, x1, y1, ... */
  path: number[];
}

export interface NetworkFlowsDto {
  id: number;
  rates: Record<string, number>;
  flows: FlowDto[];
}

export interface ResourceAmountDto {
  resource: string;
  amount: number;
  max: number;
}

export interface BuildingFlowDto {
  direction: "in" | "out";
  resource: string;
  amount: number;
  partner: number;
  partnerType: string;
  partnerOwner: string;
  kind: FlowKind;
  price: number;
}

export interface BuildingDetailsDto {
  building: BuildingDto;
  ownerName: string;
  rate: number;
  locationMultiplier: number;
  inputs: ResourceAmountDto[];
  outputs: ResourceAmountDto[];
  researchPerMinute: number;
  contents: Record<string, number> | null;
  capacity: number | null;
  throughput: number | null;
  imports: string[] | null;
  filter: string[] | null;
  pumpFactor: number;
  upgradeCost: number | null;
  upgradeCost10: number | null;
  maxAffordableLevels: number;
  upgradeCostMax: number | null;
  flows: BuildingFlowDto[];
  demolishRefund: number;
  issues: string[];
  setting: number;
  repairCost: number | null;
  /** A complex: how many buildings it merged (1 for an ordinary building) */
  complexMembers: number;
  teleporter: TeleporterDto | null;
  /** What it can evolve into where it stands */
  evolve: { into: string; cost: number; problem: string | null } | null;
  /** A storage: what it keeps of each resource, and whether a full trading post sends it the rest */
  keep: { keeps: Record<string, number>; tradingPostFull: boolean } | null;
}

/** A teleporter station (GDD §11.4) */
export interface TeleporterDto {
  role: "sender" | "receiver" | null;
  partner: number | null;
  resource: string | null;
  distance: number;
  perMinute: number;
  throughput: number;
  energyPerUnit: number;
  loss: number;
  range: string;
  candidates: Array<{ id: number; x: number; y: number; distance: number; problem: string | null }>;
}

/** The kinds of WorldEventKind on the server */
export type ToastKind =
  | "constructionFinished"
  | "levelUp"
  | "researchFinished"
  | "priceChanged"
  | "orderCompleted"
  | "contractChanged"
  | "exchangeTrade"
  | "corporation"
  | "social"
  | "storageFull"
  | "neighbour"
  | "calendar"
  | "wonder"
  | "achievement";

export interface ToastDto {
  kind: ToastKind;
  text: string;
  building: number | null;
}

// ---------------------------------------------------------------- Content (/api/content)

export type TerrainType =
  | "grass"
  | "hills"
  | "forest"
  | "beach"
  | "water"
  | "desert"
  | "mountain"
  | "road"
  | "city"
  | "yard"
  | "rock"
  | "cavern";

export type BuildingKind =
  | "producer"
  | "storage"
  | "tradingPost"
  | "lab"
  | "hq"
  | "gate"
  | "valve"
  | "priorityValve"
  | "pump"
  | "tollMeter"
  | "cargoDepot"
  | "exchangeOffice"
  | "showroom"
  | "workshop"
  | "weatherStation"
  | "recycler"
  | "expeditionOffice"
  | "blueprintOffice"
  | "deepLift"
  | "bank"
  | "teleporter"
  | "terraformer"
  | "powerLine"
  | "corporationHq"
  | "decoration";

export interface ResourceDefinition {
  id: string;
  name: string;
  icon: string;
  color: string;
  tier: number;
  era: number;
  price: number;
}

export interface BuildingDefinition {
  id: string;
  name: string;
  category: string;
  kind: BuildingKind;
  size: number;
  buildable: boolean;
  unlock: { level: number; era: number; research: string | null; collection: string | null };
  cost: { gold: number; materials: Record<string, number> };
  buildSeconds: number;
  in: Record<string, number>;
  out: Record<string, number>;
  outFromDeposit: string[];
  researchPerMinute: number;
  placement: {
    terrain: TerrainType[];
    deposit: string | null;
    shore: boolean;
    forest: boolean;
    minRichness: number;
  };
  modifier: "none" | "wind" | "sun" | "richness" | "forest";
  capacity: number;
  throughput: number;
  priceBonus: number;
  range: string | null;
  /** Power line pylons: how far the cable reaches */
  reach: number;
  layer: "surface" | "deep" | "any";
  evolvesTo: string | null;
  sprite: string | null;
  description: string;
}

export interface PipeTierDefinition {
  id: string;
  name: string;
  tier: number;
  era: number;
  gold: number;
  materials: Record<string, number>;
  lossPerTile: number;
  throughput: number;
  research: string | null;
}

export type ResearchBranch = "energy" | "industry" | "logistics" | "economy";

export interface ResearchDefinition {
  id: string;
  name: string;
  branch: ResearchBranch;
  era: number;
  level: number;
  cost: number;
  minutes: number;
  requires: string[];
  outputBonuses: Array<{ targets: string[]; amount: number }>;
  throughputBonus: number;
  bandWidthBonus: number;
  managerSlots: number;
  description: string;
}

export interface GameContentDto {
  resourceList: ResourceDefinition[];
  relics: Array<{ id: string; name: string; lore: string }>;
  buildings: BuildingDefinition[];
  pipeTiers: PipeTierDefinition[];
  research: ResearchDefinition[];
  progression: {
    maxCompanyLevel: number;
    xpForLevel: number[];
    xpRewards: Record<string, number>;
    maxBuildingLevel: number;
    outputPerLevel: number;
    costPerLevel: number;
    milestones: number[];
    costPerCopy: number;
    startParcels: number[];
    parcelLevels: number[];
    parcelCosts: number[];
    hubSpread: number;
  };
  layout: {
    estateSize: number;
    corridorWidth: number;
    blocksPerSide: number;
    parcelSize: number;
    parcelsPerSide: number;
    wildernessWidth: number;
    chunkSize: number;
    blockPitch: number;
    districtSize: number;
    districtPitch: number;
    hubBlock: number;
    originOffset: number;
    yardSize: number;
    /** Every slot's Yard gate, estate-local */
    yardGates: Array<{ x: number; y: number }>;
    /** The stations of a district: their Yard (district-local) and seats */
    stations: Array<{ yard: { x0: number; y0: number; x1: number; y1: number }; slots: number[] }>;
    stationOfSlot: number[];
    terrain: TerrainType[];
    richnessMultiplier: number[];
  };
}

export interface NetworkResourceDto {
  resource: string;
  produced: number;
  used: number;
  sold: number;
  imported: number;
  traded: number;
  stored: number;
  fromStorage: number;
  lost: number;
  /** What its buildings would use at full speed */
  needed: number;
}

export interface NetworkDetailsDto {
  id: number;
  buildings: number;
  pipes: number;
  pumps: number;
  pumpFactor: number;
  owners: Array<{
    id: number;
    name: string;
    color: string;
    goldPerMinute: number;
    /** What its buildings in the network make, use and sell, per minute */
    resources: Array<{ resource: string; made: number; used: number; sold: number }>;
    /** Gold per minute: from the hub and from neighbours, paid to neighbours and to the hub */
    hubSales: number;
    neighbourSales: number;
    neighbourPurchases: number;
    hubPurchases: number;
  }>;
  resources: NetworkResourceDto[];
  bottlenecks: Array<{ a: number; b: number; capacity: number; used: number; path: number[] }>;
}

export interface ResourceRateDto {
  resource: string;
  perMinute: number;
}

export interface NeighbourDto {
  id: number;
  name: string;
  color: string;
  level: number;
  contractStars: number;
  isNpc: boolean;
  slot: number;
  estateX: number;
  estateY: number;
  /** Their pipes reach their Yard gate (they trade with the station) */
  connected: boolean;
  surplus: ResourceRateDto[];
  needs: ResourceRateDto[];
  /** What each building that misses something misses: it buys only when all of it comes */
  shortfalls: string[][];
  youEarn: number;
  youPay: number;
  /** The best pipe from your buildings to theirs: how much of what is sent arrives, and how many tiles long */
  linkEfficiency: number | null;
  pipeTiles: number | null;
  /** You don't trade with them */
  blocked: boolean;
}

export interface ContractDto {
  id: number;
  proposer: number;
  proposerName: string;
  seller: number;
  sellerName: string;
  buyer: number;
  buyerName: string;
  resource: string;
  rate: number;
  price: number;
  days: number;
  state: "offered" | "active" | "ended" | "declined" | "cancelled";
  createdAt: string;
  endsAt: string | null;
  delivered: number;
  expected: number;
  proposerStars: number;
}

export interface ContractsDto {
  mine: ContractDto[];
  board: ContractDto[];
}

export interface ExchangeOrderDto {
  id: number;
  resource: string;
  selling: boolean;
  amount: number;
  price: number;
  createdAt: string;
  mine: boolean;
  global: boolean;
}

export interface ShipmentDto {
  id: number;
  resource: string;
  amount: number;
  arrivesAt: string;
  incoming: boolean;
  partner: string;
}

export interface ExchangeBookDto {
  resource: string;
  bestBid: number;
  bidAmount: number;
  bestAsk: number;
  askAmount: number;
}

export interface ExchangeDto {
  hasOffice: boolean;
  books: ExchangeBookDto[];
  orders: ExchangeOrderDto[];
  shipments: ShipmentDto[];
  fee: number;
  /** Global Exchange: orders trade with every hub */
  global: boolean;
}

// ---------------------------------------------------------------- Society

export type CorporationRole = "member" | "officer" | "leader";

export interface CorporationSummaryDto {
  id: number;
  name: string;
  tag: string;
  color: string;
  description: string;
  members: number;
  hqLevel: number;
  weeklyEarnings: number;
  applied: boolean;
  invited: boolean;
}

export interface CorporationMemberDto {
  id: number;
  name: string;
  color: string;
  level: number;
  role: CorporationRole;
  weeklyEarnings: number;
  contribution: number;
  online: boolean;
}

export interface CorporationGoalDto {
  resource: string;
  amount: number;
  delivered: number;
  rewardGold: number;
  endsAt: string;
  yourContribution: number;
  yourStock: number;
  rewarded: boolean;
}

export interface CorporationPerkDto {
  level: number;
  text: string;
  active: boolean;
}

export interface CorporationDto {
  id: number;
  name: string;
  tag: string;
  color: string;
  description: string;
  treasury: number;
  tax: number;
  hqLevel: number;
  nextHqCost: number | null;
  perkResource: string | null;
  yourRole: CorporationRole;
  members: CorporationMemberDto[];
  applications: CorporationMemberDto[];
  goal: CorporationGoalDto | null;
  perks: CorporationPerkDto[];
}

export interface CorporationsDto {
  mine: CorporationDto | null;
  district: CorporationSummaryDto[];
  foundLevel: number;
  foundCost: number;
  maxMembers: number;
  canJoinAt: string | null;
}

export type LeaderboardKind =
  "netWorth" | "income" | "weeklyEarnings" | "level" | "partners" | "likes" | "efficiency" | "league";

export interface LeaderboardEntryDto {
  company: number;
  name: string;
  color: string;
  value: number;
  rank: number;
  you: boolean;
}

export interface LeaderboardDto {
  kind: LeaderboardKind;
  district: boolean;
  entries: LeaderboardEntryDto[];
  you: LeaderboardEntryDto | null;
}

export interface GuestbookNoteDto {
  id: number;
  author: number;
  authorName: string;
  text: string;
  at: string;
}

/** An estate as a visitor sees it */
export interface EstateVisitDto {
  id: number;
  name: string;
  color: string;
  level: number;
  isNpc: boolean;
  isYou: boolean;
  likes: number;
  likedToday: boolean;
  corporationTag: string | null;
  corporationName: string | null;
  netWorth: number;
  buildings: number;
  partners: number;
  createdAt: string;
  estateX: number;
  estateY: number;
  canChooseAsMentor: boolean;
  isYourMentor: boolean;
  mentees: number;
  guestbook: GuestbookNoteDto[];
  swapFromThem: boolean;
  swapFromYou: boolean;
}

export interface ChatMessageDto {
  id: number;
  channel: string;
  sender: number;
  senderName: string;
  text: string;
  sentAt: string;
}

export type ChatChannelKind = "district" | "corporation" | "direct";

export interface ChatChannelDto {
  id: string;
  name: string;
  kind: ChatChannelKind;
  lastAt: string | null;
}

export interface ChatDto {
  channels: ChatChannelDto[];
  blocked: number[];
}

export interface InboxMessageDto {
  id: number;
  kind: string;
  text: string;
  at: string;
  read: boolean;
}

export interface InboxDto {
  messages: InboxMessageDto[];
  unread: number;
}

// ---------------------------------------------------------------- Wonders and Eras

export interface WonderNeedDto {
  resource: string;
  required: number;
  delivered: number;
  yourStock: number;
}

export interface WonderBuilderDto {
  company: number;
  name: string;
  color: string;
  value: number;
}

export type WonderTier = "none" | "bronze" | "silver" | "gold" | "diamond";

export interface WonderDto {
  type: string;
  name: string;
  description: string;
  isEra: boolean;
  stage: number;
  stages: number;
  finished: boolean;
  needs: WonderNeedDto[];
  total: number;
  yourContribution: number;
  yourTier: WonderTier;
  yourShare: number;
  topBuilders: WonderBuilderDto[];
}

export interface WondersDto {
  era: number;
  eraStartedAt: string;
  eraDeadline: string;
  district: WonderDto | null;
  eraWonder: WonderDto | null;
  districtName: string;
}

// ---------------------------------------------------------------- Company: managers, achievements, statistics

export interface ManagerDto {
  id: string;
  name: string;
  cost: number;
  color: string;
  automation: string;
  bonus: string;
  hired: boolean;
}

export interface AchievementDto {
  id: string;
  name: string;
  description: string;
  amount: number;
  value: number;
  done: boolean;
  xp: number;
  gold: number;
  title: string | null;
}

export interface CollectionDto {
  id: string;
  name: string;
  description: string;
  level: number;
  buildings: string[];
  done: string[];
  finished: boolean;
  reward: string | null;
}

export interface StatDto {
  name: string;
  value: number;
}

export interface CompanyDetailsDto {
  managerSlots: number;
  managers: ManagerDto[];
  achievements: AchievementDto[];
  collections: CollectionDto[];
  titles: string[];
  stats: StatDto[];
  produced: StatDto[];
}

// ---------------------------------------------------------------- Moderation

export interface ReportedMessageDto {
  id: number;
  channel: string;
  sender: number;
  senderName: string;
  text: string;
  sentAt: string;
}

// ---------------------------------------------------------------- Blueprints

export type BlueprintSharing = "private" | "corporation" | "board";

export interface BlueprintDto {
  id: number;
  name: string;
  owner: number;
  ownerName: string;
  ownerColor: string;
  width: number;
  height: number;
  buildings: Array<{ type: string; dx: number; dy: number }>;
  pipes: Array<{ dx: number; dy: number; tier: number }>;
  sharing: BlueprintSharing;
  rating: number;
  ratings: number;
  myRating: number | null;
  cost: number;
  createdAt: string;
}

export interface BlueprintsDto {
  hasOffice: boolean;
  canRebuild: boolean;
  max: number;
  mine: BlueprintDto[];
  corporation: BlueprintDto[];
  board: BlueprintDto[];
}

// ---------------------------------------------------------------- The Deep Level

/** The Deep Level (GDD §11.2): deep tiles are surface tiles shifted down by `offset` */
export interface DeepDto {
  open: boolean;
  /** Dug out parcels, a bit per parcel */
  parcels: number;
  digParcel: number | null;
  digStartedAt: string | null;
  digEndsAt: string | null;
  nextCost: number;
  nextHours: number;
  offset: number;
}

/** A branch estate (GDD §8.7): its index for BuyParcel is its place in CompanyDto.branches plus one */
export interface BranchDto {
  districtX: number;
  districtY: number;
  districtName: string;
  slot: number;
  x: number;
  y: number;
  unlockedParcels: number;
  nextParcelCost: number;
  nextParcelLevel: number;
}

/** A step of Loady's guide; the hint says what "Show me" does (tool, building, overlay, select, panel) */
export interface TutorialDto {
  step: number;
  steps: number;
  id: string;
  text: string;
  progress: number;
  amount: number;
  gold: number;
  hint: Record<string, string>;
  /** The Company Level the step needs first (null when it can be done now) */
  waitLevel: number | null;
  /** Why it isn't done yet although the player did what it says */
  tip: string | null;
  /** Chapter 1: the first session; 2: the middle game. chapterStep is 1-based within it. */
  chapter: number;
  chapterStep: number;
  chapterSteps: number;
}

/** Expeditions (GDD §11.3) */
export interface ExpeditionDto {
  hours: number | null;
  endsAt: string | null;
  costs: Record<string, number>;
  blocker: string | null;
  log: string[];
  relics: string[];
}

/** One of the advisor's next steps (GameWorld.NextSteps): x, y is where Show me looks */
export interface NextStepDto {
  kind:
    | "shortage"
    | "pump"
    | "full"
    | "pipes"
    | "research"
    | "upgrade"
    | "chain"
    | "partner"
    | "parcel"
    | "start";
  title: string;
  reason: string;
  action: "upgrade" | "build" | "select" | "research" | "parcel" | "partner" | "pipes" | "connect";
  cost: number;
  gainPerMinute: number;
  paybackMinutes: number | null;
  waitMinutes: number | null;
  building: number | null;
  buildingType: string | null;
  levels: number;
  parcel: number | null;
  partner: number | null;
  x: number | null;
  y: number | null;
  /** A research step: the tech */
  research: string | null;
  /** A build step: how many */
  count: number;
  /** A pipes step: the network, its full pipe tiles (x, y pairs) and the tier they become */
  network: number | null;
  tiles: number[];
  tier: number;
}

// ---------------------------------------------------------------- The Yard (v1.2 §7)

/** A resident of a station: whether its pipes reach its Yard gate, what it has spare (it goes to the hub) and lacks */
export interface YardResidentDto {
  id: number;
  name: string;
  color: string;
  level: number;
  isNpc: boolean;
  slot: number;
  connected: boolean;
  activity: ActivityStage | null;
  spare: ResourceRateDto[];
  lacks: ResourceRateDto[];
  blocked: boolean;
  /** A House's kind (v1.2 §9) */
  house: "reliable" | "collector" | "grower" | "neighbourInNeed" | null;
  /** The House standing in for an asleep player */
  standIn: string | null;
  /** The Plan board's pins: what it makes and needs (v1.2 §12) */
  makes: string[];
  needs: string[];
  asks: AskDto[];
}

/** An Ask: a good asked of the station, how much came, until when */
export interface AskDto {
  resource: string;
  amount: number;
  got: number;
  until: string;
  renew: boolean;
  perMinute: number;
}

/** One of your trades through the Yard: with whom, what, how much per minute, which way, at what price */
export interface YardTradeDto {
  partner: number;
  partnerName: string;
  resource: string;
  perMinute: number;
  selling: boolean;
  price: number;
}

/** Your station and its Yard */
export interface YardDto {
  station: string;
  small: boolean;
  x0: number;
  y0: number;
  x1: number;
  y1: number;
  gateX: number;
  gateY: number;
  /** Your own pipes reach your Yard gate */
  connected: boolean;
  residents: YardResidentDto[];
  yours: YardTradeDto[];
  /** What goes through the Yard per minute */
  totals: ResourceRateDto[];
  line: string;
  discoveries: DiscoveryDto[];
}

// ---------------------------------------------------------------- Crews and seats (v1.2 §4–6)

export type SeatState = "free" | "house" | "held" | "taken" | "you";

/** A seat of a station as the picker shows it */
export interface SeatDto {
  slot: number;
  estateX: number;
  estateY: number;
  primary: string;
  secondary: string | null;
  state: SeatState;
  ownerId: number | null;
  ownerName: string | null;
  ownerColor: string | null;
  ownerLevel: number;
  /** For strangers: lively, some days or resting */
  activity: string | null;
  crewName: string | null;
  heldForYou: boolean;
}

export interface StationDto {
  districtX: number;
  districtY: number;
  index: number;
  name: string;
  small: boolean;
  /** Strangers may settle (a station where one crew lives in or holds 3+ seats is closed) */
  open: boolean;
  free: number;
  lively: number;
  seats: SeatDto[];
  crewName: string | null;
  x0: number;
  y0: number;
  x1: number;
  y1: number;
  /** The project's stage and progress ("Water Tower · 40%") */
  project: string | null;
  /** The Plan board: what its players pinned they make and need */
  makes: string[] | null;
  needs: string[] | null;
}

export interface SeatRefDto {
  districtX: number;
  districtY: number;
  slot: number;
}

export interface StationsDto {
  districtX: number;
  districtY: number;
  district: string;
  stations: StationDto[];
  /** The seats the game suggests for each way of starting */
  neighbours: SeatRefDto;
  ownStation: SeatRefDto;
  friends: SeatRefDto;
  /** The open districts, nearest the Capital first */
  districts: DistrictRefDto[];
}

export interface DistrictRefDto {
  districtX: number;
  districtY: number;
  name: string;
}

/** What a seat code or crew link opens */
export interface InviteDto {
  valid: boolean;
  crewName: string | null;
  emblem: string | null;
  color: string | null;
  color2: string | null;
  from: string | null;
  station: StationDto | null;
  /** The seat a seat code keeps */
  slot: number | null;
  seatCode: boolean;
  /** An old crew link: a member lets you in */
  needsApproval: boolean;
  /** A company name nobody uses yet */
  name: string | null;
}

export interface CrewMemberDto {
  id: number;
  name: string;
  color: string;
  level: number;
  activity: string | null;
  station: string;
  slot: number;
  you: boolean;
  /** You may tend their estate today (v1.2 §12) */
  canTend: boolean;
  tended: boolean;
  asleep: boolean;
}

/** An asleep crew mate's seat whose hold ends soon: keep it 60 more days? */
export interface KeepCardDto {
  id: number;
  name: string;
  ends: string;
}

export interface HeldSeatDto {
  code: string;
  districtX: number;
  districtY: number;
  slot: number;
  primary: string;
  secondary: string | null;
  until: string;
  /** A seat kept for a member (a crew move, an asleep friend's seat) */
  for: number | null;
  forName: string | null;
  station: string | null;
}

export interface CrewDto {
  id: number;
  name: string;
  emblem: string;
  color: string;
  color2: string;
  members: CrewMemberDto[];
  holds: HeldSeatDto[];
  inviteCode: string;
  inviteOpenUntil: string;
  requests: CrewMemberDto[];
  stationOpen: boolean;
  stationClosed: boolean;
  maxSize: number;
  keepCards: KeepCardDto[];
  consolidationCard: boolean;
  /** Where the crew could move together */
  moveTo: string | null;
}

export interface CrewViewDto {
  crew: CrewDto | null;
  emblems: string[];
}

export interface FoundCrewResultDto {
  success: boolean;
  error: string | null;
  holds: HeldSeatDto[];
}

/** A company's crew as the top bar and the map show it */
export interface CrewBadgeDto {
  id: number;
  name: string;
  emblem: string;
  color: string;
  color2: string;
  members: number;
}

/**
 * A link (v1.2 §7) as one of its ends sees it: the good, the partner, the flow, the tank at the buyer's gate, the
 * notice, and one state line that always says what happens
 */
export interface LinkDto {
  id: number;
  partnerId: number;
  partnerName: string;
  partnerColor: string;
  resource: string;
  youSell: boolean;
  youOffered: boolean;
  state: "offered" | "active" | "paused";
  tier: number;
  length: number;
  /** The share of what is sent that arrives (the pipe's loss over the route) */
  efficiency: number;
  throughput: number;
  price: number;
  /** Per minute: what the buyer needs from the link, what reaches it */
  need: number;
  rate: number;
  tank: number;
  tankSize: number;
  delivered: number;
  noticeAt: string | null;
  noticeEnds: boolean;
  noticeYours: boolean;
  canResume: boolean;
  offerEndsAt: string | null;
  tone: "good" | "info" | "warn";
  line: string;
  /** The route's corners as x, y pairs */
  route: number[];
}

export interface LinkSuggestionDto {
  partnerId: number;
  partnerName: string;
  partnerColor: string;
  station: string;
  resource: string;
  youSell: boolean;
  perMinute: number;
  length: number;
  efficiency: number;
}

export interface LinkPartnerDto {
  id: number;
  name: string;
  color: string;
  level: number;
  station: string;
}

export interface LinksDto {
  links: LinkDto[];
  suggestions: LinkSuggestionDto[];
  max: number;
  partners: LinkPartnerDto[];
  trunks: TrunksDto;
}

/** Beyond your own hub (v1.2 §13): far sales, your crew's trunks, where a new one could go */
export interface TrunksDto {
  sales: RemoteSaleDto[];
  crew: TrunkDto[];
  targets: TrunkTargetDto[];
  segmentGold: number;
  segmentMaterials: ResourceRateDto[];
  canBuild: boolean;
  blocker: string | null;
}

export interface RemoteSaleDto {
  resource: string;
  market: string;
  perMinute: number;
  price: number;
  public: boolean;
}

export interface TrunkDto {
  id: number;
  target: string;
  length: number;
  segments: number;
  needed: number;
  colors: string[];
  built: boolean;
  efficiency: number;
}

export interface TrunkTargetDto {
  districtX: number | null;
  districtY: number | null;
  port: boolean;
  name: string;
}

export interface LinkPlanDto {
  length: number;
  efficiency: number;
  throughput: number;
  gold: number;
  materials: ResourceRateDto[];
  error: string | null;
  route: number[];
}

/** A link on the map: its route (x, y pairs) along the corridors */
export interface MapLinkDto {
  id: number;
  resource: string;
  tier: number;
  flowing: boolean;
  seller: number;
  buyer: number;
  route: number[];
  /** A link between two companies, or a crew's trunk (its paid segments in their payers' colours) */
  kind: "link" | "trunk";
  colors: string[] | null;
  needed: number;
}

/**
 * A station's project (v1.2 §12): the stage, its basket (with the keystone nobody here makes), who helped (shares, as a
 * ring of colours), what the finished stages give every seat, and your own part
 */
export interface ProjectDto {
  station: string;
  stage: string;
  stageIndex: number;
  stages: string[];
  basket: ProjectGoodDto[];
  helpers: ProjectHelperDto[];
  bonus: number;
  foundryChains: string[];
  opensAt: string | null;
  yourCredit: number;
  yourAllowance: number;
  projectFirst: string[];
  line: string;
}

export interface ProjectGoodDto {
  resource: string;
  target: number;
  delivered: number;
  keystone: boolean;
  madeHere: boolean;
}

export interface ProjectHelperDto {
  color: string;
  share: number;
  you: boolean;
}

/** A Discovery: where, until when its markers are given, which stations got one, the three who brought most */
export interface DiscoveryDto {
  resource: string;
  x: number;
  y: number;
  near: string;
  markingEnds: string;
  stations: string[];
  top: string[];
  yourStation: boolean;
}
