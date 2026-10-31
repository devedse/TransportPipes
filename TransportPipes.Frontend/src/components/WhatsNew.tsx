import { useEffect, useRef, useState } from "react";
import { appStore, setState, useAppStore } from "../state/store";

/** The game's changes, newest first (scripts/changes.mjs writes them into the build) */
interface Changes {
  commit: string;
  date: string;
  changes: Array<{ commit: string; date: string; text: string[] }>;
}

/** The date of the newest change the player has seen */
const SEEN = "tp.whatsNewSeen";
const MAX = 20;
/** Earlier changes come this many at a time */
const PAGE = 30;

function readSeen(): string | null {
  try {
    return localStorage.getItem(SEEN);
  } catch {
    return null;
  }
}

function writeSeen(date: string) {
  try {
    localStorage.setItem(SEEN, date);
  } catch {
    // A private window: the card comes again next time
  }
}

async function loadChanges(): Promise<Changes | null> {
  try {
    const response = await fetch("/changes.json", { cache: "no-store" });
    return response.ok ? ((await response.json()) as Changes) : null;
  } catch {
    return null;
  }
}

/** "Today", "Yesterday", "Thu 29 Oct" */
function dayName(date: Date): string {
  const today = new Date();
  const yesterday = new Date(today.getTime() - 86_400_000);
  if (date.toDateString() === today.toDateString()) {
    return "Today";
  }
  if (date.toDateString() === yesterday.toDateString()) {
    return "Yesterday";
  }
  return date.toLocaleDateString([], { weekday: "short", day: "numeric", month: "short" });
}

/**
 * What's new (playtest: an update came with "New update!" and no word of what changed): after an update reloads the page,
 * the changes since the newest one the player saw on this browser; from the version badge or the keys list, the latest.
 * A new player gets none: nothing to catch up on.
 */
export function WhatsNew() {
  const open = useAppStore((s) => s.whatsNewOpen);
  const phase = useAppStore((s) => s.phase);
  const [changes, setChanges] = useState<Changes | null>(null);
  const [since, setSince] = useState<string | null>(null);
  // Looking further back (playtest: "can we also show historical updates?"): how many of all the changes show
  const [back, setBack] = useState<number | null>(null);

  useEffect(() => {
    if (phase !== "playing") {
      return;
    }
    void loadChanges().then((loaded) => {
      if (loaded === null || loaded.changes.length === 0) {
        return;
      }
      setChanges(loaded);
      const seen = readSeen();
      if (seen === null) {
        // The first time on this browser: a new company has nothing to catch up on; one that played before this card
        // existed gets the last two days
        if ((appStore.getState().company?.level ?? 0) < 2) {
          writeSeen(loaded.date);
          return;
        }
        setSince(new Date(new Date(loaded.date).getTime() - 2 * 86_400_000).toISOString());
        setState({ whatsNewOpen: "new" });
        return;
      }
      if (loaded.changes.some((t) => new Date(t.date) > new Date(seen))) {
        setSince(seen);
        setState({ whatsNewOpen: "new" });
      } else {
        writeSeen(loaded.date);
      }
    });
  }, [phase]);

  useEffect(() => {
    if (open === "latest" && changes === null) {
      void loadChanges().then(setChanges);
    }
  }, [open, changes]);

  // Closed any way (Got it, ×, Esc, a click beside it): seen
  const was = useRef(open);
  useEffect(() => {
    if (was.current !== false && open === false && changes !== null) {
      writeSeen(changes.date);
      setBack(null);
    }
    was.current = open;
  }, [open, changes]);

  if (open === false || changes === null) {
    return null;
  }
  const close = () => setState({ whatsNewOpen: false });
  const fresh =
    open === "new" && since !== null
      ? changes.changes.filter((t) => new Date(t.date) > new Date(since))
      : null;
  const shown = back !== null ? changes.changes.slice(0, back) : (fresh ?? changes.changes).slice(0, MAX);
  const earlier = changes.changes.length - shown.length;
  // One list a day, newest first
  const days: Array<{ day: string; items: string[] }> = [];
  for (const change of shown) {
    const day = dayName(new Date(change.date));
    if (days.at(-1)?.day !== day) {
      days.push({ day, items: [] });
    }
    days.at(-1)!.items.push(...change.text);
  }
  return (
    <div className="modal-backdrop whats-new-backdrop" onClick={close}>
      <div
        className="panel whats-new"
        role="dialog"
        aria-label="What's new"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="panel-header">
          <div>
            <h3>What&apos;s new</h3>
            <div className="muted small">
              {back !== null
                ? "Every change to the game, newest first"
                : open === "new"
                  ? "Since you last looked"
                  : "The latest changes to the game"}
            </div>
          </div>
          <button className="icon-button" title="Close (Esc)" onClick={close}>
            ×
          </button>
        </div>
        {days.map((d) => (
          <section key={d.day}>
            <h4>{d.day}</h4>
            <ul className="whats-new-list">
              {d.items.map((text, i) => (
                <li key={i}>{text}</li>
              ))}
            </ul>
          </section>
        ))}
        {earlier > 0 && (
          <button className="wide whats-new-earlier" onClick={() => setBack(shown.length + PAGE)}>
            Earlier changes ▸
          </button>
        )}
        <button className="primary wide" onClick={close}>
          Got it
        </button>
      </div>
    </div>
  );
}
