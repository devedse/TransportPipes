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
  /** A failure the client acts on: "seatTaken" (the picker loads the stations again) */
  code?: string | null;
  /** Done, and what the player should know about how (said instead of the client's own success line) */
  note?: string | null;
}

export interface CompanyDto {
  id: number;
  name: string;
  color: string;
  gold: number;
  goldPerMinute: number;
  /** The Company Level (1.4 §14): the gold earned in all it needs, and what the next one needs */
  level: number;
  levelEarned: number;
  nextLevelEarned: number;
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
  research: ResearchLinesDto;
  dailyOrders: DailyOrderDto[];
  noTrade: string[];
  mentorId: number | null;
  likes: number;
  /** Running events and the ones within the forecast */
  events: GameEventDto[];
  forecastHours: number;
  /** Your town's Era, and your own (the highest a town reached while you lived there) */
  era: number;
  yourEra: number;
  titles: string[];
  /** Finished collections (they unlock decorations) */
  collections: string[];
  outposts: OutpostDto[];
  /** The Legacy level the player brought from earlier seasons */
  legacyLevel: number;
  moving: MovingDto;
  deep: DeepDto;
  /** Loady's guide: the current step (null when done) */
  tutorial: TutorialDto | null;
  expedition: ExpeditionDto;
  /** What holds the company back right now */
  /** What holds it back: pumps, post, storage or damage (with what Repair all costs) */
  alerts: Array<{
    text: string;
    building: number | null;
    kind: "pumps" | "post" | "storage" | "damage" | "";
    cost: number | null;
  }>;
  serverTime: string;
  /** The gold earned (1.4 §14): what it earns a minute now, and where it came from (per source per item) */
  earned: {
    perMinute: number;
    log: Record<string, Record<string, number>>;
  };
  /** The company's crew (v1.2 §6) */
  crew: CrewBadgeDto | null;
  /** Your plot's land: what it has, and who in your Yard has the rest */
  land: LandDto | null;
  /** Neighbours' scaffolds and pipes this player could help with */
  wantsNear: WantNearDto[] | null;
  /** This player's own open wants, a building's or a pipe's */
  wants: OwnWantDto[] | null;
  /** The Era Wonder on the goal tracker (after run 3, step 5): its stage's share, and your part or what it wants */
  eraWonderLine: ProjectWeekDto | null;
  /** The pipe ladder on the goal tracker while a pipe is full (after run 4) */
  pipes: PipesLineDto | null;
  /** Your prices for other players (1.4 §8): every one you set, with a rise that waits */
  prices: PriceDto[] | null;
  /** What you give your town's Wonder by itself, per good (1.4 §13) */
  give: Record<string, GiveMode> | null;
  /** Whether the market brings what your levels and buildings lack, at 2.5× (1.4 §9) */
  wantsFromMarket: boolean;
  outskirts: OutskirtsDto | null;
  workshops: WorkshopsDto | null;
  /** The wilderness chunks your expeditions mapped ("x,y"): the rest of it is fog on your map (1.4 §20) */
  explored: string[] | null;
  /** What your own goods going into your wants now would sell for a minute (your income is that much lower until they're in) */
  ownGoodsIntoWants?: number;
  /** What buildings and parcels cost of their price (the Workshop wonder's perk makes them cheaper) */
  costFactor?: number;
  /** When the Yards' next round comes (links, Yard trades and neighbours' goods move then) */
  nextYardRound?: string | null;
  /** What the market charges a minute now for the goods it brings for your levels (paid as it comes) */
  marketForWants?: number;
}

export type GiveMode = "off" | "beyondHubOrder" | "all";

/** Your plot's outskirts (1.4 §5): the parcels you bought (bits 0..8), what the next costs, and why you can't buy one yet */
export interface OutskirtsDto {
  parcels: number;
  nextCost: number;
  blocker: string | null;
}

/** Building in the Yard (1.4 §7): the tiles of its ends your workshops cover, of how many, from which Level; the crew's Depot */
export interface WorkshopsDto {
  tilesUsed: number;
  tiles: number;
  level: number;
  depot: DepotDto | null;
}

/** The Yard's Depot: where it stands, what it holds of how much, and every resident's goods in it */
export interface DepotDto {
  building: number;
  x: number;
  y: number;
  capacity: number;
  holds: number;
  goods: Array<{ resource: string; amount: number; owner: number; ownerName: string; yours: boolean }>;
}

/**
 * The Lab's two lines (1.4 §12): the Era tree's items done, being researched and queued; the tracks' tiers, the tier
 * being researched and the queue (a track once for every tier); whether research starts by itself while you're away,
 * and whether something can be researched now
 */
export interface ResearchLinesDto {
  hasLab: boolean;
  researched: string[];
  currentResearch: string | null;
  researchEndsAt: string | null;
  researchQueue: string[];
  tiers: Record<string, number>;
  currentTrack: string | null;
  trackEndsAt: string | null;
  trackQueue: string[];
  keepResearching: boolean;
  open: boolean;
  /** Research stopped on its way: its id and the minutes of work it still needs (paid already) */
  held?: Record<string, number> | null;
  /** How long the Era tree item being researched takes in all, at the Lab's speed (its progress) */
  researchMinutes?: number | null;
  /** How long the track's tier being researched takes in all */
  trackMinutes?: number | null;
}

/**
 * A track as the research panel shows it: its tiers, the next tier's price (gold, and from tier 3 its good: from your
 * Storages, the rest bought), how long it takes, what it does and about what it adds a minute; why it can't start
 */
export interface TrackStateDto {
  id: string;
  tiers: number;
  next: number | null;
  gold: number;
  good: string | null;
  units: number;
  fromStorages: number;
  bought: number;
  total: number;
  minutes: number;
  effect: TierEffect | null;
  amount: number;
  gain: number;
  hubOrderFactor: number;
  reason: string | null;
}

/**
 * One of your prices: its key ("public", "crew", "public:good", "crew:good", "for:companyId"), what it is now, and a
 * rise that counts at pendingAt (pending null with back: the setting goes); forName names a special price's player
 */
export interface PriceDto {
  key: string;
  value: number;
  pending: number | null;
  back: boolean;
  pendingAt: string | null;
  forName: string | null;
}

/** Your plot's land (1.4 §5): what it has and lacks, its strength, and every plot of your Yard with its land */
export interface LandDto {
  goods: string[];
  lacks: string[];
  /** A ★4 deposit of something it has, next to the start area */
  strength: string;
  station: LandSeatDto[];
}

export interface LandSeatDto {
  slot: number;
  owner: string | null;
  you: boolean;
  goods: string[];
  lacks: string[];
  strength: string;
  /** Of its land goods, those its owner makes now */
  makes: string[];
}

export type ActivityStage = "active" | "resting" | "dormant";

/** Moving and activity (GDD §12) */
export interface MovingDto {
  activity: ActivityStage;
  arrivesAt: string | null;
  crates: CrateDto[];
  freeMoves: number;
  moveCost: number;
  canMoveAt: string | null;
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
  yours: boolean;
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
  expiresAt: string;
  /** A Shortage order: bigger, better paid, gone when the event ends */
  urgent: boolean;
  /** What your trading posts sell of the good a minute now: the order fills from it (after run 3, step 7) */
  selling: number;
  /** What it paid over the hub's price so far */
  premium: number;
  /** What your Storages hold of the good: Deliver fills the order from it at once */
  stored: number;
  /** What the hub pays you for a unit now: the order pays the rest of its price on top */
  hubPrice: number;
}

/**
 * One good at the market of the player's town (1.4 §8): what it pays the player now (its own Hub Order), what it
 * charges, what the player sold there lately against its Hub Order, the default player price, and the trade settings
 */
export interface MarketResourceDto {
  resource: string;
  basePrice: number;
  /** What the market pays you now (your own Hub Order: nobody else's sales move it) */
  buyPrice: number;
  /** What the market charges */
  sellPrice: number;
  /** What you sold there per minute over the last hour */
  soldPerMinute: number;
  /** Your Hub Order: up to it a minute the full price, beyond it the price falls */
  hubOrder: number;
  /** The default price between players (the Yard, the Exchange) */
  playerPrice: number;
  noTrade: boolean;
  /** Not bought from other companies */
  noBuy: boolean;
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
  /** How long it takes you (your Lab's level, managers, the Monument, the Wonder) */
  minutes?: number;
}

export interface HelloDto {
  user: UserInfo;
  company: CompanyDto | null;
  /** The return card (F5): what happened since the last session */
  card: ReturnCardDto | null;
  serverTime: string;
  era: number;
  unread: number;
  /** When the player last left: the chat's "new since you left" */
  leftAt: string | null;
}

/**
 * The return card (F5): one card for every return with news, built on the server's clock. Gold and level, the fine
 * print, at most six lines in their sections, and where to land after the Quiet Isles.
 */
export interface ReturnCardDto {
  /** How long the player was away: "2 days", "5 h 10 min" */
  away: string;
  earned: number;
  levelFrom: number;
  level: number;
  fine: string[];
  lines: ReturnLineDto[];
  hint: string | null;
  /** What folds under "more" (after run 3, row 11: the card leads with what matters) */
  folded: ReturnLineDto[] | null;
  /** What was spent while away: the market's goods for levels, research, managers' buys */
  spent?: number;
}

/** A line of the return card: its section, and its one button with what it costs */
export interface ReturnLineDto {
  kind: "people" | "look" | "project" | "social" | "today" | "invest" | "crew" | "still" | "orders";
  icon: string;
  text: string;
  action: "repairAll" | "show" | "upgrade" | "links" | "grow" | "fillWant" | "orderWant" | null;
  building: number | null;
  cost: number | null;
  partner: number | null;
  resource: string | null;
  /** orderWant: whose want it is (null: your own) */
  partnerName: string | null;
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
  /** A scaffold that wants materials: how much of them is in (0–1); null for any other building */
  siteShare?: number | null;
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

/** An outpost claim in the wilderness (1.4 §20: bought once; its price only for its owner) */
export interface OutpostDto {
  id: number;
  owner: number;
  ownerName: string;
  ownerColor: string;
  x: number;
  y: number;
  size: number;
  price: number | null;
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
  /** What the claim costs, once */
  price: number;
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
  /** The owner's activity stage (players only) */
  activity: ActivityStage | null;
  /** What its land has and lacks, and its strength */
  goods: string[] | null;
  lacks: string[] | null;
  strength: string | null;
  /** The outskirts parcels its owner bought (bits 0..8, 1.4 §5) */
  outskirtsParcels: number;
}

/** A hub structure; a Wonder site has its Wonder's name, stage and variant */
export interface HubStructureDto {
  type: string;
  x: number;
  y: number;
  size: number;
  label: string | null;
  color: string | null;
  level: number;
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
  /** The town's four Yards, by their index (0 NW, 1 NE, 2 SW, 3 SE) */
  yards: DistrictYardDto[];
  /** Where it stands in the race between towns: its Era, its Launch, its stars (missions done) */
  era: number;
  launched: boolean;
  stars: number;
  /** The trunk lines that leave it (1.4 §20) */
  lines: MapLineDto[] | null;
}

/** A trunk line on the map: its route's corners (x, y pairs), its tier and its carrier's colour */
export interface MapLineDto {
  id: number;
  route: number[];
  tier: number;
  color: string;
}

/** A trunk line (1.4 §20) as a player sees it */
export interface TrunkLineDto {
  id: number;
  carrier: string;
  carrierColor: string;
  yours: boolean;
  from: string;
  to: string;
  toX: number;
  toY: number;
  tier: number;
  length: number;
  efficiency: number;
  throughput: number;
  /** Units it carried in all */
  carried: number;
  publicFee: number;
  crewFee: number;
  /** What you pay its carrier (of the goods' base value) */
  yourFee: number;
  /** What it earned its carrier (yours only) */
  earned: number;
  route: number[];
  /** The carrier's: every player of the two towns, with the fee of their own (null: the public or crew fee) */
  players: LinePlayerDto[];
}

export interface LinePlayerDto {
  id: number;
  name: string;
  color: string;
  fee: number | null;
}

/** A neighbouring town a line could run to: what it would cost, and why it can't be (or null) */
export interface LineOptionDto {
  x: number;
  y: number;
  name: string;
  era: number;
  tier: number;
  length: number;
  gold: number;
  error: string | null;
}

export interface LinesDto {
  lines: TrunkLineDto[];
  options: LineOptionDto[];
  tiers: number[];
  /** The goods you send over a line to another town's market, and the line */
  exports: Record<string, number>;
}

/** A town's Yard on the map: its crew's name and banner (none while nobody lives there), open or closed, its stalls */
export interface DistrictYardDto {
  index: number;
  name: string;
  emblem: string | null;
  color: string | null;
  color2: string | null;
  open: boolean;
  stalls: StallDto[];
  /** The Monuments standing in its square (1.4 §13) */
  monuments: Array<{ era: number; kind: string; stages: number }> | null;
}

/** A resident's stall at its Yard gate (1.4 §7): what it sells the crew, its crew price, what it would buy */
export interface StallDto {
  slot: number;
  sells: string[];
  crewPrice: number;
  wants: string[];
}

/** toYard, fromYard: through your Yard gate (the gate's end has no building: 0) */
export type FlowKind =
  "internal" | "trade" | "toStorage" | "fromStorage" | "sale" | "import" | "toYard" | "fromYard";

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
  /** What leaves and what arrives (the pipe loses the difference), and the gold it moves a minute */
  sent?: number;
  delivered?: number;
  gold?: number;
}

/**
 * A good a trading post sells: what all your posts sell of it a minute, your Hub Order for it (what the market pays the
 * full price for: beyond it each unit pays less), and the gold a minute this post gets for it
 */
export interface PostSaleDto {
  resource: string;
  perMinute: number;
  hubOrder: number;
  gold: number;
  /** What a unit is paid now, and the full price (the price up to the Hub Order) */
  price?: number;
  fullPrice?: number;
}

export interface BuildingDetailsDto {
  building: BuildingDto;
  ownerName: string;
  rate: number;
  locationMultiplier: number;
  inputs: ResourceAmountDto[];
  outputs: ResourceAmountDto[];
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
  /** A storage: what it keeps of each resource */
  /**
   * A Storage: what it keeps of each good (keeps: in effect, at least what the company needs), what its owner set (set:
   * its sliders, or the old share of its room for every good), what the company needs of each, whether its sliders are set
   */
  keep: {
    keeps: Record<string, number>;
    set?: Record<string, number> | null;
    needs?: Record<string, number> | null;
    manual?: boolean;
  } | null;
  /**
   * Smart Max: maxAffordableLevels are the levels its pipes can feed; these are the levels the gold could buy beyond
   * them, how fast it would run there, and what holds it back
   */
  maxMore: MaxMoreDto | null;
  /** Why it doesn't run fully (F2): the cause, the status pill, the reach line and the one fix button */
  status: BuildingStatusDto | null;
  /** Its trade with the neighbours, for its owner (step 4) */
  trade: BuildingTradeDto | null;
  /** What the next level and Max will do to your income, solved ("adds nothing: its pipes carry 100 coal a minute") */
  upgradePreview: string | null;
  maxPreview: string | null;
  /** Its last purchase can be taken back for this much gold until undoUntil */
  undoRefund: number | null;
  undoUntil: string | null;
  /** A scaffold that still wants materials */
  site: SiteDto | null;
  /** Its pipe door, when its pipe is what holds it back */
  pipeDoor: PipeDoorDto | null;
  /** The levels bought whose goods are still coming (1.4 §9; null: none) */
  levelWait: LevelWaitDto | null;
  /** What this copy cost to build (its levels cost from it), and the goods the next step takes */
  copyPrice: number;
  nextLevelGoods: ResourceAmountDto[] | null;
  /** The track it follows and your tiers in it (1.4 §12) */
  track: string | null;
  trackTiers: number;
  /** A building of a later Era than its town's: it sleeps until the town gets there */
  sleeping: boolean;
  /** A trading post: what it sells by good against your Hub Orders (null: not a trading post) */
  sales?: PostSaleDto[] | null;
  /** The level goods of the Level, +10 and Max buttons (from level 11; null: none) */
  goods1?: LevelGoodsDto | null;
  goods10?: LevelGoodsDto | null;
  goodsMax?: LevelGoodsDto | null;
}

/** What upgrading a selection costs: +1 and +10 each, the cheapest single level, how many can, what they make */
export interface SelectionCostsDto {
  plusOne: number;
  plusTen: number;
  cheapest: number;
  upgradable: number;
  makes: Record<string, number>;
  goodsPlusOne?: LevelGoodsDto | null;
  goodsPlusTen?: LevelGoodsDto | null;
}

/**
 * The goods some levels take on top of their gold (from level 11), and what they would cost if the market brought all
 * of them (your own and your neighbours' cost less)
 */
export interface LevelGoodsDto {
  goods: ResourceAmountDto[];
  marketCost: number;
}

/** A building that works at its old level until the goods of the levels bought are in (1.4 §9) */
export interface LevelWaitDto {
  boughtLevel: number;
  wants: SiteWantDto[];
  /** What buying the rest at the market costs now (null: nothing to buy) */
  buyRest: number | null;
}

/**
 * A building's trade with its neighbours, for its owner (step 4): who takes its goods and who sends it its inputs (both
 * ends named), who could use its goods, what the project wants of them, and the pipe to the gate a Send lays first
 */
export interface BuildingTradeDto {
  takers: TradeEndDto[];
  suppliers: TradeEndDto[];
  bids: BidDto[];
  reachesGate: boolean;
  gatePipe: BuildingFixDto | null;
  /** "runs on Ben's sand 💛" */
  runsOn: string | null;
}

/** One end of a Yard trade; sent: a Send, canThank: Thanks today, house: an NPC partner */
export interface TradeEndDto {
  resource: string;
  partner: number;
  partnerName: string;
  partnerBuilding: string | null;
  partnerBuildingId: number | null;
  perMinute: number;
  price: number;
}

/** "Ana's Glassworks could use 15/min · it buys from Bertha ⚙ now · Send to Ana ▸" */
export interface BidDto {
  resource: string;
  buyer: number;
  buyerName: string;
  building: string | null;
  buildingId: number | null;
  perMinute: number;
}

export interface BuildingStatusDto {
  cause: string;
  /** The cause in five words; null when it runs fine */
  pill: string | null;
  reach: string | null;
  fix: BuildingFixDto | null;
}

/** What your gold can do (after run 4): the shared goal, a want, and "Grow everything that pays"; an hour of your income */
export interface GoldUsesDto {
  shared: GoldLineDto | null;
  want: GoldLineDto | null;
  grow: GrowPlanDto | null;
  hour: number;
  /** Where your gold a minute comes from */
  income?: IncomeDto | null;
}

/**
 * Where your gold a minute comes from: its lines (they add up to total, the top bar's), today's hub orders' pay on top,
 * and what your own goods going into your levels would have sold for
 */
export interface IncomeDto {
  lines: IncomeLineDto[];
  total: number;
  ordersToday: number;
  ownGoodsIntoWants: number;
  /** What the market brings for your levels, buildings and pipes now (paid as it comes, not in the gold a minute) */
  wantImports?: WantImportDto[] | null;
}

/** A good the market brings for your wants: how many a minute now, at what price (2.5× base), how many still to come */
export interface WantImportDto {
  resource: string;
  perMinute: number;
  price: number;
  left: number;
}

/** A line of your gold a minute: what, the good, with whom, how many a minute, the gold a minute (a hub sale: your Hub Order) */
export interface IncomeLineDto {
  kind:
    | "hub"
    | "storage"
    | "player"
    | "fromPlayer"
    | "import"
    | "yardSold"
    | "yardOnItsWay"
    | "yardBought"
    | "yardFloor";
  resource: string;
  who: string | null;
  units: number;
  gold: number;
  hubOrder: number | null;
}

/** One line: "wonder" orders resource for cost (a day of your income) or allCost; "want" orders the rest of target */
export interface GoldLineDto {
  text: string;
  action: "wonder" | "want";
  cost: number | null;
  resource: string | null;
  target: number | null;
  forName: string | null;
  allCost: number | null;
}

export interface GrowPlanDto {
  parts: GrowPartDto[];
  cost: number;
  gain: number;
  /** The last Grow can be taken back until then */
  undoUntil: string | null;
  /** What Grow keeps the gold for instead (the next parcel, soon): "Saving for your next parcel (…)" */
  saving: string | null;
  /** What its levels' goods (from level 11) cost on top: they come in after the click */
  goods?: number;
  /** Its levels' goods, by good */
  levelGoods?: LevelGoodsDto | null;
}

export interface GrowPartDto {
  building: number;
  text: string;
  cost: number;
  gain: number;
}

/** "Your Iron Mine's pipe is full: Copper Pipe on its link, then Level 30": the building, and the ladder */
export interface PipesLineDto {
  text: string;
  building: number;
  full: number;
  /** "Grey Pipe · Copper Pipe · Steel Pipe with Era 2" */
  ladder: string;
}

/**
 * The way up a full pipe (after run 4: the pipe tier is the game's real level cap): the next tier or Widen on its full
 * links, then the levels the new pipe carries, with one preview
 */
export interface PipeDoorDto {
  kind: "tier" | "widen" | "waits" | "locked";
  /** What it does: "Copper Pipe on its 2 links (34 tiles), then Level 30" */
  text: string;
  /** Its button: the text with the price */
  label: string;
  gold: number;
  levelCost: number;
  toLevel: number;
  gain: number;
  materials: ResourceAmountDto[];
  /** Of what the pipe takes, what your Storages lack */
  short: ResourceAmountDto[];
  /** Where the goods it lacks come from: "Cleo makes copper ingots: the Yard brings them" */
  source: string | null;
  orderCost: number | null;
  /** What the goods of its levels from 11 cost on top: they come in after the click */
  goods?: number;
}

/** The one fix button: lay a planned pipe (tiles as x, y pairs), open another building, repair, upgrade, build, or its pipe door */
export interface BuildingFixDto {
  kind: "layPipe" | "open" | "repair" | "upgrade" | "build" | "climb";
  label: string;
  tiles: number[];
  newTiles: number;
  cost: number;
  building: number | null;
  buildingType: string | null;
  /** Where a pipe goes (a building's name) */
  target: string | null;
  /** The pipe goes to one of your own buildings short of what this one makes: it comes before a neighbour's Send */
  ownNeed: boolean;
  /** pipeTier: the tier the tiles go to; widen: ×2 or ×4 */
  tier: number;
}

export interface MaxMoreDto {
  levels: number;
  cost: number;
  /** 0..1: how fast it would run at that level */
  share: number;
  resource: string | null;
  /** Per minute: what its pipes bring of that input (or take of that output), and what it would need */
  have: number;
  need: number;
  output: boolean;
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
  | "achievement"
  | "orderPosted";

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
  | "cavern"
  | "redHills"
  | "outskirts";

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
  | "cargoDepot"
  | "depot"
  | "workshop"
  | "weatherStation"
  | "recycler"
  | "expeditionOffice"
  | "blueprintOffice"
  | "deepLift"
  | "teleporter"
  | "terraformer"
  | "powerLine"
  | "decoration";

export interface ResourceDefinition {
  id: string;
  name: string;
  icon: string;
  color: string;
  tier: number;
  era: number;
  price: number;
  /** "solid", "fluid" or "power" (the pipe's three lanes, 1.4 §9) */
  kind: string;
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

/** An item of the Era tree (1.4 §12): short research that opens an Era's pipes, buildings and systems */
export interface ResearchDefinition {
  id: string;
  name: string;
  era: number;
  /** Every company gets it at this Company Level once its Era has come (0: only by research) */
  grant: number;
  gold: number;
  minutes: number;
  managerSlots: number;
  description: string;
}

/** A research track (1.4 §12): deep research for one chain, nine tiers */
export interface TrackDefinition {
  id: string;
  name: string;
  era: number;
  /** The share of each faster tier's boost (Power: half) */
  strength: number;
  /** What tiers from 3 take besides gold */
  good: string | null;
  /** Whose Hub Orders it raises from tier 5 */
  goods: string[];
  buildings: string[];
  description: string;
}

export type TierEffect = "faster" | "leaner" | "complexes" | "megaComplexes" | "master";

export type MonumentPerk = "production" | "hubOrders" | "research" | "cheaper" | "yardSales";

export interface MonumentKindDefinition {
  id: string;
  name: string;
  perk: MonumentPerk;
  amount: number;
  cap: number;
  description: string;
}

export interface GameContentDto {
  resourceList: ResourceDefinition[];
  relics: Array<{ id: string; name: string; lore: string }>;
  buildings: BuildingDefinition[];
  pipeTiers: PipeTierDefinition[];
  research: ResearchDefinition[];
  tracks: TrackDefinition[];
  /** The nine tiers every track has: how long, what it does, its gold in an Era-1 track (×4 per Era) */
  trackTiers: Array<{ tier: number; minutes: number; effect: TierEffect; amount: number; gold: number }>;
  hubOrderFrom: number;
  hubOrderPerTier: number;
  monuments: MonumentKindDefinition[];
  wonders: Array<{ id: string; name: string; era: number; description: string; worth: number }>;
  progression: {
    maxCompanyLevel: number;
    /** The gold earned in all each Company Level needs */
    earnedForLevel: number[];
    maxBuildingLevel: number;
    outputPerLevel: number;
    costPerLevel: number;
    milestones: number[];
    costPerCopy: number;
    /** From this building level a step takes goods too, worth this share of its gold */
    levelGoodsFrom: number;
    levelGoodsShare: number;
    parcelLevels: number[];
    parcelCosts: number[];
    hubSpread: number;
  };
  /** The geometry of 1.4 §4 (district-local tiles) */
  layout: {
    estateSize: number;
    parcelSize: number;
    parcelsPerSide: number;
    chunkSize: number;
    districtWidth: number;
    districtHeight: number;
    wildernessWidth: number;
    districtPitchX: number;
    districtPitchY: number;
    originOffsetX: number;
    originOffsetY: number;
    roadX: number;
    roadY: number;
    roadWidth: number;
    hub: Rect | null;
    townGates: Rect[];
    plots: Array<{
      origin: Point | null;
      side: string;
      gate: Point | null;
      startParcels: number[];
      hq: Point | null;
      tradingPost: Point | null;
      /** The nine outskirts parcels set aside for the plot (district-local corners) */
      outskirts: Point[];
    }>;
    yards: Array<Rect | null>;
    terrain: TerrainType[];
    richnessMultiplier: number[];
  };
}

interface Point {
  x: number;
  y: number;
}

interface Rect {
  x0: number;
  y0: number;
  x1: number;
  y1: number;
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
  /** What neighbours' goods bring to its buildings through the Yard gate */
  fromYard: number;
  /** What its buildings sell to neighbours through the Yard gate */
  toYard: number;
  /** The gold a minute its sales to the hub bring */
  soldGold?: number;
  /** Of toYard, what goes into its owner's links (what arrives, what fills the buyers' tanks, what the links' pipes lose) */
  toLinks?: number;
  /** Of what its buildings lack, what is for buildings that couldn't run on it anyway (another input missing) */
  shortWhy?: string | null;
  /** What its buildings could make of it at full speed (with the network's pumps) */
  couldMake?: number;
  /** Why some of them make less: how many, and the first one's reason */
  slowWhy?: string | null;
  /** The first of them, to open */
  slowFirst?: number | null;
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
  /** The buildings whose goods can't leave (F2): the card names them first */
  stuck: Array<{ building: number; type: string; line: string }> | null;
  /** Your pipe tiles that wait for a better pipe already (x, y pairs): Upgrade leaves them out */
  waiting: number[] | null;
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
  /** A block you set that waits its hour's notice: when it counts */
  blockAt: string | null;
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
  /** What was placed (0: not kept), and when it closes */
  placed: number;
  expiresAt: string | null;
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
  /** The Company Level the Exchange opens at (1.4 §8: 12), null once it's open */
  opens: number | null;
  books: ExchangeBookDto[];
  orders: ExchangeOrderDto[];
  shipments: ShipmentDto[];
  fee: number;
  /** Global Exchange: orders trade with every hub */
  global: boolean;
  /** The default player price of each good, and what your Storages hold */
  prices: Record<string, number> | null;
  stored: Record<string, number> | null;
}

// ---------------------------------------------------------------- Society

export type LeaderboardKind =
  "netWorth" | "income" | "weeklyEarnings" | "level" | "partners" | "likes" | "efficiency";

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
  isYou: boolean;
  likes: number;
  likedToday: boolean;
  /** The Yard they live at */
  yard: string | null;
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
  /** What their scaffolds and pipes wait for, and what ordering it for them costs you (after run 4) */
  wants?: WantNearDto[] | null;
}

export interface ChatMessageDto {
  id: number;
  channel: string;
  sender: number;
  senderName: string;
  text: string;
  sentAt: string;
}

export type ChatChannelKind = "crew" | "station" | "district" | "corporation" | "direct";

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

// ---------------------------------------------------------------- Your town: its Era, its Wonder, your Yard's Monuments (1.4 §11, §13)

export interface WonderNeedDto {
  resource: string;
  required: number;
  delivered: number;
  yourStock: number;
  /** What buying and giving the rest of it costs (the market's 2.5×) */
  buyCost: number;
  /** What the town's give switches send it now a minute */
  perMinute: number;
  /** Your give switch for it */
  give: GiveMode;
  /** The town's players who make it */
  makers: string[];
  /** When you don't make it: the building that would */
  chain: string | null;
  /** That building's type, for Show me (null when you may not build it yet) */
  chainBuilding: string | null;
}

export interface WonderBuilderDto {
  company: number;
  name: string;
  color: string;
  value: number;
  /** Their share of the stage */
  share: number;
}

export type WonderTier = "none" | "bronze" | "silver" | "gold" | "diamond";

/** A town's Era Wonder: its stage and goods, when a full stage stands, your part and tier, the perks it gave */
export interface TownWonderDto {
  type: string;
  name: string;
  description: string;
  era: number;
  stage: number;
  stages: number;
  needs: WonderNeedDto[];
  stageWorth: number;
  worth: number;
  /** A full stage stands a day after (null: not full yet) */
  standsAt: string | null;
  yourStageShare: number;
  yourTier: WonderTier;
  yourGiven: number;
  topBuilders: WonderBuilderDto[];
  buyAllCost: number;
  /** How much smaller towns that finished it before made it (0.9: 10%) */
  trail: number;
  perks: string[];
  nextPerk: string | null;
}

/** A Monument site of your Yard: its Era, what was built there, its stages and the next stage's price */
export interface MonumentDto {
  era: number;
  kind: string | null;
  name: string | null;
  stages: number;
  paid: number;
  nextCost: number;
  capped: boolean;
  perk: string | null;
  yourPaid: number;
}

export interface MonumentKindDto {
  id: string;
  name: string;
  description: string;
  built: boolean;
}

/** Your town and Yard (1.4 §11, §13): its Era, its launch party, its Wonder, your Yard's Monuments */
export interface TownDto {
  name: string;
  era: number;
  yourEra: number;
  eraStartedAt: string;
  partyUntil: string | null;
  wonder: TownWonderDto | null;
  monuments: MonumentDto[];
  kinds: MonumentKindDto[];
  /** The Company Level that pays for Monuments */
  monumentLevel: number;
  /** An hour of your income */
  yourHour: number;
  launchedAt: string | null;
  /** Missions done since the Launch: a star each */
  stars: number;
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
  /** What it pays: hours of your income, at least this much gold */
  hours: number;
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
  /** The step's text on a phone, when it differs */
  phoneText: string | null;
  /** The pipe Show me lays (x, y pairs), its price and where it goes; null: Show me does what the hint says */
  pipe: number[] | null;
  pipeCost: number;
  pipeTarget: string | null;
  /** When the step began: open two days, it shrinks to one line */
  since: string | null;
  /** The Era its chapter waits for (null: it can be done now) */
  waitEra: number | null;
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
    | "start"
    | "stuck"
    | "shared";
  title: string;
  reason: string;
  action:
    | "upgrade"
    | "build"
    | "select"
    | "research"
    | "parcel"
    | "partner"
    | "climb"
    | "connect"
    | "layPipe"
    | "wonder"
    | "storey";
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
  /** An upgrade step's level goods (from level 11) */
  goods?: LevelGoodsDto | null;
}

// ---------------------------------------------------------------- The Yard (v1.2 §7)

/** A resident of a station: whether its pipes reach its Yard gate, what it has spare (it goes to the hub) and lacks */
export interface YardResidentDto {
  id: number;
  name: string;
  color: string;
  level: number;
  slot: number;
  connected: boolean;
  activity: ActivityStage | null;
  spare: ResourceRateDto[];
  lacks: ResourceRateDto[];
  blocked: boolean;
  /** The Plan board's pins: what it makes and needs (v1.2 §12) */
  makes: string[];
  needs: string[];
  /** Its price for you per good (a multiple of the base), for the goods it sells */
  prices: Record<string, number>;
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
  /** Who could use your goods */
  bids: BidDto[];
  /** What you and each neighbour sold each other this week */
  trades: WeekTradeDto[];
  /** What the Yard pays you of your income, and whom for it (null: nothing yet) */
  incomeLine: string | null;
  /** The Company Level from which you pay into the Yard's Monuments */
  monumentLevel: number;
}

// ---------------------------------------------------------------- Yards and plots (1.4 §6–7)

/** A plot on the map: free, closed (free, but its Yard is closed: you ask first), taken, yours */
export type SeatState = "free" | "closed" | "taken" | "you";

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
  /** What the plot's land has and lacks, and its strength (a ★4 deposit of something it has) */
  goods: string[] | null;
  lacks: string[] | null;
  strength: string | null;
  /** For a player who would move here: how many of their buildings wouldn't fit its land and wait in crates */
  crates: number | null;
}

export interface StationDto {
  districtX: number;
  districtY: number;
  index: number;
  /** Its crew's name, or the name the game gave it while nobody lives there */
  name: string;
  /** Its crew's banner (null: nobody lives there) */
  emblem: string | null;
  color: string | null;
  color2: string | null;
  /** Anyone may settle on its free plots (closed: newcomers ask first) */
  open: boolean;
  free: number;
  lively: number;
  seats: SeatDto[];
  x0: number;
  y0: number;
  x1: number;
  y1: number;
  /** The Plan board: what its players pinned they make and need */
  makes: string[] | null;
  needs: string[] | null;
}

export interface SeatRefDto {
  districtX: number;
  districtY: number;
  slot: number;
}

/** A player Find a player found (1.4 §6): who, and where they live */
export interface FoundPlayerDto {
  id: number;
  name: string;
  color: string;
  level: number;
  districtX: number;
  districtY: number;
  slot: number;
  district: string;
  yard: string | null;
}

export interface StationsDto {
  districtX: number;
  districtY: number;
  district: string;
  stations: StationDto[];
  /** The plot Pick for me chooses */
  pickForMe: SeatRefDto;
  /** The open towns, nearest the Capital first */
  districts: DistrictRefDto[];
}

export interface DistrictRefDto {
  districtX: number;
  districtY: number;
  name: string;
  era: number;
  launched: boolean;
  stars: number;
}

export interface CrewMemberDto {
  id: number;
  name: string;
  color: string;
  level: number;
  activity: string | null;
  slot: number;
  you: boolean;
}

/** A scaffold's wants (after run 3, step 2): each material, what is in, what comes now and from whom, and what Cancel gives back */
export interface SiteDto {
  wants: SiteWantDto[];
  refund: number;
}

export interface SiteWantDto {
  resource: string;
  amount: number;
  got: number;
  perMinute: number;
  from: string[];
  /** When nobody near makes it: the building that would, and who makes its inputs */
  chain: string | null;
  /** An order's state ("Ordered: the hub brings the rest from Cleo's next session, at the latest Thu 20:00") */
  order: string | null;
  /** What ordering the rest costs now (null: ordered already, or nothing left) */
  orderCost: number | null;
  /** The building of the chain line, when you may build it (Show me) */
  chainBuilding: string | null;
}

/** An own want: its target (a building's id, or minus a pipe want's number), what it wants, what is in and coming; tiles for a pipe */
export interface OwnWantDto {
  target: number;
  name: string;
  resource: string;
  amount: number;
  got: number;
  perMinute: number;
  tiles: number | null;
  order: string | null;
  orderCost: number | null;
  /** The level its goods are for (null: a new building's materials or a pipe's) */
  toLevel?: number | null;
  /** A pipe's tiles (x, y, x, y, …) and the tier they become, for the map */
  pipeAt?: number[] | null;
  pipeTier?: number | null;
}

/** A neighbour's scaffold this player could help with */
export interface WantNearDto {
  site: number;
  owner: string;
  building: string;
  resource: string;
  left: number;
  holds: number;
  makes: boolean;
  orderCost: number | null;
}

export interface CrewDto {
  id: number;
  name: string;
  emblem: string;
  color: string;
  color2: string;
  /** A resident named the Yard (until then it carries the name the game gave it) */
  named: boolean;
  /** Open: anyone may settle on its free plots; closed: newcomers ask first */
  open: boolean;
  members: CrewMemberDto[];
  /** Who asked to settle while it's closed, on which plot, and until when the request waits */
  requests: CrewRequestDto[];
  /** Your "Play next to me" link and until when it works (null: none yet, or its first user came) */
  link: string | null;
  linkUntil: string | null;
  /** You'd be the last one out: moving away takes the Yard's own things with it */
  lastOut: boolean;
}

export interface CrewViewDto {
  crew: CrewDto | null;
  emblems: string[];
}

/** A company's crew as the top bar and the map show it */
export interface CrewBadgeDto {
  id: number;
  name: string;
  emblem: string;
  color: string;
  color2: string;
  members: number;
  /** Requests to settle that wait for an answer */
  requests: number;
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
  /** A starting link: when its first goods come (the Yards' rounds) */
  startsAt?: string | null;
  /** What the seller's gate sends into it a minute: what arrives, what fills the tank, what the pipe loses */
  sent?: number;
  /** What the seller promises a minute (before its own buildings), and its cap (0: what the pipe carries) */
  guaranteed?: number;
  upTo?: number;
  /** A change one side asked for: its numbers, and whether you asked it */
  askedGuaranteed?: number | null;
  askedUpTo?: number | null;
  askedByYou?: boolean;
  /** The most its pipe brings a minute, and what the seller made and had spare of the good last round */
  pipeMax?: number;
  sellerMakes?: number;
  sellerSpare?: number;
  /** The seller sends less than it promised (it makes less now) */
  guaranteeShort?: boolean;
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
  /** In your crew */
  crew: boolean;
}

export interface LinksDto {
  links: LinkDto[];
  suggestions: LinkSuggestionDto[];
  max: number;
  partners: LinkPartnerDto[];
}

export interface LinkPlanDto {
  length: number;
  efficiency: number;
  throughput: number;
  gold: number;
  materials: ResourceRateDto[];
  error: string | null;
  route: number[];
  /** What the seller makes of the good a minute where the link starts: the most it can promise */
  sellerMakes?: number;
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
}

/**
 * The station's project in one line (This week, the return card, the Project tab): "🏗 Workshop 96% · 9 days on water ·
 * you can: a Water Pump beside the water · passed over Fri 20:30 unless one of you brings some"
 */
export interface ProjectWeekDto {
  text: string;
  share: number;
  /** What its Show me does: your idle maker, or the build tool with a building on a tile where it stands */
  step: NextStepDto | null;
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

/** What you and a neighbour sold each other of a good this week (units each way) */
export interface WeekTradeDto {
  partner: number;
  name: string;
  resource: string;
  given: number;
  received: number;
}

/** What a "Play next to me" link opens (1.4 §6): who sent it, their Yard, whether it lets you into a closed Yard, a free company name */
export interface NextToMeDto {
  valid: boolean;
  from: string | null;
  fromColor: string | null;
  yard: StationDto | null;
  opens: boolean;
  name: string | null;
}

/** Someone who asked to settle in your closed Yard, on which plot, and until when the request waits */
export interface CrewRequestDto {
  id: number;
  name: string;
  color: string;
  level: number;
  slot: number;
  until: string;
}

// ---------------------------------------------------------------- Statistics (docs/design-notes/statistics.md)

export type StatsRange = "day" | "week" | "month" | "all";

/** A key moment: big ones are flags on every graph; ref is a good or building for its icon */
export interface MomentDto {
  at: string;
  /** founded, level, first, land, era, wonder, crew, partner, move, track, research, achievement, monument, event */
  kind: string;
  text: string;
  big: boolean;
  ref: string | null;
}

/** Samples as columns: a key per series, a time per row, null where a sample lacks a series; the last row is now */
export interface StatsColumns {
  keys: string[];
  at: string[];
  rows: (number | null)[][];
}

export interface StatsTownDto extends StatsColumns {
  name: string;
}

/** Someone to compare with: your crew first, then your town */
export interface StatsPeerDto {
  id: number;
  name: string;
  color: string;
  crew: boolean;
}

/** A company's statistics over a range: yours whole, another's public part and its big moments */
export interface StatisticsDto extends StatsColumns {
  company: number;
  name: string;
  color: string;
  founded: string;
  own: boolean;
  range: StatsRange;
  from: string;
  now: string;
  moments: MomentDto[];
  town: StatsTownDto;
  peers: StatsPeerDto[];
}

/** One series of a company over a range, to compare with */
export interface StatsSeriesDto {
  company: number;
  name: string;
  color: string;
  founded: string;
  key: string;
  at: string[];
  values: (number | null)[];
}

/** The admin's Pace: every company's gold earned by days since it was founded, and the model's player by day */
export interface PaceDto {
  companies: PaceLineDto[];
  model: number[];
}

export interface PaceLineDto {
  id: number;
  name: string;
  color: string;
  founded: string;
  days: number[];
  earned: number[];
}
