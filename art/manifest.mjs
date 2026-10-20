// The generated art's manifest (TransportPipes.Frontend/public/art/manifest.json), shared by process.mjs and pipes.mjs
import { createHash } from "node:crypto";
import { existsSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

export const OUT = new URL("../TransportPipes.Frontend/public/art/", import.meta.url).pathname;
export const MANIFEST = join(OUT, "manifest.json");

export function manifest(empty) {
  return existsSync(MANIFEST) ? JSON.parse(readFileSync(MANIFEST, "utf8")) : empty;
}

/**
 * Writes the manifest with a content hash of every sprite: the game asks for `<path>?v=<hash>`, so browsers can keep
 * the art for good and still get a new picture when it changes.
 */
export function save(m) {
  m.hashes = {};
  for (const dir of ["buildings", "terrain", "props", "pipes"]) {
    if (!existsSync(join(OUT, dir))) continue;
    for (const file of readdirSync(join(OUT, dir)).filter((f) => f.endsWith(".webp")).sort()) {
      m.hashes[`${dir}/${file}`] = createHash("sha256").update(readFileSync(join(OUT, dir, file))).digest("hex").slice(0, 10);
    }
  }
  writeFileSync(MANIFEST, JSON.stringify(m, null, 1));
}
