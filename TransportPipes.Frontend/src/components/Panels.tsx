import { useEffect } from "react";
import { SHORTCUT_HELP } from "../app/shortcuts";
import { useAppStore, type Toast } from "../state/store";

/** The server's status report for a tile (key I). */
export function InspectorPanel() {
  const inspector = useAppStore((state) => state.inspector);
  const close = useAppStore((state) => state.setInspector);
  if (inspector === null) {
    return null;
  }
  return (
    <aside className="panel inspector" aria-label="Inspector">
      <div className="panel-header">
        <h2 className="panel-title">
          Tile {inspector.tile.x}, {inspector.tile.y}
        </h2>
        <button
          type="button"
          className="icon-button icon-button--text"
          onClick={() => close(null)}
          aria-label="Close"
        >
          ×
        </button>
      </div>
      <pre className="inspector-text">{inspector.text}</pre>
    </aside>
  );
}

export function ShortcutHelp() {
  const open = useAppStore((state) => state.shortcutHelpOpen);
  const setOpen = useAppStore((state) => state.setShortcutHelpOpen);
  if (!open) {
    return null;
  }
  return (
    <aside className="panel shortcut-help" aria-label="Keyboard shortcuts">
      <div className="panel-header">
        <h2 className="panel-title">Keyboard shortcuts</h2>
        <button
          type="button"
          className="icon-button icon-button--text"
          onClick={() => setOpen(false)}
          aria-label="Close"
        >
          ×
        </button>
      </div>
      <dl className="shortcut-list">
        {SHORTCUT_HELP.map((shortcut) => (
          <div key={shortcut.keys} className="shortcut-row">
            <dt>
              <kbd>{shortcut.keys}</kbd>
            </dt>
            <dd>{shortcut.description}</dd>
          </div>
        ))}
      </dl>
    </aside>
  );
}

export function Toasts() {
  const toasts = useAppStore((state) => state.toasts);
  return (
    <div className="toasts" aria-live="polite">
      {toasts.map((toast) => (
        <ToastItem key={toast.id} toast={toast} />
      ))}
    </div>
  );
}

function ToastItem({ toast }: { toast: Toast }) {
  const dismiss = useAppStore((state) => state.dismissToast);
  useEffect(() => {
    if (toast.durationMs === null) {
      return;
    }
    const id = window.setTimeout(() => dismiss(toast.id), toast.durationMs);
    return () => window.clearTimeout(id);
  }, [toast, dismiss]);

  return (
    <div className={`toast toast--${toast.level}`} role={toast.level === "error" ? "alert" : "status"}>
      <p className="toast-message">{toast.message}</p>
      <button
        type="button"
        className="icon-button icon-button--text"
        onClick={() => dismiss(toast.id)}
        aria-label="Dismiss"
      >
        ×
      </button>
    </div>
  );
}

/** Full overlay until the first connection; a small banner while reconnecting afterwards. */
export function ConnectionOverlay() {
  const status = useAppStore((state) => state.connectionStatus);
  const everConnected = useAppStore((state) => state.everConnected);

  if (!everConnected) {
    return (
      <div className="connection-overlay" role="status">
        <div className="panel connection-card">
          <span className="spinner" aria-hidden="true" />
          <h2>Connecting…</h2>
          <p className="muted">Waiting for the Transport Pipes server.</p>
        </div>
      </div>
    );
  }
  if (status !== "connected") {
    return (
      <div className="connection-banner" role="status">
        <span className="spinner spinner--small" aria-hidden="true" />
        Connection lost, reconnecting…
      </div>
    );
  }
  return null;
}
