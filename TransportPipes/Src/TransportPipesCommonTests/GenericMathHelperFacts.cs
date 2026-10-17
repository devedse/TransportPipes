using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TransportPipesCommon.Helpers;

namespace TransportPipesCommonTests
{
    namespace GenericMathHelperFacts
    {
        [TestClass]
        public class GenericMathHelperTheGetXYStartForChunkCoordinatesMethod
        {
            [TestMethod]
            public void WorksForMinutValues()
            {
                //Act
                var result = GenericMathHelper.GetXYStartForChunkCoordinates(-1, -2);

                //Assert
                Assert.AreEqual(-8, result.X);
                Assert.AreEqual(-16, result.Y);
            }

            [TestMethod]
            public void WorksForZero()
            {
                //Act
                var result = GenericMathHelper.GetXYStartForChunkCoordinates(0, 0);

                //Assert
                Assert.AreEqual(0, result.X);
                Assert.AreEqual(0, result.Y);
            }

            [TestMethod]
            public void WorksForPositiveValues()
            {
                //Act
                var result = GenericMathHelper.GetXYStartForChunkCoordinates(1, 2);

                //Assert
                Assert.AreEqual(8, result.X);
                Assert.AreEqual(16, result.Y);
            }
        }
    }
}
