using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TransportPipesV1.Helpers.AStarPathfinding
{
    public class AStarPoint
    {
        public int X { get; set; }
        public int Y { get; set; }

        public AStarPoint(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}