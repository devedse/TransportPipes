# After run 2: the analyst's twelve recommendations (2026-10-22)

Run 2 (`playtest/runs/story2`, report `report.html`) ended with twelve recommendations. The owner: "follow the
experts". What was built, in the analyst's order, each pushed on `feature/playing-together` with its tests:

| # | Recommendation | What changed | Commit |
| --- | --- | --- | --- |
| 1 | A stage stands only by delivery | A leaver takes their part of what's still missing; the last one counted never takes the rest; nobody active: the stage waits. This week names each wanted good and who makes it now | 00ee609, 1f46607 |
| 2 | Name every gift at both ends | The seller's card: "ana_pm Pipes' Quarry bought 1,190 of your energy · +1,190 g" (and once: the Yard sells your spare by itself); the receiver's card: "Ben's sand ran your Glassworks: 138.6k of it · 💛 Thanks"; the network card counts the Yard gate | 1f46607 |
| 3 | Make Send move something | Bids only where your goods reach the gate and the friend lacks half a building's worth; each Send shows what it moved this week and why it moves nothing | 1ae1231 |
| 4 | One tap lays the pipe; own needs first | Fix buttons and Next steps lay their pipe on the first tap; a stuck producer pipes to your own short building before a neighbour's Send | 83e201c |
| 5 | The middle game's chores explain themselves | Orders show what your Storages hold, "Build a Storage", Deliver's result inline; a full Storage names what it holds and who waits, with one pipe; the Build menu's supply line; a Lab is suggested only once its ingots are made | c96f6c5, 3f09f7c |
| 6 | Long shortages and damage in sight | Repair all stays in the goal tracker; the card names a building under 60% and a friend's damage with 💛 Tend | 6c81232 |
| 7 | Friends' trade in the content | The light bots tried both levers (ingots need coal; half the ingots' Hub Order): 0.7–2.2%, neither ships (`light-bots.md`) | 0ec502d |
| 8 | A lone player's land never waits | A keystone nobody brings for two days is passed over; "makes" only when someone does; "a player" in the Yard | 183ce41 |
| 9 | The factory-on-the-pipe rule | A building may stand on its owner's own pipe, which runs on under it | d025b82 |
| 10 | The bank and the Lab | Keep researching while away (on by default); the Invest line sized to the bank | 497ffc8 |
| 11 | The small breakers | Link offers wait for the next session; slowed-taker text; Escape; Show me says what's in hand (rimmed tile, then an arrow); Next steps hold still; which building; Deposits labels; a pipe in an unpiped building's bubble; and more | 04a5946, 3f09f7c |
| 12 | Replays | Below | |

Found while fixing, in the playtest itself: every pair of AI players counted as one device (one machine's IP on one
real day), so run 2 priced every trade between them at the hub's. Each player now has an address of its own
(5a423bc); the run 2 report was corrected and shared again.

## The replays (from run 2's snapshots, on the new build)

- **Scripted, day 10 → 15 with nobody playing** (`runs/replay2-stages`): the crew's Workshop stood on day 11 by real
  deliveries; its Foundry Hall then waited with its whole basket to day 15. In run 2 it stood on day 14 with nothing
  delivered.
- **Ana's day-8 evening** (`runs/replay2-ana8`): clarity 4, fun 4 (run 2: 3, 3). Her card named the sale of her
  ingots and Ben's energy for her Quarry; she delivered a hub order from her Storage (+5,900 g, clearing the full
  warning), built a Lab beside its ingot maker, and kept researching. Two things she missed led to the arrow over Show
  me's tile and the supply line's cost in sales (3f09f7c). This replay ran on a server from before tasks 8–11.
- **Dev's day-21 evening** (`runs/replay2-dev21`, the full build): clarity 3, fun 4. He pressed 💛 Thanks for Ana's
  ingots, the first Thanks in the playtest (run 2: none in 36 days), repaired, upgraded (288 → 486 g/min) and queued
  research. Open: he didn't find Copper Pipes in the research tree, and Evolve into Warehouse didn't appear after a
  Storage upgrade.

## Open for run 3

- Friends' paid trade: the light bots show the iron chain isn't the lever; chains that need a neighbour must beat a raw
  producer at the hub (or the hub must pay less for raw goods past a point). A designer's decision.
- Run 3 on a fresh world, with the corrected device addresses, to measure the act marks again.
