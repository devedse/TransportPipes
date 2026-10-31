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
const RISE_WAITS = "A higher price starts in one hour; your buyers are told first";

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
        title="They pay your price for other Yards or your Yard price again"
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
        Pipe your buildings to your Yard gate. Then your spare goes to neighbours who need it, at your price,
        and your buildings buy what they lack from neighbours, at theirs. If several of your prices fit a
        buyer, they pay the lowest.
      </p>

      <section>
        <h4>Your prices</h4>
        <p className="muted small">
          What players pay you for your goods, as × the base price. {times(MIN_PRICE)} is what the hub pays
          you anyway; {times(MAX_PRICE)} is what the market would charge them. Higher earns you more; lower
          keeps neighbours buying from you. A change counts from the Yard&apos;s next round (the clock in the
          top bar), and at once for what you sell over your own pipes.
        </p>
        <ul className="partner-list price-list">
          <li className="partner">
            <span className="partner-name">
              <strong>Price for other Yards</strong>
              <span className="muted small" title={pub.pending !== null ? RISE_WAITS : undefined}>
                {pub.pending ?? "what players on your links pay"}
              </span>
            </span>
            <PriceStepper
              value={pub.value}
              label="Your price for other Yards"
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
              <strong>Yard price</strong>
              <span className="muted small" title={crew.pending !== null ? RISE_WAITS : undefined}>
                {crew.pending ?? "what your Yard's neighbours pay"}
              </span>
            </span>
            <PriceStepper
              value={crew.value}
              label="Your Yard price"
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
          Buy missing upgrade goods from the hub by themselves (2.5× their price, within about ten minutes)
        </label>
        <p className="muted small">
          First come your Storages, your own spare, your Yard and its Depot. What none of them has, the market
          brings within about ten minutes, at 2.5× its price. Off: your levels wait for goods from players.
        </p>
      </section>

      <section>
        {/* Everyone in your town (the menu map, 2026-10-31: it said "Your Yard" over the whole town): a switch stops
            all trade with them, at the Yard, over links and over pipes */}
        <h4>Who you trade with</h4>
        <p className="muted small">
          Everyone in your town. Switch someone off and you stop trading with them: at the Yard, over links
          and over pipes. It counts from the Yard&apos;s next round.
        </p>
        {residents.length === 0 ? (
          <p className="muted small">Nobody else lives in your town yet.</p>
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
                        ` · trading stops at ${clock(n.blockAt)}: switch it back on to cancel`}
                      {n.blocked && " · you don't trade with them"}
                    </span>
                  </span>
                  <label
                    className="switch"
                    title={
                      off
                        ? "Trade with them again"
                        : "Block: don't trade with them (from the Yard's next round)"
                    }
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
          They aren&apos;t told: they just see your goods go to others. It counts from the Yard&apos;s next
          round, and so does switching them back on.
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
                  <th title="What players on your links pay">Other Yards</th>
                  <th title="What your Yard's neighbours pay">Yard</th>
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
                      {cell("public", own, "Price for other Yards")}
                      {cell("crew", ownCrew, "Yard price")}
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
