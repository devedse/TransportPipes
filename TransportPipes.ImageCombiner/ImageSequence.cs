namespace TransportPipes.ImageCombiner;

internal class ImageSequence
{
    public string Name { get; }
    public List<ImageObject> ImageObjects { get; } = new List<ImageObject>();

    public ImageSequence(string name)
    {
        this.Name = name;
    }
}
