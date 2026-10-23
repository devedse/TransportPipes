import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import type { TrunksDto } from "../api/contracts";
import { useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

/**
 * Beyond your own hub (v1.2 §13): when your hub pays less than the full price (you sell more than its order), part of
 * your spare goes over the public copper trunks to the neighbouring hubs (their own order for you) or the Port (any
 * amount of tier-2 goods at 0.6), at a 2% fee; a crew's steel trunk loses less and costs nothing to use
 */
export function TrunksSection({ trunks }: { trunks: TrunksDto }) {
  const content = useAppStore((s) => s.content)!;
  const [target, setTarget] = useState(0);
  const materials = trunks.segmentMaterials
    .map((m) => `${formatAmount(m.perMinute)} ${content.resourceName(m.resource).toLowerCase()}`)
    .join(" + ");
  const chosen = trunks.targets[target];
  return (
    <section className="trunks">
      <h4>Beyond your hub</h4>
      {trunks.sales.length === 0 ? (
        <p className="muted small">
          Your hub pays the full price for all you sell: nothing needs to go further. When you sell more than
          its order, part of your spare goes to the next hubs over the public trunks.
        </p>
      ) : (
        <ul className="yard-trades">
          {trunks.sales.map((s) => (
            <li key={`${s.resource}-${s.market}`} className="small">
              <ResourceIcon resource={s.resource} /> {formatAmount(s.perMinute)}/min{" "}
              {content.resourceName(s.resource).toLowerCase()} to {s.market} at {s.price.toFixed(2)} g
              <span className="muted"> · {s.public ? "public trunk, 2% fee" : "your crew's trunk"}</span>
            </li>
          ))}
        </ul>
      )}
      {trunks.crew.length > 0 && (
        <ul className="trunk-list">
          {trunks.crew.map((t) => (
            <li key={t.id}>
              <div className="small">
                <strong>Trunk to {t.target}</strong>{" "}
                <span className="muted">
                  · {t.length} tiles of steel · {Math.round(t.efficiency * 100)}% arrives ·{" "}
                  {t.built ? "built" : `${t.segments} of ${t.needed} segments`}
                </span>
              </div>
              <div className="trunk-segments" aria-hidden="true">
                {Array.from({ length: t.needed }, (_, i) => (
                  <span key={i} style={i < t.colors.length ? { background: t.colors[i] } : undefined} />
                ))}
              </div>
              {!t.built && (
                <button
                  className="small"
                  onClick={() =>
                    void gameClient.linkCommand(
                      gameClient.connection.buildTrunkSegment(t.id),
                      "A segment laid",
                    )
                  }
                >
                  Pay a segment · {formatAmount(trunks.segmentGold)} g + {materials}
                </button>
              )}
            </li>
          ))}
        </ul>
      )}
      {trunks.canBuild ? (
        <span className="button-row">
          <select aria-label="Trunk to" value={target} onChange={(e) => setTarget(Number(e.target.value))}>
            {trunks.targets.map((t, i) => (
              <option key={t.name} value={i}>
                A trunk to {t.name}
              </option>
            ))}
          </select>
          <button
            disabled={chosen === undefined}
            title={`Steel, in 10-tile segments any of you pays: ${formatAmount(trunks.segmentGold)} g + ${materials} each`}
            onClick={() =>
              chosen !== undefined &&
              void gameClient.linkCommand(
                gameClient.connection.startTrunk(chosen.districtX, chosen.districtY, chosen.port),
                "The trunk is started: the first segment is yours",
              )
            }
          >
            Start it
          </button>
        </span>
      ) : (
        trunks.blocker !== null && <p className="muted small">{trunks.blocker}</p>
      )}
    </section>
  );
}
