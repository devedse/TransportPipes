import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import { useAppStore } from "../state/store";
import { NetworkSummary } from "./NetworkSummary";
import { Term } from "./Term";

/** What happens in one network: who is in it, what is made, used, sold and lost, and which pipes are full */
export function NetworkPanel() {
  const details = useAppStore((state) => state.networkDetails);
  const content = useAppStore((state) => state.content);
  const company = useAppStore((state) => state.company);
  const era = useAppStore((state) => state.era);
  if (details === null || content === null) {
    return null;
  }
  // The player's own pipes on the full connections, and the next better pipe they can lay
  const world = gameClient.world;
  const full = new Map<string, { x: number; y: number; tier: number }>();
  for (const bottleneck of details.bottlenecks) {
    for (let i = 0; i < bottleneck.path.length; i += 2) {
      const conductor = world?.conductorAt(bottleneck.path[i]!, bottleneck.path[i + 1]!);
      if (conductor !== undefined && conductor.kind === "pipe" && conductor.owner === company?.id) {
        full.set(`${conductor.x},${conductor.y}`, { x: conductor.x, y: conductor.y, tier: conductor.tier });
      }
    }
  }
  // Tiles that wait for a better pipe already are left out (playtest: every click asked for the same pipe again)
  const waiting = new Set<string>();
  for (let i = 0; i < (details.waiting?.length ?? 0); i += 2) {
    waiting.add(`${details.waiting![i]},${details.waiting![i + 1]}`);
  }
  const open = [...full.values()].filter((t) => !waiting.has(`${t.x},${t.y}`));
  const waits = full.size - open.length;
  const lowest = Math.min(...open.map((t) => t.tier));
  const next = content.pipeTiers
    .filter((t) => t.tier > lowest && t.era <= era)
    .sort((a, b) => a.tier - b.tier)[0];
  const weakest = open.filter((t) => t.tier === lowest);
  const nextUnlocked =
    next !== undefined && (next.research === null || company?.research.researched.includes(next.research));
  return (
    <aside className="panel network-panel" aria-label="Network">
      <div className="panel-header">
        <div>
          <h3>Network</h3>
          <div className="muted small">
            {details.buildings} buildings · {details.pipes} pipe tiles · {details.pumps}{" "}
            <Term word="pump">pump stations</Term>
          </div>
        </div>
        <button className="icon-button" title="Close (Esc)" onClick={() => gameClient.selectNetwork(null)}>
          ×
        </button>
      </div>

      {details.pumpFactor < 1 && (
        <p className="status idle">
          Too few pump stations: everything runs at {Math.round(details.pumpFactor * 100)}%. A long network
          loses pressure: every 50 buildings in one network need a Pump Station (build it anywhere on its
          pipes).
        </p>
      )}

      <section>
        <h4>Per minute</h4>
        <NetworkSummary details={details} content={content} ownCompany={company?.id ?? null} />
      </section>

      {details.bottlenecks.length > 0 && (
        <div className="status idle">
          <p>
            {details.bottlenecks.length} connection{details.bottlenecks.length === 1 ? " is" : "s are"} full
            (red on the map): a better pipe carries more.
          </p>
          {waits > 0 && (
            <p className="small">
              {waits} of their tiles wait for a better pipe already: it is laid once its goods are in (Yard ›
              What you&apos;re waiting for).
            </p>
          )}
          {weakest.length > 0 && next !== undefined && (
            <>
              <button
                className="primary"
                disabled={!nextUnlocked}
                onClick={() =>
                  void gameClient.upgradePipes(
                    weakest.map((t) => ({ x: t.x, y: t.y })),
                    next.tier,
                  )
                }
              >
                Upgrade {weakest.length} tiles to {next.name} · {formatAmount(weakest.length * next.gold)} g
              </button>
              <p className="small muted">
                {nextUnlocked
                  ? `${next.name}s carry ${formatAmount(next.throughput)} a minute` +
                    (Object.keys(next.materials).length > 0
                      ? ` and need ${Object.entries(next.materials)
                          .map(
                            ([r, a]) =>
                              `${formatAmount(a * weakest.length)} ${content.resourceName(r).toLowerCase()}`,
                          )
                          .join(", ")} from your storages.`
                      : ".")
                  : `${next.name}s need research first (Research panel). Or draw a better pipe over these with the pipe tool.`}
              </p>
            </>
          )}
        </div>
      )}
    </aside>
  );
}
