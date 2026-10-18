import { gameClient } from "../app/client";
import { formatAmount, formatRate } from "../app/format";
import { useAppStore } from "../state/store";

/** What happens in one network: who is in it, what is made, used, sold and lost, and which pipes are full */
export function NetworkPanel() {
  const details = useAppStore((state) => state.networkDetails);
  const content = useAppStore((state) => state.content);
  if (details === null || content === null) {
    return null;
  }
  return (
    <aside className="panel network-panel">
      <div className="panel-header">
        <div>
          <h3>Network</h3>
          <div className="muted small">
            {details.buildings} buildings · {details.pipes} pipe tiles · {details.pumps} pump stations
          </div>
        </div>
        <button className="icon-button" title="Close (Esc)" onClick={() => gameClient.selectNetwork(null)}>
          ×
        </button>
      </div>

      {details.pumpFactor < 1 && (
        <p className="status idle">
          Too few pump stations: everything runs at {Math.round(details.pumpFactor * 100)}%. A network needs
          one for every 50 buildings.
        </p>
      )}

      <section>
        <h4>Companies</h4>
        <ul className="flow-list">
          {details.owners.map((owner) => (
            <li key={owner.id}>
              <span className="company-dot" style={{ background: owner.color }} />
              <span>{owner.name}</span>
              <span className="flow-amount">{formatRate(owner.goldPerMinute)} g</span>
            </li>
          ))}
        </ul>
      </section>

      {details.resources.length > 0 && (
        <section>
          <h4>Resources per minute</h4>
          <table className="network-table">
            <thead>
              <tr>
                <th />
                <th>made</th>
                <th>used</th>
                <th>sold</th>
                <th>stored</th>
                <th>lost</th>
              </tr>
            </thead>
            <tbody>
              {details.resources.map((r) => (
                <tr key={r.resource}>
                  <td>
                    <span className="flow-dot" style={{ background: content.resourceColor(r.resource) }} />{" "}
                    {content.resourceName(r.resource)}
                  </td>
                  <td>{formatAmount(r.produced + r.imported + r.fromStorage)}</td>
                  <td>{formatAmount(r.used)}</td>
                  <td>{formatAmount(r.sold)}</td>
                  <td>{formatAmount(r.stored)}</td>
                  <td className={r.lost > 0.05 * (r.produced + r.imported + r.fromStorage) ? "warning" : ""}>
                    {formatAmount(r.lost)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}

      {details.bottlenecks.length > 0 && (
        <p className="status idle">
          {details.bottlenecks.length} connection{details.bottlenecks.length === 1 ? " is" : "s are"} full
          (red on the map): a better pipe carries more.
        </p>
      )}
    </aside>
  );
}
