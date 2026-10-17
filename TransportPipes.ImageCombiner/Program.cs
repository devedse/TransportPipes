using TransportPipes.ImageCombiner;

if (args.Length is < 1 or > 2)
{
    Console.WriteLine("Combines all images of the game objects into one sprite sheet (TexturePacker JSON hash format, used by PixiJS).");
    Console.WriteLine();
    Console.WriteLine("Usage: TransportPipes.ImageCombiner <input directory> [output directory]");
    Console.WriteLine();
    Console.WriteLine("  <input directory>   A directory with a png per image, animations are named Name-0.png, Name-1.png, ...");
    Console.WriteLine("  [output directory]  Where combinedimage.png, combinedjson.json and combinedjsoninfo.json are written");
    Console.WriteLine("                      (default: the current directory)");
    return 1;
}

var inputDirectory = args[0];
var outputDirectory = args.Length > 1 ? args[1] : Directory.GetCurrentDirectory();

if (!Directory.Exists(inputDirectory))
{
    Console.Error.WriteLine($"Input directory '{inputDirectory}' does not exist.");
    return 1;
}

Directory.CreateDirectory(outputDirectory);

var combiner = new ImageCombiner(inputDirectory, outputDirectory);
combiner.GoCombineImages();
return 0;
