using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesDatabase.Poco;

namespace TransportPipesCalculators.Helpers
{
    public static class MoneyInDictionaryHelper
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
