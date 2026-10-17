import { AnimatedSprite, Assets, Sprite, type Spritesheet, type Texture } from "pixi.js";
import {
  ANIMATION_SPEED,
  LOADING_TERRAIN,
  OBJECT_ATLAS_URL,
  TERRAIN_ATLAS_URL,
  TERRAIN_FRAMES,
} from "./constants";
import { resolveFrames } from "./spriteFrames";

/** Creates terrain and game object sprites from the two spritesheets. */
export class SpriteFactory {
  private readonly textures: ReadonlyMap<string, Texture>;
  private readonly terrainTextures: Texture[];

  private constructor(textures: ReadonlyMap<string, Texture>) {
    this.textures = textures;
    this.terrainTextures = TERRAIN_FRAMES.map((frame) => {
      const texture = textures.get(frame);
      if (texture === undefined) {
        throw new Error(`Terrain frame '${frame}' is missing from ${TERRAIN_ATLAS_URL}`);
      }
      return texture;
    });
  }

  static async load(): Promise<SpriteFactory> {
    const sheets = await Promise.all([
      Assets.load<Spritesheet>(OBJECT_ATLAS_URL),
      Assets.load<Spritesheet>(TERRAIN_ATLAS_URL),
    ]);
    const textures = new Map<string, Texture>();
    for (const sheet of sheets) {
      for (const [name, texture] of Object.entries(sheet.textures)) {
        textures.set(name, texture);
      }
    }
    return new SpriteFactory(textures);
  }

  /** Texture for a terrain type from the server (0 grass, 1 beach, 2 water); anything else shows "loading". */
  terrainTexture(terrainType: number): Texture {
    return this.terrainTextures[terrainType] ?? this.terrainTextures[LOADING_TERRAIN]!;
  }

  /** A sprite (animated when the atlas has several frames) for a game object, or null if unknown. */
  createGameObject(name: string): Sprite | null {
    const resolved = resolveFrames(name, (frame) => this.textures.has(frame));
    if (resolved === null) {
      console.warn(`No sprite for game object '${name}'`);
      return null;
    }
    const textures = resolved.frames.map((frame) => this.textures.get(frame)!);
    if (resolved.animated) {
      const animated = new AnimatedSprite(textures);
      animated.animationSpeed = ANIMATION_SPEED;
      animated.play();
      return animated;
    }
    return new Sprite(textures[0]);
  }
}
