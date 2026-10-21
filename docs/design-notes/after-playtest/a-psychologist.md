# Round A · psychologist

I championed concept A, Stations with a Yard. I read the findings, all 28 diaries, the friends' 37 messages, the
director's notes, the fix plan and its record, the v1.2 build notes and my old papers.

My verdict on my own design, before the details:
- **The world layer held:** places, seats, crews, Houses and absence.
- **The social core failed, and it failed by design, not only by bugs.** The Yard assumed that trade which *happens*
  is contact that is *felt*. The players showed the opposite.
- **They wanted to give from minute one,** and the game gave them nothing to give, no one who needed it, and no act to
  give with.

The fix plan repairs the plumbing and the screens very well. It does not give the friends an act, and the owner is
right to expect more.

## What held, what failed

Classes: **(a)** built differently from the design · **(b)** built as designed, and the design was wrong · **(c)** not
reached or not tested.

### What held

- **The way in.**
  - Seat codes, Move in → Done, and Dev opening a day-1 code on day 8 with nobody online (findings, "The way in works";
    dev d08-2000 #3–#8).
  - Ana: *"'Start with friends' is exactly what I need"* (d01-2000 #9).
- **The return without shame.**
  - Ben after a month: *"a month away and nothing lost"* (ben d36-2000 #78).
  - The anti-return-shame lesson (Animal Crossing's weeds) worked exactly as intended.
- **No votes, no kicks, no lists of who didn't deliver.** Nobody felt blamed by the game.
  - Ben's guilt came from elsewhere (below).
- **Presence moments.** The two things that felt like neighbours were the Handshakes and answers in chat.
  - Ben, day 36: *"handshake with windy heights (dev)"*.
  - Mia, day 8: *"Finally got replies from Ana and Cleo – warm feeling"* (#105).
  - Both are events addressed to you, naming a person, at a moment.

### What failed

| Promise or assumption | Evidence | Class |
| --- | --- | --- |
| The Yard pays a seller for what reaches the Yard (§7) | It paid for what the buyer *took*, and the false lacks made it take goods nobody used: 3.5M gone in eight days; no Yard trade paid a player in 36 days (findings, Yard bug; r1-economy §1) | **a** (bug) |
| "Your energy now sells to Dev's mine at 1.75" on the first screen (§5) | Never built; the Yard panel was first opened on day 14 (cleo #234) | **a** |
| Minute 20: the Foundation stands and the crew decides the keystone | It stood after 42 h: iron ore came with no kit, a 19 h tail, and a basket sized by T(L) for one resident (Projects.cs 222–257). The Water Tower sat at 0 for 33 days | **b** (T(L), recipes from land) and **a** (tail) |
| Seats with complementary land create needs between friends | Every kit makes energy (events, day 1; Tenants.cs 372); every estate has ★2 copper, coal and stone (Survey.cs); "no building of Ana's used sand" (r1-researcher) | **b**, plus **a** for kits of "producers that need nothing" |
| Laying your own pipe to the Yard is the consent | The kit came piped to the gate (Tenants.cs 437–442), so no friend ever laid it; even if they had, it is one act, once | **a** and **b** |
| Asks, the Plan board and tending give everyday contact | 0 Asks, 0 pins, 0 tends (metrics). They sat behind the crew chip and below the Yard panel, and tending did nothing (Return.cs 412: "no stat bonus, so it can't be farmed") | **b** (no function, secondary) and **a** (placement) |
| The absent friend is announced to partners on day 23 | It went to a bell with 93 unread, under "Riverside Co." (ana d27-2000 #247–#253) | **a** |
| Mia is seated beside an active human | The crew held all four seats of the only human station, and the seat picker looped (STUCK, mia #18) | **c** (story) and **a** (picker bug) |
| Week 3 grows +15% from trunks, Discoveries and the Foundry Hall | Nobody reached them; the middle game stalled on dead ends | **c** |
| One price, 1.75, is fair and makes friends worth having | At 1.75 the buyer gains +1 g/min on a Small Factory fed with a neighbour's ore and loses 1.25 on a Smelter (r3-economy). Gold stopped mattering by day 6 (Cleo 1.6M) | **b** |

The numbers that sum it up:
- **The "together" score was 3 for everyone on the first evening, before any trade, and it never rose.** Its only 4
  was Ana on day 27, the evening she discovered the Water Tower wanted sand.
- "Come back tomorrow" from the crew, from day 8 on: 1 yes in 13.

### What the Yard assumed about players that turned out false

I wrote these assumptions myself, so I judge them hardest.

**1. "Trade that happens is contact that is felt."**
- My round-1 paper opened with *"a neighbour you were assigned, who trades with you automatically, feels like the water
  company"*. Then I designed an automatic matcher, made it smaller, gave it names on a ledger, and called the problem
  solved.
- The Yard panel speaks the language of a market: "Spare", "Lacks", "You buy 194/min energy from AnaPlans Pipes at 1.75
  g" (cleo d14-1900-0236-yard.jpg).
- Every player's own words were the words of a gift:
  - Ana: *"can someone bring sand?"* (group, day 1).
  - Ben: *"not let ana down"* (d01).
  - Dev, three times: *"How do I actually send sand to you?"* (group, days 10, 14 and 21).
  - Cleo: *"I don't know how to send her copper"* (d06).
- Nobody asked how the station *market* works. They asked how to *give*.
- In my round-4 defence I said strangers in the pool get their first trade "with no request and no wait". That is true,
  and it is exactly why it meant nothing: there was no request and no answer.
- This is WoW's Dungeon Finder lesson. Frictionless matching makes groups efficient and anonymous. I listed the
  lessons and still built the Finder.

**2. "One act of consent is enough choice."**
- Relationships live on small, repeated bids and responses (Gottman's research on couples).
- Ana's *"can someone bring sand? 😊"* was a bid, at minute one. The game had no way to turn toward it.
- The Yard asked for one choice, once (the pipe), and then made every later "choice" automatically. The kit even took
  that one choice away.

**3. "Complementary land creates needs."**
- The players brought the interdependence themselves. They read the seat names and split roles in the first five
  minutes: Ana energy, Ben sand, Cleo copper (group, day 1, 20:04–20:13).
- The game made every role impossible to keep:
  - Ben's "Riverside: water and sand" had no sand. Sand needs a dredger, Level 5 and a 5,000 g shore parcel.
  - Cleo's copper had no road to Ana.
  - Dev's sand had no road to the gate.
- **My own decision 10 put the home goods behind the fence at Levels 5–7 "so the role emerges once players understand
  chains".** But the plan forms at minute one, in chat, from the names.
- My veto rule "solo is a full game" plus ★2 of everything made sure nobody needed a friend in week 1. Ana's sand was a
  story, not a need.
- The crew then did the rational thing and gave up: *"Sand is optional now"* (Ana, day 21 and 27).
- That is learned helplessness in a group: "five weeks side by side but never together".

**4. "The price is the reward for helping."**
- The Yard's whole offer to a helper was money: sell at 1.75 instead of 1.0. By day 6, gold meant nothing (Cleo 1.6M,
  *"goals like 700 g feel pointless"*).
- Cleo: *"Helping costs me nothing but I still see no payoff for helping"* (d10).
- What the players valued was function:
  - a friend's building that runs because of you;
  - a puzzle solved together.
- Their longest, warmest conversation was Ana's idle factory, the crew's only real shared project, and it was a bug.
- Heyman and Ariely's "two markets" studies apply: a small payment makes people help *less* than no payment framed as
  a favour, because it moves the act from social norms to market norms. The Yard framed friendship as a price.

**5. "Social information can wait in a panel."**
- 0 clicks in 36 days on the crew chip, the bell and the Plan board (actions.jsonl).
- The outsider warned us in round 2 ("players read the report"), and we put the Yard's faces in a panel anyway.

**6. "Guilt comes only from mechanics."**
- I removed every mechanic that could make an absent friend guilty, and it worked. But Ben was guilty in 3 of his 5
  diaries anyway:
  - *"No sand sent again, letting Ana down"* (d02-0800);
  - *"guilt"* (d03);
  - *"still guilty about sand"* (d36).
- The guilt came from a promise the seat's name made and the game wouldn't let him keep, or let go of.
- Obligations form in chat whatever we design. A game must either make them keepable or let them end gracefully.

## Is the fix plan enough?

It repairs real causes, and I would ship most of it unchanged:
- **F1, the honest Yard.** Necessary: it stops the harm.
- **F2, dead ends that explain themselves.** It addresses the most pain (5 of 9 real STUCKs).
- **F4, a project the station can reach:** sizing from measured income, recipes from what residents make, no tail.
- **F5, one return card.** It puts news where people read.
- **F7, the seat picker. F8, smart Max.**
- **F9's tend that repairs.** It gives tending the job my design took away.

These are the right fixes for problems that were bugs or bad numbers.

Where it patches a symptom of the design flaw:

- **The plan itself says the honest Yard will trade less.** "An honest Yard trades little between kits that all make
  energy" (fix plan, top). After F1 the automatic channel shrinks, and with it the friends' only social channel. The
  plan answers this with the project and neighbour-fed chains, both still automatic.
- **F6 announces gifts nobody made.**
  - *"∴ Your sand reached the Water Tower"* labels an automatic flow after the fact. Knowledge of a transfer is not the
    experience of giving.
  - The fMRI study by Harbaugh, Mayr and Burghart (2007) found that the same transfer, given voluntarily, rewards the
    giver more than when it is mandatory.
- **The plan defers the act.** It drops "a send to a friend button" and keeps "giving needs an act (an Ask, or 'I make
  it')" as a trigger for after run 2 ("What would change the plan").
  - Run 1 has already pulled that trigger: every expression of cooperation in 36 days was a verb of giving, and the one
    tool that was an act (the Ask) went unused only because nobody could find it.
- **Its own cooperation designer rates the core promises low** (r3-together, appendix):
  - row 7, "the first Ask or link": low ("neither is promoted");
  - row 9, tending: low;
  - row 17, week 3: low;
  - row 12, "Dev brings what the crew lacked": medium.
- **The run-2 measure can pass without the thing it measures.** "First good knowingly given: named by the giver" will
  be met by AI players quoting the announcement in their diaries. AI players read everything and are kind (brief, "The
  playtest's limits"). A real player would see "your sand reached the Water Tower", think "oh, nice", and not feel they
  gave anything.
- **F2 removes the crew's one shared problem without replacing it.** Right, since it was a bug, but it shows what the
  crew wanted: something hard each of them could help with.
- **F6's seat-card honesty lowers the promise instead of keeping it.** *"wind kit · sand: a dredger on the shore,
  Level 5"* stops the lie. It also tells Ben he has nothing to give for two days, which is the window in which he
  quit.

So: **necessary, not sufficient.** The plan makes the Yard true. It doesn't make playing together something you *do*.

## What I'd change: bids, sends and "runs on Ben's sand"

The proposal changes the social core, not the world. It keeps stations, crews, seat codes, Houses, stand-ins,
absence, the one price, pipes between named companies, and the fix plan's F0–F5 and F7–F11. It is built on engines
that already exist: renewable Asks (GameWorld.Asks.cs 121, 298), F2's pipe planner, and the stand-in's rules.

### 1. Real needs on evening one: a ring of starter needs

Each seat's starter kit gets its producers *and one consumer whose input is another seat's kit good*, so the station's
kits form a ring:
- Ben's Riverside kit includes a Sand Dredger on its own shore. The seat keeps its promise.
- Ana's kit includes a Glassworks that wants sand.
- Cleo's kit wants Ana's iron ore.
- Dev's seat makes coal for Ben's kit consumer (its House sends it until Dev arrives), and Dev's own kit wants Cleo's
  copper.

The rules around the ring:
- **Every consumer always runs**, from the House (2.0) or the hub (2.5) when no friend supplies it. Solo is never
  walled, and a missing friend costs a few g/min, announced, as the design already allows.
- A ring of two is a pair. For six to eight, the ring runs through the big station.
- Mia's kit consumer is fed by her House neighbour.

This replaces kits of "producers that need nothing" (Tenants.cs 372), and it makes the fix plan's contingency "seat
kits get one consumer of a neighbour's specialty" the plan. It is Animal Crossing's native fruit: friends' fruit is
worth more, and a Nook Miles island lets the lonely player get it anyway.

### 2. The game makes the bid, to the friend who can answer

When a building of yours could use a good a station mate makes now, the game posts the bid for you. Nobody types
anything, and it is never addressed as a debt. It shows where people already look:
- the friend's return card (F5);
- the producing building's panel;
- the Next steps.

> 💛 **Ana's Glassworks could use your sand** · it buys from the hub now · **Send it**

The rules:
- It names the friend who *can*, never who didn't (the fix plan's "Ben can ›" from F4, used everywhere).
- It shows at most once per session and then folds into one line.
- It never says "unanswered". Meanwhile the House or the hub keeps the building running, so nothing waits.

Naming the able friend matters. Requests "to the station" (today's Asks, Asks.cs line 7) spread the responsibility so
that nobody feels it is theirs.

### 3. The answer is a send, and a send is a pipe

**Send it** is the act. One tap does three things:
- it lays the pipe from the producing building's network to the giver's Yard gate, if it isn't connected (F2's
  planner: *"Lay the pipe to your Yard gate · 26 tiles · 52 g"*);
- it opens a standing, directed Ask from giver to receiver: the renewable Ask, addressed to one company;
- it puts that good first in the giver's Yard flow for that friend, at the one price.

What it looks like and how it behaves:
- On the Yard's ring pipe the line is drawn as a stripe in both colours, with pellets running into the friend's
  socket. Pipes are where the friendship shows.
- **Stop** is one tap, with the 1-hour notice and the tank.
- It never takes more than the receiver's building really uses (F1's cap), so no funnel and no waste.
- It works while both sleep.

### 4. The effect, where both look

- **The receiver's building panel:** *"Runs on Ben's sand 💛"*.
- **The map:** the input pellet in Ben's colour.
- **The giver's return card:** *"Your sand kept Ana's Glassworks running all night: 1,240 sand"*.
- **Thanks:** the fix plan's once-a-day Thanks tap, on the receiver's panel, where Ana already looks.

Recognition goes to the effect, not to gold. That turns Cleo's "no payoff for helping" into a payoff she can see in a
friend's building.

### 5. Promises that can be kept, or let go

- When a friend can't make the good yet, the bid offers **"I'll bring it"**. The game turns it into a promise card with
  the path ("sand: a dredger on a shore · Level 5 · a 5,000 g parcel · about 2 days") and its progress.
- **Let it go** hands it back to the House without a word to anyone.
- This would have ended Ben's 36 days of guilt on day 1, either with a send or with a graceful release.

### 6. The automatic Yard stays, as the market

What no send takes still goes, honestly (F1), to whoever lacks it in the station, and then to the project and the hub.
It is shown as the market ("sold to the station: 12,400 energy this week"), without names, colours or thanks.
- **Strangers and Houses keep the instant, async trade** my round-4 defence was right about for Mia.
- **Friendship gets its own channel:** sends, the project, tending and Handshakes.
- Market norms and social norms stay apart.

### 7. The project by pledge

- Each good on a stage card has **"I bring it"**: one tap by a resident who can, and it becomes a send to the project.
- The card shows who pledged what, named only by their own pledge.
- Unpledged goods still fill from spare (F4). The keystone becomes a person's job, not a mystery on a tab nobody
  opened until day 21.
- The goal is shown on the "This week" row (F4). A bar that moves near its end pulls (the goal-gradient effect); a bar
  at 0% for 33 days does the opposite.

### What it replaces

- **In v1.2:** the Yard as the social layer (it stays as the market), hand-made Plan-board pins, the station-wide Ask
  as the main tool (it stays for free requests), tending without a job, energy-only kits, and home goods out of reach
  of a seat's promise.
- **In the fix plan:** F6's announcements of automatic flows (kept for the project and sends), F6's "Who could use
  your sand" (it becomes the bid), F6's honest seat card (the kit keeps the promise), F4's "Ben can ›" (it becomes the
  pledge), and the contingencies "giving needs an act" and "seat kits get one consumer" (both become the plan).

### How it answers the run

**Ana**
- *20:05.* Her kit's Glassworks reads *"buys sand from the hub · Ben makes sand"*. Her chat line "can someone bring
  sand?" is now also the game's bid.
- *Day 4.* F2 shows her the dead end in one line. That evening's group chat is about who sends what, not about a bug.
- *Day 27.* Her return card names Ben's stand-in (F5), and her Glassworks still reads *"Runs on Ben's sand (Granny Ida
  ⚙ stands in)"*. She never loses her friend's thread.

**Ben, on his phone**
- *20:08.* One card: *"Ana's Glassworks could use your sand · Send it"*. One tap, pellets in his colour, Ana's building
  running. His day-1 goal, "not let ana down", is met in minute eight.
- *Day 3.* He quits as in run 1. His estate keeps sending while it rests. The stand-in keeps the line at the old price
  for 14 days, then glides (§9).
- *Day 36.* The return card's best line is no longer "tended on N days". It is *"Your sand kept Ana's Glassworks running
  for a month"* (Granny Ida ⚙ stood in at the end): someone needed you, and nothing was lost.

**Cleo**
- *Day 1.* Her kit factory wants Ana's iron ore, so Ana's card reads *"Cleo's factory could use your ore · Send it"*.
- Her copper has no friend to go to until Dev comes, and it sells through the market meanwhile.
- *From day 8* she sees what her help does: *"your copper keeps Dev's Smelter at 100%"*. Smart Max (F8) stops the
  Lab.

**Dev, day 8**
- The House that warmed his seat was sending Ben's consumer its coal. When he moves in, the line becomes his (*"Dev
  now sends coal to Ben's works (was Capital Power ⚙)"*), and Cleo gets *"Dev's Smelter could use your copper ·
  Send it"*.
- He is the missing piece in a way the crew *sees*, as my round-2 paper promised and the build never showed. His
  dredger's dead end is fixed by F2, and its sand can be sent the session it runs.

**Mia**
- Seated beside Jo (F7, run 2). Her spare energy sells to Jo's factory through the market, announced once (F6). That
  is presence, which is right for strangers.
- Her kit consumer bids to Jo. A scripted Jo won't answer, so the House fills it, and she is never stuck.
- A human Jo could send: help from a stranger, answered only with thanks (the Death Stranding moment).

### What it costs to build

| Piece | Effort | Notes |
| --- | --- | --- |
| The ring of kit consumers | M | The generator picks the ring from the station's home goods; a kit may hold a building the player's level doesn't unlock yet (a new rule) |
| Bids | M | Derived from F1's honest lacks and "who makes it now"; shown on F5's card, panels and Next steps |
| Sends | M | A directed renewable Ask plus F2's planner plus the ring stripe |
| Effect lines and Thanks | S | Partly F6 already |
| Promise and pledge cards | S–M | |

About 5–7 days in all, against F6's M+M, which it mostly replaces. Art: a two-colour stripe on the ring pipe, checked
with `fit.mjs` like every Yard picture.

### Risks, and what would prove me wrong

| Risk | Answer |
| --- | --- |
| Bids becoming obligations | Bids are written by the game in positive words, shown once a session, never "unanswered", always filled by an NPC meanwhile, with "Let it go". If run-2 diaries show "I should send…" guilt, halve how often bids show |
| A neat ring breaking when seats fill out of order | Houses hold the missing links; after week 1 the Era recipes and F6's chains take over |
| Funnels | Goods only, at the one price, capped by real use: an alt is still worth one partner |
| Clarity | No new noun: a verb ("Send it") and a phrase ("runs on Ben's sand") |

**What would prove me wrong:**
- Bids reach all four friends on evening one, and no send opens by day 2. Then the act was not the missing piece, and
  the problem is reasons, not channels.
- Sends open, but "together" stays at 3 in the diaries. Then the effect isn't seen, and that is a display problem.

## What I'd keep no matter what

- Seat codes and the two-tap move-in. Held seats warmed by Houses.
- Labelled Houses within one 25% cap. Stand-ins. "A month away and nothing lost".
- No votes, no kicks, no list of who didn't deliver. Absence costs nobody else.
- One price between players, whatever level the bots choose.
- Trade between named companies on two-ended pipes. Never a merged network.
- The honest Yard (F1), dead ends that explain themselves (F2), the reachable project (F4), one return card (F5).
- The Handshake, the one moment that already felt like being neighbours.

## Questions for the others

- **Friends:** is a ring of needs too neat for real groups? What about two friends who want the same role, or a crew
  of six?
- **Lonely:** can a stranger's bid ("Mia's mine could use your energy") ever feel like pressure? Should bids between
  strangers show only after one Handshake?
- **Economy:** at the one price, does a send pay the *receiver* (r3-economy: +1 g/min at 1.75 on a Small Factory)? Does
  a kit consumer fed by a House at 2.0 keep Mia within 85–90%?
- **Outsider:** does "Send it / runs on Ben's sand" pass your five-word test better than Ask, Plan board and Links
  together?
- **Wild:** is there something bolder than a send that is still a pipe? Two friends' buildings sharing one factory
  floor, for example.
- **Everyone:** if run 1's players already wanted to give, why did all of us, me first, design a matcher instead of a
  gift?
