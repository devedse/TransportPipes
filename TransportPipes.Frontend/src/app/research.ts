import type { CompanyDto } from "../api/contracts";
import type { Content } from "../game/content";

/** A track's tier by name: "Iron 3" */
export function tierName(track: string, tier: number, content: Content): string {
  return `${content.track(track)?.name ?? track} ${tier}`;
}

/** What the Lab researches now, by name (1.4 §12): a track's tier and an Era tree item, either or both */
export function researchName(company: CompanyDto, content: Content): string | null {
  const { currentResearch, currentTrack, tiers } = company.research;
  const names: string[] = [];
  if (currentTrack !== null) {
    names.push(tierName(currentTrack, (tiers[currentTrack] ?? 0) + 1, content));
  }
  if (currentResearch !== null) {
    names.push(content.research(currentResearch)?.name ?? currentResearch);
  }
  return names.length > 0 ? names.join(" · ") : null;
}
