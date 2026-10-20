import { useEffect } from "react";
import { gameClient } from "./app/client";
import { AuthScreen } from "./components/AuthScreen";
import { BuildingPanel } from "./components/BuildingPanel";
import { BuildMenu } from "./components/BuildMenu";
import { ChatPanel } from "./components/ChatPanel";
import { EventBar } from "./components/EventBar";
import { CompanyPanel } from "./components/CompanyPanel";
import { CorporationPanel } from "./components/CorporationPanel";
import { InboxPanel } from "./components/InboxPanel";
import { LeaderboardPanel } from "./components/LeaderboardPanel";
import { VisitPanel } from "./components/VisitPanel";
import { WildernessPanel } from "./components/WildernessPanel";
import { ConnectionOverlay } from "./components/ConnectionOverlay";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { FoundCompany } from "./components/FoundCompany";
import { GameCanvas } from "./components/GameCanvas";
import { GatePanel } from "./components/GatePanel";
import { HoverTip } from "./components/HoverTip";
import { HubPanel } from "./components/HubPanel";
import { NeighboursPanel, PlannedRouteBanner } from "./components/NeighboursPanel";
import { NetworkPanel } from "./components/NetworkPanel";
import { OverlayBar } from "./components/OverlayBar";
import { ParcelPanel } from "./components/ParcelPanel";
import { PipeTierBar } from "./components/PipeTierBar";
import { ReportDialog } from "./components/ReportDialog";
import { BlueprintPanel, BlueprintSaveDialog } from "./components/BlueprintPanel";
import { DeepParcelPanel, DeepVignette, LayerSwitch } from "./components/DeepPanel";
import { GoalTracker } from "./components/GoalTracker";
import { ModerationPanel } from "./components/ModerationPanel";
import { ResearchPanel } from "./components/ResearchPanel";
import { SeasonPanel } from "./components/SeasonPanel";
import { Toasts } from "./components/Toasts";
import { Toolbar } from "./components/Toolbar";
import { TopBar } from "./components/TopBar";
import { VersionBadge } from "./components/VersionBadge";
import { appStore, MODALS, setState, useAppStore } from "./state/store";

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
          if (
            state.researchOpen ||
            state.hubOpen ||
            state.neighboursOpen ||
            state.corporationOpen ||
            state.leaderboardOpen ||
            state.companyOpen ||
            state.blueprintsOpen ||
            state.moderationOpen
          ) {
            setState({ ...MODALS, buildMenuOpen: state.buildMenuOpen });
          } else if (state.selectedDeepParcel !== null) {
            setState({ selectedDeepParcel: null });
          } else if (state.blueprintDraft !== null) {
            setState({ blueprintDraft: null });
          } else if (state.inboxOpen) {
            setState({ inboxOpen: false });
          } else if (state.visiting !== null) {
            gameClient.closeVisit();
          } else if (state.wildTile !== null) {
            gameClient.closeWild();
          } else if (state.plannedRoute !== null) {
            setState({ plannedRoute: null, plannedPartner: null });
          } else if (state.tool.kind !== "select") {
            gameClient.setTool({ kind: "select" });
          } else if (state.buildMenuOpen) {
            setState({ buildMenuOpen: false });
          } else if (state.selectedGate !== null) {
            setState({ selectedGate: null });
          } else if (state.selectedNetwork !== null) {
            gameClient.selectNetwork(null);
          } else {
            gameClient.select(null);
          }
          break;
        case "o": {
          const order = ["none", "deposits", "wind", "sun", "owners"] as const;
          setState({ overlay: order[(order.indexOf(state.overlay) + 1) % order.length]! });
          break;
        }
        case "b":
          setState({ buildMenuOpen: !state.buildMenuOpen });
          break;
        case "p":
          gameClient.setTool({ kind: "pipe", tier: 1 });
          break;
        case "x":
          gameClient.setTool({ kind: "bulldoze" });
          break;
        case "n":
          if (state.neighboursOpen) {
            setState({ neighboursOpen: false });
          } else {
            void gameClient.openNeighbours();
          }
          break;
        case "m":
          if (state.hubOpen) {
            setState({ hubOpen: false });
          } else {
            void gameClient.openHub();
          }
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
        case "c":
          if (state.corporationOpen) {
            setState({ corporationOpen: false });
          } else {
            void gameClient.openCorporation();
          }
          break;
        case "i":
          if (state.companyOpen) {
            setState({ companyOpen: false });
          } else {
            void gameClient.openCompany();
          }
          break;
        case "g":
          gameClient.toggleDeep();
          break;
        case "u":
          if (state.blueprintsOpen) {
            setState({ blueprintsOpen: false });
          } else {
            void gameClient.openBlueprints();
          }
          break;
        case "l":
          if (state.leaderboardOpen) {
            setState({ leaderboardOpen: false });
          } else {
            void gameClient.openLeaderboard();
          }
          break;
        case "t":
          void gameClient.toggleChat();
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
          <DeepVignette />
          <ErrorBoundary name="EventBar">
            <EventBar />
          </ErrorBoundary>
          <ErrorBoundary name="TopBar">
            <TopBar />
          </ErrorBoundary>
          <ErrorBoundary name="GoalTracker">
            <GoalTracker />
          </ErrorBoundary>
          <ErrorBoundary name="BuildMenu">
            <BuildMenu />
          </ErrorBoundary>
          <ErrorBoundary name="BuildingPanel">
            <BuildingPanel />
          </ErrorBoundary>
          <ErrorBoundary name="GatePanel">
            <GatePanel />
          </ErrorBoundary>
          <ErrorBoundary name="NetworkPanel">
            <NetworkPanel />
          </ErrorBoundary>
          <ErrorBoundary name="OverlayBar">
            <OverlayBar />
          </ErrorBoundary>
          <ErrorBoundary name="PipeTierBar">
            <PipeTierBar />
          </ErrorBoundary>
          <ErrorBoundary name="ParcelPanel">
            <ParcelPanel />
            <DeepParcelPanel />
            <LayerSwitch />
          </ErrorBoundary>
          <ErrorBoundary name="HoverTip">
            <HoverTip />
          </ErrorBoundary>
          <ErrorBoundary name="Toolbar">
            <Toolbar />
          </ErrorBoundary>
          <ErrorBoundary name="NeighboursPanel">
            <NeighboursPanel />
            <PlannedRouteBanner />
          </ErrorBoundary>
          <ErrorBoundary name="HubPanel">
            <HubPanel />
          </ErrorBoundary>
          <ErrorBoundary name="ResearchPanel">
            <ResearchPanel />
          </ErrorBoundary>
          <ErrorBoundary name="CompanyPanel">
            <CompanyPanel />
          </ErrorBoundary>
          <ErrorBoundary name="BlueprintPanel">
            <BlueprintPanel />
            <BlueprintSaveDialog />
          </ErrorBoundary>
          <ErrorBoundary name="CorporationPanel">
            <CorporationPanel />
          </ErrorBoundary>
          <ErrorBoundary name="LeaderboardPanel">
            <LeaderboardPanel />
          </ErrorBoundary>
          <ErrorBoundary name="VisitPanel">
            <VisitPanel />
          </ErrorBoundary>
          <ErrorBoundary name="WildernessPanel">
            <WildernessPanel />
          </ErrorBoundary>
          <ErrorBoundary name="InboxPanel">
            <InboxPanel />
          </ErrorBoundary>
          <ErrorBoundary name="ChatPanel">
            <ChatPanel />
          </ErrorBoundary>
          <ErrorBoundary name="ModerationPanel">
            <ModerationPanel />
          </ErrorBoundary>
          <ErrorBoundary name="SeasonPanel">
            <SeasonPanel />
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
