# After run 4, round B: economy

## 1. What I take from the others, and what I drop

**I take:**
- **Friends: the half cap is one cap for the whole Capital.** `EraWant.cs:195–198` counts everyone's orders, so the
  first person to order uses it up for all. I missed this. Ana: *"I see no way to order more myself … only my name on
  the list"* (ana d36 #354).
- **Psychologist and friends: gold's late wish was social.** All 30,660 g the nine steps' sinks took went into the one
  shared, visible door, the Era wonder's order (*"nice that I can chip in without building"*, ana d27 #316), and five
  offers of gold went into the chat. A shared goal needs a gold door in hours of the giver's own income, with givers
  named in hours.
- **Lonely: income arrives in bulk, our doors charged by the click.** Mia came back to 2.2M and 7.6M and spent 30–37k
  each time (mia d08 #87, d15 #119). My "Copper Pipe, then Max" was one building at a time. *"Grow everything that
  pays"* is the same idea, sized to the bank.
- **Outsider and wild: Widen on grey pipes too.** Cleo had 237k ingots and never laid copper, so the problem was the
  hidden door, not scarce copper. Grey ×4 (400 a minute) still stays below one Copper Pipe (500), which can widen
  further, to 2,000. The light bots' three seats without copper (Ana 18.9, Dev 16.8, Ben 10.8 days banked) need a door
  that doesn't wait on a maker.
- **Psychologist: a waiting building reads "is being built"** (`HoverTip.tsx:59`), and gold for a friend's want is a
  gift, so it gets a 💛 at both ends. For shared goals, orders keep their 25% credit.

**I drop:**
- my Landmark tiers sized only in goods, with gold coming in only through orders;
- the doubling (6 h × 2^(n−1));
- "Copper first, then Widen";
- my mark.

## 2. What I reject, and why

- **Wild's Works** (station-owned machines, 1, 2, 4 days of the station's income; 5–7 build days). Three reasons:
  - It duplicates two cheaper parts all six of us keep: gold ordering at once what nobody near makes, and the chain
    pointer.
  - It takes away the role that four papers give Dev: *"Bricks need stone + coal, I have neither"* (dev d14 #116–117)
    becomes "a Brickworks on your estate, 1,400 g" (`buildings.yaml:276–281`), and a Works would make his bricks for
    him.
  - It adds a new kind of owner, the station, to the books.

  I keep its insight, that the shared goal should *do* something: each storey widens the residents' pipes.
- **Wild's 2.5× price.** No evidence that price was the barrier. Players never pressed an order because the door was
  folded or gone. 2.0 is already built in two places (`WantOrders.cs:16`, `Asks.cs:33`). Keep one number.
- **Lonely's Era stage at 1 day of the Capital's income** (4× today's). Run 3's stages froze at 89–96% and run 4's at
  the pipe half. A bigger stage with half by pipe only freezes harder. Keep 6 h, which all others keep.
- **Steep tier curves.** At about one storey a week:
  - lonely's 1, 2, 4, 8 days asks for 8 days of income in week 4;
  - the psychologist's 1, 2, 3 days asks for 43% of income by ring 3.

  Two of the crew stop after day 21 in this story (Cleo's and Dev's last sessions). Any goal that outgrows the givers
  freezes like run 3's. Friends' 1 day × 1.2 a storey asks about 14–20% of income, which is the share we want.
- **Parcels at 2 h of the buyer's income** (friends). The same land at a higher price is not a reason to grow. In run 4
  the 10k parcel was the This-week line's only offer, and nobody took it.
- **A "≤ 3 days banked" mark on people** (lonely). A player who comes every 5–7 days holds at least those 5–7 days just
  before a session, so the mark can't pass for them. It works for bots, which play every second evening
  (`LightBotSimulation.cs:9–12`). People need a mark on what they spend in a session.

## 3. The open questions, final

- **After the Landmark:** storeys (section 4, row 4), not Works.
- **Gold in a shared goal:** *Give*, in hours of your own income, shown in hours, at most half a purse each for 3 days.
  Unit prices stay the same for everyone.
- **Price and credit:** 2.0× base. Orders into a shared goal are credited 25%; goods bought for a friend's want get a 💛.
- **Widen:** every tier, grey included, with the friend's Copper Pipe shown first as the better deal.
- **One door or one plan:** both, in one list (row 5). The plan only buys what pays back within 7 days. Banks were
  24–29 days of income, so anything that repays within a week beats holding gold.
- **The player alone:** in. The chat and the named gifts are cheap (analyst rows 1, 5 and 11). The branch goes last,
  behind the pacing check: our run-3 trigger for it fired.
- **Before run 5:** two checks on the existing simulators, each with a stop rule.
  - The light bots with every new door. The three seats without copper must fall from 10.8–18.9 days to ≤ 5 days on
    day 28. If they don't, the wall isn't the pipe, and we stop and look again.
  - `typical`, for Level 30 on day 25 ± 2. If it comes before day 21, grey Widen stops at ×2.

## 4. The solution I'd sign, in build order

| # | Mechanism, with its numbers | Replaces | Days |
| --- | --- | --- | --- |
| 1 | **Sizing.** Shared goals count residents seen in 7 days (`ActivePeriod`). They are never sized from the 60 g/min floor while anyone in the district has played in 7 days, and a stage first sizes at a resident's session. Each stage resizes daily, upward only, until a quarter of it has arrived. Era stage: 6 h of the Capital's income | The 3-day `IsActive` (`Wonders.cs:139–142`); the freeze at the first delivery (`EraWant.cs:230`) | ½–1 |
| 2 | **The pipe door.** The upgrade panel at the pipe shows the next tier and Widen side by side, copper first. A tier short of materials becomes a want, and the ingots can be ordered when nobody near makes them. Widen: ×2 for 1 h, ×4 for 2 h more, on every tier. Then *"Max now pays"* with one preview. The advisor's dead ends become the want or the research step | "+N would make nothing more"; `Widens` grey refusal (`Widen.cs:18`); `Advisor.cs:503`, `:515–523`, `:548` | 2 |
| 3 | **One order rule.** "Nobody near makes it" means no resident seen in 7 days made the good in the last 24 h. Then anyone may order all of it at once, at 2.0× base. Otherwise friends go first (the window as built) and half of a shared good comes by pipe. *Chip in 1 h · 6 h · 1 day* on the Era wonder buys its orderable goods. A spent half says who ordered it and who makes the rest. A waiting building reads *"waits for 1,800 bricks · Order · 18,000 g · or a Brickworks on your estate (Ana's stone, Cleo's coal)"*, offered first to the friend with gold and no maker role. Goods bought for a friend get a 💛 | The vanished button; "is being built"; the folded Order | 2 |
| 4 | **Landmark storeys.** Each storey has three parts. Goods: 6 h of the station's spare in two goods a playing resident makes, filled by the Yard. Purse: 1 day × 1.2^(n−1) of the station's 7-day income, through *Give*, with half per giver for 3 days. Perk: +10% on the residents' pipes, named like a Refinement. The next storey opens when the last stands | This-week's 10k parcel; nothing to build after day 17 | 3 + 1–2 art |
| 5 | **Your gold can.** The top bar reads *"39.8M · 14 days"*. Tapping it shows up to three lines: the open storey (your part in hours), then a want with no maker (the Era's or a friend's), then *"Grow everything that pays · 4.1M of 13.4M · +420 g/min"*, which means Max plus pipe doors where payback is ≤ 7 days, with one preview, one tap and Undo for an hour. The same lines go in Next steps (shared lines rank at least as high as research does, `Advisor.cs:136–150`), and one of them on the return card, never folded | The bare gold number; the folded growth line | 2½ |
| 6 | **People.** The chat opens on the district channel if nobody in the player's station has written. A stranger's gift is named at both ends | Mia's unanswered greetings | 1½ |
| 7 | **The branch** (if the pacing check passes with it): from Level 15, for 2 days of the buyer's income, next to a crew that plays | — | 1½ |

**Total:** 13–15 days, and every step is playable on its own. If time is short, cut 7, then 6. Steps 1–5 are the gold.

On run 4's numbers, Capital SE made about 5,800 g/min on day 21, so storey 1's purse is about 8.4M and the next is
10M. Cleo's line to level 30, then to 40, costs 2.0M and then 8.4M. Ben's 1-hour Give is 24k. Mia and Jo's storey 1 is
about 1.8M.

## 5. One mark for run 5

**The sessions of players at least a week past their start spend, on average, at least 10% of the gold they open
with.** In run 4 such sessions spent about 2% or less: Mia 37k of 7.7M on day 15, Ana 42.8k across her day-21 and
day-27 sessions with 18–26M, and Mia-B about 31k of roughly 1.4M on day 8 (`marks.json`, samples 7.9–35.8).

10% a session is roughly 20–30% of earnings for a player who comes weekly. That is our old 20% mark, measured in a way
the session calendar can't spoil.

**It proves the plan wrong if the average stays under 5%**, while every such session showed *Your gold can* with a
shared line, and the pipe door wherever a building was at its pipe (both logged).

Readings, not marks:
- the share that went to shared goals against private ones (psychologist, wild);
- Ben's day-36 session (lonely);
- the Gives (friends);
- doors pressed (outsider).

## Points I expect to be hardest to merge

1. **Works against storeys.** Wild will hold that the shared goal must make goods.
2. **The storey curve and the purse.** Doubling, linear or ×1.2; half per giver for 3 days, or naming only; whether a
   two-friend keystone is in each storey (friends) or not (my draft, for Mia's sake).
3. **The perk:** +10% (mine), +25% (friends), +5% (outsider), or the psychologist's targeted perks.
4. **Grey Widen ×4** against the friend's copper role, and who decides if the pacing check fails.
5. **The branch:** in, last, or deferred again. It changes Mia's story, so run 5's player-alone reading changes too.
6. **Credit:** a 💛 and full credit for gold bought for friends (psychologist) against 25% everywhere (lonely).
7. **The mark:** a session share against 20% of earnings, Ben's 25%, or chip-ins ≥ 5%.
