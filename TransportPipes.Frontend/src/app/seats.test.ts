import { describe, expect, it } from "vitest";
import type { SeatDto, StationDto, StationsDto } from "../api/contracts";
import { chooseSeat } from "./seats";

const seat = (slot: number, state: SeatDto["state"]): SeatDto => ({
  slot,
  estateX: 0,
  estateY: 0,
  primary: "windyHeights",
  secondary: null,
  state,
  ownerId: null,
  ownerName: null,
  ownerColor: null,
  ownerLevel: 0,
  activity: null,
  crewName: null,
  heldForYou: false,
});

const station = (index: number, seats: SeatDto[], crewName: string | null = null): StationDto => ({
  districtX: 0,
  districtY: 0,
  index,
  name: `Capital ${index}`,
  small: false,
  open: crewName === null,
  free: seats.filter((t) => t.state === "free").length,
  lively: 0,
  seats,
  crewName,
  x0: 0,
  y0: 0,
  x1: 0,
  y1: 0,
  project: null,
  makes: null,
  needs: null,
});

/** Capital NW just taken by a crew (one seat lived, three held), Capital N free */
function stations(): StationsDto {
  return {
    districtX: 0,
    districtY: 0,
    district: "Capital",
    stations: [
      station(0, [seat(0, "taken"), seat(1, "held"), seat(5, "held"), seat(6, "held")], "Tea Time Pipes"),
      station(1, [seat(2, "house"), seat(3, "free"), seat(7, "house"), seat(8, "house")]),
    ],
    neighbours: { districtX: 0, districtY: 0, slot: 3 },
    ownStation: { districtX: 0, districtY: 0, slot: 3 },
    friends: { districtX: 0, districtY: 0, slot: 3 },
    districts: [],
  };
}

describe("the seat picker (F7)", () => {
  it("replaces a seat the fresh data says is gone with the suggestion", () => {
    // Mia's picker had chosen seat 1 a moment before Ana's crew claimed the station
    expect(chooseSeat(stations(), "neighbours", { districtX: 0, districtY: 0, slot: 1 })).toEqual({
      districtX: 0,
      districtY: 0,
      slot: 3,
    });
  });

  it("keeps the player's own choice while it can still be picked", () => {
    const choice = { districtX: 0, districtY: 0, slot: 2 };
    expect(chooseSeat(stations(), "neighbours", choice)).toBe(choice);
  });

  it("never keeps a seat for a station of your own where someone lives", () => {
    expect(chooseSeat(stations(), "own", { districtX: 0, districtY: 0, slot: 1 })?.slot).toBe(3);
  });

  it("starts with the suggestion when nothing is chosen", () => {
    expect(chooseSeat(stations(), "friends", null)?.slot).toBe(3);
  });
});
