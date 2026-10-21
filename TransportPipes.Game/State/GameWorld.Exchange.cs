using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>An order on a hub's Exchange: goods (selling) or gold (buying) are reserved while it waits</summary>
public sealed class ExchangeOrder
{
    public long Id { get; init; }
    public long Company { get; init; }
    public DistrictCoord District { get; init; }
    public required string Resource { get; init; }
    public bool Selling { get; init; }

    /// <summary>What is still open</summary>
    public double Amount { get; set; }
    public double Price { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>Global Exchange: it trades with the global orders of every hub</summary>
    public bool Global { get; init; }
}

/// <summary>Goods on their way by cargo: they arrive in the buyer's storages</summary>
public sealed class Shipment
{
    public long Id { get; init; }
    public long From { get; init; }
    public long To { get; init; }
    public required string Resource { get; init; }
    public double Amount { get; set; }
    public DateTime ArrivesAt { get; set; }
}

/// <summary>
/// The Exchange (GDD §5.8): trade without a pipe. Sell orders hold their goods, buy orders their gold; matching orders
/// trade at the price of the order that was there first. Goods go by cargo (faster with a Cargo Depot) and the seller pays
/// a fee that disappears.
/// </summary>
public sealed partial class GameWorld
{
    public const double ExchangeFee = 0.05;
    public static readonly TimeSpan OrderLifetime = TimeSpan.FromDays(7);

    private readonly List<ExchangeOrder> _orders = new();
    private readonly List<Shipment> _shipments = new();

    public long NextOrderId { get; set; } = 1;
    public IReadOnlyList<ExchangeOrder> ExchangeOrders => _orders;
    public IReadOnlyList<Shipment> Shipments => _shipments;

    public void LoadExchange(IEnumerable<ExchangeOrder> orders, IEnumerable<Shipment> shipments)
    {
        _orders.AddRange(orders);
        _shipments.AddRange(shipments);
        NextOrderId = Math.Max(NextOrderId, _orders.Select(t => t.Id).Concat(_shipments.Select(t => t.Id)).DefaultIfEmpty(0).Max() + 1);
    }

    public bool HasBuilding(long companyId, BuildingKind kind) =>
        _buildings.Values.Any(t => t.Owner == companyId && !t.UnderConstruction && Content.Building(t.Type).Kind == kind);

    /// <summary>How long cargo takes from one estate to another: 10 minutes plus 1 per 10 tiles, halved by a Cargo Depot</summary>
    public TimeSpan CargoTime(Company from, Company to)
    {
        var a = WorldLayout.EstateOrigin(from.District, from.Slot);
        var b = WorldLayout.EstateOrigin(to.District, to.Slot);
        var minutes = 10 + (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y)) / 10.0;
        if (HasBuilding(to.Id, BuildingKind.CargoDepot) || HasBuilding(from.Id, BuildingKind.CargoDepot))
        {
            minutes /= 2;
        }
        // Trains
        if (HasResearch(to, "trains") || HasResearch(from, "trains"))
        {
            minutes /= 2;
        }
        // A Lighthouse in the buyer's district
        return TimeSpan.FromMinutes(minutes * WonderCargoFactor(to));
    }

    public CommandResult PlaceExchangeOrder(long companyId, string resource, bool selling, double amount, double price)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (!HasBuilding(companyId, BuildingKind.ExchangeOffice))
        {
            return CommandResult.Fail("You need an Exchange Office to trade on the Exchange.");
        }
        if (!Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown resource.");
        }
        amount = Math.Floor(amount);
        if (amount < 1)
        {
            return CommandResult.Fail("Choose an amount.");
        }
        // One price between players (v1.2 §8)
        price = PlayerPrice(resource);
        // Reserve what the order needs
        var reserve = selling
            ? Pay(company, 0, new Dictionary<string, double> { [resource] = amount })
            : Pay(company, amount * price, new Dictionary<string, double>());
        if (!reserve.Success)
        {
            return reserve;
        }
        var order = new ExchangeOrder
        {
            Id = NextOrderId++,
            Company = companyId,
            District = company.District,
            Global = HasResearch(company, "globalExchange"),
            Resource = resource,
            Selling = selling,
            Amount = amount,
            Price = price,
            CreatedAt = Now,
        };
        Match(order);
        if (order.Amount > 1e-9)
        {
            _orders.Add(order);
        }
        Changes.Exchange = true;
        Changes.Companies.Add(companyId);
        return CommandResult.Ok;
    }

    /// <summary>Trades a new order against the waiting ones: best price first, then oldest</summary>
    private void Match(ExchangeOrder order)
    {
        var opposite = _orders
            .Where(t => (t.District == order.District || (t.Global && order.Global)) && t.Resource == order.Resource && t.Selling != order.Selling && t.Company != order.Company)
            .Where(t => order.Selling ? t.Price >= order.Price : t.Price <= order.Price)
            .OrderBy(t => order.Selling ? -t.Price : t.Price).ThenBy(t => t.Id)
            .ToList();
        foreach (var resting in opposite)
        {
            if (order.Amount <= 1e-9)
            {
                break;
            }
            var amount = Math.Min(order.Amount, resting.Amount);
            var (sell, buy) = order.Selling ? (order, resting) : (resting, order);
            Trade(sell, buy, amount, resting.Price);
            order.Amount -= amount;
            resting.Amount -= amount;
        }
        _orders.RemoveAll(t => t.Amount <= 1e-9);
    }

    private void Trade(ExchangeOrder sell, ExchangeOrder buy, double amount, double price)
    {
        if (Company(sell.Company) is not { } seller || Company(buy.Company) is not { } buyer)
        {
            return;
        }
        var value = amount * price;
        var fee = value * ExchangeFee;
        seller.Gold += value - fee;
        seller.LifetimeEarnings += value - fee;
        seller.Report.GoldEarned += value - fee;
        AddStat(seller, "exchangeTrades", 1);
        AddStat(buyer, "exchangeTrades", 1);
        GainXpFromEarnings(seller, value - fee);
        // The buyer reserved its own limit price: the difference comes back
        buyer.Gold += amount * (buy.Price - price);
        seller.Touch();
        buyer.Touch();
        _shipments.Add(new Shipment
        {
            Id = NextOrderId++,
            From = seller.Id,
            To = buyer.Id,
            Resource = sell.Resource,
            Amount = amount,
            ArrivesAt = LastTick.Add(CargoTime(seller, buyer)),
        });
        Changes.Companies.Add(seller.Id);
        Changes.Companies.Add(buyer.Id);
        Changes.Events.Add(new WorldEvent(seller.Id, WorldEventKind.ExchangeTrade, $"Sold {amount:N0} {Content.Resources[sell.Resource].Name.ToLowerInvariant()} on the Exchange"));
        Changes.Events.Add(new WorldEvent(buyer.Id, WorldEventKind.ExchangeTrade, $"Bought {amount:N0} {Content.Resources[sell.Resource].Name.ToLowerInvariant()}: on its way"));
    }

    public CommandResult CancelExchangeOrder(long companyId, long orderId)
    {
        var order = _orders.FirstOrDefault(t => t.Id == orderId && t.Company == companyId);
        if (order == null)
        {
            return CommandResult.Fail("That order is gone.");
        }
        Refund(order);
        _orders.Remove(order);
        Changes.Exchange = true;
        return CommandResult.Ok;
    }

    private void Refund(ExchangeOrder order)
    {
        if (Company(order.Company) is not { } company)
        {
            return;
        }
        if (order.Selling)
        {
            // The goods go back into the storages (or wait as a shipment when they are full)
            _shipments.Add(new Shipment { Id = NextOrderId++, From = company.Id, To = company.Id, Resource = order.Resource, Amount = order.Amount, ArrivesAt = LastTick });
        }
        else
        {
            company.Gold += order.Amount * order.Price;
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
    }

    /// <summary>Delivers the shipments that have arrived into the storages; what doesn't fit waits</summary>
    private void DeliverShipments()
    {
        foreach (var shipment in _shipments.Where(t => t.ArrivesAt <= LastTick).ToList())
        {
            var left = shipment.Amount;
            foreach (var storage in _buildings.Values.Where(t => t.Owner == shipment.To && !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.Storage).OrderBy(t => t.Id))
            {
                var room = StorageCapacity(storage, Content.Building(storage.Type)) - storage.StoredTotal;
                var put = Math.Min(room, left);
                if (put <= 1e-9)
                {
                    continue;
                }
                storage.Contents[shipment.Resource] = (storage.Contents.TryGetValue(shipment.Resource, out var v) ? v : 0) + put;
                storage.Touch();
                MarkNeedsSolve(storage.Id);
                left -= put;
            }
            shipment.Amount = left;
            if (left <= 1e-9)
            {
                _shipments.Remove(shipment);
            }
            else
            {
                // Full storages: try again in a while
                shipment.ArrivesAt = LastTick.AddMinutes(15);
            }
            Changes.Exchange = true;
        }
    }

    private void ExpireOrders()
    {
        foreach (var order in _orders.Where(t => t.CreatedAt + OrderLifetime <= LastTick).ToList())
        {
            Refund(order);
            _orders.Remove(order);
            Changes.Exchange = true;
        }
    }
}
