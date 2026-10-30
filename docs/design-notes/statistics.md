# Statistics: your company over time

Every player can open a Statistics panel. It shows how their company grew over hours, days and weeks, marks the key
moments on the graphs, and lists them in a journal. Until now the game only kept today's totals (the Company panel's
numbers and the leaderboards), so nobody could look back. This note covers what the game stores, how players browse it,
and what stays private.

## What is stored

**A sample every 15 minutes** of world time, at each market update, for every company (also while its player is away)
and for every town with players. The game keeps:

- every sample of the last 2 days,
- the samples on the hour for 30 days,
- the midnight sample (UTC) for good.

A year of one company is about 1,250 rows and about a megabyte. A sample holds:

| Group | Series | Kind | Who sees it |
| --- | --- | --- | --- |
| Gold | Income: gold a minute the plot makes now (the top bar's number) | value | everyone |
| | Company value: bank gold plus what buildings and land are worth (the leaderboard's Net worth) | value | everyone |
| | Gold in the bank | value | you |
| | Gold earned in all (the Company Level comes from it) | counter | everyone |
| | Earned from the market, other players, daily orders, rewards | counters | you |
| | Spent in all; on research, on land, given to the Wonder and Monuments; the rest is buildings and levels | counters | you |
| | Company Level | value | everyone |
| Production | Made, per good (units) | counters | you |
| | Sold, per good (gold from the market and players) | counters | you |
| Your plot | Buildings, their levels together, land (tiles you own), pipe tiles, gold per tile an hour | values | everyone |
| Research | Track tiers, Era tree items, Era | values | everyone |
| Together | Trading partners, likes | values | everyone |
| | Sold to players, bought from players (gold) | counters | you |
| Activity | Minutes played, by quarter hours (a stretch without any is shaded as away) | counter | you |
| Town | Its players' income together, players, Era, Wonder stages and the current stage's progress | values | its players and visitors |

A *value* is drawn as it was at each sample. A *counter* only grows, so it is drawn as a rate: the difference between
two samples, per minute, like every rate in the game. That way a missed sample (a server restart) doesn't distort the graph. A graph always ends with
a live point for now.

## Key moments

Key moments are stored with their time, a line of text and, where it fits, a good or building for the icon. Big
moments show as flags on every graph; small ones show as ticks on short ranges.

- **Big:**
  - the company founded;
  - every fifth Company Level;
  - the first unit of each good ("Your first Steel");
  - land bought (parcel, outskirts, Deep, outpost);
  - your Era;
  - a Wonder stage your town finished (with your builder tier);
  - joining or leaving a crew, or a neighbour joining yours;
  - moving town;
  - a mastered track (tier 9).
- **Small:**
  - the other levels;
  - track tiers and Era tree items;
  - achievements;
  - a new trading partner;
  - a Monument stage;
  - the town's calendar events (a storm or an Expo explains a bump in income).
- **Worked out when a graph is drawn, not stored:**
  - the stretches you were away, shaded on the graph: what your plot did on its own;
  - the peak of the range.

## The Statistics panel

- **Opening it:** the top bar's chart button (an easel with a rising line; key J), the numbers on the Company panel (each opens its graph), the
  Company Level panel, a visited plot ("Their statistics"), a leaderboard row, and the return card. The return card also
  gets a small graph of your income while you were away.
- **Range:** 24 hours · 7 days · 30 days · All. All uses a log scale, because a company grows by factors (there is a
  toggle).
- **Browsing:** cards in groups (Gold, Production, Your plot, Research, Together, Town). Each card has its number now,
  its change over the range and a small graph; a click shows it large. Arrow keys step through the cards.
- **The graph:**
  - The time axis counts your company's days ("Day 3"), the days the design's model uses; over 24 hours it shows the
    clock.
  - Hovering or tapping a point gives its value and time, and the moments near it.
  - Stretches you were away are shaded.
  - Earned by source and spent by kind are stacked areas.
- **Insights:** three or four sentences on the range, such as "Income ×3.2 in these 7 days", "41% of your sales were
  Steel", "68% of your gold came while you were away" and "Best: 240k/min earned, Tue 21:00 · day 9".
- **Journal:** the moments of the range by day, all or big only. A click points at the moment on the graph.
- **Compare:** on the series everyone sees, add up to three other players (your crew first, then your town) as lines in
  their colours.
- **Someone else's statistics:** only the series everyone sees, and their big moments. Their bank, production, spending
  and trades stay private.
- **Admin only, Pace:** every company's gold earned by days since it was founded, next to the model's regular player.
  This is how we check the balance against real players (1.4, B13).
- **On a phone:** the panel is full-screen, the cards are a strip you swipe, and the journal sits under the graph.

## How it is built

- **Game** (`GameWorld.Statistics.cs`): `TakeSamples` runs with the market update and the moments are added where they
  happen. Both wait in lists for the database (`PendingSamples`, `PendingMoments`).
- **Database:**
  - New tables `StatSamples` (kind, subject, time, values as JSON) and `Moments`.
  - They are written in the same transaction as the world, so a crash loses both or neither. A failed save puts them
    back in the lists.
  - An hourly clean-up thins old samples.
  - A world reset clears both tables, and so does a deleted company.
- **Server:** `GetStatistics(company, range)` picks the step that gives at most about 200 points (15 minutes, an hour,
  4 hours or a day), leaves out what the viewer may not see, and adds the live point. `GetStatisticsCompare` returns the
  same for other players, and `GetPace` returns the admin's Pace view.
- **Client:** a Statistics panel with hand-made SVG charts (no chart library).
