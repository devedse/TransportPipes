import { gameClient } from "../app/client";
import { useNow } from "../app/useNow";
import { formatAmount, possessive } from "../app/format";
import type { CompanyDto, ResourceRateDto, YardDto } from "../api/contracts";
import type { Content } from "../game/content";
import { setState, useAppStore } from "../state/store";
import { LinksTab } from "./LinksTab";
import { PlanBoard } from "./PlanBoard";
import { ResourceIcon } from "./ResourceIcon";
import { DepotSection } from "./DepotSection";
import { Term } from "./Term";

function Rates({ rates, content, kind }: { rates: ResourceRateDto[]; content: Content; kind: string }) {
  if (rates.length === 0) {
    return <span className="muted small">—</span>;
  }
  return (
    <span className="chips">
      {rates.slice(0, 4).map((r) => (
        <span
          key={r.resource}
          className={`res-chip ${kind}`}
          style={{ borderColor: content.resourceColor(r.resource) }}
        >
          <ResourceIcon resource={r.resource} /> {formatAmount(r.perMinute)}{" "}
          {content.resourceName(r.resource).toLowerCase()}
        </span>
      ))}
    </span>
  );
}

const ACTIVITY: Record<string, string> = {
  active: "lively",
  resting: "resting",
  dormant: "away a while",
};

/** "1.15×" */
function times(value: number): string {
  return `${value.toFixed(2).replace(/0$/, "")}×`;
}

/**
 * What the Yard does, and when (playtest: "can we clearly explain if you click the yard what happens when?"): the
 * pipe first, then every minute, a build, a Wonder switch, and what comes at which Company Level. Folded once you are
 * piped (after run 4, Mia: "Yard panel is a lot of words").
 */
function YardHow({ yard, company }: { yard: YardDto; company: CompanyDto }) {
  const crew = company.prices?.find((t) => t.key === "crew")?.value ?? 1.15;
  const workshops = company.workshops;
  const level = (at: number) => (company.level >= at ? "✓" : "🔒");
  const rows: Array<{ mark: string; when: string; text: string }> = [
    {
      mark: yard.connected ? "✓" : "→",
      when: "First",
      text: "Lay a pipe from your network to your Yard gate (the yellow posts, G). Nothing goes through the Yard before.",
    },
    {
      mark: "↻",
      when: "At every Yard round, also while you're away",
      text: `What your buildings don't use goes to a neighbour here whose buildings lack it, at your crew price (${times(crew)} the base price); what a neighbour has spare comes to your buildings that lack it, at their price. What nobody here needs goes on to your Trading Post, which sells it to the town's market (the hub). The rounds come about once a minute while trades change, and up to 16 minutes apart when nothing does (the clock in the top bar); a new trade is set up at one round and its goods come at the next.`,
    },
    {
      mark: "🏗",
      when: "When you build or level up",
      text: `Its planks, bricks and other goods come from your Storages first, then from your own spare and your neighbours' through the Yard. ${
        company.wantsFromMarket
          ? "What nobody has, the market brings within about ten minutes at 2.5× its price."
          : "The market brings nothing (your trade settings): Buy the rest brings it at 2.5× its price."
      }`,
    },
    {
      mark: "🏛",
      when: "A give switch in Town & Wonder",
      text: "From the Yard's next round, within a minute or two, your spare of that good goes through your gate to the town's Wonder: a gift, so it no longer sells.",
    },
    {
      mark: level(yard.monumentLevel),
      when: `From Company Level ${yard.monumentLevel}`,
      text: "The Yard's Monument: everyone here pays gold into it, and every stage helps everyone here.",
    },
    ...(workshops === null
      ? []
      : [
          {
            mark: level(workshops.level),
            when: `From Company Level ${workshops.level}`,
            text: `Workshops: up to ${workshops.tiles} tiles at the Yard's ends for your own producers and Storages, joined to your gate without pipes, and the crew's Depot to keep goods for each other.`,
          },
        ]),
    {
      mark: "🔗",
      when: "Any time",
      text: "Links (the tab above): one good to a company in another Yard, by a pipe the game lays along the roads.",
    },
  ];
  return (
    <details className="yard-fold yard-how" open={!yard.connected}>
      <summary className="small">How your Yard works: what happens when ▸</summary>
      <ol className="yard-how-list small">
        {rows.map((r) => (
          <li key={r.when} className={r.mark === "🔒" ? "locked" : undefined}>
            <span className="yard-how-mark" aria-hidden="true">
              {r.mark}
            </span>
            <span>
              <strong>{r.when}:</strong> {r.text}
            </span>
          </li>
        ))}
      </ol>
    </details>
  );
}

/**
 * Your Yard (1.4 §7): who lives around it, what you sell to and buy from them and at what price, and what goes through
 * the Yard. The Yard is no buyer of its own: it passes what one resident has spare to the others who need it, each at
 * the seller's price for them.
 */
export function YardPanel() {
  const open = useAppStore((state) => state.yardOpen);
  const yard = useAppStore((state) => state.yard);
  const tab = useAppStore((state) => state.yardTab);
  const links = useAppStore((state) => state.links);
  const content = useAppStore((state) => state.content);
  const company = useAppStore((state) => state.company);
  const serverOffset = useAppStore((state) => state.serverOffsetMs);
  const now = useNow(60000) + serverOffset;
  if (!open || content === null || company === null) {
    return null;
  }
  const waiting = links?.links.filter((t) => t.state === "offered" && !t.youOffered).length ?? 0;
  // Buy all the rest (playtest: "there should be a button to finish all at the top"): when more than one line can be bought
  const buyable = (company.wants ?? []).filter((t) => t.orderCost !== null);
  const waitingCost = buyable.length > 1 ? buyable.reduce((sum, t) => sum + (t.orderCost ?? 0), 0) : null;
  return (
    <aside className="panel yard-panel" aria-label="Yard">
      <div className="panel-header">
        <div>
          <h3>
            {yard?.station ?? "Your Yard"} · <Term word="yard">Yard</Term>
          </h3>
          <div className="muted small">
            Four plots around one Yard: every seller sets its price for the crew
          </div>
        </div>
        <button
          className="icon-button"
          title="Close (Esc)"
          onClick={() => setState({ yardOpen: false, focusLink: null })}
        >
          ×
        </button>
      </div>
      <div className="tabs" role="tablist">
        <button
          role="tab"
          aria-selected={tab === "yard"}
          className={tab === "yard" ? "tab active" : "tab"}
          onClick={() => {
            setState({ yardTab: "yard", focusLink: null });
            void gameClient.refreshYard();
          }}
        >
          Yard
        </button>
        <button
          role="tab"
          aria-selected={tab === "links"}
          className={tab === "links" ? "tab active" : "tab"}
          onClick={() => void gameClient.openLinks()}
        >
          Links{waiting > 0 && <span className="badge">{waiting}</span>}
        </button>
      </div>
      {tab === "links" ? (
        <LinksTab />
      ) : yard === null ? (
        <p className="muted">Loading…</p>
      ) : (
        <>
          <p className={yard.connected ? "yard-line" : "yard-line todo"}>{yard.line}</p>
          {/* After run 4: Cleo added up 22 g/min of trades against her +112/min and asked where the rest came from */}
          {yard.incomeLine !== null && <p className="muted small">{yard.incomeLine}</p>}
          {!yard.connected && (
            <button className="primary wide" onClick={() => void gameClient.planYardPipe()}>
              Plan the pipe to my Yard gate
            </button>
          )}
          <YardHow yard={yard} company={company} />
          {yard.yours.length > 0 && (
            <section>
              <h4>Your trades</h4>
              <ul className="yard-trades">
                {yard.yours.map((t) => (
                  <li key={`${t.partner}-${t.resource}-${t.selling}`}>
                    <span>
                      <ResourceIcon resource={t.resource} /> {t.selling ? "You sell" : "You buy"}{" "}
                      {formatAmount(t.perMinute)}/min {content.resourceName(t.resource).toLowerCase()}{" "}
                      {t.selling ? "to" : "from"} <strong>{t.partnerName}</strong> at {t.price.toFixed(2)} g
                    </span>
                  </li>
                ))}
              </ul>
            </section>
          )}
          {/* Your own wants (after run 3, steps 2 and 3): what each waits for, what is in and coming, and Give it up */}
          {company !== null && (company.wants ?? []).length > 0 && (
            <section className="yard-waiting">
              <div className="yard-waiting-head">
                <h4>What you&apos;re waiting for</h4>
                {waitingCost !== null && (
                  <button
                    className="small primary yard-waiting-all"
                    onClick={() => void gameClient.orderAll()}
                  >
                    Buy all the rest · {formatAmount(waitingCost)} g
                  </button>
                )}
              </div>
              {(company.ownGoodsIntoWants ?? 0) >= 1 && (
                <p className="muted small">
                  Your own goods go in first: they sell for {formatAmount(company.ownGoodsIntoWants ?? 0)}{" "}
                  g/min less until they&apos;re in, and then the levels pay.
                </p>
              )}
              <ul className="yard-trades">
                {(company.wants ?? []).map((w) => (
                  <li key={`${w.target}-${w.resource}`}>
                    <span>
                      <ResourceIcon resource={w.resource} /> Your <strong>{w.name}</strong>
                      {w.tiles !== null && ` on ${w.tiles} tiles`} wants {formatAmount(w.amount)}{" "}
                      {content.resourceName(w.resource).toLowerCase()}
                      {w.toLevel != null && ` for level ${w.toLevel}`} ·{" "}
                      <strong>{formatAmount(w.got)} in</strong>
                      {w.perMinute > 0.005 && (
                        <span className="muted"> · {formatAmount(w.perMinute)}/min coming</span>
                      )}
                      {w.order !== null && <span className="muted"> · {w.order}</span>}
                    </span>
                    {w.orderCost !== null && (
                      <button
                        className="small"
                        onClick={() => void gameClient.orderRest(w.target, null, w.resource)}
                      >
                        Buy the rest · {formatAmount(w.orderCost)} g
                      </button>
                    )}
                    <button className="link small" onClick={() => void gameClient.cancelSite(w.target)}>
                      give it up
                    </button>
                  </li>
                ))}
              </ul>
            </section>
          )}
          {/* Neighbours' scaffolds you can help with (after run 3, step 2): your Storage at a tap, or your spare by itself */}
          {company !== null && (company.wantsNear ?? []).length > 0 && (
            <section>
              <h4>What your neighbours are building</h4>
              <ul className="yard-trades">
                {(company.wantsNear ?? []).map((w) => (
                  <li key={`${w.site}-${w.resource}`}>
                    <span>
                      <ResourceIcon resource={w.resource} /> <strong>{w.owner}</strong>
                      {`${possessive(w.owner)} ${w.building}`} wants {formatAmount(w.left)}{" "}
                      {content.resourceName(w.resource).toLowerCase()}
                      <span className="muted">
                        {w.holds >= 1
                          ? ` · your Storage holds ${formatAmount(w.holds)}`
                          : w.makes
                            ? " · your spare goes there by itself"
                            : ""}
                      </span>
                    </span>
                    {w.holds >= 1 && (
                      <button className="small" onClick={() => void gameClient.sendToSite(w.site)}>
                        Send them ▸
                      </button>
                    )}
                    {w.orderCost !== null && (
                      <button
                        className="link small"
                        onClick={() => void gameClient.orderRest(w.site, w.owner, w.resource)}
                      >
                        order it for {w.owner} · {formatAmount(w.orderCost)} g
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            </section>
          )}
          {yard.bids.length > 0 && (
            <section>
              <h4>Who could use your goods</h4>
              <ul className="yard-trades">
                {yard.bids.map((b) => (
                  <li key={`${b.buyer}-${b.resource}`}>
                    <span>
                      <ResourceIcon resource={b.resource} /> <strong>{b.buyerName}</strong>
                      {b.building !== null && `${possessive(b.buyerName)} ${b.building}`} could use{" "}
                      {formatAmount(b.perMinute)}/min {content.resourceName(b.resource).toLowerCase()}
                      <span className="muted"> · pipe it to your Yard gate and it goes there</span>
                    </span>
                  </li>
                ))}
              </ul>
            </section>
          )}
          {yard.trades.length > 0 && (
            <section>
              <h4>This week between you and your neighbours</h4>
              <ul className="yard-trades">
                {yard.trades.map((g) => (
                  <li key={`${g.partner}-${g.resource}`}>
                    <span>
                      <ResourceIcon resource={g.resource} /> <strong>{g.name}</strong>:{" "}
                      {[
                        g.given > 0 && `you sold ${formatAmount(g.given)}`,
                        g.received > 0 && `you bought ${formatAmount(g.received)}`,
                      ]
                        .filter(Boolean)
                        .join(" · ")}{" "}
                      {content.resourceName(g.resource).toLowerCase()}
                    </span>
                  </li>
                ))}
              </ul>
            </section>
          )}
          <section>
            <h4>Who lives here</h4>
            <p className="muted small">
              Spare: what each has left over and sells at its Trading Post to the town&apos;s market (the
              hub); a neighbour here who lacks it gets it first, at the seller&apos;s price. Lacks: what
              nobody here brings them yet.
            </p>
            <ul className="yard-residents">
              {yard.residents.map((r) => (
                <li key={r.id} className={r.id === company.id ? "me" : undefined}>
                  <div className="resident-name">
                    <span className="dot" style={{ background: r.color }} />
                    <strong>{r.id === company.id ? "You" : r.name}</strong>
                    <span className="muted small">
                      Lv {r.level}
                      {r.activity !== null && r.id !== company.id
                        ? ` · ${ACTIVITY[r.activity] ?? r.activity}`
                        : ""}
                      {r.connected ? "" : " · not piped to the Yard yet"}
                      {r.blocked ? " · blocked" : ""}
                    </span>
                  </div>
                  {(r.makes.length > 0 || r.needs.length > 0) && r.id !== company.id && (
                    <div className="resident-pins small">
                      📌{" "}
                      {[
                        ...r.makes.map((t) => `makes ${content.resourceName(t).toLowerCase()}`),
                        ...r.needs.map((t) => `needs ${content.resourceName(t).toLowerCase()}`),
                      ].join(" · ")}
                    </div>
                  )}
                  {r.id !== company.id && Object.keys(r.prices).length > 0 && (
                    // Their price for you (1.4 §8): what you pay them for each good they have spare
                    <div className="resident-pins small">
                      🏷{" "}
                      {Object.entries(r.prices)
                        .map(([good, price]) => `${content.resourceName(good).toLowerCase()} ${times(price)}`)
                        .join(" · ")}
                    </div>
                  )}
                  {r.connected && (
                    <div className="resident-rates small">
                      <span className="muted">
                        {r.id === company.id ? "Spare (sold at your Trading Post)" : "Spare"}
                      </span>{" "}
                      <Rates rates={r.spare} content={content} kind="surplus" />
                      {r.id === company.id && yard.yours.some((t) => t.selling) && (
                        <>
                          <span className="muted">To neighbours</span>{" "}
                          <Rates
                            rates={Object.values(
                              yard.yours
                                .filter((t) => t.selling)
                                .reduce<Record<string, ResourceRateDto>>((all, t) => {
                                  all[t.resource] = {
                                    resource: t.resource,
                                    perMinute: (all[t.resource]?.perMinute ?? 0) + t.perMinute,
                                  };
                                  return all;
                                }, {}),
                            )}
                            content={content}
                            kind="surplus"
                          />
                        </>
                      )}
                      <span className="muted">Lacks</span>{" "}
                      <Rates rates={r.lacks} content={content} kind="needs" />
                    </div>
                  )}
                </li>
              ))}
            </ul>
          </section>
          {yard.residents.find((t) => t.id === company.id) !== undefined && (
            // Folded (after run 4, Mia: "Yard panel is a lot of words (Spare/Lacks/Plan board/Through the Yard)")
            <details className="yard-fold">
              <summary className="small">📌 Plan board: say what you make or need ▸</summary>
              <PlanBoard me={yard.residents.find((t) => t.id === company.id)!} />
            </details>
          )}
          {yard.discoveries.length > 0 && (
            <section>
              <h4>🚩 Discoveries</h4>
              {yard.discoveries.map((d) => {
                const open = new Date(d.markingEnds).getTime() > now;
                return (
                  <div key={`${d.x},${d.y}`} className="discovery small">
                    <div>
                      <ResourceIcon resource={d.resource} /> A rich{" "}
                      {content.resourceName(d.resource).toLowerCase()} deposit near {d.near}{" "}
                      <button
                        className="link small"
                        onClick={() => gameClient.engine?.lookAt(d.x, d.y, 0.35)}
                      >
                        show
                      </button>
                    </div>
                    <div className="muted">
                      {open
                        ? "Every Yard whose players bring something out of it in its first three days gets its marker."
                        : "Its markers are given."}{" "}
                      {d.yourStation && "Your Yard has one. "}
                      {d.stations.length > 0 && `Marked: ${d.stations.join(", ")}.`}
                      {d.top.length > 0 && ` Most brought: ${d.top.join(", ")}.`}
                    </div>
                  </div>
                );
              })}
            </section>
          )}
          <DepotSection />
          {yard.totals.length > 0 && (
            <section>
              <h4>Through the Yard, all residents</h4>
              <Rates rates={yard.totals} content={content} kind="surplus" />
            </section>
          )}
          <span className="button-row">
            <button onClick={() => void gameClient.openTradeSettings()}>⚙ Trade settings</button>
            <button onClick={() => void gameClient.openLinks()}>🔗 Links to other Yards</button>
            <button onClick={() => void gameClient.openCrew()}>👥 Crew</button>
          </span>
        </>
      )}
    </aside>
  );
}
