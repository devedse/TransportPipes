using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// Absence and the way back (v1.2 §10). An asleep crew member's estate stays in place and its seat is held to day 90;
/// seven days before, every active crew member gets one card ("Keep Ben's seat 60 more days?", the first answer counts,
/// once). Without a crew the seat is held to day 37. Nobody can release a hold early; when it ends the estate travels
/// safely to the Quiet Isles and a House warms the seat. Friends tend each other's estates (a one-tap favour that banks
/// for three days and shows when they come back, never who), and Tailwind helps whoever is far behind the people they
/// live with.
/// </summary>
public sealed partial class GameWorld
{
    public static readonly TimeSpan CrewHold = TimeSpan.FromDays(90);
    public static readonly TimeSpan LoneHold = TimeSpan.FromDays(37);
    public static readonly TimeSpan KeepCardBefore = TimeSpan.FromDays(7);
    public static readonly TimeSpan KeepExtends = TimeSpan.FromDays(60);
    public const int TendBanksDays = 3;

    // ---------------------------------------------------------------- Holds for those who sleep

    /// <summary>Whether a company's crew has a member who played in the last two weeks (else its holds lapse)</summary>
    private bool CrewAwake(Company company) =>
        CrewOf(company) is { } crew && CrewMembers(crew).Any(t => t.Id != company.Id && !t.Asleep && LastTick - t.LastSeenAt < HoldNeedsActivity);

    /// <summary>When an asleep company's seat stops being held: day 90 in a crew (60 days later when a friend kept it), else day 37</summary>
    public DateTime HoldEnds(Company company) =>
        company.HoldUntil ?? company.LastSeenAt + (CrewAwake(company) ? CrewHold : LoneHold);

    /// <summary>Once a day: the keep card a week before a crew member's hold ends, and the holds that end</summary>
    private void CheckAsleepHolds()
    {
        foreach (var company in _companies.Values.Where(t => t.Asleep && t.Packed == null).ToList())
        {
            var ends = HoldEnds(company);
            if (LastTick >= ends)
            {
                PackAsleep(company);
                continue;
            }
            if (!company.KeepAsked && CrewAwake(company) && LastTick >= ends - KeepCardBefore && CrewOf(company) is { } crew)
            {
                company.KeepAsked = true;
                company.Touch();
                crew.KeepCards.Add(company.Id);
                CrewChanged(crew);
                foreach (var member in CrewMembers(crew).Where(t => t.Id != company.Id && IsActive(t)))
                {
                    Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Crew,
                        $"Keep {company.Name}'s seat 60 more days? Answer in the Crew panel: without an answer it travels safely to the Quiet Isles in a week"));
                }
            }
        }
    }

    /// <summary>The keep card's answer (the first one counts, once): keep the sleeper's seat 60 more days, or let it go in a week</summary>
    public CommandResult KeepSeat(long companyId, long sleeperId, bool keep)
    {
        if (Company(companyId) is not { } company || Company(sleeperId) is not { Asleep: true } sleeper || CrewOf(company) is not { } crew
            || sleeper.CrewId != crew.Id || !crew.KeepCards.Contains(sleeperId))
        {
            return CommandResult.Fail("That card is gone.");
        }
        crew.KeepCards.Remove(sleeperId);
        CrewChanged(crew);
        if (keep)
        {
            sleeper.HoldUntil = HoldEnds(sleeper) + KeepExtends;
            sleeper.Touch();
            foreach (var member in CrewMembers(crew).Where(t => t.Id != companyId && t.Id != sleeperId))
            {
                Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Crew, $"{company.Name} kept {sleeper.Name}'s seat for 60 more days"));
            }
        }
        return CommandResult.Ok;
    }

    // ---------------------------------------------------------------- Landing

    /// <summary>A place a returner can land (v1.2 §10): its seat, how many buildings wouldn't fit there, and whether it's where the estate is now</summary>
    public sealed record LandingCard(string Kind, EstateKey Seat, int Crates, bool Here);

    /// <summary>Where a packed returner lands: its old seat when it is still free, else next to its crew, else next to people</summary>
    private EstateKey PreferredLanding(Company company)
    {
        if (company.LastSeat is { } old && Estate(old.District, old.Slot) != null && SeatFree(old, company))
        {
            return old;
        }
        if (CrewOf(company) is { } crew && CrewMembers(crew).Any(t => t.Id != company.Id && EstateOf(t) != null))
        {
            return SeatNearCrew(crew, null);
        }
        return SuggestSeat(Placement.Neighbours, company.Level, 1, company);
    }

    /// <summary>How many of a company's buildings wouldn't fit another seat's land and would wait in crates</summary>
    public int CratesIfMovedTo(Company company, Estate target)
    {
        if (EstateOf(company) is not { } home)
        {
            return company.Crates.Count;
        }
        var crates = company.Crates.Count;
        foreach (var building in BuildingsOf(company.Id).Where(t => home.Contains(t.Position)))
        {
            var definition = Content.Building(building.Type);
            if (definition.Kind is BuildingKind.Hq or BuildingKind.TradingPost)
            {
                continue;
            }
            var pos = target.Origin.Offset(building.Position.X - home.Origin.X, building.Position.Y - home.Origin.Y);
            if (!FitsTheLand(definition, pos))
            {
                crates++;
            }
        }
        return crates;
    }

    /// <summary>Whether a building's footprint suits the land there (terrain, deposit, shore, forest), whoever owns it</summary>
    private bool FitsTheLand(BuildingDefinition definition, TilePos pos)
    {
        var footprint = Enumerable.Range(0, definition.Size).SelectMany(dy => Enumerable.Range(0, definition.Size).Select(dx => pos.Offset(dx, dy))).ToList();
        if (footprint.Any(t => WorldLayout.ZoneAt(t.X, t.Y).Kind == ZoneKind.Yard || !definition.Placement.Terrain.Contains(Tile(t).Terrain)))
        {
            return false;
        }
        if (definition.Placement.Deposit is { } deposit
            && footprint.Select(Tile).Where(t => DepositMatches(deposit, t.Deposit)).Select(t => t.Richness).DefaultIfEmpty(0).Max() < Math.Max(1, definition.Placement.MinRichness))
        {
            return false;
        }
        if (definition.Placement.Shore && !BorderTiles(pos, definition.Size).Any(t => Tile(t).Terrain == TerrainType.Water))
        {
            return false;
        }
        return !definition.Placement.Forest || ForestAround(pos, definition.Size) >= 3;
    }

    /// <summary>
    /// The landing cards for a returner (v1.2 §10): its own seat (when it is still free), next to its crew, a lively
    /// station and the Frontier (the newest district), each with how many buildings would wait in crates there
    /// </summary>
    public List<LandingCard> LandingCards(Company company)
    {
        var here = EstateOf(company) is { } estate ? KeyOf(estate) : (EstateKey?)null;
        var cards = new List<LandingCard>();
        void Card(string kind, EstateKey? seat)
        {
            if (seat is not { } s || Estate(s.District, s.Slot) is not { } target || cards.Any(t => t.Seat == s))
            {
                return;
            }
            if (s != here && !SeatFree(s, company))
            {
                return;
            }
            cards.Add(new LandingCard(kind, s, s == here ? company.Crates.Count : CratesIfMovedTo(company, target), s == here));
        }
        Card("yours", company.LastSeat is { } last ? last : here);
        if (CrewOf(company) is { } crew && CrewMembers(crew).Any(t => t.Id != company.Id && EstateOf(t) != null))
        {
            Card("crew", SeatNearCrew(crew, null));
        }
        // Only where people play (never an empty station called lively)
        Card("lively", LivelierStations(company, 1).Select(t => (EstateKey?)t.Seat).FirstOrDefault());
        var frontier = _openDistricts.OrderByDescending(DistrictRank).FirstOrDefault();
        Card("frontier", _estates.Values.Where(t => t.District == frontier && SeatFree(KeyOf(t), company)).Select(KeyOf).OrderBy(t => t.Slot).Select(t => (EstateKey?)t).FirstOrDefault());
        return cards;
    }

    // ---------------------------------------------------------------- Lonely, together, on probation

    public const int LoneCardAfterDays = 5;
    public static readonly TimeSpan LoneCardEvery = TimeSpan.FromDays(30);
    public static readonly TimeSpan CrewMoveHold = TimeSpan.FromDays(7);
    public static readonly TimeSpan ProbationTime = TimeSpan.FromDays(14);
    public static readonly TimeSpan ProbationAway = TimeSpan.FromDays(5);

    /// <summary>The players of a company's (big) station who played in the last three days, the company aside</summary>
    private List<Company> ActiveNeighbours(Company company) =>
        EstateOf(company) is { } estate
            ? BigStation(estate.Station).SelectMany(StationSeats)
                .Select(t => Estate(t.District, t.Slot)?.OwnerId is { } o ? Company(o) : null)
                .OfType<Company>()
                .Where(t => t.Id != company.Id && !t.IsNpc && !t.Asleep && IsActive(t))
                .ToList()
            : [];

    /// <summary>
    /// Once a day: whoever is the only active player in their station counts the day; on the fifth (at most once every 30
    /// days) the lone-human card offers livelier stations. And the consolidation card, once, for a crew of which more
    /// than half sleeps while two or more play.
    /// </summary>
    private void CheckTogether()
    {
        foreach (var company in _companies.Values.Where(t => !t.IsNpc && !t.Asleep && t.Packed == null && IsActive(t)).ToList())
        {
            if (ActiveNeighbours(company).Count > 0)
            {
                continue;
            }
            company.LoneDays++;
            company.Touch();
            if (company.LoneDays >= LoneCardAfterDays && (company.LoneCardAt is not { } last || LastTick - last >= LoneCardEvery) && LivelierStations(company).Count > 0)
            {
                company.LoneDays = 0;
                company.LoneCardAt = LastTick;
                company.LoneCard = true;
                Changes.Companies.Add(company.Id);
                Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.Neighbour,
                    "Nobody else here has played lately: there are livelier stations, and moving is free"));
            }
        }
        foreach (var crew in _crews.Values.Where(t => !t.ConsolidationAsked).ToList())
        {
            var members = CrewMembers(crew).ToList();
            var awake = members.Count(t => !t.Asleep && IsActive(t));
            if (awake >= 2 && members.Count(t => t.Asleep) * 2 > members.Count)
            {
                crew.ConsolidationAsked = true;
                crew.ConsolidationOpen = true;
                CrewChanged(crew);
                foreach (var member in members.Where(t => !t.Asleep && IsActive(t)))
                {
                    Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Crew,
                        $"Most of {crew.Name} is asleep: move to a fresh station together? (Crew panel)"));
                }
            }
        }
        foreach (var company in _companies.Values.Where(t => t.ProbationUntil is { } until && until > LastTick && !t.Asleep && t.Packed == null
                     && LastTick - t.LastSeenAt >= ProbationAway).ToList())
        {
            EndProbation(company);
        }
    }

    /// <summary>Up to three stations where active players of about your level live and a seat is free (the lone-human card)</summary>
    public List<(EstateKey Seat, StationRef Station, int Active)> LivelierStations(Company company, int count = 3)
    {
        var home = EstateOf(company) is { } estate ? BigStation(estate.Station)[0] : (StationRef?)null;
        var options = new List<(EstateKey, StationRef, int, int)>();
        foreach (var station in _openDistricts.SelectMany(WorldLayout.Stations))
        {
            if (BigStation(station)[0] == home)
            {
                continue;
            }
            var active = ActiveResidents(station).Where(t => !t.IsNpc && t.Id != company.Id).ToList();
            if (active.Count == 0 || StationSeats(station).Where(t => Estate(t.District, t.Slot) != null && SeatFree(t, company)).Select(t => (EstateKey?)t).FirstOrDefault() is not { } seat)
            {
                continue;
            }
            options.Add((seat, station, active.Count, active.Min(t => Math.Abs(t.Level - company.Level))));
        }
        return options.OrderByDescending(t => t.Item3).ThenBy(t => t.Item4).Take(count).Select(t => (t.Item1, t.Item2, t.Item3)).ToList();
    }

    public CommandResult DismissLoneCard(long companyId)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        company.LoneCard = false;
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>
    /// A crew moves as a block (v1.2 §4): the first member's tap moves it to a seat and keeps a seat of that station for
    /// every other member for 7 days; each moves with their own tap
    /// </summary>
    public CommandResult CrewMove(long companyId, EstateKey seat)
    {
        if (Company(companyId) is not { } company || CrewOf(company) is not { } crew)
        {
            return CommandResult.Fail("You are not in a crew.");
        }
        var station = WorldLayout.StationOf(seat.District, seat.Slot);
        var others = CrewMembers(crew).Where(t => t.Id != company.Id && !t.Asleep).ToList();
        var free = CrewRoom(station).Where(t => t != seat && Estate(t.District, t.Slot) != null && SeatFree(t, company)).ToList();
        if (free.Count < others.Count)
        {
            return CommandResult.Fail($"That station has no room for all of {crew.Name}.");
        }
        var moved = RelocateTo(company, seat);
        if (!moved.Success)
        {
            return moved;
        }
        foreach (var (member, place) in others.Zip(free))
        {
            HoldSeatFor(crew, place, member.Id, LastTick + CrewMoveHold);
            Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Crew,
                $"{company.Name} moves {crew.Name} to {WorldNames.Station(station)}: a seat there is kept for you for a week (Crew panel)"));
        }
        crew.ConsolidationOpen = false;
        CrewChanged(crew);
        return CommandResult.Ok;
    }

    /// <summary>Where a crew could move together: a station with a seat for every member</summary>
    public EstateKey CrewMoveSeat(Company company) =>
        SuggestSeat(Placement.Friends, company.Level, CrewOf(company) is { } crew ? CrewMembers(crew).Count() : 1, company);

    /// <summary>
    /// The consolidation card's answer: the awake members move together to a fresh station; the asleep friends' estates
    /// travel to the Quiet Isles and their held seats point to the new station, so "Your seat" on their return is next to
    /// their friends
    /// </summary>
    public CommandResult Consolidate(long companyId)
    {
        if (Company(companyId) is not { } company || CrewOf(company) is not { } crew)
        {
            return CommandResult.Fail("You are not in a crew.");
        }
        var seat = CrewMoveSeat(company);
        var moved = CrewMove(companyId, seat);
        if (!moved.Success)
        {
            return moved;
        }
        var station = WorldLayout.StationOf(seat.District, seat.Slot);
        foreach (var sleeper in CrewMembers(crew).Where(t => t.Asleep).ToList())
        {
            var until = HoldEnds(sleeper);
            if (sleeper.Packed == null)
            {
                PackAsleep(sleeper);
            }
            if (CrewRoom(station).Where(t => Estate(t.District, t.Slot) != null && SeatFree(t, company)).Select(t => (EstateKey?)t).FirstOrDefault() is { } place)
            {
                HoldSeatFor(crew, place, sleeper.Id, until > LastTick ? until : LastTick + LoneHold);
                sleeper.LastSeat = place;
                sleeper.Touch();
            }
        }
        return CommandResult.Ok;
    }

    public CommandResult DismissConsolidation(long companyId)
    {
        if (Company(companyId) is not { } company || CrewOf(company) is not { } crew)
        {
            return CommandResult.Fail("You are not in a crew.");
        }
        crew.ConsolidationOpen = false;
        CrewChanged(crew);
        return CommandResult.Ok;
    }

    /// <summary>The seats a crew can live in around a station: a corner station and the small ones next to it (one big station for a crew)</summary>
    private IEnumerable<EstateKey> CrewRoom(StationRef station)
    {
        var corner = BigStation(station)[0];
        var stations = corner.IsSmall
            ? [corner]
            : WorldLayout.Stations(corner.District).Where(t => t == corner || (t.IsSmall && WorldLayout.CornersNextTo(t.Index).Contains(corner.Index)));
        return stations.SelectMany(StationSeats);
    }

    /// <summary>A seat kept for one member of the crew</summary>
    private void HoldSeatFor(Crew crew, EstateKey seat, long member, DateTime until)
    {
        var hold = new SeatHold { Code = NewCode(), DistrictX = seat.District.X, DistrictY = seat.District.Y, Slot = seat.Slot, HeldAt = LastTick, Until = until, For = member };
        crew.Holds.Add(hold);
        _holds[seat] = (crew, hold);
        Changes.Estates.Add((seat.District, seat.Slot));
        CrewChanged(crew);
    }

    /// <summary>A stranger who settles in a station a crew fills (two or more of its seats) is on probation for 14 days</summary>
    private void StartProbation(Company company)
    {
        if (EstateOf(company) is { } estate && CrewSeats(estate.Station).Any(t => t.Seats >= 2 && t.Crew.Id != company.CrewId))
        {
            company.ProbationUntil = LastTick + ProbationTime;
        }
    }

    /// <summary>Away five days in a row within the first 14 in a crew's station: packed to the Quiet Isles, nothing lost; the old seat is the first landing card</summary>
    private void EndProbation(Company company)
    {
        company.ProbationUntil = null;
        company.LastSeat = EstateOf(company) is { } estate ? KeyOf(estate) : null;
        EndTradeOf(company);
        Pack(company);
        company.Asleep = true;
        company.Touch();
        Changes.Companies.Add(company.Id);
        WakeCoupling();
    }

    // ---------------------------------------------------------------- Tending

    /// <summary>Whether a crew mate may tend a friend's estate now (once a day each, only friends who are away)</summary>
    /// <summary>A crew mate who is away can be tended when the tend does a chore: their storm-damaged buildings (F9)</summary>
    public bool CanTend(Company tender, Company friend) =>
        tender.Id != friend.Id && tender.CrewId != null && tender.CrewId == friend.CrewId && EstateOf(friend) != null
        && LastTick - friend.LastSeenAt >= TimeSpan.FromDays(1) && friend.TendedBy.GetValueOrDefault(tender.Id) != DayNumber
        && BuildingsOf(friend.Id).Any(t => t.Damaged);

    /// <summary>What a tend would repair of a friend's estate: its storm-damaged buildings</summary>
    public IReadOnlyList<Building> TendRepairs(Company friend) => BuildingsOf(friend.Id).Where(t => t.Damaged).ToList();

    private int DayNumber => (int)(LastTick - DateTime.UnixEpoch).TotalDays;

    /// <summary>
    /// Tends a friend's estate (v1.2 §12, F9): a one-tap chore while they're away, the repair of their storm damage
    /// (their estate pays it, as it would); when they come back they read on how many days their crew tended it, never
    /// who. It carries no stat bonus and waives no fee, so it can't be farmed.
    /// </summary>
    public CommandResult Tend(long companyId, long friendId)
    {
        if (Company(companyId) is not { } tender || Company(friendId) is not { } friend || !CanTend(tender, friend))
        {
            // Run 4: Cleo's card said "the storm damaged Ben's Moily", and Ana had tended it two minutes before (never who)
            return CommandResult.Fail(Company(friendId) is { } tended && tended.TendedBy.Any(t => t.Key != companyId && t.Value == DayNumber) && !BuildingsOf(tended.Id).Any(t => t.Damaged)
                ? "A crew mate tended it already today 💛"
                : "Nothing to tend: a crew mate's estate needs it once a storm damaged it while they're away.");
        }
        foreach (var building in TendRepairs(friend))
        {
            Repair(friend.Id, building.Id);
        }
        friend.TendedBy[tender.Id] = DayNumber;
        var until = LastTick.Date.AddDays(TendBanksDays);
        // Every day it covers counts once, however many friends tend it
        var from = friend.TendedUntil is { } covered && covered.Date > LastTick.Date ? covered.Date : LastTick.Date;
        for (var day = from; day < until; day = day.AddDays(1))
        {
            friend.Report.TendedDays++;
        }
        if (friend.TendedUntil is not { } banked || banked < until)
        {
            friend.TendedUntil = until;
        }
        friend.Touch();
        Changes.Companies.Add(friend.Id);
        return CommandResult.Ok;
    }

    /// <summary>Whether a friend tended this estate lately (it looks cared for)</summary>
    public bool Tended(Company company) => company.TendedUntil is { } until && until > LastTick;

    // ---------------------------------------------------------------- Tailwind

    /// <summary>
    /// Tailwind (v1.2 §10): XP × min(2.5, 1 + (the active median level of your crew, else your station, else your
    /// district − your level) ÷ 10) while you are more than 5 levels below that median. For returners, late friends and
    /// newcomers alike.
    /// </summary>
    public double TailwindFor(Company company)
    {
        if (company.IsNpc)
        {
            return 1;
        }
        List<int> Levels(IEnumerable<Company> people) =>
            people.Where(t => t.Id != company.Id && !t.IsNpc && !t.Asleep && IsActive(t)).Select(t => t.Level).Order().ToList();
        var crew = CrewOf(company) is { } c ? Levels(CrewMembers(c)) : [];
        var station = EstateOf(company) is { } estate
            ? Levels(BigStation(estate.Station).SelectMany(StationSeats).Select(t => Estate(t.District, t.Slot)?.OwnerId is { } o ? Company(o) : null).OfType<Company>())
            : [];
        var levels = crew.Count > 0 ? crew : station.Count > 0 ? station : Levels(_companies.Values.Where(t => t.District == company.District));
        if (levels.Count == 0)
        {
            return 1;
        }
        var median = levels[levels.Count / 2];
        var behind = median - company.Level;
        return behind > 5 ? Math.Min(2.5, 1 + behind / 10.0) : 1;
    }

    /// <summary>Every quarter of an hour: each company's Tailwind</summary>
    private void UpdateTailwinds()
    {
        foreach (var company in _companies.Values.Where(t => !t.IsNpc))
        {
            var tailwind = TailwindFor(company);
            if (Math.Abs(tailwind - company.Tailwind) > 1e-9)
            {
                company.Tailwind = tailwind;
                Changes.Companies.Add(company.Id);
            }
        }
    }
}
