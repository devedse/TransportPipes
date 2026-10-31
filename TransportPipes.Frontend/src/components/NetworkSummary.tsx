import { gameClient } from "../app/client";
import { formatAmount, formatRate } from "../app/format";
import type { NetworkDetailsDto, NetworkResourceDto } from "../api/contracts";
import type { Content } from "../game/content";
import { useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";
import { Term } from "./Term";

/** Where a resource of a network goes: used by buildings, sold to the hub, stored, lost in the pipes */
const SEGMENTS = [
  { key: "used", name: "used by buildings", className: "seg-used" },
  { key: "sold", name: "sold to the hub", className: "seg-sold" },
  { key: "toYard", name: "sold through the Yard", className: "seg-yard" },
  { key: "stored", name: "stored", className: "seg-stored" },
  { key: "lost", name: "lost in the pipes", className: "seg-lost" },
] as const;

function ResourceRow({ r, content }: { r: NetworkResourceDto; content: Content }) {
  const made = r.produced + r.imported + r.fromStorage;
  // What the Yard brings is part of what its buildings get, said apart from what it makes (run 2: "sand 15 short"
  // while Ben's sand ran the Glassworks)
  const got = made + r.fromYard;
  const short = Math.max(0, r.needed - r.used);
  const spare = Math.max(0, r.sold + r.stored + r.toYard);
  const isShort = r.needed > 0 && short > Math.max(0.05, r.needed * 0.01);
  const total = Math.max(got, r.used + r.sold + r.toYard + r.stored + r.lost, 1e-9);
  // Its buildings make less than they could, because what they make can't all leave (the owner, 2026-10-31: upgrading a
  // Smelter raised the copper ore made: the mines made only what it took)
  const slowed = r.slowWhy != null && (r.couldMake ?? 0) > r.produced + Math.max(0.5, r.produced * 0.02);
  const values: Record<(typeof SEGMENTS)[number]["key"], number> = {
    used: r.used,
    sold: r.sold,
    toYard: r.toYard,
    stored: r.stored,
    lost: r.lost,
  };
  return (
    <div className="net-res">
      <div className="net-res-head">
        <ResourceIcon resource={r.resource} />
        <strong>{content.resourceName(r.resource)}</strong>
        {isShort ? (
          <span className="net-pill short">
            {formatAmount(short)} <Term word="short">short</Term>
          </span>
        ) : spare > 0.05 ? (
          <span className="net-pill spare">
            {formatAmount(spare)} <Term word="spare">spare</Term>
          </span>
        ) : (
          <span className="net-pill even">balanced</span>
        )}
      </div>
      <div className="net-numbers">
        {(made > 0.05 || r.fromYard <= 0.05) && (
          <>
            makes <strong>{formatAmount(made)}</strong>
            {slowed && (
              <>
                {" "}
                of the <strong>{formatAmount((r.couldMake ?? 0) + made - r.produced)}</strong> it could
              </>
            )}
          </>
        )}
        {r.fromYard > 0.05 && (
          <>
            {made > 0.05 ? " · " : ""}
            the Yard brings <strong>{formatAmount(r.fromYard)}</strong>
          </>
        )}
        {r.needed > 0 && (
          <>
            {" "}
            · needs <strong>{formatAmount(r.needed)}</strong>
          </>
        )}
        <span className="muted"> per minute</span>
      </div>
      {got > 0 && (
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
      {got > 0 && (
        // The numbers behind the bar, and the gold the hub pays (playtest: "I want you to make the numbers clearly
        // traceable"): its hub sales add up to the hub's line under Money per minute
        <div className="net-where small muted">
          {[
            r.used > 0.05 && `used by buildings ${formatAmount(r.used)}`,
            // What goes into links said apart (the owner, 2026-10-31: "sold through the Yard 100", the buyer got 25)
            (r.toLinks ?? 0) > 0.05 &&
              `into your links ${formatAmount(r.toLinks ?? 0)} (what arrives, what fills your buyers' tanks, and what the links' pipes lose: Trades › Links)`,
            r.toYard - (r.toLinks ?? 0) > 0.05 &&
              `sold through the Yard ${formatAmount(r.toYard - (r.toLinks ?? 0))}`,
            r.stored > 0.05 && `stored ${formatAmount(r.stored)}`,
            r.lost > 0.05 && `lost in the pipes ${formatAmount(r.lost)}`,
          ]
            .filter(Boolean)
            .join(" · ")}
          {r.sold > 0.05 && (
            <span className="net-where-gold">
              {r.used > 0.05 || r.toYard > 0.05 || r.stored > 0.05 || r.lost > 0.05 ? " · " : ""}
              sold to the hub {formatAmount(r.sold)} × {((r.soldGold ?? 0) / r.sold).toFixed(2)} g ={" "}
              <span className="coin" /> +{formatAmount(r.soldGold ?? 0)} g
            </span>
          )}
        </div>
      )}
      {slowed && (
        <p className="net-slow small">
          {r.slowWhy}{" "}
          {r.slowFirst != null && (
            <button className="link small" onClick={() => void gameClient.showBuilding(r.slowFirst!)}>
              Show it ›
            </button>
          )}
        </p>
      )}
      {isShort && (
        <div className="net-need">
          <div className="net-need-bar">
            <span style={{ width: `${Math.min(100, (r.used / r.needed) * 100)}%` }} />
          </div>
          <span className="small">
            the buildings get {formatAmount(r.used)} of the {formatAmount(r.needed)} they need
            {/* Both spare and short: the spare can't reach them (run 2: Cleo's "two numbers that disagree") */}
            {spare > 0.05 && ": the spare can't reach them, since goods never pass through a building"}
            {r.shortWhy != null && ` · ${r.shortWhy}`}
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
  const allMine = useAppStore((s) => s.company?.goldPerMinute ?? null);
  const here = owners.find((t) => t.id === ownCompany)?.goldPerMinute ?? null;
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

      {/* Gold says so with its coin (playtest: "when I click a pipe I clearly want to see the gold icon if it's about gold") */}
      <h4 className="net-money-title">
        <span className="coin" /> Money per minute
      </h4>
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
                  <span className="coin" /> {formatRate(owner.goldPerMinute)} g
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
                  {owner.hubSales > 0.05 && (
                    <span className="plus" title="Its goods' sales to the hub above, added up">
                      hub +{formatAmount(owner.hubSales)}
                    </span>
                  )}
                  {owner.neighbourSales > 0.05 && (
                    <span className="plus">the Yard +{formatAmount(owner.neighbourSales)}</span>
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
      {/* After run 4: this network's +2,547, the top bar's +2,574 and "hub +2,598" side by side, unexplained */}
      {allMine !== null && here !== null && Math.abs(allMine - here) >= 1 && (
        <p className="muted small">
          This network&apos;s pipes now. Your top bar&apos;s {formatRate(allMine)} counts all your pipes, and
          what your neighbours and links pay too.
        </p>
      )}
      {details.owners.length > 1 && (
        <button className="wide" onClick={() => void gameClient.openTradeSettings()}>
          ⚙ Trade settings: who you sell to and buy from
        </button>
      )}
    </div>
  );
}
