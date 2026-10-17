using SkiaSharp;

namespace TransportPipes.ImageCombiner;

internal class ImageObject
{
    public string Name { get; }
    public SKBitmap Bitmap { get; }
    public SKRectI Location { get; set; }

    public ImageObject(string name, SKBitmap bitmap)
    {
        this.Name = name;
        this.Bitmap = bitmap;
    }
}
