import { useEffect, useState } from "react";
import { gameClient } from "../app/client";
import { askForNotifications, notificationsAsked, notificationsSupported } from "../app/notifications";
import { formatAmount } from "../app/format";
import type { NeighbourDto } from "../api/contracts";
import type { Content } from "../game/content";
import { useAppStore } from "../state/store";

/**
 * Always three goals in view (GDD §10.1): Loady's next step (or the next Company Level), today's order and something
 * for this week. Loady himself helps through the first session.
 */
export function GoalTracker() {
  const company = useAppStore((s) => s.company);
  const content = useAppStore((s) => s.content);
  const buildMenuOpen = useAppStore((s) => s.buildMenuOpen);
  const neighbours = useAppStore((s) => s.neighbours);
  const [asked, setAsked] = useState(notificationsAsked);
  const level = company?.level ?? 0;
  // Neighbours come with Level 3: look for someone who needs what you make now and then
  useEffect(() => {
    if (level < 3) {
      return;
    }
    void gameClient.refreshNeighbours();
    const timer = window.setInterval(() => void gameClient.refreshNeighbours(), 90_000);
    return () => clearInterval(timer);
  }, [level]);
  if (company === null || content === null || buildMenuOpen) {
    return null;
  }
  const opportunity = bestOpportunity(company.id, neighbours, content);
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
            {step.waitLevel != null && (
              <p className="loady-wait small">
                <strong>At Company Level {step.waitLevel}.</strong> Your company keeps earning while you're
                away: build, upgrade, or come back later.
              </p>
            )}
            <div className="loady-actions">
              {step.waitLevel == null && (
                <button className="primary" onClick={() => gameClient.showTutorialHint()}>
                  Show me
                </button>
              )}
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
      {(company.alerts ?? []).slice(0, 2).map((alert) => (
        <div key={alert.text} className="panel opportunity alert">
          <p>⚠ {alert.text}</p>
          {alert.building !== null && (
            <button onClick={() => gameClient.showBuilding(alert.building!)}>Show me</button>
          )}
        </div>
      ))}
      {opportunity !== null && (
        <div className="panel opportunity">
          <p>
            🤝 <strong>{opportunity.name}</strong> needs {opportunity.resource}: neighbours pay more than the
            hub.
          </p>
          <button className="primary" onClick={() => void gameClient.planConnection(opportunity.id)}>
            Connect a pipe
          </button>
        </div>
      )}
      {step !== null && step.step >= 2 && !asked && notificationsSupported() && (
        <div className="panel opportunity">
          <p>🔔 Want a note when a storage is full or research is done, while you're away?</p>
          <button
            className="primary"
            onClick={() => void askForNotifications().finally(() => setAsked(true))}
          >
            Notify me
          </button>
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

/** A neighbour you aren't connected to that needs something your buildings make */
function bestOpportunity(
  companyId: number,
  neighbours: NeighbourDto[],
  content: Content,
): { id: number; name: string; resource: string } | null {
  const world = gameClient.world;
  if (world === null) {
    return null;
  }
  const made = new Set<string>();
  for (const building of world.buildings.values()) {
    if (building.owner === companyId && building.constructionEndsAt === null) {
      Object.keys(content.building(building.type)?.out ?? {}).forEach((t) => made.add(t));
    }
  }
  // A building only buys when all it misses comes: a neighbour that misses energy and stone is no use to a company
  // that makes only energy
  for (const neighbour of neighbours) {
    const shortfall = neighbour.connected
      ? undefined
      : (neighbour.shortfalls ?? []).find((set) => set.every((t) => made.has(t)));
    if (shortfall !== undefined) {
      const names = shortfall.map((t) => content.resourceName(t).toLowerCase());
      const resource = names.length > 1 ? `${names.slice(0, -1).join(", ")} and ${names.at(-1)}` : names[0]!;
      return { id: neighbour.id, name: neighbour.name, resource };
    }
  }
  return null;
}
