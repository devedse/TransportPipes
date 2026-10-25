import { useEffect, useRef, useState, type FormEvent } from "react";
import { gameClient } from "../app/client";
import { COLORS, SPECIALTIES, andList, emblemIcon, goodWord, landIcon, lived, nameIdea } from "../app/crews";
import { formatAmount, formatDuration } from "../app/format";
import { chooseSeat, findSeat, pickable, sameSeat, type PickMode } from "../app/seats";
import { useNow } from "../app/useNow";
import type {
  FoundPlayerDto,
  SeatDto,
  SeatRefDto,
  SeatState,
  StationDto,
  StationsDto,
} from "../api/contracts";
import { sideOf } from "../game/layout";
import { appStore, seatKey, setState, useAppStore } from "../state/store";
import { Swatches } from "./CrewFields";
import { ResourceIcon } from "./ResourceIcon";

/** A different colour suggestion for every player */
const SUGGESTED_COLOR = COLORS[Math.floor(Math.random() * COLORS.length)]!;

/** Every plot of the town's Yards as the map shows it */
function seatsOf(dto: StationsDto): Record<string, SeatState> {
  const seats: Record<string, SeatState> = {};
  for (const station of dto.stations) {
    for (const seat of station.seats) {
      seats[seatKey({ districtX: station.districtX, districtY: station.districtY, slot: seat.slot })] =
        seat.state;
    }
  }
  return seats;
}

/** A Yard's banner: its crew's two colours, or the plain stone of a Yard nobody lives at */
function Banner({ station }: { station: StationDto }) {
  const lived = station.emblem !== null;
  return (
    <div
      className={lived ? "crew-banner" : "crew-banner empty"}
      style={
        lived
          ? {
              background: `linear-gradient(90deg, ${station.color} 50%, ${station.color2 ?? station.color} 50%)`,
            }
          : undefined
      }
    >
      {lived && <span>{emblemIcon(station.emblem)}</span>} <strong>{station.name}</strong>
      <span className="small"> · {station.open ? "open" : "🔒 closed"}</span>
    </div>
  );
}

/** What a plot's land has, lacks and is strong in (1.4 §5) */
function Land({ seat }: { seat: SeatDto }) {
  const content = useAppStore((s) => s.content)!;
  const era = useAppStore((s) => s.era);
  const goods = seat.goods ?? [];
  const now = goods.filter((t) => (content.resource(t)?.era ?? 1) <= era);
  const later = goods.filter((t) => (content.resource(t)?.era ?? 1) > era);
  const lacks = seat.lacks ?? [];
  const specialty = SPECIALTIES[seat.primary];
  return (
    <div className="plot-land">
      {seat.strength !== null && (
        <p className="plot-strength">
          <span className="icon">{landIcon(seat.primary)}</span>{" "}
          <strong>{specialty?.name ?? seat.primary}</strong>
          <span className="muted small"> · a rich ★4 deposit of {goodWord(seat.strength)} by your start</span>
        </p>
      )}
      <p className="plot-goods">
        <span className="muted small">Has</span>
        {now.map((t) => (
          <span key={t} className="plot-good" title={content.resourceName(t)}>
            <ResourceIcon resource={t} /> {goodWord(t)}
          </span>
        ))}
        <span className="plot-good" title="Wind and sun on every plot: hills are windier, deserts sunnier">
          🌬 wind · ☀ sun
        </span>
      </p>
      {later.length > 0 && (
        <p className="plot-goods later">
          <span className="muted small">Later</span>
          {later.map((t) => (
            <span key={t} className="plot-good" title={`Wakes with Era ${content.resource(t)?.era ?? 2}`}>
              <ResourceIcon resource={t} /> {goodWord(t)}
            </span>
          ))}
        </p>
      )}
      {lacks.length > 0 && (
        <p className="plot-goods lacks">
          <span className="muted small">Lacks</span>
          {lacks.map((t) => (
            <span key={t} className="plot-good" title={content.resourceName(t)}>
              <ResourceIcon resource={t} /> {goodWord(t)}
            </span>
          ))}
          <span className="muted small">
            your neighbours or the market bring {lacks.length === 1 ? "it" : "them"}
          </span>
        </p>
      )}
    </div>
  );
}

/** Who lives around the Yard: each plot with its company, or free with its land */
function Residents({ station, chosen }: { station: StationDto; chosen: SeatRefDto | null }) {
  return (
    <ul className="plot-residents">
      {station.seats.map((seat) => {
        const ref = { districtX: station.districtX, districtY: station.districtY, slot: seat.slot };
        const mine = sameSeat(ref, chosen);
        return (
          <li key={seat.slot} className={mine ? "chosen" : undefined}>
            <span className="muted small side">{sideOf(seat.slot)}</span>
            {lived(seat) ? (
              <span>
                <span className="dot" style={{ background: seat.ownerColor ?? "#999" }} />{" "}
                <strong>{seat.state === "you" ? "You" : seat.ownerName}</strong>
                <span className="muted small">
                  {" "}
                  · Lv {seat.ownerLevel}
                  {seat.activity !== null && ` · ${seat.activity}`}
                </span>
              </span>
            ) : (
              <button
                type="button"
                className="link"
                onClick={() => {
                  const picker = appStore.getState().picker;
                  if (picker !== null) {
                    setState({ picker: { ...picker, chosen: ref } });
                  }
                }}
              >
                {mine ? "this plot" : seat.state === "closed" ? "free · ask first" : "free"}
                <span className="muted small"> · {SPECIALTIES[seat.primary]?.name ?? seat.primary}</span>
              </button>
            )}
          </li>
        );
      })}
    </ul>
  );
}

/** Find a player (1.4 §6): a name, and the map flies to them */
function FindPlayer({ onFound }: { onFound: (player: FoundPlayerDto) => void }) {
  const [text, setText] = useState("");
  const [found, setFound] = useState<FoundPlayerDto[] | null>(null);
  const search = async (event: FormEvent) => {
    event.preventDefault();
    setFound(await gameClient.connection.findPlayer(text).catch(() => []));
  };
  return (
    <form className="find-player" onSubmit={(e) => void search(e)}>
      <input
        aria-label="A player's name"
        placeholder="Find a player…"
        value={text}
        onChange={(e) => {
          setText(e.target.value);
          setFound(null);
        }}
      />
      <button type="submit" disabled={text.trim().length < 2}>
        Find
      </button>
      {found !== null && (
        <div className="find-results">
          {found.length === 0 && <span className="muted small">Nobody of that name.</span>}
          {found.map((t) => (
            <button
              key={t.id}
              type="button"
              className="small"
              onClick={() => {
                onFound(t);
                setFound(null);
                setText("");
              }}
            >
              <span className="dot" style={{ background: t.color }} /> {t.name}
              <span className="muted"> · {t.yard ?? t.district}</span>
            </button>
          ))}
        </div>
      )}
    </form>
  );
}

/** A friend's link or code pasted in: the map opens at their Yard */
function FriendLink() {
  const [open, setOpen] = useState(false);
  const [text, setText] = useState("");
  const [error, setError] = useState<string | null>(null);
  if (!open) {
    return (
      <button type="button" className="link small" onClick={() => setOpen(true)}>
        A friend sent me a link
      </button>
    );
  }
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError(await gameClient.openInviteCode(text));
  };
  return (
    <form className="find-player" onSubmit={(e) => void submit(e)}>
      <input
        aria-label="Your friend's link or code"
        placeholder="Paste the link or code"
        value={text}
        autoFocus
        onChange={(e) => setText(e.target.value)}
      />
      <button type="submit" disabled={text.trim() === ""}>
        Open
      </button>
      {error !== null && (
        <span className="form-error small" role="alert">
          {error}
        </span>
      )}
    </form>
  );
}

/** Starting: the company's name and colour, and Play (or the request for a plot in a closed Yard) */
function StartForm({ seat, closed, code }: { seat: SeatRefDto; closed: boolean; code: string | null }) {
  const user = useAppStore((s) => s.user);
  const invite = useAppStore((s) => s.invite);
  const [name, setName] = useState(
    invite?.dto.name ?? (user !== null && !user.isGuest ? `${user.userName} Pipes` : nameIdea()),
  );
  const [color, setColor] = useState(SUGGESTED_COLOR);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const short = name.trim().length < 3;
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    try {
      const failure = await gameClient.foundCompany(name, color, seat, code);
      if (failure?.code === "seatTaken") {
        setError("Someone settled there a moment ago: the map shows the free plots again.");
        setState({ pickerReload: appStore.getState().pickerReload + 1 });
      } else {
        setError(failure?.error ?? null);
      }
    } finally {
      setBusy(false);
    }
  };
  return (
    <form className="start-form" onSubmit={(e) => void submit(e)}>
      <label>
        Your company
        <input value={name} onChange={(e) => setName(e.target.value)} required minLength={3} maxLength={24} />
      </label>
      <Swatches value={color} onChange={setColor} colors={COLORS} />
      {closed && (
        <p className="small picker-honest">
          This Yard is closed: you ask, and a resident says yes. Meanwhile you start on a free plot nearby,
          and move in for free when they do.
        </p>
      )}
      {error !== null && (
        <p className="form-error small" role="alert">
          {error}
        </p>
      )}
      <button className="primary wide" type="submit" disabled={busy || short}>
        {busy ? "Settling…" : closed ? "Ask to settle here, and play" : "Play here"}
      </button>
      {user?.isGuest === true && (
        <p className="muted small">
          You play as a guest: keep your company with a name and a password later.
        </p>
      )}
    </form>
  );
}

/** Moving: what it costs, what waits in crates, and Move (or the request for a plot in a closed Yard) */
function MoveForm({
  seat,
  plot,
  closed,
  code,
}: {
  seat: SeatRefDto;
  plot: SeatDto;
  closed: boolean;
  code: string | null;
}) {
  const company = useAppStore((s) => s.company)!;
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow(1000) + serverOffset;
  const moving = company.moving;
  const waitMs = moving.canMoveAt === null ? 0 : new Date(moving.canMoveAt).getTime() - now;
  const free = moving.freeMoves > 0;
  const crates = plot.crates ?? 0;
  return (
    <div className="start-form">
      <p className="small">
        {free
          ? `A free move (${moving.freeMoves} left)`
          : `A move costs ${formatAmount(moving.moveCost)} gold`}
        {waitMs > 0 && ` · next move in ${formatDuration(waitMs / 1000)}`}. Your plot lifts off on balloons
        and lands with its layout, turned to face its new Yard.
      </p>
      <p className="small">
        {crates === 0
          ? "Everything you built fits this land."
          : `${crates} of your buildings don't fit this land: they wait in crates until you place them.`}
      </p>
      {closed ? (
        <button
          className="primary wide"
          onClick={() =>
            void gameClient
              .crewCommand(gameClient.connection.requestPlot(seat), "Asked: a resident says yes or no")
              .then((ok) => ok && gameClient.closePicker())
          }
        >
          Ask to settle here
        </button>
      ) : (
        <button
          className="primary wide"
          disabled={waitMs > 0 || (!free && company.gold < moving.moveCost)}
          onClick={() =>
            void gameClient.relocateTo(seat, code).then((ok) => {
              if (ok) {
                gameClient.forgetInvite();
                gameClient.closePicker();
              }
            })
          }
        >
          Move here
        </button>
      )}
    </div>
  );
}

/**
 * Picking your spot on the map (1.4 §6): the real map with the free plots glowing, Pick for me, Find a player and a
 * friend's link along the top, and the chosen plot's card (its land, its Yard and who lives there, the town) with
 * the company's name and colour and Play. Moving uses the same picker with Move here.
 */
export function PlotPicker({ mode }: { mode: PickMode }) {
  const picker = useAppStore((s) => s.picker);
  const invite = useAppStore((s) => s.invite);
  const company = useAppStore((s) => s.company);
  const reload = useAppStore((s) => s.pickerReload);
  const [stations, setStations] = useState<StationsDto | null>(null);
  const [town, setTown] = useState<{ x: number; y: number } | null>(() =>
    mode === "move" && company !== null
      ? { x: company.districtX, y: company.districtY }
      : invite?.dto.yard != null
        ? { x: invite.dto.yard.districtX, y: invite.dto.yard.districtY }
        : null,
  );
  // The Yard to start at once the town is loaded: a friend's, or a player's Find a player found
  const [focus, setFocus] = useState<{ districtX: number; districtY: number; index: number } | null>(
    () => invite?.dto.yard ?? null,
  );
  const [note, setNote] = useState<string | null>(null);
  const looked = useRef(false);
  const code = invite?.code ?? null;

  useEffect(() => {
    setState({ picker: { mode, seats: {}, chosen: null } });
    return () => setState({ picker: null });
  }, [mode]);

  useEffect(() => {
    let cancelled = false;
    void gameClient.connection.getStations(town?.x ?? null, town?.y ?? null, code).then((dto) => {
      if (cancelled) {
        return;
      }
      setStations(dto);
      const current = appStore.getState().picker;
      const before = current?.chosen ?? null;
      const yard =
        focus !== null && focus.districtX === dto.districtX && focus.districtY === dto.districtY
          ? focus.index
          : null;
      let chosen = chooseSeat(dto, yard !== null ? null : before, yard);
      if (yard !== null && chosen === null) {
        // Nothing is held: their Yard filled up, so the nearest free plot
        chosen = dto.pickForMe;
        setNote("Their Yard is full now: here's the nearest free plot.");
      } else if (before !== null && findSeat(dto, before) !== null && !sameSeat(before, chosen)) {
        setNote("Someone just settled on that plot: here's another free one.");
      }
      setFocus(null);
      setState({ picker: { mode, seats: seatsOf(dto), chosen: chosen ?? before } });
      if (!looked.current) {
        looked.current = true;
        if (chosen !== null) {
          gameClient.lookAtPlot(chosen);
        } else {
          gameClient.lookAtTown(dto.districtX, dto.districtY);
        }
      }
    });
    return () => {
      cancelled = true;
    };
    // The focus is taken once, when its town loads
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [town?.x, town?.y, code, reload, mode]);

  // A tap on a plot of another town loads that town
  const chosen = picker?.chosen ?? null;
  if (
    chosen !== null &&
    stations !== null &&
    (chosen.districtX !== stations.districtX || chosen.districtY !== stations.districtY) &&
    (town?.x !== chosen.districtX || town?.y !== chosen.districtY)
  ) {
    setTown({ x: chosen.districtX, y: chosen.districtY });
  }
  // The camera follows the chosen plot
  const chosenKey = chosen === null ? null : seatKey(chosen);
  useEffect(() => {
    const seat = appStore.getState().picker?.chosen ?? null;
    if (seat !== null && looked.current) {
      gameClient.lookAtPlot(seat);
    }
  }, [chosenKey]);

  const found = stations !== null && chosen !== null ? findSeat(stations, chosen) : null;
  const seat = found?.seat ?? null;
  const station = found?.station ?? null;
  const choose = (ref: SeatRefDto) => {
    setNote(null);
    setState({ picker: { mode, seats: picker?.seats ?? {}, chosen: ref } });
  };

  return (
    <>
      <div className="panel picker-top" role="region" aria-label="Pick your spot">
        <div className="picker-title">
          <h2>{mode === "move" ? "Move your plot" : "Pick your spot"}</h2>
          {mode === "move" && (
            <button className="icon-button" title="Close (Esc)" onClick={() => gameClient.closePicker()}>
              ×
            </button>
          )}
        </div>
        <p className="muted small">
          Four plots share each Yard: your neighbours there buy your spare and sell you what your land lacks.
          Free plots glow; tap one to see its land.
        </p>
        <div className="picker-actions">
          {stations !== null && (
            <button type="button" className="primary" onClick={() => choose(stations.pickForMe)}>
              🎲 Pick for me
            </button>
          )}
          {stations !== null && stations.districts.length > 1 && (
            <select
              aria-label="Town"
              value={`${stations.districtX},${stations.districtY}`}
              onChange={(e) => {
                const [x, y] = e.target.value.split(",").map(Number);
                setTown({ x: x!, y: y! });
                gameClient.lookAtTown(x!, y!);
              }}
            >
              {stations.districts.map((d) => (
                <option key={`${d.districtX},${d.districtY}`} value={`${d.districtX},${d.districtY}`}>
                  {d.name}
                </option>
              ))}
            </select>
          )}
          <FindPlayer
            onFound={(t) => {
              setFocus({ districtX: t.districtX, districtY: t.districtY, index: Math.floor(t.slot / 4) });
              setTown({ x: t.districtX, y: t.districtY });
              choose({ districtX: t.districtX, districtY: t.districtY, slot: t.slot });
            }}
          />
          {mode === "start" && <FriendLink />}
        </div>
        {invite !== null && invite.dto.yard !== null && (
          <p className="small picker-friend">
            <span className="dot" style={{ background: invite.dto.fromColor ?? "#999" }} />{" "}
            {invite.dto.from ?? "Your friend"} plays at <strong>{invite.dto.yard.name}</strong>
            {invite.dto.opens && !invite.dto.yard.open && ": their link lets you in although it's closed"}.
          </p>
        )}
        {note !== null && (
          <p className="picker-moved small" role="status">
            {note}
          </p>
        )}
      </div>
      {seat !== null && station !== null && chosen !== null && (
        <div className="panel plot-card" role="dialog" aria-label="The plot">
          <Banner station={station} />
          <h3>
            The {sideOf(seat.slot)} plot{" "}
            <span className="muted small">
              · {stations?.district}
              {seat.state === "you" && " · yours"}
            </span>
          </h3>
          <Land seat={seat} />
          <h4>The Yard</h4>
          <Residents station={station} chosen={chosen} />
          {((station.makes?.length ?? 0) > 0 || (station.needs?.length ?? 0) > 0) && (
            <p className="muted small">
              📌{" "}
              {andList([
                ...(station.makes ?? []).map((t) => `makes ${goodWord(t)}`),
                ...(station.needs ?? []).map((t) => `needs ${goodWord(t)}`),
              ])}
            </p>
          )}
          {!pickable(seat) ? (
            <p className="muted small">
              {seat.state === "you" ? "This is your plot." : "Someone lives here: tap a glowing plot."}
            </p>
          ) : mode === "start" ? (
            <StartForm key={seatKey(chosen)} seat={chosen} closed={seat.state === "closed"} code={code} />
          ) : (
            <MoveForm seat={chosen} plot={seat} closed={seat.state === "closed"} code={code} />
          )}
        </div>
      )}
    </>
  );
}
