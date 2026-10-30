using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;

namespace TransportPipes.Game.State;

/// <summary>
/// What the Max button buys (F8): the levels a building's own pipes, and what its gate brings now, keep at 90% or more,
/// as far as the gold goes (a trading post: the levels that still raise what the market pays); and what the levels
/// beyond would do
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
        var from = BoughtLevel(building);
        var (share, resource, have, need, output) = (1.0, (string?)null, 0.0, 0.0, false);
        var network = NetworkOf(building.Id);
        if (affordable > 0 && (network?.Nodes.FirstOrDefault(t => t.Id == building.Id) ?? ToFlowNode(building)) is { Kind: FlowNodeKind.Building } node && (node.In.Count > 0 || node.Out.Count > 0))
        {
            // Not on any pipes: it can't run faster at any level
            var reach = network == null ? new Reach(node, [], []) : LevelReach(network, node);
            double ShareAt(int levels) => reach.Share(Content.Progression.OutputMultiplier(from + levels) / Content.Progression.OutputMultiplier(building.Level));
            useful = 0;
            while (useful < affordable && ShareAt(useful + 1) >= UsefulShare)
            {
                useful++;
            }
            if (useful < affordable)
            {
                var factor = Content.Progression.OutputMultiplier(from + affordable) / Content.Progression.OutputMultiplier(building.Level);
                share = reach.Share(factor);
                (resource, have, need, output) = reach.Limit(factor);
            }
        }
        else if (affordable > 0 && Content.Building(building.Type) is { Kind: BuildingKind.TradingPost })
        {
            // A trading post: the levels that still raise what the market pays (all its sales within the Hub Orders: none)
            var pending = from - building.Level;
            useful = 0;
            while (useful < affordable && PostLevelGain(company, pending + useful + 1) - PostLevelGain(company, pending + useful) > 0.01)
            {
                useful++;
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
    /// <param name="changeLink">What a link would carry instead (a better or wider pipe on it: the pipe door's levels)</param>
    private Reach LevelReach(Network network, FlowNode node, Func<FlowLink, FlowLink>? changeLink = null)
    {
        var index = network.Index;
        var nodes = index.Nodes;
        var links = index.LinksOf(node.Id).Select(changeLink ?? (t => t)).ToList();
        var inputs = new Dictionary<string, double>();
        foreach (var resource in node.In.Keys)
        {
            var potential = index.FlowsTo(node.Id).Where(t => t.Resource == resource).Sum(t => t.Delivered)
                + index.GateFlowsOf(node.Id).Where(t => t.Import && t.Resource == resource).Sum(t => t.Delivered);
            foreach (var link in links.Where(t => t.Allows(resource)))
            {
                if (!nodes.TryGetValue(link.A == node.Id ? link.B : link.A, out var other) || other.Owner != node.Owner)
                {
                    continue;
                }
                // A connection carries at most its pipe's throughput (run 3: a grey pipe's 100 a minute capped Cleo's Coal Pit)
                var onLink = index.FlowsFrom(other.Id).Where(t => t.To == node.Id && t.Resource == resource).Sum(t => t.Sent);
                var linkLeft = Math.Max(0, link.Capacity - onLink) * link.Efficiency;
                if ((other.Kind == FlowNodeKind.Storage && other.StorageContents.Contains(resource)) || (other.Kind == FlowNodeKind.TradingPost && other.Imports.Contains(resource)))
                {
                    potential += linkLeft;
                    continue;
                }
                if (other.Kind == FlowNodeKind.Building && other.Out.TryGetValue(resource, out var makes))
                {
                    var toBuildings = index.FlowsFrom(other.Id).Where(t => t.Resource == resource && t.Kind is FlowKind.Internal or FlowKind.Trade).Sum(t => t.Sent);
                    potential += Math.Min(Math.Max(0, makes * other.MaxRate - toBuildings) * link.Efficiency, linkLeft);
                }
            }
            inputs[resource] = potential;
        }
        var outputs = new Dictionary<string, double>();
        foreach (var resource in node.Out.Keys)
        {
            var room = index.GateFlowsOf(node.Id).Where(t => !t.Import && t.Resource == resource).Sum(t => t.Sent);
            foreach (var link in links.Where(t => t.Allows(resource)))
            {
                if (!nodes.TryGetValue(link.A == node.Id ? link.B : link.A, out var other))
                {
                    continue;
                }
                if (other.Owner == node.Owner && other.Kind == FlowNodeKind.Storage && other.StorageCanAccept)
                {
                    room += link.Capacity;
                    continue;
                }
                if (other.Owner == node.Owner && other.Kind == FlowNodeKind.TradingPost && _buildings.TryGetValue(other.Id, out var post))
                {
                    // A trading post takes all it is brought (1.4 §8): as much as the pipe carries
                    room += link.Capacity;
                }
                else if (other.Kind == FlowNodeKind.Building && other.In.TryGetValue(resource, out var uses))
                {
                    room += Math.Min(uses * other.MaxRate / Math.Max(1e-12, link.Efficiency), link.Capacity);
                }
            }
            outputs[resource] = room;
        }
        return new Reach(node, inputs, outputs);
    }
}
