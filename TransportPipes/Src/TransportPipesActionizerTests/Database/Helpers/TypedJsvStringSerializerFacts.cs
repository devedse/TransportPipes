using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransportPipesCommon.Enums;
using TransportPipesDatabase.Helpers;
using TransportPipesDatabase.Poco.ActionDetails;

namespace TransportPipesActionizerTests.Database.Helpers
{
    namespace TypedJsvStringSerializerFacts
    {
        [TestClass]
        public class SerializeAndDeserializeMethodsOfTypedJsvStringSerialized
        {
            [TestMethod]
            public void CorrectlySerializeAndDeserializeTypedObjects()
            {
                var ser = new TypedJsvStringSerializer();

                var listOfActionDetails = new List<ActionDetailsBase>();

                for (int i = 0; i < 10000; i++)
                {
                    if (i % 2 == 0)
                    {
                        var act = new ActionDetailsCreateBuilding() { GameObject = i % 2 == 0 ? GameObjectType.TradingBuilding : GameObjectType.SolarPanel };
                        listOfActionDetails.Add(act);
                    }
                    else
                    {
                        var act = new ActionDetailsResearch() { ResearchLevel = i * 20, NogWat = new DateTime(2015, 1, 1) };
                        listOfActionDetails.Add(act);
                    }
                }

                var results = new List<string>();

                foreach (var item in listOfActionDetails)
                {
                    var result = ser.SerializeToString(item);
                    results.Add(result);
                }



                for (int i = 0; i < results.Count; i++)
                {
                    var str = results[i];
                    var deserialized = ser.DeserializeFromString(str, typeof(ActionDetailsBase));

                    if (i % 2 == 0)
                    {
                        Assert.IsNotNull(deserialized as ActionDetailsCreateBuilding);
                        Assert.IsNull(deserialized as ActionDetailsResearch);
                    }
                    else
                    {
                        Assert.IsNull(deserialized as ActionDetailsCreateBuilding);
                        Assert.IsNotNull(deserialized as ActionDetailsResearch);
                    }
                }
            }
        }
    }
}
