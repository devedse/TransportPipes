using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;
using TransportPipesDatabase.Poco;

namespace TransportPipesCalculators.CalculationObjects
{
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
            CalculationForGameObject calculationForGameObject;
            var worked = _calculations.TryGetValue(gameObject, out calculationForGameObject);
            if (!worked)
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
                AddPlayerMoney(transaction.Seller.Owner, value);
                AddPlayerMoney(transaction.Buyer.Owner, value * -1.0);
            }

            if (transaction.TransactionType == BackendTransactionInfoType.TransactionWithTheMarket)
            {
                var value = transaction.ValueOfMaterials;
                AddPlayerMoney(transaction.Seller.Owner, value);
            }
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
}
