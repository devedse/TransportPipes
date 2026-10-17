using TransportPipes.Calculators.CalculationObjects;
using TransportPipes.Calculators.Helpers;
using TransportPipes.Database.Entities;
using TransportPipes.Database.Repositories;

namespace TransportPipes.Calculators.Calculators;

public static class MoneyForAllPlayersDeterminer
{
    /// <summary>
    /// Adds the difference in Money to a dictionary for all calculation packages provided
    /// </summary>
    /// <param name="calculationPackages">The packages to use in calculations</param>
    /// <param name="dictOfMoneyForPlayers">A dictionary which will be filled with changed money for each player</param>
    /// <param name="dateToBeExecuted">The date this calculation should be executed at (how long was the calculation already running when determining this). Provide null to calculate this for 1 second for all calculations.</param>
    /// <returns>A list of touched GlobalConnectionIds which can be used to update these calculations</returns>
    public static List<int> AddMoneyForPlayersToDictionary(List<CalculationPackage> calculationPackages, Dictionary<DbUser, double> dictOfMoneyForPlayers, DateTime? dateToBeExecuted)
    {
        var completedCalculationsGlobalConnectionIds = new List<int>();
        foreach (var calculationPackage in calculationPackages)
        {
            //var foundGameObject = _connectedGameObjectsRepository.GetGameObject(foundGameObjectInGlobalConnection.X, foundGameObjectInGlobalConnection.Y, superLock);

            TotalCalculationObject? executedCalculation;
            if (dateToBeExecuted.HasValue)
            {
                executedCalculation = CreateCalculationForCalculationPackage(calculationPackage, dateToBeExecuted.Value);
            }
            else
            {
                executedCalculation = CreateCalculationForCalculationPackage(calculationPackage, 1);
            }


            if (executedCalculation != null)
            {
                foreach (var kvpMoney in executedCalculation.PlayersAndMoney)
                {
                    MoneyInDictionaryHelper.AddPlayerMoney(dictOfMoneyForPlayers, kvpMoney.Key, kvpMoney.Value);
                }

                completedCalculationsGlobalConnectionIds.Add(calculationPackage.GlobalConnectionId);
            }
        }
        return completedCalculationsGlobalConnectionIds;
    }


    public static TotalCalculationObject? CreateCalculationForCalculationPackage(CalculationPackage calculationPackage, DateTime dateToBeExecuted)
    {
        var timePassed = dateToBeExecuted - calculationPackage.LastCalculatedDateTime;
        var secondsPassedRoundedDown = (int)timePassed.TotalSeconds;

        return CreateCalculationForCalculationPackage(calculationPackage, secondsPassedRoundedDown);
    }

    public static TotalCalculationObject? CreateCalculationForCalculationPackage(CalculationPackage calculationPackage, int secondsPassed, DbGameObject? startGameObject = null)
    {
        if (secondsPassed != 0)
        {
            //Initialize the
            //var pacRepository = new ProvidersAndConsumersRepository(allGameObjectsInThisGlobalConnection, consumerProviders);
            var resourceCalculator = new ResourceCalculator(calculationPackage.ProvidersAndConsumersRepository);

            //Execute the calculation
            var totalCalculation = new TotalCalculationObject();
            if (startGameObject != null)
            {
                resourceCalculator.ExecuteCompleteCalculation(startGameObject, secondsPassed, totalCalculation);
            }
            else
            {
                resourceCalculator.ExecuteCompleteCalculation(calculationPackage.StartGameObject, secondsPassed, totalCalculation);
            }
            return totalCalculation;
        }

        return null;
    }
}
