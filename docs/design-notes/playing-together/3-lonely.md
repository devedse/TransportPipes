# 3 · Guardian of the lonely and the leavers

The players nobody designs for are the majority. In an idle MMO most players play alone most of the time, most
friend groups shrink to one within a week, and most estates go quiet within a month. If the design only works for
three friends online together at 21:00, it works for maybe 10% of the sessions. Everything below assumes the
"links" model (a pipe from my gate to yours joins exactly two companies, `docs/TradingRethink.md` option A), but
almost all of it works with any model that has explicit partners.

## The five laws (the guardian's veto list)

1. **Every partner can be replaced, nothing stalls.** A link never goes dead silently. When the other end goes quiet,
   the link keeps working, is handed to a stand-in, or falls back to the hub, and the player is told which, in one
   line, before it happens.
2. **Solo is a full game, not a handicap.** Nothing (building, tech, level, Wonder reward, corporation perk) needs
   another human. Humans add variety, warmth and +10–15%, never ×2.
3. **Presence, not simultaneity.** The world shows that people *were* here today (lights, changes, notes), never how
   few are online right now. No social action needs both sides online.
4. **Leaving is free, returning is a gift.** Absence never costs what you built, your land rights, your rank in
   things you already earned, or your friends' progress.
5. **AI is honest.** AI companies are always labelled as such (a small cog badge on the banner), never pretend to be
   people, and never take anything from humans (land, ranks, rewards).

---

## Situation 1 · The solo player on a quiet server at 3 a.m.

What they feel today: the district is 24 estates, 8 of them tenants that never change, the rest players who were
last seen yesterday. Nothing moves except their own pipes. Nobody answers an offer until morning.

### 1.1 Houses: AI companies with personalities (replace static tenants)

Tenants become **Houses**: named AI companies that *live* (grow, build, ask, thank, compete). Each district gets
6–10 of them, fewer as humans arrive (§7).

| House archetype | Example | Personality on a link | What they do over time |
| --- | --- | --- | --- |
| **The Reliable** | *Oakridge Coal & Sons* | Buys a steady amount at hub +5%, always pays, never haggles | Slowly adds a generator every 2–3 days |
| **The Grower** | *Brassworth Brothers* | Wants 10% more of something every day | Expands visibly, buys parcels, posts "we're building a Steel Mill, need 2k ingots" |
| **The Haggler** | *Madame Ferro's Exchange* | Offers below hub, then +8% if you hold out 24 h (a little game) | Opens and closes trades, gossips in letters |
| **The Collector** | *The Curio Guild* | Buys odd, small batches of rare goods at hub +30% | Builds a museum on its estate showing what it bought (and from whom) |
| **The Rival** | *Kestrel Industrial* | Doesn't trade with you; races you | Always 5–10% ahead or behind you in the District Wonder and weekly board |
| **The Neighbour in need** | *Willowmere Bakery* | Asks for help ("our generator broke, 500 ⚡ for 2 days?"), pays in favours (XP, a decoration) | Recovers and thanks you with a statue/plaque naming you |

What the player sees:
- Houses' estates change: scaffolding, new buildings, lights at night. Their Level follows the server
  (`min(median active player Level − 2, days since district opened × 1.2)`), so they never lag behind into
  irrelevance and never outgrow humans.
- Houses write **letters** (short, 1–2 lines, in their voice) into the inbox at most once a day per House, and only
  Houses you're linked to or next to: "Brassworth: Your bricks built our new hall. Next week we want twice as many,
  can you keep up?"
- A House you've traded with for 7+ days becomes **Friendly** (a heart on its banner): +5% on its link prices, and it
  appears in your offline report ("Oakridge bought 4,200 coal from you, 'Fine stuff, as always.'").
- Cost to build: Houses are tenants with a growth script (a build queue drawn from a template per archetype), a
  price stance and a text template table. No LLM needed; a table of ~30 lines per archetype gives enough voice.

### 1.2 The hub town grows with the district (the world never feels empty)

The hub city in the middle is the solo player's constant audience. Its size follows what the district sells to it
(humans and Houses).

- 6 growth stages (Hamlet → Village → Town → City → Capital quarter → Metropolis), at 1M, 5M, 20M, 80M, 300M gold of
  total district sales. Each stage adds visible things: houses, trams, a market square, a festival on stage-up.
- The town has 5 **named citizens** (the Mayor, the Baker, the Engineer, the Teacher, the Stationmaster) who post the
  daily orders as notes on a board: "The Baker needs 300 bricks for a new oven." Delivering shows the oven built in
  town the next time you zoom out. Your contribution share is on a plaque: "This oven was built with Ana's bricks."
- Solo player at 3 a.m. still moves something visible: the town reacts to *their* deliveries within a minute.

### 1.3 Presence without simultaneity

- **Lights on:** estates whose owner played in the last 24 h have lit windows at night and a lit gate lamp. The
  district overview says "Lights on in 11 estates today", never "2 online".
- **"Since you last looked" sparkles:** on the district map, every estate that changed since your last visit
  (new building, new parcel, new decoration) shows a small sparkle. Tapping it plays a 4-second timelapse of the
  change. Cheap: a list of build events per estate since timestamp X.
- **Usual hours on the banner:** "Usually here: mornings" (from their last 14 days of sessions, in 4 buckets).
  Tells the 3 a.m. player who shares their hours.
- **Postcards & gift crates:** you can leave a postcard (one of 20 pre-written lines + emoji stamp, so no
  moderation load) or a small gift crate (up to 1 hour of your production of one good) at a neighbour's gate. It
  waits there, visible as a crate, until they log in. Max 3 gifts/day given, gifts from one giver to one receiver
  max 1/day (anti-funnelling; the value is capped by the giver's own production rate).

### 1.4 The Guild of Independents (a corporation for people without friends)

At Company Level 8 every player can join the **Guild of Independents**, an AI-led corporation per district (led by
the Mayor). It has the corp perks that don't need humans (corp HQ, weekly goal, free-trade between members), its
weekly goal scales to the number of *active* members (min. 1), and human members see each other in its roster.
It's the place where lonely players find each other without having to found something. When 3+ human members have
traded for a week, the Guild suggests: "You three could found your own corporation" (one tap, keeps the history).

### 1.5 Solo parity numbers

| Thing | With a human partner | With a House / the hub |
| --- | --- | --- |
| Link price bonus | Up to +15% over hub (negotiated) | +5% (Reliable), +30% on small rare batches (Collector) |
| District Wonder reward tier | Personal tiers by own contribution | Same personal tiers; scaled to active population |
| Corporation perks | Own corp | Guild of Independents gives 80% of the perks |
| Contracts | With anyone | Houses accept any fair contract (exists) |
| Weekly contests | Board vs. everyone | Plus "beat your Rival House" medal for everyone |

The difference between solo and social is ~10% income plus the warmth. That's the right size: worth inviting a
friend, never worth quitting over.

### 1.6 Odd hours

- Everyone's "day" (daily orders, daily streak counters) rolls over at their own chosen check-in time, not at
  server midnight.
- Weekly contests count your **best 3 days** of the week, so playing at odd hours or on few days is not a handicap.
- Events last at least 48 hours; no reward requires presence at a given hour. Countdowns (the season finale launch)
  are a show to watch live or as a replay, never a reward gate.

---

## Situation 2 · The player whose friends quit after two days

Ana, Bob and Cem started together. On day 3 Bob and Cem stop. Ana's links go to two estates that still run (idle
production continues), but they never grow, never take more, never answer. Ana's group goal is stuck at 1/3.

### 2.1 The crew shrinks gracefully

- A **crew** (friend group, from the friends design) counts **active members** for its goals: when Bob rests,
  the crew goal resizes to Ana + Cem's share, the sleeping members' share is done by their caretakers at 50%.
  A crew goal never fails because someone left.
- Bob's banner in the crew panel turns to "resting, 4 days" with a small zzz. No red, no "inactive", no shame.

### 2.2 Autopilot: absent friends keep growing a little

Set when founding the crew (default on, the absent player can change it): **"While I'm away, my caretaker may keep
my estate growing for my crew."**

- From day 3 away, Bob's caretaker reinvests up to 30% of Bob's income into **upgrades of existing buildings** that
  feed Bob's links (never new buildings, never demolition, never parcels). So Ana's demand from Bob grows ~5% a week
  instead of 0.
- It stops after 30 days (asleep). Bob comes back to a slightly bigger estate and a note: "Your caretaker upgraded
  your Brickworks twice to keep up with Ana."

### 2.3 Tending a friend's estate (a ritual and a return hook)

- Ana can **tend** a resting crew member's estate once a day: walk over (visit), tap the HQ, "Tend". It costs
  nothing, gives Ana +20 XP (small) and Bob's estate a visible flower bed / swept path (cosmetic) and +2% production
  for a day (max one tend per day per estate).
- Bob's return screen says: "Ana tended your estate 17 times while you were away." That is the single strongest
  win-back line in the whole design: someone missed you.
- Tending is also how Ana feels she still plays "with" someone, even alone.

### 2.4 Win-back, at most three times

If Bob opted into email/push, he gets at most three messages per absence, all positive and concrete:
- day 3: "Your Brickworks earned 4,200 g while you were away. Ana is still building next to you."
- day 12: "Ana tended your estate 6 times. Your caretaker keeps your trades running."
- next chapter start: "Chapter 3 begins. Your estate is safe on the Quiet Isles; land next to Ana in one tap."
Never "your workers miss you", never a countdown, never "you'll lose X".

### 2.5 Finding new neighbours (opt-in, not forced)

When 2 of Ana's 3 linked partners have been away 7+ days, the neighbourhood panel shows one card, once:
"Your neighbours are resting. **Find lively neighbours?**" Three options:
1. **Stay** (default; the card goes away for 14 days).
2. **Invite lively players here:** Ana's free/sleeping-adjacent spots are advertised on the placement screen for
   newcomers and movers with a "Ana is looking for neighbours, plays evenings, Lv 14" card.
3. **Move to a lively spot:** a free relocation (no 7-day cooldown, no gold cost) to one of 3 suggested spots next to
   active players of similar Level and similar usual hours. Bob and Cem's links are kept as long-distance links
   (cargo-priced) or ended at Ana's choice.

---

## Situation 3 · The neighbour who vanished, your pipe still plugged in

Ana built her whole iron chain around selling coal to Dara next door. Dara stopped 20 days ago, half-built estate,
ivy growing. In 10 days Dara's estate floats off to the Quiet Isles. What happens to Ana's pipe?

### 3.1 A link always has an honest status

The link's panel and its pipe on the map show one of five states:

| State | Pipe on the map | Panel line |
| --- | --- | --- |
| **Live** | Pellets flowing | "Dara buys 1,200 coal/h at 14 g" |
| **Resting partner** (3–14 d) | Pellets flowing, zzz on the far gate | "Dara is resting (5 days). Her estate keeps running; this link keeps delivering." |
| **Caretaker** (14–30 d) | Pellets, a small caretaker hat | "Dara's caretaker keeps this link at the same price." |
| **Handed over** | Pellets, new banner at the far gate | "Dara's estate sailed to the Quiet Isles. Hollow & Sons took over her spot and keep buying your coal." |
| **Capped** | A brass cap on the far gate, no pellets | "No one at this gate yet. Your coal goes to the hub meanwhile (−12%). A newcomer here will be offered your link." |

A link never shows zero flow without a reason in words.

### 3.2 Successor Houses (the link survives the partner)

When an estate goes asleep and its spot is freed, a **successor House** moves in within the hour if no human takes it:

- It **inherits the link profile**: what crossed each of the old estate's links, at what price, at ~90% of the
  volume. Ana's coal link keeps running with a new name on the other end.
- 72 hours before, Ana's inbox: "Dara's estate will sail to the Quiet Isles on Friday. A House will take over and
  keep your coal link (−10% volume). [Keep, default] [End the link] [Ask a friend to take the spot]".
- The successor builds its own pretty estate (from an archetype template) on the free land; Dara's buildings went
  with Dara (nothing of hers is touched or left as ruins next to Ana).
- When a human later takes the spot, the House moves out, and the human is offered every link the House had:
  "Ana already has a coal pipe at your west gate. Connect? She sells 1,080 coal/h at 14 g." A newcomer gets a
  ready-made, proven trade partner in minute one. (This is the best tutorial partner possible: a real, active
  human who wants the link.)

### 3.3 Never blocked land, never blocked corridors

- Corridors are public: a dormant player's corridor pipe can always be **bridged** by others (a bridge piece crosses
  any one pipe, 1 tile). No one can wall a corridor, active or not.
- **Spot request:** if a dormant (14+ days) estate is next to an active player, that player can request the spot
  for a friend or a mover. The dormant owner gets a notification and 72 h; without an answer, the estate goes to the
  Quiet Isles early. Fine, because the Quiet Isles lose nothing and the return move is free. Max 1 spot request per
  player per week.
- Half-built eyesores: from day 14, a dormant estate's unfinished construction sites are wrapped in tidy scaffolding
  tarps (cosmetic), so the neighbourhood doesn't look abandoned; ivy stays as a gentle "they're away" signal.

### 3.4 Shared things that the leaver took part in

| Thing | What happens when the partner goes away |
| --- | --- |
| A contract with the leaver | Continues while resting; caretaker renews good ones while dormant; on sleep, the successor House offers the same contract at the same price |
| A toll pipe the leaver owned | Free hub pipe on sleep (exists) |
| A corp the leaver led | Leader passes to the most active officer at 14 d (exists); at 3 d if every officer agrees |
| A shared project (crew/corp build) | The leaver's share is resized away; what they delivered stays credited to them |
| A teleporter pair with the leaver | Keeps working while the estate exists; on sleep, Ana's end becomes a hub-bound teleporter until a new partner pairs |

---

## Situation 4 · The player who returns after a month

### 4.1 The return in three screens (under 60 seconds)

1. **While you were away** (one screen, 5 items max, not 200 inbox entries): "Era 2 began. Your estate earned
   38,400 g (banked). Ana tended your estate 17 times. Your two partners: Ana still active, Dara is on the Quiet Isles
   too. 3 neighbours are new." A 10-second timelapse of the district over the month (it grew; the hub is a Town now).
2. **Your estate is safe**: the estate on its island, complete, balloons ready. Nothing lost, all levels kept.
3. **Where do you want to land?** Up to 4 cards, each with neighbours (names, Levels, usual hours, lights-on days in
   the last week), land (deposits, terrain), and who you know there:
   - **Your old spot** (if free; reserved for you for the first 7 days of sleep, offered first after that if free)
   - **Next to a friend / old partner** ("2 free spots next to Ana")
   - **A lively spot** (most lights on, players of your Level and hours)
   - **The newest district** (fresh start among newcomers and other returners, high hub demand)
   Landing is free, takes 1 minute (not 1 hour: the return should be instant), and links to anyone who still lives
   there are offered back.

### 4.2 Banked earnings that are bounded

Today offline earnings stop when storages fill (often after a day). That makes a month away feel like nothing
happened. Proposal: from day 3 away, the caretaker sells what overflows the storages to the hub at 70% of the hub
price into a **savings jar**, capped at **7 days of the player's income** total. The returner always has a pile to
spend (the gift), but quitting never out-earns playing (playing gets 100% and no cap).

### 4.3 Catching up to the Era

- **Era jump kit** per Era missed: a free blueprint of a standard chain for that Era (e.g. "Steel line: 2 Iron Mines,
  Smelter, Coal, Steel Mill"), one free crate of its first building, and research for that Era's first 3 techs at
  the diffusion discount (exists) minus another 25%.
- **No timed pressure.** The kit never expires; no "boost for 24 hours" that forces logins.
- A "**Back!**" ribbon on the banner for 7 days. Neighbours can send a one-tap welcome (a postcard or a small gift
  crate, §1.3) and get +10 XP for it. The returner's first session ends with 2–5 "welcome back" postcards from
  neighbours or Houses (Houses always send one, so it's never zero).

### 4.4 Old partners are remembered

The game keeps a **partner history** (who you had links with, how long, how much). On return: "You traded with Ana
for 12 days. Land next to her?" and after landing, one-tap "relink with Ana" if adjacent.

---

## Situation 5 · The newcomer in week three among veterans

Risk: the world looks finished, every estate is huge, prices are set by Level 30s, the District Wonder is at 80%, the
leaderboards are unreachable. The newcomer concludes "I'm too late" in 20 minutes.

### 5.1 Placement: next to the right people

Default placement (when the newcomer doesn't choose) scores each free spot:
- +3 for each active neighbour within 10 Levels (or a **Host**, below) — someone to grow with
- +2 for a waiting **capped link** at one of its gates (§3.2) — an instant partner
- +2 for neighbours with similar usual hours (from the newcomer's timezone)
- +1 for a House neighbour (always a partner)
- −3 for a spot next to only sleeping/resting estates
On a quiet server this still finds the best available, and Houses make sure there's always someone.

### 5.2 Hosts (veterans who opt in to welcome newcomers)

- Players Lv 15+ can mark themselves **Host** (a lantern on their banner). Newcomers are placed next to Hosts first.
- A newcomer next to a Host gets a **welcome link**: the Host's estate buys the newcomer's surplus at hub +15%, the
  extra 15% **paid by the game, not by the Host**, for the newcomer's first 7 days, capped at 5,000 g total. So
  there's no way to funnel gold between alts through it.
- The Host earns **Host stars** when a newcomer they hosted reaches Level 10 *and* has played on 4+ distinct days
  (anti-alt-farming): a banner trim per star, a "Host of 10" title, a statue at the hub at 25. No gold reward.

### 5.3 Protection by rule

- A link between companies 10+ Levels apart has a **fair-price band**: the lower one sells at ≥ hub price and buys
  at ≤ hub price + 5%. A veteran can't fleece a newcomer.
- Newcomers can't be asked for contracts by players 10+ Levels higher for their first 3 days (they still see Houses'
  and Hosts' offers). No offer spam.

### 5.4 Catch-up that doesn't devalue veterans

- **Late-start XP:** XP ×(1 + days since chapter start / 14), max ×2.5, until the player reaches the active median
  Level − 5. A newcomer in week 3 reaches mid-game in about a week instead of three.
- **Tech diffusion** (exists, up to −50%).
- **Wonder share by effort:** personal Wonder reward tiers are measured as a share of *your own* income delivered, not
  absolute amounts, so a Level 6 delivering 20% of their output gets the same tier as a Level 30 delivering 20%.
- **Boards they can win:** "Rookies of the week" (founded in the last 14 days), "Growth this week" (% net-worth
  growth), "Prettiest estate under Level 10" (likes). Every week someone new is on a board.

### 5.5 The world that looks finished becomes inspiration

- **Blueprint gifts:** veterans can publish up to 3 blueprints as "Starter" in the district Showroom; newcomers place
  them as ghosts and build them piece by piece. The author gets likes and a "used by 14 newcomers" counter.
- On the newcomer's first zoom-out, the camera shows a Host's estate: "In two weeks yours can look like this."

---

## Situation 6 · Idle play: 5 minutes a day, at different times

Rules for every co-op feature (the checklist an idea has to pass):

| Rule | Number |
| --- | --- |
| No social action needs both sides online | 0 synchronous actions |
| Offers, requests, invites wait at least | 72 h |
| Shortest social timer | 12 h |
| Events last at least | 48 h |
| Push notifications | max 1/day, in the player's chosen window, digest form |
| A partner who doesn't answer | auto-declines politely after 72 h, the asker is told and offered a House instead |

Async co-op mechanics that fit 5-minute sessions:

- **Requests board:** "I need 2,000 bricks by Friday" posted on your gate; any linked partner (or a House) can fill it
  any time; standing contracts fill it automatically while both sleep.
- **Relay goals:** a crew goal split into steps that different people do at different times ("Ana delivers ore →
  Bob's smelter → Cem's steel to the Wonder"); each step's progress waits in a buffer storage on the link, so a
  morning player and a night player both see their part done and the other's part pending, with no waiting on each
  other in real time.
- **Standing orders on links:** "Always send Bob what he needs up to 500/h" instead of anything that needs answering.
- **A daily crew note:** one line each, visible on the crew panel the next time anyone logs in ("Built the smelter, send
  coal!"). Pre-written phrases + a free-text option for friends (crew-only, so moderation is the crew's own).

---

## Situation 7 · World population balancing and seasons

### 7.1 Liveliness per district

Each district has a **liveliness** target: at least 12 of 24 estates "alive" (a human active in the last 3 days, or
a House).

| Active humans in district | Houses | What else |
| --- | --- | --- |
| 16+ | 0–2 (only Rivals) | Normal |
| 8–15 | up to 8 | Houses fill the gaps, move out for humans |
| 3–7 | up to 10, more active (Growers, Neighbours in need) | Quiet district: newcomers and movers are steered here first, the Guild gets a bonus |
| 1–2 for 7 days | up to 12 | The players get the free move offer with a free parcel (exists), **plus** "invite to this district" links for friends |

So 5 players on a whole server get one lively district (5 humans + 10 Houses), not 5 lonely corners. 2,000 players
get ~100 districts, Houses only where needed. Cost: Houses are tenants with a growth script; ~10 per district is
already what exists.

### 7.2 Hour clustering (a creative one)

Placement and the "lively spot" suggestions prefer players with overlapping **usual hours**. Over a chapter,
districts drift into gentle "rhythms": a district of evening players, one of early birds, one of night owls. Not
enforced, just a +2 in the placement score, and shown on the placement screen ("This district is busiest in the
evening"). The 3 a.m. player finds the other 3 a.m. players.

### 7.3 Chapter start: Homecoming

At every chapter start (the persistent world's "season"):
- Everyone on the Quiet Isles gets one message: "Chapter N begins. Land in one tap."
- A **Frontier district** opens for newcomers and returners together, so they start on equal footing with each
  other instead of in the shadow of veterans.
- **The Great Migration** (opt-in, once per chapter, for active players too): tick "re-place me among active players
  like me" and you're moved with your whole estate (free, crates where terrain doesn't fit) to a district formed from
  others who ticked it, matched by Level band and hours. Friends/crews/corps move as a block. This reshuffles dead
  neighbourhoods into lively ones without forcing anybody.

---

## What must NEVER happen (anti-patterns that make people quit)

1. **A dead pipe.** A link that delivers nothing, with no word why. Every zero has a sentence.
2. **"Both online."** Any reward, trade or goal that needs two humans at the same time.
3. **Punished absence.** Decay, lost buildings, lost land, lost Levels, lost earned titles, a reset streak. Use
   cumulative counters ("days played this chapter: 23") instead of streaks that drop to 0.
4. **Solo walls.** A building, tech, Level or Wonder reward behind "have a human partner" or "be in a human corp".
5. **The leaver's shadow.** A quitter's estate blocking land, a corridor, a gate, a corp leadership, a crew goal or a
   Wonder tier for longer than 3 days of visible effect.
6. **The empty map.** Grey placeholder estates, "0 online", a hub that never changes. Show "lights on today".
7. **Guilt and pressure.** "Your workers miss you", "boost expires in 2 hours", more than one push a day, return
   offers with deadlines.
8. **Fake people.** Bots that pretend to be players. Houses are labelled, always.
9. **Newcomer fleecing.** A veteran buying a newcomer's goods below hub price, or spamming them with offers.
10. **The inbox wall.** 200 unread items on return. One summary screen, 5 items.
11. **Forced moves.** Moving an active player without their tap. (Only asleep estates move, and they move to safety.)
12. **A worse home without a choice.** A returner landing somewhere worse than they left, without having picked it.
13. **"You're too late."** Boards and Wonder rewards a week-3 newcomer can never reach.
14. **Quitting out-earns playing.** Caretaker and savings jar must always stay below active play (70% price, 7-day cap,
    10% fee).

---

## How it scores on the checklist

| # | Score | Why |
| --- | --- | --- |
| 1 Friends | ++ | Crews shrink gracefully, tending, partner history, Great Migration moves crews as a block |
| 2 Choosing | ++ | Placement cards with lights-on, hours, Levels, waiting links |
| 3 Lonely | +++ | Houses with personalities, a growing hub town, Guild of Independents, solo parity at ~10% |
| 4 Leavers | +++ | Link states, successor Houses, capped links waiting for newcomers, spot requests, savings jar |
| 5 Early game | ++ | Hosts with game-paid welcome links, fair-price band, inherited links as first partner |
| 6 Late game | + | Rivals, Host stars, Great Migration; others' proposals carry the late game |
| 7 Async | +++ | Every rule sized for 5-minute sessions; standing orders, relay buffers |
| 8 Fair | ++ | Game-paid subsidies, caps, anti-alt conditions on Host stars, gift caps tied to own production |
| 9 Clear | ++ | One-sentence link states, sparkles, return summary |
| 10 Pipes | ++ | Every mechanism lives on a link/pipe: caps, handover, welcome links, relay buffers |
| 11 Scale | ++ | Houses only where needed; links keep networks small; no per-tick cost for presence (event lists) |
| 12 Seasons | ++ | Homecoming, Frontier district, migration is opt-in; existing Quiet Isles reused |
| 13 Glue | ++ | Tending, postcards, welcome-back notes, Host titles, plaques in the hub town |
| 14 Buildable | ++ | Ships in small steps on existing code (tenants, caretaker, Quiet Isles, relocation) |

## Build order (each step playable on its own)

| Step | What | Builds on | Days |
| --- | --- | --- | --- |
| 1 | Link states with one-line reasons; capped links fall back to the hub; "connect to waiting link" offer for whoever lands on the spot | Links / today's partners panel | 1 |
| 2 | Successor House inherits the leaver's link profile; 72 h notice in the inbox | Tenants, Quiet Isles | 1 |
| 3 | Return flow: one summary screen, 4 landing cards, 1-minute landing, savings jar, old-spot reservation | Offline report, relocation | 1–2 |
| 4 | Presence: lights on, "lights on today" count, change sparkles, usual hours on banners | Activity stages, build events | 1 |
| 5 | Houses with archetypes: growth script, price stance, letters table, Friendly status, the Rival | Tenants | 2–3 |
| 6 | Crew resizing, autopilot reinvest, tending, win-back messages | Friends/crew design, caretaker | 1–2 |
| 7 | Newcomer placement score, Hosts, welcome link, fair-price band, late-start XP, rookie/growth boards | Placement, leaderboards | 2 |
| 8 | Hub town growth stages and citizens' order board | Daily orders, hub art | 2 (+art) |
| 9 | Guild of Independents, liveliness targets, hour clustering, Great Migration, Frontier district | Corporations, district creation | 2–3 |

Steps 1–3 are the ones that stop people quitting; do them first. Steps 4–5 stop the world from feeling empty.
