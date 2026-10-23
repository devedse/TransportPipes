import { useEffect } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import { setState, useAppStore } from "../state/store";

/**
 * What your gold can do (after run 4: the players spent 0.2% of what they earned, and five offers of gold in the group
 * chat had no door): at most three lines, shared first: the shared goal, a want gold can fill, and "Grow everything that
 * pays" with one preview, one tap and an hour's Undo
 */
export function GoldPanel({ days, onClose }: { days: number; onClose: () => void }) {
  const uses = useAppStore((s) => s.goldUses);
  const gold = useAppStore((s) => s.company?.gold ?? 0);
  useEffect(() => {
    void gameClient.loadGoldUses();
    return () => setState({ goldUses: null });
  }, []);
  const now = useNow(30000) + useAppStore((s) => s.serverOffsetMs);
  const undoLeft = uses?.grow?.undoUntil != null ? (new Date(uses.grow.undoUntil).getTime() - now) / 1000 : 0;
  const empty =
    uses !== null && uses.shared === null && uses.want === null && (uses.grow?.parts.length ?? 0) === 0;
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
      {uses?.shared != null && uses.shared.action === "storey" && uses.shared.allCost !== null && (
        // The station's storey: hours of your income, as often as you like (after run 4)
        <div className="gold-use shared">
          <span>🏗 {uses.shared.text}</span>
          <span className="gold-use-buttons">
            {[
              { hours: 1, label: "an hour" },
              { hours: 6, label: "6 hours" },
              { hours: 24, label: "a day" },
            ].map((t) => {
              const cost = Math.min(t.hours * uses.hour, uses.shared!.allCost!);
              return (
                <button
                  key={t.hours}
                  className="small"
                  disabled={gold < cost}
                  onClick={() => void gameClient.giveToStorey(t.hours)}
                >
                  Give {t.label} · {formatAmount(cost)} g
                </button>
              );
            })}
          </span>
        </div>
      )}
      {uses?.shared != null && uses.shared.action === "wonder" && (
        <div className="gold-use shared">
          <span>🏛 {uses.shared.text}</span>
          <span className="gold-use-buttons">
            {uses.shared.cost !== null && (
              <button
                className="small"
                disabled={gold < uses.shared.cost}
                title="As much as a day of your income buys, at twice the price; it counts a quarter for your tier"
                onClick={() => void gameClient.orderForWonder(uses.shared!.resource!, uses.shared!.cost)}
              >
                Order a day&apos;s worth · {formatAmount(uses.shared.cost)} g
              </button>
            )}
            {uses.shared.allCost !== null && (
              <button
                className="small"
                disabled={gold < uses.shared.allCost}
                onClick={() => void gameClient.orderForWonder(uses.shared!.resource!)}
              >
                Order all of it · {formatAmount(uses.shared.allCost)} g
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
                .orderRest(uses.want!.target!, uses.want!.forName)
                .then(() => gameClient.loadGoldUses())
            }
          >
            {uses.want.forName !== null ? `Order it for ${uses.want.forName}` : "Order the rest"} ·{" "}
            {formatAmount(uses.want.cost)} g
          </button>
        </div>
      )}
      {uses?.grow != null && uses.grow.parts.length > 0 && (
        <div className="gold-use grow">
          <span>
            💡 <strong>Grow everything that pays</strong>: about +{formatAmount(uses.grow.gain)} g/min for{" "}
            {formatAmount(uses.grow.cost)} g (
            {Math.max(1, Math.round((100 * uses.grow.cost) / Math.max(1, gold)))}% of your gold), each part
            back within a week
          </span>
          <details>
            <summary className="small">
              {uses.grow.parts.length} {uses.grow.parts.length === 1 ? "building" : "buildings"}
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
      {undoLeft > 0 && (
        <p className="small">
          <button className="small" onClick={() => void gameClient.undoGrowth()}>
            Undo the last Grow
          </button>{" "}
          <span className="muted">
            for {formatDuration(undoLeft)} more: the levels back, less what they earned since
          </span>
        </p>
      )}
      {empty && (
        <p className="small">
          Nothing pays back within a week now: your buildings are at what their pipes and inputs carry, and
          nobody near waits for anything gold can bring. New chains, parcels and your station&apos;s next goal
          are in Next steps.
        </p>
      )}
      {(uses?.shared != null || uses?.want != null) && (
        <p className="muted small">
          {uses?.shared?.action === "wonder" && (
            <button className="link small" onClick={() => void gameClient.openHub("wonders")}>
              Hub › Wonders
            </button>
          )}
          {uses?.shared?.action === "storey" && (
            <button className="link small" onClick={() => void gameClient.openProject()}>
              The station&apos;s project
            </button>
          )}
        </p>
      )}
    </div>
  );
}
