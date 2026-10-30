import { useEffect, useState } from "react";
import { useAppStore } from "../state/store";

const RELOAD_AFTER_SECONDS = 5;

/** A new version of the game is out: everything greys out and the page reloads (now, or in a few seconds) */
export function UpdateOverlay() {
  const updateAvailable = useAppStore((state) => state.updateAvailable);
  const [left, setLeft] = useState(RELOAD_AFTER_SECONDS);

  useEffect(() => {
    if (!updateAvailable) {
      return;
    }
    const timer = window.setInterval(() => {
      setLeft((seconds) => {
        if (seconds <= 1) {
          window.location.reload();
          return 0;
        }
        return seconds - 1;
      });
    }, 1000);
    return () => window.clearInterval(timer);
  }, [updateAvailable]);

  if (!updateAvailable) {
    return null;
  }
  return (
    <div className="update-overlay" role="alertdialog" aria-modal="true" aria-label="New update">
      <div className="panel update-card">
        <img src="/art/ui/loady-cheer.webp" alt="" className="update-loady" />
        <h2>New update!</h2>
        <p>The game has been updated. Reloading in {left} s… then you see what&apos;s new.</p>
        <button className="primary" onClick={() => window.location.reload()} autoFocus>
          Reload now
        </button>
      </div>
    </div>
  );
}
