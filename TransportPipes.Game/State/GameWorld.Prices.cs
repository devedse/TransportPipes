namespace TransportPipes.Game.State;

/// <summary>One of a seller's prices (a multiple of the base): what it is, and a rise that waits for its hour</summary>
public sealed class PriceSetting
{
    public double Value { get; set; }

    /// <summary>A rise that counts at PendingAt: a new value, or (Back) the setting goes and the price is what it inherits</summary>
    public double? Pending { get; set; }
    public bool Back { get; set; }
    public DateTime? PendingAt { get; set; }
}

/// <summary>
/// Your own prices (1.4 §8): for every good you sell to players, a public price for everyone (1.3× by default), a crew
/// price for your Yard (1.15×) and special prices for named players ("Ben: 1.0×"), each a multiple of the good's base
/// price between the market's two prices (1× and 2.5×), so trading with a friend is never worse than the market for
/// either side. A buyer pays the lowest price that applies to them. A rise waits an hour and the buyers are told; a cut
/// counts at once. Keys: "public", "crew", "public:good", "crew:good", "for:companyId".
/// </summary>
public sealed partial class GameWorld
{
    public const double MinPriceFactor = 1.0;
    public const double MaxPriceFactor = 2.5;
    public const double DefaultPublicPrice = 1.3;
    public const double DefaultCrewPrice = 1.15;
    public static readonly TimeSpan PriceRiseNotice = TimeSpan.FromHours(1);

    public static string PublicKey(string? good = null) => good == null ? "public" : $"public:{good}";

    public static string CrewKey(string? good = null) => good == null ? "crew" : $"crew:{good}";

    public static string ForKey(long companyId) => $"for:{companyId}";

    private static double? Setting(Company seller, string key) => seller.Prices.TryGetValue(key, out var setting) ? setting.Value : null;

    /// <summary>A seller's price for a buyer (a multiple of the base): the lowest of its public, crew and special prices that apply</summary>
    public double PriceFactor(Company seller, Company buyer, string good)
    {
        var price = Setting(seller, PublicKey(good)) ?? Setting(seller, PublicKey()) ?? DefaultPublicPrice;
        if (seller.CrewId != null && seller.CrewId == buyer.CrewId)
        {
            price = Math.Min(price, Setting(seller, CrewKey(good)) ?? Setting(seller, CrewKey()) ?? DefaultCrewPrice);
        }
        if (Setting(seller, ForKey(buyer.Id)) is { } special)
        {
            price = Math.Min(price, special);
        }
        return Math.Clamp(price, MinPriceFactor, MaxPriceFactor);
    }

    /// <summary>What a buyer pays a seller for a unit of a good</summary>
    public double PriceFor(Company seller, Company buyer, string good) => Math.Round(BasePrice(good) * PriceFactor(seller, buyer, good), 4);

    /// <summary>A seller's price class for a buyer: its multiple in hundredths (the gate's buyers take the cheapest first)</summary>
    private int PriceClass(Company seller, Company buyer, string good) => (int)Math.Round(PriceFactor(seller, buyer, good) * 100);

    /// <summary>The gate node that sells at a price class (its owner as the solver sees it, never a company)</summary>
    private static long PriceClassOwner(int priceClass) => -1_000 - priceClass;

    /// <summary>The price per unit of a gate node of a price class, or null for a node that isn't one</summary>
    private double? PriceClassPrice(long owner, string good) =>
        owner <= -1_000 ? Math.Round(BasePrice(good) * (-owner - 1_000) / 100.0, 4) : null;

    /// <summary>
    /// Sets one of a company's prices (null: back to its default): a cut counts at once, a rise waits an hour and the
    /// buyers who take that good from it hear of it now
    /// </summary>
    public CommandResult SetPrice(long companyId, string key, double? value)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        var parts = key.Split(':', 2);
        var kind = parts[0];
        if (kind is not ("public" or "crew" or "for") || (parts.Length == 2 && kind != "for" && !Content.Resources.ContainsKey(parts[1])))
        {
            return CommandResult.Fail("Unknown price.");
        }
        if (kind == "for" && (parts.Length != 2 || !long.TryParse(parts[1], out var other) || Company(other) is null || other == companyId))
        {
            return CommandResult.Fail("Choose another player.");
        }
        if (value is { } v && (double.IsNaN(v) || v < MinPriceFactor - 1e-9 || v > MaxPriceFactor + 1e-9))
        {
            return CommandResult.Fail($"A price is between {MinPriceFactor:0.##}× and {MaxPriceFactor:0.##}× the base: never worse than the market for either side.");
        }
        var before = company.Prices.GetValueOrDefault(key);
        // What the key comes to without a setting of its own: a good's price its general one, a special nothing
        var inherited = kind switch
        {
            "public" => parts.Length == 2 ? Setting(company, PublicKey()) ?? DefaultPublicPrice : DefaultPublicPrice,
            "crew" => parts.Length == 2 ? Setting(company, CrewKey()) ?? DefaultCrewPrice : DefaultCrewPrice,
            _ => (double?)null,
        };
        var current = before?.Value ?? inherited;
        var next = value ?? inherited;
        if (current == null || (next != null && next <= current + 1e-9))
        {
            // A cut counts at once (and a rise that waited is off)
            if (value == null)
            {
                company.Prices.Remove(key);
            }
            else
            {
                company.Prices[key] = new PriceSetting { Value = Math.Round(value.Value, 2) };
            }
        }
        else
        {
            // A rise waits an hour: the buyers hear of it now
            before ??= new PriceSetting { Value = current.Value };
            before.Pending = next is { } n ? Math.Round(n, 2) : null;
            before.Back = value == null;
            before.PendingAt = LastTick + PriceRiseNotice;
            company.Prices[key] = before;
            TellPriceRise(company, key, next);
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkTradeDirty(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>The buyers who took goods from a company this week, of a good (null: any), hear of a rise an hour ahead</summary>
    private void TellPriceRise(Company seller, string key, double? factor)
    {
        var parts = key.Split(':', 2);
        var good = parts[0] != "for" && parts.Length == 2 ? parts[1] : null;
        var only = parts[0] == "for" ? long.Parse(parts[1]) : (long?)null;
        var buyers = seller.UsualTrades.Keys
            .Select(t => t.Split(':', 2))
            .Where(t => t.Length == 2 && (good == null || t[1] == good) && long.TryParse(t[0], out var id) && (only == null || id == only))
            .Select(t => long.Parse(t[0]))
            .Distinct()
            .Select(Company)
            .OfType<Company>()
            .Where(t => parts[0] != "crew" || t.CrewId == seller.CrewId);
        var what = good != null ? GoodsWord(good) : "goods";
        foreach (var buyer in buyers)
        {
            Changes.Events.Add(new WorldEvent(buyer.Id, WorldEventKind.Neighbour, factor is { } f
                ? $"{seller.Name}'s price for {what} rises to {f:0.##}× in an hour"
                : $"{seller.Name}'s special price for you ends in an hour"));
        }
    }

    /// <summary>The moments a waiting rise counts</summary>
    private IEnumerable<(DateTime At, Action Run)> PriceMoments(DateTime from, DateTime to)
    {
        foreach (var company in _companies.Values.Where(t => t.Prices.Count > 0).OrderBy(t => t.Id).ToList())
        {
            foreach (var (key, setting) in company.Prices.Where(t => t.Value.PendingAt is { } at && at <= to).OrderBy(t => t.Key, StringComparer.Ordinal).ToList())
            {
                yield return (setting.PendingAt!.Value > from ? setting.PendingAt.Value : from, () => ApplyPrice(company, key));
            }
        }
    }

    private void ApplyPrice(Company company, string key)
    {
        if (!company.Prices.TryGetValue(key, out var setting) || setting.PendingAt == null)
        {
            return;
        }
        if (setting.Back)
        {
            company.Prices.Remove(key);
        }
        else
        {
            setting.Value = setting.Pending ?? setting.Value;
            setting.Pending = null;
            setting.PendingAt = null;
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkTradeDirty(company.Id);
    }
}
