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
        Math.Round(tech.Cost * (1 - MaxDiffusionDiscount * ResearchDiffusion(company, tech.Id)));

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

    public CommandResult StartResearch(long companyId, string techId)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
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
        company.ResearchEndsAt = Now.AddMinutes(tech.Minutes);
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
        company.Researched.Add(techId);
        company.CurrentResearch = null;
        company.ResearchEndsAt = null;
        company.Touch();
        Changes.Companies.Add(company.Id);
        if (Content.Research.TryGetValue(techId, out var tech))
        {
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.ResearchFinished, $"Research done: {tech.Name}"));
            GainXp(company, Content.Progression.XpReward("research"));
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
        if (company == null || company.Researched.Count == 0)
        {
            return 0;
        }
        double bonus = 0;
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
