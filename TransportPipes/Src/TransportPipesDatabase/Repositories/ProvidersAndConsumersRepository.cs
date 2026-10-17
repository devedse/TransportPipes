using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesDatabase.Poco;
using TransportPipesDatabase.Poco.CombinedPoco;

namespace TransportPipesDatabase.Repositories
{
    public class ProvidersAndConsumersRepository
    {
        public List<DbGameObject> ConnectedGameObjects { get; private set; }
        private List<ConsumerProviderConnection> _consumerProviderConnections;

        /// <summary>
        /// When obtaining the Consumers or Providers this class looks at the passed connectedGameObject Id's.
        /// Make sure that the consumerProviderConnections don't contain any connections where the Provider == Consumer
        /// </summary>
        /// <param name="connectedGameObjects">List of all referenced GameObjects</param>
        /// <param name="consumerProviderConnections">List of ConsumerProviderConnections where the Provider != Consumer</param>
        public ProvidersAndConsumersRepository(List<DbGameObject> connectedGameObjects, List<ConsumerProviderConnection> consumerProviderConnections)
        {
            this.ConnectedGameObjects = connectedGameObjects;
            this._consumerProviderConnections = consumerProviderConnections;
        }

        /// <summary>
        /// Obtains a list of all Consumers of this GameObject
        /// </summary>
        /// <param name="gameObject">The GameObject</param>
        /// <returns></returns>
        public List<DbGameObject> GetConsumersOfThisGameObject(DbGameObject gameObject)
        {
            var theConsumerIds = _consumerProviderConnections.Where(t => t.SourceId == gameObject.Id);
            var theConsumers = ConnectedGameObjects.Where(t => theConsumerIds.Any(z => z.DestinationId == t.Id)).ToList();
            return theConsumers;
        }

        /// <summary>
        /// Obtains a list of all Providers to this GameObject
        /// </summary>
        /// <param name="gameObject">The GameObject</param>
        /// <returns></returns>
        public List<DbGameObject> GetProvidersToThisGameObject(DbGameObject gameObject)
        {
            var theProviderIds = _consumerProviderConnections.Where(t => t.DestinationId == gameObject.Id);
            var theProviders = ConnectedGameObjects.Where(t => theProviderIds.Any(z => z.SourceId == t.Id)).ToList();
            return theProviders;
        }

        /// <summary>
        /// Obtain the Connected Game Object on this location so we can keep using the default implementations of Dictionary comparisson.
        /// </summary>
        /// <param name="x">X of the GameObject</param>
        /// <param name="y">Y of the GameObject</param>
        /// <returns></returns>
        public DbGameObject GetConnectedGameObject(int x, int y)
        {
            var foundObject = ConnectedGameObjects.SingleOrDefault(t => t.X == x && t.Y == y);
            return foundObject;
        }
    }
}
