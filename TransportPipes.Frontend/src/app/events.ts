import type { EventKind, GameEventDto } from "../api/contracts";
import { formatDuration } from "./format";

export const EVENT_ICONS: Record<EventKind, string> = {
  storm: "⛈",
  heatwave: "☀",
  drought: "🏜",
  coldSnap: "❄",
  expo: "🎪",
  shortage: "📢",
  discovery: "💎",
  festival: "🎉",
  eraLaunch: "🚀",
};

/** "in 3 h 20 min" or "2 h left" */
export function eventTime(event: GameEventDto, serverNow: number): string {
  const start = new Date(event.startsAt).getTime();
  const end = new Date(event.endsAt).getTime();
  if (serverNow < start) {
    return `in ${formatDuration((start - serverNow) / 1000)}`;
  }
  return `${formatDuration((end - serverNow) / 1000)} left`;
}
