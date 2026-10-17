import { describe, expect, it } from "vitest";
import type { ClientEvents, GameObjectInfo, LoginResult, ServerMethods } from "../api/contracts";
import type { ConnectionStatus, HubClient } from "../api/transportConnection";
import { createAppStore } from "../state/store";
import { GameClient, SESSION_EXPIRED_MESSAGE } from "./GameClient";

type Handlers = {
  [K in keyof ServerMethods]?: (...args: Parameters<ServerMethods[K]>) => ReturnType<ServerMethods[K]>;
};

class FakeHub implements HubClient {
  status: ConnectionStatus = "connected";
  readonly calls: Array<{ method: string; args: unknown[] }> = [];
  private readonly events = new Map<string, (...args: never[]) => void>();
  private readonly reconnected = new Set<() => void>();
  private readonly statusListeners = new Set<(status: ConnectionStatus) => void>();
  handlers: Handlers;

  constructor(handlers: Handlers) {
    this.handlers = handlers;
  }

  invoke<K extends keyof ServerMethods>(
    method: K,
    ...args: Parameters<ServerMethods[K]>
  ): Promise<ReturnType<ServerMethods[K]>> {
    this.calls.push({ method, args });
    const handler = this.handlers[method] as ((...a: unknown[]) => ReturnType<ServerMethods[K]>) | undefined;
    return Promise.resolve(
      handler === undefined ? (undefined as ReturnType<ServerMethods[K]>) : handler(...args),
    );
  }

  on<K extends keyof ClientEvents>(event: K, handler: ClientEvents[K]): () => void {
    this.events.set(event, handler as (...args: never[]) => void);
    return () => this.events.delete(event);
  }

  onStatusChanged(listener: (status: ConnectionStatus) => void): () => void {
    this.statusListeners.add(listener);
    return () => this.statusListeners.delete(listener);
  }

  onReconnected(listener: () => void): () => void {
    this.reconnected.add(listener);
    return () => this.reconnected.delete(listener);
  }

  emit<K extends keyof ClientEvents>(event: K, ...args: Parameters<ClientEvents[K]>) {
    (this.events.get(event) as ((...a: Parameters<ClientEvents[K]>) => void) | undefined)?.(...args);
  }

  async simulateReconnect() {
    this.reconnected.forEach((listener) => listener());
    await new Promise((resolve) => setTimeout(resolve, 0));
  }

  callsTo(method: keyof ServerMethods) {
    return this.calls.filter((call) => call.method === method);
  }
}

const success: LoginResult = { success: true, message: "", userName: "Devedse", userId: 7 };
const windmill: GameObjectInfo = {
  gameObjectType: 200,
  gameObjectTypeName: "WindMoily",
  price: 100,
  consumptionInfo: [],
  productionInfo: [{ resourceType: 0, resourceTypeName: "Energy", value: 10 / 3600 }],
  buildTimeInSeconds: 60,
};

function setup(handlers: Handlers) {
  const hub = new FakeHub(handlers);
  const store = createAppStore();
  const client = new GameClient(hub, store);
  return { hub, store, client };
}

describe("GameClient login", () => {
  it("stores the user and loads the session data after a successful login", async () => {
    const { hub, store, client } = setup({ Login: () => success, GetGameObjectInfos: () => [windmill] });

    const result = await client.login("Devedse", "a");

    expect(result.success).toBe(true);
    expect(hub.callsTo("Login")[0]?.args).toEqual(["Devedse", "a"]);
    expect(store.getState().user).toEqual({ userId: 7, userName: "Devedse" });
    expect(store.getState().gameObjectInfos).toEqual([windmill]);
    expect(hub.callsTo("CalculateMyIncome")).toHaveLength(1);
  });

  it("leaves the store alone when the login fails", async () => {
    const failure: LoginResult = {
      success: false,
      message: "Error: invalid username or password...",
      userName: "",
      userId: 0,
    };
    const { store, client } = setup({ Login: () => failure });

    const result = await client.login("Devedse", "wrong");

    expect(result).toEqual(failure);
    expect(store.getState().user).toBeNull();
  });

  it("only applies gold updates for the logged in user", async () => {
    const { hub, store, client } = setup({ Login: () => success, GetGameObjectInfos: () => [] });
    await client.login("Devedse", "a");

    hub.emit("GoldUpdated", { userName: "someone else", currentGold: 1, goldPerSecond: 1 });
    expect(store.getState().gold).toBeNull();

    hub.emit("GoldUpdated", { userName: "devedse", currentGold: 250, goldPerSecond: 0.5 });
    expect(store.getState().gold).toMatchObject({ currentGold: 250, goldPerSecond: 0.5 });
  });
});

describe("GameClient reconnect", () => {
  it("logs in again silently with the last credentials", async () => {
    const { hub, store, client } = setup({ Login: () => success, GetGameObjectInfos: () => [] });
    await client.login("Devedse", "a");

    await hub.simulateReconnect();

    expect(hub.callsTo("Login")).toHaveLength(2);
    expect(hub.callsTo("Login")[1]?.args).toEqual(["Devedse", "a"]);
    expect(store.getState().user?.userId).toBe(7);
  });

  it("shows the login dialog again when the silent login fails", async () => {
    const { hub, store, client } = setup({ Login: () => success, GetGameObjectInfos: () => [] });
    await client.login("Devedse", "a");
    hub.handlers.Login = () => ({ success: false, message: "nope", userName: "", userId: 0 });

    await hub.simulateReconnect();

    expect(store.getState().user).toBeNull();
    expect(store.getState().loginNotice).toBe(SESSION_EXPIRED_MESSAGE);
  });

  it("does not log in after a reconnect when nobody was logged in", async () => {
    const { hub } = setup({ Login: () => success });
    await hub.simulateReconnect();
    expect(hub.callsTo("Login")).toHaveLength(0);
  });
});

describe("GameClient building", () => {
  it("turns a map click with a selected building into a pending build, then builds it", async () => {
    const { hub, store, client } = setup({ Login: () => success, GetGameObjectInfos: () => [windmill] });
    await client.login("Devedse", "a");
    store.getState().selectBuildType(200);

    client.handleTileClicked({ x: 3, y: -2 });
    expect(store.getState().selectedBuildType).toBeNull();
    expect(store.getState().pendingBuild).toEqual({ info: windmill, tile: { x: 3, y: -2 } });

    await client.confirmPendingBuild();
    expect(store.getState().pendingBuild).toBeNull();
    expect(hub.callsTo("BuildGameObject")[0]?.args).toEqual([3, -2, 200]);
  });

  it("ignores map clicks without a selected building", () => {
    const { store, client } = setup({});
    client.handleTileClicked({ x: 1, y: 1 });
    expect(store.getState().pendingBuild).toBeNull();
  });

  it("shows failed server calls as an error toast", async () => {
    const { hub, store, client } = setup({});
    hub.invoke = (() => Promise.reject(new Error("boom"))) as typeof hub.invoke;

    await client.build({ x: 0, y: 0 }, 1);

    expect(store.getState().toasts).toHaveLength(1);
    expect(store.getState().toasts[0]).toMatchObject({ level: "error" });
  });
});
