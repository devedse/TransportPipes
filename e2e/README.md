# Screenshot scripts

Scripts that play the game in a headless browser and take screenshots, used to check the look and the flows after
changes. They need the development setup running:

```sh
docker compose -f compose.dev.yaml up -d                                   # the database
ASPNETCORE_ENVIRONMENT=Development dotnet run --project TransportPipes.Server --urls http://localhost:5080
cd TransportPipes.Frontend && npx vite                                     # http://localhost:5173
```

Then `cd e2e && npm install && node <script>.mjs shots/<name>`. Each script registers a fresh player and uses the
development cheats (`TransportPipes:Cheats`) through `window.tp`.

| Script | What it shows |
| --- | --- |
| `onboarding.mjs` | A new player following Loady's first steps |
| `right-click.mjs` | A right-click on the map steps back (deselects, puts a tool away); a right-drag still pans |
| `selection.mjs` | Several buildings at once: a left-drag box, Shift and Ctrl clicks, Ctrl+A, upgrade all (button, U, Shift+U, a budget); a right-drag still pans, also under a level-up card |
| `network-view.mjs` | The network view (made against needed per resource, money per company) and the trade settings |
| `survey.mjs` | A new estate's starter kit in the Deposits overlay and what a parcel holds |
| `gate.mjs` | Closing and opening a gate: the buildings behind it keep running |
| `consistency.mjs` | After each kind of action (pipes, gates, upgrade, build, demolish, move) the playing tab matches a freshly loaded one (slow: a reload per action, about 5 min) |
| `network-ids.mjs` | A selected network rebuilt three times: the selection covers every pipe, its buildings don't go idle |
| `neighbour.mjs` | Loady's neighbour step as a player does it: pipe to the own gate, Neighbours → Connect to a works |
| `guide.mjs` | Loady's later steps: the Storage and what it keeps, the daily orders, a step that waits for a level |
| `art-check.mjs`, `showcase.mjs` | Generated buildings on an estate (`showcase.mjs shots/x type,type,...`) |
| `pipes.mjs` | Pipes and flowing resources |
| `pipe-gallery.mjs` | Every pipe piece close up: runs, bends, junctions, ends, sockets into buildings, the five tiers |
| `juice.mjs` | A finished building, coin pops and the level-up |
| `ui-check.mjs`, `title.mjs` | The panels, the title screen and the managers |
| `mobile.mjs` | The phone layout (390×844) |
| `far.mjs` | The hub and the zoomed-out world |
| `evolution.mjs` | Buildings at the level milestones |
| `photo.mjs` | Photo mode at golden hour and night |
| `beauty.mjs` | A working, decorated estate, in daylight and at golden hour (`beauty.mjs shots/x 1.5` close up) |

`load.mjs [fast4g|slow4g] [seconds on the title]` measures how long a new and a returning player wait on a throttled
phone network. It needs a production build served by the server (caching and compression as in production):
`npm run build` in TransportPipes.Frontend, then the server with
`--webroot <absolute path to TransportPipes.Frontend/dist>`; it talks to http://localhost:5080.
