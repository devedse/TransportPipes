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
          const tech = tier.research !== null ? content.research(tier.research) : undefined;
          // Copper Pipes come at Company Level 8, Steel Pipes with Era 2 (after run 4)
          const why =
            tier.era > era
              ? `Comes with Era ${tier.era}`
              : tech !== undefined && tech.grant > 0
                ? `Yours at Company Level ${tech.grant} (a Lab gets it sooner)`
                : `Needs research: ${tech?.name ?? ""}`;
          const materials = Object.entries(tier.materials)
            .map(([resource, amount]) => ` + ${amount} ${content.resourceName(resource).toLowerCase()}`)
            .join("");
          return (
            <button
              key={tier.id}
              role="radio"
              aria-checked={tool.tier === tier.tier}
              disabled={locked}
              className={tool.tier === tier.tier ? "pipe-tier active" : "pipe-tier"}
              onClick={() => gameClient.setTool({ kind: "pipe", tier: tier.tier })}
              title={locked ? why : undefined}
            >
              <strong>{tier.name}</strong>
              <span className="small">
                {formatAmount(tier.gold)} g{materials} a tile
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
