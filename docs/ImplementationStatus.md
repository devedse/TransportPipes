# Implementation status

How far the game in [GameDesign.md](GameDesign.md) is implemented. Updated with every milestone.

## Architecture decisions

| Decision | Choice | Why |
| --- | --- | --- |
| Simulation | In-memory world per district, ticked every few seconds; flows are cached per network and only re-solved when the network changes (or a storage fills/empties) | Same result as the event-driven model of GDD §18.1, far simpler; offline progress is just the world running |
| Persistence | Commands are written to PostgreSQL immediately; fast-changing values (gold, storage contents, XP) are snapshotted every 30 s | A crash loses at most 30 s of production; the database is not hammered every tick |
| Flow solver | New solver (`TransportPipes.Game`) with the GDD flow order, replacing `ResourceCalculator` | Priorities, prices, capacities and losses don't fit the old recursion (agreed with the user) |
| Connectivity | Only pipes connect; buildings never pass resources on; trades happen between buildings on the same pipe group (original rule) | Layout matters, as in the original game |
| Content | Resources, buildings, research etc. in YAML files in `TransportPipes.Game/Content`, served to the client as JSON | Balancing without code changes, one source of truth |
| Accounts | Cookie authentication (own users table + ASP.NET Core password hasher); SignalR uses the cookie | Reconnects don't need a login |
| Art | Until M9: existing hand-drawn sprites plus a procedural `SpriteKit` (canvas drawing). From M9: generated art for everything (buildings, map, UI) with Codex image generation, `gpt-6.1-sol` at reasoning effort `high`; the procedural art stays as a fallback | A test on 2026-10-19 showed generated sprites far above the procedural ones, and looks are a primary goal of v1 |

## Milestones

Legend: ✅ done · 🚧 in progress · ⬜ not started

### M1 · Estates and the new core

- ✅ `TransportPipes.Game`: content (YAML), world layout (districts, estates, corridors, gates, parcels), terrain and deposits
- ✅ Flow solver with the flow order, storage, trading post, imports, losses and throughput; water-filling shares
- ✅ World state: buildings (1×1/2×2/3×3), pipes, networks, construction, levels and milestones, parcels, Company Level and XP, ticks with exact catch-up
- ✅ Persistence (new schema, world snapshots), cookie accounts (logins survive restarts), company creation, estate assignment
- ✅ SignalR protocol v2 (chunk groups, chunk deltas, networks and flows, districts, company state, toasts)
- ✅ Frontend v2: estate/district rendering at two levels of detail, procedural art for every building, build and pipe tools, HUD, building panel with issues, parcels, flow animation, offline report
- ✅ Old game code removed (Actionizer, Calculators, Common, old schema); the original calculation and connection scenarios run on the new world and solver

### M2 · Industry

- ✅ All Era 1 resources and buildings with art (procedural art for the whole catalog)
- ✅ Research and Labs: 45 techs in four branches (Eras 1–5 and the late game), research tree UI, knowledge diffusion, output and throughput bonuses
- ✅ Pipe tiers (with their materials), losses, throughput, valves (resource filters), priority valves, pump stations (one per 50 buildings)
- ✅ Network inspector (resources made/used/sold/stored/lost, full connections in red), overlays (deposits, wind, sun, owners)

### M3 · Economy

- ✅ Hub markets with dynamic prices per district (demand per active company, sold rate averaged over an hour, updated every 15 minutes)
- ✅ Player prices in the fair band (wider with Haggling), buyer maximum price, no-trade, one hour notice for partners; gate policies (open, contract, corporation, closed), public corridor pipes, toll meters
- ✅ Contracts (offers to a company or on the contract board, served before open trade, penalties) and contract reputation (stars)
- ✅ Exchange (order book per hub, reserved goods and gold, cargo with travel time, Cargo Depot halves it, fee as gold sink)
- ✅ Daily orders (three a day), hub reputation (+1% hub price per level)
- ✅ NPC tenants (producers and works on free estates, they answer contract offers, move out for new players), Neighbourhood panel with spare capacity, needs and one-click Connect

### M4 · Society

- ✅ Corporations: founding (Level 8, 10k gold), applications and invites, roles (leader, officers, members), kicks, leaving with a one day wait, treasury (donations, up to 10% tax on members' trade), HQ levels 1–10 with perks, weekly goals delivered from storages, HQ towers in the hub's corporation quarter
- ✅ Chat: district, corporation and direct channels with history, rate limit, block list and reporting
- ✅ Visiting (click an estate, or a name anywhere), likes, guestbook, mentors
- ✅ Inbox with unread count (offers, partners' price changes, full storages, likes, guestbook notes, new neighbours, corporation news), offline report v2 (full storages with one-tap upgrade, research, contracts, orders, neighbours, likes)
- ✅ Leaderboards (net worth, income, this week, level, partners, likes; district and world). Wonder and season boards come with M5 and M6

### M5 · Living world

- ✅ Events and calendar: storms (wind ×2, damage and repairs), heatwaves, droughts, cold snaps, expos, shortages (urgent orders), festivals; planned per district from the seed, announced a day ahead, three days with a Weather Station; weather on the screen
- ✅ District Wonders (Clock Tower, Grand Fountain, Observatory, Lighthouse; three stages, a perk for the district) and the Era Wonders in the Capital (Great Dynamo … Space Elevator; Bronze to Diamond rewards and titles), Eras with deadlines and a launch party, Era 2 power lines (energy-only cables between pylons, across water)
- ✅ Managers (Engineer Jo, Accountant Bea, Foreman Rick, Trader Lin, Planner Sam; slots from Company Level, Automation, Engineer's Workshop and corporation HQ 4), 44 achievements with XP, gold, titles and small bonuses, collections that unlock golden decorations, the Company panel
- ✅ Outposts and the wilderness: 8×8 claims on rich deposits from Company Level 15 (three at most, weekly lease), pipes through the wilderness, Cargo Depots that connect all their owner's networks, Discovery events with new rich deposits

### M6 · Full season

- ✅ Eras 3–5 content (buildings, resources, pipe tiers, Era Wonders up to the Space Elevator) and Era deadlines; the remaining tech effects: Futures (contracts up to 30 days), Global Exchange (orders trade across hubs), Trains (Cargo Depots ×2, cargo twice as fast), Market Insight (where hub prices are heading), Showroom (+10% for one product), Brand (hub billboards)
- ✅ Franchise (Shares: √(earnings since the last franchise / 1000), +1% production each, 24 h notice), the season finale (the Space Elevator or Era 5's deadline, a 24 h launch countdown, rankings), Legacy on the account (starting gold, titles, a manager slot from Legacy level 5) and the next world after a week
- ✅ Activity stages (active, resting, dormant with a Caretaker taking 10% and handing over corporation leadership, asleep after 30 days on the Quiet Isles), estates packed and unpacked with their layout, relocation once a week (an hour in transit), crates, swaps, corporation blocks (72 h), quiet districts offering a free move with a free parcel
- ✅ Mobile layout (bottom sheets, a compact top bar and toolbar, no sideways scrolling, pinch to zoom) and moderation tools (reported chat messages, delete, mute 24 h, ban 7 days, rename companies; banned players can't log in)

### M7 · Late game (the rest moved into the plan to v1 below)

- ✅ Denser: complexes (2×2 ×5 from Era 2, 3×3 ×12 from Era 3, one shared level), tile efficiency with a leaderboard, terraforming (drain, lakes, hills with more wind, forests, seeding deposits), blueprints (drag an area to save it, a ghost to place it, built at the normal costs; shared with the corporation or on the rated board)
- ✅ The Deep Level: Deep Lifts with a shaft below (throughput 2,000 per level), parcels dug out over hours to days (stone and ore into the storages), rock with veins of ★3–★5 metal, magma and rare earth, lantern-lit caverns, lossless pipes, Magma Tap, Vault, Crystal Grotto; the layer switch (G), deep build menu and dig panel
- ➡️ Teleporters, expeditions and mastery research → M8; branch estates → M8 (merged with the Franchise); Loady → M8 (as the guide) and M9 (art); photo mode and legendary looks → M9
- ➡️ Bank, District Steward, weekly challenges, season themes → after v1

## The plan to v1 (2026-10-19)

The first version has to be a **fully playable, fun idle game that looks good, and is multiplayer**. Looks are one of
the primary goals: the buildings, the map and the UI. The design may change where the loop needs it.

### A critical look at the game loop

| Problem | Why it matters | What we do |
| --- | --- | --- |
| The first session has no guide: an empty valley, 2,000 gold and many systems to discover | Idle games lose most players in the first minutes | Loady guides the first session; an always visible goal tracker (next, today, this week) |
| Two resets (Franchise wipes the estate, the season wipes the world) | Players who like to expand lose what they built; rebuilding isn't the fun | Expansion instead of resets (decisions below) |
| Little feedback: plain toasts, no celebration of chains, level-ups or milestones | "Number goes up" needs juice to feel good | Coin pops, level-up and chain moments, upgrade bursts, sound (M9) |
| Too many systems at once: 9 tools, 35 panels, several bars | Cognitive overload; the good parts stay hidden | Progressive UI: tools and panels appear when they unlock; fewer, merged panels |
| Neighbour trade (the multiplayer twist) only happens if the player finds the Neighbourhood panel | The multiplayer has to be felt early | Neighbour requests as cards with one-click Connect; other players visible on the map |
| Balance is untested: every screenshot used cheats | Too slow feels dead, too fast leaves nothing for week 3 | A bot simulation plays days at speed; tune to the GDD §10 day-by-day targets |
| Many late-game systems that add complexity rather than fun for a first version | Breadth before polish | Bank, Steward, challenges, gates after v1 |
| 73 buildings, 44 techs, 24 resources | Every building will need hand-made-quality art | Review the list: drop or merge buildings that add little before the art pass |
| The map is flat tiles with hard edges and strong lines; the UI is generic dark web panels with emoji icons | Doesn't look like a game people want to play | The art and UI redesign (M9) |

### Decisions

- **Expansion, not rebuilding.** The **Franchise opens a new estate** (a branch in another district, starting small
  and growing like the first); the old estate keeps running and the Shares bonus works for all estates. This replaces
  both the old Franchise reset and the separate branch estate feature.
- **A persistent world.** No season wipe: a season becomes a **chapter**. After the finale come the rankings, Legacy,
  titles and statues; then the next chapter starts a new Wonder race with a theme, while estates stay. Eras stay
  unlocked; newcomers are paced by Company Level and research, start in new frontier districts and get the catch-up
  help that exists (knowledge diffusion, mentors).
- **Evolve in place** where possible: a better building replaces an older one on the same spot, pipes kept, with a
  full refund of the old one, so an Era change feels like an upgrade, not a rebuild.

### M8 · Fun first: the loop for v1 (≈ 6–9 h)

- ✅ Teleporters (Pneumatic Post in a district, Quantum Link anywhere with 2% loss): a sender consumes, a receiver
  produces, networks never merge; linking in the building panel, arcs on the map
- ✅ Expansion instead of resets: the Franchise opens a branch estate (up to three estates, Shares for good); the
  world persists over chapters; evolve in place (the old building's gold and upgrades count, pipes stay); the
  Recycler gives all the gold back
- ✅ Onboarding: Loady guides the first sessions in eleven steps with "Show me" (tool, overlay, a pulsing marker on the
  best spot, a panel); a step that needs a higher level says so ("At Company Level 4 … come back later"); the goal
  tracker (Loady's step or the next level, today, this week)
- ✅ Progressive UI: tools appear with the level that brings them and glow until tried
- ✅ Neighbour trade offers itself: a card for a neighbour who needs what you make, with "Connect a pipe"
- ✅ Alerts for what holds you back: a network short of pump stations, a full trading post, a full storage
- ✅ Expeditions (1/4/12 h, gold, deposits, decorations, eight relics with lore) and mastery research
- ✅ Browser notifications while the game is in the background
- ✅ Balance: a bot simulation (typical, casual, hardcore days; `TP_BALANCE=1`, reports in `docs/balance`); the XP
  curve ramps to 4× by Level 9, so a typical player meets GDD §8.1's table (Level 10 on day 2–3, 20 in week 1, 30 in
  week 4). It also found the stale network bug (income counted again with every pipe extension), now fixed
- ⬜ Content review: fewer, more distinct buildings and techs (the list was checked: every building has a role; the
  Bank waits for its features)

### M9 · Looks: art and UI redesign (≈ 8–12 h plus generation time)

- ✅ Art direction (`docs/ArtDirection.md`) from four generated direction pictures (estate, world, UI kit, Loady)
- ✅ Art pipeline (`art/`): prompts from lists, a queue over the accounts, `process.mjs` aligns buildings to their
  plate, makes terrain seamless, places props and decals; rotors and smoke as separate parts; procedural fallback
- ✅ Every building has generated art (all reviewed; the gate stays procedural, its colour shows its policy), with a
  visual evolution at the milestones: a medal with a star per milestone, the pad's edge in bronze, silver, gold,
  copper, the owner's pennant from level 50, a golden glow and sparkles at 100
- ✅ The map: eleven terrain textures projected per tile with soft transitions; generated trees, peaks, bushes, reeds,
  flowers, stalagmites and deposits; the hub with generated buildings and a town of houses and shops along its
  streets; nine Wonders rising stage by stage; a painted far view; thinner borders and a light veil over locked parcels
- ✅ Pipes and flows: chunky shaded tubes with brass collars, per tier, flowing coloured dots
- ✅ UI: the teal, brass and cream theme with Fraunces and Nunito; generated icons for the tools, the top bar and all
  24 resources; coin pops, bursts, twinkles, the level-up celebration, synthesized sounds with a mute button; the
  phone layout checked
- ✅ Loady (four poses), manager portraits, the logo and the key art on the title screens
- ✅ Photo mode (F): no UI, day, golden hour, dusk or night, save as PNG

### M10 · v1: playable, fun, good-looking (≈ 3–5 h)

- ✅ The first session at a human pace (`BalanceSimulation.TheFirstSession`, `docs/balance/first-session.md`): Loady's
  first eight steps in 17 minutes (GDD §10.2's 20), then the Lab waits for Company Level 4, which the break brings
  (§10.3). What it found and what changed:
  - the neighbour card offered neighbours a new player can't supply (a building that misses energy *and* stone buys
    nothing): it now offers one whose whole shortfall you make
  - a new Storage took everything first and stopped the income for most of an hour: a storage now keeps what the
    company needs (today's orders, the corporation goal, the next building's materials) or the share its owner
    chose, the Trading Post sells the rest, and what it can't sell still goes into the storage
  - Loady asked for a Level 5 parcel at minute 6 and the Lab after it: the steps follow the sessions now (a Storage
    and a daily order in the first; the Lab, then the parcel) and say when they wait for a level
  - daily orders and corporation goals asked for what an era unlocks but a new company can't feed (gears, oil, rare
    earth): they ask for what the company makes and the next step of its chains
- ✅ Day 1 at a human pace (`TheFirstDay`, `docs/balance/first-day.md`; sessions at 0:00, 3:00, 10:00, 14:30 and the
  next morning): Level 4 comes in the first break and the Lab in session 2, the first research and Level 5 in the
  evening, a parcel before bed, as GDD §10.3 plans; week 1 follows the bot days (`TheFirstTwoWeeks`)
- ✅ Performance: building pictures load when they are first needed (the map waits only for its own art), hashed
  art is cached by browsers for good, the frontend is compressed; a new player on fast 4G waits 1.3 s from founding
  to the map (was 8.3 s), a returning one 0.7 s (was 3.3 s); `e2e/load.mjs` measures it
- ✅ v1.1 design (`docs/v1.1-design.html`): the plan below comes from the play day
- ✅ v1.1 · surveyors and the starter kit: every estate has copper, coal and stone (2×2, 2★) in its start parcels and a
  shore within reach; existing estates get it once at startup (deposit seeds and a pond, only on free land), new ones
  when they're founded; the parcel panel lists what a parcel holds
- ✅ v1.1 · direct partners only: no route crosses a third company's estate, so you trade with the companies you piped
  to yourself; pump stations per company (`docs/TradingRethink.md` has the full islands model for later)
- ✅ v1.1 · controls: a left-drag on the map draws a selection box (a right-drag pans); Shift adds or takes out, Ctrl
  takes all of a kind, a double click all of a kind in its network, Ctrl+A all of yours on screen; the selection panel
  upgrades them together (+1, +10 each, or a budget spent on the cheapest levels first) and demolishes them; U,
  Shift+U and Delete work on the selection. The level-up card no longer blocks the map
- ✅ v1.1 · the advisor: the goal tracker's "Next steps" are the three best things to do now (a shortage and its
  cheapest fix, full pipes, too few pumps, idle research, the upgrade that pays back quickest, the next chain, a
  partner, the next parcel, the basics), each measured by solving its network as if it were done; following only the
  advisor, the first day ends at Level 11 (`docs/balance/first-day-advisor.md`; the hand-written bot: Level 6)
- ✅ v1.1 · pipes upgrade where they lie: draw a better pipe over your own, or Upgrade on the network view's full
  connections; the advisor asks for the research or the copper ingots first
- ✅ v1.1 · Loady's chapter 2 (eight goals from copper ingots to bricks, done on day 2 when following Loady and the
  advisor) and the level roadmap (what the next levels bring and whether your land can use it, in the XP panel and
  the level-up card)
- ✅ v1.1 · painting with the build tool: a drag builds a row (Shift an area) where the building fits, as far as the
  gold goes, with ghosts and the total cost; the selection panel says what the selection makes together; the
  toolbar's ? lists the mouse and the keys
- ✅ v1.1 · screen rules: big windows dim everything and leave the inspector as it was; toasts stay above the toolbar
  and left of the inspector; on phones the guidance is one line plus Loady (159 px of 844)
- ✅ v1.1 · touch: a finger held still for a moment selects with a box (with the build tool it paints); a quick drag
  still moves the map; keyboard hints are hidden on touch screens
- ✅ Balance after v1.1 (`TheFirstTwoWeeks`, 28 days, `docs/balance/`): the starter kit made the middle game quicker,
  so from Level 22 the XP curve grows on to twice by Level 30 (`xpLateRamp`; levels only go up, nobody loses one).
  Typical: Level 10 on day 2, 20 on day 5, 30 on day 25; casual 30 on day 27; hardcore 30 on day 21; following only
  the advisor: 10 in the first day, 20 on day 4, 30 on day 27 (GDD §8.1: 20 in week 1, 30 in week 4). After run 4
  the ramp is 4× (gold that is spent now raises income: see the refinements below)
- ✅ The advisor on big estates (found by the simulation following it): it went silent from day 4. A network of 56
  buildings took 380 ms a solve (the solver chased rounding errors for 500 rounds; now about 24, the same result),
  big networks' gains are now estimated from their last solution instead of solved again, and it points at what an
  estate with full pipes lacks: a Lab for the research, a Storage for the copper ingots (storages keep the materials
  of the pipes you can lay), the mine an idle Smelter waits for
- ✅ Fair trade for now (found in play): a trade only happens when it beats the own trading post (long lossy pipes
  earned less), trade settings with partners per company (pipe length, how much arrives, a switch), who you trade
  with (everyone, corporation, contracts), gates open or closed, buying can be switched off per resource
- ✅ The neighbour step could get stuck (found in play): connected to two Ironworks, a new player sold them nothing,
  because a works only runs with all its inputs and the player's own factory used all its ore. Tenants' works now
  import what no neighbour brings, so any one of their inputs sells; Loady says why when a connection is not a trade
- ✅ A rebuilt network kept its old id on most pipes and buildings in the clients (found in play: selecting a network
  highlighted part of it, running buildings showed the idle bubble, until a reload): the changes now carry the new
  ids and every chunk of the network goes out again; a selection follows its network
- ✅ Pipes redesigned (the game's core): real 3D pieces rendered by `art/pipes.mjs` that join exactly, smooth bends,
  bolted hubs, tees and inlet boxes where pipes plug into buildings, flanges and feet spaced along the runs, five tier
  materials, glowing pellets for the flows; the drag preview shows the real pieces
- ✅ Bug bash: all fifteen e2e scripts run without errors; fixed what they showed (forest crowns cut at the tile, the
  phone's cut gold amount and toolbar, building pictures that never replaced their plates after a second engine)
- ✅ Accessibility basics: less motion when the system asks for it (CSS and the map's bursts and sparkles), WCAG AA
  contrast for the text on the cream panels, named side panels, Loady's steps announced, labelled icon buttons,
  visible focus
- ✅ The e2e screenshot scripts are in the repository (`e2e/`)
- 📐 v1.2 design: playing together (`docs/multiplayer-design.html`, the debate in `docs/design-notes/playing-together/`):
  stations of four seats around a shared Yard (a matcher between residents at one player price, 1.75), crews of 1–8
  with seat codes, two-ended links instead of shared networks, labelled NPC Houses and stand-ins for whoever sleeps,
  absence that costs nobody else, the station's project, crew trunks; the personal √ Hub Order; eight build steps
  (about 41 days), friends first. Not started; the start area stays as it is, new estates get their home goods in
  the first two parcels you can buy
- ⬜ Merge PR #3 and release

### v1.2 · Playing together (branch `feature/playing-together`, design `docs/multiplayer-design.html`)

The owner's go-ahead (2026-10-21): build all of it in a second PR; the test server runs this branch from now on; a
fresh world (no migration of the old one: the server starts a new world when the rules version changes, accounts
stay); name + password accounts only (guests on a hidden account they can keep); moves free with a 5 minute cooldown
(configurable). The design's steps, in the order the engine needs them:

- ✅ 1 · Stations and the Yard: station geometry (4 corner stations, 4 small stations per district), the Yard plaza and
  one Yard gate per estate (no side gates, no corridor pipes, no toll meters), the trade engine (gate nodes in each
  estate's own network, a matcher per Yard, payment for what the buyer takes), one player price (1.75 × base), the
  personal Hub Order (today's √ curve per company and hub, quiet grace per server), the Yard panel. Measured at the
  gate (the buyer's own pipe loss is its own), Houses after players within one 25% cap, contracts first; the Yards
  round once a minute while terms move and less often when they don't (16 minutes at most). Typical balance: Level
  30 on day 25 (was day 24 with the hub paying up to 1.25×); e2e `yard.mjs` plays two neighbours
- ✅ 2 · Crews and choosing where you live: crews of up to 8 (name filter, 40 emblems, two colours, nobody can be
  removed, a free move when you leave once a month), seats kept for friends with a code each (28 days, lapsing when
  nobody in the crew plays for two weeks), stations a crew fills closed to strangers unless it opens them, a crew
  spread over a corner station and the small one next to it trades in one Yard; the start screen's three ways (Find
  me neighbours, Start with friends, A station of my own) on a station picker (the district's seats, Yards and
  players); a friend's link opens the invite screen and moves in as a guest with two taps (a free company name
  ready; inside chat apps' browsers the account comes with the second tap), the crew link settles you next to the
  crew; guests keep their estate with a name and password (asked at Levels 3, 6 and 10, and in the company panel);
  moves to a chosen seat (free, a minute in the air, 5 minutes between moves, both configurable); Block. e2e
  `crew.mjs` plays Ana, Ben and Cleo
- ✅ 3 · Links between stations: one good from one company's Yard gate to another's outside its station, on a pipe
  routed through the corridors (and the wilderness between districts; at most two turns), loss per tile by pipe tier,
  the offerer pays the pipe (refunded when an offer lapses after three days or is declined), at most 12 links an
  estate and 5 offers an hour; a two-hour tank at the buyer's gate fills from the seller's spare and covers when it
  stops, a pause or the end comes with an hour's notice, what a link doesn't bring the Yard and the hub do, and every
  link has one state line; matched after contracts and before the Yard, damped like the Yard; links take the new way
  when an estate moves; contracts between stations run over a link (and survive a move); the Links tab in the Yard
  panel (offers, state lines, tanks, who a link would suit), links drawn on the map as covered channels with the goods
  moving along them. The Exchange already trades at the one price. e2e `links.mjs`
- ✅ 4 · Houses and stand-ins: today's tenants are Houses of four kinds (Reliable, Collector, Grower, Neighbour in
  need: no rival), as strong as their station's players (building levels = median active level − 3, at least 5,
  every day; a Grower adds buildings), labelled ⚙ with what they do; in small mode (fewer than 50 active companies)
  they keep every free seat warm, and every seat a crew holds for a friend; a newcomer who takes a House's seat gets
  its starter kit (3–4 producers that need nothing, at most level 5, piped to the Yard gate already, with a note);
  "Find me neighbours" prefers stations of four. Asleep in place: from day 30 the estate stays where it is and makes
  nothing, and a stand-in House keeps its Yard flows and links going (announced on day 23): what partners bought
  comes at the old price for 14 days, then 10% less a week (that gold leaves the game); what partners sold is paid
  NPC gold at the old price (a week longer for a crew member's partners), gliding to House terms over 14 days while
  the volume shrinks to a quarter; on day 90 the estate is packed for the Quiet Isles; waking in place gives the
  trades back. Frozen districts: not needed (see the refinements). e2e `houses.mjs`. The solo-parity balance run
  moves to step 8
- ✅ 5 · Absence and return: an asleep crew member's seat is held in place to day 90 (a week before, every active
  crew mate gets one keep card: "Keep Ben's seat 60 more days?", the first answer counts, once; holds lapse when no
  crew mate played for two weeks), without a crew to day 37; then the estate travels safely to the Quiet Isles and a
  House warms the seat. Coming back after three days or more: the way back's screens (what the estate earned, what
  the caretaker took, on how many days the crew tended it, the stand-in; that the estate is safe and the links are
  back; after the Quiet Isles the landing cards: your seat preselected, next to your crew, a lively station, the
  Frontier, each with its crates; another card is a free move). Tending (once a day per crew mate, banks for three
  days, counted once per day, never who). Tailwind (XP × min(2.5, 1 + (median − level) ÷ 10) while more than five
  levels behind the active median of the crew, else the station, else the district; shown in the top bar). The
  lone-human card (the fifth day as the only active player of a station, at most every 30 days: up to three livelier
  stations), crew moves as a block (a seat kept for every member for a week), the consolidation card (once, when
  more than half of a crew sleeps: the others move together, the sleepers' seats follow them), narrow probation
  (a stranger in a station a crew fills: away five days in its first two weeks, packed with nothing lost, its old
  seat the first landing card). e2e `return.mjs`. Not in this version: e-mail (the in-game report only), the five
  stills of the estate, the Era jump kit, the "Back!" ribbon and the Houses' postcard
- ✅ 6 · The station's project: Foundation → Water Tower → Workshop → Foundry Hall → Landmark, one per (big) station,
  a buyer in the Yard after the residents and Houses (or before them for a seller's "project first" goods) at the
  hub's full price; the Foundation's fixed basket (15 minutes of a Level-3 player's energy and iron ore), later stages
  three goods of the stage's tier (two the station is at home with, the keystone nobody makes, +20% credit; a tier up
  when the station is at home with every good), sized by the delivering residents (6 h of T(L) each, the Foundry Hall
  9 h for at least two, opening on day 14; the Landmark half their summed T(L) for 6 h); what each delivers is two
  hours of T(L) a day at most, banked for three days; credit as a ring of colours; bonuses for every seat (+3%, +3%,
  +4%, the Foundry Hall +10% on the station's two most-traded chains, the Landmark's heirloom); the project in the
  corner of the Yard, rising with its basket. T(L) is a table in progression.yaml. The Plan board (pins on the Yard
  panel and the picker's station card) and Asks (12–72 h, into the asker's storages at the player price, a House
  fills the rest at 2.0 × base at the deadline, renewable). e2e `project.mjs`. The Landmark is a basket, not yet a
  sustained flow with a tank
- ✅ 7 · The long game: beyond the own hub, public copper trunks join every hub to its open neighbours and the Port
  for everyone (2% fee that leaves the game, copper's loss over the distance); a company whose hub pays less than the
  full price sends part of its spare to the best far market, split so both Hub Orders pay alike (a second hub about
  √2 for a split volume good), the Port taking any amount of tier-2+ goods at 0.6 × base; crew trunks of steel to a
  hub or the Port, built in 10-tile segments any member pays (2 h of T(L), 300 steel, 100 concrete; from Era 2), no
  fee, public when the whole crew sleeps, drawn on the map in the payers' colours; outposts feed the Yard through
  their Cargo Depots; Discoveries every four days (announced a day ahead, a week long) with a marker for every station
  that brings something out in the first 72 hours and the three who brought most named; leagues of about 25 at a
  similar stage by the week's earnings, drawn every week, champions titled (verified companies only, off in small
  mode); Wonder stages sized like baskets (6 h of T(L) per active player of the district, everyone for the Era
  Wonder); Era Wonder tiers by the share of your own Era output (Bronze 1%, Silver 3%, Gold 7%, Diamond 15%)
- ✅ 8 · The first evening: four browsers (`e2e/evening.mjs`): Ana starts with friends and keeps three seats, Ben (a
  phone) and Cleo open their codes as guests, the Houses that kept the seats warm leave them starter kits piped to
  the Yard gate and the trading post, the pipes meet (a Handshake for both, Loady's Yard step), the Foundation buys
  from the station at once, the Water Tower comes with its keystone, and Dev opens the last code later with nobody
  online. Balance: the solo game is unchanged (`balance-typical.md`), and a newcomer in a v1.2 world (Houses, kit,
  project, trunks; `balance-typical-together.md`) reaches the same levels within hours: Level 10 on day 2, 20 on day
  5, 30 on day 25. The run found two things: an estate piped only to its gate sold the Houses less every round until
  nothing (their quarter is now of what it could make), and the kit did Loady's first steps for the player (it now
  counts only what the player builds and pipes). The first session at a human pace (`first-session.md`) earns from
  minute one and gets one Loady step further in 45 minutes

### v1.2 after the AI playtest (built; runs 2 and 3 played)

The AI playtest (`docs/playtest-design.html`, tools in `playtest/`): five AI players, 36 game days. Its verdict: a
lovely first evening, then five weeks side by side but never together; 4 of 20 promises kept.

What followed:
- **The fix plan.** A panel of five decided it: `docs/playtest-fixes.html`, F0–F11.
- **The redesign.** The six v1.2 designers came back, agreed one shared idea, and argued it against the fix panel's
  plan through one representative each, to a joint statement both signed. The record is in
  `docs/design-notes/after-playtest/`.
- **The revised plan:** `docs/playtest-redesign.html`, "Needs from the land, gifts by pipe".
  - Land goods are dealt per station, with no copies.
  - Kits make the seat's own good.
  - One price, 1.3, confirmed by light bots.
  - Houses sell land goods at 1.4, with a floor and a station backstop.
  - Send ▸ sits on top of the automatic Yard.
  - A stranger is seated next door to a crew.

**The build, in seven playable steps:**
- On today's world:
  1. The fix plan's foundations.
  2. The money's guards.
  3. The project and one return card.
  4. Send.
- With a new world:
  5. The land deal.
  6. The stranger next door.
  7. The rest, then run 2.

The owner's decisions (2026-10-22): build all of it; a new world on the test server is fine; the stranger next door
is the design's call (yes).

**Progress:**
- Step 1 (3962abe and after): the honest Yard, dead ends that explain themselves (IdleReason, one fix button),
  Next steps for everyone, the seat picker, smart Max. Gates: the row10 Yard replay 11/11 and agent replays of Ana,
  Dev, Cleo and Mia.
- Step 2 (d95cdad): the money's guards.
- Step 3: a project the station can reach (f3ed56a) and one return card (622992c), with friends by name. Gates: the
  Foundation by 18:25 on evening one, the day-7 restage, Ana's day-27 card naming Ben's stand-in, Ben's phone return
  in one tap.
- Step 4 (7026d26): Send, Thanks, named ends, bids, the Neighbours panel, the first gift, neighbour-fed chains.
  Gates: Dev's stuck dredger sends to the Water Tower in one tap, on a desktop and a phone.
- Step 5: dealt land (rules 3: a world made before keeps its land; the test server starts a new one),
  kits that make the seat's good, the Build-menu line and the Deposits overlay by seat, the seat card, Houses' land
  sales at 1.4 with the floor and the station backstop, the project paying the one price, Loady's chain steps on dealt
  land. The light bots (LightBotSimulation, TP_BALANCE=1; docs/design-notes/after-playtest/light-bots.md) chose
  1.3: the only price at which a factory buying its ore from a neighbour doesn't lose. Their open finding: trade
  between friends stays near 2%, because the iron chain on every estate is Era 1's best and fills the slots first.
- Step 5 done (24b06ea): the land deal at the one price 1.3.
- Step 6 (24b06ea): the stranger next door (on a small server someone who comes alone lives in the small station beside
  a crew, sharing its Yard and project) and the first-comer's line. Gate: the squatter probe (StrangerFacts,
  TP_BALANCE=1): a stranger seated next door who never plays again changes none of the crew's chains.
- Step 7 (a9b140d): F9–F11's rest and Loady's gift step. Then **run 2** (`playtest/runs/story2`, 2026-10-22): run 1's 14
  rows again on dealt land, with Jo (a scripted solo human) and Mia played twice (beside Jo, and next door to the
  crew); 32 sessions. Five fixes during the run (5ffd344, 28f6e4e, 964bb1e, f031e73): the phone Build menu, "only the
  X takes its energy", phone panels under full-screen panels, the Experience panel's Escape and ×, the "short of"
  text. The report (findings.json, report.html, shared with the owner): 11 of 24 promises kept; dead ends explain
  themselves, one return card, goods crossed estates and were paid (the crew earned twice run 1's income by day 36).
  But nobody felt the automatic flows as gifts, Sends went to energy crumbs, Thanks was never pressed, paid trade
  between friends stayed at 2–3% (mark 15%), the project's stages stood by themselves after day 11 (a bug: a basket
  shrinks to what was delivered when its residents are away), a building set into a pipe split the line (Ana, Cleo),
  and the middle game's chores (hub orders and Storage, the two-tap pipe, Labs that need ingots) made 13 STUCKs.
- **After run 2** (`docs/design-notes/after-playtest/run2-fixes.md`): the analyst's twelve recommendations, built:
  stages stand only by delivery, gifts named at both ends with Thanks, Send only where it moves something, the
  one-tap pipe, chores that explain themselves, damage and shortages in sight, a lone player's keystone, a building on
  its own pipe, keep researching, and the small breakers. The light bots found neither content lever raises friends'
  trade (an open design decision). Replays from run 2's snapshots: no stage stands by itself; Ana's day 8 rose to
  clarity 4, fun 4; Dev pressed the playtest's first Thanks. The playtest's one-device artefact (every pair of AI
  players priced at the hub) is fixed.
- **Run 3** (`playtest/runs/story3`, 2026-10-22): run 2's story again on that build, a fresh world, 32 sessions. Fixed
  during it: a friend's seat link closes a crew you're alone in (ccf5ca1). Better than run 2: gifts named and thanked
  at both ends (3 Thanks, 2 Tends; run 2: none), the first chosen chain between friends (Ana's stone ran Cleo's
  Brickworks for a week), one-tap pipe fixes, Level 10 by days 5–16. Worse: every station's project froze at 89–96% on
  a water keystone (passed over only when none ever came; "Not on water" for a pump on the pond), the Era 1 wonder got
  nothing, Ben never found the crew. Chores: about 11–15% of commands, nobody called it one; what wore players down was
  warnings that never clear and nothing left to want. 9 of 22 promises, 11 marks pass and 7 fail; the analyst's twelve
  recommendations are in `runs/story3/findings.json` (the owner decides what's next).
- **After run 3** (2026-10-23): a panel of three analysts (`docs/playtest-run3-review.html`) chose fixes first, then
  one redesign; the owner's read ("nothing to do with your gold") went to the six designers, whose nine steps
  (`docs/playtest-run3-design.html`, record in `docs/design-notes/after-run3/`) were all built, with the analysts'
  remaining rows: the keystone moves on and a small station keeps its stages; a truthful project line with Show me;
  only what works is offered; a friend's link first and a kept seat on every return; the stranger beside the crew
  that plays. Then step 1 a preview before every buy, "what stops you" and an hour's Undo; step 2 wants (a building
  short of materials is a scaffold the station fills: own Storage, a friend's one tap, the Yard); step 3 the pipe tier
  is a want; step 4 Order the rest (2 × base after the friends' window, over six hours); step 5 the Era wonder buys the
  Capital's spare by pipe, orders for half, stage perks, tiers in hours of income, an Era row; step 6 Refinements after
  an Era's tree and resting Labs; step 7 hub orders fill from the trading post's sales; step 8 a keystone two friends
  make together; step 9 Widen Copper and Steel pipes; and the rest: a falling gift told to both ends with its cause,
  exact books, a card that folds (one people line, a crew line, "Still:", order for a friend, the bell only for what
  you can act on), Crew and Station chat, projects named by station, friends elsewhere with Visit and Link, no
  caretaker fee. Each with tests and an e2e script; run 4 checks them against the design's marks.
- **Run 4** (`playtest/runs/story4`, 2026-10-23): run 3's story again on that build, a fresh world, 32 sessions. Fixed
  during it: a parcel offers only what the seat's land may make (c5a8828); a fix that puts a building in hand shows
  where it goes (a24082f); "nothing moves" says when the taker opens, the chat beside every right panel (cf7b133); a
  second tend says a crew mate tended it (6080147). Better than run 3: the whole crew lived together (Ben and Dev by
  Ana's links), every project stage stood (Capital SE's Landmark on day 17; run 3 froze at 96%), the stranger next
  door's wood stood the crew's Water Tower, previews and books within 1%, Ana's copper ran Cleo's Smelter all month,
  hub orders filled by themselves (no chores). **The gold is still not answered:** the players spent 0.09–0.25% of
  what they earned and banked 24–29 days of income; the new sinks took 30,660 g. The Era wonder stopped at its pipe
  half twice (Dynamo 89%: nobody made bricks; Cathedral 50%) and its stages were sized at the floor (a daily resize
  while nobody had played for three days; the Dynamo frozen by the first delivery), only two buildings became wants
  (bricks, never filled), nobody widened, and after the Landmark the crew had nothing shared to build. The light bots
  with the advisor bank 2.7–18.9 days on day 28 (`runs/story4/light-bots-day28.md`). 13 of 24 promises, 10 marks pass
  and 15 fail; the analyst's eleven recommendations are in `runs/story4/findings.json` (the owner decides what's next).
- **After run 4** (2026-10-23): the six designers met twice (`docs/design-notes/after-run4/`); the owner asked for my
  own reading instead of a third round, and my plan (`docs/playtest-run4-plan.html`) rebuilt theirs around one finding:
  the pipe tier is the game's real level cap. Then, to make it playable for people, all of it was built and every run-4
  finding fixed: the Era Wonder sized from this week's Capital players and orderable in full when nobody makes a good
  (90a4b56); the pipe ladder: Copper Pipes at Company Level 8 and Steel with Era 2 for everyone, one door on a full
  building's panel, Widen per link (f3e4440); wants that say they wait, the chain that would make a good, a friend's
  want in reach of the whole crew (44cf4f0); "Your gold can" with Grow everything that pays and an hour's Undo
  (e119134); gifts named at both ends and the lone player heard (3100920); the Landmark's storeys with a purse in hours
  of income and +10% pipes a storey (8a52a1c); words and numbers that agree: one source for income, previews capped by
  the inputs there, keystones chosen when a stage opens (c1d6868); the map and clicks (6eb9c87); the Exchange honest
  (39cb3d2); phone and reading (cf39fa6); harness fixes for the next run (the driver reports every message that popped
  up, refusals marked, the golden tile in the text map, clicks inside an open dialog first). The light bots, now pressing
  the new doors (the storey, the Wonder, Grow everything), found three more things, fixed: levels came too fast (spent
  gold raises income, and XP comes from income: Level 30 on day 14, so the late curve grows to 4× instead of 2×), a full
  Trading Post that Max priced at the whole bank so Grow left it out, and a storey gift that always ranked above a
  5-minute upgrade. On day 28 (`docs/balance/light-bots-after-run4.md`): the bots hold 1.1–3.8 days of income (run 4's
  bots 2.7–18.9, its players 24–29) and Mia 5.9 (she plays every other day); Level 30 on days 20–25; paid trade between
  friends by day 14 5.8% (was 2.0%); nothing runs at a loss. Still open: the solo bot earns 15% more a building than the
  crew (the mark wants 8–15% less), because the crew's gold goes into the storey's purse
- **Before playing with friends** (2026-10-23): the owner remembered a build taking 10 s in a moderately sized
  network. Not reproduced on today's rules: on a grown estate (87 buildings, 317 pipe tiles; `e2e/big-estate.mjs`) a
  build is on the map 60–160 ms after the click, 180–290 ms with the page six times slower; the play day's 10 s came
  with one network over the whole district (68 buildings, 1,050 pipe tiles, every company), which can't happen since
  pipes stay on their estate. Timing a build's steps on estates up to 216 buildings (`EstateSpeedFacts`, `TP_PERF=1`)
  found the advisor at 1.5 s, Max and the upgrade preview at 20 ms, and every Yard round slow in a big station: each
  building's why-it-idles, reach and spare went through all of its network's links and flows (20,000 links at 216
  buildings). A network index (links, flows and gate flows by building, once per solve) brought them to 0.12 s and
  1 ms, and the links between buildings are found 2.2× faster (arrays, one search per building). The server logs
  every world operation that takes over half a second with where its time went (waiting, running, collecting,
  sending). Friends on one Wi-Fi were one device to the Yard (hub price, no Sends or orders for each other, one
  keystone resident): the same IP the same day now only sizes a project's baskets, as v1.2 §3 says

### 1.4 · Your own place (branch `feature/own-place`, design `docs/v1.4-design.html`)

The design is `docs/v1.4-design.html` (built from `docs/design-notes/v1.4`, with the economy model in its `sim/`). It
is built in four phases (§23); each lands on the test server when it's done, and the test world starts again at phase
A (rules 4: accounts stay).

**Phase A · Your own place** (2026-10-25, 02ed076 … 1b63b16):
- Only people, one balance: Houses and stand-ins, starter kits, small mode, quiet grace and every size taken from
  the number of active players are gone; so are the station's project and Asks, corporations, trunks, leagues, sleep
  and the Quiet Isles, tending, Sends and Thanks, Refinements, knowledge diffusion and the Franchise (game, server and
  every panel). Wonder stages have fixed baskets.
- The map (§4): towns of four Yard groups (a Yard of 32×16 with a plot on each side), two roads crossing at the hub,
  the outskirts set aside for each plot, 128 tiles of wilderness and a town gate where each road leaves the town;
  plots turn to face their new Yard when they move; links run along the roads. The map draws square plots, the
  Yards with their crews' banners and names, the gates and the outskirts.
- The land (§5): every plot has water, iron ore, wood, wind and sun, and lacks one basic and one later good, dealt
  so a Yard's four plots lack different ones; a ★4 deposit next to the start area; later-Era deposits sleep. Red
  hills (bauxite) are the hills' texture on red earth (`node art/process.mjs derived`).
- Starting from scratch (an HQ and a trading post by the Yard gate) and picking your spot on the real map (§6): free
  plots glow, a plot card says what the land has and lacks and who lives at the Yard, Pick for me (a Yard where
  people played in the last three days, else an empty one; never a closed Yard), Find a player, a friend's "Play
  next to me" link (seven days, the first user may enter a closed Yard; without a plot it settles at the friend's
  Yard), requests to closed Yards. Moving uses the same picker.
- Yard = crew (§7): the first settler founds it, every resident is an owner, any of them names it, opens or closes
  it and answers requests (three days); the last one out takes the crew with it; a new town opens when every open
  town has fewer than two free plots. The Crew panel (K) and the top bar show it.
- Your own prices (§8): a public price (1.3×), a crew price (1.15×), per good and special prices for named players,
  1×–2.5× the base; the lowest that applies is paid; a rise waits an hour and the buyers are told. Each connected
  resident has a stall at its Yard gate with what it sells, its crew price and what it wants.
- Max and Grow keep spending up with income (§17): Grow buys the next parcel first as soon as the level and the gold
  allow (and saves for it while it is at most eight hours of income away), then every pipe door and Max that pays
  back within five days. The advice suggests another generator while the trading post sells all it is given.
- The model's player (`BalanceSimulation.Bot.ModelStep`): research when the Labs are idle, land first, then of the
  game's own next steps the one that pays back soonest within five days (nothing that waits for missing materials),
  the Era Wonder when nothing pays.
- The words of §3 in what players read: plot, Yard, town, crew.

Checks: 350 game tests (CrewFacts, PriceFacts, WorldLayoutFacts, WorldGeneratorFacts new), 22 server tests;
`e2e/four-friends.mjs` (four browsers, Ben on a phone: a Yard named and its link opened as a guest, Find a player, a
closed Yard's plot asked for and answered yes), `e2e/start-map.mjs` (desktop and phone), `e2e/big-estate.mjs` on the
new layout (a build into a 76-building network on the map in about 100 ms); the bots' first hour from nothing
(`ModelBotSimulation`: 15 buildings and 459 g/min after an hour, about four minutes of income in hand). The model's
days (`TP_BALANCE=1`) show where today's economy stops: income flat from day 3 with factories short of inputs and
the gold piling up; Company Level reaches 3 in the first hour where the model's reaches 5. Both are phase B's
economy (levels per copy with goods, Company Level from gold earned, the Monuments).

Not in phase A: the Yard's workshops and Depot, Monuments (phase B), art for the town gates (a paved apron for now),
towns in later Eras greyed out on the map (per-town Eras come with phase B).

**Phase B · The economy** (2026-10-25, ec3264f …; the test world starts again: rules 5):
- The Company Level comes from all the gold ever earned (§14: at the market, from players, orders and rewards), with
  where it came from in the top bar's panel; XP, its log and its rules are gone.
- Levels to 25 priced per copy (§9): every copy costs 7% more than the last and its levels cost from it, 15% more each;
  from level 11 a step also takes goods worth 30% of its gold, which come like a scaffold's materials (Storages, own
  spare, the Yard, the Depot, then the market at 2.5× within about ten minutes, which a switch in the trade settings
  turns off); the building works at its old level until they are in, "Buy the rest" brings them at once, Undo takes
  the waiting steps back. Max, Grow and the advisor count the goods.
- The market (§8): a fixed Hub Order per good and player (1,000 g a minute of an Era-1 good, ×4 per Era) raised by the
  trading post's levels, research, the Wonder, Trader Lin and the Market Monument; the square-root curve over the
  hour's average; later-Era goods pay a quarter; trading posts have no unit limit. The Exchange opens at Level 12 with
  your own price between the market's two and a 4% fee.
- Pipes carry per building pair: 100 / 5,000 / 50,000 / 500k / 5M / no limit, six tiers (the composite pipe is new);
  Widen is gone.
- Research (§12): two lines in the Lab, each with a queue of five and Keep researching: 21 tracks of nine tiers
  (faster, leaner, complexes, mega complexes, master; from tier 3 the track's own good, from tier 5 bigger Hub Orders;
  Power at half strength; a Coal Pit follows the better of Copper and Stone), and the Era tree that opens pipes,
  buildings and systems (Copper Pipes for everyone at Level 8). Research points, Labs that eat ingots and mastery
  are gone.
- Per-town Eras (§11, §13): each town has its Era and its Era Wonder at a fixed size (the Great Dynamo 4.3 billion g of
  goods, smaller for each town that built it before); give switches per good (beyond my Hub Order, all), give from
  your Storages and the Depot, buy and give at 2.5×; a full stage stands a day later and pays Bronze to Diamond in
  hours of income; perks per stage; the last brings the next Era with a day's +10% launch party. A building of a
  later Era than its town sleeps; you may move to towns of your Era or one below. The season and the finale are gone.
- Monuments (§13): one site per Era in each Yard, five kinds, endless stages 40% dearer each, caps and perks, paid in
  gold from Level 5 by any resident.
- The outskirts (§5): nine 8×8 parcels per plot from Era 2 with Survey the Outskirts, 100M × 1.4ⁿ, built and piped like
  the plot, bought back at half on a move; Grow, the advisor and the bots use them.
- The Yard's ends (§7): from Level 10 each resident builds producers and Storages on up to 64 tiles there, joined to
  its own Yard gate without pipes; the crew's Depot keeps goods for the neighbours' levels and buildings (at the
  depositor's crew price) and for the Wonder; a move takes the workshops and your Depot goods along.
- Retired with it: contracts, corporations, TradeWith, hub reputation, the Showroom, Exchange Office, Bank, Pneumatic
  Post, Assembly Plant and engines, district Wonders, the EraLaunch event.
- The frontend follows: the research panel's two lines, Hub › Town & Wonder (give switches, buy and give, tiers,
  Monuments), the market's Hub Orders, the Exchange at your own price, level waits with Buy the rest, the copy price
  and track on a building, the Depot in the Yard panel, outskirts on the map with their buy panel, the Company Level
  bar from gold earned. Art: the six pipe tiers, and Codex drew the Bauxite Pit, the Aluminium Works, the Yard Depot
  and the Motor, Battery, Titanium and Spaceframe Works for phase C.

Checks: game tests (LevelFacts, ResearchFacts, WonderFacts, OutskirtsFacts, WorkshopFacts new), server tests,
`e2e/economy.mjs` (the Level from gold earned, a Lab's two lines, Hub Orders, a give switch, a level that waits for its
goods, a workshop and the Depot, an outskirts parcel bought, a Monument's first stage in the Yard's square).

The bots against the model (§17): the advisor now values a good its network doesn't make at what the market's Hub Order
still takes, offers more extractors while their goods' Hub Orders have room, and counts materials the market brings in
a building's price; the bot buys outskirts, tries the next step when one fails and takes a few steps per list.
Big plots: wants are filled from the Storages once a settle (they scanned every building for every want, every tick)
and the solver's sharing loop allocates nothing per consumer.

**Phase C · Eras 3 to 5** (started 2026-10-25):
- Art: the Motor, Battery, Titanium (mine and works) and Spaceframe Works (Codex, processed and fit-checked); the five
  Monuments in three sizes each, standing on five sites in the Yard's square as their stages grow.
- The pipe's three lanes (§9): where a pipe meets a building, a manifold and one connector per kind of good (a belt, a
  slim pipe with a valve wheel, cables over a post), rendered pieces in the tier's trim; the pellets ride their lane.
  A Storage or a trading post shows the kinds moving through it.
- The wilderness (§20): fog over what your expeditions haven't mapped (1, 3 or 8 pieces a trip); outposts bought once
  with the Era-4 research, only on mapped land; the Deep Level stays at Era 3.
- Trunk lines (§20): from Era 3 to a neighbouring town, the right of way bought once, the builder its carrier with
  public, crew and special fees; links between towns need one and pay its fee; a good can be sent to the other town's
  market against half its Hub Order there; the Lines track makes lines cheaper and lose less, its master builds
  superconductor lines. On the map, and in the Links tab.
- The Launch and missions (§13): the Space Elevator launches (titles, Legacy written to every player's account) and
  missions follow without end, each a star and +1% production for the town; the race (Era, Launch, stars) on the map
  and in the picker.
- Loady opens a chapter for each Era (waiting until the town gets there).

### After v1

Designed (GDD §11), not needed for a fun first version: the Bank (investing, insurance, Wonder bonds), District
Steward elections, weekly challenges, the Public Quantum Gate, Estate of the Week, chapter themes, running a cargo
company for others, legendary skins from events.

## Design refinements made during implementation

Changes to [GameDesign.md](GameDesign.md) that came up while building; the design document is updated with them.

| Date | Refinement | Reason |
| --- | --- | --- |
| 2026-10-18 | Buildings only connect through pipes (not by touching each other); resources never pass through a building | Original rule, keeps layouts meaningful |
| 2026-10-23 | "Same device" for the money's guards and keystones is a shared browser id; the same IP address the same day only keeps a project's basket from growing | Friends playing in one room share an IP: they traded at the hub price and couldn't send or order for each other |
| 2026-10-23 | Copper Pipes come with Company Level 8 and Steel Pipes with Era 2 for everyone (a Lab's research still gets them sooner) | Run 4: every pipe stayed grey for 36 days, five of seven players never researched, and the pipe tier is the real level cap |
| 2026-10-23 | One order rule for every shared goal: while a player near makes a good, half comes by pipe; while nobody does, gold orders all of it at twice the price | Run 4: the Era Wonder stopped at its pipe half twice, and the order button vanished without a word |
| 2026-10-23 | After the Landmark, storeys without end: the Landmark's basket again and a purse of a day of the station's income ×1.2 a storey, given in hours of each giver's income, +10% on the residents' pipes each (to +50%) | Run 4: from day 17 the crew had nothing to build together (fun 2.83, "come back" 2 of 6) |
| 2026-10-23 | A keystone is chosen when its stage opens and waits for everyone who played this week (it was three days) | Run 4: a two-friend keystone was chosen and dropped twice while the Foundry Hall said "opens in 10 days" |
| 2026-10-23 | The Exchange shows its one player price before the click and tells an order's owner when it closes | Run 4: a typed 3 g became 1.95 g, and an unfilled order vanished and was read as filled |
| 2026-10-23 | From Level 22 the XP curve grows to 4× by Level 30 (was 2×) | Spent gold now raises income (storeys, the Wonder, Grow everything), and XP comes from income: the advisor's light bots reached Level 30 on day 14 at 2×, on days 20–25 at 4× |
| 2026-10-23 | Max on a Trading Post buys the levels its buildings keep 90% busy, and its panel shows what a level brings | The light bots: Max priced a post at every level the bank could buy, so Grow everything left a full post out and Mia's 5-minute level waited to day 28 |
| 2026-10-23 | A step that pays itself back within the hour ranks above a gift to a shared goal | The light bots: the storey's "give an hour" always came first, and a quick upgrade never got its turn |
| 2026-10-18 | Simulation by district ticks with cached flows instead of a pure event queue | Simpler, same behaviour |
| 2026-10-18 | Pipes may be laid on unbought parcels of your own estate | Gates are on the outer parcels; trading with neighbours (Level 3) must not wait for Level 25 parcels |
| 2026-10-18 | Shares within a tier are water-filled: same fraction of need for everyone, unused shares go to others | Proportional shares left energy unused when one consumer was limited by another input |
| 2026-10-18 | Storages and trading posts only serve their owner (in the original game a storage bought from anyone) | Your storage should never pay for a neighbour's surplus |
| 2026-10-18 | Smoke, rotors and machine animations only run while a building works; idle buildings show "zz" and the panel says why | Players see at a glance what is not connected |
| 2026-10-18 | A valve with no resources chosen lets everything through | A new valve should not silently cut a working pipe |
| 2026-10-18 | Priority valve: consumers whose pipe path crosses it are served first (within your own company) | Simple to understand, works in both directions |
| 2026-10-18 | Pump stations: the first 50 buildings of a network are free, every pump station adds 50; above that everything runs proportionally slower | No hard failure, a gentle push to use pumps |
| 2026-10-18 | Big Factory, Steel Mill, Wire Mill, Silo, Showroom, Refinery, Circuit Fab, Assembly, Robots and Superconductor Lab need their tech (as in the GDD tree) | The research tree unlocks them |
| 2026-10-18 | Hub prices rise at most 25% for scarce resources (GDD: up to 4×) and drop to a quarter at most; changes take effect when they move more than 1% | Early income stays close to the balanced values; networks are not solved again for tiny changes |
| 2026-10-18 | Contract and corporation gates carry the pipe but only allow the matching trades; only closed gates cut it | A gate policy should not split your own networks |
| 2026-10-18 | Toll: the sender of a flow pays, on the trade or hub value of what passes | One simple rule for trade, sales and own flows over someone else's public pipe |
| 2026-10-18 | Contract stars: 1 + 4 × score (score = running average of delivered share), rounded down | Five stars only for (nearly) everything delivered |
| 2026-10-18 | Exchange fee 5%, all of it removed from the game for now | Cargo depots that deliver for others (GDD §11.5) come with the late game |
| 2026-10-18 | Eight NPC tenants per district on the estates players get last; tenants accept fair contract offers for what they make or use | A new district is never empty and offers trade partners from day one |
| 2026-10-18 | Corporation perks: 1 chat and trade between members whatever the buyer's maximum price, 3 no tolls between members, 5 techs 20% cheaper for members (instead of one shared tech a week), 6–10 the production perk grows 1% per level | Each perk is felt every day and is easy to explain |
| 2026-10-18 | HQ upgrades cost 20k × 3^(level − 1) gold from the treasury; the corporation tax is at most 10% of members' trade income | A long goal for a corporation without taxing members out of the game |
| 2026-10-18 | The corporation quarter shows the four corporations with the highest HQ; the HQ tower grows a floor per level and carries the tag on a roof sign | A district can have more corporations than lots; the best ones are visible to everyone |
| 2026-10-18 | Likes give 20 × level gold and 10 XP; mentors (Level 30+) take up to five newcomers (below Level 10), both get 1,000 × level gold at 10, 15 and 20 | Small, friendly rewards that can't be farmed |
| 2026-10-18 | Chat: 300 characters, at most 6 messages per 20 seconds; blocked companies are hidden in channels and can't write you directly; reported messages are flagged for moderation (M6) | Basic safety from day one |
| 2026-10-18 | The offline report shows after 15 minutes away; every notable event also lands in the inbox (the newest 200 are kept) | Nothing important is missed, nothing piles up forever |
| 2026-10-18 | Events are planned per district four days ahead from the world seed; a storm damages each wind building with 10% chance (5% with a Weather Station), a repair costs 10% of the building, an Engineer's Workshop repairs for free when the storm ends | Predictable for players who plan, cheap enough that a storm is an event and not a punishment |
| 2026-10-18 | The Capital (the first district) has the Era Wonder on its own site next to its District Wonder; Wonder stages scale with the number of active companies (at least 3, base 8) when they begin; deliveries count as earnings for XP | Every district has its own Wonder, also the Capital; Wonders fit small and large servers |
| 2026-10-18 | Era Wonder tiers by share of everything delivered: Bronze any part, Silver 1%, Gold 4%, Diamond 10%; rewards 2k/10k/40k/100k gold × Era and a title. An Era lasts at most 21 days | Everyone who helps gets something, the big builders a lot |
| 2026-10-18 | Power line pylons reach 8 tiles to the owner's next pylon, carry only energy, without losses and over water | Long distance energy in Era 2 without losses, but pipes stay needed for goods |
| 2026-10-18 | Managers are hired once (no wages) and can be dismissed to free a slot; their automations run with the market update every 15 minutes (Bea sells 5% of a full storage's capacity, Rick buys one level that pays back within 2 h while keeping 3× its cost) | Simple to understand, never drains a company that is away |
| 2026-10-18 | Achievements are checked once a minute; collections need every building of a list at level 10 and unlock a golden decoration | Cheap to check, clear goals for veterans |
| 2026-10-18 | An outpost needs at least 4 rich tiles (3 stars or more); the claim covering a clicked tile is chosen for the most rich tiles; the lease is 3,000 × Era gold a week, paid automatically, and an unpaid lease gives the land back (buildings demolished at half their cost) | Fair, simple, no surprise costs that pile up |
| 2026-10-18 | Cargo Depots connect all networks of their owner (estate and outposts) like a lossless pipe of 200 per minute per level, instead of shipping in batches | The same flow model everywhere; trucks are a stronger but expensive alternative to a long, lossy pipe |
| 2026-10-18 | Anyone may lay pipes through the wilderness, except in someone else's outpost | Outposts need a way home |
| 2026-10-18 | Contracts run at most 7 days; Futures allows 30. Global Exchange orders only trade with other global orders (of any hub) | Gives the Era 2–3 economy techs a clear, felt effect |
| 2026-10-18 | A franchise resets the Company Level too (parcels stay); a quarter of the research stays: the cheapest techs | Back to a fast early game with a head start, as the design intends |
| 2026-10-18 | Legacy points = √(net worth / 1000) + 10 per achievement + 5 per title + 20 per franchise + up to 100 for the top 10; Legacy level = √(points / 10); +500 starting gold per level (max 10,000) | Rewards a whole season of play, but a newcomer can still catch up |
| 2026-10-18 | The next world (new seed) opens a week after the season ends; chat, inbox and guestbooks start fresh with it | A clean start for everyone; accounts and Legacy stay |
| 2026-10-18 | A move costs 1,000 × Company Level gold (free moves after waking up or from a quiet district); a sleeping estate's public pipes become toll-free hub pipes; waking up gives 1,000 × level gold, 500 × level research points and a free move | Coming back feels like a gift; nobody is stuck next to an empty estate |
| 2026-10-18 | Quiet districts are not folded into the wilderness (estates stay for tenants and newcomers); their active players get the free move with a parcel | Same effect for players, no risk of breaking the map |
| 2026-10-18 | Moderators are users with IsAdmin; moderation is checked against the database on every call, so a ban or a lost role works right away | Simple and safe; a full moderator role system can come later |
| 2026-10-18 | Complexes form by themselves when the research is there and four (nine) finished identical 1×1 producers stand in a square; the complex takes the lowest level of its members and can't be moved (demolish refunds all members) | Clear and automatic: a complex is always better than its members |
| 2026-10-18 | Terraforming is done tile by tile with costs per action; seeding a deposit takes 20,000 gold and 50 steel and adds one star (up to five) | A big, visible gold sink, as the design intends |
| 2026-10-18 | Shortage orders are a fourth, urgent order (twice the size, three times the reward per unit) that disappears when the shortage ends; festivals fall on fixed holidays | They add to the daily rhythm without replacing it |
| 2026-10-18 | Blueprints need a finished Blueprint Office to save, share and build; a franchised company can rebuild its own blueprints without one. Building a blueprint places what fits and is unlocked (and what you can pay for); a complex is saved as its members; better pipes than you have become the best you have | Blueprints carry over into a Franchise (GDD §11.7) without making the office pointless |
| 2026-10-18 | Blueprints belong to the season's world like everything else and go with the reset | Blueprint owners are companies, which don't survive the season |
| 2026-10-18 | A new Era launch ends the running one, and identical toasts are merged with a count | Found while testing blueprints: a fast Era change and a finished blueprint flooded the screen |
| 2026-10-18 | Deep tiles are surface tiles shifted down by 969,600 (whole districts and chunks): the same tables, chunks, networks and renderer serve both layers. The dug out parcels belong to the company (a bit mask) instead of the land, so they move along with it | The tech design's "layer index" without a second set of tables |
| 2026-10-18 | A Deep Lift is a pair: the lift on the surface and a shaft below, linked like Cargo Depots; the shaft can't be demolished, moved or upgraded on its own | One link mechanism for pylons, depots and lifts |
| 2026-10-18 | Factories stay on the surface; the Deep Level is for mining, magma energy, storage and decoration | Keeps the lifts the bottleneck and the surface the factory floor |
| 2026-10-19 | The Franchise opens a new estate instead of resetting the old one, and the world persists over chapters instead of a season wipe | The owner prefers expansion over rebuilding; to be built in M8 |
| 2026-10-19 | Looks are a primary goal: generated art (Codex, `gpt-6.1-sol` high) for buildings, map and UI; v1 scope is a fun, good-looking multiplayer idle game, the rest after v1 | Owner's direction after the image test |
| 2026-10-21 | Playing together (v1.2 design): places, people and trade are separate things (stations, crews, the Yard and links); one price between players; no votes, kicks or releases; NPCs labelled and capped at 25% | Six design agents in five rounds, on the owner's principles: fun with three friends, choosing where you live, lonely players, players who stop |
| 2026-10-21 | No frozen districts (v1.2 §11): a quiet district already costs almost nothing (its Yard's rounds come every 16 minutes, its networks are never solved again), and running it in quarter-hour steps changed what players earn (the Hub Order follows a one-hour average of sales: a lump of sales keeps the price higher, about 12% in a test) | Being away must never pay better than playing |
| 2026-10-21 | The Landmark is a basket like the other stages (half the delivering residents' summed T(L) for 6 hours), not yet a sustained flow with its own tank | The flow needs a tank and pause state of its own; the basket gives the same size and the heirloom |
| 2026-10-21 | A starter kit is at least three producers that need nothing (the House's own where it had them, else what suits the land) | A works House's sawmills, without wood, were no gift |
| 2026-10-21 | "Find me neighbours" prefers stations of four to small ones; the project takes from every resident in the same share | Small stations give fewer partners; first come, first served left later friends out of the ring of colours |
| 2026-10-21 | A sleeper without a crew keeps a stand-in only until its seat's hold ends on day 37; a House then warms the seat and trades with the partners | The stand-in needs the seat to trade in its Yard |
| 2026-10-21 | No e-mail, no stills of the estate, no Era jump kit, no "Back!" ribbon in this version | The in-game way back covers the first return; these come when testers ask |
| 2026-10-21 | Houses buy at most a quarter of what a player could make (its buildings that have all they need, at full speed), not of what it made last round | Piped to its gate only, an estate's buildings make only what is taken: a quarter of that shrank every round to nothing (found by the balance run) |
| 2026-10-21 | The starter kit is piped to the trading post as well as the Yard gate | Most of the kit's output found no buyer: the Houses take a quarter, the hub the rest |
| 2026-10-21 | Loady's build and pipe steps count only what the player builds (`piped:` for the first sale), never the starter kit | The kit did the first three steps, so a newcomer never placed or piped anything before the Iron Mine |
