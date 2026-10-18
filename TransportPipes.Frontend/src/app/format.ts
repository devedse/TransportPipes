/** "FactorySmall" -> "Factory Small", "WindMoily" -> "Wind Moily". */
export function displayName(gameObjectTypeName: string): string {
  return gameObjectTypeName.replace(/([a-z])([A-Z])/g, "$1 $2");
}

/** "v1.0.5-beta · 0307205" (the commit is shortened, and left out when unknown). */
export function formatVersion(info: { version: string; commit: string | null }): string {
  const commit = info.commit === null || info.commit === "" ? "" : ` · ${info.commit.slice(0, 7)}`;
  return `v${info.version}${commit}`;
}

export function formatDuration(seconds: number): string {
  seconds = Math.round(seconds);
  if (seconds <= 0) {
    return "instant";
  }
  if (seconds >= 3600) {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.round((seconds % 3600) / 60);
    return minutes === 0 ? `${hours} h` : `${hours} h ${minutes} min`;
  }
  const minutes = Math.floor(seconds / 60);
  const rest = seconds % 60;
  if (minutes === 0) {
    return `${rest} s`;
  }
  return rest === 0 ? `${minutes} min` : `${minutes} min ${rest} s`;
}

const SUFFIXES = ["", "k", "M", "B", "T", "Qa", "Qi"];

/** 950, 1,240, 12.4k, 3.21M ... */
export function formatAmount(value: number): string {
  const sign = value < 0 ? "-" : "";
  const abs = Math.abs(value);
  if (abs < 10) {
    return sign + (Math.round(abs * 10) / 10).toLocaleString("en-US");
  }
  if (abs < 10_000) {
    return sign + Math.round(abs).toLocaleString("en-US");
  }
  const tier = Math.min(SUFFIXES.length - 1, Math.floor(Math.log10(abs) / 3));
  const scaled = abs / Math.pow(1000, tier);
  const digits = scaled < 100 ? 1 : 0;
  return `${sign}${scaled.toFixed(digits)}${SUFFIXES[tier]}`;
}

/** "+12.4/min" */
export function formatRate(perMinute: number): string {
  return `${perMinute >= 0 ? "+" : ""}${formatAmount(perMinute)}/min`;
}

/** "1 building", "3 buildings" */
export function plural(count: number, one: string, many = `${one}s`): string {
  return `${formatAmount(count)} ${count === 1 ? one : many}`;
}

export function formatTimeAgo(since: Date, now = new Date()): string {
  return formatDuration((now.getTime() - since.getTime()) / 1000);
}
