export const CHUNK_SIZE = 8;
export const TILE_WIDTH = 96;
export const TILE_HEIGHT = 96;

export const MIN_ZOOM = 0.3;
export const MAX_ZOOM = 2;
export const ZOOM_STEP = 0.1;

/** Pointer movement (in px) below which a press/release counts as a click instead of a drag. */
export const CLICK_TOLERANCE_PX = 5;

/** Frames per tick for animated sprites (the original game used 0.2). */
export const ANIMATION_SPEED = 0.2;

export const TERRAIN_FRAMES = ["grass.png", "beach.png", "water.png", "loading.png"] as const;
export const LOADING_TERRAIN = 3;

export const NOT_OWNED_TINT = 0x886666;
export const OWNED_TINT = 0xffffff;

export const OBJECT_ATLAS_URL = "/images/CombinedGameObjects/combinedjson.json";
export const OBJECT_ATLAS_IMAGE_URL = "/images/CombinedGameObjects/combinedimage.png";
export const TERRAIN_ATLAS_URL = "/images/roadTiles/transportTiles.json";
