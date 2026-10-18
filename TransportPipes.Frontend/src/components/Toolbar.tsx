import { gameClient } from "../app/client";
import { setState, useAppStore } from "../state/store";

/** The tools at the bottom of the screen */
export function Toolbar() {
  const tool = useAppStore((state) => state.tool);
  const buildMenuOpen = useAppStore((state) => state.buildMenuOpen);
  const content = useAppStore((state) => state.content);
  const researchOpen = useAppStore((state) => state.researchOpen);
  const hubOpen = useAppStore((state) => state.hubOpen);
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
      label: `Pipe · ${content?.pipeTiers[0]?.gold ?? 2} g/tile`,
      icon: "⧉",
      hotkey: "P",
      active: tool.kind === "pipe",
      run: () => gameClient.setTool({ kind: "pipe", tier: 1 }),
    },
    {
      key: "hub",
      label: ordersOpen > 0 ? `Hub · ${ordersOpen} orders` : "Hub",
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
          title={`${t.label} (${t.hotkey})`}
          onClick={t.run}
        >
          <span className="tool-icon">{t.icon}</span>
          <span className="tool-label">{t.label}</span>
          <kbd>{t.hotkey}</kbd>
        </button>
      ))}
    </div>
  );
}
