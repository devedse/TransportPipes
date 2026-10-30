// Crew emblems, the colours to pick from, and what a plot's land is good for (the picker and the crew panel)
import type { SeatDto } from "../api/contracts";

export const EMBLEM_ICONS: Record<string, string> = {
  anchor: "⚓",
  anvil: "⚒️",
  bee: "🐝",
  bolt: "⚡",
  bridge: "🌉",
  candle: "🕯️",
  cog: "⚙️",
  comet: "☄️",
  crown: "👑",
  drop: "💧",
  fern: "🌿",
  flame: "🔥",
  fox: "🦊",
  gear: "🛠️",
  hammer: "🔨",
  heart: "❤️",
  hive: "🍯",
  kettle: "🫖",
  key: "🔑",
  lantern: "🏮",
  leaf: "🍃",
  lighthouse: "🗼",
  moon: "🌙",
  mountain: "⛰️",
  oak: "🌳",
  owl: "🦉",
  pick: "⛏️",
  pipe: "🔧",
  rocket: "🚀",
  rose: "🌹",
  sail: "⛵",
  shell: "🐚",
  shield: "🛡️",
  star: "⭐",
  sun: "☀️",
  tower: "🏰",
  train: "🚂",
  valve: "🚰",
  wave: "🌊",
  wheel: "☸️",
};

export function emblemIcon(emblem: string | null | undefined): string {
  return (emblem !== null && emblem !== undefined ? EMBLEM_ICONS[emblem] : undefined) ?? "⭐";
}

export const CREW_COLORS = [
  "#236b69",
  "#c9963a",
  "#e0533c",
  "#2d6fb4",
  "#33761f",
  "#8a5d00",
  "#9b59b6",
  "#e84393",
  "#1abc9c",
  "#34495e",
];

/** A plot's strength (1.4 §5): its name and the good its ★4 deposit holds */
export const SPECIALTIES: Record<string, { name: string; icon: string; good: string }> = {
  windyHeights: { name: "Windy Heights", icon: "🌬️", good: "wind" },
  sunnyFlats: { name: "Sunny Flats", icon: "☀️", good: "sun" },
  riverside: { name: "Riverside", icon: "🌊", good: "water and sand" },
  copperGulch: { name: "Copper Gulch", icon: "🟠", good: "copper" },
  coalValley: { name: "Coal Valley", icon: "⚫", good: "coal" },
  oldForest: { name: "Old Forest", icon: "🌲", good: "wood" },
  stoneQuarry: { name: "Stone Quarry", icon: "🪨", good: "stone" },
  ironHills: { name: "Iron Hills", icon: "⛰️", good: "iron" },
};

/** The plain word for a land good */
const GOOD_WORDS: Record<string, string> = {
  copperOre: "copper",
  ironOre: "iron",
  titaniumOre: "titanium",
  rareEarth: "rare earth",
};

export function goodWord(good: string): string {
  return GOOD_WORDS[good] ?? good;
}

/** "copper and oil", "copper, coal and oil" */
export function andList(items: string[]): string {
  return items.length <= 1 ? (items[0] ?? "") : `${items.slice(0, -1).join(", ")} and ${items.at(-1)}`;
}

/** A plot's land in one line (1.4 §5): its strength and what it lacks ("Coal Valley · no copper or oil") */
export function landText(primary: string, lacks: string[] | null = null): string {
  const name = SPECIALTIES[primary]?.name ?? primary;
  if (lacks === null || lacks.length === 0) {
    return name;
  }
  const words = lacks.map(goodWord);
  return `${name} · no ${words.length <= 1 ? words[0] : `${words.slice(0, -1).join(", ")} or ${words.at(-1)}`}`;
}

export function landIcon(primary: string): string {
  return SPECIALTIES[primary]?.icon ?? "🌾";
}

/** The colours a company can pick */
export const COLORS = [
  "#e74c3c",
  "#e67e22",
  "#f1c40f",
  "#2ecc71",
  "#1abc9c",
  "#3498db",
  "#9b59b6",
  "#e84393",
  "#ecf0f1",
  "#95a5a6",
];

/** A company name to start with when the land doesn't suggest one */
const NAME_IDEAS = [
  "Brass & Kettle",
  "Copperleaf Co.",
  "Steam & Sons",
  "Valve Street Works",
  "Gaslight Pipes",
  "Ironfern Co.",
  "Bellows & Bolt",
  "Lantern Lane Co.",
  "Cogwheel Pipes",
  "Riverbend Works",
];

export function nameIdea(): string {
  return NAME_IDEAS[Math.floor(Math.random() * NAME_IDEAS.length)]!;
}

/** Whether a player lives on a plot */
export function lived(seat: SeatDto): boolean {
  return seat.state === "taken" || seat.state === "you";
}
