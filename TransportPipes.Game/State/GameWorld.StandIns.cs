using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// The House that keeps an asleep company's trades going (v1.2 §9): what went out of and came in through its Yard gate
/// when it fell asleep, and since when
/// </summary>
public sealed class StandIn
{
    public required string Name { get; init; }
    public DateTime Since { get; init; }

    /// <summary>Per minute: what the sleeper sold through its gate (to the Yard and its links), and what it bought</summary>
    public Dictionary<string, double> Sells { get; init; } = new();
    public Dictionary<string, double> Buys { get; init; } = new();

    /// <summary>
    /// A crew member who played on 10+ days: its partners on other devices keep the old price for a week, then it glides
    /// to House terms; everyone else gets House terms from the first day (an alt can't farm a fake quitter)
    /// </summary>
    public bool OldPriceWeek { get; init; }
}

/// <summary>
/// Asleep in place and stand-ins (v1.2 §9, §10). From day 30 an asleep estate stays where it is and makes and earns
/// nothing; its trades pass that minute to a stand-in House, announced to every partner on day 23, so there is no day
/// on which they stop. Split by direction, so the friend who stays isn't hurt and an alt can't farm it: a partner who
/// buys from the stand-in gets what the sleeper sold at the old price for 14 days, then 10% less a week, and that gold
/// leaves the game; a partner who sells to it is paid NPC gold at the old price (a week longer for a crew member's
/// partners), gliding over 14 days to House terms while the volume shrinks to the NPC share. When the seat's hold ends
/// (GameWorld.Return) the estate is packed for the Quiet Isles and a House warms the seat.
/// </summary>
public sealed partial class GameWorld
{
    public static readonly TimeSpan StandInNoticeAfter = TimeSpan.FromDays(23);
    private const double StandInFullDays = 14;
    private const double StandInGlideDays = 14;
    private const int StandInOldPriceDays = 10;

    private static readonly string[] StandInNames =
    [
        "Old Pete", "Aunt May", "Uncle Bram", "Granny Ida", "Captain Oswin", "Miss Hattie", "Farmer Tobias", "Widow Clem",
        "Old Ned", "Mother Rook", "Sir Barnaby", "Cousin Ottilie",
    ];

    /// <summary>A stand-in takes over: what the sleeper's gate traded now (its Yard flows and its links)</summary>
    private StandIn NewStandIn(Company company)
    {
        var sells = new Dictionary<string, double>();
        var buys = new Dictionary<string, double>();
        if (EstateOf(company) is { } estate && GateNetwork(estate) is { } network)
        {
            foreach (var flow in network.GateFlows.Where(t => t.Estate == KeyOf(estate)))
            {
                Add(flow.Import ? buys : sells, flow.Resource, flow.Import ? flow.Sent : flow.Delivered);
            }
        }
        return new StandIn
        {
            Name = $"{StandInNames[(int)(company.Id % StandInNames.Length)]}",
            Since = LastTick,
            Sells = sells.Where(t => t.Value > 1e-6).ToDictionary(),
            Buys = buys.Where(t => t.Value > 1e-6).ToDictionary(),
            OldPriceWeek = company.CrewId != null && company.DistinctDays >= StandInOldPriceDays,
        };
    }

    private double StandInDays(StandIn standIn) => Math.Max(0, (LastTick - standIn.Since).TotalDays);

    /// <summary>How much of what the sleeper sold the stand-in still sells: all of it for 14 days, then 10% less a week</summary>
    public double StandInSellShare(StandIn standIn) =>
        Math.Clamp(1 - 0.1 * (StandInDays(standIn) - StandInFullDays) / 7, 0, 1);

    /// <summary>How far the stand-in's purchases have glided to House terms (0: the old terms, 1: House terms)</summary>
    public double StandInGlide(StandIn standIn) =>
        Math.Clamp((StandInDays(standIn) - (standIn.OldPriceWeek ? 7 : 0)) / StandInGlideDays, 0, 1);

    /// <summary>
    /// What a sleeper's stand-in pays a partner for a good: for a crew member's partners on another device the old price,
    /// gliding to what a House pays; for everyone else what a House pays
    /// </summary>
    public double StandInBuyPrice(Company sleeper, Company seller, string resource)
    {
        var standIn = sleeper.StandIn!;
        if (!standIn.OldPriceWeek || SharedDeviceWithSleeper(sleeper, seller))
        {
            return HouseBuyPrice(seller, resource);
        }
        var glide = StandInGlide(standIn);
        return PlayerPrice(resource) * (1 - glide) + HouseBuyPrice(seller, resource) * glide;
    }

    /// <summary>The stand-in as its seat's Yard member: what the sleeper sold and bought, as far as the rules still carry it</summary>
    private YardMember StandInMember(Estate estate, Company sleeper, StandIn standIn)
    {
        var member = new YardMember { Key = KeyOf(estate), Company = sleeper, Network = null };
        var sell = StandInSellShare(standIn);
        var buy = 1 - (1 - Content.Progression.Together.NpcCap) * StandInGlide(standIn);
        foreach (var (resource, rate) in standIn.Sells)
        {
            Add(member.Spare, resource, rate * sell);
            // It always sends what it was matched for
            Add(member.Sent, resource, rate * sell);
        }
        foreach (var (resource, rate) in standIn.Buys)
        {
            Add(member.Lacks, resource, rate * buy);
        }
        return member;
    }

    /// <summary>What partners delivered to stand-ins is paid (NPC gold, at the stand-in's price)</summary>
    private void SettleStandIns(double minutes)
    {
        foreach (var sleeper in _companies.Values.Where(t => t.Asleep && t.StandIn != null && t.Packed == null))
        {
            if (EstateOf(sleeper) is not { } estate || !_gateTerms.TryGetValue(KeyOf(estate), out var terms))
            {
                continue;
            }
            foreach (var ((resource, _), sources) in terms.Import)
            {
                foreach (var (sellerId, perMinute) in sources)
                {
                    if (Company(sellerId) is not { } seller || seller.Asleep)
                    {
                        continue;
                    }
                    var units = perMinute * minutes;
                    var gold = units * StandInBuyPrice(sleeper, seller, resource);
                    AddStat(seller, "npcGold", gold);
                    seller.Gold += gold;
                    seller.Report.GoldEarned += gold;
                    Add(seller.Report.Sold, resource, units);
                    AddStat(seller, "soldToPlayers", units);
                    GainXpFromEarnings(seller, gold);
                    seller.Touch();
                    Changes.Companies.Add(seller.Id);
                }
            }
        }
    }

    /// <summary>Everyone who trades with a company: its links' partners, its crew and the players of its station</summary>
    private IEnumerable<Company> PartnersOf(Company company)
    {
        var partners = LinksOf(company.Id).Where(t => t.State != LinkState.Offered).Select(t => Company(t.Partner(company.Id)));
        var crew = CrewOf(company) is { } c ? CrewMembers(c) : [];
        var station = EstateOf(company) is { } estate
            ? BigStation(estate.Station).SelectMany(StationSeats).Select(t => Estate(t.District, t.Slot)?.OwnerId is { } o ? Company(o) : null)
            : [];
        return partners.Concat(crew).Concat(station).OfType<Company>().Where(t => !t.IsNpc && t.Id != company.Id).DistinctBy(t => t.Id);
    }

    /// <summary>Day 23: the partners hear that a stand-in will keep their trades with this company going from day 30</summary>
    private void AnnounceStandIn(Company company)
    {
        company.StandInAnnounced = true;
        company.Touch();
        foreach (var partner in PartnersOf(company))
        {
            Changes.Events.Add(new WorldEvent(partner.Id, WorldEventKind.Neighbour,
                $"{company.Name} hasn't played for three weeks: from next week a stand-in House ⚙ keeps your trades with them going"));
        }
    }

    /// <summary>For testing: the hold is over now</summary>
    public void PackForTesting(Company company)
    {
        if (company.Asleep && company.Packed == null)
        {
            PackAsleep(company);
        }
    }

    /// <summary>The hold ends: the asleep estate travels safely to the Quiet Isles (nothing is lost) and a House warms its seat</summary>
    private void PackAsleep(Company company)
    {
        var seat = EstateOf(company);
        // Its landing card when it comes back
        company.LastSeat = seat is { } home ? KeyOf(home) : null;
        company.StandIn = null;
        company.KeepAsked = false;
        if (CrewOf(company) is { } crew && crew.KeepCards.Remove(company.Id))
        {
            CrewChanged(crew);
        }
        EndTradeOf(company);
        Pack(company);
        company.Touch();
        Changes.Companies.Add(company.Id);
        WakeCoupling();
        if (TenantsEnabled && seat is { OwnerId: null })
        {
            CreateTenant(seat, seat.Slot);
        }
    }
}
