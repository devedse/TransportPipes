import { useEffect, useRef, useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import type { LandingCardDto, ReturnLineDto } from "../api/contracts";
import { setState, useAppStore } from "../state/store";

const CARD_TITLE: Record<LandingCardDto["kind"], string> = {
  yours: "Your seat",
  crew: "Next to your crew",
  lively: "A lively station",
  frontier: "The Frontier",
};

/** A line's one button: what it says */
function actionLabel(line: ReturnLineDto): string | null {
  switch (line.action) {
    case "repairAll":
      return `Repair all · ${formatAmount(line.cost ?? 0)} g`;
    case "project":
      return "Open";
    case "send":
      return "Send ▸";
    case "thank":
      return "💛 Thanks";
    case "tend":
      return "💛 Tend";
    case "links":
      return "Look";
    case "seat":
      return "Move in";
    case "fillWant":
      return "Send them ▸";
    case "orderWant":
      return line.partnerName === null
        ? `Order the rest · ${formatAmount(line.cost ?? 0)} g`
        : `Order the rest for ${line.partnerName} · ${formatAmount(line.cost ?? 0)} g`;
    case "crew":
      return "Crew";
    case "show":
    case "upgrade":
      return "Show me";
    default:
      return null;
  }
}

function close() {
  setState({ returnCard: null });
}

/**
 * The return card (F5): one card for every return with news, built by the server on its clock. Gold and level, then
 * what matters (one line about people, the crew, the shared goal you can unblock, one-tap fixes, what stops you, and one
 * "Still:" line for what hasn't changed), each with its one button, the rest folded under "more", and one Continue. While it is open the keys are its own: Tab stays inside, Escape
 * continues, and the map's keys wait. After the Quiet Isles it also asks where to land.
 */
export function ReturnCard() {
  const card = useAppStore((s) => s.returnCard);
  const dialog = useRef<HTMLDivElement>(null);
  const [done, setDone] = useState<ReadonlySet<string>>(new Set());
  const [busy, setBusy] = useState(false);
  const [more, setMore] = useState(false);

  useEffect(() => {
    if (card === null) {
      return;
    }
    const onKey = (event: KeyboardEvent) => {
      const box = dialog.current;
      if (box === null) {
        return;
      }
      if (event.key === "Escape") {
        event.preventDefault();
        event.stopPropagation();
        close();
        return;
      }
      if (event.key === "Tab") {
        const focusable = [...box.querySelectorAll<HTMLElement>("button:not([disabled])")];
        if (focusable.length > 0) {
          const index = focusable.indexOf(document.activeElement as HTMLElement);
          const next = event.shiftKey
            ? index <= 0
              ? focusable.length - 1
              : index - 1
            : (index + 1) % focusable.length;
          focusable[next]?.focus();
        }
        event.preventDefault();
        event.stopPropagation();
        return;
      }
      // The map's keys wait until the card is closed (Enter and Space still press the focused button)
      event.stopPropagation();
      if (!box.contains(event.target as Node)) {
        event.preventDefault();
      }
    };
    window.addEventListener("keydown", onKey, true);
    return () => window.removeEventListener("keydown", onKey, true);
  }, [card]);

  if (card === null) {
    return null;
  }
  const act = async (line: ReturnLineDto) => {
    setBusy(true);
    try {
      if (await gameClient.returnAction(line)) {
        setDone(new Set([...done, line.text]));
      }
    } finally {
      setBusy(false);
    }
  };
  const land = async (landing: LandingCardDto) => {
    setBusy(true);
    try {
      await gameClient.relocateTo(landing.seat);
    } finally {
      setBusy(false);
      close();
    }
  };
  const choosing = card.landing.length > 1;
  return (
    <div className="modal-backdrop">
      <div
        ref={dialog}
        className="panel report return-card"
        role="dialog"
        aria-modal="true"
        aria-label="While you were away"
      >
        <h2>While you were away</h2>
        <div className="return-head">
          {card.earned >= 1 && (
            <div className="report-gold">
              <span className="gold-coin" />+{formatAmount(card.earned)} gold
            </div>
          )}
          <span className="muted small">
            {card.away} away
            {card.level > card.levelFrom && (
              <>
                {" · "}
                <strong>
                  Level {card.levelFrom} → {card.level}
                </strong>
              </>
            )}
          </span>
        </div>
        {card.hint !== null && <p>{card.hint}</p>}
        {(card.lines.length > 0 || more) && (
          <ul className="return-lines">
            {[...card.lines, ...(more ? (card.folded ?? []) : [])].map((line) => {
              const label = actionLabel(line);
              return (
                <li key={line.text} className={`return-line ${line.kind}`}>
                  <span className="return-icon" aria-hidden="true">
                    {line.icon}
                  </span>
                  <span className="return-text">{line.text}</span>
                  {label !== null &&
                    (done.has(line.text) ? (
                      <span className="small ok">Done</span>
                    ) : (
                      <button type="button" className="small" disabled={busy} onClick={() => void act(line)}>
                        {label}
                      </button>
                    ))}
                </li>
              );
            })}
          </ul>
        )}
        {/* The rest folds (after run 3, row 11: "I can't tell what matters") */}
        {!more && (card.folded?.length ?? 0) > 0 && (
          <button type="button" className="link small return-more" onClick={() => setMore(true)}>
            {card.folded!.length} more ▸
          </button>
        )}
        {card.fine.length > 0 && <p className="muted small">{card.fine.join(" ")}</p>}
        {choosing && (
          <>
            <p className="muted small">Your estate is on your seat already. Another place is a free move.</p>
            <div className="landing-cards">
              {card.landing.map((landing) => (
                <button
                  key={`${landing.kind}-${landing.seat.slot}`}
                  type="button"
                  className={landing.here ? "landing-card here" : "landing-card"}
                  disabled={busy}
                  onClick={() => (landing.here ? close() : void land(landing))}
                >
                  <strong>{CARD_TITLE[landing.kind]}</strong>
                  <span>{landing.station}</span>
                  <span className="muted small">
                    {landing.neighbours > 0
                      ? `${landing.neighbours} ${landing.neighbours === 1 ? "neighbour" : "neighbours"}`
                      : "Houses for neighbours"}{" "}
                    ·{" "}
                    {landing.crates === 0
                      ? "everything fits"
                      : `${landing.crates} ${landing.crates === 1 ? "crate" : "crates"}`}
                  </span>
                  {landing.here && <span className="badge">here</span>}
                </button>
              ))}
            </div>
          </>
        )}
        <button type="button" className="primary wide" autoFocus onClick={close}>
          {choosing ? "Stay here" : "Continue"}
        </button>
      </div>
    </div>
  );
}
