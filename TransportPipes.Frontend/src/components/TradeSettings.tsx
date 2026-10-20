import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import type { GateSide } from "../api/contracts";
import { useAppStore } from "../state/store";
import { PriceInput } from "./HubPanel";
import { ResourceIcon } from "./ResourceIcon";

const SIDES: Array<{ side: GateSide; name: string }> = [
  { side: "top", name: "North" },
  { side: "right", name: "East" },
  { side: "bottom", name: "South" },
  { side: "left", name: "West" },
];

/** Below this share arriving, a neighbour pays less than the hub at the usual prices (1.75× the hub's price) */
const WORTH_IT = 1 / 1.75;

const TRADE_WITH: Array<{ id: "everyone" | "corporation" | "contracts"; name: string; text: string }> = [
  { id: "everyone", name: "Everyone", text: "every company on your pipes (unless you switch it off below)" },
  { id: "corporation", name: "My corporation", text: "only members of your corporation" },
  { id: "contracts", name: "Contracts only", text: "nobody, except what you agreed in a contract" },
];

/** The Company Level from which a corporation can be founded or joined (GameWorld.CorporationFoundLevel) */
const CORPORATION_LEVEL = 8;

/**
 * Who you trade with and how: everyone, your corporation or contract partners; each neighbour on your pipes with how
 * long the pipe to it is and how much arrives (switch one off); your gates (open or closed); and per resource
 * whether you sell it to neighbours, at what price, and whether you buy it, up to what price
 */
export function TradeSettings() {
  const market = useAppStore((s) => s.market);
  const company = useAppStore((s) => s.company);
  const content = useAppStore((s) => s.content);
  const neighbours = useAppStore((s) => s.neighbours);
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
  const partners = neighbours
    .filter((t) => t.connected && t.id !== company.id)
    .sort((a, b) => (b.linkEfficiency ?? 0) - (a.linkEfficiency ?? 0));

  return (
    <div className="trade-settings">
      <p className="trade-intro">
        Neighbours on your pipes buy what your buildings don't use, and sell you what they lack, at the
        seller&apos;s price. A trade only happens when it pays you more than the hub: a long pipe loses so
        much on the way that the hub pays more.
      </p>

      <section>
        <h4>Who you trade with</h4>
        <div className="segmented">
          {TRADE_WITH.map((option) => (
            <button
              key={option.id}
              className={company.tradeWith === option.id ? "active" : ""}
              disabled={option.id === "corporation" && !inCorporation}
              title={
                option.id === "corporation" && !inCorporation
                  ? `Needs a corporation: from Company Level ${CORPORATION_LEVEL}, in the Corp panel (you're Level ${company.level})`
                  : option.text
              }
              onClick={() => void gameClient.setTradeWith(option.id)}
            >
              {option.name}
            </button>
          ))}
        </div>
        <p className="muted small">
          You trade with {TRADE_WITH.find((t) => t.id === company.tradeWith)?.text}. Contracts always go
          through.
          {!inCorporation &&
            ` Corporations come at Company Level ${CORPORATION_LEVEL} (you're Level ${company.level}): found or join one in the Corp panel.`}
        </p>
      </section>

      <section>
        <h4>Your neighbours on your pipes</h4>
        {partners.length === 0 ? (
          <p className="muted small">
            Nobody is on your pipes yet. Lay a pipe to a neighbour&apos;s gate: Neighbours → Connect a pipe.
          </p>
        ) : (
          <ul className="partner-list">
            {partners.map((n) => {
              const share = n.linkEfficiency ?? 1;
              const quality = share >= 0.8 ? "good" : share >= WORTH_IT ? "ok" : "far";
              return (
                <li key={n.id} className={n.blocked ? "partner blocked" : "partner"}>
                  <span className="company-dot" style={{ background: n.color }} />
                  <span className="partner-name">
                    <strong>{n.name}</strong>
                    {n.isNpc && <span className="tag">tenant</span>}
                    <span className="muted small">
                      {n.pipeTiles !== null ? `${n.pipeTiles} pipe tiles · ` : ""}
                      {Math.round(share * 100)}% arrives
                      {n.youEarn > 0.05 && ` · you earn ${formatAmount(n.youEarn)} g/min`}
                      {n.youPay > 0.05 && ` · you pay ${formatAmount(n.youPay)} g/min`}
                    </span>
                  </span>
                  <span className={`partner-quality ${quality}`}>
                    {quality === "good" ? "close" : quality === "ok" ? "fair" : "too far: the hub pays more"}
                  </span>
                  <label className="switch" title={n.blocked ? "Trade with them" : "Don't trade with them"}>
                    <input
                      type="checkbox"
                      checked={!n.blocked}
                      onChange={() => void gameClient.setTradeBlocked(n.id, !n.blocked)}
                    />
                    <span className="switch-track" />
                  </label>
                </li>
              );
            })}
          </ul>
        )}
        <p className="muted small">
          Neighbours pay for what arrives: a pipe loses a little on every tile (a grey one 0.3%), so over a very
          long one your trading post earns more. Better pipes lose less.
        </p>
      </section>

      <section>
        <h4>Your gates</h4>
        <div className="gate-grid">
          {SIDES.map(({ side, name }) => {
            const policy = estate?.gatePolicies[side] ?? "open";
            const open = policy !== "closed";
            return (
              <label key={side} className="gate-row">
                <span>{name}</span>
                <span className={open ? "gate-state open" : "gate-state"}>{open ? "open" : "closed"}</span>
                <span className="switch">
                  <input
                    type="checkbox"
                    checked={open}
                    onChange={() => void gameClient.setGatePolicy(side, open ? "closed" : "open")}
                  />
                  <span className="switch-track" />
                </span>
              </label>
            );
          })}
        </div>
        <p className="muted small">
          A closed gate cuts your pipes off on that side. Who you trade with is chosen above.
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
