import { use, type CSSProperties } from "react";
import { objectAtlas } from "../assets/objectAtlas";
import { useFrameIndex } from "../app/hooks";
import { cropToBottomSquare, resolveFrames } from "../game/spriteFrames";

interface SpriteThumbnailProps {
  /** Game object name, e.g. "FactorySmall" or "GreyPipe". */
  name: string;
  /** Rendered width in px; the height follows the sprite's aspect ratio. */
  width: number;
  /** Crop tall buildings to their bottom square, like the original build menu. */
  crop?: boolean;
  /** Play multi-frame sprites (about 12 frames per second, like the map). */
  animate?: boolean;
}

/** A sprite from the game object atlas, drawn with CSS so it works outside the PixiJS canvas. */
export function SpriteThumbnail({ name, width, crop = false, animate = false }: SpriteThumbnailProps) {
  const atlas = use(objectAtlas());
  const resolved = atlas === null ? null : resolveFrames(name, (frame) => frame in atlas.frames);
  const frameIndex = useFrameIndex(animate && resolved !== null ? resolved.frames.length : 1, 12);

  if (atlas === null || resolved === null) {
    return (
      <span className="sprite-thumbnail sprite-thumbnail--missing" style={{ width, height: width / 2 }} />
    );
  }
  const frame = atlas.frames[resolved.frames[frameIndex] ?? resolved.frames[0]!]!;
  const rect = crop ? cropToBottomSquare(frame) : frame;
  const scale = width / rect.w;
  const style: CSSProperties = {
    width: rect.w * scale,
    height: rect.h * scale,
    backgroundImage: `url(${atlas.imageUrl})`,
    backgroundPosition: `${-rect.x * scale}px ${-rect.y * scale}px`,
    backgroundSize: `${atlas.width * scale}px ${atlas.height * scale}px`,
  };
  return <span className="sprite-thumbnail" style={style} role="img" aria-label={name} />;
}
