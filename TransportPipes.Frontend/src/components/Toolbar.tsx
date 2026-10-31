import { useEffect, useRef, useState } from "react";
import { gameClient } from "../app/client";
import { setState, useAppStore } from "../state/store";

/** Tools appear when they are useful: the Company Level that brings them (GDD §10.1, "one new toy per day") */
const UNLOCK_LEVEL: Record<string, number> = {
  hub: 2,
  research: 4,
  corporation: 8,
  blueprints: 22,
};

const SEEN_KEY = "tp.seenTools";

/** The generated icon of a tool (public/art/ui/tool-*.webp) */
const TOOL_ART: Record<string, string> = { bulldoze: "remove", corporation: "corp", blueprints: "plans" };
/** A tool drawn with another picture of the interface (public/art/ui/*.webp): Trades with the gold coin */
const UI_ART: Record<string, string> = { trades: "ui-gold" };

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
  const bar = useRef<HTMLDivElement>(null);
  // Toasts stay above the toolbar however wide it gets: its top, measured from the bottom of the screen
  useEffect(() => {
    const element = bar.current;
    if (element === null) {
      return;
    }
    const update = () => {
      const top = element.getBoundingClientRect().top;
      document.documentElement.style.setProperty(
        "--toolbar-clear",
        `${Math.round(window.innerHeight - top + 8)}px`,
      );
    };
    update();
    const observer = new ResizeObserver(update);
    observer.observe(element);
    window.addEventListener("resize", update);
    return () => {
      observer.disconnect();
      window.removeEventListener("resize", update);
    };
  });
  const tool = useAppStore((state) => state.tool);
  const keysOpen = useAppStore((state) => state.keysOpen);
  const buildMenuOpen = useAppStore((state) => state.buildMenuOpen);
  const content = useAppStore((state) => state.content);
  const researchOpen = useAppStore((state) => state.researchOpen);
  const hubOpen = useAppStore((state) => state.hubOpen);
  const yardOpen = useAppStore((state) => state.yardOpen);
  const tradesOpen = useAppStore((state) => state.tradesOpen);
  const tradeAsks = useAppStore(
    (state) =>
      state.links?.links.filter(
        (t) => (t.state === "offered" && !t.youOffered) || (t.askedGuaranteed != null && !t.askedByYou),
      ).length ?? 0,
  );
  const crewOpen = useAppStore((state) => state.crewOpen);
  const requests = useAppStore((state) => state.company?.crew?.requests ?? 0);
  const blueprintsOpen = useAppStore((state) => state.blueprintsOpen);
  const level = useAppStore((state) => state.company?.level ?? 1);
  // Tools the player has not tried yet glow
  const [seen, setSeen] = useState(seenTools);
  useEffect(() => rememberSeen(seen), [seen]);

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
      // Who lives around you, and what you do together (the owner, 2026-10-31: each menu one clear goal)
      key: "neighbours",
      label: "Yard",
      icon: "🤝",
      hotkey: "N",
      active: yardOpen,
      run: () => (yardOpen ? setState({ yardOpen: false }) : void gameClient.openYard()),
    },
    {
      // Every trade in one place: what runs now, links, prices and settings, the town
      key: "trades",
      label: "Trades",
      badge: tradeAsks,
      badgeTitle: "link offers and asks waiting for your answer",
      icon: "⚖",
      hotkey: "C",
      active: tradesOpen,
      run: () => (tradesOpen ? setState({ tradesOpen: false }) : void gameClient.openTrades()),
    },
    {
      key: "crew",
      label: "Crew",
      badge: requests,
      badgeTitle: "requests to settle in your Yard",
      icon: "🏳",
      hotkey: "K",
      active: crewOpen,
      run: () => (crewOpen ? setState({ crewOpen: false }) : void gameClient.openCrew()),
    },
    {
      key: "hub",
      label: "Hub",
      // Hub orders fill by themselves: no badge asks for them (after run 3, step 7)
      badge: 0,
      badgeTitle: "",
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
      hotkey: "Y",
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
    <div ref={bar} className="panel toolbar" role="toolbar">
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
            <img
              src={
                UI_ART[t.key] !== undefined
                  ? `/art/ui/${UI_ART[t.key]}.webp`
                  : `/art/ui/tool-${TOOL_ART[t.key] ?? t.key}.webp`
              }
              alt=""
            />
          </span>
          <span className="tool-label">
            {t.label}
            {"badge" in t && typeof t.badge === "number" && t.badge > 0 && (
              <span
                className="badge"
                title={`${t.badge} ${t.badgeTitle}`}
                aria-label={`${t.badge} ${t.badgeTitle}`}
              >
                {t.badge}
              </span>
            )}
          </span>
          <kbd>{t.hotkey}</kbd>
        </button>
      ))}
      <button
        className="tool keys-button"
        title="Mouse and keys (?)"
        onClick={() => setState({ keysOpen: !keysOpen })}
      >
        ?
      </button>
    </div>
  );
}
