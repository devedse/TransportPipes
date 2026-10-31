import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import type { BuildingFlowDto } from "../api/contracts";
import { useAppStore } from "../state/store";
import { HoverPopup } from "./HoverPopup";
import { ResourceIcon } from "./ResourceIcon";

/** What a Storage keeps of each good: in effect, as its owner set it, what the company needs of it */
interface Keep {
  keeps: Record<string, number>;
  set?: Record<string, number> | null;
  needs?: Record<string, number> | null;
  manual?: boolean;
}

/** A slider's steps: about a hundred over the room, in round numbers */
function step(room: number): number {
  const raw = room / 100;
  const magnitude = 10 ** Math.floor(Math.log10(Math.max(1, raw)));
  return [1, 2, 5, 10].map((t) => t * magnitude).find((t) => t >= raw) ?? magnitude * 10;
}

/**
 * A Storage's sliders (the owner, 2026-10-31: "drag sliders per resource. At the bottom a total slider should show the
 * total. If you specify more than the total that would fit, it goes red and you can't save it"): for every good it
 * holds, takes in or has a slider for, how much it keeps; it always keeps at least what your next buildings, levels and
 * orders need (the light mark), so a slider at 0 means "only what's needed". What it holds above that goes on to your
 * Trading Post. Nothing changes until Save; the sliders together can't be more than its room.
 *
 * Shown first as a line a good, how full it is (the owner, 2026-10-31: "since the new storage update there's no bars
 * anymore that show how full the resource is per resource ... as compact as possible. It would be nice to not need a
 * whole screen"): a bar of what it holds against what it keeps; Set what it keeps opens the sliders.
 */
export function StorageSliders({
  building,
  room,
  contents,
  keep,
  flows,
}: {
  building: number;
  room: number;
  contents: Record<string, number>;
  keep: Keep;
  flows: BuildingFlowDto[];
}) {
  const content = useAppStore((s) => s.content)!;
  const era = useAppStore((s) => s.era);
  const saved = keep.set ?? {};
  const needs = keep.needs ?? {};
  const [draft, setDraft] = useState<Record<string, number> | null>(null);
  const [added, setAdded] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [editing, setEditing] = useState(false);
  const shown = draft ?? saved;
  const coming: Record<string, number> = {};
  for (const flow of flows.filter((t) => t.direction === "in")) {
    coming[flow.resource] = (coming[flow.resource] ?? 0) + flow.amount;
  }
  // What it lets go to be sold now, a minute (the audit, 2026-10-31: "about a tenth of it a minute", never the number)
  const selling: Record<string, number> = {};
  for (const flow of flows.filter((t) => t.direction === "out" && t.kind === "sale")) {
    selling[flow.resource] = (selling[flow.resource] ?? 0) + flow.amount;
  }
  const listed = [
    ...new Set([
      ...Object.keys(contents),
      ...Object.keys(keep.keeps),
      ...Object.keys(shown).filter((t) => (shown[t] ?? 0) > 0),
      ...Object.keys(coming),
      ...added,
    ]),
  ].sort((a, b) => content.resourceName(a).localeCompare(content.resourceName(b)));
  const others = content.resources.filter((t) => t.era <= era && !listed.includes(t.id));
  const total = listed.reduce((sum, t) => sum + (shown[t] ?? 0), 0);
  const withNeeds = listed.reduce((sum, t) => sum + Math.max(shown[t] ?? 0, needs[t] ?? 0), 0);
  const over = total - room;
  const sliderStep = step(room);
  const legacy = !keep.manual && Object.values(saved).some((t) => t > 0);
  const set = (resource: string, value: number) =>
    setDraft({ ...shown, [resource]: Math.max(0, Math.min(room, Math.round(value))) });
  const all = (value: (resource: string) => number) =>
    setDraft(Object.fromEntries(listed.map((t) => [t, Math.max(0, Math.floor(value(t)))])));

  if (!editing && draft === null) {
    return (
      <div className="storage-sliders">
        <p className="muted small">
          A bar a good: what it holds of what it keeps. Amber: more than it keeps, on its way to your Trading
          Post to be sold.
        </p>
        <ul className="storage-view">
          {listed.map((resource) => {
            const holds = contents[resource] ?? 0;
            const need = needs[resource] ?? 0;
            const kept = Math.max(saved[resource] ?? 0, need);
            const top = Math.max(kept, holds, 1);
            const inComing = coming[resource] ?? 0;
            const sold = selling[resource] ?? 0;
            return (
              <li key={resource}>
                <HoverPopup
                  width={280}
                  popup={
                    <>
                      <strong className="tip-title">{content.resourceName(resource)}</strong>
                      <span className="tip-section">
                        <span>
                          Holds {formatAmount(holds)} · keeps {formatAmount(kept)}
                        </span>
                        {need > (saved[resource] ?? 0) + 0.5 && (
                          <span>
                            You set {formatAmount(saved[resource] ?? 0)}; it keeps {formatAmount(need)} for
                            what your next buildings, levels and orders need.
                          </span>
                        )}
                        {holds > kept + 0.5 && (
                          <span>{formatAmount(holds - kept)} more than it keeps: on its way to be sold.</span>
                        )}
                      </span>
                      {(inComing > 0.05 || sold > 0.05) && (
                        <span className="tip-section">
                          {inComing > 0.05 && <span>Coming in: {formatAmount(inComing)}/min</span>}
                          {sold > 0.05 && <span>To your Trading Post: {formatAmount(sold)}/min</span>}
                        </span>
                      )}
                    </>
                  }
                >
                  <span className="storage-view-row">
                    <ResourceIcon resource={resource} />
                    <span className="storage-view-name">{content.resourceName(resource)}</span>
                    <span className="storage-fill" aria-hidden="true">
                      <span className="kept" style={{ width: `${(100 * Math.min(holds, kept)) / top}%` }} />
                      <span
                        className="over"
                        style={{ width: `${(100 * Math.max(0, holds - kept)) / top}%` }}
                      />
                    </span>
                    <span className="storage-view-amount">
                      {formatAmount(holds)} / {formatAmount(kept)}
                    </span>
                    <span
                      className={sold > 0.05 ? "storage-view-flow out" : "storage-view-flow"}
                      aria-label={
                        sold > 0.05 ? "being sold" : inComing > 0.05 ? "coming in" : "nothing moving"
                      }
                    >
                      {sold > 0.05 ? "↑" : inComing > 0.05 ? "↓" : ""}
                    </span>
                  </span>
                </HoverPopup>
              </li>
            );
          })}
        </ul>
        <div className="storage-view-actions">
          <button className="small" onClick={() => setEditing(true)}>
            Set what it keeps
          </button>
          <span className="muted small">↓ coming in · ↑ being sold · hover a good for its numbers</span>
        </div>
      </div>
    );
  }

  return (
    <div className="storage-sliders">
      <p className="muted small">
        How much of each good it keeps: above that goes on to your Trading Post and is sold (about a tenth of
        it a minute). The light mark is what your next buildings, levels and daily orders need: it keeps that
        even at 0.
        {legacy && " Now: the same share of its room for every good (the old buttons)."}
      </p>
      <ul className="storage-rows">
        {listed.map((resource) => {
          const value = shown[resource] ?? 0;
          const need = needs[resource] ?? 0;
          const holds = contents[resource] ?? 0;
          const kept = Math.max(value, need);
          return (
            <li
              key={resource}
              className="storage-edit-row"
              title={`Holds ${formatAmount(holds)}${need > value + 0.5 ? ` · keeps ${formatAmount(kept)}: what your next buildings, levels and orders need` : ""}${(coming[resource] ?? 0) > 0.05 ? ` · +${formatAmount(coming[resource] ?? 0)}/min coming in` : ""}`}
            >
              <ResourceIcon resource={resource} />
              <strong className="storage-view-name">{content.resourceName(resource)}</strong>
              <div className="storage-track">
                <span
                  className="storage-holds"
                  style={{ width: `${Math.min(100, (100 * holds) / room)}%` }}
                />
                {need > 0.5 && (
                  <span
                    className="storage-need"
                    title={`At least ${formatAmount(need)} kept for what your next buildings, levels and orders need`}
                    style={{ left: `${Math.min(100, (100 * need) / room)}%` }}
                  />
                )}
                <input
                  type="range"
                  min={0}
                  max={Math.floor(room)}
                  step={sliderStep}
                  aria-label={`${content.resourceName(resource)}: how much it keeps`}
                  value={value}
                  onChange={(event) => set(resource, Number(event.target.value))}
                />
              </div>
              <input
                className="storage-number"
                type="number"
                min={0}
                max={Math.floor(room)}
                step={sliderStep}
                aria-label={`${content.resourceName(resource)}: how much it keeps`}
                value={value}
                onChange={(event) => set(resource, Number(event.target.value) || 0)}
              />
            </li>
          );
        })}
      </ul>
      {others.length > 0 && (
        <select
          className="storage-add"
          aria-label="Add a good"
          value=""
          onChange={(event) => event.target.value !== "" && setAdded([...added, event.target.value])}
        >
          <option value="">+ Add a good…</option>
          {others.map((t) => (
            <option key={t.id} value={t.id}>
              {t.name}
            </option>
          ))}
        </select>
      )}
      <div className="chips">
        <button className="chip" onClick={() => all(() => 0)}>
          Only what&apos;s needed
        </button>
        <button className="chip" onClick={() => all(() => room / Math.max(1, listed.length))}>
          Split the room evenly
        </button>
        <button className="chip" onClick={() => all((t) => contents[t] ?? 0)}>
          Keep what it holds now
        </button>
      </div>
      <div className={`storage-total${over > 0.5 ? " over" : ""}`}>
        <div className="storage-total-line">
          <strong>Together</strong>
          <span>
            {formatAmount(total)} of {formatAmount(room)} room
          </span>
        </div>
        <div className="storage-total-bar">
          <span style={{ width: `${Math.min(100, (100 * total) / room)}%` }} />
          {withNeeds > total + 0.5 && (
            <span
              className="storage-total-needs"
              style={{
                left: `${Math.min(100, (100 * total) / room)}%`,
                width: `${Math.max(0, Math.min(100, (100 * withNeeds) / room) - Math.min(100, (100 * total) / room))}%`,
              }}
            />
          )}
        </div>
        <div className="small">
          {over > 0.5 && (
            <button className="link small" onClick={() => all((t) => ((shown[t] ?? 0) * room) / total)}>
              Fit to the room
            </button>
          )}{" "}
          {over > 0.5
            ? `${formatAmount(over)} more than fits: lower a slider to save.`
            : withNeeds > room + 0.5
              ? `With what's needed it keeps ${formatAmount(withNeeds)}: more than fits, so what comes first fills it.`
              : `With what's needed it keeps ${formatAmount(withNeeds)}; ${formatAmount(room - withNeeds)} room is free for what comes in on its way to be sold.`}
        </div>
      </div>
      {draft !== null && (
        <div className="button-row">
          <button
            className="primary"
            disabled={over > 0.5 || saving}
            title={over > 0.5 ? "The sliders together are more than its room" : undefined}
            onClick={() => {
              setSaving(true);
              void gameClient.setStorageKeeps(building, draft).then((ok) => {
                setSaving(false);
                if (ok) {
                  setDraft(null);
                  setAdded([]);
                  setEditing(false);
                }
              });
            }}
          >
            Save
          </button>
          <button
            onClick={() => {
              setDraft(null);
              setEditing(false);
              setAdded([]);
            }}
          >
            Cancel
          </button>
        </div>
      )}
      {draft === null && (
        <div className="button-row">
          <button onClick={() => setEditing(false)}>Done</button>
        </div>
      )}
    </div>
  );
}
