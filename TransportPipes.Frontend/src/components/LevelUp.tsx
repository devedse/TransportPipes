import { useEffect } from "react";
import { setState, useAppStore } from "../state/store";

/** The moment a Company Level is reached: Loady cheers, and what the level brings (GDD §10.1, "visible growth") */
export function LevelUp() {
  const level = useAppStore((s) => s.levelUp);
  const content = useAppStore((s) => s.content);
  useEffect(() => {
    if (level === null) {
      return;
    }
    const timer = window.setTimeout(() => setState({ levelUp: null }), 5200);
    return () => clearTimeout(timer);
  }, [level]);
  if (level === null || content === null) {
    return null;
  }
  const unlocked = content.buildings
    .filter((t) => t.buildable && t.unlock.level === level)
    .map((t) => t.name);
  return (
    <div className="level-up" role="status">
      <div className="level-up-burst" aria-hidden="true" />
      <img src="/art/ui/loady-cheer.webp" alt="" className="level-up-loady" />
      <div className="level-up-card" onClick={() => setState({ levelUp: null })}>
        <span className="level-up-title">Company Level</span>
        <strong className="level-up-number">{level}</strong>
        {unlocked.length > 0 && <span className="level-up-unlocks">New: {unlocked.join(", ")}</span>}
      </div>
    </div>
  );
}
