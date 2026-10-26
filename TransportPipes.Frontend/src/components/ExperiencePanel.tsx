import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import type { CompanyDto } from "../api/contracts";
import { levelEta } from "../app/experience";
import { levelRoadmap } from "../app/roadmap";
import type { Content } from "../game/content";
import { useAppStore } from "../state/store";

const SOURCE_NAMES: Record<string, string> = {
  market: "The market",
  players: "Other players",
  orders: "Daily orders",
  achievements: "Achievements",
  rewards: "Rewards",
  cheats: "Cheats",
};

/** An item of a source: a good by its name, else as it is (an achievement, a reward) */
function itemName(content: Content, source: string, item: string): string {
  if (item === "storages") {
    return "What your Storages sold";
  }
  return source === "market" || source === "players" || source === "orders"
    ? content.resourceName(item)
    : item;
}

/**
 * The Company Level (1.4 §14): it comes from all the gold the company ever earned, so this is where the gold came from,
 * per source (open one for its goods), and what the next levels bring
 */
export function ExperiencePanel({
  company,
  content,
  onClose,
}: {
  company: CompanyDto;
  content: Content;
  onClose: () => void;
}) {
  const sources = Object.entries(company.earned.log)
    .map(([source, items]) => ({
      source,
      total: Object.values(items).reduce((a, b) => a + b, 0),
      items: Object.entries(items).sort((a, b) => b[1] - a[1]),
    }))
    .filter((t) => t.total > 0.05)
    .sort((a, b) => b.total - a.total);
  const era = useAppStore((s) => s.era);
  const left = Math.max(0, company.nextLevelEarned - company.lifetimeEarnings);
  const eta = levelEta(company);
  const roadmap = levelRoadmap(content, company, gameClient.world, era);
  return (
    <div className="panel xp-panel" role="dialog" aria-label="Company Level">
      <div className="panel-header">
        <h3>Company Level {company.level}</h3>
        <button className="icon-button" title="Close (Esc)" onClick={onClose}>
          ×
        </button>
      </div>
      <p>
        <strong>{formatAmount(left)} g</strong> more to earn for Level {company.level + 1}
        {eta !== null && (
          <>
            {" "}
            · about <strong>{formatDuration(eta)}</strong> at {formatAmount(company.earned.perMinute)} g/min
          </>
        )}
      </p>
      <p className="muted small">
        Your Company Level comes from all the gold you ever earned ({formatAmount(company.lifetimeEarnings)} g
        so far): spending it takes nothing away.
      </p>

      {roadmap.length > 0 && (
        <>
          <h4>Coming next</h4>
          <ul className="roadmap">
            {roadmap.map((r) => (
              <li key={r.level}>
                <span className="roadmap-level">Level {r.level}</span>
                <span className="roadmap-items">
                  {r.items.map((item) => (
                    <span
                      key={item.name}
                      className={`roadmap-item${item.ok === true ? " ok" : item.ok === false ? " no" : ""}`}
                    >
                      <strong>{item.name}</strong>
                      {item.note !== null && <span className="muted"> · {item.note}</span>}
                    </span>
                  ))}
                </span>
              </li>
            ))}
          </ul>
        </>
      )}

      <h4>
        Where your gold came from{" "}
        <button
          className="link small"
          onClick={() => {
            onClose();
            void gameClient.openStatistics(null, "earnedBy");
          }}
        >
          Over time 📈
        </button>
      </h4>
      {sources.length === 0 ? (
        <p className="muted small">Nothing yet: build something!</p>
      ) : (
        <ul className="xp-sources">
          {sources.map((s) => (
            <li key={s.source}>
              <details>
                <summary>
                  <span>{SOURCE_NAMES[s.source] ?? s.source}</span>
                  <span className="xp-amount">{formatAmount(s.total)} g</span>
                </summary>
                <ul className="xp-items">
                  {s.items.map(([item, gold]) => (
                    <li key={item}>
                      <span>{itemName(content, s.source, item)}</span>
                      <span className="xp-amount">{formatAmount(gold)} g</span>
                    </li>
                  ))}
                </ul>
              </details>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
