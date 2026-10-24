# Round A · psychologist: what gold is for

I read the brief and its pipe addition, run 3's findings and diaries (from day 6), `metrics.jsonl`, the review and the
analysts' final papers, and checked `buildings.yaml`, `pipes.yaml` and `GameWorld.Franchise.cs`.

## 1 · Diagnosis: not missing sinks, missing reasons

The owner is right that gold stopped mattering. "Add sinks" is the wrong reading of why. The game already has sinks
nobody used:
- every upgrade costs 1.15× the last and pays back in 1–2.5 h;
- decorations cost 20 g to 20,000 g (`buildings.yaml` 752–760);
- parcels double in price.

Players sat on 95–100% of what they earned anyway: Ana 39.8M of 39.8M, Mia-B 27.7M of 29.2M (day 36 sample). A pile
is not the problem. A pile with nothing *wanted* to spend it on is. Three causes stack, and only the first is
structural.

**1. The wall (structural; the analysts are right).** The wants of weeks 2–5 exist, but they need materials only
your own Storage can supply:
- the Big Factory needs 100 bricks, the Market Hall 500, the Steel Mill 300 (`buildings.yaml` 309–330, 539–548);
- `Pay()` takes materials only from your own Storages;
- the brief's addition: past 100 a minute per pipe connection, upgrades stop paying, and the copper or steel that
  raises the limit is again a material from your own Storage.

On dealt land those materials lie on a friend's estate.

**2. Spending was punished, so players learned to hoard.** This is the cause nobody has named.
- **Cleo spent freely on day 6** and lost:
  - the Lab's Max (238k) took her income from 783 to 285 g/min (cleo d06 #178–#180);
  - the Coal Pit's level 25 (117k) bought 882 coal a minute, of which the pipe passes 100;
  - after those two her income was 173 (#190).

  In her next three sessions she spent only on things she could check first: *"Trading Post to L10 for 42k: +105,
  promise was +106. Numbers agree"* (d10 #288).
- **Dev:** *"a Solar upgrade for 329k felt like a trap"* (dev d10 diary).

When a purchase can silently cost income, and the card blames the wrong thing, the safe play is to keep the gold:
loss aversion plus unpredictability.

**3. Gold had no route into what players came back for.** The player researcher's count: friends counting on them,
and a goal moving. Never gold after week 1. The diaries say why they'd return:
- *"my friends are waiting on me"* (dev d08);
- *"Ana is counting on iron ingots"* (dev d14);
- *"Era 1 ends in 8 days and the friends need my stone"* (ana d14).

And the players kept trying to turn gold into help, the only use they could imagine:
- *"I can fund whatever the tower needs"* (Cleo, group, day 2);
- *"I can pay for anything: 3.5M banked"* (day 6);
- *"I have 9.9M idle, can pay for upgrades"* (day 14);
- *"I have 19M gold, say if you need money 💛"* (Ana, day 21).

Run 1's Ana said it outright: *"Gold piles up with nothing to spend it on that helps my friends."* Every purchase the
game offered was solo and productive: more of the same, measured in g/min. Once a session's away pay was 3–9M (Ana,
day 21: +8.7M), g/min stopped meaning anything. Mia: *"the best thing I could buy cost 15k; the numbers feel
meaningless"* (mia d08); *"the gold feels empty"* (d15).

**So the answer isn't a bigger sink.** Give gold *jobs* that serve the three motives the players showed:
- **to build what they're proud of**, which means making it reachable;
- **to help their friends**;
- **to grow the place they share.**

Add the safety that makes spending feel smart again. Decorations prove that a sink without a motive does nothing.

## 2 · The proposal: "gold builds, friends fill"

Five parts. The first is the analysts' row 7, which I adopt with one change.

### 2.1 Every want is a site, filled through the pipes (row 7, adopted)

- **A site.** A building, upgrade or pipe tier that needs materials becomes a site the moment you place it. It buys
  its materials through the existing Ask engine (`GameWorld.Asks.cs`, already in the Yard round):
  1. your own producers;
  2. friends' and neighbours' spare at the one price, named at both ends;
  3. a House at the deadline.

  Materials go into the site, never into a Storage.
- **The friends' side.** The need shows once to the friends who can fill it: *"Ana's Big Factory waits for 100 bricks
  · your Brickworks sends its spare · or send the 500 you hold ▸"*. When it stands: *"Cleo's bricks built your Big
  Factory 💛 Thanks"*.
- **The pipe is a site too** (the brief's addition). A link at its limit says so on the producer: *"This pipe carries
  100 a minute · a Copper pipe here: 28 copper ingots + 280 g · Cleo's Smelter makes them · Order it"*. Ordering it
  makes a site, filled like any other.
  - On dealt land the copper usually comes from a friend, so the pipe wall becomes a gift.
  - Max stops at the pipe's limit, unless a pipe is ordered with it.
- **The Era wonder and the hub orders** become buyers on the same rails (row 7, and the player and social analysts on
  orders). No Deliver, no Storage.

**My one change: gold never buys at once (open question 1, below).**

### 2.2 Spending is safe

**One sentence before the click, from the solver, on every purchase** (row 3, made universal):
- *"+105 g/min"*;
- or *"+0 g/min: this pipe carries 100 a minute · order a Copper pipe with it"*;
- or *"−498 g/min: the Lab eats 29 of your iron ingots"*.

**Undo for an hour, with a full refund, on any build or upgrade.**
- Cleo would have undone the Lab's Max at 14:03 and kept her trust in the game.
- Reversible choices make people try things (the old v1.2 rule: every choice can be undone).
- It is cheap: an hour's ledger of the last purchase.

### 2.3 Wants priced in time

Every gold cost shows beside it as time: *"Market Hall · 50k g = 50 min of your income + 500 bricks"*.
- People judge prices against income, and an absolute number means nothing once you hold 8M.
- **The Invest line** on the return card stops offering *"Max … for 13k"* against a 27.8M bank (ana d27 #317). It
  names the biggest open want instead: *"Your gold could build: the Market Hall · Cleo makes its bricks · Show me"*.
  If nothing is worth the bank, it stays quiet (row 10).

### 2.4 Gold for the shared place: the Landmark keeps growing

**Why.** When a station's project has stood, its Landmark goes on as **public works**. This is Animal Crossing's best
Bell sink: bridges and slopes the player pays for on the island everyone visits. The Bells meant something because
they built the shared place.

**What a tier is:**
- **Gold from any resident,** one tap: *"Give 2 h of your income"*.
- **Materials through the Yard:** bricks, then glass, then steel by Era.
- **Size:** tier n costs 2ⁿ⁻¹ days of the station's summed measured income in gold (1, 2, 4, 8 days…), and materials
  for 6 h of one level-1 maker per good.
- **Gold alone never finishes a tier:** the materials are the friends' part.

**What it gives:**
- every tier changes the Landmark on the map, the picker's station card and "Today here" (*"Capital SE's Landmark rose
  a floor: Ana's gold, Cleo's bricks, Dev's planks"*);
- every seat gets +1% production per tier, at most +10%. That is small on purpose: the reward is the look and the
  names.
- The ring names who gave, gold and goods alike, and never who didn't.

**Why the size grows:** an 8M bank meets a want its own size by week 4. It needs art for 3–4 tiers through the art
pipeline (`process.mjs`, `fit.mjs`).

### 2.5 Research points have a use (row 10, adopted)

- **A Lab with nothing to research rests** and eats nothing. Cleo's Lab ate about 400 g/min of ingots for points that
  bought nothing (cleo d10 #268).
- **After an Era's tree, Refinements:** +2% for one chain, 5k × 1.6ⁿ points, capped at +50% per chain.
- **Why a ladder per chain:** an optimiser chooses an identity ("the brick seat") rather than just topping up a
  number.

## 3 · The three open questions

**1. Can gold buy at once? No.** Gold can pay for the rest, but the House brings it at the site's deadline: 12 h for a
building or pipe, the stage's deadline for the wonder. The price is 2.0 × base, and the gold leaves the game.
- **Who can pay:** the owner, or any crew mate. Same-device accounts can't.
- **What friends save you is time.** Their goods arrive within a Yard round at the one price, named. A House's arrive
  tomorrow, unnamed.
- **Evidence:**
  - Run 3's one felt gift was a friend's goods in a friend's building (Ana's stone in Cleo's Brickworks, cleo d14
    #366–#368).
  - Gray (2012): a gift believed to be chosen is experienced as better.
  - Stardew's Joja route: you may buy the Community Center with gold, but nobody remembers that version. The bundles
    of goods are what players love.
  - If 39.8M can buy 100 bricks now, Cleo's Brickworks never matters again.
- **It also gives gold its social job.** *"Pay the rest for Ana ▸"* is the button behind every *"say if you need
  money"* in the group chat. It is a gift that pays nothing to anyone and leaves the friends' part intact.

**2. Do hub daily orders become buyers? Yes.** An order buys at your trading post as your goods pass: no Storage, no
Deliver, no expiry penalty. Its pay is shown in hours of income.
- Orders stopped after day 14 (review) because they needed a Storage chore for gold nobody wanted.
- As buyers they cost no attention. They appear on the return card only once they are filled (*"Your coal filled
  today's hub order"*), never as a task.

**3. A keystone that needs two friends: now.** From the Workshop on, where the station has such a pair.
- **What it is:** a tier-2 good whose land inputs lie on two active residents' seats: bricks (stone + coal) or copper
  ingots (copper + coal).
- **Why now:**
  - The run's only "together 4" had exactly this shape (cleo d14).
  - Water, the keystone in all four stations, needed nobody.
  - The pass-over rule (row 1, built) already guarantees no freeze: an absent friend's input comes from the seat's
    House, and after two days the keystone moves on.
- **The player researcher's worry about readability** is real. But run 4 is meant to test the late game together, and
  this is the cheapest proven pull toward it.

## 4 · Walk-through, weeks 2–5

Rows 1–6 are built and Ben is in Ana's station (row 4). Run 3's dealt land: Ana stone, Cleo coal, Dev wood, Ben sand
and water.

### Week 2 (days 8–14)

**Ana, day 8** (Level 12, 3.1M).
- Her card: *"Your gold could build: a Big Factory · 23 min of income + 100 bricks · bricks need your stone and Cleo's
  coal · Show me"*. She places it, and it becomes a site.
- Group chat: *"Cleo, if you build a Brickworks, my stone becomes your bricks and your bricks my Big Factory 🫖"*.
- A plan between friends now has a site on the map.

**Cleo, day 10** (6.5M, 449k points).
- Her Lab rests (no ingot drain), and her points buy Refinements (*"Coal +2% · 5k points"*).
- Her Coal Pit reads *"This pipe carries 100 a minute · a Copper pipe here: 28 copper ingots"*. She orders it, and her
  own Smelter fills the site. (On a seat without copper, a friend's would.)
- On day 6 the Lab's Max showed *"−498 g/min"* before the click. She'd have skipped it, or undone it at 14:03.
- She keeps trusting the numbers, which is her whole persona.

**Dev, day 10** (365k): instead of *"a 329k trap"*, his card names a want he can reach, a Small Factory whose ingots
the Great Dynamo buys by itself.

### Week 3 (days 14–21)

**Cleo, day 14:** she builds the Brickworks on Ana's stone, as in run 3. Now her bricks have three buyers on the
pipes:
- Ana's Big Factory site (100);
- the Great Dynamo (1,800);
- her own Market Hall site (500).

**Day 15.** Ana's card: *"Cleo's bricks built your Big Factory 💛 Thanks"*.
- Ana taps Thanks.
- Ana: *"your bricks!! 🎉"*. The felt gift, both ways, and this time it built something lasting.

**The Dynamo.**
- 1,800 bricks at 12–55 a minute is 0.5–2.5 h of Cleo's real output (social analyst), and Dev's ingots flow in by
  themselves.
- It reaches its first stage before day 22. Every card tells the result: *"The Great Dynamo stood: Cleo's bricks,
  Dev's ingots, Ana's stone in every brick"*.
- Ana's run-3 *"Era 1 ended without me knowing"* becomes the crew's first shared victory.

**Ana, day 21** (8.7M): she taps *"Pay the rest for Cleo's Market Hall ▸"*.
- A House brings the last 200 bricks by tomorrow evening.
- *"I have 19M gold, say if you need money"* finally has a button.

**Dev, day 21:** *"I feel late and useless"* becomes *"Dev's ingots: a quarter of the Great Dynamo"*.

### Weeks 4–5 (days 22–36)

**Era 2:** Ana's Steel Mill site takes Cleo's bricks and coal, and Cleo's Steel pipe site takes Ana's steel. **The
first loop of gifts both ways.**

**The Workshop's keystone is bricks.** It needs Ana's stone and Cleo's coal, and the card names both.

**After the Workshop, the Landmark's first tier: 1 day of the station's income.**
- Ana's and Cleo's piles finally meet a want their own size. Ana gives a day of income in four taps over two sessions.
- Dev's planks fill its materials.
- It rises on the map, and the picker shows Capital SE with a Landmark of two floors.

**Ben on day 36** (5.5M after a month).
- His resting estate's sand went into the station's Glassworks and the Landmark's windows: *"Your sand is in Capital
  SE's Landmark"*. That is the "someone needed you" line, now about a thing on the map.
- His phone quick win: *"Your gold could build: a Big Factory · Cleo's bricks · Show me"*. One tap on the golden tile.

**Mia alone beside Jo** (Jo never chats).
- Her Big Factory site fills from her own producers, then Jo's spare if he has any. Otherwise she taps *"Pay for the
  rest"*, and the House's bricks arrive by her next session. Her gold has a job, and she is never stuck.
- Her small station's Landmark grows on her gold and Jo's goods.
- "Today here" tells the Capital: *"Mia's Landmark rose a floor"*. Ana and Mia-B see it.
- *"I'd love bigger things to buy"* (mia d08) is answered by things others can see.

## 5 · Cost

| Part | Days |
| --- | --- |
| Sites and the friends' side, pipes as sites, hub orders and the wonder as buyers (row 7) | about 9 (analysts' 8–12; systems: the Ask engine already sits in the Yard round) |
| Spending is safe: the one-sentence preview everywhere (row 3, already "now") and Undo for an hour | +1 |
| Wants priced in time, and the Invest line naming a want | 0.5 |
| The Landmark's public-works tiers | about 3, plus art for 3–4 tiers |
| Labs rest and Refinements (row 10) | 2 |

About 15 days, most of it row 7, which is coming anyway.

## 6 · Risks

| Risk | Answer |
| --- | --- |
| **Obligation.** A friend's site waiting on your bricks turns into "Cleo is waiting on me", guilt in a new form (Ben's sand in run 1) | The need shows once, only to those who can fill it. A House fills it at the deadline. Nothing ever says "unanswered", and no list names who didn't help |
| **Houses fill everything, and friends stop mattering** | Measured: under a third of site materials from friends means the reason to help is too weak. Then the two-friend keystone and the Landmark's materials carry more |
| **A whale funds the Landmark alone** (Cleo had 30M) | Allowed. Gold can't finish a tier without the friends' goods, and the ring names every giver. Ringelmann's loafing needs a pool where effort vanishes; here the names stay |
| **Too many wants on the card** | The card shows one want (the biggest the solver says pays or that friends can fill) and folds the rest (row 11) |

## 7 · How run 4 would show it worked

| Mark | Pass | Run 3 |
| --- | --- | --- |
| Earnings spent, days 14–36 | ≥ 20% | 0–5% |
| New buildings each, days 14–36 | ≥ 4 | 1 |
| Site materials from players, not Houses | ≥ ⅓ | — |
| Diaries calling gold empty, pointless or a trap after day 10 | 0 | at least 5 |
| A diary that names a want ("I want to build the Market Hall") | in at least half of the sessions after day 10 | — |
| Undo | used at least once, and no diary blames an upgrade for lost income | Cleo's −500 g/min, unexplained |
| Late game, days 18–36 | fun ≥ 3.5, "come back" yes in 3 of 6 | 3.0 and 1 of 6 |
| The Great Dynamo | stands before its deadline with 3+ contributors, named | 0 / 1,800 |

If gold is still 95% unspent and nobody calls it empty, the pile is harmless. The test is the second line: whether
people built.
