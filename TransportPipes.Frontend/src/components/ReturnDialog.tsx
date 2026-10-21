import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount, plural } from "../app/format";
import type { LandingCardDto } from "../api/contracts";
import { setState, useAppStore } from "../state/store";

const CARD_TITLE: Record<LandingCardDto["kind"], string> = {
  yours: "Your seat",
  crew: "Next to your crew",
  lively: "A lively station",
  frontier: "The Frontier",
};

/**
 * The way back (v1.2 §10): three screens, one minute. What happened while you were away, that your estate is safe and
 * your links are yours again, and (after the Quiet Isles) where you land: your seat is preselected, the other cards
 * show how many buildings would wait in crates there, and moving to one is free.
 */
export function ReturnDialog() {
  const back = useAppStore((s) => s.welcomeBack);
  const [page, setPage] = useState(0);
  const [busy, setBusy] = useState(false);
  if (back === null) {
    return null;
  }
  const pages = back.landing.length > 1 ? 3 : 2;
  const close = () => {
    setPage(0);
    setState({ welcomeBack: null });
  };
  const land = async (card: LandingCardDto) => {
    setBusy(true);
    try {
      await gameClient.relocateTo(card.seat);
    } finally {
      setBusy(false);
      close();
    }
  };
  const days = Math.max(1, Math.round(back.daysAway));
  return (
    <div className="modal-backdrop">
      <div className="panel report return-dialog" role="dialog" aria-label="Welcome back">
        <div className="return-steps" aria-hidden="true">
          {Array.from({ length: pages }, (_, i) => (
            <span key={i} className={i === page ? "step active" : i < page ? "step done" : "step"} />
          ))}
        </div>
        {page === 0 && (
          <>
            <h2>While you were away</h2>
            <p className="muted">{plural(days, "day")} of your company on its own</p>
            {back.earned >= 1 ? (
              <div className="report-gold">
                <span className="gold-coin" />+{formatAmount(back.earned)} gold
              </div>
            ) : (
              <p>
                Your estate waited for you. Pipe your buildings to your Trading Post (or your Yard gate) and
                they earn while you&apos;re away.
              </p>
            )}
            <ul className="facts">
              {back.caretakerFee > 0 && (
                <li>
                  🧹 The caretaker kept things going for {formatAmount(back.caretakerFee)} of it (from day 14)
                </li>
              )}
              {back.tendedDays > 0 && (
                <li>💛 Your crew tended your estate on {plural(back.tendedDays, "day")}</li>
              )}
              {back.standIn !== null && (
                <li>⚙ {back.standIn} stood in for you: your neighbours&apos; trades with you went on</li>
              )}
              {back.wasAsleep && (
                <li>💤 From day 30 your estate rested: it made nothing, and nothing was lost</li>
              )}
            </ul>
          </>
        )}
        {page === 1 && (
          <>
            <h2>Your estate is safe</h2>
            <ul className="facts">
              {back.fromQuietIsles ? (
                <li>🏝 It waited on the Quiet Isles with its whole layout and landed in {back.station}</li>
              ) : (
                <li>🏡 Where you left it, in {back.station}: your seat was kept for you</li>
              )}
              {back.links > 0 && (
                <li>
                  🔗 {back.links === 1 ? "Your link is" : `Your ${back.links} links are`} back on the old
                  terms: your partners read that you&apos;re back
                </li>
              )}
              {back.crates > 0 && (
                <li>
                  📦 {plural(back.crates, "building")} didn&apos;t fit: they wait in crates in the Move tab
                </li>
              )}
              <li>🎈 A free move, if you&apos;d like to live somewhere else</li>
            </ul>
          </>
        )}
        {page === 2 && (
          <>
            <h2>Where do you land?</h2>
            <p className="muted small">Your estate is on your seat already. Another place is a free move.</p>
            <div className="landing-cards">
              {back.landing.map((card) => (
                <button
                  key={`${card.kind}-${card.seat.slot}`}
                  type="button"
                  className={card.here ? "landing-card here" : "landing-card"}
                  disabled={busy}
                  onClick={() => (card.here ? close() : void land(card))}
                >
                  <strong>{CARD_TITLE[card.kind]}</strong>
                  <span>{card.station}</span>
                  <span className="muted small">
                    {card.neighbours > 0 ? plural(card.neighbours, "neighbour") : "Houses for neighbours"} ·{" "}
                    {card.crates === 0 ? "everything fits" : plural(card.crates, "crate")}
                  </span>
                  {card.here && <span className="badge">here</span>}
                </button>
              ))}
            </div>
          </>
        )}
        <div className="button-row return-buttons">
          {page > 0 && (
            <button type="button" onClick={() => setPage(page - 1)}>
              Back
            </button>
          )}
          {page < pages - 1 ? (
            <button type="button" className="primary" onClick={() => setPage(page + 1)}>
              Next
            </button>
          ) : (
            <button type="button" className="primary" onClick={close}>
              {pages === 3 ? "Stay here" : "Let's go"}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
