import { describe, expect, it } from "vitest";
import { cropToBottomSquare, resolveFrames } from "./spriteFrames";

const atlasFrames = new Set([
  "IronMine",
  "Storage",
  "GreyPipeurdl",
  "GreyPiperl",
  ...Array.from({ length: 7 }, (_, index) => `WindMoily-${index}`),
  ...Array.from({ length: 28 }, (_, index) => `UnderConstruction-${index}`),
  "Single-0",
]);
const hasFrame = (frame: string) => atlasFrames.has(frame);

describe("resolveFrames", () => {
  it("resolves static sprites by name", () => {
    expect(resolveFrames("IronMine", hasFrame)).toEqual({
      name: "IronMine",
      frames: ["IronMine"],
      animated: false,
    });
    expect(resolveFrames("GreyPiperl", hasFrame)?.frames).toEqual(["GreyPiperl"]);
  });

  it("collects numbered frames into an animation", () => {
    const windmill = resolveFrames("WindMoily", hasFrame);
    expect(windmill?.animated).toBe(true);
    expect(windmill?.frames).toHaveLength(7);
    expect(windmill?.frames[0]).toBe("WindMoily-0");
    expect(windmill?.frames[6]).toBe("WindMoily-6");
    expect(resolveFrames("UnderConstruction", hasFrame)?.frames).toHaveLength(28);
  });

  it("treats a single numbered frame as static", () => {
    expect(resolveFrames("Single", hasFrame)).toEqual({
      name: "Single",
      frames: ["Single-0"],
      animated: false,
    });
  });

  it("falls back to the four-way pipe for names without frames", () => {
    expect(resolveFrames("GreyPipe", hasFrame)).toEqual({
      name: "GreyPipeurdl",
      frames: ["GreyPipeurdl"],
      animated: false,
    });
  });

  it("returns null for unknown names", () => {
    expect(resolveFrames("Nope", hasFrame)).toBeNull();
  });
});

describe("cropToBottomSquare", () => {
  it("crops tall 192 wide sprites to their bottom 192x192", () => {
    expect(cropToBottomSquare({ x: 386, y: 351, w: 192, h: 350 })).toEqual({
      x: 386,
      y: 509,
      w: 192,
      h: 192,
    });
  });

  it("leaves other sprites alone", () => {
    const pipe = { x: 0, y: 4315, w: 192, h: 150 };
    expect(cropToBottomSquare(pipe)).toBe(pipe);
    const exactSquare = { x: 0, y: 0, w: 192, h: 192 };
    expect(cropToBottomSquare(exactSquare)).toBe(exactSquare);
    const wide = { x: 0, y: 0, w: 300, h: 400 };
    expect(cropToBottomSquare(wide)).toBe(wide);
  });
});
