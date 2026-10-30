import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import { useAppStore } from "../state/store";

const ACTIVITY: Record<string, string> = {
  active: "Active",
  resting: "Resting (3+ days away): everything keeps running",
  dormant: "Dormant (14+ days away): a Caretaker keeps your trades going, free",
};

/** Moving your plot to another one (1.4 §6), picked on the map, and the crates of buildings that didn't fit */
export function MoveTab() {
  const company = useAppStore((s) => s.company)!;
  const content = useAppStore((s) => s.content)!;
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow(1000) + serverOffset;
  const moving = company.moving;
  const waitMs = moving.canMoveAt === null ? 0 : new Date(moving.canMoveAt).getTime() - now;
  return (
    <div className="move-tab">
      <p className="small">
        <strong>{ACTIVITY[moving.activity] ?? moving.activity}.</strong> Your whole plot can move to another
        one: it lifts off on balloons and lands with its layout, turned to face its new Yard, next to new
        neighbours. Buildings that don&apos;t fit the new land wait in crates.
      </p>
      {moving.arrivesAt !== null ? (
        <p className="status">
          🎈 On its way: lands in{" "}
          {formatDuration(Math.max(0, (new Date(moving.arrivesAt).getTime() - now) / 1000))}
        </p>
      ) : (
        <>
          <p className="muted small">
            A move costs{" "}
            {moving.freeMoves > 0
              ? `nothing (${moving.freeMoves} free)`
              : `${formatAmount(moving.moveCost)} gold`}
            {waitMs > 0 && ` · next move in ${formatDuration(waitMs / 1000)}`}
          </p>
          <button className="primary" disabled={waitMs > 0} onClick={() => gameClient.openPicker()}>
            Choose a plot on the map
          </button>
        </>
      )}
      {moving.crates.length > 0 && (
        <>
          <div className="panel-title">Crates</div>
          <div className="crate-list">
            {moving.crates.map((crate) => (
              <div key={crate.index} className="crate">
                <span>
                  📦 {content.building(crate.type)?.name ?? crate.type} · level {crate.level}
                </span>
                <span className="button-row">
                  <button className="primary" onClick={() => gameClient.placeCrate(crate.index, crate.type)}>
                    Place
                  </button>
                  <button onClick={() => void gameClient.scrapCrate(crate.index)}>
                    Scrap for {formatAmount((content.building(crate.type)?.cost.gold ?? 0) * 0.75)} g
                  </button>
                </span>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
