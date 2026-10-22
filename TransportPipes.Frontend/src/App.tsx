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
import { MapLoading } from "./components/MapLoading";
import { YardPanel } from "./components/YardPanel";
import { CrewPanel, ShareCodes } from "./components/CrewPanel";
import { InviteInGame, KeepEstate } from "./components/KeepEstate";
import { HoverTip } from "./components/HoverTip";
import { HubPanel } from "./components/HubPanel";
import { NeighboursPanel, PlannedRouteBanner } from "./components/NeighboursPanel";
import { NetworkPanel } from "./components/NetworkPanel";
import { OverlayBar } from "./components/OverlayBar";
import { ParcelPanel } from "./components/ParcelPanel";
import { SelectionPanel } from "./components/SelectionPanel";
import { KeysHelp } from "./components/KeysHelp";
import { PipeTierBar } from "./components/PipeTierBar";
import { ReturnCard } from "./components/ReturnCard";
import { LoneCard } from "./components/LoneCard";
import { BlueprintPanel, BlueprintSaveDialog } from "./components/BlueprintPanel";
import { DeepParcelPanel, DeepVignette, LayerSwitch } from "./components/DeepPanel";
import { GoalTracker } from "./components/GoalTracker";
import { LevelUp } from "./components/LevelUp";
import { PhotoBar } from "./components/PhotoBar";
import { ModerationPanel } from "./components/ModerationPanel";
import { ResearchPanel } from "./components/ResearchPanel";
import { SeasonPanel } from "./components/SeasonPanel";
import { Toasts } from "./components/Toasts";
import { Toolbar } from "./components/Toolbar";
import { TopBar } from "./components/TopBar";
import { VersionBadge } from "./components/VersionBadge";
import { UpdateOverlay } from "./components/UpdateOverlay";
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
      // Ctrl+A: all your buildings on screen
      if (
        state.phase === "playing" &&
        !isTyping(event.target) &&
        (event.ctrlKey || event.metaKey) &&
        event.key.toLowerCase() === "a"
      ) {
        event.preventDefault();
        gameClient.selectInView();
        return;
      }
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
          gameClient.back();
          break;
        case "?":
          setState({ keysOpen: !state.keysOpen });
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
          if (state.yardOpen) {
            setState({ yardOpen: false });
          } else {
            void gameClient.openYard();
          }
          break;
        case "k":
          if (state.crewOpen) {
            setState({ crewOpen: false });
          } else {
            void gameClient.openCrew();
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
        case "f":
          gameClient.setPhotoMode(!state.photoMode);
          break;
        case "u":
          // With buildings selected: upgrade them (Shift: by ten)
          if (state.selection.length > 0) {
            void gameClient.upgradeSelection(event.shiftKey ? 10 : 1);
            break;
          }
          if (state.selectedBuilding !== null && state.buildingDetails?.upgradeCost != null) {
            void gameClient.upgrade(state.selectedBuilding, event.shiftKey ? 10 : 1);
          }
          break;
        case "y":
          // Plans (blueprints): U upgrades
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
        case "delete":
          if (state.selection.length > 0 && window.confirm(`Demolish ${state.selection.length} buildings?`)) {
            void gameClient.demolishSelection();
          }
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
  const photoMode = useAppStore((state) => state.photoMode);
  const mapShown = useAppStore((state) => state.mapShown);

  return (
    <div className="app">
      {content !== null && <GameCanvas />}
      {phase === "playing" && !mapShown && <MapLoading text="Unrolling the valley…" cover />}
      {phase === "playing" && photoMode && <PhotoBar />}
      {phase === "playing" && !photoMode && (
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
          <ErrorBoundary name="LevelUp">
            <LevelUp />
          </ErrorBoundary>
          <ErrorBoundary name="BuildMenu">
            <BuildMenu />
          </ErrorBoundary>
          <ErrorBoundary name="BuildingPanel">
            <BuildingPanel />
          </ErrorBoundary>
          <ErrorBoundary name="CrewPanel">
            <CrewPanel />
            <ShareCodes />
            <KeepEstate />
            <InviteInGame />
          </ErrorBoundary>
          <ErrorBoundary name="YardPanel">
            <YardPanel />
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
          <ErrorBoundary name="KeysHelp">
            <KeysHelp />
          </ErrorBoundary>
          <ErrorBoundary name="SelectionPanel">
            <SelectionPanel />
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
          <ErrorBoundary name="ReturnCard">
            <ReturnCard />
          </ErrorBoundary>
          <ErrorBoundary name="LoneCard">
            <LoneCard />
          </ErrorBoundary>
          <ConnectionOverlay />
        </>
      )}
      {phase === "login" && <AuthScreen />}
      {phase === "found" && <FoundCompany />}
      {phase === "loading" && <MapLoading text="Loading the world…" />}
      {phase === "error" && (
        <div className="screen">
          <div className="panel">{error}</div>
        </div>
      )}
      <Toasts />
      <VersionBadge />
      <UpdateOverlay />
    </div>
  );
}
