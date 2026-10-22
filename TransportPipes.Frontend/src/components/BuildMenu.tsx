import { gameClient } from "../app/client";
import { formatAmount, formatDuration, possessive } from "../app/format";
import type { BuildingDefinition, LandDto } from "../api/contracts";
import { CATEGORY_NAMES, CATEGORY_ORDER, type Content } from "../game/content";
import { usePhone } from "../app/usePhone";
import { currentGold, setState, toast, useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

function Recipe({ definition, content }: { definition: BuildingDefinition; content: Content }) {
  const part = (amounts: Record<string, number>) =>
    Object.entries(amounts).map(([resource, amount]) => (
      <span key={resource} className="amount">
        <ResourceIcon resource={resource} />
        {formatAmount(amount)} {content.resourceName(resource)}
      </span>
    ));
  const inputs = part(definition.in);
  const outputs = part(definition.out);
  if (definition.researchPerMinute > 0) {
    outputs.push(
      <span key="research" className="amount research">
        {definition.researchPerMinute} 🔬
      </span>,
    );
  }
  if (definition.kind === "storage") {
    outputs.push(<span key="capacity">holds {formatAmount(definition.capacity)}</span>);
  }
  if (definition.kind === "tradingPost") {
    outputs.push(<span key="throughput">sells {formatAmount(definition.throughput)}/min</span>);
  }
  return (
    <div className="recipe">
      {inputs.length > 0 && (
        <>
          {inputs}
          <span className="arrow">→</span>
        </>
      )}
      {outputs}
      {inputs.length + outputs.length > 0 && <span className="muted"> /min</span>}
    </div>
  );
}

/** "Cleo", "Cleo and Dev" */
function names(items: string[]): string {
  return items.length <= 1 ? (items[0] ?? "") : `${items.slice(0, -1).join(", ")} and ${items.at(-1)}`;
}

/**
 * Dealt land (step 5): where a land good is missing, who makes it and which House sells it, and the building that gets
 * it by itself ("there's no copper on your land. Cleo makes it, and Bertha ⚙ sells it: build a Smelter, and its copper
 * comes by itself"); null when it lies on your land, or on old land
 */
function landLine(definition: BuildingDefinition, land: LandDto | null, content: Content): string | null {
  if (land === null) {
    return null;
  }
  const good = Object.keys(definition.out).find((t) =>
    ["copperOre", "coal", "sand", "wood", "stone"].includes(t),
  );
  if (
    good === undefined ||
    land.goods.includes(good) ||
    Object.keys(definition.in).some((t) => t !== "energy")
  ) {
    return null;
  }
  const word = content.resourceName(good).toLowerCase().replace(" ore", "");
  const seats = land.station.filter((t) => t.goods.includes(good));
  // "Makes" only when someone does (run 2: Mia was told Jo makes coal, and Jo never built a Coal Pit)
  const makers = seats
    .filter((t) => t.owner !== null && !t.house && t.makes.includes(good))
    .map((t) => t.owner!);
  const holders = seats
    .filter((t) => t.owner !== null && !t.house && !t.makes.includes(good))
    .map((t) => t.owner!);
  const houses = seats.filter((t) => t.owner !== null && t.house).map((t) => `${t.owner} ⚙`);
  const free = seats.some((t) => t.owner === null);
  const who = [
    makers.length > 0 && `${names(makers)} ${makers.length === 1 ? "makes" : "make"} it`,
    houses.length > 0 && `${names(houses)} ${houses.length === 1 ? "sells" : "sell"} it`,
    makers.length === 0 &&
      holders.length > 0 &&
      `it lies on ${names(holders)}${possessive(names(holders))} land, but nobody makes it yet`,
    makers.length === 0 &&
      holders.length === 0 &&
      houses.length === 0 &&
      free &&
      "a free seat of your station holds it",
  ]
    .filter(Boolean)
    .join(", and ");
  const user = content.buildings
    .filter((t) => t.buildable && Object.keys(t.in).includes(good) && t.unlock.era <= 1)
    .sort((a, b) => a.unlock.level - b.unlock.level)[0];
  // Plainly: the user building uses the good, and the Yard brings it (Mia: "I don't understand how a smelter gives me coal")
  const use =
    user !== undefined && (makers.length > 0 || houses.length > 0)
      ? `: ${/^[aeiou]/i.test(user.name) ? "an" : "a"} ${user.name} uses it, and the Yard brings it to one by itself`
      : "";
  return `There's no ${word} on your land.${who === "" ? "" : ` ${who.charAt(0).toUpperCase()}${who.slice(1)}`}${use}.`;
}

/**
 * What a building uses besides energy and land goods, and where it would come from (run 2: three players built a Lab
 * without knowing it needs iron ingots): "Uses 2 iron ingots a minute: your Small Factory makes them", or "you make
 * none yet: a Small Factory does"; null when it uses nothing such
 */
function supplyLine(definition: BuildingDefinition, companyId: number, content: Content): string | null {
  const land = ["copperOre", "coal", "sand", "wood", "stone"];
  const good = Object.keys(definition.in).find((t) => t !== "energy" && !land.includes(t));
  if (good === undefined) {
    return null;
  }
  const word = content.resourceName(good).toLowerCase();
  const words = word.endsWith("ingot") ? `${word}s` : word;
  const amount = `${formatAmount(definition.in[good]!)} ${definition.in[good] === 1 ? word : words} a minute`;
  const own = [...(gameClient.world?.buildings.values() ?? [])].find(
    (t) =>
      t.owner === companyId && t.type !== definition.id && (content.building(t.type)?.out[good] ?? 0) > 0,
  );
  if (own !== undefined) {
    return `Uses ${amount}: your ${content.building(own.type)!.name} makes ${words.endsWith("s") ? "them" : "it"}`;
  }
  const maker = content.buildings
    .filter((t) => t.buildable && (t.out[good] ?? 0) > 0)
    .sort(
      (a, b) => a.unlock.era - b.unlock.era || a.unlock.level - b.unlock.level || a.cost.gold - b.cost.gold,
    )[0];
  return `Uses ${amount}: you make none yet${maker !== undefined ? `, ${/^[aeiou]/i.test(maker.name) ? "an" : "a"} ${maker.name} does` : ""}`;
}

export function BuildMenu() {
  const open = useAppStore((state) => state.buildMenuOpen);
  const content = useAppStore((state) => state.content);
  const company = useAppStore((state) => state.company);
  const companyAt = useAppStore((state) => state.companyAt);
  const era = useAppStore((state) => state.era);
  const selectedCategory = useAppStore((state) => state.buildCategory);
  const tool = useAppStore((state) => state.tool);
  const deep = useAppStore((state) => state.deepView);
  const phone = usePhone();
  if (!open || content === null || company === null) {
    return null;
  }
  const gold = currentGold({ company, companyAt });
  // What can be built on the layer the camera looks at
  const inLayer = (t: BuildingDefinition) =>
    t.buildable && (deep ? t.layer !== "surface" : t.layer !== "deep") && t.unlock.era <= era + 1;
  const categories = CATEGORY_ORDER.filter(
    (c) => !deep || content.buildings.some((t) => t.category === c && inLayer(t)),
  );
  const category = categories.includes(selectedCategory)
    ? selectedCategory
    : (categories[0] ?? selectedCategory);
  // What uses goods shows under Processing too (run 2: Mia-B looked for the Lab there and under Commerce)
  const listed = (t: BuildingDefinition) =>
    t.category === category ||
    (category === "processing" && t.category === "company" && Object.keys(t.in).some((r) => r !== "energy"));
  const buildings = content.buildings
    .filter((t) => listed(t) && inLayer(t))
    .sort(
      (a, b) => a.unlock.era - b.unlock.era || a.unlock.level - b.unlock.level || a.cost.gold - b.cost.gold,
    );

  return (
    <div className="panel build-menu">
      <div className="panel-header">
        <span className="panel-title">{deep ? "Build in the Deep Level" : "Build"}</span>
        <button
          className="icon-button"
          title="Close (Esc)"
          onClick={() => setState({ buildMenuOpen: false })}
        >
          ×
        </button>
      </div>
      <div className="category-tabs">
        {categories.map((c) => (
          <button
            key={c}
            className={c === category ? "chip active" : "chip"}
            onClick={() => setState({ buildCategory: c })}
          >
            {CATEGORY_NAMES[c]}
          </button>
        ))}
      </div>
      <ul className="build-list">
        {buildings.map((definition) => {
          const lock = content.lockReason(definition, company, era);
          const missing = landLine(definition, company.land, content);
          const copies = company.buildingCounts[definition.id] ?? 0;
          const cost = content.buildCost(definition, copies);
          const affordable = gold >= cost;
          const active = tool.kind === "build" && tool.type === definition.id;
          const thumbnail = gameClient.textures?.thumbnail(definition.id, definition.size);
          return (
            <li key={definition.id}>
              <button
                className={[
                  "build-item",
                  active ? "active" : "",
                  lock !== null ? "locked" : "",
                  !affordable ? "expensive" : "",
                ].join(" ")}
                disabled={lock !== null || missing !== null}
                onClick={() => {
                  gameClient.setTool({ kind: "build", type: definition.id });
                  // On a phone the menu covers half the map, where the building goes (Ben's run 2 evening)
                  if (phone) {
                    setState({ buildMenuOpen: false });
                    toast(`Tap where your ${definition.name} goes`);
                  }
                }}
                title={definition.description}
              >
                {thumbnail !== undefined && <img className="thumb" src={thumbnail} alt="" />}
                <div className="build-info">
                  <div className="build-name">
                    {definition.name}
                    {definition.size > 1 && (
                      <span className="size-badge">
                        {definition.size}×{definition.size}
                      </span>
                    )}
                  </div>
                  {missing !== null ? (
                    <div className="land-line">{missing}</div>
                  ) : lock !== null ? (
                    <div className="lock">🔒 {lock}</div>
                  ) : (
                    <>
                      <div className="build-cost">
                        <span className="gold-text">{formatAmount(cost)} g</span>
                        {/* Every copy costs more: the price is for the next one (Cleo's replay read it as an overcharge) */}
                        {copies > 0 && <span className="muted"> for another</span>}
                        {Object.entries(definition.cost.materials).map(([resource, amount]) => (
                          <span key={resource}>
                            {" "}
                            + {formatAmount(amount)} {content.resourceName(resource).toLowerCase()}
                          </span>
                        ))}
                        <span className="muted"> · {formatDuration(definition.buildSeconds)}</span>
                        {copies > 0 && <span className="muted"> · you have {copies}</span>}
                      </div>
                      <Recipe definition={definition} content={content} />
                      {supplyLine(definition, company.id, content) !== null && (
                        <div className="supply-line small">{supplyLine(definition, company.id, content)}</div>
                      )}
                    </>
                  )}
                </div>
              </button>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
