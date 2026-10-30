namespace TransportPipes.Game.State;

/// <summary>
/// "Buy the rest" (1.4 §9): gold fills what a want still lacks at once from the market, at the market's 2.5× base (the
/// gold leaves the game). For your own wants, or a neighbour's in your Yard or crew. Before it, a want takes its goods by
/// itself: your Storages, your own spare, the Yard at your neighbours' prices, the market if you allow imports.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>The "who brought it" key of goods the market brought for a want</summary>
    public const long HubSource = 0;

    /// <summary>A want's parts by its target: a building's id, or minus a pipe want's number</summary>
    private (Company Owner, List<CompanyAsk> Parts)? WantByTarget(Company near, long target)
    {
        if (target > 0)
        {
            return Building(target) is { } site && Wanting(site) && Company(site.Owner) is { } owner ? (owner, WantsOf(site)) : null;
        }
        foreach (var owner in NearPlayers(near, activeOnly: false))
        {
            var parts = owner.Asks.Where(t => t.PipeTiles != null && t.WantId == -target).ToList();
            if (parts.Count > 0)
            {
                return (owner, parts);
            }
        }
        return null;
    }

    /// <summary>What a good costs to buy for a want: the market's price</summary>
    public double WantMarketPrice(Company owner, string resource) => HubSellPrice(owner.District, resource);

    /// <summary>What buying the rest of a want costs now, of one good or all (null: nothing left)</summary>
    public double? OrderCost(Company payer, long target, string? resource = null) =>
        WantByTarget(payer, target) is { } want && want.Parts.Where(t => t.Left > 1e-6 && (resource == null || t.Resource == resource)).ToList() is { Count: > 0 } open
            ? Math.Ceiling(open.Sum(t => t.Left * WantMarketPrice(want.Owner, t.Resource)))
            : null;

    /// <summary>
    /// Buys the rest of a want, its owner's or a neighbour's ("Buy the rest for Ana ▸"): of one good, or of all (playtest:
    /// the planks' "Buy the rest · 302 g" bought the bricks as well, for 609 g)
    /// </summary>
    public CommandResult OrderRest(long payerId, long target, string? resource = null)
    {
        if (Company(payerId) is not { } payer || WantByTarget(payer, target) is not { } want)
        {
            return CommandResult.Fail("That has what it needs.");
        }
        var owner = want.Owner;
        if (owner.Id != payer.Id)
        {
            // Your Yard or your crew (after run 4: friends who live elsewhere could only watch)
            if (!NearPlayers(payer, activeOnly: false).Any(t => t.Id == owner.Id))
            {
                return CommandResult.Fail("Only your Yard's neighbours can buy for it.");
            }
            if (SameDevice(payer, owner))
            {
                return CommandResult.Fail("Not between two accounts on one device.");
            }
        }
        var open = want.Parts.Where(t => t.Left > 1e-6 && (resource == null || t.Resource == resource)).ToList();
        if (open.Count == 0)
        {
            return CommandResult.Fail("It has what it needs.");
        }
        var cost = Math.Ceiling(open.Sum(t => t.Left * WantMarketPrice(owner, t.Resource)));
        if (payer.Gold < cost)
        {
            return CommandResult.Fail($"The rest costs {cost:N0} gold.");
        }
        foreach (var ask in open)
        {
            var gold = ask.Left * WantMarketPrice(owner, ask.Resource);
            payer.Gold -= gold;
            payer.Report.GoldSpent += gold;
            AddStat(payer, "goldSpent", gold);
            AddStat(payer, "ordered", gold);
            if (payer.Id == owner.Id)
            {
                ask.Paid += gold;
            }
            ask.From[HubSource] = ask.From.GetValueOrDefault(HubSource) + ask.Left;
            ask.Got = ask.Amount;
        }
        AddStat(payer, owner.Id == payer.Id ? "pressed:orderOwn" : "pressed:orderFriend", 1);
        var name = WantName(open[0]);
        if (open[0].Site is { } id && Building(id) is { } site)
        {
            FinishWantsIfFull(owner, site);
            StepUpIfFull(owner, site);
        }
        else
        {
            foreach (var ask in open)
            {
                FinishPipeWantIfFull(owner, ask);
            }
        }
        payer.Touch();
        owner.Touch();
        Changes.Companies.Add(payer.Id);
        Changes.Companies.Add(owner.Id);
        if (owner.Id != payer.Id)
        {
            Changes.Events.Add(new WorldEvent(owner.Id, WorldEventKind.Neighbour, $"{NameFor(payer, owner)} bought the rest of what your {name} wanted 💛"));
            return CommandResult.Done($"💛 Bought for {NameFor(owner, payer)}'s {name}");
        }
        return CommandResult.Done($"Bought: your {name} has its goods");
    }

    private string WantName(CompanyAsk ask) =>
        ask.Site is { } id && Building(id) is { } b ? Content.Building(b.Type).Name : ask.PipeTiles != null ? Content.PipeTier(ask.PipeTier).Name : "want";
}
