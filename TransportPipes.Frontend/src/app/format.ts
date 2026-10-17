/** "FactorySmall" -> "Factory Small", "WindMoily" -> "Wind Moily". */
export function displayName(gameObjectTypeName: string): string {
  return gameObjectTypeName.replace(/([a-z])([A-Z])/g, "$1 $2");
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
