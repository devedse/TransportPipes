import { gameClient } from "../app/client";
import { formatAmount, formatDuration, formatRate } from "../app/format";
import { useNow } from "../app/useNow";
import { currentGold, setState, useAppStore } from "../state/store";

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
  const content = useAppStore((state) => state.content);
  const unread = useAppStore((state) => state.unread);
  const inboxOpen = useAppStore((state) => state.inboxOpen);
  const serverOffset = useAppStore((state) => state.serverOffsetMs);
  const serverNow = useNow(60000) + serverOffset;
  const gold = useLiveGold();
  if (company === null) {
    return null;
  }
  const levelSpan = company.nextLevelXp - company.levelXp;
  const levelProgress = levelSpan > 0 ? Math.min(1, (company.xp - company.levelXp) / levelSpan) : 1;

  return (
    <div className="top-bar">
      <div className="panel top-company">
        <span className="company-dot" style={{ background: company.color }} />
        <div>
          <div className="company-name">
            {company.name}
            {company.corporationTag !== null && (
              <button
                className="corp-tag small-tag"
                title="Your corporation (C)"
                onClick={() => void gameClient.openCorporation()}
              >
                {company.corporationTag}
              </button>
            )}
          </div>
          <div className="muted small">{user?.userName}</div>
        </div>
      </div>

      <div className="panel top-gold" title="Gold: sales to the hub and to neighbours, minus what you buy">
        <span className="gold-coin" />
        <span className="gold-amount">{formatAmount(gold)}</span>
        <span className={company.goldPerMinute >= 0 ? "gold-rate" : "gold-rate negative"}>
          {formatRate(company.goldPerMinute)}
        </span>
      </div>

      <div
        className="panel top-level"
        title={`${formatAmount(company.xp)} XP · next level at ${formatAmount(company.nextLevelXp)} XP`}
      >
        <div className="level-badge">{company.level}</div>
        <div className="level-info">
          <div className="small">Company Level</div>
          <div className="xp-bar">
            <div className="xp-fill" style={{ width: `${levelProgress * 100}%` }} />
          </div>
        </div>
      </div>

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

      {company.researchPerMinute > 0 || company.researchPoints > 0 || company.currentResearch !== null ? (
        <button
          className="panel top-research"
          title="Research (R)"
          onClick={() => void gameClient.openResearch()}
        >
          <span>🔬 {formatAmount(company.researchPoints)}</span>
          <span className="muted small">{formatRate(company.researchPerMinute)}</span>
          {company.currentResearch !== null && content !== null && (
            <span className="small current-research">
              → {content.research(company.currentResearch)?.name}
            </span>
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
          🔔
          {unread > 0 && <span className="dot-badge">{unread > 99 ? "99+" : unread}</span>}
        </button>
        <button
          className="icon-button"
          title="Leaderboards (L)"
          onClick={() => void gameClient.openLeaderboard()}
        >
          🏆
        </button>
        <button className="icon-button" title="Back to my estate (H)" onClick={() => gameClient.goHome()}>
          ⌂
        </button>
        <button className="icon-button" title="Zoom out (-)" onClick={() => gameClient.zoomBy(-1)}>
          −
        </button>
        <button className="icon-button" title="Zoom in (+)" onClick={() => gameClient.zoomBy(1)}>
          +
        </button>
        <button className="icon-button" title="Log out" onClick={() => void gameClient.logout()}>
          ⎋
        </button>
      </div>
    </div>
  );
}
