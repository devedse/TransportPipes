namespace TransportPipes.Common.Helpers;

public static class GenericMathHelper
{
    public static Point GetChunkForPos(int x, int y)
    {
        int chunkX = (int)Math.Floor(x / (float)StaticConfigInfo.ChunkWidth);
        int chunkY = (int)Math.Floor(y / (float)StaticConfigInfo.ChunkHeight);
        return new Point(chunkX, chunkY);
    }

    public static Point ConvertPosToPosInChunk(int x, int y)
    {
        var inChunkX = x % StaticConfigInfo.ChunkWidth;
        var inChunkY = y % StaticConfigInfo.ChunkHeight;

        if (inChunkX < 0)
            inChunkX += StaticConfigInfo.ChunkWidth;

        if (inChunkY < 0)
            inChunkY += StaticConfigInfo.ChunkHeight;

        return new Point(inChunkX, inChunkY);
    }

    public static Point GetXYStartForChunkCoordinates(int chunkX, int chunkY)
    {
        return new Point(chunkX * StaticConfigInfo.ChunkWidth, chunkY * StaticConfigInfo.ChunkHeight);
    }
}
