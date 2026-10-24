# Round 2 · the systems designer (`systems`)

I read `r1-player.md` and `r1-social.md` and checked each claim of theirs that I adopt or dispute against the logs or
the code. Session and action numbers are as in the digest. Two checks changed my own round-1 view:
- **The Ask engine is already the buyer I proposed to build.**
- **Cleo's Coal Pit was limited by its output, not by energy.**

## 1. What I take from the others

**From the player researcher:**
- **The contract (their rows 1–3):** nothing is suggested, called "you can" or offered as a fix unless the build
  command's own check and the network what-if say it works.
  - It contains my round-1 row 5 (Max counts the pipe), which I fold into it.
  - I checked their best new case. Mia's Water Pump was `Stuck` (nowhere to go) from day 2.85. The game's own fix
    button ("Lay the pipe · 8 g", mia d08 #96) turned it into `InputMissing energy` from day 8.85 to the end (metrics
    `idle`). So the fix moved the pump from one dead state to another, and no card said so.
- **The truthful project line (their row 1).** "Never '(nobody here yet)' when a resident's land allows it." When
  your own building makes the waited-for good and isn't running, that leads your card.
- **The Era wonder in the tracker, and its end told (their row 5).** I checked "one tap away": the top bar's
  "Era 1" button does open Hub › Wonders (cleo d10 #271). But Deliver was disabled for anyone without bricks in a
  Storage (dev d21 #178), so the line fixes visibility, not supply.
- **Their marks 1 and 3.** Late-game fun and "come back", and no standing line. I verified "yes 1 of 6 from day 18".

**From the social designer:**
- **The door (their rows 1 and 7, the player's row 4)**, adopted whole.
- **Ask and answer (their row 3) as the one redesign, merged with my round-1 "sites buy materials".** The engine
  exists (GameWorld.Asks.cs):
  - `MatchAsks` runs in every station's Yard round.
  - Neighbours' spare goes to an Ask first, even while they are away (only resting sellers are skipped).
  - A House fills what is left at the deadline at 2.0× base.
  - It is unused because the only door to it is a form under the Plan board (PlanBoard.tsx:66).

  What I proposed as new machinery (10–11 days) is mostly a destination change: into the build, not a Storage. So
  their 5 days plus about 1 is right.
- **The Wonder fed by a flow tick, with no Deliver button (their row 4).** This is my round-1 row 4 in their words.
- **A running gift stays first, and both ends hear when it falls (their row 5).** I adopt it, with a correction to
  its cause (section 2).
- **Seat the stranger beside a crew that plays (their row 8)**, merged with my Capital W row.
- **Station and Crew channels, and a crew line for the skimmer (their rows 6 and 10)**, merged with the player's
  row 9.
- **"A House's quarter never counts."** Both of us arrived at it. I checked the Yard: Capital Waterworks S, a House
  with "57 water spare", is in Ana's Yard (ana d01 #65). So the House probably supplied the water that froze Capital
  SE's Workshop.

## 2. Where I disagree, and why

**The keystone rule: mine, sharpened to a pace test.** "No *new* delivery for 2 days" lets a trickle reset the clock
forever, and run 3 had trickles:
- Ben's Send moved "0.6/min 💛" into the Water Tower (ben d02-0800 #70).
- Ana's pump at 57% added 13 water on day 36 (1,990 → 2,003), because its water went mostly to her trading post
  (ana d36 #388).

The rule I pick: each day, if the last 2 days' player deliveries wouldn't finish the keystone within 2 more days at
that pace, it is passed over. A House never counts, and with nobody active the stage waits.

After a pass-over:
1. What was delivered stays paid and credited.
2. Every resident's next card says it once: "Water is no longer needed: the Workshop waits on bricks now".
3. The next keystone comes from the chain rule (row 8).
4. A passed keystone doesn't come back in that stage. The social designer would let it return; I decline, because a
   moving target is what Mia couldn't follow ("nothing told me it no longer matters", mia d08).
5. After a second pass-over, the stage stands on its made goods.

The worst case is 4 days per stage while somebody plays (run 3: 27).

**Which redesign, if only one: the social designer's Ask and answer, built as the buyer on the pipes. Two of the
three positions are one thing.**
- **Theirs** gives the felt verb: a friend taps, the goods go straight into the build, both ends are named, and
  Thanks follows. It also gives gold its only social use, "Pay for it" (a House at 2.0×, the gold leaves the game).
- **Mine** adds that an Ask fills from the station's spare by itself. That matters with 4–9 days between sessions:
  the one chain friends felt, Ana's stone in Cleo's Brickworks, ran while both were away.
- **Hub daily orders out of Storage** leave the redesign and become a later fix.

I don't back "no redesign". The player researcher's own condition for changing their mind is diaries that name
"nothing to want", and run 3 already has them:
- "Gold is 2.3M and the best thing I could buy cost 15k" (mia d08)
- "365k gold and no clue what to spend it on" (dev d10)
- "3.5M gold has little to buy" (cleo d06)
- "the gold feels empty" (mia d15)

The design's own advisor balance run is flat from day 4 with no texts involved. And under the contract alone, a step
whose bricks nobody in the crew can make must wait. That hides the Big Factory, the Market Hall and every Era 2
building from anyone without both stone and coal, which turns dead ends into absences. With Needs as Asks, the
contract can be kept honestly: "Big Factory · 12k g + 100 bricks" is true, because the bricks will come.

**Retiring "paid trade ≥ 15%": yes.** I proposed the same.
- The crew's paid share was 0.0%, 1.3% and 0.7%. Iron, the best chain, is on every estate (WorldGenerator.cs: "Energy,
  iron ore and water every estate has"), and the light bots found no content lever.
- The new mark is "goods that built something". With Asks, it is the same thing as the social designer's answered
  asks: a friend's goods are in your build, named at both ends.
- Keep the paid share as a watched number that must not fall below run 3, to catch a broken Yard.

**Two corrections:**
- **"Lay a second grey pipe" (the player's row 6) wouldn't work.** A link is one per producer–consumer pair, along its
  best path, with the path's lowest throughput (GameWorld.Networks.cs `Trace`). Its capacity is used per link
  (FlowSolver `CapacityLeft`), so a parallel pipe adds nothing.
- **Cleo's Coal Pit wasn't short of energy.** Its own panel (cleo d21 #393) says three things: "Makes Coal 126 /
  882/min", "to Trading Post 100 coal/min", and "+34 would make nothing more: nothing on its pipes takes more coal".
  - The grey link to her trading post caps the output, and the solver gives the pit only the energy it can use. The
    card blamed energy for 11 days, and her Moily and Copper Pipe aimed at the wrong thing.
  - The L25 upgrade (117k, cleo d06 #184) bought 882 a minute of coal she could sell 100 of. `LevelReach` ignores link
    capacity on outputs as well as inputs.
  - What works: a second buyer on its own link (a second Trading Post, 2,500 g, +100 a minute), a better pipe tier,
    or row 11's pipe bonus. The idle reason must name the binding limit.

**The cause of Cleo's stone drop (the social designer's row 5).** On day 22.06 her import fell 55.8 → 0.8 a minute.
In the same sample Ana's Quarry turned `InputShort energy` and her Storage stopped being Full (metrics). That came
right after Ana upgraded her second Small Factory (#293) and evolved her Storage into a Warehouse (#302). The samples
can't separate the two causes, so the card must take its cause from the solver, not assume the Warehouse.

**The chain keystone isn't held back.** It is my row 8, at "next": the pass-over bounds the slowdown, and a station
with no pair falls back to today's rule. The run's only "together 4" had this shape (cleo d14).

## 3. The final table

| # | Improvement | Fix or redesign | Answers (evidence) | Effort (days) | Priority | Proposed by |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | **The keystone always moves on.** The pace test above. A House never counts. A passed keystone is announced once and never returns in that stage. After two pass-overs the stage stands on its made goods. With nobody active it waits. | Fix | **The freezes:** Capital SE sat at 96% from day 9.85 to 36 (water 1,990), and Capital NW at 89% from day 6.13 (1,855). Both are about a House's cap of 1,800 (`npcCap` 0.25).<br>**The code:** `Delivered < 1e-6` (GameWorld.Projects.cs:433).<br>**Trickles:** Ben's 0.6 a minute (ben d02 #70) and Ana's 13 water (ana d36 #388).<br>**Player:** Mia's STUCK (mia d15 #141). | 1.5 (with a replay from row 9) | now | systems, social |
| 2 | **Lines name the real limit and the way through.**<br>• The This week line, the card and the Project tab say one thing: "Workshop 96% · 9 days on water · you can: a Water Pump beside your pond · Show me".<br>• A reader's own idle maker leads their card.<br>• Idle reasons name the binding limit, output or input ("its trading post takes 100 coal a minute over this pipe · a second Trading Post here: +100/min").<br>• Send lines say what moved this week.<br>• The bell carries only what you can act on. | Fix | **"(nobody here yet)"** on 6 crew cards (ana d21 #260, d27 #317, d36 #357; cleo d14 #313, d21 #391; dev d14 #105).<br>**Mia's pump:** `InputMissing energy` from day 8.85 to 36 (metrics), with nothing on her cards.<br>**Cleo's Coal Pit:** at 12–14% "energy short", while its own panel says its coal can't leave (cleo d21 #393).<br>**Events:** "I can't tell what matters" (ana d36). | 3 | now | player, social, systems |
| 3 | **Offer only what works.**<br>• "You can", parcel steps, Next steps, fix buttons and Max all pass the command's own check and the network what-if first.<br>• `Makers()`/`FitsOnEstate` follow dealt land's deposits.<br>• `LevelReach` counts link capacity in and out.<br>• A step names every cost ("12k g + 100 bricks").<br>• Refusals teach ("A pump stands beside the water") and the ghost hops to a tile that works. Tiles in hand are rimmed.<br>• Max and Storage show the net change before the click.<br>• On a phone, the folded step is itself the button; Escape closes Build and chat. | Fix | **Forest parcels without wood:** two STUCKs (cleo d06 #232, #244).<br>**The Big Factory's bricks:** cleo d10 #281; ana d21 #286 STUCK.<br>**"Not on water" three times:** ana d27 #348 STUCK.<br>**The Copper Pipe no-op:** cleo d21 #404 STUCK.<br>**Upgrades that hurt:** the Lab's Max 783 → 285 g/min (cleo d06 #180); the Coal Pit's L25, 882/min with 100 sellable.<br>**The phone:** Ben's step with nothing to tap (ben d36 #105 STUCK). | 5 | now | player, systems |
| 4 | **Find your friends.**<br>• "A friend sent you a link?" comes first on the start screen.<br>• A crew takes one name field.<br>• A seat kept for you shows on every return until you answer: "Join and move · Not with them".<br>• The inviter sees "Ben opened your link but lives in Capital SW · Send again · 👁 Visit".<br>• Friends outside your station are listed in the Crew and Links panels.<br>• "Copy Ben's link". | Fix | **Ben was lost in 33 s:** "Leave your crew first." (ben d01 #29).<br>**He asked twice:** "which station am i in?" (group, day 1 22:58 and day 2 07:58).<br>**Ana:** STUCK (ana d01 #68); six promised visits; "where do you live?" (group, day 21).<br>**Ben, day 36:** "Everyone has been asking where I am". | 2.5 | now | social, player |
| 5 | **Needs become Asks (the redesign).**<br>• A material cost (a building, an upgrade, a pipe tier, an Era 2 building), the keystone and the crew's wonder share raise an Ask by themselves, shown to the friends and neighbours who can meet it.<br>• It fills four ways: a friend's one tap from storage (straight into the build, named at both ends, 💛 Thanks); the station's spare through the Yard, by itself and while friends are away (`MatchAsks`); "Pay for it" from anyone's bank (a House at 2.0×, gold leaves the game); a House at the deadline (12 h).<br>• Limits: 3 open asks per player; game-raised asks go to the crew and station only.<br>• It replaces "N materials in your storages", the Plan board's Ask form and Deliver for Wonders. | Redesign | **Materials nobody could hand over:**<br>• "I don't know how to hand you 100 bricks directly" (group, day 21).<br>• Ana's bricks ask (ana d21 #274, #290); Dev's "how do I hand ingots to the Dynamo?".<br>**Gold with nowhere to go:** offered four times; the crew held 95–100% of what it earned.<br>**The code:** `Pay()` takes only from your own Storages; hub imports feed only building inputs.<br>**The Ask engine** (GameWorld.Asks.cs) was used 0 times in three runs.<br>**What's behind materials:** Market Hall (500 bricks), Steel Mill (300), Cement Kiln (200), Big Turbine (steel). | 6 | next (before run 4) | social, systems, player (row 11) |
| 6 | **The Era wonder: the Capital's project, in sight.**<br>• A tracker line with your part ("Great Dynamo 41% · your bricks go in").<br>• A tick sends a flow to it, as for the project; no Deliver.<br>• Sized at each stage's start from 6 h of the Capital's spare of its goods.<br>• Tiers pay hours of your own income.<br>• Every card tells how it ended. | Redesign (with row 5) | **The size:** 6 h × 60 g/min at world creation (`StageRequirements`, `MinMeasuredIncome`) = 1,800 bricks and 900 ingots, which is 33 min of Cleo's 55 bricks a minute (cleo d21 #396).<br>**Still 0 / 1,800,** and 500 bricks in a full Storage (#423).<br>**Rewards:** the tiers pay 2k–100k × Era (`TierReward`).<br>**Players:** "ended without me knowing" (ana d27 #318); "late and useless" (dev d21). | 3 (the line alone 1) | next (the line now) | all three |
| 7 | **A stranger next to a crew that plays, and stages kept.**<br>• "Next door to a crew" counts residents active in the last 3 days, not held seats.<br>• A small station that leaves a shared Yard keeps the stages it stood with. | Fix | **Mia-B's neighbours:** a crew of one plus three held seats (mia2 d01 #10–#15).<br>**The restart:** Capital W went back to the Foundation on day 18.13.<br>**The loss:** her stages went [Foundation, Water Tower] → [Foundation] (marks day 20.9), +3% lost without her doing anything. | 1.5 | now | social, systems |
| 8 | **The keystone is a chain between friends.**<br>• A tier-2 good whose inputs two counted residents make (in Capital SE, bricks from Ana's stone and Cleo's coal), at 3 h of one maker.<br>• `Makers()` counts an input the Yard brings.<br>• Today's rule only where no pair exists. | Fix (rule) | **Water** was the keystone in all four stations by day 16.13, and nothing in Era 1 buys it below a Level-14 Coal Plant.<br>**Felt chains:** the run's only "together 4" was stone into bricks (cleo d14 #341–#368). | 2 | next | systems, social (their held-back row 11) |
| 9 | **A running gift stays running, and both ends hear.**<br>• A usual partner's trade comes before the giver's own Storage, unless the giver chooses otherwise.<br>• A fall of half or more reaches both cards, with the solver's cause and one button.<br>• Gift totals come from the books. | Fix | **The fall:** Cleo's stone 55.8 → 0.8 a minute at day 22.06. Ana's Quarry went energy-short in the same sample (metrics) after her factory upgrade (#293) and Warehouse (#302). Nobody was told.<br>**The card total:** "+86.5k g" against about 14k paid (ana d27 #317). | 1.5 | next | social, player |
| 10 | **Gold and points have a use.**<br>• Labs with nothing to research rest and eat nothing.<br>• Repeatable Refinements: +2% to one chain, 5k × 1.6ⁿ points, capped at +50%.<br>• "Pick one!" only when something can be picked.<br>• Hub orders pay 30 min of measured income.<br>• The Invest line offers the biggest useful buy (an open Ask's "Pay for it" included) or stays quiet. | Fix (numbers) | **Points:** Cleo had 449k on day 10 (STUCK #268) and 1.6M by day 21, against 1.42M for all five eras' trees.<br>**The Lab's cost:** 29 ingots a minute, about 400 g/min, for nothing (cleo d10 #267).<br>**Orders:** none after day 14.<br>**The Invest line:** "27796.8k … for 13k g" (ana d27 #317). | 3 | next | systems, player |
| 11 | **Stage rewards aimed at the bottleneck.**<br>• Water Tower: the station's pipes carry +50% (the research effect `throughput`).<br>• Workshop: +4% and materials 25% cheaper.<br>• The keystone pays the hub order's 2× base. | Fix (numbers) | **The reward:** "+38 g/min for you" at 951 g/min (ana d27 #329).<br>**The pay:** 1.30 at the tower against 2.0 for hub orders (group, day 2 08:00).<br>**The real bottleneck:** grey links of 100 a minute bound Cleo's pit (row 2). | 1.5 | next | systems |
| 12 | **The crew's news in one place.**<br>• Station and Crew chat channels.<br>• Every project named by its station.<br>• One crew line on the return card for the skimmer ("Cleo bakes bricks for the Dynamo · you could: Send ▸"). | Fix | **Four Water Towers in one chat** (mia d02 #65; mia2 d02 #73, d08 #105).<br>**Dev:** "Capital chat full of other Mias" (dev d08 #20).<br>**Ben:** "a wall of 30+ messages … I understood little" (ben d36). | 3 | next | social, player |

The total is about 33 days. Rows 5 and 6 (about 9 days) are the redesign.

## 4. My final vote

**A mix.**
- **The fixes**, rows 1–4 and 7, go first, before anyone plays again.
- **One redesign: Needs become Asks** (rows 5–6). It is the social designer's Ask and answer, built on the existing
  Ask engine as the buyer on the pipes I asked for in round 1, plus the Era wonder as the Capital's project.
- **Why a redesign at all:** the fixes stop the game from lying and freezing. They cannot give friends a way to move a
  material, or gold a reason to exist. Three runs and the advisor balance run say the middle game stalls there, and
  dealt land put the material needs there on purpose.
- **What changed from round 1:**
  - The redesign is cheaper (6 + 3 days, not 10–11), because the Ask engine already sits in the Yard round. Hub orders
    leave it.
  - The keystone rule is a pace test.
  - The chain keystone moves to "next".
  - Two energy diagnoses are corrected: Cleo's pit was bound by its output, and a second grey pipe adds nothing.
- **What would change my mind:** the day-9 replay below, with only the fixes. If Ana, Cleo and Dev build a Big Factory
  and fill half the Dynamo from their Storages by day 21, the missing pipe was attention. Then I'd hold rows 5–6 until
  run 4's late diaries say whether anything is left to want. If run 4's asks are mostly closed by Houses (the social
  designer's test), the lever is the reason to help: rows 8 and 11, not more verbs.

## 5. The first two weeks of work, in order (ten working days)

1. **Day 1:** the keystone (row 1) and the small station's kept stages (row 7, the stage part), with tests.
2. **Day 2:** a scripted replay of run 3 from the day-8 snapshot to day 14 with nobody playing. Capital SE's Workshop
   and NW's Water Tower must stand or be passed over within 4 days, with the cards saying so.
3. **Days 3–4:** the truthful lines (row 2). First the project line and the reader's own idle maker, then idle reasons
   that name the binding limit.
4. **Day 5:** the door (row 4). Then the e2e with Ben pressing Play now before Ana's link.
5. **Days 6–7:** the core of row 3: one "can make" rule, refusals that teach, steps that name every cost, and
   `LevelReach` counting link capacity.
6. **Day 8:** the wonder's tracker line and its end told (row 6's 1-day part). Also the stranger's seating (the rest
   of row 7).
7. **Day 9:** the decision replay. Run 3 from the day-8 snapshot with Ana, Cleo and Dev to day 21, on the fixes only.
   The test is my mind-changer above.
8. **Day 10 on:** Needs become Asks (row 5): first game-raised Asks that fill into a site, server side. The friend's
   tap, "Pay for it" and the cards come in week 3. Then the wonder as a buyer (row 6), rows 8–12, and run 4.
