// Small synthesized sounds (no files): a coin, a finished building, a level-up, a click. Muted when the player says so.

const MUTE_KEY = "tp.muted";
let context: AudioContext | null = null;

export function isMuted(): boolean {
  try {
    return localStorage.getItem(MUTE_KEY) === "1";
  } catch {
    return false;
  }
}

export function setMuted(muted: boolean) {
  try {
    localStorage.setItem(MUTE_KEY, muted ? "1" : "0");
  } catch {
    // Private windows: muted for this visit only
  }
}

function audio(): AudioContext | null {
  if (isMuted() || typeof AudioContext === "undefined") {
    return null;
  }
  context ??= new AudioContext();
  if (context.state === "suspended") {
    void context.resume();
  }
  return context;
}

function tone(
  frequency: number,
  start: number,
  length: number,
  volume: number,
  type: OscillatorType = "sine",
) {
  const ctx = audio();
  if (ctx === null) {
    return;
  }
  const t = ctx.currentTime + start;
  const oscillator = ctx.createOscillator();
  const gain = ctx.createGain();
  oscillator.type = type;
  oscillator.frequency.setValueAtTime(frequency, t);
  gain.gain.setValueAtTime(0.0001, t);
  gain.gain.exponentialRampToValueAtTime(volume, t + 0.01);
  gain.gain.exponentialRampToValueAtTime(0.0001, t + length);
  oscillator.connect(gain).connect(ctx.destination);
  oscillator.start(t);
  oscillator.stop(t + length + 0.02);
}

export const sounds = {
  coin() {
    tone(1318, 0, 0.12, 0.05, "triangle");
    tone(1760, 0.06, 0.18, 0.04, "triangle");
  },
  built() {
    tone(196, 0, 0.18, 0.12, "triangle");
    tone(392, 0.08, 0.25, 0.06, "sine");
    tone(587, 0.16, 0.3, 0.05, "sine");
  },
  levelUp() {
    [523, 659, 784, 1046].forEach((f, i) => tone(f, i * 0.11, 0.35, 0.07, "triangle"));
    tone(1318, 0.5, 0.6, 0.05, "sine");
  },
  click() {
    tone(880, 0, 0.05, 0.03, "square");
  },
};
