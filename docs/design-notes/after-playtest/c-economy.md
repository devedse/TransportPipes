# Round C · economy: the land and the money (spec)

This is the spec of the land and the money for "Needs from the land, gifts by pipe". It takes the lonely guardian's
solo floor and guards, the friends-first designer's start-area placement and condition, and the outsider's and wild's
"kits that keep the seat's promise".

**How to read the numbers:**
- **Prices** are × the hub's base price of a good.
- **Margins** are for a level-1 building, in g/min, with own energy at 1 g. Levels multiply inputs and outputs alike,
  so every ratio holds at any level.
- **Recipes** come from `buildings.yaml`, **base prices** from `resources.yaml`.
- **Richness multipliers** come from `GameWorld.Networks.cs:624`: ★1 ×0.6, ★2 ×1.0, ★3 ×1.3, ★4 ×1.6, ★5 ×2.0. The
  centre of a cluster is one star richer (`WorldGenerator.cs:256`).

**A correction to my round A:** today's ★2 surveyor patches give ×1.0, not ×0.75. A 2×2 copper patch feeds four mines
of 10 ore/min each, so the copies are even stronger than I said.

## 0 · The decisions

| # | Decision | Value |
| --- | --- | --- |
| 1 | Universal goods, on every estate | energy, iron ore, water |
| 2 | Land goods, dealt per station, never copied | copper ore, coal, sand, wood, stone |
| 3 | The deal | Corner station of 4 seats: (2, 1, 1, 1). Small station of 2 seats: (3, 2). Every station holds all five once. No seat holds both inputs of one chain |
| 4 | ★ levels | First good: ★5/★4 in the start area beside the HQ. Second good: ★4/★3 in the first buyable parcel. Third (small stations only): ★4/★3 in the second |
| 5 | Kits | The seat's land producer + moilies, up to 4 buildings, about 100 g/min on day 1 for every seat |
| 6 | The one price | **1.3**, for the Yard, Send, links, contracts, the Exchange and the project |
| 7 | Houses | Buy at 1.15 × your hub price (25% cap, as now). Sell land goods at 1.4, up to (2 + ⌊level ÷ 10⌋) buildings' need per good per buyer. Energy and iron at 2.0 as now |
| 8 | No chain runs at a loss on NPC supply | It pauses, with one line saying why |
| 9 | Guards | Storage-bound units at the hub price; same-device trades at the hub price; the stand-in's old price only after 10 play days; a held seat's House sells its land goods uncapped; a new Send cuts a usual buyer by at most 10% a week |
| 10 | The project's pay | 1.3 for what it takes |
| 11 | Confirmed by | A crew-of-four light-bot run (10–15 buildings) at 1.3 and 1.5, plus a solo light bot |

## 1 · The goods

**Universal (every estate):**
- **Energy:** wind and sun everywhere, as now.
- **Iron ore:** the guaranteed 3×3 cluster in the start parcels (`WorldGenerator.cs:287–290`), ★2 with a ★3 centre.
- **Water:** the surveyors' pond, water tiles only, which gives a shore for pumps (`Survey.cs:80–98`) and no beach.

So Loady's first hour (panels, moilies, the Iron Mine, the Small Factory, the Lab: Levels 1–4) works alone, exactly as
today.

**Land goods (dealt):** copper ore (Copper Mine, L5), coal (Coal Pit, L5), sand (Sand Dredger on **beach**, L5), wood
(Lumber Camp, L5) and stone (Quarry 2×2, L10).
- **Sand** follows from terrain: a dredger needs beach tiles (`buildings.yaml`: `sandDredger.placement.terrain: [beach]`),
  and the surveyor pond has none.
- **Wood** needs a new marker, because natural forest is on every estate: the Lumber Camp's placement becomes
  `{ deposit: wood }`. "Old forest" tiles carry a `wood` deposit and stay forest; the forest modifier (`Networks.cs:641`)
  stays. Natural forest elsewhere stays as decoration, and trees still give wood when cleared.

Era-2 recipes reuse these goods, so the deal carries into Era 2 without changes:
- the Steel Mill: iron ingot + coal;
- the Wire Mill: copper ingot;
- the Cement Kiln: stone + water.

## 2 · The deal per station

**The goods:** `WorldGenerator.DistrictSpecialties` (lines 158–178) is replaced by a deal per station, one of the 8
`StationBlocks` (`WorldLayout.cs:217–221`). For each station, the five land goods are shuffled into seat groups:

| Station | Seats | Group sizes | Example |
| --- | --- | --- | --- |
| Corner | 4 | 2, 1, 1, 1 | {wood, stone} · {sand} · {copper} · {coal} |
| Small | 2 | 3, 2 | {coal, sand, wood} · {copper, stone} |

**The pair rule.** No seat holds every bought input of one Era-1 chain, so no seat makes a full chain alone. In Era 1
that means no group contains both {copper, coal} (Smelter) or both {stone, coal} (Brickworks). In a small station the
three-good seat holds coal only with sand and wood.

**The rest of the seat:**
- **Specialty names stay** for the seat card: Riverside = sand, Copper Gulch = copper, Coal Valley = coal, Old Forest =
  wood, Stone Quarry = stone. The seat's first good is its Primary, the second its Secondary.
- **Energy traits:** every corner station also gets one windy seat (Windy Heights hills, wind ×1.35 on two parcels, as
  `:301–306`) and one sunny seat (desert, as `:307–312`). They come on top of the land goods, never instead of them.
  Energy is universal, so these are flavour and a small edge, not a need.

**Coverage per district:** 8 stations × 5 goods = 40 good-slots on 24 seats. Every good sits on 8 seats, once in every
station. A big station (a crew of 5–8 joining a small station) holds each good twice, which is fine.

**★ and place:**

| Good | First good: start area, beside the HQ (friends) | Second good: parcel `ParcelOrder[0]` (L5) | Third good, small stations: `ParcelOrder[1]` (L7) |
| --- | --- | --- | --- |
| Copper, coal | 3×3 cluster at stars 4 (★5 centre, ★4 border) | 3×3 at stars 3 (★4 centre, ★3 border) | as the second |
| Stone | 4×4 at stars 4, room for 2×2 quarries | 3×3 at stars 3 | as the second |
| Sand | a lake of radius 1.6 with its beach ring (`Lake()`) | a lake of radius 1.1 | as the second |
| Wood | a 3×3 forest blob with `wood` markers | a blob of radius 2.3 with markers | as the second |

**No copies, for new worlds:**
- `Survey.cs:13`: `StarterDeposits` becomes `[]`. The pond stays.
- `WorldGenerator.cs:346–359`, the "small extras": the stone strip, the 40% coal and the 50% lake with beach go,
  except on the seat that holds that good.
- Natural beach terrain (elevation < 0.31, line 119) turns to grass on estates without sand.

The test server starts a fresh world when the rules version changes, so no tester loses land.

**Ship order** (the synthesis's guard): "no copies" ships only after F2's one-tap gate pipe passes its phone replay.

## 3 · Starter kits

**The change** (`GameWorld.Tenants.cs:382–389` and `:448–452`):
- The kit keeps producers whose only input is energy, not only those "that need nothing". Today's filter drops every
  mine.
- The seat's first land producer comes first, then moilies, up to `KitBuildings = 4` at level ≤ 5.
- The land producer may be above the player's unlock level ("a gift: you can build more at Level 5").
- `PlanFor`'s Riverside plan becomes the Sand Dredger instead of water pumps (`Tenants.cs` around line 62).

| Seat | Kit | Day-1 income (hub value) |
| --- | --- | --- |
| Copper | Copper Mine on a ★4 tile (16 ore = 56 g, −12 energy) + 2 moilies (60) | **104 g/min** |
| Coal | Coal Pit on ★4 (19.2 coal = 48 g, −8) + 2 moilies | **100** |
| Sand | Sand Dredger (15 sand = 18 g, −8) + 3 moilies | **100** |
| Wood | Lumber Camp at forest ×1.0 (10 wood = 15 g, −4) + 3 moilies | **101** |
| Stone | Quarry 2×2 on ★4 (32 stone = 48 g, −15) + 2 moilies | **93** |
| Today | 3 moilies | 90 |

Every seat earns within 93–104 g/min, so day-1 pacing moves at most +16% against today. `TheFirstDay` must still reach
Level 10 by day 2. The kit sits on the ★4 border, not the ★5 centre: the centre is left for the player's own first
building.

## 4 · The one price: 1.3

**The buyer's margin by price.** "Both" means every non-energy input is bought.

| Chain | Inputs bought | Own-made | at 1.75 (today) | at 1.5 | **at 1.3** | Seller's gain at 1.3 |
| --- | --- | --- | --- | --- | --- | --- |
| Small Factory | ore (universal) | +28 | — | — | — | — |
| Smelter | copper | +25 | −1.25 | +7.5 | **+14.5** | +10.5 |
| Smelter | copper + coal | +25 | −8.75 | +2.5 | **+11.5** | +13.5 |
| Brickworks | stone | +12.5 | +1.25 | +5 | **+8** | +4.5 |
| Brickworks | stone + coal | +12.5 | −8.1 | **−1.25** | **+4.25** | +8.25 |
| Glassworks | sand | +27 | +13.5 | +18 | **+21.6** | +5.4 |
| Sawmill | wood | +7 | −4.25 | **−0.5** | **+2.5** | +4.5 |

**Why 1.3 and not 1.5:**
1. **The deal makes two bought inputs the normal case.** With no copies, a seat without coal that wants a Smelter or a
   Brickworks buys both inputs. At 1.5 the Brickworks loses money (−1.25) and the Smelter makes +2.5, less than a
   Sawmill. At 1.3 every Era-1 chain pays its buyer and its seller.
2. **The House floor must sit above the friend price and still pay.** With friends at 1.5, Houses would sell at about
   1.6. At 1.6, chains with both inputs bought lose money (Smelter −2, Brickworks −4, Sawmill −2), so a solo player
   could never run them. With friends at 1.3 and Houses at 1.4, every chain stays positive (§5).
3. **Funnels shrink.** At 1.3 a unit carries at most 0.3 × base above the hub, against 0.5 at 1.5 and 0.75 today.
4. **What sellers lose is small and real.** Ben's 15 sand to a Glassworks earns +5.4 g/min at 1.3 against +9 at 1.5.
   But at 1.5 half the chains don't get built, and a seller earns 0 from a buyer who doesn't exist.

**The ladder:**

| hub buys | House buys | friend (one price) | House sells land goods | House sells energy, iron | hub sells |
| --- | --- | --- | --- | --- | --- |
| 1.0 | 1.15 × your hub price | **1.3** | 1.4 | 2.0 | 2.5 |

## 5 · Houses: land sales and the solo floor

**What Houses buy:** at 1.15 × the price the hub pays you, at most 1.15 × base, inside the existing 25% cap
(`progression.yaml:56–59`, `Yards.cs:170–173`).

**What Houses sell:**
- **Land goods at 1.4,** up to `(2 + ⌊buyer's level ÷ 10⌋) × one building's need` per good and per buyer. One
  building's need is the largest level-1 Era-1 consumer of the good:

  | Good | One building's need (per minute) |
  | --- | --- |
  | Copper | 10 |
  | Coal | 5 |
  | Sand | 15 |
  | Wood | 10 |
  | Stone | 10 |

  So two buildings at Levels 1–9, three at 10–19, four at 20–29. This is the lonely guardian's floor, adopted
  unchanged. A building's upgrades count in "buildings' need" at level 1, so an upgraded Smelter counts as one.
- **Energy and iron at 2.0**, as now: everybody makes them.

**Which Houses sell:** any House in the buyer's station or big station, after the players (Houses come after humans in
the matcher, as built). For a station with no House, the hub's market sells land goods at 1.4 under the same cap to
players whose station has no seat for that good left. That case only arises when a crew of 5–8 fills two stations
whose deals miss nothing, so it is a backstop.

**A held seat's House sells uncapped.** While a seat is held (for a friend who hasn't arrived, or for a sleeper), its
House or stand-in sells that seat's land goods at 1.4 **with no cap**. Players pay for it, so it is a sink, never a
faucet. The good never leaves the station while the friends keep the seat (lonely, attack 2).

**Nothing runs at a loss on NPC supply.** A building whose only supply of an input is an NPC pauses when its margin at
that supply price falls below zero. The panel says: *"Waits for copper: Cleo makes it; Bertha ⚙'s copper would cost
you more than it brings."* With these prices no Era-1 chain ever reaches that point:

| Chain on House supply at 1.4 | Margin |
| --- | --- |
| Smelter, both inputs bought | +7 |
| Smelter, copper only | +11 |
| Brickworks, both | +1.5 |
| Brickworks, stone only | +6.5 |
| Glassworks | +19.8 |
| Sawmill | +1 |
| Steel Mill (Era 2) with coal at 1.4 | +85 |

**New gold from NPCs** is bounded by 0.15 × 25% = 3.75% of a company's output value. The fix plan's target is ≤ 6%.

## 6 · The guards

1. **Storage-bound units are paid at the seller's hub price.** That covers an Ask, or a Send ending in a Storage. The
   1.3 premium applies only to units a building or the project uses (F1's lack cap).
   - *Closes:* the live Ask funnel (`Asks.cs:246–251` pays `PlayerPrice` into storages, and an alt can pass on nearly
     its whole bank by reselling at ≤ 1.0).
2. **Same-device trades are at the seller's hub price.** Same-device accounts never count for goal sizing (v1.2 §12).
3. **The stand-in's old price only for a crew member with ≥ 10 distinct play days, on another device.** Everyone else
   gets House terms from day one: 1.15 buying inside the 25% cap, 1.4 selling.
   - The change: `StandIns.cs:36`, `StandInOldPriceDays` from 5 to 10, plus a device check.
   - *Closes:* the fake-quitter farm. Today one alt Glassworks fed at 1.75 is worth about 0.27M over 14 days, nearly a
     day of a real player's income. Under the guard it is about 0 above the House baseline.
4. **A new Send takes new spare first.** It shrinks a usual buyer's 7-day share by at most 10% a week, with the 1-hour
   notice and the tank. Nobody's running supply falls overnight, the stranger's included.
5. **The one price is one number.** No presets and no slider. The most a unit can carry is 0.3 × base above the hub.
6. **Project credit for bought goods.** Goods bought from Houses or the hub count 25% toward project credit. Their pay
   stays 1.3, which is below what they cost (1.4 or 2.5), so the project can't be used to buy credit or gold.
7. **NPCs never owe** (contracts: 9621766). Every Wish or Ask fill counts inside the NPC caps.

## 7 · The project's pay

- **The project pays 1.3, the one price, for every unit it takes,** instead of base (`Projects.cs:354`). Credit stays
  counted at base. A gift to the Water Tower then pays exactly like a gift to a friend: one number everywhere.
- **New gold:** 0.3 × the basket. On F4's crew Water Tower (about 93k at base) that is about 28k, once.
- **Buying from a House at 1.4 to deliver at 1.3 loses money,** and so does importing at 2.5, so nothing can be farmed.
- **The Works stays an option for Era 2+ stages,** with the friends' condition: no feeder share above the one price.

## 8 · What changes in the files

| File | Change |
| --- | --- |
| `progression.yaml` `together` (52–66) | `playerPrice: 1.3`; `houseBuy: 1.15`; `houseSell: 2.0` stays for energy and iron; new `houseSellLand: 1.4` and `houseLandCap: { buildings: 2, perLevels: 10 }`; new `projectPay: player` |
| `buildings.yaml` | `lumberCamp.placement: { deposit: wood }` (the forest modifier stays) |
| `WorldGenerator.cs` | The deal per station (replaces 158–178); first good in the start area, second and third in `ParcelOrder[0]` and `[1]`; small extras only for the seat's own goods (346–359); no beach without sand (119); `wood` markers on old forest; energy traits per corner station; `EstateProfile` gets `LandGoods` |
| `GameWorld.Survey.cs` | `StarterDeposits = []` for new worlds; the pond stays |
| `GameWorld.Tenants.cs` | Kit filter "inputs only energy", land producer first, above-level allowed (382–389, 448–452); Riverside plan → `sandDredger` |
| `GameWorld.Yards.cs` | `HouseSellPrice` by good (land → 1.4 under the cap, uncapped for a held seat's House); the no-loss pause for NPC-only supply (164–178) |
| `GameWorld.Asks.cs` | Storage-bound units at `HubBuyPrice(seller)` (246–251) |
| `GameWorld.StandIns.cs` | `StandInOldPriceDays = 10` and the device check (36); House terms otherwise |
| `GameWorld.Projects.cs` | Pay `PlayerPrice` for what it takes (354) |
| `GameWorld.Projects.cs` `HomeGoods` (156–184) | Read `LandGoods` instead of the specialty mapping |
| Era-2+ recipes, a content bug found in round B | Machine Shop (−90), Assembly Plant (−1,600) and Robot Works (−200) lose value at base. Rule: every recipe's value added at base ≥ 25% of its input value. For example, the Machine Shop makes 5 gears instead of 3 (+130) |

**Effort:** about 2–3 days for the land, kits, prices and guards, plus the light-bot run.

## 9 · Worked numbers

### Capital NW (Ana, Ben, Cleo, Dev): a deal consistent with the run

| Seat | Land goods (★) | Trait | Kit | Day 1 |
| --- | --- | --- | --- | --- |
| Ana · Old Forest + Stone Quarry | wood (start area), stone ★4/★3 (parcel L5) | windy | Lumber Camp + 3 moilies (on hills, wind ×1.35 spots) | ≈ 101–125 g/min |
| Ben · Riverside | sand (a lake beside his HQ: *"i have sand on my riverside seat"* becomes true) | — | Sand Dredger + 3 moilies | 100 |
| Cleo · Copper Gulch | copper ★5/★4 | sunny | Copper Mine + 2 moilies | 104 |
| Dev · Coal Valley (held by Coal Co ⚙ until day 8) | coal ★5/★4 | — | Coal Pit + 2 moilies, kept warm by the House | 100 |

**What each of them can build, and from whom, at 1.3:**

| Who, when | Chain | Supplier | Buyer's margin | Seller's gain |
| --- | --- | --- | --- | --- |
| Ana, Level 7 (day 4 in the run) | Glassworks | Ben's sand | +21.6 | Ben +5.4 |
| Ana, Level 6 | Smelter | Cleo's copper + Coal Co ⚙'s coal at 1.4 (until day 8) | +10.5 | Cleo +10.5 |
| Ana, after day 8 | Smelter | Cleo's copper + Dev's coal | +11.5 | Cleo +10.5, Dev +3 |
| Cleo | Smelter | own copper + Dev's coal | +22 | Dev +3 |
| Dev | Smelter | Cleo's copper + own coal | +14.5 | Cleo +10.5 |
| Dev | Brickworks | Ana's stone + own coal | +8 | Ana +4.5 |
| Ben | Brickworks | Ana's stone + Dev's coal | +4.25 | Ana +4.5, Dev +3.75 |
| Ana | Sawmill | own wood | +7 | — |

**Ana in week 1–2.** A Glassworks and a Smelter add ≈ +33 g/min at level 1, about **+16% of her real 200 g/min**. Her
suppliers earn +16 between them. Nobody in the station can replace those inputs from their own land: there is no beach
on Ana's estate and no copper patch.

**Ben stops on day 3**, with 3 play days, fewer than 10:
- **Days 3–30:** he rests at 100%, and his dredger keeps sending sand. F1 pays him for what Ana takes: +5.4 g/min on
  his return card.
- **Day 23:** the notice.
- **Day 30:** his seat is held, so the stand-in sells his sand at 1.4, uncapped.
- **For Ana:** her Glassworks goes from +21.6 to +19.8 g/min, a price step of +7.7% (1.3 → 1.4) with 7 days' notice.
  It never stalls.
- **Day 36:** his line comes back at 1.3.

**Dev arrives on day 8.** Coal Co ⚙'s kit Coal Pit becomes his, and its buyers' lines become his (wild's fix). Ana's
and Cleo's coal falls from 1.4 to 1.3: 0.25 g a unit, 1 g/min per Smelter. That is small on purpose: the House is the floor. Dev
matters because he is the station's only coal by name, and because his supply has no cap.

**In gold, honestly:** a friend beats a House by 0.1 × base a unit and by volume above the cap. The need comes from the
land (nobody can make the good themselves), not from the friend's discount. That keeps the solo floor (§5) and puts the
friend's weight in the name and the Send, which is the other specs' job.

### Mia's station

**Case 1: a small station alone.** Capital N, deal (3, 2):
- Mia · Coal Valley: coal ★5/★4, sand, wood.
- Bertha ⚙: copper and stone.

Her kit is a Coal Pit + 2 moilies, 100 g/min on day 1 (run 1: 78).

| Her chain | Supply | Margin | A crewed player's margin |
| --- | --- | --- | --- |
| Glassworks | her own sand | +27 | +27 |
| Sawmill | her own wood | +7 | +7 |
| Smelter | own coal + Bertha's copper at 1.4 | +11 | +14.5 from a copper friend |
| Brickworks | own coal + Bertha's stone at 1.4 | +6.5 | +8 |

Her cap at Level 12 is 3 buildings' need per good. Her estate in run 1 had 7 buildings, so it never binds.

**Parity, worked for an equal estate of 10 buildings on day 14:**
- Base production about 150 g/min, plus a Smelter and a Brickworks at level 5 (×1.46).
- **Crewed:** 150 + (14.5 + 8) × 1.46 + 0.3 × 25% × 150 = 150 + 32.9 + 11.3 = **194 g/min**.
- **Solo:** 150 + (11 + 6.5) × 1.46 + 0.15 × 25% × 150 = 150 + 25.6 + 5.6 = **181 g/min**.
- **93%.** The bar is ≥ 85%.

**Case 2: the porch.** Capital N joins Capital NW's Yard (lonely). Her coal and sand now double Dev's and Ben's in the
big station:
- **Before day 8,** while Dev's seat is held, her coal is cheaper than Coal Co ⚙'s for Ana's and Cleo's Smelters (1.3
  against 1.4), so the matcher takes hers first: about 4–8 coal/min, +3–6 g/min for her.
- **After day 30,** her sand feeds Ana's second Glassworks while Ben sleeps.

Mia trades with people by the first week. Her share t rises from 0 to about 5–10% of her output.

## 10 · The light-bot run that confirms it

**New profiles in `BalanceSimulation.cs`:**

| Profile | Pattern | Cap | Advice |
| --- | --- | --- | --- |
| `light` | 20 minutes at 20:00 every second day, 5 minutes at 08:00 on the days between, close to the run's 2–7-day cadence | 15 buildings | Follows the advisor (after F3), which now offers chains fed by a neighbour or a House (F6, natural with no copies) |
| `light-crew` | four `light` bots in one station with Capital NW's deal: Ben stops after day 3, Dev starts on day 8, Cleo uses the `typical` pattern with a 15-building cap | | |
| `light-solo` | one `light` bot in a station of Houses (Mia's case 1), and one on the porch (case 2) | | |

Run each at 1.3 and at 1.5, with 1.75 as the baseline.

**Pass marks at the chosen price:**

| # | Mark | Pass |
| --- | --- | --- |
| 1 | Trade share between humans, paid | ≥ 15% of the crew's output value by day 14 (expected about 0 at 1.75, under 10% at 1.5, 15–25% at 1.3) |
| 2 | No building runs at a loss on its supply | 0 minutes |
| 3 | Solo against the crew, per building, paid | 85–95% on days 7, 14, 21 and 28 |
| 4 | Pacing | `typical` solo unchanged (Level 10 on day 2, Level 30 on day 25 ± 1); `light` bots reach Level 10 within days 4–6 |
| 5 | New gold from NPCs | ≤ 6% of all faucets (bound 3.75%) |
| 6 | Ben's stop | Ana's chains get ≥ 99% of their need every hour to day 36; her largest price step is ≤ +8% |
| 7 | Conservation | Sellers' receipts = buyers' payments + project pay + hub floor, every sample |
| 8 | Funnel probes | An alt's storage Ask moves 0 premium; a same-device trade moves 0; a 9-day quitter gets House terms |
| 9 | Copies gone | Over every seat of 3 seeds, no estate can place a producer of a land good it wasn't dealt |

If mark 1 fails at 1.3 while marks 2–9 pass, the cause isn't the price. Then the advisor or the act (Send) is the
problem, and the other specs answer.

## 11 · What would change it

- **The light crew at 1.3 trades under 15% by day 14:** check the advisor's chain steps first, then lower the price to
  1.25. Houses then buy at 1.1 and sell at 1.35.
- **Solo parity falls under 85% by day 28:** raise the House land cap to 3 + ⌊level ÷ 10⌋.
- **Run 2's diaries read a friend's absence as a loss** even though the House keeps two buildings running: lower the
  House land price to 1.35. The need stays, and the friend's discount shrinks to 0.05.
- **`TheFirstDay` reaches Level 10 later than day 2:** give non-energy kits a third moily.
