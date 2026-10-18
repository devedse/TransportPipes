import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import { gameClient } from "./app/client";
import * as rules from "./game/rules";
import { appStore } from "./state/store";
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
