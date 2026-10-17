// The SignalR contract of the TransportHub (TransportPipes.Server). JSON is camelCase.

export const HUB_URL = "/hubs/transport";

/** Plain HTTP endpoint (not part of the hub) with the version of the running server. */
export const VERSION_URL = "/api/version";

export const GameObjectType = {
  None: 0,
  FactorySmall: 1,
  IronMine: 2,
  WindMoily: 200,
  SolarPanel: 201,
  Storage: 400,
  TradingBuilding: 450,
  GreyPipe: 10000,
} as const;

export type GameObjectTypeValue = (typeof GameObjectType)[keyof typeof GameObjectType];

/** Name the server uses for an empty tile in ChunkData.gameObjects. */
export const EMPTY_GAME_OBJECT = "none";

export interface LoginResult {
  success: boolean;
  message: string;
  userName: string;
  userId: number;
}

export interface ResourceInfo {
  resourceType: number;
  resourceTypeName: string;
  /** Amount per second. */
  value: number;
}

export interface GameObjectInfo {
  gameObjectType: number;
  gameObjectTypeName: string;
  price: number;
  consumptionInfo: ResourceInfo[];
  productionInfo: ResourceInfo[];
  buildTimeInSeconds: number;
}

/** One 8x8 chunk. All arrays are indexed [x][y]. */
export interface ChunkData {
  chunkX: number;
  chunkY: number;
  /** 0 grass, 1 beach, 2 water. */
  terrain: number[][];
  /** Sprite name of the game object on the tile, or "none". */
  gameObjects: string[][];
  /** Owner user id per tile (0 = no owner). */
  owners: number[][];
}

export interface GoldInfo {
  userName: string;
  currentGold: number;
  goldPerSecond: number;
}

export type NotificationLevel = "info" | "warning" | "error";

export interface ServerNotification {
  message: string;
  level: NotificationLevel;
  /** null = sticky until dismissed. */
  durationMs: number | null;
}

/** GET /api/version */
export interface VersionInfo {
  /** e.g. "1.0.5" (master) or "1.0.5-beta" (other branches). */
  version: string;
  /** The git commit the server was built from, if known. */
  commit: string | null;
}

/** Methods on the hub that the client invokes, with their return types. */
export interface ServerMethods {
  Login: (userName: string, password: string) => LoginResult;
  Register: (userName: string, password: string, email: string) => LoginResult;
  GetGameObjectInfos: () => GameObjectInfo[];
  RequestChunk: (chunkX: number, chunkY: number) => ChunkData;
  BuildGameObject: (x: number, y: number, gameObjectType: number) => void;
  DeleteGameObject: (x: number, y: number) => void;
  GetStatusOfGameObject: (x: number, y: number) => string | null;
  CalculateMyIncome: () => void;
}

/** Messages the hub pushes to the client. */
export interface ClientEvents {
  ChunkUpdated: (chunk: ChunkData) => void;
  GoldUpdated: (gold: GoldInfo) => void;
  Notify: (notification: ServerNotification) => void;
}
