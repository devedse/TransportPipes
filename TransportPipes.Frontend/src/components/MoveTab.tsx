import { useEffect, useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { DistrictChoiceDto } from "../api/contracts";
import { useAppStore } from "../state/store";

const ACTIVITY: Record<string, string> = {
  active: "Active",
  resting: "Resting (3+ days away): everything keeps running",
  dormant: "Dormant (14+ days away): a Caretaker trades for you for 10%",
  asleep: "Asleep on the Quiet Isles",
};

/** Moving the estate to another district (GDD §12.3), and the crates of buildings that didn't fit */
export function MoveTab() {
  const company = useAppStore((s) => s.company)!;
  const content = useAppStore((s) => s.content)!;
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow(1000) + serverOffset;
  const [districts, setDistricts] = useState<DistrictChoiceDto[] | null>(null);
  useEffect(() => {
    void gameClient.connection.getDistricts().then(setDistricts);
  }, []);
  const moving = company.moving;
  const waitMs = moving.canMoveAt === null ? 0 : new Date(moving.canMoveAt).getTime() - now;
  return (
    <div className="move-tab">
      <p className="small">
        <strong>{ACTIVITY[moving.activity] ?? moving.activity}.</strong> You can move your whole estate to
        another district once a week: it lifts off on balloons and lands an hour later with its layout.
        Buildings that don&apos;t fit the new land wait in crates.
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
          <div className="district-list">
            {districts === null && <p className="muted small">Loading…</p>}
            {districts?.map((d) => (
              <div key={`${d.x},${d.y}`} className={d.yours ? "district-row yours" : "district-row"}>
                <div>
                  <strong>{d.name}</strong>
                  {d.reservedForYou && <span className="badge">your corporation&apos;s block</span>}
                  <div className="muted small">
                    {d.players} players · {Math.round(d.activeShare * 100)}% active · {d.freeEstates} free
                    estates
                  </div>
                </div>
                {d.yours ? (
                  <span className="muted small">You live here</span>
                ) : (
                  <button
                    disabled={d.freeEstates === 0 || waitMs > 0 || company.gold < moving.moveCost}
                    onClick={() => void gameClient.relocate(d.x, d.y)}
                  >
                    Move here
                  </button>
                )}
              </div>
            ))}
          </div>
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
