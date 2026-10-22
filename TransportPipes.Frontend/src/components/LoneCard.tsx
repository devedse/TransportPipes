import { useEffect, useState } from "react";
import { gameClient } from "../app/client";
import { plural } from "../app/format";
import type { LandingCardDto } from "../api/contracts";
import { useAppStore } from "../state/store";

/**
 * The lone-human card (v1.2 §4): on the fifth day on which you were the only one playing in your station, up to three
 * livelier stations; moving is free and your crew, credit and layout come along
 */
export function LoneCard() {
  const show = useAppStore((s) => s.company?.loneCard ?? false);
  const returnCard = useAppStore((s) => s.returnCard);
  const [stations, setStations] = useState<LandingCardDto[] | null>(null);
  useEffect(() => {
    if (!show) {
      return;
    }
    let cancelled = false;
    void gameClient.connection.getLivelierStations().then((t) => !cancelled && setStations(t));
    return () => {
      cancelled = true;
    };
  }, [show]);
  if (!show || returnCard !== null) {
    return null;
  }
  const dismiss = () => void gameClient.crewCommand(gameClient.connection.dismissLoneCard());
  return (
    <div className="modal-backdrop" onClick={dismiss}>
      <div
        className="panel report"
        role="dialog"
        aria-label="Livelier stations"
        onClick={(e) => e.stopPropagation()}
      >
        <h2>Nobody else here has played lately</h2>
        <p className="muted">
          Trade is better with people. These stations are lively right now and have a free seat: your whole
          estate moves with you, for free.
        </p>
        {stations === null ? (
          <p className="muted">Looking…</p>
        ) : (
          <div className="landing-cards">
            {stations.map((card) => (
              <button
                key={`${card.seat.districtX},${card.seat.districtY},${card.seat.slot}`}
                type="button"
                className="landing-card"
                onClick={() => {
                  void gameClient.relocateTo(card.seat).then(dismiss);
                }}
              >
                <strong>{card.station}</strong>
                <span className="muted small">
                  {plural(card.neighbours, "player")} playing ·{" "}
                  {card.crates === 0 ? "everything fits" : plural(card.crates, "crate")}
                </span>
              </button>
            ))}
          </div>
        )}
        <div className="button-row return-buttons">
          <button type="button" onClick={dismiss}>
            I&apos;ll stay
          </button>
        </div>
      </div>
    </div>
  );
}
