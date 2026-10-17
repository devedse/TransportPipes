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
  if (seconds <= 0) {
    return "instant";
  }
  const minutes = Math.floor(seconds / 60);
  const rest = seconds % 60;
  if (minutes === 0) {
    return `${rest} s`;
  }
  return rest === 0 ? `${minutes} min` : `${minutes} min ${rest} s`;
}
