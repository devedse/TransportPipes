# Round 1 · economy

My seat: the numbers. Everything below comes from `metrics.jsonl` (the 6-hour gold samples; "real" means how fast a
player's gold actually grew), the digest, `docs/balance/` and the code. Estimates are marked as estimates.

## What I see

**1. The Yard destroyed money instead of making it.** From Cleo's Lab Max (cleo d06-1400 #177) to its demolition
(cleo d14-1900 #257), the Yard matched 361 energy/min of Ana's, Dev's and Ben's spare to a Lab that could use none of
it. Real gold:

| | before | during the Lab | after | lost |
| --- | --- | --- | --- | --- |
| Ana | 198 g/min | 70 | 229 | ≈ 1.9M |
| Ben | 90 | 0.25 | 76 | ≈ 0.9M |
| Dev | (arrived day 8) | 60 | 146 | ≈ 0.75M |

About **3.5M gold** left the three neighbours over eight days. The Lab cost them 70% of their combined income (137 vs
453 g/min). Ana's XP rate fell from 3,580 to 475 a day and she sat at Level 8 for six days. One correction to the
headline: the station's income did not "double" because of the Lab alone. Of the +627 g/min, +316 is the neighbours;
the rest is Cleo's own upgrades in the same session (#226, #229).

**No Yard trade paid a player in 36 days.** After day 14, Ana's "Yard 9.1 g/min" never reached her gold (days 15.9–17.6:
she grew 228.2 g/min against a hub rate of 228.1). The same goes for Mia's "31 g/min to Houses" (days 25.8–29.8: 99.7
g/min against a hub rate of 99–100). The design's target for the share of output traded between humans is 15–30% (§21).
The real share was 0%.

**2. The project was sized for a player ten times richer than anyone in the run.** The Water Tower basket is
6 h × T(6) for one resident: 360 min × 915 g/min = 329,400 g, split three ways: 91,500 sand, 73,200 wood, 73,200 stone.
That matches the screen exactly (cleo d21-1900 #303). Ana's real income then was 192 g/min, so the basket was 28 hours
of her whole income, paid in goods nobody in the station made. At the end of the run:

| | Level | T(L) | real | T ÷ real |
| --- | --- | --- | --- | --- |
| Ana | 16 | 2,303 | 186 | 12× |
| Dev | 14 | 2,054 | 152 | 13× |
| Mia | 12 | 1,785 | 101 | 18× |
| Cleo | 23 | 3,230 | 799 | 4× |

Ana's "740k g worth" (ana d27-2000 #262) is no display bug: it is 3 days × 2 h × T(14) = 739.6k. T(L) comes from the
typical bot: five sessions a day, about 290 buildings, 3,000 g/min by day 7 (`balance-typical-together.md`). Our
players had a session every 2–7 days and 6–13 buildings; the bot reached Level 13 on day 3, Ana on day 23. The recipe
also ignored what people made: sand and wood from the seats' land types, stone as the keystone (the Quarry needs Level
10; nobody had it until day 8.85), not the energy and iron ore all four made.

**3. Every stage has an 18-hour tail.** The project takes at most what is left ÷ 60 per minute, so what is left decays
exponentially, and a good only counts as done at 10⁻⁶ units. For 150 iron ore that takes 60 × ln(1.5 × 10⁸) ≈ 1,130
min ≈ 19 h, however much ore there is. We saw it twice: the crew's first iron mines came on day 2 at 20:04 and the
Foundation stood on day 3 at 14:18 (18.2 h). Mia's mine came on day 15 at about 21:13 and her Foundation stood on day 16
at 15:17 (18.1 h). The Foundation's iron ore is also in no starter kit, so Mia's Foundation took 15 days.

**4. Gold piles up because nothing asks for it, not because nothing can take it.** In 21 of the 23 sessions after day 1,
the player spent under 3% of their bank. The two exceptions were both Cleo's: on day 2 (10.5% of her bank: +167 g/min
for 10.3k) and the Lab Max on day 6 (97.5%: +0). From day 8 on, players came back to banks of 4 to 33 days of income.
Yet every upgrade they did buy paid back in 29–74 minutes: +136 g/min for 10.1k (cleo d02-2000 #84), +114 for 3,357
(d10-2000 #203), +106 for 3,357 and +97 for 4,917 (d14-1900 #228, #231). The sink is there. A level costs 1.15× the
last and makes 1.10× more, doubling at 10, 25, 50, 75 and 100, so upgrades pay back in 1–2.5 h at the full price up to
the Hub Order (4,000 energy/min with quiet grace). But the next steps are sized to income, not to the bank.

**5. Max bought 25 levels that did nothing.** At level 35 the Lab's multiplier is 1.1³⁴ × 2² = 102. It needs 1,022
energy and 204 ingots and would make 511 points a minute at full speed. It got 6.1 ingots, so it ran at 3% and made
15.3 points a minute. That is correct arithmetic, not the bug the analyst suspected. Level 10 at 65% would have made the
same 15.3 points for 33.6k, and level 9 at 100% 10.7 points for 27.5k. Max +34 cost 1.53M, so 98% of it bought nothing.
It also created the Yard drain (item 1).

**6. Research points are worthless because only one tech can run at a time and sessions are days apart.** Era 1 costs
3,500 points for 12 techs (plus 5,000 for the Deep Lift), Era 2 41.5k and Era 3 104k. Cleo's 179k on day 14 would have
paid for Eras 1–3. Yet in 21 days she started five techs, one a session. `StartResearch` refuses while one runs
(`GameWorld.Research.cs:64`). Ana had 0 points, because a Lab needs ingots and her only ingot source was the idle
factory.

**7. Mia and week-3 growth can't be judged from this run.** Mia ended at 101 g/min against Dev's 152 and Ana's 187, but
the crew's trade paid nothing, so the gap is effort: per building 14.4 against Dev's 15.2 (95%); per 100 commands 69
against Dev's 78 (88%) and Ana's 64 (108%). From day 14 to 21 the crew moved −8%, +2% and +5%; the typical bot −2%
(3,618 → 3,549). Nobody built anything: Ana had 9 buildings from day 5 to day 36.

## Root causes

- **The Yard counts lacks wrongly** (checked, `GameWorld.Yards.cs:341–366`). `Measure` takes a building's scarcest
  input and counts it as lacking its full need, even when another input caps the building, and even when its own
  network sells that good to the hub. The solver gives a capped building exactly what it can use, so all its input
  fractions come out almost equal, and the minimum picks noise. For the Lab, energy was 30 of 1,022 (0.0294) and ingots
  6.1 of 204 (0.0299). Energy was the "scarcest" by 0.0005, so the Lab "lacked" 1,022 × 0.97 ≈ 992 energy, plus what
  it took. That is the "Lacks 1,082" on the panel (cleo d14-1900 #239), next to 395 spare energy in the same network.
- **The Yard pays only for what is taken** (checked). A seller is paid for what the buyer's buildings take
  (`Yards.cs:209–218`), but its network already gave those goods to the gate instead of the hub (`SplitGateFlows`,
  `:131–134`). The comment says "goods nobody takes are lost on the way (the next round sends less)" (`:20–26`), but
  the next round never sends less: the lack is measured from the full need every round. The design said "a seller is
  paid for what reaches the Yard" and unneeded spare "goes on down your flow order (… the hub)" (§7). The code drifted.
- **The project is sized from a fixed table** (checked). T(L) never changes (`progression.yaml:73`,
  `GameWorld.Projects.cs:128`). To count as "delivering", a resident needs 30 minutes of T(L) in credit
  (`Projects.cs:370`), which is 23k at Level 5, against a Foundation worth 1,050 g in all. So nobody ever counted, and
  every basket was sized for one resident, the one with the highest level (`:222–227`). The goods come from the seats'
  land types (`:156–180`). The project takes what is left ÷ 60 per minute and a good counts as done at 10⁻⁶ (`:293`,
  `:381`). Each resident's allowance uses T(L) (`:330`), though the design says "2 h of your output" (§12).
- **The gold glut has three causes.** The advisor counts 10 minutes of income as a step's effort
  (`GameWorld.Advisor.cs:140`) and offers +1 level or the next milestone within 15 levels (`:814`), so Cleo, with
  1.6M, was offered "+16 g/min for 353 g". Max spends all the gold without asking whether the levels are any use
  (`GameWorld.cs:915`). And the stalled middle (a factory whose goods can't leave) took the non-optimisers' attention.
- **Research** runs one tech at a time, and sessions come every 2–7 days, so a player gets one tech per session. Points
  never limit anyone who has a Lab, and they block everyone who doesn't.

## What I propose

**1. The Yard matches only what raises output, and a seller never does worse than the hub.** *Effort M. Do it first.*

- **A · Own spare first.** A building never lacks a good its own gate network sells to the hub or puts in storage.
- **B · Only what it can use.** A building lacks input r at most `need_r × f − got_r`, where f is the lowest, over its
  other inputs q, of `min(1, (got_q + network spare_q + Yard spare_q) ÷ need_q)`. When that is 0 or less, it lacks
  nothing.
- **C · Feedback.** A buyer that took under 80% of what it was matched last round is matched at most 1.25× what it
  took.
- **D · Never worse than the hub.** What a seller sent to its gate and nobody took is paid at the seller's own hub
  price, counted in its Hub Order. The buyer pays 1.75 × base only for what it takes. This gold comes from the hub, as
  it always would have, never from a player.

*Worked, the Lab on day 6:* energy has 395 spare in its own network, so by A it lacks no energy. For ingots,
f = (30 + 395) ÷ 1,022 = 0.416, so by B it lacks at most 204 × 0.416 − 6.1 = 79 ingots/min. Nobody had spare ingots,
so nothing is matched. Ana, Ben and Dev keep selling their 361 energy to the hub, and the 3.5M stays theirs. Cleo's
row after day 14 ("504 spare, lacks 5.2 energy") becomes no lack at all. Rule D alone would also have paid the three
sellers about 320 g/min all along.

*Risk:* a factory short of two inputs at once is served only when both show up as spare somewhere; B uses the Yard's
spare for that. Houses' lacks change in the same way, and the Houses' 25% cap still applies.

*Verify:* a test `YardNeverCostsASeller` (a level-35 Lab next door doesn't change A's gold rate by more than 1%), a test
`OwnSpareIsNeverALack`, and a conservation test (gold paid by player buyers = gold received by player sellers at 1.75).
Replay from the row07 snapshot and log the **paid** Yard gold apart from what was matched.

**2. A project the station can reach, sized by what it makes, with no tail.** *Effort M (a and b are S).*

- **a · Foundation:** energy only, 900 energy. Ana alone, with about 100 spare a minute from 20:07, finishes it in 9
  minutes. With Ben and Cleo from 20:09 it stands about 20:13. Mia alone, after the Houses' 25%, needs about 10
  minutes. That meets "minute 20" in both stories.
- **b · No tail:** the project takes everything offered, up to what is left, shared in proportion to the offers (the
  allowance does the pacing). A good is done at 99.5% or with less than 1 unit left. The 150 iron ore then takes about
  10 minutes of one mine instead of 19 hours.
- **c · Recipe:** two goods active residents make now, preferring goods other than energy. The keystone is a good
  nobody makes but some active resident can make with **one building** that their level unlocks and that fits their
  land (`FindSpot`). If no such good exists, the stage has no keystone.
- **d · Size:** each made good takes 4 h of the station's spare of it (at least 4 h of one level-1 building). The keystone
  takes 6 h of one level-1 building: sand 5,400, stone 7,200, copper ore 3,600, wood 3,600. The Workshop uses the same
  rule with 6 h, and the Foundry Hall with 9 h. Baskets only shrink, never grow: when a resident whose spare was counted
  goes three days without a session, their part comes off what is left.
- **e · Measured income:** I(c) is a company's gold a minute from its production, averaged over 24 h (at least 60).
  It replaces T(L) for the allowance (2 h of I(c) a day, banked 3 days, as §12 says), for the "delivering" threshold,
  for the Landmark and for Wonder stages. T(L) stays only where it sets a gold price: trunk segments, which are a sink.
- **f · Valve:** after 72 h open, any resident may buy the rest of the keystone from the hub at 2.5 × base. It gives no
  credit and shows grey on the ring.

*Worked, the crew's Water Tower on day 3 at 14:18 (spare estimated from hub sales of 192, 276 and 85 g/min):* iron ore
and energy, keystone sand (Ben: Level 5, beach; Cleo's copper or coal would also do). Basket: 4,800 iron ore (14.4k g),
96k energy (96k g), 5,400 sand (6.5k g): about 117k g instead of 329k, every unit in reach. Allowances (Ana 23k, Cleo
33k, Ben 10k a day) fill the made goods in about 1.7 days; one dredger fills the sand in 6 h. Ben stops on day 3: from
day 6 his share of the energy comes off, and if nobody makes sand by day 9 the valve sells the rest for 16.2k g.

*Risk:* `FindSpot` runs once per resident and good at a stage's start (at most 4 × 8 calls). Making less to shrink a
basket gains nothing worth having.

*Verify:* a test `FoundationStandsInMinutes` (900 energy from one starter kit in under 15 game minutes), a test
`EveryRecipeGoodIsReachable`, and a test `NoStageTail`. Replay row01: the Foundation by 20:20, Mia's included. Replay
row07: the Water Tower by day 8, and the stage reached by day 14 and day 21.

**3. Max buys only levels the building can use; the advice grows with the bank.** *Effort S (Max) + M (Invest).*

- **Smart Max:** buy levels while the building would still run at 90% or more with what it can get (got + network spare
  + Yard spare) and, for buildings that sell, while each level pays back within 4 hours at today's prices. Show
  "Max +8 · 27.5k" and, behind a confirm, "+34 · 1.53M · would run at 3%". For a producer, Max stops where the trading
  post is full, and the next step says to upgrade the trading post.
- **Advisor:** effort = max(200, 10 min of income, 2% of the bank) (`Advisor.cs:140`), so with a big bank the steps rank
  by what they bring, not by what they cost. `UpgradeSteps` may also offer a smart-Max level, not only +1 or the next
  milestone.
- **Invest:** when the bank holds 12 h of income or more, one step spends up to half of it on the best smart-Max levels
  (`UpgradeMany` with a budget already exists, `GameWorld.cs:990`), with the total gain and the payback.

*Worked, Cleo on day 14 (4.8M, 477 g/min):* a Wind Moily from level 10 to 30 costs 72.1k for +1,761 energy/min. Her
trading post from level 1 to 10 costs 42.0k, so it can sell 2,830 a minute. Together that is about 114k, 2.4% of her
bank, for about +1,760 g/min at the full price. It pays back in about 65 minutes, her income goes ×3.3, and her next
level comes in about 12 h instead of 41 h (#228).

*Risk:* incomes climb toward the bot's curve and the Hub Order's √ starts to bite. The levels were tuned on that curve,
and the baskets now follow measured output (2e), so they keep up.

*Verify:* a test `MaxStopsAtUsefulLevel` (a level-1 Lab with 6.1 ingots: Max goes to level 9 for 27.5k). From metrics:
the median share of the bank spent per session goes from under 1% to 30% or more, and the bank at the end of a session
is no more than 1 day of income.

**4. Research: a queue, and points for sale at the hub.** *Effort S.*

- A queue of up to five techs that run one after another, while away too. Points are paid as each one starts.
- "Need N more" can buy the missing points for the tech you start, at 25 × 4^(era − 1) g a point. For Ana on day 14:
  Copper Pipes 5,000 g, Valves 7,500 g. All of Era 1 costs 212k, all of Era 2 4.15M, all of Era 3 41.6M: a real sink at
  the size of these banks. Labs stay 3–30× cheaper (a level-1 Lab: 7.6 g a point; a Research Institute: 3.1 g).
- Points can't be given to anyone. The Lab's 3% stays as it is (item 5 of What I see).

*Verify:* a player without a Lab starts Copper Pipes on day 14. Cleo queues three or more techs in a session. Her bank of
points on day 21 is no more than the next era's cost (41.5k).

**5. Keep the prices; measure parity and growth with equal players.** *Effort S.*

- The prices stay as they are: 1.75 between players, Houses buy at 1.25 and sell at 2.0, the 25% cap. A solo player
  makes 0.75 + 0.25 × 1.25 = 1.0625 × the hub. A crewed player trading a share t of their output makes 1 + 0.75t. So
  parity is 95% at t = 15%, 90% at 25% and 87% at 30%: the 85–90% promise holds when the crew trades 25–30%. Measure t.
- Week 3, once 1 and 2 work: the Foundry Hall's +10% on two chains (energy and iron ore are about 90% of this crew's
  output) gives about +9%. The Yard at t = 15% adds about +11%. Together that is about +20%. Without trade it is +9%, a
  miss. Whether the +15% promise holds depends on trade, not on Discoveries.

*Verify:* a balance run of a solo bot in a House station against a crew of four of the same bots: 85–90%. In the
replay, compare per building and per command, never raw income between players who put in different effort.

## What I'd not do

- **Divide T(L) by five.** A table can't know who plays, and the next crew will differ again. Size by what the station
  makes.
- **Charge the buyer for everything matched** (the literal "paid for what reaches the Yard"). With the Lab's bad lack,
  Cleo would have paid 361 × 1.75 = 632 g/min for nothing, 7.3M over eight days. Let the hub take what nobody uses.
- **New gold sinks** (cosmetics, early parcels, lotteries). The best sink pays back in an hour; players don't see it.
- **Cap Max at a share of the bank.** 97% of the bank on producers would have tripled Cleo's income. Cap by usefulness.
- **Gold or point gifts between friends.** §8 says gold never moves directly between players, and an alt could farm.
- **Make research 10× dearer** so the points "matter". It punishes Ana; the queue makes the points matter.
- **Change prices or Houses for Mia's 66%.** That gap is effort plus the bug.
- **Drop the keystone.** Keep the shared decision, and make the keystone buildable.
- **Retune XP now.** The XP wall follows income, and investing moves both.

## Questions for others

- **engineer:** (1) Is Mia's unpaid "31 g/min to Houses" the same `Measure` flaw inside a House's works, or something
  on the House side? (2) Please confirm the 18-hour tail: what is left ÷ 60 per minute, and done at 10⁻⁶ units. (3) Can
  rule B read the Yard's spare per good cheaply? `MatchYard` already loops over goods. (4) Where should rule D settle:
  on the seller's side or in `SettleGateFlows`?
- **clarity:** the smart-Max text and its confirm, the Invest step, a line for paid Yard income, and the stage bonus in
  g/min. Cleo couldn't price "+3%" (cleo d21-1900 #303).
- **together:** is 72 hours right for the keystone valve, or does the valve take away the crew's "who brings it"?
- **researcher:** please add to the metrics the paid Yard gold per player (apart from what was matched), the share of
  the bank spent per session, the stage reached by day, the share of output traded t, and income per command.
