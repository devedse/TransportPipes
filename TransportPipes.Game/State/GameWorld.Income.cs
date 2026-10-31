using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;

namespace TransportPipes.Game.State;

/// <summary>
/// One line of a company's gold a minute: what (hub: sold at the hub; storage: a Storage selling what it holds above its
/// line; player / fromPlayer: sold to or bought from a player over a pipe; import: a trading post buying from the hub;
/// yardSold / yardOnItsWay / yardBought: sold through the Yard, matched and on its way, bought through it; yardFloor:
/// what a neighbour didn't take of what the Yard sent, paid at your hub price), the good, with whom, how much a minute
/// and the gold a minute (paid out: below zero)
/// </summary>
public sealed record IncomeLine(string Kind, string Resource, string? Who, double Units, double Gold);

public sealed partial class GameWorld
{
    /// <summary>
    /// Where a company's gold a minute comes from (playtest: "add a very detailed explanation what makes this total gold
    /// come to be ... we keep seeing it change all the time and I don't understand why"): the very terms the top bar adds
    /// up, its networks' sales, trades and imports and the Yard's pay, line by line; they add up to GoldPerMinute exactly
    /// </summary>
    public List<IncomeLine> IncomeBreakdown(Company company)
    {
        SolveNetworks();
        var lines = new Dictionary<(string Kind, string Resource, string? Who), (double Units, double Gold)>();
        void Add(string kind, string resource, Company? who, double units, double gold)
        {
            var key = (kind, resource, who == null ? null : NameFor(who, company));
            var (u, g) = lines.GetValueOrDefault(key);
            lines[key] = (u + units, g + gold);
        }
        foreach (var network in _networks.Values)
        {
            foreach (var flow in network.Solution.Flows)
            {
                var from = _buildings.GetValueOrDefault(flow.From);
                var to = _buildings.GetValueOrDefault(flow.To);
                switch (flow.Kind)
                {
                    case FlowKind.Sale when from?.Owner == company.Id:
                        Add(Content.Building(from.Type).Kind == BuildingKind.Storage ? "storage" : "hub", flow.Resource, null, flow.Delivered, flow.Delivered * flow.PricePerUnit);
                        break;
                    case FlowKind.Trade when from?.Owner == company.Id:
                        Add("player", flow.Resource, to == null ? null : Company(to.Owner), flow.Delivered, flow.Delivered * flow.PricePerUnit);
                        break;
                    case FlowKind.Trade when to?.Owner == company.Id:
                        Add("fromPlayer", flow.Resource, from == null ? null : Company(from.Owner), flow.Delivered, -flow.Delivered * flow.PricePerUnit);
                        break;
                    case FlowKind.Import when to?.Owner == company.Id:
                        Add("import", flow.Resource, null, flow.Sent, -flow.Sent * flow.PricePerUnit);
                        break;
                }
            }
            // What the Yard brings: its buyer pays for what leaves the Yard (SplitGateFlows)
            foreach (var flow in network.GateFlows.Where(t => t.Import))
            {
                if (_buildings.TryGetValue(flow.Building, out var building) && building.Owner == company.Id)
                {
                    Add("yardBought", flow.Resource, null, flow.Sent, -flow.Sent * flow.Price);
                }
            }
        }
        foreach (var entry in TradeIncomeEntries().Where(t => t.Seller.Id == company.Id))
        {
            Add(entry.Floor ? "yardFloor" : entry.OnItsWay ? "yardOnItsWay" : "yardSold", entry.Resource, entry.Floor ? null : entry.Buyer, entry.Units, entry.Gold);
        }
        return lines.Where(t => Math.Abs(t.Value.Gold) > 1e-6 || t.Value.Units > 1e-6)
            .Select(t => new IncomeLine(t.Key.Kind, t.Key.Resource, t.Key.Who, t.Value.Units, t.Value.Gold))
            .OrderByDescending(t => t.Gold)
            .ToList();
    }
}
