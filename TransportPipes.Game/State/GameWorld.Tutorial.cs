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

    /// <summary>How far a company is with a step's goal</summary>
    public double TutorialProgress(Company company, TutorialStep step) =>
        step.Stat.StartsWith("built:", StringComparison.Ordinal)
            // built:solarPanel|windMoily counts any of them
            ? BuildingsOf(company.Id).Count(t => step.Stat["built:".Length..].Split('|').Contains(t.Type))
            : StatValue(company, step.Stat);

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

    /// <summary>Gold a minute a company's neighbours pay it now (trades and contracts over its pipes)</summary>
    public double NeighbourIncome(Company company)
    {
        var income = 0.0;
        foreach (var network in Networks)
        {
            foreach (var flow in network.Solution.Flows)
            {
                if (flow.Kind is FlowKind.Trade or FlowKind.Contract && _buildings.TryGetValue(flow.From, out var from) && from.Owner == company.Id)
                {
                    income += flow.Delivered * flow.PricePerUnit;
                }
            }
        }
        return income;
    }

    /// <summary>
    /// Why a step doesn't get done although the player did what Loady said, or null. For the neighbour step: a pipe
    /// to a neighbour is not yet a trade (it needs something spare that they need).
    /// </summary>
    public string? TutorialTip(Company company, TutorialStep step)
    {
        if (step.Stat != "partners" || TutorialProgress(company, step) >= step.Amount)
        {
            return null;
        }
        var neighbourhood = NeighbourhoodOf(company);
        var others = neighbourhood.Connected.Where(t => t != company.Id).Select(Company).OfType<Company>().ToList();
        if (others.Count == 0)
        {
            return null;
        }
        var spare = neighbourhood.Surplus.TryGetValue(company.Id, out var surplus)
            ? surplus.Where(t => t.Value > 0.01).Select(t => t.Key).ToHashSet()
            : [];
        if (spare.Count == 0)
        {
            return "You're connected, but your own buildings use everything you make. Build another Wind Moily: its spare energy sells to your neighbours.";
        }
        if (others.Any(t => neighbourhood.CanSupply(t.Id, spare)))
        {
            // They need it: the trade starts with the next tick, unless the pipe to them loses so much on the way
            // that the hub pays more (the solver then keeps selling to the hub)
            var solved = Networks.Where(t => t.Buildings.Any(b => _buildings[b].Owner == company.Id)).All(t => !t.NeedsSolve);
            return solved
                ? "They need what you make, but the pipe to them is so long that most of it is lost on the way: your hub pays you more. Connect to a closer neighbour, or lay better pipes (Copper loses half as much)."
                : null;
        }
        var names = string.Join(", ", spare.Select(t => Content.Resources[t].Name.ToLowerInvariant()));
        return $"You're connected to {others[0].Name}{(others.Count > 1 ? $" and {others.Count - 1} more" : "")}, but they don't need what you have spare ({names}). Connect to a neighbour that needs it: the Neighbours panel shows what each needs.";
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
