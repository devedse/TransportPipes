import { gameClient } from "../app/client";
import { formatAmount, formatTimeAgo } from "../app/format";
import { useAppStore } from "../state/store";

/** What happened while the player was away */
export function ReportDialog() {
  const report = useAppStore((state) => state.report);
  const content = useAppStore((state) => state.content);
  if (report === null || content === null) {
    return null;
  }
  const sold = Object.entries(report.sold).sort((a, b) => b[1] - a[1]);
  return (
    <div className="modal-backdrop" onClick={() => gameClient.closeReport()}>
      <div
        className="panel report"
        role="dialog"
        aria-label="While you were away"
        onClick={(event) => event.stopPropagation()}
      >
        <h2>While you were away</h2>
        <p className="muted">{formatTimeAgo(new Date(report.since))} of work by your company</p>
        <div className="report-gold">
          <span className="gold-coin" />+{formatAmount(report.goldEarned)} gold
        </div>
        <ul className="facts">
          {report.buildingsCompleted > 0 && <li>🏗 {report.buildingsCompleted} buildings finished</li>}
          {report.levelsGained > 0 && <li>⭐ {report.levelsGained} Company Levels gained</li>}
          {sold.slice(0, 5).map(([resource, amount]) => (
            <li key={resource}>
              <span className="flow-dot" style={{ background: content.resourceColor(resource) }} />{" "}
              {formatAmount(amount)} {content.resourceName(resource).toLowerCase()} sold
            </li>
          ))}
          {report.notes.slice(-3).map((note) => (
            <li key={note}>{note}</li>
          ))}
        </ul>
        <button className="primary wide" autoFocus onClick={() => gameClient.closeReport()}>
          Collect and continue
        </button>
      </div>
    </div>
  );
}
