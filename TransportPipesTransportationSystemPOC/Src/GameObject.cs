using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesTransportationSystem
{
    public class GameObject
    {
        public String Name { get; private set; }
        public List<ResourceInfo> ConsumptionInfo { get; private set; }
        public List<ResourceInfo> GenerationInfo { get; private set; }

        public List<Connection> Connections = new List<Connection>();


        public IEnumerable<GameObject> AllDestinations
        {
            get { return Connections.Where(t => t.Source.Any(z => z == this)).SelectMany(t => t.Destination); }
        }

        public IEnumerable<GameObject> AllSources
        {
            get { return Connections.Where(t => t.Destination.Any(z => z == this)).SelectMany(t => t.Source); }
        }





        public GameObject(String name, List<ResourceInfo> consumptionInfo, List<ResourceInfo> generationInfo)
        {
            this.Name = name;
            this.ConsumptionInfo = consumptionInfo;
            this.GenerationInfo = generationInfo;
        }

        public string ConsumptionInfoToString(int ticks)
        {
            return String.Join(",", ConsumptionInfo.Select(t => " " + t.ResourceType + ": " + t.Value * ticks));
        }

        public string GenerationInfoToString(int ticks)
        {
            return String.Join(",", GenerationInfo.Select(t => " " + t.ResourceType + ": " + t.Value * ticks));
        }


        public void Request(RequestInfo request, int ticks, TotalCalculationObject totalCalculation)
        {
            var myCalculationObject = totalCalculation.GetCalculationForGameObject(this);

            Console.WriteLine("Request from '" + request.Requester.Name + "' to '" + this.Name + "' with '" + request.ToString() + "'");
            Console.WriteLine("Of total able to generate: '" + GenerationInfoToString(ticks) + "'");

            myCalculationObject.DestinationsHandled++;
            myCalculationObject.Requests.Add(request);

            var destinationCount = AllDestinations.Count();

            if (myCalculationObject.DestinationsHandled < destinationCount)
            {
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
                    Console.WriteLine("An extra request is made on '" + Name + "'. If this is due to debugging it's okay.");
                }

                //Handle request somehow
                var whatICanGenerate = CalculateWhatICanGenerate(ticks);
                var whatINeedToGenerate = CalculateWhatINeedToGenerate(myCalculationObject);
                var rateINeedToWorkAt = CalculateWorkRate(whatICanGenerate, whatINeedToGenerate);

                //Somehow distribute this workrate etc
                rateINeedToWorkAt = Math.Min(1.0, rateINeedToWorkAt);


                if (ConsumptionInfo.Count == 0)
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

        public List<ResourceInfo> FixRequestsBasedOnOtherProviders(List<ResourceInfo> whatINeedToGenerate, int ticks, CalculationForGameObject myCalculationObject)
        {
            //If we could somehow use the real numbers we can generate here it would be nice.
            //We can't though because that would mean we need to loop through other's read numbers too (for the maxGeneratedCombinedWithOtherProviders)
            var listOfWhatICanGenerateMax = CalculateWhatICanGenerate(ticks);
            var resourcesThatINeedToGenerateWhenKnowingAboutTheOtherProviders = new List<ResourceInfo>();

            foreach (var currentRequest in myCalculationObject.Requests)
            {
                var resourcesProvidedToThisRequester = new List<ResourceInfo>();

                //Now loop through all resources in this request and determine what percentage of this resource we need to give out to this requester
                foreach (var requestResourceType in currentRequest.ResourcesRequested)
                {
                    var canIGenerateThis = GenerationInfo.Any(t => t.ResourceType == requestResourceType.ResourceType);
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
                            elementInListToReturn = new ResourceInfo(requestResourceType.ResourceType, totalRequested.Value);
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

        public void ProvideBasedOnResourcesAndRequests(List<ResourceInfo> whatINeedToGenerateMax, List<ResourceInfo> whatICanGenerate, int ticks, TotalCalculationObject totalCalculation, CalculationForGameObject myCalculationObject)
        {
            var whatINeedToGenerate = FixRequestsBasedOnOtherProviders(whatINeedToGenerateMax, ticks, myCalculationObject);


            //Apparently I am a storage or a windmill or something

            foreach (var currentRequest in myCalculationObject.Requests)
            {
                var resourcesProvidedToThisRequester = new List<ResourceInfo>();

                //Now loop through all resources in this request and determine what percentage of this resource we need to give out to this requester
                foreach (var requestResourceType in currentRequest.ResourcesRequested)
                {
                    var canIGenerateThis = GenerationInfo.Any(t => t.ResourceType == requestResourceType.ResourceType);
                    if (canIGenerateThis)
                    {
                        var totalRequested = whatINeedToGenerate.First(t => t.ResourceType == requestResourceType.ResourceType);
                        var totalGenerated = whatICanGenerate.First(t => t.ResourceType == requestResourceType.ResourceType);

                        var maxGeneratedCombinedWithOtherProviders = currentRequest.OtherProviders.SelectMany(t => t.GenerationInfo).Where(t => t.ResourceType == requestResourceType.ResourceType).Sum(t => t.Value);

                        var resourcesICouldHaveGeneratedMax = this.CalculateWhatICanGenerate(ticks).First(t => t.ResourceType == requestResourceType.ResourceType).Value;
                        var howMuchPercentIGenerateBasedOnTotal = resourcesICouldHaveGeneratedMax / (maxGeneratedCombinedWithOtherProviders * ticks);

                        var percentageToGive = (requestResourceType.Value * howMuchPercentIGenerateBasedOnTotal) / totalRequested.Value;
                        //If the percentage is above 1, round it back to 1, else we give more then they can handle (Exploding factories are dangerous!!!)
                        //We could also do it * workSpeed or something
                        percentageToGive = Math.Min(1.0, percentageToGive);

                        resourcesProvidedToThisRequester.Add(new ResourceInfo(requestResourceType.ResourceType, totalGenerated.Value * percentageToGive));
                    }


                }
                var ding = new ProvisionInfo(resourcesProvidedToThisRequester, this);

                currentRequest.Requester.Provide(ding, ticks, totalCalculation);
            }
        }


        public void Provide(ProvisionInfo provisionInfo, int ticks, TotalCalculationObject totalCalculation)
        {
            var myCalculationObject = totalCalculation.GetCalculationForGameObject(this);

            Console.WriteLine("Provision from '" + provisionInfo.Provisioner.Name + "' to '" + this.Name + "' with '" + provisionInfo.ToString() + "'");
            Console.WriteLine("Of total able to consume: '" + ConsumptionInfoToString(ticks) + "'");

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
            }
            else
            {
                //Temp code to store resources when I am a storage
                if (this.Name == "Storage")
                {
                    foreach (var resourceInfo in myCalculationObject.Provisions.SelectMany(t => t.ResourcesProvided))
                    {
                        var currentResourceTypeInStorage = GenerationInfo.FirstOrDefault(t => t.ResourceType == resourceInfo.ResourceType);
                        if (currentResourceTypeInStorage == null)
                        {
                            currentResourceTypeInStorage = new ResourceInfo(resourceInfo.ResourceType, resourceInfo.Value);
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

                foreach (var resourceINeed in ConsumptionInfo)
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

            if (!myCalculationObject.IHaveSendedOutMyRequests)
            {
                if (GenerationInfo.Count == 0)
                {
                    //I Generate nothing, let's send out requests
                    var resourcesINeed = CalculateResourcesINeed(1.0, ticks);
                    GoRequestResourcesFromChildren(resourcesINeed, ticks, totalCalculation, myCalculationObject);
                }
                else if (AllDestinations.Count() == 0)
                {
                    //I don't need anything but I still make a request because else my connections don't know when to start providing items
                    var resourcesINeed = new List<ResourceInfo>();
                    GoRequestResourcesFromChildren(resourcesINeed, ticks, totalCalculation, myCalculationObject);
                }
                else
                {
                    //Apparently I have parents that have not provided their request, let's kindly ask them what they need
                    //It could also be the case that they are still in the progress of sending their requests, thats why we do the where
                    foreach (var destination in AllDestinations.Where(t => !totalCalculation.GetCalculationForGameObject(t).IHaveSendedOutMyRequests))
                    {
                        destination.SendOutMyRequestsIfNeeded(ticks, totalCalculation);
                    }
                }
            }
        }


        private void GoRequestResourcesFromChildren(List<ResourceInfo> resourcesINeed, int ticks, TotalCalculationObject totalCalculation, CalculationForGameObject myCalculationObject)
        {
            //Make sure that I don't get updated again
            myCalculationObject.IHaveSendedOutMyRequests = true;

            var requestToChildren = new RequestInfo(this, resourcesINeed, AllSources.ToList());

            foreach (var source in AllSources)
            {
                source.Request(requestToChildren, ticks, totalCalculation);
            }
        }



        public List<ResourceInfo> CalculateWhatICanGenerate(int ticks)
        {
            return GenerationInfo.Select(t => new ResourceInfo(t.ResourceType, t.Value * ticks)).ToList();
        }

        public List<ResourceInfo> CalculateWhatICanGenerateBasedOnThisMaxWorkSpeed(List<ResourceInfo> whatICanGenerateTotal, double maxWorkSpeed)
        {
            return whatICanGenerateTotal.Select(t => new ResourceInfo(t.ResourceType, t.Value * maxWorkSpeed)).ToList();
        }

        public List<ResourceInfo> CalculateWhatINeedToGenerate(CalculationForGameObject myCalculationObject)
        {
            var whatINeedToGenerate = new List<ResourceInfo>();
            var combinedRequestResources = myCalculationObject.Requests.SelectMany(t => t.ResourcesRequested);

            foreach (var resourceType in GenerationInfo.Select(t => t.ResourceType))
            {
                var requestedReqourcesTotal = combinedRequestResources.Where(t => t.ResourceType == resourceType).Sum(t => t.Value);

                whatINeedToGenerate.Add(new ResourceInfo(resourceType, requestedReqourcesTotal));
            }
            return whatINeedToGenerate;
        }

        public double CalculateWorkRate(List<ResourceInfo> whatICanGenerate, List<ResourceInfo> whatINeedToGenerate)
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

        public List<ResourceInfo> CalculateResourcesINeed(double rateINeedToWorkAt, int ticks)
        {
            var resourcesINeed = ConsumptionInfo.Select(t => new ResourceInfo(t.ResourceType, t.Value * rateINeedToWorkAt * ticks)).ToList();
            return resourcesINeed;
        }
    }
}
