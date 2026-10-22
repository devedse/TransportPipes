# Round B · psychologist

My round-A line stands ("gold builds, friends fill"), but four of my own parts were weaker than what the others
proposed.

## 1 · What I take, and what I drop

**I take:**
- **Friends: the paced Order.** "Order the rest" ships over 6 hours at 2.0 × base, friends' goods go first in every
  Yard round, and what they bring comes off the Order with its gold back. It beats my deadline on both sides: the
  player acts at once, and the friend still wins the race (a Brickworks makes 12 bricks a minute; the Order ships 100
  bricks at 0.3 a minute).
- **Friends: Refinements for every resident** who runs the chain, named (*"Bricks +6% · Cleo's research 💛"*). The
  optimiser's private ladder becomes a gift.
- **Outsider: one word, "wants"**, a scaffold on the map, and the **"What stops you" line**: the one binding limit from
  the solver, its key and its price. It is my "true preview" on every panel.
- **Outsider: the chain shown when nobody makes the good** (*"a Brickworks uses your stone and Bertha ⚙'s coal · Show
  me"*), **"Cancel · your gold back"**, and **Refinements that raise limits first**.
- **Economy: every wonder stage sized at its start from 6 h of the Capital's measured income** (the code does it for
  later stages, GameWorld.Wonders.cs:116–126; the first was sized at world start for 60 g/min), the first resized on
  day 2; **gold fills at most half of a shared stage**, with a Patron line; the Era turns early when the last stage
  stands; and the planks check (every land good has an Era-1 want).
- **Lonely: nothing waits on a player who isn't playing**, the returner's card offers one act for a friend (her
  Fund ▸, which is the same Order), and no sink ever charges the absent.
- **Wild: hours of your own income** as the unit of prices and of credit.

**I drop of my own:**
- **"Only at the deadline."** A deadline is a dead wait: nothing on screen moves, so there is no goal gradient (people
  speed up as they see a goal come closer; Kivetz, Urminsky & Zheng 2006) and nothing for the friend to visibly beat. A
  flow gives both.
- **The hour's Undo.** Run 3's trap was visible to the solver before the click: *"Number I should have been told before
  clicking (the button only said '+21 238k')"* (cleo d06 #179). The Max button shows levels and price only
  (BuildingPanel.tsx:408–410). The income on Max, Max stopping at the pipe, and Cancel cover what Undo was for.
- **Landmark tiers, for run 4.** No station got past its Workshop (the crew's waited 27 days at 96% on water,
  GameWorld.Projects.cs:480), so tiers after the Landmark would go unseen in run 4. Friends' "station levels" are the
  version to build when a station gets there.

## 2 · What I reject, and why

- **"Buy now" at 2.5 × within the next Yard round (lonely).** Price can't protect a friend: 100 bricks at 2.5 × are
  1,250 g, 0.003% of Ana's 39.8M. At once, the player in session always beats the friend's Brickworks, and the run's one
  felt chain (Ana's stone in Cleo's Brickworks, cleo d14 #341–#368) would have had no reason to start. Lonely's real
  point is that "never at once" must not mean "never" for Mia and Ben. A 6-hour Order that starts at the tap doesn't:
  Mia's sessions were a week apart (days 8 and 15), so the want stands long before she is back.
- **"Never" (outsider).** It throws away the run's clearest wish: friends offered each other gold four times (Cleo on
  days 2, 6 and 14; Ana on day 21, #274, #309) and the game had no way to make it count. Ben came back with 5.5M and
  wrote *"I still can't help"* (ben d36). Under "never" he still can't. I keep outsider's chain as the first offer and the
  Order as the second.
- **A window of 24–72 h, "until every crew mate who can make it has had a session" (economy).** It waits on the absent:
  Ben was away 33 days, so his crew's wants would sit the full 72 hours, and during it gold has no act. Economy's
  insight, that the station's spare fills a want by itself while friends sleep, holds without a window.
- **"Widen" (wild).** Grey ×4 carries 400 a minute, close to copper's 500. It removes the Copper Pipe want, a natural
  two-seat chain (copper + coal), for every producer under 400 a minute. Limits-first Refinements give an earned
  version.
- **Later, not rejected:** lonely's "While I'm away" (Ben's goods already flow through the automatic Yard, and his card
  can say where); wild's Capital lots and branch (the next sinks if run 4's advisor bot still banks over 3 days of
  income; they need new art and rules).

## 3 · The three open questions, final

**1 · Can gold buy a want's materials? Yes, after a wait, as a flow; never at once.**
- **"Order the rest ▸"** on any want: the hub ships the missing materials evenly over **6 hours at 2.0 × base** (the
  Ask's House price, `AskHousePrice`, GameWorld.Asks.cs:33). Ana's Big Factory: 1,000 g. A Market Hall: 5,000 g. Cleo's
  Copper Pipe, 28 ingots at 18 g: about 1,000 g.
- **Friends first:** in every Yard round a friend's Send, a tap from a friend's Storage and the station's spare at 1.3
  go in before the Order's share; what they bring comes off the Order, and its gold comes back.
- **Who:** anyone in the big station for any want there, anyone in the Capital for the wonder. The gold goes to the
  hub, never between players, never between same-device accounts.
- **Shared goals:** an Order fills at most **half** of a stage, counts **25%** of credit, and carries no 💛. Gold can
  finish your building, not your friends' goal.
- **Why:** against banks of 28 days' income, only time, order and the name make a friend's goods matter (Gray 2012:
  the same act feels better when someone meant it). Stardew Valley keeps Joja, a gold route past the Community Center,
  and most players still take the bundles: not because they are cheaper, but because they mean something.

**2 · Do hub orders become buyers? Yes.** A buyer at your trading post for 24 hours, sized at 30 minutes of measured
income, paying 2.0 × base, filled from what flows past it: no Storage, no Deliver, nothing lost when it runs out.
Personal, so no Send into it. In run 3 none was delivered after day 14. It comes after the doors (economy): it adds a
want, not a door.

**3 · The two-friend keystone: now.** From the Workshop on, where two residents who are playing at the stage's start
each hold one land input: bricks (stone + coal) or copper ingots (copper + coal). An away seat's input comes from its
House at 1.4, uncapped; the 2-day pass-over (dcf31c2) stays; everywhere else, today's rule, so Mia-B's station of
Houses is unchanged. The wants carry two-seat goods too (outsider), but as gifts into one player's building; the
keystone is the goal that needs both of you, and from day 8 every "yes, I'll come back" named a shared goal or a friend
(*"the friends need my stone"*, ana d14). Run 4 stays readable: only the good changes, not how it moves.

## 4 · The solution I'd sign

| # | Mechanism and numbers | Days | Replaces |
| --- | --- | --- | --- |
| 1 | **Wants.** A building, upgrade or pipe tier short of materials stands as a scaffold and waits on the Ask engine. Fill order: your producers → a friend's Send or a tap from their Storage → the station's spare at 1.3, named at both ends → an Order. Goods go into the build. A want shows once, to a friend who is playing; nothing waits on the absent. **Cancel · your gold back** until it stands | 7–9 | `Pay()` reading only your Storage, "N in your storages", Deliver from Storage, the Plan board's Ask form |
| 2 | **The pipe as a want, and "What stops you".** A binding link offers its next tier in place (*"Sells 100 of 882 · Copper Pipe, 28 tiles: 280 g + 28 copper ingots · Start it ▸"*). Max stops at the pipe and shows the income after (*"Max +21 · 238k · income 783 → 285"*). One card line names the binding limit, its key and price, or stays quiet | 3–4 | The silent Copper Pipe click (cleo d21 #399–#404), "+21 238k", the Invest line's 13k against 27.8M (ana d27 #317) |
| 3 | **Order the rest**, as in §3: 6 h, 2.0 ×, friends first, refunds, at most half a shared stage, 25% credit | 1 | The Ask's deadline House fill; Buy now, Fund, Pay the rest and Commission as four verbs |
| 4 | **The Era wonder as the Capital's want.** A buyer in every Yard round after the station's project, at 1.0 × base; each stage sized at its start from 6 h of the Capital's measured income (about 1.38M of goods on run 3's day 15), the first on day 2; a Patron line; the next Era starts when the last stage stands; every card tells your part | 3.5 | The Wonders minigame, Dev's disabled Deliver (dev d21 #176–#178), a stage sized for 60 g/min |
| 5 | **The two-friend keystone**, as in §3 | 2 | A keystone nobody needs a friend for |
| 6 | **Points.** Labs rest when nothing is left to research. Refinements at 5k × 1.6ⁿ points: first pipes +10%, trading +10%, Hub Order +5%, then +2% to one chain; each capped at +50%; each applies to every resident running it, named | 3–4 | Cleo's Lab eating about 400 g/min of ingots; 1.6M idle points |
| 7 | **Hub orders as buyers**, as in §3, and the planks check | 2 | The Storage chore behind daily orders |

About **22–26 days**, each step playable. Later, if run 4 asks for them: station levels after the Landmark, Capital
lots, the branch, "While I'm away".

## 5 · The mark that would prove it wrong

**Of the materials that go into wants from anyone but the owner, at least a third come from friends and neighbours
(Send, a tap from Storage, the station's spare), in every crew, days 7 to 36.** Run 3: none. If Orders bring more
than two thirds, gold has replaced the friend and my answer to question 1 is wrong; the lever is then the pace (12
hours), not a ban. I chose this mark over a lonely one because the other failure, a player alone who never gets her
materials, can't happen by construction: every Order lands within 6 hours.
