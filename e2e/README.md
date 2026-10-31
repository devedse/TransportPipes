# Screenshot scripts

Scripts that play the game in a headless browser and take screenshots, used to check the look and the flows after
changes. They need the development setup running:

```sh
docker compose -f compose.dev.yaml up -d                                   # the database
ASPNETCORE_ENVIRONMENT=Development dotnet run --project TransportPipes.Server --urls http://localhost:5080
cd TransportPipes.Frontend && npx vite                                     # http://localhost:5173
```

Then `cd e2e && npm install && node <script>.mjs shots/<name>`. Each script registers a fresh player and uses the
development cheats (`TransportPipes:Cheats`) through `window.tp`. A 1.4 plot starts empty: `kit(page)` in `lib.mjs` lays
a small estate by the trading post (Wind Moilys, a Solar Panel, an Iron Mine and Small Factory, a Storage), a tile apart
and piped to the post (`kit(page, types, 2)` in Copper Pipe, its ingots stocked).

| Script | What it shows |
| --- | --- |
| `start-map.mjs` | The start on the map (1.4 §6): the free plots glowing, the whole town zoomed out, Pick for me and the plot card, Play (`start-map.mjs shots/x phone` on a phone) |
| `four-friends.mjs` | Phase A's check: Ana names her Yard and sends her "Play next to me" link; Ben opens it on a phone as a guest and lands next to her; Cleo finds Ana by name on the map; Ana closes the Yard, Dev's Pick for me goes elsewhere and his tap on the last plot asks; Ana says yes (four browsers) |
| `economy.mjs` | 1.4 phase B: the Company Level from gold earned; a Lab's two lines (a track tier and an Era tree item at once); the market's Hub Orders; the town's Wonder and a give switch; a level from 11 that waits for its goods; a workshop at the Yard's end and the Depot; an outskirts parcel bought in Era 2; a Monument's stage; a Wonder stage given whole and its Diamond builder's statue in the hub |
| `eras.mjs` | 1.4 phase C: a plot through Eras 2 to 5 with each Era's buildings (a screenshot each), the fog over the wilderness, a trunk line built from the Links tab and drawn over the fog, a player's fee of their own on it |
| `pipes.mjs` | A small estate's pipes, the lanes up close, look inside (the pointer on a pipe, then Alt), the connectors fading zoomed out |
| `onboarding.mjs` | A new player following Loady's first steps |
| `yard.mjs` | Two neighbours in one Yard, each piped to their Yard gate with the Yard panel's button: one's energy goes to the other's mine at the seller's crew price (two browsers, moved into a Yard nobody else lives at; the `yard` cheat runs the Yards' rounds now) |
| `links.mjs` | A link between two stations: A's Links tab suggests B (who needs A's energy), A offers it, B accepts, and both see it flow at the player price, drawn along the corridors (two browsers) |
| `return.mjs` | The way back: A is "gone" ten days (dev cheat) and comes back to one card |
| `right-click.mjs` | A right-click on the map steps back (deselects, puts a tool away); a right-drag still pans |
| `onetap.mjs` | One tap lays the pipe: a lone Wind Moily is stuck, its panel's fix names where the pipe goes and its price, and a single tap lays it (no preview to confirm) |
| `chores.mjs` | The middle game's chores explain themselves: a factory under Processing says what it uses and who makes it, the Hub's orders say how fast the trading posts fill them |
| `pipeunder.mjs` | A Small Factory set onto a tile of the kit's line: allowed, and the pipe runs on under it |
| `research-keep.mjs` | The Research panel's Keep researching switch, on by default, turned off |
| `advisor.mjs` | The Next steps card on a real estate: its three steps, Show me on the first, an upgrade done from the card |
| `chapter2.mjs` | Loady's second chapter above the advisor, the level roadmap in the XP panel, the level-up card with what the level brings |
| `paint.mjs` | The build tool paints: a drag builds a row, Shift-drag an area, a click one; the ? key lists the mouse and keys |
| `pipe-upgrade.mjs` | A copper pipe drawn with the mouse over grey pipes upgrades them where they lie |
| `touch.mjs` | Real touch events: a quick drag moves the map, a long press and drag selects with a box |
| `selection.mjs` | Several buildings at once: a left-drag box, Shift and Ctrl clicks, Ctrl+A, upgrade all (button, U, Shift+U, a budget); a right-drag still pans, also under a level-up card |
| `network-view.mjs` | The network view (made against needed per resource, money per company) and the trade settings |
| `survey.mjs` | A new plot in the Deposits overlay and what a parcel holds |
| `consistency.mjs` | After each kind of action (pipes, gates, upgrade, build, demolish, move) the playing tab matches a freshly loaded one (slow: a reload per action, about 5 min) |
| `network-ids.mjs` | A selected network rebuilt three times: the selection covers every pipe, its buildings don't go idle |
| `guide.mjs` | Loady's later steps: the Storage and what it keeps, the daily orders, a step that waits for a level |
| `art-check.mjs`, `showcase.mjs` | Generated buildings on an estate (`showcase.mjs shots/x type,type,...`; scaffolds buy their materials) |
| `pipes.mjs` | Pipes and flowing resources |
| `pipe-gallery.mjs` | Every pipe piece close up: runs, bends, junctions, ends, sockets into buildings, the six tiers |
| `pipe-connections.mjs` | Building inlets on all four sides, fed by an end, bend or continuous run; dragging to the raised Yard post and immediate quest completion; Loady's Show me highlights and plans the gate connection. `GATE_ONLY=1 SIDE=0..3 TOUCH=1` checks touch dragging at each side of a Yard; `PLAN_ONLY=1 TOUCH=1` checks Show me and Lay it on a phone |
| `connector-motion.mjs` | A gate receiving box and real ore / ingot, electricity and water traffic on building connectors. Records frames for the factory and water pump, then checks motion, reduced motion, idle connectors, distance fading and the sprite limit |
| `connector-depth.mjs` | Connectors on all four sides of a pipe: on the far sides (the building behind the pipe) the belt, cables and goods are drawn under the tube, on the near sides over it; close-ups of each (`BEFORE=1` for the old layering) |
| `prices.mjs` | Your prices in the trade settings: a cut counts at once, a rise waits an hour; the steppers step from where a price is going (the playtest's crew price stuck at 1.0×), say what it is until then and step back to what it inherits |
| `lumber.mjs` | Lumber Camps next to a plot's forest: the client allows them and the server builds one (playtest: the client still asked for the old forest's marked trees) |
| `buy-rest.mjs` | A level's goods bought one good at a time: each good's Buy the rest buys that good at its price, the level waits for the rest |
| `level-flicker.mjs` | A level's goods coming in: the building steps up in its own network (same id), and the running buildings on its pipes never show ZZ meanwhile |
| `income.mjs` | Where your gold a minute comes from: the gold panel's lines add up to the top bar's number, with what isn't in it and why it keeps changing |
| `one-lab.mjs` | One Lab: with one, the Build menu's Lab says "You have one", and a second is refused with why |
| `yard-short.mjs` | A building short of a good says why the Yard doesn't bring it, and the hub as the other way (the Yard's own cases are in WantFacts) |
| `own-goods-note.mjs` | Your own goods going into your levels: the Waits line and the Yard's What you're waiting for say what they would have sold for (the number is the server's, set here) |
| `panel-tops.mjs` | What a building does at the top of its panel, above Upgrade: a producer's Needs and Makes, a Storage's Stored, the Trading Post's sales against your Hub Orders with Buy from the hub |
| `pipe-sign.mjs` | A pipe on its way to a better one glows in the new pipe's colour with an arrow, the hover says what it waits for; the corner's Waits line offers Buy all the rest |
| `storage-drain.mjs` | A Storage's line lowered below what it holds: what's above goes on to the Trading Post and is sold, as fast as its pipe carries it; the panel says so |
| `chat-focus.mjs` | Typing on in the chat after Enter: the box keeps the focus and the next message goes in it (no map key fires), what is typed meanwhile stays |
| `finish-all.mjs` | The Yard's What you're waiting for: levels bought together are one line per good that costs what it says, no "has what it needs"; Buy all the rest at the top buys every want's rest |
| `yard-how.mjs` | The Yard panel's "How your Yard works: what happens when": open until you are piped to the gate, then folded, the pipe step ticked; Monument and workshops at their Company Level |
| `block.mjs` | Blocking a neighbour in the trade settings: the switch goes off at once and says when the block counts (an hour's notice); on again calls it off |
| `move-key.mjs` | The Move key: E with one of your buildings selected picks it up, E again puts it back; nothing selected or someone else's: nothing |
| `pipe-wants.mjs` | A better pipe asked for twice: the second ask is refused and says why, one want waits, paid once |
| `whats-new.mjs` | What's new after an update: the changes since the newest one seen on this browser, a day a list; seen once closed; none for a new company; the version badge shows the latest (needs `node scripts/changes.mjs` first) |
| `juice.mjs` | A finished building, coin pops and the level-up |
| `ui-check.mjs`, `title.mjs` | The panels, the title screen and the managers |
| `mobile.mjs` | The phone layout (390×844) |
| `far.mjs` | The hub and the zoomed-out world |
| `evolution.mjs` | Buildings at the level milestones |
| `photo.mjs` | Photo mode at golden hour and night |
| `beauty.mjs` | A working, decorated estate, in daylight and at golden hour (`beauty.mjs shots/x 1.5` close up) |

The playtests' fixes (each script's first lines say which finding it checks). A busy test world can run out of free
plots: a second server on its own database (`--ConnectionStrings:TransportPipes=…Database=transportpipes_e2e…
--TransportPipes:SeedTestUsers=false --urls http://localhost:5081`), Vite with `TP_SERVER=http://localhost:5081`, and
`BASE_URL` pointing at that Vite.

| Script | What it shows |
| --- | --- |
| `deadend.mjs`, `onetap.mjs`, `waits.mjs` | A building that can't run says why, with one fix; a scaffold says what it waits for |
| `land.mjs` | The seat picker tells the truth; friends find each other late; dealt land: a Yard's plots hold all five land goods |
| `spending.mjs`, `refusal.mjs`, `wants.mjs` | A panel says what the next level and Max will do; a refusal points at a tile that works; scaffolds want materials |
| `card-fold.mjs` | The return card leads with what matters; the crew's news in one place |
| `orders.mjs`, `era-wonder.mjs` | Hub orders as buyers: one a good sold, a good that starts selling today gets its own, how far each is, Deliver from the Storages; the town's Era Wonder from the tracker's Era row: Town & Wonder, its give switches, buy and give |
| `pipe-door.mjs`, `gold.mjs` | After run 4: the pipe ladder's door; "Your gold can" and Grow everything; the Landmark's storeys |
| `numbers.mjs`, `loady-upgrade.mjs`, `map-clicks.mjs` | After run 4: one income everywhere; Loady's upgrade lands where a level pays; refusals, ghosts and clicks |
| `exchange.mjs`, `phone-reading.mjs`, `post-max.mjs` | After run 4: the Exchange at your own price, and its expiry; phone panels, reading and a top bar that fits; a Trading Post selling past its Hub Orders: its Max and Grow |
| `consistency.mjs` | After every kind of change, the playing tab matches a freshly loaded one |
| `big-estate.mjs` | A grown estate (about 90 buildings on 300 pipe tiles, with the dev cheats): a building placed into its network shows on the map within the budget (1.5 s; `THROTTLE=4` runs the page four times slower). Start the server with `--TransportPipes:SlowWorldMilliseconds=30` to log where each slow world operation's time went |
| `panels.mjs`, `panels-network.mjs`, `phone-pipe.mjs`, `deep.mjs` | Every panel under the theme; the post's flows; a phone's pipe; the Deep Level |
| `statistics.mjs` | Statistics (`docs/design-notes/statistics.md`): two and a half days of play with stretches away, then the panel's graphs, moments, journal, compare, someone else's public part, the admin's Pace, the return card's graph and the phone. It moves the clock, so it needs a server on the playtest clock and its own database (`TransportPipes__Playtest=true TransportPipes__PlaytestToken=stats-local TransportPipes__PlaytestDataDir=<dir>` and a `…Database=transportpipes_stats…` connection string, in Development for the cheats and the seeded admin) |

`load.mjs [fast4g|slow4g] [seconds on the title]` measures how long a new and a returning player wait on a throttled
phone network. It needs a production build served by the server (caching and compression as in production):
`npm run build` in TransportPipes.Frontend, then the server with
`--webroot <absolute path to TransportPipes.Frontend/dist>`; it talks to http://localhost:5080.
