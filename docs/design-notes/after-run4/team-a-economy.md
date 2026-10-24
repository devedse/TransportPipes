# After run 4, round A: economy

## My position

The game already has a sink that grows with income by itself: building levels. They took three quarters of all
spending in run 4 and stopped when the grey pipes filled, around day 7. My nine steps put doors *beside* that loop:
priced in single goods, capped at half, and (by a bug) sized for a Capital earning 60 g a minute. The two doors that
would have reopened the loop, the pipe tier as a want and Widen, never reached a player. So my answer is not a new sink.
**Open the one we have**: make the next pipe the upgrade panel's own next step, let gold buy what no friend near makes,
size the *goals* (not the prices) to income, and give the station something to build after the Landmark.

## 1. Diagnosis

**Levels are the self-scaling sink, and the pipe shut them.** A level costs base × 1.15^(L−1) and gives ×1.10 output,
doubled at levels 10, 25 and 50 (`Content/Definitions.cs:294–302`). Price and output grow together, so the payback
stays roughly constant: that is the idle loop. It took ¾ of run 4's spending, mostly in week 1 (Cleo 227k of 297k by
day 7.9). Then the panel said *"Level 21: +0 g/min: its pipes carry 205 iron ore a minute"* and *"+30 would make nothing
more"* (cleo d06 #220). Behind that wall the curve is big enough for any bank. One Iron Mine, four Small Factories and
three Wind Moilies cost 1.98M to take from level 21 to 30 (×4.7 output), 8.4M from 30 to 40 and 34M from 40 to 50 (my
arithmetic from the formula). Cleo's bot, which got past the wall, ended with an Iron Mine at level 69 and 2.7 days of
income banked (`light-bots-day28.md`).

**The door through the wall was hidden, and in two places it was dead.**
- The next pipe tier is offered only in a full building's own status (`GameWorld.IdleTexts.cs:145–162`), and only
  after the research (`:186`).
- The upgrade panel's "+0" line points nowhere (`GameWorld.Spending.cs:66–71`, `BuildingPanel.tsx:851–857`).
- Next steps saw the wall and said *"Build a Lab: your pipes are full, better ones need research"* (cleo #165, ana
  #186). Cleo finished Copper Pipes at day 6 20:45. Her next steps (#242–#266) were a Big Factory, a Coal Plant and
  "Unstick your Smelter", never the pipe. The advisor's pipe step asks `SupplyStep` for the missing ingots
  (`GameWorld.Advisor.cs:515–523`), and `SupplyStep` returns nothing when you already make them (`:548`). Cleo made 237k
  ingots and sold every one.
- When the research isn't affordable yet, the advisor also says nothing (`:503`).
- Widen exists only for Copper and Steel (`GameWorld.Widen.cs:18`), so it could never appear.

All 1,087 pipe tiles were grey on day 36.

**My shared doors were 100× too small, then shut.**
- The Era stages were sized from residents seen in the last 3 days (`GameWorld.Wonders.cs:139–142`; `IsActive` uses
  `LitPeriod`, `GameWorld.Market.cs:202–204`).
- With nobody seen, the size falls to the 60 g/min floor (`GameWorld.Tick.cs:208`): 21.6k of goods instead of about
  1.6M.
- The daily resize stops at the first delivery (`GameWorld.EraWant.cs:230`).
- The order half is capped (`:15`). Once it is used the offer goes null (`:195–198`), and the words come only after a
  failed click (`:210`). Ben was STUCK on day 36 (#92).
- Order the rest already lets gold finish a want at once when nobody near makes the good (`GameWorld.WantOrders.cs:3–9`),
  but it sat folded on a card. It was offered once, for 1,000 g, while both owners read "is being built".

**The faucets are not the problem.** Crew income grew 39.7% from day 14 to day 36 without spending. A week of Cleo's
hub-order bonuses (306,822 g) is about 2% of a week of her income. The bank grew a day of income a day because nothing
took it.

**A session is days of income.** People came every 4–7 days (Cleo: d6, d10, d14, d21; Ana: d8, d14, d21, d27, d36; Ben:
d3, then d36) and found 2–12M waiting (*"Away 4 days: +11.7M"*, cleo #247; *"+7.6M"*, mia #119). A door they will use
spends days of income in one or two taps. Ours cost 1k–60k: minutes.

**Verdict.** The idea was right: goods as the wall, friends first. But the steps were too small (a bug), too capped
(half, then silence), and hidden from where a player with gold looks: the upgrade panel of a building at its pipe.

## 2. Proposal, in build order

1. **The pipe door on the upgrade panel and in Next steps.** At the pipe the panel never says "+0" alone. It names the
   pipe and offers one button that goes the whole way, with one preview: the research, then the tier (as a want when
   its materials aren't in Storage), then Max to the new limit. At Copper's limit the button is Widen, then Max. The
   advisor's dead ends (`Advisor.cs:503`, `:515–523`, `:548`) become the want or the research step, never nothing.
   *Replaces* "+N would make nothing more" and the fix buried in the idle status.
2. **Gold finishes what no friend near makes**, by one rule for every want: a building's, a pipe's, an Era stage's, a
   Landmark tier's (Q1).
3. **Size the goals, not the prices**, and fix the sizing bug (Q2).
4. **Landmark tiers** after day 17 (Q4).
5. **The bank in days** on the top bar.

**On the analyst's recommendations.** I adopt 1, and 3 as the cheaper way shown beside every order. I change 2: goals
are sized to income, but not per-unit prices. A parcel at two hours of Cleo's income (279k instead of 10k) is the same
land at a higher price, not a reason to grow. "Words on the upgrade panel" is my change 1. Recommendations 4–5 belong to
other seats.

## 3. The five questions

**Q1. Gold for what no friend near makes: yes.** "Near" means a station resident seen in 7 days whose buildings made
the good in the last 24 hours, or who holds it.
- A good nobody near makes may be ordered in full, at once, at 2× base. That is the price already built
  (`WantOrders.cs:16`), so there is one number to learn.
- A good a friend near makes stays theirs: in their window for a private want (built), and half by pipe for a shared
  goal (built).
- When a friend starts making a good, the cap snaps back to half for whatever isn't ordered yet. Their deliveries come
  off open orders, with the gold back to the payer (built).

The friends keep three things:
- the pipe half of everything someone near makes;
- making the missing good at 1× instead of 2× (*"or a Brickworks on your estate makes bricks from Ana's stone and Cleo's
  coal · Show me"*);
- the Patron line, where a pipe delivery counts four times an order.

The keystone is never ordered past half.

**Q2. Sizing.**

| What | Sized in | Curve |
| --- | --- | --- |
| Levels, Max | base × 1.15^(L−1), unchanged | tracks income already |
| Pipe tier | gold + one ingot or steel a tile, unchanged | a key, not a sink |
| Widen | 1 h, then 2 more, of the buyer's `MeasuredIncome` (built) | ×2, ×4 |
| Orders | 2× base of the goods | the goal sets the amount |
| Era stage | 6 h of the Capital's income | flat per stage |
| Landmark tier n | 6 h × 2^(n−1) of the station's income | doubling |
| Parcels | 5k × 2^i, unchanged | land is not the sink |

Shared goals count residents seen in `ActivePeriod` (7 days, `GameWorld.Research.cs:8`), not the 3-day `IsActive`.
A stage is resized daily, upward only, until a quarter of it has arrived, so a first delivery no longer freezes it.

**Q3. The doors, with plenty of gold and the building at its pipe.**
- **Upgrade panel**: *"Its Grey Pipe is full: 205 iron ore a minute. [Copper Pipe, then Max to 30 · 165k] +N g/min"*.
  - Ingots elsewhere: *"[Lay Copper Pipe · 140 g] it waits for 14 copper ingots: Cleo makes them"*.
  - Nobody near makes them: *"…and order 14 ingots · 504 g"*.
  - No research yet: *"[Research Copper Pipes]"* or *"[Build a Lab · Show me]"*.
  - At Copper's limit: *"[Widen ×2, then Max]"*.
- **Next steps**: the same step, ranked by the advisor's bank-sized effort (`Advisor.cs:150`).
- **Top bar**: *"40.6M · 24 days"*. Tapping it opens Next steps.
- **Cards**: a friend's stuck want unfolds when nobody makes the good: *"Dev's Big Factory waits for 1,800 bricks.
  Nobody near makes bricks. [Order · 18,000 g] · a Brickworks on Dev's estate would make them"*. It reads "waits for",
  not "is being built".
- **Wonder tab**: no vanishing button. *"Steel: nobody in the Capital makes it: order all of it · 120 g each"*, or
  *"The other half comes by pipe: Cleo's Steel Mill makes it."*

**Q4. After the Landmark: tiers.** The Landmark grows a storey per tier. Tier n wants 6 h × 2^(n−1) of the station's
income, in its two most-made goods plus one it doesn't make. Each tier that stands makes the residents' pipes carry +10%,
named like a Refinement (*"Pipes carry +10% · Capital SE's Landmark, 3rd storey"*). The shared goal feeds the private
loop: wider pipes make more levels worth buying. Run 4's Foundry Hall and Landmark stood within about two days of
opening (day 15 to day 17). At that flow, tiers 1–5 take about 2, 3, 5, 8 and 14 days: four tiers in days 17–36, the
fifth near day 40, then on without end.

**Q5. The bots.** The light bot plays a person's hours: 20 minutes every second evening, at most 15 buildings
(`LightBotSimulation.cs:9–12`). But it takes the first affordable advisor step up to 30 times a session. It never sees
"+0", only steps with a gain. That is why it spends. The bots also show a split:
- the three seats that mine copper ore spent (Cleo 2.7, Mia 3.2, Jo 5.1 days banked);
- the three that don't, didn't (Ana 18.9, Dev 16.8, Ben 10.8).

My reading is that their pipe step had no road to ingots they can't make (`Advisor.cs:515–523`). A bot run with ingots
orderable when no maker is near would confirm it before run 5. What it says: people stop where the game stops pointing
(Ben: *"I still don't know if upgrading earns anything"*, d36 #82), and for some seats the wall is real, not just words.

## 4. Cost and risks

**Cost** (one developer, each step playable): about 8 days in all.

| Step | What | Time |
| --- | --- | --- |
| 1 | Sizing fix | ½ day |
| 2 | Advisor dead ends, plus the panel's pipe line and its combined preview (the what-ifs exist: `Advisor.cs:491–493`, `Spending.cs:51–53`) | 2 days |
| 3 | One order rule, with words on the cards and the Wonder tab | 1½ days |
| 4 | Landmark tiers, with storey art through `process.mjs` and `fit.mjs` | 3–4 days |
| 5 | Top bar | ½ day |

**Risks.**
- **The hub's √ price.** ×4.7 output brings about ×2.2 income, so each tier's payback is longer. The previews must show
  the true number. A 1–7 day payback still beats a 24-day bank.
- **Gold replacing friends.** This applies only to goods nobody near makes, it is half again once a friend makes one,
  and pipe delivery counts four times as much.
- **Incomes running away after the pipe opens.** Goals are sized as each tier starts.
- **The friend who comes a week later.** Shared goals are sized on everyone's summed income, so a newcomer's small
  income adds little. Their goods are wanted.

## 5. Walk-through, weeks 2–5 (run 4's numbers; costs from the formulas above)

**Cleo** (day 10: 2,322 g/min, 19M).
- Day 10: taps *"Copper Pipe, then Max to 30"* on her Iron Mine's line, about 2.0M: 10% of her bank in one tap.
- Day 14 (32.5M): Widen ×4 for 436k, then Max to 40 for 8.4M.
- Day 21, Era 2 (60M): the Cathedral, sized right, wants steel and concrete nobody makes. She and Ana order it, up to
  3.2M at 2× base. Then Steel Pipes, and levels 40→50 for up to 34M.

That is roughly 40% of what she earns in weeks 2–5, as her bot did.

**Ana** (day 14: 723 g/min, 10.5M).
- Day 14: her panel at the pipe says *"[Build a Lab · Show me]"*. She passed it in Next steps in run 4 (#186, #282).
- Day 21: Copper Pipe as a want, with Cleo's ingots through the Yard and no gold needed. Then Max 18→25 for about 0.86M.
- Day 27: orders the steel for the station's third storey.

**Dev** (day 21: 12M, *"tell me how to be useful"*). His Big Factory's card offers *"Order the bricks · 18,000 g, or build
a Brickworks"*. He does both and becomes the Capital's brick maker: the Dynamo's last 450 bricks, Ana's Kiln on day 36.
His wood fills the Landmark tiers, and his bank levels the Brickworks through Copper.

**Ben** (back on day 36: 13.4M, a phone, a skim). Two doors that need no reading:
- *"Steel and concrete for the Cathedral: nobody in the Capital makes them, order all of it"*, which is what he tried
  at #92;
- *"Landmark, 4th storey: your Quarry's stone goes there by itself"*.

He spends 1–3M and sees his name on the Patron line.

**Mia** (alone beside Jo; day 15: 761 g/min, 9.8M).
- Day 15: her Lab is going up (#139–143). Next time: Copper Pipes. Nobody near makes ingots, so *"order 14 ingots ·
  504 g"* or *"a Smelter makes them from your copper ore and Jo's coal"*.
- The NW Landmark tiers are sized on Mia's and Jo's income (about 1,300 g/min: tier 1 is 468k of goods). Gold finishes
  what Jo doesn't make, so her goal never freezes like the Dynamo.

If she stops, nothing decays.

## 6. The mark that would prove me wrong

In run 5, log every time a full building's upgrade panel shows the pipe door. **If Cleo, Ana and Dev each see it at least
three times and two of them still spend under 5% of what they earn on days 8–36, I am wrong:** the cause is not the
door or its size, and gold needs a use that doesn't raise income. If I'm right, each of them spends at least 15% of
days 8–36, and more than half of that goes on levels for buildings whose pipe they upgraded or widened in the 48 hours
before.
