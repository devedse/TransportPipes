import { gameClient } from "../app/client";
import { formatTimeAgo } from "../app/format";
import { useNow } from "../app/useNow";
import { setState, useAppStore } from "../state/store";

const KIND_ICONS: Record<string, string> = {
  levelUp: "⭐",
  researchFinished: "🔬",
  priceChanged: "🏷",
  orderCompleted: "✅",
  contractChanged: "📜",
  exchangeTrade: "📦",
  corporation: "🏢",
  social: "♥",
  storageFull: "🛢",
  neighbour: "🏘",
  guestbook: "✍",
};

/**
 * When an entry came: "just now", "12 min ago", then its day and time on the server's clock ("Tue 20:14"), so entries
 * from different evenings tell apart (after run 4: "all say 'instant ago', no real times")
 */
function when(at: Date, now: number): string {
  const seconds = (now - at.getTime()) / 1000;
  if (seconds < 60) {
    return "just now";
  }
  if (seconds < 3 * 3600) {
    return `${formatTimeAgo(at, new Date(now))} ago`;
  }
  const time = at.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  return at.toDateString() === new Date(now).toDateString()
    ? `today ${time}`
    : `${at.toLocaleDateString([], { weekday: "short" })} ${time}`;
}

/** Notifications: offers, partners' price changes, full storages, likes, ... */
export function InboxPanel() {
  const open = useAppStore((s) => s.inboxOpen);
  const inbox = useAppStore((s) => s.inbox);
  // The server's clock, like every other panel (run 4: "instant ago" on every entry when the device's clock differs)
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow(30000) + serverOffset;
  if (!open) {
    return null;
  }
  return (
    <div className="panel inbox-panel" role="dialog" aria-label="Inbox">
      <div className="panel-header">
        <span className="panel-title">Inbox</span>
        <button className="icon-button" title="Close" onClick={() => setState({ inboxOpen: false })}>
          ×
        </button>
      </div>
      <div className="inbox-list">
        {inbox === null && <p className="muted small">Loading…</p>}
        {inbox?.messages.length === 0 && (
          <p className="muted small">Nothing yet. News about your company lands here.</p>
        )}
        {inbox?.messages.map((m) => (
          <div key={m.id} className={m.read ? "inbox-item" : "inbox-item unread"}>
            <span className="inbox-icon">{KIND_ICONS[m.kind] ?? "•"}</span>
            <span className="inbox-text">{m.text}</span>
            <span className="muted small inbox-time">{when(new Date(m.at), now)}</span>
          </div>
        ))}
      </div>
      {(inbox?.messages.length ?? 0) > 0 && (
        <button className="wide" onClick={() => void gameClient.refreshInbox()}>
          Refresh
        </button>
      )}
    </div>
  );
}
