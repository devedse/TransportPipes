# Round A · wild

## Diagnosis: the scarce things can't be bought, and the buyable things aren't scarce

Gold piling up is the symptom. Run 3's numbers show three causes, and they share one root.

**1. After day 2, no gold price is a decision.**
- Every gold price in the game is a fixed number: a 5,000 g parcel, 1.15× per building level, a Big Factory at 12k.
- Income compounds and lands while you are away. Ana earned about 1.3M a day from day 21 (929 g/min, metrics day 32).
- So the bank outran every price by week 1:
  - *"Gold is 2.3M and the best thing I could buy cost 15k"* (mia d08);
  - *"27796.8k banked: Max … for 13k"* (ana d27 #317).
- Ana held 4 parcels from day 1 to day 36 at Level 24 (metrics: `parcels` 4), although parcels 5–9 cost 155k in all.
  She had nothing to put on them that would pay.

**2. The things that do limit growth can't be bought, least of all with gold.**
- **Goods in the right place.** Every bigger building needs materials that only your own Storages supply (`Pay()`,
  GameWorld.cs). On dealt land those materials need a friend's land good: *"I don't know how to hand you 100 bricks
  directly"* (Cleo, group, day 21).
- **Throughput** (the brief's addition). A grey pipe carries 100 a minute per connection.
  - Cleo's level-25 Coal Pit made 882 coal a minute and sold 100 (cleo d21 #393).
  - The way past is a Copper Pipe, which costs a copper ingot per tile from your own Storages, and she had none (cleo
    d21 #399–#404).
  - So the obvious sink, upgrades, became a trap the players learned to fear:
    - *"a Solar upgrade for 329k felt like a trap"* (dev d10 #68);
    - the Lab's Max cut Cleo's income from 783 to 285 g/min (cleo d06 #180).
- **Time.** Levels, Eras, and one research at a time.

**3. The root: every road past a limit runs through your own Storage, and gold never touches it.**
- The bots show it without any story:
  - The scripted `typical-together` bot keeps upgrading Solar Panels, the one thing with no input and no material. It
    grows from 3,618 to 4,392 g/min between days 14 and 28 (`balance-typical-together.md`).
  - The advisor bot hits one water dead end and is flat at 2,884 g/min from day 4 to day 21, banking 78M
    (`balance-typical-advisor.md`).
  - Humans with 11–16 buildings hit the pipe and the materials wall in week 2, and stopped: three buildings added
    between day 15 and 36.

So the owner is right, and it is deeper than "add sinks". **Gold must be able to buy what is scarce, at a price that
grows with income, without replacing the friend.** The friend is the other scarce thing, and the only one players came
back for in week 3 (*"a friend counting on them and a moving shared goal"*, the brief).

## The proposal: gold widens the pipes, friends fill them, and the Capital grows with both

Three sinks, each aimed at one of the scarce things, each priced in **hours of your own measured income**. I(c) already
exists: a 24-hour average of production gold a minute, `MeasuredIncome`, GameWorld.Projects.cs.

### 1. Widen the pipe: gold buys capacity, at once

**The rule.** When a building's output (or input) is held back by its pipe, its panel says so, and offers one button:

> *"Its pipe carries 100 of 882 coal a minute · **Widen ×2** · 42k g (1 hour of your income)"*

- **What a widening does:** it doubles the throughput of that connection's path (every tile on it). A second widening
  makes it ×4, at 2 more hours of income.
- **The cap:** ×4 on any tier. Grey goes 100 → 400 a minute; Copper 500 → 2,000.
- **Beyond ×4** the next tier is the way, and it needs that tier's material: *"Copper Pipe: 500 a minute, ×4 → 2,000 ·
  needs 26 copper ingots · your crew can bring them ▸"* (part 2).
- **What gold buys is capacity, never goods.** Capacity is yours to buy, and goods stay a friend's to give.

**Why it is the right sink:**
- It turns every upgrade back into growth up to 4× the pipe. That is the step Cleo's pit, Dev's sun and the Lab traps
  were missing.
- Its price grows with income, so it is never trivial and never out of reach.
- It is pipes, the heart of the game.

**The numbers, Cleo on day 21** (I ≈ 700 g/min):
- ×4 on her pit's link costs 3 h = 126k, which is 0.8% of her 16.4M.
- It sells 400 coal a minute instead of 100: +300 × 2.5 g = up to +750 g/min, under her Hub Order. That pays back in
  about 3 hours.
- Her next upgrade then pays again, up to 400 a minute.
- Past that, Ana's copper (a friend) or Cleo's own (if dealt to her) makes the Copper Pipe.

**Build:** a `width` on pipe tiles (1, 2 or 4) multiplies the tier's throughput. `Trace` already takes the path's
lowest throughput (GameWorld.Networks.cs). Add a panel button and one price rule. About 2 days.

### 2. Every want is a buyer on the pipes (the analysts' row 7, adopted, with its money rule)

I adopt row 7 whole:
- **Sites.** A building, upgrade or pipe tier short of materials becomes a site that buys them through today's Ask
  engine. The order is your producers, then friends' and neighbours' spare at the one price (named at both ends), then a
  House. Materials go into the site, never into a Storage.
- **The Era wonder** buys in every Yard round, sized at each stage's start.
- **Friends** see the need once on their card, can send what their Storage holds with one tap, and get *"Cleo's bricks
  built your Big Factory 💛"*.

**The money rule for a site:**
- **The friends' window.** A site waits for friends' and neighbours' spare for **24 hours**. After that, a House
  delivers the rest **as a flow over 12 hours** at 2.0× base.
- **"Don't wait ▸"** (one tap) starts the House's 12-hour flow at once.
- So a friend's goods arrive in minutes at 1.3. A House's arrive over 12–36 hours at 2.0. **A friend saves time and
  35%, and gold never replaces them instantly.**
- **"Pay the rest for Ana ▸"** is allowed. The gold goes to the House, which means it leaves the game. It never passes
  between players, and is blocked between same-device accounts (social).

### 3. The Capital grows: the District Wonders become an endless row of civic lots

The District Wonders exist already (`wonders.yaml`: Clock Tower, Grand Fountain, Observatory, Lighthouse, with three
stages and a district perk), and nobody in three runs reached one. I turn them into **the Capital's lots**: the town in
the middle of the map, rising stage by stage, built by everyone in the district.

**Each stage has two parts:**
- **Gold:** sized at **one day of the summed measured income of the district's active players** when the stage starts.
  For run 3's Capital on day 21 that is Ana, Cleo, Dev, Mia, Jo and Mia-B at about 3,400 g/min, so about 4.9M. Anyone
  pays any part with "Fund ▸".
- **Goods:** 3 hours of the district's spare of the stage's two goods, bought on the pipes (part 2). Anyone can Send to
  it.

**The perks are aimed at the bottlenecks, for every seat in the district** (strangers and Houses' neighbours too):

| Lot | Perk |
| --- | --- |
| Pipe Works | Every grey connection carries +50% (stacks under the ×4 widening cap) |
| Market Hall | Every Hub Order +25% |
| Academy | Takes research points (below) |
| Clock Tower | One more daily order |
| Grand Fountain | +5% production |
| Bandstand | Cosmetic: music in the Capital, a festival line in "Today here" |

**It never ends:**
- When a lot stands, the next opens at 1.25× the size.
- Lots persist into the next chapter, so expansion never needs rebuilding.
- The Capital's skyline is the picture of everyone's spare gold, which serves "a fun idle game that looks like one".

**Credit is hours of your own income, not gold.** Mia's 2 hours count like Ana's 2 hours. Every funder's colour goes in
the lot's banners, and the plaque names them.

**Research points have a home here too.** Row 10 already gives Labs a rest when nothing is left to research, and capped
Refinements after an Era's tree. The **Academy** lot adds one more use: it takes points from anyone, and each 100k
points buys one district Refinement (+2% on one chain, at most +50% per chain). Cleo's 1.6M points would have bought 16
for her whole district. No points pass between players.

### 4. A branch next to a friend (the existing Franchise, moved earlier)

The Franchise already opens a second estate (built in M8).
- **Unlock:** Level 15, from Level 25 and Era 2.
- **Price:** 2 days of measured income.
- **Placement:** the picker offers a seat in a crew mate's station first (Ben's Capital SW), then the frontier.
- **What it is:** a seat, with that seat's dealt good and kit. You become your friend's neighbour.
- **No free transfer.** Your two estates trade like any two neighbours: a Cargo Depot doesn't join them. A branch adds a
  voice to a friend's Yard; it doesn't make you self-sufficient.

Run 3's Ana promised six visits and never found Ben (*"where do you live?"*, group, day 21). A branch on day 19, at her
Level 19, would have put her next door to him.

**From row 10, adopted:** Labs rest when nothing is left to research; capped repeatable Refinements; stage rewards
aimed at the bottleneck; and the return card's money line offers the biggest useful buy (a widening, a site, a lot or a
branch) or stays quiet.

## The three open questions

**1. Can gold buy at once?** It depends on what it buys:
- **Capacity:** yes, at once (a widening).
- **Construction:** yes, at once (a lot's gold part, a building's gold cost).
- **Goods:** never at once. A House's part flows over 12 hours, after a 24-hour friends' window, or right away with
  "Don't wait".
- The gold goes to the game, never to a player.

That keeps the player researcher's point (if gold buys goods at once, a friend's goods never matter: Ana held 39.8M)
and the social designer's (friends offered gold four times, so let them pay for a friend's site, to the hub).

**2. Do hub orders become buyers?** **Yes.**
- A buyer at your trading post: no Storage, no Deliver.
- Sized at 30 minutes of measured income, paying 2.0× base as today.
- In run 3 no order was delivered after day 14, and Ana's daily order hit "Your storages have no iron ore". That is
  the same wall as the bricks.

**3. The two-friend keystone?** **Now.**
- From the Water Tower on, use it where two *active* residents each hold one land input (bricks from stone and coal,
  copper ingots from copper and coal). Elsewhere, today's rule.
- Row 1's pass-over (now built) bounds its stalls to about 4 days.
- The evidence: water was the keystone in all four stations by day 16 and needed nobody. The run's only "together 4"
  was exactly this shape: Ana's stone in Cleo's Brickworks (cleo d14 #341–#368).
- The player researcher's worry is that run 4 would be "unreadable" if the goal changes too. Run 4 counts acts (a
  friend's goods in your build), so it can still read who brought what.

## Weeks 2–5, walked through

| Player | Run 3 | With this design |
| --- | --- | --- |
| **Cleo** (optimiser, 4.8M on day 14) | Her pit ran at 12–14% for 11 days. The Lab trap. 1.6M points with nothing to research | Day 14: *"Its pipe carries 100 of 882 · Widen ×4 · 126k"*, and her pit sells 400 a minute. Her Brickworks' bricks flow into Ana's Big Factory site and the Dynamo by the Yard: *"your bricks built Ana's Big Factory 💛"*. Her points go to the Academy (16 Refinements for the Capital). Day 21: she funds the Pipe Works, and her colour is on the skyline |
| **Ana** (organiser, 39.8M unspent) | *"Gold piles up … nothing to spend it on that helps my friends"* (run 1), the same in run 3 | Day 21: "You need 100 bricks" becomes a site, and Cleo's bricks arrive in minutes. Day 19: a branch next to Ben in Capital SW, and the crew finally lives together. Days 21–36: she funds the Market Hall (Hub Orders +25% for everyone) and widens her pump's pipe |
| **Dev** (late, 365k on day 10: *"no clue what to spend it on"*) | *"I feel late and useless"* (d21) | Day 10: his return card's money line is *"Widen your Solar Panel's pipe ×2 · 7k (1 hour of your income)"*. Day 21: the Dynamo buys his ingots by the Yard, with his name in its tracker. Later he funds a Capital lot with 2 hours of his income, counted like Cleo's 2 hours |
| **Ben** (skimmer, away days 3–36, 5.5M) | Lost in 33 seconds, never found | Day 36: one card, *"Ana opened a branch next to you · Capital SW"*. The friend who couldn't find him moved next door. The money line: *"Fund the next lot ▸"* or one widening, one tap |
| **Mia beside Jo** | Jo's factory bought a fifth of her output; 27M unspent; *"the gold feels empty"* (d15) | Her gold builds the Capital's Bandstand and Pipe Works, with her name on the plaque beside Ana's and Cleo's. Her widened pipe sells Jo more energy. The district's shared goal includes her, and no friend is required |

## Cost and risks

**Cost:**

| Part | Days |
| --- | --- |
| Widening | 2 |
| Row 7 (sites, the wonder and hub orders as buyers) | 8–10 (the analysts' estimate) |
| Lots on the District Wonders: gold part, endless chain, new perks, the Academy | 3, plus art for the lots' stages through `process.mjs` and `fit.mjs` |
| The branch rules | 1–2 |
| Row 10 | 3 |
| **All together** | **About 17–20 days. About 6–7 of them are new beyond the analysts' plan** |

**Risks:**

| Risk | Answer |
| --- | --- |
| **Widening undercuts the friend's copper** | It is capped at ×4. Past 400 a minute only a Copper Pipe helps, and that needs copper, a friend's or your own dealt good |
| **Pacing** | Income rises, so XP rises (XP follows earnings). The light bots must confirm Level 30 in week 4 with widenings bought |
| **A lot as a donation box** (the outsider's old warning) | Every lot carries a perk felt in g/min, a stage you can see rising, and names. Its goods come by pipe, so it is never a checkbox |
| **A whale funds a lot alone** | Fine. It is a sink, credit is in hours of your own income, and the goods part still needs many makers |
| **A branch dilutes the need for friends** | It sits in another station, holds one seat's good, and isn't joined by a Cargo Depot |
| **Words** | Widen, Fund and Branch arrive in week 2 or later, never on the first evening |

## How run 4 would show it worked

| Mark | Run 3 | Pass |
| --- | --- | --- |
| Share of days 14–36 earnings spent | 0–5% | ≥ 20% |
| Crew income, days 14 to 36 | +3% from day 21 | +30% |
| Widening | — | Every player shown one buys it within that session or the next |
| A Capital lot stands | — | By day 21, with 3+ funders, one of them Mia or Jo |
| A friend's goods in your build | none | By day 14 for every crew member with 3+ sessions (the analysts' headline) |
| Diaries saying "nothing to spend" | many | None after day 10 |
| Labs | Ate ingots with nothing to research | None does |
| Points | Piled up | Spent, in the Academy or on Refinements |
