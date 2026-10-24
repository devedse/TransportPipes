# Round 3 · researcher

I read the draft plan in full, and all five round-2 papers. The plan takes almost everything the evidence supports
and drops what it doesn't. My amendments are about order, about where a measure was missing, and two places where the
plan departs from what the players did.

## Verdict per item

- **F0 · amend.** Add a **row08** scripted replay to the list (F2 names it, but F0 doesn't build it). Add the metrics:
  untaken per seller, `damagedSince`, owner play minutes since a building went idle, return clicks, toasts on a phone
  screen, bank share spent per session, income per command and per building, and the first session in which a player
  sees the Next steps. Every replay must first **fail on the old build** (at row10: Ben 0.0 g/min). Otherwise we don't
  know it can catch the bug.
- **F1 · amend.** Move the game function `IdleReason` and its tests (no UI) into F1. F1.2 caps lacks by it ("0 when
  `IdleReason` is stuck"), and the plan ships it in F2, after F1. Add the pass mark **untaken ≤ 5% of what a seller
  sent, after a match's first hour**. That makes the floor's "revisit if the gate dodges the hub's √" measurable.
- **F2 · accept.** Add one test: a full Storage or post reads "full", never "stuck" (clarity's risk: a false "stuck"
  is worse than today).
- **F3 · amend.** The kit counts **from step 3**, not "for every step but the first". Engineer E9 and clarity's round
  2 both say so. Step 2 is where a player learns to pipe: Mia's first pipe was Loady's step 2 (mia d01-2000 #55), and
  Ben's one real STUCK was that drag (ben d03-2000 #60). If the kit completes step 2, the player's own first panel
  stays unpiped and earns nothing, and nothing says so: a new silent dead end.
- **F4 · amend.** The row07 replay acts for nobody, so "Water Tower ≥ 50%" is the wrong assert. Assert instead: the
  goods the station already makes reach 100% within 3 days of the restage; the keystone stays at 0; and its card names
  who can make it. Add a sweep test over every station of the world seed (every recipe good can be made by a resident
  by Level 5). Then the rule isn't graded only on Capital NW.
- **F5 · accept**, with together's rule that no line scolds (*"nobody here has sent any yet"* goes). The Invest line
  as voted.
- **F6 · amend.**
  1. The first-gift event, and the "knowingly given" measure, count a delivery into the **project** too (it has one
     end). In week 1 the project is the only honest buyer.
  2. **Object** to "`firstYard` counts only the player's own pipe". Together withdrew it in round 2. No player's pain
     came from the kit doing that step. And it adds a long drag to a far gate at step 5 on Ben's phone, where his only
     real STUCK was a drag. The door lesson already lives on the producer's panel and in "Neighbours".
- **F7 · accept.**
- **F8 · accept**, including no 25% confirm. A useful Max removes Cleo's harm, and a size confirm would fire on the
  spends we want.
- **F9 · amend: it has no verification.** Add `StandInFacts` (the 14-day median of paid flows) and a replay: row10 to
  day 36, with a scripted tend in each Ana session. Ben's card must say "tended on N days, M moilies repaired", and
  Ana's day-27 card must say "Riverside Co. (Ben)".
- **F10 · amend: it has no verification.** Add `TheIncomeIsRightStraightAfterAnUpgrade`; `mobile.mjs` and
  `evening.mjs` (at most one toast on a phone); `touch.mjs` (the first pipe after Show me); a chat-date test across
  midnight and a DST change; and `ui-check.mjs` (no two buttons share a name).
- **F11 · accept.** The lone-human card only offers a link for a real spare–lack pair, which answers my round-2
  objection.
- **Dropped · accept** all eight.

## VOTE A

**As proposed.** "Show me" opening the best upgrade, one building at a time, is the lever players lacked: 21 of 23
sessions spent under 3% of the bank (economy). A one-tap spend of half the bank is an unseen spend, the very thing that
hurt Cleo (d06-1400 #177), and it moves every income at once. Condition: it is the lowest of the card's six lines, and
it shows only for an upgrade that pays back within 2 h on the building's own supply.

## My last dissent

**The agent replays must be gates, not extras.** The plan makes every fix pass its tests and scripted replay before
the next ships. It gives the agent replays no place in that order. But run 1's worst failure would have passed every
code check. The factory panel's line was *correct*: *"Nothing on its pipes takes its iron ingot: connect your trading
post…"* (GameViews.cs:874). Ana read it in five sessions and still chased energy for 32 days (*"confusing since it IS
connected to the trading post"*, d36-2000 #289). A test checks that the line is true. Only a person, or an agent
playing one, checks that it is understood.

So:
- F2 and F3 ship only after Ana's two replays and Dev's pass.
- F5 ships only after Ana d27, F7 after Mia d01, F8 after Cleo d06.
- Run 2 waits for all six sessions (about 300k tokens and 25 minutes of agents).
- Under time pressure, cut from F9–F11, never the replays.

## Top 8

1. **F2 · Dead ends explain themselves.** The most pain: 5 of 9 real STUCKs, 32 + 11 days. And the surest change: one
   click at ana d04 #122 and dev d10 #101.
2. **F1 · The Yard is honest** (with `IdleReason` moved in). Ana lost two thirds of her income and Ben all of his for 8
   days, and nobody was paid in 36. Nothing that shows trade can ship before it.
3. **F4 · A project the station can reach, on the "This week" row.** The only shared buyer in week 1. Its first look
   gave the only "together 4", and that row was read in every session.
4. **F5 · One return card.** All five players, every return (51 clicks). The only place the absent friend's news gets
   read.
5. **F3 · Next steps for everyone, and Loady follows the estate** (kit from step 3). 4 of 5 players never saw the
   advisor, and Ana, Mia and Dev sat on one step for 13–32 days.
6. **F6 · Trade with a face, and things a friend needs.** It answers Dev's three "how do I send sand", Cleo's copper and
   Ana's Lab, in the panels they opened.
7. **F7 · The seat picker**, with F10's chat dates. Mia's STUCK in minute one, and the chat was her only sign of life.
8. **F10 · Trust in minute one and the phone.** 5 worst moments in first sessions, and Ben's pipe STUCK.

F8, F9 and F11 follow. F0 comes before all of them.

## Appendix: verification

### A1 · Final pass marks (the plan's table, amended)

| Measure | Run 1 | Run 2 passes if |
| --- | --- | --- |
| **First good knowingly given**: a good of the giver's reaches a crew mate's building or the station's project, **paid** (`yardPaid` or project credit), its first-gift event shows on the giver's first screen (and the receiver's, for a pair), and the giver names it in a diary, note or chat | never | one by day 2 20:00 (row 4); every crew member with 3+ sessions by day 14 |
| Yard honesty: real growth − hub rate = paid Yard shown | 32–90 shown, ≈ 0 paid | within 2%, every sample, every player |
| Untaken: what a seller sent that nobody took (new) | 99%+ during the Lab | ≤ 5% of sent, after a match's first hour |
| Harm from a neighbour: a fall of more than 10% without the player's own action | Ana −83%, Ben −100% | none |
| Dead ends: owner play minutes from stuck to running, or to a diary that names the true cause (was "owner sessions") | Ana never, Dev 3 sessions | ≤ 10 minutes; no dead-end STUCK |
| Real STUCK notes (excluding the listed artifacts) | 9 | ≤ 3, none on dead ends or seats |
| Project | Foundation day 3; Water Tower 0% to day 36; first opened day 21 | Foundation by day 1 20:30 in both stations; Water Tower done by day 8, Workshop by day 14; every crew member opens it (row or tab) by row 4; the keystone made by a crew member's own building |
| Return | 3 dialogs, 51 clicks | 1 card and 1 click (≤ 2 taps on a phone); friends' news on it whenever the events hold some |
| Absent friend | Ana never learned of the stand-in | Ana's day-27 diary names Ben's stand-in; Ben's day-36 card names tended days if anyone tended |
| Mia | 9 min to a seat; "alive" 3/5 four times | seated within 3 min beside Jo, no STUCK; alive ≥ 4 in 2 sessions; 85–110% of Dev per building and per command |
| Trade share t between humans | 0% | ≥ 15% by day 14 |
| New gold from NPCs (new; economy) | not measured | ≤ 6% of faucets |
| Come back tomorrow, crew, from day 8 | 1 yes in 13 | yes in at least half |
| Silent losses (storms) | Ana −19%, Ben −15% | no building damaged across 2 owner sessions without a repair or a line on the return card |

Diagnostics, with no pass mark (read them, don't optimise them): bank share spent per session, income per command and
per building, the first session with the Next steps, toasts on a phone screen, return-card lines used.

### A2 · Scripted replays (`e2e/replay-*.mjs`: restore, steps, asserts; no agents)

**row01 (day 1 20:00, empty world): the first evening and Mia's race.**
1. Restore and pause. Browser M (Mia): Play now → Find me neighbours (the picker shows Capital NW).
2. 20 s later, browser A (Ana): New player → Start with friends → "Tea Time Pipes" → Settle and invite (ana #4–#14).
3. M: Settle here.
4. B (Ben, 390×844) and C (Cleo) open their seat links → Move in → Done. Ben taps Show me on step 1, then the yellow tile.
5. Resume at 5× for 30 game minutes; read the top bar right after every command.

Asserts:
- M is refused with `seatTaken`, the picker reloads with a named next seat, and M's next Settle works. M never sees a
  closed station. Settle is disabled on unpickable seats. M is seated within 3 minutes, in a station of four.
- Handshakes at move-in. Both stations' Foundations stand by 20:30.
- The Water Tower's goods are all made or makeable by Level 5, with makers named, and its basket is ≤ 6 h of summed
  I(c). It shows on the "This week" row.
- On Ben's phone: at most 1 toast, no level-up before step 1 is done, and Show me on step 2 picks the Pipe tool.
- Never "+0/min". Never "Something is in the way" after a placement that worked.

**row07 (day 6 14:00): Cleo's Max, and the project restaged.**
1. Restore; `project restage`.
2. Script cleo #105–#176 from `actions.jsonl` and open the Lab.
3. Assert the button reads "Max +N · price", with N the useful level (about +8, 27.5k), and the reason line under it.
4. Choose "Level 35 anyway". Advance 2 days. Run a control branch without step 4.

Asserts:
- Ana's and Ben's real growth stays within 2% of the control's.
- The Lab is matched under 1 energy/min.
- Its research reads "15 of 509/min".
- Advance to day 9: the goods the station already makes reach 100%, the keystone stays at 0% with its makers named,
  and Ben's part comes off once he has been away 3 days.

**row08 (day 8 20:00): Ana's factory, Mia's step.**
1. Restore. Open Ana (her `storage.json`).
2. Assert her return card: one card. "To look at" names the Small Factory stuck since day 4. "Your crew" names Ben
   resting, with Tend.
3. Open the factory. Assert:
   - the pill "Stuck: ingots can't leave";
   - "Its pipes reach only: Iron Mine, Wind Moily";
   - one button to the Trading Post;
   - no "short" on the network card;
   - the goal tracker alert;
   - Loady's tip on 7/11.
4. Click the button. Assert: it runs within 2 game minutes, the first ingot is made, and Loady moves on to the Storage
   step.
5. Open Mia. Assert: she is no longer on 4/11, and the Next steps show.

**Dev's d10 (row08 + his d08 scripted).**
1. Restore row08 and script dev d08 #3–#56 (seat link, Move in, Done, moilies). Advance to day 10 20:00.
2. Script dev d10 #72–#97 (Deposits, buy the shore parcel, the dredger, the pipe from the moily).
3. Assert in the same session:
   - the dredger reads "Stuck: sand can't leave";
   - the moily reads "Idle: Sand Dredger is stuck";
   - no "Energy 8 short".
4. Click the button. Assert: the dredger runs within 2 game minutes. If sand is in the basket, the button led to the
   gate, the project's sand from Dev is above 0 within 20 game minutes, and the first-gift event shows.

**row10 (day 14 19:00): every middle-game bug at once.**
1. Restore. Advance 6 h with no actions. Assert:
   - Ben ≥ 70 g/min, Ana ≥ 200, Dev ≥ 120, Mia ≥ 90;
   - each equals hub + paid Yard within 2%;
   - the Lab is matched under 1 energy/min, and untaken is ≤ 5%;
   - the Glassworks House has a post;
   - Cleo's Yard row shows no energy lack while energy is spare.
2. Run the factory asserts of row08 here as well, and the dredger's stuck pill.
3. `project restage`, then click Dev's button. Assert as in Dev's d10.
4. Branch A: advance to day 27 20:00, no actions. Assert Ana's card is one card, names "Riverside Co. (Ben)"'s
   stand-in, and lists every damaged building of hers with Repair all.
5. Branch B: script a Tend by Ana on days 15, 21 and 27, and advance to day 36 20:00. Assert Ben's phone card: "tended
   on N days, M repaired", at most 2 taps to the estate.

**row14 (day 36 20:00, the old world): Ben's return.**
1. Restore. Open Ben on the phone, then Ana.
2. Assert:
   - Ben: one card, at most 2 taps, "33 days" from the server's clock, the stand-in named;
   - Ana: "Riverside Co. (Ben) is back";
   - new neighbours listed once.

### A3 · The agent replays

Each uses run 1's own prompt file (it carries that day's memory), pointed at a new run folder `story1-replay`, with
the group chat cut at the session's time and the player's `storage.json`. Each one is read for STUCK notes, for "Where
I got stuck", for "Together", and for "come back".

| Session | Prompt | Start | Passes if the diary and metrics show |
| --- | --- | --- | --- |
| Ana d08 | `ana/prompt-d08-2000.md` | row08 | the factory runs within the session (`idleCause` clear); no STUCK about it; she names the true cause (its pipes, not the trading post "selling first") |
| Ana d14 | `ana/prompt-d14-1900.md` | row10 | the same, despite a memory that says "the trading post takes everything first"; she doesn't demolish the post |
| Dev d10 | `dev/prompt-d10-2000.md` | row08 + his scripted d08, advanced to day 10 | the dredger runs in the session he builds it; his diary says where his sand goes and how he sent it |
| Cleo d06 | `cleo/prompt-d06-1400.md` | row07 | no Max past the useful level; she names how her goods reach Ana or the project; she mentions the project on the "This week" row |
| Mia d01 with Ana d01 | both `prompt-d01-2000.md` files, in parallel | row01 | Mia seated within 3 min, no STUCK, and her "Other people" describes her station truly; Ana's crew and links as in run 1 |
| Ana d27 | `ana/prompt-d27-2000.md` | row10, branch A, at day 27 | one return card; she names Ben's stand-in by Ben's name, and what the project wants from whom |

If Ana's two sessions fail after F2, the plan's own trigger fires: change the rule (pass-through, or lay the pipe for
her) before F4 ships.

### A4 · The second run

- **Build:** one commit with F0–F7 at least (F8 if it's done), frozen. A hotfix restarts the run from the last
  snapshot, and the restart is noted.
- **Story:** the same 14 rows, 28 sessions, persona cards, devices (Ben on a phone, skimming), budgets, 5× clock,
  calendar and world seed. Row 2 is testable now, because the driver is fixed. A snapshot before every row.
- **Jo:** a scripted human account (no agent). The director creates it at day 1 19:00 through the driver, by "Find me
  neighbours" on the quiet server, so it lands where placement puts the first stranger. Jo has a Small Factory and an
  Iron Mine on its gate network with no energy of its own, plus a trading post. So Jo lacks energy, which is what Mia's
  kit makes (§17b: Mia's spare sells to Jo by minute 12). The director opens and ends Jo once a day, at a different
  hour, so Jo counts as active. Jo never builds again and never chats: no fake person in the chat.
- **Measures:** `digest.mjs` computes table A1 from the F0 metrics. The analyst gets the table, a list of "known, not
  fixed", and the usual prompt, and writes the story.
- **Exit question:** in each player's last session, after `end`: "Who buys what you make, and what did you give your
  friends?"
- **Beside the run:** the equal-bot balance run (a solo bot in a House station against a crew of four of the same bots)
  for the 85–90% promise.
- **Cost:** about 1.3M tokens and 70 minutes of agents. The replays cost about 0.3M.

### A5 · The order: what must pass before the next ships

| Step | Ships | Must pass before the next step |
| --- | --- | --- |
| 0 | F0: metrics, `project restage`, the replay scripts | every replay fails on the old build exactly as run 1 did |
| 1 | F1.1, the floor | row10 +6 h: the four income marks; `SellingThroughTheYardNeverPaysLessThanTheHub` |
| 2 | `IdleReason` (core) + F1.2–F1.5 | row10: Lab < 1/min, untaken ≤ 5%, Mia ≥ 90, the House's post; the conservation and speed tests |
| 3 | F1.6, screens show paid | row10: top bar × 10 min = the gold change; no matched figure on any screen |
| 4 | F2 + F3 | row08, row10 and Dev's d10 scripted; `deadend.mjs`, `guide.mjs`; **agent: Ana d08, Ana d14, Dev d10** |
| 5 | F4 | row01 (both Foundations by 20:30), row07 restage (100% in 3 days), the sweep test |
| 6 | F5 | `return.mjs`; row08 card; row10 branch A; row14 phone; **agent: Ana d27** |
| 7 | F6 | `yard.mjs`; row10 step 3 (Dev's sand into the project in session) |
| 8 | F7 | `seat-race.mjs`; row01 Mia; **agent: Mia d01 with Ana** |
| 9 | F8 | row07 Max asserts; **agent: Cleo d06** |
| 10 | **Run 2** | all of the above green |
| 11 | F9–F11 | their tests; row10 branch B. They can go before or after run 2; if they're ready, they ship before it |
