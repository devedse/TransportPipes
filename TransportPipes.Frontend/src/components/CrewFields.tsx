import { CREW_COLORS, EMBLEM_ICONS, emblemIcon } from "../app/crews";
import { bannerBackground } from "../app/banner";

export function Swatches({
  value,
  onChange,
  colors,
  label = "Colour",
}: {
  value: string;
  onChange: (c: string) => void;
  colors: string[];
  label?: string;
}) {
  return (
    <div className="swatches" role="radiogroup" aria-label={label}>
      {colors.map((c) => (
        <button
          key={c}
          type="button"
          role="radio"
          aria-checked={c === value}
          aria-label={c}
          className={c === value ? "swatch active" : "swatch"}
          style={{ background: c }}
          onClick={() => onChange(c)}
        />
      ))}
    </div>
  );
}

/** A crew's name, emblem and two colours (the Yard's name is its crew's) */
export function CrewFields({
  name,
  emblem,
  colors,
  onChange,
  label = "Crew name",
}: {
  name: string;
  emblem: string;
  colors: [string, string];
  onChange: (name: string, emblem: string, colors: [string, string]) => void;
  label?: string;
}) {
  return (
    <fieldset className="crew-fields">
      <label>
        {label}
        <input
          value={name}
          onChange={(e) => onChange(e.target.value, emblem, colors)}
          required
          minLength={3}
          maxLength={24}
        />
      </label>
      <details className="crew-looks">
        <summary className="small">Emblem and colours (later is fine)</summary>
        <div className="emblems" role="radiogroup" aria-label="Emblem">
          {Object.keys(EMBLEM_ICONS).map((id) => (
            <button
              key={id}
              type="button"
              role="radio"
              aria-checked={id === emblem}
              title={id}
              className={id === emblem ? "emblem active" : "emblem"}
              onClick={() => onChange(name, id, colors)}
            >
              {EMBLEM_ICONS[id]}
            </button>
          ))}
        </div>
        <div className="crew-colors">
          <span className="muted small">The banner&apos;s left half</span>
          <Swatches
            value={colors[0]}
            onChange={(c) => onChange(name, emblem, [c, colors[1]])}
            colors={CREW_COLORS}
            label="The banner's colour"
          />
          <span className="muted small">and its right half</span>
          <Swatches
            value={colors[1]}
            onChange={(c) => onChange(name, emblem, [colors[0], c])}
            colors={CREW_COLORS}
            label="Its sash"
          />
        </div>
      </details>
      <div className="crew-banner" style={{ background: bannerBackground(colors[0]!, colors[1]) }}>
        <span>{emblemIcon(emblem)}</span> <strong>{name || "Your crew"}</strong>
      </div>
    </fieldset>
  );
}
