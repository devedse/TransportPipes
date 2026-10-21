# Rethinking trade between estates (proposal)

Status: a proposal to decide on. The quick fixes of October 21 (a trade only when it beats the hub, choosing
partners, gates open or closed) make today's model fair; this is about whether to change the model itself.

## The problem

A network is every pipe and building that touch, whoever owns them. That joins far more than anyone means to:

- **Tenants pipe all four of their gates** (`GameWorld.Tenants`), so every tenant estate bridges the corridors
  on all its sides.
- **Corridor pipes join whatever they touch**: one pipe from your gate to a neighbour's makes you part of
  everything that neighbour is connected to.
- So with four players and the tenants, a district becomes **one network**: in one tester's game 68 buildings and
  1,050 pipe tiles.

What that does:

| Effect | Why it hurts |
| --- | --- |
| You trade with companies you never chose, far away | Until Oct 21 automatically, at a loss over long pipes |
| "Connected" means nothing | Everyone is, through someone |
| Gates make little sense | One open gate connects you to the whole district |
| A district-sized network shares one solution | Pump stations, a full pipe or a shortage somewhere touches everyone |
| Others' goods flow through your pipes | Your pipes' capacity is used by trades between others |
| Big networks are slow to solve | Every change anywhere re-solves the whole district |

## Goals

1. Trade is a choice: you know who you trade with and why.
2. Neighbours matter: the companies next to you are your natural partners, far ones aren't.
3. Your pipes are yours: your network is your estate, others' flows don't run through it.
4. You can see it on the map: a trade route is a visible pipe with things flowing.
5. Simple enough for an idle game, and fast to solve.

## Options

### A. Estates are islands, neighbours are linked (recommended)

- **No network crosses a gate.** The pipes in an estate make that estate's networks, nothing more.
- **A trade link** is a pipe in the corridor from one of your gates to a neighbour's gate. It joins exactly two
  companies and is **not transitive**: you linked to B, and B linked to C, doesn't link you to C.
- **What a link does:**
  - Capacity comes from its pipe tier; its loss comes from its length (the corridor part only, so a direct
    neighbour is close).
  - Each side decides what crosses it: the trade settings per partner and resource, as they are now.
  - The receiving side must accept the link; tenants accept by themselves.
- **Under the hood:** it works like the teleporters do already (`GameWorld.Teleporters`). The link is a buyer
  in the seller's network and a seller in the buyer's network, filled from the last solve. The networks stay
  small and are solved on their own.
- **On the map:** links are the corridor pipes with pellets flowing; a link's panel shows what crosses it, the
  price and the loss.
- **Gates finally make sense:** a gate is where links attach; a closed gate has none.

| Good | Cost |
| --- | --- |
| Explicit partners; distance and neighbours matter | The network flood must stop at gates (`GameWorld.Networks`) |
| Small networks: fast, pumps and shortages stay yours | Links become two-ended entities with their own panel |
| Fits the name: pipes are how you trade | Migration (below); tenants offer links instead of piping all gates |

Effort: about 1–2 days with tests and the e2e checks.

### B. Market-only trade between estates

Pipes stay inside estates. Companies trade through the hub's order book, contracts and cargo trucks only.

- Good: simplest and fastest.
- Bad: it loses the fantasy of piping to your neighbour, which is the heart of TransportPipes.

### C. Keep merged networks, add routing rules

Keep today's model and make it fairer: partner choice (done), selling only when it beats the hub (done), maybe
distance-based prices.

- Good: smallest change.
- Bad: the mega-network, shared bottlenecks, others' flows in your pipes and the slow solves all stay.

### D. A district grid owned by the hub

Each corridor carries one hub-owned main line. Companies attach at their gate through a meter station. What you
feed in goes to buyers on the grid or to the hub at a district price, with a fixed loss.

- Good: easy to understand, like an electricity grid.
- Bad: partners become anonymous, neighbours stop mattering, and so does laying pipes in corridors.

## Recommendation: A

It meets all five goals and builds on what already works: the teleporters' cross-network mechanics, and the
trade settings per partner and per resource.

**Migration for running worlds:**
- A corridor pipe chain from one gate to another gate becomes a link.
- Other corridor pipes (dead ends, chains that join three or more gates) are refunded.
- Tenants get link offers on all sides instead of pipes to every gate.

**Steps:**
1. Stop the network flood at gates; links as entities.
2. Solve links teleporter-style.
3. The link panel and map look.
4. The trade settings show links instead of "neighbours on your pipes".
5. Loady's neighbour step becomes "link to a neighbour".
6. Migration, tests (game, the e2e consistency check), balance run.

## Questions to decide

1. **Transit:** may a link pass through a third estate's gate if that company allows it (for a toll)? Or only
   gate to gate across one corridor?
2. **Corporations:** should a corporation's members get a shared "corporation grid" (their links join into one
   network), as a perk?
3. **Cost:** does a link cost anything beyond its pipes (a toll per unit, a weekly fee), or is the loss enough?
4. **Accepting:** must players accept links, or is linking to an open gate enough (with the partner switch to
   stop trading)?
