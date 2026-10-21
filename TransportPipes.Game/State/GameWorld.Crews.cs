using System.Security.Cryptography;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>A seat a crew keeps for a friend who hasn't moved in yet; the friend moves in with its code</summary>
public sealed class SeatHold
{
    public required string Code { get; init; }
    public int DistrictX { get; init; }
    public int DistrictY { get; init; }
    public int Slot { get; init; }
    public DateTime HeldAt { get; init; }

    /// <summary>Held for 28 days at most (and only while a member played in the last 14 days)</summary>
    public DateTime Until { get; set; }

    /// <summary>A seat kept for a member (a crew move, or an asleep friend's seat after the crew moved): only they take it</summary>
    public long? For { get; init; }

    public EstateKey Seat => new(new DistrictCoord(DistrictX, DistrictY), Slot);
}

/// <summary>
/// A crew (v1.2 §6): 1–8 companies that chose each other, with a name, an emblem and two colours. No shared wallet, no
/// votes: anyone leaves in one tap, nobody can be removed.
/// </summary>
public sealed class Crew : Entity
{
    public long Id { get; init; }
    public required string Name { get; set; }
    public required string Emblem { get; set; }
    public required string Color { get; set; }
    public required string Color2 { get; set; }
    public long Founder { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>The crew's link for friends without a held seat: for a day anyone with it joins, after that a member has to let them in</summary>
    public required string InviteCode { get; set; }
    public DateTime InviteCodeAt { get; set; }

    /// <summary>Companies that asked to join with an old invite link</summary>
    public HashSet<long> JoinRequests { get; init; } = new();

    /// <summary>Seats kept for friends who haven't moved in yet</summary>
    public List<SeatHold> Holds { get; init; } = new();

    /// <summary>Stations ("x,y,index") where the crew lets strangers settle although it lives in or holds 3+ seats there</summary>
    public HashSet<string> OpenStations { get; init; } = new();

    /// <summary>Asleep members whose seat's hold ends within a week: the keep card every active member sees</summary>
    public HashSet<long> KeepCards { get; init; } = new();

    /// <summary>The consolidation card (v1.2 §4): asked once, and whether its members still see it</summary>
    public bool ConsolidationAsked { get; set; }
    public bool ConsolidationOpen { get; set; }

    /// <summary>Hidden after reports until a moderator looks at it (it shows as "Crew 12")</summary>
    public bool NameHidden { get; set; }
    public HashSet<long> NameReports { get; init; } = new();

    public override string ToString() => $"{Name} (crew #{Id})";
}

/// <summary>How a newcomer wants to live (v1.2 §4): next to people, on a station of its own, or with friends</summary>
public enum Placement
{
    Neighbours,
    OwnStation,
    Friends,
}

public sealed partial class GameWorld
{
    public const int MaxCrewSize = 8;
    public static readonly TimeSpan HoldTime = TimeSpan.FromDays(28);
    public static readonly TimeSpan HoldNeedsActivity = TimeSpan.FromDays(14);
    public static readonly TimeSpan InviteOpenFor = TimeSpan.FromDays(1);
    public static readonly TimeSpan LeaveMoveEvery = TimeSpan.FromDays(30);

    /// <summary>A station where one crew lives in or holds this many seats is closed to strangers (unless the crew opens it)</summary>
    public const int CrewClosesStationAt = 3;

    /// <summary>Districts open when this share of the open ones' seats is lit or held, so groups always find room</summary>
    public const double OpenNextDistrictAt = 0.6;

    private readonly Dictionary<long, Crew> _crews = new();
    private readonly Dictionary<EstateKey, (Crew Crew, SeatHold Hold)> _holds = new();
    public long NextCrewId { get; set; } = 1;

    public IEnumerable<Crew> Crews => _crews.Values;

    public Crew? Crew(long id) => _crews.TryGetValue(id, out var crew) ? crew : null;

    public Crew? CrewOf(Company company) => company.CrewId is { } id ? Crew(id) : null;

    public IEnumerable<Company> CrewMembers(Crew crew) => _companies.Values.Where(t => t.CrewId == crew.Id).OrderBy(t => t.Id);

    public void LoadCrew(Crew crew)
    {
        _crews[crew.Id] = crew;
        NextCrewId = Math.Max(NextCrewId, crew.Id + 1);
        foreach (var hold in crew.Holds)
        {
            _holds[hold.Seat] = (crew, hold);
        }
        crew.Dirty = false;
    }

    /// <summary>The crew a seat is held for (and the hold), when a friend is expected there</summary>
    public (Crew Crew, SeatHold Hold)? HoldOn(EstateKey seat) =>
        _holds.TryGetValue(seat, out var hold) && hold.Hold.Until > LastTick ? hold : null;

    public (Crew Crew, SeatHold Hold)? HoldByCode(string code)
    {
        code = NormalizeCode(code);
        foreach (var (_, (crew, hold)) in _holds)
        {
            if (hold.Code == code && hold.Until > LastTick)
            {
                return (crew, hold);
            }
        }
        return null;
    }

    public Crew? CrewByInvite(string code)
    {
        code = NormalizeCode(code);
        return _crews.Values.FirstOrDefault(t => t.InviteCode == code);
    }

    private static string NormalizeCode(string code) => new(code.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

    /// <summary>A new code nobody uses: 8 letters and digits without the ones that look alike</summary>
    private string NewCode()
    {
        const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        while (true)
        {
            var code = new string(Enumerable.Range(0, 8).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());
            if (HoldByCode(code) == null && CrewByInvite(code) == null)
            {
                return code;
            }
        }
    }

    /// <summary>How a code is shown: two groups of four</summary>
    public static string ShowCode(string code) => code.Length == 8 ? $"{code[..4]}-{code[4..]}" : code;

    private void CrewChanged(Crew crew)
    {
        crew.Touch();
        Changes.Crews.Add(crew.Id);
        foreach (var member in CrewMembers(crew))
        {
            Changes.Companies.Add(member.Id);
        }
    }

    // ---------------------------------------------------------------- Names

    /// <summary>Why a crew name can't be used, or null when it is fine</summary>
    public string? CrewNameError(string name, long? except = null)
    {
        name = name.Trim();
        if (name.Length is < 3 or > 24)
        {
            return "A crew name has 3 to 24 characters.";
        }
        if (!name.All(t => char.IsLetterOrDigit(t) || t is ' ' or '-' or '_' or '.' or '&' or '\''))
        {
            return "Use letters, digits, spaces and - _ . & ' in a crew name.";
        }
        if (!NameFilter.IsAllowed(name))
        {
            return "Choose another name.";
        }
        if (_crews.Values.Any(t => t.Id != except && string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return "That crew name is already taken.";
        }
        return null;
    }

    public static readonly IReadOnlyList<string> CrewEmblems =
    [
        "anchor", "anvil", "bee", "bolt", "bridge", "candle", "cog", "comet", "crown", "drop",
        "fern", "flame", "fox", "gear", "hammer", "heart", "hive", "kettle", "key", "lantern",
        "leaf", "lighthouse", "moon", "mountain", "oak", "owl", "pick", "pipe", "rocket", "rose",
        "sail", "shell", "shield", "star", "sun", "tower", "train", "valve", "wave", "wheel",
    ];

    private static bool IsColor(string color) =>
        color.Length == 7 && color[0] == '#' && color.Skip(1).All(Uri.IsHexDigit);

    // ---------------------------------------------------------------- Founding, joining, leaving

    /// <summary>
    /// Founds a crew (from Level 1): a name, an emblem, two colours. The founder's free neighbours' seats in its station
    /// are held for friends (as many as asked, up to the crew's size), each with its own code.
    /// </summary>
    public (CommandResult Result, Crew? Crew) FoundCrew(long companyId, string name, string emblem, string color, string color2, int holdSeats = 0)
    {
        if (Company(companyId) is not { } company || EstateOf(company) is not { } estate)
        {
            return (CommandResult.Fail("You need an estate first."), null);
        }
        if (company.CrewId != null)
        {
            return (CommandResult.Fail("You are in a crew already: leave it first."), null);
        }
        if (CrewNameError(name) is { } error)
        {
            return (CommandResult.Fail(error), null);
        }
        if (!CrewEmblems.Contains(emblem))
        {
            return (CommandResult.Fail("Pick an emblem."), null);
        }
        if (!IsColor(color) || !IsColor(color2))
        {
            return (CommandResult.Fail("Pick two colours."), null);
        }
        var crew = new Crew
        {
            Id = NextCrewId++,
            Name = name.Trim(),
            Emblem = emblem,
            Color = color,
            Color2 = color2,
            Founder = company.Id,
            CreatedAt = LastTick,
            InviteCode = NewCode(),
            InviteCodeAt = LastTick,
        };
        _crews[crew.Id] = crew;
        company.CrewId = crew.Id;
        company.Touch();
        HoldSeats(crew, estate.Station, Math.Clamp(holdSeats, 0, MaxCrewSize - 1));
        CrewChanged(crew);
        return (CommandResult.Ok, crew);
    }

    /// <summary>Holds up to <paramref name="count"/> free seats of a station (and its joined small stations) for the crew's friends</summary>
    private List<SeatHold> HoldSeats(Crew crew, StationRef station, int count)
    {
        var held = new List<SeatHold>();
        var room = MaxCrewSize - CrewMembers(crew).Count() - crew.Holds.Count;
        foreach (var seat in StationSeats(station))
        {
            if (held.Count >= Math.Min(count, room))
            {
                break;
            }
            if (Estate(seat.District, seat.Slot) is not { } estate || HoldOn(seat) != null)
            {
                continue;
            }
            if (estate.OwnerId is { } owner && Company(owner) is not { IsNpc: true })
            {
                continue;
            }
            var hold = new SeatHold { Code = NewCode(), DistrictX = seat.District.X, DistrictY = seat.District.Y, Slot = seat.Slot, HeldAt = LastTick, Until = LastTick + HoldTime };
            crew.Holds.Add(hold);
            _holds[seat] = (crew, hold);
            held.Add(hold);
            Changes.Estates.Add((seat.District, seat.Slot));
        }
        CrewChanged(crew);
        return held;
    }

    /// <summary>Holds more seats in the crew's station for friends (the crew grows to 8 at most)</summary>
    public CommandResult HoldSeatsFor(long companyId, int count)
    {
        if (Company(companyId) is not { } company || CrewOf(company) is not { } crew || EstateOf(company) is not { } estate)
        {
            return CommandResult.Fail("You are not in a crew.");
        }
        return HoldSeats(crew, estate.Station, count).Count > 0
            ? CommandResult.Ok
            : CommandResult.Fail("There is no free seat left in your station (or your crew is full).");
    }

    /// <summary>Gives a held seat free again</summary>
    public CommandResult ReleaseHold(long companyId, string code)
    {
        if (Company(companyId) is not { } company || CrewOf(company) is not { } crew || crew.Holds.FirstOrDefault(t => t.Code == NormalizeCode(code)) is not { } hold)
        {
            return CommandResult.Fail("No such held seat.");
        }
        RemoveHold(crew, hold);
        return CommandResult.Ok;
    }

    private void RemoveHold(Crew crew, SeatHold hold)
    {
        crew.Holds.Remove(hold);
        _holds.Remove(hold.Seat);
        Changes.Estates.Add((hold.Seat.District, hold.Seat.Slot));
        CrewChanged(crew);
    }

    /// <summary>
    /// Joins a crew with a code: a seat code (the company moves to that seat, for free) or the crew's invite link
    /// (for a day anyone joins with it, after that a member lets them in)
    /// </summary>
    public CommandResult JoinCrew(long companyId, string code)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (HoldByCode(code) is { } held)
        {
            if (company.CrewId is { } current && current != held.Crew.Id)
            {
                return CommandResult.Fail("Leave your crew first.");
            }
            if (CrewMembers(held.Crew).Count() >= MaxCrewSize)
            {
                return CommandResult.Fail("That crew is full.");
            }
            company.CrewId = held.Crew.Id;
            company.Touch();
            var seat = held.Hold.Seat;
            RemoveHold(held.Crew, held.Hold);
            if (EstateOf(company) is not { } estate || KeyOf(estate) != seat)
            {
                company.FreeMoves++;
                var moved = RelocateTo(company, seat);
                if (!moved.Success)
                {
                    company.FreeMoves--;
                    return moved;
                }
            }
            AnnounceArrival(held.Crew, company);
            return CommandResult.Ok;
        }
        if (CrewByInvite(code) is not { } crew)
        {
            return CommandResult.Fail("That code doesn't open any crew or seat.");
        }
        if (company.CrewId == crew.Id)
        {
            return CommandResult.Fail("You are in this crew already.");
        }
        if (company.CrewId != null)
        {
            return CommandResult.Fail("Leave your crew first.");
        }
        if (CrewMembers(crew).Count() + crew.Holds.Count >= MaxCrewSize)
        {
            return CommandResult.Fail("That crew is full.");
        }
        if (LastTick - crew.InviteCodeAt > InviteOpenFor)
        {
            crew.JoinRequests.Add(company.Id);
            CrewChanged(crew);
            foreach (var member in CrewMembers(crew))
            {
                Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Crew, $"{company.Name} would like to join {crew.Name}: let them in from the Crew panel"));
            }
            return CommandResult.Ok;
        }
        AddToCrew(crew, company);
        return CommandResult.Ok;
    }

    private void AddToCrew(Crew crew, Company company)
    {
        company.CrewId = crew.Id;
        crew.JoinRequests.Remove(company.Id);
        company.Touch();
        AnnounceArrival(crew, company);
        CrewChanged(crew);
    }

    /// <summary>A member lets in a company that asked with an old invite link</summary>
    public CommandResult AcceptJoinRequest(long memberId, long applicantId)
    {
        if (Company(memberId) is not { } member || CrewOf(member) is not { } crew || !crew.JoinRequests.Contains(applicantId) || Company(applicantId) is not { } applicant)
        {
            return CommandResult.Fail("No such request.");
        }
        if (applicant.CrewId != null)
        {
            crew.JoinRequests.Remove(applicantId);
            CrewChanged(crew);
            return CommandResult.Fail($"{applicant.Name} joined another crew.");
        }
        if (CrewMembers(crew).Count() + crew.Holds.Count >= MaxCrewSize)
        {
            return CommandResult.Fail("Your crew is full.");
        }
        AddToCrew(crew, applicant);
        return CommandResult.Ok;
    }

    /// <summary>A fresh invite link: for a day anyone with it joins</summary>
    public CommandResult RenewInvite(long memberId)
    {
        if (Company(memberId) is not { } member || CrewOf(member) is not { } crew)
        {
            return CommandResult.Fail("You are not in a crew.");
        }
        crew.InviteCode = NewCode();
        crew.InviteCodeAt = LastTick;
        CrewChanged(crew);
        return CommandResult.Ok;
    }

    /// <summary>Leaves the crew in one tap: no price changes, the estate stays; one extra free move (once a month)</summary>
    public CommandResult LeaveCrew(long companyId)
    {
        if (Company(companyId) is not { } company || CrewOf(company) is not { } crew)
        {
            return CommandResult.Fail("You are not in a crew.");
        }
        company.CrewId = null;
        if (company.LeftCrewAt is not { } left || LastTick - left >= LeaveMoveEvery)
        {
            company.FreeMoves++;
        }
        company.LeftCrewAt = LastTick;
        company.Touch();
        Changes.Companies.Add(company.Id);
        if (!CrewMembers(crew).Any())
        {
            // The last one out: the crew and its held seats are gone
            foreach (var hold in crew.Holds.ToList())
            {
                RemoveHold(crew, hold);
            }
            _crews.Remove(crew.Id);
            Deleted.Add(crew);
            Changes.Crews.Add(crew.Id);
            return CommandResult.Ok;
        }
        CrewChanged(crew);
        return CommandResult.Ok;
    }

    /// <summary>Whether strangers may settle in a station: closed where one crew lives in or holds 3+ seats, unless it opened it</summary>
    public bool StationOpenToStrangers(StationRef station, Company? newcomer = null, Crew? joining = null)
    {
        foreach (var (crew, seats) in CrewSeats(station))
        {
            if (seats >= CrewClosesStationAt && !crew.OpenStations.Contains(StationKey(station)) && newcomer?.CrewId != crew.Id && joining != crew)
            {
                return false;
            }
        }
        return true;
    }

    private static string StationKey(StationRef station) => $"{station.District.X},{station.District.Y},{station.Index}";

    /// <summary>Per crew, how many seats of a station it lives in or holds</summary>
    private IEnumerable<(Crew Crew, int Seats)> CrewSeats(StationRef station)
    {
        var counts = new Dictionary<long, int>();
        foreach (var slot in WorldLayout.StationSlots(station.Index))
        {
            var seat = new EstateKey(station.District, slot);
            var crewId = HoldOn(seat)?.Crew.Id ?? (Estate(station.District, slot)?.OwnerId is { } owner ? Company(owner)?.CrewId : null);
            if (crewId is { } id)
            {
                counts[id] = counts.GetValueOrDefault(id) + 1;
            }
        }
        return counts.Select(t => (Crew(t.Key)!, t.Value)).Where(t => t.Item1 != null);
    }

    /// <summary>
    /// The corner station a small station is joined to (v1.2 §6): a crew that lives in or holds three or more seats of
    /// the corner station and seats of the small one next to it makes them one big station, with one Yard
    /// </summary>
    public StationRef? JoinedTo(StationRef small)
    {
        if (!small.IsSmall)
        {
            return null;
        }
        var here = CrewSeats(small).Select(t => t.Crew.Id).ToHashSet();
        if (here.Count == 0)
        {
            return null;
        }
        foreach (var corner in WorldLayout.CornersNextTo(small.Index).Select(t => new StationRef(small.District, t)))
        {
            if (CrewSeats(corner).Any(t => here.Contains(t.Crew.Id) && t.Seats >= CrewClosesStationAt))
            {
                return corner;
            }
        }
        return null;
    }

    /// <summary>The stations whose residents share a Yard: a station and the small stations joined to it</summary>
    public IReadOnlyList<StationRef> BigStation(StationRef station)
    {
        if (JoinedTo(station) is { } corner)
        {
            station = corner;
        }
        if (station.IsSmall)
        {
            return [station];
        }
        var stations = new List<StationRef> { station };
        foreach (var small in Enumerable.Range(WorldLayout.CornerStations, WorldLayout.StationsPerDistrict - WorldLayout.CornerStations).Select(t => new StationRef(station.District, t)))
        {
            if (JoinedTo(small) == station)
            {
                stations.Add(small);
            }
        }
        return stations;
    }

    /// <summary>A member opens (or closes again) their station to strangers</summary>
    public CommandResult SetStationOpen(long memberId, bool open)
    {
        if (Company(memberId) is not { } member || CrewOf(member) is not { } crew || EstateOf(member) is not { } estate)
        {
            return CommandResult.Fail("You are not in a crew.");
        }
        var key = StationKey(estate.Station);
        if (open)
        {
            crew.OpenStations.Add(key);
        }
        else
        {
            crew.OpenStations.Remove(key);
        }
        CrewChanged(crew);
        return CommandResult.Ok;
    }

    /// <summary>The crew hears that a friend moved in; the next first screen says so</summary>
    private void AnnounceArrival(Crew crew, Company company)
    {
        foreach (var member in CrewMembers(crew).Where(t => t.Id != company.Id))
        {
            member.Report.NewNeighbours.Add(company.Name);
            member.Report.Notes.Add($"{company.Name} moved in next door");
            member.Touch();
            Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Crew, $"{company.Name} moved in next door"));
        }
    }

    /// <summary>Once a day: held seats lapse after 28 days, or when no member played in the last 14 days</summary>
    private void CheckHolds()
    {
        foreach (var crew in _crews.Values.ToList())
        {
            var active = CrewMembers(crew).Any(t => LastTick - t.LastSeenAt < HoldNeedsActivity);
            foreach (var hold in crew.Holds.ToList())
            {
                if (hold.Until <= LastTick || !active)
                {
                    RemoveHold(crew, hold);
                }
            }
        }
    }

    // ---------------------------------------------------------------- Seats and placement

    /// <summary>The seats of a station</summary>
    public static IEnumerable<EstateKey> StationSeats(StationRef station) =>
        WorldLayout.StationSlots(station.Index).Select(t => new EstateKey(station.District, t));

    /// <summary>Whether a newcomer may take this seat: free (or kept warm by a tenant), not held for someone else, not in a closed station</summary>
    public bool SeatFree(EstateKey seat, Company? newcomer = null, string? code = null)
    {
        if (Estate(seat.District, seat.Slot) is not { } estate)
        {
            return false;
        }
        if (estate.OwnerId is { } owner && Company(owner) is not { IsNpc: true })
        {
            return false;
        }
        if (estate.ReservedFor is { } corporation && estate.ReservedUntil > LastTick && corporation != newcomer?.CorporationId)
        {
            return false;
        }
        if (HoldOn(seat) is { } hold)
        {
            return (code != null && hold.Hold.Code == NormalizeCode(code)) || (newcomer != null && hold.Hold.For == newcomer.Id);
        }
        // A crew's link (while anyone may join with it) opens the crew's own stations
        var joining = code != null && CrewByInvite(code) is { } crew && LastTick - crew.InviteCodeAt <= InviteOpenFor ? crew : null;
        return StationOpenToStrangers(estate.Station, newcomer, joining);
    }

    /// <summary>
    /// Where a friend with the crew link settles: a free seat in a station the crew lives in (the one with the most
    /// members first), else the free seat nearest to the crew's Yard
    /// </summary>
    public EstateKey SeatNearCrew(Crew crew, string code)
    {
        var homes = CrewMembers(crew)
            .Select(EstateOf)
            .OfType<Estate>()
            .GroupBy(t => t.Station)
            .OrderByDescending(t => t.Count())
            .Select(t => t.Key)
            .ToList();
        if (homes.Count == 0)
        {
            return SuggestSeat(Placement.Neighbours);
        }
        var home = WorldLayout.Yard(homes[0]);
        var best = _openDistricts
            .Append(homes[0].District)
            .Distinct()
            .SelectMany(WorldLayout.Stations)
            .SelectMany(StationSeats)
            .Where(t => SeatFree(t, null, code))
            .OrderBy(t => homes.Contains(WorldLayout.StationOf(t.District, t.Slot)) ? 0 : 1)
            .ThenBy(t => Distance(WorldLayout.Yard(WorldLayout.StationOf(t.District, t.Slot)), home))
            .ThenBy(t => t.Slot)
            .Select(t => (EstateKey?)t)
            .FirstOrDefault();
        return best ?? SuggestSeat(Placement.Neighbours);

        static int Distance(TileRect a, TileRect b) => Math.Abs(a.X0 + a.X1 - b.X0 - b.X1) + Math.Abs(a.Y0 + a.Y1 - b.Y0 - b.Y1);
    }

    /// <summary>Players who opened the game in the last three days living in a station, with their levels</summary>
    private List<Company> ActiveResidents(StationRef station) =>
        StationSeats(station)
            .Select(t => Estate(t.District, t.Slot)?.OwnerId is { } owner ? Company(owner) : null)
            .OfType<Company>()
            .Where(IsActive)
            .ToList();

    /// <summary>Where a district is in the order districts open (the Capital first)</summary>
    private int DistrictRank(DistrictCoord district)
    {
        var rank = 0;
        foreach (var t in WorldLayout.DistrictOrder())
        {
            if (t == district)
            {
                return rank;
            }
            if (++rank > 10_000)
            {
                break;
            }
        }
        return int.MaxValue;
    }

    /// <summary>
    /// The seat the game suggests (v1.2 §4): next to active players within 10 levels (the most first, then the fullest
    /// station, then the oldest district; never a station of Houses only while one with an active player has room), a
    /// station of one's own (nobody but Houses there), or a station with room for a group of friends. A new district
    /// opens when the open ones are 60% lit or held, or when nothing fits.
    /// </summary>
    public EstateKey SuggestSeat(Placement placement, int level = 1, int seats = 1, Company? newcomer = null)
    {
        OpenRoomForGroups();
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var candidates = new List<(EstateKey Seat, int Near, int Residents, int Free, int Rank, bool Small)>();
            foreach (var district in _openDistricts)
            {
                var rank = DistrictRank(district);
                foreach (var station in WorldLayout.Stations(district))
                {
                    var free = StationSeats(station).Where(t => SeatFree(t, newcomer)).ToList();
                    if (free.Count == 0)
                    {
                        continue;
                    }
                    var residents = StationSeats(station).Count(t => Estate(t.District, t.Slot)?.OwnerId is { } o && Company(o) is { IsNpc: false });
                    var near = ActiveResidents(station).Count(t => Math.Abs(t.Level - level) <= 10);
                    var fits = placement switch
                    {
                        Placement.OwnStation => residents == 0,
                        Placement.Friends => residents == 0 && free.Count >= Math.Min(seats, WorldLayout.StationSlots(station.Index).Count) && station.IsSmall == seats <= 2,
                        _ => true,
                    };
                    if (fits)
                    {
                        candidates.Add((free[0], near, residents, free.Count, rank, station.IsSmall));
                    }
                }
            }
            if (candidates.Count > 0)
            {
                // Next to people: where the most active players of about your level live, in a station of four (more
                // partners than a small one) in the oldest district
                return placement == Placement.Neighbours
                    ? candidates.OrderByDescending(t => t.Near).ThenByDescending(t => t.Residents).ThenBy(t => t.Small).ThenBy(t => t.Rank).ThenBy(t => t.Seat.Slot).First().Seat
                    : candidates.OrderBy(t => t.Rank).ThenBy(t => t.Seat.Slot).First().Seat;
            }
            OpenNextDistrict();
        }
        throw new InvalidOperationException("The world is infinite, this can't happen.");
    }

    /// <summary>A new district opens once the open ones are 60% lit or held, so a group of any size finds room</summary>
    private void OpenRoomForGroups()
    {
        if (_openDistricts.Count == 0)
        {
            OpenNextDistrict();
            return;
        }
        var seats = _openDistricts.Count * WorldLayout.EstatesPerDistrict;
        var taken = _estates.Values.Count(t => _openDistricts.Contains(t.District)
            && (HoldOn(KeyOf(t)) != null || t.OwnerId is { } owner && Company(owner) is { IsNpc: false } c && IsActive(c)));
        if (taken >= seats * OpenNextDistrictAt)
        {
            OpenNextDistrict();
        }
    }

    /// <summary>
    /// Moves a company's estate to a seat (a free one, or one its crew holds for it): balloons lift it, a moment later it
    /// lands. Free and quick (content: together), with a few minutes between moves.
    /// </summary>
    public CommandResult RelocateTo(long companyId, EstateKey seat) =>
        Company(companyId) is { } company ? RelocateTo(company, seat) : CommandResult.Fail("Unknown company.");

    private CommandResult RelocateTo(Company company, EstateKey seat, string? code = null)
    {
        if (company.Packed != null)
        {
            return CommandResult.Fail("Your estate is on its way already.");
        }
        if (EstateOf(company) is { } current && KeyOf(current) == seat)
        {
            return CommandResult.Fail("You live there already.");
        }
        if (company.LastMovedAt is { } last && last + MoveEvery > LastTick && company.FreeMoves == 0)
        {
            return CommandResult.Fail($"You can move again in {Math.Ceiling((last + MoveEvery - LastTick).TotalMinutes):N0} minutes.");
        }
        OpenDistrict(seat.District);
        var heldForMe = HoldOn(seat) is { } hold && hold.Crew.Id == company.CrewId;
        if (!heldForMe && !SeatFree(seat, company, code))
        {
            return CommandResult.Fail("That seat isn't free.");
        }
        var cost = MoveCost(company);
        if (company.Gold < cost)
        {
            return CommandResult.Fail($"A move costs {cost:N0} gold.");
        }
        if (heldForMe)
        {
            RemoveHold(Crew(company.CrewId!.Value)!, HoldOn(seat)!.Value.Hold);
        }
        if (Estate(seat.District, seat.Slot) is { OwnerId: { } tenant } && Company(tenant) is { IsNpc: true })
        {
            EvictTenant(Estate(seat.District, seat.Slot)!);
        }
        if (company.FreeMoves > 0)
        {
            company.FreeMoves--;
        }
        else
        {
            company.Gold -= cost;
            company.Report.GoldSpent += cost;
        }
        EndTradeOf(company);
        Pack(company);
        var target = Estate(seat.District, seat.Slot)!;
        // The new spot is kept for the company while it travels
        target.OwnerId = company.Id;
        target.Touch();
        Changes.Estates.Add((target.District, target.Slot));
        company.District = target.District;
        company.ArrivesAt = LastTick + MoveTime;
        company.LastMovedAt = LastTick;
        _arrivals[company.Id] = target.Slot;
        return CommandResult.Ok;
    }

    /// <summary>The day a company last played (counted in days since the world began) and on how many different days it played</summary>
    public void CountPlayDay(Company company)
    {
        var day = (int)Math.Floor((Now - DateTime.UnixEpoch).TotalDays);
        if (company.LastPlayedDay != day)
        {
            company.LastPlayedDay = day;
            company.DistinctDays++;
            company.Touch();
        }
    }

    /// <summary>A verified company (v1.2 §3): an account (no guest) and three days of play</summary>
    public static bool IsVerified(Company company) => !company.IsGuest && company.DistinctDays >= 3;
}

/// <summary>Names nobody should see on the map (crews, companies): a short list of words, checked inside the name too</summary>
public static class NameFilter
{
    private static readonly string[] Blocked =
    [
        "fuck", "shit", "cunt", "nigg", "faggot", "nazi", "hitler", "whore", "slut", "pussy", "bitch", "asshole",
        "kanker", "tering", "tyfus", "retard", "porn",
    ];

    public static bool IsAllowed(string name)
    {
        var plain = new string(name.ToLowerInvariant()
            .Replace('0', 'o').Replace('1', 'i').Replace('3', 'e').Replace('4', 'a').Replace('5', 's').Replace('$', 's')
            .Where(char.IsLetter).ToArray());
        return !Blocked.Any(plain.Contains);
    }
}
