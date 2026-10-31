import { useEffect, useState } from "react";
import type { LevelGoodsDto, SelectionCostsDto } from "../api/contracts";
import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import { levelTier } from "../game/levels";
import { currentGold, setState, useAppStore } from "../state/store";
import { GoodsLine, GoodsTip, LevelPrice } from "./LevelPrice";

/** Several buildings selected together: what they are, upgrading them at once, demolishing them */
export function SelectionPanel() {
  const selection = useAppStore((s) => s.selection);
  const content = useAppStore((s) => s.content);
  const company = useAppStore((s) => s.company);
  const companyAt = useAppStore((s) => s.companyAt);
  const [costs, setCosts] = useState<SelectionCostsDto | null>(null);
  const [budget, setBudget] = useState<number | null>(null);
  // What Spend buys for the slider's gold: its levels and their goods (the owner, 2026-10-31: the goods in every menu
  // that buys levels)
  const [plan, setPlan] = useState<{
    budget: number;
    levels: number;
    gold: number;
    goods: LevelGoodsDto | null;
  } | null>(null);
  const [confirm, setConfirm] = useState(false);
  const levels = selection.map((id) => gameClient.world?.buildings.get(id)?.level ?? 0).join(",");

  // The costs again when the selection or its levels change
  useEffect(() => {
    if (selection.length === 0) {
      return;
    }
    let cancelled = false;
    void gameClient.connection.getSelectionCosts(selection).then(
      (result) => !cancelled && setCosts(result),
      () => undefined,
    );
    return () => {
      cancelled = true;
    };
  }, [selection, levels]);

  const goldNow = company === null ? 0 : currentGold({ company, companyAt });
  const spendNow = Math.floor(Math.min(budget ?? niceAmount(goldNow / 4), goldNow));
  // The slider at rest for a moment: what that gold buys
  useEffect(() => {
    if (selection.length < 2 || spendNow <= 0) {
      return;
    }
    let cancelled = false;
    const timer = window.setTimeout(() => {
      void gameClient.connection.getSpendPlan(selection, spendNow).then(
        (result) => !cancelled && result !== null && setPlan({ budget: spendNow, ...result }),
        () => undefined,
      );
    }, 300);
    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [selection, levels, spendNow]);

  if (selection.length < 2 || content === null || company === null) {
    return null;
  }
  const gold = goldNow;
  const buildings = selection.map((id) => gameClient.world?.buildings.get(id)).filter((t) => t !== undefined);
  const types = new Map<string, { count: number; min: number; max: number; ids: number[] }>();
  for (const b of buildings) {
    const known = types.get(b.type) ?? { count: 0, min: Infinity, max: 0, ids: [] };
    types.set(b.type, {
      count: known.count + 1,
      min: Math.min(known.min, b.level),
      max: Math.max(known.max, b.level),
      ids: [...known.ids, b.id],
    });
  }
  // A quarter of the gold to start with: the slider never starts at everything
  const spend = Math.min(budget ?? niceAmount(gold / 4), gold);
  const hasBlueprintOffice = [...(gameClient.world?.buildings.values() ?? [])].some(
    (t) => t.owner === company.id && t.type === "blueprintOffice" && t.constructionEndsAt === null,
  );

  return (
    <aside className="panel building-panel selection-panel" aria-label="Selection">
      <div className="panel-header">
        <div>
          <h3>{buildings.length} buildings</h3>
          <div className="muted small keyboard-hint">
            Shift-click to add or take out · Ctrl-click: all of a kind
          </div>
        </div>
        <button className="icon-button" title="Clear (Esc)" onClick={() => setState({ selection: [] })}>
          ×
        </button>
      </div>

      <section className="upgrade">
        <h4>Upgrade together</h4>
        {costs === null ? (
          <p className="muted small">…</p>
        ) : costs.upgradable === 0 ? (
          <p className="muted small">None of these can be upgraded.</p>
        ) : (
          <>
            <div className="button-row">
              <GoodsTip what="All +1" gold={costs.plusOne} goods={costs.goodsPlusOne}>
                <button
                  className="primary"
                  disabled={gold < costs.cheapest}
                  title={
                    costs.goodsPlusOne == null
                      ? "Each one level up, the cheapest first when the gold doesn't reach for all"
                      : undefined
                  }
                  onClick={() => void gameClient.upgradeSelection(1)}
                >
                  All +1 · <LevelPrice gold={costs.plusOne} goods={costs.goodsPlusOne} />
                </button>
              </GoodsTip>
              <GoodsTip what="+10 each" gold={costs.plusTen} goods={costs.goodsPlusTen}>
                <button disabled={gold < costs.cheapest} onClick={() => void gameClient.upgradeSelection(10)}>
                  +10 each · <LevelPrice gold={costs.plusTen} goods={costs.goodsPlusTen} />
                </button>
              </GoodsTip>
            </div>
            <div className="budget">
              <label className="small">
                Spend{" "}
                <input
                  type="range"
                  min={0}
                  max={Math.max(1, Math.floor(gold))}
                  value={Math.floor(spend)}
                  onChange={(event) => setBudget(Number(event.target.value))}
                />{" "}
                <strong>{gold > 0 ? Math.round((spend / gold) * 100) : 0}%</strong>
              </label>
              {(() => {
                const known = plan !== null && plan.budget === Math.floor(spend) ? plan : null;
                return (
                  <GoodsTip
                    what={known !== null ? `${known.levels} levels, cheapest first` : "Spend"}
                    gold={known?.gold ?? spend}
                    goods={known?.goods}
                  >
                    <button
                      disabled={spend < costs.cheapest}
                      title={
                        known?.goods == null
                          ? "Level by level, always the cheapest next one: they level out"
                          : undefined
                      }
                      onClick={() => void gameClient.upgradeSelection(1, spend)}
                    >
                      Spend {formatAmount(spend)} g, cheapest first
                      {known !== null && known.levels > 0 && (
                        <span className="level-goods">
                          {known.levels} levels for {formatAmount(known.gold)} g
                        </span>
                      )}
                      <GoodsLine goods={known?.goods} />
                    </button>
                  </GoodsTip>
                );
              })()}
            </div>
          </>
        )}
        <p className="muted small keyboard-hint">
          Keys: <kbd>U</kbd> all +1 · <kbd>Shift</kbd>+<kbd>U</kbd> +10 each · <kbd>Del</kbd> demolish
        </p>
      </section>

      {costs !== null && Object.keys(costs.makes).length > 0 && (
        <p className="selection-makes small">
          Together{" "}
          {Object.entries(costs.makes)
            .sort((a, b) => b[1] - a[1])
            .map(
              ([resource, amount]) =>
                `+${formatAmount(amount)} ${content.resourceName(resource).toLowerCase()}/min`,
            )
            .join(" · ")}
        </p>
      )}

      <section>
        <h4>In it</h4>
        <ul className="flow-list">
          {[...types.entries()].map(([type, t]) => {
            const tier = levelTier(t.max);
            return (
              <li key={type}>
                <span className="selection-count">{t.count}×</span>
                <span>{content.building(type)?.name ?? type}</span>
                <span className="flow-amount" style={{ color: tier?.css }}>
                  Lv {t.min === t.max ? t.min : `${t.min}–${t.max}`}
                </span>
                {types.size > 1 && (
                  <button
                    className="link small selection-only"
                    onClick={() => gameClient.selectMany(t.ids, false)}
                  >
                    only these
                  </button>
                )}
              </li>
            );
          })}
        </ul>
      </section>

      <div className="button-row footer-row">
        {hasBlueprintOffice && (
          <button
            title="The rectangle around the selection becomes a blueprint draft"
            onClick={() => {
              const left = Math.min(...buildings.map((t) => t.x));
              const top = Math.min(...buildings.map((t) => t.y));
              const right = Math.max(...buildings.map((t) => t.x + t.size));
              const bottom = Math.max(...buildings.map((t) => t.y + t.size));
              setState({ blueprintDraft: { x: left, y: top, width: right - left, height: bottom - top } });
            }}
          >
            Save as blueprint
          </button>
        )}
        {confirm ? (
          <>
            <button className="danger" onClick={() => void gameClient.demolishSelection()}>
              Demolish all {buildings.length}
            </button>
            <button onClick={() => setConfirm(false)}>Keep</button>
          </>
        ) : (
          <button onClick={() => setConfirm(true)}>Demolish…</button>
        )}
      </div>
    </aside>
  );
}

/** Rounded down to two significant digits: 12,345 → 12,000 */
function niceAmount(amount: number) {
  if (amount < 100) {
    return Math.floor(amount);
  }
  const step = 10 ** (Math.floor(Math.log10(amount)) - 1);
  return Math.floor(amount / step) * step;
}
