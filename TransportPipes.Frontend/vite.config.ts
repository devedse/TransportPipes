/// <reference types="vitest/config" />
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// In development the ASP.NET Core server runs on :5080 (TP_SERVER for another one, like the playtest server); in
// production it serves dist/ itself, so the hub is always reachable on the same origin under /hubs.
const env = (globalThis as { process?: { env: Record<string, string | undefined> } }).process?.env ?? {};
const server = env.TP_SERVER ?? "http://localhost:5080";

export default defineConfig({
  plugins: [react()],
  base: "/",
  server: {
    port: 5173,
    proxy: {
      "/hubs": {
        target: server,
        ws: true,
        changeOrigin: true,
      },
      "/api": {
        target: server,
        changeOrigin: true,
      },
    },
  },
  build: {
    outDir: "dist",
    // PixiJS alone is ~700 kB minified; that is expected for a WebGL game.
    chunkSizeWarningLimit: 1500,
  },
  test: {
    environment: "node",
    include: ["src/**/*.test.ts"],
  },
});
