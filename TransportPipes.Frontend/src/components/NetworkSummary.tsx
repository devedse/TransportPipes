import { gameClient } from "../app/client";
import { formatAmount, formatRate } from "../app/format";
import type { NetworkDetailsDto, NetworkResourceDto } from "../api/contracts";
import type { Content } from "../game/content";
import { ResourceIcon } from "./ResourceIcon";

/** Where a resource of a network goes: used by buildings, sold to the hub, stored, lost in the pipes */
const SEGMENTS = [
  { key: "used", name: "used by buildings", className: "seg-used" },
  { key: "sold", name: "sold to the hub", className: "seg-sold" },
  { key: "stored", name: "stored", className: "seg-stored" },
  { key: "lost", name: "lost in the pipes", className: "seg-lost" },
] as const;

function ResourceRow({ r, content }: { r: NetworkResourceDto; content: Content }) {
  const made = r.produced + r.imported + r.fromStorage;
  const short = Math.max(0, r.needed - r.used);
  const spare = Math.max(0, r.sold + r.stored);
  const isShort = r.needed > 0 && short > Math.max(0.05, r.needed * 0.01);
  const total = Math.max(made, r.used + r.sold + r.stored + r.lost, 1e-9);
  const values: Record<(typeof SEGMENTS)[number]["key"], number> = {
    used: r.used,
    sold: r.sold,
    stored: r.stored,
    lost: r.lost,
  };
  return (
    <div className="net-res">
      <div className="net-res-head">
        <ResourceIcon resource={r.resource} />
        <strong>{content.resourceName(r.resource)}</strong>
        {isShort ? (
          <span className="net-pill short">{formatAmount(short)} short</span>
        ) : spare > 0.05 ? (
          <span className="net-pill spare">{formatAmount(spare)} spare</span>
        ) : (
          <span className="net-pill even">balanced</span>
        )}
      </div>
      <div className="net-numbers">
        makes <strong>{formatAmount(made)}</strong>
        {r.needed > 0 && (
          <>
            {" "}
            · needs <strong>{formatAmount(r.needed)}</strong>
          </>
        )}
        <span className="muted"> per minute</span>
      </div>
      {made > 0 && (
        <div
          className="net-bar"
          title={SEGMENTS.map((s) => `${formatAmount(values[s.key])} ${s.name}`).join(" · ")}
        >
          {SEGMENTS.map((s) =>
            values[s.key] > 0 ? (
              <span
                key={s.key}
                className={s.className}
                style={{ width: `${(values[s.key] / total) * 100}%` }}
              />
            ) : null,
          )}
        </div>
      )}
      {isShort && (
        <div className="net-need">
          <div className="net-need-bar">
            <span style={{ width: `${Math.min(100, (r.used / r.needed) * 100)}%` }} />
          </div>
          <span className="small">
            the buildings get {formatAmount(r.used)} of the {formatAmount(r.needed)} they need
          </span>
        </div>
      )}
    </div>
  );
}

/**
 * What a network does: per resource what it makes against what its buildings need (short or spare) and where it
 * goes; per company the gold it earns or pays here, and from what
 */
export function NetworkSummary({
  details,
  content,
  ownCompany,
}: {
  details: NetworkDetailsDto;
  content: Content;
  ownCompany: number | null;
}) {
  const owners = [...details.owners].sort(
    (a, b) =>
      (b.id === ownCompany ? 1 : 0) - (a.id === ownCompany ? 1 : 0) || b.goldPerMinute - a.goldPerMinute,
  );
  const biggest = Math.max(1, ...owners.map((t) => Math.abs(t.goldPerMinute)));
  const lossy = details.resources
    .filter((r) => r.lost > 0.25 * (r.produced + r.imported + r.fromStorage) && r.lost > 1)
    .sort((a, b) => b.lost - a.lost)[0];
  return (
    <div className="net-summary">
      {/* What can't leave comes first (F2): a stuck building is never "short" of what it would use */}
      {(details.stuck ?? []).map((s) => (
        <button
          key={s.building}
          className="net-stuck link"
          onClick={() => gameClient.showBuilding(s.building)}
        >
          ⛔ {s.line} ›
        </button>
      ))}
      {details.resources.map((r) => (
        <ResourceRow key={r.resource} r={r} content={content} />
      ))}
      {details.resources.length > 0 && (
        <div className="net-legend small">
          {SEGMENTS.map((s) => (
            <span key={s.key}>
              <span className={`net-swatch ${s.className}`} /> {s.name}
            </span>
          ))}
        </div>
      )}
      {lossy !== undefined && (
        <p className="net-tip small">
          ⚠ {formatAmount(lossy.lost)} {content.resourceName(lossy.resource).toLowerCase()}/min is lost in
          long pipes. Better pipes lose less (Copper half as much), and shorter ways to your buildings and
          trading post help.
        </p>
      )}

      <h4 className="net-money-title">Money per minute</h4>
      <div className="net-money">
        {owners.map((owner) => {
          const mine = owner.id === ownCompany;
          const share = Math.abs(owner.goldPerMinute) / biggest;
          return (
            <div key={owner.id} className={mine ? "net-owner mine" : "net-owner"}>
              <div className="net-owner-row">
                <span className="company-dot" style={{ background: owner.color }} />
                <span className="net-owner-name">
                  {owner.name}
                  {mine && <span className="muted"> (you)</span>}
                </span>
                <span className={owner.goldPerMinute >= 0 ? "net-gold plus" : "net-gold minus"}>
                  {formatRate(owner.goldPerMinute)} g
                </span>
              </div>
              <div className="net-diverge">
                <span className="net-diverge-minus">
                  {owner.goldPerMinute < 0 && <span style={{ width: `${share * 100}%` }} />}
                </span>
                <span className="net-diverge-plus">
                  {owner.goldPerMinute > 0 && <span style={{ width: `${share * 100}%` }} />}
                </span>
              </div>
              {mine && (
                <div className="net-breakdown small">
                  {owner.hubSales > 0.05 && <span className="plus">hub +{formatAmount(owner.hubSales)}</span>}
                  {owner.neighbourSales > 0.05 && (
                    <span className="plus">neighbours +{formatAmount(owner.neighbourSales)}</span>
                  )}
                  {owner.neighbourPurchases > 0.05 && (
                    <span className="minus">
                      bought from neighbours −{formatAmount(owner.neighbourPurchases)}
                    </span>
                  )}
                  {owner.hubPurchases > 0.05 && (
                    <span className="minus">bought from the hub −{formatAmount(owner.hubPurchases)}</span>
                  )}
                </div>
              )}
            </div>
          );
        })}
      </div>
      {details.owners.length > 1 && (
        <button className="wide" onClick={() => void gameClient.openTradeSettings()}>
          ⚙ Trade settings: who you sell to and buy from
        </button>
      )}
    </div>
  );
}
