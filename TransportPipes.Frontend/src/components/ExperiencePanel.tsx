import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import type { CompanyDto } from "../api/contracts";
import { levelEta } from "../app/experience";
import { levelRoadmap } from "../app/roadmap";
import type { Content } from "../game/content";
import { useAppStore } from "../state/store";

const SOURCE_NAMES: Record<string, string> = {
  earnings: "Gold earned",
  buildings: "Buildings",
  upgrades: "Upgrades",
  parcels: "Parcels",
  research: "Research",
  orders: "Daily orders",
  contracts: "Contracts",
  achievements: "Achievements",
  corporation: "Corporation goals",
  likes: "Likes",
  cheats: "Cheats",
};

/** An item of a source: a building type or a resource by its name, else as it is (a tech, an achievement) */
function itemName(content: Content, source: string, item: string): string {
  if (source === "buildings" || source === "upgrades") {
    return content.building(item)?.name ?? item;
  }
  if (source === "orders" || source === "contracts" || source === "corporation") {
    return content.resourceName(item);
  }
  return item;
}

/** Where a company's XP came from, per source (open one for its items), and what gives XP */
export function ExperiencePanel({
  company,
  content,
  onClose,
}: {
  company: CompanyDto;
  content: Content;
  onClose: () => void;
}) {
  const sources = Object.entries(company.experience.log)
    .map(([source, items]) => ({
      source,
      total: Object.values(items).reduce((a, b) => a + b, 0),
      items: Object.entries(items).sort((a, b) => b[1] - a[1]),
    }))
    .filter((t) => t.total > 0.05)
    .sort((a, b) => b.total - a.total);
  const era = useAppStore((s) => s.era);
  const left = Math.max(0, company.nextLevelXp - company.xp);
  const eta = levelEta(company);
  const roadmap = levelRoadmap(content, company, gameClient.world, era);
  return (
    <div className="panel xp-panel" role="dialog" aria-label="Experience">
      <div className="panel-header">
        <h3>Company Level {company.level}</h3>
        <button className="icon-button" title="Close (Esc)" onClick={onClose}>
          ×
        </button>
      </div>
      <p>
        <strong>{formatAmount(left)} XP</strong> to Level {company.level + 1}
        {eta !== null && (
          <>
            {" "}
            · about <strong>{formatDuration(eta)}</strong> at +{formatAmount(company.experience.perMinute)}{" "}
            XP/min
          </>
        )}
      </p>
      <p className="muted small">
        The XP per minute is what your earnings bring now; building, upgrading and research add more on top.
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

      <h4>Where your XP came from</h4>
      {sources.length === 0 ? (
        <p className="muted small">Nothing yet: build something!</p>
      ) : (
        <ul className="xp-sources">
          {sources.map((s) => (
            <li key={s.source}>
              <details>
                <summary>
                  <span>{SOURCE_NAMES[s.source] ?? s.source}</span>
                  <span className="xp-amount">{formatAmount(s.total)} XP</span>
                </summary>
                <ul className="xp-items">
                  {s.items.map(([item, xp]) => (
                    <li key={item}>
                      <span>{itemName(content, s.source, item)}</span>
                      <span className="xp-amount">{formatAmount(xp)} XP</span>
                    </li>
                  ))}
                </ul>
              </details>
            </li>
          ))}
        </ul>
      )}

      <h4>What gives XP</h4>
      <ul className="xp-rules small">
        {company.experience.rules.map((rule) => (
          <li key={rule.source}>
            <strong>{SOURCE_NAMES[rule.source] ?? rule.source}:</strong> {rule.text}
          </li>
        ))}
      </ul>
    </div>
  );
}
