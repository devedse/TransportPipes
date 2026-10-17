using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesCommon.Enums;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;
using TransportPipesDatabase.Poco;

namespace TransportPipesCalculators.CalculationObjects
{
    public class TransactionInfo
    {
        public DbGameObject Seller { get; private set; }
        public DbGameObject Buyer { get; private set; }
        public BackendResourceInfo ResourceInfo { get; private set; }
        public BackendTransactionInfoType TransactionType { get; private set; }
        public double ValueOfMaterials { get; private set; }

        public TransactionInfo(DbGameObject seller, DbGameObject buyer, BackendResourceInfo resourceInfo)
        {
            this.Seller = seller;
            this.Buyer = buyer;
            this.ResourceInfo = resourceInfo;

            //If I trade to myself this is false
            if (buyer.GameObjectType == GameObjectType.TradingBuilding || seller.GameObjectType == GameObjectType.TradingBuilding)
            {
                TransactionType = BackendTransactionInfoType.TransactionWithTheMarket;
            }
            else if (Seller.Owner != Buyer.Owner)
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
                    if (buyer.GameObjectType == GameObjectType.TradingBuilding)
                    {
                        resourceMultiplier = resourceTypeValues[ResourceInfo.ResourceType].SellValueToMarket;
                    }
                    else if (seller.GameObjectType == GameObjectType.TradingBuilding)
                    {
                        resourceMultiplier = resourceTypeValues[ResourceInfo.ResourceType].BuyValueFromMarket;
                    }
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
            return ResourceInfo.ResourceType + " (" + Math.Round(ResourceInfo.Value, 5) + "): " + Math.Round(ValueOfMaterials, 5) + "$ from [" + Seller.Id + ": " + Seller.GameObjectType + " (" + Seller.Owner + ")" + " -> " + Buyer.Id + ": " + Buyer.GameObjectType + " (" + Buyer.Owner + ")]";
        }
    }
}

