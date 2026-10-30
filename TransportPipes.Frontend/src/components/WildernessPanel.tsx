import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import { useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

/** The wilderness (1.4 §20): an outpost, or claiming one on a rich deposit you have mapped */
export function WildernessPanel() {
  const tile = useAppStore((s) => s.wildTile);
  const check = useAppStore((s) => s.outpostCheck);
  const company = useAppStore((s) => s.company);
  const content = useAppStore((s) => s.content);
  if (tile === null || company === null || content === null) {
    return null;
  }
  const outpost = gameClient.world?.outpostAt(tile.x, tile.y);
  const info = gameClient.world?.tile(tile.x, tile.y);
  const own = outpost !== undefined && outpost.owner === company.id;
  const mine = own ? company.outposts.find((t) => t.id === outpost.id) : undefined;
  return (
    <aside className="panel parcel-panel wild-panel" aria-label="Wilderness">
      <div className="panel-header">
        <h3>{outpost !== undefined ? `⛺ Outpost of ${outpost.ownerName}` : "The wilderness"}</h3>
        <button className="icon-button" title="Close (Esc)" onClick={() => gameClient.closeWild()}>
          ×
        </button>
      </div>
      {info !== null && info !== undefined && info.deposit !== null && info.richness > 0 && (
        <p className="small">
          <ResourceIcon resource={info.deposit} /> {content.resourceName(info.deposit)} deposit{" "}
          {"★".repeat(info.richness)}
        </p>
      )}
      {outpost !== undefined ? (
        own && mine !== undefined ? (
          <>
            <p className="small">
              Yours for good (bought for {formatAmount(mine.price ?? 0)} g). Bring its goods home with a Cargo
              Depot here and one on your plot, or a Quantum Link.
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
            From Era 4, with the Outposts research, you can claim an 8×8 outpost on a rich deposit in the
            wilderness your expeditions mapped (at most three), bought once and yours for good. Rich deposits
            also appear during Discovery events.
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
                disabled={company.gold < check.price}
                onClick={() => void gameClient.claimOutpost()}
              >
                Claim this outpost · {formatAmount(check.price)} g
              </button>
            </>
          )}
        </>
      )}
    </aside>
  );
}
