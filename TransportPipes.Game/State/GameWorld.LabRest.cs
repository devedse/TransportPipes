using TransportPipes.Game.Content;

namespace TransportPipes.Game.State;

/// <summary>Resting Labs (after run 3, step 6): when this Era's research is done, Labs eat nothing until the next Era</summary>
public sealed partial class GameWorld
{
    /// <summary>Whether a company has something left to research this Era</summary>
    private bool TreeOpen(Company company) =>
        Content.ResearchList.Any(t => !company.Researched.Contains(t.Id)
            && (company.CurrentResearch == t.Id || (t.Era <= Era && company.Level >= t.Level && t.Requires.All(company.Researched.Contains))));

    /// <summary>Whether a company's Labs rest: nothing to research and no Mastery (they eat nothing)</summary>
    public bool LabsRest(Company? company) =>
        company is { CurrentResearch: null } && !TreeOpen(company) && !ResearchedEverything(company);

    /// <summary>Companies whose Labs rested at the last check: their networks are solved again when that changes</summary>
    private readonly HashSet<long> _labsResting = new();

    /// <summary>Every quarter of an hour, and when research ends: Labs that start or stop resting change their networks</summary>
    private void CheckRestingLabs(IEnumerable<Company>? companies = null)
    {
        foreach (var company in companies ?? _companies.Values)
        {
            var rests = LabsRest(company) && BuildingsOf(company.Id).Any(IsLab);
            if (rests != _labsResting.Contains(company.Id))
            {
                if (rests)
                {
                    _labsResting.Add(company.Id);
                }
                else
                {
                    _labsResting.Remove(company.Id);
                }
                MarkCompanyNetworksDirty(company.Id);
            }
        }
    }

    private bool IsLab(Building building) =>
        Content.Building(building.Type) is { ResearchPerMinute: > 0 } definition && definition.Out.All(t => t.Key == GameContent.ResearchPoints);

    /// <summary>Whether a Lab rests now (it takes no part in its network)</summary>
    public bool LabResting(Building building) => _labsResting.Contains(building.Owner) && IsLab(building);
}
