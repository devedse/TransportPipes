using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

public sealed partial class GameWorld
{
    /// <summary>The highest toll a Toll Meter may ask (GDD §14.1)</summary>
    public const double MaxToll = 0.05;

    /// <summary>Toll income per minute per Toll Meter, from the last solutions</summary>
    private readonly Dictionary<long, double> _tollIncome = new();

    public double TollIncome(long meterId) => _tollIncome.TryGetValue(meterId, out var v) ? v : 0;


    /// <summary>Corporations come later; until then nobody shares one</summary>
    public bool SameCorporation(long a, long b) => a == b;

    /// <summary>
    /// Toll Meters on public pipes: every flow through a meter of another company pays its toll on the value of what it
    /// carries; half goes to the meter's owner, half is destroyed (a gold sink).
    /// </summary>
    private FlowSolution WithTolls(Network network, FlowSolution solution)
    {
        var meters = network.Conductors
            .Select(t => _buildingAt.TryGetValue(t, out var id) ? _buildings[id] : null)
            .Where(t => t is { UnderConstruction: false } && Content.Building(t.Type).Kind == BuildingKind.TollMeter && t.Setting > 0)
            .ToDictionary(t => t!.Position, t => t!);
        foreach (var meter in meters.Values)
        {
            _tollIncome.Remove(meter.Id);
        }
        if (meters.Count == 0 || solution.Flows.Count == 0)
        {
            return solution;
        }

        var gold = solution.GoldPerMinute.ToDictionary(t => t.Key, t => t.Value);
        void AddGold(long company, double amount) => gold[company] = (gold.TryGetValue(company, out var v) ? v : 0) + amount;

        foreach (var flow in solution.Flows)
        {
            var link = network.Links
                .Where(t => t.Connects(flow.From, flow.To) && t.Allows(flow.Resource))
                .OrderByDescending(t => t.Priority).ThenByDescending(t => t.Efficiency)
                .FirstOrDefault();
            if (link == null)
            {
                continue;
            }
            var payer = _buildings[flow.From].Owner;
            var value = flow.Delivered * (flow.PricePerUnit > 0 ? flow.PricePerUnit : HubBuyPrice(network.District, flow.Resource));
            foreach (var tile in link.Path)
            {
                if (meters.TryGetValue(tile, out var meter) && meter.Owner != payer)
                {
                    var fee = value * Math.Min(MaxToll, meter.Setting);
                    AddGold(payer, -fee);
                    AddGold(meter.Owner, fee / 2);
                    _tollIncome[meter.Id] = TollIncome(meter.Id) + fee / 2;
                }
            }
        }

        return new FlowSolution
        {
            Rates = solution.Rates,
            Flows = solution.Flows,
            GoldPerMinute = gold,
            ResearchPerMinute = solution.ResearchPerMinute,
            StorageNet = solution.StorageNet,
        };
    }

    /// <summary>A number the owner sets on a building: the toll of a Toll Meter (0..5%)</summary>
    public CommandResult SetBuildingSetting(long companyId, long buildingId, double value)
    {
        if (Building(buildingId) is not { } building || building.Owner != companyId)
        {
            return CommandResult.Fail("That is not your building.");
        }
        switch (Content.Building(building.Type).Kind)
        {
            case BuildingKind.TollMeter:
                building.Setting = Math.Round(Math.Clamp(value, 0, MaxToll), 4);
                break;
            default:
                return CommandResult.Fail("This building has no setting.");
        }
        building.Touch();
        MarkDirty(building);
        return CommandResult.Ok;
    }
}
