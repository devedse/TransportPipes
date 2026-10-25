import type { SeatDto, SeatRefDto, StationDto, StationsDto } from "../api/contracts";

/** The ways of using the plot picker: starting, or moving your plot */
export type PickMode = "start" | "move";

/**
 * Whether a plot can be chosen (1.4 §6): a free one, or a free one in a closed Yard (that sends a request: a resident
 * says yes). Moving, your own plot is where you are already.
 */
export function pickable(seat: SeatDto): boolean {
  return seat.state === "free" || seat.state === "closed";
}

/** The plot of a town's Yards, with its Yard, or null when it isn't in them */
export function findSeat(dto: StationsDto, ref: SeatRefDto): { seat: SeatDto; station: StationDto } | null {
  if (dto.districtX !== ref.districtX || dto.districtY !== ref.districtY) {
    return null;
  }
  for (const station of dto.stations) {
    const seat = station.seats.find((t) => t.slot === ref.slot);
    if (seat !== undefined) {
      return { seat, station };
    }
  }
  return null;
}

/**
 * The plot the picker shows chosen after it loads a town: the player's own choice while it can still be chosen there,
 * else the first free plot of the Yard it looks at (a friend's; in a closed Yard a request), else Pick for me's when it
 * lies in this town
 */
export function chooseSeat(
  dto: StationsDto,
  value: SeatRefDto | null,
  yard: number | null = null,
): SeatRefDto | null {
  const found = value !== null ? findSeat(dto, value) : null;
  if (found !== null && pickable(found.seat)) {
    return value;
  }
  // At a friend's Yard: a free plot, else one of a closed Yard (a request)
  const station = yard !== null ? dto.stations.find((t) => t.index === yard) : undefined;
  const free =
    station?.seats.find((t) => t.state === "free") ?? station?.seats.find((t) => t.state === "closed");
  if (station !== undefined && free !== undefined) {
    return { districtX: station.districtX, districtY: station.districtY, slot: free.slot };
  }
  const pick = dto.pickForMe;
  return pick.districtX === dto.districtX && pick.districtY === dto.districtY ? pick : null;
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
