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
| 2026-10-18 | Shortage orders are a fourth, urgent order (twice the size, three times the reward per unit) that disappears when the shortage ends; festivals fall on fixed holidays | They add to the daily rhythm without replacing it |
