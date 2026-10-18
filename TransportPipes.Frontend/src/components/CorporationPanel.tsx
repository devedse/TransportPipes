import { useEffect, useState, type FormEvent } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type {
  CorporationDto,
  CorporationMemberDto,
  CorporationRole,
  CorporationsDto,
  DistrictChoiceDto,
} from "../api/contracts";
import { setState, useAppStore } from "../state/store";

const COLORS = ["#2e86de", "#aa3355", "#2ec4a6", "#f6c744", "#8e44ad", "#e67e22", "#16a085", "#c0392b"];

const ROLE_NAMES: Record<CorporationRole, string> = {
  leader: "Leader",
  officer: "Officer",
  member: "Member",
};

function Tag({ tag, color }: { tag: string; color: string }) {
  return (
    <span className="corp-tag" style={{ background: color }}>
      {tag}
    </span>
  );
}

/** A corporation block (GDD §12.3): keep free estates of a district for the members for 72 hours */
function BlockReservation() {
  const [districts, setDistricts] = useState<DistrictChoiceDto[] | null>(null);
  const [choice, setChoice] = useState("");
  useEffect(() => {
    void gameClient.connection.getDistricts().then(setDistricts);
  }, []);
  const target = districts?.find((t) => `${t.x},${t.y}` === choice);
  return (
    <div className="block-reservation">
      <label>
        Corporation block: keep free estates for your members for 72 hours, so you can move in together
        <select value={choice} onChange={(e) => setChoice(e.target.value)}>
          <option value="">Choose a district…</option>
          {districts
            ?.filter((t) => t.freeEstates > 0)
            .map((t) => (
              <option key={`${t.x},${t.y}`} value={`${t.x},${t.y}`}>
                {t.name} · {t.freeEstates} free
              </option>
            ))}
        </select>
      </label>
      <button
        disabled={target === undefined}
        onClick={() => target && void gameClient.reserveBlock(target.x, target.y)}
      >
        Reserve the block
      </button>
    </div>
  );
}

/** Not in a corporation yet: join one of the district or found one */
function Directory({ data }: { data: CorporationsDto }) {
  const company = useAppStore((s) => s.company)!;
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow(1000);
  const [name, setName] = useState("");
  const [tag, setTag] = useState("");
  const [color, setColor] = useState(COLORS[0]!);
  const canFound = company.level >= data.foundLevel;
  const waitMs = data.canJoinAt === null ? 0 : new Date(data.canJoinAt).getTime() - (now + serverOffset);
  const submit = (event: FormEvent) => {
    event.preventDefault();
    void gameClient.foundCorporation(name, tag, color);
  };
  return (
    <>
      <p className="muted">
        A corporation is a team of up to {data.maxMembers} companies of your district. Members trade freely
        with each other, share a treasury, build an HQ in the hub with perks for everyone and chase a goal
        every week.
      </p>
      {waitMs > 0 && (
        <p className="warning-text">
          You left a corporation recently: you can join again in {formatDuration(waitMs / 1000)}.
        </p>
      )}
      <h3 className="section-title">Corporations in your district</h3>
      {data.district.length === 0 && <p className="muted">None yet. Be the first!</p>}
      <div className="corp-list">
        {data.district.map((c) => (
          <div key={c.id} className={c.invited ? "corp-row invited" : "corp-row"}>
            <Tag tag={c.tag} color={c.color} />
            <div className="corp-row-body">
              <strong>{c.name}</strong>
              <span className="muted small">
                {c.members}/{data.maxMembers} members · HQ level {c.hqLevel} ·{" "}
                {formatAmount(c.weeklyEarnings)} gold this week
              </span>
              {c.description !== "" && <span className="small">{c.description}</span>}
            </div>
            <div className="button-row">
              {c.invited ? (
                <>
                  <button className="primary" onClick={() => void gameClient.applyToCorporation(c.id)}>
                    Accept invite
                  </button>
                  <button onClick={() => void gameClient.declineCorporation(c.id)}>Decline</button>
                </>
              ) : c.applied ? (
                <span className="muted small">Applied, waiting for an officer</span>
              ) : (
                <button
                  disabled={waitMs > 0 || c.members >= data.maxMembers}
                  onClick={() => void gameClient.applyToCorporation(c.id)}
                >
                  Apply
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
      <h3 className="section-title">Found a corporation</h3>
      {canFound ? (
        <form className="corp-found" onSubmit={submit}>
          <label>
            Name
            <input
              value={name}
              maxLength={30}
              onChange={(e) => setName(e.target.value)}
              placeholder="Pipe Guild"
            />
          </label>
          <label>
            Tag
            <input
              value={tag}
              maxLength={5}
              onChange={(e) => setTag(e.target.value.toUpperCase())}
              placeholder="PG"
            />
          </label>
          <div className="swatches">
            {COLORS.map((c) => (
              <button
                type="button"
                key={c}
                className={c === color ? "swatch selected" : "swatch"}
                style={{ background: c }}
                onClick={() => setColor(c)}
                aria-label={c}
              />
            ))}
          </div>
          <button className="primary" disabled={name.trim().length < 3 || tag.length < 2}>
            Found for {formatAmount(data.foundCost)} gold
          </button>
        </form>
      ) : (
        <p className="muted">🔒 Founding a corporation needs Company Level {data.foundLevel}.</p>
      )}
    </>
  );
}

function MemberRow({
  member,
  corporation,
  meId,
}: {
  member: CorporationMemberDto;
  corporation: CorporationDto;
  meId: number;
}) {
  const leader = corporation.yourRole === "leader";
  const officer = leader || corporation.yourRole === "officer";
  const me = member.id === meId;
  return (
    <div className="corp-member">
      <span
        className={member.online ? "company-dot online" : "company-dot"}
        style={{ background: member.color }}
        title={member.online ? "Online now" : "Away"}
      />
      <button className="link" onClick={() => void gameClient.visitEstate(member.id, true)}>
        {member.name}
        {me && " (you)"}
      </button>
      <span className="muted small">Lv {member.level}</span>
      <span className={`role ${member.role}`}>{ROLE_NAMES[member.role]}</span>
      <span className="small" title="Earned this week">
        {formatAmount(member.weeklyEarnings)} g
      </span>
      <span className="small" title="Delivered to this week's goal">
        {member.contribution > 0 ? `🎯 ${formatAmount(member.contribution)}` : ""}
      </span>
      <span className="button-row">
        {leader && !me && member.role === "member" && (
          <button onClick={() => void gameClient.setCorporationRole(member.id, "officer")}>Promote</button>
        )}
        {leader && !me && member.role === "officer" && (
          <>
            <button onClick={() => void gameClient.setCorporationRole(member.id, "leader")}>
              Make leader
            </button>
            <button onClick={() => void gameClient.setCorporationRole(member.id, "member")}>Demote</button>
          </>
        )}
        {officer && !me && member.role !== "leader" && (member.role === "member" || leader) && (
          <button className="danger" onClick={() => void gameClient.kickFromCorporation(member.id)}>
            Kick
          </button>
        )}
      </span>
    </div>
  );
}

function Overview({ corporation }: { corporation: CorporationDto }) {
  const content = useAppStore((s) => s.content)!;
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow(1000);
  const [donation, setDonation] = useState(1000);
  const officer = corporation.yourRole !== "member";
  const goal = corporation.goal;
  return (
    <div className="corp-overview">
      {goal !== null && (
        <div className="corp-goal">
          <div className="panel-title">Weekly goal</div>
          <div className="goal-line">
            <span className="flow-dot" style={{ background: content.resourceColor(goal.resource) }} />
            <strong>
              Deliver {formatAmount(goal.amount)} {content.resourceName(goal.resource).toLowerCase()}
            </strong>
            <span className="muted small">
              {goal.rewarded
                ? "reached!"
                : `ends in ${formatDuration((new Date(goal.endsAt).getTime() - (now + serverOffset)) / 1000)}`}
            </span>
          </div>
          <div className="meter-bar">
            <div
              className="meter-fill"
              style={{
                width: `${Math.min(100, (goal.delivered / goal.amount) * 100)}%`,
                background: goal.rewarded ? "var(--gold)" : "var(--accent)",
              }}
            />
          </div>
          <div className="goal-line small">
            <span>
              {formatAmount(goal.delivered)} / {formatAmount(goal.amount)} · reward{" "}
              {formatAmount(goal.rewardGold)} gold for the treasury and XP for everyone who delivers
            </span>
          </div>
          {!goal.rewarded && (
            <div className="goal-line">
              <span className="muted small">
                Your storages: {formatAmount(goal.yourStock)} · you delivered{" "}
                {formatAmount(goal.yourContribution)}
              </span>
              <button
                className="primary"
                disabled={goal.yourStock < 1}
                onClick={() => void gameClient.deliverToCorporationGoal()}
              >
                Deliver from storage
              </button>
            </div>
          )}
        </div>
      )}
      <div className="corp-cards">
        <div className="corp-card">
          <div className="panel-title">Treasury</div>
          <div className="report-gold">
            <span className="gold-coin" />
            {formatAmount(corporation.treasury)}
          </div>
          <div className="muted small">Tax {Math.round(corporation.tax * 100)}% of members' trade income</div>
          <div className="donate">
            <input
              type="number"
              min={1}
              step={100}
              value={donation}
              onChange={(e) => setDonation(Number(e.target.value))}
            />
            <button disabled={donation <= 0} onClick={() => void gameClient.donateToCorporation(donation)}>
              Donate
            </button>
          </div>
        </div>
        <div className="corp-card">
          <div className="panel-title">HQ level {corporation.hqLevel}</div>
          <ul className="perks">
            {corporation.perks.map((perk) => (
              <li key={perk.level} className={perk.active ? "perk active" : "perk"}>
                <span className="perk-level">{perk.level}</span>
                {perk.text}
                {perk.level === 2 && corporation.perkResource !== null && (
                  <span className="muted">
                    {" "}
                    ({content.resourceName(corporation.perkResource).toLowerCase()})
                  </span>
                )}
              </li>
            ))}
          </ul>
          {corporation.nextHqCost !== null && (
            <button
              className="primary wide"
              disabled={!officer || corporation.treasury < corporation.nextHqCost}
              title={officer ? "Paid from the treasury" : "Officers upgrade the HQ"}
              onClick={() => void gameClient.upgradeCorporationHq()}
            >
              Upgrade to level {corporation.hqLevel + 1} · {formatAmount(corporation.nextHqCost)} gold
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

function Settings({ corporation }: { corporation: CorporationDto }) {
  const content = useAppStore((s) => s.content)!;
  const era = useAppStore((s) => s.era);
  const [tax, setTax] = useState(corporation.tax);
  const [perk, setPerk] = useState(corporation.perkResource ?? "");
  const [description, setDescription] = useState(corporation.description);
  const officer = corporation.yourRole !== "member";
  return (
    <div className="corp-settings">
      {officer ? (
        <>
          <label>
            Corporation tax: {Math.round(tax * 100)}% of members' trade income goes to the treasury
            <input
              type="range"
              min={0}
              max={0.1}
              step={0.01}
              value={tax}
              onChange={(e) => setTax(Number(e.target.value))}
            />
          </label>
          <label>
            Production perk (HQ level 2+)
            <select value={perk} onChange={(e) => setPerk(e.target.value)}>
              <option value="">Choose a resource…</option>
              {content.dto.resourceList
                .filter((r) => r.era <= era + 1)
                .map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.name}
                  </option>
                ))}
            </select>
          </label>
          <label>
            Description
            <textarea
              value={description}
              maxLength={300}
              rows={3}
              onChange={(e) => setDescription(e.target.value)}
            />
          </label>
          <button
            className="primary"
            onClick={() =>
              void gameClient.setCorporationSettings(tax, perk === "" ? null : perk, description)
            }
          >
            Save
          </button>
        </>
      ) : (
        <p className="muted">Officers change the tax, the production perk and the description.</p>
      )}
      {officer && corporation.members.length >= 5 && <BlockReservation />}
      <hr />
      <button className="danger" onClick={() => void gameClient.leaveCorporation()}>
        Leave {corporation.name}
      </button>
      <p className="muted small">After leaving you can join a corporation again after a day.</p>
    </div>
  );
}

function Mine({ corporation }: { corporation: CorporationDto }) {
  const company = useAppStore((s) => s.company)!;
  const [tab, setTab] = useState<"overview" | "members" | "applications" | "settings">("overview");
  const officer = corporation.yourRole !== "member";
  return (
    <>
      {corporation.description !== "" && <p className="muted">{corporation.description}</p>}
      <div className="tabs">
        <button className={tab === "overview" ? "tab active" : "tab"} onClick={() => setTab("overview")}>
          Overview
        </button>
        <button className={tab === "members" ? "tab active" : "tab"} onClick={() => setTab("members")}>
          Members ({corporation.members.length})
        </button>
        {officer && (
          <button
            className={tab === "applications" ? "tab active" : "tab"}
            onClick={() => setTab("applications")}
          >
            Applications
            {corporation.applications.length > 0 && (
              <span className="badge">{corporation.applications.length}</span>
            )}
          </button>
        )}
        <button className={tab === "settings" ? "tab active" : "tab"} onClick={() => setTab("settings")}>
          Settings
        </button>
        <button className="tab" onClick={() => void gameClient.openCorporationChat()}>
          💬 Chat
        </button>
      </div>
      {tab === "overview" && <Overview corporation={corporation} />}
      {tab === "members" && (
        <div className="corp-members">
          {corporation.members.map((m) => (
            <MemberRow key={m.id} member={m} corporation={corporation} meId={company.id} />
          ))}
        </div>
      )}
      {tab === "applications" && (
        <div className="corp-members">
          {corporation.applications.length === 0 && <p className="muted">No applications right now.</p>}
          {corporation.applications.map((m) => (
            <div key={m.id} className="corp-member">
              <span className="company-dot" style={{ background: m.color }} />
              <button className="link" onClick={() => void gameClient.visitEstate(m.id, true)}>
                {m.name}
              </button>
              <span className="muted small">Lv {m.level}</span>
              <span className="button-row">
                <button className="primary" onClick={() => void gameClient.inviteToCorporation(m.id)}>
                  Accept
                </button>
                <button onClick={() => void gameClient.declineApplication(m.id)}>Decline</button>
              </span>
            </div>
          ))}
        </div>
      )}
      {tab === "settings" && <Settings corporation={corporation} />}
    </>
  );
}

/** The corporation: directory and founding, or your corporation's goal, treasury, HQ, members and settings */
export function CorporationPanel() {
  const open = useAppStore((s) => s.corporationOpen);
  const data = useAppStore((s) => s.corporations);
  if (!open) {
    return null;
  }
  const close = () => setState({ corporationOpen: false });
  const mine = data?.mine ?? null;
  return (
    <div className="modal-backdrop" onClick={close}>
      <div
        className="panel corp-panel"
        role="dialog"
        aria-label="Corporation"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="panel-header">
          <h2>
            {mine !== null ? (
              <>
                <Tag tag={mine.tag} color={mine.color} /> {mine.name}
              </>
            ) : (
              "Corporations"
            )}
          </h2>
          {mine !== null && <span className="muted">{ROLE_NAMES[mine.yourRole]}</span>}
          <button className="icon-button" title="Close (Esc)" onClick={close}>
            ×
          </button>
        </div>
        {data === null ? (
          <p className="muted">Loading…</p>
        ) : mine !== null ? (
          <Mine key={mine.id} corporation={mine} />
        ) : (
          <Directory data={data} />
        )}
      </div>
    </div>
  );
}
