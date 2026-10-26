import { useEffect, useRef, useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import type { ReturnLineDto } from "../api/contracts";
import { HOUR, awayStretches, column, time, type Series } from "../app/statistics";
import { setState, useAppStore } from "../state/store";
import { Sparkline } from "./StatisticsPanel";

/** A line's one button: what it says */
function actionLabel(line: ReturnLineDto): string | null {
  switch (line.action) {
    case "repairAll":
      return `Repair all · ${formatAmount(line.cost ?? 0)} g`;
    case "links":
      return "Look";
    case "fillWant":
      return "Send them ▸";
    case "grow":
      return `Grow it · ${formatAmount(line.cost ?? 0)} g`;
    case "orderWant":
      return line.partnerName === null
        ? `Buy the rest · ${formatAmount(line.cost ?? 0)} g`
        : `Buy the rest for ${line.partnerName} · ${formatAmount(line.cost ?? 0)} g`;
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
 * continues, and the map's keys wait.
 */
export function ReturnCard() {
  const card = useAppStore((s) => s.returnCard);
  const dialog = useRef<HTMLDivElement>(null);
  const [done, setDone] = useState<ReadonlySet<string>>(new Set());
  const [busy, setBusy] = useState(false);
  const [more, setMore] = useState(false);
  // Your income while you were away: the statistics' last away stretch, if it ends about now
  const [awayIncome, setAwayIncome] = useState<Series | null>(null);
  useEffect(() => {
    if (card === null) {
      return;
    }
    let current = true;
    void gameClient.connection
      .getStatistics(null, "week")
      .then((stats) => {
        const stretch = stats === null ? undefined : awayStretches(stats).at(-1);
        if (!current || stats === null || stretch === undefined || time(stats.now) - stretch.to > HOUR) {
          return;
        }
        const points = column(stats, "income").filter((p) => p.t >= stretch.from);
        setAwayIncome(points.length > 2 ? { label: "Income", color: "#236b69", points } : null);
      })
      .catch(() => undefined);
    return () => {
      current = false;
    };
  }, [card]);

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
    // What lies under the card can't be reached while it is open: not by the pointer, not by a screen reader (after
    // run 4: Ana's four clicks on the card's "Show me" found the Next steps' buttons of that name under it)
    const backdrop = dialog.current?.parentElement;
    const under = [...(backdrop?.parentElement?.children ?? [])].filter(
      (t): t is HTMLElement =>
        t instanceof HTMLElement &&
        t !== backdrop &&
        !t.classList.contains("toasts") &&
        !t.hasAttribute("inert"),
    );
    for (const element of under) {
      element.setAttribute("inert", "");
    }
    return () => {
      window.removeEventListener("keydown", onKey, true);
      for (const element of under) {
        element.removeAttribute("inert");
      }
    };
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
        {awayIncome !== null && (
          <button
            type="button"
            className="return-graph"
            title="Your statistics"
            onClick={() => {
              close();
              void gameClient.openStatistics(null, "income");
            }}
          >
            <span className="small muted">Your income while you were away 📈</span>
            <Sparkline series={[awayIncome]} stacked={false} />
          </button>
        )}
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
                      <button
                        type="button"
                        className="small"
                        disabled={busy}
                        // Its own name: "Show me: Your Small Factory is stuck" (many lines say Show me)
                        aria-label={`${label}: ${line.text.split(/[:(·]/)[0]!.trim()}`}
                        onClick={() => void act(line)}
                      >
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
        <button type="button" className="primary wide" autoFocus onClick={close}>
          Continue
        </button>
      </div>
    </div>
  );
}
