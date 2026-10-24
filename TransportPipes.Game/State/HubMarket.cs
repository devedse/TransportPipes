namespace TransportPipes.Game.State;

/// <summary>
/// A company's order at one hub (v1.2 §8, the Hub Order): the hub buys every resource; when the company sells more than
/// the hub's order, its price drops (to a quarter at most), up to the order it pays the full price. Nobody else's sales
/// move it. Updated every 15 minutes.
/// </summary>
public sealed class HubMarket
{
    public double MinMultiplier { get; set; } = 0.25;
    public double MaxMultiplier { get; set; } = 1.0;

    /// <summary>Sold per minute, averaged over about an hour</summary>
    public Dictionary<string, double> SoldRate { get; init; } = new();

    /// <summary>Sold since the last update</summary>
    public Dictionary<string, double> SoldSinceUpdate { get; init; } = new();

    /// <summary>The price multiplier per resource the latest update came to (missing = 1)</summary>
    public Dictionary<string, double> Multipliers { get; init; } = new();

    /// <summary>The multipliers networks were last solved with: small steps add up until they matter</summary>
    public Dictionary<string, double> Announced { get; init; } = new();

    public DateTime LastUpdate { get; set; }

    /// <summary>The price multiplier the hub pays with now (the last announced one, so all networks agree)</summary>
    public double Multiplier(string resource) => Announced.TryGetValue(resource, out var m) ? m : 1;

    /// <summary>Market Insight: the multiplier the next updates move towards, with what was sold lately</summary>
    public double Heading(string resource, double demand)
    {
        var rate = SoldRate.TryGetValue(resource, out var r) ? r : 0;
        return rate <= 1e-9 ? MaxMultiplier : Math.Round(Math.Clamp(Math.Sqrt(demand / rate), MinMultiplier, MaxMultiplier), 3);
    }

    public void AddSold(string resource, double amount) =>
        SoldSinceUpdate[resource] = (SoldSinceUpdate.TryGetValue(resource, out var v) ? v : 0) + amount;

    /// <summary>Updates the averages and prices. Returns true when a price changed noticeably.</summary>
    public bool Update(DateTime now, Func<string, double> demand, IEnumerable<string> resources)
    {
        var minutes = Math.Max(1, (now - LastUpdate).TotalMinutes);
        LastUpdate = now;
        var changed = false;
        // Exponential average with a time constant of an hour
        var weight = 1 - Math.Exp(-minutes / 60);
        foreach (var resource in resources)
        {
            var sold = (SoldSinceUpdate.TryGetValue(resource, out var s) ? s : 0) / minutes;
            var rate = (SoldRate.TryGetValue(resource, out var r) ? r : 0) * (1 - weight) + sold * weight;
            SoldRate[resource] = rate;
            var wanted = demand(resource);
            var multiplier = rate <= 1e-9 ? MaxMultiplier : Math.Clamp(Math.Sqrt(wanted / rate), MinMultiplier, MaxMultiplier);
            multiplier = Math.Round(multiplier, 3);
            Multipliers[resource] = multiplier;
            if (Math.Abs(multiplier - Multiplier(resource)) > 0.01)
            {
                Announced[resource] = multiplier;
                changed = true;
            }
        }
        SoldSinceUpdate.Clear();
        return changed;
    }
}
