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
| Art | Existing hand-drawn sprites where they exist; a procedural `SpriteKit` (canvas drawing in the same outlined, flat-coloured style) for all new buildings and terrain | Codex image generation is not logged in; procedural art keeps 60 buildings consistent and animated |

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

- ⬜ Corporations (treasury, HQ levels and perks, roles, weekly goals)
- ⬜ Chat (district, corporation, direct)
- ⬜ Visiting, likes, guestbook, mentors
- ⬜ Notifications and inbox, offline report v2
- ⬜ Leaderboards

### M5 · Living world

- ⬜ Events and calendar, Weather Station
- ⬜ District Wonders and the server Wonder, Eras 1–2 with Era 2 content
- ⬜ Managers, achievements and collections
- ⬜ Outposts and the wilderness

### M6 · Full season

- ⬜ Eras 3–5 content, Era deadlines
- ⬜ Franchise (Shares), season finale, Legacy
- ⬜ Activity stages, Caretaker, Quiet Isles, relocation, crates, swaps, corporation blocks, district merges
- ⬜ Mobile layout polish, moderation tools

### M7 · Late game

- ⬜ Complexes, blueprints, terraforming
- ⬜ The Deep Level
- ⬜ Expeditions, branch estates, teleporters
- ⬜ Bank (investing, insurance, bonds), District Steward elections
- ⬜ Weekly challenges, season themes, Loady, photo mode, mastery research

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
