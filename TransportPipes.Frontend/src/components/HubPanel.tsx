import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { GameEventDto, MarketResourceDto } from "../api/contracts";
import { setState, useAppStore, type HubTab } from "../state/store";
import { TownTab } from "./TownTab";
import { EVENT_ICONS, eventTime } from "../app/events";
import { ResourceIcon } from "./ResourceIcon";
import { Term } from "./Term";

function Trend({ row }: { row: MarketResourceDto }) {
  const ratio = row.buyPrice / row.basePrice;
  if (ratio < 0.98) {
    return (
      <span className="trend down" title="You sell more than your Hub Order">
        ▼ {Math.round((1 - ratio) * 100)}%
      </span>
    );
  }
  if (ratio > 1.02) {
    return (
      <span className="trend up" title="Above its base price">
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

/** Your town's market: today's orders, the prices with your Hub Orders, the calendar, the town and its Wonder, the Exchange */
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
        What is coming to your town in the next {company.forecastHours} hours.
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

  return (
    <div className="modal-backdrop" onClick={close}>
      <div
        className="panel hub-panel"
        role="dialog"
        aria-label="Market"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="panel-header">
          <h2>
            {market?.district ?? "Town"} · Era {company.era}
          </h2>
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
            Town &amp; Wonder
          </button>
          <button className={tab === "exchange" ? "tab active" : "tab"} onClick={() => setTab("exchange")}>
            Exchange
          </button>
        </div>

        {tab === "wonders" ? (
          <TownTab serverNow={serverNow} />
        ) : tab === "calendar" ? (
          <CalendarTab serverNow={serverNow} />
        ) : tab === "exchange" ? (
          <ExchangeTab serverNow={serverNow} />
        ) : tab === "orders" ? (
          <div className="orders">
            <p className="muted small">
              Today the hub pays double for these goods: up to three, one for each good you sell, and a good
              you start selling today gets one too. They fill by themselves as you sell, or deliver what your
              Storages hold. New orders at{" "}
              {new Date(new Date(company.serverTime).setUTCHours(24, 0, 0, 0)).toLocaleTimeString([], {
                hour: "2-digit",
                minute: "2-digit",
              })}
              .
            </p>
            {company.dailyOrders.map((order) => {
              const done = order.delivered >= order.amount - 1e-9;
              const left = (new Date(order.expiresAt).getTime() - serverNow) / 1000;
              const resource = content.resource(order.resource);
              const good = content.resourceName(order.resource).toLowerCase();
              // Orders of one good fill one after another, the shortage first (after run 4: two iron ore orders
              // each counted Cleo's same 41 a minute)
              const ahead = company.dailyOrders.filter(
                (t) =>
                  t.resource === order.resource &&
                  t.id !== order.id &&
                  t.delivered < t.amount - 1e-9 &&
                  new Date(t.expiresAt).getTime() > serverNow &&
                  (t.urgent !== order.urgent ? t.urgent : t.id < order.id),
              );
              const before = ahead.reduce((sum, t) => sum + t.amount - t.delivered, 0);
              // Twice the base for every unit, the hub's price as the goods pass and the rest from the order
              const unit = order.rewardGold / order.amount;
              const bonus = order.rewardGold - order.amount * order.hubPrice;
              const deliver = done || left <= 0 ? 0 : Math.min(order.stored, order.amount - order.delivered);
              const eta =
                order.selling > 1e-6
                  ? ((before + order.amount - order.delivered) / order.selling) * 60
                  : null;
              return (
                <div key={order.id} className={done ? "order done" : order.urgent ? "order urgent" : "order"}>
                  <div className="order-icon" style={{ borderColor: resource?.color }}>
                    <ResourceIcon resource={order.resource} />
                  </div>
                  <div className="order-body">
                    <div className="order-title">
                      {order.urgent && <span className="urgent-tag">📢 Shortage</span>}
                      {formatAmount(order.amount)} {good}
                    </div>
                    <div className="meter-bar order-bar">
                      <div
                        className="meter-fill"
                        style={{
                          width: `${Math.min(1, order.delivered / order.amount) * 100}%`,
                          background: resource?.color,
                        }}
                      />
                    </div>
                    <div className="small order-progress">
                      <strong>
                        {formatAmount(order.delivered)} / {formatAmount(order.amount)}
                      </strong>{" "}
                      delivered
                      {order.delivered > 0 && (
                        <>
                          {" "}
                          · <span className="gold-text">{formatAmount(order.delivered * unit)} g</span> paid
                        </>
                      )}
                    </div>
                    <div className="small muted">
                      Pays <span className="gold-text">{formatAmount(order.rewardGold)} g</span> in all
                      {bonus > 0.5 && `, ${formatAmount(bonus)} g more than the hub would`}
                      {!done && ` · ${formatDuration(Math.max(0, left))} left`}
                    </div>
                    <div className="small order-stock">
                      {done
                        ? `Filled: +${formatAmount(order.premium)} g over the hub's price`
                        : eta !== null
                          ? // At the rate you sell now: a fact, no "about" (the owner, 2026-10-31: "everything you find make it exact")
                            `You sell ${formatAmount(order.selling)} a minute${ahead.length > 0 ? ` (${ahead.some((t) => t.urgent) ? "the shortage order" : "the order above"} fills first)` : ""}. At that rate it is full in ${formatDuration(eta)}${eta > left ? `, after it closes: ${formatAmount(Math.max(0, order.selling * (left / 60) - before))} of it fills by then` : ""}.`
                          : `Your trading posts sell no ${good} now: pipe a building that makes it to one`}
                    </div>
                    {deliver >= 1 && (
                      <div className="order-deliver">
                        <button
                          className="small"
                          title="Your Storages kept these for your next buildings and pipes: they go to the hub now"
                          onClick={() => void gameClient.deliverOrder(order.id)}
                        >
                          Deliver {formatAmount(deliver)} from your Storages · +{formatAmount(deliver * unit)}{" "}
                          g
                        </button>
                      </div>
                    )}
                  </div>
                  {done && <span className="order-check">✔</span>}
                </div>
              );
            })}
          </div>
        ) : market === null ? (
          <p className="muted">Loading…</p>
        ) : (
          <div className="market">
            <p className="muted small">
              The market buys everything your Trading Post brings it. Up to your <Term word="hubOrder" /> a
              minute it pays the full price. Sell more and every unit pays less: 4× as much earns 2× the gold.
              It looks again every minute, and only your own sales count. What raises your Hub Orders: your
              Trading Post&apos;s levels, research from tier 5, your town&apos;s Wonder and a Market Monument.
              Goods of a later Era than your town&apos;s earn a quarter. Players set their own prices, between
              what the market pays and what it charges.
            </p>
            <table className="market-table">
              <thead>
                <tr>
                  <th>Good</th>
                  <th>Pays you</th>
                  <th>Hub sells at</th>
                  <th>You sell a minute / full price up to</th>
                  <th>Players&apos; usual price</th>
                  <th>Sell to players</th>
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
                    <td className={row.soldPerMinute > row.hubOrder ? "warning-text" : "muted"}>
                      {formatAmount(row.soldPerMinute)} /{" "}
                      {row.hubOrder > 0 ? formatAmount(row.hubOrder) : "a later Era"}
                    </td>
                    <td>{row.playerPrice.toFixed(2)}</td>
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
  const [typed, setTyped] = useState<number | null>(null);
  const gold = useAppStore((state) => state.company?.gold ?? 0);
  const yourEra = useAppStore((state) => state.company?.yourEra ?? 1);
  if (exchange === null) {
    return <p className="muted">Loading…</p>;
  }
  // Your own price, between the market's two (1.4 §8; the default player price until you type one), said before the
  // click with what a sell order needs in your Storages (after run 4: Cleo's typed 3 g became 1.95 without a word, and
  // Ana's sell order was refused for stone she didn't have stored)
  const base = content.resource(resource)?.price ?? 1;
  const high = base * content.progression.hubSpread;
  const price = Math.min(high, Math.max(base, typed ?? exchange.prices?.[resource] ?? base));
  const stored = exchange.stored?.[resource] ?? 0;
  const good = content.resourceName(resource).toLowerCase();
  const blocked = selling
    ? stored < amount
      ? `You have ${formatAmount(stored)} ${good} in your Storages: a sell order holds its goods there`
      : null
    : gold < amount * price
      ? `It holds ${formatAmount(amount * price)} g till it fills: you have ${formatAmount(gold)} g`
      : null;
  return (
    <div className="exchange">
      <p className="muted small">
        Trade with anyone {exchange.global ? "in every town" : "in the town"} without a pipe, at your own
        price. Sell orders hold their goods from your Storages, buy orders hold their gold; you buy goods of
        your own Era or older. Goods go by cargo; the seller pays a {Math.round(exchange.fee * 100)}% fee that
        disappears. <strong>Your Yard&apos;s neighbours need no Exchange:</strong> what you pipe to your Yard
        gate goes to them by itself.
      </p>
      {exchange.opens !== null ? (
        <p className="status idle">The Exchange opens at Company Level {exchange.opens}.</p>
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
                setTyped(null);
              }}
            >
              {content.resources
                .filter((r) => r.era <= yourEra + (selling ? 1 : 0))
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
            Price
            <input
              type="number"
              min={base}
              max={high}
              step={0.01}
              value={Number(price.toFixed(2))}
              onChange={(e) => setTyped(Number(e.target.value))}
            />
          </label>
          <button className="primary" type="submit" disabled={blocked !== null}>
            Place order
          </button>
          <p className="small exchange-terms">
            <strong>{price.toFixed(2)} g</strong> each (between {base.toFixed(2)} and {high.toFixed(2)}) ·{" "}
            {selling
              ? `${formatAmount(stored)} ${good} in your Storages`
              : `holds ${formatAmount(amount * price)} g till it fills`}{" "}
            · waits up to 7 days
            {blocked !== null && <span className="warning-text"> · {blocked}</span>}
          </p>
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
                <span className="muted small">
                  {order.placed > order.amount + 0.5 &&
                    ` · ${formatAmount(order.placed - order.amount)} of ${formatAmount(order.placed)} ${order.selling ? "sold" : "came"}`}
                  {order.expiresAt !== null &&
                    ` · closes in ${formatDuration(Math.max(0, (new Date(order.expiresAt).getTime() - serverNow) / 1000))}, then the rest comes back`}
                </span>
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
