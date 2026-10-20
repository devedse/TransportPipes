import { gameClient } from "../app/client";
import { useAppStore } from "../state/store";
import { NetworkSummary } from "./NetworkSummary";

/** What happens in one network: who is in it, what is made, used, sold and lost, and which pipes are full */
export function NetworkPanel() {
  const details = useAppStore((state) => state.networkDetails);
  const content = useAppStore((state) => state.content);
  const company = useAppStore((state) => state.company);
  if (details === null || content === null) {
    return null;
  }
  return (
    <aside className="panel network-panel" aria-label="Network">
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
        <h4>Per minute</h4>
        <NetworkSummary details={details} content={content} ownCompany={company?.id ?? null} />
      </section>

      {details.bottlenecks.length > 0 && (
        <p className="status idle">
          {details.bottlenecks.length} connection{details.bottlenecks.length === 1 ? " is" : "s are"} full
          (red on the map): a better pipe carries more.
        </p>
      )}
    </aside>
  );
}
