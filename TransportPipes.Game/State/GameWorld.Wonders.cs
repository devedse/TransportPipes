using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>A Wonder being built: a District Wonder or the Era Wonder of the Capital</summary>
public sealed class Wonder
{
    public required string Type { get; init; }

    /// <summary>The district of a District Wonder; null for the Era Wonder</summary>
    public DistrictCoord? District { get; init; }

    /// <summary>The stage being built; equal to the number of stages when the Wonder is finished</summary>
    public int Stage { get; set; }

    /// <summary>What the current stage needs and what was delivered so far</summary>
    public Dictionary<string, double> Required { get; set; } = new();
    public Dictionary<string, double> Delivered { get; set; } = new();

    /// <summary>The value (in gold at base prices) every company delivered</summary>
    public Dictionary<long, double> Contributions { get; init; } = new();

    public DateTime? FinishedAt { get; set; }

    public bool Finished => FinishedAt != null;
}

public enum WonderTier
{
    None,
    Bronze,
    Silver,
    Gold,
    Diamond,
}

/// <summary>
/// District Wonders, the Era Wonder and Eras (GDD §8.8, §9.3, §9.4). Everyone delivers to them from their storages; a
/// finished District Wonder gives its district a perk for the season, a finished Era Wonder starts the next Era (and so
/// does the Era's deadline).
/// </summary>
public sealed partial class GameWorld
{
    public static readonly DistrictCoord Capital = new(0, 0);
    public static readonly TimeSpan EraLength = TimeSpan.FromDays(21);
    public const int LastEra = 5;

    private readonly Dictionary<DistrictCoord, Wonder> _districtWonders = new();

    public Wonder? EraWonder { get; private set; }

    /// <summary>When the current Era began; it ends at the latest EraLength later</summary>
    public DateTime EraStartedAt { get; set; }

    public DateTime EraDeadline => EraStartedAt + EraLength;

    public IReadOnlyDictionary<DistrictCoord, Wonder> DistrictWonders => _districtWonders;

    public void LoadWonders(DateTime eraStartedAt, Wonder? eraWonder, IEnumerable<Wonder> districtWonders)
    {
        EraStartedAt = eraStartedAt;
        EraWonder = eraWonder;
        foreach (var wonder in districtWonders.Where(t => t.District != null))
        {
            _districtWonders[wonder.District!.Value] = wonder;
        }
    }

    public WonderDefinition WonderDefinition(Wonder wonder) =>
        (wonder.District == null ? Content.EraWonders : Content.DistrictWonders).First(t => t.Id == wonder.Type);

    /// <summary>The District Wonder of a district (the Capital builds the Era Wonder next to it)</summary>
    public Wonder? DistrictWonder(DistrictCoord district)
    {
        if (Content.DistrictWonders.Count == 0)
        {
            return null;
        }
        if (!_districtWonders.TryGetValue(district, out var wonder))
        {
            // Every district has its own Wonder, picked from the seed
            var index = (int)(Noise.Hash(Generator.Seed, 5003, district.X, district.Y) % (ulong)Content.DistrictWonders.Count);
            var definition = Content.DistrictWonders[index];
            wonder = new Wonder { Type = definition.Id, District = district };
            wonder.Required = StageRequirements(definition, 0, district);
            _districtWonders[district] = wonder;
        }
        return wonder;
    }

    /// <summary>Makes sure the Era Wonder of the current Era exists (and the Era has a start)</summary>
    private void EnsureEraWonder()
    {
        if (EraStartedAt == default)
        {
            EraStartedAt = LastTick;
        }
        if (EraWonder == null && Content.EraWonders.FirstOrDefault(t => t.Era == Era) is { } definition)
        {
            EraWonder = new Wonder { Type = definition.Id, Required = StageRequirements(definition, 0, null) };
        }
    }

    public int ActiveCompaniesEverywhere()
    {
        var since = LastTick - ActivePeriod;
        return _companies.Values.Count(t => !t.IsNpc && t.LastSeenAt >= since);
    }

    /// <summary>
    /// A Wonder stage sized like a project's basket (v1.2 §13): six hours of T(L) for every active player of the district
    /// (everywhere for the Era Wonder), at least one, in the stage's own mix of goods, so a district of one human still
    /// finishes its stage
    /// </summary>
    private Dictionary<string, double> StageRequirements(WonderDefinition definition, int stage, DistrictCoord? district)
    {
        var people = _companies.Values.Where(t => !t.IsNpc && IsActive(t) && (district == null || t.District == district)).ToList();
        var worth = people.Count > 0
            ? people.Sum(t => WonderHours * 60 * TypicalIncome(t.Level))
            : WonderHours * 60 * TypicalIncome(5);
        var goods = definition.Stages[stage];
        var base_ = goods.Sum(t => t.Value * BasePrice(t.Key));
        var scale = base_ <= 0 ? 1 : worth / base_;
        return goods.ToDictionary(t => t.Key, t => Math.Max(10, Math.Round(t.Value * scale / 10) * 10));
    }

    private const double WonderHours = 6;

    // ---------------------------------------------------------------- Perks

    /// <summary>The perk of the finished District Wonder of a district</summary>
    private double PerkAmount(DistrictCoord district, WonderPerk perk) =>
        _districtWonders.TryGetValue(district, out var wonder) && wonder.Finished && WonderDefinition(wonder) is { } definition && definition.Perk == perk
            ? definition.PerkAmount
            : 0;

    private double WonderOutputFactor(Building building) =>
        _districtWonders.Count == 0 ? 1 : 1 + PerkAmount(WorldLayout.DistrictOf(building.X, building.Y), WonderPerk.Production);

    private double WonderResearchFactor(Building building) =>
        _districtWonders.Count == 0 ? 1 : 1 + PerkAmount(WorldLayout.DistrictOf(building.X, building.Y), WonderPerk.Research);

    private double WonderDemandFactor(DistrictCoord district) => _districtWonders.Count == 0 ? 1 : 1 + PerkAmount(district, WonderPerk.HubDemand);

    private double WonderCargoFactor(Company company) => 1 - PerkAmount(company.District, WonderPerk.Cargo);

    // ---------------------------------------------------------------- Deliveries

    /// <summary>Delivers what the storages have for the current stage of a Wonder</summary>
    public CommandResult DeliverToWonder(long companyId, bool eraWonder)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        EnsureEraWonder();
        var wonder = eraWonder ? EraWonder : DistrictWonder(company.District);
        if (wonder == null)
        {
            return CommandResult.Fail("There is no Wonder to deliver to.");
        }
        if (wonder.Finished)
        {
            return CommandResult.Fail("This Wonder is finished.");
        }
        var materials = Materials(company.Id);
        var delivery = new Dictionary<string, double>();
        foreach (var (resource, required) in wonder.Required)
        {
            var missing = required - wonder.Delivered.GetValueOrDefault(resource);
            var amount = Math.Min(missing, materials.GetValueOrDefault(resource));
            if (amount > 1e-9)
            {
                delivery[resource] = Math.Floor(amount * 1000) / 1000;
            }
        }
        if (delivery.Values.Sum() < 1e-6)
        {
            var names = string.Join(", ", wonder.Required.Where(t => wonder.Delivered.GetValueOrDefault(t.Key) < t.Value).Select(t => Content.Resources[t.Key].Name.ToLowerInvariant()));
            return CommandResult.Fail($"Your storages have nothing it needs ({names}).");
        }
        var payment = Pay(company, 0, delivery);
        if (!payment.Success)
        {
            return payment;
        }
        var value = 0.0;
        foreach (var (resource, amount) in delivery)
        {
            wonder.Delivered[resource] = wonder.Delivered.GetValueOrDefault(resource) + amount;
            value += amount * BasePrice(resource);
        }
        wonder.Contributions[company.Id] = wonder.Contributions.GetValueOrDefault(company.Id) + value;
        AddStat(company, "wonder", value);
        // Building a Wonder is as good for your company's experience as selling the goods
        GainXpFromEarnings(company, value);
        company.Touch();
        Changes.Companies.Add(company.Id);
        Changes.Wonders = true;

        if (wonder.Required.All(t => wonder.Delivered.GetValueOrDefault(t.Key) >= t.Value - 1e-6))
        {
            CompleteStage(wonder);
        }
        return CommandResult.Ok;
    }

    private void CompleteStage(Wonder wonder)
    {
        var definition = WonderDefinition(wonder);
        wonder.Stage++;
        wonder.Delivered.Clear();
        var everyone = wonder.District is { } district
            ? _companies.Values.Where(t => !t.IsNpc && t.District == district)
            : _companies.Values.Where(t => !t.IsNpc);
        if (wonder.Stage < definition.Stages.Count)
        {
            wonder.Required = StageRequirements(definition, wonder.Stage, wonder.District);
            foreach (var company in everyone)
            {
                Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Wonder, $"{definition.Name}: stage {wonder.Stage} of {definition.Stages.Count} is built!"));
            }
            MarkWonderTiles(wonder);
            return;
        }

        wonder.Required.Clear();
        wonder.FinishedAt = LastTick;
        MarkWonderTiles(wonder);
        if (wonder.District is { } home)
        {
            foreach (var company in everyone)
            {
                Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Wonder, $"The {definition.Name} is finished! {definition.Description}"));
            }
            // The perk changes production and prices in the district
            foreach (var network in _networks.Values.Where(t => t.District == home))
            {
                network.NeedsSolve = true;
                Changes.Networks.Add(network.Id);
            }
            Changes.Markets.Add(home);
        }
        else if (Era >= LastEra)
        {
            StartFinale();
        }
        else
        {
            AdvanceEra();
        }
    }

    /// <summary>The hub (with the Wonder) is drawn again</summary>
    private void MarkWonderTiles(Wonder wonder)
    {
        Changes.Wonders = true;
        if (wonder.District is { } district)
        {
            Changes.Districts.Add(district);
        }
        else
        {
            Changes.Districts.Add(Capital);
        }
    }

    // ---------------------------------------------------------------- Eras

    /// <summary>The share of the Era Wonder a company built, and the tier that gives</summary>
    public (double Share, WonderTier Tier) EraWonderTier(Company company)
    {
        if (EraWonder == null)
        {
            return (0, WonderTier.None);
        }
        var total = EraWonder.Contributions.Values.Sum();
        var mine = EraWonder.Contributions.GetValueOrDefault(company.Id);
        if (total <= 0 || mine <= 0)
        {
            return (0, WonderTier.None);
        }
        // v1.2 §13: the share of your own Era output you delivered, so a small company counts as much as a big one
        var share = mine / Math.Max(1, company.EraOutput);
        return (share, share >= 0.15 ? WonderTier.Diamond : share >= 0.07 ? WonderTier.Gold : share >= 0.03 ? WonderTier.Silver : share >= 0.01 ? WonderTier.Bronze : WonderTier.None);
    }

    public static double TierReward(WonderTier tier, int era) => era * tier switch
    {
        WonderTier.Diamond => 100_000,
        WonderTier.Gold => 40_000,
        WonderTier.Silver => 10_000,
        WonderTier.Bronze => 2_000,
        _ => 0,
    };

    /// <summary>The next Era: rewards for the Era Wonder's builders, new content, a launch party</summary>
    public void AdvanceEra()
    {
        if (Era >= LastEra)
        {
            return;
        }
        EnsureEraWonder();
        if (EraWonder != null)
        {
            var name = WonderDefinition(EraWonder).Name;
            foreach (var company in _companies.Values.Where(t => !t.IsNpc))
            {
                var (_, tier) = EraWonderTier(company);
                if (tier == WonderTier.None)
                {
                    continue;
                }
                var reward = TierReward(tier, Era);
                company.Gold += reward;
                company.Titles.Add($"{name} · {tier}");
                company.Touch();
                Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Wonder, $"{tier} builder of {name}: +{reward:N0} gold"));
            }
        }

        Era++;
        EraStartedAt = LastTick;
        // The tiers of the next Era Wonder are a share of what each makes in that Era
        foreach (var company in _companies.Values)
        {
            company.EraOutput = 0;
        }
        EraWonder = null;
        EnsureEraWonder();
        foreach (var company in _companies.Values.Where(t => !t.IsNpc))
        {
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Wonder, $"Era {Era} begins! New buildings and resources, and +10% production for a day"));
            Changes.Companies.Add(company.Id);
        }
        // Hub demand grows with the Era, new buildings unlock
        foreach (var network in _networks.Values)
        {
            network.NeedsSolve = true;
            Changes.Networks.Add(network.Id);
        }
        foreach (var district in _openDistricts)
        {
            Changes.Markets.Add(district);
        }
        Changes.Districts.Add(Capital);
        Changes.Wonders = true;
        // A quick next Era ends the party of the last one (both would be called after the current Era)
        foreach (var launch in _events.Where(t => t.Kind == EventKind.EraLaunch && t.Started && !t.Ended).ToList())
        {
            launch.EndsAt = LastTick;
            EndEvent(launch);
        }
        StartEventNow(EventKind.EraLaunch, null, TimeSpan.FromDays(1));
    }

    /// <summary>The Era's deadline in a span: the next Era starts even when the Wonder isn't finished</summary>
    private IEnumerable<(DateTime At, Action Run)> EraMoments(DateTime from, DateTime to)
    {
        if (Content.EraWonders.Count != 0 && EraStartedAt != default && EraDeadline <= to && FinaleAt == null)
        {
            yield return (EraDeadline > from ? EraDeadline : from, Era < LastEra ? AdvanceEra : StartFinale);
        }
    }
}
