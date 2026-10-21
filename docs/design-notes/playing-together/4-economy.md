# 4 · Economy and the long game: why players need each other

Angle: from the first hour to week four, why does a player WANT another player, and how does that stay fun rather
than forced? Numbers are first guesses for a balance run, in "× hub base price" unless said otherwise.

## The core idea in five lines

1. **Comparative, not absolute advantage.** Everyone can make everything, but your land makes two things cheaply and
   the rest dearly. Trade is a gain, never a gate.
2. **A price ladder that everyone understands:** the hub pays least and charges most, the station market is in the
   middle, a chosen partner is best. Each step up is a social step (nobody → your station → a person you picked).
3. **Scale is what pushes you outwards.** Early volumes fit the hub; by week 1–2 your main product saturates what the
   hub pays *you*, and tier 3+ inputs are scarce at the hub. Interdependence grows on its own, exactly when the player
   is ready for it.
4. **Logistics is the late game.** Short links are cheap and personal; long distance needs trunk lines (shared,
   owned, rented), which become a playstyle and a group project.
5. **Nobody can hurt you through trade.** Every link has a hub fallback, prices are a position in a fair band, every
   change has notice, and nobody can move anybody else's hub price.

---

## Part 1 · Mechanics (15)

### M1. Land with comparative advantage ("home goods")

Every estate has **two home goods** (★4–★5) and small ★2 deposits of every other Era 1 basic, so solo play can make
everything, just less well.

| Richness | Extraction | Where |
| --- | --- | --- |
| ★2 | ×0.75 | one small deposit of every basic on every estate |
| ★3 | ×1.0 | the guaranteed iron deposit, average terrain |
| ★4 | ×1.5 | home good #2 |
| ★5 | ×2.0 | home good #1 |

- A specialist makes its home good with about **2.5× fewer buildings** than an outsider (×2.0 vs ×0.75). Turned into
  money: covering an off-good yourself costs a building slot that would earn 2.5× more on your home good. That is the
  whole reason to trade, and a solo player feels it only as "slower", never "stuck".
- Energy gets the same treatment through terrain (wind ×1.6 hills, sun ×1.5 desert), so "the energy neighbour" is a
  natural early partner.
- **Stations/positions advertise their land mix** before you choose (see the positioning designers): "this station:
  2× coal, 1× copper, 1× sand free; you'd be its only copper". The default placement picks the estate whose home
  goods are rarest in that station (complements, not competitors).
- Friends who start together get a hint: "pick different home goods, together you cover the steel chain".

### M2. The price ladder: hub < station market < partner

| Ring | You sell at | You buy at | Who | Fee |
| --- | --- | --- | --- | --- |
| Hub (trading post) | 1.0 | 2.5 | always there | none, the spread is the sink |
| **Station market** (M4) | 1.4 | 2.0 | anyone in your station, incl. tenants | spread 0.6, of which 0.1 → station fund |
| **Direct partner** (M3) | 1.0–2.5, default 1.75 | same | the company you linked to | none |

- Easy to say in one sentence: *"Neighbours pay best, your station next, the hub last."*
- At Auto price a direct link pays the seller **+25%** over the station market and saves the buyer **12.5%**; the
  station market pays the seller **+40%** over the hub and saves the buyer **20%**. So choosing a partner matters, but
  not choosing is fine too.
- Expected effect on a typical estate (30% of output to others): station market only ≈ +12% income, good links ≈
  +22%. Target: a well-linked player reaches Level 30 roughly 3–4 days before a pure solo player in a 28-day arc.
  Visible, not decisive.

### M3. Prices as a position in the band, not a number

- A price is a **slider 0–100% across the fair band** (hub buy → hub sell), default **Auto = 50%**. It moves with
  the hub price by itself, so contracts and settings never go stale and nobody has to watch prices.
- **Friend price** = 25% (a "mates' rate" preset for crew members), **Premium** = 75%. Three presets cover 95% of
  players; the slider is for traders.
- The band makes gold funnelling bounded: the most value a unit can carry from one company to another is
  (2.5 − 1.0) × base, and only for goods that were actually produced (see the exploit table).

### M4. The station market (the liquidity ring)

The station is the 4–8 estates around one junction (or whatever the positioning design calls the group). Each
estate pipes **one link to the junction** (its "market line"); everything not taken by own consumers, contracts and
direct partners reaches it, and the junction pairs station sellers with station buyers.

- **Continuous, pro-rata, no order book:** for each resource the junction sums supply S and demand D (from the last
  solve, teleporter-style). Each seller delivers its share × min(1, D/S) at 1.4, each buyer gets its need ×
  min(1, S/D) at 2.0. The rest goes to the hub (1.0) or comes from the hub (2.5, if imports are on).
- **Tenants** take part, so a quiet station still has buyers; a solo player is never without a market.
- **The 0.1 to the station fund** pays the junction's upkeep and the Station Works (M12): trading in your station
  visibly builds your station ("+3,240 g to the station chest this week").
- **Why it isn't the old mega-network:** no pipes merge, every estate's losses are its own market line only, nothing
  of others flows through your pipes, and the solve is O(estates) per station.
- On the map: the junction is a small plaza with a board; pellets flow in and out on each market line in the owner's
  colour.

### M5. Direct links: estates are islands, links are bilateral

TradingRethink option A, kept and sharpened:

- **No network crosses a gate.** A link is a pipe from your gate to another gate; it joins exactly two companies and
  is never transitive.
- **Capacity by pipe tier, loss by length** (corridor part only). With today's losses:

  | Link length | Grey (0.3%/tile) | Copper (0.15%) | Steel (0.05%) |
  | --- | --- | --- | --- |
  | 10 tiles (next door) | 3% lost | 1.5% | 0.5% |
  | 50 tiles (across a station) | 14% | 7% | 2.5% |
  | 200 tiles (next station) | 45% | 26% | 9.5% |

  So grey links are for neighbours, copper for your station, and only steel trunk lines (M9) make the next station
  pay. Distance becomes a real, readable decision.
- **Links are auto-routed through corridors** (the player picks the two gates, optionally waypoints) and **don't
  occupy exclusive corridor tiles**; many links share a corridor drawn as a bundle. Nobody can block a corridor by
  squatting it with pipes.
- **Accepting:** a link to an Open gate starts at once; otherwise a request card. Either side can switch the link to
  "paused" (with the 1 hour notice).
- **Never stall:** every link and contract has an automatic **hub fallback**: when a partner's delivery drops (offline
  shortage, paused, asleep), your consumer is fed from the station market, then the hub (imports switch on for that
  input only, up to the lost amount). Trade is upside only.

### M6. Partner trust (why stable pairs are worth keeping)

- Each link has a **trust level** that grows +1 per day of continuous trade (any amount), up to 10.
- Effect: **−0.5% loss and +0.5% throughput per trust level** on that link (max −5% loss, +5% capacity), and a badge
  on both estates ("partners for 10 days").
- Breaking (pausing the link for more than 24 h) halves the trust; a sleeping partner's caretaker keeps it.
- Small, but it makes "my copper friend" a relationship rather than a vending machine, and it rewards loyalty without
  locking anyone in.

### M7. Personal hub saturation (scale pushes you to players)

Replace the global hub price reaction (which lets one big seller crash everyone's price) with a **personal allowance**:

- The hub pays full price for each resource up to an **hourly allowance per company**: `A = 600 g/h × Era ×
  (1 + Company Level / 10)` of hub value per resource. Above it, the marginal price halves for each further A
  (1.0 → 0.5 → 0.25 …).
- A day-1 player never sees it. A week-1 estate with one dominant product hits it and the UI says: "the hub is full
  of your iron ingots. Your station buys at 1.4, Anna's Steel Mill would pay 1.75, the Wonder takes them for XP."
- Nobody can move anybody else's price: **veterans can't crush newcomers by dumping.** District-level variety comes
  from the hub's weekly specialty demand (+20% for one good) instead.

### M8. Allocation: tier 3+ goods are scarce at the hub

- The hub sells tier 1–2 freely at 2.5. Tier 3+ (steel, wire, concrete, gears, plastic, circuits …): an **import
  allowance of about 2 hours of one factory's need per day** at 2.5, then 4.0.
- Late chains therefore make you choose: build the whole chain at home (slow with ★2 land, but always possible), or
  source from a specialist. This is where week-2 interdependence comes from, and solo is still a complete path.
- Tenants never sell tier 3+; that is the players' market.

### M9. Trunk lines: shared, owned, rented infrastructure

The long-distance layer and the "magnate" playstyle.

- **Built by a consortium** (1+ companies: friends, a corporation or a station) along corridors and wilderness,
  between junctions or gates. Steel tier from Era 2: 40 g + 1 steel per tile, 0.05% loss per tile, 5,000/min.
  Example: a 200-tile line to the next station ≈ 8,000 g + 200 steel; an inter-district Grand Trunk (~350 tiles,
  Glass Conduit) ≈ 70,000 g + glass and circuits.
- **Capacity is cut into 20 slots** (250/min each). Founders receive slots in proportion to what they paid. A slot is
  a bilateral link (M5) between any two on-ramps on the line: still direct trade between two named companies, the
  trunk only carries it.
- **Spare slots are rented** at a toll the consortium sets, **0–3% of the value carried** (or a flat weekly fee), with
  24 h notice for changes; a renter's slot can't be taken away mid-week.
- **Upkeep** 0.5% of the build cost per week, paid from tolls first, then by the owners in proportion to slots.
- **Abandonment:** if every owner is dormant for 14 days (or the consortium dissolves), the hub buys the line at 50% of
  its cost and runs it as a public line at a 2% toll (a gold sink). A line never dies and never strands its renters.
- On the map: a thick line with coloured lanes per renter, a name plate ("Copper Road · The Smiths & Co.") and an
  income counter. Owning a famous trunk line is status.
- Simulation: at most 20 links per line, solved like teleporters; no merged network.

### M10. Crews: friends from minute one

Corporations need Level 8 and are built for 30. Friends need something on day one.

- A **crew** is 2–6 companies, formed from Level 1 by invite code. Crew members: start in the same station
  (reserved spots, see positioning), share a colour stripe on banners and pipes, see each other's surplus and needs,
  and get **pre-approved links** (no requests) with the friend price (25%) as default.
- **Crew links skip the station fee** and get trust +2 at the start.
- A crew can later become (or join) a corporation; the crew keeps its stripe as a "house" inside the corp.
- Joining a week later: the late friend gets the crew's station (a reserved or freed spot, or a swap with a tenant
  estate), and a **catch-up boost** while their level is below the crew's average − 5: +50% XP from crew trade.
  Their friends' surplus at the friend price is the best possible start.
- **No gifts.** Value only moves with goods inside the band; the friend price is the generosity knob.

### M11. Contracts v2: async and safe

- **Price as a band position** (M3), so it tracks the hub. Length 1–7 days (30 with Futures), **auto-renew** if both
  sides leave it on (the caretaker renews for dormant players).
- **Contract tank:** the buyer gets a free buffer of 2 h of the contracted flow. Short offline hiccups never reach
  the factory.
- **Shortfall = the difference, not a fine:** when the tank runs dry, the hub fallback feeds the buyer and the seller
  pays the difference between hub price and contract price, capped at 10% of the week's contract value. The buyer is
  made whole, the seller learns, nobody is ruined.
- Newcomers (below Level 6) can't sign contracts with a shortfall clause; they can only sell "best effort".

### M12. Station Works: the joint megaproject

Every station has a **Works site** at its junction that the station builds together over a season chapter.

- **Three stages**, each a **basket** of 3–4 goods from different home goods, e.g. Foundry Hall stage 1: 40% bricks,
  30% iron ingots, 30% glass. Size scales with active members (≈ 6 h of a typical member's output × active members;
  tenants count as 0.5 member and deliver slowly by themselves, so a lonely player's station still finishes).
- **A sustained-flow stage** for the top stage: e.g. "800 steel/min for 6 hours". No single estate can, a few
  together can, and it is async because storages and contract tanks buffer it. This is the "we did it" moment.
- **Reward:** a station perk for the chapter (+10% on one chain, freight ×2 faster, junction capacity +50%) for
  everyone in the station (a public good: a non-contributor costs nobody anything), plus **personal credit by share**
  (gold, XP, a name on the Works' plaque, the contributor's colour in its banners).
- **Keystone bonus:** the delivery that completes a stage's missing good gets +20% credit, so a specialist who brings
  the rare part is celebrated.
- Hub imports count at 25% for credit, so credit reflects production.

### M13. Weekly station goals

- Each week the station gets one goal sized to its active members ("2,000 planks and 1,500 glass by Sunday").
- **Contribution floor:** the full reward needs at least 3% of the goal or 1 hour of your own output, whichever is
  smaller. Below that you get a "thanks" share (25%). Leeching gets you little; a newcomer can always qualify.
- Shown on the junction board with every contributor's bar. Simple, weekly, social glue.

### M14. Mastery and market share

- **Mastery per resource:** every 25k units you make of a resource (season total) gives +1% output for it, up to
  +15%. It rewards specialising and tells the world what you are: titles "Glassmaker", "Master Glassmaker of Riverside".
- **Market share** in your station and district per resource, on the junction board and in the leaderboards:
  friendly competition between two copper specialists for the station's buyers (price, reliability, trust).
- **Recognition is the late-game currency**: plaques, trunk-line name plates, mastery titles, Wonder statues.

### M15. Scarcity events: shortages and discoveries

- **Shortage** (every 3–4 days, one district, 48 h): the hub's buy price for one good ×2 and its imports ×1.5. Players
  with stock and specialists win; storages and Warehouses become a speculation game.
- **Discovery:** a rich wilderness deposit appears; outposts there pay only with a trunk line or freight, so a group
  that built the line first wins the rush. A reason to build infrastructure ahead of need.
- Both are announced 6 h ahead (async fairness: you don't have to be online at the moment).

---

## Part 2 · Interdependence over time

| When | What pulls players together | Who can it hurt? |
| --- | --- | --- |
| Hour 1 (L1–3) | Nothing required. At L3 the market line: "your station bought 120 ⚡ at 1.4" (first proof others exist) | Nobody: fixed prices, no choices |
| Day 1 (L3–6) | First direct link: a card for a neighbour or crew mate who needs what you make; friend price for crews | Nobody: hub fallback, notice period |
| Day 2–3 (L6–10) | Contracts with tanks; the weekly station goal; home goods (M1) start to matter as chains branch | Shortfall capped at the difference |
| Week 1 (L10–18) | Personal hub saturation (M7) on your main product; trust levels; Station Works stage 1 | Nobody can crash your price |
| Week 2 (Era 2) | Tier 3 allocation (M8): steel, wire, concrete from specialists; first trunk lines between stations | Make-or-buy is always open |
| Week 3–4 | Trunk-line consortia and rents; Station Works sustained-flow stage; Era Wonder baskets; shortages; mastery titles | Trunk abandonment → hub line |

The pattern: **each layer appears when the player's scale makes it useful**, and each is upside over a solo path
that always exists.

---

## Part 3 · "Direct trade only" vs alternatives

| Rule | Verdict |
| --- | --- |
| **Direct only, pipes merge** (v1 before 1.1) | No: mega-networks, unchosen partners, slow solves |
| **Direct only, gate to gate** (v1.1, TradingRethink A) | Right core, but too narrow alone: no liquidity, no distance, no role for the solo player |
| **Transit through a third estate** (carrier pipes across someone's land, with a toll) | No: eats the carrier's land, creates hold-ups and blocking, and the estate stops being "yours" |
| **Transit on owned trunk lines** (M9) | Yes: trade stays between two named companies, a third named party carries it for a capped toll, nobody's estate is crossed |
| **Shared grid** (district grid, option D) | Only at station scale, as a market (M4), never as merged pipes |
| **Hub markets / exchange + freight** | Yes, as the slow, far, batch channel (keep the existing Exchange and cargo) |

So: **"direct trade, carried by whoever you pay"**. Every trade has exactly two parties you can name; a carrier is a
visible third party on the link panel; networks never merge across gates.

---

## Part 4 · Three economic models, end to end

### Model A · "Bilateral": links, contracts, trunk slots

Trade = direct links (M5) + contracts (M11) + rented trunk slots (M9) + Exchange/freight for the rest. No station
market.

- Flow: produce → own consumers → contracts → direct partners → storage → hub. Far partners via a trunk slot.
- Good: maximally explicit (every trade is a chosen person), purest pipe fantasy, smallest change from v1.1.
- Bad: liquidity depends on finding partners; quiet stations and odd-hours players fall back to the hub (solo feels
  like a handicap); every new player must make a choice before trade means anything; a partner who quits leaves a
  hole you must fill by hand.

### Model B · "Station grid": everyone pipes to the junction

Trade = one market line per estate to the junction (M4), uniform station prices, trunk lines between junctions (the
station as a consortium). Direct links don't exist.

- Flow: produce → own consumers → contracts → station market → hub. Between stations: junction to junction trunks.
- Good: zero choices, liquid, robust to quitters, simplest to simulate and explain, great for solo and async.
- Bad: anonymous; partners don't matter, so "playing with 3 friends" is barely different from playing with strangers;
  less pipe gameplay (one pipe per estate); the crew fantasy is lost.

### Model C · "Three rings" (recommended)

Hub ← station market ← direct partners, with trunk lines as the long-distance layer and crews as the friends layer.
Models A and B combined, the ladder (M2) deciding which ring wins.

- **Flow for a producer:** own consumers → contracts (with tanks) → direct partners (links, trunk slots) → own
  storage (what it keeps) → **station market** (1.4) → hub (1.0, personal allowance).
- **Flow for a consumer:** own producers → contracts → cheapest direct partner → own storage → station market (2.0)
  → hub import (2.5; tier 3+ allowance).
- Good:
  - Friends: crews link pre-approved at the friend price, share a station, build its Works together (checklist 1, 13).
  - Solo: the station market and tenants give a market and a Works that finishes; solo is ~10% slower, not broken (3).
  - Quitters: links fall back automatically, the market never depends on one person, trunks pass to the hub (4).
  - Early game is one automatic market line; choices come at L3+ as cards (5); the late game is trunks,
    allocation, consortia, mastery and baskets (6).
  - Async throughout: no step needs both players online (7). Exploits bounded by band, allowance and caps (8).
  - Everything is a visible pipe with coloured pellets: market lines, links, trunks (9, 10).
  - Cheap: per station O(estates), per trunk ≤ 20 links, no merged networks (11).
- Bad:
  - Two trading concepts to teach (market line and links). Mitigation: the market line is automatic and teaches the
    ladder; links come later as offers.
  - The 1.4/2.0 station spread competes with direct links; it needs tuning so links stay clearly better (target: a
    direct link beats the market by ≥ 10% for both sides at Auto).
  - More to build than A alone (see steps).

### Recommendation: Model C, shipped in this order

| Step | What | Days |
| --- | --- | --- |
| 1 | Islands + bilateral links (TradingRethink A), auto-routed in corridors, band-position prices, hub fallback | 1–2 |
| 2 | Station market on the junction (pro-rata, 1.4/2.0, station fund) + migration: tenants' gate pipes → market lines | 1 |
| 3 | Personal hub saturation (M7) replacing the global price reaction; tier 3+ allocation (M8) | 1 |
| 4 | Crews (M10), trust (M6), contract tanks and shortfall-as-difference (M11) | 1–2 |
| 5 | Station Works + weekly station goals (M12, M13) on the existing Wonder code | 1–2 |
| 6 | Trunk lines with slots, rents, hub takeover (M9) | 2 |
| 7 | Mastery, market share boards, shortages and discoveries (M14, M15) | 1–2 |

Steps 1–3 already fix today's problems and make solo safe; 4–5 make friends matter; 6–7 are the week-3+ depth.

---

## Part 5 · Exploits and how the design closes them

| Exploit | How it works | Closed by |
| --- | --- | --- |
| **Gold funnelling with alts** | Alt sells to main at 0%, or main buys from alt at 100% | Band caps the value per unit at 1.5 × base, and only for produced goods (reselling hub imports loses money: buy 2.5, sell ≤ 2.5). An alt is worth exactly one real partner's output. Links between companies on the same account/device are flagged and limited to Auto price. No gifts, only band trades and capped corp donations (1,000 g × level per day) |
| **Wonder/Works credit farming** | Buying goods to claim credit | Hub imports count 25%; rewards by share with tiers, capped per Company Level band |
| **Leeching shared goals** | Collect the reward without delivering | Contribution floor (M13); public-good perks cost nobody; personal rewards by share |
| **Price dumping** to ruin a rival or newcomer | Flood the hub | Personal hub allowance (M7): nobody moves anyone else's price |
| **Sudden cut-off** | Pause a link or raise the price at night | 1 h notice, contract tanks, automatic hub/station fallback, shortfall paid as the difference |
| **Corridor blocking / land denial** | Squat corridor tiles with pipes, claim gates | Links are auto-routed and non-exclusive; trunks share corridors; no corridor tile can be owned |
| **Toll gouging / trunk monopoly** | Own the only line, raise tolls | Toll cap 3%, 24 h notice, a renter's week is guaranteed, anyone may build a parallel line, hub takeover when abandoned |
| **Veteran crushing newcomers** | Contracts with big penalties, predatory prices | No shortfall clauses below L6, prices only inside the band, hub fallback, newcomers' sales never below hub price |
| **Dead partners** | A partner quits, your factory starves | Fallback in seconds, caretaker keeps links, asleep partners are replaced by the station market; trunks go to the hub |
| **Spam** | Link requests, contract spam | Requests rate-limited (5 per hour), block list, Open/Closed gates |
| **Collusive cartels** | A corp fixes station prices | Station price is fixed by the hub (1.4/2.0); cartels can only lower prices for others or keep goods (and lose income) |

## One-paragraph pitch

Your land makes two things brilliantly and everything else badly. On day one the station quietly buys your surplus
and you see your neighbours' goods flow in. By evening a friend's copper runs to your Smelter through a short pipe at
mates' rates. By the end of week one the hub is full of your ingots and the Steel Mill next station over wants them,
so you, Anna and Tom pay for a steel trunk line with your names on it, rent its spare lanes, and pour steel into the
station's Foundry Hall for six hours straight. If your friends stop playing, nothing breaks: the station market and the
hub step in, and the trunk keeps running.
