import { describe, expect, it } from "vitest";
import type { FlowDto } from "../api/contracts";
import {
  connectorDensity,
  connectorPath,
  connectorTraffic,
  packetResource,
  type PipeSide,
} from "./connectors";

const flow = (changes: Partial<FlowDto> = {}): FlowDto => ({
  from: 1,
  to: 2,
  resource: "ironOre",
  sent: 20,
  delivered: 18,
  kind: "internal",
  price: 0,
  path: [4, 5, 4, 6],
  ...changes,
});
const traffic = (...flows: FlowDto[]) => connectorTraffic([{ id: 1, rates: {}, flows }]);

describe("traffic on the short building connectors", () => {
  it("uses sent goods at the source and delivered goods at the destination", () => {
    const rows = traffic(flow());
    expect(rows.get("4,5:1")).toEqual([{ resource: "ironOre", outgoing: true, rate: 20 }]);
    expect(rows.get("4,6:2")).toEqual([{ resource: "ironOre", outgoing: false, rate: 18 }]);
  });
  it("aggregates several buyers at one inlet without losing the opposite direction", () => {
    const rows = traffic(
      flow(),
      flow({ to: 3 }),
      flow({ from: 4, to: 1, resource: "ironIngot", path: [0, 0, 4, 5] }),
    );
    expect(rows.get("4,5:1")).toEqual([
      { resource: "ironOre", outgoing: true, rate: 40 },
      { resource: "ironIngot", outgoing: false, rate: 18 },
    ]);
  });
  it("does not invent a building connector at the Yard's virtual seller or buyer", () => {
    const rows = traffic(flow({ from: -1000 }));
    expect([...rows.keys()]).toEqual(["4,6:2"]);
  });
  it("stops idle traffic and still shows goods that left but were lost on the way", () => {
    expect(traffic(flow({ sent: 0, delivered: 0 })).size).toBe(0);
    const lost = traffic(flow({ delivered: 0 }));
    expect([...lost.keys()]).toEqual(["4,5:1"]);
  });
  it("ignores incomplete routes", () => {
    expect(traffic(flow({ path: [] }), flow({ path: [4] })).size).toBe(0);
  });
});

describe("connector geometry and animation budget", () => {
  it.each<PipeSide>(["u", "r", "d", "l"])(
    "keeps both belt directions on separate tracks on side %s",
    (side) => {
      const inbound = connectorPath(side, 1, "solid", -0.04);
      const outbound = connectorPath(side, 1, "solid", 0.04);
      expect(inbound).toHaveLength(2);
      expect(Math.hypot(inbound[0]!.x - outbound[0]!.x, inbound[0]!.y - outbound[0]!.y)).toBeGreaterThan(8);
      expect(Math.hypot(inbound[0]!.x - inbound[1]!.x, inbound[0]!.y - inbound[1]!.y)).toBeGreaterThan(35);
    },
  );
  it("follows the electricity cable over its post", () => {
    const points = connectorPath("r", 1, "power");
    expect(points).toHaveLength(3);
    expect(points[1]!.y).toBeLessThan((points[0]!.y + points[2]!.y) / 2);
  });
  it("caps density even for late-game output", () => {
    expect(connectorDensity(0)).toBe(0);
    expect(connectorDensity(1)).toBe(1);
    expect(connectorDensity(100)).toBe(3);
    expect(connectorDensity(1e15)).toBe(3);
  });
  it("shows only the actual goods and favours the busier resource", () => {
    const rows = [
      { resource: "ironOre", outgoing: false, rate: 90 },
      { resource: "coal", outgoing: false, rate: 10 },
    ];
    const packets = Array.from({ length: 200 }, (_, i) => packetResource(rows, i));
    expect(new Set(packets)).toEqual(new Set(["ironOre", "coal"]));
    expect(packets.filter((r) => r === "ironOre").length).toBeGreaterThan(170);
  });
});
