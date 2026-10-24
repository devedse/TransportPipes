# Round B · lonely

My players come least: **Ben** (away from day 3 to day 36, on a phone) and **Mia** (beside Jo, a session a week). I
read the six papers asking what each shows someone who returns with days of income, and whether any of it waits on
someone who isn't there.

## 1 · What I take, and what I drop of my own

**Taken:**
- **Economy's pipe door that goes the whole way:** research, then the tier (as a want if the ingots aren't in hand),
  then Max to the new limit, all in one preview. Also its finding that the advisor's pipe step dies in three places:
  - when the research can't be paid yet (`GameWorld.Advisor.cs:500–509`);
  - when the ingots are missing (`:517–523`);
  - when you already make them, because `SupplyStep` returns null (`:548`).

  The bots split exactly on this: the seats with a Copper Mine banked 2.7, 3.2 and 5.1 days on day 28 (Cleo, Mia,
  Jo), those without 18.9, 16.8 and 10.8 (Ana, Dev, Ben) (`light-bots-day28.md`). Ben has no copper.
- **Economy's "near"** (a resident seen in 7 days who made the good in the last 24 hours, or holds it), and its
  resize upward until a quarter has arrived.
- **Friends' floor per seat** (60 g/min for each seat taken), and **chip-in** in 1 h, 3 h or 1 day of your own income,
  named in hours. Ben's *"what do i order for the cathedral, just tell me the button"* (group, day 36 20:03) asks for
  exactly that line.
- **Psychologist's** credit in hours of your own income, so Ben's hour stands beside Cleo's day.
- **Outsider's rule:** the return card's growth line never folds and always has a button. Our run-3 fold hid the only
  Order ever offered (ana d27 #308).
- **Wild's "Lay copper on every full link"**: one button with the total and the gain. It is my "one plan" at its
  honest size.

**Dropped of mine:**
- **"Grow everything that pays" as Max on every building.** At the pipe, Max adds nothing; below the pipe, the advisor
  already offers it. What survives is the pipe door on every full link at once, then Max. It becomes one line of the
  gold list, not a new noun.
- **Era stages at 1 day of income.** With the fix, economy's 6 h makes the Cathedral about 1.6M of goods, 70 times what
  was asked. One day was a guess I can't back.
- **Landmark tiers at 1, 2, 4, 8 days.** Worked through on my own player, tier 4 at Capital NW is 8 days of Mia's and
  Jo's income. Mia would pay it alone, because Jo never gives. I had built a wall for my own player.
- **A quarter's credit and no 💛 for gold given.** My own walk-through said *"Ben's gold finished the Cathedral 💛"*;
  Ana: *"nice that I can chip in without building"* (ana d27 #316), then *"only my name on the list"* (ana d36 #354).
  A gift is named, in hours, with a 💛; only the Patron points stay at 25%, so a maker's pipe counts four times.
- **The branch, out of this plan.** It doesn't answer the gold question, nobody else took it up, and Mia's evidence
  points first at the chat (part 4, step 6).

## 2 · What I reject, and why

- **Wild's Works (5–7 days).** It builds with gold the maker a friend could be. Dev asked for a role (*"12M gold
  sitting around, tell me how to be useful"*, group day 21), and his own Brickworks costs 1,400 g, the Yard bringing
  Ana's stone and Cleo's coal. Orders and the chain pointer do the job for about 1.5 days, with no new noun.
- **Wild's 2.5 × base, and keeping the Wonder at half.**
  - That makes two prices to learn where one is built (`GameWorld.WantOrders.cs:16`).
  - The half is exactly where Ben got stuck (ben d36 #88–#94) and where the Cathedral stopped at 50%.
- **Psychologist's half per person for 3 days, as written, for a station with one giver.**
  - Mia gives half on day 28 and comes back on day 35 to finish it, so her rhythm turns 3 days into 7.
  - My amendment: the cap doesn't apply when no other resident has given to the station in 14 days. Jo never chatted
    in run 4 (director's observations, row 1).
- **Friends' tier with a purse at ×1.2, a pipes part and a keystone:** three parts to read on a phone. Goods can
  fill by themselves; the purse is the only part my players act on, so it is the only part on the card.
- **Grey Widen in this plan (wild, outsider).**
  - For Ben and Ana the wall is the ingots, not the grey (see the bot split above).
  - A Copper Pipe gives ×5 for about 1,000 g of ordered ingots on a long link. Grey ×4 costs hours of income on every
    link and raises incomes fastest (outsider's own risk 1).
  - So: copper first, then Widen (economy's order). Grey Widen goes in only if the bot check before run 5 fails.

## 3 · The open questions, final

| Question | My answer |
| --- | --- |
| After the Landmark | **Tiers.** The goods come by pipe: 6 h of the station's spare in two goods its residents make, never a good nobody near makes (psychologist). The gold is a purse of *n* days of the station's 7-day income: 1, 2, 3 and on, linear. Each tier adds a storey, the givers' names in hours on its plaque, and pipes +10% (at most +50%; after that, picture and names only) |
| Gold in a shared goal | Chip in 1 h, 3 h or 1 day of your own income, as often as you like. Nobody gives more than half until the goal has waited 3 days, except at a one-giver station |
| Price and credit | 2.0 × base, one number. A gift is named in hours with a 💛. Ordered goods count 25% on the Patron line |
| Widen | Copper first, then Widen. Grey only if the check fails |
| One door or one plan | **One list,** "What your gold can do": three lines at most, shared first. Its private line goes the whole way on every full link, with one preview and Undo |
| The player alone | **In:** the district channel first, and gifts named at both ends. **Out:** the branch, unless Mia's alive stays at 2 or below in run 5 |
| Before run 5 | One light-bot run that does both checks: ingots orderable (economy) and Level 30's day (outsider). **Stop and change** if the seats without copper still bank more than 5 days (then add grey Widen), or if Level 30 comes before day 23 (then halve the tier perk) |

## 4 · The solution I'd sign, in build order

Each step is playable on its own.

**1 · Fix the sizing (½ day).**
- Count residents seen in the last 7 days (`ActivePeriod`), with a floor of 60 g/min per seat taken.
- Never size while nobody is lit.
- Resize upward only, until a quarter of the stage has arrived.
- Replaces: the 3-day window (`GameWorld.Market.cs:202–204`), the one-seat floor (`GameWorld.Tick.cs:208`), and the
  freeze at the first delivery (`GameWorld.EraWant.cs:230`).

**2 · One order rule, and a button that never vanishes (1½ days).**
- **Nobody near makes the good:** order all of it at once, at 2.0 × base, over 6 h. Ingots for a pipe tier count as
  such a good.
- **Someone near makes it:** their window first (at most 72 h), then the flow; half on a shared goal.
- **The line names the chain**, offered first to the friend with gold and no role: *"a Brickworks on your estate makes
  bricks from Ana's stone and Cleo's coal · Show me"*.
- **A waiting building says so:** *"waits for 100 bricks · Order ▸"*.
- Replaces: the half cap where no maker exists, the null offer (`EraWant.cs:195–198`), "is being built", and the folded
  Order.

**3 · The pipe door (2 days).**
- On the upgrade panel at the pipe, one button goes the whole way: research, then the tier (your ingots, a friend's,
  or ordered), then Max, with one preview in g/min and payback.
- At Copper's limit the button is Widen, then Max.
- The advisor's three dead ends become steps.
- Replaces: *"+N would make nothing more"*, and the door buried in the idle panel.

**4 · The gold list (1½ days).**
- The top bar reads *"40.6M · 24 days ▸"*. Tapping it opens **What your gold can do**, three lines at most, shared
  first:
  - a goal nobody near can fill;
  - a friend's waiting want;
  - the best private buy (*"Copper on every full link, then Max"*).
- The return card's first line after the people line is the list's top line, never folded.
- Next steps rank the same way.
- Replaces: the bare number and the folded growth line.

**5 · Chip in, and gifts (1 day).**
- Chip in 1 h, 3 h or 1 day of your own income on any shared goal or friend's waiting want.
- Givers are named in hours, with a 💛.
- Replaces: one Capital-wide half that the first buyer used up (`EraWant.cs:15`).

**6 · The player alone (1 day).**
- If no other human at her station has chatted, her chat opens on the district channel.
- A newcomer's hello shows as one line in crews' chats.
- A gift that stands a stage is named at both ends: *"mia_pipes Pipes' wood finished your Water Tower 💛"*.
- Run 3's two Mias greeted the district and answered each other; run 4's two met only on day 15 (director's
  observations, rows 1 and 11).

**7 · Landmark tiers (3–4 days, plus art through `process.mjs` and `fit.mjs`).**
- As in the table in part 3.
- Replaces: This week's *"A new parcel: Level 7 · 10.0k g"* (ana d36 #375).

**Total: about 11 days, plus art.**

**On my two players:** Ben's day-36 card leads with *"The Copper Cathedral waits for steel nobody in the Capital
makes · Chip in 1 day ▸"*, and his top bar with *"Copper on your full links, ingots ordered, then Max"*: two taps.
Mia's hello reaches the district, her pipe door orders the ingots, and she can finish her station's purse alone.

## 5 · One mark for run 5 that would prove it wrong

**Count every return after 4 or more days away. The median such session must spend at least 10% of the gold that came
in while away, and Ben's day-36 return must not fall below that median.**

Run 4's returns spent about 1.4% (Mia, day 8, of 2.2M), 0.5% (Mia, day 15, of 7.6M) and 0.03% (Ben, day 36, of 13M).

**If the median stays under 5%, this plan is wrong.** The doors would then be in sight and sized to the bank, and the
people who come least still wouldn't use them. Gold would need a job that doesn't have to be found.
