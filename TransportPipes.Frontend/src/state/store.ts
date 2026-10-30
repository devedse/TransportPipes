import { createStore } from "zustand/vanilla";
import { useStore } from "zustand";
import type {
  NextToMeDto,
  BuildingDetailsDto,
  ChatDto,
  ChatMessageDto,
  NextStepDto,
  CompanyDetailsDto,
  OutpostCheckDto,
  CompanyDto,
  EstateVisitDto,
  ExchangeDto,
  InboxDto,
  LeaderboardDto,
  LeaderboardKind,
  StatisticsDto,
  StatsRange,
  TownDto,
  TrackStateDto,
  LinesDto,
  GoldUsesDto,
  MarketDto,
  NeighbourDto,
  NetworkDetailsDto,
  BlueprintsDto,
  ResearchStateDto,
  UserInfo,
  YardDto,
  LinksDto,
  MapLinkDto,
  ReturnCardDto,
  CrewViewDto,
  SeatRefDto,
  SeatState,
} from "../api/contracts";
import type { ConnectionStatus } from "../api/server";
import type { Content } from "../game/content";
import type { Overlay, Tool } from "../game/Engine";
import type { TilePosition } from "../game/isometric";
import type { Placement } from "../game/rules";

export type Phase = "loading" | "login" | "found" | "playing" | "error";

/**
 * Picking a plot on the map (1.4 §6): to start, or to move. The map shows the plots of the towns the picker loaded (by
 * "x,y,slot") as free, closed, taken or yours, and the chosen one; a tap on a plot chooses it.
 */
export interface PickerState {
  mode: "start" | "move";
  seats: Record<string, SeatState>;
  chosen: SeatRefDto | null;
}

export function seatKey(seat: SeatRefDto): string {
  return `${seat.districtX},${seat.districtY},${seat.slot}`;
}

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
  /** The list of mouse actions and keys */
  keysOpen: boolean;
  /** What's new: the changes since the player last looked (after an update), or the latest (version badge) */
  whatsNewOpen: false | "new" | "latest";
  buildingDetails: BuildingDetailsDto | null;
  selectedParcel: number | null;
  /** Which of the company's estates the selected parcel belongs to (0 is the first, then the branches) */
  selectedParcelEstate: number;
  /** One of the nine outskirts parcels set aside for your plot (1.4 §5), to buy */
  selectedOutskirts: number | null;
  selectedNetwork: number | null;
  /** The Yard panel: your station, its residents and your trades with them */
  yardOpen: boolean;
  yard: YardDto | null;
  /** The Yard panel's tab: your station, or your links to other stations */
  yardTab: "yard" | "links";
  /** The return card (F5): what happened since the last session, until the player continues */
  returnCard: ReturnCardDto | null;
  /** When the player last left (the chat marks what came since) */
  leftAt: string | null;
  links: LinksDto | null;
  /** A friend the Links tab's new link starts with (the Crew panel's "Link ▸", after run 3, row 12) */
  linkPartner: number | null;
  /** Every link on the map, and the one the Links tab points at */
  mapLinks: MapLinkDto[];
  focusLink: number | null;
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
  helpOpen: boolean;
  researchOpen: boolean;
  research: ResearchStateDto[];
  /** The Lab's tracks (1.4 §12), loaded with the research panel */
  tracks: TrackStateDto[];
  hubOpen: boolean;
  hubTab: HubTab;
  market: MarketDto | null;
  /** Your town (1.4 §11, §13): its Era, its Wonder, your Yard's Monuments */
  town: TownDto | null;
  /** Your town's trunk lines (1.4 §20) */
  lines: LinesDto | null;
  /** What your gold can do (after run 4), loaded when the top bar's gold is opened */
  goldUses: GoldUsesDto | null;
  exchange: ExchangeDto | null;
  neighboursOpen: boolean;
  neighboursTab: "neighbours" | "trade";
  neighbours: NeighbourDto[];
  /** A pipe route the player is looking at (Connect), drawn on the map until it is built or dropped */
  plannedRoute: TilePosition[] | null;
  plannedPartner: number | null;
  /** Where a planned pipe goes, when it isn't to a neighbour ("your Trading Post"), and what to say once it is laid */
  plannedTarget: string | null;
  /** The building just placed (its corner and size): until the pointer leaves it, the hover tip says so, not "in the way" */
  justBuilt: { x: number; y: number; size: number; name: string } | null;
  plannedDone: string | null;

  /** Your Yard's crew (1.4 §7): its name and banner, open or closed, its residents, the requests, your link */
  crewOpen: boolean;
  crew: CrewViewDto | null;
  /** The "Play next to me" link this visit came with, and what it opens */
  invite: { code: string; dto: NextToMeDto } | null;
  picker: PickerState | null;
  /** Counts up when the picker must load its town again (a plot taken a moment ago) */
  pickerReload: number;
  /** The form that turns a guest into an account */
  keepOpen: boolean;
  /** A guest reached a keep level: the top bar's "Keep my plot" glows until it is tapped */
  keepNudge: boolean;
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
  moderationOpen: boolean;
  leaderboardOpen: boolean;
  leaderboardKind: LeaderboardKind;
  leaderboardDistrict: boolean;
  leaderboard: LeaderboardDto | null;
  /** Statistics (docs/design-notes/statistics.md): whose (null: yours), the range, the graph shown, the numbers */
  statsOpen: boolean;
  statsCompany: number | null;
  statsRange: StatsRange;
  statsMetric: string;
  stats: StatisticsDto | null;
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
  keysOpen: false,
  whatsNewOpen: false,
  buildingDetails: null,
  selectedParcel: null,
  selectedParcelEstate: 0,
  selectedOutskirts: null,
  selectedNetwork: null,
  yardOpen: false,
  yard: null,
  yardTab: "yard",
  returnCard: null,
  leftAt: null,
  links: null,
  linkPartner: null,
  mapLinks: [],
  focusLink: null,
  districtVersion: 0,
  networkDetails: null,
  buildingNetwork: null,
  overlay: "none",
  hoverTile: null,
  placement: null,
  pipeRoute: null,
  zoom: 1,
  toasts: [],
  helpOpen: false,
  researchOpen: false,
  research: [],
  tracks: [],
  hubOpen: false,
  hubTab: "orders",
  market: null,
  town: null,
  lines: null,
  goldUses: null,
  exchange: null,
  neighboursOpen: false,
  neighboursTab: "neighbours",
  neighbours: [],
  plannedRoute: null,
  plannedPartner: null,
  plannedTarget: null,
  justBuilt: null,
  plannedDone: null,
  crewOpen: false,
  crew: null,
  invite: null,
  picker: null,
  pickerReload: 0,
  keepOpen: false,
  keepNudge: false,
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
  moderationOpen: false,
  leaderboardOpen: false,
  leaderboardKind: "netWorth",
  leaderboardDistrict: true,
  leaderboard: null,
  statsOpen: false,
  statsCompany: null,
  statsRange: "week",
  statsMetric: "income",
  stats: null,
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
  crewOpen: false,
  leaderboardOpen: false,
  statsOpen: false,
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
  yardOpen: { yardOpen: false },
  selectedParcel: { selectedParcel: null },
  selectedOutskirts: { selectedOutskirts: null },
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

/** Shows a toast; returns its id (dismissToast takes it away early) */
export function toast(text: string, kind: Toast["kind"] = "info", durationMs = 4500): number {
  const id = nextToastId++;
  const same = appStore.getState().toasts.find((t) => t.text === text && t.kind === kind);
  if (same !== undefined) {
    // One toast with a count; the new id restarts its timer
    appStore.setState((state) => ({
      toasts: state.toasts.map((t) => (t.id === same.id ? { ...t, id, count: (t.count ?? 1) + 1 } : t)),
    }));
    setTimeout(() => dismissToast(id), durationMs);
    return id;
  }
  appStore.setState((state) => ({ toasts: [...state.toasts.slice(-4), { id, text, kind }] }));
  setTimeout(() => dismissToast(id), durationMs);
  return id;
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
