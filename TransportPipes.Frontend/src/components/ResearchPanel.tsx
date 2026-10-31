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

/** What Do it first does on a busy line */
const firstTitle = (now: string) =>
  `Starts this now. ${now} waits on hold: paid, its work so far kept, first in the queue, and goes on where it stopped right after.`;

/** A track (the first line): its tiers, the next tier's price and what it does, Research and Queue */
function TrackCard({ state, content }: { state: TrackStateDto; content: Content }) {
  const company = useAppStore((s) => s.company)!;
  const track = content.track(state.id);
  const lines = company.research;
  const current = lines.currentTrack === state.id;
  const nextMinutes = state.minutes;
  // Its bar by the tier being researched (the next tier's time made it run ahead or behind)
  const progress = useProgress(current ? lines.trackEndsAt : null, lines.trackMinutes ?? nextMinutes);
  const tiers = content.dto.trackTiers.length;
  const researching = content.dto.trackTiers[state.tiers];
  const name = track?.name ?? state.id;
  const held = current ? undefined : (lines.held?.[state.id] ?? undefined);
  const affordable = held !== undefined || company.gold >= state.total;
  const busy = lines.currentTrack !== null;
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
      {state.tiers >= tiers ? (
        <div className="tech-effect">✔ Mastered</div>
      ) : (
        <>
          {/* Being researched, the tier it is at (the server's next is the one after it) */}
          {current && researching !== undefined ? (
            <div className="tech-effect">
              Tier {researching.tier} now:{" "}
              {effectText(researching.effect, researching.amount, state.id === "power")}
              {researching.tier >= content.dto.hubOrderFrom &&
                " · Hub Orders ×" + content.dto.hubOrderPerTier}
            </div>
          ) : (
            <div className="tech-effect">
              Tier {state.next}:{" "}
              {state.effect !== null && effectText(state.effect, state.amount, state.id === "power")}
              {(state.next ?? 0) >= content.dto.hubOrderFrom &&
                " · Hub Orders ×" + content.dto.hubOrderPerTier}
            </div>
          )}
          {progress !== null ? (
            <Progress progress={progress} />
          ) : held !== undefined ? (
            <div className="tech-cost info-text">On hold: paid, {minutesText(held)} of work left</div>
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
              className={busy ? "" : "primary"}
              disabled={!affordable}
              title={busy ? firstTitle(content.track(lines.currentTrack!)?.name ?? "") : undefined}
              onClick={() => void gameClient.researchFirst(state.id)}
            >
              {!affordable
                ? `Need ${formatAmount(state.total - company.gold)} g more`
                : held !== undefined
                  ? busy
                    ? `Continue tier ${state.next} first`
                    : `Continue tier ${state.next}`
                  : busy
                    ? `Do tier ${state.next} first`
                    : `Research tier ${state.next}`}
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
  const content = useAppStore((s) => s.content)!;
  const lines = company.research;
  const status = state?.state ?? "locked";
  // How long it takes you, as the server works it out (a Lab level makes it faster: the content's time was shown)
  const minutes = state?.minutes ?? tech.minutes;
  const progress = useProgress(
    status === "researching" ? lines.researchEndsAt : null,
    lines.researchMinutes ?? minutes,
  );
  const cost = state?.cost ?? tech.gold;
  const held = status === "researching" ? undefined : (lines.held?.[tech.id] ?? undefined);
  const affordable = held !== undefined || company.gold >= cost;
  const busy = lines.currentResearch !== null;
  return (
    <div className={`tech ${status}`} title={tech.description} role="group" aria-label={tech.name}>
      <div className="tech-name">
        {status === "done" && "✔ "}
        {tech.name}
      </div>
      <div className="tech-effect">{tech.description}</div>
      {status === "done" ? null : progress !== null ? (
        <Progress progress={progress} />
      ) : held !== undefined ? (
        <div className="tech-cost info-text">On hold: paid, {minutesText(held)} of work left</div>
      ) : (
        <div className="tech-cost">
          <span>{formatAmount(cost)} g</span>
          <span className="muted"> · {formatDuration(minutes * 60)}</span>
          {tech.grant > 0 && <span className="muted"> · everyone gets it at Level {tech.grant}</span>}
        </div>
      )}
      {status === "available" && (
        <button
          className={busy ? "" : "primary"}
          disabled={!affordable}
          title={busy ? firstTitle(content.research(lines.currentResearch!)?.name ?? "") : undefined}
          onClick={() => void gameClient.researchFirst(tech.id)}
        >
          {!affordable
            ? `Need ${formatAmount(cost - company.gold)} g more`
            : held !== undefined
              ? busy
                ? "Continue first"
                : "Continue"
              : busy
                ? "Do it first"
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

/** Minutes as a time ("2h 10m") */
const minutesText = (minutes: number) => formatDuration(minutes * 60);

/**
 * A line now and next (the owner, 2026-10-31: "change your research queue in a way that makes sense ... stop existing
 * research without removing spend research on it"): what it researches, with Stop; its queue, with up, down and out;
 * and what waits on hold, paid, with Continue
 */
function LineBox({
  track,
  name,
  content,
}: {
  track: boolean;
  name: (id: string, index: number) => string;
  content: Content;
}) {
  const lines = useAppStore((s) => s.company)!.research;
  const current = track ? lines.currentTrack : lines.currentResearch;
  const queue = track ? lines.trackQueue : lines.researchQueue;
  const progress = useProgress(track ? lines.trackEndsAt : lines.researchEndsAt, 1);
  const held = Object.entries(lines.held ?? {}).filter(
    ([id]) => (content.track(id) !== undefined) === track && id !== current && !queue.includes(id),
  );
  // In the queue, the first place of something on hold goes on with it (a track's later places are its next tiers)
  const heldAt = (id: string, index: number) =>
    queue.indexOf(id) === index ? (lines.held?.[id] ?? undefined) : undefined;
  const label = track ? "Tracks line" : "Era tree line";
  return (
    <div className="research-line" role="group" aria-label={label}>
      <div className="line-row">
        <span className="line-label">Now</span>
        {current === null ? (
          <span className="muted">
            {queue.length > 0
              ? "Free: the first in the queue starts when its gold is there"
              : "Free: pick something below"}
          </span>
        ) : (
          <span className="line-item">
            <b>{name(current, -1)}</b>
            {progress !== null && <span className="muted"> · {formatDuration(progress.left)} left</span>}
            <button
              className="small"
              title="It waits on hold: paid, its work so far kept. Continue it any time, it costs nothing more."
              onClick={() => void gameClient.stopResearch(track)}
            >
              Stop
            </button>
          </span>
        )}
      </div>
      {queue.length > 0 && (
        <div className="line-row">
          <span className="line-label">Next</span>
          <ol className="line-queue">
            {queue.map((id, i) => (
              <li key={`${id}-${i}`}>
                <span>
                  {i + 1}. {name(id, i)}
                  {heldAt(id, i) !== undefined && (
                    <span className="info-text"> · on hold, paid: {minutesText(heldAt(id, i)!)} left</span>
                  )}
                </span>
                <span className="line-buttons">
                  <button
                    className="icon-button"
                    aria-label={`Move ${name(id, i)} up`}
                    title="Earlier"
                    disabled={i === 0}
                    onClick={() => void gameClient.moveQueuedResearch(track, i, -1)}
                  >
                    ↑
                  </button>
                  <button
                    className="icon-button"
                    aria-label={`Move ${name(id, i)} down`}
                    title="Later"
                    disabled={i === queue.length - 1}
                    onClick={() => void gameClient.moveQueuedResearch(track, i, 1)}
                  >
                    ↓
                  </button>
                  <button
                    className="icon-button"
                    aria-label={`Take ${name(id, i)} out of the queue`}
                    title="Out of the queue"
                    onClick={() => void gameClient.takeOutOfQueue(track, i)}
                  >
                    ×
                  </button>
                </span>
              </li>
            ))}
          </ol>
        </div>
      )}
      {held.length > 0 && (
        <div className="line-row">
          <span className="line-label">On hold</span>
          <ul className="line-queue">
            {held.map(([id, minutes]) => (
              <li key={id}>
                <span>
                  {name(id, -2)}{" "}
                  <span className="info-text">· paid, {minutesText(minutes)} of work left</span>
                </span>
                <span className="line-buttons">
                  <button
                    className="small"
                    title={
                      current === null
                        ? "It goes on where it stopped, for nothing"
                        : `It goes on now, for nothing; ${name(current, -1)} waits on hold, paid, and goes on right after`
                    }
                    onClick={() => void gameClient.researchFirst(id)}
                  >
                    {current === null ? "Continue" : "Continue first"}
                  </button>
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
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
  // Now (index −1) and on hold (−2) it is the tier after the last done
  const trackName = (id: string, index: number) => {
    const before = index < 0 ? 0 : lines.trackQueue.slice(0, index).filter((t) => t === id).length;
    const tier = (lines.tiers[id] ?? 0) + (index >= 0 && lines.currentTrack === id ? 1 : 0) + before + 1;
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

        <p className="muted small">
          Stop what a line researches when you want something else first: it waits on hold, paid, and goes on
          where it stopped, for nothing. <b>Do it first</b> on a card does both at once: what the line was at
          waits, first in the queue.
        </p>

        <h3>Tracks · deep research for one chain</h3>
        <p className="muted small">
          Nine tiers over about ten days: faster, leaner, complexes, and from tier {content.dto.hubOrderFrom}{" "}
          bigger Hub Orders for its goods. Tiers cost gold, and from tier 3 also the track&apos;s own good
          (from your Storages; what they lack is bought at 2.5×). Each Era&apos;s tracks cost four times the
          gold of the Era before.
        </p>
        <LineBox track name={trackName} content={content} />
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
        <LineBox track={false} name={(id) => content.research(id)?.name ?? id} content={content} />
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
