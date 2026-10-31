import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { ResearchDefinition, ResearchStateDto, TierEffect, TrackStateDto } from "../api/contracts";
import type { Content } from "../game/content";
import { setState, useAppStore } from "../state/store";

const QUEUE_SIZE = 5;

/** What a tier does, in words ("everything +50%") */
function effectText(effect: TierEffect, amount: number, power: boolean): string {
  switch (effect) {
    case "faster":
      return `works ${Math.round((amount - 1) * 100 * (power ? 0.5 : 1))}% faster`;
    case "leaner":
      return power
        ? `wind, sun and water make more`
        : `takes ${Math.round((1 - amount) * 100)}% less of every input`;
    case "complexes":
      return "four in a square merge into a complex with the output of five";
    case "megaComplexes":
      return "nine in a square give the output of twelve";
    case "master":
      return "master: twice as much, and golden trim";
  }
}

/** How long is left of something that ends at a server time, and how far along it is */
function useProgress(endsAt: string | null, minutes: number): { left: number; share: number } | null {
  const now = useNow();
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  if (endsAt === null) {
    return null;
  }
  const left = Math.max(0, (new Date(endsAt).getTime() - (now + serverOffset)) / 1000);
  return { left, share: minutes > 0 ? Math.max(0, Math.min(1, 1 - left / (minutes * 60))) : 1 };
}

function Progress({ progress }: { progress: { left: number; share: number } }) {
  return (
    <div className="tech-progress">
      <div className="meter-bar">
        <div
          className="meter-fill"
          style={{ width: `${progress.share * 100}%`, background: "var(--accent)" }}
        />
      </div>
      <span className="small">{formatDuration(progress.left)} left</span>
    </div>
  );
}

/** Puts something in a line's queue or takes it out (five, in order; each starts when the line is free) */
function QueueButton({ id, name, queue }: { id: string; name: string; queue: string[] }) {
  const position = queue.indexOf(id);
  const queued = position >= 0;
  return (
    <button
      className="link small"
      disabled={!queued && queue.length >= QUEUE_SIZE}
      aria-label={queued ? `Take ${name} out of the queue` : `Queue ${name}`}
      onClick={() => void gameClient.queueResearch(id, !queued, name)}
    >
      {queued
        ? `#${position + 1} in the queue · take out`
        : queue.length >= QUEUE_SIZE
          ? "the queue is full"
          : "Queue it"}
    </button>
  );
}

/** A track (the first line): its tiers, the next tier's price and what it does, Research and Queue */
function TrackCard({ state, content }: { state: TrackStateDto; content: Content }) {
  const company = useAppStore((s) => s.company)!;
  const track = content.track(state.id);
  const lines = company.research;
  const current = lines.currentTrack === state.id;
  const nextMinutes = state.minutes;
  const progress = useProgress(current ? lines.trackEndsAt : null, nextMinutes);
  const tiers = content.dto.trackTiers.length;
  const name = track?.name ?? state.id;
  const affordable = company.gold >= state.total;
  return (
    <div
      className={`tech track ${state.next === null ? "done" : current ? "researching" : state.reason === null ? "available" : "locked"}`}
      title={track?.description}
      role="group"
      aria-label={name}
    >
      <div className="tech-name">
        {name}
        <span className="track-pips" aria-label={`tier ${state.tiers} of ${tiers}`}>
          {Array.from({ length: tiers }, (_, i) => (
            <span
              key={i}
              className={i < state.tiers ? "pip done" : current && i === state.tiers ? "pip now" : "pip"}
            />
          ))}
        </span>
      </div>
      {track !== undefined && <div className="tech-effect muted">{track.description}</div>}
      {state.next === null ? (
        <div className="tech-effect">✔ Mastered</div>
      ) : (
        <>
          <div className="tech-effect">
            Tier {state.next}:{" "}
            {state.effect !== null && effectText(state.effect, state.amount, state.id === "power")}
            {state.next >= content.dto.hubOrderFrom && " · Hub Orders ×" + content.dto.hubOrderPerTier}
          </div>
          {progress !== null ? (
            <Progress progress={progress} />
          ) : (
            <div className="tech-cost">
              <span>{formatAmount(state.gold)} g</span>
              {state.good !== null && state.units > 0 && (
                <span>
                  {" "}
                  + {formatAmount(state.units)} {content.resourceName(state.good).toLowerCase()}
                  <span className="muted">
                    {state.bought > 0
                      ? ` (${formatAmount(state.fromStorages)} from your Storages, the rest bought: ${formatAmount(state.total)} g in all)`
                      : " from your Storages"}
                  </span>
                </span>
              )}
              <span className="muted"> · {formatDuration(nextMinutes * 60)}</span>
              {state.gain > 0.05 && <span className="gold-text"> · +{formatAmount(state.gain)} g/min</span>}
            </div>
          )}
          {state.hubOrderFactor > 1.001 && (
            <div className="small muted">
              Your Hub Orders for its goods: ×{state.hubOrderFactor.toFixed(1)}
            </div>
          )}
          {!current && state.reason === null && (
            <button
              className="primary"
              disabled={!affordable || lines.currentTrack !== null}
              onClick={() => void gameClient.startResearch(state.id)}
            >
              {lines.currentTrack !== null
                ? "The line is busy: queue it"
                : affordable
                  ? `Research tier ${state.next}`
                  : `Need ${formatAmount(state.total - company.gold)} g more`}
            </button>
          )}
          {state.reason !== null && <div className="lock small">🔒 {state.reason}</div>}
          {state.reason === null && (
            <QueueButton id={state.id} name={`${name} ${state.next}`} queue={lines.trackQueue} />
          )}
        </>
      )}
    </div>
  );
}

/** An Era tree item (the second line): what it opens, its price, Research and Queue */
function TreeCard({ tech, state }: { tech: ResearchDefinition; state: ResearchStateDto | undefined }) {
  const company = useAppStore((s) => s.company)!;
  const lines = company.research;
  const status = state?.state ?? "locked";
  // How long it takes you, as the server works it out (a Lab level makes it faster: the content's time was shown)
  const minutes = state?.minutes ?? tech.minutes;
  const progress = useProgress(status === "researching" ? lines.researchEndsAt : null, minutes);
  const cost = state?.cost ?? tech.gold;
  return (
    <div className={`tech ${status}`} title={tech.description} role="group" aria-label={tech.name}>
      <div className="tech-name">
        {status === "done" && "✔ "}
        {tech.name}
      </div>
      <div className="tech-effect">{tech.description}</div>
      {status === "done" ? null : progress !== null ? (
        <Progress progress={progress} />
      ) : (
        <div className="tech-cost">
          <span>{formatAmount(cost)} g</span>
          <span className="muted"> · {formatDuration(minutes * 60)}</span>
          {tech.grant > 0 && <span className="muted"> · everyone gets it at Level {tech.grant}</span>}
        </div>
      )}
      {status === "available" && (
        <button
          className="primary"
          disabled={company.gold < cost || lines.currentResearch !== null}
          onClick={() => void gameClient.startResearch(tech.id)}
        >
          {lines.currentResearch !== null
            ? "The line is busy: queue it"
            : company.gold < cost
              ? `Need ${formatAmount(cost - company.gold)} g more`
              : "Research"}
        </button>
      )}
      {status === "locked" && state?.reason && <div className="lock small">🔒 {state.reason}</div>}
      {status !== "done" && status !== "researching" && !(state?.reason ?? "").includes("Era") && (
        <QueueButton id={tech.id} name={tech.name} queue={lines.researchQueue} />
      )}
    </div>
  );
}

/** The queue of a line, in words */
function QueueLine({ queue, name }: { queue: string[]; name: (id: string, index: number) => string }) {
  if (queue.length === 0) {
    return null;
  }
  return (
    <p className="small research-queue">
      Next, by themselves:{" "}
      {queue.map((t, i) => (
        <span key={`${t}-${i}`}>
          {i > 0 && " → "}
          {name(t, i)}
        </span>
      ))}
    </p>
  );
}

/** The Lab (1.4 §12): two lines at once, the tracks and the Era tree, each with a queue of five */
export function ResearchPanel() {
  const open = useAppStore((s) => s.researchOpen);
  const content = useAppStore((s) => s.content);
  const company = useAppStore((s) => s.company);
  const states = useAppStore((s) => s.research);
  const tracks = useAppStore((s) => s.tracks);
  if (!open || content === null || company === null) {
    return null;
  }
  const lines = company.research;
  const byId = new Map(states.map((t) => [t.id, t]));
  const close = () => setState({ researchOpen: false });
  // A track appears in its queue once for every tier: count them to name each "Iron 4", "Iron 5"
  const trackName = (id: string, index: number) => {
    const before = lines.trackQueue.slice(0, index).filter((t) => t === id).length;
    const tier = (lines.tiers[id] ?? 0) + (lines.currentTrack === id ? 1 : 0) + before + 1;
    return `${content.track(id)?.name ?? id} ${tier}`;
  };
  const eras = [...new Set(content.tracks.map((t) => t.era))].sort((a, b) => a - b);
  return (
    <div className="modal-backdrop" onClick={close}>
      <div
        className="panel research-panel"
        role="dialog"
        aria-label="Research"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="panel-header">
          <h2>Research</h2>
          <span className="muted small">Your Lab works on two lines at once: a track, and the Era tree.</span>
          <button className="icon-button" title="Close (Esc)" onClick={close}>
            ×
          </button>
        </div>
        {!lines.hasLab && <p className="status idle">Build a Lab to research.</p>}
        {/* Research that keeps going while you're away (run 2: points piled up while the queue sat empty) */}
        <label className="small keep-researching">
          <input
            type="checkbox"
            checked={lines.keepResearching}
            onChange={(event) => void gameClient.setKeepResearching(event.target.checked)}
          />{" "}
          Keep researching: when a line&apos;s queue is empty, the cheapest next step your gold covers starts
          by itself
        </label>

        <h3>Tracks · deep research for one chain</h3>
        <p className="muted small">
          Nine tiers over about ten days: faster, leaner, complexes, and from tier {content.dto.hubOrderFrom}{" "}
          bigger Hub Orders for its goods. Tiers cost gold, and from tier 3 also the track&apos;s own good
          (from your Storages; what they lack is bought at 2.5×). Each Era&apos;s tracks cost four times the
          gold of the Era before.
        </p>
        <QueueLine queue={lines.trackQueue} name={trackName} />
        {eras.map((era) => (
          <div key={era} className="research-era">
            <div className="era-head">Era {era}</div>
            <div className="research-row">
              {tracks
                .filter((t) => content.track(t.id)?.era === era)
                .map((state) => (
                  <TrackCard key={state.id} state={state} content={content} />
                ))}
            </div>
          </div>
        ))}

        <h3>Era tree · the pipes and buildings of each Era</h3>
        <p className="muted small">Short research: pipes, buildings and systems.</p>
        <QueueLine queue={lines.researchQueue} name={(id) => content.research(id)?.name ?? id} />
        {[1, 2, 3, 4, 5].map((era) => (
          <div key={era} className="research-era">
            <div className="era-head">Era {era}</div>
            <div className="research-row">
              {content.researchList
                .filter((t) => t.era === era)
                .map((tech) => (
                  <TreeCard key={tech.id} tech={tech} state={byId.get(tech.id)} />
                ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
