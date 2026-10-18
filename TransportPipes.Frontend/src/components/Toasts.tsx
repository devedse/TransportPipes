import { dismissToast, useAppStore } from "../state/store";

export function Toasts() {
  const toasts = useAppStore((state) => state.toasts);
  return (
    <div className="toasts" aria-live="polite">
      {toasts.map((t) => (
        <button key={t.id} className={`panel toast ${t.kind}`} onClick={() => dismissToast(t.id)}>
          {t.kind === "level" && <span className="toast-icon">⭐</span>}
          {t.text}
        </button>
      ))}
    </div>
  );
}
