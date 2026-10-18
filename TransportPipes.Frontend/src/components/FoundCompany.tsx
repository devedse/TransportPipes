import { useState, type FormEvent } from "react";
import { gameClient } from "../app/client";
import { useAppStore } from "../state/store";

const COLORS = [
  "#e74c3c",
  "#e67e22",
  "#f1c40f",
  "#2ecc71",
  "#1abc9c",
  "#3498db",
  "#9b59b6",
  "#e84393",
  "#ecf0f1",
  "#95a5a6",
];

/** A different suggestion for every player */
const SUGGESTED_COLOR = COLORS[Math.floor(Math.random() * COLORS.length)]!;

/** The first thing a new player does: name a company and pick its colour */
export function FoundCompany() {
  const user = useAppStore((state) => state.user);
  const [name, setName] = useState(user !== null ? `${user.userName} Pipes` : "");
  const [color, setColor] = useState(SUGGESTED_COLOR);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(await gameClient.foundCompany(name, color).catch((reason: unknown) => String(reason)));
    setBusy(false);
  };

  return (
    <div className="screen found-screen">
      <form className="panel found-panel" onSubmit={(event) => void submit(event)}>
        <h2>Found your company</h2>
        <p className="muted">
          You get an estate of your own in the district, 2,000 gold, a headquarters and a trading post. Name
          your company and pick its colour: your neighbours will see both.
        </p>
        <label>
          Company name
          <input
            autoFocus
            value={name}
            onChange={(event) => setName(event.target.value)}
            required
            minLength={3}
            maxLength={24}
          />
        </label>
        <div className="swatches" role="radiogroup" aria-label="Colour">
          {COLORS.map((c) => (
            <button
              key={c}
              type="button"
              role="radio"
              aria-checked={c === color}
              aria-label={c}
              className={c === color ? "swatch active" : "swatch"}
              style={{ background: c }}
              onClick={() => setColor(c)}
            />
          ))}
        </div>
        <div className="company-preview">
          <span className="company-dot" style={{ background: color }} />
          <strong>{name || "Your company"}</strong>
        </div>
        {error !== null && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}
        <button className="primary" type="submit" disabled={busy}>
          {busy ? "Founding…" : "Found the company"}
        </button>
      </form>
    </div>
  );
}
