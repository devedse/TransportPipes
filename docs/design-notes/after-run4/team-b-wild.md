# Round B · wild

All six papers agree the doors were too small, capped at the wall and hidden from the upgrade panel. We differ on
what follows the Landmark and how gold enters a shared goal. I give up most of my round A to get one plan.

## 1 · What I take, and what I drop of my own

**I drop the Works.** Station-owned machines are a new kind of owner, cost 5–7 days, and five of six papers chose a
Landmark that grows. The chain line does the Works' job better: it gives the maker's role to a *player*. The player is
Dev, who asked for it twice:
- *"tell me how to be useful"* (dev d21 #146);
- *"Bricks need stone + coal, I have neither"* (dev d14 #116–#117).

**I also drop:**
- my 2.5× price, because where gold finishes, there is no maker to protect;
- "the wonder's order half stays half", for the same reason;
- my "copper on every full link" button, because lonely's plan contains it;
- my mark's split.

**I take:**
- **From economy:**
  - why Cleo never saw copper: the advisor goes silent at the pipe (`Advisor.cs:503`, `:515–523`, `:548`). She made
    237k ingots, and her Next steps (#242–#266) never named the pipe;
  - "near" as a resident seen in the last 7 days who made the good in the last 24 h or holds it;
  - resizing upward until a quarter has arrived;
  - a tier perk on pipes.
- **From friends:**
  - chip in 1 h · 3 h · 1 day of *your* income, again and again, with givers named in hours;
  - the tier as a pipes part plus a purse, about one a week;
  - "waits for", in place of "is being built";
  - their honest arithmetic: chip-ins alone reach about 10%, because Cleo and Dev stop after day 21.
- **From lonely:** *"Grow everything that pays"* in one tap with Undo (people don't walk chains; bots do: Cleo's bot
  took an Iron Mine to level 69), the district channel first, and gifts named at both ends.
- **From outsider:** the friend's copper and Widen side by side on the panel, Widen priced per link, a card growth
  line that never folds, and the Level 30 check.
- **From psychologist:**
  - shared first. The only sink that took gold was shared and in sight: all 30,660 g went to wonder orders (Cleo 9,000,
    Ana 6,660 + 15,000);
  - credit in hours of the giver's income;
  - a 💛 for gold given to a friend's waiting building.

## 2 · What I reject, and why

1. **Half per person until a ring has waited 3 days (psychologist).** Every cap in run 4 ended as a dead button:
   - `WonderOrderOffer` returns null at half (`EraWant.cs:190–199`);
   - Ben found *"only bars"* (ben d36 #89–#93);
   - Ana: *"the Cathedral cannot finish without the other half and I can't make it happen"* (ana d36).

   No cap is needed to make the goal two-person. The tier's goods need every seat's land good, and they fill from
   absent friends' spare (`Projects.cs:125–130`).
2. **Doubling tiers (lonely, economy) and linear rings.** After day 21 only Ana and Ben played.
   - Lonely's tier 3 is 4 days of Capital SE's income, about 30M: 18 days of Ana's 1,154 g/min.
   - The psychologist's ring 3 is 22.8M.
   - A stalled goal was run 4's worst moment. The Dynamo sat at 89% from day 6 to day 22, and Ana wrote *"I feel I'm
     the only one pushing"* (ana d27).

   Let a tier grow with the income it is measured on, not with a steep curve.
3. **Tiers with no purse (economy).** There, gold enters only by ordering the one good the station doesn't make. Once
   Dev's Brickworks runs, that good is made and the door shuts: run 4's half-cap again. Yet gold is what friends tried
   to give: Dev's three offers, and Ana's *"nice that I can chip in without building"* (ana d27 #316).
4. **"None could make it with one building" (outsider).** Anyone in Capital SE could have built a Brickworks, and the
   Dynamo still waited 16 days. Under that clause, bricks are never orderable.
5. **"Lit" (3 days) as "near", and an Era stage of a day (lonely).** The 3-day window *was* the sizing bug: the
   Cathedral fell to the floor after everyone had been away 3 days (`Market.cs:202–204`). The stage has a deadline,
   and Era 1 ended at 89%. Six hours, sized right, is already 1.6M, 75 times run 4's 21.6k.
6. **"Copper first, then Widen" (economy).** That is today's rule: `Widens(tier) => tier is 2 or 3`, with grey closed
   *"so the first door past 100 a minute stays a friend's copper"*. Run 4 tested it: zero copper tiles, zero widenings.
   On day 28 the bots without copper ore banked 10.8–18.9 days, and those with it 2.7–5.1 (`light-bots-day28.md`).

## 3 · The open questions, final

| Question | Ruling |
| --- | --- |
| After the Landmark | **Tiers:** the goods of a Landmark stage as built, plus a purse of 1 day of the station's income × 1.2 a tier; at most one a week |
| Gold in a shared goal | **Chip in** 1 h · 3 h · 1 day of your own income, any number of times, no cap per person, named in hours |
| Price and credit | **2.0× base everywhere** (`WantOrders.cs`). In the wonder, pipe goods keep counting 4 times ordered goods (`EraWant.cs:16`). Gold for a friend's waiting building gets a 💛 and a name |
| Widen | **Grey too:** ×2 for 1 h of your income, ×4 for 2 h more, cap ×4 (400 a minute, below Copper's 500). **Priced per link.** `WidenCost` charges once per call, so one tap would widen a whole estate for 3 h of income |
| One door or one plan | **Both.** The gold door has three lines: the shared goal, the plan, and the best single buy |
| The player alone | **The channel is in; the branch is out** for run 5. Run 3's Mias met on day 1, run 4's on day 15, through the wrong channel (director's observations, row 1). Fix the cheap cause first |
| Before run 5 | **One light-bot run with steps 1–5 on**, ingots orderable included (the gates below) |

## 4 · The solution I'd sign, in build order

Every step stays playable.

**1. Sizing (½ day).**
- Shared goals count residents seen in the last 7 days.
- No goal is sized from the floor while anyone in the district has played in the last 7 days. The floor is 60 g/min
  for each seat taken.
- A goal is resized daily, upward only, until a quarter of it has arrived.
- An Era stage stays at 6 h of the Capital's income: about 1.6M of goods on run 4's numbers.

*Replaces* the 3-day `IsActive` in `StageRequirements`, and the freeze at the first delivery (`EraWant.cs:230`).

**2. The door at the pipe (2½ days).** The panel says:

> *"Its pipe carries 205 a minute · [Copper Pipe · 18 tiles · 180 g + 18 ingots · then Max to 30 · +N g/min ▸] ·
> [Widen ×2 · 1 h of your income ▸]"*

- Without ingots, the pipe becomes a want, or an order when nobody near makes them.
- The advisor's dead ends point to the research, the Lab, the want or the order.

*Replaces* "would make nothing more", the grey refusal, and a door that showed only in the building's idle status.

**3. One order rule, with words (2 days).**
- **Nobody near makes the good:** order all of it, with no window, at 2.0× base, shipped over 6 h.
- **Someone near makes it:** their window (72 h at most), then the flow, as built. The wonder's pipe half stays.
- **Every line names the wait and the role:** *"Waits for 100 bricks · nobody near makes them · Order · 1,000 g ▸ · or
  a Brickworks on your estate (Ana's stone, Cleo's coal) · Show me"*. The role is offered first to the friend with gold
  and no good of their own in demand.
- **Buttons never vanish.** When one can't act, it says why.

*Replaces* the half-cap where no maker exists, the null offer, and "is being built".

**4. The gold door (2½ days).** The top bar reads *"40.6M · 24 days ▸"*, with a dot when the bank holds more than a
day of income. Tapping it shows three lines:
1. the open shared goal, with the chip-in buttons;
2. *"Grow everything that pays · 4.1M of 13.4M · +420 g/min · back in 7 days"*: every step that pays back within 7
   days, in one preview and one tap, with Undo for an hour;
3. the best single buy.

The card's first growth line is line 1, and it never folds. In Next steps the shared step ranks high whenever the bank
holds more than a day of income.

*Replaces* the bare gold number and the folded Order line.

**5. Landmark tiers (3 days, plus 1–2 days of storey art through `process.mjs` and `fit.mjs`).**
- **Goods:** a Landmark stage as built: 9 h of the station's spare in its residents' land goods, with the keystone's
  2-day pass-over.
- **Purse:** 1 day × 1.2ⁿ⁻¹ of the summed 7-day income of residents seen in the last 7 days, fixed when the tier opens.
- **Who gives:** anyone in the district, *"from next door"* included.
- **What it gives:** a storey, a plaque in hours, and +10% on the residents' pipes a tier, capped at +50%.
- **On run 4's numbers:** Capital SE's tier 1 is about 5.7M (Ana 663, Cleo 2,460 and Dev 806 g/min on day 17), and
  Capital NW's about 1.7M.

*Replaces* *"A new parcel: Level 7 · 10.0k g"* (ana d36 #375).

**6. The player alone (1 day).**
- With no other human chatting at her station, chat opens on the district channel.
- Gifts are named at both ends.

**Total: about 12 days, plus art.**

**Gates before run 5.** One light-bot run with steps 1–5 on, in which the bots press the shared step when it ranks.
- **If Level 30 comes before day 23**, grey Widen stops at ×2 and the tier perk drops to +5%.
- **If any bot holds more than 10 days of income on day 28**, a door is missing for that seat. Find it before people
  play.

## 5 · The mark for run 5

> **From day 14 to day 36 the crew spends at least 20% of what it earns, and at least three of the four crew each
> spend at least 10% of theirs.**

The second half keeps one whale from passing it alone. If it fails while the doors were seen (logged), the plan is
wrong; the split of the spending, Ben's day-36 session and the pipe door's presses tell us which part.
