# Round B · psychologist (after run 4)

All six of us read run 4 the same way: doors too small, too capped, in the wrong place, and nothing shared after day 17.
We differ on what goes after the Landmark and how gold gets in. Below I settle those, mostly by taking others' parts.

## 1 · What I take, and what I drop

**I take:**
- **Economy: why Next steps never offered the pipe.** When the pipe's ingots aren't in Storage, the advisor asks
  `SupplyStep`, which returns nothing if you already make them (`GameWorld.Advisor.cs`:515–523, 548). Cleo made 237k
  copper ingots and sold every one. Also economy's light-bot split: the copper-ore seats banked 2.7–5.1 days, the
  others 10.8–18.9 (`light-bots-day28.md`). So the gap was the road to ingots. From economy too: "near" means a resident
  seen in 7 days whose buildings made the good in the last 24 h, or who holds it; resize upward until a quarter has
  arrived; the bank in days on the top bar.
- **Economy: one button that goes all the way:** research, then the tier, then Max, with one preview.
- **Friends: chip in for 1 h, 3 h or 1 day of your own income,** any number of times, givers named in hours, *"from
  next door"* for neighbours. A tier has a **pipes part** and a **purse**, and its keystone starts when the tier opens.
  Shared doors get a minimum rank in Next steps once the bank passes a day (`Advisor.cs`:136–150).
- **Outsider: the growth line never folds,** and no new nouns. *"Order the rest"* was folded away the only time it
  was offered (ana d27 #308).
- **Lonely: one bundled buy,** *"Grow everything that pays · 4.1M of 13.4M · +420 g/min ▸"*, with a preview and Undo.
  It is the genre's "buy max", and Ben's return was a month of income met by a 5-minute phone session (3.4k spent,
  ben d36 #77–#82). Lonely also gets the district channel first, and gifts named at both ends, strangers included.
- **Wild: growth came free,** so a door has to beat the free sources, and the bots are stuck at the pipe too (Ana's
  bot: 18.9 days).

**I drop of my own:**
- **"Rings":** "storeys" is the word three of us used, and outsider is right that we need no new noun.
- **My linear curve** (ring *n* = *n* days). Ring 3 would be 22.8M for Capital SE, and friends' honest walk-through
  shows a crew giving about 9M in three weeks. A goal people can't see the end of is a treadmill.
- **Full credit for bought goods in a shared goal.** I keep 25% there (a friend's bricks count four times), but keep
  the name and the 💛 for gold given to a friend's own building.

## 2 · What I reject, and why

- **Wild's Works** (a station Brickworks at 1 day of income, 5.5M). The same building costs 1,400 g on an estate
  (`buildings.yaml`:281). The crew compares prices (Cleo checked ten previews; she and Ana traded Exchange and Yard
  prices in the chat), and a building priced at 4,000 times its list price reads as a bad deal. It also takes away the
  maker's role the chain pointer gives Dev, and at 5–7 days it is the biggest build. Its true part, that someone near
  must make the missing good, is met by the chain pointer plus the order.
- **Goods-only tiers** (economy's 6 h × 2ⁿ⁻¹, outsider's ×1.5 floors), where gold gets in only by ordering goods nobody
  near makes. Then the crew's gold can't touch the goods the station *does* make, which is how the Dynamo felt: Ana
  wrote *"only my name on the list"* (ana d36 #354) and asked *"Can you each order the other half?"* (group, day 27).
  Ben asked for *"the button"*. A purse is that button.
- **Doubling tiers** (lonely's 1, 2, 4, 8 days): tier 4 is about 60M for Capital SE, weeks of the giving we can expect.
- **Grey Widen** (wild, outsider). The bot split says the missing piece was the ingots, not grey Widen. Once ingots can
  be ordered when nobody near makes them (2 × 18 g each), copper is open to every seat. Grey ×4 (400 a minute) beside
  copper (500) puts two near-equal doors on one panel. Widen stays where it is built: after copper.
- **2.5 × base** (wild): a second number to learn, with no evidence that price protects anyone at these banks. The
  built 2.0 (`GameWorld.WantOrders.cs`:16) stays.
- **No 💛 for an order bought for a friend** (lonely). Dev asked *"tell me how to be useful"*. Buying a friend's bricks
  *is* help, and the thanks at both ends was the loop that worked (Ana's copper, Cleo's Smelter).

## 3 · The open questions, final

| Question | Final |
| --- | --- |
| After the Landmark | **Storeys**: a pipes part (9 h of the station's spare in its residents' land goods, plus a two-friend keystone where a lit pair exists, chosen when the storey opens) and a **purse** (1 day of the station's 7-day income × 1.25 a storey). Perk: the station's pipes carry +10%, named like a Refinement. A new storey you can see. The next opens the day after one stands, so the plaque is seen first |
| Gold in a shared goal | **Chip in** 1 h, 3 h or 1 day of your income, as often as you like, named in hours. **Nobody pays more than half a purse until it has been open 3 days**: Cleo held 38.2M on day 17 and offered 15M on day 10, and one tap of hers would have ended the goal for everyone else. After 3 days anyone may finish it, so Mia beside Jo isn't stuck |
| Price and credit | **2.0 × base.** Ordered goods count 25% in a shared goal. Gold for a friend's building is named with a 💛. Purse gold is named in hours |
| Widen | **Copper first** (one tap, ingots from your Storage, a friend, or ordered), **then Widen** on copper, as built. Grey Widen only if the bot run fails (below) |
| One door or one plan | **Both, in one list of three.** Tapping the top bar (*"40.6M · 24 days ▸"*) opens: the first shared door, a friend's waiting want, then lonely's bundle or the single best pipe door |
| The player alone | **District channel first, now** (Mia's greetings went unanswered until day 15; director rows 1, 5, 11). **The branch later**, only if Mia's alive mark fails again with the channel open |
| Before run 5 | **One light-bot run** on the built plan, the advisor offering the pipe door, ingot orders and chip-ins. Pass: every seat ≤ 5 days banked on day 28, and Level 30 on day 25 ± 2. If the non-copper seats still bank over 10 days, open grey Widen before run 5. Plus the analyst's replay of row 12 from the day-21 snapshot with the Brickworks pointer |

## 4 · The solution I'd sign, in build order

| # | Mechanism and numbers | Replaces | Days |
| --- | --- | --- | --- |
| 1 | **Sizing.** Count residents seen in 7 days (`ActivePeriod`), on their 7-day income. Resize daily, upward only, until a quarter has arrived. Never use the 60 g/min floor while anyone in the district has played in 7 days | The 3-day count (`GameWorld.Wonders.cs`:139–142); the freeze at the first delivery (`GameWorld.EraWant.cs`:228–235) | ½ |
| 2 | **The pipe door.** At a full pipe the upgrade panel offers one button: research → Copper Pipe on the full links → Max to the new limit, with one preview. Ingots from Storage, a friend's (named), or ordered when nobody near makes them; then Widen. The advisor's dead ends lead to the want or the research | *"+N would make nothing more"* (`BuildingPanel.tsx`:856); a fix shown only in the idle status (`GameWorld.IdleTexts.cs`:145–162) | 2 |
| 3 | **One order rule.** A good nobody near makes: order all of it, at once, at 2.0 ×. With a maker near: their window, then half by pipe (shared) or the flow (private). The button never vanishes and says why, with the chain line (*"a Brickworks on your estate makes bricks from Ana's stone and Cleo's coal; the Yard brings both · 1,400 g · Show me"*), offered first to the friend with gold and no role. A waiting building says *"waits for"*; the Exchange shows its cap | The half cap where nobody makes the good; *"is being built"*; the silent Exchange cap (cleo d14 #328) | 1½ |
| 4 | **Storeys** (§3), with chip-in, the 3-day half and a plaque named in hours. Capital SE: purses of about 6.1M (day 17), 7.9M, 11M and 15M, about 40M in all, a quarter of the 160M the crew earned on days 14–36. Mia and Jo: 1.8M, 2.3M, 2.8M | *"A new parcel: Level 7 · 10.0k g"* (ana d36 #375) | 3 + 1–2 art (`process.mjs`, `fit.mjs`) |
| 5 | **Where people look.** The top bar's list of three, the same list in Next steps (shared doors ranked once the bank passes a day), and a card line that never folds: the first shared door, then the plan | The bare gold number; the folded order | 1½ |
| 6 | **Alone.** District channel first; gifts that stand a stage named at both ends, strangers included | Mia's empty station channel; Mia-B's unthanked wood | 1 |

**About 9½–10 days plus art**, each step playable. Steps 1–3 come before 4, because a storey built on a broken size or
a vanishing button would teach the run-4 lesson again.

## 5 · The mark for run 5

**From day 14 to 36 the crew spends at least 20% of what it earns, and at least three of the four crew give gold to a
shared build** (a storey's purse, the Era wonder, or a friend's waiting building). Run 4: 0.2%, and two players gave.
- If spending stays under 10% with these doors in sight, the solution is wrong: these players don't want what gold
  buys, and gold needs another job.
- If spending passes 20% but fewer than three give, the private door was enough, and the purse should go.
