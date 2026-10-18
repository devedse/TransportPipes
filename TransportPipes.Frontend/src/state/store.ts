import { createStore } from "zustand/vanilla";
import { useStore } from "zustand";
import type {
  BuildingDetailsDto,
  ChatDto,
  ChatMessageDto,
  CompanyDto,
  ContractsDto,
  CorporationsDto,
  EstateVisitDto,
  ExchangeDto,
  InboxDto,
  LeaderboardDto,
  LeaderboardKind,
  GateSide,
  MarketDto,
  NeighbourDto,
  NetworkDetailsDto,
  ReportDto,
  ResearchStateDto,
  UserInfo,
} from "../api/contracts";
import type { ConnectionStatus } from "../api/server";
import type { Content } from "../game/content";
import type { Overlay, Tool } from "../game/Engine";
import type { TilePosition } from "../game/isometric";
import type { Placement } from "../game/rules";

export type Phase = "loading" | "login" | "found" | "playing" | "error";

export interface Toast {
  id: number;
  text: string;
  kind: "info" | "success" | "error" | "level";
}

export interface AppState {
  phase: Phase;
  error: string | null;
  content: Content | null;
  user: UserInfo | null;
  company: CompanyDto | null;
  /** Date.now() when the company arrived, to count gold up between updates */
  companyAt: number;
  era: number;
  serverOffsetMs: number;
  connectionStatus: ConnectionStatus;

  tool: Tool;
  buildMenuOpen: boolean;
  buildCategory: string;
  selectedBuilding: number | null;
  buildingDetails: BuildingDetailsDto | null;
  selectedParcel: number | null;
  selectedNetwork: number | null;
  selectedGate: GateSide | null;
  /** Counts district updates, so panels that read the world model render again */
  districtVersion: number;
  networkDetails: NetworkDetailsDto | null;
  overlay: Overlay;
  hoverTile: TilePosition | null;
  placement: Placement | null;
  pipeRoute: TilePosition[] | null;
  zoom: number;

  toasts: Toast[];
  report: ReportDto | null;
  helpOpen: boolean;
  researchOpen: boolean;
  research: ResearchStateDto[];
  hubOpen: boolean;
  market: MarketDto | null;
  exchange: ExchangeDto | null;
  neighboursOpen: boolean;
  neighbours: NeighbourDto[];
  contracts: ContractsDto | null;
  /** A pipe route the player is looking at (Connect), drawn on the map until it is built or dropped */
  plannedRoute: TilePosition[] | null;
  plannedPartner: number | null;

  corporationOpen: boolean;
  corporations: CorporationsDto | null;
  leaderboardOpen: boolean;
  leaderboardKind: LeaderboardKind;
  leaderboardDistrict: boolean;
  leaderboard: LeaderboardDto | null;
  /** The company whose estate the player visits */
  visiting: number | null;
  visit: EstateVisitDto | null;
  inboxOpen: boolean;
  inbox: InboxDto | null;
  unread: number;
  chatOpen: boolean;
  chat: ChatDto | null;
  chatChannel: string | null;
  chatMessages: Record<string, ChatMessageDto[]>;
  /** New messages per channel since the player last looked at it */
  chatUnread: Record<string, number>;
}

export const initialState: AppState = {
  phase: "loading",
  error: null,
  content: null,
  user: null,
  company: null,
  companyAt: 0,
  era: 1,
  serverOffsetMs: 0,
  connectionStatus: "disconnected",
  tool: { kind: "select" },
  buildMenuOpen: false,
  buildCategory: "energy",
  selectedBuilding: null,
  buildingDetails: null,
  selectedParcel: null,
  selectedNetwork: null,
  selectedGate: null,
  districtVersion: 0,
  networkDetails: null,
  overlay: "none",
  hoverTile: null,
  placement: null,
  pipeRoute: null,
  zoom: 1,
  toasts: [],
  report: null,
  helpOpen: false,
  researchOpen: false,
  research: [],
  hubOpen: false,
  market: null,
  exchange: null,
  neighboursOpen: false,
  neighbours: [],
  contracts: null,
  plannedRoute: null,
  plannedPartner: null,
  corporationOpen: false,
  corporations: null,
  leaderboardOpen: false,
  leaderboardKind: "netWorth",
  leaderboardDistrict: true,
  leaderboard: null,
  visiting: null,
  visit: null,
  inboxOpen: false,
  inbox: null,
  unread: 0,
  chatOpen: false,
  chat: null,
  chatChannel: null,
  chatMessages: {},
  chatUnread: {},
};

/** The big panels in the middle of the screen; one at a time */
export const MODALS = {
  researchOpen: false,
  hubOpen: false,
  neighboursOpen: false,
  corporationOpen: false,
  leaderboardOpen: false,
  buildMenuOpen: false,
} satisfies Partial<AppState>;

export const appStore = createStore<AppState>(() => initialState);

export function useAppStore<T>(selector: (state: AppState) => T): T {
  return useStore(appStore, selector);
}

export function setState(partial: Partial<AppState>) {
  appStore.setState(partial);
}

let nextToastId = 1;

export function toast(text: string, kind: Toast["kind"] = "info", durationMs = 4500) {
  const id = nextToastId++;
  appStore.setState((state) => ({ toasts: [...state.toasts.slice(-4), { id, text, kind }] }));
  setTimeout(() => dismissToast(id), durationMs);
}

export function dismissToast(id: number) {
  appStore.setState((state) => ({ toasts: state.toasts.filter((t) => t.id !== id) }));
}

/** Gold right now: the last value from the server plus what was earned since */
export function currentGold(state: Pick<AppState, "company" | "companyAt">, now = Date.now()): number {
  if (state.company === null) {
    return 0;
  }
  return Math.max(0, state.company.gold + (state.company.goldPerMinute * (now - state.companyAt)) / 60000);
}
