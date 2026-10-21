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
};

const COLLAPSED_KEY = "tp.nextStepsCollapsed";

function readCollapsed() {
  try {
    return localStorage.getItem(COLLAPSED_KEY) === "1";
  } catch {
    return false;
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
  const steps = useAppStore((s) => s.nextSteps);
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
    <section className="panel next-steps" aria-label="Next steps">
      <button className="next-steps-head" onClick={toggle} aria-expanded={!collapsed}>
        <span>Next steps</span>
        {collapsed && <span className="next-steps-peek">{steps[0]!.title}</span>}
        <span className="next-steps-chevron">{collapsed ? "▸" : "▾"}</span>
      </button>
      {!collapsed && (
        <ol className="next-step-list">
          {steps.map((step, i) => {
            const short = step.cost > gold + 0.5;
            return (
              <li key={`${step.kind}-${step.title}`} className={`next-step kind-${step.kind}`}>
                <span className="next-step-icon" aria-hidden="true">
                  {ICONS[step.kind]}
                </span>
                <div className="next-step-body">
                  <strong className="next-step-title">
                    <span className="next-step-rank">{i + 1}</span> {step.title}
                  </strong>
                  <span className="next-step-reason">{step.reason}</span>
                  <div className="next-step-actions">
                    {step.action === "pipes" ? (
                      <button
                        className="primary"
                        disabled={short}
                        onClick={() =>
                          void gameClient.upgradePipes(
                            Array.from({ length: step.tiles.length / 2 }, (_, i) => ({
                              x: step.tiles[i * 2]!,
                              y: step.tiles[i * 2 + 1]!,
                            })),
                            step.tier,
                          )
                        }
                      >
                        {short && step.waitMinutes !== null
                          ? `In ${minutes(step.waitMinutes)}`
                          : `Upgrade pipes · ${formatAmount(step.cost)} g`}
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
                        step.action === "upgrade" || step.action === "pipes" ? "link small" : "small"
                      }
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
