//using DeveMazeGenerator;
//using DeveMazeGenerator.Generators;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using TransportPipesCommon.Enums;
//using TransportPipesDatabase;
//using TransportPipesDatabase.Poco;
//using TransportPipesDatabase.Poco.ActionDetails;

//namespace TransportPipesActionizerTests.Database
//{
//    [TestClass]
//    public class CreateMazeInActions
//    {
//        [TestMethod]
//        public void FillDbWithMaze()
//        {
//            var curTime = DateTime.Now;

//            ITransportDatabaseFrontEnd transportDb = new TransportPipesDatabaseOrmLite();

//            var generator = new AlgorithmBacktrackSmartMemory();
//            var maze = generator.Generate(64, 64, InnerMapType.BitArreintjeFast, 1337, null);

//            var allActions = new List<DbAction>();

//            var deletesForAll = new List<DbAction>();

//            for (int y = 0; y < maze.Height; y++)
//            {
//                for (int x = 0; x < maze.Width; x++)
//                {
//                    if (maze.InnerMap[x, y])
//                    {
//                        var curAction = new DbAction()
//                        {
//                            DateToBeExecuted = curTime,
//                            ExecutedById = 1,
//                            TransportAction = TransportAction.CreateBuilding,
//                            ActionDetails = new ActionDetailsCreateBuilding()
//                            {
//                                X = x - 30 - maze.Width,
//                                Y = y,
//                                GameObject = GameObjectType.GreyPipe
//                            }
//                        };

//                        allActions.Add(curAction);

//                        var deleteAction = new DbAction()
//                        {
//                            DateToBeExecuted = curTime,
//                            ExecutedById = 1,
//                            TransportAction = TransportAction.DeleteBuilding,
//                            ActionDetails = new ActionDetailsDeleteBuilding()
//                            {
//                                X = x - 30 - maze.Width,
//                                Y = y
//                            }
//                        };

//                        deletesForAll.Add(deleteAction);
//                    }
//                }
//            }


//            foreach (var createAction in allActions)
//            {
//                transportDb.AddDbAction(createAction);
//                Thread.Sleep(100);
//            }

//            //foreach (var deleteAction in deletesForAll)
//            //{
//            //    transportDb.AddDbAction(deleteAction);
//            //    Thread.Sleep(100);
//            //}
//        }
//    }
//}
