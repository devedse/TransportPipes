# Round A · outsider: doors in the walls

My verdict before the details: **the game doesn't lack a place to spend gold.** It has one sink that scales to
billions: building levels, at ×1.15 cost and ×1.10 output per level (`progression.yaml`, `buildingLevels`). What it
lacks are **doors in the walls in front of that sink**. After week 1, every limit that decides income is priced in
something other than gold:
- the pipe carries 100 a minute per connection;
- the Trading Post sells 600 a minute;
- the Market Hall needs 500 bricks;
- a Copper Pipe needs one copper ingot a tile;
- research is gated by Era and level.

The keys to those limits are materials that only your own Storage can supply, and the screens never say which wall
you are standing at. So the bank fills up, and gold, the number the top bar shows biggest, stops meaning anything.

## 1 · Diagnosis: the wall, the sinks, or the loop?

**It is the loop, and the wall is where the loop breaks.** An idle game's loop is *earn → spend on what raises your
rate → earn faster*. Run 3 shows each step.

| Step | What run 3 shows |
| --- | --- |
| **Earn** | It works. Ana went 375 → 758 g/min from day 2 to day 8 (metrics). Every upgrade card paid what it promised (cleo d10 #288: *"Trading Post to L10 for 42k: income 505 → 610/min"*) |
| **Spend on what raises your rate** | It stops in week 2. Ana's income went 758 → 961 g/min from day 8 to day 35 (+27% in four weeks) while her buildings went 12 → 14 and her gold 5.4M → 39.8M, of 39.8M earned |
| **Why it stops** | Not for want of things to buy. The rate-raising buys hit a wall that gold can't open: <br>• Cleo's level-25 Coal Pit made 882 coal a minute and sold 100 through a grey pipe (cleo d21 #393); her Copper Pipe click needed 28 copper ingots and *"did nothing visible, no message why"* (#399–#404).<br>• The Big Factory wanted 100 bricks its card never named (cleo d10 #281; ana d21 #286, STUCK).<br>• The advisor bot, with no story and no AI player, read *"Your Trading Post sells all it can"* at hour 13. Its income stood at about 2,900 g/min from day 3 to day 28 while its bank grew to 107M (`balance-typical-advisor.md`) |

**And the cheap wants don't help.** Everything except levels is priced three orders of magnitude below the bank:
- the Big Factory 12k, the Market Hall 50k, a parcel 5k–80k;
- *"Gold is 2.3M and the best thing I could buy cost 15k"* (mia d08 #106);
- *"With 4M gold, the 25k upgrade and 10k parcel are trivial"* (cleo d06 #157);
- *"365k gold and no clue what to spend it on; a Solar upgrade for 329k felt like a trap"* (dev d10 #68). Dev's "trap"
  is the point: the one big buy he was offered went into a wall.

**So the answer isn't new sinks.** A monument, a Bank that turns gold into more gold, or a cosmetic shop would each
give the 40M a place to go, and leave the rate and the friends where they are. In an idle game a sink must change what
you see (a building that stands, a pipe that carries more, a friend's goods in your build), or it is a number moving to
another number.

**What players came back for** in run 3 was a friend counting on them and a goal that moved (the player researcher's
diary count). That is the other half of the answer. **The keys to the walls are the friends' goods:**
- bricks are stone + coal, which dealt land puts on two seats;
- copper ingots are copper + coal;
- steel is iron + coal.

The redesign put the needs on friends' land on purpose, and then left no door for them to come through.

## 2 · Rows 7 and 10: adopt both, with four changes

**Row 7 ("every want is a buyer on the pipes"): adopt.** It is the door. Four changes:

1. **One word for all of it: "wants".**
   - Players already read *"🏗 Water Tower 89% · wants water"* on every card in run 3.
   - So a building waiting for materials *wants* bricks, a pipe waiting for its tier *wants* copper ingots, the Great
     Dynamo *wants* bricks and ingots, and the hub *wants* 2,000 iron ore today.
   - No "site", no "Ask", no "Deliver", no "basket" on any screen. The game already has the grammar; it just used five
     words for one idea.
2. **The pipe is a want too.**
   - When a connection binds a building, its panel offers the pipe tier for that connection, in place, as a want:
     > *"Sells 100 of its 882 a minute: the grey pipe to your Trading Post carries 100. Copper Pipe, 28 tiles: 280 g
     > and 28 copper ingots · Start it ▸"*
   - The ingots then come by pipe like any other want's goods. That turns Cleo's dead click into the run's most
     natural two-friend chain: her coal and a copper seat's ore make the ingots her own pipe needs.
3. **One line: "what stops it".**
   - Every producer's panel, the return card's growth line and the Invest line name the **one binding limit**, taken
     from the solver (the analysts' IdleReason and what-if, rows 2–3), with the key and its price.
   - When the key is in, the same line becomes *"Max now pays: +1,400 g/min for 2.1M · Max ▸"*.
   - This makes gold matter again without a new sink: the 1.15× ladder pays once more.
4. **Houses never fill a want's materials** (open question 1, below). Houses keep selling **land goods** under their
   floor (`houseSellLand`, 1.4), so anyone can make any material themselves.

**Row 10 ("gold and research points have a use"): adopt.**
- **Labs rest** when nothing can be researched.
- **Refinements** follow an Era's tree (5k × 1.6ⁿ points, capped). I add one change: **the first refinement lines raise
  the walls, not output.** *Pipes carry +10%*, *Trading +10%*, *Hub Order +5%* before *+2% to a chain*. A +2% output
  refinement on a building already capped at its pipe is another trap like Dev's.
- **Hub orders and the Invest line in hours of income:** yes, and the Invest line stays quiet unless its buy passes the
  what-if (*"27796.8k … for 13k g"*, ana d27 #317, taught players to ignore the card).

## 3 · The proposal in what players see and do

**The growth line.**
- Every return card has one growth line, under the people line. It always has the same shape:
  > *"What stops you: [the limit] · [the key] · [price] ▸"*
- It names one wall: the one whose key adds the most gold a minute.
- On a phone it is one tap: *Start it ▸* puts a scaffold on the estate.

**The scaffold.** A building, a Market Hall or a pipe that waits for materials stands on the map as a crane and
scaffold, with a sign: *"Market Hall · wants 500 bricks · 120 in"*. Nothing looks finished until it is: pipes are the
heart, and you can watch the goods arrive.

**How a want fills, in this order:**
1. Your own producers, through your pipes.
2. A friend's Storage or spare, by **Send ▸** on their card: *"Ana's Market Hall wants 500 bricks · your Storage holds
   500 · Send them ▸"*. It goes straight into the build, named at both ends, with 💛 Thanks.
3. The station's spare through the Yard, by itself at the one price, even while friends sleep (`MatchAsks` already does
   this).
4. If nobody has any, the want shows the chain that makes it:
   > *"Nobody near you makes bricks yet · a Brickworks (1,400 g) uses your stone and Bertha ⚙'s coal · Show me"*

**Gold's use, at last.** Starting a want costs the building's gold part. After the key, the "what stops it" line
offers the levels that now pay, sized to the bank (F3's effort term). Once a gate opens, the 40M has a destination
that grows the rate: upgrades, then the next key, then upgrades.

**The Era wonder is the Capital's want.**
- It is sized when each stage starts (row 6), and fed by every resident's pipes in each Yard round.
- Its tiers pay hours of your own income.
- Every card tells how it ended.
- Houses bring at most a quarter, as for any shared goal.

**Words:** "wants" and "what stops you" are plain English, and the scaffold is a picture. The first-evening budget
is untouched: the first wall comes in week 2.

## 4 · The three open questions

**1. Can gold buy at once, and at what price? No: gold never buys a want's materials, at once or at a deadline.**
- **Price can't make a friend matter.** 500 bricks at the House's 2.0 × base (5 g) cost 5,000 g, 0.013% of Ana's 39.8M.
  At any price a rich player would pay, and friends' bricks would be decoration.
- **A deadline can't either.** Players return every 2–7 days, so a 12-hour House fill always lands before they're back.
- **Under this rule:**
  - gold pays the gold part, and buys land goods from Houses under the floor;
  - the material is made by somebody's building: yours (always possible, from your land and Houses' land goods) or a
    friend's (faster, at 1.3, with a name);
  - the House fill at the deadline (`AskHousePrice`, GameWorld.Asks.cs:33) stays only for the Ask's old use, if any;
    wants don't have it.
- **Nobody is stuck,** because the chain is always buildable, and its line says how. **A friend matters** because
  their goods are there today and yours would need a new building first.
- **Expansion, never a loss:** a want unfilled for three play days offers *"Cancel · your 50k back"*.

**2. Do hub daily orders become buyers too? Yes, in the same step.**
- *"The hub wants 2,000 iron ore today · pays 2× · it takes from what your Trading Post sells"*. No Storage, no
  Deliver.
- Hub orders died after day 14 in run 3 for the reason the analysts give: they needed a Storage nobody mentioned (run
  1's *"Your storages have no iron ore"*, ana d21 STUCK).
- One rule ("things want goods, goods go by pipe") is easier to learn than one rule and one exception, and the engine
  is the same.
- Sized at 30 minutes of measured income (row 10), so the order is a bonus for doing what you do, not a chore.

**3. A keystone that needs two friends: now, but not as a keystone rule.**
- The wants carry it on their own. The first walls players meet need two seats' goods:
  - the Market Hall and the Big Factory want bricks (stone + coal);
  - the Copper Pipe wants copper ingots (copper + coal);
  - the Steel Mill wants bricks;
  - the Dynamo wants bricks and ingots.
- The run's one "together 4" had exactly this shape, and it came from a want (Cleo's Brickworks for the Dynamo, cleo
  d14 #341–#368), not from the project's rule.
- Keep the project's keystone as row 1 now fixes it (it moves on), so run 4 can read one change at a time (the player
  researcher's point).

## 5 · Weeks 2–5, walked through

The gold-a-minute figures in this section are illustrations; the light bots set the real ones.

**Ana** (stone and iron; day 8: Level 13, 758 g/min, 5.4M)
- **Day 8.** The growth line: *"What stops you: your Trading Post sells all it can (600/min) · a Market Hall sells
  6,000 and pays 5% more · 50k g + 500 bricks · Start it ▸"*. She taps it, and a scaffold rises beside her post.
- **Day 10.** Cleo's card offers *"Ana's Market Hall wants 500 bricks · your Brickworks makes them from Ana's stone ·
  Send ▸"*. The bricks are in that evening, then *"Cleo's bricks built your Market Hall 💛 Thanks"*.
- **Day 14.** Her growth line becomes *"Max your Quarry now pays: +1,100 g/min for 1.9M"*. She spends a third of her
  bank and her income steps up.
- **Weeks 3–4.** Era 2: her Steel Mill wants 300 bricks (Cleo's again), and her Steel Pipe wants steel (her own mill).
  Her diary has something to want every session.

**Cleo** (coal; day 10: 7.3M and 449k research points)
- **Day 10.** Her Coal Pit's line: *"Sells 100 of 882: the grey pipe … Copper Pipe, 28 tiles: 28 copper ingots ·
  Start it ▸"*. Nobody makes copper ingots, so the want shows the chain: *"a Smelter (1,800 g) uses your coal and
  Bertha ⚙'s copper"*. She builds it; 28 ingots take about six minutes.
- **After the pipe.** It upgrades in place, the pit sells 500 a minute, and Max pays again.
- **Research.** Her points go to *"Pipes carry +10%"* refinements, which lift every capped link at once. The optimiser
  gets a ladder that is hers.

**Dev** (wood; arrives day 8; day 10: *"365k gold and no clue"*)
- **Day 10.** His card says two things:
  - *"The Water Tower wants wood · you make it · Send ▸"*, the run's real moment for him;
  - *"What stops you: Lumber Camp at its pipe…"*, or *"Big Factory: 12k g + 100 bricks (Cleo can ›)"*.
- The Big Factory's ingots then go into the Great Dynamo's want. The late friend is useful in his first week, which run
  3's *"I feel late and useless"* (dev d21) says he wasn't.

**Ben** (phone; plays days 1–3, back on day 36)
- **While he's away:** no want is raised for an absent player. His resting dredger's sand fills whatever wants sand
  through the Yard by itself. His return card: *"While you were away: your sand went into Ana's Glassworks and the Water
  Tower"*.
- **On day 36:** one growth line, one tap: *Start it ▸*, opened by *Show me* as a bottom sheet.

**Mia, alone beside Jo**
- **Day 15.** Her growth line: *"Market Hall · 50k g + 500 bricks"*. Nobody near makes bricks, so the want shows the
  chain: *"a Brickworks uses Bertha ⚙'s stone and coal at 1.4 · Show me"*.
- She builds it, and the Market Hall stands two sessions later. Slower than the crew, never stuck.
- If Jo has bricks, his Send is a stranger's gift with her name on it: the "alive" moment she got in run 3 only from
  the chat.

## 6 · Cost and risks

**Cost** (one AI developer, each step playable):

| Part | Days | Note |
| --- | --- | --- |
| Wants on the existing Ask engine: into the build, not a Storage; the scaffold; Send from Storage | 6–8 | Row 7's estimate. `MatchAsks` already runs in every Yard round |
| The pipe tier as a want, in place | 1–2 | The upgrade command exists (cleo d21 #399); only the materials' path changes |
| "What stops it": the binding limit from the solver, its key and price | 2 | Shares rows 2–3's IdleReason and what-if |
| Hub orders as wants at the Trading Post | 1 | The same engine |
| Labs rest; refinements with wall lines first | 2–3 | Row 10 |
| The Era wonder as the Capital's want | 2–3 | Row 6/7 |

That is **about 14–19 days**, mostly inside rows 6, 7 and 10's estimates. Only the "what stops it" line and the pipe
want are new work.

**Risks:**
1. **Incomes jump once the walls open** (a Market Hall is ×10 trade). XP and levels, the light bots and run 4's income
   marks all move.
   - *Check:* the light-bot runs before run 4 with the doors open. Watch Level 30's day (25 ± 1) and the hub's √ (the
     next wall, which the Market Hall's +5% and the far markets meet).
2. **A want nobody fills becomes the next frozen keystone.**
   - *Answer:* the line always shows the chain that makes it, with Show me, and a want unfilled for three play days
     offers Cancel with the gold back.
3. **Without a House fill, a skimmer may never build the chain.**
   - *Answer:* Show me makes the chain one tap; the Yard fills from the station by itself; Ben's sand works for others
     while he's away.
   - *What would change my mind:* if run 4 shows wants sitting unfilled for a week while the owner plays, allow a House
     fill of **land goods only** into the want's chain, never the finished material.
4. **A line that lies.** "What stops it" must name the solver's binding limit, and its key must pass the what-if.
   Otherwise it is *"Your buildings are 226 energy short"* again (the bot's 30 lines in a row).
5. **Cards fill up.** One growth line per card, under the people line (row 11's order).

**How run 4 shows it worked:**

| Mark | Pass | Run 3 |
| --- | --- | --- |
| Gold spent | By day 36, each crew member has spent 20%+ of what they earned (analysts' mark) | 0–5% |
| Growth after the wall | Income +30% from day 14 to 36, and 4+ buildings each (analysts' mark) | +27% from day 8 to 35, 3 buildings in all |
| Keys that work | Every "what stops you" key a player completes raises that building's sales within one round. Zero lying lines | — |
| Friends hold keys | A third or more of the materials that go into wants come from someone other than the owner, named | none |
| Diaries | No *"nothing to buy"*, *"gold feels empty"* or *"no clue what to spend"* after day 14 | in every run |
