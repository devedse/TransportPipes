import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import type { FoundPlayerDto, PriceDto } from "../api/contracts";
import { useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

/** The price band (GameWorld.MinPriceFactor, MaxPriceFactor) and the defaults */
const MIN_PRICE = 1;
const MAX_PRICE = 2.5;
const DEFAULT_PUBLIC = 1.3;
const DEFAULT_CREW = 1.15;
const STEP = 0.05;
const RISE_WAITS = "A rise waits an hour, and your buyers are told";

function find(prices: PriceDto[], key: string): PriceDto | undefined {
  return prices.find((t) => t.key === key);
}

/** A price as it goes: what it comes to (a rise that waits counted), and what it is until then */
interface Price {
  value: number;
  now: number;
  /** When the rise counts (null: nothing waits) */
  at: string | null;
}

/** A setting of its own as it goes, else what it inherits */
function resolve(setting: PriceDto | undefined, inherited: Price): Price {
  if (setting === undefined) {
    return inherited;
  }
  if (setting.pendingAt == null) {
    return { value: setting.value, now: setting.value, at: null };
  }
  // A rise back (the setting goes) comes to what it inherits
  return {
    value: setting.back ? inherited.value : (setting.pending ?? setting.value),
    now: setting.value,
    at: setting.pendingAt,
  };
}

/** What a price key comes to: its own setting, else the general one for its kind, else the default */
function priceOf(prices: PriceDto[], kind: "public" | "crew", good?: string): Price {
  const fallback = kind === "public" ? DEFAULT_PUBLIC : DEFAULT_CREW;
  const general = resolve(find(prices, kind), { value: fallback, now: fallback, at: null });
  return good === undefined ? general : resolve(find(prices, `${kind}:${good}`), general);
}

function times(value: number): string {
  return `${value.toFixed(2).replace(/0$/, "")}×`;
}

function clock(at: string): string {
  return new Date(at).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

/** "1.0× until 15:42": what a price is while its rise waits */
function waiting(price: Price): string | null {
  return price.at === null || Math.abs(price.now - price.value) < 1e-9
    ? null
    : `${times(price.now)} until ${clock(price.at)}`;
}

/** Sets a price; one that comes to what it would inherit goes back to inheriting it */
function setPrice(key: string, value: number, inherited: number | null) {
  void gameClient.setPrice(key, inherited !== null && Math.abs(value - inherited) < 1e-9 ? null : value);
}

/** A price as a multiple of the base, one step at a time between 1× and 2.5× */
function PriceStepper({
  value,
  onChange,
  label,
}: {
  value: number;
  onChange: (value: number) => void;
  label: string;
}) {
  const round = (v: number) => Math.round(Math.min(MAX_PRICE, Math.max(MIN_PRICE, v)) * 100) / 100;
  return (
    <span className="price-stepper" role="group" aria-label={label}>
      <button
        type="button"
        className="small"
        aria-label={`${label}: lower`}
        disabled={value <= MIN_PRICE + 1e-9}
        onClick={() => onChange(round(value - STEP))}
      >
        −
      </button>
      <strong>{times(value)}</strong>
      <button
        type="button"
        className="small"
        aria-label={`${label}: higher`}
        disabled={value >= MAX_PRICE - 1e-9}
        onClick={() => onChange(round(value + STEP))}
      >
        +
      </button>
    </span>
  );
}

/** A special price for one player: who, how much, and the stepper */
function SpecialPrice({ price }: { price: PriceDto }) {
  // Removed, it still counts until its hour is up
  const special = resolve(price, { value: price.value, now: price.value, at: null });
  const pending =
    price.back && price.pendingAt != null ? `ends at ${clock(price.pendingAt)}` : waiting(special);
  return (
    <li className="partner">
      <span className="partner-name">
        <strong>{price.forName ?? "A player"}</strong>
        {pending !== null && (
          <span className="muted small" title={RISE_WAITS}>
            {pending}
          </span>
        )}
      </span>
      <PriceStepper
        value={special.value}
        label={`Your price for ${price.forName ?? "them"}`}
        onChange={(v) => setPrice(price.key, v, null)}
      />
      <button
        type="button"
        className="link small"
        title="They pay your public or crew price again"
        onClick={() => void gameClient.setPrice(price.key, null)}
      >
        remove
      </button>
    </li>
  );
}

/** Finds a player by name for a special price */
function AddSpecial({ taken, myId }: { taken: Set<string>; myId: number }) {
  const [text, setText] = useState("");
  const [found, setFound] = useState<FoundPlayerDto[] | null>(null);
  const search = async (value: string) => {
    setText(value);
    setFound(value.trim().length < 2 ? null : await gameClient.connection.findPlayer(value).catch(() => []));
  };
  const options = (found ?? []).filter((t) => t.id !== myId && !taken.has(`for:${t.id}`));
  return (
    <div className="add-special">
      <input
        aria-label="A player's name"
        placeholder="Add a player by name…"
        value={text}
        onChange={(e) => void search(e.target.value)}
      />
      {found !== null && options.length === 0 && <span className="muted small">Nobody of that name.</span>}
      {options.map((t) => (
        <button
          key={t.id}
          type="button"
          className="small"
          onClick={() => {
            void gameClient.setPrice(`for:${t.id}`, MIN_PRICE);
            setText("");
            setFound(null);
          }}
        >
          <span className="company-dot" style={{ background: t.color }} /> {t.name}
          <span className="muted"> · {t.yard ?? t.district}</span>
        </button>
      ))}
    </div>
  );
}

/**
 * Who you trade with through your Yard and on your links, your prices (1.4 §8: public, crew and special prices for
 * named players, each a multiple of the good's base between the market's two prices), who in your Yard you block, and
 * per good whether you sell it to players and buy it from them
 */
export function TradeSettings() {
  const market = useAppStore((s) => s.market);
  const company = useAppStore((s) => s.company);
  const content = useAppStore((s) => s.content);
  const neighbours = useAppStore((s) => s.neighbours);
  if (company === null || content === null) {
    return null;
  }
  const prices = company.prices ?? [];
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
  const rows = (market?.resources ?? []).filter(
    (t) =>
      mine.has(t.resource) ||
      t.noTrade ||
      t.noBuy ||
      find(prices, `public:${t.resource}`) !== undefined ||
      find(prices, `crew:${t.resource}`) !== undefined,
  );
  const residents = neighbours.filter((t) => t.id !== company.id);
  const specials = prices.filter((t) => t.key.startsWith("for:"));
  const general = (kind: "public" | "crew") => {
    const price = priceOf(prices, kind);
    return {
      value: price.value,
      pending: waiting(price),
      own: Math.abs(price.value - (kind === "public" ? DEFAULT_PUBLIC : DEFAULT_CREW)) > 1e-9,
    };
  };
  const pub = general("public");
  const crew = general("crew");

  return (
    <div className="trade-settings">
      <p className="trade-intro">
        Lay a pipe from your network to your Yard gate: what your buildings don&apos;t use goes to the
        neighbours who need it, at your price for them, and your factories buy what they lack at theirs. A
        buyer pays the lowest of the prices that apply to them.
      </p>

      <section>
        <h4>Your prices</h4>
        <p className="muted small">
          What another player pays you for a unit of your goods, as a multiple of its base price: from{" "}
          {times(MIN_PRICE)}, what the hub pays you for it anyway, to {times(MAX_PRICE)}, what the market
          would charge them. So trading with a player is never worse than the market for either side; higher
          earns you more a unit, lower keeps your neighbours buying from you. A change counts at once.
        </p>
        <ul className="partner-list price-list">
          <li className="partner">
            <span className="partner-name">
              <strong>Public price</strong>
              <span className="muted small" title={pub.pending !== null ? RISE_WAITS : undefined}>
                {pub.pending ?? "for players outside your Yard (on links)"}
              </span>
            </span>
            <PriceStepper
              value={pub.value}
              label="Your public price"
              onChange={(v) => setPrice("public", v, DEFAULT_PUBLIC)}
            />
            {pub.own ? (
              <button
                type="button"
                className="link small"
                onClick={() => void gameClient.setPrice("public", null)}
              >
                {times(DEFAULT_PUBLIC)}
              </button>
            ) : (
              <span />
            )}
          </li>
          <li className="partner">
            <span className="partner-name">
              <strong>Crew price</strong>
              <span className="muted small" title={crew.pending !== null ? RISE_WAITS : undefined}>
                {crew.pending ?? "for your Yard's neighbours"}
              </span>
            </span>
            <PriceStepper
              value={crew.value}
              label="Your crew price"
              onChange={(v) => setPrice("crew", v, DEFAULT_CREW)}
            />
            {crew.own ? (
              <button
                type="button"
                className="link small"
                onClick={() => void gameClient.setPrice("crew", null)}
              >
                {times(DEFAULT_CREW)}
              </button>
            ) : (
              <span />
            )}
          </li>
          {specials.map((price) => (
            <SpecialPrice key={price.key} price={price} />
          ))}
        </ul>
        <AddSpecial taken={new Set(specials.map((t) => t.key))} myId={company.id} />
      </section>

      <section>
        <h4>What the market brings</h4>
        <label className="check">
          <input
            type="checkbox"
            checked={company.wantsFromMarket}
            onChange={(e) => void gameClient.setWantsFromMarket(e.target.checked)}
          />
          The market brings what your levels and new buildings still lack
        </label>
        <p className="muted small">
          First come your Storages, your own spare, your Yard and its Depot. What none of them has, the market
          brings within about ten minutes, at 2.5× its price. Off: your levels wait for goods from players.
        </p>
      </section>

      <section>
        <h4>Your Yard</h4>
        {residents.length === 0 ? (
          <p className="muted small">Nobody else lives around your Yard yet.</p>
        ) : (
          <ul className="partner-list">
            {residents.map((n) => {
              // A block waits an hour's notice: the switch is off from the click (playtest: it snapped back)
              const off = n.blocked || n.blockAt !== null;
              return (
                <li key={n.id} className={off ? "partner blocked" : "partner"}>
                  <span className="company-dot" style={{ background: n.color }} />
                  <span className="partner-name">
                    <strong>{n.name}</strong>
                    <span className="muted small">
                      {n.connected ? "piped to the Yard" : "not piped to the Yard yet"}
                      {n.youEarn > 0.05 && ` · you earn ${formatAmount(n.youEarn)} g/min`}
                      {n.youPay > 0.05 && ` · you pay ${formatAmount(n.youPay)} g/min`}
                      {n.blockAt !== null &&
                        ` · trading stops at ${clock(n.blockAt)}: switch it on to call it off`}
                      {n.blocked && " · you don't trade with them"}
                    </span>
                  </span>
                  <label
                    className="switch"
                    title={off ? "Trade with them again" : "Block: don't trade with them (at once)"}
                  >
                    <input
                      type="checkbox"
                      checked={!off}
                      onChange={() => void gameClient.setTradeBlocked(n.id, !off)}
                    />
                    <span className="switch-track" />
                  </label>
                </li>
              );
            })}
          </ul>
        )}
        <p className="muted small">
          Blocking is private: they only read that you trade your spare with others. It counts at once, and so
          does trading again.
        </p>
      </section>

      <section>
        <h4>Per good</h4>
        {market === null ? (
          <p className="muted">Loading…</p>
        ) : rows.length === 0 ? (
          <p className="muted">Build something first: what your buildings make and use shows up here.</p>
        ) : (
          <div className="table-scroll">
            <table className="trade-table">
              <thead>
                <tr>
                  <th />
                  <th title="What the market pays you for it, within your Hub Order">Market pays</th>
                  <th title="Your price for players outside your Yard (on links)">Public</th>
                  <th title="Your price for your Yard's neighbours">Crew</th>
                  <th>Sell to players</th>
                  <th>Buy from players</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => {
                  const own = priceOf(prices, "public", row.resource);
                  const ownCrew = priceOf(prices, "crew", row.resource);
                  const cell = (kind: "public" | "crew", price: Price, label: string) => {
                    const pending = waiting(price);
                    return (
                      <td>
                        <PriceStepper
                          value={price.value}
                          label={`${label} for ${content.resourceName(row.resource)}`}
                          onChange={(v) =>
                            setPrice(`${kind}:${row.resource}`, v, priceOf(prices, kind).value)
                          }
                        />
                        <span className="muted small"> {formatAmount(row.basePrice * price.value)} g</span>
                        {pending !== null && (
                          <span className="price-waiting muted small" title={RISE_WAITS}>
                            {pending}
                          </span>
                        )}
                      </td>
                    );
                  };
                  return (
                    <tr key={row.resource}>
                      <td>
                        <ResourceIcon resource={row.resource} /> {content.resourceName(row.resource)}
                      </td>
                      <td>{row.buyPrice.toFixed(2)} g</td>
                      {cell("public", own, "Public price")}
                      {cell("crew", ownCrew, "Crew price")}
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
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
