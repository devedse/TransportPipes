using System.Text.Json;
using SkiaSharp;

namespace TransportPipes.ImageCombiner;

internal class ImageCombiner
{
    private readonly Dictionary<string, ImageSequence> imageSequences = new Dictionary<string, ImageSequence>();
    private readonly Dictionary<string, ImageObject> imageObjects = new Dictionary<string, ImageObject>();

    private const string OutputImage = "combinedimage.png";
    private const string OutputJson = "combinedjson.json";
    private const string OutputJsonInfo = "combinedjsoninfo.json";

    private const int MaxWidth = 1024;

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

    private readonly string _inputDirectory;
    private readonly string _outputDirectory;

    public ImageCombiner(string inputDirectory, string outputDirectory)
    {
        _inputDirectory = inputDirectory;
        _outputDirectory = outputDirectory;
    }

    public void GoCombineImages()
    {
        LoadFromDisk();

        try
        {
            var allImages = imageSequences.SelectMany(t => t.Value.ImageObjects).Concat(imageObjects.Values).ToList();
            if (allImages.Count == 0)
            {
                Console.WriteLine($"No png files found in '{_inputDirectory}'.");
                return;
            }

            var listies = PutImagesInList(allImages);

            var totalWidth = listies.Max(t => t.Sum(z => z.Bitmap.Width + 1));
            var totalHeight = listies.Sum(t => t.Max(z => z.Bitmap.Height + 1));

            SaveAsImage(listies, totalWidth, totalHeight);
            GenerateAndSaveJson(allImages, totalWidth, totalHeight);
            GenerateAndSaveJsonInfo();
        }
        finally
        {
            foreach (var image in imageSequences.Values.SelectMany(t => t.ImageObjects).Concat(imageObjects.Values))
            {
                image.Bitmap.Dispose();
            }
        }
    }

    private void LoadFromDisk()
    {
        //Sort the files so the result is the same on every operating system
        var allFiles = Directory.GetFiles(_inputDirectory, "*.png").Order(StringComparer.Ordinal);

        foreach (var file in allFiles)
        {
            var bitmapje = SKBitmap.Decode(file) ?? throw new InvalidOperationException($"Could not read image '{file}'.");
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
                if (!imageSequences.TryGetValue(realName, out var imageSequence))
                {
                    imageSequence = new ImageSequence(realName);
                    imageSequences.Add(realName, imageSequence);
                }

                imageSequence.ImageObjects.Add(imageObject);
            }
        }

        //Animation frames are ordered by their number (Name-2 before Name-10)
        foreach (var imageSequence in imageSequences.Values)
        {
            imageSequence.ImageObjects.Sort((a, b) => FrameNumber(a.Name).CompareTo(FrameNumber(b.Name)));
        }
    }

    private static int FrameNumber(string name)
    {
        return int.TryParse(name[(name.LastIndexOf('-') + 1)..], out var number) ? number : 0;
    }

    private static List<List<ImageObject>> PutImagesInList(IEnumerable<ImageObject> allImages)
    {
        var allImagesQueue = new Queue<ImageObject>(allImages);

        var listies = new List<List<ImageObject>>();

        var curList = new List<ImageObject>();
        int x = 0;
        while (allImagesQueue.Count > 0)
        {
            var curimage = allImagesQueue.Peek();

            //An image that is wider than the maximum width gets a row of its own
            if (x + curimage.Bitmap.Width < MaxWidth || curList.Count == 0)
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

        using var totalBitmap = new SKBitmap(new SKImageInfo(totalWidth, totalHeight, SKColorType.Rgba8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(totalBitmap))
        {
            canvas.Clear(SKColors.Transparent);

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
                    canvas.DrawBitmap(curItem.Bitmap, x, y, SKSamplingOptions.Default, null);
                    curItem.Location = SKRectI.Create(x, y, curItem.Bitmap.Width, curItem.Bitmap.Height);
                    x += curItem.Bitmap.Width + 1;
                }

                y += rowHeight;
                rowNumber++;
            }
        }

        using var image = SKImage.FromBitmap(totalBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(Path.Combine(_outputDirectory, OutputImage));
        data.SaveTo(stream);
    }

    private void GenerateAndSaveJson(IEnumerable<ImageObject> allImages, int totalWidth, int totalHeight)
    {
        var frames = new Dictionary<string, JsonImage>();
        foreach (var image in allImages)
        {
            var location = image.Location;
            frames.Add(image.Name, new JsonImage(
                Frame: new JsonRectangle(location.Left, location.Top, location.Width, location.Height),
                Rotated: false,
                Trimmed: false,
                SpriteSourceSize: new JsonRectangle(0, 0, location.Width, location.Height),
                SourceSize: new JsonSize(location.Width, location.Height)));
        }

        var meta = new JsonMeta(
            App: "TransportPipes.ImageCombiner",
            Version: "1.0",
            Image: OutputImage,
            Format: "RGBA8888",
            Size: new JsonSize(totalWidth, totalHeight),
            Scale: 1);

        var json = JsonSerializer.Serialize(new RootJsonObject(frames, meta), JsonOptions);
        File.WriteAllText(Path.Combine(_outputDirectory, OutputJson), json);
        Console.WriteLine($"Wrote {frames.Count} frames to {OutputJson}");
    }

    private void GenerateAndSaveJsonInfo()
    {
        var imageInfos = new Dictionary<string, JsonInfoImage>();

        foreach (var seq in imageSequences.Values)
        {
            imageInfos.Add(seq.Name, new JsonInfoImage(seq.ImageObjects.Count));
        }

        foreach (var img in imageObjects.Values)
        {
            imageInfos.Add(img.Name, new JsonInfoImage(1));
        }

        var json = JsonSerializer.Serialize(new RootInfoJsonObject(imageInfos, new JsonInfoMeta(OutputJson)), JsonOptions);
        File.WriteAllText(Path.Combine(_outputDirectory, OutputJsonInfo), json);
        Console.WriteLine($"Wrote {imageInfos.Count} image infos to {OutputJsonInfo}");
    }
}
