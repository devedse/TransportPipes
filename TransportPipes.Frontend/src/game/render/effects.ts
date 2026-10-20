import { AnimatedSprite, Container, Sprite, Texture } from "pixi.js";
import { drawFlag, drawRotor, drawWheel } from "../art/buildings";
import { createCanvas, type Attachment } from "../art/iso";

// The moving details of buildings: smoke from chimneys (only while the building works), turning rotors and wheels,
// waving flags, blinking lights and glows.

const textures = new Map<string, Texture | Texture[]>();

function cachedTexture(key: string, draw: () => HTMLCanvasElement): Texture {
  let texture = textures.get(key) as Texture | undefined;
  if (texture === undefined) {
    texture = Texture.from(draw());
    textures.set(key, texture);
  }
  return texture;
}

function cachedFrames(key: string, count: number, draw: (frame: number) => HTMLCanvasElement): Texture[] {
  let frames = textures.get(key) as Texture[] | undefined;
  if (frames === undefined) {
    frames = Array.from({ length: count }, (_, i) => Texture.from(draw(i)));
    textures.set(key, frames);
  }
  return frames;
}

function puffTexture(): Texture {
  return cachedTexture("puff", () => {
    const canvas = createCanvas(48, 48);
    const g = canvas.getContext("2d")!;
    const gradient = g.createRadialGradient(24, 24, 2, 24, 24, 22);
    gradient.addColorStop(0, "rgba(245,245,245,0.95)");
    gradient.addColorStop(0.7, "rgba(225,225,225,0.7)");
    gradient.addColorStop(1, "rgba(200,200,200,0)");
    g.fillStyle = gradient;
    g.beginPath();
    g.arc(24, 24, 22, 0, Math.PI * 2);
    g.fill();
    return canvas;
  });
}

export function dotTexture(): Texture {
  return cachedTexture("dot", () => {
    const canvas = createCanvas(32, 32);
    const g = canvas.getContext("2d")!;
    g.beginPath();
    g.arc(16, 16, 12, 0, Math.PI * 2);
    g.fillStyle = "#ffffff";
    g.fill();
    g.lineWidth = 3;
    g.strokeStyle = "rgba(20,20,25,0.9)";
    g.stroke();
    return canvas;
  });
}

function glowTexture(): Texture {
  return cachedTexture("glow", () => {
    const canvas = createCanvas(64, 64);
    const g = canvas.getContext("2d")!;
    const gradient = g.createRadialGradient(32, 32, 2, 32, 32, 30);
    gradient.addColorStop(0, "rgba(255,255,255,1)");
    gradient.addColorStop(1, "rgba(255,255,255,0)");
    g.fillStyle = gradient;
    g.fillRect(0, 0, 64, 64);
    return canvas;
  });
}

function rotorTexture(length: number): Texture {
  return cachedTexture(`rotor:${length}`, () => {
    const size = Math.ceil(length * 2 + 16);
    const canvas = createCanvas(size, size);
    const g = canvas.getContext("2d")!;
    g.translate(size / 2, size / 2);
    drawRotor(g, length, 0);
    return canvas;
  });
}

function wheelFrames(radius: number, color: string): Texture[] {
  return cachedFrames(`wheel:${radius}:${color}`, 8, (frame) => {
    const canvas = createCanvas(radius * 1.4 + 8, radius * 2 + 8);
    const g = canvas.getContext("2d")!;
    g.translate(canvas.width / 2, canvas.height / 2);
    drawWheel(g, radius, color, (frame * Math.PI) / 32);
    return canvas;
  });
}

function flagFrames(width: number, color: string): Texture[] {
  return cachedFrames(`flag:${width}:${color}`, 8, (frame) => {
    const canvas = createCanvas(width + 8, width * 0.6 + 14);
    const g = canvas.getContext("2d")!;
    g.translate(2, 6);
    drawFlag(g, width, color, (frame * Math.PI * 2) / 8);
    return canvas;
  });
}

interface Puff {
  sprite: Sprite;
  age: number;
  life: number;
  vx: number;
  spark: boolean;
}

/** The animated details of one building */
export class BuildingEffects {
  readonly container = new Container();
  private readonly rotors: Sprite[] = [];
  private readonly blinkers: Sprite[] = [];
  private readonly glows: Sprite[] = [];
  private readonly animated: AnimatedSprite[] = [];
  private readonly emitters: Array<{
    x: number;
    y: number;
    size: number;
    kind: "smoke" | "spark";
    color: string;
    timer: number;
  }> = [];
  private readonly puffs: Puff[] = [];
  private running = true;
  private readonly phase = Math.random() * 10;

  constructor(attachments: Attachment[], flagColor: string) {
    for (const a of attachments) {
      switch (a.kind) {
        case "rotor": {
          const sprite = new Sprite(a.texture ?? rotorTexture(Math.round(a.size * 70)));
          sprite.anchor.set(0.5);
          if (a.scale !== undefined) {
            sprite.scale.set(a.scale);
          }
          sprite.position.set(a.x, a.y);
          this.rotors.push(sprite);
          this.container.addChild(sprite);
          break;
        }
        case "wheel": {
          const sprite = new AnimatedSprite(wheelFrames(Math.round(a.size * 40), a.color ?? "#6b4a2a"));
          sprite.anchor.set(0.5);
          sprite.position.set(a.x, a.y);
          sprite.animationSpeed = 0.25;
          sprite.play();
          this.animated.push(sprite);
          this.container.addChild(sprite);
          break;
        }
        case "flag": {
          const sprite = new AnimatedSprite(flagFrames(Math.round(a.size * 34), a.color ?? flagColor));
          sprite.anchor.set(0, 0.1);
          sprite.position.set(a.x, a.y);
          sprite.animationSpeed = 0.12;
          sprite.play();
          this.container.addChild(sprite);
          break;
        }
        case "blink": {
          const sprite = new Sprite(glowTexture());
          sprite.anchor.set(0.5);
          sprite.scale.set(0.35);
          sprite.tint = a.color ?? "#ff4040";
          sprite.position.set(a.x, a.y);
          this.blinkers.push(sprite);
          this.container.addChild(sprite);
          break;
        }
        case "glow": {
          const sprite = new Sprite(glowTexture());
          sprite.anchor.set(0.5);
          sprite.scale.set(a.size * 1.1);
          sprite.tint = a.color ?? "#ff8a2a";
          sprite.blendMode = "add";
          sprite.position.set(a.x, a.y);
          this.glows.push(sprite);
          this.container.addChild(sprite);
          break;
        }
        case "smoke":
        case "spark":
          this.emitters.push({
            x: a.x,
            y: a.y,
            size: a.size,
            kind: a.kind,
            color: a.color ?? "#ffffff",
            timer: Math.random(),
          });
          break;
      }
    }
  }

  get isEmpty(): boolean {
    return this.container.children.length === 0 && this.emitters.length === 0;
  }

  setRunning(running: boolean) {
    if (running === this.running) {
      return;
    }
    this.running = running;
    for (const sprite of this.animated) {
      if (running) {
        sprite.play();
      } else {
        sprite.stop();
      }
    }
    for (const glow of this.glows) {
      glow.visible = running;
    }
  }

  update(deltaSeconds: number, time: number) {
    if (this.running) {
      for (const rotor of this.rotors) {
        rotor.rotation += deltaSeconds * 2.2;
      }
    }
    for (const blinker of this.blinkers) {
      blinker.alpha = Math.sin(time * 3 + this.phase) > 0.2 ? 1 : 0.15;
    }
    for (const glow of this.glows) {
      glow.alpha = 0.55 + Math.sin(time * 2 + this.phase) * 0.25;
    }
    if (this.running) {
      for (const emitter of this.emitters) {
        emitter.timer -= deltaSeconds;
        if (emitter.timer <= 0) {
          emitter.timer = emitter.kind === "smoke" ? 0.45 + Math.random() * 0.3 : 0.12;
          this.spawn(emitter);
        }
      }
    }
    for (let i = this.puffs.length - 1; i >= 0; i--) {
      const puff = this.puffs[i]!;
      puff.age += deltaSeconds;
      const t = puff.age / puff.life;
      if (t >= 1) {
        puff.sprite.destroy();
        this.puffs.splice(i, 1);
        continue;
      }
      puff.sprite.y -= deltaSeconds * 26;
      puff.sprite.x += puff.vx * deltaSeconds;
      puff.sprite.scale.set(puff.spark ? 0.2 * (1 - t) : 0.5 + t * 1.3);
      puff.sprite.alpha = (1 - t) * 0.85;
    }
  }

  private spawn(emitter: (typeof this.emitters)[number]) {
    const sprite = new Sprite(emitter.kind === "smoke" ? puffTexture() : glowTexture());
    sprite.anchor.set(0.5);
    sprite.position.set(emitter.x + (Math.random() - 0.5) * 6, emitter.y);
    if (emitter.kind === "spark") {
      sprite.tint = emitter.color;
      sprite.scale.set(0.2);
    }
    this.container.addChild(sprite);
    const spark = emitter.kind === "spark";
    this.puffs.push({
      sprite,
      age: 0,
      life: spark ? 0.8 : 2.6,
      vx: spark ? (Math.random() - 0.5) * 40 : 10 + Math.random() * 8,
      spark,
    });
  }

  destroy() {
    this.container.destroy({ children: true });
  }
}
