# Round B · wild

All six of us read run 3 the same way. The wants exist, a wall stands in front of them, and gold has no door through it.
We all adopt rows 7 and 10. The spread is in three places:
- what gold may buy;
- what scales with income;
- how safe spending feels.

## 1. What I take from the others, and what I drop of my own

**I take:**

| From | What | Why |
| --- | --- | --- |
| **outsider, friends, economy, psychologist, lonely** | **The pipe tier is a want** ("Copper Pipe, 28 tiles: 280 g + 28 copper ingots · Start it ▸"), filled like any site | It opens Cleo's wall with a friend's goods, which is the point of dealt land. It costs almost nothing: 28 ingots are about 6 minutes of one Smelter |
| **psychologist** | **A one-sentence preview from the solver before every purchase, and Undo for an hour** with a full refund | Spending was punished: the Lab's Max took Cleo from 783 to 285 g/min (cleo d06 #180), and after that she bought only what she could check (d10 #288). Dev called a 329k upgrade "a trap" (dev d10). Without safety, unlocked walls still won't make players spend |
| **outsider** | **"What stops you"**: one growth line on the card that names the binding limit, its key and its price, then *"Max now pays ▸"* | It turns 1.15× levels back into the unlimited sink they are. Taking one Big Factory from level 25 to 50 costs about 73M (friends), so no new sink is needed for growth |
| **outsider** | The first Refinements raise the walls (*pipes +10%*, *trading +10%*) before *+2% to a chain* | A +2% output on a building already capped at its pipe is Dev's trap again |
| **friends** | **"Order the rest"**: paced, friends first, unused gold refunded, open to anyone in the big station. Also **Refinements for every resident** who runs that chain | Pacing protects the friend without a deadline nobody sees. Points become a gift with no points moving |
| **economy** | **Commission**: gold ships up to half of an Era wonder stage at 2.5×, credited 25%, with a Patron line | The one sink that scales with the server (about 1.7M for a run-3 stage), it is shared, and it pushes the next Era. Era 2 came on its deadline with the Dynamo at 0 |
| **all but lonely** | **The two-friend keystone now** | See question 3 |

**I drop of my own:**
- **Widen** (gold buys pipe capacity ×4). Once the pipe tier is a want, a Copper Pipe gives 500 a minute for 28 ingots
  and 280 g. Widening grey to 400 for 3 hours of income is strictly worse, and it would undercut the friend's copper.
- **My Capital lots,** as a separate system. They return later as the District Wonders on the economy's commission rule,
  the same engine as the Era wonder.
- **The branch next to a friend.** It moves to "later", where the lonely guardian also put it.

## 2. What I reject, and why

**Outsider: "gold never buys a want's materials".**
- It makes every want wait on someone's building, and the run's two players at the edge can't build that:
  - Ben, back on day 36, wrote *"I still can't help with the Era 2 water need"* (ben d36 diary);
  - Mia had no stone neighbour and wrote *"the gold feels empty"* (mia d15).
- For them "never" means never, as the lonely guardian says.
- The outsider's own fallback ("if wants sit unfilled for a week, allow a House fill") concedes the risk. Run 4 is our
  last look before the owner's next test, so we shouldn't ship a rule we already expect to relax.

**Lonely: "Buy now, in the next Yard round".**
- Instant goods make a friend's bricks pointless against banks of 25–33 days of income: Ana held 39.8M for 100 bricks
  worth 1,250 g at 2.5×.
- The lonely player loses nothing to a paced order. Sessions came 2–7 days apart (economy), so a 6-hour Order is done
  long before she returns.

**Lonely: "While I'm away", an opt-in budget that spends an absent player's gold.**
- It creates gifts nobody chose. The psychologist's own evidence is that a gift believed to be chosen is the one that's
  felt.
- The player it's for wouldn't set it: Ben, skimming on a phone, was lost in 33 seconds (ben d01 #29).
- His absent estate already gives: his resting producers run at 100% and fill wants through the Yard by themselves. On
  return, "Order the rest for Ana ▸" lets him give by his own tap.

**Psychologist: the House only at the deadline,** and **economy: the hub only after a 24–72 h window.**
- Both are right about protecting friends, but a deadline is invisible. Players return every 2–7 days, so a 12–24 h fill
  always "just happened".
- An Order the player taps is visible, priced, refundable, and still slower than a friend: 6 hours against minutes. I
  keep the deadline as the no-tap backstop (below), not as the only door.

**Station levels (friends) and the Landmark's tiers (psychologist), now.**
- Right idea, and I'd sign them, but later. They need new art and a third goal system on top of the project and the
  wonder.
- The Era wonder with Commission is the shared, scaling sink that already exists, and it changes the game: the next Era
  sooner.

## 3. The three open questions, final

**1. Can gold buy a want's materials?** Yes: after a wait you choose, never at once, never between players.

The fill order for any want:
1. your own producers;
2. a friend's Send, or one tap from a friend's Storage;
3. the station's spare through the Yard, by itself, at the one price, named at both ends;
4. **"Order the rest ▸"** (any time, by anyone in the big station): the hub ships evenly over **6 hours** at **2.5 ×
   base**. Friends' goods arriving meanwhile come first, the unused gold is refunded, and the gold leaves the game.
   Same-device accounts can't order for each other;
5. **with no tap at all:** at the want's **24-hour deadline** a House fills the rest at **2.0 × base**
   (`AskHousePrice`, `GameWorld.Asks.cs` 33).

**Two rules on bought goods:**
- They go into the build, never a Storage, so nothing can be resold.
- They count 25% for shared credit and never carry a 💛.

**What a friend gives you:**

| | Gold (an Order or the House) | A friend |
| --- | --- | --- |
| Price | 2.0–2.5 × base | 1.3 |
| Time | 6–24 hours | Minutes |
| What you see | — | A name, a Thanks and full credit |

**2. Do hub orders become buyers?** **Yes, in the same step.**
- A buyer at your Trading Post, filled from production as it passes. No Storage, no Deliver.
- Sized at 30 minutes of measured income, paying 2.0 × base.
- It shows on the card only once filled (*"Your coal filled today's hub order"*), never as a task.
- It stays personal: no Send to someone else's order.

**3. A two-friend keystone?** **Now, where two active residents hold its land inputs.**
- Bricks from stone and coal, or copper ingots from copper and coal. Today's rule applies elsewhere. An away seat's input
  comes from the House backstop, and the 2-day pass-over is built.
- **The evidence:** water was the keystone in all four stations and needed nobody. The run's only "together 4" had
  exactly this shape (cleo d14 #341–#368).
- **The lonely guardian's worry** (a station of Houses gets nothing from it) is answered by the rule itself: no pair, no
  change.

## 4. The solution I'd sign, in build order

| # | Mechanism | Numbers | Replaces | Days |
| --- | --- | --- | --- | --- |
| 1 | **Safe spending** | A solver preview in one sentence before every purchase (*"+105 g/min"*, *"+0: this pipe carries 100 · Copper Pipe ▸"*, *"−498 g/min"*). Undo with a full refund for 1 hour. Max stops at the pipe's limit | The silent Lab trap; Max past the pipe | 2 |
| 2 | **Wants on the pipes** (row 7): buildings, upgrades and **pipe tiers** that need materials stand as scaffolds that buy, in the fill order above | 24 h deadline; Order over 6 h at 2.5×; House at 2.0×; credit 25% for bought goods | `Pay()`'s Storage pull, "N in your storages", Deliver, the Plan board's Ask form | 6–8 |
| 3 | **"What stops you"**: one growth line on the return card and the producer's panel naming the binding limit, its key and its price, then *"Max now pays: +X g/min for Y ▸"*, priced in hours of income | One line, the solver's binding limit only | The Invest line's *"… for 13k"* against 27.8M (ana d27 #317) | 2 |
| 4 | **Hub orders as buyers** | 30 min of measured income, 2.0× | The Storage chore behind orders | 1 |
| 5 | **The Era wonder as the Capital's buyer, with Commission** | Each stage sized at its start from 6 h of the Capital's summed income (about 1.38M of goods in run 3's day 15). Commission ≤ half a stage at 2.5× (about 1.7M of gold), credited 25%, with a Patron line. The last stage starts the next Era early. Every card tells the result | The Wonders minigame; the 1,800-brick stage sized at world start | 3–4 |
| 6 | **The two-friend keystone** | Where two active residents hold the inputs | The water keystone | 2 |
| 7 | **Points** (row 10) | Labs rest. Refinements cost 5k × 1.6ⁿ points, capped at +50% a chain. Wall lines first (pipes +10%, trading +10%, Hub Order +5%), then +2% chains, for every resident who runs the chain | 1.6M points with nothing to buy | 3 |
| **Later** | The station Landmark's public-works tiers (2ⁿ⁻¹ days of station income in gold, plus materials); the District Wonders on the commission rule; a branch next to a friend from Level 15 | | | |

That is **about 19–22 days now**, mostly the analysts' rows 7 and 10. Steps 1 and 3 are what make gold *feel* spendable
again, and they are cheap.

## 5. One mark for run 4 that would prove it wrong

> **From day 14 to day 36, every crew member spends at least 20% of what they earn, and at most a third of it goes on
> Orders and Commission.**

In run 3 they spent 0–5%. If players still hoard, the doors and the safety weren't enough. If they spend mostly on
Orders, gold has bought the friend out of the game. Either result means this solution is wrong.
