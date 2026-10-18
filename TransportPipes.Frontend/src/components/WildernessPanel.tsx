import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import { useAppStore } from "../state/store";

/** The wilderness (GDD §4.6): an outpost, or claiming one on a rich deposit */
export function WildernessPanel() {
  const tile = useAppStore((s) => s.wildTile);
  const check = useAppStore((s) => s.outpostCheck);
  const company = useAppStore((s) => s.company);
  const content = useAppStore((s) => s.content);
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow(60000) + serverOffset;
  if (tile === null || company === null || content === null) {
    return null;
  }
  const outpost = gameClient.world?.outpostAt(tile.x, tile.y);
  const info = gameClient.world?.tile(tile.x, tile.y);
  const own = outpost !== undefined && outpost.owner === company.id;
  const mine = own ? company.outposts.find((t) => t.id === outpost.id) : undefined;
  return (
    <aside className="panel parcel-panel wild-panel">
      <div className="panel-header">
        <h3>{outpost !== undefined ? `⛺ Outpost of ${outpost.ownerName}` : "The wilderness"}</h3>
        <button className="icon-button" title="Close (Esc)" onClick={() => gameClient.closeWild()}>
          ×
        </button>
      </div>
      {info !== null && info !== undefined && info.deposit !== null && info.richness > 0 && (
        <p className="small">
          <span className="flow-dot" style={{ background: content.resourceColor(info.deposit) }} />{" "}
          {content.resourceName(info.deposit)} deposit {"★".repeat(info.richness)}
        </p>
      )}
      {outpost !== undefined ? (
        own && mine !== undefined ? (
          <>
            <p className="small">
              Lease {formatAmount(mine.lease ?? 0)} gold a week, paid automatically; next payment in{" "}
              {formatDuration(Math.max(0, (new Date(mine.paidUntil ?? 0).getTime() - now) / 1000))}. Bring the
              goods home with a pipe through the wilderness, or put a Cargo Depot here and one on your estate.
            </p>
            <button className="danger" onClick={() => void gameClient.releaseOutpost(outpost.id)}>
              Give the land back
            </button>
          </>
        ) : (
          <p className="muted small">This land is claimed.</p>
        )
      ) : (
        <>
          <p className="small">
            From Company Level 15 you can claim an 8×8 outpost on a rich deposit out here (at most three),
            leased by the week. Rich deposits also appear during Discovery events.
          </p>
          {check === null ? (
            <p className="muted small">Surveying…</p>
          ) : check.error !== null ? (
            <p className="warning-text small">{check.error}</p>
          ) : (
            <>
              <p className="small">
                {check.richTiles} rich tiles
                {check.deposit !== null && ` of ${content.resourceName(check.deposit).toLowerCase()}`} in the
                claim.
              </p>
              <button
                className="primary wide"
                disabled={company.gold < check.lease}
                onClick={() => void gameClient.claimOutpost()}
              >
                Claim this outpost · {formatAmount(check.lease)} g a week
              </button>
            </>
          )}
        </>
      )}
    </aside>
  );
}
