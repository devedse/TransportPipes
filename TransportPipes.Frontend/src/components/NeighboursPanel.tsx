import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import type { NeighbourDto } from "../api/contracts";
import type { Content } from "../game/content";
import { setState, useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

function Chips({
  rates,
  content,
  kind,
}: {
  rates: NeighbourDto["surplus"];
  content: Content;
  kind: "surplus" | "needs";
}) {
  if (rates.length === 0) {
    return <span className="muted small">—</span>;
  }
  return (
    <span className="chips">
      {rates.slice(0, 4).map((r) => (
        <span
          key={r.resource}
          className={`res-chip ${kind}`}
          style={{ borderColor: content.resourceColor(r.resource) }}
        >
          <ResourceIcon resource={r.resource} /> {formatAmount(r.perMinute)}{" "}
          {content.resourceName(r.resource).toLowerCase()}
        </span>
      ))}
    </span>
  );
}

/**
 * Everyone in your town (the Trades panel's Town tab): what each has spare and needs, what you earn from them and pay
 * them, how far your best pipe to them is; visit them or write to them
 */
export function TownList() {
  const neighbours = useAppStore((s) => s.neighbours);
  const content = useAppStore((s) => s.content);
  if (content === null) {
    return null;
  }
  return (
    <div className="neighbour-list">
      {neighbours.length === 0 && <p className="muted">Nobody else in your town yet.</p>}
      {neighbours.map((n) => (
        <div key={n.id} className="neighbour">
          <span className="company-dot" style={{ background: n.color }} />
          <div className="neighbour-body">
            <div>
              <strong>{n.name}</strong> <span className="muted small">Lv {n.level}</span>{" "}
              {n.connected && <span className="badge connected">connected</span>}
            </div>
            <div className="small">
              <span className="muted">Spare:</span>{" "}
              <Chips rates={n.surplus} content={content} kind="surplus" />
            </div>
            <div className="small">
              <span className="muted">Needs:</span> <Chips rates={n.needs} content={content} kind="needs" />
            </div>
            {(n.youEarn > 0 || n.youPay > 0) && (
              <div className="small">
                {n.youEarn > 0 && (
                  <span className="gold-text">you earn +{formatAmount(n.youEarn)} g/min </span>
                )}
                {n.youPay > 0 && <span className="muted">you pay {formatAmount(n.youPay)} g/min</span>}
              </div>
            )}
            {n.linkEfficiency !== null && n.pipeTiles !== null && (
              // How far they are by pipe, and what that loses (the audit, 2026-10-31: sent, never shown)
              <div className="muted small">
                your best pipe to them: {n.pipeTiles} tiles, {Math.round(n.linkEfficiency * 100)}% of what is
                sent arrives
              </div>
            )}
          </div>
          <div className="neighbour-actions">
            {!n.connected && <span className="muted small">not piped to the Yard yet</span>}
            <span className="button-row">
              <button title="Visit their plot" onClick={() => void gameClient.visitEstate(n.id, true)}>
                Visit
              </button>
              <button title="Send a message" onClick={() => void gameClient.messageCompany(n.id, n.name)}>
                ✉
              </button>
            </span>
          </div>
        </div>
      ))}
    </div>
  );
}

/** The pipe that Connect planned, with its cost: build it or drop it */
export function PlannedRouteBanner() {
  const route = useAppStore((s) => s.plannedRoute);
  const company = useAppStore((s) => s.company);
  const content = useAppStore((s) => s.content);
  const partner = useAppStore((s) => s.neighbours.find((t) => t.id === s.plannedPartner));
  const target = useAppStore((s) => s.plannedTarget);
  if (route === null || company === null || content === null || gameClient.world === null) {
    return null;
  }
  const tiles = route.filter((t) => gameClient.world?.conductorAt(t.x, t.y)?.owner !== company.id);
  const gold = tiles.length * (content.pipeTiers[0]?.gold ?? 2);
  return (
    <div className="panel route-banner">
      <span>
        Pipe to {partner?.name ?? target ?? "your neighbour"}:{" "}
        {tiles.length === 1 ? "1 tile" : `${tiles.length} tiles`} · {formatAmount(gold)} g
      </span>
      <button className="primary" onClick={() => void gameClient.buildPlannedRoute()}>
        Lay it · {formatAmount(gold)} g
      </button>
      <button
        onClick={() =>
          setState({
            plannedRoute: null,
            plannedPartner: null,
            plannedTarget: null,
            plannedDone: null,
            hintTile: null,
          })
        }
      >
        Cancel
      </button>
    </div>
  );
}
