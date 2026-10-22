# Round A · lonely

I am the guardian of players alone and players who stop. I judge every answer to the owner's question by two players
from run 3:
- **Mia**, alone beside Jo, a scripted neighbour who never chats;
- **Ben**, who played three days and came back on day 36.

## 1 · Diagnosis: the wall, the pipe, and gold that can't reach anyone

The cause has three layers. The analysts found the first two; the third is the one my two players show.

**1. The wall (materials only from your own Storage).** This is real, and it hits the solo player hardest.
- The advisor bot has no friends and no story. It researches the Big Factory on day 3 and never builds anything that
  needs materials. It stays at 101 buildings and 2,884 g/min from day 4 to day 21, banking 107M by day 28
  (`docs/balance/balance-typical-advisor.md`).
- Mia, beside Jo, had 10 buildings on day 8 and 11 from day 15 to day 36, with 27.7M of the 29.2M she ever earned
  unspent (`metrics.jsonl`, samples at days 8–36).

**2. The pipe (added to the brief).** Upgrades, the big sink, stop paying at about 100 a minute per connection: Cleo's
level-25 Coal Pit could make 882 coal a minute and sold 100 (cleo d21 #393). The way past it, a Copper Pipe, costs
copper ingots from your own Storage (`pipes.yaml`), and her click did nothing (cleo d21 #399–#404). So bigger buildings
and higher levels end at the same wall, and what remains costs 5–15k against millions: *"Gold is 2.3M and the best
thing I could buy cost 15k"* (mia d08 #106).

**3. Deeper: gold is private, and it can't reach anyone.** The researcher's count says players came back for a friend
counting on them and a moving shared goal, never for gold after week 1. For my two players, gold is the one thing
they have plenty of, and it connects to nothing:
- **Mia** wanted *"bigger things to buy and more people live"* (mia d08 #119). Then *"the gold feels empty"* and "come
  back: maybe" (mia d15). She never played again.
- **Ben** came back to *"+5.5M gold, level 5 → 14"* and wrote: *"Everyone has been asking where I am for weeks and I
  let them down"* and *"I still can't help with the Era 2 water need"* (ben d36-2000 diary). He returned with a pile
  that could have bought everything his friends were missing, and no way to give any of it.

Removing the wall and the pipe gives gold back its use for growth, and that is necessary. But it doesn't give a
returner, an absent friend or a player alone a way to be part of what others are building. That is my addition.

I accept the analysts' rows 7 (every want a buyer on the pipes) and 10 (gold and points have a use). Everything below
sits on top of them.

## 2 · Proposal: "Gold that reaches people"

There are five parts. Parts A and B are row 7 with guards from my seat; C and D are new; E is row 10 with one rule.

### A · Sites never wait on someone who isn't there (row 7, with two guards)

A building, upgrade or **pipe tier** that lacks materials becomes a site. It buys through the existing Ask (`GameWorld.Asks.cs`):
1. your own producers;
2. friends' and neighbours' spare at the one price, named at both ends;
3. a House at the deadline, at 2.0 × base (`AskHousePrice`, line 33).

**Guard 1: the deadline counts only lit players.** A site raised while a crew mate is away never waits for him. Its
deadline (12–72 h, as the Ask) runs from the moment it is raised. Nobody away is shown it or asked.

**Guard 2: "Buy now ▸"** on every site, for the player who won't wait.
- The hub sells the missing materials into the site at its own sell price, **2.5 × base**, within the next Yard round.
- The hub already sells at 2.5 to buildings' inputs; this sells into a site too. It is no new price, and the ladder
  stays hub 1.0 < friend 1.3 < House 2.0 < hub now 2.5.
- Goods bought with gold go **into the site only**, never into a Storage, so nothing can be resold.
- For shared goals (the station's stages, the Era wonder) they count 25% toward credit, as bought goods already do, and
  never carry a 💛.

**Why:** without both guards, Mia's Market Hall (500 bricks) waits for a stone friend she doesn't have, forever.

**The pipe:** a pipe tier is a site like any other. Cleo's Coal Pit panel reads: *"Makes 882, its pipe carries 100 ·
a Copper Pipe (12 tiles · 12 copper ingots) sells 500 ▸"*. One tap raises the site. Her Smelter, Dev's coal or the
hub fills it. Upgrades start paying again.

### B · The Era wonder and the station's stages are buyers that anyone can feed (row 7)

Sized at each stage to the active players, one buyer for the whole Capital in every Yard round. For a player alone
they are her only "together" with the world: Mia's water and bricks go in by pipe, her part is on her tracker and on
"Today here", and she is named on the plaque.

### C · Fund ▸: gold reaches a friend's need (new)

On the card of anyone who can't send goods (a returner, a player whose land lacks the good, a player alone), a waiting
need of a crew mate or of the station offers one tap:

> 🏗 *Ana's Market Hall waits for 120 bricks · Fund ▸ 1,500 g*

**What it does:**
- It pays the hub's "Buy now" price (2.5 × base) for that site's missing materials. **The gold goes to the hub, the
  goods into Ana's site.** No gold moves between players.
- Ana's card reads: *"Ben's gold bought the bricks for your Market Hall 💛 Thanks"*.

**Who and guards:** crew mates and station residents fund each other's sites and the stage; anyone in the Capital
funds the wonder. Same-device accounts can't fund each other. The goods are consumed by the build, never stored. At
most 6 h of the funder's own income a day. Funded goods count 25% for shared credit, so the friend who brought real
bricks stays the one who mattered.

**The size:** materials are cheap at base (bricks 5 g, iron ingots 14, steel 60; `resources.yaml`). Funding Ana's
Market Hall costs about 1,500 g, and the whole Great Dynamo (1,800 bricks, 900 ingots) about 54k at 2.5×. **Fund is
not a sink for millions. It is a way to matter.** The millions go into what parts A and E reopen: buildings, levels
and pipes.

### D · While I'm away (new, opt-in)

One switch: **"While I'm away, my gold funds my crew's and my station's waiting needs, up to ▸ 1M"**.
- It works from a player's third day away until the budget is spent or he comes back.
- It pays only for needs that already waited 24 hours.
- It never spends without the switch: a caretaker must never spend someone's gold unasked. That is my own rule from
  v1.2, kept.

**Offered** once, when a player's bank first passes a day of their income with nothing of their own waiting (*"You've
2.3M and nothing waiting: let it help your crew while you're away? ▸"*), and on the return card.

**Friends see** *"Ben is away · his gold bought the last 40 bricks of your Steel Mill 💛"*: an absent friend keeps
showing up. Nobody is ever shown who didn't set it.

### E · Research and gold have a use (row 10), and nothing drains a player who is away

**I adopt:**
- the resting Lab;
- capped, repeatable Refinements;
- stage rewards aimed at the bottleneck;
- hub orders and the Invest line priced in hours of income.

**One rule from my seat:** every sink is a choice, never upkeep. No fee, decay or maintenance eats a resting estate's
gold; Refinements and orders keep running while away. A sink that charges the absent punishes leaving, and run 1's Ben
came back because *"nothing was lost"*.

### And, later: a second home next to people

The Franchise already opens a branch estate (up to three; `docs/ImplementationStatus.md`, "Expansion instead of
resets"), gated at Level 25 + Era 2. Nobody in run 3 reached Level 25 by day 36 (Ana 24).

**The proposal:** in small mode, from Level 15, a branch can be placed by "Find me neighbours" next to a crew that
plays. That is the lonely player's late want, *"more people live"*, bought with gold, with no move and no rebuild.
**Later:** after sites, and only if run 4's lonely player still has nothing to want.

## 3 · The three open questions

**1. Can gold buy at once, and at what price?**

Yes, and here is the rule:

| What | Rule |
| --- | --- |
| Price and source | Only from the hub, at its sell price **2.5 × base** |
| Where the goods go | Only into a site, a stage or the wonder, never a Storage, never a player's wallet |
| When they arrive | Within the next Yard round |
| Waiting instead | The House fills at the deadline at 2.0 |

Friends' goods always go first, at 1.3. Gold-bought goods count 25% for shared credit and carry no heart.

The player researcher warns that *"if gold buys at once, a friend's goods never matter again"*. Mia and Ben answer it:
- For the player alone and the returner, "never at once" means "never". Mia's want had no friend to wait for, and
  Ben's friends were waiting for him.
- What makes a friend's goods matter is the name, the credit and the Thanks, which gold never gets. Price can't do it:
  Ana held 39.8M.

**2. Do hub daily orders become buyers?** Yes: a buyer at the trading post, filled from production, never from a
Storage.
- They are priced at about 30 minutes of income (row 10) and offered only to lit players.
- An open order fills by itself while you're away and shows on the return card.
- For a player alone, the daily order is the one thing that asks her for something every day.
- Today none was delivered after day 14 (findings, chores), and in run 1 a pull from Storage was a STUCK (ana d21).

**3. A keystone that needs two friends?** Not for run 4. Later, and only when both seats are lit at the stage's start.
An away seat's input comes from its House, and the pass-over after 2 days stays.
A keystone that needs two humans makes the shared goal depend on presence; a station of Houses (Mia-B's) would always
fill it from Houses, so it gives her nothing; and changing the goal and how it moves in one run would make run 4
unreadable (player researcher). Revisit if run 4's sites are filled mostly by Houses (under a third from friends).

## 4 · Cost and risks

| Part | Effort | Builds on |
| --- | --- | --- |
| A's two guards and "Buy now" | S, about 0.5 day | The site engine of row 7; hub imports exist for building inputs |
| C, Fund ▸ | S–M, about 1.5 days | The site's House or hub fill with a payer other than the owner; one event; the card line |
| D, While I'm away | S, about 1 day | A budget and a daily pass over waiting needs |
| E's "no upkeep" | 0 | A rule for row 10 |
| A second home | S–M, later | The Franchise exists |

**About 3 days on top of rows 7 and 10.**

**Risks:**
- **Friends' goods matter less.** Only real goods get the name, the 💛 and full credit, and a site waits for its deadline
  unless someone taps "Buy now". Revisit if under a third of crew site materials come from friends.
- **A funnel.** Gold goes to the hub, goods are consumed by a build, same device is excluded, with a 6 h daily cap. An
  alt paying 2.5× for goods worth 1× moves at most 40% of its own real income, less than any trade.
- **Obligation.** Fund and "While I'm away" show only gifts given, and a bid to fund appears once per need.
- **Away pays better?** No: these spend the away player's gold, never earn. Waiting stays 20% cheaper than "Buy now".

## 5 · Weeks 2–5, walked through

| Player | Run 3 | With this |
| --- | --- | --- |
| **Ana** (39.8M on day 36, 13 buildings from day 15) | Big Factory STUCK on 100 bricks (ana d21); Market Hall out of reach | Day 14: the Big Factory is a site, and Cleo's Brickworks spare fills it in minutes, named. Day 21: the Market Hall (500 bricks) and a Copper Pipe (ingots from her Small Factory) as sites; her income grows again. Day 27: *"Ben is away · his gold bought the last 40 bricks of your Steel Mill 💛"* (if Ben set D on day 2) |
| **Cleo** (32.3M, 1.6M points) | Coal Pit sold 100 of 882; Lab ate ingots for nothing | Day 10: the Lab rests; Refinements (+2% a chain, capped). Day 14: *"a Copper Pipe sells 500 ▸"*, filled by her own Smelter; +400 coal/min to sell. Her bricks go into the Dynamo by pipe: her tier and name |
| **Dev** (late, *"I feel late and useless"*, dev d21) | 18.9M unspent | Day 8 on: Tailwind, and his wood into the stage. Day 21: one tap, *Fund ▸ the Dynamo's last bricks*: his name on it the day he felt useless. His own sites reach the Big Factory |
| **Ben** (stops on day 3, back on day 36, 5.5M) | *"I let them down"*, *"I still can't help"* | Day 2: the offer *"let your gold help your crew while you're away? ▸"*, one tap on a phone. Days 3–33: his budget buys what Ana's and Cleo's sites waited 24 h for, named. Day 36: his card leads with *"Your gold bought the bricks for Ana's Steel Mill and 300 bricks of the Dynamo 💛 Ana thanked you"*, then one more *Fund ▸* for the Era-2 water. Relief, not guilt |
| **Mia**, alone beside Jo (27.7M, 11 buildings from day 8) | *"the gold feels empty"*; never played after day 15 | Day 8: the Market Hall and a Glassworks extension are sites. Jo's spare fills what it can, named (Jo never chats, so the name is the company), and *Buy now ▸* the rest for about 1.5k: she builds the "bigger things" she asked for. Day 15: a Copper Pipe site lets her upgrades pay again; her water and bricks are named in the Capital's wonder. She sees others' parts on "Today here". Later, at Level 15, *"A second home next to Tea Time Pipes ▸"* |

## 6 · How run 4 would show it worked (my marks)

| Mark | Pass | Run 3 |
| --- | --- | --- |
| Mia beside Jo spends | ≥ 20% of earnings by day 15 and adds ≥ 3 buildings from day 8 to 15 | 0–5%; 10 → 11 |
| No empty gold | No diary after day 8 says gold is empty or pointless | mia d08, d15; dev d10; cleo d06 |
| The returner gives | Ben's day-36 session ends with one act that reaches a friend (Fund or Send), "together" ≥ 3 | 0 acts; together 2 |
| Nobody waits on the absent | No lit player's site waits more than its deadline on an away friend | — |
| Funnel probes | A same-device Fund moves 0; Fund ≤ 6 h of the funder's income a day | — |
| Friends still matter | At least a third of crew site materials come from friends, not gold | — |
