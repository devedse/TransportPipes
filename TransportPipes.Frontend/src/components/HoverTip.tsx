import { formatAmount } from "../app/format";
import { gameClient } from "../app/client";
import { newPipeTiles, pipeGold, upgradablePipeTiles } from "../game/rules";
import { isDeep, zoneAt } from "../game/layout";
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
        // The pointer still rests on what it just built: not "in the way" (Cleo's replay)
        text = `${justBuilt.name} is being built here · move to place another (Esc to stop)`;
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
      text = `${parts.join(" · ") || "Nothing new"} · ${formatAmount(gold)} g · release to build`;
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
    text = "Drag a rectangle over your estate to save it as a blueprint (right-click or Esc to stop)";
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
    text = `${names[tool.action] ?? tool.action}: click tiles of your estate (right-click or Esc to stop)`;
  }

  if (text === null && info !== null) {
    const zone = zoneAt(tile.x, tile.y);
    const parts = [TERRAIN_NAMES[info.terrain] ?? info.terrain];
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
