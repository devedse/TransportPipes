import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { GameEventDto, MarketResourceDto } from "../api/contracts";
import { setState, useAppStore, type HubTab } from "../state/store";
import { WondersTab } from "./WondersTab";
import { EVENT_ICONS, eventTime } from "../app/events";
import { ResourceIcon } from "./ResourceIcon";

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

export function PriceInput({
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
/** The event calendar of the district (GDD §15) */
function CalendarTab({ serverNow }: { serverNow: number }) {
  const company = useAppStore((state) => state.company)!;
  const days = new Map<string, GameEventDto[]>();
  for (const event of company.events) {
    const day = new Date(Math.max(new Date(event.startsAt).getTime(), serverNow)).toLocaleDateString([], {
      weekday: "long",
      day: "numeric",
      month: "short",
    });
    days.set(day, [...(days.get(day) ?? []), event]);
  }
  return (
    <div className="calendar">
      <p className="muted small">
        What is coming to your district in the next {company.forecastHours} hours.
        {company.forecastHours < 72 && " A Weather Station sees three days ahead and halves storm damage."}
      </p>
      {company.events.length === 0 && <p className="muted">A quiet day: nothing planned.</p>}
      {[...days.entries()].map(([day, events]) => (
        <div key={day} className="calendar-day">
          <div className="panel-title">{day}</div>
          {events.map((event) => (
            <div
              key={event.id}
              className={
                event.active ? `calendar-event active ${event.kind}` : `calendar-event ${event.kind}`
              }
            >
              <span className="event-icon">{EVENT_ICONS[event.kind]}</span>
              <div>
                <div>
                  <strong>{event.name}</strong>{" "}
                  <span className="muted small">
                    {new Date(event.startsAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}–
                    {new Date(event.endsAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}
                    {event.everywhere && " · everywhere"}
                  </span>
                </div>
                <div className="small">{event.effect}</div>
              </div>
              <span className={event.active ? "event-when now" : "event-when"}>
                {eventTime(event, serverNow)}
              </span>
            </div>
          ))}
        </div>
      ))}
    </div>
  );
}

export function HubPanel() {
  const open = useAppStore((state) => state.hubOpen);
  const market = useAppStore((state) => state.market);
  const company = useAppStore((state) => state.company);
  const content = useAppStore((state) => state.content);
  const serverOffset = useAppStore((state) => state.serverOffsetMs);
  const now = useNow();
  const tab = useAppStore((state) => state.hubTab);
  const setTab = (hubTab: HubTab) => setState({ hubTab });
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
          <button className={tab === "calendar" ? "tab active" : "tab"} onClick={() => setTab("calendar")}>
            Calendar
            {company.events.some((t) => t.active) && (
              <span className="badge">{company.events.filter((t) => t.active).length}</span>
            )}
          </button>
          <button className={tab === "wonders" ? "tab active" : "tab"} onClick={() => setTab("wonders")}>
            Wonders
          </button>
          <button className={tab === "exchange" ? "tab active" : "tab"} onClick={() => setTab("exchange")}>
            Exchange
          </button>
        </div>

        {tab === "wonders" ? (
          <WondersTab serverNow={serverNow} />
        ) : tab === "calendar" ? (
          <CalendarTab serverNow={serverNow} />
        ) : tab === "exchange" ? (
          <ExchangeTab serverNow={serverNow} />
        ) : tab === "orders" ? (
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
                <div key={order.id} className={done ? "order done" : order.urgent ? "order urgent" : "order"}>
                  <div className="order-icon" style={{ borderColor: resource?.color }}>
                    <ResourceIcon resource={order.resource} />
                  </div>
                  <div className="order-body">
                    <div className="order-title">
                      {order.urgent && <span className="urgent-tag">📢 Shortage</span>}
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
                      {row.heading !== null && Math.abs(row.heading - row.buyPrice) > 0.005 && (
                        <span
                          className={row.heading > row.buyPrice ? "trend up" : "trend down"}
                          title="Market Insight: where the hub price is heading"
                        >
                          {" "}
                          → {row.heading.toFixed(2)}
                        </span>
                      )}
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

function ExchangeTab({ serverNow }: { serverNow: number }) {
  const exchange = useAppStore((state) => state.exchange);
  const content = useAppStore((state) => state.content)!;
  const [selling, setSelling] = useState(true);
  const [resource, setResource] = useState("ironOre");
  const [amount, setAmount] = useState(100);
  const [price, setPrice] = useState(4);
  if (exchange === null) {
    return <p className="muted">Loading…</p>;
  }
  return (
    <div className="exchange">
      <p className="muted small">
        Trade with anyone {exchange.global ? "at every hub (Global Exchange)" : "in the district"} without a
        pipe. Sell orders hold their goods from your storages, buy orders hold their gold. Goods go by cargo
        (faster with a Cargo Depot and Trains); the seller pays a {Math.round(exchange.fee * 100)}% fee that
        disappears.
      </p>
      {!exchange.hasOffice ? (
        <p className="status idle">Build an Exchange Office (Company Level 12) to place orders.</p>
      ) : (
        <form
          className="form-row exchange-form"
          onSubmit={(event) => {
            event.preventDefault();
            void gameClient.placeExchangeOrder(resource, selling, amount, price);
          }}
        >
          <label>
            I want to
            <select value={selling ? "sell" : "buy"} onChange={(e) => setSelling(e.target.value === "sell")}>
              <option value="sell">sell</option>
              <option value="buy">buy</option>
            </select>
          </label>
          <label>
            Resource
            <select
              value={resource}
              onChange={(e) => {
                setResource(e.target.value);
                setPrice(Math.round((content.resource(e.target.value)?.price ?? 1) * 150) / 100);
              }}
            >
              {content.resources
                .filter((r) => r.era <= 2)
                .map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.name}
                  </option>
                ))}
            </select>
          </label>
          <label>
            Amount
            <input type="number" min={1} value={amount} onChange={(e) => setAmount(Number(e.target.value))} />
          </label>
          <label>
            Price (g)
            <input
              type="number"
              min={0}
              step={0.01}
              value={price}
              onChange={(e) => setPrice(Number(e.target.value))}
            />
          </label>
          <button className="primary" type="submit">
            Place order
          </button>
        </form>
      )}
      <table className="market-table">
        <thead>
          <tr>
            <th>Resource</th>
            <th>Best buy offer</th>
            <th>Best sell offer</th>
          </tr>
        </thead>
        <tbody>
          {exchange.books.length === 0 && (
            <tr>
              <td colSpan={3} className="muted">
                No orders yet.
              </td>
            </tr>
          )}
          {exchange.books.map((book) => (
            <tr key={book.resource}>
              <td>
                <ResourceIcon resource={book.resource} /> {content.resourceName(book.resource)}
              </td>
              <td>
                {book.bidAmount > 0 ? `${formatAmount(book.bidAmount)} @ ${book.bestBid.toFixed(2)}` : "—"}
              </td>
              <td>
                {book.askAmount > 0 ? `${formatAmount(book.askAmount)} @ ${book.bestAsk.toFixed(2)}` : "—"}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {exchange.orders.length > 0 && (
        <>
          <h4>Your orders</h4>
          {exchange.orders.map((order) => (
            <div key={order.id} className="contract">
              <span>
                {order.selling ? "Selling" : "Buying"} {formatAmount(order.amount)}{" "}
                {content.resourceName(order.resource).toLowerCase()} @ {order.price.toFixed(2)} g
              </span>
              <button onClick={() => void gameClient.cancelExchangeOrder(order.id)}>Cancel</button>
            </div>
          ))}
        </>
      )}
      {exchange.shipments.length > 0 && (
        <>
          <h4>Cargo on its way</h4>
          {exchange.shipments.map((shipment) => (
            <div key={shipment.id} className="small">
              🚚 {formatAmount(shipment.amount)} {content.resourceName(shipment.resource).toLowerCase()}{" "}
              {shipment.incoming ? `from ${shipment.partner}` : `to ${shipment.partner}`} · arrives in{" "}
              {formatDuration(Math.max(0, (new Date(shipment.arrivesAt).getTime() - serverNow) / 1000))}
            </div>
          ))}
        </>
      )}
    </div>
  );
}
