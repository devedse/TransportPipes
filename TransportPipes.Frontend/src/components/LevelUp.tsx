import { useEffect } from "react";
import { gameClient } from "../app/client";
import { levelUnlocks } from "../app/roadmap";
import { appStore, setState, useAppStore } from "../state/store";

/** The levels at which a guest is asked to keep the estate (v1.2 §5): the evening it starts to matter */
const KEEP_AT = [3, 6, 10];

/** Closes the celebration; a guest who reached a keep level then sees the keep card */
function close(level: number) {
  const guest = appStore.getState().user?.isGuest === true;
  setState({
    levelUp: null,
    keepOpen: guest && KEEP_AT.includes(level) ? true : appStore.getState().keepOpen,
  });
}

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
    const timer = window.setTimeout(() => close(level), 5200);
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
      <div className="level-up-card" onClick={() => close(level)}>
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
