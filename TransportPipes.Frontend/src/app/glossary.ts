/**
 * The game's words, each explained from scratch for someone who plays for the first time (the owner, 2026-10-31: "go
 * through all other things if we're missing similar things elsewhere"): shown in a popup wherever the word is
 * underlined (components/Term.tsx)
 */
export const GLOSSARY = {
  hubOrder: {
    title: "Hub Order",
    text: "How much of a good a minute the town's market (the hub) buys from you at the full price, through your Trading Post. Sell more, and it pays less for all of it: the full price × √(Hub Order ÷ what you sell), so 4× the Hub Order brings 2× the gold. Every level of your Trading Post makes every Hub Order 10% bigger.",
  },
  yard: {
    title: "The Yard",
    text: "The square your plot shares with its neighbours. Your buildings trade there once a pipe joins them to your Yard gate (the yellow posts). At every Yard round your spare goes to neighbours who need it, and theirs comes to you. What nobody needs goes on to your Trading Post.",
  },
  yardRound: {
    title: "Yard round",
    text: "The moment the Yard moves goods between neighbours and along links. It comes about once a minute (up to 16 minutes apart when nothing changes). A new trade is set up at one round. Its goods come at the next.",
  },
  link: {
    title: "Link",
    text: "A pipe from your Yard gate to one company at another Yard, for one good, at the seller's price. It takes what the buyer's buildings lack, as far as its pipe carries it; a longer pipe loses more on the way.",
  },
  tank: {
    title: "Tank",
    text: "A store at the buyer's Yard gate that keeps two hours of what a link brings. It fills from what the seller has spare, and runs the buyer's buildings when the seller sends less; what is in it is paid when it is used.",
  },
  spare: {
    title: "Spare",
    text: "What your buildings make that none of them use: it goes to neighbours through the Yard, into a Storage, or on to your Trading Post to be sold.",
  },
  short: {
    title: "Short",
    text: "What your buildings need and don't get: they work slower. The Yard and links bring a building only what it can use, so a building that also lacks another good gets none.",
  },
  spot: {
    title: "This spot ×",
    text: "How well the place suits the building: wind for a Wind Moily, sun for a Solar Panel, a deposit's stars for a mine, the forest around a Lumber Camp. ×1.15 makes 15% more than an average spot.",
  },
  tier: {
    title: "Level tiers",
    text: "Every level makes a building 10% more, up to level 25. Every fifth level earns a tier: Upgraded, Fine (10), Superior, Epic, Legendary and Mythic (25), with its colour on the badge.",
  },
  levelGoods: {
    title: "Levels from 11",
    text: "From level 11 a level takes goods as well as gold. The gold goes at once; the goods come by themselves (your Storages, your own spare, your Yard neighbours', and what nobody has the market brings at 2.5× its price), and the building works at its old level until they're in.",
  },
  pump: {
    title: "Pump Station",
    text: "Pushes a network's goods further: a long network loses less with pumps on it. The network's panel says how much they lift it.",
  },
  hub: {
    title: "The hub",
    text: "Your town's market. It buys your goods through your Trading Post (at the full price up to each good's Hub Order) and sells goods at 2.5× their price.",
  },
} as const;

export type GlossaryWord = keyof typeof GLOSSARY;
