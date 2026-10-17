import { useEffect } from "react";
import { gameClient } from "./app/instance";
import { isTypingTarget, shortcutForKey } from "./app/shortcuts";
import { BuildConfirmDialog } from "./components/BuildConfirmDialog";
import { BuildMenu } from "./components/BuildMenu";
import { GameCanvas } from "./components/GameCanvas";
import { Hud } from "./components/Hud";
import { LoginDialog } from "./components/LoginDialog";
import { ConnectionOverlay, InspectorPanel, ShortcutHelp, Toasts } from "./components/Panels";
import { appStore } from "./state/store";

function useKeyboardShortcuts() {
  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      const action = shortcutForKey(event);
      if (action === null) {
        return;
      }
      const state = appStore.getState();
      if (action.kind === "dismiss") {
        if (state.dismissTopmost()) {
          event.preventDefault();
        }
        return;
      }
      // Keys belong to inputs while typing, and to the dialog while one is open.
      if (isTypingTarget(event.target) || state.pendingBuild !== null) {
        return;
      }
      if (action.kind === "help") {
        event.preventDefault();
        state.setShortcutHelpOpen(!state.shortcutHelpOpen);
        return;
      }
      if (state.user === null) {
        return;
      }
      event.preventDefault();
      switch (action.kind) {
        case "build":
          void gameClient.buildAtHoveredTile(action.gameObjectType);
          break;
        case "delete":
          void gameClient.deleteAtHoveredTile();
          break;
        case "inspect":
          void gameClient.inspectHoveredTile();
          break;
        case "income":
          void gameClient.calculateMyIncome();
          break;
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);
}

export function App() {
  useKeyboardShortcuts();
  return (
    <div className="app">
      <GameCanvas />
      <Hud />
      <BuildMenu />
      <InspectorPanel />
      <ShortcutHelp />
      <Toasts />
      <BuildConfirmDialog />
      <LoginDialog />
      <ConnectionOverlay />
    </div>
  );
}
