using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// The Landmark's storeys (after run 4: Capital SE's Landmark stood on day 17, and from then the crew had nothing to build
/// together; days 18–36 scored fun 2.83, and "come back tomorrow" was yes for 2 of 6). When the Landmark stands its first
/// storey opens, and the next the midnight after one stands, without end: a goods part by the Yard (the Landmark's own
/// basket again) and a purse of a day of the station's income, ×1.2 a storey, that anyone in the district may give to in
/// hours of their own income, as often as they like. Each storey makes its residents' pipes carry 10% more (to +50%),
/// and its plaque names who built it in hours of their own income, so an hour of a small company's stands beside a day
/// of a big one's.
/// </summary>
public sealed partial class GameWorld
{
    private const double StoreyPurseDays = 1;
    private const double StoreyPurseGrowth = 1.2;

    /// <summary>What each storey's purse grows by (for tests)</summary>
    public static double StoreyPurseGrowthFactor => StoreyPurseGrowth;
    public const double StoreyPipeBonus = 0.1;
    public const double MaxStoreyPipeBonus = 0.5;

    /// <summary>What a storey's purse still takes (0: full, or no storey open)</summary>
    public static double PurseLeft(StationProject project) =>
        project.Stage == ProjectStage.Storey ? Math.Max(0, project.Purse - project.GivenBy.Values.Sum()) : 0;

    /// <summary>Whether a stage may stand by its purse: a storey's purse is full (any other stage has none)</summary>
    private static bool PurseFull(StationProject project) => PurseLeft(project) < 0.5;

    /// <summary>What a company's station's storeys give its pipes</summary>
    public double StoreyPipeBonusOf(long companyId) =>
        Company(companyId) is { } company && EstateOf(company) is { } estate && ExistingProject(estate.Station) is { Storeys: > 0 } project
            ? Math.Min(MaxStoreyPipeBonus, StoreyPipeBonus * project.Storeys)
            : 0;

    /// <summary>
    /// Gives to the open storey's purse: hours of your own income (as often as you like, no cap), anyone in the district;
    /// when its goods are in too, it stands
    /// </summary>
    public CommandResult GiveToStorey(long companyId, double hours)
    {
        if (Company(companyId) is not { } giver || EstateOf(giver) is not { } estate)
        {
            return CommandResult.Fail("Found your company first.");
        }
        if (hours is not (1 or 6 or 24))
        {
            return CommandResult.Fail("Give an hour, six hours or a day of your income.");
        }
        // Your station's Landmark, or the one you give to from next door
        var project = ExistingProject(estate.Station) is { Stage: ProjectStage.Storey } own ? own
            : _projects.Values.FirstOrDefault(t => t.Stage == ProjectStage.Storey && StationOfKey(t.Station).District == giver.District);
        if (project == null)
        {
            return CommandResult.Fail("No storey is open near you: a Landmark's first storey opens when it stands.");
        }
        var left = PurseLeft(project);
        if (left < 0.5)
        {
            return CommandResult.Fail("Its purse is full: it stands when its goods are in.");
        }
        var gold = Math.Min(Math.Ceiling(hours * 60 * Math.Max(MinMeasuredIncome, MeasuredIncome(giver))), Math.Ceiling(left));
        if (giver.Gold < gold)
        {
            return CommandResult.Fail($"It takes {Gold(gold)}.");
        }
        giver.Gold -= gold;
        giver.Report.GoldSpent += gold;
        AddStat(giver, "goldSpent", gold);
        AddStat(giver, "given", gold);
        AddStat(giver, "pressed:give", 1);
        project.GivenBy[giver.Id] = project.GivenBy.GetValueOrDefault(giver.Id) + gold;
        project.LandmarkHelpers.Add(giver.Id);
        project.Touch();
        giver.Touch();
        Changes.Companies.Add(giver.Id);
        var station = WorldNames.Station(StationOfKey(project.Station));
        var stands = project.Basket.Count > 0 && project.Basket.Keys.All(t => ProjectLacks(project, t) <= 1e-6) && PurseFull(project);
        if (stands)
        {
            FinishStage(project);
        }
        return CommandResult.Done(stands
            ? $"Given {Gold(gold)}: {station}'s Landmark rises a storey 💛"
            : $"Given {Gold(gold)} to {station}'s Landmark, storey {project.Storeys + 1} 💛 · {Gold(PurseLeft(project))} to go");
    }

    /// <summary>A storey stood: its plaque, its pipe perk for every resident, the news, and the next storey from midnight</summary>
    private void StoreyStands(StationProject project)
    {
        project.Storeys++;
        // The plaque: gold and goods in hours of each giver's own income
        var hours = new Dictionary<long, double>();
        foreach (var (id, gold) in project.GivenBy)
        {
            hours[id] = hours.GetValueOrDefault(id) + gold / (60 * Math.Max(MinMeasuredIncome, Company(id) is { } c ? MeasuredIncome(c) : MinMeasuredIncome));
        }
        foreach (var (id, goods) in project.StageGifts)
        {
            var worth = goods.Sum(t => t.Value * PlayerPrice(t.Key));
            hours[id] = hours.GetValueOrDefault(id) + worth / (60 * Math.Max(MinMeasuredIncome, Company(id) is { } c ? MeasuredIncome(c) : MinMeasuredIncome));
        }
        project.Plaques.Add(new StoreyPlaque(project.Storeys, LastTick, hours.Where(t => t.Value >= 0.05).ToDictionary(t => t.Key, t => Math.Round(t.Value, 1))));
        project.NextStoreyAt = NextDay(LastTick);
        var bonus = Math.Min(MaxStoreyPipeBonus, StoreyPipeBonus * project.Storeys);
        var residents = ProjectResidents(project);
        foreach (var resident in residents)
        {
            var names = hours.OrderByDescending(t => t.Value).Take(3)
                .Select(t => Company(t.Key) is { } who ? $"{(who.Id == resident.Id ? "you" : NameFor(who, resident))} {HoursWords(t.Value)}" : null)
                .OfType<string>().ToList();
            Changes.Events.Add(new WorldEvent(resident.Id, WorldEventKind.Wonder,
                $"Your station's Landmark rose to storey {project.Storeys}{(names.Count > 0 ? $" ({string.Join(", ", names)})" : "")} 💛: its pipes carry +{bonus * 100:0}% now; storey {project.Storeys + 1} opens at midnight"));
            // Their pipes carry more: their links are built again
            foreach (var conductor in _conductors.Values.Where(t => t.Owner == resident.Id))
            {
                MarkDirty(conductor.Pos);
            }
            MarkCompanyNetworksDirty(resident.Id);
        }
    }

    /// <summary>"40 min", "6 h", "1.5 days" of income</summary>
    public static string HoursWords(double hours) =>
        hours < 1 ? $"{Math.Max(1, Math.Round(hours * 60)):0} min" : hours < 24 ? $"{hours:0.#} h" : $"{hours / 24:0.#} days";

    /// <summary>For the dev cheat and e2e: a company's station project to its Landmark, then one storey more, its goods and purse filled</summary>
    public void RaiseStorey(Company company)
    {
        if (EstateOf(company) is not { } estate)
        {
            return;
        }
        var station = estate.Station;
        var project = ProjectOf(station);
        for (var i = 0; i < 6 && project.Stage is not (ProjectStage.Storey or ProjectStage.Done); i++)
        {
            FinishStage(project);
        }
        if (project.Stage == ProjectStage.Done)
        {
            StartStage(project, ProjectStage.Storey);
        }
        foreach (var good in project.Basket.Keys.ToList())
        {
            project.Delivered[good] = project.Basket[good];
        }
        project.GivenBy[company.Id] = project.GivenBy.GetValueOrDefault(company.Id) + PurseLeft(project);
        FinishStage(project);
    }

    /// <summary>
    /// Once a day: a storey that waits opens (the midnight after one stood), and a Landmark that stood before the storeys
    /// existed opens its first
    /// </summary>
    /// <summary>For the dev cheat: storeys that wait open now</summary>
    public void OpenStoreysNow()
    {
        foreach (var project in _projects.Values.Where(t => t.Stage == ProjectStage.Done && t.Finished.ContainsKey(ProjectStage.Landmark)).ToList())
        {
            StartStage(project, ProjectStage.Storey);
        }
    }

    private void OpenStoreys()
    {
        foreach (var project in _projects.Values.Where(t => t.Stage == ProjectStage.Done).ToList())
        {
            var due = project.NextStoreyAt is { } at ? at <= LastTick : project.Finished.ContainsKey(ProjectStage.Landmark);
            if (due)
            {
                StartStage(project, ProjectStage.Storey);
            }
        }
    }
}
