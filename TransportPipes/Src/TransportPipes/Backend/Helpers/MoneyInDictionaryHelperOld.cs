using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipes.Backend.Database;

namespace TransportPipes.Backend.Helpers
{
    public static class MoneyInDictionaryHelperOld
    {
        public static void AddPlayerMoney(Dictionary<DbUser, double> dict, DbUser user, double money)
        {
            double previousValue;
            var worked = dict.TryGetValue(user, out previousValue);

            if (!worked)
            {
                previousValue = 0.0;
            }

            dict[user] = previousValue + money;
        }
    }
}