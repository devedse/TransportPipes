import { GameObjectType } from "../api/contracts";

export type ShortcutAction =
  | { kind: "build"; gameObjectType: number }
  | { kind: "delete" }
  | { kind: "inspect" }
  | { kind: "income" }
  | { kind: "dismiss" }
  | { kind: "help" };

const BUILD_KEYS: Record<string, number> = {
  f: GameObjectType.FactorySmall,
  m: GameObjectType.IronMine,
  w: GameObjectType.WindMoily,
  s: GameObjectType.SolarPanel,
  o: GameObjectType.Storage,
  t: GameObjectType.TradingBuilding,
  p: GameObjectType.GreyPipe,
};

export interface KeyLike {
  key: string;
  ctrlKey: boolean;
  metaKey: boolean;
  altKey: boolean;
}

export function shortcutForKey(event: KeyLike): ShortcutAction | null {
  if (event.ctrlKey || event.metaKey || event.altKey) {
    return null;
  }
  const key = event.key.length === 1 ? event.key.toLowerCase() : event.key;
  const buildType = BUILD_KEYS[key];
  if (buildType !== undefined) {
    return { kind: "build", gameObjectType: buildType };
  }
  switch (key) {
    case "Delete":
      return { kind: "delete" };
    case "i":
      return { kind: "inspect" };
    case "c":
      return { kind: "income" };
    case "Escape":
      return { kind: "dismiss" };
    case "?":
      return { kind: "help" };
    default:
      return null;
  }
}

/** True for elements where typed keys belong to the element, not to the game. */
export function isTypingTarget(target: EventTarget | null): boolean {
  if (!(target instanceof HTMLElement)) {
    return false;
  }
  return target.isContentEditable || ["INPUT", "TEXTAREA", "SELECT"].includes(target.tagName);
}

export const SHORTCUT_HELP: ReadonlyArray<{ keys: string; description: string }> = [
  { keys: "F", description: "Build a small factory at the hovered tile" },
  { keys: "M", description: "Build an iron mine" },
  { keys: "W", description: "Build a wind moily (windmill)" },
  { keys: "S", description: "Build a solar panel" },
  { keys: "O", description: "Build a storage" },
  { keys: "T", description: "Build a trading building" },
  { keys: "P", description: "Build a pipe" },
  { keys: "Delete", description: "Demolish the hovered building" },
  { keys: "I", description: "Inspect the hovered tile" },
  { keys: "C", description: "Recalculate my income" },
  { keys: "Esc", description: "Cancel building / close dialogs" },
  { keys: "Drag", description: "Pan the map" },
  { keys: "Wheel", description: "Zoom in and out" },
  { keys: "?", description: "Show or hide this help" },
];
