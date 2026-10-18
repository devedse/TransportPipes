import { HubConnectionBuilder, LogLevel, type HubConnection } from "@microsoft/signalr";
import type {
  AuthResult,
  BuildingDetailsDto,
  ChunkDto,
  ChunkObjectsDto,
  CommandResult,
  CompanyDto,
  DistrictDto,
  GameContentDto,
  GatePolicy,
  GateSide,
  HelloDto,
  NetworkDetailsDto,
  NetworkFlowsDto,
  ResearchStateDto,
  ToastDto,
  UserInfo,
} from "./contracts";

export const HUB_URL = "/hubs/game";

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
  register: (userName: string, password: string, email: string) =>
    postJson<AuthResult>("/api/auth/register", { userName, password, email }),
  async logout(): Promise<void> {
    await fetch("/api/auth/logout", { method: "POST", credentials: "same-origin" });
  },
};

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
}

const EVENT_NAMES: Record<keyof ServerEvents, string> = {
  chunkChanged: "ChunkChanged",
  networksChanged: "NetworksChanged",
  districtChanged: "DistrictChanged",
  companyChanged: "CompanyChanged",
  toast: "Toast",
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
      .withUrl(url)
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
  createCompany = (name: string, color: string) => this.invoke<CommandResult>("CreateCompany", name, color);
  getChunks = (pairs: number[]) => this.invoke<ChunkDto[]>("GetChunks", pairs);
  unwatch = (pairs: number[]) => this.invoke<void>("Unwatch", pairs);
  getDistrict = (x: number, y: number) => this.invoke<DistrictDto>("GetDistrict", x, y);
  getFlows = (networks: number[]) => this.invoke<NetworkFlowsDto[]>("GetFlows", networks);
  getBuilding = (id: number) => this.invoke<BuildingDetailsDto | null>("GetBuilding", id);
  getNetwork = (id: number) => this.invoke<NetworkDetailsDto | null>("GetNetwork", id);
  getCompany = () => this.invoke<CompanyDto | null>("GetCompany");
  getResearch = () => this.invoke<ResearchStateDto[]>("GetResearch");
  startResearch = (tech: string) => this.invoke<CommandResult>("StartResearch", tech);
  placeBuilding = (type: string, x: number, y: number) =>
    this.invoke<CommandResult>("PlaceBuilding", type, x, y);
  placePipes = (tiles: number[], tier: number) => this.invoke<CommandResult>("PlacePipes", tiles, tier);
  removePipes = (tiles: number[]) => this.invoke<CommandResult>("RemovePipes", tiles);
  upgrade = (building: number, levels: number) => this.invoke<CommandResult>("Upgrade", building, levels);
  demolish = (building: number) => this.invoke<CommandResult>("Demolish", building);
  move = (building: number, x: number, y: number) => this.invoke<CommandResult>("Move", building, x, y);
  buyParcel = (parcel: number) => this.invoke<CommandResult>("BuyParcel", parcel);
  setSellPrice = (resource: string, price: number | null) =>
    this.invoke<CommandResult>("SetSellPrice", resource, price);
  setBuyMaxPrice = (resource: string, price: number | null) =>
    this.invoke<CommandResult>("SetBuyMaxPrice", resource, price);
  setImport = (building: number, resource: string, enabled: boolean) =>
    this.invoke<CommandResult>("SetImport", building, resource, enabled);
  setGatePolicy = (side: GateSide, policy: GatePolicy) =>
    this.invoke<CommandResult>("SetGatePolicy", side, policy);
  setValveFilter = (building: number, resource: string, enabled: boolean) =>
    this.invoke<CommandResult>("SetValveFilter", building, resource, enabled);
  cheat = (what: string) => this.invoke<CommandResult>("Cheat", what);
}
