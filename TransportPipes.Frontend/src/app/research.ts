import type { CompanyDto } from "../api/contracts";
import type { Content } from "../game/content";

/** What the Labs research now, by name: a tech, Mastery, or a Refinement ("Pipes carry +10%") */
export function researchName(company: CompanyDto, content: Content): string | null {
  const current = company.currentResearch;
  if (current === null) {
    return null;
  }
  if (current === "mastery") {
    return `Mastery ${company.mastery.level + 1}`;
  }
  if (current.startsWith("refine:")) {
    const line = company.refine.lines.find((t) => t.id === company.refine.current);
    return line === undefined ? "a Refinement" : `${line.name} +${Math.round(line.step * 100)}%`;
  }
  return content.research(current)?.name ?? current;
}
