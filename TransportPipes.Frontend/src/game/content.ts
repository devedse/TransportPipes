import type {
  BuildingDefinition,
  CompanyDto,
  GameContentDto,
  PipeTierDefinition,
  ResearchDefinition,
  ResourceDefinition,
} from "../api/contracts";

/** The game content (resources, buildings, pipes, progression) with the same formulas as the server */
export class Content {
  readonly buildings: BuildingDefinition[];
  readonly resources: ResourceDefinition[];
  readonly pipeTiers: PipeTierDefinition[];
  private readonly buildingById = new Map<string, BuildingDefinition>();
  private readonly resourceById = new Map<string, ResourceDefinition>();
  private readonly researchById = new Map<string, ResearchDefinition>();

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
    return id === "research" ? "Research" : (this.resourceById.get(id)?.name ?? id);
  }

  resourceColor(id: string): string {
    return this.resourceById.get(id)?.color ?? "#ffffff";
  }

  /** Output multiplier at a building level: +10% per level, milestones double */
  outputMultiplier(level: number): number {
    const p = this.progression;
    const milestones = p.milestones.filter((t) => level >= t).length;
    return Math.pow(p.outputPerLevel, level - 1) * Math.pow(2, milestones);
  }

  /** Gold to upgrade from this level to the next */
  upgradeCost(definition: BuildingDefinition, level: number): number {
    return Math.round(
      Math.max(definition.cost.gold, 10) * Math.pow(this.progression.costPerLevel, level - 1),
    );
  }

  /** Gold for another copy when you already own some */
  buildCost(definition: BuildingDefinition, copiesOwned: number): number {
    return Math.round(definition.cost.gold * Math.pow(this.progression.costPerCopy, copiesOwned));
  }

  nextMilestone(level: number): number | null {
    return this.progression.milestones.find((t) => t > level) ?? null;
  }

  xpForLevel(level: number): number {
    return this.progression.xpForLevel[level] ?? Infinity;
  }

  research(id: string): ResearchDefinition | undefined {
    return this.researchById.get(id);
  }

  get researchList(): ResearchDefinition[] {
    return this.dto.research;
  }

  /** Why a building can't be built yet, or null */
  lockReason(
    definition: BuildingDefinition,
    company: Pick<CompanyDto, "level" | "researched">,
    era: number,
  ): string | null {
    if (!definition.buildable) {
      return "Can't be built";
    }
    if (definition.unlock.era > era) {
      return `Era ${definition.unlock.era}`;
    }
    if (company.level < definition.unlock.level) {
      return `Level ${definition.unlock.level}`;
    }
    if (definition.unlock.research !== null && !company.researched.includes(definition.unlock.research)) {
      return `Research ${this.research(definition.unlock.research)?.name ?? definition.unlock.research}`;
    }
    if (definition.layer === "deep") {
      return "Deep Level";
    }
    return null;
  }
}

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
