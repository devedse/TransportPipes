import { useState, type FormEvent } from "react";
import { gameClient, inviteLink } from "../app/client";
import { CREW_COLORS, emblemIcon, landText } from "../app/crews";
import { useNow } from "../app/useNow";
import { formatDuration } from "../app/format";
import type { HeldSeatDto } from "../api/contracts";
import { setState, toast, useAppStore } from "../state/store";
import { CrewFields } from "./FoundCompany";

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

/** The seats held for friends, each with its own link */
export function HeldSeats({ holds, crewName }: { holds: HeldSeatDto[]; crewName: string }) {
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const company = useAppStore((s) => s.company);
  const now = useNow(60000) + serverOffset;
  if (holds.length === 0) {
    return <p className="muted small">No seats held for friends.</p>;
  }
  return (
    <ul className="held-seats">
      {holds.map((hold) => {
        if (hold.for !== null) {
          // A seat kept for a member: after a crew move, or an asleep friend's seat next to the others
          const mine = hold.for === company?.id;
          return (
            <li key={hold.code}>
              <div>
                <strong>{mine ? "Your seat" : `${hold.forName ?? "A friend"}'s seat`}</strong>{" "}
                <span className="muted small">
                  in {hold.station} · {landText(hold.primary, hold.secondary)} · kept{" "}
                  {formatDuration(Math.max(0, (new Date(hold.until).getTime() - now) / 1000))} more
                </span>
              </div>
              {mine && (
                <button
                  className="primary"
                  onClick={() =>
                    void gameClient.relocateTo({
                      districtX: hold.districtX,
                      districtY: hold.districtY,
                      slot: hold.slot,
                    })
                  }
                >
                  Move there
                </button>
              )}
            </li>
          );
        }
        return <FriendSeat key={hold.code} hold={hold} crewName={crewName} now={now} />;
      })}
    </ul>
  );
}

/**
 * A seat kept for a friend (F9): who it is for ("Ben"), so the link says "Copy Ben's link", the move-in screen
 * "Ana kept this seat for Ben", and the crew sees "Riverside Co. (Ben)"
 */
function FriendSeat({ hold, crewName, now }: { hold: HeldSeatDto; crewName: string; now: number }) {
  const [friend, setFriend] = useState(hold.friendName ?? "");
  const [saved, setSaved] = useState(hold.friendName ?? "");
  const link = inviteLink(hold.code, true);
  const save = () => {
    const name = friend.trim();
    if (name !== saved) {
      setSaved(name);
      void gameClient.connection.nameHold(hold.code, name === "" ? null : name);
    }
  };
  const whose = saved === "" ? "" : `${saved}'s `;
  return (
    <li>
      <div>
        <strong className="code">{hold.code}</strong>{" "}
        <span className="muted small">
          {landText(hold.primary, hold.secondary)} · kept{" "}
          {formatDuration(Math.max(0, (new Date(hold.until).getTime() - now) / 1000))} more
        </span>
      </div>
      <label className="friend-name small">
        Who is it for?
        <input
          value={friend}
          maxLength={20}
          placeholder="a friend's name"
          onChange={(e) => setFriend(e.target.value)}
          onBlur={save}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              save();
            }
          }}
        />
      </label>
      <span className="button-row">
        <button
          onClick={() =>
            void share(link, `A seat next to me in ${crewName}${saved === "" ? "" : ` for ${saved}`}`)
          }
        >
          {saved === "" ? "Send" : `Send to ${saved}`}
        </button>
        <button onClick={() => void copy(link)}>Copy {whose}link</button>
        <button
          className="link small"
          onClick={() => void gameClient.crewCommand(gameClient.connection.releaseHold(hold.code))}
        >
          give free
        </button>
      </span>
    </li>
  );
}

/** Right after starting with friends: the codes of their seats, to send */
export function ShareCodes() {
  const codes = useAppStore((s) => s.shareCodes);
  const company = useAppStore((s) => s.company);
  if (codes === null || company === null) {
    return null;
  }
  const close = () => setState({ shareCodes: null });
  return (
    <div className="modal-backdrop" onClick={close}>
      <div
        className="panel share-panel"
        role="dialog"
        aria-label="Invite your friends"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="panel-header">
          <h2>Invite your friends</h2>
          <button className="icon-button" title="Close" onClick={close}>
            ×
          </button>
        </div>
        <p>
          Every friend gets a seat of their own next to you, kept for them until they come. Send each one
          their link: they move in with two taps, even when you are not online.
        </p>
        <HeldSeats holds={codes} crewName={company.crew?.name ?? "my crew"} />
        <button className="primary wide" onClick={close}>
          Start playing
        </button>
      </div>
    </div>
  );
}

/** Founding a crew later (from Level 1): a name, an emblem, two colours, and seats to keep for friends */
function FoundCrewForm() {
  const [crew, setCrew] = useState<{ name: string; emblem: string; colors: [string, string] }>({
    name: "",
    emblem: "kettle",
    colors: [CREW_COLORS[0]!, CREW_COLORS[1]!],
  });
  const [holds, setHolds] = useState(1);
  const [code, setCode] = useState("");
  const submit = (event: FormEvent) => {
    event.preventDefault();
    void gameClient.foundCrew(crew.name, crew.emblem, crew.colors[0], crew.colors[1], holds);
  };
  return (
    <>
      <p>
        A crew is the friends you play with: you live next to each other, your names fly one banner, and
        nobody can be removed (anyone leaves with one tap). There is no shared wallet.
      </p>
      <form onSubmit={submit}>
        <CrewFields
          name={crew.name}
          emblem={crew.emblem}
          colors={crew.colors}
          onChange={(n, e, c) => setCrew({ name: n, emblem: e, colors: c })}
        />
        <label>
          Seats to keep for friends in your station
          <select value={holds} onChange={(e) => setHolds(Number(e.target.value))}>
            {[0, 1, 2, 3].map((n) => (
              <option key={n} value={n}>
                {n}
              </option>
            ))}
          </select>
        </label>
        <button className="primary" type="submit">
          Found the crew
        </button>
      </form>
      <h4>Or join one</h4>
      <form
        className="button-row"
        onSubmit={(event) => {
          event.preventDefault();
          void gameClient.crewCommand(gameClient.connection.joinCrew(code), "Welcome to the crew");
        }}
      >
        <input
          placeholder="A seat code or crew code"
          value={code}
          onChange={(e) => setCode(e.target.value)}
        />
        <button type="submit" disabled={code.trim() === ""}>
          Join
        </button>
      </form>
    </>
  );
}

/** Your crew (v1.2 §6): members, the seats kept for friends, the invite link, who asked to join */
export function CrewPanel() {
  const open = useAppStore((s) => s.crewOpen);
  const view = useAppStore((s) => s.crew);
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow(60000) + serverOffset;
  if (!open) {
    return null;
  }
  const close = () => setState({ crewOpen: false });
  const crew = view?.crew ?? null;
  const inviteOpen = crew !== null && new Date(crew.inviteOpenUntil).getTime() > now;
  return (
    <div className="modal-backdrop" onClick={close}>
      <div className="panel crew-panel" role="dialog" aria-label="Crew" onClick={(e) => e.stopPropagation()}>
        <div className="panel-header">
          <h2>
            {crew !== null ? (
              <span className="crew-title" style={{ borderColor: crew.color }}>
                {emblemIcon(crew.emblem)} {crew.name}
              </span>
            ) : (
              "Crew"
            )}
          </h2>
          <button className="icon-button" title="Close (Esc)" onClick={close}>
            ×
          </button>
        </div>
        {view === null ? (
          <p className="muted">Loading…</p>
        ) : crew === null ? (
          <FoundCrewForm />
        ) : (
          <>
            <div
              className="crew-banner"
              style={{ background: `linear-gradient(90deg, ${crew.color} 50%, ${crew.color2} 50%)` }}
            >
              <span>{emblemIcon(crew.emblem)}</span> <strong>{crew.name}</strong>
              <span className="small">
                {" "}
                · {crew.members.length} of {crew.maxSize}
              </span>
            </div>
            <section>
              <h4>Members</h4>
              <ul className="crew-members">
                {crew.members.map((m) => (
                  <li key={m.id}>
                    <span className="dot" style={{ background: m.color }} />{" "}
                    <strong>{m.you ? "You" : m.name}</strong>{" "}
                    <span className="muted small grow">
                      Lv {m.level} · {m.station}
                      {m.activity !== null && ` · ${m.activity}`}
                      {m.asleep && " (a stand-in keeps their trades)"}
                      {m.tended && " · 💛 tended"}
                    </span>
                    {m.canTend && (
                      <button
                        className="small"
                        title="A favour while they're away: they'll read on how many days their crew tended it (never who)"
                        onClick={() =>
                          void gameClient.crewCommand(
                            gameClient.connection.tend(m.id),
                            `You tended ${m.name}'s estate`,
                          )
                        }
                      >
                        💛 Tend
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            </section>
            {crew.consolidationCard && (
              <section className="keep-card">
                <strong>Move to a fresh station together?</strong>
                <p className="muted small">
                  Most of {crew.name} is asleep. You and the others who play move to{" "}
                  {crew.moveTo ?? "a fresh station"}; the sleepers&apos; estates wait safely on the Quiet
                  Isles, and their seats are kept next to you for when they come back.
                </p>
                <span className="button-row">
                  <button
                    className="primary"
                    onClick={() =>
                      void gameClient.crewCommand(
                        gameClient.connection.consolidate(),
                        "Up, up and away, together",
                      )
                    }
                  >
                    Move together
                  </button>
                  <button
                    onClick={() => void gameClient.crewCommand(gameClient.connection.dismissConsolidation())}
                  >
                    Not now
                  </button>
                </span>
              </section>
            )}
            {crew.keepCards.map((card) => (
              <section key={card.id} className="keep-card">
                <strong>Keep {card.name}&apos;s seat 60 more days?</strong>
                <p className="muted small">
                  {card.name} has been asleep for a while; their seat is kept until{" "}
                  {new Date(card.ends).toLocaleDateString()}. Without an answer their estate travels safely to
                  the Quiet Isles then, and a House keeps the seat warm. The first answer counts.
                </p>
                <span className="button-row">
                  <button
                    className="primary"
                    onClick={() =>
                      void gameClient.crewCommand(
                        gameClient.connection.keepSeat(card.id, true),
                        "Kept 60 days more",
                      )
                    }
                  >
                    Keep it
                  </button>
                  <button
                    onClick={() =>
                      void gameClient.crewCommand(gameClient.connection.keepSeat(card.id, false))
                    }
                  >
                    Let it go
                  </button>
                </span>
              </section>
            ))}
            {crew.requests.length > 0 && (
              <section>
                <h4>Want to join</h4>
                <ul className="crew-members">
                  {crew.requests.map((m) => (
                    <li key={m.id}>
                      <strong>{m.name}</strong> <span className="muted small">Lv {m.level}</span>{" "}
                      <button
                        onClick={() =>
                          void gameClient.crewCommand(gameClient.connection.acceptJoinRequest(m.id), "Let in")
                        }
                      >
                        Let them in
                      </button>
                    </li>
                  ))}
                </ul>
              </section>
            )}
            <section>
              <h4>Seats kept for friends</h4>
              <HeldSeats holds={crew.holds} crewName={crew.name} />
              {crew.members.length + crew.holds.length < crew.maxSize && (
                <button
                  onClick={() => void gameClient.crewCommand(gameClient.connection.holdSeats(1), "Seat kept")}
                >
                  Keep a seat in our station for a friend
                </button>
              )}
            </section>
            <section>
              <h4>Crew link</h4>
              <p className="muted small">
                For friends without a kept seat.{" "}
                {inviteOpen
                  ? `Anyone with it joins for the next ${formatDuration((new Date(crew.inviteOpenUntil).getTime() - now) / 1000)}; after that a member lets them in.`
                  : "It's older than a day: a member lets people in."}
              </p>
              <span className="button-row">
                <button onClick={() => void share(inviteLink(crew.inviteCode, false), `Join ${crew.name}`)}>
                  Send
                </button>
                <button onClick={() => void copy(inviteLink(crew.inviteCode, false))}>Copy link</button>
                <button
                  className="link small"
                  onClick={() =>
                    void gameClient.crewCommand(gameClient.connection.renewInvite(), "A fresh link")
                  }
                >
                  new link
                </button>
              </span>
            </section>
            {crew.moveTo !== null && crew.members.length > 1 && (
              <section>
                <h4>Live together</h4>
                <p className="muted small">
                  Move the crew as a block: you go to {crew.moveTo} now, and a seat there is kept for everyone
                  else for a week; each moves with one tap.
                </p>
                <button
                  onClick={() =>
                    void gameClient.crewCommand(gameClient.connection.crewMove(), `Off to ${crew.moveTo}`)
                  }
                >
                  Move the crew to {crew.moveTo}
                </button>
              </section>
            )}
            {(crew.stationClosed || crew.stationOpen) && (
              <section>
                <label className="check">
                  <input
                    type="checkbox"
                    checked={crew.stationOpen}
                    onChange={() =>
                      void gameClient.crewCommand(gameClient.connection.setStationOpen(!crew.stationOpen))
                    }
                  />
                  Let strangers settle in free seats of our station
                </label>
              </section>
            )}
            <button
              className="link danger"
              onClick={() => {
                if (
                  window.confirm(
                    `Leave ${crew.name}? Your estate stays where it is, and nothing about your trade changes.`,
                  )
                ) {
                  void gameClient.crewCommand(gameClient.connection.leaveCrew(), "You left the crew");
                }
              }}
            >
              Leave the crew
            </button>
          </>
        )}
      </div>
    </div>
  );
}
