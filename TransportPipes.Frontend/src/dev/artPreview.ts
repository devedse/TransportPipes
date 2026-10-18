// Dev page (art.html): every procedural sprite, to check the art. Not part of the game build.
import { drawBuilding, drawGate, drawHubStructure } from "../game/art/buildings";
import { drawDeposit, drawForestTrees, drawRocks, drawTerrainTile } from "../game/art/terrain";
import type { TerrainType } from "../api/contracts";

const root = document.getElementById("art")!;
const params = new URLSearchParams(location.search);
const scale = Number(params.get("scale") ?? "0.5");
const only = params.get("only");

function show(name: string, canvas: HTMLCanvasElement) {
  const figure = document.createElement("figure");
  const view = document.createElement("canvas");
  view.width = canvas.width * scale;
  view.height = canvas.height * scale;
  view.getContext("2d")!.drawImage(canvas, 0, 0, view.width, view.height);
  const caption = document.createElement("figcaption");
  caption.textContent = name;
  figure.append(view, caption);
  root.append(figure);
}

const response = await fetch("/api/content").catch(() => null);
const content = response?.ok
  ? ((await response.json()) as { buildings: Array<{ id: string; size: number }> })
  : null;
const buildings = content?.buildings ?? [];

if (only === null || only === "terrain") {
  for (const type of [
    "grass",
    "hills",
    "forest",
    "beach",
    "water",
    "desert",
    "mountain",
    "road",
    "city",
  ] as TerrainType[]) {
    show(type, drawTerrainTile(type, 1));
  }
  show("trees", drawForestTrees(1));
  show("trees2", drawForestTrees(2));
  show("rocks", drawRocks(1));
  show("iron ★3", drawDeposit("#c0603a", 3, 1));
}
if (only === null || only === "buildings") {
  for (const building of buildings) {
    show(building.id, drawBuilding(building.id, building.size, true).canvas);
  }
  show("gate open", drawGate("open", true).canvas);
  show("gate closed", drawGate("closed", true).canvas);
  show("gate side", drawGate("contract", false).canvas);
}
if (only === null || only === "hub") {
  for (const [type, size] of [
    ["marketHall", 3],
    ["townHall", 3],
    ["exchange", 3],
    ["contractBoard", 2],
    ["tradingHouse", 2],
    ["park", 3],
    ["wonderSite", 6],
    ["corporationQuarter", 8],
  ] as const) {
    show(type, drawHubStructure(type, size).canvas);
  }
  for (const [label, color, level] of [
    ["PG", "#aa3355", 1],
    ["IRON", "#2e86de", 4],
    ["NOVA", "#2ec4a6", 7],
    ["MAX", "#f6c744", 10],
  ] as const) {
    show(`corporationHq ${level}`, drawHubStructure("corporationHq", 3, { label, color, level }).canvas);
  }
}
document.body.dataset.ready = "1";
