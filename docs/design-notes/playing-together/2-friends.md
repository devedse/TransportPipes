# 2 · Friends first: Ana, Ben and Cleo start tonight, Dev joins next week

The test case for every idea below: three friends hear about TransportPipes in a group chat, open it on the same
evening and want to play *together* from the first minute. Dev, the fourth, is busy and joins eight days later. Six
weeks from now Ben has stopped playing. The design has to be fun for all four at every one of those moments.

The core move: **a station is a Quad.** Four estates around one shared square, the **Yard**. A Quad is the unit
you choose, the unit you see on the map, and the unit that plays together. Friends claim a whole Quad; strangers who
land in one become a crew by living there. Pipes into the Yard are how a crew works together. Links between Quads
(gate to gate, non-transitive, option A of `docs/TradingRethink.md`) are how crews trade with each other.

```
   A district today (5×5 blocks)            One corner Quad, zoomed in
  ┌────┬────┬────┬────┬────┐
  │ Q1 │ Q1 │ D1 │ Q2 │ Q2 │               ┌──────────────┬──────────────┐
  ├────┼────┼────┼────┼────┤               │ Ana          │          Ben │
  │ Q1 │ Q1 │ D1 │ Q2 │ Q2 │               │ Windy Heights│  Coal Valley │
  ├────┼────┼────┼────┼────┤               │         ┌────┼────┐         │
  │ D2 │ D2 │ H  │ D3 │ D3 │               │         │  YARD   │         │
  ├────┼────┼────┼────┼────┤               ├─────────┼─ Depot ─┼─────────┤
  │ Q3 │ Q3 │ D4 │ Q4 │ Q4 │               │         │         │         │
  ├────┼────┼────┼────┼────┤               │         └────┼────┘         │
  │ Q3 │ Q3 │ D4 │ Q4 │ Q4 │               │ Cleo         │  (reserved)  │
  └────┴────┴────┴────┴────┘               │ Riverside    │ Copper Gulch │
  Q = Quad (4 seats)                       └──────────────┴──────────────┘
  D = Duo (2 seats), H = hub               Yard = the 4 inner-corner parcels
                                           + the corridor crossing (18×18)
```

This fits the current geometry: the four corner 2×2 groups become Quads (16 seats), the eight estates on the arms
of the cross become four Duos (pairs: couples, two friends). The Yard is carved from each estate's **inner-corner
parcel** (the parcel bought last today, so almost nobody has built there) plus the corridor crossing: an 18×18 plaza.
Each estate keeps 15 parcels and gets a fifth gate, the **Yard gate**, at its inner corner.

---

## Part 1 · Ideas and mechanics

### 1. "Play with friends" creates a crew before anyone has an estate

- The start screen has two buttons: **Start alone** and **Start with friends**. The second asks for a crew name
  ("Kettle Works"), an emblem (40 simple icons: kettle, fox, gear, wave…) and two colours. 15 seconds.
- Out comes a link: `transportpipes.gg/c/KETTLE-7Q`, with a share button that opens the phone's share sheet. The
  preview card in WhatsApp/Discord shows the crew banner and "Ana invites you to Kettle Works · 3 seats free".
- Opening the link skips the generic intro and lands in the **crew lobby**, logged in as a guest if needed (an account
  is only asked for when they settle). No level requirement: a crew is not a corporation.

### 2. Choosing the place together: the Seat Map

The lobby is the district map zoomed out to Quads, not a list. Every Quad shows as a card on the map:

| On the card | Example |
| --- | --- |
| Free seats and their specialties | "3 free: Windy Heights · Coal Valley · Riverside · (Copper Gulch taken)" |
| The residents | "Mo, Lv 14, active today, plays evenings" or "Lodger: Bertha's Brickworks" |
| What the land holds | ★ for wind/sun, deposits with richness, a river or not |
| Neighbours | The Quads and Duos next to it, their crew names and activity (green/amber/grey dot) |
| The hub | Its specialty demand this week, hub level, how busy the district is |

- Each friend drops their **banner pin** on a Quad they like (works asynchronously: pins stay). The lobby shows
  "Ana and Cleo picked Ashford NE; Ben picked Ashford SW". Ana (the founder) presses **Settle here**, or the crew is
  settled automatically on the most-pinned Quad when the third friend arrives.
- **Inside the Quad, each friend drags their banner onto a seat.** Seats are generated complementary: every Quad has
  one energy seat (Windy Heights or Sunny Flats), one ore seat (Copper Gulch or Coal Valley), one water/sand seat
  (Riverside) and one material seat (Old Forest or Stone Quarry). So choosing a seat is choosing a role, and it is
  visible: "Ben takes coal".
- **Default for people who don't want to choose:** the button **"Pick for us"** chooses a fresh Quad in the newest
  district with the best mix for the number of friends, next to at least one active crew.
- **Reserved seats.** Unclaimed seats of a friends' Quad stay reserved for the crew for 7 days (renewable once by the
  founder), shown on the map as a seat with the crew's banner and "reserved for a friend". Meanwhile a Lodger
  (idea 9) keeps the seat warm.

### 3. Landing together

**Settle** plays one shared moment: all three estates arrive on balloons at the same time, the crew banner unfurls in
the Yard and Loady says "Welcome to Ashford NE, Kettle Works!". If Ben settles two hours later, Ana sees his
balloons land next door the next time she opens the game (a short replay: "Ben moved in at 21:40").

### 4. Seeing each other: colours, lights and a crew ticker

- **Colours everywhere.** Each member picks a personal colour inside the crew palette. It appears on their gate flags,
  as a thin stripe on their pipes, on the pellets their goods make in shared pipes, and as their HQ roof.
- **Presence that fits an idle game:** a friend online now has lit windows and a smoking chimney; played today has
  daylight; resting has a sleeping cat on the HQ steps (not a shaming grey). Hover shows "Ben · Lv 6 · played 2 h ago".
- **Live cursors:** when two friends are online at once and look at the Quad, each sees the other's cursor as a
  small coloured hand with their name. Building near each other feels like being in the same room.
- **Crew ticker** in a corner of the screen and as a board in the Yard: "Cleo upgraded Glassworks to Lv 4 · 12 min
  ago", "Ben's coal reached your Smelter for the first time". Tapping an entry flies the camera there.

### 5. The Crew Line: the first shared pipe in the first 15 minutes

- The Yard starts with one free building: the **Crew Depot** (3×3, a shared storage with a counter). Loady's fifth
  step, right after the first Trading Post: "Lay a pipe from your Yard gate to the Depot (6 tiles)".
- When a second member's pipe reaches the Depot: the **Handshake**. Both pipes flash in their colours, a little
  firework, and both get the First Link gift (a free Storage and 200 g). The ticker says "Ana and Ben are connected".
- From then on, every member's buildings on a Yard-connected network can trade with every other member's (see 6).
  This is the only place where a network spans several estates, and it is bounded by the crew (2–4 estates, up to
  8 in a Street), so it stays cheap to solve and never grows into a district-wide blob.

### 6. Crew trade: a fixed friend price, never a gold pipe

- Goods between crew members flow automatically through the Yard at the **crew price: 1.75× the hub buy price**.
  The seller earns 75% more than at the hub, the buyer pays 30% less than importing. Both win, no haggling, no
  settings needed.
- The price is fixed by the game, so trade cannot be used to move gold (no "sell 1 ore for 10,000 g").
- Flow order for crew goods: your own consumers → crew members (in the Yard network) → links to other crews → your
  storage → hub. A per-resource switch "keep for myself" exists, off by default.
- **Distance is short on purpose:** the Yard sits at everyone's corner, so a crew link is 6–20 tiles of pipe, a loss
  of 2–6% on grey pipes. Your friends are always your cheapest partners.

### 7. Crew Orders and the Yard projects: something to build together

- The Depot shows **3 Crew Orders a day**, sized to the crew: e.g. day 1 "Water Tower foundation: 200 stone, 100
  planks, 300 energy". They need several chains, so each friend naturally fills their specialty, but **one player
  alone can always finish one** (an order is at most ~45 minutes of one member's production).
- Finished orders fill the **Crew bar** (Crew Level 1–10). Each level grows the Yard: a Water Tower (L2), Yard
  Storage (L3), a Crew Workshop that gives +5% production to all members (L4), a Street expansion (L5), the shared
  outpost licence (L6), and the **Crew Landmark** (L8–10), a big monument that rises over the Quad and is visible
  from the district map.
- Contributions show as **coloured segments of a ring** around the Yard project, one colour per friend. No ranking,
  no "who did least": just a ring that gets more colourful.
- **Diversity bonus instead of a requirement:** the weekly crew reward is ×1.0 when one member contributed, ×1.2 for
  two, ×1.35 for three, ×1.5 for four (Lodgers count as half a member). Playing together pays; playing alone never
  blocks.

### 8. Wishes: asynchronous help that works while the other is asleep

- Any member can pin a **Wish** to their Yard gate: "200 copper ingots by tonight" or "coal for my new Smelter". It
  floats as a speech balloon over their gate on the map.
- A friend who logs in later sees it, and with one tap sets their surplus to flow to the wisher first (a
  **standing order**: "my spare coal goes to Cleo first, for 24 h"). It is delivered by pipe at the crew price, even
  while both are offline.
- **Morning report, crew edition:** "While you slept: Ben's coal kept your Smelter running (1,240 coal, you paid
  2,170 g). You sold Cleo 3,100 ⚡. Cleo's wish 'glass for the Workshop' was filled by Ana."
- **Gentle alerts** in the crew ticker, phrased as help, never blame: "Cleo's Smelter has been out of coal since 3:10.
  Ben has 2,000 coal in storage." (Only offered when someone in the crew can actually help.)

### 9. Lodgers: NPC neighbours with names that keep empty seats alive

- An empty or reserved seat is run by a **Lodger**: an NPC company with a portrait, a name and a small personality
  ("Bertha's Brickworks · grumpy, loves stone"). It has modest works matching its seat, trades in the Yard at the
  crew price, posts simple Wishes and contributes small amounts to Crew Orders.
- Lodgers are deliberately capped (works at about Level 5, ~30% of an active player's trade by week 2), so a real
  friend is always better, but an empty seat is never a hole.
- **The solo player gets a Quad with three Lodgers.** They behave like quiet friends: they buy your surplus, send
  thank-you notes, ask for things, celebrate your Crew Orders. The solo player has a full crew game from minute one;
  real players can later take Lodger seats (idea 13).

### 10. The late joiner: the seat comes furnished, and he is the missing piece

Dev opens the link on day 8. Ana, Ben and Cleo are Level 12–15; he is Level 1. Four things make him an asset, not a
burden:

1. **His seat is the one the crew lacked.** The crew has been importing copper from the hub at 2.5× (or from Bertha
   the Lodger). Dev's seat is Copper Gulch. From his first mine he sells to three eager buyers at 1.75× instead of to
   the hub at 1×: day-1 Dev earns like a day-3 solo player, and his friends' costs drop. The ticker says so: "Dev's
   copper saves the crew 840 g/h".
2. **The Lodger hands over its works.** The Lodger's buildings (a Copper Mine, a Smelter, a wind generator, Level 5)
   become Dev's, intact, not ruins. Bertha leaves with a goodbye note and a cake in the Yard.
3. **Tailwind:** +100% XP while his level is more than 3 below the crew median, parcels at half price up to the
   crew's median parcel count. It ends by itself; he catches up in about 4–5 days, not 8.
4. **Housewarming:** each member may send Dev one gift from their storage in his first 48 h, capped at 500 g of goods
   per Dev level (so a Level 3 Dev can receive up to 1,500 g each). Enough to feel welcome, too little to be a funnel.

His contributions to Crew Orders count ×2 while Tailwind is on, so his small amounts move the ring visibly.

### 11. When a friend stops: the estate keeps running, then a Lodger takes the seat

The idle game helps here: an absent friend's buildings keep producing and their pipes keep delivering. What changes:

| Away | What the crew sees | What happens |
| --- | --- | --- |
| 0–3 days | Nothing | Normal |
| 3–14 days | Sleeping cat on the HQ steps | Everything runs; standing orders continue; their Crew Order share is simply zero |
| 14–30 days | Ivy on the HQ, "Ben is away" | The crew may name a **Housesitter**: one member who can change Ben's trade settings and storage shares (never build, demolish or move) |
| 30+ days | Ben's banner moves to the **Crew Wall** in the Yard | Ben's estate packs up on balloons to the Quiet Isles; a Lodger moves into his seat **that copies his last 7 days of trade** (same coal, same volumes, capped), so no friend's Smelter goes cold overnight |

- The crew chooses whether Ben's seat stays **held for Ben** (the Lodger stays until he returns; default for 60 days)
  or is **opened for a recruit** (idea 13).
- **Ben returns:** the offline report, the Welcome-back package, and if his seat is held, his estate lands back in
  it with its full layout. If the seat was given away, he lands in the nearest free seat of the Street or a
  neighbouring Quad, and the Yard link follows him (a Street link, idea 14).

### 12. The Crew Lens: who sends what, on the map

- A map overlay (key **C**) that hides everything except crew flows: pellets in the senders' colours moving through
  the Yard, thicker lines for bigger flows, each estate labelled with its main export icon.
- Tapping a pipe in the Yard shows "This week: Ben → Ana 12,400 coal (21,700 g) · Ana → Cleo 31,000 ⚡ (54,250 g)".
- The weekly **Crew Story**: a shareable postcard (an isometric snapshot of the Quad with the banner, the Landmark's
  progress and three highlights: "Most coal ever: 84k", "Dev's first Steel Mill", "Ashford's #2 crew").

### 13. Open seats and recruiting: how a crew grows past its friends

- A seat a crew doesn't hold for a friend can be **opened**: it then appears on the Seat Map for other players with
  the crew card ("Kettle Works, Crew Level 5, 3 active, plays evenings CET, looking for: Copper Gulch").
- A solo player browsing can **apply** or, if the crew allows, **move in directly** (free move for new or Quiet Isles
  players). The crew sees a welcome card with the newcomer's level and activity.
- **Invite rewards that celebrate the crew, not spam:** when an invited friend reaches Level 5 and 10, both get a
  cosmetic (a pipe pattern, a Yard lamp) and the Yard gets a new decoration with the newcomer's name.

### 14. Streets: when four is not enough

- From Crew Level 5 a crew can join with an adjacent Quad or Duo into a **Street** (both crews agree): their Yards are
  linked by a **Street pipe** across the corridor, and trade between all members runs at the crew price.
- A Street is at most 8 seats (two Quads) or 6 (a Quad and a Duo), so the shared network stays bounded.
- For bigger groups the corporation stays the next layer (a corporation of several crews); crews are the everyday,
  corporations the big projects.

### 15. The long line: a shared outpost and a trunk through the wilderness

The late-game project of a crew, week 3 onwards (Crew Level 6):

- The crew claims **one shared outpost** in the wilderness, e.g. an oil ★5 field 50 tiles away. It belongs to the
  crew; its output enters the Yard network.
- The pipe to it is built **segment by segment**: each 10-tile segment costs a member steel and gold, and keeps that
  member's colour stripe. The line becomes a striped ribbon across the map, visible to the whole district.
- Every 25 tiles it needs a **Pump House**, which a member "adopts" (keeps it upgraded; an adopted pump shows their
  banner). An unadopted pump still works at base level, so nobody's absence breaks the line.
- **The District Line Race:** each season the hub announces a target (the district Wonder site, a Discovery field).
  Crews race to connect their Yard to it; the first three get a permanent marker on the map and a trophy in the Yard.

---

## Part 2 · The friends journey

### Evening 1, 20:00–20:20 · "Let's do this"

| Minute | Ana | Ben | Cleo |
| --- | --- | --- | --- |
| 0 | **Start with friends** → "Kettle Works", kettle emblem, teal & copper → shares the link in the group chat | | |
| 2 | Lobby: drops her pin on Ashford NE (fresh Quad, newest district, next to an active crew) | Opens the link on his phone, drops his pin on the same Quad | |
| 4 | Drags her banner on Windy Heights | Takes Coal Valley ("obviously") | Opens on her laptop, takes Riverside. Copper Gulch shows "reserved for a friend · Lodger: Bertha" |
| 5 | **Settle here.** Three balloons land at once; the banner unfurls in the Yard; three names over three HQs | | |
| 6–12 | Personal tutorial for each: wind, iron mine, Trading Post, first gold. Each sees the others' cursors hopping around next door | | |
| 12 | Loady: "Lay a pipe from your Yard gate to the Crew Depot". Ana is first | Ben second: **Handshake** fireworks; Ben's coal pellets start rolling towards Ana's future smelter | Cleo third: the Yard lights up in three colours |
| 15 | First Crew Order on the Depot: "Water Tower foundation: 200 stone, 100 planks, 300 ⚡" | Bertha the Lodger chips in 40 stone ("for the young ones") | |
| 19 | The order is done (Ana's energy, Ben's stone from the quarry, Cleo's planks from the beach-side forest) → Crew Level 2, the Water Tower rises in the Yard, confetti in crew chat | | |

At minute 20 every friend has their own working estate (nothing depended on the others), one shared building, a
visible shared pipe and a reason to come back tomorrow.

### Day 1, sessions 2–4

- Morning: each one's offline report has a crew section ("Ben's coal kept your Smelter running all night").
- Cleo pins a Wish: "coal for my Glassworks". Ben sees it on the bus and taps **send spare coal to Cleo first**.
- The first complementary chain appears without anyone planning it: Ben's coal + Ana's energy → Cleo's glass, sold
  to the hub, which pays +20% for glass this week. The Crew Lens shows it as three coloured streams meeting.
- Crew Level 3: Yard Storage. They argue happily about which parcels to buy next.

### Week 1

- Daily rhythm: everyone plays 3–4 short sessions, at different times. Crew Orders are filled by whoever is online;
  the ring shows all three colours most days (diversity bonus ×1.35).
- First link to the next Quad (another crew, "Fox & Sons") through a corner gate: Kettle Works buys their stone at
  their price. Crews trade with crews; friends trade inside.
- Crew Level 4, the Workshop: +5% production for everyone. First weekly Crew Story postcard; Ana posts it in the group
  chat and tags Dev: "your seat is waiting, Bertha is getting old".
- The founder renews Dev's reservation on day 7 with one tap.

### Day 8 · Dev arrives

- Dev opens the old link. The lobby shows his seat, the crew card (Level 4, three members at Level 12–15) and what
  the crew needs: "Copper: the crew imports 1,100/day from the hub".
- **Settle:** Bertha leaves with a cake on the Yard bench and a note; her Copper Mine, Smelter and wind generator
  (Level 5) become Dev's. Dev's balloons land. The three others see a ticker entry and a Housewarming button.
- In his first hour Dev's copper replaces the hub imports: he earns 1.75×, the crew saves money, the ticker says so.
  Tailwind is on: on day 12 he is Level 11, two levels below the median, and helping with the Workshop orders.

### Weeks 2–3

- **Street:** at Crew Level 5 Kettle Works and Fox & Sons join into a Street: seven players, one shared crew price,
  Street pipe across the corridor.
- **The long line:** Crew Level 6 gives the outpost licence. The crew claims an oil ★5 field 48 tiles away and builds
  the trunk segment by segment over five days; it becomes a teal-copper-amber ribbon across the wilderness. Ben adopts
  the first Pump House, Dev the second.
- **District Line Race:** the hub's target is the Observatory Wonder site. Kettle Works finishes second; a bronze
  marker on the map and a trophy in the Yard.

### Week 4 and the season finale

- Ben's job gets busy; he hasn't played for 9 days. The sleeping cat appears on his steps. His coal keeps flowing;
  the crew notices nothing in their Smelters. On day 30 a Lodger copying Ben's coal trade takes his seat, which the
  crew holds for him; his banner hangs on the Crew Wall.
- Ana, Cleo and Dev push the **Crew Landmark** (a giant copper kettle with a steam plume, Level 8–10). It is visible
  from the district map and gets the crew into the Ashford top three.
- Cleo invites her sister to the open fifth seat of the Street; the invite reward is a Yard lamp with her name.
- **Season end:** the crew persists. The Landmark becomes an **heirloom** (a small version stands in next season's
  Yard), and the crew re-settles together in the next season through the same lobby, Ben's seat still reserved.
  When Ben comes back in season 2, his estate lands next to his friends.

---

## Part 3 · How it scores on the checklist

| # | Check | How |
| --- | --- | --- |
| 1 | Friends together | A crew claims a Quad; reserved seats for 7+7 days; Lodger keeps the seat warm |
| 2 | Choosing where | Seat Map with residents, activity, land, hub; "Pick for us" default |
| 3 | Lonely player | A Quad of three named Lodgers; open seats to join real crews later |
| 4 | Players who stop | Estate keeps running; Housesitter; a Lodger copies the last 7 days of trade; seat held for the return |
| 5 | Early game | Personal tutorial first; the Crew Line is minute 12; nothing needs another player |
| 6 | Late game | Crew Levels, Streets, shared outpost and the long line, Landmark, Line Race |
| 7 | Async | Wishes, standing orders, crew morning report; pipes do everything offline |
| 8 | Fair | Fixed crew price; capped housewarming; Lodgers capped; Yard changes logged and undoable |
| 9 | Clear | Colours on pipes and pellets, Crew Lens, ticker, Wish balloons, rings |
| 10 | Pipes | The Crew Line, Yard network, Street pipe, the striped trunk with adopted pumps |
| 11 | Scale | Shared networks are bounded to 4–8 estates; everything else is option-A links |
| 12 | Seasons | Crews persist; heirlooms; corner parcels become Yards in a migration |
| 13 | Social glue | Names, emblems, colours, Landmarks, Crew Story postcards, invite rewards |
| 14 | Buildable | See the steps below |

### Shipping in steps (each step playable on its own)

1. **Crews and seats (2 days):** "Start with friends", the invite link, the lobby with the Seat Map, reserved seats,
   crew colours on gates and pipes, presence on HQs. Placement uses the existing district geometry (corner Quads,
   arm Duos).
2. **The Yard (2 days):** inner-corner parcels become the Yard; the Yard gate; the Crew Depot; the Yard network
   joins members' networks (bounded); the crew price; Crew Orders and Crew Levels 1–4.
3. **Lodgers and leaving (1–2 days):** tenants with names in empty seats, the handover of works, Tailwind,
   Housewarming, the copy-trade Lodger for asleep members, held seats.
4. **Async and clarity (1 day):** Wishes, standing orders, the crew morning report, the ticker, the Crew Lens.
5. **Late game (2–3 days):** Streets, the shared outpost and segmented trunk with adopted pumps, the Line Race, the
   Landmark, Crew Story postcards, heirlooms.

**Migration for the testers:** every current corner group of four estates becomes a Quad; residents of a Quad who
are not in a crew get a nameless "Quad crew" they can name (or leave by a free move). Buildings on the four
inner-corner parcels stay where they are and count as Yard buildings of their owner until moved. Corridor pipes are
migrated as in `TradingRethink.md` option A.

---

## Part 4 · Risks: what could make this unfun

1. **Obligation instead of fun.** Crew Orders, Wishes and diversity bonuses can turn into "I have to log in or I let
   them down". Mitigation: no penalties ever, no per-member ranking, Wishes expire quietly, alerts only when someone
   *can* help, and nothing a crew needs requires a specific member.
2. **Sleeping cats as shame.** Visible inactivity can feel like a public scolding of the friend who plays less.
   Keep the signs cute and vague ("resting"), never show "last online 9 d" to strangers.
3. **The Yard eats the world.** If the crew price and Yard perks are too good, links to other crews, the hub's
   contracts and the Exchange stop mattering. Keep the crew price at 1.75× (other crews may offer better), cap Yard
   pipe throughput by tier, keep perks small (+5%).
4. **Strangers in a Quad.** Solo players put together may not get along, or one may re-route Yard pipes badly.
   Mitigation: Yard pipes and buildings belong to the crew, every change is logged with a name and can be undone
   for 24 h, nobody can demolish a Yard building alone, and a free move out is always available.
5. **Five friends.** A Quad has four seats. If the fifth can't get into the adjacent Quad (taken), the group feels
   split. Mitigation: Duos next to every Quad, Streets, and a "move the whole crew" option to a fresh Quad.
6. **The late joiner still feels behind.** Seeing friends at Level 15 with Steel Mills while he has a Lodger's Smelter
   can still sting. Tailwind and "you are the missing piece" help; also show him a short catch-up goal ("Level 11:
   you can build what Ben builds") instead of the gap.
7. **Lodgers too good or too dull.** Too good and players prefer bots to strangers; too dull and the solo game feels
   empty. Needs tuning and personality (notes, moods, small requests), and the cap at ~30% of a player.
8. **Alt accounts.** One person with three alts can farm the diversity bonus and housewarming. Mitigation: bonuses are
   small and cosmetic-heavy, housewarming is capped by level, the crew price is fixed, and seats joined from the same
   device in the same hour don't count toward the diversity bonus.
9. **Complexity.** Crews, Quads, Streets, corporations, links, Yards: too many layers for an idle game. Keep the
   vocabulary small (crew, seat, Yard), introduce Streets and outposts only when they unlock, and consider folding
   corporations into "crews of crews" later.
10. **Geometry pain.** Taking the inner-corner parcel from every estate changes layouts and may break art and the
    current parcel pricing. It is the cheapest Quad that fits today's world; if it doesn't work, a cleaner district v2
    (a 6×6 estate grid around a 2×2 hub = 8 Quads, 32 seats) is the alternative for new districts.
