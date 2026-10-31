import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useYardRoundIn } from "../app/yardRound";
import type { OwnWantDto } from "../api/contracts";
import { currentGold, useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

/**
 * The Yards' next round, counting down (the owner, 2026-10-31: "a timer in the top for that 1 minute yard update"):
 * links, Yard trades and what neighbours bring for your levels move at its rounds
 */
export function YardClock() {
  const left = useYardRoundIn();
  if (left === null) {
    return null;
  }
  return (
    <div
      className="panel top-yard"
      title={
        "The Yard's next round. At every round your links and the Yard's trades move and your neighbours' goods for " +
        "your levels arrive: once a minute while trades change, less often (up to every 16 minutes) when nothing " +
        "changes. What you change at your Yard gate, in your links or your trade settings comes in at the next round."
      }
    >
      <span className="small muted">Yard round</span>
      <strong>{left >= 1 ? formatDuration(left) : "now"}</strong>
    </div>
  );
}

/** One building's (or pipe's) wants together: its goods still to come */
interface Waiting {
  target: number;
  name: string;
  toLevel: number | null;
  goods: OwnWantDto[];
  orderCost: number;
}

/**
 * What your levels and buildings wait for (the owner, 2026-10-31: "next to the gold ... upgrades ... if you hover you
 * can see the buildings waiting for resources and a clear 1 button click to upgrade all ... and how much it would
 * take"): each one's goods to come, where they come from, and Buy all the rest in one click
 */
export function WaitingGoods() {
  const company = useAppStore((s) => s.company);
  const companyAt = useAppStore((s) => s.companyAt);
  const content = useAppStore((s) => s.content);
  const [open, setOpen] = useState(false);
  const wants = company?.wants ?? [];
  if (company === null || content === null || wants.length === 0) {
    return null;
  }
  const byTarget = new Map<number, Waiting>();
  for (const want of wants) {
    const known = byTarget.get(want.target) ?? {
      target: want.target,
      name: want.name,
      toLevel: want.toLevel ?? null,
      goods: [],
      orderCost: 0,
    };
    known.goods.push(want);
    known.orderCost += want.orderCost ?? 0;
    known.toLevel = Math.max(known.toLevel ?? 0, want.toLevel ?? 0) || null;
    byTarget.set(want.target, known);
  }
  const waiting = [...byTarget.values()];
  const all = waiting.reduce((sum, t) => sum + t.orderCost, 0);
  const gold = currentGold({ company, companyAt });
  return (
    <div className="top-waits-wrap" onMouseEnter={() => setOpen(true)} onMouseLeave={() => setOpen(false)}>
      <button className="panel top-waits" aria-expanded={open} onClick={() => setOpen((t) => !t)}>
        <span className="small muted">Waiting for goods</span>
        <strong>
          🏗 {waiting.length} · {formatAmount(all)} g
        </strong>
      </button>
      {open && (
        <div className="panel top-waits-panel" role="dialog" aria-label="What waits for goods">
          <strong>What waits for its goods</strong>
          <p className="muted small">
            From level 11 a level takes goods as well as gold, and some buildings take materials. They come in
            by themselves: your Storages and what you make first, then your Yard neighbours&apos; spare at the
            Yard&apos;s rounds,{" "}
            {company.wantsFromMarket
              ? "and what nobody has the market brings within about ten minutes at 2.5× its price (paid as it comes)."
              : "and the market brings nothing for them (your trade settings)."}{" "}
            Buy the rest brings it all at once at 2.5×.
          </p>
          <ul className="top-waits-list">
            {waiting.map((w) => (
              <li key={w.target}>
                <div className="top-waits-head">
                  <button
                    className="link"
                    disabled={w.target <= 0}
                    onClick={() => w.target > 0 && gameClient.showBuilding(w.target)}
                  >
                    {w.name}
                    {w.toLevel !== null && ` → level ${w.toLevel}`}
                  </button>
                  {w.orderCost > 0 && (
                    <button
                      className="small"
                      disabled={gold < w.orderCost}
                      onClick={() => void gameClient.orderRest(w.target)}
                    >
                      Buy the rest · {formatAmount(w.orderCost)} g
                    </button>
                  )}
                </div>
                <div className="top-waits-goods small">
                  {w.goods.map((g) => (
                    <span key={g.resource}>
                      <ResourceIcon resource={g.resource} /> {formatAmount(Math.max(0, g.amount - g.got))} of{" "}
                      {formatAmount(g.amount)} {content.resourceName(g.resource).toLowerCase()}
                      {g.perMinute > 0.005 && ` · ${formatAmount(g.perMinute)}/min coming`}
                    </span>
                  ))}
                </div>
              </li>
            ))}
          </ul>
          {all > 0 && (
            <button className="primary wide" disabled={gold < all} onClick={() => void gameClient.orderAll()}>
              Buy all the rest · {formatAmount(all)} g
            </button>
          )}
          {gold < all && company.goldPerMinute > 0 && (
            <p className="muted small">
              You have {formatAmount(gold)} g: it comes in{" "}
              {formatDuration(((all - gold) / Math.max(1e-9, company.goldPerMinute)) * 60)} at your income.
            </p>
          )}
        </div>
      )}
    </div>
  );
}
