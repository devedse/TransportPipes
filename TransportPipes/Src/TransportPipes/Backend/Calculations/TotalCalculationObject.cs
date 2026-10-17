using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipes.Backend.Database;
using TransportPipes.Backend.GameLogicObjects;
using TransportPipes.Backend.GameLogicObjects.Resources;
using TransportPipes.Backend.UserManagement;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;

namespace TransportPipes.Backend.Calculations
{
	public class TotalCalculationObject
	{
		private Dictionary<BackendGameObject, CalculationForGameObject> _calculations = new Dictionary<BackendGameObject, CalculationForGameObject>();

		public Dictionary<DbUser, double> PlayersAndMoney = new Dictionary<DbUser, double>();

		public TotalCalculationObject()
		{

		}

		public CalculationForGameObject GetCalculationForGameObject(BackendGameObject gameObject)
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

		public void AddPlayerMoney(List<BackendTransactionInfo> transactions)
		{
			foreach (var transaction in transactions)
			{
				AddPlayerMoney(transaction);
			}
		}

		private void AddPlayerMoney(BackendTransactionInfo transaction)
		{
			if (transaction.TransactionType == BackendTransactionInfoType.TransactionWithSomeoneElse)
			{
				var value = transaction.ValueOfMaterials;
				AddPlayerMoney(transaction.Seller.DbGameObject.Owner, value);
				AddPlayerMoney(transaction.Buyer.DbGameObject.Owner, value * -1.0);
			}

			if (transaction.TransactionType == BackendTransactionInfoType.TransactionWithTheMarket)
			{
				var value = transaction.ValueOfMaterials;
				AddPlayerMoney(transaction.Seller.DbGameObject.Owner, value);
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

		public List<BackendTransactionInfo> GetSalesForGameObject(BackendGameObject gameObject)
		{
			return _calculations.SelectMany(t => t.Value.Provisions).SelectMany(t => t.Transactions).Where(t => t.Seller == gameObject).Where(t => t.TransactionType == BackendTransactionInfoType.TransactionWithSomeoneElse).ToList();
		}
		
		public List<BackendTransactionInfo> GetBuysForGameObject(BackendGameObject gameObject)
		{
			return GetCalculationForGameObject(gameObject).Provisions.SelectMany(t => t.Transactions).Where(t => t.TransactionType == BackendTransactionInfoType.TransactionWithSomeoneElse).ToList();
		}

		public List<BackendTransactionInfo> GetSalesWithMarketForGameObject(BackendGameObject gameObject)
		{
			return _calculations.SelectMany(t => t.Value.Provisions).SelectMany(t => t.Transactions).Where(t => t.Seller == gameObject).Where(t => t.TransactionType == BackendTransactionInfoType.TransactionWithTheMarket).ToList();
		}

		public List<BackendTransactionInfo> GetBuysWithMarketForGameObject(BackendGameObject gameObject)
		{
			return GetCalculationForGameObject(gameObject).Provisions.SelectMany(t => t.Transactions).Where(t => t.TransactionType == BackendTransactionInfoType.TransactionWithTheMarket).ToList();
		}
	}
}
