using TransportPipes.Game.Simulation;

namespace TransportPipes.Game.State;

public sealed partial class GameWorld
{
    public const double MaxContractDays = 30;

    private readonly Dictionary<long, Contract> _contracts = new();

    public long NextContractId { get; set; } = 1;

    public IEnumerable<Contract> Contracts => _contracts.Values;

    public Contract? Contract(long id) => _contracts.TryGetValue(id, out var contract) ? contract : null;

    public void LoadContract(Contract contract)
    {
        _contracts[contract.Id] = contract;
        NextContractId = Math.Max(NextContractId, contract.Id + 1);
        contract.Dirty = false;
    }

    public bool HasContract(long seller, long buyer, string resource) =>
        _contracts.Values.Any(t => t.State == ContractState.Active && t.Seller == seller && t.Buyer == buyer && t.Resource == resource);

    /// <summary>The stars (1..5) of a company's contract reputation: five only for (nearly) everything delivered</summary>
    public static int ContractStars(Company company) => Math.Clamp((int)Math.Floor(1 + 4 * company.ContractScore + 1e-9), 1, 5);

    /// <summary>Contracts the solver serves in a network: active ones between companies that are in it</summary>
    private List<FlowContract> ActiveContracts(IReadOnlySet<long> owners) =>
        _contracts.Values
            .Where(t => t.State == ContractState.Active && owners.Contains(t.Seller) && owners.Contains(t.Buyer))
            .Select(t => new FlowContract(t.Id, t.Seller, t.Buyer, t.Resource, t.Rate, t.Price))
            .ToList();

    /// <summary>
    /// Proposes a contract: selling (you deliver) or buying (you receive), to one company or, without a partner, to
    /// everyone in your district on the contract board
    /// </summary>
    public CommandResult ProposeContract(long companyId, long? partnerId, bool selling, string resource, double rate, double price, double days)
    {
        if (Company(companyId) is not { } company)
        {
            return CommandResult.Fail("Unknown company.");
        }
        if (!Content.Resources.ContainsKey(resource))
        {
            return CommandResult.Fail("Unknown resource.");
        }
        if (rate <= 0 || double.IsNaN(rate))
        {
            return CommandResult.Fail("Choose an amount per minute.");
        }
        if (days is < 1 or > MaxContractDays)
        {
            return CommandResult.Fail($"A contract runs 1 to {MaxContractDays} days.");
        }
        var basePrice = HubBuyPrice(company.District, resource);
        if (price < basePrice * 0.5 || price > HubSellPrice(company.District, resource) * 1.2)
        {
            return CommandResult.Fail($"Choose a price between {basePrice * 0.5:0.##} and {HubSellPrice(company.District, resource) * 1.2:0.##} gold.");
        }
        Company? partner = null;
        if (partnerId is { } id)
        {
            partner = Company(id);
            if (partner == null || partner.Id == company.Id)
            {
                return CommandResult.Fail("Choose another company.");
            }
        }
        var seller = selling ? company.Id : partner?.Id ?? 0;
        var buyer = selling ? partner?.Id ?? 0 : company.Id;
        if (seller != 0 && buyer != 0 && _contracts.Values.Any(t => t.IsOpen && t.Seller == seller && t.Buyer == buyer && t.Resource == resource))
        {
            return CommandResult.Fail("You already have a contract with them for this.");
        }
        if (_contracts.Values.Count(t => t.IsOpen && (t.Seller == company.Id || t.Buyer == company.Id || t.Proposer == company.Id)) >= MaxOpenContracts(company))
        {
            return CommandResult.Fail($"You can have {MaxOpenContracts(company)} contracts at a time. Contracts II allows more.");
        }

        var contract = new Contract
        {
            Id = NextContractId++,
            Proposer = company.Id,
            Seller = seller,
            Buyer = buyer,
            Resource = resource,
            Rate = Math.Round(rate, 2),
            Price = Math.Round(price, 2),
            Days = days,
            CreatedAt = Now,
            State = ContractState.Offered,
        };
        _contracts[contract.Id] = contract;
        ContractChanged(contract);
        if (partner is { IsNpc: true })
        {
            TenantAnswers(partner, contract);
        }
        return CommandResult.Ok;
    }

    /// <summary>
    /// A tenant accepts a fair offer for something it makes (selling to you) or uses (buying from you) right away, and
    /// declines the rest
    /// </summary>
    private void TenantAnswers(Company tenant, Contract contract)
    {
        var definitions = BuildingsOf(tenant.Id).Select(t => Content.Building(t.Type)).ToList();
        var auto = AutoPrice(tenant, contract.Resource);
        var fair = contract.Seller == tenant.Id
            ? definitions.Any(t => t.Out.ContainsKey(contract.Resource)) && contract.Price >= auto * 0.9
            : definitions.Any(t => t.In.ContainsKey(contract.Resource)) && contract.Price <= auto * 1.1;
        if (fair)
        {
            AcceptContract(tenant.Id, contract.Id);
        }
        else
        {
            DeclineContract(tenant.Id, contract.Id);
        }
    }

    public int MaxOpenContracts(Company company) => HasResearch(company, "contracts2") ? 12 : 5;

    /// <summary>Accepts an offer made to you, or takes an offer from the contract board</summary>
    public CommandResult AcceptContract(long companyId, long contractId)
    {
        if (Company(companyId) is not { } company || Contract(contractId) is not { State: ContractState.Offered } contract)
        {
            return CommandResult.Fail("That offer is gone.");
        }
        if (contract.Proposer == companyId)
        {
            return CommandResult.Fail("You made this offer yourself.");
        }
        if (contract.Seller == 0)
        {
            if (Company(contract.Proposer)?.District != company.District)
            {
                return CommandResult.Fail("That offer is for another district.");
            }
            contract.Seller = companyId;
        }
        else if (contract.Buyer == 0)
        {
            if (Company(contract.Proposer)?.District != company.District)
            {
                return CommandResult.Fail("That offer is for another district.");
            }
            contract.Buyer = companyId;
        }
        else if (contract.Seller != companyId && contract.Buyer != companyId)
        {
            return CommandResult.Fail("That offer is for another company.");
        }
        if (_contracts.Values.Any(t => t.Id != contract.Id && t.State == ContractState.Active && t.Seller == contract.Seller && t.Buyer == contract.Buyer && t.Resource == contract.Resource))
        {
            return CommandResult.Fail("You already have a contract with them for this.");
        }
        contract.State = ContractState.Active;
        contract.StartedAt = Now;
        contract.EndsAt = Now.AddDays(contract.Days);
        ContractChanged(contract);
        MarkCompanyNetworksDirty(contract.Seller);
        MarkCompanyNetworksDirty(contract.Buyer);
        Changes.Events.Add(new WorldEvent(contract.Proposer, WorldEventKind.ContractChanged, $"{company.Name} accepted your contract"));
        return CommandResult.Ok;
    }

    /// <summary>Declines an offer made to you, or withdraws your own offer</summary>
    public CommandResult DeclineContract(long companyId, long contractId)
    {
        if (Contract(contractId) is not { State: ContractState.Offered } contract)
        {
            return CommandResult.Fail("That offer is gone.");
        }
        if (contract.Proposer == companyId)
        {
            contract.State = ContractState.Cancelled;
        }
        else if (contract.Seller == companyId || contract.Buyer == companyId)
        {
            contract.State = ContractState.Declined;
            Changes.Events.Add(new WorldEvent(contract.Proposer, WorldEventKind.ContractChanged, $"{Company(companyId)?.Name} declined your contract"));
        }
        else
        {
            return CommandResult.Fail("That offer is for another company.");
        }
        ContractChanged(contract);
        return CommandResult.Ok;
    }

    private void ContractChanged(Contract contract)
    {
        contract.Touch();
        foreach (var id in new[] { contract.Proposer, contract.Seller, contract.Buyer }.Where(t => t != 0).Distinct())
        {
            Changes.Companies.Add(id);
        }
        Changes.Contracts.Add(contract.Id);
    }

    /// <summary>Counts what contracts delivered and should have delivered</summary>
    private void TrackContracts(FlowSolution solution, double minutes)
    {
        foreach (var flow in solution.Flows)
        {
            if (flow.Kind != FlowKind.Contract)
            {
                continue;
            }
            var seller = _buildings[flow.From].Owner;
            var buyer = _buildings[flow.To].Owner;
            var contract = _contracts.Values.FirstOrDefault(t => t.State == ContractState.Active && t.Seller == seller && t.Buyer == buyer && t.Resource == flow.Resource);
            if (contract != null)
            {
                contract.Delivered += flow.Delivered * minutes;
                contract.Touch();
            }
        }
    }

    private void ExpectContracts(double minutes)
    {
        foreach (var contract in _contracts.Values.Where(t => t.State == ContractState.Active))
        {
            contract.Expected += contract.Rate * minutes;
            contract.Touch();
        }
    }

    /// <summary>The end of a contract: the seller pays a penalty on what was missing, and both see the result</summary>
    private void EndContract(Contract contract)
    {
        if (contract.State != ContractState.Active)
        {
            return;
        }
        contract.State = ContractState.Ended;
        var shortfall = Math.Max(0, contract.Expected - contract.Delivered);
        var penalty = Math.Round(shortfall * contract.Price * contract.PenaltyRate, 2);
        if (Company(contract.Seller) is { } seller)
        {
            seller.Gold -= penalty;
            seller.ContractScore = seller.ContractScore * 0.8 + contract.Fulfillment * 0.2;
            seller.ContractsCompleted++;
            GainXp(seller, Content.Progression.XpReward("contract"));
            seller.Touch();
            var text = penalty > 0 ? $"Contract ended: {contract.Fulfillment:P0} delivered, {penalty:N0} gold penalty" : "Contract completed";
            Changes.Events.Add(new WorldEvent(seller.Id, WorldEventKind.ContractChanged, text));
            seller.Report.Contracts.Add($"{text} ({Company(contract.Buyer)?.Name})");
        }
        if (Company(contract.Buyer) is { } buyer)
        {
            buyer.Gold += penalty;
            GainXp(buyer, Content.Progression.XpReward("contract"));
            buyer.Touch();
            Changes.Events.Add(new WorldEvent(buyer.Id, WorldEventKind.ContractChanged, $"Contract ended: {contract.Fulfillment:P0} delivered"));
            buyer.Report.Contracts.Add($"Contract ended: {contract.Fulfillment:P0} delivered ({Company(contract.Seller)?.Name})");
        }
        // Trader Lin: reputation for every finished contract, and a good contract is offered again
        foreach (var party in new[] { Company(contract.Seller), Company(contract.Buyer) })
        {
            if (HasManager(party, TraderLin))
            {
                party!.HubReputation += 10;
            }
        }
        ContractChanged(contract);
        MarkCompanyNetworksDirty(contract.Seller);
        MarkCompanyNetworksDirty(contract.Buyer);
        RenewContract(contract);
    }

    /// <summary>Offers that nobody took for a week disappear</summary>
    private void ExpireOffers()
    {
        foreach (var contract in _contracts.Values.Where(t => t.State == ContractState.Offered && t.CreatedAt < LastTick.AddDays(-7)).ToList())
        {
            contract.State = ContractState.Cancelled;
            ContractChanged(contract);
        }
    }
}
