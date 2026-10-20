using TransportPipes.Game.Content;
using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>
/// NPC tenants (GDD §4.7): estates nobody has taken yet are run by simple companies with a fixed trade, so a new district
/// never feels empty and a new player always has someone to trade with. Producers sell their surplus to neighbours,
/// works buy what they need from neighbours and sell their products to the hub. When a player gets the estate, the
/// tenant moves out.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>The estates of a district that get a tenant (spread out, the ones players get last)</summary>
    public static readonly int[] TenantSlots = [23, 20, 17, 14, 11, 8, 5, 2];

    public const double TenantGold = 1_000_000_000;

    private sealed record TenantPlan(string Name, string Color, string[] Buildings, bool TradingPost);

    private static TenantPlan PlanFor(EstateSpecialty specialty, int index)
    {
        if (index % 2 == 1)
        {
            // Works that buy from the neighbours
            return index % 4 == 1
                ? new TenantPlan("Ironworks", "#8e9aaf", ["smallFactory", "smallFactory"], true)
                : new TenantPlan("Brickworks", "#b5655a", ["brickworks", "brickworks"], true);
        }
        return specialty switch
        {
            EstateSpecialty.WindyHeights => new TenantPlan("Power", "#d4b84a", ["windMoily", "windMoily", "windMoily"], false),
            EstateSpecialty.SunnyFlats => new TenantPlan("Solar", "#e0a040", ["solarPanel", "solarPanel", "solarPanel", "solarPanel", "solarPanel", "solarPanel"], false),
            EstateSpecialty.Riverside => new TenantPlan("Waterworks", "#5a9ad4", ["waterPump", "waterPump", "windMoily"], false),
            EstateSpecialty.CopperGulch => new TenantPlan("Copper Co", "#c47a4a", ["copperMine", "copperMine", "windMoily"], false),
            EstateSpecialty.CoalValley => new TenantPlan("Coal Co", "#6a6a70", ["coalPit", "coalPit", "windMoily"], false),
            EstateSpecialty.OldForest => new TenantPlan("Timber", "#7a9a4a", ["lumberCamp", "sawmill", "windMoily"], false),
            _ => new TenantPlan("Stone", "#9a9a90", ["quarry", "windMoily", "windMoily"], false),
        };
    }

    /// <summary>A tenant's trading post is never the bottleneck</summary>
    private const double TenantThroughput = 1_000_000;

    /// <summary>What a tenant's buildings use: its trading post imports it when no neighbour brings it</summary>
    private HashSet<string> TenantInputs(Company tenant) =>
        BuildingsOf(tenant.Id).SelectMany(t => Content.Building(t.Type).In.Keys).ToHashSet();

    /// <summary>Gives every open district its tenants (for worlds that existed before tenants)</summary>
    public void EnsureTenants()
    {
        foreach (var district in _openDistricts.ToList())
        {
            CreateTenants(district);
        }
    }

    private void CreateTenants(DistrictCoord district)
    {
        for (var i = 0; i < TenantSlots.Length; i++)
        {
            if (_estates.TryGetValue((district, TenantSlots[i]), out var estate) && estate.OwnerId == null)
            {
                CreateTenant(estate, i);
            }
        }
    }

    private void CreateTenant(Estate estate, int index)
    {
        var profile = Generator.Estate(estate.District, estate.Slot);
        var plan = PlanFor(profile.Primary, index);
        var name = $"{WorldNames.District(estate.District)} {plan.Name}";
        if (_companies.Values.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            name = $"{name} {(char)('A' + estate.Slot % 26)}";
        }
        var id = NextCompanyId++;
        var company = new Company
        {
            Id = id,
            UserId = -id,
            Name = name,
            Color = plan.Color,
            Gold = TenantGold,
            Level = 10,
            CreatedAt = Now,
            LastSeenAt = Now,
            IsNpc = true,
            District = estate.District,
            Slot = estate.Slot,
            Report = new CompanyReport { Since = Now },
        };
        _companies[company.Id] = company;
        estate.OwnerId = company.Id;
        estate.UnlockedParcels = (1 << WorldLayout.ParcelCount) - 1;
        estate.Touch();
        Changes.Estates.Add((estate.District, estate.Slot));
        Changes.Companies.Add(company.Id);

        foreach (var side in Enum.GetValues<GateSide>())
        {
            var pos = WorldLayout.GatePosition(estate.District, estate.Slot, side);
            _conductors[pos] = new Conductor { Pos = pos, Kind = ConductorKind.Gate, Owner = company.Id, Side = side };
            MarkDirty(pos);
            Changes.Tiles.Add(pos);
        }

        var placed = new List<Building>();
        var center = estate.Origin.Offset(WorldLayout.EstateSize / 2, WorldLayout.EstateSize / 2);
        foreach (var type in plan.Buildings.Append(plan.TradingPost ? "tradingPost" : null).OfType<string>())
        {
            if (FindTenantSpot(company, estate, Content.Building(type), center) is { } spot)
            {
                placed.Add(AddBuilding(company, type, spot, finished: true));
            }
        }

        // Every building gets a pipe to every gate, so neighbours on any side can connect
        foreach (var building in placed)
        {
            foreach (var side in Enum.GetValues<GateSide>())
            {
                var gate = WorldLayout.GatePosition(estate.District, estate.Slot, side);
                LayTenantPipe(company, estate, building, gate);
            }
        }
    }

    /// <summary>The free spot nearest to the estate's centre where a tenant's building may stand</summary>
    private TilePos? FindTenantSpot(Company company, Estate estate, BuildingDefinition definition, TilePos center)
    {
        var candidates = new List<TilePos>();
        for (var y = 3; y < WorldLayout.EstateSize - 3 - definition.Size; y++)
        {
            for (var x = 3; x < WorldLayout.EstateSize - 3 - definition.Size; x++)
            {
                candidates.Add(estate.Origin.Offset(x, y));
            }
        }
        foreach (var pos in candidates.OrderBy(t => Math.Abs(t.X - center.X) + Math.Abs(t.Y - center.Y)))
        {
            // Room around it for the pipes
            if (BorderTiles(pos, definition.Size).Any(IsOccupied) || !CanPlace(company, definition, pos).Success)
            {
                continue;
            }
            return pos;
        }
        return null;
    }

    /// <summary>A pipe from a building to the tile inside a gate (breadth first over free land of the estate)</summary>
    private void LayTenantPipe(Company company, Estate estate, Building building, TilePos gate)
    {
        bool Free(TilePos t) =>
            estate.Contains(t) && (_conductors.TryGetValue(t, out var c) ? c.Owner == company.Id && c.Kind == ConductorKind.Pipe : !_buildingAt.ContainsKey(t))
            && Tile(t).Terrain is not (TerrainType.Water or TerrainType.Mountain);

        var previous = new Dictionary<TilePos, TilePos?>();
        var queue = new Queue<TilePos>();
        foreach (var start in building.Border().Where(Free))
        {
            previous[start] = null;
            queue.Enqueue(start);
        }
        while (queue.TryDequeue(out var tile))
        {
            if (Math.Abs(tile.X - gate.X) + Math.Abs(tile.Y - gate.Y) == 1)
            {
                for (TilePos? t = tile; t != null; t = previous[t.Value])
                {
                    if (!_conductors.ContainsKey(t.Value))
                    {
                        _conductors[t.Value] = new Conductor { Pos = t.Value, Tier = 1, Owner = company.Id };
                        MarkDirty(t.Value);
                        Changes.Tiles.Add(t.Value);
                    }
                }
                return;
            }
            foreach (var (dx, dy) in Directions)
            {
                var next = tile.Offset(dx, dy);
                if (!previous.ContainsKey(next) && Free(next))
                {
                    previous[next] = tile;
                    queue.Enqueue(next);
                }
            }
        }
    }

    /// <summary>The tenant moves out: its buildings, pipes and offers disappear, the estate is free</summary>
    private void EvictTenant(Estate estate)
    {
        if (estate.OwnerId is not { } ownerId || Company(ownerId) is not { IsNpc: true } tenant)
        {
            return;
        }
        foreach (var building in BuildingsOf(tenant.Id).ToList())
        {
            RemoveBuilding(building);
        }
        foreach (var conductor in _conductors.Values.Where(t => t.Owner == tenant.Id).ToList())
        {
            MarkDirty(conductor.Pos);
            _conductors.Remove(conductor.Pos);
            Deleted.Add(conductor);
            Changes.Tiles.Add(conductor.Pos);
        }
        foreach (var contract in _contracts.Values.Where(t => t.IsOpen && (t.Seller == tenant.Id || t.Buyer == tenant.Id || t.Proposer == tenant.Id)).ToList())
        {
            contract.State = ContractState.Cancelled;
            ContractChanged(contract);
        }
        _companies.Remove(tenant.Id);
        Deleted.Add(tenant);
        estate.OwnerId = null;
        estate.UnlockedParcels = 0;
        estate.GatePolicies.Clear();
        foreach (var side in Enum.GetValues<GateSide>())
        {
            estate.GatePolicies[side] = GatePolicy.Open;
        }
        estate.Touch();
        Changes.Estates.Add((estate.District, estate.Slot));
    }
}
