import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import { gameClient } from "./app/client";
import { TextureBank } from "./game/art/textures";
import { watchForUpdates } from "./app/updates";
import * as rules from "./game/rules";
import { appStore } from "./state/store";
import "@fontsource-variable/fraunces";
import "@fontsource-variable/nunito";
import "./styles/global.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);

if (import.meta.env.DEV) {
  // For the end-to-end scripts and the console
  Object.assign(window, { tp: { gameClient, appStore, rules } });
}

void gameClient.start();
watchForUpdates((listener) => gameClient.connection.onReconnected(listener));

// The first icons of the HUD and the map's art start loading behind the title screen
const FIRST_ICONS = [
  "tool-select",
  "tool-build",
  "tool-pipe",
  "tool-remove",
  "ui-gold",
  "ui-level",
  "ui-inbox",
  "ui-trophy",
  "ui-home",
  "ui-chat",
  "loady-wave",
];
window.addEventListener("load", () => {
  for (const icon of FIRST_ICONS) {
    new Image().src = `/art/ui/${icon}.webp`;
  }
  void TextureBank.load().catch(() => undefined);
});
