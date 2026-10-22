import type { Overlay } from "../game/Engine";
import { setState, useAppStore } from "../state/store";

const OVERLAYS: Array<{ id: Overlay; label: string; title: string }> = [
  { id: "none", label: "Map", title: "The normal map with the flows" },
  { id: "deposits", label: "Deposits", title: "Ore, coal and stone in the ground; brighter is richer" },
  { id: "wind", label: "Wind", title: "Wind strength: red is the best spot for wind moilies" },
  { id: "sun", label: "Sun", title: "Sunshine: yellow is the best spot for solar panels" },
  { id: "owners", label: "Owners", title: "Who owns which buildings and pipes" },
];

/** The Wind and Sun overlays' colour scales (the map's: 0.8 to 1.4 gets the whole range), as legends (F10) */
const LEGENDS: Partial<
  Record<Overlay, { from: string; to: string; colors: string; low: string; high: string }>
> = {
  wind: {
    from: "×0.8",
    to: "×1.4",
    colors: "rgb(40, 120, 255), rgb(147, 200, 147), rgb(255, 120, 40)",
    low: "calm",
    high: "windy: moilies make the most",
  },
  sun: {
    from: "×0.8",
    to: "×1.4",
    colors: "rgb(70, 80, 120), rgb(255, 230, 30)",
    low: "shade",
    high: "sunny: panels make the most",
  },
};

/** Switches between the map overlays */
export function OverlayBar() {
  const overlay = useAppStore((state) => state.overlay);
  const legend = LEGENDS[overlay];
  return (
    <div className="panel overlay-bar">
      <div className="overlay-chips" role="radiogroup" aria-label="Map overlay">
        <OverlayChips overlay={overlay} />
      </div>
      {legend !== undefined && (
        <div className="overlay-legend" aria-label={`${overlay} legend`}>
          <span className="small">
            {legend.from} {legend.low}
          </span>
          <span className="legend-bar" style={{ background: `linear-gradient(90deg, ${legend.colors})` }} />
          <span className="small">
            {legend.to} {legend.high}
          </span>
        </div>
      )}
    </div>
  );
}

function OverlayChips({ overlay }: { overlay: Overlay }) {
  return (
    <>
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
    </>
  );
}
