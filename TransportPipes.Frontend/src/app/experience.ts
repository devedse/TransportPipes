import type { CompanyDto } from "../api/contracts";

/** How far the next Company Level is, at the current pace */
export function levelEta(company: CompanyDto): number | null {
  const left = company.nextLevelXp - company.xp;
  return company.experience.perMinute > 0.001 && left > 0 ? (left / company.experience.perMinute) * 60 : null;
}
