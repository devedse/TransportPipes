import { describe, expect, it } from "vitest";
import {
  estateOutline,
  stationOfSlot,
  stationSlots,
  yardGateLocal,
  yardLocal,
  yardPart,
  zoneAt,
  yardRect,
} from "./layout";

// The same geometry as TransportPipes.Game/World/WorldLayout.cs (WorldLayoutFacts checks the server's)
describe("stations and Yards", () => {
  it("has four stations of four and four of two", () => {
    const sizes = Array.from({ length: 8 }, (_, i) => stationSlots(i).length);
    expect(sizes).toEqual([4, 4, 4, 4, 2, 2, 2, 2]);
    expect(stationSlots(0)).toEqual([0, 1, 5, 6]);
    expect(stationOfSlot(2)).toBe(4);
  });

  it("puts a corner station's Yard on its corridor crossing and a small one's beside its middle", () => {
    expect(yardLocal(0)).toEqual({ x0: 30, y0: 30, x1: 39, y1: 39 });
    expect(yardLocal(4)).toEqual({ x0: 90, y0: 30, x1: 99, y1: 39 });
    expect(yardPart(0)).toEqual({ x0: 28, y0: 28, x1: 31, y1: 31 });
    expect(yardPart(2)).toEqual({ x0: 20, y0: 28, x1: 29, y1: 31 });
  });

  it("puts every Yard gate right next to its Yard, as the server does", () => {
    expect(yardGateLocal(0)).toEqual({ x: 27, y: 28 });
    expect(yardGateLocal(1)).toEqual({ x: 4, y: 28 });
    expect(yardGateLocal(2)).toEqual({ x: 24, y: 27 });
    expect(yardGateLocal(7)).toEqual({ x: 24, y: 4 });
  });

  it("knows a Yard tile and its station", () => {
    const yard = yardRect(0, 0, 3);
    const zone = zoneAt(yard.x0 + 1, yard.y0 + 1);
    expect(zone.kind).toBe("yard");
    expect(zone.slot).toBe(3);
  });

  it("draws an estate's border around its part of the Yard", () => {
    expect(estateOutline(0)).toEqual([
      [0, 0],
      [32, 0],
      [32, 28],
      [28, 28],
      [28, 32],
      [0, 32],
    ]);
    expect(estateOutline(2)).toHaveLength(8);
  });
});
