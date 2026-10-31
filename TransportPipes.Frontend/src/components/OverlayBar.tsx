import type { Overlay } from "../game/Engine";
import { setState, useAppStore } from "../state/store";
import { HoverPopup } from "./HoverPopup";

const OVERLAYS: Array<{ id: Overlay; label: string; title: string }> = [
  { id: "none", label: "Map", title: "The normal map: your buildings, pipes and the goods flowing in them" },
  {
    id: "deposits",
    label: "Deposits",
    title:
      "Ore, coal and stone in the ground, where a mine or a quarry can dig them; the brighter, the richer (more stars: more a minute)",
  },
  {
    id: "wind",
    label: "Wind",
    title: "How strong the wind is: red is the best spot for a Wind Moily, which makes more energy there",
  },
  {
    id: "sun",
    label: "Sun",
    title:
      "How much sun each spot gets: yellow is the best spot for a Solar Panel, which makes more energy there",
  },
  { id: "owners", label: "Owners", title: "Who owns which buildings and pipes, in each company's colour" },
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
      {overlay === "deposits" && (
        // After run 4: "the map is full of forest around my plot but Lumber Camp says no wood on my land"
        <div className="overlay-legend small" aria-label="deposits legend">
          Coloured: what you can mine or cut, brighter is richer · green-grey trees are scenery, no wood
        </div>
      )}
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
        <HoverPopup
          key={o.id}
          width={260}
          popup={
            <>
              <strong>{o.label}</strong>
              <span className="goods-tip-line">{o.title}</span>
            </>
          }
        >
          <button
            role="radio"
            aria-checked={overlay === o.id}
            className={overlay === o.id ? "chip active" : "chip"}
            onClick={() => setState({ overlay: o.id })}
          >
            {o.label}
          </button>
        </HoverPopup>
      ))}
    </>
  );
}
