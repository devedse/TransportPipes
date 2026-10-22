import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { BuildingDetailsDto, BuildingFlowDto, BuildingTradeDto } from "../api/contracts";
import type { Content } from "../game/content";
import type { TerraformAction } from "../game/Engine";
import { currentGold, useAppStore } from "../state/store";
import { levelTier, nextTier } from "../game/levels";
import { NetworkSummary } from "./NetworkSummary";
import { ResourceIcon } from "./ResourceIcon";

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

/** A group's partners in a few words: "Wind Moily", or "9 Iron Mines, 2 Wind Moilies" */
function partners(content: Content, flows: BuildingFlowDto[]): string {
  const counts = new Map<string, number>();
  for (const flow of flows) {
    counts.set(flow.partnerType, (counts.get(flow.partnerType) ?? 0) + 1);
  }
  return [...counts.entries()]
    .sort((a, b) => b[1] - a[1])
    .map(([type, count]) =>
      count === 1 ? partnerName(content, type) : `${count}× ${partnerName(content, type)}`,
    )
    .join(", ");
}

/**
 * A building's trade with its neighbours (step 4), for its owner: who takes its goods and who sends it its inputs, both
 * ends named; who could use its goods and what the project wants of them, each with Send ▸ (the same tap lays the pipe
 * to the gate when its pipes don't reach it); 💛 Thanks to a neighbour who sends it something.
 */
/** The Send that is a building's one fix button (F2, step 4): the project's want, else the first bid, when its pipes don't reach the gate */
function primarySend(trade: BuildingTradeDto | null) {
  if (trade === null || trade.reachesGate || trade.gatePipe === null || trade.sends >= trade.maxSends) {
    return null;
  }
  const want = trade.project.find((t) => !t.sent);
  if (want !== undefined) {
    return {
      resource: want.resource,
      to: null,
      name: `the ${want.stage}`,
      building: null,
      pipe: trade.gatePipe,
    };
  }
  const bid = trade.bids[0];
  return bid === undefined
    ? null
    : {
        resource: bid.resource,
        to: bid.buyer,
        name: bid.buyerName,
        building: bid.buildingId,
        pipe: trade.gatePipe,
      };
}

function NeighbourTrade({ trade, content }: { trade: BuildingTradeDto; content: Content }) {
  // The Send the panel's head offers already isn't offered twice
  const primary = primarySend(trade);
  const good = (resource: string) => content.resourceName(resource).toLowerCase();
  const full = trade.sends >= trade.maxSends;
  const pipe = trade.reachesGate ? null : trade.gatePipe;
  const sendLabel = (to: string) =>
    pipe === null
      ? `Send to ${to} ▸`
      : `Lay the pipe and send to ${to} · ${pipe.newTiles} tiles · ${formatAmount(pipe.cost)} g`;
  const send = (resource: string, to: number | null, name: string, building: number | null = null) =>
    void gameClient.sendGood(resource, to, name, building, pipe?.tiles ?? null);
  return (
    <section className="neighbour-trade">
      <h4>Neighbours</h4>
      <ul className="trade-lines">
        {trade.takers.map((t) => (
          <li key={`to-${t.partner}-${t.resource}`}>
            <ResourceIcon resource={t.resource} />
            <span>
              to <strong>{t.partnerName}</strong>
              {t.partnerBuilding !== null && `'s ${t.partnerBuilding}`} · {formatAmount(t.perMinute)}/min ·{" "}
              {t.price.toFixed(2)} g{t.sent && " 💛"}
            </span>
            {!t.house &&
              (t.sent ? (
                <button
                  className="link small"
                  onClick={() => void gameClient.stopSend(t.resource, t.partner)}
                >
                  stop
                </button>
              ) : (
                <button
                  className="small"
                  disabled={full}
                  title="A standing promise: they come first when your spare gets short"
                  onClick={() => send(t.resource, t.partner, t.partnerName, t.partnerBuildingId)}
                >
                  Keep {t.partnerName} first
                </button>
              ))}
          </li>
        ))}
        {trade.project.map((p) => (
          <li key={`project-${p.resource}`}>
            <span aria-hidden="true">🏗</span>
            <span>
              The {p.stage} wants {formatAmount(p.wants)} {good(p.resource)}
              {p.sent
                ? ` · yours goes there first${p.perMinute > 0 ? `: ${formatAmount(p.perMinute)}/min 💛` : ""}`
                : " · you make it"}
            </span>
            {p.sent ? (
              <button className="link small" onClick={() => void gameClient.stopSend(p.resource, null)}>
                stop
              </button>
            ) : primary !== null && primary.to === null && primary.resource === p.resource ? null : (
              <button
                className="small"
                disabled={full}
                onClick={() => send(p.resource, null, `the ${p.stage}`)}
              >
                {pipe === null ? "Send ▸" : sendLabel(`the ${p.stage}`)}
              </button>
            )}
          </li>
        ))}
        {trade.bids.map((b) => (
          <li key={`bid-${b.buyer}-${b.resource}`}>
            <ResourceIcon resource={b.resource} />
            <span>
              <strong>{b.buyerName}</strong>
              {b.building !== null && `'s ${b.building}`} could use {formatAmount(b.perMinute)}/min
              {b.house !== null && <span className="muted"> · it buys from {b.house} ⚙ now</span>}
            </span>
            {(primary === null || primary.to !== b.buyer || primary.resource !== b.resource) && (
              <button
                className="small"
                disabled={full}
                onClick={() => send(b.resource, b.buyer, b.buyerName, b.buildingId)}
              >
                {sendLabel(b.buyerName)}
              </button>
            )}
          </li>
        ))}
        {trade.suppliers.map((t) => (
          <li key={`from-${t.partner}-${t.resource}`}>
            <ResourceIcon resource={t.resource} />
            <span>
              {good(t.resource)} {formatAmount(t.perMinute)}/min from <strong>{t.partnerName}</strong>
              {t.house ? " ⚙" : t.sent ? ", sent 💛" : ""} · {t.price.toFixed(2)} g
            </span>
            {t.canThank && (
              <button
                className="small"
                onClick={() => void gameClient.thank(t.partner, t.resource, t.partnerName)}
              >
                💛 Thanks
              </button>
            )}
          </li>
        ))}
      </ul>
      {(trade.sends > 0 || full) && (
        <p className="muted small">
          You send {trade.sends} of {trade.maxSends} things{full && ": stop one to send another"}.
        </p>
      )}
    </section>
  );
}

/**
 * Where a building's resources come from and go: one row per resource (and kind), with the total, the gold and
 * its partners; open a row for every single flow. A trading post shows how much of what it can sell it sells.
 */
function Flows({ details, content }: { details: BuildingDetailsDto; content: Content }) {
  if (details.flows.length === 0) {
    return null;
  }
  const groups = new Map<string, BuildingFlowDto[]>();
  for (const flow of details.flows) {
    const key = `${flow.direction}:${flow.resource}:${flow.kind}`;
    groups.set(key, [...(groups.get(key) ?? []), flow]);
  }
  const rows = [...groups.values()]
    .map((flows) => ({
      flows,
      first: flows[0]!,
      amount: flows.reduce((sum, t) => sum + t.amount, 0),
      gold: flows.reduce((sum, t) => sum + t.amount * t.price, 0),
    }))
    .sort((a, b) => b.gold - a.gold || b.amount - a.amount);
  const throughput = details.throughput;
  const traded = details.flows
    .filter((t) => t.kind === "sale" || t.kind === "import")
    .reduce((sum, t) => sum + t.amount, 0);
  const sales = details.flows
    .filter((t) => t.kind === "sale")
    .reduce((sum, t) => sum + t.amount * t.price, 0);
  return (
    <section>
      <h4>Where it goes</h4>
      {throughput !== null && throughput > 0 && (
        <>
          <Meter label="Selling" color="#c9963a" amount={traded} max={throughput} />
          <p className="muted small">
            {formatAmount(sales)} g/min from the hub
            {traded >= throughput * 0.98 && " · full: upgrade it or build another to sell more"}
          </p>
        </>
      )}
      <ul className="flow-list flow-groups">
        {rows.map(({ flows, first, amount, gold }) => (
          <li key={`${first.direction}:${first.resource}:${first.kind}`}>
            <details>
              <summary>
                <ResourceIcon resource={first.resource} />
                <span>
                  {first.direction === "in" ? "from" : "to"} {partners(content, flows)}
                </span>
                <span className="flow-amount">
                  {formatAmount(amount)} {content.resourceName(first.resource).toLowerCase()}/min
                </span>
                <span className="muted flow-kind">
                  {KIND_LABELS[first.kind]}
                  {gold > 0 && ` · ${formatAmount(gold)} g/min`}
                </span>
              </summary>
              <ul className="flow-list">
                {flows.map((flow, i) => (
                  <li key={i}>
                    <span>
                      {flow.direction === "in" ? "from" : "to"} {partnerName(content, flow.partnerType)}
                      {flow.kind === "trade" && <span className="muted"> ({flow.partnerOwner})</span>}
                    </span>
                    <span className="flow-amount">{formatAmount(flow.amount)}/min</span>
                    <span className="muted flow-kind">
                      {flow.price > 0 && `@ ${flow.price.toFixed(2)} g`}
                    </span>
                  </li>
                ))}
              </ul>
            </details>
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
  const network = useAppStore((state) => state.buildingNetwork);
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
  const send = mine ? primarySend(details.trade) : null;
  const gold = currentGold({ company, companyAt });
  const underConstruction = building.constructionEndsAt !== null;
  const secondsLeft = underConstruction
    ? (new Date(building.constructionEndsAt!).getTime() - (now + serverOffset)) / 1000
    : 0;
  const nextMilestone = content.nextMilestone(building.level);
  const isProducer = details.inputs.length + details.outputs.length > 0 || details.researchPerMinute > 0;
  const thumbnail = gameClient.textures?.thumbnail(definition.id, definition.size);

  return (
    <aside className="panel building-panel" aria-label={definition.name}>
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
              {/* The HQ has no levels of its own: it shows the Company Level */}
              {definition.kind === "hq" ? "" : ` · Level ${building.level}`}
              {definition.kind !== "hq" && levelTier(building.level) !== null && (
                <span className="tier-name" style={{ color: levelTier(building.level)!.css }}>
                  {" "}
                  {levelTier(building.level)!.name}
                </span>
              )}
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
      ) : details.status?.pill != null ? (
        // Why it doesn't run fully, in five words (F2), what its pipes reach, and the one fix
        <div className={`idle-status ${details.status.cause}`}>
          <p className={details.status.cause === "stuck" ? "status stuck" : "status idle"}>
            {details.status.pill}
          </p>
          {details.status.reach !== null && <p className="muted small reach">{details.status.reach}</p>}
          {mine && send !== null ? (
            // Step 4: a neighbour or the project wants its goods, so the one fix is the Send that lays the pipe too
            <button
              className="primary wide fix"
              onClick={() =>
                void gameClient.sendGood(send.resource, send.to, send.name, send.building, send.pipe.tiles)
              }
            >
              Lay the pipe and send to {send.name} · {send.pipe.newTiles}{" "}
              {send.pipe.newTiles === 1 ? "tile" : "tiles"} · {formatAmount(send.pipe.cost)} g
            </button>
          ) : (
            mine &&
            details.status.fix !== null && (
              <button
                className="primary wide fix"
                onClick={() => void gameClient.fixBuilding(details, details.status!.fix!)}
              >
                {details.status.fix.label}
              </button>
            )
          )}
        </div>
      ) : isProducer ? (
        <p className={details.rate > 0.005 ? "status running" : "status idle"}>
          {details.rate > 0.995
            ? "Running at full speed"
            : details.rate > 0.005
              ? `Running at ${Math.round(details.rate * 100)}%`
              : "Idle"}
          {details.rate > 0.005 && details.trade?.runsOn != null && ` · ${details.trade.runsOn}`}
        </p>
      ) : null}
      {/* Upgrading at the top: it stays under the pointer while the rest of the panel refreshes */}
      {mine && details.upgradeCost !== null && !underConstruction && (
        <section className="upgrade">
          <h4>
            Upgrade · output +10% per level
            {nextMilestone !== null && <span className="muted"> · level {nextMilestone} doubles it</span>}
            {nextTier(building.level) !== null && (
              <span className="muted">
                {" "}
                ·{" "}
                <span style={{ color: nextTier(building.level)!.css }}>
                  {nextTier(building.level)!.name}
                </span>{" "}
                at level {nextTier(building.level)!.from}
              </span>
            )}
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
            {details.maxAffordableLevels > 1 && details.upgradeCostMax !== null && (
              <button onClick={() => void gameClient.upgrade(building.id, details.maxAffordableLevels)}>
                Max +{details.maxAffordableLevels} · {formatAmount(details.upgradeCostMax)} g
              </button>
            )}
          </div>
          {details.maxMore !== null && (
            <MaxMore details={details} content={content} gold={gold} level={building.level} />
          )}
        </section>
      )}

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
      {mine && definition.kind === "expeditionOffice" && building.constructionEndsAt === null && (
        <ExpeditionSection />
      )}
      {mine && details.teleporter !== null && building.constructionEndsAt === null && (
        <TeleporterSection details={details} />
      )}
      {mine && details.evolve !== null && (
        <section className="evolve">
          <button
            className="primary wide"
            disabled={details.evolve.problem !== null || gold < details.evolve.cost}
            title={details.evolve.problem ?? undefined}
            onClick={() =>
              void gameClient.evolve(
                building.id,
                content.building(details.evolve!.into)?.name ?? details.evolve!.into,
              )
            }
          >
            ⬆ Evolve into {content.building(details.evolve.into)?.name} · {formatAmount(details.evolve.cost)}{" "}
            g
          </button>
          <p className="muted small">
            {details.evolve.problem ??
              "Built where it stands: its gold and upgrades count towards the new one, the pipes stay."}
          </p>
        </section>
      )}
      {mine && definition.kind === "blueprintOffice" && building.constructionEndsAt === null && (
        <button className="primary wide" onClick={() => void gameClient.openBlueprints()}>
          📐 Blueprints (Y)
        </button>
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

      {mine && details.trade !== null && !underConstruction && (
        <NeighbourTrade trade={details.trade} content={content} />
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
      {definition.kind === "lab" && mine && company !== null && (
        <button className="primary wide" onClick={() => void gameClient.openResearch()}>
          {company.currentResearch !== null
            ? `Researching ${content.research(company.currentResearch)?.name ?? ""} · open research`
            : "🔬 Pick a research"}
        </button>
      )}

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
              mark={details.keep?.keeps[resource]}
              markTitle={`It keeps ${formatAmount(details.keep?.keeps[resource] ?? 0)}`}
            />
          ))}
          {details.keep !== null &&
            Object.entries(details.contents).some(([r, a]) => a > (details.keep!.keeps[r] ?? 0) + 1) && (
              <p className={details.keep.tradingPostFull ? "status idle" : "muted small"}>
                {details.keep.tradingPostFull
                  ? "It holds more than it keeps (the line): your trading post is full, so what it can't sell comes in here instead of going to waste. Upgrade the trading post or build another one to sell it."
                  : "It holds more than it keeps (the line) from before: new surplus is sold, and it fills up again only when your trading post is full."}
              </p>
            )}
          {definition.kind === "storage" && (
            <>
              <p className="muted small">
                It keeps what your daily orders and your next buildings need; the rest goes on to be sold.
                {mine && " Keep more of each resource:"}
              </p>
              {mine && (
                <div className="chips">
                  {(
                    [
                      [0, "What's needed"],
                      [0.1, `${formatAmount(details.capacity * 0.1)} each`],
                      [0.5, `${formatAmount(details.capacity * 0.5)} each`],
                      [1, "Everything"],
                    ] as const
                  ).map(([share, label]) => (
                    <button
                      key={share}
                      className={details.setting === share ? "chip active" : "chip"}
                      onClick={() => void gameClient.setBuildingSetting(building.id, share)}
                    >
                      {label}
                    </button>
                  ))}
                </div>
              )}
            </>
          )}
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

      {details.pumpFactor < 1 && (
        <p className="status idle">
          Its network needs a Pump Station: a long network loses pressure, and every 50 buildings in one
          network need a pump to keep it up. Until then everything in it runs at{" "}
          {Math.round(details.pumpFactor * 100)}%.
        </p>
      )}

      {definition.kind === "hq" && mine && company !== null && (
        <section>
          <Meter
            label={`Company Level ${company.level}`}
            color="#c9963a"
            amount={company.xp - company.levelXp}
            max={company.nextLevelXp - company.levelXp}
            unit=" XP"
          />
          <p className="muted small">
            Level {company.level + 1} in {formatAmount(company.nextLevelXp - company.xp)} XP. Your company
            gains XP from the gold it earns, new kinds of buildings, upgrades, research, parcels and daily
            orders.
          </p>
        </section>
      )}

      <Flows details={details} content={content} />

      {network !== null && (
        <section>
          <h4>
            Its network ·{" "}
            <button className="link small" onClick={() => gameClient.showBuildingNetwork()}>
              show on the map
            </button>
          </h4>
          <NetworkSummary details={network} content={content} ownCompany={company?.id ?? null} />
        </section>
      )}

      {mine && definition.kind !== "hq" && (
        <div className="button-row footer-row">
          {details.complexMembers <= 1 && (
            <button onClick={() => gameClient.setTool({ kind: "move", building: building.id })}>Move</button>
          )}
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

/** Why Max stops where it does (F8): what the levels beyond would do, and on "more…" the choice to buy them anyway */
function MaxMore({
  details,
  content,
  gold,
  level,
}: {
  details: BuildingDetailsDto;
  content: Content;
  gold: number;
  level: number;
}) {
  const [open, setOpen] = useState(false);
  const more = details.maxMore!;
  const good = more.resource !== null ? content.resourceName(more.resource).toLowerCase() : "";
  const useful = details.maxAffordableLevels;
  const reason = more.output
    ? details.status?.cause === "postFull"
      ? `+${more.levels} would sell nothing more: your Trading Post sells all it can. Upgrade it first.`
      : `+${more.levels} would make nothing more: nothing on its pipes takes more ${good}`
    : `+${more.levels} would run at ${Math.round(more.share * 100)}%: its pipes bring ${formatAmount(more.have)} of ${formatAmount(more.need)} ${good}`;
  return (
    <div className="max-more">
      <p className="muted small">
        {reason} ·{" "}
        <button className="link" onClick={() => setOpen(!open)}>
          more…
        </button>
      </p>
      {open && (
        <div className="max-choice">
          <p className="small">
            Level {level + more.levels} · {formatAmount(more.cost)} g,{" "}
            {Math.round((100 * more.cost) / Math.max(1, gold))}% of your gold. It would run at{" "}
            {Math.round(more.share * 100)}%.
            {useful > 0 && details.upgradeCostMax !== null && (
              <>
                {" "}
                Level {level + useful} runs at full speed for {formatAmount(details.upgradeCostMax)} g.
              </>
            )}
          </p>
          <div className="button-row">
            {useful > 0 && details.upgradeCostMax !== null && (
              <button
                className="primary"
                onClick={() => void gameClient.upgrade(details.building.id, useful)}
              >
                Level {level + useful} · {formatAmount(details.upgradeCostMax)} g
              </button>
            )}
            <button
              onClick={() =>
                void gameClient.upgrade(details.building.id, more.levels).then(() => setOpen(false))
              }
            >
              Level {level + more.levels} anyway
            </button>
            <button onClick={() => setOpen(false)}>Cancel</button>
          </div>
        </div>
      )}
    </div>
  );
}

function Meter({
  label,
  color,
  amount,
  max,
  unit = "/min",
  mark,
  markTitle,
}: {
  label: string;
  color: string;
  amount: number;
  max: number;
  unit?: string;
  /** A line on the bar (what a storage keeps) */
  mark?: number;
  markTitle?: string;
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
        {mark !== undefined && mark > 0 && max > 0 && (
          <div
            className="meter-mark"
            title={markTitle}
            style={{ left: `${Math.min(100, (mark / max) * 100)}%` }}
          />
        )}
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
/** Expeditions from the office: send a team out, see what came back */
function ExpeditionSection() {
  const company = useAppStore((s) => s.company)!;
  const companyAt = useAppStore((s) => s.companyAt);
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow(1000) + serverOffset;
  const expedition = company.expedition;
  const gold = currentGold({ company, companyAt });
  return (
    <section className="expedition">
      <h4>Expeditions</h4>
      {expedition.endsAt !== null ? (
        <p className="status building">
          🧭 The team is out for {expedition.hours} h: back in{" "}
          {formatDuration(Math.max(0, (new Date(expedition.endsAt).getTime() - now) / 1000))}
        </p>
      ) : (
        <div className="button-row">
          {Object.entries(expedition.costs).map(([hours, cost]) => (
            <button
              key={hours}
              className="primary"
              disabled={expedition.blocker !== null || gold < cost}
              onClick={() => void gameClient.startExpedition(Number(hours))}
            >
              {hours} h · {formatAmount(cost)} g
            </button>
          ))}
        </div>
      )}
      <p className="muted small">
        Longer trips bring more luck: rich deposits in the wilderness, decorations, relics (
        {expedition.relics.length} found).
      </p>
      {expedition.log.length > 0 && (
        <ul className="facts">
          {expedition.log.map((line, i) => (
            <li key={i}>{line}</li>
          ))}
        </ul>
      )}
    </section>
  );
}

/** A teleporter: what it sends or receives, or linking it to another station */
function TeleporterSection({ details }: { details: BuildingDetailsDto }) {
  const content = useAppStore((s) => s.content)!;
  const t = details.teleporter!;
  const [receiver, setReceiver] = useState<number | null>(null);
  const [resource, setResource] = useState("ironOre");
  const id = details.building.id;
  const resourceName = (r: string | null) => (r === null ? "" : content.resourceName(r).toLowerCase());
  if (t.role !== null) {
    return (
      <section className="teleporter">
        <h4>{t.role === "sender" ? "Sends" : "Receives"}</h4>
        <p>
          {t.role === "sender" ? "→" : "←"}{" "}
          {formatAmount(t.perMinute * (t.role === "receiver" ? 1 - t.loss : 1))} {resourceName(t.resource)}
          /min {t.role === "sender" ? "to" : "from"} the station {formatAmount(t.distance)} tiles away
        </p>
        <p className="muted small">
          Up to {formatAmount(t.throughput)}/min · {t.energyPerUnit.toFixed(3)} energy per unit for the
          distance
          {t.loss > 0 && ` · ${Math.round(t.loss * 100)}% lost on the way`}
        </p>
        <button onClick={() => void gameClient.unlinkTeleporter(id)}>Unlink</button>
      </section>
    );
  }
  const options = t.candidates;
  return (
    <section className="teleporter">
      <h4>Link to another station</h4>
      {options.length === 0 ? (
        <p className="muted small">
          Build a second {content.building(details.building.type)?.name}{" "}
          {t.range === "district" ? "in this district" : "anywhere"}: one sends, the other receives. Their
          networks stay separate.
        </p>
      ) : (
        <>
          <label className="small">
            Send to{" "}
            <select
              value={receiver ?? ""}
              onChange={(e) => setReceiver(e.target.value === "" ? null : Number(e.target.value))}
            >
              <option value="">pick a station…</option>
              {options.map((c) => (
                <option key={c.id} value={c.id} disabled={c.problem !== null}>
                  {formatAmount(c.distance)} tiles away{c.problem !== null ? ` (${c.problem})` : ""}
                </option>
              ))}
            </select>
          </label>
          <label className="small">
            Resource{" "}
            <select value={resource} onChange={(e) => setResource(e.target.value)}>
              {content.dto.resourceList.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name}
                </option>
              ))}
            </select>
          </label>
          <button
            className="primary"
            disabled={receiver === null}
            onClick={() => void gameClient.linkTeleporter(id, receiver!, resource)}
          >
            Link
          </button>
        </>
      )}
    </section>
  );
}

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
