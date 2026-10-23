import { useEffect, useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { CrewDto, CrewMemberDto, LinkDto, LinkPlanDto, LinkSuggestionDto } from "../api/contracts";
import type { Content } from "../game/content";
import { setState, useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";
import { TrunksSection } from "./TrunksSection";

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
  const now = useNow(30000) + serverOffset;
  const pipe = content.pipeTiers.find((t) => t.tier === link.tier);
  const better = content.pipeTiers.find(
    (t) =>
      t.tier === link.tier + 1 &&
      t.era <= era &&
      (t.research === null || company.researched.includes(t.research)),
  );
  const good = content.resourceName(link.resource).toLowerCase();
  const tankMinutes = link.need > 1e-6 ? link.tank / link.need : 0;
  const waitingForYou = link.state === "offered" && !link.youOffered;
  return (
    <li className={`link-card ${link.tone}${focus === link.id ? " focus" : ""}`}>
      <div className="link-head">
        <ResourceIcon resource={link.resource} />
        <strong>{content.resourceName(link.resource)}</strong>
        <span className="link-arrow">{link.youSell ? "→" : "←"}</span>
        <span className="dot" style={{ background: link.partnerColor }} />
        <strong>{link.partnerName}</strong>
        <span className="muted small grow">
          {" "}
          · {pipe?.name ?? "Pipe"}, {link.length} tiles, {arrives(link.efficiency)}
        </span>
        {link.route.length >= 4 && (
          <button className="link small" onClick={() => gameClient.showLink(link.route, link.id)}>
            show
          </button>
        )}
      </div>
      <p className={`link-line ${link.tone}`}>{link.line}</p>
      {link.state !== "offered" && link.tankSize > 1e-6 && (
        <div
          className="link-tank"
          title={`${formatAmount(link.tank)} of ${formatAmount(link.tankSize)} ${good}`}
        >
          <span className="small muted">Tank at {link.youSell ? `${link.partnerName}'s` : "your"} gate</span>
          <span className="tank-bar">
            <span style={{ width: `${Math.min(100, (100 * link.tank) / link.tankSize)}%` }} />
          </span>
          <span className="small">{formatDuration(tankMinutes * 60)}</span>
        </div>
      )}
      {link.rate > 0 && (
        <div className="muted small">
          {formatAmount(link.rate)}/min at {link.price.toFixed(2)} g · {formatAmount(link.delivered)} {good}{" "}
          delivered so far
        </div>
      )}
      <span className="button-row">
        {waitingForYou && (
          <>
            <button
              className="primary"
              onClick={() =>
                void gameClient.linkCommand(gameClient.connection.acceptLink(link.id), "The link is laid")
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
                link.state === "paused" ? "The link flows again" : "Notice taken back",
              )
            }
          >
            {link.state === "paused" ? "Resume" : "Take back the notice"}
          </button>
        )}
        {link.state === "active" && link.noticeAt === null && (
          <>
            <button
              onClick={() =>
                void gameClient.linkCommand(
                  gameClient.connection.endLink(link.id, false),
                  "It pauses in an hour",
                )
              }
            >
              Pause
            </button>
            <button
              className="link small danger"
              onClick={() =>
                void gameClient.linkCommand(
                  gameClient.connection.endLink(link.id, true),
                  "It ends in an hour",
                )
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
          <button
            className="small"
            title={`${better.name}: loses ${(better.lossPerTile * 100).toFixed(2)}% a tile, carries ${formatAmount(better.throughput)}/min`}
            onClick={() =>
              void gameClient.linkCommand(
                gameClient.connection.upgradeLink(link.id, better.tier),
                `${better.name} laid`,
              )
            }
          >
            {better.name} · {formatAmount(better.gold * link.length)} g
          </button>
        )}
      </span>
    </li>
  );
}

/** Crew mates who live in another station than you */
function elsewhere(crew: CrewDto): CrewMemberDto[] {
  const mine = crew.members.find((t) => t.you)?.station;
  return crew.members.filter((t) => !t.you && t.station !== mine);
}

interface Draft {
  partnerId: number | null;
  resource: string;
  selling: boolean;
  tier: number;
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
    (t) => t.era <= era && (t.research === null || company.researched.includes(t.research)),
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
            `${plan.length} tiles through the corridors · ${arrives(plan.efficiency)} · carries ${formatAmount(plan.throughput)}/min · the pipe costs ${formatAmount(plan.gold)} g${plan.materials
              .filter((m) => m.perMinute > 0)
              .map((m) => ` + ${formatAmount(m.perMinute)} ${content.resourceName(m.resource).toLowerCase()}`)
              .join("")}`}
        </p>
      )}
      <button
        className="primary"
        disabled={plan === null || plan.error !== null || partner === undefined}
        onClick={() =>
          draft.partnerId !== null &&
          void gameClient
            .linkCommand(
              gameClient.connection.offerLink(draft.partnerId, draft.resource, draft.selling, draft.tier),
              `Offered to ${partner?.name ?? "them"}: it starts when they accept`,
            )
            .then((ok) => ok && onChange({ ...draft, partnerId: null, resource: "" }))
        }
      >
        Offer the link
      </button>
      <p className="muted small">
        They accept with one tap; you pay the pipe (refunded if they don&apos;t). Both sides pay and earn the
        one player price, and either side pauses or ends it with an hour&apos;s notice.
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
 * Links (v1.2 §7): trade with anyone outside your station on a two-ended pipe through the corridors, at the one player
 * price. Offers waiting for you come first, then your links with their state line and tank, then who a link would suit.
 */
export function LinksTab() {
  const links = useAppStore((s) => s.links);
  const content = useAppStore((s) => s.content);
  const crew = useAppStore((s) => s.crew?.crew ?? null);
  const linkPartner = useAppStore((s) => s.linkPartner);
  // The Crew panel's "Link ▸": the new link starts with that friend
  const [draft, setDraft] = useState<Draft>(() => ({
    partnerId: linkPartner,
    resource: "",
    selling: true,
    tier: 1,
  }));
  useEffect(() => {
    if (linkPartner !== null) {
      setState({ linkPartner: null });
    }
  }, [linkPartner]);
  useEffect(() => {
    if (crew === null) {
      void gameClient.refreshCrew();
    }
  }, [crew]);
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
      <p className="muted small">
        A link joins your Yard gate to one other company&apos;s, outside your station: one good, one way, at
        the one player price. What it doesn&apos;t bring, the Yard and the hub do.
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
        {laid} of {links.max} links
      </div>
      {/* Friends who live in other stations (after run 3, row 12: Ben was offered as a link with no word he was Ana's friend) */}
      {crew !== null && elsewhere(crew).length > 0 && (
        <section>
          <h4>Your crew in other stations</h4>
          <ul className="link-suggestions">
            {elsewhere(crew).map((m) => (
              <li key={m.id}>
                <span className="dot" style={{ background: m.color }} /> <strong>{m.name}</strong>{" "}
                <span className="muted small">· {m.station}</span>
                <span className="button-row">
                  <button className="small" onClick={() => void gameClient.visitFriend(m.id)}>
                    👁 Visit
                  </button>
                  <button className="small" onClick={() => setDraft({ ...draft, partnerId: m.id })}>
                    Link to {m.name} ▸
                  </button>
                </span>
              </li>
            ))}
          </ul>
        </section>
      )}
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
      <TrunksSection trunks={links.trunks} />
      <section>
        <h4>New link</h4>
        {links.partners.length === 0 ? (
          <p className="muted small">Nobody lives outside your station yet.</p>
        ) : (
          <NewLink draft={draft} onChange={setDraft} content={content} />
        )}
      </section>
    </>
  );
}
