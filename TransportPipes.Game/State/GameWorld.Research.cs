using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

public sealed partial class GameWorld
{
    /// <summary>Companies seen in this period count for knowledge diffusion</summary>
    public static readonly TimeSpan ActivePeriod = TimeSpan.FromDays(7);

    /// <summary>A tech costs at most this much less when every active company has it</summary>
    public const double MaxDiffusionDiscount = 0.5;

    public bool HasResearch(Company company, string research) => company.Researched.Contains(research);

    /// <summary>The share (0..1) of the other active companies that have a tech</summary>
    public double ResearchDiffusion(Company company, string research)
    {
        var since = Now - ActivePeriod;
        var active = 0;
        var having = 0;
        foreach (var other in _companies.Values)
        {
            if (other.Id == company.Id || other.IsNpc || other.LastSeenAt < since)
            {
                continue;
            }
            active++;
            if (other.Researched.Contains(research))
            {
                having++;
            }
        }
        return active == 0 ? 0 : having / (double)active;
    }

    /// <summary>Research points a tech costs this company (cheaper when many others have it)</summary>
    public double ResearchCost(Company company, ResearchDefinition tech) =>
        Math.Round(tech.Cost * (1 - MaxDiffusionDiscount * ResearchDiffusion(company, tech.Id)) * (1 - CorporationResearchDiscount(company)));

    /// <summary>Why a company can't start a tech, or null when it can</summary>
    public string? ResearchBlocker(Company company, ResearchDefinition tech)
    {
        if (company.Researched.Contains(tech.Id))
        {
            return "Already researched.";
        }
        if (company.CurrentResearch == tech.Id)
        {
            return "Being researched.";
        }
        if (tech.Era > Era)
        {
            return $"Needs Era {tech.Era}.";
        }
        if (company.Level < tech.Level)
        {
            return $"Needs Company Level {tech.Level}.";
        }
        var missing = tech.Requires.Where(t => !company.Researched.Contains(t)).ToList();
        if (missing.Count != 0)
        {
            return $"Needs {string.Join(" and ", missing.Select(t => Content.Research.TryGetValue(t, out var r) ? r.Name : t))} first.";
        }
        if (company.CurrentResearch != null)
        {
            return "Your labs are busy with another tech.";
        }
        return null;
    }

    /// <summary>Mastery (GDD §11.6): after the whole tree, endless steps of +0.5% output keep the Labs useful</summary>
    public const string MasteryId = "mastery";
    public const double MasteryBonus = 0.005;

    public bool ResearchedEverything(Company company) => Content.ResearchList.All(t => company.Researched.Contains(t.Id));

    public static double MasteryCost(Company company) => Math.Round(20_000 * Math.Pow(1.25, company.Mastery) / 100) * 100;

    public static double MasteryMinutes(Company company) => Math.Min(24 * 60, 120 + 30 * company.Mastery);

    public CommandResult StartResearch(long companyId, string techId)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (techId == MasteryId)
        {
            return StartMastery(company);
        }
        if (!Content.Research.TryGetValue(techId, out var tech))
        {
            return CommandResult.Fail("Unknown research.");
        }
        if (ResearchBlocker(company, tech) is { } reason)
        {
            return CommandResult.Fail(reason);
        }
        var cost = ResearchCost(company, tech);
        if (company.ResearchPoints < cost - 1e-9)
        {
            return CommandResult.Fail($"You need {cost:N0} research points. Labs make them.");
        }
        company.ResearchPoints -= cost;
        company.CurrentResearch = tech.Id;
        // Planner Sam
        company.ResearchEndsAt = Now.AddMinutes(tech.Minutes * ManagerResearchFactor(company));
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    public const int ResearchQueueLength = 5;

    /// <summary>Puts a tech in the research queue (or takes it out): it starts by itself when the Labs are free and the points are there</summary>
    public CommandResult QueueResearch(long companyId, string techId, bool queued)
    {
        if (Company(companyId) is not { } company || !Content.Research.TryGetValue(techId, out _))
        {
            return CommandResult.Fail("Unknown research.");
        }
        if (!queued)
        {
            company.ResearchQueue.Remove(techId);
        }
        else
        {
            if (company.Researched.Contains(techId) || company.CurrentResearch == techId)
            {
                return CommandResult.Fail("That's researched already.");
            }
            if (!company.ResearchQueue.Contains(techId))
            {
                if (company.ResearchQueue.Count >= ResearchQueueLength)
                {
                    return CommandResult.Fail($"The queue holds {ResearchQueueLength} techs.");
                }
                company.ResearchQueue.Add(techId);
            }
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        StartQueuedResearch(company);
        return CommandResult.Ok;
    }

    /// <summary>The Labs are free: the first queued tech that can start (its prerequisites done, its points there) starts</summary>
    private void StartQueuedResearch(Company company)
    {
        if (company.CurrentResearch != null || company.ResearchQueue.Count == 0)
        {
            return;
        }
        company.ResearchQueue.RemoveAll(company.Researched.Contains);
        foreach (var techId in company.ResearchQueue.ToList())
        {
            if (Content.Research.TryGetValue(techId, out var tech) && ResearchBlocker(company, tech) == null)
            {
                if (company.ResearchPoints >= ResearchCost(company, tech) - 1e-9 && StartResearch(company.Id, techId).Success)
                {
                    company.ResearchQueue.Remove(techId);
                }
                // The first that could start waits for its points: the order is the player's
                return;
            }
        }
    }

    private CommandResult StartMastery(Company company)
    {
        if (!ResearchedEverything(company))
        {
            return CommandResult.Fail("Mastery comes after the whole research tree.");
        }
        if (company.CurrentResearch != null)
        {
            return CommandResult.Fail("Your Labs are busy with research already.");
        }
        var cost = MasteryCost(company);
        if (company.ResearchPoints < cost - 1e-9)
        {
            return CommandResult.Fail($"You need {cost:N0} research points. Labs make them.");
        }
        company.ResearchPoints -= cost;
        company.CurrentResearch = MasteryId;
        company.ResearchEndsAt = Now.AddMinutes(MasteryMinutes(company) * ManagerResearchFactor(company));
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    private void CompleteResearch(Company company)
    {
        if (company.CurrentResearch is not { } techId)
        {
            return;
        }
        if (techId == MasteryId)
        {
            company.Mastery++;
            company.CurrentResearch = null;
            company.ResearchEndsAt = null;
            company.Touch();
            Changes.Companies.Add(company.Id);
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.ResearchFinished, $"Mastery {company.Mastery}: everything makes {company.Mastery * MasteryBonus:P1} more"));
            GainXp(company, Content.Progression.XpReward("research"), "research", "Mastery");
            MarkCompanyNetworksDirty(company.Id);
            return;
        }
        company.Researched.Add(techId);
        company.CurrentResearch = null;
        company.ResearchEndsAt = null;
        company.Touch();
        Changes.Companies.Add(company.Id);
        StartQueuedResearch(company);
        if (Content.Research.TryGetValue(techId, out var tech))
        {
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.ResearchFinished, $"Research done: {tech.Name}"));
            company.Report.ResearchDone.Add(tech.Name);
            GainXp(company, Content.Progression.XpReward("research"), "research", tech.Name);
            if (tech.ThroughputBonus > 0)
            {
                // The pipes of this company carry more: their links change
                foreach (var conductor in _conductors.Values.Where(t => t.Owner == company.Id))
                {
                    MarkDirty(conductor.Pos);
                }
            }
        }
        MarkCompanyNetworksDirty(company.Id);
        if (techId is "complexes" or "megaComplexes")
        {
            MergeAllComplexes(company);
        }
        PlanNextResearch(company);
    }

    /// <summary>Finishes the current research right away (for tests and admins)</summary>
    public void FinishResearch(long companyId)
    {
        if (Company(companyId) is { } company)
        {
            CompleteResearch(company);
        }
    }

    /// <summary>The extra output (0.25 = +25%) a company's research gives a building for a resource</summary>
    public double OutputBonus(Company? company, BuildingDefinition building, string resource)
    {
        if (company == null)
        {
            return 0;
        }
        var bonus = CorporationOutputBonus(company, resource) + ManagerOutputBonus(company, building) + AchievementOutputBonus(company, building, resource)
            + ShareBonus(company) + company.Mastery * MasteryBonus;
        foreach (var techId in company.Researched)
        {
            if (Content.Research.TryGetValue(techId, out var tech))
            {
                foreach (var output in tech.OutputBonuses)
                {
                    if (output.Applies(building, resource))
                    {
                        bonus += output.Amount;
                    }
                }
            }
        }
        return bonus;
    }

    public double ThroughputBonus(long companyId) =>
        Company(companyId) is { } company ? company.Researched.Sum(t => Content.Research.TryGetValue(t, out var tech) ? tech.ThroughputBonus : 0) : 0;

    public double BandWidthBonus(Company company) =>
        company.Researched.Sum(t => Content.Research.TryGetValue(t, out var tech) ? tech.BandWidthBonus : 0);
}
