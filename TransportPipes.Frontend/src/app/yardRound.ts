import { appStore, useAppStore } from "../state/store";
import { formatDuration } from "./format";
import { useNow } from "./useNow";

/** Seconds to the Yards' next round, from the server's word (null: not known yet) */
export function yardRoundIn(next: string | null, offsetMs: number, now = Date.now()): number | null {
  return next === null ? null : Math.max(0, (new Date(next).getTime() - (now + offsetMs)) / 1000);
}

/** The same, counting down by the second */
export function useYardRoundIn(): number | null {
  const next = useAppStore((s) => s.nextYardRound);
  const offset = useAppStore((s) => s.serverOffsetMs);
  return yardRoundIn(next, offset, useNow(1000));
}

/**
 * For what something you did waits for (the owner, 2026-10-31: "if you do something that needs to wait for that, it
 * should mention that"): "the Yard's next round, in 42 s"
 */
export function yardRoundText(): string {
  const state = appStore.getState();
  const left = yardRoundIn(state.nextYardRound, state.serverOffsetMs);
  return left === null
    ? "the Yard's next round (within a minute)"
    : left >= 1
      ? `the Yard's next round, in ${formatDuration(left)}`
      : "the Yard's round now";
}
