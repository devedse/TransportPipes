# Round B · outsider

All six papers agree on the core: the wall is real, row 7 ("every want is a buyer on the pipes") is the door, and
row 10 (resting Labs, Refinements) gives points a use. We differ on three things: how much gold may buy, how many
new shared sinks to add, and the keystone. The others moved me on all three.

## 1 · What I take from the others, and what I drop of my own

**I take:**
- **Wild's Widen.** A link at its limit gets one button: *"Widen ×2 · 1 hour of your income"*, up to ×4. It is the
  sink I said we didn't need, and economy shows why we do.
- **Economy's point that open doors still leave gold cheap.**
  - Once the walls open, the Big Factory still costs 12k against 1.37M a day.
  - The advisor bot spends on every step it can, and still banks 107M by day 28 (`balance-typical-advisor.md`).
  - So at least one want must be **priced in hours of income**. Widen is one. The Era wonder, sized at 6 h of the
    Capital's income (`GameWorld.Wonders.cs:116–126`), is the other.
- **Economy's window.** A want waits for friends until every active crew mate or neighbour who makes the good has had
  one session, then the hub ships the rest at 2.5×. Of all the timing rules, this is the only one that respects 2–7 days
  between sessions.
- **The psychologist's "spending is safe".** It names the cause I missed: Cleo spent on day 6 and lost (the Lab's Max,
  783 → 285 g/min, cleo d06 #178–#180), then bought only what she could check first (d10 #288). Dev called a 329k
  upgrade *"a trap"* (dev d10). Taken from that paper:
  - one sentence from the solver before every buy;
  - an hour's Undo;
  - prices shown in hours of income.

  My "what stops you" line is the same engine, so the two merge.
- **Lonely's case against "never".** For Mia beside a silent Jo, "never at once" means a 500-brick Market Hall waits
  for a friend she doesn't have.
- **Friends' station Refinements.** Cleo's points help every resident who runs that chain, named on their panel
  (*"Bricks +6% · Cleo's research 💛"*). Points finally do something for someone.

**I drop of my own:**
- **"Gold never buys a want's materials."** It fails Mia and Ben, and economy's window protects the friends better than
  "never" does.
- **"Cancel · your gold back."** With the window ending in a fill, no want can freeze.
- **"No new sink is needed."** Open doors alone leave the bank at 28 days of income. One scaling want is needed, and
  Widen is a second.

**I keep:**
- one word on screen for every buyer: **wants** (*"Big Factory · wants 100 bricks"*);
- one growth line on the return card, *"what stops you"*, with the key;
- Refinements that **raise walls first** (pipes, the Hub Order) before +2% to a chain, so points never buy output a pipe
  can't carry.

## 2 · What I reject, and why

**1. Four new shared sinks at once:**
- wild's endless Capital lots (six of them, including an Academy and a Bandstand);
- wild's earlier Branch;
- the psychologist's Landmark tiers;
- economy's Commission with a Patron line.

Each is reasonable alone. Together they put lot, Fund, Branch, Patron, Commission, Academy and tier on the screens of
players who in run 3 already wrote *"I can't tell what matters"* (ana d36) and read four Water Towers in one chat (mia
d02 #65). **Pick one shared scaling want: the Era wonder.**
- It exists, and its later stages are already sized by income.
- It is the only sink that changes the world for everyone: its last stage opens the next Era early (economy).
- Wild's best idea, perks aimed at the bottlenecks (grey pipes +50%, Hub Orders +25%), becomes its tier rewards.

Lots and Landmark tiers come later if run 4's banks still pile up.

**2. Lonely's "While I'm away" budget.**
- **It spends an absent player's gold by a rule he set once and never sees.** Ben, the skimmer it is for, acts on what
  a card offers him at that moment: in run 3 his one gift was a Send from a card (ben d02-0800 #70). A switch set on day
  2 and never seen again is the opposite.
- **Gifts nobody chose aren't felt as gifts.** That is the psychologist's own Gray (2012) point.
- **Ben's return line can come from his goods instead.** His resting estate's sand fills wants through the Yard by
  itself (`MatchAsks` already does this while away), so day 36 can read *"Your sand went into Ana's Market Hall"* with no
  autopilot.

**3. Wild's Branch at Level 15.**
- It answers "find your friends" by giving Ana a second estate to run. Row 4 already fixes the cause: Ben pressed Play 33
  seconds before the link (ben d01 #29).
- A skimmer can barely tend one estate.

Later, if at all.

**4. Friends' Order paced over 6 hours, at once for anyone.** Players come back every 2–7 days, so a 6-hour pace lands
before the friend's next session every time, and the friends' goods matter only by name. Economy's window replaces it.

**5. A daily job for tending** (lonely's earlier paper, echoed here as funding duties). A job that matters every day
becomes *"I should"*, the guilt the redesign removed.

## 3 · The three open questions, final

**1. Can gold buy a want's materials? Yes, after the friends' window, at 2.5×, into the build only.**
- **The window.**
  - It lasts until every active crew mate or neighbour who makes that good has had one session since the want was
    raised, at most 72 hours.
  - If nobody near makes it (Mia, often), there is no window.
  - During it, the station's spare fills the want by itself, even while friends sleep.
- **After it, "Pay the rest ▸".**
  - The hub ships the rest at its sell price, 2.5 × base, within one Yard round.
  - Anyone in the big station may pay for anyone's want. The gold leaves the game, and same-device accounts are blocked.
- **The goods.**
  - They go into the build, never a Storage, so nothing can be resold.
  - Bought goods count 25% toward shared credit and carry no 💛. A friend's goods are named, thanked and credited in
    full.
- **Why not instantly:** 100 bricks at 2.5× cost 1,250 g against Ana's 39.8M, so price protects nothing. Only order and
  time protect the friend, and the window measures time in sessions, not hours.

**2. Do hub daily orders become buyers? Yes, in the same step as wants.**
- An order buys from what your Trading Post sells. No Storage, no Deliver.
- It is sized at 30 minutes of measured income and pays 2.0 × base.
- It shows on the return card only when it has filled (*"Your coal filled today's hub order"*).
- Run 3 delivered none after day 14, for the same Storage reason as the bricks (run 1's *"Your storages have no iron
  ore"* STUCK). One rule, one engine.

**3. The two-friend keystone: now, with lonely's guard.**
- **Where:** from the Water Tower on, where two residents who are both active when the stage starts hold its inputs:
  bricks (stone + coal) or copper ingots (copper + coal). An away seat's input comes from its House. Elsewhere, today's
  rule applies.
- **Why I move from "only through the wants":**
  - the pass-over is built, so a stall is bounded at about 4 days;
  - water, the keystone in all four stations, needed nobody;
  - a brick keystone feeds the same Brickworks the wants and the wonder use, so the crew learns one chain, not three.
- **The readability worry:** run 4 counts acts (a friend's goods in your build), so it can still tell who did what.

## 4 · The solution I'd sign, in build order

| # | Mechanism | Numbers | Replaces | Days |
| --- | --- | --- | --- | --- |
| 1 | **Spending is safe, and "what stops you".** One sentence from the solver before every buy. Each producer panel and the return card's growth line name the binding limit and its key. An hour's Undo | Sentences like *"+105 g/min"*, *"+0: this pipe carries 100"*, *"−498 g/min: the Lab eats 29 ingots"*. Prices in hours of income (*"Market Hall · 50 min of your income + 500 bricks"*). Undo refunds in full and takes back the XP | Silent Max; idle reasons that blame energy; the Invest line's *"Max … for 13k"* against 27.8M | 2–3 (inside row 3) |
| 2 | **Widen.** A link at its limit doubles its capacity for gold | ×2 for 1 h of measured income, ×4 for 2 more hours; the cap is ×4 per tier. Past ×4, the next tier is a want. Cleo, day 21: ×4 for about 126k, selling 400 a minute instead of 100 | The dead Copper Pipe click as the only way past 100 a minute | 2 |
| 3 | **Wants (row 7).** A building, upgrade, pipe tier or hub order that needs materials stands as a scaffold and fills by pipe | The order: your producers → a friend's Send or one tap from their Storage (named, Thanks) → the station's spare at 1.3 → after the window, *"Pay the rest"* at 2.5. Houses sell land goods at 1.4 for your own chain. One growth line per card | `Pay()` from your own Storage only; Deliver; "N in your storages"; the Plan board's Ask form; the Storage chore behind orders | 8 |
| 4 | **The Era wonder, the Capital's scaling want** | Each stage is sized at 6 h of the Capital's summed measured income when it starts. Half by pipe (players' goods; Houses at most 25%), half open to *"Pay the rest"* by anyone. Tiers pay bottleneck perks to every seat: grey pipes +50%, Hub Orders +25%. The last stage starts the next Era early | The Dynamo's 1,800 bricks sized once at world start; Deliver; the Wonders minigame; an Era that ends without a word | 3 |
| 5 | **Points.** Labs rest when nothing can be researched. Refinements raise walls first, then chains, for the whole station, named | Pipes +10% and Hub Order +5% before +2% a chain; 5k × 1.6ⁿ points; capped at +50% a line | A Lab eating 400 g/min of ingots for 1.6M unusable points | 3 |
| 6 | **The two-friend keystone**, under the guard above | 3 h of one level-1 maker | Water as the keystone everywhere | 2 |

That is **about 20 days**, most of it rows 3, 7 and 10, which are coming anyway.

**On screen,** after the first evening's budget: **wants**, **what stops you**, **Widen**, **Pay the rest**. These
are two phrases and two verbs, all in plain English, and none appears before week 2.

## 5 · One mark for run 4 that would prove it wrong

**From day 14 to day 36: if the crew spends 20% or more of what it earns, but its income grows less than 15%, this
solution is wrong.** That would mean we opened doors into rooms that don't pay, and the loop *earn → spend → earn
faster* is still broken.

Run 3 spent 0–5% and grew about 3% from day 21 to day 36. Spending without growth would be worse than both.
