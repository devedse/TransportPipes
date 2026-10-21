# Concept B · Streets & Lines: the definitive spec

Champion: the wild idea thrower (from r2-1-wild.md). The economy layer comes from r2-4-economy.md. Also taken: the
seat lifecycle and gravity from r2-3, and "friends are never kicked" from r2-5.

Notation: prices are × the hub's base price of the good. **T(L)** is the typical income at Company Level L in the
balance runs: about 800 g/min on day 1, 3,000 on day 7, 3,800 on days 14–21 and 4,900 on day 28.

## 1. The concept in five sentences

1. Friends live on the lots facing one corridor, a **Street**, in an elastic crew of 1–8. Nobody's parcel is carved
   away.
2. In that corridor the game lays the crew's **Main**, a pipe that carries goods between members at one fixed
   price. Under the hood it is a many-ended teleporter, so no network ever crosses a gate.
3. Crew Orders grow the Main from a grey pipe into a steel aqueduct with lamps, a Cistern and a Gate Arch that is
   visible from the district map.
4. From Crew Level 5 the Main extends as a **Line** across the map, built in the members' colours. It reaches
   outposts, Discoveries, Wonders, other hubs and other crews; crews trade where two Lines meet, and spare lanes are
   rented at a fixed toll.
5. Every empty, held or abandoned lot is run by a labelled NPC **Lodger** at prices clearly worse than a friend's.
   Nothing stalls, nobody gains from anybody's absence, and coming back takes a minute.

## 2. The rules

**Seven words:** lot, street, crew, Main, Lodger, link, Line. A **Junction** is where two Lines meet. Corporations,
Wonders, the hub and outposts stay as they are.

### 2.1 Place and crews

- **Lots** are today's estates: 32×32 tiles, 16 parcels, a 16×16 start area.
- **A street** is one corridor stretch and the lots facing it: 10 lots, or 9 next to the hub.
- **A crew's stretch** is a run of neighbouring lots on one street, at most 8: its members plus its held and open
  lots. Two crews can share a street.
- **Shapes:**
  - a couple: two lots facing each other;
  - four friends: a 2×2 block (a Quad without a carved yard);
  - seven friends: 4 + 3 lots;
  - nine or more: two crews with a Junction built at founding.
- **Districts open on demand** once the open ones are 60% lit (lit = the owner played in the last 3 days), so a free
  stretch of any size always exists.
- **Invites:**
  - a **seat code** for each held lot, so a link posted publicly can't hand out Dev's lot;
  - a general crew link that needs a member's tap after 24 h.
- **Held lots** (up to members + 2):
  - kept with no renewal needed while any member plays;
  - a friend who never arrived: 28 days;
  - a member who fell asleep: 60 days more;
  - then the lot opens by itself, so nobody ever presses "replace Ben".
- **Open lots** are open by default while the crew has 2 or fewer active humans, and the crew can close them. A
  stranger who takes one joins the crew on **probation**: away 5 days in a row within their first 14, they are
  packed to the Quiet Isles early (nothing lost) and the lot reopens.
- **Leaving** is one tap, with 1 h notice. The estate stays where it is, and the leaver gets one free move this
  chapter.
- **Removal:** only open-lot joiners can be removed, by a majority of the active members, at most weekly, logged.
- **Invited friends are never removed.** Anyone can **pair-mute** anyone: the Main stops moving goods between those
  two, silently, both ways.
- **When a crew splits,** the name, Level and Main stay with the members on the street. Credit, plaques and Line
  segments stay with each person.

### 2.2 The Main

- **Laid by the game** along the stretch: free, striped in the crew colours, lossless.
- **Each member's street gate has a valve:** your surplus goes on, your needs come off.
- **Price:** 1.75 between humans, fixed. Lodgers on the Main use their own prices (§4).
- **Flow order:** own consumers → contracts → Main → links and lanes → storage share → hub.
- **When goods are short:**
  1. first the Wishes and standing orders the seller accepted;
  2. then by need, with at most 50% of a good's supply per member while others want it;
  3. humans always before Lodgers.
- **Throughput per gate:** grey 100/min, then copper 500 (Crew Level 4), then steel 5,000 (Crew Level 8, Era 2).

### 2.3 Links, Lines, Junctions

- **Links** follow TradingRethink option A:
  - gate to gate, never transitive;
  - auto-routed as non-exclusive bundles, so nobody can block a corridor;
  - band prices with presets (Friend 1.375, Auto 1.75, Premium 2.125);
  - a 2 h tank at the buyer, 1 h notice, the hub fallback, and a status line in words.
- **Lines** (Crew Level 5) are the Main extended in 10-tile segments:
  - **Cost:** a steel segment is about 2 h of the funder's T(L), plus 300 steel and 100 concrete. Each segment shows
    its funder's colour and name.
  - **Pump Houses** stand every 25 tiles. A member can adopt one; an unadopted one still runs at base level.
  - **Losses** follow the tier (steel 0.05% per tile).
  - **Destinations:** an outpost (Crew Level 6 gives a shared licence), a Discovery, a Wonder site, another
    district's hub (its own Hub Order and its weekly specialty), or another crew's Line.
- **Lanes:** 20 per Line, each a link between two named companies.
  - Members' lanes are free. Spare lanes rent to anyone at a **fixed 2% toll**, paid to the funders of the segments
    the lane uses.
  - There is no upkeep.
  - When all funders are asleep, the hub runs the Line and keeps the toll (a sink).
- **A Junction** is a link between two Mains, opened by a majority of each crew's active members. Auto 1.75, shared
  out by need from what each Main has left, never transitive.
- **Public hub lines:** each hub's existing trunk becomes a copper line to the neighbouring hubs at 2%, so a solo
  player always reaches far markets.

### 2.4 Absence

| Days away | The estate | Others see |
| --- | --- | --- |
| 3–14 (resting) | Runs at 100%, valve included | Crew: a sleeping cat. Strangers: nothing |
| 14–30 (dormant) | The Caretaker (exists, 10%) makes decisions only | Ivy; crew mates can tend it |
| Day 23 | — | A 7-day notice: "Old Wim (Lodger) will hold Ben's lot and keep 80% of his coal at Lodger prices" |
| 30 and more (asleep) | Packed to the Quiet Isles with its layout (exists) | A successor Lodger; the lot is held 60 days |

`Sleep()` stops ending trades (`EndTradeOf(…, cancelPipes: true)`): every trade is handed over.

### 2.5 Projects and help

- **Crew Orders:**
  - one new order every 8 h, up to 3 waiting, never expiring;
  - each is at most 45 minutes of one active member's output;
  - credit is your share of your own output, capped at 2 h of output a day and saveable for 3 days;
  - the delivery that completes a missing good gets +20% (keystone);
  - hub imports count 25%;
  - no head count anywhere: a thank-you line, never empty slots.
- **Crew Level 1–10 builds the Main:**

  | Crew Level | What the Main gets |
  | --- | --- |
  | 2 | Lamps |
  | 3 | The Cistern (2 h of buffer) |
  | 4 | Copper tier |
  | 5 | Lines |
  | 6 | The outpost licence |
  | 7 | Arches |
  | 8 | Steel tier |
  | 9–10 | The **Gate Arch** |

  - The Gate Arch's last stage is a sustained flow worth half the crew's T(L) for 6 h. It pauses, never fails, and
    tanks buffer it.
  - Perk: +5% production for every member.
- **Wishes:** up to 3 per player, 12–48 h. At the deadline a Lodger fills the rest at 2.0, so no Wish ever fails.
- **Standing orders:** "my spare coal to Cleo first".
- **Sketches:** draw a ghost blueprint on a crew mate's land; they build it with one tap, at their own cost.
- **Tending:** a resting friend's estate gets a flower bed and the visitor +20 XP. No stat buff, so no aid-back
  etiquette. The count goes on the return screen.
- **Housewarming:** in a newcomer's first 48 h, one crate per member, worth 1 h of the giver's own production, only
  to members below Level 10.
- **Presence:** lit windows; "lights on in N lots today"; usual hours on banners; sparkles on what changed; live
  cursors as a bonus.

## 3. What the player sees

- **The picker, the Street Map.**
  - Each stretch is a coloured line with:
    - lit windows and residents (name, level, usual hours);
    - held lots, and open lots with their home goods ("looking for copper");
    - land stars, the hub's specialty and Host lanterns.
  - Three buttons:
    - **Start with friends**: "Pick for us" finds a free stretch of the right size next to active crews.
    - **Pick for me**: the solo default.
    - **A street of my own**.
  - Pick for me scores lots: +3 per active human within 10 levels; +2 for matching hours (−2 for opposite ones); +2
    if the stretch lacks this lot's home good; +2 for a Host; −3 for a dark stretch while lit ones have room. With
    nothing above zero, it founds a street of one next to the liveliest stretch.
- **The Street.**
  - Your street side faces a striped Main, with pellets in each member's colour running gate to gate and a street
    sign with the crew name.
  - Arches come at Crew Level 7 and the Gate Arch at Crew Level 10.
  - Sleepers are balloons tied to the sign, each with a postcard count, never a memorial wall.
- **The Main panel:** the valve, this week's ledger by pair ("Ben → Ana · 12,400 coal · 21,700 g"), Crew Orders
  with their colour ring, the Cistern, and pair-mute (seen only by whoever uses it).
- **The Lines view (key L):** the district as a metro diagram.
  - Each crew is a station on its line, with Junctions as interchanges and lane counts.
  - A Line's panel shows segments by funder, lanes, tolls and its name plate ("Kettle Line · 4,100 g this week").
- **The return,** three screens in under a minute:
  1. *While you were away*: five items, "Ana tended your estate 17 times", a 10-s timelapse.
  2. *Your estate is safe.*
  3. *Where to land*: your held lot / next to your crew / a lively stretch / the frontier.

  Then a jump kit for each missed Era (it never expires), Tailwind, and a "Back!" ribbon.

## 4. The economy

| With | You sell at | You buy at | Limit |
| --- | --- | --- | --- |
| Crew mates on the Main | 1.75 | 1.75 | the Main's tier |
| Link and lane partners | band, Auto 1.75 | band | the link's tier |
| Lodgers | 1.25 | 2.0 | 25% of your output of that good; tier 1–2 goods only (a successor keeps the tier-3 trades it inherits) |
| Hub | 1.0 up to your **Hub Order**, then 0.7 | 2.5 | — |

*"Friends pay best, Lodgers a little better than the hub, the hub always."*

- **The Hub Order** replaces the district-wide price reaction.
  - The hub pays full price for 0.6 × T(L) g/min of each good *from you*, and 0.7 above that. It is shown as a
    standing order you fill, not a cap.
  - Each hub has its own order, so a Line to a second hub doubles it.
  - **Quiet-district grace:** ×2 with 6–11 lit lots, off with 5 or fewer.
  - There is no tier-3 import cap. Nobody can move anybody else's price.
- **Lodgers:**
  - Their works level is the median Level of the humans on the street − 3, at least 5.
  - Four archetypes: Reliable, Grower, Collector, Neighbour in need.
  - Never on boards, at most 25% of any goal, never a Wonder rank.
  - One line in the morning digest; a letter at most weekly.
- **A successor Lodger** gets Lodger prices and 80% of the leaver's 14-day median volume, falling 10% a week to the
  normal Lodger volume.
- **Solo parity:**
  - a solo player sells about 25% of their output to Lodgers at 1.25 (+6% income);
  - a crewed player sells about 25% at 1.75 (+19%);
  - the gap is 10–15%: worth an invite, never worth quitting over.
- **Anti-funnel:**
  - the fixed Main price, and band prices carry at most 1.5 × base per unit, only on produced goods;
  - accounts on one device are flagged and locked at Auto;
  - no gifts but housewarming crates;
  - credit is your own-output share;
  - hub imports count 25%.
- **Faucets** (new gold) are the hub's 1.0 (bounded by the Hub Order), the Lodgers' extra 0.25 (at most 6% of a
  player's output value) and order rewards (scaled to T(L)).
- **Sinks** (gold destroyed) are hub imports at 2.5, Lodger margins, segments, pumps, Main upgrades, public-line tolls
  and the Caretaker fee.

## 5. The twelve decisions

| # | Decision | Why | Changes if |
| --- | --- | --- | --- |
| 1 | **The Main is crew-only.** A stranger joins it only by taking an open lot (joining on probation); all other neighbours trade by link | Consent on both sides (r2-5 A1); no stranger inside the friends' pool | Open-lot joiners quit faster than streets of one: make open lots neighbour lots with a one-tap link card |
| 2 | **Elastic, 1–8.** Couples are a facing pair; 9+ are two crews and a Junction. Friends who fall out pair-mute or leave; invitees are never removed | Groups aren't 4; no carving; no kick drama | 90% of crews are 2–4: cap at 6 for a simpler picker |
| 3 | **Shared: the Main, an N-way teleporter with a ledger by pair.** No merged network and no shared ground to grief | The repository's 380 ms solve lesson; nothing to vandalise | Over 30% of testers can't say who buys their coal: make the Main explicit pair links under the same picture |
| 4 | **Direct between two named companies, carried by the Main, a link or a lane, never through a third estate.** Segment funders own Lines; fixed 2% toll; no upkeep; the hub takes over abandoned Lines | Answers "direct only" exactly; no toll wars, no bills owed by absent owners | Lane rentals under 5% in week 4: drop rentals. One Line carries over 50% of a district's far trade: upgrade the public line |
| 5 | **A ladder:** Main 1.75 fixed; band links; Lodgers 1.25/2.0; hub 1.0/2.5 with the Hub Order | Nothing to haggle among friends; traders keep the band; an alt can't carry more than real production | Human trade under 15% of output: Main to 1.85. Solo more than 15% behind: Lodger volume to 35% |
| 6 | **One NPC kind, the Lodger:** labelled, weaker by price and volume; successors at 80% and −10% a week | One system for empty, held and abandoned lots; the decay kills the fake-quitter farm | Crews lose over 10% of income on a handover day: 90%. Successors make over 2% of new gold: lower it |
| 7 | **Absence:** 3 / 14 / 30 days (as built), a 7-day notice, held 28 days for a friend who never came and 60 days after asleep, then it opens by itself; probation for strangers | Existing code; time to adapt; nobody presses "replace" | Over 20% of day-60–90 returners find their lot gone: hold 90 days |
| 8 | **Picker:** the Street Map, Pick for me preferring humans; free moves in the first 3 days, weekly after that, crews move as a block; fix `WakeUp()` (landing cards) and `CheckQuietDistricts()` (count lit lots) | Choosing is a pleasure with good information; solo players land among people | Pick for me players churn more than choosers: show two cards instead of a default |
| 9 | **Migration with no wipe:** nothing moves, no parcel is taken (§7) | The testers' layouts are their investment | — |
| 10 | **Keep 32×32 with the 16×16 start and the surveyors' starter kit.** New lots put their ★5/★4 home goods in the two street-side parcels by the gate (parcel 5 at Level 5, 6 at Level 7) | 225 of 256 start tiles are free, so the start isn't small. It is complete in kind and poor in amount, and expanding *towards the Main* gives the reason to grow, where the crew sees it. The Level 30-in-week-4 pacing stays | The first-day run buys parcel 5 after hour 3: move the ★4 deposit into the start area |
| 11 | **Late game:** Lines, Junctions, lanes, Lines Races to Discoveries and the chapter's Wonder, the Gate Arch's sustained stage, leagues. Next chapter: crews, streets and Lines persist, a new Race, a frontier district, the Gate Arch as an heirloom, opt-in street moves | Income is flat from day 14 to 21 (+2%), so trade over distance must *be* the growth, and it's pipes | The crew bot gains under 8% from day 14 to 21: cheaper segments |
| 12 | **Performance:** networks stop at gates. Links, lanes and Junctions are teleporter-style (from the last solve, 1 min delay). One Main allocation per crew, O(members × goods), after its members' solves; cross-district lanes use the Quantum Link path. Dark districts don't tick their Lodgers | No district-wide network; a change re-solves one estate; the advisor stays per estate | Goods flip between members from tick to tick: allocate on 1-minute averages |

## 6. The hard cases

- **(a) Three friends tonight, a 4th on day 8, one quits on day 3 and returns on day 36.**
  - 20:05: three lots in a 2×2 block, the 4th held by Bertha. 20:12: the gate pipes and the Handshake. 20:19: the
    first order and the lamps.
  - Ben stops on day 3; his coal keeps flowing.
  - Day 8: Dev's seat code. Bertha hands over a starter kit (at most Level 5) and her links. Dev's copper replaces her
    2.0 with 1.75, and Tailwind runs up to ×2.5.
  - Day 26: the notice. Day 33: Ben sails, and Old Wim keeps 80% of his coal at 2.0.
  - Day 36: three screens, then his held lot in a minute. Old Wim leaves, the valve reconnects, coal is back at 1.75,
    and his trust with partners is kept at half.
- **(b) A solo player on a 5-player server; a server shrinking from 200 to 20.**
  - Sam joins the only crew whose hours match, or founds Sam's Row with 2 Lodger lots on his Main. Grace switches the
    Hub Order off, and the public line and his own Line reach far. Leagues pause below 10 active companies ("your
    best week").
  - When the server shrinks, new districts stop opening and arrivals land only in the liveliest ones.
  - A lone human (the only lit lot on the stretch for 7 days) gets one card ("Fox Street has an open lot at your
    hours: move free, your crew name and Level come along"), then just a button. Dark districts freeze.
- **(c) A newcomer in week 3.**
  - Pick for me picks a Host crew's open lot that lacks her home good, at her hours, with a starter kit and links.
  - The fixed 1.75 means nobody can fleece her. Also: the fair band across 10+ levels, no shortfall clauses below
    Level 6, no offers from 10+ levels higher for 3 days.
  - Tailwind ×(1 + day/14), up to ×2.5. Her own-output share makes her 20% equal a veteran's 20%. Keystone credit and
    the Rookies board.
- **(d) A griefer with two alts.**
  - Holding lots with alts fails: alts count only after 4 distinct days of play, and land is never scarce.
  - He can't wall a corridor (bundles), and hogging fails: the 50% cap, he pays 1.75, and open-lot joiners can be
    removed.
  - Funnelling fails: the fixed price, the band, device flags at Auto, housewarming only downwards.
  - The fake quit fails: the successor pays 1.25 for 80%, falling 10% a week, which is less than trading with the
    alt while it was active.
  - Tolls are fixed, the Hub Order is personal, and requests are limited to 5 an hour.
- **(e) Two friends fall out on day 10.**
  - Cleo pair-mutes Ana; Ben's and Dev's flows are untouched.
  - On day 14 Cleo leaves in one tap. Her estate stays as a neighbour, she gets a free move, and she keeps her
    credit, plaque and segment tolls. The crew keeps its Level, and nobody removed anybody.
- **(f) Seven friends.**
  - Pick for us takes a fresh 4 + 3 stretch, opening a district if needed. One lossless Main of about 136 tiles, and
    orders sized to the active members.
  - An 8th friend fits; a 9th starts a second crew with a Junction.
- **(g) Week 4 and the next chapter.**
  - Kettle Line runs 60 tiles to an oil ★5 outpost and 100 more to Riverside's hub (a fresh Hub Order, glass +20%).
    That is +18% for Cleo, with a payback of 3–5 days. Six lanes are rented to two crews.
  - A Junction with Fox Street. The Gate Arch's 6 h steel flow is buffered over two days. Bronze in the Lines Race.
  - The next chapter keeps everything, starts a new Race and gives the Gate Arch as an heirloom.
- **(h) Migration:** §7.

## 7. Migration of the testers' world

1. Networks stop at gates. Corridor chains between two gates become links; the rest is refunded.
2. Tenants become Lodgers and keep their trades as links.
3. Every company becomes a crew of one, on the street its busiest gate faces. Its Main is laid there, and up to 2
   neighbouring tenant lots become its open lots.
4. Neighbours who traded the most, and corporation members who share a street, get one card: "Found a crew
   together?". One tap joins their stretches into one Main.
5. Scattered friends get a free crew block move this week (built on the existing corporation blocks).
6. Hub prices reset to base; Hub Orders start from each company's Level.
7. Sleepers stay on the Quiet Isles and get the landing cards when they wake. Nothing moves; no parcel or building is
   taken.

## 8. Build steps (each one playable)

| # | Step | Days |
| --- | --- | --- |
| 1 | Links, honest states, handover in `Sleep()`, the hub fallback | 2 |
| 2 | Crews, seat codes, held and open lots, the Street Map, Pick for me, the gravity fixes | 2–3 |
| 3 | The Main: allocation, valve, ledger, pair-mute, Handshake | 2 |
| 4 | Lodgers: prices, caps, the starter kit handover, successors, probation | 2 |
| 5 | The Hub Order and grace, newcomer rules, Tailwind, return screens, jump kits | 2 |
| 6 | Crew Orders and Levels, the Main's looks, Wishes, standing orders, Sketches, tending | 3 |
| 7 | Lines, lanes, Junctions, tolls, hub takeover, public lines, the Lines view | 3 |
| 8 | The Gate Arch's sustained stage, leagues, the Lines Race, chapter carry-over | 2 |
| | **Total** | **18–19** |

Steps 1–3 already make "three friends tonight" work.

## 9. The rivals

### A · Stations with a Yard (r2-5, with r2-3 and r2-6)

1. **A stranger in your Yard, and no way out but leaving.**
   - A's Yard is a pool at 1.6 for every resident who lays the pipe, and Loady tells everyone to lay it at minute 12.
   - Ben's held seat in Kettle's station lapses by itself on day 63, and Vex takes it, lays his pipe and builds 12
     Brickworks. A has no cap on hogging, so the pool gives him 75% of Dev's coal and Cleo's Glassworks falls to a
     quarter.
   - The board says "Dev → Vex · coal". Cleo can't prefer her friend, and A's rule 3 forbids removing anyone. The
     friends' only escape is to move away from the Yard they built.
   - The owner's founding complaint, "nobody chose a partner", now lives inside the friends' own home.
2. **The copy-trade lodger is a gold farm.**
   - A: from day 14 a copy-trade lodger "keeps the last 7 days of trades at the same price", and from day 30 a
     successor inherits the link profile. In r2-3 it copies 100% of the volume for 14 days.
   - Gary's alt buys 800 coal/min from him in the Yard at 1.6, then stops on day 10. The NPC goes on paying 3,200
     g/min instead of the hub's 2,000.
   - That is **+1,200 g/min of new gold**, about 30% of a day-20 income, and 60% with two alts. The "30% of a player"
     cap only slows it down.
   - With honest friends it freezes a last-week spike, and the remaining crew stops building its own chain.
3. **The table for four doesn't fit people, and it costs every tester a parcel.**
   - A crew is capped at 6 (a station plus a halt). In a Discord group of seven, Gus lands in the next station,
     outside the Yard pool, its orders and its perk. By day 4 he is "the one outside".
   - Couples get an 8×18 halt plaza.
   - Every tester estate loses its inner-corner parcel; C does the same, so the 16th parcel is gone for everyone.
   - The 74% arithmetic still holds: most stations in week 3 are one human at a 4-seat table of lodgers, built into
     the ground.

### C · Places, crews, links (r2-2)

1. **Friends get a price knob, and friends compare.**
   - Crew pair links default to Auto 1.75, with an optional mates' rate of 1.375.
   - On day 5 Cleo gives her best friend Ana the mates' rate and leaves Ben at Auto. Ben's panel shows Ana paying
     1.375 and him 1.75 for the same glass. Inequity aversion follows, and an argument in the group chat.
   - Meanwhile Cleo's grey junction pipe (100/min) is shared by four pair links, so Ben's coal to her is throttled by
     Ana's energy. Fixing it means understanding capacity per pair.
   - A crew of six reads 15 pair links, there are three layers to learn, and a price comparison that a fixed price
     makes impossible.
2. **Held seats keep ghosts in place for up to 120 days.**
   - In C a held estate isn't packed. It stays, run by a caretaker that by default reinvests up to 30%, for 60 days,
     renewable once.
   - Ben quits on day 3 and Cleo on day 40. By day 70, Ana's station card shows four residents (two ivy-covered
     ghosts) and a House. It looks *full* to every newcomer, and C has no lone-human gravity, so nobody joins and Ana
     is never offered a lively place.
   - Ben's estate meanwhile sells to the hub for four months. He returns richer than the casual friend who kept
     playing, and since XP comes from earnings, perhaps higher in level.
3. **The stacked solo tax is back.**
   - C keeps the tier-3 import allowance: 2 h of one factory a day at 2.5, then 4.0.
   - Sam is alone on a 5-player server. In Era 2 his steel costs 60% more beyond the allowance, and Houses don't sell
     tier 3.
   - r2-4 and r2-3 both measured this at 25–40% slower in week 2, against the 10–15% gap all three teams promise. Sam
     quits on day 16: the feature meant to make specialists matter has made being alone a wall.
