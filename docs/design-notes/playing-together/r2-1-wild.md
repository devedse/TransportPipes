# Round 2 · The wild idea thrower defends: Streets & Lines

What changed since round 1: I read all six files, the brief, `docs/GameDesign.md` §4, §5, §11–12,
`docs/TradingRethink.md` and `docs/ImplementationStatus.md`. Two findings cut my own round 1 down to size:

- **Half of my "wild" churn ideas are already built.** Activity stages, the Caretaker, the Quiet Isles, packing and
  unpacking an estate with its layout, relocation, crates, swaps and corporation blocks all shipped in M6. My
  "moving van" and "time capsule" are features that already exist under other names. The real gaps are elsewhere: who
  you live next to, what you share, and what happens to the *partners* of someone who leaves.
- **The repository already learned the mega-network lesson the hard way.** "A network of 56 buildings took 380 ms a
  solve" and "a big network took 500 rounds, 0.4 s, on every change" (commits 716d68f and 9309a22). Any idea that joins
  several estates into one network, even a "bounded" one, has to answer for that.

The proposal at the end (§4) keeps the small group that five of six files want, but **makes the shared thing a pipe
instead of a place, and makes the group elastic instead of four fixed seats.**

---

## 1. The five weakest ideas, and how they fail

### W1. Quartet Recipes: "only humans can supply the inputs" (6-outsider #15)

Same family: the diversity bonus ×1.0 → ×1.5 by head count (2-friends §7), and my own Relay Recipes that need three
different companies (1-wild #13).

**How it fails.** Chapter 1 shows its ten season trophies on the hub board; six of them, the Brass Orrery among them,
say "4 players". Kettle Works is Ana, Ben and Cleo. Ben stopped on day 3 (a new job). On day 10, with Dev joining on
day 8, they are three active players and the Orrery is out of reach. On day 12 Ana writes in the group chat: "Ben, log
in for two minutes and put some coal in, please?" Ben feels the game has become a debt. He mutes the chat and never
opens the game again: he would have come back in month two. Meanwhile Sam, who plays alone at 3 a.m., looks at the
same board and sees that the best things in the chapter are locked for him forever. On day 14 he reads in district chat
that someone made three alt accounts to craft quartets. He concludes that the game rewards crowds and cheaters, and he
quits.

**Why it fails.** It ties the most visible rewards to the scarcest thing in an idle game: four people committed at
the same time. That punishes the majority (solo players and groups that shrink), it turns friends into creditors
(Habitica), and alts beat the "human" test anyway, so it only filters out honest players (FarmVille). **Rule I take
from it:** playing with others may pay *a little more* or look different, but nothing may need a head count.

### W2. The Yard network: one shared network across a crew's estates (2-friends §5, §14)

"This is the only place where a network spans several estates, and it is bounded by the crew (2–4 estates, up to 8 in
a Street)."

**How it fails.** Day 19, Kettle Works and Fox & Sons have joined into a Street: seven players, about 450 buildings on
one network through the Yard. At 21:00 Dev places his first Steel Mill on a Yard-connected pipe. It pulls coal through
the Yard pipes until they are full, and Cleo's Glassworks, also on the Yard network, starts to starve. Cleo changed
nothing. All she sees is "why did my glass stop?". Every pipe tile any of the seven lays re-solves all 450 buildings.
The advisor, which runs what-ifs per network for each player, now runs seven players' what-ifs on a 450-building
network. Mo from Fox & Sons rebuilds his pipes every morning at 06:00, and every rebuild shifts the others' flows.
Then the migration: the Yard is carved from each estate's inner-corner parcel, "the parcel bought last". But parcel
prices double with each purchase, so the parcel bought last is the most expensive one. A tester who owns all 16
parcels loses the one he paid the most for.

**Why it fails.** It is the district mega-network in miniature: other people's choices break your factory, a shared
bottleneck nobody owns, slow solves. That is exactly what the owner wants to get rid of. **Rule:** the crew's shared
thing must be a *carrier* (like the teleporters: a buyer in one network, a seller in the other), never a merged
network.

### W3. Spot requests on dormant neighbours (3-lonely §3.3)

"If a dormant (14+ days) estate is next to an active player, that player can request the spot … without an answer,
the estate goes to the Quiet Isles early."

**How it fails.** Lena plays in a Quad with her two best friends. On day 20 of the chapter she leaves for a 16-day
hiking trip with no phone signal. On day 34 she becomes dormant. Rob, a stranger in the Quad next door, wants her spot
for his cousin and files a request. Lena never sees the notification. On day 37 her estate sails to the Quiet Isles,
and Rob's cousin lands in her seat between her two friends. Lena comes home that same evening. The return screen
offers "next to a friend: no free spot", so she lands two streets away. Her friends' crew price, Crew Orders and Yard
are now shared with a stranger, and Lena plays alone.

**Why it fails.** It turns a holiday into an eviction. It breaks two of the same file's own never-rules (#11 "forced
moves", #12 "a worse home without a choice") and the GDD's first principle ("nothing someone built is ever taken
away"). It also solves a problem that doesn't exist: land is never scarce, because new districts open on demand.
**Rule:** nobody's spot can be taken because someone else wants it. New lots appear instead.

### W4. The station market with tenants as buyers (4-economy M4)

Every estate pipes one market line to the junction, and the junction pairs sellers with buyers pro rata at 1.4 / 2.0,
with tenants taking part.

**How it fails, on a busy server.** If the tenants buy without a limit, the station market is simply a hub that pays
40% more. On day 4 Tom's copper flows into the junction at 1.4. Mira, who wanted Tom as a partner, gets that same
copper from the junction at 2.0 anyway. A direct link would give them only 25% more on one side and 12.5% less on the
other, plus a request, a setting and the risk that Tom quits. In the balance runs nobody links. "Everyone trades with
everyone, nobody chose anyone" is back: the owner's exact complaint, only now without pipes. The 1.4 also becomes a
faucet, and because deliveries count as earnings for XP, it moves the whole Level 30 in week 4 pacing.

**How it fails, on a quiet server.** If the tenants buy a limited amount, Sam's station is himself plus four tenants.
His copper goes past the tenants' limit on day 3 and falls back to the hub at 1.0, while players on busy stations get
1.4. The lonely player gets the worst prices, which is exactly the wrong way round.

**Why it fails.** It has to choose between killing chosen partnerships and punishing quiet stations, and both are
fatal. **Rule:** the "anyone in the area" market must stay the hub. Everything better than the hub needs a choice
(being in a crew, or a link).

### W5. The accordion map: re-pack the world so active estates slide together (1-wild #7, mine)

**How it fails.** On day 9 Mira's estate sits next to Joe's. They have had a link for five days, and Mira routed her
whole iron chain to the gate that faces him. Joe is away on a long weekend (resting, 4 days). The nightly re-pack
folds Joe into the "sleeping quarter" and slides Pia in. Mira's link now points at a stranger's gate, her corridor
pipes have been "re-routed automatically" along a new path, and her estate looks over a different hill. When Joe comes
back on day 11, he lands somewhere else. Neither of them ever builds a neighbour relationship again, because the game
has taught them that neighbours are temporary.

**Why it fails.** Spatial memory is half the pleasure of a builder. Forge of Empires reshuffled neighbourhoods and
players never bonded (5-lessons B). Moves must only ever happen on the player's own tap (the Great Migration in
3-lonely §7.3 is the right, opt-in version). I withdraw it.

### Dishonourable mentions (one line each)

- **Estates on rails** (1-wild #3, mine, and one of my round-1 favourites): every 32×32 estate becomes a moving
  carriage. The renderer, the district simulation and pipes between moving cars all break: months of work, not days.
  What survives: *a group is something you couple onto*, which became the Junction in §4.
- **The shared river** (1-wild #26, mine): a commons where whoever logs in first fishes out everything. Leechers
  win, and a griefer can flood it with junk.
- **The clocktower / Shift Log** (1-wild #16, mine; 6-outsider #9): a group streak. When the crew shrinks, the silent
  clock tells everyone the crew is dying.
- **Night Market** (6-outsider #29): extra pay in a fixed hour, the one thing asynchronous play can't do.
- **Station Rotation** (6-outsider #16): in a persistent world, rotating stations means rebuilding.
- **The Rival House, "always 5–10% ahead or behind you"** (3-lonely §1.1): rubber-banding. Players notice it and
  then distrust every House.
- **Contract shortfall paid by the absent seller** (4-economy M11): even capped at 10%, it fines a player for having a
  life.

---

## 2. The five strongest ideas, and what makes them strong

### S1. Absent players fade into an NPC, and the link survives the partner (3-lonely §3.2, 2-friends §9–11, 5-lessons A4 and #6)

A successor House or Lodger inherits what crossed the old estate's links, at about 90% of the volume. A Lodger hands
over intact works to the friend who takes its seat. **Why it's strong:** one mechanism covers the lonely player (the
seats are never empty), the leaver (nobody's factory goes cold) and the newcomer (a waiting link is a proven, wanting
trade partner in minute one). It fixes the *feeling* (guilt and resentment), not just the numbers. It is buildable
because tenants, the Caretaker and the Quiet Isles already exist.

### S2. Every zero has a sentence: honest link states (3-lonely §3.1)

Live, Resting partner, Caretaker, Handed over, Capped: one line each, on the pipe and in its panel. **Why it's strong:**
about a day of work, it serves clarity (9) and churn (4), and it is the general version of bugs this project has already
fixed in play ("connected to two Ironworks, sold nothing", fixed by Loady saying why). A pipe that silently carries
nothing is the most demoralising thing in a pipe game.

### S3. The late joiner is the missing piece (2-friends §2 and §10, 5-lessons #2)

Lots are generated to complement each other, so choosing a lot is choosing a role ("Ben takes coal"). The unclaimed
lot is held by a Lodger who hands over its works. Dev's copper replaces the hub imports from his first hour.
**Why it's strong:** it turns the classic problem (the friend who arrives a week late is a burden) into a moment where
he is needed. Nobody is carried, his small output counts at once, and the ticker says so out loud.

### S4. "Direct trade, carried by whoever you pay": trunk lines with slots (4-economy M9 and Part 3)

Every trade still has exactly two named parties. A third party only *carries* it, on a line it built and owns, for a
capped toll. If the line is abandoned, the hub takes it over. **Why it's strong:** it answers the owner's doubt about
"direct trade only" precisely. Direct between two companies, yes; through a third company's *estate*, never; over a
third company's *pipe*, yes, visibly and for a fee. It also gives the late game a status role (a famous line with your
name on it) and keeps the simulation bounded (at most 20 links per line).

### S5. Wishes on an order board that an NPC fills at the deadline (5-lessons #8, 2-friends §8, Hay Day crates)

"200 bricks by tomorrow evening." Friends fill it whenever they log in, standing orders fill it while everyone
sleeps, and at the deadline a Lodger fills the rest at the hub price. **Why it's strong:** it is the most natural
"help me" in an idle game, fully asynchronous, helping feels good and costs little, and *nobody can let anybody down*.

**Honourable mentions:** tending ("Ana tended your estate 17 times while you were away", 3-lonely §2.3, the best
win-back line in all six files); the personal hub allowance, so nobody can move anybody else's price (4-economy M7);
per-active-member cups and local leagues (5-lessons #10); the Death Stranding template, where other players' pipes help
you and the only reaction is thanks (5-lessons B); "the Great Pipe must be an aggregated ledger, not a real flow
simulation" (6-outsider).

---

## 3. Everyone converged on "4–6 estates around a shared yard". Right, or groupthink?

Five of six files land there: the Quad and Yard (2), the station and junction (4, 5), the four stations around a
Junction (6), and my own hamlet around a well (1). Only 3-lonely kept plain links.

**What is right about it:** the group size (Dunbar's first layer, 3–5 people), a shared object everyone can see, and
consent (friends claim seats together).

**What is groupthink:**

1. **We all drew it because of the grid, not because of the players.** A 5×5 district has four natural 2×2
   corners, and the brief said "station". Six designers looked at the same picture.
2. **It puts the seat count into the ground.** Groups are 1, 2, 3, 5 or 7 people. A Quad fits exactly four. The fifth
   friend is split off, two friends get two strangers, and a solo player gets a table for four.
3. **For strangers it brings back assigned relationships.** 5-lessons A1: a neighbour you were assigned feels like the
   water company. A Quad of four solo players is the district problem, shrunk.
4. **It assumes groups stay full, and the numbers say they don't.** Even at a generous 25% of players still active on
   day 14, chosen independently, a Quad of four strangers has at most one active human in **74%** of cases
   (0.75⁴ + 4 · 0.25 · 0.75³). So the typical Quad in week 3 is "you and three Lodgers". The design that has to be
   good is the Lodger design. The spatial cost (fixed seats, carved yards) buys little for that majority.
5. **The shared object is a place, not a pipe.** A Yard at everyone's corner shrinks the crew's pipes to 6–20 tiles:
   one short pipe each, and the logistics are gone. "Pipes are the heart" then has to be bolted on later as trunks.
6. **A shared place needs rules about who may change it** (2-friends risk #4 already lists logs, undo and vetoes). That
   is the Factorio permission-group problem, in a cosy game.

### Alternatives that could beat it

**Alt A · The Street: the pipe is the place (elastic, the smallest change).** A crew lives on the lots that face one
corridor, from 1 to 6 of them. The corridor between them carries the crew's **Main**, a pipe the game lays for free.
The Main is the shared object, the crew's project and the thing that later grows into a Line across the map. Four
friends on a street look like a Quad without a carved yard; five take the next lot; two are a pair; one is a Street of
one. No parcel is taken from anyone. *Beats the Quad on:* elastic size, migration (nothing moves, nothing is carved),
pipes as the shared thing, and a natural path into the late game.

**Alt B · One estate, many hands (genuinely different).** OpenTTD's best-loved mode: friends don't trade, they build
*one* estate together. Each friend brings their 16 parcels as a wing of a bigger estate, owns the buildings they
place and has their own wallet, and the pipes run freely between wings: "our factory". *Beats the Quad on:* the purest
"play together" feeling, live cursors on one map, no trade rules to learn. *Loses on:* churn (a quitter's wing is a
hole *inside* your factory, crossed by your pipes; packing it to the Quiet Isles cuts them), wallet friction
(Stardew's lesson), and network size (one 250-building network per crew is W2 again). I don't pick it, but I steal its
best part, **Sketches**: a crew mate can draw a ghost blueprint on your land, and you build it with one tap.

**Alt C · Lines, not places (genuinely different).** The group isn't a neighbourhood. It is a pipeline company with a
named Line across the map, from a rich deposit to a hub or a Wonder. Estates settle *along* the Line (upstream raw
materials, downstream factories), and joining a group means building an on-ramp. Friends who live anywhere can be on
one Line. The map reads like a metro diagram of coloured lines. *Beats the Quad on:* pipes (10), late-game depth (6),
clarity (9), and group sizes from 2 to 20. *Loses on:* the first evening, because friends aren't neighbours from
minute one and a long Line is a week-2 thing. So I use it as the **second layer**: the Street's Main grows into a Line.

**Alt D · No geography at all (the anti-proposal).** Friends live anywhere and share a crew Pneumatic Post. It is the
cheapest to build, and it kills "see each other's estates" and pipes as the heart. It is listed only to show that the
neighbourhood is worth what it costs.

**Verdict.** The small group is right; the fixed yard is groupthink. **A + C beats the Quad:** a Street whose Main is
the shared thing, which grows into a Line, with B's Sketches for the "building together" feeling.

---

## 4. The revised proposal: Streets & Lines

### 4.1 Seven words

| Word | What it is |
| --- | --- |
| **Lot** | An estate as today (32×32, 16 parcels). Nothing about estates changes |
| **Street** | One corridor stretch and the lots facing it on both sides (up to 10) |
| **Crew** | 1–6 companies from Company Level 1, with a name, an emblem, colours and an invite link; its lots all face its street |
| **Main** | The crew's pipe in its street's corridor, laid by the game. It carries goods between members and grows with the crew |
| **Lodger** | A named NPC family (always marked with a cog) on a crew lot that has no active human |
| **Link** | A bilateral pipe between two gates (TradingRethink option A), never transitive |
| **Line** | A Main extended beyond its street (into the wilderness, to an outpost, a Wonder or another crew). Where two Lines meet, the map shows a **Junction**: a link whose two ends are Mains |

If the owner prefers the word: the Street can be called a **Station** and the Main its **platform pipe**. On the Lines
map view each crew shows as a station on its coloured line.

### 4.2 The rules

**Choosing where you live: the Street Map.**
- The start screen has three buttons: **Start with friends**, **Start alone** and **Pick for me**.
- The Street Map is the district map at street level. Each street card shows:
  - lit windows for the lots whose owner played today;
  - the residents (names, levels, usual hours: "evenings CET");
  - open and held lots with their home goods ("looking for: copper");
  - the land (deposit stars, wind and sun);
  - the hub's specialty this week and the distance to the hub.
- Lots on a street are generated to complement each other: one energy lot, one ore lot, one water/sand lot and one
  material lot in every four.
- **Pick for me** (the default) chooses:
  - for a solo player: an open lot on an *active* crew's street that lacks this lot's home good and plays at similar
    hours (Egg Inc.'s lesson: match by activity);
  - otherwise a new Street of one in the newest district, next to active crews.

**Invites and held lots.**
- "Start with friends" makes a crew in 15 seconds and an invite link (2-friends §1).
- The founder holds up to *crew size + 2* lots for 14 days, renewable once, so a friend can join until day 28.
- A Lodger keeps each held lot warm and hands over its works intact to the friend who arrives.

**The Main** (the heart of the proposal).
- Laid by the game along the crew's frontage at founding, free, striped in the crew's colours. Each member's
  street-side gate gets a **Main valve**: what goes on the Main (default: your surplus) and what you take from it
  (default: what your buildings need).
- **Solved as an N-way teleporter, never as a merged network.** On each member's network the Main is one buyer and one
  seller. Each solve shares the members' offers out to the members' needs pro rata, filled from the last solve. That is
  O(members × resources) per crew, and no network crosses a gate. Pellets in each member's colour run along it.
- **Crew price: fixed at Auto, 1.75 × the hub's buy price** (the hub buys at 1.0 and sells at 2.5). The seller gets
  75% more, the buyer pays 30% less. Because it's fixed, it can't be used to funnel gold.
- **Flow order:** own consumers → contracts → the Main → links (Junctions and Line slots too) → own storage → hub.
- Lossless along the street (the one friends' perk on distance). Its throughput is its pipe tier, which the crew
  raises together.

**Crew Orders and the Main as the crew's project.**
- The Main's sign shows 3 Crew Orders a day ("street lamps: 200 stone, 100 planks, 300 ⚡"). They are delivered over
  the Main, sized from the members active in the last 3 days, and one member alone can always finish one (about 45
  minutes of their output).
- Contributions show as a ring in the members' colours. There is no ranking and no head-count multiplier. The
  delivery that completes a missing input gets **+20% credit** (the keystone bonus, 4-economy M12).
- Crew Levels 1–10 build the Main up, not a yard:

  | Crew Level | What the Main gets |
  | --- | --- |
  | 2 | Lamps |
  | 3 | The **Cistern**, a shared tank on the Main that buffers Wishes and orders |
  | 4 | Copper tier |
  | 5 | The Line licence |
  | 6 | A shared outpost licence |
  | 7 | The Main rises onto arches over the corridor |
  | 8–10 | The **Gate Arch** landmark at the street's end, with the emblem, seen from the district map |

  The Steel tier follows Era 2.

**Help that works while the others sleep.**
- **Wishes:** pinned to your gate, filled by crew mates or standing orders, and topped up by a Lodger at the hub price
  when they expire after 24 h. Nobody is ever let down.
- **Tune-ups:** one a day per crew mate or link partner, +5% on one building for 12 h, banked for up to 3 days,
  thanked with one tap.
- **Sketches:** a ghost blueprint drawn on a crew mate's land, built by its owner with one tap at their own cost.
- **The crew morning report and ticker:** "While you slept, Ben's coal kept your Smelter running: 1,240 coal."

**Neighbours who aren't in your crew** trade by **links**, as in TradingRethink A:
- auto-routed through the corridors and never exclusive (drawn as bundles), so a corridor can't be blocked;
- priced at a position in the fair band (Auto by default), with the hub always behind as the fallback;
- with honest link states (S2).

A lot's other three gates face other streets, so each crew has natural link partners at its back gates.

**Absence**, the existing stages plus what partners need:

| Away | What happens |
| --- | --- |
| 3–14 days, resting | Everything runs, the Main valve too, and a sleeping cat sits on the HQ steps |
| 14–30 days, dormant | The Caretaker keeps the links and the Main. Crew mates and partners can tend the estate (flower beds; counted for the return screen). The crew may name a Housesitter, who can change trade settings only |
| 30 days and more, asleep | The estate sails to the Quiet Isles (exists), and its lot stays **held for the owner for 60 days**. A Lodger copies the owner's last 7 days of Main trade and **fades to the normal Lodger limit over 14 days**, with a heads-up to whoever depends on it |
| Links outside the crew | Handed to a successor House at 90% of the volume, or capped and waiting for whoever lands on the lot next (S1) |
| A held lot | Opened earlier for a recruit only by a majority of the active members, and never before the owner is asleep |

**Lodgers.**
- Their works stay small (about Level 5). They trade on the Main at the crew price and buy up to **about 15% of a
  typical active player's output**, only tier 1–2 goods.
- So a Street of one earns roughly 10% more than the hub alone, and a crew of humans about 20–25% more (3-lonely's
  rule: humans add about 10–15% and warmth, never ×2).
- They have names, moods, small Wishes and thank-you notes.

**Lines (the late game, from Crew Level 5).**
- Members extend the Main beyond the street in 10-tile segments, each paid by one member and striped in their colour.
  Every 25 tiles a Pump House stands that a member can adopt; an unadopted one still runs at base level.
- Losses follow the pipe tier: steel loses 9.5% over 200 tiles, grey 45%. Distance is a real decision.
- A Line can run to the crew's outpost, a Discovery, the Wonder site or another crew's Line. Where two Lines meet, both
  crews (a majority of active members each) can open a **Junction**: a link between the two Mains, at Auto, never
  transitive.
- A Line has **20 slots**. The crew rents its spare slots as links riding the Line, to companies outside the crew:
  - a toll of at most 3%, with 24 h notice for changes;
  - a renter's week is guaranteed;
  - anyone may build a parallel line;
  - if every owner is dormant for 14 days, the hub buys the line at half its cost and runs it at 2%.
- The **Lines view** shows the district as a metro diagram.

**Fairness.**
- Fixed crew price; band prices on links.
- Housewarming gifts are capped at 500 g of goods × the receiver's level, per giver, in the first 48 h. There are no
  other gifts.
- Hub imports count 25% towards Crew Order and Wonder credit.
- A **personal hub allowance** replaces the district price reaction for selling (4-economy M7). It is generous enough
  that a diversified solo estate rarely meets it, and only one-product dumpers feel it.
- A fair-price band applies between companies 10+ levels apart, and there are no shortfall clauses below Level 6.
- Crew membership is by consent (an invite or an accepted application). A majority of the active members can take a
  member off the Main, at most once a week and logged. That member keeps their estate and gets a free move.
- No spot requests (W3) and no re-packing (W5).

**Recognition and competition.**
- Crew name, emblem and colours on gates, pipes and pellets; the street sign; the Gate Arch; Line name plates ("Kettle
  Line") with an income counter.
- The weekly Crew Story postcard and an auto-written street gazette (5-lessons #11).
- A crew cup scored per active member over the best 3 days of the week; personal leagues of about 25 companies at a
  similar stage; Rookies and Growth boards.
- Host stars for veterans whose newcomers reach Level 10 on 4 or more different days.

**Chapters.**
- Crews, streets and Lines persist. Each chapter opens with a **Lines Race** to its Wonder site; the first three crews
  get a permanent marker.
- The Great Migration (3-lonely §7.3) is opt-in: a whole street can move as a block to a fresh frontier district,
  using the existing corporation blocks. The Gate Arch becomes an heirloom.

### 4.3 Stolen goods

| From | What |
| --- | --- |
| 2-friends | Start with friends, the invite link, the Seat Map (now the Street Map), complementary lots, the Handshake, Crew Orders, the ring, Lodgers handing over works, Tailwind, Housewarming, the copy-trade Lodger, segmented trunks with adopted Pump Houses, the Line Race, the Crew Story |
| 3-lonely | Honest link states, successor Houses inheriting links, tending, the three-screen return, the savings jar, the Era jump kit, Hosts, the fair-price band, no-guilt rules, the Great Migration |
| 4-economy | Comparative-advantage land, band prices, trunk slots with tolls and hub takeover, the personal hub allowance, the keystone bonus, the hub fallback everywhere |
| 5-lessons | Match by activity, favours that bank, the order board with an NPC fill, the chronicle, per-active-member cups, local leagues, pack it and don't wreck it |
| 6-outsider | Caretakers as one system for being alone, absent and gone; Coloured Flow; the Great Pipe as a ledger |
| 1-wild (me) | The pipe as the shared object, elastic streets, Lines and Junctions (my "trunk lines" and "estates on rails" made buildable), the metro view, streets moving as a block |

---

## 4.4 Scenarios, step by step

### (a) Three friends start tonight; Ben stops after day 3; Dev joins on day 8

| When | What happens | Why it works |
| --- | --- | --- |
| Day 1, 20:00 | Ana taps **Start with friends**: "Kettle Works", a kettle emblem, teal and copper, "we're 4". She shares the link in the group chat | 15 seconds, no level needed |
| 20:02 | **Pick for us** suggests Ashford Street 2: four fresh lots facing one corridor (Windy Heights and Coal Valley north, Riverside and Copper Gulch south). Neighbours: Fox & Sons on Street 3, lights on today, evening players | Information to choose well; one tap for those who don't care |
| 20:04 | Ben, on his phone, drags his banner onto Coal Valley; Cleo, on her laptop, onto Riverside. Copper Gulch shows "held for a friend · Lodger: Bertha's Brass" | Choosing a lot is choosing a role, and everyone can see it |
| 20:05 | **Settle.** Three estates land on balloons at once. The game lays the Main along the corridor, striped teal and copper, with a street sign "Kettle Street" | A shared moment, nothing built by hand yet |
| 20:06–20:12 | Each player's own Loady tutorial: wind, iron mine, Trading Post, first gold. They see each other's cursors next door | Nothing depends on the others |
| 20:12 | Loady: "Lay a pipe from your network to your street gate (9 tiles)." Ana is first, and Bertha's copper pellets are already rolling along the Main. Ben is second: the **Handshake** (both pipes flash, a First Link gift). Then Cleo | The first shared pipe is real pipe-laying, not a menu |
| 20:15–20:19 | Crew Order "Street lamps" is done with Ana's energy, Ben's stone and Cleo's planks (Bertha chips in 40 stone). Crew Level 2: lamps light up along the Main | Something together within 20 minutes |
| Day 1, later | The morning report: "Ben's coal kept your Smelter running." Cleo pins a Wish, "coal for my Glassworks"; Ben fills it with one tap from the bus. Ana draws a **Sketch** of a Smelter next to Ben's coal mine, and Ben builds it with one tap | Async help; building together without shared ownership |
| Day 2 | Ana's north gate faces Street 1. The neighbour card offers a **link** to Fox & Sons' Mo (stone for energy, Auto price), and Mo accepts while Ana sleeps | Crews trade with crews by choice, gate to gate |
| Day 3, 22:40 | Ben's last session, though nobody knows it yet. He upgrades his coal mine | — |
| Days 4–7 | Ben's mine keeps putting coal on the Main; Ana's Smelter notices nothing. From day 6 a sleeping cat sits on his HQ steps. Crew Orders are sized from Ana and Cleo; Ben's colour just doesn't appear on the ring | Absence hurts nobody; no list of who didn't help |
| Day 8, 21:00 | Dev opens the old link (the lot is held until day 14). His card says "the crew imports copper from the hub". **Settle:** Bertha leaves a cake and a note, and her Copper Mine, Smelter and generator (Level 5) become Dev's. Ana and Cleo each send a Housewarming crate (capped) | The late joiner arrives into working buildings |
| Day 8, first hour | Dev's copper replaces the hub imports at 1.75. The ticker: "Dev's copper saves the crew 840 g/h." Tailwind is on (XP up to ×2.5 while he is more than 3 levels below the crew median), and his Crew Order deliveries count ×2 | He is the missing piece, not a burden |
| Day 12 | Dev is Level 11 and filling Crew Orders | Caught up in 4–5 days, not 8 |
| Day 17 | Ben is dormant: the Caretaker keeps his links and his Main valve. Ana tends his estate most days (a flower bed by his gate). Cleo is named Housesitter and turns his full coal storage onto the Main | The crew keeps him in the picture without guilt |
| Day 33 | Ben is asleep. His estate sails to the Quiet Isles; his banner hangs on the street sign. Lodger "Old Wim" holds his lot and copies Ben's last week (1,100 coal/h), fading to the Lodger limit by day 47. Ana gets a notice with options: Dev's ★2 coal, a link to Fox & Sons' coal, the hub | No factory goes cold overnight; two weeks to adapt, with the options named |
| Day 40 | Ana, Cleo and Dev keep holding Ben's lot (the default; they could open it from day 33 by majority) | The friends decide, not the game and not a stranger |
| Day 93 | If Ben never comes back, the lot opens on its own and his banner stays on the wall. Cleo invites her sister, who takes the lot and Old Wim's works | The seat isn't lost to the crew |

### (b) A solo player on a server of five players

The world is one district (the Capital): Kettle Works (3 players), Lina (dormant, a Street of one), Sam (new) and
Lodgers and Houses on the other 19 lots.

| When | What happens | Why it works |
| --- | --- | --- |
| 03:10 | Sam taps **Start alone**. The Street Map: "lights on today: 5 estates" (never "1 online"). Kettle Street has no open lot, so **Pick for me** founds **Sam's Row**: Sam's lot plus two Lodger lots, Oakridge Coal & Sons and Bertha's Brickworks | He gets a Main, Crew Orders and a Crew Level from minute one, honestly labelled as NPCs |
| 03:22 | Sam's gate pipe reaches the Main. Oakridge plugs in: a smaller Handshake and a note, "Fine pipe, young one." Oakridge buys Sam's spare energy at 1.75 and sells him coal at 1.75 instead of the hub's 2.5, up to its limit | Solo isn't a handicap: about +10% over the hub, never stuck |
| Day 1 | Crew Orders for one; the Lodgers chip in small amounts. Sam's Wish "bricks for my Kiln" is filled by Bertha at the deadline. The hub town grows a little with every delivery, and a plaque names him | The world reacts to *him* at 3 a.m. |
| Day 2 | Sam's east gate faces Ana's lot across a corridor. A neighbour card: "Ana (Lv 9, evenings) needs glass, you make it." Sam taps Connect; the request waits up to 72 h. Ana accepts at 19:00 while Sam sleeps, and the morning report shows the first glass delivered | A human partner without being online together |
| Week 1 | Sam's Row reaches Crew Level 4. Sam opens a lot ("looking for: copper · plays nights"). Nobody comes on a five-player server, and that's fine: nothing needs anyone | Complete alone |
| Week 2 | Sam extends his Main into a Line towards a Discovery copper field. Kettle Line passes 30 tiles away, and the crews open a **Junction** | Sam's street couples onto theirs (the "rails" idea, buildable) |
| Week 3 | The crew cup counts per active member, best 3 days: Sam's Row (1) is third of four streets, ahead of a 3-player street from a newer district. His personal league is padded with labelled Houses, and his best-week record is on the board | Competition he can win |

What's honest: Sam earns about 10–15% less than a Kettle member and his warmth comes from Lodgers, one link and
one Junction. That is the right size: worth inviting someone, never worth quitting over.

### (c) A newcomer in week 3 among veterans

Nora joins on day 18. The district has 18 humans at Level 20–30.

| When | What happens | Why it works |
| --- | --- | --- |
| Minute 0 | The Street Map has two kinds of offer: the **newest district** (newcomers' streets with Lodgers, high hub demand) and **open lots on veteran streets**. Fox Street's card: "Crew Level 7, 3 active, evenings, Host ★, looking for: sand and glass (Riverside)". **Pick for me** picks it: the right hours, and her lot is the one they lack | Placed next to people who want her |
| Minute 5 | She settles on Fox Street. The Riverside Lodger hands over a Level 5 Glassworks and Sand Pit: a working chain from the first minute | No empty valley in a finished world |
| Minute 12 | Her gate pipe reaches the Main. Three veterans' factories buy her sand and glass at the fixed 1.75 | Nobody can fleece her: the price is fixed |
| Days 1–3 | Players 10+ levels above her can't offer her contracts, and none with shortfall clauses before Level 6. Tailwind gives XP ×2.5 while she is more than 3 levels below the crew median. Crew Orders are sized from the members' production, so her joining doesn't make them harder, and her deliveries count ×2 | Protected by rule; the veterans lose nothing by hosting |
| Day 4 | Mo sketches a glass line on her land; she builds it with one tap. Fox's Line already reaches the Era Wonder site, so her glass rides it there, and Wonder tiers go by the share of her *own* income | Inspiration and access instead of "you're too late" |
| Day 9 | Nora is Level 15. The Rookies board lists her; Mo gets a Host star when she reaches Level 10 on her fourth different day | Veterans are rewarded for welcoming, with cosmetics only |

### (d) A griefer tries to block or exploit others

"Grim" tries everything:

| Attempt | What stops it |
| --- | --- |
| Wall a corridor with pipes to cut off Kettle Street | Links are auto-routed entities and never exclusive, drawn as bundles; nobody owns corridor tiles; the Main is laid by the game. Nothing to block |
| Settle on Kettle Street's free lot to block their 5th friend | A free lot that isn't held is just a lot. Grim becomes a neighbour, not a member, and is not on the Main. The 5th friend takes the next lot, or the crew holds lots with the invite (size + 2) |
| Join an open lot and drain the crew | On the Main he can only buy produced goods at 1.75 (selling them on to the hub at 1.0 loses money) or sell at 1.75. He can't touch the Cistern beyond his share, can't build on others' land (Sketches need the owner's tap), and his Wishes are rate-limited preset phrases. A majority of the active members takes him off the Main; he keeps his estate and gets a free move |
| Funnel gold with four alts in one crew | The fixed crew price, Housewarming capped by the receiver's level, no other gifts, Crew Order rewards per own delivery (hub imports count 25%). Four alts are simply four estates of work |
| The copy-trade exploit: a "friend" buys a lot, then quits so a Lodger keeps buying at 1.75 forever | The copy fades to the Lodger limit (about 15% of a typical player's output) within 14 days |
| Monopolise a Line and raise its toll | At most 3%, 24 h notice, the renter's week guaranteed, anyone may build a parallel line, the hub takes over an abandoned one |
| Dump iron to crash a newcomer's price | The personal hub allowance: his dumping only lowers *his own* marginal price |
| Trap a newcomer with a contract | No shortfall clauses below Level 6, no offers from 10+ levels above in the first 3 days, the fair-price band |
| Spam requests, offensive names | 5 requests an hour, the block list, the moderation tools that exist (mute, ban, rename) |
| Evict a dormant player to grab their lot | Not possible: there are no spot requests, and held lots are only opened by that crew's own majority |

The worst Grim can do is be an unpleasant neighbour whose estate you can see. Everything that touches someone else
needs that person's consent.

### (e) A player returns after a month

Ben comes back on day 45, after twelve days on the Quiet Isles.

1. **While you were away** (one screen, five lines). Era 2 has begun. The savings jar holds 31,000 g (70% of the hub
   price, capped at 7 days of income). "Ana tended your estate 14 times." Kettle Street is Crew Level 6 and has a
   Line to an oil field, and Dev joined. A 10-second timelapse of the street follows.
2. **Your estate is safe.** It floats on its island, complete.
3. **Where to land?** The first card is "Your lot on Kettle Street, held for you". One tap, and the balloons land in
   a minute. Old Wim leaves a note, and Ben's valve plugs back into the Main; his coal is on it within the hour.
- The ticker says "Ben is back!", and Ana, Cleo and Dev each get a one-tap welcome postcard.
- He gets the Era 2 jump kit (a steel-line blueprint, the first techs cheaper) and Tailwind: he is Level 9 and the
  crew median is 24.
- The "Back!" ribbon shows for 7 days, with no deadlines and no "you lost".
- If the lot had been opened (only possible from day 93, or earlier by the crew's own majority), Ben would land on the
  next free lot of Kettle Street or the street behind, with a Junction offer to Kettle Street. He would still be on his
  friends' pipe.

### (f) The late game in week 4

Kettle Street (Ana, Cleo, Dev, and Ben again) is at Crew Level 8.

- **The Main is a monument.** It is a steel pipe on arches over the corridor, the Cistern straddles the middle, and
  the Gate Arch with the kettle emblem rises at the street's end, visible from the district map.
- **Kettle Line** runs 60 tiles into the wilderness to the crew's shared oil ★5 outpost. It is a ribbon striped in
  four colours; Dev and Ben each adopted a Pump House.
- **Junctions** to Fox Street and Sam's Row. On the Lines view the district is a little metro map, and Kettle is the
  hub of three lines.
- **Slots:** 6 of the 20 slots are rented to companies outside the crew at a 2% toll. The name plate says
  "Kettle Line · 4,100 g this week".
- **Specialisation** happens because the land makes it pay (comparative advantage), not because a rule forces it:
  Ana runs energy and glass, Cleo plastics on the oil, Dev wire, Ben steel.
- **Friendly competition:**
  - the Lines Race to the Copper Cathedral site (Kettle came second: a bronze marker);
  - the crew cup per active member;
  - a Discovery announced 6 h ahead (a rich crater 40 tiles off the Line) and a race to extend towards it;
  - mastery titles ("Master Glassmaker of Ashford").
- **Expansion instead of rebuilding:** at Level 25, Ana's Franchise branch can seed a new street in the frontier
  district, with Ana as its Host. Her Line can reach it later.
- **At the chapter's end** the crew, the street and the Line stay. A small Gate Arch becomes an heirloom, and the
  next chapter's Lines Race gives week-4 players a new reason to build.

---

## 5. Score against the checklist (0–3)

| # | Point | Score | Why |
| --- | --- | --- | --- |
| 1 | Friends together | 3 | Lots on one street from minute one, lots held for 28 days with a Lodger, the Main from minute 12, elastic up to 6 |
| 2 | Choosing where | 3 | The Street Map with lit windows, residents, hours, land and what a street lacks; Pick for me matches by activity |
| 3 | The lonely player | 2 | A Street of one has a Main, orders and Lodgers, plus links and Junctions. But Lodgers' warmth is unproven, and a five-player server stays thin |
| 4 | Players who stop | 3 | Existing stages, the held lot, the fading copy-trade Lodger, successor Houses, honest link states, no evictions |
| 5 | Early game | 3 | Nothing depends on others; the Main is real pipe-laying; fixed prices and newcomer rules mean nobody can hurt a newcomer |
| 6 | Late game | 2 | Lines, Junctions, slots, the Gate Arch, Lines Races, Discoveries, cups. It depends on tuning, and competition between crews is mild |
| 7 | Async | 3 | The Main runs offline; Wishes with an NPC fill; orders a single member can finish; nothing needs two players online |
| 8 | Fair and safe | 2 | Fixed and band prices, allowances, fading copies, capped tolls. Taking someone off the Main is a possible source of drama, and alts can still farm small cosmetics |
| 9 | Clear | 2 | The crew is a coloured line on the map, with pellets in colours and the metro view. But a pro-rata Main hides "who got my coal" unless the Main panel shows the weekly flows per pair |
| 10 | Pipes are the heart | 3 | The shared object *is* a pipe; it grows into a Line built segment by segment; Junctions and slots |
| 11 | Scale | 3 | The Main is O(members) and teleporter-style; no merged networks; at most 20 links per Line; the district simulation is unchanged |
| 12 | Seasons and migration | 3 | Nothing moves and no parcel is carved; corridor pipes become links; crews are suggested from existing neighbours; streets persist |
| 13 | Social glue | 3 | Names, colours, the Gate Arch, Line name plates, the Handshake, tending, postcards, the gazette, Host stars |
| 14 | Buildable | 2 | Eight steps over about 12–16 days, each playable. The Main is an N-way teleporter, but Lines, Junctions and the arch art are the unknowns |
| | **Total** | **37 / 42** | |

## 6. Shipping in steps (each one playable on its own)

| Step | What | Builds on | Days |
| --- | --- | --- | --- |
| 1 | Islands and links (TradingRethink A): auto-routed, non-exclusive, the hub fallback, honest link states | Teleporters, the partner settings | 2 |
| 2 | Crews from Level 1, the invite link, the Street Map, held lots, crew colours on gates and pipes | Corporations (lighter), relocation | 2 |
| 3 | **The Main**: the N-way teleporter in the corridor, the Main valve, the crew price, the Handshake | Teleporters | 1–2 |
| 4 | Lodgers: names, works handed over, successor Houses, the fading copy-trade Lodger, held lots after asleep | Tenants, Caretaker, Quiet Isles | 1–2 |
| 5 | Crew Orders and Levels, the ring, Wishes with the NPC fill, tune-ups, Sketches, the crew report and ticker | Corporation weekly goals, blueprints and ghosts, the offline report | 2 |
| 6 | Lines: segments, adoptable Pump Houses, Junctions, slots and tolls, hub takeover, the Lines view | Wilderness pipes, outposts, toll meters | 2–3 |
| 7 | The Main's looks (lamps, arches, the Cistern, the Gate Arch) and the Crew Story postcard | The art pipeline, photo mode | 1–2 (plus generation) |
| 8 | The crew cup, local leagues, Rookies, Host stars, the Lines Race, the personal hub allowance (after the solo bot's balance run) | Leaderboards, Wonders, the balance simulation | 2 |

**Migration of the testers' world:**
- Nothing moves and no parcel is taken.
- Corridor pipe chains between two gates become links, and the rest is refunded (TradingRethink A).
- Everyone starts as a Street of one; tenants on their street become their Lodgers (up to two).
- Neighbours across one corridor who traded most with each other last week get a card: "Found a crew together?"

## 7. What would prove me wrong (and what to test first)

1. **Lodgers are the real product.** If a Street of one with two Lodgers is dull, solo players quit, just as they would
   in a Quad of three Lodgers. Test first: the balance bot plus one human tester playing alone for three days, rating
   the Lodgers' notes, Wishes and trade.
2. **A pro-rata Main can feel anonymous.** If players can't say "my coal goes to Ana", the Main is a small station
   market (W4 in miniature). The Main panel must show the weekly flows pair by pair. If that isn't enough, fall back
   to explicit member-to-member links on the Main (still teleporter-style).
3. **The arches may look cramped over a 2-tile corridor.** Check with `e2e/showcase.mjs` before committing to the
   Gate Arch art; the fallback is a gatehouse at the street's end.
4. **Taking someone off the Main is a source of drama.** If it's used against quiet friends rather than griefers,
   limit it to members who joined through an open lot.
5. **Six is a guess.** Watch the crew-size histogram in the first chapter; the Main's cost is linear, so the cap can
   move.
