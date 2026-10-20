import { useEffect, useState } from "react";
import { gameClient } from "../app/client";
import { setState, useAppStore } from "../state/store";

/** Tools appear when they are useful: the Company Level that brings them (GDD §10.1, "one new toy per day") */
const UNLOCK_LEVEL: Record<string, number> = {
  hub: 2,
  neighbours: 3,
  research: 4,
  corporation: 8,
  blueprints: 22,
};

const SEEN_KEY = "tp.seenTools";

/** The generated icon of a tool (public/art/ui/tool-*.webp) */
const TOOL_ART: Record<string, string> = { bulldoze: "remove", corporation: "corp", blueprints: "plans" };

function seenTools(): Set<string> {
  try {
    return new Set(JSON.parse(localStorage.getItem(SEEN_KEY) ?? "[]") as string[]);
  } catch {
    return new Set();
  }
}

function rememberSeen(seen: Set<string>) {
  try {
    localStorage.setItem(SEEN_KEY, JSON.stringify([...seen]));
  } catch {
    // Private windows: the glow just shows again
  }
}

/** The tools at the bottom of the screen */
export function Toolbar() {
  const tool = useAppStore((state) => state.tool);
  const buildMenuOpen = useAppStore((state) => state.buildMenuOpen);
  const content = useAppStore((state) => state.content);
  const researchOpen = useAppStore((state) => state.researchOpen);
  const hubOpen = useAppStore((state) => state.hubOpen);
  const neighboursOpen = useAppStore((state) => state.neighboursOpen);
  const corporationOpen = useAppStore((state) => state.corporationOpen);
  const invites = useAppStore(
    (state) =>
      (state.corporations?.mine?.applications.length ?? 0) +
      (state.corporations?.mine === null
        ? (state.corporations?.district.filter((t) => t.invited).length ?? 0)
        : 0),
  );
  const blueprintsOpen = useAppStore((state) => state.blueprintsOpen);
  const level = useAppStore((state) => state.company?.level ?? 1);
  // Tools the player has not tried yet glow
  const [seen, setSeen] = useState(seenTools);
  useEffect(() => rememberSeen(seen), [seen]);
  const ordersOpen = useAppStore(
    (state) => state.company?.dailyOrders.filter((t) => t.delivered < t.amount).length ?? 0,
  );

  const tools = [
    {
      key: "select",
      label: "Select",
      icon: "➤",
      hotkey: "V",
      active: tool.kind === "select",
      run: () => gameClient.setTool({ kind: "select" }),
    },
    {
      key: "build",
      label: "Build",
      icon: "🏗",
      hotkey: "B",
      active: tool.kind === "build" || buildMenuOpen,
      run: () => setState({ buildMenuOpen: !buildMenuOpen }),
    },
    {
      key: "pipe",
      label: "Pipe",
      icon: "⧉",
      hotkey: "P",
      active: tool.kind === "pipe",
      run: () => gameClient.setTool({ kind: "pipe", tier: 1 }),
    },
    {
      key: "neighbours",
      label: "Neighbours",
      icon: "🤝",
      hotkey: "N",
      active: neighboursOpen,
      run: () => (neighboursOpen ? setState({ neighboursOpen: false }) : void gameClient.openNeighbours()),
    },
    {
      key: "corporation",
      label: "Corp",
      badge: invites,
      icon: "🏢",
      hotkey: "C",
      active: corporationOpen,
      run: () => (corporationOpen ? setState({ corporationOpen: false }) : void gameClient.openCorporation()),
    },
    {
      key: "hub",
      label: "Hub",
      badge: ordersOpen,
      icon: "🏛",
      hotkey: "M",
      active: hubOpen,
      run: () => (hubOpen ? setState({ hubOpen: false }) : void gameClient.openHub()),
    },
    {
      key: "research",
      label: "Research",
      icon: "🔬",
      hotkey: "R",
      active: researchOpen,
      run: () => (researchOpen ? setState({ researchOpen: false }) : void gameClient.openResearch()),
    },
    {
      key: "blueprints",
      label: "Plans",
      icon: "📐",
      hotkey: "U",
      active: blueprintsOpen || tool.kind === "blueprint" || tool.kind === "blueprintArea",
      run: () => (blueprintsOpen ? setState({ blueprintsOpen: false }) : void gameClient.openBlueprints()),
    },
    {
      key: "bulldoze",
      label: "Remove",
      icon: "✖",
      hotkey: "X",
      active: tool.kind === "bulldoze",
      run: () => gameClient.setTool({ kind: "bulldoze" }),
    },
  ];

  const shown = tools.filter((t) => level >= (UNLOCK_LEVEL[t.key] ?? 1));
  return (
    <div className="panel toolbar" role="toolbar">
      {shown.map((t) => (
        <button
          key={t.key}
          className={[
            "tool",
            t.active ? "active" : "",
            UNLOCK_LEVEL[t.key] !== undefined && !seen.has(t.key) ? "new" : "",
          ].join(" ")}
          title={
            t.key === "pipe"
              ? `Pipe, ${content?.pipeTiers[0]?.gold ?? 2} g per tile (P)`
              : `${t.label} (${t.hotkey})`
          }
          onClick={() => {
            if (!seen.has(t.key)) {
              setSeen(new Set([...seen, t.key]));
            }
            t.run();
          }}
        >
          <span className="tool-icon">
            <img src={`/art/ui/tool-${TOOL_ART[t.key] ?? t.key}.webp`} alt="" />
          </span>
          <span className="tool-label">
            {t.label}
            {"badge" in t && typeof t.badge === "number" && t.badge > 0 && (
              <span className="badge">{t.badge}</span>
            )}
          </span>
          <kbd>{t.hotkey}</kbd>
        </button>
      ))}
    </div>
  );
}
