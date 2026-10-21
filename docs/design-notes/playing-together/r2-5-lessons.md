# Round 2 · The player psychologist and game historian

I judge every idea by two questions: **what will the player feel at that moment**, and **what happened in a real game
that tried something like it**. I read all six round-1 files, the brief, `docs/TradingRethink.md`, and the parts of
`docs/GameDesign.md` and `docs/ImplementationStatus.md` that matter here. Already built: tenants, activity stages, a
Caretaker, the Quiet Isles (estates packed and unpacked with their layout), weekly relocation and blueprints. So much of
what follows extends existing code rather than inventing new systems.

File shorthand: **W** = 1-wild, **F** = 2-friends, **L** = 3-lonely, **E** = 4-economy, **P** = 5-lessons (mine),
**O** = 6-outsider. "F#10" means idea 10 in 2-friends.

---

## 1. The five weakest ideas, and how they fail

### W1. Rewards that count how many friends turned up

**Which ideas:** F#7's diversity bonus (weekly crew reward ×1.0 / 1.2 / 1.35 / 1.5 by number of contributing
members) and its contribution ring with one colour per friend; O#9 Shift Log ("coverage bonus for every day somebody
was there"); W#16 the Clocktower (a boost while someone tended it in the last 8 h); O#29 the Night Market (one hour per
time zone, extra pay for goods delivered *together*).

**How it fails:** In week 2, Ben is away on a work trip. On Sunday the crew screen says **×1.2** where last week it
said ×1.35, and the ring has no amber in it. Everyone knows the missing 0.15 is Ben's. Nobody says it. Cleo
writes in the group chat: "Ben, can you just log in and drop some coal? 🙏". Ben logs in from the hotel, drops 40
coal, and feels like he clocked in. The next week he skips again, feels it again, and mutes the group chat. The
Clocktower does the same to the night owl: once the others rely on "her shift", her evening off has a cost for them.

**Where it went wrong before:**
- **Snapchat's Snapstreaks:** teenagers handed their logins to friends to keep streaks alive while on holiday, and
  the anxiety was widely reported.
- **Habitica:** in a boss quest, one member's missed tasks damage the whole party.
- **Hay Day's Derby:** neighbourhoods kick members who don't finish tasks.
- **Clash of Clans:** clans forced everyone into wars until the war-preference opt-out came.

**Why it fails:** Loss aversion turns a bonus for attendance into a penalty for absence, and the penalty can be
pinned on one person. That makes it guilt for the absent friend and resentment for the others, which are the two
feelings that end friend groups in games.

**Instead:** reward what people deliver, never who showed up. Give each player credit for their own share, give
perks that everyone in the station gets anyway, and never show one slot per member that can stay empty. If you want
to reward togetherness, make it a one-off party the first time a new combination happens ("Ben's coal, Ana's energy
and Cleo's sand met in the Yard for the first time"), never a weekly ratio.

### W2. Things only a group of humans can unlock

**Which ideas:** O#15 Quartet Recipes ("four station goods from four different people; caretakers can't supply").
Partly W#39 (profession bonuses only when trading with a *different* profession) and W#13 (relay goods through three
different companies), when NPCs can't fill a step.

**How it fails:** Iris plays alone on a quiet server. The season's trophy item is on the board every day, and she
can never make it. Or take Kettle Works after Ben quits: they are three humans now, and the Quartet trophy they were
chasing has become impossible "because of Ben". The blame lands on a person who isn't even there. Then the first
optimiser in the district makes three alt accounts and makes Quartets alone. The mechanic now rewards cheating and
punishes honest small groups.

**Where it went wrong before:**
- **Destiny 1's raids** had no matchmaking, so many players never saw the best content, and Bungie added Guided Games
  in Destiny 2.
- **FarmVille** walled buildings behind a number of "neighbours", which produced spam, "add me" groups and fake
  friends.
- **Egg, Inc.** co-ops filled up with alt accounts and "sleepers".

**Why it fails:** It blocks autonomy and competence with something outside the player's control. That produces
learned helplessness, and it makes alt accounts worth having.

**Instead:** humans make things better, faster or prettier, but never possible in the first place. A Quartet item
made with a lodger's input is a plain version; made by four humans, it gets the four-colour banner and a plaque. You
get recognition, not access.

### W3. A map that moves under you

**Which ideas:** W#7 the Accordion map (repacked every night or season so active estates slide together), W#5 sky
islands that drift closer or away depending on connections, W#4 rafts that drift downstream when inactive, W#3 estates
as train cars on a loop.

**How it fails:** Cleo opens the game on the bus. The friend she pipes coal to is now on another side. Her
hand-laid corridor pipe was re-routed into a shape she never built. The stranger she always waved to has been folded
into a "sleeping quarter". She feels disoriented first, then that her work was edited without asking.

**Where it went wrong before:**
- **Guild Wars 2's megaserver (2014)** mixed map instances dynamically, broke up server communities (role-play hubs
  especially), and ArenaNet had to group players by guild, party and language.
- **WoW's cross-realm zones (2012)** led to "I never see the same people twice" and the realm communities faded.
- **Forge of Empires** reshuffles neighbourhoods regularly, so nobody bonds with their neighbours.
- The train cars would also leave nowhere fixed for trunk lines, the late game.

**Why it fails:** In a builder, players form an attachment to their place. They build for the view, the neighbours
and the line of their pipe (the IKEA effect). Moving that without consent erases it.

**Instead:** moving is always the player's own choice: a one-time event with balloons and a ceremony (the existing
relocation, made easier). The world never moves an active player (L's anti-pattern 11 is right).

### W4. A land rush at the start of each season

**Which idea:** W#9 (a fresh map at season start, a 24-hour window, everyone plants flags).

**How it fails:** Season 2 opens on Tuesday at 18:00 UTC. Kofi in Sydney is asleep, and Ana's crew is at work. A
corporation of 30 veterans with a Discord plants flags on every ★5 coal station in the first hour, and an alt army
reserves seats. By morning, a newcomer's first impression is the leftovers. In a persistent world that one night
becomes a grudge for the whole season.

**Where it went wrong before:**
- **Ultima Online's** land rush covered the map with houses, and newcomers had nowhere to build.
- **FFXIV housing:** the 2018 Shirogane rush brought timer-camping and placard bots, until Square Enix moved to a
  lottery in 2022.
- **Travian:** the first days of a round decide it, and players set alarms for the server start.

**Why it fails:** Scarcity plus a deadline means anxiety and FOMO. It rewards being online at one particular hour,
which fails checklist 7, and the veterans' head start fails checklist 8.

**Instead:** land is never scarce, because new stations open on demand. Stations are balanced, with a complementary
land mix and no ★5 jackpots to fight over. Your position can be changed later. A season start is a homecoming week
you can join on any day.

### W5. Mechanics that pay you when your neighbour leaves

**Which ideas:**
- W#30 Adoption: a neighbour runs the abandoned estate as an annex and keeps a share.
- W#19 Seed colonies: the founder earns a share of the colony "while it lives".
- L §3.3 Spot request: ask a dormant neighbour to leave early so your friend can have the spot.
- E M11's shortfall fine: a seller who can't deliver pays the difference.

**How it fails:**
- With Adoption, Ana's best move is to hope her slow neighbour Dara quits, because an annex is worth more than a
  partner. When Dara comes back after three weeks, her trades have been rearranged by someone else. She "gets her
  estate back", but it no longer feels like hers.
- With Spot requests, Dara's first message from the game is "your neighbour wants your spot", which tells her she
  isn't wanted at exactly the moment the game wants her back.
- With the shortfall fine, Ben's holiday costs him gold, and the first thing he sees when he returns is a fine.

**Where it went wrong before:**
- **Travian:** an inactive player becomes a "farm" for the neighbours.
- **Ultima Online:** collapsing houses became looting events, with crowds waiting for a house to fall.
- **Eve Online:** in 2010 Goonswarm lost its space because a sovereignty bill went unpaid; any upkeep that depends on
  one person being present is a trap.

**Why it fails:** Misaligned incentives make neighbours root against each other, and the return message reads "you
were replaced", which kills any win-back.

**Instead:** absence benefits nobody and costs only the absent player's own growth. An NPC (lodger or hub) fills the
gap, and coming back is a gift.

### Keep, but reframe (right mechanic, wrong feeling)

- **E M7 personal hub saturation.** Economically right: nobody can crash anyone else's price. Psychologically
  wrong as written: "the hub is full of your ingots, the price halves" reads as a penalty for success. **WoW's rested
  XP** is the classic case. In beta, XP dropped after long play ("fatigue") and players hated it. Blizzard flipped the
  frame (normal speed became "rested bonus ×2", with the same maths) and players loved it. So present it as the
  **Hub Order**: the hub's standing order buys your first *N* per hour at the full price, a bar you fill. Above that,
  goods sell at the open-market price. Same numbers, opposite feeling.
- **E M8 tier-3 import cap at 4.0.** Fine on a busy server. On a quiet one it is Eco's trap (forced dependency on
  people who aren't there). Lodgers/Houses at Era 2+ must sell tier 3 at about 3.0, so solo is slower, not walled.
- **L's Rival House** ("always 5–10% ahead or behind you"). That's rubber-banding, and players see through it within
  days (as in racing games). After that every win feels fake. Rivals should be real (your league) or honest (a named
  House with a fixed growth plan).
- **E M9 trunk tolls set by the owners, plus upkeep.** This turns into passive rent for veterans and toll wars, and
  upkeep paid by the one person who logs in (the Eve bill). Keep the lanes, let the game fix the toll, drop upkeep.
- **Signs of absence (F's sleeping cat and ivy, L's "resting 4 days").** Fine for your own crew if they stay vague.
  Never show day counts to strangers. L's "Ana tended your estate 17 times" is the best win-back line in all six files.
  Keep it, but tending must never become a streak for Ana.
- **My own round-1 daily tune-up (+10% for 8 h).** A stat buff given daily becomes Forge of Empires' "aid-back"
  etiquette: spreadsheets of who motivated whom, and "aid back or be removed". I am dropping the buff. Favours become
  gifts and cosmetics.
- **My own Station Cup "per active member".** Dividing by members makes a casual friend a drag on the score, which
  means pressure to kick them or to move out (as with Clash war-weight drama). Instead, "your station's best three
  contributions count", so a quiet fourth member costs nothing.
- **O#16 Station Rotation** (roles rotate every season). It breaks mastery and identity ("I'm the glass person").
  Make rotation an option, never forced.

---

## 2. The five strongest ideas, and why

### S1. One safety net for the lonely, the absent and the gone: caretaker → lodger copies the trade → deed

**From:** F#9/#11 (Lodgers, the Lodger that copies the last 7 days of trade), L §3.2 (the successor House inherits
the link profile; a human who later takes the seat is offered every link), W#15 (steward), O B (caretakers), and the
existing Caretaker and Quiet Isles.

**Why it's strong:** one mechanism covers checklist 3, 4 and 7, and it removes the three worst feelings at once:
- **Guilt:** my absence hurts nobody.
- **Resentment:** nothing stalls.
- **Shame:** nothing is lost.

The killer detail is L's. When a human moves into a lodger's seat, they inherit the lodger's links: "Ana already pipes
coal to your west gate. Connect?" A newcomer's first partner is then a real, active human who wants them.

**History:** SimCity 2013's public regions show the opposite. Abandoned cities stayed in the region forever, and
their neighbours suffered. FFXIV shows the right way: an inactive home is packed up and returned, never destroyed.

### S2. The invite link that holds a furnished seat, and makes the late friend the missing piece

**From:** F#1, #2 and #10, W#44, E M10.

**Why it's strong:** It turns the late friend's "I'm a week behind" into "I'm exactly what the crew needed".
- His seat is the one the crew lacked (copper).
- The lodger hands over its works intact.
- His first mine replaces the crew's hub imports.
- Tailwind closes the gap in days.

He becomes a provider, not a passenger, which is competence plus relatedness. Recruit-a-friend programmes (WoW,
others) give the latecomer XP and make him a passenger. Travian's quadrant choice puts friends only roughly near each
other. This idea fixes both.

### S3. Two-ended links with gate tanks, honest states and an automatic fallback

**From:** W#21 and #14, L §3.1 (five link states, "every zero has a sentence"), E M5, `TradingRethink.md` option A.

**Why it's strong:** It turns the original failure inside out:
- **Consent** makes the trade a relationship.
- **The tank** makes it asynchronous.
- **The one-line state** makes it clear.
- **The fallback** removes dependency.
- **Auto-routing through shared corridors** removes blocking.

**History:** Eve's courier contracts with collateral show that explicit, guaranteed bilateral deals let strangers
trade without fear. The district mega-network was the counter-example.

### S4. Home goods and the price ladder (comparative advantage, never a gate)

**From:** E M1–M2, F#2 (seats with complementary land, so choosing a seat is choosing a role).

**Why it's strong:** It gives a reason to trade that fits in one sentence ("your land makes two things brilliantly;
neighbours pay best, your station next, the hub last"). It makes the choice of position meaningful, and the ★2
deposits of everything keep solo play complete.

**History:** Catan's whole game is this (numbered hexes, plus the bank at 4:1 as an always-available fallback). Eco
shows the alternative, forced specialisation, collapsing on quiet servers.

### S5. Pack and land: every place is undoable

**From:** L §4.1 (the return in three screens, landing cards, one-minute landing, bounded savings jar), W#6 (the
moving van) and #29 (time capsule), on top of the existing Quiet Isles and relocation.

**Why it's strong:** A choice you can undo is a choice you dare to make. This one mechanism solves:
- the friend who started somewhere else,
- the dead station,
- the bad neighbour,
- the returner,
- consolidating quiet servers,
- the testers' migration.

**History:** Animal Crossing shows what a hard return feels like: weeds, villagers saying they missed you, and
players avoiding the game for months.

**Close runners-up:**
- **L's Houses with personalities** and **"lights on" presence:** the answer to empty worlds, as Animal Crossing's
  villagers and Hay Day's NPC customers show.
- **F#15's trunk line built segment by segment in the builders' colours, with adopted pump houses:** pride in public
  works, as in Minecraft's nether highways and Death Stranding's roads.
- **L §5.2's Hosts with a welcome link paid by the game:** veterans help without it costing them, and no gold can be
  funnelled through it.
- **E M12's sustained-flow stage:** a real "we did it together" moment that is still asynchronous.

---

## 3. The convergence on 4–6 estates around a shared Yard: history, and is it groupthink?

Five of six files arrived at the same shape: W#1 hamlets, W#2 stations, F's Quads around a Yard, E M4's junction
market, O A's Junction, and my own stations. Who did this before?

| Game | The shape | What happened |
| --- | --- | --- |
| **Stardew Valley / Valheim co-op** | Up to 4 cabins around one farm (8 since 1.6) | Loved by friends. A shared place for a *chosen* group works beautifully. Almost never used with strangers |
| **SimCity (2013) regions** | Several city plots around shared Great Works, trading power, water and services | The closest analogue. Private regions with friends were the best part of the game. Public regions with strangers filled with abandoned cities, unfinished Great Works and neighbours' problems spilling over. Plots were criticised as cramped |
| **FFXIV housing wards, WildStar neighbourhoods** | Strangers' homes clustered around a shared plaza | Next to each other, but next to no interaction. Proximity alone does not bond strangers |
| **WoW Garrisons (2014)** | A personal base with everything in it | Capital cities emptied and players felt isolated. Blizzard later called it a mistake. A private base drains the shared place unless the shared place holds what you need |
| **Travian** | Villages on a map, travel time matters | Proximity *plus* mutual benefit (defence, trade time) made near strangers into alliances. Proximity bonds when it pays |
| **Eve Online wormhole corps** | 5–30 players living in one system | Very strong bonds, but self-selected |
| **Monopoly Go partner events, Egg, Inc. co-ops** | 2–5 players build one shared thing | Works when partners are chosen and active. With random partners the main complaint is "lazy partners" |
| **Clash of Clans, Hay Day, FoE guilds** | Groups of 30–50 with no geography | The 30–50 layer works fine without space |

**Verdict.** The convergence is right for the friend layer (Dunbar's 5, Stardew), and it suits the pipe fantasy
(short visible pipes into a shared square). It becomes groupthink in five places:

1. **The number 4 comes from geometry, not from people.** Couples are 2, friend groups are often 5–6. All the files
   took 4 because today's district has 2×2 corners. Fix: stations of 4 plus adjacent halts of 2, joined when a
   crew needs 5–6.
2. **Strangers in a Quad are not a crew.** F's migration gives strangers a "nameless Quad crew". FFXIV shows that
   a shared address isn't friendship. Fix: a station is a *neighbourhood* (shared Yard, Yard projects, mild
   interdependence). A crew is only the people who chose each other, and the game *suggests* forming one when
   strangers have traded for a week (L §1.4).
3. **Most players are solo, so most seats could be bots.** Without consolidation, stations become three lodgers and
   one human, which is "playing with bots" (Fortnite's bot lobbies: welcome for beginners, a letdown when veterans
   noticed). Fix: placement fills stations with humans first, and lodgers are labelled and clearly weaker than people.
4. **Too many group layers.** Across the files: crew, Quad, Duo, Street, corporation, district, server, with a
   project for each. An idle player can hold three. Fix: **crew** (people), **station** (place), **world** (hub,
   Wonders, leagues), with corporations as optional "crews of crews".
5. **The Yard is the mega-network in miniature.** It is acceptable because it is bounded (≤ 6 estates), visible and
   chosen (you lay the Yard pipe yourself; strangers aren't joined by default). It must never chain to other Yards
   except through explicit trunk lines.

One more lesson from the Garrison: **the Yard must be worth visiting**, with the best prices, the projects and the
board. But it must not empty the hub town and the world, so the Wonders, trunk lines and leagues stay outside.

---

## 4. The revised design: **Stations, crews and caretakers**

### 4.1 Six rules (every mechanic below obeys them)

1. **Together makes you better, never stuck, guilty or prey.** People add about +10–20% and warmth, never access.
2. **Absence costs nobody anything except the absent player's own growth.** An NPC fills every gap, in seconds.
3. **Nobody ever presses a button that feels like kicking a friend.** The game handles seats; people only invite.
4. **Every choice can be undone.** Seats, crews, links and positions can all be changed later.
5. **Positive-only contact between players.** Gifts, links, thanks and races. Nothing that takes, breaks or blocks.
6. **Every zero has a sentence.** No silent dead pipe, no unexplained empty number.

### 4.2 The words a player learns (eight)

**Estate** (your land), **crew** (people you chose), **station** (where you live: 4 seats around a Yard), **halt**
(a small station of 2), **Yard** (the shared square), **link** (a pipe from your gate to one other company),
**lodger** (a labelled NPC neighbour), **trunk** (a long, shared pipe line). Corporations, Wonders and the hub
already exist.

### 4.3 The pieces

**A. Land with home goods (from E M1, F#2).** Every estate has two home goods (★5 and ★4) and small ★2 deposits of
every basic. A station's four seats are generated to complement each other: an energy seat, an ore seat, a sand/water
seat and a materials seat. Choosing a seat is choosing a role, and solo can still make everything, just slower.

**B. Stations and halts (geometry from F).** In today's district, the four corner 2×2 groups become **stations**
(4 seats around a Yard carved from the inner-corner parcels and the corridor crossing), and the arms of the cross
become **halts** (2 seats). New districts open whenever free seats run low (Frontier districts), so land is never
scarce.

**C. The station board and placement (F#2, L §5.1 and §7.2, P#3).** Choosing where to live is a map of station cards
showing:
- seats free or taken, with banners;
- **lights on** (played today, this week) and **usual hours** ("busiest in the evening");
- the land: home goods, river, wind;
- what the station sells and wants;
- the hub's specialty this week;
- whether a link is **waiting** at a free seat ("Ana pipes coal to this gate").

Two default buttons: **"Pick for me"**, which scores free seats on active neighbours within 10 levels, waiting
links, similar hours and complementary goods, and **"Pick for us"** for a crew. Humans fill stations before lodgers do.

**D. Crews from minute one (F#1, E M10, P#4).**
- "Start with friends" makes a crew name, emblem and colours in 15 seconds, plus a share link.
- A crew is 1–6 people, with separate wallets, always.
- Crew colours appear on gates, pipe stripes and pellets.
- There is a crew chat (pre-written phrases plus free text inside the crew).
- Crew links are pre-approved.
- A crew of 5–6 takes a station plus the halt next to it, joined by a short Street pipe.
- A crew can later join a corporation as a "house" inside it.

**E. The invite link holds a furnished seat (F#2 and #10, W#44).**
- Free seats of a crew's station are **held for the crew** as long as any member is active (up to 21 days). Nobody
  has to remember to renew them.
- A lodger keeps each held seat warm and builds modest works there (up to about Level 5).
- When the friend arrives, the lodger moves out with a goodbye note and hands over its works intact.
- The newcomer gets **Tailwind**: +100% XP while more than 3 levels below the crew's median, and parcels at half
  price up to the crew's median parcel count.

**F. The Yard (F#5–#6, E M4, O A).**
- Each estate has a **Yard gate**. Laying the 6–20-tile pipe from it to the Yard is the player's own choice, and
  Loady suggests it at about minute 12.
- Everything you have spare goes to the Yard after your own consumers, contracts and links, and before the hub.
- Inside the Yard, supply and demand are matched pro rata, at **one Yard price, the same number for seller and buyer
  (first guess 1.6× hub base; the hub buys at 1.0 and sells at 2.5)**.
- The Yard board shows every trade as two names ("Ben → Ana · coal · 1,200/h"), and pellets carry the seller's colour.
- At most 6 estates are in one Yard (station plus halt), so the solve is O(6).
- Strangers in a station are in the Yard only if they pipe to it.
- **The first time two players' pipes reach the Yard, both get the Handshake:** fireworks and a small gift.

**G. Links to anyone else (TradingRethink A, E M5, L §3.1).**
- A link is a pipe from your gate to one other company's gate, routed through shared corridors that nobody can
  block, and it is never transitive.
- The receiver accepts (lodgers and Open gates accept automatically).
- The seller sets a price inside the fair band (Auto, Friend and Premium presets).
- Every link has a **gate tank** (2 h of its flow) and an **honest state** (live, resting partner, caretaker,
  handed over, or capped with a hub fallback), each with one sentence.
- Every link falls back to the Yard, then the hub, the moment a partner's delivery drops.
- **Trust** grows by one level per day of trade, up to 10, worth up to −5% loss and +5% capacity (E M6). A caretaker
  keeps it alive.

**H. The Hub Order (E M7, reframed).** The hub keeps a standing order per company and resource: your first *N* per
hour at the full price, shown as a bar you fill. Above it, goods sell at the open-market price (half). Nobody's
dumping moves anybody else's price. The bar reads as a bonus you fill, not a cut you suffer.

**I. Wishes (F#8, P#8, W#20).**
- Anyone can pin up to 3 Wishes at their Yard gate ("200 copper ingots by tomorrow evening").
- Station neighbours, linked partners and lodgers can fill them asynchronously, or with a one-tap **standing order**
  ("my spare coal goes to Cleo first for 24 h").
- At the deadline, a lodger fills what's left at the hub price. **Nobody is ever let down.**

**J. Yard projects (F#7 minus the multiplier, E M12–M13).**
- The Yard Depot shows **Station Orders**, each at most about 45 minutes of one member's production.
- Orders grow the Yard: Water Tower → Yard Storage → Workshop (+5% for all residents) → Landmark.
- The Landmark's top stage is a **sustained flow** ("600 steel/min for 6 hours"), which tanks buffer, so it works
  asynchronously.
- Perks go to everyone in the station.
- Personal credit (gold, XP, your name on the plaque) is **by your own delivered share**, so a quiet member costs
  nobody anything.
- The display shows the total, plus a **thank-you line** naming who delivered this week. There are no empty slots,
  no per-member bars and no multiplier.
- Lodgers trickle in about half a member's worth, so a station with one human still finishes.
- A one-off **first meeting party** happens when three different players' goods meet in a project for the first time.

**K. Favours that can't become chores (L §1.3 and §2.3, P#5 minus the buff).**
- **Gift crates:** one per giver per receiver per day, worth at most 1 hour of the giver's production of one good.
- **Postcards:** 20 pre-written lines and stamps.
- **Tending** a resting friend's estate: cosmetic flowers and a swept path, +20 XP for the visitor, no stat buff, and
  no streak count for the visitor.

**L. Presence without numbers (L §1.3, F#4).**
- Lit windows, "lights on in 11 estates today", sparkles on what changed since your last look, usual hours on
  banners.
- Live cursors when two crew mates happen to be online together (a delight, never a requirement).
- Strangers never see "last online 9 days ago". Crew mates see only "resting".

**M. Lodgers and Houses (L §1.1, F#9).**
- An empty seat is a **lodger**: a named NPC company with a cog badge (never pretending to be a person), a
  personality and a short letter now and then.
- Lodgers trade in the Yard, post small Wishes and contribute to projects.
- They are capped at about 30% of an active player's trade, and their level follows the server (median active level
  − 2), so they never become irrelevant and never outgrow humans.
- From Era 2 they sell tier-3 goods at about 3.0, so solo is never walled.

**N. The absence ladder (existing stages, plus F#11 and L §2–3).**

| Away | What changes | What others see |
| --- | --- | --- |
| 0–3 days | Nothing | Nothing |
| 3–14 days | Estate runs, links and standing orders continue; overflow sold into a **savings jar** at 70%, capped at 7 days of income (L §4.2) | Crew: "resting". Strangers: nothing |
| 14–30 days | The Caretaker (exists) becomes a **copy-trade lodger**: it keeps the last 7 days of trades at the same price, renews good contracts, keeps trust | Link state "caretaker" with one sentence |
| Day 30 (72 h notice to partners) | The estate is packed onto the Quiet Isles as it is (exists). A successor lodger takes the seat and **inherits its link profile** (L §3.2). For a crew member, the seat stays the crew's for 30 more days, then opens to newcomers **by itself** | The crew sees the friend's banner move to the **Crew Wall**: "Ben can always land next door" |

Nobody in the crew ever decides to "replace" Ben. If his seat is taken when he returns, he lands in the nearest seat
(the halt, or the next station) and gets a free Street pipe to his crew's Yard.

**O. The return (L §4, F#11).** Three screens in under a minute:
1. "While you were away", in five items, with a 10-second timelapse and who tended your estate.
2. Your estate is safe, complete.
3. Landing cards: your held seat, next to your crew, a lively station, or the Frontier.

Landing takes one minute. Each missed Era brings a free **jump kit** (the chain's blueprint and the first building).
A "Back!" ribbon lets neighbours send one-tap welcomes, and lodgers always send one, so it's never zero.

**P. Moving house (exists, made friendlier, W#6).** Moving is free in the first 3 days, then free once a week, with an
hour in transit. Crews move as a block. Moving is the player's own act and has a ceremony (balloons, a farewell
postcard to the old station).

**Q. Newcomers among veterans (L §5).**
- **Placement** next to people of a similar stage, or in the Frontier.
- **Hosts:** opt-in veterans whose welcome link buys the newcomer's surplus at hub +15%, **paid by the game**, capped
  at 5,000 g. Host stars only count when the newcomer has played on 4+ different days (no alt farming).
- **A fair-price band** for links across a 10-level gap.
- **No contract offers** from players 10+ levels higher in the newcomer's first 3 days.
- **Late-start XP** (×(1 + days since chapter start / 14), up to ×2.5) until the newcomer reaches the median − 5.
- **Personal Wonder tiers** measured as a share of your own income.
- **Boards a newcomer can win:** Rookies, Growth this week, Prettiest under Level 10.

**R. The late game (F#15, E M9/M12/M15, W#22, O C).**
- **Trunk lines.** A crew (or a corporation) builds a steel trunk from its Yard to a wilderness deposit or another
  station, one segment at a time. Each segment carries the builder's colour stripe, and members can **adopt** a pump
  house; an unadopted pump still runs at the base level. A trunk carries **lanes** (at most 20): each lane is a
  link between two named companies, so trades stay direct and the trunk only carries them. Spare lanes are rented at
  a **game-fixed 2% toll** that goes to the builders. There is no upkeep. If every builder is asleep, the line
  becomes a public hub line.
- **Discoveries.** A rich deposit appears in the wilderness, announced 6 h ahead and lasting at least a week. The
  first three trunks to reach it get a marker on the map.
- **The Landmark's sustained-flow stage**, then the District Wonder and the Era Wonder (exist), fed by trunks or cargo.
- **Mastery titles** per resource ("Master Glassmaker of Riverside").
- **Leagues** of about 25 companies at a similar stage, plus a **Station League** in which the station's best three
  contributions count.
- **Corporations** as crews of crews: the non-spatial 30–50 layer that spans stations, linked by trunks.

**S. Chronicle and postcards (P#11, F#12).**
- A weekly **Crew Story** postcard: an isometric snapshot with the banner and three highlights, shareable outside the
  game with an invite seat attached.
- A daily station gazette that writes itself.
- Names on every plaque, pipe segment and pump.

### 4.4 A station, drawn

```
      Ana · Windy Heights (energy)        Ben · Coal Valley (coal, stone)
   ┌──────────────────────────────┬──────────────────────────────┐
   │   your estate, 15 parcels     │                              │
   │                       gate ●══╪══● gate                      │
   │                          ╔════╧════╗                         │
   │                          ║  YARD   ║  Depot · board · tower  │  ══ Street pipe ══ halt (2 seats)
   │                          ╚════╤════╝                         │
   │                       gate ●══╪══● gate                      │
   │   Cleo · Riverside (sand)     │  Copper Gulch: held for a     │
   │                               │  friend · lodger "Bertha"     │
   └───────────────────────────────┴──────────────────────────────┘
   Links to anyone else leave from the outer gates through the corridors; trunks start at the Yard.
```

---

## 5. Play-throughs: what each player FEELS

### (a) Three friends start tonight, a fourth joins on day 8, one quits on day 3

**Evening 1, 20:00, Ana's phone.** Ana taps **Start with friends** and makes "Kettle Works" (kettle emblem, teal and
copper) in about 15 seconds, then drops the link in the group chat. *She feels like the host of a small party: a little
proud, a little nervous that nobody will come.* The preview in WhatsApp shows the banner and "3 seats free".

**20:03.** Ben opens it on his phone and Cleo on her laptop. The station board shows the stations, with **Ashford NE**
glowing as recommended: fresh, next to an active crew, lights on in 9 estates today. Each drops a pin on it. The seat
cards say "Windy Heights: energy", "Coal Valley: coal and stone", "Riverside: sand and water", and "Copper Gulch:
copper". Ben says "obviously coal". *The choice is fast, a bit of banter, and each has a role before anyone has built
anything: identity before effort.* Copper Gulch shows "held for a friend · lodger: Bertha's Brickworks".

**20:05, Settle.** Three balloons land at once, and the banner unfurls in the Yard. *This is the moment that sells the
game to a group: "we live here".*

**20:06–20:18.** Each plays their own tutorial (wind, a mine, a Trading Post, first gold). Nothing waits for the
others. They see each other's cursors hopping next door. *They feel company without coordination, like cooking in
the same kitchen.*

**20:12.** Loady: "Lay a pipe from your Yard gate to the Yard." Ben is second, and the **Handshake** fires: both pipes
flash in their colours and his coal pellets start rolling towards the Yard. *A small, physical proof of being
connected, and the pipe fantasy and the friendship meet in one animation.*

**20:19.** The first Station Order (the Water Tower foundation) is finished with Ana's energy, Ben's stone and Cleo's
planks, and Bertha chips in 40 stone "for the young ones". The **first meeting party** plays: three goods from three
people met for the first time. *They feel "we did a thing together", and they log off at a high point (peak-end).*

**Day 2.** The morning report: "While you slept: Ben's coal kept your Smelter running (1,240 coal through the Yard
at 1.6)." Cleo pins a Wish, "coal for my Glassworks". Ben sees it on the bus and taps "my spare coal goes to Cleo
first, 24 h". *Helping costs Ben ten seconds and makes him feel useful. Cleo feels looked after, not indebted, because
the Yard price is fair and fixed.*

**Day 3, Ben stops.** Nobody knows yet, Ben included. His estate keeps producing and his Yard pipe keeps delivering.
- *Ana and Cleo feel nothing, which is correct.* Nothing has broken, so there is nothing to feel bad about.
- **Day 6:** the crew panel shows "Ben · resting". Ana tends his estate once (flowers on his steps, +20 XP). *That is
  a small gesture of care, and it costs Ana nothing.* There is no counter telling her to do it again tomorrow.
- **What the design deliberately avoids:** there is no ×1.35 → ×1.2 and no gap in a ring. The Water Tower order
  shows "Thanks this week: Ana, Cleo, Bertha".
- *Ana is honestly disappointed that Ben stopped. The game can't fix that, but it doesn't amplify it.* Her chain still
  works because Ben's coal still flows, and from day 17 (two weeks after he stopped) the copy-trade lodger keeps the same trade at the same price.

**Day 7.** The weekly Crew Story postcard shows "Most coal ever: 84k (Ben)", and Ana posts it in the group chat. *Ben
sees his name doing well and feels a pull back, not a reproach.* Ana tags Dev: "your seat is waiting, Bertha is getting
old". Dev's seat is still held, and nobody had to renew it.

**Day 8, Dev arrives.** Dev opens the old link. The station card reads "Kettle Works · 2 active, 1 resting · the
station buys 1,100 copper a day from the hub". *Dev expected to feel late.* Then:
1. **Bertha moves out** with a cake on the Yard bench, and her Copper Mine, Smelter and wind turbine (Level 5)
   become his.
2. **His first copper replaces the hub imports.** He sells at 1.6 instead of 1.0, Ana and Cleo buy at 1.6 instead of
   2.5, and the board says "Dev's copper saves the station 840 g/h".
3. **Tailwind** (+100% XP) carries him from Level 1 to Level 11 by day 12.

*What Dev feels is "they needed me", and that is the opposite of "I'm behind".* Ana and Cleo each send a housewarming
crate (capped by Dev's level). *Giving feels better than receiving, and the cap stops it feeling like charity.*

**Day 33 (30 days after he stopped).** Ben's estate is packed onto the Quiet Isles with its whole layout, and his banner moves to the Crew Wall.
The 72-hour notice to the station read "a lodger will keep Ben's coal trade at the same price". A copy-trade lodger,
"Hollow & Sons", keeps exactly the coal flow Ben had. *Ana's Smelter never notices. Nobody had to decide to "replace"
Ben.* After 30 more days the seat opens to newcomers by itself, with the line "Ben can always land next door". (His
return is play-through e.)

### (b) A solo player on a server with 5 players

Iris installs at 02:40 her time. The server has five humans.

- **The world isn't dead.** Placement has put all five humans in the same district: three in Ashford NE (Kettle
  Works), and the other two in stations next to it. Iris taps "Pick for me" and gets **Ashford SE**: one active
  human (Mo, Level 9, "usually here: nights") and two lodgers, Oakridge Coal & Sons and the Curio Guild, both with
  cog badges. The station card says "lights on in 7 estates today". *She feels she arrived somewhere lived-in, and
  that someone shares her hours.*
- **Minute 12.** She lays her Yard pipe. Oakridge buys her surplus energy at 1.6 at once, and she gets the Handshake
  with Oakridge, a little smaller than with a human (an honest difference). *She is already someone's partner.*
- **Day 1.** The Curio Guild writes: "Your odd little batch of glass is in our museum now." The hub town (the
  Baker's oven) grows visibly from *her* bricks within a minute. *Her actions change the world even at 3 a.m.*
- **Day 2.** Mo fills one of her Wishes overnight; she sends him a postcard back. *A human she has never "met" did
  something for her, and nobody had to be online at the same time.* (Death Stranding's lesson.)
- **Day 5.** Iris's Station Orders still finish because the lodgers trickle in about half a member's worth. The Water
  Tower rises. *She is not carrying a dead group. The group is small, and it works.*
- **Day 8.** Mo and Iris have traded for a week. The game suggests once, "You and Mo could found a crew". They do
  ("Night Shift"). *A relationship that grew from proximity and mutual benefit, as in Travian, but gentle.*
- **What she never meets:** a Quartet she can't make, a reward ×1.0 because she's alone, or a "0 online" counter.
- **What she does meet, honestly:** lodgers are clearly weaker than people and say so in their cards. The game
  celebrates when a human moves in next to her, and she gets the waiting-link offer card for the newcomer, so she is
  the welcomer.
- **What she feels:** *a complete small game in a world with a pulse.* Without the lodgers' honesty, she would feel
  tricked later; with it, she knows they are props in a living set.

### (c) A newcomer in week 3, among veterans

Kofi joins on day 19. The district's veterans are Level 25–30.

1. **Placement.** The board offers two cards: **Frontier station Brightwater** (opened 2 days ago, five newcomers
   and returners) and **Ashford SW** (one free seat next to a Host, Lena, Level 27, with a link already waiting:
   "Lena's Steel Mill buys sand at your gate"). He takes Ashford SW. *He chose "next to someone who wants me"
   over "among other beginners". Both are good, and he chose.*
2. **First hour.** His welcome link to Lena pays hub +15%, paid by the game, not by Lena, with a visible line saying
   so. *He gets a real customer and owes Lena nothing.* Lena gets a Host star only if he plays on 4+ different days.
   She is pleased when he does. *Her pleasure is genuine, because it isn't gold.*
3. **The world looks finished.** The camera shows Lena's estate once: "In two weeks yours can look like this", and
   Lena's three published Starter blueprints are in the district Showroom. *It inspires him instead of intimidating
   him, because the gap now comes with a path.*
4. **Nobody can prey on him.** He gets no contract spam from players 10+ levels higher for 3 days, links across the
   gap stay inside the fair-price band, and the Hub Order means a veteran dumping sand can't move *his* price. *He
   never meets a predator, so he never learns to fear the others.*
5. **Late-start XP** (×2.4 on day 19) gets him to Level 15 by day 26. The Wonder tier is measured as a share of his
   own income, so he gets bronze with the same effort as Lena. The "Rookies of the week" board puts him 2nd. *He has
   something to win this week.*
6. **What he still feels:** veterans' estates are huge and he isn't. That's fine, and it's aspiration rather than
   humiliation, *as long as nothing compares him directly to them.* The leagues of about 25 companies at a similar
   stage make sure of that.

### (d) A griefer tries to block or exploit others

Vex wants to annoy people or to farm gold.

| Vex tries | What happens | What his victims feel |
| --- | --- | --- |
| Reserve every seat of a good station with alts | A crew can only hold the free seats of its own station (at most 3), for at most 21 days, and only while a member plays; alts count as members only after 4 distinct days of play; there are never too few stations, because new ones open on demand | Nothing: they never see a "full" station |
| Squat a corridor with pipes to block links | Links are auto-routed and share corridors as bundles; no corridor tile can be owned | Nothing |
| Build something ugly or flood a Yard he lives in | Yard buildings belong to the station: changes are logged with a name and undoable for 24 h, nobody can demolish a Yard building alone, and a free move out is always there | Mild annoyance, an undo, and a "move out" button they don't need |
| Flood the Yard to crash a station price | The Yard price is fixed; supply above demand is matched pro rata, and his surplus goes to his own hub order (open-market price) | Nothing |
| Cut a link at night to starve a partner | 1-hour notice, the gate tank (2 h) and the automatic fallback to the Yard, then the hub. The partner's state line reads "Vex paused; your Smelter gets ore from the Yard (−8%)" | "Oh, Vex paused." Then nothing, because the factory never stopped |
| Funnel gold to his main from alts | Prices stay inside the fair band; reselling hub imports loses money (buy 2.5, sell ≤ 2.5); gifts are capped at 1 h of the giver's own production; links between accounts on the same device are flagged and fixed at Auto; Host stars need 4 distinct days | Nobody is hurt; he gains about one weak partner's worth, which is not worth the effort |
| Leech Yard projects | Credit is by his own share; the perk is a public good he gets anyway | Nobody is short of anything, so nobody resents him |
| Toll-gouge on a trunk he built | The toll is fixed at 2% by the game; anyone may build a parallel line | Nothing |
| Offensive crew name or banner | Pre-made emblems, a name filter, reports, block lists; free text only inside crews | Rare; one tap to report and block |
| Spam link requests or Wishes | Rate limits (5 link requests an hour, 3 open Wishes), block list, closed gates | Nothing |

**The design insight:** in a cosy game, the griefer's real weapon is making someone *feel* hurt, not taking resources.
So the defence is mostly that **nothing ever stops**, and every pause is explained in one calm sentence. A griefer
whose attacks produce no visible victim gets no reward (the "no reaction" principle behind Death Stranding's
positive-only design).

### (e) A player returning after a month

Ben, the friend who quit on day 3, comes back on day 36. Over the month he got two short messages, both opted into
and both positive: on day 3, "your Brickworks earned 4,200 g, Ana is still building next to you"; on day 12, "Ana
tended your estate". He is opening the game expecting embarrassment.

1. **Screen 1, while you were away:**
   - Era 2 began.
   - Your estate earned 31,000 g; your savings jar has 9,800 g (7 days' worth, at 70%).
   - **Ana tended your estate 6 times.**
   - Dev joined Kettle Works; your coal trade was kept by Hollow & Sons.
   - A 10-second timelapse: the Yard got a Workshop and the Landmark's scaffolding.

   *Embarrassment turns into "they kept a place for me".*
2. **Screen 2, your estate is safe:** complete, on its island, balloons ready. *Nothing lost, so nothing to mourn.*
3. **Screen 3, landing cards:**
   - **"Your seat at Ashford NE"**: still the crew's, day 3 of its 30 extra days.
   - Next to a lively station.
   - The Frontier.

   He takes his seat. Hollow & Sons moves out with a note ("kept your coal flowing for 19 days"), and Ben's links come
   back with their trust intact. *He lands home in one minute, not one hour.*
4. **Catching up.** An Era-2 jump kit (the steel chain blueprint and a free first building), the "Back!" ribbon, and
   four welcome postcards: from Ana, Dev, Bertha the lodger, and Mo from next door. The crew ticker says "Ben is back!"
   in the crew colours. *He feels welcomed, not audited.*
5. **What he never sees:** a fine, decay, a lost seat, a reset streak, a list of goals he "missed", or an inbox of
   200 items.
6. **If his seat had already been opened**, he would land in the halt next door and get a free Street pipe to the
   Yard. *That is still home, one corridor over.*

### (f) The late game in week 4

Kettle Works (Ana, Cleo and Dev; Ben's seat is still run by his copy-trade lodger), Level 25–32, is part of the
"Copper Coast" corporation of four crews.

- **The long line.** On day 22 a **Discovery** is announced: an oil ★5 field 48 tiles into the wilderness, live
  from tomorrow and lasting 9 days. Kettle Works builds a steel trunk from its Yard, segment by segment over four
  evenings, each member at their own time. It becomes a teal, copper and violet ribbon across the map. Cleo
  adopts the first pump house and Dev the second. *Public works with your name on them: pride.* Twenty lanes; they
  use eight and rent the rest at the game's 2% toll to two other crews.
  *Renters feel served rather than exploited: the toll is small and fixed.*
- **The race.** Kettle Works reaches the field second, earning a bronze marker on the map forever. *Friendly
  competition that leaves a mark and takes nothing from anyone.*
- **Specialisation.** Dev's Hub Order bar for copper ingots is full every day (*a goal met, not a cap hit*), so his
  surplus now goes to a Copper Coast corp-mate's Wire Works two stations away through a rented lane. Ana's mastery
  title "Master of Wind" appears on her banner.
- **The Landmark's final stage:** "600 steel/min for 6 hours". Nobody can do it alone. Three members' Steel Mills
  feed it, and the Yard tanks hold the morning player's output for the night player's hours, so the six hours fill
  over two days. The ring fills. *"We did it", without a single synchronised evening.*
- **The league.** Kettle Works is 3rd in the Station League (best three contributions count), and Dev is 1st in his
  company league. *Everyone has a race they can win.*
- **The Era Wonder** takes their steel and oil by trunk. Copper Coast reaches the gold tier, and the statue goes into
  next chapter's hub.
- **What stops it going stale:** the next chapter brings new Discoveries, new Eras and a Homecoming week. The crew,
  its Wall, the Landmark (as an heirloom miniature) and the trunk carry over, and the veterans' best plays are trunk
  lines and Hosting, *which help newcomers instead of crushing them.*

---

## 6. Scoring against the 14-point checklist (0–3)

| # | Point | Score | Why |
| --- | --- | --- | --- |
| 1 | Friends together | 3 | A crew and a station in minute 5; held, furnished seats; the late friend is the missing piece; crews of 5–6 get a halt |
| 2 | Choosing where you live | 3 | Station board with lights on, hours, land, waiting links; "Pick for me / us"; every choice undoable by moving |
| 3 | The lonely player | 2 | Lodgers, Houses, hub town, presence and consolidation make it full. A risk remains: on very quiet servers most neighbours are labelled NPCs, and some players will feel that |
| 4 | Players who stop | 3 | Copy-trade lodger, successor inherits links, Quiet Isles, seat lapses by itself; no holes, no dead partners |
| 5 | Early game | 3 | Own tutorial first; the Yard pipe at minute 12 is optional, fixed-price and instantly useful; nobody can touch a newcomer |
| 6 | Late game | 2 | Trunks, Discoveries, sustained-flow Landmark, leagues, corporations. Strong for weeks 2–5; month 3 depends on chapters keeping it fresh |
| 7 | Asynchronous idle | 3 | Tanks, standing orders, Wishes with NPC fill, sustained flows buffered; no social action needs two people online |
| 8 | Fair and safe | 3 | Fixed Yard price, fair band, Hub Order, game-paid Host bonus, capped gifts, fixed tolls, logged and undoable Yard changes |
| 9 | Clear | 2 | One-line link states, names on the Yard board, colours. But it's 8 new words and three trade routes (Yard, link, hub); it needs good onboarding |
| 10 | Pipes are the heart | 3 | The Yard pipe, links, Street pipes and striped trunks: every social act is a pipe on the map |
| 11 | Scale and performance | 3 | Yard ≤ 6 estates; links are teleporter-style; trunks ≤ 20 lanes; no network crosses a gate |
| 12 | Seasons and persistent world | 2 | Crews, Walls, heirlooms and the Quiet Isles carry over; migration is planned. Carving Yards out of inner-corner parcels touches every tester estate and needs care |
| 13 | Social glue | 3 | Crew identity, Handshake, Wishes, postcards, tending, Crew Wall, Landmark, trunks with names, invite link with a seat |
| 14 | Buildable | 2 | Most pieces extend existing code (tenants, Caretaker, Quiet Isles, relocation, teleporter-style links, Wonders), but the whole is about 12–15 days; it must ship in the steps below |

**Total: 37 / 42.** The honest weak spots: solo on a near-empty server (3), vocabulary load (9), carving the Yards (12)
and the size of the whole (14).

### Shipping in steps (each step playable)

1. **Links** (TradingRethink A), gate tanks, honest states, hub fallback. *This fixes the mega-network by itself.*
   (2 days)
2. **Crews, the invite link and held seats**, crew colours, station and halt geometry, "Pick for me / us". (2 days)
3. **The Yard:** Yard gate, fixed-price pro-rata market, Yard board, Handshake, first Station Orders. (2 days)
4. **Absence and return:** copy-trade lodger, successor inherits links, seat lapse, Crew Wall, the three return
   screens, savings jar. (2 days)
5. **Lodgers with personalities**, presence (lights on, hours), Wishes, gifts and postcards, tending. (2 days)
6. **Newcomers:** placement score, Hosts, fair band, late-start XP, Hub Order reframing, rookie boards. (2 days)
7. **Late game:** trunks with lanes and adopted pumps, Discoveries, sustained-flow Landmark stage, leagues. (3 days)

---

## 7. What I changed from my round-1 proposal, and why

- **Dropped** the daily +10% tune-up (an aid-back chore) and the per-active-member Station Cup (pressure to kick).
- **Took from others:** home goods and the price ladder (E), the furnished seat, the late friend as the missing piece,
  Wishes and Yard geometry (F), link states, successor lodgers, the return in three screens, Hosts and the savings jar
  (L), lines with gate tanks and the moving van (W), caretakers as one system for solo, absence and leaving (O).
- **Added from the history:**
  - the Hub Order frame (WoW's rested XP);
  - "nobody presses a kick button" (Hay Day and Clash drama);
  - stations as neighbourhoods, crews only by choice (FFXIV wards, SimCity 2013 regions);
  - first-meeting parties instead of headcount multipliers (Snapstreaks, Habitica);
  - "humans make things better, never possible" (Destiny, FarmVille).
- **Open question for the owner:** should strangers in a station share the Yard at all, or should the Yard be for
  crews only, with strangers trading only by links? My view: share it (it's what makes a solo player's first hour
  social), but keep it opt-in by laying the pipe. Test both with the testers.
