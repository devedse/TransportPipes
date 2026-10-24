# Round 1 · the systems designer (`systems`)

## 1. What run 3 says, through my lens

**The loop runs for ten days, then has nothing left to sell.** Week 1 works: Level 10 on days 4.9 (Cleo) and 6.4
(Ana), one-tap fixes, the Water Tower on Dev's wood. Around day 10 three systems run dry:
- **Research.** Era 1's tree costs 8,500 points, and Cleo's Lab makes about 107k a day. She had 449k on day 10
  (STUCK, cleo d10 #268) and 1.6M by day 21, more than all five eras' trees (1.42M). The Lab still ate 29 ingots a
  minute.
- **Gold.** On day 36 the crew held 95–100% of all it had earned (Ana 38.8M of 38.8M, Cleo 31.6M of 33.2M; metrics
  `earned` against `gold`). From day 20.9 to 35.8 its income rose 3%, its buildings 39 → 41. The advisor
  balance run (`docs/balance/balance-typical-advisor.md`) is flat from day 4 too, banking 107M.
- **Shared goals.** No stage stood after day 9 but a restart; the wonder got nothing.

**The cause is structural.** The game has two kinds of demand:
- **Buyers on the pipes** worked: hub sales, the Yard, the Houses, the station project. Ana's stone ran Cleo's
  Brickworks within minutes (cleo d14 #366).
- **Pulls from your own Storage** failed: hub orders (none after day 14), the Big Factory's 100 bricks (ana d21 #286
  STUCK), the Copper Pipe's ingots (cleo d21 #404 STUCK), and the Great Dynamo at 0 / 1,800 while Cleo's network made
  55 bricks a minute (cleo d21 #396) and 500 sat in a full Storage (#423).

Dealt land put exactly those materials on a neighbour's estate, and nothing carries them across: `Pay()` takes
materials only from your own Storages, hub imports feed only a building's inputs, Storages serve only their owner. The
content for weeks 3–5 (Market Hall, Steel Mill, Big Turbine, Copper Pipe) all needs them, so Era 2 (day 22) changed
nothing.

**The project froze on two rules.**
1. A keystone is passed over only if nothing at all came (`Delivered < 1e-6`, GameWorld.Projects.cs:433), and a
   House's capped quarter (1,800 of 7,200 water) counts. Capital SE sat at 96% from day 9.85 to 36.
2. "The lowest-tier good nobody makes" picks a good nobody needs: water, worth 0.5 g and used in Era 1 only by a
   Level-14 Coal Plant, was the keystone in all four stations by day 16.

And the reward is small: "+38 g/min for you" to Ana at 951 g/min (ana d27 #329).

## 2. The table

| # | Improvement (what changes, concretely) | Fix or redesign | Answers (findings, evidence) | Effort | Priority |
| --- | --- | --- | --- | --- | --- |
| 1 | **The keystone always moves on.**<br>• Each stage records when its keystone last got a delivery from a player.<br>• With a counted resident active, a keystone with no player delivery for 2 days is passed over. A House's delivery never counts and never restarts the 2 days.<br>• After two pass-overs, the stage stands on its made goods ("Capital SE's Workshop stands without water").<br>• The line says how long it has waited ("9 days waiting on water"). | Fix (rule) | **Where it froze:** Capital SE's Workshop sat at 96% from day 9.85 to 36 (water 1,990 → 2,003). Capital NW's Water Tower sat at 89% from day 6.13 (water 1,855). Both are about a House's cap (`npcCap` 0.25 × 7,200 = 1,800) plus one round.<br>**The rule:** `UpdateBaskets` checks `Delivered < 1e-6`.<br>**Players:** Mia STUCK (mia d15 #141); ana d27 #329. | 1 day, plus a replay from the row-9 snapshot. Risk: a stage stands too easily (keep "somebody active"). | now |
| 2 | **The keystone is a chain between friends, not a good nobody wants.**<br>• Choose it among tier-2 goods whose non-energy inputs are made by two different counted residents. In Capital SE that is bricks: Ana's stone plus Cleo's coal. Glass and copper ingots work the same way.<br>• Size it at 3 h of one level-1 maker (1,800 bricks).<br>• `Makers()` counts an input a neighbour makes, since the Yard brings it.<br>• `Makers()` checks dealt land's deposit, not forest tiles (`FitsOnEstate` checks `Placement.Forest`).<br>• Keep today's rule only for a station with no such pair. | Fix (rule) | **Water was the keystone in all four stations by day 16.13**, after wood and sand (metrics `projectBasket`). In Era 1 nothing buys water but a Level-14 Coal Plant.<br>**"Can make" named seats that hold no wood:** dev d08 #12, ben d01-2300 #34, Cleo's two STUCKs (cleo d06 #232, #244).<br>**The chain already exists:** stone into bricks formed by itself for the wonder (cleo d14 #341–#366), and bricks are what the middle game is missing. | 1.5–2 days. Risk: small stations with no pair need the fallback. | now |
| 3 | **Sites buy their materials (redesign, part 1).**<br>• A building, upgrade or pipe tier that needs materials becomes a site that buys them. First from your own producers on its pipes, then the station's Yard at the one price 1.3 (friends and neighbours, named at both ends: "Cleo's bricks built your Big Factory"), then the Houses.<br>• For any rest, the hub ships it in 12 h at 2.5×, paid in gold, or "Buy the rest now".<br>• Hub daily orders become a buyer at your trading post: 2× until filled, with no Storage and no Deliver.<br>• No goal needs a Storage any more. | Redesign | **Building materials:**<br>• The Big Factory needs 100 bricks (cleo d10 #281: "6.5M gold and no way to buy bricks"; ana d21 #286 STUCK).<br>• The Copper Pipe click did nothing (cleo d21 #404 STUCK).<br>• Market Hall needs 500 bricks, Steel Mill 300, Cement Kiln 200, Big Turbine 20 steel, so Era 2 brought nothing buildable.<br>• "I don't know how to hand you 100 bricks directly" (group, day 21).<br>**In the code:** `Pay()` uses `Materials()` (own Storages), and hub imports feed only building inputs (FlowSolver import step).<br>**The Storage chore:** three income drops right after building a Storage (cleo d01, ana d02, dev d08 #33), and the full-Storage line on 9 of 26 returns. | 6–8 days with the client. Risk: the flow solver and Yard rounds take a new buyer; the hub must stay slower than a friend. | next (before run 4) |
| 4 | **The Era wonder becomes the Capital's project (redesign, part 2).**<br>• It is one more buyer in every station's Yard round, after the station project. It pays the one price, and Send ▸ reaches it.<br>• Each stage is sized when it starts, from 6 h of the Capital's spare of its goods. Today it is sized once, when the world is made.<br>• Its line in every tracker shows your part: "Great Dynamo 41% · your bricks go in".<br>• Tiers pay hours of your own income: Bronze 1 h, Silver 3 h, Gold 6 h, Diamond 12 h.<br>• When the Era changes, a result card says how it ended. District Wonders work the same way. | Redesign | **Sized at world start:** 6 h × 60 g/min (`MinMeasuredIncome`) = 21,600 g, which became 1,800 bricks and 900 ingots (`StageRequirements`). That is 33 minutes of Cleo's bricks on day 21 (55 a minute, #396).<br>**Still 0:**<br>• Delivery is manual, from a 5,000-unit Storage that also takes energy (500 bricks, cleo #422–#423).<br>• "Deliver from storage" was disabled for Dev (dev d21 #178).<br>• Cleo found the wonder only by hunting (cleo d10 #293).<br>**Rewards and ending:** the tiers pay 2k–100k × Era (`TierReward`) against 17–20M banked. Era 2 came on the deadline (day 22 19:24) and nobody heard the result (ana d27 #318). | 3–4 days. Risk: four stations' rounds compete for one spare (the station project goes first). | next (before run 4) |
| 5 | **Max and upgrades count the pipe.**<br>• `LevelReach` (GameWorld.Max.cs) counts a supplier's spare but ignores the pipe's capacity. Cap each input at what the link can still carry (a grey connection takes 100 a minute).<br>• Check Next steps' upgrades the same way.<br>• Buttons show the net change, including sales a consumer eats ("+67 research/min · −29 ingots ≈ −400 g/min").<br>• "Spare" counts only what can reach. | Fix (rule) | **The Coal Pit:** an L25 upgrade for 117k left the network 237 energy short, at 61% then 47%, and 173 g/min (cleo d06 #184–#191). It then ran at 11–14% from day 10 to 21: "the grey pipe carries only 100/min" (group, day 21).<br>**The Lab:** Max cut Cleo's income from 783 to 285 g/min (#180).<br>**Ana's Small Factory:** 40–59% from day 8 to 36.<br>"Two numbers that don't agree" (cleo d21 #397). | 1.5 days. Risk: Max offers fewer levels, which is right. | now |
| 6 | **Labs rest, and points turn into growth.**<br>• A Lab with nothing it may research stops and eats nothing ("resting until Era 2").<br>• After an Era's tree come repeatable Refinements: +2% output of one chain each, 5k × 1.6ⁿ points, one at a time, kept up while away, capped at +50% a chain.<br>• "Pick one!" shows only when something can be picked. | Fix (rule + numbers) | **The tree is tiny next to a Lab.** Era 1 costs 8,500 points, and an L22 Lab makes about 107k a day.<br>**The bank:** Cleo had 449k on day 10 (STUCK #268) and 1.6M on day 21, against 1.42M for all five eras.<br>**The cost:** 29 ingots a minute (about 400 g/min) for points nothing buys ("wasted and eats my ingots", cleo d10 #267).<br>Runs 1 and 2 found the same. | 2 days. Risk: Refinements compound (hence the cap). | next |
| 7 | **Stage rewards that hit the bottleneck, and pay like the hub.**<br>• Water Tower: every seat's pipes carry +50%, using the existing research effect `throughput`.<br>• Workshop: +4% production and sites' materials 25% cheaper.<br>• The keystone pays the hub order's 2× base, not 1.3. | Fix (numbers) | **The rewards are small:**<br>• The Workshop promised "+4% … about +38 g/min for you" at +951/min (ana d27 #329).<br>• "Helping Ana does not pay me directly (+21 g/min when it stands)" (cleo d06 diary).<br>**The pay:** 1.30 a unit at the tower against 2.0 for hub orders (group, day 2 08:00).<br>**The bottleneck:** energy was short for weeks (row 5). | 1.5 days. Risk: a pipe bonus widens gaps between stations (small on a tier-1 pipe). | next |
| 8 | **Daily wants priced in hours of income.**<br>• Hub orders pay about 30 minutes of measured income. Today a Level-20 order pays about 14–27k (120 × L^1.35 × 1–2, ×2 for the reward) against about 1.3M a day.<br>• The Invest line offers the biggest useful buy: a site's hub rest, a parcel or a Max. Never 13k against 27.8M. | Fix (numbers) | **Orders:** none was delivered after day 14 (findings, chores).<br>**The Invest line:** "27796.8k gold banked: Max … for 13k g" (ana d27 #317).<br>**Players:** "best thing I could buy cost 15k" (mia d08 #109); "365k … a Solar upgrade for 329k felt like a trap" (dev d10 #70). | 1 day. Risk: more gold flows in. The order's real value is its XP and the habit. | next |
| 9 | **A small station keeps its finished stages when its Yard changes.** | Fix (bug) | **The restart:** Capital W started again at the Foundation on day 18.13, after "Nobody else here has played lately".<br>**The loss:** Mia-B's finished stages went from [Foundation, Water Tower] to [Foundation] (marks, day 20.9). She lost +3% production without doing anything, against "expansion over rebuilding". | 0.5 day. Risk: low. | now |
| 10 | **Measure interdependence by goods that built something.**<br>• Retire the 15% paid-flow mark and keep the flow share only as a watched number.<br>• New mark: a friend's goods are in your building, pipe or the wonder by day 14. | Fix (measure) | **The flow share:** the crew's paid share was 0.0%, 1.3% and 0.7% (days 13.8, 20.9, 35.8), and all three runs stayed under 4%.<br>**Why it can't rise:** iron ore is on every estate (WorldGenerator: "Energy, iron ore and water every estate has"), and its chain is Era 1's best. The dealt goods are tier 1 at 1.2–3.5 g, and the light bots found no content lever.<br>**What worked instead:** the one real chain between friends came from a goal, the wonder. | 0.5 day in the report tools. Risk: losing a guard (keep the number). | next |

Kept out of these findings, because they came from the playtest itself:
- The 4–9 days between sessions make each return's gold bigger: +1M to +12M per return.
- Cleo and Dev don't play after day 21 by the story, so the flat line from day 21 is partly that. The advisor run
  shows the same plateau without it.

## 3. Fixes or another redesign?

**A mix.** Fix the project now (rows 1, 2, 5 and 9). The redesign is rows 3–4: every want becomes a buyer on the
pipes.

Fixes alone won't do it:
- **The same findings keep coming back.** All three runs found gold and research with nothing to want (run 1: "gold
  stops mattering in week 1"; run 3: 0–5% spent), and the shared goals stalled a different way each time.
- **No AI player is needed to show it:** the advisor balance run's income is flat from day 4.
- **No price is in the way; a good that can't move is.** A real player visiting daily hits this wall sooner. Hundreds
  of Solar Panels (as the balance bot built) are a degenerate way out: that run's energy price fell to 0.42 of base.

What changes:
- **Materials.** A building, upgrade or pipe tier that needs materials becomes a site. It buys them from your own
  producers, then the station's Yard at the one price (named at both ends), then the Houses. The hub ships the rest
  in 12 h at 2.5×. Gold never reaches a dead end, and a friend is quicker and half the price.
- **Hub orders** become a buyer at the trading post.
- **The Era wonder** (and the District Wonders) become a buyer in every Yard round of the Capital. They pay the one
  price, are sized at each stage's start from what the Capital makes, show in every tracker and announce their result.

What it replaces:
- Deliver from storage, for orders and Wonders.
- "N bricks in your storages".
- The owner-only rule for Storages, as far as materials go.
- The Wonders tab as a separate minigame, and its sizing at world start.

What stays: dealt land, the one price, the Houses, Send, the station project and the return card.

**Cost:** about 10–11 days, mostly the Yard round (GameWorld.Yards.cs), which takes two new buyers. The fixes add
about 8 days.

**What would change my mind:** a replay from run 3's day-8 snapshot with only the fixes, in which the crew delivers
the Dynamo by hand and builds a Big Factory and an Era 2 building. That would mean the bottleneck was attention, not
structure.

## 4. Run 4: the marks I'd watch

1. **Shared goals move.** In every station with an active resident a stage stands at least every 4 days. No keystone
   waits more than 2 days while no player delivers it. Run 3: 27 days at 96%.
2. **The Era wonder stands.** The Dynamo's stage 1 stands before the deadline with 3 or more contributors, and each
   contributor's card names their part and the result. Run 3: 0 / 1,800, and no word.
3. **Goods that built something.** By day 14 a friend's goods are in a crew member's building, pipe tier or the
   wonder, named at both ends. Run 3: none.
4. **The ladder holds.** From day 14 to day 36 the crew's g/min rises 30% or more, and each member adds 4 or more
   buildings. Run 3: +3% from day 21, 39 → 41 buildings.
5. **Gold and points have a use.** By day 36 the crew spends 20% or more of what it earned, and no Lab runs with
   nothing to research. Run 3: 0–5%, and 1.6M idle points.
