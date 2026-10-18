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
  sellPrices: Record<string, number>;
  buyMaxPrices: Record<string, number>;
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
  serverTime: string;
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

export interface MarketResourceDto {
  resource: string;
  basePrice: number;
  buyPrice: number;
  sellPrice: number;
  soldPerMinute: number;
  demand: number;
  yourPrice: number;
  yourPriceIsAuto: boolean;
  pendingPrice: number | null;
  pendingAt: string | null;
  yourMaxPrice: number;
  noTrade: boolean;
  bandLow: number;
  bandHigh: number;
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
}

export type ConductorKind = "pipe" | "gate";
export type GateSide = "top" | "right" | "bottom" | "left";
export type GatePolicy = "closed" | "corporation" | "contract" | "open";

export interface ConductorDto {
  x: number;
  y: number;
  kind: ConductorKind;
  tier: number;
  owner: number;
  side: GateSide | null;
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
  gatePolicies: Record<string, GatePolicy>;
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
  tollIncome: number;
  repairCost: number | null;
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
  "grass" | "hills" | "forest" | "beach" | "water" | "desert" | "mountain" | "road" | "city";

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
  layer: "surface" | "deep";
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
    gateLocal: Record<GateSide, { x: number; y: number }>;
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
}

export interface NetworkDetailsDto {
  id: number;
  buildings: number;
  pipes: number;
  pumps: number;
  pumpFactor: number;
  owners: Array<{ id: number; name: string; color: string; goldPerMinute: number }>;
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
  gatePolicies: Record<string, GatePolicy>;
  connected: boolean;
  surplus: ResourceRateDto[];
  needs: ResourceRateDto[];
  youEarn: number;
  youPay: number;
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

export type LeaderboardKind = "netWorth" | "income" | "weeklyEarnings" | "level" | "partners" | "likes";

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
