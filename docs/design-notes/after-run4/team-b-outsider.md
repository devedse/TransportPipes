# Round B · outsider

All six of us found the same three faults:
- the doors stood where nobody looked;
- they were capped exactly where the wall was;
- they were sized in crumbs against incomes that arrive in bulk.

So the solution should be the fewest mechanisms that fix all three, in words a skimmer can read. Below is what I take,
what I drop, what I reject, and the plan I'd sign.

## 1 · What I take, and what I drop of my own

**I take:**
- **Lonely's diagnosis: income comes in bulk, spending in crumbs.** Every rare return brought days of income: Mia 2.2M
  and 7.6M, Ben 13M (mia d08 #87, d15 #119, ben d36 #77). Every buy cost 3–60k, one click at a time. So the door for a
  returner must spend a pile in one tap: **"Grow everything that pays"**, one preview, one tap, Undo. It is the genre's
  "buy max", and it is how the light bots spent: they walk every step (Cleo's bot: Iron Mine level 69).
- **Economy's single pipe door.** One button goes the whole way: research → the Copper Pipe (its ingots yours, a
  friend's, or ordered) → Max to the new limit, with one preview. Economy also found why the advisor never offered it:
  `SupplyStep` returns nothing when you already make the ingots (`Advisor.cs:515–523`, `:548`). That is a dead end in
  the very list the bots follow.
- **The psychologist's point that the wish to spend was social.**
  - The only sink that took real gold was the shared one in sight: all 30,660 g, by three of the four crew, unasked.
  - Five gold offers went into the chat. Ben: *"just tell me the button"* (group, day 36).
  - So the shared goal after the Landmark takes gold directly, and gold given for someone else is named.
- **Friends' "chip in" in hours of your own income.**
  - *"Ben · 1 h"* stands beside *"Cleo · 1 day"*, so a small giver isn't shamed.
  - It is a button that never vanishes, unlike the Wonders tab's (ben d36 #88–#94).
- **The chain pointer, offered first to the player with gold and no role** (all six, the analyst's recommendation 3).
  Dev's *"Bricks need stone + coal, I have neither"* (dev d14 #116–#117) needed one line telling him the Yard brings
  both.

**I drop, of my own:**
- **Grey Widen and the two buttons side by side.** Economy is right: once ingots can always be ordered, the Copper Pipe
  is reachable from every seat. One button that goes the whole way is clearer than a choice between two doors. Widen
  stays the long sink after copper.
- **The top bar's "one best buy".** A returner with a month's income needs a plan, not a step. Lonely's plan and
  friends' list replace it.
- **Floors at ×1.5.** The psychologist's sizing (n days of the station's income) meets the banks; mine started at 6
  hours.

## 2 · What I reject, and why

1. **Wild's Works (station-owned machines).**
   - It answers the wonder's pipe half, which the plan already answers twice: orders that fill everything when nobody
     near makes the good, and the chain pointer that makes a friend the maker.
   - It adds a noun, a kind of owner (the station) that run 4's players never met, and invisible routing rules (wants
     first, then the wonder, then the hub).
   - It costs 5–7 days.
   - If run 5 shows the pointer doesn't create makers, the Works comes next.
2. **The psychologist's "nobody more than half until it has waited 3 days".** That fences off the giver, which is the
   psychologist's own diagnosis of run 4 (*"we fenced it off"*). Credit in hours already stops a rich player from making
   the rest feel useless, and Mia beside a silent Jo would wait three days for nothing.
3. **Lonely's wonder stage at 1 day of the Capital's income.** On day 21 that is 9M of goods. Its pipe half, about 900k
   bricks' worth, could never come by pipe, so orders would fill every stage and the makers would be decoration. Keep
   6 hours.
4. **Lonely's branch, for run 5.** A second estate doubles what a 5-minute returner must read, and Ben already couldn't
   tell what one estate earned (ben d36 #82). The stranger next door and the district channel give Mia people first.
   The branch can come after run 5.
5. **Wild's 2.5×.** One number for bought goods, 2.0, already built (`WantOrders.cs:16`).
6. **Pricing goods or parcels by the buyer's income.** A brick that costs Ana more than Ben reads as unfair in a chat
   where they compare prices (psychologist, economy). Size the *goals*, not the unit prices.

## 3 · The open questions, final

| Question | My answer |
| --- | --- |
| **After the Landmark** | **Landmark stages,** one at a time, without end (the word "stage" players already know). <br>• *Gold:* n days of the station's 7-day income, fixed when the stage opens. Capital SE on day 17: about 6.1M, 12.7M, 22.8M (psychologist).<br>• *Goods:* 6 h of the station's spare in two goods its residents make, by pipe.<br>• *Perk, aimed at the bottleneck:* grey pipes +25%, then Trading Posts +25%, then Hub Orders +10%, then +2% production up to +10%.<br>• *Looks:* a visible addition to the Landmark each time, and a plaque naming givers in hours |
| **Gold in a shared goal** | **Give ▸ 1 h · 6 h · 1 day of your own income,** repeatable, on the stage's line, the card and the gold panel. On a wonder stage it orders the goods nobody makes at 2.0 × base; on a Landmark stage it fills the gold part |
| **Price and credit of bought goods** | **2.0 × base.** The plaque names every giver, of gold or goods. Gold given for a friend carries a 💛. Wonder tier credit stays 25% for ordered goods, so a maker's tier still means more |
| **Widen** | **Copper first, then Widen:** ×2 for 1 hour of the buyer's 7-day income, ×4 for 2 more. Grey is reached through the pipe door, whose ingots can always be ordered |
| **One door or one plan** | **Both, by place.** The upgrade panel at the pipe has the one door for that building. The gold panel and the return card lead with the plan for the bank |
| **The player alone** | **District channel first (in); branch (out for run 5)** |
| **Before run 5** | The light bots with the pipe door (ingots orderable), Widen, the plan and Landmark stages. **Pass:** ≤ 5 days of income banked on day 28, and `typical` reaches Level 30 on day 25 ± 2. **Stop rule:** if the bots still bank over 10 days, raise the stage sizes before people play |

## 4 · The solution I'd sign, in build order

| # | Mechanism | Numbers | Replaces | Days |
| --- | --- | --- | --- | --- |
| 1 | **The sizing fix** | Count residents seen in 7 days. Never size from the 60 g/min floor while anyone has played in 7 days. Resize upward daily until a quarter has arrived (economy). The first wonder stage waits for a played resident | The 3-day window; world-start sizing; the freeze at the first delivery | 0.5–1 |
| 2 | **The pipe door** | On the upgrade panel and in Next steps: *"Its Grey Pipe is full: 205 a minute · Copper Pipe, then Max to 30 · 165k · +N g/min ▸"*. Ingots come from you, a friend's Send, or an order at 2.0 when nobody near makes them. At copper's limit: Widen, then Max. The advisor's three dead ends (`Advisor.cs:503`, `:515–523`, `:548`) become steps | *"+N would make nothing more"*; the fix buried in the idle status | 2 |
| 3 | **Gold finishes what nobody near makes** | One rule for wants, wonder stages and Landmark stages. **Nobody near makes the good** (no resident seen in 7 days whose buildings made it in 24 h): the order covers everything, shipped the next Yard round. **Somebody does:** their window, then the 6 h flow, with half for shared goals. The button never vanishes: it says why and names the chain (*"a Brickworks on your estate makes bricks from Ana's stone and Cleo's coal · Show me"*), offered first to the player with gold and no role. A scaffold reads *"waits for 100 bricks · Order ▸"* | The half cap where no maker exists; the vanishing button; *"is being built"* | 1.5 |
| 4 | **"Your gold can"** | The top bar's gold shows *"40.6M · 24 days"*. A tap opens three lines at most: **Grow everything that pays** (Max and pipe doors whose payback is ≤ 3 days, one preview, one tap, Undo for an hour); the open shared goal with **Give ▸**; a friend's waiting want with **Order for Dev ▸**. The return card's first growth line is the first of these and never folds. Next steps rank the same | The bare gold number; the folded order; one Max at a time for returners | 2 |
| 5 | **Landmark stages** | As in §3: n days of the station's income in gold, 6 h of goods, a bottleneck perk, givers named in hours | The 10k parcel on the This week line after day 17 | 3–4, plus art |
| 6 | **The player alone heard** | Her chat opens on the district channel when nobody else at her station has written. Every gift that stands a stage is named at both ends, strangers included | Five greetings to a silent Jo (director's observations, rows 1, 5, 11); Mia-B's unnamed wood | 1–1.5 |
| 7 | **Bots before people** | §3's run and its stop rule | — | 0.5, plus the runs |

**About 10.5–12.5 days, plus the stages' art.** It adds one verb (**Give**) and one panel (the gold's), and uses
"stage" for the Landmark. It adds no new noun: no Works, ring, tier or branch.

## 5 · The mark for run 5

**From day 14 to day 36, the crew spends at least 20% of what it earns, by its own taps. If it spends under 10%, with
these doors on the panels, the card and the gold, this solution is wrong.** Under 10% would mean the doors and sizes
were not the cause, and gold needs a job other than growth and giving. Run 4: 0.2%.
