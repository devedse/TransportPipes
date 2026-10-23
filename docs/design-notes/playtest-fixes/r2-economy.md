# Round 2 · economy

I read all five round-1 papers and checked their numbers against `metrics.jsonl`, `server/events.jsonl` and the code.
First, one correction to my own paper. The engineer is right (r1-engineer, Root causes): the solver gives every input
of a building that runs below full speed the same fraction, so the inputs always tie. My "energy was scarcest by
0.0005" was rounding noise. The Lab "lacked" both, 1,082 energy and 216 ingots, and Cleo's own note says so (cleo
d14-1900 #261). Everything that follows from it stands.

## Answers to the questions put to me

**Engineer: does E1's "unsold, paid at the hub price" count toward the seller's Hub Order? Yes, and toward its trading
post's throughput too.** Without that, a false match could sell beyond the Hub Order at full price. And what a full
post couldn't have sold was never income in the first place. "Never worse than the hub" means exactly what the hub
would have paid: `min(untaken, the post's free throughput) × HubBuyPrice`, booked as a hub sale.

**Engineer: keep NeighbourInNeed Houses? Yes, with E3's trading post.** For a lone player they are the "a neighbour
needs your X" hook. But their one missing good must follow the recipe rule below: a good that some human in the
station makes or can make. Mia's House wanted sand, and she had none, so even fixed it would have bought nothing from
her.

**Engineer and together: the hours for baskets based on measured income, and a pace of about 3 days.** I(c) is a
company's gold a minute from its production, averaged over 24 hours. A basket is then sized like this:

- each good the station already makes: 4 h of the station's spare of it;
- the keystone: 6 h of one level-1 building that makes it;
- the whole basket: at most 6 h of the residents' summed I(c).

The allowance is 2 h of I(c) a day, banked for 3 days. So the cap guarantees that a basket fills within 3 days by
construction. Worked, at the new time of sizing (see "missed" 2): the Foundation stands around 20:15 on day 1, with
everyone at Level 2. Spare energy is about 220/min across Ana, Ben and Cleo, giving 53k energy. Iron ore gets the
floor of one level-1 mine, 4 h × 12 = 2,880 (8.6k g). The keystone, copper ore or sand, takes 6 h × 10–15 = 3.6k–5.4k.
That is about 68k g in all, against a cap of 108k and allowances of 36k a day. It is done in about 2 days if one
resident builds the keystone's building, against 329k and never.

**Researcher: do baskets, allowances and the other T(L) amounts follow what players really earn? Yes, per company,
not per level.** I(c) replaces T(L) in baskets, the allowance, the "delivering" threshold, the Landmark and Wonder
stages. T(L) stays only where it is a gold price or a cap: trunk segments (a sink) and corporation donations. **Is
85–90% measured on paid income at equal effort? It must be.** Mia had 7 buildings and Dev 10, so raw income measures
effort. I propose a balance run with identical bots (solo in a House station against a crew of four), plus per-building
income in run 2.

**Together: is +3% enough once it's shown in gold?** It is the right size and the wrong unit. +3% is +5.6 g/min for
Ana and +36 g/min for the station (end-of-run incomes 187 + 77 + 799 + 152). The stages add up to about +18–20% by week
3, and the project already pays the hub's full price for what it takes. Show the station's total: "+36 g/min for
Capital NW, 52k a day". Don't raise it, because solo baskets earn the same bonuses.

**Together: is waiving the caretaker's fee on tended days an acceptable faucet? Yes.** The fee is 10% of a dormant
company's earnings (`GameWorld.Moving.cs:27, 566`), only between day 14 and day 30 of an absence. Waiving it on tended
days gives at most 10% of an idle estate's own income. Ben's whole fee was 186k, against the 2.6M he earned away. Away
then pays the same as playing, never more. Free storm repair is fine too: a repair is 10% of the building's price
(`GameWorld.Events.cs:445`), 20 g for a moily.

**Clarity: are 90% and a quarter of the gold the right thresholds for Max?** Yes, with your own caveat: count only the
building's own pipes, not what the Yard might sell it. Neighbours can leave. Cleo's case comes out at level 9 for
27.5k and 10.7 research a minute, which matches your mock-up.

**Clarity: what should replace "You may still bring 740k g worth"?** It is 3 days × 2 h × T(14) = 739.6k, ten times
Ana's output. With I(c) it becomes 26k a day (77k banked). Show it only when it holds someone back: "You've brought
your share for today (26k g); more tomorrow." Otherwise show the price ("pays 1.20 g a sand, the hub's full price")
and the pace ("at this pace: about 2 days").

## Where I agree

- **Engineer E2 (lacks from a probe solve) replaces my rules A and B.** It also covers stuck outputs, pipe capacity
  and Mia's idle Glassworks, and it fixes links, the Links tab and the advisor in one place. If the 5 ms budget fails,
  fall back to my A + B + C: own spare first, the other-inputs cap, and the feedback damping, which is also together's
  proposal 1.
- **Engineer E1 and together 1 are my rule D.** I merge them with the Hub Order and throughput point above.
- **Engineer E3** (a trading post for NeighbourInNeed Houses), **E4** (screens show what was paid), and **E10** (solve
  before reading rates).
- **Engineer E8 and together 5 are my project proposal**, with one change: the Foundation is energy only at 900, not
  600. With Houses bringing 25% by themselves, Ana alone fills it in about 7 minutes. It still stands by minute 15–20,
  and Handshakes at 20:08 come before it, not with it.
- **Engineer E7 and clarity 5 are my smart Max.** I adopt "own pipes only" and the 25% confirm.
- **Clarity 2.1: show the Next steps from Level 3.** This decides whether my bank-sized advice exists at all. Clarity
  counted it: 4 of 5 players never saw the Next steps (GoalTracker.tsx:37).
- **Clarity 3: one return card.** That is where the Invest line goes (see "missed" 4).
- **Together 2 and 3: two named ends for every trade, and the giver's door.** Together's prices check out: an Ask at
  1.75 × 1.2 = 2.10 g a sand, and 41k energy × 1.75 = 72k g on the return card.
- **The researcher's money table** (real growth minus the hub rate is about 0 for everyone) and the corrected Lab effect
  (+63%) match mine (+316 for the neighbours, +71 for Cleo). The storm losses also check out in
  `server/events.jsonl`: Ana's moilies were damaged on day 18 (230 → 209 g/min) and day 32 (215 → 185), Ben's on day 10
  (90 → 76).

**I withdraw my keystone valve** (r1-economy 2f, buying the rest from the hub after 72 h). The researcher and together
are right that it turns the keystone into a gold check, and it would be unneeded once every recipe good is makeable by
an active resident. In its place: when no active resident can make the keystone any more (its maker has slept 3 days),
pick the keystone again among the goods someone can make.

## Where I object

**1. "Cleo's 179k points came from the bug that let a Lab at 3% make 15 points a minute" (r1-researcher, "Where I
differ" 6).** There is no bug. 3% × 511 = 15.3. The engineer gets the same (E12: 5 × 101.8 × 2.94% = 15.0). And
points are plentiful by design, Max or not: a level-1 Lab makes 5 a minute, so Era 1's 3,500 points (12 techs, without
the Deep Lift) take 12 hours, and Cleo's 11,800 minutes would have given 59k even at level 1. What gates research is
one tech at a time with sessions 2–7 days apart (`GameWorld.Research.cs:64`). Cleo started five techs in 21 days. I
agree it ranks low, but for that reason. My queue of five (S) stays, at the bottom of the list.

**2. "Gold piled up because players couldn't turn it into anything that worked... re-measure after fixes 1–4"
(r1-researcher, What I'd not do).** That is true of Ana's factory and false of the gold. In 21 of 23 sessions after
day 1, players spent under 3% of their bank. Yet every upgrade they did buy worked and paid back in 29–74 minutes:
cleo d02-2000 #84 (+136 g/min for 10.1k), d10-2000 #203 (+114 for 3,357), d14-1900 #228 and #231 (+106, +97), and ana
d08-2000 #146 (+10 levels for 4,061 g). Cleo had "no clear income lever left" (d14 diary) while a moily from level 10
to 30 (72k) plus a trading post to level 10 (42k) would have tripled her income for 2.4% of her bank. The advisor
can't say so: it costs a step at 10 minutes of income and offers +1 level (Advisor.cs:140, 814). And once the Yard is
honest, investment is the only income lever left, so waiting a round costs the whole of run 2's middle game.

**3. Run 2's pass mark "Mia ≥ 85% of Dev's real income" (r1-researcher 6).** That mark measures effort. Fixed, Mia's
energy goes back to the hub: about 99 + 28 = 127 g/min against Dev's ~150, or 82–85% (the engineer predicts 82%, the
researcher 87%). That is with 7 buildings against 10. The mark could fail with the game right, or pass with it wrong.
Use per-building income in the run and the equal-bot parity run for the promise.

**4. "Water Tower done by day 14" as run 2's pass mark (r1-researcher 6) is too late.** Stages run in order, and the
Foundry Hall opens on day 14 for the week-3 growth (§12, done by day 18–19). With the Foundation on day 1, the mark
should be: the Water Tower done by day 8 and the Workshop by day 14. Otherwise the +15% row fails for a reason we
built in.

**5. A Foundation of 600 energy (together 5).** That is too short to be seen (see above). It's a small point.

## What everyone missed

**1. An honest Yard will trade almost nothing, and then Mia out-earns the crew.** With E2's true lacks, this run's real
trade between humans would have been about zero. The Lab lacked ingots nobody had spare, Ana's factory and Dev's
dredger were stuck, and Mia's House wanted sand. Every kit is energy, and every estate gets iron, stone and forest
(WorldGenerator.cs: "Guaranteed basics", "Small extras every estate gets"). On top of that, a bank of millions makes
self-sufficiency free. The advisor even builds it in: `ChainSteps` suggests a factory only when the company makes all
its inputs itself (Advisor.cs, `In.Keys.All(made[n].Contains)`). With a trade share t = 0, a crewed player earns
1.0 × the hub. A solo player selling a quarter to Collector Houses at 1.25× earns 1.0625. **Parity inverts: solo is
106% of crewed.** Lasting trade can only come from goods a seat can't make at all: copper outside Copper Gulch, sand
away from a beach, coal outside Coal Valley (40% of other estates get a little). So:

- `ChainSteps` also offers a factory whose missing input a station neighbour has spare, priced at 1.75 × base. For
  example: "Ben makes sand: a Glassworks on your gate network gets it at 2.10 g by itself and makes glass worth 14 g".
  Even at 2.10 a sand, a Glassworks earns 13.5 g/min over its inputs.
- Recipes prefer those goods.
- Run 2 measures t. If t is under 15% after the fixes, the crew promise fails for structural reasons, and the next
  lever is seat kits with one consumer of a neighbour's specialty.

This turns together's "receiver card" from luck into a rule.

**2. Fixing the Foundation moves the Water Tower's sizing to day 1, at Level 2.** Then nobody makes anything but
energy, and nothing but the Iron Mine is unlocked. A recipe rule of "made now or unlocked now" gives an energy-only
tower. So: goods made now, or makeable with one building unlocked by Level 5. Everyone reached Level 5 within about a
day: Ana on day 2, Cleo and Ben on day 3, Dev two days after arriving. The card names the level: "Sand: a Sand Dredger,
Level 5, on a beach".

**3. The allowance is spent in alphabetical order.** `MatchProject` walks `Basket.Keys.Order(Ordinal)`
(Projects.cs:289), so energy uses up each resident's daily allowance before the keystone. Exempt the keystone from the
allowance, since one maker usually brings it all.

**4. The Invest advice has to sit where people look: the return card,** because that is when the bank is big. For
example: "You have 1.8M · upgrading your 3 Wind Moilys to level 25 brings about +X g/min, back in about 1 h · Invest".
It spends at most half the bank, on levels that pass smart Max only. The advisor's effort term becomes max(200, 10 min
of income, 2% of the bank), so the steps rank by what they bring.

**5. E3 and E2 turn on NPC gold for the first time.** Houses' purchases have never been paid, so the design's "new gold
from NPCs ≤ 6% of faucets" (§21) has never been measured. Add it to the run-2 measures.

## My ranked top 10

| # | Fix (merged) | Effort | Why this rank |
| --- | --- | --- | --- |
| 1 | **An honest Yard:** lacks from a probe solve (E2; fallback my A+B+C); never worse than the hub, counted in the Hub Order and the post's throughput (E1, together 1, my D); a trading post for NeighbourInNeed Houses (E3) | M | It took 3.5M from three friends and about 28 g/min from Mia, and it paid nobody in 36 days. Every later "show the trade" fix needs true numbers |
| 2 | **Dead ends explain themselves:** `IdleReason`, the card leaves stuck buildings out of "short", the pipe-to-post button, Loady's `firstIngot` text (E5, clarity 1) | M | The top pain (5 real STUCK notes). It is also why Ana's income sat flat for 32 days: her attention went to the factory, not to investing |
| 3 | **A project the station can reach:** Foundation of 900 energy; done at 99.5% (no 18-h tail); goods made or makeable by Level 5; per-good size capped at 6 h of the summed I(c); keystone exempt from the allowance; shrink and re-pick when makers sleep; I(c) replaces T(L) (E8, together 5, mine) | M | The only shared buyer in week 1, and most of week 3's growth: Foundry Hall +10% on two chains ≈ +9% |
| 4 | **Paid trade, visible:** the top bar, "Where it goes" and the return card show paid Yard, Ask and project income, with two named ends (E4, together 2) | S–M | Only after 1, as the researcher says. It turns 1 into "playing together" |
| 5 | **One return card** with friends, the project, storms (Repair all), a stuck building and the Invest line (clarity 3, E11) | M | All five players, every return. It is the only screen read every session |
| 6 | **Gold that turns into income:** smart Max (E7, clarity 5), the advisor scaled to the bank, Next steps from Level 3 (clarity 2.1), Invest on the return card | S–M | 21 of 23 sessions spent under 3% of the bank, while upgrades paid back within an hour. With an honest Yard it is the main lever for income |
| 7 | **Interdependence:** `ChainSteps` offers neighbour-fed chains at 1.75; the giver's door and the receiver's card (together 3); measure t | M | Without it, t ≈ 0 and solo beats crew (missed 1) |
| 8 | **Seat picker** (E6, clarity 4), and "A station of my own" prefers stations of four | S | Mia's 9 minutes. A 2-seat House station also gave her a useless Houses' quarter |
| 9 | **Loady follows the estate** (E9, clarity 2) | S | Ana, Mia and Dev stuck on steps for 13–32 days |
| 10 | **The absent friend and a tend that does a chore:** repair and the fee waiver (together 7), names (together 6, clarity 6) | M | Rows 6, 13 and 14. The faucet is bounded (above) |

Below the line, each S, in any order: "+0/min" (E10), daily orders from production, the research queue of five, chat
dates. I've dropped the hub's research points (r1 4): Ana's valve wouldn't have fixed her factory (the researcher), and
fix 2 covers her case.
