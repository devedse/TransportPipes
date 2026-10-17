export interface GoldSnapshot {
  currentGold: number;
  goldPerSecond: number;
  /** performance.now() / Date.now() style timestamp in ms when the snapshot was received. */
  receivedAt: number;
}

/** Gold at a moment in time, extrapolated from the last snapshot the server sent. */
export function goldAt(snapshot: GoldSnapshot | null, now: number): number {
  if (snapshot === null) {
    return 0;
  }
  const elapsedSeconds = (now - snapshot.receivedAt) / 1000;
  return snapshot.currentGold + elapsedSeconds * snapshot.goldPerSecond;
}

export function formatGold(gold: number): string {
  return Math.round(gold).toLocaleString("en-US");
}

/** Gold per second with up to 4 decimals, like the original HUD. */
export function formatGoldPerSecond(goldPerSecond: number): string {
  return (Math.round(goldPerSecond * 10000) / 10000).toString();
}

export function formatPerHour(valuePerSecond: number): string {
  const perHour = valuePerSecond * 3600;
  return Number.isInteger(perHour) ? perHour.toString() : perHour.toFixed(2);
}
