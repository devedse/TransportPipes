# Round C · wild: "Friends give, strangers trade"

The rival answer to question 1 of the synthesis. Everything else in "Needs from the land, gifts by pipe" stays as the
economy and friends specs decide it:
- the land dealt per station;
- kits that make the seat's good;
- the price (1.3 in this text, but nothing below depends on it);
- the House floor, Sketch, Thanks, the porch;
- F1–F11.

This paper changes **one rule** and specifies everything that rule touches.

> **Between two members of the same crew, goods move only by a Send that the giver chose. Everyone else (strangers,
> porch residents, Houses, the project) trades on the honest automatic Yard, as the market.**

The section "Where it is weaker" names honestly where this loses to "Send on top of the automatic Yard".

---

## 1 · The rule, and why it is the only version in which Send is real

**What moves, and how:**

| From → to | How goods move | Shown as |
| --- | --- | --- |
| Crew mate → crew mate's building | **Only a Send** (§3) | A gift: both names, "Runs on Ben's sand 💛", Thanks |
| Crew mate → the project | The market (automatic), **plus** Sends, which go first | The ring: a Send is named in your colour; market deliveries count for credit but show as "via the market" |
| Crew mate ↔ stranger (porch, station resident) | The market, automatic. A Send is allowed on top after a Handshake | Market totals, no names (the fix plan's privacy rule) |
| Anyone ↔ House | The market, within the one 25% NPC cap | Market totals |

**Why this rule.** Three things from the run and the record point to it.
- **The players asked for an act.** Every cooperative line in 37 group messages was a verb of giving:
  - *"can someone bring sand?"* (Ana, day 1);
  - *"How do I actually send sand to you? Station?"* (Dev, group, days 10, 14 and 21);
  - *"I don't know how to send her copper"* (cleo d06-1400 diary).
- **An outcome nobody chose isn't felt.** The Foundation bought Ana's energy from 20:07 on the first evening, and no
  player noticed (findings, promise "Minute 12"). The psychologist's Harbaugh citation says the same: a voluntary
  transfer rewards the giver; a mandatory one doesn't.
- **On top of an automatic Yard, a Send between friends is a placebo.** With dealt land, the honest Yard would carry
  Ben's spare sand to Ana's lacking Glassworks with or without the button. The outsider's test (b-outsider, attack 5)
  is "Send real or not shown at all", and Cleo, who checks every number (cleo d02-2000 #84), finds placebos. Under this
  rule Send is real: no tap, no gift.

Strangers keep the automatic market because the lonely guardian is right that a lonely player at 02:40 must trade with
whoever exists without asking anyone (b-lonely, "What I take", psychologist §6).

## 2 · Under the hood

All in `GameWorld.Yards.cs`, `MatchYard`:

1. **Contracts first** (459–472), unchanged.
2. **Sends next.** A Send is a giver-chosen step with no rate and no penalty: seller, good, receiving building, created
   at. It takes the lesser of the giver's spare and the receiving building's honest lack (F1's lack cap).
3. **The market:**
   - "Players with players" (484) gets one more condition: **not the same crew** (`CrewOf`, `GameWorld.Crews.cs` 95). It
     belongs in `YardTradeAllowed` (372), beside Blocks and the trade settings, so links and Asks obey it too.
   - Houses (486–490) are unchanged.
   - The project stays a Yard buyer, as today.

**The guards from the synthesis, all kept:**
- **Storage:** units that only go into a storage are paid at the seller's hub price. This closes the live Ask funnel
  (`GameWorld.Asks.cs` 246–251).
- **Ramp:** a new Send phases in against a stranger's usual 7-day share at 10% of that share a week.
- **Stand-in:** the old price only for crew members with 10+ play days on another device. Today the threshold is 5
  (`StandInOldPriceDays`, `GameWorld.StandIns.cs` 36).
- **Held seat:** while a seat is held for a sleeper, its stand-in sells the seat's land good uncapped at the House
  price.

**What it costs:** the crew condition is S. The Send step, its ledger and its stop notice are the same M that the
friends spec builds. There is no new channel, no link geometry and no change to `Links.cs`.

## 3 · The Send: taps, texts, limits

**Where Send ▸ appears** (one verb, everywhere the same):

1. **The producer's building panel,** in F6's "Who could use your sand" block, first. Only real takers with an honest
   lack are listed, crew mates first:
   - *"Ana's Glassworks · could use 15 sand/min · buys it from Bertha ⚙ now · Send ▸"*
   - *"The Water Tower · wants 5,400 sand · Send ▸"*
   - *"Mia's Smelter (next door) · could use 8 copper ore/min · gets it at the market · Send ▸"* (only after a
     Handshake with Mia, and marked *optional* because the market already serves her)
2. **The bid** (§4): one line, on the giver's return card and in the Next steps.
3. **Loady's first-gift step,** the step after the Yard step: *"Your Sand Dredger makes sand. The Water Tower wants it.
   Send it."* Its *Show me* opens the dredger's panel with **Send ▸** lit.

**One tap does everything:**
- **Already piped to the gate:** the Send starts. The panel line becomes *"💛 Sending to Ana's Glassworks · 15
  sand/min · Stop"*.
- **Not piped:** the button reads *"Send ▸ · lays the pipe to your gate · 26 tiles · 52 g"*. F2's planner lays it with
  the player's own pipe rules, in the same tap, with no drag. That matters because Ben's only real STUCK was a drag (ben
  d03-2000 #60).
- **The first unit arriving** is announced to both, once, within the fix plan's event budget:
  - giver: *"💛 Your sand reached Ana's Glassworks"*;
  - receiver: *"💛 Ben sends you sand: 15/min to your Glassworks"*.

**What the receiver sees from then on:**
- On the Glassworks panel: *"Runs on Ben's sand 💛 · 15/min · Thanks ▸"*. Thanks is once a day per pair, as F6
  decides.
- On the map: the input pellets in Ben's colour, at Ana's building, where she looks. Not only in the Yard corner,
  which is off screen at the default zoom (b-outsider, attack 1).

**Limits:**

| Rule | Value |
| --- | --- |
| A Send ends only at… | a building that uses the good, or the project. Never at a storage (the funnel guard) |
| How much | Never more than the building really takes (F1). What it doesn't take goes on down the giver's flow order: the market, the project, storage, the hub |
| Price | The one player price |
| Order | After contracts, before the market. Two Sends to the same building share by need |
| Against a stranger's running supply | The 10%-a-week ramp |
| How many | At most 12 Sends per estate (the links cap) |

**How a Send ends:**
- **The giver taps Stop.** *"Stops in 1 hour · Ana's Glassworks has 2 hours in its tank"*. Later the receiver reads,
  with no name: *"Your Glassworks gets its sand from the market now"*.
- **The building no longer takes the good** (demolished, or F2's IdleReason says stuck): the line pauses, never ends.
  The giver reads, as a crew mate who may help: *"Ana's Glassworks takes no sand right now: it's waiting for energy ·
  your sand goes to the market meanwhile · Sketch a fix ›"*.
- **A Block ends it,** as Block ends every trade between a pair today.
- **"Stopped sending" is never shown, anywhere.**

## 4 · The bid: the game asks the friend who can

**When it is made.** A crew mate's building lacks a good (honest lack, F1), and you have it spare or make it.

**What it says, everywhere:** *"💛 Ana's Glassworks could use your sand · Send ▸"*. It names the building and the
good, never a debt. It is never addressed "to the station" (the psychologist's point on diffused responsibility).

**Where it shows, and how often:**

| Place | Rule |
| --- | --- |
| Your producer's panel | Always, while the need lasts (§3, place 1) |
| Your return card (F5) | **At most one social line per card**, the outsider's budget. In this order: a friend back or a stand-in notice; a bid; a Sketch for you; a "runs on your sand" line |
| Next steps | A step valued like any other: the giver's gain plus the receiver's |
| Frequency | Once per need (building × good) per giver, then folded into the panel. Never "unanswered", never a reminder |
| To whom | Only active (lit) crew mates who can send now. Never to a resting or asleep one, who can't answer |

**A new need makes a new bid.** When Ana builds a third Glassworks beyond the House floor, the bid to Ben comes once
more. That is the moment growth needs a friend, and the only moment the game says so.

## 5 · The Yard panel: gifts and the market, kept apart

The toolbar's "Station" becomes "Neighbours" (F6). Its panel has two blocks:

- **Gifts** (names, colours):
  - *"You send: sand → Ana's Glassworks · 15/min · 21 days"*;
  - *"You get: copper ore ← Cleo · 8/min"*;
  - *"The Water Tower: your sand 4,200 of 5,400 ✓"*.

  Only your own gifts, in and out, as the fix plan's privacy rule says: a pair is shown only to its two ends.
- **Market this week** (totals, no names): *"Sold to the station's market: 12,400 energy · bought: 300 stone"*.

**One line on top explains the rule in plain words:** *"Your crew's goods move when you Send them. Everyone else trades
at the market by itself."*

The "Spare / Lacks" rows that confused Cleo (*"why do I pay when I have surplus?"*, cleo d21-1900 #301) leave the
screen.

## 6 · When friends don't tap

This is the weak point of this version, so here it is in numbers.

### What happens

Ana's Glassworks lacks sand, and Ben has sand spare but never taps Send.
- **Ana's Glassworks still runs.** It buys from a stranger at the market if one makes sand, else from Bertha ⚙ at
  1.4 × base within the House floor (two buildings' need per good, plus one per 10 levels), else from the hub at 2.5.
- **Ben's sand still sells:** to a stranger or a House that lacks it (Houses buy at 1.15 × his hub price), to the
  project, or to the hub at 1.0.

### What it costs, per level-1 Glassworks (15 sand/min, base 1.2)

| | Ben Sends it | Ben doesn't |
| --- | --- | --- |
| Ana pays | 1.3 × 18 = 23.4 g/min | 1.4 × 18 = 25.2 (House), so **−1.8 g/min**, about 1% of her real ≈ 200 |
| Ben earns | 23.4 g/min | 18.0 (hub) to 20.7 (House), so **−2.7 to −5.4 g/min** |
| Ana's third Glassworks (beyond the floor) | runs on Ben's sand | buys from the hub at 2.5 and loses money, so she doesn't build it |

So not tapping costs **little gold and all of the meaning**, and it caps **growth beyond the House floor**. That is the
same ceiling the economy spec gives a solo player.

### Do friends trade less than strangers?

**Yes, they can, and that is the honest worst case of this version.** Put Mia on the porch with a stone seat:
- Cleo's Brickworks gets Mia's stone automatically at 1.3, while Ben's sand reaches Ana only if Ben taps.
- In a crew where nobody taps, all of the crew's trade goes through strangers and Houses, and the friends are side by
  side again. That is run 1, with better goods in it.

### What catches it: five nets, from cheapest to last resort

1. **The handover at move-in** (no extra tap). When a friend takes a House's seat, the House's market flows to crew
   mates from the last 7 days become *his* Sends, kept by default. The move-in screen, the one Dev and Ben already saw,
   gains one line: *"Bertha ⚙ sold sand to Ana's Glassworks · it's yours now · ☑ Keep sending it"*.
   - Ben gives with the same two taps he made in run 1 (Move in, Done: ben d01-2000 #11, #13).
   - The same applies when a porch resident joins the crew (friends' invite): *"Your trades with Tea Time Pipes become
     gifts · ☑ Keep them"*.
2. **Loady's first-gift step** with *Show me*, for the skimmer who does only what *Show me* opens (ben d01-2000 #15,
   d36-2000 #81–#84).
3. **The bid, once per need,** on the one screen every player read on every return (F5: 51 clicks over 23 returns in
   run 1).
4. **The Next steps,** valued in g/min for both: *"Send sand to Ana's Glassworks · +5.4 g/min for you, and her
   Glassworks runs on yours"*. F3 ranks it among the solo steps.
5. **The switch, decided now.** The crew condition in `YardTradeAllowed` is one flag in `progression.yaml`
   (`together.crewTradesBySendOnly: true`). In run 2, if either of these holds, the flag flips and this version
   becomes the friends-first "on top" version with no other change:
   - by day 8 fewer than half of the crew members with 3+ sessions have opened a Send by a tap (handovers don't count);
   - or the crew's sent volume is below the volume its members get from strangers at the market.

   One flag, the same screens, so the team doesn't have to rebuild anything to change its mind.

## 7 · Phones: Ben's evening, tap by tap

| Time | What Ben sees | What he taps |
| --- | --- | --- |
| Move-in | *"Riverside · your kit makes sand from the first minute"* (the land spec); Move in → Done; nothing to keep yet on evening one, because no friend's building uses sand | 2 |
| 20:08, first screen | One toast at a time (F10). The Handshake: *"Your gate pipe met Ana's"* | 0 |
| About 20:15 | Loady's first-gift step: *"Your Sand Dredger makes sand. The Water Tower wants it. Send it."* · Show me | 1 |
| | The dredger's panel opens as a bottom sheet (≤ 55%, F10), with **Send ▸ The Water Tower** lit. His kit is piped already, so no pipe is drawn | 1 |
| | *"💛 Your sand goes to the Water Tower first"* | 0 |
| Day 2, 08:00, return card | *"Your sand built 30% of the Water Tower"* | 0 |
| Day 2, if Ana built a Glassworks | One bid line on the card: *"💛 Ana's Glassworks could use your sand · Send ▸"* | 1, optional |

That is **four taps on evening one**: two he made in run 1 anyway, and two inside Loady's Show me, which is the only help
he used.

**If Ben never taps,** his sand still feeds the Water Tower through the market. It counts for his credit, shown as
"via the market". The return card then offers *"Make it yours: Send ▸"* once. He is never asked again for that need.

**Drawing a Sketch stays desktop work. Building one is one tap** (the friends spec).

## 8 · Sleepers and stand-ins

**The giver rests or sleeps:**

| Days away | What happens to his Sends | What the receiver reads (crew only, never strangers) |
| --- | --- | --- |
| 3–14, resting | They run at 100%: the estate keeps producing | *"Runs on Ben's sand 💛 · Ben is resting; his sand keeps coming"* |
| 14–30, dormant | The caretaker keeps them at their terms. It never creates or stops one | the same line |
| Day 23 | — | The notice on the return card (F5), not the bell: *"Ben (Riverside Co.) has been away three weeks. From next week Granny Ida ⚙ keeps his sand coming to your Glassworks"* |
| 30 and more, asleep | The **stand-in keeps each Send as a Send**: Ben's name stays on the line. Old price only with 10+ play days on another device, then the glide to House terms. While the seat is held, uncapped | *"Runs on Ben's sand 💛 · Granny Ida ⚙ keeps it coming until he's back"* |
| Return | Every Send comes back to Ben on landing; nothing to accept | *"Ben is back · his sand comes from him again"* |

Ben's return card says: *"Your sand kept Ana's Glassworks running for 33 days · Granny Ida ⚙ kept it going from day
30"*. That replaces *"+2.6M gold"* as the line that says someone needed him.

**The receiver rests or sleeps.** Her buildings use nothing while asleep, so the giver's Send pauses by itself:
*"Ana's Glassworks is resting · your sand goes to the market meanwhile"*. It resumes when she lands. No bid is ever
made *for* a sleeper's building.

**A Send nobody made can't be inherited.** A stand-in keeps only Sends a human started, or kept at a handover, and
the crew can't create one for an absent friend. This is the line the lonely guardian's "tend carries his spare to the
project" crossed (b-wild, attack 5).

## 9 · How a crew mate's absence shows

**Where it shows, and only there:**
- crew only;
- in four places: the crew line, the receiver's building panel, the return card and the HQ's sleeping cat (exists);
- strangers see dark windows and nothing else (multiplayer-design §10).

**How the texts are made:**
- **Names:** *"Riverside Co. (Ben)"* (F9), because Ana never tied the company name to Ben (ana d21-1900 #240).
- **What a text says:** what still works, never what stopped. *"His sand keeps coming"*, never *"Ben hasn't sent"*.
- **No list of who didn't send.** The ring, the panel and the card show only gifts that exist.

**One new rule this version needs.** Because crew mates don't trade automatically, a crew mate who *never* sent
anything simply has no line. Nothing says so, and the bid isn't repeated. The absence of a gift is invisible, by
design.

## 10 · Joining, leaving, falling out

| Event | What happens to trade |
| --- | --- |
| A friend moves into a House's seat | The handover (§6, net 1) |
| A porch resident is invited into the crew | Her market flows with the crew become Sends, kept by default |
| A member leaves the crew (one tap, as today) | His Sends stay as Sends, now to a non-crew neighbour, with Stop as before. New trade with his old crew is market. Leaving never cuts a running supply |
| Two friends fall out | Block (exists) ends the Sends and the market between that pair after the 1-hour notice and the tank. Nobody else's line changes |
| A crew of 5–8 over a station and its small stations | The same rule over the big station's one Yard |

## 11 · "First-comer is the host"

I keep it. It is independent of question 1, and both versions can take it.

**The case.** The first solo player on an empty server, or in an empty district, has only Houses today. The lonely
guardian has nothing better than "Today here" with an empty district (a-lonely, question to wild). Nothing should
pretend to be a person (§9 of the design: NPCs are honest). What can beat Houses is **a promise the game keeps**.

**The rules:**
1. **The picker and her first card:** *"You're the first in Capital. The next player who comes will live next to
   you."* It is said once, never repeated.
2. **Placement:** "Find me neighbours" ranks the free seat in the first lit solo player's station, or her porch, above
   every other seat (+5, the strongest factor). An empty station never wins while hers has room.
3. **When someone arrives,** her return card shows it first: *"Jo moved in next to you · your gate pipes met:
   Handshake"*. The Handshake already exists.
4. **A seat code from her first evening,** the same share sheet as Start with friends: *"Invite someone to live next to
   you"*. Being first is the best moment to invite.
5. **Nothing else.** No timer, and no message if nobody comes. If no player arrives within 7 days, the line goes and
   "Today here" stays.

**Cost:** S. One placement factor, two texts and the existing invite sheet.

**Weakness:** on a server where nobody ever comes, the promise is never kept. The text must stay a statement about the
next arrival, never a countdown.

## 12 · Where this is weaker than "Send on top"

1. **Friends can trade less than strangers.**
   - If nobody taps, the crew's mutual trade is zero, while strangers and Houses trade by themselves (§6).
   - On top, Ben's sand would reach Ana's Glassworks whether he tapped or not.
   - The handover and the bid catch most of it, but not all of it. The switch exists because I might be wrong.
2. **The skimmer gives less.**
   - Run 1's Ben made 15–25 commands a session and quit on day 3. Under "on top" his sand flows to Ana for 33 days
     with zero taps. Here it flows to the Water Tower via the market, unnamed, unless he taps once.
   - His relief would come with the outcome (the psychologist's point); here it waits for an act he may not make.
3. **More of the first screen.**
   - This version leans on the bid and on Loady's step to catch the skimmer. Each is one line inside the outsider's
     budget, but "on top" needs neither.
   - Card inflation is the outsider's attack 3, and this version is closer to it.
4. **The trade share t may fall.**
   - The run-2 target (t ≥ 15% between humans, measured by light bots) is easier "on top", where needs alone move goods.
   - Bots tap Send by script, so they can't tell us whether humans will. AI players tap more than people do (the
     brief's limits).
5. **One odd asymmetry to explain.** A stranger next door trades with you automatically; your friend doesn't. The one
   line in §5 explains it, but it is one more thing to understand.
6. **AI players will flatter it.** They read everything and click Show me. A real Ben may not.

**Where it is stronger:**
- **Send is real.** Every gift between friends exists because someone chose it, so Cleo can't find a placebo, and
  Thanks thanks a person for an act.
- **Market and gift are separate** in the panel, so the confusing "Spare / Lacks" rows leave the friends' screens.
- **The stand-in keeps a *gift*,** with Ben's name on it. That is the strongest leaver line in the record.
- **The absence of a gift is invisible,** so it can't become blame. Under "on top", a friend's flow can shrink while
  the matcher reshuffles, and "why did Ben's sand drop?" has an answer that names Ben.

## 13 · Cost, measures, and the switch

**Build:** the crew condition is S, and the handover at move-in and at invite is S. Sends, the bid, the panel blocks,
the first-gift step and the stand-in's Send ledger are the same M that the friends spec builds. So this version costs
**about one day more than "on top"**.

**Run 2 measures it with acts, not diary mentions** (psychologist and outsider):

| Measure | Pass |
| --- | --- |
| Sends opened by a tap (not by a handover), per crew member with 3+ sessions | ≥ 1 by day 8 for at least half of them |
| Crew sent volume against what crew members get from strangers at the market | Sent ≥ market by day 14 |
| Diaries | "I sent" / "X needed my …" (an act), counted apart from "my sand reached …" (an announcement) |
| Ben's profile | A skimmer at 15–25 commands. Pass: one Send or a kept handover by day 3 |
| Mia | Unchanged: the porch and the market serve her, and "first-comer is the host" applies when she is first |

**The switch, again:** if the first two marks fail, set `crewTradesBySendOnly: false`, and this version is the
friends-first version, built.
