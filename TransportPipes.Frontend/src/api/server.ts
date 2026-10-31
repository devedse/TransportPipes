import { HubConnectionBuilder, LogLevel, type HubConnection } from "@microsoft/signalr";
import type {
  LevelGoodsDto,
  SelectionCostsDto,
  PaceDto,
  StatisticsDto,
  StatsRange,
  StatsSeriesDto,
  NextToMeDto,
  GoldUsesDto,
  LinkPlanDto,
  LinksDto,
  MapLinkDto,
  NextStepDto,
  AuthResult,
  BlueprintSharing,
  BlueprintsDto,
  BuildingDetailsDto,
  ChunkDto,
  ChunkObjectsDto,
  CommandResult,
  ChatDto,
  ChatMessageDto,
  CompanyDetailsDto,
  OutpostCheckDto,
  DistrictChoiceDto,
  ReportedMessageDto,
  CompanyDto,
  EstateVisitDto,
  InboxDto,
  LeaderboardDto,
  LeaderboardKind,
  TownDto,
  TrackStateDto,
  GiveMode,
  LinesDto,
  ExchangeDto,
  NeighbourDto,
  DistrictDto,
  GameContentDto,
  HelloDto,
  MarketDto,
  NetworkDetailsDto,
  NetworkFlowsDto,
  ResearchStateDto,
  ToastDto,
  UserInfo,
  YardDto,
  CrewViewDto,
  GuestResult,
  SeatRefDto,
  FoundPlayerDto,
  StationsDto,
} from "./contracts";

export const HUB_URL = "/hubs/game";

/**
 * A random id this browser keeps (v1.2 §3, "same device"): two accounts that used one browser trade at the hub's price
 * and never size a project for two. Kept in the browser only; a new one when storage is blocked.
 */
function browserId(): string {
  const make = () => crypto.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2)}`;
  try {
    const known = localStorage.getItem("tp.browserId");
    if (known !== null) {
      return known;
    }
    const id = make();
    localStorage.setItem("tp.browserId", id);
    return id;
  } catch {
    return make();
  }
}

export type ConnectionStatus = "connecting" | "connected" | "reconnecting" | "disconnected";

async function postJson<T>(url: string, body: unknown): Promise<T> {
  const response = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
    credentials: "same-origin",
  });
  if (!response.ok) {
    throw new Error(`POST ${url} failed with ${response.status}`);
  }
  return (await response.json()) as T;
}

export const auth = {
  /** The logged in user, or null when the login cookie is missing or no longer valid */
  async me(): Promise<UserInfo | null> {
    const response = await fetch("/api/auth/me", { credentials: "same-origin", cache: "no-store" });
    if (response.status === 401) {
      return null;
    }
    if (!response.ok) {
      throw new Error(`GET /api/auth/me failed with ${response.status}`);
    }
    return (await response.json()) as UserInfo;
  },
  login: (userName: string, password: string) =>
    postJson<AuthResult>("/api/auth/login", { userName, password }),
  /** Plays without an account first: a hidden account the browser keeps */
  guest: () => postJson<GuestResult>("/api/auth/guest", {}),
  /** A guest keeps the estate: a name and a password of its own */
  keep: (userName: string, password: string, email: string) =>
    postJson<AuthResult>("/api/auth/keep", { userName, password, email }),
  register: (userName: string, password: string, email: string) =>
    postJson<AuthResult>("/api/auth/register", { userName, password, email }),
  async logout(): Promise<void> {
    await fetch("/api/auth/logout", { method: "POST", credentials: "same-origin" });
  },
};

/** What a "Play next to me" link opens (no login needed) */
export async function fetchInvite(code: string): Promise<NextToMeDto> {
  const response = await fetch(`/api/invite/${encodeURIComponent(code)}`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error(`GET /api/invite failed with ${response.status}`);
  }
  return (await response.json()) as NextToMeDto;
}

export async function fetchContent(): Promise<GameContentDto> {
  const response = await fetch("/api/content", { cache: "no-store" });
  if (!response.ok) {
    throw new Error(`GET /api/content failed with ${response.status}`);
  }
  return (await response.json()) as GameContentDto;
}

/** What the server pushes */
export interface ServerEvents {
  chunkChanged(chunk: ChunkObjectsDto): void;
  networksChanged(networks: number[]): void;
  districtChanged(district: DistrictDto): void;
  companyChanged(company: CompanyDto): void;
  toast(toast: ToastDto): void;
  marketChanged(districtX: number, districtY: number): void;
  exchangeChanged(): void;
  chatMessage(message: ChatMessageDto): void;
  inboxChanged(unread: number): void;
  wondersChanged(): void;
  crewChanged(): void;
  linksChanged(): void;
  mapLinksChanged(): void;
  yardRound(next: string): void;
}

const EVENT_NAMES: Record<keyof ServerEvents, string> = {
  chunkChanged: "ChunkChanged",
  networksChanged: "NetworksChanged",
  districtChanged: "DistrictChanged",
  companyChanged: "CompanyChanged",
  toast: "Toast",
  marketChanged: "MarketChanged",
  exchangeChanged: "ExchangeChanged",
  chatMessage: "ChatMessage",
  inboxChanged: "InboxChanged",
  wondersChanged: "WondersChanged",
  crewChanged: "CrewChanged",
  linksChanged: "LinksChanged",
  mapLinksChanged: "MapLinksChanged",
  yardRound: "YardRound",
};

const START_RETRY_DELAYS_MS = [0, 2000, 5000, 10000];

/**
 * The game hub. The login cookie authenticates it. withAutomaticReconnect only covers drops after a successful start,
 * so starting (and restarting after it gives up) is retried here.
 */
export class GameConnection {
  private readonly connection: HubConnection;
  private readonly statusListeners = new Set<(status: ConnectionStatus) => void>();
  private readonly reconnectedListeners = new Set<() => void>();
  private currentStatus: ConnectionStatus = "disconnected";
  private hasBeenConnected = false;
  private stopped = false;

  constructor(url: string = HUB_URL) {
    this.connection = new HubConnectionBuilder()
      .withUrl(`${url}${url.includes("?") ? "&" : "?"}bid=${encodeURIComponent(browserId())}`)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    this.connection.onreconnecting(() => this.setStatus("reconnecting"));
    this.connection.onreconnected(() => {
      this.setStatus("connected");
      this.reconnectedListeners.forEach((listener) => listener());
    });
    this.connection.onclose(() => {
      this.setStatus("disconnected");
      if (!this.stopped) {
        void this.start();
      }
    });
  }

  get status(): ConnectionStatus {
    return this.currentStatus;
  }

  async start(): Promise<void> {
    this.stopped = false;
    for (let attempt = 0; !this.stopped; attempt++) {
      const delay = START_RETRY_DELAYS_MS[Math.min(attempt, START_RETRY_DELAYS_MS.length - 1)] ?? 0;
      if (delay > 0) {
        await new Promise((resolve) => setTimeout(resolve, delay));
      }
      this.setStatus(this.hasBeenConnected ? "reconnecting" : "connecting");
      try {
        await this.connection.start();
        this.setStatus("connected");
        if (this.hasBeenConnected) {
          this.reconnectedListeners.forEach((listener) => listener());
        }
        this.hasBeenConnected = true;
        return;
      } catch (error) {
        console.warn("Could not connect to the game server, retrying...", error);
      }
    }
  }

  async stop(): Promise<void> {
    this.stopped = true;
    await this.connection.stop();
  }

  on<K extends keyof ServerEvents>(event: K, handler: ServerEvents[K]): () => void {
    const untyped = handler as (...args: unknown[]) => void;
    this.connection.on(EVENT_NAMES[event], untyped);
    return () => this.connection.off(EVENT_NAMES[event], untyped);
  }

  onStatusChanged(listener: (status: ConnectionStatus) => void): () => void {
    this.statusListeners.add(listener);
    return () => this.statusListeners.delete(listener);
  }

  onReconnected(listener: () => void): () => void {
    this.reconnectedListeners.add(listener);
    return () => this.reconnectedListeners.delete(listener);
  }

  private setStatus(status: ConnectionStatus) {
    if (status !== this.currentStatus) {
      this.currentStatus = status;
      this.statusListeners.forEach((listener) => listener(status));
    }
  }

  private invoke<T>(method: string, ...args: unknown[]): Promise<T> {
    return this.connection.invoke<T>(method, ...args);
  }

  hello = () => this.invoke<HelloDto>("Hello");
  createCompany = (name: string, color: string, seat: SeatRefDto | null, code: string | null) =>
    this.invoke<CommandResult>(
      "CreateCompany",
      name,
      color,
      seat?.districtX ?? null,
      seat?.districtY ?? null,
      seat?.slot ?? null,
      code,
    );
  getStations = (districtX: number | null, districtY: number | null, code: string | null = null) =>
    this.invoke<StationsDto>("GetStations", districtX, districtY, code);
  getCrew = () => this.invoke<CrewViewDto | null>("GetCrew");
  /** Any resident names the Yard and picks its banner */
  setYard = (name: string, emblem: string, color: string, color2: string) =>
    this.invoke<CommandResult>("SetYard", name, emblem, color, color2);
  /** Find a player by name, to fly the map to them */
  findPlayer = (text: string) => this.invoke<FoundPlayerDto[]>("FindPlayer", text);
  /** One of your prices ("public", "crew", "public:good", "crew:good", "for:companyId"; null: back to its default) */
  setPrice = (key: string, value: number | null) => this.invoke<CommandResult>("SetPrice", key, value);
  deliverOrder = (orderId: number) => this.invoke<CommandResult>("DeliverOrder", orderId);
  /** Any resident opens or closes the Yard */
  setYardOpen = (open: boolean) => this.invoke<CommandResult>("SetYardOpen", open);
  /** Asks to settle on a free plot of a closed Yard (the request waits three days) */
  requestPlot = (seat: SeatRefDto) =>
    this.invoke<CommandResult>("RequestPlot", seat.districtX, seat.districtY, seat.slot);
  /** A resident says yes (the newcomer moves in for free) or no to a request */
  answerRequest = (requester: number, yes: boolean) =>
    this.invoke<CommandResult>("AnswerRequest", requester, yes);
  /** Your "Play next to me" link's code (valid for seven days) */
  getNextToMeLink = () => this.invoke<string | null>("GetNextToMeLink");
  /** A friend's tap: what your Storages hold of what a neighbour's scaffold wants goes into it */
  sendToSite = (site: number) => this.invoke<CommandResult>("SendToSite", site);
  /** Orders the rest of a want, one's own or a neighbour's: 2 × base, after the friends' window, over 6 hours */
  orderRest = (target: number, resource: string | null = null) =>
    this.invoke<CommandResult>("OrderRest", target, resource);
  /** Buys the rest of all your own wants at once */
  orderAll = () => this.invoke<CommandResult>("OrderAll");
  /** Gives up a scaffold that still waits for its materials: its gold comes back */
  cancelSite = (site: number) => this.invoke<CommandResult>("CancelSite", site);
  /** Takes a building's last purchase back within the hour */
  undo = (building: number) => this.invoke<CommandResult>("Undo", building);
  /** Moves your plot to a free one (into a closed Yard with a friend's "Play next to me" code) */
  relocateTo = (seat: SeatRefDto, code: string | null = null) =>
    this.invoke<CommandResult>("RelocateTo", seat.districtX, seat.districtY, seat.slot, code);
  getChunks = (pairs: number[]) => this.invoke<ChunkDto[]>("GetChunks", pairs);
  unwatch = (pairs: number[]) => this.invoke<void>("Unwatch", pairs);
  getDistrict = (x: number, y: number) => this.invoke<DistrictDto>("GetDistrict", x, y);
  getFlows = (networks: number[]) => this.invoke<NetworkFlowsDto[]>("GetFlows", networks);
  getBuilding = (id: number) => this.invoke<BuildingDetailsDto | null>("GetBuilding", id);
  getNetwork = (id: number) => this.invoke<NetworkDetailsDto | null>("GetNetwork", id);
  getCompany = () => this.invoke<CompanyDto | null>("GetCompany");
  getResearch = () => this.invoke<ResearchStateDto[]>("GetResearch");
  getMarket = () => this.invoke<MarketDto | null>("GetMarket");
  getNeighbourhood = () => this.invoke<NeighbourDto[]>("GetNeighbourhood");
  getYard = () => this.invoke<YardDto | null>("GetYard");
  setPin = (resource: string, need: boolean, on: boolean) =>
    this.invoke<CommandResult>("SetPin", resource, need, on);
  getLinks = () => this.invoke<LinksDto | null>("GetLinks");
  planLink = (partnerId: number, resource: string, selling: boolean, tier: number) =>
    this.invoke<LinkPlanDto | null>("PlanLink", partnerId, resource, selling, tier);
  offerLink = (
    partnerId: number,
    resource: string,
    selling: boolean,
    tier: number,
    guaranteed: number,
    upTo: number,
  ) => this.invoke<CommandResult>("OfferLink", partnerId, resource, selling, tier, guaranteed, upTo);
  /** A link's numbers: what is guaranteed, and its cap (what binds the other side is asked of them) */
  setLinkTerms = (id: number, guaranteed: number, upTo: number) =>
    this.invoke<CommandResult>("SetLinkTerms", id, guaranteed, upTo);
  answerLinkTerms = (id: number, accept: boolean) =>
    this.invoke<CommandResult>("AnswerLinkTerms", id, accept);
  acceptLink = (id: number) => this.invoke<CommandResult>("AcceptLink", id);
  declineLink = (id: number) => this.invoke<CommandResult>("DeclineLink", id);
  endLink = (id: number, end: boolean) => this.invoke<CommandResult>("EndLink", id, end);
  resumeLink = (id: number) => this.invoke<CommandResult>("ResumeLink", id);
  upgradeLink = (id: number, tier: number) => this.invoke<CommandResult>("UpgradeLink", id, tier);
  getMapLinks = () => this.invoke<MapLinkDto[]>("GetMapLinks");
  getExchange = () => this.invoke<ExchangeDto | null>("GetExchange");
  placeExchangeOrder = (resource: string, selling: boolean, amount: number, price: number) =>
    this.invoke<CommandResult>("PlaceExchangeOrder", resource, selling, amount, price);
  cancelExchangeOrder = (order: number) => this.invoke<CommandResult>("CancelExchangeOrder", order);
  setNoTrade = (resource: string, noTrade: boolean) =>
    this.invoke<CommandResult>("SetNoTrade", resource, noTrade);
  setNoBuy = (resource: string, noBuy: boolean) => this.invoke<CommandResult>("SetNoBuy", resource, noBuy);
  setTradeBlocked = (other: number, blocked: boolean) =>
    this.invoke<CommandResult>("SetTradeBlocked", other, blocked);
  getTracks = () => this.invoke<TrackStateDto[]>("GetTracks");
  /** An Era tree item, or a track's next tier (its id) */
  startResearch = (tech: string) => this.invoke<CommandResult>("StartResearch", tech);
  queueResearch = (tech: string, queued: boolean) =>
    this.invoke<CommandResult>("QueueResearch", tech, queued);
  setKeepResearching = (keep: boolean) => this.invoke<CommandResult>("SetKeepResearching", keep);
  stopResearch = (track: boolean) => this.invoke<CommandResult>("StopResearch", track);
  researchFirst = (tech: string) => this.invoke<CommandResult>("ResearchFirst", tech);
  moveQueuedResearch = (track: boolean, index: number, by: number) =>
    this.invoke<CommandResult>("MoveQueuedResearch", track, index, by);
  takeOutOfQueue = (track: boolean, index: number) =>
    this.invoke<CommandResult>("TakeOutOfQueue", track, index);
  placeBuilding = (type: string, x: number, y: number) =>
    this.invoke<CommandResult>("PlaceBuilding", type, x, y);
  placePipes = (tiles: number[], tier: number) => this.invoke<CommandResult>("PlacePipes", tiles, tier);
  removePipes = (tiles: number[]) => this.invoke<CommandResult>("RemovePipes", tiles);
  upgradePipes = (tiles: number[], tier: number) => this.invoke<CommandResult>("UpgradePipes", tiles, tier);
  climbPipe = (buildingId: number, order: boolean) =>
    this.invoke<CommandResult>("ClimbPipe", buildingId, order);
  getGoldUses = () => this.invoke<GoldUsesDto | null>("GetGoldUses");
  growEverything = () => this.invoke<CommandResult>("GrowEverything");
  undoGrowth = () => this.invoke<CommandResult>("UndoGrowth");
  upgrade = (building: number, levels: number) => this.invoke<CommandResult>("Upgrade", building, levels);
  upgradeMany = (buildings: number[], levelsEach: number, budget: number | null) =>
    this.invoke<{ success: boolean; error: string | null; levels: number; gold: number }>(
      "UpgradeMany",
      buildings,
      levelsEach,
      budget,
    );
  getSelectionCosts = (buildings: number[]) => this.invoke<SelectionCostsDto>("GetSelectionCosts", buildings);
  /** What "Spend …, cheapest first" buys for a budget */
  getSpendPlan = (buildings: number[], budget: number) =>
    this.invoke<{ levels: number; gold: number; goods: LevelGoodsDto | null } | null>(
      "GetSpendPlan",
      buildings,
      budget,
    );
  getNextSteps = () => this.invoke<NextStepDto[]>("GetNextSteps");
  placeBuildings = (type: string, spots: number[]) =>
    this.invoke<{ placed: number; gold: number; error: string | null }>("PlaceBuildings", type, spots);
  demolish = (building: number) => this.invoke<CommandResult>("Demolish", building);
  repair = (building: number) => this.invoke<CommandResult>("Repair", building);
  repairAll = () => this.invoke<CommandResult>("RepairAll");

  dig = (parcel: number) => this.invoke<CommandResult>("Dig", parcel);
  evolve = (building: number) => this.invoke<CommandResult>("Evolve", building);
  skipTutorial = () => this.invoke<CommandResult>("SkipTutorial");
  startExpedition = (hours: number) => this.invoke<CommandResult>("StartExpedition", hours);
  linkTeleporter = (sender: number, receiver: number, resource: string) =>
    this.invoke<CommandResult>("LinkTeleporter", sender, receiver, resource);
  unlinkTeleporter = (station: number) => this.invoke<CommandResult>("UnlinkTeleporter", station);
  terraform = (x: number, y: number, action: string, resource: string | null) =>
    this.invoke<CommandResult>("Terraform", x, y, action, resource);
  move = (building: number, x: number, y: number) => this.invoke<CommandResult>("Move", building, x, y);
  buyParcel = (parcel: number, estate: number) => this.invoke<CommandResult>("BuyParcel", parcel, estate);
  setImport = (building: number, resource: string, enabled: boolean) =>
    this.invoke<CommandResult>("SetImport", building, resource, enabled);
  setBuildingSetting = (building: number, value: number) =>
    this.invoke<CommandResult>("SetBuildingSetting", building, value);
  /** A Storage's sliders: how much of each good it keeps */
  setStorageKeeps = (building: number, keeps: Record<string, number>) =>
    this.invoke<CommandResult>("SetStorageKeeps", building, keeps);
  setValveFilter = (building: number, resource: string, enabled: boolean) =>
    this.invoke<CommandResult>("SetValveFilter", building, resource, enabled);
  cheat = (what: string) => this.invoke<CommandResult>("Cheat", what);

  // Statistics (docs/design-notes/statistics.md)
  getStatistics = (company: number | null, range: StatsRange) =>
    this.invoke<StatisticsDto | null>("GetStatistics", company, range);
  getStatisticsSeries = (company: number, key: string, range: StatsRange) =>
    this.invoke<StatsSeriesDto | null>("GetStatisticsSeries", company, key, range);
  getPace = () => this.invoke<PaceDto | null>("GetPace");

  // Society
  getLeaderboard = (kind: LeaderboardKind, district: boolean) =>
    this.invoke<LeaderboardDto | null>("GetLeaderboard", kind, district);
  visitEstate = (owner: number) => this.invoke<EstateVisitDto | null>("VisitEstate", owner);
  likeEstate = (owner: number) => this.invoke<CommandResult>("LikeEstate", owner);
  chooseMentor = (mentor: number) => this.invoke<CommandResult>("ChooseMentor", mentor);
  writeGuestbook = (owner: number, text: string) => this.invoke<CommandResult>("WriteGuestbook", owner, text);
  deleteGuestbookNote = (note: number) => this.invoke<CommandResult>("DeleteGuestbookNote", note);
  getChat = () => this.invoke<ChatDto | null>("GetChat");
  getChatHistory = (channel: string, before: number | null) =>
    this.invoke<ChatMessageDto[]>("GetChatHistory", channel, before);
  sendChat = (channel: string, text: string) => this.invoke<CommandResult>("SendChat", channel, text);
  reportChat = (message: number) => this.invoke<CommandResult>("ReportChat", message);
  setBlocked = (company: number, blocked: boolean) =>
    this.invoke<CommandResult>("SetBlocked", company, blocked);
  getInbox = () => this.invoke<InboxDto | null>("GetInbox");
  markInboxRead = () => this.invoke<void>("MarkInboxRead");
  getTown = () => this.invoke<TownDto | null>("GetTown");
  setGive = (resource: string, mode: GiveMode) => this.invoke<CommandResult>("SetGive", resource, mode);
  giveFromStorages = () => this.invoke<CommandResult>("GiveFromStorages");
  /** Buys what the stage lacks (one good, or every good) at the market's 2.5× and gives it */
  buyAndGive = (resource: string | null, gold: number | null) =>
    this.invoke<CommandResult>("BuyAndGive", resource, gold);
  chooseMonument = (era: number, kind: string) => this.invoke<CommandResult>("ChooseMonument", era, kind);
  payMonument = (era: number, gold: number) => this.invoke<CommandResult>("PayMonument", era, gold);
  putInDepot = (resource: string, amount: number) =>
    this.invoke<CommandResult>("PutInDepot", resource, amount);
  takeFromDepot = (resource: string, amount: number) =>
    this.invoke<CommandResult>("TakeFromDepot", resource, amount);
  buyOutskirts = (index: number) => this.invoke<CommandResult>("BuyOutskirts", index);
  getLines = () => this.invoke<LinesDto | null>("GetLines");
  buildLine = (toX: number, toY: number, tier: number) =>
    this.invoke<CommandResult>("BuildLine", toX, toY, tier);
  setLineFee = (line: number, who: string, fee: number | null) =>
    this.invoke<CommandResult>("SetLineFee", line, who, fee);
  setExport = (resource: string, line: number | null) =>
    this.invoke<CommandResult>("SetExport", resource, line);
  setWantsFromMarket = (on: boolean) => this.invoke<CommandResult>("SetWantsFromMarket", on);
  getCompanyDetails = () => this.invoke<CompanyDetailsDto | null>("GetCompanyDetails");
  getDistricts = () => this.invoke<DistrictChoiceDto[]>("GetDistricts");
  placeCrate = (crate: number, x: number, y: number) => this.invoke<CommandResult>("PlaceCrate", crate, x, y);
  scrapCrate = (crate: number) => this.invoke<CommandResult>("ScrapCrate", crate);
  getReports = () => this.invoke<ReportedMessageDto[]>("GetReports");
  moderateMessage = (message: number, action: "dismiss" | "delete" | "mute" | "ban") =>
    this.invoke<CommandResult>("ModerateMessage", message, action);
  renameCompany = (company: number, name: string) =>
    this.invoke<CommandResult>("RenameCompany", company, name);
  checkOutpost = (x: number, y: number) => this.invoke<OutpostCheckDto | null>("CheckOutpost", x, y);
  claimOutpost = (x: number, y: number) => this.invoke<CommandResult>("ClaimOutpost", x, y);
  releaseOutpost = (outpost: number) => this.invoke<CommandResult>("ReleaseOutpost", outpost);
  hireManager = (manager: string) => this.invoke<CommandResult>("HireManager", manager);
  dismissManager = (manager: string) => this.invoke<CommandResult>("DismissManager", manager);
  getBlueprints = () => this.invoke<BlueprintsDto | null>("GetBlueprints");
  saveBlueprint = (name: string, x: number, y: number, width: number, height: number) =>
    this.invoke<CommandResult>("SaveBlueprint", name, x, y, width, height);
  buildBlueprint = (blueprint: number, x: number, y: number) =>
    this.invoke<CommandResult>("BuildBlueprint", blueprint, x, y);
  shareBlueprint = (blueprint: number, sharing: BlueprintSharing) =>
    this.invoke<CommandResult>("ShareBlueprint", blueprint, sharing);
  rateBlueprint = (blueprint: number, stars: number) =>
    this.invoke<CommandResult>("RateBlueprint", blueprint, stars);
  deleteBlueprint = (blueprint: number) => this.invoke<CommandResult>("DeleteBlueprint", blueprint);
}
