import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import type { BuildingDefinition } from "../api/contracts";
import { CATEGORY_NAMES, CATEGORY_ORDER, type Content } from "../game/content";
import { currentGold, setState, useAppStore } from "../state/store";

function Recipe({ definition, content }: { definition: BuildingDefinition; content: Content }) {
  const part = (amounts: Record<string, number>) =>
    Object.entries(amounts).map(([resource, amount]) => (
      <span key={resource} className="amount" style={{ color: content.resourceColor(resource) }}>
        {formatAmount(amount)} {content.resource(resource)?.icon ?? ""} {content.resourceName(resource)}
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

export function BuildMenu() {
  const open = useAppStore((state) => state.buildMenuOpen);
  const content = useAppStore((state) => state.content);
  const company = useAppStore((state) => state.company);
  const companyAt = useAppStore((state) => state.companyAt);
  const era = useAppStore((state) => state.era);
  const selectedCategory = useAppStore((state) => state.buildCategory);
  const tool = useAppStore((state) => state.tool);
  const deep = useAppStore((state) => state.deepView);
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
  const buildings = content.buildings
    .filter((t) => t.category === category && inLayer(t))
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
                disabled={lock !== null}
                onClick={() => gameClient.setTool({ kind: "build", type: definition.id })}
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
                  {lock !== null ? (
                    <div className="lock">🔒 {lock}</div>
                  ) : (
                    <>
                      <div className="build-cost">
                        <span className="gold-text">{formatAmount(cost)} g</span>
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
