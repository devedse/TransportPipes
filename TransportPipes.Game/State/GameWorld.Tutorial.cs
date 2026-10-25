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
    /// How far a company is with a step's goal. built:solarPanel|windMoily counts any of them that the player built,
    /// piped:… those of them that sell: in a network with its trading post or its Yard gate.
    /// </summary>
    public double TutorialProgress(Company company, TutorialStep step)
    {
        if (step.Stat.Split(':') is not [("built" or "piped") and var kind, var types])
        {
            return StatValue(company, step.Stat);
        }
        var own = BuildingsOf(company.Id).Where(t => types.Split('|').Contains(t.Type));
        return kind == "built" ? own.Count() : own.Count(t => NetworkOf(t.Id) is { } network
            && (network.Gates.Any(t => EstateOfGate(t) != null) || network.Buildings.Any(id => Building(id) is { } b && Content.Building(b.Type).Kind == BuildingKind.TradingPost)));
    }

    /// <summary>
    /// The Company Level a step waits for (its building, or the next parcel, needs it), or null when it can be done
    /// now: Loady then says to come back later instead of pointing at something locked
    /// </summary>
    /// <summary>A step's buildings for this company</summary>
    public IReadOnlyList<string> TutorialBuildings(Company company, TutorialStep step) => step.HintBuildings;

    /// <summary>What Loady says</summary>
    public string TutorialText(TutorialStep step) => step.Text;

    /// <summary>The step's hint for this company: its building list without the mines its land can't hold</summary>
    public IReadOnlyDictionary<string, string> TutorialHint(Company company, TutorialStep step)
    {
        if (!step.Hint.ContainsKey("building") && !(step.Hint.TryGetValue("select", out var select) && select is "paying" or "highest"))
        {
            return step.Hint;
        }
        var hint = step.Hint.ToDictionary();
        if (step.Hint.ContainsKey("building"))
        {
            hint["building"] = string.Join(",", TutorialBuildings(company, step));
        }
        if (TutorialTarget(company, hint.GetValueOrDefault("select")) is { } target)
        {
            hint["target"] = target.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        return hint;
    }

    /// <summary>
    /// The building Loady's Show me opens for an upgrade step: "paying", where a level pays back soonest; "highest", the
    /// highest under level 10 whose next levels pay (after run 4: Loady's "upgrade it" opened a Solar Panel whose
    /// preview said it added nothing, and Mia-B called it "a bit odd to be told to upgrade something pointless")
    /// </summary>
    private Building? TutorialTarget(Company company, string? select)
    {
        if (select is not ("paying" or "highest"))
        {
            return null;
        }
        // A preview per building is a solve each: chosen once a minute
        if (_tutorialTargets.TryGetValue(company.Id, out var known) && known.Select == select && LastTick - known.At < TimeSpan.FromMinutes(1)
            && (known.Id is not { } id || Building(id) is { } still && still.Owner == company.Id))
        {
            return known.Id is { } kept ? Building(kept) : null;
        }
        var target = ChooseTutorialTarget(company, select);
        _tutorialTargets[company.Id] = (LastTick, select, target?.Id);
        return target;
    }

    private readonly Dictionary<long, (DateTime At, string Select, long? Id)> _tutorialTargets = new();

    private Building? ChooseTutorialTarget(Company company, string select)
    {
        var candidates = BuildingsOf(company.Id)
            .Where(t => !t.UnderConstruction && !IsSite(t) && Content.Building(t.Type) is { Buildable: true, Kind: not (BuildingKind.Hq or BuildingKind.Gate or BuildingKind.Decoration or BuildingKind.Storage) })
            .Where(t => select == "paying" || t.Level < 10)
            .ToList();
        var paying = candidates
            .Select(t => (Building: t, Gain: UpgradePreview(company, t, 1).Gain, Cost: UpgradeCost(t, 1)))
            .Where(t => t.Gain > 0.05)
            .ToList();
        return select == "highest"
            ? paying.OrderByDescending(t => t.Building.Level).ThenBy(t => t.Cost / t.Gain).Select(t => t.Building).FirstOrDefault()
                ?? candidates.OrderByDescending(t => t.Level).FirstOrDefault()
            : paying.OrderBy(t => t.Cost / t.Gain).Select(t => t.Building).FirstOrDefault();
    }

    public int? TutorialWaitLevel(Company company, TutorialStep step)
    {
        var level = step.Stat switch
        {
            "parcels" => NextParcel(company).Level,
            // Copper Pipes come with Company Level 8 (after run 4)
            "pipeTiers" => Content.Research.GetValueOrDefault("copperPipes")?.Grant ?? 0,
            // The first building of the hint the company doesn't have yet
            _ => TutorialBuildings(company, step).Where(t => CopiesOwned(company.Id, t) == 0).Select(t => Content.Buildings.TryGetValue(t, out var d) ? d.Unlock.Level : 0).FirstOrDefault(),
        };
        return level > company.Level ? level : null;
    }

    /// <summary>The Era a step waits for (its chapter opens when the player's town gets there), or null when it can be done now</summary>
    public int? TutorialWaitEra(Company company, TutorialStep step) => step.Era > YourEra(company) ? step.Era : null;

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
        // A building the step wants to produce something, built but stuck (F2): what holds it, and Show me's fix
        if (step.Stat.StartsWith("produced:", StringComparison.Ordinal) && TutorialProgress(company, step) < step.Amount
            && StuckHintBuilding(company, step) is { } stuck)
        {
            var reason = IdleReasonOf(stuck);
            var fix = PlanFixPipe(stuck);
            return $"Built, but stuck: its {GoodsWord(reason.Resource!)} can't leave." + (fix != null ? $" Show me lays the pipe to your {Content.Building(fix.Target!.Type).Name} ({Gold(fix.Cost)})." : "");
        }
        // Built, and not making it yet (after run 4: Loady's bubble kept "Put a Small Factory…" after Dev built it, with
        // no word why): it is still being built, waits for its materials, or lacks an input
        if (step.Stat.StartsWith("produced:", StringComparison.Ordinal) && TutorialProgress(company, step) < step.Amount
            && BuildingsOf(company.Id).Where(t => TutorialBuildings(company, step).Contains(t.Type)).OrderBy(t => t.UnderConstruction ? 1 : 0).FirstOrDefault() is { } built)
        {
            var name = Content.Building(built.Type).Name;
            if (IsSite(built))
            {
                return MaterialsLine(company, Content.Building(built.Type)) is { Short.Count: > 0 } waits
                    ? $"Your {name} is placed: it waits for {Join(waits.Short.Select(t => $"{Amount(t.Value)} {GoodsWord(t.Key)}").ToList())}, then it is built."
                    : $"Your {name} is placed: it is built when its materials are in.";
            }
            if (built.UnderConstruction)
            {
                var left = built.ConstructionEndsAt is { } ends ? Math.Max(1, Math.Ceiling((ends - LastTick).TotalMinutes)) : 1;
                return $"Your {name} is being built: {left:0} min more, then this step counts what it makes.";
            }
            if (IdleReasonOf(built) is { Cause: IdleCause.InputMissing or IdleCause.InputShort, Resource: { } missing })
            {
                return $"Your {name} stands but makes nothing yet: not enough {GoodsWord(missing)} reaches it. {IdleStatusOf(built, plan: false).Reach}".TrimEnd();
            }
        }
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
            return "You're piped to the Yard, but nobody else at your Yard is yet: the hub takes your spare until they are.";
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

    /// <summary>The step's building the company built that is stuck (its goods can't leave), if any</summary>
    private Building? StuckHintBuilding(Company company, TutorialStep step) =>
        BuildingsOf(company.Id).FirstOrDefault(t => TutorialBuildings(company, step).Contains(t.Type) && !t.UnderConstruction && IdleReasonOf(t).Cause == IdleCause.Stuck);

    /// <summary>
    /// The pipe Loady's Show me lays (F2, F3): for a stuck building of the step, the pipe that unsticks it; for the pipe
    /// step on a phone, the pipe from the player's own first panel to the trading post (no drag on a phone)
    /// </summary>
    public PipePlan? TutorialPipe(Company company, TutorialStep step)
    {
        if (step.Stat.StartsWith("produced:", StringComparison.Ordinal) && StuckHintBuilding(company, step) is { } stuck)
        {
            return PlanFixPipe(stuck);
        }
        if (step.Stat.Split(':') is ["piped", var types] && TutorialProgress(company, step) < step.Amount)
        {
            var unpiped = BuildingsOf(company.Id).FirstOrDefault(t => types.Split('|').Contains(t.Type) && !t.UnderConstruction
                && !(NetworkOf(t.Id) is { } network && network.Buildings.Any(id => Building(id) is { } b && Content.Building(b.Type).Kind == BuildingKind.TradingPost)));
            return unpiped == null ? null : PlanFixPipe(unpiped);
        }
        return null;
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
        foreach (var company in _companies.Values.Where(t => t.TutorialStep < Content.Tutorial.Count))
        {
            company.TutorialStepSince ??= LastTick;
            // Chapter 1 retires at Level 10 for everyone: the Next steps take it from there (F3)
            if (CurrentTutorialStep(company) is { Chapter: 1 } && company.Level >= ChapterOneRetiresAt)
            {
                var next = Content.Tutorial.ToList().FindIndex(t => t.Chapter > 1);
                company.TutorialStep = next < 0 ? Content.Tutorial.Count : next;
                company.TutorialStepSince = LastTick;
                company.Touch();
                Changes.Companies.Add(company.Id);
                Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Achievement, "Loady: You're past my first chapter. Your Next steps take it from here."));
                continue;
            }
            // Several steps can be done at once (a returning player): one thank-you for all of them
            var done = new List<TutorialStep>();
            while (CurrentTutorialStep(company) is { } step)
            {
                // An Era's chapter opens when the player's town gets there
                if (TutorialWaitEra(company, step) != null)
                {
                    break;
                }
                var progress = step.Stat == "partners" ? (partners ??= PartnerCounts()).GetValueOrDefault(company.Id) : TutorialProgress(company, step);
                if (progress < step.Amount)
                {
                    break;
                }
                company.TutorialStep++;
                company.TutorialStepSince = LastTick;
                company.Gold += step.Gold;
                company.Touch();
                Changes.Companies.Add(company.Id);
                done.Add(step);
            }
            if (done.Count == 1)
            {
                Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Achievement,
                    CurrentTutorialStep(company) == null ? $"Loady: You've got this! +{done[0].Gold:N0} gold" : $"Loady: Well done! +{done[0].Gold:N0} gold"));
            }
            else if (done.Count > 1)
            {
                var (first, last) = (TutorialChapter(done[0]).Index, TutorialChapter(done[^1]).Index);
                Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Achievement, $"Loady: you've done steps {first}–{last} · +{done.Sum(t => t.Gold):N0} gold"));
            }
        }
    }

    /// <summary>From this Company Level Loady's first chapter is over for everyone</summary>
    private const int ChapterOneRetiresAt = 10;

    /// <summary>Skips the rest of the guide</summary>
    public CommandResult SkipTutorial(long companyId)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        company.TutorialStep = Content.Tutorial.Count;
        company.TutorialStepSince = LastTick;
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }
}
