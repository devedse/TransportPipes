import { useState, type FormEvent } from "react";
import { gameClient } from "../app/client";
import { COLORS, CREW_COLORS, EMBLEM_ICONS, emblemIcon } from "../app/crews";
import type { SeatRefDto } from "../api/contracts";
import { useAppStore } from "../state/store";
import { InviteScreen } from "./InviteScreen";
import { StationPicker, type PickMode } from "./StationPicker";

/** A different suggestion for every player */
const SUGGESTED_COLOR = COLORS[Math.floor(Math.random() * COLORS.length)]!;

const WAYS: Array<{ mode: PickMode; title: string; text: string }> = [
  {
    mode: "neighbours",
    title: "Find me neighbours",
    text: "A seat next to players who are around, so you trade with people from the first evening.",
  },
  {
    mode: "friends",
    title: "Start with friends",
    text: "Found a crew, pick a station for all of you, and send each friend the code of their seat.",
  },
  {
    mode: "own",
    title: "A station of my own",
    text: "A station nobody lives in yet: NPC Houses are your neighbours until someone moves in.",
  },
];

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

/** A crew's name, emblem and two colours */
export function CrewFields({
  name,
  emblem,
  colors,
  onChange,
}: {
  name: string;
  emblem: string;
  colors: [string, string];
  onChange: (name: string, emblem: string, colors: [string, string]) => void;
}) {
  return (
    <fieldset className="crew-fields">
      <label>
        Crew name
        <input
          value={name}
          onChange={(e) => onChange(e.target.value, emblem, colors)}
          required
          minLength={3}
          maxLength={24}
        />
      </label>
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
          label="The banner's left half"
        />
        <span className="muted small">and its right half</span>
        <Swatches
          value={colors[1]}
          onChange={(c) => onChange(name, emblem, [colors[0], c])}
          colors={CREW_COLORS}
          label="The banner's right half"
        />
      </div>
      <div
        className="crew-banner"
        style={{ background: `linear-gradient(90deg, ${colors[0]} 50%, ${colors[1]} 50%)` }}
      >
        <span>{emblemIcon(emblem)}</span> <strong>{name || "Your crew"}</strong>
      </div>
    </fieldset>
  );
}

/**
 * Where a new player starts (v1.2 §4): three ways (next to people, with friends, on a station of one's own), a seat
 * on the station picker, and a name and colour for the company. With a friend's code: the invite screen.
 */
export function FoundCompany() {
  const user = useAppStore((state) => state.user);
  const invite = useAppStore((state) => state.invite);
  const [mode, setMode] = useState<PickMode | null>(null);
  const [seat, setSeat] = useState<SeatRefDto | null>(null);
  const [name, setName] = useState(user !== null && !user.isGuest ? `${user.userName} Pipes` : "");
  const [color, setColor] = useState(SUGGESTED_COLOR);
  const [crew, setCrew] = useState<{ name: string; emblem: string; colors: [string, string] }>({
    name: "",
    emblem: "kettle",
    colors: [CREW_COLORS[0]!, CREW_COLORS[1]!],
  });
  const [friends, setFriends] = useState(3);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  // A refused seat: the picker loads the stations again and says where the next free seat is (F7)
  const [reload, setReload] = useState(0);
  const [moved, setMoved] = useState<string | null>(null);
  const nameShort = name.trim().length < 3;

  if (invite !== null) {
    return <InviteScreen />;
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    try {
      const failure =
        mode === "friends" && seat !== null
          ? await gameClient.startWithFriends(
              name,
              color,
              crew.name,
              crew.emblem,
              crew.colors[0],
              crew.colors[1],
              seat,
              friends,
            )
          : await gameClient.foundCompany(name, color, seat);
      setMoved(null);
      if (failure?.code === "seatTaken") {
        setError(null);
        setReload(reload + 1);
      } else {
        setError(failure?.error ?? null);
      }
    } catch (reason) {
      setError(String(reason));
    } finally {
      setBusy(false);
    }
  };

  if (mode === null) {
    return (
      <div className="screen found-screen">
        <div className="panel found-panel start-ways">
          <h2>Where do you want to live?</h2>
          <p className="muted">
            Your estate is part of a station: four seats around a shared Yard where neighbours buy each
            other&apos;s spare goods. You can move later.
          </p>
          {WAYS.map((way) => (
            <button
              key={way.mode}
              type="button"
              className="way"
              onClick={() => {
                // A seat chosen for another way of starting doesn't carry over
                setSeat(null);
                setMode(way.mode);
              }}
            >
              <strong>{way.title}</strong>
              <span className="muted small">{way.text}</span>
            </button>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="screen found-screen">
      <form className="panel found-panel wide" onSubmit={(event) => void submit(event)}>
        <div className="panel-header">
          <h2>{WAYS.find((t) => t.mode === mode)?.title}</h2>
          <button type="button" className="link" onClick={() => setMode(null)}>
            ← another way
          </button>
        </div>
        <StationPicker mode={mode} value={seat} onChange={setSeat} reload={reload} onMoved={setMoved} />
        {moved !== null && (
          <p className="picker-moved" role="status">
            {moved}
          </p>
        )}
        {mode === "friends" && (
          <>
            <CrewFields
              name={crew.name}
              emblem={crew.emblem}
              colors={crew.colors}
              onChange={(n, e, c) => setCrew({ name: n, emblem: e, colors: c })}
            />
            <label>
              Seats to keep for friends
              <select value={friends} onChange={(e) => setFriends(Number(e.target.value))}>
                {[1, 2, 3].map((n) => (
                  <option key={n} value={n}>
                    {n}
                  </option>
                ))}
              </select>
            </label>
          </>
        )}
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
        {nameShort && <p className="muted small">Give your company a name first (3 letters or more).</p>}
        <Swatches value={color} onChange={setColor} colors={COLORS} />
        <div className="company-preview">
          <span className="company-dot" style={{ background: color }} />
          <strong>{name || "Your company"}</strong>
        </div>
        {error !== null && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}
        <button className="primary" type="submit" disabled={busy || seat === null || nameShort}>
          {busy ? "Settling…" : mode === "friends" ? "Settle and invite my friends" : "Settle here"}
        </button>
      </form>
    </div>
  );
}
