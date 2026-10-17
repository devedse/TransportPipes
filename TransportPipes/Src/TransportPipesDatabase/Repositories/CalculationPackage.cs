using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesDatabase.Poco;

namespace TransportPipesDatabase.Repositories
{
    public class CalculationPackage
    {
        public int GlobalConnectionId { get; private set; }
        public ProvidersAndConsumersRepository ProvidersAndConsumersRepository { get; private set; }
        public DbGameObject StartGameObject { get; private set; }
        public DateTime LastCalculatedDateTime { get; private set; }

        public CalculationPackage(int globalConnectionId, ProvidersAndConsumersRepository providersAndConsumersRepository, DbGameObject startGameObject, DateTime lastCalculatedDateTime)
        {
            this.GlobalConnectionId = globalConnectionId;
            this.ProvidersAndConsumersRepository = providersAndConsumersRepository;
            this.StartGameObject = startGameObject;
            this.LastCalculatedDateTime = lastCalculatedDateTime;
        }
    }
}
