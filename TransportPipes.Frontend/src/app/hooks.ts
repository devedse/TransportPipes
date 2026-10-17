import { useEffect, useState } from "react";

/** A clock that re-renders the component every intervalMs (performance.now() based). */
export function useNow(intervalMs: number): number {
  const [now, setNow] = useState(() => performance.now());
  useEffect(() => {
    const id = window.setInterval(() => setNow(performance.now()), intervalMs);
    return () => window.clearInterval(id);
  }, [intervalMs]);
  return now;
}

/** Cycles 0..count-1 at the given frame rate (stays 0 when count <= 1). */
export function useFrameIndex(count: number, framesPerSecond: number): number {
  const [tick, setTick] = useState(0);
  useEffect(() => {
    if (count <= 1) {
      return;
    }
    const id = window.setInterval(() => setTick((value) => value + 1), 1000 / framesPerSecond);
    return () => window.clearInterval(id);
  }, [count, framesPerSecond]);
  return count <= 1 ? 0 : tick % count;
}
