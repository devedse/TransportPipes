using TransportPipes.Database.Entities;

namespace TransportPipes.Calculators.Helpers;

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
