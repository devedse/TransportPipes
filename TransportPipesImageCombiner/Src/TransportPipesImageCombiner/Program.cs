using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesImageCombiner
{
    class Program
    {
        static void Main(string[] args)
        {
            ImageCombiner combiner = new ImageCombiner();
            combiner.GoCombineImages();

            Console.ReadLine();
        }
    }
}
