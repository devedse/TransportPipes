import type { SeatDto, SeatRefDto, StationDto, StationsDto } from "../api/contracts";

/** The ways of starting (and moving) the station picker serves */
export type PickMode = "neighbours" | "own" | "friends" | "move";

/** Whether a seat can be picked in this way of starting */
export function pickable(seat: SeatDto, station: StationDto, mode: PickMode): boolean {
  if (seat.state === "held") {
    return seat.heldForYou;
  }
  if (seat.state !== "free" && seat.state !== "house") {
    return false;
  }
  if (mode === "own" || mode === "friends") {
    return station.seats.every((t) => t.state === "free" || t.state === "house" || t.state === "you");
  }
  return station.open;
}

/** The seat of a station list, or null when it isn't in it */
export function findSeat(dto: StationsDto, ref: SeatRefDto): { seat: SeatDto; station: StationDto } | null {
  for (const station of dto.stations) {
    if (station.districtX !== ref.districtX || station.districtY !== ref.districtY) {
      continue;
    }
    const seat = station.seats.find((t) => t.slot === ref.slot);
    if (seat !== undefined) {
      return { seat, station };
    }
  }
  return null;
}

/**
 * The seat the picker shows chosen after it loads a district (F7): the player's own choice while it is still pickable
 * there, else the game's suggestion for this way of starting (null: keep what there is, the suggestion is elsewhere)
 */
export function chooseSeat(dto: StationsDto, mode: PickMode, value: SeatRefDto | null): SeatRefDto | null {
  const suggested = mode === "own" ? dto.ownStation : mode === "friends" ? dto.friends : dto.neighbours;
  const found = value !== null ? findSeat(dto, value) : null;
  if (found !== null && pickable(found.seat, found.station, mode)) {
    return value;
  }
  return suggested.districtX === dto.districtX && suggested.districtY === dto.districtY ? suggested : null;
}

export function sameSeat(a: SeatRefDto | null, b: SeatRefDto | null): boolean {
  return (
    a !== null &&
    b !== null &&
    a.districtX === b.districtX &&
    a.districtY === b.districtY &&
    a.slot === b.slot
  );
}
