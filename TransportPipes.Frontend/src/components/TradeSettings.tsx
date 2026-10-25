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

function find(prices: PriceDto[], key: string): PriceDto | undefined {
  return prices.find((t) => t.key === key);
}

/** What a price key comes to: its own setting, else the general one for its kind, else the default */
function priceOf(prices: PriceDto[], kind: "public" | "crew", good?: string): number {
  const fallback = kind === "public" ? DEFAULT_PUBLIC : DEFAULT_CREW;
  return (
    (good !== undefined ? find(prices, `${kind}:${good}`)?.value : undefined) ??
    find(prices, kind)?.value ??
    fallback
  );
}

function times(value: number): string {
  return `${value.toFixed(2).replace(/0$/, "")}×`;
}

/** "rises to 1.6× at 15:42", "back to 1.3× at 15:42" */
function pendingText(setting: PriceDto | undefined, fallback: number): string | null {
  if (setting?.pendingAt == null) {
    return null;
  }
  const at = new Date(setting.pendingAt).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  return setting.back
    ? `back to ${times(fallback)} at ${at}`
    : `rises to ${times(setting.pending ?? fallback)} at ${at}`;
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
  const pending = pendingText(price, price.value);
  return (
    <li className="partner">
      <span className="partner-name">
        <strong>{price.forName ?? "A player"}</strong>
        {pending !== null && <span className="muted small">{pending}</span>}
      </span>
      <PriceStepper
        value={price.value}
        label={`Your price for ${price.forName ?? "them"}`}
        onChange={(v) => void gameClient.setPrice(price.key, v)}
      />
      <button
        type="button"
        className="link small"
        title="They pay your public or crew price again (after an hour, when that is more)"
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
    const setting = find(prices, kind);
    const fallback = kind === "public" ? DEFAULT_PUBLIC : DEFAULT_CREW;
    return {
      value: setting?.value ?? fallback,
      pending: pendingText(setting, fallback),
      own: setting !== undefined,
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
          Multiples of each good&apos;s base price, between what the market pays ({times(MIN_PRICE)}) and what
          it charges ({times(MAX_PRICE)}): trading with a friend is never worse than the market for either
          side. A cut counts at once; a rise waits an hour, and your buyers are told.
        </p>
        <ul className="partner-list price-list">
          <li className="partner">
            <span className="partner-name">
              <strong>Public price</strong>
              <span className="muted small">{pub.pending ?? "for everyone"}</span>
            </span>
            <PriceStepper
              value={pub.value}
              label="Your public price"
              onChange={(v) => void gameClient.setPrice("public", v)}
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
              <span className="muted small">{crew.pending ?? "for your Yard"}</span>
            </span>
            <PriceStepper
              value={crew.value}
              label="Your crew price"
              onChange={(v) => void gameClient.setPrice("crew", v)}
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
            {residents.map((n) => (
              <li key={n.id} className={n.blocked ? "partner blocked" : "partner"}>
                <span className="company-dot" style={{ background: n.color }} />
                <span className="partner-name">
                  <strong>{n.name}</strong>
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
                  <th title="Your price for everyone">Public</th>
                  <th title="Your price for your Yard">Crew</th>
                  <th>Sell to players</th>
                  <th>Buy from players</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => {
                  const own = priceOf(prices, "public", row.resource);
                  const ownCrew = priceOf(prices, "crew", row.resource);
                  return (
                    <tr key={row.resource}>
                      <td>
                        <ResourceIcon resource={row.resource} /> {content.resourceName(row.resource)}
                      </td>
                      <td>{row.buyPrice.toFixed(2)} g</td>
                      <td>
                        <PriceStepper
                          value={own}
                          label={`Public price for ${content.resourceName(row.resource)}`}
                          onChange={(v) => void gameClient.setPrice(`public:${row.resource}`, v)}
                        />
                        <span className="muted small"> {formatAmount(row.basePrice * own)} g</span>
                      </td>
                      <td>
                        <PriceStepper
                          value={ownCrew}
                          label={`Crew price for ${content.resourceName(row.resource)}`}
                          onChange={(v) => void gameClient.setPrice(`crew:${row.resource}`, v)}
                        />
                        <span className="muted small"> {formatAmount(row.basePrice * ownCrew)} g</span>
                      </td>
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
