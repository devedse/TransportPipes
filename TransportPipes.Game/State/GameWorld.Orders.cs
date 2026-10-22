using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

public sealed partial class GameWorld
{
    /// <summary>Reputation points needed for hub reputation levels 1..10</summary>
    public static readonly double[] ReputationLevels = [0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600, 4500];

    public const int OrdersPerDay = 3;

    public int ReputationLevel(Company company)
    {
        var level = 1;
        while (level < ReputationLevels.Length && company.HubReputation >= ReputationLevels[level])
        {
            level++;
        }
        return level;
    }

    /// <summary>The next midnight (UTC) after a moment: daily orders change then</summary>
    public static DateTime NextDay(DateTime after) => after.Date.AddDays(1);

    private void NewDay()
    {
        _likesToday.Clear();
        PlanEvents();
        CheckActivity();
        CheckHolds();
        CheckTogether();
        if (TenantsEnabled)
        {
            UpdateHouses();
        }
        ForgetOldUsualTrades();
        if (ProjectsEnabled)
        {
            UpdateBaskets();
        }
        foreach (var company in _companies.Values.Where(t => !t.IsNpc && t.LastSeenAt >= LastTick - ActivePeriod))
        {
            EnsureOrders(company);
        }
    }

    /// <summary>Gives a company today's orders when it doesn't have them yet</summary>
    public void EnsureOrders(Company company)
    {
        var removed = company.DailyOrders.RemoveAll(t => t.Urgent && t.ExpiresAt <= LastTick);
        var daily = company.DailyOrders.Where(t => !t.Urgent).ToList();
        if (daily.Count != 0 && daily.All(t => t.ExpiresAt > LastTick))
        {
            if (removed > 0)
            {
                company.Touch();
                Changes.Companies.Add(company.Id);
            }
            return;
        }
        company.DailyOrders = [.. GenerateOrders(company), .. company.DailyOrders.Where(t => t.Urgent)];
        // Storages keep what the new orders need
        MarkCompanyNetworksDirty(company.Id);
        company.Touch();
        Changes.Companies.Add(company.Id);
    }

    /// <summary>
    /// What a company's orders ask for: what it makes now, and the next step of a chain it can already feed (ingots
    /// when it makes ore and energy). Not all it has unlocked: an era unlocks buildings a new company can't feed yet.
    /// </summary>
    public List<string> OrderResources(Company company)
    {
        var made = BuildingsOf(company.Id)
            .Where(t => !t.UnderConstruction)
            .SelectMany(t => Content.Building(t.Type).Out.Keys)
            .Where(t => t != GameContent.ResearchPoints)
            .ToHashSet();
        // Processing only (more than energy goes in): extraction needs a deposit the company may not have
        var next = Content.BuildingList
            .Where(t => t.Buildable && t.Out.Count != 0 && t.In.Keys.Any(k => k != "energy") && t.In.Keys.All(made.Contains) && IsUnlocked(company, t, out _))
            .SelectMany(t => t.Out.Keys)
            .Where(t => t != GameContent.ResearchPoints);
        return made.Concat(next).Distinct().Order(StringComparer.Ordinal).ToList();
    }

    private List<DailyOrder> GenerateOrders(Company company)
    {
        var resources = OrderResources(company);
        if (resources.Count == 0)
        {
            resources = ["energy"];
        }
        // LastTick: the moment in the simulation (during a catch-up the day of this midnight)
        var day = (int)(LastTick.Date - DateTime.UnixEpoch).TotalDays;
        var random = new SeededRandom((ulong)Generator.Seed * 31 + (ulong)company.Id * 7919 + (ulong)day * 104729);
        random.Shuffle(resources);
        var expires = NextDay(LastTick);
        var orders = new List<DailyOrder>();
        for (var i = 0; i < OrdersPerDay; i++)
        {
            var resource = resources[i % resources.Count];
            var price = BasePrice(resource);
            // About a quarter of an hour of a small chain early on, growing with the Company Level
            var value = 120 * Math.Pow(company.Level, 1.35) * (1 + random.NextDouble());
            var amount = Math.Max(10, Math.Round(value / price / 10) * 10);
            orders.Add(new DailyOrder
            {
                Id = day * 10 + i,
                Resource = resource,
                Amount = amount,
                RewardGold = Math.Round(amount * price * 2),
                RewardXp = 30 + 8.0 * company.Level,
                RewardReputation = 40,
                ExpiresAt = expires,
            });
        }
        return orders;
    }

    /// <summary>Whether a company has a finished Storage (hub orders come out of storages)</summary>
    public bool HasStorage(Company company) =>
        BuildingsOf(company.Id).Any(t => !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.Storage);

    /// <summary>Delivers what the storages have for an order; the reward comes when it is complete</summary>
    public CommandResult DeliverOrder(long companyId, int orderId)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        var order = company.DailyOrders.FirstOrDefault(t => t.Id == orderId);
        if (order == null || order.ExpiresAt <= Now)
        {
            return CommandResult.Fail("That order has expired.");
        }
        if (order.Done)
        {
            return CommandResult.Fail("That order is complete.");
        }
        var available = Materials(company.Id).TryGetValue(order.Resource, out var have) ? have : 0;
        var amount = Math.Min(order.Amount - order.Delivered, available);
        if (amount <= 1e-9)
        {
            // Say how an order is filled (run 2: "Your storages have no coal" left three players asking what a storage is)
            var good = Content.Resources[order.Resource].Name.ToLowerInvariant();
            var storages = BuildingsOf(company.Id).Where(t => !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.Storage).ToList();
            return CommandResult.Fail(storages.Count == 0
                ? $"Orders come out of a Storage: build one (Build › Logistics) and pipe your {good} into it."
                : storages.All(t => StorageCapacity(t, Content.Building(t.Type)) - t.StoredTotal <= StorageEpsilon)
                    ? $"Your Storage is full of other goods, so no {good} gets in: upgrade it, or sell what is in it."
                    : $"Your Storage has no {good} yet: pipe a building that makes {good} into it.");
        }
        var payment = Pay(company, 0, new Dictionary<string, double> { [order.Resource] = amount });
        if (!payment.Success)
        {
            return payment;
        }
        order.Delivered += amount;
        if (order.Done)
        {
            company.Gold += order.RewardGold;
            company.LifetimeEarnings += order.RewardGold;
            company.Report.GoldEarned += order.RewardGold;
            company.HubReputation += order.RewardReputation;
            GainXp(company, order.RewardXp, "orders", order.Resource);
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.OrderCompleted, $"Order complete: +{order.RewardGold:N0} gold"));
            company.Report.OrdersCompleted++;
            AddStat(company, "orders", 1);
            MarkCompanyNetworksDirty(company.Id);
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }
}
