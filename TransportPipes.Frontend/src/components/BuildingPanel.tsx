import { useState } from "react";
import { researchName } from "../app/research";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration, possessive } from "../app/format";
import { useNow } from "../app/useNow";
import type {
  BuildingDetailsDto,
  BuildingFlowDto,
  BuildingTradeDto,
  LevelWaitDto,
  SiteDto,
  SiteWantDto,
} from "../api/contracts";
import type { Content } from "../game/content";
import type { TerraformAction } from "../game/Engine";
import { currentGold, useAppStore } from "../state/store";
import { levelTier, nextTier } from "../game/levels";
import { NetworkSummary } from "./NetworkSummary";
import { DepotSection } from "./DepotSection";

/** A phone-sized screen when the panel opens: long blocks start folded */
const PHONE = typeof window !== "undefined" && window.matchMedia?.("(max-width: 720px)").matches === true;
import { ResourceIcon } from "./ResourceIcon";

const KIND_LABELS: Record<string, string> = {
  internal: "own",
  trade: "trade",
  toStorage: "stored",
  fromStorage: "from your Storage",
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
 * The pipe to the Yard gate as a building's one fix (F2, step 4): when a neighbour could use its goods and its pipes
 * don't reach the gate yet
 */
function gatePipeFix(trade: BuildingTradeDto | null) {
  if (trade === null || trade.reachesGate || trade.gatePipe === null || trade.bids.length === 0) {
    return null;
  }
  return { name: trade.bids[0]!.buyerName, pipe: trade.gatePipe };
}

/**
 * A building's trade with its neighbours (1.4 §8), for its owner: who takes its goods and who sends it its inputs, both
 * ends named with the price; who could use its goods, and the pipe to the Yard gate when its pipes don't reach it
 */
function NeighbourTrade({ details, content }: { details: BuildingDetailsDto; content: Content }) {
  const trade = details.trade!;
  const good = (resource: string) => content.resourceName(resource).toLowerCase();
  const pipe = trade.reachesGate ? null : trade.gatePipe;
  // The pipe the panel's head offers already isn't offered twice
  const offered = gatePipeFix(trade) !== null && details.status?.pill != null;
  return (
    <section className="neighbour-trade">
      <h4>Neighbours</h4>
      <ul className="trade-lines">
        {trade.takers.map((t) => (
          <li key={`to-${t.partner}-${t.resource}`}>
            <ResourceIcon resource={t.resource} />
            <span>
              to <strong>{t.partnerName}</strong>
              {t.partnerBuilding !== null && `${possessive(t.partnerName)} ${t.partnerBuilding}`} ·{" "}
              {formatAmount(t.perMinute)}/min · {t.price.toFixed(2)} g
            </span>
          </li>
        ))}
        {trade.bids.map((b) => (
          <li key={`bid-${b.buyer}-${b.resource}`}>
            <ResourceIcon resource={b.resource} />
            <span>
              <strong>{b.buyerName}</strong>
              {b.building !== null && `${possessive(b.buyerName)} ${b.building}`} could use{" "}
              {formatAmount(b.perMinute)}/min of your {good(b.resource)}
            </span>
          </li>
        ))}
        {trade.suppliers.map((t) => (
          <li key={`from-${t.partner}-${t.resource}`}>
            <ResourceIcon resource={t.resource} />
            <span>
              {good(t.resource)} {formatAmount(t.perMinute)}/min from <strong>{t.partnerName}</strong> ·{" "}
              {t.price.toFixed(2)} g
            </span>
          </li>
        ))}
      </ul>
      {pipe !== null && trade.bids.length > 0 && !offered && (
        <button className="small" onClick={() => void gameClient.fixBuilding(details, pipe)}>
          Lay the pipe to your Yard gate · {pipe.newTiles} {pipe.newTiles === 1 ? "tile" : "tiles"} ·{" "}
          {formatAmount(pipe.cost)} g
        </button>
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

/**
 * What a scaffold or a level step still wants (after run 3, step 2): each good with what is in and what comes now, who
 * brought it, the chain when nobody near makes it, and Buy the rest
 */
function WantList({
  wants,
  content,
  mine,
  building,
}: {
  wants: SiteWantDto[];
  content: Content;
  mine: boolean;
  building: number;
}) {
  return (
    <ul className="site-list">
      {wants.map((want) => {
        const name = content.resourceName(want.resource).toLowerCase();
        return (
          <li key={want.resource}>
            <span>
              Wants {formatAmount(want.amount)} {name} · <strong>{formatAmount(want.got)} in</strong>
              {want.perMinute > 0.005 && (
                <span className="muted"> · {formatAmount(want.perMinute)}/min coming</span>
              )}
            </span>
            <span className="goal-bar">
              <span
                style={{ width: `${Math.round(Math.min(1, want.got / Math.max(1, want.amount)) * 100)}%` }}
              />
            </span>
            {want.from.length > 0 && <span className="muted small">from {want.from.join(" · ")}</span>}
            {want.chain !== null && (
              <span className="small">
                {want.chain}
                {want.chainBuilding !== null && (
                  <button className="link small" onClick={() => gameClient.handBuilding(want.chainBuilding!)}>
                    Show me
                  </button>
                )}
              </span>
            )}
            {want.order !== null && <span className="small">{want.order}</span>}
            {mine && want.orderCost !== null && (
              <button className="small" onClick={() => void gameClient.orderRest(building)}>
                Buy the rest · {formatAmount(want.orderCost)} g
              </button>
            )}
          </li>
        );
      })}
    </ul>
  );
}

/** Where a want's goods come from (1.4 §9), said once under the list */
function WantsNote() {
  const fromMarket = useAppStore((s) => s.company?.wantsFromMarket ?? true);
  return (
    <p className="muted small">
      Your Storages give what they hold; your spare, your neighbours&apos; and the Depot&apos;s reach it
      through the Yard by themselves.{" "}
      {fromMarket
        ? "What nobody has, the market brings within about ten minutes at 2.5× its price; Buy the rest brings it at once."
        : "The market brings nothing (your trade settings): Buy the rest brings it at once at 2.5× its price."}
    </p>
  );
}

/** A scaffold that wants its materials, and Cancel with the gold it gives back */
function SiteWants({
  site,
  content,
  mine,
  building,
}: {
  site: SiteDto;
  content: Content;
  mine: boolean;
  building: number;
}) {
  return (
    <div className="site-wants">
      <p className="status building">🏗 A scaffold: it stands once its materials are in</p>
      <WantList wants={site.wants} content={content} mine={mine} building={building} />
      <WantsNote />
      {mine && (
        <button className="small" onClick={() => void gameClient.cancelSite(building)}>
          Give it up · {formatAmount(site.refund)} g back
        </button>
      )}
    </div>
  );
}

/**
 * Levels bought whose goods are still coming (1.4 §9): from level 11 a step takes goods as well as gold, and the
 * building works at its old level under a little scaffolding until they are in
 */
function LevelWait({
  wait,
  level,
  content,
  mine,
  building,
}: {
  wait: LevelWaitDto;
  level: number;
  content: Content;
  mine: boolean;
  building: number;
}) {
  return (
    <div className="site-wants level-wait">
      <p className="status building">
        🏗 {wait.boughtLevel === level + 1 ? `Level ${level + 1}` : `Levels ${level + 1}–${wait.boughtLevel}`}{" "}
        bought: it works at level {level} until the goods are in
      </p>
      <WantList wants={wait.wants} content={content} mine={mine} building={building} />
      {mine && wait.buyRest !== null && wait.wants.length > 1 && (
        <button className="small" onClick={() => void gameClient.orderRest(building)}>
          Buy all the rest · {formatAmount(wait.buyRest)} g
        </button>
      )}
      <WantsNote />
    </div>
  );
}

/** Minutes of income as a short time: "27 min", "3 h", "2 days" */
function formatTimeOfIncome(minutes: number): string {
  return minutes < 90
    ? `${Math.max(1, Math.round(minutes))} min`
    : minutes < 48 * 60
      ? `${Math.round(minutes / 60)} h`
      : `${Math.round(minutes / 1440)} days`;
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
  const gatePipe = mine ? gatePipeFix(details.trade) : null;
  const gold = currentGold({ company, companyAt });
  // A price also as time: " (27 min of income)", from about a minute of income up (after run 3, step 1)
  const income = company?.goldPerMinute ?? 0;
  const asTime = (cost: number | null) =>
    cost === null || income <= 0 || cost < income ? "" : ` (${formatTimeOfIncome(cost / income)})`;
  const underConstruction = building.constructionEndsAt !== null;
  const secondsLeft = underConstruction
    ? (new Date(building.constructionEndsAt!).getTime() - (now + serverOffset)) / 1000
    : 0;
  // The levels bought whose goods are still coming count: the next one to buy comes after them (1.4 §9)
  const bought = details.levelWait?.boughtLevel ?? building.level;
  const isProducer = details.inputs.length + details.outputs.length > 0;
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

      {details.site !== null ? (
        <SiteWants site={details.site} content={content} mine={mine} building={building.id} />
      ) : details.sleeping ? (
        <p className="status idle">
          💤 Asleep: it belongs to Era {definition.unlock.era}, and wakes when your town gets there
        </p>
      ) : underConstruction ? (
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
          {mine && gatePipe !== null && details.status.fix?.ownNeed !== true ? (
            // Step 4: a neighbour could use its goods, so the one fix is the pipe to the Yard gate (your own short
            // building comes first: run 2, Cleo's mines)
            <button
              className="primary wide fix"
              onClick={() => void gameClient.fixBuilding(details, gatePipe.pipe)}
            >
              Lay the pipe to your Yard gate for {gatePipe.name} · {gatePipe.pipe.newTiles}{" "}
              {gatePipe.pipe.newTiles === 1 ? "tile" : "tiles"} · {formatAmount(gatePipe.pipe.cost)} g
            </button>
          ) : (
            mine &&
            details.status.fix !== null &&
            // The pipe door stands in the upgrade section below, with its preview: one button, not two
            !(details.status.fix.kind === "climb" && details.pipeDoor !== null) && (
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
      {details.levelWait !== null && (
        <LevelWait
          wait={details.levelWait}
          level={building.level}
          content={content}
          mine={mine}
          building={building.id}
        />
      )}
      {details.track !== null && (
        <p className="muted small track-line">
          🔬 {content.track(details.track)?.name ?? details.track} track · tier {details.trackTiers} of{" "}
          {content.dto.trackTiers.length}
          {mine && (
            <button className="link small" onClick={() => void gameClient.openResearch()}>
              Research
            </button>
          )}
        </p>
      )}
      {/* Upgrading at the top: it stays under the pointer while the rest of the panel refreshes */}
      {mine && details.upgradeCost !== null && !underConstruction && (
        <section className="upgrade">
          <h4>
            Upgrade · output +10% per level, up to level {content.progression.maxBuildingLevel}
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
          <div className="button-row" title="The time in brackets is how long your income takes to pay it">
            <button
              className="primary"
              disabled={gold < details.upgradeCost}
              onClick={() => void gameClient.upgrade(building.id, 1)}
            >
              Level {bought + 1} · {formatAmount(details.upgradeCost)} g{asTime(details.upgradeCost)}
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
                {asTime(details.upgradeCostMax)}
              </button>
            )}
          </div>
          {/* 1.4 §9: every copy costs 7% more than the last, and its levels cost from what it cost */}
          <p className="muted small">
            This one cost {formatAmount(details.copyPrice)} g: its levels cost from that, 15% more each
            {details.nextLevelGoods !== null && details.nextLevelGoods.length > 0 && (
              <>
                {" "}
                · the next takes{" "}
                {details.nextLevelGoods
                  .map((t) => `${formatAmount(t.amount)} ${content.resourceName(t.resource).toLowerCase()}`)
                  .join(" and ")}{" "}
                as well
              </>
            )}
          </p>
          {/* What it will do, before the click (after run 3: the Lab's Max cut Cleo's income from 783 to 285 g/min) */}
          {(details.upgradePreview !== null || details.maxPreview !== null) && (
            <p
              className={`small spend-preview${(details.upgradePreview ?? "").startsWith("−") ? " bad" : ""}`}
            >
              {details.upgradePreview !== null && (
                <>
                  Level {bought + 1}: {details.upgradePreview}
                </>
              )}
              {details.maxPreview !== null && (
                <>
                  {details.upgradePreview !== null && " · "}Max: {details.maxPreview}
                </>
              )}
            </p>
          )}
          {details.pipeDoor !== null ? (
            <PipeDoor details={details} content={content} gold={gold} income={income} />
          ) : (
            details.maxMore !== null && (
              <MaxMore details={details} content={content} gold={gold} level={bought} />
            )
          )}
        </section>
      )}
      {mine && details.undoRefund !== null && details.site === null && (
        <p className="small undo-line">
          <button className="small" onClick={() => void gameClient.undo(building.id)}>
            Undo · {formatAmount(details.undoRefund)} g back
          </button>{" "}
          <span className="muted">for an hour after you buy: the price less what it earned since</span>
        </p>
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
      {mine &&
        details.evolve !== null &&
        details.evolve.problem !== null &&
        /needs Era/.test(details.evolve.problem) && (
          // Locked by the Era: a line, not a dead button "· 0 g" (after run 4, a phone's first panel)
          <p className="muted small">
            With Era {/needs Era (\d+)/.exec(details.evolve.problem)?.[1]} it evolves into{" "}
            {content.building(details.evolve.into)?.name ?? details.evolve.into}, where it stands.
          </p>
        )}
      {mine &&
        details.evolve !== null &&
        !(details.evolve.problem !== null && /needs Era/.test(details.evolve.problem)) && (
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
              ⬆ Evolve into {content.building(details.evolve.into)?.name} ·{" "}
              {formatAmount(details.evolve.cost)} g
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
        <NeighbourTrade details={details} content={content} />
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
      {definition.kind === "lab" && mine && company !== null && (
        <button className="primary wide" onClick={() => void gameClient.openResearch()}>
          {researchName(company, content) !== null
            ? `Researching ${researchName(company, content) ?? ""} · open research`
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
              <p className="muted small">
                It holds more than it keeps (the line) from before: new surplus goes on to be sold.
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

      {definition.kind === "depot" && <DepotSection />}

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
            amount={company.lifetimeEarnings - company.levelEarned}
            max={company.nextLevelEarned - company.levelEarned}
            unit=" g"
          />
          <p className="muted small">
            Level {company.level + 1} once you have earned{" "}
            {formatAmount(company.nextLevelEarned - company.lifetimeEarnings)} g more. The Company Level comes
            from all the gold your company ever earned: at the market, from players, from orders and rewards.
            Spending takes nothing away.
          </p>
        </section>
      )}

      <Flows details={details} content={content} />

      {network !== null && (
        // Folded on a phone (after run 4, Ben: "Panel text is very long for a phone, lots of numbers")
        <details className="net-fold" open={!PHONE}>
          <summary>
            <h4>
              Its network ·{" "}
              <button
                className="link small"
                onClick={(event) => {
                  event.preventDefault();
                  gameClient.showBuildingNetwork();
                }}
              >
                show on the map
              </button>
            </h4>
          </summary>
          <NetworkSummary details={network} content={content} ownCompany={company?.id ?? null} />
        </details>
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

/**
 * The pipe door (after run 4: the pipe tier is the game's real level cap, and the panel said "+0 g/min: its pipes carry
 * 205 iron ore a minute" with no button): the next pipe tier or Widen on its full links, then the levels the new pipe
 * carries, as one button with its preview; goods it lacks come from a friend or an order
 */
function PipeDoor({
  details,
  content,
  gold,
  income,
}: {
  details: BuildingDetailsDto;
  content: Content;
  gold: number;
  income: number;
}) {
  const door = details.pipeDoor!;
  const name = (resource: string) => content.resourceName(resource).toLowerCase();
  if (door.kind === "locked") {
    return <p className="muted small pipe-door locked">Its pipe is full. {door.text}.</p>;
  }
  if (door.kind === "waits") {
    return (
      <div className="pipe-door">
        <p className="small">
          <strong>{door.text}</strong>
          {door.source !== null && <> · {door.source}</>}
        </p>
        {door.orderCost !== null && (
          <button
            className="primary"
            disabled={gold < door.orderCost}
            onClick={() => void gameClient.climbPipe(details.building.id)}
          >
            Order them · {formatAmount(door.orderCost)} g
          </button>
        )}
      </div>
    );
  }
  const cost = door.gold + door.levelCost;
  const payback = door.gain > 0.5 ? cost / door.gain : null;
  return (
    <div className="pipe-door">
      <p className="small pipe-door-why">
        <strong>Its pipe is full: this is the way up.</strong>{" "}
        {door.kind === "widen"
          ? "A wider pipe carries twice as much, for hours of your income."
          : "A better pipe carries far more, and the levels after it pay."}
      </p>
      <button
        className="primary wide"
        disabled={gold < cost}
        onClick={() => void gameClient.climbPipe(details.building.id)}
      >
        {door.label}
      </button>
      <p className="small spend-preview">
        {door.gain > 0.5 ? `+${formatAmount(door.gain)} g/min` : "Its gain shows once its goods are in"}
        {payback !== null && income > 0 && ` · pays back in ${formatDuration(payback * 60)}`}
      </p>
      {door.short.length > 0 && (
        <p className="small muted">
          It takes {door.materials.map((t) => `${formatAmount(t.amount)} ${name(t.resource)}`).join(" and ")};
          your Storages lack {door.short.map((t) => `${formatAmount(t.amount)}`).join(" and ")}. {door.source}
          .{" "}
          {!(door.source ?? "").startsWith("nobody") && (
            <button
              className="link small"
              onClick={() => void gameClient.climbPipe(details.building.id, true)}
            >
              Order them now instead
            </button>
          )}
        </p>
      )}
    </div>
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
  // In words, never a bare "+41" beside a gold figure (after run 4: Ben read "+41 would make nothing more" as income)
  const past = `Past level ${level + useful}`;
  const post = content.building(details.building.type)?.kind === "tradingPost";
  const reason = post
    ? `${past} all it sells would be within your Hub Orders: a level more pays nothing more until you sell more`
    : more.output
      ? details.status?.cause === "postFull"
        ? `${past} it would sell nothing more: your Trading Post sells all it can. Upgrade the Trading Post first.`
        : `${past} it would make nothing more: nothing on its pipes takes more ${good}`
      : `${past} it would run at ${Math.round(more.share * 100)}%: its pipes bring ${formatAmount(more.have)} of the ${formatAmount(more.need)} ${good} it would need`;
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
        Each trip maps the wilderness around your town (1, 3 or 8 pieces: the rest is fog on your map, and
        outposts are claimed only where you mapped). Longer trips bring more luck: rich deposits, decorations,
        relics ({expedition.relics.length} found). You have mapped {company.explored?.length ?? 0} pieces.
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
