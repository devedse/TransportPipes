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

- 🚧 `TransportPipes.Game`: content (YAML), world layout (districts, estates, corridors, gates, parcels), terrain and deposits
- ⬜ Flow solver with the flow order, storage, trading post, losses and throughput
- ⬜ World state: buildings (1×1/2×2/3×3), pipes, networks, construction, levels and milestones, parcels, Company Level and XP, ticks
- ⬜ Persistence, cookie accounts, company creation, estate assignment
- ⬜ SignalR protocol v2 (district groups, chunk deltas, flows)
- ⬜ Frontend v2: estate/district rendering, SpriteKit, build and pipe tools, HUD, building panel, parcels, flow animation, offline report
- ⬜ Remove the old game code (Actionizer, Calculators, old schema), port the calculation scenarios to the new solver

### M2 · Industry

- ⬜ All Era 1 resources and buildings with art
- ⬜ Research and Labs, research tree UI, knowledge diffusion
- ⬜ Pipe tiers, losses, throughput, valves, priority valves, pump stations
- ⬜ Network inspector, overlays (flow, ownership, losses, climate, deposits)

### M3 · Economy

- ⬜ Hub markets with dynamic prices
- ⬜ Player prices, fair band, buyer maximum price, gate policies, public pipes, toll meters
- ⬜ Contracts, reputation
- ⬜ Exchange and cargo
- ⬜ Daily orders, hub reputation
- ⬜ NPC tenants, Neighbourhood panel

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
