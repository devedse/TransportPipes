using TransportPipes.Game.World;

namespace TransportPipes.Game.State;

/// <summary>Anything that has to be saved: the world marks changed entities dirty, the persistence layer saves them</summary>
public abstract class Entity
{
    public bool Dirty { get; set; } = true;

    public void Touch() => Dirty = true;
}

/// <summary>A trade with one partner over the last 7 days, by day: the units taken and the minutes it ran</summary>
public sealed class UsualTrade
{
    /// <summary>The world's day (since the epoch) of the newest bucket</summary>
    public int Day { get; set; }
    public double[] Units { get; set; } = new double[7];
    public double[] Minutes { get; set; } = new double[7];
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

    /// <summary>What a House (an NPC company) does for its station; null for players</summary>
    public HouseKind? House { get; set; }

    /// <summary>Total gold earned (sales and trades), for statistics and XP</summary>
    public double LifetimeEarnings { get; set; }

    public DistrictCoord District { get; set; }
    public int Slot { get; set; }

    /// <summary>The hubs' orders for this company per hub ("x,y"), v1.2 §8: what it sold lately and the prices that follow</summary>
    public Dictionary<string, HubMarket> HubMarkets { get; init; } = new();

    /// <summary>Resources this company never sells to other companies</summary>
    public HashSet<string> NoTrade { get; init; } = new();

    /// <summary>Where its XP came from: per source (buildings, upgrades, earnings...) per item (a building type...)</summary>
    public Dictionary<string, Dictionary<string, double>> XpLog { get; init; } = new();

    /// <summary>Companies this one doesn't trade with (neither sells to nor buys from)</summary>
    public HashSet<long> TradeBlocked { get; init; } = new();

    /// <summary>Blocks that take effect after the notice period (an hour: the partner's tank covers it), by company</summary>
    public Dictionary<long, DateTime> PendingBlocks { get; init; } = new();

    /// <summary>Who the company trades with on its pipes: everyone, its corporation, or nobody but contract partners</summary>
    public TradeWith TradeWith { get; set; } = TradeWith.Everyone;

    /// <summary>Resources the company doesn't buy from other companies</summary>
    public HashSet<string> NoBuy { get; init; } = new();

    /// <summary>Building types this company has built at least once (for the first-time XP bonus)</summary>
    public HashSet<string> BuiltTypes { get; init; } = new();

    /// <summary>Techs this company has researched</summary>
    public HashSet<string> Researched { get; init; } = new();

    /// <summary>The tech being researched now, and when it is done</summary>
    public string? CurrentResearch { get; set; }

    /// <summary>The techs to research next, in order (F11: at most five); each starts when the last is done and the points are there</summary>
    public List<string> ResearchQueue { get; set; } = new();
    public DateTime? ResearchEndsAt { get; set; }

    /// <summary>The crew this company is in (v1.2 §6), and when it last left one (an extra free move once a month)</summary>
    public long? CrewId { get; set; }
    public DateTime? LeftCrewAt { get; set; }

    /// <summary>A guest: the player hasn't made an account yet (counts as new for every rule against alts)</summary>
    public bool IsGuest { get; set; }

    /// <summary>On how many different days the company played, and the last one (days since 1970)</summary>
    public int DistinctDays { get; set; }
    public int LastPlayedDay { get; set; }

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

    /// <summary>Franchise (GDD §8.7): Shares (+1% production each), when a franchise takes effect, how often it happened</summary>
    public int Shares { get; set; }
    /// <summary>No longer used (a franchise used to be announced 24 hours ahead); kept for old saves</summary>
    public DateTime? FranchiseAt { get; set; }

    /// <summary>Branch estates opened by franchising (the first estate is District/Slot)</summary>
    public List<EstateRef> Branches { get; init; } = new();
    public int Franchises { get; set; }

    /// <summary>LifetimeEarnings at the last franchise: Shares come from what was earned since</summary>
    public double EarningsAtFranchise { get; set; }

    /// <summary>The Legacy level the owner brought from earlier seasons (a free manager slot from level 5)</summary>
    public int LegacyLevel { get; set; }

    /// <summary>Moving and sleeping (GDD §12): the packed estate, buildings that didn't fit, the move in progress</summary>
    public PackedEstate? Packed { get; set; }
    public List<PackedBuilding> Crates { get; init; } = new();
    public bool Asleep { get; set; }

    /// <summary>What its crew calls its player ("Ben"): the name the seat it took was kept for (F9)</summary>
    public string? KnownAs { get; set; }

    /// <summary>
    /// The stranger next door (step 6): it came alone and lives in the small station beside a crew's, sharing their Yard
    /// and project
    /// </summary>
    public bool SeatedNextDoor { get; set; }

    /// <summary>The House that keeps an asleep company's trades going (v1.2 §9), and whether its partners heard on day 23</summary>
    public StandIn? StandIn { get; set; }
    public bool StandInAnnounced { get; set; }

    /// <summary>When an asleep company's seat stops being held when a friend kept it longer, and whether its crew got the keep card</summary>
    public DateTime? HoldUntil { get; set; }
    public bool KeepAsked { get; set; }

    /// <summary>Tending (v1.2 §12): who tended the estate on which day, and until when it counts as tended</summary>
    public Dictionary<long, int> TendedBy { get; set; } = new();
    public DateTime? TendedUntil { get; set; }

    /// <summary>The seat a returner had before the Quiet Isles (its landing card)</summary>
    public EstateKey? LastSeat { get; set; }

    /// <summary>The lone-human card (v1.2 §4): days alone among the active, when it last came, whether it waits to be seen</summary>
    public int LoneDays { get; set; }
    public DateTime? LoneCardAt { get; set; }
    public bool LoneCard { get; set; }

    /// <summary>The league it plays in this week (null: none, in small mode or before three days of play)</summary>
    public int? League { get; set; }

    /// <summary>The worth (base prices) of what it made this Era: the Era Wonder's tiers are a share of it (v1.2 §13)</summary>
    public double EraOutput { get; set; }

    /// <summary>Goods whose spare goes to the station's project before the Yard (v1.2 §7): a Send to the project</summary>
    public HashSet<string> ProjectFirst { get; set; } = new();

    /// <summary>Its Sends (step 4): goods whose spare goes to a neighbour first</summary>
    public List<GoodSend> Sends { get; set; } = new();

    /// <summary>The day it last thanked each giver (once a day per pair)</summary>
    public Dictionary<long, int> ThankedOn { get; set; } = new();

    /// <summary>The first gifts it was told about ("receiver:good", "project:stage:good"): each is announced once</summary>
    public HashSet<string> GiftsAnnounced { get; set; } = new();

    /// <summary>The Plan board (v1.2 §12): what the company pinned it makes and needs</summary>
    public HashSet<string> PinMakes { get; set; } = new();
    public HashSet<string> PinNeeds { get; set; } = new();

    /// <summary>What it asked its station for (v1.2 §12)</summary>
    public List<CompanyAsk> Asks { get; set; } = new();

    /// <summary>The starter kit a House left it (v1.2 §5): Loady counts only what the player builds</summary>
    public HashSet<long> KitBuildings { get; set; } = new();

    /// <summary>When the company's current step of Loady's guide began (a step open two days shrinks to one line)</summary>
    public DateTime? TutorialStepSince { get; set; }

    /// <summary>
    /// Measured income I(c) (F4): a 24-hour average of the gold a minute the company earns by producing (hub sales, what
    /// the Yard and Houses pay, the project), and the running total it is measured from
    /// </summary>
    public double MeasuredIncome { get; set; }
    public double MeasuredEarned { get; set; }

    /// <summary>
    /// The device signals its player was seen with (v1.2 §3, "same device"): a browser id ("b:…") and a day's hash of
    /// the IP address ("ip:…", never the address itself), each with when it was last seen (kept 30 days)
    /// </summary>
    public Dictionary<string, DateTime> Devices { get; set; } = new();

    /// <summary>
    /// What this company sold its usual buyers over the last 7 days, per "buyerId:good" (taken and paid units only):
    /// a usual partner's running trade comes first in the Yard, before anything new (step 2 of the redesign)
    /// </summary>
    public Dictionary<string, UsualTrade> UsualTrades { get; set; } = new();

    /// <summary>A stranger in a crew's station is on probation for its first 14 days (v1.2 §6)</summary>
    public DateTime? ProbationUntil { get; set; }

    /// <summary>The XP multiplier while far behind the people you live with (v1.2 §10), worked out every quarter of an hour</summary>
    public double Tailwind { get; set; } = 1;
    public DateTime? ArrivesAt { get; set; }
    public DateTime? LastMovedAt { get; set; }

    /// <summary>Free moves (after waking up, or when a district merges) and a free parcel on arrival</summary>
    public int FreeMoves { get; set; }
    public bool MergeBonus { get; set; }

    /// <summary>Mastery steps researched after the whole tree (+0.5% output each)</summary>
    public int Mastery { get; set; }

    /// <summary>The expedition that is out (GDD §11.3)</summary>
    public Expedition? Expedition { get; set; }

    /// <summary>What the last expeditions brought back, newest first</summary>
    public List<string> ExpeditionLog { get; init; } = new();

    /// <summary>Relics found by expeditions</summary>
    public HashSet<string> Relics { get; init; } = new();

    /// <summary>How far the company is with Loady's guide (an index into the tutorial steps)</summary>
    public int TutorialStep { get; set; }

    /// <summary>The dug out parcels of the Deep Level (a bit per parcel; they move with the company)</summary>
    public int DeepParcels { get; set; }

    /// <summary>The parcel being dug out, and when that started and ends</summary>
    public int? DigParcel { get; set; }
    public DateTime? DigStartedAt { get; set; }
    public DateTime? DigEndsAt { get; set; }

    /// <summary>A swap of estates this company proposed to another one</summary>
    public long? SwapWith { get; set; }

    /// <summary>Hired managers (ids), achievements and collections reached (ids), and the counters behind them</summary>
    public HashSet<string> Managers { get; init; } = new();
    public HashSet<string> Achievements { get; init; } = new();
    public Dictionary<string, double> Stats { get; init; } = new();

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

    /// <summary>On how many days a crew mate tended the estate, and what the caretaker took</summary>
    public int TendedDays { get; set; }
    public double CaretakerFee { get; set; }
    public int OrdersCompleted { get; set; }
    public int LikesReceived { get; set; }

    /// <summary>Who thanked it for its goods ("Ana thanked you for your coal 💛"): the return card's social line</summary>
    public List<string> Thanks { get; set; } = new();
}

public sealed class Estate : Entity
{
    public DistrictCoord District { get; init; }
    public int Slot { get; init; }
    public TilePos Origin { get; init; }
    public long? OwnerId { get; set; }

    /// <summary>Bit n is set when parcel n is unlocked</summary>
    public int UnlockedParcels { get; set; }

    /// <summary>A corporation block (GDD §12.3): a free estate kept for the members of a corporation for a while</summary>
    public long? ReservedFor { get; set; }
    public DateTime? ReservedUntil { get; set; }

    public bool IsUnlocked(int parcel) => (UnlockedParcels & (1 << parcel)) != 0;

    public int UnlockedCount => System.Numerics.BitOperations.PopCount((uint)UnlockedParcels);

    public bool Contains(TilePos pos) =>
        pos.X >= Origin.X && pos.Y >= Origin.Y && pos.X < Origin.X + WorldLayout.EstateSize && pos.Y < Origin.Y + WorldLayout.EstateSize;

    public int ParcelAt(TilePos pos) => WorldLayout.ParcelOf(pos.X - Origin.X, pos.Y - Origin.Y);

    public StationRef Station => WorldLayout.StationOf(District, Slot);

    /// <summary>Where the estate's own pipes meet its station's Yard</summary>
    public TilePos YardGate => WorldLayout.YardGate(District, Slot);
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

    /// <summary>An estate's Yard gate: where its own pipes meet the Yard (and its links)</summary>
    Gate,
}

/// <summary>A tile that carries resources: a pipe or a gate</summary>
public sealed class Conductor : Entity
{
    public TilePos Pos { get; init; }
    public ConductorKind Kind { get; init; } = ConductorKind.Pipe;
    public int Tier { get; set; } = 1;
    public long Owner { get; set; }
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

/// <summary>A building in a crate or a packed estate: where it stood (relative to the estate), its level and contents</summary>
public sealed class PackedBuilding
{
    public required string Type { get; init; }
    public int Dx { get; init; }
    public int Dy { get; init; }
    public int Level { get; init; } = 1;
    public Dictionary<string, double> Contents { get; init; } = new();
    public HashSet<string> Imports { get; init; } = new();
    public HashSet<string> Filter { get; init; } = new();
    public double Setting { get; init; }
}

/// <summary>An estate packed up for a move or for the Quiet Isles: nothing is lost</summary>
public sealed class PackedEstate
{
    public int UnlockedParcels { get; init; }
    public List<PackedBuilding> Buildings { get; init; } = new();
    public List<PackedPipe> Pipes { get; init; } = new();
}

public sealed record PackedPipe(int Dx, int Dy, int Tier);

/// <summary>An expedition out in the wilderness</summary>
public sealed record Expedition(int Hours, DateTime StartedAt, DateTime EndsAt);

/// <summary>An estate by its district and slot (saved with the company)</summary>
public sealed record EstateRef(int DistrictX, int DistrictY, int Slot);

/// <summary>Who a company trades with on its pipes (contracts always go through)</summary>
public enum TradeWith
{
    Everyone,
    Corporation,
    Contracts,
}
