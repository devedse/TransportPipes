import { HubConnectionBuilder, LogLevel, type HubConnection } from "@microsoft/signalr";
import { HUB_URL, type ClientEvents, type ServerMethods } from "./contracts";

export type ConnectionStatus = "connecting" | "connected" | "reconnecting" | "disconnected";

type Listener<T> = (value: T) => void;

/** What the rest of the app needs from the hub connection (lets tests swap in a fake). */
export interface HubClient {
  readonly status: ConnectionStatus;
  invoke<K extends keyof ServerMethods>(
    method: K,
    ...args: Parameters<ServerMethods[K]>
  ): Promise<ReturnType<ServerMethods[K]>>;
  on<K extends keyof ClientEvents>(event: K, handler: ClientEvents[K]): () => void;
  onStatusChanged(listener: Listener<ConnectionStatus>): () => void;
  /** Fires after the connection came back (automatic reconnect or a full restart). */
  onReconnected(listener: () => void): () => void;
}

const START_RETRY_DELAYS_MS = [0, 2000, 5000, 10000];

/**
 * Typed wrapper around the SignalR HubConnection.
 * withAutomaticReconnect only covers drops after a successful start, so the initial start
 * (and a restart after the automatic reconnect gives up) is retried here forever.
 */
export class TransportConnection implements HubClient {
  private readonly connection: HubConnection;
  private readonly statusListeners = new Set<Listener<ConnectionStatus>>();
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

  invoke<K extends keyof ServerMethods>(
    method: K,
    ...args: Parameters<ServerMethods[K]>
  ): Promise<ReturnType<ServerMethods[K]>> {
    return this.connection.invoke<ReturnType<ServerMethods[K]>>(method, ...args);
  }

  on<K extends keyof ClientEvents>(event: K, handler: ClientEvents[K]): () => void {
    const untypedHandler = handler as (...args: unknown[]) => void;
    this.connection.on(event, untypedHandler);
    return () => this.connection.off(event, untypedHandler);
  }

  onStatusChanged(listener: Listener<ConnectionStatus>): () => void {
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
}
