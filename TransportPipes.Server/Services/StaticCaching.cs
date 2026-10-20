namespace TransportPipes.Server.Services;

/// <summary>
/// How long browsers keep the frontend's files. Vite's assets have a hash in their name and the art is asked for with
/// its content hash (<c>?v=</c>, see art/process.mjs), so those are kept for good. The other pictures may be a day
/// old and are refreshed in the background; the page and the art's manifest are checked every time.
/// </summary>
public static class StaticCaching
{
    public const string Forever = "public, max-age=31536000, immutable";
    public const string Pictures = "public, max-age=3600, stale-while-revalidate=86400";
    public const string Check = "no-cache";

    public static string For(PathString path, IQueryCollection query)
    {
        if (path.StartsWithSegments("/assets") || (path.StartsWithSegments("/art") && query.ContainsKey("v")))
        {
            return Forever;
        }

        var extension = Path.GetExtension(path.Value ?? "");
        return extension is ".webp" or ".png" or ".svg" ? Pictures : Check;
    }

    public static void Apply(HttpContext context) =>
        context.Response.Headers.CacheControl = For(context.Request.Path, context.Request.Query);
}
