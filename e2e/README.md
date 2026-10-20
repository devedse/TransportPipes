# Screenshot scripts

Scripts that play the game in a headless browser and take screenshots, used to check the look and the flows after
changes. They need the development setup running:

```sh
docker compose -f compose.dev.yaml up -d                                   # the database
ASPNETCORE_ENVIRONMENT=Development dotnet run --project TransportPipes.Server --urls http://localhost:5080
cd TransportPipes.Frontend && npx vite                                     # http://localhost:5173
```

Then `cd e2e && npm install && node <script>.mjs shots/<name>`. Each script registers a fresh player and uses the
development cheats (`TransportPipes:Cheats`) through `window.tp`.

| Script | What it shows |
| --- | --- |
| `onboarding.mjs` | A new player following Loady's first steps |
| `art-check.mjs`, `showcase.mjs` | Generated buildings on an estate (`showcase.mjs shots/x type,type,...`) |
| `pipes.mjs` | Pipes and flowing resources |
| `juice.mjs` | A finished building, coin pops and the level-up |
| `ui-check.mjs`, `title.mjs` | The panels, the title screen and the managers |
| `mobile.mjs` | The phone layout (390×844) |
| `far.mjs` | The hub and the zoomed-out world |
| `evolution.mjs` | Buildings at the level milestones |
| `photo.mjs` | Photo mode at golden hour and night |
