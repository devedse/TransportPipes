import { Container, Graphics, Sprite, Text, Texture } from "pixi.js";

// Feedback that makes progress feel good: coins popping off a trading post, a burst when a building is done, sparkles
// for an upgrade. Everything floats in its own layer above the map and goes away by itself.

interface Particle {
  display: Container;
  vx: number;
  vy: number;
  age: number;
  life: number;
  gravity: number;
  spin: number;
  /** Grows by this factor over its life (a ring) */
  grow: number;
}

let coinTexture: Texture | null = null;

/** A small shiny gold coin, drawn once */
function coin(): Texture {
  if (coinTexture === null) {
    const canvas = document.createElement("canvas");
    canvas.width = canvas.height = 40;
    const g = canvas.getContext("2d")!;
    const gradient = g.createRadialGradient(15, 13, 3, 20, 20, 19);
    gradient.addColorStop(0, "#fff3b0");
    gradient.addColorStop(0.55, "#f6c744");
    gradient.addColorStop(1, "#b8860b");
    g.beginPath();
    g.arc(20, 20, 17, 0, Math.PI * 2);
    g.fillStyle = gradient;
    g.fill();
    g.lineWidth = 3;
    g.strokeStyle = "#6e4b0a";
    g.stroke();
    g.beginPath();
    g.arc(20, 20, 10, 0, Math.PI * 2);
    g.strokeStyle = "rgba(110,75,10,0.55)";
    g.lineWidth = 2;
    g.stroke();
    coinTexture = Texture.from(canvas);
  }
  return coinTexture;
}

/** Players who asked their system for less motion get the coins (they tell how much), without bursts and sparkles */
function reducedMotion(): boolean {
  return (
    typeof window.matchMedia === "function" && window.matchMedia("(prefers-reduced-motion: reduce)").matches
  );
}

export class Juice {
  readonly layer = new Container();
  private readonly particles: Particle[] = [];

  /** "+123" with a coin, rising from a spot */
  coinPop(x: number, y: number, amount: number) {
    const group = new Container();
    const icon = new Sprite(coin());
    icon.anchor.set(0.5);
    icon.scale.set(0.9);
    const label = new Text({
      text: `+${amount >= 1000 ? `${(amount / 1000).toFixed(1)}k` : Math.round(amount)}`,
      style: {
        fontFamily: "Nunito Variable, system-ui",
        fontSize: 34,
        fontWeight: "900",
        fill: "#ffe27a",
        stroke: { color: "#4a3008", width: 7 },
      },
    });
    label.anchor.set(0, 0.5);
    label.position.set(20, 0);
    group.addChild(icon, label);
    group.position.set(x - 30, y);
    // With less motion it fades where it is instead of rising
    this.add(group, 0, reducedMotion() ? 0 : -55, 1.4, 0, 0);
  }

  /** Dust and sparkles flying off a building that just finished */
  burst(x: number, y: number, size: number) {
    if (reducedMotion()) {
      return;
    }
    for (let i = 0; i < 18 + size * 8; i++) {
      const g = new Graphics();
      const sparkle = i % 3 === 0;
      if (sparkle) {
        g.star(0, 0, 4, 9, 4).fill({ color: 0xffe9a0 });
      } else {
        g.circle(0, 0, 6 + Math.random() * 8).fill({ color: 0xe8dcc0, alpha: 0.75 });
      }
      g.position.set(x + (Math.random() - 0.5) * 60 * size, y + (Math.random() - 0.5) * 20 * size);
      const angle = Math.random() * Math.PI * 2;
      const speed = 80 + Math.random() * 140;
      this.add(
        g,
        Math.cos(angle) * speed,
        Math.sin(angle) * speed * 0.5 - 120,
        0.9 + Math.random() * 0.5,
        260,
        sparkle ? 4 : 0,
      );
    }
  }

  /** A ring that grows out of a building in a colour (a new level tier) */
  ring(x: number, y: number, color: number, size: number) {
    if (reducedMotion()) {
      return;
    }
    for (let i = 0; i < 2; i++) {
      const g = new Graphics();
      g.ellipse(0, 0, 70 * size, 35 * size).stroke({ color, width: 10 - i * 4, alpha: 0.9 });
      g.position.set(x, y);
      this.add(g, 0, 0, 0.9 + i * 0.25, 0, 0, 1.6);
    }
    this.sparkle(x, y);
  }

  /** A short twinkle around a building (an upgrade) */
  sparkle(x: number, y: number) {
    if (reducedMotion()) {
      return;
    }
    for (let i = 0; i < 10; i++) {
      const g = new Graphics();
      g.star(0, 0, 4, 10, 4).fill({ color: 0xfff3b0 });
      g.position.set(x + (Math.random() - 0.5) * 120, y - Math.random() * 120);
      this.add(g, 0, -40 - Math.random() * 40, 0.8 + Math.random() * 0.4, 0, 3);
    }
  }

  private add(
    display: Container,
    vx: number,
    vy: number,
    life: number,
    gravity: number,
    spin: number,
    grow = 0,
  ) {
    this.layer.addChild(display);
    this.particles.push({ display, vx, vy, age: 0, life, gravity, spin, grow });
  }

  update(deltaSeconds: number) {
    for (let i = this.particles.length - 1; i >= 0; i--) {
      const p = this.particles[i]!;
      p.age += deltaSeconds;
      if (p.age >= p.life) {
        p.display.destroy({ children: true });
        this.particles.splice(i, 1);
        continue;
      }
      p.vy += p.gravity * deltaSeconds;
      p.display.x += p.vx * deltaSeconds;
      p.display.y += p.vy * deltaSeconds;
      p.display.rotation += p.spin * deltaSeconds;
      const t = p.age / p.life;
      p.display.alpha = t < 0.7 ? 1 : 1 - (t - 0.7) / 0.3;
      if (p.grow > 0) {
        p.display.scale.set(1 + p.grow * t);
      } else if (p.age < 0.15) {
        p.display.scale.set(0.6 + (p.age / 0.15) * 0.4);
      }
    }
  }
}
