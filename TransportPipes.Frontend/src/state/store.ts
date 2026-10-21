import { createStore } from "zustand/vanilla";
import { useStore } from "zustand";
import type {
  BuildingDetailsDto,
  ChatDto,
  ChatMessageDto,
  NextStepDto,
  CompanyDetailsDto,
  OutpostCheckDto,
  SeasonDto,
  CompanyDto,
  ContractsDto,
  CorporationsDto,
  EstateVisitDto,
  ExchangeDto,
  InboxDto,
  LeaderboardDto,
  LeaderboardKind,
  WondersDto,
  GateSide,
  MarketDto,
  NeighbourDto,
  NetworkDetailsDto,
  ReportDto,
  BlueprintsDto,
  ResearchStateDto,
  UserInfo,
} from "../api/contracts";
import type { ConnectionStatus } from "../api/server";
import type { Content } from "../game/content";
import type { Overlay, Tool } from "../game/Engine";
import type { TilePosition } from "../game/isometric";
import type { Placement } from "../game/rules";

export type Phase = "loading" | "login" | "found" | "playing" | "error";

export type HubTab = "orders" | "market" | "calendar" | "wonders" | "exchange";

export interface Toast {
  id: number;
  text: string;
  kind: "info" | "success" | "error" | "level";
  /** The same message more than once (a blueprint finishing, a burst of sales) */
  count?: number;
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
  /** A new version of the game is out: the page reloads */
  updateAvailable: boolean;

  tool: Tool;
  buildMenuOpen: boolean;
  buildCategory: string;
  selectedBuilding: number | null;
  /** Several buildings selected together (the selection panel) */
  selection: number[];
  /** The advisor's next steps (GameWorld.NextSteps) */
  nextSteps: NextStepDto[];
  buildingDetails: BuildingDetailsDto | null;
  selectedParcel: number | null;
  /** Which of the company's estates the selected parcel belongs to (0 is the first, then the branches) */
  selectedParcelEstate: number;
  selectedNetwork: number | null;
  selectedGate: GateSide | null;
  /** Counts district updates, so panels that read the world model render again */
  districtVersion: number;
  networkDetails: NetworkDetailsDto | null;
  /** The network of the selected building */
  buildingNetwork: NetworkDetailsDto | null;
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
  hubTab: HubTab;
  market: MarketDto | null;
  wonders: WondersDto | null;
  exchange: ExchangeDto | null;
  neighboursOpen: boolean;
  neighboursTab: "neighbours" | "contracts" | "board" | "trade";
  neighbours: NeighbourDto[];
  contracts: ContractsDto | null;
  /** A pipe route the player is looking at (Connect), drawn on the map until it is built or dropped */
  plannedRoute: TilePosition[] | null;
  plannedPartner: number | null;

  corporationOpen: boolean;
  corporations: CorporationsDto | null;
  companyOpen: boolean;
  companyDetails: CompanyDetailsDto | null;
  blueprintsOpen: boolean;
  blueprints: BlueprintsDto | null;
  /** The camera looks at the Deep Level */
  deepView: boolean;
  /** A parcel of the Deep Level the player clicked (to dig it out) */
  selectedDeepParcel: number | null;
  /** An area of the estate the player dragged, to be named and saved as a blueprint */
  blueprintDraft: { x: number; y: number; width: number; height: number } | null;
  /** A wilderness tile the player clicked: an outpost there, or the claim it would make */
  wildTile: TilePosition | null;
  /** Where Loady suggests to build (a pulsing marker), with the footprint size */
  hintTile: { x: number; y: number; size: number } | null;
  /** Photo mode: no UI, a time of day */
  photoMode: boolean;
  /** The map has been drawn where the camera looks (a loading picture covers it until then) */
  mapShown: boolean;
  timeOfDay: "day" | "golden" | "dusk" | "night";
  /** A Company Level just reached: the celebration shows */
  levelUp: number | null;
  outpostCheck: OutpostCheckDto | null;
  /** The season results, shown when the season ended */
  season: SeasonDto | null;
  seasonOpen: boolean;
  moderationOpen: boolean;
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
  updateAvailable: false,
  tool: { kind: "select" },
  buildMenuOpen: false,
  buildCategory: "energy",
  selectedBuilding: null,
  selection: [],
  nextSteps: [],
  buildingDetails: null,
  selectedParcel: null,
  selectedParcelEstate: 0,
  selectedNetwork: null,
  selectedGate: null,
  districtVersion: 0,
  networkDetails: null,
  buildingNetwork: null,
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
  hubTab: "orders",
  market: null,
  wonders: null,
  exchange: null,
  neighboursOpen: false,
  neighboursTab: "neighbours",
  neighbours: [],
  contracts: null,
  plannedRoute: null,
  plannedPartner: null,
  corporationOpen: false,
  corporations: null,
  companyOpen: false,
  companyDetails: null,
  blueprintsOpen: false,
  blueprints: null,
  blueprintDraft: null,
  deepView: false,
  selectedDeepParcel: null,
  wildTile: null,
  hintTile: null,
  levelUp: null,
  photoMode: false,
  mapShown: false,
  timeOfDay: "day",
  outpostCheck: null,
  season: null,
  seasonOpen: false,
  moderationOpen: false,
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
  companyOpen: false,
  blueprintsOpen: false,
  moderationOpen: false,
  buildMenuOpen: false,
} satisfies Partial<AppState>;

export const appStore = createStore<AppState>(() => initialState);

export function useAppStore<T>(selector: (state: AppState) => T): T {
  return useStore(appStore, selector);
}

/**
 * The panels on the right side of the map: one at a time. Opening one (a building, a network, a gate, a parcel,
 * the wilderness, a visit, the inbox) closes the others.
 */
const SIDE_PANELS = {
  selectedBuilding: { selectedBuilding: null, buildingDetails: null, buildingNetwork: null },
  selection: { selection: [] },
  selectedNetwork: { selectedNetwork: null, networkDetails: null },
  selectedGate: { selectedGate: null },
  selectedParcel: { selectedParcel: null },
  selectedDeepParcel: { selectedDeepParcel: null },
  wildTile: { wildTile: null, outpostCheck: null },
  visiting: { visiting: null, visit: null },
  inboxOpen: { inboxOpen: false },
} satisfies Partial<Record<keyof AppState, Partial<AppState>>>;

export function setState(partial: Partial<AppState>) {
  const opened = (Object.keys(SIDE_PANELS) as Array<keyof typeof SIDE_PANELS>).find((key) => {
    const value = partial[key];
    return (
      key in partial && value !== null && value !== false && !(Array.isArray(value) && value.length === 0)
    );
  });
  if (opened === undefined) {
    appStore.setState(partial);
    return;
  }
  const closing: Partial<AppState> = {};
  for (const [key, reset] of Object.entries(SIDE_PANELS)) {
    if (key !== opened && !(key in partial)) {
      Object.assign(closing, reset);
    }
  }
  appStore.setState({ ...closing, ...partial });
}

let nextToastId = 1;

export function toast(text: string, kind: Toast["kind"] = "info", durationMs = 4500) {
  const id = nextToastId++;
  const same = appStore.getState().toasts.find((t) => t.text === text && t.kind === kind);
  if (same !== undefined) {
    // One toast with a count; the new id restarts its timer
    appStore.setState((state) => ({
      toasts: state.toasts.map((t) => (t.id === same.id ? { ...t, id, count: (t.count ?? 1) + 1 } : t)),
    }));
    setTimeout(() => dismissToast(id), durationMs);
    return;
  }
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
