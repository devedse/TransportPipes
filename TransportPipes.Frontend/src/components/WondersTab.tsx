import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import type { WonderDto, WonderTier } from "../api/contracts";
import { useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

const TIERS: Array<{ tier: WonderTier; share: number; name: string }> = [
  { tier: "bronze", share: 0, name: "Bronze" },
  { tier: "silver", share: 0.01, name: "Silver" },
  { tier: "gold", share: 0.04, name: "Gold" },
  { tier: "diamond", share: 0.1, name: "Diamond" },
];

function WonderCard({ wonder, era }: { wonder: WonderDto; era: number }) {
  const content = useAppStore((s) => s.content)!;
  const canDeliver = wonder.needs.some((t) => t.yourStock > 0.5 && t.delivered < t.required);
  return (
    <div className={wonder.finished ? "wonder-card finished" : "wonder-card"}>
      <div className="wonder-head">
        <div>
          <div className="panel-title">
            {wonder.isEra ? `Era ${era} Wonder · the Capital` : "District Wonder"}
          </div>
          <h3>{wonder.name}</h3>
          <div className="small">{wonder.description}</div>
        </div>
        <div className="wonder-stage">
          {wonder.finished ? "Finished" : `Stage ${wonder.stage + 1} of ${wonder.stages}`}
          <div className="stage-dots">
            {Array.from({ length: wonder.stages }, (_, i) => (
              <span
                key={i}
                className={
                  i < wonder.stage ? "dot done" : i === wonder.stage && !wonder.finished ? "dot now" : "dot"
                }
              />
            ))}
          </div>
        </div>
      </div>
      {!wonder.finished && (
        <div className="wonder-needs">
          {wonder.needs.map((need) => (
            <div key={need.resource} className="wonder-need">
              <ResourceIcon resource={need.resource} />
              <span className="need-name">{content.resourceName(need.resource)}</span>
              <div className="meter-bar">
                <div
                  className="meter-fill"
                  style={{
                    width: `${Math.min(100, (need.delivered / need.required) * 100)}%`,
                    background: content.resourceColor(need.resource),
                  }}
                />
              </div>
              <span className="small">
                {formatAmount(need.delivered)} / {formatAmount(need.required)}
              </span>
              <span className="muted small" title="In your storages">
                you have {formatAmount(need.yourStock)}
              </span>
            </div>
          ))}
        </div>
      )}
      <div className="wonder-foot">
        <div className="small">
          You built {formatAmount(wonder.yourContribution)} g worth
          {wonder.total > 0 && ` · ${(wonder.yourShare * 100).toFixed(1)}% of all`}
          {wonder.isEra && wonder.yourTier !== "none" && (
            <span className={`tier ${wonder.yourTier}`}>
              {TIERS.find((t) => t.tier === wonder.yourTier)?.name}
            </span>
          )}
        </div>
        {!wonder.finished && (
          <button
            className="primary"
            disabled={!canDeliver}
            title="Delivers what your storages have for this stage"
            onClick={() => void gameClient.deliverToWonder(wonder.isEra)}
          >
            Deliver from storage
          </button>
        )}
      </div>
      {wonder.isEra && (
        <div className="muted small">
          Builders get a reward when the next Era starts: Bronze for any part, Silver from 1%, Gold from 4%,
          Diamond from 10% of the Wonder.
        </div>
      )}
      {wonder.topBuilders.length > 0 && (
        <div className="builders">
          {wonder.topBuilders.map((b, i) => (
            <span key={b.company} className="builder">
              {i + 1}. <span className="company-dot" style={{ background: b.color }} /> {b.name}{" "}
              <span className="muted">{formatAmount(b.value)} g</span>
            </span>
          ))}
        </div>
      )}
    </div>
  );
}

/** The District Wonder and the Era Wonder (GDD §9.3, §9.4) */
export function WondersTab({ serverNow }: { serverNow: number }) {
  const wonders = useAppStore((s) => s.wonders);
  if (wonders === null) {
    return <p className="muted">Loading…</p>;
  }
  const left = (new Date(wonders.eraDeadline).getTime() - serverNow) / 1000;
  return (
    <div className="wonders">
      <p className="muted small">
        Everyone builds the Wonders together from their storages. Era {wonders.era} ends when the Era Wonder
        is finished, at the latest in {formatDuration(Math.max(0, left))}.
      </p>
      {wonders.district !== null && <WonderCard wonder={wonders.district} era={wonders.era} />}
      {wonders.eraWonder !== null && <WonderCard wonder={wonders.eraWonder} era={wonders.era} />}
    </div>
  );
}
