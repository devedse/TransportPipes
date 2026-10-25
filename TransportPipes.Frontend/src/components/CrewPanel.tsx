import { useState, type FormEvent } from "react";
import { gameClient } from "../app/client";
import { emblemIcon } from "../app/crews";
import { useNow } from "../app/useNow";
import { formatDuration } from "../app/format";
import type { CrewDto } from "../api/contracts";
import { setState, toast, useAppStore } from "../state/store";
import { CrewFields } from "./CrewFields";

/** Copies a link, or shows it to copy by hand when the browser won't */
async function copy(text: string) {
  try {
    await navigator.clipboard.writeText(text);
    toast("Link copied: send it to your friend", "success");
  } catch {
    window.prompt("Copy this link and send it to your friend", text);
  }
}

/** Shares a link the way phones do (the share sheet), or copies it */
async function share(text: string, title: string) {
  if (navigator.share !== undefined) {
    try {
      await navigator.share({ title, text: `${title}: ${text}`, url: text });
      return;
    } catch {
      // Cancelled: copy instead
    }
  }
  await copy(text);
}

/** The Yard's name and banner: the first resident names it, and any resident may change it */
function YardLooks({ crew, onDone }: { crew: CrewDto; onDone?: () => void }) {
  const [looks, setLooks] = useState<{ name: string; emblem: string; colors: [string, string] }>({
    name: crew.named ? crew.name : "",
    emblem: crew.emblem,
    colors: [crew.color, crew.color2],
  });
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const ok = await gameClient.crewCommand(
      gameClient.connection.setYard(looks.name, looks.emblem, looks.colors[0], looks.colors[1]),
      `${looks.name.trim()} it is`,
    );
    if (ok) {
      onDone?.();
    }
  };
  return (
    <form onSubmit={(e) => void submit(e)}>
      <CrewFields
        label="The Yard's name"
        name={looks.name}
        emblem={looks.emblem}
        colors={looks.colors}
        onChange={(n, e, c) => setLooks({ name: n, emblem: e, colors: c })}
      />
      <button className="primary" type="submit">
        {crew.named ? "Save" : "Name the Yard"}
      </button>
    </form>
  );
}

/** Your "Play next to me" link (1.4 §6): it opens the map at your Yard, and lets the first friend in although it's closed */
function NextToMe({ crew, now }: { crew: CrewDto; now: number }) {
  const [link, setLink] = useState<string | null>(
    crew.link !== null ? `${location.origin}/?near=${encodeURIComponent(crew.link.replace("-", ""))}` : null,
  );
  const get = async () => setLink(await gameClient.nextToMeLink());
  const until = crew.linkUntil !== null ? new Date(crew.linkUntil).getTime() : 0;
  return (
    <section>
      <h4>Play next to me</h4>
      <p className="muted small">
        Your link opens the map at your Yard. The first friend who uses it may settle here even when the Yard
        is closed. It works for seven days, and nothing is held: if the Yard fills up first, the map shows the
        nearest free plots instead.
      </p>
      {link === null ? (
        <button className="primary" onClick={() => void get()}>
          Get my link
        </button>
      ) : (
        <>
          <p className="code-line">
            <code>{link}</code>
            {until > now && (
              <span className="muted small"> · works {formatDuration((until - now) / 1000)} more</span>
            )}
          </p>
          <span className="button-row">
            {navigator.share !== undefined && (
              <button onClick={() => void share(link, `Play next to me in ${crew.name}`)}>Send</button>
            )}
            <button onClick={() => void copy(link)}>Copy link</button>
          </span>
        </>
      )}
    </section>
  );
}

/** Your Yard's crew (1.4 §7): its name and banner, open or closed, its residents, the requests, your link */
export function CrewPanel() {
  const open = useAppStore((s) => s.crewOpen);
  const view = useAppStore((s) => s.crew);
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow(60000) + serverOffset;
  const [editing, setEditing] = useState(false);
  if (!open) {
    return null;
  }
  const close = () => setState({ crewOpen: false });
  const crew = view?.crew ?? null;
  return (
    <div className="modal-backdrop" onClick={close}>
      <div
        className="panel crew-panel"
        role="dialog"
        aria-label="Your Yard"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="panel-header">
          <h2>
            {crew !== null ? (
              <span className="crew-title" style={{ borderColor: crew.color }}>
                {emblemIcon(crew.emblem)} {crew.name}
              </span>
            ) : (
              "Your Yard"
            )}
          </h2>
          <button className="icon-button" title="Close (Esc)" onClick={close}>
            ×
          </button>
        </div>
        {view === null ? (
          <p className="muted">Loading…</p>
        ) : crew === null ? (
          <p className="muted">You live at a Yard once your plot has landed.</p>
        ) : (
          <>
            <div
              className="crew-banner"
              style={{ background: `linear-gradient(90deg, ${crew.color} 50%, ${crew.color2} 50%)` }}
            >
              <span>{emblemIcon(crew.emblem)}</span> <strong>{crew.name}</strong>
              <span className="small"> · {crew.members.length} of 4 plots</span>
            </div>
            {!crew.named ? (
              <section>
                <h4>Name your Yard</h4>
                <p className="muted small">
                  The people around a Yard are its crew. You live here first: give it a name and a banner.
                  Everyone who moves in is an owner too.
                </p>
                <YardLooks crew={crew} />
              </section>
            ) : editing ? (
              <section>
                <YardLooks crew={crew} onDone={() => setEditing(false)} />
              </section>
            ) : (
              <button className="link small" onClick={() => setEditing(true)}>
                Change the name or banner
              </button>
            )}
            <section>
              <h4>Residents</h4>
              <ul className="crew-members">
                {crew.members.map((m) => (
                  <li key={m.id}>
                    <span className="dot" style={{ background: m.color }} />{" "}
                    <strong>{m.you ? "You" : m.name}</strong>{" "}
                    <span className="muted small grow">
                      Lv {m.level}
                      {m.activity !== null && ` · ${m.activity}`}
                    </span>
                    {!m.you && (
                      <button
                        className="small"
                        title={`Look at ${m.name}'s plot`}
                        onClick={() => void gameClient.visitFriend(m.id)}
                      >
                        👁 Visit
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            </section>
            <section>
              <h4>Who may settle here</h4>
              <label className="check">
                <input
                  type="checkbox"
                  checked={crew.open}
                  onChange={() => void gameClient.crewCommand(gameClient.connection.setYardOpen(!crew.open))}
                />
                Open: anyone may settle on a free plot
              </label>
              <p className="muted small">
                {crew.open
                  ? "Close it and newcomers ask first; any resident says yes. A closed Yard stays closed."
                  : "Closed: newcomers ask, and any resident says yes. Your link still lets one friend in."}
              </p>
            </section>
            {crew.requests.length > 0 && (
              <section>
                <h4>Asking to settle</h4>
                <ul className="crew-members">
                  {crew.requests.map((r) => (
                    <li key={r.id}>
                      <span className="dot" style={{ background: r.color }} /> <strong>{r.name}</strong>{" "}
                      <span className="muted small grow">
                        Lv {r.level} · waits{" "}
                        {formatDuration(Math.max(0, (new Date(r.until).getTime() - now) / 1000))}
                      </span>
                      <span className="button-row">
                        <button
                          className="primary small"
                          onClick={() =>
                            void gameClient.crewCommand(
                              gameClient.connection.answerRequest(r.id, true),
                              `${r.name} moves in`,
                            )
                          }
                        >
                          Yes
                        </button>
                        <button
                          className="small"
                          onClick={() =>
                            void gameClient.crewCommand(gameClient.connection.answerRequest(r.id, false))
                          }
                        >
                          No
                        </button>
                      </span>
                    </li>
                  ))}
                </ul>
              </section>
            )}
            <NextToMe crew={crew} now={now} />
            <p className="muted small">
              Nobody can be thrown out of a Yard: you leave it by moving your plot (Company › Move).
              {crew.lastOut && " You're its last resident: moving away takes the Yard's own things with it."}
            </p>
          </>
        )}
      </div>
    </div>
  );
}
