# Round 1 · researcher

I read findings.json in full, all 28 diaries, every STUCK note, the group chat, the events, the director's notes and
the screenshots behind the big claims. I recomputed the money from `metrics.jsonl` myself (real gold growth between
6-hour samples), because several claims rest on it.

## What I see

### The problems, ranked by player pain

Pain = how many players, how long stuck, how often in the diaries' "three worst moments" (84 entries, about 75 real),
and whether "come back tomorrow" fell from yes to maybe. Of 13 STUCK notes, 4 come from the playtest's tools (Ben's
link, map and mouse drag, Cleo's `--nth`). Of the 9 real ones, 5 are dead ends.

| # | Problem | Who | How long | Signals |
| --- | --- | --- | --- | --- |
| 1 | **A building whose output can't leave sits Idle, and the screens blame its inputs** | Ana, Dev (Cleo via advice) | Ana's factory day 4–36, 5 sessions; Dev's dredger day 10–21, 3 sessions | 5 real STUCKs, 11 worst moments. Ana fell from "yes" to "maybe" on day 8 for good; fun 2 on day 21 (*"nothing to do that works"*). Dev's only "yes" and fun 4: the session he fixed it (d21-1900 #185) |
| 2 | **No way to knowingly help a friend** | All four in the crew | 36 days | 0 Asks, links, tends, pins. Sand in 24 of 37 group messages; Ben's guilt in 3 of 5 diaries; Cleo *"no payoff for helping"*. "Together" above 3 only once |
| 3 | **The Yard pays nobody, and once drained three friends** | Ana, Ben, Dev (money), Mia (probably), Cleo (guilt) | Drain days 6–14; no pay all run | Felt little, because invisible (Ana: *"+33/min, I remembered +198"*). Harm far above felt pain |
| 4 | **The project can't be reached, and nobody sees it** | The crew | Water Tower 0 from day 3 to 36; first opened day 21 | Seeing it gave the run's only "together 4" (ana d27-2000) and Ana's whole last-evening plan |
| 5 | **Three dialogs before every return, without the news** | All five | 51 clicks over 23 returns | 7 worst moments. The one screen everyone reads, and it carries neither friends, project nor stand-in |
| 6 | **Mia's first minutes** (seat-picker loop), then nothing says she's alone | Mia | 9 min, STUCK | Her worst moments 1 and 2 on day 1. A real stranger likely leaves here |
| 7 | **Loady stalls or contradicts the estate** | Ana, Mia, Dev, Cleo | Ana 32 days at 7/11; Mia and Dev 13 days | 10 worst moments. "Show me" is the most used help (17 clicks) |
| 8 | **The phone** (popup pile, the pipe tool, Chat under a panel) | Ben | 3 sessions | 1 real STUCK, 4 worst moments, from the least patient persona |
| 9 | **"Did it break?" in minute one** (+0/min, "Something is in the way") | Ana, Cleo, Dev, Mia | Seconds, first sessions | 5 worst moments, where trust is decided |
| 10 | **Silent storm losses** | Ana, Ben, Dev, Mia | Weeks, never repaired | Ana 228 → 185 g/min (−19%) after the storms of days 18 and 32; Ben 90 → 76 (−15%) after day 10, while away |

Lower: daily orders need a Storage (Ana, 1 STUCK); gold, research and the XP wall (Cleo, 4 worst moments); Max
(Cleo, once); "Riverside Co." vs Ben; chat without dates; overlays.

### What the players actually did

- **Ana did what Loady said, and that built the dead end.** The step reads *"Build a Small Factory and connect it to
  the mine and your energy"*. She did both (d04-2100 #107, #113). Its pipes reach only the mine and a windmill, so the
  ingots can't leave. The next step, *"Build a Storage on your pipes"*, would have cured it, but waits behind it. The
  network card calls it one network of *"8 buildings · 61 pipe tiles"* with the trading post, *"Energy 20 short"*
  (d36-2000-0289). She believed "the trading post sells first" for five weeks, demolished it as a test (d14-1900 #183)
  and chased a Priority Valve two researches away.
- **Dev's case was different.** His dredger and its moily made a 2-building network with no taker at all
  (d21-1900-0161). The panel's first line was right, but the bigger card said *"Energy 8 short"* and the moily *"makes 0
  of 27"*: he read "broken moily".
- **The friends had nothing to trade.** Every starter kit is energy (3 Wind Moilys or 4 Solar Panels), and Loady sends
  everyone to an iron mine next. No building of Ana's used sand; her ask was a story, not a need. The one shared buyer
  in week 1 is the project, and its Water Tower asked for sand, wood and stone, which nobody made.
- **Ben was the natural keystone maker, and nobody could tell him.** The keystone was stone; Ben wrote on day 3
  *"only found stone here"*. The card saying so was first opened on day 21. A Quarry needs Level 10; Ben was at 5.
- **The "Yard g/min" was never money.** Over all 35 days, every player's real gold grew at the hub rate:

  | Company | Hub rate | Real growth − hub rate | "Yard g/min" shown |
  | --- | --- | --- | --- |
  | Ana | 175.6 | +0.03 | 90.5 |
  | Ben (Riverside) | 54.8 | −3.3 | 37.5 |
  | Dev (Windy Heights) | 135.5 | −0.5 | 43.1 |
  | Mia | 90.9 | +0.15 | 32.4 |
  | Cleo | 634.4 | −2.3 | 0.0 |

  (Averages over the 6-hour intervals between sessions; the live sessions show the same.)

### Where I differ from the analyst

1. **The Yard bug is wider than the Lab.** No Yard sale reached anyone's gold at any time, Mia's 31 g/min "to Houses"
   included. The Lab week is the one where it also took the sellers' hub sales (Ben's hub rate went to 0).
2. **"Income doubled when she demolished the Lab" overstates it.** The station went 612 → 1,237 g/min, but about 240
   of Cleo's +311 came from her own upgrades that session (cleo d14-1900 #228, #231). The Lab's own effect was about
   +385 (Ana +150, Ben +75, Dev +89, Cleo +71): +63%. Still large: Ana lost two thirds of her income for eight days, Ben
   all of it.
3. **Row 11 (Mia at 85–90%) is inconclusive, not missed.** The crew earned nothing from trade either, so the gap
   measures effort (7 buildings against Dev's 10) and the Yard bug. Had the Yard paid what it matched, Mia would be at
   about 130 against Dev's 150 (87%).
4. **"Mia never got neighbours" was mostly the story.** The crew held all four seats of the only human station, so no
   placement could seat her beside a human, and the lone-human card rightly never came: it needs a livelier station
   with a free seat (`GameWorld.Return.cs:208`). The picker loop is a real bug; loneliness needs a free human seat to
   be tested.
5. **The dead end isn't one rule.** Letting goods pass through buildings fixes Ana, not Dev. The shared cause is the
   wrong reason on screen.
6. **Research is low, not medium.** Cleo's 179k points came from the bug that let a Lab at 3% make 15 points a minute;
   Ana wanted points for a valve that wouldn't have fixed her factory.
7. **Tending wasn't only unfound; it does nothing.** `Tend` (`GameWorld.Return.cs:412`) adds a line to the friend's
   return card. Make it findable and it gets one tap, never a second.

## Root causes (checked in the code unless marked)

- **Loady teaches the dead end.** Step `firstIngot` (`tutorial.yaml:17`) names the mine and the energy, not where the
  ingots go. It waits for `produced:ironIngot`, which can't happen; `firstStorage`, the cure, comes after it.
- **"Short" counts needs that can't be met anyway.** *Short* is `needed − used` (`NetworkSummary.tsx:17`), and *needed*
  is every building's full need (`GameViews.cs:970`), including a factory whose output is blocked.
- **The Yard sends goods to lacks that aren't real.** `Measure` (`GameWorld.Yards.cs:286–370`) counts a building's
  scarcest input as lacking, whatever really holds the building back (the Lab, capped by ingots; I guess also Ana's
  dead-end factory, whose row read *"Lacks 11 energy"*). The seller's export comes off its hub gold at once
  (`SplitGateFlows`, line 133) and is paid only for what the buyer's buildings take (`SettleGateFlows`, line 184).
  Why Mia's trades with Houses never paid is a guess for the engineer.
- **Baskets are sized by T(L), which is 3–10× what these players earned.** Nobody delivered "30 minutes of T(L)" to the
  Foundation (`GameWorld.Projects.cs:370`), so the Water Tower fell back to one resident (line 226): 6 h × T(6) = 6 × 60
  × 915 = 329k g, exactly 91.5k sand + 73.2k wood + 73.2k stone. Ana earned 196 g/min: 28 hours of her income, in goods
  she didn't make. The "740k worth" is 6 h × T(14) = 2,054 g/min against her real 213. T(L) fits the balance bot (92
  buildings on day 3, `balance-typical-together.md`); Ana had 8. The recipe comes from the seats' land (`HomeGoods`,
  line 156), not from what residents make.
- **Loady's chapter-2 Yard step counts the match, not the pay** (`NeighbourIncome`, `GameWorld.Tutorial.cs:55`). The
  simple moily-to-mine case does pay (`YardFacts.cs:59`).

## What I propose

I own the evidence and the test of success: the order of fixes, what each must achieve, and how we'll prove it. The
other roles own the mechanics and the words.

1. **Fix in the order of pain, and judge each fix by one question: would these five have done something different?**

   | Analyst's recommendation | Would it have changed what they did? |
   | --- | --- |
   | Fix the Yard's matching and pay | Their income, yes. Their behaviour, no: they had almost nothing to trade. Still first, so every fix that shows trades shows true ones |
   | Dead ends explain themselves | Yes, most of all: Ana read the panel every session, and a true first line is one command from the fix. Fix Loady's step text and order with it |
   | The project visible and movable | Yes: the only shared buyer in week 1, and seeing it gave the one "together 4". Recipe from what residents make; size from real income |
   | A visible path to "send to a friend" | Only with the project: no friend's building needed sand or copper, so an honest path ended at the hub |
   | One return card | Yes, all five, every return; and the only place the friends' news will be read |
   | Seat picker | Yes for Mia's 9 minutes, not for her loneliness |
   | Loady follows the estate | Yes for Mia (4/11 for 13 days) and Dev |
   | Surface tending | Only if tending does something (say, repairs a sleeping friend's storm damage) |
   | Guard big upgrades | Cleo once; with the Yard fixed the harm stays hers. Small, cheap |
   | Research, gold, sinks | Mostly no (below). Daily orders from production: yes (Ana's STUCK) |

   Effort: none. Risk: none.

2. **Instruments first**, so we measure gold, not displayed figures. `Playtest.cs` `Companies()`: `yardPaid` (gold from
   `SettleGateFlows` since the last sample) beside `yardMatched`; per player building `idleSince` and `idleCause`;
   project deliveries per good and company; return dialogs shown; and an event when a player's good first reaches a
   crew mate's building or the project. `digest.mjs` computes the measures below. Effort M. Risk: none for players.

3. **Keep the snapshots loadable**: database changes during this work are additive EF migrations, so `director.mjs
   restore row07|row08|row10|row14` keeps working. A step that can't is checked on a fresh world and says so. Effort S.

4. **Scripted replays from the snapshots after every fix step** (no agents; `e2e/replay-<row>.mjs`: restore, advance,
   assert). Effort M for the set.
   - **row10** (day 14 19:00) holds every middle-game bug at once: the level-35 Lab, Ana's dead-end factory, Dev's
     takerless dredger, Ben resting, the Water Tower at 0. Advance 6 h, act nothing. Pass: every seller's real growth =
     hub rate + `yardPaid` within 2%; Ben ≥ 70 g/min (run 1: 0.0), Ana ≥ 200 (77), Dev ≥ 120 (44–57); the Lab is
     matched no more energy than its ingots let it use (about 30/min); Ana's and Dev's panels give the dead end as the
     first line, and no network card calls them "short". Pipe the factory to the trading post: first ingot within 2 game
     minutes. Advance to day 27 20:00: Ana's return is one card naming Ben's stand-in and the storm damage, with a repair.
   - **row07** (day 6 14:00): replay Cleo's #105–#177 from `actions.jsonl` and press Max on the Lab. Pass: she sees the
     run rate on this network before paying. Force it: in 2 days no neighbour's real growth moves more than 2%, and the
     Lab's research follows its run rate.
   - **row01**: the first evening with Mia's race (Ana founds in Mia's station 20 s after Mia opens the picker). Pass:
     Mia's next Settle offers a free seat, never a closed station; the Foundation stands by 20:30 on kits alone.
   - **row14**: Ben returns on a phone. Pass: one card, at most 2 taps; Ana's card says Ben is back.

5. **Five agent replays** from the snapshots with run 1's own prompts (`prompt-*.md` carry each memory; the group chat
   cut at that moment). The memories carry wrong beliefs (*"the trading post takes everything first"*), which makes it
   a harder test, like a real returner. About 250k tokens.
   - Ana d14-1900 from row10: her factory runs within the session, no STUCK on it (run 1: never).
   - Dev d10-2000 from row08: the dredger runs the session he builds it; his diary says where the sand goes.
   - Cleo d06-1400 from row07: no Max into 3%; her diary names how her goods reach Ana or the project.
   - Mia d01-2000 with Ana from row01: seated within 3 minutes, no STUCK.
   - Ana d27-2000 (row10 advanced): her diary names Ben's stand-in and what the project wants from whom.

6. **The second full run, once the top five fixes pass their replays.** Effort L (about 1.3M tokens, 70 min of agents).
   - The same story (§17a and §17b, 14 rows, 28 sessions), personas, devices, budgets and 5× clock, so numbers compare.
     One build, no republish; if a hotfix can't wait, restart from the last snapshot and write it down.
   - A snapshot before every row.
   - **Jo**, a scripted solo human (no agent) seated before row 1 in a Capital station with free seats, with a factory
     that lacks energy, logged in daily. Without Jo, §17b (*"Placement seats Mia beside Jo"*) can't be tested.
   - `digest.mjs` computes the measures, not the analyst. Only in each player's last session, after play: "Who buys
     what you make, and what did you give your friends?"
   - Pass marks, fixed now. AI players are patient, so a real player gives up sooner: the marks are strict.

   | Measure | Run 1 | Run 2 passes if |
   | --- | --- | --- |
   | **First good knowingly given**: a crew member names, in diary or chat, a good of theirs that reached a friend or the project, and `yardPaid` or the basket confirms it | Never | By day 2 20:00 for one pair; by day 14 for every crew member with 3+ sessions |
   | Yard honesty: real growth − hub rate = Yard gold shown, per sample | Shown 32–90, paid ≈ 0 | Within 2%, every sample |
   | Harm from a neighbour: a >10% fall in real income without the player's own action | Ana −83%, Ben −100% | None |
   | Dead ends: owner sessions a building stays Idle without the owner knowing why | Ana 5, Dev 3 | At most 1; no dead-end STUCK |
   | Real STUCK notes | 9 | ≤ 3, none on dead ends or seats |
   | Project | Foundation day 3 14:18; Water Tower 0% to day 36; first seen day 21 | Foundation by day 1 20:30; Water Tower ≥ 50% by day 8, done by day 14; opened by row 4; keystone made by a crew member |
   | Return | 3 dialogs, 51 clicks over 23 returns | 1 card, 1 click, friends' news on it when there is some |
   | Absent friend | Ana never learned of the stand-in | Ana's day-27 diary names it |
   | Mia | 9 min to a seat; "alive" 3/5 in all 4 sessions | Seated within 3 min beside Jo; alive ≥ 4 twice; ≥ 85% of Dev's real income |
   | Come back tomorrow, crew, day 8 on | 1 yes in 13 | Yes in at least half |
   | Silent losses | Ana −19%, Ben −15% | Repaired, or on the next return card |

7. **What would change my mind**, decided now:
   - Ana's replay still ends with an idle factory despite the true first line and a map mark: then the rule is the
     problem. Let a building pass on goods it doesn't use, or lay the missing pipe for her.
   - Run 2 pays and shows trade honestly, but nobody knowingly gives by day 8: then giving needs an act (an Ask, or
     "I make it" from the building panel).
   - The project is seen and asks for what residents make, but is under 50% by day 8: then the size is wrong.

## What I'd not do

- **A "Send to Ana" button that moves goods without a pipe.** It breaks principle 5 and opens a funnel. The pipe to the
  gate should be the act of giving.
- **Gold buying the project's missing goods.** It makes the project a gold check and a funnel, and takes away the reason
  to build the keystone's Quarry.
- **Changing the pass-through rule now.** Try the true explanation first and test it with Ana's replay. Dev's case
  wouldn't change anyway.
- **Hiding the Yard's contradictions, or adding Yard income to the top bar, before the Yard really pays.** That hides
  the bug.
- **Promoting tending while it does nothing.**
- **Reworking research or adding gold sinks because of this run.** Cleo's points were a bug. Gold piled up because
  players couldn't turn it into anything that worked (8–14 buildings against the bot's 92 by day 3). Re-measure after
  fixes 1–4.
- **Tuning trunks, outposts, leagues or Wonders from this run.** Nobody reached them.
- **A crew channel in the game chat.** Nobody asked; the friends talked fine outside (37 messages).
- **Fixing the AI's habits**: tile-number aiming, clicks behind dialogs, `--nth`, the 30-message replay. Fix the shared
  button names as an accessibility bug.
- **Longer texts.** Ben reads only the first words and Dev doesn't read. The cause belongs in the first five words and
  on the map.

## Questions for others

- **Engineer:** (1) Did any Yard sale settle in this run? I find none, for anyone, live or after a jump. (2) Did Mia's
  energy really leave for the Houses (lowering her hub sales), or was it never sent? (3) Does Ana's dead-end factory
  make a Yard lack? (4) Why did 150 iron ore take 18 hours to reach the Foundation after the first mines? (5) Can every
  fix keep the snapshots loadable?
- **Economy:** T(L) is 3–10× what these players earned (Ana at Level 14: 213 against 2,054). Will baskets, allowances
  and the other T(L) amounts follow what players of a level really earn? And is 85–90% measured on paid income at equal
  effort?
- **Clarity:** What first line do the factory and the dredger show, in five words or fewer, and what mark on the map?
  Where does Loady's factory step send the ingots? What goes on the one return card for a phone, in what order?
- **Together:** In week 1 nobody had a need a friend could fill. What gives the crew complementary needs on evening one
  (the recipe, the seat's goods, the kits)? Can the game say "your sand reached the Water Tower", so a player knows they
  gave? That event would make my main measure visible in the game itself.
