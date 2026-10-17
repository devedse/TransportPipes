using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TransportPipes.Backend.GameLogicObjects
{
    public class BackendConnection
    {
        public List<BackendGameObject> Destination = new List<BackendGameObject>();
        public List<BackendGameObject> Source = new List<BackendGameObject>();

        public int GlobalConnectionId { get; private set; }

        public BackendConnection()
        {
            GlobalConnectionId = -1;
        }
    }
}