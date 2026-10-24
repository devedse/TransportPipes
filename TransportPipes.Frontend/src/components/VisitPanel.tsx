import { useState, type FormEvent } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatTimeAgo } from "../app/format";
import { useNow } from "../app/useNow";
import { useAppStore } from "../state/store";

/** Visiting an estate: who lives there, a like, the guestbook, a mentor */
export function VisitPanel() {
  const content = useAppStore((s) => s.content);
  const resourceWord = (resource: string) => content?.resourceName(resource).toLowerCase() ?? resource;
  const visiting = useAppStore((s) => s.visiting);
  const visit = useAppStore((s) => s.visit);
  const blocked = useAppStore((s) => s.chat?.blocked);
  const now = useNow(30000);
  const [note, setNote] = useState("");
  if (visiting === null) {
    return null;
  }
  if (visit === null) {
    return (
      <div className="panel visit-panel">
        <p className="muted">Knocking on the door…</p>
      </div>
    );
  }
  const write = async (event: FormEvent) => {
    event.preventDefault();
    if (await gameClient.writeGuestbook(visit.id, note)) {
      setNote("");
    }
  };
  const isBlocked = blocked?.includes(visit.id) ?? false;
  return (
    <div className="panel visit-panel" role="dialog" aria-label={`Visiting ${visit.name}`}>
      <div className="panel-header">
        <div className="building-title">
          <span className="company-dot" style={{ background: visit.color }} />
          <div>
            <h3>
              {visit.name}
              {visit.corporationTag !== null && (
                <span className="corp-tag small-tag" title={visit.corporationName ?? ""}>
                  {visit.corporationTag}
                </span>
              )}
              {visit.isNpc && <span className="badge npc">NPC</span>}
            </h3>
            <div className="muted small">
              Company Level {visit.level} · founded {formatTimeAgo(new Date(visit.createdAt), new Date(now))}{" "}
              ago
            </div>
          </div>
        </div>
        <button className="icon-button" title="Close (Esc)" onClick={() => gameClient.closeVisit()}>
          ×
        </button>
      </div>
      <div className="visit-stats">
        <div>
          <strong>{formatAmount(visit.netWorth)}</strong>
          <span className="muted small">net worth</span>
        </div>
        <div>
          <strong>{visit.buildings}</strong>
          <span className="muted small">buildings</span>
        </div>
        <div>
          <strong>{visit.partners}</strong>
          <span className="muted small">partners</span>
        </div>
        <div>
          <strong>♥ {visit.likes}</strong>
          <span className="muted small">likes</span>
        </div>
      </div>
      {!visit.isYou && !visit.isNpc && (
        <div className="button-row">
          <button
            className={visit.likedToday ? "liked" : "primary"}
            disabled={visit.likedToday}
            title="One like per estate per day: the owner gets a little gold and XP"
            onClick={() => void gameClient.likeEstate(visit.id)}
          >
            {visit.likedToday ? "♥ Liked today" : "♥ Like"}
          </button>
          <button onClick={() => void gameClient.messageCompany(visit.id, visit.name)}>✉ Message</button>
          <button
            onClick={() => gameClient.lookAtEstate(visit.estateX, visit.estateY)}
            title="Look at the estate"
          >
            👁 Look
          </button>
          <button
            className="icon-button"
            title={isBlocked ? "Unblock: see their messages again" : "Block: hide their messages"}
            onClick={() => void gameClient.setBlocked(visit.id, !isBlocked)}
          >
            {isBlocked ? "✅" : "🚫"}
          </button>
        </div>
      )}
      {!visit.isYou && !visit.isNpc && (
        <div className="swap-offer">
          {visit.swapFromThem ? (
            <>
              <span className="small">{visit.name} proposes to swap estates with you.</span>
              <button className="primary" onClick={() => void gameClient.proposeSwap(visit.id)}>
                Accept the swap
              </button>
            </>
          ) : visit.swapFromYou ? (
            <span className="muted small">You proposed to swap estates; they have to agree.</span>
          ) : (
            <button
              title="Both estates trade places with their whole layout"
              onClick={() => void gameClient.proposeSwap(visit.id)}
            >
              ⇄ Propose to swap estates
            </button>
          )}
        </div>
      )}
      {(visit.wants ?? []).length > 0 && (
        // A friend's want where you visit (after run 4: five offers of gold in the group chat, and no door)
        <div className="visit-wants">
          <div className="panel-title">What {visit.name} is building</div>
          <ul>
            {visit.wants!.map((w) => (
              <li key={`${w.site}-${w.resource}`} className="small">
                {w.building} waits for {formatAmount(w.left)} {resourceWord(w.resource)}
                {w.holds >= 1
                  ? ` · your Storage holds ${formatAmount(w.holds)}`
                  : w.makes
                    ? " · your spare goes there"
                    : ""}{" "}
                {w.holds >= 1 && (
                  <button className="small" onClick={() => void gameClient.sendToSite(w.site)}>
                    Send them ▸
                  </button>
                )}
                {w.orderCost !== null && (
                  <button className="small" onClick={() => void gameClient.orderRest(w.site, visit.name)}>
                    Order it for them · {formatAmount(w.orderCost)} g
                  </button>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}
      {visit.canChooseAsMentor && (
        <div className="mentor-offer">
          <span className="small">
            {visit.name} is a veteran. Choose them as your mentor: you both get gold when you reach Company
            Levels 10, 15 and 20.
          </span>
          <button onClick={() => void gameClient.chooseMentor(visit.id)}>Choose as mentor</button>
        </div>
      )}
      {visit.isYourMentor && <p className="small">🎓 Your mentor.</p>}
      {visit.mentees > 0 && <p className="muted small">🎓 Mentor of {visit.mentees} newcomers.</p>}
      {!visit.isNpc && (
        <div className="guestbook">
          <div className="panel-title">Guestbook</div>
          {!visit.isYou && (
            <form className="guestbook-form" onSubmit={(event) => void write(event)}>
              <input
                value={note}
                maxLength={200}
                placeholder="Leave a note…"
                onChange={(e) => setNote(e.target.value)}
              />
              <button disabled={note.trim() === ""}>Sign</button>
            </form>
          )}
          {visit.guestbook.length === 0 && <p className="muted small">No notes yet.</p>}
          {visit.guestbook.map((n) => (
            <div key={n.id} className="guestbook-note">
              <div>
                <strong className="small">{n.authorName}</strong>{" "}
                <span className="muted small">{formatTimeAgo(new Date(n.at), new Date(now))} ago</span>
              </div>
              <div>{n.text}</div>
              {visit.isYou && (
                <button className="link small" onClick={() => void gameClient.deleteGuestbookNote(n.id)}>
                  Remove
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
