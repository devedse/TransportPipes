import { useEffect, useRef, useState } from "react";
import { gameClient } from "../app/client";
import { isMuted, setMuted } from "../app/sound";
import { formatAmount, formatDuration, formatRate } from "../app/format";
import { useNow } from "../app/useNow";
import { currentGold, setState, useAppStore } from "../state/store";
import { GoldPanel } from "./GoldPanel";
import { HoverPopup } from "./HoverPopup";
import { WaitingGoods, YardClock } from "./TopWaits";
import { levelEta } from "../app/experience";
import { ExperiencePanel } from "./ExperiencePanel";
import { emblemIcon } from "../app/crews";
import { researchName, runningResearch } from "../app/research";

/**
 * A line of the Lab at work, in the top bar (the owner, 2026-10-31: "power should show a progress bar in the top"):
 * its name, the time left and how far along it is; in words for the popup
 */
function ResearchLine({
  name,
  endsAt,
  minutes,
  words = false,
}: {
  name: string;
  endsAt: string | null;
  minutes: number | null;
  words?: boolean;
}) {
  const now = useNow(1000);
  const serverOffset = useAppStore((state) => state.serverOffsetMs);
  const left = endsAt === null ? 0 : Math.max(0, (new Date(endsAt).getTime() - (now + serverOffset)) / 1000);
  const share = minutes !== null && minutes > 0 ? Math.max(0, Math.min(1, 1 - left / (minutes * 60))) : null;
  if (words) {
    return (
      <span>
        {name}: {formatDuration(left)} left{share !== null && ` · ${Math.floor(share * 100)}% done`}
      </span>
    );
  }
  return (
    <span className="top-research-line">
      <span className="small current-research">{name}</span>
      <span className="muted small">{formatDuration(left)}</span>
      {share !== null && (
        <span className="top-research-bar" aria-label={`${Math.floor(share * 100)}% done`}>
          <span style={{ width: `${share * 100}%` }} />
        </span>
      )}
    </span>
  );
}

/** Gold that counts up smoothly between server updates */
function useLiveGold(): number {
  const company = useAppStore((state) => state.company);
  const companyAt = useAppStore((state) => state.companyAt);
  const now = useNow(200);
  return currentGold({ company, companyAt }, now);
}

export function TopBar() {
  const company = useAppStore((state) => state.company);
  const user = useAppStore((state) => state.user);
  const keepNudge = useAppStore((state) => state.keepNudge);
  const content = useAppStore((state) => state.content);
  const unread = useAppStore((state) => state.unread);
  const chatUnread = useAppStore((state) => Object.values(state.chatUnread).reduce((sum, n) => sum + n, 0));
  const inboxOpen = useAppStore((state) => state.inboxOpen);
  const serverOffset = useAppStore((state) => state.serverOffsetMs);
  const serverNow = useNow(60000) + serverOffset;
  const gold = useLiveGold();
  const [xpOpen, setXpOpen] = useState(false);
  const [goldOpen, setGoldOpen] = useState(false);
  const bar = useRef<HTMLDivElement>(null);
  // Everything below the top bar starts under it: it grows with its contents (the XP line, a moving estate)
  useEffect(() => {
    const element = bar.current;
    if (element === null) {
      return;
    }
    const update = () => {
      const bottom = element.getBoundingClientRect().bottom;
      document.documentElement.style.setProperty("--hud-top", `${Math.round(bottom + 8)}px`);
    };
    update();
    const observer = new ResizeObserver(update);
    observer.observe(element);
    window.addEventListener("resize", update);
    return () => {
      observer.disconnect();
      window.removeEventListener("resize", update);
    };
  });
  // Escape closes the Experience panel, like every other panel (Cleo's run 2 afternoon: it stayed open)
  useEffect(() => {
    if (!xpOpen) {
      return;
    }
    const close = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setXpOpen(false);
      }
    };
    window.addEventListener("keydown", close);
    return () => window.removeEventListener("keydown", close);
  }, [xpOpen]);
  if (company === null) {
    return null;
  }
  // The bank in days of what the top bar earns (a sleeping or new company: none)
  const bankDays = company.goldPerMinute > 0.5 ? gold / (company.goldPerMinute * 1440) : 0;
  // The Company Level comes from the gold earned in all (1.4 §14)
  const levelSpan = company.nextLevelEarned - company.levelEarned;
  const levelProgress =
    levelSpan > 0 ? Math.min(1, (company.lifetimeEarnings - company.levelEarned) / levelSpan) : 1;
  const researching = content !== null ? researchName(company, content) : null;

  return (
    <div className="top-bar" ref={bar}>
      <HoverPopup
        popup={
          <>
            <strong className="tip-title">{company.name}</strong>
            <span className="tip-section">
              <span>Your company.</span>
              <span>Click (or press I) for your managers, achievements and statistics.</span>
            </span>
          </>
        }
      >
        <div className="panel top-company clickable" onClick={() => void gameClient.openCompany()}>
          <span className="company-dot" style={{ background: company.color }} />
          <div>
            <div className="company-name">{company.name}</div>
            <div className="muted small">
              {user?.isGuest === true ? (
                <button
                  className={keepNudge ? "link small keep-link nudge" : "link small keep-link"}
                  onClick={(event) => {
                    event.stopPropagation();
                    setState({ keepOpen: true, keepNudge: false });
                  }}
                >
                  Keep my plot
                </button>
              ) : (
                user?.userName
              )}
              {company.crew !== null && (
                <button
                  className="link small"
                  title="Your Yard's crew (K)"
                  onClick={(event) => {
                    event.stopPropagation();
                    void gameClient.openCrew();
                  }}
                >
                  {" "}
                  · {emblemIcon(company.crew.emblem)} {company.crew.name}
                </button>
              )}
            </div>
          </div>
        </div>
      </HoverPopup>

      {/* F9: the crew where people look, its own spot (it was clipped inside the company's name) */}
      {company.crew !== null && (
        <HoverPopup
          popup={
            <>
              <strong className="tip-title">Your crew: {company.crew.name}</strong>
              <span className="tip-section">
                <strong>
                  {company.crew.members} {company.crew.members === 1 ? "member" : "members"}
                </strong>
                <span>The companies on the plots around your Yard.</span>
              </span>
              <span className="tip-section">
                <strong>Together you</strong>
                <ul className="goods-tip-list">
                  <li>build your Yard&apos;s Monuments: each gives every member a lasting bonus;</li>
                  <li>share a Depot for goods.</li>
                </ul>
              </span>
              <span className="tip-section muted">
                <span>Click (or press K) for the crew.</span>
              </span>
            </>
          }
        >
          <button className="panel top-crew" onClick={() => void gameClient.openCrew()}>
            <span
              className="crew-banner-dot"
              style={{
                background: `linear-gradient(90deg, ${company.crew.color} 50%, ${company.crew.color2} 50%)`,
              }}
            >
              {emblemIcon(company.crew.emblem)}
            </span>
            <span className="top-crew-name">{company.crew.name}</span>
          </button>
        </HoverPopup>
      )}

      {/* Your gold in days of income, and what it can do (after run 4: the players banked 24–29 days and spent 0.2%) */}
      <div className="top-gold-wrap">
        <HoverPopup
          off={goldOpen}
          popup={
            <>
              <strong className="tip-title">Your gold: {formatAmount(gold)}</strong>
              <span className="tip-section">
                <strong>{formatRate(company.goldPerMinute)}</strong>
                <span>What you earn a minute now.</span>
                <span>
                  It is what you sell (at your Trading Post, to neighbours, over links) minus what you buy.
                </span>
              </span>
              {bankDays >= 1 && (
                <span className="tip-section">
                  <strong>{bankDays < 10 ? bankDays.toFixed(1) : bankDays.toFixed(0)} days</strong>
                  <span>Your gold is that many days of this income.</span>
                </span>
              )}
              <span className="tip-section">
                <strong>Why it moves</strong>
                <span>
                  The town&apos;s market looks again every minute, and the Yard trades at every round.
                </span>
              </span>
              <span className="tip-section muted">
                <span>Click: every line it is made of, and what your gold can do.</span>
              </span>
            </>
          }
        >
          <button
            className="panel top-gold"
            aria-expanded={goldOpen}
            onClick={() => setGoldOpen((open) => !open)}
          >
            <span className="gold-coin" />
            <span className="gold-amount">{formatAmount(gold)}</span>
            <span className={company.goldPerMinute >= 0 ? "gold-rate" : "gold-rate negative"}>
              {formatRate(company.goldPerMinute)}
            </span>
            {bankDays >= 1 && (
              <span
                className="gold-days"
                aria-label={`${bankDays.toFixed(0)} days of your income: what it can do`}
              >
                · {bankDays < 10 ? bankDays.toFixed(1) : bankDays.toFixed(0)} days ▸
                <span className="gold-dot" />
              </span>
            )}
          </button>
        </HoverPopup>
        {goldOpen && <GoldPanel days={bankDays} onClose={() => setGoldOpen(false)} />}
      </div>
      <WaitingGoods />
      <YardClock />

      <div className="top-level-wrap">
        <HoverPopup
          off={xpOpen}
          popup={
            <>
              <strong className="tip-title">Company Level {company.level}</strong>
              <span className="tip-section">
                <strong>Where it comes from</strong>
                <span>All the gold your company ever earned. Spending takes nothing away.</span>
              </span>
              <span className="tip-section">
                <strong>Level {company.level + 1}</strong>
                <span>
                  {formatAmount(company.lifetimeEarnings - company.levelEarned)} of {formatAmount(levelSpan)}{" "}
                  g earned.
                </span>
                {levelEta(company) !== null && (
                  <span>At your income now: in {formatDuration(levelEta(company)!)}.</span>
                )}
              </span>
              <span className="tip-section">
                <strong>What a level gives</strong>
                <span>New buildings, research and land to buy.</span>
              </span>
              <span className="tip-section muted">
                <span>Click: what each level opens.</span>
              </span>
            </>
          }
        >
          <button
            className="panel top-level"
            aria-expanded={xpOpen}
            onClick={() => setXpOpen((open) => !open)}
          >
            <div className="level-badge">{company.level}</div>
            <div className="level-info">
              <div className="level-line">
                <span className="small">Company Level</span>
                <span className="xp-numbers">
                  {formatAmount(company.lifetimeEarnings - company.levelEarned)} / {formatAmount(levelSpan)} g
                </span>
              </div>
              <div className="xp-bar">
                <div className="xp-fill" style={{ width: `${levelProgress * 100}%` }} />
              </div>
              <div className="xp-pace small">
                {levelEta(company) !== null
                  ? `Level ${company.level + 1} in ${formatDuration(levelEta(company)!)}`
                  : "earned in all"}
                <span className="xp-help">?</span>
              </div>
            </div>
          </button>
        </HoverPopup>
        {xpOpen && content !== null && (
          <>
            <div className="xp-backdrop" onClick={() => setXpOpen(false)} />
            <ExperiencePanel company={company} content={content} onClose={() => setXpOpen(false)} />
          </>
        )}
      </div>

      {company.moving.arrivesAt !== null && (
        <div className="panel top-era finale" title="Your plot is moving to its new town">
          <span className="era-number">🎈 Moving</span>
          <span className="muted small">
            lands in{" "}
            {formatDuration(Math.max(0, (new Date(company.moving.arrivesAt).getTime() - serverNow) / 1000))}
          </span>
        </div>
      )}
      <HoverPopup
        popup={
          <>
            <strong className="tip-title">Your town is in Era {company.era}</strong>
            <span className="tip-section">
              <strong>What an Era is</strong>
              <span>A stage of the game.</span>
              <span>Each Era brings new buildings, goods and pipes.</span>
            </span>
            <span className="tip-section">
              <strong>The next Era</strong>
              <span>It comes when the town&apos;s Era Wonder is built.</span>
              <span>
                Everyone in the town gives its stages the goods they want (Hub › Town &amp; Wonder).
              </span>
            </span>
            {company.yourEra > company.era && (
              <span className="tip-section">
                <strong>You: Era {company.yourEra}</strong>
                <span>Your research opened it, so you can build its buildings already.</span>
              </span>
            )}
            <span className="tip-section muted">
              <span>Click: the town, its Wonder and your Yard&apos;s Monuments.</span>
            </span>
          </>
        }
      >
        <button className="panel top-era" onClick={() => void gameClient.openHub("wonders")}>
          <span className="era-number">Era {company.era}</span>
          <span className="muted small">
            {company.yourEra > company.era ? `you: Era ${company.yourEra}` : "your town"}
          </span>
        </button>
      </HoverPopup>

      {company.research.hasLab && (
        <HoverPopup
          popup={
            <>
              <strong className="tip-title">Research{researching !== null ? `: ${researching}` : ""}</strong>
              <span className="tip-section">
                <strong>What it does</strong>
                <span>Your Lab works on two lines at once: a track and an Era tree item.</span>
                <span>A track makes one chain of buildings faster and cheaper to run.</span>
                <span>The Era tree opens the next Era&apos;s pipes and systems.</span>
              </span>
              {researching === null && company.research.open && (
                <span className="tip-section">
                  <strong>Now</strong>
                  <span>Nothing is being researched: pick one.</span>
                </span>
              )}
              {content !== null && researching !== null && (
                <span className="tip-section">
                  <strong>Now</strong>
                  {runningResearch(company, content).map((line) => (
                    <ResearchLine key={line.name} {...line} words />
                  ))}
                </span>
              )}
              <span className="tip-section muted">
                <span>Click (or press R) to see and pick.</span>
              </span>
            </>
          }
        >
          <button className="panel top-research" onClick={() => void gameClient.openResearch()}>
            <span>🔬</span>
            {researching !== null && content !== null ? (
              <span className="top-research-lines">
                {runningResearch(company, content).map((line) => (
                  <ResearchLine key={line.name} {...line} />
                ))}
              </span>
            ) : company.research.open ? (
              <span className="small current-research pick">pick one!</span>
            ) : (
              <span className="small current-research muted">nothing to research</span>
            )}
          </button>
        </HoverPopup>
      )}

      <div className="top-spacer" />

      <div className="panel top-actions">
        <button
          className="icon-button with-badge"
          title="Inbox"
          onClick={() => (inboxOpen ? setState({ inboxOpen: false }) : void gameClient.openInbox())}
        >
          <img className="ui-icon" src="/art/ui/ui-inbox.webp" alt="" />
          {unread > 0 && <span className="dot-badge">{unread > 99 ? "99+" : unread}</span>}
        </button>
        {user?.isAdmin && (
          <button
            className="icon-button"
            title="Moderation"
            onClick={() => setState({ moderationOpen: true })}
          >
            🛡
          </button>
        )}
        <button
          className="icon-button"
          title="Leaderboards (L)"
          onClick={() => void gameClient.openLeaderboard()}
        >
          <img className="ui-icon" src="/art/ui/ui-trophy.webp" alt="" />
        </button>
        {/* (on a phone the Company panel opens them: the top bar has no room) */}
        <button
          className="icon-button desktop-only"
          title="Statistics (J): your company over time"
          onClick={() => void gameClient.openStatistics()}
        >
          <img className="ui-icon" src="/art/ui/ui-stats.webp" alt="" />
        </button>
        <button className="icon-button" title="Back to my plot (H)" onClick={() => gameClient.goHome()}>
          <img className="ui-icon" src="/art/ui/ui-home.webp" alt="" />
        </button>
        <button
          className="icon-button desktop-only"
          title="Zoom out (-)"
          onClick={() => gameClient.zoomBy(-1)}
        >
          −
        </button>
        <button className="icon-button desktop-only" title="Zoom in (+)" onClick={() => gameClient.zoomBy(1)}>
          +
        </button>
        {/* On a phone the chat is up here, above every panel (after run 4: Ben's "close the panel first") */}
        <button
          className="icon-button with-badge phone-only"
          title="Chat"
          aria-label="Chat"
          onClick={() => void gameClient.toggleChat()}
        >
          <img className="ui-icon" src="/art/ui/ui-chat.webp" alt="" />
          {chatUnread > 0 && <span className="dot-badge">{chatUnread > 99 ? "99+" : chatUnread}</span>}
        </button>
        <button
          className="icon-button desktop-only"
          title="Photo mode (F)"
          onClick={() => gameClient.setPhotoMode(true)}
        >
          📷
        </button>
        <MuteButton />
        {/* On a phone Log out is in the company's panel (the bar had no room for Chat) */}
        <button className="icon-button desktop-only" title="Log out" onClick={() => void gameClient.logout()}>
          ⎋
        </button>
      </div>
    </div>
  );
}

/** Sounds on or off (remembered in this browser) */
function MuteButton() {
  const [muted, setMutedState] = useState(isMuted);
  return (
    <button
      className="icon-button"
      title={muted ? "Sounds off (click to turn on)" : "Sounds on"}
      onClick={() => {
        setMuted(!muted);
        setMutedState(!muted);
      }}
    >
      {muted ? "🔇" : "🔊"}
    </button>
  );
}
