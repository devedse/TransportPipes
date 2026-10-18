import { useState, type FormEvent } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatTimeAgo } from "../app/format";
import { useNow } from "../app/useNow";
import { useAppStore } from "../state/store";

/** Visiting an estate: who lives there, a like, the guestbook, a mentor */
export function VisitPanel() {
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
