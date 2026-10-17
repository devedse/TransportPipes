using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using TransportPipesImageCombiner.JsonClasses;
using TransportPipesImageCombiner.JsonClassesInfo;

namespace TransportPipesImageCombiner
{
    internal class ImageCombiner
    {
        private Dictionary<string, ImageSequence> imageSequences = new Dictionary<string, ImageSequence>();
        private Dictionary<string, ImageObject> imageObjects = new Dictionary<string, ImageObject>();

        private const string OutputImage = "combinedimage.png";
        private const string OutputJson = "combinedjson.json";
        private const string OutputJsonInfo = "combinedjsoninfo.json";
        private const int MaxWidth = 1024;

        public ImageCombiner()
        {

        }

        public void GoCombineImages()
        {
            LoadFromDisk();

            var allImages = imageSequences.SelectMany(t => t.Value.ImageObjects).Concat(imageObjects.Values);

            var listies = PutImagesInList(allImages);

            var totalWidth = listies.Max(t => t.Sum(z => z.Bitmap.Width + 1));
            var totalHeight = listies.Sum(t => t.Max(z => z.Bitmap.Height + 1));
            SaveAsImage(listies, totalWidth, totalHeight);

            GenerateAndSaveJson(allImages, totalWidth, totalHeight);

            GenerateAndSaveJsonInfo();
        }

        private void LoadFromDisk()
        {
            var dir = @"D:\Dropbox\Transport\Kapot Geknipte art";

            var allFiles = Directory.GetFiles(dir, "*.png");

            foreach (var file in allFiles)
            {
                Bitmap bitmapje = new Bitmap(file);

                string fileName = Path.GetFileNameWithoutExtension(file);

                string[] splitted = fileName.Split('-');

                var imageObject = new ImageObject(fileName, bitmapje);

                if (splitted.Length == 1)
                {
                    imageObjects.Add(fileName, imageObject);
                }
                else
                {
                    var realName = splitted[0];
                    if (!imageSequences.ContainsKey(realName))
                    {
                        imageSequences.Add(realName, new ImageSequence(realName));
                    }

                    imageSequences[realName].ImageObjects.Add(imageObject);
                }
            }
        }

        private List<List<ImageObject>> PutImagesInList(IEnumerable<ImageObject> allImages)
        {
            var allImagesQueue = new Queue<ImageObject>(allImages);

            var listies = new List<List<ImageObject>>();

            var curList = new List<ImageObject>();

            int x = 0;
            while (allImagesQueue.Count > 0)
            {
                var curimage = allImagesQueue.Peek();
                if (x + curimage.Bitmap.Width < MaxWidth)
                {
                    curList.Add(curimage);
                    x += curimage.Bitmap.Width;
                    allImagesQueue.Dequeue();
                }
                else
                {
                    listies.Add(curList);
                    curList = new List<ImageObject>();
                    x = 0;
                }
            }

            if (curList.Count > 0)
            {
                listies.Add(curList);
            }

            return listies;
        }

        private void SaveAsImage(List<List<ImageObject>> listies, int totalWidth, int totalHeight)
        {
            Console.WriteLine("Combining image, total size: Width: {0}, Height: {1}", totalWidth, totalHeight);

            var totalBitmap = new Bitmap(totalWidth, totalHeight);

            var graphics = Graphics.FromImage(totalBitmap);

            int rowNumber = 0;
            int y = 0;
            foreach (var curRow in listies)
            {
                int x = 0;
                int rowWidth = curRow.Sum(z => z.Bitmap.Width + 1);
                int rowHeight = curRow.Max(z => z.Bitmap.Height + 1);

                Console.WriteLine("Row {0} has Width: {1} and Height: {2}", rowNumber, rowWidth, rowHeight);

                foreach (var curItem in curRow)
                {
                    Console.WriteLine("    Image {0}, X: {1}, Y: {2}, Width: {3}, Height: {4}", curItem.Name, x, y, curItem.Bitmap.Width, curItem.Bitmap.Height);

                    graphics.DrawImage(curItem.Bitmap, x, y, curItem.Bitmap.Width, curItem.Bitmap.Height);

                    curItem.location = new Rectangle(x, y, curItem.Bitmap.Width, curItem.Bitmap.Height);

                    x += curItem.Bitmap.Width + 1;
                }
                y += rowHeight;
            }

            totalBitmap.Save(OutputImage);
        }

        private void GenerateAndSaveJson(IEnumerable<ImageObject> allImages, int totalWidth, int totalHeight)
        {
            var rjo = new RootJsonObject();

            foreach (var image in allImages)
            {
                var curJsonImage = new JsonImage();

                curJsonImage.frame = new JsonFrame() { x = image.location.X, y = image.location.Y, w = image.location.Width, h = image.location.Height };
                curJsonImage.rotated = false;
                curJsonImage.trimmed = false;
                curJsonImage.spriteSourceSize = new JsonSpriteSourceSize() { x = image.location.X, y = image.location.Y, w = image.location.Width, h = image.location.Height };
                curJsonImage.sourceSize = new JsonSize() { w = image.location.Width, h = image.location.Height };

                rjo.frames.Add(image.Name, curJsonImage);
            }

            var meta = new JsonMeta()
            {
                app = "",
                version = "1.0",
                image = OutputImage,
                format = "RGBA8888",
                size = new JsonSize()
                {
                    w = totalWidth,
                    h = totalHeight
                },
                scale = 1
            };

            rjo.meta = meta;

            string json = JsonConvert.SerializeObject(rjo, Formatting.Indented);

            Console.WriteLine(json);

            File.WriteAllText(OutputJson, json);
        }

        private void GenerateAndSaveJsonInfo()
        {
            var rijo = new RootInfoJsonObject();

            foreach (var seq in imageSequences.Values)
            {
                var curImageInfo = new JsonInfoImage()
                {
                    count = seq.ImageObjects.Count
                };

                rijo.imageinfos.Add(seq.Name, curImageInfo);
            }

            foreach (var img in imageObjects.Values)
            {
                var curImageInfo = new JsonInfoImage()
                {
                    count = 1
                };

                rijo.imageinfos.Add(img.Name, curImageInfo);
            }

            var meta = new JsonInfoMeta()
            {
                OtherJsonFile = OutputJson
            };

            rijo.meta = meta;

            string json = JsonConvert.SerializeObject(rijo, Formatting.Indented);

            Console.WriteLine(json);

            File.WriteAllText(OutputJsonInfo, json);
        }
    }
}