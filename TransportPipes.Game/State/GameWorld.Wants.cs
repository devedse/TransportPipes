using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// Wants (after run 3, the design team's step 2, docs/playtest-run3-design.html): a building short of materials stands on
/// the map as a scaffold that wants them. It fills from its owner's Storages and spare first, then a friend's tap from
/// their Storage, then the station's spare through the Yard at the one player price; the goods go into the build, never a
/// Storage, and its construction starts once it has them all. Until then Cancel gives the gold back. (Run 3: the Big
/// Factory's 100 bricks, the Market Hall's 500 and every Era 2 building could only come from the player's own Storage:
/// "I don't know how to hand you 100 bricks directly", Cleo, day 21.)
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>A scaffold's construction "ends" at this far moment: it starts for real once its materials are in</summary>
    public static readonly DateTime SiteWaiting = new(9999, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Whether a building is a scaffold that still wants materials</summary>
    public static bool IsSite(Building building) => building.ConstructionEndsAt == SiteWaiting;

    /// <summary>A scaffold's wants: one per material still lacking (its owner's Asks for that building)</summary>
    public List<CompanyAsk> WantsOf(Building building) =>
        Company(building.Owner) is { } owner ? owner.Asks.Where(t => t.Site == building.Id).ToList() : [];

    /// <summary>
    /// A building whose gold is there but not its materials: it stands as a scaffold, takes what its owner's Storages
    /// hold now, and wants the rest
    /// </summary>
    private Building PlaceSite(Company company, BuildingDefinition definition, TilePos pos, double cost)
    {
        company.Gold -= cost;
        company.Report.GoldSpent += cost;
        var site = AddBuilding(company, definition.Id, pos, finished: false);
        site.ConstructionEndsAt = SiteWaiting;
        foreach (var (resource, amount) in definition.Cost.Materials)
        {
            company.Asks.Add(new CompanyAsk { Resource = resource, Amount = amount, Until = DateTime.MaxValue, Hours = 0, Site = site.Id, Raised = LastTick });
        }
        FillWantsFromStorages(company);
        FinishWantsIfFull(company, site);
        company.Touch();
        Changes.Companies.Add(company.Id);
        WakeCoupling();
        AnnounceWant(company, site);
        return site;
    }

    /// <summary>The station's players who can fill a want hear of it once: who holds it in a Storage or makes it</summary>
    private void AnnounceWant(Company owner, Building site)
    {
        if (!IsSite(site) || EstateOf(owner) is not { } estate)
        {
            return;
        }
        var name = Content.Building(site.Type).Name;
        foreach (var ask in WantsOf(site))
        {
            foreach (var friend in StationPlayers(estate.Station).Where(t => t.Id != owner.Id && !t.Asleep && IsActive(t)))
            {
                var holds = Materials(friend.Id).GetValueOrDefault(ask.Resource);
                var makes = BuildingsOf(friend.Id).Any(b => !b.UnderConstruction && Content.Building(b.Type).Out.ContainsKey(ask.Resource));
                if (holds >= 1 || makes)
                {
                    Changes.Events.Add(new WorldEvent(friend.Id, WorldEventKind.Neighbour,
                        $"{NameFor(owner, friend)}'s {name} wants {ask.Left:N0} {GoodsWord(ask.Resource)}: {(holds >= 1 ? $"your Storage holds {holds:N0}" : "your spare goes there first")}"));
                }
            }
        }
    }

    /// <summary>Its owner's Storages give a want what they hold of it (theirs already: nothing is paid)</summary>
    private void FillWantsFromStorages(Company company)
    {
        foreach (var ask in company.Asks.Where(t => t.IsWant && t.Left > 1e-6))
        {
            var have = Materials(company.Id).GetValueOrDefault(ask.Resource);
            if (have > 1e-6)
            {
                var units = TakeFromStorages(company.Id, ask.Resource, Math.Min(have, ask.Left));
                ask.Got += units;
                ask.From[company.Id] = ask.From.GetValueOrDefault(company.Id) + units;
            }
        }
    }

    /// <summary>Takes goods out of a company's Storages, the oldest first; returns how much it took</summary>
    private double TakeFromStorages(long companyId, string resource, double amount)
    {
        var left = amount;
        foreach (var storage in _buildings.Values.Where(t => t.Owner == companyId && t.Contents.ContainsKey(resource)).OrderBy(t => t.Id))
        {
            var take = Math.Min(left, storage.Contents[resource]);
            storage.Contents[resource] -= take;
            if (storage.Contents[resource] <= StorageEpsilon)
            {
                storage.Contents.Remove(resource);
            }
            storage.Touch();
            MarkDirty(storage);
            left -= take;
            if (left <= 1e-9)
            {
                break;
            }
        }
        return amount - left;
    }

    /// <summary>What the Yard brought a want arrives in the build: its owner's own goods free, a neighbour's at the one player price</summary>
    private void SettleWant(Company owner, CompanyAsk ask, double minutes)
    {
        foreach (var (sellerId, perMinute) in ask.Incoming)
        {
            if (Company(sellerId) is not { } seller)
            {
                continue;
            }
            var units = Math.Min(perMinute * minutes, ask.Left);
            if (units <= 1e-9)
            {
                continue;
            }
            ask.Got += units;
            ask.From[sellerId] = ask.From.GetValueOrDefault(sellerId) + units;
            if (seller.Id != owner.Id)
            {
                PayForWant(owner, seller, ask, units);
            }
        }
        ShipOrder(owner, ask, minutes);
        FillWantsFromStorages(owner);
        if (ask.Site is { } id && Building(id) is { } site)
        {
            FinishWantsIfFull(owner, site);
        }
        else if (ask.PipeTiles != null)
        {
            FinishPipeWantIfFull(owner, ask);
        }
    }

    /// <summary>A neighbour's goods in a want: the owner pays the one player price (the goods are in a build, not to be resold)</summary>
    private void PayForWant(Company owner, Company seller, CompanyAsk ask, double units)
    {
        var gold = units * PlayerPrice(ask.Resource);
        owner.Gold -= gold;
        ask.Paid += gold;
        AddStat(owner, "wantPaid", gold);
        seller.Gold += gold;
        seller.Report.GoldEarned += gold;
        Add(seller.Report.Sold, ask.Resource, units);
        AddStat(seller, "wantGold", gold);
        GainXpFromEarnings(seller, gold);
        seller.Touch();
        owner.Touch();
        Changes.Companies.Add(seller.Id);
        Changes.Companies.Add(owner.Id);
    }

    /// <summary>A scaffold with all its materials starts being built; everyone who brought some hears whose building they built</summary>
    private void FinishWantsIfFull(Company owner, Building site)
    {
        if (!IsSite(site))
        {
            return;
        }
        var wants = WantsOf(site);
        if (wants.Any(t => t.Left > 1e-6))
        {
            return;
        }
        var definition = Content.Building(site.Type);
        site.ConstructionEndsAt = LastTick.AddSeconds(definition.BuildSeconds);
        site.Touch();
        MarkDirty(site);
        foreach (var tile in site.Footprint())
        {
            Changes.Tiles.Add(tile);
        }
        foreach (var want in wants)
        {
            CloseOrder(want);
        }
        var givers = wants.SelectMany(t => t.From.Where(f => f.Key != owner.Id && f.Key != HubSource && f.Value > 0.5).Select(f => (Who: f.Key, t.Resource, Units: f.Value))).ToList();
        owner.Asks.RemoveAll(t => t.Site == site.Id);
        Changes.Events.Add(new WorldEvent(owner.Id, WorldEventKind.ConstructionFinished, givers.Count == 0
            ? $"Your {definition.Name} has its materials: it's being built"
            : $"Your {definition.Name} has its materials, {string.Join(" and ", givers.Select(t => $"{Company(t.Who)?.Name}'s {GoodsWord(t.Resource)}").Distinct())} among them: it's being built 💛",
            site.Id));
        foreach (var (who, resource, units) in givers)
        {
            Changes.Events.Add(new WorldEvent(who, WorldEventKind.Neighbour, $"Your {GoodsWord(resource)} built {owner.Name}'s {definition.Name}: {units:N0} of them"));
        }
        owner.Touch();
        Changes.Companies.Add(owner.Id);
    }

    /// <summary>
    /// A friend's tap: what their Storages hold of what a scaffold wants goes straight into it, paid at the one player
    /// price ("Ana's Big Factory wants 100 bricks · your Storage holds 500 · Send them ▸")
    /// </summary>
    public CommandResult SendToSite(long friendId, long siteId)
    {
        // Minus a number: a neighbour's pipe want (step 3)
        if (siteId < 0)
        {
            return SendToPipeWant(friendId, -siteId);
        }
        if (Company(friendId) is not { } friend || Building(siteId) is not { } site || !IsSite(site) || Company(site.Owner) is not { } owner)
        {
            return CommandResult.Fail("That building has what it needs.");
        }
        if (owner.Id == friendId)
        {
            return CommandResult.Fail("Your own Storages give it what they hold by themselves.");
        }
        if (EstateOf(friend) is not { } estate || !StationPlayers(estate.Station).Any(t => t.Id == owner.Id))
        {
            return CommandResult.Fail("Only your station's neighbours can send to it.");
        }
        if (SameDevice(friend, owner))
        {
            return CommandResult.Fail("Not between two accounts on one device.");
        }
        var sent = new List<string>();
        foreach (var ask in WantsOf(site).Where(t => t.Left > 1e-6))
        {
            var units = TakeFromStorages(friend.Id, ask.Resource, Math.Min(Materials(friend.Id).GetValueOrDefault(ask.Resource), ask.Left));
            if (units <= 1e-6)
            {
                continue;
            }
            ask.Got += units;
            ask.From[friend.Id] = ask.From.GetValueOrDefault(friend.Id) + units;
            PayForWant(owner, friend, ask, units);
            sent.Add($"{units:N0} {GoodsWord(ask.Resource)}");
        }
        if (sent.Count == 0)
        {
            return CommandResult.Fail("Your Storages hold none of what it wants.");
        }
        Changes.Events.Add(new WorldEvent(owner.Id, WorldEventKind.Neighbour, $"{NameFor(friend, owner)} sent {string.Join(" and ", sent)} to your {Content.Building(site.Type).Name} 💛"));
        FinishWantsIfFull(owner, site);
        return CommandResult.Ok;
    }

    /// <summary>A friend's tap into a neighbour's pipe want (its owner is whoever has a pipe want of that number in the friend's station)</summary>
    private CommandResult SendToPipeWant(long friendId, long wantId)
    {
        if (Company(friendId) is not { } friend || EstateOf(friend) is not { } estate)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (WantsNear(friend).FirstOrDefault(t => t.Target == -wantId) is not { Owner: not null } near)
        {
            return CommandResult.Fail("That pipe has what it needs.");
        }
        var owner = near.Owner;
        if (SameDevice(friend, owner))
        {
            return CommandResult.Fail("Not between two accounts on one device.");
        }
        var sent = new List<string>();
        foreach (var ask in owner.Asks.Where(t => t.PipeTiles != null && t.WantId == wantId && t.Left > 1e-6).ToList())
        {
            var units = TakeFromStorages(friend.Id, ask.Resource, Math.Min(Materials(friend.Id).GetValueOrDefault(ask.Resource), ask.Left));
            if (units <= 1e-6)
            {
                continue;
            }
            ask.Got += units;
            ask.From[friend.Id] = ask.From.GetValueOrDefault(friend.Id) + units;
            PayForWant(owner, friend, ask, units);
            sent.Add($"{units:N0} {GoodsWord(ask.Resource)}");
            FinishPipeWantIfFull(owner, ask);
        }
        if (sent.Count == 0)
        {
            return CommandResult.Fail("Your Storages hold none of what it wants.");
        }
        Changes.Events.Add(new WorldEvent(owner.Id, WorldEventKind.Neighbour, $"{NameFor(friend, owner)} sent {string.Join(" and ", sent)} for your {near.Name} 💛"));
        return CommandResult.Ok;
    }

    /// <summary>A scaffold is given up before it stands: the building's price and what was paid for its goods come back</summary>
    public CommandResult CancelSite(long companyId, long siteId)
    {
        if (Company(companyId) is not { } company || Building(siteId) is not { } site || site.Owner != companyId || !IsSite(site))
        {
            return CommandResult.Fail("Only a building that still waits for its materials can be given up.");
        }
        var wants = WantsOf(site);
        foreach (var want in wants)
        {
            CloseOrder(want);
        }
        var refund = wants.Sum(t => t.Paid) + (company.Purchases.FirstOrDefault(t => t.Building == siteId)?.Cost ?? BuildCost(company, Content.Building(site.Type)));
        // What its owner's own Storages gave goes back into them, as far as they hold it
        foreach (var ask in wants)
        {
            if (ask.From.GetValueOrDefault(company.Id) is > 1e-6 and var own)
            {
                PutInStorages(company.Id, ask.Resource, own);
            }
        }
        company.Asks.RemoveAll(t => t.Site == siteId);
        company.Purchases.RemoveAll(t => t.Building == siteId);
        RemoveBuilding(site);
        company.Gold += refund;
        company.Touch();
        Changes.Companies.Add(company.Id);
        WakeCoupling();
        return CommandResult.Ok;
    }

    /// <summary>For tests and the dev cheat: goods put into a company's Storages; returns how much fitted</summary>
    public double Stock(long companyId, string resource, double amount) => PutInStorages(companyId, resource, amount);

    /// <summary>
    /// The wants in a company's station it could help with (a building's or a pipe's, with its target as OpenWants gives
    /// it): what its Storages hold of them, and whether it makes them
    /// </summary>
    public List<(Company Owner, long Target, string Name, CompanyAsk Want, double Holds, bool Makes)> WantsNear(Company me)
    {
        var found = new List<(Company, long, string, CompanyAsk, double, bool)>();
        if (EstateOf(me) is not { } estate)
        {
            return found;
        }
        var materials = Materials(me.Id);
        foreach (var owner in StationPlayers(estate.Station).Where(t => t.Id != me.Id))
        {
            foreach (var (target, name, ask) in OpenWants(owner))
            {
                var holds = materials.GetValueOrDefault(ask.Resource);
                var makes = BuildingsOf(me.Id).Any(b => !b.UnderConstruction && Content.Building(b.Type).Out.ContainsKey(ask.Resource));
                if (holds >= 1 || makes)
                {
                    found.Add((owner, target, name, ask, holds, makes));
                }
            }
        }
        return found;
    }
}
