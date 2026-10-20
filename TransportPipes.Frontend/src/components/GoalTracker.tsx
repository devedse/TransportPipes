import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import { useAppStore } from "../state/store";

/**
 * Always three goals in view (GDD §10.1): Loady's next step (or the next Company Level), today's order and something
 * for this week. Loady himself helps through the first session.
 */
export function GoalTracker() {
  const company = useAppStore((s) => s.company);
  const content = useAppStore((s) => s.content);
  const buildMenuOpen = useAppStore((s) => s.buildMenuOpen);
  if (company === null || content === null || buildMenuOpen) {
    return null;
  }
  const step = company.tutorial;
  const order = company.dailyOrders.find((t) => t.delivered < t.amount);
  const levelShare = Math.min(
    1,
    (company.xp - company.levelXp) / Math.max(1, company.nextLevelXp - company.levelXp),
  );
  return (
    <aside className="goal-tracker" aria-label="Goals">
      {step !== null && (
        <div className="loady-card">
          <img
            className="loady"
            src={step.step === 0 ? "/art/ui/loady-wave.webp" : "/art/ui/loady-point.webp"}
            alt="Loady"
          />
          <div className="loady-bubble">
            <p>{step.text}</p>
            <div className="loady-actions">
              <button className="primary" onClick={() => gameClient.showTutorialHint()}>
                Show me
              </button>
              <span className="small muted">
                {step.step + 1}/{step.steps} · +{formatAmount(step.gold)} g
              </span>
              <button className="link small" onClick={() => void gameClient.skipTutorial()}>
                Skip
              </button>
            </div>
          </div>
        </div>
      )}
      <ul className="goals">
        {step === null && (
          <li>
            <span className="goal-label">Next</span>
            <span className="goal-text">Company Level {company.level + 1}</span>
            <span className="goal-bar">
              <span style={{ width: `${Math.round(levelShare * 100)}%` }} />
            </span>
          </li>
        )}
        {order !== undefined && (
          <li>
            <span className="goal-label">Today</span>
            <span className="goal-text">
              {formatAmount(order.amount)} {content.resourceName(order.resource).toLowerCase()} for the hub
            </span>
            <span className="goal-bar">
              <span style={{ width: `${Math.round((order.delivered / order.amount) * 100)}%` }} />
            </span>
          </li>
        )}
        {company.nextParcelCost !== null && company.nextParcelLevel !== null && (
          <li>
            <span className="goal-label">This week</span>
            <span className="goal-text">
              A new parcel: Level {company.nextParcelLevel} · {formatAmount(company.nextParcelCost)} g
            </span>
            <span className="goal-bar">
              <span
                style={{ width: `${Math.round(Math.min(1, company.gold / company.nextParcelCost) * 100)}%` }}
              />
            </span>
          </li>
        )}
      </ul>
    </aside>
  );
}
