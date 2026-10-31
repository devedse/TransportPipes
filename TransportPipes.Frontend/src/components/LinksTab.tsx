import { useEffect, useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { LinkDto, LinkPlanDto, LinkSuggestionDto } from "../api/contracts";
import type { Content } from "../game/content";
import { setState, useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";
import { TrunkLines } from "./TrunkLines";
import { Term } from "./Term";

/** How much of what is sent arrives, as players read it */
function arrives(efficiency: number): string {
  return `${Math.round(efficiency * 100)}% arrives`;
}

/** One link: the good and the partner, the state line, the tank at the buyer's gate, what you can do with it */
function LinkCard({ link, content }: { link: LinkDto; content: Content }) {
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const focus = useAppStore((s) => s.focusLink);
  const company = useAppStore((s) => s.company)!;
  const era = useAppStore((s) => s.era);
  const startsAt = link.startsAt != null ? new Date(link.startsAt).getTime() : null;
  // A starting link counts down by the second
  const now = useNow(startsAt !== null ? 1000 : 30000) + serverOffset;
  const pipe = content.pipeTiers.find((t) => t.tier === link.tier);
  const better = content.pipeTiers.find(
    (t) =>
      t.tier === link.tier + 1 &&
      t.era <= era &&
      (t.research === null || company.research.researched.includes(t.research)),
  );
  const good = content.resourceName(link.resource).toLowerCase();
  // How long the tank lasts at the need; when nothing is needed now, what is in it (it said "instant", full or empty)
  const tankLasts =
    link.tank <= 1e-6
      ? "empty"
      : link.need > 1e-6
        ? formatDuration((link.tank / link.need) * 60)
        : `${formatAmount(link.tank)} ${good}`;
  const waitingForYou = link.state === "offered" && !link.youOffered;
  return (
    <li className={`link-card ${link.tone}${focus === link.id ? " focus" : ""}`}>
      <div className="link-head">
        <ResourceIcon resource={link.resource} />
        <strong>{content.resourceName(link.resource)}</strong>
        <span className="link-arrow">{link.youSell ? "→" : "←"}</span>
        <span className="dot" style={{ background: link.partnerColor }} />
        <strong className="grow">{link.partnerName}</strong>
        {link.route.length >= 4 && (
          <button className="link small" onClick={() => gameClient.showLink(link.route, link.id)}>
            show
          </button>
        )}
      </div>
      {/* On a line of its own (playtest: beside a long name it was squeezed to a word a line) */}
      <div className="muted small">
        {pipe?.name ?? "Pipe"}, {link.length} tiles, {arrives(link.efficiency)}
      </div>
      <p className={`link-line ${link.tone}`}>{link.line}</p>
      {link.state !== "offered" && <LinkTerms link={link} good={good} />}
      {startsAt !== null && (
        <div className="link-starts small">
          {startsAt > now
            ? `First ${good} in ${formatDuration((startsAt - now) / 1000)}`
            : `First ${good} any moment now`}
        </div>
      )}
      {link.state !== "offered" && link.tankSize > 1e-6 && (
        <div
          className="link-tank"
          title={`${formatAmount(link.tank)} of ${formatAmount(link.tankSize)} ${good}`}
        >
          <span className="small muted">
            <Term word="tank">Tank</Term> at {link.youSell ? `${link.partnerName}'s` : "your"} gate
          </span>
          <span className="tank-bar">
            <span style={{ width: `${Math.min(100, (100 * link.tank) / link.tankSize)}%` }} />
          </span>
          <span className="small">{tankLasts}</span>
        </div>
      )}
      {link.rate > 0 && (
        <div className="muted small">
          {formatAmount(link.rate)}/min at {link.price.toFixed(2)} g · {formatAmount(link.delivered)} {good}{" "}
          delivered so far
        </div>
      )}
      {(link.sent ?? 0) > 0.05 && (
        // Where what goes into it goes (the owner, 2026-10-31: the seller's network said 100 sold through the Yard, the
        // buyer bought 25: the rest filled the buyer's tank, and the pipe lost 28% over its 109 tiles)
        <LinkSplit link={link} good={good} />
      )}
      <span className="button-row">
        {waitingForYou && (
          <>
            <button
              className="primary"
              onClick={() =>
                void gameClient.linkCommand(
                  gameClient.connection.acceptLink(link.id),
                  "The link is laid: its first goods come at the Yard's second round from now, within two minutes (its card counts down)",
                )
              }
            >
              Accept
            </button>
            <button onClick={() => void gameClient.linkCommand(gameClient.connection.declineLink(link.id))}>
              Decline
            </button>
          </>
        )}
        {link.state === "offered" && link.youOffered && (
          <>
            <span className="muted small">
              {link.offerEndsAt !== null &&
                `The offer stays ${formatDuration(Math.max(0, (new Date(link.offerEndsAt).getTime() - now) / 1000))}`}
            </span>
            <button
              className="link small"
              onClick={() =>
                void gameClient.linkCommand(gameClient.connection.declineLink(link.id), "Offer taken back")
              }
            >
              take back
            </button>
          </>
        )}
        {link.state !== "offered" && link.canResume && (
          <button
            onClick={() =>
              void gameClient.linkCommand(
                gameClient.connection.resumeLink(link.id),
                link.state === "paused"
                  ? "The link flows again from the Yard's second round from now, within two minutes"
                  : "Notice taken back",
              )
            }
          >
            {link.state === "paused" ? "Resume" : "Keep the link (cancel the end)"}
          </button>
        )}
        {link.state === "active" && link.noticeAt === null && (
          <>
            <button
              onClick={() =>
                void gameClient.linkCommand(gameClient.connection.endLink(link.id, false), "Paused")
              }
            >
              Pause
            </button>
            <button
              className="link small danger"
              onClick={() =>
                void gameClient.linkCommand(gameClient.connection.endLink(link.id, true), "Ended")
              }
            >
              end
            </button>
          </>
        )}
        {link.state === "paused" && (
          <button
            className="link small danger"
            onClick={() => void gameClient.linkCommand(gameClient.connection.endLink(link.id, true), "Ended")}
          >
            end
          </button>
        )}
        {link.state !== "offered" && better !== undefined && (
          // Its whole price, the materials too, and what it changes (the audit, 2026-10-31: it showed the gold alone,
          // and failed after the click with "You need 109 copper ingot")
          <button
            className="small"
            title={`${better.name}: ${Math.round((1 - better.lossPerTile) ** link.length * 100)}% of what is sent arrives (now ${Math.round(link.efficiency * 100)}%), carries ${formatAmount(better.throughput)}/min; it takes ${Object.entries(
              better.materials,
            )
              .map(([r, a]) => `${formatAmount(a * link.length)} ${content.resourceName(r).toLowerCase()}`)
              .join(", ")} from your Storages too; it counts from the Yard's next round`}
            onClick={() =>
              void gameClient.linkCommand(
                gameClient.connection.upgradeLink(link.id, better.tier),
                `${better.name} laid: it counts from the Yard's next round`,
              )
            }
          >
            {better.name} · {formatAmount(better.gold * link.length)} g
            {Object.entries(better.materials).map(([resource, amount]) => (
              <span key={resource} className="level-good">
                {" "}
                + {formatAmount(amount * link.length)}
                <ResourceIcon resource={resource} />
              </span>
            ))}
            <span className="level-goods">
              {Math.round((1 - better.lossPerTile) ** link.length * 100)}% arrives instead of{" "}
              {Math.round(link.efficiency * 100)}%
            </span>
          </button>
        )}
      </span>
    </li>
  );
}

/**
 * What a link's pipe carries a minute and where it goes: what arrives for the buyer's buildings (paid), what fills the
 * buyer's tank (paid when it is used), what the pipe loses on its length (nobody pays for it)
 */
function LinkSplit({ link, good }: { link: LinkDto; good: string }) {
  const sent = link.sent ?? 0;
  const arrives = sent * link.efficiency;
  const lost = sent - arrives;
  const fill = Math.max(0, arrives - link.rate);
  const tankLeft = Math.max(0, link.tankSize - link.tank);
  return (
    <div className="link-split small">
      <div>
        {link.youSell ? "You send" : `${link.partnerName} sends`} {formatAmount(sent)} {good}/min into it:
      </div>
      <ul>
        <li>
          {formatAmount(link.rate)} arrive for {link.youSell ? `${link.partnerName}'s` : "your"} buildings,
          paid {link.price.toFixed(2)} g each
        </li>
        {fill > 0.05 && (
          <li>
            {formatAmount(fill)} fill the reserve tank at {link.youSell ? `${link.partnerName}'s` : "your"}{" "}
            gate (it holds two hours of supply; paid as it is used)
            {tankLeft > 0.5 && `, full in ${formatDuration((tankLeft / fill) * 60)}`}
          </li>
        )}
        {lost > 0.05 && (
          <li>
            {formatAmount(lost)} are lost on the {link.length} tiles of the pipe (
            {Math.round((1 - link.efficiency) * 100)}%: the longer the pipe, the more it loses; a better pipe
            loses less) — nobody pays for it
          </li>
        )}
      </ul>
    </div>
  );
}

/**
 * A link's promise and cap, and changing them (the owner, 2026-10-31): what binds yourself or helps the other counts
 * from the next Yard round, what binds the other is asked of them, who accepts or declines
 */
function LinkTerms({ link, good }: { link: LinkDto; good: string }) {
  const [editing, setEditing] = useState<{ guaranteed: number; upTo: number } | null>(null);
  const guaranteed = link.guaranteed ?? 0;
  const upTo = link.upTo ?? 0;
  const seller = link.youSell ? "You" : link.partnerName;
  const asked = link.askedGuaranteed != null || link.askedUpTo != null;
  return (
    <div className="link-terms small">
      <div>
        <Term word="guaranteed">Guaranteed</Term>{" "}
        <strong>{guaranteed > 0 ? `${formatAmount(guaranteed)}/min` : "nothing"}</strong> ·{" "}
        <Term word="upTo">Up to</Term>{" "}
        <strong>{upTo > 0 ? `${formatAmount(upTo)}/min` : "all the pipe brings"}</strong>
        {editing === null && !asked && (
          <button className="link small" onClick={() => setEditing({ guaranteed, upTo })}>
            change
          </button>
        )}
      </div>
      <div className="muted">
        The pipe brings at most {formatAmount(link.pipeMax ?? 0)} {good}/min. {seller}{" "}
        {link.youSell ? "make" : "makes"} {formatAmount(link.sellerMakes ?? 0)}/min of it,{" "}
        {formatAmount(link.sellerSpare ?? 0)} spare (what {link.youSell ? "your" : "their"} own buildings
        don&apos;t use).
      </div>
      {asked &&
        (link.askedByYou ? (
          <div className="muted">
            You asked for {formatAmount(link.askedGuaranteed ?? 0)}/min guaranteed, up to{" "}
            {(link.askedUpTo ?? 0) > 0 ? `${formatAmount(link.askedUpTo ?? 0)}/min` : "all the pipe brings"}:
            waiting for {link.partnerName}.
          </div>
        ) : (
          <div className="link-ask">
            <span>
              {link.partnerName} asks for {formatAmount(link.askedGuaranteed ?? 0)}/min guaranteed, up to{" "}
              {(link.askedUpTo ?? 0) > 0 ? `${formatAmount(link.askedUpTo ?? 0)}/min` : "all the pipe brings"}
              .
            </span>
            <span className="button-row">
              <button
                className="primary small"
                onClick={() =>
                  void gameClient.linkCommand(
                    gameClient.connection.answerLinkTerms(link.id, true),
                    "Accepted: it counts from the Yard's next round",
                  )
                }
              >
                Accept
              </button>
              <button
                className="small"
                onClick={() =>
                  void gameClient.linkCommand(gameClient.connection.answerLinkTerms(link.id, false))
                }
              >
                Decline
              </button>
            </span>
          </div>
        ))}
      {editing !== null && (
        <>
          <TermsInputs
            guaranteed={editing.guaranteed}
            upTo={editing.upTo}
            most={Math.min(link.sellerMakes ?? 0, link.pipeMax ?? 0)}
            onChange={(g, u) => setEditing({ guaranteed: g, upTo: u })}
          />
          <p className="muted">
            {link.youSell
              ? "Your changes count from the Yard's next round."
              : "Lowering counts from the Yard's next round; asking for more is asked of " +
                link.partnerName +
                ", who accepts or declines."}
          </p>
          <span className="button-row">
            <button
              className="primary small"
              onClick={() =>
                void gameClient
                  .linkCommand(
                    gameClient.connection.setLinkTerms(link.id, editing.guaranteed, editing.upTo),
                    "Saved: it counts from the Yard's next round",
                  )
                  .then((ok) => ok && setEditing(null))
              }
            >
              Save
            </button>
            <button className="small" onClick={() => setEditing(null)}>
              Cancel
            </button>
          </span>
        </>
      )}
    </div>
  );
}

interface Draft {
  partnerId: number | null;
  resource: string;
  selling: boolean;
  tier: number;
  /** What the seller promises a minute, and the most in all (0: what the pipe carries) */
  guaranteed: number;
  upTo: number;
}

/**
 * A link's two numbers (the owner, 2026-10-31: "25 guaranteed, but 100 on reserve ... 0 guaranteed and 100 reserved
 * would be possible too, then it would just only do your spare"): Guaranteed comes before the seller's own buildings,
 * Up to is the seller's cap
 */
function TermsInputs({
  guaranteed,
  upTo,
  most,
  onChange,
}: {
  guaranteed: number;
  upTo: number;
  /** The most that can be promised: what the seller makes, what the pipe brings */
  most: number;
  onChange: (guaranteed: number, upTo: number) => void;
}) {
  return (
    <div className="link-terms-inputs">
      <label className="small">
        <Term word="guaranteed">Guaranteed</Term>
        <input
          type="number"
          min={0}
          max={Math.floor(most)}
          step={1}
          value={guaranteed}
          onChange={(e) => onChange(Math.max(0, Number(e.target.value) || 0), upTo)}
        />
        <span className="muted">a minute</span>
      </label>
      <label className="small">
        <Term word="upTo">Up to</Term>
        <input
          type="number"
          min={0}
          step={1}
          value={upTo}
          onChange={(e) => onChange(guaranteed, Math.max(0, Number(e.target.value) || 0))}
        />
        <span className="muted">a minute{upTo === 0 ? " (0: all the pipe brings)" : ""}</span>
      </label>
    </div>
  );
}

/** A new link: to whom, which good, which way, which pipe; the plan shows the route, the loss and the cost */
function NewLink({
  draft,
  onChange,
  content,
}: {
  draft: Draft;
  onChange: (d: Draft) => void;
  content: Content;
}) {
  const links = useAppStore((s) => s.links)!;
  const company = useAppStore((s) => s.company)!;
  const era = useAppStore((s) => s.era);
  // The plan for the draft as it is now (an older answer is never shown for a newer draft)
  const key = `${draft.partnerId}:${draft.resource}:${draft.selling}:${draft.tier}`;
  const [planned, setPlanned] = useState<{ key: string; plan: LinkPlanDto | null } | null>(null);
  useEffect(() => {
    if (draft.partnerId === null || draft.resource === "") {
      return;
    }
    let cancelled = false;
    void gameClient.connection
      .planLink(draft.partnerId, draft.resource, draft.selling, draft.tier)
      .then((p) => !cancelled && setPlanned({ key, plan: p }));
    return () => {
      cancelled = true;
    };
  }, [key, draft.partnerId, draft.resource, draft.selling, draft.tier]);
  const plan =
    draft.partnerId !== null && draft.resource !== "" && planned?.key === key ? planned.plan : null;
  const tiers = content.pipeTiers.filter(
    (t) => t.era <= era && (t.research === null || company.research.researched.includes(t.research)),
  );
  const resources = content.resources.filter((r) => r.era <= era);
  const partner = links.partners.find((t) => t.id === draft.partnerId);
  return (
    <div className="new-link">
      <div className="link-form">
        <select
          aria-label="Partner"
          value={draft.partnerId ?? ""}
          onChange={(e) =>
            onChange({ ...draft, partnerId: e.target.value === "" ? null : Number(e.target.value) })
          }
        >
          <option value="">Choose a company…</option>
          {links.partners.map((p) => (
            <option key={p.id} value={p.id}>
              {p.crew ? "👥 " : ""}
              {p.name}
              {p.crew ? " (your crew)" : ""} · Lv {p.level} · {p.station}
            </option>
          ))}
        </select>
        <select
          aria-label="Direction"
          value={draft.selling ? "sell" : "buy"}
          onChange={(e) => onChange({ ...draft, selling: e.target.value === "sell" })}
        >
          <option value="sell">I sell them</option>
          <option value="buy">I buy from them</option>
        </select>
        <select
          aria-label="Good"
          value={draft.resource}
          onChange={(e) => onChange({ ...draft, resource: e.target.value })}
        >
          <option value="">Which good…</option>
          {resources.map((r) => (
            <option key={r.id} value={r.id}>
              {r.name}
            </option>
          ))}
        </select>
        <select
          aria-label="Pipe"
          value={draft.tier}
          onChange={(e) => onChange({ ...draft, tier: Number(e.target.value) })}
        >
          {tiers.map((t) => (
            <option key={t.tier} value={t.tier}>
              {t.name}
            </option>
          ))}
        </select>
      </div>
      {plan !== null && (
        <p className={plan.error !== null ? "form-error" : "small"}>
          {plan.error ??
            `${plan.length} tiles long. ${arrives(plan.efficiency)}: pipes lose goods on the way. Carries up to ${formatAmount(plan.throughput)}/min. The pipe costs ${formatAmount(plan.gold)} g${plan.materials
              .filter((m) => m.perMinute > 0)
              .map((m) => ` + ${formatAmount(m.perMinute)} ${content.resourceName(m.resource).toLowerCase()}`)
              .join("")}`}
        </p>
      )}
      {plan !== null && plan.error === null && (
        <>
          <TermsInputs
            guaranteed={draft.guaranteed}
            upTo={draft.upTo}
            most={Math.min(plan.sellerMakes ?? 0, plan.throughput * plan.efficiency)}
            onChange={(guaranteed, upTo) => onChange({ ...draft, guaranteed, upTo })}
          />
          <p className="muted small">
            {draft.selling ? "You make" : `${partner?.name ?? "They"} make`}{" "}
            {formatAmount(plan.sellerMakes ?? 0)} {content.resourceName(draft.resource).toLowerCase()} a
            minute where the link starts: the most that can be guaranteed.{" "}
            {draft.guaranteed > 0
              ? `${formatAmount(draft.guaranteed)} a minute comes before ${draft.selling ? "your" : "their"} own buildings, as far as the buyer's buildings use it.`
              : "Nothing guaranteed: only what the seller has spare."}
          </p>
        </>
      )}
      <button
        className="primary"
        disabled={plan === null || plan.error !== null || partner === undefined}
        onClick={() =>
          draft.partnerId !== null &&
          void gameClient
            .linkCommand(
              gameClient.connection.offerLink(
                draft.partnerId,
                draft.resource,
                draft.selling,
                draft.tier,
                draft.guaranteed,
                draft.upTo,
              ),
              `Offered to ${partner?.name ?? "them"}: it starts when they accept`,
            )
            .then((ok) => ok && onChange({ ...draft, partnerId: null, resource: "" }))
        }
      >
        Offer the link
      </button>
      <p className="muted small">
        They accept with one tap. You pay for the pipe (refunded if they don&apos;t accept). The buyer pays
        the seller&apos;s price, and either side can pause or end it at any time.
      </p>
    </div>
  );
}

function Suggestion({ s, content, onPick }: { s: LinkSuggestionDto; content: Content; onPick: () => void }) {
  const good = content.resourceName(s.resource).toLowerCase();
  return (
    <li className="link-suggestion">
      <ResourceIcon resource={s.resource} />
      <span className="grow">
        <span className="dot" style={{ background: s.partnerColor }} /> <strong>{s.partnerName}</strong>{" "}
        <span className="muted small">
          ({s.station}){" "}
          {s.youSell
            ? `needs ${formatAmount(s.perMinute)} ${good}/min you have spare`
            : `has ${formatAmount(s.perMinute)} ${good}/min spare you need`}{" "}
          · {s.length} tiles, {arrives(s.efficiency)}
        </span>
      </span>
      <button className="small" onClick={onPick}>
        Link
      </button>
    </li>
  );
}

/**
 * Links (1.4 §8): standing deals with players in other Yards of your town, on a two-ended pipe along the roads, at the
 * seller's price for the buyer. Offers waiting for you come first, then your links with their state line and tank, then
 * who a link would suit.
 */
export function LinksTab() {
  const links = useAppStore((s) => s.links);
  const content = useAppStore((s) => s.content);
  const linkPartner = useAppStore((s) => s.linkPartner);
  // The Crew panel's "Link ▸": the new link starts with that friend
  const [draft, setDraft] = useState<Draft>(() => ({
    partnerId: linkPartner,
    resource: "",
    selling: true,
    tier: 1,
    guaranteed: 0,
    upTo: 0,
  }));
  useEffect(() => {
    if (linkPartner !== null) {
      setState({ linkPartner: null });
    }
  }, [linkPartner]);
  useEffect(() => {
    // The flows change every round: the tab follows
    const timer = window.setInterval(() => void gameClient.refreshLinks(), 30000);
    return () => window.clearInterval(timer);
  }, []);
  if (links === null || content === null) {
    return <p className="muted">Loading…</p>;
  }
  const laid = links.links.filter((t) => t.state !== "offered").length;
  return (
    <>
      {/* One sentence and a picture (after run 4, Mia-B: "lots of words (Yard gate, trunks) - I'd need a picture") */}
      <div className="link-picture" aria-hidden="true">
        <span className="link-end">🏠 your Yard gate</span>
        <span className="link-pipe">one good ▶</span>
        <span className="link-end">🏠 another company</span>
      </div>
      <p className="small">
        <strong>A link is a pipe that sends one good to one company</strong>, at the seller&apos;s price, at
        every Yard round. It can come with a promise (Guaranteed) and a cap (Up to). Your Yard neighbours need
        none to share spare goods: the Yard does that by itself.
      </p>
      {links.links.length > 0 ? (
        <ul className="link-list">
          {links.links.map((link) => (
            <LinkCard key={link.id} link={link} content={content} />
          ))}
        </ul>
      ) : (
        <p className="muted small">No links yet.</p>
      )}
      <div className="muted small">
        {laid} {laid === 1 ? "link" : "links"} (you can have {links.max})
      </div>
      {/* Friends who live in other stations (after run 3, row 12: Ben was offered as a link with no word he was Ana's friend) */}
      {links.suggestions.length > 0 && (
        <section>
          <h4>A link would suit</h4>
          <ul className="link-suggestions">
            {links.suggestions.map((s) => (
              <Suggestion
                key={`${s.partnerId}-${s.resource}-${s.youSell}`}
                s={s}
                content={content}
                onPick={() =>
                  setDraft({ ...draft, partnerId: s.partnerId, resource: s.resource, selling: s.youSell })
                }
              />
            ))}
          </ul>
        </section>
      )}
      <section>
        <h4>New link</h4>
        {links.partners.length === 0 ? (
          <p className="muted small">Nobody lives outside your Yard yet.</p>
        ) : (
          <NewLink draft={draft} onChange={setDraft} content={content} />
        )}
      </section>
      <TrunkLines />
    </>
  );
}
