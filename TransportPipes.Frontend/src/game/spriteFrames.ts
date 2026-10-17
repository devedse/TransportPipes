export interface FrameRect {
  x: number;
  y: number;
  w: number;
  h: number;
}

export interface ResolvedFrames {
  /** The name that actually matched in the atlas (may differ from the requested one). */
  name: string;
  /** Atlas frame names, one for a static sprite, several for an animation. */
  frames: string[];
  animated: boolean;
}

/** Suffix for the pipe variant with connections on all four sides. */
const PIPE_FALLBACK_SUFFIX = "urdl";

/**
 * Resolves a game object name to its atlas frames: "Name" for a static sprite, or
 * "Name-0" .. "Name-N" for an animation. Names without frames (such as the plain "GreyPipe"
 * from the build menu) fall back to their four-way variant "Name" + "urdl".
 */
export function resolveFrames(name: string, hasFrame: (frameName: string) => boolean): ResolvedFrames | null {
  for (const candidate of [name, name + PIPE_FALLBACK_SUFFIX]) {
    if (hasFrame(candidate)) {
      return { name: candidate, frames: [candidate], animated: false };
    }
    const animationFrames: string[] = [];
    while (hasFrame(`${candidate}-${animationFrames.length}`)) {
      animationFrames.push(`${candidate}-${animationFrames.length}`);
    }
    if (animationFrames.length > 0) {
      return { name: candidate, frames: animationFrames, animated: animationFrames.length > 1 };
    }
  }
  return null;
}

const THUMBNAIL_SIZE = 192;

/**
 * Tall building sprites (192 wide, taller than 192) are cropped to their bottom 192x192 square
 * for thumbnails, so the base of the building stays visible.
 */
export function cropToBottomSquare(frame: FrameRect): FrameRect {
  if (frame.w !== THUMBNAIL_SIZE || frame.h <= THUMBNAIL_SIZE) {
    return frame;
  }
  return { x: frame.x, y: frame.y + frame.h - THUMBNAIL_SIZE, w: THUMBNAIL_SIZE, h: THUMBNAIL_SIZE };
}
