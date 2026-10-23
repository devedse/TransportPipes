# Round 3 · engineer

I read the draft plan and the four other r2 papers. I accept the plan's shape and its order. Before code I need these
fixed: one item is wrong as written, and two items rely on things that are scheduled to land after them. The
one that is wrong is my own r2 answer, which the plan took over: the floor on the seller's side would pay twice. I take
that back here, with the evidence.

## Verdict per item

- **F0 · accept, amend.** `idleCause` needs `IdleReason`. So F0 ships `yardPaid`, `yardMatched`, the dialog count and
  the share of new gold from NPCs first, and `idleCause` lands with the `IdleReason` engine (commit 3 below).
- **F1.1 · object, and amend: the floor is settled on the buyer's side, for goods the Yard matched, and only those.**
  - On the seller's side it would pay twice. The project (Yards.cs 626), Asks (Asks.cs 199) and trunks (Trunks.cs 171)
    all add to the same `terms.Export` as the Yard. All of it leaves through the same gate buyer node. Each of them
    already pays the seller itself: `SettleProjects` pays base × units (Projects.cs 355), `SettleAsks` the player price
    (Asks.cs 249), `SettleTrunks` the remote price (Trunks.cs 220). If `SplitGateFlows` keeps hub value "for every
    unit that reaches its gate", every project, Ask and trunk unit is paid that hub value a second time.
  - Fix: in `SettleGateFlows`, per (estate, resource, price class), untaken = what the Yard matched there (kept apart
    from link imports, which have their own tank) − what the buyer's buildings took. Each source seller gets its share
    at its own `HubBuyPrice`.
  - It is booked as a hub sale: Report.Sold, plus the seller's Hub Order through `AddSold`. That answers economy's r2
    point, and now it can be booked, because the untaken part is known exactly.
  - It is capped by the seller's hub spare: the spare measured from its sale flows to the hub, stored in its gate
    terms at matching. The plan says "bounded because spare is measured from what already goes to the hub". That stops
    being true once F1.4 counts storage-bound and idle capacity as spare, so the cap has to be written into the code.
  - Cost: one round's loss when a buyer leaves the station between rounds (at most 16 minutes, rare). Accepted.
- **F1.2 · accept, amend the order.** Its output cap needs `IdleReason`, so the `IdleReason` engine (game side only)
  moves from F2 into F1. Also: the station's spare counts for an input only when that input reaches the building
  through its gate link, and only up to what the link can still carry.
- **F1.3 · accept.** "A small probe" means max(1/min, 5% of what was taken).
- **F1.4 · accept**, with the floor's cap above.
- **F1.5 · accept.** The trading post goes outside the newcomer's HQ area (`FindTenantSpot` already keeps out of it).
  It is not a producer, so it is never part of a starter kit (Tenants.cs 383). Existing Houses get theirs in
  `EnsureTenants`.
- **F1.6 · accept.**
- **F1 tests · amend.** Replace "`SolverSpeedFacts` within budget": a wall-clock test is flaky in CI, and no extra solve
  is left in live code. Instead, `YardFacts.AYardRoundSolvesEachNetworkAtMostOnce` counts the solves. Add
  `YardFacts.TheFloorNeverPaysTheProjectAsksOrTrunksTwice`.
- **F2 · accept, amend.**
  - "Stuck" is decided from links, never from flows: no own Storage or post on its links, and no linked building that
    takes the good. "Taker stuck" means every linked taker is itself stuck. Replace the plan's "no running taker",
    which would flicker.
  - The planner is not `LayTenantPipe` as it stands. That one ignores the player's locked parcels and keeps out of
    the HQ area, where the trading post stands (Tenants.cs `Free`, `intoHqArea`). The dry run uses the player's own
    pipe rules.
  - The button's "Yard gate" variant only has reasons once F1 (true lacks) and F4 (recipes) are in.
- **F3 · accept, amend two things.**
  - The kit counts **from step 3**. `piped:` counts kit-free buildings too (Tutorial.cs 26). "Every step but the first"
    would finish the pipe lesson for every kit holder, and that is the lesson Ben needed (ben d03-2000 #60). Clarity's r2
    already agreed to step 3.
  - "Upgrade steps may offer a smart-Max level" needs F8's `UsefulLevels`, so the engine lands first (commit 8 before 9).
- **F4 · accept, amend two things.**
  - The offer capped by the allowance is the allowance's daily rate, not what is left ÷ 60 (Projects.cs 300). Otherwise
    the 18-hour tail comes back through the allowance.
  - I(c) is kept in `CompanySettings`, the JSON the world store already saves (WorldStore.cs 71, 158), so it needs no
    migration. It starts at load as max(60, the current paid gold rate). T(L) also leaves `Wonders.cs` 120.
- **F5 · accept.** The card replaces the report for every return, short ones too. Repair all is a new command, a loop
  over `Repair`.
- **F6 · accept.**
  - Attributing a building's gate export to named buyers is a proportional split of `Matched`, fine.
  - The neighbour-fed `ChainSteps` needs a what-if with a gate source node: M on its own, so a separate commit.
  - `firstYard` needs its own stat. Today `Handshakes` and Loady share `yard` (Yards.cs 693).
- **F7 · accept.**
- **F8 · accept.** Cache the what-ifs per network solve (≤ 7 per panel read).
- **F9 · amend.** The stand-in copies what partners *took* from the sleeper at the moment it falls asleep, read from
  their import gate flows. Today it copies `Delivered` at the sleeper's gate (StandIns.cs 47–56), which is what was
  matched. That is S, and it fixes the observed fault (a false match frozen for 14 days). The 14-day median is M, for a
  fault nobody saw (a quiet minute). Do it only if run 2 shows that hole.
- **F10 · accept.** "Something is in the way" is a client hint on the tile that was just built on. Hide it until the
  pointer moves.
- **F11 · accept.** The queue keeps `CurrentResearch` and adds a list in `CompanySettings`, so no migration.
- **Dropped list · accept.**

On the plan's rule "every database change is an additive migration": as far as I can see, none is needed. Companies,
projects, stand-ins and crews are JSON in existing columns (WorldStore.cs 71–161, 296–310), so the snapshots load as
they are.

## VOTE A

**As proposed** ("Show me" on the best upgrade, smart-Max level preselected, no one-tap spend). A one-tap spend on the
card is an unseen spend of half a bank before the player has looked at the estate, the exact shape of Cleo's loss. It
would also need smart-Max what-ifs for every building inside `Hello`.

## My last dissent

**The floor must be settled on the buyer's side, for Yard matches only, capped by the seller's hub spare (F1.1 above).**
This is not taste. As drafted, it pays every project, Ask and trunk unit twice, and its only bound vanishes with F1.4.
I proposed the seller's side in r2; I was wrong, and the plan inherited it.

## Top 8

1. **F1, the Yard is honest** (with the floor amended). Real harm, and every later screen shows its numbers.
2. **F2, dead ends explain themselves.** The most pain and the surest change of behaviour.
3. **F3, Next steps for everyone, and Loady follows the estate**, shipped with F2. The gate that hid the advisor from 4
   of 5 players is one line.
4. **F4, a project the station can reach.** The only shared buyer in week 1.
5. **F5, one return card.** All five players, every return.
6. **F6, trade with a face.** Turns 1 and 4 into giving on purpose.
7. **F7, the seat picker.** A STUCK in a stranger's first minute.
8. **F8, smart Max.** One case, but 1.53M, and cheap.

F10's "+0/min" fix is ranked below these but ships early (commit 2), because it is S and on its own.

## Appendix: the build

One commit = one step on `feature/playing-together`. Each one passes `dotnet test` for the Game and Server tests and
`npm run lint`, `format:check`, `typecheck` and `test`, and is playable on the test server. "→" means "must land
before". Times are mine: S ≤ 2 h, M ≤ a day, L more.

**1 · Instruments, the restage cheat, baseline replays (S–M).** No dependency.
- Files: GameWorld.Yards.cs (paid counters per company and resource, from `SettleGateFlows`); Playtest.cs (`yardPaid`,
  `yardMatched` = today's `neighbourIncome`, `npcGold`); GameHub.cs (a dialog-count hook, the `project restage`
  cheat); GameWorld.Projects.cs (`RestageNow`); playtest/digest.mjs; `e2e/replay-row10.mjs`, `-row07`, `-row01`,
  `-row14`.
- Test: `YardFacts.PaidYardGoldIsCountedApartFromTheMatch` (after 10 minutes, paid = gold moved, matched unchanged).
- The replays first record run 1's failures. That proves they catch them.
- Risk: none for players.

**2 · Rates read after the solve (S).** No dependency.
- `GoldPerMinute()` and `ResearchPerMinute()` call `SolveNetworks()` (Networks.cs 904–929).
- Test: `GameWorldFacts.TheIncomeIsRightStraightAfterAnUpgrade` (no tick between: the same rate as after a tick, > 0).
- Risk: a command pays for its own solve (the same work, earlier).

**3 · The floor, on the buyer's side (S).** → 5.
- `SettleGateFlows` gets the untaken rule. `GateTerms` gets `YardSources` and `HubSpare`. The paid part is booked in
  Report.Sold and the Hub Order.
- Tests:
  - `YardFacts.SellingThroughTheYardNeverPaysLessThanTheHub` (A's gold over 60 minutes next to B's level-35 Lab ≥ A's
    gold without B, −0.1%);
  - `YardFacts.TheFloorNeverPaysTheProjectAsksOrTrunksTwice` (a seller delivering to the project earns only the
    project's price for it);
  - `YardFacts.GoldIsConservedThroughTheYard` (sellers receive = buyers pay + hub price × untaken);
  - `YardFacts.TheFloorIsCappedByWhatWouldHaveGoneToTheHub`.
- Risk: the timing between the two networks' `Apply` calls. Settle by minutes, as `SettleGateFlows` does today.

**4 · The `IdleReason` engine (M).** → 5, 7, 9.
- A new GameWorld.Idle.cs: `IdleReason(building)` from links and node definitions (stuck, taker stuck with a visited
  set, full, input missing, input short, post full, pumps, damaged, building), cached per network solve.
- Also: Playtest.cs `idleSince` and `idleCause`.
- Tests:
  - `IdleFacts.AFactoryWhoseIngotsHaveNoTakerIsStuck` (Ana's day-4 layout);
  - `IdleFacts.AMoilyWhoseOnlyTakerIsStuckSaysWho` (Dev's);
  - `IdleFacts.AFullStorageIsFullNotStuck`;
  - `IdleFacts.StuckNeverDependsOnThisRoundsFlows` (the same answer with the solution emptied).
- Risk: big networks. It is O(links), with no solve.

**5 · Honest lacks and real spare (M).** After 3 and 4. → 6.
- `YardMembers` runs in two passes.
- `Measure` caps lacks by the output state, by the other inputs (own links + the station's spare through the gate
  link, up to the link's free capacity) and by the feedback (F1.3).
- Spare counts storage-bound output beyond `StorageWants`, and the idle capacity of output-limited buildings.
- `LinkSuggestions` (Links.cs 740) uses the same path.
- Tests:
  - `YardFacts.ABuyerHeldBackByAnotherInputIsMatchedOnlyWhatItCanUse` (Cleo's Lab: under 1 energy/min matched);
  - `YardFacts.ABuildingWhoseOutputIsStuckLacksNothing`;
  - `YardFacts.AStorageBoundProducerOffersItsSpare`;
  - `YardFacts.AMatchNobodyTakesShrinks`;
  - `YardFacts.TheLackFormulaAgreesWithAProbeSolve` (the dense-factory fixture and run 1's layouts, within 5%).
- Replay row10 + 6 h: Ben ≥ 70, Ana ≥ 200, Dev ≥ 120, Mia ≥ 90 g/min; real growth = hub + paid Yard, within 2%.
- Risk: contracts, links, Asks and the project read the same Spare and Lacks (Yards.cs 459–491, Links.cs 692–713). Run
  the whole suite and docs/balance's together run, and expect TenantFacts' numbers to move.

**6 · No dead Houses (S).** After 5.
- Tenants.cs `PlanFor` (55–57) and `EnsureTenants`.
- Tests: `TenantFacts.ANeighbourInNeedTakesOnlyWhatItNeeds` (energy only: nothing matched to it; with sand: taken, the
  seller paid the House price × units), `TenantFacts.AnOldNeighbourInNeedGetsItsPostAtStartup`.
- Risk: no free spot for the post. Log it, and the House stays as it is.

**7 · Paid figures on every screen (S).** After 3, 5, 6. → 11.
- `GoldPerMinute()` adds the gate settlement, project and Ask rates.
- `YardTrades` and `NeighbourIncome` (Tutorial.cs 55) read what was taken. The Yard panel follows.
- Tests: `YardFacts.TheTopBarCountsWhatNeighboursPay` (rate × 10 = the gold change ±1e-6),
  `YardFacts.TheYardPanelListsOnlyWhatArrives`.
- Risk: Loady's chapter-2 neighbour step gets harder (it was free before). That is right.

**8 · Smart Max (S–M).** After 2. → 9.
- GameWorld.cs: `UsefulLevels`, and `AffordableLevels` priced like `UpgradeCost` (911).
- The building details DTO, and BuildingPanel.tsx (prices and the choice row).
- Tests:
  - `GameWorldFacts.MaxStopsWhereTheNetworkCanFeedTheBuilding` (a Lab with one factory and 2M gold → level 9);
  - `GameWorldFacts.MaxPricesAComplexLikeTheUpgrade`;
  - `GameWorldFacts.MaxStopsWhereThePostIsFull`.
- Replay row07, Cleo #177.
- Risk: what-ifs per panel read. Cache them per solve.

**9 · Dead ends on screen, the Next steps for everyone, Loady (M+).** After 4 and 8.
- Server:
  - GameViews.cs `Issues` → `IdleReason` (the pill and the reach line);
  - Advisor.cs `NetworkNeeds` skips stuck buildings, the line-608 fallback goes, and so do `ShortageSteps` and
    `ValuesOf`;
  - `NetworkDetailsDto.stuck`;
  - `PlanFixPipe(building)`, a dry run with the player's pipe rules;
  - tutorial.yaml (`firstIngot` text, the kit flag from step 3, no levels in texts);
  - Tutorial.cs (26, 118, the `produced:` tip);
  - the advisor's effort term (140);
  - a `demolished:<type>` stat;
  - no duplicate titles.
- Client: BuildingPanel, NetworkSummary, GoalTracker.tsx 37 (Level ≥ 3), NextSteps.tsx 77 (keyed by building).
- Tests:
  - `AdvisorFacts.AFactoryWhoseIngotsCantLeaveIsNotShortOfEnergy` and `AProducerWhoseOnlyTakerIsStuckSaysSo`;
  - `PlanFixPipeFacts.TheRouteStaysOnUnlockedLandAndReachesThePost`;
  - `TutorialFacts.AStarterKitCountsFromStepThree`, `AStepWaitingForAnIngotSaysWhy`, `ChapterOneRetiresAtLevelTen`;
  - `AdvisorFacts.ADemolishedTypeIsNotSuggestedForAWeek`, `NoTwoStepsShareATitle`;
  - `e2e/deadend.mjs`, `guide.mjs`, `touch.mjs` (the button on a phone).
- Replays: row08 (Ana), row10 (Ana, Dev), Dev's d10.
- Risk: a false "stuck" (commit 4's test guards it), and clutter on a phone (the steps start collapsed).

**10 · A project the station can reach (L).** After 5 (true spare). → 11, 12.
- Projects.cs:
  - the 900-energy Foundation;
  - take all that's offered, done at 99.5% or with under 1 unit left;
  - the allowance at its daily rate, the keystone exempt;
  - the recipe from running output that reaches a gate, and a makeable keystone (`FindSpot` on owned or buyable
    parcels, unlocked by Level 5);
  - the size rule, shrinking, picking the keystone again.
- I(c) in `Apply`, stored in `CompanySettings`. T(L) leaves Projects.cs 251, 252, 330 and 370, and Wonders.cs 120.
- Client: ProjectTab (who can make it), GoalTracker's "This week" row (168–180), clickable.
- Tests:
  - `ProjectFacts.TheFoundationStandsInMinutesOnStarterKits`;
  - `EveryRecipeGoodIsMadeOrMakeable`;
  - `NoStageTail` (150 ore within 15 minutes of one mine);
  - `ABasketShrinksWhenACountedResidentStops`;
  - `TheKeystoneIsChosenAgainWhenItsMakerSleeps`;
  - `MeasuredIncomeFollowsPaidGold`.
- Replays: row01 (Foundation by 20:30, Mia's too), and row07 + restage + 8 days (Water Tower ≥ 50%).
- Risk: Wonder and Landmark sizes move, so run docs/balance again. Old baskets in the snapshots need `restage`.

**11 · One return card (M).** After 7 and 10.
- GameViews.cs `ReturnCard` (from `Return` and the report), GameHub.cs `Hello`, a new ReturnCard.tsx replacing
  ReportDialog.tsx and ReturnDialog pages 1–2, App.tsx (keys ignored while it's open), a `RepairAll` command, and
  `NewNeighbours` as a set (GameWorld.cs 397, Crews.cs 550).
- Tests:
  - `ReturnFacts.TheCardMeasuresTheAbsenceOnTheServerClock`;
  - `ANewNeighbourIsListedOnce`;
  - `TheCardNamesAFriendsStandIn`;
  - a vitest (one dialog, one button);
  - `return.mjs` (after 12 hours and after 10 days).
- Replays: row10 → day 27, and row14 on a phone.
- Risk: the Quiet Isles' landing choice must survive.

**12 · Trade with a face, part 1 (M).** After 7, 9 and 10.
- "Where it goes" gets named ends and the project.
- The first-gift event and Thanks.
- The privacy filter.
- "Who could use your X" with the planner aimed at the gate (commit 9's planner).
- `firstYard` gets its own stat.
- The "Neighbours" button and its first line.
- The seat card tells the truth.
- Tests: `YardFacts.AFirstGiftIsAnnouncedOnceToBothEnds`, `ATradePairIsShownOnlyToItsTwoEnds`,
  `TutorialFacts.TheKitDoesNotDoTheYardStep`, `e2e/yard.mjs`. Replay: row10, Dev's dredger to the gate.
- Risk: the event budget. Houses fold into one line.

**13 · Trade with a face, part 2: neighbour-fed chains (M).** After 12.
- `ChainSteps` with a what-if gate source at 1.75, and the receiver's card.
- Test: `AdvisorFacts.ANeighboursSpareIngotsSuggestALabOnTheGate`.
- Risk: what-if count. It shares the advisor's budget.

**14 · The seat picker (S).** No dependency, so it ships early: right after 2 is fine.
- StationPicker.tsx (79), FoundCompany.tsx (194, 240), GameHub.cs (the `seatTaken` code), Crews.cs `SuggestSeat`
  (699–705).
- Tests: a vitest, `CrewFacts.FriendsAndAStrangerAreSuggestedDifferentStations`,
  `CrewFacts.OwnStationPrefersFourSeats`, `seat-race.mjs`.

**15 · Friends by name, tending that repairs, stand-ins from what partners took (S–M).** After 7.
- Return.cs `Tend` (repairs the friend's storms for free), StandIns.cs `NewStandIn`, holds "For:", the crew's own spot
  in TopBar.tsx.
- Tests: `ReturnFacts.TendingRepairsAFriendsStormDamage`, `StandInFacts.AStandInCopiesWhatPartnersTook`.

**16 · Trust and the phone, the rest of F10 (S–M), then F11 (S each).** After 9 for the lit pipe groups.

Order on one line:

1 · 2 · 14 · 3 · 4 · 5 · 6 · 7 · 8 · 9 · 10 · 11 · 12 · 13 · 15 · 16

That is about 8–10 working days.

Replay gates:
- The row10 Yard marks before 7 ships.
- Ana's and Dev's dead-end replays before the Next steps go to everyone (9).
- row01 and row07 + restage before the project shows on the "This week" row (10).
- Run 2 after 12.
