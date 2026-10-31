import type { ReactNode } from "react";
import type { LevelGoodsDto } from "../api/contracts";
import { formatAmount } from "../app/format";
import { useAppStore } from "../state/store";
import { HoverPopup } from "./HoverPopup";
import { ResourceIcon } from "./ResourceIcon";

/**
 * What some levels cost (the owner, 2026-10-31: "when you upgrade a building above level 10 it should show 1100g,
 * 100 wood, 3838 iron ingot"): the gold, and from level 11 the goods with their icons on a line of their own
 */
export function LevelPrice({ gold, goods }: { gold: number; goods?: LevelGoodsDto | null }) {
  return (
    <>
      <span className="level-gold">{formatAmount(gold)} g</span>
      <GoodsLine goods={goods} />
    </>
  );
}

/** The level goods alone, a line with their icons */
export function GoodsLine({ goods }: { goods?: LevelGoodsDto | null }) {
  if (goods == null || goods.goods.length === 0) {
    return null;
  }
  return (
    <span className="level-goods">
      {goods.goods.map((t) => (
        <span key={t.resource} className="level-good">
          + {formatAmount(t.amount)}
          <ResourceIcon resource={t.resource} />
        </span>
      ))}
    </span>
  );
}

/**
 * Around a button that buys levels with goods: on hover or focus a little popup says what its price is made of ("when
 * you hover over it it should explain it clearly in a little popup"): the gold goes at once, the goods come by
 * themselves (from where, at what price), and the building works at its level until they're in
 */
export function GoodsTip({
  what,
  gold,
  goods,
  children,
}: {
  /** "Level 16", "10 levels", "All +1": what the button buys */
  what: string;
  gold: number;
  goods?: LevelGoodsDto | null;
  children: ReactNode;
}) {
  const fromMarket = useAppStore((s) => s.company?.wantsFromMarket ?? true);
  const content = useAppStore((s) => s.content);
  if (goods == null || goods.goods.length === 0 || content === null) {
    return <>{children}</>;
  }
  return (
    <HoverPopup
      popup={
        <>
          <strong>
            {what}: {formatAmount(gold)} g and goods
          </strong>
          <span className="goods-tip-line">From level 11 a level takes goods as well as gold:</span>
          <span className="goods-tip-goods">
            {goods.goods.map((t) => (
              <span key={t.resource}>
                <ResourceIcon resource={t.resource} /> {formatAmount(t.amount)}{" "}
                {content.resourceName(t.resource).toLowerCase()}
              </span>
            ))}
          </span>
          <span className="goods-tip-line">
            The gold goes at once. The goods come in by themselves: first from your Storages and what you
            make, then your Yard neighbours&apos; spare at their price.{" "}
            {fromMarket
              ? "What nobody has, the market brings within about ten minutes at 2.5× its price."
              : "The market brings nothing for them (your trade settings): Buy the rest brings it at once at 2.5× its price."}
          </span>
          <span className="goods-tip-line">
            If the market brought all of it: {formatAmount(goods.marketCost)} g more (paid as it comes).
          </span>
          <span className="goods-tip-line muted">
            Until a level&apos;s goods are in, the building works at the level before, under a little
            scaffolding.
          </span>
        </>
      }
    >
      {children}
    </HoverPopup>
  );
}
