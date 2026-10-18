import { useEffect } from "react";
import { gameClient } from "./app/client";
import { AuthScreen } from "./components/AuthScreen";
import { BuildingPanel } from "./components/BuildingPanel";
import { BuildMenu } from "./components/BuildMenu";
import { ConnectionOverlay } from "./components/ConnectionOverlay";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { FoundCompany } from "./components/FoundCompany";
import { GameCanvas } from "./components/GameCanvas";
import { HoverTip } from "./components/HoverTip";
import { ParcelPanel } from "./components/ParcelPanel";
import { ReportDialog } from "./components/ReportDialog";
import { ResearchPanel } from "./components/ResearchPanel";
import { Toasts } from "./components/Toasts";
import { Toolbar } from "./components/Toolbar";
import { TopBar } from "./components/TopBar";
import { VersionBadge } from "./components/VersionBadge";
import { appStore, setState, useAppStore } from "./state/store";

function isTyping(target: EventTarget | null): boolean {
  return (
    target instanceof HTMLInputElement ||
    target instanceof HTMLTextAreaElement ||
    target instanceof HTMLSelectElement
  );
}

function useShortcuts() {
  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      const state = appStore.getState();
      if (
        state.phase !== "playing" ||
        isTyping(event.target) ||
        event.ctrlKey ||
        event.metaKey ||
        event.altKey
      ) {
        return;
      }
      switch (event.key.toLowerCase()) {
        case "escape":
          if (state.researchOpen) {
            setState({ researchOpen: false });
          } else if (state.tool.kind !== "select") {
            gameClient.setTool({ kind: "select" });
          } else if (state.buildMenuOpen) {
            setState({ buildMenuOpen: false });
          } else {
            gameClient.select(null);
          }
          break;
        case "b":
          setState({ buildMenuOpen: !state.buildMenuOpen });
          break;
        case "p":
          gameClient.setTool({ kind: "pipe", tier: 1 });
          break;
        case "x":
          gameClient.setTool({ kind: "bulldoze" });
          break;
        case "r":
          if (state.researchOpen) {
            setState({ researchOpen: false });
          } else {
            void gameClient.openResearch();
          }
          break;
        case "v":
          gameClient.setTool({ kind: "select" });
          break;
        case "h":
          gameClient.goHome();
          break;
        case "+":
        case "=":
          gameClient.zoomBy(1);
          break;
        case "-":
          gameClient.zoomBy(-1);
          break;
        default:
          return;
      }
      event.preventDefault();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);
}

export function App() {
  useShortcuts();
  const phase = useAppStore((state) => state.phase);
  const error = useAppStore((state) => state.error);
  const content = useAppStore((state) => state.content);

  return (
    <div className="app">
      {content !== null && <GameCanvas />}
      {phase === "playing" && (
        <>
          <ErrorBoundary name="TopBar">
            <TopBar />
          </ErrorBoundary>
          <ErrorBoundary name="BuildMenu">
            <BuildMenu />
          </ErrorBoundary>
          <ErrorBoundary name="BuildingPanel">
            <BuildingPanel />
          </ErrorBoundary>
          <ErrorBoundary name="ParcelPanel">
            <ParcelPanel />
          </ErrorBoundary>
          <ErrorBoundary name="HoverTip">
            <HoverTip />
          </ErrorBoundary>
          <ErrorBoundary name="Toolbar">
            <Toolbar />
          </ErrorBoundary>
          <ErrorBoundary name="ResearchPanel">
            <ResearchPanel />
          </ErrorBoundary>
          <ErrorBoundary name="ReportDialog">
            <ReportDialog />
          </ErrorBoundary>
          <ConnectionOverlay />
        </>
      )}
      {phase === "login" && <AuthScreen />}
      {phase === "found" && <FoundCompany />}
      {phase === "loading" && <div className="screen loading-screen">Loading the world…</div>}
      {phase === "error" && (
        <div className="screen">
          <div className="panel">{error}</div>
        </div>
      )}
      <Toasts />
      <VersionBadge />
    </div>
  );
}
