using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

/// <summary>
/// A running gift stays running, and both ends hear when it falls (after run 3, the analysts' row 9: Cleo's stone from
/// Ana fell from 55.8 to 0.8 a minute on day 22, Ana's Quarry short of energy, and neither was told). Every quarter of an
/// hour, a player's running trade with a player that runs under half its usual rate becomes a fall: both hear it with
/// the cause the solver gives, and both cards say it until it recovers.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>A running trade of less than this a minute isn't watched</summary>
    private const double FallWatchedRate = 3;

    /// <summary>A fall is over once the trade is back at this share of what it was</summary>
    private const double FallRecovered = 0.8;

    private void CheckTradeFalls()
    {
        var now = new Dictionary<(long Seller, long Buyer, string Resource), double>();
        foreach (var network in _networks.Values)
        {
            foreach (var payment in GatePayments(network).Where(t => !t.Floor && !t.Seller.IsNpc && !t.Buyer.IsNpc))
            {
                var key = (payment.Seller.Id, payment.Buyer.Id, payment.Resource);
                now[key] = now.GetValueOrDefault(key) + payment.Units;
            }
        }
        foreach (var seller in _companies.Values.Where(t => !t.IsNpc && (t.UsualTrades.Count > 0 || t.TradeFalls.Count > 0)))
        {
            foreach (var key in seller.UsualTrades.Keys.Union(seller.TradeFalls.Keys).ToList())
            {
                var parts = key.Split(':', 2);
                if (parts.Length != 2 || !long.TryParse(parts[0], out var buyerId) || Company(buyerId) is not { IsNpc: false } buyer)
                {
                    continue;
                }
                var resource = parts[1];
                var rate = now.GetValueOrDefault((seller.Id, buyer.Id, resource));
                if (seller.TradeFalls.TryGetValue(key, out var fall))
                {
                    if (rate >= FallRecovered * fall.From || LastTick - fall.At > TimeSpan.FromDays(UsualTradeDays))
                    {
                        seller.TradeFalls.Remove(key);
                        seller.Touch();
                    }
                    continue;
                }
                var usual = UsualRate(seller, buyer, resource);
                if (usual < FallWatchedRate || rate >= 0.5 * usual || seller.Asleep || buyer.Asleep)
                {
                    continue;
                }
                var (cause, building) = FallCause(seller, buyer, resource);
                fall = new TradeFall { From = usual, To = rate, At = LastTick, Cause = cause, Building = building?.Id, Owner = building?.Owner };
                seller.TradeFalls[key] = fall;
                seller.Touch();
                Changes.Companies.Add(seller.Id);
                Changes.Events.Add(new WorldEvent(seller.Id, WorldEventKind.Neighbour, FallText(seller, buyer, resource, fall, seller), fall.Owner == seller.Id ? fall.Building : null));
                Changes.Events.Add(new WorldEvent(buyer.Id, WorldEventKind.Neighbour, FallText(seller, buyer, resource, fall, buyer), fall.Owner == buyer.Id ? fall.Building : null));
            }
        }
    }

    /// <summary>
    /// Why a running trade fell, as the solver sees it: the seller's maker of the good short of something of its own, else
    /// the buyer's building that took it, else the Yard sharing it among more buyers (the cause, and the building it's about)
    /// </summary>
    private (string Cause, Building? Building) FallCause(Company seller, Company buyer, string resource)
    {
        // The seller's side only for a trouble of its own: nobody taking its goods is the fall itself, not its cause
        var maker = BuildingsOf(seller.Id)
            .Where(t => !t.UnderConstruction && Content.Building(t.Type).Out.ContainsKey(resource)
                && IdleReasonOf(t).Cause is IdleCause.InputShort or IdleCause.InputMissing or IdleCause.Damaged or IdleCause.Pumps or IdleCause.PipeFull)
            .OrderByDescending(t => Content.Building(t.Type).Out[resource] * t.Level)
            .FirstOrDefault();
        if (maker != null && IdleLine(maker) is { } line)
        {
            return (line, maker);
        }
        if (NeedOf(buyer, resource) is { } taker && IdleReasonOf(taker) is { Cause: not IdleCause.None } reason)
        {
            // Short of the very good it bought is the fall itself: what holds it back is where its own goods go (run 3's
            // test: a mine whose pipe to its post was cut takes no energy, and read "energy short")
            if (reason.Cause is IdleCause.InputShort or IdleCause.InputMissing && reason.Resource == resource
                && Content.Building(taker.Type).Out.Keys.FirstOrDefault(t => t != GameContent.ResearchPoints) is { } made
                && !PipeReach(taker).Any(t => t.Owner == taker.Owner && Content.Building(t.Type).Kind is BuildingKind.TradingPost or BuildingKind.Storage))
            {
                var word = GoodsWord(made);
                return ($"Idle: its {word} {(word.EndsWith('s') ? "reach" : "reaches")} no trading post", taker);
            }
            if (IdleLine(taker) is { } takerLine)
            {
                return (takerLine, taker);
            }
        }
        return ("the Yard shares it among more buyers now", null);
    }

    /// <summary>A fall as one end reads it: "Your stone to Cleo fell from 55.8 to 0.8 a minute: your Quarry is short of energy"</summary>
    public string FallText(Company seller, Company buyer, string resource, TradeFall fall, Company viewer)
    {
        var good = GoodsWord(resource);
        var head = viewer.Id == seller.Id
            ? $"Your {good} to {NameFor(buyer, viewer)} fell from {fall.From:0.#} to {fall.To:0.#} a minute"
            : $"{PossessiveFor(seller, viewer)} {good} to you fell from {fall.From:0.#} to {fall.To:0.#} a minute";
        if (fall.Building is not { } id || Building(id) is not { } building || Company(building.Owner) is not { } owner)
        {
            return $"{head}: {fall.Cause}";
        }
        var whose = owner.Id == viewer.Id ? "your" : PossessiveFor(owner, viewer);
        var name = Content.Building(building.Type).Name;
        var cause = fall.Cause.StartsWith("Idle: ", StringComparison.Ordinal) ? $"{whose} {name} is idle ({fall.Cause[6..]})"
            : fall.Cause.StartsWith("Stuck: ", StringComparison.Ordinal) ? $"{whose} {name} is stuck ({fall.Cause[7..]})"
            : $"{whose} {name}: {char.ToLowerInvariant(fall.Cause[0])}{fall.Cause[1..]}";
        return $"{head}: {cause}";
    }

    /// <summary>The falls a company's card tells: its own trades and the ones to it, since a moment (the biggest first)</summary>
    public List<(Company Seller, Company Buyer, string Resource, TradeFall Fall)> FallsFor(Company me, DateTime since)
    {
        var falls = new List<(Company, Company, string, TradeFall)>();
        foreach (var seller in _companies.Values.Where(t => t.TradeFalls.Count > 0))
        {
            foreach (var (key, fall) in seller.TradeFalls.Where(t => t.Value.At >= since))
            {
                var parts = key.Split(':', 2);
                if (parts.Length == 2 && long.TryParse(parts[0], out var buyerId) && (seller.Id == me.Id || buyerId == me.Id) && Company(buyerId) is { } buyer)
                {
                    falls.Add((seller, buyer, parts[1], fall));
                }
            }
        }
        return falls.OrderByDescending(t => (t.Item4.From - t.Item4.To) * BasePrice(t.Item3)).ToList();
    }
}
