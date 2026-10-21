import { useEffect, useState } from "react";
import { gameClient } from "../app/client";
import { landIcon, landText, lived } from "../app/crews";
import type { SeatDto, SeatRefDto, StationDto, StationsDto } from "../api/contracts";
import { useAppStore } from "../state/store";

export type PickMode = "neighbours" | "own" | "friends" | "move";

const SEAT_TEXT: Record<SeatDto["state"], string> = {
  free: "free",
  house: "a House keeps it warm",
  held: "held for a friend",
  taken: "",
  you: "you",
};

/** Whether a seat can be picked in this way of starting */
function pickable(seat: SeatDto, station: StationDto, mode: PickMode): boolean {
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

function sameSeat(a: SeatRefDto | null, b: SeatRefDto | null): boolean {
  return (
    a !== null &&
    b !== null &&
    a.districtX === b.districtX &&
    a.districtY === b.districtY &&
    a.slot === b.slot
  );
}

/**
 * Choosing where you live (v1.2 §4): the district's stations as a little map, one card per station with who lives
 * there (strangers as lively, some days or resting), the free seats with their land, and the seat the game suggests
 * for this way of starting
 */
export function StationPicker({
  mode,
  value,
  onChange,
}: {
  mode: PickMode;
  value: SeatRefDto | null;
  onChange: (seat: SeatRefDto) => void;
}) {
  const [stations, setStations] = useState<StationsDto | null>(null);
  const company = useAppStore((s) => s.company);
  // Moving: your own district first
  const [district, setDistrict] = useState<{ x: number; y: number } | null>(
    mode === "move" && company !== null ? { x: company.districtX, y: company.districtY } : null,
  );
  useEffect(() => {
    let cancelled = false;
    void gameClient.connection.getStations(district?.x ?? null, district?.y ?? null).then((dto) => {
      if (cancelled) {
        return;
      }
      setStations(dto);
      if (mode === "move") {
        return;
      }
      const suggested = mode === "own" ? dto.ownStation : mode === "friends" ? dto.friends : dto.neighbours;
      const here = suggested.districtX === dto.districtX && suggested.districtY === dto.districtY;
      if (district === null && !here) {
        // The suggested seat is in another district: show that one
        onChange(suggested);
        setDistrict({ x: suggested.districtX, y: suggested.districtY });
      } else if (
        here &&
        (value === null || value.districtX !== dto.districtX || value.districtY !== dto.districtY)
      ) {
        onChange(suggested);
      }
    });
    return () => {
      cancelled = true;
    };
    // The suggestion is taken once per district
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [district?.x, district?.y, mode]);

  if (stations === null) {
    return <p className="muted">Looking at the map…</p>;
  }
  const selectedStation =
    stations.stations.find((t) =>
      t.seats.some((s) => sameSeat(value, { districtX: t.districtX, districtY: t.districtY, slot: s.slot })),
    ) ?? (value === null ? stations.stations.find((t) => t.seats.some((s) => s.state === "you")) : undefined);
  // The district map: 5×5 blocks with the hub in the middle (the stations' seats as squares)
  const block = 34;
  return (
    <div className="station-picker">
      <div className="picker-head">
        {stations.districts.length <= 1 && <strong>{stations.district}</strong>}
        {stations.districts.length > 1 && (
          <select
            value={`${stations.districtX},${stations.districtY}`}
            onChange={(e) => {
              const [x, y] = e.target.value.split(",").map(Number);
              setDistrict({ x: x!, y: y! });
            }}
          >
            {stations.districts.map((d) => (
              <option key={`${d.districtX},${d.districtY}`} value={`${d.districtX},${d.districtY}`}>
                {d.name}
              </option>
            ))}
          </select>
        )}
      </div>
      <div className="picker-body">
        <div className="map-column">
          <svg
            className="district-map"
            viewBox={`0 0 ${block * 5} ${block * 5}`}
            role="img"
            aria-label="The district's stations"
          >
            <rect
              x={block * 2 + 2}
              y={block * 2 + 2}
              width={block - 4}
              height={block - 4}
              rx={4}
              className="hub"
            />
            {stations.stations.flatMap((station) =>
              station.seats.map((seat) => {
                const raw = seat.slot >= 12 ? seat.slot + 1 : seat.slot;
                const bx = raw % 5;
                const by = Math.floor(raw / 5);
                const ref = { districtX: station.districtX, districtY: station.districtY, slot: seat.slot };
                const can = pickable(seat, station, mode);
                const chosen = sameSeat(value, ref);
                return (
                  <rect
                    key={`${station.index}-${seat.slot}`}
                    x={bx * block + 2}
                    y={by * block + 2}
                    width={block - 4}
                    height={block - 4}
                    rx={4}
                    className={`seat ${seat.state}${chosen ? " chosen" : ""}${can ? " can" : ""}${selectedStation?.index === station.index ? " in-station" : ""}`}
                    style={lived(seat) && seat.ownerColor !== null ? { fill: seat.ownerColor } : undefined}
                    onClick={() => can && onChange(ref)}
                  >
                    <title>{seat.ownerName ?? landText(seat.primary, seat.secondary)}</title>
                  </rect>
                );
              }),
            )}
            {stations.stations.flatMap((station) =>
              station.seats.filter(lived).map((seat) => {
                // A player lives here: a dot in the middle of the seat
                const raw = seat.slot >= 12 ? seat.slot + 1 : seat.slot;
                return (
                  <circle
                    key={`home-${station.index}-${seat.slot}`}
                    cx={(raw % 5) * block + block / 2}
                    cy={Math.floor(raw / 5) * block + block / 2}
                    r={4}
                    className={seat.state === "you" ? "home-mark you" : "home-mark"}
                  />
                );
              }),
            )}
            {stations.stations.map((station) => {
              // Each station's Yard, in the middle of its seats
              const spots = station.seats.map((seat) => {
                const raw = seat.slot >= 12 ? seat.slot + 1 : seat.slot;
                return { x: (raw % 5) * block + block / 2, y: Math.floor(raw / 5) * block + block / 2 };
              });
              const cx = spots.reduce((sum, t) => sum + t.x, 0) / spots.length;
              const cy = spots.reduce((sum, t) => sum + t.y, 0) / spots.length;
              return (
                <rect
                  key={`yard-${station.index}`}
                  x={cx - 6}
                  y={cy - 6}
                  width={12}
                  height={12}
                  rx={2}
                  transform={`rotate(45 ${cx} ${cy})`}
                  className={selectedStation?.index === station.index ? "yard-mark in-station" : "yard-mark"}
                >
                  <title>{station.name}&apos;s Yard</title>
                </rect>
              );
            })}
          </svg>
          <div className="map-legend small">
            <span>
              <i className="free" /> free
            </span>
            <span>
              <i className="house" /> a House
            </span>
            <span>
              <i className="held" /> kept for a friend
            </span>
            <span>
              <i className="player" /> a player
            </span>
            <span>
              <i className="yard" /> a Yard
            </span>
          </div>
        </div>
        {selectedStation !== undefined && (
          <div className="station-card">
            <div className="plaque">
              {selectedStation.name} · {selectedStation.small ? "small station" : "station"}
            </div>
            {selectedStation.crewName !== null && (
              <div className="muted small">Home of {selectedStation.crewName}</div>
            )}
            {!selectedStation.open && mode !== "own" && (
              <div className="muted small">A crew lives here: closed to strangers</div>
            )}
            <ul className="seat-list">
              {selectedStation.seats.map((seat) => {
                const ref = {
                  districtX: selectedStation.districtX,
                  districtY: selectedStation.districtY,
                  slot: seat.slot,
                };
                const can = pickable(seat, selectedStation, mode);
                const chosen = sameSeat(value, ref);
                return (
                  <li key={seat.slot}>
                    <button
                      type="button"
                      className={chosen ? "seat-row chosen" : "seat-row"}
                      disabled={!can}
                      onClick={() => onChange(ref)}
                    >
                      <span className="icon">{lived(seat) ? "🏠" : landIcon(seat.primary)}</span>
                      <span className="grow">
                        {lived(seat) ? (
                          <>
                            <span className="dot" style={{ background: seat.ownerColor ?? "#999" }} />{" "}
                            <strong>{seat.state === "you" ? "You" : seat.ownerName}</strong> · Lv{" "}
                            {seat.ownerLevel}
                            {seat.activity !== null && ` · ${seat.activity}`}
                          </>
                        ) : (
                          <>
                            <strong>{landText(seat.primary, seat.secondary)}</strong>
                            <span className="muted small">
                              {" "}
                              · {seat.heldForYou ? "kept by your crew: yours to take" : SEAT_TEXT[seat.state]}
                              {seat.state === "house" && seat.ownerName !== null
                                ? ` (${seat.ownerName} ⚙)`
                                : ""}
                            </span>
                          </>
                        )}
                      </span>
                      {chosen && <span className="badge">your seat</span>}
                    </button>
                  </li>
                );
              })}
            </ul>
          </div>
        )}
      </div>
    </div>
  );
}
