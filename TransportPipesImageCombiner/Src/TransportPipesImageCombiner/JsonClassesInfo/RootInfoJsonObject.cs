using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesImageCombiner.JsonClassesInfo
{
    class RootInfoJsonObject
    {
        public Dictionary<String, JsonInfoImage> imageinfos = new Dictionary<String, JsonInfoImage>();

        public JsonInfoMeta meta { get; set; }
    }
}
