import type {
  BuildingDefinition,
  CompanyDto,
  GameContentDto,
  PipeTierDefinition,
  ResearchDefinition,
  ResourceDefinition,
  TrackDefinition,
} from "../api/contracts";

/** The game content (resources, buildings, pipes, progression) with the same formulas as the server */
export class Content {
  readonly buildings: BuildingDefinition[];
  readonly resources: ResourceDefinition[];
  readonly pipeTiers: PipeTierDefinition[];
  private readonly buildingById = new Map<string, BuildingDefinition>();
  private readonly resourceById = new Map<string, ResourceDefinition>();
  private readonly researchById = new Map<string, ResearchDefinition>();
  private readonly trackById = new Map<string, TrackDefinition>();

  readonly dto: GameContentDto;

  constructor(dto: GameContentDto) {
    this.dto = dto;
    this.buildings = dto.buildings;
    this.resources = dto.resourceList;
    this.pipeTiers = dto.pipeTiers;
    for (const building of dto.buildings) {
      this.buildingById.set(building.id, building);
    }
    for (const resource of dto.resourceList) {
      this.resourceById.set(resource.id, resource);
    }
    for (const tech of dto.research) {
      this.researchById.set(tech.id, tech);
    }
    for (const track of dto.tracks) {
      this.trackById.set(track.id, track);
    }
  }

  get progression() {
    return this.dto.progression;
  }

  building(id: string): BuildingDefinition | undefined {
    return this.buildingById.get(id);
  }

  resource(id: string): ResourceDefinition | undefined {
    return this.resourceById.get(id);
  }

  resourceName(id: string): string {
    return id === "research"
      ? "Research"
      : (this.resourceById.get(id)?.name ?? SPECIAL_DEPOSITS[id]?.name ?? id);
  }

  resourceColor(id: string): string {
    return this.resourceById.get(id)?.color ?? SPECIAL_DEPOSITS[id]?.color ?? "#ffffff";
  }

  /** Output multiplier at a building level: +10% per level up to the last (1.4 §9: 9.85× at level 25) */
  outputMultiplier(level: number): number {
    const p = this.progression;
    return Math.pow(p.outputPerLevel, Math.min(level, p.maxBuildingLevel) - 1);
  }

  /** Gold to upgrade from this level to the next (1.4 §9: from what the copy cost, its build price when unknown) */
  upgradeCost(definition: BuildingDefinition, level: number, copyPrice?: number): number {
    return Math.round(
      Math.max(copyPrice ?? definition.cost.gold, 10) * Math.pow(this.progression.costPerLevel, level - 1),
    );
  }

  /**
   * Gold for another copy when you already own some (every copy 7% more than the last), less the town wonder's discount
   * (costFactor): rounded as the server rounds it, so the price shown is the gold taken
   */
  buildCost(definition: BuildingDefinition, copiesOwned: number, costFactor = 1): number {
    return roundHalfEven(
      roundHalfEven(definition.cost.gold * Math.pow(this.progression.costPerCopy, copiesOwned)) * costFactor,
    );
  }

  /** The n-th extra parcel's price, less the town wonder's discount (as the server takes it) */
  parcelCost(index: number, costFactor = 1): number {
    return roundHalfEven((this.progression.parcelCosts[index] ?? 0) * costFactor);
  }

  nextMilestone(level: number): number | null {
    return this.progression.milestones.find((t) => t > level) ?? null;
  }

  /** The gold earned in all a Company Level needs (1.4 §14) */
  earnedForLevel(level: number): number {
    return this.progression.earnedForLevel[level] ?? Infinity;
  }

  /** Whether a company has a Lab already (one is enough) */
  hasLab(company: Partial<Pick<CompanyDto, "buildingCounts">>): boolean {
    return Object.entries(company.buildingCounts ?? {}).some(
      ([type, n]) => n > 0 && this.building(type)?.kind === "lab",
    );
  }

  research(id: string): ResearchDefinition | undefined {
    return this.researchById.get(id);
  }

  track(id: string): TrackDefinition | undefined {
    return this.trackById.get(id);
  }

  get tracks(): TrackDefinition[] {
    return this.dto.tracks;
  }

  /** The track a building follows (a Coal Pit has two: the first is shown) */
  trackOf(building: string): TrackDefinition | undefined {
    return this.dto.tracks.find((t) =>
      t.buildings.some((b) => b === building || b.startsWith(`${building}:`)),
    );
  }

  get researchList(): ResearchDefinition[] {
    return this.dto.research;
  }

  /** Why a building can't be built yet, or null */
  lockReason(
    definition: BuildingDefinition,
    company: Pick<CompanyDto, "level" | "research"> &
      Partial<Pick<CompanyDto, "collections" | "buildingCounts">>,
    era: number,
  ): string | null {
    if (!definition.buildable) {
      return "Can't be built";
    }
    // One Lab: its levels make research faster
    if (definition.kind === "lab" && this.hasLab(company)) {
      return "You have one";
    }
    if (definition.unlock.era > era) {
      return `Era ${definition.unlock.era}`;
    }
    // Research everyone gets at a Company Level opens the building before it (the Big Factory, as the Copper Pipes)
    const early =
      definition.unlock.research !== null && (this.research(definition.unlock.research)?.grant ?? 0) > 0
        ? this.research(definition.unlock.research)!
        : null;
    if (
      company.level < definition.unlock.level &&
      !(early !== null && company.research.researched.includes(early.id))
    ) {
      return early !== null
        ? `Level ${definition.unlock.level} or research`
        : `Level ${definition.unlock.level}`;
    }
    if (
      definition.unlock.research !== null &&
      !company.research.researched.includes(definition.unlock.research) &&
      !(early !== null && company.level >= early.grant)
    ) {
      return `Research ${this.research(definition.unlock.research)?.name ?? definition.unlock.research}`;
    }
    if (
      definition.unlock.collection !== null &&
      !(company.collections ?? []).includes(definition.unlock.collection)
    ) {
      return "Collection reward";
    }
    return null;
  }
}

/** Deposits that are not resources themselves (magma heats a Magma Tap) */
/** Rounds as the server does (a half to the even number): 22.5 is 22, 23.5 is 24 */
export function roundHalfEven(value: number): number {
  const down = Math.floor(value);
  const rest = value - down;
  return rest > 0.5 || (rest === 0.5 && down % 2 !== 0) ? down + 1 : down;
}

const SPECIAL_DEPOSITS: Record<string, { name: string; color: string }> = {
  magma: { name: "Magma", color: "#ff6a1a" },
  geothermal: { name: "Geothermal heat", color: "#e0603a" },
};

export const CATEGORY_ORDER = [
  "energy",
  "extraction",
  "processing",
  "logistics",
  "commerce",
  "company",
  "decoration",
];

export const CATEGORY_NAMES: Record<string, string> = {
  energy: "Energy",
  extraction: "Extraction",
  processing: "Processing",
  logistics: "Logistics",
  commerce: "Commerce",
  company: "Company",
  decoration: "Decoration",
  late: "Late game",
};
