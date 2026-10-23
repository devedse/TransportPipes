import { HubConnectionBuilder, LogLevel, type HubConnection } from "@microsoft/signalr";
import type {
  LandingCardDto,
  ProjectDto,
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
  SleeperDto,
  ReportedMessageDto,
  SeasonDto,
  CompanyDto,
  ContractsDto,
  CorporationRole,
  CorporationsDto,
  EstateVisitDto,
  InboxDto,
  LeaderboardDto,
  LeaderboardKind,
  WondersDto,
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
  FoundCrewResultDto,
  GuestResult,
  InviteDto,
  SeatRefDto,
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

/** What a seat code or crew link opens (no login needed) */
export async function fetchInvite(code: string): Promise<InviteDto> {
  const response = await fetch(`/api/invite/${encodeURIComponent(code)}`, { cache: "no-store" });
  if (!response.ok) {
    throw new Error(`GET /api/invite failed with ${response.status}`);
  }
  return (await response.json()) as InviteDto;
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
  corporationChanged(): void;
  wondersChanged(): void;
  seasonChanged(): void;
  crewChanged(): void;
  linksChanged(): void;
  mapLinksChanged(): void;
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
  corporationChanged: "CorporationChanged",
  wondersChanged: "WondersChanged",
  seasonChanged: "SeasonChanged",
  crewChanged: "CrewChanged",
  linksChanged: "LinksChanged",
  mapLinksChanged: "MapLinksChanged",
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
  startWithFriends = (
    name: string,
    color: string,
    crewName: string,
    emblem: string,
    crewColor: string,
    crewColor2: string,
    seat: SeatRefDto,
    holdSeats: number,
  ) =>
    this.invoke<FoundCrewResultDto>(
      "StartWithFriends",
      name,
      color,
      crewName,
      emblem,
      crewColor,
      crewColor2,
      seat.districtX,
      seat.districtY,
      seat.slot,
      holdSeats,
    );
  getStations = (districtX: number | null, districtY: number | null) =>
    this.invoke<StationsDto>("GetStations", districtX, districtY);
  getCrew = () => this.invoke<CrewViewDto | null>("GetCrew");
  foundCrew = (name: string, emblem: string, color: string, color2: string, holdSeats: number) =>
    this.invoke<FoundCrewResultDto>("FoundCrew", name, emblem, color, color2, holdSeats);
  joinCrew = (code: string) => this.invoke<CommandResult>("JoinCrew", code);
  leaveCrew = () => this.invoke<CommandResult>("LeaveCrew");
  acceptJoinRequest = (applicant: number) => this.invoke<CommandResult>("AcceptJoinRequest", applicant);
  renewInvite = () => this.invoke<CommandResult>("RenewInvite");
  holdSeats = (count: number) => this.invoke<CommandResult>("HoldSeats", count);
  releaseHold = (code: string) => this.invoke<CommandResult>("ReleaseHold", code);
  nameHold = (code: string, name: string | null) => this.invoke<CommandResult>("NameHold", code, name);
  /** A friend's tap: what your Storages hold of what a neighbour's scaffold wants goes into it */
  sendToSite = (site: number) => this.invoke<CommandResult>("SendToSite", site);
  /** Gives up a scaffold that still waits for its materials: its gold comes back */
  cancelSite = (site: number) => this.invoke<CommandResult>("CancelSite", site);
  /** Takes a building's last purchase back within the hour */
  undo = (building: number) => this.invoke<CommandResult>("Undo", building);
  /** A friend's seat link opened while playing elsewhere (decline: "Not with them") */
  sawSeatCode = (code: string, decline: boolean) => this.invoke<CommandResult>("SawSeatCode", code, decline);
  setStationOpen = (open: boolean) => this.invoke<CommandResult>("SetStationOpen", open);
  relocateTo = (seat: SeatRefDto) =>
    this.invoke<CommandResult>("RelocateTo", seat.districtX, seat.districtY, seat.slot);
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
  tend = (friendId: number) => this.invoke<CommandResult>("Tend", friendId);
  getProject = () => this.invoke<ProjectDto | null>("GetProject");
  setPin = (resource: string, need: boolean, on: boolean) =>
    this.invoke<CommandResult>("SetPin", resource, need, on);
  ask = (resource: string, amount: number, hours: number, renew: boolean) =>
    this.invoke<CommandResult>("Ask", resource, amount, hours, renew);
  cancelAsk = (resource: string) => this.invoke<CommandResult>("CancelAsk", resource);
  setProjectFirst = (resource: string, first: boolean) =>
    this.invoke<CommandResult>("SetProjectFirst", resource, first);
  getLivelierStations = () => this.invoke<LandingCardDto[]>("GetLivelierStations");
  dismissLoneCard = () => this.invoke<CommandResult>("DismissLoneCard");
  crewMove = () => this.invoke<CommandResult>("CrewMove");
  consolidate = () => this.invoke<CommandResult>("Consolidate");
  dismissConsolidation = () => this.invoke<CommandResult>("DismissConsolidation");
  keepSeat = (sleeperId: number, keep: boolean) => this.invoke<CommandResult>("KeepSeat", sleeperId, keep);
  getLandingCards = () => this.invoke<LandingCardDto[]>("GetLandingCards");
  getLinks = () => this.invoke<LinksDto | null>("GetLinks");
  planLink = (partnerId: number, resource: string, selling: boolean, tier: number) =>
    this.invoke<LinkPlanDto | null>("PlanLink", partnerId, resource, selling, tier);
  offerLink = (partnerId: number, resource: string, selling: boolean, tier: number) =>
    this.invoke<CommandResult>("OfferLink", partnerId, resource, selling, tier);
  acceptLink = (id: number) => this.invoke<CommandResult>("AcceptLink", id);
  declineLink = (id: number) => this.invoke<CommandResult>("DeclineLink", id);
  endLink = (id: number, end: boolean) => this.invoke<CommandResult>("EndLink", id, end);
  resumeLink = (id: number) => this.invoke<CommandResult>("ResumeLink", id);
  upgradeLink = (id: number, tier: number) => this.invoke<CommandResult>("UpgradeLink", id, tier);
  getMapLinks = () => this.invoke<MapLinkDto[]>("GetMapLinks");
  startTrunk = (districtX: number | null, districtY: number | null, port: boolean) =>
    this.invoke<CommandResult>("StartTrunk", districtX, districtY, port);
  buildTrunkSegment = (id: number) => this.invoke<CommandResult>("BuildTrunkSegment", id);
  getExchange = () => this.invoke<ExchangeDto | null>("GetExchange");
  placeExchangeOrder = (resource: string, selling: boolean, amount: number, price: number) =>
    this.invoke<CommandResult>("PlaceExchangeOrder", resource, selling, amount, price);
  cancelExchangeOrder = (order: number) => this.invoke<CommandResult>("CancelExchangeOrder", order);
  getContracts = () => this.invoke<ContractsDto | null>("GetContracts");
  proposeContract = (
    partner: number | null,
    selling: boolean,
    resource: string,
    rate: number,
    price: number,
    days: number,
  ) => this.invoke<CommandResult>("ProposeContract", partner, selling, resource, rate, price, days);
  acceptContract = (contract: number) => this.invoke<CommandResult>("AcceptContract", contract);
  declineContract = (contract: number) => this.invoke<CommandResult>("DeclineContract", contract);
  deliverOrder = (order: number) => this.invoke<CommandResult>("DeliverOrder", order);
  setNoTrade = (resource: string, noTrade: boolean) =>
    this.invoke<CommandResult>("SetNoTrade", resource, noTrade);
  setNoBuy = (resource: string, noBuy: boolean) => this.invoke<CommandResult>("SetNoBuy", resource, noBuy);
  setTradeBlocked = (other: number, blocked: boolean) =>
    this.invoke<CommandResult>("SetTradeBlocked", other, blocked);
  setTradeWith = (tradeWith: string) => this.invoke<CommandResult>("SetTradeWith", tradeWith);
  startResearch = (tech: string) => this.invoke<CommandResult>("StartResearch", tech);
  queueResearch = (tech: string, queued: boolean) =>
    this.invoke<CommandResult>("QueueResearch", tech, queued);
  setKeepResearching = (keep: boolean) => this.invoke<CommandResult>("SetKeepResearching", keep);
  placeBuilding = (type: string, x: number, y: number) =>
    this.invoke<CommandResult>("PlaceBuilding", type, x, y);
  placePipes = (tiles: number[], tier: number) => this.invoke<CommandResult>("PlacePipes", tiles, tier);
  removePipes = (tiles: number[]) => this.invoke<CommandResult>("RemovePipes", tiles);
  upgradePipes = (tiles: number[], tier: number) => this.invoke<CommandResult>("UpgradePipes", tiles, tier);
  upgrade = (building: number, levels: number) => this.invoke<CommandResult>("Upgrade", building, levels);
  upgradeMany = (buildings: number[], levelsEach: number, budget: number | null) =>
    this.invoke<{ success: boolean; error: string | null; levels: number; gold: number }>(
      "UpgradeMany",
      buildings,
      levelsEach,
      budget,
    );
  getSelectionCosts = (buildings: number[]) =>
    this.invoke<{
      plusOne: number;
      plusTen: number;
      cheapest: number;
      upgradable: number;
      makes: Record<string, number>;
    }>("GetSelectionCosts", buildings);
  getNextSteps = () => this.invoke<NextStepDto[]>("GetNextSteps");
  placeBuildings = (type: string, spots: number[]) =>
    this.invoke<{ placed: number; gold: number; error: string | null }>("PlaceBuildings", type, spots);
  demolish = (building: number) => this.invoke<CommandResult>("Demolish", building);
  repair = (building: number) => this.invoke<CommandResult>("Repair", building);
  repairAll = () => this.invoke<CommandResult>("RepairAll");
  sendGood = (resource: string, to: number | null, building: number | null) =>
    this.invoke<CommandResult>("SendGood", resource, to, building);
  stopSend = (resource: string, to: number | null) => this.invoke<CommandResult>("StopSend", resource, to);
  thank = (giver: number, resource: string) => this.invoke<CommandResult>("Thank", giver, resource);
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
  setShowroomProduct = (building: number, resource: string) =>
    this.invoke<CommandResult>("SetShowroomProduct", building, resource);
  setValveFilter = (building: number, resource: string, enabled: boolean) =>
    this.invoke<CommandResult>("SetValveFilter", building, resource, enabled);
  cheat = (what: string) => this.invoke<CommandResult>("Cheat", what);

  // Society
  getCorporations = () => this.invoke<CorporationsDto | null>("GetCorporations");
  foundCorporation = (name: string, tag: string, color: string) =>
    this.invoke<CommandResult>("FoundCorporation", name, tag, color);
  applyToCorporation = (corporation: number) => this.invoke<CommandResult>("ApplyToCorporation", corporation);
  inviteToCorporation = (company: number) => this.invoke<CommandResult>("InviteToCorporation", company);
  declineCorporation = (corporation: number) => this.invoke<CommandResult>("DeclineCorporation", corporation);
  declineApplication = (applicant: number) => this.invoke<CommandResult>("DeclineApplication", applicant);
  leaveCorporation = () => this.invoke<CommandResult>("LeaveCorporation");
  kickFromCorporation = (member: number) => this.invoke<CommandResult>("KickFromCorporation", member);
  setCorporationRole = (member: number, role: CorporationRole) =>
    this.invoke<CommandResult>("SetCorporationRole", member, role);
  donateToCorporation = (gold: number) => this.invoke<CommandResult>("DonateToCorporation", gold);
  setCorporationSettings = (tax: number, perkResource: string | null, description: string) =>
    this.invoke<CommandResult>("SetCorporationSettings", tax, perkResource, description);
  upgradeCorporationHq = () => this.invoke<CommandResult>("UpgradeCorporationHq");
  deliverToCorporationGoal = () => this.invoke<CommandResult>("DeliverToCorporationGoal");
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
  getWonders = () => this.invoke<WondersDto | null>("GetWonders");
  getCompanyDetails = () => this.invoke<CompanyDetailsDto | null>("GetCompanyDetails");
  getDistricts = () => this.invoke<DistrictChoiceDto[]>("GetDistricts");
  placeCrate = (crate: number, x: number, y: number) => this.invoke<CommandResult>("PlaceCrate", crate, x, y);
  scrapCrate = (crate: number) => this.invoke<CommandResult>("ScrapCrate", crate);
  proposeSwap = (other: number) => this.invoke<CommandResult>("ProposeSwap", other);
  reserveBlock = (x: number, y: number) => this.invoke<CommandResult>("ReserveBlock", x, y);
  getQuietIsles = () => this.invoke<SleeperDto[]>("GetQuietIsles");
  getReports = () => this.invoke<ReportedMessageDto[]>("GetReports");
  moderateMessage = (message: number, action: "dismiss" | "delete" | "mute" | "ban") =>
    this.invoke<CommandResult>("ModerateMessage", message, action);
  renameCompany = (company: number, name: string) =>
    this.invoke<CommandResult>("RenameCompany", company, name);
  openFranchise = (districtX: number | null, districtY: number | null) =>
    this.invoke<CommandResult>("OpenFranchise", districtX, districtY);
  getSeason = () => this.invoke<SeasonDto>("GetSeason");
  checkOutpost = (x: number, y: number) => this.invoke<OutpostCheckDto | null>("CheckOutpost", x, y);
  claimOutpost = (x: number, y: number) => this.invoke<CommandResult>("ClaimOutpost", x, y);
  releaseOutpost = (outpost: number) => this.invoke<CommandResult>("ReleaseOutpost", outpost);
  hireManager = (manager: string) => this.invoke<CommandResult>("HireManager", manager);
  dismissManager = (manager: string) => this.invoke<CommandResult>("DismissManager", manager);
  deliverToWonder = (eraWonder: boolean) => this.invoke<CommandResult>("DeliverToWonder", eraWonder);
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
