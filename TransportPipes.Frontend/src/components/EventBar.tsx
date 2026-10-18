import { EVENT_ICONS, eventTime } from "../app/events";
import { useNow } from "../app/useNow";
import { useAppStore } from "../state/store";

/** Running events in the player's district, under the top bar; the weather shows on the screen too */
export function EventBar() {
  const events = useAppStore((s) => s.company?.events);
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow(1000) + serverOffset;
  const active = (events ?? []).filter(
    (t) => new Date(t.startsAt).getTime() <= now && now < new Date(t.endsAt).getTime(),
  );
  const weather = active.find((t) => t.kind === "storm" || t.kind === "heatwave" || t.kind === "festival");
  return (
    <>
      {weather !== undefined && <div className={`weather ${weather.kind}`} aria-hidden="true" />}
      {active.length > 0 && (
        <div className="event-bar">
          {active.map((event) => (
            <div key={event.id} className={`panel event-chip ${event.kind}`} title={event.effect}>
              <span className="event-icon">{EVENT_ICONS[event.kind]}</span>
              <span>
                <strong>{event.name}</strong>
                <span className="muted small"> · {eventTime(event, now)}</span>
              </span>
            </div>
          ))}
        </div>
      )}
    </>
  );
}
