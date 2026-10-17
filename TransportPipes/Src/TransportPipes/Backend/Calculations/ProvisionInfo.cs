using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipes.Backend.GameLogicObjects;
using TransportPipes.Backend.GameLogicObjects.Resources;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;

namespace TransportPipes.Backend.Calculations
{
    public class ProvisionInfo
    {
        public List<BackendResourceInfo> ResourcesProvided { get; private set; }
        public BackendGameObject Provisioner { get; private set; }
        public BackendGameObject Receiver { get; private set; }
        public List<BackendTransactionInfo> Transactions { get; private set; }

        public ProvisionInfo(List<BackendResourceInfo> resourcesProvided, BackendGameObject provisioner, BackendGameObject receiver)
        {
            this.ResourcesProvided = resourcesProvided;
            this.Provisioner = provisioner;
            this.Receiver = receiver;
            this.Transactions = GenerateTransactionInformation();
        }

        private List<BackendTransactionInfo> GenerateTransactionInformation()
        {
            var transactions = new List<BackendTransactionInfo>();
            foreach (var resourceInfo in this.ResourcesProvided)
            {
                var trans = new BackendTransactionInfo(Provisioner, Receiver, resourceInfo);
                transactions.Add(trans);
            }
            return transactions;
        }

        public override string ToString()
        {
            return String.Join(",", ResourcesProvided.Select(t => " " + t.ResourceType + ": " + t.Value));
        }
    }
}