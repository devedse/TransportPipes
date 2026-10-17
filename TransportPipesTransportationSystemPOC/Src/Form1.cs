using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System.Diagnostics;

namespace TransportPipesTransportationSystem
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            simpleButton_Click(null, null);
        }

        private void simpleButton_Click(object sender, EventArgs e)
        {
            var w = Stopwatch.StartNew();

            //Create buildings
            var windMill1 = GenerateWindMill();
            var ironMine1 = GenerateIronMine();
            var factory1 = GenerateFactory();
            var lightBulb1 = GenerateLightBulb();
            var storage1 = GenerateStorage();

            //Create connections
            var connectionWind1Iron = new Connection()
            {
                Source = new List<GameObject>() { windMill1 },
                Destination = new List<GameObject>() { ironMine1 }
            };

            var connectionWind1Factory = new Connection()
            {
                Source = new List<GameObject>() { windMill1 },
                Destination = new List<GameObject>() { factory1 }
            };

            var connectionIronFactory = new Connection()
            {
                Source = new List<GameObject>() { ironMine1 },
                Destination = new List<GameObject>() { factory1 }
            };

            var connectionWind1Light1 = new Connection()
            {
                Source = new List<GameObject>() { windMill1 },
                Destination = new List<GameObject>() { lightBulb1 }
            };

            var connectionFactoryStorage = new Connection()
            {
                Source = new List<GameObject>() { factory1 },
                Destination = new List<GameObject>() { storage1 }
            };

            //Put the stuff in lists
            var gameObjects = new List<GameObject>()
            {
                windMill1, ironMine1, factory1
            };
            var connections = new List<Connection>()
            {
                connectionWind1Iron, connectionWind1Factory, connectionIronFactory, connectionWind1Light1, connectionFactoryStorage
            };

            //Load that stuff into the objects
            LoadConnectionsIntoGameObjects(connections);

            //Request all the iron I can get now and put it in storage. Time passed = 60 ticks (60 seconds)

            var resourcesRequested = new ResourceInfo(ResourceType.Iron, 10000);
            var listOfResourcesRequested = new List<ResourceInfo>() { resourcesRequested };
            var completeRequest = new RequestInfo(storage1, listOfResourcesRequested, new List<GameObject>() { factory1 });

            var totalCalculation = new TotalCalculationObject();

            factory1.Request(completeRequest, 60, totalCalculation);

            var storageResources = totalCalculation.GetCalculationForGameObject(storage1).Storage;

            var obtainedResourcesInStorage = storageResources.First();

            if (obtainedResourcesInStorage.ResourceType != ResourceType.Iron || obtainedResourcesInStorage.Value != 187.5)
            {
                throw new Exception("Er gaat iets mis");
            }

            Console.WriteLine("Elapsed time: " + w.Elapsed.TotalSeconds);
        }

        private void complexButton_Click(object sender, EventArgs e)
        {
            //Create buildings
            var windMill1 = GenerateWindMill();
            var windMill2 = GenerateWindMill();
            var ironMine1 = GenerateIronMine();
            var factory1 = GenerateFactory();
            var lightBulb1 = GenerateLightBulb();
            var lightBulb2 = GenerateLightBulb();

            //Create connections
            var connectionWind1Iron = new Connection()
            {
                Source = new List<GameObject>() { windMill1 },
                Destination = new List<GameObject>() { ironMine1 }
            };

            var connectionWind1Factory = new Connection()
            {
                Source = new List<GameObject>() { windMill1 },
                Destination = new List<GameObject>() { factory1 }
            };

            var connectionIronFactory = new Connection()
            {
                Source = new List<GameObject>() { ironMine1 },
                Destination = new List<GameObject>() { factory1 }
            };

            var connectionWind2Factory = new Connection()
            {
                Source = new List<GameObject>() { windMill2 },
                Destination = new List<GameObject>() { factory1 }
            };

            var connectionWind2Light2 = new Connection()
            {
                Source = new List<GameObject>() { windMill2 },
                Destination = new List<GameObject>() { lightBulb2 }
            };

            var connectionWind1Light1 = new Connection()
            {
                Source = new List<GameObject>() { windMill1 },
                Destination = new List<GameObject>() { lightBulb1 }
            };

            //Put the stuff in lists
            var gameObjects = new List<GameObject>()
            {
                windMill1, ironMine1, factory1
            };
            var connections = new List<Connection>()
            {
                connectionWind1Iron, connectionWind1Factory, connectionIronFactory, connectionWind2Factory,connectionWind2Light2,connectionWind1Light1
            };

            //Load that stuff into the objects
            LoadConnectionsIntoGameObjects(connections);

            //Request all the iron I can get now. Time passed = 60 ticks (60 seconds)
            var gameObjectReceivingThisShit = new GameObject("GodKnows", new List<ResourceInfo>(), new List<ResourceInfo>());

            var resourcesRequested = new ResourceInfo(ResourceType.Iron, 10000);
            var listOfResourcesRequested = new List<ResourceInfo>() { resourcesRequested };
            var completeRequest = new RequestInfo(gameObjectReceivingThisShit, listOfResourcesRequested, new List<GameObject>() { factory1 });

            var totalCalculation = new TotalCalculationObject();
            factory1.Request(completeRequest, 60, totalCalculation);
        }

        private void LoadConnectionsIntoGameObjects(List<Connection> connections)
        {
            foreach (var connection in connections)
            {
                foreach (var source in connection.Source)
                {
                    source.Connections.Add(connection);
                }

                foreach (var dest in connection.Destination)
                {
                    dest.Connections.Add(connection);
                }
            }
        }

        private GameObject GenerateIronMine()
        {
            return new GameObject("IronMine",
                new List<ResourceInfo>() { new ResourceInfo(ResourceType.Energy, 5) },
                new List<ResourceInfo>() { new ResourceInfo(ResourceType.IronOre, 5) }
            );
        }

        private GameObject GenerateWindMill()
        {
            return new GameObject("WindMill",
                new List<ResourceInfo>(),
                new List<ResourceInfo>() { new ResourceInfo(ResourceType.Energy, 10) }
            );
        }

        private GameObject GenerateFactory()
        {
            return new GameObject("Factory",
                new List<ResourceInfo>() { new ResourceInfo(ResourceType.Energy, 6), new ResourceInfo(ResourceType.IronOre, 5) },
                new List<ResourceInfo>() { new ResourceInfo(ResourceType.Iron, 5) }
            );
        }

        private GameObject GenerateLightBulb()
        {
            return new GameObject("LightBulb",
                new List<ResourceInfo>() { new ResourceInfo(ResourceType.Energy, 5) },
                new List<ResourceInfo>()
            );
        }

        private GameObject GenerateSolarPanel()
        {
            return new GameObject("SolarPanel",
                new List<ResourceInfo>(),
                new List<ResourceInfo>() { new ResourceInfo(ResourceType.Energy, 20) }
            );
        }

        private GameObject GenerateStorage()
        {
            return new GameObject("Storage",
                new List<ResourceInfo>(),
                new List<ResourceInfo>()
            );
        }

        private GameObject GeneratePassThroughEnergy()
        {
            return new GameObject("PassThroughEnergy",
                new List<ResourceInfo>() { new ResourceInfo(ResourceType.Energy, 999999)},
                new List<ResourceInfo>() { new ResourceInfo(ResourceType.Energy, 999999) }
            );
        }

        private void multiResource_Click(object sender, EventArgs e)
        {
            var w = Stopwatch.StartNew();

            //Create buildings
            var windMill1 = GenerateWindMill();
            var solarPanel1 = GenerateSolarPanel();
            var passThrough1 = GeneratePassThroughEnergy();
            var storage1 = GenerateStorage();


            //Create connections
            var connectionToPassThrough = new Connection()
            {
                Source = new List<GameObject>() { windMill1, solarPanel1 },
                Destination = new List<GameObject>() { passThrough1 }
            };

            var connectionToStorage1 = new Connection()
            {
                Source = new List<GameObject>() { passThrough1 },
                Destination = new List<GameObject>() { storage1 }
            };



            //Put the stuff in lists
            var gameObjects = new List<GameObject>()
            {
                windMill1, solarPanel1, passThrough1, storage1
            };
            var connections = new List<Connection>()
            {
                connectionToPassThrough, connectionToStorage1
            };

            //Load that stuff into the objects
            LoadConnectionsIntoGameObjects(connections);

            //Request all the iron I can get now and put it in storage. Time passed = 60 ticks (60 seconds)

            var resourcesRequested = new ResourceInfo(ResourceType.Energy, 30 * 120);
            var listOfResourcesRequested = new List<ResourceInfo>() { resourcesRequested };
            var completeRequest = new RequestInfo(storage1, listOfResourcesRequested, new List<GameObject>() { passThrough1 });

            var totalCalculation = new TotalCalculationObject();
            passThrough1.Request(completeRequest, 60, totalCalculation);

            var storageResources = totalCalculation.GetCalculationForGameObject(storage1).Storage;
            var obtainedResourcesInStorage = storageResources.First();

            if (obtainedResourcesInStorage.ResourceType != ResourceType.Energy || obtainedResourcesInStorage.Value != 30 * 60)
            {
                throw new Exception("Er gaat iets mis");
            }

            Console.WriteLine("Elapsed time: " + w.Elapsed.TotalSeconds);
        }

    }
}