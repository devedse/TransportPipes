import type { CompanyDto } from "../api/contracts";
import type { Content } from "../game/content";

/** What the Labs research now, by name: a tech or Mastery */
export function researchName(company: CompanyDto, content: Content): string | null {
  const current = company.currentResearch;
  if (current === null) {
    return null;
  }
  if (current === "mastery") {
    return `Mastery ${company.mastery.level + 1}`;
  }
  return content.research(current)?.name ?? current;
}
