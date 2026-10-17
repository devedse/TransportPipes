using TransportPipes.Common.GameLogicObjects.GameObjectInfo;
using TransportPipes.Database.Entities;

namespace TransportPipes.Calculators.CalculationObjects;

public class ProvisionInfo
{
    public List<BackendResourceInfo> ResourcesProvided { get; private set; }
    public DbGameObject Provisioner { get; private set; }
    public DbGameObject Receiver { get; private set; }
    public List<TransactionInfo> Transactions { get; private set; }

    public ProvisionInfo(List<BackendResourceInfo> resourcesProvided, DbGameObject provisioner, DbGameObject receiver)
    {
        this.ResourcesProvided = resourcesProvided;
        this.Provisioner = provisioner;
        this.Receiver = receiver;
        this.Transactions = GenerateTransactionInformation();
    }

    private List<TransactionInfo> GenerateTransactionInformation()
    {
        var transactions = new List<TransactionInfo>();
        foreach (var resourceInfo in this.ResourcesProvided)
        {
            var trans = new TransactionInfo(Provisioner, Receiver, resourceInfo);
            transactions.Add(trans);
        }
        return transactions;
    }

    public override string ToString()
    {
        return String.Join(",", ResourcesProvided.Select(t => " " + t.ResourceType + ": " + t.Value));
    }
}
