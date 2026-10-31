import { useEffect, useState } from "react";
import type { GoldUsesDto } from "../api/contracts";
import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import { setState, useAppStore, type TradesTab } from "../state/store";
import { IncomeBreakdown } from "./GoldPanel";
import { LinksTab } from "./LinksTab";
import { TownList } from "./NeighboursPanel";
import { ResourceIcon } from "./ResourceIcon";
import { Term } from "./Term";
import { TradeSettings } from "./TradeSettings";

const TABS: Array<{ id: TradesTab; label: string; goal: string }> = [
  { id: "now", label: "Now", goal: "What you sell and buy this minute, and with whom." },
  {
    id: "links",
    label: "Links",
    goal: "Pipes that send one good to one company: its promise, its cap, and new ones.",
  },
  {
    id: "settings",
    label: "Settings",
    goal: "Your prices, what you sell to and buy from players, and whom you don't trade with.",
  },
  { id: "town", label: "Town", goal: "Everyone in your town: what they have spare and need." },
];

/**
 * Every trade in one place (the owner, 2026-10-31: "a separate button where we can clearly see these trades happening
 * would make sense. A lot of these things are now at the bottom of other menus ... each menu has a very clear goal"):
 * what runs now, the links, the trade settings and the town. The Yard panel keeps who lives around you and what you do
 * together.
 */
export function TradesPanel() {
  const open = useAppStore((s) => s.tradesOpen);
  const tab = useAppStore((s) => s.tradesTab);
  const links = useAppStore((s) => s.links);
  const content = useAppStore((s) => s.content);
  if (!open || content === null) {
    return null;
  }
  const asks =
    links?.links.filter(
      (t) => (t.state === "offered" && !t.youOffered) || (t.askedGuaranteed != null && !t.askedByYou),
    ).length ?? 0;
  const goal = TABS.find((t) => t.id === tab)?.goal ?? "";
  const close = () => setState({ tradesOpen: false });
  // In the middle of the screen, as the trading window was (the owner, 2026-10-31: "having it in the middle screen
  // makes more sense like it was before")
  return (
    <div className="modal-backdrop" onClick={close}>
      <div
        className="panel yard-panel trades-panel"
        role="dialog"
        aria-label="Trades"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="panel-header">
          <div>
            <h3>Trades</h3>
            <div className="muted small">Who you sell to and buy from, and at what price</div>
          </div>
          <button className="icon-button" title="Close (Esc)" onClick={close}>
            ×
          </button>
        </div>
        <div className="tabs" role="tablist">
          {TABS.map((t) => (
            <button
              key={t.id}
              role="tab"
              aria-selected={tab === t.id}
              className={tab === t.id ? "tab active" : "tab"}
              onClick={() => void gameClient.openTrades(t.id)}
            >
              {t.label}
              {t.id === "links" && asks > 0 && <span className="tab-badge">{asks}</span>}
            </button>
          ))}
        </div>
        <p className="muted small trades-goal">{goal}</p>
        {tab === "now" && <TradesNow />}
        {tab === "links" && <LinksTab />}
        {tab === "settings" && <TradeSettings />}
        {tab === "town" && <TownList />}
      </div>
    </div>
  );
}

/** What runs this minute: every line of your gold a minute by whom, and your links */
function TradesNow() {
  const content = useAppStore((s) => s.content)!;
  const links = useAppStore((s) => s.links);
  const [uses, setUses] = useState<GoldUsesDto | null>(null);
  useEffect(() => {
    let current = true;
    const load = () => {
      void gameClient.connection.getGoldUses().then(
        (t) => current && setUses(t),
        () => undefined,
      );
      void gameClient.refreshLinks();
    };
    load();
    const timer = window.setInterval(load, 15000);
    return () => {
      current = false;
      window.clearInterval(timer);
    };
  }, []);
  const laid = links?.links.filter((t) => t.state !== "offered") ?? [];
  return (
    <>
      <p className="muted small">
        It changes when the town&apos;s market looks again (every minute) and at every{" "}
        <Term word="yardRound">Yard round</Term> (the clock in the top bar).
      </p>
      {uses?.income != null ? (
        <IncomeBreakdown income={uses.income} content={content} />
      ) : (
        <p className="muted small">Looking…</p>
      )}
      <section>
        <h4>Your links</h4>
        {laid.length === 0 ? (
          <p className="muted small">
            None yet. A <Term word="link">link</Term> sends one good to one company, with a promise if you
            like.
          </p>
        ) : (
          <ul className="flow-list">
            {laid.map((link) => (
              <li key={link.id}>
                <ResourceIcon resource={link.resource} />
                <span>
                  {content.resourceName(link.resource)} {link.youSell ? "→" : "←"} {link.partnerName}
                </span>
                <span className="flow-amount">
                  {formatAmount(link.rate)}/min
                  {(link.guaranteed ?? 0) > 0 && (
                    <span className="muted"> · {formatAmount(link.guaranteed ?? 0)} guaranteed</span>
                  )}
                </span>
              </li>
            ))}
          </ul>
        )}
        <button className="link small" onClick={() => void gameClient.openTrades("links")}>
          All links and new ones ›
        </button>
      </section>
    </>
  );
}
