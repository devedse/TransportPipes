# Round B, after run 4: the friends-first seat

## 1. What I take from the others, and what I drop of my own

**From the economy designer: the pipe door has to go the whole way, and the advisor has to stop going quiet.** I had
the door on the panel. They found why Cleo never saw it, even in Next steps:
- the pipe step asks `SupplyStep` for ingots, and `SupplyStep` returns nothing when you already make them
  (`GameWorld.Advisor.cs:548`), so Cleo's 237k ingots hid her own door;
- one button should go the whole way, *"Copper Pipe, then Max to 30 · 165k"*, with one preview.

Their bot split convinces me the wall is real for some seats. The copper-ore seats banked 2.7, 3.2 and 5.1 days; Ana's,
Dev's and Ben's banked 18.9, 16.8 and 10.8 (`light-bots-day28.md`). So ingots must be orderable where nobody nearby
makes them. I also take:
- their definition of "near": a resident seen in 7 days whose buildings made the good in the last 24 h;
- "the cap snaps back to half once a friend starts making it";
- the 7-day `ActivePeriod` that already exists (`GameWorld.Research.cs:8`).

**From the psychologist: rings, and "nobody more than half until it has waited 3 days".** This is my chip-in done
better. Both of us name givers in hours of their own income, so Ben's hour stands next to Cleo's day. The half rule
adds what my "press it again" lacked: a ring needs two givers. Ana invented that rule herself (*"Can you each order the
other half"*, group, day 27 20:03). The 3 days protect Mia, whose only neighbour never gives. I also take the 💛 and
full credit, counted in hours, for gold spent on a friend: *"Dev's gold finished your Big Factory 💛"*. Five gold offers
in the chat show that players spend gold to be a good friend.

**From the outsider: Widen on grey, next to the friend's copper and below it.** In run 3 I argued grey should stay
closed so a friend's copper is the first door. Run 4 broke that argument. The copper was never scarce: Cleo sold all
237k ingots. The door was never shown. Grey ×4 carries 400, and one Copper Pipe carries 500 for an ingot and 10 g a tile
(`pipes.yaml:2–3`), so the friend's door stays the better deal. From the outsider I also take *"14 days of income"* on
the top bar and a growth line on the card that never folds.

**From the lonely seat:** opening a lone player's chat on the district channel, and naming gifts at both ends.

**What I drop of my own:**
- My tier purse of 24 h × 1.2. My own walk-through reached about 10% and admitted it.
- The two-friend keystone in tiers. It fired once in 36 days, unseen.
- Parcels priced in hours. The economy designer is right that the same land at 279k isn't a reason to grow. The
  psychologist is right that a price that differs between friends reads as unfair.
- Chip-ins with no per-person limit.

## 2. What I reject, and why

**Wild's Works, station-owned machines that make the missing goods.** They take the role my seat most wanted to give.
Dev's reason to come back on day 21 was *"I want to build the Brickworks"* (dev d21, "Would I come back"). His week-old
dead end was *"Bricks need stone + coal, I have neither"* (dev d14 #116–#117). A line saying the Yard brings both,
offered to the friend with gold and no role, gives him that part for 1,400 g (`buildings.yaml:276–284`). A station
Brickworks would make the friend with gold and no role unnecessary. It is also a new noun, a new kind of owner, and the
most expensive part on the table (5–7 days).

**A 2.5× price (wild).** 2.0× is built (`GameWorld.WantOrders.cs:16`). Two prices means two numbers to learn for the
same bricks.

**Lonely's estate-wide "Grow everything that pays" in run 5.**
- Ben's trouble on day 36 wasn't the number of taps. It was two lines that contradicted each other: *"+41"* and then
  *"would make nothing more"* (ben d36 #80–#82). A plan that sums many buys adds numbers rather than trust.
- Cleo trusted previews one at a time (ten, exact).
- A bundle would also hide which door worked, and run 5 has to tell us that.

One ranked buy at a time, going the whole way on its building, is enough. The plan can follow if Ben's return still
fails.

**Lonely's doubling tiers (1, 2, 4, 8 days) and a 1-day Era stage.** At a day of the Capital's income (≈ 9M), the pipe
half is ≈ 4.5M of goods. In bricks (5 g, `resources.yaml:13`) that is 900k bricks: 62 days of a level-1 Brickworks at
10 a minute. The pipe half could never fill, and we'd be back to gold alone. Doubling also makes ring 4 eight days of
income.

**The psychologist's "Hub Orders +10%" ring perk.** That is the faucet that already beat everything Cleo spent (*"+306,822
g over the hub's"* in one week, cleo d21 #354). Every ring should lift the pipe, which feeds the levels.

**Lonely's branch next to a crew: not in run 5.** The chat fix reaches the cause more cheaply. Mia's first answer came
from Mia-B in the district channel (mia d15 #151–#155), after two weeks of greetings into a channel holding only Jo
(director, row 1). The branch can come later, by trigger: Mia's "alive" stays ≤ 2 with the chat fix in.

## 3. The open questions, final

| Question | My answer |
| --- | --- |
| After the Landmark | **Rings**. Ring n costs n days of the station's 7-day income: linear, never ending, with a goods part by pipe. Not the Works, and not doubling |
| Gold in a shared goal | **Give**: 1 h, 6 h or 1 day of your own income. Nobody gives more than half until the ring has waited 3 days. The plaque shows hours, for goods and gold alike |
| Price and credit | **2.0× base**. Ordered goods count 25% in the wonder's Patron line, so pipes count four times as much. Gold that fills a *friend's* want earns a 💛 to the friend and the giver's hours on their card |
| Widen | **Grey too**: ×2 for 1 h, ×4 for 2 more hours of the buyer's income. It sits next to the Copper Pipe button and below it |
| One door or one plan | **One ranked buy that goes the whole way** (pipe, then Max), three of them on the top bar's list |
| The player alone | **In:** the chat on the district channel, and gifts named at both ends. **Out:** the branch, by trigger |
| Before run 5 | **Both sims.** (1) Light bots with ingots orderable: stop and resize the rings if the bots still bank > 5 days on day 28. (2) Level 30's day with every full pipe widened: if it moves before day 23 (target 25 ± 2), grey Widen stops at ×2 |
| The mark | See part 5 |

## 4. The solution I'd sign

| # | Mechanism and numbers | Replaces | Days |
| --- | --- | --- | --- |
| 1 | **Sizing.** Count residents seen in the last 7 days (`ActivePeriod`). Size a stage when it opens, resize it upward daily until a quarter has arrived, and never go below the stage before. Size the first Era stage when the Capital has 3 active players, not at world start. Era stage: 6 h of the Capital's income (≈ 1.6M for the Cathedral) | The 3-day `IsActive` and the 60 g/min floor (`GameWorld.Wonders.cs:139–142`); the freeze at the first delivery (`GameWorld.EraWant.cs:230`) | ½ |
| 2 | **The pipe door, the whole way.** At the pipe: *"Its pipe carries 205 a minute · [Copper Pipe, then Max to 30 · 165k · +N g/min] · Cleo makes the ingots"*. Ingots are orderable at 2.0× when nobody nearby makes them. Below that, *[Widen ×2 · 1 h of your income]*, grey included. The advisor's dead ends become the want or the research step (`GameWorld.Advisor.cs:503, 515–523, 548`) | *"+N would make nothing more"*; Widen's grey refusal | 2½ |
| 3 | **One order rule.** Nobody nearby makes the good: order all of it at once at 2.0×. A maker nearby: their window (built), then gold, and half again once they start. The button never vanishes and says why. A chain line is offered first to the friend with gold and no role. Waiting buildings read *"waits for 100 bricks · Order ▸"*. The Exchange says when it caps a bid. Ordering for a friend earns a 💛 | The half-cap where no maker exists; *"is being built"*; the vanished button; Order folded on cards | 2 |
| 4 | **Rings.** Ring n = n days of the station's 7-day income, fixed when it opens. Capital SE on day 17: about 6.1M, then 12.7M, then 22.8M (psychologist's sums). The goods part is 6 h of the station's spare of what playing residents make, brought by the Yard. Give 1 h, 6 h or 1 day; half per person for 3 days; a plaque in hours. Each ring adds something visible and makes residents' pipes carry +10%, named | The This week line's 10k parcel (ana d36 #375) | 4 + art |
| 5 | **Doors where people look.** Top bar: *"40.6M · 24 days ▸"*, which opens *Your gold can*: at most three lines, shared first (the open ring or stage with your part so far, a friend's waiting building), then the best private buy. The card's growth line never folds. Next steps show the same list | The bare gold number; folded growth lines | 1½ |
| 6 | **The player alone.** Her chat opens on the district channel when no other human in her station has chatted. Gifts are named at both ends (*"mia_pipes' wood finished your Water Tower 💛"*) | The station-only first chat; unnamed gifts | 1 |
| | **Before run 5:** the two sims in part 3 | | ½ |
| | **Total** | | **≈ 12 + art** |

**What run 4's players would have done.**
- **Cleo** (day 10) presses Copper-then-Max with her own ingots.
- **Dev** (day 14) builds the Brickworks from the chain line, and the Dynamo's bricks are his.
- **Ana** (day 21) sees four names on ring 1. On day 27 she orders the Cathedral's steel, which nobody makes, all of
  it, and the Cathedral stands.
- **Ben** (day 36) gets two buttons on his card: ring 3 *"Give a day (570k)"* and his Moily's *"Widen ×2"*.
- **Mia's** hello reaches the Capital on day 1. From day 28 her ring is a day of her and Jo's income (≈ 1.8M); she
  gives half and finishes it after 3 days.

## 5. One mark for run 5

**From day 14 to day 36, the crew spends at least 20% of what it earns, and at least three of the four crew give gold
to a ring, the Era wonder or a friend's waiting building.**

How it fails:
- **Under 10%** with the doors in place: the solution is wrong.
- **Over 20% but from one or two people:** the pipe door did the work and the shared part did not. The rings would
  then be a cost the next design should drop.
