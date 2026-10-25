using TransportPipes.Game.World;
using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

/// <summary>
/// The chain that would make a good nobody near makes (after run 4: "Bricks need stone + coal, I have neither", dev d14,
/// while the Houses sold both through the Yard and Dev had 12M gold; the Dynamo waited 15 days for a Brickworks nobody
/// built): the cheapest building that makes it, and where each of its inputs would come from.
/// </summary>
public sealed partial class GameWorld
{
    /// <summary>
    /// One input of a chain: who near makes it (you first), whether the Houses sell it through the Yard, and else the
    /// building you could make it with yourself (an Iron Mine for iron ore)
    /// </summary>
    public sealed record ChainInput(string Good, List<Company> Makers, bool Houses, BuildingDefinition? YouCould);

    /// <summary>A building that makes a good, whether this company may build it now (else why not), and its inputs</summary>
    public sealed record Chain(BuildingDefinition Building, double Cost, string? Locked, List<ChainInput> Inputs);

    /// <summary>The cheapest building a company could make a good with, preferring one it may build now (null: none makes it this Era)</summary>
    public Chain? ChainFor(Company me, string good)
    {
        var choices = Content.BuildingList
            .Where(t => t.Buildable && t.Out.ContainsKey(good) && t.Unlock.Era <= Era)
            .Select(t => (Definition: t, Unlocked: IsUnlocked(me, t, out var reason), Reason: reason))
            .OrderBy(t => t.Unlocked ? 0 : 1)
            .ThenBy(t => t.Definition.Cost.Gold)
            .ToList();
        if (choices.Count == 0)
        {
            return null;
        }
        var (definition, unlocked, why) = choices[0];
        var near = NearPlayers(me);
        var inputs = definition.In.Keys
            .Where(t => t != "energy" && Content.Resources.ContainsKey(t))
            .Order(StringComparer.Ordinal)
            .Select(t => new ChainInput(t,
                near.Where(c => BuildingsOf(c.Id).Any(b => !b.UnderConstruction && Content.Building(b.Type).Out.ContainsKey(t)))
                    .OrderBy(c => c.Id == me.Id ? 0 : 1).ThenBy(c => c.Id).ToList(),
                IsLandGood(t),
                OwnMaker(me, t)))
            .ToList();
        return new Chain(definition, BuildCost(me, definition), unlocked ? null : why, inputs);
    }

    /// <summary>A good a plot may lack (copper ore, coal, sand, wood, stone): it comes from a neighbour or the market</summary>
    public bool IsLandGood(string resource) => WorldGenerator.LandGoods.Contains(resource);

    /// <summary>A building that would make a good on this company's own land (not a land good dealt to another seat), cheapest first</summary>
    private BuildingDefinition? OwnMaker(Company me, string good)
    {
        var dealt = EstateOf(me) is { } estate ? Generator.LandOf(estate.District, estate.Slot) : null;
        if (IsLandGood(good) && dealt != null && !dealt.Contains(good))
        {
            return null;
        }
        return Content.BuildingList
            .Where(t => t.Buildable && t.Out.ContainsKey(good) && t.Unlock.Era <= Era && t.Unlock.Research == null)
            .OrderBy(t => t.Cost.Gold)
            .FirstOrDefault();
    }

    /// <summary>
    /// You, your station's players and your crew: who a chain can count on (activeOnly: those who played this week), or
    /// whose wants you may help with and order for (all of them: a friend away for a month too)
    /// </summary>
    public List<Company> NearPlayers(Company me, bool activeOnly = true)
    {
        var since = LastTick - ActivePeriod;
        var station = EstateOf(me) is { } estate ? StationPlayers(estate.Station) : [];
        var crew = me.CrewId is { } id && Crew(id) is { } c ? CrewMembers(c) : [];
        return station.Concat(crew).Append(me)
            .Where(t => (t.Id == me.Id || !activeOnly || (t.LastSeenAt >= since)))
            .DistinctBy(t => t.Id)
            .ToList();
    }

    /// <summary>
    /// "A Brickworks (1,400 g) makes bricks from stone and coal: Ana makes stone, Cleo makes coal; the Yard brings both",
    /// "… stone and coal: the Houses sell both through the Yard"
    /// </summary>
    public string ChainText(Chain chain, string good, Company viewer)
    {
        string Name(string resource) => GoodsWord(resource);
        var head = $"{Capitalized(Article(chain.Building.Name))} ({chain.Cost:N0} g) makes {Name(good)}";
        if (chain.Inputs.Count == 0)
        {
            return chain.Locked is { } locked ? $"{head} · {locked}" : head;
        }
        head += $" from {Join(chain.Inputs.Select(t => Name(t.Good)).ToList())}";
        var parts = new List<string>();
        foreach (var input in chain.Inputs)
        {
            if (input.Makers.FirstOrDefault() is { } maker)
            {
                parts.Add(maker.Id == viewer.Id ? $"you make {Name(input.Good)}" : $"{NameFor(maker, viewer)} makes {Name(input.Good)}");
            }
        }
        var bought = chain.Inputs.Where(t => t.Makers.Count == 0 && t.Houses).Select(t => Name(t.Good)).ToList();
        if (bought.Count > 0)
        {
            parts.Add($"the Houses sell {Join(bought)} through the Yard");
        }
        foreach (var input in chain.Inputs.Where(t => t.Makers.Count == 0 && !t.Houses && t.YouCould != null))
        {
            parts.Add($"you can make {Name(input.Good)} with {Article(input.YouCould!.Name)}");
        }
        var missing = chain.Inputs.Where(t => t.Makers.Count == 0 && !t.Houses && t.YouCould == null).Select(t => Name(t.Good)).ToList();
        if (missing.Count > 0)
        {
            parts.Add($"nobody near makes {Join(missing)} yet");
        }
        var text = parts.Count == 0 ? head : $"{head}: {string.Join("; ", parts)}";
        if (chain.Inputs.Any(t => t.Makers.Any(m => m.Id != viewer.Id) || t.Houses) && missing.Count == 0)
        {
            text += chain.Inputs.Count > 1 ? "; the Yard brings them" : "; the Yard brings it";
        }
        return chain.Locked is { } why ? $"{text} · {why}" : text;
    }

    /// <summary>
    /// What a building takes besides gold that the company's Storages don't hold (empty: they hold it all), and the line
    /// that says so (after run 4: the step said "It also takes 100 bricks from your Storages" to players with no bricks,
    /// the toast "is being built", and two buildings waited to the end for bricks nobody made)
    /// </summary>
    public (Dictionary<string, double> Short, string Text)? MaterialsLine(Company company, BuildingDefinition definition)
    {
        if (definition.Cost.Materials.Count == 0)
        {
            return null;
        }
        var have = Materials(company.Id);
        var shortOf = definition.Cost.Materials.Where(t => have.GetValueOrDefault(t.Key) < t.Value - 1e-9)
            .ToDictionary(t => t.Key, t => t.Value - have.GetValueOrDefault(t.Key));
        var all = Join(definition.Cost.Materials.Select(t => $"{t.Value:N0} {GoodsWord(t.Key)}").ToList());
        if (shortOf.Count == 0)
        {
            return (shortOf, $"It also takes {all} from your Storages.");
        }
        var near = NearPlayers(company);
        var parts = shortOf.Select(t =>
        {
            var maker = near.FirstOrDefault(c => c.Id != company.Id && BuildingsOf(c.Id).Any(b => !b.UnderConstruction && Content.Building(b.Type).Out.ContainsKey(t.Key)));
            return maker != null
                ? $"{NameFor(maker, company)} makes {GoodsWord(t.Key)}: the Yard brings them"
                : $"nobody near makes {GoodsWord(t.Key)}: order them at twice the price ({Gold(Math.Ceiling(t.Value * BasePrice(t.Key) * OrderPrice))})";
        }).ToList();
        return (shortOf, $"It waits for {all} before it stands: {string.Join("; ", parts)}.");
    }

    private static string Capitalized(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    private static string Join(IReadOnlyList<string> items) =>
        items.Count <= 1 ? string.Join("", items) : $"{string.Join(", ", items.Take(items.Count - 1))} and {items[^1]}";
}
