import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { SeasonResultDto } from "../api/contracts";
import { setState, useAppStore } from "../state/store";

const MEDALS = ["🥇", "🥈", "🥉"];

function Row({ result }: { result: SeasonResultDto }) {
  return (
    <tr className={result.you ? "you" : undefined}>
      <td className="rank">{result.rank <= 3 ? MEDALS[result.rank - 1] : result.rank}</td>
      <td>
        {result.company}
        {result.title !== null && <span className="title-chip small-title">{result.title}</span>}
      </td>
      <td className="value">{formatAmount(result.netWorth)}</td>
      <td className="value">+{result.legacy}</td>
    </tr>
  );
}

/** The end of the season (GDD §8.8): the ceremony with the rankings and everyone's Legacy */
export function SeasonPanel() {
  const open = useAppStore((s) => s.seasonOpen);
  const season = useAppStore((s) => s.season);
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow(1000) + serverOffset;
  if (!open || season === null) {
    return null;
  }
  const close = () => setState({ seasonOpen: false });
  const next = season.nextWorldAt === null ? 0 : (new Date(season.nextWorldAt).getTime() - now) / 1000;
  return (
    <div className="modal-backdrop" onClick={close}>
      <div
        className="panel season-panel"
        role="dialog"
        aria-label="Season"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="season-head">
          <div className="season-rocket">🚀</div>
          <h2>Season {season.season} is over</h2>
          <p className="muted">
            The Space Elevator has launched. Every company's results became Legacy for its owner: more
            starting gold, titles and from Legacy level 5 a manager slot in every next season.
          </p>
          {season.you !== null && (
            <div className="season-you">
              You finished <strong>#{season.you.rank}</strong> and earned{" "}
              <strong>{season.you.legacy} Legacy</strong>
              {season.you.title !== null && (
                <>
                  {" "}
                  and the title <span className="title-chip">{season.you.title}</span>
                </>
              )}
            </div>
          )}
        </div>
        <table className="leaderboard">
          <thead>
            <tr>
              <th>#</th>
              <th>Company</th>
              <th className="value">Net worth</th>
              <th className="value">Legacy</th>
            </tr>
          </thead>
          <tbody>
            {season.top.map((result) => (
              <Row key={result.rank} result={result} />
            ))}
            {season.you !== null && !season.top.some((t) => t.you) && <Row result={season.you} />}
          </tbody>
        </table>
        <p className="muted small">
          {next > 0
            ? `A new world opens in ${formatDuration(next)}. Until then this world stays open to look around.`
            : "The new world is opening…"}
        </p>
        <button className="primary wide" onClick={close}>
          Look around
        </button>
      </div>
    </div>
  );
}
