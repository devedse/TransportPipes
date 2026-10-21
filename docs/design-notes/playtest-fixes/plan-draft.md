# The draft fix plan (moderator, after round 2)

Built from all ten papers. Where you agreed, it takes the merged proposal; where you disagreed, it decides, says why,
and puts the close calls to the vote in round 3 (marked **VOTE**). Effort: S ≤ 2 h, M ≤ a day, L more.

## The verdict

The way in works. What broke playing together is four things, in this order of cause:

1. the Yard took friends' goods for buyers that couldn't use them and paid almost nobody;
2. buildings whose output couldn't leave sat idle while every screen blamed their inputs;
3. the friends had nothing a friend needed (every kit is energy), and the one shared buyer, the project, asked for
   goods nobody made, at sizes from a table 3–18× their real income;
4. the screens players read (Loady, the return dialogs, the building panel, the "This week" row) never carried the
   friends, the project or the fix.

**Make it true, then show it.** Fix the Yard before any screen shows trades. Fix the dead ends before the Next steps go
to everyone. Fix the recipe and size before the project is shown on any row or card.

## Rules for every fix

- **One truth per fact.** One `IdleReason` for the panel, the card, the next steps and Loady. One paid figure, never a
  matched one, wherever income or trade shows.
- **The cause in the first five words** (Ben skims, Dev doesn't read). A building panel shows a block only when it has
  something true to say, in a fixed order: status and reach line → the one fix button → where it goes → upgrade →
  needs and makes → its network.
- **An event budget.** A toast is for what happens while you watch; what happened while you were away goes on the
  return card; the bell is the archive. On a phone, one toast at a time.
- **Privacy:** a trade pair is shown only to its two ends (§7: "never a list of who didn't deliver").
- **No gold moves between players and no gold buys a shared decision:** no gifts, no point trading, no keystone valve.
- **Keep v1.2's prices** (1.75 between players, Houses at 1.25 and 2.0, the 25% cap) and its "no votes, no kicks".
- **Every database change is an additive migration**, so the snapshots `row01`, `row07`, `row08`, `row10` and `row14`
  stay loadable for replays.

## The fixes, in ship order

### F0 · Instruments and replays (S–M) — before anything else

Playtest metrics:
- `yardPaid` beside `yardMatched`;
- per building, `idleSince` and `idleCause`;
- project deliveries per good and company;
- return dialogs shown;
- the share of output traded between humans, t;
- the share of new gold from NPCs;
- the first-gift event.

A dev cheat `project restage`. The scripted replays (`e2e/replay-*.mjs`): restore, advance, assert, for:
- row01 (with Mia's seat race);
- row07 (Cleo's forced Max);
- row10 (+6 h, no actions);
- row14 (Ben's return on a phone);
- Dev's d10 (his d08 move-in scripted from `actions.jsonl`).

### F1 · The Yard is honest (S + M) — the leak stops on day one

1. **Floor (S, first commit).** The seller keeps its own hub value for every unit that reaches its gate
   (`SplitGateFlows` removes only the premium). `SettleGateFlows` pays the premium, settlement price − the seller's hub
   price (never negative), for what the buyer took. It is *not* counted in the Hub Order (engineer: taken and untaken
   can't be told apart at the seller's solve). It is bounded because spare is measured from what already goes to the
   hub. Revisit if run 2 shows the gate used to dodge the hub's √.
2. **Lacks only what a building can use (M).** In `Measure`, a lack is capped:
   - by the building's **output state**: 0 when `IdleReason` is stuck or taker stuck, the current run fraction when its
     own post or Storage is full;
   - by its **other inputs**, counted on the building's own links plus the station's spare of them (economy 1B, with
     the engineer's corrections; "own network" scope dropped).

   A probe solve stays in a test fixture only, to check the formula.
3. **Feedback.** A buyer that took under 80% of last round's match is matched at most 1.25× what it took, plus a small
   probe.
4. **Spare is real.** It counts storage-bound output beyond what the Storage keeps (`StorageWants`), and the idle
   capacity of a building held back by its output.
5. **No dead Houses.** `NeighbourInNeed` Houses get a trading post (existing ones at startup).
6. **Screens show what was paid.** `GoldPerMinute()` adds the gate settlement, and `YardTrades` and `NeighbourIncome`
   read taken flows. This ships only after 1–5.

Tests:
- `SellingThroughTheYardNeverPaysLessThanTheHub`;
- `ABuyerHeldBackByAnotherInputIsMatchedOnlyWhatItCanUse`;
- `ABuildingWhoseOutputIsStuckLacksNothing`;
- the conservation test (sellers receive = buyers pay + hub price × untaken);
- `TenantFacts.ANeighbourInNeedTakesOnlyWhatItNeeds`;
- `SolverSpeedFacts` within budget.

Replay row10 +6 h: Ben ≥ 70 g/min, Ana ≥ 200, Dev ≥ 120, Mia ≥ 90. Each player's real growth = hub + paid Yard, within
2%. The Lab is matched under 1 energy/min.

### F2 · Dead ends explain themselves (M)

- **`IdleReason`:** under construction · damaged · stuck (no own Storage or post on its links and no running taker) ·
  taker stuck (one level, visited set) · full · input missing · input short · post full · pumps.
- **`NetworkNeeds`** skips stuck buildings (the line-608 fallback goes), and so do `ShortageSteps` and `ValuesOf`.
- **The panel.** The pill reads `⛔ Stuck: ingots can't leave`. The reach line reads `Its pipes reach only: Iron Mine,
  Wind Moily.` One button, **`Lay the pipe to your Trading Post · 12 g`**: a `LayTenantPipe` dry run to the nearest
  pipe group that touches the post or a Storage, shown as a ghost and laid on click. The same button points at the
  Yard gate when the building's best taker is a neighbour or the project.
- **The network card** names stuck buildings first and shows no false "short".
- **A map bubble** on a stuck building (art check).
- **Loady's `firstIngot`** reads: "build it right on the pipe from your Iron Mine to your Trading Post … Goods never pass
  through a building." It gets a `produced:` tip.
- **The goal tracker** alerts a stuck building for everyone.
- The lit pipe groups come later (F10).

Tests: `AdvisorFacts.AFactoryWhoseIngotsCantLeaveIsNotShortOfEnergy`, `AProducerWhoseOnlyTakerIsStuckSaysSo`, and
`e2e/deadend.mjs`. Replays: row08 (Ana), row10 (Ana, Dev), Dev d10. Pass: ≤ 10 owner play minutes from stuck to
running, and no dead-end STUCK.

### F3 · Next steps for everyone, and Loady follows the estate (S–M) — ships with F2

- The Next steps show from Level 3 whatever the chapter (collapsed on a phone).
- The kit counts for every step but the first, whose text says the kit already makes energy.
- A step open for 2 play days shrinks to one line with its tip.
- Chapter 1 retires at **Level 10** for everyone.
- No levels in step texts, and no suggestion of a building type demolished in the last 7 days.
- No duplicate titles; steps are keyed by building.
- The advisor's effort is max(200, 10 min of income, 2% of the bank), and upgrade steps may offer a smart-Max level.

Tests: `TutorialFacts` (kit counts, produced tip, retires at 10), `AdvisorFacts` (no demolished type, no duplicate
titles), `guide.mjs`.

### F4 · A project the station can reach (M)

- **Foundation:** 900 energy (stands at about minute 13–20 on kits alone).
- **No tail:** the project takes all that's offered, up to what's left, in proportion. A good is done at 99.5% or with
  under 1 unit left.
- **Recipe:**
  - two goods active residents make now, preferring goods other than energy;
  - a **keystone**: a good nobody makes yet that an active resident can make with one building unlocked by **Level 5**
    on land they have or can buy;
  - "makes" means running output that reaches a gate (`StationMakes` fixed);
  - the keystone is chosen again when its maker stops.
- **Card:** it names who can make the keystone and how: `Sand: a Sand Dredger, Level 5, on a beach · Ben, Dev`.
- **Size:**
  - each made good is 4 h of the station's spare of it (at least 4 h of one level-1 building), capped at 6 h of the
    summed income I(c);
  - the keystone is 6 h of one level-1 building;
  - the Workshop is 6 h and the Foundry Hall 9 h;
  - baskets only shrink: a counted resident's part comes off after 3 days without a session.
- **Measured income** I(c), a 24-h average of production gold a minute (at least 60), replaces T(L) for allowances (2 h a
  day, banked 3 days), the delivering threshold, the Landmark and Wonder stages. T(L) stays only for trunk segment
  prices.
- **The keystone** is exempt from the allowance.
- **Shown** on the **"This week" row**, clickable (`This week · 🏗 Water Tower 12% · wants sand · +6 g/min for you ›`;
  the parcel moves to the Next steps), on the return card, and in "Who could use" (F6). The bonus is shown honestly in
  g/min. No top-bar chip.

Tests: `ProjectFacts` (Foundation in minutes, every good makeable, no tail, shrink). Replays: row01 (Foundation by
20:30, Mia's too), and row07 + `project restage` + 8 days (Water Tower ≥ 50%).

### F5 · One return card (M)

- Built by the server (its clock), replacing `ReportDialog` and `ReturnDialog` pages 1–2.
- Sections:
  - the duration;
  - gold and level;
  - **your crew** (resting · tend · stand-in · back · moved in · what your goods did for them);
  - the station's project;
  - **to look at** (stuck buildings · storm damage with Repair all);
  - **VOTE A**, the Invest line.
- Six lines at most, one button, keys trapped.
- The landing screen only for the Quiet Isles or crates.
- New neighbours listed once.

Tests: `return.mjs` (one dialog after 12 h and 10 days). Replays: row10 → day 27 (Ana's card names Ben's stand-in and
the storm damage), row14 on a phone (≤ 2 taps).

### F6 · Trade with a face, and things a friend needs (M) — after F1

- **"Where it goes"** shows gate flows with named ends (`⚡ to the Yard → Dev's Sand Dredger · 8/min · 1.75 g`) and the
  project.
- **The top bar** counts paid Yard, Ask and project income, with a hover split.
- **The first-gift event**, for human pairs only (Houses folded into one line), within the event budget.
- **A 💛 Thanks** tap, once a day per pair.
- **Pairs** are shown only to their two ends.
- **"Who could use your sand"** on a producer's panel, only when a real taker exists, with **"Plan the pipe to my Yard
  gate"**.
- **The receiver's card** in the Next steps: `Cleo has 6 iron ingots spare: a Lab piped to your Yard gate gets them by
  itself`.
- **Interdependence:** `ChainSteps` also offers factories whose missing input a station neighbour has spare, priced at
  1.75, and recipes prefer such goods.
- **`firstYard`** counts only the player's own pipe.
- **The toolbar's "Station"** becomes **"Neighbours"**, and the panel's first line says how to give: `To give a neighbour
  something, pipe the building that makes it to your Yard gate (G). It goes to whoever here lacks it, at 1.75 g.`
- **The seat card** says what's really there (`wind kit · sand: a dredger on the shore, Level 5`).

Verify: `yard.mjs` (both panels). Row10: Dev's dredger offers the gate pipe, and after one click its sand reaches the
project in the session. Run 2: the first good knowingly given.

### F7 · The seat picker tells the truth (S)

- After a refusal it reloads with the next suggestion, never keeps a stale seat, and disables Settle on a seat that
  can't be picked.
- The server returns a `seatTaken` code.
- Friends and a stranger are suggested different stations, and OwnStation prefers four seats.
- An honest text when no human has a free seat.
- An empty company name says so.

Tests: a vitest, `CrewFacts`, and `seat-race.mjs`.

### F8 · Smart Max (S–M)

- **Useful levels:** the highest level the building's own links plus what its gate brings now keep at ≥ 90%. The Yard's
  spare is not counted; it shows as a separate line ("with your neighbours' spare it could run at 40%").
- A price on every upgrade button.
- A choice row only when Max goes past the useful level. **No 25%-of-gold confirm.**
- `AffordableLevels` prices like `UpgradeCost` (complexes, managers).

Tests: `MaxStopsWhereTheNetworkCanFeedTheBuilding`, `MaxPricesAComplexLikeTheUpgrade`. Replay: row07, Cleo #177.

### F9 · Friends by name, the crew where people look, tending that does a chore (S–M)

- "Who is this seat for?" → "Copy Ben's link"; "Ana kept this seat for Ben"; crew mates read `Riverside Co. (Ben)` to
  their crew.
- The crew gets its own top-bar spot (out of the clipped name; an icon with a dot on a phone).
- Tending repairs a friend's storm damage for free. **No caretaker's-fee waiver** (a faucet nobody missed; researcher).
- Stand-ins copy a 14-day median of *paid* flows per partner and good (together, missed 2).

### F10 · Trust in minute one, the phone, and small breakers (S–M)

- Solve before reading rates (no "+0/min"), and no "Something is in the way" after a placement that worked.
- On a phone: one toast at a time, the level-up after Loady's first step, the pipe step's text, and a Show me that picks
  the Pipe tool. The building panel is a bottom sheet of at most 55%.
- Overlays turn off when their step is done; Wind and Sun get a legend.
- Chat gets day stamps and a "new since you left" line.
- Buttons have distinct accessible names, and research cards are named by their tech.
- The lit pipe groups (after F2's button).

### F11 · The edges (S each)

- Link offers wait until 24 h after the other side's next session (at most 14 days) and show on their return card.
- When no livelier station has a free seat, the lone-human card gives the honest text, plus a link suggestion only
  when a real spare–lack pair exists.
- A research queue of five (low; economy keeps it, clarity and the researcher rank it last).

**Dropped:**
- the crew chat channel (withdrawn);
- the keystone valve (withdrawn);
- hub research points (withdrawn);
- letting goods pass through buildings (unanimous: explain it instead; reconsider only if Ana's replay still fails
  after F2);
- a "send to a friend" button;
- gold sinks;
- a 25% confirm;
- a top-bar project chip.

## VOTE A · the Invest line

- **Economy:** on the return card, "You have 1.8M · upgrading your 3 Wind Moilys to level 25 brings about +1,700 g/min,
  back in about 1 h · Invest". One tap spends at most half the bank on smart-Max levels only, counting consumers on the
  player's own supply (together's guard).
- **Researcher:** autopilot; it moves incomes toward the bot's curve without a choice. Show the levers (F3's
  bank-scaled advice) and re-measure in run 2.
- **The moderator's proposal:** the line, with **"Show me"** opening the best upgrade (one building at a time, smart-Max
  level preselected), and no one-tap spend of half the bank. Vote: as proposed / economy's one-tap / none.

## Verification (the researcher's, with economy's amendments)

1. Each fix step passes its tests and its scripted replay before the next ships.
2. **Five agent replays** from the snapshots with run 1's prompts and memories (about 250k tokens):
   - Ana d08 and Ana d14 (factory);
   - Dev d10 (dredger);
   - Cleo d06 (Max);
   - Mia d01 with Ana (picker);
   - Ana d27 (stand-in, project).
3. **The second full run** after F1–F6 pass. It uses the same story, personas, devices, budgets and 5× clock, with one
   build, a snapshot before every row, and a scripted solo human, Jo, so §17b can be tested. The digest computes the
   measures.

| Measure | Run 1 | Run 2 passes if |
| --- | --- | --- |
| First good knowingly given (a paid flow seen on both first screens + named by the giver) | never | one pair by day 2 20:00; every crew member with 3+ sessions by day 14 |
| Yard honesty: real growth − hub = paid Yard shown | 32–90 shown, ≈ 0 paid | within 2%, every sample |
| Harm from a neighbour (> 10% fall without own action) | Ana −83%, Ben −100% | none |
| Dead ends: owner sessions idle without knowing why | Ana 5, Dev 3 | ≤ 1; no dead-end STUCK |
| Real STUCK notes | 9 | ≤ 3, none on dead ends or seats |
| Project | Foundation day 3; Water Tower 0% to day 36 | Foundation by day 1 20:30; Water Tower done by day 8, Workshop by day 14; opened by row 4; keystone made by a crew member |
| Return | 3 dialogs, 51 clicks | 1 card, 1 click, friends' news on it |
| Absent friend | Ana never learned of the stand-in | Ana's day-27 diary names it |
| Mia | 9 min to a seat; "alive" 3/5 | seated within 3 min beside Jo; alive ≥ 4 twice; 85–110% of Dev per building and per command |
| Trade share t | 0% | ≥ 15% between humans by day 14 |
| Come back tomorrow, crew, day 8 on | 1 yes in 13 | yes in at least half |
| Silent losses (storms) | Ana −19%, Ben −15% | repaired, or on the next return card |

Plus an equal-bot balance run (one solo bot in a House station against a crew of four of the same bots) for the
85–90% promise.

## What would change the plan (decided now)

- **Ana's replay still ends with an idle factory despite F2:** then the rule is the problem. Let a building pass on goods
  it doesn't use, or lay the pipe for her.
- **Run 2 pays and shows trade, but nobody knowingly gives by day 8:** giving needs an act (an Ask, or "I make it" on
  the building panel).
- **The project is seen and reachable but under 50% by day 8:** the size is wrong.
- **t < 15% after F6:** the crew promise fails for structural reasons. The next lever is seat kits with one consumer of a
  neighbour's specialty.
