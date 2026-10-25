using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;

namespace TransportPipes.Game.State;

public sealed partial class GameWorld
{
    private const int MaxStepsPerAdvance = 100;

    /// <summary>Runs the world up to now</summary>
    public void Tick() => Tick(Now);

    /// <summary>
    /// Runs the world up to a moment. Constructions that finish in between are handled at the moment they finish, and
    /// networks are solved again whenever a storage fills up or runs empty or a company runs out of gold, so a tick of a
    /// few seconds and a catch-up of a day give the same result.
    /// </summary>
    public void Tick(DateTime now)
    {
        if (now <= LastTick)
        {
            return;
        }
        PlanEvents();
        if (!_grantsChecked)
        {
            _grantsChecked = true;
            foreach (var company in _companies.Values.ToList())
            {
                GrantResearch(company);
            }
        }
        // Everything that finishes in between, in the order it happens
        var events = new List<(DateTime At, long Order, Action Run)>();
        foreach (var (at, run) in EventMoments(LastTick, now).ToList())
        {
            events.Add((at, long.MinValue + 4, run));
        }
        foreach (var at in _towns.Values.Select(t => t.Wonder?.FullAt).OfType<DateTime>().Select(t => t + StageBuildTime).Where(t => t <= now).Distinct())
        {
            events.Add((at > LastTick ? at : LastTick, long.MinValue + 5, CheckStagesBuilt));
        }
        foreach (var (at, run) in LeaseMoments(LastTick, now).ToList())
        {
            events.Add((at, long.MinValue + 6, run));
        }
        foreach (var (at, run) in ArrivalMoments(LastTick, now).ToList())
        {
            events.Add((at, long.MinValue + 9, run));
        }
        foreach (var (at, run) in DigMoments(LastTick, now).ToList())
        {
            events.Add((at, long.MinValue + 10, run));
        }
        foreach (var (at, run) in ExpeditionMoments(LastTick, now).ToList())
        {
            events.Add((at, long.MinValue + 11, run));
        }
        foreach (var (at, run) in LinkMoments(LastTick, now).ToList())
        {
            events.Add((at, long.MinValue + 12, run));
        }
        foreach (var (at, run) in PriceMoments(LastTick, now).ToList())
        {
            events.Add((at, long.MinValue + 13, run));
        }
        foreach (var company in _companies.Values.Where(t => t.PendingBlocks.Count != 0))
        {
            foreach (var (other, at) in company.PendingBlocks.Where(t => t.Value <= now).ToList())
            {
                events.Add((at, -company.Id, () => ApplyBlock(company, other)));
            }
        }
        foreach (var building in _buildings.Values.Where(t => t.ConstructionEndsAt is { } end && end <= now))
        {
            events.Add((building.ConstructionEndsAt!.Value, building.Id, () => CompleteConstruction(building)));
        }
        foreach (var company in _companies.Values.Where(t => t.ResearchEndsAt is { } end && end <= now))
        {
            events.Add((company.ResearchEndsAt!.Value, -company.Id, () => CompleteResearch(company)));
        }
        foreach (var company in _companies.Values.Where(t => t.TrackEndsAt is { } end && end <= now))
        {
            events.Add((company.TrackEndsAt!.Value, -company.Id, () => CompleteTier(company)));
        }
        // A free line with a queue (or a player away) starts what the gold now covers
        foreach (var company in _companies.Values.Where(t => (t.CurrentResearch == null || t.CurrentTrack == null) && t.Level >= 4))
        {
            StartQueuedResearch(company);
        }
        for (var at = NextMarketUpdate(LastTick); at <= now; at = NextMarketUpdate(at))
        {
            events.Add((at, long.MinValue, () =>
            {
                UpdateMarkets();
                RunManagers();
                // A free line whose player went away starts what the gold covers (KeepResearching)
                foreach (var company in _companies.Values.Where(t => (t.CurrentResearch == null || t.CurrentTrack == null) && t.Level >= 4))
                {
                    StartQueuedResearch(company);
                }
                CheckTradeFalls();
                CheckStalls();
            }));
        }
        for (var at = NextDay(LastTick); at <= now; at = NextDay(at))
        {
            events.Add((at, long.MinValue + 1, () =>
            {
                NewDay();
                ExpireOrders();
            }));
        }
        for (var at = NextWeek(LastTick); at <= now; at = NextWeek(at))
        {
            events.Add((at, long.MinValue + 3, NewWeek));
        }
        foreach (var at in _shipments.Where(t => t.ArrivesAt <= now).Select(t => t.ArrivesAt).Distinct())
        {
            events.Add((at, long.MinValue + 2, DeliverShipments));
        }
        foreach (var (at, _, run) in events.OrderBy(t => t.At).ThenBy(t => t.Order))
        {
            CoupleUntil(at);
            if (at > LastTick)
            {
                Advance(at);
            }
            run();
        }
        // Research that started in between and ends before now (a queue running through a long catch-up), in order
        for (var step = 0; step < 1000 && NextResearchEnd(now) is { } next; step++)
        {
            CoupleUntil(next.At);
            if (next.At > LastTick)
            {
                Advance(next.At);
            }
            if (next.Track)
            {
                CompleteTier(next.Company);
            }
            else
            {
                CompleteResearch(next.Company);
            }
        }
        CoupleUntil(now);
        Advance(now);
        CheckAchievements();
        CheckTutorial();
    }

    /// <summary>The first research of either line that ends by a moment (null: none)</summary>
    private (DateTime At, Company Company, bool Track)? NextResearchEnd(DateTime by)
    {
        (DateTime At, Company Company, bool Track)? first = null;
        foreach (var company in _companies.Values)
        {
            if (company.ResearchEndsAt is { } tree && tree <= by && (first == null || tree < first.Value.At))
            {
                first = (tree, company, false);
            }
            if (company.TrackEndsAt is { } track && track <= by && (first == null || track < first.Value.At))
            {
                first = (track, company, true);
            }
        }
        return first;
    }

    /// <summary>The Yards' rounds up to a moment (v1.2 §7): once a minute while terms move, less often when they don't</summary>
    private void CoupleUntil(DateTime at)
    {
        for (var round = 0; round < 100_000 && NextCoupling() <= at; round++)
        {
            if (NextCoupling() > LastTick)
            {
                Advance(NextCoupling());
            }
            RunCoupling();
        }
    }

    private void CompleteConstruction(Building building)
    {
        building.ConstructionEndsAt = null;
        building.Touch();
        if (Content.Building(building.Type).Kind is BuildingKind.PowerLine or BuildingKind.CargoDepot or BuildingKind.DeepLift)
        {
            // Its cables (or cargo routes, or the lift's cage) connect now
            MarkDirty(building);
        }
        EnsureShaft(building);
        MarkNeedsSolve(building.Id);
        foreach (var tile in building.Footprint())
        {
            Changes.Tiles.Add(tile);
        }
        if (Company(building.Owner) is { } company)
        {
            company.Report.BuildingsCompleted++;
            AddStat(company, "buildings", 1);
            TryMergeComplex(building);
            company.Touch();
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.ConstructionFinished, $"{Content.Building(building.Type).Name} is ready", building.Id));
        }
    }

    /// <summary>Finishes a construction right away (for tests and for paying to rush, later)</summary>
    public void FinishConstruction(long buildingId)
    {
        // A scaffold stands only once its materials are in
        if (Building(buildingId) is { UnderConstruction: true } building && !IsSite(building))
        {
            CompleteConstruction(building);
        }
    }

    private void Advance(DateTime to)
    {
        var minutes = (to - LastTick).TotalMinutes;
        LastTick = to;
        if (minutes <= 0)
        {
            return;
        }
        SolveNetworks();
        CountLinkDeliveries(minutes);
        SettleWonderGifts(minutes);
        SettleAsks(minutes);
        foreach (var network in _networks.Values.OrderBy(t => t.Id).ToList())
        {
            AdvanceNetwork(network, minutes);
        }
        MeasureIncome(minutes);
    }

    /// <summary>Measured income I(c) is never below this many gold a minute (a newcomer's projects and allowances have a size)</summary>
    public const double MinMeasuredIncome = 60;

    /// <summary>A company's measured income I(c) (F4): the 24-hour average of what it earns by producing, at least 60 a minute</summary>
    public double MeasuredIncome(Company company) => Math.Max(MinMeasuredIncome, company.MeasuredIncome);

    /// <summary>What a company has earned by producing, all told: hub sales, what the Yard, Houses and stand-ins paid, the project</summary>
    private static double ProductionEarned(Company company) =>
        company.Stats.GetValueOrDefault("hubGold") + company.Stats.GetValueOrDefault("yardPaid") + company.Stats.GetValueOrDefault("yardFloor")
        + company.Stats.GetValueOrDefault("npcGold") + company.Stats.GetValueOrDefault("projectGold");

    /// <summary>Every company's measured income moves towards what it earned this step (a 24-hour average)</summary>
    private void MeasureIncome(double minutes)
    {
        foreach (var company in _companies.Values)
        {
            var earned = ProductionEarned(company);
            if (company.MeasuredEarned <= 0)
            {
                // The first measure (a new company, or a world from before I(c)): from what its networks earn now
                company.MeasuredEarned = earned;
                company.MeasuredIncome = Math.Max(company.MeasuredIncome, _networks.Values.Sum(t => t.Solution.GoldPerMinute.GetValueOrDefault(company.Id)));
                continue;
            }
            var rate = Math.Max(0, earned - company.MeasuredEarned) / minutes;
            company.MeasuredIncome += (rate - company.MeasuredIncome) * Math.Min(1, minutes / (24 * 60));
            company.MeasuredEarned = earned;
        }
    }

    private void AdvanceNetwork(Network network, double minutes)
    {
        for (var step = 0; step < MaxStepsPerAdvance && minutes > 1e-9; step++)
        {
            if (network.NeedsSolve)
            {
                Solve(network);
            }
            var solution = network.Solution;
            if (solution.Flows.Count == 0 && network.GateFlows.Count == 0 && !HasGateImports(network))
            {
                return;
            }

            var span = Math.Min(minutes, TimeUntilChange(solution));
            Apply(network, solution, span);
            minutes -= span;
            if (minutes > 1e-9)
            {
                // Something filled up, ran empty or went broke: the flows change
                network.NeedsSolve = true;
            }
        }
    }

    /// <summary>Minutes until a storage of this solution is full or empty, or a company runs out of gold</summary>
    private double TimeUntilChange(FlowSolution solution)
    {
        var time = double.MaxValue;
        foreach (var (storageId, net) in solution.StorageNet)
        {
            var storage = _buildings[storageId];
            var capacity = StorageCapacity(storage, Content.Building(storage.Type));
            var total = net.Values.Sum();
            if (total > 1e-9)
            {
                time = Math.Min(time, Math.Max(0, capacity - storage.StoredTotal) / total);
            }
            foreach (var (resource, change) in net)
            {
                var amount = storage.Contents.TryGetValue(resource, out var have) ? have : 0;
                if (change < -1e-9)
                {
                    time = Math.Min(time, amount / -change);
                }
                else if (change > 1e-9 && StorageKeep(storage, Content.Building(storage.Type), resource) - amount is > StorageEpsilon and var toKeep)
                {
                    // When it has what it keeps, the rest goes on to be sold
                    time = Math.Min(time, toKeep / change);
                }
            }
        }
        foreach (var (companyId, goldPerMinute) in solution.GoldPerMinute)
        {
            if (goldPerMinute < -1e-9 && Company(companyId) is { Gold: > 0 } company)
            {
                time = Math.Min(time, company.Gold / -goldPerMinute);
            }
        }
        return time;
    }

    private const double StorageFullShare = 0.999;
    private static readonly TimeSpan StorageFullNoticeEvery = TimeSpan.FromHours(6);
    private readonly Dictionary<long, DateTime> _storageFullNoticeAt = new();

    /// <summary>
    /// A full Storage as a line says it (run 2: Ana's warning said "full" for four weeks and never what was in it or what
    /// waited behind it): "4,800 energy and 200 iron ingots", and the buildings that slow behind it ("your 4 Wind Moilies")
    /// </summary>
    public (string Holds, string? Waiting, Building? First) StorageLine(Building storage)
    {
        var holds = storage.Contents.Where(t => t.Value >= 1).OrderByDescending(t => t.Value).Take(2)
            .Select(t => $"{(t.Value >= 100 ? t.Value.ToString("N0") : t.Value.ToString("0"))} {GoodsWord(t.Key)}").ToList();
        var waiting = BuildingsOf(storage.Owner)
            .Where(t => t.Id != storage.Id && IdleReasonOf(t).Cause == IdleCause.Full && PipeReach(t).Any(r => r.Id == storage.Id))
            .ToList();
        var kinds = waiting.GroupBy(t => Content.Building(t.Type).Name).OrderByDescending(t => t.Count())
            .Select(t => t.Count() == 1 ? $"a {t.Key}" : $"{t.Count()} {Plural(t.Key)}").ToList();
        return (holds.Count == 0 ? "goods" : string.Join(" and ", holds),
            kinds.Count == 0 ? null : $"your {string.Join(" and ", kinds.Take(2))}{(kinds.Count > 2 ? " and more" : "")}",
            waiting.FirstOrDefault());
    }

    /// <summary>"Wind Moilies", "Solar Panels", "Small Factories"</summary>
    private static string Plural(string name) =>
        name.EndsWith('y') && !name.EndsWith("ey", StringComparison.Ordinal) ? name[..^1] + "ies" : name.EndsWith('s') ? name : name + "s";

    /// <summary>Storages that are (nearly) full</summary>
    public IEnumerable<Building> FullStorages(long companyId) =>
        BuildingsOf(companyId).Where(t => Content.Building(t.Type).Kind == BuildingKind.Storage && !t.UnderConstruction
            && t.StoredTotal >= StorageCapacity(t, Content.Building(t.Type)) * StorageFullShare);

    private void Apply(Network network, FlowSolution solution, double minutes)
    {
        if (minutes <= 0)
        {
            return;
        }

        foreach (var (companyId, goldPerMinute) in solution.GoldPerMinute)
        {
            if (Company(companyId) is { } company)
            {
                company.Gold += goldPerMinute * minutes;
                if (company.Gold < 1e-6 && goldPerMinute < 0)
                {
                    company.Gold = 0;
                }
                company.Touch();
                Changes.Companies.Add(companyId);
            }
        }

        var earnings = new Dictionary<long, double>();
        SettleGateFlows(network, minutes, earnings);
        foreach (var flow in solution.Flows)
        {
            var from = _buildings[flow.From];
            switch (flow.Kind)
            {
                case FlowKind.Trade or FlowKind.Sale:
                    var income = flow.Delivered * flow.PricePerUnit * minutes;
                    earnings[from.Owner] = (earnings.TryGetValue(from.Owner, out var e) ? e : 0) + income;
                    if (Company(from.Owner) is not { } seller)
                    {
                        break;
                    }
                    if (flow.Kind == FlowKind.Trade)
                    {
                        CountEarned(seller, income, "players", flow.Resource);
                        break;
                    }
                    CountEarned(seller, income, "market", flow.Resource);
                    // Today's hub orders buy at their own price as the goods pass (after run 3, step 7)
                    if (seller.DailyOrders.Count > 0 && FillOrders(seller, flow.Resource, flow.Delivered * minutes, flow.PricePerUnit) is > 0 and var premium)
                    {
                        earnings[from.Owner] += premium;
                        CountEarned(seller, premium, "orders", flow.Resource);
                    }
                    AddStat(seller, "hubGold", income);
                    Add(seller.Report.Sold, flow.Resource, flow.Delivered * minutes);
                    // The company's own order at the hub follows what it sells there
                    HubMarketOf(seller, network.District).AddSold(flow.Resource, flow.Delivered * minutes);
                    break;
            }
            if (flow.Kind is FlowKind.Internal or FlowKind.Trade or FlowKind.ToStorage or FlowKind.Sale
                && Content.Building(from.Type).Kind is not (BuildingKind.Storage or BuildingKind.TradingPost)
                && Company(from.Owner) is { } producer)
            {
                CountDiscovery(from, flow.Sent * minutes);
                producer.EraOutput += flow.Sent * minutes * BasePrice(flow.Resource);
                Add(producer.Report.Produced, flow.Resource, flow.Sent * minutes);
                AddStat(producer, $"produced:{flow.Resource}", flow.Sent * minutes);
            }
        }

        foreach (var (companyId, earned) in earnings)
        {
            if (Company(companyId) is { } company)
            {
                company.Report.GoldEarned += earned;
            }
        }

        foreach (var (storageId, net) in solution.StorageNet)
        {
            var storage = _buildings[storageId];
            var capacity = StorageCapacity(storage, Content.Building(storage.Type));
            var wasFull = storage.StoredTotal >= capacity * StorageFullShare;
            foreach (var (resource, change) in net)
            {
                var amount = (storage.Contents.TryGetValue(resource, out var v) ? v : 0) + change * minutes;
                if (amount <= StorageEpsilon)
                {
                    storage.Contents.Remove(resource);
                }
                else
                {
                    storage.Contents[resource] = amount;
                }
            }
            var total = storage.StoredTotal;
            if (total > capacity)
            {
                // Rounding: scale down to exactly full
                foreach (var resource in storage.Contents.Keys.ToList())
                {
                    storage.Contents[resource] *= capacity / total;
                }
            }
            if (!wasFull && storage.StoredTotal >= capacity * StorageFullShare && Company(storage.Owner) is { } owner
                && (!_storageFullNoticeAt.TryGetValue(storageId, out var noticed) || LastTick - noticed >= StorageFullNoticeEvery))
            {
                _storageFullNoticeAt[storageId] = LastTick;
                Changes.Events.Add(new WorldEvent(owner.Id, WorldEventKind.StorageFull, $"Your {Content.Building(storage.Type).Name} is full", storageId));
            }
            storage.Touch();
            Changes.Storages.Add(storageId);
        }
    }

    private static void Add(Dictionary<string, double> totals, string resource, double amount) =>
        totals[resource] = (totals.TryGetValue(resource, out var v) ? v : 0) + amount;
}
