import { describe, expect, it } from "vitest";
import type { BuildingDto, ChunkObjectsDto } from "../api/contracts";
import { CHUNK_SIZE } from "./layout";
import { WorldModel } from "./world";

const empty = btoa(String.fromCharCode(...new Uint8Array(CHUNK_SIZE * CHUNK_SIZE)));

function objects(cx: number, cy: number, buildings: BuildingDto[] = []): ChunkObjectsDto {
  return { cx, cy, buildings, conductors: [], terrainOverrides: [], outposts: [], deposits: [] };
}

function world(): WorldModel {
  const model = new WorldModel(["grass"]);
  for (const cx of [0, 1]) {
    model.setChunk({
      cx,
      cy: 0,
      terrain: empty,
      deposits: empty,
      richness: empty,
      wind: empty,
      sun: empty,
      depositNames: [],
      objects: objects(cx, 0),
    });
  }
  return model;
}

const storage = (x: number): BuildingDto => ({
  id: 7,
  type: "storage",
  owner: 1,
  x,
  y: 5,
  size: 2,
  level: 1,
  constructionEndsAt: null,
  network: null,
  damaged: false,
});

describe("a building that moves into another chunk", () => {
  // Its old chunk and its new chunk are sent again; their updates come in any order
  for (const order of ["old chunk first", "new chunk first"] as const) {
    it(`is at its new place and nowhere else (${order})`, () => {
      const model = world();
      model.applyObjects(objects(0, 0, [storage(CHUNK_SIZE - 3)]));
      const updates = [objects(0, 0), objects(1, 0, [storage(CHUNK_SIZE + 1)])];
      for (const update of order === "old chunk first" ? updates : updates.reverse()) {
        model.applyObjects(update);
      }

      expect(model.buildings.get(7)?.x).toBe(CHUNK_SIZE + 1);
      expect(model.buildingAt(CHUNK_SIZE + 1, 5)?.id).toBe(7);
      expect(model.buildingAt(CHUNK_SIZE + 2, 6)?.id).toBe(7);
      expect(model.buildingAt(CHUNK_SIZE - 3, 5)).toBeUndefined();
      expect(model.buildingAt(CHUNK_SIZE - 2, 6)).toBeUndefined();
    });
  }
});
