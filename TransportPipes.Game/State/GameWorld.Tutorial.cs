using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;

namespace TransportPipes.Game.State;

/// <summary>
/// Loady's guide through the first session (GDD §10.2): one step at a time, each done when its stat reaches the
/// amount, with a little gold as a thank-you. Checked every tick, so the next step shows right away.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>The step a company is on (null when the guide is done)</summary>
    public TutorialStep? CurrentTutorialStep(Company company) =>
        company.TutorialStep < Content.Tutorial.Count ? Content.Tutorial[company.TutorialStep] : null;

    /// <summary>
    /// How far a company is with a step's goal. built:solarPanel|windMoily counts any of them that the player built (not
    /// its starter kit), piped:… those of them that sell: in a network with its trading post or its Yard gate.
    /// </summary>
    public double TutorialProgress(Company company, TutorialStep step)
    {
        if (step.Stat.Split(':') is not [("built" or "piped") and var kind, var types])
        {
            return StatValue(company, step.Stat);
        }
        var own = BuildingsOf(company.Id).Where(t => types.Split('|').Contains(t.Type) && !company.KitBuildings.Contains(t.Id));
        return kind == "built" ? own.Count() : own.Count(t => NetworkOf(t.Id) is { } network
            && (network.Gates.Any(t => EstateOfGate(t) != null) || network.Buildings.Any(id => Building(id) is { } b && Content.Building(b.Type).Kind == BuildingKind.TradingPost)));
    }

    /// <summary>
    /// The Company Level a step waits for (its building, or the next parcel, needs it), or null when it can be done
    /// now: Loady then says to come back later instead of pointing at something locked
    /// </summary>
    public int? TutorialWaitLevel(Company company, TutorialStep step)
    {
        var level = step.Stat switch
        {
            "parcels" => NextParcel(company).Level,
            "corporation" => CorporationFoundLevel,
            // The first building of the hint the company doesn't have yet
            _ => step.HintBuildings.Where(t => CopiesOwned(company.Id, t) == 0).Select(t => Content.Buildings.TryGetValue(t, out var d) ? d.Unlock.Level : 0).FirstOrDefault(),
        };
        return level > company.Level ? level : null;
    }

    /// <summary>The steps of a chapter, and where a step is in its chapter (1-based)</summary>
    public (int Chapter, int Index, int Count) TutorialChapter(TutorialStep step)
    {
        var steps = Content.Tutorial.Where(t => t.Chapter == step.Chapter).ToList();
        return (step.Chapter, steps.IndexOf(step) + 1, steps.Count);
    }

    /// <summary>Gold a minute a company's neighbours pay it now (through the Yards)</summary>
    public double NeighbourIncome(Company company)
    {
        var income = 0.0;
        foreach (var estate in EstatesOf(company))
        {
            foreach (var (partner, resource, perMinute, selling) in YardTrades(estate))
            {
                if (selling && Company(partner) is { } buyer)
                {
                    income += perMinute * SettlementPrice(company, buyer, resource);
                }
            }
        }
        return income;
    }

    /// <summary>
    /// Why a step doesn't get done although the player did what Loady said, or null. For the neighbour step: a pipe to
    /// the Yard gate is not yet a trade (it needs something spare that a neighbour lacks).
    /// </summary>
    public string? TutorialTip(Company company, TutorialStep step)
    {
        if (step.Stat != "partners" || TutorialProgress(company, step) >= step.Amount || EstateOf(company) is not { } estate)
        {
            return null;
        }
        var residents = YardResidents(estate.Station);
        if (residents.FirstOrDefault(t => t.Company.Id == company.Id) is not { Connected: true } me)
        {
            return null;
        }
        var others = residents.Where(t => t.Company.Id != company.Id && t.Connected).ToList();
        if (others.Count == 0)
        {
            return "You're piped to the Yard, but nobody else in your station is yet: the hub takes your spare until they are.";
        }
        var spare = me.Spare.Where(t => t.Value > 0.01).Select(t => t.Key).ToHashSet();
        if (spare.Count == 0)
        {
            return "You're piped to the Yard, but your own buildings use everything you make. Build another Wind Moily: its spare energy sells to your neighbours.";
        }
        if (others.Any(t => t.Lacks.Any(l => l.Value > 0.01 && spare.Contains(l.Key))))
        {
            // They need it: the trade starts with the Yard's next round
            return null;
        }
        var names = string.Join(", ", spare.Select(t => Content.Resources[t].Name.ToLowerInvariant()));
        return $"Your neighbours don't need what you have spare ({names}) yet. The Yard shows what each of them lacks: make that, and it sells to them.";
    }

    /// <summary>Loady's guide (off in most tests: its gold would change the expected numbers)</summary>
    public bool TutorialEnabled { get; set; } = true;

    private void CheckTutorial()
    {
        if (!TutorialEnabled || Content.Tutorial.Count == 0)
        {
            return;
        }
        Dictionary<long, int>? partners = null;
        foreach (var company in _companies.Values.Where(t => !t.IsNpc && t.TutorialStep < Content.Tutorial.Count))
        {
            // Players who are far beyond the first session don't need the guide
            if (company.TutorialStep == 0 && company.Level >= 10)
            {
                company.TutorialStep = Content.Tutorial.Count;
                company.Touch();
                continue;
            }
            // Several steps can be done at once (a returning player)
            while (CurrentTutorialStep(company) is { } step)
            {
                var progress = step.Stat == "partners" ? (partners ??= PartnerCounts()).GetValueOrDefault(company.Id) : TutorialProgress(company, step);
                if (progress < step.Amount)
                {
                    break;
                }
                company.TutorialStep++;
                company.Gold += step.Gold;
                company.Touch();
                Changes.Companies.Add(company.Id);
                Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Achievement,
                    CurrentTutorialStep(company) == null ? $"Loady: You've got this! +{step.Gold:N0} gold" : $"Loady: Well done! +{step.Gold:N0} gold"));
            }
        }
    }

    /// <summary>Skips the rest of the guide</summary>
    public CommandResult SkipTutorial(long companyId)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        company.TutorialStep = Content.Tutorial.Count;
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }
}
