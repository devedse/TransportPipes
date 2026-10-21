import { useEffect } from "react";
import { gameClient } from "../app/client";
import { levelUnlocks } from "../app/roadmap";
import { setState, useAppStore } from "../state/store";

/** The moment a Company Level is reached: Loady cheers, and what the level brings (GDD §10.1, "visible growth") */
export function LevelUp() {
  const level = useAppStore((s) => s.levelUp);
  const content = useAppStore((s) => s.content);
  const company = useAppStore((s) => s.company);
  const era = useAppStore((s) => s.era);
  useEffect(() => {
    if (level === null) {
      return;
    }
    const timer = window.setTimeout(() => setState({ levelUp: null }), 5200);
    return () => clearTimeout(timer);
  }, [level]);
  if (level === null || content === null || company === null) {
    return null;
  }
  // What the level brings, and whether the estate can use it ("Smelter · you have copper ore")
  const unlocked = levelUnlocks(content, company, gameClient.world, era, level).slice(0, 4);
  return (
    <div className="level-up" role="status">
      <div className="level-up-burst" aria-hidden="true" />
      <img src="/art/ui/loady-cheer.webp" alt="" className="level-up-loady" />
      <div className="level-up-card" onClick={() => setState({ levelUp: null })}>
        <span className="level-up-title">Company Level</span>
        <strong className="level-up-number">{level}</strong>
        {unlocked.length > 0 && (
          <ul className="level-up-unlocks">
            {unlocked.map((item) => (
              <li key={item.name}>
                <strong>{item.name}</strong>
                {item.note !== null && <span> · {item.note}</span>}
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
