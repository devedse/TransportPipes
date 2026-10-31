import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import type {
  GiveMode,
  MonumentDto,
  TownDto,
  TownWonderDto,
  WonderNeedDto,
  WonderTier,
} from "../api/contracts";
import { useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

/** A Wonder stage's tiers (1.4 §13): a share of the stage, paid in hours of your own income when it stands */
const TIERS: Array<{ tier: WonderTier; share: number; name: string; hours: number }> = [
  { tier: "bronze", share: 0.01, name: "Bronze", hours: 1 },
  { tier: "silver", share: 0.03, name: "Silver", hours: 3 },
  { tier: "gold", share: 0.07, name: "Gold", hours: 6 },
  { tier: "diamond", share: 0.15, name: "Diamond", hours: 12 },
];

const GIVE_MODES: Array<{ mode: GiveMode; name: string; title: string }> = [
  { mode: "off", name: "Off", title: "You give it only by hand" },
  {
    mode: "beyondHubOrder",
    name: "Only what the hub pays less for",
    title:
      "What you make of it beyond your Hub Order goes to the Wonder through your Yard gate, at its base price",
  },
  {
    mode: "all",
    name: "All",
    title:
      "All your spare of it goes to the Wonder through your Yard gate: it no longer sells at your Trading Post",
  },
];

/** A good the stage wants: what is in, your switch, and buy and give */
function Need({ need }: { need: WonderNeedDto }) {
  const content = useAppStore((s) => s.content)!;
  const gold = useAppStore((s) => s.company?.gold ?? 0);
  const lacks = need.delivered < need.required - 0.5;
  return (
    <div className="wonder-need">
      <ResourceIcon resource={need.resource} />
      <span className="need-name">{content.resourceName(need.resource)}</span>
      <div className="meter-bar">
        <div
          className="meter-fill"
          style={{
            width: `${Math.min(100, (need.delivered / Math.max(1, need.required)) * 100)}%`,
            background: content.resourceColor(need.resource),
          }}
        />
      </div>
      <span className="small">
        {formatAmount(need.delivered)} / {formatAmount(need.required)}
      </span>
      <span className="muted small" title="In your Storages">
        you have {formatAmount(need.yourStock)}
      </span>
      <div className="wonder-order">
        <span className="segmented small" role="group" aria-label="Give by itself">
          {GIVE_MODES.map((t) => (
            <button
              key={t.mode}
              className={need.give === t.mode ? "active" : ""}
              title={t.title}
              onClick={() => void gameClient.setGive(need.resource, t.mode)}
            >
              {t.name}
            </button>
          ))}
        </span>
        {need.perMinute > 0.005 && (
          <span className="small">
            {formatAmount(need.perMinute)}/min coming from the town&apos;s switches
          </span>
        )}
        {lacks && need.buyCost > 0 && (
          <button
            className="small"
            disabled={gold < 1}
            title="Buys what it still lacks at the market's 2.5× and gives it (as much as your gold buys)"
            onClick={() => void gameClient.buyAndGive(need.resource)}
          >
            Buy and give the rest · {formatAmount(need.buyCost)} g
          </button>
        )}
        {need.makers.length > 0 && <span className="small muted">made by {need.makers.join(", ")}</span>}
        {need.chain !== null && (
          <span className="small wonder-chain">
            {need.chain}
            {need.chainBuilding !== null && (
              <button className="small link" onClick={() => gameClient.handBuilding(need.chainBuilding!)}>
                Show me
              </button>
            )}
          </span>
        )}
      </div>
    </div>
  );
}

function WonderCard({
  wonder,
  town,
  serverNow,
}: {
  wonder: TownWonderDto;
  town: TownDto;
  serverNow: number;
}) {
  const gold = useAppStore((s) => s.company?.gold ?? 0);
  const finished = wonder.stage >= wonder.stages;
  const standsIn = wonder.standsAt === null ? null : (new Date(wonder.standsAt).getTime() - serverNow) / 1000;
  const canGive = wonder.needs.some((t) => t.yourStock > 0.5 && t.delivered < t.required - 0.5);
  const tier = TIERS.find((t) => t.tier === wonder.yourTier);
  const nextTier = TIERS.find((t) => t.share > wonder.yourStageShare + 1e-9);
  return (
    <div className={finished ? "wonder-card finished" : "wonder-card"}>
      <div className="wonder-head">
        <div>
          <div className="panel-title">Era {wonder.era} Wonder · your town builds it together</div>
          <h3>{wonder.name}</h3>
          <div className="small">{wonder.description}</div>
        </div>
        <div className="wonder-stage">
          {finished ? "Finished" : `Stage ${wonder.stage + 1} of ${wonder.stages}`}
          <div className="stage-dots">
            {Array.from({ length: wonder.stages }, (_, i) => (
              <span
                key={i}
                className={
                  i < wonder.stage ? "dot done" : i === wonder.stage && !finished ? "dot now" : "dot"
                }
              />
            ))}
          </div>
        </div>
      </div>
      {standsIn !== null && !finished && (
        <p className="small wonder-opens">
          🏗 Everything is in: the stage stands in {formatDuration(Math.max(0, standsIn))}
          {wonder.nextPerk !== null && <>, then {wonder.nextPerk}</>}
        </p>
      )}
      {!finished && standsIn === null && (
        <div className="wonder-needs">
          {wonder.needs.map((need) => (
            <Need key={need.resource} need={need} />
          ))}
        </div>
      )}
      <div className="wonder-foot">
        <div className="small">
          This stage: you gave {(wonder.yourStageShare * 100).toFixed(1)}%
          {tier !== undefined && <span className={`tier ${tier.tier}`}>{tier.name}</span>}
          {nextTier !== undefined && !finished && (
            <span className="muted">
              {" "}
              · {nextTier.name} from {nextTier.share * 100}% ({nextTier.hours} h of your income)
            </span>
          )}
          <span className="muted"> · {formatAmount(wonder.yourGiven)} g given in all</span>
        </div>
        {!finished && standsIn === null && (
          <span className="button-row">
            <button
              disabled={!canGive}
              title="Gives what your Storages and your goods in the Depot hold of what it lacks"
              onClick={() => void gameClient.giveFromStorages()}
            >
              Give from my Storages
            </button>
            {wonder.buyAllCost > 0 && (
              <button
                className="primary"
                disabled={gold < Math.min(wonder.buyAllCost, town.yourHour)}
                title="Buys what the stage lacks at the market's 2.5× and gives it; at most an hour of your income"
                onClick={() =>
                  void gameClient.buyAndGive(null, Math.min(wonder.buyAllCost, Math.max(1, town.yourHour)))
                }
              >
                Buy and give an hour&apos;s worth · {formatAmount(Math.min(wonder.buyAllCost, town.yourHour))}{" "}
                g
              </button>
            )}
          </span>
        )}
      </div>
      <div className="muted small">
        A stage is worth {formatAmount(wonder.stageWorth)} g of goods
        {wonder.trail < 0.999 &&
          ` (${Math.round((1 - wonder.trail) * 100)}% smaller: other towns built it before)`}
        . Once its goods are in it stands a day later, and everyone who gave gets hours of their own income by
        their share of the stage: Bronze from 1% (1 h), Silver from 3% (3 h), Gold from 7% (6 h), Diamond from
        15% (12 h). The last stage brings the town into the next Era. A switch works from your Yard&apos;s
        next round, within a minute or two: your spare of the good goes to your Yard gate and on to the
        Wonder, a gift, so it no longer sells at your Trading Post.
        {wonder.perks.length > 0 && <> Built so far: {wonder.perks.join(" · ")}.</>}
      </div>
      {wonder.topBuilders.length > 0 && (
        <div className="builders">
          {wonder.topBuilders.map((b, i) => (
            <span key={b.company} className="builder">
              {i + 1}. <span className="company-dot" style={{ background: b.color }} /> {b.name}{" "}
              <span className="muted">
                {formatAmount(b.value)} g · {(b.share * 100).toFixed(1)}%
              </span>
            </span>
          ))}
        </div>
      )}
    </div>
  );
}

/** A Monument site of your Yard: choose its kind, or pay into its next stage */
function MonumentSite({ site, town }: { site: MonumentDto; town: TownDto }) {
  const company = useAppStore((s) => s.company);
  const gold = company?.gold ?? 0;
  const level = company?.level ?? 0;
  if (site.kind === null) {
    const free = town.kinds.filter((t) => !t.built);
    return (
      <div className="monument-site">
        <div className="panel-title">Era {site.era} site</div>
        <p className="small">Your crew picks what stands here (each kind once per Yard):</p>
        <div className="monument-kinds">
          {free.map((kind) => (
            <button
              key={kind.id}
              className="small"
              title={kind.description}
              onClick={() => void gameClient.chooseMonument(site.era, kind.id)}
            >
              {kind.name}
            </button>
          ))}
        </div>
      </div>
    );
  }
  const left = Math.max(0, site.nextCost - site.paid);
  const hour = Math.max(1, Math.min(left, town.yourHour));
  return (
    <div className="monument-site">
      <div className="panel-title">
        Era {site.era} · {site.name} · {site.stages} {site.stages === 1 ? "stage" : "stages"}
      </div>
      {site.perk !== null && <p className="small">{site.perk}</p>}
      {site.capped ? (
        <p className="small muted">It gives all it can.</p>
      ) : (
        <>
          <div className="meter-bar" title={`${formatAmount(site.paid)} of ${formatAmount(site.nextCost)} g`}>
            <div
              className="meter-fill"
              style={{ width: `${Math.min(100, (site.paid / site.nextCost) * 100)}%` }}
            />
          </div>
          <p className="small">
            Stage {site.stages + 1}: {formatAmount(site.paid)} / {formatAmount(site.nextCost)} g
            {site.yourPaid > 0 && (
              <span className="muted"> · you paid {formatAmount(site.yourPaid)} g in all</span>
            )}
          </p>
          {level < town.monumentLevel ? (
            <p className="small muted">You pay into Monuments from Company Level {town.monumentLevel}.</p>
          ) : (
            <span className="button-row">
              <button
                className="small"
                disabled={gold < 1}
                onClick={() => void gameClient.payMonument(site.era, hour)}
              >
                Pay an hour&apos;s income · {formatAmount(hour)} g
              </button>
              {left > hour && (
                <button
                  className="small"
                  disabled={gold < left}
                  onClick={() => void gameClient.payMonument(site.era, left)}
                >
                  Finish the stage · {formatAmount(left)} g
                </button>
              )}
            </span>
          )}
        </>
      )}
    </div>
  );
}

/** Your town (1.4 §11, §13): its Era and launch party, its Era Wonder, and your Yard's Monuments */
export function TownTab({ serverNow }: { serverNow: number }) {
  const town = useAppStore((s) => s.town);
  if (town === null) {
    return <p className="muted">Loading…</p>;
  }
  const party = town.partyUntil === null ? null : (new Date(town.partyUntil).getTime() - serverNow) / 1000;
  return (
    <div className="wonders">
      <p className="muted small">
        {town.name} is in Era {town.era}
        {town.yourEra > town.era &&
          ` · you reached Era ${town.yourEra} elsewhere: your buildings of a later Era sleep here until the town gets there`}
        . Everyone in town builds its Wonder together; its last stage brings the next Era.
      </p>
      {town.launchedAt !== null && (
        <p className="status running">
          🚀 Your town launched its Space Elevator. Missions now: each one finished gives everyone in town +1%
          production for good{town.stars > 0 ? `, and so far ${town.stars} ★` : ""}.
        </p>
      )}
      {party !== null && party > 0 && (
        <p className="status running">
          🎉 Launch party: +10% for everyone in town for another {formatDuration(party)}
        </p>
      )}
      {town.wonder !== null ? (
        <WonderCard wonder={town.wonder} town={town} serverNow={serverNow} />
      ) : (
        <p className="muted">The town has built every Wonder there is so far.</p>
      )}
      <h4>Your Yard&apos;s Monuments</h4>
      {town.monuments.length === 0 ? (
        <p className="muted small">
          Move into a Yard with neighbours, or found its crew: Monuments stand in its square.
        </p>
      ) : (
        <>
          <p className="muted small">
            Every Era gives your Yard a Monument site. Any resident pays gold into it, in stages without end,
            each 40% dearer; every stage helps every resident.
          </p>
          <div className="monument-sites">
            {town.monuments.map((site) => (
              <MonumentSite key={site.era} site={site} town={town} />
            ))}
          </div>
        </>
      )}
    </div>
  );
}
