using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesImageCombiner.JsonClasses
{
    class JsonImage
    {
        public JsonFrame frame { get; set; }

        public Boolean rotated;
        public Boolean trimmed;

        public JsonSpriteSourceSize spriteSourceSize { get; set; }
        public JsonSize sourceSize { get; set; }
    }
}
