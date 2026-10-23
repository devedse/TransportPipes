using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

public sealed partial class GameWorld
{
    /// <summary>Reputation points needed for hub reputation levels 1..10</summary>
    public static readonly double[] ReputationLevels = [0, 100, 300, 600, 1000, 1500, 2100, 2800, 3600, 4500];

    /// <summary>Hub orders a day at most: one a good</summary>
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
        _fitsOnEstate.Clear();
        SizeWonderStages();
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
        // Offered only to players who play (after run 3, step 7); a player who comes back gets today's at Hello
        foreach (var company in _companies.Values.Where(IsActive))
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

    /// <summary>
    /// Hub orders are buyers at the trading post (after run 3, the design team's step 7: none was delivered after day 14
    /// in run 3, because they came out of a Storage nobody mentioned). One a good, three at most, together about half an
    /// hour of the company's income at base, each at most what its posts sell of the good in eight hours, so it fills by
    /// itself; it pays 2 × base for what it takes, the difference to the hub's price as the goods pass, and shows once it
    /// is filled.
    /// </summary>
    public const double OrderIncomeMinutes = 30;
    private const double OrderFillHours = 8;

    /// <summary>What a company's trading posts sell now, a minute, by good</summary>
    public Dictionary<string, double> SellingPerMinute(Company company)
    {
        var selling = new Dictionary<string, double>();
        foreach (var post in BuildingsOf(company.Id).Where(t => !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.TradingPost))
        {
            foreach (var flow in NetworkOf(post.Id)?.Solution.Flows.Where(t => t.To == post.Id && t.Kind == FlowKind.Sale) ?? [])
            {
                Add(selling, flow.Resource, flow.Delivered);
            }
        }
        return selling;
    }

    private List<DailyOrder> GenerateOrders(Company company)
    {
        // What its posts sell first: an order is a bonus for doing what you do, not a chore
        var selling = SellingPerMinute(company);
        var resources = selling.Where(t => t.Value > 1e-6).Select(t => t.Key).Order(StringComparer.Ordinal).ToList();
        if (resources.Count == 0)
        {
            resources = OrderResources(company);
        }
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
        var count = Math.Min(OrdersPerDay, resources.Count);
        for (var i = 0; i < count; i++)
        {
            var resource = resources[i];
            var price = BasePrice(resource);
            var units = OrderIncomeMinutes / count * MeasuredIncome(company) * (0.75 + 0.5 * random.NextDouble()) / price;
            if (selling.GetValueOrDefault(resource) is var rate && rate > 1e-6)
            {
                units = Math.Min(units, rate * OrderFillHours * 60);
            }
            var amount = Math.Max(10, Math.Round(units / 10) * 10);
            orders.Add(new DailyOrder
            {
                Id = day * 10 + i,
                Resource = resource,
                Amount = amount,
                RewardGold = Math.Round(amount * price * 2 * (1 + WonderOrderBonus(company) + Refined(company, "hubOrders"))),
                RewardXp = 30 + 8.0 * company.Level,
                RewardReputation = 40,
                ExpiresAt = expires,
            });
        }
        return orders;
    }

    /// <summary>
    /// What a company's trading posts sell fills its open orders of that good (shortage orders first): the order pays its
    /// price for what it takes, so on top of the hub's price comes the difference; returns that difference
    /// </summary>
    private double FillOrders(Company company, string resource, double units, double price)
    {
        var premium = 0.0;
        foreach (var order in company.DailyOrders.Where(t => t.Resource == resource && !t.Done && t.ExpiresAt > LastTick).OrderByDescending(t => t.Urgent).ThenBy(t => t.Id).ToList())
        {
            if (units <= 1e-9)
            {
                break;
            }
            var take = Math.Min(units, order.Amount - order.Delivered);
            units -= take;
            order.Delivered = Math.Min(order.Amount, order.Delivered + take);
            var paid = take * Math.Max(0, order.RewardGold / order.Amount - price);
            order.Premium += paid;
            premium += paid;
            if (order.Done)
            {
                CompleteOrder(company, order);
            }
            company.Touch();
            Changes.Companies.Add(company.Id);
        }
        return premium;
    }

    /// <summary>An order is filled: reputation and XP, and it shows now ("Your coal filled a hub order")</summary>
    private void CompleteOrder(Company company, DailyOrder order)
    {
        company.HubReputation += order.RewardReputation;
        GainXp(company, order.RewardXp, "orders", order.Resource);
        Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.OrderCompleted,
            $"Your {GoodsWord(order.Resource)} filled {(order.Urgent ? "a shortage order" : "a hub order")}: {order.Amount:N0} at {(order.Urgent ? "three times" : "twice")} the price, +{order.Premium:N0} g over the hub's"));
        company.Report.OrdersCompleted++;
        company.Report.OrderGold += order.Premium;
        if (!company.Report.OrdersFilled.Contains(order.Resource))
        {
            company.Report.OrdersFilled.Add(order.Resource);
        }
        AddStat(company, "orders", 1);
    }
}
