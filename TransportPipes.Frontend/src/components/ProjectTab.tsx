import { useEffect } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { ProjectHelperDto } from "../api/contracts";
import { useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

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
 * basket lacks from everyone's spare at the hub's full price, with no button; every finished stage lifts every seat.
 */
export function ProjectTab() {
  const project = useAppStore((s) => s.project);
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
                    {!good.madeHere && <span className="muted small"> · nobody here makes it yet</span>}
                    <span className="grow" />
                    <span className="small">
                      {formatAmount(good.delivered)} / {formatAmount(good.target)}
                    </span>
                  </div>
                  <span className="tank-bar">
                    <span style={{ width: `${Math.min(100, share * 100)}%` }} />
                  </span>
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
                    My spare {content.resourceName(good.resource).toLowerCase()} goes here before my
                    neighbours
                  </label>
                </li>
              );
            })}
          </ul>
        </section>
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
          <div className="muted">
            You may still bring {formatAmount(project.yourAllowance)} g worth: two hours of a player&apos;s
            output a day, banked for three days, so nobody carries it alone.
          </div>
        </div>
      </section>
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
