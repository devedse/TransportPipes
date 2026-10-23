# Round 1 · engineer

I checked every root cause below in the code at `b5b5a83` and give the file and line. Where I infer, I say so. Each
item has a class: **bug** (the code does not do what it says), **design flaw** (it does what was designed, and the
design is wrong), **unclear rule** (it works, and players can't see why) or **artifact** (the playtest's doing).

## What I see

1. **The Yard takes goods its buyer can't use, and pays nobody. It happened twice, not once (bug).**
   - Cleo's Lab at level 35 needed 1,022 energy and 204 ingots a minute and got 30 and 6.1 (cleo d14-1900 #243). It
     could use about 0.6 more energy. The Yard matched it 361 (#235). The sellers were paid for the 0.6: 0.6 × 1.75 ≈
     1 g/min, split 194 : 95 : 72. Ben's share is 0.2 g/min, and his gold grew 0.1–0.3 g/min for eight days
     (metrics). Ana's gold growth fell from 197 to 33.5 g/min, which is what her top bar said.
   - **New: Mia lost the same way for 36 days.** Her "Yard" figure in metrics was 30–35 g/min in every sample. Her real
     gold growth equals her hub-only top-bar rate in nearly every 6-hour sample (day 5.6: 79.6 against 79.6, Yard
     34.9), so the Yard paid her nothing. "A station of my own" put her in Capital N, a *small* station of two seats
     (WorldLayout.cs 217–221). In the row07 snapshot the other seat's buildings (owner 8, at tiles 0,−31 to −4,−38)
     are a Glassworks House: a glassworks and two moilies, no trading post. That House can never run. So the Yard took
     up to a quarter of her energy, about 28 g/min of hub sales by my estimate, and nobody paid for it. Paid, she would
     have been at about 82% of Dev instead of 64% (99 against 154 g/min, row 11).
2. **The network card blames an input when the output is stuck (bug), and the rule behind it is hidden (unclear rule).**
   Ana's card read "Energy · 20 short · makes 179 · needs 30" and "Iron ore · 12 short" while both went to the hub
   (shot ana d04-2100-0122). The building panel's line was right: "Nothing on its pipes takes its iron ingot". Dev's
   moily read "makes 0 of 27" with no reason (dev d21-1900 quote). The next steps then sent players to build more
   moilies.
3. **The project can't be reached, by construction (design flaw).** The Water Tower's basket is exactly 6 hours of the
   typical income T(6) = 915 g/min for *one* player: 329.4k gold, split three ways (91.5k sand × 1.2 = 73.2k wood ×
   1.5 = 73.2k stone × 1.5 = 109.8k each). Nobody in the station made any of the three. The whole station's real income
   then was about 557 g/min. Across the run, T(L) was 3–15 times what these players earned at that level: Ana at L12
   had T = 1,785 and earned 213; Dev at L10 had T = 1,495 and earned 154; Mia at L10 had 1,495 and earned 99.
4. **The seat picker keeps a seat it knows is gone (bug).** There is also a race on a quiet server: all three ways
   suggest the same first empty station (design flaw, small). Mia d01-2000 #10–#22, ana d01-2000 #14.
5. **Max spends all your gold whatever the network can feed (design flaw).** Cleo spent 1.55M on +34 levels (cleo
   d06-1400 #177). The sum checks out: 2,000 × (1.15³⁴ − 1) / 0.15 = 1.53M. Max also miscounts for complexes (bug,
   small).
6. **Loady stalls for weeks (design flaw, several causes).** Ana sat at 7/11 from day 4 to day 36, Mia at 4/11 from day
   2 to day 15, and Dev at Level 10 still read "At Company Level 2 you can mine". After Cleo demolished her Lab, the
   next steps said "Build a Lab".
7. **The small ones.** The "+0/min" flicker is a bug. The away screen's duration is a bug: the clock jumps
   made it loud, but it is real. The three return dialogs are a design flaw: two components that were never merged.
   "New neighbours: Windy Heights Co., Windy Heights Co." is a bug. The Lab's "3% yet 15 research/min" is an unclear
   rule: 5 × 101.8 (the level-35 multiplier) × 2.94% = 15.0.

## Root causes

**The Yard (TransportPipes.Game/State/GameWorld.Yards.cs).**
- `Measure` (286–369) sizes a buyer's lack as the unused nominal need of each input that ties for "scarcest":
  `amount × MaxRate × (1 − fraction)` at line 365, for every input with `fractions[r] <= least + 1e-6` (line 363).
  The solver's consistency phase lowers a building's rate until every input gets exactly `amount × rate`
  (FlowSolver.cs 168–217, `AllocateAtRates` 230–246). So every input of a building that runs below full speed has
  the same fraction, `rate / MaxRate`. **They always tie.** Every input then counts as lacking at its full unused
  need, whatever really holds the building back: another input (Cleo's ingots), or its output (Mia's Glassworks).
  The comment at 340 says what was meant ("its scarcest inputs"). The code can't tell which input that is.
- Nothing corrects it. The doc comment (24–25) promises "goods nobody takes are lost on the way (the next round
  sends less)". The only thing that sends less is a *seller* sending less (`ratio`, 562). A buyer that doesn't take
  keeps the same lack: line 321 adds what it took, and 365 adds the nominal need again. The terms stay inside the 1%
  tolerance, so the round slows to every 16 minutes and runs like that for weeks.
- Where the goods go. In the seller's network the gate buyer node is "another company". It takes at tier 3, before
  the seller's own storage and trading post (FlowSolver.cs 384–411). `SplitGateFlows` removes the solver's credit for
  it (Yards.cs 133). The seller is paid only in `SettleGateFlows`, for `flow.Sent`: what left the buyer's gate node
  for the buyer's buildings (216). The rest is never offered, never paid and never counted. It vanishes.
- Mia's House. A `NeighbourInNeed` House has no trading post (GameWorld.Tenants.cs 55–57, `TradingPost: false`) and
  imports nothing (74–75). Its glass has no taker, so its glassworks run at 0 forever. Because of the tie, it "lacks"
  energy and sand at full need, though its own moilies stand idle. One House in four is of this kind
  (`KindFor`, index % 4).
- The same false lacks feed links (Links.cs 692), the Links tab's suggestions (751, 780), the district view's "needs"
  (Social.cs 162), the advisor's partner step (Advisor.cs 982) and Loady's Yard tip (Tutorial.cs 96).
- The screens can't show the truth. `YardTrades` (Yards.cs 835–869) and `NeighbourIncome` (Tutorial.cs 55) report the
  *matched* terms, not what was taken: that is the Yard panel's "You buy 194/min" and the metrics' "Yard g/min". The
  top bar sums `network.Solution.GoldPerMinute` (Networks.cs 904–915), which leaves out the gate settlement. So a
  seller's top bar never shows Yard income, even when it is paid.
- Tests. Every Yard test has a one-input buyer, an Iron Mine (YardFacts.cs 14–29). The Houses tests assert on matched
  terms, never on the seller's gold (TenantFacts.cs 135–187). A buyer held back by a second input was never tested.

**Idle buildings, wrong reason (TransportPipes.Game/State/GameWorld.Advisor.cs `NetworkNeeds`, 573–618).** The card's
"needs" comes from here (GameViews.cs 969–970). The rule says an input that is left over doesn't hold a building
back. But when *every* input is left over (Ana: energy and ore both sold), line 608 falls back to "all of them hold it
back", and so they count at full need. That is exactly the case where the output is stuck. Nothing here looks at
outputs. The building panel's check (GameViews.cs 862–876) finds the stuck output, but only when no linked building
uses it at all. Dev's moily has the dredger linked, so it gets no line even though the dredger can't run. The
advisor's `ShortageSteps` (624) build on `NetworkNeeds`, so they told Ana to build moilies.

**The seat picker (TransportPipes.Frontend/src/components/StationPicker.tsx, FoundCompany.tsx).** The picker fetches
once per district and mode (59–89). It never fetches again after a failed Settle. It takes the server's suggestion
only when no seat is chosen or the chosen seat is in another district (79). `FoundCompany` keeps `seat` across "←
another way" (194). So "A station of my own" fetched fresh, found Mia's old seat in the same district, kept it, and
drew the crew's station with "your seat" on a held seat. Settle stays enabled for any non-null seat (240). The server
was right every time (GameHub.cs 161–164). On a quiet server `SuggestSeat` orders Neighbours (no active players
anywhere), OwnStation and Friends the same way, by district and then slot (Crews.cs 699–705). So Mia and Ana got the
same station 20 seconds apart. OwnStation also takes the lowest slot, which here was a small station.

**Max (TransportPipes.Game/State/GameWorld.cs).** `AffordableLevels` (915–931) counts the levels your gold pays for,
nothing else. The client fires it with no confirm (BuildingPanel.tsx 226–230). It also prices one building, while
`UpgradeCost` multiplies by the complex size and Foreman Rick (911). So on a complex Max offers levels the payment
then refuses.

**The project (TransportPipes.Game/State/GameWorld.Projects.cs `StartStage`, 230–257).**
- The basket's worth is Σ 6 h × 60 × T(L) over the "delivering residents" (249–251). T(L) is a fixed table "never
  measured again" (127).
- A resident counts as delivering after 30 minutes of T(L) in credit (370). The Foundation's whole basket is 600
  energy + 150 ore = 1,050 g (83), below 30 × T(L) for every level (at least 4,500 g). So no one ever qualifies, and
  `DeliveringResidents` (222–227) falls back to the single highest-level resident. Nobody can be counted at the first
  sizing.
- The goods come from the seats' land profiles (`HomeGoods`, 156–184; `Recipe`, 186–216), not from what anyone makes
  or can unlock. Riverside means "sand", although sand needs a Level-5 dredger on a bought shore parcel.
- A basket is never re-sized.
- The Foundation wants iron ore, and no starter kit makes any (83).

**Loady (TransportPipes.Game/State/GameWorld.Tutorial.cs).** Steps run strictly in order, and each waits for its stat.
`firstIngot` waits for `produced:ironIngot`, so an idle factory blocks every later step forever. `built:` steps don't
count the starter kit (26), so Mia's three kit moilies didn't count for "build a Wind Moily". The guide retires at
Level 10 only if the player never started it (`TutorialStep == 0`, 118). The texts hard-code levels (tutorial.yaml
`firstMine`). The advisor suggests a Lab whenever you have none (Advisor.cs 734–745), with no memory of a demolish.
Two Solar Panels at one level give two steps with the same title, and the client keys them by kind and title
(NextSteps.tsx 77).

**"+0/min" (TransportPipes.Server/Services/WorldHost.cs 312–380).** After every command `CollectPushes` calls
`TakeChanges()`, which rebuilds the dirty networks. A new network starts with `Solution = FlowSolution.Empty`
(Networks.cs 19). Then it calls `world.GoldPerMinute()` without solving (374). The server never calls
`SolveNetworks` itself. Only the 2-second tick and `NextSteps` do (Advisor.cs 135, whose comment names this very
problem). So for one tick every rebuilt network reads 0.

**Away screen and dialogs (TransportPipes.Frontend).** `ReportDialog` measures `report.since` against the browser's
`new Date()` (ReportDialog.tsx 27, format.ts 66). The client already keeps `serverOffsetMs` (client.ts 300), and this
line doesn't use it. `ReturnDialog` (2–3 pages) shows when the absence is 3 days or more (GameHub.cs 128). When it
closes, `ReportDialog` shows, because it hides only while `welcomeBack` is set (ReportDialog.tsx 13–15). The new
neighbour is added twice: once as a station neighbour (GameWorld.cs 397) and once as a crew member (Crews.cs 550).
Keys reach the game because App.tsx's keydown handler (59–199) doesn't check for an open modal.

## What I propose

Order of commits, each playable: E1, E2 and E3 first (the leak stops that day), then E4, E5, E6, then the rest.

**E1. The Yard pays at least what the hub would have (S).** In `SettleGateFlows`, add up per (estate, resource,
price class) what the gate offered (`terms.Import` sources) and what the buyer's buildings took (Σ `flow.Sent`). Pay
each player seller its share of the difference at its own `HubBuyPrice`, and count it as a hub sale (Report.Sold, its
Hub Order). The rule is "the Yard is never worse than the hub". *Why:* it closes the leak, and the griefing it
allowed, whatever the matching gets wrong now or later. *Risk:* a seller whose post is full could sell slightly more
than its throughput (only matched spare, one round at most once E2 lands). *Verify:*
`YardFacts.SellingThroughTheYardNeverPaysLessThanTheHub` (A's gold over 60 minutes next to B's maxed Lab ≥ A's gold
with B absent, −0.1%).

**E2. A buyer lacks only what its buildings would take (M).** Replace the nominal-need loop (Yards.cs 356–367) with a
probe solve:
1. Pass 1: `Measure` each member's Spare, Makes, Uses and Sent.
2. Pass 2: for each member, offer at its gate, per resource, what the other members have spare and may sell it, plus
   what it imports now.
3. Solve its network once with those offers as gate seller nodes. Lacks[r] is what the probe node sent.
The solver already handles co-limits, stuck outputs, pipe capacity, valves and the flow order, so there is one truth.
Also: a member never buys and sells the same good in one round (net its own spare against its lack before
`MatchYard`). *Why:* this is the root cause, and it also corrects links, the Links tab, the advisor and the
district view. *Effort:* one more solve per member per round, only when it has a candidate. *Risk:* CPU. The design
sets the budget ("change it if a Yard solve exceeds 5 ms at 8 members", multiplayer-design §15). Matching changes for
every station, and Houses' quarter tests may need new numbers. *Verify:*
`YardFacts.ABuyerHeldBackByAnotherInputIsMatchedOnlyWhatItCanUse` (Cleo's Lab: matched energy ≤ its take + 1e-6, A's
hub sales unchanged), `YardFacts.ABuildingWhoseOutputIsStuckLacksNothing`, `YardFacts.NoOneBuysWhatItSells`, and
`SolverSpeedFacts.AYardRoundWithProbesStaysInBudget` (8 members, under 5 ms). If the speed test fails, fall back to
feedback: a buyer's next lack is at most 1.25 × what it took + 1 per minute.

**E3. Houses that can't run get a trading post (S).** Set `TradingPost: true` for `NeighbourInNeed`
(Tenants.cs 55–57). It still imports nothing (`TenantInputs` returns []), so it buys its one missing good only from
the station, as designed, and can sell what it makes. *Why:* without it, one House in four is a permanent sink.
*Risk:* House kits and pictures (the post isn't in the kit). *Verify:*
`TenantFacts.ANeighbourInNeedTakesOnlyWhatItNeeds` (with spare energy only, nothing is matched to the Glassworks; with
spare sand, it takes the sand and the seller's gold rises by the House price × units taken).

**E4. Screens show what was paid, not what was matched (S).**
- `GoldPerMinute()` adds each seller's gate settlement rate, from the import gate flows' `Sent` × price × share.
- `YardTrades` and `NeighbourIncome` read the taken flows, not the terms.
- The metrics' `neighbourIncome` changes the same way.
*Risk:* small; the report's graphs of the old run keep the old meaning. *Verify:* `YardFacts.TheTopBarCountsWhatNeighboursPay` (GoldPerMinute × 10 equals the 10-minute gold change, ±1e-6,
with a Yard trade running).

**E5. Dead ends explain themselves (M).**
- One game function, `IdleReason(building)`. It checks in order: under construction, damaged, output stuck (no own
  Storage or post on its links, and no linked taker that runs; follow one taker deep, "its only taker, the Sand
  Dredger, is stuck"), input missing, input short, post full, pumps.
- `NetworkNeeds` counts a stuck building as needing what it gets, and when every input is left over, none holds it
  back (delete the line-608 fallback).
- The network DTO gets `stuck: [{building, resource}]` for one card line.
- The building panel's issues come from `IdleReason`.
- A new advisor step, "Pipe your Small Factory to your trading post", uses a what-if with an added link
  (`WhatIfGold` gets `extraLinks`).
- `ShortageSteps` skip stuck buildings.
*Risk:* everyone's card numbers change, and AdvisorFacts must stay green. *Verify:*
`AdvisorFacts.AFactoryWhoseIngotsCantLeaveIsNotShortOfEnergy` (Ana's day-4 layout: Needed = Got for energy and ore,
`IdleReason` = stuck ingot, a dead-end step and no energy shortage step) and
`AdvisorFacts.AProducerWhoseOnlyTakerIsStuckSaysSo` (Dev's moily and dredger). New `e2e/deadend.mjs`: build Ana's
layout, read the card (no "short", one stuck line), press the step's Show me, and the factory runs.

**E6. The seat picker (S).**
- Take the suggestion whenever the chosen seat isn't pickable in the fetched data (StationPicker.tsx 79).
- Clear `seat` on a mode change.
- After a failed Settle, fetch again, preselect the next seat and say so.
- Disable Settle for a seat that isn't pickable.
- The server returns a code (`seatTaken`) instead of only text.
- `SuggestSeat`: Friends takes the district's *last* fitting station, OwnStation and lonely Neighbours the first, and
  OwnStation prefers a station of four.
*Risk:* small; the crew e2e scripts expect today's first station. *Verify:* a vitest for the picker (a stale value is replaced) and `CrewFacts.FriendsAndAStrangerAreSuggestedDifferentStations`.
New `e2e/seat-race.mjs`: two browsers replay Mia #6–#22, and B settles on the first try after the message.

**E7. Max stops where the network can feed the building (S–M).**
- `UsefulLevels`: the highest level, up to what you can afford, at which the building still runs at 90% or more,
  found by what-if solves (binary search, ≤ 7 solves; big networks estimate from need against got + spare).
- The details DTO adds `usefulLevels`, `maxRunsAt` and the needs at Max.
- "Max +N" uses the useful number. Beyond it, a confirm gives the needs and the run rate.
- Any spend over 25% of gold asks once.
- `AffordableLevels` prices like `UpgradeCost`.
*Risk:* up to 7 what-if solves per building-panel read; cache them per network solve. *Verify:* `GameWorldFacts.MaxStopsWhereTheNetworkCanFeedTheBuilding` (a Lab with one factory and 2M gold: the useful
level's ingot need ≤ 6.1/min) and `GameWorldFacts.MaxPricesAComplexLikeTheUpgrade`.

**E8. The project's mechanics (M; the numbers are economy's).**
- (a) Make the Foundation's basket energy only (Projects.cs 83).
- (b) `Recipe` draws its "home" goods from what active residents make now (Makes), and its keystone from a good nobody
  makes yet but some resident has unlocked and has land for.
- (c) Size by measured income: a new rolling 24-hour gold/min per company, in place of T(L).
- (d) Anyone who delivered to the previous stage counts for the next basket.
- (e) After 3 days with no delivery, or when a counted resident sleeps, scale the rest of the basket to who delivers
  now.
*Risk:* the balance runs in docs/balance size stages differently and must be run again; a basket that shrinks can
be gamed by not delivering (E8e only ever scales down to who delivers, never below one resident). *Verify:* `ProjectFacts.TheFoundationStandsOnTheFirstEveningWithStarterKitsOnly` (fresh kits, done within 30 game
minutes), `ProjectFacts.TheWaterTowerAsksOnlyForGoodsTheStationMakesOrCanMake` and
`ProjectFacts.ABasketShrinksWhenNobodyDeliversForThreeDays`.

**E9. Loady and the next steps follow the estate (S).**
- Count kit buildings from step 3 on (a `kit: true` flag in tutorial.yaml).
- Retire chapter 1 at Level 10 whatever the step (Tutorial.cs 118).
- After 3 play days on one step, offer "Skip this one".
- `TutorialTip` covers `produced:` steps with `IdleReason` ("Built, but it makes no ingots: …").
- No hard-coded levels in texts.
- No "Build a Lab" within 3 days of demolishing one.
- Remove duplicate step titles, and key steps by building.
*Risk:* players skip more of the guide's gold; TutorialFacts that walk the steps in order need the kit flag. *Verify:* `TutorialFacts.AStarterKitCountsForTheMoilyStep`, `TutorialFacts.AStepWaitingForAnIngotSaysWhy`,
`TutorialFacts.ChapterOneRetiresAtLevelTen`, `AdvisorFacts.ADemolishedLabIsNotSuggestedAgain`,
`AdvisorFacts.NoTwoStepsShareATitle`.

**E10. Solve before reading rates (S).** `GameWorld.GoldPerMinute()` and `ResearchPerMinute()` call `SolveNetworks()`
first, as `NextSteps` already does. That fixes the push, the hello and the panels in one place. *Risk:* a command pays
for its solve at once instead of at the next tick (the same work). *Verify:*
`GameWorldFacts.TheIncomeIsRightStraightAfterAnUpgrade` (no tick between: the rate equals the one after a tick, and
is greater than 0).

**E11. One way back (S–M; the content is clarity's).**
- The report DTO carries the absence in server minutes, and the dialog uses it.
- When `welcomeBack` is set, its first page also carries the report's facts, and its one button collects the report.
- Fold page 2 into page 1, and keep the landing cards for the Quiet Isles only.
- `NewNeighbours` becomes a set.
- The keydown handler ignores keys while a modal is open.
*Risk:* the Quiet Isles path (3 pages) must keep its landing choice. *Verify:* a vitest (welcomeBack and report both set: one dialog, one button), `ReturnFacts.ANewNeighbourIsListedOnce`
and `e2e/return.mjs` (counts one dialog).

**E12. Research shows its rate as "15 of 509/min · 3%" (S).** Nothing to fix in the rule. *Risk:* none.

## What I'd not do

- **Let goods pass through buildings.** The solver's link model (one link per pair in a pipe group) and every layout's
  balance rest on that rule. Explain it where it bites (E5). Ana's wish, "send to buildings first", already exists:
  own buildings come first in the flow order (FlowSolver.cs 373–374). Her factory's problem was its output.
- **Bill the buyer for everything matched.** That would have charged Cleo about 7M gold for energy she never used.
  The fix is to match what she can take (E2), and pay sellers no less than the hub (E1).
- **Patch the Lab or Max alone** (for example, cap the lacks of upgraded buildings). The same tie took Mia's energy
  through a House. It's one rule in `Measure`.
- **Lower the Houses' 25% cap or switch Houses off** to stop the leak. The cap did its job: it bounded the damage.
- **Re-tune the T(L) table from five AI players.** Measure each station's income instead (E8c), and let the table
  stay the cap.
- **Smooth "+0/min" in the client.** It would hide a server read before the solve. Solve first (E10).
- **Chase the WebSocket 404.** It came with the clock jumps. Look again only if it shows without them.

## Questions for others

- **Economy:** should E1's "unsold at hub price" count toward the seller's Hub Order (and so lower its hub price)? I
  say yes: it is a hub sale. Do you want NeighbourInNeed Houses at all, once they need a post? And the hours for
  measured-income baskets (E8c)?
- **Clarity:** the texts for `IdleReason` (stuck, taker stuck), the stuck line on the network card, and the Max
  confirm. Where should paid Yard income show: the top bar only, or also "Where it goes"?
- **Together:** with true lacks the Yard will trade *less*, because its biggest flows here were false. Is it still
  visible enough to carry "playing together", or do Asks and links have to carry it?
- **Researcher:** for E1–E3, replay from row07 (before Cleo's Max). Measure each seller's real gold growth against
  hub-only and paid Yard, and Mia's income against Dev's from row01. I predict Mia goes from 64% to about 80%
  of Dev from E1–E3 alone.
