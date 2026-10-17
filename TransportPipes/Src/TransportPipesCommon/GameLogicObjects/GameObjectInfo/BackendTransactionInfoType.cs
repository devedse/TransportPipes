using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TransportPipesCommon.GameLogicObjects.GameObjectInfo
{
	public enum BackendTransactionInfoType
	{
		TransactionWithMyself = 0,
		TransactionWithSomeoneElse = 1,
		TransactionWithTheMarket = 2
	}
}