using System;
using System.Collections.Generic;

namespace TransportPipesImageCombiner.JsonClasses
{
    internal class RootJsonObject
    {
        public Dictionary<String, JsonImage> frames = new Dictionary<string, JsonImage>();

        public JsonMeta meta { get; set; }
    }
}