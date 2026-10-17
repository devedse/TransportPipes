using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TransportPipes.Backend.Calculations;
using TransportPipes.Backend.GameLogicObjects.Resources;
using TransportPipesCommon.Enums;
using TransportPipesCommon.GameLogicObjects.GameObjectInfo;
using TransportPipesCommon.Repositories;

namespace TransportPipes.Backend.GameLogicObjects
{
    public partial class BackendGameObject
    {


        public string ConsumptionInfoToString(int ticks)
        {
            return String.Join(",", GameResourcesRepository.DetermineGameObjectInfo(this.GameObjectType).ConsumptionInfo.Select(t => " " + t.ResourceType + ": " + t.Value * ticks));
        }

        public string GenerationInfoToString(int ticks)
        {
            return String.Join(",", GameResourcesRepository.DetermineGameObjectInfo(this.GameObjectType).ProductionInfo.Select(t => " " + t.ResourceType + ": " + t.Value * ticks));
        }


        public void Request(RequestInfo request, int ticks, TotalCalculationObject totalCalculation)
        {
            var myCalculationObject = totalCalculation.GetCalculationForGameObject(this);

            Console.WriteLine("Request from '" + request.Requester.GameObjectType.ToString() + "' to '" + this.GameObjectType.ToString() + "' with '" + request.ToString() + "'");
            Console.WriteLine("Of total able to generate: '" + GenerationInfoToString(ticks) + "'");

            //Shouldn't be needed because all requests are already sent out
            //if (myCalculationObject.Requests.Any(t => t.Requester == request.Requester))
            //{
            //    Console.WriteLine("We've already had a request from this one, we are now avoiding an infinite loop");
            //    return;
            //}

            myCalculationObject.DestinationsHandled++;
            myCalculationObject.Requests.Add(request);

            var destinationCount = AllDestinations.Count();

            if (myCalculationObject.DestinationsHandled < destinationCount)
            {
                myCalculationObject.IHaveRequestedToSendOutRequestsForMyParents = true;
                //I have not received all requests yet I would have expected. Ask my parents to send them now
                foreach (var destination in AllDestinations.Where(t => !totalCalculation.GetCalculationForGameObject(t).IHaveSendedOutMyRequests))
                {
                    destination.SendOutMyRequestsIfNeeded(ticks, totalCalculation);
                }
            }
            else
            {
                if (myCalculationObject.DestinationsHandled > destinationCount)
                {
                    Console.WriteLine("An extra request is made on '" + this.GameObjectType.ToString() + "'. If this is due to debugging it's okay.");
                    throw new Exception("An extra request is made on '" + this.GameObjectType.ToString() + "'. If this is due to debugging it's okay.");
                }

                //Handle request somehow
                var whatICanGenerate = CalculateWhatICanGenerate(ticks);
                var whatINeedToGenerate = CalculateWhatINeedToGenerate(myCalculationObject);
                var rateINeedToWorkAt = CalculateWorkRate(whatICanGenerate, whatINeedToGenerate);

                //Somehow distribute this workrate etc
                rateINeedToWorkAt = Math.Min(1.0, rateINeedToWorkAt);

                var consumptionInfo = GameResourcesRepository.DetermineGameObjectInfo(this.GameObjectType).ConsumptionInfo;
                if (consumptionInfo.Count == 0)
                {
                    ProvideBasedOnResourcesAndRequests(whatINeedToGenerate, whatICanGenerate, ticks, totalCalculation, myCalculationObject);
                }
                else
                {
                    //I am something with an input and an output, I shall request from children what I need now
                    var resourcesINeed = CalculateResourcesINeed(rateINeedToWorkAt, ticks);
                    GoRequestResourcesFromChildren(resourcesINeed, ticks, totalCalculation, myCalculationObject);

                }

            }

        }

        public List<BackendResourceInfo> FixRequestsBasedOnOtherProviders(List<BackendResourceInfo> whatINeedToGenerate, int ticks, CalculationForGameObject myCalculationObject)
        {
            //If we could somehow use the real numbers we can generate here it would be nice.
            //We can't though because that would mean we need to loop through other's read numbers too (for the maxGeneratedCombinedWithOtherProviders)
            var listOfWhatICanGenerateMax = CalculateWhatICanGenerate(ticks);
            var resourcesThatINeedToGenerateWhenKnowingAboutTheOtherProviders = new List<BackendResourceInfo>();

            var productionInfo = GameResourcesRepository.DetermineGameObjectInfo(this.GameObjectType).ProductionInfo;

            foreach (var currentRequest in myCalculationObject.Requests)
            {
                var resourcesProvidedToThisRequester = new List<BackendResourceInfo>();

                //Now loop through all resources in this request and determine what percentage of this resource we need to give out to this requester
                foreach (var requestResourceType in currentRequest.ResourcesRequested)
                {

                    var canIGenerateThis = productionInfo.Any(t => t.ResourceType == requestResourceType.ResourceType);
                    if (canIGenerateThis)
                    {
                        var totalRequested = whatINeedToGenerate.First(t => t.ResourceType == requestResourceType.ResourceType);


                        var maxGeneratedCombinedWithOtherProviders = currentRequest.OtherProviders.SelectMany(t => t.CalculateWhatICanGenerate(ticks)).Where(t => t.ResourceType == requestResourceType.ResourceType).Sum(t => t.Value);

                        var whatICanGenerate = listOfWhatICanGenerateMax.FirstOrDefault(t => t.ResourceType == requestResourceType.ResourceType);

                        var percentageThisIs = 1 - (whatICanGenerate.Value / maxGeneratedCombinedWithOtherProviders);

                        var valueThatNeedToBeDeducted = requestResourceType.Value * percentageThisIs;

                        var elementInListToReturn = resourcesThatINeedToGenerateWhenKnowingAboutTheOtherProviders.FirstOrDefault(t => t.ResourceType == requestResourceType.ResourceType);
                        if (elementInListToReturn == null)
                        {
                            elementInListToReturn = new BackendResourceInfo(requestResourceType.ResourceType, totalRequested.Value);
                            resourcesThatINeedToGenerateWhenKnowingAboutTheOtherProviders.Add(elementInListToReturn);
                        }

                        elementInListToReturn.Value = elementInListToReturn.Value - valueThatNeedToBeDeducted;
                    }
                    else
                    {
                        //A resource is requested that we don't generate. Ignore this :)
                    }
                }
            }

            return resourcesThatINeedToGenerateWhenKnowingAboutTheOtherProviders;
        }

        public void ProvideBasedOnResourcesAndRequests(List<BackendResourceInfo> whatINeedToGenerateMax, List<BackendResourceInfo> whatICanGenerate, int ticks, TotalCalculationObject totalCalculation, CalculationForGameObject myCalculationObject)
        {
            //var whatINeedToGenerate = FixRequestsBasedOnOtherProviders(whatINeedToGenerateMax, ticks, myCalculationObject);


            //Apparently I am a storage or a windmill or something

            var productionInfo = GameResourcesRepository.DetermineGameObjectInfo(this.GameObjectType).ProductionInfo;

            foreach (var currentRequest in myCalculationObject.Requests)
            {
                var resourcesProvidedToThisRequester = new List<BackendResourceInfo>();

                //Now loop through all resources in this request and determine what percentage of this resource we need to give out to this requester
                foreach (var requestResourceType in currentRequest.ResourcesRequested)
                {

                    var canIGenerateThis = productionInfo.Any(t => t.ResourceType == requestResourceType.ResourceType);
                    if (canIGenerateThis)
                    {
                        var totalRequested = whatINeedToGenerateMax.First(t => t.ResourceType == requestResourceType.ResourceType);

                        var percentageINeedToGiveToThisProvider = requestResourceType.Value / totalRequested.Value;

                        if (double.IsNaN(percentageINeedToGiveToThisProvider))
                        {
                            //When the total requested value is 0 (which happens when a provider has no consumer but does have other providers that provide to him)
                            //we want to set this to 0, else it will propagate a nan through whole the calculation which is untrue.
                            percentageINeedToGiveToThisProvider = 0;
                        }

                        var totalGenerated = whatICanGenerate.First(t => t.ResourceType == requestResourceType.ResourceType);

                        //var maxGeneratedCombinedWithOtherProviders = currentRequest.OtherProviders.SelectMany(t => t.GenerationInfo).Where(t => t.ResourceType == requestResourceType.ResourceType).Sum(t => t.Value);

                        ////Here we determine how much % we generate based on the total generation in this connection. We then multiply this by the amount we deliver so we don't deliver too much resources to every consumer
                        //var resourcesICouldHaveGeneratedMax = this.CalculateWhatICanGenerate(ticks).First(t => t.ResourceType == requestResourceType.ResourceType).Value;
                        //var howMuchPercentIGenerateBasedOnTotal = resourcesICouldHaveGeneratedMax / (maxGeneratedCombinedWithOtherProviders * ticks);

                        //var percentageToGive = (requestResourceType.Value * howMuchPercentIGenerateBasedOnTotal) / totalRequested.Value;
                        ////If the percentage is above 1, round it back to 1, else we give more then they can handle (Exploding factories are dangerous!!!)
                        ////We could also do it * workSpeed or something
                        //percentageToGive = Math.Min(1.0, percentageToGive);

                        //var whatIGiveOutMax = totalGenerated.Value * percentageToGive;

                        ////Here we make sure we don't give out too much resources
                        //var whatIGive = Math.Min(totalRequested.Value, whatIGiveOutMax);

                        //If we need it, these are the resources we have spare, this is not used currently
                        //Ok doesnt work :o
                        //var spareResources = whatIGive - whatIGiveOutMax;

                        //resourcesProvidedToThisRequester.Add(new BackendResourceInfo(requestResourceType.ResourceType, whatIGive));

                        var whatIGiveOutMax = percentageINeedToGiveToThisProvider * totalGenerated.Value;
                        var whatIGiveOut = Math.Min(requestResourceType.Value, whatIGiveOutMax);

                        resourcesProvidedToThisRequester.Add(new BackendResourceInfo(requestResourceType.ResourceType, whatIGiveOut));
                    }


                }
                var ding = new ProvisionInfo(resourcesProvidedToThisRequester, this, currentRequest.Requester);

                currentRequest.Requester.Provide(ding, ticks, totalCalculation);
            }
        }

        public void Provide(ProvisionInfo provisionInfo, int ticks, TotalCalculationObject totalCalculation)
        {
            //Add all transactions to global calculation object
            totalCalculation.AddPlayerMoney(provisionInfo.Transactions);


            var myCalculationObject = totalCalculation.GetCalculationForGameObject(this);

            Console.WriteLine("Provision from '" + provisionInfo.Provisioner.GameObjectType.ToString() + "' to '" + this.GameObjectType.ToString() + "' with '" + provisionInfo.ToString() + "'");
            Console.WriteLine("Of total able to consume: '" + ConsumptionInfoToString(ticks) + "'");

            //if (myCalculationObject.Provisions.Any(t => t.Provisioner == provisionInfo.Provisioner))
            //{
            //    Console.WriteLine("We've already had a provision from this one, we are now avoiding an infinite loop");
            //    return;
            //}

            myCalculationObject.SourcesHandled++;
            myCalculationObject.Provisions.Add(provisionInfo);

            var sourcesCount = AllSources.Count();

            if (myCalculationObject.SourcesHandled < sourcesCount)
            {
                //Apparently we have not received information from all our sources, let's keep waiting
            }
            else if (myCalculationObject.SourcesHandled > sourcesCount)
            {
                Console.WriteLine("I got a provision from something that's not my source, should not happen only when debugging");
                throw new Exception("I got a provision from something that's not my source, should not happen only when debugging");
            }
            else
            {
                var productionInfo = GameResourcesRepository.DetermineGameObjectInfo(this.GameObjectType).ProductionInfo;
                var consumptionInfo = GameResourcesRepository.DetermineGameObjectInfo(this.GameObjectType).ConsumptionInfo;

                //Temp code to store resources when I am a storage
                if (this.GameObjectType == GameObjectType.Storage)
                {
                    foreach (var resourceInfo in myCalculationObject.Provisions.SelectMany(t => t.ResourcesProvided))
                    {
                        var currentResourceTypeInStorage = productionInfo.FirstOrDefault(t => t.ResourceType == resourceInfo.ResourceType);
                        if (currentResourceTypeInStorage == null)
                        {
                            currentResourceTypeInStorage = new BackendResourceInfo(resourceInfo.ResourceType, resourceInfo.Value);
                            //GenerationInfo.Add(currentResourceTypeInStorage);
                            myCalculationObject.Storage.Add(currentResourceTypeInStorage);
                        }
                        else
                        {
                            currentResourceTypeInStorage.Value += resourceInfo.Value;
                        }
                    }
                    return;
                }





                //Calculate percentage I got from what I requested
                var fastestRateICanWorkAt = 1.0;

                foreach (var resourceINeed in consumptionInfo)
                {
                    var resourceValueIReceived = myCalculationObject.Provisions.SelectMany(t => t.ResourcesProvided).Where(t => t.ResourceType == resourceINeed.ResourceType).Sum(t => t.Value);
                    var resourceValueIRequested = resourceINeed.Value * ticks;

                    var rateICanWorkAtForThisResource = resourceValueIReceived / resourceValueIRequested;
                    if (fastestRateICanWorkAt > rateICanWorkAtForThisResource)
                    {
                        fastestRateICanWorkAt = rateICanWorkAtForThisResource;
                    }
                }

                //Just to be sure min it to 1.0, we can't work harder then we can
                fastestRateICanWorkAt = Math.Min(1.0, fastestRateICanWorkAt);

                //Here we might spill resources :(, no idea how to fix this because we have to somehow return them to our sender and then that one has to redistribute them or something, god knows
                //(It happens if the slowest rate is smaller another slowest rate)

                var whatINeedToGenerate = CalculateWhatINeedToGenerate(myCalculationObject);
                var whatICanGenerateTotal = CalculateWhatICanGenerate(ticks);
                var whatICanGenerateBasedOnProvidedResources = CalculateWhatICanGenerateBasedOnThisMaxWorkSpeed(whatICanGenerateTotal, fastestRateICanWorkAt);

                //Somehow distribute this workrate etc

                ProvideBasedOnResourcesAndRequests(whatINeedToGenerate, whatICanGenerateBasedOnProvidedResources, ticks, totalCalculation, myCalculationObject);
            }

        }


        public void SendOutMyRequestsIfNeeded(int ticks, TotalCalculationObject totalCalculation)
        {
            var myCalculationObject = totalCalculation.GetCalculationForGameObject(this);

            if (!(myCalculationObject.IHaveSendedOutMyRequests || myCalculationObject.IHaveRequestedToSendOutRequestsForMyParents))
            {
                var productionInfo = GameResourcesRepository.DetermineGameObjectInfo(this.GameObjectType).ProductionInfo;
                if (productionInfo.Count == 0)
                {
                    //I Generate nothing, let's send out requests
                    var resourcesINeed = CalculateResourcesINeed(1.0, ticks);
                    GoRequestResourcesFromChildren(resourcesINeed, ticks, totalCalculation, myCalculationObject);
                }
                else if (AllDestinations.Count() == 0)
                {
                    //I don't need anything but I still make a request because else my connections don't know when to start providing items
                    var resourcesINeed = new List<BackendResourceInfo>();
                    GoRequestResourcesFromChildren(resourcesINeed, ticks, totalCalculation, myCalculationObject);
                }
                else
                {
                    myCalculationObject.IHaveRequestedToSendOutRequestsForMyParents = true;
                    //Apparently I have parents that have not provided their request, let's kindly ask them what they need
                    //It could also be the case that they are still in the progress of sending their requests, thats why we do the where
                    foreach (var destination in AllDestinations.Where(t => !totalCalculation.GetCalculationForGameObject(t).IHaveSendedOutMyRequests))
                    {
                        destination.SendOutMyRequestsIfNeeded(ticks, totalCalculation);
                    }
                }
            }
        }


        private void GoRequestResourcesFromChildren(List<BackendResourceInfo> resourcesINeed, int ticks, TotalCalculationObject totalCalculation, CalculationForGameObject myCalculationObject)
        {
            //Make sure that I don't get updated again
            myCalculationObject.IHaveSendedOutMyRequests = true;

            //var requestToChildren = new RequestInfo(this, resourcesINeed, AllSources.ToList());

            var allSources = AllSources.ToList();



            foreach (var source in allSources)
            {
                var sourceProductionInfo = GameResourcesRepository.DetermineGameObjectInfo(source.GameObjectType).ProductionInfo;
                var resourcesINeedForThisResourceThing = new List<BackendResourceInfo>();
                foreach (var resourceINeed in resourcesINeed)
                {
                    var totalGeneratedOfThisResource = allSources.SelectMany(t => GameResourcesRepository.DetermineGameObjectInfo(t.GameObjectType).ProductionInfo).Where(t => t.ResourceType == resourceINeed.ResourceType).Sum(t => t.Value) * ticks;
                    var resourcePossiblyGeneratedByThisSource = sourceProductionInfo.FirstOrDefault(t => t.ResourceType == resourceINeed.ResourceType && t.Value > 0);
                    if (resourcePossiblyGeneratedByThisSource != null)
                    {
                        var resourceThisProviderPossiblyGenerates = resourcePossiblyGeneratedByThisSource.Value * ticks;


                        if (this.GameObjectType == GameObjectType.Storage)
                        {
                            //If we are a storage request what a specific resource can generate. We don't want to combine all resources
                            //because then we run into trouble that other storages don't know what is requested and we get an even more
                            //unfair distribution.

                            //Maybe for the future what we could do is something like, if connected to storage then request all I want
                            //And only provide to storages what I have left after all other provisions

                            resourcesINeedForThisResourceThing.Add(new BackendResourceInfo(resourceINeed.ResourceType, resourceThisProviderPossiblyGenerates));

                            //Maybe this should be up to a max of me filling up my storage + all requests that I got in
                            //So let's say I can store 800 Iron, I currently have 750 Iron but someone also requested 100
                            //We would have to do 800-750+100 = 150 so we can request a maximum of 150 Iron
                            //This should then possibly be redestributed over the requesters or maybe also then up to a maximum of what the provider can provide
                        }
                        else if (this.GameObjectType == GameObjectType.TradingBuilding)
                        {
                            //Do the same as with storage
                            resourcesINeedForThisResourceThing.Add(new BackendResourceInfo(resourceINeed.ResourceType, resourceThisProviderPossiblyGenerates));
                        }
                        else
                        {

                            var percentage = resourceThisProviderPossiblyGenerates / totalGeneratedOfThisResource;
                            var amountIShouldRequest = percentage * resourceINeed.Value;
                            resourcesINeedForThisResourceThing.Add(new BackendResourceInfo(resourceINeed.ResourceType, amountIShouldRequest));
                        }
                    }
                }

                var requestToThisChild = new RequestInfo(this, resourcesINeedForThisResourceThing, AllSources.ToList());
                source.Request(requestToThisChild, ticks, totalCalculation);
            }
        }



        public List<BackendResourceInfo> CalculateWhatICanGenerate(int ticks)
        {
            var productionInfo = GameResourcesRepository.DetermineGameObjectInfo(this.GameObjectType).ProductionInfo;
            return productionInfo.Select(t => new BackendResourceInfo(t.ResourceType, t.Value * ticks)).ToList();
        }

        public List<BackendResourceInfo> CalculateWhatICanGenerateBasedOnThisMaxWorkSpeed(List<BackendResourceInfo> whatICanGenerateTotal, double maxWorkSpeed)
        {
            return whatICanGenerateTotal.Select(t => new BackendResourceInfo(t.ResourceType, t.Value * maxWorkSpeed)).ToList();
        }

        public List<BackendResourceInfo> CalculateWhatINeedToGenerate(CalculationForGameObject myCalculationObject)
        {
            var whatINeedToGenerate = new List<BackendResourceInfo>();
            var combinedRequestResources = myCalculationObject.Requests.SelectMany(t => t.ResourcesRequested);

            var productionInfo = GameResourcesRepository.DetermineGameObjectInfo(this.GameObjectType).ProductionInfo;

            foreach (var resourceType in productionInfo.Select(t => t.ResourceType))
            {
                var requestedReqourcesTotal = combinedRequestResources.Where(t => t.ResourceType == resourceType).Sum(t => t.Value);

                whatINeedToGenerate.Add(new BackendResourceInfo(resourceType, requestedReqourcesTotal));
            }
            return whatINeedToGenerate;
        }

        public double CalculateWorkRate(List<BackendResourceInfo> whatICanGenerate, List<BackendResourceInfo> whatINeedToGenerate)
        {
            double highestRateINeedToWorkAt = 0.0;

            foreach (var canGenerate in whatICanGenerate)
            {
                var needToGenerate = whatINeedToGenerate.FirstOrDefault();
                if (needToGenerate != null)
                {
                    var rateINeedToWorkAt = needToGenerate.Value / canGenerate.Value;
                    if (rateINeedToWorkAt > highestRateINeedToWorkAt)
                    {
                        highestRateINeedToWorkAt = rateINeedToWorkAt;
                    }
                }
            }

            return highestRateINeedToWorkAt;
        }

        public List<BackendResourceInfo> CalculateResourcesINeed(double rateINeedToWorkAt, int ticks)
        {
            var consumptionInfo = GameResourcesRepository.DetermineGameObjectInfo(this.GameObjectType).ConsumptionInfo;

            var resourcesINeed = consumptionInfo.Select(t => new BackendResourceInfo(t.ResourceType, t.Value * rateINeedToWorkAt * ticks)).ToList();
            return resourcesINeed;
        }
    }
}