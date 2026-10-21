using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

public enum ProjectStage
{
    Foundation,
    WaterTower,
    Workshop,
    FoundryHall,
    Landmark,
    Done,
}

/// <summary>
/// A station's project (v1.2 §12): one shared building over the chapter, Foundation → Water Tower → Workshop → Foundry
/// Hall → Landmark. It is a buyer in the Yard: it takes what its basket lacks from the residents' spare and pays the
/// hub's full price.
/// </summary>
public sealed class StationProject : Entity
{
    /// <summary>The (big) station, as "x,y,index" of its corner</summary>
    public required string Station { get; init; }
    public ProjectStage Stage { get; set; }
    public DateTime StartedAt { get; init; }
    public DateTime StageStartedAt { get; set; }

    /// <summary>The stage's basket (units per good), what came so far, and the good nobody here makes (the keystone)</summary>
    public Dictionary<string, double> Basket { get; set; } = new();
    public Dictionary<string, double> Delivered { get; set; } = new();
    public string? Keystone { get; set; }

    /// <summary>This stage's credit per company (the gold worth it delivered, the keystone counting 20% more)</summary>
    public Dictionary<long, double> Credit { get; set; } = new();

    /// <summary>What each company may still deliver (gold worth): two hours of its output a day, banked for three days</summary>
    public Dictionary<long, double> Allowance { get; set; } = new();
    public DateTime AllowanceAt { get; set; }

    /// <summary>When each company last delivered at least half an hour of its output (a delivering resident sizes the next basket)</summary>
    public Dictionary<long, DateTime> DeliveredAt { get; set; } = new();

    /// <summary>Per minute until the next round: who sends what to the project (it arrives and is paid in the meantime)</summary>
    public Dictionary<long, Dictionary<string, double>> Incoming { get; set; } = new();

    /// <summary>What the last round matched: who sends what (it arrives the round after, less when they sent less)</summary>
    public Dictionary<long, Dictionary<string, double>> Matched { get; set; } = new();

    /// <summary>The finished stages and when, and the goods the Foundry Hall's bonus lifts</summary>
    public Dictionary<ProjectStage, DateTime> Finished { get; set; } = new();
    public List<string> FoundryChains { get; set; } = new();

    /// <summary>Who delivered to the Landmark (its heirloom is theirs)</summary>
    public HashSet<long> LandmarkHelpers { get; set; } = new();

    /// <summary>The progress the map shows (in tenths: the district is sent again when it moves)</summary>
    public int ShownTenths { get; set; }
}

public sealed partial class GameWorld
{
    /// <summary>The stages' bonuses for every seat of the station (v1.2 §12): production, and the Foundry Hall on two chains</summary>
    private static readonly Dictionary<ProjectStage, double> StageBonus = new()
    {
        [ProjectStage.Foundation] = 0.03,
        [ProjectStage.WaterTower] = 0.03,
        [ProjectStage.Workshop] = 0.04,
    };

    private const double FoundryBonus = 0.10;
    private const double KeystoneCredit = 1.2;
    private const double BasketHours = 6;
    private const double FoundryHours = 9;
    private const double AllowanceHoursPerDay = 2;
    private const double AllowanceBankDays = 3;
    private const double DeliveringMinutes = 30;
    private const double LandmarkHours = 6;
    private static readonly TimeSpan FoundryOpensAfter = TimeSpan.FromDays(14);
    private static readonly TimeSpan DeliveringWithin = TimeSpan.FromDays(3);

    /// <summary>The Foundation's fixed basket: 15 minutes of a Level-3 player's energy and iron ore</summary>
    public static readonly IReadOnlyDictionary<string, double> FoundationBasket = new Dictionary<string, double> { ["energy"] = 600, ["ironOre"] = 150 };

    /// <summary>Stations' projects buy in the Yards (on in the server, off in tests about other things)</summary>
    public bool ProjectsEnabled { get; set; }

    private readonly Dictionary<string, StationProject> _projects = new();
    public IEnumerable<StationProject> Projects => _projects.Values;

    public void LoadProject(StationProject project)
    {
        _projects[project.Station] = project;
        project.Dirty = false;
    }

    private static string ProjectKey(StationRef corner) => $"{corner.District.X},{corner.District.Y},{corner.Index}";

    /// <summary>The station's project (it starts with the Foundation the first time anybody asks)</summary>
    public StationProject ProjectOf(StationRef station)
    {
        var corner = BigStation(station)[0];
        var key = ProjectKey(corner);
        if (!_projects.TryGetValue(key, out var project))
        {
            project = new StationProject
            {
                Station = key,
                Stage = ProjectStage.Foundation,
                StartedAt = LastTick,
                StageStartedAt = LastTick,
                Basket = FoundationBasket.ToDictionary(),
                AllowanceAt = LastTick,
            };
            _projects[key] = project;
            Changes.Districts.Add(corner.District);
        }
        return project;
    }

    private static StationRef StationOfKey(string key)
    {
        var parts = key.Split(',').Select(int.Parse).ToArray();
        return new StationRef(new DistrictCoord(parts[0], parts[1]), parts[2]);
    }

    /// <summary>T(L): the typical income per minute at a Company Level (a fixed table, never measured again)</summary>
    public double TypicalIncome(int level)
    {
        var table = Content.Progression.Together.TypicalIncome;
        if (table.Count == 0)
        {
            return 100 * level;
        }
        var points = table.OrderBy(t => t.Key).ToList();
        if (level <= points[0].Key)
        {
            return points[0].Value;
        }
        for (var i = 1; i < points.Count; i++)
        {
            if (level <= points[i].Key)
            {
                var (l0, v0) = (points[i - 1].Key, points[i - 1].Value);
                var (l1, v1) = (points[i].Key, points[i].Value);
                return v0 + (v1 - v0) * (level - l0) / (double)(l1 - l0);
            }
        }
        return points[^1].Value;
    }

    /// <summary>The players of a project's station</summary>
    private List<Company> ProjectResidents(StationProject project) => StationPlayers(StationOfKey(project.Station));

    /// <summary>The goods a station is at home with: what its seats' land is rich in, and what is made from that</summary>
    private HashSet<string> HomeGoods(StationRef station)
    {
        var raw = new HashSet<string>();
        foreach (var seat in BigStation(station).SelectMany(StationSeats))
        {
            var profile = Generator.Estate(seat.District, seat.Slot);
            foreach (var specialty in new[] { profile.Primary }.Concat(profile.Secondary is { } s ? [s] : []))
            {
                raw.UnionWith(specialty switch
                {
                    EstateSpecialty.WindyHeights or EstateSpecialty.SunnyFlats => ["energy"],
                    EstateSpecialty.Riverside => ["water", "sand"],
                    EstateSpecialty.CopperGulch => ["copperOre"],
                    EstateSpecialty.CoalValley => ["coal"],
                    EstateSpecialty.OldForest => ["wood"],
                    _ => ["stone"],
                });
            }
        }
        raw.Add("ironOre");
        var made = Content.Buildings.Values
            .Where(t => t.In.Count > 0 && t.In.Keys.All(raw.Contains) && t.In.Keys.Any(k => k != "energy"))
            .SelectMany(t => t.Out.Keys);
        return raw.Concat(made).ToHashSet();
    }

    /// <summary>
    /// A later stage's recipe (v1.2 §12): three goods of the stage's tier, two the station is at home with and one that no
    /// seat makes (the keystone, +20% credit); tier 3 only from Era 2
    /// </summary>
    private (List<string> Goods, string? Keystone) Recipe(StationRef station, ProjectStage stage)
    {
        var tier = stage switch
        {
            ProjectStage.WaterTower => 1,
            ProjectStage.Workshop => 2,
            _ => Era >= 2 ? 3 : 2,
        };
        var candidates = Content.Resources.Values.Where(t => t.Tier == tier && t.Era <= Era).Select(t => t.Id).Order(StringComparer.Ordinal).ToList();
        var home = HomeGoods(station);
        var seed = StableHash($"{station.District.X},{station.District.Y},{station.Index},{(int)stage}") & 0x7fffffff;
        // A stable order per station and stage (string hashes change between runs)
        string Order(string id) => $"{(StableHash(id) ^ seed) & 0xffff:x5}{id}";
        var atHome = candidates.Where(home.Contains).OrderBy(Order, StringComparer.Ordinal).Take(2).ToList();
        // A station at home with every good of the tier finds its keystone a tier up
        var away = candidates.Where(t => !home.Contains(t)).OrderBy(Order, StringComparer.Ordinal).FirstOrDefault()
            ?? Content.Resources.Values.Where(t => t.Tier == tier + 1 && t.Era <= Era && !home.Contains(t.Id)).Select(t => t.Id).OrderBy(Order, StringComparer.Ordinal).FirstOrDefault();
        var goods = atHome.ToList();
        foreach (var other in candidates.Where(t => !goods.Contains(t) && t != away).OrderBy(Order, StringComparer.Ordinal))
        {
            if (goods.Count >= (away == null ? 3 : 2))
            {
                break;
            }
            goods.Add(other);
        }
        if (away != null)
        {
            goods.Add(away);
        }
        return (goods, away);
    }

    private static int StableHash(string text) => text.Aggregate(17, (hash, c) => unchecked(hash * 31 + c));

    /// <summary>The players who count for a basket: active, and delivered half an hour of their output in the last three days (at least one)</summary>
    private List<Company> DeliveringResidents(StationProject project)
    {
        var residents = ProjectResidents(project).Where(t => !t.Asleep && IsActive(t)).ToList();
        var delivering = residents.Where(t => project.DeliveredAt.TryGetValue(t.Id, out var at) && LastTick - at < DeliveringWithin).ToList();
        return delivering.Count > 0 ? delivering : residents.OrderByDescending(t => t.Level).Take(1).ToList();
    }

    /// <summary>The next stage starts: its recipe and its basket, sized by who delivers now</summary>
    private void StartStage(StationProject project, ProjectStage stage)
    {
        var station = StationOfKey(project.Station);
        project.Stage = stage;
        project.StageStartedAt = LastTick;
        project.Delivered = new Dictionary<string, double>();
        project.Credit = new Dictionary<long, double>();
        project.Basket = new Dictionary<string, double>();
        project.Keystone = null;
        if (stage is ProjectStage.Done)
        {
            project.Touch();
            return;
        }
        var (goods, keystone) = Recipe(station, stage);
        var people = DeliveringResidents(project);
        var hours = stage == ProjectStage.FoundryHall ? FoundryHours : BasketHours;
        var count = stage == ProjectStage.FoundryHall ? Math.Max(2, people.Count) : Math.Max(1, people.Count);
        var level = people.Count > 0 ? people.Select(t => t.Level).Order().ElementAt(people.Count / 2) : 5;
        var worth = stage == ProjectStage.Landmark
            // The Landmark: a flow worth half the summed T(L) for six hours
            ? 0.5 * people.Sum(t => TypicalIncome(t.Level)) * LandmarkHours * 60
            : people.Sum(t => hours * 60 * TypicalIncome(t.Level)) + (count - people.Count) * hours * 60 * TypicalIncome(level);
        foreach (var good in goods)
        {
            project.Basket[good] = Math.Ceiling(worth / goods.Count / BasePrice(good));
        }
        project.Keystone = keystone;
        project.Touch();
    }

    /// <summary>What a project still takes of a good (for the Landmark: what fills its tank to two hours of the flow)</summary>
    private double ProjectLacks(StationProject project, string good)
    {
        if (!project.Basket.TryGetValue(good, out var basket))
        {
            return 0;
        }
        return Math.Max(0, basket - project.Delivered.GetValueOrDefault(good));
    }

    /// <summary>Whether a station's project is open for deliveries now (the Foundry Hall waits for day 14)</summary>
    private bool ProjectOpen(StationProject project) =>
        project.Stage != ProjectStage.Done && (project.Stage != ProjectStage.FoundryHall || LastTick - project.StartedAt >= FoundryOpensAfter);

    /// <summary>
    /// The project's share of a station's round (v1.2 §12): after the residents (and Houses), it takes what its basket
    /// still lacks from what is spare, from each company at most what its allowance lets; a seller's "project first"
    /// goods come to it before the Yard. Returns what each seller sends per minute.
    /// </summary>
    private Dictionary<(EstateKey Seller, string Resource), double> MatchProject(StationProject project, List<YardMember> members, bool first)
    {
        var result = new Dictionary<(EstateKey, string), double>();
        if (!ProjectOpen(project))
        {
            return result;
        }
        RefreshAllowances(project);
        var houseShare = Content.Progression.Together.NpcCap;
        foreach (var good in project.Basket.Keys.Order(StringComparer.Ordinal))
        {
            // It takes in an hour at most what is left (the round comes again within a minute while it flows)
            var lacks = ProjectLacks(project, good) / 60;
            foreach (var member in members.Where(t => t.Spare.GetValueOrDefault(good) > 1e-9 && !t.Company.Asleep && first == t.Company.ProjectFirst.Contains(good)).OrderBy(t => t.Company.Id))
            {
                if (lacks <= 1e-9)
                {
                    break;
                }
                var allowance = member.Company.IsNpc
                    // Houses deliver on their own, at most a quarter of the basket
                    ? Math.Max(0, houseShare * project.Basket[good] - project.Delivered.GetValueOrDefault($"house:{good}")) / 60
                    : project.Allowance.GetValueOrDefault(member.Company.Id) / BasePrice(good) / 60;
                var amount = Math.Min(Math.Min(member.Spare[good], lacks), allowance);
                if (amount <= 1e-9)
                {
                    continue;
                }
                Add(member.Spare, good, -amount);
                lacks -= amount;
                result[(member.Key, good)] = result.GetValueOrDefault((member.Key, good)) + amount;
            }
        }
        return result;
    }

    /// <summary>Every company's allowance grows by two hours of its output a day, banked for three days</summary>
    private void RefreshAllowances(StationProject project)
    {
        var days = (LastTick - project.AllowanceAt).TotalDays;
        project.AllowanceAt = LastTick;
        foreach (var resident in ProjectResidents(project))
        {
            var perDay = AllowanceHoursPerDay * 60 * TypicalIncome(resident.Level);
            var have = project.Allowance.TryGetValue(resident.Id, out var a) ? a : perDay;
            project.Allowance[resident.Id] = Math.Min(perDay * AllowanceBankDays, have + perDay * days);
        }
    }

    /// <summary>What was sent to the projects arrives and is paid (the hub's full price), counted as credit; finished stages give way to the next</summary>
    private void SettleProjects(double minutes)
    {
        foreach (var project in _projects.Values.Where(t => t.Incoming.Count > 0).ToList())
        {
            foreach (var (sellerId, goods) in project.Incoming)
            {
                if (Company(sellerId) is not { } seller)
                {
                    continue;
                }
                foreach (var (good, perMinute) in goods)
                {
                    var units = Math.Min(perMinute * minutes, ProjectLacks(project, good));
                    if (units <= 1e-9)
                    {
                        continue;
                    }
                    var worth = units * BasePrice(good);
                    Add(project.Delivered, good, units);
                    seller.Gold += worth;
                    seller.Report.GoldEarned += worth;
                    Add(seller.Report.Sold, good, units);
                    GainXpFromEarnings(seller, worth);
                    seller.Touch();
                    Changes.Companies.Add(seller.Id);
                    if (seller.IsNpc)
                    {
                        Add(project.Delivered, $"house:{good}", units);
                        continue;
                    }
                    project.Allowance[seller.Id] = Math.Max(0, project.Allowance.GetValueOrDefault(seller.Id) - worth);
                    project.Credit[seller.Id] = project.Credit.GetValueOrDefault(seller.Id) + worth * (good == project.Keystone ? KeystoneCredit : 1);
                    // Half an hour of its output makes a delivering resident
                    if (project.Credit[seller.Id] >= DeliveringMinutes * TypicalIncome(seller.Level) * 0.999 || project.DeliveredAt.ContainsKey(seller.Id))
                    {
                        project.DeliveredAt[seller.Id] = LastTick;
                    }
                    if (project.Stage == ProjectStage.Landmark)
                    {
                        project.LandmarkHelpers.Add(seller.Id);
                    }
                }
            }
            project.Touch();
            if (project.Basket.Count > 0 && project.Basket.Keys.All(t => ProjectLacks(project, t) <= 1e-6))
            {
                FinishStage(project);
            }
            // The map shows the project rising: every tenth, its district again
            var target = project.Basket.Sum(t => t.Value * BasePrice(t.Key));
            var tenths = target <= 0 ? 10 : (int)(10 * project.Basket.Sum(t => Math.Min(t.Value, project.Delivered.GetValueOrDefault(t.Key)) * BasePrice(t.Key)) / target);
            if (tenths != project.ShownTenths)
            {
                project.ShownTenths = tenths;
                Changes.Districts.Add(StationOfKey(project.Station).District);
            }
        }
    }

    /// <summary>A stage is done: its bonus for every seat, the news for the station, and the next stage</summary>
    private void FinishStage(StationProject project)
    {
        var done = project.Stage;
        project.Finished[done] = LastTick;
        Changes.Districts.Add(StationOfKey(project.Station).District);
        project.Incoming.Clear();
        if (done == ProjectStage.FoundryHall)
        {
            // The station's two most-traded chains
            project.FoundryChains = YardTotals(StationOfKey(project.Station)).OrderByDescending(t => t.Value * BasePrice(t.Key)).Take(2).Select(t => t.Key).ToList();
            if (project.FoundryChains.Count == 0)
            {
                project.FoundryChains = project.Basket.Keys.Take(2).ToList();
            }
        }
        var helpers = project.Credit.Keys.ToHashSet();
        foreach (var resident in ProjectResidents(project))
        {
            Changes.Events.Add(new WorldEvent(resident.Id, WorldEventKind.Wonder, done switch
            {
                ProjectStage.Landmark => $"Your station's Landmark is finished{(helpers.Contains(resident.Id) || project.LandmarkHelpers.Contains(resident.Id) ? ": its heirloom is yours" : "")}",
                ProjectStage.FoundryHall => $"Your station's Foundry Hall stands: +10% on its {string.Join(" and ", project.FoundryChains.Select(t => Content.Resources[t].Name.ToLowerInvariant()))}",
                _ => $"Your station's {StageName(done)} is built: +{StageBonus[done] * 100:0}% production for every seat",
            }));
            if (done == ProjectStage.Landmark && (helpers.Contains(resident.Id) || project.LandmarkHelpers.Contains(resident.Id)))
            {
                resident.Titles.Add($"Landmark of {WorldNames.Station(StationOfKey(project.Station))}");
                resident.Touch();
            }
        }
        // Production changes for every seat
        foreach (var resident in BigStation(StationOfKey(project.Station)).SelectMany(StationSeats).Select(t => Estate(t.District, t.Slot)?.OwnerId).OfType<long>())
        {
            MarkCompanyNetworksDirty(resident);
        }
        var next = done switch
        {
            ProjectStage.Foundation => ProjectStage.WaterTower,
            ProjectStage.WaterTower => ProjectStage.Workshop,
            ProjectStage.Workshop => ProjectStage.FoundryHall,
            ProjectStage.FoundryHall => ProjectStage.Landmark,
            _ => ProjectStage.Done,
        };
        StartStage(project, next);
        WakeCoupling();
    }

    public static string StageName(ProjectStage stage) => stage switch
    {
        ProjectStage.Foundation => "Foundation",
        ProjectStage.WaterTower => "Water Tower",
        ProjectStage.Workshop => "Workshop",
        ProjectStage.FoundryHall => "Foundry Hall",
        ProjectStage.Landmark => "Landmark",
        _ => "project",
    };

    /// <summary>The production bonus of a building's station: its finished stages, and the Foundry Hall on its two chains</summary>
    private double ProjectOutputFactor(Building building, BuildingDefinition definition)
    {
        if (EstateAt(building.Position) is not { } estate || !_projects.TryGetValue(ProjectKey(BigStation(estate.Station)[0]), out var project))
        {
            return 1;
        }
        var bonus = project.Finished.Keys.Sum(t => StageBonus.GetValueOrDefault(t));
        if (project.Finished.ContainsKey(ProjectStage.FoundryHall) && definition.Out.Keys.Any(project.FoundryChains.Contains))
        {
            bonus += FoundryBonus;
        }
        return 1 + bonus;
    }

    /// <summary>A station's project if it has started (the picker doesn't start one)</summary>
    public StationProject? ExistingProject(StationRef station) =>
        _projects.GetValueOrDefault(ProjectKey(BigStation(station)[0]));

    /// <summary>What a project's finished stages give every seat of its station (the Foundry Hall's chains aside)</summary>
    public double ProjectBonus(StationProject project) => project.Finished.Keys.Sum(t => StageBonus.GetValueOrDefault(t));

    /// <summary>The goods somebody in the station makes now (its buildings' outputs)</summary>
    public HashSet<string> StationMakes(StationRef station) =>
        BigStation(station).SelectMany(StationSeats)
            .Select(t => Estate(t.District, t.Slot)?.OwnerId)
            .OfType<long>()
            .SelectMany(BuildingsOf)
            .SelectMany(t => Content.Building(t.Type).Out.Keys)
            .ToHashSet();

    /// <summary>For tests and the dev cheat: the project's current stage is delivered now</summary>
    public void FinishStageNow(StationRef station) => FinishStage(ProjectOf(station));

    /// <summary>Switches "project first" for a good: the company's spare of it goes to its station's project before the Yard</summary>
    public CommandResult SetProjectFirst(long companyId, string resource, bool first)
    {
        if (Company(companyId) is not { } company || !Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown good.");
        }
        if (first)
        {
            company.ProjectFirst.Add(resource);
        }
        else
        {
            company.ProjectFirst.Remove(resource);
        }
        company.Touch();
        MarkTradeDirty(company.Id);
        return CommandResult.Ok;
    }
}
