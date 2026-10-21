import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import type { ResourceRateDto } from "../api/contracts";
import type { Content } from "../game/content";
import { setState, useAppStore } from "../state/store";
import { LinksTab } from "./LinksTab";
import { ResourceIcon } from "./ResourceIcon";

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

/** What each kind of House does for the station */
const HOUSE_KIND: Record<string, string> = {
  reliable: "a steady supplier",
  collector: "buys what the station makes",
  grower: "grows with the station",
  neighbourInNeed: "needs what it lacks from the station",
};

const ACTIVITY: Record<string, string> = {
  active: "lively",
  resting: "resting",
  dormant: "away a while",
  asleep: "asleep",
};

/**
 * Your station and its Yard (v1.2 §7): who lives here, what you sell to and buy from them, and what goes through the
 * Yard. The Yard is no buyer of its own: it passes what one resident has spare to the others who need it, at one price.
 */
export function YardPanel() {
  const open = useAppStore((state) => state.yardOpen);
  const yard = useAppStore((state) => state.yard);
  const tab = useAppStore((state) => state.yardTab);
  const links = useAppStore((state) => state.links);
  const content = useAppStore((state) => state.content);
  const company = useAppStore((state) => state.company);
  if (!open || content === null || company === null) {
    return null;
  }
  const waiting = links?.links.filter((t) => t.state === "offered" && !t.youOffered).length ?? 0;
  return (
    <aside className="panel yard-panel" aria-label="Yard">
      <div className="panel-header">
        <div>
          <h3>{yard?.station ?? "Your station"} · Yard</h3>
          <div className="muted small">
            {yard?.small ? "A small station of two seats" : "A station of four seats"}: one fair price for
            everybody
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
          {!yard.connected && (
            <button className="primary wide" onClick={() => void gameClient.planYardPipe()}>
              Plan the pipe to my Yard gate
            </button>
          )}
          {yard.yours.length > 0 && (
            <section>
              <h4>Your trades</h4>
              <ul className="yard-trades">
                {yard.yours.map((t) => (
                  <li key={`${t.partner}-${t.resource}-${t.selling}`}>
                    <ResourceIcon resource={t.resource} /> {t.selling ? "You sell" : "You buy"}{" "}
                    {formatAmount(t.perMinute)}/min {content.resourceName(t.resource).toLowerCase()}{" "}
                    {t.selling ? "to" : "from"} <strong>{t.partnerName}</strong> at {t.price.toFixed(2)} g
                  </li>
                ))}
              </ul>
            </section>
          )}
          <section>
            <h4>Who lives here</h4>
            <ul className="yard-residents">
              {yard.residents.map((r) => (
                <li key={r.id} className={r.id === company.id ? "me" : undefined}>
                  <div className="resident-name">
                    <span className="dot" style={{ background: r.color }} />
                    <strong>{r.id === company.id ? "You" : r.name}</strong>
                    {r.isNpc && (
                      <span
                        className="badge house"
                        title="A House: an NPC company that trades with the station"
                      >
                        ⚙ House
                      </span>
                    )}
                    <span className="muted small">
                      Lv {r.level}
                      {r.house !== null && ` · ${HOUSE_KIND[r.house] ?? r.house}`}
                      {r.standIn !== null
                        ? ` · asleep: ${r.standIn} ⚙ stands in`
                        : r.activity !== null && r.id !== company.id
                          ? ` · ${ACTIVITY[r.activity] ?? r.activity}`
                          : ""}
                      {r.connected ? "" : " · not piped to the Yard yet"}
                      {r.blocked ? " · blocked" : ""}
                    </span>
                  </div>
                  {r.connected && (
                    <div className="resident-rates small">
                      <span className="muted">Spare</span>{" "}
                      <Rates rates={r.spare} content={content} kind="surplus" />
                      <span className="muted">Lacks</span>{" "}
                      <Rates rates={r.lacks} content={content} kind="needs" />
                    </div>
                  )}
                </li>
              ))}
            </ul>
          </section>
          {yard.totals.length > 0 && (
            <section>
              <h4>Through the Yard</h4>
              <Rates rates={yard.totals} content={content} kind="surplus" />
            </section>
          )}
          <span className="button-row">
            <button onClick={() => void gameClient.openTradeSettings()}>⚙ Trade settings</button>
            <button onClick={() => void gameClient.openLinks()}>🔗 Links to other stations</button>
            <button onClick={() => void gameClient.openCrew()}>👥 Crew</button>
          </span>
        </>
      )}
    </aside>
  );
}
