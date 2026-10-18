import { createStore } from "zustand/vanilla";
import { useStore } from "zustand";
import type {
  BuildingDetailsDto,
  CompanyDto,
  GateSide,
  MarketDto,
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
};

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
