# Round C · the shared idea: "Needs from the land, gifts by pipe"

> **After the dialogue:** the representatives changed parts of this signed text. Their joint statement in `dialogue.md`
> and the revised plan in `docs/playtest-redesign.html` are the current version. The changes:
> - the money switches on with the land;
> - the House floor: one building fully fed, then the 25% cap;
> - no probation next door;
> - Sketch is deferred;
> - harder run-2 marks;
> - 19–24 days;
> - the rest of ledger point 17.

This is the design team's one shared idea at the end of round C. The moderator drafted it from the three specs (c-economy,
c-friends, c-wild) and the three exams (c-lonely, c-outsider, c-psychologist). Where the specs disagreed, this text
takes the exams' fixes.

**All six signed it** (`votes.md`). This version includes their nine amendments, each marked with its author, such as
*(lonely, sign-off)*. One conflict the moderator settled, the handover at move-in, is marked **(moderator)**, and all
six agreed to it.

## 1 · The answer to the owner

**Yes, the design needs to change.** The playtest shows two layers:
- **The world layer held:** seats, crews, stations, absence, stand-ins and the way back.
- **The together layer failed by design.** "Nobody played together on purpose" (findings): in 36 days there were
  0 Asks, 0 links, 0 tends and 0 pins, and no good was knowingly sent to a friend.

The fix plan repairs what was built wrong: the Yard that paid nobody, dead ends that blamed their inputs, a project out
of reach and screens that hid it all. It is necessary, and it ships underneath this idea. But it leaves three causes
standing, and they are in the design, not the build:

1. **Nobody needed a friend.**
   - Every estate got copper, coal and stone (`GameWorld.Survey.cs:13`, `StarterDeposits`).
   - Every kit made energy.
   - So a friend's goods were never something you couldn't make yourself.
2. **There was no act.**
   - The only way goods moved between friends was an automatic matcher.
   - Nobody chose to give, so nobody felt given to.
   - Cleo and Dev asked *"how do I send?"* and found no answer.
3. **The price paid nobody both ways.** At 1.75 × base, buying a neighbour's copper loses the buyer money on a Smelter.
   The fix plan left 1.75 or 1.5 to the bots. At either price, half the chains of the new land would run at a loss.

The fix plan's own triggers (its §7) name the first two: *"nobody knowingly gives by day 8: giving needs an act"*, and
*"the trade share stays under 15%: seat kits get one consumer of a neighbour's specialty"*. We think run 1 has already
pulled both triggers. Waiting for run 2 to pull them again costs a run and a month.

## 2 · The idea in one paragraph

Each station's land is dealt so that its seats hold different goods: copper, coal, sand, wood and stone, each on one
seat, and never copied onto the others. Energy, iron ore and water stay everywhere, so the first hour works alone.
Every seat's starter kit makes that seat's own good from minute one. A friend therefore makes what you can't, and one
price (1.3 × base) pays both sides on every Era-1 chain. Houses sell land goods at 1.4 for the first few buildings, so a
player alone or a player whose friend has gone is never stuck. The automatic Yard still carries every resident's spare
to every honest lack. On top of it, one verb, **Send ▸**, makes a gift: it names the friend, keeps that friend first,
and lays the pipe if one is missing. The receiver's building reads *"Runs on Dev's coal 💛"*, and **Thanks** answers it.
A stranger who comes alone is seated next door and shares the crew's Yard and project. A crew mate who is stuck gets a
**Sketch** from a friend, built with one tap of their own.

## 3 · The parts

### 3.1 Needs from the land (c-economy §1–§3)

| What | Rule |
| --- | --- |
| Goods on every estate | Energy, iron ore, water. Loady's chapter 1 (Levels 1–4) works alone, as today |
| Land goods, dealt per station | Copper ore, coal, sand, wood, stone. Corner station (4 seats): groups of 2, 1, 1, 1. Small station (2 seats): 3, 2. Every station holds all five once |
| The pair rule | No seat holds both bought inputs of one Era-1 chain (copper + coal, stone + coal) |
| Where they lie | First good ★5/★4 in the start area beside the HQ; second in the first bought parcel (L5); third, small stations only, in the second (L7) |
| No copies | `StarterDeposits = []` for new worlds; the small extras (stone strip, 40% coal, lakes with beach) only on the seat that holds that good; no natural beach without sand; the Lumber Camp needs a `wood` marker on old forest |
| Kits | The seat's own land producer first, then moilies, up to 4 buildings, about 100 g/min on day 1 on every seat (93–104). The producer may be above the player's level: *"a gift: you can build more at Level 5"* |
| Seat cards | Tell the truth: *"Riverside · sand: a dredger on your lake"* |

**What a player sees where their land has no good** (outsider, E6; no copies ships only with it). The Build menu lists
extractors only for the goods your land holds. The others show once, greyed, in the building's own words:

> *"Copper Mine · there's no copper on your land. Cleo (Copper Gulch) makes it, and Bertha ⚙ sells it: build a Smelter,
> and its copper comes by itself."*

The Deposits overlay labels the station's goods by seat (*"copper: Cleo"*). A missing good then reads as a friend, not
a wall.

### 3.2 The money (c-economy §4–§7, with lonely's fixes)

| hub buys | House buys | friend (the one price) | House sells land goods | House sells energy, iron | hub sells |
| --- | --- | --- | --- | --- | --- |
| 1.0 | 1.15 × your hub price, 25% cap | **1.3** | **1.4**, under the floor | 2.0 | 2.5 |

- **One price, 1.3,** for the Yard, Send, links, contracts, the Exchange and the project. At 1.3 every Era-1 chain
  pays its buyer and its seller, even with both inputs bought (Smelter +11.5 and Brickworks +4.25 for the buyer;
  sellers +3 to +13.5). At 1.5 the Brickworks on bought stone and coal loses money, and the Smelter makes less than a
  Sawmill.
- **The House floor counts buildings, not units** (lonely, case 2). A House sells each land good to a buyer for
  `2 + ⌊level ÷ 10⌋` buildings that use it, at any building level. A level-25 Smelter is one building. Under a unit cap
  a solo player's House-fed chains stall at the level-10 milestone, and solo parity falls to 76–84%. Counting
  buildings, it is 90–93%.
- **The station backstop** (lonely's one insisted fix). *"Whenever no seat of your station will sell you a land good
  (its owner has been away 3+ days, has Blocked you or set 'not with' you, or the seat is held), the station's House or
  the hub's market sells you what that seat doesn't cover at 1.4, with no cap."* No player is ever the only door to a
  good. That covers the friend who rests (Ben's 33 days: otherwise about 0.77M of Ana's growth blocked) and the friend
  who falls out (Cleo's copper held hostage: −30.5 g/min, or +8 with the backstop). Players pay for it, so it is a sink.
- **No chain runs at a loss on NPC supply.** If it would, it pauses, with one line saying why. At these prices no
  Era-1 chain reaches that point.
- **The project pays 1.3 for what it takes.** Credit stays counted at base. A gift to the Water Tower pays like a gift
  to a friend.
- **The guards:**
  1. Units bound for a Storage (an Ask, a Send into a Storage) are paid at the seller's hub price. That closes the
     live funnel at `GameWorld.Asks.cs:246–251`.
  2. Same-device trades are at the hub price, and same-device accounts never size a goal.
  3. A stand-in pays its sleeper's old price only to a crew member with 10+ distinct play days on another device.
  4. Goods bought from Houses or the hub count 25% toward project credit.
  5. NPCs never owe.
  6. The one price is one number: no presets and no slider.

New gold from NPCs is bounded at 3.75% of output (target ≤ 6%).

### 3.3 Gifts by pipe (c-friends, with the exams' fixes)

**Send sits on top of the automatic Yard (answer (a)), decided by all six.** The Yard keeps carrying every resident's
spare to every honest lack, friends included, so a friend who never taps still runs his friends' buildings. Send adds
the name, the choice and a real priority.

**Where Send shows, and what it says.** It is always offered when a crew mate's building, or the station's project,
uses your good (outsider fix 1, psychologist fix 1). It is also offered toward any resident's building once your gate
pipes have met theirs (the Handshake). So Mia, next door with no crew, can give too *(friends, sign-off)*. The label
tells the truth about what it changes:

| Today's flow | Button | What the tap changes |
| --- | --- | --- |
| Ana's Glassworks buys from Bertha ⚙ | **Send to Ana ▸** | Your sand goes to Ana first |
| It already takes yours | **Send to Ana ▸ · keeps Ana first** | A standing promise: Ana first when your spare gets short, kept by the stand-in under your name |
| Your producer can't reach your gate | **Send to the Water Tower ▸ · lay 26 tiles · 52 g** | F2's planner lays the pipe in the same tap |
| Nobody needs more than they get | no button: *"Cleo's Smelter has all the coal it needs"* | — |

A player can predict the button, and no button does nothing. Nothing shows as "stopped", ever.

**What one tap does:**
- It lays the missing pipe on your own land (F2's planner, your own pipe rules, the price on the button).
- It creates the Send. Within a minute, the Yard serves it after contracts and before the shared matching.
- It announces the first unit once per pair: as a toast if you are watching, otherwise on both return cards.

There is no amount, no deadline and no confirm. **Stop sending** gives one hour's notice.

**The order in the Yard:**

```
1. contracts
2. protected shares: each human buyer's usual sellers, each human seller's usual buyers
3. Sends, in the order they were made
4. everyone else, pro rata
5. the project, Houses, storage, the hub
```

- **Usual partners are protected both ways** (psychologist fix 3, outsider fix 3, friends' guard).
  - A *usual* partner is a human company that traded this good with you over the last 7 days. Its protected share is
    its 7-day average rate.
  - **Nobody's running trade is cut by someone else's Send or arrival**, a crew mate's or a stranger's, as a buyer or as
    a seller. A Send, and a newcomer, take only new spare, new lack, and what was going to the hub, a House or the
    project.
  - A trade ends only by its own two ends: Stop with notice, the buyer no longer using the good, Block or "not with",
    or leaving.
  - This replaces the 10%-a-week glide for strangers *(psychologist, sign-off)*. The stranger is the most fragile
    player, so she isn't protected less than a friend. The rule also keeps the lonely guardian's standing rule:
    nobody's action lowers your income. It costs the felt gift nothing, because Dev's day-8 Send fills new demand
    (Ana's new Smelter), not Mia's share.
  - So when Dev's coal arrives on day 8, Mia keeps what she was selling Ana. Today's matcher shares pro rata
    (`GameWorld.Yards.cs` 379–383, 485) and would halve her sales overnight. The v1.2 design promised *"usual sellers
    first"*; this builds it.
- **A Send never exceeds the receiver's honest lack** (F1). It can't create demand, so a maxed Lab can't pull energy
  through it.
- **A Send to a sleeper passes to his stand-in** (lonely, case 4), at stand-in terms inside the 25% NPC cap: *"Granny
  Ida ⚙ takes it while Ana is away."* It never pauses, because the built stand-in keeps the sleeper's purchases going
  (`GameWorld.StandIns.cs:95–98`).
- **Who may Send:** any resident of the same big station (a crew mate or the stranger next door), to another resident's
  building or the station's project, never to a House. Between stations, links stay the tool. At most 6 Sends per
  giver, one per good and receiver. A pair is shown only to its two ends.

**What the receiver sees:**

| Situation | The building's line |
| --- | --- |
| A Send covers half an input or more | *"● Running · runs on Ben's sand 💛"* · input line *"sand 15/min · from Ben, sent · 1.30 g"* · **💛 Thanks** |
| An automatic flow from a human | *"sand 15/min · from Ben, via the gate · 1.30 g"* · **💛 Thanks**. Named, with no heart: relief, not a gift |
| The giver is resting | *"Runs on Ben's sand 💛 · Ben is resting; his dredger keeps sending"* (crew only) |
| The stand-in | *"Runs on Ben's sand · Granny Ida ⚙ keeps it coming while Ben is away"* |

**Strangers' automatic trades are named to both ends** and take a Thanks (lonely, case 1; psychologist fix 4). Mia's
warmest moment in run 1 was being addressed (mia d08-2000 #105). Under (b) her coal would have run Ana's Smelter
anonymously.

**On the map:** the receiving building's input pellets are drawn in the giver's colour. The giver's gate carries a
small pennant per Send, in the receiver's colour, with a heart for a day after a Thanks.

**Thanks:** once a day per pair, on the input line and on the return card. It gives no gold and no XP, and no counts
are shown.

**"Make it yours: Send ▸"** (from (b), psychologist fix 5). When a player's goods ran a friend's building by themselves,
their return card or their move-in card carries one line, **once per flow**. After that it lives only on the
producer's panel, so it never becomes a nag *(friends, sign-off)*:
- **Ben on day 36:** *"Your sand ran Ana's Glassworks for 33 days · Granny Ida ⚙ from day 33 · make it yours: Send ▸"*.
- **Dev on day 8:** *"Coal Co ⚙ sold coal to Ana's and Cleo's Smelters: your Coal Pit keeps it going · make it yours:
  Send ▸"*.

**(moderator, agreed by all six)** Outsider's fix 6 turns a newcomer's handed-over flows into Sends by default, with
no tap. The psychologist's felt gift is Dev's own tap in his first session (day 8, 20:05). Under (a) the flows continue
automatically either way, so they stay named flows and the tap is offered. Relief comes with no tap, and the gift is his
to choose. Only tapped Sends count for Loady's gift step and for run 2's marks.

**Bids.** One line per new need, on the Next steps and the return card: *"Ana's Glassworks could use your sand · Send
▸"*.
- It shows once per need, and never says "unanswered".
- It shows only to a player who is playing now, never to a friend who is away.
- A bid between two companies outside one crew shows only after their gate pipes met (the Handshake), and at most once
  a session (lonely).

**The return card** has one social line, the best one, chosen in this order:
1. a friend arrived or came back;
2. your gift's effect, or a Thanks;
3. a gift you got;
4. a Sketch to build;
5. a bid.

The rest fold under *"and N more"*.

**The Neighbours panel** (from (b)) has two blocks:
- **Gifts:** your Sends and the gifts you get, with names;
- **This week:** totals.

The *Spare / Lacks* rows that confused Cleo (*"why do I pay when I have surplus?"*, cleo d21-1900 #301) go.

### 3.4 Sketch: help a stuck friend (c-friends §7)

- **How it starts.** A crew mate's building has been stuck for more than one of their sessions. Then your Next steps
  and return card read *"🔧 Ana's Small Factory is stuck: its ingots can't leave [Look ›]"*.
- **Look and sketch.** Look › opens her estate read-only, at the building. On a desktop you sketch ghost pipes and up
  to 2 of her unlocked buildings. The ghost shows its effect and her cost (*"+28 g/min for Ana · costs Ana 12 g"*).
- **She builds it.** Ana gets *"Dev sketched a fix for your Small Factory · +28 g/min · 12 g [Build it] Not now"*.
  Build it places it with her gold and her pipe rules. Nothing on her land changes before her tap, and no gold or goods
  move.
- **Limits:** crew only; one open sketch per helper per friend; at most 20 tiles; it expires after 7 days.

Ana's 32 days behind a stuck factory would have been one evening.

### 3.5 The stranger next door (lonely, with wild's host)

- **Seated next door by default.** On a small server (under 50 active companies), "Find me neighbours" seats a
  stranger in the small station attached to a crew's corner station.
  - Its seats join the crew's Yard and project as residents. The crew's own seats stay closed.
  - Any active crew mate can close it to *future* strangers. Nobody living there is moved or cut off.
  - The crew's invite makes a neighbour a crew mate.
  - Players never read the word "porch": the card says *"Capital N · next door to Tea Time Pipes"*.
- **Under the land deal the stranger is also the crew's guard.** The big station then holds every good twice: Mia's
  sand covers Ana while Ben sleeps.
- **Probation counts the next-door station as part of the crew's big station,** so nobody can squat it (lonely, case
  4).
- **First-comer is the host** (wild §11, lonely's text). On an empty server the first player alone reads, once: *"You're
  the first in Capital. The next player who comes alone will live next to you."*
  - Placement ranks that station first.
  - The arrival tops her return card.
  - Her first evening offers a seat code.
  - If nobody comes within 7 days, the line goes. There is no timer and no countdown.
- **"Today here"** (lonely, S, in F5). One line of three public district events, on the return card from the second
  session. It stays off evening one's first screen, where it would repeat the move-in toast *(outsider, sign-off)*.
  Without it, Mia without Jo has only the chat.

**F7's "friends and a stranger are suggested different stations" stays true:** the stranger is in a different station
that shares the Yard.

### 3.6 Players who stop

**Ben** stops on day 3 and returns on day 36, under (a) and the backstop:
- **Days 3–33:** his kit dredger keeps running Ana's Glassworks with no tap from him, and he is paid for what she takes.
- **Beyond his supply:** the backstop sells Ana sand at 1.4 from day 6, his third day away, so she can build her second
  Glassworks.
- **From day 33,** when he has been away 30 days: Granny Ida ⚙ keeps his line *(lonely, sign-off: in run 1 she took
  over on day 34)*.
- **On day 36:** his card says someone needed him, and offers *"make it yours"*.
- **For Ana:** her largest price step is +7.7%, with notice, and nothing stalls.

Tending stays F9's: it shows only when there is damage to repair, and it names the tenders. There is no daily tend job,
which would be a chore (outsider).

### 3.7 The first evening (outsider's budget)

- **Five nouns and two verbs:**
  - nouns: crew, seat, House ⚙, your gate (G), the Water Tower;
  - verbs: Send and Thanks;
  - one phrase: *"runs on Ben's sand"*.
- **Words kept off the first evening:**

  | Word | What players read instead |
  | --- | --- |
  | "station" | *"Capital NW · 4 seats"* |
  | "Yard" | "your gate (G)" |
  | "starter kit" | *"Pine Hollow ⚙ left you a Lumber Camp and 3 Wind Moilys"* |
  | Foundation | "the Water Tower's foundation" |

  Handshake, Sketch, Ask, bid and keystone also wait.
- **Toasts:** one move-in toast that carries the arrival, the kit and the gate pipes, then Loady. On a phone, one toast
  at a time. The "Hello Neighbour" achievement and the Company Level card wait until after Loady's first step (F10).
  Both were in Ben's first-screen pile (ben d01-2000 #14) *(outsider, sign-off)*.
- **No drag on a phone.** Loady's pipe step becomes F2's one-tap planner: *"Lay the pipe to your trading post · 4 tiles
  · 8 g"*. The first gift is then not stuck behind the one move Ben couldn't make.
- **The first act.** After the gate step, Loady's step is *"Send your first gift"*.
  - On evening one it is a Send to the Water Tower, which always offers a row while it wants your good.
  - From day 2 it can be a friend's building.
  - The step completes on the first Send by a tap. A kit flow doesn't count.
  - The day-2 example cards show the Water Tower, not a Glassworks at Level 7.

## 4 · What it keeps, and what it replaces

| | Under the shared idea |
| --- | --- |
| **v1.2, kept** | Seats, crews, stations and Houses; the Yard as plumbing; absence, stand-ins and the way back; the project; one price between players; no votes, no kicks, no gold between players; privacy |
| **v1.2, replaced** | Copies of copper, coal and stone on every estate; energy-only kits; the price 1.75; the project paying base; the pro-rata matcher between usual partners; the stranger in a station of her own |
| **F0–F3, F5, F7, F8, F10, F11** | Ship as decided, with their gates. F2's one-tap planner is also Send's pipe and Loady's phone step |
| **F1** | Ships. Its premium is the one price minus the seller's hub price, so at 1.3 it is 0.3 × base for a base-price seller |
| **F4** | Ships. The recipe reads the station's dealt land goods. A keystone is a land good of a seat that has not built its producer yet. The project pays 1.3 |
| **F6** | Mostly replaced: Send, Thanks, the receiver's line, bids and the Neighbours panel's two blocks. It keeps "Where it goes" naming both ends, paid figures in the top bar, and the advisor's neighbour-fed chains, which the land deal makes the normal case |
| **F9** | Ships. The crew in the top bar, names, tending that repairs |
| **The fix plan's "dropped" list** | Unchanged except one item: its *"send to a friend" button* comes back as Send, because the land deal gives it something real to send |
| **The fix plan's open question** | 1.75 or 1.5 becomes 1.3 or 1.5, decided by light bots on the dealt land (§6) |

## 5 · What we decided against

| Idea | Why not |
| --- | --- |
| **(b) "Friends give, strangers trade"**: inside a crew, goods move only by a chosen Send | It fails the run's own skimmer. Ben stops on day 3, before any friend's building needs his sand, so under (b) his sand runs nobody's building and his return card has no one who needed him. A friend who never taps would trade less than a stranger. The asking then moves to the chat, where it caused Ben's guilt in four of five diaries. Its strength (every gift is chosen) comes into (a) as the always-offered, priority-keeping Send |
| **Send shown only when it changes where goods go** | It is real, but a player can't predict it. In the common case it is never shown, so the gift never appears |
| **Price 1.5** | With no copies two bought inputs are normal. At 1.5 the Brickworks loses money on them, and the House floor would have to sit at about 1.6, where a solo player can run no two-input chain. The bots get to prove us wrong (§6) |
| **The Works**, a station machine paying feeders a margin share | Later, for Era 2+ stages, with no feeder share above the one price |
| **Crew research at 75% off** | Not now: it is worth about 3 hours of a basic Lab, and it helps nobody who needed help in the run |
| **Daily tend jobs** | A chore, and the road back to guilt. F9's repair-only tend stays |
| **Letting goods pass through buildings** | Still dropped. F2 explains the rule and lays the pipe, and Sketch lets a friend help |

The Era-2+ content bug ships as a content fix with the money. The Machine Shop, Assembly Plant and Robot Works lose
value at base prices. The rule: every recipe adds at least 25% of its input value.

## 6 · How we'll know

**Before run 2, the light bots on the dealt land** (c-economy §10). Four profiles:
- `light`: 20 minutes every second evening, a 15-building cap, following the advisor;
- `light-crew`: four of them, with Ben stopping on day 3, Dev arriving on day 8, and Cleo `typical`;
- `light-solo`;
- `light-porch`.

Each runs at 1.3 and at 1.5, with 1.75 as the baseline. Ship the price that passes:

| # | Mark | Pass |
| --- | --- | --- |
| 1 | Paid trade share between humans | ≥ 15% of the crew's output value by day 14 |
| 2 | Buildings running at a loss on their supply | 0 minutes |
| 3 | Solo against crew, per building, paid | 85–95% on days 7, 14, 21, 28, with a light solo bot that follows F3 and F8 |
| 4 | Pacing | `typical` unchanged (Level 10 by day 2, Level 30 on day 25 ± 1); `light` reaches Level 10 on days 4–6 |
| 5 | New gold from NPCs | ≤ 6% of faucets |
| 6 | Ben's stop | Ana's chains get ≥ 99% of their need every hour to day 36; her largest price step is ≤ +8% |
| 7 | Conservation | Every sample |
| 8 | Funnel probes | A storage Ask moves 0 premium; a same-device trade moves 0; a 9-day quitter gets House terms |
| 9 | Copies gone | Over every seat of 3 seeds, no estate can place a producer of a good it wasn't dealt |
| 10 | Usual partners | Dev's arrival cuts none of Mia's coal sales |

**Run 2** keeps the fix plan's table and adds the felt part (psychologist, wild). Chosen acts (Sends by a tap, Thanks
given, tends, Sketches built) are counted apart from named flows:

| Measure | Pass |
| --- | --- |
| A chosen Send to a friend's building | In at least one session by day 10 (run 1's Dev, day 8) |
| Sends by a tap | ≥ 1 by day 8 for at least half the crew members with 3+ sessions |
| The skimmer | A Send by a tap, or a named flow that ran a friend's building, by day 3 |
| Mia, played twice *(lonely, sign-off)* | Once beside Jo, where placement seats her; once in run 1's world, next door to the crew. Next to the crew: named to a crew member by day 8, by a Thanks or a named flow. Both: "alive" ≥ 4 twice |
| Diaries | *"I sent"* and *"X needed my…"* counted apart from *"my sand reached…"* |

**What would change it:**
- **The light crew trades under 15% at 1.3:** check the advisor's chain steps first, then try 1.25.
- **Solo parity falls under 85%:** raise the floor to 3 + ⌊level ÷ 10⌋.
- **Diaries read a friend's absence as a loss:** lower the House land price to 1.35.
- **Run 2 has named flows but no chosen Send by day 10:** the land works and the act doesn't. Then make the bid
  stronger (Loady's chapter-2 step, the bid on every lit player's Next steps), not the rule. (b) stays rejected,
  because its failure case is run 1.

## 7 · The build, in playable steps

Every step is a playable commit with its gate, as in the fix plan.

| Step | What ships | Gate |
| --- | --- | --- |
| 1 | The fix plan's steps 0–4: F0, F7, F1, F8, F2 + F3, with F2's one-tap planner | The fix plan's gates, unchanged |
| 2 | **The money:** the one price as one number (1.3 provisional until step 5's bots confirm it *(economy, sign-off)*), the storage and same-device guards, the stand-in's 10 days, the project paying the price, Houses' land sales with the building floor, the station backstop, usual partners first in the matcher, the Era-2+ content fix. It is mostly inert while copies exist, so it is safe on today's world | Unit tests of each guard; conservation |
| 3 | F4 on the station's goods, F5 with the social line slot and "Today here" | The fix plan's gates 5–6 |
| 4 | **Send,** Thanks, the receiver's line, bids, Loady's gift step, the Neighbours panel's two blocks, "make it yours" | `send.mjs` on a desktop and a phone (touch): Dev's stuck dredger, one tap, his sand at the Water Tower within 20 game minutes, the first gift on both cards. This is the fix plan's gate 7, re-aimed |
| 5 | **The land deal:** the deal per station, no copies, kits with the land producer, the Build-menu line, the overlay by seat. A new world on the test server | The light-bot marks above. The price is decided here |
| 6 | The stranger next door, the host, probation | Agents: Mia d01 next to the crew; a squatter probe |
| 7 | Sketch, the gate pennant, the pellets in the giver's colour | e2e: Dev sketches Ana's factory pipe, Ana builds it |
| 8 | F9–F11's rest, then run 2 on a fresh seed with Jo | §6 |

**Cost.** The fix plan's estimate is 8–10 working days. Without most of F6, add:
- the money and the land, about 3 days;
- Send and its parts, about 4–5 days;
- the stranger next door and the host, 1 day;
- the light-bot harness: four bots in one station, a 15-building cap and the station next door. Today
  `BalanceSimulation.cs` runs one bot per profile (lines 18–28, 270), so this is about 1 day of new work *(economy,
  sign-off)*.

That makes about **15–18 working days**, plus the bot runs and run 2.

**One cost the owner should hear first:** step 5 starts a new world on the test server, so the owner's estate there
restarts.

## 8 · For the dialogue: where we expect the fix panel to differ

1. **Redesign now, or after run 2.** The fix panel kept the design and set triggers. We say run 1 already pulled
   them.
2. **The price.** They decided between 1.75 and 1.5. We say 1.3, because with no copies two bought inputs are normal.
3. **Send.** They dropped a "send to a friend" button, because there was nothing real to send. We bring it back,
   because the land deal makes something real to send.
4. **The stranger next door by default.** It sits against F7's "different stations", which this keeps.
5. **Order.** We ship F1–F3 first, as they do. We add the money before the land, and the land only after the phone
   replay of the one-tap gate pipe passes.
