import { gameClient } from "../app/client";
import { formatAmount, formatRate } from "../app/format";
import { useNow } from "../app/useNow";
import { currentGold, useAppStore } from "../state/store";

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
          <div className="company-name">{company.name}</div>
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

      {company.researchPerMinute > 0 || company.researchPoints > 0 ? (
        <div className="panel top-research" title="Research points from your labs">
          🔬 {formatAmount(company.researchPoints)}{" "}
          <span className="muted small">{formatRate(company.researchPerMinute)}</span>
        </div>
      ) : null}

      <div className="top-spacer" />

      <div className="panel top-actions">
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
