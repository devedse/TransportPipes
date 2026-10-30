import type { CompanyDto } from "../api/contracts";

/** How far the next Company Level is, at the current pace (seconds): it comes from the gold earned (1.4 §14) */
export function levelEta(company: CompanyDto): number | null {
  const left = company.nextLevelEarned - company.lifetimeEarnings;
  return company.earned.perMinute > 0.001 && left > 0 ? (left / company.earned.perMinute) * 60 : null;
}
