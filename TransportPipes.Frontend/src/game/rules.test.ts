import { describe, expect, it } from "vitest";
import type { BuildingDto, CompanyDto } from "../api/contracts";
import { CHUNK_SIZE, chunkOf, estateOrigin } from "./layout";
import { routePipe } from "./rules";
import { WorldModel } from "./world";

const empty = btoa(String.fromCharCode(...new Uint8Array(CHUNK_SIZE * CHUNK_SIZE)));
const origin = estateOrigin(0, 0, 0);

/** Grass around the first estate of the Capital, with a solar panel and a trading post on it */
function estate(): { world: WorldModel; company: CompanyDto; panel: BuildingDto; post: BuildingDto } {
  const building = (id: number, type: string, x: number, y: number): BuildingDto => ({
    id,
    type,
    owner: 1,
    x: origin.x + x,
    y: origin.y + y,
    size: 1,
    level: 1,
    constructionEndsAt: null,
    network: null,
    damaged: false,
  });
  const panel = building(1, "solarPanel", 10, 6);
  const post = building(2, "tradingPost", 16, 12);
  const world = new WorldModel(["grass"]);
  const chunks = new Set<string>();
  for (let y = origin.y - 2; y < origin.y + 34; y += 8) {
    for (let x = origin.x - 2; x < origin.x + 34; x += 8) {
      const c = chunkOf(x, y);
      chunks.add(`${c.x},${c.y}`);
    }
  }
  for (const k of chunks) {
    const [cx, cy] = k.split(",").map(Number) as [number, number];
    const mine = [panel, post].filter((b) => chunkOf(b.x, b.y).x === cx && chunkOf(b.x, b.y).y === cy);
    world.setChunk({
      cx,
      cy,
      terrain: empty,
      deposits: empty,
      richness: empty,
      wind: empty,
      sun: empty,
      depositNames: [],
      objects: { cx, cy, buildings: mine, conductors: [], terrainOverrides: [], outposts: [], deposits: [] },
    });
  }
  const company = {
    id: 1,
    estateX: origin.x,
    estateY: origin.y,
    unlockedParcels: -1,
    branches: [],
  } as unknown as CompanyDto;
  return { world, company, panel, post };
}

const nextTo = (b: BuildingDto, t: { x: number; y: number }) =>
  Math.abs(t.x - b.x) + Math.abs(t.y - b.y) === 1;

describe("a pipe dragged from a building to another", () => {
  it("runs between the tiles around them, as Loady says (drag from the panel to your Trading Post)", () => {
    const { world, company, panel, post } = estate();

    const route = routePipe(world, company, { x: panel.x, y: panel.y }, { x: post.x, y: post.y });

    expect(route).not.toBeNull();
    expect(nextTo(panel, route![0]!)).toBe(true);
    expect(nextTo(post, route![route!.length - 1]!)).toBe(true);
    expect(route!.some((t) => world.buildingAt(t.x, t.y) !== undefined)).toBe(false);
  });

  it("from a building to free land, and never from a building to itself", () => {
    const { world, company, panel } = estate();

    const route = routePipe(world, company, { x: panel.x, y: panel.y }, { x: panel.x + 4, y: panel.y });

    expect(route).not.toBeNull();
    expect(route![route!.length - 1]).toEqual({ x: panel.x + 4, y: panel.y });
    expect(routePipe(world, company, { x: panel.x, y: panel.y }, { x: panel.x, y: panel.y })).toBeNull();
  });
});
