using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TransportPipesActionizer.Calculators;
using TransportPipesDatabase;

namespace TransportPipesActionizer
{
    class Program
    {
        static void Main(string[] args)
        {
            Real();

            Console.WriteLine();
            Console.WriteLine("Press any key to continue . . .");
            Console.ReadKey();
        }

        public static void Real()
        {
            var transportPipesDbFactory = new TransportPipesDbFactory();
            
            using (var transportDatabase = transportPipesDbFactory.CreateDatabaseWithOpenConnection())
            {
                transportDatabase.SetDatabaseBrokerAndSnapshotIsolation();

                var createdDbUsersTable = transportDatabase.CreateDbIfNotExists();
                if (createdDbUsersTable)
                {
                    transportDatabase.CreateTestUsers();
                }
            }
            
            var transportActionizer = new TransportActionizer(transportPipesDbFactory);

            Console.WriteLine("Started Actionizer... (Press Escape to exit gracefully)");

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                }

                transportActionizer.ExecuteAllOpenActions(DateTime.Now);
                Thread.Sleep(1000);
            }

            Console.WriteLine("Actionizer Shutting down...");
        }
    }
}
