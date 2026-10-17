# TransportPipes.Frontend

The browser client of Transport Pipes: a React UI on top of a PixiJS isometric map, talking to the
ASP.NET Core SignalR hub at `/hubs/transport`.

## Requirements

Node 24 (see `.nvmrc`) and npm.

## Scripts

| Command             | What it does                                                 |
| ------------------- | ------------------------------------------------------------ |
| `npm ci`            | Install the exact dependencies from `package-lock.json`      |
| `npm run dev`       | Dev server on http://localhost:5173 with hot reload          |
| `npm run build`     | Type check and build the production bundle into `dist/`      |
| `npm run preview`   | Serve `dist/` locally                                        |
| `npm run typecheck` | TypeScript only                                              |
| `npm run lint`      | ESLint                                                       |
| `npm run format`    | Format everything with Prettier (`format:check` only checks) |
| `npm test`          | Unit tests (Vitest)                                          |

## Running against the backend

In development the Vite dev server proxies `/hubs` (including the WebSocket) to the backend on
`http://localhost:5080`, so start `TransportPipes.Server` on that port and run `npm run dev`.

In production the Docker build copies `dist/` into the server's `wwwroot`, so the app and the hub are
served from the same origin and no proxy or CORS setup is needed.

## Structure

```
public/images/        sprite atlases (game objects, terrain) and HUD icons
src/api/              hub contract (DTOs, methods, pushes) and the typed SignalR connection
src/app/              GameClient (glue between connection, store and map), shortcuts, helpers
src/game/             PixiJS engine: camera, isometric math, chunk streaming, sprites
src/state/            zustand store and gold interpolation
src/components/       React UI: login, HUD, build menu, build confirmation, inspector, toasts
src/styles/           global stylesheet
```

The pure logic (isometric math, camera, chunk ranges, sprite frames, gold, shortcuts, GameClient)
has unit tests next to the code (`*.test.ts`).
