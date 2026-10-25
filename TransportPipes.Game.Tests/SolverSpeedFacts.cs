using System.Text.Json;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.World;

namespace TransportPipes.Game.Tests;

/// <summary>Networks that were slow to solve stay quick</summary>
[TestClass]
public class SolverSpeedFacts
{
    [TestMethod]
    public void ADenseFactoryNetworkSettlesInAFewRounds()
    {
        var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "dense-factory-network.json"))).RootElement;
        static Dictionary<string, double> Amounts(JsonElement node, string name) =>
            node.TryGetProperty(name, out var e) ? e.EnumerateObject().ToDictionary(t => t.Name, t => t.Value.GetDouble()) : [];
        var nodes = doc.GetProperty("nodes").EnumerateArray().Select(n => new FlowNode
        {
            Id = n.GetProperty("id").GetInt64(),
            Owner = 1,
            Kind = n.TryGetProperty("kind", out var kind) ? Enum.Parse<FlowNodeKind>(kind.GetString()!) : FlowNodeKind.Building,
            In = Amounts(n, "in"),
            Out = Amounts(n, "out"),
        }).ToList();
        var links = doc.GetProperty("links").EnumerateArray()
            .Select(l => new FlowLink(l[0].GetInt64(), l[1].GetInt64(), l[2].GetDouble(), l[3].GetDouble(), [new TilePos(0, 0)]))
            .ToList();
        var prices = doc.GetProperty("prices").EnumerateObject().ToDictionary(t => t.Name, t => t.Value.GetDouble());
        var market = new FlowMarket
        {
            HubBuyPrice = r => prices[r],
            HubSellPrice = r => prices[r] * 1.2,
            SellerPrice = (_, r) => prices[r] * 1.5,
            BuyerMaxPrice = (_, r) => prices[r] * 2,
        };

        var solution = FlowSolver.Solve(nodes, links, market);

        Assert.IsLessThan(60, solution.Rounds, "it took 500 rounds (0.4 s) before");
        // 1.4 §8: the trading posts have no unit limit any more, so the network sells more than with the 500 rounds of before
        Assert.AreEqual(2085.881, solution.GoldPerMinute[1], 0.01, "the same result every time");
        // Nothing comes from nothing: every building gets what its rate needs
        foreach (var node in nodes.Where(t => t.Kind == FlowNodeKind.Building))
        {
            foreach (var (resource, amount) in node.In)
            {
                var received = solution.Flows.Where(f => f.To == node.Id && f.Resource == resource).Sum(f => f.Delivered);
                Assert.AreEqual(amount * solution.Rate(node.Id), received, 1e-4 * Math.Max(1, amount), $"#{node.Id} {resource}");
            }
        }
    }
}
