using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesImageCombiner
{
    class ImageSequence
    {
        public String Name { get; set; }
        public List<ImageObject> ImageObjects = new List<ImageObject>();
        public ImageSequence(String name)
        {
            this.Name = name;
        }
    }
}
