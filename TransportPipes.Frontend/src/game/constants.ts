export { CHUNK_SIZE } from "./layout";

/** Half the width of a tile diamond (a tile is 192×96 pixels, like the original game) */
export const TILE_WIDTH = 96;
export const TILE_HEIGHT = 96;

export const MIN_ZOOM = 0.04;
export const MAX_ZOOM = 1.5;
/** Every zoom step multiplies or divides the scale by this */
export const ZOOM_FACTOR = 1.18;

/** Below this zoom the map shows estates and districts instead of every tile */
export const FAR_ZOOM = 0.16;

/** Pointer movement (in px) below which a press/release counts as a click instead of a drag. */
export const CLICK_TOLERANCE_PX = 5;

/** Frames per tick for animated sprites (the original game used 0.2). */
export const ANIMATION_SPEED = 0.2;
