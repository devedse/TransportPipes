import { useEffect, useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import type { NextStepDto } from "../api/contracts";
import { currentGold, useAppStore } from "../state/store";

const ICONS: Record<NextStepDto["kind"], string> = {
  shortage: "⚠",
  pump: "💨",
  full: "📦",
  research: "🔬",
  upgrade: "⬆",
  chain: "🏭",
  partner: "🤝",
  parcel: "🗺",
  pipes: "🔧",
  start: "⭐",
  stuck: "⛔",
};

const COLLAPSED_KEY = "tp.nextStepsCollapsed";

/** Collapsed as the player left it; on a phone it starts as one line, so the map stays in view */
function readCollapsed() {
  const phone = typeof window !== "undefined" && window.matchMedia?.("(max-width: 720px)").matches === true;
  try {
    const stored = localStorage.getItem(COLLAPSED_KEY);
    return stored === null ? phone : stored === "1";
  } catch {
    return phone;
  }
}

function minutes(value: number) {
  return value < 1 ? "a minute" : value < 90 ? `${Math.ceil(value)} min` : `${(value / 60).toFixed(1)} h`;
}

/**
 * The advisor (docs/v1.1-design.html §4): the three best things to do right now, ranked, each with its reason in
 * numbers and a button that does it or shows where
 */
export function NextSteps() {
  const latest = useAppStore((s) => s.nextSteps);
  // The list holds still while the pointer is over it: a step that moved under the pointer was clicked by mistake
  // (run 2: Ana built a Glassworks she didn't want)
  const [held, setHeld] = useState<NextStepDto[] | null>(null);
  const keyOf = (t: NextStepDto) => `${t.kind}-${t.building ?? t.title}`;
  // Held: the same order, with each step's newest numbers, and a step that's done drops out
  const steps =
    held === null ? latest : held.flatMap((h) => latest.filter((l) => keyOf(l) === keyOf(h)).slice(0, 1));
  const company = useAppStore((s) => s.company);
  const companyAt = useAppStore((s) => s.companyAt);
  const [collapsed, setCollapsed] = useState(readCollapsed);
  const level = company?.level ?? 0;
  useEffect(() => {
    void gameClient.refreshNextSteps();
    const timer = window.setInterval(() => void gameClient.refreshNextSteps(), 45_000);
    return () => clearInterval(timer);
  }, [level]);
  if (company === null || steps.length === 0) {
    return null;
  }
  const gold = currentGold({ company, companyAt });
  const toggle = () => {
    setCollapsed(!collapsed);
    try {
      localStorage.setItem(COLLAPSED_KEY, collapsed ? "0" : "1");
    } catch {
      // Remembering it is a convenience
    }
  };

  return (
    <section
      className="panel next-steps"
      aria-label="Next steps"
      onPointerEnter={() => setHeld(latest)}
      onPointerLeave={() => setHeld(null)}
    >
      <div className="next-steps-bar">
        <button className="next-steps-head" onClick={toggle} aria-expanded={!collapsed}>
          <span>Next steps</span>
          <span className="next-steps-chevron">{collapsed ? "▸" : "▾"}</span>
        </button>
        {/* Folded, the first step is itself a button (after run 3, row 12: Ben's step with nothing to tap) */}
        {collapsed && (
          <button
            className="link next-steps-peek"
            aria-label={`Show me: ${steps[0]!.title}`}
            onClick={() => void gameClient.showNextStep(steps[0]!)}
          >
            {steps[0]!.title} ›
          </button>
        )}
      </div>
      {!collapsed && (
        <ol className="next-step-list">
          {steps.map((step, i) => {
            const short = step.cost > gold + 0.5;
            return (
              <li key={keyOf(step)} className={`next-step kind-${step.kind}`}>
                <span className="next-step-icon" aria-hidden="true">
                  {ICONS[step.kind]}
                </span>
                <div className="next-step-body">
                  <strong className="next-step-title">
                    <span className="next-step-rank">{i + 1}</span> {step.title}
                  </strong>
                  <span className="next-step-reason">{step.reason}</span>
                  <div className="next-step-actions">
                    {step.action === "climb" && step.building !== null ? (
                      // The pipe door (after run 4): the pipe, then the levels it carries, in one tap
                      <button
                        className="primary"
                        disabled={short}
                        onClick={() => void gameClient.climbPipe(step.building!)}
                      >
                        {short && step.waitMinutes !== null
                          ? `In ${minutes(step.waitMinutes)}`
                          : `Do it · ${formatAmount(step.cost)} g`}
                      </button>
                    ) : step.action === "layPipe" ? (
                      <button
                        className="primary"
                        disabled={short}
                        onClick={() => void gameClient.showNextStep(step)}
                      >
                        {short && step.waitMinutes !== null
                          ? `In ${minutes(step.waitMinutes)}`
                          : `Lay the pipe · ${formatAmount(step.cost)} g`}
                      </button>
                    ) : step.action === "upgrade" && step.building !== null ? (
                      <button
                        className="primary"
                        disabled={short}
                        title={short ? `You have ${formatAmount(gold)} g` : undefined}
                        onClick={() => void gameClient.upgrade(step.building!, step.levels)}
                      >
                        {short && step.waitMinutes !== null
                          ? `In ${minutes(step.waitMinutes)}`
                          : `Upgrade · ${formatAmount(step.cost)} g`}
                      </button>
                    ) : (
                      short &&
                      step.waitMinutes !== null && (
                        <span className="next-step-wait">affordable in {minutes(step.waitMinutes)}</span>
                      )
                    )}
                    <button
                      className={
                        step.action === "upgrade" || step.action === "climb" || step.action === "layPipe"
                          ? "link small"
                          : "small"
                      }
                      // Each its own name, for screen readers and the playtest's players alike
                      aria-label={`${step.action === "research" ? "Open research" : step.action === "partner" ? "Plan the pipe" : "Show me"}: ${step.title}`}
                      onClick={() => void gameClient.showNextStep(step)}
                    >
                      {step.action === "research"
                        ? "Open research"
                        : step.action === "partner"
                          ? "Plan the pipe"
                          : "Show me"}
                    </button>
                  </div>
                </div>
              </li>
            );
          })}
        </ol>
      )}
    </section>
  );
}
