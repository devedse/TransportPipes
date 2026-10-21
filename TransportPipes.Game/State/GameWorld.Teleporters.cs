using TransportPipes.Game.Content;
using TransportPipes.Game.Simulation;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// Teleporters (GDD §11.4): a pair of stations of the same kind. The sending station is a consumer in its network (the
/// resource plus energy for the distance), the receiving station a producer in its own network delivering what was sent
/// at the last solve. Networks never merge. A sender keeps the receiver's id in <see cref="Building.Setting"/> and the
/// resource in <see cref="Building.Filter"/>.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>What a sender received at the last solve (per minute), delivered by its receiver</summary>
    private readonly Dictionary<long, double> _teleported = new();

    /// <summary>Energy per unit and tile: a Pneumatic Post spends more per tile, a Quantum Link reaches further</summary>
    public static double TeleportEnergyPerTile(string type) => type == "quantumLink" ? 0.0005 : 0.002;

    /// <summary>The share a Quantum Link destroys on the way</summary>
    public static double TeleportLoss(string type) => type == "quantumLink" ? 0.02 : 0;

    private bool IsTeleporter(Building building) => Content.Building(building.Type).Kind == BuildingKind.Teleporter;

    /// <summary>The station a sender sends to (null when it isn't linked or the receiver is gone)</summary>
    public Building? TeleportReceiver(Building sender)
    {
        if (!IsTeleporter(sender) || sender.Setting <= 0 || sender.Filter.Count != 1)
        {
            return null;
        }
        return Building((long)sender.Setting) is { } receiver && receiver.Owner == sender.Owner && receiver.Type == sender.Type ? receiver : null;
    }

    /// <summary>The station that sends to a receiver</summary>
    public Building? TeleportSender(Building receiver) =>
        IsTeleporter(receiver)
            ? _buildings.Values.FirstOrDefault(t => t.Owner == receiver.Owner && t.Type == receiver.Type && (long)t.Setting == receiver.Id && TeleportReceiver(t) == receiver)
            : null;

    public static double TeleportDistance(Building a, Building b)
    {
        var from = WorldLayout.Surface(a.Position);
        var to = WorldLayout.Surface(b.Position);
        return Math.Sqrt((double)(from.X - to.X) * (from.X - to.X) + (double)(from.Y - to.Y) * (from.Y - to.Y));
    }

    /// <summary>How much a station sends per minute at most (more with its level)</summary>
    public double TeleportThroughput(Building station) =>
        Content.Building(station.Type).Throughput * Content.Progression.OutputMultiplier(station.Level);

    /// <summary>Whether two stations can be linked: a Pneumatic Post stays in its district, a Quantum Link reaches anywhere</summary>
    private string? TeleportRangeProblem(Building sender, Building receiver)
    {
        if (Content.Building(sender.Type).Range == "district")
        {
            var a = WorldLayout.Surface(sender.Position);
            var b = WorldLayout.Surface(receiver.Position);
            if (WorldLayout.DistrictOf(a.X, a.Y) != WorldLayout.DistrictOf(b.X, b.Y))
            {
                return $"A {Content.Building(sender.Type).Name} only reaches stations in the same district.";
            }
        }
        return null;
    }

    /// <summary>What was sent by a sender at the last solve (per minute)</summary>
    public double Teleported(Building sender) => _teleported.GetValueOrDefault(sender.Id);

    /// <summary>Why a station can't send to another one (null when it can)</summary>
    public string? TeleportLinkProblem(Building sender, Building receiver)
    {
        if (receiver.UnderConstruction || sender.UnderConstruction)
        {
            return "Still being built";
        }
        if (TeleportRangeProblem(sender, receiver) is { } range)
        {
            return range;
        }
        if (TeleportReceiver(receiver) != null)
        {
            return "Sends already";
        }
        return TeleportSender(receiver) is { } other && other.Id != sender.Id ? "Receives from another station" : null;
    }

    /// <summary>Links a sending station to a receiving one for one resource</summary>
    public CommandResult LinkTeleporter(long companyId, long senderId, long receiverId, string resource)
    {
        if (Building(senderId) is not { } sender || sender.Owner != companyId || !IsTeleporter(sender)
            || Building(receiverId) is not { } receiver || receiver.Owner != companyId || !IsTeleporter(receiver))
        {
            return CommandResult.Fail("Link two of your own stations.");
        }
        if (sender.Id == receiver.Id || sender.Type != receiver.Type)
        {
            return CommandResult.Fail("Link two stations of the same kind.");
        }
        if (sender.UnderConstruction || receiver.UnderConstruction)
        {
            return CommandResult.Fail("Wait until both stations are built.");
        }
        if (!Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown resource.");
        }
        if (TeleportRangeProblem(sender, receiver) is { } problem)
        {
            return CommandResult.Fail(problem);
        }
        if (TeleportReceiver(receiver) != null)
        {
            return CommandResult.Fail("That station sends already; a station either sends or receives.");
        }
        if (TeleportSender(receiver) is { } other && other.Id != sender.Id)
        {
            return CommandResult.Fail("That station receives from another station already.");
        }
        if (TeleportSender(sender) != null)
        {
            return CommandResult.Fail("This station receives; a station either sends or receives.");
        }
        Unlink(sender);
        sender.Setting = receiver.Id;
        sender.Filter.Clear();
        sender.Filter.Add(resource);
        sender.Touch();
        Touched(sender, receiver);
        return CommandResult.Ok;
    }

    public CommandResult UnlinkTeleporter(long companyId, long stationId)
    {
        if (Building(stationId) is not { } station || station.Owner != companyId || !IsTeleporter(station))
        {
            return CommandResult.Fail("That is not your station.");
        }
        var sender = TeleportReceiver(station) != null ? station : TeleportSender(station);
        if (sender == null)
        {
            return CommandResult.Fail("This station isn't linked.");
        }
        Unlink(sender);
        return CommandResult.Ok;
    }

    private void Unlink(Building sender)
    {
        var receiver = TeleportReceiver(sender);
        sender.Setting = 0;
        sender.Filter.Clear();
        sender.Touch();
        _teleported.Remove(sender.Id);
        Touched(sender, receiver);
    }

    /// <summary>Both ends work differently now</summary>
    private void Touched(Building a, Building? b)
    {
        foreach (var station in new[] { a, b }.OfType<Building>())
        {
            MarkNeedsSolve(station.Id);
            foreach (var tile in station.Footprint())
            {
                Changes.Tiles.Add(tile);
            }
        }
    }

    /// <summary>The flow node of a station: a consumer when it sends, a producer when it receives, nothing when unlinked</summary>
    private FlowNode? TeleporterNode(Building station)
    {
        if (TeleportReceiver(station) is { } receiver)
        {
            var resource = station.Filter.First();
            var throughput = TeleportThroughput(station);
            var energy = TeleportEnergyPerTile(station.Type) * TeleportDistance(station, receiver);
            var inputs = new Dictionary<string, double> { [resource] = throughput };
            if (energy > 0 && resource != "energy")
            {
                inputs["energy"] = throughput * energy;
            }
            else if (resource == "energy")
            {
                // Sending energy costs some of it
                inputs[resource] = throughput * (1 + energy);
            }
            return new FlowNode { Id = station.Id, Owner = station.Owner, Kind = FlowNodeKind.Building, In = inputs };
        }
        if (TeleportSender(station) is { } sender && _teleported.TryGetValue(sender.Id, out var sent) && sent > 1e-9)
        {
            return new FlowNode
            {
                Id = station.Id,
                Owner = station.Owner,
                Kind = FlowNodeKind.Building,
                Out = new Dictionary<string, double> { [sender.Filter.First()] = sent * (1 - TeleportLoss(station.Type)) },
            };
        }
        return null;
    }

    /// <summary>After a network is solved: what its senders sent goes to their receivers (one solve later)</summary>
    private void PassTeleported(Network network)
    {
        foreach (var id in network.Buildings)
        {
            if (_buildings[id] is var station && TeleportReceiver(station) is { } receiver)
            {
                var sent = network.Solution.Rate(id) * TeleportThroughput(station);
                if (Math.Abs(sent - _teleported.GetValueOrDefault(id)) > 1e-6)
                {
                    _teleported[id] = sent;
                    MarkNeedsSolve(receiver.Id);
                }
            }
        }
    }
}
