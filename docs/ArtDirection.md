# Art direction

Looks are one of the primary goals of v1: the buildings, the map and the UI. All art is generated with Codex image
generation (`gpt-6.1-sol`, reasoning effort `high`) and turned into game sprites by the scripts in `art/`. The
procedural art (`TransportPipes.Frontend/src/game/art`) stays as a fallback for anything without generated art.

## The look

Polished, stylised, hand-painted game art like a premium mobile tycoon game: chunky readable shapes, clean dark
outlines of medium weight, warm saturated (not neon) colours, soft painterly shading, light from the top left. The mood
is cheerful, cozy industry: brick, painted steel, brass, timber and glowing windows in a lush valley.

The target pictures (generated first, in `transportpipes-art/00-direction`): an estate mock-up (green valley, chunky
grey pipes with glowing dots, small paved pads under buildings, a stone wall along the estate, a teal and brass HUD), the
zoomed-out world (district grid, hub town, rivers and mountains), the UI kit (teal panels with brass rivets, a wooden
title plaque, cream content, chunky buttons, gold coin) and Loady, the mascot (the original game's doodled LOADING tile
come alive).

| Colour | Use |
| --- | --- |
| Teal `#24575a` / `#173c3f` | Panel frames, primary buttons |
| Cream `#f2e7cc` | Panel content, Loady's speech bubble |
| Brass `#c9963a` | Rivets, borders, highlights |
| Wood `#7a4a24` | Title plaques |
| Gold `#f6c744` | Gold, goals, rewards |
| Grass greens, warm brick, green-copper roofs | The map |

## Rules for generated art

- **Buildings** (`art/buildings.yaml`): square 1024² PNGs with a transparent background, the standard isometric camera,
  each building on its own ground plate that covers exactly its footprint (N×N tiles) and is the widest part of the
  picture. `art/process.mjs buildings` measures the plate's top surface, scales the sprite so it matches the footprint at
  1.5× world resolution, anchors it at the top surface's bottom corner and cuts off what sticks out below the
  footprint's front edges (the plate's side), so every building sits flush on its tiles. `art/fit.mjs` draws every
  sprite on its footprint diamond to check it; do that for every new or changed picture (see `CLAUDE.md`). Moving parts are
  separate pictures (`-rotor.png`, spun by the game); chimneys and rotor hubs are listed in `art/attachments.yaml`.
- **Terrain** (`art/terrain.yaml`): seamless top-down textures. The script tones them (the ground stays calmer than the
  buildings) and makes them seamless; the game maps a texture over 4×4 tiles, each tile its own cell projected onto its
  diamond, and blends a stronger terrain softly over a weaker one at their borders (water < beach < desert < grass <
  hills < forest < mountain; roads and the hub's paving keep crisp edges).
- **Props and decals** (`art/props.yaml`): trees, rocks, peaks, reeds, flowers and the deposits. Props stand on the
  ground (anchored at their foot), decals lie on it (anchored at their middle). Forest tiles get two or three trees,
  composed into one sprite per tile.

## Pipes

The pipes are the heart of the game, and they have to join their neighbours exactly in every combination, so they are
not generated pictures: `art/pipes.mjs` renders them as real 3D pieces with a small raymarcher, with the camera and the
light of the generated art (top left), soft shadows and the same dark outlines.

- Chunky tubes on concrete feet; smooth bends around the tile corner; a bolted cast hub where three or four pipes
  meet (a red valve wheel on a cross, a gauge on a T); a blind flange where a pipe ends; a tee with a welded collar
  where a straight run taps into a building beside it; an inlet box where a pipe plugs into a building's plate.
- Overlays drawn over the pieces where the game wants them: a brass flange at every third joint and where the tier
  changes, a foot under every other straight tile, the inlet boxes. Each overlay hides behind the tube it belongs to.
- Pipes plug only into buildings that take or give resources (not decorations or the HQ), and run on under gates.
- One material per tier: painted iron, copper, blue steel, glass with a glowing core, superconductor with violet
  bands. The flows are small glowing pellets riding along the tube, around the bends and into the sockets.
- The pieces are rendered once per shape and shaded per tier into one atlas per tier (`public/art/pipes/`); the first
  tier loads with the map, the others when they are first needed.

## Pipeline

```sh
cd art
python3 make-prompts.py <batch> buildings|terrain|props <ids...>   # writes prompts/<batch>.md
./queue.sh <batch>...                                              # Codex, one batch after the other
node process.mjs buildings|terrain|props <source dir>              # → TransportPipes.Frontend/public/art
node pipes.mjs [shape]                                             # the pipe atlases (a shape: a quick look in scratch/)
node sheet.mjs <out.png> 360 <images...>                           # a contact sheet to review a batch
```

Every run stamps a content hash of each sprite into the manifest (`node process.mjs hashes` does only that): the game
asks for `<sprite>?v=<hash>`, which the server lets browsers keep for good. The game waits only for the map's own art
(terrain and props); a building's picture loads the first time it is needed (a bare ground plate stands in for it
meanwhile), and once the map is up the rest goes quietly into the browser's cache.

The generated originals live outside the repository (`../transportpipes-art`, 1–4 MB each); only the processed WebP
sprites and `manifest.json` are committed. Every batch is reviewed before it goes in: angle and plate, style next to
the other art, readability at game size; anything off is generated again with a better prompt.
