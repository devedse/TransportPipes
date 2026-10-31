import { useEffect } from "react";
import type { IncomeDto, IncomeLineDto } from "../api/contracts";
import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import type { Content } from "../game/content";
import { setState, useAppStore } from "../state/store";

/** The income's sections, in order: what each kind of line is */
const INCOME_SECTIONS: Array<{ kinds: IncomeLineDto["kind"][]; title: string }> = [
  { kinds: ["hub"], title: "Sold at the hub (your Trading Post)" },
  { kinds: ["storage"], title: "Storages selling what they hold above their line" },
  { kinds: ["yardSold", "yardOnItsWay", "yardFloor"], title: "Sold through the Yard" },
  { kinds: ["player"], title: "Sold to players over your pipes" },
  { kinds: ["yardBought"], title: "Bought through the Yard" },
  { kinds: ["fromPlayer"], title: "Bought from players over your pipes" },
  { kinds: ["import"], title: "Bought from the hub (Buy from the hub)" },
];

function signed(gold: number): string {
  return `${gold < 0 ? "−" : "+"}${formatAmount(Math.abs(gold))} g`;
}

/**
 * Where your gold a minute comes from (playtest: "a very detailed explanation what makes this total gold come to be ...
 * we keep seeing it change all the time and I don't understand why"): every line the top bar adds up, what isn't in it,
 * and why it moves
 */
function IncomeBreakdown({ income, content }: { income: IncomeDto; content: Content }) {
  return (
    <details className="income" open>
      <summary>
        <strong>Where your {signed(income.total)}/min comes from</strong>
      </summary>
      <table className="income-table">
        <tbody>
          {INCOME_SECTIONS.map((section) => {
            const lines = income.lines.filter((t) => section.kinds.includes(t.kind));
            if (lines.length === 0) {
              return null;
            }
            const sum = lines.reduce((total, t) => total + t.gold, 0);
            return [
              <tr key={section.title} className="income-section">
                <th>{section.title}</th>
                <td>{signed(sum)}</td>
              </tr>,
              ...lines.map((line) => (
                <tr key={`${line.kind}-${line.resource}-${line.who ?? ""}`}>
                  <td>
                    {content.resourceName(line.resource)} · {formatAmount(line.units)}/min ×{" "}
                    {line.units > 0 ? (Math.abs(line.gold) / line.units).toFixed(2) : "0"} g
                    {line.who !== null &&
                      (line.kind === "yardBought" || line.kind === "fromPlayer"
                        ? ` from ${line.who}`
                        : ` to ${line.who}`)}
                    {line.kind === "yardOnItsWay" && " · on its way"}
                    {line.kind === "yardFloor" && " · not taken: paid at your hub price"}
                    {line.hubOrder !== null && line.units > line.hubOrder + 0.5 && (
                      <span className="muted">
                        {" "}
                        · beyond your Hub Order of {formatAmount(line.hubOrder)}/min: the market pays less for
                        all of it
                      </span>
                    )}
                  </td>
                  <td>{signed(line.gold)}</td>
                </tr>
              )),
            ];
          })}
          <tr className="income-total">
            <th>Your gold a minute (the top bar)</th>
            <td>{signed(income.total)}</td>
          </tr>
        </tbody>
      </table>
      <p className="small">
        <strong>Not in this number</strong>
      </p>
      <ul className="muted small income-notes">
        {income.ordersToday >= 1 && (
          <li>
            Hub orders: today they paid you {formatAmount(income.ordersToday)} g extra, on top of the
            hub&apos;s price, as your goods went through your Trading Post.
          </li>
        )}
        {income.ownGoodsIntoWants >= 1 && (
          <li>
            Your levels: your own goods that go into the levels you bought would sell for{" "}
            {formatAmount(income.ownGoodsIntoWants)} g a minute. Until the levels have them, your sales above
            are that much lower.
          </li>
        )}
        <li>
          What the market brings for your levels and buildings, and Buy the rest, is paid when it comes.
        </li>
      </ul>
      <p className="small">
        <strong>Why it keeps changing</strong>
      </p>
      <ul className="muted small income-notes">
        <li>
          The hub pays by what you sold in the last hour, and looks again every 15 minutes. It pays the full
          price for your Hub Order of each good; sell more than that, and it pays less for all of it.
        </li>
        <li>Every minute the Yard matches what your neighbours take from you and bring you.</li>
        <li>
          Levels coming in, buildings starting or waiting for goods, Storages filling or selling, and events
          such as a Festival or a storm change what your buildings make.
        </li>
      </ul>
    </details>
  );
}

/**
 * What your gold can do (after run 4: the players spent 0.2% of what they earned, and five offers of gold in the group
 * chat had no door): at most three lines, shared first: the shared goal, a want gold can fill, and "Grow everything that
 * pays" with one preview and one tap
 */
export function GoldPanel({ days, onClose }: { days: number; onClose: () => void }) {
  const uses = useAppStore((s) => s.goldUses);
  const gold = useAppStore((s) => s.company?.gold ?? 0);
  const content = useAppStore((s) => s.content);
  useEffect(() => {
    void gameClient.loadGoldUses();
    // The income moves: looked at again while the panel is open
    const timer = window.setInterval(() => void gameClient.loadGoldUses(), 15000);
    return () => {
      window.clearInterval(timer);
      setState({ goldUses: null });
    };
  }, []);
  const empty =
    uses !== null &&
    uses.shared === null &&
    uses.want === null &&
    (uses.grow?.parts.length ?? 0) === 0 &&
    uses.grow?.saving == null;
  return (
    <div className="panel gold-uses" role="dialog" aria-label="What your gold can do">
      <div className="gold-uses-head">
        <strong>Your gold can</strong>
        <button className="icon-button" title="Close" onClick={onClose}>
          ×
        </button>
      </div>
      <p className="muted small">
        {days >= 1
          ? `${days.toFixed(days < 10 ? 1 : 0)} days of your income`
          : "Less than a day of your income"}
        {uses !== null && ` · an hour of it is ${formatAmount(uses.hour)} g`}
      </p>
      {uses === null && <p className="muted small">Looking…</p>}
      {uses?.income != null && content !== null && <IncomeBreakdown income={uses.income} content={content} />}
      {uses?.shared != null && uses.shared.action === "wonder" && (
        <div className="gold-use shared">
          <span>🏛 {uses.shared.text}</span>
          <span className="gold-use-buttons">
            {uses.shared.cost !== null && (
              <button
                className="small"
                disabled={gold < uses.shared.cost}
                title="As much as a day of your income buys, at the market's 2.5×; it counts for your tier"
                onClick={() => void gameClient.buyAndGive(null, uses.shared!.cost)}
              >
                Buy and give a day&apos;s worth · {formatAmount(uses.shared.cost)} g
              </button>
            )}
            {uses.shared.allCost !== null && (
              <button
                className="small"
                disabled={gold < uses.shared.allCost}
                onClick={() => void gameClient.buyAndGive(null)}
              >
                Buy and give all of it · {formatAmount(uses.shared.allCost)} g
              </button>
            )}
          </span>
        </div>
      )}
      {uses?.want != null && uses.want.target !== null && uses.want.cost !== null && (
        <div className="gold-use">
          <span>🧱 {uses.want.text}</span>
          <button
            className="small"
            disabled={gold < uses.want.cost}
            onClick={() =>
              void gameClient
                .orderRest(uses.want!.target!, uses.want!.forName, uses.want!.resource)
                .then(() => gameClient.loadGoldUses())
            }
          >
            {uses.want.forName !== null ? `Buy it for ${uses.want.forName}` : "Buy the rest"} ·{" "}
            {formatAmount(uses.want.cost)} g
          </button>
        </div>
      )}
      {uses?.grow != null && uses.grow.parts.length > 0 && (
        <div className="gold-use grow">
          <span>
            💡 <strong>Grow everything that pays</strong>: about +{formatAmount(uses.grow.gain)} g/min for{" "}
            {formatAmount(uses.grow.cost)} g (
            {Math.max(1, Math.round((100 * uses.grow.cost) / Math.max(1, gold)))}% of your gold)
            {(uses.grow.goods ?? 0) >= 1 &&
              // Levels from 11 wait for goods (playtest: Grow said +504 g/min, and the income first dipped)
              `, and about ${formatAmount(uses.grow.goods ?? 0)} g of goods its levels from 11 wait for: your own go in first, so your income dips until they're in`}
            : your next parcel first, then each level back within five days
          </span>
          <details>
            <summary className="small">
              {uses.grow.parts.length} {uses.grow.parts.length === 1 ? "part" : "parts"}
            </summary>
            <ul className="small">
              {uses.grow.parts.map((part) => (
                <li key={part.building}>
                  {part.text} · {formatAmount(part.cost)} g · +{formatAmount(part.gain)} g/min
                </li>
              ))}
            </ul>
          </details>
          <button
            className="primary"
            disabled={gold < uses.grow.cost}
            onClick={() => void gameClient.growEverything()}
          >
            Grow · {formatAmount(uses.grow.cost)} g
          </button>
        </div>
      )}
      {uses?.grow?.saving != null && (
        // Land first, as the model plays (1.4 §17): Grow keeps the gold while the next parcel is a few hours away
        <p className="gold-use grow small">💡 {uses.grow.saving}</p>
      )}
      {empty && (
        <p className="small">
          Nothing pays back within five days now: your buildings are at what their pipes and inputs carry, and
          nobody near waits for anything gold can bring. New chains, the next parcel and the Era Wonder are in
          Next steps.
        </p>
      )}
      {(uses?.shared != null || uses?.want != null) && (
        <p className="muted small">
          {uses?.shared?.action === "wonder" && (
            <button className="link small" onClick={() => void gameClient.openHub("wonders")}>
              Hub › Town &amp; Wonder
            </button>
          )}
        </p>
      )}
    </div>
  );
}
