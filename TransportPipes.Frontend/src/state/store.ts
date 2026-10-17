import { createStore } from "zustand/vanilla";
import { useStore } from "zustand";
import type { GameObjectInfo, NotificationLevel } from "../api/contracts";
import type { ConnectionStatus } from "../api/transportConnection";
import type { TilePosition } from "../game/isometric";
import type { GoldSnapshot } from "./gold";

export interface CurrentUser {
  userId: number;
  userName: string;
}

export interface Toast {
  id: number;
  message: string;
  level: NotificationLevel;
  /** null = sticky until dismissed. */
  durationMs: number | null;
}

export interface PendingBuild {
  info: GameObjectInfo;
  tile: TilePosition;
}

export interface InspectorState {
  tile: TilePosition;
  text: string;
}

export interface AppState {
  connectionStatus: ConnectionStatus;
  /** True once the first connection succeeded (switches the full overlay to a small banner). */
  everConnected: boolean;
  user: CurrentUser | null;
  /** Set when a silent re-login after a reconnect failed, shown in the login dialog. */
  loginNotice: string | null;
  gold: GoldSnapshot | null;
  gameObjectInfos: GameObjectInfo[];
  /** Game object type picked in the build menu; its ghost follows the cursor. */
  selectedBuildType: number | null;
  pendingBuild: PendingBuild | null;
  inspector: InspectorState | null;
  hoveredTile: TilePosition | null;
  zoom: number;
  toasts: Toast[];
  shortcutHelpOpen: boolean;
}

export interface AppActions {
  setConnectionStatus(status: ConnectionStatus): void;
  loggedIn(user: CurrentUser): void;
  loggedOut(notice?: string): void;
  setGold(gold: GoldSnapshot): void;
  setGameObjectInfos(infos: GameObjectInfo[]): void;
  selectBuildType(type: number | null): void;
  setPendingBuild(pendingBuild: PendingBuild | null): void;
  setInspector(inspector: InspectorState | null): void;
  setHoveredTile(tile: TilePosition | null): void;
  setZoom(zoom: number): void;
  pushToast(toast: Omit<Toast, "id">): void;
  dismissToast(id: number): void;
  setShortcutHelpOpen(open: boolean): void;
  /** Escape: closes the topmost thing that is open. Returns false when there was nothing. */
  dismissTopmost(): boolean;
}

export type AppStore = AppState & AppActions;

const MAX_TOASTS = 6;

export const initialState: AppState = {
  connectionStatus: "connecting",
  everConnected: false,
  user: null,
  loginNotice: null,
  gold: null,
  gameObjectInfos: [],
  selectedBuildType: null,
  pendingBuild: null,
  inspector: null,
  hoveredTile: null,
  zoom: 1,
  toasts: [],
  shortcutHelpOpen: false,
};

export function createAppStore() {
  let nextToastId = 1;
  return createStore<AppStore>()((set, get) => ({
    ...initialState,
    setConnectionStatus: (connectionStatus) =>
      set((state) => ({
        connectionStatus,
        everConnected: state.everConnected || connectionStatus === "connected",
      })),
    loggedIn: (user) => set({ user, loginNotice: null }),
    loggedOut: (notice) =>
      set({
        user: null,
        loginNotice: notice ?? null,
        gold: null,
        selectedBuildType: null,
        pendingBuild: null,
      }),
    setGold: (gold) => set({ gold }),
    setGameObjectInfos: (gameObjectInfos) => set({ gameObjectInfos }),
    selectBuildType: (selectedBuildType) => set({ selectedBuildType }),
    setPendingBuild: (pendingBuild) => set({ pendingBuild }),
    setInspector: (inspector) => set({ inspector }),
    setHoveredTile: (hoveredTile) => {
      const current = get().hoveredTile;
      if (current?.x !== hoveredTile?.x || current?.y !== hoveredTile?.y) {
        set({ hoveredTile });
      }
    },
    setZoom: (zoom) => set({ zoom }),
    pushToast: (toast) =>
      set((state) => ({ toasts: [...state.toasts, { ...toast, id: nextToastId++ }].slice(-MAX_TOASTS) })),
    dismissToast: (id) => set((state) => ({ toasts: state.toasts.filter((toast) => toast.id !== id) })),
    setShortcutHelpOpen: (shortcutHelpOpen) => set({ shortcutHelpOpen }),
    dismissTopmost: () => {
      const state = get();
      if (state.pendingBuild !== null) {
        set({ pendingBuild: null });
      } else if (state.shortcutHelpOpen) {
        set({ shortcutHelpOpen: false });
      } else if (state.selectedBuildType !== null) {
        set({ selectedBuildType: null });
      } else if (state.inspector !== null) {
        set({ inspector: null });
      } else {
        return false;
      }
      return true;
    },
  }));
}

export const appStore = createAppStore();

export function useAppStore<T>(selector: (state: AppStore) => T): T {
  return useStore(appStore, selector);
}
