import { dismissToast, useAppStore } from "../state/store";

export function Toasts() {
  const toasts = useAppStore((state) => state.toasts);
  return (
    <div className="toasts" aria-live="polite">
      {toasts.map((t) => (
        <button key={t.id} className={`panel toast ${t.kind}`} onClick={() => dismissToast(t.id)}>
          {t.kind === "level" && <span className="toast-icon">⭐</span>}
          {t.text}
          {(t.count ?? 1) > 1 && <span className="toast-count">×{t.count}</span>}
        </button>
      ))}
    </div>
  );
}
