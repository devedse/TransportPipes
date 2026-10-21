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
        EnsureEraWonder();
        // Everything that finishes in between, in the order it happens
        var events = new List<(DateTime At, long Order, Action Run)>();
        foreach (var (at, run) in EventMoments(LastTick, now).ToList())
        {
            events.Add((at, long.MinValue + 4, run));
        }
        foreach (var (at, run) in EraMoments(LastTick, now).ToList())
        {
            events.Add((at, long.MinValue + 5, run));
        }
        foreach (var (at, run) in LeaseMoments(LastTick, now).ToList())
        {
            events.Add((at, long.MinValue + 6, run));
        }
        foreach (var (at, run) in SeasonMoments(LastTick, now).ToList())
        {
            events.Add((at, long.MinValue + 8, run));
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
        foreach (var (at, run) in AskMoments(LastTick, now).ToList())
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
        for (var at = NextMarketUpdate(LastTick); at <= now; at = NextMarketUpdate(at))
        {
            events.Add((at, long.MinValue, () =>
            {
                UpdateMarkets();
                UpdateTailwinds();
                RunManagers();
            }));
        }
        for (var at = NextDay(LastTick); at <= now; at = NextDay(at))
        {
            events.Add((at, long.MinValue + 1, () =>
            {
                NewDay();
                ExpireOffers();
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
        foreach (var contract in _contracts.Values.Where(t => t.State == ContractState.Active && t.EndsAt <= now))
        {
            events.Add((contract.EndsAt!.Value, contract.Id, () => EndContract(contract)));
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
        CoupleUntil(now);
        Advance(now);
        CheckAchievements();
        CheckTutorial();
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
        if (Building(buildingId) is { UnderConstruction: true } building)
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
        ExpectContracts(minutes);
        CountLinkDeliveries(minutes);
        SettleStandIns(minutes);
        SettleProjects(minutes);
        SettleAsks(minutes);
        SettleTrunks(minutes);
        foreach (var network in _networks.Values.OrderBy(t => t.Id).ToList())
        {
            AdvanceNetwork(network, minutes);
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
            if (solution.Flows.Count == 0 && solution.ResearchPerMinute.Count == 0 && network.GateFlows.Count == 0)
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
                case FlowKind.Trade or FlowKind.Contract or FlowKind.Sale:
                    var income = flow.Delivered * flow.PricePerUnit * minutes;
                    earnings[from.Owner] = (earnings.TryGetValue(from.Owner, out var e) ? e : 0) + income;
                    if (flow.Kind == FlowKind.Sale && Company(from.Owner) is { } seller)
                    {
                        Add(seller.Report.Sold, flow.Resource, flow.Delivered * minutes);
                        // The company's own order at the hub follows what it sells there
                        HubMarketOf(seller, network.District).AddSold(flow.Resource, flow.Delivered * minutes);
                        // Trading with your hub builds reputation: a point per 2,000 gold
                        var level = ReputationLevel(seller);
                        seller.HubReputation += income / 2000;
                        if (ReputationLevel(seller) != level)
                        {
                            // Better hub prices from now on
                            MarkCompanyNetworksDirty(seller.Id);
                        }
                    }
                    break;
            }
            if (flow.Kind is FlowKind.Internal or FlowKind.Trade or FlowKind.Contract or FlowKind.ToStorage or FlowKind.Sale
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
                // The Caretaker of a dormant company
                company.Gold -= earned * CaretakerShare(company);
                company.Report.CaretakerFee += earned * CaretakerShare(company);
                TaxEarnings(company, earned);
                company.LifetimeEarnings += earned;
                company.Report.GoldEarned += earned;
                GainXpFromEarnings(company, earned);
            }
        }

        foreach (var (companyId, research) in solution.ResearchPerMinute)
        {
            if (Company(companyId) is { } company)
            {
                company.ResearchPoints += research * minutes;
                company.Touch();
                Changes.Companies.Add(companyId);
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
            if (!wasFull && storage.StoredTotal >= capacity * StorageFullShare && Company(storage.Owner) is { IsNpc: false } owner
                && (!_storageFullNoticeAt.TryGetValue(storageId, out var noticed) || LastTick - noticed >= StorageFullNoticeEvery))
            {
                _storageFullNoticeAt[storageId] = LastTick;
                Changes.Events.Add(new WorldEvent(owner.Id, WorldEventKind.StorageFull, $"Your {Content.Building(storage.Type).Name} is full", storageId));
            }
            storage.Touch();
            Changes.Storages.Add(storageId);
        }
    }

    /// <summary>
    /// One XP per (goldPerXp × Company Level) gold. A level reached halfway through counts from that moment, so a long
    /// catch-up gives the same XP as many short ticks.
    /// </summary>
    private void GainXpFromEarnings(Company company, double gold)
    {
        var progression = Content.Progression;
        var goldPerXp = progression.XpReward("goldPerXp");
        var xp = 0.0;
        var level = company.Level;
        while (gold > 1e-12)
        {
            var rate = goldPerXp * level;
            var toNext = level >= progression.MaxCompanyLevel ? double.MaxValue : progression.XpForLevel(level + 1) - (company.Xp + xp);
            if (gold / rate < toNext)
            {
                xp += gold / rate;
                break;
            }
            xp += toNext;
            gold -= toNext * rate;
            level++;
        }
        GainXp(company, xp, "earnings", "gold earned");
    }

    private static void Add(Dictionary<string, double> totals, string resource, double amount) =>
        totals[resource] = (totals.TryGetValue(resource, out var v) ? v : 0) + amount;
}
