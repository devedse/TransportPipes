import { EVENT_ICONS, eventTime } from "../app/events";
import { useNow } from "../app/useNow";
import { useAppStore } from "../state/store";
import { HoverPopup } from "./HoverPopup";

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
            <HoverPopup
              key={event.id}
              popup={
                <>
                  <strong>
                    {EVENT_ICONS[event.kind]} {event.name} · {eventTime(event, now)}
                  </strong>
                  <span className="goods-tip-line">{event.effect}</span>
                  <span className="goods-tip-line muted">
                    An event comes to your town now and then, for a while: it changes what some buildings make
                    or what the market pays, for everyone in the town.
                  </span>
                </>
              }
            >
              <div className={`panel event-chip ${event.kind}`} tabIndex={0}>
                <span className="event-icon">{EVENT_ICONS[event.kind]}</span>
                <span>
                  <strong>{event.name}</strong>
                  <span className="muted small"> · {eventTime(event, now)}</span>
                </span>
              </div>
            </HoverPopup>
          ))}
        </div>
      )}
    </>
  );
}
