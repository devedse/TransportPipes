import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import { useAppStore } from "../state/store";

/** The pipe tiers to choose from while the pipe tool is active */
export function PipeTierBar() {
  const tool = useAppStore((state) => state.tool);
  const content = useAppStore((state) => state.content);
  const company = useAppStore((state) => state.company);
  const era = useAppStore((state) => state.era);
  if (tool.kind !== "pipe" || content === null || company === null) {
    return null;
  }
  return (
    <div className="panel pipe-tiers" role="radiogroup" aria-label="Pipe">
      {content.pipeTiers
        .filter((tier) => tier.era <= era + 1)
        .map((tier) => {
          const locked =
            tier.era > era || (tier.research !== null && !company.researched.includes(tier.research));
          const materials = Object.entries(tier.materials)
            .map(([resource, amount]) => ` + ${amount} ${content.resourceName(resource).toLowerCase()}`)
            .join("");
          return (
            <button
              key={tier.id}
              role="radio"
              aria-checked={tool.tier === tier.tier}
              disabled={locked}
              className={tool.tier === tier.tier ? "tier active" : "tier"}
              onClick={() => gameClient.setTool({ kind: "pipe", tier: tier.tier })}
              title={
                locked ? `Needs research: ${content.research(tier.research ?? "")?.name ?? ""}` : undefined
              }
            >
              <strong>{tier.name}</strong>
              <span className="small">
                {formatAmount(tier.gold)} g{materials} per tile
              </span>
              <span className="small muted">
                {tier.lossPerTile * 100}% loss · {formatAmount(tier.throughput)}/min
              </span>
            </button>
          );
        })}
    </div>
  );
}
