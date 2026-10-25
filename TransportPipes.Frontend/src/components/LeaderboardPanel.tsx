import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import type { LeaderboardEntryDto, LeaderboardKind } from "../api/contracts";
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
    // Its value is the gold earned in all: the Company Level comes from it (1.4 §14)
    content.dto.progression.earnedForLevel.forEach((earned, i) => {
      if (earned <= entry.value) {
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

/** Leaderboards of the town and the whole world (GDD §9.5) */
export function LeaderboardPanel() {
  const open = useAppStore((s) => s.leaderboardOpen);
  const board = useAppStore((s) => s.leaderboard);
  const kind = useAppStore((s) => s.leaderboardKind);
  const district = useAppStore((s) => s.leaderboardDistrict);
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
              Town
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
          {KINDS.map((k) => (
            <button
              key={k.id}
              className={k.id === kind ? "tab active" : "tab"}
              onClick={() => void gameClient.refreshLeaderboard(k.id)}
            >
              {k.name}
            </button>
          ))}
        </div>
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
        {board?.entries.length === 0 && <p className="muted">No companies yet.</p>}
      </div>
    </div>
  );
}
