using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

/// <summary>
/// Research (1.4 §12): two lines in your Lab, each working on one thing at a time with a queue of five, also while you're
/// away. The first line works on tracks: deep research for one chain, nine tiers over about ten days. The second on the
/// Era tree: short research that opens an Era's pipes, buildings and systems. Research costs gold (and from tier 3 a
/// track's own goods) when it starts, but mostly time. It is yours: nothing gets cheaper because others have it.
/// </summary>
public sealed partial class GameWorld
{
    public const int ResearchQueueLength = 5;

    /// <summary>
    /// The Era tree items every company gets at a Company Level once its Era has come (Copper Pipes at Level 8): the pipe
    /// tier is the game's real level cap, so the way up is nobody's Lab's luck (after run 4: every pipe stayed grey all
    /// month, and five of seven players never researched)
    /// </summary>
    public void GrantResearch(Company company)
    {
        foreach (var tech in Content.ResearchList.Where(t => t.Grant > 0 && t.Era <= YourEra(company) && company.Level >= t.Grant && !company.Researched.Contains(t.Id)))
        {
            company.Researched.Add(tech.Id);
            company.ResearchQueue.Remove(tech.Id);
            company.ResearchHeld.Remove(tech.Id);
            if (company.CurrentResearch == tech.Id)
            {
                // The Lab was at it: the line moves on
                company.CurrentResearch = null;
                company.ResearchEndsAt = null;
                StartQueuedResearch(company);
            }
            // A pipe's or a building's (the Big Factory at 12)
            var opens = Content.BuildingList.Where(t => t.Buildable && t.Unlock.Research == tech.Id).Select(t => t.Name).ToList();
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.ResearchFinished,
                Content.PipeTiers.Any(t => t.Research == tech.Id) ? $"{tech.Name} are yours at Company Level {tech.Grant}: a full pipe's building offers them on its panel"
                : opens.Count > 0 ? $"{tech.Name} is yours at Company Level {tech.Grant}: you can build the {string.Join(" and the ", opens)} now"
                : $"{tech.Name} is yours at Company Level {tech.Grant}"));
            TreeItemDone(company, tech);
        }
    }

    /// <summary>Every company's grants, once after the world is loaded (a world from before the grants)</summary>
    private bool _grantsChecked;

    /// <summary>Whether a company has an Era tree item</summary>
    public bool HasResearch(Company company, string research) => company.Researched.Contains(research);

    /// <summary>Whether a company has a finished Lab (research needs one, from Level 4)</summary>
    public bool HasLab(Company company) => HasBuilding(company.Id, BuildingKind.Lab);

    /// <summary>How long research takes for a company: the Observatory, the Thinking Machine, Planner Sam, its Lab's level</summary>
    public double ResearchMinutes(Company company, double minutes) =>
        minutes * ManagerResearchFactor(company) * MonumentResearchFactor(company) * WonderResearchFactor(company) / LabSpeed(company);

    /// <summary>Research is this much faster for every Lab level above the first</summary>
    public const double LabSpeedPerLevel = 0.01;

    /// <summary>
    /// How fast a company researches for its Lab's level (the owner, 2026-10-31: its levels did nothing; "can we make the
    /// lab upgrade make the research slightly faster? Not too much, maybe 1% per level"): its best finished Lab's
    /// </summary>
    public double LabSpeed(Company company) =>
        1 + LabSpeedPerLevel * (BuildingsOf(company.Id).Where(t => !t.UnderConstruction && Content.Building(t.Type).Kind == BuildingKind.Lab)
            .Select(t => t.Level).DefaultIfEmpty(1).Max() - 1);

    // ---------------------------------------------------------------- The Era tree

    /// <summary>The gold an Era tree item costs</summary>
    public double ResearchCost(Company company, ResearchDefinition tech) => Math.Round(tech.Gold);

    /// <summary>
    /// Why a company can't start an Era tree item now, or null when it can; <paramref name="evenBusy"/>: as if its line
    /// were free (the cards: it can be queued or done first)
    /// </summary>
    public string? ResearchBlocker(Company company, ResearchDefinition tech, bool evenBusy = false)
    {
        if (company.Researched.Contains(tech.Id))
        {
            return "Already researched.";
        }
        if (company.CurrentResearch == tech.Id)
        {
            return "Being researched.";
        }
        if (tech.Era > YourEra(company))
        {
            return $"Needs Era {tech.Era}.";
        }
        if (!HasLab(company))
        {
            return "Research needs a Lab.";
        }
        if (company.CurrentResearch != null && !evenBusy)
        {
            return "Your Lab's second line is busy.";
        }
        return null;
    }

    /// <summary>Whether something can be researched now on either line, gold or not</summary>
    public bool ResearchOpen(Company company) =>
        HasLab(company) && (Content.ResearchList.Any(t => !company.Researched.Contains(t.Id) && t.Era <= YourEra(company))
            || Content.TrackList.Any(t => NextTier(company, t) is not null));

    /// <summary>Starts an Era tree item on the second line, or the next tier of a track on the first</summary>
    public CommandResult StartResearch(long companyId, string id)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (Content.Tracks.TryGetValue(id, out var track))
        {
            return StartTier(company, track);
        }
        if (!Content.Research.TryGetValue(id, out var tech))
        {
            return CommandResult.Fail("Unknown research.");
        }
        if (ResearchBlocker(company, tech) is { } reason)
        {
            return CommandResult.Fail(reason);
        }
        // Stopped before: it goes on where it stopped, paid already
        if (company.ResearchHeld.Remove(tech.Id, out var left))
        {
            company.CurrentResearch = tech.Id;
            company.ResearchEndsAt = LastTick.AddMinutes(left);
            company.ResearchQueue.Remove(tech.Id);
            company.Touch();
            Changes.Companies.Add(company.Id);
            return CommandResult.Ok;
        }
        var payment = Pay(company, ResearchCost(company, tech), new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }
        AddStat(company, "researchGold", ResearchCost(company, tech));
        company.CurrentResearch = tech.Id;
        company.ResearchEndsAt = LastTick.AddMinutes(ResearchMinutes(company, tech.Minutes));
        company.ResearchQueue.Remove(tech.Id);
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>
    /// Puts an Era tree item or a track's next tier in its line's queue (or takes it out): it starts by itself when the
    /// line is free and the gold is there. A track may stand in the queue more than once: one tier after the other.
    /// </summary>
    public CommandResult QueueResearch(long companyId, string id, bool queued)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (Content.Tracks.TryGetValue(id, out var track))
        {
            if (!queued)
            {
                // The last tier of it in the queue goes
                var at = company.TrackQueue.LastIndexOf(id);
                if (at >= 0)
                {
                    company.TrackQueue.RemoveAt(at);
                }
            }
            else
            {
                if (company.TrackQueue.Count >= ResearchQueueLength)
                {
                    return CommandResult.Fail($"A line's queue holds {ResearchQueueLength}.");
                }
                if (PlannedTiers(company, track) >= Content.TrackRules.Tiers.Count)
                {
                    return CommandResult.Fail($"Every tier of {track.Name} is researched or on its way.");
                }
                company.TrackQueue.Add(id);
            }
        }
        else if (Content.Research.ContainsKey(id))
        {
            if (!queued)
            {
                company.ResearchQueue.Remove(id);
            }
            else
            {
                if (company.Researched.Contains(id) || company.CurrentResearch == id)
                {
                    return CommandResult.Fail("That's researched already.");
                }
                if (!company.ResearchQueue.Contains(id))
                {
                    if (company.ResearchQueue.Count >= ResearchQueueLength)
                    {
                        return CommandResult.Fail($"A line's queue holds {ResearchQueueLength}.");
                    }
                    company.ResearchQueue.Add(id);
                }
            }
        }
        else
        {
            return CommandResult.Fail("Unknown research.");
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        StartQueuedResearch(company);
        return CommandResult.Ok;
    }

    /// <summary>
    /// A line is free: the first queued item that can start (its Era there, its gold there) starts; with nothing queued
    /// and the player away, the cheapest thing the gold covers, unless the player turned that off (run 2: research points
    /// piled up while a queue of five emptied in hours)
    /// </summary>
    private void StartQueuedResearch(Company company)
    {
        // Nothing to start: no queue on a free line, and Keep researching isn't on while away (most companies, every tick)
        var away = company.KeepResearching && LastTick - company.LastSeenAt >= KeepResearchingAway;
        if ((company.CurrentResearch != null || (company.ResearchQueue.Count == 0 && !away))
            && (company.CurrentTrack != null || (company.TrackQueue.Count == 0 && !away)))
        {
            return;
        }
        if (!HasLab(company))
        {
            return;
        }
        if (company.CurrentResearch == null)
        {
            company.ResearchQueue.RemoveAll(company.Researched.Contains);
            if (company.ResearchQueue.Count > 0)
            {
                // The first that could start waits for its gold: the order is the player's
                if (company.ResearchQueue.Select(t => Content.Research.GetValueOrDefault(t)).OfType<ResearchDefinition>().FirstOrDefault(t => t.Era <= YourEra(company)) is { } first
                    && (company.ResearchHeld.ContainsKey(first.Id) || company.Gold >= ResearchCost(company, first)))
                {
                    StartResearch(company.Id, first.Id);
                }
            }
            else if (away && CheapestResearch(company) is { } next)
            {
                StartResearch(company.Id, next.Id);
            }
        }
        if (company.CurrentTrack == null)
        {
            PruneTrackQueue(company);
            if (company.TrackQueue.Count > 0)
            {
                if (Content.Tracks.GetValueOrDefault(company.TrackQueue[0]) is { } track && NextTier(company, track) is { } tier
                    && (company.ResearchHeld.ContainsKey(track.Id) || company.Gold >= TierCost(company, track, tier).Total))
                {
                    StartTier(company, track, fromQueue: true);
                }
            }
            else if (away && CheapestTier(company) is { } next)
            {
                StartTier(company, next);
            }
        }
    }

    /// <summary>
    /// Stops what a line researches (the owner, 2026-10-31: "stop existing research without removing spend research on
    /// it"): it waits on hold with what it was paid and the work it still needs, and starts again where it stopped, for
    /// nothing; the line is free for the next in its queue
    /// </summary>
    public CommandResult StopResearch(long companyId, bool track)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        var (id, endsAt) = track ? (company.CurrentTrack, company.TrackEndsAt) : (company.CurrentResearch, company.ResearchEndsAt);
        if (id == null)
        {
            return CommandResult.Fail("Nothing is being researched on that line.");
        }
        company.ResearchHeld[id] = Math.Max(0, ((endsAt ?? LastTick) - LastTick).TotalMinutes);
        if (track)
        {
            company.CurrentTrack = null;
            company.TrackEndsAt = null;
            // Its later tiers in the queue come after it: they would only start it again
            company.TrackQueue.RemoveAll(t => t == id);
        }
        else
        {
            company.CurrentResearch = null;
            company.ResearchEndsAt = null;
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        StartQueuedResearch(company);
        return CommandResult.Ok;
    }

    /// <summary>
    /// Researches something now although its line is busy: what the line was at goes on hold, paid, first in the queue,
    /// and goes on by itself where it stopped once this is done. Nothing changes when this can't start.
    /// </summary>
    public CommandResult ResearchFirst(long companyId, string id)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        var track = Content.Tracks.ContainsKey(id);
        var (current, endsAt) = track ? (company.CurrentTrack, company.TrackEndsAt) : (company.CurrentResearch, company.ResearchEndsAt);
        if (current == null)
        {
            return StartResearch(companyId, id);
        }
        if (current == id)
        {
            return CommandResult.Fail("It is being researched now.");
        }
        var queue = track ? company.TrackQueue : company.ResearchQueue;
        var before = queue.ToList();
        if (track)
        {
            company.CurrentTrack = null;
            company.TrackEndsAt = null;
            // Brought forward from the queue: its first tier there is the one that starts now
            if (queue.IndexOf(id) is var at and >= 0)
            {
                queue.RemoveAt(at);
            }
        }
        else
        {
            company.CurrentResearch = null;
            company.ResearchEndsAt = null;
        }
        var result = StartResearch(companyId, id);
        if (!result.Success)
        {
            if (track)
            {
                company.CurrentTrack = current;
                company.TrackEndsAt = endsAt;
                company.TrackQueue = before;
            }
            else
            {
                company.CurrentResearch = current;
                company.ResearchEndsAt = endsAt;
            }
            return result;
        }
        company.ResearchHeld[current] = Math.Max(0, ((endsAt ?? LastTick) - LastTick).TotalMinutes);
        // It was started already: it goes first even when the queue is full
        (track ? company.TrackQueue : company.ResearchQueue).Insert(0, current);
        company.Touch();
        Changes.Companies.Add(company.Id);
        return result;
    }

    /// <summary>Moves something in a line's queue up (by −1) or down (by +1)</summary>
    public CommandResult MoveQueuedResearch(long companyId, bool track, int index, int by)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        var queue = track ? company.TrackQueue : company.ResearchQueue;
        var to = index + Math.Sign(by);
        if (index < 0 || index >= queue.Count || to < 0 || to >= queue.Count)
        {
            return CommandResult.Fail("It can't move further.");
        }
        (queue[index], queue[to]) = (queue[to], queue[index]);
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    /// <summary>Takes the item at a place in a line's queue out (a track's later tiers in it move one tier up)</summary>
    public CommandResult TakeOutOfQueue(long companyId, bool track, int index)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        var queue = track ? company.TrackQueue : company.ResearchQueue;
        if (index < 0 || index >= queue.Count)
        {
            return CommandResult.Fail("It isn't in the queue.");
        }
        queue.RemoveAt(index);
        company.Touch();
        Changes.Companies.Add(company.Id);
        StartQueuedResearch(company);
        return CommandResult.Ok;
    }

    /// <summary>Keep researching starts the cheapest research only when the player has been away this long</summary>
    private static readonly TimeSpan KeepResearchingAway = TimeSpan.FromMinutes(30);

    /// <summary>The cheapest Era tree item a company can start now with its gold</summary>
    public ResearchDefinition? CheapestResearch(Company company) =>
        Content.ResearchList
            .Where(t => ResearchBlocker(company, t) == null && company.Gold >= ResearchCost(company, t))
            .OrderBy(t => ResearchCost(company, t)).ThenBy(t => t.Id, StringComparer.Ordinal)
            .FirstOrDefault();

    /// <summary>The track whose next tier is cheapest and the gold covers (ties: the deeper track, then the content's order)</summary>
    public TrackDefinition? CheapestTier(Company company) =>
        Content.TrackList
            .Where(t => TierBlocker(company, t) == null && NextTier(company, t) is { } tier && company.Gold >= TierCost(company, t, tier).Total)
            .OrderBy(t => TierCost(company, t, NextTier(company, t)!.Value).Total).ThenByDescending(t => TrackTier(company, t.Id))
            .FirstOrDefault();

    /// <summary>Whether research starts by itself while the player is away</summary>
    public CommandResult SetKeepResearching(long companyId, bool keep)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        company.KeepResearching = keep;
        company.Touch();
        Changes.Companies.Add(company.Id);
        StartQueuedResearch(company);
        return CommandResult.Ok;
    }

    private void CompleteResearch(Company company)
    {
        if (company.CurrentResearch is not { } techId)
        {
            return;
        }
        company.Researched.Add(techId);
        company.CurrentResearch = null;
        company.ResearchEndsAt = null;
        company.Touch();
        Changes.Companies.Add(company.Id);
        if (Content.Research.TryGetValue(techId, out var tech))
        {
            Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.ResearchFinished, $"Research done: {tech.Name}"));
            AddMoment(company, "research", $"Researched {tech.Name}", false);
            company.Report.ResearchDone.Add(tech.Name);
            TreeItemDone(company, tech);
        }
        StartQueuedResearch(company);
        PlanNextResearch(company);
    }

    /// <summary>What an Era tree item changes: pipes that may become better, the manager slots, the networks</summary>
    private void TreeItemDone(Company company, ResearchDefinition tech)
    {
        foreach (var conductor in _conductors.Values.Where(t => t.Owner == company.Id))
        {
            MarkDirty(conductor.Pos);
        }
        MarkCompanyNetworksDirty(company.Id);
        company.Touch();
        Changes.Companies.Add(company.Id);
    }

    /// <summary>Finishes the current research of both lines right away (for tests and admins)</summary>
    public void FinishResearch(long companyId)
    {
        if (Company(companyId) is { } company)
        {
            CompleteResearch(company);
            CompleteTier(company);
        }
    }

    // ---------------------------------------------------------------- Tracks

    /// <summary>The tiers a company has of a track</summary>
    public int TrackTier(Company? company, string track) => company?.TrackTiers.GetValueOrDefault(track) ?? 0;

    /// <summary>The tiers of a track researched, being researched and in the queue</summary>
    private int PlannedTiers(Company company, TrackDefinition track) =>
        TrackTier(company, track.Id) + (company.CurrentTrack == track.Id ? 1 : 0) + company.TrackQueue.Count(t => t == track.Id);

    /// <summary>The queue keeps no unknown track and no tier beyond the last</summary>
    private void PruneTrackQueue(Company company)
    {
        var counted = new Dictionary<string, int>();
        company.TrackQueue = company.TrackQueue.Where(t =>
        {
            if (!Content.Tracks.ContainsKey(t))
            {
                return false;
            }
            counted[t] = counted.GetValueOrDefault(t) + 1;
            return TrackTier(company, t) + (company.CurrentTrack == t ? 1 : 0) + counted[t] <= Content.TrackRules.Tiers.Count;
        }).ToList();
    }

    /// <summary>The tier a track's next research would be (null: mastered, or on its way)</summary>
    public int? NextTier(Company company, TrackDefinition track)
    {
        var next = TrackTier(company, track.Id) + (company.CurrentTrack == track.Id ? 1 : 0) + 1;
        return next <= Content.TrackRules.Tiers.Count ? next : null;
    }

    /// <summary>
    /// What a tier costs: its gold (×4 per Era of the track), and from tier 3 the track's own good worth a fifth of it,
    /// from the company's Storages and, for what they lack, bought from the market at its 2.5× when the tier starts
    /// </summary>
    public (double Gold, string? Good, double Units, double FromStorages, double Bought, double Total) TierCost(Company company, TrackDefinition track, int tier)
    {
        var rules = Content.TrackRules;
        var gold = Math.Round(rules.TierGold(tier, track.Era));
        if (tier < rules.TierGoodsFrom || track.Good is not { } good)
        {
            return (gold, null, 0, 0, 0, gold);
        }
        var units = Math.Ceiling(gold * rules.TierGoodsShare / Math.Max(0.01, BasePrice(good)));
        var have = Math.Min(units, Materials(company.Id).GetValueOrDefault(good));
        var bought = Math.Ceiling((units - have) * HubSellPrice(company.District, good));
        return (gold, good, units, have, bought, gold + bought);
    }

    /// <summary>
    /// Why a company can't start a track's next tier now, or null when it can; <paramref name="evenBusy"/>: as if its
    /// line were free
    /// </summary>
    public string? TierBlocker(Company company, TrackDefinition track, bool evenBusy = false)
    {
        if (NextTier(company, track) == null)
        {
            return company.CurrentTrack == track.Id ? "Its last tier is being researched." : "Mastered.";
        }
        if (track.Era > YourEra(company))
        {
            return $"Needs Era {track.Era}.";
        }
        if (!HasLab(company))
        {
            return "Research needs a Lab.";
        }
        if (company.CurrentTrack != null && !evenBusy)
        {
            return "Your Lab's first line is busy.";
        }
        return null;
    }

    /// <summary>Starts a track's next tier on the first line: its gold, its goods from the Storages, the rest bought</summary>
    private CommandResult StartTier(Company company, TrackDefinition track, bool fromQueue = false)
    {
        if (TierBlocker(company, track) is { } reason)
        {
            return CommandResult.Fail(reason);
        }
        var tier = NextTier(company, track)!.Value;
        // Stopped before: its tier goes on where it stopped, paid already
        if (company.ResearchHeld.Remove(track.Id, out var held))
        {
            company.CurrentTrack = track.Id;
            company.TrackEndsAt = LastTick.AddMinutes(held);
            if (fromQueue && company.TrackQueue.Count > 0 && company.TrackQueue[0] == track.Id)
            {
                company.TrackQueue.RemoveAt(0);
            }
            PruneTrackQueue(company);
            company.Touch();
            Changes.Companies.Add(company.Id);
            return CommandResult.Ok;
        }
        var cost = TierCost(company, track, tier);
        if (company.Gold < cost.Total - 1e-9)
        {
            return CommandResult.Fail($"You need {Math.Ceiling(cost.Total):N0} gold.");
        }
        var payment = Pay(company, cost.Total, cost.Good is { } good && cost.FromStorages > 0 ? new Dictionary<string, double> { [good] = cost.FromStorages } : new Dictionary<string, double>());
        if (!payment.Success)
        {
            return payment;
        }
        AddStat(company, "researchGold", cost.Total);
        company.CurrentTrack = track.Id;
        company.TrackEndsAt = LastTick.AddMinutes(ResearchMinutes(company, Content.TrackRules.Tiers[tier - 1].Minutes));
        if (fromQueue && company.TrackQueue.Count > 0 && company.TrackQueue[0] == track.Id)
        {
            company.TrackQueue.RemoveAt(0);
        }
        PruneTrackQueue(company);
        company.Touch();
        Changes.Companies.Add(company.Id);
        return CommandResult.Ok;
    }

    private void CompleteTier(Company company)
    {
        if (company.CurrentTrack is not { } id || !Content.Tracks.TryGetValue(id, out var track))
        {
            company.CurrentTrack = null;
            company.TrackEndsAt = null;
            return;
        }
        var tier = TrackTier(company, id) + 1;
        company.TrackTiers[id] = tier;
        company.CurrentTrack = null;
        company.TrackEndsAt = null;
        company.Touch();
        Changes.Companies.Add(company.Id);
        var effect = Content.TrackRules.Tiers[tier - 1].Effect;
        var text = effect switch
        {
            TierEffect.Faster => "its buildings work faster",
            TierEffect.Leaner => "its buildings need less of every input",
            TierEffect.Complexes => "four of its 1×1 buildings in a square merge into a complex with the output of five",
            TierEffect.MegaComplexes => "nine of its 1×1 buildings in a square merge into a mega complex with the output of twelve",
            _ => $"you're its master: the last ×2, golden trim and the title \"{track.Name} Master\"",
        };
        Changes.Events.Add(new WorldEvent(company.Id, WorldEventKind.ResearchFinished, $"{track.Name} tier {tier}: {text}"));
        company.Report.ResearchDone.Add($"{track.Name} {tier}");
        AddMoment(company, "track", effect == TierEffect.Master ? $"{track.Name} Master: the track's last tier" : $"{track.Name} tier {tier}", effect == TierEffect.Master);
        if (effect == TierEffect.Master)
        {
            company.Titles.Add($"{track.Name} Master");
        }
        MarkCompanyNetworksDirty(company.Id);
        // Hub Orders grow from tier 5: the hub's prices change
        Changes.Markets.Add(company.District);
        if (effect is TierEffect.Complexes or TierEffect.MegaComplexes)
        {
            MergeAllComplexes(company);
        }
        StartQueuedResearch(company);
    }

    /// <summary>For testing: every Era tree item (of the company's Era) and every tier of every track</summary>
    public void GrantAllResearch(Company company, bool thisEra = false)
    {
        company.Researched.UnionWith(Content.ResearchList.Where(t => !thisEra || t.Era <= YourEra(company)).Select(t => t.Id));
        foreach (var track in Content.TrackList.Where(t => !thisEra || t.Era <= YourEra(company)))
        {
            company.TrackTiers[track.Id] = Content.TrackRules.Tiers.Count;
        }
        company.Touch();
        Changes.Companies.Add(company.Id);
        MarkCompanyNetworksDirty(company.Id);
        MergeAllComplexes(company);
    }

    /// <summary>For tests: a company's tiers of a track</summary>
    public void SetTrackTiers(Company company, string track, int tiers)
    {
        company.TrackTiers[track] = tiers;
        company.Touch();
        MarkCompanyNetworksDirty(company.Id);
        MergeAllComplexes(company);
    }

    // ---------------------------------------------------------------- What tracks do

    /// <summary>Which tracks cover a building type, each with the deposit it must stand on (null: anywhere)</summary>
    private Dictionary<string, List<(TrackDefinition Track, string? Deposit)>>? _tracksByBuilding;

    private List<(TrackDefinition Track, string? Deposit)> TracksOf(string type)
    {
        _tracksByBuilding ??= Content.TrackList
            .SelectMany(t => t.Buildings.Select(b => (Entry: GameContent.SplitTrackBuilding(b), Track: t)))
            .GroupBy(t => t.Entry.Building)
            .ToDictionary(g => g.Key, g => g.Select(t => (t.Track, t.Entry.Deposit)).ToList());
        return _tracksByBuilding.GetValueOrDefault(type) ?? [];
    }

    /// <summary>
    /// The track a building follows and the company's tiers in it: of the tracks that cover it (and its deposit), the one
    /// with the most tiers (a Coal Pit follows the better of Copper and Stone)
    /// </summary>
    public (TrackDefinition? Track, int Tiers) TrackOf(Building building)
    {
        var tracks = TracksOf(building.Type);
        if (tracks.Count == 0)
        {
            return (null, 0);
        }
        var definition = Content.Building(building.Type);
        var deposit = definition.OutFromDeposit.Count > 0 ? BaseOutputs(building, definition).Keys.FirstOrDefault() : null;
        var owner = Company(building.Owner);
        return tracks
            .Where(t => t.Deposit == null || t.Deposit == deposit)
            .Select(t => (Track: (TrackDefinition?)t.Track, Tiers: TrackTier(owner, t.Track.Id)))
            .OrderByDescending(t => t.Tiers)
            .DefaultIfEmpty((null, 0))
            .First();
    }

    /// <summary>
    /// What a building's track gives it (1.4 §22): faster (everything it takes and makes), leaner (its inputs), the
    /// output wind, sun and water power get instead of leaner, and the bonus of a big building where small ones merge;
    /// atTiers: as if its track had that many tiers (the research panel's what-ifs)
    /// </summary>
    public (double Faster, double Leaner, double PowerFree, double Big) TrackFactors(Building building, int? atTiers = null)
    {
        var (track, tiers) = TrackOf(building);
        tiers = atTiers ?? tiers;
        if (track == null || tiers == 0)
        {
            return (1, 1, 1, 1);
        }
        var definition = Content.Building(building.Type);
        var rules = Content.TrackRules;
        double faster = 1, leaner = 1, powerFree = 1, big = 1;
        var leanerSteps = 0;
        foreach (var tier in rules.Tiers.Take(tiers))
        {
            switch (tier.Effect)
            {
                case TierEffect.Faster or TierEffect.Master:
                    faster *= 1 + (tier.Amount - 1) * track.Strength;
                    break;
                case TierEffect.Leaner:
                    leaner *= tier.Amount;
                    powerFree *= rules.LeanerOutput.ElementAtOrDefault(leanerSteps) is > 0 and var more ? more : 1;
                    leanerSteps++;
                    break;
                case TierEffect.Complexes or TierEffect.MegaComplexes when definition.Size > 1:
                    // Bigger buildings don't merge: they get the bonus instead (mega replaces the first)
                    big = tier.Amount;
                    break;
            }
        }
        // Wind, sun and water power have no inputs to save: they make more instead
        return definition.In.Count == 0 ? (faster, 1, powerFree, big) : (faster, leaner, 1, big);
    }

    /// <summary>Whether a company's track for a building type lets small ones merge: 2 (complexes), 3 (mega), 0 (not yet)</summary>
    private int ComplexSizeFor(Building building)
    {
        var (track, tiers) = TrackOf(building);
        if (track == null)
        {
            return 0;
        }
        var effects = Content.TrackRules.Tiers.Take(tiers).Select(t => t.Effect).ToList();
        return effects.Contains(TierEffect.MegaComplexes) ? 3 : effects.Contains(TierEffect.Complexes) ? 2 : 0;
    }

    /// <summary>
    /// The Hub Order factor of a company's tracks for a good (1.4 §12): from tier HubOrderFrom every tier makes them 1.8
    /// times bigger (×19 at tier 9); a good in two tracks (coal) follows the better
    /// </summary>
    public double TrackHubOrderFactor(Company company, string resource)
    {
        var rules = Content.TrackRules;
        var tiers = Content.TrackList.Where(t => t.Goods.Contains(resource)).Select(t => TrackTier(company, t.Id)).DefaultIfEmpty(0).Max();
        return Math.Pow(rules.HubOrderPerTier, Math.Max(0, tiers - (rules.HubOrderFrom - 1)));
    }

    /// <summary>The extra output (0.25 = +25%) a company's managers and achievements give a building for a resource</summary>
    public double OutputBonus(Company? company, BuildingDefinition building, string resource) =>
        company == null ? 0 : ManagerOutputBonus(company, building) + AchievementOutputBonus(company, building, resource);
}
