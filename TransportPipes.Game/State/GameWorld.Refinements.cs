using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

/// <summary>A Refinement line: a wall ("pipes", "trading", "hubOrders") or a good, what one step adds, and how it reads</summary>
public sealed record RefineLine(string Id, string Name, double Step, bool Wall);

/// <summary>
/// Refinements (after run 3, the design team's step 6, docs/playtest-run3-design.html): research points have a use after
/// an Era's tree (run 3: Cleo banked 1.6M points by day 21 with every tech locked, while her Lab ate 400 g/min of
/// ingots). One at a time, 5k × 1.6ⁿ points and two hours each, they go on by themselves while Keep researching is on:
/// first the walls (pipes carry +10%, trading posts sell +10%, Hub Orders pay +5%), then +2% to the good the company
/// earns most from. Each line counts for every resident of the station and stops at +50%, summed over the station; a Lab
/// with nothing left to research rests and eats nothing.
/// </summary>
public sealed partial class GameWorld
{
    public const string RefinePrefix = "refine:";
    public const double RefineCap = 0.5;
    public static readonly TimeSpan RefineTime = TimeSpan.FromHours(2);
    private static readonly string[] WallLines = ["pipes", "trading", "hubOrders"];

    /// <summary>The station's Refinement of a line per company, worked out once a tick (OutputBonus asks for it per building)</summary>
    private readonly Dictionary<(long Company, string Line), double> _refined = new();
    private DateTime _refinedAt;

    public RefineLine RefineLineOf(string id) => id switch
    {
        "pipes" => new RefineLine(id, "Pipes carry", 0.10, true),
        "trading" => new RefineLine(id, "Trading posts sell", 0.10, true),
        "hubOrders" => new RefineLine(id, "Hub Orders pay", 0.05, true),
        _ => new RefineLine(id, Content.Resources.TryGetValue(id, out var r) ? r.Name : id, 0.02, false),
    };

    /// <summary>What a company's Labs refine now (null: something else, or nothing)</summary>
    public static string? Refining(Company company) =>
        company.CurrentResearch is { } current && current.StartsWith(RefinePrefix, StringComparison.Ordinal) ? current[RefinePrefix.Length..] : null;

    /// <summary>The Refinement of a line where a company lives: every resident's steps together, at most +50%</summary>
    public double Refined(Company? company, string line)
    {
        if (company == null || company.IsNpc)
        {
            return 0;
        }
        if (_refinedAt != LastTick)
        {
            _refined.Clear();
            _refinedAt = LastTick;
        }
        if (_refined.TryGetValue((company.Id, line), out var known))
        {
            return known;
        }
        var steps = RefineResidents(company).Sum(t => t.Refinements.GetValueOrDefault(line));
        return _refined[(company.Id, line)] = Math.Min(RefineCap, steps * RefineLineOf(line).Step);
    }

    /// <summary>Who shares a company's Refinements: the players of its station (itself where it has none)</summary>
    private List<Company> RefineResidents(Company company)
    {
        var residents = EstateOf(company) is { } estate ? StationPlayers(estate.Station) : [];
        if (!residents.Any(t => t.Id == company.Id))
        {
            residents.Add(company);
        }
        return residents;
    }

    /// <summary>Who in its station has researched a line, most steps first ("Bricks +6% · Cleo's research 💛")</summary>
    public List<(Company Company, int Steps)> RefinedBy(Company company, string line) =>
        RefineResidents(company).Where(t => t.Refinements.GetValueOrDefault(line) > 0)
            .Select(t => (t, t.Refinements[line])).OrderByDescending(t => t.Item2).ThenBy(t => t.t.Id).ToList();

    /// <summary>Points the next Refinement costs: 5k × 1.6ⁿ, n the company's Refinements so far (the one under way counts)</summary>
    public static double RefineCost(Company company) =>
        Math.Round(5_000 * Math.Pow(1.6, company.Refinements.Values.Sum() + (Refining(company) != null ? 1 : 0)) / 100) * 100;

    /// <summary>Whether a tech of the tree is open to a company now or being researched (points aside)</summary>
    private bool TreeOpen(Company company) =>
        Content.ResearchList.Any(t => !company.Researched.Contains(t.Id)
            && (company.CurrentResearch == t.Id || (t.Era <= Era && company.Level >= t.Level && t.Requires.All(company.Researched.Contains))));

    /// <summary>The goods a company makes, the one it earns most from first</summary>
    public List<string> MadeGoods(Company company) =>
        BuildingsOf(company.Id).Where(t => !t.UnderConstruction)
            .SelectMany(b => Content.Building(b.Type).Out.Where(t => t.Key != GameContent.ResearchPoints).Select(t => (t.Key, Worth: t.Value * b.Level * BasePrice(t.Key))))
            .GroupBy(t => t.Key).OrderByDescending(t => t.Sum(x => x.Worth)).ThenBy(t => t.Key, StringComparer.Ordinal)
            .Select(t => t.Key).ToList();

    /// <summary>
    /// The lines a company may refine now: the walls, the goods it makes, and the goods its station makes (a friend's chain
    /// is a gift), none at its cap; empty while the tree is open
    /// </summary>
    public List<string> RefineLines(Company company) =>
        company.IsNpc || TreeOpen(company) ? []
            : WallLines.Concat(MadeGoods(company)).Concat(RefineResidents(company).Where(t => t.Id != company.Id).SelectMany(MadeGoods).Distinct().Order(StringComparer.Ordinal))
                .Distinct().Where(t => Refined(company, t) < RefineCap - 1e-9).ToList();

    /// <summary>The line the Labs take next: the player's choice; else each wall once (in the station), then the best-earning good</summary>
    public string? NextRefineLine(Company company)
    {
        var lines = RefineLines(company);
        if (lines.Count == 0)
        {
            return null;
        }
        if (company.RefineChoice is { } choice && lines.Contains(choice))
        {
            return choice;
        }
        var own = MadeGoods(company);
        return lines.FirstOrDefault(t => WallLines.Contains(t) && Refined(company, t) < 1e-9 && Refining(company) != t)
            ?? own.FirstOrDefault(lines.Contains)
            ?? lines[0];
    }

    /// <summary>The line the Labs refine next, or back to "walls first, then the best-earning good" (null)</summary>
    public CommandResult ChooseRefinement(long companyId, string? line)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (line != null && !RefineLines(company).Contains(line))
        {
            return CommandResult.Fail(TreeOpen(company) ? "Refinements come after this Era's research." : "That one can't be refined further.");
        }
        company.RefineChoice = line;
        company.Touch();
        Changes.Companies.Add(company.Id);
        StartQueuedResearch(company);
        return CommandResult.Ok;
    }

    /// <summary>Starts the next Refinement when the Labs are free, the tree is done and the points are there</summary>
    private bool StartRefinement(Company company)
    {
        if (company.CurrentResearch != null || company.ResearchPoints < RefineCost(company) - 1e-9 || NextRefineLine(company) is not { } line)
        {
            return false;
        }
        company.ResearchPoints -= RefineCost(company);
        company.CurrentResearch = RefinePrefix + line;
        company.ResearchEndsAt = Now.Add(RefineTime * ManagerResearchFactor(company));
        company.Touch();
        Changes.Companies.Add(company.Id);
        return true;
    }

    /// <summary>A Refinement is done: the station's line rises, and everyone it helps hears whose research it was</summary>
    private void CompleteRefinement(Company company, string line)
    {
        company.Refinements[line] = company.Refinements.GetValueOrDefault(line) + 1;
        company.CurrentResearch = null;
        company.ResearchEndsAt = null;
        _refined.Clear();
        var refine = RefineLineOf(line);
        var now = Refined(company, line);
        var text = $"{refine.Name} +{now * 100:0}%";
        company.Report.ResearchDone.Add(text);
        Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.ResearchFinished, $"Refined: {text} for your station"));
        GainXp(company, Content.Progression.XpReward("research"), "research", text);
        foreach (var resident in RefineResidents(company))
        {
            if (resident.Id != company.Id && (refine.Wall || MadeGoods(resident).Contains(line)))
            {
                Changes.Events.Add(new WorldEvent(resident.Id, WorldEventKind.ResearchFinished, $"{text} · {NameFor(company, resident)}'s research 💛"));
            }
            if (line == "pipes")
            {
                foreach (var conductor in _conductors.Values.Where(t => t.Owner == resident.Id))
                {
                    MarkDirty(conductor.Pos);
                }
            }
            MarkCompanyNetworksDirty(resident.Id);
            resident.Touch();
            Changes.Companies.Add(resident.Id);
        }
        StartQueuedResearch(company);
    }

    /// <summary>Whether a company's Labs rest: nothing to research, no Refinement left, no Mastery (they eat nothing)</summary>
    public bool LabsRest(Company? company) =>
        company is { IsNpc: false, CurrentResearch: null } && !TreeOpen(company) && !ResearchedEverything(company) && RefineLines(company).Count == 0;

    /// <summary>Companies whose Labs rested at the last check: their networks are solved again when that changes</summary>
    private readonly HashSet<long> _labsResting = new();

    /// <summary>Every quarter of an hour, and when research ends: Labs that start or stop resting change their networks</summary>
    private void CheckRestingLabs(IEnumerable<Company>? companies = null)
    {
        foreach (var company in companies ?? _companies.Values.Where(t => !t.IsNpc))
        {
            var rests = LabsRest(company) && BuildingsOf(company.Id).Any(IsLab);
            if (rests != _labsResting.Contains(company.Id))
            {
                if (rests)
                {
                    _labsResting.Add(company.Id);
                }
                else
                {
                    _labsResting.Remove(company.Id);
                }
                MarkCompanyNetworksDirty(company.Id);
            }
        }
    }

    private bool IsLab(Building building) =>
        Content.Building(building.Type) is { ResearchPerMinute: > 0 } definition && definition.Out.All(t => t.Key == GameContent.ResearchPoints);

    /// <summary>Whether a Lab rests now (it takes no part in its network)</summary>
    public bool LabResting(Building building) => _labsResting.Contains(building.Owner) && IsLab(building);
}
