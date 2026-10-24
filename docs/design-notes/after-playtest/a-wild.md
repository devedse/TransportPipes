# Round A · wild

I was the wild idea thrower and the champion of concept B (Streets & Lines). B lost. Its "soul" went into v1.2: the
crew's stripes on a pipe that grows lamps and arches, "usual buyers first", and damping.

I read the brief, all of `findings.json`, every diary and the group chat in `digest.md`, the director's notes, the fix
plan and its record (engineer r1, economy r1 and r3, researcher r1 and r2), the v1.2 status and refinements, and the
code behind each claim.

**My verdict in one paragraph.** The playtest doesn't ask us to throw out stations, crews, Houses or the absence rules:
those held. It asks us to replace our idea of *what friends do together*. We designed playing together as goods
flowing by themselves between neighbours at one price. The five players showed two things:
- friends want to **do things for each other, by name**, and to **run one chain together**;
- in week 1 they had **nothing the others' buildings needed**.

The fix plan makes the automatic market honest and visible, and that is necessary. It doesn't give the friends a
reason or a verb, and it parks both levers in its "what would change the plan" list. The owner's instinct is right.

---

## What held, what failed

Classes: **(a)** built differently from the design · **(b)** built as designed, and the design was wrong ·
**(c)** not reached or not tested.

### What held

| Promise | Evidence |
| --- | --- |
| **The way in:** seat codes, two taps, held seats warmed by a House, kits already piped, the Handshake | Ben and Cleo moved in on the first evening (ben d01-2000 #11–#13, cleo #9–#12). Dev opened a day-1 code on day 8 with nobody online (dev d08-2000 #3–#8). The Handshakes were *"the one moment that felt like being neighbours"* (findings, "together") |
| **A return without shame** | Ben: 42 seconds, *"a month away and nothing lost"* (ben d36-2000 #72–#79) |
| **One price, no votes, labelled Houses** | No finding against them. No drama, no confusion about NPCs. Griefing wasn't tested |
| **The look** | Every player praised it |

### What failed

**1. "Your spare goes to the neighbours whose buildings need it" (§7): (a), plus a bug.**
- §7 says *"a seller is paid for what reaches the Yard"*. The code pays only for what the buyer's buildings take
  (`GameWorld.Yards.cs` 209–218).
- `Measure` counts every tied input as lacking at full need (Yards.cs 363–365, engineer r1).
- The cost: 3.5M gold lost in eight days, and no Yard trade paid a player in 36 days (economy r1 §1).

**2. "Friends trade what their land is rich in" (§2, §14): (b). This is the root.**
- Every starter kit makes energy. Kits are *"producers in the start area that need nothing to run"*
  (`GameWorld.Tenants.cs` 371–372; refinement of 2026-10-21).
- Our start-area decision put each seat's rich goods behind the Level-5 and Level-7 parcels (§14). That was my own
  r3-B rule, adopted in the merge.
- So on Dev's day-21 Yard screen, all four residents' *Spare* is energy, plus a little ore and ingots of Cleo's
  (dev d21-1900-0191). An honest Yard has nothing to match.
- Ben's seat card promised *"Riverside: water and sand, wood"*, and he *"only found stone here"* (group chat, day 3).
  Sand appears in 24 of 37 group messages (researcher r1).

**3. "The crew decides who brings the keystone" (§5, minute 20): (b).**
- The decision lived in Station › Project. It was first opened on day 21 (cleo d21-1900 #302).
- The Plan board got 0 pins.
- The recipe comes from the seats' land (`GameWorld.Projects.cs` 156–184), not from what anyone makes.

**4. "The project stands at minute 20, and moves" (§12): (a) and (b).**
- (a) The 18-hour tail is a bug (economy r1 §3).
- (b) The basket of 6 h × T(L) was our own rule: 28 hours of Ana's income, in goods nobody made.
- (b) Worse, the project *"pays the hub's full price (1.0 × base)"* (§12). Delivering to it earns nothing more than
  the hub pays. Its only pull is a +3% that Cleo priced at *"~24/min"* (cleo d21-1900 diary).

**5. Giving: 0 Asks, 0 links, 0 tends in 36 days: (b).**
- A link between station mates is refused by design: *"{partner} lives in your station: you trade through the Yard
  already"* (`GameWorld.Links.cs` 313–315). That is exactly where Ana looked for Cleo: *"Links tab only lists Mia
  Studio as partner, not Cleo … So how do I get Cleo's iron ingots?"* (ana d27-2000 #258).
- Tending *"carries no stat bonus"* (§12), so nobody had a reason to look for it.
- Asks sat in a tab nobody opened.

**6. "Mia sits beside a human" (§11, §17b): (a), then (b) and (c).**
- (a) The seat picker looped (mia d01-2000 #10–#22).
- (b) Two of our rules collide in a small world. A crew holding 3 or more seats closes its station
  (`GameWorld.Crews.cs` 81, 447), and the only human station was the crew's.
- Mia landed in Capital N, the small station right next door, across one corridor (`WorldLayout.cs` StationBlocks:
  N is block (2,0)–(2,1), NW is (0,0)–(1,1)). For 36 days she had no way to share anything with them. A link needs an
  offer and a matching need, and both sides made energy. Their Links tab listed her as their only partner, and nobody
  offered (findings, "together").
- (c) Solo parity couldn't be measured. Nobody earned anything from anybody, so Mia's 95% of Dev per building
  (economy r1 §7) means nothing.

**7. "Ana learns of Ben's stand-in" (§9): (a) and (b).** The design never said where the notice goes. It went to a
bell with 93 unread (ana d27-2000 #247–#253).

**8. The return in three screens: (a).** Two components were never merged (engineer r1).

**9. The middle game (Foundry Hall, trunks, Discoveries, +15% in week 3): (c).** Not reached.

**10. Dead ends: not a v1.2 rule, but it ate the crew.**
- "Goods don't pass through buildings" is v1's core rule, and the screens gave the wrong reason
  (`GameWorld.Advisor.cs` 608, engineer r1).
- It mattered for us because it became the friends' *only* shared activity: their longest conversation, and
  advice they couldn't act on (group chat, days 6–21).

### The pattern

We designed the shared space to be **safe** (nothing stalls, nobody is guilty, no funnel, no vote, solo complete) and
**automatic** (the matcher, the project buying at the hub's price, kits that come piped). It reached safety completely
and togetherness not at all.

Look at the vetoes one by one. Each removed a reason for friends to need each other:
- kits need nothing;
- the project pays what the hub pays;
- links between station mates are banned;
- tending does nothing;
- gifts are forbidden.

The friends brought their own reason in minute 2: *"I'll start with the energy, can someone bring sand? 😊"* (Ana,
group chat). That is a Glassworks split over two estates. The game had no place for it.

My own B would have failed the same way: its Main was also an automatic pool. The instinct the evidence now supports
was my round-1 "Well", a shared machine four seats feed. The merge turned it into a basket.

---

## Is the fix plan enough?

**Where it repairs a cause, and I'd ship it unchanged:**
- F1, the Yard never worse than the hub, and lacks only what a building can use;
- F2, the dead ends (5 of 9 real STUCK notes);
- F3, F5 (one return card), F7 (the seat picker), F8 (smart Max), F10, F11;
- the replays as gates.

These are bugs and clarity. Any design needs them.

**Where it patches the symptom of a design flaw:**

1. **Nothing to give.**
   - The plan itself says *"An honest Yard trades little between kits that all make energy"* (§1.3). The engineer
     warned that *"with true lacks the Yard will trade less"* (r1).
   - F6's answer is the advisor offering factories whose input a neighbour has spare. At 1.75 that offers *"almost
     nothing in Era 1 except the Glassworks"*: Small Factory +1 g/min, Smelter −1.25 (economy r3 dissent).
   - The real lever, *"seat kits get one consumer of a neighbour's specialty"*, waits in §7 until run 2 fails. Run 1
     already failed on it.
2. **No verb.**
   - F6 names both ends *after* an automatic flow. It explicitly drops *"a 'send to a friend' button"* (§4,
     Dropped) and keeps the in-station link ban.
   - *"Giving needs an act"* is also deferred to after run 2 (§7).
   - Yet all four friends tried to give by name:
     - Dev three times (*"How do I actually send sand to you? Station?"*, dev d10-2000 #103);
     - Cleo (*"Sending copper ingots will need a station"*, group chat, day 6);
     - Ana (ana d27-2000 #258);
     - Ben four promises.
3. **The project.**
   - F4 makes the basket reachable: 900 energy, no tail, recipes from what residents make. That is good.
   - It keeps the shape: a one-off sink at the hub's price, a % bonus Cleo can't price, and a page of constants
     (horizon per stage, keystone eligibility, 4/6/9 h, a 1.5× cap, a shrink after 3 days).
   - It's the right size of the wrong thing. The researcher saw it: *"The project's pull is the crew and the building
     on the map … not its bonus"* (r2).
4. **Mia.** F7 fixes the loop. Run 2 adds Jo, a scripted human "who never chats": a test fixture, not a design
   answer. In a small world a crew still closes the only human station, and the station next door still can't share
   anything.
5. **The price.** It is left to the bots, but it decides whether any cross-estate chain exists. At 1.75 *"a solo
   player would then earn about 106% of a crewed one"* (fix plan §5). That isn't tuning. It means "together makes you
   better" is false in Era 1.

**So:** enough to make v1.2 honest and readable; not enough to make it played together. The plan's own pass mark,
*"first good knowingly given by day 2 20:00"*, depends on levers it hasn't pulled.

---

## What I'd change: "The Works runs, and every gift has a name"

**The idea.** The station's project stops being a basket and becomes the **Works**: a small factory the station owns
and its residents feed by pipe. It runs while everyone sleeps, and it pays its feeders the chain's margin.
- Seats make what they promise from evening one, so the Works has suppliers in minute 12.
- Giving gets three verbs with a name on them: **Send to…**, **a hand on a friend's land**, and **research for the
  crew**.
- An estate across one corridor counts as a neighbour.

### 1. The Works runs (replaces the project's baskets and bonuses)

**What players do and see:**
- The Foundation is F4's 900 energy. When it stands (about 20:12), the Works' first **machine** goes up in the Yard
  corner. It is a real building from `buildings.yaml`, using two goods the station's kits make, for example a
  Glassworks (25 energy + 15 sand → 5 glass).
- The Works is a buyer in the Yard, as the project is today. Its machine runs on whatever arrives, and its output
  sells to the hub under the station's own Hub Order.
- On every producer's panel (F2's slot): *"The Works buys your sand · pipe it to your Yard gate (G)"*, with F2's
  one button.
- In the Yard: the machine turning, a ring of colours, and one line: *"The Works made 1,240 glass today: Ana's energy
  61%, Ben's sand 39%"*.

**The money rule: the Works shares its margin, and makes no new gold.** Each minute:
- The machine's sales R are compared with its inputs at hub value H. The value it adds is VA = R − H.
- Each feeder gets its input at hub value **plus 80% of VA**, split by input value.
- The other 20% builds the Works' next machine.

Worked at level 1:

| Machine | Value it adds | What a feeder gets |
| --- | --- | --- |
| Glassworks | VA = 70 − 43 = 27 | 1.50 × base |
| Small Factory | VA = 84 − 56 = 28 | 1.40 × base |
| Smelter | VA = 90 − 65 = 25 | 1.31 × base |
| Brickworks | VA = 50 − 37.5 = 12.5 | 1.27 × base |

Every one is above a House's 1.25 and below the hub's 2.5. The gold is exactly what a player's own factory would earn,
so an alt feeding a Works gets its own share and moves nothing. The Works runs only machines whose VA is positive.

**More rules:**
- **Next machines** (Brickworks, Smelter, Steel Mill…) come from the 20% plus gold that anyone in the station puts
  in, for construction only. No goods are bought with it, and no gold goes to a player. It is the sink Ana asked for:
  *"Gold piles up (6.2M) with nothing to spend it on that helps my friends"* (ana d27-2000).
- **It never stalls.** When an input hasn't arrived for 24 h, a House sells it at 2.0 within the 25% cap. The machine
  runs slower and never stops, and nobody's own income depends on it. A friend who rests keeps feeding it at 100%.

**Why (evidence):**
- Ana's only "together 4": *"the Project tab shows the Water Tower wants sand … Dev's sand has a purpose"* (ana
  d27-2000 #262).
- Cleo: *"Helping costs me nothing but I still see no payoff for helping"* (cleo d10-2000). Now her share is gold she
  can see.
- Ana's first message *is* this machine.

### 2. Seats keep their promise (replaces kits that "need nothing")

- A House's kit includes the seat's own good, at most level 5, piped to the gate:
  - Riverside: a Sand Dredger on its start-area shore (the surveyors already put a pond there, §14);
  - Coal Valley: a Coal Pit;
  - Copper Gulch: a Copper Mine;
  - the stone seat: a Quarry;
  - Windy Heights and Sunny Flats: energy, as now.
- Kit buildings may be above the player's level, as gifts are.
- A station's seats are generated so the Works' first two machines use goods the kits make.
- The seat card says it plainly: *"Riverside · your kit makes sand for the Works"*. Ben's broken promise drove 36
  days of *"sorry ana still no sand"*.

### 3. Every gift has a name (replaces the in-station link ban and the do-nothing tend)

- **Send to…** On a producer's panel: *"Send sand to: the Works · Ana's Glassworks · Mia next door"*.
  - It plans a short link from your gate to theirs through the Yard. Links exist; drop the refusal at Links.cs 313–315
    for station mates and for estates across one corridor.
  - It is accepted by itself when it only replaces what the receiver buys from the hub or a House, and the receiver
    can pause it at any time.
  - It is drawn in your colour, and the first unit is announced to both of you (F6's event).
  - It trades at the one player price. I'd pick 1.5 now from the economy's own table: every Era-1 chain except the
    Sawmill pays the buyer at 1.5. The bots confirm parity.
- **A hand on a friend's land.**
  - A crew mate can draw a plan (the existing blueprint ghost) on your estate. You build it with one tap, at your own
    cost.
  - A plan of pipes only, worth 100 g or less, the helper may lay at their own cost. This needs "crew hands", which is
    on in a crew and never for strangers, and can be undone in one tap for 24 h.
  - Then the screen says: *"Dev laid the pipe that got your factory running: +28 g/min"*.
  - Dev had the right lesson on day 8 (*"Nothing on its pipes takes its energy"*, group chat) and the full fix on day
    21 (*"its own moily AND a Storage"*). Ana's factory stayed idle to day 36.
- **Research for the crew.** A tech any crew mate has finished costs you 75% less (today's diffusion is at most 50%,
  `GameWorld.Research.cs` 11). Ana asked *"Could you do the research for me, or can points be sent?"* (group chat,
  day 21). No points move.
- **Tend** stays as F9: it repairs storm damage. That makes it a hand too.

### 4. Next door counts (Mia)

- Estates facing each other across one corridor can use Send to on each other, and on each other's Works, with no
  offer, under the same "only replaces an import" rule.
- In small mode, "Find me neighbours" seats a stranger in the station *next to* a crew whose station is closed, and
  says so: *"Tea Time Pipes lives next door: send to their Works, and they to yours"*. F7 suggests a different
  station; I'd make it the adjacent one.

### What it replaces

| In v1.2 | In the fix plan | Replaced by |
| --- | --- | --- |
| Project baskets, credit allowances, keystone, +3/+3/+4/+10% bonuses | F4's sizing rules (made goods 4/6/9 h, horizon, 1.5× cap, shrink) | The Works and its 80/20 margin rule (F4's 900-energy Foundation and "no tail" stay) |
| Kits of "producers that need nothing" | Kit consumers as a run-2 trigger (§7) | Kits that make the seat's good, now |
| Links refused inside a station (Links.cs 313–315) | "Send to a friend" dropped | Send to… for station mates and next door |
| Tending as a counter | F9 (tend repairs) | Kept, as one of the hands |
| — | "Giving needs an act" as a run-2 trigger | The hands and Send to, now |
| The 1.75 price | The bots decide | 1.5, the bots confirm |

The Yard matcher stays, made honest by F1, for spare nobody sent anywhere and for the Houses. It stops being the
place where "playing together" is supposed to happen.

### The five, played through

| Player | Run 1 | With this design |
| --- | --- | --- |
| **Ana** | Asked for sand from minute 2; her factory sat idle from day 4 to 36; she found the project on day 27 | Day 1 20:12: the Works' Glassworks takes her energy and Ben's kit sand. *"Ana's energy + Ben's sand made the first glass"* is on both screens: her plan, run by the game. Day 4: F2's button, or on day 8 Dev's plan on her land, one tap. Day 21–27: she puts 2M gold into the Works' Brickworks, the sink that helps her friends |
| **Ben** | Promised sand four times; *"only found stone"*; stopped on day 3 | His kit dredger is piped. His first screen says *"Your sand made the Works' first glass"*: the quick win and *"not let ana down"*, with zero taps. Days 3–30 resting, the dredger feeds the Works at 100%. Day 30 a stand-in. Day 36 the return card: *"Your sand kept the Works' Glassworks running while you were away"* |
| **Cleo** | No way to send copper; her Lab drained three friends; points useless | Her copper ore feeds the Works' Smelter, with her share in g/min. Send to Ana's Lab for ingots. Her Copper Pipes research is 75% cheaper for Ana. F1 makes the Lab drain impossible |
| **Dev** | *"How do I actually send sand to you?"*; useful to nobody | Day 8: the Works wants energy from his kit moilies at once, with Tailwind. Day 10: his dredger's panel says *"The Works buys your sand · pipe it to your Yard gate"*, F2's button lays it, and his sand reaches the Works in the session |
| **Mia** | Picker loop; a station of Houses next door to the crew; never traded with a person | F7 seats her next door with an honest line. When the crew's Works wants her seat's good, it shows *"Mia next door makes stone"*. Her Send to puts her colour in their ring, and their 💛 Thanks reaches her return card (F5). Her own small station's Works runs on her goods and a House's |

### Cost and risks

**Cost** (each step playable, on top of F0–F3, F5 and F7–F11):

| Step | Effort | Why it's that size |
| --- | --- | --- |
| The Works as a station-owned building | M–L (3–4 days) | The project is already a Yard buyer; machines reuse building recipes and sprites in the Yard corner (2×2 fits the 4×4 corner; fit.mjs); a margin ledger |
| Truthful kits | S–M (1–2 days) | The kit already picks producers; shores already exist |
| Send to | M (2 days) | The links engine exists; the station check comes off; the auto-accept rule; the panel button |
| Hands | M (2 days) | Blueprint ghosts exist; permission, undo |
| Crew research | S | A diffusion constant |
| The price | S | Plus balance runs |

That is about 9–11 days, and it replaces F4 (L) and most of F6 (M+M): roughly **5–7 days more than the fix plan**.

**Risks:**

| Risk | Answer |
| --- | --- |
| **The Works as a faucet** | It can only pay out the VA its machine really makes. The economy must check every machine's VA at level 1–5 |
| **Dependency** | The Works is extra income. Houses fill its inputs after 24 h, and nobody's own buildings depend on it |
| **Guilt** | The ring shows who helped, never who didn't. A friend who rests still feeds it |
| **Alts** | Send to at 1.5 caps a funnel at 0.5 × base per produced unit, lower than today. Helper pipes are capped at 100 g a day |
| **An automatic kit gift may not feel like giving** (Ben) | For a skimmer the outcome matters more than the tap. For Dev and Cleo, Send to is the tap |
| **Screen space** | Nothing new to find: the Works replaces the project card, and Send to lives on the building panel |
| **Words** | "Works" replaces "project"; "Send" is a verb. The count stays at five |
| **Performance** | One building per station in its own network; station links stay under 12 per estate |

---

## What I'd keep no matter what

- **The way in:** seat codes, two taps, held seats warmed by Houses, kits already piped, the Handshake.
- **Absence costs nobody:** resting at 100%, the stand-in, the held seat, a one-minute return.
- **The money rules:** one price between players, Houses labelled and capped at 25%, no votes, no kicks, no gold
  between players.
- **Networks never merge across gates.** The 380 ms district solve must never come back.
- **From the fix plan:** F1 (never worse than the hub), F2 (one true reason and one button), F5 (one return card),
  F7 (the honest picker), F8 (smart Max), and the replays as gates.
- **The look.**

## Questions for the others

- **Economy:**
  - Check the Works' 80/20 rule for every Era-1 and Era-2 machine at levels 1–5.
  - Does the 20% build the next machine fast enough without gold?
  - Is 1.5 still right once Send to carries friend-to-friend trade?
- **Friends-first:** should Send to between crew mates go *before* the Yard in the flow order (a named claim on your
  spare), and does that make the matcher pointless inside a crew?
- **Lonely:** is "next door counts" plus a Thanks enough for Mia's "alive", or does she need a human who answers in
  the game?
- **Psychologist:** does a gift the giver didn't tap (Ben's kit already piped to the Works) feel like giving, once it
  is announced?
- **Outsider:** is "the Works" plus "Send to" clearer than Yard + project + Plan board + Ask? Can the Plan board go?
- **The fix panel's engineer (final round):**
  - What does a station-owned machine fed by Yard terms cost?
  - Does every Riverside start area really have a shore for a kit dredger?
  - Can links inside a station share the plaza with the matcher without double counting?
