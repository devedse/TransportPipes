# Round 2 · engineer

I read the four other papers in full. I checked every root cause they state and every fix I would have to build
against the code at `b5b5a83` and the run's data. Where I change my round-1 position, I say so.

## Answers to the questions put to me

**Economy.**
1. *Mia's unpaid 31 g/min.* It is both the same `Measure` flaw and a House-side flaw. The tie (Yards.cs 363–365)
   makes her neighbour, the Glassworks, "lack" energy and sand at full need. And a `NeighbourInNeed` House has no
   trading post (Tenants.cs 55–57), so its glass has no taker and it can never run. Nothing matched to it is ever
   taken.
2. *The 18-hour tail.* Confirmed. `MatchProject` asks for what is left ÷ 60 each minute (Projects.cs 293). The round
   returns every minute while terms move by more than 1%, and a good counts as done at 10⁻⁶ (381). So what is left
   decays as e^(−t/60): 150 ore takes 60 × ln(1.5 × 10⁸) ≈ 1,130 minutes. The allowance-capped offer (300) decays the
   same way.
3. *Can rule B read the Yard's spare cheaply?* Yes. `YardMembers` becomes two passes: measure everyone's spare, then
   cap each member's lacks with the station's spare per good. It is all dictionaries, with no extra solve. B needs two
   corrections (see the objections).
4. *Where rule D settles.* On the seller's side. `SplitGateFlows` (133) removes only the premium, so the seller keeps
   its own hub value for every unit that reaches its gate. `SettleGateFlows` (216) then pays the premium,
   `SettlementPrice − seller's hub price`, for what the buyer took. The premium is never negative: the player price
   is 1.75 × base, a House pays at least the hub, and the hub pays at most the base.
   - It stays right when a buyer disconnects between rounds, which the buyer's side would get wrong for one round.
   - It needs no new state.
   - The gold it creates is the hub price × (sent − taken), exactly what a hub sale would have made.
   - I would not count it in the Hub Order. At the seller's solve, taken and untaken can't be told apart, and counting
     everything would lower the hub price for real Yard sales.
   - Your conservation test becomes: sellers receive = buyers pay + hub price × untaken.

**Clarity.**
- *Can `Issues()` name what a building's pipes reach?* Yes. `linked` is already computed (GameViews.cs 839–844).
  Splitting it per pipe group takes one connected-components pass over the network's conductors (S).
- *"Nothing could take it" or "nothing took it this round"?* Decide from links, never from flows.
  - An output is **stuck** when no building on its own links takes that good and no own Storage or post is linked.
  - It is **taker stuck** when every such taker is itself stuck (one level deep, with a visited set).
  - A full Storage or post is **full**, a different state.
  - So the status can't flicker.
- *Can `NetworkNeeds` skip stuck buildings without changing the advisor's other steps?* It has three users: the
  network card (GameViews.cs 970), `ShortageSteps` (Advisor.cs 626) and `ValuesOf` (254), which prices one more unit
  for the advisor's estimates. All three should skip a stuck building: more energy is worth nothing to it.
- *Can the planner route to another pipe group?* Yes, on the server. `LayTenantPipe` (Tenants.cs) already does a
  two-cost breadth-first route from a building to a goal over free estate tiles and own pipes. A dry-run variant can
  return the tiles, with the goal "a tile next to a pipe group that touches the trading post or a Storage" (S–M).
- *Is a pipe-group id on the client's conductors cheap?* Don't put it on every conductor. Send each network's groups as
  tile lists in `NetworkDetailsDto`, computed once per rebuild (S).

**Together.**
- *Is the floor simple?* Yes: two lines in `SplitGateFlows` and one in `SettleGateFlows`.
- *Does a Storage hide spare?* Yes, and it is worse than your guess.
  - Spare counts only hub sales and idle capacity (Yards.cs 307, 343–355). A flow into storage is neither.
  - When the Storage is full, the producer is held back by its output, so its inputs tie at its rate (`least` < 0.999)
    and the idle-capacity branch never fires.
  - Only a building with no inputs can ever show idle capacity. Dev's dredger, even piped to his gate, would never
    have offered sand to the Yard or the Water Tower.
- *Were Mia's 33 g/min paid?* No (my round 1).
- *Can the planner start from one building's network?* Yes (above).

**Researcher.**
1. *Did any Yard sale settle between players?* Effectively none. The only real take I can find is the Lab's ~0.6
   energy/min, about 1 g/min split three ways. The project's purchases did pay, and the test case pays (YardFacts.cs
   39–61).
2. *Did Mia's energy really leave?* Yes. The gate buyer node takes at tier 3, before her trading post (FlowSolver.cs
   384–411), and her kit is piped to both. It lowered her hub sales by what was matched.
3. *Does Ana's dead-end factory make a Yard lack?* By the code, no. Cleo's false ingot lack (216 ingots/min, cleo
   d14-1900 #235) would have pulled the factory's ingots through the gate if its pipe group reached Ana's gate, and the
   factory would have run. It sat at 0, so its group doesn't reach the gate. Her "Lacks 11 energy" comes from another
   building, and I can't name it without the solver dump.
4. *Why did 150 iron ore take 18 hours?* The tail (Economy, answer 2). The crew's mines came on day 2 at 20:04 and the
   Foundation stood on day 3 at 14:18. Mia's took the same 18 hours on days 15–16.
5. *Can every fix keep the snapshots loadable?* Yes.
   - Everything I build is code only, except measured income and a research queue: one column each, as additive
     migrations.
   - Existing `NeighbourInNeed` Houses get their trading post in `EnsureTenants` at startup.

## Where I agree

- **Economy 1 A–D replaces my E1–E2, with corrections.**
  - D is my E1, settled on the seller's side.
  - C is my fallback, now kept for good.
  - B replaces my probe solve as the live rule. `SolverSpeedFacts` records that a dense network once took 0.4 s for a
    single solve. One probe per member per round would break the design's own budget ("change it if a Yard solve
    exceeds 5 ms at 8 members", multiplayer-design §15) on late-game networks, and would slow the catch-up after an
    absence.
  - The probe stays in the tests: a fixture test compares B's lacks with a probe solve.
- **I drop my round-1 "net a member's own spare against its lack".** It is wrong when the spare and the lack sit on two
  pipe groups that both reach the gate. Then the swap through neighbours is real help.
- **Economy 2b (no tail) has to ship with 2a.** Without it the energy-only Foundation that the analyst, Together 5 and my
  E8a propose still takes 60 × ln(9 × 10⁸) ≈ 20.6 hours for 900 energy. Row 1 fails. I adopt 2c, 2d and 2e. 2e is one
  column and a 24-hour moving average updated in `Apply`.
- **Economy 3: smart Max at 90%.** Level 9 for 27.5k is right for Cleo's Lab: at level 9 it needs 4.3 ingots and 21.4
  energy, and level 10 needs 9.4 ingots. Clarity 5 gets the same answer. Sizing the advisor's effort to the bank is S.
- **Economy 4: research queue.** It is feasible (S–M: `CurrentResearch` is used in 11 places), and so are points sold at
  the hub (S).
- **Clarity 1 is my E5 with better words.** That covers the stuck status, "Its pipes reach only…", the one-click pipe,
  the network card's stuck line, the lit pipe groups and Loady's `firstIngot` text.
- **Clarity found a root cause I missed, and I correct my round 1.** `GoalTracker.tsx` 37 renders the Next steps (116)
  only from chapter 2 or while a step waits for a level. So Ana never saw them: they didn't send her to the moilies,
  Cleo's chat tips did.
- **I adopt Clarity 2 (Next steps from Level 3), 3 (one return card, built by the server), 4 (the picker gets the next
  suggestion with the error) and 6 (names).** I confirmed 7: the crew chip sits inside `.company-name` at 22ch with an
  ellipsis (global.css 450–456), and on a phone it is hidden (4374).
- **Together 2, 3, 4 and 9b.**
  - 4 is confirmed: `Handshakes` adds the `yard` stat to every member whose network reaches the gate (Yards.cs
    684–694). So the kits completed `firstYard` with no pipe of the player's own.
  - 9b is confirmed: link offers lapse after 72 hours (Links.cs 96).
- **Researcher 1–4.**
  - The order of pain, and "nothing on the top bar before the Yard pays".
  - The instruments: `yardPaid` from the `earnings` that `SettleGateFlows` already fills, and `idleSince` and
    `idleCause` from `IdleReason` (S).
  - The scripted replays from row01, row07, row10 and row14.

## Where I object

1. **Caps on inputs alone don't fix the Yard (Economy 1B, Together 1).** Together writes: "Ana's factory, short of
   both energy and ore, still gets both."
   - Ana's factory was never short of anything. Its ingots had nowhere to go.
   - Under B both inputs have spare, so f = 1, and the factory "lacks" 20 energy and 12 ore at full need, which is
     matched and never taken.
   - The floor saves the sellers, not the matching. `Share` (Yards.cs 383–440) splits spare in proportion to lacks,
     so a false lack takes its share away from real buyers. And the project gets only what the residents leave
     (618–620).
   - B needs an output term: 0 when `IdleReason` says stuck or taker stuck, and the current run fraction when the
     building's post or Storage is full.
2. **"Its own network" is the wrong scope (Economy 1A and 1B's "network spare").** A network is not what a building
   can reach (Networks.cs 7–12), the same confusion that trapped Ana. Cleo's 395 spare energy sat in the same network
   as her Lab and didn't reach it. A and B must read the building's own links. Then A follows from B and can be
   dropped.
3. **"Energy only, so it stands on the first evening" (Together 5, the analyst).** It doesn't stand without Economy 2b
   (objection to the wording, not the fix).
4. **"Cleo's 179k points came from the bug that let a Lab at 3% make 15 points a minute" (Researcher, "Where I differ"
   6).** That is no bug. Research is `ResearchPerMinute × level multiplier × rate` (Networks.cs 813): 5 × 101.8 ×
   0.0294 = 15/min. Economy 5 gets the same. Max tripled her research while 98% of its cost bought nothing. Research
   can stay low, for the other reasons the researcher gives.
5. **Smart Max should not count the Yard's spare (Economy 3).** That spare is shared by the whole station and
   disappears when its seller builds. Max would buy levels that run at 3% the next week. Count the building's own
   links plus what its gate brings now (Clarity 5's rule).
6. **The replay's pass mark "matched no more energy than its ingots let it use (about 30/min)" (Researcher 4, row10).**
   The Lab's own pipes already bring that 30. The Yard should match it under 1/min.
7. **Chapter 1 retires at Level 12 (Clarity 2.5).** Dev sat at step 6 at Level 10 (dev d21-1900 #155), and the code
   already uses 10 for players who never started. Use 10 for everyone.

## What everyone missed

1. **False lacks crowd out real buyers and starve the project**, even with the floor (objection 1). The floor makes the
   bug cheap for sellers, not harmless.
2. **Spare has the same tie as lacks.** A building held back by its output never shows idle capacity: the branch at
   Yards.cs 343 needs every input at 99.9%, and a stuck building's inputs tie at its rate. Spare also ignores flows into
   storage. So "Dev brings the sand" can't work even after he pipes to his gate. Item 1 must count storage-bound output
   beyond what the Storage keeps (`StorageWants`) and the idle capacity of a building held back by its output.
3. **The solver's own test asserts the tie.** `SolverSpeedFacts` checks that every input gets `amount × rate`: "every
   building gets what its rate needs". `Measure` looks for the scarcest of quantities the solver guarantees to be equal.
4. **`StationMakes` (Projects.cs 477–483) counts any building**, stuck or off the gate. Together noted it for the card.
   It also wrongly feeds "Nobody here makes it" into the keystone choice once 2c lands, so 2c must use running output
   that reaches a gate.
5. **Two small engineering bugs only I raised.** `AffordableLevels` ignores the complex and manager factors
   (GameWorld.cs 911 against 915–931). And OwnStation takes the lowest slot, which put Mia in a two-seat station
   (Crews.cs 705).

## My ranked top 10

1. **The Yard is honest and never costs a seller (M).**
   - The floor, on the seller's side (Economy D).
   - Lacks capped by the building's output state and by its other inputs, counted on its own links plus the station's
     spare (Economy B, corrected).
   - The buyer feedback (C).
   - Spare that counts storage-bound and output-limited capacity.
   - `NeighbourInNeed` Houses get a post.

   It ranks first because it is real harm (3.5M gold lost, Mia's quarter of her energy) and because every later
   surface shows these numbers.
2. **Dead ends explain themselves (M).**
   - `IdleReason` with stuck and taker stuck.
   - `NetworkNeeds` without the line-608 fallback.
   - "Its pipes reach only…".
   - A one-click pipe from a `LayTenantPipe` dry run.
   - Loady's `firstIngot` text and a tip for `produced:` steps.

   It is the most pain: 5 real STUCK notes, Ana for 32 days.
3. **A project the station can reach (M).**
   - Foundation energy only, and no tail.
   - The recipe from what residents make and can make.
   - Sized by measured spare and income.
   - It shrinks when a counted resident stops delivering.
   - The bonus in g/min.

   It was the only shared buyer in week 1, and seeing it gave the run's only "together 4".
4. **One return card (M).** Server clock, the report merged in, friends' news, keys trapped, neighbours deduplicated.
   All five players, every return.
5. **Loady and the Next steps (S).**
   - The advisor visible from Level 3.
   - The kit counts.
   - Chapter 1 retires at Level 10.
   - No suggestion of what was just demolished.
   - No duplicate titles, no levels in texts.

   Four players, up to 32 days, and it's cheap.
6. **Trades with two named ends (S–M, after 1).** "Where it goes → Dev's Sand Dredger", and the top bar counts paid
   Yard and project income. It is the precondition for "knowingly given".
7. **The giver's door (M).**
   - "Who could use your sand" on producer panels.
   - The pipe planned from this building's network to the gate.
   - `firstYard` done with the player's own pipe.

   It ranks below 6 because it only helps once there is a true buyer (1, 3).
8. **The seat picker (S).** It reloads with the next suggestion, never keeps a stale seat, Friends and OwnStation
   suggest different stations, and OwnStation prefers four seats. One STUCK, a stranger's first nine minutes.
9. **Smart Max and advice sized to the bank (S–M).** Cleo once, but 1.53M and the trigger of the drain.
10. **Read rates after the solve, and the instruments (S).** `GoldPerMinute()` solves first, and Playtest.cs gets
    `yardPaid`, idle cause and return-dialog counts. It is the trust moment in minute one, and it lets us measure run 2.

**Below the line:** the research queue and points at the hub (S–M), the crew chip and names (S), tending that repairs
storm damage (S), link offers that wait for the other side's session (S), days in chat (S), the phone (S–M).
