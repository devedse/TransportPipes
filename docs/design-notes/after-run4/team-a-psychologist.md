# Round A · psychologist (after run 4)

## 1 · Diagnosis: the wish to spend was there, and we fenced it off

**The nine steps opened walls in front of private growth. But late in the run players didn't want private growth: they
wanted to help each other, and gold was how they tried.** We designed gold as a threat to friendship (it comes last, at
most half, a quarter of the credit, no 💛). The players used it to be a good friend, and every door they tried was
capped, hidden or tiny.

| Door | In sight? | Shared? | What happened |
| --- | --- | --- | --- |
| Era wonder order (Hub ▸ Wonders) | Yes | Yes | **Used to its limit by two players, unasked:** Cleo 9,000 g (d06-1400 #148), Ana 6,660 + 15,000 g (d27-2000 #314, #316). Then the button vanished (`WonderOrderOffer` returns null, `GameWorld.EraWant.cs`:190–199) |
| Exchange bids for shared goals | Yes | Yes | Cleo built an Exchange Office to pay *more* for stone and bricks; the game cut her price without a word (3 g → 1.95, cleo d10 #296; 7.50 → 6.50, d14 #328) |
| The chat | Yes | Yes | Five gold offers (Cleo 4.2M, 15M; Dev 600k, 3.6M, *"12M gold sitting around, tell me how to be useful :)"*); Ana: *"Can you each order the other half?"* (day 27); Ben: *"just tell me the button"* (d36) |
| Upgrade panel at the pipe | Yes | No | *"+30 would make nothing more"* (cleo d06 #220; `BuildingPanel.tsx`:856): true, with no door after it. The pipe tier the solver had already worked out (`GameWorld.IdleTexts.cs`:145–167) shows only in a full building's status. Cleo made 237k copper ingots; all 1,087 pipe tiles stayed grey |
| Order the rest for a friend | No (folded) | Yes | Offered once at 1,000 g, never seen (ana d27 #308) |
| Wants (scaffolds) | No | No | *"… is being built"* (`HoverTip.tsx`:59), then nothing for good: Dev's Big Factory, Ana's Cement Kiln |

Three of the four crew went to the wonder's order button, and the fourth offered his gold in the chat. **The only sink
that took real gold was the one that was in sight and shared**: all 30,660 g of the nine steps' sinks.

**Four reasons it stopped there:**
1. **The shared door was capped, then went quiet.** Half of each good came "only by pipe" (`EraWant.cs`:210), and that
   sentence never reached a screen. Ana took it for a cap on each person, and Ben found *"only bars"*. A goal that no
   button can move is a goal you give up on: *"the Cathedral cannot finish
   without the other half and I can't make it happen"* (ana d36).
2. **Growth came free, so spending wasn't why you grew.** Crew income rose 39.7% from day 14 to 36 on stage perks,
   Refinements and hub orders. One week of Cleo's hub-order bonuses (*"+306,822 g over the hub's"*, cleo d21 #354) beat
   all she spent in 36 days. When the reward comes whatever you do, the habit of buying dies out. It ran strong while
   it paid: Cleo spent 227k of her 297k by day 7.9.
3. **Buys that failed taught players not to buy.** Dev paid 12k for a Big Factory the preview said would earn
   *"+235 g/min"*. It never stood, and he left unsure *"whether it works without pipes"*. Ben, with 13.4M: *"I still
   don't know if upgrading earns anything"* (d36). In run 3 spending hurt; in run 4 it did nothing. Both teach: keep it.
4. **Nothing cost enough to be a choice.** Prices were 3–60k against 1.4–4M a day of income. Both Era stages were sized
   at the floor, 6 h × 60 g/min ≈ 21.6k g of goods (`GameWorld.Wonders.cs`:139–142, counting only residents seen
   lately). A gift means something when it costs the giver something.

**Underneath: after the Landmark (day 17) the station had nothing to build together.** Every "yes, I'll come back" from
day 8 named a friend or a shared goal (*"my friends need my wood"*, dev d08; *"the group needs my stone"*, ana d14;
*"Cleo has a clear need"*, dev d21). The "maybe"s are what was left without one: *"income without a sink is pointless"*
(cleo d21), *"I feel I'm the only one pushing"* (ana d27), *"the thing my friends need from me is hidden"* (ben d36).
Days 18–36: fun 2.83, "come back" yes 2 of 6.

**My own part.** After run 3 I argued for "never at once", the half cap, a quarter of the credit and no heart for gold,
to protect a friend's goods from a big bank. Nobody near made bricks, steel or concrete, so there was no friend's goods
to protect, and what we blocked was the friends' own wish. **So: too capped, too hidden and too small for the shared
door; the right idea for private growth, on the wrong panel.**

## 2 · Proposal: gold is how you help, and the station always has a next thing to build

### 2.1 The Landmark grows in rings

When a station's Landmark stands, its **first ring** opens: an addition to the Landmark and the Yard (banners, an
arcade, a clock, a garden; four looks, then ornaments that repeat). Then the next, for as long as the station lives.

- **Gold:** ring *n* costs **n days of the station's measured income** (residents' 7-day average, fixed when the ring
  opens). Capital SE on day 17 (Ana 663, Ben 297, Cleo 2,460, Dev 806 g/min): ring 1 ≈ **6.1M**; ring 2 (day ~21)
  ≈ 12.7M; ring 3 (day ~28) ≈ 22.8M.
- **Goods:** 6 h of the station's spare in two goods that a **resident who plays makes**, brought by the Yard by itself.
  Never a good nobody near makes.
- **Give ▸** on the card and the ring's panel: 1 hour, 6 hours or a day of *your* income.
- **Two people at least:** nobody gives more than half of a ring's gold until it has waited 3 days; then anyone may
  finish it. Ana invented this rule herself (day 27), and the 3 days are for Mia beside a Jo who never gives.
- **Credit in hours of your own income**, on the plaque and in banners of each giver's colour: *"Ring 1 · Cleo 20 h ·
  Dev 8 h · Ana 6 h · Ben 6 h · stone: Ana · wood: Dev, mia_pipes"*. Equal sacrifice reads as equal, so one rich player
  can't make the rest feel useless.
- **It gives** the picture and a station perk aimed at the commonest binding limit there (ring 1: grey pipes +25%;
  ring 2: Trading Posts +25%; ring 3: Hub Orders +10%; then +2% production, capped at +10%).
- **Replaces** the week line after the Landmark: *"A new parcel: Level 7 · 10.0k g"* (ana d36 #375).

Rings 1–3 come to about 41.6M, a quarter of the 160M the crew earned from day 14 to 36. Animal Crossing's public works
are the model: people fund a bridge because it stands where they live, with their name on it.

### 2.2 Gold finishes what no friend near makes

- **No resident who plays makes the good** (the station for a want, the Capital for the wonder): **Order ▸ fills it at
  once**, in the next Yard round, at 2.0 × base, one unit price for everyone.
- **Someone near makes it:** their goods first, then the built window (their next session, at most 72 h), then the
  6-hour flow.
- **The half-of-each-good cap goes**, replaced by 2.1's rule: half per person until the stage has waited 3 days.
- **Gold for someone else is a gift, with a 💛** and full credit in hours: *"Dev's gold finished your Big Factory 💛"*.
- **A button never vanishes:** when an order can't go in, the line says why and who could make the rest.

### 2.3 Doors where the player looks

| Place | Today, rich and at the pipe | Proposed |
| --- | --- | --- |
| Upgrade panel | *"+30 would make nothing more"* | *"Its pipe carries 205 a minute. A Copper Pipe here: 14 ingots (you hold them) + 140 g · then levels 22–30 pay +X g/min · Lay it and Max ▸"*, one tap. Without ingots: the want, with Order ▸ |
| Top bar | The gold number | Tap it: **"Your gold can"**, three lines at most, shared first: the open ring or stage (your part so far), a friend's waiting want, the best private buy |
| Next steps | *"+235 g/min"* for a building that can't stand | *"+235 g/min once its 100 bricks are in · nobody near makes them · Order: 1,000 g"* |
| Return card | Order folded under "N more" | One line after people: *"Ring 2 · 8.1M of 12.7M · your part 6 h · Give ▸"* |
| Scaffold | *"is being built"* | *"waits for 100 bricks · Order ▸"* on the ghost and the toast |
| Exchange | A silent price cap | *"Capped at the one player price, 6.50"* before the click |

**The rule:** wherever a true "+0" or "waits" appears, the next door is on the same line.

### 2.4 The chain as a role

Where nobody near makes a wanted good, the line adds *"A Brickworks on your estate makes bricks from Ana's stone and
Cleo's coal; the Yard brings both · 1,400 g · Show me"* (the analyst's recommendation 3), offered **first to the friend
with gold and no role**. Dev read *"Bricks need stone + coal, I have neither"* (dev d14 #116–#117) and wrote *"I still
don't know what I could do that matters next to Cleo"*. An order finishes today's need; being the maker is the friend's
lasting part.

## 3 · The five questions

**1 · Gold and what no friend near makes: yes, at once, at 2.0 × base.** Where someone near makes it, they go first,
then the window, then the flow. Left for friends: being the maker (cheaper, ongoing, named on every delivery); gold for
a friend counts as a friend's gift; every shared stage needs two people.

**2 · Sizing.** A shared goal is sized by its group: a ring by the station's income, the wonder by the Capital's. Both
use the 7-day measured income of residents seen in 7 days, fixed when the stage opens, never shrinking; the first Era
stage is sized when the Capital first has three active players, not at world start (the bug, `Wonders.cs`:139–142).
Rings rise linearly in days (1, 2, 3, …) and with income. **Unit prices stay the same for everyone**: players compare
prices in the chat, and a brick that costs Ana more than Ben reads as unfair. Gifts are *shown* in hours of the giver's
income. Widen stays in hours of the buyer's income: capacity is private.

**3 · The doors:** the table in 2.3.

**4 · After the Landmark:** rings, one at a time, without end; each takes a day to a week, as the station gives.

**5 · The bots.** The advisor's bot acts on every offer that pays. It needs no door in sight, no reason and no trust
after a failed buy, and it grows one deep chain whose every level has a taker (Cleo's bot: Iron Mine level 69, nine
factories level 50, 2.7 days banked). Even so, Ana's bot banked 18.9 days and Dev's 16.8 when their private doors ran
out. The bot mark measures what the economy can absorb, not why a person would spend: rich people didn't spend on
returns, they spent on the crew. Keep the bot mark; judge the design on the human one.

**The analyst's recommendations:** adopt 1 with 2.2's changes; adopt 2's sizing and Landmark tiers (as rings), but not
goods priced by the buyer's income; adopt 3–11.

## 4 · Cost and risks

| Part | Days |
| --- | --- |
| Rings: sizing, Give, the cap per person, plaque and hours, perks | 4–5, plus art for four looks (`process.mjs`, `fit.mjs`) |
| Order at once without a maker, cap per person, lines that never vanish, 💛 credit | 1.5 |
| Doors: upgrade panel, "Your gold can", Next steps, card, scaffold, Exchange words | 3 |
| Sizing fix | 0.5–1 |
| The chain pointer | 1–1.5 |
| **Total** | **10–12 days, plus art** |

| Risk | Answer |
| --- | --- |
| A ring is a donation box | A goods part by pipe, a picture that grows, names, two givers |
| A rich player makes the rest feel useless | Credit in hours of your own income; half per person for 3 days |
| Guilt (run 3's Ben: *"I let them down"*) | Givers are shown, never who didn't give; no reminders |
| Gold replaces the maker | At once only where nobody near makes the good; the chain pointer offers the role |
| Banks still grow | Ring sizes follow income; if not enough, the economy seat trims the hub-order bonus |

## 5 · Weeks 2–5

| Player | Run 4 | With this |
| --- | --- | --- |
| **Cleo** (113.7M at the end) | Ordered the Dynamo's half on day 6; bids cut; *"income without a sink is pointless"* (d21) | Day 6: her order fills what nobody makes and asks for a second giver; Ana's day-8 session finishes it, so the Dynamo stands on day 8, not at 89% for good. Day 10: *"Copper Pipe · 14 ingots (you hold them) · Lay it and Max"*, and her levels pay again. Day 17: 20 h to ring 1, her colour on the arcade |
| **Ana** (organiser) | *"I feel I'm the only one pushing"* (d27); Kiln and Cathedral stuck | Day 21: ring 1's plaque shows four names. Day 27: the Cathedral sized right (about 1.6M of goods nobody makes): she orders her half, her card line asks the crew. Day 36: *"Cement Kiln waits for 200 bricks · Order: 2,000 g"*, standing that evening |
| **Dev** (a week late) | *"12M gold, tell me how to be useful"* (d21); a factory that never stood | Day 14: *"A Brickworks makes bricks from Ana's stone and Cleo's coal · Show me"*: he becomes the brick maker, his Big Factory fills from it, the ring's goods name him. Day 21: 8 h to ring 1 |
| **Ben** (phone, back day 36, 13.4M) | *"the thing my friends need from me is hidden"* | His card: *"Ring 3 · 9.2M of 22.8M · Give ▸ a day (430k)"* and *"Copper Cathedral · your part ▸"*. Two taps; his name on the plaque beside people who played all month. Relief, not guilt |
| **Mia** beside Jo (40.1M, 72k spent) | *"spend my big pile of gold sensibly"* (d08); Landmark on day 28 | Day 28: ring 1, 1.8M (a day of her station's 1,257 g/min). She gives half, finishes it after 3 days, and it stands in her Yard in her colour. Her part in the Cathedral is named beside the crew's. Rings 1–3 take about 11M of her 40M, by choice |

## 6 · The mark that would prove me wrong

**From day 14 to 36 the crew spends at least 20% of what it earns, and at least half of that goes to shared goals and
friends (rings, the wonder, gifts and orders for someone else).** Run 4: 0.2%, of which 30,660 g went to shared goals.
If spending stays under 10% with the rings and orders open and on the cards, my claim that the wish to spend is social
is wrong. If it passes 20% but nearly all on private pipes and Max, the doors were the whole problem, and the rings
weren't needed.
