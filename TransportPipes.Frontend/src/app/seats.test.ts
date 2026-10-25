import { describe, expect, it } from "vitest";
import type { SeatDto, StationDto, StationsDto } from "../api/contracts";
import { chooseSeat, pickable } from "./seats";

const seat = (slot: number, state: SeatDto["state"]): SeatDto => ({
  slot,
  estateX: 0,
  estateY: 0,
  primary: "coalValley",
  secondary: null,
  state,
  ownerId: null,
  ownerName: null,
  ownerColor: null,
  ownerLevel: 0,
  activity: null,
  goods: null,
  lacks: null,
  strength: null,
  crates: null,
});

const station = (index: number, seats: SeatDto[], open = true): StationDto => ({
  districtX: 0,
  districtY: 0,
  index,
  name: `Capital ${index}`,
  emblem: null,
  color: null,
  color2: null,
  open,
  free: seats.filter((t) => t.state === "free").length,
  lively: 0,
  seats,
  x0: 0,
  y0: 0,
  x1: 0,
  y1: 0,
  makes: null,
  needs: null,
});

/** The Capital: NW lived in by one player and closed, NE with a free plot (Pick for me's) */
function stations(): StationsDto {
  return {
    districtX: 0,
    districtY: 0,
    district: "Capital",
    stations: [
      station(0, [seat(0, "taken"), seat(1, "closed"), seat(2, "closed"), seat(3, "closed")], false),
      station(1, [seat(4, "taken"), seat(5, "free"), seat(6, "free"), seat(7, "taken")]),
    ],
    pickForMe: { districtX: 0, districtY: 0, slot: 5 },
    districts: [],
  };
}

describe("the plot picker (1.4 §6)", () => {
  it("lets a free plot be chosen, and one in a closed Yard as a request", () => {
    expect(pickable(seat(1, "free"))).toBe(true);
    expect(pickable(seat(1, "closed"))).toBe(true);
    expect(pickable(seat(1, "taken"))).toBe(false);
  });

  it("replaces a plot the fresh data says is taken with Pick for me's", () => {
    // Mia's picker had chosen plot 4 a moment before Ana settled there
    expect(chooseSeat(stations(), { districtX: 0, districtY: 0, slot: 4 })).toEqual({
      districtX: 0,
      districtY: 0,
      slot: 5,
    });
  });

  it("keeps the player's own choice while it can still be chosen", () => {
    const choice = { districtX: 0, districtY: 0, slot: 6 };
    expect(chooseSeat(stations(), choice)).toBe(choice);
  });

  it("starts at a friend's Yard on its first free plot", () => {
    expect(chooseSeat(stations(), null, 1)?.slot).toBe(5);
  });

  it("goes to Pick for me's plot when the friend's Yard is full", () => {
    const dto = stations();
    dto.stations[1]!.seats = dto.stations[1]!.seats.map((t) => ({ ...t, state: "taken" as const }));
    dto.pickForMe = { districtX: 1, districtY: 0, slot: 2 };
    expect(chooseSeat(dto, null, 1)).toBeNull();
  });
});

describe("landText", () => {
  it("says a plot's strength and what its land lacks", async () => {
    const { landText } = await import("./crews");
    expect(landText("coalValley", ["copperOre", "oil"])).toBe("Coal Valley · no copper or oil");
    expect(landText("ironHills", ["sand", "rareEarth"])).toBe("Iron Hills · no sand or rare earth");
    expect(landText("windyHeights")).toBe("Windy Heights");
  });
});
