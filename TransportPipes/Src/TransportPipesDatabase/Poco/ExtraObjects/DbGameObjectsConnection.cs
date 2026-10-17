using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesDatabase.Poco.ExtraObjects
{
    public class DbGameObjectsConnection
    {
        public List<DbGameObject> Providers { get; private set; }
        public List<DbGameObject> Consumers { get; private set; }
        public List<DbGameObject> NonConsumerProviderBuildings { get; private set; }
        public List<DbGameObject> Pipes { get; private set; }

        public DbGameObjectsConnection(List<DbGameObject> providers, List<DbGameObject> consumers, List<DbGameObject> nonConsumerProviderBuildings, List<DbGameObject> pipes)
        {
            this.Providers = providers;
            this.Consumers = consumers;
            this.NonConsumerProviderBuildings = nonConsumerProviderBuildings;
            this.Pipes = pipes;
        }
    }
}
