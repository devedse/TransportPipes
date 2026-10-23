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
| `yard.mjs` | Two neighbours in one station, each piped to their Yard gate with the Yard panel's button: one's energy goes to the other's mine at the player price (two browsers, moved into a station nobody else lives in; the `yard` cheat runs the Yards' rounds now) |
| `links.mjs` | A link between two stations: A's Links tab suggests B (who needs A's energy), A offers it, B accepts, and both see it flow at the player price, drawn along the corridors (two browsers) |
| `houses.mjs` | Houses and stand-ins (on a fresh dev world): A takes a House's seat and gets its starter kit, B moves in next door, the Yard panel shows the Houses and what they do; B is put to sleep and A's Yard panel names B's stand-in (two browsers) |
| `return.mjs` | The way back: A is "gone" ten days (dev cheat) and sees the two screens, then is sent to the Quiet Isles ("isles") and sees the landing cards (its own seat preselected) and moves to another one |
| `project.mjs` | The station's project: A's spare goes to the Foundation, the `project` cheat finishes a stage and the next asks for a keystone; the Plan board and an Ask; the project in the Yard's corner on the map |
| `trunk.mjs` | A crew trunk: A founds a crew, the world moves to Era 2 (dev cheat), A starts a steel trunk to the next hub and pays its segments (the `stock` cheat brings steel and concrete); the trunk in the Links tab and on the map |
| `evening.mjs` | The first evening with four browsers: Ana starts with friends, Ben (a phone) and Cleo open their codes as guests and get the Houses' starter kits, the pipes meet (Handshake), the Foundation buys at once, the `project` cheat brings the Water Tower and its keystone, Dev opens the last code later |
| `crew.mjs` | Start with friends: Ana founds a crew from the start screen and keeps three seats, Ben opens his seat link on a phone and moves in as a guest, Cleo joins with the crew link and lands next door; Ana's crew panel, Ben's keep note and a move to another seat (three browsers) |
| `right-click.mjs` | A right-click on the map steps back (deselects, puts a tool away); a right-drag still pans |
| `onetap.mjs` | One tap lays the pipe: a lone Wind Moily is stuck, its panel's fix names where the pipe goes and its price, and a single tap lays it (no preview to confirm) |
| `chores.mjs` | The middle game's chores explain themselves: the Lab under Processing says what it uses and who makes it, the Hub's orders say they come out of a Storage, with a button to build one |
| `pipeunder.mjs` | A Small Factory set onto a tile of the starter kit's line: allowed, and the pipe runs on under it |
| `research-keep.mjs` | The Research panel's Keep researching switch, on by default, turned off |
| `advisor.mjs` | The Next steps card on a real estate: its three steps, Show me on the first, an upgrade done from the card |
| `chapter2.mjs` | Loady's second chapter above the advisor, the level roadmap in the XP panel, the level-up card with what the level brings |
| `paint.mjs` | The build tool paints: a drag builds a row, Shift-drag an area, a click one; the ? key lists the mouse and keys |
| `pipe-upgrade.mjs` | A copper pipe drawn with the mouse over grey pipes upgrades them where they lie |
| `touch.mjs` | Real touch events: a quick drag moves the map, a long press and drag selects with a box |
| `selection.mjs` | Several buildings at once: a left-drag box, Shift and Ctrl clicks, Ctrl+A, upgrade all (button, U, Shift+U, a budget); a right-drag still pans, also under a level-up card |
| `network-view.mjs` | The network view (made against needed per resource, money per company) and the trade settings |
| `survey.mjs` | A new estate's starter kit in the Deposits overlay and what a parcel holds |
| `consistency.mjs` | After each kind of action (pipes, gates, upgrade, build, demolish, move) the playing tab matches a freshly loaded one (slow: a reload per action, about 5 min) |
| `network-ids.mjs` | A selected network rebuilt three times: the selection covers every pipe, its buildings don't go idle |
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

The playtests' fixes (each script's first lines say which finding it checks). Many need a world with free Capital
seats: a second server on its own database (`--ConnectionStrings:TransportPipes=…Database=transportpipes_e2e…
--TransportPipes:SeedTestUsers=false --urls http://localhost:5081`), Vite with `TP_SERVER=http://localhost:5081`, and
`BASE_URL` pointing at that Vite.

| Script | What it shows |
| --- | --- |
| `deadend.mjs`, `onetap.mjs`, `waits.mjs` | A building that can't run says why, with one fix; a scaffold says what it waits for |
| `seat-race.mjs`, `friend-late.mjs`, `land.mjs` | The seat picker tells the truth; friends find each other late; dealt land and its kit |
| `spending.mjs`, `refusal.mjs`, `wants.mjs` | A panel says what the next level and Max will do; a refusal points at a tile that works; scaffolds want materials |
| `card-fold.mjs`, `crew-news.mjs` | The return card leads with what matters; the crew's news in one place |
| `orders.mjs`, `refinements.mjs`, `era-wonder.mjs` | Hub orders as buyers; Refinements after an Era's tree; the Era Wonder as the Capital's want |
| `pipe-door.mjs`, `gold.mjs`, `storeys.mjs` | After run 4: the pipe ladder's door; "Your gold can" and Grow everything; the Landmark's storeys |
| `numbers.mjs`, `loady-upgrade.mjs`, `map-clicks.mjs` | After run 4: one income everywhere; Loady's upgrade lands where a level pays; refusals, ghosts and clicks |
| `exchange.mjs`, `phone-reading.mjs`, `post-max.mjs` | After run 4: the Exchange's price and expiry; phone panels and reading; a full Trading Post's Max and Grow |
| `consistency.mjs` | After every kind of change, the playing tab matches a freshly loaded one |
| `panels.mjs`, `panels-network.mjs`, `phone-pipe.mjs`, `deep.mjs` | Every panel under the theme; the post's flows; a phone's pipe; the Deep Level |

`load.mjs [fast4g|slow4g] [seconds on the title]` measures how long a new and a returning player wait on a throttled
phone network. It needs a production build served by the server (caching and compression as in production):
`npm run build` in TransportPipes.Frontend, then the server with
`--webroot <absolute path to TransportPipes.Frontend/dist>`; it talks to http://localhost:5080.
