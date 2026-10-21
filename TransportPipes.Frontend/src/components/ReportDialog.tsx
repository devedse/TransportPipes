import { gameClient } from "../app/client";
import { formatAmount, formatTimeAgo, plural } from "../app/format";
import { useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

/** What happened while the player was away */
export function ReportDialog() {
  const report = useAppStore((state) => state.report);
  const content = useAppStore((state) => state.content);
  const gold = useAppStore((state) => state.company?.gold ?? 0);
  const level = useAppStore((state) => state.company?.level ?? 1);
  // After a long absence the way back comes first
  const welcomeBack = useAppStore((state) => state.welcomeBack);
  if (report === null || content === null || welcomeBack !== null) {
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
          {report.buildingsCompleted > 0 && (
            <li>🏗 {plural(report.buildingsCompleted, "building")} finished</li>
          )}
          {report.levelsGained > 0 && (
            <li>
              ⭐ +{report.levelsGained} Company {report.levelsGained === 1 ? "Level" : "Levels"}, now Level{" "}
              {level}
            </li>
          )}
          {sold.slice(0, 5).map(([resource, amount]) => (
            <li key={resource}>
              <ResourceIcon resource={resource} /> {formatAmount(amount)}{" "}
              {content.resourceName(resource).toLowerCase()} sold
            </li>
          ))}
          {report.researchDone.map((tech) => (
            <li key={tech}>🔬 Research done: {tech}</li>
          ))}
          {report.ordersCompleted > 0 && (
            <li>✅ {plural(report.ordersCompleted, "daily order")} completed</li>
          )}
          {report.contracts.slice(-4).map((note, i) => (
            <li key={`contract${i}`}>📜 {note}</li>
          ))}
          {report.newNeighbours.length > 0 && <li>🏘 New neighbours: {report.newNeighbours.join(", ")}</li>}
          {report.likesReceived > 0 && <li>♥ {plural(report.likesReceived, "visitor")} liked your estate</li>}
          {report.notes.slice(-3).map((note) => (
            <li key={note}>{note}</li>
          ))}
        </ul>
        {report.fullStorages.length > 0 && (
          <div className="report-storages">
            <div className="panel-title">Full storages</div>
            {report.fullStorages.map((storage) => (
              <div key={storage.building} className="report-storage">
                <span>
                  🛢 {content.building(storage.type)?.name ?? storage.type} (level {storage.level}) holds{" "}
                  {formatAmount(storage.capacity)}
                </span>
                {storage.upgradeCost !== null && (
                  <button
                    disabled={gold < storage.upgradeCost}
                    onClick={() => void gameClient.upgradeFromReport(storage.building)}
                  >
                    Upgrade · {formatAmount(storage.upgradeCost)} g
                  </button>
                )}
              </div>
            ))}
          </div>
        )}
        <button className="primary wide" autoFocus onClick={() => gameClient.closeReport()}>
          Collect and continue
        </button>
      </div>
    </div>
  );
}
