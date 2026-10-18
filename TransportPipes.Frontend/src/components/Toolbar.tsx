import { gameClient } from "../app/client";
import { setState, useAppStore } from "../state/store";

/** The tools at the bottom of the screen */
export function Toolbar() {
  const tool = useAppStore((state) => state.tool);
  const buildMenuOpen = useAppStore((state) => state.buildMenuOpen);
  const content = useAppStore((state) => state.content);
  const researchOpen = useAppStore((state) => state.researchOpen);
  const hubOpen = useAppStore((state) => state.hubOpen);
  const neighboursOpen = useAppStore((state) => state.neighboursOpen);
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
      key: "bulldoze",
      label: "Remove",
      icon: "✖",
      hotkey: "X",
      active: tool.kind === "bulldoze",
      run: () => gameClient.setTool({ kind: "bulldoze" }),
    },
  ];

  return (
    <div className="panel toolbar" role="toolbar">
      {tools.map((t) => (
        <button
          key={t.key}
          className={t.active ? "tool active" : "tool"}
          title={
            t.key === "pipe"
              ? `Pipe, ${content?.pipeTiers[0]?.gold ?? 2} g per tile (P)`
              : `${t.label} (${t.hotkey})`
          }
          onClick={t.run}
        >
          <span className="tool-icon">{t.icon}</span>
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
