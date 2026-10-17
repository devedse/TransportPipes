using TransportPipes.Common.GameLogicObjects.GameObjectInfo;
using TransportPipes.Database.Entities;

namespace TransportPipes.Calculators.CalculationObjects;

public class TotalCalculationObject
{
    private Dictionary<DbGameObject, CalculationForGameObject> _calculations = new Dictionary<DbGameObject, CalculationForGameObject>();

    public Dictionary<DbUser, double> PlayersAndMoney = new Dictionary<DbUser, double>();

    public IEnumerable<DbGameObject> GameObjects
    {
        get
        {
            return _calculations.Keys;
        }
    }

    public CalculationForGameObject GetCalculationForGameObject(DbGameObject gameObject)
    {
        if (!_calculations.TryGetValue(gameObject, out var calculationForGameObject))
        {
            calculationForGameObject = new CalculationForGameObject();
            _calculations.Add(gameObject, calculationForGameObject);
        }
        return calculationForGameObject;
    }

    public void AddPlayerMoney(List<TransactionInfo> transactions)
    {
        foreach (var transaction in transactions)
        {
            AddPlayerMoney(transaction);
        }
    }

    private void AddPlayerMoney(TransactionInfo transaction)
    {
        if (transaction.TransactionType == BackendTransactionInfoType.TransactionWithSomeoneElse)
        {
            var value = transaction.ValueOfMaterials;
            AddPlayerMoney(GetOwner(transaction.Seller), value);
            AddPlayerMoney(GetOwner(transaction.Buyer), value * -1.0);
        }

        if (transaction.TransactionType == BackendTransactionInfoType.TransactionWithTheMarket)
        {
            var value = transaction.ValueOfMaterials;
            AddPlayerMoney(GetOwner(transaction.Seller), value);
        }
    }

    private static DbUser GetOwner(DbGameObject gameObject)
    {
        return gameObject.Owner ?? throw new InvalidOperationException($"The Owner of GameObject {gameObject} should be loaded to calculate money.");
    }

    private void AddPlayerMoney(DbUser user, double money)
    {
        double previousValue;
        var worked = PlayersAndMoney.TryGetValue(user, out previousValue);

        if (!worked)
        {
            previousValue = 0.0;
        }

        PlayersAndMoney[user] = previousValue + money;
    }

    public List<TransactionInfo> GetSalesForGameObject(DbGameObject gameObject)
    {
        return _calculations.SelectMany(t => t.Value.Provisions).SelectMany(t => t.Transactions).Where(t => t.Seller == gameObject).Where(t => t.TransactionType == BackendTransactionInfoType.TransactionWithSomeoneElse).ToList();
    }

    public List<TransactionInfo> GetBuysForGameObject(DbGameObject gameObject)
    {
        return GetCalculationForGameObject(gameObject).Provisions.SelectMany(t => t.Transactions).Where(t => t.TransactionType == BackendTransactionInfoType.TransactionWithSomeoneElse).ToList();
    }

    public List<TransactionInfo> GetSalesWithMarketForGameObject(DbGameObject gameObject)
    {
        return _calculations.SelectMany(t => t.Value.Provisions).SelectMany(t => t.Transactions).Where(t => t.Seller == gameObject).Where(t => t.TransactionType == BackendTransactionInfoType.TransactionWithTheMarket).ToList();
    }

    public List<TransactionInfo> GetBuysWithMarketForGameObject(DbGameObject gameObject)
    {
        return GetCalculationForGameObject(gameObject).Provisions.SelectMany(t => t.Transactions).Where(t => t.TransactionType == BackendTransactionInfoType.TransactionWithTheMarket).ToList();
    }
}
