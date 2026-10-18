import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { BuildingDetailsDto } from "../api/contracts";
import type { Content } from "../game/content";
import type { TerraformAction } from "../game/Engine";
import { currentGold, useAppStore } from "../state/store";

const KIND_LABELS: Record<string, string> = {
  internal: "own",
  trade: "trade",
  toStorage: "stored",
  fromStorage: "from storage",
  sale: "sold to hub",
  import: "bought from hub",
};

function partnerName(content: Content, type: string): string {
  return content.building(type)?.name ?? type;
}

function Flows({ details, content }: { details: BuildingDetailsDto; content: Content }) {
  if (details.flows.length === 0) {
    return null;
  }
  return (
    <section>
      <h4>Where it goes</h4>
      <ul className="flow-list">
        {details.flows.map((flow, i) => (
          <li key={i}>
            <span className="flow-dot" style={{ background: content.resourceColor(flow.resource) }} />
            <span>
              {flow.direction === "in" ? "from" : "to"} {partnerName(content, flow.partnerType)}
              {flow.kind === "trade" && <span className="muted"> ({flow.partnerOwner})</span>}
            </span>
            <span className="flow-amount">
              {formatAmount(flow.amount)} {content.resourceName(flow.resource).toLowerCase()}/min
            </span>
            <span className="muted flow-kind">
              {KIND_LABELS[flow.kind]}
              {flow.price > 0 && ` @ ${flow.price.toFixed(2)} g`}
            </span>
          </li>
        ))}
      </ul>
    </section>
  );
}

export function BuildingPanel() {
  const details = useAppStore((state) => state.buildingDetails);
  const content = useAppStore((state) => state.content);
  const company = useAppStore((state) => state.company);
  const companyAt = useAppStore((state) => state.companyAt);
  const serverOffset = useAppStore((state) => state.serverOffsetMs);
  const [confirmDemolish, setConfirmDemolish] = useState(false);
  const now = useNow();
  if (details === null || content === null) {
    return null;
  }
  const building = details.building;
  const definition = content.building(building.type);
  if (definition === undefined) {
    return null;
  }
  const mine = company !== null && building.owner === company.id;
  const gold = currentGold({ company, companyAt });
  const underConstruction = building.constructionEndsAt !== null;
  const secondsLeft = underConstruction
    ? (new Date(building.constructionEndsAt!).getTime() - (now + serverOffset)) / 1000
    : 0;
  const nextMilestone = content.nextMilestone(building.level);
  const isProducer = details.inputs.length + details.outputs.length > 0 || details.researchPerMinute > 0;
  const thumbnail = gameClient.textures?.thumbnail(definition.id, definition.size);

  return (
    <aside className="panel building-panel">
      <div className="panel-header">
        <div className="building-title">
          {thumbnail !== undefined && <img className="thumb large" src={thumbnail} alt="" />}
          <div>
            <h3>{definition.name}</h3>
            <div className="muted small">
              {mine ? (
                "Yours"
              ) : (
                <button className="link" onClick={() => void gameClient.visitEstate(building.owner)}>
                  {details.ownerName}
                </button>
              )}{" "}
              · Level {building.level}
              {details.locationMultiplier !== 1 &&
                definition.modifier !== "none" &&
                ` · spot ×${details.locationMultiplier.toFixed(2)}`}
            </div>
          </div>
        </div>
        <button className="icon-button" title="Close (Esc)" onClick={() => gameClient.select(null)}>
          ×
        </button>
      </div>

      {underConstruction ? (
        <p className="status building">
          🏗 Under construction · ready in {formatDuration(Math.max(0, secondsLeft))}
        </p>
      ) : isProducer ? (
        <p className={details.rate > 0.005 ? "status running" : "status idle"}>
          {details.rate > 0.995
            ? "Running at full speed"
            : details.rate > 0.005
              ? `Running at ${Math.round(details.rate * 100)}%`
              : "Idle"}
        </p>
      ) : null}
      {details.issues.length > 0 && (
        <ul className="issues">
          {details.issues.map((issue) => (
            <li key={issue}>{issue}</li>
          ))}
        </ul>
      )}
      {details.complexMembers > 1 && (
        <p className="status">
          🏗 A {details.complexMembers === 9 ? "mega complex" : "complex"} of {details.complexMembers}: the
          output of {details.complexMembers === 9 ? 12 : 5}, upgraded together
        </p>
      )}
      {mine && definition.kind === "terraformer" && building.constructionEndsAt === null && (
        <TerraformTools />
      )}
      {mine && details.repairCost !== null && (
        <button
          className="primary wide"
          disabled={gold < details.repairCost}
          onClick={() => void gameClient.repair(building.id)}
        >
          🔧 Repair · {formatAmount(details.repairCost)} g
        </button>
      )}

      {details.inputs.length > 0 && (
        <section>
          <h4>Needs</h4>
          {details.inputs.map((input) => (
            <Meter
              key={input.resource}
              label={content.resourceName(input.resource)}
              color={content.resourceColor(input.resource)}
              amount={input.amount}
              max={input.max}
            />
          ))}
        </section>
      )}
      {details.outputs.length > 0 && (
        <section>
          <h4>Makes</h4>
          {details.outputs.map((output) => (
            <Meter
              key={output.resource}
              label={content.resourceName(output.resource)}
              color={content.resourceColor(output.resource)}
              amount={output.amount}
              max={output.max}
            />
          ))}
        </section>
      )}
      {details.researchPerMinute > 0 && <p>🔬 {formatAmount(details.researchPerMinute)} research/min</p>}

      {details.contents !== null && details.capacity !== null && (
        <section>
          <h4>
            Stored · {formatAmount(Object.values(details.contents).reduce((a, b) => a + b, 0))} /{" "}
            {formatAmount(details.capacity)}
          </h4>
          {Object.keys(details.contents).length === 0 && (
            <p className="muted small">Empty. Surplus of your buildings on the same pipe goes in here.</p>
          )}
          {Object.entries(details.contents).map(([resource, amount]) => (
            <Meter
              key={resource}
              label={content.resourceName(resource)}
              color={content.resourceColor(resource)}
              amount={amount}
              max={details.capacity!}
              unit=""
            />
          ))}
        </section>
      )}

      {details.throughput !== null && (
        <section>
          <h4>Trading · up to {formatAmount(details.throughput)}/min</h4>
          <p className="muted small">Everything your buildings on its pipe can't use is sold to the hub.</p>
          {mine && (
            <details className="imports">
              <summary>Buy from the hub ({details.imports?.length ?? 0})</summary>
              <div className="import-grid">
                {content.resources
                  .filter((r) => r.era <= 2)
                  .map((resource) => {
                    const enabled = details.imports?.includes(resource.id) ?? false;
                    return (
                      <label key={resource.id} className="check">
                        <input
                          type="checkbox"
                          checked={enabled}
                          onChange={() => void gameClient.setImport(building.id, resource.id, !enabled)}
                        />
                        {resource.name}{" "}
                        <span className="muted">
                          {(resource.price * content.progression.hubSpread).toFixed(2)} g
                        </span>
                      </label>
                    );
                  })}
              </div>
            </details>
          )}
        </section>
      )}

      {definition.kind === "showroom" && details.filter !== null && (
        <section>
          <h4>Showroom · partners pay 10% more for</h4>
          {mine ? (
            <select
              value={details.filter[0] ?? ""}
              onChange={(e) => void gameClient.setShowroomProduct(building.id, e.target.value)}
            >
              <option value="">Choose a product…</option>
              {content.resources
                .filter((r) => r.id !== "energy")
                .map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.name}
                  </option>
                ))}
            </select>
          ) : (
            <p className="small">
              {details.filter[0] !== undefined ? content.resourceName(details.filter[0]) : "Nothing yet"}
            </p>
          )}
        </section>
      )}

      {definition.kind === "valve" && details.filter !== null && (
        <section>
          <h4>Valve · {details.filter.length === 0 ? "everything passes" : "only these pass"}</h4>
          {mine && (
            <div className="import-grid">
              {content.resources
                .filter((r) => r.era <= 2)
                .map((resource) => {
                  const enabled = details.filter?.includes(resource.id) ?? false;
                  return (
                    <label key={resource.id} className="check">
                      <input
                        type="checkbox"
                        checked={enabled}
                        onChange={() => void gameClient.setValveFilter(building.id, resource.id, !enabled)}
                      />
                      {resource.name}
                    </label>
                  );
                })}
            </div>
          )}
        </section>
      )}

      {definition.kind === "tollMeter" && (
        <section>
          <h4>Toll · {(details.setting * 100).toFixed(1)}% of what passes</h4>
          {mine && (
            <input
              className="slider"
              type="range"
              min={0}
              max={5}
              step={0.5}
              value={details.setting * 100}
              onChange={(event) =>
                void gameClient.setBuildingSetting(building.id, Number(event.target.value) / 100)
              }
            />
          )}
          <p className="small muted">
            Others pay it on everything flowing through this pipe. You get half (+
            {formatAmount(details.tollIncome)} g/min), the other half disappears.
          </p>
        </section>
      )}

      {details.pumpFactor < 1 && (
        <p className="status idle">
          Its network needs more pump stations: everything in it runs at{" "}
          {Math.round(details.pumpFactor * 100)}%.
        </p>
      )}

      <Flows details={details} content={content} />

      {mine && details.upgradeCost !== null && !underConstruction && (
        <section className="upgrade">
          <h4>
            Upgrade · output +10% per level
            {nextMilestone !== null && <span className="muted"> · level {nextMilestone} doubles it</span>}
          </h4>
          <div className="button-row">
            <button
              className="primary"
              disabled={gold < details.upgradeCost}
              onClick={() => void gameClient.upgrade(building.id, 1)}
            >
              Level {building.level + 1} · {formatAmount(details.upgradeCost)} g
            </button>
            {details.upgradeCost10 !== null && (
              <button
                disabled={gold < details.upgradeCost10}
                onClick={() => void gameClient.upgrade(building.id, 10)}
              >
                +10 · {formatAmount(details.upgradeCost10)} g
              </button>
            )}
            {details.maxAffordableLevels > 1 && (
              <button onClick={() => void gameClient.upgrade(building.id, details.maxAffordableLevels)}>
                Max +{details.maxAffordableLevels}
              </button>
            )}
          </div>
        </section>
      )}

      {mine && definition.kind !== "hq" && (
        <div className="button-row footer-row">
          <button onClick={() => gameClient.setTool({ kind: "move", building: building.id })}>Move</button>
          {confirmDemolish ? (
            <>
              <button className="danger" onClick={() => void gameClient.demolish(building.id)}>
                Demolish (+{formatAmount(details.demolishRefund)} g)
              </button>
              <button onClick={() => setConfirmDemolish(false)}>Keep</button>
            </>
          ) : (
            <button onClick={() => setConfirmDemolish(true)}>Demolish…</button>
          )}
        </div>
      )}
    </aside>
  );
}

function Meter({
  label,
  color,
  amount,
  max,
  unit = "/min",
}: {
  label: string;
  color: string;
  amount: number;
  max: number;
  unit?: string;
}) {
  const fraction = max > 0 ? Math.min(1, amount / max) : 0;
  return (
    <div className="meter">
      <div className="meter-label">
        <span>{label}</span>
        <span>
          {formatAmount(amount)} / {formatAmount(max)}
          {unit}
        </span>
      </div>
      <div className="meter-bar">
        <div className="meter-fill" style={{ width: `${fraction * 100}%`, background: color }} />
      </div>
    </div>
  );
}

const TERRAFORM: Array<{ action: TerraformAction; name: string; text: string; cost: number }> = [
  { action: "drain", name: "Drain", text: "Water becomes grass", cost: 2000 },
  { action: "lake", name: "Dig a lake", text: "Grass becomes water (for shore buildings)", cost: 1000 },
  { action: "raise", name: "Raise a hill", text: "Hills catch 20% more wind", cost: 3000 },
  { action: "flatten", name: "Flatten", text: "Hills become grass", cost: 1500 },
  { action: "forest", name: "Plant a forest", text: "Grass becomes forest", cost: 500 },
];

/** The Terraformer (GDD §11.1): pick an action, then click the tiles of your estate */
function TerraformTools() {
  const content = useAppStore((s) => s.content)!;
  const [resource, setResource] = useState("ironOre");
  return (
    <section>
      <h4>Terraform · pick an action, then click tiles</h4>
      <div className="terraform-grid">
        {TERRAFORM.map((t) => (
          <button
            key={t.action}
            title={t.text}
            onClick={() => gameClient.setTool({ kind: "terraform", action: t.action })}
          >
            {t.name} · {formatAmount(t.cost)} g
          </button>
        ))}
      </div>
      <div className="seed-row">
        <select value={resource} onChange={(e) => setResource(e.target.value)}>
          {content.resources
            .filter((r) => r.tier === 1 && r.id !== "energy" && r.id !== "water")
            .map((r) => (
              <option key={r.id} value={r.id}>
                {r.name}
              </option>
            ))}
        </select>
        <button
          title="A new deposit (one star), or one more star on a deposit of that resource"
          onClick={() => gameClient.setTool({ kind: "terraform", action: "seedDeposit", resource })}
        >
          Seed a deposit · 20k g + 50 steel
        </button>
      </div>
    </section>
  );
}
