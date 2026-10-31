/**
 * A Yard's banner in its two colours: a field and a slanted sash, so it reads as a flag (playtest: split down the middle
 * beside "4 of 4 plots" it read as a half-full bar)
 */
export function bannerBackground(color: string, color2: string | null | undefined): string {
  const sash = color2 ?? color;
  return `linear-gradient(115deg, ${color} 0 62%, ${sash} 62% 78%, ${color} 78% 100%)`;
}
