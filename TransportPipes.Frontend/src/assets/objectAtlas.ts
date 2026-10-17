import { OBJECT_ATLAS_IMAGE_URL, OBJECT_ATLAS_URL } from "../game/constants";
import type { FrameRect } from "../game/spriteFrames";

/** The game object atlas as plain data, for DOM thumbnails (the map itself uses PixiJS). */
export interface ObjectAtlas {
  imageUrl: string;
  width: number;
  height: number;
  frames: Record<string, FrameRect>;
}

interface AtlasJson {
  frames: Record<string, { frame: FrameRect }>;
  meta: { size: { w: number; h: number } };
}

async function fetchObjectAtlas(): Promise<ObjectAtlas | null> {
  try {
    const response = await fetch(OBJECT_ATLAS_URL);
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}`);
    }
    const json = (await response.json()) as AtlasJson;
    const frames: Record<string, FrameRect> = {};
    for (const [name, entry] of Object.entries(json.frames)) {
      frames[name] = entry.frame;
    }
    return { imageUrl: OBJECT_ATLAS_IMAGE_URL, width: json.meta.size.w, height: json.meta.size.h, frames };
  } catch (error) {
    console.warn("Could not load the sprite atlas for thumbnails", error);
    return null;
  }
}

let atlasPromise: Promise<ObjectAtlas | null> | null = null;

/** Loaded once and shared; resolves to null when it cannot be loaded (thumbnails are then hidden). */
export function objectAtlas(): Promise<ObjectAtlas | null> {
  atlasPromise ??= fetchObjectAtlas();
  return atlasPromise;
}
