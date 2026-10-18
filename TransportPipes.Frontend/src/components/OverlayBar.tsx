import type { Overlay } from "../game/Engine";
import { setState, useAppStore } from "../state/store";

const OVERLAYS: Array<{ id: Overlay; label: string; title: string }> = [
  { id: "none", label: "Map", title: "The normal map with the flows" },
  { id: "deposits", label: "Deposits", title: "Ore, coal and stone in the ground; brighter is richer" },
  { id: "wind", label: "Wind", title: "Wind strength: red is the best spot for wind moilies" },
  { id: "sun", label: "Sun", title: "Sunshine: yellow is the best spot for solar panels" },
  { id: "owners", label: "Owners", title: "Who owns which buildings and pipes" },
];

/** Switches between the map overlays */
export function OverlayBar() {
  const overlay = useAppStore((state) => state.overlay);
  return (
    <div className="panel overlay-bar" role="radiogroup" aria-label="Map overlay">
      {OVERLAYS.map((o) => (
        <button
          key={o.id}
          role="radio"
          aria-checked={overlay === o.id}
          className={overlay === o.id ? "chip active" : "chip"}
          title={o.title}
          onClick={() => setState({ overlay: o.id })}
        >
          {o.label}
        </button>
      ))}
    </div>
  );
}
