import { useEffect, useState } from "react";
import { gameClient } from "../app/client";
import { askForNotifications, notificationsAsked, notificationsSupported } from "../app/notifications";
import { formatAmount } from "../app/format";
import type { NeighbourDto } from "../api/contracts";
import type { Content } from "../game/content";
import { useAppStore } from "../state/store";
import { NextSteps } from "./NextSteps";

/** A step's first clause, for its one-line form: "Built, but stuck", "Mine the iron on your land" */
function firstClause(text: string) {
  const end = text.search(/[.:!]/);
  return end > 0 ? text.slice(0, end) : text;
}

/**
 * Always three goals in view (GDD §10.1): Loady's next step (or the next Company Level), today's order and something
 * for this week. Loady himself helps through the first session.
 */
export function GoalTracker() {
  const company = useAppStore((s) => s.company);
  const content = useAppStore((s) => s.content);
  const buildMenuOpen = useAppStore((s) => s.buildMenuOpen);
  const neighbours = useAppStore((s) => s.neighbours);
  const nextSteps = useAppStore((s) => s.nextSteps);
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
  const step = company.tutorial;
  // The advisor for everyone from Level 3 (F3: a single line hid it from 4 of 5 players in run 1), and before that
  // when Loady is done or a step waits for a level: it covers the pump and trading post alerts and the neighbour who
  // needs something
  const advisor = level >= 3 || step === null || step.chapter >= 2 || step.waitLevel != null;
  const phone = window.matchMedia?.("(max-width: 720px)").matches === true;
  const text = step === null ? "" : phone && step.phoneText !== null ? step.phoneText : step.text;
  // A step open for two days shrinks to one line, with what holds it up
  // (on the server's clock, which a playtest runs fast)
  const stale =
    step !== null &&
    step.since !== null &&
    new Date(company.serverTime).getTime() - new Date(step.since).getTime() > 2 * 86_400_000;
  const opportunity = advisor ? null : bestOpportunity(company.id, neighbours, content);
  // The advisor's steps cover pumps and a full trading post; a full Storage and storm damage stay (damage with Repair
  // all while it lasts: run 2, Ben lost it with the return card)
  const alerts = (company.alerts ?? []).filter(
    (t) => !advisor || t.kind === "storage" || t.kind === "damage",
  );
  // Hub orders show once they are filled, never as a task (after run 3, step 7)
  const filled = company.dailyOrders.filter((t) => t.delivered >= t.amount - 1e-9);
  const levelShare = Math.min(
    1,
    (company.lifetimeEarnings - company.levelEarned) /
      Math.max(1, company.nextLevelEarned - company.levelEarned),
  );
  return (
    <aside className="goal-tracker" aria-label="Goals">
      {step !== null && stale && (
        <div className="panel loady-line" aria-live="polite">
          <img className="loady-mini" src="/art/ui/loady-point.webp" alt="Loady" />
          <span className="small">
            <strong>
              Loady {step.chapterStep}/{step.chapterSteps}
            </strong>{" "}
            · {firstClause(step.tip ?? text)}
          </span>
          {step.waitLevel == null && (
            <button className="small" onClick={() => gameClient.showTutorialHint()}>
              Show me
            </button>
          )}
          <button className="link small" onClick={() => void gameClient.skipTutorial()}>
            Skip
          </button>
        </div>
      )}
      {step !== null && !stale && step.chapter >= 2 && (
        <div className="panel loady-goal" aria-live="polite">
          <img className="loady-mini" src="/art/ui/loady-point.webp" alt="Loady" />
          <div className="loady-goal-body">
            <div className="loady-goal-head">
              <strong>Loady · Chapter {step.chapter}</strong>
              <span className="small muted">
                {step.chapterStep}/{step.chapterSteps} · +{formatAmount(step.gold)} g
              </span>
            </div>
            <p>{text}</p>
            {step.tip != null && <p className="loady-wait small">{step.tip}</p>}
            {step.waitEra != null ? (
              <p className="loady-wait small">
                <strong>When your town reaches Era {step.waitEra}.</strong> Its Wonder brings it: give to it
                in Hub › Town &amp; Wonder.
              </p>
            ) : step.waitLevel != null ? (
              <p className="loady-wait small">
                <strong>At Company Level {step.waitLevel}.</strong>{" "}
                {nextSteps.length > 0
                  ? "The next steps below get you there."
                  : "Your company keeps earning while you're away: build, upgrade, or come back later."}
              </p>
            ) : (
              <span className="goal-bar">
                <span style={{ width: `${Math.round(Math.min(1, step.progress / step.amount) * 100)}%` }} />
              </span>
            )}
            <div className="loady-actions">
              {step.waitLevel == null && step.waitEra == null && (
                <button className="small" onClick={() => gameClient.showTutorialHint()}>
                  Show me
                </button>
              )}
              <button className="link small" onClick={() => void gameClient.skipTutorial()}>
                Skip
              </button>
            </div>
          </div>
        </div>
      )}
      {step !== null && !stale && step.chapter < 2 && (
        <div className="loady-card" aria-live="polite">
          <img
            className="loady"
            src={step.step === 0 ? "/art/ui/loady-wave.webp" : "/art/ui/loady-point.webp"}
            alt="Loady"
          />
          <div className="loady-bubble">
            <p>{text}</p>
            {step.tip != null && <p className="loady-wait small">{step.tip}</p>}
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
                {step.chapterStep}/{step.chapterSteps} · +{formatAmount(step.gold)} g
              </span>
              <button className="link small" onClick={() => void gameClient.skipTutorial()}>
                Skip
              </button>
            </div>
          </div>
        </div>
      )}
      {advisor && <NextSteps />}
      {alerts.slice(0, 2).map((alert) => (
        <div key={alert.text} className="panel opportunity alert">
          <p>
            {alert.kind === "damage" ? "⛈" : "⚠"} {alert.text}
          </p>
          {alert.kind === "damage" ? (
            <button onClick={() => void gameClient.repairAll()}>
              Repair all · {formatAmount(alert.cost ?? 0)} g
            </button>
          ) : (
            alert.building !== null && (
              <button onClick={() => gameClient.showBuilding(alert.building!)}>Show me</button>
            )
          )}
        </div>
      ))}
      {opportunity !== null && (
        <div className="panel opportunity">
          <p>
            🤝 <strong>{opportunity.name}</strong> needs {opportunity.resource}: neighbours pay more than the
            hub.
          </p>
          <button className="primary" onClick={() => void gameClient.planYardPipe()}>
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
        {(step === null || step.chapter >= 2) && (
          <li>
            <span className="goal-label">Next</span>
            <span className="goal-text">Company Level {company.level + 1}</span>
            <span className="goal-bar">
              <span style={{ width: `${Math.round(levelShare * 100)}%` }} />
            </span>
          </li>
        )}
        {filled.length > 0 && (
          <li>
            <span className="goal-label">Today</span>
            <button
              className="goal-text link"
              title="The hub's daily orders: open them"
              onClick={() => void gameClient.openHub("orders")}
            >
              ✔ {filled.map((t) => content.resourceName(t.resource).toLowerCase()).join(", ")} filled{" "}
              {filled.length === 1 ? "a hub order" : `${filled.length} hub orders`}: +
              {formatAmount(filled.reduce((sum, t) => sum + t.premium, 0))} g ›
            </button>
            <span className="goal-bar">
              <span style={{ width: "100%" }} />
            </span>
          </li>
        )}
        {(company.wants ?? []).length > 0 &&
          (() => {
            // Your own scaffold, said as it is (after run 4: "is being built", and it waited for bricks to the end)
            const want = company.wants![0]!;
            const left = Math.max(0, want.amount - want.got);
            return (
              <li>
                <span className="goal-label">Waits</span>
                <span className="goal-text">
                  🏗 Your {want.name} waits for {formatAmount(left)}{" "}
                  {content.resourceName(want.resource).toLowerCase()}
                  {want.order !== null
                    ? ` · ${want.order}`
                    : want.perMinute > 0.005
                      ? ` · ${formatAmount(want.perMinute)}/min coming`
                      : ""}
                </span>
                {want.order === null && want.orderCost !== null && (
                  <button
                    className="small goal-show"
                    disabled={company.gold < want.orderCost}
                    onClick={() => void gameClient.orderRest(want.target)}
                  >
                    Buy the rest · {formatAmount(want.orderCost)} g
                  </button>
                )}
                <span className="goal-bar">
                  <span
                    style={{
                      width: `${Math.round(Math.min(1, want.got / Math.max(1, want.amount)) * 100)}%`,
                    }}
                  />
                </span>
              </li>
            );
          })()}
        {company.pipes !== null && (
          // The pipe ladder while a pipe is full (after run 4: the way up was in nobody's sight)
          <li>
            <span className="goal-label">Pipes</span>
            <button
              className="goal-text link"
              title={`Your pipes: ${company.pipes.ladder}. Open the building: its panel has the way up.`}
              onClick={() => gameClient.showBuilding(company.pipes!.building)}
            >
              🔧 {company.pipes.text} ›
            </button>
            <span className="goal-bar">
              <span style={{ width: "100%" }} />
            </span>
          </li>
        )}
        {company.eraWonderLine !== null && (
          <li>
            <span className="goal-label">Era</span>
            <button
              className="goal-text link"
              title="Your town's Era Wonder: open Town & Wonder"
              onClick={() => void gameClient.openHub("wonders")}
            >
              {company.eraWonderLine.text} ›
            </button>
            <span className="goal-bar">
              <span style={{ width: `${Math.round(company.eraWonderLine.share * 100)}%` }} />
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
