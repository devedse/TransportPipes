import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { MarketResourceDto } from "../api/contracts";
import { setState, useAppStore } from "../state/store";

function Trend({ row }: { row: MarketResourceDto }) {
  const ratio = row.buyPrice / row.basePrice;
  if (ratio < 0.98) {
    return (
      <span className="trend down" title="Players sell more than the hub wants">
        ▼ {Math.round((1 - ratio) * 100)}%
      </span>
    );
  }
  if (ratio > 1.02) {
    return (
      <span className="trend up" title="The hub wants more than players sell">
        ▲ {Math.round((ratio - 1) * 100)}%
      </span>
    );
  }
  return <span className="trend">=</span>;
}

function PriceInput({
  value,
  low,
  high,
  onSet,
}: {
  value: number;
  low: number;
  high: number;
  onSet: (price: number) => void;
}) {
  const [draft, setDraft] = useState<string | null>(null);
  const commit = () => {
    if (draft !== null) {
      const parsed = Number(draft.replace(",", "."));
      if (Number.isFinite(parsed)) {
        onSet(parsed);
      }
      setDraft(null);
    }
  };
  return (
    <input
      className="price-input"
      type="number"
      step={0.01}
      min={low}
      max={high}
      value={draft ?? value.toFixed(2)}
      onChange={(event) => setDraft(event.target.value)}
      onBlur={commit}
      onKeyDown={(event) => event.key === "Enter" && commit()}
    />
  );
}

/** The hub of your district: today's orders, your reputation and the market with your prices for other players */
export function HubPanel() {
  const open = useAppStore((state) => state.hubOpen);
  const market = useAppStore((state) => state.market);
  const company = useAppStore((state) => state.company);
  const content = useAppStore((state) => state.content);
  const serverOffset = useAppStore((state) => state.serverOffsetMs);
  const now = useNow();
  const [tab, setTab] = useState<"orders" | "market">("orders");
  if (!open || company === null || content === null) {
    return null;
  }
  const close = () => setState({ hubOpen: false });
  const serverNow = now + serverOffset;
  const reputationSpan = company.nextReputation === null ? 1 : company.nextReputation;
  const reputationProgress =
    company.nextReputation === null ? 1 : Math.min(1, company.hubReputation / reputationSpan);

  return (
    <div className="modal-backdrop" onClick={close}>
      <div
        className="panel hub-panel"
        role="dialog"
        aria-label="Hub"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="panel-header">
          <h2>{market?.district ?? "Hub"} market</h2>
          <div className="reputation" title={`${formatAmount(company.hubReputation)} reputation points`}>
            <span>
              Hub reputation <strong>{company.reputationLevel}</strong>/10
            </span>
            <div className="xp-bar">
              <div className="xp-fill gold" style={{ width: `${reputationProgress * 100}%` }} />
            </div>
            <span className="muted small">+{company.reputationLevel - 1}% on hub sales</span>
          </div>
          <button className="icon-button" title="Close (Esc)" onClick={close}>
            ×
          </button>
        </div>
        <div className="tabs">
          <button className={tab === "orders" ? "tab active" : "tab"} onClick={() => setTab("orders")}>
            Daily orders
          </button>
          <button className={tab === "market" ? "tab active" : "tab"} onClick={() => setTab("market")}>
            Prices
          </button>
        </div>

        {tab === "orders" ? (
          <div className="orders">
            <p className="muted small">
              The hub wants these today. Deliver them from your storages for a reward, reputation and XP. New
              orders at midnight.
            </p>
            {company.dailyOrders.map((order) => {
              const done = order.delivered >= order.amount - 1e-9;
              const left = (new Date(order.expiresAt).getTime() - serverNow) / 1000;
              const resource = content.resource(order.resource);
              return (
                <div key={order.id} className={done ? "order done" : "order"}>
                  <div className="order-icon" style={{ background: resource?.color }}>
                    {resource?.icon}
                  </div>
                  <div className="order-body">
                    <div className="order-title">
                      {formatAmount(order.amount)} {content.resourceName(order.resource).toLowerCase()}
                    </div>
                    <div className="meter-bar">
                      <div
                        className="meter-fill"
                        style={{
                          width: `${Math.min(1, order.delivered / order.amount) * 100}%`,
                          background: resource?.color,
                        }}
                      />
                    </div>
                    <div className="small muted">
                      Reward <span className="gold-text">{formatAmount(order.rewardGold)} g</span> ·{" "}
                      {formatAmount(order.rewardXp)} XP · {formatAmount(order.rewardReputation)} reputation
                      {!done && ` · ${formatDuration(Math.max(0, left))} left`}
                    </div>
                  </div>
                  {done ? (
                    <span className="order-check">✔</span>
                  ) : (
                    <button className="primary" onClick={() => void gameClient.deliverOrder(order.id)}>
                      Deliver
                    </button>
                  )}
                </div>
              );
            })}
          </div>
        ) : market === null ? (
          <p className="muted">Loading…</p>
        ) : (
          <div className="market">
            <p className="muted small">
              The hub buys everything, but pays less when players sell more than it wants. Your price is what
              neighbours pay you, within the fair band; changes wait an hour when a partner buys from you.
            </p>
            <table className="market-table">
              <thead>
                <tr>
                  <th>Resource</th>
                  <th>Hub buys</th>
                  <th>Hub sells</th>
                  <th>Sold / wanted</th>
                  <th>Your price</th>
                  <th>You pay at most</th>
                  <th>Trade</th>
                </tr>
              </thead>
              <tbody>
                {market.resources.map((row) => (
                  <tr key={row.resource}>
                    <td>
                      <span
                        className="flow-dot"
                        style={{ background: content.resourceColor(row.resource) }}
                      />{" "}
                      {content.resourceName(row.resource)}
                    </td>
                    <td>
                      {row.buyPrice.toFixed(2)} <Trend row={row} />
                    </td>
                    <td>{row.sellPrice.toFixed(2)}</td>
                    <td className="muted">
                      {formatAmount(row.soldPerMinute)} / {formatAmount(row.demand)}
                    </td>
                    <td>
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
                          className="link"
                          onClick={() => void gameClient.setSellPrice(row.resource, null)}
                        >
                          auto
                        </button>
                      )}
                      {row.pendingAt !== null && (
                        <div className="small warning-text">
                          → {row.pendingPrice?.toFixed(2) ?? "auto"} in{" "}
                          {formatDuration(
                            Math.max(0, (new Date(row.pendingAt).getTime() - serverNow) / 1000),
                          )}
                        </div>
                      )}
                    </td>
                    <td>
                      <PriceInput
                        value={row.yourMaxPrice}
                        low={row.bandLow}
                        high={row.bandHigh}
                        onSet={(price) => void gameClient.setBuyMaxPrice(row.resource, price)}
                      />
                    </td>
                    <td>
                      <label className="check" title="Sell this resource to other players">
                        <input
                          type="checkbox"
                          checked={!row.noTrade}
                          onChange={() => void gameClient.setNoTrade(row.resource, !row.noTrade)}
                        />
                        sell
                      </label>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
