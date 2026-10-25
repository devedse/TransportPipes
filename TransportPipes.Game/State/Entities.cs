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

    /// <summary>The Company Level: from the gold earned in all (1.4 §14)</summary>
    public int Level { get; set; } = 1;

    public DateTime CreatedAt { get; init; }
    public DateTime LastSeenAt { get; set; }

    /// <summary>All the gold it ever earned (1.4 §14: the market, other players, orders, rewards): its Company Level</summary>
    public double LifetimeEarnings { get; set; }

    /// <summary>Its Era (1.4 §11): the highest Era a town reached while it lived there; it never goes down</summary>
    public int Era { get; set; } = 1;

    /// <summary>
    /// Whether what its levels and new buildings wait for comes from the market (2.5×) when its own spare and the Yard
    /// don't bring it within about ten minutes (1.4 §9: "the market if you allow imports"); on by default, like the model's
    /// player, so a level never waits for goods nobody near makes
    /// </summary>
    public bool WantsFromMarket { get; set; } = true;

    /// <summary>What it gives the town's Wonder by itself, per good (1.4 §13)</summary>
    public Dictionary<string, GiveMode> Give { get; init; } = new();

    public DistrictCoord District { get; set; }
    public int Slot { get; set; }

    /// <summary>The hubs' orders for this company per hub ("x,y"), v1.2 §8: what it sold lately and the prices that follow</summary>
    public Dictionary<string, HubMarket> HubMarkets { get; init; } = new();

    /// <summary>Resources this company never sells to other companies</summary>
    public HashSet<string> NoTrade { get; init; } = new();

    /// <summary>Where the gold it earned came from: per source (market, players, orders, rewards...) per item (a good...)</summary>
    public Dictionary<string, Dictionary<string, double>> EarnedLog { get; init; } = new();

    /// <summary>Companies this one doesn't trade with (neither sells to nor buys from)</summary>
    public HashSet<long> TradeBlocked { get; init; } = new();

    /// <summary>Blocks that take effect after the notice period (an hour: the partner's tank covers it), by company</summary>
    public Dictionary<long, DateTime> PendingBlocks { get; init; } = new();

    /// <summary>Resources the company doesn't buy from other companies</summary>
    public HashSet<string> NoBuy { get; init; } = new();

    /// <summary>Building types this company has built at least once (for the first-time XP bonus)</summary>
    public HashSet<string> BuiltTypes { get; init; } = new();

    /// <summary>The Era tree items it has researched (1.4 §12, the Lab's second line)</summary>
    public HashSet<string> Researched { get; init; } = new();

    /// <summary>The Era tree item being researched now, and when it is done</summary>
    public string? CurrentResearch { get; set; }

    /// <summary>The Era tree items to research next, in order (at most five); each starts when the last is done and the gold is there</summary>
    public List<string> ResearchQueue { get; set; } = new();
    public DateTime? ResearchEndsAt { get; set; }

    /// <summary>Its tiers of every track (1.4 §12, the Lab's first line)</summary>
    public Dictionary<string, int> TrackTiers { get; init; } = new();

    /// <summary>The track whose next tier is being researched now, when it is done, and the tracks next (one tier each)</summary>
    public string? CurrentTrack { get; set; }
    public DateTime? TrackEndsAt { get; set; }
    public List<string> TrackQueue { get; set; } = new();

    /// <summary>Its prices for other players (1.4 §8), by key: "public", "crew", "public:good", "crew:good", "for:companyId"</summary>
    public Dictionary<string, PriceSetting> Prices { get; set; } = new();

    /// <summary>The crew of the Yard this company lives at (1.4 §7)</summary>
    public long? CrewId { get; set; }

    /// <summary>Its "Play next to me" link (1.4 §6): the code, until when it works, and whether its first user came</summary>
    public string? NextToMeCode { get; set; }
    public DateTime? NextToMeUntil { get; set; }
    public bool NextToMeUsed { get; set; }

    /// <summary>A guest: the player hasn't made an account yet (counts as new for every rule against alts)</summary>
    public bool IsGuest { get; set; }

    /// <summary>On how many different days the company played, and the last one (days since 1970)</summary>
    public int DistinctDays { get; set; }
    public int LastPlayedDay { get; set; }

    /// <summary>The mentor of a newcomer, and the levels the mentorship rewarded already</summary>
    public long? MentorId { get; set; }
    public HashSet<int> MentorRewards { get; init; } = new();

    /// <summary>Lifetime earnings at the start of the week (for the weekly leaderboards)</summary>
    public double WeekStartEarnings { get; set; }

    /// <summary>Likes of the estate (all time)</summary>
    public int Likes { get; set; }

    /// <summary>Titles earned (Wonder tiers, achievements)</summary>
    public HashSet<string> Titles { get; init; } = new();

    /// <summary>The Legacy level the owner brought from earlier seasons (a free manager slot from level 5)</summary>
    public int LegacyLevel { get; set; }

    /// <summary>Moving (1.4 §18): the packed estate and the buildings that didn't fit</summary>
    public PackedEstate? Packed { get; set; }
    public List<PackedBuilding> Crates { get; init; } = new();

    /// <summary>The return card has said once that the Yard sells its spare to neighbours by itself (run 2: "did I agree?")</summary>
    public bool YardSalesExplained { get; set; }

    /// <summary>How the last Era ended and this company's part (the return card says it once)</summary>
    public string? EraEnded { get; set; }

    /// <summary>Purchases it may take back for an hour (after run 3: spending is safe)</summary>
    public List<Purchase> Purchases { get; set; } = new();

    /// <summary>The last "Grow everything that pays", for its hour's Undo (after run 4)</summary>
    public GrowthPurchase? LastGrowth { get; set; }

    /// <summary>The lines its last return card led with: one that comes back unchanged folds into "Still:" (after run 3, row 11)</summary>
    public List<string> LastCardLines { get; set; } = new();

    /// <summary>Its running trades to players that fell to under half, by "buyer:good", until they recover</summary>
    public Dictionary<string, TradeFall> TradeFalls { get; set; } = new();

    /// <summary>When its player's current (or last) session began; not stored (LastSeenAt moves on every tick they're online)</summary>
    public DateTime SessionStartedAt { get; set; }

    /// <summary>When a line's queue is empty and the player away, the cheapest research the gold covers starts by itself</summary>
    public bool KeepResearching { get; set; } = true;

    /// <summary>The worth (base prices) of what it made this Era: the Era Wonder's tiers are a share of it (v1.2 §13)</summary>
    public double EraOutput { get; set; }

    /// <summary>The first gifts it was told about ("receiver:good", "project:stage:good"): each is announced once</summary>
    public HashSet<string> GiftsAnnounced { get; set; } = new();

    /// <summary>The Plan board (v1.2 §12): what the company pinned it makes and needs</summary>
    public HashSet<string> PinMakes { get; set; } = new();
    public HashSet<string> PinNeeds { get; set; } = new();

    /// <summary>What it asked its station for (v1.2 §12)</summary>
    public List<CompanyAsk> Asks { get; set; } = new();

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

    /// <summary>A move in progress lands then, and when the last move was (moves wait a few minutes between them)</summary>
    public DateTime? ArrivesAt { get; set; }
    public DateTime? LastMovedAt { get; set; }

    /// <summary>Moves that skip the wait between moves</summary>
    public int FreeMoves { get; set; }

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

    /// <summary>Hired managers (ids), achievements and collections reached (ids), and the counters behind them</summary>
    public HashSet<string> Managers { get; init; } = new();
    public HashSet<string> Achievements { get; init; } = new();
    public Dictionary<string, double> Stats { get; init; } = new();

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
    public DateTime ExpiresAt { get; set; }

    /// <summary>A Shortage order: bigger, better paid, gone when the event ends</summary>
    public bool Urgent { get; set; }

    /// <summary>What it paid over the hub's price so far (it buys at its own price as the goods pass the trading post)</summary>
    public double Premium { get; set; }

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
    public List<string> NewNeighbours { get; set; } = new();

    public int OrdersCompleted { get; set; }

    /// <summary>What filled hub orders paid over the hub's price, and their goods (the return card shows them once filled)</summary>
    public double OrderGold { get; set; }
    public List<string> OrdersFilled { get; set; } = new();
    public int LikesReceived { get; set; }

    /// <summary>
    /// The books since the report began, by "partner:good" (after run 3, row 9: a card said +86.5k g against about 14k
    /// paid, counting whole days): what each player bought of its goods and the gold they paid, what it bought from each
    /// </summary>
    public Dictionary<string, double> SoldToUnits { get; set; } = new();
    public Dictionary<string, double> SoldToGold { get; set; } = new();
    public Dictionary<string, double> BoughtFromUnits { get; set; } = new();

    /// <summary>Crew mates who looked at its estate (after run 3, row 12: a visit leaves a line on the friend's card)</summary>
    public List<string> VisitedBy { get; set; } = new();
}

/// <summary>A friend's running trade that fell to under half (after run 3, row 9): both ends hear it, with its cause</summary>
public sealed class TradeFall
{
    public double From { get; set; }
    public double To { get; set; }
    public DateTime At { get; set; }
    public required string Cause { get; set; }

    /// <summary>The building it's about, and whose it is</summary>
    public long? Building { get; set; }
    public long? Owner { get; set; }
}

public sealed class Estate : Entity
{
    public DistrictCoord District { get; init; }
    public int Slot { get; init; }
    public TilePos Origin { get; init; }
    public long? OwnerId { get; set; }

    /// <summary>Bit n is set when parcel n is unlocked</summary>
    public int UnlockedParcels { get; set; }

    public bool IsUnlocked(int parcel) => (UnlockedParcels & (1 << parcel)) != 0;

    public int UnlockedCount => System.Numerics.BitOperations.PopCount((uint)UnlockedParcels);

    /// <summary>Bit n is set when outskirts parcel n (of the nine set aside for the plot, 1.4 §5) is bought</summary>
    public int OutskirtsParcels { get; set; }

    public bool HasOutskirts(int index) => (OutskirtsParcels & (1 << index)) != 0;

    public int OutskirtsCount => System.Numerics.BitOperations.PopCount((uint)OutskirtsParcels);

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

    /// <summary>What this copy cost to build (1.4 §9): its levels cost from it</summary>
    public double Price { get; set; }

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

/// <summary>A building in a crate or a packed estate: where it stood (relative to the estate), its level and contents</summary>
public sealed class PackedBuilding
{
    public required string Type { get; init; }
    public int Dx { get; init; }
    public int Dy { get; init; }
    public int Level { get; init; } = 1;

    /// <summary>What the copy cost (its levels cost from it), and its id before it was packed (its level goods follow it)</summary>
    public double Price { get; init; }
    public long OldId { get; init; }
    public Dictionary<string, double> Contents { get; init; } = new();
    public HashSet<string> Imports { get; init; } = new();
    public HashSet<string> Filter { get; init; } = new();
    public double Setting { get; init; }
}

/// <summary>An estate packed up for a move or for the Quiet Isles: nothing is lost</summary>
public sealed class PackedEstate
{
    /// <summary>The side of its Yard the plot lay on: unpacked on another side, the layout turns to face its new Yard</summary>
    public PlotSide Side { get; init; }

    public int UnlockedParcels { get; init; }
    public List<PackedBuilding> Buildings { get; init; } = new();
    public List<PackedPipe> Pipes { get; init; } = new();
}

public sealed record PackedPipe(int Dx, int Dy, int Tier);

/// <summary>An expedition out in the wilderness</summary>
public sealed record Expedition(int Hours, DateTime StartedAt, DateTime EndsAt);

/// <summary>An estate by its district and slot (saved with the company)</summary>
public sealed record EstateRef(int DistrictX, int DistrictY, int Slot);

/// <summary>What a company gives the town's Wonder of a good by itself (1.4 §13), after its own buildings, links and Yard</summary>
public enum GiveMode
{
    Off,

    /// <summary>What the market would pay least for: the spare beyond its Hub Order</summary>
    BeyondHubOrder,

    /// <summary>All of its spare</summary>
    All,
}
