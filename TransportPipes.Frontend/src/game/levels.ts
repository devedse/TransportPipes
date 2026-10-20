/**
 * A building's level tier, in the colours of loot in games (GDD §17.3): each starts at a milestone of the building
 * levels (10 doubles the output, then 25, 50, 75, 100). Level 1 has none: a new building.
 */
export interface LevelTier {
  from: number;
  name: string;
  color: number;
  /** A lighter shade for highlights and the glow */
  light: number;
  /** The colour as CSS */
  css: string;
}

export const LEVEL_TIERS: LevelTier[] = [
  { from: 2, name: "Upgraded", color: 0x9aa3ab, light: 0xe3e7ea, css: "#7c858d" },
  { from: 10, name: "Fine", color: 0x3fae4a, light: 0xb6f0a8, css: "#2f8f3a" },
  { from: 25, name: "Superior", color: 0x2f7fe0, light: 0xa8d0ff, css: "#2468b8" },
  { from: 50, name: "Epic", color: 0x9b4fe0, light: 0xdcb8ff, css: "#7f3bc0" },
  { from: 75, name: "Legendary", color: 0xf08a24, light: 0xffd2a0, css: "#c56a10" },
  { from: 100, name: "Mythic", color: 0xf6c744, light: 0xfff3b0, css: "#a87a00" },
];

export function levelTier(level: number): LevelTier | null {
  return LEVEL_TIERS.filter((t) => level >= t.from).pop() ?? null;
}

export function tierIndex(level: number): number {
  return LEVEL_TIERS.filter((t) => level >= t.from).length - 1;
}

/** The next tier above a level, or null at the top */
export function nextTier(level: number): LevelTier | null {
  return LEVEL_TIERS.find((t) => t.from > level) ?? null;
}
