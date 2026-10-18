import { formatAmount } from "../app/format";
import { gameClient } from "../app/client";
import { newPipeTiles, pipeGold } from "../game/rules";
import { zoneAt } from "../game/layout";
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
};

/** A line of information about what is under the pointer and what the tool would do */
export function HoverTip() {
  const tile = useAppStore((state) => state.hoverTile);
  const tool = useAppStore((state) => state.tool);
  const placement = useAppStore((state) => state.placement);
  const route = useAppStore((state) => state.pipeRoute);
  const content = useAppStore((state) => state.content);
  const company = useAppStore((state) => state.company);
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
      } else {
        text = placement.reason;
        tone = "bad";
      }
    }
  } else if (tool.kind === "pipe" && company !== null) {
    if (route !== null && route.length > 0) {
      const tiles = newPipeTiles(world, company, route);
      const tier = content.pipeTiers.find((t) => t.tier === tool.tier);
      const gold = pipeGold(world, tiles, tier?.gold ?? 2);
      text = `${tiles.length} pipe tiles · ${formatAmount(gold)} g · release to build`;
      tone = "good";
    } else {
      text = "Drag from a building to another to lay a pipe";
    }
  } else if (tool.kind === "bulldoze") {
    text =
      "Click or drag over your pipes to remove them (half the cost back); click a building to demolish it";
  } else if (tool.kind === "move") {
    text = placement?.ok === false ? placement.reason : "Click where it should go";
    tone = placement?.ok === false ? "bad" : "";
  }

  if (text === null && info !== null) {
    const zone = zoneAt(tile.x, tile.y);
    const parts = [TERRAIN_NAMES[info.terrain] ?? info.terrain];
    if (info.deposit !== null) {
      parts.push(`${content.resourceName(info.deposit)} ${"★".repeat(info.richness)}`);
    }
    if (zone.kind === "estate" && info.terrain !== "water") {
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
