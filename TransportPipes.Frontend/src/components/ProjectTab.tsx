import { useEffect } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { ProjectHelperDto } from "../api/contracts";
import { useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

/** "Ben", "Ben and Dev", "Ben, Dev and you" */
function names(items: string[]): string {
  return items.length <= 1 ? (items[0] ?? "") : `${items.slice(0, -1).join(", ")} and ${items.at(-1)}`;
}

function capital(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1);
}

/** Who helped, as a ring of colours (shares of the credit), never a list of who didn't */
function HelperRing({ helpers }: { helpers: ProjectHelperDto[] }) {
  const size = 64;
  const r = 24;
  const circumference = 2 * Math.PI * r;
  // Each helper's arc starts where the one before it ends
  const arcs = helpers.map((h, i) => ({
    helper: h,
    length: h.share * circumference,
    start: helpers.slice(0, i).reduce((sum, t) => sum + t.share * circumference, 0),
  }));
  return (
    <svg
      className="helper-ring"
      width={size}
      height={size}
      viewBox={`0 0 ${size} ${size}`}
      aria-hidden="true"
    >
      <circle cx={size / 2} cy={size / 2} r={r} className="ring-track" />
      {/* A dark edge under every arc, so a white or pale company colour still shows on the cream panel */}
      {arcs.map(({ helper, length, start }, i) => (
        <circle
          key={`edge-${i}`}
          cx={size / 2}
          cy={size / 2}
          r={r}
          fill="none"
          className="ring-edge"
          strokeWidth={(helper.you ? 10 : 7) + 2}
          strokeDasharray={`${length} ${circumference - length}`}
          strokeDashoffset={-start}
          transform={`rotate(-90 ${size / 2} ${size / 2})`}
        />
      ))}
      {arcs.map(({ helper, length, start }, i) => (
        <circle
          key={i}
          cx={size / 2}
          cy={size / 2}
          r={r}
          fill="none"
          stroke={helper.color}
          strokeWidth={helper.you ? 10 : 7}
          strokeDasharray={`${length} ${circumference - length}`}
          strokeDashoffset={-start}
          transform={`rotate(-90 ${size / 2} ${size / 2})`}
        />
      ))}
    </svg>
  );
}

/**
 * The station's project (v1.2 §12): Foundation → Water Tower → Workshop → Foundry Hall → Landmark. It buys what its
 * basket lacks from everyone's spare at the one player price, with no button; every finished stage lifts every seat.
 */
export function ProjectTab() {
  const project = useAppStore((s) => s.project);
  const companyId = useAppStore((s) => s.company?.id ?? null);
  const content = useAppStore((s) => s.content);
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow(30000) + serverOffset;
  useEffect(() => {
    const timer = window.setInterval(() => void gameClient.refreshProject(), 30000);
    return () => window.clearInterval(timer);
  }, []);
  if (project === null || content === null) {
    return <p className="muted">Loading…</p>;
  }
  const done = project.stageIndex >= project.stages.length;
  // What your own buildings make: only those goods can go to the project first
  const makes = new Set<string>();
  for (const building of gameClient.world?.buildings.values() ?? []) {
    if (building.owner === companyId && building.constructionEndsAt === null) {
      Object.keys(content.building(building.type)?.out ?? {}).forEach((t) => makes.add(t));
    }
  }
  return (
    <>
      <ol className="project-stages">
        {project.stages.map((name, i) => (
          <li
            key={name}
            className={i < project.stageIndex ? "done" : i === project.stageIndex ? "current" : undefined}
          >
            {name}
          </li>
        ))}
      </ol>
      <p className="yard-line">{project.line}</p>
      {!done && (
        <section>
          <h4>The {project.stage}&apos;s basket</h4>
          <ul className="project-basket">
            {project.basket.map((good) => {
              const share = good.target <= 0 ? 1 : good.delivered / good.target;
              const first = project.projectFirst.includes(good.resource);
              return (
                <li key={good.resource} className={good.keystone ? "keystone" : undefined}>
                  <div className="basket-head">
                    <ResourceIcon resource={good.resource} />
                    <strong>{content.resourceName(good.resource)}</strong>
                    {good.keystone && (
                      <span
                        className="badge"
                        title="Nobody's land here is rich in it: +20% credit for whoever brings it"
                      >
                        keystone
                      </span>
                    )}
                    <span className="grow" />
                    <span className="small">
                      {formatAmount(good.delivered)} / {formatAmount(good.target)}
                    </span>
                  </div>
                  <span className="tank-bar">
                    <span style={{ width: `${Math.min(100, share * 100)}%` }} />
                  </span>
                  <span className="muted small">
                    {!good.madeHere || good.keystone
                      ? good.makers.length > 0
                        ? `${capital(names(good.makers))} can make it${good.makerBuilding === null ? "" : `: ${good.makerBuilding}`}`
                        : "Nobody here can make it yet."
                      : `Pays ${good.price.toFixed(2)} g a unit, the one player price${good.keystone ? ", and 20% more credit" : ""}.`}
                  </span>
                  {(first || makes.has(good.resource)) && (
                    <label className="check small">
                      <input
                        type="checkbox"
                        checked={first}
                        onChange={() =>
                          void gameClient.connection
                            .setProjectFirst(good.resource, !first)
                            .then(() => gameClient.refreshProject())
                        }
                      />
                      Send ▸ my spare {content.resourceName(good.resource).toLowerCase()} here first (what my
                      neighbours already take stays theirs)
                    </label>
                  )}
                </li>
              );
            })}
          </ul>
        </section>
      )}
      {!done && (
        <p className="small">
          {project.goingIn.length > 0 &&
            `Your ${names(project.goingIn.map((t) => content.resourceName(t).toLowerCase()))} ${project.goingIn.length === 1 ? "goes" : "go"} in now. `}
          {project.minutesLeft !== null &&
            project.minutesLeft > 0 &&
            `At this pace: about ${formatDuration(project.minutesLeft * 60)}.`}
        </p>
      )}
      {project.opensAt !== null && new Date(project.opensAt).getTime() > now && (
        <p className="muted small">
          Opens in {formatDuration((new Date(project.opensAt).getTime() - now) / 1000)}
        </p>
      )}
      <section className="project-helpers">
        <HelperRing helpers={project.helpers} />
        <div className="small">
          {project.helpers.length === 0 ? (
            <span className="muted">Nobody has delivered to this stage yet.</span>
          ) : (
            <span>
              {project.helpers.length === 1
                ? "One company helps"
                : `${project.helpers.length} companies help`}
              {project.yourCredit > 0 && `: your part is worth ${formatAmount(project.yourCredit)} g`}
            </span>
          )}
          {project.shareCapped && (
            <div className="muted">
              Your share is {formatAmount(project.sharePerDay)} g a day (two hours of your output), so nobody
              carries it alone: the rest of your spare goes to your neighbours and the hub.
            </div>
          )}
        </div>
      </section>
      {!done && (
        <p className="small">
          {project.stageBonus > 0
            ? `When it stands: every seat makes ${Math.round(project.stageBonus * 100)}% more, about +${formatAmount(project.yourGain)} g/min for you${project.stationGain > project.yourGain * 1.01 ? ` and +${formatAmount(project.stationGain)} for ${project.station}` : ""}.`
            : project.stage === "Foundry Hall"
              ? "When it stands: +10% on the station's two most-traded chains."
              : "When it stands: the station's Landmark, and its heirloom for everyone who helped."}
        </p>
      )}
      {(project.bonus > 0 || project.foundryChains.length > 0) && (
        <p className="small">
          🏗 Every seat of {project.station} makes {Math.round(project.bonus * 100)}% more
          {project.foundryChains.length > 0 &&
            `, and 10% more ${project.foundryChains.map((t) => content.resourceName(t).toLowerCase()).join(" and ")}`}
          .
        </p>
      )}
    </>
  );
}
