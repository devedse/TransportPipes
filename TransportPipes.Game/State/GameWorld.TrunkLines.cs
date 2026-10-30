using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// A trunk line (1.4 §20): from a town gate of one town through the wilderness to the facing gate of a neighbouring
/// town, owned by whoever built it (its carrier). It carries links between players of the two towns and sales to the
/// other town's market, for a fee the carrier sets.
/// </summary>
public sealed class TrunkLine : Entity
{
    public long Id { get; init; }

    /// <summary>The carrier: who built it, and keeps it when they move or stop playing</summary>
    public long Owner { get; init; }

    public DistrictCoord From { get; init; }
    public DistrictCoord To { get; init; }

    /// <summary>The pipe tier it is built in (composite, glass conduit or superconductor)</summary>
    public int Tier { get; set; }

    /// <summary>Its corners (from the first town's gate to the other's) and its length in tiles</summary>
    public List<TilePos> Route { get; init; } = new();
    public int Length { get; init; }

    public DateTime BuiltAt { get; init; }

    /// <summary>The fees, as a share of the goods' base value, paid by the seller: anyone's, the carrier's crew's, named players'</summary>
    public double PublicFee { get; set; } = GameWorld.DefaultLineFee;
    public double CrewFee { get; set; } = GameWorld.DefaultLineFee / 2;
    public Dictionary<long, double> SpecialFees { get; set; } = new();

    /// <summary>Last round: what crossed it a minute, and the fees it earned in all</summary>
    public double Carried { get; set; }
    public double Earned { get; set; }

    public bool Joins(DistrictCoord a, DistrictCoord b) => (From == a && To == b) || (From == b && To == a);
}

/// <summary>
/// Trunk lines (1.4 §20): from Era 3 anyone who researched Trunk Lines builds one to a neighbouring town and is its
/// carrier. The right of way is bought once (no rent: rule 7), and whoever builds a route first has it. A link to a player
/// of another town needs a line between the two towns; a company may also send a good over a line to the other town's
/// market, where its Hub Order is half of its own town's. The Lines track makes lines cheaper and lose less, and its
/// master builds superconductor lines from Era 3.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>The right of way of a tile (provisional until the model plays Era 3)</summary>
    public const double LineTilePrice = 10_000_000;

    /// <summary>A new line's fees, and the most a carrier may ask (of the goods' base value)</summary>
    public const double DefaultLineFee = 0.05;
    public const double MaxLineFee = 0.1;

    /// <summary>Of a company's own town's Hub Order, what it gets at another town's market (1.4 §20)</summary>
    public const double ExportHubOrderShare = 0.5;

    private readonly Dictionary<long, TrunkLine> _lines = new();

    public long NextLineId { get; set; } = 1;
    public IEnumerable<TrunkLine> TrunkLines => _lines.Values;

    public void LoadTrunkLines(IEnumerable<TrunkLine> lines)
    {
        foreach (var line in lines)
        {
            _lines[line.Id] = line;
            NextLineId = Math.Max(NextLineId, line.Id + 1);
        }
    }

    public TrunkLine? TrunkLine(long id) => _lines.GetValueOrDefault(id);

    /// <summary>The line between two towns, if anyone built one</summary>
    public TrunkLine? LineBetween(DistrictCoord a, DistrictCoord b) => _lines.Values.FirstOrDefault(t => t.Joins(a, b));

    /// <summary>The towns a line could reach from a town: its four neighbours that are open</summary>
    public IEnumerable<DistrictCoord> LineNeighbours(DistrictCoord town) =>
        new[] { new DistrictCoord(town.X + 1, town.Y), new DistrictCoord(town.X - 1, town.Y), new DistrictCoord(town.X, town.Y + 1), new DistrictCoord(town.X, town.Y - 1) }
            .Where(_openDistricts.Contains);

    /// <summary>
    /// A line's route: from the middle of a town's gate facing the other town, straight along the road's line through
    /// the wilderness (towns of a row or a column line up), to the middle of the facing gate
    /// </summary>
    public static List<TilePos> LineRoute(DistrictCoord from, DistrictCoord to)
    {
        var (a, b) = (from.X != to.X || from.Y != to.Y) && (from.X < to.X || from.Y < to.Y) ? (from, to) : (to, from);
        if (a.Y == b.Y)
        {
            // Side by side: from a's right gate to b's left gate, along the road's second lane
            var y = WorldLayout.DistrictOriginY(a.Y) + WorldLayout.RoadY + 2;
            return [new TilePos(WorldLayout.DistrictOriginX(a.X) + WorldLayout.DistrictWidth - 1, y), new TilePos(WorldLayout.DistrictOriginX(b.X), y)];
        }
        var x = WorldLayout.DistrictOriginX(a.X) + WorldLayout.RoadX + 2;
        return [new TilePos(x, WorldLayout.DistrictOriginY(a.Y) + WorldLayout.DistrictHeight - 1), new TilePos(x, WorldLayout.DistrictOriginY(b.Y))];
    }

    /// <summary>The tiers a company may build lines in: composite and glass with their research, superconductor with its own or as the Lines track's master</summary>
    public IEnumerable<int> LineTiers(Company company) =>
        Content.PipeTiers.Where(t => t.Tier >= 4 && t.Era <= Math.Max(3, YourEra(company))
                && (t.Research == null || HasResearch(company, t.Research) || (t.Tier == 6 && TrackTier(company, "lines") >= Content.TrackRules.Tiers.Count)))
            .Select(t => t.Tier);

    /// <summary>The Lines track's discount on a carrier's tiles and its cut of their loss: 5% and 10% a tier</summary>
    private double LinesCheaper(Company company) => 1 - 0.05 * TrackTier(company, "lines");

    private double LinesLeaner(long carrier) => Company(carrier) is { } owner ? Math.Max(0, 1 - 0.1 * TrackTier(owner, "lines")) : 1;

    /// <summary>What reaches the far end of a line: its tier's loss over its length, less with its carrier's Lines tiers</summary>
    public double LineEfficiency(TrunkLine line) => Math.Pow(1 - Content.PipeTier(line.Tier).LossPerTile * LinesLeaner(line.Owner), line.Length);

    public double LineThroughput(TrunkLine line) => Content.PipeTier(line.Tier).Throughput;

    /// <summary>What a line to a town would be: its route, length, price and why it can't be (or null)</summary>
    public sealed record LinePlan(List<TilePos> Route, int Length, double Gold, IReadOnlyDictionary<string, double> Materials, string? Error);

    public LinePlan PlanLine(long companyId, DistrictCoord to, int tier)
    {
        LinePlan Fail(string error) => new([], 0, 0, new Dictionary<string, double>(), error);
        if (Company(companyId) is not { } company)
        {
            return Fail("Unknown company.");
        }
        if (YourEra(company) < 3 || !HasResearch(company, "trunkLines"))
        {
            return Fail("Trunk lines come with Era 3 and the Trunk Lines research.");
        }
        if (!LineNeighbours(company.District).Contains(to))
        {
            return Fail("A line runs to a town next to yours: across the wilderness from one of your town's gates.");
        }
        if (LineBetween(company.District, to) is { } taken)
        {
            return Fail($"{Company(taken.Owner)?.Name ?? "Someone"} built the line to {WorldNames.District(to)} first.");
        }
        if (!LineTiers(company).Contains(tier) || Content.PipeTiers.FirstOrDefault(t => t.Tier == tier) is not { } pipe)
        {
            return Fail("Research that pipe first.");
        }
        var route = LineRoute(company.District, to);
        var length = RouteLength(route);
        var gold = Math.Round((LineTilePrice * LinesCheaper(company) + pipe.Gold) * length * CostFactor(company));
        return new LinePlan(route, length, gold, pipe.Materials.ToDictionary(t => t.Key, t => t.Value * length), null);
    }

    /// <summary>Builds a trunk line to a neighbouring town: the right of way and the pipe, once; you are its carrier</summary>
    public CommandResult BuildLine(long companyId, DistrictCoord to, int tier)
    {
        var plan = PlanLine(companyId, to, tier);
        if (plan.Error != null)
        {
            return CommandResult.Fail(plan.Error);
        }
        var company = Company(companyId)!;
        var payment = Pay(company, plan.Gold, plan.Materials);
        if (!payment.Success)
        {
            return payment;
        }
        AddStat(company, "lineGold", plan.Gold);
        var line = new TrunkLine { Id = NextLineId++, Owner = company.Id, From = company.District, To = to, Tier = tier, Route = plan.Route, Length = plan.Length, BuiltAt = LastTick };
        _lines[line.Id] = line;
        LineChanged(line);
        foreach (var other in CompaniesInTown(company.District).Concat(CompaniesInTown(to)).Where(t => t.Id != company.Id))
        {
            Changes.Events.Add(new WorldEvent(other.Id, WorldEventKind.Link,
                $"{company.Name} built a trunk line between {WorldNames.District(line.From)} and {WorldNames.District(line.To)}: links between the towns, and a second market"));
        }
        return CommandResult.Ok;
    }

    /// <summary>A carrier sets its line's fees (0 to 10% of the goods' base value): anyone's, its crew's, a named player's (null: the public fee)</summary>
    public CommandResult SetLineFee(long companyId, long lineId, string who, double? fee)
    {
        if (!_lines.TryGetValue(lineId, out var line) || line.Owner != companyId)
        {
            return CommandResult.Fail("That is not your line.");
        }
        var value = Math.Clamp(fee ?? line.PublicFee, 0, MaxLineFee);
        switch (who)
        {
            case "public":
                line.PublicFee = value;
                break;
            case "crew":
                line.CrewFee = value;
                break;
            default:
                if (!long.TryParse(who, out var partner) || Company(partner) == null)
                {
                    return CommandResult.Fail("Unknown player.");
                }
                if (fee == null)
                {
                    line.SpecialFees.Remove(partner);
                }
                else
                {
                    line.SpecialFees[partner] = value;
                }
                break;
        }
        LineChanged(line);
        return CommandResult.Ok;
    }

    /// <summary>What a seller pays the carrier of a line, as a share of base value: a special fee, the crew's, or the public one (nothing on your own line)</summary>
    public double LineFee(TrunkLine line, Company seller) =>
        seller.Id == line.Owner ? 0
        : line.SpecialFees.TryGetValue(seller.Id, out var special) ? special
        : Company(line.Owner) is { CrewId: { } crew } && seller.CrewId == crew ? Math.Min(line.CrewFee, line.PublicFee)
        : line.PublicFee;

    /// <summary>The line a link between two towns crosses (null: both in one town)</summary>
    public TrunkLine? LineOf(Link link) =>
        Company(link.Seller) is { } seller && Company(link.Buyer) is { } buyer && seller.District != buyer.District ? LineBetween(seller.District, buyer.District) : null;

    /// <summary>The carrier's fee for what crossed its line: the seller pays it from what it earns</summary>
    private void PayLineFee(TrunkLine line, Company seller, string resource, double units)
    {
        var fee = units * BasePrice(resource) * LineFee(line, seller);
        line.Carried += units;
        if (fee <= 1e-9 || Company(line.Owner) is not { } carrier)
        {
            return;
        }
        seller.Gold -= fee;
        AddStat(seller, "lineFees", fee);
        carrier.Gold += fee;
        CountEarned(carrier, fee, "players", resource);
        AddStat(carrier, "carried", fee);
        line.Earned += fee;
        line.Touch();
        seller.Touch();
        carrier.Touch();
        Changes.Companies.Add(seller.Id);
        Changes.Companies.Add(carrier.Id);
    }

    // ---------------------------------------------------------------- The second market

    /// <summary>
    /// A company sends a good over a line to the other town's market (1.4 §20): what its own town's Hub Order doesn't take
    /// sells there, against half of its Hub Order there, less what the line loses and the carrier's fee (null: stops)
    /// </summary>
    public CommandResult SetExport(long companyId, string resource, long? lineId)
    {
        if (Company(companyId) is not { } company || !Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown good.");
        }
        if (lineId is not { } id)
        {
            company.Exports.Remove(resource);
        }
        else
        {
            if (!_lines.TryGetValue(id, out var line) || (line.From != company.District && line.To != company.District))
            {
                return CommandResult.Fail("That line doesn't leave your town.");
            }
            var other = line.From == company.District ? line.To : line.From;
            if (Content.Resources[resource].Era > TownEra(other))
            {
                return CommandResult.Fail($"Goods only go to towns of their Era or later: {WorldNames.District(other)} is in Era {TownEra(other)}.");
            }
            company.Exports[resource] = id;
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkCompanyNetworksDirty(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>The line a company sends a good over to another town's market, and that town (null: it doesn't)</summary>
    private (TrunkLine Line, DistrictCoord Town)? ExportOf(Company company, string resource) =>
        company.Exports.TryGetValue(resource, out var id) && _lines.TryGetValue(id, out var line) && (line.From == company.District || line.To == company.District)
            ? (line, line.From == company.District ? line.To : line.From)
            : null;

    /// <summary>What a company's export of a good adds to its Hub Order at home: half of its Hub Order in the other town, as much as arrives there</summary>
    private double ExportHubOrder(Company company, string resource) =>
        ExportOf(company, resource) is { } export && Content.Resources[resource].Era <= TownEra(export.Town)
            ? ExportHubOrderShare * BaseHubOrder(company, export.Town, resource) * LineEfficiency(export.Line)
            : 0;

    /// <summary>The fees for what a company's sales sent over lines since the last market update: what sold beyond its own town's Hub Order</summary>
    private void PayExportFees(double minutes)
    {
        foreach (var company in _companies.Values.Where(t => t.Exports.Count > 0))
        {
            var sold = HubMarketOf(company, company.District).SoldRate;
            foreach (var resource in company.Exports.Keys.ToList())
            {
                if (ExportOf(company, resource) is not { } export)
                {
                    continue;
                }
                var home = BaseHubOrder(company, company.District, resource);
                var exported = Math.Clamp(sold.GetValueOrDefault(resource) - home, 0, ExportHubOrder(company, resource));
                if (exported > 1e-9)
                {
                    PayLineFee(export.Line, company, resource, exported * minutes);
                }
            }
        }
    }

    private void LineChanged(TrunkLine line)
    {
        line.Touch();
        Changes.Districts.Add(line.From);
        Changes.Districts.Add(line.To);
        if (Company(line.Owner) is { } owner)
        {
            owner.Touch();
            Changes.Companies.Add(owner.Id);
        }
    }
}
