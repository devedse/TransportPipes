using System.Text.Json.Serialization;

namespace TransportPipes.ImageCombiner;

// The TexturePacker "JSON hash" format that PixiJS reads as a spritesheet

internal record RootJsonObject(
    [property: JsonPropertyName("frames")] Dictionary<string, JsonImage> Frames,
    [property: JsonPropertyName("meta")] JsonMeta Meta);

internal record JsonImage(
    [property: JsonPropertyName("frame")] JsonRectangle Frame,
    [property: JsonPropertyName("rotated")] bool Rotated,
    [property: JsonPropertyName("trimmed")] bool Trimmed,
    [property: JsonPropertyName("spriteSourceSize")] JsonRectangle SpriteSourceSize,
    [property: JsonPropertyName("sourceSize")] JsonSize SourceSize);

internal record JsonRectangle(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("w")] int W,
    [property: JsonPropertyName("h")] int H);

internal record JsonSize(
    [property: JsonPropertyName("w")] int W,
    [property: JsonPropertyName("h")] int H);

internal record JsonMeta(
    [property: JsonPropertyName("app")] string App,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("image")] string Image,
    [property: JsonPropertyName("format")] string Format,
    [property: JsonPropertyName("size")] JsonSize Size,
    [property: JsonPropertyName("scale")] int Scale);

// The amount of frames per image (combinedjsoninfo.json)

internal record RootInfoJsonObject(
    [property: JsonPropertyName("imageinfos")] Dictionary<string, JsonInfoImage> ImageInfos,
    [property: JsonPropertyName("meta")] JsonInfoMeta Meta);

internal record JsonInfoImage([property: JsonPropertyName("count")] int Count);

internal record JsonInfoMeta([property: JsonPropertyName("OtherJsonFile")] string OtherJsonFile);
