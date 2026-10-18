// The contract with the server (TransportPipes.Server/Dtos/GameDtos.cs and GameViews.Content). camelCase JSON, enums as
// camelCase strings.

export interface VersionInfo {
  version: string;
  commit: string | null;
}

export interface UserInfo {
  id: number;
  userName: string;
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
  serverTime: string;
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
}

export interface HelloDto {
  user: UserInfo;
  company: CompanyDto | null;
  report: ReportDto | null;
  serverTime: string;
  era: number;
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
}

export interface HubStructureDto {
  type: string;
  x: number;
  y: number;
  size: number;
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
}

export interface ToastDto {
  kind: "constructionFinished" | "levelUp" | "researchFinished" | "priceChanged" | "orderCompleted";
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
  unlock: { level: number; era: number; research: string | null };
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
