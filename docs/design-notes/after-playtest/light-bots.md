# The light bots: the price on dealt land (step 5)

`TransportPipes.Game.Tests/LightBotSimulation.cs`, run with `TP_BALANCE=1` (optionally `TP_BALANCE_PRICE`,
`TP_BALANCE_DAYS`).
- **Who plays:** six light bots on dealt land in one small-mode world. A crew of four (Ana, Ben, Cleo, Dev) shares a
  corner station; Ben stops on day 3 and Dev arrives on day 8. Mia lives in the small station next door, and Jo alone
  in another corner station.
- **How they play:** 20 minutes every second evening, at most 15 buildings, always the advisor's first affordable step.
- **Seed:** 4242.
- **Seats:** Ana wood + coal, Ben stone, Cleo copper, Dev sand, Mia copper + sand + stone, Jo copper.

## 36 days at three prices (2026-10-22)

| Mark (plan §7) | Pass | 1.30 | 1.50 | 1.75 |
| --- | --- | --- | --- | --- |
| Paid trade between humans by day 14 | ≥ 15% of the crew's output | 1.8% | 2.0% | 2.4% |
| Buildings running at a loss on their supply | 0 minutes | 9,600 | 13,920 | 83,040 |
| Solo against crew, per building (days 10 on) | 85–92% | 107% | 107% | 103% |
| Pacing: Level 10 | days 4–6 | day 4 (Dev day 10) | day 4 | day 4 |
| New gold from NPCs | ≤ 6% of faucets | 0.1% | 0.1% | 0.1% |
| Ben's stop: Ana's chains get their need | ≥ 99% | 100% | 100% | 100% |

Losses are building-minutes, counted at list prices: what a processor's goods are worth against what its supply cost.

## The decision: 1.3

1.3 is the only price at which a factory that buys its ore from a neighbour doesn't lose money:
- **At 1.75:** Cleo's Small Factories bought iron ore from the crew and lost on every unit (83k building-minutes).
- **At 1.5:** fewer of them lost (14k).
- **At 1.3:** what was left was Mia's iron bought from Houses at 2.0. A House now sells a player a good only where
  the building that uses it still pays at the House's price.

The price doesn't move the other marks, so 1.3 it is (content: `together.playerPrice`).

## What the bots found: iron decides everything

Paid trade between friends stays near 2% at every price, and Jo alone earns as much per building as the crew. The
cause is in the content, not the price:
- **Iron is on every estate**, as the plan wants for the first hour, and its chain is the best of Era 1. A Small
  Factory makes about +28 g a minute at the base prices. The chains that need a neighbour make less with one input
  bought at 1.3:

  | Chain | g a minute |
  | --- | --- |
  | Smelter | about +22 |
  | Glassworks | about +22 |
  | Brickworks | about +9 |
  | Sawmill | about +3 |

- **The light bots fill their 15 buildings by day 3,** with Small Factories at Level 2. The chains that need a
  neighbour unlock at Levels 6–7, after every slot is taken.

Two price experiments at 1.3 over 14 days:

| Variant | Trade by day 14 | Losses | Solo / crew | Buildings |
| --- | --- | --- | --- | --- |
| Today | 1.8% | 9,600 | 107% | the same |
| A: iron ingots 11.5, bricks 6, planks 4.5 | 10.1% | 27,840 | 99% | Ana builds a Coal Pit |
| B: copper ingots 20, bricks 7, glass 16, planks 5.5 | 2.0% | 1,920 | 103% | exactly as today |

- **Variant A** raised the trade, but most of it was iron ore and energy, and factories buying both lost money.
- **Variant B** changed nothing, because no slot is free when those chains unlock.

Neither ships.

**For run 2:** the AI players aren't light bots. They have Send, bids, the Build-menu line, the neighbour-fed chains
of the advisor and Loady's dealt-land steps. Run 2 measures what people do with them. If trade between friends still
stays low, the next lever is in the content, and it is a designer's decision for the whole team:
- iron ingots that need coal, the one good every station has once;
- or a smaller Hub Order for ingots, so a fifth factory pays less than a first Smelter.

## After run 2: the two levers (2026-10-22)

Run 2's analyst asked to try the levers above in a bot run before run 3 (recommendation 7). 14 days at 1.3, on the
build after run 2's fixes (`TP_BALANCE_LEVER`):

| Variant | Trade by day 14 | Losses | Solo / crew | What the bots built |
| --- | --- | --- | --- | --- |
| Today | 0.7% | 2,640 | 101% | as before: Small Factories fill the slots |
| `coal`: iron ingots need 4 coal a minute (a Big Factory a third of its ore) | 2.2% | 0 | 73% | no Small Factories: 8 Lumber Camps (Ana), 8 Water Pumps (Cleo), Sand Dredgers; incomes a third lower |
| `order`: the ingots' Hub Order halved | 1.5% | 2,400 | 83% | the same factories; incomes a quarter lower |

Neither ships. When the best chain needs a neighbour, the bots don't buy from one: they build the next self-sufficient
producer, whose goods the hub buys. Trade between friends won't come from the iron chain alone. It needs chains that
need a neighbour *and* beat a raw producer at the hub, or a hub that pays less for raw goods past a point. That is the
team's decision for run 3, together with run 2's finding that the real flows between friends (Ben's sand, Ana's
ingots) did happen and weren't felt as gifts. Run 2's trade figures were also measured with every trade between
players at the hub's price (the playtest's one-device artefact, fixed since), so they under-count the gold.
