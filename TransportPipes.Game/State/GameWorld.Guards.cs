namespace TransportPipes.Game.State;

/// <summary>
/// The money's guards (step 2 of the redesign, docs/playtest-redesign.html): two accounts on one device trade at the
/// seller's hub price, goods that only fill a Storage are paid at the seller's hub price, a stand-in pays the old price
/// only to a crew mate with 10 play days on another device, and a usual partner's running trade comes first in the
/// Yard: nobody's trade is cut by someone else's arrival.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>How long a device signal counts (v1.2 §3: the same browser id within 30 days, or the same IP the same day)</summary>
    private static readonly TimeSpan DeviceMemory = TimeSpan.FromDays(30);

    /// <summary>How many days back a usual partner's trade counts</summary>
    private const int UsualTradeDays = 7;

    /// <summary>A company's player was here with this browser id and (a day's hash of) this IP address</summary>
    public void SeenOn(Company company, string? browserId, string? ipDayHash)
    {
        foreach (var stale in company.Devices.Where(t => LastTick - t.Value >= DeviceMemory).Select(t => t.Key).ToList())
        {
            company.Devices.Remove(stale);
        }
        if (!string.IsNullOrWhiteSpace(browserId))
        {
            company.Devices[$"b:{browserId.Trim()[..Math.Min(64, browserId.Trim().Length)]}"] = LastTick;
        }
        if (!string.IsNullOrWhiteSpace(ipDayHash))
        {
            company.Devices[$"ip:{ipDayHash}"] = LastTick;
        }
        company.Touch();
    }

    /// <summary>Whether two players share a device: a browser id both used in the last 30 days, or an IP address on the same day</summary>
    public bool SameDevice(Company a, Company b) =>
        a.Id != b.Id && !a.IsNpc && !b.IsNpc
        && a.Devices.Any(t => LastTick - t.Value < DeviceMemory && b.Devices.TryGetValue(t.Key, out var seen) && LastTick - seen < DeviceMemory);

    /// <summary>
    /// Whether a sleeper shared a device with a partner: a sleeper hasn't played for 30 days by definition, so the
    /// signals it was last seen with count whatever their age (it keeps them until it comes back)
    /// </summary>
    public bool SharedDeviceWithSleeper(Company sleeper, Company partner) =>
        sleeper.Id != partner.Id && !partner.IsNpc && sleeper.Devices.Keys.Any(partner.Devices.ContainsKey);

    private int WorldDay => (int)Math.Floor((LastTick - DateTime.UnixEpoch).TotalDays);

    private static string UsualKey(long buyer, string resource) => $"{buyer}:{resource}";

    /// <summary>A trade's buckets moved on to a day (the newest first), the ones older than a week gone</summary>
    private static (double[] Units, double[] Minutes) UsualBuckets(UsualTrade trade, int day)
    {
        var shift = Math.Max(0, day - trade.Day);
        var units = new double[UsualTradeDays];
        var minutes = new double[UsualTradeDays];
        for (var i = 0; i + shift < UsualTradeDays; i++)
        {
            units[i + shift] = trade.Units[i];
            minutes[i + shift] = trade.Minutes[i];
        }
        return (units, minutes);
    }

    /// <summary>A seller's trade with a buyer this settlement: taken and paid units, over these minutes</summary>
    private void CountUsualTrade(Company seller, Company buyer, string resource, double units, double minutes)
    {
        if (seller.IsNpc || buyer.IsNpc || seller.Id == buyer.Id || units <= 1e-12)
        {
            return;
        }
        var key = UsualKey(buyer.Id, resource);
        var day = WorldDay;
        var trade = seller.UsualTrades.GetValueOrDefault(key) ?? new UsualTrade { Day = day };
        var (bucketUnits, bucketMinutes) = UsualBuckets(trade, day);
        bucketUnits[0] += units;
        bucketMinutes[0] += minutes;
        seller.UsualTrades[key] = new UsualTrade { Day = day, Units = bucketUnits, Minutes = bucketMinutes };
        seller.Touch();
    }

    /// <summary>
    /// What a usual partner's running trade is per minute: what the buyer took from the seller over the last 7 days,
    /// per minute that trade ran (0: they haven't traded that good this week)
    /// </summary>
    public double UsualRate(Company seller, Company buyer, string resource)
    {
        if (!seller.UsualTrades.TryGetValue(UsualKey(buyer.Id, resource), out var trade))
        {
            return 0;
        }
        var (units, minutes) = UsualBuckets(trade, WorldDay);
        var ran = minutes.Sum();
        return ran <= 1e-9 ? 0 : units.Sum() / ran;
    }

    /// <summary>What each buyer took of a seller's goods since a moment (by whole days, at most a week): the return card's "Ben took your sand"</summary>
    public Dictionary<(long Buyer, string Resource), double> TakenSince(Company seller, DateTime since)
    {
        var today = WorldDay;
        var first = (int)Math.Floor((since - DateTime.UnixEpoch).TotalDays);
        var taken = new Dictionary<(long, string), double>();
        foreach (var (key, trade) in seller.UsualTrades)
        {
            var (units, _) = UsualBuckets(trade, today);
            var sum = Enumerable.Range(0, Math.Min(UsualTradeDays, today - first + 1)).Sum(i => units[i]);
            var parts = key.Split(':', 2);
            if (sum > 1e-6 && parts.Length == 2 && long.TryParse(parts[0], out var buyer))
            {
                taken[(buyer, parts[1])] = sum;
            }
        }
        return taken;
    }

    /// <summary>Usual trades older than a week are forgotten (once a day)</summary>
    private void ForgetOldUsualTrades()
    {
        var day = WorldDay;
        foreach (var company in _companies.Values.Where(t => t.UsualTrades.Count > 0))
        {
            foreach (var key in company.UsualTrades.Where(t => day - t.Value.Day >= UsualTradeDays).Select(t => t.Key).ToList())
            {
                company.UsualTrades.Remove(key);
                company.Touch();
            }
        }
    }
}
