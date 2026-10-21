# Concept C · Places, Crews, Links: the definitive spec

Champion: the friends-first designer. Built on my r2 file, with the economy layer from r2-4 (prices as multiples of
the hub's base price, numbers as shares of T(L), the typical income at Company Level L, from about 800 g/min on day 1
to 4,900 g/min on day 28). Every one of the 12 questions is decided below, each with a reason and the evidence that
would change it.

## 1 · The concept in five sentences

1. **A place** is a *station* of 4 seats or a *halt* of 2 seats around a small plaza. The geometry is today's 5×5
   district with nothing moved, and you choose your seat with full information.
2. **A crew** is the people who chose each other: 1 to 8 friends from Level 1, with one banner. A crew can live in a
   station and the halts next to it, and it holds seats for its friends.
3. **A link** is the only way goods cross from one company to another. It joins exactly two named companies who both
   want it: crew mates are linked automatically, anyone else by mutual consent. It is never transitive.
4. **The plaza shares a project, not a market.** Everyone in a place can build the station Works together and see
   each other's goods flow, but nobody's goods go to a company they didn't choose.
5. **Named NPC Houses fill every gap** (empty seats, seats held for friends, the trades of people who leave). They are
   honestly labelled and priced below humans, so friends are always better and nobody is ever stuck.

## 2 · The rules, with the numbers

### 2.1 Places

| | Station | Halt |
| --- | --- | --- |
| Where (5×5 district) | the 4 corner 2×2 groups | the 4 arm pairs next to the hub |
| Seats | 4 | 2 |
| Plaza | 18×18 tiles: the 4 inner-corner parcels plus the corridor crossing | 8×18 tiles: 1 corner parcel each plus the crossing |
| Works | yes | no; a halt delivers to the Works of the station it is attached to |
| Attached to | its halt in a pinwheel: N1↔h1, N2↔h3, N3↔h2, N4↔h4, each 34 tiles of crew pipe | one station |

- **Plazas are carved only from corner parcels.** In `progression.yaml` the corners are the last four parcels in
  the order and unlock at Company Level 35, 40, 45 and 50. The typical player reaches Level 30 on day 25, so in
  practice no tester owns one. Each estate keeps 15 parcels and gets a fifth gate, the **junction gate**, on its
  plaza.
- **Seats are generated to complement each other.** Each station has one energy seat (Windy Heights or Sunny Flats),
  one ore seat (Copper Gulch or Coal Valley), one water/sand seat (Riverside) and one materials seat (Old Forest or
  Stone Quarry). A halt has two complementary seats. Every seat has two home goods (★5 ×2.0, ★4 ×1.5), iron ★3, and
  ★2 (×0.75) patches of every other Era 1 basic, so every chain can be built anywhere.
- **Land is never scarce.** A new district opens when fewer than two fresh stations are free in the open districts.

### 2.2 Crews

- **Founding takes 15 seconds:** a name, one of 40 preset emblems and two colours. It is free from Level 1 and has no
  crew levels and no shared wallet. A crew has 1–8 members:

  | Members | Where they live |
  | --- | --- |
  | 2 | a halt |
  | 3–4 | a station |
  | 5–6 | a station + its attached halt |
  | 7–8 | a station + both adjacent halts (crew pipes of 34 and 68 tiles) |

- **Seat codes.** Every held seat has its own code, and the crew link needs a founder's tap when it is used after 24
  hours. A publicly posted link can't hand a stranger a friend's seat.
- **Seats held for friends who haven't arrived** stay held while any member has played in the last 3 days, for at most
  28 days. Nobody has to tap "renew".
- **Crew links:** every pair of members is linked at once, at the fixed crew price (§2.4). Members share colours on
  roofs, gate flags, pipe stripes and pellets, the crew ticker, Wishes, tending and the weekly postcard.
- **Leaving** takes one tap and works at once. **Removal** needs a majority of the active members (at least 2 votes)
  and happens at most once a week. Either way the person keeps their seat and estate and becomes a neighbour. Nobody
  can ever be evicted from a seat.

### 2.3 Links

- **No network crosses a gate.** Each estate's networks are its own. A link is a pipe between two gates (the junction
  counts as a gate) of two companies.
- **Inside a place:** each estate has one game-drawn **junction pipe** into the plaza's manifold, upgraded by its owner
  (grey 100/min, copper 500, steel 5,000). All of that estate's pair links share its capacity. The loss is the plaza
  distance only, about 10 tiles, so 3% on grey.
- **Between places:** links run gate to gate, auto-routed through the corridors and drawn as bundles. Nobody owns a
  corridor tile. The loss depends on length: grey 0.3%/tile, copper 0.15%, steel 0.05%.
- **Consent:** crew pairs are linked automatically. Anyone else gets a link card, which the other side accepts with
  one tap (offers wait 72 h; Houses accept at once).
- **Each pair has its own settings and manifest:** goods, price preset, priority among the seller's partners ("Ana
  first"), an optional cap, and this week's flow, loss and value.
- **Every link has a tank** at the buyer's gate holding 2 hours of its flow, a **1-hour notice** for pauses and price
  changes, and an automatic **fallback** (Houses, then hub imports for that input, up to the lost amount).
- **Every link shows its state in one sentence:** Live, Partner resting, Caretaker, Handed over or Paused.
- **Flow order for a producer:** own consumers → contracts → links by priority, then by price → own storage → hub.

### 2.4 Prices (× the hub's base price of the good)

| Price | Value | Who |
| --- | --- | --- |
| Hub buys | 1.0, up to your **Hub Order**, then 0.5 | always |
| Houses buy / sell | 1.25 / 2.0 | NPC companies |
| **Crew price** | **1.75, fixed, both ways** | crew pairs |
| Link presets | Friend 1.375 · Fair 1.75 (default) · Premium 2.125 | everyone else; no free slider |
| Hub sells | 2.5 | imports |
| The Port | 1.1 for any tier 2+ good, no limit, only reached by pipe | the late valve |

- **Hub Order:** the hub's standing order for each good *from you* pays full price up to 0.6 × T(L) per minute of hub
  value, shown as a bar you fill (Level 22: about 1,800 g/min of ingots). Above it the hub pays half.
- **Your sales move only your own price**, so nobody can crash a newcomer's market.
- **Quiet-district grace:** the Hub Order is ×2 with 6–11 lit seats in the district and switched off with 5 or fewer.
  (A seat is lit when its owner played in the last 3 days.) A push towards other players must vanish when there are
  none.
- **Fair-price band for newcomers:** across a gap of 10+ Levels the lower company sells at ≥ hub price and buys at ≤
  hub price + 5%. No shortfall clauses below Level 6.

### 2.5 Houses (NPC companies)

- Every House has a name, a portrait and a **cog badge**, and its card says "House (NPC)". Houses never appear on
  boards, never take ranks or rewards, and never deliver more than 25% of any shared goal.
- **Volume:** all Houses together buy at most 25% of a company's output of a good, and sell it at most 25% of its
  need. They trade tiers 1–3 (tier 3 from Era 2, at 2.0), never tiers 4–5. Their works level is the median level of
  the humans in their place minus 3 (at least 5).
- **Seat warming:** a House holds every empty or held seat. When a human arrives it leaves a **starter kit** (3–5
  buildings at level ≤ 5) and hands over its links, then moves out with a note.
- **Successor:** when a seat whose owner is asleep isn't held, a House takes it after a 7-day notice. It trades at
  House prices, inherits 80% of the leaver's 14-day median volume, and decays by 10% a week to the House baseline.
  It counts inside the same 25% cap.
- **Faucet bound:** at most 0.25 × 25% ≈ 6% of a company's output value is new gold from Houses, however many Houses
  or "quitters" surround it.

### 2.6 Absence

| Days away | The estate | The crew sees | Partners read |
| --- | --- | --- | --- |
| 0–3 | runs | nothing | Live |
| 3–14 · resting | runs at 100% (the world keeps running) | a sleeping cat on the HQ steps | "Ben is resting; his coal keeps coming, it doesn't grow" |
| 14–30 · dormant | caretaker (exists, 10% fee) keeps link prices and renews contracts; autopilot (on by default for crew members, the owner can switch it off) puts up to 30% of income into upgrades that feed links | ivy; a card: **Hold Ben's seat** (preselected) | "Ben's caretaker keeps this link" |
| 30+ · asleep, seat held | **stays in place**, caretaker-run, while the hold lasts | a balloon tied to the plaza fence with his postcard count | Caretaker |
| 30+ · asleep, not held | packed to the Quiet Isles (exists); a successor House after the 7-day notice | the balloon | Handed over |

- **Who may hold a seat:** any active crew member, with one tap. A hold lasts 60 days from the day the member went to
  sleep, and any active member can renew it once (120 days in all). It survives into the next chapter's lobby. If no
  crew member has been active for 14 days, the holds lapse.

### 2.7 Arrivals and returns

- **Tailwind** is one rule for late friends, newcomers and returners: XP × (1 + days since the chapter started / 14),
  at most ×2.5, until the player is within 5 levels of the active median of their crew (else their place, else the
  district).
- **Housewarming:** each crew mate may send one crate worth at most 1 hour of the giver's own production of one good,
  only to a player below Level 10, in their first 48 hours.
- **Hosts:** veterans of Level 15+ can light a Host lantern. A newcomer next to a Host gets +15% on sales to the Host
  for 7 days, paid by the game (capped at 2 h of the newcomer's T(L)). Host stars only count once the newcomer reaches
  Level 10 on 4 different days.
- **Era jump kit** for every Era missed: the chain's blueprint, a crate of its first building, its first 3 techs at
  the diffusion discount −25%. It never expires.

### 2.8 Projects and the late game

- **The station Works** grows in stages over the chapter: Foundation (Level 3, one player alone needs about 15
  minutes) → Water Tower → Workshop → Foundry Hall → the **finale**.
  - **Baskets** are sized at 6 h of T(L) × the active residents (Houses count 0.5).
  - **Credit** is your share of your *own* output, capped per day and bankable for 3 days. **Keystone:** the delivery
    that completes the missing good gets +20%. Hub imports count 25%.
  - **The perk goes to everyone in the place.** Contributions show as a ring of colours, never a ranking or a list of
    who didn't deliver.
  - **The finale is a sustained flow** worth half the active residents' combined output for 6 hours. It starts by
    itself when the Works' tank holds 1 hour of it and pauses (never fails) when the flow dips. It leaves a Landmark
    visible from the district map and becomes an heirloom.
- **Trunk lines** are built by anyone (a crew, a place, a corporation, a solo player) in 10-tile segments, each in its
  funder's colour and name.
  - **Cost:** about 2 h of the funder's T(L) per steel segment, plus 300 steel and 100 concrete.
  - **Pump houses** every 25 tiles can be adopted; an unadopted pump still works at base level.
  - **Lanes:** 20 per trunk. A lane is a link, or a delivery route to a hub, the Port or a Wonder. Owners' lanes are
    free; spare lanes rent at a **game-fixed 2%** toll split among the segment funders. There is no upkeep. When
    every owner is asleep, the hub runs the trunk as a public line.
- **Public hub trunks** (copper tier, 2%) run to the neighbouring hubs and the Port, so a solo player can always reach
  far markets.
- **District Line Race:** for each Discovery (announced 6 h ahead, lasting at least 7 days), the first three places
  to pipe their plaza to it get a permanent marker.
- **Competition:**
  - leagues of about 25 companies at a similar stage, with no demotion; they pause when the server has fewer than 10
    active companies;
  - a **Station Cup** that counts each place's best 3 contributions of the week;
  - a rotating "Supplier of the week".

## 3 · What the player sees

**The picker** (start screen: *Start with friends* · *Find me neighbours* (default) · *A place of my own*). The
district map, zoomed out, shows one card per station or halt:

| On the card | Example |
| --- | --- |
| Free seats and their land | "Riverside: sand ★5, water ★4, shore · Copper Gulch: held for a friend" |
| Residents | "Mo · Lv 14 · lights on 6 of 7 days · usually here: evenings" or "Bertha's Copperworks ⚙ House" |
| What it trades | "imports 1,100 copper a day · sells energy, coal" |
| A waiting link | "Fox already buys glass at this seat's gate" |
| The Works | "Water Tower done · Workshop 40%" |
| The district | hub specialty this week, hub stage, "busiest in the evening", lights on today |

Crews drop banner pins, drag their banner onto seats and tap **Settle**: the balloons land together. "Find me
neighbours" picks the best open seat in a place with 1–3 active humans of similar hours and complementary land.
"A place of my own" opens a fresh station with Houses.

**The plaza** is at your inner corner, on screen whenever you look at your own land:
- the brass manifold with one socket per seat and your junction pipe in your colour;
- the pellets of every pair link, in the seller's colour;
- the Works in the middle, with its ring and its next basket;
- the notice board with Wishes, and the balloons of resting friends;
- your own quarter, which you may decorate (cosmetic only).

Nobody can lay or remove anything there.

**A link** has a panel with both names, the goods, the price preset, the priority and cap, this week's flow, loss and
value, the tank's fill and its state sentence. **Crew Lens** (key C) hides everything except your crew's and your
place's flows.

**The crew panel** lists the members (lit or resting, never a day count), the ticker ("Dev's copper saves the crew 840
g/h"), Wishes and standing orders, this week's flows pair by pair, held seats with their codes, and Leave.

**The return** takes three screens, under a minute:
1. **While you were away:** at most 5 items and a 10-second timelapse of your place, for example "Ana tended your
   estate 17 times".
2. **Your estate is where you left it** (or safe on its island).
3. **Where to land:** your held seat, your old seat, next to your crew, a lively place, or the Frontier district.
   Landing takes one minute.

## 4 · The economy: what the prices and limits add up to

The prices are in §2.4 and the Houses in §2.5. In one sentence: *friends pay best, chosen partners about as much,
Houses less, the hub least*.

**Bounds:**
- Between companies only goods move, never gold.
- The most value a unit can carry over the hub price is 1.5 × base, and only for goods that were really produced.
- Houses add at most about 6% of a company's output value in new gold.
- Your own sales move only your own hub price.
- The Port is reached only by pipe, with its loss and toll.

**Solo parity:** a solo player sells about 25% of his output to Houses at 1.25 (+6% income) and buys at 2.0 instead
of 2.5. A crew player sells about 25% at 1.75 (+19%). That makes the gap about **10–15%**: worth inviting a friend,
never worth quitting over.

**Gold sinks:** hub imports, House margins, trunk segments (the big late sink for the millions that pile up by week 2),
the caretaker's 10% and the tolls.

## 5 · The 12 decisions

| # | Decision | Why | What would change it |
| --- | --- | --- | --- |
| 1 | **Strangers in a place share the project and the view, not a market.** They are neighbours: the Works, the board, the plaza. Goods between them flow only over links both sides accepted (one tap on a card) | The owner's complaint was trade nobody chose. A shared pool also lets a stranger take a couple's coal (A1 below). A public-good Works still gives strangers something to do together | If fewer than 30% of profitable stranger link cards are accepted within 72 h, links between place neighbours start automatically and either side can decline |
| 2 | **Places of 4 and 2; crews of 1–8 over a station and its halts.** Couples get a halt. 5–6 friends take a station and its attached halt, 7–8 both adjacent halts. A falling-out: leave in one tap, nobody can be evicted, links pause with 1 h notice | Seats come from today's geometry with nothing moved; people come in any number, so the crew spans places. Consent both ways | If more than 10% of crews reach 8, allow two stations joined by a crew trunk. If halts stay empty, merge them into their station's plaza |
| 3 | **Nothing is shared but chosen bilateral links** (and the Works as a sink). No Yard pool, no Main, no merged network | Per-pair priority, caps and manifests answer "who got my coal". One member's expansion can't silently take another's supply. It solves per estate | If players never touch pair settings (more than 90% at default after 2 weeks), keep the engine and show the crew as one pooled "Main" in the UI: B's view on C's engine |
| 4 | **Direct trade between two named companies, never through a third estate. A third party may carry it** on a trunk lane at a game-fixed 2%, with no upkeep; the hub runs abandoned trunks. Public hub trunks at 2% | Direct and visible, yet long-distance logistics becomes the late game. A fixed toll prevents gouging and toll wars; no upkeep prevents the Eve bill trap | If rented lanes stay under 5% of all lanes by week 4, drop rents (public once built, as r2-6 suggests). If owners ask for control, allow 0–3% |
| 5 | **A ladder:** crew 1.75 fixed; links in three presets; Houses 1.25/2.0; hub 1.0/2.5 with the Hub Order. **Funnelling:** value ≤ 1.5 × base per produced unit; links between accounts on one device locked to Fair; no gold gifts; crates ≤ 1 h of own production | Friends think about nothing; strangers get a little choice; the caps bound what an alt is worth (one real partner) | If same-device links carry more than 5% of trade value, new accounts trade at Fair only for their first 7 days. If crews ask for a mates' rate, add Friend 1.375 to crew links |
| 6 | **Houses fill empty and held seats and succeed leavers**, labelled with a cog, at House prices, all of them together capped at 25% of a company's output. Successors take 80% of the 14-day median and decay 10% a week | One NPC system covers solo, waiting and leaving. House prices make "quitting" alts worth less than a playing alt, and the total cap makes many Houses no better than one | If solo players earn less than 85% of crew players, raise House volume to 35%. If Houses' new gold exceeds 8% of all faucets, lower their price to 1.15 |
| 7 | **Timeline 3 / 14 / 30 days; the estate runs at 100%; any active crew member holds a seat for 60 + 60 days, and a held estate stays in place.** Seats for arriving friends are held while the crew is active, up to 28 days | Absence costs nobody anything; the friends decide, never a rule or a stranger; no Quiet Isles trip while friends wait | If more than 30% of holds end unclaimed at 120 days, shorten them to 60. If more than 20% of returns happen after 120 days, extend |
| 8 | **The picker shows people, land, trade and the Works; "Find me neighbours" is the default; moving** is free in the first 3 days, free once per chapter to join a crew, otherwise weekly as now. A lone human gets one card after 7 days alone | Choice with information (Catan), and a default that puts solo players next to humans (safe in C, because neighbours don't share a pool) | If more than 40% of solo players switch to "A place of my own", make it the default |
| 9 | **The migration below, with no wipe** and nothing moved | The corner parcels are unbought; corridor pipes become links (TradingRethink A) | If more than 5% of testers have buildings on corner parcels, use r2-6's fallback: plazas only in new districts |
| 10 | **Keep 32×32 and the 16×16 start area.** It holds the HQ, iron ★3 3×3 and the seat's ★5 home good 3×3 (an energy seat gets its hill). The surveyors' ★2 patches move into the first two buyable parcels (Levels 5 and 7) for new estates; existing estates keep theirs | The role is visible at minute 1; the first parcel has a purpose; at Level 5–6 comes the first make-or-buy ("Ben's coal at 1.75 or my ★2 patch"). Loady already asks for that parcel on the first evening | If `TheFirstDay` reaches Level 10 later than day 2, or the first Smelter comes more than 10 minutes later, put the patches back |
| 11 | **Late game:** the Works finale, trunks with lanes, Line Races, Hub Order pushing surplus to far hubs and the Port, Era Wonders, leagues, the Station Cup. **Next chapter:** crews, estates, places, trunks and holds persist; the Landmark becomes an heirloom; the Works restart with a new project; an opt-in move of a whole crew to a Frontier district | Week 3 income is flat (+2% from day 14 to 21), so growth has to come from crossing borders, and that is logistics | If week-4 testers spend under 20% of their gold on trunks and Works, make Discoveries more frequent (every 4 days instead of 7) |
| 12 | **Each estate solves on its own;** links and lanes are teleporter-style terms filled from the last solve (one tick of lag, hidden by the tanks). Each estate has at most 12 links, each trunk at most 20 lanes. Houses are supply tables updated every 15 min. Districts with no lit seat skip House updates | A change re-solves one estate, never a district: the 380 ms mega-network can't come back | If a re-solve exceeds 20 ms per estate on the biggest tester estate, cap links at 8 |

## 6 · The hard cases

**a · Three friends tonight; Dev on day 8; Ben quits on day 3 and returns on day 36.**
- 20:00: Ana founds Kettle Works. 20:05: the three settle in Ashford NE, and Dev's seat (Copper Gulch) is held by
  Bertha ⚙. 20:12: Ana reaches Level 3, the junction pipe is drawn, and the **Handshake** plays with Ben as their
  crew link starts at 1.75. 20:40: the Foundation is done.
- Day 2: Wishes and standing orders (Ben's coal goes to Cleo first).
- Day 3: Ben's last session. His estate keeps delivering at 100%, and the Works baskets resize when he counts as
  resting on day 6. Ana tends his estate.
- Day 8: Dev settles. Bertha leaves him a starter kit and her links. His copper sells at 1.75 instead of 1.0, and the
  crew pays 1.75 instead of Bertha's 2.0. Tailwind ×1.57.
- Day 17: Ana taps Hold. Ben's estate stays in place, with the caretaker and autopilot running it.
- Day 36: Ben's return in three screens ("Ana tended your estate 17 times"), the Era 2 jump kit, Tailwind ×2.5. His
  coal now feeds the crew's steel chain: he is the keystone of their Era 2.

**b · Solo on a server of 5; a server shrinking from 200 to 20.**
- On the server of 5, Sam taps "Find me neighbours" and lands next to Mo (mornings) with one House. A link card to Mo
  is accepted overnight. Quiet grace switches the Hub Order off with ≤ 5 lit seats, leagues pause, and the Station
  Cup and "your best week" remain. He earns about 10–15% less than a crew player, with no wall anywhere. A solo trunk
  to the Port becomes the server's main road, with his name on it.
- On the shrinking server, newcomers and returners land only in the 1–3 districts with the most lit seats, and a lone
  human gets one card offering a free move to a place with humans. A district with no lit seat freezes its Houses.
  The quiet-district check counts lit seats instead of "≥ 4 players" (r2-3's code finding). Over two weeks the 20
  drift into one or two districts by their own choice.

**c · A newcomer in week 3.** Noor's card shows "Fox already buys glass at this seat". The seat is a House's starter
kit plus a waiting link. The fair-price band, the Host's welcome bonus (paid by the game), Tailwind ×2.36 and the
Era kit apply. Her glass completes the Foundry Hall (keystone +20%) and her name is on the plaque. In the Rookies
league she can win.

**d · A griefer with two alts.**
- A leaked crew link doesn't give a seat: seats need their code, and the link needs a founder's tap after 24 h.
- He can't block a corridor, because links are bundles; he can't touch a plaza, because the game draws it.
- Dumping moves only his own price.
- Trading with his alts: at most 1.5 × base per produced unit, locked to Fair when they share a device.
- His alts "quit": their successors pay House prices, decay, and sit inside the 25% House cap, so the farm earns less
  than playing the alts.
- He can't trap newcomers (the fair-price band, no shortfall below Level 6, no offers from 10+ levels above in their
  first 3 days). Tolls are a fixed 2%. Requests are limited to 5 an hour, and the block list works.
- The worst he can be is a neighbour you block.

**e · Two friends fall out on day 10.** Cleo leaves Kettle Works with one tap. Her crew links become ordinary links
at Fair, the same 1.75, so nothing changes until someone acts. Ana pauses her side: after the 1-hour notice and the
2-hour tank, Cleo's sand goes to Cleo's other partners and her Trading Post. Both state lines read neutrally
("paused"). Cleo keeps her seat, can move for free once this chapter, and both can block each other's postcards. The
Works stays a public good for both, with separate credit. The crew's banner, heirlooms and held seats stay with the
crew.

**f · Seven friends.** "Pick for us" finds a station whose two adjacent halts are free: 8 seats, 7 humans and one
held seat. The game draws crew pipes to the halts (34 and 68 tiles); all crew pairs are linked at 1.75; the halts'
members deliver to the station's Works through the crew pipe. There is one banner and one ticker. A ninth friend
starts a sister crew next door, and the two crews join a corporation.

**g · Week 4 and the next chapter.**
- Week 4: the Hub Order bars show 107–125%, so Kettle builds a 17-segment steel trunk to a Discovery oil field and on
  to another hub. Payback is 3–5 days, and the spare lanes rent at 2%.
- The Works finale runs over two days without anyone online: the tank opens the window at 02:14 and it pauses twice.
  The Landmark rises, and Kettle is second in the Line Race.
- Next chapter: everything persists; the Landmark becomes an heirloom; the Works start a new project; Ben's hold
  continues; there is a Homecoming week.

**h · The testers' world:** see §7.

## 7 · Migration (no wipe)

1. **Links engine first.** Networks stop at gates. Corridor pipe chains that join exactly two gates become links at
   Fair (TradingRethink A); other corridor pipes are refunded at 100%. Tenants' all-gate pipes become House link
   offers.
2. **Places.** Every district is read as 4 stations and 4 halts. Nobody moves.
3. **Plazas.** The corner parcels are carved. If anyone owns one or has pipes or buildings on those tiles, the
   buildings go into crates with a full refund and the parcel's price is paid back. Junction gates are added.
4. **No automatic links or crews.** One card: "In your station: Mo and a House. Link?" Another card: "You traded most
   with Ana and Ben last week. Form a crew?"
5. **Tenants become Houses**, keeping their names and works; their prices change to 1.25/2.0 with a 7-day notice to
   their partners.
6. **Hub Order.** Hub prices reset to base, and each company's Hub Order starts from its level.
7. **Moves.** Every tester gets one free move this chapter, as a crew block, to sit with friends.
8. **The Quiet Isles keep their estates;** returns use the new landing cards.
9. **Checks.** Run the e2e consistency check and the balance runs before and after; the targets are human trade at
   15–30% of output and solo parity at 85–90%.

## 8 · Build steps (each one playable)

| Step | What | Days |
| --- | --- | --- |
| 1 | Links: no network crosses a gate, bilateral links with presets, tanks, notice, fallback, state lines, migration of corridor pipes | 2 |
| 2 | Places: plazas, junction gates and pipes, pair links at the manifold, the picker and placement | 3 |
| 3 | Crews: founding, seat codes, held seats, crew links at 1.75, colours, ticker, leave/remove, crew pipes | 2 |
| 4 | Houses: archetype prices and caps, seat warming and starter kits, successors with notice and decay | 2 |
| 5 | Absence and return: holds, held estates in place, balloons, tending, the three return screens, Tailwind, jump kits | 2 |
| 6 | Economy: Hub Order with quiet grace, the Port, public hub trunks | 2 |
| 7 | The Works with baskets, keystone credit and the finale; Wishes with a House fill; postcards | 2 |
| 8 | Trunks with lanes, the Line Race, leagues and the Station Cup | 3 |

That is 18 days. Steps 1–3 (7 days) already make "three friends tonight" work.

---

## 9 · Attacking the rivals

### Concept A · Stations with a Yard (r2-5, with r2-3 and r2-6)

**A1. The Yard pools friends with strangers, so a stranger can take a couple's trade.** In r2-5 everyone who pipes
to the Yard joins one pro-rata pool at one price; in r2-3 non-crew residents share a 1.4/2.0 Yard market; r2-6 even
keeps a merged crew network.
- Lea and Max, a couple, are placed in a station next to Vex and a House. All four pipe to the Yard, because it pays
  best.
- Day 6: Vex builds 8 Brickworks, and the pool now gives him 70% of Max's coal. Lea's Smelter, which ran on Max's
  coal, gets 30% and imports the rest at 2.5.
- Lea can't tell the pool "my partner's coal comes to me first". Her only way out is to unplug from the Yard, losing
  the best price on everything else, and lay a gate link.
- So a stranger they never chose takes their trade. That is the owner's original complaint, inside a 4-seat box.

**A2. The game hands a friend's seat to a stranger, and nobody can undo it.** In r2-5 an asleep crew member's seat
"stays the crew's for 30 more days, then opens to newcomers by itself". r2-3's default "Join a table" puts solo
players into open seats next to humans, and nobody can evict.
- Mara, the crew's fourth, goes on parental leave on day 5. On day 35 she is asleep, and on day 65 her seat opens by
  itself.
- On day 66 the default placement drops Grim into it: it is "a table with active humans". Grim now shares the
  friends' Yard pool.
- Mara opens the game on day 80 and lands in the halt next door, 34 tiles from the Yard she built with her friends.
- Nobody pressed a kick button, and that is the problem. A rule made a social decision for the crew, and the crew
  has no way to reverse it.

**A3. Copy-trade lodgers and hub top-ups create gold.** In r2-5 the lodger "keeps the last 7 days of trades at the
same price". In r2-3 it keeps 100% of the volume for 14 days, then up to 60% of a human's, grows to cover unmet
demand, and the hub tops up a short link at the link price for 14 days.
- Gary's alt runs a real brick business for a week, buying 1,200 coal/min from Gary in the Yard at 1.6 (coal's base
  price is 2.5 g). Then the alt "quits".
- The lodger keeps buying 1,200 coal/min at 1.6 without needing any stone or energy, and r2-5 gives the copy no
  decay and no price change. That is 1,200 × 2.5 × 0.6 = **1,800 g/min of new gold**, about half a day-14 income.
  Two alts make it a whole income.
- With r2-3's top-up, an alt that links coal to Gary at Friend (1.375) and then "rests" makes the hub sell Gary coal
  at 1.375 instead of 2.5 for 14 days. Repeat with each alt.
- In C a successor pays 1.25, decays 10% a week, and sits inside the 25% House cap, so the most a fake quitter can be
  worth is about 6% of output.

### Concept B · Streets & Lines (r2-1)

**B1. Lodgers on the Main pay the friend price, so two bots are worth two friends, and alts multiply them.** B's
Lodgers trade on the Main at the crew price 1.75 and buy up to about 15% of a typical player's output each. A Street
of one gets two of them, and a crew holds "crew size + 2" lots.
- Honest Sam on "Sam's Row" gets 2 × 15% × 0.75 ≈ **+22% income**, not the "roughly 10%" B claims. That is B's
  own figure for "a crew of humans, about 20–25% more". So bots equal friends, and inviting someone earns nothing.
- Gary founds a crew with 5 alts that play for ten minutes each, and holds 2 more lots. The alts "quit" into copies
  that fade to the 15% Lodger limit. That leaves 7 Lodgers on his Main, about **+79% of a typical income** in new
  gold: the 2 held lots for 28 days, the alts' lots for as long as no human takes them.

**B2. A stranger on a crew's street can't use the street's pipe, and the solo default leaves players alone.** "Pick
for me" places a solo player on an active crew's street, where non-members trade only by links, or else founds a
Street of one.
- Sam lives on Kettle Street. The teal Main with its lamps, Cistern and Gate Arch runs past his gate every day, and
  he can't plug in unless the crew adopts a stranger. If they do, "a crew" stops meaning people who chose each other,
  and "taking someone off the Main" becomes the drama B itself fears.
- On a server shrinking to 20, the alternative default produces twenty Streets of one, each with two Lodgers,
  scattered over the districts. B has nothing that draws them together except an opt-in migration once per chapter.

**B3. The pro-rata Main hides who gets what, and one member's growth silently takes another's supply.** The Main
shares all offers among all needs, and its valve is set per member, not per partner.
- Day 12: Ben's 120 coal/min feeds Ana's Smelters (80/min). Dev builds six Steel Mills (60 coal/min). The Main now
  splits Ben's coal 69 to Ana and 51 to Dev, so Ana's Smelters drop to 86% and import the rest at 2.5. Ana changed
  nothing.
- Ben can't say "Ana first", and Ana can't see why her coal fell. Her remedies are a contract or B's own fallback,
  "explicit member-to-member links on the Main", which is exactly C's pair links.
- B attacked my round-1 Yard with "other people's choices break your factory". Its Main does the same through a
  teleporter.
