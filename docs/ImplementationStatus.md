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
- ⬜ v1.1 · painting rows and areas with the build tool
- ⬜ v1.1 · screen rules: a big window over the inspector without closing it, the phone check
- ⬜ Follow-ups seen while building v1.1: in a network of 19 factories the solver leaves some ore and energy to the
  hub while factories run short (each producer–consumer pair has its own pipe capacity); following the advisor the
  first days go about a day faster than the balance plan (run `TheFirstTwoWeeks` with an advisor profile)
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
- ⬜ Merge PR #3 and release

### After v1

Designed (GDD §11), not needed for a fun first version: the Bank (investing, insurance, Wonder bonds), District
Steward elections, weekly challenges, the Public Quantum Gate, Estate of the Week, chapter themes, running a cargo
company for others, legendary skins from events.

## Design refinements made during implementation

Changes to [GameDesign.md](GameDesign.md) that came up while building; the design document is updated with them.

| Date | Refinement | Reason |
| --- | --- | --- |
| 2026-10-18 | Buildings only connect through pipes (not by touching each other); resources never pass through a building | Original rule, keeps layouts meaningful |
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
