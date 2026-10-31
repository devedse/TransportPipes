import { formatAmount } from "../app/format";
import { gameClient } from "../app/client";
import { newPipeTiles, pipeGold, upgradablePipeTiles } from "../game/rules";
import { isDeep, zoneAt } from "../game/layout";
import { insidePipe, LANE_NAMES } from "../game/inside";
import { useAppStore } from "../state/store";

const TERRAIN_NAMES: Record<string, string> = {
  grass: "Grassland",
  hills: "Hills",
  forest: "Forest",
  beach: "Beach",
  water: "Water",
  desert: "Desert",
  mountain: "Mountain",
  road: "Corridor",
  city: "Hub city",
  rock: "Solid rock (dig it out)",
  cavern: "Cavern",
};

/** A line of information about what is under the pointer and what the tool would do */
export function HoverTip() {
  const tile = useAppStore((state) => state.hoverTile);
  const tool = useAppStore((state) => state.tool);
  const placement = useAppStore((state) => state.placement);
  const route = useAppStore((state) => state.pipeRoute);
  const content = useAppStore((state) => state.content);
  const company = useAppStore((state) => state.company);
  const justBuilt = useAppStore((state) => state.justBuilt);
  const world = gameClient.world;
  if (tile === null || content === null || world === null) {
    return null;
  }
  const info = world.tile(tile.x, tile.y);
  let text: string | null = null;
  let tone = "";

  if (tool.kind === "build") {
    const definition = content.building(tool.type);
    if (placement !== null && definition !== undefined) {
      if (placement.ok) {
        const outputs = Object.entries(definition.out)
          .map(
            ([resource, amount]) =>
              `${formatAmount(amount * placement.multiplier)} ${content.resourceName(resource).toLowerCase()}/min`,
          )
          .join(", ");
        const spot = definition.modifier !== "none" ? ` · this spot ×${placement.multiplier.toFixed(2)}` : "";
        text = `Click to build ${definition.name}${outputs ? ` → ${outputs}` : ""}${spot}`;
        tone = "good";
      } else if (
        justBuilt !== null &&
        tile.x >= justBuilt.x &&
        tile.x < justBuilt.x + justBuilt.size &&
        tile.y >= justBuilt.y &&
        tile.y < justBuilt.y + justBuilt.size
      ) {
        // The pointer still rests on what it just built: not "in the way" (Cleo's replay); a scaffold says it waits
        // (run 4: "is being built", and it waited for bricks to the end)
        const placed =
          world === null
            ? undefined
            : [...world.buildings.values()].find((t) => t.x === justBuilt.x && t.y === justBuilt.y);
        text = placed?.constructionEndsAt?.startsWith("9999")
          ? `${justBuilt.name} waits here for its materials (its panel says what and from whom) · move to place another (Esc to stop)`
          : `${justBuilt.name} is being built here · move to place another (Esc to stop)`;
        tone = "good";
      } else {
        text = placement.reason;
        tone = "bad";
      }
    }
  } else if (tool.kind === "pipe" && company !== null) {
    if (route !== null && route.length > 0) {
      const tiles = newPipeTiles(world, company, route, content);
      const upgrades = upgradablePipeTiles(world, company, route, tool.tier);
      const tier = content.pipeTiers.find((t) => t.tier === tool.tier);
      const gold = pipeGold(world, tiles, tier?.gold ?? 2) + upgrades.length * (tier?.gold ?? 2);
      const parts = [
        tiles.length > 0 ? `${tiles.length} pipe tiles` : null,
        upgrades.length > 0 ? `${upgrades.length} upgraded to ${tier?.name ?? "this pipe"}` : null,
      ].filter((t) => t !== null);
      // What it loses over its length, before it is laid (the owner, 2026-10-31: "if things are lost due to pipe
      // length always display this"), and the next pipe's when there is one
      const loss = (lossPerTile: number) => Math.round((1 - (1 - lossPerTile) ** route.length) * 100);
      const better = content.pipeTiers.find((t) => t.tier === tool.tier + 1);
      const lost =
        tier !== undefined && tier.lossPerTile > 0
          ? ` · loses ${loss(tier.lossPerTile)}% over its ${route.length} tiles${better !== undefined ? ` (${better.name}: ${loss(better.lossPerTile)}%)` : ""}`
          : "";
      text = `${parts.join(" · ") || "Nothing new"} · ${formatAmount(gold)} g${lost} · release to build`;
      tone = "good";
    } else {
      text = "Drag from a building to another to lay a pipe · right-click to stop";
    }
  } else if (tool.kind === "bulldoze") {
    text =
      "Click or drag over your pipes to remove them (half the cost back); click a building to demolish it · right-click to stop";
  } else if (tool.kind === "move") {
    text = placement?.ok === false ? placement.reason : "Click where it should go · right-click to cancel";
    tone = placement?.ok === false ? "bad" : "";
  } else if (tool.kind === "blueprintArea") {
    text = "Drag a rectangle over your plot to save it as a blueprint (right-click or Esc to stop)";
  } else if (tool.kind === "blueprint") {
    const blueprint = tool.blueprint;
    if (placement !== null && placement.ok && placement.reason === null) {
      text = `Click to build "${blueprint.name}" · ${formatAmount(blueprint.cost)} g`;
      tone = "good";
    } else if (placement !== null) {
      text = placement.ok
        ? `Click to build what fits: ${placement.reason}`
        : (placement.reason ?? "It doesn't fit here");
      tone = placement.ok ? "" : "bad";
    }
  } else if (tool.kind === "terraform") {
    const names: Record<string, string> = {
      drain: "Drain",
      lake: "Dig a lake",
      raise: "Raise a hill",
      flatten: "Flatten",
      forest: "Plant a forest",
      seedDeposit: `Seed ${tool.resource !== undefined ? content.resourceName(tool.resource).toLowerCase() : "a deposit"}`,
    };
    text = `${names[tool.action] ?? tool.action}: click tiles of your plot (right-click or Esc to stop)`;
  }

  // Look inside a pipe (1.4 §9): what runs in each lane, and how full the pipe between the two buildings is
  if (text === null && tool.kind === "select" && world.buildingAt(tile.x, tile.y) === undefined) {
    const conductor = world.conductorAt(tile.x, tile.y);
    if (conductor?.kind === "pipe") {
      // One line a good in each lane: what all its flows here carry, and the fullest pipe pair among them
      const lanes = new Map<
        string,
        { lane: string; resource: string; perMinute: number; sent: number; full: number }
      >();
      for (const t of insidePipe(world, content, tile.x, tile.y)) {
        const key = `${t.kind}:${t.flow.resource}`;
        const line = lanes.get(key) ?? {
          lane: LANE_NAMES[t.kind],
          resource: t.flow.resource,
          perMinute: 0,
          sent: 0,
          full: 0,
        };
        line.perMinute += t.flow.delivered;
        line.sent += t.flow.sent;
        line.full = Math.max(line.full, t.full);
        lanes.set(key, line);
      }
      const tier = content.pipeTiers.find((t) => t.tier === conductor.tier)?.name ?? "Pipe";
      const lines = [...lanes.values()];
      // A pipe that waits for a better one's goods says so first
      const waiting = (company?.wants ?? []).filter((w) => {
        const at = w.pipeAt ?? [];
        for (let i = 0; i < at.length; i += 2) {
          if (at[i] === tile.x && at[i + 1] === tile.y) {
            return true;
          }
        }
        return false;
      });
      text =
        waiting.length > 0
          ? `Becomes a ${waiting[0]!.name} once its goods are in: ${waiting
              .map(
                (w) =>
                  `${formatAmount(w.got)} of ${formatAmount(w.amount)} ${content.resourceName(w.resource).toLowerCase()}`,
              )
              .join(", ")} (Yard › What you're waiting for)`
          : lines.length === 0
            ? `${tier}: nothing runs through here now`
            : // Only what passes this tile (playtest: its numbers didn't match the network's totals, unexplained)
              `${tier}, what runs through this tile: ${lines
                .slice(0, 4)
                .map(
                  (t) =>
                    `${t.lane}: ${content.resourceName(t.resource).toLowerCase()} ${
                      t.sent - t.perMinute > Math.max(0.05, t.sent * 0.005)
                        ? `${t.sent < 100 ? t.sent.toFixed(1) : formatAmount(t.sent)}/min sent, ${t.perMinute < 100 ? t.perMinute.toFixed(1) : formatAmount(t.perMinute)} arrive (${Math.round((100 * (t.sent - t.perMinute)) / t.sent)}% lost)`
                        : `${formatAmount(t.perMinute)}/min`
                    } (${Math.round(t.full * 100)}% full)`,
                )
                .join(
                  " · ",
                )}${lines.length > 4 ? ` · ${lines.length - 4} more` : ""} · click it for the whole network's totals · hold Alt to see every pipe`;
    }
  }

  if (text === null && info !== null) {
    const zone = zoneAt(tile.x, tile.y);
    const parts = [TERRAIN_NAMES[info.terrain] ?? info.terrain];
    if (info.terrain === "forest") {
      parts[0] = "Forest: wood for a Lumber Camp";
    }
    if (info.deposit !== null) {
      parts.push(`${content.resourceName(info.deposit)} ${"★".repeat(info.richness)}`);
    }
    if (zone.kind === "estate" && info.terrain !== "water" && !isDeep(tile.y)) {
      parts.push(`wind ×${info.wind.toFixed(2)}`, `sun ×${info.sun.toFixed(2)}`);
    }
    const estate = world.estateAt(tile.x, tile.y);
    if (estate?.ownerName) {
      parts.push(estate.ownerName);
    }
    text = parts.join(" · ");
  }
  if (text === null) {
    return null;
  }
  return <div className={`panel hover-tip ${tone}`}>{text}</div>;
}
