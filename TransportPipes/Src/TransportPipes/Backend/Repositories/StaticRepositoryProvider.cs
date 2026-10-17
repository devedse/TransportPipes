using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipes.Backend.Database;

namespace TransportPipes.Backend.Repositories
{
    public static class StaticRepositoryProvider
    {
        private static object _lockjectTransportDb = new object();
        private static object _lockjectUserRepository = new object();
        private static object _lockjectCachedGameObjectsRepository = new object();
        private static object _lockjectGameObjectsDatabaseRepository = new object();
        private static object _lockjectConnectedGameObjectsRepository = new object();
        private static object _lockjectConnectionFinder = new object();
        private static object _lockjectResourcesAndGlobalConnectionRepository = new object();

        private static TransportDb _transportDb;
        public static TransportDb TransportDb
        {
            get
            {
                if (_transportDb == null)
                {
                    lock (_lockjectTransportDb)
                    {
                        if (_transportDb == null)
                        {
                            _transportDb = new TransportDb();
                        }
                    }
                }
                return _transportDb;
            }
        }

        private static UserRepository _userRepository;
        public static UserRepository UserRepository
        {
            get
            {
                if (_userRepository == null)
                {
                    lock (_lockjectUserRepository)
                    {
                        if (_userRepository == null)
                        {
                            _userRepository = new UserRepository(TransportDb);
                        }
                    }
                }
                return _userRepository;
            }
        }

        private static CachedGameObjectsRepository _cachedGameObjectsRepository;
        public static CachedGameObjectsRepository CachedGameObjectsRepository
        {
            get
            {
                if (_cachedGameObjectsRepository == null)
                {
                    lock (_lockjectCachedGameObjectsRepository)
                    {
                        if (_cachedGameObjectsRepository == null)
                        {
                            _cachedGameObjectsRepository = new CachedGameObjectsRepository(GameObjectsDatabaseRepository);
                        }
                    }
                }
                return _cachedGameObjectsRepository;
            }
        }

        private static GameObjectsDatabaseRepository _gameObjectsDatabaseRepository;
        public static GameObjectsDatabaseRepository GameObjectsDatabaseRepository
        {
            get
            {
                if (_gameObjectsDatabaseRepository == null)
                {
                    lock (_lockjectGameObjectsDatabaseRepository)
                    {
                        if (_gameObjectsDatabaseRepository == null)
                        {
                            _gameObjectsDatabaseRepository = new GameObjectsDatabaseRepository(TransportDb);
                        }
                    }
                }
                return _gameObjectsDatabaseRepository;
            }
        }

        private static ConnectedGameObjectsRepository _connectedGameObjectsRepository;
        public static ConnectedGameObjectsRepository ConnectedGameObjectsRepository
        {
            get
            {
                if (_connectedGameObjectsRepository == null)
                {
                    lock (_lockjectConnectedGameObjectsRepository)
                    {
                        if (_connectedGameObjectsRepository == null)
                        {
                            _connectedGameObjectsRepository = new ConnectedGameObjectsRepository(CachedGameObjectsRepository, ConnectionFinder);
                        }
                    }
                }
                return _connectedGameObjectsRepository;
            }
        }

        private static ConnectionFinder _connectionFinder;
        public static ConnectionFinder ConnectionFinder
        {
            get
            {
                if (_connectionFinder == null)
                {
                    lock (_lockjectConnectionFinder)
                    {
                        if (_connectionFinder == null)
                        {
                            _connectionFinder = new ConnectionFinder(CachedGameObjectsRepository, TransportDb);
                        }
                    }
                }
                return _connectionFinder;
            }
        }

        private static ResourcesAndGlobalConnectionsRepository _resourcesAndGlobalConnectionsRepository;
        public static ResourcesAndGlobalConnectionsRepository ResourcesAndGlobalConnectionsRepository
        {
            get
            {
                if (_resourcesAndGlobalConnectionsRepository == null)
                {
                    lock (_lockjectResourcesAndGlobalConnectionRepository)
                    {
                        if (_resourcesAndGlobalConnectionsRepository == null)
                        {
                            _resourcesAndGlobalConnectionsRepository = new ResourcesAndGlobalConnectionsRepository(TransportDb, ConnectedGameObjectsRepository);
                        }
                    }
                }
                return _resourcesAndGlobalConnectionsRepository;
            }
        }

    }
}