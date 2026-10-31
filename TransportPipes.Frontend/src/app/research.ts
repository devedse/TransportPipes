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

/** What each of the Lab's lines researches now: its name, when it ends and how long it takes in all */
export function runningResearch(
  company: CompanyDto,
  content: Content,
): Array<{ name: string; endsAt: string | null; minutes: number | null }> {
  const lines = company.research;
  const running: Array<{ name: string; endsAt: string | null; minutes: number | null }> = [];
  if (lines.currentTrack !== null) {
    running.push({
      name: tierName(lines.currentTrack, (lines.tiers[lines.currentTrack] ?? 0) + 1, content),
      endsAt: lines.trackEndsAt,
      minutes: lines.trackMinutes ?? null,
    });
  }
  if (lines.currentResearch !== null) {
    running.push({
      name: content.research(lines.currentResearch)?.name ?? lines.currentResearch,
      endsAt: lines.researchEndsAt,
      minutes: lines.researchMinutes ?? null,
    });
  }
  return running;
}
