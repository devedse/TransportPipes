using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using TransportPipesV1.Enums;
using TransportPipesV1.Repositories;

namespace TransportPipesV1
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            // Register the default hubs route: ~/signalr/hubs
            RouteTable.Routes.MapHubs();

            //Set up a basic example pipe
            //var gameObjectsRepository = new GameObjectsRepository();

            //for (int x = 0; x < 10; x++)
            //{
            //    gameObjectsRepository.SetGameObjectTile(x, 0, GameObjectsEnumDb.GreyPipe);
            //}

            //for (int y = 0; y < 100; y++)
            //{
            //    gameObjectsRepository.SetGameObjectTile(0, y, GameObjectsEnumDb.GreyPipe);
            //}

            //for (int x = 10; x < 100; x++)
            //{
            //    gameObjectsRepository.SetGameObjectTile(x, (x - 10) * -1, GameObjectsEnumDb.GreyPipe);
            //    gameObjectsRepository.SetGameObjectTile(x + 1, (x - 10) * -1, GameObjectsEnumDb.GreyPipe);
            //}
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}