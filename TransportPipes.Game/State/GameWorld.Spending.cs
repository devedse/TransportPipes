using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;

namespace TransportPipes.Game.State;

public enum PurchaseKind
{
    Upgrade,
    Build,
}

/// <summary>A purchase its player may take back for an hour (after run 3, the design team's step 1)</summary>
public sealed class Purchase
{
    public long Id { get; set; }
    public PurchaseKind Kind { get; set; }
    public long Building { get; set; }
    public int Levels { get; set; }
    public double Cost { get; set; }
    public DateTime At { get; set; }

    /// <summary>What it adds a minute (measured the first time it is asked; −1: not yet): what it earned since comes off the refund</summary>
    public double Gain { get; set; } = -1;
}

/// <summary>What a purchase will do to the company's income: the change a minute, and one sentence that says it and why</summary>
public sealed record SpendPreview(double Gain, string Text);

/// <summary>
/// Spending is safe (after run 3, the design team's step 1): a preview from the flow solver before every purchase, and
/// an hour's Undo (run 3: the Lab's Max took Cleo from 783 to 285 g/min, and Dev called a 329k upgrade "a trap", so
/// players learned to hoard)
/// </summary>
public sealed partial class GameWorld
{
    public static readonly TimeSpan UndoFor = TimeSpan.FromHours(1);

    /// <summary>
    /// What upgrading a building by some levels does to its owner's income, solved like the network will run:
    /// "+105 g/min", "adds nothing: its pipes carry 100 coal a minute", "−498 g/min: it eats more of the iron ingots you sell"
    /// (after run 4: "+0 g/min" beside "+41 would make nothing more" read as two incomes; Ben: "does it even earn?")
    /// </summary>
    public SpendPreview UpgradePreview(Company company, Building building, int levels)
    {
        if (levels <= 0 || building.UnderConstruction)
        {
            return new SpendPreview(0, "");
        }
        // A trading post: every level raises every Hub Order by 10% (1.4 §8)
        if (Content.Building(building.Type).Kind == BuildingKind.TradingPost)
        {
            var more = PostLevelGain(company, BoughtLevel(building) + levels - building.Level) - PostLevelGain(company, BoughtLevel(building) - building.Level);
            return more > 0.05
                ? new SpendPreview(more, $"+{Gold(more)}/min")
                : new SpendPreview(0, "adds nothing: the market pays the full price for all you sell already");
        }
        if (NetworkOf(building.Id) is not { } network || ToFlowNode(building) is not { Kind: FlowNodeKind.Building } node)
        {
            return new SpendPreview(0, "");
        }
        var up = ToFlowNode(building, BoughtLevel(building) + levels);
        var gain = Estimated(network)
            ? EstimateChange(network, company, node, up)
            : WhatIfGold(network, company.Id, t => t.Id == building.Id && up != null ? WithPumps(network, [up])[0] : t) - WhatIfGold(network, company.Id);
        // A small gain is still a gain (after run 4: a Solar Panel's level 2 sold half an energy more a minute, and the
        // panel said "adds nothing: nothing on its pipes takes more energy" under Loady's "upgrade it")
        if (gain > 0.05)
        {
            return new SpendPreview(gain, $"{(Estimated(network) ? "about " : "")}+{Gold(gain)}/min");
        }
        if (gain < -0.5)
        {
            var eats = node.In.Keys.Where(t => t != "energy").Select(GoodsWord).ToList();
            return new SpendPreview(gain, $"−{Gold(-gain)}/min: it eats more of {(eats.Count == 0 ? "the energy" : $"the {string.Join(" and ", eats)}")} you sell now");
        }
        // Nothing more: what holds it back at that level
        var factor = Content.Progression.OutputMultiplier(BoughtLevel(building) + levels) / Content.Progression.OutputMultiplier(building.Level);
        var (resource, have, need, output) = LevelReach(network, NetworkNode(network, building) ?? node).Limit(factor);
        var good = resource is { } r ? GoodsWord(r) : "goods";
        var why = resource == null ? ""
            : output && IdleReasonOf(building).Cause == IdleCause.PipeFull ? $": its pipes carry {Amount(have)} {good} a minute"
            : output ? $": nothing on its pipes takes more {good}"
            : $": it gets only {Amount(have)} of the {Amount(need)} {good} it would need";
        return new SpendPreview(0, $"adds nothing{why}");
    }

    private static FlowNode? NetworkNode(Network network, Building building) => network.Nodes.FirstOrDefault(t => t.Id == building.Id);

    /// <summary>Remembers a purchase for an hour's Undo, one per building: levels bought one after another make one</summary>
    private void RecordPurchase(Company company, PurchaseKind kind, Building building, int levels, double cost)
    {
        company.Purchases.RemoveAll(t => LastTick - t.At > UndoFor);
        if (kind == PurchaseKind.Upgrade && company.Purchases.LastOrDefault(t => t.Building == building.Id) is { } last)
        {
            if (last.Kind == PurchaseKind.Upgrade)
            {
                last.Levels += levels;
            }
            last.Cost += cost;
            last.Gain = -1;
            last.At = LastTick;
            company.Touch();
            return;
        }
        company.Purchases.RemoveAll(t => t.Building == building.Id);
        company.Purchases.Add(new Purchase
        {
            Id = company.Purchases.Select(t => t.Id).DefaultIfEmpty(0).Max() + 1,
            Kind = kind, Building = building.Id, Levels = levels, Cost = cost, At = LastTick,
        });
        company.Touch();
    }

    /// <summary>
    /// What a purchase adds a minute now, measured once: an upgrade against the building without its levels, a new
    /// building by what it sells and trades away
    /// </summary>
    private double PurchaseGain(Company company, Building building, Purchase purchase)
    {
        if (purchase.Gain >= 0)
        {
            return purchase.Gain;
        }
        var gain = 0.0;
        if (NetworkOf(building.Id) is { } network && !building.UnderConstruction)
        {
            if (purchase.Kind == PurchaseKind.Upgrade && ToFlowNode(building, Math.Max(1, building.Level - purchase.Levels)) is { } before)
            {
                gain = Estimated(network)
                    ? -EstimateChange(network, company, ToFlowNode(building), before)
                    : WhatIfGold(network, company.Id) - WhatIfGold(network, company.Id, t => t.Id == building.Id ? WithPumps(network, [before])[0] : t);
            }
            else if (purchase.Kind == PurchaseKind.Build)
            {
                gain = network.Solution.Flows.Where(t => t.From == building.Id && t.Kind is FlowKind.Sale or FlowKind.Trade).Sum(t => t.Delivered * t.PricePerUnit);
            }
        }
        // A new building earns only once it is built: measured then
        if (!building.UnderConstruction)
        {
            purchase.Gain = Math.Max(0, gain);
        }
        return Math.Max(0, gain);
    }

    /// <summary>The purchase of a building its owner can still take back, and what Undo returns (null: none)</summary>
    public (Purchase Purchase, double Refund)? Undoable(Company company, Building building) =>
        company.Purchases.LastOrDefault(t => t.Building == building.Id && LastTick - t.At <= UndoFor) is { } purchase
            ? (purchase, Math.Round(Math.Max(0, purchase.Cost - PurchaseGain(company, building, purchase) * (LastTick - purchase.At).TotalMinutes)))
            : null;

    /// <summary>Takes a purchase back within the hour: the levels or the building go, the price less what it earned comes back</summary>
    public CommandResult Undo(long companyId, long buildingId)
    {
        if (Company(companyId) is not { } company || Building(buildingId) is not { } building || building.Owner != companyId || Undoable(company, building) is not { } undo)
        {
            return CommandResult.Fail("Nothing to undo here any more: Undo lasts an hour.");
        }
        if (IsSite(building))
        {
            return CancelSite(companyId, buildingId);
        }
        var (purchase, refund) = undo;
        if (purchase.Kind == PurchaseKind.Upgrade)
        {
            var back = BoughtLevel(building) - purchase.Levels;
            if (back < 1)
            {
                return CommandResult.Fail("Nothing to undo here any more.");
            }
            // The steps whose goods are still coming go too, their goods back where they came from
            DropLevelSteps(company, building, back);
            building.Level = Math.Min(building.Level, back);
            building.Touch();
            MarkDirty(building);
            foreach (var tile in building.Footprint())
            {
                Changes.Tiles.Add(tile);
            }
        }
        else
        {
            RemoveBuilding(building);
        }
        company.Purchases.Remove(purchase);
        company.Gold += refund;
        // Taken back: it counts as unspent (the playtest's measure)
        AddStat(company, "goldSpent", -refund);
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }
}
