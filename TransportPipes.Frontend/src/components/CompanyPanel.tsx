import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import type { AchievementDto, ManagerDto } from "../api/contracts";
import { setState, useAppStore } from "../state/store";
import { MoveTab } from "./MoveTab";
import { ResourceIcon } from "./ResourceIcon";

function initials(name: string): string {
  return name
    .split(" ")
    .map((t) => t[0])
    .join("")
    .slice(0, 2);
}

function ManagerCard({ manager, freeSlot }: { manager: ManagerDto; freeSlot: boolean }) {
  const gold = useAppStore((s) => s.company?.gold ?? 0);
  return (
    <div className={manager.hired ? "manager-card hired" : "manager-card"}>
      <div className="manager-portrait" style={{ background: manager.color }}>
        <img
          src={`/art/ui/manager-${manager.id}.webp`}
          alt=""
          onError={(e) => {
            e.currentTarget.style.display = "none";
          }}
        />
        <span>{initials(manager.name)}</span>
      </div>
      <div className="manager-body">
        <strong>{manager.name}</strong>
        <span className="small">⚙ {manager.automation}</span>
        <span className="small bonus">★ {manager.bonus}</span>
      </div>
      {manager.hired ? (
        <button onClick={() => void gameClient.dismissManager(manager.id)}>Dismiss</button>
      ) : (
        <button
          className="primary"
          disabled={!freeSlot || gold < manager.cost}
          title={freeSlot ? undefined : "No free slot"}
          onClick={() => void gameClient.hireManager(manager.id)}
        >
          Hire · {formatAmount(manager.cost)} g
        </button>
      )}
    </div>
  );
}

function Achievement({ achievement }: { achievement: AchievementDto }) {
  const progress = Math.min(1, achievement.value / achievement.amount);
  const rewards = [
    // It pays hours of your income, at least its gold
    achievement.hours > 0
      ? `${achievement.hours} h of income (at least ${formatAmount(achievement.gold)} g)`
      : achievement.gold > 0
        ? `${formatAmount(achievement.gold)} g`
        : null,
    achievement.title !== null ? `“${achievement.title}”` : null,
  ].filter((t) => t !== null);
  return (
    <div className={achievement.done ? "achievement done" : "achievement"} title={achievement.description}>
      <div className="achievement-medal">{achievement.done ? "🏅" : "○"}</div>
      <div className="achievement-body">
        <strong>{achievement.name}</strong>
        <span className="small muted">{achievement.description}</span>
        {!achievement.done && (
          <div className="meter-bar">
            <div className="meter-fill" style={{ width: `${progress * 100}%`, background: "var(--gold)" }} />
          </div>
        )}
        <span className="small">{rewards.join(" · ")}</span>
      </div>
    </div>
  );
}

/** Your company: statistics and titles, managers, achievements and collections (GDD §8.4, §8.5) */
export function CompanyPanel() {
  const open = useAppStore((s) => s.companyOpen);
  const details = useAppStore((s) => s.companyDetails);
  const company = useAppStore((s) => s.company);
  const content = useAppStore((s) => s.content);
  const user = useAppStore((s) => s.user);
  const [tab, setTab] = useState<"overview" | "managers" | "achievements" | "collections" | "move">(
    "overview",
  );
  if (!open || company === null || content === null) {
    return null;
  }
  const close = () => setState({ companyOpen: false });
  const hired = details?.managers.filter((t) => t.hired).length ?? 0;
  const done = details?.achievements.filter((t) => t.done).length ?? 0;
  return (
    <div className="modal-backdrop" onClick={close}>
      <div
        className="panel company-panel"
        role="dialog"
        aria-label="Company"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="panel-header">
          <h2>
            <span className="company-dot" style={{ background: company.color }} /> {company.name}
          </h2>
          <span className="muted">Company Level {company.level}</span>
          <button className="small phone-only" onClick={() => void gameClient.logout()}>
            Log out
          </button>
          <button className="icon-button" title="Close (Esc)" onClick={close}>
            ×
          </button>
        </div>
        <div className="tabs">
          <button className={tab === "overview" ? "tab active" : "tab"} onClick={() => setTab("overview")}>
            Overview
          </button>
          <button className={tab === "managers" ? "tab active" : "tab"} onClick={() => setTab("managers")}>
            Managers {details !== null && `${hired}/${details.managerSlots}`}
          </button>
          <button
            className={tab === "achievements" ? "tab active" : "tab"}
            onClick={() => setTab("achievements")}
          >
            Achievements {details !== null && `${done}/${details.achievements.length}`}
          </button>
          <button
            className={tab === "collections" ? "tab active" : "tab"}
            onClick={() => setTab("collections")}
          >
            Collections
          </button>
          <button className={tab === "move" ? "tab active" : "tab"} onClick={() => setTab("move")}>
            Move
            {company.moving.crates.length > 0 && (
              <span className="badge">{company.moving.crates.length}</span>
            )}
          </button>
        </div>
        {tab === "move" ? (
          <MoveTab />
        ) : details === null ? (
          <p className="muted">Loading…</p>
        ) : tab === "overview" ? (
          <div className="company-overview">
            {user?.isGuest === true && (
              <div className="keep-note">
                <span>You play as a guest: only this browser knows your plot.</span>
                <button className="primary" onClick={() => setState({ keepOpen: true })}>
                  Keep my plot
                </button>
              </div>
            )}
            <div className="stat-grid">
              {details.stats.map((stat) => (
                <div key={stat.name} className="stat">
                  <strong>{formatAmount(stat.value)}</strong>
                  <span className="muted small">{stat.name}</span>
                </div>
              ))}
            </div>
            {details.produced.length > 0 && (
              <>
                <div className="panel-title">Made so far</div>
                <div className="chips">
                  {details.produced.map((t) => (
                    <span
                      key={t.name}
                      className="res-chip"
                      style={{ borderColor: content.resourceColor(t.name) }}
                    >
                      <ResourceIcon resource={t.name} /> {formatAmount(t.value)}{" "}
                      {content.resourceName(t.name).toLowerCase()}
                    </span>
                  ))}
                </div>
              </>
            )}
            <div className="panel-title">Titles</div>
            {details.titles.length === 0 ? (
              <p className="muted small">Achievements, collections and Wonders give titles.</p>
            ) : (
              <div className="chips">
                {details.titles.map((t) => (
                  <span key={t} className="title-chip">
                    {t}
                  </span>
                ))}
              </div>
            )}
          </div>
        ) : tab === "managers" ? (
          <div className="managers">
            <p className="muted small">
              Managers work while you are away: each runs one task and gives one bonus. You have{" "}
              {details.managerSlots} {details.managerSlots === 1 ? "slot" : "slots"}: more with Company Levels
              10, 20, 30, 45 and 60, an Engineer&apos;s Workshop, the Automation tech and a corporation HQ of
              level 4.
            </p>
            {details.managers.map((manager) => (
              <ManagerCard key={manager.id} manager={manager} freeSlot={hired < details.managerSlots} />
            ))}
          </div>
        ) : tab === "achievements" ? (
          <div className="achievements">
            {[...details.achievements]
              .sort((a, b) => Number(a.done) - Number(b.done) || b.value / b.amount - a.value / a.amount)
              .map((achievement) => (
                <Achievement key={achievement.id} achievement={achievement} />
              ))}
          </div>
        ) : (
          <div className="collections">
            <div className="collection relics">
              <div className="collection-head">
                <strong>Relics</strong>
                <span className="small muted">
                  {company.expedition.relics.length}/{content.dto.relics.length} · expeditions find them
                </span>
              </div>
              <ul className="facts">
                {content.dto.relics.map((relic) =>
                  company.expedition.relics.includes(relic.id) ? (
                    <li key={relic.id}>
                      <strong>{relic.name}</strong>: <span className="muted">{relic.lore}</span>
                    </li>
                  ) : (
                    <li key={relic.id} className="muted">
                      ??? (not found yet)
                    </li>
                  ),
                )}
              </ul>
            </div>
            {details.collections.map((collection) => (
              <div key={collection.id} className={collection.finished ? "collection done" : "collection"}>
                <div className="collection-head">
                  <strong>{collection.name}</strong>
                  <span className="small muted">
                    {collection.done.length}/{collection.buildings.length} at level {collection.level}
                  </span>
                </div>
                <span className="small">{collection.description}</span>
                <div className="collection-items">
                  {collection.buildings.map((b) => (
                    <span
                      key={b}
                      className={collection.done.includes(b) ? "collection-item done" : "collection-item"}
                    >
                      {collection.done.includes(b) ? "✔ " : ""}
                      {content.building(b)?.name ?? b}
                    </span>
                  ))}
                </div>
                {collection.finished && collection.reward !== null && (
                  <span className="small gold-text">
                    Unlocked: {content.building(collection.reward)?.name}
                  </span>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
