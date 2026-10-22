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

    /// <summary>The residents who sized this stage's made goods, with their share: a part comes off when its resident stops (F4)</summary>
    public Dictionary<long, double> Counted { get; set; } = new();

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

    /// <summary>Dealt land: what goods a player doesn't make itself count toward its credit (bought from a House)</summary>
    private const double BoughtCredit = 0.25;
    private const double AllowanceHoursPerDay = 2;
    private const double AllowanceBankDays = 3;
    private const double LandmarkHours = 6;
    private static readonly TimeSpan FoundryOpensAfter = TimeSpan.FromDays(14);
    private static readonly TimeSpan DeliveringWithin = TimeSpan.FromDays(3);

    /// <summary>A counted resident's part of a basket comes off after this long without a session (F4)</summary>
    private static readonly TimeSpan BasketShrinksAfter = TimeSpan.FromDays(3);

    /// <summary>A stage's made goods are this many hours of the station's spare of them (at least of one level-1 building)</summary>
    private static double StageHours(ProjectStage stage) => stage switch
    {
        ProjectStage.WaterTower => 4,
        ProjectStage.Workshop => 6,
        _ => 9,
    };

    /// <summary>The keystone is this many hours of one level-1 building that makes it</summary>
    private const double KeystoneHours = 6;

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

    /// <summary>
    /// The players who count for a project: active and awake, and only one of several accounts on one device (an alt
    /// never makes a basket bigger)
    /// </summary>
    private List<Company> CountedResidents(StationProject project)
    {
        var counted = new List<Company>();
        foreach (var resident in ProjectResidents(project).Where(t => !t.Asleep && IsActive(t)).OrderBy(t => t.Id))
        {
            if (!counted.Any(t => SameDevice(t, resident)))
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

    /// <summary>Whether a building could stand somewhere on a company's estate (any parcel: it can buy the rest), by its placement rules</summary>
    private bool FitsOnEstate(Company company, BuildingDefinition definition)
    {
        if (EstateOf(company) is not { } estate)
        {
            return false;
        }
        var terrain = definition.Placement.Terrain.Count > 0 ? definition.Placement.Terrain : BuildingPlacement.DefaultTerrain;
        for (var y = 0; y < WorldLayout.EstateSize; y++)
        {
            for (var x = 0; x < WorldLayout.EstateSize; x++)
            {
                var tile = Tile(estate.Origin.Offset(x, y));
                if (terrain.Contains(tile.Terrain) && (definition.Placement.Deposit == null || DepositMatches(definition.Placement.Deposit, tile.Deposit))
                    && (!definition.Placement.Forest || tile.Terrain == TerrainType.Forest))
                {
                    return true;
                }
            }
        }
        return false;
    }

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
    private double OneBuildingRate(string good) =>
        Content.BuildingList.Where(t => t.Buildable && t.Kind == BuildingKind.Producer && t.Out.ContainsKey(good))
            .OrderBy(t => t.Unlock.Level).ThenBy(t => t.Cost.Gold).Select(t => t.Out[good]).FirstOrDefault(1);

    /// <summary>
    /// A stage's recipe (F4): two goods the station's counted residents make now (running output that reaches a gate),
    /// filled in from what they could make with one building by the stage's horizon, goods other than energy first; and
    /// a keystone, a good nobody makes yet that one of them can make with one building on land they have or can buy
    /// (none when none qualifies). Stable per station and stage.
    /// </summary>
    private (List<string> Made, string? Keystone) Recipe(StationRef station, ProjectStage stage, List<Company> residents)
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
            .Where(t => t.Era <= Era && t.Id != "energy" && !goods.Contains(t.Id) && !everybodyMakes.Contains(t.Id) && Makers(t.Id, residents, horizon, made).Count > 0)
            .OrderBy(t => t.Tier).ThenBy(t => Order(t.Id), StringComparer.Ordinal)
            .Select(t => t.Id)
            .FirstOrDefault();
        return (goods, keystone);
    }

    private static int StableHash(string text) => text.Aggregate(17, (hash, c) => unchecked(hash * 31 + c));

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
        project.Basket = new Dictionary<string, double>();
        project.Keystone = null;
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
        var cap = BasketCapFactor * hours * 60 * income;
        var madeWorth = project.Basket.Sum(t => t.Value * BasePrice(t.Key));
        if (keystone != null)
        {
            project.Basket[keystone] = Math.Ceiling(KeystoneHours * 60 * OneBuildingRate(keystone));
            project.Keystone = keystone;
        }
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
    /// (baskets only shrink), and a keystone nobody active can make any more is chosen again (F4). A stage stands only by
    /// delivery: the last counted resident never takes the rest with it, and with nobody active the stage waits as it is
    /// (run 2: from day 12 stages stood by themselves while everyone was away, with empty baskets)
    /// </summary>
    private void UpdateBaskets()
    {
        foreach (var project in _projects.Values.Where(t => t.Stage is not (ProjectStage.Foundation or ProjectStage.Done)))
        {
            var station = StationOfKey(project.Station);
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
            var active = CountedResidents(project);
            if (project.Keystone is { } keystone && ProjectLacks(project, keystone) > 1e-6 && active.Count > 0)
            {
                var residents = active;
                var made = residents.ToDictionary(t => t.Id, t => BuildingsOf(t.Id).Where(b => !b.UnderConstruction).SelectMany(b => Content.Building(b.Type).Out.Keys).ToHashSet());
                if (Makers(keystone, residents, StageHorizon(project.Stage), made).Count == 0)
                {
                    var (_, next) = Recipe(station, project.Stage, residents);
                    project.Basket.Remove(keystone);
                    project.Keystone = next != null && !project.Basket.ContainsKey(next) ? next : null;
                    if (project.Keystone is { } chosen)
                    {
                        project.Basket[chosen] = Math.Ceiling(KeystoneHours * 60 * OneBuildingRate(chosen));
                    }
                    project.Touch();
                    Changes.Districts.Add(station.District);
                }
            }
            if (project.Basket.Count > 0 && project.Basket.Keys.All(t => ProjectLacks(project, t) <= 1e-6))
            {
                FinishStage(project);
            }
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
        var residents = CountedResidents(project);
        var made = residents.ToDictionary(t => t.Id, t => BuildingsOf(t.Id).Where(b => !b.UnderConstruction).SelectMany(b => Content.Building(b.Type).Out.Keys).ToHashSet());
        var makers = Makers(good, residents, StageHorizon(project.Stage), made)
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
