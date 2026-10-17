import { useNow } from "../app/hooks";
import { gameClient } from "../app/instance";
import type { ConnectionStatus } from "../api/transportConnection";
import { formatGold, formatGoldPerSecond, goldAt } from "../state/gold";
import { useAppStore } from "../state/store";

const STATUS_LABELS: Record<ConnectionStatus, string> = {
  connecting: "Connecting",
  connected: "Online",
  reconnecting: "Reconnecting",
  disconnected: "Offline",
};

export function Hud() {
  const user = useAppStore((state) => state.user);
  const status = useAppStore((state) => state.connectionStatus);
  const zoom = useAppStore((state) => state.zoom);
  const hoveredTile = useAppStore((state) => state.hoveredTile);
  const helpOpen = useAppStore((state) => state.shortcutHelpOpen);
  const setHelpOpen = useAppStore((state) => state.setShortcutHelpOpen);

  return (
    <>
      <header className="hud">
        <div className="panel hud-brand">
          <span className="hud-title">Transport Pipes</span>
        </div>

        {user !== null && <GoldDisplay />}

        <div className="panel hud-controls">
          <span className={`status-pill status-pill--${status}`}>
            <span className="status-dot" aria-hidden="true" />
            {STATUS_LABELS[status]}
          </span>
          {user !== null && (
            <span className="hud-user" title="Logged in as">
              {user.userName}
            </span>
          )}
          <div className="zoom-controls" role="group" aria-label="Zoom">
            <button
              type="button"
              className="icon-button"
              onClick={() => gameClient.zoomBy(-1)}
              title="Zoom out"
            >
              <img src="/images/zoomout.png" alt="Zoom out" />
            </button>
            <span className="zoom-level">{Math.round(zoom * 100)}%</span>
            <button
              type="button"
              className="icon-button"
              onClick={() => gameClient.zoomBy(1)}
              title="Zoom in"
            >
              <img src="/images/zoomin.png" alt="Zoom in" />
            </button>
          </div>
          <button
            type="button"
            className="icon-button icon-button--text"
            aria-expanded={helpOpen}
            onClick={() => setHelpOpen(!helpOpen)}
            title="Keyboard shortcuts (?)"
          >
            ?
          </button>
        </div>
      </header>
      {hoveredTile !== null && (
        <div className="panel hud-tile" aria-live="off">
          Tile {hoveredTile.x}, {hoveredTile.y}
        </div>
      )}
    </>
  );
}

/** Gold, extrapolated between server updates with the current gold per second. */
function GoldDisplay() {
  const gold = useAppStore((state) => state.gold);
  const now = useNow(100);
  return (
    <div className="panel hud-gold" aria-label="Gold">
      <span className="gold-coin" aria-hidden="true" />
      <span className="gold-label">Gold:</span>
      <span className="gold-amount">{gold === null ? "…" : formatGold(goldAt(gold, now))}</span>
      {gold !== null && (
        <span className="gold-rate">({formatGoldPerSecond(gold.goldPerSecond)} per second)</span>
      )}
    </div>
  );
}
