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

    /// <summary>
    /// A storey of the Landmark (after run 4: once the Landmark stood on day 17, the crew had nothing shared to build):
    /// a goods part by the Yard and a purse of gold, one after another without end. After Done, so saved stages keep
    /// their numbers; between storeys the project is Done
    /// </summary>
    Storey,
}

/// <summary>A storey that stood: its number, when, and its givers' gold and goods in hours of their own income</summary>
public sealed record StoreyPlaque(int Number, DateTime At, Dictionary<long, double> Hours);

/// <summary>
/// A station's project (v1.2 §12): one shared building over the chapter, Foundation → Water Tower → Workshop → Foundry
/// Hall → Landmark. It is a buyer in the Yard: it takes what its basket lacks from the residents' spare and pays the
/// one player price.
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

    /// <summary>Since when the keystone is wanted, and the keystones this stage passed over (they don't come back in it)</summary>
    public DateTime KeystoneSince { get; set; }
    public HashSet<string> PassedKeystones { get; set; } = new();

    /// <summary>When each resident first played after the keystone was chosen (its clock starts once every active one has)</summary>
    public Dictionary<long, DateTime> KeystoneSeen { get; set; } = new();

    /// <summary>The players' own deliveries of the keystone, sampled every 6 hours (its pace: a House's quarter doesn't count)</summary>
    public List<KeystoneMark> KeystoneMarks { get; set; } = new();

    /// <summary>A two-friend keystone's pair (after run 3, step 8): the two residents who each make one of its inputs (empty: none)</summary>
    public List<long> KeystonePair { get; set; } = new();

    /// <summary>
    /// Whether the stage's keystone was chosen: a stage that opens later (the Foundry Hall) chooses it when it opens
    /// (after run 4: one was chosen and dropped twice while the Hall said "opens in 10 days")
    /// </summary>
    public bool KeystoneChosen { get; set; } = true;

    /// <summary>The keystone passed over last, and when (the station's line says so for a day)</summary>
    public string? LastPassed { get; set; }
    public DateTime? LastPassedAt { get; set; }

    /// <summary>The station keys that shared this project's Yard (a small station that leaves keeps the stages it stood with)</summary>
    public HashSet<string> JoinedStations { get; set; } = new();

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

    /// <summary>What each player brought to this stage, by good (named at both ends when it stands: after run 4)</summary>
    public Dictionary<long, Dictionary<string, double>> StageGifts { get; set; } = new();

    /// <summary>The Landmark's storeys that stand, the open storey's purse and who gave to it, and when the next opens</summary>
    public int Storeys { get; set; }
    public double Purse { get; set; }
    public Dictionary<long, double> GivenBy { get; set; } = new();
    public DateTime? NextStoreyAt { get; set; }
    public List<StoreyPlaque> Plaques { get; set; } = new();
    public List<string> FoundryChains { get; set; } = new();

    /// <summary>Who delivered to the Landmark (its heirloom is theirs)</summary>
    public HashSet<long> LandmarkHelpers { get; set; } = new();

    /// <summary>The residents who sized this stage's made goods, with their share: a part comes off when its resident stops (F4)</summary>
    public Dictionary<long, double> Counted { get; set; } = new();

    /// <summary>The progress the map shows (in tenths: the district is sent again when it moves)</summary>
    public int ShownTenths { get; set; }
}

/// <summary>What the players had delivered of a keystone at a moment</summary>
public sealed class KeystoneMark
{
    public DateTime At { get; set; }
    public double Units { get; set; }
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

    /// <summary>Dealt land: what goods a player doesn't make itself count toward its credit (bought from a House)</summary>
    private const double BoughtCredit = 0.25;
    private const double AllowanceHoursPerDay = 2;
    private const double AllowanceBankDays = 3;
    private const double LandmarkHours = 6;
    private static readonly TimeSpan FoundryOpensAfter = TimeSpan.FromDays(14);
    private static readonly TimeSpan DeliveringWithin = TimeSpan.FromDays(3);

    /// <summary>A counted resident's part of a basket comes off after this long without a session (F4)</summary>
    private static readonly TimeSpan BasketShrinksAfter = TimeSpan.FromDays(3);

    /// <summary>
    /// A keystone's clock: this long after every active resident has seen it, it is passed over unless the players' own
    /// deliveries of the last this-long would bring the rest within this long again
    /// </summary>
    private static readonly TimeSpan KeystoneStallsAfter = TimeSpan.FromDays(2);

    /// <summary>After this many keystones passed over, a stage stands on its other goods</summary>
    private const int MaxPassedKeystones = 2;

    /// <summary>A stage's made goods are this many hours of the station's spare of them (at least of one level-1 building)</summary>
    private static double StageHours(ProjectStage stage) => stage switch
    {
        ProjectStage.WaterTower => 4,
        ProjectStage.Workshop => 6,
        _ => 9,
    };

    /// <summary>The keystone is this many hours of one level-1 building that makes it</summary>
    private const double KeystoneHours = 6;

    /// <summary>
    /// A two-friend keystone (after run 3, the design team's step 8: water was the keystone in all four stations, and it
    /// needed nobody): a good made from two land goods, bricks (stone + coal) or copper ingots (copper ore + coal), three
    /// hours of one maker, from the Water Tower on
    /// </summary>
    private static readonly string[] PairGoods = ["bricks", "copperIngot"];
    private const double PairKeystoneHours = 3;

    /// <summary>Both of the pair are this Level or more (the Brickworks and the Smelter unlock then)</summary>
    private const int PairLevel = 6;

    /// <summary>The whole basket is worth at most this many times the stage's hours of the delivering residents' summed I(c)</summary>
    private const double BasketCapFactor = 1.5;

    /// <summary>The Company Level a stage's goods may need to be made (one building unlocked by then)</summary>
    private static int StageHorizon(ProjectStage stage) => stage switch
    {
        ProjectStage.WaterTower => 5,
        ProjectStage.Workshop => 7,
        _ => 10,
    };

    /// <summary>How often a stage with an empty basket (nobody counted when it started) tries to size itself again</summary>
    private static readonly TimeSpan EmptyStageRetry = TimeSpan.FromHours(1);

    /// <summary>A good of a basket is done at this share, or with less than a unit left (no 18-hour tail, F4)</summary>
    private const double DoneShare = 0.995;

    /// <summary>The Foundation's fixed basket: about a quarter of an hour of a kit's energy for a few residents (F4: 900 energy)</summary>
    public static readonly IReadOnlyDictionary<string, double> FoundationBasket = new Dictionary<string, double> { ["energy"] = 900 };

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
        var big = BigStation(station);
        var corner = big[0];
        var key = ProjectKey(corner);
        if (!_projects.TryGetValue(key, out var project))
        {
            // A small station that shared a Yard and left keeps the stages it stood with (run 3: Capital W began again
            // at the Foundation on day 18 when its neighbour's held seats lapsed, and lost its Water Tower)
            var shared = _projects.Values.FirstOrDefault(t => t.JoinedStations.Contains(key));
            project = new StationProject
            {
                Station = key,
                Stage = ProjectStage.Foundation,
                StartedAt = shared?.StartedAt ?? LastTick,
                StageStartedAt = LastTick,
                Basket = FoundationBasket.ToDictionary(),
                AllowanceAt = LastTick,
            };
            _projects[key] = project;
            Changes.Districts.Add(corner.District);
            if (shared != null)
            {
                shared.JoinedStations.Remove(key);
                shared.Touch();
                // The stages every seat's bonus comes from (the Foundry Hall's chains and the Landmark stay where they stood)
                project.Finished = shared.Finished.Where(t => t.Key < ProjectStage.FoundryHall).ToDictionary();
                if (project.Finished.Count > 0)
                {
                    StartStage(project, project.Finished.Keys.Max() + 1);
                }
            }
        }
        foreach (var joined in big.Skip(1).Select(ProjectKey).Where(t => !project.JoinedStations.Contains(t)).ToList())
        {
            project.JoinedStations.Add(joined);
            project.Touch();
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

    /// <summary>
    /// The players who count for a project: active and awake, and only one of several accounts that may be one person
    /// (an alt never makes a basket bigger; friends on one Wi-Fi only make it smaller)
    /// </summary>
    private List<Company> CountedResidents(StationProject project)
    {
        var counted = new List<Company>();
        foreach (var resident in ProjectResidents(project).Where(t => !t.Asleep && IsActive(t)).OrderBy(t => t.Id))
        {
            if (!counted.Any(t => MaybeSameDevice(t, resident)))
            {
                counted.Add(resident);
            }
        }
        return counted;
    }

    /// <summary>
    /// What the station's counted residents have spare at their Yard gates now, per good: running output that reaches a
    /// gate ("makes", F4: StationMakes counted goods nobody could deliver)
    /// </summary>
    private Dictionary<string, double> StationSpare(StationRef station, IReadOnlyCollection<Company> residents)
    {
        var ids = residents.Select(t => t.Id).ToHashSet();
        var spare = new Dictionary<string, double>();
        foreach (var member in YardMembers(BigStation(station)[0]).Where(t => ids.Contains(t.Company.Id) && t.Network != null))
        {
            foreach (var (good, amount) in member.SpareMeasured.Where(t => t.Value > 1e-6))
            {
                Add(spare, good, amount);
            }
        }
        return spare;
    }

    /// <summary>
    /// Whether a building could stand somewhere on a company's estate (any parcel: it can buy the rest), by the same site
    /// rule the build command uses; cached per company and building until the estate's buildings change
    /// </summary>
    private bool FitsOnEstate(Company company, BuildingDefinition definition)
    {
        if (EstateOf(company) is not { } estate)
        {
            return false;
        }
        var key = (company.Id, definition.Id);
        if (_fitsOnEstate.TryGetValue(key, out var known) && known.At == estate.Origin)
        {
            return known.Fits;
        }
        var fits = false;
        for (var y = 0; y <= WorldLayout.EstateSize - definition.Size && !fits; y++)
        {
            for (var x = 0; x <= WorldLayout.EstateSize - definition.Size && !fits; x++)
            {
                fits = SiteRule(definition, estate.Origin.Offset(x, y)).Success;
            }
        }
        _fitsOnEstate[key] = (estate.Origin, fits);
        return fits;
    }

    /// <summary>The land doesn't change under an estate: whether a building fits on it, per company and building, with the estate's corner</summary>
    private readonly Dictionary<(long Company, string Building), (TilePos At, bool Fits)> _fitsOnEstate = new();

    /// <summary>
    /// The producers of a good that one of these residents could build and run by a Company Level: unlocked by then,
    /// needing only energy and what that resident makes already, standing somewhere on its estate; with who can
    /// </summary>
    private List<(BuildingDefinition Producer, Company Maker)> Makers(string good, IEnumerable<Company> residents, int horizon, IReadOnlyDictionary<long, HashSet<string>> made)
    {
        var makers = new List<(BuildingDefinition Producer, Company Maker)>();
        foreach (var producer in Content.BuildingList.Where(t => t.Buildable && t.Kind == BuildingKind.Producer && t.Out.ContainsKey(good) && t.Unlock.Level <= horizon && t.Unlock.Era <= Era && t.Unlock.Research == null))
        {
            foreach (var resident in residents)
            {
                var own = made.GetValueOrDefault(resident.Id) ?? [];
                if (producer.In.Keys.All(t => t == "energy" || own.Contains(t)) && FitsOnEstate(resident, producer))
                {
                    makers.Add((producer, resident));
                }
            }
        }
        return makers;
    }

    /// <summary>What one level-1 building that makes a good makes of it a minute (the cheapest such producer)</summary>
    public double OneBuildingRate(string good) =>
        Content.BuildingList.Where(t => t.Buildable && t.Kind == BuildingKind.Producer && t.Out.ContainsKey(good))
            .OrderBy(t => t.Unlock.Level).ThenBy(t => t.Cost.Gold).Select(t => t.Out[good]).FirstOrDefault(1);

    /// <summary>
    /// A stage's recipe (F4): two goods the station's counted residents make now (running output that reaches a gate),
    /// filled in from what they could make with one building by the stage's horizon, goods other than energy first; and
    /// a keystone, a good nobody makes yet that one of them can make with one building on land they have or can buy
    /// (none when none qualifies). Stable per station and stage.
    /// </summary>
    private (List<string> Made, string? Keystone) Recipe(StationRef station, ProjectStage stage, List<Company> residents, IReadOnlySet<string>? passed = null)
    {
        var horizon = StageHorizon(stage);
        var spare = StationSpare(station, residents);
        var made = residents.ToDictionary(t => t.Id, t => BuildingsOf(t.Id).Where(b => !b.UnderConstruction).SelectMany(b => Content.Building(b.Type).Out.Keys).ToHashSet());
        var seed = StableHash($"{station.District.X},{station.District.Y},{station.Index},{(int)stage}") & 0x7fffffff;
        string Order(string id) => $"{(StableHash(id) ^ seed) & 0xffff:x5}{id}";
        var goods = new List<string>();
        // Made now, the most valuable kind first (a later stage wants more than ore), energy last
        foreach (var good in spare.Keys.Where(t => Content.Resources.TryGetValue(t, out var r) && r.Era <= Era)
                     .OrderBy(t => t == "energy" ? 1 : 0).ThenByDescending(t => Content.Resources[t].Tier).ThenBy(Order, StringComparer.Ordinal).Take(2))
        {
            goods.Add(good);
        }
        // Too few made: what one building by the horizon would make
        if (goods.Count < 2)
        {
            foreach (var good in Content.Resources.Values.Where(t => t.Era <= Era && !goods.Contains(t.Id) && Makers(t.Id, residents, horizon, made).Count > 0)
                         .OrderBy(t => t.Id == "energy" ? 1 : 0).ThenBy(t => t.Tier).ThenBy(t => Order(t.Id), StringComparer.Ordinal).Select(t => t.Id))
            {
                if (goods.Count >= 2)
                {
                    break;
                }
                goods.Add(good);
            }
        }
        var everybodyMakes = made.Values.SelectMany(t => t).ToHashSet();
        var keystone = Content.Resources.Values
            .Where(t => t.Era <= Era && t.Id != "energy" && !goods.Contains(t.Id) && !everybodyMakes.Contains(t.Id) && passed?.Contains(t.Id) != true && Makers(t.Id, residents, horizon, made).Count > 0)
            .OrderBy(t => t.Tier).ThenBy(t => Order(t.Id), StringComparer.Ordinal)
            .Select(t => t.Id)
            .FirstOrDefault();
        return (goods, keystone);
    }

    private static int StableHash(string text) => text.Aggregate(17, (hash, c) => unchecked(hash * 31 + c));

    /// <summary>
    /// A two-friend keystone for a stage: a pair good nobody in the station makes yet and nobody can make alone, where two
    /// residents who play now and are Level 6 or more each make one of its two inputs (null: today's rule)
    /// </summary>
    private (string Good, List<long> Pair)? PairKeystone(StationRef station, ProjectStage stage, IReadOnlySet<string>? passed, IReadOnlyCollection<string> taken)
    {
        if (stage is not (ProjectStage.WaterTower or ProjectStage.Workshop or ProjectStage.FoundryHall))
        {
            return null;
        }
        var residents = StationPlayers(station);
        var lit = residents.Where(t => IsActive(t) && t.Level >= PairLevel).OrderBy(t => t.Id).ToList();
        if (lit.Count < 2)
        {
            return null;
        }
        var made = residents.ToDictionary(t => t.Id, t => MadeGoods(t).ToHashSet());
        foreach (var good in PairGoods)
        {
            if (passed?.Contains(good) == true || taken.Contains(good) || !Content.Resources.TryGetValue(good, out var resource) || resource.Era > Era
                || made.Values.Any(t => t.Contains(good)) || PairInputs(good) is not [var first, var second]
                || made.Values.Any(t => t.Contains(first) && t.Contains(second)))
            {
                continue;
            }
            if (lit.FirstOrDefault(t => made[t.Id].Contains(first)) is { } a && lit.FirstOrDefault(t => t.Id != a.Id && made[t.Id].Contains(second)) is { } b)
            {
                return (good, [a.Id, b.Id]);
            }
        }
        return null;
    }

    /// <summary>
    /// For a two-friend keystone, each of the pair counts as making what the other makes: either can build the maker and
    /// the other's input comes by the Yard (or from its House while the other is away)
    /// </summary>
    private static Dictionary<long, HashSet<string>> PairPooled(StationProject project, Dictionary<long, HashSet<string>> made)
    {
        if (project.KeystonePair.Count != 2)
        {
            return made;
        }
        var pooled = project.KeystonePair.SelectMany(t => made.GetValueOrDefault(t) ?? []).ToHashSet();
        return made.ToDictionary(t => t.Key, t => project.KeystonePair.Contains(t.Key) ? pooled : t.Value);
    }

    /// <summary>The Company Level a good of a stage may need to be made: a two-friend keystone's pair is Level 6 or more</summary>
    private static int KeystoneHorizon(StationProject project, string good) =>
        project.KeystonePair.Count == 2 && good == project.Keystone ? Math.Max(StageHorizon(project.Stage), PairLevel) : StageHorizon(project.Stage);

    /// <summary>The two goods besides energy a pair good is made of ("stone", "coal" for bricks)</summary>
    private List<string> PairInputs(string good) =>
        Content.BuildingList.Where(t => t.Buildable && t.Kind == BuildingKind.Producer && t.Out.ContainsKey(good)).OrderBy(t => t.Unlock.Level)
            .Select(t => t.In.Keys.Where(k => k != "energy").ToList()).FirstOrDefault() ?? [];

    /// <summary>A two-friend keystone's pair with the input each makes ("Ana's stone and Cleo's coal"; empty: not one)</summary>
    public List<(Company Company, string Input)> KeystonePairInputs(StationProject project) =>
        project.Keystone is { } good && project.KeystonePair.Count == 2 && PairInputs(good) is [var first, var second]
            && Company(project.KeystonePair[0]) is { } a && Company(project.KeystonePair[1]) is { } b
            ? [(a, first), (b, second)]
            : [];

    /// <summary>A keystone is chosen: its basket, and a two-friend keystone's pair hear what they make together</summary>
    private void SetKeystone(StationProject project, string? keystone, List<long>? pair)
    {
        project.Keystone = keystone;
        project.KeystonePair = pair ?? new List<long>();
        project.KeystoneSince = LastTick;
        if (keystone == null)
        {
            return;
        }
        project.Basket[keystone] = Math.Ceiling((pair != null ? PairKeystoneHours : KeystoneHours) * 60 * OneBuildingRate(keystone));
        if (KeystonePairInputs(project) is [var (a, aInput), var (b, bInput)])
        {
            var stage = StageName(project.Stage);
            var good = GoodsWord(keystone);
            var maker = Content.BuildingList.Where(t => t.Buildable && t.Kind == BuildingKind.Producer && t.Out.ContainsKey(keystone)).OrderBy(t => t.Unlock.Level).First().Name;
            Changes.Events.Add(new WorldEvent(a.Id, WorldEventKind.Wonder, $"The {stage} wants {good} from your {GoodsWord(aInput)} and {PossessiveFor(b, a)} {GoodsWord(bInput)}: one of you builds {Article(maker)} 💛", Bell: true));
            Changes.Events.Add(new WorldEvent(b.Id, WorldEventKind.Wonder, $"The {stage} wants {good} from your {GoodsWord(bInput)} and {PossessiveFor(a, b)} {GoodsWord(aInput)}: one of you builds {Article(maker)} 💛", Bell: true));
        }
    }

    /// <summary>The players who size a basket (F4): counted residents who delivered to this stage or the last within 3 days (at least one)</summary>
    private List<Company> DeliveringResidents(StationProject project)
    {
        var residents = CountedResidents(project);
        var delivering = residents.Where(t => project.DeliveredAt.TryGetValue(t.Id, out var at) && LastTick - at < DeliveringWithin).ToList();
        return delivering.Count > 0 ? delivering : residents.OrderByDescending(t => MeasuredIncome(t)).Take(1).ToList();
    }

    /// <summary>
    /// The next stage starts (F4): its recipe and its basket, sized by what the station makes and who delivers now. Made
    /// goods are the stage's hours of the station's spare of them (at least of one level-1 building), the keystone 6 h of
    /// one level-1 building; the whole basket is at most 1.5 × the stage's hours of the delivering residents' summed
    /// measured income (the made goods shrink to fit, the keystone stays).
    /// </summary>
    private void StartStage(StationProject project, ProjectStage stage)
    {
        var station = StationOfKey(project.Station);
        project.Stage = stage;
        project.StageStartedAt = LastTick;
        project.Delivered = new Dictionary<string, double>();
        project.Credit = new Dictionary<long, double>();
        project.StageGifts = new Dictionary<long, Dictionary<string, double>>();
        project.Basket = new Dictionary<string, double>();
        project.Keystone = null;
        project.KeystonePair = new List<long>();
        project.PassedKeystones = new HashSet<string>();
        project.KeystoneSeen = new Dictionary<long, DateTime>();
        project.KeystoneMarks = new List<KeystoneMark>();
        project.Counted = new Dictionary<long, double>();
        if (stage is ProjectStage.Done)
        {
            project.Touch();
            return;
        }
        var people = DeliveringResidents(project);
        var income = people.Sum(MeasuredIncome);
        foreach (var person in people)
        {
            project.Counted[person.Id] = income <= 0 ? 1.0 / people.Count : MeasuredIncome(person) / income;
        }
        if (stage == ProjectStage.Storey)
        {
            // A storey: the Landmark's goods part again, and a purse of a day of the station's income, ×1.2 a storey
            var (storeyGoods, _) = Recipe(station, ProjectStage.Landmark, CountedResidents(project));
            var storeyWorth = 0.5 * income * LandmarkHours * 60;
            foreach (var good in storeyGoods.DefaultIfEmpty("energy"))
            {
                project.Basket[good] = Math.Ceiling(storeyWorth / Math.Max(1, storeyGoods.Count) / BasePrice(good));
            }
            // The purse: a day of the income of everyone in the station who played this week, not only who delivered
            var since = LastTick - ActivePeriod;
            var stationIncome = ProjectResidents(project).Where(t => !t.Asleep && t.LastSeenAt >= since).Sum(MeasuredIncome);
            project.Purse = Math.Ceiling(StoreyPurseDays * 24 * 60 * Math.Max(MinMeasuredIncome, stationIncome) * Math.Pow(StoreyPurseGrowth, project.Storeys));
            project.GivenBy = new Dictionary<long, double>();
            project.NextStoreyAt = null;
            foreach (var resident in ProjectResidents(project))
            {
                Changes.Events.Add(new WorldEvent(resident.Id, WorldEventKind.Wonder,
                    $"Your station's Landmark opens storey {project.Storeys + 1}: its goods by the Yard, and a purse of {Gold(project.Purse)} anyone may give to"));
            }
            project.Touch();
            return;
        }
        if (stage == ProjectStage.Landmark)
        {
            // The Landmark: half the summed measured income for six hours, in the station's two most valuable goods
            var (goods, _) = Recipe(station, stage, CountedResidents(project));
            var worth = 0.5 * income * LandmarkHours * 60;
            foreach (var good in goods.DefaultIfEmpty("energy"))
            {
                project.Basket[good] = Math.Ceiling(worth / Math.Max(1, goods.Count) / BasePrice(good));
            }
            project.Touch();
            return;
        }
        var (made, keystone) = Recipe(station, stage, CountedResidents(project));
        var spare = StationSpare(station, CountedResidents(project));
        var hours = StageHours(stage);
        foreach (var good in made)
        {
            project.Basket[good] = Math.Ceiling(hours * 60 * Math.Max(spare.GetValueOrDefault(good), OneBuildingRate(good)));
        }
        project.KeystoneChosen = false;
        if (ProjectOpen(project))
        {
            ChooseKeystone(project, keystone);
        }
        project.Touch();
    }

    /// <summary>
    /// A stage's keystone, chosen when it opens: a two-friend one from the Water Tower on (after run 3, step 8), else
    /// today's rule; the made goods shrink so the whole basket stays within its cap
    /// </summary>
    private void ChooseKeystone(StationProject project, string? keystone)
    {
        var station = StationOfKey(project.Station);
        var made = project.Basket.Keys.ToList();
        var income = DeliveringResidents(project).Sum(MeasuredIncome);
        var cap = BasketCapFactor * StageHours(project.Stage) * 60 * income;
        var madeWorth = project.Basket.Sum(t => t.Value * BasePrice(t.Key));
        if (PairKeystone(station, project.Stage, null, made) is { } pair)
        {
            SetKeystone(project, pair.Good, pair.Pair);
            keystone = pair.Good;
        }
        else if (keystone != null && !project.Basket.ContainsKey(keystone))
        {
            SetKeystone(project, keystone, null);
        }
        else
        {
            keystone = null;
        }
        project.KeystoneChosen = true;
        var keystoneWorth = keystone != null ? project.Basket[keystone] * BasePrice(keystone) : 0;
        if (madeWorth > 1e-9 && madeWorth + keystoneWorth > cap)
        {
            var factor = Math.Max(0, cap - keystoneWorth) / madeWorth;
            foreach (var good in made)
            {
                project.Basket[good] = Math.Max(1, Math.Ceiling(project.Basket[good] * factor));
            }
        }
        project.Touch();
    }

    /// <summary>
    /// Once a day: a counted resident who hasn't played for 3 days takes its part of what the made goods still lack with it
    /// (baskets only shrink, F4; the keystone moves on in <see cref="CheckKeystones"/>). A stage stands only by
    /// delivery: the last counted resident never takes the rest with it, and with nobody active the stage waits as it is
    /// (run 2: from day 12 stages stood by themselves while everyone was away, with empty baskets)
    /// </summary>
    private void UpdateBaskets()
    {
        foreach (var project in _projects.Values.Where(t => t.Stage is not (ProjectStage.Foundation or ProjectStage.Done)))
        {
            foreach (var (residentId, share) in project.Counted.ToList())
            {
                if (Company(residentId) is { } resident && LastTick - resident.LastSeenAt < BasketShrinksAfter && !resident.Asleep || project.Counted.Count == 1)
                {
                    continue;
                }
                var part = share / Math.Max(1e-9, project.Counted.Values.Sum());
                project.Counted.Remove(residentId);
                foreach (var good in project.Basket.Keys.Where(t => t != project.Keystone).ToList())
                {
                    var delivered = project.Delivered.GetValueOrDefault(good);
                    project.Basket[good] = delivered + Math.Ceiling(Math.Max(0, project.Basket[good] - delivered) * (1 - part));
                }
                project.Touch();
            }
            if (project.Basket.Count > 0 && project.Basket.Keys.All(t => ProjectLacks(project, t) <= 1e-6))
            {
                FinishStage(project);
            }
        }
    }

    /// <summary>
    /// Every quarter of an hour, the keystones move on (run 3: the crew's Workshop waited 27 days at 96% on water, a House's
    /// quarter of it delivered, which kept the old "nobody brought any" rule from ever passing it over). A keystone's
    /// clock starts once every active resident has played since it was chosen, so a stage doesn't decide for friends who
    /// are away; two days on, it is passed over unless the players' own deliveries of the last two days would bring the
    /// rest within two more (a House's quarter and a trickle don't hold a stage). One nobody active can make any more goes
    /// at once. A passed keystone doesn't come back in that stage, after two the stage stands on its other goods, and
    /// every resident hears it.
    /// </summary>
    private void CheckKeystones()
    {
        // A stage that opened since: its keystone now, from who lives and plays here now
        foreach (var project in _projects.Values.Where(t => !t.KeystoneChosen && t.Stage is not (ProjectStage.Foundation or ProjectStage.Done) && ProjectOpen(t)).ToList())
        {
            var (_, keystone) = Recipe(StationOfKey(project.Station), project.Stage, CountedResidents(project));
            ChooseKeystone(project, keystone);
        }
        foreach (var project in _projects.Values.Where(t => t.Keystone != null && t.Stage is not (ProjectStage.Foundation or ProjectStage.Done) && ProjectOpen(t)).ToList())
        {
            var keystone = project.Keystone!;
            // A two-friend keystone waits for both friends while they play this week, not only the last three days
            // (after run 4: Ana's and Mia-B's pair was dropped the hour Mia-B's last session was three days old)
            var active = KeystoneResidents(project);
            if (ProjectLacks(project, keystone) <= 1e-6 || active.Count == 0)
            {
                continue;
            }
            var since = KeystoneWantedSince(project);
            foreach (var resident in active.Where(t => !project.KeystoneSeen.ContainsKey(t.Id) && t.LastSeenAt > since))
            {
                // Played since it was chosen: from that session's start, or from the moment itself when they were playing
                project.KeystoneSeen[resident.Id] = resident.SessionStartedAt > since ? resident.SessionStartedAt : since;
                project.Touch();
            }
            if (project.KeystoneMarks.Count == 0 || LastTick - project.KeystoneMarks[^1].At >= TimeSpan.FromHours(6))
            {
                project.KeystoneMarks.Add(new KeystoneMark { At = LastTick, Units = PlayersDelivered(project, keystone) });
                project.KeystoneMarks.RemoveAll(t => LastTick - t.At > KeystoneStallsAfter + TimeSpan.FromDays(1));
                project.Touch();
            }
            var made = PairPooled(project, active.ToDictionary(t => t.Id, t => BuildingsOf(t.Id).Where(b => !b.UnderConstruction).SelectMany(b => Content.Building(b.Type).Out.Keys).ToHashSet()));
            if (Makers(keystone, active, KeystoneHorizon(project, keystone), made).Count == 0)
            {
                PassKeystone(project, active, "nobody here can make it now");
            }
            else if (KeystonePassesAt(project) is { } at && at <= LastTick)
            {
                PassKeystone(project, active, $"none came for {KeystoneStallsAfter.TotalDays:0} days");
            }
        }
    }

    /// <summary>Since when a stage wants its keystone</summary>
    public static DateTime KeystoneWantedSince(StationProject project) =>
        project.KeystoneSince > project.StageStartedAt ? project.KeystoneSince : project.StageStartedAt;

    /// <summary>What players (not Houses) delivered of a good to the stage</summary>
    private static double PlayersDelivered(StationProject project, string good) =>
        project.Delivered.GetValueOrDefault(good) - project.Delivered.GetValueOrDefault($"house:{good}");

    /// <summary>
    /// Who a keystone waits for, and whose makers its line names: the residents who played this week (one per device),
    /// so a friend three days away doesn't drop it or flip its line (after run 4: Ana's and Mia-B's pair was dropped
    /// the hour Mia-B's last session was three days old, and Mia's line went "you can" → "Jo's Works can" → "nobody here
    /// can make it" and back for the same good and the same residents)
    /// </summary>
    private List<Company> KeystoneResidents(StationProject project)
    {
        var since = LastTick - ActivePeriod;
        var counted = new List<Company>();
        foreach (var resident in ProjectResidents(project).Where(t => !t.IsNpc && !t.Asleep && t.LastSeenAt >= since).OrderBy(t => t.Id))
        {
            if (!counted.Any(t => SameDevice(t, resident)))
            {
                counted.Add(resident);
            }
        }
        return counted;
    }

    /// <summary>When the keystone's clock started: once every active resident had played since it was chosen (null: not yet)</summary>
    public DateTime? KeystoneClockStart(StationProject project)
    {
        var active = KeystoneResidents(project);
        return project.Keystone == null || active.Count == 0 || active.Any(t => !project.KeystoneSeen.ContainsKey(t.Id))
            ? null
            : active.Max(t => project.KeystoneSeen[t.Id]);
    }

    /// <summary>The players' pace on the keystone over the last two days, a minute (null: too early to tell)</summary>
    private double? KeystonePace(StationProject project)
    {
        if (project.Keystone is not { } keystone || project.KeystoneMarks.Count == 0)
        {
            return null;
        }
        var from = project.KeystoneMarks.LastOrDefault(t => t.At <= LastTick - KeystoneStallsAfter) ?? project.KeystoneMarks[0];
        var minutes = (LastTick - from.At).TotalMinutes;
        return minutes < 60 ? null : Math.Max(0, PlayersDelivered(project, keystone) - from.Units) / minutes;
    }

    /// <summary>
    /// When the keystone will be passed over if nothing changes: two days after its clock started, unless the players'
    /// pace brings the rest within two more days (null: its clock hasn't started, or it's on pace)
    /// </summary>
    public DateTime? KeystonePassesAt(StationProject project)
    {
        if (project.Keystone is not { } keystone || KeystoneClockStart(project) is not { } start)
        {
            return null;
        }
        var lacks = ProjectLacks(project, keystone);
        if (lacks <= 1e-6 || KeystonePace(project) is { } pace && pace > 1e-9 && lacks / pace <= KeystoneStallsAfter.TotalMinutes)
        {
            return null;
        }
        return start + KeystoneStallsAfter;
    }

    /// <summary>The keystone is passed over: it leaves the basket (what came stays paid), the next is chosen unless two went already, and every resident hears</summary>
    private void PassKeystone(StationProject project, List<Company> active, string reason)
    {
        var keystone = project.Keystone!;
        var station = StationOfKey(project.Station);
        project.PassedKeystones.Add(keystone);
        project.Basket.Remove(keystone);
        project.LastPassed = keystone;
        project.LastPassedAt = LastTick;
        string? next = null;
        List<long>? pair = null;
        if (project.PassedKeystones.Count < MaxPassedKeystones)
        {
            if (PairKeystone(station, project.Stage, project.PassedKeystones, project.Basket.Keys.ToList()) is { } friends)
            {
                (next, pair) = friends;
            }
            else
            {
                (_, next) = Recipe(station, project.Stage, active, project.PassedKeystones);
                next = next != null && !project.Basket.ContainsKey(next) ? next : null;
            }
        }
        project.KeystoneSeen = new Dictionary<long, DateTime>();
        project.KeystoneMarks = new List<KeystoneMark>();
        SetKeystone(project, next, pair);
        string Name(string good) => Content.Resources.TryGetValue(good, out var r) ? r.Name.ToLowerInvariant() : good;
        var rest = project.Basket.Keys.Where(t => ProjectLacks(project, t) > 1e-6).Select(Name).ToList();
        var then = next != null ? $"it wants {Name(next)} now"
            : rest.Count == 0 ? "it stands now"
            : $"it stands once its {(rest.Count == 1 ? rest[0] : $"{string.Join(", ", rest.Take(rest.Count - 1))} and {rest[^1]}")} {(rest.Count == 1 ? "is" : "are")} in";
        var text = $"{WorldNames.Station(station)}'s {StageName(project.Stage)} stopped waiting on {Name(keystone)} ({reason}): {then}";
        foreach (var resident in ProjectResidents(project))
        {
            Changes.Events.Add(new WorldEvent(resident.Id, WorldEventKind.Wonder, text));
        }
        project.Touch();
        Changes.Districts.Add(station.District);
        if (project.Basket.Count == 0 || project.Basket.Keys.All(t => ProjectLacks(project, t) <= 1e-6))
        {
            FinishStage(project);
        }
    }

    /// <summary>The makers of a good a project's card shows, kept for 10 minutes (the card is built on every company update)</summary>
    private readonly Dictionary<(string Station, ProjectStage Stage, string Good), (DateTime At, List<(Company Maker, BuildingDefinition Producer)> Makers)> _makersShown = new();

    /// <summary>
    /// Who in a project's station can make a good with one building by the stage's horizon, and with which building (the
    /// cheapest one each can): the keystone's card names them
    /// </summary>
    public List<(Company Maker, BuildingDefinition Producer)> MakersOf(StationProject project, string good)
    {
        var key = (project.Station, project.Stage, good);
        if (_makersShown.TryGetValue(key, out var shown) && LastTick - shown.At < TimeSpan.FromMinutes(10))
        {
            return shown.Makers;
        }
        var residents = KeystoneResidents(project);
        var made = residents.ToDictionary(t => t.Id, t => BuildingsOf(t.Id).Where(b => !b.UnderConstruction).SelectMany(b => Content.Building(b.Type).Out.Keys).ToHashSet());
        if (good == project.Keystone)
        {
            made = PairPooled(project, made);
        }
        var makers = Makers(good, residents, KeystoneHorizon(project, good), made)
            .GroupBy(t => t.Maker.Id)
            .Select(t => t.OrderBy(m => m.Producer.Unlock.Level).ThenBy(m => m.Producer.Cost.Gold).First())
            .Select(t => (t.Maker, t.Producer))
            .ToList();
        _makersShown[key] = (LastTick, makers);
        return makers;
    }

    /// <summary>What a project's card tells one resident (F4)</summary>
    /// <param name="Share">How much of the stage is in, by worth</param>
    /// <param name="MinutesLeft">At this pace (null: a good nobody brings now)</param>
    /// <param name="GoingIn">The goods this resident's spare brings now</param>
    /// <param name="ShareCapped">Its spare is more than its daily share takes</param>
    /// <param name="SharePerDay">Its daily share, in gold</param>
    /// <param name="Bonus">What the stage gives every seat when it stands (0: the Foundry Hall's chains, the Landmark)</param>
    /// <param name="YourGain">That bonus for this resident, in gold a minute</param>
    /// <param name="StationGain">That bonus for the whole station, in gold a minute</param>
    public sealed record ProjectCard(double Share, double? MinutesLeft, IReadOnlyList<string> GoingIn, bool ShareCapped, double SharePerDay, double Bonus, double YourGain, double StationGain);

    public ProjectCard ProjectCardFor(StationProject project, Company me)
    {
        var target = project.Basket.Sum(t => t.Value * BasePrice(t.Key));
        var done = project.Basket.Sum(t => Math.Min(t.Value, project.Delivered.GetValueOrDefault(t.Key)) * BasePrice(t.Key));
        var coming = new Dictionary<string, double>();
        foreach (var goods in project.Incoming.Values)
        {
            foreach (var (good, perMinute) in goods)
            {
                Add(coming, good, perMinute);
            }
        }
        double? left = 0;
        foreach (var good in project.Basket.Keys)
        {
            var lacks = ProjectLacks(project, good);
            if (lacks <= 1e-6)
            {
                continue;
            }
            var rate = coming.GetValueOrDefault(good);
            left = rate <= 1e-9 || left == null ? null : Math.Max(left.Value, lacks / rate);
        }
        var bonus = StageBonus.GetValueOrDefault(project.Stage);
        return new ProjectCard(
            target <= 0 ? 0 : done / target,
            project.Stage == ProjectStage.Done ? null : left,
            project.Incoming.GetValueOrDefault(me.Id)?.Where(t => t.Value > 1e-9).Select(t => t.Key).Order(StringComparer.Ordinal).ToList() ?? [],
            _projectCapped.Contains((project.Station, me.Id)),
            AllowancePerDay(me),
            bonus,
            bonus * MeasuredIncome(me),
            bonus * ProjectResidents(project).Sum(MeasuredIncome));
    }

    /// <summary>The players whose spare a project's round cut to their daily share (their card says so)</summary>
    private readonly HashSet<(string Station, long Company)> _projectCapped = new();

    /// <summary>The station a project belongs to (its big station's corner)</summary>
    public StationRef StationOf(StationProject project) => StationOfKey(project.Station);

    /// <summary>When a stage last finished (the "This week" row shows it for a day)</summary>
    public DateTime? LastFinished(StationProject project) => project.Finished.Count == 0 ? null : project.Finished.Values.Max();

    /// <summary>What a project still takes of a good: nothing once 99.5% of it is in, or less than a unit is left (no tail)</summary>
    private double ProjectLacks(StationProject project, string good)
    {
        if (!project.Basket.TryGetValue(good, out var basket))
        {
            return 0;
        }
        var left = basket - project.Delivered.GetValueOrDefault(good);
        return left < 1 || project.Delivered.GetValueOrDefault(good) >= basket * DoneShare ? 0 : left;
    }

    /// <summary>Whether a station's project is open for deliveries now (the Foundry Hall waits for day 14)</summary>
    private bool ProjectOpen(StationProject project) =>
        project.Stage != ProjectStage.Done && (project.Stage != ProjectStage.FoundryHall || LastTick - project.StartedAt >= FoundryOpensAfter);

    /// <summary>
    /// The project's share of a station's round (v1.2 §12, F4): after the residents (and Houses), it takes all that is
    /// offered of what its basket still lacks, up to what is left, from everybody who has some spare in the same share
    /// (the ring of colours shows it); a player offers at most its allowance's daily rate (the Foundation and the
    /// keystone take what comes); a seller's "project first" goods come to it before the Yard. Returns what each seller
    /// sends per minute.
    /// </summary>
    private Dictionary<(EstateKey Seller, string Resource), double> MatchProject(StationProject project, List<YardMember> members, bool first)
    {
        var result = new Dictionary<(EstateKey, string), double>();
        if (!ProjectOpen(project))
        {
            return result;
        }
        RefreshAllowances(project);
        if (first)
        {
            _projectCapped.RemoveWhere(t => t.Station == project.Station);
            // A stage that started while nobody counted has an empty basket: it is sized again once somebody is back (an
            // hour apart), or it would never finish
            if (project.Basket.Count == 0 && LastTick - project.StageStartedAt >= EmptyStageRetry)
            {
                if (CountedResidents(project).Count > 0)
                {
                    StartStage(project, project.Stage);
                }
                else
                {
                    project.StageStartedAt = LastTick;
                }
            }
        }
        var houseShare = Content.Progression.Together.NpcCap;
        foreach (var good in project.Basket.Keys.Order(StringComparer.Ordinal))
        {
            var lacks = ProjectLacks(project, good);
            var free = project.Stage == ProjectStage.Foundation || good == project.Keystone;
            var offers = new List<(YardMember Member, double Offer)>();
            foreach (var member in members.Where(t => t.Spare.GetValueOrDefault(good) > 1e-9 && !t.Company.Asleep && first == t.Company.ProjectFirst.Contains(good)).OrderBy(t => t.Company.Id))
            {
                var allowance = member.Company.IsNpc
                    // Houses deliver on their own, at most a quarter of the basket
                    ? Math.Max(0, houseShare * project.Basket[good] - project.Delivered.GetValueOrDefault($"house:{good}"))
                    : free ? double.MaxValue
                    : Math.Min(project.Allowance.GetValueOrDefault(member.Company.Id), AllowancePerDay(member.Company) / (24 * 60)) / PlayerPrice(good);
                var offer = Math.Min(member.Spare[good], allowance);
                if (!member.Company.IsNpc && member.Spare[good] > allowance * 1.01 && lacks > offer)
                {
                    _projectCapped.Add((project.Station, member.Company.Id));
                }
                if (offer > 1e-9)
                {
                    offers.Add((member, offer));
                }
            }
            var total = offers.Sum(t => t.Offer);
            if (total <= 1e-9 || lacks <= 1e-9)
            {
                continue;
            }
            var share = Math.Min(1, lacks / total);
            foreach (var (member, offer) in offers)
            {
                var amount = offer * share;
                Add(member.Spare, good, -amount);
                result[(member.Key, good)] = result.GetValueOrDefault((member.Key, good)) + amount;
            }
        }
        return result;
    }

    /// <summary>A player's project allowance a day: two hours of its measured income</summary>
    private double AllowancePerDay(Company company) => AllowanceHoursPerDay * 60 * MeasuredIncome(company);

    /// <summary>Every company's allowance grows by two hours of its measured income a day, banked for three days</summary>
    private void RefreshAllowances(StationProject project)
    {
        var days = (LastTick - project.AllowanceAt).TotalDays;
        project.AllowanceAt = LastTick;
        foreach (var resident in ProjectResidents(project))
        {
            var perDay = AllowancePerDay(resident);
            var have = project.Allowance.TryGetValue(resident.Id, out var a) ? a : perDay;
            project.Allowance[resident.Id] = Math.Min(perDay * AllowanceBankDays, have + perDay * days);
        }
    }

    /// <summary>What was sent to the projects arrives and is paid (the one player price), counted as credit; finished stages give way to the next</summary>
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
                    // The project pays the one player price (step 5: one price for a friend and the project)
                    var worth = units * PlayerPrice(good);
                    Add(project.Delivered, good, units);
                    AddStat(seller, "projectGold", worth);
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
                    if (project.Stage != ProjectStage.Foundation && good != project.Keystone)
                    {
                        project.Allowance[seller.Id] = Math.Max(0, project.Allowance.GetValueOrDefault(seller.Id) - worth);
                    }
                    if (!project.StageGifts.TryGetValue(seller.Id, out var gifts))
                    {
                        project.StageGifts[seller.Id] = gifts = new Dictionary<string, double>();
                    }
                    Add(gifts, good, units);
                    // Goods a player doesn't make itself (bought from a House) count a quarter toward the credit (step 5's guard)
                    var bought = Generator.DealtLand && !BuildingsOf(seller.Id).Any(t => !t.UnderConstruction && Content.Building(t.Type).Out.ContainsKey(good)) ? BoughtCredit : 1;
                    project.Credit[seller.Id] = project.Credit.GetValueOrDefault(seller.Id) + worth * bought * (good == project.Keystone ? KeystoneCredit : 1);
                    AnnounceProjectGift(seller, project, good, units / minutes);
                    // Whoever delivers to this stage (or did to the last within 3 days) sizes the next basket
                    project.DeliveredAt[seller.Id] = LastTick;
                    if (project.Stage == ProjectStage.Landmark)
                    {
                        project.LandmarkHelpers.Add(seller.Id);
                    }
                }
            }
            project.Touch();
            if (project.Basket.Count > 0 && project.Basket.Keys.All(t => ProjectLacks(project, t) <= 1e-6) && PurseFull(project))
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
        var gifts = NameStageGifts(project, done);
        // A storey's news is its own (StoreyStands): its plaque and its pipes
        foreach (var resident in done == ProjectStage.Storey ? [] : ProjectResidents(project))
        {
            // Who built it, strangers too (after run 4: Mia-B's wood finished the crew's Water Tower and no card said so)
            var by = gifts.Where(t => t.Giver != resident.Id).Take(2)
                .Select(t => Company(t.Giver) is { } giver ? $"{PossessiveFor(giver, resident)} {GoodsWord(t.Good)}" : null).OfType<string>().ToList();
            var built = by.Count > 0 ? $" ({string.Join(" and ", by)} in it 💛)" : "";
            Changes.Events.Add(new WorldEvent(resident.Id, WorldEventKind.Wonder, done switch
            {
                ProjectStage.Landmark => $"Your station's Landmark is finished{built}{(helpers.Contains(resident.Id) || project.LandmarkHelpers.Contains(resident.Id) ? ": its heirloom is yours" : "")}",
                ProjectStage.FoundryHall => $"Your station's Foundry Hall stands{built}: +10% on its {string.Join(" and ", project.FoundryChains.Select(t => Content.Resources[t].Name.ToLowerInvariant()))}",
                _ => $"Your station's {StageName(done)} is built{built}: +{StageBonus[done] * 100:0}% production for every seat",
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
        if (done == ProjectStage.Storey)
        {
            StoreyStands(project);
        }
        StartStage(project, next);
        // The Landmark's first storey opens when it stands (after run 4: nothing to build together from day 17)
        if (done == ProjectStage.Landmark)
        {
            StartStage(project, ProjectStage.Storey);
        }
        WakeCoupling();
    }

    /// <summary>
    /// A stage stood: each player's goods in it, named at both ends (after run 4: Ben's sand filled a fifth of the Water
    /// Tower and Mia-B's wood was its keystone, and neither heard; only a ticked Send was ever announced). The giver
    /// hears its share; every resident hears the biggest two; both carry it to their return cards, with a Thanks
    /// </summary>
    private List<StoodGift> NameStageGifts(StationProject project, ProjectStage stage)
    {
        var worth = project.Basket.Sum(t => t.Value * BasePrice(t.Key));
        if (worth <= 0)
        {
            return [];
        }
        var name = StageName(stage);
        var gifts = project.StageGifts
            .SelectMany(t => t.Value.Select(g => new StoodGift(t.Key, g.Key, name, g.Value * BasePrice(g.Key) / worth)))
            .Where(t => t.Share >= 0.02 && Company(t.Giver) is { IsNpc: false })
            .OrderByDescending(t => t.Share)
            .ToList();
        var residents = ProjectResidents(project);
        foreach (var gift in gifts)
        {
            var giver = Company(gift.Giver)!;
            giver.Report.StageGifts.Add(gift);
            giver.Touch();
            Changes.Events.Add(new WorldEvent(giver.Id, WorldEventKind.Neighbour,
                $"Your {GoodsWord(gift.Good)} built {ShareWords(gift.Share)} of {WorldNames.Station(StationOfKey(project.Station))}'s {name} 💛"));
        }
        foreach (var resident in residents)
        {
            foreach (var gift in gifts.Where(t => t.Giver != resident.Id).Take(2))
            {
                resident.Report.StageGifts.Add(gift);
            }
            resident.Touch();
        }
        return gifts;
    }

    /// <summary>"all", "most", "half", "a third", "a fifth", "a tenth", "12%"</summary>
    public static string ShareWords(double share) => share switch
    {
        >= 0.97 => "all",
        >= 0.6 => "most",
        >= 0.45 => "half",
        >= 0.3 => "a third",
        >= 0.22 => "a quarter",
        >= 0.17 => "a fifth",
        >= 0.09 and <= 0.11 => "a tenth",
        _ => $"{share * 100:0}%",
    };

    public static string StageName(ProjectStage stage) => stage switch
    {
        ProjectStage.Foundation => "Foundation",
        ProjectStage.WaterTower => "Water Tower",
        ProjectStage.Workshop => "Workshop",
        ProjectStage.FoundryHall => "Foundry Hall",
        ProjectStage.Landmark => "Landmark",
        ProjectStage.Storey => "Landmark's storey",
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

    /// <summary>Who in a project's station makes a good now (a finished building of theirs makes it), players who play first</summary>
    public List<Company> MakersNow(StationProject project, string good) =>
        ProjectResidents(project)
            .Where(t => !t.Asleep && BuildingsOf(t.Id).Any(b => !b.UnderConstruction && Content.Building(b.Type).Out.ContainsKey(good)))
            .OrderBy(t => IsActive(t) ? 0 : 1).ThenBy(t => t.Id)
            .ToList();

    /// <summary>For replays and the dev cheat: every project's current stage starts again, sized and chosen by today's rules</summary>
    public void RestageProjects()
    {
        foreach (var project in _projects.Values.Where(t => t.Stage is not ProjectStage.Done).ToList())
        {
            if (project.Stage == ProjectStage.Foundation)
            {
                // The Foundation's basket is fixed: what was delivered of it stays
                project.Basket = FoundationBasket.ToDictionary(t => t.Key, t => t.Value);
                project.Delivered = project.Delivered.Where(t => project.Basket.ContainsKey(t.Key)).ToDictionary(t => t.Key, t => Math.Min(t.Value, project.Basket[t.Key]));
                project.Keystone = null;
                project.Touch();
                continue;
            }
            StartStage(project, project.Stage);
        }
        WakeCoupling();
    }

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
