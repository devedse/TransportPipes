import { describe, expect, it } from "vitest";
import {
  districtOriginX,
  districtOriginY,
  estateOrigin,
  ESTATES_PER_DISTRICT,
  hqLocal,
  outskirtsParcels,
  parcelOf,
  startParcels,
  stationOfSlot,
  stationSlots,
  tradingPostLocal,
  yardGate,
  yardGateLocal,
  yardLocal,
  yardRect,
  zoneAt,
} from "./layout";

// The same geometry as TransportPipes.Game/World/WorldLayout.cs (WorldLayoutFacts checks the server's)
describe("the town", () => {
  it("has the Capital's road crossing at the origin, in the hub", () => {
    expect(zoneAt(0, 0).kind).toBe("hub");
    expect(districtOriginX(0)).toBe(-98);
    expect(districtOriginY(0)).toBe(-82);
  });

  it("is four Yard groups with roads between them and wilderness around", () => {
    const at = (lx: number, ly: number) => zoneAt(districtOriginX(0) + lx, districtOriginY(0) + ly);
    expect(at(97, 10).kind).toBe("corridor");
    expect(at(10, 81).kind).toBe("corridor");
    expect(at(196, 10).kind).toBe("wilderness");
    expect(at(10, 164).kind).toBe("wilderness");
    expect(at(196 + 128, 10).districtX).toBe(1);
    expect(at(196 + 127, 10).kind).toBe("wilderness");
  });

  it("puts every plot and Yard where its group puts it", () => {
    for (let station = 0; station < 4; station++) {
      const yard = yardRect(2, -1, station);
      expect(yard.x1 - yard.x0 + 1).toBe(32);
      expect(yard.y1 - yard.y0 + 1).toBe(16);
      expect(zoneAt(yard.x0 + 3, yard.y0 + 5)).toMatchObject({ kind: "yard", slot: station });
      for (const slot of stationSlots(station)) {
        const plot = estateOrigin(2, -1, slot);
        expect(zoneAt(plot.x + 5, plot.y + 7)).toMatchObject({ kind: "estate", slot, localX: 5, localY: 7 });
        expect(stationOfSlot(slot)).toBe(station);
      }
    }
    expect(yardLocal(3)).toEqual({ x0: 132, y0: 116, x1: 163, y1: 131 });
  });

  it("puts every gate on its plot's edge against the Yard, in the start area", () => {
    for (let slot = 0; slot < ESTATES_PER_DISTRICT; slot++) {
      const origin = estateOrigin(0, 0, slot);
      const gate = yardGate(origin.x, origin.y, slot);
      const yard = yardRect(0, 0, stationOfSlot(slot));
      const touches = [
        [1, 0],
        [-1, 0],
        [0, 1],
        [0, -1],
      ].filter(([dx, dy]) => {
        const x = gate.x + dx!;
        const y = gate.y + dy!;
        return x >= yard.x0 && x <= yard.x1 && y >= yard.y0 && y <= yard.y1;
      });
      expect(touches).toHaveLength(1);
      const local = yardGateLocal(slot);
      expect(startParcels(slot)).toContain(parcelOf(local.x, local.y));
    }
  });

  it("puts the HQ and the trading post by the gate, as the server does", () => {
    expect(hqLocal(3)).toEqual({ x: 13, y: 1 });
    expect(tradingPostLocal(3)).toEqual({ x: 18, y: 1 });
    expect(hqLocal(0)).toEqual({ x: 13, y: 29 });
    expect(tradingPostLocal(0)).toEqual({ x: 18, y: 30 });
  });

  it("sets nine outskirts parcels aside for every plot", () => {
    const all = new Set<string>();
    for (let slot = 0; slot < ESTATES_PER_DISTRICT; slot++) {
      const parcels = outskirtsParcels(slot);
      expect(parcels).toHaveLength(9);
      for (const t of parcels) {
        all.add(`${t.x},${t.y}`);
        expect(zoneAt(districtOriginX(0) + t.x + 3, districtOriginY(0) + t.y + 3)).toMatchObject({
          kind: "outskirts",
          slot,
        });
      }
    }
    expect(all.size).toBe(144);
  });
});
