using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TransportPipesCommon.Helpers
{
    public class Point
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }
            
            var p = obj as Point;

            return Equals(p);
        }

        public bool Equals(Point p)
        {
            // If parameter is null return false:
            if (p == null)
            {
                return false;
            }

            // Return true if the fields match:
            return (X == p.X) && (Y == p.Y);
        }
    }
}