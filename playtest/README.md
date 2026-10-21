# The AI playtest

AI players (Claude agents, each playing a person from `players/`) play the real game in real browsers, on a playtest
server whose clock the director moves between sessions. The plan is `docs/playtest-design.html`. A run's logs,
screenshots, diaries and report stay in `runs/<run>/` (gitignored).

## Running a story

```sh
export PLAYTEST_RUN=story1
dotnet publish TransportPipes.Server -c Release -o playtest/bin      # the server's own copy (rebuilds don't touch it)
TP_SERVER=http://localhost:5090 npx vite --port 5174                  # in TransportPipes.Frontend: the players' client
node playtest/director.mjs reset && node playtest/director.mjs server start && node playtest/director.mjs pause
node playtest/driver.mjs &                                            # the browsers (players.json in the run: phone, skim)
```

Then, for each row of the story (`stories/17a.md`):

```sh
node playtest/director.mjs goto <day> <HH:MM>                         # jump (samples every 6 h)
node playtest/prompt.mjs <player> <session> <budget> "<situation>"    # writes runs/<run>/<player>/prompt-<session>.md
node playtest/director.mjs resume 5                                   # 5× during sessions
#   …one agent per player: "read <prompt file> and follow it" (in parallel when they play together)
node playtest/director.mjs pause && node playtest/director.mjs sample <label>
```

`director.mjs snapshot <name>` / `restore <name>` keep and bring back a moment of the world. Note in
`runs/<run>/director-notes.md` what the playtest itself got wrong, so the analyst keeps it out of the game's findings.

## The report

```sh
node playtest/digest.mjs                                              # one file with every session, the chats, events, numbers
#   an analyst agent with analyst.md (placeholders filled in) writes runs/<run>/findings.json
node playtest/report.mjs                                              # runs/<run>/report/report.html, screenshots embedded
```

`runs/<run>/director-summary.md` (how the run went) and `costs.json` (tokens and agent time per session) go into the
report too.

## The driver's commands

`node playtest/pt.mjs <player> help` lists them: `open`, `look [--shot]`, `shot`, `map [--sun|--wind]`,
`click "<label>" [--nth n]`, `tile`, `rclick`, `hover`, `drag`, `fill`, `type`, `key`, `goto`, `wait`, `chat`, `group`,
`clipboard`, `note`, `end`. Phones get real touch events. The game draws at about 12 frames a second, so four
browsers fit on four cores.
