import { Container, Sprite, Texture } from "pixi.js";
import { createCanvas } from "../art/iso";
import type { TextureBank } from "../art/textures";
import type { Content } from "../content";
import {
  connectorDensity,
  connectorPath,
  packetResource,
  LANE_SLOTS,
  type ConnectorFlow,
  type ConnectorPoint,
  type PipeSide,
} from "../connectors";
import type { LaneKind } from "../inside";

const textures = new Map<string, Texture>();
function detailTexture(kind: LaneKind | "tread"): Texture {
  let texture = textures.get(kind);
  if (texture !== undefined) return texture;
  const canvas = createCanvas(32, 24);
  const g = canvas.getContext("2d")!;
  if (kind === "solid") {
    g.fillStyle = "#cdb38a";
    g.strokeStyle = "#453422";
    g.lineWidth = 2;
    g.beginPath();
    g.moveTo(6, 17);
    g.lineTo(8, 7);
    g.lineTo(20, 4);
    g.lineTo(26, 15);
    g.lineTo(20, 21);
    g.closePath();
    g.fill();
    g.stroke();
  } else if (kind === "power") {
    g.shadowColor = "#a9eaff";
    g.shadowBlur = 6;
    g.strokeStyle = "#6dd5ff";
    g.lineWidth = 6;
    g.lineJoin = "round";
    const bolt = () => {
      g.beginPath();
      g.moveTo(3, 12);
      g.lineTo(11, 7);
      g.lineTo(15, 16);
      g.lineTo(21, 8);
      g.lineTo(29, 12);
      g.stroke();
    };
    bolt();
    g.shadowBlur = 0;
    g.strokeStyle = "#fff5ab";
    g.lineWidth = 2;
    bolt();
  } else if (kind === "fluid") {
    g.fillStyle = "#ffffff";
    g.beginPath();
    g.roundRect(3, 6, 26, 12, 6);
    g.fill();
    g.strokeStyle = "#e2f9ff";
    g.lineWidth = 2;
    g.beginPath();
    g.moveTo(8, 8);
    g.lineTo(21, 8);
    g.stroke();
  } else {
    g.fillStyle = "#9a9d91";
    g.fillRect(4, 10, 24, 4);
  }
  texture = Texture.from(canvas);
  textures.set(kind, texture);
  return texture;
}

interface Track {
  kind: LaneKind;
  outgoing: boolean;
  rows: ConnectorFlow[];
  count: number;
  path: ConnectorPoint[];
  particles: Sprite[];
  treads: Sprite[];
}

/** Short, pooled animations drawn with their pipe, so foreground buildings occlude them naturally. */
export class ConnectorAnimation {
  readonly container = new Container();
  private readonly tracks = new Map<string, Track>();
  private flows: ConnectorFlow[] = [];
  readonly key: string;
  readonly x: number;
  readonly y: number;
  private readonly side: PipeSide;
  private readonly kinds: LaneKind[];
  private readonly content: Content;
  private readonly textures: TextureBank;
  private readonly phase: number;

  constructor(
    key: string,
    x: number,
    y: number,
    side: PipeSide,
    kinds: LaneKind[],
    content: Content,
    textures: TextureBank,
    phase: number,
  ) {
    this.key = key;
    this.x = x;
    this.y = y;
    this.side = side;
    this.kinds = kinds;
    this.content = content;
    this.textures = textures;
    this.phase = phase;
  }

  get active(): boolean {
    return this.flows.length > 0;
  }
  get activeKinds(): LaneKind[] {
    return [...new Set([...this.tracks.values()].filter((t) => t.count > 0).map((t) => t.kind))];
  }

  setFlows(flows: ConnectorFlow[]) {
    this.flows = flows;
    for (const track of this.tracks.values()) {
      track.rows = [];
      track.count = 0;
    }
    for (const flow of flows) {
      const kind = (this.content.resource(flow.resource)?.kind ?? "solid") as LaneKind;
      const index = this.kinds.indexOf(kind);
      if (index < 0) continue;
      const key = `${kind}:${flow.outgoing}`;
      let track = this.tracks.get(key);
      if (track === undefined) {
        const offset =
          kind === "solid"
            ? flow.outgoing
              ? 0.04
              : -0.04
            : kind === "power"
              ? flow.outgoing
                ? 0.024
                : -0.024
              : 0;
        track = {
          kind,
          outgoing: flow.outgoing,
          rows: [],
          count: 0,
          path: connectorPath(this.side, LANE_SLOTS[this.kinds.length]![index]!, kind, offset),
          particles: [],
          treads: [],
        };
        for (let i = 0; i < 3; i++) {
          const sprite = new Sprite(detailTexture(kind));
          sprite.anchor.set(0.5, kind === "solid" ? 0.85 : 0.5);
          sprite.width = kind === "solid" ? 12 : kind === "power" ? 15 : 10;
          sprite.height = kind === "solid" ? 12 : kind === "power" ? 7 : 4;
          this.container.addChild(sprite);
          track.particles.push(sprite);
        }
        if (kind === "solid")
          for (let i = 0; i < 5; i++) {
            const sprite = new Sprite(detailTexture("tread"));
            sprite.anchor.set(0.5);
            sprite.width = 6;
            sprite.height = 1.4;
            sprite.rotation =
              this.side === "u" || this.side === "d" ? Math.atan2(48, 96) : Math.atan2(48, -96);
            this.container.addChildAt(sprite, 0);
            track.treads.push(sprite);
          }
        this.tracks.set(key, track);
      }
      track.rows.push(flow);
    }
    for (const track of this.tracks.values())
      track.count = connectorDensity(track.rows.reduce((sum, f) => sum + f.rate, 0));
  }

  update(time: number, alpha: number, reduced: boolean) {
    this.container.visible = alpha > 0 && this.active;
    if (!this.container.visible) return;
    this.container.alpha = alpha;
    const clock = (reduced ? 0.35 : time * 0.8) + this.phase;
    for (const track of this.tracks.values()) {
      for (let i = 0; i < track.treads.length; i++) {
        const sprite = track.treads[i]!;
        sprite.visible = track.count > 0;
        if (sprite.visible) this.position(sprite, track, (clock + i / track.treads.length) % 1);
      }
      for (let i = 0; i < track.particles.length; i++) {
        const sprite = track.particles[i]!;
        sprite.visible = i < track.count;
        if (!sprite.visible) continue;
        const progress = clock + i / track.count;
        const t = progress % 1;
        const resource = packetResource(track.rows, Math.floor(progress) + i * 13 + this.phase * 17);
        sprite.label = `${track.kind}:${resource}:${track.outgoing ? "out" : "in"}`;
        if (track.kind === "solid") {
          const texture = this.textures.resourceIcon(resource);
          const next = texture ?? detailTexture("solid");
          sprite.tint = texture === null ? this.content.resourceColor(resource) : 0xffffff;
          if (sprite.texture !== next) {
            sprite.texture = next;
            sprite.width = 12;
            sprite.height = 12;
          }
        } else if (track.kind === "fluid") sprite.tint = this.content.resourceColor(resource);
        this.position(sprite, track, t);
        // Fade at the hatch and building edge, so goods enter rather than popping on/off.
        sprite.alpha =
          Math.min(1, t * 9, (1 - t) * 9) *
          (track.kind === "power" && !reduced ? 0.8 + 0.2 * Math.sin(time * 13 + i * 2) : 1);
      }
    }
  }

  private position(sprite: Sprite, track: Track, progress: number) {
    const t = track.outgoing ? 1 - progress : progress;
    const piece = t * (track.path.length - 1);
    const index = Math.min(track.path.length - 2, Math.floor(piece));
    const a = track.path[index]!;
    const b = track.path[index + 1]!;
    const f = piece - index;
    sprite.position.set(a.x + (b.x - a.x) * f, a.y + (b.y - a.y) * f);
    if (track.kind !== "solid") sprite.rotation = Math.atan2(b.y - a.y, b.x - a.x);
  }
}
