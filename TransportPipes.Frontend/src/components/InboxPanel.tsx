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
            <span className="muted small inbox-time">{formatTimeAgo(new Date(m.at), new Date(now))} ago</span>
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
