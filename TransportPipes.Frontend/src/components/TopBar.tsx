import { useEffect, useRef, useState } from "react";
import { gameClient } from "../app/client";
import { isMuted, setMuted } from "../app/sound";
import { formatAmount, formatDuration, formatRate } from "../app/format";
import { useNow } from "../app/useNow";
import { currentGold, setState, useAppStore } from "../state/store";
import { GoldPanel } from "./GoldPanel";
import { levelEta } from "../app/experience";
import { ExperiencePanel } from "./ExperiencePanel";
import { emblemIcon } from "../app/crews";
import { researchName } from "../app/research";

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
  const levelSpan = company.nextLevelXp - company.levelXp;
  const levelProgress = levelSpan > 0 ? Math.min(1, (company.xp - company.levelXp) / levelSpan) : 1;

  return (
    <div className="top-bar" ref={bar}>
      <div
        className="panel top-company clickable"
        title="Your company: managers, achievements and statistics (I)"
        onClick={() => void gameClient.openCompany()}
      >
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

      {/* F9: the crew where people look, its own spot (it was clipped inside the company's name) */}
      {company.crew !== null && (
        <button
          className="panel top-crew"
          title={`Your crew: ${company.crew.name} · ${company.crew.members} ${company.crew.members === 1 ? "member" : "members"}`}
          onClick={() => void gameClient.openCrew()}
        >
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
      )}

      {/* Your gold in days of income, and what it can do (after run 4: the players banked 24–29 days and spent 0.2%) */}
      <div className="top-gold-wrap">
        <button
          className="panel top-gold"
          aria-expanded={goldOpen}
          title="Gold: sales to the hub and to neighbours, minus what you buy. Tap: what your gold can do"
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
        {goldOpen && <GoldPanel days={bankDays} onClose={() => setGoldOpen(false)} />}
      </div>

      <div className="top-level-wrap">
        <button
          className="panel top-level"
          aria-expanded={xpOpen}
          title="Experience: where it comes from"
          onClick={() => setXpOpen((open) => !open)}
        >
          <div className="level-badge">{company.level}</div>
          <div className="level-info">
            <div className="level-line">
              <span className="small">Company Level</span>
              <span className="xp-numbers">
                {formatAmount(company.xp - company.levelXp)} / {formatAmount(levelSpan)} XP
              </span>
            </div>
            <div className="xp-bar">
              <div className="xp-fill" style={{ width: `${levelProgress * 100}%` }} />
            </div>
            <div className="xp-pace small">
              +{formatAmount(company.experience.perMinute)} XP/min
              {levelEta(company) !== null && (
                <>
                  {" "}
                  · Level {company.level + 1} in {formatDuration(levelEta(company)!)}
                </>
              )}
              <span className="xp-help">?</span>
            </div>
          </div>
        </button>
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
      {company.season.endedAt !== null ? (
        <button
          className="panel top-era finale"
          title="The season is over"
          onClick={() => void gameClient.openSeason()}
        >
          <span className="era-number">Season {company.season.season}</span>
          <span className="muted small">over · results</span>
        </button>
      ) : company.season.finaleAt !== null ? (
        <button
          className="panel top-era finale"
          title="The Space Elevator launches: the season ends"
          onClick={() => void gameClient.openHub("wonders")}
        >
          <span className="era-number">🚀 Launch</span>
          <span className="muted small">
            in {formatDuration(Math.max(0, (new Date(company.season.finaleAt).getTime() - serverNow) / 1000))}
          </span>
        </button>
      ) : (
        <button
          className="panel top-era"
          title="Wonders: the Era ends when the Era Wonder is finished, at the latest on its deadline"
          onClick={() => void gameClient.openHub("wonders")}
        >
          <span className="era-number">Era {company.era}</span>
          <span className="muted small">
            {formatDuration(Math.max(0, (new Date(company.eraDeadline).getTime() - serverNow) / 1000))} left
          </span>
        </button>
      )}

      {company.researchPerMinute > 0 || company.researchPoints > 0 || company.currentResearch !== null ? (
        <button
          className="panel top-research"
          title="Research (R)"
          onClick={() => void gameClient.openResearch()}
        >
          <span>🔬 {formatAmount(company.researchPoints)}</span>
          <span className="muted small">{formatRate(company.researchPerMinute)}</span>
          {company.currentResearch !== null && content !== null ? (
            <span className="small current-research">→ {researchName(company, content)}</span>
          ) : (
            company.researchPerMinute > 0 &&
            (company.researchOpen ? (
              <span className="small current-research pick">pick one!</span>
            ) : (
              <span className="small current-research muted">nothing left this Era</span>
            ))
          )}
        </button>
      ) : null}

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
        <button className="icon-button" title="Log out" onClick={() => void gameClient.logout()}>
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
