using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// Corporations (GDD §9.2). HQ perks: 1 free trade between members (no maximum price) and corporation gates, 2 +5%
/// production of a chosen resource, 3 no tolls between members, 4 +1 manager slot, 5 techs 20% cheaper for members,
/// 6-10 the production perk grows by 1% per level.
/// </summary>
public sealed partial class GameWorld
{
    public const int MaxCorporationMembers = 30;
    public const int CorporationFoundLevel = 8;
    public const double CorporationFoundCost = 10_000;
    public const int MaxHqLevel = 10;
    public static readonly TimeSpan RejoinWait = TimeSpan.FromDays(1);

    private readonly Dictionary<long, Corporation> _corporations = new();

    public long NextCorporationId { get; set; } = 1;
    public IEnumerable<Corporation> Corporations => _corporations.Values;

    public Corporation? Corporation(long id) => _corporations.TryGetValue(id, out var corporation) ? corporation : null;

    public Corporation? CorporationOf(Company company) => company.CorporationId is { } id ? Corporation(id) : null;

    public IEnumerable<Company> Members(Corporation corporation) => _companies.Values.Where(t => t.CorporationId == corporation.Id);

    public void LoadCorporation(Corporation corporation)
    {
        _corporations[corporation.Id] = corporation;
        NextCorporationId = Math.Max(NextCorporationId, corporation.Id + 1);
        corporation.Dirty = false;
    }

    public bool SameCorporation(long a, long b) =>
        a == b || (Company(a)?.CorporationId is { } corporation && Company(b)?.CorporationId == corporation);

    private int PerkLevel(long companyId) => Company(companyId) is { } company && CorporationOf(company) is { } corporation ? corporation.HqLevel : 0;

    /// <summary>Members of a corporation trade with each other whatever the buyer's maximum price</summary>
    public bool MembersTradeFreely(long seller, long buyer) => seller != buyer && SameCorporation(seller, buyer) && PerkLevel(seller) >= 1;

    /// <summary>The production perk: +5% of the chosen resource from HQ level 2, +1% per level above 5</summary>
    public double CorporationOutputBonus(Company company, string resource) =>
        CorporationOf(company) is { HqLevel: >= 2 } corporation && corporation.PerkResource == resource
            ? 0.05 + Math.Max(0, corporation.HqLevel - 5) * 0.01
            : 0;

    public bool TollFree(long payer, long meterOwner) => SameCorporation(payer, meterOwner) && PerkLevel(payer) >= 3;

    public double CorporationResearchDiscount(Company company) => CorporationOf(company) is { HqLevel: >= 5 } ? 0.2 : 0;

    /// <summary>The gold to upgrade an HQ from a level to the next, paid from the treasury</summary>
    public static double HqUpgradeCost(int level) => Math.Round(20_000 * Math.Pow(3, level - 1));

    private CommandResult NameCheck(string name, string tag, long? except)
    {
        name = name.Trim();
        tag = tag.Trim().ToUpperInvariant();
        if (name.Length is < 3 or > 30)
        {
            return CommandResult.Fail("A corporation name has 3 to 30 characters.");
        }
        if (tag.Length is < 2 or > 5 || !tag.All(char.IsLetterOrDigit))
        {
            return CommandResult.Fail("A tag has 2 to 5 letters or digits.");
        }
        if (_corporations.Values.Any(t => t.Id != except && (string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase) || t.Tag == tag)))
        {
            return CommandResult.Fail("That name or tag is taken.");
        }
        return CommandResult.Ok;
    }

    public CommandResult FoundCorporation(long companyId, string name, string tag, string color)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (company.CorporationId != null)
        {
            return CommandResult.Fail("Leave your corporation first.");
        }
        if (company.Level < CorporationFoundLevel)
        {
            return CommandResult.Fail($"Founding a corporation needs Company Level {CorporationFoundLevel}.");
        }
        var check = NameCheck(name, tag, null);
        if (!check.Success)
        {
            return check;
        }
        var payment = Pay(company, CorporationFoundCost, new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }
        var corporation = new Corporation
        {
            Id = NextCorporationId++,
            Name = name.Trim(),
            Tag = tag.Trim().ToUpperInvariant(),
            Color = color,
            District = company.District,
            CreatedAt = Now,
        };
        _corporations[corporation.Id] = corporation;
        Join(company, corporation, CorporationRole.Leader);
        corporation.Goal = NewGoal(corporation);
        return CommandResult.Ok;
    }

    private void Join(Company company, Corporation corporation, CorporationRole role)
    {
        company.CorporationId = corporation.Id;
        company.Role = role;
        company.Touch();
        foreach (var other in _corporations.Values)
        {
            other.Applications.Remove(company.Id);
            other.Invites.Remove(company.Id);
            other.Touch();
        }
        CorporationChanged(corporation);
        MarkTradeDirty(company.Id);
    }

    private void CorporationChanged(Corporation corporation)
    {
        corporation.Touch();
        Changes.Corporations.Add(corporation.Id);
        foreach (var member in Members(corporation))
        {
            Changes.Companies.Add(member.Id);
        }
        Changes.Estates.UnionWith(Members(corporation).Select(t => (t.District, t.Slot)));
    }

    private string? CanJoin(Company company, Corporation corporation)
    {
        if (company.CorporationId != null)
        {
            return "Leave your corporation first.";
        }
        if (company.District != corporation.District)
        {
            return "Corporations are for companies of one district.";
        }
        if (Members(corporation).Count() >= MaxCorporationMembers)
        {
            return "That corporation is full.";
        }
        if (company.LeftCorporationAt is { } left && left + RejoinWait > Now)
        {
            return "You left a corporation less than a day ago.";
        }
        return null;
    }

    /// <summary>Asks to join; an invite that is already there is accepted right away</summary>
    public CommandResult ApplyToCorporation(long companyId, long corporationId)
    {
        if (Company(companyId) is not { } company || Corporation(corporationId) is not { } corporation)
        {
            return CommandResult.Fail("Unknown corporation.");
        }
        if (CanJoin(company, corporation) is { } reason)
        {
            return CommandResult.Fail(reason);
        }
        if (corporation.Invites.Contains(companyId))
        {
            Join(company, corporation, CorporationRole.Member);
            return CommandResult.Ok;
        }
        corporation.Applications.Add(companyId);
        CorporationChanged(corporation);
        foreach (var officer in Members(corporation).Where(t => t.Role >= CorporationRole.Officer))
        {
            Changes.Events.Add(new WorldEvent(officer.Id, WorldEventKind.Corporation, $"{company.Name} wants to join {corporation.Name}"));
        }
        return CommandResult.Ok;
    }

    private (Company Officer, Corporation Corporation)? Officer(long companyId)
    {
        if (Company(companyId) is { Role: >= CorporationRole.Officer } company && CorporationOf(company) is { } corporation)
        {
            return (company, corporation);
        }
        return null;
    }

    /// <summary>Invites a company; when it applied already it joins right away</summary>
    public CommandResult InviteToCorporation(long officerId, long companyId)
    {
        if (Officer(officerId) is not var (officer, corporation))
        {
            return CommandResult.Fail("Only leaders and officers can invite.");
        }
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (CanJoin(company, corporation) is { } reason)
        {
            return CommandResult.Fail(reason);
        }
        if (corporation.Applications.Contains(companyId))
        {
            Join(company, corporation, CorporationRole.Member);
            return CommandResult.Ok;
        }
        corporation.Invites.Add(companyId);
        CorporationChanged(corporation);
        Changes.Events.Add(new WorldEvent(companyId, WorldEventKind.Corporation, $"{officer.Name} invites you to {corporation.Name}"));
        return CommandResult.Ok;
    }

    public CommandResult DeclineCorporation(long companyId, long corporationId)
    {
        if (Corporation(corporationId) is not { } corporation)
        {
            return CommandResult.Fail("Unknown corporation.");
        }
        corporation.Invites.Remove(companyId);
        corporation.Applications.Remove(companyId);
        CorporationChanged(corporation);
        return CommandResult.Ok;
    }

    /// <summary>An officer turns an application down</summary>
    public CommandResult DeclineApplication(long officerId, long applicantId)
    {
        if (Company(officerId) is not { Role: CorporationRole.Leader or CorporationRole.Officer } officer || CorporationOf(officer) is not { } corporation)
        {
            return CommandResult.Fail("Only officers handle applications.");
        }
        if (!corporation.Applications.Remove(applicantId))
        {
            return CommandResult.Fail("No application from that company.");
        }
        Changes.Events.Add(new WorldEvent(applicantId, WorldEventKind.Corporation, $"{corporation.Name} declined your application"));
        CorporationChanged(corporation);
        return CommandResult.Ok;
    }

    public CommandResult LeaveCorporation(long companyId) => RemoveMember(companyId, companyId);

    public CommandResult KickFromCorporation(long officerId, long memberId)
    {
        if (Officer(officerId) is not var (officer, corporation) || Company(memberId) is not { } member || member.CorporationId != corporation.Id)
        {
            return CommandResult.Fail("Only leaders and officers can remove members.");
        }
        if (member.Role >= officer.Role)
        {
            return CommandResult.Fail("You can only remove members with a lower role.");
        }
        return RemoveMember(officerId, memberId);
    }

    private CommandResult RemoveMember(long byId, long memberId)
    {
        if (Company(memberId) is not { } member || CorporationOf(member) is not { } corporation)
        {
            return CommandResult.Fail("Not in a corporation.");
        }
        var wasLeader = member.Role == CorporationRole.Leader;
        member.CorporationId = null;
        member.Role = CorporationRole.Member;
        member.LeftCorporationAt = Now;
        member.Touch();
        Changes.Companies.Add(member.Id);
        Changes.Estates.Add((member.District, member.Slot));
        MarkTradeDirty(member.Id);
        if (byId != memberId)
        {
            Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Corporation, $"You were removed from {corporation.Name}"));
        }
        var rest = Members(corporation).OrderByDescending(t => t.Role).ThenBy(t => t.Id).ToList();
        if (rest.Count == 0)
        {
            // The last member left: the corporation is dissolved, the treasury is lost
            _corporations.Remove(corporation.Id);
            Deleted.Add(corporation);
            Changes.Corporations.Add(corporation.Id);
        }
        else
        {
            if (wasLeader)
            {
                rest[0].Role = CorporationRole.Leader;
                rest[0].Touch();
            }
            CorporationChanged(corporation);
        }
        return CommandResult.Ok;
    }

    public CommandResult SetCorporationRole(long leaderId, long memberId, CorporationRole role)
    {
        if (Company(leaderId) is not { Role: CorporationRole.Leader } leader || CorporationOf(leader) is not { } corporation
            || Company(memberId) is not { } member || member.CorporationId != corporation.Id || member.Id == leader.Id)
        {
            return CommandResult.Fail("Only the leader can change roles.");
        }
        if (role == CorporationRole.Leader)
        {
            leader.Role = CorporationRole.Officer;
            leader.Touch();
        }
        member.Role = role;
        member.Touch();
        CorporationChanged(corporation);
        return CommandResult.Ok;
    }

    public CommandResult DonateToCorporation(long companyId, double gold)
    {
        if (Company(companyId) is not { } company || CorporationOf(company) is not { } corporation)
        {
            return CommandResult.Fail("You are not in a corporation.");
        }
        if (gold <= 0)
        {
            return CommandResult.Fail("Choose an amount.");
        }
        var payment = Pay(company, gold, new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }
        corporation.Treasury += gold;
        CorporationChanged(corporation);
        return CommandResult.Ok;
    }

    public CommandResult SetCorporationSettings(long officerId, double tax, string? perkResource, string description)
    {
        if (Officer(officerId) is not var (_, corporation))
        {
            return CommandResult.Fail("Only leaders and officers can change this.");
        }
        if (perkResource != null && !Content.Resources.ContainsKey(perkResource))
        {
            return CommandResult.Fail("Unknown resource.");
        }
        corporation.Tax = Math.Round(Math.Clamp(tax, 0, 0.1), 3);
        if (corporation.PerkResource != perkResource)
        {
            corporation.PerkResource = perkResource;
            foreach (var member in Members(corporation))
            {
                MarkTradeDirty(member.Id);
            }
        }
        corporation.Description = description.Length > 300 ? description[..300] : description;
        CorporationChanged(corporation);
        return CommandResult.Ok;
    }

    public CommandResult UpgradeCorporationHq(long officerId)
    {
        if (Officer(officerId) is not var (_, corporation))
        {
            return CommandResult.Fail("Only leaders and officers can upgrade the HQ.");
        }
        if (corporation.HqLevel >= MaxHqLevel)
        {
            return CommandResult.Fail("The HQ is at its highest level.");
        }
        var cost = HqUpgradeCost(corporation.HqLevel);
        if (corporation.Treasury < cost)
        {
            return CommandResult.Fail($"The treasury needs {cost:N0} gold.");
        }
        corporation.Treasury -= cost;
        corporation.HqLevel++;
        foreach (var member in Members(corporation))
        {
            MarkTradeDirty(member.Id);
            Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Corporation, $"{corporation.Name} HQ is now level {corporation.HqLevel}"));
        }
        CorporationChanged(corporation);
        return CommandResult.Ok;
    }

    /// <summary>The week's goal: an amount of something the members can make, more with more members</summary>
    private CorporationGoal NewGoal(Corporation corporation)
    {
        var members = Members(corporation).ToList();
        var resources = members.SelectMany(OrderResources).Distinct().Order(StringComparer.Ordinal).ToList();
        if (resources.Count == 0)
        {
            resources = ["energy"];
        }
        var week = (int)((LastTick - DateTime.UnixEpoch).TotalDays / 7);
        var resource = resources[(int)((corporation.Id * 31 + week) % resources.Count)];
        var level = members.Count == 0 ? 1 : members.Average(t => t.Level);
        var value = 4000 * Math.Pow(level, 1.3) * Math.Max(1, members.Count);
        var amount = Math.Max(100, Math.Round(value / BasePrice(resource) / 100) * 100);
        return new CorporationGoal
        {
            Resource = resource,
            Amount = amount,
            RewardGold = Math.Round(amount * BasePrice(resource) * 1.5),
            EndsAt = NextWeek(LastTick),
        };
    }

    /// <summary>The next Monday midnight (UTC)</summary>
    public static DateTime NextWeek(DateTime after)
    {
        var days = ((int)DayOfWeek.Monday - (int)after.DayOfWeek + 7) % 7;
        var next = after.Date.AddDays(days == 0 ? 7 : days);
        return DateTime.SpecifyKind(next, DateTimeKind.Utc);
    }

    /// <summary>Delivers what the storages have for the corporation's goal</summary>
    public CommandResult DeliverToCorporationGoal(long companyId)
    {
        if (Company(companyId) is not { } company || CorporationOf(company) is not { Goal: { } goal } corporation)
        {
            return CommandResult.Fail("Your corporation has no goal.");
        }
        if (goal.Delivered >= goal.Amount)
        {
            return CommandResult.Fail("The goal is reached.");
        }
        var available = Materials(company.Id).TryGetValue(goal.Resource, out var have) ? have : 0;
        var amount = Math.Min(goal.Amount - goal.Delivered, available);
        if (amount <= 1e-9)
        {
            return CommandResult.Fail($"Your storages have no {Content.Resources[goal.Resource].Name.ToLowerInvariant()}.");
        }
        var payment = Pay(company, 0, new Dictionary<string, double> { [goal.Resource] = amount });
        if (!payment.Success)
        {
            return payment;
        }
        goal.Delivered += amount;
        goal.Contributions[companyId] = (goal.Contributions.TryGetValue(companyId, out var c) ? c : 0) + amount;
        if (goal.Delivered >= goal.Amount - 1e-9 && !goal.Rewarded)
        {
            RewardGoal(corporation, goal);
        }
        CorporationChanged(corporation);
        return CommandResult.Ok;
    }

    /// <summary>A reached goal: gold for the treasury and XP for everyone who delivered</summary>
    private void RewardGoal(Corporation corporation, CorporationGoal goal)
    {
        goal.Rewarded = true;
        corporation.Treasury += goal.RewardGold;
        foreach (var (id, amount) in goal.Contributions)
        {
            if (Company(id) is { } member)
            {
                GainXp(member, 50 + 200 * amount / goal.Amount * member.Level, "corporation", goal.Resource);
                Changes.Events.Add(new WorldEvent(member.Id, WorldEventKind.Corporation, $"{corporation.Name} reached its weekly goal!"));
            }
        }
    }

    private void NewWeek()
    {
        RegroupLeagues();
        foreach (var corporation in _corporations.Values)
        {
            corporation.Goal = NewGoal(corporation);
            CorporationChanged(corporation);
        }
        foreach (var company in _companies.Values)
        {
            company.WeekStartEarnings = company.LifetimeEarnings;
            company.Touch();
        }
        CheckQuietDistricts();
    }

    /// <summary>The corporation tax on a member's trade income goes to the treasury</summary>
    private void TaxEarnings(Company company, double earned)
    {
        if (CorporationOf(company) is { Tax: > 0 } corporation)
        {
            var tax = earned * corporation.Tax;
            company.Gold -= tax;
            corporation.Treasury += tax;
            corporation.Touch();
        }
    }
}
