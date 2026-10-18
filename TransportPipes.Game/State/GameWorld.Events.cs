using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

public enum EventKind
{
    Storm,
    Heatwave,
    Drought,
    ColdSnap,
    Expo,
    Shortage,
    Discovery,
    Festival,
    EraLaunch,
}

/// <summary>A temporary multiplier with a start and an end (GDD §15)</summary>
public sealed class GameEvent
{
    public long Id { get; init; }
    public EventKind Kind { get; init; }

    /// <summary>The district it happens in; null: everywhere</summary>
    public DistrictCoord? District { get; init; }

    public DateTime StartsAt { get; init; }
    public DateTime EndsAt { get; init; }

    /// <summary>Expo and Shortage: the product</summary>
    public string? Resource { get; init; }

    public bool Announced { get; set; }
    public bool Started { get; set; }
    public bool Ended { get; set; }

    public bool ActiveAt(DateTime at) => StartsAt <= at && at < EndsAt;

    public bool Covers(DistrictCoord district) => District == null || District == district;
}

/// <summary>
/// Events and the calendar (GDD §15). Every district gets its own weather and hub events, planned a few days ahead from
/// the world seed and announced 24 hours before they start (72 hours with a Weather Station).
/// </summary>
public sealed partial class GameWorld
{
    public static readonly TimeSpan AnnounceAhead = TimeSpan.FromHours(24);
    public static readonly TimeSpan ForecastAhead = TimeSpan.FromHours(72);
    private const int PlanDays = 4;
    private const double StormDamageChance = 0.1;

    private readonly List<GameEvent> _events = new();

    /// <summary>The (district, day) pairs that have their events: "x:y:yyyy-MM-dd"</summary>
    private readonly HashSet<string> _plannedDays = new();

    public long NextEventId { get; set; } = 1;

    /// <summary>Plans weather and hub events (off in most tests, so they stay predictable)</summary>
    public bool EventsEnabled { get; set; } = true;

    public IReadOnlyList<GameEvent> Events => _events;

    public IReadOnlyCollection<string> PlannedDays => _plannedDays;

    public void LoadEvents(IEnumerable<GameEvent> events, IEnumerable<string> plannedDays)
    {
        _events.AddRange(events);
        _plannedDays.UnionWith(plannedDays);
        NextEventId = Math.Max(NextEventId, _events.Select(t => t.Id + 1).DefaultIfEmpty(1).Max());
    }

    /// <summary>Events that are running right now in a district</summary>
    public IEnumerable<GameEvent> ActiveEvents(DistrictCoord district) =>
        _events.Where(t => t.Covers(district) && t.ActiveAt(LastTick));

    private bool EventActive(DistrictCoord district, EventKind kind, string? resource = null) =>
        _events.Any(t => t.Kind == kind && t.Covers(district) && t.ActiveAt(LastTick) && (resource == null || t.Resource == resource));

    /// <summary>How far ahead a company sees the calendar</summary>
    public TimeSpan Horizon(Company company) =>
        BuildingsOf(company.Id).Any(t => !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.WeatherStation) ? ForecastAhead : AnnounceAhead;

    /// <summary>What a company sees: running events and the ones within its horizon</summary>
    public IEnumerable<GameEvent> Calendar(Company company)
    {
        var until = Now + Horizon(company);
        return _events.Where(t => t.Covers(company.District) && t.EndsAt > Now && t.StartsAt <= until).OrderBy(t => t.StartsAt);
    }

    // ---------------------------------------------------------------- Effects

    /// <summary>Output factor of a building from the events in its district (and storm damage)</summary>
    private double EventOutputFactor(Building building, BuildingDefinition definition)
    {
        var factor = building.Damaged ? 0.5 : 1;
        if (_events.Count == 0)
        {
            return factor;
        }
        var district = WorldLayout.DistrictOf(building.X, building.Y);
        if (definition.Modifier == ProductionModifier.Wind && EventActive(district, EventKind.Storm))
        {
            factor *= 2;
        }
        if (definition.Modifier == ProductionModifier.Sun && EventActive(district, EventKind.Heatwave))
        {
            factor *= 1.5;
        }
        if (definition.Out.ContainsKey("water") && EventActive(district, EventKind.Drought))
        {
            factor *= 0.5;
        }
        if (EventActive(district, EventKind.EraLaunch))
        {
            factor *= 1.1;
        }
        return factor;
    }

    /// <summary>Hub prices during a Drought (water ×3) and an Expo (×3 for its product)</summary>
    private double EventPriceFactor(DistrictCoord district, string resource)
    {
        if (_events.Count == 0)
        {
            return 1;
        }
        var factor = 1.0;
        if (resource == "water" && EventActive(district, EventKind.Drought))
        {
            factor *= 3;
        }
        if (EventActive(district, EventKind.Expo, resource))
        {
            factor *= 3;
        }
        return factor;
    }

    /// <summary>Hub demand during a Cold Snap (energy ×2) and a Heatwave (water ×2)</summary>
    private double EventDemandFactor(DistrictCoord district, string resource)
    {
        if (_events.Count == 0)
        {
            return 1;
        }
        if (resource == "energy" && EventActive(district, EventKind.ColdSnap))
        {
            return 2;
        }
        if (resource == "water" && EventActive(district, EventKind.Heatwave))
        {
            return 2;
        }
        return 1;
    }

    /// <summary>Likes count double during a Festival</summary>
    private int LikeFactor(DistrictCoord district) => EventActive(district, EventKind.Festival) ? 2 : 1;

    // ---------------------------------------------------------------- Planning

    private static string DayKey(DistrictCoord district, DateTime day) => $"{district.X}:{district.Y}:{day:yyyy-MM-dd}";

    /// <summary>Plans the events of the coming days for every open district (each day once)</summary>
    private void PlanEvents()
    {
        if (!EventsEnabled)
        {
            return;
        }
        var today = LastTick.Date;
        for (var d = 0; d < PlanDays; d++)
        {
            var day = DateTime.SpecifyKind(today.AddDays(d), DateTimeKind.Utc);
            foreach (var district in _openDistricts)
            {
                if (_plannedDays.Add(DayKey(district, day)))
                {
                    PlanDistrictDay(district, day);
                }
            }
            if (_plannedDays.Add($"world:{day:yyyy-MM-dd}"))
            {
                PlanWorldDay(day);
            }
        }
        // Forget what is long over
        var old = today.AddDays(-2);
        _events.RemoveAll(t => t.Ended && t.EndsAt < old);
        _plannedDays.RemoveWhere(t => DateTime.TryParse(t[(t.LastIndexOf(':') + 1)..], out var date) && date < old);
    }

    private void PlanDistrictDay(DistrictCoord district, DateTime day)
    {
        var dayNumber = (ulong)(day - DateTime.UnixEpoch).TotalDays;
        var random = new SeededRandom((ulong)Generator.Seed * 6151 + (ulong)(uint)district.X * 7919 + (ulong)(uint)district.Y * 104729 + dayNumber * 15485863);
        var earliest = LastTick + TimeSpan.FromHours(1);

        void Maybe(EventKind kind, double chance, int fromHour, int toHour, double hours, string? resource = null)
        {
            var roll = random.NextDouble();
            var start = day.AddHours(fromHour + random.NextDouble() * (toHour - fromHour));
            start = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0, DateTimeKind.Utc);
            // Events of the first days are planned when the world (or district) starts; nothing starts unannounced
            if (roll >= chance || start < earliest)
            {
                return;
            }
            AddEvent(new GameEvent { Id = NextEventId++, Kind = kind, District = district, StartsAt = start, EndsAt = start.AddHours(hours), Resource = resource });
        }

        var products = Content.ResourceList.Where(t => t.Era <= Era && t.Tier >= 2).Select(t => t.Id).ToList();
        var basics = Content.ResourceList.Where(t => t.Era <= Era && t.Tier <= 2 && t.Id != "water").Select(t => t.Id).ToList();
        Maybe(EventKind.Storm, 1 / 7.0, 0, 20, 6);
        Maybe(EventKind.Heatwave, 1 / 14.0, 9, 13, 10);
        Maybe(EventKind.Drought, 1 / 30.0, 0, 12, 24);
        Maybe(EventKind.ColdSnap, 1 / 30.0, 0, 12, 12);
        Maybe(EventKind.Expo, 1 / 7.0, 8, 18, 6, products.Count == 0 ? null : products[random.Next(products.Count)]);
        Maybe(EventKind.Shortage, 3 / 7.0, 6, 20, 8, basics.Count == 0 ? null : basics[random.Next(basics.Count)]);
    }

    /// <summary>Festivals on holidays, for everyone</summary>
    private void PlanWorldDay(DateTime day)
    {
        var festivals = new[] { (4, 27), (6, 21), (10, 31), (12, 24), (12, 25), (12, 26), (12, 31), (1, 1) };
        if (festivals.Contains((day.Month, day.Day)) && day.AddDays(1) > LastTick)
        {
            AddEvent(new GameEvent { Id = NextEventId++, Kind = EventKind.Festival, District = null, StartsAt = day, EndsAt = day.AddDays(1) });
        }
    }

    private void AddEvent(GameEvent gameEvent)
    {
        // One event of a kind at a time in a place
        if (_events.Any(t => t.Kind == gameEvent.Kind && t.District == gameEvent.District && t.StartsAt < gameEvent.EndsAt && gameEvent.StartsAt < t.EndsAt))
        {
            return;
        }
        _events.Add(gameEvent);
    }

    /// <summary>Starts an event for everyone right now (Era launch, and for testing)</summary>
    public GameEvent StartEventNow(EventKind kind, DistrictCoord? district, TimeSpan duration, string? resource = null)
    {
        var gameEvent = new GameEvent { Id = NextEventId++, Kind = kind, District = district, StartsAt = LastTick, EndsAt = LastTick + duration, Resource = resource, Announced = true };
        _events.Add(gameEvent);
        StartEvent(gameEvent);
        return gameEvent;
    }

    // ---------------------------------------------------------------- Running events

    /// <summary>The moments in a span where events are announced, start or end</summary>
    private IEnumerable<(DateTime At, Action Run)> EventMoments(DateTime from, DateTime to)
    {
        foreach (var gameEvent in _events)
        {
            var announce = gameEvent.StartsAt - AnnounceAhead;
            if (!gameEvent.Announced && announce <= to)
            {
                yield return (announce > from ? announce : from, () => AnnounceEvent(gameEvent));
            }
            if (!gameEvent.Started && gameEvent.StartsAt <= to)
            {
                yield return (gameEvent.StartsAt, () => StartEvent(gameEvent));
            }
            if (!gameEvent.Ended && gameEvent.EndsAt <= to)
            {
                yield return (gameEvent.EndsAt, () => EndEvent(gameEvent));
            }
        }
    }

    private IEnumerable<Company> CompaniesIn(GameEvent gameEvent) =>
        _companies.Values.Where(t => !t.IsNpc && gameEvent.Covers(t.District));

    public string EventName(GameEvent gameEvent) => gameEvent.Kind switch
    {
        EventKind.Storm => "Storm",
        EventKind.Heatwave => "Heatwave",
        EventKind.Drought => "Drought",
        EventKind.ColdSnap => "Cold Snap",
        EventKind.Expo => $"{ResourceName(gameEvent.Resource)} Expo",
        EventKind.Shortage => $"{ResourceName(gameEvent.Resource)} Shortage",
        EventKind.Discovery => "Discovery",
        EventKind.Festival => "Festival",
        _ => $"Era {Era} launch",
    };

    public string EventEffect(GameEvent gameEvent) => gameEvent.Kind switch
    {
        EventKind.Storm => "Wind ×2; wind buildings may be damaged (half speed until repaired)",
        EventKind.Heatwave => "Sun ×1.5, the hub wants twice as much water",
        EventKind.Drought => "Water pumps −50%, the hub pays ×3 for water",
        EventKind.ColdSnap => "The hub wants twice as much energy",
        EventKind.Expo => $"The hub pays ×3 for {ResourceName(gameEvent.Resource).ToLowerInvariant()}",
        EventKind.Shortage => $"An urgent order for {ResourceName(gameEvent.Resource).ToLowerInvariant()} with a big reward",
        EventKind.Discovery => "A rich deposit appeared in the wilderness",
        EventKind.Festival => "Likes count double",
        _ => "+10% production for everyone",
    };

    private string ResourceName(string? resource) => resource != null && Content.Resources.TryGetValue(resource, out var definition) ? definition.Name : "";

    private void AnnounceEvent(GameEvent gameEvent)
    {
        gameEvent.Announced = true;
        if (gameEvent.StartsAt <= LastTick)
        {
            return;
        }
        foreach (var company in CompaniesIn(gameEvent))
        {
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Calendar, $"Coming {gameEvent.StartsAt:ddd HH:mm} UTC: {EventName(gameEvent)}. {EventEffect(gameEvent)}"));
            Changes.Companies.Add(company.Id);
        }
    }

    private void StartEvent(GameEvent gameEvent)
    {
        gameEvent.Started = true;
        gameEvent.Announced = true;
        EventChangesWorld(gameEvent);
        foreach (var company in CompaniesIn(gameEvent))
        {
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Calendar, $"{EventName(gameEvent)}! {EventEffect(gameEvent)}"));
        }
        switch (gameEvent.Kind)
        {
            case EventKind.Storm:
                DamageInStorm(gameEvent);
                break;
            case EventKind.Shortage when gameEvent.Resource != null:
                foreach (var company in CompaniesIn(gameEvent).Where(t => t.LastSeenAt >= LastTick - ActivePeriod))
                {
                    AddShortageOrder(company, gameEvent);
                }
                break;
        }
    }

    private void EndEvent(GameEvent gameEvent)
    {
        gameEvent.Ended = true;
        gameEvent.Started = true;
        EventChangesWorld(gameEvent);
        if (gameEvent.Kind == EventKind.Shortage)
        {
            foreach (var company in CompaniesIn(gameEvent))
            {
                if (company.DailyOrders.RemoveAll(t => t.Urgent && t.Id == -(int)gameEvent.Id) > 0)
                {
                    company.Touch();
                }
            }
        }
        if (gameEvent.Kind == EventKind.Storm)
        {
            // An Engineer's Workshop repairs its owner's storm damage when the storm is over
            foreach (var building in _buildings.Values.Where(t => t.Damaged && gameEvent.Covers(WorldLayout.DistrictOf(t.X, t.Y))).ToList())
            {
                if (BuildingsOf(building.Owner).Any(t => !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.Workshop))
                {
                    SetDamaged(building, false);
                }
            }
        }
    }

    /// <summary>Production and prices change: networks are solved again and clients get the new prices</summary>
    private void EventChangesWorld(GameEvent gameEvent)
    {
        foreach (var network in _networks.Values.Where(t => gameEvent.Covers(t.District)))
        {
            network.NeedsSolve = true;
            Changes.Networks.Add(network.Id);
        }
        foreach (var district in _markets.Keys.Where(gameEvent.Covers))
        {
            Changes.Markets.Add(district);
        }
        foreach (var company in _companies.Values.Where(t => gameEvent.Covers(t.District)))
        {
            Changes.Companies.Add(company.Id);
        }
    }

    // ---------------------------------------------------------------- Storm damage and repairs

    private void DamageInStorm(GameEvent storm)
    {
        var random = new SeededRandom((ulong)Generator.Seed * 977 + (ulong)storm.Id * 7919);
        foreach (var building in _buildings.Values.OrderBy(t => t.Id).ToList())
        {
            var definition = Content.Building(building.Type);
            if (building.UnderConstruction || building.Damaged || definition.Modifier != ProductionModifier.Wind
                || !storm.Covers(WorldLayout.DistrictOf(building.X, building.Y)) || Company(building.Owner) is not { IsNpc: false } owner)
            {
                continue;
            }
            // A Weather Station halves the risk
            var chance = Horizon(owner) == ForecastAhead ? StormDamageChance / 2 : StormDamageChance;
            if (random.NextDouble() < chance)
            {
                SetDamaged(building, true);
                Changes.Events.Add(new WorldEvent(owner.Id, WorldEventKind.Calendar, $"The storm damaged your {definition.Name}: it works at half speed until it is repaired", building.Id));
            }
        }
    }

    private void SetDamaged(Building building, bool damaged)
    {
        building.Damaged = damaged;
        building.Touch();
        MarkNeedsSolve(building.Id);
        foreach (var tile in building.Footprint())
        {
            Changes.Tiles.Add(tile);
        }
    }

    /// <summary>What a repair costs: a tenth of what the building cost</summary>
    public double RepairCost(Building building) => Math.Max(10, Math.Round(Content.Building(building.Type).Cost.Gold * 0.1));

    public CommandResult Repair(long companyId, long buildingId)
    {
        if (Company(companyId) is not { } company || Building(buildingId) is not { } building || building.Owner != companyId)
        {
            return CommandResult.Fail("That is not your building.");
        }
        if (!building.Damaged)
        {
            return CommandResult.Fail("It isn't damaged.");
        }
        var payment = Pay(company, RepairCost(building), new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }
        SetDamaged(building, false);
        return CommandResult.Ok;
    }

    // ---------------------------------------------------------------- Shortage orders

    private void AddShortageOrder(Company company, GameEvent shortage)
    {
        var resource = shortage.Resource!;
        var price = BasePrice(resource);
        // Twice a normal order, three times the reward per unit
        var value = 240 * Math.Pow(company.Level, 1.35);
        var amount = Math.Max(20, Math.Round(value / price / 10) * 10);
        company.DailyOrders.Add(new DailyOrder
        {
            Id = -(int)shortage.Id,
            Resource = resource,
            Amount = amount,
            RewardGold = Math.Round(amount * price * 3),
            RewardXp = 60 + 12.0 * company.Level,
            RewardReputation = 80,
            ExpiresAt = shortage.EndsAt,
            Urgent = true,
        });
        company.Touch();
        Changes.Companies.Add(company.Id);
    }
}
