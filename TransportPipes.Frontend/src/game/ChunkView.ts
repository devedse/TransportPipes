import { Sprite, type Container } from "pixi.js";
import { EMPTY_GAME_OBJECT, type ChunkData } from "../api/contracts";
import { CHUNK_SIZE, LOADING_TERRAIN, NOT_OWNED_TINT, OWNED_TINT } from "./constants";
import { diagonalToScreen } from "./isometric";
import type { SpriteFactory } from "./SpriteFactory";

interface PlacedObject {
  sprite: Sprite;
  ownerId: number;
}

export type ChunkState = "loading" | "loaded" | "failed";

function tileIndex(localX: number, localY: number) {
  return localX * CHUNK_SIZE + localY;
}

/**
 * The sprites of one 8x8 chunk. Terrain goes into the shared terrain layer and game objects into
 * the shared, depth-sorted objects layer, so tall buildings overlap neighbouring chunks correctly.
 */
export class ChunkView {
  readonly chunkX: number;
  readonly chunkY: number;
  state: ChunkState = "loading";
  /** When a failed request may be retried (performance.now() ms). */
  retryAt = 0;
  private readonly tiles: Sprite[] = [];
  private readonly objects = new Map<number, PlacedObject>();
  private readonly factory: SpriteFactory;
  private readonly objectsLayer: Container;
  private destroyed = false;

  constructor(
    chunkX: number,
    chunkY: number,
    factory: SpriteFactory,
    terrainLayer: Container,
    objectsLayer: Container,
  ) {
    this.chunkX = chunkX;
    this.chunkY = chunkY;
    this.factory = factory;
    this.objectsLayer = objectsLayer;
    for (let x = 0; x < CHUNK_SIZE; x++) {
      for (let y = 0; y < CHUNK_SIZE; y++) {
        const tile = new Sprite(factory.terrainTexture(LOADING_TERRAIN));
        this.placeAtTile(tile, x, y);
        this.tiles[tileIndex(x, y)] = tile;
        terrainLayer.addChild(tile);
      }
    }
  }

  get isDestroyed() {
    return this.destroyed;
  }

  apply(data: ChunkData, currentUserId: number | null) {
    if (this.destroyed) {
      return;
    }
    for (let x = 0; x < CHUNK_SIZE; x++) {
      for (let y = 0; y < CHUNK_SIZE; y++) {
        this.tiles[tileIndex(x, y)]!.texture = this.factory.terrainTexture(
          data.terrain[x]?.[y] ?? LOADING_TERRAIN,
        );
      }
    }

    this.clearObjects();
    for (let x = 0; x < CHUNK_SIZE; x++) {
      for (let y = 0; y < CHUNK_SIZE; y++) {
        const name = data.gameObjects[x]?.[y] ?? EMPTY_GAME_OBJECT;
        if (name === EMPTY_GAME_OBJECT) {
          continue;
        }
        const sprite = this.factory.createGameObject(name);
        if (sprite === null) {
          continue;
        }
        this.placeAtTile(sprite, x, y);
        const globalX = this.chunkX * CHUNK_SIZE + x;
        const globalY = this.chunkY * CHUNK_SIZE + y;
        sprite.zIndex = globalX + globalY;
        this.objectsLayer.addChild(sprite);
        this.objects.set(tileIndex(x, y), { sprite, ownerId: data.owners[x]?.[y] ?? 0 });
      }
    }
    this.applyOwnership(currentUserId);
    this.state = "loaded";
  }

  /** Objects of other players (or everything while logged out) are tinted. */
  applyOwnership(currentUserId: number | null) {
    for (const placed of this.objects.values()) {
      placed.sprite.tint = placed.ownerId === currentUserId ? OWNED_TINT : NOT_OWNED_TINT;
    }
  }

  tileSprite(localX: number, localY: number): Sprite | undefined {
    return this.tiles[tileIndex(localX, localY)];
  }

  objectSprite(localX: number, localY: number): Sprite | undefined {
    return this.objects.get(tileIndex(localX, localY))?.sprite;
  }

  destroy() {
    if (this.destroyed) {
      return;
    }
    this.destroyed = true;
    this.clearObjects();
    for (const tile of this.tiles) {
      tile.destroy();
    }
    this.tiles.length = 0;
  }

  private clearObjects() {
    for (const placed of this.objects.values()) {
      placed.sprite.destroy();
    }
    this.objects.clear();
  }

  private placeAtTile(sprite: Sprite, localX: number, localY: number) {
    const position = diagonalToScreen(this.chunkX * CHUNK_SIZE + localX, this.chunkY * CHUNK_SIZE + localY);
    sprite.anchor.set(0, 1);
    sprite.position.set(position.x, position.y);
  }
}
