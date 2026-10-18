using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>Anything that has to be saved: the world marks changed entities dirty, the persistence layer saves them</summary>
public abstract class Entity
{
    public bool Dirty { get; set; } = true;

    public void Touch() => Dirty = true;
}

public enum GatePolicy
{
    Closed,
    Corporation,
    Contract,
    Open,
}

public sealed class Company : Entity
{
    public long Id { get; init; }
    public long UserId { get; init; }
    public required string Name { get; set; }
    public required string Color { get; set; }
    public double Gold { get; set; }
    public double Xp { get; set; }
    public int Level { get; set; } = 1;
    public double ResearchPoints { get; set; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastSeenAt { get; set; }
    public bool IsNpc { get; init; }

    /// <summary>Total gold earned (sales and trades), for statistics and XP</summary>
    public double LifetimeEarnings { get; set; }

    public DistrictCoord District { get; set; }
    public int Slot { get; set; }

    /// <summary>Price per resource this company asks from other companies (missing = automatic, the middle of the fair band)</summary>
    public Dictionary<string, double> SellPrices { get; init; } = new();

    /// <summary>The highest price this company pays other companies (missing = the hub's sell price)</summary>
    public Dictionary<string, double> BuyMaxPrices { get; init; } = new();

    /// <summary>Resources this company never sells to other companies</summary>
    public HashSet<string> NoTrade { get; init; } = new();

    /// <summary>Building types this company has built at least once (for the first-time XP bonus)</summary>
    public HashSet<string> BuiltTypes { get; init; } = new();

    /// <summary>Techs this company has researched</summary>
    public HashSet<string> Researched { get; init; } = new();

    /// <summary>The tech being researched now, and when it is done</summary>
    public string? CurrentResearch { get; set; }
    public DateTime? ResearchEndsAt { get; set; }

    /// <summary>Price changes that wait for the notice period because a partner buys from this company</summary>
    public Dictionary<string, PendingPrice> PendingPrices { get; init; } = new();

    /// <summary>The corporation this company is in, and its role there</summary>
    public long? CorporationId { get; set; }
    public CorporationRole Role { get; set; }

    /// <summary>When it last left (or was removed from) a corporation: joining again waits a day</summary>
    public DateTime? LeftCorporationAt { get; set; }

    /// <summary>The mentor of a newcomer, and the levels the mentorship rewarded already</summary>
    public long? MentorId { get; set; }
    public HashSet<int> MentorRewards { get; init; } = new();

    /// <summary>Lifetime earnings at the start of the week (for the weekly leaderboards)</summary>
    public double WeekStartEarnings { get; set; }

    /// <summary>Likes of the estate (all time)</summary>
    public int Likes { get; set; }

    /// <summary>Titles earned (Wonder tiers, achievements)</summary>
    public HashSet<string> Titles { get; init; } = new();

    /// <summary>How well this company keeps its contracts (0..1, starts at 1): the stars others see</summary>
    public double ContractScore { get; set; } = 1;

    public int ContractsCompleted { get; set; }

    /// <summary>Hub reputation points (daily orders, trade with the hub)</summary>
    public double HubReputation { get; set; }

    /// <summary>Today's orders from the hub</summary>
    public List<DailyOrder> DailyOrders { get; set; } = new();

    /// <summary>Statistics of the current session, shown in the offline report</summary>
    public CompanyReport Report { get; set; } = new();

    public override string ToString() => $"{Name} (#{Id})";
}

/// <summary>A new sell price (null: back to automatic) that applies at a moment</summary>
public sealed record PendingPrice(double? Price, DateTime At);

/// <summary>An order from the hub: deliver an amount of a resource before the end of the day</summary>
public sealed class DailyOrder
{
    public int Id { get; set; }
    public required string Resource { get; set; }
    public double Amount { get; set; }
    public double Delivered { get; set; }
    public double RewardGold { get; set; }
    public double RewardXp { get; set; }
    public double RewardReputation { get; set; }
    public DateTime ExpiresAt { get; set; }

    /// <summary>A Shortage order: bigger, better paid, gone when the event ends</summary>
    public bool Urgent { get; set; }

    public bool Done => Delivered >= Amount - 1e-9;
}

/// <summary>What happened since the player was last online</summary>
public sealed class CompanyReport
{
    public DateTime Since { get; set; }
    public double GoldEarned { get; set; }
    public double GoldSpent { get; set; }
    public Dictionary<string, double> Sold { get; set; } = new();
    public Dictionary<string, double> Produced { get; set; } = new();
    public int LevelsGained { get; set; }
    public int BuildingsCompleted { get; set; }
    public List<string> Notes { get; set; } = new();
    public List<string> ResearchDone { get; set; } = new();
    public List<string> Contracts { get; set; } = new();
    public List<string> NewNeighbours { get; set; } = new();
    public int OrdersCompleted { get; set; }
    public int LikesReceived { get; set; }
}

public sealed class Estate : Entity
{
    public DistrictCoord District { get; init; }
    public int Slot { get; init; }
    public TilePos Origin { get; init; }
    public long? OwnerId { get; set; }

    /// <summary>Bit n is set when parcel n is unlocked</summary>
    public int UnlockedParcels { get; set; }

    public Dictionary<GateSide, GatePolicy> GatePolicies { get; init; } = Enum.GetValues<GateSide>().ToDictionary(t => t, _ => GatePolicy.Open);

    public bool IsUnlocked(int parcel) => (UnlockedParcels & (1 << parcel)) != 0;

    public int UnlockedCount => System.Numerics.BitOperations.PopCount((uint)UnlockedParcels);

    public bool Contains(TilePos pos) =>
        pos.X >= Origin.X && pos.Y >= Origin.Y && pos.X < Origin.X + WorldLayout.EstateSize && pos.Y < Origin.Y + WorldLayout.EstateSize;

    public int ParcelAt(TilePos pos) => WorldLayout.ParcelOf(pos.X - Origin.X, pos.Y - Origin.Y);
}

public sealed class Building : Entity
{
    public long Id { get; init; }
    public required string Type { get; init; }
    public long Owner { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Size { get; init; } = 1;
    public int Level { get; set; } = 1;

    /// <summary>Null when the building is finished</summary>
    public DateTime? ConstructionEndsAt { get; set; }

    /// <summary>Storage contents</summary>
    public Dictionary<string, double> Contents { get; init; } = new();

    /// <summary>Trading post: resources it buys from the hub when its company needs them</summary>
    public HashSet<string> Imports { get; init; } = new();

    /// <summary>A number the owner sets: the toll (0..0.05) of a Toll Meter</summary>
    public double Setting { get; set; }

    /// <summary>Valve: the only resources that pass (empty: everything passes)</summary>
    public HashSet<string> Filter { get; init; } = new();

    /// <summary>Storm damage: half speed until repaired</summary>
    public bool Damaged { get; set; }

    public bool UnderConstruction => ConstructionEndsAt != null;

    public TilePos Position => new(X, Y);

    public IEnumerable<TilePos> Footprint()
    {
        for (var dy = 0; dy < Size; dy++)
        {
            for (var dx = 0; dx < Size; dx++)
            {
                yield return new TilePos(X + dx, Y + dy);
            }
        }
    }

    /// <summary>The tiles around the footprint (where pipes connect)</summary>
    public IEnumerable<TilePos> Border()
    {
        for (var i = 0; i < Size; i++)
        {
            yield return new TilePos(X + i, Y - 1);
            yield return new TilePos(X + i, Y + Size);
            yield return new TilePos(X - 1, Y + i);
            yield return new TilePos(X + Size, Y + i);
        }
    }

    public double StoredTotal => Contents.Values.Sum();

    public override string ToString() => $"{Type} #{Id} at <{X},{Y}> L{Level}";
}

public enum ConductorKind
{
    Pipe,
    Gate,
}

/// <summary>A tile that carries resources: a pipe or a gate</summary>
public sealed class Conductor : Entity
{
    public TilePos Pos { get; init; }
    public ConductorKind Kind { get; init; } = ConductorKind.Pipe;
    public int Tier { get; set; } = 1;
    public long Owner { get; set; }

    /// <summary>Gates: the side of the estate</summary>
    public GateSide? Side { get; init; }
}

public enum ContractState
{
    Offered,
    Active,
    Ended,
    Declined,
    Cancelled,
}

/// <summary>
/// A fixed amount per minute of a resource from a seller to a buyer at a fixed price for some days (GDD §5.7).
/// Offered to one company, or to everyone in the district on the contract board.
/// </summary>
public sealed class Contract : Entity
{
    public long Id { get; init; }
    public long Proposer { get; init; }

    /// <summary>0 while it is an open offer on the board without a seller or buyer yet</summary>
    public long Seller { get; set; }
    public long Buyer { get; set; }
    public required string Resource { get; init; }
    public double Rate { get; init; }
    public double Price { get; init; }
    public double Days { get; init; }
    public double PenaltyRate { get; init; } = 0.05;
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public ContractState State { get; set; }

    /// <summary>Delivered and expected so far (units)</summary>
    public double Delivered { get; set; }
    public double Expected { get; set; }

    public bool IsOpen => State is ContractState.Offered or ContractState.Active;

    public double Fulfillment => Expected <= 1e-9 ? 1 : Math.Min(1, Delivered / Expected);
}

public enum CorporationRole
{
    Member,
    Officer,
    Leader,
}

/// <summary>A goal for a corporation's week: deliver an amount of a resource together</summary>
public sealed class CorporationGoal
{
    public required string Resource { get; set; }
    public double Amount { get; set; }
    public double Delivered { get; set; }
    public double RewardGold { get; set; }
    public DateTime EndsAt { get; set; }
    public Dictionary<long, double> Contributions { get; init; } = new();
    public bool Rewarded { get; set; }
}

/// <summary>A corporation (GDD §9.2): up to 30 companies with a treasury, an HQ with perks and weekly goals</summary>
public sealed class Corporation : Entity
{
    public long Id { get; init; }
    public required string Name { get; set; }
    public required string Tag { get; set; }
    public required string Color { get; set; }
    public DistrictCoord District { get; init; }
    public DateTime CreatedAt { get; init; }
    public double Treasury { get; set; }

    /// <summary>Share of members' trade income that goes to the treasury (0..0.1)</summary>
    public double Tax { get; set; }

    public int HqLevel { get; set; } = 1;

    /// <summary>The resource the level 2 perk boosts</summary>
    public string? PerkResource { get; set; }

    public string Description { get; set; } = "";

    /// <summary>Companies that asked to join, and companies invited</summary>
    public HashSet<long> Applications { get; init; } = new();
    public HashSet<long> Invites { get; init; } = new();

    public CorporationGoal? Goal { get; set; }
}
