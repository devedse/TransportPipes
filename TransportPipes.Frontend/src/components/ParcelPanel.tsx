import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import { currentGold, setState, useAppStore } from "../state/store";

/** Buying a locked parcel of your own estate */
export function ParcelPanel() {
  const parcel = useAppStore((state) => state.selectedParcel);
  const company = useAppStore((state) => state.company);
  const companyAt = useAppStore((state) => state.companyAt);
  if (parcel === null || company === null) {
    return null;
  }
  const buyable = gameClient.canBuyParcel(parcel);
  const cost = company.nextParcelCost;
  const level = company.nextParcelLevel;
  const gold = currentGold({ company, companyAt });
  const levelOk = level !== null && company.level >= level;
  const goldOk = cost !== null && gold >= cost;

  return (
    <aside className="panel parcel-panel">
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
        8×8 tiles of your estate. You can already lay pipes over it; buy it to build on it.
      </p>
      {!buyable ? (
        <p className="status idle">Buy a parcel next to it first: parcels are bought next to land you own.</p>
      ) : cost === null || level === null ? (
        <p className="status idle">You own every parcel.</p>
      ) : (
        <>
          <ul className="facts">
            <li className={levelOk ? "ok" : "missing"}>Company Level {level}</li>
            <li className={goldOk ? "ok" : "missing"}>{formatAmount(cost)} gold</li>
          </ul>
          <button
            className="primary wide"
            disabled={!levelOk || !goldOk}
            onClick={() => void gameClient.buyParcel(parcel)}
          >
            Buy for {formatAmount(cost)} g
          </button>
          <p className="muted small">Every parcel costs twice as much as the one before.</p>
        </>
      )}
    </aside>
  );
}
