using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportPipesImageCombiner
{
    class ImageObject
    {
        public String Name { get; set; }
        public Bitmap Bitmap { get; set; }

        public Rectangle location { get; set; }

        public ImageObject(String name, Bitmap bitmap)
        {
            this.Name = name;
            this.Bitmap = bitmap;
        }


    }
}
