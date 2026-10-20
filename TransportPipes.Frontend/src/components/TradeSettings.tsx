import { gameClient } from "../app/client";
import type { GatePolicy, GateSide } from "../api/contracts";
import { useAppStore } from "../state/store";
import { PriceInput } from "./HubPanel";
import { ResourceIcon } from "./ResourceIcon";

const SIDES: Array<{ side: GateSide; name: string }> = [
  { side: "top", name: "North" },
  { side: "right", name: "East" },
  { side: "bottom", name: "South" },
  { side: "left", name: "West" },
];

const POLICIES: Array<{ id: GatePolicy; name: string }> = [
  { id: "open", name: "Open: anyone" },
  { id: "contract", name: "Contracts only" },
  { id: "corporation", name: "Corporation only" },
  { id: "closed", name: "Closed" },
];

/** The Company Level from which a corporation can be founded or joined (GameWorld.CorporationFoundLevel) */
const CORPORATION_LEVEL = 8;

/**
 * Who you trade with and how: your gates (who is on your pipes) and per resource whether you sell it to neighbours,
 * at what price, and whether you buy it from them, up to what price
 */
export function TradeSettings() {
  const market = useAppStore((s) => s.market);
  const company = useAppStore((s) => s.company);
  const content = useAppStore((s) => s.content);
  // Renders again when the district (with the gate policies) changes
  useAppStore((s) => s.districtVersion);
  if (company === null || content === null) {
    return null;
  }
  const world = gameClient.world;
  const estate = world
    ?.district(company.districtX, company.districtY)
    ?.estates.find((t) => t.slot === company.slot);
  // What the company makes or uses, and what it set something for
  const mine = new Set<string>();
  for (const building of world?.buildings.values() ?? []) {
    if (building.owner === company.id) {
      const definition = content.building(building.type);
      Object.keys(definition?.out ?? {}).forEach((t) => mine.add(t));
      Object.keys(definition?.in ?? {}).forEach((t) => mine.add(t));
    }
  }
  const rows = (market?.resources ?? []).filter(
    (t) => mine.has(t.resource) || t.noTrade || t.noBuy || !t.yourPriceIsAuto,
  );
  const inCorporation = company.corporationId !== null;

  return (
    <div className="trade-settings">
      <p className="trade-intro">
        Neighbours on your pipes buy what your buildings don't use, and sell you what they lack, at the
        seller&apos;s price. A trade only happens when it pays you more than the hub: a long pipe loses so
        much on the way that the hub pays more.
      </p>

      <section>
        <h4>Your gates · who is on your pipes</h4>
        <div className="gate-grid">
          {SIDES.map(({ side, name }) => {
            const policy = estate?.gatePolicies[side] ?? "open";
            return (
              <label key={side} className="gate-row">
                <span>{name}</span>
                <select
                  value={policy}
                  onChange={(event) => void gameClient.setGatePolicy(side, event.target.value as GatePolicy)}
                >
                  {POLICIES.map((p) => (
                    <option key={p.id} value={p.id} disabled={p.id === "corporation" && !inCorporation}>
                      {p.name}
                    </option>
                  ))}
                </select>
              </label>
            );
          })}
        </div>
        <p className="muted small">
          {inCorporation
            ? "Corporation only: only members of your corporation trade through that gate."
            : `Corporation only needs a corporation: found one or join one in the Corp panel from Company Level ${CORPORATION_LEVEL} (you're Level ${company.level}).`}{" "}
          Contracts only: just the companies you have a contract with.
        </p>
      </section>

      <section>
        <h4>Per resource</h4>
        {market === null ? (
          <p className="muted">Loading…</p>
        ) : rows.length === 0 ? (
          <p className="muted">Build something first: what your buildings make and use shows up here.</p>
        ) : (
          <table className="trade-table">
            <thead>
              <tr>
                <th />
                <th title="What your trading post gets for it">Hub pays</th>
                <th>Sell to neighbours</th>
                <th>Buy from neighbours</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.resource}>
                  <td>
                    <ResourceIcon resource={row.resource} /> {content.resourceName(row.resource)}
                  </td>
                  <td>{row.buyPrice.toFixed(2)} g</td>
                  <td>
                    <label className="switch">
                      <input
                        type="checkbox"
                        checked={!row.noTrade}
                        onChange={() => void gameClient.setNoTrade(row.resource, !row.noTrade)}
                      />
                      <span className="switch-track" />
                    </label>
                    {!row.noTrade && (
                      <span className="trade-price">
                        at{" "}
                        <PriceInput
                          value={row.yourPrice}
                          low={row.bandLow}
                          high={row.bandHigh}
                          onSet={(price) => void gameClient.setSellPrice(row.resource, price)}
                        />
                        {row.yourPriceIsAuto ? (
                          <span className="muted small"> auto</span>
                        ) : (
                          <button
                            className="link small"
                            onClick={() => void gameClient.setSellPrice(row.resource, null)}
                          >
                            auto
                          </button>
                        )}
                      </span>
                    )}
                  </td>
                  <td>
                    <label className="switch">
                      <input
                        type="checkbox"
                        checked={!row.noBuy}
                        onChange={() => void gameClient.setNoBuy(row.resource, !row.noBuy)}
                      />
                      <span className="switch-track" />
                    </label>
                    {!row.noBuy && (
                      <span className="trade-price">
                        up to{" "}
                        <PriceInput
                          value={row.yourMaxPrice}
                          low={row.bandLow}
                          high={row.bandHigh}
                          onSet={(price) => void gameClient.setBuyMaxPrice(row.resource, price)}
                        />
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </div>
  );
}
