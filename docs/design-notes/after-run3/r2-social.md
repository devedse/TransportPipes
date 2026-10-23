# Round 2 · the social designer (`social`)

## 1. What I take from the others

**What I checked before adopting it** (logs and code):
- **Water was the keystone everywhere.** It was the keystone in all four stations by day 16.13: NW from 6.13, SE from
  9.10, SW and W from 16.13 (metrics `projectBasket`).
- **Mia's water.** Mia's Water Pump sat at `InputMissing energy` from day 8.85 to day 36 (metrics `idle`). So her
  Send moved nothing because her pump had no energy, not because of how Send works. I had the cause wrong in
  round 1.
- **Materials come only from your own Storages.** `Pay()` (GameWorld.cs:753) takes materials only from the payer's
  own Storages.
- **Delivering to a Wonder pays no gold.** `DeliverToWonder` (GameWorld.Wonders.cs:150–200) pays XP only. Cleo's
  Brickworks read *"Bricks 12 / 55/min · 12 bricks/min sold to hub · 63 g/min"* on day 21 (cleo d21 #421): her bricks
  were going past an empty Dynamo to the hub.
- **The Era button.** It did open Wonders (cleo d10 #271: *"Bricks 0 / 1,800 you have 0"*); it reads as a clock.
- **Gold.** The crew held 95–100% of all it earned (day 36: Ana 39.8M of 39.8M, Cleo 32.3M of 33.9M, Dev 18.9M of
  19.0M).
- **One pump would have been enough.** A Water Pump makes 20 water a minute at Level 1, so one would have filled
  NW's tower in about 4.5 h.
- **The late game.** From day 18 the diaries show fun 3.0 and one "yes" in six.

**What I adopt:**
- **From the systems designer:**
  - the keystone rule (their 1), with one amendment (below);
  - the keystone between two friends (their 2). I held it back in round 1; I now want it **now**, because water was
    a keystone no friend was needed for, in every station;
  - sites and the Wonder as buyers on the pipes (their 3 and 4), as **the** redesign; my Ask and answer folds into
    it (below);
  - Max counting the pipe (5), Labs and Refinements (6), rewards (7), wants priced in hours (8), and the small
    station keeping its stages (9, merged with my 8);
  - their measure (10), as the headline social mark.
- **From the player researcher:**
  - the contract "nothing offered that the rules refuse" (their 1–3);
  - finding friends (4, the same as my 1);
  - the Wonder line (5), standing lines (6), warnings (7), panels (8), station names (9, merged into my chat row)
    and events (10);
  - their 11, a friend's goods reaching a friend's build, is inside the redesign.
- **Corrections I accept:**
  - The cause of Mia's freeze was a dead pump plus a silent card, not only the pass-over rule.
  - "(nobody here yet)" did more harm than "Not on water", because three active players were told for 12 days that
    none of them could make water.

## 2. Where I disagree, and why

### The keystone rule: the systems rule, with one amendment

**I pick the systems designer's rule over mine.**
- Each stage records its keystone's last delivery from a player.
- A House's delivery never counts and never restarts the clock.
- With a counted resident active, 2 days without a player delivery passes the keystone over.

Mine ("no new delivery") left open who the delivery comes from. A keystone should wait on people, not on an NPC's
quarter: both frozen keystones sit at the Houses' cap of 1,800 (`houseShare`, GameWorld.Projects.cs:586–596).

**The amendment: the 2 days start once every active resident has had the keystone on a card.** Without it, the plain
rule decides for the friends while they're away. Played on run 3's dates:
- The Workshop started on day 9.10. Water would be passed over on day 11.1, while Ana and Dev were still active.
- A second keystone on 13.1, with Dev active to 13.8.
- So the Workshop would have stood on its made goods on day 13, before anyone had seen a second keystone.

With the amendment, the clock for the second keystone starts at day 14 19:00. That is the evening every one of them
played, and Cleo built a Brickworks. A stage nobody chose is run 2's *"the project finished itself"*, and it reads as
nothing.

**After a pass-over:**
- The next keystone follows the pair rule (row 4), and is never one already passed in this stage.
- Every card and the Station channel say it once: *"Water passed over: the Workshop now wants bricks · Ana's stone +
  Cleo's coal"*.
- After two pass-overs the stage stands on its made goods (systems), which are deliveries too.

### The three positions on a redesign

**If only one is built, I back the systems designer's: every want is a buyer on the pipes.** My own Ask and answer
fails a test the run already ran.
- A manual hand-over existed for the Dynamo: Deliver from storage. It went unpressed by the player who held 500
  bricks.
- Manual acts need both friends present and attentive. Sessions here are 4–9 days apart.
- The run's best social moment was an automatic flow into a building a friend chose: Ana's stone in Cleo's Brickworks,
  named at both ends and thanked (cleo d14 #366–#368).
- Buyers on the pipes work while people sleep. Pulls from Storage didn't work once: the Dynamo, hub orders after day
  14, the Big Factory, the Copper Pipe.

**Two of them can be one thing: mine becomes the social face of theirs**, at about 1.5 days instead of 5. Three gaps
remain in a buyer-only design:
1. **A friend's Storage stock is not Yard spare.** `MeasureSpare` leaves out what a Storage keeps, so Ana's site would
   buy at 2.5× from the hub while Cleo's 500 bricks sat next door. The want needs one tap on the friend's card:
   *"Ana's Big Factory waits for 100 bricks · your Storage holds 500 · Send them ▸"*.
2. **"Buy the rest now" should be open to friends** (*"Pay the rest for Ana ▸"*). Gold goes to the hub, nothing passes
   between players, and it's blocked between same-device accounts. Friends offered gold four times (Cleo d02, d06,
   d14; Ana d21).
3. **The want shows once to the friends who can fill it**, and the filled site says who built it with 💛 Thanks.

**Two small corrections to the systems notes:**
- Glass is not a two-friend chain: a Glassworks takes sand and energy, and energy is on every estate. Bricks and copper
  ingots are.
- "33 minutes of Cleo's bricks" uses the Brickworks' capacity (55 a minute). She made 12, and sent them to the hub.
  The Dynamo was about 2.5 hours of her real output, which is a stronger case for the Wonder as a buyer.

**On the player researcher's "no redesign":** I agree the contract comes first and is a root fix, but it isn't an
alternative. An honest step (*"Big Factory · 12k g + 100 bricks"*) still sends Ana to build a second chain for bricks
that sit in a friend's Storage. It also leaves the 95–100% of gold nobody spent. Their own test is "nothing to want"
in late diaries, and the systems designer shows it without AI players: the advisor run is flat from day 4.

### What replaces "paid trade ≥ 15%"

**Headline act mark: built together.**
- By day 14, every crew member with 3 or more sessions has a friend's goods in one of their builds (a site, the
  keystone or the Wonder), or theirs in a friend's.
- Both ends' cards name it.

This is the systems designer's "goods that built something", plus "named at both ends", because run 2 showed unnamed
goods count for nothing.

**Headline feeling mark: felt together.** Ana, Cleo and Dev average "together" ≥ 3.2 (runs 1–3: 2.95, 2.84, 2.79),
with no "together 2" whose reason is a frozen goal, a lost friend or a gift the game can't make.

**What stays, as numbers rather than headline marks:** the paid-flow share, new gold from NPCs and harm from a
neighbour, kept as guards.

## 3. The final table

| # | Improvement | Fix or redesign | Answers (evidence) | Effort (days) | Priority | Proposed by |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | **Find your friends.**<br>• The start screen's first choice is *"A friend sent me a link or code"*, with a paste field. "Start with friends" becomes "Found a crew for my friends", with one name field.<br>• A seat kept for you, or a link you opened, shows on every return until you answer: *"Ana keeps a seat for you in Capital SE (you live in Capital SW) · Move in, free · Not with them"*.<br>• The founder sees *"Ben opened your link but lives in Capital SW · Send the link again · 👁 Visit"*.<br>• "Moved in next door" only within one station; otherwise *"moved into Capital SW (another station)"*.<br>• "Send to Ben" becomes "Copy Ben's link". | Fix | **Ben:** pressed Play now 33 s before Ana's link (ben d01 #5–#21), then *"Leave your crew first."* (#29). Three cards said nothing of Ana's seat (#36, #57, #77). *"which station am i in?"* twice.<br>**Ana:** STUCK (ana d01 #68); *"it says Ben Co already moved into my district"* (group d01 20:10; GameWorld.cs:411).<br>**Ben's d36 card:** *"Old Forest Co. moved in next door"* (GameViews.Return.cs:66). | 2 | now | social, player |
| 2 | **The keystone moves on, and its line tells the truth.**<br>• *The rule:* last player delivery; Houses never count; 2 days with a counted resident active.<br>• *The amendment:* the clock starts once every active resident has had the keystone on a card.<br>• *After a pass-over:* the next keystone by row 4; after two, the stage stands on its made goods; each pass-over is told.<br>• *The line, the same everywhere:* *"Workshop 96% · 9 days waiting on water · you can: a Water Pump beside your pond, 300 g · Show me"*. Your own idle maker leads: *"Your Water Pump is idle (no energy) · Show me"*. | Fix | **The freeze:** SE sat at 96% from day 9.10 to 36 (water 1,990 / 7,200), NW at 89% from 6.13 (1,855). Both are about the Houses' 1,800 cap (Projects.cs:586–596, `Delivered < 1e-6`).<br>**The line:** "(nobody here yet)" on 6 crew cards.<br>**Mia:** her pump idle from day 8.85 to 36 (metrics); STUCK at mia d15 #141.<br>**The project:** its gold flat from day 9.85. | 2.5 | now | systems, player, social |
| 3 | **Every want is a buyer on the pipes, and the friends who can fill it see it.**<br>• *Sites:* a building, upgrade or pipe tier that needs materials buys them from your producers, then the Yard at 1.3 (friends first, named at both ends), then Houses. The hub ships any rest in 12 h at 2.5×, or "Buy the rest now".<br>• *Hub orders:* a buyer at the trading post.<br>• *The Era wonder and District Wonders:* buyers in every Capital Yard round after the station project. Each stage is sized when it starts, its tiers pay hours of your income, and a result card tells how it ended.<br>• *The social face:* the want shows once to the friends who can fill it (*"Ana's Big Factory waits for 100 bricks · your Storage holds 500 · Send them ▸"*). *"Pay the rest for Ana ▸"* sends the gold to the hub, never between players, and never between same-device accounts. *"Cleo's bricks built your Big Factory 💛 Thanks"*.<br>• *It replaces:* Deliver from storage, "N in your storages", the Storage chore for orders, the Wonders minigame and the Plan board's Ask form. | **Redesign** | **Pulls from your own Storage failed:**<br>• the Big Factory (ana d21 #286 STUCK; *"I don't know how to hand you 100 bricks directly"*, group d21 19:17);<br>• the Copper Pipe (cleo d21 #404);<br>• the Dynamo at 0 while Cleo's bricks went to the hub (cleo d21 #421), and Wonder delivery pays no gold;<br>• no hub order after day 14.<br>**Gold:** 95–100% of it unspent, and four offers to friends.<br>**What worked:** a buyer on the pipes (cleo d14 #366). | 12 | next | systems, social, player |
| 4 | **A keystone that needs two friends.**<br>• A tier-2 good whose land inputs lie on two counted residents' seats: bricks (stone + coal) or copper ingots (copper + coal). Not glass.<br>• Sized at 3 h of one level-1 maker (1,800 bricks).<br>• `Makers()` counts an input the Yard brings, and checks the deposit, not forest tiles.<br>• An away seat's input comes from the station's House at 1.4.<br>• Today's rule only where no pair exists. | Fix (rule) | **Water** was the keystone in all four stations by day 16.13, and needed no friend (Ana alone at her pond, ana d27 #348).<br>**The run's one "together 4"** had this shape (cleo d14 #341–#368).<br>**"Can make" wood** was claimed for seats without it (dev d08 #12, ben d01-2300 #34, cleo d06 #244). | 2 | now | systems, social |
| 5 | **Nothing offered that the rules refuse.**<br>• One "can make" rule, taken from the build command.<br>• Refusals name the rule and move the ghost to a tile that works (*"A pump stands beside the water, not on it"*). A building in hand rims its tiles.<br>• Next steps and fix buttons dry-run before they show, name every cost, and aren't offered if they would leave the building idle.<br>• Copper Pipe is disabled with its reason; "pick one!" shows only when something can be picked. | Fix | 7 of 10 STUCK incidents: cleo d06 #244, ana d21 #286, ana d27 #348, cleo d21 #404, cleo d10 #268, ana d14 #253, mia d15 #141. Mia's "Unstick" pipe left her pump without energy (mia d08 #96, metrics). | 4.5 | now | player |
| 6 | **The crew's Wonder in sight, and its end told** (until row 3 makes it a buyer).<br>• A tracker line with your part and Deliver.<br>• The Era button names the goal (*"Era 1 · Dynamo 0% · 11 d"*).<br>• Every card after the deadline says how it ended: *"Era 1 ended: the Great Dynamo got 0 of 1,800 · Teapot Four gave 0"*. | Fix | **Cleo's bricks:** 500 never delivered (cleo d21 #420–#423).<br>**The Era button:** read as a clock (cleo d10 #271, #291–#293).<br>**The end:** *"Era 1 ended without me knowing"* (ana d27 #318). *"late and useless"* (dev d21). | 1.5 | now | player, social |
| 7 | **Clicks that cost income say so, and a friend's flow that falls is told to both.**<br>• Max counts the pipe (`LevelReach`) and shows the net change.<br>• Placing a Storage says what it holds back.<br>• A neighbour's running trade comes before your own Storage keeping that good, unless you choose otherwise.<br>• A flow between players that halves is on both next cards, with its cause and one button.<br>• Gift totals come from the books. | Fix | **Upgrades:** the Lab Max took 783 to 285 g/min (cleo d06 #180); the Coal Pit ran at 11–14%.<br>**Storages:** three income drops (cleo d01 #68, ana d02 #90, dev d08 #33).<br>**The gift that stopped:** Cleo's stone fell from 55.8 to 0.8 a minute after ana d21 #296–#302, told to neither. Ana's d27 card said +86.5k g against about 14k paid (#317). | 3 | next | systems, player, social |
| 8 | **A table for the station.**<br>• Station and Crew chat channels; the stranger next door is in the Station channel.<br>• The game posts the station's own news there once.<br>• Projects are named by their station; district lines carry the writer's station. | Fix | **Four Water Towers in one chat:** mia d02 #65, mia2 d02 #73, d08 #105, #122.<br>**Crossed lines:** Cleo's crew thanks in Mia's chat (mia d15 shot 0138); Dev: *"unclear who my friends are"* (dev d08 #20).<br>**Today's channels:** district, corporation and direct (GameHub.cs:791–807). | 2 | next | social, player |
| 9 | **The stranger beside a crew that plays; a small station keeps its stages.**<br>• "Next door to a crew" counts residents active in the last 3 days, not held seats (`CrewSeats`, Crews.cs:523–548).<br>• Finished stages stay when a small station's Yard changes. | Fix | **Mia-B's neighbours:** a crew of one and three held seats (mia2 d01 #10–#15; shot d15-0179).<br>**The split:** the holds lapsed 14 days after Ben's last session. Capital W restarted at the Foundation on day 18.13 and lost its Water Tower (marks, day 20.9). | 1 | now | social, systems |
| 10 | **Points and gold turn into growth.**<br>• Labs rest when nothing can be researched; repeatable Refinements follow an Era's tree.<br>• Stage rewards hit the bottleneck (Water Tower: pipes +50%), and the keystone pays like the hub.<br>• Orders and the Invest line are priced in hours of income. | Fix (rules, numbers) | **Research:** 449k points on day 10 and 1.6M by day 21, against 1.42M for every tree (cleo d10 #268).<br>**Rewards:** *"+38 g/min for you"* at 951 g/min (ana d27 #329).<br>**The Invest line:** 13k against a 27.8M bank (ana d27 #317). | 4.5 | next | systems |
| 11 | **Upkeep that fixes or folds; panels and a phone that tap through; events you can act on.**<br>• A line that comes back unchanged folds into one "Still:" line with a working fix.<br>• Chat moves to the right; Escape closes Build and chat.<br>• The phone's folded Next step is a button.<br>• The bell carries only what you can act on. | Fix | **Standing lines:** Cleo's Storage line on 5 of 5 cards; Ana's factory at 55, 59, 50 and 40%.<br>**Panels:** chat over the goals (ana d04 #140–#144, cleo d14 #350).<br>**Ben:** a step with nothing to tap (ben d36 #105 STUCK); *"21 bell notifications, I never look"*. | 4 | next (events later) | player |
| 12 | **The friend who lives elsewhere, and the skimmer.**<br>• The Crew panel and the Links tab list friends in other stations with 👁 Visit and "Link to Ben ▸". A visit leaves a line on their card.<br>• Send lines say what moved, or the real cause.<br>• One crew line on the return card. | Fix | **Visits:** six promised and *"where do you live?"* (ana d21 #306).<br>**Links:** Ben was offered as a link with no word that he was Ana's friend (ana d36 #366).<br>**Send:** Mia's moved nothing for 27 days.<br>**Ben:** *"a wall of 30+ messages … I understood little"* (ben d36). | 2.5 | next (crew line later) | social |

The table comes to about 41.5 days in all: 13.5 now and 28 next (row 3 is 12 of them). The events filter and the crew
line, about 2 days of that, can wait.

## 4. My final vote

**A mix.** First the fixes that give friends back to each other and let the shared build move:
- the door (row 1);
- a keystone that moves on (2) and needs two friends (4);
- the contract (5);
- the Wonder in sight (6);
- the stranger's seat (9).

Then one redesign: **every want is a buyer on the pipes, and the friends who can fill it see it** (row 3). It is the
systems designer's engine with my round-1 Ask and answer folded in as its social face: send from your Storage to a
friend's site, pay a friend's rest, name both ends.
- **It replaces** Deliver from storage, "N in your storages", the Storage chore for orders, the Wonders minigame and
  the Plan board's Ask form.
- **It keeps** dealt land, the one price, the Houses, Send, the project and the return card.

**What would change my mind:** the systems designer's replay from run 3's day-8 snapshot with only the fixes. If the
crew gets 100 bricks into Ana's Big Factory and the Dynamo to Bronze by hand, I'd shrink row 3 to its social face: a
friend's Storage feeding a friend's build, and paying a friend's rest, for about 3 days. If run 4 then shows sites
filled mostly by the hub and Houses rather than friends (under a third from friends), or "together" stays at 3.0 or
below despite friends' goods in each other's builds, goods aren't the lever. The next would be content that needs two
seats in Era 2 recipes.

## 5. The first two weeks of work, in order

1. **The keystone moves on** (1.5 days): the systems rule, the "seen by every active resident" clock and standing after
   two pass-overs; a small station keeps its stages. Replay from the row-9 snapshot: Capital SE's Workshop moves on.
2. **Find your friends** (2 days): the start screen, the kept seat on every return, the founder's line, the texts
   that place friends and "Copy Ben's link". e2e: Ben presses Play now before the link and lives in Ana's station by
   his second session.
3. **The line tells the truth** (2 days): one "can make" rule (deposits, not forest), "beside the water, not on it",
   the project line with its wait and its way through, and your own idle maker first.
4. **A keystone that needs two friends** (2 days): the pair rule, `Makers()` counting Yard inputs, both friends named
   on the card, and the House at 1.4 for an away seat.
5. **The Wonder in sight** (1.5 days): the tracker line with your part and Deliver, the Era button
   naming the goal, and the end on every card.
6. **The stranger's seat** (half a day): "next door" counts active residents.
7. **The decision replay** (the last day): run 3's day-8 snapshot to day 36 with Ana, Cleo and Dev on this build (about
   0.3M tokens). It decides whether row 3 is the full buyer engine (12 days) or its social face (3 days).

Then: the contract's dry-runs (row 5), row 3, rows 7, 10 and 8, and run 4.
