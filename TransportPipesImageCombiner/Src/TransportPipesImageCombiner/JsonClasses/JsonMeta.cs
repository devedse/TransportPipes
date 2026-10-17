using System;

namespace TransportPipesImageCombiner.JsonClasses
{
    internal class JsonMeta
    {
        public String app { get; set; }

        public String version { get; set; }

        public String image { get; set; }

        public String format { get; set; }

        public JsonSize size { get; set; }

        public int scale { get; set; }
    }
}