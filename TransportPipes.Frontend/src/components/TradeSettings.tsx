import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import { useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

const TRADE_WITH: Array<{ id: "everyone" | "corporation" | "contracts"; name: string; text: string }> = [
  { id: "everyone", name: "Everyone", text: "everyone in your station (unless you block someone below)" },
  { id: "corporation", name: "My corporation", text: "only members of your corporation" },
  { id: "contracts", name: "Contracts only", text: "nobody, except what you agreed in a contract" },
];

/** The Company Level from which a corporation can be founded or joined (GameWorld.CorporationFoundLevel) */
const CORPORATION_LEVEL = 8;

/**
 * Who you trade with through your Yard (everyone, your corporation or contract partners), who in your station you
 * block, and per resource whether you sell it to your neighbours and buy it from them. Everybody pays one price.
 */
export function TradeSettings() {
  const market = useAppStore((s) => s.market);
  const company = useAppStore((s) => s.company);
  const content = useAppStore((s) => s.content);
  const neighbours = useAppStore((s) => s.neighbours);
  if (company === null || content === null) {
    return null;
  }
  const world = gameClient.world;
  // What the company makes or uses, and what it set something for
  const mine = new Set<string>();
  for (const building of world?.buildings.values() ?? []) {
    if (building.owner === company.id) {
      const definition = content.building(building.type);
      Object.keys(definition?.out ?? {}).forEach((t) => mine.add(t));
      Object.keys(definition?.in ?? {}).forEach((t) => mine.add(t));
    }
  }
  const rows = (market?.resources ?? []).filter((t) => mine.has(t.resource) || t.noTrade || t.noBuy);
  const inCorporation = company.corporationId !== null;
  const residents = neighbours.filter((t) => t.id !== company.id);

  return (
    <div className="trade-settings">
      <p className="trade-intro">
        Your neighbours in the station trade with you through the Yard: lay a pipe from your network to your
        Yard gate, and what your buildings don&apos;t use goes to the neighbours who need it, while your
        factories buy what they lack. Everybody pays the same price, always more than the hub pays you.
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
        <h4>Your station</h4>
        {residents.length === 0 ? (
          <p className="muted small">Nobody else lives in your station yet.</p>
        ) : (
          <ul className="partner-list">
            {residents.map((n) => (
              <li key={n.id} className={n.blocked ? "partner blocked" : "partner"}>
                <span className="company-dot" style={{ background: n.color }} />
                <span className="partner-name">
                  <strong>{n.name}</strong>
                  {n.isNpc && <span className="tag">⚙ House</span>}
                  <span className="muted small">
                    {n.connected ? "piped to the Yard" : "not piped to the Yard yet"}
                    {n.youEarn > 0.05 && ` · you earn ${formatAmount(n.youEarn)} g/min`}
                    {n.youPay > 0.05 && ` · you pay ${formatAmount(n.youPay)} g/min`}
                  </span>
                </span>
                <label
                  className="switch"
                  title={n.blocked ? "Trade with them again" : "Block: don't trade with them"}
                >
                  <input
                    type="checkbox"
                    checked={!n.blocked}
                    onChange={() => void gameClient.setTradeBlocked(n.id, !n.blocked)}
                  />
                  <span className="switch-track" />
                </label>
              </li>
            ))}
          </ul>
        )}
        <p className="muted small">
          Blocking is private: they only read that you trade your spare with others.
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
                <th title="What the hub pays you for it">Hub pays you</th>
                <th title="The one price between players">Neighbours pay</th>
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
                  <td>{row.playerPrice.toFixed(2)} g</td>
                  <td>
                    <label className="switch">
                      <input
                        type="checkbox"
                        checked={!row.noTrade}
                        onChange={() => void gameClient.setNoTrade(row.resource, !row.noTrade)}
                      />
                      <span className="switch-track" />
                    </label>
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
