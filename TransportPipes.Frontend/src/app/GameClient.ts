import type { StoreApi } from "zustand/vanilla";
import type { ChunkData, GameObjectInfo, LoginResult } from "../api/contracts";
import type { HubClient } from "../api/transportConnection";
import type { GameEngine, GameEngineCallbacks } from "../game/GameEngine";
import type { TilePosition } from "../game/isometric";
import type { AppStore } from "../state/store";

interface Credentials {
  userName: string;
  password: string;
}

export const SESSION_EXPIRED_MESSAGE = "The connection was lost and you could not be logged in again.";

/**
 * Glue between the hub connection, the app store and the map engine.
 * The login is per SignalR connection, so after a reconnect it is redone silently with the
 * credentials of the last successful login (kept in memory only).
 */
export class GameClient {
  private readonly hub: HubClient;
  private readonly store: StoreApi<AppStore>;
  private credentials: Credentials | null = null;
  private engine: GameEngine | null = null;
  private unsubscribeGhost: (() => void) | null = null;

  constructor(hub: HubClient, store: StoreApi<AppStore>) {
    this.hub = hub;
    this.store = store;

    hub.on("ChunkUpdated", (chunk) => this.engine?.applyChunk(chunk));
    hub.on("GoldUpdated", (gold) => {
      const user = this.store.getState().user;
      if (user !== null && user.userName.toLowerCase() === gold.userName.toLowerCase()) {
        this.store.getState().setGold({ ...gold, receivedAt: performance.now() });
      }
    });
    hub.on("Notify", (notification) => this.store.getState().pushToast(notification));
    this.store.getState().setConnectionStatus(hub.status);
    hub.onStatusChanged((status) => this.store.getState().setConnectionStatus(status));
    hub.onReconnected(() => void this.handleReconnected());
  }

  /** Callbacks for the map engine. */
  readonly engineCallbacks: GameEngineCallbacks = {
    requestChunk: (chunkX, chunkY): Promise<ChunkData> => this.hub.invoke("RequestChunk", chunkX, chunkY),
    canRequestChunks: () => this.hub.status === "connected",
    getCurrentUserId: () => this.store.getState().user?.userId ?? null,
    onTileClicked: (tile) => this.handleTileClicked(tile),
    onHoveredTileChanged: (tile) => this.store.getState().setHoveredTile(tile),
    onZoomChanged: (scale) => this.store.getState().setZoom(scale),
  };

  attachEngine(engine: GameEngine) {
    this.detachEngine();
    this.engine = engine;
    this.store.getState().setZoom(engine.scale);
    const syncGhost = (state: AppStore) => engine.setGhost(this.ghostNameFor(state));
    syncGhost(this.store.getState());
    this.unsubscribeGhost = this.store.subscribe(syncGhost);
  }

  detachEngine() {
    this.unsubscribeGhost?.();
    this.unsubscribeGhost = null;
    this.engine = null;
  }

  zoomBy(steps: number) {
    this.engine?.zoomBy(steps);
  }

  async login(userName: string, password: string): Promise<LoginResult> {
    const result = await this.hub.invoke("Login", userName, password);
    await this.handleLoginResult(result, { userName, password });
    return result;
  }

  async register(userName: string, password: string, email: string): Promise<LoginResult> {
    const result = await this.hub.invoke("Register", userName, password, email);
    await this.handleLoginResult(result, { userName, password });
    return result;
  }

  /** Opens the build confirmation for the clicked tile when a building is selected in the menu. */
  handleTileClicked(tile: TilePosition) {
    const state = this.store.getState();
    if (state.selectedBuildType === null) {
      return;
    }
    const info = state.gameObjectInfos.find(
      (candidate) => candidate.gameObjectType === state.selectedBuildType,
    );
    state.selectBuildType(null);
    if (info !== undefined) {
      state.setPendingBuild({ info, tile });
    }
  }

  async confirmPendingBuild() {
    const pending = this.store.getState().pendingBuild;
    this.store.getState().setPendingBuild(null);
    if (pending !== null) {
      await this.build(pending.tile, pending.info.gameObjectType);
    }
  }

  async build(tile: TilePosition, gameObjectType: number) {
    await this.call(() => this.hub.invoke("BuildGameObject", tile.x, tile.y, gameObjectType));
  }

  async buildAtHoveredTile(gameObjectType: number) {
    const tile = this.hoveredTile();
    if (tile !== null) {
      await this.build(tile, gameObjectType);
    }
  }

  async deleteAtHoveredTile() {
    const tile = this.hoveredTile();
    if (tile !== null) {
      await this.call(() => this.hub.invoke("DeleteGameObject", tile.x, tile.y));
    }
  }

  async inspectHoveredTile() {
    const tile = this.hoveredTile();
    if (tile === null) {
      return;
    }
    const text = await this.call(() => this.hub.invoke("GetStatusOfGameObject", tile.x, tile.y));
    if (text === undefined) {
      return;
    }
    if (text === null || text.trim() === "") {
      this.store.getState().setInspector(null);
      this.store.getState().pushToast({
        message: `Nothing to inspect at ${tile.x}, ${tile.y}.`,
        level: "info",
        durationMs: 2500,
      });
    } else {
      this.store.getState().setInspector({ tile, text });
    }
  }

  async calculateMyIncome() {
    await this.call(() => this.hub.invoke("CalculateMyIncome"));
  }

  private hoveredTile(): TilePosition | null {
    return this.engine?.getHoveredTile() ?? this.store.getState().hoveredTile;
  }

  private ghostNameFor(state: AppStore): string | null {
    if (state.selectedBuildType === null) {
      return null;
    }
    const info: GameObjectInfo | undefined = state.gameObjectInfos.find(
      (candidate) => candidate.gameObjectType === state.selectedBuildType,
    );
    return info?.gameObjectTypeName ?? null;
  }

  private async handleLoginResult(result: LoginResult, credentials: Credentials) {
    if (!result.success) {
      return;
    }
    this.credentials = credentials;
    this.store.getState().loggedIn({ userId: result.userId, userName: result.userName });
    await this.loadSessionData();
  }

  private async loadSessionData() {
    await this.call(async () => {
      const [infos] = await Promise.all([
        this.hub.invoke("GetGameObjectInfos"),
        this.hub.invoke("CalculateMyIncome"),
      ]);
      this.store.getState().setGameObjectInfos(infos);
    });
  }

  private async handleReconnected() {
    this.engine?.refreshLoadedChunks();
    const credentials = this.credentials;
    if (credentials === null) {
      return;
    }
    try {
      const result = await this.hub.invoke("Login", credentials.userName, credentials.password);
      if (result.success) {
        this.store.getState().loggedIn({ userId: result.userId, userName: result.userName });
        await this.loadSessionData();
        return;
      }
    } catch (error) {
      console.warn("Re-login after reconnect failed", error);
    }
    this.credentials = null;
    this.store.getState().loggedOut(SESSION_EXPIRED_MESSAGE);
  }

  /** Runs a hub call; failures become an error toast. Resolves to undefined when the call failed. */
  private async call<T>(action: () => Promise<T>): Promise<T | undefined> {
    try {
      return await action();
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error);
      this.store
        .getState()
        .pushToast({ message: `Server call failed: ${message}`, level: "error", durationMs: 6000 });
      return undefined;
    }
  }
}
