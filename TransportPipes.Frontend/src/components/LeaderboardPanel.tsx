import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import { useEffect, useState } from "react";
import type { LeaderboardEntryDto, LeaderboardKind, SleeperDto } from "../api/contracts";
import { setState, useAppStore } from "../state/store";

const KINDS: Array<{ id: LeaderboardKind; name: string; unit: string }> = [
  { id: "netWorth", name: "Net worth", unit: "gold" },
  { id: "income", name: "Income", unit: "gold/hour" },
  { id: "weeklyEarnings", name: "This week", unit: "gold earned" },
  { id: "level", name: "Level", unit: "" },
  { id: "partners", name: "Partners", unit: "trading partners" },
  { id: "likes", name: "Likes", unit: "likes" },
  { id: "efficiency", name: "Efficiency", unit: "gold per tile per hour" },
];

function Row({ entry, kind }: { entry: LeaderboardEntryDto; kind: LeaderboardKind }) {
  const content = useAppStore((s) => s.content);
  let level = 0;
  if (kind === "level" && content !== null) {
    content.dto.progression.xpForLevel.forEach((xp, i) => {
      if (xp <= entry.value) {
        level = i;
      }
    });
  }
  return (
    <tr className={entry.you ? "you" : undefined}>
      <td className="rank">{entry.rank <= 3 ? ["🥇", "🥈", "🥉"][entry.rank - 1] : entry.rank}</td>
      <td>
        <span className="company-dot" style={{ background: entry.color }} />{" "}
        <button className="link" onClick={() => void gameClient.visitEstate(entry.company, true)}>
          {entry.name}
        </button>
      </td>
      <td className="value">{kind === "level" ? `Level ${level}` : formatAmount(entry.value)}</td>
    </tr>
  );
}

/** The Quiet Isles (GDD §12.2): the estates that sleep until their owners come back */
function QuietIsles() {
  const [sleepers, setSleepers] = useState<SleeperDto[] | null>(null);
  useEffect(() => {
    void gameClient.connection.getQuietIsles().then(setSleepers);
  }, []);
  return (
    <div className="quiet-isles">
      <p className="muted small">
        Estates of players who have been away for a month sleep here, packed up with everything they built.
        When their owners come back, they move to a free spot for free.
      </p>
      {sleepers?.length === 0 && <p className="muted">Nobody is asleep.</p>}
      {sleepers?.map((t) => (
        <div key={t.id} className="sleeper">
          <span className="company-dot" style={{ background: t.color }} /> {t.name}
          <span className="muted small">
            {" "}
            · Level {t.level} · asleep since {new Date(t.since).toLocaleDateString()}
          </span>
        </div>
      ))}
    </div>
  );
}

/** Leaderboards of the district and the whole world (GDD §9.5) */
export function LeaderboardPanel() {
  const open = useAppStore((s) => s.leaderboardOpen);
  const board = useAppStore((s) => s.leaderboard);
  const kind = useAppStore((s) => s.leaderboardKind);
  const district = useAppStore((s) => s.leaderboardDistrict);
  const [isles, setIsles] = useState(false);
  if (!open) {
    return null;
  }
  const close = () => setState({ leaderboardOpen: false });
  const unit = KINDS.find((t) => t.id === kind)?.unit ?? "";
  const youShown = board?.entries.some((t) => t.you) ?? false;
  return (
    <div className="modal-backdrop" onClick={close}>
      <div
        className="panel leaderboard-panel"
        role="dialog"
        aria-label="Leaderboards"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="panel-header">
          <h2>Leaderboards</h2>
          <div className="tabs compact">
            <button
              className={district ? "tab active" : "tab"}
              onClick={() => void gameClient.refreshLeaderboard(undefined, true)}
            >
              District
            </button>
            <button
              className={!district ? "tab active" : "tab"}
              onClick={() => void gameClient.refreshLeaderboard(undefined, false)}
            >
              World
            </button>
          </div>
          <button className="icon-button" title="Close (Esc)" onClick={close}>
            ×
          </button>
        </div>
        <div className="tabs">
          <button className={isles ? "tab active" : "tab"} onClick={() => setIsles(true)}>
            Quiet Isles
          </button>
          {KINDS.map((k) => (
            <button
              key={k.id}
              className={!isles && k.id === kind ? "tab active" : "tab"}
              onClick={() => {
                setIsles(false);
                void gameClient.refreshLeaderboard(k.id);
              }}
            >
              {k.name}
            </button>
          ))}
        </div>
        {isles ? (
          <QuietIsles />
        ) : (
          <table className="leaderboard">
            <thead>
              <tr>
                <th>#</th>
                <th>Company</th>
                <th className="value">{unit}</th>
              </tr>
            </thead>
            <tbody>
              {board?.entries.map((entry) => (
                <Row key={entry.company} entry={entry} kind={kind} />
              ))}
              {board !== null && board.you !== null && !youShown && (
                <>
                  <tr className="gap">
                    <td colSpan={3}>…</td>
                  </tr>
                  <Row entry={board.you} kind={kind} />
                </>
              )}
            </tbody>
          </table>
        )}
        {!isles && board?.entries.length === 0 && <p className="muted">No companies yet.</p>}
      </div>
    </div>
  );
}
