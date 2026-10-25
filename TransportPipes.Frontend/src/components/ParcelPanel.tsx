import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import {
  districtOriginX,
  districtOriginY,
  outskirtsParcels,
  PARCEL_SIZE,
  PARCELS_PER_SIDE,
} from "../game/layout";
import { ownEstates } from "../game/rules";
import { ResourceIcon } from "./ResourceIcon";
import { currentGold, setState, useAppStore } from "../state/store";

/** Buying a locked parcel of your own estate */
export function ParcelPanel() {
  const parcel = useAppStore((state) => state.selectedParcel);
  const estate = useAppStore((state) => state.selectedParcelEstate);
  const company = useAppStore((state) => state.company);
  const companyAt = useAppStore((state) => state.companyAt);
  const own = company === null ? undefined : ownEstates(company)[estate];
  if (parcel === null || company === null || own === undefined) {
    return null;
  }
  const buyable = gameClient.canBuyParcel(parcel, estate);
  const cost = own.nextParcelCost;
  const level = own.nextParcelLevel;
  const gold = currentGold({ company, companyAt });
  const levelOk = level !== null && company.level >= level;
  const goldOk = cost !== null && gold >= cost;

  return (
    <aside className="panel parcel-panel" aria-label="Parcel">
      <div className="panel-header">
        <h3>Parcel {parcel + 1}</h3>
        <button
          className="icon-button"
          title="Close (Esc)"
          onClick={() => setState({ selectedParcel: null })}
        >
          ×
        </button>
      </div>
      <p className="muted">
        8×8 tiles of your plot. You can already lay pipes over it; buy it to build on it.
      </p>
      <ParcelContents
        x={own.x + (parcel % PARCELS_PER_SIDE) * PARCEL_SIZE}
        y={own.y + Math.floor(parcel / PARCELS_PER_SIDE) * PARCEL_SIZE}
      />
      {!buyable ? (
        <p className="status idle">Buy a parcel next to it first: parcels are bought next to land you own.</p>
      ) : cost === null || level === null ? (
        <p className="status idle">You own every parcel.</p>
      ) : (
        <>
          <ul className="facts">
            <li className={levelOk ? "ok" : "missing"}>
              Company Level {level}
              {!levelOk && (
                <span className="muted"> (you're Level {company.level}: the top bar shows it)</span>
              )}
            </li>
            <li className={goldOk ? "ok" : "missing"}>{formatAmount(cost)} gold</li>
          </ul>
          <button
            className="primary wide"
            disabled={!levelOk || !goldOk}
            onClick={() => void gameClient.buyParcel(parcel, estate)}
          >
            Buy for {formatAmount(cost)} g
          </button>
          <p className="muted small">Every parcel costs twice as much as the one before.</p>
        </>
      )}
    </aside>
  );
}

/**
 * One of the nine outskirts parcels set aside for your plot (1.4 §5): from Era 2, once you researched Survey the
 * Outskirts, in any order; every one 40% dearer than the one before. On a move the market buys them back at half.
 */
export function OutskirtsPanel() {
  const index = useAppStore((state) => state.selectedOutskirts);
  const company = useAppStore((state) => state.company);
  const companyAt = useAppStore((state) => state.companyAt);
  if (index === null || company === null || company.outskirts === null) {
    return null;
  }
  const corner = outskirtsParcels(company.slot)[index];
  if (corner === undefined) {
    return null;
  }
  const { nextCost, blocker } = company.outskirts;
  const gold = currentGold({ company, companyAt });
  const owned = [...Array(9).keys()].filter((t) => (company.outskirts!.parcels & (1 << t)) !== 0).length;
  return (
    <aside className="panel parcel-panel" aria-label="Outskirts parcel">
      <div className="panel-header">
        <h3>Outskirts parcel</h3>
        <button
          className="icon-button"
          title="Close (Esc)"
          onClick={() => setState({ selectedOutskirts: null })}
        >
          ×
        </button>
      </div>
      <p className="muted">
        8×8 tiles beyond your plot, set aside for you: one of nine. Build and pipe on it like on your own
        land. If you move, the market buys it back for half its price.
      </p>
      <ParcelContents
        x={districtOriginX(company.districtX) + corner.x}
        y={districtOriginY(company.districtY) + corner.y}
      />
      {blocker !== null ? (
        <p className="status idle">{blocker}</p>
      ) : (
        <>
          <button
            className="primary wide"
            disabled={gold < nextCost}
            onClick={() => void gameClient.buyOutskirts(index)}
          >
            Buy for {formatAmount(nextCost)} g
          </button>
          <p className="muted small">You own {owned} of 9. Every one costs 40% more than the one before.</p>
        </>
      )}
    </aside>
  );
}

/** The goods land is dealt (WorldGenerator.LandGoods): on dealt land only a seat's own can be made on it */
const LAND_GOODS = ["copperOre", "coal", "sand", "wood", "stone"];

/** What a parcel holds: its deposits (with their best richness), water, forest, hills */
function ParcelContents({ x, y }: { x: number; y: number }) {
  const content = useAppStore((state) => state.content);
  // Dealt land: only your seat's goods can be made here (run 4: Cleo bought a "forest" parcel for a Lumber Camp her
  // land can't have); null on old land, where everything counts
  const goods = useAppStore((state) => state.company?.land?.goods ?? null);
  const world = gameClient.world;
  if (world === null || content === null) {
    return null;
  }
  const deposits = new Map<string, { tiles: number; richness: number }>();
  const terrain = new Map<string, number>();
  for (let dy = 0; dy < PARCEL_SIZE; dy++) {
    for (let dx = 0; dx < PARCEL_SIZE; dx++) {
      const tile = world.tile(x + dx, y + dy);
      if (tile === null) {
        continue;
      }
      terrain.set(tile.terrain, (terrain.get(tile.terrain) ?? 0) + 1);
      if (tile.deposit !== null) {
        const known = deposits.get(tile.deposit) ?? { tiles: 0, richness: 0 };
        deposits.set(tile.deposit, {
          tiles: known.tiles + 1,
          richness: Math.max(known.richness, tile.richness),
        });
      }
    }
  }
  const yours = (good: string) => goods === null || goods.includes(good);
  const land = [
    (terrain.get("water") ?? 0) > 0 && `water (a shore for pumps${yours("sand") ? " and dredgers" : ""})`,
    (terrain.get("forest") ?? 0) >= 4 &&
      (yours("wood") ? "forest" : "forest (trees only: wood isn't your land's good)"),
    (terrain.get("hills") ?? 0) >= 6 && "windy hills",
    (terrain.get("redHills") ?? 0) >= 6 && "red hills",
    (terrain.get("desert") ?? 0) >= 6 && "sunny desert",
  ].filter(Boolean);
  if (deposits.size === 0 && land.length === 0) {
    return <p className="small muted">Open grassland.</p>;
  }
  return (
    <div className="parcel-contents">
      <h4>In it</h4>
      <ul className="flow-list">
        {[...deposits.entries()].map(([resource, d]) => (
          <li key={resource}>
            <ResourceIcon resource={resource} />
            <span>{content.resourceName(resource)}</span>
            <span className="flow-amount">
              {LAND_GOODS.includes(resource) && !yours(resource)
                ? "not your land's good"
                : `${d.tiles} tiles · ${"★".repeat(d.richness)}`}
            </span>
          </li>
        ))}
      </ul>
      {land.length > 0 && <p className="small">Also: {land.join(", ")}</p>}
    </div>
  );
}
