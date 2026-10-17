using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipesCommon.Enums;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;

namespace TransportPipes.Backend.GameLogicObjects.Resources
{
	public class BackendTransactionInfo
	{
		public BackendGameObject Seller { get; private set; }
		public BackendGameObject Buyer { get; private set; }
		public BackendResourceInfo ResourceInfo { get; private set; }
		public BackendTransactionInfoType TransactionType { get; private set; }
		public double ValueOfMaterials { get; private set; }


		public BackendTransactionInfo(BackendGameObject seller, BackendGameObject buyer, BackendResourceInfo resourceInfo)
		{
			this.Seller = seller;
			this.Buyer = buyer;
			this.ResourceInfo = resourceInfo;

			//If I trade to myself this is false
			if (buyer.DbGameObject.GameObjectType == GameObjectType.TradingBuilding)
			{
				TransactionType = BackendTransactionInfoType.TransactionWithTheMarket;
			}
			else if (Seller.DbGameObject.Owner != Buyer.DbGameObject.Owner)
			{
				TransactionType = BackendTransactionInfoType.TransactionWithSomeoneElse;
			}
			else
			{
				TransactionType = BackendTransactionInfoType.TransactionWithMyself;
			}


			double resourceMultiplier = 0;
			var resourceTypeValues = BackendResourceWorthDeterminer.AllResourceTypeValues;

			switch (TransactionType)
			{
				case BackendTransactionInfoType.TransactionWithSomeoneElse:
					resourceMultiplier = resourceTypeValues[ResourceInfo.ResourceType].ValueForPlayers;
					break;
				case BackendTransactionInfoType.TransactionWithTheMarket:
					resourceMultiplier = resourceTypeValues[ResourceInfo.ResourceType].ValueForPlayers;
					break;
				case BackendTransactionInfoType.TransactionWithMyself:
				default:
					break;
			}

			this.ValueOfMaterials = resourceMultiplier * ResourceInfo.Value;
		}

		public override string ToString()
		{
			//Because why use StringBuilders????
			return ResourceInfo.ResourceType.ToString() + " (" + Math.Round(ResourceInfo.Value, 2) + "): " + Math.Round(ValueOfMaterials, 2) + "$ from [" + Seller.DbGameObject.ID + ": " + Seller.GameObjectType + " (" + Seller.DbGameObject.Owner + ")" + " -> " + Buyer.DbGameObject.ID + ": " + Buyer.GameObjectType + " (" + Buyer.DbGameObject.Owner + ")]";
		}
	}
}