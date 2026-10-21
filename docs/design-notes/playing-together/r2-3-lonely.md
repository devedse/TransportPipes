# Round 2 · Guardian of the lonely and the leavers

I read the brief, all six round-1 files, and the code that handles sleeping and placing players
(`GameWorld.Moving.cs`, `GameWorld.cs`, `GameWorld.Tenants.cs`). Three findings from the code matter for this
round:

- `Sleep()` calls `EndTradeOf(company, cancelPipes: true)`: when a player goes to the Quiet Isles, every contract
  with them simply ends ("Your contract with Dara ended: they moved away"). The partner's pipe now leads to an empty
  estate. **Today the game produces exactly the dead pipe that every file in round 1 warns about.**
- `WakeUp()` lands the returning player in `FindFreeEstate()`, the first free seat in district order. No choice, not
  next to friends, and not their old spot.
- `CheckQuietDistricts()` skips districts with fewer than 4 non-sleeping players. **A district with 2 or 3 lonely
  players never gets the "move to a busy district" offer**: the loneliest players are exactly the ones it misses.

Offline progress is "the world keeps running" (the trading post sells while you're away), so a resting estate keeps
earning on its own. That makes the "savings jar" from my round 1 unnecessary. I drop it.

## My veto rules (round 1's five, plus one I learned this round)

1. **Every partner can be replaced, nothing stalls.** A link never goes dead without a reason in words.
2. **Solo is a full game.** Nothing needs another human. With humans you make about 10–15% more, never ×1.5.
3. **Presence, not simultaneity.** Show who *was* here today, never how few are online now. Nothing needs two
   people online at once.
4. **Leaving is free, returning is a gift.** Absence never costs what you built, your seat while your friends hold
   it, your earned ranks, or your friends' progress.
5. **AI is honest.** NPC companies are labelled, never pretend to be people, and never take land, ranks or rewards
   from humans.
6. **(new) Every push towards others must disappear when there are no others.** A mechanic that nudges you to trade
   with players ("the hub is full of your ingots, find a buyer") is a tax on whoever has nobody to trade with. In a
   quiet place the nudge has to switch off.

---

## Part 1 · The five weakest ideas, attacked

### W1. Rewards only humans can unlock: Quartet Recipes (6 #15) and Relay Recipes (1 #13)

*"Special season items need four different station goods from four different people … caretakers can't supply
quartet inputs, only humans can."*

**Failure scenario.** Kettle Works is Ana, Ben, Cleo and Dev. Ben stops on day 3. From day 3 to the end of the
season the Quartet slot in their Junction shows a grey quarter with Ben's icon on it, every time they log in. On day
9 Ana writes in the group chat: "Ben, can you just log in and send copper once a week? We need you for the
trophy." Ben feels guilty. Guilt in a hobby turns into avoidance: he mutes the group chat and never comes back.
Meanwhile Mia, a solo player, sees the season's trophy item on every leaderboard and can never make one, because her
three seats are caretakers. In week 2 a forum post explains the best strategy: "open 3 browser profiles, make 3
alts, you get the Quartet". The most valuable item of the season rewards multi-accounting and punishes quitters'
friends.

**Veto:** rules 2 and 4. **Fix:** recipes that need several *inputs* or *chains*, not several *people*. "Made in a
crew" can be a cosmetic stamp on the item (four colours on the crate), not a gate.

### W2. Attendance multipliers: the diversity bonus (2 #7), the clock tower (1 #16), the Shift Log (6 #9), the Night Market (6 #29)

*"The weekly crew reward is ×1.0 when one member contributed, ×1.2 for two, ×1.35 for three, ×1.5 for four."*
*"The clock tower gives a boost while someone in the hamlet has tended it in the last 8 hours."*

**Failure scenario.** Week 2 at Kettle Works: Ben is resting. The crew reward drops from ×1.5 to ×1.35, and the
Crew panel says so. The ring around the Yard project has three colours where it used to have four. Nobody blames
Ben, but everyone can do the maths: Ben costs the crew 10% every week. On day 12 Cleo opens Ben's seat for a
stranger "so we get the ×1.5 back". When Ben returns on day 20 he finds that he was swapped out to keep a
multiplier. The clock tower is worse: three friends in one time zone sleep 00:00–07:00, so it can never be kept lit,
and a solo player can't keep it lit at all. It is a 24-hour attendance streak dressed up as a cute building. The
Night Market rewards being online in one particular hour.

**Veto:** rules 2, 3 and 4 (this is Habitica's party damage again, see 5-lessons). **Fix:** keep the warmth and
drop the multiplier. The Yard's festoon lights get one colour for each member who contributed this week (purely
cosmetic). Crew goals are sized to the *active* members. Rewards go to each person by their own share.

### W3. Adoption of abandoned estates (1 #30)

*"An abandoned estate can be adopted by a hamlet neighbour as a temporary annex. The adopter runs it and keeps a
share."*

**Failure scenario.** Eli and Dara are neighbours. Dara stops on day 5 (a family holiday, two weeks without
internet). On day 14 Eli adopts her estate. He points its pipes at his own factories, sells her 40k bricks from
storage at the hub for his share, and switches her Glassworks to the sand he needs. Dara comes back on day 22. Her
storage is empty, her own partners were dropped, and her estate is "returned with a bonus" that doesn't undo the
feeling that a stranger lived in her house. Worse, the incentive is perverse: Eli *profits* from Dara staying away.
By week 3, veterans surrounded by quitters run three estates each, and the alt-account trick (make an alt, let it go
dormant, adopt it) appears on day 10.

**Veto:** rules 4 and 5 in spirit (nobody may profit from someone else's absence), plus fairness. **Fix:** an
absent estate is never run by another player. It runs itself (idle), its links are kept by an NPC caretaker, and
after 30 days it is packed and the *seat* goes to a named NPC that copies its trades (§4.2). Nobody gains from your
absence, so nobody wants it.

### W4. The stacked solo tax: home goods + personal hub saturation + scarce tier-3 imports (4 M1, M7, M8)

Each of these alone is a good idea. Stacked on a lonely player they compound.

**Failure scenario.** Mia plays alone on a server with 5 players. Her land's home goods are iron ★5 and stone ★4,
so coal, copper and sand are ★2 (×0.75). Day 9: her iron ingots reach the hub's hourly allowance. The UI says: "the
hub is full of your iron ingots. Your station buys at 1.4, Anna's Steel Mill would pay 1.75." There is no Anna. The
station market's demand is three tenants buying 300/h; her surplus is 3,000/h. Above the allowance the hub pays 0.5,
then 0.25. Day 15, Era 2: steel needs coal, which is ★2 on her land. Tier-3 imports are allowed for 2 hours of one
factory per day at 2.5, then cost 4.0. Her income curve flattens exactly when a solo player needs momentum most. The
game's advice, "find a partner", is impossible on her server. The promised "~10% slower" turns into ~35% slower in
week 2 (my estimate: −25% on her saturated main product, −15% on steel costs, −10% from ★2 inputs). She quits on day
16.

**Veto:** rules 2 and 6. **Fix: quiet-district grace.** The allowance and the tier-3 import cap scale with how many
lit seats (played in the last 3 days) the district has: ×1 with 12 or more, ×2 with 6–11, ×4 (effectively off) with 5
or fewer. The panel says "The hub is generous in quiet districts". Comparative advantage stays, because it makes trade
worthwhile without ever being a wall.

### W5. My own: "Spot request" (round 1 §3.3), plus the Rival House and Houses that follow the server median

*"An active player can request a dormant (14+ days) neighbour's spot for a friend … without an answer, the estate
goes to the Quiet Isles early."*

**Failure scenario.** Dara is in hospital for 3 weeks. On day 15 her neighbour Rex files a spot request "for a
friend". Dara doesn't read the 72-hour notice, because she is in hospital. On day 18 her estate is packed and
shipped to the Quiet Isles. On day 22 she comes back to find her spot taken by Rex's friend, her old neighbours'
links re-pointed, and a "free move" to somewhere she never chose. I wrote "fine, because the Quiet Isles lose
nothing" in round 1. That is false: she lost her *place*. It is also griefing: Rex can evict any dormant player once
a week. It breaks my own rule 4 and my own anti-pattern 11. **I withdraw it.** The only early pack-up that survives
is narrow probation for brand-new strangers who take a crew's open seat and vanish (§4.4), because a 3-day-old
estate has no place yet to lose.

Two more of mine fall too:
- **The Rival House** ("always 5–10% ahead or behind you") is rubber-banding. Players notice by week 2, and then it
  feels fake. On a server of 5 it can beat the only human on the district board, which breaks rule 5.
- **House level = the median active player's level − 2** puts Level 20 NPCs at the top of a 5-player server's boards.
  Lodgers must follow *their own seat's neighbours*, never appear on boards, and never be the top contributor to
  anything.

### Rejected in one line each

- **Land rush at season start (1 #9):** whoever is asleep, on holiday or in the wrong time zone during the 24 hours
  gets the leftovers for a whole chapter. A synchronous event with permanent consequences.
- **The accordion map (1 #7):** you open the game and your neighbours have moved, your link is 40 tiles longer and
  loses 12% instead of 3%. Neighbours are a relationship (FoE's reshuffles, see 5-lessons), not a layout.
- **Echoes (1 #32, 6 #17):** "a recorded surplus curve of a real player arrives as if from a partner". It is a fake
  partner, and it breaks rule 5 the moment someone finds out.
- **Seed colonies (1 #19):** a veteran founds an estate for someone else and takes a share forever. That is
  feudalism, and an alt farm.
- **Live cursors (2 #4):** harmless, but they reward being online together. Fine as decoration, never with a
  reward attached.
- **Autopilot reinvest (my round 1 §2.2):** a caretaker spending someone's gold without asking. When Ben returns
  he wanted to save for a parcel, and his gold went into two Brickworks upgrades. I replace it with NPC growth
  (§4.2): when a Quad has demand nobody meets, a Lodger grows, not the absent friend's wallet.

---

## Part 2 · The five strongest ideas

### S1. Estates are islands, links are two-ended, every link has a hub fallback (4 M5, 1 #21, TradingRethink A)

This is the foundation everything I care about rests on. A link is an *object* with two named ends, so it can
outlive one of them: it can be handed to a successor, capped, or shown with a reason. In a merged network a leaver
punches a hole you can't even see. With links, the leaver is one row in one panel. The automatic hub fallback ("your
consumer is fed from the station market, then the hub, up to the lost amount") turns every dependency into upside
only.

### S2. One NPC concept for every empty seat: Lodgers that keep seats warm, hand over their works and copy a leaver's trade (2 #9–#11, 6 caretakers)

*"The Lodger's buildings become Dev's, intact, not ruins"* and *"a Lodger moves into his seat that copies his last 7
days of trade."* One system covers the solo player (a Quad of Lodgers), the reserved seat for a friend who is late,
the absent friend, the friend who left for good, and the newcomer's first minute (a furnished seat). It is 5-lessons'
rule made into a mechanic: *absent players degrade gracefully into something an NPC could be.* It is cheap, because
tenants exist already.

### S3. The station or Yard market: a liquidity ring that never depends on one person (4 M4)

Every estate's surplus is pooled pro rata with its neighbours' needs at 1.4 (sell) / 2.0 (buy), and tenants take
part. A solo player always has a market that is better than the hub. A quitter can't leave a hole, because the
market is the sum of everyone and not any one person. It is automatic, so a newcomer needs no decision, and the
solve is O(estates). It is the strongest single anti-loneliness mechanic in all six files.

### S4. Requests with a safety net, and favours that bank (5 #5 and #8, 2 #8 Wishes, 1 #20 bounty pins)

*"At the deadline, a tenant fills the remainder at the hub price, so the requester is never stuck and nobody failed
anybody."* *"Favours bank up for up to 3 days so missing a day costs nothing."* These two sentences fix the two
worst feelings of async co-op: being let down, and being the one who let someone down. Both are one tap, both work
between strangers, and both let a solo player give and receive.

### S5. Goals sized to the active members, rewards by your own share, contribution floors (4 M12/M13, 2 #7 "one player alone can always finish one", 5 Egg Inc. lesson)

A sleeper costs nobody anything, a leech gets little (below the floor you get a 25% "thanks" share), a newcomer can
always qualify, and a solo player's station still finishes its Works because tenants deliver part of it. It is the
counter-design to W2. Hand in hand with it go the **positive-only presence** ideas (5's Death Stranding template,
2's lit windows and sleeping cat, 6's coloured flow): other players' work shows up in your world and you can only
thank it.

---

## Part 3 · Stress test: the Quad (4 seats around a Yard)

Files 1, 2, 5 and 6 converge on 4–6 estates around a shared Yard, junction or well. From my angle it is the right
unit (Dunbar's 5, a table you can read at a glance), but as written it breaks in three situations.

### 3.1 A Quad where 3 of 4 have quit

Ana is alone with three sleepers from day 30. What 2-friends.md does, as written:

| Problem | What Ana sees | Why it hurts |
| --- | --- | --- |
| **Cap cliff** on day 30: Lodgers trade about 30% of a player's volume | Her coal from Ben drops from 1,200/h to 360/h. She sells less in the Yard at 1.75 and more at the hub at 1.0, and buys more at 2.5 | My estimate: −15 to −20% income on the day her friends leave. **Punished twice** |
| **Seats held for 60 days** by default | Three seats with "held for Ben/Cleo/Dev", no recruits possible | Ana sits at a table of three empty chairs for two months |
| **Shared decisions** (nobody can demolish a Yard building alone, the founder opens seats) | If the founder was Ben, nobody can open his seat | Lock-in by an absent player |
| **The Crew Wall** of banners | Three banners on a wall in her Yard | A memorial to the people who left her, shown every day |
| **The crew stays behind** | If she moves to a lively Quad, the Crew Level and Landmark stay in the empty Yard | Moving costs her everything social she built |

**Fixes (in the revised design):** the Lodger copies 100% of the leaver's trade for 14 days and then follows the
Quad's demand (up to 60% of a human's volume), so there is no cliff. Decisions are taken by the *active* members only,
and one active member decides alone. Seats are held, but the remaining member is offered once, kindly, "open one
seat?". The crew (name, Level, Landmark as an heirloom) **moves with its members**. Sleepers are shown as balloons tied
to the Yard fence with a postcard count, not as a wall.

### 3.2 A solo player surrounded by Lodgers

2-friends.md: *"The solo player gets a Quad with three Lodgers."* Failure scenario: Mia, day 1–14. The Lodgers
are capped at about Level 5, so by week 2 her table-mates buy little and sell little. The diversity bonus gives her
×1.0 against a full crew's ×1.5, so she gets two thirds of a crew's reward forever. Streets need an adjacent *crew*
that agrees, and the Quads next to her are Lodger Quads too. 6-outsider names the feeling: *"I'm playing with
bots."* And worst of all, an all-Lodger Quad as the default means **the game puts lonely players in separate empty
rooms** while three other Quads each have one or two humans with open seats.

**Fixes:** the default for a solo player is **"Join a table"**: an open seat in a Quad with 1–3 active humans whose
usual hours match. A Quad of your own with Lodgers is an option, not the default. Lodgers grow with their human
neighbours. Crew rewards are per active member (W2), so a solo table loses nothing against a full one.

### 3.3 A server that shrinks from 200 to 20 players

At 200 players about 9 districts are open. At 20, there are about 2 humans per district, most Quads have 0 or 1
human, and every lit window is far from every other. Today `CheckQuietDistricts` never fires in a district with
fewer than 4 non-sleeping players. With Quads it gets worse, because Quads make loneliness *visible* (one lit seat
of four).

**Fixes, "gravity":**
- **Newcomers and returners** are placed only in the 1–3 districts with the most lit seats, and new districts open
  only when the open ones are 60% lit.
- **A lone human**, the only lit seat in their Quad for 7 days, gets *one* card: "Kestrel Quad (2 players, evenings,
  Lv 12–15) has a free seat. Move there for free; your crew name and Crew Level come with you." Up to 3 options. After
  that it is a button in the Quad panel, never pushed again for 14 days. Never automatic.
- **Districts with no lit seat freeze**: no tick for their Lodgers, so a world of 20 players costs what a world of 20
  players should.
- Fix the code: the district check counts lit seats, not "≥ 4 players".

Over two weeks the 20 drift into 1–2 lively districts by choice.

---

## Part 4 · The revised design: "No empty tables"

One coherent design, built from everyone's best parts. My additions are the lifecycle of a seat, the gravity, the
return, and the guards.

### 4.1 The shape of the world (from 2-friends, 4-economy and TradingRethink A)

- **District** = hub + 4 **Quads** (the corner 2×2 groups, 16 seats) + 4 **Duos** (the arms, 8 seats). Each Quad
  has a **Yard**, built from the four inner-corner parcels and the corridor crossing (2-friends' geometry).
- **The Yard's Depot is game-owned and has one socket per seat.** Each resident lays their own short pipe (6–20
  tiles) from their Yard gate to *their* socket. Nobody can touch another seat's socket or pipe, and there are no
  free-form shared buildings to grief. A Duo's porch can join the next Quad's Yard (at most 6 sockets).
- **Three rings of trade (4-economy model C):**

  | Ring | Price | With whom |
  | --- | --- | --- |
  | **Crew price** | 1.75 both ways (fixed, no spread) | Members of the same crew, in the Yard |
  | **Yard market** | sell 1.4 / buy 2.0, 0.1 to the Yard chest | Every other resident: strangers, Lodgers, a Duo's porch |
  | **Links** | a position in the band hub-buy to hub-sell, Auto = middle | Any two gates you choose, non-transitive, with an automatic hub fallback |
  | **Hub** | sell 1.0 / buy 2.5, personal allowance with **quiet-district grace** (W4) | Always |

  Effect on rule 2: a solo player at a table of strangers and Lodgers sells their Yard share at 1.4 instead of 1.75.
  On about 30% of their output that is about 7.5% less income than a crew. Worth inviting a friend, never worth
  quitting over.

- **Crews** (2-friends #1, 4 M10): formed at Level 1 by an invite link; name, emblem, colours; seats reserved for 7
  days, renewable by any active member up to 28 days; **decisions by active members only**.

### 4.2 The life of a seat (my core, built on S2)

Every seat is in exactly one state, and the state is visible on the HQ and in one line on every link to it.

| State | When | The seat | Its links (what partners read) |
| --- | --- | --- | --- |
| **Lodger** | Never taken, or reserved for a friend | A named NPC company (4 archetypes: Reliable, Grower, Collector, Neighbour in need) with starter works | "Bertha's Brickworks (NPC) buys your stone at the Yard price" |
| **Active** | Played in the last 3 days | Lit windows at night | "Ana buys 1,200 coal/h" |
| **Resting** | 3–14 days away | A sleeping cat on the HQ steps. Everything runs and earns. The caretaker renews good contracts from day 3 (not 14) | "Ben is resting. His estate keeps running; this link keeps delivering." |
| **Dormant** | 14–30 days | Ivy on the HQ. Caretaker fee 10% (as today); crew and corp roles pass to active members | "Ben's caretaker keeps this link at the same price." |
| **Asleep** | 30+ days | The estate is packed onto balloons to the Quiet Isles. **Announced 72 h ahead** to everyone linked. The seat goes to a **successor Lodger** | "Ben is on the Quiet Isles. Old Pete's Colliery (NPC) keeps your coal coming." |
| **Returning** | Any time | Three screens, you choose where to land (§4.6) | Old links are offered back in one tap |

**Rules that make it work:**
- **Successor Lodgers copy the leaver's links**: same goods, same prices, 100% of the last 7 days' volume for 14
  days, then they follow the Quad's demand like any Lodger. **No contract or link ends on sleep.** It is handed over.
  This replaces `EndTradeOf(..., cancelPipes: true)` in `Sleep()`.
- **Top-up on any link that runs short**, for any reason (a resting partner whose own inputs dried up, a capped
  link): the shortfall is filled from the hub at the *link* price, up to the link's 7-day average, for up to 14 days.
  Partners never see a starving factory because someone else went quiet.
- **Lodger strength:** works level = the median Level of the human residents in its Quad − 3 (at least 5).
  Throughput up to 60% of a human's for its specialty. It **never** delivers more than 25% of a shared goal, never
  appears on a board, never counts toward Wonder ranks, and never answers a Wish beyond its specialty.
- **NPC growth instead of the absent wallet:** when a Quad has demand that nobody meets for 24 h (e.g. Ana's
  Smelter wants 1,800 coal/h and Ben's frozen estate makes 1,200), the Lodger of that specialty (or the Yard market's
  hub import) grows to cover it, and Ana's panel says so. A resting friend's estate is never changed by anyone.
- **Lodger voices:** all of a Quad's Lodgers together write at most one short letter a day, as a digest.
- **Capped link:** a link whose far end is an empty seat with no Lodger yet (rare, e.g. a frozen district) shows a
  brass cap: "No one at this gate yet. Your goods go to the Yard market and hub meanwhile." Whoever moves in is offered
  the link first ("Ana already has a coal pipe at your west gate. Connect?"). That is an instant partner for a
  newcomer.

### 4.3 Presence without simultaneity (S5, 2 #4, 5 #11, my round 1)

- **Lit windows** for played in the last 24 h. The district header says "Lights on in 11 of 24 seats today",
  never "2 online". The Seat Map shows lit seats per Quad.
- **"Since you last looked" sparkles** on changed estates, each with a 4-second timelapse.
- **Usual hours** on every banner ("usually here: nights"), from 14 days of sessions in 4 buckets.
- **The Quad chronicle** (5 #11): one auto-written line a day ("Mia's sand made Jo's first glass"). The district feed
  gets one line per Quad.
- **Sleepers as balloons** tied to the Yard fence, each with the number of postcards waiting for them.

### 4.4 Async help (S4)

- **Wishes** (2 #8): one active Wish per player, pinned to the gate, for up to 48 h. Any resident can answer with a
  standing order ("my spare coal to Cleo first, 24 h"). **At expiry the Yard market or the hub fills the rest at the
  hub price**, so a Wish never fails. Nobody sees "unanswered".
- **Favours** (5 #5): 3 a day, banked up to 9. A **tune-up** gives one building +10% for 8 h. A favour on a resting
  or asleep friend's estate becomes a **postcard** they find on return ("Ana tuned up your mine 9 times").
- **Gift crates** (a compromise with 4-economy's "no gifts"): worth at most one hour of your own production of one
  good, one crate a day per pair, three a day in total, so an alt can't carry more than it makes.
- **No social timer shorter than 12 h, no offer that expires before 72 h, and at most 1 push a day** in the player's
  chosen window.

### 4.5 Gravity: placement, merging, quiet places

- **Placement score per free seat** (the default when you don't choose):
  +3 for each active human resident within 10 Levels · +2 for matching usual hours, −2 for opposite ones · +2 for a capped link waiting at
  one of its gates · +2 for a Host (§4.7) · +1 for a Lodger with your missing specialty · −3 for a Quad with no lit
  seat while lit Quads have room.
- **The start screen** offers three buttons: *Start with friends* (crew), *Join a table* (the best open seat at a
  table with humans, **default**), *A place of my own* (a Quad or Duo of Lodgers next to the liveliest Quad).
- **The lone-human card** (§3.3), once, then a button.
- **The crew moves together:** a crew's name, Crew Level, Yard projects in progress (as credit) and Landmark (as an
  heirloom model) travel with its members to a new Quad.
- **Quiet-district grace** (W4): hub allowance and tier-3 import cap ×1 / ×2 / ×4 for 12+ / 6–11 / ≤5 lit seats.
- **Frozen districts:** no lit seat means no Lodger tick.
- **Probation for open-seat strangers:** someone who takes a crew's open seat and is then away 5 days in a row within
  their first 14 is packed to the Quiet Isles early (they lose nothing, return is free and they choose where to land; the
  seat is offered back first if it's still open). It applies **only** to strangers in someone else's crew seat in
  their first 14 days. Friends and long-time residents are never moved early.

### 4.6 Coming back (fixing `WakeUp()`)

1. **While you were away**, one screen with 5 items at most: what you earned (the world kept running), what the
   world did (Era 2 began), who missed you (postcards), what your partners are doing, a 10-second district timelapse.
2. **Your estate is safe**: shown on its island, complete.
3. **Where do you land?** Up to 4 cards: *your held seat* (if your crew held it), *your old seat* (if free),
   *next to your crew or an old partner* (from the partner history), *a lively table* (placement score). Landing
   takes 1 minute. The successor Lodger moves out with a thank-you and hands the links back.
- **Era kit** per Era you missed: a blueprint of its standard chain, one crate of its first building, and its first
  3 techs at the diffusion discount −25%. It never expires.
- **Tailwind** (2 #10) also for returners: XP ×2 while more than 5 Levels below the active median of your Quad (or
  district), for at most 14 days.
- **A "Back!" ribbon** for 7 days. Neighbours can send a one-tap welcome, and Lodgers always send one, so the first
  session never ends with zero postcards.

### 4.7 Newcomers among veterans

- **Furnished seats** (2 #10): a Lodger's works (Level 5, the seat's specialty) are handed to whoever takes the seat.
- **Hosts:** players of Level 15+ can light a Host lantern. Newcomers are placed next to Hosts first. A **welcome
  bonus** adds +15% to everything the newcomer sells to the Host (through the Yard or a link), **paid by the game,
  not the Host**, for 7 days, capped at 5,000 g. The Host earns
  **Host stars** when a hosted newcomer reaches Level 10 *and* has played on 4 different days. The rewards are
  cosmetics and titles, no gold.
- **Fair-price band:** on a link between companies 10+ Levels apart, the lower one sells at hub price or more and buys
  at hub price +5% or less. No contracts with shortfall clauses below Level 6 (4 M11).
- **Tailwind:** XP ×2 while more than 5 Levels below the Quad's or district's active median, for at most 14 days.
- **Leagues** (5 #10): weekly, about 25 companies of similar Level. **With fewer than 10 active companies on the
  server, leagues pause**, and the board shows "your best week" and the **Quad Cup** (per active member, best 3 days).
- **Wonder and Works tiers by your own share** of your own output, not absolute amounts.

### 4.8 The long game (from 4-economy, 2-friends and 6-outsider, guarded)

- **Yard Works** (4 M12): three stages of baskets, the last one a sustained flow ("800 steel/min for 6 h") buffered
  by Yard tanks, so 5-minute players count. Sized to the active humans, with Lodgers delivering up to 25%.
- **Trunk lines** (4 M9, 2 #15): consortium-built, 20 slots, names and colour stripes on the segments, rentable lanes.
  **When every owner is dormant, the hub pays the upkeep and the renters keep their lanes**: a trunk never dies with
  its builders.
- **District Line Race** (2 #15) and **the Great Pipe** as a world ledger (6 C) with personal thresholds.
- **Recognition**: mastery titles (4 M14), Host stars, trunk name plates, the Landmark.

### 4.9 Guards against griefing and exploits

| Attempt | Closed by |
| --- | --- |
| Pipe-wall a corridor | Links are auto-routed bundles; nobody owns a corridor tile (4 M5) |
| Squat or wreck the Yard | One socket per seat; the Depot is game-owned; no free-form Yard building |
| Take a crew's open seat and idle to block it | Probation (§4.5) |
| Flood a newcomer with cheap offers | Fair-price band, 5 requests an hour, no offers from 10+ Levels higher in a newcomer's first 3 days |
| Alt crews for crew price or rewards | Crew price fixed (it moves goods, not gold); band caps value per unit; same-device links Auto only; no diversity multiplier exists; Host stars need 4 distinct days |
| Contract trap: promise supply, then stop | Contract tank of 2 h, hub top-up, shortfall paid as the difference (capped 10%) |
| Quit-and-return to farm welcome gifts | Era kit only for Eras actually missed; the welcome package once per 30 days away |
| Push a dormant neighbour out for their spot | Not possible (Spot request withdrawn) |
| Abusive names or banners | Emblems from a set, name filter, reports; chat optional, postcards are preset lines |

---

## Part 5 · Play-throughs

### (a) Ana, Ben and Cleo start tonight; Ben quits on day 3; Dev joins on day 8

| When | What happens | What they see |
| --- | --- | --- |
| **Day 0, 21:00** | Ana taps *Start with friends* → "Kettle Works", kettle emblem, teal & copper, and shares the link | A preview card in the group chat: "3 seats free" |
| 21:03 | Ben and Cleo open the link. The Seat Map defaults to the freshest Quad next to the liveliest one. They each drag a banner onto a seat: Windy Heights (Ana), Coal Valley (Ben), Riverside (Cleo). Copper Gulch is **reserved for Dev**, and Lodger *Bertha's Brickworks* keeps it warm | Three balloons land at once; the crew banner unfurls in the Yard |
| 21:05–21:12 | Personal tutorials, nothing shared yet | Each one's own estate works alone |
| 21:12 | Each lays a pipe from the Yard gate to their own socket. Second pipe in: the **Handshake** | Pellets in three colours meet at the Depot; crew price 1.75 |
| 21:20 | First Crew Order (sized to 3 humans + Bertha's 25%): done in 8 minutes | The Water Tower rises; three colours on the festoon lights |
| **Day 1–2** | Short sessions at different hours. Cleo pins a Wish "coal". Ben answers from the bus with a standing order | Morning report: "Ben's coal kept your Smelter running all night" |
| **Day 3** | **Ben stops** (nobody knows yet). His estate keeps mining and selling | Nothing changes. His coal keeps flowing |
| Day 6 | Ben has been away 3 days → **Resting** | A sleeping cat on his steps; his link says "Ben is resting; this link keeps delivering". Crew Orders resize to 2 active humans. No multiplier drops |
| Day 6 | Ana spends a favour on Ben's mine | It turns into a postcard for Ben. If Ben opted into push: "Your coal earned 2,100 g. Ana tuned up your mine." (1 of at most 3 messages) |
| **Day 8, 19:00** | **Dev opens the old link.** His seat card: "Copper Gulch, reserved for you. The crew imports 1,100 copper a day from the hub" | Bertha leaves with a cake on the Yard bench; her Level 5 Copper Mine, Smelter and wind turbine become Dev's |
| Day 8–12 | Dev's copper replaces the hub imports. Tailwind ×2 XP (he is 10+ Levels below the median). Each friend may send one housewarming crate (capped) | The ticker: "Dev's copper saves the crew 840 g/h". Day 12: Dev is Level 9 |
| Day 13 | Ana's Smelter has grown and wants 1,800 coal/h; Ben's frozen estate makes 1,200 | The link panel: "Ben's coal: 1,200 of your 1,800/h. The other 600/h come from the hub; Fox & Sons next door sell coal, link?" Nobody touched Ben's estate. (In a Quad with a Lodger, the Lodger would grow to cover it) |
| Day 17 | Ben has been away 14 days → **Dormant** | Ivy; caretaker fee 10%. He was no officer, so no roles change |
| Day 30 | 72-hour notice to Ana, Cleo, Dev: "Ben's estate will sail to the Quiet Isles on Thursday. His seat stays **held for him** (change). A Lodger will keep his coal coming at the same volume." | One inbox card with three choices; the default is held |
| **Day 33** | Ben sleeps. Successor Lodger *Old Pete's Colliery* takes his seat and copies his links: 1,200 coal/h to Ana at Ben's crew price (1.75) for 14 days, then follows demand | Ben's banner becomes a balloon on the Yard fence: "Ben · on the Quiet Isles · 6 postcards". No Smelter goes cold |
| Week 4+ | Ana, Cleo and Dev play on: Yard Works stage 2 sized to 3, the Landmark (§f) | The crew never had a reason to replace Ben, so he still has a place |

What never happened: a dead pipe, a multiplier lost because of Ben, guilt messages, a stranger in Ben's seat, Dev
starting from nothing.

### (b) A solo player on a server of 5

The world: one open district. **Tom & Lee** (friends, Quad NE, two Lodgers, one seat opened to strangers, play
evenings), **Jo** (solo, Quad SW with three Lodgers, plays nights), **Kai** (played twice, resting). **Mia**
arrives on day 4 at 02:40.

| When | What happens | What Mia sees |
| --- | --- | --- |
| Day 4, 02:40 | Start screen → **Join a table** (default). The scores: Jo's Quad +3 (a human), +2 (nights) = 5; Tom & Lee's open seat +6 (two humans) −2 (evenings, against Mia's nights) = 4; a place of her own 1 | "Jo's table: 1 player, usually here at night, Lv 7, looking for sand". She takes Riverside next to Jo |
| 02:55 | Solo tutorial; the Yard socket at minute 12 | Jo's Glassworks buys her sand at the Yard price of 1.4, Lodger *Willowmere Bakery* buys her energy. Jo's windows are lit: Jo played an hour ago |
| Day 5–7 | Jo pins a Wish "sand for my second Glassworks"; Mia answers with a standing order at 03:10; Jo sees it at 23:00 | Chronicle: "Mia's sand made Jo's first glass". On day 7 a card: "You and Jo traded 4,000 g this week. Form a crew?" They do. Crew price 1.75 |
| Day 9 | Mia's iron ingots pass the hub allowance | **Quiet-district grace** (5 lit seats or fewer): ×4, no halving. Her panel: "The hub is generous in quiet districts" |
| **Day 12** | **Jo stops** | Day 15: the sleeping cat. Jo's estate keeps buying sand. Crew goals resize to 1 human |
| Day 19 | Mia has been the only lit seat at her table for 7 days | **One card:** "Tom & Lee's table has a free seat (evenings, Lv 10–12). Move for free; your crew comes with you." She declines: she plays at night and likes her river. The card becomes a button |
| Week 2–3 | Leagues paused (fewer than 10 companies). The board shows "your best week" and the **Quad Cup** per active member, best 3 days | Mia wins the Quad Cup in week 2 alone, against Tom & Lee's two-player table |
| Day 20 | District Works stage 1: sized to 3 active humans; Lodgers deliver 25% | Finished on day 23. Mia's name is on the plaque next to Tom's and Lee's |
| Day 24, 03:05 | A sixth player, **Sam**, arrives and is placed at Mia's table (+2 nights, +3 a human) | "Sam moved in at 03:05" with balloons. Mia, now Level 15, lights a Host lantern; Sam gets the welcome bonus |

What never happened: an empty district, "0 online", income halved because nobody else exists, a forced move, a bot
pretending to be a player.

### (c) A newcomer in week 3 among veterans

The server has 200 players. **Noor** starts on day 22.

| When | What happens | What Noor sees |
| --- | --- | --- |
| Day 22, 18:30 | *Join a table* → 3 cards: **Kettle Works' table** (3 active, Lv 18–24, a Host lantern, a furnished copper seat, evenings CET); **a Frontier Duo** (everyone here started this week); *Pick for me* | She picks Kettle Works' table |
| 18:32 | Lodger Bertha hands over her Level 5 Copper Mine, Smelter and turbine | Her estate is not empty: three buildings already hum |
| 18:45 | Yard socket: Yard market at 1.4 (she isn't crew yet). Host Ana's **welcome bonus**: +15% on everything Noor sells to Ana, paid by the game | First sale: "Ana's Smelter bought 300 copper (+15% welcome bonus)" |
| Day 22–29 | Tailwind ×2 XP. Fair-price band on every link to the veterans. No offers from 10+ Levels higher for 3 days | Level 1 → Level 13 by day 29. Her **league**: 25 companies of Level 1–12; she's 4th |
| Day 25 | The District Wonder: tiers by **her own share** of her output | She reaches tier 2 with 20% of her copper, the same tier as Level 24 Ana |
| Day 26 | Kettle Works invites her into the crew (active members decide) | Crew price 1.75; her colour joins the festoon lights |
| Day 30 | She reaches Level 10 on her 6th distinct day | Ana gets a Host star; Noor gets a "Welcomed by Ana" plaque in the Yard |

What she never saw: a price below the hub from a veteran, rank 180 of 200, an empty seat, a gate to progress that
needed someone else.

### (d) A griefer who tries to block or exploit

**Rex**, Level 16, enjoys annoying people. Day by day:

| Day | Rex tries | Result |
| --- | --- | --- |
| 10 | Lays pipe along the corridor between two Quads to wall off a link route | Links are auto-routed bundles; no corridor tile can be owned. His pipe is one more line in the bundle |
| 11 | Takes the open seat of a friends' Quad and goes idle to block it for a month | Probation: away 5 days in a row within his first 14 → packed to the Isles on day 16; the seat goes back to the Lodger and the crew is told "the seat is open again" |
| 12 | Offers newcomer Noor a link at 0.6× hub for her copper | Fair-price band: the link can't go below hub price for her; the offer card shows it at 1.0 or not at all. A 10+ Level gap blocks offers to her for 3 days anyway |
| 13 | Signs a 7-day contract to supply Noor's Smelter, then stops delivering at night | Contract tank (2 h), then the hub tops up at the contract price; Rex pays the difference (capped 10% of the week's value). Noor's Smelter never stops |
| 14 | Makes three alts as a "crew" to trade at the crew price and farm crew rewards | The crew price moves goods, not gold; the band caps the value per unit; same-device links run at Auto only; no diversity multiplier exists to farm; rewards are by share with floors |
| 15 | Files a "spot request" to push a dormant neighbour out | The mechanic doesn't exist |
| 16 | Names his company something offensive | Emblems from a set, name filter, reports; postcards are preset lines |
| 20 | Quits and returns repeatedly to farm the welcome package | Once per 30 days away; the Era kit only for Eras actually missed |
| 25 | Floods the Yard market with sand to drown Mia's price | The Yard market is pro rata at fixed prices; his sand gets the same share as hers, and the excess goes to the hub at *his* allowance |

Nothing Rex does reaches anyone else's buildings, income or seat.

### (e) A player who returns after a month

**Ben** (from a) has been asleep since day 33 and opens the game on day 41.

| Screen | Content |
| --- | --- |
| 1 · While you were away | "Your estate earned 31,000 g before it went to sleep. Era 2 began on day 20. **Ana tuned up your mine 9 times, Cleo 4, Dev 2.** Dev joined the crew on day 8. Old Pete kept your coal going." A 10-second timelapse: the Yard got a Water Tower, a Workshop and a half-built kettle Landmark |
| 2 · Your estate is safe | His estate on its island, all buildings at their levels |
| 3 · Where do you land? | **Your seat at Kettle Works, held for you** (default) · next to an old partner · a lively table · the Frontier |

He taps the held seat. One minute later his balloons land. Old Pete leaves a sack of coal and a note and hands back
the links: Ana's coal link continues without a gap. The ticker for the crew says "Ben is back!" and the "Back!" ribbon
is on his banner. He finds 15 postcards and 3 welcome crates. The **Era 2 kit** gives him a steel-line blueprint and
a Steel Mill crate. **Tailwind** ×2 XP, because he is Level 7 against a median of 21. Crew Orders resize to 4.

Variant: **Kai** (from b) returns on day 45. Nobody held his seat, and it's now Sam's. He gets the same three
screens. His cards: "Mia's table, 1 free seat, nights, a Host" or "Tom & Lee's table". `WakeUp()` no longer
drops him into the first free seat.

### (f) The late game in week 4

| Who | What they do | Lonely and leaver guards |
| --- | --- | --- |
| **Kettle Works** (Ana, Cleo, Dev, Ben back, Noor) | Yard Works stage 3: "800 steel/min for 6 h", buffered by Yard tanks. Dev's evening, Ana's morning and Noor's after-work sessions all feed the same tanks | Sized to 5 active humans; nobody has to be online in the 6 hours, the tanks carry it |
| **Kettle Works + Fox & Sons** | A steel trunk line to the District Wonder site, 180 tiles, 20 slots, segments striped in their colours | When every owner of a segment is dormant, the hub pays its upkeep; renters keep their lanes |
| **Mia** (b's world, the night owl) | Rents a lane on a public trunk to the Wonder; delivers her own share; earns her mastery title "Ironmonger" | Personal tiers by share; leagues paused on a small server, so the Quad Cup and "your best week" are her goals |
| **Veterans** | District Line Race, Host stars, trunk name plates, the Landmark rising over the Quad | Recognition, not power over others; Lodgers never on a board |
| **Somebody quits in week 4** (Cleo, a trunk owner) | Her links go to a successor Lodger on day 30; her trunk segment's upkeep goes to the hub once all its owners are dormant | Nothing stalls; the Landmark keeps her colour in its banner |

---

## Part 6 · Scores against the brief's checklist

| # | Point | Score | Why |
| --- | --- | --- | --- |
| 1 | Friends together | 3 | A crew claims a Quad in minutes; a late friend's seat is reserved, warm and furnished; a held seat for a friend who returns |
| 2 | Choosing where you live | 3 | Seat cards with lit seats, usual hours, Levels, Hosts, what the land needs; "Join a table" as a sensible default |
| 3 | The lonely player | 3 | Placed with humans by default, Lodgers that grow, quiet-district grace, presence everywhere, crew rewards per active member, paused leagues on tiny servers |
| 4 | Players who stop | 3 | Every link handed over, never ended; top-ups; held seats; balloons not walls; the crew moves with its members |
| 5 | The early game | 3 | Solo tutorial first; the Yard socket at minute 12; furnished seats; the game-paid welcome bonus; nobody can fleece a newcomer |
| 6 | The late game | 2 | Works, trunks, the Line Race and the Great Pipe are borrowed and guarded, but this design adds little new depth for veterans beyond hosting |
| 7 | Asynchronous idle play | 3 | No 2-online requirement anywhere; tanks and standing orders; banked favours; Wishes that can't fail; 12 h / 72 h / 1-push rules |
| 8 | Fair and safe | 2 | Bands, sockets, probation and game-paid subsidies close the known abuses; alt detection by device is a heuristic, and probation can still be gamed |
| 9 | Clear | 2 | One status line per link, lit seats, the chronicle. But the vocabulary is heavy (crew, Quad, Yard, socket, Lodger, caretaker, links, trunk); needs strict staging |
| 10 | Pipes are the heart | 2 | Yard pipes, links and trunks are pipes, and handovers happen on visible pipes. But the safety net (top-ups, Yard market) is invisible plumbing |
| 11 | Scale and performance | 3 | Yards of 4–6 sockets, non-transitive links, frozen districts without lit seats, Lodgers as cheap tenants |
| 12 | Seasons, persistent world | 2 | Quiet Isles and crews persist; migration turns corner groups into Quads, but taking the inner-corner parcel changes some testers' layouts |
| 13 | Social glue | 3 | Favours and postcards, balloons with counts, Host stars, plaques, Landmarks, a reason to invite (a held, furnished seat) |
| 14 | Buildable | 2 | Each step is 1–2 days on existing code (tenants, caretaker, Isles, relocation, blueprints), but the whole is about 15 days |

**Total 36 / 42.**

## Part 7 · Build order (each step playable on its own)

| Step | What | On top of | Days |
| --- | --- | --- | --- |
| 1 | **Stop the dead pipe:** `Sleep()` hands contracts and links to a successor tenant with a 72 h notice; the link status line; top-up at the link price | Tenants, contracts, Quiet Isles | 1 |
| 2 | **Choose where to land:** `WakeUp()` shows the three screens and up to 4 cards; held seat; Era kit; postcards from favours | Relocation, offline report | 1–2 |
| 3 | **Gravity:** lit-seat counts, the district check by lit seats (fix the "< 4 players" skip), placement score, the lone-human card, frozen districts | Placement, `CheckQuietDistricts` | 1–2 |
| 4 | **Quiet-district grace** for the hub allowance and tier-3 imports (only once 4 M7/M8 ship) | Hub market | 0.5 |
| 5 | **Quads, Yards with sockets, crews, crew price, Yard market** (with 2-friends and 4-economy) | Districts, networks | 3–4 |
| 6 | **Lodgers:** archetypes, growth with their Quad, furnished seats, the letter digest, the 25% cap | Tenants | 2 |
| 7 | **Async help:** Wishes with a fallback fill, banked favours, gift crates within the band, the push budget | Inbox, orders | 1–2 |
| 8 | **Newcomers:** Hosts, the game-paid welcome bonus, fair-price band, Tailwind, leagues that pause on tiny servers, the Quad Cup | Leaderboards | 2 |
| 9 | **Presence:** lit windows, sparkles, usual hours, the chronicle, balloons | Build events | 1–2 |

Steps 1–3 fix the three things that make people quit *today* and need no new world model. Step 5 is the big one,
shared with the friends and economy designs.
