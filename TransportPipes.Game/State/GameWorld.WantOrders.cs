namespace TransportPipes.Game.State;

/// <summary>
/// "Order the rest" (after run 3, the design team's step 4, docs/playtest-run3-design.html): gold finishes a want after
/// the friends have had their turn. It is paid up front at 2 × base (the Ask's House price; the gold leaves the game);
/// the hub starts once every friend in the station who played in the last three days and makes or holds the good has had
/// a session since the want was raised (at most 72 hours, none when nobody near makes it), then ships evenly over six
/// hours; what friends bring meanwhile comes off it, its gold back to whoever paid. Price can't protect a friend's goods
/// against a 39.8M bank; their turn and their pace can (a Brickworks makes 12 bricks a minute, an order ships 100 at 0.3).
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>The "who brought it" key of goods the hub shipped for an order</summary>
    public const long HubSource = 0;

    public const double OrderPrice = 2.0;
    public static readonly TimeSpan OrderWindowMax = TimeSpan.FromHours(72);
    public const double OrderHours = 6;

    /// <summary>A want's parts by its target: a building's id, or minus a pipe want's number</summary>
    private (Company Owner, List<CompanyAsk> Parts)? WantByTarget(Company near, long target)
    {
        if (target > 0)
        {
            return Building(target) is { } site && IsSite(site) && Company(site.Owner) is { } owner ? (owner, WantsOf(site)) : null;
        }
        var owners = EstateOf(near) is { } estate ? StationPlayers(estate.Station) : [near];
        foreach (var owner in owners.Append(near).DistinctBy(t => t.Id))
        {
            var parts = owner.Asks.Where(t => t.PipeTiles != null && t.WantId == -target).ToList();
            if (parts.Count > 0)
            {
                return (owner, parts);
            }
        }
        return null;
    }

    /// <summary>What ordering the rest of a want costs now: what it still lacks at 2 × base (null: nothing to order)</summary>
    public double? OrderCost(Company payer, long target) =>
        WantByTarget(payer, target) is { } want && want.Parts.Where(t => t.Order == null && t.Left > 1e-6).ToList() is { Count: > 0 } open
            ? Math.Ceiling(open.Sum(t => t.Left * BasePrice(t.Resource) * OrderPrice))
            : null;

    /// <summary>Orders the rest of a want, its owner's or a station neighbour's ("Order the rest for Ana ▸")</summary>
    public CommandResult OrderRest(long payerId, long target)
    {
        if (Company(payerId) is not { } payer || WantByTarget(payer, target) is not { } want)
        {
            return CommandResult.Fail("That has what it needs.");
        }
        var owner = want.Owner;
        if (owner.Id != payer.Id)
        {
            if (EstateOf(payer) is not { } estate || !StationPlayers(estate.Station).Any(t => t.Id == owner.Id))
            {
                return CommandResult.Fail("Only your station's neighbours can order for it.");
            }
            if (SameDevice(payer, owner))
            {
                return CommandResult.Fail("Not between two accounts on one device.");
            }
        }
        var open = want.Parts.Where(t => t.Order == null && t.Left > 1e-6).ToList();
        if (open.Count == 0)
        {
            return CommandResult.Fail("It's ordered already.");
        }
        var cost = Math.Ceiling(open.Sum(t => t.Left * BasePrice(t.Resource) * OrderPrice));
        if (payer.Gold < cost)
        {
            return CommandResult.Fail($"The rest costs {cost:N0} gold.");
        }
        foreach (var ask in open)
        {
            var price = BasePrice(ask.Resource) * OrderPrice;
            var escrow = ask.Left * price;
            payer.Gold -= escrow;
            payer.Report.GoldSpent += escrow;
            AddStat(payer, "ordered", escrow);
            ask.Order = new WantOrder { By = payer.Id, Escrow = escrow, Price = price, At = LastTick };
        }
        payer.Touch();
        owner.Touch();
        Changes.Companies.Add(payer.Id);
        Changes.Companies.Add(owner.Id);
        if (owner.Id != payer.Id)
        {
            Changes.Events.Add(new WorldEvent(owner.Id, WorldEventKind.Neighbour,
                $"{NameFor(payer, owner)} ordered the rest of what your {WantName(open[0])} wants: the hub brings it {OrderStartText(owner, open[0], owner)}"));
        }
        return CommandResult.Ok;
    }

    private string WantName(CompanyAsk ask) =>
        ask.Site is { } id && Building(id) is { } b ? Content.Building(b.Type).Name : ask.PipeTiles != null ? Content.PipeTier(ask.PipeTier).Name : "want";

    /// <summary>
    /// The friends an order waits for: in the owner's station, played in the last three days, make or hold the good,
    /// and haven't had a session since the want was raised; and the latest the hub starts anyway
    /// </summary>
    public (List<Company> Awaited, DateTime Latest) OrderWindow(Company owner, CompanyAsk ask)
    {
        var latest = (ask.Order?.At ?? LastTick) + OrderWindowMax;
        if (EstateOf(owner) is not { } estate)
        {
            return ([], latest);
        }
        var awaited = StationPlayers(estate.Station)
            .Where(t => t.Id != owner.Id && t.Id != ask.Order?.By && IsActive(t) && t.LastSeenAt <= ask.Raised
                && (Materials(t.Id).GetValueOrDefault(ask.Resource) >= 1 || BuildingsOf(t.Id).Any(b => !b.UnderConstruction && Content.Building(b.Type).Out.ContainsKey(ask.Resource))))
            .ToList();
        return (awaited, latest);
    }

    /// <summary>"from Cleo's next session, at the latest Thu 20:00", "over the next 6 hours", "at 0.3 a minute"</summary>
    public string OrderStartText(Company owner, CompanyAsk ask, Company viewer)
    {
        if (ask.Order is { Started: not null } order)
        {
            return $"at {order.Rate:0.##} a minute";
        }
        var (awaited, latest) = OrderWindow(owner, ask);
        return awaited.Count == 0
            ? $"over the next {OrderHours:0} hours"
            : $"from {string.Join(" and ", awaited.Select(t => NameFor(t, viewer)))}'s next session, at the latest {latest:ddd HH:mm}";
    }

    /// <summary>An order's turn: once the friends' window has passed it starts, then ships its share of the six hours (what friends brought comes off it)</summary>
    private void ShipOrder(Company owner, CompanyAsk ask, double minutes)
    {
        if (ask.Order is not { } order || ask.Left <= 1e-6)
        {
            return;
        }
        if (order.Started == null)
        {
            var (awaited, latest) = OrderWindow(owner, ask);
            if (awaited.Count > 0 && LastTick < latest)
            {
                return;
            }
            order.Started = LastTick;
            order.Rate = ask.Left / (OrderHours * 60);
            owner.Touch();
            return;
        }
        var units = Math.Min(order.Rate * minutes, ask.Left);
        if (units <= 1e-9)
        {
            return;
        }
        ask.Got += units;
        order.Shipped += units;
        ask.From[HubSource] = ask.From.GetValueOrDefault(HubSource) + units;
        owner.Touch();
    }

    /// <summary>An order ends (its want is filled, or given up): what it didn't ship goes back to whoever paid</summary>
    private void CloseOrder(CompanyAsk ask)
    {
        if (ask.Order is not { } order)
        {
            return;
        }
        var refund = Math.Max(0, order.Escrow - order.Shipped * order.Price);
        if (Company(order.By) is { } payer && refund > 1e-6)
        {
            payer.Gold += refund;
            payer.Touch();
            Changes.Companies.Add(payer.Id);
        }
        ask.Order = null;
    }
}
