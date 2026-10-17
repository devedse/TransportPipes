using System;
using System.Collections.Generic;
using System.Data;
using TransportPipesDatabase;
using TransportPipesDatabase.Poco;

namespace TransportPipesActionizer
{
    public class TransportActionizer
    {
        private TransportPipesDbFactory _transportPipesDbFactory;

        public TransportActionizer(TransportPipesDbFactory transportPipesDbFactory)
        {
            _transportPipesDbFactory = transportPipesDbFactory;
        }

        public void ExecuteAllOpenActions(DateTime beforeThisTime)
        {
            using (var transportDatabase = _transportPipesDbFactory.CreateDatabaseWithOpenConnection())
            {
                ExecuteAllOpenActionsInternal(transportDatabase, beforeThisTime);
            }
        }

        public void ExecuteAllOpenActionsInternal(ITransportDatabase transportDatabase, DateTime beforeThisTime)
        {
            var transportActionRunner = new TransportActionRunner(transportDatabase);

            var actions = transportDatabase.ObtainUnexecutedActions(beforeThisTime);
            foreach (var action in actions)
            {
                using (var transaction = transportDatabase.OpenTransaction(IsolationLevel.ReadCommitted))
                {
                    transportActionRunner.ExecuteSingleAction(action);
                    //Only complete this action so we can do 1 action per transaction
                    transportDatabase.ChangeGoldOfPlayer(action.ExecutedById, -action.GoldClaimed);
                    transportDatabase.CompleteActions(new List<DbAction> { action });

                    transaction.Commit();
                    Console.WriteLine("\tTransaction Comitted!");
                }
            }
        }
    }
}