import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { CompanyDto } from "../api/contracts";
import { DEEP_OFFSET, ESTATE_SIZE, PARCEL_SIZE, PARCELS_PER_SIDE, parcelOf } from "../game/layout";
import { currentGold, setState, useAppStore } from "../state/store";

/** Stone and ore the miners bring up per minute of digging (GameWorld.DigStonePerMinute, DigOrePerMinute) */
const STONE_PER_MINUTE = 30;
const ORE_PER_MINUTE = 10;

/** Whether a parcel may be dug: it holds a lift's shaft or lies next to a dug out parcel (GameWorld.CanDig) */
function canDig(company: CompanyDto, parcel: number): boolean {
  const world = gameClient.world;
  if (world !== null) {
    for (const building of world.buildings.values()) {
      if (building.type !== "liftShaft" || building.owner !== company.id) {
        continue;
      }
      for (let dy = 0; dy < building.size; dy++) {
        for (let dx = 0; dx < building.size; dx++) {
          const lx = building.x + dx - company.estateX;
          const ly = building.y + dy - DEEP_OFFSET - company.estateY;
          if (lx >= 0 && ly >= 0 && lx < ESTATE_SIZE && ly < ESTATE_SIZE && parcelOf(lx, ly) === parcel) {
            return true;
          }
        }
      }
    }
  }
  const x = parcel % PARCELS_PER_SIDE;
  const y = Math.floor(parcel / PARCELS_PER_SIDE);
  return [
    [x - 1, y],
    [x + 1, y],
    [x, y - 1],
    [x, y + 1],
  ].some(
    ([px, py]) =>
      px! >= 0 &&
      py! >= 0 &&
      px! < PARCELS_PER_SIDE &&
      py! < PARCELS_PER_SIDE &&
      (company.deep.parcels & (1 << (py! * PARCELS_PER_SIDE + px!))) !== 0,
  );
}

/** A parcel of the Deep Level still in the rock: dig it out (GDD §11.2) */
export function DeepParcelPanel() {
  const parcel = useAppStore((s) => s.selectedDeepParcel);
  const company = useAppStore((s) => s.company);
  const companyAt = useAppStore((s) => s.companyAt);
  const now = useNow();
  if (parcel === null || company === null) {
    return null;
  }
  const deep = company.deep;
  const gold = currentGold({ company, companyAt });
  const minutes = deep.nextHours * 60;
  const digging = deep.digParcel !== null && deep.digEndsAt !== null;
  const left = digging ? (new Date(deep.digEndsAt!).getTime() - now) / 1000 : 0;
  return (
    <aside className="panel parcel-panel deep-panel" aria-label="Deep Level parcel">
      <div className="panel-header">
        <h3>⛏ Deep parcel {parcel + 1}</h3>
        <button
          className="icon-button"
          title="Close (Esc)"
          onClick={() => setState({ selectedDeepParcel: null })}
        >
          ×
        </button>
      </div>
      <p className="muted">
        {PARCEL_SIZE}×{PARCEL_SIZE} tiles of solid rock under your estate. Dug out parcels aren't bought: the
        miners dig them out, and bring up stone and ore while they work.
      </p>
      {deep.digParcel === parcel ? (
        <>
          <p className="status building">Digging… {formatDuration(left)} to go</p>
          <div className="meter-bar">
            <div
              className="meter-fill"
              style={{
                width: `${Math.min(100, Math.max(0, 100 - (left / Math.max(1, (new Date(deep.digEndsAt!).getTime() - new Date(deep.digStartedAt!).getTime()) / 1000)) * 100))}%`,
              }}
            />
          </div>
        </>
      ) : digging ? (
        <p className="status idle">
          Your miners are digging parcel {deep.digParcel! + 1} ({formatDuration(left)} to go). One parcel at a
          time.
        </p>
      ) : !canDig(company, parcel) ? (
        <p className="status idle">Dig next to a lift or to a parcel that is dug out.</p>
      ) : (
        <>
          <ul className="facts">
            <li>⏱ {formatDuration(minutes * 60)} of digging</li>
            <li>
              ▲ brings up {formatAmount(minutes * STONE_PER_MINUTE)} stone and{" "}
              {formatAmount(minutes * ORE_PER_MINUTE)} ore into your storages
            </li>
            <li>Every next parcel takes longer: hours for the first, days for the last ones</li>
          </ul>
          <button
            className="primary wide"
            disabled={gold < deep.nextCost}
            onClick={() => void gameClient.dig(parcel)}
          >
            Dig it out · {formatAmount(deep.nextCost)} g
          </button>
        </>
      )}
    </aside>
  );
}

/** Down to the Deep Level and back up; it shows the dig going on */
export function LayerSwitch() {
  const company = useAppStore((s) => s.company);
  const deepView = useAppStore((s) => s.deepView);
  const now = useNow(10_000);
  if (company === null || !company.deep.open) {
    return null;
  }
  const digEndsAt = company.deep.digEndsAt;
  return (
    <button
      className={deepView ? "panel layer-switch deep" : "panel layer-switch"}
      title={deepView ? "Back up to the surface (G)" : "Down to the Deep Level (G)"}
      onClick={() => gameClient.toggleDeep()}
    >
      {deepView ? "☀ Surface" : "⛏ Deep Level"}
      {digEndsAt !== null && (
        <span className="small muted">
          {" "}
          · digging {formatDuration(Math.max(0, (new Date(digEndsAt).getTime() - now) / 1000))}
        </span>
      )}
    </button>
  );
}

/** The lantern-lit darkness around the screen down in the Deep Level */
export function DeepVignette() {
  const deepView = useAppStore((s) => s.deepView);
  return deepView ? <div className="deep-vignette" aria-hidden="true" /> : null;
}
