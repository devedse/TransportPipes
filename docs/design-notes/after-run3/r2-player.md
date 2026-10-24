# Round 2 · the player researcher (`player`)

I read `r1-systems.md` and `r1-social.md` and checked the claims below against the logs, the samples and the code.
Sessions and action numbers are as in `actions.jsonl` ("ana d27-2000 #341").

## 1 · What I take from the others

**The finding that changes my view (systems, rows 3–4): the late game's wants exist, but they're walled off.** I
checked it.
- **Materials come only from your own Storages.** From the Big Factory on, every bigger building needs materials,
  and only your own Storages can supply them:
  - Big Factory: 100 bricks.
  - Era 2 buildings: Steel Mill 300 bricks, Cement Kiln 200 bricks, Big Turbine 20 steel, Machine Shop 50 steel.
  - Others: Market Hall 500 bricks, Bank 1,000 bricks (`buildings.yaml`).
  - `Pay()` takes materials from `Materials(company)` only (GameWorld.cs:752–775).
- **The crew stopped building and stopped spending.** From day 15 to day 36 it added three buildings (Ana 13 → 14,
  Cleo 15 → 16, Dev 11 → 12). On day 36 it still held 95–100% of all it had earned: Ana 39.8M of 39.8M, Cleo 32.3M
  of 33.9M (metrics).
- **Where I was wrong in round 1.** Of my "texts the rules didn't keep", three were not texts:
  - the Big Factory's bricks (ana d21 #286);
  - the Copper Pipe (cleo d21 #404);
  - the Dynamo at 0.

  An honest line would only have made those dead ends honest.

**The Dynamo was never out of reach. Its goods couldn't get to it.**
- **Its size was small.** It was sized once, at world start, from 6 h × 60 g/min (`StageRequirements`,
  GameWorld.Wonders.cs:113–126): 1,800 bricks and 900 ingots.
- **Cleo's output.** Her Brickworks made 12 bricks a minute in her day-21 session, short of coal (cleo d21 #421). On
  days 18–21, while Ana's stone arrived at 55.8 a minute, it ran at about 55 a minute (metrics). Either way, 1,800
  bricks was 0.5 to 2.5 hours of her bricks.
- **Where the bricks went.** Her panel said "to Trading Post · sold to hub": her only Storage was full of energy and
  coal (#420–#421).
- **The misreading.** Cleo's "we reach 1,800 only with ~10x more stone" (group, day 21) was a misreading the screens
  allowed. My round-1 line "a wonder out of reach" was the same misreading.

**What nobody had in round 1: the buyer already exists.** `GameWorld.Asks.cs` already does it:
- An Ask asks the station for an amount of a good, for 12–72 hours.
- Neighbours' spare goes to it first, at the one price, into the asker's Storages.
- At the deadline a House fills the rest at 2.0×.

It sits under Neighbours › Plan board ("🙋 Ask the station", PlanBoard.tsx:66) and was used 0 times in run 3 (every
sample: asks 0). Ana's 100 bricks were one Ask away. The game's refusal, "You need 100 bricks in your storages"
(GameWorld.cs:766), never mentioned it.

**Adopted and merged into my rows** (each checked):
- **The keystone rule (systems row 1, social row 2).** `Delivered < 1e-6` is at GameWorld.Projects.cs:433. A House
  delivers up to a quarter of each good (`NpcCap`, :586–596). Both frozen keystones sit at that quarter: NW 1,855 and
  SE 1,990 of 7,200.
- **Max counts the pipe (systems row 5).** `LevelReach` scales by a link's efficiency, never its capacity
  (GameWorld.Max.cs:99–130). Merged into my steps-and-fixes row.
- **Labs rest (systems row 6).** Era 1's tree costs 8,500 points, and all five eras' trees 1.42M (`research.yaml`).
  Cleo had 1.6M by day 21.
- **The Capital W restart and seating beside a crew that plays (systems row 9, social row 8).** Capital W restarted
  from the Foundation on day 18.13, a day after "Nobody else here has played lately" (events, day 17 03:11).
- **The door (social rows 1 and 7), merged with my row 4.** Their wrong texts are real: "Ben Co moved into your
  district" on Ana's screen, and "Old Forest Co. moved in next door" on Ben's day-36 card (ben d36 #89), though Dev
  lives in Capital SE.
- **The social designer's face for the redesign (social row 3):**
  - the need appears on the card of each friend who can meet it;
  - a Storage's stock answers it with one tap;
  - a friend can pay for it.
- **The rest:**
  - both ends hear when a flow between friends drops (social row 5);
  - a Crew channel (social row 6). Dev wrote "Capital chat full of other Mias … unclear who my friends are"
    (dev d08 #20), and Ben "nothing from my friends there" (ben d01-2300 diary);
  - a crew line on the card for the skimmer (social row 10): Ben's "a wall of 30+ messages … I understood little"
    (ben d36 diary).

## 2 · Where I disagree, and why

### The keystone rule: the systems designer's rule, with a pace guard

**I pick the systems designer's rule: a keystone with no delivery from a player for 2 days is passed over, and
Houses never count.**
- **Both texts already drop the Houses' quarter.** What separates them is whether any delivery restarts the clock.
- **Players can act only on what players do.** A House's trickle that keeps a stage waiting can't be explained on the
  line, and run 3's freeze was exactly a House's quarter.

**My addition: a player's trickle doesn't hold a stage either.** A player delivery restarts the clock only if, at
the players' pace over the last day, the keystone would be met within 3 more days.
- The card already computes the pace: "At this pace: about 4 h 46 min" (dev d08 #12).
- Ben's Send moved 0.6 energy a minute (ben d02-0800 #70). A trickle like that would otherwise hold a stage for weeks.

**After a pass-over:**
1. **The line warns before.** "Workshop 96% · 1 d 20 h without water · passed over Friday 20:30 unless one of you
   brings some · you can: a Water Pump beside your pond".
2. **The good leaves the basket.** What came stays paid.
3. **A new keystone only if a resident can make one,** by the build command's own placement rule, and it wasn't passed
   over before. After the second pass-over in a stage, the stage stands on its made goods (systems). Capital NW went
   coal → planks → water; with this rule it would have stood at the second pass-over on day 5.3, its wood and ingots
   already in (2,396 / 2,400 and 1,433 / 1,440, both past the 99.5% that counts as done).
4. **Every resident's next card says it once, by station.** "Capital SE's Workshop stopped waiting on water (none came
   for 2 days): it stands when its iron ingots and stone are in." Mia was never told that coal had stopped mattering
   ("nothing told me it no longer matters", mia d08 diary).

### The redesigns: if only one is built, I back the systems designer's, and two of them are one thing

**Why the buyer on the pipes, not a separate Ask-and-answer verb:**
1. **Friends in an idle game are away.**
   - From day 14, every one of Ana's cards said Cleo and Dev were resting (ana d14 #197, d21 #260, d27 #317, d36
     #357).
   - Her calls for water on days 27 and 36 got one reply, from Ben, who wrote "i dont have water either" though the
     game had told him he had a pond.
   - A need that waits for a friend's tap waits for days. A buyer fills from a friend's spare while both are away, as
     Ana's stone filled Cleo's Brickworks within minutes and for a week (cleo d14 #366; 308k stone).
2. **What players felt was the name, not the verb.**
   - The one gift both ends felt was an automatic flow named at both ends: Cleo's 💛 Thanks and her "together 4"
     (cleo d14 #368), and Ana's card "thanked you for your stone" (ana d21 #260).
   - The verbs that need a deliberate tap went unused: 6 Sends, none to a friend; "Make it yours: Send ▸" unpressed
     (ana d21 #260); 0 Asks.
   - Thanks was pressed (3 times) only once it sat where the news was.
3. **It answers my own test.** I asked whether players had "nothing to want". The wants exist (every building from the
   Big Factory on), and this opens all of them at once. Ask-and-answer opens them one ask at a time, and only when a
   friend is there.
4. **It takes the Storage out of every goal.** The Storage was the run's commonest standing warning: Cleo's full
   Storage was on 5 of her 5 cards, and three players saw their income drop right after building one (cleo d01 #68,
   ana d02 #90, dev d08 #33).

**How they are one thing:**
- **A site.** A placement short of materials becomes a site (systems). It raises a station Ask for its materials,
  using the engine that exists: neighbours' spare first, at the one price, named at both ends; a House fills the rest
  at the deadline.
- **The face.** Friends who can meet it see it on their card (social): "Ana's Big Factory wants 100 bricks · your
  Brickworks sends its spare by the Yard · or send the 500 you hold ▸". A crew mate can pay for it (social).
- **The Era wonder** is a standing buyer for the whole Capital on the same rails (systems row 4).
- **Hub orders** become a buyer at the trading post: no Storage, no Deliver.

**Two changes to their versions:**
- **Nothing is instant.** Drop the systems designer's "Buy the rest now", and make the social designer's "Pay for it"
  move the House's part to the next hour, not to now. Gold is not scarce (Ana held 39.8M on day 36). If gold buys at
  once, a friend's goods never matter again. What a friend saves you is time.
- **Materials go into the site, not into a Storage.** Today's Ask refuses without room in your Storages ("Your
  storages haven't room for that much", GameWorld.Asks.cs). Ana's and Cleo's Storages were full.

**Cost:** about 8–10 days, a little under the systems designer's 10–11, because Asks already take part in the Yard
round (`Incoming` and `Matched`).

### Retire "paid trade ≥ 15%" as the headline social mark: yes

Both designers propose it, and the players agree.
- The only "together 4" of the run (cleo d14) came at a crew paid share of 0.0% (day 13.8 sample) and 1.3% (day 20.9).
- Run 2's 2.8% brought no gift anyone felt.

Keep the share as a number the systems designer watches. The headline becomes a friend's goods inside your building,
site or the wonder by day 14, named at both ends (the systems designer's mark), plus acts answered with a Thanks.
Weigh the diaries' "together" scores low: AI players are kind, so count acts first.

### Smaller disagreements

- **"Cleo found the wonder by hunting"** (systems row 4, social row 4). In fact the top bar's Era button opened Hub ›
  Wonders on her first try (cleo d10 #271: the screen after that click shows "The Great Dynamo … Bricks 0 / 1,800").
  The agent missed it. The fault is that the button reads as a clock.
- **Mia's water (social row 9).** It didn't fail on a pipe that missed the gate. Her pump had no energy from day 8
  20:30 to day 36 (metrics `idle`: "InputMissing energy"), after the game's own "Unstick" pipe (mia d08 #96). The Send
  line has to name the building's own state.
- **The bricks keystone (systems row 2) "now".** I'd make it "next".
  - Changing what the goal is in the same run as how it moves would make run 4 unreadable.
  - A two-input line is harder for a skimmer.
  - The social designer holds it back to "later". With sites, bricks become a personal want too, so I wouldn't wait
    that long.
- **The advisor balance run's plateau (systems).** It is a solo bot. Its income is flat from day 3 17h on "could not:
  Your Coal Plants are 120 water short" (`docs/balance/balance-typical-advisor.md`), a water dead end like Ana's pond.
  After researching the Big Factory (day 3 9h) it never builds anything that needs materials. It shows both causes,
  guidance and structure.

## 3 · The final table

| # | Improvement | Fix or redesign | Answers (evidence) | Effort (days) | Priority | Proposed by |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | **The keystone always moves on.**<br>• No delivery from a player for 2 days: passed over. Houses never count and never restart the clock.<br>• A player's trickle restarts the clock only if, at the players' pace, the keystone would be met within 3 more days.<br>• After a pass-over the good leaves the basket, and what came stays paid. A new keystone only if a resident can make one by the build rule.<br>• After the second pass-over the stage stands on its made goods.<br>• The line warns before ("passed over Friday 20:30 unless one of you brings water"), and every card says it after, by station. | Fix (rule) | **Where it froze:** Capital SE's Workshop at 96% from day 9.85 to 36 (water 1,990 / 7,200); Capital NW's Water Tower at 89% from day 6.13 (1,855).<br>**Why:** both sit at the Houses' quarter (`NpcCap`, GameWorld.Projects.cs:586–596) under `Delivered < 1e-6` (:433).<br>**Players:** Mia STUCK (mia d15 #141); "nothing told me it no longer matters" (mia d08 diary). | 1.5 (with a replay from the row-9 snapshot) | now | systems, social; player (pace guard, texts) |
| 2 | **The project's line tells the truth and gives the way through.**<br>• One sentence, the same on the This week line, the card and the Project tab, from the build command's rule: what it waits on, how long, who here can make it and how, and one button. "Workshop 96% · 9 days on water · you can: a Water Pump beside your pond · Show me".<br>• Never "(nobody here yet)" when a resident's land allows it.<br>• Your own idle maker leads your card: "Your Water Pump is idle (no energy): the Water Tower waits on its water".<br>• Send says what it moved this week, or why nothing moved. | Fix | **"(nobody here yet)"** on 6 crew cards, days 14–36 (ana d21 #260, d27 #317, d36 #357; cleo d14 #313, d21 #391; dev d14 #105), though all six estates got "Surveyors found a pond on your estate!" (events).<br>**Mia's pump:** idle from day 8 20:30 to day 36 (metrics), and her card said nothing (mia d15 #125).<br>**Ben:** "i dont have water either" (group, day 36). | 2 | now | player, social |
| 3 | **Materials, hub orders and the Era wonder become buyers on the pipes.**<br>• A placement short of materials becomes a site. It holds its own materials and raises a station Ask (today's engine): neighbours' spare first, at the one price, named at both ends; a House fills the rest in 12 h.<br>• Friends who can meet it see it on their card ("your Brickworks sends its spare by the Yard · send the 500 you hold ▸"). A crew mate may "Pay for it": the House's part comes in the next hour, never at once.<br>• Hub orders buy at your trading post: no Storage, no Deliver.<br>• The Era wonder (and the District Wonders) is a buyer for the whole Capital in every Yard round. It is sized when each stage starts and shows in every tracker with your part. | **Redesign** | **Every bigger building needs materials** from your own Storages (`buildings.yaml`; `Pay()`, GameWorld.cs:752–775).<br>**Big Factory:** cleo d10 #281; ana d21 #286, STUCK; "I don't know how to hand you 100 bricks directly" (group, day 21).<br>**The crew stalled:** 3 buildings added from day 15 to 36 while it held 95–100% of its earnings (metrics).<br>**The Dynamo:** 0 / 1,800, though that was 0.5–2.5 h of Cleo's bricks, sold to the hub behind a full Storage (cleo d21 #420–#421).<br>**The Ask** that could have carried them was used 0 times. | 8–10 (sites on the Ask engine 5–6, the wonder as a buyer 2–3, friends' card lines 1) | first step now (the refusal raises an Ask, 1 d); the rest next, before run 4 | systems (the mechanism), social (the face), player (today's Ask; nothing instant) |
| 4 | **Find your friends.**<br>• The start screen opens with "A friend sent you a link? Open it · or type the code".<br>• "Start a crew and invite friends" asks for one name only.<br>• A seat kept for you shows on every return until you answer: "Ana keeps a seat for you in Capital SE · Join and move, free · Not with them".<br>• The founder sees "Ben opened your link but lives in Capital SW · Send it again · 👁 Visit".<br>• "Moved in next door" only within one station.<br>• "Send to Ben" becomes "Copy Ben's link". | Fix | **Ben:** founded a crew 33 s before Ana's link arrived, then "Leave your crew first." (ben d01-2000 #5–#29). Three return cards said nothing of the seat (ben d01-2300 #34, d02-0800 #55, d03-2000 #75), and he asked "which station am i in?" twice.<br>**Ana:** STUCK (ana d01 #68), and six promised visits.<br>**Wrong text:** "Old Forest Co. moved in next door" on Ben's card (ben d36 #89). | 2.5 | now | player, social |
| 5 | **The Era wonder in sight now, and its end told.** Until row 3 makes it a buyer:<br>• a tracker line with your part and its button ("Great Dynamo · bricks 0 / 1,800 · you hold 500 · Deliver");<br>• the Era button names the goal, not just a clock;<br>• every card after the Era ends says how it ended and the crew's part. | Fix | **The button:** opened Wonders but read as a clock (cleo d10 #271); she found it again at #291.<br>**The bricks:** Cleo's 500 were never delivered (cleo d21 #420).<br>**The end:** "Era 1 ended without me knowing" (ana d27 #318), and the only event was "Era 2 begins!" (day 22 19:24).<br>**Dev:** "I feel late and useless" (dev d21 diary). | 1 | now | player, social, systems |
| 6 | **One rule for "can make", and refusals that teach.**<br>• Makers, parcel steps and the Build menu use the build command's own check. `FitsOnEstate` tests forest terrain, while dealt land needs the marked trees.<br>• Parcels are named by what a building can use there.<br>• A refusal names the rule and moves you to a tile that works: "A pump stands beside the water, not on it".<br>• A building in hand outlines every tile where it may stand, on a phone too. | Fix | **Forest:** Cleo's two "forest" parcels, two STUCKs (cleo d06 #232, #244); "you can" wood for seats with no wood (ben d01-2300 #34, dev d08 #12).<br>**Water:** "Not on water" three times (ana d27 #341–#348, STUCK).<br>**Placement:** no tiles shown for a Lumber Camp in hand (mia2 d08 #134, STUCK). | 2 | rule and refusals now; tile outline next | player, systems |
| 7 | **Steps, fixes and Max offer only what will work.** Each is first tried out through the command's own check and the network what-if:<br>• a step names every cost, or waits until it can be met;<br>• a fix that leaves the building idle isn't offered;<br>• Copper Pipe is disabled with its reason;<br>• Max counts each link's capacity and shows the net change ("+67 research/min · −29 ingots ≈ −400 g/min");<br>• "spare" counts only what can arrive;<br>• one gain figure per building. | Fix | **Copper Pipe:** no-op (cleo d21 #404, STUCK).<br>**Big Factory:** its gain read +80, +124, then +57 (cleo d10 #271, #291, d14 #361).<br>**The Lab Max:** 783 → 285 g/min (cleo d06 #180).<br>**Energy:** 32 of 52 against 281 spare (ana d04 #136); Coal Pit at 11–14% for 11 days, "two numbers that don't agree" (cleo d21 #397).<br>**In the code:** `LevelReach` ignores capacity. | 3 | now | player, systems |
| 8 | **A small station keeps its stages, and the stranger sits beside a crew that plays.**<br>• A small station that leaves a shared Yard keeps the stages it stood with.<br>• "Next door to a crew" counts residents who played in the last 3 days, not held seats. | Fix (bug, rule) | **Capital W:** restarted from the Foundation on day 18.13, after "Nobody else here has played lately" (day 17).<br>**Mia-B:** seated beside a crew of one and three held seats (mia2 d01 #10–#15). | 1.5 | now | systems, social |
| 9 | **A keystone that needs two friends, and stage rewards worth having.**<br>• From the Water Tower on, the keystone is a tier-2 good whose inputs two residents make (Capital SE: bricks, from Ana's stone and Cleo's coal). The line names both friends, and an away seat's input comes from its House.<br>• Rewards hit the bottleneck (pipes carry more) and pay like the hub. | Fix (rule, numbers) | **The keystone:** water in all four stations by day 16.13 (metrics).<br>**The shape that worked:** the run's only "together 4" (cleo d14 #341–#368).<br>**The pay:** "+38 g/min for you" at 951 g/min (ana d27 #329); 1.30 at the tower against 2.0 for orders (group, day 2 08:00). | 3 | next, once row 1 is seen working | systems; social (held back) |
| 10 | **The card leads with what matters, and folds the rest.**<br>• Order: people; the shared goal's blocker if you can unblock it; one-tap fixes.<br>• A line unchanged since the last return becomes one "Still:" line with its real cause and a fix that works.<br>• A crew line built from state ("Your crew: Cleo bakes bricks for the Dynamo · …").<br>• Both ends hear, with one button, when a flow between friends falls by half.<br>• "Today in" and the bell carry only what you can act on.<br>• No Invest line until something is worth the bank; 27.8M, not 27796.8k. | Fix | **Standing lines:** Cleo's full Storage on 5 of 5 cards, frozen at "4,831 energy" (cleo d10 #263 → d21 #391); Ana's factory at 55/59/50/40% on four cards.<br>**The Invest line:** on 14 of 23 cards.<br>**A flow that fell:** Ana's stone to Cleo went 55.8 → 0.8 a minute on day 22, and neither was told.<br>**Too much to read:** "a wall of 30+ messages" (ben d36); about 49 calendar events per player; "I can't tell what matters" (ana d36). | 3 | next | player, social, systems |
| 11 | **Labs rest, and points turn into growth.**<br>• A Lab with nothing left to research stops eating.<br>• After an Era's tree come capped, repeatable Refinements.<br>• "Pick one!" only when something can be picked. | Fix (rule, numbers) | **Points:** 449k under "pick one!" (cleo d10 #268, STUCK), and 1.6M by day 21, against 1.42M for all five eras' trees.<br>**Cost:** the Lab ate 29 ingots a minute (cleo d06 #180). | 2 | next | systems |
| 12 | **Chat and panels that don't hide your friends or your goals.**<br>• A Crew channel (and a Station channel) beside the Capital one.<br>• Projects named by their station everywhere.<br>• The chat opens on the right, and Escape closes Build.<br>• On the phone, the folded Next steps line is itself the button. | Fix | **Chat:** four Water Towers in one chat (mia d02 #65, mia2 d08 #105, #122, mia d08 #112); "Capital chat full of other Mias" (dev d08 #20).<br>**Covered panels:** chat over the goals (ana d04 #140–#144, cleo d14 #350, ben d01-2300 #44).<br>**Phone:** "Upgrade Coal Pit to level 10" with nothing to tap (ben d36 #105, STUCK). | 3 | next | player, social |

## 4 · My final vote

**A mix: fix the guidance at its source now, and build one redesign before run 4 — the systems designer's buyers
on the pipes for materials, hub orders and the Era wonder.**
- **Its base is the Ask the game already has.** It shows as the social designer proposes: the need on the friend's card,
  one tap from storage, a friend who can pay. Nothing is instant, so a friend's goods still save time.
- **I don't back Ask-and-answer as a separate verb.** In an idle game the friend is usually away. Run 3's one felt gift
  was an automatic flow named at both ends; the verbs that need a deliberate tap went unused.
- **What changed from round 1.** I voted fixes because I read every late drop as a text problem. Three were not: the
  bricks, the Copper Pipe and the Dynamo sat behind a real wall. Materials come only from your own Storages, and the
  whole Era 2 list needs them. The Dynamo was two hours of Cleo's bricks, sold to the hub. The buyer that could have
  carried them existed, and nobody found it.
- **What would make me back fixes only.** Day 8 of the plan below makes the "You need 100 bricks" refusal raise the
  existing Ask. Then I'd replay from the day-21 snapshot. If Ana's Big Factory stands that way, and the crew goes on
  to build Era 2 buildings by Asks, the wall was a missing door. In that case, sites can wait.
- **What would make me back the social designer's lever.** If in run 4 friends supply less than a third of the sites'
  materials and the Houses the rest, the rails work but nobody has a reason to help. Then the two-friend keystone
  (row 9) moves up.

## 5 · The first two weeks of work, in order

1. **Day 1:**
   - the keystone rule with the pace guard and the before-and-after texts (row 1);
   - Capital W keeps its stages (row 8, first half);
   - a replay from the row-9 snapshot: Capital SE's Workshop must stand or be passed over by day 12.
2. **Days 2–3: the project's line (row 2).** One sentence everywhere; the idle maker leads the card; Send says what it
   moved. Check it from the day-27 snapshot: Ana's card names the pond and the button.
3. **Day 4: one rule for "can make" and refusals that teach (row 6, rule and refusals).**
4. **Day 5: the Era wonder in every tracker, and its end told (row 5).** Also "pick one!" only when something can be
   picked.
5. **Days 6–7: the door (row 4).** Then replay row 1 with Ben pressing Play now first. He must be in Ana's station by
   his second session.
6. **Day 8: the redesign's first, smallest step (row 3).** The "You need N bricks" refusal raises the existing Ask
   ("Cleo's Brickworks makes them · a House brings the rest in 12 h · Ask"), and an open Ask shows on the neighbours'
   cards. Then replay Ana from the day-21 snapshot: does her Big Factory stand?
7. **Days 9–10: steps and fixes tried out before they're offered (row 7, first part).** Materials on steps, Copper
   Pipe disabled with its reason, no fix that leaves a building idle.

**Weeks 3–4:**
- sites with their own materials, hub orders as a buyer, and the wonder as a buyer for the whole Capital (row 3);
- the rest of row 7 (Max and capacity);
- seating beside a crew that plays (row 8);
- the tile outline (row 6);
- then run 4, watched on the late game (days 18–36: fun 3.5 or more, "come back" yes in at least 3 of 6), no STUCK
  caused by a suggestion, and a friend's goods inside a building by day 14.
