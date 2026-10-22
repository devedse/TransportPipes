using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;

namespace TransportPipes.Game.State;

/// <summary>
/// What the Max button buys (F8): the levels a building's own pipes, and what its gate brings now, keep at 90% or more,
/// as far as the gold goes; and what the levels beyond would do
/// </summary>
/// <param name="Levels">What Max buys: the useful levels the company can afford</param>
/// <param name="Cost">Their price</param>
/// <param name="AffordableLevels">All the levels the company can afford</param>
/// <param name="AffordableCost">Their price</param>
/// <param name="AffordableShare">How fast the building would run at that level</param>
/// <param name="Resource">What holds it back beyond the useful levels (an input it can't get more of, or its output); null: nothing</param>
/// <param name="Have">Per minute: what its pipes bring of that input (or take of that output)</param>
/// <param name="Need">Per minute: what it would need of it at the affordable level</param>
/// <param name="Output">Whether what holds it back is its output (its trading post sells all it can)</param>
public sealed record MaxPlan(int Levels, double Cost, int AffordableLevels, double AffordableCost, double AffordableShare, string? Resource, double Have, double Need, bool Output);

public sealed partial class GameWorld
{
    /// <summary>A level counts as useful while the building would run at least this fast</summary>
    public const double UsefulShare = 0.9;

    /// <summary>What the Max button buys for a building now, and why it stops there</summary>
    public MaxPlan SmartMax(Company company, Building building)
    {
        var affordable = AffordableLevels(company, building);
        var useful = affordable;
        var (share, resource, have, need, output) = (1.0, (string?)null, 0.0, 0.0, false);
        var network = NetworkOf(building.Id);
        if (affordable > 0 && (network?.Nodes.FirstOrDefault(t => t.Id == building.Id) ?? ToFlowNode(building)) is { Kind: FlowNodeKind.Building } node && (node.In.Count > 0 || node.Out.Count > 0))
        {
            // Not on any pipes: it can't run faster at any level
            var reach = network == null ? new Reach(node, [], []) : LevelReach(network, node);
            double ShareAt(int levels) => reach.Share(Content.Progression.OutputMultiplier(building.Level + levels) / Content.Progression.OutputMultiplier(building.Level));
            useful = 0;
            while (useful < affordable && ShareAt(useful + 1) >= UsefulShare)
            {
                useful++;
            }
            if (useful < affordable)
            {
                var factor = Content.Progression.OutputMultiplier(building.Level + affordable) / Content.Progression.OutputMultiplier(building.Level);
                share = reach.Share(factor);
                (resource, have, need, output) = reach.Limit(factor);
            }
        }
        return new MaxPlan(useful, useful > 0 ? UpgradeCost(building, useful) : 0, affordable, affordable > 0 ? UpgradeCost(building, affordable) : 0, share, resource, have, need, output);
    }

    /// <summary>What a building's pipes can bring it of each input, and take of each output, without anything changing elsewhere</summary>
    private sealed class Reach(FlowNode node, Dictionary<string, double> inputs, Dictionary<string, double> outputs)
    {
        /// <summary>How fast it would run with its recipe times this factor (1: as it is now)</summary>
        public double Share(double factor)
        {
            var share = 1.0;
            foreach (var (resource, amount) in node.In)
            {
                share = Math.Min(share, inputs.GetValueOrDefault(resource) / Math.Max(1e-12, amount * node.MaxRate * factor));
            }
            foreach (var (resource, amount) in node.Out)
            {
                share = Math.Min(share, outputs.GetValueOrDefault(resource) / Math.Max(1e-12, amount * node.MaxRate * factor));
            }
            return Math.Min(1, share);
        }

        /// <summary>How fast its inputs alone would let it run (1: all it needs comes or could come)</summary>
        public double InputShare()
        {
            var share = 1.0;
            foreach (var (resource, amount) in node.In)
            {
                share = Math.Min(share, inputs.GetValueOrDefault(resource) / Math.Max(1e-12, amount * node.MaxRate));
            }
            return share;
        }

        /// <summary>What holds it back at that factor: the scarcest input or output, what its pipes manage of it and what it would need</summary>
        public (string? Resource, double Have, double Need, bool Output) Limit(double factor)
        {
            var limits = node.In.Select(t => (Resource: t.Key, Have: inputs.GetValueOrDefault(t.Key), Need: t.Value * node.MaxRate * factor, Output: false))
                .Concat(node.Out.Select(t => (Resource: t.Key, Have: outputs.GetValueOrDefault(t.Key), Need: t.Value * node.MaxRate * factor, Output: true)))
                .OrderBy(t => t.Have / Math.Max(1e-12, t.Need))
                .ToList();
            return limits.Count == 0 ? (null, 0, 0, false) : (limits[0].Resource, limits[0].Have, limits[0].Need, limits[0].Output);
        }
    }

    /// <summary>
    /// What a building's pipes reach (F8): per input, what it gets now (from its pipes and its gate) plus what its own
    /// suppliers on its pipes still make for others than buildings or could make more of, or without end from an own
    /// Storage holding it or a post buying it; per output, what its takers on its pipes use, what its own trading posts
    /// can still sell, and without end an own Storage with room
    /// </summary>
    private Reach LevelReach(Network network, FlowNode node)
    {
        var solution = network.Solution;
        var nodes = network.Nodes.ToDictionary(t => t.Id);
        var links = network.Links.Where(t => t.A == node.Id || t.B == node.Id).ToList();
        var inputs = new Dictionary<string, double>();
        foreach (var resource in node.In.Keys)
        {
            var potential = solution.Flows.Where(t => t.To == node.Id && t.Resource == resource).Sum(t => t.Delivered)
                + network.GateFlows.Where(t => t.Import && t.Building == node.Id && t.Resource == resource).Sum(t => t.Delivered);
            foreach (var link in links.Where(t => t.Allows(resource)))
            {
                if (!nodes.TryGetValue(link.A == node.Id ? link.B : link.A, out var other) || other.Owner != node.Owner)
                {
                    continue;
                }
                if ((other.Kind == FlowNodeKind.Storage && other.StorageContents.Contains(resource)) || (other.Kind == FlowNodeKind.TradingPost && other.Imports.Contains(resource)))
                {
                    potential = double.MaxValue;
                    break;
                }
                if (other.Kind == FlowNodeKind.Building && other.Out.TryGetValue(resource, out var makes))
                {
                    var toBuildings = solution.Flows.Where(t => t.From == other.Id && t.Resource == resource && t.Kind is FlowKind.Internal or FlowKind.Trade or FlowKind.Contract).Sum(t => t.Sent);
                    potential += Math.Max(0, makes * other.MaxRate - toBuildings) * link.Efficiency;
                }
            }
            inputs[resource] = potential;
        }
        var outputs = new Dictionary<string, double>();
        foreach (var resource in node.Out.Keys)
        {
            var room = network.GateFlows.Where(t => !t.Import && t.Building == node.Id && t.Resource == resource).Sum(t => t.Sent);
            foreach (var link in links.Where(t => t.Allows(resource)))
            {
                if (!nodes.TryGetValue(link.A == node.Id ? link.B : link.A, out var other))
                {
                    continue;
                }
                if (other.Owner == node.Owner && other.Kind == FlowNodeKind.Storage && other.StorageCanAccept)
                {
                    room = double.MaxValue;
                    break;
                }
                if (other.Owner == node.Owner && other.Kind == FlowNodeKind.TradingPost && _buildings.TryGetValue(other.Id, out var post))
                {
                    var sold = solution.Flows.Where(t => t.To == other.Id && t.Kind == FlowKind.Sale).Sum(t => t.Sent);
                    var mine = solution.Flows.Where(t => t.From == node.Id && t.To == other.Id && t.Kind == FlowKind.Sale).Sum(t => t.Sent);
                    room += Math.Max(0, TradingPostThroughput(post, Content.Building(post.Type)) - sold) + mine;
                }
                else if (other.Kind == FlowNodeKind.Building && other.In.TryGetValue(resource, out var uses))
                {
                    room += uses * other.MaxRate / Math.Max(1e-12, link.Efficiency);
                }
            }
            outputs[resource] = room;
        }
        return new Reach(node, inputs, outputs);
    }
}
