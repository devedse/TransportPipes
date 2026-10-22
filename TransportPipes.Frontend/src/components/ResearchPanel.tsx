import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { ResearchBranch, ResearchDefinition, ResearchStateDto } from "../api/contracts";
import type { Content } from "../game/content";
import { setState, useAppStore } from "../state/store";

const BRANCHES: Array<{ id: ResearchBranch; name: string; icon: string }> = [
  { id: "energy", name: "Energy", icon: "⚡" },
  { id: "industry", name: "Industry", icon: "🏭" },
  { id: "logistics", name: "Logistics", icon: "⧉" },
  { id: "economy", name: "Economy", icon: "💰" },
];

const ERAS = [1, 2, 3, 4, 5];

function effectText(tech: ResearchDefinition, content: Content): string | null {
  const parts: string[] = [];
  for (const bonus of tech.outputBonuses) {
    const targets = bonus.targets
      .map((target) => {
        const [kind, value = ""] = target.split(":");
        if (kind === "modifier") {
          return value === "wind" ? "wind power" : "solar power";
        }
        if (kind === "building") {
          return content.building(value)?.name ?? value;
        }
        if (kind === "resource") {
          return content.resourceName(value).toLowerCase();
        }
        return value;
      })
      .join(", ");
    parts.push(`+${Math.round(bonus.amount * 100)}% ${targets}`);
  }
  if (tech.throughputBonus > 0) {
    parts.push(`+${Math.round(tech.throughputBonus * 100)}% pipe throughput`);
  }
  const unlocks = content.buildings.filter((b) => b.unlock.research === tech.id).map((b) => b.name);
  const pipes = content.pipeTiers.filter((p) => p.research === tech.id).map((p) => p.name);
  if (unlocks.length + pipes.length > 0) {
    parts.push(`Unlocks ${[...unlocks, ...pipes].join(", ")}`);
  }
  return parts.length > 0 ? parts.join(" · ") : null;
}

function TechCard({
  tech,
  state,
  points,
}: {
  tech: ResearchDefinition;
  state: ResearchStateDto | undefined;
  points: number;
}) {
  const content = useAppStore((s) => s.content)!;
  const company = useAppStore((s) => s.company);
  const now = useNow();
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const status = state?.state ?? "locked";
  const effect = effectText(tech, content);
  const cost = state?.cost ?? tech.cost;
  let progress = 0;
  if (status === "researching" && company?.researchEndsAt) {
    const left = new Date(company.researchEndsAt).getTime() - (now + serverOffset);
    progress = Math.max(0, Math.min(1, 1 - left / (tech.minutes * 60000)));
  }
  return (
    <div className={`tech ${status}`} title={tech.description}>
      <div className="tech-name">
        {status === "done" && "✔ "}
        {tech.name}
      </div>
      {effect !== null && <div className="tech-effect">{effect}</div>}
      {status === "done" ? null : status === "researching" ? (
        <div className="tech-progress">
          <div className="meter-bar">
            <div
              className="meter-fill"
              style={{ width: `${progress * 100}%`, background: "var(--accent)" }}
            />
          </div>
          <span className="small">{formatDuration((1 - progress) * tech.minutes * 60)} left</span>
        </div>
      ) : (
        <div className="tech-cost">
          <span>
            🔬 {formatAmount(cost)}
            {cost < tech.cost && (
              <span className="muted"> (−{Math.round((1 - cost / tech.cost) * 100)}%)</span>
            )}
          </span>
          <span className="muted"> · then {formatDuration(tech.minutes * 60)}</span>
          {/* The points come first (Cleo's replay read "5 min" as the whole wait) */}
          {status === "available" && points < cost && (company?.researchPerMinute ?? 0) > 0 && (
            <span className="muted">
              {" "}
              · enough points in {formatDuration(((cost - points) / company!.researchPerMinute) * 60)}
            </span>
          )}
        </div>
      )}
      {status === "available" && (
        <button
          className="primary"
          disabled={points < cost}
          onClick={() => void gameClient.startResearch(tech.id)}
        >
          {points < cost ? `Need ${formatAmount(cost - points)} more` : "Research"}
        </button>
      )}
      {status === "locked" && state?.reason && <div className="lock small">🔒 {state.reason}</div>}
    </div>
  );
}

/** The research tree: branches by Era */
export function ResearchPanel() {
  const open = useAppStore((s) => s.researchOpen);
  const content = useAppStore((s) => s.content);
  const company = useAppStore((s) => s.company);
  const states = useAppStore((s) => s.research);
  if (!open || content === null || company === null) {
    return null;
  }
  const byId = new Map(states.map((t) => [t.id, t]));
  return (
    <div className="modal-backdrop" onClick={() => setState({ researchOpen: false })}>
      <div
        className="panel research-panel"
        role="dialog"
        aria-label="Research"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="panel-header">
          <h2>Research</h2>
          <div className="research-points">
            🔬 <strong>{formatAmount(company.researchPoints)}</strong> research points
            <span className="muted"> · {formatAmount(company.researchPerMinute)}/min from your labs</span>
          </div>
          <button
            className="icon-button"
            title="Close (Esc)"
            onClick={() => setState({ researchOpen: false })}
          >
            ×
          </button>
        </div>
        <p className="muted small">
          Labs turn energy and iron ingots into research points. Techs get cheaper the more active companies
          already have them.
        </p>
        {company !== null && company.mastery.available && (
          <div className="mastery">
            <strong>
              🎓 Mastery {company.mastery.level + 1}
              {company.mastery.level > 0 &&
                ` · now +${(company.mastery.bonus * 100).toFixed(1)}% on everything`}
            </strong>
            <span className="muted small">
              The whole tree is done: every Mastery step makes everything 0.5% more, for good.
            </span>
            <button
              className="primary"
              disabled={company.currentResearch !== null || company.researchPoints < company.mastery.cost}
              onClick={() => void gameClient.startResearch("mastery")}
            >
              {company.currentResearch === "mastery"
                ? "Researching…"
                : `Research · ${formatAmount(company.mastery.cost)} points, ${formatDuration(company.mastery.minutes * 60)}`}
            </button>
          </div>
        )}
        <div className="research-grid">
          <div />
          {ERAS.map((era) => (
            <div key={era} className="era-head">
              Era {era}
            </div>
          ))}
          {BRANCHES.map((branch) => (
            <div key={branch.id} className="branch-row">
              <div className="branch-head">
                <span className="branch-icon">{branch.icon}</span>
                {branch.name}
              </div>
              {ERAS.map((era) => (
                <div key={era} className="era-cell">
                  {content.researchList
                    .filter((t) => t.branch === branch.id && t.era === era)
                    .map((tech) => (
                      <TechCard
                        key={tech.id}
                        tech={tech}
                        state={byId.get(tech.id)}
                        points={company.researchPoints}
                      />
                    ))}
                </div>
              ))}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
