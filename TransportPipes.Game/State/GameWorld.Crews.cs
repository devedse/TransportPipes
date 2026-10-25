using System.Security.Cryptography;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>A request to settle in a closed Yard (1.4 §7): it waits three days for a resident's yes</summary>
public sealed class YardRequest
{
    public long Company { get; init; }

    /// <summary>The plot asked for (when it's taken by the time a resident says yes, another free plot of the Yard)</summary>
    public int Slot { get; init; }

    public DateTime At { get; init; }
}

/// <summary>
/// A Yard's crew (1.4 §7): the one to four people who live around a Yard. Moving in makes you a member, moving away
/// ends it; there are no codes or invites to manage. The first resident names the Yard, picks its banner and chooses
/// open (anyone may settle on a free plot) or closed (a newcomer asks, and a resident says yes); open is the default.
/// Every resident is an owner, and nobody can be thrown out.
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

    /// <summary>The Yard it lives around: its town and its index there</summary>
    public int DistrictX { get; init; }
    public int DistrictY { get; init; }
    public int Station { get; init; }

    /// <summary>Open: anyone may settle on a free plot; closed: a newcomer asks first</summary>
    public bool Open { get; set; } = true;

    /// <summary>Whether a resident named the Yard yet (until then it carries the name the game gave it)</summary>
    public bool Named { get; set; }

    /// <summary>Newcomers who asked to settle while the Yard is closed</summary>
    public List<YardRequest> Requests { get; init; } = new();

    /// <summary>Hidden after reports until a moderator looks at it (it shows as "Crew 12")</summary>
    public bool NameHidden { get; set; }
    public HashSet<long> NameReports { get; init; } = new();

    public StationRef Yard => new(new DistrictCoord(DistrictX, DistrictY), Station);

    public override string ToString() => $"{Name} (crew #{Id})";
}

public sealed partial class GameWorld
{
    /// <summary>A request to a closed Yard waits this long for a yes</summary>
    public static readonly TimeSpan RequestWaits = TimeSpan.FromDays(3);

    /// <summary>A "Play next to me" link works this long</summary>
    public static readonly TimeSpan NextToMeFor = TimeSpan.FromDays(7);

    /// <summary>A new town opens when every open town has fewer free plots than this</summary>
    public const int OpenNextTownBelow = 2;

    private readonly Dictionary<long, Crew> _crews = new();
    private readonly Dictionary<StationRef, Crew> _crewAt = new();
    public long NextCrewId { get; set; } = 1;

    public IEnumerable<Crew> Crews => _crews.Values;

    public Crew? Crew(long id) => _crews.TryGetValue(id, out var crew) ? crew : null;

    public Crew? CrewOf(Company company) => company.CrewId is { } id ? Crew(id) : null;

    /// <summary>The crew of a Yard (null: nobody lives there)</summary>
    public Crew? CrewAt(StationRef yard) => _crewAt.GetValueOrDefault(yard);

    public IEnumerable<Company> CrewMembers(Crew crew) => _companies.Values.Where(t => t.CrewId == crew.Id).OrderBy(t => t.Id);

    public void LoadCrew(Crew crew)
    {
        _crews[crew.Id] = crew;
        _crewAt[crew.Yard] = crew;
        NextCrewId = Math.Max(NextCrewId, crew.Id + 1);
        crew.Dirty = false;
    }

    private void CrewChanged(Crew crew)
    {
        crew.Touch();
        Changes.Crews.Add(crew.Id);
        foreach (var member in CrewMembers(crew))
        {
            Changes.Companies.Add(member.Id);
        }
    }

    /// <summary>Whether a Yard lets anyone settle (an empty Yard is open)</summary>
    public bool YardOpen(StationRef yard) => CrewAt(yard)?.Open ?? true;

    // ---------------------------------------------------------------- Names

    /// <summary>Why a crew name can't be used, or null when it is fine</summary>
    public string? CrewNameError(string name, long? except = null)
    {
        name = name.Trim();
        if (name.Length is < 3 or > 24)
        {
            return "A Yard's name has 3 to 24 characters.";
        }
        if (!name.All(t => char.IsLetterOrDigit(t) || t is ' ' or '-' or '_' or '.' or '&' or '\''))
        {
            return "Use letters, digits, spaces and - _ . & ' in a Yard's name.";
        }
        if (!NameFilter.IsAllowed(name))
        {
            return "Choose another name.";
        }
        if (_crews.Values.Any(t => t.Id != except && string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return "That name is already taken.";
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

    /// <summary>A company as others see it</summary>
    public string NameFor(Company company, Company viewer) => company.Name;

    /// <summary>Whose something is, as a line says it: "ana_pm Pipes'" for a name ending in s, else "Jo's Works's"</summary>
    public string PossessiveFor(Company company, Company viewer) =>
        company.Name.EndsWith('s') ? $"{company.Name}'" : $"{company.Name}'s";

    /// <summary>A neighbour looks at your plot: your next card says so (after run 3, row 12: six promised visits, none seen)</summary>
    public void RecordVisit(Company owner, Company visitor)
    {
        if (owner.Id == visitor.Id || owner.CrewId == null || owner.CrewId != visitor.CrewId)
        {
            return;
        }
        if (!owner.Report.VisitedBy.Contains(visitor.Name))
        {
            owner.Report.VisitedBy.Add(visitor.Name);
            owner.Touch();
        }
    }

    // ---------------------------------------------------------------- Living around a Yard

    /// <summary>
    /// A company settles on a plot: it joins the Yard's crew, or founds it as the Yard's first resident (with the name
    /// the game gives the Yard until a resident names it, open)
    /// </summary>
    private void SettleInYard(Company company, Estate estate)
    {
        var yard = estate.Station;
        if (CrewAt(yard) is { } crew)
        {
            company.CrewId = crew.Id;
            crew.Requests.RemoveAll(t => t.Company == company.Id);
            company.Touch();
            AnnounceArrival(crew, company);
            CrewChanged(crew);
            return;
        }
        crew = new Crew
        {
            Id = NextCrewId++,
            Name = FreeYardName(yard),
            Emblem = "cog",
            Color = IsColor(company.Color) ? company.Color : "#2f7d6d",
            Color2 = "#f4ead3",
            Founder = company.Id,
            CreatedAt = LastTick,
            DistrictX = yard.District.X,
            DistrictY = yard.District.Y,
            Station = yard.Index,
        };
        _crews[crew.Id] = crew;
        _crewAt[yard] = crew;
        company.CrewId = crew.Id;
        company.Touch();
        CrewChanged(crew);
    }

    /// <summary>The name the game gives a Yard until a resident names it: its town and where it lies ("Capital NW")</summary>
    private string FreeYardName(StationRef yard)
    {
        var name = WorldNames.Station(yard);
        for (var n = 2; CrewNameError(name) != null; n++)
        {
            name = $"{WorldNames.Station(yard)} {n}";
        }
        return name;
    }

    /// <summary>
    /// A company leaves its Yard (it moves away): its crew membership ends. The last one out takes the crew with it, and
    /// the Yard's own things go (they belonged to the crew); a Yard of people who stopped playing stays as it is.
    /// </summary>
    private void LeaveYard(Company company)
    {
        if (CrewOf(company) is not { } crew)
        {
            return;
        }
        company.CrewId = null;
        company.Touch();
        Changes.Companies.Add(company.Id);
        if (!CrewMembers(crew).Any())
        {
            _crews.Remove(crew.Id);
            _crewAt.Remove(crew.Yard);
            Deleted.Add(crew);
            Changes.Crews.Add(crew.Id);
            return;
        }
        foreach (var member in CrewMembers(crew))
        {
            Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Crew, $"{company.Name} moved away from {crew.Name}"));
        }
        CrewChanged(crew);
    }

    /// <summary>Whether a company would be the last one out of its Yard if it moved (the move asks first)</summary>
    public bool LastInYard(Company company) => CrewOf(company) is { } crew && CrewMembers(crew).All(t => t.Id == company.Id);

    /// <summary>Any resident names the Yard and picks its banner</summary>
    public CommandResult SetYard(long companyId, string name, string emblem, string color, string color2)
    {
        if (Company(companyId) is not { } company || CrewOf(company) is not { } crew)
        {
            return CommandResult.Fail("You don't live at a Yard yet.");
        }
        if (CrewNameError(name, crew.Id) is { } error)
        {
            return CommandResult.Fail(error);
        }
        if (!CrewEmblems.Contains(emblem))
        {
            return CommandResult.Fail("Pick an emblem.");
        }
        if (!IsColor(color) || !IsColor(color2))
        {
            return CommandResult.Fail("Pick two colours.");
        }
        crew.Name = name.Trim();
        crew.Emblem = emblem;
        crew.Color = color;
        crew.Color2 = color2;
        crew.Named = true;
        crew.NameHidden = false;
        crew.NameReports.Clear();
        CrewChanged(crew);
        return CommandResult.Ok;
    }

    /// <summary>Any resident opens or closes the Yard; a closed Yard stays closed, also when everyone in it stopped playing</summary>
    public CommandResult SetYardOpen(long companyId, bool open)
    {
        if (Company(companyId) is not { } company || CrewOf(company) is not { } crew)
        {
            return CommandResult.Fail("You don't live at a Yard yet.");
        }
        crew.Open = open;
        CrewChanged(crew);
        foreach (var member in CrewMembers(crew).Where(t => t.Id != company.Id))
        {
            Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Crew, open ? $"{company.Name} opened {crew.Name}: anyone may settle on its free plots" : $"{company.Name} closed {crew.Name}: newcomers ask first"));
        }
        return CommandResult.Ok;
    }

    // ---------------------------------------------------------------- Requests to closed Yards

    /// <summary>
    /// Asks to settle on a free plot of a closed Yard: the request waits three days; the company plays where it is
    /// meanwhile and moves in for free when a resident says yes
    /// </summary>
    public CommandResult RequestPlot(long companyId, EstateKey seat)
    {
        if (Company(companyId) is not { } company || Estate(seat.District, seat.Slot) is not { } estate)
        {
            return CommandResult.Fail("Unknown plot.");
        }
        if (estate.OwnerId != null)
        {
            return CommandResult.Fail("That plot isn't free.");
        }
        if (CrewAt(estate.Station) is not { Open: false } crew)
        {
            return CommandResult.Fail("That Yard is open: settle there right away.");
        }
        if (company.CrewId == crew.Id)
        {
            return CommandResult.Fail("You live there already.");
        }
        crew.Requests.RemoveAll(t => t.Company == company.Id);
        crew.Requests.Add(new YardRequest { Company = company.Id, Slot = seat.Slot, At = LastTick });
        CrewChanged(crew);
        foreach (var member in CrewMembers(crew))
        {
            Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Crew, $"{company.Name} asks to settle at {crew.Name}: say yes or no in the Yard panel", Bell: true));
        }
        return CommandResult.Ok;
    }

    /// <summary>A resident answers a request: yes moves the newcomer in for free (onto the plot asked for, or another free one)</summary>
    public CommandResult AnswerRequest(long residentId, long requesterId, bool yes)
    {
        if (Company(residentId) is not { } resident || CrewOf(resident) is not { } crew || crew.Requests.FirstOrDefault(t => t.Company == requesterId) is not { } request)
        {
            return CommandResult.Fail("That request is gone.");
        }
        crew.Requests.Remove(request);
        CrewChanged(crew);
        if (Company(requesterId) is not { } requester)
        {
            return CommandResult.Ok;
        }
        if (!yes)
        {
            Changes.Events.Add(new WorldEvent(requester.Id, WorldEventKind.Crew, $"{crew.Name} stays as it is for now: your plot is still yours"));
            return CommandResult.Ok;
        }
        var seat = new EstateKey(crew.Yard.District, request.Slot);
        if (Estate(seat.District, seat.Slot)?.OwnerId != null)
        {
            seat = StationSeats(crew.Yard).FirstOrDefault(t => Estate(t.District, t.Slot) is { OwnerId: null }, seat);
        }
        if (Estate(seat.District, seat.Slot)?.OwnerId != null)
        {
            return CommandResult.Fail($"{crew.Name} has no free plot left.");
        }
        requester.FreeMoves++;
        var moved = RelocateTo(requester, seat, invitedBy: crew);
        if (!moved.Success)
        {
            requester.FreeMoves--;
            return moved;
        }
        Changes.Events.Add(new WorldEvent(requester.Id, WorldEventKind.Crew, $"{resident.Name} said yes: your plot moves to {crew.Name}", Bell: true));
        return CommandResult.Ok;
    }

    /// <summary>Once a day: requests older than three days lapse</summary>
    private void CheckRequests()
    {
        foreach (var crew in _crews.Values.Where(t => t.Requests.Count > 0).ToList())
        {
            if (crew.Requests.RemoveAll(t => LastTick - t.At >= RequestWaits || Company(t.Company) == null) > 0)
            {
                CrewChanged(crew);
            }
        }
    }

    // ---------------------------------------------------------------- "Play next to me"

    /// <summary>
    /// A company's "Play next to me" link (1.4 §6): it opens the map at its Yard and lets the first player who uses it
    /// settle there even when the Yard is closed, for seven days. Nothing is held. Asking again renews it.
    /// </summary>
    public string NextToMeLink(long companyId)
    {
        var company = Company(companyId) ?? throw new ArgumentException("Unknown company.", nameof(companyId));
        if (company.NextToMeCode == null || company.NextToMeUntil <= LastTick || company.NextToMeUsed)
        {
            company.NextToMeCode = NewCode();
            company.NextToMeUsed = false;
        }
        company.NextToMeUntil = LastTick + NextToMeFor;
        company.Touch();
        return company.NextToMeCode;
    }

    /// <summary>The company whose "Play next to me" link this is (null: no such link, or it ran out)</summary>
    public Company? NextToMeOf(string code)
    {
        code = NormalizeCode(code);
        return _companies.Values.FirstOrDefault(t => t.NextToMeCode == code && t.NextToMeUntil > LastTick);
    }

    /// <summary>Whether a link still lets its first user into its owner's Yard while the Yard is closed</summary>
    private bool LinkOpens(string? code, StationRef yard) =>
        code != null && NextToMeOf(code) is { NextToMeUsed: false } owner && EstateOf(owner)?.Station == yard;

    private static string NormalizeCode(string code) => new(code.Trim().ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

    /// <summary>A new code nobody uses: 8 letters and digits without the ones that look alike</summary>
    private string NewCode()
    {
        const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        while (true)
        {
            var code = new string(Enumerable.Range(0, 8).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());
            if (_companies.Values.All(t => t.NextToMeCode != code))
            {
                return code;
            }
        }
    }

    /// <summary>How a code is shown: two groups of four</summary>
    public static string ShowCode(string code) => code.Length == 8 ? $"{code[..4]}-{code[4..]}" : code;

    /// <summary>The crew hears that someone moved in</summary>
    private void AnnounceArrival(Crew crew, Company company)
    {
        foreach (var member in CrewMembers(crew).Where(t => t.Id != company.Id))
        {
            var text = $"{company.Name} moved in next door";
            member.Report.NewNeighbours.Add(company.Name);
            member.Report.Notes.Add(text);
            member.Touch();
            Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Neighbour, text));
        }
    }

    // ---------------------------------------------------------------- Plots and placement

    /// <summary>The plots of a Yard</summary>
    public static IEnumerable<EstateKey> StationSeats(StationRef station) =>
        WorldLayout.StationSlots(station.Index).Select(t => new EstateKey(station.District, t));

    /// <summary>
    /// Whether a newcomer may settle on a plot: it is free, and its Yard is open, or the newcomer's request was
    /// answered yes (invitedBy), or the newcomer has a resident's unused "Play next to me" link
    /// </summary>
    public bool SeatFree(EstateKey seat, Company? newcomer = null, string? code = null, Crew? invitedBy = null)
    {
        if (Estate(seat.District, seat.Slot) is not { OwnerId: null } estate)
        {
            return false;
        }
        var crew = CrewAt(estate.Station);
        return crew == null || crew.Open || crew == invitedBy || LinkOpens(code, estate.Station);
    }

    /// <summary>Players who opened the game in the last three days living around a Yard</summary>
    private List<Company> ActiveResidents(StationRef station) =>
        StationSeats(station)
            .Select(t => Estate(t.District, t.Slot)?.OwnerId is { } owner ? Company(owner) : null)
            .OfType<Company>()
            .Where(IsActive)
            .ToList();

    /// <summary>Where a town is in the order towns open (the Capital first)</summary>
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
    /// Pick for me (1.4 §6): a free plot in an open Yard whose residents played in the last three days (the Yard where
    /// most of them did first, then the oldest town), else a plot in an empty Yard of the oldest town with room. Never a
    /// closed Yard, never a Yard whose residents all stopped playing.
    /// </summary>
    public EstateKey PickForMe(Company? newcomer = null)
    {
        OpenRoom();
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var lively = new List<(EstateKey Seat, int Active, int Rank)>();
            var empty = new List<(EstateKey Seat, int Rank)>();
            foreach (var district in _openDistricts)
            {
                var rank = DistrictRank(district);
                foreach (var station in WorldLayout.Stations(district))
                {
                    if (StationSeats(station).Where(t => SeatFree(t, newcomer)).Select(t => (EstateKey?)t).FirstOrDefault() is not { } free)
                    {
                        continue;
                    }
                    if (CrewAt(station) is null)
                    {
                        empty.Add((free, rank));
                        continue;
                    }
                    var active = ActiveResidents(station).Count(t => t.Id != newcomer?.Id);
                    if (active > 0)
                    {
                        lively.Add((free, active, rank));
                    }
                }
            }
            if (lively.Count > 0)
            {
                return lively.OrderByDescending(t => t.Active).ThenBy(t => t.Rank).ThenBy(t => t.Seat.Slot).First().Seat;
            }
            if (empty.Count > 0)
            {
                return empty.OrderBy(t => t.Rank).ThenBy(t => t.Seat.Slot).First().Seat;
            }
            OpenNextDistrict();
        }
        throw new InvalidOperationException("The world is infinite, this can't happen.");
    }

    /// <summary>A new town opens when every open town has fewer than two free plots, so newcomers always find room</summary>
    private void OpenRoom()
    {
        if (_openDistricts.Count == 0)
        {
            OpenNextDistrict();
            return;
        }
        if (_openDistricts.All(district => Enumerable.Range(0, WorldLayout.EstatesPerDistrict).Count(t => _estates[(district, t)].OwnerId == null) < OpenNextTownBelow))
        {
            OpenNextDistrict();
        }
    }

    /// <summary>
    /// Moves a company's plot to a free plot (1.4 §18): balloons lift it, a moment later it lands. Free and quick
    /// (content: together), with a few minutes between moves; into a closed Yard only with a resident's yes or link.
    /// </summary>
    public CommandResult RelocateTo(long companyId, EstateKey seat, string? code = null) =>
        Company(companyId) is { } company ? RelocateTo(company, seat, code) : CommandResult.Fail("Unknown company.");

    private CommandResult RelocateTo(Company company, EstateKey seat, string? code = null, Crew? invitedBy = null)
    {
        if (company.Packed != null)
        {
            return CommandResult.Fail("Your plot is on its way already.");
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
        if (Estate(seat.District, seat.Slot) is not { OwnerId: null } target)
        {
            return CommandResult.Fail("That plot isn't free.");
        }
        if (!SeatFree(seat, company, code, invitedBy))
        {
            return CommandResult.Fail("That Yard is closed: ask to settle there, and a resident says yes.");
        }
        var cost = MoveCost(company);
        if (company.Gold < cost)
        {
            return CommandResult.Fail($"A move costs {cost:N0} gold.");
        }
        if (LinkOpens(code, target.Station) && CrewAt(target.Station) is { Open: false })
        {
            NextToMeOf(code!)!.NextToMeUsed = true;
        }
        if (company.FreeMoves > 0)
        {
            company.FreeMoves--;
        }
        else
        {
            company.Gold -= cost;
            company.Report.GoldSpent += cost;
            AddStat(company, "goldSpent", cost);
        }
        EndTradeOf(company);
        Pack(company);
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
