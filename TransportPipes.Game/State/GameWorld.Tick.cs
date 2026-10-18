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
        // Everything that finishes in between, in the order it happens
        var events = new List<(DateTime At, long Order, Action Run)>();
        foreach (var building in _buildings.Values.Where(t => t.ConstructionEndsAt is { } end && end <= now))
        {
            events.Add((building.ConstructionEndsAt!.Value, building.Id, () => CompleteConstruction(building)));
        }
        foreach (var company in _companies.Values.Where(t => t.ResearchEndsAt is { } end && end <= now))
        {
            events.Add((company.ResearchEndsAt!.Value, -company.Id, () => CompleteResearch(company)));
        }
        foreach (var (at, _, run) in events.OrderBy(t => t.At).ThenBy(t => t.Order))
        {
            if (at > LastTick)
            {
                Advance(at);
            }
            run();
        }
        Advance(now);
    }

    private void CompleteConstruction(Building building)
    {
        building.ConstructionEndsAt = null;
        building.Touch();
        MarkNeedsSolve(building.Id);
        foreach (var tile in building.Footprint())
        {
            Changes.Tiles.Add(tile);
        }
        if (Company(building.Owner) is { } company)
        {
            company.Report.BuildingsCompleted++;
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
            if (solution.Flows.Count == 0 && solution.ResearchPerMinute.Count == 0)
            {
                return;
            }

            var span = Math.Min(minutes, TimeUntilChange(solution));
            Apply(solution, span);
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
                if (change < -1e-9)
                {
                    time = Math.Min(time, (storage.Contents.TryGetValue(resource, out var amount) ? amount : 0) / -change);
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

    private void Apply(FlowSolution solution, double minutes)
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
        foreach (var flow in solution.Flows)
        {
            var from = _buildings[flow.From];
            switch (flow.Kind)
            {
                case FlowKind.Trade or FlowKind.Sale:
                    var income = flow.Delivered * flow.PricePerUnit * minutes;
                    earnings[from.Owner] = (earnings.TryGetValue(from.Owner, out var e) ? e : 0) + income;
                    if (flow.Kind == FlowKind.Sale && Company(from.Owner) is { } seller)
                    {
                        Add(seller.Report.Sold, flow.Resource, flow.Delivered * minutes);
                    }
                    break;
            }
            if (flow.Kind is FlowKind.Internal or FlowKind.Trade or FlowKind.ToStorage or FlowKind.Sale
                && Content.Building(from.Type).Kind is not (BuildingKind.Storage or BuildingKind.TradingPost)
                && Company(from.Owner) is { } producer)
            {
                Add(producer.Report.Produced, flow.Resource, flow.Sent * minutes);
            }
        }

        foreach (var (companyId, earned) in earnings)
        {
            if (Company(companyId) is { } company)
            {
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
        GainXp(company, xp);
    }

    private static void Add(Dictionary<string, double> totals, string resource, double amount) =>
        totals[resource] = (totals.TryGetValue(resource, out var v) ? v : 0) + amount;
}
