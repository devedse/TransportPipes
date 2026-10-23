import { usePhone } from "../app/usePhone";
import { dismissToast, useAppStore } from "../state/store";

/** What just happened, while you watch; on a phone one at a time (F10), the newest, with how many wait */
export function Toasts() {
  const all = useAppStore((state) => state.toasts);
  const phone = usePhone();
  const toasts = phone ? all.slice(-1) : all;
  const waiting = all.length - toasts.length;
  return (
    <div className="toasts" aria-live="polite">
      {toasts.map((t) => (
        <button key={t.id} className={`panel toast ${t.kind}`} onClick={() => dismissToast(t.id)}>
          {t.kind === "level" && <span className="toast-icon">⭐</span>}
          {t.text}
          {(t.count ?? 1) > 1 && <span className="toast-count">×{t.count}</span>}
          {waiting > 0 && <span className="toast-count">+{waiting}</span>}
        </button>
      ))}
    </div>
  );
}
